using System;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Dwsg.Shared.Economy
{
    public static class BuildingRules
    {
        public static double UpgradeCost(double basis, double level)
        {
            double cost = basis;
            for (int i = 0; i < level; i++) cost *= 2.0;
            return cost * 200.0;
        }

        public static GameResult ConstructPlot(JObject fief, int plot, int buildingType)
        {
            var buildings = fief?["建筑信息表"] as JArray;
            if (buildings == null || plot <= 0 || plot >= buildings.Count || buildingType < 1 || buildingType > 7)
                return GameResult.Reject(GameCodes.InvalidArgument, "建筑位置或类型无效");
            var target = buildings[plot] as JObject;
            if (target == null || target.Value<int?>("类型") != -1 || target.Value<int?>("等级") != 0)
                return GameResult.Reject(GameCodes.Conflict, "该位置已有建筑");
            if (buildingType == 1 && buildings.OfType<JObject>().Any(item => item.Value<int>("类型") == 1))
                return GameResult.Reject(GameCodes.Conflict, "每个封地只能有一座书院");
            target["类型"] = buildingType;
            target["等级"] = 1;
            return GameResult.Success();
        }

        public static GameResult UpgradePlot(JObject fief, int plot)
        {
            var buildings = fief?["建筑信息表"] as JArray;
            if (buildings == null || plot < 0 || plot >= buildings.Count || !(buildings[plot] is JObject))
                return GameResult.Reject(GameCodes.InvalidArgument, "建筑位置无效");
            var target = (JObject)buildings[plot];
            int type = target.Value<int>("类型");
            int level = target.Value<int>("等级");
            if (type < 0 || type > 7 || level < 1) return GameResult.Reject(GameCodes.Conflict, "该位置没有建筑");
            int maximum = fief.Value<int>("ID") == 1 ? 15 : 10;
            if (type >= 4) maximum = 10;
            if (level >= maximum || (plot != 0 && level >= buildings[0].Value<int>("等级")))
                return GameResult.Reject(GameCodes.Conflict, "升级失败,等级上限!");
            target["等级"] = level + 1;
            return GameResult.Success();
        }

        public static GameResult DemolishPlot(JObject fief, int plot)
        {
            var buildings = fief?["建筑信息表"] as JArray;
            if (buildings == null || plot <= 0 || plot >= buildings.Count || !(buildings[plot] is JObject))
                return GameResult.Reject(GameCodes.InvalidArgument, "建筑位置无效");
            var target = (JObject)buildings[plot];
            if (target.Value<int>("类型") < 1) return GameResult.Reject(GameCodes.Conflict, "该位置没有可拆除建筑");
            target["类型"] = -1;
            target["等级"] = 0;
            return GameResult.Success();
        }

        public static double FarmRate(JObject player)
        {
            var fiefs = player?["封地信息表"] as JArray;
            if (fiefs == null) throw new InvalidOperationException("原封地列表缺失");
            double rate = 0;
            foreach (JObject fief in fiefs)
                foreach (JObject building in (JArray)fief["建筑信息表"])
                    if (building.Value<int>("类型") == 3) rate += building.Value<int>("等级");
            return rate;
        }

        private static GameResult Apply(JObject player, JObject fief, int plot, int? buildingType, bool demolish)
        {
            var basic = player?["基础信息"] as JObject;
            var wallet = player?["财产信息"] as JObject;
            if (basic == null || wallet == null) return GameResult.Reject(GameCodes.Unavailable, "角色经营数据不完整");
            // Validate the production source before changing a building or charging resources.
            FarmRate(player);
            double copper = 0, grain = 0, copperCost = 0, grainCost = 0;
            if (!buildingType.HasValue && !demolish)
            {
                var buildings = fief?["建筑信息表"] as JArray;
                if (buildings == null || plot < 0 || plot >= buildings.Count || !(buildings[plot] is JObject))
                    return GameResult.Reject(GameCodes.InvalidArgument, "建筑位置无效");
                int level = buildings[plot].Value<int>("等级");
                copperCost = UpgradeCost(2.0, level);
                grainCost = UpgradeCost(4.0, level);
                if (!ShopRules.TryNumber(wallet["铜钱"], out copper) || !ShopRules.TryNumber(wallet["粮食"], out grain) ||
                    copper <= copperCost || grain <= grainCost)
                    return GameResult.Reject(GameCodes.InsufficientFunds, "升级失败!\n需要铜钱:" + copperCost + "\n需要粮食:" + grainCost);
            }
            GameResult result = buildingType.HasValue ? ConstructPlot(fief, plot, buildingType.Value)
                : demolish ? DemolishPlot(fief, plot) : UpgradePlot(fief, plot);
            if (result.Code != GameCodes.Ok) return result;
            if (!buildingType.HasValue && !demolish)
            {
                wallet["铜钱"] = copper - copperCost;
                wallet["粮食"] = grain - grainCost;
                result.Message = "升级成功!\n消耗铜钱:" + copperCost + "\n消耗粮食:" + grainCost;
            }
            // Derive production from existing farms, preventing upgraded demolished farms from producing forever.
            basic["粮食增加"] = FarmRate(player);
            return result;
        }

        public static GameResult Construct(JObject player, JObject fief, int plot, int buildingType) { return Apply(player, fief, plot, buildingType, false); }
        public static GameResult Upgrade(JObject player, JObject fief, int plot) { return Apply(player, fief, plot, null, false); }
        public static GameResult Demolish(JObject player, JObject fief, int plot) { return Apply(player, fief, plot, null, true); }
    }
}
