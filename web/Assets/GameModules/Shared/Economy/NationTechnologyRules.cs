using System;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Dwsg.Shared.Economy
{
    public static class NationTechnologyRules
    {
        private static readonly string[] Names = { "攻击科技", "防御科技", "资源科技" };
        private static readonly string[] Currencies = { "黄金", "粮食", "铜钱" };

        public static bool TryLevels(JObject nation, string technology, out int level, out int total)
        {
            level = 0; total = 0;
            if (nation == null || System.Array.IndexOf(Names, technology) < 0) return false;
            long sum = 0;
            foreach (string name in Names)
            {
                double value;
                if (!ShopRules.TryNumber(nation[name], out value) || value < 0 || value > int.MaxValue || value != Math.Truncate(value)) return false;
                sum += (int)value;
                if (name == technology) level = (int)value;
            }
            if (sum > int.MaxValue) return false;
            total = (int)sum;
            return true;
        }

        public static double[] Costs(int total)
        {
            if (total < 0) throw new ArgumentOutOfRangeException(nameof(total));
            if (total == 0) return new double[] { 5000, 10000, 10000 };
            int gold, resource;
            if (total <= 20) { gold = 5000; resource = 10000; }
            else if (total <= 30) { gold = 10000; resource = 15000; }
            else if (total <= 50) { gold = 15000; resource = 20000; }
            else if (total <= 70) { gold = 20000; resource = 30000; }
            else if (total <= 80) { gold = 30000; resource = 50000; }
            else if (total <= 90) { gold = 50000; resource = 80000; }
            else if (total <= 100) { gold = 50000; resource = 90000; }
            else { gold = 200000; resource = 200000; }
            // The source multiplies two Int32 values before converting to double.
            // Reject corrupt overflow; retain its float rounding when actually deducting.
            return new double[] { checked(total * gold), checked(total * resource), checked(total * resource) };
        }

        public static GameResult Upgrade(WorldState world, string playerId, string technology, int expectedLevel, int expectedTotal)
        {
            if (System.Array.IndexOf(Names, technology) < 0) return GameResult.Reject(GameCodes.NotFound, "国家科技不存在");
            JObject player; int identity;
            try { identity = world.ResolvePlayerIndex(playerId); player = world.RequirePlayer(playerId); }
            catch (InvalidOperationException) { return GameResult.Reject(GameCodes.Unavailable, "原角色身份无效"); }
            var basic = player["基础信息"] as JObject;
            var nation = TerritoryRules.Nation(world, basic?.Value<string>("国家"));
            if (nation == null) return GameResult.Reject(GameCodes.Unavailable, "原所属国家不存在");
            double index;
            var members = nation["成员列表"] as JArray;
            if (!ShopRules.TryNumber(basic?["ID"], out index) || index != identity || members == null ||
                !members.Any(m => ShopRules.TryNumber(m, out index) && index == identity))
                return GameResult.Reject(GameCodes.Forbidden, "原国家成员身份不一致");
            int level, total;
            if (!TryLevels(nation, technology, out level, out total)) return GameResult.Reject(GameCodes.Unavailable, "原国家科技等级无效");
            if (level != expectedLevel || total != expectedTotal) return GameResult.Reject(GameCodes.Conflict, "国家科技已更新，请刷新");
            double[] costs;
            try { costs = Costs(total); }
            catch (OverflowException) { return GameResult.Reject(GameCodes.Unavailable, "原国家科技费用超出数值范围"); }
            var wallet = player["财产信息"] as JObject;
            var balances = new double[3]; var deducted = new double[3];
            for (int i = 0; i < Currencies.Length; i++)
            {
                if (wallet == null || !ShopRules.TryNumber(wallet[Currencies[i]], out balances[i]) || balances[i] < 0)
                    return GameResult.Reject(GameCodes.Unavailable, "角色资源数据无效");
                if (balances[i] < costs[i]) return GameResult.Reject(GameCodes.InsufficientFunds, "升级失败" + Currencies[i] + "不足\n需要" + Currencies[i] + costs[i]);
                deducted[i] = Math.Floor((float)costs[i]);
                if (balances[i] < deducted[i]) return GameResult.Reject(GameCodes.InsufficientFunds, "升级失败" + Currencies[i] + "不足\n还需" + Currencies[i] + (deducted[i] - balances[i]));
            }
            for (int i = 0; i < Currencies.Length; i++) wallet[Currencies[i]] = balances[i] - deducted[i];
            nation[technology] = (double)level + 1;
            nation["科技等级"] = (double)total + 1;
            var result = GameResult.Success(new JObject { ["technology"] = technology, ["level"] = level + 1,
                ["total"] = total + 1, ["tag"] = nation["国号"].DeepClone(),
                ["cost"] = new JObject { ["黄金"] = deducted[0], ["粮食"] = deducted[1], ["铜钱"] = deducted[2] } });
            result.Message = "黄金-" + deducted[0] + "\r\n粮食-" + deducted[1] + "\r\n铜钱-" + deducted[2];
            return result;
        }
    }
}
