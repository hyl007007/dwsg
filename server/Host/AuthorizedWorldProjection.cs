using Dwsg.Shared;
using Newtonsoft.Json.Linq;

namespace Dwsg.Host;

public sealed class AuthorizedWorldProjection : IWorldProjection
{
    private static JObject Select(JObject source, params string[] fields)
    {
        var result = new JObject();
        foreach (var field in fields) if (source[field] != null) result[field] = source[field].DeepClone();
        return result;
    }
    public WorldSnapshot Build(WorldState state, AuthenticatedActor actor, long serverUtcMs)
    {
        var players = (JArray)state.Data["玩家列表"];
        var mapping = (JObject)state.EntityMappings["players"];
        var ids = mapping.Properties().ToDictionary(p => p.Value.Value<int>(), p => p.Name);
        string PlayerId(JToken index) => index != null && index.Type == JTokenType.Integer && ids.TryGetValue(index.Value<int>(), out var id) ? id : null;
        var publicPlayers = new JArray();
        var npcIndexes = new HashSet<int>((state.Data["原玩家来源映射"] as JArray ?? new JArray()).Cast<JObject>()
            .Where(p => p.Value<bool>("NPC")).Select(p => p.Value<int>("原索引")));
        for (var i = 0; i < players.Count; i++)
        {
            var item = new JObject { ["playerId"] = ids[i], ["NPC"] = npcIndexes.Contains(i), ["基础信息"] = Select((JObject)players[i]["基础信息"],
                "名字", "性别", "头像", "等级", "称号名", "国家", "官阶", "官职", "声望") };
            if (npcIndexes.Contains(i))
                item["封地信息表"] = new JArray(((JArray)players[i]["封地信息表"]).Cast<JObject>().Select(f => new JObject {
                    ["将领信息表"] = new JArray(((JArray)f["将领信息表"]).Cast<JObject>().Select(g => new JObject {
                        ["将领属性"] = new JObject { ["初始属性"] = Select((JObject)g["将领属性"]["初始属性"], "名字", "突围") } })) }));
            publicPlayers.Add(item);
        }
        var privatePlayer = actor == null ? new JObject() : (JObject)state.RequirePlayer(actor.PlayerId).DeepClone();
        // Numeric legacy identity is reconstructed only inside the client adapter.
        (privatePlayer["基础信息"] as JObject)?.Remove("ID");
        var nation = privatePlayer["基础信息"]?.Value<string>("国家");
        var nations = new JArray();
        foreach (var original in ((JArray)state.Data["国家列表"]).Cast<JObject>())
        {
            var item = Select(original, "国名", "国号", "国都x", "国都y", "城池列表", "效率", "科技等级", "攻击科技", "防御科技", "资源科技", "公告", "宣言");
            item["国王"] = PlayerId(original["国王"]);
            item["成员列表"] = new JArray(((JArray)original["成员列表"]).Select(PlayerId));
            foreach (var office in new[] { "大都督", "丞相", "奋武将军", "征东将军", "都尉", "侍郎" }) item[office] = PlayerId(original[office]);
            if (nation == original.Value<string>("国号"))
                foreach (var field in new[] { "科技积分", "民生值", "铜钱", "粮食" }) item[field] = original[field]?.DeepClone();
            nations.Add(item);
        }
        var cities = new JArray();
        foreach (var original in ((JArray)state.Data["城池列表"]).Cast<JObject>())
        {
            var item = Select(original, "名称", "坐标x", "坐标y", "规模", "国家", "天赋类型", "天赋加成", "税率", "图腾类型", "图腾加成", "公告",
                "城墙", "道路", "炮塔", "战功", "协防几率", "协防数量f", "协防数量m");
            item["城主"] = PlayerId(original["城主"]);
            item["封地数量"] = (original["城池封地列表"] as JArray)?.Count ?? 0;
            cities.Add(item);
        }
        var world = new JObject { ["玩家列表"] = publicPlayers, ["国家列表"] = nations, ["城池列表"] = cities };
        world["chatMessages"] = Dwsg.Server.Chat.ChatModule.ReadHistory(state, actor);
        foreach (var field in new[] { "商城商品", "商城配置版本", "道具配置", "山贼难度配置", "将领姓名配置" })
            if (state.Data[field] != null) world[field] = state.Data[field].DeepClone();
        if (state.Data["山贼列表"] is JArray bandits)
            world["山贼列表"] = new JArray(bandits.Cast<JObject>().Select(b => {
                var item = Select(b, "坐标x", "坐标y", "等级", "难度", "掉落宝物", "掉落宝箱", "掉落装备");
                item["将领数据列表"] = new JArray(((JArray)b["将领数据列表"]).Cast<JObject>().Select(g => new JObject {
                    ["ID"] = g["ID"]?.DeepClone(),
                    ["将领配兵"] = Select((JObject)g["将领配兵"], "ID", "数量"),
                    ["将领属性"] = new JObject {
                        ["成长点数"] = Select((JObject)g["将领属性"]["成长点数"], "等级"),
                        ["初始属性"] = Select((JObject)g["将领属性"]["初始属性"], "名字") } }));
                return item;
            }));
        var entities = new JObject();
        if (actor != null)
            foreach (var kind in new[] { "generals", "equipment", "fiefs" })
            {
                var own = new JObject();
                if (state.EntityMappings[kind] is JObject all)
                    foreach (var entry in all.Properties())
                        if (entry.Value.Value<string>("playerId") == actor.PlayerId) own[entry.Name] = entry.Value.DeepClone();
                entities[kind] = own;
            }
        privatePlayer["entityMappings"] = entities;
        privatePlayer["chatCities"] = Dwsg.Server.Chat.ChatModule.ReadCities(state, actor);
        if (actor != null)
            privatePlayer["marketQuote"] = Dwsg.Shared.Economy.MarketRules.Quote(state.RequirePlayer(actor.PlayerId));
        if (actor != null && state.EntityMappings["taverns"]?[actor.PlayerId] != null)
            privatePlayer["tavern"] = state.EntityMappings["taverns"][actor.PlayerId].DeepClone();
        var publicMarches = new JArray();
        var ownBattles = new JObject();
        if (state.Data["战斗运行"] is JObject battles)
            foreach (var entry in battles.Properties())
            {
                var battle = (JObject)entry.Value;
                if (!battle.Value<bool>("SettlementApplied"))
                    publicMarches.Add(Select(battle, "BattleId", "X", "Y", "Phase", "ArrivalUtcMs", "PlayerId"));
                if (actor != null && battle.Value<string>("PlayerId") == actor.PlayerId)
                {
                    var own = (JObject)battle.DeepClone();
                    own.Remove("RandomState");
                    ownBattles[entry.Name] = own;
                }
            }
        privatePlayer["战斗运行"] = ownBattles;
        world["军情摘要"] = publicMarches;
        return new WorldSnapshot { WorldId = state.WorldId, PlayerId = actor?.PlayerId, WorldRevision = state.Revision,
            ServerUtcMs = serverUtcMs, PublicWorld = world, PrivatePlayer = privatePlayer };
    }
}
