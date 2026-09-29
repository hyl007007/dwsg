using System;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Dwsg.Shared.Economy
{
    public static class CombatEconomy
    {
        public static double GetActiveBonus(JObject player, string name, long serverUtcMs)
        {
            var status = (player?["道具状态表"] as JArray)?.OfType<JObject>().FirstOrDefault(item => item.Value<string>("名字") == name);
            double bonus;
            return status != null && (status.Value<long?>("到期时间") ?? 0) > serverUtcMs / 1000 && ShopRules.TryNumber(status["加成"], out bonus) ? bonus : 0;
        }

        public static GameResult ApplyCombatRewards(WorldState candidate, string playerId, string battleId,
            double prestige, double treasuryCopper, double treasuryGrain)
        {
            if (string.IsNullOrEmpty(battleId) || !ValidReward(prestige) || !ValidReward(treasuryCopper) || !ValidReward(treasuryGrain))
                return GameResult.Reject(GameCodes.InvalidArgument, "战斗奖励无效");
            JObject player;
            try { player = candidate.RequirePlayer(playerId); }
            catch (InvalidOperationException) { return GameResult.Reject(GameCodes.Forbidden, "战斗角色不存在"); }
            var basic = player["基础信息"] as JObject;
            var nation = (candidate.Data["国家列表"] as JArray)?.OfType<JObject>()
                .FirstOrDefault(item => item.Value<string>("国号") == basic?.Value<string>("国家"));
            double currentPrestige, currentLevel, copper, grain;
            if (basic == null || nation == null || !ShopRules.TryNumber(basic["声望"], out currentPrestige) ||
                !ShopRules.TryNumber(basic["等级"], out currentLevel) || !ShopRules.TryNumber(nation["铜钱"], out copper) || !ShopRules.TryNumber(nation["粮食"], out grain))
                return GameResult.Reject(GameCodes.Unavailable, "战斗结算国家或君主数据不完整");
            double addedPrestige = Math.Floor((float)prestige);
            double addedCopper = Math.Floor((float)treasuryCopper);
            double addedGrain = Math.Floor((float)treasuryGrain);
            if (double.IsInfinity(copper + addedCopper) || double.IsInfinity(grain + addedGrain))
                return GameResult.Reject(GameCodes.Unavailable, "国家资源数据无效");
            float level = (float)currentLevel;
            MonarchRules.GrantExperience(addedPrestige, ref currentPrestige, ref level);
            basic["声望"] = currentPrestige;
            basic["等级"] = level;
            nation["铜钱"] = copper + addedCopper;
            nation["粮食"] = grain + addedGrain;
            // Battle ownership and the single settlement marker belong to M06's same candidate transaction.
            return GameResult.Success(new JObject { ["battleId"] = battleId, ["prestige"] = addedPrestige,
                ["treasuryCopper"] = addedCopper, ["treasuryGrain"] = addedGrain });
        }

        private static bool ValidReward(double reward)
        {
            return !double.IsNaN(reward) && !double.IsInfinity(reward) && reward >= 0 && reward <= 10000000;
        }
    }
}
