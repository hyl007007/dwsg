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

internal static class CityChecks
{
    public static void Run(string seedPath, string outputPath)
    {
        string directory = Path.GetFullPath(outputPath);
        if (!directory.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Contains("audit", StringComparer.OrdinalIgnoreCase)) throw new ArgumentException("Checks must stay in ignored audit.");
        directory = Path.Combine(directory, Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
        var seed = JObject.Parse(File.ReadAllText(seedPath));
        var cities = ((JArray)seed["城池列表"]).OfType<JObject>().ToArray();
        var lookup = cities.ToDictionary(city => Tuple.Create(city.Value<int>("坐标x"), city.Value<int>("坐标y")));
        string actorNation = "汉";
        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(); int checks = 0;
        void Check(bool value, string name) { if (!value) throw new InvalidOperationException(name); checks++; Console.WriteLine("PASS " + name); }
        var world = new WorldState { WorldId = "city-check-" + Guid.NewGuid().ToString("N"), Data = (JObject)seed.DeepClone(), EntityMappings = new JObject { ["players"] = new JObject() } };
        JObject players = (JObject)world.EntityMappings["players"];
        for (int index = 0; index < ((JArray)world.Data["玩家列表"]).Count; index++) players[Guid.NewGuid().ToString("N")] = index;
        var actors = new List<AuthenticatedActor>(); var bindings = new List<RoleBinding>();
        var random = new CombatRandom(100200300);
        for (int account = 0; account < 2; account++)
        {
            Check(LegacyWorldModule.CreatePlayer(world, account == 1 ? "997788" : "城战君主", actorNation, now, out int index).Code == GameCodes.Ok, "real original city check role " + account);
            string id = Guid.NewGuid().ToString("N"); players[id] = index;
            var actor = new AuthenticatedActor("city-account-" + account, id, world.WorldId, "city-connection-" + account); actors.Add(actor);
            bindings.Add(new RoleBinding { WorldId = world.WorldId, AccountId = actor.AccountId, PlayerId = id, LegacyPlayerIndex = index });
            JObject player = world.RequirePlayer(id); JObject fief = (JObject)player["封地信息表"][0];
            // 隔离检查中的成熟军队仍使用真实角色/封地模板、原将领生成、99级经验、科技与统兵公式。
            // 原书院升级按钮：工程15，其余普通科技10，仓储/安置/格斗/精准/驯马/精工5。
            string[] advanced = { "仓储", "安置", "格斗", "精准", "驯马", "精工" };
            foreach (JProperty technology in ((JObject)player["科技信息"]).Properties())
                technology.Value = technology.Name == "工程设计" ? 15.0 : advanced.Contains(technology.Name) ? 5.0 : 10.0;
            Check(((JObject)player["科技信息"]).Properties().Count() == 21 && advanced.All(name => player["科技信息"].Value<double>(name) == 5)
                && player["科技信息"].Value<double>("工程设计") == 15 && player["科技信息"].Value<double>("统帅能力") == 10,
                "mature fixture uses original attainable school technology ceilings " + account);
            int count = account == 0 ? 25 : 5;
            for (int number = 1; number <= count; number++)
            {
                JObject configuration = ((JArray)seed["将领配置"]).OfType<JObject>().Single(item => item.Value<int>("ID") == (number % 2 == 0 ? 5 : 6));
                JObject general = GeneralCreationRules.CreateBanditGeneral(configuration, random.Next); general["ID"] = number;
                GeneralExperienceRules.Add(general, GeneralExperienceRules.TotalForLevel(99));
                general["将领培养"]["保底次数"] = general["将领培养"]["保底上限"].DeepClone();
                ((JArray)fief["将领信息表"]).Add(general);
                if (account == 0)
                    LegacyGenerals.Equipment(player, 0).Add(new JObject { ["将领ID"] = -1, ["品质"] = 4.0, ["强化等级"] = 0.0,
                        ["强化值"] = 0.0, ["保底次数"] = 0.0, ["已强化次数"] = 20.0, ["炼魂属性"] = new JArray(),
                        ["装备信息"] = ((JArray)seed["装备配置"]).OfType<JObject>().First(item => item.Value<string>("类型") == "头盔").DeepClone() });
            }
            player["将领ID标识"] = count + 1;
            Check(!GeneralAttributeRules.Recalculate(player, now / 1000), "mature fixture remains inside original attribute caps " + account);
            // 仅隔离检查预置成熟库存与既有保底进度；不是生产中的招募或商城发放。
            foreach (int type in new[] { 304, 403 }) ((JArray)fief["闲兵信息表"]).Add(new JObject { ["ID"] = type, ["数量"] = 500000.0 });
            JObject soul = ((JArray)seed["道具配置"]).OfType<JObject>().Single(item => item.Value<string>("名字") == "将神魂");
            ((JArray)player["背包道具列表"][soul.Value<string>("分类") + "道具列表"]).Add(new JObject { ["名字"] = "将神魂", ["数量"] = 2.0 });
            JObject material = ((JArray)seed["道具配置"]).OfType<JObject>().Single(item => item.Value<string>("名字") == "冰玉");
            ((JArray)player["背包道具列表"][material.Value<string>("分类") + "道具列表"]).Add(new JObject { ["名字"] = "冰玉", ["数量"] = 1.0 });
        }
        GeneralsModule.EnsureMappings(world);
        string Id(AuthenticatedActor actor, int legacy) => ((JObject)world.EntityMappings["generals"]).Properties().Single(entry => entry.Value.Value<string>("playerId") == actor.PlayerId && entry.Value.Value<int>("legacyId") == legacy).Name;
        string db = Path.Combine(directory, "world.sqlite3");
        var store = new SqliteWorldStore(db); store.ImportWorld(world, bindings);
        WorldRuntime Runtime()
        {
            var runtime = new WorldRuntime(store, actor => actor.IsSystem || actors.Any(real => real.AccountId == actor.AccountId && real.PlayerId == actor.PlayerId && real.ConnectionId == actor.ConnectionId), () => now);
            runtime.Register(new GeneralsModule()); runtime.Register(new CombatModule()); return runtime;
        }
        WorldRuntime runtime = Runtime();
        void Tick()
        {
            foreach (GameCommand due in new CombatModule().CollectDueCommands(store.Load(world.WorldId), now).ToArray())
            {
                GameResult advanced = runtime.Execute(AuthenticatedActor.System(world.WorldId), due);
                if (advanced.Code != GameCodes.Ok) throw new InvalidOperationException("Real city tick rejected: " + advanced.Code + " " + advanced.Message);
            }
        }
        GameCommand Command(string type, JObject payload, string request = null) => new GameCommand { WorldId = world.WorldId, RequestId = request ?? Guid.NewGuid().ToString("N"), Type = type, Payload = payload };
        BanditBattle Battle(string battleId) => store.Load(world.WorldId).Data["战斗运行"][battleId].ToObject<BanditBattle>();
        JObject General(int legacy) { JObject fief; return GeneralsModule.ResolveGeneral(store.Load(world.WorldId), actors[0].PlayerId, Id(actors[0], legacy), out fief); }
        JObject CurrentCity(JObject city) => ((JArray)store.Load(world.WorldId).Data["城池列表"]).OfType<JObject>().Single(item => item.Value<int>("坐标x") == city.Value<int>("坐标x") && item.Value<int>("坐标y") == city.Value<int>("坐标y"));
        GameCommand Dispatch(JObject city, params int[] ids) => Command("combat.city.dispatch", new JObject { ["x"] = city["坐标x"], ["y"] = city["坐标y"], ["generalIds"] = new JArray(ids.Select(number => Id(actors[0], number))) });
        void Rejected(AuthenticatedActor actor, GameCommand command, string name)
        {
            WorldState before = store.Load(world.WorldId); string data = before.Data.ToString(Formatting.None), mappings = before.EntityMappings.ToString(Formatting.None);
            GameResult result = runtime.Execute(actor, command);
            Check(result.Code != GameCodes.Ok, name + " rejected");
            WorldState after = store.Load(world.WorldId);
            Check(after.Revision == before.Revision && data == after.Data.ToString(Formatting.None) && mappings == after.EntityMappings.ToString(Formatting.None), name + " has no partial domain writes");
        }
        JObject origin = (JObject)world.RequirePlayer(actors[0].PlayerId)["封地信息表"][0]["所在城池"];
        int Cost(JObject city) => new CityMarchRules(全局大地图库.大地图表, (x, y) => lookup[Tuple.Create(x, y)].Value<string>("国家") == actorNation)
            .Find(origin.Value<int>("x") - 1, origin.Value<int>("y") - 1, city.Value<int>("坐标x") - 1, city.Value<int>("坐标y") - 1);
        var reachable = cities.Where(city => city.Value<string>("国家") != actorNation && Cost(city) >= 0).ToArray();
        Check(reachable.Length > 3, "actual 942 city map has real reachable NPC targets");
        JObject target = reachable.Where(city => city.Value<int>("规模") == 0).OrderByDescending(city => ((JArray)city["城池驻防列表"]).Count).First();
        Console.WriteLine("TARGET " + target.Value<string>("名称") + " " + target["坐标x"] + "," + target["坐标y"]);
        for (int number = 1; number <= 25; number++)
        {
            string equipmentId = ((JObject)world.EntityMappings["equipment"]).Properties().Single(entry => entry.Value.Value<string>("playerId") == actors[0].PlayerId
                && entry.Value.Value<int>("slot") == 0 && entry.Value.Value<int>("legacyIndex") == number - 1).Name;
            Check(runtime.Execute(actors[0], Command("generals.equip", new JObject { ["generalId"] = Id(actors[0], number), ["equipmentId"] = equipmentId })).Code == GameCodes.Ok,
                "original mature general equips real catalog helmet " + number);
            int troop = number >= 21 ? 403 : 304;
            int quantity = General(number)["将领属性"]["最终属性"].Value<int>("统兵");
            Check(runtime.Execute(actors[0], Command("generals.allocateTroops", new JObject { ["generalId"] = Id(actors[0], number), ["troopTypeId"] = troop, ["count"] = quantity })).Code == GameCodes.Ok, "original mature army allocation " + number);
        }
        Rejected(actors[1], Dispatch(target, 1), "foreign city army");
        Rejected(actors[0], Command("combat.city.advance", new JObject { ["battleId"] = "fake", ["tickUtcMs"] = now }), "client forged city progress");
        Rejected(actors[0], Dispatch(cities.First(city => city.Value<string>("国家") == actorNation), 1), "own nation target");
        JObject unreachable = cities.First(city => city.Value<string>("国家") != actorNation && Cost(city) < 0);
        Rejected(actors[0], Dispatch(unreachable, 1), "actual blocked original road");
        var dispatch = Dispatch(target, 1, 2, 3, 4, 5);
        GameResult result = runtime.Execute(actors[0], dispatch);
        Check(result.Code == GameCodes.Ok, "real Runtime city dispatch");
        string battleId = result.Data.Value<string>("battleId");
        var march = Battle(battleId);
        Check(march.Kind == "city" && march.Phase == "marching" && march.ArrivalUtcMs == now + 10000, "original city ten second march persists");
        Rejected(actors[1], Command("combat.city.dispatch", new JObject { ["x"] = target["坐标x"], ["y"] = target["坐标y"], ["generalIds"] = new JArray(Id(actors[1], 1)) }), "foreign busy NPC city");
        Rejected(actors[1], Command("combat.city.withdraw", new JObject { ["battleId"] = battleId }), "foreign city withdrawal");
        long revision = store.Load(world.WorldId).Revision;
        Check(runtime.Execute(actors[0], dispatch).Data.Value<string>("battleId") == battleId && store.Load(world.WorldId).Revision == revision, "city dispatch receipt replays one army");
        now += 9999; Tick(); Check(Battle(battleId).Phase == "marching", "city cannot arrive one millisecond early");
        now++; Tick();
        var arrived = Battle(battleId);
        Check(arrived.Phase == "fighting" && arrived.Frame == 0 && arrived.Defenders.Count(unit => unit.Ephemeral) >= 20 && arrived.Defenders.Count(unit => unit.Ephemeral) < 30, "original small city militia generated at actual arrival");
        Check(arrived.Defenders.Where(unit => unit.Ephemeral).All(unit => unit.General.Value<int>("ID") == 0 && unit.Troop.Value<int>("ID") != 403), "original temporary guards never become capture identities or siege carts");
        Check(arrived.Wall == target.Value<double>("城墙") && arrived.WallMaximum == target.Value<double>("城墙"), "actual original city wall used without fixture shortening");
        Check(arrived.AttackFormations[0].PositionX == -40.25f && arrived.DefenseFormations.All(group => group.PositionX == -24f), "original city brigade positions differ from bandit arena");
        foreach (var owner in arrived.Defenders.Where(unit => !unit.Ephemeral).GroupBy(unit => unit.GeneralOwnerId))
            Check(owner.All(unit => store.Load(world.WorldId).EntityMappings["generalOccupancy"][unit.GeneralId].Value<string>("armyId") == battleId), "real named guard binding " + owner.Key);
        store.Dispose(); store = new SqliteWorldStore(db); runtime = Runtime();
        Check(Battle(battleId).RandomState == arrived.RandomState && Battle(battleId).Defenders.Count == arrived.Defenders.Count, "real NPC guard wave and persisted random stream survive SQLite restart");
        GameCommand Reinforce(params int[] numbers) => Command("combat.city.reinforce", new JObject { ["battleId"] = battleId, ["generalIds"] = new JArray(numbers.Select(number => Id(actors[0], number))) });
        Check(runtime.Execute(actors[0], Reinforce(6, 7, 8, 9, 10)).Code == GameCodes.Ok, "real city immediate attack reinforcement");
        Check(runtime.Execute(actors[0], Reinforce(11, 12, 13, 14, 15)).Code == GameCodes.Ok, "second real city attack brigade");
        Check(runtime.Execute(actors[0], Reinforce(16, 17, 18, 19, 20)).Code == GameCodes.Ok, "third real city attack brigade");
        Check(runtime.Execute(actors[0], Reinforce(21, 22, 23, 24, 25)).Code == GameCodes.Ok, "original siege cart brigade joins actual city fight");
        Rejected(actors[1], Reinforce(1), "foreign city reinforcement");
        Rejected(actors[0], Command("combat.bandit.withdraw", new JObject { ["battleId"] = battleId }), "wrong battle command kind");
        now += 1000; Tick();
        Check(Battle(battleId).Defenders.Count(unit => unit.Slot >= 0) > 0 && Battle(battleId).Attackers.All(unit => unit.Slot < 0), "city guards enter original nearby pits before far attackers");
        File.WriteAllText(Path.Combine(directory, "arrived-world.json"), store.Load(world.WorldId).Data.ToString());
        File.WriteAllText(Path.Combine(directory, "arrived-snapshot.json"), JsonConvert.SerializeObject(store.Load(world.WorldId), Formatting.Indented));
        File.WriteAllText(Path.Combine(directory, "arrived-battle.json"), JObject.FromObject(Battle(battleId)).ToString());
        var nation = ((JArray)store.Load(world.WorldId).Data["国家列表"]).OfType<JObject>().Single(item => item.Value<string>("国号") == actorNation);
        double copper = nation.Value<double>("铜钱"), grain = nation.Value<double>("粮食"), war = world.RequirePlayer(actors[0].PlayerId)["基础信息"].Value<double>("战功");
        bool hitWall = false, pitQueue = false, cultivatedWounded = false;
        for (int tick = 0; tick < 180 && !Battle(battleId).SettlementApplied; tick++)
        {
            now += 10000; Tick(); var battle = Battle(battleId);
            hitWall |= battle.LastHits.Any(hit => hit.DefenderId == "wall");
            pitQueue |= battle.Attackers.Count(unit => unit.Slot >= 0) == 15 && battle.Attackers.Any(unit => unit.Slot < 0 && unit.Remaining > 0);
            CombatUnit wounded = battle.Attackers.FirstOrDefault(unit => !unit.Retired && unit.Remaining > 0 && unit.Wounded > 0);
            if (!cultivatedWounded && !battle.SettlementApplied && wounded != null)
            {
                string savedBattle = JObject.FromObject(battle).ToString(Formatting.None);
                double growth = wounded.General["将领属性"]["初始属性"].Value<double>("成长");
                GameCommand cultivate = Command("generals.cultivate", new JObject { ["generalId"] = wounded.GeneralId, ["count"] = 1 });
                GameResult cultivation = runtime.Execute(actors[0], cultivate);
                Check(cultivation.Code == GameCodes.Ok && cultivation.Data.Value<int>("growthIncreases") == 1, "original guaranteed cultivation works while real wounded army remains active");
                Check(JObject.FromObject(Battle(battleId)).ToString(Formatting.None) == savedBattle, "cultivation itself does not reset combat wounds progress wait slots army or random state");
                JObject fief;
                JObject canonical = GeneralsModule.ResolveGeneral(store.Load(world.WorldId), actors[0].PlayerId, wounded.GeneralId, out fief);
                Check(canonical["详细信息"].Value<int>("状态") == 1 && canonical["将领属性"]["初始属性"].Value<double>("成长") == growth + 1
                    && (canonical["将领属性"]["最终属性"].Value<double>("攻击") > wounded.General["将领属性"]["最终属性"].Value<double>("攻击")
                        || canonical["将领属性"]["最终属性"].Value<double>("防御") > wounded.General["将领属性"]["最终属性"].Value<double>("防御"))
                    && canonical["将领属性"]["最终属性"].Value<double>("生命值") == wounded.General["将领属性"]["最终属性"].Value<double>("生命值"), "active growth updates original attack defense while equipment life and occupation remain intact");
                string detailsBefore = canonical["详细信息"].ToString(Formatting.None);
                double lifeBefore = canonical["将领属性"]["最终属性"].Value<double>("生命值");
                string equipmentId = ((JObject)world.EntityMappings["equipment"]).Properties().Single(entry => entry.Value.Value<string>("playerId") == actors[0].PlayerId
                    && entry.Value.Value<int>("slot") == 0 && entry.Value.Value<int>("legacyIndex") == wounded.General.Value<int>("ID") - 1).Name;
                GameCommand enhance = Command("generals.enhanceEquipment", new JObject { ["equipmentId"] = equipmentId, ["count"] = 1 });
                GameResult enhancement = runtime.Execute(actors[0], enhance);
                Check(enhancement.Code == GameCodes.Ok && enhancement.Data.Value<int>("upgradedCount") == 1, "actual original helmet enhancement succeeds on already wounded active army");
                canonical = GeneralsModule.ResolveGeneral(store.Load(world.WorldId), actors[0].PlayerId, wounded.GeneralId, out fief);
                Check(canonical["将领属性"]["最终属性"].Value<double>("生命值") == lifeBefore + 10 && canonical["详细信息"].ToString(Formatting.None) == detailsBefore,
                    "original enhancement adds exactly ten equipment life without resetting stamina or army details");
                Check(JObject.FromObject(Battle(battleId)).ToString(Formatting.None) == savedBattle, "enhancement cannot redivide old damage heal prior casualties or reset battle progress");
                revision = store.Load(world.WorldId).Revision;
                Check(runtime.Execute(actors[0], enhance).Data.Value<int>("upgradedCount") == 1 && store.Load(world.WorldId).Revision == revision, "wounded active equipment enhancement replay cannot upgrade or consume twice");
                File.WriteAllText(Path.Combine(directory, "wounded-before-cultivate.json"), savedBattle);
                now = battle.NextTickUtcMs; Tick();
                BanditBattle next = Battle(battleId); CombatUnit refreshed = next.Attackers.Single(unit => unit.CombatId == wounded.CombatId);
                Check(next.Frame == battle.Frame + BanditBattleRules.FramesPerTick && refreshed.General["将领属性"]["初始属性"].Value<double>("成长") == growth + 1
                    && JToken.DeepEquals(refreshed.General["将领属性"]["最终属性"], canonical["将领属性"]["最终属性"]), "next authoritative hundred millisecond tick reads cultivated attack defense and life");
                Check(refreshed.OriginalQuantity == wounded.OriginalQuantity && refreshed.Remaining <= wounded.Remaining && refreshed.Wounded >= wounded.Wounded
                    && refreshed.Remaining > 0 && refreshed.ArmyId == wounded.ArmyId && refreshed.Slot == wounded.Slot
                    && refreshed.General["详细信息"].Value<double>("剩余体力") == canonical["详细信息"].Value<double>("剩余体力"), "already wounded active army preserves casualties original HP ratio and army continuity");
                revision = store.Load(world.WorldId).Revision;
                Check(runtime.Execute(actors[0], cultivate).Data.Value<int>("growthIncreases") == 1 && store.Load(world.WorldId).Revision == revision, "active cultivation receipt cannot consume another soul or grow twice");
                File.WriteAllText(Path.Combine(directory, "wounded-after-cultivate.json"), JObject.FromObject(next).ToString());
                cultivatedWounded = true;
            }
        }
        var won = Battle(battleId);
        Check(won.Phase == "won" && won.SettlementApplied && won.Wall == 0 && BanditBattleRules.DefendingForce(won, now) == 0, "actual mature original army clears NPC city defenders and full wall");
        Check(hitWall && pitQueue, "original siege damage and twenty five unit fifteen pit queue occur");
        Check(cultivatedWounded, "cultivation regression uses actual already wounded living army rather than full HP setup");
        Check(CurrentCity(target).Value<int>("城主") == store.Load(world.WorldId).ResolvePlayerIndex(actors[0].PlayerId) && CurrentCity(target).Value<string>("国家") == actorNation, "trusted city victory updates owner and nation in same committed world");
        var committed = store.Load(world.WorldId);
        nation = ((JArray)committed.Data["国家列表"]).OfType<JObject>().Single(item => item.Value<string>("国号") == actorNation);
        Check(nation.Value<double>("铜钱") == copper + won.Reward.国库铜钱 && nation.Value<double>("粮食") == grain + won.Reward.国库粮食, "original city treasury kill rewards once without advertised extra resources");
        Check(committed.RequirePlayer(actors[0].PlayerId)["基础信息"].Value<double>("战功") == war + 500 && won.WarReward == 500, "original small city war reward in same settlement");
        Check(won.Attackers.All(unit => committed.EntityMappings["generalOccupancy"][unit.GeneralId] == null) && won.Defenders.Where(unit => !unit.Ephemeral).All(unit => committed.EntityMappings["generalOccupancy"][unit.GeneralId] == null), "actual attacker and NPC named guard occupations all released");
        foreach (CombatUnit unit in won.Defenders.Where(unit => !unit.Ephemeral))
        {
            JObject fief; JObject general = GeneralsModule.ResolveGeneral(committed, unit.GeneralOwnerId, unit.GeneralId, out fief);
            Check(general["详细信息"].Value<int>("状态") == (unit.General["详细信息"].Value<int>("状态") == 3 ? 3 : 0), "named guard capture state survives real city terminal outcome");
        }
        string dataOnce = committed.Data.ToString(Formatting.None);
        Check(runtime.Execute(AuthenticatedActor.System(world.WorldId), Command("combat.city.advance", new JObject { ["battleId"] = battleId, ["tickUtcMs"] = won.NextTickUtcMs })).Code == GameCodes.Ok && store.Load(world.WorldId).Data.ToString(Formatting.None) == dataOnce, "finished city cannot reward or occupy twice");
        store.Dispose(); store = new SqliteWorldStore(db); runtime = Runtime();
        Check(store.Load(world.WorldId).Data.ToString(Formatting.None) == dataOnce && runtime.Execute(actors[0], dispatch).Data.Value<string>("battleId") == battleId, "city ownership reward and command receipt survive process store restart");
        // 同一真实原世界再走少兵失败；不改NPC生命、数量或城墙。
        JObject defeatTarget = reachable.First(city => city.Value<int>("规模") == 0 && !JToken.DeepEquals(city["坐标x"], target["坐标x"]));
        Check(runtime.Execute(actors[0], Command("generals.allocateTroops", new JObject { ["generalId"] = Id(actors[0], 1), ["troopTypeId"] = 304, ["count"] = 300 })).Code == GameCodes.Ok, "real small army allocation for city loss");
        result = runtime.Execute(actors[0], Dispatch(defeatTarget, 1)); Check(result.Code == GameCodes.Ok, "actual second NPC city march");
        string lossId = result.Data.Value<string>("battleId");
        for (int tick = 0; tick < 180 && !Battle(lossId).SettlementApplied; tick++) { now += 10000; Tick(); }
        var lost = Battle(lossId);
        Check(lost.Phase == "lost" && lost.Attackers.Single().Remaining == 0 && lost.Attackers.Single().Wounded > 0 && lost.Attackers.Single().Wounded < 300, "real city defeat preserves original per hit seventy percent wounded rule");
        Check(CurrentCity(defeatTarget).Value<int>("城主") == defeatTarget.Value<int>("城主") && lost.WarReward == 0 && General(1)["详细信息"].Value<int>("状态") == 0, "city loss releases army without occupying NPC city or granting war points");
        // 大城的后续真实NPC编队延后，余数编队立即；到达后撤退检验未出场名将也释放。
        JObject large = cities.First(city => city.Value<int>("规模") == 3 && city.Value<string>("国家") != actorNation && ((JArray)city["城池驻防列表"]).Count > 0);
        Check(Cost(large) < 0, "large city stays blocked for ordinary original capital routes");
        for (int number = 1; number <= 5; number++)
            Check(runtime.Execute(actors[1], Command("generals.allocateTroops", new JObject { ["generalId"] = Id(actors[1], number), ["troopTypeId"] = 304, ["count"] = 300 })).Code == GameCodes.Ok, "original second army allocation " + number);
        result = runtime.Execute(actors[1], Command("combat.city.dispatch", new JObject { ["x"] = large["坐标x"], ["y"] = large["坐标y"], ["generalIds"] = new JArray(Enumerable.Range(1, 5).Select(number => Id(actors[1], number))) }));
        Check(result.Code == GameCodes.Ok, "actual original named player route exception reaches unmodified large NPC city");
        string largeId = result.Data.Value<string>("battleId");
        now += 10000; Tick(); var largeBattle = Battle(largeId);
        Check(largeBattle.Defenders.Count(unit => unit.Ephemeral) >= 100 && largeBattle.Defenders.Count(unit => unit.Ephemeral) < 150 && largeBattle.DefenseFormations.Count > 8, "original large city garrison count and brigade sizes");
        Check(largeBattle.DefenseFormations.Take(8).All(group => group.AvailableUtcMs == largeBattle.ArrivalUtcMs)
            && largeBattle.DefenseFormations.Skip(8).Where((group, index) => (index + 8) * 5 + 5 <= largeBattle.Defenders.Count).All(group => group.AvailableUtcMs > largeBattle.ArrivalUtcMs && group.AvailableUtcMs <= (largeBattle.ArrivalUtcMs / 1000 + 20) * 1000), "original delayed full defender waves retain strict second boundary");
        File.WriteAllText(Path.Combine(directory, "large-world.json"), store.Load(world.WorldId).Data.ToString());
        File.WriteAllText(Path.Combine(directory, "large-battle.json"), JObject.FromObject(largeBattle).ToString());
        Check(largeBattle.Defenders.Any(unit => !unit.Ephemeral), "actual configured original named guard joins large city with stable identity");
        string reusedId = Id(actors[1], 2);
        Check(runtime.Execute(actors[1], Command("generals.cultivate", new JObject { ["generalId"] = reusedId, ["count"] = 1 })).Code == GameCodes.Ok,
            "original active cultivation before immediate city withdrawal");
        double cultivatedGrowth = store.Load(world.WorldId).RequirePlayer(actors[1].PlayerId)["封地信息表"][0]["将领信息表"].OfType<JObject>()
            .Single(item => item.Value<int>("ID") == 2)["将领属性"]["初始属性"].Value<double>("成长");
        Check(runtime.Execute(actors[1], Command("combat.city.withdraw", new JObject { ["battleId"] = largeId, ["generalId"] = reusedId })).Code == GameCodes.Ok,
            "original single city unit withdrawal releases only its occupation");
        CombatUnit retired = Battle(largeId).Attackers.Single(unit => unit.GeneralId == reusedId);
        Check(retired.Retired && retired.General["将领属性"]["初始属性"].Value<double>("成长") == cultivatedGrowth,
            "withdrawal before next tick preserves canonical cultivation instead of restoring stale growth");
        string retiredJson = JObject.FromObject(retired).ToString(Formatting.None);
        Check(runtime.Execute(actors[1], Command("generals.allocateTroops", new JObject { ["generalId"] = reusedId, ["troopTypeId"] = 304, ["count"] = 300 })).Code == GameCodes.Ok
            && runtime.Execute(actors[1], Command("combat.city.reinforce", new JObject { ["battleId"] = largeId, ["generalIds"] = new JArray(reusedId) })).Code == GameCodes.Ok,
            "same stable city general rejoins through a new authoritative army");
        Check(runtime.Execute(actors[1], Command("generals.cultivate", new JObject { ["generalId"] = reusedId, ["count"] = 1 })).Code == GameCodes.Ok,
            "original cultivation affects the rejoined active army");
        now = Battle(largeId).NextTickUtcMs; Tick();
        var reused = Battle(largeId).Attackers.Where(unit => unit.GeneralId == reusedId).ToArray();
        Check(reused.Length == 2 && reused.Count(unit => unit.Retired) == 1 && reused.Select(unit => unit.ArmyId).Distinct().Count() == 2
            && JObject.FromObject(reused.Single(unit => unit.Retired)).ToString(Formatting.None) == retiredJson,
            "retired city army remains frozen while canonical cultivation refreshes the current occupation only");
        var withdraw = Command("combat.city.withdraw", new JObject { ["battleId"] = largeId });
        Check(runtime.Execute(actors[1], withdraw).Code == GameCodes.Ok && Battle(largeId).Phase == "withdrawn", "actual city full withdrawal settles original army");
        Check(Battle(largeId).Defenders.Where(unit => !unit.Ephemeral).All(unit => store.Load(world.WorldId).EntityMappings["generalOccupancy"][unit.GeneralId] == null), "city withdrawal releases pending and arrived real NPC defenders");
        revision = store.Load(world.WorldId).Revision;
        Check(runtime.Execute(actors[1], withdraw).Code == GameCodes.Ok && store.Load(world.WorldId).Revision == revision, "city withdrawal retry does not settle twice");
        File.WriteAllText(Path.Combine(directory, "final-world.json"), store.Load(world.WorldId).Data.ToString());
        File.WriteAllText(Path.Combine(directory, "result.json"), new JObject { ["checks"] = checks, ["database"] = db, ["city"] = target.Value<string>("名称"), ["battleId"] = battleId, ["phase"] = won.Phase }.ToString());
        store.Dispose(); Console.WriteLine("CITY_CHECKS_PASSED " + checks + " " + directory);
    }
}
