using Dwsg.Shared;
using Microsoft.Data.Sqlite;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Dwsg.Persistence;

public sealed class SqliteWorldStore : IWorldStore, IDisposable
{
    private const int ApplicationId = 0x44575347;
    private const int SchemaVersion = 1;
    // ponytail: one connection for the five-player host; SQLite already serializes writes.
    private readonly object gate = new();
    private readonly SqliteConnection connection;
    private readonly Action<string>? commitProbe;

    public SqliteWorldStore(string databasePath) : this(databasePath, null) { }

    internal SqliteWorldStore(string databasePath, Action<string>? commitProbe)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
        string path = Path.GetFullPath(databasePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = path, Mode = SqliteOpenMode.ReadWriteCreate, Pooling = false, DefaultTimeout = 10
        }.ToString());
        this.commitProbe = commitProbe;
        try
        {
            connection.Open();
            Initialize();
        }
        catch
        {
            connection.Dispose();
            throw;
        }
    }

    public WorldState? Load(string worldId)
    {
        RequireId(worldId);
        lock (gate)
        {
            using var command = Command("SELECT revision,data_json,mappings_json FROM worlds WHERE world_id=$world", null,
                ("$world", worldId));
            WorldState state;
            using (var reader = command.ExecuteReader())
            {
                if (!reader.Read()) return null;
                state = new WorldState
                {
                    WorldId = worldId, Revision = reader.GetInt64(0),
                    Data = JObject.Parse(reader.GetString(1)), EntityMappings = JObject.Parse(reader.GetString(2))
                };
            }
            ValidateWorld(state);
            using var roles = Command("SELECT player_id,legacy_index FROM roles WHERE world_id=$world", null, ("$world", worldId));
            using var roleReader = roles.ExecuteReader();
            while (roleReader.Read())
                if (state.ResolvePlayerIndex(roleReader.GetString(0)) != roleReader.GetInt32(1))
                    throw new InvalidDataException("Persisted role mapping does not match the original world.");
            return state;
        }
    }

    public RoleBinding? ResolveRole(string worldId, string accountId)
    {
        RequireId(worldId);
        RequireId(accountId);
        lock (gate)
        {
            using var command = Command("SELECT player_id,legacy_index FROM roles WHERE world_id=$world AND account_id=$account", null,
                ("$world", worldId), ("$account", accountId));
            using var reader = command.ExecuteReader();
            return reader.Read() ? new RoleBinding
            {
                WorldId = worldId, AccountId = accountId, PlayerId = reader.GetString(0), LegacyPlayerIndex = reader.GetInt32(1)
            } : null;
        }
    }

    public CommandReceipt? FindReceipt(string worldId, string playerId, string requestId)
    {
        RequireId(worldId);
        RequireId(playerId);
        RequireId(requestId);
        lock (gate) return ReadReceipt(worldId, playerId, requestId, null);
    }

    // Only the administrator's startup/import path calls this; existing worlds cannot be overwritten.
    public void ImportWorld(WorldState state, IEnumerable<RoleBinding>? bindings = null)
    {
        ValidateWorld(state);
        if (state.Revision != 0) throw new InvalidDataException("Imported world must start at revision zero.");
        lock (gate)
        {
            using var transaction = connection.BeginTransaction();
            using var command = Command("INSERT INTO worlds(world_id,revision,data_json,mappings_json) VALUES($world,0,$data,$maps)", transaction,
                ("$world", state.WorldId), ("$data", state.Data.ToString(Formatting.None)),
                ("$maps", state.EntityMappings.ToString(Formatting.None)));
            command.ExecuteNonQuery();
            foreach (var binding in bindings ?? Array.Empty<RoleBinding>()) InsertBinding(state, binding, transaction);
            transaction.Commit();
        }
    }

    public CommitResult Commit(WorldCommit commit)
    {
        ArgumentNullException.ThrowIfNull(commit);
        ArgumentNullException.ThrowIfNull(commit.Receipt);
        ArgumentNullException.ThrowIfNull(commit.Actor);
        ValidateWorld(commit.Candidate);
        var receipt = commit.Receipt;
        RequireId(receipt.PlayerId);
        RequireId(receipt.RequestId);
        ArgumentException.ThrowIfNullOrWhiteSpace(receipt.Fingerprint);
        var result = JsonConvert.DeserializeObject<GameResult>(receipt.ResultJson)
            ?? throw new InvalidDataException("Missing command result.");
        if (receipt.WorldId != commit.Candidate.WorldId) return Failure(GameCodes.Conflict);
        if (result.WorldId != receipt.WorldId || result.RequestId != receipt.RequestId ||
            result.WorldRevision != commit.Candidate.Revision)
            return Failure(GameCodes.Conflict);
        var actor = commit.Actor;
        if (actor.WorldId != receipt.WorldId || actor.PlayerId != receipt.PlayerId ||
            (actor.IsSystem && (actor.AccountId != "server" || actor.PlayerId != "server" || commit.Binding != null)) ||
            (!actor.IsSystem && actor.PlayerId == "server"))
            return Failure(GameCodes.Forbidden);
        string data = commit.Candidate.Data.ToString(Formatting.None);
        string maps = commit.Candidate.EntityMappings.ToString(Formatting.None);
        lock (gate)
        {
            try
            {
                using var transaction = connection.BeginTransaction();
                if (!actor.IsSystem)
                {
                    using var owner = Command("SELECT player_id,legacy_index FROM roles WHERE world_id=$world AND account_id=$account", transaction,
                        ("$world", receipt.WorldId), ("$account", actor.AccountId));
                    using var ownerReader = owner.ExecuteReader();
                    bool hasRole = ownerReader.Read();
                    if (hasRole && (ownerReader.GetString(0) != actor.PlayerId ||
                        commit.Candidate.ResolvePlayerIndex(actor.PlayerId) != ownerReader.GetInt32(1)))
                        return Failure(GameCodes.Forbidden);
                    if (!hasRole && commit.Binding == null) return Failure(GameCodes.Forbidden);
                    if (commit.Binding != null && (commit.Binding.AccountId != actor.AccountId ||
                        commit.Binding.PlayerId != actor.PlayerId || commit.Binding.WorldId != actor.WorldId))
                        return Failure(GameCodes.Forbidden);
                }
                var oldReceipt = ReadReceipt(receipt.WorldId, receipt.PlayerId, receipt.RequestId, transaction);
                if (oldReceipt != null)
                    return oldReceipt.Fingerprint == receipt.Fingerprint
                        ? new CommitResult { Code = GameCodes.Ok, Receipt = oldReceipt, Replayed = true }
                        : Failure(GameCodes.RequestConflict);

                using var current = Command("SELECT revision,data_json,mappings_json FROM worlds WHERE world_id=$world", transaction,
                    ("$world", receipt.WorldId));
                long revision;
                string oldData, oldMaps;
                using (var reader = current.ExecuteReader())
                {
                    if (!reader.Read()) return Failure(GameCodes.Conflict);
                    revision = reader.GetInt64(0);
                    oldData = reader.GetString(1);
                    oldMaps = reader.GetString(2);
                }
                if (revision != commit.ExpectedRevision ||
                    commit.Candidate.Revision != checked(revision + (result.Code == GameCodes.Ok ? 1 : 0)))
                    return Failure(GameCodes.Conflict);
                if (commit.Candidate.Revision == revision &&
                    (commit.Binding != null || !JToken.DeepEquals(JObject.Parse(oldData), commit.Candidate.Data) ||
                     !JToken.DeepEquals(JObject.Parse(oldMaps), commit.Candidate.EntityMappings)))
                    return Failure(GameCodes.Conflict);

                var previousPlayers = (JObject)JObject.Parse(oldMaps)["players"]!;
                var nextPlayers = (JObject)commit.Candidate.EntityMappings["players"]!;
                foreach (var mapping in previousPlayers.Properties())
                    if (!JToken.DeepEquals(mapping.Value, nextPlayers[mapping.Name])) return Failure(GameCodes.Conflict);

                if (commit.Binding != null)
                {
                    if (previousPlayers.Property(commit.Binding.PlayerId) != null ||
                        commit.Binding.LegacyPlayerIndex < ((JArray)JObject.Parse(oldData)["玩家列表"]!).Count)
                        return Failure(GameCodes.Conflict);
                    InsertBinding(commit.Candidate, commit.Binding, transaction);
                }

                using var update = Command("UPDATE worlds SET revision=$next,data_json=$data,mappings_json=$maps WHERE world_id=$world AND revision=$expected",
                    transaction, ("$next", commit.Candidate.Revision), ("$data", data), ("$maps", maps),
                    ("$world", receipt.WorldId), ("$expected", revision));
                if (update.ExecuteNonQuery() != 1) return Failure(GameCodes.Conflict);
                using var insert = Command("INSERT INTO receipts(world_id,player_id,request_id,fingerprint,result_json) VALUES($world,$player,$request,$fingerprint,$result)",
                    transaction, ("$world", receipt.WorldId), ("$player", receipt.PlayerId), ("$request", receipt.RequestId),
                    ("$fingerprint", receipt.Fingerprint), ("$result", receipt.ResultJson));
                insert.ExecuteNonQuery();
                commitProbe?.Invoke("before-commit");
                transaction.Commit();
                commitProbe?.Invoke("after-commit");
                return new CommitResult { Code = GameCodes.Ok, Receipt = receipt, Replayed = false };
            }
            catch (SqliteException exception) when (exception.SqliteErrorCode == 19)
            {
                return Failure(GameCodes.Conflict);
            }
            catch (SqliteException)
            {
                return Failure(GameCodes.Unavailable);
            }
            catch (IOException)
            {
                return Failure(GameCodes.Unavailable);
            }
        }
    }

    public void Dispose()
    {
        lock (gate) connection.Dispose();
    }

    private void Initialize()
    {
        int applicationId = Convert.ToInt32(Scalar("PRAGMA application_id"));
        int version = Convert.ToInt32(Scalar("PRAGMA user_version"));
        if (version == 0 && applicationId == 0 && Convert.ToInt32(Scalar(
            "SELECT COUNT(*) FROM sqlite_master WHERE name NOT LIKE 'sqlite_%'")) == 0)
        {
            using var transaction = connection.BeginTransaction();
            using var create = Command("""
                CREATE TABLE worlds(
                    world_id TEXT PRIMARY KEY NOT NULL,
                    revision INTEGER NOT NULL CHECK(revision>=0),
                    data_json TEXT NOT NULL,
                    mappings_json TEXT NOT NULL);
                CREATE TABLE roles(
                    world_id TEXT NOT NULL REFERENCES worlds(world_id),
                    account_id TEXT NOT NULL,
                    player_id TEXT NOT NULL,
                    legacy_index INTEGER NOT NULL CHECK(legacy_index>=0),
                    PRIMARY KEY(world_id,account_id),
                    UNIQUE(world_id,player_id),
                    UNIQUE(world_id,legacy_index));
                CREATE TABLE receipts(
                    world_id TEXT NOT NULL,
                    player_id TEXT NOT NULL,
                    request_id TEXT NOT NULL,
                    fingerprint TEXT NOT NULL,
                    result_json TEXT NOT NULL,
                    PRIMARY KEY(world_id,player_id,request_id),
                    FOREIGN KEY(world_id) REFERENCES worlds(world_id));
                """, transaction);
            create.ExecuteNonQuery();
            using var metadata = Command($"PRAGMA application_id={ApplicationId}; PRAGMA user_version={SchemaVersion}", transaction);
            metadata.ExecuteNonQuery();
            transaction.Commit();
        }
        else if (applicationId != ApplicationId || version != SchemaVersion)
            throw new InvalidDataException("Unknown game database or unsupported schema version.");

        if (!Equals(Scalar("PRAGMA integrity_check"), "ok")) throw new InvalidDataException("Game database integrity check failed.");
        using (var foreignKeys = Command("PRAGMA foreign_key_check", null))
        using (var reader = foreignKeys.ExecuteReader())
            if (reader.Read()) throw new InvalidDataException("Game database contains broken role/receipt references.");
        // Each acknowledged transaction is synchronized, including writes to the WAL.
        using var settings = Command("PRAGMA foreign_keys=ON; PRAGMA journal_mode=WAL; PRAGMA synchronous=FULL", null);
        settings.ExecuteNonQuery();
        if (!Equals(Scalar("PRAGMA journal_mode"), "wal") || Convert.ToInt32(Scalar("PRAGMA synchronous")) != 2)
            throw new InvalidDataException("Required SQLite durability settings are unavailable.");
    }

    private void InsertBinding(WorldState state, RoleBinding binding, SqliteTransaction transaction)
    {
        RequireId(binding.AccountId);
        RequireId(binding.PlayerId);
        var players = (JArray)state.Data["玩家列表"]!;
        if (binding.WorldId != state.WorldId || binding.LegacyPlayerIndex < 0 || binding.LegacyPlayerIndex >= players.Count ||
            players[binding.LegacyPlayerIndex]?["基础信息"] is not JObject ||
            state.ResolvePlayerIndex(binding.PlayerId) != binding.LegacyPlayerIndex)
            throw new InvalidDataException("Role does not refer to a player in this world.");
        using var insert = Command("INSERT INTO roles(world_id,account_id,player_id,legacy_index) VALUES($world,$account,$player,$index)", transaction,
            ("$world", binding.WorldId), ("$account", binding.AccountId), ("$player", binding.PlayerId), ("$index", binding.LegacyPlayerIndex));
        insert.ExecuteNonQuery();
    }

    private CommandReceipt? ReadReceipt(string worldId, string playerId, string requestId, SqliteTransaction? transaction)
    {
        using var command = Command("SELECT fingerprint,result_json FROM receipts WHERE world_id=$world AND player_id=$player AND request_id=$request", transaction,
            ("$world", worldId), ("$player", playerId), ("$request", requestId));
        using var reader = command.ExecuteReader();
        if (!reader.Read()) return null;
        string result = reader.GetString(1);
        JObject.Parse(result);
        return new CommandReceipt
        {
            WorldId = worldId, PlayerId = playerId, RequestId = requestId, Fingerprint = reader.GetString(0), ResultJson = result
        };
    }

    private static void ValidateWorld(WorldState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        RequireId(state.WorldId);
        if (state.Revision < 0 || state.Data == null || state.EntityMappings == null ||
            state.Data["国家列表"] is not JArray || state.Data["城池列表"] is not JArray ||
            state.Data["玩家列表"] is not JArray players || players.Count == 0)
            throw new InvalidDataException("Missing original world data.");
        if (state.EntityMappings["players"] is not JObject playerMappings)
            throw new InvalidDataException("Missing stable player mappings.");
        var indexes = new HashSet<int>();
        foreach (var mapping in playerMappings.Properties())
        {
            RequireId(mapping.Name);
            if (!indexes.Add(state.ResolvePlayerIndex(mapping.Name)))
                throw new InvalidDataException("Duplicate player mapping.");
        }
        if (indexes.Count != players.Count) throw new InvalidDataException("Incomplete stable player mappings.");
        if (state.Data.Property("存档版本") != null && state.Data["存档版本"]!.ToObject<int>() != 1)
            throw new InvalidDataException("Unsupported original save version.");
        long timestamp = state.Data.Property("存档时间") == null ? 0 : state.Data["存档时间"]!.ToObject<long>();
        try { new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(timestamp).ToLocalTime(); }
        catch (ArgumentOutOfRangeException exception) { throw new InvalidDataException("Invalid original save timestamp.", exception); }
    }

    private static void RequireId(string value) => ArgumentException.ThrowIfNullOrWhiteSpace(value);
    private static CommitResult Failure(string code) => new() { Code = code };
    private object? Scalar(string sql)
    {
        using var command = Command(sql, null);
        return command.ExecuteScalar();
    }

    private SqliteCommand Command(string sql, SqliteTransaction? transaction, params (string Name, object Value)[] parameters)
    {
        var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Transaction = transaction;
        foreach (var parameter in parameters) command.Parameters.AddWithValue(parameter.Name, parameter.Value);
        return command;
    }
}
