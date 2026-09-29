using System;
using Newtonsoft.Json.Linq;

namespace Dwsg.Shared.Economy
{
    public static class ProductionRules
    {
        public static GameResult ProduceOneSecond(JObject player, double resourceTechnology)
        {
            var wallet = player?["财产信息"] as JObject;
            var basic = player?["基础信息"] as JObject;
            double grain;
            if (basic == null || wallet == null || !ShopRules.TryNumber(wallet["粮食"], out grain) ||
                double.IsNaN(resourceTechnology) || double.IsInfinity(resourceTechnology))
                return GameResult.Reject(GameCodes.Unavailable, "角色生产数据不完整");
            double rate = BuildingRules.FarmRate(player);
            double bonus = rate * (resourceTechnology > 0 ? resourceTechnology / 100 : 0);
            double total = grain + (rate + bonus);
            if (double.IsNaN(total) || double.IsInfinity(total))
                return GameResult.Reject(GameCodes.Unavailable, "粮食产量无效");
            basic["粮食增加"] = rate;
            wallet["粮食"] = total;
            return GameResult.Success();
        }

        public static GameResult ClampWallet(JObject wallet)
        {
            var names = new[] { "黄金", "白银", "铜钱", "粮食" };
            var limits = new[] { 6666666.0, 6666666.0, 100000000.0, 300000000.0 };
            var values = new double[names.Length];
            for (int i = 0; i < names.Length; i++)
                if (wallet == null || !ShopRules.TryNumber(wallet[names[i]], out values[i]))
                    return GameResult.Reject(GameCodes.Unavailable, "角色资源数据不完整");
            for (int i = 0; i < names.Length; i++) wallet[names[i]] = Math.Min(values[i], limits[i]);
            return GameResult.Success();
        }
    }
}
