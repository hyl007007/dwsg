using Dwsg.Persistence;
using Dwsg.Runtime;
using Dwsg.Server.Modules.Combat;
using Dwsg.Server.Modules.Generals;
using Dwsg.Server.World;
using Dwsg.Shared;
using Dwsg.Shared.Combat;
using Dwsg.Shared.Generals;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.Data.Sqlite;

internal static class GarrisonChecks
{
    public static void Run(string fixturePath, string outputPath)
    {
        string directory = Path.GetFullPath(outputPath);
        if (!directory.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Contains("audit", StringComparer.OrdinalIgnoreCase)) throw new ArgumentException("Checks stay in audit.");
        directory = Path.Combine(directory, Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
        JObject data = JObject.Parse(File.ReadAllText(fixturePath));
        WorldState world;
        if (data["Data"] != null) world = data.ToObject<WorldState>();
        else
        {
            // 旧129夹具导出的是Data；同一隔离DB保留原稳定ID映射，从真实已保存unit恢复原占用。
            using var source = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = Path.Combine(Path.GetDirectoryName(fixturePath), "world.sqlite3"), Mode = SqliteOpenMode.ReadOnly }.ToString());
            source.Open(); using var query = source.CreateCommand(); query.CommandText = "SELECT world_id,mappings_json FROM worlds";
            using var row = query.ExecuteReader(); if (!row.Read()) throw new InvalidOperationException("Actual fixture mappings missing.");
            world = new WorldState { WorldId = row.GetString(0), Data = data, EntityMappings = JObject.Parse(row.GetString(1)) };
            var occupancy = new JObject(); world.EntityMappings["generalOccupancy"] = occupancy;
            foreach (BanditBattle battle in ((JObject)data["战斗运行"]).Properties().Select(entry => entry.Value.ToObject<BanditBattle>()).Where(battle => !battle.SettlementApplied))
                foreach (CombatUnit unit in battle.Attackers.Concat(battle.Defenders).Where(unit => !unit.Ephemeral && !unit.Retired))
                    occupancy[unit.GeneralId] = new JObject { ["playerId"] = unit.GeneralOwnerId ?? battle.PlayerId, ["armyId"] = unit.Side == 1 ? battle.BattleId : unit.ArmyId };
        }
        BanditBattle original = ((JObject)world.Data["战斗运行"]).Properties().Select(entry => entry.Value.ToObject<BanditBattle>()).Single(battle => battle.Kind == "city" && battle.Phase == "fighting");
        long now = original.StartedUtcMs + 1; int checks = 0;
        void Check(bool condition, string name) { if (!condition) throw new InvalidOperationException(name); checks++; Console.WriteLine("PASS " + name); }
        var actors = new List<AuthenticatedActor> { new AuthenticatedActor("garrison-attack", original.PlayerId, world.WorldId, "attack-connection") };
        var bindings = new List<RoleBinding> { new RoleBinding { WorldId = world.WorldId, AccountId = actors[0].AccountId, PlayerId = actors[0].PlayerId, LegacyPlayerIndex = world.ResolvePlayerIndex(actors[0].PlayerId) } };
        var random = new CombatRandom(22334455);
        // 原大型城池夹具主攻已有五将；通过原生成/配兵/增援补足其余队列，不改任何NPC或城墙。
        JObject attackerPlayer = world.RequirePlayer(original.PlayerId), attackerFief = (JObject)attackerPlayer["封地信息表"][0];
        for (int number = 6; number <= 25; number++)
        {
            JObject configuration = ((JArray)world.Data["将领配置"]).OfType<JObject>().Single(item => item.Value<int>("ID") == (number % 2 == 0 ? 5 : 6));
            JObject general = GeneralCreationRules.CreateBanditGeneral(configuration, random.Next); general["ID"] = number;
            GeneralExperienceRules.Add(general, GeneralExperienceRules.TotalForLevel(99)); ((JArray)attackerFief["将领信息表"]).Add(general);
        }
        attackerPlayer["将领ID标识"] = 26;
        ((JArray)attackerFief["闲兵信息表"]).OfType<JObject>().Single(pool => pool.Value<int>("ID") == 304)["数量"] = 2000000.0;
        foreach (int troop in new[] { 104, 404 }) ((JArray)attackerFief["闲兵信息表"]).Add(new JObject { ["ID"] = troop, ["数量"] = 500000.0 });
        ((JArray)attackerFief["闲兵信息表"]).Add(new JObject { ["ID"] = 204, ["数量"] = 500000.0 });
        Check(!GeneralAttributeRules.Recalculate(attackerPlayer, now / 1000), "original generated attacker reinforcement stats");
        // 成熟装备由原目录和强化规则生成；不编辑守军、城墙、将领最终属性或战斗伤害。
        foreach (string material in new[] { "冰玉", "仙芝" })
        {
            JObject definition = ((JArray)world.Data["道具配置"]).OfType<JObject>().Single(item => item.Value<string>("名字") == material);
            JArray bag = (JArray)attackerPlayer["背包道具列表"][definition.Value<string>("分类") + "道具列表"];
            JObject stock = bag.OfType<JObject>().SingleOrDefault(item => item.Value<string>("名字") == material);
            if (stock != null) stock["数量"] = 999.0;
            for (int stack = 0; stack < (material == "冰玉" ? 50 : 20); stack++)
                bag.Add(new JObject { ["名字"] = material, ["数量"] = 999.0 });
        }
        string[] equipmentTypes = { "头盔", "武器", "铠甲", "坐骑" };
        for (int slot = 0; slot < 4; slot++)
        for (int number = 1; number <= 25; number++)
        {
            JObject configuration = ((JArray)world.Data["装备配置"]).OfType<JObject>().Where(item => item.Value<string>("类型") == equipmentTypes[slot]).OrderByDescending(item => item.Value<double>("基础值")).First();
            JArray equipment = LegacyGenerals.Equipment(attackerPlayer, slot);
            int index = equipment.Count;
            equipment.Add(new JObject { ["将领ID"] = -1, ["品质"] = 4.0, ["强化等级"] = 0.0, ["强化值"] = 0.0,
                ["保底次数"] = 0.0, ["已强化次数"] = 0.0, ["炼魂属性"] = new JArray(), ["装备信息"] = configuration.DeepClone() });
            for (int upgrade = 0; upgrade < 3; upgrade++) attackerPlayer = EquipmentEnhancementRules.Execute(attackerPlayer, slot, index, 10, (JArray)world.Data["道具配置"], (min, max) => min, now / 1000, out _);
            Check(LegacyGenerals.Equipment(attackerPlayer, slot)[index].Value<double>("强化等级") == 30, "original catalog and enhancement generated equipment " + slot + ":" + number);
        }
        world.Data["玩家列表"][world.ResolvePlayerIndex(original.PlayerId)] = attackerPlayer;
        for (int role = 1; role <= 2; role++)
        {
            Check(LegacyWorldModule.CreatePlayer(world, "驻防君主" + role, role == 1 ? original.CityNation : "汉", now, out int index).Code == GameCodes.Ok, "real original garrison role " + role);
            string playerId = Guid.NewGuid().ToString("N"); world.EntityMappings["players"][playerId] = index;
            var actor = new AuthenticatedActor("garrison-account-" + role, playerId, world.WorldId, "garrison-connection-" + role); actors.Add(actor);
            bindings.Add(new RoleBinding { WorldId = world.WorldId, AccountId = actor.AccountId, PlayerId = playerId, LegacyPlayerIndex = index });
            JObject player = world.RequirePlayer(playerId), fief = (JObject)player["封地信息表"][0];
            string[] advanced = { "仓储", "安置", "格斗", "精准", "驯马", "精工" };
            foreach (JProperty technology in ((JObject)player["科技信息"]).Properties()) technology.Value = technology.Name == "工程设计" ? 15.0 : advanced.Contains(technology.Name) ? 5.0 : 10.0;
            for (int number = 1; number <= 10; number++)
            {
                JObject configuration = ((JArray)world.Data["将领配置"]).OfType<JObject>().Single(item => item.Value<int>("ID") == (number % 2 == 0 ? 5 : 6));
                JObject general = GeneralCreationRules.CreateBanditGeneral(configuration, random.Next); general["ID"] = number;
                GeneralExperienceRules.Add(general, GeneralExperienceRules.TotalForLevel(99)); ((JArray)fief["将领信息表"]).Add(general);
            }
            player["将领ID标识"] = 11;
            Check(!GeneralAttributeRules.Recalculate(player, now / 1000), "garrison original mature stats and attainable technology " + role);
            // 仅隔离夹具预置成熟闲兵库存，派遣数量仍由真实allocate命令和原统兵决定。
            ((JArray)fief["闲兵信息表"]).Add(new JObject { ["ID"] = 304, ["数量"] = 500000.0 });
        }
        GeneralsModule.EnsureMappings(world);
        string Id(int role, int legacy) => ((JObject)world.EntityMappings["generals"]).Properties().Single(entry => entry.Value.Value<string>("playerId") == actors[role].PlayerId && entry.Value.Value<int>("legacyId") == legacy).Name;
        string db = Path.Combine(directory, "world.sqlite3");
        var store = new SqliteWorldStore(db); store.ImportWorld(world, bindings);
        WorldRuntime Runtime() { var runtime = new WorldRuntime(store, actor => actor.IsSystem || actors.Any(real => actor.AccountId == real.AccountId && actor.PlayerId == real.PlayerId && actor.ConnectionId == real.ConnectionId), () => now); runtime.Register(new GeneralsModule()); runtime.Register(new CombatModule()); return runtime; }
        var runtime = Runtime();
        GameCommand Command(string type, JObject payload) => new GameCommand { WorldId = world.WorldId, RequestId = Guid.NewGuid().ToString("N"), Type = type, Payload = payload };
        BanditBattle Battle() => store.Load(world.WorldId).Data["战斗运行"][original.BattleId].ToObject<BanditBattle>();
        JObject Garrison(params int[] numbers) => new JObject { ["x"] = original.X, ["y"] = original.Y, ["generalIds"] = new JArray(numbers.Select(number => Id(1, number))) };
        void Tick() { foreach (var due in new CombatModule().CollectDueCommands(store.Load(world.WorldId), now).ToArray()) { var result = runtime.Execute(AuthenticatedActor.System(world.WorldId), due); if (result.Code != GameCodes.Ok) throw new InvalidOperationException("Garrison Tick rejected " + result.Code + ": " + result.Message); } }
        void Rejected(int role, GameCommand command, string name)
        {
            WorldState before = store.Load(world.WorldId); string data = before.Data.ToString(Formatting.None), map = before.EntityMappings.ToString(Formatting.None);
            Check(runtime.Execute(actors[role], command).Code != GameCodes.Ok, name + " rejected");
            WorldState after = store.Load(world.WorldId); Check(after.Revision == before.Revision && data == after.Data.ToString(Formatting.None) && map == after.EntityMappings.ToString(Formatting.None), name + " whole candidate unchanged");
        }
        for (int number = 6; number <= 25; number++)
        {
            Check(runtime.Execute(actors[0], Command("generals.equipBest", new JObject { ["generalId"] = Id(0, number) })).Code == GameCodes.Ok, "real attacking general equips generated catalog items " + number);
            JObject fief; JObject general = GeneralsModule.ResolveGeneral(store.Load(world.WorldId), actors[0].PlayerId, Id(0, number), out fief);
            int career = general["将领属性"]["初始属性"].Value<int>("职业");
            var allocated = runtime.Execute(actors[0], Command("generals.allocateTroops", new JObject { ["generalId"] = Id(0, number), ["troopTypeId"] = career == 1 ? 104 : career == 4 ? 304 : 204, ["count"] = general["将领属性"]["最终属性"].Value<int>("统兵") }));
            Check(allocated.Code == GameCodes.Ok, "actual attacking army reinforcement allocation " + number + ": " + allocated.Code + " " + allocated.Message);
        }
        for (int number = 6; number <= 21; number += 5)
            Check(runtime.Execute(actors[0], Command("combat.city.reinforce", new JObject { ["battleId"] = original.BattleId, ["generalIds"] = new JArray(Enumerable.Range(number, 5).Select(legacy => Id(0, legacy))) })).Code == GameCodes.Ok, "original five troop reinforcement command " + number);
        for (int number = 1; number <= 5; number++)
        {
            Check(runtime.Execute(actors[0], Command("combat.city.withdraw", new JObject { ["battleId"] = original.BattleId, ["generalId"] = Id(0, number) })).Code == GameCodes.Ok, "original small march general withdraw before real cavalry allocation " + number);
            Check(runtime.Execute(actors[0], Command("generals.equipBest", new JObject { ["generalId"] = Id(0, number) })).Code == GameCodes.Ok, "real existing attacker equips generated catalog items " + number);
            JObject fief; JObject general = GeneralsModule.ResolveGeneral(store.Load(world.WorldId), actors[0].PlayerId, Id(0, number), out fief);
            Check(runtime.Execute(actors[0], Command("generals.allocateTroops", new JObject { ["generalId"] = Id(0, number), ["troopTypeId"] = 104, ["count"] = general["将领属性"]["最终属性"].Value<int>("统兵") })).Code == GameCodes.Ok, "true cavalry allocation for original combined arms " + number);
        }
        Check(runtime.Execute(actors[0], Command("combat.city.reinforce", new JObject { ["battleId"] = original.BattleId, ["generalIds"] = new JArray(Enumerable.Range(1, 5).Select(legacy => Id(0, legacy))) })).Code == GameCodes.Ok, "real existing cavalry formation reinforcement");
        for (int role = 1; role <= 2; role++)
        for (int number = 1; number <= 10; number++)
        {
            JObject fief; JObject general = GeneralsModule.ResolveGeneral(store.Load(world.WorldId), actors[role].PlayerId, Id(role, number), out fief);
            Check(runtime.Execute(actors[role], Command("generals.allocateTroops", new JObject { ["generalId"] = Id(role, number), ["troopTypeId"] = 304, ["count"] = general["将领属性"]["最终属性"].Value<int>("统兵") })).Code == GameCodes.Ok, "real defender allocation " + role + ":" + number);
        }
        Rejected(1, Command("combat.city.garrison.dispatch", Garrison(1, 1)), "duplicate generals");
        var injected = Garrison(1); injected["owner"] = actors[0].PlayerId;
        Rejected(1, Command("combat.city.garrison.dispatch", injected), "injected owner");
        Rejected(2, Command("combat.city.garrison.dispatch", new JObject { ["x"] = original.X, ["y"] = original.Y, ["generalIds"] = new JArray(Id(2, 1)) }), "foreign nation");
        Rejected(1, Command("combat.city.garrison.dispatch", new JObject { ["x"] = original.X, ["y"] = original.Y, ["generalIds"] = new JArray(Id(0, 1)) }), "foreign stable general");
        GameCommand dispatch = Command("combat.city.garrison.dispatch", Garrison(1, 2, 3, 4, 5));
        GameResult started = runtime.Execute(actors[1], dispatch); Check(started.Code == GameCodes.Ok, "ordinary real capital route dispatch: " + started.Message);
        string armyId = started.Data.Value<string>("armyId"); long arrival = started.Data.Value<long>("arrivalUtcMs");
        Check(arrival == CityPlayerGarrisonRules.ArrivalUtcMs(now) && Battle().GarrisonArmies.Single().ArmyId == armyId, "real existing battle binding and original second plus five arrival");
        long revision = store.Load(world.WorldId).Revision;
        Check(JsonConvert.SerializeObject(runtime.Execute(actors[1], dispatch)) == JsonConvert.SerializeObject(started) && store.Load(world.WorldId).Revision == revision, "dispatch receipt replay cannot duplicate army");
        Check(Battle().Defenders.Count(unit => unit.PlayerGarrison && unit.ArmyId == armyId) == 5 && store.Load(world.WorldId).EntityMappings["generalOccupancy"][Id(1, 1)].Value<string>("armyId") == armyId, "real five defenders and own occupancy");
        Check(started.Events.Where(ev => ev.Type == "combat.city.garrison.dispatched").All(ev => ev.AudiencePlayerIds.Count() == 1) && started.Events.Any(ev => ev.AudiencePlayerIds.Contains(actors[1].PlayerId)), "separate owner filtered events");
        JObject saved = (JObject)store.Load(world.WorldId).Data["战斗运行"][original.BattleId];
        JObject attackView = CombatBattleProjection.ProjectBattle(saved, actors[0].PlayerId), defenseView = CombatBattleProjection.ProjectBattle(saved, actors[1].PlayerId);
        Check(((JArray)attackView["Defenders"]).OfType<JObject>().All(unit => !unit.Value<bool>("PlayerGarrison")) && ((JArray)attackView["GarrisonArmies"]).Count == 0, "pending opponent private army hidden");
        Check(defenseView["RandomState"] == null && defenseView["Reward"] == null && defenseView["WarReward"] == null && defenseView["Attackers"][0]["General"]["将领装备"] == null && defenseView["Attackers"][0]["General"]["将领培养"] == null, "defender view hides random and attacker private equipment cultivation rewards");
        Check(CombatBattleProjection.ProjectBattle(saved, actors[2].PlayerId) == null && ((JArray)defenseView["Defenders"]).OfType<JObject>().Any(unit => unit.Value<bool>("PlayerGarrison") && unit["General"]["将领培养"] != null), "only saved participant and own full general");
        Rejected(0, Command("combat.city.garrison.withdraw", new JObject { ["armyId"] = armyId }), "foreign army withdrawal");
        Rejected(1, Command("combat.city.garrison.withdraw", new JObject { ["armyId"] = armyId, ["generalId"] = Id(1, 6) }), "different own army general withdrawal");
        Rejected(1, Command("combat.city.garrison.dispatch", Garrison(1)), "already occupied general");
        now = arrival - 1; Tick(); Check(Battle().GarrisonArmies.Single().Phase == "marching" && Battle().Defenders.Where(unit => unit.PlayerGarrison).All(unit => unit.Slot == -1 && unit.Wounded == 0), "one millisecond early no defense entry or damage");
        now = arrival + 100; Tick(); Check(Battle().GarrisonArmies.Single().Phase == "fighting", "real five second arrival joins current defender");
        store.Dispose(); store = new SqliteWorldStore(db); runtime = Runtime();
        Check(Battle().GarrisonArmies.Single().Phase == "fighting" && store.Load(world.WorldId).EntityMappings["generalOccupancy"][Id(1, 1)].Value<string>("armyId") == armyId, "SQLite reopen keeps real defender army binding");
        for (int step = 0; step < 180 && !Battle().IsTerminal && !Battle().Defenders.Any(unit => unit.PlayerGarrison && unit.Wounded > 0); step++) { now += 1000; Tick(); }
        Check(Battle().Defenders.Any(unit => unit.PlayerGarrison && unit.Wounded > 0), "real city combat damages human defender and records seventy percent original wounded");
        File.WriteAllText(Path.Combine(directory, "wounded-world.json"), JsonConvert.SerializeObject(store.Load(world.WorldId), Formatting.Indented));
        CombatUnit selected = Battle().Defenders.First(unit => unit.PlayerGarrison && !unit.Retired && unit.Wounded > 0);
        double remaining = selected.Remaining, wounded = selected.Wounded;
        GameCommand single = Command("combat.city.garrison.withdraw", new JObject { ["armyId"] = armyId, ["generalId"] = selected.GeneralId });
        Check(runtime.Execute(actors[1], single).Code == GameCodes.Ok, "single real damaged defender withdrawal");
        WorldState current = store.Load(world.WorldId); JObject actualFief;
        JObject canonical = GeneralsModule.ResolveGeneral(current, actors[1].PlayerId, selected.GeneralId, out actualFief);
        Check(canonical["将领配兵"].Value<double>("数量") == remaining && ((JArray)actualFief["伤兵信息表"]).OfType<JObject>().Single(pool => pool.Value<int>("ID") == 304).Value<double>("数量") >= wounded && current.EntityMappings["generalOccupancy"][selected.GeneralId] == null, "withdrawal applies actual casualties and only corresponding binding");
        Check(!Battle().IsTerminal && Battle().Defenders.Any(unit => unit.PlayerGarrison && !unit.Retired), "single defender cannot terminate attacking battle or release other units");
        File.WriteAllText(Path.Combine(directory, "partial-withdraw-world.json"), JsonConvert.SerializeObject(store.Load(world.WorldId), Formatting.Indented));
        GameCommand all = Command("combat.city.garrison.withdraw", new JObject { ["armyId"] = armyId });
        Check(runtime.Execute(actors[1], all).Code == GameCodes.Ok && Battle().GarrisonArmies.Single().Phase == "withdrawn" && !Battle().IsTerminal, "whole defender army withdraws without ending other battle");
        File.WriteAllText(Path.Combine(directory, "defender-withdraw-world.json"), JsonConvert.SerializeObject(store.Load(world.WorldId), Formatting.Indented));
        revision = store.Load(world.WorldId).Revision;
        Check(runtime.Execute(actors[1], all).Code == GameCodes.Ok && store.Load(world.WorldId).Revision == revision, "withdrawal receipt replay no second casualties");
        GameResult pendingArmy = runtime.Execute(actors[1], Command("combat.city.garrison.dispatch", Garrison(6, 7)));
        Check(pendingArmy.Code == GameCodes.Ok, "second real pending garrison army"); string pendingId = pendingArmy.Data.Value<string>("armyId");
        Check(runtime.Execute(actors[0], Command("combat.city.withdraw", new JObject { ["battleId"] = original.BattleId })).Code == GameCodes.Ok, "true attacker withdrawal settles current city once");
        Check(Battle().GarrisonArmies.Single(army => army.ArmyId == pendingId).Phase == "returned" && new[] { 6, 7 }.All(number => store.Load(world.WorldId).EntityMappings["generalOccupancy"][Id(1, number)] == null), "disappearing target returns actual marching soldiers and bindings without new battle");
        Check(((JObject)store.Load(world.WorldId).Data["战斗运行"]).Count == ((JObject)world.Data["战斗运行"]).Count, "garrison never creates NPC attack or synthetic battle");
        File.WriteAllText(Path.Combine(directory, "final-world.json"), JsonConvert.SerializeObject(store.Load(world.WorldId), Formatting.Indented));
        File.WriteAllText(Path.Combine(directory, "result.json"), new JObject { ["passed"] = true, ["checks"] = checks, ["database"] = db,
            ["sourceWorld"] = Path.GetFullPath(fixturePath), ["sourceCityCandidate"] = "b12ace255e65f429c7e2628b436d630a46d08e88/51b58c176b70e50db8650ad8dab56f8193060a85/39545594563d29778aeb5b8c2ecb82fc37698613",
            ["notificationSource"] = "8864d095b6f43d85fa4bdf47e2cf1aa74429d1b7", ["cityDefenderGeneralSource"] = "21c29a9c10c3f5f40eeac9f822bda9c970c3adac",
            ["preparation"] = "Ignored mature fixture: original 25 excellent generals/99-level experience, human tech normal10/advanced5/engineering15. Original catalog equipment quality4/level30 generated by original enhancement rules with consumed material stacks <=999, then actual equipBest/allocateTroops. Original 997788 attack route exception; ordinary defender original route. NPC/127 guards/1600000 wall/special NPC tech65/damage unchanged. No production grant or new NPC attack." }.ToString());
        store.Dispose(); Console.WriteLine("GARRISON_RUNTIME_PASSED checks=" + checks + " directory=" + directory);
    }
}
