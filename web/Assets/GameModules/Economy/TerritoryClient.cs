using System;
using Dwsg.Shared;
using Dwsg.Shared.Economy;
using Newtonsoft.Json.Linq;
using 玩家数据结构;

public static class TerritoryClient
{
    public static void CreateFief(城池信息库类 city, Action<GameResult> completed)
    {
        if (Dwsg.Network.GameNetwork.Enabled)
        {
            Dwsg.Network.GameNetwork.SendCommand("fief.create", new JObject { ["cityX"] = city.坐标x, ["cityY"] = city.坐标y }, completed);
            return;
        }
        int index = 全局变量.本机身份;
        var player = 全局变量.所有玩家数据表[index];
        var template = new 封地信息();
        template.初始化封地信息();
        template.初始化建筑列表();
        const string playerId = "offline";
        var state = new WorldState
        {
            Data = new JObject { ["玩家列表"] = JArray.FromObject(全局变量.所有玩家数据表),
                ["城池列表"] = new JArray(JObject.FromObject(city)), ["初始封地模板"] = JObject.FromObject(template),
                ["城池容量配置"] = new JArray(new JObject { ["规模"] = city.规模, ["容量"] = city.获取封地上限() }) },
            EntityMappings = new JObject { ["players"] = new JObject { [playerId] = index } }
        };
        var result = TerritoryRules.CreateFief(state, playerId, city.坐标x, city.坐标y);
        if (result.Code == GameCodes.Ok)
        {
            var updated = state.RequirePlayer(playerId);
            var fief = ((JArray)updated["封地信息表"]).Last.ToObject<封地信息>();
            player.封地信息表.Add(fief);
            player.封地ID标识 = updated.Value<int>("封地ID标识");
            city.城池封地列表.Add(new 封地索引(index, fief.ID));
        }
        completed(result);
    }
}
