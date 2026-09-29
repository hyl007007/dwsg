using System;
using System.Linq;
using Dwsg.Shared;
using Dwsg.Shared.Economy;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public static class TechnologyClient
{
    public static void Upgrade(int playerIndex, int fiefIndex, int plot, int technologyIndex, Action<GameResult> completed)
    {
        string name = TechnologyRules.NameAt(technologyIndex);
        if (name == null || playerIndex != 全局变量.本机身份 || playerIndex < 0 || playerIndex >= 全局变量.所有玩家数据表.Count)
        { completed(GameResult.Reject(GameCodes.Forbidden, "只能研究当前角色的科技")); return; }
        var player = 全局变量.所有玩家数据表[playerIndex];
        if (fiefIndex < 0 || fiefIndex >= player.封地信息表.Count)
        { completed(GameResult.Reject(GameCodes.NotFound, "封地不存在")); return; }
        var fief = player.封地信息表[fiefIndex];
        var technologies = JObject.FromObject(player.科技信息);
        double level;
        if (!ShopRules.TryNumber(technologies[name], out level) || level < 0 || level > 15 || level != Math.Truncate(level))
        { completed(GameResult.Reject(GameCodes.Unavailable, "科技等级尚未同步")); return; }
        if (Dwsg.Network.GameNetwork.Enabled)
        {
            var snapshot = Dwsg.Network.GameNetwork.CurrentSnapshot;
            var mappings = snapshot?.PrivatePlayer?["entityMappings"]?["fiefs"] as JObject;
            string stableId = mappings?.Properties().SingleOrDefault(p => p.Value is JObject &&
                p.Value.Value<string>("playerId") == snapshot.PlayerId && p.Value.Value<int>("legacyId") == fief.ID)?.Name;
            if (stableId == null) { completed(GameResult.Reject(GameCodes.Unavailable, "封地身份尚未同步，请重试")); return; }
            Dwsg.Network.GameNetwork.SendCommand("technology.upgrade", new JObject { ["fiefId"] = stableId, ["plot"] = plot,
                ["technology"] = name, ["expectedLevel"] = (int)level }, completed);
            return;
        }
        var state = new JObject { ["科技信息"] = technologies, ["财产信息"] = JObject.FromObject(player.财产信息) };
        var result = TechnologyRules.Upgrade(state, new JObject { ["ID"] = fief.ID, ["建筑信息表"] = JArray.FromObject(fief.建筑信息表) }, plot, name, (int)level);
        if (result.Code == GameCodes.Ok)
        {
            JsonConvert.PopulateObject(technologies.ToString(), player.科技信息);
            player.财产信息.铜钱 = state["财产信息"].Value<double>("铜钱");
            player.财产信息.黄金 = state["财产信息"].Value<double>("黄金");
        }
        completed(result);
    }
}
