using System;
using Newtonsoft.Json.Linq;

namespace Dwsg.Shared.Economy
{
    public static class TechnologyRules
    {
        public const int Count = 21;
        private static readonly string[] Names = { "工程设计", "征召技巧", "种植技术", "行军技巧", "市场贸易", "建筑学", "铸铁技术",
            "甲胄制造", "药草研究", "阵法技巧", "抛射技巧", "驾驭技巧", "战车设计", "统帅能力", "信仰", "仓储", "安置", "格斗", "精准", "驯马", "精工" };
        private static readonly double[] Bases = { 2, 2, 0.24, 2.2, 0.24, 0.24, 2.8, 2.5, 2.5, 2, 2, 2, 2, 3, 0.4, 5, 5, 5, 5, 5, 5 };

        public static string NameAt(int index) { return index >= 0 && index < Count ? Names[index] : null; }
        public static int Maximum(int index) { return index == 0 ? 15 : index >= 15 ? 5 : 10; }
        public static string Currency(int index) { return index >= 15 ? "黄金" : "铜钱"; }

        public static double UpgradeCost(int index, int level)
        {
            if (index < 0 || index >= Count || level < 0 || level > 15) throw new ArgumentOutOfRangeException();
            double cost = Bases[index];
            // Keep the original double multiplication order and its common copper/gold cost cap.
            for (int i = 0; i < level; i++) cost *= 2.0;
            return Math.Min(cost * 10000.0, 15000000.0);
        }

        public static GameResult Upgrade(JObject player, JObject fief, int plot, string technology, int expectedLevel)
        {
            int index = Array.IndexOf(Names, technology);
            if (index < 0) return GameResult.Reject(GameCodes.NotFound, "科技不存在");
            var buildings = fief?["建筑信息表"] as JArray;
            if (buildings == null || plot < 1 || plot >= buildings.Count || !(buildings[plot] is JObject))
                return GameResult.Reject(GameCodes.InvalidArgument, "书院位置无效");
            var academy = (JObject)buildings[plot];
            double type, academyLevel, level;
            if (!ShopRules.TryNumber(academy["类型"], out type) || type != 1 || !ShopRules.TryNumber(academy["等级"], out academyLevel) ||
                academyLevel < 1 || academyLevel > (fief.Value<int>("ID") == 1 ? 15 : 10) || academyLevel != Math.Truncate(academyLevel))
                return GameResult.Reject(GameCodes.Conflict, "该位置没有有效书院");
            var technologies = player?["科技信息"] as JObject;
            if (technologies == null || !ShopRules.TryNumber(technologies[technology], out level) || level < 0 || level > Maximum(index) || level != Math.Truncate(level))
                return GameResult.Reject(GameCodes.Unavailable, "原个人科技等级无效");
            if (level != expectedLevel) return GameResult.Reject(GameCodes.Conflict, "科技等级已更新，请刷新");
            if (level >= Maximum(index)) return GameResult.Reject(GameCodes.Conflict, "科技已满级!");
            if (academyLevel <= level + (index >= 15 ? 10 : 0)) return GameResult.Reject(GameCodes.Conflict, "书院等级不足!");
            string currency = Currency(index);
            var wallet = player["财产信息"] as JObject;
            double balance;
            if (wallet == null || !ShopRules.TryNumber(wallet[currency], out balance) || balance < 0)
                return GameResult.Reject(GameCodes.Unavailable, "角色资源数据无效");
            double cost = UpgradeCost(index, (int)level);
            if (balance < cost) return GameResult.Reject(GameCodes.InsufficientFunds, currency + "不足 " + (cost / 10000.0) + "W 升级失败!");
            // Same >= cost wallet rule as the original 财产信息 and M05 commerce rules.
            wallet[currency] = balance - cost;
            technologies[technology] = level + 1.0;
            var result = GameResult.Success(new JObject { ["technology"] = technology, ["level"] = level + 1.0, ["currency"] = currency, ["cost"] = cost });
            result.Message = "升级成功,消耗" + currency + ":" + (cost / 10000.0) + "W";
            return result;
        }
    }
}
