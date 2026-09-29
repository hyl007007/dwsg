using System;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Dwsg.Shared.Economy
{
    public static class NationSalaryRules
    {
        public const long IntervalMs = 300000;
        private static readonly double[] Gold = { 0, 100000, 200000, 300000, 400000, 450000, 500000, 50000 };
        private static readonly double[] Rates = { 0, .5, .55, .6, .7, .75, .9, .95 };
        private static readonly int[] Merit = { 0, 2000, 3000, 5000, 7000, 8000, 15000, 0 };

        public static bool TryOffice(double warMerit, out int office)
        {
            office = 0;
            if (double.IsNaN(warMerit) || double.IsInfinity(warMerit) || warMerit < 0 || warMerit > int.MaxValue) return false;
            int merit = (int)warMerit;
            for (int i = 1; i <= 6; i++) if (merit >= Merit[i]) office = i;
            return true;
        }

        // Preserve all eight actual enum branches; the original refresh never assigns king (7).
        public static GameResult Quote(int office, JObject nation)
        {
            if (office < 0 || office > 7) return GameResult.Reject(GameCodes.Unavailable, "原官职数据无效");
            if (office == 0) return GameResult.Reject(GameCodes.Forbidden, "未有一官半职，没有俸禄可领取！！");
            double copper, grain;
            if (nation == null || !ShopRules.TryNumber(nation["铜钱"], out copper) || !ShopRules.TryNumber(nation["粮食"], out grain) || copper < 0 || grain < 0)
                return GameResult.Reject(GameCodes.Unavailable, "原国库资源数据无效");
            double paidCopper = copper * Rates[office], paidGrain = grain * Rates[office];
            if (Math.Truncate(paidCopper) > int.MaxValue || Math.Truncate(paidGrain) > int.MaxValue) return GameResult.Reject(GameCodes.Unavailable, "原俸禄数值超出范围");
            return GameResult.Success(new JObject { ["黄金"] = Gold[office], ["铜钱"] = (int)paidCopper, ["粮食"] = (int)paidGrain, ["战功"] = Merit[office] });
        }

        public static GameResult Claim(WorldState world, string playerId, int expectedOffice, long utcMs)
        {
            var player = world.RequirePlayer(playerId); int index = world.ResolvePlayerIndex(playerId);
            var basic = player["基础信息"] as JObject; var nation = TerritoryRules.Nation(world, basic?.Value<string>("国家"));
            double value; int office;
            if (nation == null) return GameResult.Reject(GameCodes.Unavailable, "原所属国家不存在");
            if (!ShopRules.TryNumber(basic?["ID"], out value) || value != index || !(nation["成员列表"] is JArray members) ||
                !members.Any(m => ShopRules.TryNumber(m, out value) && value == index)) return GameResult.Reject(GameCodes.Forbidden, "原国家成员身份不一致");
            if (!ShopRules.TryNumber(basic["官职"], out value) || value < 0 || value > 7 || value != Math.Truncate(value) ||
                !ShopRules.TryNumber(basic["战功"], out value) || !TryOffice(value, out office)) return GameResult.Reject(GameCodes.Unavailable, "原官职或战功数据无效");
            double warMerit = value;
            if (office != expectedOffice) return GameResult.Reject(GameCodes.Conflict, "官职已更新，请刷新");
            var clock = world.EntityMappings["nationSalary"]?[playerId] as JObject; long deadline;
            if (utcMs < 0 || clock?["readyUtcMs"]?.Type != JTokenType.Integer || !long.TryParse(clock["readyUtcMs"].ToString(), out deadline) || deadline < 0 || utcMs > long.MaxValue - IntervalMs)
                return GameResult.Reject(GameCodes.Unavailable, "原个人俸禄时钟尚未准备");
            if (utcMs < deadline) return GameResult.Reject(GameCodes.Conflict, "未到领取时间");
            var quote = Quote(office, nation); if (quote.Code != GameCodes.Ok) return quote;
            var wallet = player["财产信息"] as JObject;
            var next = new double[3]; string[] currencies = { "黄金", "铜钱", "粮食" };
            for (int i = 0; i < 3; i++)
            {
                double balance;
                if (wallet == null || !ShopRules.TryNumber(wallet[currencies[i]], out balance) || balance < 0 ||
                    double.IsInfinity(next[i] = balance + quote.Data.Value<double>(currencies[i]))) return GameResult.Reject(GameCodes.Unavailable, "原个人资源数据无效");
            }
            double remaining = warMerit - quote.Data.Value<int>("战功"); int newOffice;
            if (!TryOffice(remaining, out newOffice)) return GameResult.Reject(GameCodes.Unavailable, "原剩余战功数据无效");
            nation["铜钱"] = nation.Value<double>("铜钱") - quote.Data.Value<int>("铜钱");
            nation["粮食"] = nation.Value<double>("粮食") - quote.Data.Value<int>("粮食");
            for (int i = 0; i < 3; i++) wallet[currencies[i]] = next[i];
            basic["战功"] = remaining; basic["官职"] = newOffice; clock["readyUtcMs"] = utcMs + IntervalMs;
            quote.Data["office"] = office; quote.Data["newOffice"] = newOffice; quote.Data["readyUtcMs"] = utcMs + IntervalMs;
            quote.Message = "本次领取了黄金：" + quote.Data["黄金"] + "\n铜钱：" + quote.Data["铜钱"] + "\n粮食：" + quote.Data["粮食"];
            return quote;
        }
    }
}
