using System.Collections.Concurrent;
using Dwsg.Runtime;
using Dwsg.Shared;
using Newtonsoft.Json.Linq;

namespace Dwsg.Host;

public sealed class GameSession
{
    public AuthenticatedActor Actor { get; init; }
    public PhpSessionProof Proof { get; init; }
    public long ExpiresUtcMs;
    public long LastSeenUtcMs;
    public string InvalidCode;
    public readonly object StreamGate = new();
    public readonly Queue<JObject> Events = new();
    public long Sequence;
    public WorldSnapshot LastSnapshot;
}

public sealed class GameSessions
{
    private readonly object connectionGate = new();
    private readonly ConcurrentDictionary<string, GameSession> connections = new();
    private readonly ConcurrentDictionary<string, GameSession> accounts = new();
    private readonly PhpAuthentication auth;
    private readonly IWorldProjection projection;
    private readonly CreatePlayer create;
    private readonly string worldId;
    private readonly long leaseMs;
    private readonly int maxOnlinePlayers;
    public WorldRuntime Runtime { get; set; }
    public GameSessions(PhpAuthentication auth, IWorldProjection projection, CreatePlayer create, string worldId, long leaseMs = 30000, int maxOnlinePlayers = 5)
    {
        if (maxOnlinePlayers <= 0) throw new ArgumentOutOfRangeException(nameof(maxOnlinePlayers));
        this.auth = auth; this.projection = projection; this.create = create; this.worldId = worldId; this.leaseMs = leaseMs;
        this.maxOnlinePlayers = maxOnlinePlayers;
    }
    private static long Now() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    public bool Authorize(AuthenticatedActor actor)
    {
        if (actor.IsSystem) return actor.WorldId == worldId && actor.AccountId == "server" && actor.PlayerId == "server";
        return connections.TryGetValue(actor.ConnectionId, out var session) && ReferenceEquals(session.Actor, actor) && IsCurrent(session);
    }
    private bool IsCurrent(GameSession session) => session.InvalidCode == null &&
        accounts.TryGetValue(session.Actor.AccountId, out var current) && ReferenceEquals(current, session) &&
        Interlocked.Read(ref session.ExpiresUtcMs) > Now() && Now() - Interlocked.Read(ref session.LastSeenUtcMs) < leaseMs;
    private void Sweep()
    {
        foreach (var pair in accounts)
            if (!IsCurrent(pair.Value))
            {
                pair.Value.InvalidCode ??= GameCodes.Unauthenticated;
                accounts.TryRemove(pair.Key, out _);
            }
        foreach (var pair in connections)
            if (pair.Value.InvalidCode != null && Now() - pair.Value.LastSeenUtcMs > leaseMs * 2) connections.TryRemove(pair.Key, out _);
    }
    public async Task<JObject> ConnectAsync(JObject input, CancellationToken cancellation)
    {
        if (input.Value<int?>("protocolVersion") != 1) return Reply(GameResult.Reject(GameCodes.ProtocolMismatch, "协议版本不兼容。"));
        if (input.Value<string>("worldId") != worldId) return Reply(GameResult.Reject(GameCodes.NotFound, "世界不存在。"));
        var proof = input["proof"]?.ToObject<PhpSessionProof>();
        var verified = await auth.VerifyAsync(proof, true, cancellation);
        lock (connectionGate)
        {
            Sweep();
            if (accounts.Values.Count(s => s.Actor.WorldId == worldId && s.Actor.AccountId != verified.AccountId) >= maxOnlinePlayers)
                return Reply(GameResult.Reject(GameCodes.WorldFull, "当前世界已有" + maxOnlinePlayers + "名真人在线，请稍后重试。"));
            var connectionId = Guid.NewGuid().ToString("N");
            var result = Runtime.EnsureRole(verified.AccountId, worldId, connectionId, input.Value<string>("requestId"),
                input.Value<string>("nickname"), input.Value<string>("nation"), create, out var binding);
            if (result.Code == GameCodes.RoleRequired)
                return Reply(result, Runtime.Preview(worldId, projection));
            if (result.Code != GameCodes.Ok) return Reply(result);
            var session = new GameSession { Actor = new AuthenticatedActor(verified.AccountId, binding.PlayerId, worldId, connectionId),
                Proof = proof, ExpiresUtcMs = verified.ExpiresUtcMs, LastSeenUtcMs = Now() };
            if (accounts.TryGetValue(verified.AccountId, out var previous)) previous.InvalidCode = GameCodes.SessionReplaced;
            connections[connectionId] = session;
            accounts[verified.AccountId] = session;
            var response = SnapshotReply(session, result, -1, 0);
            response["connectionId"] = connectionId;
            response["sessionExpiresUtcMs"] = verified.ExpiresUtcMs;
            return response;
        }
    }
    private GameSession Require(string connectionId)
    {
        if (connectionId == null || !connections.TryGetValue(connectionId, out var session)) throw new PhpSessionRejected("连接已断开，请重新连接。");
        return session;
    }
    private async Task<GameResult> ValidateAsync(GameSession session, bool renew, CancellationToken cancellation)
    {
        if (!IsCurrent(session)) return GameResult.Reject(session.InvalidCode ?? GameCodes.Unauthenticated, "连接已失效，请重新连接。");
        try
        {
            var verified = await auth.VerifyAsync(session.Proof, renew, cancellation);
            if (verified.AccountId != session.Actor.AccountId) throw new PhpSessionRejected("账号会话已失效。");
            // A takeover during the PHP request must not reactivate the old connection.
            if (!IsCurrent(session)) return GameResult.Reject(session.InvalidCode ?? GameCodes.Unauthenticated, "连接已失效，请重新连接。");
            Interlocked.Exchange(ref session.ExpiresUtcMs, verified.ExpiresUtcMs);
            Interlocked.Exchange(ref session.LastSeenUtcMs, Now());
            return GameResult.Success();
        }
        catch (PhpSessionRejected ex)
        {
            session.InvalidCode = GameCodes.Unauthenticated;
            return GameResult.Reject(GameCodes.Unauthenticated, ex.Message);
        }
    }
    public async Task<JObject> CommandAsync(string connectionId, JObject input, CancellationToken cancellation)
    {
        var session = Require(connectionId);
        var verified = await ValidateAsync(session, true, cancellation);
        if (verified.Code != GameCodes.Ok) return Reply(verified);
        var command = input.ToObject<GameCommand>();
        var result = Runtime.Execute(session.Actor, command);
        if (!IsCurrent(session)) return Reply(GameResult.Reject(session.InvalidCode ?? GameCodes.Unauthenticated, "连接已失效，请重新连接。"));
        return SnapshotReply(session, result, input.Value<long?>("sinceRevision") ?? -1, input.Value<long?>("afterSequence") ?? 0);
    }
    public async Task<JObject> PollAsync(string connectionId, JObject input, CancellationToken cancellation)
    {
        var session = Require(connectionId);
        var verified = await ValidateAsync(session, true, cancellation);
        if (verified.Code != GameCodes.Ok) return Reply(verified);
        var response = SnapshotReply(session, verified, input.Value<long?>("sinceRevision") ?? -1, input.Value<long?>("afterSequence") ?? 0);
        response["sessionExpiresUtcMs"] = Interlocked.Read(ref session.ExpiresUtcMs);
        return response;
    }
    public JObject Disconnect(string connectionId)
    {
        lock (connectionGate)
        {
            if (connections.TryGetValue(connectionId ?? "", out var session))
            {
                session.InvalidCode = GameCodes.Unauthenticated;
                if (accounts.TryGetValue(session.Actor.AccountId, out var current) && ReferenceEquals(current, session)) accounts.TryRemove(session.Actor.AccountId, out _);
            }
        }
        return Reply(GameResult.Success());
    }
    public void Publish(GameResult result)
    {
        foreach (var gameEvent in result.Events)
            foreach (var session in accounts.Values)
            {
                if (session.Actor.WorldId != result.WorldId || !IsCurrent(session) ||
                    gameEvent.AudiencePlayerIds != null && !gameEvent.AudiencePlayerIds.Contains(session.Actor.PlayerId)) continue;
                gameEvent.WorldId = result.WorldId;
                lock (session.StreamGate)
                {
                    session.Events.Enqueue(new JObject { ["sequence"] = ++session.Sequence, ["event"] = JObject.FromObject(gameEvent) });
                    while (session.Events.Count > 256) session.Events.Dequeue();
                }
            }
    }
    private JObject SnapshotReply(GameSession session, GameResult result, long sinceRevision, long afterSequence)
    {
        long sequenceLimit;
        lock (session.StreamGate) sequenceLimit = session.Sequence;
        var snapshot = Runtime.Snapshot(session.Actor, projection);
        var visibleMessages = new HashSet<string>((snapshot.PublicWorld["chatMessages"] as JArray ?? new JArray())
            .OfType<JObject>().Select(message => message.Value<string>("messageId")), StringComparer.Ordinal);
        lock (session.StreamGate)
        {
            var wire = snapshot;
            var delta = session.LastSnapshot != null && sinceRevision == session.LastSnapshot.WorldRevision;
            if (delta) wire = new WorldSnapshot { WorldId = snapshot.WorldId, PlayerId = snapshot.PlayerId,
                WorldRevision = snapshot.WorldRevision, ServerUtcMs = snapshot.ServerUtcMs,
                PublicWorld = Changed(session.LastSnapshot.PublicWorld, snapshot.PublicWorld),
                PrivatePlayer = Changed(session.LastSnapshot.PrivatePlayer, snapshot.PrivatePlayer) };
            var response = Reply(result, wire);
            response["snapshotDelta"] = delta;
            if (delta) response["baseRevision"] = sinceRevision;
            response["events"] = new JArray(session.Events.Where(e => e.Value<long>("sequence") > afterSequence && e.Value<long>("sequence") <= sequenceLimit &&
                (e["event"].Value<string>("type") != "chat.message" || visibleMessages.Contains(e["event"]["data"]?.Value<string>("messageId"))))
                .Select(e => e.DeepClone()));
            // Events published during snapshot construction remain unacknowledged until the next reply.
            response["sequence"] = sequenceLimit;
            response["sessionExpiresUtcMs"] = Interlocked.Read(ref session.ExpiresUtcMs);
            response["eventsReset"] = session.Events.Count > 0 && afterSequence < session.Events.Peek().Value<long>("sequence") - 1;
            session.LastSnapshot = snapshot;
            return response;
        }
    }
    private static JObject Changed(JObject previous, JObject next)
    {
        var result = new JObject();
        foreach (var item in next.Properties()) if (!JToken.DeepEquals(previous[item.Name], item.Value)) result[item.Name] = item.Value.DeepClone();
        foreach (var item in previous.Properties()) if (next[item.Name] == null) result[item.Name] = JValue.CreateNull();
        return result;
    }
    private static JObject Reply(GameResult result, WorldSnapshot snapshot = null)
    {
        var response = new JObject { ["result"] = JObject.FromObject(result) };
        if (snapshot != null) response["snapshot"] = JObject.FromObject(snapshot);
        return response;
    }
}
