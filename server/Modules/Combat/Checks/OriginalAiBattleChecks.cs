using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dwsg.Persistence;
using Dwsg.Runtime;
using Dwsg.Server.Modules.Combat;
using Dwsg.Server.Modules.Generals;
using Dwsg.Server.World;
using Dwsg.Shared;
using Dwsg.Shared.Combat;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public static class OriginalAiBattleChecks
{
    public static void CheckCapturedZero(string seedPath)
    {
        var world = new WorldState { WorldId = "ai-force-boundary", Data = JObject.Parse(File.ReadAllText(seedPath)),
            EntityMappings = new JObject { ["players"] = new JObject(), ["humanPlayers"] = new JObject() } };
        for (int index = 0; index < ((JArray)world.Data["玩家列表"]).Count; index++)
            world.EntityMappings["players"]["force-player-" + index] = index;
        if (LegacyWorldModule.CreatePlayer(world, "原捕获兵力边界", "汉", 123000, out int humanIndex).Code != GameCodes.Ok)
            throw new InvalidOperationException("Original role boundary unavailable");
        string human = "force-human"; world.EntityMappings["players"][human] = humanIndex; world.EntityMappings["humanPlayers"][human] = true;
        GeneralsModule.EnsureMappings(world);
        world.Data["难度"] = 4; // 原合法高难度输入，只用于新场捕获0/高待入兵量的独立回归。
        world.Data["原AI推城"] = new JObject { ["军情"] = new JObject() };
        JObject city = ((JArray)world.Data["城池列表"]).OfType<JObject>().First(item => item.Value<int>("规模") == 2 && item.Value<string>("国家") != "汉");
        // 仅构造原驻防读分支的高队列边界；守将全部取真实export并用现有满配方法，不代表客户端写此表的入口已经接通。
        double queued = 0;
        foreach (JProperty owner in ((JObject)world.EntityMappings["players"]).Properties().Where(entry => entry.Value.Value<int>() >= 2 && entry.Value.Value<int>() < 17 && entry.Value.Value<int>() != 3))
        {
            foreach (JObject original in ((JArray)world.RequirePlayer(owner.Name)["封地信息表"][0]["将领信息表"]).OfType<JObject>())
            {
                JObject guard = (JObject)original.DeepClone(); CityGarrisonRules.EquipNamedGuard(guard);
                guard["详细信息"]["身份"] = world.ResolvePlayerIndex(owner.Name);
                ((JArray)city["城池玩家驻防列表"]).Add(guard); queued += guard["将领配兵"].Value<double>("数量");
                if (queued >= 300000) break;
            }
            if (queued >= 300000) break;
        }
        string OwnerInputs(WorldState state) => new JArray(((JArray)state.Data["玩家列表"]).OfType<JObject>().Select(owner =>
            new JObject { ["科技"] = owner["科技信息"].DeepClone(), ["装备"] = owner["背包装备列表"].DeepClone(),
                ["将领属性"] = new JArray(((JArray)owner["封地信息表"]).OfType<JObject>().SelectMany(fief => ((JArray)fief["将领信息表"]).OfType<JObject>()).Select(general => general["将领属性"].DeepClone())) })).ToString(Formatting.None);
        string owners = OwnerInputs(world);
        for (ulong seed = 1; seed <= 16; seed++)
        {
            WorldState candidate = world.Clone();
            var battle = new BanditBattle { Kind = "city", BattleId = "force-boundary-" + seed, PlayerId = "force-player-3",
                X = city.Value<int>("坐标x"), Y = city.Value<int>("坐标y"), ArrivalUtcMs = 123000, RandomState = seed };
            candidate.Data["原AI推城"]["军情"][battle.BattleId] = new JObject { ["ReferencePlayerId"] = human };
            typeof(CombatModule).GetMethod("StartCityBattle", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                .Invoke(null, new object[] { candidate, battle });
            int full = battle.Defenders.Count / 5;
            double force = battle.Defenders.Sum(unit => unit.OriginalQuantity);
            if (force < 500000 || full <= 8) continue;
            if (!battle.DefenseFormations.Skip(8).Take(full - 8).All(formation => formation.AvailableUtcMs > battle.ArrivalUtcMs
                && formation.AvailableUtcMs <= battle.ArrivalUtcMs + 20000))
                throw new InvalidOperationException("Pending force was substituted for original captured0 and froze real original reinforcements");
            if (OwnerInputs(candidate) != owners)
                throw new InvalidOperationException("Temporary high-force militia mutated a real owner");
            if (!((JArray)candidate.Data["城池列表"]).OfType<JObject>().Single(item => item.Value<int>("坐标x") == battle.X && item.Value<int>("坐标y") == battle.Y).Value<bool>("正在交战"))
                throw new InvalidOperationException("Defender candidate replacement lost the actual city fighting flag");
            Console.WriteLine("PASS actual new AI arrival helper: pending real militia=" + force + " across " + battle.Defenders.Count
                + " generals, captured prefab force0 still permits original delayed formations");
            Console.WriteLine("PASS high-force temporary generator preserves owner technology/equipment/attributes, legitimate guard leases remain separate");
            Console.WriteLine("PASS original AI city flag survives canonical defender candidate replacement");
            return;
        }
        throw new InvalidOperationException("No actual original high-force input exercised");
    }

    public static void Run(string seedPath, string outputPath)
    {
        string directory = Path.GetFullPath(outputPath);
        if (!directory.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Contains("audit", StringComparer.OrdinalIgnoreCase))
            throw new ArgumentException("Checks must stay in ignored audit.");
        Directory.CreateDirectory(directory);
        JObject seed = JObject.Parse(File.ReadAllText(seedPath));
        int checks = 0;
        void Check(bool condition, string name) { if (!condition) throw new InvalidOperationException(name); checks++; Console.WriteLine("PASS " + name); }
        foreach (bool humanKing in new[] { false, true })
        {
            long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000 * 1000;
            var world = new WorldState { WorldId = "ai-battle-" + Guid.NewGuid().ToString("N"), Data = (JObject)seed.DeepClone(),
                EntityMappings = new JObject { ["players"] = new JObject(), ["humanPlayers"] = new JObject() } };
            // 合法原输入边界：全局任务脚本.调整普通难度设1；原export为0且参考统帅0时规模0出军本来就是0兵。
            // 不修改生产世界，不把此规则fixture当PHP真人当前世界。
            world.Data["难度"] = 1;
            JObject players = (JObject)world.EntityMappings["players"];
            for (int index = 0; index < ((JArray)world.Data["玩家列表"]).Count; index++) players[Guid.NewGuid().ToString("N")] = index;
            Check(LegacyWorldModule.CreatePlayer(world, "原AI边界角色", "汉", now, out int humanIndex).Code == GameCodes.Ok, "actual original role for controlled AI boundary");
            string human = Guid.NewGuid().ToString("N"); players[human] = humanIndex; world.EntityMappings["humanPlayers"][human] = true;
            Check(LegacyWorldModule.CreatePlayer(world, "原AI第二真人", "汉", now, out int secondIndex).Code == GameCodes.Ok, "second actual role tests reward recipient independent of first reference");
            string secondHuman = Guid.NewGuid().ToString("N"); players[secondHuman] = secondIndex; world.EntityMappings["humanPlayers"][secondHuman] = true;
            GeneralsModule.EnsureMappings(world);
            int selectedCountry = -1; ulong selectedRandom = 0; JObject selectedCity = null;
            bool FindMilitiaChoice()
            {
                var cities = ((JArray)world.Data["城池列表"]).OfType<JObject>().ToArray();
                var lookup = cities.ToDictionary(city => Tuple.Create(city.Value<int>("坐标x"), city.Value<int>("坐标y")));
                var countries = (JArray)world.Data["国家列表"];
                for (int countryIndex = 0; countryIndex < countries.Count; countryIndex++)
                {
                    JObject country = (JObject)countries[countryIndex];
                    string name = country.Value<string>("国号");
                    var path = new CityMarchRules(全局大地图库.大地图表, (x, y) => lookup[Tuple.Create(x, y)].Value<string>("国家") == name);
                    for (ulong randomSeed = 1; randomSeed <= 64; randomSeed++)
                    {
                        var random = new CombatRandom(randomSeed); random.Next(120, 300);
                        int target = OriginalAiRules.SelectTarget((JArray)world.Data["城池列表"], country, path.Find, random.Next);
                        if (target < 0 || cities[target].Value<int>("规模") < 1 || cities[target].Value<int>("规模") > 4) continue;
                        selectedCountry = countryIndex; selectedRandom = randomSeed; selectedCity = cities[target]; return true;
                    }
                }
                return false;
            }
            // 原新世界最近的候选都是0级空城。用真实AI/墙战/原M03占领逐步扩张，不能直接伪造前沿归属。
            var preparation = new CombatModule(); int prepared = 0;
            while (!FindMilitiaChoice() && prepared < 40)
            {
                if (world.Data["原AI推城"] == null)
                {
                    var initial = new JArray(Enumerable.Repeat(now + 299000L, ((JArray)world.Data["国家列表"]).Count)); initial[0] = now;
                    world.Data["原AI推城"] = new JObject { ["ReferencePlayerId"] = human, ["RandomState"] = 123UL,
                        ["各国下次出手UtcMs"] = initial, ["上次出手UtcMs"] = now - 10000, ["NextTickUtcMs"] = now, ["军情"] = new JObject() };
                }
                now = world.Data["原AI推城"].Value<long>("NextTickUtcMs");
                GameCommand tick = preparation.CollectOriginalAiCommands(world, now).Single();
                GameResult dispatched = preparation.Execute(world, new CommandContext(AuthenticatedActor.System(world.WorldId), now), tick);
                if (dispatched.Code != GameCodes.Ok) throw new InvalidOperationException("Original frontier dispatch " + dispatched.Code);
                string preparationId = dispatched.Data.Value<string>("battleId");
                if (preparationId != null)
                {
                    for (int step = 0; step < 240 && !world.Data["战斗运行"][preparationId].Value<bool>("SettlementApplied"); step++)
                    {
                        now = Math.Max(now, world.Data["战斗运行"][preparationId].Value<long>("NextTickUtcMs")) + 10000;
                        GameCommand advance = preparation.CollectDueCommands(world, now).Single(command => command.Type == "combat.city.advance" && command.Payload.Value<string>("battleId") == preparationId);
                        GameResult advanced = preparation.Execute(world, new CommandContext(AuthenticatedActor.System(world.WorldId), now), advance);
                        if (advanced.Code != GameCodes.Ok) throw new InvalidOperationException("Original frontier battle " + advanced.Code + " " + advanced.Message);
                    }
                    if (!world.Data["战斗运行"][preparationId].Value<bool>("SettlementApplied")) throw new InvalidOperationException("Actual frontier battle did not finish");
                    Console.WriteLine("FRONTIER " + prepared + " nation=" + dispatched.Data["countryIndex"] + " city=" + world.Data["战斗运行"][preparationId]["CityName"]
                        + " phase=" + world.Data["战斗运行"][preparationId]["Phase"] + " frame=" + world.Data["战斗运行"][preparationId]["Frame"]);
                }
                else Console.WriteLine("FRONTIER " + prepared + " dispatch=" + dispatched.Data.ToString(Formatting.None));
                prepared++;
            }
            Console.WriteLine("Original actual frontier preparations=" + prepared);
            Check(selectedCity != null, "real original map has AI candidate with actual militia");
            var nations = (JArray)world.Data["国家列表"];
            now = (now / 1000 + 1) * 1000; // 原TIME秒级军情；战斗100ms补算后回到完整秒的排期边界。
            JObject selectedNation = (JObject)nations[selectedCountry];
            if (humanKing)
            {
                // 多人兼容边界：第二个真实绑定角色任原国家国王；首真人仍仅作为兵数科技参考。
                selectedNation["国王"] = secondIndex; world.RequirePlayer(secondHuman)["基础信息"]["国家"] = selectedNation["国号"].DeepClone();
            }
            var deadlines = new JArray(Enumerable.Repeat(now + 299000L, nations.Count));
            deadlines[selectedCountry] = now;
            JObject committedMilitary = (JObject)world.Data["原AI推城"]?["军情"]?.DeepClone() ?? new JObject();
            world.Data["原AI推城"] = new JObject { ["ReferencePlayerId"] = human, ["RandomState"] = selectedRandom,
                ["各国下次出手UtcMs"] = deadlines, ["上次出手UtcMs"] = now - 10000, ["NextTickUtcMs"] = now, ["军情"] = committedMilitary };
            string db = Path.Combine(directory, world.WorldId + ".sqlite3");
            SqliteWorldStore store = new SqliteWorldStore(db);
            store.ImportWorld(world, new[] { new RoleBinding { WorldId = world.WorldId, AccountId = "ai-boundary", PlayerId = human, LegacyPlayerIndex = humanIndex },
                new RoleBinding { WorldId = world.WorldId, AccountId = "ai-second", PlayerId = secondHuman, LegacyPlayerIndex = secondIndex } });
            WorldRuntime Runtime()
            {
                var runtime = new WorldRuntime(store, actor => actor.IsSystem || actor.PlayerId == human, () => now);
                runtime.Register(new CombatModule()); return runtime;
            }
            WorldRuntime runtime = Runtime();
            string OwnerState(WorldState state) => new JArray(((JArray)state.Data["玩家列表"]).OfType<JObject>().Select(owner => {
                var copy = (JObject)owner.DeepClone(); copy.Remove("notifications"); return copy;
            })).ToString(Formatting.None);
            string originalOwners = OwnerState(world);
            runtime.Tick(world.WorldId);
            WorldState current = store.Load(world.WorldId);
            string id = ((JObject)current.Data["原AI推城"]["军情"]).Properties().Single(entry => committedMilitary[entry.Name] == null).Name;
            BanditBattle Battle() => store.Load(world.WorldId).Data["战斗运行"][id].ToObject<BanditBattle>();
            BanditBattle marching = Battle();
            Check(marching.X == selectedCity.Value<int>("坐标x") && marching.Y == selectedCity.Value<int>("坐标y")
                && marching.ArrivalUtcMs == now + 30000 && marching.Phase == "marching", "actual CombatModule registered Tick dispatches original chosen army /30 seconds");
            Check(OwnerState(current) == originalOwners, "temporary ATT generation never changes real human or NPC owner serialized inputs");
            store.Dispose(); store = new SqliteWorldStore(db); runtime = Runtime();
            Check(Battle().BattleId == id && Battle().RandomState == marching.RandomState, "actual marching army resumes same battleId / RNG after native SQLite reopen");
            now += 29999; runtime.Tick(world.WorldId);
            Check(Battle().Phase == "marching", "actual registered Tick cannot arrive at29.999 seconds");
            now++; runtime.Tick(world.WorldId);
            BanditBattle fighting = Battle();
            Check(fighting.Phase == "fighting" && fighting.StartedUtcMs == marching.ArrivalUtcMs && fighting.Frame == 0, "actual registered Tick starts exact original AI arrival branch");
            int minimum = fighting.CityScale == 1 ? 30 : fighting.CityScale == 2 ? 40 : fighting.CityScale == 3 ? 20 : 10;
            int maximum = fighting.CityScale == 1 ? 40 : fighting.CityScale == 2 ? 60 : fighting.CityScale == 3 ? 35 : 30;
            Check(fighting.Defenders.Count >= minimum && fighting.Defenders.Count < maximum
                && fighting.Defenders.All(unit => unit.Ephemeral && unit.Side == 1), "AI arrival creates actual original militia rather than player attack named guards");
            Check(OwnerState(store.Load(world.WorldId)) == originalOwners, "temporary DEF generation preserves all real owner technology / equipment / generals");
            store.Dispose(); store = new SqliteWorldStore(db); runtime = Runtime();
            Check(Battle().Phase == "fighting" && Battle().Defenders.Count == fighting.Defenders.Count, "fighting / defender formations survive store restart without respawn");
            string ownerId = fighting.PlayerId;
            double prestige = store.Load(world.WorldId).RequirePlayer(ownerId)["基础信息"].Value<double>("声望");
            double war = store.Load(world.WorldId).RequirePlayer(ownerId)["基础信息"].Value<double>("战功");
            double spectatorPrestige = store.Load(world.WorldId).RequirePlayer(human)["基础信息"].Value<double>("声望");
            GameCommand last = null;
            for (int step = 0; step < 240 && !Battle().SettlementApplied; step++)
            {
                now = Math.Max(now, Battle().NextTickUtcMs) + 10000;
                // 只验证已提交这一个真实战场；不修改世界排期，不以大量无关AI战斗稀释结果。
                last = new CombatModule().CollectDueCommands(store.Load(world.WorldId), now).Single(command => command.Type == "combat.city.advance" && command.Payload.Value<string>("battleId") == id);
                GameResult advanced = runtime.Execute(AuthenticatedActor.System(world.WorldId), last);
                if (advanced.Code != GameCodes.Ok) throw new InvalidOperationException("AI kernel failed: " + advanced.Code + " " + advanced.Message);
            }
            BanditBattle settled = Battle();
            Check(settled.SettlementApplied && settled.Phase == "won" && settled.Defenders.Sum(unit => unit.Remaining) == 0,
                "actual original AI army fights real militia and completes existing kernel victory");
            current = store.Load(world.WorldId);
            Check(current.Data["城池占领结算"]?[id] != null && current.Data["城池列表"].OfType<JObject>().Single(city => city.Value<int>("坐标x") == settled.X && city.Value<int>("坐标y") == settled.Y)
                .Value<int>("城主") == current.ResolvePlayerIndex(ownerId), "existing M03 conquest commits actual attacking king once");
            Check(!((JObject)current.EntityMappings["generalOccupancy"]).Properties().Any(entry => entry.Value.Value<string>("armyId").StartsWith(marching.ArmyId, StringComparison.Ordinal)),
                "ephemeral ID0 ATT never creates or releases a canonical lease");
            double afterPrestige = current.RequirePlayer(ownerId)["基础信息"].Value<double>("声望");
            double afterWar = current.RequirePlayer(ownerId)["基础信息"].Value<double>("战功");
            Check(humanKing ? settled.Reward.声望 > 0 && afterPrestige > prestige && afterWar > war
                : settled.Reward.声望 == 0 && settled.Reward.国库铜钱 == 0 && afterPrestige == prestige && afterWar == war,
                humanKing ? "actual second human king receives original rewards independent of first reference" : "NPC king receives original conquest without invented player rewards");
            Check(current.RequirePlayer(human)["基础信息"].Value<double>("声望") == spectatorPrestige, "first reference / spectator never receives actual king personal rewards");
            long revision = current.Revision; string durable = current.Data.ToString(Formatting.None);
            Check(runtime.Execute(AuthenticatedActor.System(world.WorldId), last).Code == GameCodes.Ok && store.Load(world.WorldId).Revision == revision,
                "settlement receipt replay cannot reaward or reconquer");
            store.Dispose(); store = new SqliteWorldStore(db); runtime = Runtime();
            Check(store.Load(world.WorldId).Data.ToString(Formatting.None) == durable && Battle().SettlementApplied
                && !new CombatModule().CollectDueCommands(store.Load(world.WorldId), now).Any(command => command.Payload.Value<string>("battleId") == id),
                "settled victory persists and no longer schedules after native SQLite reopen");
            store.Dispose();
            Console.WriteLine("BOUNDARY humanKing=" + humanKing + " realCity=" + selectedCity.Value<string>("名称")
                + " scale=" + settled.CityScale + " army=" + settled.Attackers.Count + " militia=" + settled.Defenders.Count + " frame=" + settled.Frame);
        }
        var calls = new List<Tuple<int, int>>();
        foreach (int scale in new[] { 0, 3, 4 })
        {
            var created = OriginalAiRules.CreateMilitia(scale, "普通名", (low, high) => { calls.Add(Tuple.Create(low, high)); return low; }, level => new JObject { ["level"] = level });
            Check(created.Count == (scale == 0 ? 0 : scale == 3 ? 20 : 10)
                && created.All(general => general.Value<int>("level") == (scale == 3 ? 70 : 90)), "distinct original AI militia scale" + scale + " branch");
        }
        Check(OriginalAiRules.CreateMilitia(4, "9977886", (low, high) => { throw new InvalidOperationException("must not consume RNG"); }, level => null).Count == 0,
            "AI original special reference name skips scale4 with no extra RNG draw");
        long[] availability = CityGarrisonRules.FormationAvailability(59, 123000, (low, high) => low);
        Check(availability.Take(8).All(value => value == 123000) && availability.Skip(8).Take(3).All(value => value == 129000) && availability.Last() == 123000,
            "new AI scene captured force0 keeps first8 immediate, original5..19 strict-second delay, and immediate remainder");
        Console.WriteLine("Original AI battle checks=" + checks + "; actual original Runtime+SQLite kernel with controlled UTC/seed/king boundary; PHP human and natural wait not claimed");
    }
}
