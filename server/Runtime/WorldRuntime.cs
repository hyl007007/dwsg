using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Newtonsoft.Json;
using Dwsg.Shared;

namespace Dwsg.Runtime
{
    public sealed class WorldRuntime
    {
        private readonly IWorldStore store;
        private readonly Func<AuthenticatedActor, bool> authorize;
        private readonly Dictionary<string, IGameModule> modules = new Dictionary<string, IGameModule>(StringComparer.Ordinal);
        private readonly List<IGameTickModule> ticks = new List<IGameTickModule>();
        // Five-player worlds serialize candidate evaluation and durable commit under one world gate.
        private readonly ConcurrentDictionary<string, object> gates = new ConcurrentDictionary<string, object>();
        public event Action<GameResult> Committed;
        public WorldRuntime(IWorldStore store, Func<AuthenticatedActor, bool> authorize)
        {
            this.store = store ?? throw new ArgumentNullException(nameof(store));
            this.authorize = authorize ?? throw new ArgumentNullException(nameof(authorize));
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
                var original = store.Load(command.WorldId);
                if (original == null) return GameResult.Reject(GameCodes.NotFound, "世界不存在。");
                if (!modules.TryGetValue(command.Type, out var module))
                    return GameResult.Reject(GameCodes.InvalidArgument, "未知命令。");
                var candidate = original.Clone();
                GameResult result;
                try { result = module.Execute(candidate, new CommandContext(actor, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()), command); }
                catch (Exception) { return GameResult.Reject(GameCodes.Unavailable, "操作未提交，请稍后重试。"); }
                if (result == null) return GameResult.Reject(GameCodes.Unavailable, "模块未提供结果。");
                if (result.Code != GameCodes.Ok) { candidate = original; result.Events.Clear(); }
                else candidate.Revision = checked(original.Revision + 1);
                result.RequestId = command.RequestId; result.WorldId = command.WorldId; result.WorldRevision = candidate.Revision;
                receipt = new CommandReceipt { WorldId = command.WorldId, PlayerId = actor.PlayerId, RequestId = command.RequestId,
                    Fingerprint = fingerprint, ResultJson = JsonConvert.SerializeObject(result) };
                CommitResult committed;
                try { committed = store.Commit(new WorldCommit { Actor = actor, ExpectedRevision = original.Revision, Candidate = candidate, Receipt = receipt }); }
                catch (Exception) { return GameResult.Reject(GameCodes.Unavailable, "保存失败，操作未确认。"); }
                if (committed.Code != GameCodes.Ok) return GameResult.Reject(committed.Code, "操作未提交。");
                if (committed.Replayed) return Replay(committed.Receipt, fingerprint);
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
        public void Tick(string worldId)
        {
            lock (gates.GetOrAdd(worldId, _ => new object()))
            {
                var state = store.Load(worldId);
                if (state == null) return;
                foreach (var tick in ticks)
                    foreach (var command in tick.CollectDueCommands(state.Clone(), DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()))
                        Execute(AuthenticatedActor.System(worldId), command);
            }
        }
        public WorldSnapshot Snapshot(AuthenticatedActor actor, IWorldProjection projection)
        {
            lock (gates.GetOrAdd(actor.WorldId, _ => new object()))
            {
                if (!authorize(actor)) throw new UnauthorizedAccessException();
                var state = store.Load(actor.WorldId) ?? throw new InvalidOperationException("World missing");
                return projection.Build(state, actor, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            }
        }
    }
}
