using Dwsg.Persistence;
using Dwsg.Runtime;
using Dwsg.Server.Economy;
using Dwsg.Server.Modules.Generals;
using Dwsg.Server.World;
using Dwsg.Shared;
using Dwsg.Shared.Economy;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

internal static class CityVictoryChecks
{
    public static int Run(WorldState original, List<RoleBinding> bindings, string database)
    {
        int checks = 0;
        void Check(bool success, string name)
        {
            if (!success) throw new InvalidOperationException(name);
            checks++; Console.WriteLine("PASS " + name);
        }
        string Json(JToken value) => value.ToString(Formatting.None);
        string ownerId = bindings[0].PlayerId;
        string Id(WorldState state, int index) => ((JObject)state.EntityMappings["players"]).Properties().Single(p => p.Value.Value<int>() == index).Name;
        JObject Player(WorldState state, int index) => (JObject)state.Data["玩家列表"][index];
        JObject First(WorldState state, int index) => (JObject)Player(state, index)["封地信息表"][0];
        JObject Nation(WorldState state) => state.Data["国家列表"].OfType<JObject>().First(n => n.Value<string>("国号") != "汉");
        JObject Capital(WorldState state) => TerritoryRules.City(state, Nation(state).Value<int>("国都x"), Nation(state).Value<int>("国都y"));
        Func<int, int, int> Random(WorldState state) => (minimum, maximum) =>
        {
            state.Data["审计城池随机调用"] = state.Data.Value<int>("审计城池随机调用") + 1;
            return minimum;
        };
        GameResult Capture(WorldState state, JObject city, string battleId) => LegacyWorldModule.ApplyCityVictory(state, ownerId, city.Value<int>("坐标x"), city.Value<int>("坐标y"), battleId, Random(state));
        void MoveFirst(WorldState state, int index, JObject city)
        {
            if (TerritoryRules.MoveOriginalFief(state, index, First(state, index).Value<int>("ID"), city.Value<int>("坐标x"), city.Value<int>("坐标y")).Code != GameCodes.Ok)
                throw new InvalidOperationException("Actual original fief fixture relocation failed.");
        }
        bool CacheMatches(WorldState state) => state.Data["国家列表"].OfType<JObject>().All(n => JToken.DeepEquals(n["城池列表"],
            new JArray(state.Data["城池列表"].OfType<JObject>().Where(c => c.Value<string>("国家") == n.Value<string>("国号"))
                .Select(c => new JObject { ["x"] = c["坐标x"].DeepClone(), ["y"] = c["坐标y"].DeepClone() }))));

        var ordinary = original.Clone();
        string formerName = Nation(ordinary).Value<string>("国号");
        var cities = ordinary.Data["城池列表"].OfType<JObject>().ToArray();
        var town = cities.Last(c => c.Value<string>("国家") == formerName && c.Value<int>("规模") != 4);
        int kingIndex = Nation(ordinary).Value<int>("国王");
        var residentIndices = new[] { bindings[0].LegacyPlayerIndex, bindings[1].LegacyPlayerIndex, kingIndex };
        foreach (int index in residentIndices) MoveFirst(ordinary, index, town);
        var fiefContents = residentIndices.ToDictionary(index => index, index => Json(First(ordinary, index)));
        string maps = Json(ordinary.EntityMappings);
        town["城墙"] = 0.0; town["道路"] = 123.0; town["炮塔"] = 456.0;
        string townBattle = Guid.NewGuid().ToString("N");
        var destinations = new Queue<int>(town["城池封地列表"].Select(resident =>
        {
            string country = Player(ordinary, resident.Value<int>("第几个玩家"))["基础信息"].Value<string>("国家");
            var available = cities.Where(c => (ReferenceEquals(c, town) ? "汉" : c.Value<string>("国家")) == country).ToArray();
            return Array.FindIndex(available, c => !ReferenceEquals(c, town));
        }));
        var captured = LegacyWorldModule.ApplyCityVictory(ordinary, ownerId, town.Value<int>("坐标x"), town.Value<int>("坐标y"), townBattle,
            (_, _) => destinations.Dequeue());
        Check(captured.Code == GameCodes.Ok && town.Value<int>("城主") == bindings[0].LegacyPlayerIndex && town.Value<string>("国家") == "汉" &&
            captured.Data.Value<int>("movedFiefCount") == 3 && !town["城池封地列表"].Any(), "ordinary capture relocates every original resident without mutation skips");
        Check(town.Value<double>("城墙") == 0 && town.Value<double>("道路") == 123 && town.Value<double>("炮塔") == 456,
            "ordinary capture leaves walls and other combat settlement to Combat");
        Check(residentIndices.All(index =>
        {
            var fief = First(ordinary, index); var location = fief["所在城池"];
            var target = TerritoryRules.City(ordinary, location.Value<int>("x"), location.Value<int>("y"));
            return target.Value<string>("国家") == Player(ordinary, index)["基础信息"].Value<string>("国家") &&
                target["城池封地列表"].Count(r => r.Value<int>("第几个玩家") == index && r.Value<int>("封地ID标识") == fief.Value<int>("ID")) == 1;
        }), "all relocated original fiefs agree with their destination city registration");
        Check(residentIndices.All(index =>
        {
            var copy = (JObject)First(ordinary, index).DeepClone(); copy["所在城池"] = JObject.Parse(fiefContents[index])["所在城池"].DeepClone();
            return fiefContents[index] == Json(copy);
        }) && maps == Json(ordinary.EntityMappings), "ordinary relocation preserves generals troops buildings and all stable identities");
        Check(CacheMatches(ordinary), "original nation city caches follow captured ownership");
        string replayBefore = Json(ordinary.Data);
        Check(LegacyWorldModule.ApplyCityVictory(ordinary, ownerId, town.Value<int>("坐标x"), town.Value<int>("坐标y"), townBattle,
            (_, _) => throw new InvalidOperationException("Replay drew random again")).Code == GameCodes.Ok && replayBefore == Json(ordinary.Data), "same trusted battle capture is idempotent without random or another migration");
        Check(LegacyWorldModule.ApplyCityVictory(ordinary, bindings[1].PlayerId, town.Value<int>("坐标x"), town.Value<int>("坐标y"), townBattle, Random(ordinary)).Code == GameCodes.Conflict &&
            replayBefore == Json(ordinary.Data), "capture ledger rejects reassignment of a settled battle");

        var migration = original.Clone(); var oldCapital = Capital(migration); var nation = Nation(migration);
        formerName = nation.Value<string>("国号"); kingIndex = nation.Value<int>("国王");
        foreach (var binding in bindings) MoveFirst(migration, binding.LegacyPlayerIndex, oldCapital);
        int residents = oldCapital["城池封地列表"].Count();
        maps = Json(migration.EntityMappings);
        var migrated = Capture(migration, oldCapital, Guid.NewGuid().ToString("N"));
        var nextCapital = TerritoryRules.City(migration, nation.Value<int>("国都x"), nation.Value<int>("国都y"));
        Check(migrated.Code == GameCodes.Ok && !ReferenceEquals(oldCapital, nextCapital) && !oldCapital["城池封地列表"].Any() &&
            migrated.Data.Value<int>("movedFiefCount") == residents && TerritoryRules.Nation(migration, formerName) != null,
            "lost capital with remaining original cities migrates every resident and keeps its nation");
        Check(oldCapital.Value<int>("规模") == 0 && oldCapital.Value<double>("城墙") == 200000 && oldCapital.Value<int>("战功") == 500 &&
            oldCapital.Value<double>("协防几率") == 10 && oldCapital.Value<double>("协防数量f") == 1 && oldCapital.Value<double>("协防数量m") == 7 &&
            oldCapital.Value<double>("城主征收_铜") == 20000 && oldCapital.Value<double>("国家征收_粮") == 40000,
            "captured capital uses original village walls reward support and taxation");
        Check(nextCapital.Value<int>("规模") == 4 && nextCapital.Value<double>("城墙") == 2000000 && nextCapital.Value<int>("战功") == 10000 &&
            nextCapital.Value<int>("城主") == kingIndex && nextCapital.Value<double>("协防几率") == 100 && nextCapital.Value<double>("协防数量f") == 50 &&
            nextCapital.Value<double>("协防数量m") == 100 && nextCapital.Value<double>("城主征收_铜") == 0 && nextCapital.Value<double>("国家征收_粮") == 0,
            "replacement capital restores original king scale walls support and zero taxes");
        Check(residentIndices.All(index => First(migration, index)["所在城池"].Value<int>("x") == nextCapital.Value<int>("坐标x") &&
            First(migration, index)["所在城池"].Value<int>("y") == nextCapital.Value<int>("坐标y")) && maps == Json(migration.EntityMappings) && CacheMatches(migration),
            "capital migration keeps stable fiefs and synchronized original city coordinates");

        var extinction = original.Clone(); nation = Nation(extinction); oldCapital = Capital(extinction);
        formerName = nation.Value<string>("国号"); kingIndex = nation.Value<int>("国王");
        foreach (var city in extinction.Data["城池列表"].OfType<JObject>().Where(c => c.Value<string>("国家") == formerName && !ReferenceEquals(c, oldCapital))) city["国家"] = "汉";
        foreach (var binding in bindings) MoveFirst(extinction, binding.LegacyPlayerIndex, oldCapital);
        string kingId = Id(extinction, kingIndex), neutralId = Id(extinction, 2);
        var kingGenerals = First(extinction, kingIndex)["将领信息表"].OfType<JObject>().ToArray();
        var sourceIds = kingGenerals.Select(g => ((JObject)extinction.EntityMappings["generals"]).Properties()
            .Single(p => p.Value.Value<string>("playerId") == kingId && p.Value.Value<int>("legacyId") == g.Value<int>("ID")).Name).ToArray();
        string[] sourceTroops = kingGenerals.Select(g => Json(g["将领配兵"])).ToArray();
        int neutralCount = First(extinction, 2)["将领信息表"].Count(), neutralNext = Player(extinction, 2).Value<int>("将领ID标识");
        string stocks = Json(First(extinction, kingIndex)["闲兵信息表"]) + Json(First(extinction, kingIndex)["伤兵信息表"]);
        int guards = extinction.Data["城池列表"].Sum(c => c["城池驻防列表"].Count());
        var guardCounts = extinction.Data["城池列表"].Select(c => c["城池驻防列表"].Count()).ToArray();
        string battle = Guid.NewGuid().ToString("N");
        Check(GeneralsModule.TryOccupy(extinction, kingId, battle, new[] { sourceIds[0] }, out var occupied).Code == GameCodes.Ok, "actual Generals occupies an original king defender");
        string blockedData = Json(extinction.Data), blockedMaps = Json(extinction.EntityMappings);
        Check(Capture(extinction, oldCapital, battle).Code == GameCodes.Conflict && blockedData == Json(extinction.Data) && blockedMaps == Json(extinction.EntityMappings),
            "extinction refuses any live defender occupancy and restores ownership nation reward and random state");
        var defender = occupied[0];
        Check(GeneralsModule.ApplyCityDefenderOutcome(extinction, kingId, battle, new[] { new GeneralOutcome { GeneralId = sourceIds[0],
            General = (JObject)defender.DeepClone(), Remaining = defender["将领配兵"].Value<int>("数量"), Wounded = 0 } }).Code == GameCodes.Ok,
            "actual city defender outcome releases its reservation before extinction");
        oldCapital = Capital(extinction);
        var defeated = Capture(extinction, oldCapital, battle);
        var neutral = First(extinction, 2)["将领信息表"].OfType<JObject>().ToArray();
        Check(defeated.Code == GameCodes.Ok && defeated.Data.Value<string>("destroyedNation") == formerName && TerritoryRules.Nation(extinction, formerName) == null &&
            Player(extinction, kingIndex)["基础信息"].Value<string>("国家") == formerName && !First(extinction, kingIndex)["将领信息表"].Any(),
            "last original city destroys nation without inventing a replacement country for its members");
        Check(neutral.Length == neutralCount + sourceIds.Length && Player(extinction, 2).Value<int>("将领ID标识") == neutralNext + sourceIds.Length &&
            sourceIds.Select((id, index) => new { id, index }).All(row => extinction.EntityMappings["generals"][row.id].Value<string>("playerId") == neutralId &&
                extinction.EntityMappings["generals"][row.id].Value<int>("legacyId") == neutralNext + sourceIds.Length - 1 - row.index &&
                sourceTroops[row.index] == Json(neutral.Single(g => g.Value<int>("ID") == neutralNext + sourceIds.Length - 1 - row.index)["将领配兵"])),
            "real frozen transfer preserves stable generals and troops in original reverse order beyond NPC roster cap");
        Check(stocks == Json(First(extinction, kingIndex)["闲兵信息表"]) + Json(First(extinction, kingIndex)["伤兵信息表"]) &&
            First(extinction, kingIndex)["所在城池"].Value<int>("x") == oldCapital.Value<int>("坐标x"), "extinction leaves original fiefs and troop pools in place without returning soldiers");
        var attackerItems = (JArray)extinction.RequirePlayer(ownerId)["背包道具列表"]["宝物道具列表"];
        Check(attackerItems.Where(i => i.Value<string>("名字") == "玉玺").Sum(i => i.Value<double>("数量")) == 1 &&
            Player(extinction, 0)["背包道具列表"]["宝物道具列表"].All(i => i.Value<string>("名字") != "玉玺"), "original extinction seal goes once to authenticated attacker instead of local player zero");
        var addedGuards = extinction.Data["城池列表"].SelectMany((c, index) => c["城池驻防列表"].Skip(guardCounts[index])).ToArray();
        Check(extinction.Data["城池列表"].Sum(c => c["城池驻防列表"].Count()) == guards + neutral.Length &&
            addedGuards.All(g => g.Value<int>("第几个玩家") == 2) && addedGuards.Select(g => g.Value<int>("将领ID标识")).OrderBy(id => id)
                .SequenceEqual(neutral.Select(n => n["将领属性"]["初始属性"].Value<int>("ID")).OrderBy(id => id)),
            "all original neutral garrison additions keep configuration IDs used by original name lookup");
        Check(extinction.Data.Value<int>("审计城池随机调用") == 1 + neutral.Length && CacheMatches(extinction),
            "candidate-bound random state survives full-document general transfer and original nation cache update");
        replayBefore = Json(extinction.Data); maps = Json(extinction.EntityMappings);
        Check(Capture(extinction, TerritoryRules.City(extinction, oldCapital.Value<int>("坐标x"), oldCapital.Value<int>("坐标y")), battle).Code == GameCodes.Ok &&
            replayBefore == Json(extinction.Data) && maps == Json(extinction.EntityMappings), "extinction replay adds no seals guards transfers or random draws");

        var departed = original.Clone(); oldCapital = Capital(departed); nation = Nation(departed);
        formerName = nation.Value<string>("国号"); kingIndex = nation.Value<int>("国王"); nation.Remove();
        var orphanTown = departed.Data["城池列表"].OfType<JObject>().First(c => c.Value<string>("国家") == formerName && c.Value<int>("规模") != 4);
        MoveFirst(departed, kingIndex, orphanTown);
        Check(Capture(departed, orphanTown, Guid.NewGuid().ToString("N")).Code == GameCodes.Ok &&
            First(departed, kingIndex)["所在城池"].Value<int>("x") == orphanTown.Value<int>("坐标x"), "original nationless resident remains instead of inventing a forced destination");
        var invalid = original.Clone(); string invalidBefore = Json(invalid.Data);
        Check(LegacyWorldModule.ApplyCityVictory(invalid, ownerId, int.MaxValue, 1, Guid.NewGuid().ToString("N"), Random(invalid)).Code == GameCodes.NotFound && invalidBefore == Json(invalid.Data),
            "missing real city cannot create an invented capture");
        Check(new TerritoryModule(GeneralsModule.EnsureMappings).CommandTypes.SequenceEqual(new[] { "fief.create" }), "public territory module exposes no trusted move or client victory command");
        var broken = original.Clone();
        var brokenTown = broken.Data["城池列表"].OfType<JObject>().First(c => c.Value<string>("国家") == formerName && c.Value<int>("规模") != 4);
        MoveFirst(broken, bindings[0].LegacyPlayerIndex, brokenTown);
        brokenTown["城池封地列表"][0]["第几个玩家"] = int.MaxValue;
        string brokenBefore = Json(broken.Data), brokenMappings = Json(broken.EntityMappings);
        Check(Capture(broken, brokenTown, Guid.NewGuid().ToString("N")).Code == GameCodes.Unavailable &&
            brokenBefore == Json(broken.Data) && brokenMappings == Json(broken.EntityMappings), "malformed original resident index rolls back the entire capture candidate");

        // This adapter tests the real helper's transaction boundary, not a substitute combat kernel.
        var persisted = original.Clone(); oldCapital = Capital(persisted); nation = Nation(persisted); formerName = nation.Value<string>("国号");
        foreach (var city in persisted.Data["城池列表"].OfType<JObject>().Where(c => c.Value<string>("国家") == formerName && !ReferenceEquals(c, oldCapital))) city["国家"] = "汉";
        int capitalX = oldCapital.Value<int>("坐标x"), capitalY = oldCapital.Value<int>("坐标y");
        battle = Guid.NewGuid().ToString("N");
        var system = AuthenticatedActor.System(persisted.WorldId);
        var command = new GameCommand { WorldId = persisted.WorldId, RequestId = Guid.NewGuid().ToString("N"), Type = "audit.cityVictory" };
        string receipt, persistedData, persistedMaps;
        using (var store = new SqliteWorldStore(database))
        {
            store.ImportWorld(persisted, bindings);
            var runtime = new WorldRuntime(store, actor => actor.IsSystem || store.ResolveRole(actor.WorldId, actor.AccountId)?.PlayerId == actor.PlayerId);
            runtime.Register(new TransactionProbe(ownerId, capitalX, capitalY, battle));
            var result = runtime.Execute(system, command); receipt = JsonConvert.SerializeObject(result);
            Check(result.Code == GameCodes.Ok && TerritoryRules.Nation(store.Load(persisted.WorldId), formerName) == null,
                "actual Runtime SQLite commits complete trusted city helper and general transfer together");
            persistedData = Json(store.Load(persisted.WorldId).Data); persistedMaps = Json(store.Load(persisted.WorldId).EntityMappings);
            Check(receipt == JsonConvert.SerializeObject(runtime.Execute(system, command)) && persistedData == Json(store.Load(persisted.WorldId).Data),
                "actual trusted transaction receipt prevents duplicate extinction rewards");
        }
        using (var store = new SqliteWorldStore(database))
        {
            var runtime = new WorldRuntime(store, actor => actor.IsSystem);
            runtime.Register(new TransactionProbe(ownerId, capitalX, capitalY, battle));
            Check(receipt == JsonConvert.SerializeObject(runtime.Execute(system, command)) && persistedData == Json(store.Load(persisted.WorldId).Data) &&
                persistedMaps == Json(store.Load(persisted.WorldId).EntityMappings), "reopened actual database restores extinction receipt ownership guards seal and stable general identities");
        }
        Console.WriteLine("CITY_VICTORY_CHECKS_PASS " + checks);
        return checks;
    }

    private sealed class TransactionProbe : IGameModule
    {
        private readonly string owner, battle; private readonly int x, y;
        public TransactionProbe(string owner, int x, int y, string battle) { this.owner = owner; this.x = x; this.y = y; this.battle = battle; }
        public IReadOnlyCollection<string> CommandTypes => new[] { "audit.cityVictory" };
        public GameResult Execute(WorldState candidate, CommandContext context, GameCommand command)
        {
            if (!context.Actor.IsSystem) return GameResult.Reject(GameCodes.Forbidden, "Audit transaction probe is server-only.");
            return LegacyWorldModule.ApplyCityVictory(candidate, owner, x, y, battle, (minimum, maximum) => minimum);
        }
    }
}
