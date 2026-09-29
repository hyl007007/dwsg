using System;
using System.Globalization;
using System.Linq;
using System.Text;
using Dwsg.Network;
using Dwsg.Shared;
using Newtonsoft.Json.Linq;

namespace Dwsg.Auxiliary
{
    public static class AuxiliaryClient
    {
        public static void EquipTitle(string name, Action<GameResult> completed)
        { GameNetwork.SendCommand("title.equip", new JObject { ["name"] = name }, completed); }
        public static void Bet(int type, int amount, Action<GameResult> completed)
        { GameNetwork.SendCommand("casino.bet", new JObject { ["type"] = type, ["amount"] = amount }, completed); }
        private static readonly string[] Categories = { "压小", "压大", "豹子" };
        private static string Money(double amount) { return amount.ToString("0.##", CultureInfo.InvariantCulture); }
        public static string CasinoSummary()
        {
            var snapshot = GameNetwork.CurrentSnapshot;
            var casino = snapshot == null ? null : snapshot.PrivatePlayer["casino"] as JObject;
            if (casino == null) return "赌场状态正在同步";
            var bets = casino["bets"] as JArray ?? new JArray();
            if (bets.Count > 0)
            {
                long seconds = (long)Math.Ceiling(Math.Max(0, casino.Value<long>("nextUtcMs") - snapshot.ServerUtcMs) / 1000d);
                var text = new StringBuilder(casino.Value<bool>("drawing") ? "开奖中 · 剩余 " : "等待开局 · 剩余 ");
                text.Append(seconds).Append(" 秒");
                foreach (var bet in bets) text.Append('\n').Append(Categories[bet.Value<int>("type")]).Append(' ').Append(bet.Value<int>("amount"));
                return text.Append("\n本局已扣 ").Append(bets.Sum(b=>b.Value<long>("amount"))).Append(" 黄金").ToString();
            }
            var last = casino["last"] as JObject;
            if (last == null) return "尚未下注\n输入金额后选择下注类别\n大小返还1.2倍 · 豹子返还1.5倍";
            var dice = (JArray)last["dice"];
            int a=dice[0].Value<int>(), b=dice[1].Value<int>(), c=dice[2].Value<int>();
            var result = new StringBuilder("已结算 · 开奖 ").Append(a).Append('·').Append(b).Append('·').Append(c)
                .Append(a+b+c < 11 ? "（小" : "（大");
            if (a==b && b==c) result.Append("、豹子");
            result.Append("）");
            var settled=(JArray)last["bets"];
            foreach (var bet in settled)
            {
                double returned=bet.Value<double>("returned");
                result.Append('\n').Append(Categories[bet.Value<int>("type")]).Append(' ').Append(bet.Value<int>("amount"))
                    .Append(returned>0 ? " · 返还 " : " · 输 ").Append(returned>0 ? Money(returned) : bet.Value<int>("amount").ToString());
            }
            long total=settled.Sum(bet=>bet.Value<long>("amount")); double payout=last.Value<double>("payout");
            double net=Math.Round(payout-total,2,MidpointRounding.AwayFromZero);
            return result.Append("\n合计下注 ").Append(total).Append(" · 返还 ").Append(Money(payout))
                .Append('\n').Append(net>0 ? "本局净赚 " : net<0 ? "本局净亏 " : "本局持平 ").Append(Money(Math.Abs(net))).Append(" 黄金").ToString();
        }
    }
}
