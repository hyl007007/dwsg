using System;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Dwsg.Shared.Economy
{
    public static class TroopRecruitmentRules
    {
        public static GameResult Recruit(JObject player, JObject fief, int plot, int troopTypeId, int count, JArray configurations)
        {
            if (count <= 0) return GameResult.Reject(GameCodes.InvalidArgument, "招募数量必须为正整数");
            int troopClass = troopTypeId / 100, tier = troopTypeId % 100;
            if (troopClass < 1 || troopClass > 4 || tier < 1 || tier > 4)
                return GameResult.Reject(GameCodes.NotFound, "原兵种不存在");
            try
            {
                var fiefs = Array(player?["封地信息表"]);
                if (!fiefs.Any(f => ReferenceEquals(f, fief))) return GameResult.Reject(GameCodes.Forbidden, "无权招募此封地的兵士");
                var buildings = Array(fief["建筑信息表"]);
                if (plot < 0 || plot >= buildings.Count) return GameResult.Reject(GameCodes.InvalidArgument, "兵营位置无效");
                var building = (JObject)buildings[plot];
                if (Integer(building["类型"]) != troopClass + 3 || Integer(building["等级"]) <= (tier - 1) * 3)
                    return GameResult.Reject(GameCodes.Conflict, "原兵营类型或等级不足");
                var troop = Array(configurations).OfType<JObject>().FirstOrDefault(t => Integer(t["ID"]) == troopTypeId);
                if (troop == null) return GameResult.Reject(GameCodes.NotFound, "原兵种配置不存在");
                if (Integer(troop["兵种"]) != troopClass || Integer(troop["等级"]) != tier) throw new InvalidOperationException();
                double copperPer = Number(troop["需要铜钱"]), grainPer = Number(troop["需要粮食"]), populationPer = Number(troop["占用人口"]);
                if (copperPer <= 0 || grainPer <= 0 || populationPer <= 0) throw new InvalidOperationException();
                var wallet = (JObject)player["财产信息"];
                double copper = Number(wallet["铜钱"]), grain = Number(wallet["粮食"]);
                double copperCost = copperPer * count, grainCost = grainPer * count;
                Number(new JValue(copperCost)); Number(new JValue(grainCost));
                // Retain the original UI's float-floor limit, then prevent its round-up from overdrawing resources.
                double resourceLimit = Math.Floor((float)Math.Min(copper / copperPer, grain / grainPer));
                if (count > resourceLimit || copper < copperCost || grain < grainCost)
                    return GameResult.Reject(GameCodes.InsufficientFunds, "招募失败，铜钱或粮食不足");
                double capacity = 0, occupied = 0;
                var technology = (JObject)player["科技信息"];
                double rawSettlement = Number(technology["安置"]);
                if (rawSettlement > int.MaxValue) throw new OverflowException();
                int settlement = (int)rawSettlement;
                foreach (JObject current in fiefs)
                {
                    foreach (JObject b in Array(current["建筑信息表"]))
                    {
                        int type = Integer(b["类型"]), level = Integer(b["等级"]);
                        if (level < 0) throw new InvalidOperationException();
                        if (type == 0) capacity += checked(level * 500);
                        else if (type == 2) capacity += checked(level * 250) + checked(settlement * 40);
                    }
                    // The original population counts all idle, wounded and assigned soldiers in every fief.
                    var pools = Array(current["闲兵信息表"]).Concat(Array(current["伤兵信息表"]))
                        .Concat(Array(current["将领信息表"]).OfType<JObject>().Select(g => g["将领配兵"]));
                    foreach (JObject pool in pools)
                    {
                        int id = Integer(pool["ID"]);
                        var definition = configurations.OfType<JObject>().FirstOrDefault(t => Integer(t["ID"]) == id);
                        if (definition != null) occupied += Number(pool["数量"]) * Number(definition["占用人口"]);
                    }
                }
                capacity += Math.Floor((float)(capacity * Number(technology["工程设计"]) * 0.05000000074505806));
                Number(new JValue(capacity)); Number(new JValue(occupied));
                double remaining = capacity - occupied;
                if (count > Math.Max(0, Math.Floor((float)(remaining / populationPer))) || count * populationPer > remaining)
                    return GameResult.Reject(GameCodes.Conflict, "招募失败，原人口不足");
                var idle = Array(fief["闲兵信息表"]);
                var target = idle.OfType<JObject>().FirstOrDefault(t => Integer(t["ID"]) == troopTypeId);
                double merged = (target == null ? 0 : Number(target["数量"])) + count;
                if (merged > int.MaxValue || merged != Math.Truncate(merged)) throw new InvalidOperationException();
                // The original confirmation is immediate; its 需要时间 field is unused.
                wallet["铜钱"] = copper - copperCost; wallet["粮食"] = grain - grainCost;
                if (target == null) idle.Add(new JObject { ["ID"] = troopTypeId, ["数量"] = merged }); else target["数量"] = merged;
                var result = GameResult.Success(new JObject { ["troopTypeId"] = troopTypeId, ["count"] = count, ["copperCost"] = copperCost, ["foodCost"] = grainCost });
                result.Message = "招募成功!"; return result;
            }
            catch (InvalidOperationException) { return GameResult.Reject(GameCodes.Unavailable, "原招兵数据无效"); }
            catch (InvalidCastException) { return GameResult.Reject(GameCodes.Unavailable, "原招兵数据无效"); }
            catch (OverflowException) { return GameResult.Reject(GameCodes.Unavailable, "原招兵数值超出范围"); }
        }

        private static JArray Array(JToken value) { return value as JArray ?? throw new InvalidOperationException(); }
        private static double Number(JToken value)
        {
            double number; if (!ShopRules.TryNumber(value, out number) || number < 0) throw new InvalidOperationException(); return number;
        }
        private static int Integer(JToken value)
        {
            double number; if (!ShopRules.TryNumber(value, out number) || number < int.MinValue || number > int.MaxValue || number != Math.Truncate(number)) throw new InvalidOperationException(); return (int)number;
        }
    }
}
