using System;
using System.Collections.Generic;
using System.Linq;
using Dwsg.Shared;
using Dwsg.Shared.Combat;
using Dwsg.Server.Modules.Generals;
using Newtonsoft.Json.Linq;

namespace Dwsg.Server.Modules.Combat
{
    public sealed partial class CombatModule
    {
        private static GameResult DispatchGarrison(WorldState world, CommandContext context, JObject payload)
        {
            if (!Keys(payload, "x", "y", "generalIds") || !Coordinate(payload["x"], out int x) || !Coordinate(payload["y"], out int y)
                || !(payload["generalIds"] is JArray ids) || ids.Count < 1 || ids.Count > 5 || ids.Any(id => !Id(id, out _))
                || ids.Values<string>().Distinct(StringComparer.Ordinal).Count() != ids.Count)
                return GameResult.Reject(GameCodes.InvalidArgument, "请选择1至5名不同驻防将领和有效城池坐标");
            JObject city = City(world, x, y);
            if (city == null) return GameResult.Reject(GameCodes.NotFound, "城池不存在");
            BanditBattle battle = ActiveAt(world, x, y, kind: "city");
            if (battle == null || battle.Phase == "marching") return DispatchPeaceGarrison(world, context, payload);
            if (battle == null || battle.Phase != "fighting" || ValidateCitySiegeOwnership(world, city, out _).Code != GameCodes.Ok)
                return GameResult.Reject(GameCodes.Conflict, "当前无法驻防，城池没有正在进行的有效城战");
            if (battle.PlayerId == context.Actor.PlayerId)
                return GameResult.Reject(GameCodes.Conflict, "不能驻防自己正在进攻的城池");
            if (battle.NextTickUtcMs <= context.ServerUtcMs)
            {
                GameResult advanced = Advance(world, context, new JObject { ["battleId"] = battle.BattleId, ["tickUtcMs"] = battle.NextTickUtcMs });
                if (advanced.Code != GameCodes.Ok) return advanced;
                battle = Load(world, battle.BattleId); city = City(world, x, y);
                if (battle.SettlementApplied || battle.NextTickUtcMs <= context.ServerUtcMs)
                    return GameResult.Reject(GameCodes.Conflict, "城战已结束或正在同步，请稍后驻防");
            }
            JObject player = world.RequirePlayer(context.Actor.PlayerId), sourceFief;
            string nation = player["基础信息"].Value<string>("国家");
            string[] generalIds = ids.Values<string>().ToArray();
            GeneralsModule.ResolveGeneral(world, context.Actor.PlayerId, generalIds[0], out sourceFief);
            JObject source = (JObject)sourceFief["所在城池"];
            var cities = ((JArray)world.Data["城池列表"]).OfType<JObject>().ToDictionary(item => Tuple.Create(item.Value<int>("坐标x"), item.Value<int>("坐标y")));
            int route = new CityMarchRules(全局大地图库.大地图表, (cx, cy) => cities[Tuple.Create(cx, cy)].Value<string>("国家") == nation)
                .Find(source.Value<int>("x") - 1, source.Value<int>("y") - 1, x - 1, y - 1);
            if (!CityPlayerGarrisonRules.CanDispatch(nation, city.Value<string>("国家"), true, route, player["基础信息"].Value<string>("名字")))
                return GameResult.Reject(GameCodes.Conflict, nation != city.Value<string>("国家") ? "只能驻防本国城池" : "原地图没有可达的驻防路线");
            string armyId = "army_" + Guid.NewGuid().ToString("N");
            GameResult occupied = GeneralsModule.TryOccupy(world, context.Actor.PlayerId, armyId, generalIds, out List<JObject> generals);
            if (occupied.Code != GameCodes.Ok) return occupied;
            long arrival = CityPlayerGarrisonRules.ArrivalUtcMs(context.ServerUtcMs);
            for (int index = 0; index < generals.Count; index++)
            {
                generals[index]["详细信息"]["坑位颜色"] = 1.0;
                CombatUnit unit = BanditBattleRules.CreateUnit(generalIds[index], generals[index], Troop(world, generals[index]["将领配兵"].Value<int>("ID")), 1);
                unit.GeneralOwnerId = context.Actor.PlayerId; unit.PlayerGarrison = true; unit.ArmyId = armyId; unit.UnitId = armyId + ":" + unit.GeneralId;
                battle.Defenders.Add(unit);
            }
            battle.GarrisonArmies.Add(new CombatGarrisonArmy { PlayerId = context.Actor.PlayerId, ArmyId = armyId, Nation = nation, GeneralIds = generalIds.ToList(), ArrivalUtcMs = arrival });
            battle.DefenseFormations.Add(new CombatFormation { ArmyId = armyId, AvailableUtcMs = arrival, PositionX = -24f });
            Save(world, battle);
            GameResult result = Updated(world, context, battle, "combat.city.garrison.dispatched");
            result.Data["armyId"] = armyId; result.Data["arrivalUtcMs"] = arrival;
            return result;
        }

        private static GameResult UpdateGarrisonArrivals(WorldState world, BanditBattle battle, long tickUtcMs)
        {
            foreach (CombatGarrisonArmy army in battle.GarrisonArmies.Where(army => army.Phase == "marching" && army.ArrivalUtcMs <= tickUtcMs))
            {
                JObject city = City(world, battle.X, battle.Y);
                string nation = world.RequirePlayer(army.PlayerId)["基础信息"].Value<string>("国家");
                if (!CityPlayerGarrisonRules.CanJoinExistingBattle(battle, battle.X, battle.Y, army.ArrivalUtcMs, tickUtcMs)
                    || ValidateCitySiegeOwnership(world, city, out _).Code != GameCodes.Ok || nation != army.Nation || city.Value<string>("国家") != army.Nation)
                {
                    GameResult released = ReleaseGarrison(world, battle, army, null, "returned", tickUtcMs);
                    if (released.Code != GameCodes.Ok) return released;
                }
                else { army.Phase = "fighting"; army.JoinedUtcMs = army.ArrivalUtcMs; }
            }
            return GameResult.Success();
        }

        private static GameResult WithdrawGarrison(WorldState world, CommandContext context, JObject payload)
        {
            bool single = payload?["generalId"] != null;
            if (!Keys(payload, single ? new[] { "armyId", "generalId" } : new[] { "armyId" }) || !Id(payload["armyId"], out string armyId)
                || (single && !Id(payload["generalId"], out _))) return GameResult.Reject(GameCodes.InvalidArgument, "驻防撤回参数无效");
            var peace = (world.Data["和平驻防运行"] as JObject)?[armyId] as JObject;
            if (peace != null && peace.Value<string>("Phase") != "fighting")
            {
                if (single) return GameResult.Reject(GameCodes.InvalidArgument, "和平驻防请按队伍撤回。");
                return WithdrawPeaceGarrison(world, context, peace);
            }
            var records = world.Data["战斗运行"] as JObject;
            BanditBattle battle = records?.Properties().Select(entry => entry.Value.ToObject<BanditBattle>())
                .SingleOrDefault(entry => entry.GarrisonArmies.Any(army => army.ArmyId == armyId));
            if (battle == null) return GameResult.Reject(GameCodes.NotFound, "驻防军队不存在");
            CombatGarrisonArmy army = battle.GarrisonArmies.Single(entry => entry.ArmyId == armyId);
            if (army.PlayerId != context.Actor.PlayerId) return GameResult.Reject(GameCodes.Forbidden, "只能撤回本人的驻防军队");
            string generalId = single ? payload.Value<string>("generalId") : null;
            if (single && !army.GeneralIds.Contains(generalId)) return GameResult.Reject(GameCodes.Forbidden, "只能撤回本驻防军队的将领");
            if (army.Phase != "marching" && army.Phase != "fighting")
                return GameResult.Success(new JObject { ["battleId"] = battle.BattleId, ["armyId"] = armyId, ["phase"] = army.Phase });
            RefreshCurrentGenerals(world, battle);
            GameResult released = ReleaseGarrison(world, battle, army, generalId, "withdrawn", context.ServerUtcMs);
            if (released.Code != GameCodes.Ok) return released;
            Save(world, battle);
            GameResult result = Updated(world, context, battle, "combat.city.garrison.withdrawn");
            result.Data["armyId"] = armyId; result.Data["phase"] = army.Phase;
            return result;
        }

        private static GameResult ReleaseGarrison(WorldState world, BanditBattle battle, CombatGarrisonArmy army, string generalId, string phase, long utcMs)
        {
            CombatUnit[] units = battle.Defenders.Where(unit => unit.PlayerGarrison && unit.ArmyId == army.ArmyId && !unit.Retired
                && (generalId == null || unit.GeneralId == generalId)).ToArray();
            if (units.Length > 0)
            {
                GameResult outcome = GeneralsModule.ApplyCityDefenderOutcome(world, army.PlayerId, army.ArmyId,
                    units.Select(unit => new GeneralOutcome { GeneralId = unit.GeneralId, General = unit.General, Remaining = checked((int)unit.Remaining), Wounded = checked((int)unit.Wounded) }), generalId == null);
                if (outcome.Code != GameCodes.Ok) return outcome;
                var peaceOutcome = RestorePeaceAfterBattle(world, army, units, phase == "withdrawn", utcMs);
                if (peaceOutcome.Code != GameCodes.Ok) return peaceOutcome;
                foreach (CombatUnit unit in units)
                {
                    JObject fief;
                    JObject canonical = GeneralsModule.ResolveGeneral(world, army.PlayerId, unit.GeneralId, out fief);
                    unit.General["详细信息"]["状态"] = canonical["详细信息"]["状态"].DeepClone(); unit.Retired = true; unit.Slot = -1;
                }
            }
            if (!battle.Defenders.Any(unit => unit.PlayerGarrison && unit.ArmyId == army.ArmyId && !unit.Retired))
            { army.Phase = phase; army.SettledUtcMs = utcMs; }
            return GameResult.Success();
        }

        private static GameResult SettleGarrisons(WorldState world, BanditBattle battle, long utcMs)
        {
            foreach (CombatGarrisonArmy army in battle.GarrisonArmies.Where(army => army.Phase == "marching" || army.Phase == "fighting"))
            {
                string phase = army.Phase == "marching" ? "returned" : battle.Phase == "won" ? "lost" : "won";
                GameResult outcome = ReleaseGarrison(world, battle, army, null, phase, utcMs);
                if (outcome.Code != GameCodes.Ok) return outcome;
            }
            return GameResult.Success();
        }
    }
}
