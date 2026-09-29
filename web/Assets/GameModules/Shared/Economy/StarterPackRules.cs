using System;
using Newtonsoft.Json.Linq;

namespace Dwsg.Shared.Economy
{
    public static class StarterPackRules
    {
        public static GameResult GrantRewards(JObject wallet)
        {
            double copper, grain, gold;
            if (wallet == null || !ShopRules.TryNumber(wallet["铜钱"], out copper) || !ShopRules.TryNumber(wallet["粮食"], out grain) ||
                !ShopRules.TryNumber(wallet["黄金"], out gold) || double.IsInfinity(copper + 500000) ||
                double.IsInfinity(grain + 1000000) || double.IsInfinity(gold + 100000))
                return GameResult.Reject(GameCodes.Unavailable, "资产数据无效");
            wallet["铜钱"] = copper + 500000;
            wallet["粮食"] = grain + 1000000;
            wallet["黄金"] = gold + 100000;
            var result = GameResult.Success(new JObject { ["铜钱"] = 500000, ["粮食"] = 1000000, ["黄金"] = 100000 });
            result.Message = "铜钱+500000\n粮食+1000000\n黄金+100000";
            return result;
        }

        public static GameResult Use(JObject player, JArray originalItemDefinitions)
        {
            var wallet = player?["财产信息"] as JObject;
            if (wallet == null) return GameResult.Reject(GameCodes.Unavailable, "资产数据不完整");
            var awarded = (JObject)wallet.DeepClone();
            var reward = GrantRewards(awarded);
            if (reward.Code != GameCodes.Ok) return reward;
            var consumed = InventoryRules.Consume(player, originalItemDefinitions, "新手礼包", 1);
            if (consumed.Code != GameCodes.Ok) return consumed;
            player["财产信息"] = awarded;
            return reward;
        }
    }
}
