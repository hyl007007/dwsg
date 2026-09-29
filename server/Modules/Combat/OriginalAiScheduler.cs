using System;
using System.Collections.Generic;
using System.Linq;
using Dwsg.Server.Modules.Generals;
using Dwsg.Shared;
using Dwsg.Shared.Combat;
using Newtonsoft.Json.Linq;

namespace Dwsg.Server.Modules.Combat
{
    public sealed partial class CombatModule
    {
        public const string OriginalAiTick = "combat.city.ai.tick";

        public IEnumerable<GameCommand> CollectOriginalAiCommands(WorldState world, long utcMs)
        {
            if (FirstAiReference(world) == null) yield break;
            JObject schedule = world.Data["原AI推城"] as JObject;
            int count = ((JArray)world.Data["国家列表"]).Count;
            bool initialize = schedule == null || ((JArray)schedule["各国下次出手UtcMs"]).Count != count;
            long due = initialize ? utcMs / 1000 * 1000 : schedule.Value<long>("NextTickUtcMs");
            if (due > utcMs) yield break;
            yield return new GameCommand { WorldId = world.WorldId, Type = OriginalAiTick,
                RequestId = "combat:ai:" + world.Revision + ":" + due,
                Payload = new JObject { ["tickUtcMs"] = due, ["initialize"] = initialize } };
        }

        // 接入现有Combat.Execute的候选路径；无需另建Host、定时器或数据库。
        public GameResult ExecuteOriginalAi(WorldState candidate, CommandContext context, GameCommand command)
        {
            if (context?.Actor == null || !context.Actor.IsSystem || context.Actor.WorldId != candidate.WorldId
                || command.WorldId != candidate.WorldId || command.Type != OriginalAiTick)
                return GameResult.Reject(GameCodes.Forbidden, "原AI只能由本世界服务器调度");
            if (!Keys(command.Payload, "tickUtcMs", "initialize") || command.Payload["tickUtcMs"].Type != JTokenType.Integer
                || command.Payload["initialize"].Type != JTokenType.Boolean)
                return GameResult.Reject(GameCodes.InvalidArgument, "原AI排期参数无效");
            WorldState world = candidate.Clone();
            string reference = FirstAiReference(world);
            if (reference == null) return GameResult.Reject(GameCodes.Conflict, "原AI等待本世界首个真人角色");
            JObject schedule = world.Data["原AI推城"] as JObject;
            JArray nations = (JArray)world.Data["国家列表"];
            bool initialize = schedule == null || ((JArray)schedule["各国下次出手UtcMs"]).Count != nations.Count;
            long now = context.ServerUtcMs / 1000 * 1000, expected = initialize ? now : schedule.Value<long>("NextTickUtcMs");
            if (command.Payload.Value<bool>("initialize") != initialize || command.Payload.Value<long>("tickUtcMs") != expected
                || expected > context.ServerUtcMs)
                return GameResult.Reject(GameCodes.Conflict, "原AI排期未到或已提交");
            if (schedule == null)
                world.Data["原AI推城"] = schedule = new JObject { ["ReferencePlayerId"] = reference,
                    ["RandomState"] = Seed(), ["上次出手UtcMs"] = 0L, ["各国下次出手UtcMs"] = new JArray(), ["军情"] = new JObject() };
            if (schedule.Value<string>("ReferencePlayerId") != reference)
                return GameResult.Reject(GameCodes.Conflict, "原AI首个真人参考映射已改变");
            var random = new CombatRandom(schedule["RandomState"].ToObject<ulong>());
            JArray deadlines = (JArray)schedule["各国下次出手UtcMs"];
            while (deadlines.Count > nations.Count) deadlines.RemoveAt(deadlines.Count - 1);
            while (deadlines.Count < nations.Count) deadlines.Add(checked(now + random.Next(0, OriginalAiRules.MaximumIntervalSeconds) * 1000L));
            var result = GameResult.Success();
            long last = schedule.Value<long>("上次出手UtcMs");
            for (int index = 0; index < nations.Count; index++)
            {
                if (deadlines[index].Value<long>() > now || now - last < OriginalAiRules.StaggerSeconds * 1000L) continue;
                deadlines[index] = checked(now + random.Next(OriginalAiRules.MinimumIntervalSeconds, OriginalAiRules.MaximumIntervalSeconds) * 1000L);
                schedule["上次出手UtcMs"] = last = now;
                result = DispatchOriginalAi(world, context, (JObject)nations[index], reference, now, random);
                if (result.Code != GameCodes.Ok) return result;
                result.Data["countryIndex"] = index;
                break; // 原同帧循环受10秒全局出手间隔限制，最多一个国家出手。
            }
            schedule["RandomState"] = random.State;
            schedule["NextTickUtcMs"] = nations.Count == 0 ? checked(now + OriginalAiRules.MaximumIntervalSeconds * 1000L)
                : deadlines.Values<long>().Min(due => Math.Max(due, checked(last + OriginalAiRules.StaggerSeconds * 1000L)));
            candidate.Data = world.Data; candidate.EntityMappings = world.EntityMappings;
            return result;
        }

        private static string FirstAiReference(WorldState world)
        {
            // EnsureRole追加原player index；WorldRoleRegistration先以持久roles核验humanPlayers。
            // 依角色创建顺序选首个真人，与字典枚举、国家排序、在线连接无关。
            return (world.EntityMappings["humanPlayers"] as JObject)?.Properties().Where(entry => entry.Value.Value<bool>())
                .OrderBy(entry => world.ResolvePlayerIndex(entry.Name)).FirstOrDefault()?.Name;
        }

        private static GameResult DispatchOriginalAi(WorldState world, CommandContext context, JObject nation,
            string reference, long now, CombatRandom random)
        {
            var cities = (JArray)world.Data["城池列表"];
            var lookup = cities.OfType<JObject>().ToDictionary(city => Tuple.Create(city.Value<int>("坐标x"), city.Value<int>("坐标y")));
            string country = nation.Value<string>("国号");
            var path = new CityMarchRules(全局大地图库.大地图表, (x, y) => lookup[Tuple.Create(x, y)].Value<string>("国家") == country);
            int target = OriginalAiRules.SelectTarget(cities, nation, path.Find, random.Next);
            if (target < 0) return GameResult.Success(new JObject { ["dispatch"] = "no_reachable_city" });
            JObject city = (JObject)cities[target];
            string owner = NpcPlayerId(world, nation.Value<int>("国王"));
            GameResult targetValidation = ValidateCitySiegeTarget(world, owner, city);
            if (targetValidation.Code != GameCodes.Ok) return targetValidation;
            JObject player = CityMilitiaPlayer(world, owner);
            var battle = new BanditBattle { Kind = "city", BattleId = "battle_" + Guid.NewGuid().ToString("N"),
                ArmyId = "army_" + Guid.NewGuid().ToString("N"), PlayerId = owner, X = city.Value<int>("坐标x"), Y = city.Value<int>("坐标y"),
                ArrivalUtcMs = checked(now + OriginalAiRules.ArrivalSeconds * 1000L), NextTickUtcMs = checked(now + OriginalAiRules.ArrivalSeconds * 1000L),
                CityName = city.Value<string>("名称"), CityNation = city.Value<string>("国家"), CityScale = city.Value<int>("规模"),
                CityOwnerPlayerId = CityHumanDefender(world, city),
                AttackFormationX = -40.25f, DefenseFormationX = -24f, RandomState = random.State };
            int count = OriginalAiRules.GeneralCount(battle.CityScale);
            for (int index = 0; index < count; index++)
            {
                JObject general = CityGarrisonRules.CreateMilitiaGeneral(99, player, (JArray)world.Data["将领配置"],
                    (JObject)world.Data["姓名配置"], world.Data.Value<double>("难度"), now / 1000, random.Next);
                general["将领配兵"]["数量"] = OriginalAiRules.TroopCount(battle.CityScale, world.Data.Value<double>("难度"),
                    world.RequirePlayer(reference)["科技信息"].Value<double>("统帅能力"), general["将领配兵"].Value<int>("ID"), random.Next);
                string army = battle.ArmyId + ":" + index / 5;
                CombatUnit unit = BanditBattleRules.CreateUnit(battle.BattleId + ":ai:" + index, general, Troop(world, general["将领配兵"].Value<int>("ID")), 0);
                unit.Ephemeral = true; unit.GeneralOwnerId = owner; unit.ArmyId = army; unit.UnitId = army + ":" + index;
                battle.Attackers.Add(unit);
                if (index % 5 == 0) battle.AttackFormations.Add(new CombatFormation { ArmyId = army,
                    AvailableUtcMs = battle.ArrivalUtcMs, PositionX = battle.AttackFormationX });
            }
            battle.RandomState = random.State;
            Save(world, battle);
            world.Data["原AI推城"]["军情"][battle.BattleId] = new JObject { ["ReferencePlayerId"] = reference,
                ["Country"] = country, ["身份"] = 666, ["战场类型"] = 1 };
            // 该系统命令不广播临时将领；公共摘要/所属王的私有战场仍由现有授权投影处理。
            return GameResult.Success(new JObject { ["dispatch"] = "marching", ["battleId"] = battle.BattleId,
                ["arrivalUtcMs"] = battle.ArrivalUtcMs });
        }

        private static bool IsOriginalAiBattle(WorldState world, BanditBattle battle) => world.Data["原AI推城"]?["军情"]?[battle.BattleId] is JObject;

        // Combat.StartCityBattle唯一接入点；原AI抵达首次开战不走玩家主动攻城的驻防抽选。
        private static void StartOriginalAiCityBattle(WorldState world, BanditBattle battle)
        {
            JObject city = City(world, battle.X, battle.Y);
            battle.CityOwnerPlayerId = null;
            if (ValidateCitySiegeTarget(world, battle.PlayerId, city).Code != GameCodes.Ok)
            { battle.Phase = "withdrawn"; return; }
            battle.CityOwnerPlayerId = CityHumanDefender(world, city);
            battle.Phase = "fighting"; battle.StartedUtcMs = battle.ArrivalUtcMs;
            battle.NextTickUtcMs = checked(battle.StartedUtcMs + BanditBattleRules.TickMilliseconds);
            battle.CityName = city.Value<string>("名称"); battle.CityNation = city.Value<string>("国家"); battle.CityScale = city.Value<int>("规模");
            battle.Wall = city.Value<double>("城墙"); battle.WallMaximum = CityGarrisonRules.WallMaximum(battle.CityScale);
            var random = new CombatRandom(battle.RandomState);
            JObject country = ((JArray)world.Data["国家列表"]).OfType<JObject>().FirstOrDefault(item => item.Value<string>("国号") == battle.CityNation);
            battle.NpcPlayerId = NpcPlayerId(world, country?.Value<int>("国王") ?? 2);
            JObject player = CityMilitiaPlayer(world, battle.NpcPlayerId);
            string reference = world.Data["原AI推城"]["军情"][battle.BattleId].Value<string>("ReferencePlayerId");
            string name = world.RequirePlayer(reference)["基础信息"].Value<string>("名字");
            var defenders = new List<CombatUnit>();
            foreach (JObject general in OriginalAiRules.CreateMilitia(battle.CityScale, name, random.Next,
                level => CityGarrisonRules.CreateMilitiaGeneral(level, player, (JArray)world.Data["将领配置"],
                    (JObject)world.Data["姓名配置"], world.Data.Value<double>("难度"), battle.StartedUtcMs / 1000, random.Next)))
            {
                CombatUnit unit = BanditBattleRules.CreateUnit(battle.BattleId + ":ai-militia:" + defenders.Count,
                    general, Troop(world, general["将领配兵"].Value<int>("ID")), 1);
                unit.GeneralOwnerId = battle.NpcPlayerId; unit.Ephemeral = true; defenders.Add(unit);
            }
            random.Next(0, 101); // 原num20未使用，仍保留随机消费顺序。
            // 原只追加城池玩家驻防列表，不按城池把所有真人将领自动拉入。
            foreach (JObject listed in ((JArray)city["城池玩家驻防列表"]).OfType<JObject>())
            {
                string owner = ResolveCityPlayer(world, listed["详细信息"].Value<int>("身份"));
                if (owner == null) continue;
                string stableId = ((JObject)world.EntityMappings["generals"]).Properties().Single(entry =>
                    entry.Value.Value<string>("playerId") == owner && entry.Value.Value<int>("legacyId") == listed.Value<int>("ID")).Name;
                JObject general = GeneralsModule.ResolveGeneral(world, owner, stableId, out _);
                if (!CanUseCityGuard(world, battle, owner, general) || defenders.Any(unit => !unit.Ephemeral && unit.GeneralId == stableId)) continue;
                CombatUnit unit = BanditBattleRules.CreateUnit(stableId, general, Troop(world, general["将领配兵"].Value<int>("ID")), 1);
                unit.GeneralOwnerId = owner; unit.HumanOwner = IsHumanCityPlayer(world, owner); defenders.Add(unit);
            }
            battle.Defenders = defenders;
            ReserveDefenders(world, battle);
            long[] available = CityGarrisonRules.FormationAvailability(defenders.Count, battle.ArrivalUtcMs, random.Next);
            for (int group = 0; group < available.Length; group++)
            {
                string army = battle.BattleId + ":defense:" + group;
                foreach (CombatUnit unit in defenders.Skip(group * 5).Take(5)) { unit.ArmyId = army; unit.UnitId = army + ":" + unit.GeneralId; }
                battle.DefenseFormations.Add(new CombatFormation { ArmyId = army, AvailableUtcMs = available[group], PositionX = battle.DefenseFormationX });
            }
            // 原新prefab守方兵力=0，加入战场在同帧渲染Update前捕获0；不能用待入队伍总量代替。
            City(world, battle.X, battle.Y)["正在交战"] = true; // 预留守将会替换candidate.Data，重新取得当前城池。
            battle.RandomState = random.State;
        }

        // 原AI军情加入同坐标既有战场；不同国的临时军队也加入原攻方，原先攻身份保留。
        private static bool JoinOriginalAiBattle(WorldState world, BanditBattle joining, BanditBattle target, long utcMs)
        {
            if (!IsOriginalAiBattle(world, joining) || target == null || target.ArrivalUtcMs > joining.ArrivalUtcMs) return false;
            if (target.Phase == "marching") StartBattle(world, target);
            if (target.Phase != "fighting") return false;
            BanditBattleRules.EnsureFormations(target);
            target.Attackers.AddRange(joining.Attackers); target.AttackFormations.AddRange(joining.AttackFormations);
            joining.Phase = "joined"; joining.SettlementApplied = true; joining.SettledUtcMs = utcMs; joining.JoinBattleId = target.BattleId;
            joining.CityOwnerPlayerId = target.CityOwnerPlayerId;
            joining.Reward = new 战斗奖励(); Save(world, joining); Save(world, target);
            return true;
        }
    }
}
