using Dwsg.Persistence;
using Dwsg.Server.Economy;
using Dwsg.Server.Modules.Generals;
using Dwsg.Shared;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Dwsg.Host;

public static class WorldRoleRegistration
{
    public static void Initialize(SqliteWorldStore store, string worldId, long serverUtcMs)
    {
        var original = store.Load(worldId) ?? throw new InvalidOperationException("World missing");
        var candidate = original.Clone();
        var humans = new JObject();
        foreach (var role in store.ListRoles(worldId))
        {
            if (candidate.ResolvePlayerIndex(role.PlayerId) != role.LegacyPlayerIndex)
                throw new InvalidDataException("Persisted human role mapping mismatch");
            humans[role.PlayerId] = true;
        }
        candidate.EntityMappings["humanPlayers"] = humans;
        GeneralsModule.EnsureMappings(candidate);
        foreach (var human in humans.Properties())
            ProductionModule.InitializePlayer(candidate, human.Name, serverUtcMs);
        if (JToken.DeepEquals(original.Data, candidate.Data) && JToken.DeepEquals(original.EntityMappings, candidate.EntityMappings)) return;
        candidate.Revision = checked(original.Revision + 1);
        var command = new GameCommand { WorldId = worldId, RequestId = "host-role-registration:" + original.Revision,
            Type = "world.roles.initialize", Payload = new JObject { ["humanPlayers"] = humans.DeepClone() } };
        var result = GameResult.Success();
        result.WorldId = worldId; result.RequestId = command.RequestId; result.WorldRevision = candidate.Revision;
        var committed = store.Commit(new WorldCommit { Actor = AuthenticatedActor.System(worldId), Candidate = candidate,
            ExpectedRevision = original.Revision, Receipt = new CommandReceipt { WorldId = worldId, PlayerId = "server",
                RequestId = command.RequestId, Fingerprint = CommandFingerprint.Calculate(command), ResultJson = JsonConvert.SerializeObject(result) } });
        if (committed.Code != GameCodes.Ok || committed.Replayed) throw new InvalidOperationException("Human role registration was not committed");
    }
}
