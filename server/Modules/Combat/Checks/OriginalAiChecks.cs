using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Dwsg.Persistence;
using Dwsg.Runtime;
using Dwsg.Server.Economy;
using Dwsg.Server.Modules.Combat;
using Dwsg.Server.World;
using Dwsg.Shared;
using Dwsg.Shared.Combat;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public static class OriginalAiChecks
{
    // 该适配只供原入口接线前的检查；实际Host仍由唯一所有者接入CombatModule。
    private sealed class Entry : IGameModule, IGameTickModule
    {
        private readonly CombatModule combat = new CombatModule();
        public IReadOnlyCollection<string> CommandTypes => new[] { CombatModule.OriginalAiTick };
        public GameResult Execute(WorldState state, CommandContext context, GameCommand command) => combat.ExecuteOriginalAi(state, context, command);
        public IEnumerable<GameCommand> CollectDueCommands(WorldState state, long utc) => combat.CollectOriginalAiCommands(state, utc);
    }

    public static void Run(string seedPath, string outputPath)
    {
        string directory = Path.GetFullPath(outputPath);
        if (!directory.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Contains("audit", StringComparer.OrdinalIgnoreCase))
            throw new ArgumentException("Checks must stay in ignored audit.");
        Directory.CreateDirectory(directory);
        int checks = 0;
        void Check(bool condition, string name) { if (!condition) throw new InvalidOperationException(name); checks++; Console.WriteLine("PASS " + name); }
        JObject seed = JObject.Parse(File.ReadAllText(seedPath));
        Check(((JArray)seed["城池列表"]).Count == 942 && ((JArray)seed["山贼列表"]).Count == 400, "actual original 942 cities / 400 bandits export");
        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000 * 1000;
        var world = new WorldState { WorldId = "ai-check-" + Guid.NewGuid().ToString("N"), Data = (JObject)seed.DeepClone(),
            EntityMappings = new JObject { ["players"] = new JObject(), ["humanPlayers"] = new JObject() } };
        JObject players = (JObject)world.EntityMappings["players"];
        for (int index = 0; index < ((JArray)world.Data["玩家列表"]).Count; index++) players[Guid.NewGuid().ToString("N")] = index;
        var entry = new Entry();
        Check(!entry.CollectDueCommands(world, now).Any(), "zero bound humans waits without using original template0");
        var actors = new List<AuthenticatedActor>();
        var bindings = new List<RoleBinding>();
        for (int index = 0; index < 2; index++)
        {
            Check(LegacyWorldModule.CreatePlayer(world, "AI检查真人" + index, "汉", now, out int legacy).Code == GameCodes.Ok, "actual original role creation " + index);
            string id = Guid.NewGuid().ToString("N"); players[id] = legacy;
            actors.Add(new AuthenticatedActor("ai-account-" + index, id, world.WorldId, "ai-connection-" + index));
            bindings.Add(new RoleBinding { WorldId = world.WorldId, AccountId = actors[index].AccountId, PlayerId = id, LegacyPlayerIndex = legacy });
        }
        // 故意倒置字典属性，必须仍按持久角色创建index选第一个真人。
        world.EntityMappings["humanPlayers"] = new JObject { [actors[1].PlayerId] = true, [actors[0].PlayerId] = true };
        string db = Path.Combine(directory, world.WorldId + ".sqlite3");
        SqliteWorldStore store = new SqliteWorldStore(db); store.ImportWorld(world, bindings);
        WorldRuntime Runtime()
        {
            var runtime = new WorldRuntime(store, actor => actor.IsSystem || actors.Any(real => real.PlayerId == actor.PlayerId && real.ConnectionId == actor.ConnectionId), () => now);
            runtime.Register(entry); runtime.Register(new EconomyModule()); return runtime;
        }
        WorldRuntime runtime = Runtime();
        GameCommand init = entry.CollectDueCommands(store.Load(world.WorldId), now).Single();
        Check(runtime.Execute(actors[0], init).Code == GameCodes.Forbidden, "human cannot invoke AI system command");
        var before = store.Load(world.WorldId);
        var result = runtime.Execute(AuthenticatedActor.System(world.WorldId), init);
        Check(result.Code == GameCodes.Ok, "AI schedule executes in original Runtime candidate / native SQLite transaction");
        var after = store.Load(world.WorldId);
        JObject schedule = (JObject)after.Data["原AI推城"];
        Check(schedule.Value<string>("ReferencePlayerId") == actors[0].PlayerId, "first bound human uses persistent creation order despite reversed dictionary");
        Check(JToken.DeepEquals(before.Data["玩家列表"], after.Data["玩家列表"]), "temporary original generation leaves all real owner technology / equipment / generals unchanged");
        Check(((JArray)schedule["各国下次出手UtcMs"]).Count == ((JArray)seed["国家列表"]).Count, "all original nations scheduled in original list order");
        Check(((JArray)schedule["各国下次出手UtcMs"]).Values<long>().All(due => due >= now && due < now + 300000), "initial deadlines use original0..299 seconds");
        long revision = after.Revision;
        var replay = runtime.Execute(AuthenticatedActor.System(world.WorldId), init);
        Check(replay.Code == GameCodes.Ok && store.Load(world.WorldId).Revision == revision, "same AI request replays without a second schedule / spawn");
        long next = schedule.Value<long>("NextTickUtcMs"); now = next - 1;
        Check(!entry.CollectDueCommands(store.Load(world.WorldId), now).Any(), "one millisecond before next deadline has no AI action");
        now = next;
        int attempts = 0, marches = 0; string firstBattle = null;
        for (int loop = 0; loop < 20 && marches == 0; loop++)
        {
            before = store.Load(world.WorldId);
            var command = entry.CollectDueCommands(before, now).Single();
            result = runtime.Execute(AuthenticatedActor.System(world.WorldId), command);
            if (result.Code == GameCodes.Unavailable) entry.Execute(before.Clone(), new CommandContext(AuthenticatedActor.System(world.WorldId), now), command);
            Check(result.Code == GameCodes.Ok, "actual country due action " + loop + " " + result.Code + " " + result.Message);
            after = store.Load(world.WorldId); schedule = (JObject)after.Data["原AI推城"];
            Check(JToken.DeepEquals(before.Data["玩家列表"], after.Data["玩家列表"]), "owner source is unchanged after original temporary generation " + loop);
            if (result.Data["countryIndex"] != null)
            {
                attempts++;
                int index = result.Data.Value<int>("countryIndex");
                Check(((JArray)schedule["各国下次出手UtcMs"])[index].Value<long>() >= now + 120000
                    && ((JArray)schedule["各国下次出手UtcMs"])[index].Value<long>() < now + 300000, "subsequent original120..299 interval " + loop);
                Check(schedule.Value<long>("NextTickUtcMs") >= now + 10000, "original10 second global stagger " + loop);
            }
            if (result.Data.Value<string>("dispatch") == "marching") { marches++; firstBattle = result.Data.Value<string>("battleId"); }
            now = schedule.Value<long>("NextTickUtcMs");
        }
        Check(attempts > 0 && marches > 0, "original country AI selects reachable real city and dispatches real temporary army");
        BanditBattle battle = after.Data["战斗运行"][firstBattle].ToObject<BanditBattle>();
        Check(battle.ArrivalUtcMs == schedule.Value<long>("上次出手UtcMs") + 30000 && battle.NextTickUtcMs == battle.ArrivalUtcMs, "original army30 second arrival is persisted in existing battle record");
        Check(battle.Attackers.Count == OriginalAiRules.GeneralCount(battle.CityScale) && battle.AttackFormations.All(formation => battle.Attackers.Count(unit => unit.ArmyId == formation.ArmyId) == 5)
            && battle.Attackers.All(unit => unit.Ephemeral && unit.Side == 0 && unit.General["详细信息"].Value<int>("坑位颜色") == 0), "original temporary level99 attackers preserve five-general formations");
        Check(result.Events.Count == 0 && schedule["军情"][firstBattle].Value<int>("身份") == 666, "original AI military entry uses server metadata without publishing private NPC unit data");
        string durable = after.Data.ToString(Formatting.None), maps = after.EntityMappings.ToString(Formatting.None);
        store.Dispose(); store = new SqliteWorldStore(db); runtime = Runtime();
        Check(store.Load(world.WorldId).Data.ToString(Formatting.None) == durable && store.Load(world.WorldId).EntityMappings.ToString(Formatting.None) == maps,
            "native SQLite reopen preserves committed deadlines / RNG / real army and reference");
        var dueCommand = entry.CollectDueCommands(store.Load(world.WorldId), now).Single();
        var purchase = new GameCommand { WorldId = world.WorldId, RequestId = Guid.NewGuid().ToString("N"), Type = "shop.purchase",
            Payload = new JObject { ["itemName"] = "将神魂", ["currency"] = "黄金", ["quantity"] = 1, ["catalogVersion"] = seed["商城配置版本"].DeepClone() } };
        var concurrent = new[] { Task.Run(() => runtime.Execute(actors[0], purchase)),
            Task.Run(() => runtime.Execute(AuthenticatedActor.System(world.WorldId), dueCommand)),
            Task.Run(() => runtime.Execute(AuthenticatedActor.System(world.WorldId), dueCommand)) };
        Task.WaitAll(concurrent);
        Check(concurrent[1].Result.Code == GameCodes.Ok && JsonConvert.SerializeObject(concurrent[1].Result) == JsonConvert.SerializeObject(concurrent[2].Result), "concurrent AI retry joins same durable command receipt");
        Check(concurrent[0].Result.Code == GameCodes.Ok, "real player purchase runs concurrently with original AI under world serialization");
        Check(store.Load(world.WorldId).Data["原AI推城"]["军情"][firstBattle] != null, "concurrent player command preserves already committed AI military record");
        Check(OriginalAiRules.TroopCount(4, 2, 8, 104, (low, high) => low) == 16400
            && OriginalAiRules.TroopCount(4, 2, 8, 404, (low, high) => low) == 6560, "original reference-leadership formula and404 truncation");
        Check(OriginalAiRules.TroopCount(0, 1, 0, 104, (low, high) => { Check(low == high && low == 0, "scale0 consumes original0..0 draw"); return low; }) == 2500, "original neutral scale0 troop calculation");
        store.Dispose();
        Console.WriteLine("Original AI scheduling checks=" + checks + "; controlled UTC regression, arrival/battle integration pending owner patch");
    }
}
