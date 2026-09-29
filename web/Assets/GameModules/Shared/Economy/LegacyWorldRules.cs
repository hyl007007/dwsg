using System.Linq;
using Newtonsoft.Json.Linq;

namespace Dwsg.Shared.Economy
{
    public static class LegacyWorldRules
    {
        public static GameResult CreatePlayer(WorldState candidate, string nickname, string nationName,
            long serverUtcMs, out int legacyPlayerIndex)
        {
            legacyPlayerIndex = -1;
            nickname = nickname?.Trim();
            if (string.IsNullOrEmpty(nickname) || nickname.Length > 20 || nickname.IndexOfAny(new[] { '<', '>', '\r', '\n' }) >= 0)
                return GameResult.Reject(GameCodes.InvalidArgument, "请输入 1 到 20 字的君主名，不能包含换行或尖括号。");
            var players = candidate.Data["玩家列表"] as JArray;
            var nation = (candidate.Data["国家列表"] as JArray)?.OfType<JObject>().FirstOrDefault(item => item.Value<string>("国号") == nationName);
            if (nation == null) return GameResult.Reject(GameCodes.NotFound, "国家不存在");
            var city = (candidate.Data["城池列表"] as JArray)?.OfType<JObject>().FirstOrDefault(item =>
                JToken.DeepEquals(item["坐标x"], nation["国都x"]) && JToken.DeepEquals(item["坐标y"], nation["国都y"]));
            var cityFiefs = city?["城池封地列表"] as JArray;
            var members = nation["成员列表"] as JArray;
            var playerTemplate = candidate.Data["新角色模板"] as JObject;
            var fiefTemplate = candidate.Data["初始封地模板"] as JObject;
            var capacityConfig = (candidate.Data["城池容量配置"] as JArray)?.OfType<JObject>()
                .FirstOrDefault(item => JToken.DeepEquals(item["规模"], city?["规模"]));
            double capacity;
            if (players == null || cityFiefs == null || members == null || playerTemplate == null || fiefTemplate == null ||
                capacityConfig == null || !ShopRules.TryNumber(capacityConfig["容量"], out capacity))
                return GameResult.Reject(GameCodes.Unavailable, "原世界初始化数据不完整");
            if (cityFiefs.Count >= capacity) return GameResult.Reject(GameCodes.WorldFull, "国都封地已满");
            var player = (JObject)playerTemplate.DeepClone();
            var basic = player["基础信息"] as JObject;
            var fiefs = player["封地信息表"] as JArray;
            if (basic == null || fiefs == null || fiefs.Count != 0 || player.Value<int?>("封地ID标识") != 1)
                return GameResult.Reject(GameCodes.Unavailable, "原新角色模板无效");
            int index = players.Count;
            basic["ID"] = index;
            basic["名字"] = nickname;
            basic["国家"] = nationName;
            var fief = (JObject)fiefTemplate.DeepClone();
            fief["ID"] = 1;
            fief["封地名字"] = nickname + "基地";
            fief["所在城池"] = new JObject { ["x"] = city["坐标x"].DeepClone(), ["y"] = city["坐标y"].DeepClone() };
            fiefs.Add(fief);
            player["封地ID标识"] = 2;
            cityFiefs.Add(new JObject { ["第几个玩家"] = index, ["封地ID标识"] = 1 });
            members.Add(index);
            players.Add(player);
            legacyPlayerIndex = index;
            return GameResult.Success(new JObject { ["nickname"] = nickname, ["nation"] = nationName });
        }
    }
}
