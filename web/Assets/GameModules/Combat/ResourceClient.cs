using System;
using System.Collections.Generic;
using System.Linq;
using Dwsg.Network;
using Dwsg.Shared;
using Dwsg.Window3;
using Newtonsoft.Json.Linq;
using 玩家数据结构;

namespace Dwsg.Combat
{
    public static class ResourceClient
    {
        private static bool pending;
        public static bool Ready => GameNetwork.Connected && Read().Count > 0;
        public static List<资源点状态> Read(string search = "")
        {
            var snapshot = GameNetwork.CurrentSnapshot;
            var points = snapshot?.PublicWorld["resources"]?["points"] as JArray;
            var players = snapshot?.PublicWorld["玩家列表"] as JArray;
            var result = new List<资源点状态>();
            if (points == null || players == null) return result;
            foreach (JObject raw in points)
            {
                var point = (JObject)raw.DeepClone();
                string owner = point.Value<string>("ownerId");
                point["占领玩家ID"] = string.IsNullOrEmpty(owner) ? -1 : players.ToList().FindIndex(p => p.Value<string>("playerId") == owner);
                var value = point.ToObject<资源点状态>();
                if (string.IsNullOrEmpty(search) || (value.类型 + " " + value.坐标x + "," + value.坐标y).IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0) result.Add(value);
            }
            return result;
        }
        public static List<资源点军情信息> Armies()
        {
            return 全局变量.军情列表.OfType<资源点军情信息>().Where(a => a.世界标识 == GameNetwork.CurrentSnapshot?.WorldId && a.阶段 != 资源出征阶段.已结束).ToList();
        }
        public static CityResult CheckTarget(string id)
        {
            if (!GameNetwork.Connected || !GameNetwork.HasRole) return CityResult.Fail("请先连接服务器");
            var point = Read().Find(p => p.标识 == id);
            if (point == null) return CityResult.Fail("资源点尚未同步");
            var raw = (GameNetwork.CurrentSnapshot.PublicWorld["resources"]?["points"] as JArray)?.OfType<JObject>().SingleOrDefault(p => p.Value<string>("标识") == id);
            if (point.占领玩家ID >= 0 || point.剩余库存 <= 0 || point.恢复时间 != 0 || raw?.Value<bool>("pending") == true)
                return CityResult.Fail("资源点已占领、正在恢复或已有部队前往");
            var snapshot = GameNetwork.CurrentSnapshot;
            int occupied = (snapshot.PrivatePlayer["战斗运行"] as JObject)?.Properties().Count(p => p.Value.Value<string>("Kind") == "resource" &&
                p.Value.Value<string>("PlayerId") == snapshot.PlayerId && !p.Value.Value<bool>("SettlementApplied")) ?? 0;
            if (Read().Count(p => p.占领玩家ID == 全局变量.本机身份) + occupied >= 2) return CityResult.Fail("每人最多占领两处资源点");
            return CityResult.Ok("可以出征");
        }
        public static void Dispatch(string id, IList<将领信息> generals, Action<GameResult> completed)
        {
            string[] ids = generals.Select(g => CombatClient.GeneralId(g.ID)).ToArray();
            if (ids.Any(string.IsNullOrEmpty)) { completed(GameResult.Reject(GameCodes.Conflict, "请等待将领同步")); return; }
            Send("combat.resource.dispatch", new JObject { ["resourceId"] = id, ["generalIds"] = new JArray(ids) }, completed);
        }
        public static void Abandon(string id, Action<GameResult> completed)
        { Send("resource.abandon", new JObject { ["resourceId"] = id }, completed); }
        public static void Withdraw(资源点军情信息 army, Action<GameResult> completed)
        { Send("combat.resource.withdraw", new JObject { ["battleId"] = army.服务器战场ID }, completed); }
        private static void Send(string type, JObject payload, Action<GameResult> completed)
        {
            if (pending) { completed(GameResult.Reject(GameCodes.Conflict, "正在等待服务器确认")); return; }
            var snapshot = GameNetwork.CurrentSnapshot;
            if (!GameNetwork.Connected || snapshot == null) { completed(GameResult.Reject(GameCodes.Unavailable, "请先连接服务器")); return; }
            string role = snapshot.PlayerId, world = snapshot.WorldId;
            pending = true;
            GameNetwork.SendCommand(type, payload, result => {
                pending = false;
                if (GameNetwork.CurrentSnapshot?.PlayerId != role || GameNetwork.CurrentSnapshot?.WorldId != world) return;
                if (result.Code == GameCodes.Ok && string.IsNullOrEmpty(result.Message)) result.Message = "服务器已确认";
                completed(result);
            });
        }
    }
}
