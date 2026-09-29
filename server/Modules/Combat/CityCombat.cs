using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Dwsg.Shared;
using Dwsg.Shared.Combat;
using Dwsg.Shared.Economy;
using Dwsg.Shared.Generals;
using Dwsg.Server.Modules.Generals;
using Dwsg.Server.World;
using Dwsg.Server.Chat;
using Newtonsoft.Json.Linq;

namespace Dwsg.Server.Modules.Combat
{
    public sealed partial class CombatModule
    {
        private static GameResult DispatchCity(WorldState world, CommandContext context, JObject payload)
        {
            bool precise = payload?["arrivalUtcMs"] != null;
            if (!Keys(payload, precise ? new[] { "x", "y", "generalIds", "arrivalUtcMs" } : new[] { "x", "y", "generalIds" })
                || !Coordinate(payload["x"], out int x) || !Coordinate(payload["y"], out int y)
                || !(payload["generalIds"] is JArray ids) || ids.Count < 1 || ids.Count > 5
                || ids.Any(id => !Id(id, out _)) || ids.Values<string>().Distinct(StringComparer.Ordinal).Count() != ids.Count)
                return GameResult.Reject(GameCodes.InvalidArgument, "请选择1至5名不同将领和有效城池坐标");
            JObject city = City(world, x, y);
            if (city == null) return GameResult.Reject(GameCodes.NotFound, "城池不存在");
            if (!NpcCity(world, city)) return GameResult.Reject(GameCodes.Conflict, "当前只开放进攻原NPC城池");
            JObject player = world.RequirePlayer(context.Actor.PlayerId);
            if (city.Value<string>("国家") == player["基础信息"].Value<string>("国家"))
                return GameResult.Reject(GameCodes.Conflict, "不能进攻本国城池");
            BanditBattle existing = ActiveAt(world, x, y, kind: "city");
            if (existing != null && existing.PlayerId != context.Actor.PlayerId)
                return GameResult.Reject(GameCodes.Conflict, "该城池已有其他玩家出征");
            string[] generalIds = ids.Values<string>().ToArray();
            JObject sourceFief;
            GeneralsModule.ResolveGeneral(world, context.Actor.PlayerId, generalIds[0], out sourceFief);
            JObject source = (JObject)sourceFief["所在城池"];
            int sourceX = source.Value<int>("x"), sourceY = source.Value<int>("y");
            var cities = ((JArray)world.Data["城池列表"]).OfType<JObject>().ToDictionary(item => Tuple.Create(item.Value<int>("坐标x"), item.Value<int>("坐标y")));
            string nation = player["基础信息"].Value<string>("国家");
            int path = new CityMarchRules(全局大地图库.大地图表, (cx, cy) => cities[Tuple.Create(cx, cy)].Value<string>("国家") == nation)
                .Find(sourceX - 1, sourceY - 1, x - 1, y - 1);
            if (path < 0 && player["基础信息"].Value<string>("名字") != "997788")
                return GameResult.Reject(GameCodes.Conflict, "原地图没有可达的出征路线");
            long arrival = checked(context.ServerUtcMs + 10000);
            if (precise)
            {
                if (payload["arrivalUtcMs"].Type != JTokenType.Integer || !long.TryParse(payload["arrivalUtcMs"].ToString(), out long requested))
                    return GameResult.Reject(GameCodes.InvalidArgument, "精确到达时间无效");
                // 原输入是TIME(now+10)所在本地日期的HH:mm:ss；过去时间仍可立即到达。
                DateTime localDate = DateTimeOffset.FromUnixTimeMilliseconds(arrival).LocalDateTime.Date;
                long start = new DateTimeOffset(localDate).ToUnixTimeMilliseconds();
                long end = new DateTimeOffset(localDate.AddDays(1)).ToUnixTimeMilliseconds();
                if (requested < start || requested >= end || requested % 1000 != 0)
                    return GameResult.Reject(GameCodes.InvalidArgument, "请选择当天有效的精确到达时间");
                arrival = requested;
            }
            string battleId = "battle_" + Guid.NewGuid().ToString("N"), armyId = "army_" + Guid.NewGuid().ToString("N");
            GameResult occupied = GeneralsModule.TryOccupy(world, context.Actor.PlayerId, armyId, generalIds, out List<JObject> generals);
            if (occupied.Code != GameCodes.Ok) return occupied;
            var battle = new BanditBattle { Kind = "city", BattleId = battleId, ArmyId = armyId, PlayerId = context.Actor.PlayerId,
                X = x, Y = y, JoinBattleId = existing?.BattleId, ArrivalUtcMs = arrival, NextTickUtcMs = arrival, RandomState = Seed(),
                CityName = city.Value<string>("名称"), CityNation = city.Value<string>("国家"), CityScale = city.Value<int>("规模"),
                AttackFormationX = -40.25f, DefenseFormationX = -24f };
            AddArmy(world, battle, armyId, generalIds, generals, arrival);
            Save(world, battle);
            return Updated(world, context, battle, "combat.city.dispatched");
        }

        private static JObject City(WorldState world, int x, int y) => (world.Data["城池列表"] as JArray)?.OfType<JObject>()
            .SingleOrDefault(city => city.Value<int>("坐标x") == x && city.Value<int>("坐标y") == y);

        private static bool OriginalNpc(WorldState world, int index) => (world.Data["原玩家来源映射"] as JArray)?.OfType<JObject>()
            .Any(source => source.Value<int>("原索引") == index && source.Value<bool>("NPC")) == true;

        private static bool NpcCity(WorldState world, JObject city)
        {
            int owner = city.Value<int>("城主");
            JObject nation = ((JArray)world.Data["国家列表"]).OfType<JObject>().FirstOrDefault(item => item.Value<string>("国号") == city.Value<string>("国家"));
            return (owner == -1 || OriginalNpc(world, owner)) && (nation == null || OriginalNpc(world, nation.Value<int>("国王")));
        }

        private static void StartCityBattle(WorldState world, BanditBattle battle)
        {
            JObject city = City(world, battle.X, battle.Y);
            if (city == null || !NpcCity(world, city) || city.Value<string>("国家") == world.RequirePlayer(battle.PlayerId)["基础信息"].Value<string>("国家"))
            { battle.Phase = "withdrawn"; return; }
            battle.Phase = "fighting"; battle.StartedUtcMs = battle.ArrivalUtcMs;
            battle.NextTickUtcMs = checked(battle.StartedUtcMs + BanditBattleRules.TickMilliseconds);
            battle.CityName = city.Value<string>("名称"); battle.CityNation = city.Value<string>("国家"); battle.CityScale = city.Value<int>("规模");
            battle.Wall = city.Value<double>("城墙"); battle.WallMaximum = CityGarrisonRules.WallMaximum(battle.CityScale);
            var random = new CombatRandom(battle.RandomState);
            JObject country = ((JArray)world.Data["国家列表"]).OfType<JObject>().FirstOrDefault(item => item.Value<string>("国号") == battle.CityNation);
            int npcIndex = country?.Value<int>("国王") ?? 2;
            battle.NpcPlayerId = NpcPlayerId(world, npcIndex);
            JObject npc = world.RequirePlayer(battle.NpcPlayerId);
            var units = new List<CombatUnit>();
            foreach (JObject general in CityGarrisonRules.CreateMilitia(battle.CityScale, world.RequirePlayer(battle.PlayerId)["基础信息"].Value<string>("名字"), random.Next,
                level => CityGarrisonRules.CreateMilitiaGeneral(level, npc, (JArray)world.Data["将领配置"], (JObject)world.Data["姓名配置"], world.Data.Value<double>("难度"), battle.StartedUtcMs / 1000, random.Next)))
            {
                CombatUnit unit = BanditBattleRules.CreateUnit(battle.BattleId + ":militia:" + units.Count, general, Troop(world, general["将领配兵"].Value<int>("ID")), 1);
                unit.GeneralOwnerId = battle.NpcPlayerId; unit.Ephemeral = true; units.Add(unit);
            }
            void InsertGuard(string ownerId, JObject general)
            {
                if (!OriginalNpc(world, world.ResolvePlayerIndex(ownerId))) throw new InvalidOperationException("当前城防含真实玩家守将");
                string id = ((JObject)world.EntityMappings["generals"]).Properties().Single(entry => entry.Value.Value<string>("playerId") == ownerId && entry.Value.Value<int>("legacyId") == general.Value<int>("ID")).Name;
                if (units.Any(unit => !unit.Ephemeral && unit.GeneralId == id)) return;
                general["详细信息"]["坑位颜色"] = 1.0; general["详细信息"]["状态"] = 1.0;
                CombatUnit guard = BanditBattleRules.CreateUnit(id, general, Troop(world, 104), 1);
                guard.GeneralOwnerId = ownerId; units.Insert(random.Next(0, units.Count), guard);
            }
            int chance = random.Next(0, 101);
            if (chance <= city.Value<double>("协防几率") && country != null)
            {
                int count = (int)random.NextFloat(city.Value<float>("协防数量f"), city.Value<float>("协防数量m"));
                for (int index = 0; index < count; index++)
                {
                    JObject guard = CityGarrisonRules.SelectNamedGuard((JArray)npc["封地信息表"][0]["将领信息表"], battle.CityScale, random.Next);
                    if (guard == null) break;
                    InsertGuard(battle.NpcPlayerId, guard);
                }
            }
            foreach (JObject configured in ((JArray)city["城池驻防列表"]).OfType<JObject>())
            {
                JObject configuration = ((JArray)world.Data["将领配置"]).OfType<JObject>().Single(item => item.Value<int>("ID") == configured.Value<int>("将领ID标识"));
                string name = configuration.Value<string>("名字");
                foreach (JProperty owner in ((JObject)world.EntityMappings["players"]).Properties().OrderBy(entry => entry.Value.Value<int>()))
                {
                    JArray generals = (world.RequirePlayer(owner.Name)["封地信息表"] as JArray)?.FirstOrDefault()?["将领信息表"] as JArray;
                    if (generals == null) continue;
                    JObject guard = generals.OfType<JObject>().FirstOrDefault(item => item["将领属性"]["初始属性"].Value<string>("名字") == name);
                    if (guard == null) continue;
                    if (guard["详细信息"].Value<double>("状态") == 0) { CityGarrisonRules.EquipNamedGuard(guard); InsertGuard(owner.Name, guard); }
                    break;
                }
            }
            battle.Defenders = units;
            ReserveDefenders(world, battle);
            long[] available = CityGarrisonRules.FormationAvailability(units.Count, battle.ArrivalUtcMs, random.Next);
            for (int group = 0; group < available.Length; group++)
            {
                string army = battle.BattleId + ":defense:" + group;
                foreach (CombatUnit unit in units.Skip(group * 5).Take(5)) { unit.ArmyId = army; unit.UnitId = army + ":" + unit.GeneralId; }
                battle.DefenseFormations.Add(new CombatFormation { ArmyId = army, AvailableUtcMs = available[group], PositionX = -24f });
            }
            city = City(world, battle.X, battle.Y); city["正在交战"] = true;
            battle.RandomState = random.State;
        }

        private static void RecalculateCityOwner(WorldState world, BanditBattle battle, CombatUnit actor, long utc)
        {
            if (actor.Side == 1 && actor.Ephemeral) return; // 原临时ID0守军已从owner封地移走。
            string owner = actor.GeneralOwnerId ?? battle.PlayerId;
            JObject player = (JObject)world.RequirePlayer(owner).DeepClone();
            CombatUnit[] active = battle.Attackers.Concat(battle.Defenders).Where(unit => !unit.Retired && !unit.Ephemeral && (unit.GeneralOwnerId ?? battle.PlayerId) == owner).ToArray();
            foreach (CombatUnit unit in active)
            {
                JObject fief;
                JObject original = LegacyGenerals.General(player, unit.General.Value<int>("ID"), out fief);
                original.ReplaceAll(((JObject)unit.General.DeepClone()).Properties());
            }
            GeneralAttributeRules.Recalculate(player, utc / 1000);
            foreach (CombatUnit unit in active)
            {
                JObject fief;
                unit.General = (JObject)LegacyGenerals.General(player, unit.General.Value<int>("ID"), out fief).DeepClone();
            }
        }

        private static void ReserveDefenders(WorldState world, BanditBattle battle)
        {
            var guards = battle.Defenders.Where(unit => !unit.Ephemeral).ToArray();
            foreach (CombatUnit unit in guards)
            {
                JObject fief;
                GeneralsModule.ResolveGeneral(world, unit.GeneralOwnerId, unit.GeneralId, out fief)["详细信息"]["状态"] = 0.0;
            }
            GameResult result = GeneralsModule.ReserveCityDefenders(world, battle.BattleId,
                guards.Select(unit => new CityDefenderReference { PlayerId = unit.GeneralOwnerId, GeneralId = unit.GeneralId }), out List<JObject> reserved);
            if (result.Code != GameCodes.Ok) throw new InvalidOperationException(result.Message);
        }

        private static GameResult SettleDefenders(WorldState world, BanditBattle battle)
        {
            foreach (var owner in battle.Defenders.Where(unit => !unit.Ephemeral).GroupBy(unit => unit.GeneralOwnerId))
            {
                GameResult result = GeneralsModule.ApplyCityDefenderOutcome(world, owner.Key, battle.BattleId,
                    owner.Select(unit => new GeneralOutcome { GeneralId = unit.GeneralId, General = unit.General,
                        Remaining = checked((int)unit.Remaining), Wounded = checked((int)unit.Wounded) }));
                if (result.Code != GameCodes.Ok) return result;
            }
            return GameResult.Success();
        }

        private static void Capture(WorldState world, BanditBattle battle, CombatUnit attacker, CombatUnit defender, CombatRandom random, long utc)
        {
            JObject player = world.RequirePlayer(battle.PlayerId);
            int draw = random.Next(0, 311);
            int threshold = CityGarrisonRules.CaptureThreshold(player["基础信息"].Value<double>("抓将几率"), CombatEconomy.GetActiveBonus(player, "抓将几率", utc), defender.General["将领属性"]["初始属性"].Value<double>("突围"));
            string hash;
            using (MD5 md5 = MD5.Create()) hash = BitConverter.ToString(md5.ComputeHash(Encoding.Default.GetBytes(player["基础信息"].Value<string>("名字")))).Replace("-", "");
            bool captured = hash == "E586D0FD6B8E898AFA3B640A861EEBAB" || draw <= threshold;
            GameResult result = GeneralsModule.CaptureGeneral(world, defender.GeneralOwnerId, defender.GeneralId, battle.PlayerId, attacker.GeneralId, battle.BattleId, captured);
            if (result.Code != GameCodes.Ok) throw new InvalidOperationException(result.Message);
            JObject fief;
            JObject original = GeneralsModule.ResolveGeneral(world, defender.GeneralOwnerId, defender.GeneralId, out fief);
            foreach (string field in new[] { "状态", "俘虏玩家", "忠诚" }) defender.General["详细信息"][field] = original["详细信息"][field].DeepClone();
        }

        private static GameResult SettleCity(WorldState world, BanditBattle battle, long utc)
        {
            battle.Reward = BanditBattleRules.CalculateRewards(battle, CombatEconomy.GetActiveBonus(world.RequirePlayer(battle.PlayerId), "资源声望", utc));
            GameResult rewarded = CombatEconomy.ApplyCombatRewards(world, battle.PlayerId, battle.BattleId, battle.Reward.声望, battle.Reward.国库铜钱, battle.Reward.国库粮食);
            if (rewarded.Code != GameCodes.Ok) return rewarded;
            BanditBattleRules.EnsureFormations(battle);
            foreach (var army in battle.Attackers.Where(unit => !unit.Retired).GroupBy(unit => unit.ArmyId))
            {
                GameResult outcome = GeneralsModule.ApplyOutcome(world, battle.PlayerId, army.Key, army.Select(unit => new GeneralOutcome {
                    GeneralId = unit.GeneralId, General = unit.General, Remaining = checked((int)unit.Remaining), Wounded = checked((int)unit.Wounded) }));
                if (outcome.Code != GameCodes.Ok) return outcome;
            }
            GameResult defenders = SettleDefenders(world, battle);
            if (defenders.Code != GameCodes.Ok) return defenders;
            if (battle.StartedUtcMs != 0)
            {
                JObject city = City(world, battle.X, battle.Y); city["城墙"] = battle.Wall; city["正在交战"] = false;
                var random = new CombatRandom(battle.RandomState);
                if (battle.Phase == "won")
                {
                    battle.WarReward = CityGarrisonRules.WarReward(battle.CityScale);
                    JObject player = world.RequirePlayer(battle.PlayerId);
                    string nationName = player["基础信息"].Value<string>("国家");
                    if (((JArray)world.Data["国家列表"]).OfType<JObject>().Any(nation => nation.Value<string>("国号") == nationName))
                    {
                        GameResult capturedCity = LegacyWorldModule.ApplyCityVictory(world, battle.PlayerId, battle.X, battle.Y, battle.BattleId, random.Next);
                        if (capturedCity.Code != GameCodes.Ok) return capturedCity;
                    }
                }
                if (battle.Phase == "won" || battle.Phase == "withdrawn") RefreshNamedCityGuards(world, battle.X, battle.Y, random.Next);
                battle.RandomState = random.State;
                JObject basics = (JObject)world.RequirePlayer(battle.PlayerId)["基础信息"];
                double war = basics.Value<double>("战功") + Math.Floor((float)battle.WarReward);
                basics["战功"] = war; basics["官职"] = CityGarrisonRules.Rank(war);
            }
            battle.SettlementApplied = true; battle.SettledUtcMs = utc;
            Save(world, battle);
            if (battle.StartedUtcMs != 0) return AddCityReport(world, battle, utc);
            return GameResult.Success();
        }

        private static void RefreshNamedCityGuards(WorldState world, int x, int y, Func<int, int, int> random)
        {
            JArray list = (JArray)City(world, x, y)["城池驻防列表"];
            var cities = (JArray)world.Data["城池列表"];
            for (int index = list.Count - 1; index >= 0; index--)
            {
                ((JArray)cities[random(0, cities.Count)]["城池驻防列表"]).Add(list[index].DeepClone());
                list.RemoveAt(index);
            }
        }

        private static GameResult AddCityReport(WorldState world, BanditBattle battle, long utc)
        {
            string attacker = world.RequirePlayer(battle.PlayerId)["基础信息"].Value<string>("国家");
            if (string.IsNullOrEmpty(attacker)) attacker = "某国";
            string defender = string.IsNullOrEmpty(battle.CityNation) ? "无主势力" : battle.CityNation;
            string scale = battle.CityScale == 1 ? "小城" : battle.CityScale == 2 ? "中城" : battle.CityScale == 3 ? "大城" : battle.CityScale == 4 ? "重镇" : "";
            string content = battle.Phase == "won" ? attacker + "攻下了" + defender + scale + battle.CityName + "！" : defender + "在" + scale + battle.CityName + "击败" + attacker + "！";
            return ChatModule.RecordCityBattleReport(world, battle.BattleId, content, utc);
        }
    }
}
