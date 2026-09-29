using System;
using System.Linq;
using Dwsg.Shared;
using Dwsg.Shared.Economy;
using Newtonsoft.Json.Linq;
using 玩家数据结构;

public static class ProductionClient
{
    public static void Execute(string commandType, 玩家数据 player, int fiefIndex, int plot, int? buildingType, Action<GameResult> completed)
    {
        if (fiefIndex < 0 || fiefIndex >= player.封地信息表.Count)
        {
            completed(GameResult.Reject(GameCodes.InvalidArgument, "封地不存在"));
            return;
        }
        var fief = player.封地信息表[fiefIndex];
        if (Dwsg.Network.GameNetwork.Enabled)
        {
            var snapshot = Dwsg.Network.GameNetwork.CurrentSnapshot;
            var mappings = snapshot?.PrivatePlayer?["entityMappings"]?["fiefs"] as JObject;
            var mapping = mappings?.Properties().FirstOrDefault(p => p.Value is JObject &&
                p.Value.Value<string>("playerId") == snapshot.PlayerId && p.Value.Value<int>("legacyId") == fief.ID);
            if (mapping == null)
            {
                completed(GameResult.Reject(GameCodes.Unavailable, "封地身份尚未同步，请重试"));
                return;
            }
            var payload = new JObject { ["fiefId"] = mapping.Name, ["plot"] = plot };
            if (buildingType.HasValue) payload["buildingType"] = buildingType.Value;
            Dwsg.Network.GameNetwork.SendCommand(commandType, payload, completed);
            return;
        }
        var state = State(player);
        var fiefState = (JObject)state["封地信息表"][fiefIndex];
        var result = commandType == "fief.construct" && buildingType.HasValue
            ? BuildingRules.Construct(state, fiefState, plot, buildingType.Value)
            : commandType == "fief.upgrade" ? BuildingRules.Upgrade(state, fiefState, plot)
            : commandType == "fief.demolish" ? BuildingRules.Demolish(state, fiefState, plot)
            : GameResult.Reject(GameCodes.InvalidArgument, "建筑命令无效");
        if (result.Code == GameCodes.Ok)
        {
            // Preserve the original building, fief, wallet and UI references.
            fief.建筑信息表[plot].类型 = fiefState["建筑信息表"][plot].Value<int>("类型");
            fief.建筑信息表[plot].等级 = fiefState["建筑信息表"][plot].Value<int>("等级");
            Apply(player, state);
        }
        completed(result);
    }

    // The original resource coroutine calls this only in offline mode, once per second.
    public static GameResult ProduceOffline(玩家数据 player, double resourceTechnology)
    {
        var state = State(player);
        var result = ProductionRules.ProduceOneSecond(state, resourceTechnology);
        if (result.Code == GameCodes.Ok) Apply(player, state);
        return result;
    }

    private static JObject State(玩家数据 player)
    {
        return new JObject { ["基础信息"] = new JObject { ["粮食增加"] = player.基础信息.粮食增加 },
            ["财产信息"] = JObject.FromObject(player.财产信息),
            ["封地信息表"] = new JArray(player.封地信息表.Select(fief => new JObject
                { ["ID"] = fief.ID, ["建筑信息表"] = JArray.FromObject(fief.建筑信息表) })) };
    }

    private static void Apply(玩家数据 player, JObject state)
    {
        var wallet = state["财产信息"];
        player.财产信息.铜钱 = wallet.Value<double>("铜钱");
        player.财产信息.粮食 = wallet.Value<double>("粮食");
        player.财产信息.黄金 = wallet.Value<double>("黄金");
        player.财产信息.白银 = wallet.Value<double>("白银");
        player.基础信息.粮食增加 = state["基础信息"].Value<double>("粮食增加");
    }
}
