using Dwsg.Persistence;
using Dwsg.Runtime;
using Dwsg.Server.Modules.Combat;
using Dwsg.Server.Modules.Generals;
using Dwsg.Server.World;
using Dwsg.Shared;
using Dwsg.Shared.Combat;
using Dwsg.Shared.Economy;
using Dwsg.Shared.Generals;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.Data.Sqlite;

internal static class PlayerCitySiegeRuntimeChecks
{
    private static string NativeWorldId(string databasePath)
    {
        using var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = databasePath, Mode = SqliteOpenMode.ReadOnly }.ToString());
        connection.Open(); using var command = connection.CreateCommand(); command.CommandText = "SELECT world_id FROM worlds";
        return (string)command.ExecuteScalar();
    }

    public static void RunGuardAvailability(string databasePath, string settledBattlePath)
    {
        using var store = new SqliteWorldStore(databasePath);
        WorldState world = store.Load(NativeWorldId(databasePath)).Clone();
        var roles = store.ListRoles(world.WorldId);
        string owner = roles.Single(role => role.AccountId == "native-siege-account-0").PlayerId;
        string attacker = roles.Single(role => role.AccountId == "native-siege-account-1").PlayerId;
        BanditBattle battle = JObject.Parse(File.ReadAllText(settledBattlePath)).ToObject<BanditBattle>(); battle.PlayerId = attacker;
        JProperty mapping = ((JObject)world.EntityMappings["generals"]).Properties().First(entry => entry.Value.Value<string>("playerId") == owner
            && GeneralsModule.ResolveGeneral(world, owner, entry.Name, out _)["详细信息"].Value<double>("状态") == 0);
        JObject general = GeneralsModule.ResolveGeneral(world, owner, mapping.Name, out _);
        var method = typeof(CombatModule).GetMethod("CanUseCityGuard", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        bool Allowed() => (bool)method.Invoke(null, new object[] { world, battle, owner, general });
        int checks = 0;
        void Check(bool value, string name) { if (!value) throw new InvalidOperationException(name); checks++; Console.WriteLine("PASS " + name); }
        Check(Allowed(), "ready actual owner canonical general can defend");
        general["详细信息"]["状态"] = 1.0; Check(!Allowed(), "busy canonical general cannot become automatic city guard"); general["详细信息"]["状态"] = 0.0;
        world.EntityMappings["generalOccupancy"][mapping.Name] = new JObject { ["playerId"] = owner, ["armyId"] = "native-other-existing-army" };
        Check(!Allowed(), "existing army occupancy survives even if a stale general appears idle"); ((JObject)world.EntityMappings["generalOccupancy"]).Remove(mapping.Name);
        battle.PlayerId = owner; Check(!Allowed(), "attacker cannot also be auto assigned to defending side"); battle.PlayerId = attacker;
        string nation = world.RequirePlayer(owner)["基础信息"].Value<string>("国家");
        world.RequirePlayer(owner)["基础信息"]["国家"] = "魏"; Check(!Allowed(), "enemy human named general cannot auto defend foreign city"); world.RequirePlayer(owner)["基础信息"]["国家"] = nation;
        world.EntityMappings["humanPlayers"][owner] = false; Check(!Allowed(), "unbound original identity cannot become human automatic defender"); world.EntityMappings["humanPlayers"][owner] = true;
        var list = new JArray(general.DeepClone()); string before = list.ToString(Formatting.None);
        var selector = typeof(CityGarrisonRules).GetMethods().Single(item => item.Name == "SelectNamedGuard" && item.GetParameters().Length == 4);
        var random = new CombatRandom(911032);
        JObject selected = (JObject)selector.Invoke(null, new object[] { list, 4, (Func<int, int, int>)random.Next, (Func<JObject, bool>)(_ => false) });
        Check(selected == null && list.ToString(Formatting.None) == before, "availability filters before original automatic troop refill can mutate foreign army");
        Console.WriteLine("CITY GUARD AVAILABILITY " + checks + " PASSED; only detached Native candidate modified");
    }

    public static void RunCapturedOwnerFollowup(string databasePath, string settledBattlePath)
    {
        if (!Path.GetFullPath(databasePath).Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Contains("audit", StringComparer.OrdinalIgnoreCase))
            throw new ArgumentException("Followup must use the actual ignored Native database.");
        string worldId = NativeWorldId(databasePath);
        BanditBattle previous = JObject.Parse(File.ReadAllText(settledBattlePath)).ToObject<BanditBattle>();
        var store = new SqliteWorldStore(databasePath);
        var bindings = store.ListRoles(worldId);
        var actors = bindings.Select(binding => new AuthenticatedActor(binding.AccountId, binding.PlayerId, worldId, "native-followup-" + binding.AccountId)).ToArray();
        AuthenticatedActor attacker = actors.Single(actor => actor.AccountId == "native-siege-account-1"), owner = actors.Single(actor => actor.AccountId == "native-siege-account-0");
        long now = Math.Max(previous.SettledUtcMs, ((JObject)store.Load(worldId).Data["战斗运行"]).Properties().Max(entry => entry.Value.Value<long>("SettledUtcMs"))) + 1000;
        int checks = 0; var events = new JArray();
        void Check(bool value, string name) { if (!value) throw new InvalidOperationException(name); checks++; Console.WriteLine("PASS " + name); }
        WorldRuntime Runtime()
        {
            var result = new WorldRuntime(store, actor => actor.IsSystem || actors.Any(real => real.AccountId == actor.AccountId && real.PlayerId == actor.PlayerId && real.ConnectionId == actor.ConnectionId), () => now);
            result.Register(new GeneralsModule()); result.Register(new CombatModule());
            result.Committed += committed => {
                foreach (GameEvent item in committed.Events)
                    events.Add(new JObject { ["requestId"] = committed.RequestId, ["revision"] = committed.WorldRevision,
                        ["event"] = JObject.FromObject(item), ["audiencePlayerIds"] = new JArray(item.AudiencePlayerIds ?? Array.Empty<string>()) });
            };
            return result;
        }
        WorldRuntime runtime = Runtime();
        WorldState Current() => store.Load(worldId);
        JObject City() => ((JArray)Current().Data["城池列表"]).OfType<JObject>().Single(city => city.Value<int>("坐标x") == previous.X && city.Value<int>("坐标y") == previous.Y);
        JObject Stored(string id) => (JObject)Current().Data["战斗运行"][id];
        GameCommand Command(string type, JObject payload) => new GameCommand { WorldId = worldId, RequestId = Guid.NewGuid().ToString("N"), Type = type, Payload = payload };
        GameResult Execute(AuthenticatedActor actor, GameCommand command, string label)
        {
            GameResult result = runtime.Execute(actor, command); Check(result.Code == GameCodes.Ok, label + " " + result.Code + " " + result.Message); return result;
        }
        string[] Ready(AuthenticatedActor actor, int count)
        {
            WorldState state = Current();
            return ((JObject)state.EntityMappings["generals"]).Properties().Where(entry => entry.Value.Value<string>("playerId") == actor.PlayerId)
                .Select(entry => new { Id = entry.Name, General = GeneralsModule.ResolveGeneral(state, actor.PlayerId, entry.Name, out _) })
                .Where(entry => state.EntityMappings["generalOccupancy"]?[entry.Id] == null && entry.General["详细信息"].Value<double>("状态") == 0
                    && entry.General["将领配兵"].Value<double>("数量") > 0 && entry.General["详细信息"].Value<double>("剩余体力") >= 5)
                .Take(count).Select(entry => entry.Id).ToArray();
        }
        Check(previous.Phase == "won" && Current().Data["城池占领结算"]?[previous.BattleId]?.Value<string>("attackerPlayerId") == owner.PlayerId
            && City().Value<int>("城主") == Current().ResolvePlayerIndex(owner.PlayerId), "target is actual kernel captured city from the prior committed victory");
        string[] attackIds = Ready(attacker, 5), defenseIds = Ready(owner, 2);
        Check(attackIds.Length > 0 && defenseIds.Length == 2, "original surviving paid armies ready without any fixture rewriting");
        string battleId = Execute(attacker, Command("combat.city.dispatch", new JObject { ["x"] = previous.X, ["y"] = previous.Y, ["generalIds"] = new JArray(attackIds) }),
            "other human attacks actual captured human owner city").Data.Value<string>("battleId");
        Check(Stored(battleId).Value<string>("CityOwnerPlayerId") == owner.PlayerId, "dispatch resolves actual conquest owner stable ID");
        store.Dispose(); store = new SqliteWorldStore(databasePath); runtime = Runtime(); now += 10000; runtime.Tick(worldId);
        Check(Stored(battleId).Value<string>("Phase") == "fighting" && Stored(battleId).Value<string>("CityOwnerPlayerId") == owner.PlayerId, "actual captured owner city survives restart and reaches original battle");
        string armyId = Execute(owner, Command("combat.city.garrison.dispatch", new JObject { ["x"] = previous.X, ["y"] = previous.Y, ["generalIds"] = new JArray(defenseIds) }),
            "actual conquest owner dispatches legitimate own DEF").Data.Value<string>("armyId");
        Execute(owner, Command("combat.city.garrison.withdraw", new JObject { ["armyId"] = armyId, ["generalId"] = defenseIds[0] }), "actual owner partial DEF return");
        now += 5000; runtime.Tick(worldId);
        Check(((JArray)Stored(battleId)["GarrisonArmies"]).OfType<JObject>().Single(army => army.Value<string>("ArmyId") == armyId).Value<string>("Phase") == "fighting",
            "actual captured owner remaining DEF joins original five second arrival");
        Execute(owner, Command("combat.city.garrison.withdraw", new JObject { ["armyId"] = armyId }), "actual owner complete DEF return");
        Execute(attacker, Command("combat.city.withdraw", new JObject { ["battleId"] = battleId }), "actual second attacker retreats original army");
        Check(Stored(battleId).Value<bool>("SettlementApplied") && Stored(battleId).Value<string>("Phase") == "withdrawn"
            && City().Value<int>("城主") == Current().ResolvePlayerIndex(owner.PlayerId), "return settles while keeping actual captured city owner");
        Check(attackIds.Concat(defenseIds).All(id => Current().EntityMappings["generalOccupancy"]?[id] == null), "actual captured city followup releases each real army occupancy");
        string data = Current().Data.ToString(Formatting.None), mappings = Current().EntityMappings.ToString(Formatting.None);
        Execute(attacker, Command("combat.city.withdraw", new JObject { ["battleId"] = battleId }), "duplicate actual attacker retreat");
        store.Dispose(); store = new SqliteWorldStore(databasePath); runtime = Runtime(); now += 10000; runtime.Tick(worldId);
        Check(Current().Data.ToString(Formatting.None) == data && Current().EntityMappings.ToString(Formatting.None) == mappings, "captured owner followup duplicate and SQLite restart do not reapply outcomes");
        File.WriteAllText(Path.Combine(Path.GetDirectoryName(databasePath), "captured-owner-followup.json"), new JObject { ["checks"] = checks,
            ["worldId"] = worldId, ["sourceBattleId"] = previous.BattleId, ["battleId"] = battleId, ["phase"] = "withdrawn",
            ["identity"] = "Native original-data identities; actual preceding Combat victory; no PHP identity or direct ownership writes in followup" }.ToString());
        File.WriteAllText(Path.Combine(Path.GetDirectoryName(databasePath), "actual-captured-owner-events.json"), events.ToString());
        store.Dispose(); Console.WriteLine("CAPTURED HUMAN OWNER FOLLOWUP " + checks + " PASSED");
    }

    public static void Run(string seedPath, string outputPath)
    {
        string directory = Path.Combine(Path.GetFullPath(outputPath), Guid.NewGuid().ToString("N"));
        if (!directory.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Contains("audit", StringComparer.OrdinalIgnoreCase))
            throw new ArgumentException("Native check data must remain in ignored audit.");
        Directory.CreateDirectory(directory);
        var seed = JObject.Parse(File.ReadAllText(seedPath));
        var world = new WorldState { WorldId = "native-player-siege-" + Guid.NewGuid().ToString("N"), Data = (JObject)seed.DeepClone(),
            EntityMappings = new JObject { ["players"] = new JObject() } };
        for (int index = 0; index < ((JArray)world.Data["玩家列表"]).Count; index++) world.EntityMappings["players"]["original-" + index] = index;
        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(); int checks = 0;
        void Check(bool condition, string name) { if (!condition) throw new InvalidOperationException(name); checks++; Console.WriteLine("PASS " + name); }
        var actors = new List<AuthenticatedActor>();
        string database = Path.Combine(directory, "world.sqlite3");
        var store = new SqliteWorldStore(database); store.ImportWorld(world);
        WorldRuntime Runtime()
        {
            var result = new WorldRuntime(store, actor => actor.IsSystem || actors.Any(owner => owner.AccountId == actor.AccountId
                && owner.PlayerId == actor.PlayerId && owner.ConnectionId == actor.ConnectionId), () => now, GeneralsModule.EnsureMappings);
            result.Register(new GeneralsModule()); result.Register(new CombatModule()); return result;
        }
        WorldRuntime runtime = Runtime();
        for (int account = 0; account < 4; account++)
        {
            string nation = account == 0 ? "汉" : "魏", accountId = "native-siege-account-" + account, connection = "native-siege-connection-" + account;
            Check(runtime.EnsureRole(accountId, world.WorldId, connection, "native-role-" + account, "攻守回归" + account, nation,
                LegacyWorldModule.CreatePlayer, out RoleBinding binding).Code == GameCodes.Ok, "real original role binding " + account);
            actors.Add(new AuthenticatedActor(accountId, binding.PlayerId, world.WorldId, connection));
        }
        WorldState Current() => store.Load(world.WorldId);
        BanditBattle Battle(string id) => Current().Data["战斗运行"][id].ToObject<BanditBattle>();
        string Owner(BanditBattle battle) => JObject.FromObject(battle).Value<string>("CityOwnerPlayerId");
        bool Human(CombatUnit unit) => JObject.FromObject(unit).Value<bool>("HumanOwner");
        JObject View(JObject battle, string viewer) => (JObject)typeof(BanditBattle).Assembly.GetType("Dwsg.Shared.Combat.CombatBattleProjection", true)
            .GetMethod("ProjectBattle").Invoke(null, new object[] { battle, viewer });
        string GeneralId(AuthenticatedActor actor, int legacy) => ((JObject)Current().EntityMappings["generals"]).Properties()
            .Single(entry => entry.Value.Value<string>("playerId") == actor.PlayerId && entry.Value.Value<int>("legacyId") == legacy).Name;
        GameCommand Command(string type, JObject payload) => new GameCommand { WorldId = world.WorldId, RequestId = Guid.NewGuid().ToString("N"), Type = type, Payload = payload };
        GameResult Execute(AuthenticatedActor actor, GameCommand command, string label)
        {
            GameResult result = runtime.Execute(actor, command); Check(result.Code == GameCodes.Ok, label + " " + result.Code + " " + result.Message); return result;
        }
        void Rejected(AuthenticatedActor actor, GameCommand command, string label)
        {
            WorldState before = Current(); GameResult result = runtime.Execute(actor, command); WorldState after = Current();
            Check(result.Code != GameCodes.Ok && before.Revision == after.Revision && JToken.DeepEquals(before.Data, after.Data)
                && JToken.DeepEquals(before.EntityMappings, after.EntityMappings), label + " rolls back whole candidate");
        }
        // Only this Native regression can prepare mature inventories/ownership; there is no production command for it.
        void Prepare(Action<WorldState> action, string label)
        {
            WorldState candidate = Current(); long previous = candidate.Revision; action(candidate); candidate.Revision++;
            var command = Command("native.test.prepare", new JObject { ["label"] = label });
            GameResult result = GameResult.Success(); result.RequestId = command.RequestId; result.WorldId = world.WorldId; result.WorldRevision = candidate.Revision;
            Check(store.Commit(new WorldCommit { Actor = AuthenticatedActor.System(world.WorldId), Candidate = candidate, ExpectedRevision = previous,
                Receipt = new CommandReceipt { WorldId = world.WorldId, PlayerId = "server", RequestId = command.RequestId,
                    Fingerprint = CommandFingerprint.Calculate(command), ResultJson = JsonConvert.SerializeObject(result) } }).Code == GameCodes.Ok, "Native preparation " + label);
        }
        int targetX = 0, targetY = 0;
        Prepare(candidate => {
            var cities = ((JArray)candidate.Data["城池列表"]).OfType<JObject>().ToArray();
            var lookup = cities.ToDictionary(city => Tuple.Create(city.Value<int>("坐标x"), city.Value<int>("坐标y")));
            JObject origin = (JObject)candidate.RequirePlayer(actors[0].PlayerId)["封地信息表"][0]["所在城池"];
            int Cost(JObject source, JObject target, string nation) => new CityMarchRules(全局大地图库.大地图表,
                (x, y) => lookup[Tuple.Create(x, y)].Value<string>("国家") == nation)
                .Find(source.Value<int>("x") - 1, source.Value<int>("y") - 1, target.Value<int>("坐标x") - 1, target.Value<int>("坐标y") - 1);
            JObject target = cities.First(city => city.Value<int>("规模") == 0 && city.Value<string>("国家") != "汉" && Cost(origin, city, "汉") >= 0);
            targetX = target.Value<int>("坐标x"); targetY = target.Value<int>("坐标y");
            target["国家"] = "魏"; target["城主"] = candidate.ResolvePlayerIndex(actors[1].PlayerId);
            JObject country = ((JArray)candidate.Data["国家列表"]).OfType<JObject>().Single(nation => nation.Value<string>("国号") == "魏");
            country["国王"] = candidate.ResolvePlayerIndex(actors[1].PlayerId);
            JObject defenseOrigin = cities.First(city => city.Value<int>("规模") == 0 && !ReferenceEquals(city, target) && Cost(new JObject {
                ["x"] = city["坐标x"], ["y"] = city["坐标y"] }, target, "魏") >= 0);
            defenseOrigin["国家"] = "魏"; defenseOrigin["城主"] = candidate.ResolvePlayerIndex(actors[1].PlayerId);
            var random = new CombatRandom(91029304);
            for (int account = 0; account < 3; account++)
            {
                JObject player = candidate.RequirePlayer(actors[account].PlayerId), fief = (JObject)player["封地信息表"][0];
                if (account > 0)
                {
                    GameResult moved = TerritoryRules.MoveOriginalFief(candidate, candidate.ResolvePlayerIndex(actors[account].PlayerId), fief.Value<int>("ID"),
                        defenseOrigin.Value<int>("坐标x"), defenseOrigin.Value<int>("坐标y"));
                    if (moved.Code != GameCodes.Ok) throw new InvalidOperationException("Native original fief move failed: " + moved.Message);
                }
                string[] advanced = { "仓储", "安置", "格斗", "精准", "驯马", "精工" };
                foreach (JProperty technology in ((JObject)player["科技信息"]).Properties())
                    technology.Value = technology.Name == "工程设计" ? 15.0 : advanced.Contains(technology.Name) ? 5.0 : 10.0;
                int count = account == 0 ? 25 : 5;
                for (int number = 1; number <= count; number++)
                {
                    JObject definition = ((JArray)seed["将领配置"]).OfType<JObject>().Single(item => item.Value<int>("ID") == (number % 2 == 0 ? 5 : 6));
                    JObject general = GeneralCreationRules.CreateBanditGeneral(definition, random.Next); general["ID"] = number;
                    GeneralExperienceRules.Add(general, GeneralExperienceRules.TotalForLevel(99));
                    ((JArray)fief["将领信息表"]).Add(general);
                    if (account == 0) LegacyGenerals.Equipment(player, 0).Add(new JObject { ["将领ID"] = -1, ["品质"] = 4.0,
                        ["强化等级"] = 0.0, ["强化值"] = 0.0, ["保底次数"] = 0.0, ["已强化次数"] = 20.0, ["炼魂属性"] = new JArray(),
                        ["装备信息"] = ((JArray)seed["装备配置"]).OfType<JObject>().First(item => item.Value<string>("类型") == "头盔").DeepClone() });
                }
                player["将领ID标识"] = count + 1;
                foreach (int type in new[] { 304, 403 }) ((JArray)fief["闲兵信息表"]).Add(new JObject { ["ID"] = type, ["数量"] = 500000.0 });
                if (GeneralAttributeRules.Recalculate(player, now / 1000)) throw new InvalidOperationException("Native mature role violates original attribute caps");
            }
            // One original configured name, not the owner's whole collection, becomes an automatic guard.
            JObject named = ((JArray)candidate.Data["玩家列表"]).OfType<JObject>().Take(17).SelectMany(player =>
                ((JArray)player["封地信息表"]).OfType<JObject>().SelectMany(fief => ((JArray)fief["将领信息表"]).OfType<JObject>()))
                .First(general => general["将领属性"]["初始属性"].Value<string>("系列") == "名将");
            int configurationId = named["将领属性"]["初始属性"].Value<int>("ID");
            JObject transferred = (JObject)named.DeepClone(); named.Remove(); transferred["ID"] = 6;
            transferred["详细信息"]["状态"] = 0.0; transferred["详细信息"]["身份"] = candidate.ResolvePlayerIndex(actors[1].PlayerId);
            ((JArray)candidate.RequirePlayer(actors[1].PlayerId)["封地信息表"][0]["将领信息表"]).Add(transferred);
            candidate.RequirePlayer(actors[1].PlayerId)["将领ID标识"] = 7;
            if (GeneralAttributeRules.Recalculate(candidate.RequirePlayer(actors[1].PlayerId), now / 1000))
                throw new InvalidOperationException("Native original named guard violates human caps");
            ((JArray)target["城池驻防列表"]).Add(new JObject { ["第几个玩家"] = candidate.ResolvePlayerIndex(actors[1].PlayerId), ["将领ID标识"] = configurationId });
            GeneralsModule.EnsureMappings(candidate);
        }, "original cities and legal mature test armies");
        Check(((JArray)Current().Data["城池列表"]).Count == 942 && ((JArray)Current().Data["山贼列表"]).Count == 400, "original world unchanged in size");
        for (int account = 0; account < 3; account++)
        {
            int count = account == 0 ? 25 : 5;
            for (int number = 1; number <= count; number++)
            {
                if (account == 0)
                {
                    string equipmentId = ((JObject)Current().EntityMappings["equipment"]).Properties().Single(entry => entry.Value.Value<string>("playerId") == actors[0].PlayerId
                        && entry.Value.Value<int>("slot") == 0 && entry.Value.Value<int>("legacyIndex") == number - 1).Name;
                    Execute(actors[0], Command("generals.equip", new JObject { ["generalId"] = GeneralId(actors[0], number), ["equipmentId"] = equipmentId }), "original helmet equip " + number);
                }
                JObject fief;
                JObject general = GeneralsModule.ResolveGeneral(Current(), actors[account].PlayerId, GeneralId(actors[account], number), out fief);
                Execute(actors[account], Command("generals.allocateTroops", new JObject { ["generalId"] = GeneralId(actors[account], number),
                    ["troopTypeId"] = account == 0 && number >= 21 ? 403 : 304, ["count"] = general["将领属性"]["最终属性"].Value<int>("统兵") }), "original army allocation " + account + ":" + number);
            }
        }
        JObject Coordinates(params int[] legacyIds) => new JObject { ["x"] = targetX, ["y"] = targetY, ["generalIds"] = new JArray(legacyIds.Select(number => GeneralId(actors[0], number))) };
        GameCommand Dispatch(params int[] ids) => Command("combat.city.dispatch", Coordinates(ids));
        void Tick()
        {
            foreach (GameCommand due in new CombatModule().CollectDueCommands(Current(), now).ToArray())
            {
                GameResult result = runtime.Execute(AuthenticatedActor.System(world.WorldId), due);
                if (result.Code == GameCodes.Conflict && Battle(due.Payload.Value<string>("battleId")).NextTickUtcMs != due.Payload.Value<long>("tickUtcMs")) continue;
                if (result.Code != GameCodes.Ok) throw new InvalidOperationException("Actual combat tick failed: " + result.Code + " " + result.Message);
            }
        }
        void Reopen() { store.Dispose(); store = new SqliteWorldStore(database); runtime = Runtime(); }
        JObject City(WorldState state) => ((JArray)state.Data["城池列表"]).OfType<JObject>().Single(city => city.Value<int>("坐标x") == targetX && city.Value<int>("坐标y") == targetY);
        // Two in-flight armies must both return if the current target becomes friendly before arrival.
        Prepare(candidate => { City(candidate)["国家"] = "魏"; City(candidate)["城主"] = candidate.ResolvePlayerIndex(actors[1].PlayerId); }, "restore original city to a human test owner");
        string canceled1 = Execute(actors[0], Dispatch(1, 2), "first next native march").Data.Value<string>("battleId");
        string canceled2 = Execute(actors[0], Dispatch(3, 4), "second next native march").Data.Value<string>("battleId");
        Prepare(candidate => { City(candidate)["国家"] = "汉"; City(candidate)["城主"] = candidate.ResolvePlayerIndex(actors[0].PlayerId); }, "actual in-flight owner and nation change");
        Reopen(); now += 10000; Tick();
        Check(Battle(canceled1).Phase == "withdrawn" && Battle(canceled2).Phase == "withdrawn" && Battle(canceled1).SettlementApplied && Battle(canceled2).SettlementApplied,
            "both same city arrivals return when owner changes to self");
        Check(Owner(Battle(canceled1)) == null && Owner(Battle(canceled2)) == null
            && new[] { 1, 2, 3, 4 }.All(number => Current().EntityMappings["generalOccupancy"]?[GeneralId(actors[0], number)] == null), "canceled marches release real armies and previous owner access");
        Prepare(candidate => { City(candidate)["国家"] = "魏"; City(candidate)["城主"] = candidate.ResolvePlayerIndex(actors[1].PlayerId); }, "restore human enemy target after cancellation");
        Rejected(actors[1], Dispatch(1), "foreign attacker generals");
        var forged = Coordinates(1); forged["ownerPlayerId"] = actors[1].PlayerId;
        Rejected(actors[0], Command("combat.city.dispatch", forged), "client owner injection");
        Rejected(actors[0], Command("combat.city.advance", new JObject { ["battleId"] = "forged", ["tickUtcMs"] = now }), "client victory or progress");
        var dispatch = Dispatch(1, 2, 3, 4, 5);
        string battleId = Execute(actors[0], dispatch, "bound human owned original city dispatch").Data.Value<string>("battleId");
        string dispatchedBattleId = battleId;
        Check(Owner(Battle(battleId)) == actors[1].PlayerId && Battle(battleId).Phase == "marching", "server records actual owner without a fake army");
        Execute(actors[0], Dispatch(6, 7, 8, 9, 10), "same city second real army dispatch");
        string kingBefore = Current().RequirePlayer(actors[1].PlayerId)["科技信息"].ToString(Formatting.None);
        Reopen(); now += 10000; Tick();
        BanditBattle active = Battle(battleId);
        if (active.Phase == "joined") { battleId = active.JoinBattleId; active = Battle(battleId); }
        Check(active.Phase == "fighting" && active.Attackers.Count == 10, "same city arrivals join one actual battlefield");
        Check(active.Defenders.Count(Human) == 1 && active.Defenders.Any(unit => Human(unit) && unit.GeneralId == GeneralId(actors[1], 6)), "only configured original human named guard joins");
        Check(Current().RequirePlayer(actors[1].PlayerId)["科技信息"].ToString(Formatting.None) == kingBefore, "original temporary militia does not overwrite human king technology");
        Check(active.Defenders.Count(unit => unit.Ephemeral) >= 20 && active.Defenders.Count(unit => unit.Ephemeral) < 30 && active.Wall == 200000, "same original player attack militia and wall rules");
        JObject saved = JObject.FromObject(active), attackerView = View(saved, actors[0].PlayerId), ownerView = View(saved, actors[1].PlayerId);
        Check(ownerView != null && View(saved, actors[3].PlayerId) == null, "owner participates but unrelated country member cannot see private battle");
        Check(((JArray)attackerView["Defenders"]).OfType<JObject>().Where(unit => unit.Value<bool>("HumanOwner")).All(unit => unit["General"]["将领培养"] == null), "foreign human named guard cultivation hidden");
        Check(((JArray)ownerView["Attackers"]).OfType<JObject>().All(unit => unit["General"]["将领培养"] == null) && ownerView["Reward"] == null, "owner cannot read foreign army cultivation or attacker reward");
        string guardId = GeneralId(actors[1], 6);
        Check(Current().EntityMappings["generalOccupancy"][guardId].Value<string>("armyId") == battleId, "real named defender canonical occupancy held");
        Execute(actors[0], Command("combat.city.reinforce", new JObject { ["battleId"] = battleId, ["generalIds"] = new JArray(Enumerable.Range(11, 5).Select(number => GeneralId(actors[0], number))) }), "existing attacker reinforce kernel");
        var defensePayload = new JObject { ["x"] = targetX, ["y"] = targetY, ["generalIds"] = new JArray(GeneralId(actors[1], 1), GeneralId(actors[1], 2)) };
        string defenseArmy = Execute(actors[1], Command("combat.city.garrison.dispatch", defensePayload), "actual human owner DEF dispatch").Data.Value<string>("armyId");
        Rejected(actors[3], Command("combat.city.garrison.withdraw", new JObject { ["armyId"] = defenseArmy }), "foreign DEF withdrawal");
        Execute(actors[1], Command("combat.city.garrison.withdraw", new JObject { ["armyId"] = defenseArmy, ["generalId"] = GeneralId(actors[1], 1) }), "own pending single DEF withdraw");
        now += 5000; Tick();
        Check(((JArray)Current().Data["战斗运行"][battleId]["GarrisonArmies"]).OfType<JObject>().Single(army => army.Value<string>("ArmyId") == defenseArmy)
            .Value<string>("Phase") == "fighting", "remaining real DEF joins after original five seconds");
        Execute(actors[1], Command("combat.city.garrison.withdraw", new JObject { ["armyId"] = defenseArmy }), "own complete DEF withdraw");
        Execute(actors[1], Command("combat.city.garrison.withdraw", new JObject { ["armyId"] = defenseArmy }), "duplicate DEF withdraw receipt safe");
        Execute(actors[0], Command("combat.city.reinforce", new JObject { ["battleId"] = battleId, ["generalIds"] = new JArray(Enumerable.Range(16, 10).Select(number => GeneralId(actors[0], number)).Take(5)) }), "original infantry reinforcement");
        Execute(actors[0], Command("combat.city.reinforce", new JObject { ["battleId"] = battleId, ["generalIds"] = new JArray(Enumerable.Range(21, 5).Select(number => GeneralId(actors[0], number))) }), "actual original siege carts reinforcement");
        Reopen(); int advances = 0;
        while (!Battle(battleId).SettlementApplied && advances++ < 600) { now += 5000; Tick(); }
        BanditBattle settled = Battle(battleId);
        Check(settled.SettlementApplied && settled.Phase == "won", "whole original kernel establishes human city victory");
        Check(City(Current()).Value<int>("城主") == Current().ResolvePlayerIndex(actors[0].PlayerId) && City(Current()).Value<string>("国家") == "汉", "original city conquest changes single authoritative owner");
        Check(Current().Data["城池占领结算"]?[battleId] != null && Current().EntityMappings["generalOccupancy"]?[guardId] == null, "city migration ledger and defender occupancy settle once");
        Check(settled.Attackers.Any(unit => unit.OriginalQuantity > unit.Remaining) && settled.Defenders.Any(unit => Human(unit) && unit.OriginalQuantity > unit.Remaining), "actual kernel causes both attacker and real named defender casualties");
        string afterVictory = Current().Data.ToString(Formatting.None), afterMappings = Current().EntityMappings.ToString(Formatting.None);
        var replay = runtime.Execute(actors[0], dispatch);
        Check(replay.Data.Value<string>("battleId") == dispatchedBattleId && Current().Data.ToString(Formatting.None) == afterVictory, "original dispatch duplicate cannot repeat victory or rewards");
        Reopen(); now += 100000; runtime.Tick(world.WorldId);
        Check(Current().Data.ToString(Formatting.None) == afterVictory && Current().EntityMappings.ToString(Formatting.None) == afterMappings, "SQLite restart cannot repeat losses rewards or conquest");
        File.WriteAllText(Path.Combine(directory, "settled-battle.json"), JObject.FromObject(settled).ToString());
        File.WriteAllText(Path.Combine(directory, "result.json"), new JObject { ["checks"] = checks, ["battleId"] = battleId, ["phase"] = settled.Phase,
            ["identity"] = "Native original-data test identities; no PHP human account or natural victory claim", ["clock"] = "controlled UTC catch-up" }.ToString());
        store.Dispose(); Console.WriteLine("PLAYER CITY SIEGE RUNTIME CHECKS " + checks + " PASSED " + directory);
    }
}
