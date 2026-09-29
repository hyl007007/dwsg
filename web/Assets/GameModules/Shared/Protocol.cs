using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Dwsg.Shared
{
    public static class GameCodes
    {
        public const string Ok = "Ok", InvalidArgument = "InvalidArgument", Unauthenticated = "Unauthenticated",
            Forbidden = "Forbidden", NotFound = "NotFound", Conflict = "Conflict", RequestConflict = "RequestConflict",
            Unavailable = "Unavailable", WorldFull = "WorldFull", SessionReplaced = "SessionReplaced",
            RoleRequired = "RoleRequired", ProtocolMismatch = "ProtocolMismatch", InsufficientFunds = "InsufficientFunds";
    }

    public sealed class GameCommand
    {
        [JsonProperty("protocolVersion")] public int ProtocolVersion { get; set; } = 1;
        [JsonProperty("requestId")] public string RequestId { get; set; }
        [JsonProperty("worldId")] public string WorldId { get; set; }
        [JsonProperty("type")] public string Type { get; set; }
        [JsonProperty("payload")] public JObject Payload { get; set; } = new JObject();
    }

    public sealed class GameResult
    {
        [JsonProperty("protocolVersion")] public int ProtocolVersion { get; set; } = 1;
        [JsonProperty("requestId")] public string RequestId { get; set; }
        [JsonProperty("worldId")] public string WorldId { get; set; }
        [JsonProperty("worldRevision")] public long WorldRevision { get; set; }
        [JsonProperty("code")] public string Code { get; set; } = GameCodes.Ok;
        [JsonProperty("message")] public string Message { get; set; }
        [JsonProperty("data")] public JObject Data { get; set; } = new JObject();
        [JsonIgnore] public List<GameEvent> Events { get; set; } = new List<GameEvent>();
        public static GameResult Reject(string code, string message) { return new GameResult { Code = code, Message = message }; }
        public static GameResult Success(JObject data = null) { return new GameResult { Data = data ?? new JObject() }; }
    }

    public sealed class GameEvent
    {
        [JsonProperty("eventId")] public string EventId { get; set; } = Guid.NewGuid().ToString("N");
        [JsonProperty("worldId")] public string WorldId { get; set; }
        [JsonProperty("type")] public string Type { get; set; }
        [JsonProperty("serverUtcMs")] public long ServerUtcMs { get; set; }
        [JsonProperty("data")] public JObject Data { get; set; } = new JObject();
        // null means public; a non-null set authorizes only the listed stable player IDs.
        [JsonIgnore] public string[] AudiencePlayerIds { get; set; }
    }

    public sealed class WorldSnapshot
    {
        [JsonProperty("protocolVersion")] public int ProtocolVersion { get; set; } = 1;
        [JsonProperty("worldId")] public string WorldId { get; set; }
        [JsonProperty("playerId")] public string PlayerId { get; set; }
        [JsonProperty("worldRevision")] public long WorldRevision { get; set; }
        [JsonProperty("serverUtcMs")] public long ServerUtcMs { get; set; }
        [JsonProperty("publicWorld")] public JObject PublicWorld { get; set; } = new JObject();
        [JsonProperty("privatePlayer")] public JObject PrivatePlayer { get; set; } = new JObject();
    }

    public sealed class AuthenticatedActor
    {
        public string AccountId { get; private set; }
        public string PlayerId { get; private set; }
        public string WorldId { get; private set; }
        public string ConnectionId { get; private set; }
        public bool IsSystem { get; private set; }
        public AuthenticatedActor(string accountId, string playerId, string worldId, string connectionId)
        {
            AccountId = accountId; PlayerId = playerId; WorldId = worldId; ConnectionId = connectionId;
        }
        public static AuthenticatedActor System(string worldId)
        {
            return new AuthenticatedActor("server", "server", worldId, "server") { IsSystem = true };
        }
    }

    public sealed class CommandContext
    {
        public AuthenticatedActor Actor { get; private set; }
        public long ServerUtcMs { get; private set; }
        public CommandContext(AuthenticatedActor actor, long serverUtcMs) { Actor = actor; ServerUtcMs = serverUtcMs; }
    }

    public static class CommandFingerprint
    {
        public static string Calculate(GameCommand command)
        {
            var body = new JObject { ["protocolVersion"] = command.ProtocolVersion, ["worldId"] = command.WorldId,
                ["type"] = command.Type, ["payload"] = command.Payload.DeepClone() };
            using (var sha = SHA256.Create())
                return string.Concat(sha.ComputeHash(Encoding.UTF8.GetBytes(Canonical(body).ToString(Formatting.None)))
                    .Select(value => value.ToString("x2")));
        }
        private static JToken Canonical(JToken value)
        {
            var obj = value as JObject;
            if (obj != null) return new JObject(obj.Properties().OrderBy(p => p.Name, StringComparer.Ordinal)
                .Select(p => new JProperty(p.Name, Canonical(p.Value))));
            var array = value as JArray;
            return array != null ? new JArray(array.Select(Canonical)) : value.DeepClone();
        }
    }
}
