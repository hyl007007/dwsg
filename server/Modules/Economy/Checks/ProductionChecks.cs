using Dwsg.Persistence;
using Dwsg.Runtime;
using Dwsg.Server.Economy;
using Dwsg.Server.World;
using Dwsg.Shared;
using Dwsg.Shared.Economy;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

internal static class ProductionChecks
{
    public static int Run(JObject seed, string database)
    {
        if (File.Exists(database)) throw new ArgumentException("Use a new disposable production database.");
        int checks = 0;
        void Check(bool valid, string name)
        {
            if (!valid) throw new InvalidOperationException(name);
            checks++;
            Console.WriteLine("PASS " + name);
        }
        var world = new WorldState { WorldId = Guid.NewGuid().ToString("N"), Data = (JObject)seed.DeepClone(),
            EntityMappings = new JObject { ["players"] = new JObject(), ["humanPlayers"] = new JObject(), ["fiefs"] = new JObject() } };
        for (int i = 0; i < world.Data["玩家列表"].Count(); i++) world.EntityMappings["players"][Guid.NewGuid().ToString("N")] = i;
        var bindings = new List<RoleBinding>();
        var fiefIds = new List<string>();
        long initialUtc = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        for (int i = 0; i < 2; i++)
        {
            Check(LegacyWorldModule.CreatePlayer(world, "种田君主" + i, "汉", initialUtc, out int index).Code == GameCodes.Ok,
                "production uses actual original role creation " + i);
            string id = Guid.NewGuid().ToString("N");
            world.EntityMappings["players"][id] = index;
            world.EntityMappings["humanPlayers"][id] = true;
            string fiefId = Guid.NewGuid().ToString("N");
            fiefIds.Add(fiefId);
            world.EntityMappings["fiefs"][fiefId] = new JObject { ["playerId"] = id, ["legacyId"] = world.RequirePlayer(id)["封地信息表"][0]["ID"].DeepClone() };
            world.RequirePlayer(id)["财产信息"]["铜钱"] = 5000000.0;
            world.RequirePlayer(id)["财产信息"]["粮食"] = 5000000.0;
            ProductionModule.InitializePlayer(world, id, initialUtc);
            bindings.Add(new RoleBinding { WorldId = world.WorldId, AccountId = Guid.NewGuid().ToString("N"), PlayerId = id, LegacyPlayerIndex = index });
        }
        var actors = bindings.Select(b => new AuthenticatedActor(b.AccountId, b.PlayerId, world.WorldId, Guid.NewGuid().ToString("N"))).ToArray();
        var han = world.Data["国家列表"].OfType<JObject>().Single(n => n.Value<string>("国号") == "汉");
        han["资源科技"] = 50.0;
        GameCommand Command(string type, int owner = 0, int plot = 1) => new GameCommand
        {
            WorldId = world.WorldId, RequestId = Guid.NewGuid().ToString("N"), Type = type,
            Payload = new JObject { ["fiefId"] = fiefIds[owner], ["plot"] = plot }
        };
        GameCommand Construct(int owner = 0, int plot = 1, int type = 3)
        {
            var command = Command("fief.construct", owner, plot);
            command.Payload["buildingType"] = type;
            return command;
        }
        JObject Farmer(WorldState state) => state.RequirePlayer(actors[0].PlayerId);
        string Json(JToken value) => value.ToString(Formatting.None);
        var farmer = (JObject)Farmer(world).DeepClone();
        var fief = (JObject)farmer["封地信息表"][0];
        string freeWallet = Json(farmer["财产信息"]);
        Check(BuildingRules.Construct(farmer, fief, 1, 3).Code == GameCodes.Ok && freeWallet == Json(farmer["财产信息"]) &&
            farmer["基础信息"].Value<double>("粮食增加") == 1, "original immediate free level-one farm");
        string occupied = Json(farmer);
        Check(BuildingRules.Construct(farmer, fief, 1, 3).Code == GameCodes.Conflict && occupied == Json(farmer), "occupied plot cannot inflate production");
        Check(ProductionRules.ProduceOneSecond(farmer, 50).Code == GameCodes.Ok && farmer["财产信息"].Value<double>("粮食") == 5000001.5,
            "original one grain per level per second with nation percentage bonus");
        double grain = farmer["财产信息"].Value<double>("粮食");
        Check(BuildingRules.Upgrade(farmer, fief, 1).Code == GameCodes.Ok && fief["建筑信息表"][1].Value<int>("等级") == 2 &&
            farmer["财产信息"].Value<double>("铜钱") == 4999200 && farmer["财产信息"].Value<double>("粮食") == grain - 1600 &&
            farmer["基础信息"].Value<double>("粮食增加") == 2, "farm upgrade charges original 800 copper and 1600 grain and produces two");
        Check(BuildingRules.Demolish(farmer, fief, 1).Code == GameCodes.Ok && farmer["基础信息"].Value<double>("粮食增加") == 0,
            "upgraded demolished farm leaves no phantom production");
        string demolishedWallet = Json(farmer["财产信息"]);
        ProductionRules.ProduceOneSecond(farmer, 50);
        Check(demolishedWallet == Json(farmer["财产信息"]), "demolished farm produces no grain and has no refund");
        foreach (int type in new[] { 1, 2, 4, 5, 6, 7 })
        {
            var other = (JObject)Farmer(world).DeepClone(); var plot = (JObject)other["封地信息表"][0];
            Check(BuildingRules.Construct(other, plot, 1, type).Code == GameCodes.Ok && BuildingRules.Upgrade(other, plot, 1).Code == GameCodes.Ok &&
                plot["建筑信息表"][1].Value<int>("等级") == 2 && other["基础信息"].Value<double>("粮食增加") == 0,
                "original non-farm construction and upgrade " + type);
        }
        Check(BuildingRules.Construct(farmer, fief, 1, 1).Code == GameCodes.Ok && BuildingRules.Construct(farmer, fief, 2, 1).Code == GameCodes.Conflict,
            "one academy per fief");
        BuildingRules.Construct(farmer, fief, 2, 3);
        farmer["财产信息"]["铜钱"] = 800.0;
        string exactCopper = Json(farmer);
        Check(BuildingRules.Upgrade(farmer, fief, 2).Code == GameCodes.InsufficientFunds && exactCopper == Json(farmer), "original strict copper greater-than cost");
        farmer["财产信息"]["铜钱"] = 1000.0; farmer["财产信息"]["粮食"] = 1600.0;
        string exactGrain = Json(farmer);
        Check(BuildingRules.Upgrade(farmer, fief, 2).Code == GameCodes.InsufficientFunds && exactGrain == Json(farmer), "original strict grain greater-than cost");
        fief["建筑信息表"][0]["等级"] = 15;
        fief["建筑信息表"][2]["等级"] = 14;
        Check(BuildingRules.UpgradePlot(fief, 2).Code == GameCodes.Ok && BuildingRules.UpgradePlot(fief, 2).Code == GameCodes.Conflict,
            "original first fief farm cap fifteen");
        fief["建筑信息表"][2]["类型"] = 4; fief["建筑信息表"][2]["等级"] = 9;
        Check(BuildingRules.UpgradePlot(fief, 2).Code == GameCodes.Ok && BuildingRules.UpgradePlot(fief, 2).Code == GameCodes.Conflict,
            "original barracks cap ten in first fief");
        fief["ID"] = 2; fief["建筑信息表"][2]["类型"] = 3;
        Check(BuildingRules.UpgradePlot(fief, 2).Code == GameCodes.Conflict && BuildingRules.UpgradePlot(fief, 0).Code == GameCodes.Conflict,
            "original other fief hall and farm cap ten");
        Check(BuildingRules.DemolishPlot(fief, 0).Code == GameCodes.InvalidArgument && BuildingRules.ConstructPlot(fief, 13, 3).Code == GameCodes.InvalidArgument,
            "hall cannot be demolished and plot thirteen is invalid");

        var timed = world.Clone(); var module = new ProductionModule();
        var system = AuthenticatedActor.System(world.WorldId);
        GameResult Timed(ProductionModule target, WorldState state, string type, long utc) => target.Execute(state, new CommandContext(system, utc),
            new GameCommand { WorldId = world.WorldId, RequestId = Guid.NewGuid().ToString("N"), Type = type });
        Check(Timed(module, timed, "world.production.resume", 100000).Code == GameCodes.Ok, "server production resume initializes human clocks");
        var timedFarmer = Farmer(timed);
        BuildingRules.Construct(timedFarmer, (JObject)timedFarmer["封地信息表"][0], 1, 3);
        timedFarmer["财产信息"]["粮食"] = 300000000.0;
        timedFarmer["财产信息"]["铜钱"] = 100000001.0;
        timedFarmer["财产信息"]["黄金"] = 6666667.0;
        timedFarmer["财产信息"]["白银"] = 6666667.0;
        Check(Timed(module, timed, "world.production.tick", 103000).Code == GameCodes.Ok && timedFarmer["财产信息"].Value<double>("粮食") == 300000004.5,
            "production preserves original interval before four-second cap");
        Check(Timed(module, timed, "world.production.tick", 104000).Code == GameCodes.Ok && timedFarmer["财产信息"].Value<double>("粮食") == 300000000 &&
            timedFarmer["财产信息"].Value<double>("铜钱") == 100000000 && timedFarmer["财产信息"].Value<double>("黄金") == 6666666 &&
            timedFarmer["财产信息"].Value<double>("白银") == 6666666, "original four wallet caps occur at four seconds");
        double afterCap = timedFarmer["财产信息"].Value<double>("粮食");
        var restarted = new ProductionModule();
        Check(Timed(restarted, timed, "world.production.resume", 864104000).Code == GameCodes.Ok && timedFarmer["财产信息"].Value<double>("粮食") == afterCap,
            "server restart grants no shutdown-time grain");
        Check(Timed(restarted, timed, "world.production.tick", 864105000).Code == GameCodes.Ok && timedFarmer["财产信息"].Value<double>("粮食") == afterCap + 1.5,
            "restarted server produces only new elapsed second");

        GameCommand durable;
        string receipt;
        string npcBefore = Json(world.Data["玩家列表"][0]);
        double persistedGrain;
        using (var store = new SqliteWorldStore(database))
        {
            store.ImportWorld(world, bindings);
            var runtime = new WorldRuntime(store, actor => actor.IsSystem || store.ResolveRole(actor.WorldId, actor.AccountId)?.PlayerId == actor.PlayerId);
            var liveModule = new ProductionModule(); runtime.Register(liveModule);
            runtime.Tick(world.WorldId);
            durable = Construct();
            var built = runtime.Execute(actors[0], durable); receipt = JsonConvert.SerializeObject(built);
            Check(built.Code == GameCodes.Ok && built.Events.Single().AudiencePlayerIds.SequenceEqual(new[] { actors[0].PlayerId }), "actual Runtime and SQLite privately commit original farm");
            Check(receipt == JsonConvert.SerializeObject(runtime.Execute(actors[0], durable)) && Farmer(store.Load(world.WorldId))["基础信息"].Value<double>("粮食增加") == 1,
                "construction receipt replay cannot add another farm or production");
            string beforeRejected = Json(store.Load(world.WorldId).Data);
            Check(runtime.Execute(actors[0], Construct(1)).Code == GameCodes.Forbidden && beforeRejected == Json(store.Load(world.WorldId).Data), "another player's stable fief cannot be changed");
            var forged = Construct(plot: 2); forged.Payload["playerId"] = actors[1].PlayerId;
            Check(runtime.Execute(actors[0], forged).Code == GameCodes.InvalidArgument && beforeRejected == Json(store.Load(world.WorldId).Data), "client cannot submit player ownership");
            var fraction = Construct(plot: 2); fraction.Payload["plot"] = 1.5;
            Check(runtime.Execute(actors[0], fraction).Code == GameCodes.InvalidArgument, "fractional plot rejected");
            var oversized = Construct(plot: 2); oversized.Payload["plot"] = JToken.Parse("9223372036854775808");
            Check(runtime.Execute(actors[0], oversized).Code == GameCodes.InvalidArgument && beforeRejected == Json(store.Load(world.WorldId).Data),
                "oversized JSON integer plot rejected without world mutation");
            var manual = new GameCommand { WorldId = world.WorldId, RequestId = Guid.NewGuid().ToString("N"), Type = "world.production.tick" };
            Check(runtime.Execute(actors[0], manual).Code == GameCodes.Forbidden, "client cannot request resource production");
            var concurrent = Enumerable.Range(0, 2).Select(_ => Task.Run(() => runtime.Execute(actors[0], Construct(plot: 2)))).ToArray();
            Task.WaitAll(concurrent);
            Check(concurrent.Count(t => t.Result.Code == GameCodes.Ok) == 1 && concurrent.Count(t => t.Result.Code == GameCodes.Conflict) == 1 &&
                Farmer(store.Load(world.WorldId))["基础信息"].Value<double>("粮食增加") == 2, "actual concurrent construction claims occupied plot once");
            Thread.Sleep(1100);
            var beforeTick = store.Load(world.WorldId);
            runtime.Tick(world.WorldId);
            var afterTick = store.Load(world.WorldId);
            long elapsed = (afterTick.EntityMappings["production"][actors[0].PlayerId].Value<long>("lastUtcMs") -
                beforeTick.EntityMappings["production"][actors[0].PlayerId].Value<long>("lastUtcMs")) / 1000;
            Check(elapsed >= 1 && Farmer(afterTick)["财产信息"].Value<double>("粮食") == Farmer(beforeTick)["财产信息"].Value<double>("粮食") + elapsed * 3,
                "actual server tick atomically persists only human farm grain");
            Check(npcBefore == Json(afterTick.Data["玩家列表"][0]) && ((JObject)afterTick.EntityMappings["production"]).Count == 2, "original NPC skeleton is never registered for production");
            Check(runtime.Execute(actors[0], Command("fief.upgrade")).Code == GameCodes.Ok && Farmer(store.Load(world.WorldId))["基础信息"].Value<double>("粮食增加") == 3,
                "actual upgrade rate equals existing farm levels");
            Check(runtime.Execute(actors[0], Command("fief.demolish")).Code == GameCodes.Ok && Farmer(store.Load(world.WorldId))["基础信息"].Value<double>("粮食增加") == 1,
                "actual upgraded farm demolition keeps only other farm");
            Check(runtime.Execute(actors[0], Command("fief.demolish", plot: 2)).Code == GameCodes.Ok && Farmer(store.Load(world.WorldId))["基础信息"].Value<double>("粮食增加") == 0,
                "actual last farm demolition eliminates phantom grain");
            persistedGrain = Farmer(store.Load(world.WorldId))["财产信息"].Value<double>("粮食");
        }
        using (var store = new SqliteWorldStore(database))
        {
            var runtime = new WorldRuntime(store, actor => actor.IsSystem || store.ResolveRole(actor.WorldId, actor.AccountId)?.PlayerId == actor.PlayerId);
            runtime.Register(new ProductionModule());
            Check(receipt == JsonConvert.SerializeObject(runtime.Execute(actors[0], durable)) && Farmer(store.Load(world.WorldId))["基础信息"].Value<double>("粮食增加") == 0,
                "reopened actual database replays construction without resurrecting demolished farm");
            runtime.Tick(world.WorldId);
            Check(Farmer(store.Load(world.WorldId))["财产信息"].Value<double>("粮食") == persistedGrain && npcBefore == Json(store.Load(world.WorldId).Data["玩家列表"][0]),
                "actual reopened server resume preserves grain and NPC state");
        }
        Console.WriteLine("PRODUCTION_CHECKS_PASS " + checks);
        return checks;
    }
}
