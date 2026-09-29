using System;
using Dwsg.Network;
using Dwsg.Shared;
using Dwsg.Shared.Economy;
using Newtonsoft.Json.Linq;

namespace Dwsg.Economy
{
    public static class MarketClient
    {
        public static bool Pending { get; private set; }

        public static JObject GetQuote()
        {
            if (GameNetwork.Enabled)
                return GameNetwork.HasRole ? GameNetwork.CurrentSnapshot.PrivatePlayer["marketQuote"] as JObject : null;
            int index = 全局变量.本机身份;
            if (index < 0 || index >= 全局变量.所有玩家数据表.Count) return null;
            return MarketRules.Quote(new JObject { ["基础信息"] = JObject.FromObject(全局变量.所有玩家数据表[index].基础信息) });
        }

        public static double Maximum(JObject quote, int type)
        {
            int index = 全局变量.本机身份;
            return index < 0 || index >= 全局变量.所有玩家数据表.Count ? 0
                : MarketRules.Maximum(JObject.FromObject(全局变量.所有玩家数据表[index].财产信息), quote, type);
        }

        public static void Exchange(int type, double quantity, JObject quote, Action<GameResult> completed)
        {
            if (Pending) { completed(GameResult.Reject(GameCodes.Conflict, "正在兑换，请稍候")); return; }
            string quoteId = quote?.Value<string>("quoteId");
            if (string.IsNullOrEmpty(quoteId)) { completed(GameResult.Reject(GameCodes.Unavailable, "市场报价尚未就绪")); return; }
            if (GameNetwork.Enabled)
            {
                Pending = true;
                GameNetwork.SendCommand("market.exchange", new JObject { ["quoteId"] = quoteId, ["exchangeType"] = type, ["quantity"] = quantity }, result =>
                {
                    Pending = false;
                    completed(result);
                });
                return;
            }
            int index = 全局变量.本机身份;
            if (index < 0 || index >= 全局变量.所有玩家数据表.Count)
            { completed(GameResult.Reject(GameCodes.Forbidden, "请选择自己的角色")); return; }
            var player = 全局变量.所有玩家数据表[index];
            var data = new JObject { ["基础信息"] = JObject.FromObject(player.基础信息), ["财产信息"] = JObject.FromObject(player.财产信息) };
            var resultOffline = MarketRules.Exchange(data, type, quantity, quoteId);
            if (resultOffline.Code == GameCodes.Ok)
            {
                var wallet = data["财产信息"];
                player.财产信息.黄金 = wallet.Value<double>("黄金");
                player.财产信息.白银 = wallet.Value<double>("白银");
                player.财产信息.铜钱 = wallet.Value<double>("铜钱");
                player.财产信息.粮食 = wallet.Value<double>("粮食");
            }
            completed(resultOffline);
        }
    }
}
