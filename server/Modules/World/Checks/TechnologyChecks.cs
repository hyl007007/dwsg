using Dwsg.Persistence;
using Dwsg.Runtime;
using Dwsg.Server.World;
using Dwsg.Server.Modules.Generals;
using Dwsg.Shared;
using Dwsg.Shared.Economy;
using Dwsg.Shared.Generals;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

internal static class TechnologyChecks
{
    public static void Run(WorldState original, List<RoleBinding> bindings, string database)
    {
        int checks = 0;
        void Check(bool success, string name)
        {
            if (!success) throw new InvalidOperationException(name);
            checks++; Console.WriteLine("PASS " + name);
        }
        string Json(JToken value) => value.ToString(Formatting.None);
        string owner = bindings[0].PlayerId;
        JObject Player(WorldState state) => state.RequirePlayer(owner);
        JObject Fief(WorldState state) => (JObject)Player(state)["封地信息表"][0];
        var world = original.Clone();
        // Prepare the actual original 13-plot fief through the existing original construction rules.
        Check(BuildingRules.Construct(Player(world), Fief(world), 1, 1).Code == GameCodes.Ok, "actual original free academy construction prepares research");
        for (int level = 10; level < 15; level++)
            if (BuildingRules.UpgradePlot(Fief(world), 0).Code != GameCodes.Ok) throw new InvalidOperationException("Original hall upgrade failed");
        for (int level = 1; level < 15; level++)
            if (BuildingRules.UpgradePlot(Fief(world), 1).Code != GameCodes.Ok) throw new InvalidOperationException("Original academy upgrade failed");
        var firstCosts = new double[] { 20000, 20000, 2400, 22000, 2400, 2400, 28000, 25000, 25000, 20000, 20000, 20000, 20000, 30000, 4000, 50000, 50000, 50000, 50000, 50000, 50000 };
        for (int index = 0; index < TechnologyRules.Count; index++)
        {
            Check(TechnologyRules.UpgradeCost(index, 0) == firstCosts[index], "original level-zero fee " + TechnologyRules.NameAt(index));
            var player = (JObject)Player(world).DeepClone(); var fief = (JObject)player["封地信息表"][0];
            string name = TechnologyRules.NameAt(index), currency = TechnologyRules.Currency(index);
            string otherTechnology = index == 0 ? "行军技巧" : "工程设计";
            int maximum = TechnologyRules.Maximum(index);
            for (int level = 0; level < maximum; level++)
            {
                double cost = TechnologyRules.UpgradeCost(index, level);
                player["财产信息"][currency] = cost;
                string beforeOther = Json(player["科技信息"][otherTechnology]);
                Check(TechnologyRules.Upgrade(player, fief, 1, name, level).Code == GameCodes.Ok &&
                    player["科技信息"].Value<double>(name) == level + 1 && player["财产信息"].Value<double>(currency) == 0 &&
                    beforeOther == Json(player["科技信息"][otherTechnology]), "original exact-balance instant upgrade " + name + " " + level);
            }
            string before = Json(player);
            Check(TechnologyRules.Upgrade(player, fief, 1, name, maximum).Code == GameCodes.Conflict && before == Json(player), "original technology maximum rejects without spending " + name);
        }
        Check(TechnologyRules.UpgradeCost(0, 10) == 15000000 && TechnologyRules.UpgradeCost(0, 14) == 15000000,
            "original engineering fifteen-level progression keeps fifteen-million fee cap");
        var boundary = (JObject)Player(world).DeepClone(); var academy = (JObject)boundary["封地信息表"][0];
        academy["建筑信息表"][1]["等级"] = 10;
        boundary["科技信息"]["种植技术"] = 9.0; boundary["财产信息"]["铜钱"] = 20000000.0;
        Check(TechnologyRules.Upgrade(boundary, academy, 1, "种植技术", 9).Code == GameCodes.Ok, "academy ten permits original ordinary technology nine-to-ten");
        boundary["科技信息"]["仓储"] = 0.0; boundary["财产信息"]["黄金"] = 50000.0;
        string insufficientAcademy = Json(boundary);
        Check(TechnologyRules.Upgrade(boundary, academy, 1, "仓储", 0).Code == GameCodes.Conflict && insufficientAcademy == Json(boundary),
            "academy ten cannot research any original advanced technology");
        academy["建筑信息表"][1]["等级"] = 11;
        Check(TechnologyRules.Upgrade(boundary, academy, 1, "仓储", 0).Code == GameCodes.Ok && boundary["财产信息"].Value<double>("黄金") == 0,
            "academy eleven upgrades advanced technology once with original gold fee");
        var wrongBuilding = (JObject)Player(world).DeepClone(); var wrongFief = (JObject)wrongBuilding["封地信息表"][0];
        string wrongBefore = Json(wrongBuilding);
        Check(TechnologyRules.Upgrade(wrongBuilding, wrongFief, 0, "工程设计", 0).Code == GameCodes.InvalidArgument &&
            TechnologyRules.Upgrade(wrongBuilding, wrongFief, 2, "工程设计", 0).Code == GameCodes.Conflict && wrongBefore == Json(wrongBuilding), "hall or empty plot cannot act as an academy");
        var poor = (JObject)Player(world).DeepClone(); poor["财产信息"]["铜钱"] = 19999.0;
        string poorBefore = Json(poor);
        Check(TechnologyRules.Upgrade(poor, (JObject)poor["封地信息表"][0], 1, "工程设计", 0).Code == GameCodes.InsufficientFunds && poorBefore == Json(poor),
            "original one-short copper failure leaves all role data unchanged");

        Player(world)["财产信息"]["铜钱"] = 50000000.0;
        Player(world)["财产信息"]["黄金"] = 5000000.0;
        string fiefId = ((JObject)world.EntityMappings["fiefs"]).Properties().Single(p => p.Value.Value<string>("playerId") == owner && p.Value.Value<int>("legacyId") == 1).Name;
        string otherFief = ((JObject)world.EntityMappings["fiefs"]).Properties().Single(p => p.Value.Value<string>("playerId") == bindings[1].PlayerId && p.Value.Value<int>("legacyId") == 1).Name;
        var actor = new AuthenticatedActor(bindings[0].AccountId, owner, world.WorldId, "actual-academy-check");
        GameCommand Command(string technology = "工程设计", int level = 0) => new GameCommand { WorldId = world.WorldId, RequestId = Guid.NewGuid().ToString("N"),
            Type = "technology.upgrade", Payload = new JObject { ["fiefId"] = fiefId, ["plot"] = 1, ["technology"] = technology, ["expectedLevel"] = level } };
        var durable = Command(); string receipt, storedData, storedMaps;
        using (var store = new SqliteWorldStore(database))
        {
            store.ImportWorld(world, bindings);
            var runtime = new WorldRuntime(store, a => a.IsSystem || store.ResolveRole(a.WorldId, a.AccountId)?.PlayerId == a.PlayerId);
            runtime.Register(new TechnologyModule());
            string nations = Json(world.Data["国家列表"]), otherPlayer = Json(world.RequirePlayer(bindings[1].PlayerId));
            var result = runtime.Execute(actor, durable); receipt = JsonConvert.SerializeObject(result);
            Check(result.Code == GameCodes.Ok && result.Data.Value<double>("cost") == 20000 && result.Events.Single().AudiencePlayerIds.SequenceEqual(new[] { owner }) &&
                Player(store.Load(world.WorldId))["科技信息"].Value<double>("工程设计") == 1, "actual Runtime SQLite privately commits original academy upgrade and fee together");
            Check(Json(store.Load(world.WorldId).Data["国家列表"]) == nations && Json(store.Load(world.WorldId).RequirePlayer(bindings[1].PlayerId)) == otherPlayer,
                "personal research changes no national technology treasury or other role");
            string after = Json(store.Load(world.WorldId).Data);
            Check(receipt == JsonConvert.SerializeObject(runtime.Execute(actor, durable)) && after == Json(store.Load(world.WorldId).Data), "same actual request replays without another technology level or fee");
            Check(runtime.Execute(actor, Command()).Code == GameCodes.Conflict && after == Json(store.Load(world.WorldId).Data), "new request with stale expected level cannot charge another upgrade");
            var foreign = Command("种植技术"); foreign.Payload["fiefId"] = otherFief;
            Check(runtime.Execute(actor, foreign).Code == GameCodes.Forbidden && after == Json(store.Load(world.WorldId).Data), "other role's real stable fief cannot authorize research");
            foreach (var injected in new[] { "playerId", "cost", "currency", "academyLevel", "国家" })
            {
                var forged = Command("种植技术"); forged.Payload[injected] = bindings[1].PlayerId;
                Check(runtime.Execute(actor, forged).Code == GameCodes.InvalidArgument && after == Json(store.Load(world.WorldId).Data), "injected research authority rejected " + injected);
            }
            var large = Command("种植技术"); large.Payload["expectedLevel"] = JToken.Parse("9223372036854775808");
            Check(runtime.Execute(actor, large).Code == GameCodes.InvalidArgument, "oversized JSON research level rejected");
            var noAcademy = Command("种植技术"); noAcademy.Payload["plot"] = 2;
            Check(runtime.Execute(actor, noAcademy).Code == GameCodes.Conflict && after == Json(store.Load(world.WorldId).Data), "actual empty plot cannot buy a technology level");
            Check(runtime.Execute(actor, Command("攻击科技")).Code == GameCodes.NotFound && after == Json(store.Load(world.WorldId).Data), "country technology cannot enter the fixed twenty-one personal fields");
            var concurrent = Enumerable.Range(0, 2).Select(_ => Task.Run(() => runtime.Execute(actor, Command("种植技术")))).ToArray(); Task.WaitAll(concurrent);
            var saved = store.Load(world.WorldId);
            Check(concurrent.Count(t => t.Result.Code == GameCodes.Ok) == 1 && concurrent.Count(t => t.Result.Code == GameCodes.Conflict) == 1 &&
                Player(saved)["科技信息"].Value<double>("种植技术") == 1 && Player(saved)["财产信息"].Value<double>("铜钱") == 50000000 - 20000 - 2400,
                "actual concurrent stale intents research once at the original fee");
            storedData = Json(saved.Data); storedMaps = Json(saved.EntityMappings);
        }
        using (var store = new SqliteWorldStore(database))
        {
            var runtime = new WorldRuntime(store, a => store.ResolveRole(a.WorldId, a.AccountId)?.PlayerId == a.PlayerId);
            runtime.Register(new TechnologyModule());
            Check(receipt == JsonConvert.SerializeObject(runtime.Execute(actor, durable)) && storedData == Json(store.Load(world.WorldId).Data) && storedMaps == Json(store.Load(world.WorldId).EntityMappings),
                "actual reopened database restores personal research cost receipt and stable identities");
            Check(runtime.Execute(actor, Command("工程设计", 1)).Code == GameCodes.Ok && Player(store.Load(world.WorldId))["科技信息"].Value<double>("工程设计") == 2,
                "real restored academy permits the next original upgrade at the new server level");
            var active = store.Load(world.WorldId);
            var generals = new GeneralsModule(); long utc = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            GameResult GeneralAction(string type, JObject payload) => generals.Execute(active, new CommandContext(actor, utc),
                new GameCommand { WorldId = world.WorldId, RequestId = Guid.NewGuid().ToString("N"), Type = type, Payload = payload });
            var tavern = GeneralAction("generals.refreshTavern", new JObject { ["refreshType"] = 0 });
            Check(tavern.Code == GameCodes.Ok, "actual original free tavern supplies a real general for leadership integration");
            string generalId = tavern.Data["tavern"]["candidates"][0].Value<string>("id");
            Check(GeneralAction("generals.recruit", new JObject { ["candidateId"] = generalId, ["fiefId"] = fiefId }).Code == GameCodes.Ok &&
                GeneralAction("generals.healWounded", new JObject { ["fiefId"] = fiefId, ["troopTypeId"] = 104, ["count"] = 1 }).Code == GameCodes.Ok &&
                GeneralAction("generals.allocateTroops", new JObject { ["generalId"] = generalId, ["troopTypeId"] = 104, ["count"] = 1 }).Code == GameCodes.Ok,
                "actual recruitment recovery and allocation use only the original one wounded soldier");
            Check(GeneralsModule.TryOccupy(active, owner, "actual-leadership-army", new[] { generalId }, out var army).Code == GameCodes.Ok,
                "real occupied original general prepares research without an invented state fixture");
            var beforeGeneral = (JObject)army[0].DeepClone();
            var expected = (JObject)Player(active).DeepClone(); expected["科技信息"]["统帅能力"] = 1.0;
            GeneralAttributeRules.Recalculate(expected, utc / 1000);
            var expectedCapacity = expected["封地信息表"][0]["将领信息表"][0]["将领属性"]["最终属性"]["统兵"];
            Check(new TechnologyModule().Execute(active, new CommandContext(actor, utc), Command("统帅能力")).Code == GameCodes.Ok &&
                JToken.DeepEquals(Player(active)["封地信息表"][0]["将领信息表"][0]["将领属性"]["最终属性"]["统兵"], expectedCapacity),
                "personal leadership uses actual canonical derived capacity while the army remains occupied");
            var afterGeneral = (JObject)Player(active)["封地信息表"][0]["将领信息表"][0].DeepClone();
            afterGeneral["将领属性"]["最终属性"]["统兵"] = beforeGeneral["将领属性"]["最终属性"]["统兵"].DeepClone();
            Check(JToken.DeepEquals(beforeGeneral, afterGeneral) && active.EntityMappings["generalOccupancy"][generalId].Value<string>("armyId") == "actual-leadership-army",
                "leadership upgrade grants no free stamina and alters no troop state formation equipment or army binding");
            var abnormal = active.Clone();
            abnormal.Revision = 0;
            abnormal.RequirePlayer(owner)["封地信息表"][0]["将领信息表"][0]["将领属性"]["初始属性"]["武力"] = 1501.0;
            Check(GeneralAttributeRules.Recalculate((JObject)abnormal.RequirePlayer(owner).DeepClone(), utc / 1000),
                "real recruited occupied general triggers the original abnormal-attribute boundary");
            using (var invalidStore = new SqliteWorldStore(database + ".badattributes"))
            {
                invalidStore.ImportWorld(abnormal, bindings);
                var invalidRuntime = new WorldRuntime(invalidStore, a => invalidStore.ResolveRole(a.WorldId, a.AccountId)?.PlayerId == a.PlayerId);
                invalidRuntime.Register(new TechnologyModule());
                var beforeInvalid = invalidStore.Load(world.WorldId);
                var rejected = invalidRuntime.Execute(actor, Command("统帅能力", 1));
                var afterInvalid = invalidStore.Load(world.WorldId);
                Check(rejected.Code == GameCodes.Unavailable && beforeInvalid.Revision == afterInvalid.Revision &&
                    Json(beforeInvalid.Data) == Json(afterInvalid.Data) && Json(beforeInvalid.EntityMappings) == Json(afterInvalid.EntityMappings),
                    "actual Runtime SQLite rejects abnormal derived attributes without spending changing research or altering the occupied army");
            }
        }
        Console.WriteLine("TECHNOLOGY_CHECKS_PASS " + checks);
    }
}
