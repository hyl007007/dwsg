using System.Diagnostics;
using System.Reflection;
using Dwsg.Persistence;
using Dwsg.Shared;
using Microsoft.Data.Sqlite;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// The input is an administrator's actual Unity export. No miniature world or module is substituted.
if (args.Length > 0 && args[0] == "--crash")
{
    using var crashing = new SqliteWorldStore(args[2], stage =>
    {
        if (stage == args[1]) Process.GetCurrentProcess().Kill();
    });
    crashing.Commit(Change(crashing.Load(args[3])!, "crash"));
    throw new Exception("Crash probe was not reached.");
}
if (args.Length != 2) throw new ArgumentException("Expected actual world-seed.json and an ignored audit output directory.");
var seed = JObject.Parse(File.ReadAllText(args[0]));
var output = Path.Combine(Path.GetFullPath(args[1]), Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(output);
int assertions = 0;
Console.WriteLine($"Real export: {((JArray)seed["城池列表"]!).Count} cities, {((JArray)seed["国家列表"]!).Count} nations, " +
    $"{((JArray)seed["玩家列表"]!).Count} players, {((JArray)seed["商城商品"]!).Count} shop items.");

var world = Original("durable");
string database = Database("durable");
string playerId = ((JObject)world.EntityMappings["players"]!).Properties().First().Name;
var binding = new RoleBinding { WorldId = world.WorldId, AccountId = "check-account", PlayerId = playerId, LegacyPlayerIndex = 0 };
var actor = new AuthenticatedActor(binding.AccountId, playerId, world.WorldId, "check-connection");
var successful = Change(world, "first");
using (var store = new SqliteWorldStore(database))
{
    store.ImportWorld(world, new[] { binding });
    Check(JToken.DeepEquals(store.Load(world.WorldId)!.Data, seed), "complete original world imported without rewriting fields");
    Check(store.Load("missing") == null, "missing world is not generated");
    Check(store.ListRoles(world.WorldId).Count == 1 && store.ListRoles(world.WorldId)[0].AccountId == binding.AccountId &&
        store.ListRoles("missing").Count == 0, "human roles come only from persisted bindings, not original NPC indexes");
    bool duplicateImport = false;
    try { store.ImportWorld(world); } catch (SqliteException) { duplicateImport = true; }
    Check(duplicateImport, "existing import cannot overwrite the world");
    Check(store.Commit(successful).Code == GameCodes.Ok, "system world commit succeeds without a human role");
    var replay = store.Commit(successful);
    Check(replay.Replayed && replay.Receipt.ResultJson == successful.Receipt.ResultJson, "identical request returns saved result");
    var collision = Change(world, "first");
    collision.Receipt.Fingerprint = new string('f', 64);
    Check(store.Commit(collision).Code == GameCodes.RequestConflict, "different request fingerprint conflicts");

    var spoof = Change(store.Load(world.WorldId)!, "spoof");
    spoof.Actor = new AuthenticatedActor("server", "server", world.WorldId, "network");
    Check(store.Commit(spoof).Code == GameCodes.Forbidden, "ordinary actor cannot occupy system receipt namespace");
    var wrongOwner = Change(store.Load(world.WorldId)!, "wrong-owner", actor);
    wrongOwner.Actor = new AuthenticatedActor("another-account", playerId, world.WorldId, "other");
    Check(store.Commit(wrongOwner).Code == GameCodes.Forbidden, "account ownership checked inside transaction");
    Check(store.FindReceipt(world.WorldId, playerId, "wrong-owner") == null, "unauthorized request leaves no receipt");

    var mappingChange = Change(store.Load(world.WorldId)!, "mapping-change");
    var properties = ((JObject)mappingChange.Candidate.EntityMappings["players"]!).Properties().Take(2).ToArray();
    (properties[0].Value, properties[1].Value) = (properties[1].Value.DeepClone(), properties[0].Value.DeepClone());
    Check(store.Commit(mappingChange).Code == GameCodes.Conflict, "existing stable player mappings cannot be reassigned");

    var rejected = Change(store.Load(world.WorldId)!, "rejected", actor);
    rejected.Candidate = store.Load(world.WorldId)!;
    var error = GameResult.Reject(GameCodes.InsufficientFunds, "original rule rejected");
    error.RequestId = rejected.Receipt.RequestId; error.WorldId = world.WorldId; error.WorldRevision = rejected.Candidate.Revision;
    rejected.Receipt.ResultJson = JsonConvert.SerializeObject(error);
    Check(store.Commit(rejected).Code == GameCodes.Ok, "rejected command receipt saves with unchanged world revision");
    Check(store.Load(world.WorldId)!.Revision == 1, "rejected command leaves world unchanged");
}
using (var restarted = new SqliteWorldStore(database))
{
    Check(JToken.DeepEquals(restarted.Load(world.WorldId)!.Data, successful.Candidate.Data), "whole world recovered after reopen");
    var recoveredRole = restarted.ResolveRole(world.WorldId, binding.AccountId)!;
    Check(recoveredRole.PlayerId == playerId && recoveredRole.LegacyPlayerIndex == 0, "account role binding survives restart");
    Check(restarted.ListRoles(world.WorldId).Single().PlayerId == playerId, "human role list survives restart without adding system actors");
    Check(restarted.FindReceipt(world.WorldId, "server", "first")!.ResultJson == successful.Receipt.ResultJson, "system receipt survives restart");
    using var competitor = new SqliteWorldStore(database);
    var current = restarted.Load(world.WorldId)!;
    Check(restarted.Commit(Change(current, "winner")).Code == GameCodes.Ok, "first concurrent revision commits");
    Check(competitor.Commit(Change(current, "stale")).Code == GameCodes.Conflict, "second stale revision is rejected");
    Check(competitor.FindReceipt(world.WorldId, "server", "stale") == null, "stale candidate is entirely rolled back");
}

foreach (string stage in new[] { "before-commit", "after-commit" })
{
    var state = Original(stage);
    string path = Database(stage);
    using (var setup = new SqliteWorldStore(path)) setup.ImportWorld(state);
    var commit = Change(state, "io-failure");
    using (var failed = new SqliteWorldStore(path, reached => { if (reached == stage) throw new IOException("injected disk/ack failure"); }))
        Check(failed.Commit(commit).Code == GameCodes.Unavailable, stage + " interruption is reported");
    using (var recovered = new SqliteWorldStore(path))
    {
        bool durable = stage == "after-commit";
        Check(recovered.Load(state.WorldId)!.Revision == (durable ? 1 : 0), stage + " world recovery");
        Check((recovered.FindReceipt(state.WorldId, "server", "io-failure") != null) == durable, stage + " receipt recovery");
        var retry = recovered.Commit(commit);
        Check(retry.Code == GameCodes.Ok && retry.Replayed == durable && recovered.Load(state.WorldId)!.Revision == 1,
            stage + " retry commits at most once");
    }

    var crashState = Original("crash-" + stage);
    string crashDatabase = Database("crash-" + stage);
    using (var setup = new SqliteWorldStore(crashDatabase)) setup.ImportWorld(crashState);
    var start = new ProcessStartInfo(Environment.ProcessPath!) { UseShellExecute = false, CreateNoWindow = true,
        RedirectStandardOutput = true, RedirectStandardError = true };
    if (string.Equals(Path.GetFileNameWithoutExtension(Environment.ProcessPath), "dotnet", StringComparison.OrdinalIgnoreCase))
        start.ArgumentList.Add(Assembly.GetExecutingAssembly().Location);
    foreach (var argument in new[] { "--crash", stage, crashDatabase, crashState.WorldId }) start.ArgumentList.Add(argument);
    using var process = Process.Start(start)!;
    if (!process.WaitForExit(20000)) { process.Kill(); throw new Exception("Owned crash child did not finish."); }
    Check(process.ExitCode != 0, stage + " real subprocess terminated without disposing store");
    using var afterCrash = new SqliteWorldStore(crashDatabase);
    bool saved = stage == "after-commit";
    Check(afterCrash.Load(crashState.WorldId)!.Revision == (saved ? 1 : 0), stage + " process-crash world recovery");
    Check((afterCrash.FindReceipt(crashState.WorldId, "server", "crash") != null) == saved, stage + " process-crash receipt recovery");
    var crashRetry = afterCrash.Commit(Change(crashState, "crash"));
    Check(crashRetry.Code == GameCodes.Ok && crashRetry.Replayed == saved, stage + " process-crash retry");
}

string corrupt = Database("corrupt");
File.WriteAllBytes(corrupt, new byte[200]);
bool corruptRejected = false;
try { using var invalid = new SqliteWorldStore(corrupt); } catch (SqliteException) { corruptRejected = true; }
Check(corruptRejected, "corrupt file is rejected without generating a world");
string invalidSchema = Database("schema");
using (var raw = new SqliteConnection("Data Source=" + invalidSchema))
{
    raw.Open(); using var change = raw.CreateCommand(); change.CommandText = "PRAGMA user_version=999"; change.ExecuteNonQuery();
}
bool schemaRejected = false;
try { using var invalid = new SqliteWorldStore(invalidSchema); } catch (InvalidDataException) { schemaRejected = true; }
Check(schemaRejected, "unknown schema fails closed");

var backupWorld = Original("online-backup");
string liveDatabase = Database("backup-source"), backupDatabase = Database("backup-restored");
var backupRole = new RoleBinding { WorldId = backupWorld.WorldId, AccountId = "backup-account", LegacyPlayerIndex = 0,
    PlayerId = ((JObject)backupWorld.EntityMappings["players"]!).Properties().First().Name };
using (var live = new SqliteWorldStore(liveDatabase))
{
    live.ImportWorld(backupWorld, new[] { backupRole });
    // The real connection keeps the latest committed pages in WAL throughout backup and restore.
    var native = (SqliteConnection)typeof(SqliteWorldStore).GetField("connection", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(live)!;
    using (var checkpoint = native.CreateCommand())
    {
        checkpoint.CommandText = "PRAGMA wal_checkpoint(TRUNCATE); PRAGMA wal_autocheckpoint=0";
        checkpoint.ExecuteNonQuery();
    }
    var committed = Change(backupWorld, "wal-receipt");
    Check(live.Commit(committed).Code == GameCodes.Ok && new FileInfo(liveDatabase + "-wal").Length > 0, "online backup source contains committed WAL pages");
    live.BackupTo(backupDatabase);
    Check(native.State == System.Data.ConnectionState.Open, "hot backup keeps live source connection open");
    using (var restored = new SqliteWorldStore(backupDatabase))
    {
        var recovered = restored.Load(backupWorld.WorldId)!;
        Check(recovered.Revision == 1 && JToken.DeepEquals(recovered.Data, committed.Candidate.Data), "online backup restores whole original world and revision from WAL");
        Check(JToken.DeepEquals(recovered.EntityMappings, backupWorld.EntityMappings), "online backup restores every stable player mapping");
        Check(restored.ResolveRole(backupWorld.WorldId, backupRole.AccountId)!.PlayerId == backupRole.PlayerId, "online backup restores account role binding");
        Check(restored.ListRoles(backupWorld.WorldId).Single().AccountId == backupRole.AccountId, "online backup restores complete human role registration");
        Check(restored.FindReceipt(backupWorld.WorldId, "server", "wal-receipt")!.ResultJson == committed.Receipt.ResultJson,
            "online backup restores exact command receipt");
        Check(restored.Commit(committed).Replayed && restored.Load(backupWorld.WorldId)!.Revision == 1, "restored receipt prevents duplicate commit");
    }
    Check(live.Commit(Change(live.Load(backupWorld.WorldId)!, "after-backup")).Code == GameCodes.Ok, "live world continues after hot backup");
    using (var independent = new SqliteWorldStore(backupDatabase))
        Check(independent.Load(backupWorld.WorldId)!.Revision == 1 && independent.FindReceipt(backupWorld.WorldId, "server", "after-backup") == null,
            "later source commits do not change backup snapshot");
    bool overwriteRejected = false, sourceOverwriteRejected = false;
    try { live.BackupTo(backupDatabase); } catch (IOException) { overwriteRejected = true; }
    try { live.BackupTo(liveDatabase); } catch (IOException) { sourceOverwriteRejected = true; }
    Check(overwriteRejected && sourceOverwriteRejected && live.Load(backupWorld.WorldId)!.Revision == 2, "backup refuses to overwrite existing destination or live database");
}

Console.WriteLine($"PASS {assertions} persistence checks. Audit directory: {output}");

string Database(string name) => Path.Combine(output, name + ".sqlite");
WorldState Original(string id)
{
    var mappings = new JObject();
    for (int i = 0; i < ((JArray)seed["玩家列表"]!).Count; i++) mappings[Guid.NewGuid().ToString("N")] = i;
    return new WorldState { WorldId = id, Data = (JObject)seed.DeepClone(), EntityMappings = new JObject { ["players"] = mappings } };
}
void Check(bool condition, string description)
{
    if (!condition) throw new Exception("FAIL: " + description);
    assertions++; Console.WriteLine("PASS " + description);
}
static WorldCommit Change(WorldState original, string requestId, AuthenticatedActor? actor = null)
{
    var candidate = original.Clone(); candidate.Revision++;
    candidate.Data["存档时间"] = (candidate.Data["存档时间"]?.Value<long>() ?? 0) + 1;
    var command = new GameCommand { WorldId = original.WorldId, RequestId = requestId, Type = "persistence.check",
        Payload = new JObject { ["timestamp"] = candidate.Data["存档时间"]!.DeepClone() } };
    var result = GameResult.Success(); result.WorldId = original.WorldId; result.RequestId = requestId; result.WorldRevision = candidate.Revision;
    return new WorldCommit { Actor = actor ?? AuthenticatedActor.System(original.WorldId), ExpectedRevision = original.Revision,
        Candidate = candidate, Receipt = new CommandReceipt { WorldId = original.WorldId, PlayerId = actor?.PlayerId ?? "server",
            RequestId = requestId, Fingerprint = CommandFingerprint.Calculate(command), ResultJson = JsonConvert.SerializeObject(result) } };
}
