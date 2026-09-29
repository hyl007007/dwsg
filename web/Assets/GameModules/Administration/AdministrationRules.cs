using System;
using System.Collections.Generic;
using System.Linq;
using Dwsg.Shared;
using Dwsg.Shared.Economy;
using Newtonsoft.Json.Linq;

namespace Dwsg.Administration
{
    // Rules mirror the existing nation editor and city civic adapter. Both offline and
    // server commands use this document boundary; authoritative records keep their old schema.
    public static class AdministrationRules
    {
        public const long TaxInterval = 86400;
        public static readonly string[] Offices = { "大都督", "丞相", "奋武将军", "征东将军", "都尉", "侍郎" };
        public static JObject State(WorldState world)
        {
            var state = world.EntityMappings["administration"] as JObject;
            if (state == null) world.EntityMappings["administration"] = state = new JObject();
            foreach (string key in new[] { "Repairs", "Taxes", "Candidates", "Bookmarks" })
                if (state[key] == null) state[key] = new JArray();
            return state;
        }
        public static bool Number(JToken token, out double number)
        {
            number = 0;
            return token != null && (token.Type == JTokenType.Integer || token.Type == JTokenType.Float) &&
                double.TryParse(token.ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out number) && Valid(number);
        }
        public static bool Valid(double number) { return !double.IsNaN(number) && !double.IsInfinity(number) && number >= 0 && number <= 9007199254740991d; }
        private static GameResult Fail(string message, string code = GameCodes.InvalidArgument) { return GameResult.Reject(code, message); }
        private static GameResult Ok(string message) { var result = GameResult.Success(); result.Message = message; return result; }
        public static string StablePlayer(WorldState world, int index)
        {
            return (world.EntityMappings["players"] as JObject)?.Properties().FirstOrDefault(p => p.Value.Type == JTokenType.Integer && p.Value.Value<int>() == index)?.Name;
        }
        public static int PlayerIndex(WorldState world, string id)
        {
            int index;
            return id != null && world.EntityMappings["players"]?[id]?.Type == JTokenType.Integer && int.TryParse(world.EntityMappings["players"][id].ToString(), out index) ? index : -1;
        }
        private static JObject Player(WorldState world, int index)
        {
            var players = world.Data["玩家列表"] as JArray;
            return players != null && index >= 0 && index < players.Count ? players[index] as JObject : null;
        }
        public static bool Member(WorldState world, JObject nation, int index)
        {
            var player = Player(world, index);
            return nation != null && player != null && player["基础信息"]?.Value<string>("国家") == nation.Value<string>("国号") &&
                (nation.Value<int>("国王") == index || (nation["成员列表"] as JArray)?.Any(v => v.Type == JTokenType.Integer && v.Value<int>() == index) == true);
        }
        public static bool Friendly(WorldState world, JObject city, int index)
        {
            return city != null && Player(world, index) != null && (city.Value<int>("城主") == index ||
                Member(world, TerritoryRules.Nation(world, city.Value<string>("国家")), index));
        }
        public static bool Fighting(WorldState world, JObject city)
        {
            if (city.Value<bool?>("正在交战") == true) return true;
            return (world.Data["战斗运行"] as JObject)?.Properties().Select(p => p.Value).OfType<JObject>().Any(b =>
                b.Value<string>("Kind") == "city" && b.Value<string>("Phase") == "fighting" && !b.Value<bool>("SettlementApplied") &&
                b.Value<int>("X") == city.Value<int>("坐标x") && b.Value<int>("Y") == city.Value<int>("坐标y")) == true;
        }
        public static GameResult Publish(WorldState world, string actor, string tag, string kind, string text, string expectedText)
        {
            if (kind != "公告" && kind != "宣言" || text == null || expectedText == null) return Fail("公告类型或内容无效。");
            var nation = TerritoryRules.Nation(world, tag); int index = PlayerIndex(world, actor);
            if (!Member(world, nation, index) || !(nation.Value<int>("国王") == index || kind == "公告" && nation.Value<int>("丞相") == index))
                return Fail(kind == "公告" ? "仅本国国王或丞相可编辑公告。" : "仅本国国王可编辑宣言。", GameCodes.Forbidden);
            text = text.Trim(); int limit = kind == "公告" ? 400 : 100;
            if (text.Length > limit || text.Any(c => char.IsControl(c) && c != '\n' && c != '\r' && c != '\t')) return Fail("内容过长或包含不支持的控制字符。");
            if ((nation.Value<string>(kind) ?? "") != expectedText) return Fail("正文已经更新，请刷新后重新编辑。", GameCodes.Conflict);
            nation[kind] = text;
            return Ok("已" + (text.Length == 0 ? "清空" : "更新") + kind + "。");
        }
        public static GameResult AppointNation(WorldState world, string actor, string tag, string office, string member, string expectedHolder)
        {
            var nation = TerritoryRules.Nation(world, tag); int index = PlayerIndex(world, actor);
            if (!Member(world, nation, index) || nation.Value<int>("国王") != index) return Fail("仅本国国王可任免国家职务。", GameCodes.Forbidden);
            if (!Offices.Contains(office)) return Fail("任命参数无效。");
            int target = member == null ? -1 : PlayerIndex(world, member);
            if (member != null && (!Member(world, nation, target) || target == nation.Value<int>("国王"))) return Fail("请选择本国非国王成员，成员已换国时不能任命。", GameCodes.Forbidden);
            if (StablePlayer(world, nation.Value<int>(office)) != expectedHolder) return Fail("现任职务已变化，请刷新后重新任免。", GameCodes.Conflict);
            if (target >= 0) foreach (string old in Offices) if (nation.Value<int>(old) == target) nation[old] = -1;
            nation[office] = target;
            return Ok(target < 0 ? "已免去" + office + "。" : "已任命" + Player(world, target)["基础信息"].Value<string>("名字") + "为" + office + "。");
        }
        public static double Limit(JObject city, int kind)
        {
            if (kind == 1) return 100000;
            int scale = city.Value<int>("规模"); double[] walls = { 200000, 400000, 800000, 1600000, 2000000 };
            return scale >= 0 && scale < walls.Length ? walls[scale] : 0;
        }
        private static bool At(JObject item, int x, int y) { return item.Value<int>("X") == x && item.Value<int>("Y") == y; }
        private static IEnumerable<JObject> Entries(WorldState world, string key) { return (State(world)[key] as JArray).OfType<JObject>(); }
        public static GameResult RepairQuote(WorldState world, string actor, int x, int y, int kind)
        {
            var city = TerritoryRules.City(world, x, y); int index = PlayerIndex(world, actor);
            if (city == null || Player(world, index) == null) return Fail("城池或角色不存在。", GameCodes.NotFound);
            if (kind < 0 || kind > 1) return Fail("未知修筑类型。");
            if (!Friendly(world, city, index)) return Fail("只可修筑自己或本国城池。", GameCodes.Forbidden);
            if (Fighting(world, city)) return Fail("交战中不可修筑。", GameCodes.Conflict);
            if (Entries(world, "Repairs").Any(r => At(r, x, y))) return Fail("本城已有修筑任务，请等待完成。", GameCodes.Conflict);
            double before;
            if (!Number(city[kind == 0 ? "城墙" : "道路"], out before) || before > Limit(city, kind)) return Fail("设施数据异常，请恢复有效存档。");
            double amount = Math.Min(10000, Limit(city, kind) - before), copper = Math.Ceiling(amount / 10), food = Math.Ceiling(amount / 20);
            if (amount <= 0) return Fail("已达上限，无需修筑。", GameCodes.Conflict);
            double money, grain; var wallet = Player(world, index)["财产信息"];
            if (!Number(wallet?["铜钱"], out money) || !Number(wallet?["粮食"], out grain) || money < copper || grain < food) return Fail("铜钱或粮食不足，未扣费。", GameCodes.Conflict);
            var result = Ok("可以修筑。");
            result.Data = new JObject { ["X"] = x, ["Y"] = y, ["Kind"] = kind, ["Before"] = before, ["Amount"] = amount,
                ["Copper"] = copper, ["Food"] = food, ["Owner"] = StablePlayer(world, city.Value<int>("城主")), ["Nation"] = city.Value<string>("国家"), ["PlayerId"] = actor };
            return result;
        }
        public static GameResult Repair(WorldState world, string actor, int x, int y, int kind, double expectedBefore, string expectedOwner, string expectedNation, long now)
        {
            var quote = RepairQuote(world, actor, x, y, kind); if (quote.Code != GameCodes.Ok) return quote;
            var row = quote.Data;
            if (row.Value<double>("Before") != expectedBefore || row.Value<string>("Owner") != expectedOwner || row.Value<string>("Nation") != expectedNation) return Fail("城池状态或费用已变化，请重新确认。", GameCodes.Conflict);
            var wallet = world.RequirePlayer(actor)["财产信息"];
            wallet["铜钱"] = wallet.Value<double>("铜钱") - row.Value<double>("Copper"); wallet["粮食"] = wallet.Value<double>("粮食") - row.Value<double>("Food");
            row["StartedUtc"] = now; row["EndsUtc"] = checked(now + 30);
            ((JArray)State(world)["Repairs"]).Add(row);
            return Ok("修筑已开始，30秒后完成。");
        }
        public static bool RepairInvalid(WorldState world, JObject order)
        {
            var city = TerritoryRules.City(world, order.Value<int>("X"), order.Value<int>("Y"));
            int index = PlayerIndex(world, order.Value<string>("PlayerId"));
            return city == null || Player(world, index) == null || !Friendly(world, city, index) || Fighting(world, city) ||
                StablePlayer(world, city.Value<int>("城主")) != order.Value<string>("Owner") || city.Value<string>("国家") != order.Value<string>("Nation");
        }
        public static bool HasDue(WorldState world, long now)
        {
            var state = world.EntityMappings["administration"] as JObject;
            return (state?["Repairs"] as JArray)?.OfType<JObject>().Any(r => now >= r.Value<long>("EndsUtc") || RepairInvalid(world, r)) == true ||
                (world.Data["国家列表"] as JArray)?.OfType<JObject>().Any(n => n.Value<long>("轮选时间间隔") > 0 && now - n.Value<long>("上次轮选时间") >= n.Value<long>("轮选时间间隔")) == true;
        }
        public static GameResult Settle(WorldState world, long now)
        {
            foreach (var order in Entries(world, "Repairs").ToArray())
            {
                bool invalid = RepairInvalid(world, order);
                if (!invalid && now < order.Value<long>("EndsUtc")) continue;
                var city = TerritoryRules.City(world, order.Value<int>("X"), order.Value<int>("Y"));
                string field = order.Value<int>("Kind") == 0 ? "城墙" : "道路"; double before;
                if (!invalid && !Number(city[field], out before)) invalid = true;
                if (invalid)
                {
                    var player = Player(world, PlayerIndex(world, order.Value<string>("PlayerId"))); var wallet = player?["财产信息"];
                    double copper, food;
                    if (wallet == null || !Number(wallet["铜钱"], out copper) || !Number(wallet["粮食"], out food) ||
                        !Valid(copper + order.Value<double>("Copper")) || !Valid(food + order.Value<double>("Food")))
                        return Fail("修筑退款账户异常，保留任务等待恢复。", GameCodes.Unavailable);
                    wallet["铜钱"] = copper + order.Value<double>("Copper"); wallet["粮食"] = food + order.Value<double>("Food");
                }
                else city[field] = Math.Min(Limit(city, order.Value<int>("Kind")), city.Value<double>(field) + order.Value<double>("Amount"));
                order.Remove();
            }
            foreach (var nation in (world.Data["国家列表"] as JArray ?? new JArray()).OfType<JObject>())
            {
                long interval = nation.Value<long>("轮选时间间隔"), last = nation.Value<long>("上次轮选时间");
                // The original election only resets its timer. It does not replace the king.
                if (interval > 0 && now - last >= interval) nation["上次轮选时间"] = now;
            }
            return Ok("城池内政已结算。");
        }
        public static long TaxRemaining(WorldState world, int x, int y, int kind, long now)
        {
            var record = Entries(world, "Taxes").FirstOrDefault(t => At(t, x, y) && t.Value<int>("Kind") == kind);
            return record == null ? 0 : Math.Max(0, record.Value<long>("LastUtc") + TaxInterval - now);
        }
        public static GameResult TaxPermission(WorldState world, string actor, int x, int y, int kind, long now)
        {
            var city = TerritoryRules.City(world, x, y); int index = PlayerIndex(world, actor);
            if (city == null || Player(world, index) == null) return Fail("城池或角色不存在。", GameCodes.NotFound);
            if (Fighting(world, city)) return Fail("交战中不可征收。", GameCodes.Conflict);
            if (kind == 0) { if (city.Value<int>("城主") != index) return Fail("只有本城城主可征收。", GameCodes.Forbidden); }
            else if (kind == 1)
            {
                var nation = TerritoryRules.Nation(world, city.Value<string>("国家"));
                if (!Member(world, nation, index) || nation.Value<int>("国王") != index) return Fail("只有所属国家国王可征收，所得进入国库。", GameCodes.Forbidden);
            }
            else return Fail("未知征收类型。");
            if (TaxRemaining(world, x, y, kind, now) > 0) return Fail("本城该项已征收，24小时内不可重复。", GameCodes.Conflict);
            return Ok("");
        }
        public static GameResult Collect(WorldState world, string actor, int x, int y, int kind, string expectedOwner, string expectedNation, double expectedCopper, double expectedFood, long now)
        {
            var ready = TaxPermission(world, actor, x, y, kind, now); if (ready.Code != GameCodes.Ok) return ready;
            var city = TerritoryRules.City(world, x, y); string prefix = kind == 0 ? "城主征收_" : "国家征收_"; double copper, food;
            if (!Number(city[prefix + "铜"], out copper) || !Number(city[prefix + "粮"], out food) || copper + food <= 0) return Fail("本城没有可征收额度。");
            if (StablePlayer(world, city.Value<int>("城主")) != expectedOwner || city.Value<string>("国家") != expectedNation || copper != expectedCopper || food != expectedFood)
                return Fail("城池归属或征收额度已变化，请重新确认。", GameCodes.Conflict);
            var wallet = kind == 0 ? world.RequirePlayer(actor)["财产信息"] as JObject : TerritoryRules.Nation(world, city.Value<string>("国家"));
            double oldCopper, oldFood;
            if (!Number(wallet?["铜钱"], out oldCopper) || !Number(wallet?["粮食"], out oldFood) || !Valid(oldCopper + copper) || !Valid(oldFood + food)) return Fail("资源数值异常，未进行征收。");
            wallet["铜钱"] = oldCopper + copper; wallet["粮食"] = oldFood + food;
            var record = Entries(world, "Taxes").FirstOrDefault(t => At(t, x, y) && t.Value<int>("Kind") == kind);
            if (record == null) { record = new JObject { ["X"] = x, ["Y"] = y, ["Kind"] = kind }; ((JArray)State(world)["Taxes"]).Add(record); }
            record["LastUtc"] = now;
            return Ok("已收入" + (kind == 0 ? "个人财产" : "国家国库") + "：铜钱" + copper.ToString("N0") + " / 粮" + food.ToString("N0") + "。");
        }
        private static bool HasFief(JObject player, int x, int y)
        {
            return (player?["封地信息表"] as JArray)?.OfType<JObject>().Any(f => f["所在城池"]?.Value<int>("x") == x && f["所在城池"]?.Value<int>("y") == y) == true;
        }
        public static GameResult CandidatePermission(WorldState world, string actor, int x, int y)
        {
            var city = TerritoryRules.City(world, x, y); int index = PlayerIndex(world, actor); var player = Player(world, index);
            if (city == null || player == null) return Fail("城池或角色不存在。", GameCodes.NotFound);
            if (city.Value<int>("规模") == 4) return Fail("都城由国王治理，不参与城主竞选。", GameCodes.Forbidden);
            if (Fighting(world, city)) return Fail("交战中不可竞选。", GameCodes.Conflict);
            if (!Member(world, TerritoryRules.Nation(world, city.Value<string>("国家")), index) || !HasFief(player, x, y)) return Fail("竞选须为本国成员，并在本城拥有封地。", GameCodes.Forbidden);
            if (city.Value<int>("城主") == index) return Fail("你已是本城城主。", GameCodes.Conflict);
            if (Entries(world, "Candidates").Any(c => At(c, x, y) && c.Value<string>("PlayerId") == actor && c.Value<string>("Nation") == city.Value<string>("国家"))) return Fail("已登记候选，等待国王任命。", GameCodes.Conflict);
            return Ok("");
        }
        public static GameResult Apply(WorldState world, string actor, int x, int y, string expectedNation)
        {
            var ready = CandidatePermission(world, actor, x, y); if (ready.Code != GameCodes.Ok) return ready;
            var city = TerritoryRules.City(world, x, y);
            if (city.Value<string>("国家") != expectedNation) return Fail("城池归属已变化，请重新登记。", GameCodes.Conflict);
            foreach (var old in Entries(world, "Candidates").Where(c => At(c, x, y) && c.Value<string>("PlayerId") == actor).ToArray()) old.Remove();
            ((JArray)State(world)["Candidates"]).Add(new JObject { ["X"] = x, ["Y"] = y, ["PlayerId"] = actor, ["Nation"] = expectedNation });
            return Ok("已登记候选，等待本国国王任命。");
        }
        public static GameResult AppointCity(WorldState world, string actor, int x, int y, string member, string expectedOwner, string expectedNation)
        {
            var city = TerritoryRules.City(world, x, y); int index = PlayerIndex(world, actor), target = PlayerIndex(world, member);
            var nation = city == null ? null : TerritoryRules.Nation(world, city.Value<string>("国家"));
            if (!Member(world, nation, index) || nation.Value<int>("国王") != index) return Fail("只有所属国家国王可任命。", GameCodes.Forbidden);
            if (Fighting(world, city) || city.Value<int>("规模") == 4) return Fail("交战城池或都城不可任命。", GameCodes.Forbidden);
            if (city.Value<string>("国家") != expectedNation || StablePlayer(world, city.Value<int>("城主")) != expectedOwner) return Fail("城池归属已变化，请重新确认。", GameCodes.Conflict);
            if (!Member(world, nation, target) || !HasFief(Player(world, target), x, y) || !Entries(world, "Candidates").Any(c => At(c, x, y) && c.Value<string>("PlayerId") == member && c.Value<string>("Nation") == expectedNation))
                return Fail("候选人已失去资格，请重新登记。", GameCodes.Forbidden);
            city["城主"] = target;
            foreach (var old in Entries(world, "Candidates").Where(c => At(c, x, y)).ToArray()) old.Remove();
            return Ok("城主已任命为" + Player(world, target)["基础信息"].Value<string>("名字") + "。");
        }
        public static GameResult Bookmark(WorldState world, string actor, int x, int y, bool add)
        {
            if (Player(world, PlayerIndex(world, actor)) == null || add && TerritoryRules.City(world, x, y) == null) return Fail("无法收藏不存在的城池。");
            var rows = Entries(world, "Bookmarks").Where(b => b.Value<string>("PlayerId") == actor).ToArray();
            var old = rows.FirstOrDefault(b => At(b, x, y));
            if (add && old == null)
            {
                if (rows.Length >= 128) return Fail("每位君主最多收藏128座城池。", GameCodes.Conflict);
                ((JArray)State(world)["Bookmarks"]).Add(new JObject { ["PlayerId"] = actor, ["X"] = x, ["Y"] = y });
            }
            else if (!add) old?.Remove();
            return Ok(add ? "城池已加入收藏册。" : "已取消收藏。");
        }
    }
}
