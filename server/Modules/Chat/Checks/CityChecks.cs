using Dwsg.Client.Chat;
using Dwsg.Persistence;
using Dwsg.Runtime;
using Dwsg.Server.Chat;
using Dwsg.Server.World;
using Dwsg.Shared;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

internal static class CityChecks
{
    internal static void Run(JObject seed, string directory, Action<bool, string> check)
    {
        var original = new WorldState { WorldId = "city-chat", Data = seed, EntityMappings = new JObject { ["players"] = new JObject() } };
        for (int i = 0; i < ((JArray)seed["玩家列表"]).Count; i++) original.EntityMappings["players"][Guid.NewGuid().ToString("N")] = i;
        var firstNation = ((JArray)seed["国家列表"]).OfType<JObject>().First();
        var anotherNation = ((JArray)seed["国家列表"]).OfType<JObject>().First(n =>
            !JToken.DeepEquals(n["国都x"], firstNation["国都x"]) || !JToken.DeepEquals(n["国都y"], firstNation["国都y"]));
        var actors = new List<AuthenticatedActor>();
        string path = Path.Combine(directory, "city-chat.sqlite");
        GameCommand sent = null;
        GameResult accepted = null;
        using (var store = new SqliteWorldStore(path))
        {
            StableNationIds.EnsureMappings(original);
            store.ImportWorld(original);
            for (int i = 0; i < 3; i++)
            {
                var roleState = store.Load(original.WorldId);
                var candidate = roleState.Clone();
                string id = Guid.NewGuid().ToString("N"), account = "city-check-" + i;
                var created = LegacyWorldModule.CreatePlayer(candidate, "城池居民" + i,
                    (i == 2 ? anotherNation : firstNation).Value<string>("国号"), DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), out int index);
                if (created.Code != GameCodes.Ok) throw new Exception("Actual M03 role creation failed: " + created.Message);
                candidate.EntityMappings["players"][id] = index;
                var actor = new AuthenticatedActor(account, id, original.WorldId, "city-connection-" + i);
                var binding = new RoleBinding { WorldId = original.WorldId, AccountId = account, PlayerId = id, LegacyPlayerIndex = index };
                if (store.Commit(Commit(roleState, candidate, actor, "role-" + i, created, binding)).Code != GameCodes.Ok)
                    throw new Exception("Actual role binding transaction failed");
                actors.Add(actor);
            }
            var runtime = new WorldRuntime(store, a => actors.Any(p => p.ConnectionId == a.ConnectionId && p.PlayerId == a.PlayerId && p.AccountId == a.AccountId));
            runtime.Register(new ChatModule());
            var events = new List<GameEvent>(); runtime.Committed += r => events.AddRange(r.Events);
            int x = firstNation.Value<int>("国都x"), y = firstNation.Value<int>("国都y");
            sent = City("city-message", x, y);
            accepted = runtime.Execute(actors[0], sent);
            check(accepted.Code == GameCodes.Ok && events.Count == 1, "actual M03 fief resident commits city chat in SQLite");
            check(events[0].AudiencePlayerIds.Contains(actors[0].PlayerId) && events[0].AudiencePlayerIds.Contains(actors[1].PlayerId) &&
                !events[0].AudiencePlayerIds.Contains(actors[2].PlayerId), "city event excludes other city residents");
            var state = store.Load(original.WorldId);
            check(ChatModule.ReadHistory(state, actors[1]).Count == 1 && ChatModule.ReadHistory(state, actors[2]).Count == 0 &&
                ChatModule.ReadHistory(state, null).Count == 0, "city history is restricted to actual current residents");
            check(ChatModule.ReadCities(state, actors[0]).Count == 1 && ChatModule.ReadCities(state, null).Count == 0, "server projects private city permission without actor indexes");
            check(JsonConvert.SerializeObject(runtime.Execute(actors[0], sent)) == JsonConvert.SerializeObject(accepted) && events.Count == 1,
                "city request replay returns exact receipt and does not republish");
            check(runtime.Execute(actors[2], City("forged-city", x, y)).Code == GameCodes.Forbidden, "other city's valid actor cannot claim this city");
            var forged = City("forged-fief", x, y); forged.Payload["fiefId"] = actors[0].PlayerId;
            check(runtime.Execute(actors[2], forged).Code == GameCodes.InvalidArgument, "client cannot submit someone else's private fief or actor");
            var oversized = City("oversized-coordinate", x, y); oversized.Payload["cityX"] = long.MaxValue;
            check(runtime.Execute(actors[0], oversized).Code == GameCodes.InvalidArgument, "city coordinates reject integer overflow");

            var inbox = new ChatInbox(); var before = Snapshot(state, actors[1]);
            inbox.SetSession(before);
            var message = (JObject)accepted.Data["chatMessage"];
            check(inbox.TryReceive(state.WorldId, message, out bool self) && !self, "client receives other resident's current selected city message");
            check(!inbox.TryReceive(state.WorldId, message, out _), "city event result and history de-duplicate");
            check(inbox.SetSession(before, 99) && !inbox.TryReceive(state.WorldId, message, out _), "invalid selected fief clears city view without granting another location");
            check(inbox.SetSession(before) && inbox.TryReceive(state.WorldId, message, out _), "changing selected fief rebuilds its authorized city history");
            inbox.SetSession(Snapshot(state, actors[2]));
            check(!inbox.TryReceive(state.WorldId, message, out _), "client denies other city event even if transport misroutes");
            inbox.SetSession(before);

            // A server-admin fixture move changes both original authoritative endpoints; M03 move remains its owner's business.
            var moved = state.Clone();
            int playerIndex = moved.ResolvePlayerIndex(actors[1].PlayerId);
            var fief = (JObject)((JArray)moved.RequirePlayer(actors[1].PlayerId)["封地信息表"])[0];
            var oldCity = ((JArray)moved.Data["城池列表"]).OfType<JObject>().First(c => c.Value<int>("坐标x") == x && c.Value<int>("坐标y") == y);
            var registered = ((JArray)oldCity["城池封地列表"]).OfType<JObject>().Single(r => r.Value<int>("第几个玩家") == playerIndex && JToken.DeepEquals(r["封地ID标识"], fief["ID"]));
            var nextCity = ((JArray)moved.Data["城池列表"]).OfType<JObject>().First(c =>
                JToken.DeepEquals(c["坐标x"], anotherNation["国都x"]) && JToken.DeepEquals(c["坐标y"], anotherNation["国都y"]));
            registered.Remove(); ((JArray)nextCity["城池封地列表"]).Add(registered);
            fief["所在城池"] = new JObject { ["x"] = nextCity["坐标x"].DeepClone(), ["y"] = nextCity["坐标y"].DeepClone() };
            check(store.Commit(Commit(state, moved, AuthenticatedActor.System(state.WorldId), "admin-fixture-move", GameResult.Success())).Code == GameCodes.Ok,
                "server fixture move preserves both original city-fief endpoints atomically");
            var afterMove = store.Load(state.WorldId);
            check(ChatModule.ReadHistory(afterMove, actors[1]).Count == 0, "leaving a city removes its history permission");
            check(inbox.SetSession(Snapshot(afterMove, actors[1])) && !inbox.TryReceive(state.WorldId, message, out _), "city change clears client cache and rejects old city records");
            check(runtime.Execute(actors[1], City("old-city-after-move", x, y)).Code == GameCodes.Forbidden, "moved resident cannot continue using stale city location");
            Thread.Sleep(1100);
            var atNewCity = runtime.Execute(actors[1], City("new-city-message", nextCity.Value<int>("坐标x"), nextCity.Value<int>("坐标y")));
            check(atNewCity.Code == GameCodes.Ok && events.Last().AudiencePlayerIds.Contains(actors[2].PlayerId) && !events.Last().AudiencePlayerIds.Contains(actors[0].PlayerId),
                "new city's actual residents receive moved player's message");

            var inconsistent = afterMove.Clone();
            var oldFief = (JObject)((JArray)inconsistent.RequirePlayer(actors[0].PlayerId)["封地信息表"])[0];
            oldFief["所在城池"] = (JObject)fief["所在城池"].DeepClone();
            check(ChatModule.ReadCities(inconsistent, actors[0]).Count == 0, "a stale private fief location without original city registration grants no permission");
            var revokedInbox = new ChatInbox(); revokedInbox.SetSession(Snapshot(afterMove, actors[0]));
            check(revokedInbox.SetSession(Snapshot(inconsistent, actors[0])) && !revokedInbox.TryReceive(state.WorldId, message, out _),
                "server city permission withdrawal clears cache despite stale private fief coordinates");
        }
        using (var restored = new SqliteWorldStore(path))
        {
            var runtime = new WorldRuntime(restored, a => actors.Any(p => p.ConnectionId == a.ConnectionId)); runtime.Register(new ChatModule());
            int published = 0; runtime.Committed += r => published += r.Events.Count;
            check(JsonConvert.SerializeObject(runtime.Execute(actors[0], sent)) == JsonConvert.SerializeObject(accepted) && published == 0,
                "actual SQLite restart replays original city receipt without re-emitting");
            check(ChatModule.ReadHistory(restored.Load(original.WorldId), actors[0]).Count == 1 && ChatModule.ReadHistory(restored.Load(original.WorldId), actors[1]).Count == 1,
                "city history and changed membership survive actual restart");
        }

        GameCommand City(string id, int x, int y) => new() { WorldId = original.WorldId, RequestId = id, Type = "chat.send",
            Payload = new JObject { ["channel"] = "city", ["cityX"] = x, ["cityY"] = y, ["content"] = "真实城池消息" } };
    }

    private static WorldSnapshot Snapshot(WorldState state, AuthenticatedActor actor)
    {
        var snapshot = new WorldSnapshot { WorldId = state.WorldId, PlayerId = actor.PlayerId, WorldRevision = state.Revision,
            PrivatePlayer = (JObject)state.RequirePlayer(actor.PlayerId).DeepClone() };
        snapshot.PrivatePlayer["chatCities"] = ChatModule.ReadCities(state, actor); return snapshot;
    }

    private static WorldCommit Commit(WorldState original, WorldState candidate, AuthenticatedActor actor, string requestId,
        GameResult result, RoleBinding binding = null)
    {
        candidate.Revision = original.Revision + 1;
        result.RequestId = requestId; result.WorldId = original.WorldId; result.WorldRevision = candidate.Revision;
        return new WorldCommit { Actor = actor, ExpectedRevision = original.Revision, Candidate = candidate, Binding = binding,
            Receipt = new CommandReceipt { WorldId = original.WorldId, PlayerId = actor.PlayerId, RequestId = requestId,
                Fingerprint = CommandFingerprint.Calculate(new GameCommand { RequestId = requestId, WorldId = original.WorldId, Type = "city-check-admin" }),
                ResultJson = JsonConvert.SerializeObject(result) } };
    }
}
