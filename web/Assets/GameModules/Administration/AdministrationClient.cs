using System;
using System.Linq;
using Dwsg.Network;
using Dwsg.Shared;
using Dwsg.Window3;
using Newtonsoft.Json.Linq;
using 缺失界面.窗口2;

namespace Dwsg.Administration
{
    public static class AdministrationClient
    {
        public static string StablePlayer(int index)
        {
            var rows = GameNetwork.CurrentSnapshot?.PublicWorld["玩家列表"] as JArray;
            return rows != null && index >= 0 && index < rows.Count ? rows[index].Value<string>("playerId") : null;
        }
        public static int PlayerIndex(string id)
        {
            var rows = GameNetwork.CurrentSnapshot?.PublicWorld["玩家列表"] as JArray;
            if (id != null && rows != null) for (int i = 0; i < rows.Count; i++) if (rows[i].Value<string>("playerId") == id) return i;
            return -1;
        }
        public static JObject PublicCity(int x, int y)
        {
            return (GameNetwork.CurrentSnapshot?.PublicWorld["城池列表"] as JArray)?.OfType<JObject>().FirstOrDefault(c => c.Value<int>("坐标x") == x && c.Value<int>("坐标y") == y);
        }
        public static void Publish(INationDataSource local, string tag, NationNoticeKind kind, string text, string expectedText, Action<NationActionResult> done)
        {
            if (!GameNetwork.Enabled) { done(local.Publish(tag, kind, text)); return; }
            GameNetwork.SendCommand("nation.publish", new JObject { ["tag"] = tag, ["kind"] = kind.ToString(), ["text"] = text, ["expectedText"] = expectedText }, r => done(NationResult(r)));
        }
        public static void Appoint(INationDataSource local, string tag, NationOffice office, int target, int expectedHolder, Action<NationActionResult> done)
        {
            if (!GameNetwork.Enabled) { done(local.Appoint(tag, office, target)); return; }
            var member = StablePlayer(target);
            if (target >= 0 && member == null) { done(NationActionResult.Fail(NationError.MissingMember, "目标成员尚未同步。")); return; }
            GameNetwork.SendCommand("nation.appoint", new JObject { ["tag"] = tag, ["office"] = office.ToString(), ["memberId"] = member, ["expectedHolder"] = StablePlayer(expectedHolder) }, r => done(NationResult(r)));
        }
        private static NationActionResult NationResult(GameResult result)
        {
            return result.Code == GameCodes.Ok ? NationActionResult.Ok(result.Message) : NationActionResult.Fail(result.Code == GameCodes.Forbidden ? NationError.Forbidden : NationError.InvalidInput, result.Message);
        }
        private static void Send(string command, JObject payload, Func<CityResult> offline, Action<CityResult> done)
        {
            if (!GameNetwork.Enabled) { done(offline()); return; }
            GameNetwork.SendCommand(command, payload, r => done(r.Code == GameCodes.Ok ? CityResult.Ok(r.Message) : CityResult.Fail(r.Message)));
        }
        public static void Repair(CityRepairQuote quote, string request, Action<CityResult> done)
        {
            Send("city.repair", new JObject { ["x"] = quote.X, ["y"] = quote.Y, ["kind"] = (int)quote.Kind, ["before"] = quote.Before,
                ["owner"] = StablePlayer(quote.Owner), ["nation"] = quote.Nation }, () => CityLocalAdapter.Local.Repair(quote, request), done);
        }
        public static void Collect(int x, int y, CityTaxKind kind, int owner, string nation, double copper, double food, string request, Action<CityResult> done)
        {
            Send("city.collect", new JObject { ["x"] = x, ["y"] = y, ["kind"] = (int)kind, ["owner"] = StablePlayer(owner), ["nation"] = nation, ["copper"] = copper, ["food"] = food },
                () => CityLocalAdapter.Local.Collect(x, y, kind, request), done);
        }
        public static void Apply(int x, int y, Action<CityResult> done)
        {
            var city = CityLocalAdapter.City(x, y);
            Send("city.apply", new JObject { ["x"] = x, ["y"] = y, ["nation"] = city == null ? "" : city.国家 },
                () => CityLocalAdapter.Local.Apply(x, y, Guid.NewGuid().ToString("N")), done);
        }
        public static void AppointCity(int x, int y, int target, int owner, string nation, string request, Action<CityResult> done)
        {
            Send("city.appoint", new JObject { ["x"] = x, ["y"] = y, ["memberId"] = StablePlayer(target), ["owner"] = StablePlayer(owner), ["nation"] = nation },
                () => CityLocalAdapter.Local.Appoint(x, y, target, request), done);
        }
        public static void Bookmark(int x, int y, bool add, Action<CityResult> done)
        {
            Send("city.bookmark", new JObject { ["x"] = x, ["y"] = y, ["add"] = add }, () => CityLocalAdapter.Local.Bookmark(x, y, add), done);
        }
    }
}
