using System;
using System.Collections.Generic;
using System.Linq;
using Dwsg.Shared;
using Dwsg.Shared.Economy;
using Dwsg.Window1;
using Newtonsoft.Json.Linq;

namespace Dwsg.Server.Progress
{
    internal sealed class ProgressWorldAdapter : ILocalWorld, ISixMinistriesWorld
    {
        private readonly WorldState state;
        private readonly JObject player;
        public string PlayerKey { get; }
        public string ActorKey => PlayerKey;
        public bool IsCurrent => IsBoundWorld;
        public bool IsBoundWorld => state.EntityMappings["humanPlayers"]?[PlayerKey]?.Value<bool>() == true;
        public ISet<string> PlayerKeys => new HashSet<string>((state.EntityMappings["humanPlayers"] as JObject ?? new JObject()).Properties()
            .Where(p => p.Value.Type == JTokenType.Boolean && p.Value.Value<bool>()).Select(p => p.Name), StringComparer.Ordinal);
        public ProgressWorldAdapter(WorldState state, string playerId)
        { this.state = state; PlayerKey = playerId; player = state.RequirePlayer(playerId); }
        public bool TryRead(out WorldProgress progress, out string error)
        {
            progress = null; error = "服务器角色进度数据无效";
            if (!(player["基础信息"] is JObject info) || !(player["封地信息表"] is JArray lands) || !(player["科技信息"] is JObject tech)) return false;
            var result = new WorldProgress { LordLevel = Number(info["等级"]), Merit = Number(info["战功"]) };
            var seen = new HashSet<int>();
            foreach (var land in lands.OfType<JObject>())
            {
                foreach (var b in (land["建筑信息表"] as JArray ?? new JArray()).OfType<JObject>())
                {
                    var level = Number(b["等级"]);
                    if (b.Value<int>("类型") < 0 || level <= 0) continue;
                    result.Buildings++; result.BuildingLevels += level;
                    if (b.Value<int>("类型") == 0) result.HallLevel = Math.Max(result.HallLevel, level);
                }
                foreach (var g in (land["将领信息表"] as JArray ?? new JArray()).OfType<JObject>())
                {
                    if (!seen.Add(g.Value<int>("ID"))) continue;
                    var level = Number(g["将领属性"]?["成长点数"]?["等级"]);
                    result.Generals++; result.GeneralLevels += level; result.GeneralLevel = Math.Max(result.GeneralLevel, level);
                    result.Troops += Number(g["将领配兵"]?["数量"]);
                }
                foreach (var t in (land["闲兵信息表"] as JArray ?? new JArray()).OfType<JObject>()) result.Troops += Number(t["数量"]);
            }
            foreach (var name in new[] { "工程设计", "征召技巧", "种植技术", "行军技巧", "市场贸易", "建筑学", "铸铁技术", "甲胄制造", "药草研究", "阵法技巧", "抛射技巧", "驾驭技巧", "战车设计", "统帅能力", "信仰", "仓储", "安置", "格斗", "精准", "驯马", "精工" })
                result.Technology += Number(tech[name]);
            if (!result.IsValid()) return false;
            progress = result; error = null; return true;
        }
        private static double Number(JToken token) => ShopRules.TryNumber(token, out var value) ? value : double.NaN;
        public bool ResourcesValid() => IsCurrent && new[] { "铜钱", "粮食", "白银", "黄金" }.All(k => WorldProgress.Number(Number(player["财产信息"]?[k])));
        public bool CanPay(double copper, double grain) => ResourcesValid() && WorldProgress.Number(copper) && WorldProgress.Number(grain) && Number(player["财产信息"]["铜钱"]) >= copper && Number(player["财产信息"]["粮食"]) >= grain;
        public bool CanReceive(double copper, double grain) => ResourcesValid() && WorldProgress.Number(copper) && WorldProgress.Number(grain) &&
            Number(player["财产信息"]["铜钱"]) + copper <= Reward.ResourceLimit && Number(player["财产信息"]["粮食"]) + grain <= Reward.ResourceLimit;
        public void Pay(double copper, double grain) { player["财产信息"]["铜钱"] = Number(player["财产信息"]["铜钱"]) - copper; player["财产信息"]["粮食"] = Number(player["财产信息"]["粮食"]) - grain; }
        public void Receive(double copper, double grain) { player["财产信息"]["铜钱"] = Number(player["财产信息"]["铜钱"]) + copper; player["财产信息"]["粮食"] = Number(player["财产信息"]["粮食"]) + grain; }
        public MinistryResult Train(object general) => MinistryResult.Fail("将领训练请使用将领命令");
        public bool TryGrant(Reward reward, out string error)
        {
            error = "奖励或资产无效，本次未发放";
            if (reward == null || !reward.IsValid() || !ResourcesValid()) return false;
            var fields = new[] { "铜钱", "粮食", "白银", "黄金" };
            var adds = new[] { reward.Copper, reward.Grain, reward.Silver, reward.Gold };
            for (int i = 0; i < fields.Length; i++) if (Number(player["财产信息"][fields[i]]) + adds[i] > Reward.ResourceLimit)
            { error = fields[i] + "领取后将超过20亿上限，请先使用部分资源"; return false; }
            int used = ShopRules.UsedSlots(player); double capacity = Number(player["基础信息"]?["背包容量上限"]);
            if (used < 0 || !WorldProgress.Number(capacity) || capacity != Math.Floor(capacity)) return false;
            var bag = (JObject)player["背包道具列表"].DeepClone(); int addedSlots = 0;
            foreach (var group in reward.Items.GroupBy(i => i.Name, StringComparer.Ordinal))
            {
                var definition = (state.Data["道具配置"] as JArray)?.OfType<JObject>().FirstOrDefault(d => d.Value<string>("名字") == group.Key);
                var stacks = bag[(definition?.Value<string>("分类") ?? "") + "道具列表"] as JArray;
                if (definition == null || stacks == null || stacks.Any(s => !(s is JObject) || s["名字"]?.Type != JTokenType.String || !WorldProgress.Number(Number(s["数量"])) || Number(s["数量"]) < 1 || Number(s["数量"]) > 999 || Number(s["数量"]) != Math.Floor(Number(s["数量"])))) return false;
                int count = group.Sum(i => i.Count);
                int slots = ItemStackRules.RequiredSlots<JToken>(stacks, group.Key, count, s => s.Value<string>("名字"), s => s.Value<double>("数量"));
                if (slots < 0) return false;
                addedSlots += slots;
                ItemStackRules.Add<JToken>(stacks, group.Key, count, s => s.Value<string>("名字"), s => s.Value<double>("数量"), (s, n) => s["数量"] = n,
                    (name, n) => new JObject { ["名字"] = name, ["数量"] = n, ["ID"] = 0 });
            }
            if (reward.Items.Count > 0 && used + addedSlots > capacity) { error = "背包空间不足，请先整理背包"; return false; }
            for (int i = 0; i < fields.Length; i++) player["财产信息"][fields[i]] = Number(player["财产信息"][fields[i]]) + adds[i];
            if (reward.Items.Count > 0) player["背包道具列表"] = bag;
            error = null; return true;
        }
    }
}
