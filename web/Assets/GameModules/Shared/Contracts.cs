using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Dwsg.Shared
{
    public sealed class WorldState
    {
        public string WorldId { get; set; }
        public long Revision { get; set; }
        // Original Chinese save document. No alternative city/player schema is introduced.
        public JObject Data { get; set; } = new JObject();
        public JObject EntityMappings { get; set; } = new JObject();
        public WorldState Clone()
        {
            return new WorldState { WorldId = WorldId, Revision = Revision,
                Data = (JObject)Data.DeepClone(), EntityMappings = (JObject)EntityMappings.DeepClone() };
        }
        public JObject RequirePlayer(string playerId)
        {
            var position = ResolvePlayerIndex(playerId);
            return (JObject)((JArray)Data["玩家列表"])[position];
        }
        public int ResolvePlayerIndex(string playerId)
        {
            var index = EntityMappings["players"]?[playerId];
            if (index == null || index.Type != JTokenType.Integer) throw new InvalidOperationException("Player mapping missing");
            var players = Data["玩家列表"] as JArray;
            var position = index.Value<int>();
            if (players == null || position < 0 || position >= players.Count || !(players[position] is JObject))
                throw new InvalidOperationException("Player mapping invalid");
            return position;
        }
    }

    public sealed class RoleBinding
    {
        public string WorldId { get; set; }
        public string AccountId { get; set; }
        public string PlayerId { get; set; }
        public int LegacyPlayerIndex { get; set; }
    }

    public sealed class CommandReceipt
    {
        public string WorldId { get; set; }
        public string PlayerId { get; set; }
        public string RequestId { get; set; }
        public string Fingerprint { get; set; }
        public string ResultJson { get; set; }
    }

    public sealed class WorldCommit
    {
        // Trusted runtime context. Never populated from a network request.
        public AuthenticatedActor Actor { get; set; }
        public long ExpectedRevision { get; set; }
        public WorldState Candidate { get; set; }
        public CommandReceipt Receipt { get; set; }
        public RoleBinding Binding { get; set; }
    }

    public sealed class CommitResult
    {
        public string Code { get; set; }
        public CommandReceipt Receipt { get; set; }
        public bool Replayed { get; set; }
    }

    public interface IWorldStore
    {
        WorldState Load(string worldId);
        RoleBinding ResolveRole(string worldId, string accountId);
        CommandReceipt FindReceipt(string worldId, string playerId, string requestId);
        // State, optional binding and receipt must commit atomically; failed candidates never reach this method.
        // Identical receipts return the original. Changed fingerprints return RequestConflict, without writes.
        CommitResult Commit(WorldCommit commit);
    }

    public interface IGameModule
    {
        IReadOnlyCollection<string> CommandTypes { get; }
        GameResult Execute(WorldState candidate, CommandContext context, GameCommand command);
    }

    public interface IGameTickModule
    {
        IEnumerable<GameCommand> CollectDueCommands(WorldState state, long serverUtcMs);
    }

    public interface IWorldProjection
    {
        WorldSnapshot Build(WorldState state, AuthenticatedActor actor, long serverUtcMs);
    }

    public delegate GameResult CreatePlayer(WorldState candidate, string nickname, string nation,
        long serverUtcMs, out int legacyPlayerIndex);
}
