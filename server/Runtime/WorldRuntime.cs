using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using Newtonsoft.Json;
using Dwsg.Shared;

namespace Dwsg.Runtime
{
    public sealed class WorldRuntime
    {
        private readonly IWorldStore store;
        private readonly Func<AuthenticatedActor, bool> authorize;
        private readonly Func<long> utcNow;
        private readonly Action<WorldState> initializeEntities;
        private readonly Action<WorldState, string, long> preparePlayer;
        private readonly Action<WorldState> prepareCommit;
        private readonly Action<WorldState, long> prepareTimedCommit;
        private readonly Dictionary<string, IGameModule> modules = new Dictionary<string, IGameModule>(StringComparer.Ordinal);
        private readonly List<IGameTickModule> ticks = new List<IGameTickModule>();
        // Kept private: ordinary modules receive a copy, audited read-only collectors share it.
        private readonly ConcurrentDictionary<string, WorldState> snapshots = new ConcurrentDictionary<string, WorldState>();
        // Five-player worlds serialize candidate evaluation and durable commit under one world gate.
        private readonly ConcurrentDictionary<string, object> gates = new ConcurrentDictionary<string, object>();
        public event Action<GameResult> Committed;
        public WorldRuntime(IWorldStore store, Func<AuthenticatedActor, bool> authorize,
            Func<long> utcNow = null, Action<WorldState> initializeEntities = null,
            Action<WorldState, string, long> preparePlayer = null, Action<WorldState> prepareCommit = null,
            Action<WorldState, long> prepareTimedCommit = null)
        {
            this.store = store ?? throw new ArgumentNullException(nameof(store));
            this.authorize = authorize ?? throw new ArgumentNullException(nameof(authorize));
            this.utcNow = utcNow ?? (() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            this.initializeEntities = initializeEntities;
            this.preparePlayer = preparePlayer;
            this.prepareCommit = prepareCommit;
            this.prepareTimedCommit = prepareTimedCommit;
        }
        public void Register(IGameModule module)
        {
            foreach (var type in module.CommandTypes) modules.Add(type, module);
            if (module is IGameTickModule tick) ticks.Add(tick);
        }
        public GameResult Execute(AuthenticatedActor actor, GameCommand command)
        {
            if (command == null || string.IsNullOrWhiteSpace(command.WorldId) || command.WorldId.Length > 128 ||
                string.IsNullOrWhiteSpace(command.RequestId) || command.RequestId.Length > 128 ||
                string.IsNullOrWhiteSpace(command.Type) || command.Payload == null)
                return GameResult.Reject(GameCodes.InvalidArgument, "命令参数无效。");
            if (command.ProtocolVersion != 1) return GameResult.Reject(GameCodes.ProtocolMismatch, "协议版本不兼容。");
            lock (gates.GetOrAdd(command.WorldId, _ => new object()))
            {
                if (actor == null || actor.WorldId != command.WorldId || string.IsNullOrEmpty(actor.PlayerId) || !authorize(actor))
                    return GameResult.Reject(GameCodes.Unauthenticated, "连接已失效，请重新连接。");
                var fingerprint = CommandFingerprint.Calculate(command);
                var receipt = store.FindReceipt(command.WorldId, actor.PlayerId, command.RequestId);
                if (receipt != null) return Replay(receipt, fingerprint);
                var original = ReadState(command.WorldId);
                if (original == null) return GameResult.Reject(GameCodes.NotFound, "世界不存在。");
                var expectedRevision = original.Revision;
                if (!modules.TryGetValue(command.Type, out var module))
                    return GameResult.Reject(GameCodes.InvalidArgument, "未知命令。");
                var candidate = module is ICopyingGameModule
                    ? new WorldState { WorldId = original.WorldId, Revision = original.Revision,
                        Data = original.Data, EntityMappings = original.EntityMappings }
                    : original.Clone();
                GameResult result;
                try
                {
                    long now = utcNow();
                    result = module.Execute(candidate, new CommandContext(actor, now), command);
                    if (result != null && result.Code == GameCodes.Ok)
                    {
                        prepareCommit?.Invoke(candidate);
                        prepareTimedCommit?.Invoke(candidate, now);
                    }
                }
                catch (Exception) { return GameResult.Reject(GameCodes.Unavailable, "操作未提交，请稍后重试。"); }
                if (result == null) return GameResult.Reject(GameCodes.Unavailable, "模块未提供结果。");
                if (result.Code != GameCodes.Ok) { candidate = original; result.Events.Clear(); }
                else candidate.Revision = checked(expectedRevision + 1);
                result.RequestId = command.RequestId; result.WorldId = command.WorldId; result.WorldRevision = candidate.Revision;
                receipt = new CommandReceipt { WorldId = command.WorldId, PlayerId = actor.PlayerId, RequestId = command.RequestId,
                    Fingerprint = fingerprint, ResultJson = JsonConvert.SerializeObject(result) };
                CommitResult committed;
                WorldState savedSnapshot;
                try
                {
                    // Own a private copy before committing: results/modules may retain candidate references.
                    savedSnapshot = result.Code == GameCodes.Ok && store is IWorldRevisionStore ? candidate.Clone() : null;
                    committed = store.Commit(new WorldCommit { Actor = actor, ExpectedRevision = expectedRevision, Candidate = candidate, Receipt = receipt });
                }
                catch (Exception) { return GameResult.Reject(GameCodes.Unavailable, "保存失败，操作未确认。"); }
                if (committed.Code != GameCodes.Ok) return GameResult.Reject(committed.Code, "操作未提交。");
                if (committed.Replayed) return Replay(committed.Receipt, fingerprint);
                if (savedSnapshot != null) snapshots[command.WorldId] = savedSnapshot;
                // Publication failures cannot turn an already durable command into an apparent rollback.
                var subscribers = Committed;
                if (subscribers != null)
                    foreach (Action<GameResult> subscriber in subscribers.GetInvocationList())
                        try { subscriber(result); } catch (Exception) { }
                return result;
            }
        }
        private static GameResult Replay(CommandReceipt receipt, string fingerprint)
        {
            return receipt.Fingerprint == fingerprint ? JsonConvert.DeserializeObject<GameResult>(receipt.ResultJson)
                : GameResult.Reject(GameCodes.RequestConflict, "请求ID已用于不同参数。");
        }
        // Called only after Host verifies the PHP account. The client never selects a player ID.
        public GameResult EnsureRole(string accountId, string worldId, string connectionId, string requestId,
            string nickname, string nation, CreatePlayer create, out RoleBinding binding)
        {
            binding = null;
            lock (gates.GetOrAdd(worldId, _ => new object()))
            {
                binding = store.ResolveRole(worldId, accountId);
                if (binding != null) return GameResult.Success();
                if (string.IsNullOrWhiteSpace(nickname) || string.IsNullOrWhiteSpace(nation))
                    return GameResult.Reject(GameCodes.RoleRequired, "请选择国家并创建角色。");
                if (string.IsNullOrWhiteSpace(requestId) || requestId.Length > 128)
                    return GameResult.Reject(GameCodes.InvalidArgument, "请求ID无效。");
                var original = ReadState(worldId);
                if (original == null) return GameResult.Reject(GameCodes.NotFound, "世界不存在。");
                var candidate = original.Clone();
                var createdUtcMs = utcNow();
                var result = create(candidate, nickname, nation, createdUtcMs, out var index);
                if (result.Code != GameCodes.Ok) return result;
                var playerId = Guid.NewGuid().ToString("N");
                var players = candidate.EntityMappings["players"] as Newtonsoft.Json.Linq.JObject;
                if (players == null) throw new InvalidOperationException("Player mapping missing");
                players[playerId] = index;
                var humans = candidate.EntityMappings["humanPlayers"] as Newtonsoft.Json.Linq.JObject;
                if (humans == null) candidate.EntityMappings["humanPlayers"] = humans = new Newtonsoft.Json.Linq.JObject();
                humans[playerId] = true;
                initializeEntities?.Invoke(candidate);
                preparePlayer?.Invoke(candidate, playerId, createdUtcMs);
                try { prepareCommit?.Invoke(candidate); prepareTimedCommit?.Invoke(candidate, createdUtcMs); }
                catch (Exception) { return GameResult.Reject(GameCodes.Unavailable, "角色状态未提交，请稍后重试。"); }
                binding = new RoleBinding { AccountId = accountId, WorldId = worldId, PlayerId = playerId, LegacyPlayerIndex = index };
                candidate.Revision = checked(original.Revision + 1);
                result.RequestId = requestId; result.WorldId = worldId; result.WorldRevision = candidate.Revision;
                result.Data["playerId"] = playerId;
                var command = new GameCommand { RequestId = requestId, WorldId = worldId, Type = "role.create",
                    Payload = new Newtonsoft.Json.Linq.JObject { ["nickname"] = nickname, ["nation"] = nation } };
                var receipt = new CommandReceipt { WorldId = worldId, PlayerId = playerId, RequestId = requestId,
                    Fingerprint = CommandFingerprint.Calculate(command), ResultJson = JsonConvert.SerializeObject(result) };
                var actor = new AuthenticatedActor(accountId, playerId, worldId, connectionId);
                CommitResult committed;
                WorldState savedSnapshot;
                try
                {
                    savedSnapshot = store is IWorldRevisionStore ? candidate.Clone() : null;
                    committed = store.Commit(new WorldCommit { Actor = actor, Candidate = candidate,
                        ExpectedRevision = original.Revision, Receipt = receipt, Binding = binding });
                }
                catch (Exception)
                {
                    binding = store.ResolveRole(worldId, accountId);
                    return binding != null ? GameResult.Success() : GameResult.Reject(GameCodes.Unavailable, "角色保存未确认，请重试。");
                }
                if (committed.Code == GameCodes.Ok)
                {
                    if (!committed.Replayed && savedSnapshot != null) snapshots[worldId] = savedSnapshot;
                    return result;
                }
                binding = store.ResolveRole(worldId, accountId);
                return binding != null ? GameResult.Success() : GameResult.Reject(committed.Code, "角色创建未提交。");
            }
        }
        public WorldSnapshot Preview(string worldId, IWorldProjection projection)
        {
            var state = ReadSnapshotState(worldId) ?? throw new InvalidOperationException("World missing");
            return projection.Build(projection is IReadOnlyWorldProjection ? state : state.Clone(), null, utcNow());
        }
        public void Tick(string worldId)
        {
            var started = Stopwatch.GetTimestamp();
            var state = ReadSnapshotState(worldId);
            if (state == null) return;
            var loaded = Stopwatch.GetTimestamp();
            var commands = new List<GameCommand>();
            // Collect from one committed snapshot without holding the writer gate.
            // Unmarked collectors still receive a copy so they cannot mutate the cache.
            foreach (var tick in ticks)
                foreach (var command in tick.CollectDueCommands(tick is IReadOnlyGameTickModule ? state : state.Clone(), utcNow()))
                    commands.Add(command);
            // A batch of due work must not hold the world gate across all its transactions.
            // Each Execute revalidates the collected command against the latest committed state.
            foreach (var command in commands) Execute(AuthenticatedActor.System(worldId), command);
            if (Environment.GetEnvironmentVariable("DWSG_LATENCY_TRACE") == "1" && Stopwatch.GetElapsedTime(started).TotalMilliseconds >= 100)
                Console.Error.WriteLine($"DWSG_LATENCY tick load_ms={Stopwatch.GetElapsedTime(started, loaded).TotalMilliseconds:F1} modules_ms={Stopwatch.GetElapsedTime(loaded).TotalMilliseconds:F1}");
        }
        public WorldSnapshot Snapshot(AuthenticatedActor actor, IWorldProjection projection)
        {
            var started = Stopwatch.GetTimestamp();
            if (!authorize(actor)) throw new UnauthorizedAccessException();
            var state = ReadSnapshotState(actor.WorldId) ?? throw new InvalidOperationException("World missing");
            var loaded = Stopwatch.GetTimestamp();
            // The private snapshot is immutable. Serialization/projection must not queue writers.
            var snapshot = projection.Build(projection is IReadOnlyWorldProjection ? state : state.Clone(), actor, utcNow());
            if (Environment.GetEnvironmentVariable("DWSG_LATENCY_TRACE") == "1")
                Console.Error.WriteLine($"DWSG_LATENCY snapshot load_ms={Stopwatch.GetElapsedTime(started, loaded).TotalMilliseconds:F1} build_ms={Stopwatch.GetElapsedTime(loaded).TotalMilliseconds:F1}");
            return snapshot;
        }
        private WorldState ReadSnapshotState(string worldId)
        {
            // Readers can use the last durable immutable snapshot while a writer evaluates its
            // private candidate. Never expose that candidate, or skip external revision checks.
            if (store is IWorldRevisionStore revisions && snapshots.TryGetValue(worldId, out var cached) &&
                revisions.ReadRevision(worldId) == cached.Revision) return cached;
            lock (gates.GetOrAdd(worldId, _ => new object())) return ReadState(worldId);
        }
        // Call only under the world's gate. Revision is checked even for external store commits.
        private WorldState ReadState(string worldId)
        {
            if (!(store is IWorldRevisionStore revisions)) return store.Load(worldId);
            var revision = revisions.ReadRevision(worldId);
            if (revision == null) { snapshots.TryRemove(worldId, out _); return null; }
            if (snapshots.TryGetValue(worldId, out var state) && state.Revision == revision) return state;
            state = store.Load(worldId);
            if (state != null) snapshots[worldId] = state;
            else snapshots.TryRemove(worldId, out _);
            return state;
        }
    }
}
