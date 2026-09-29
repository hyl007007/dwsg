using System;
using System.Globalization;
using Newtonsoft.Json.Linq;

namespace Dwsg.Shared.Economy
{
    public static class MarketRules
    {
        public static JObject Quote(JObject player)
        {
            double originalLevel;
            if (!ShopRules.TryNumber(player?["基础信息"]?["等级"], out originalLevel)) return null;
            float level = (float)originalLevel;
            float copper = level * 10f;
            if (level <= 0 || float.IsInfinity(copper) || float.IsNaN(copper)) return null;
            return new JObject { ["quoteId"] = "market-v1:" + level.ToString("G9", CultureInfo.InvariantCulture),
                ["sourceLevel"] = (double)level, ["goldCopper"] = (double)copper,
                ["goldFood"] = (double)copper * 3.0, ["copperFood"] = 2.5, ["foodCopper"] = 0.3 };
        }

        public static string SourceCurrency(int type) { return type == 5 || type == 6 ? "黄金" : type == 7 ? "铜钱" : type == 8 ? "粮食" : null; }
        public static string TargetCurrency(int type) { return type == 5 || type == 8 ? "铜钱" : type == 6 || type == 7 ? "粮食" : null; }
        public static double Rate(JObject quote, int type)
        {
            string field = type == 5 ? "goldCopper" : type == 6 ? "goldFood" : type == 7 ? "copperFood" : type == 8 ? "foodCopper" : null;
            double value;
            return field != null && ShopRules.TryNumber(quote?[field], out value) && value > 0 ? value : 0;
        }

        public static double Maximum(JObject wallet, JObject quote, int type)
        {
            string source = SourceCurrency(type);
            double balance;
            double rate = Rate(quote, type);
            if (source == null || rate <= 0 || !ShopRules.TryNumber(wallet?[source], out balance) || balance < 0) return 0;
            float maximum = (float)(balance * rate);
            if (float.IsInfinity(maximum) || float.IsNaN(maximum)) return 0;
            return type == 7 || type == 8 ? Math.Floor(maximum) : (double)maximum;
        }

        public static GameResult Exchange(JObject player, int type, double quantity, string quoteId)
        {
            string source = SourceCurrency(type), target = TargetCurrency(type);
            // The original slider and text input both store a Unity float. Do not add a shop quantity limit here.
            if (source == null || target == null || double.IsNaN(quantity) || double.IsInfinity(quantity) ||
                quantity <= 0 || quantity != (double)(float)quantity)
                return GameResult.Reject(GameCodes.InvalidArgument, "请选择有效的兑换数量");
            JObject quote = Quote(player);
            if (quote == null) return GameResult.Reject(GameCodes.Unavailable, "市场报价尚未就绪");
            if (quoteId != quote.Value<string>("quoteId"))
                return GameResult.Reject(GameCodes.Conflict, "市场报价已更新，请按新报价确认");
            var wallet = player?["财产信息"] as JObject;
            foreach (string name in new[] { "黄金", "白银", "铜钱", "粮食" })
            {
                double value;
                if (!ShopRules.TryNumber(wallet?[name], out value) || value < 0)
                    return GameResult.Reject(GameCodes.Unavailable, "角色资源数据无效");
            }
            double balance = wallet.Value<double>(source);
            double cost = Math.Floor((float)(quantity / Rate(quote, type)));
            if (cost <= 0) return GameResult.Reject(GameCodes.InvalidArgument, "兑换数量过少，扣费不能为零");
            if (quantity > Maximum(wallet, quote, type) || cost > balance)
                return GameResult.Reject(GameCodes.InsufficientFunds, "兑换资源不足");
            var updated = (JObject)wallet.DeepClone();
            updated[source] = balance - cost;
            updated[target] = wallet.Value<double>(target) + quantity;
            var clamped = ProductionRules.ClampWallet(updated);
            if (clamped.Code != GameCodes.Ok) return clamped;
            foreach (var field in updated.Properties()) wallet[field.Name] = field.Value.DeepClone();
            var result = GameResult.Success(new JObject { ["exchangeType"] = type, ["quoteId"] = quoteId, ["quantity"] = quantity,
                ["sourceCurrency"] = source, ["targetCurrency"] = target, ["cost"] = cost,
                ["sourceBalance"] = wallet[source].DeepClone(), ["targetBalance"] = wallet[target].DeepClone() });
            result.Message = "兑换成功!";
            return result;
        }
    }
}
