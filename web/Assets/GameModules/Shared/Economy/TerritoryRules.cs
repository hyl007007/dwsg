using System;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Dwsg.Shared.Economy
{
    public static class TerritoryRules
    {
        public static JObject City(WorldState state, int x, int y)
        {
            return (state.Data["城池列表"] as JArray)?.OfType<JObject>()
                .FirstOrDefault(city => city.Value<int>("坐标x") == x && city.Value<int>("坐标y") == y);
        }

        public static JObject Nation(WorldState state, string name)
        {
            return (state.Data["国家列表"] as JArray)?.OfType<JObject>().FirstOrDefault(nation => nation.Value<string>("国号") == name);
        }

        public static GameResult CreateFief(WorldState state, string playerId, int cityX, int cityY)
        {
            var city = City(state, cityX, cityY);
            if (city == null) return GameResult.Reject(GameCodes.NotFound, "城池不存在");
            var player = state.RequirePlayer(playerId);
            int playerIndex = state.ResolvePlayerIndex(playerId);
            var fiefs = player["封地信息表"] as JArray;
            var residents = city["城池封地列表"] as JArray;
            var template = state.Data["初始封地模板"] as JObject;
            var capacity = (state.Data["城池容量配置"] as JArray)?.OfType<JObject>()
                .FirstOrDefault(row => JToken.DeepEquals(row["规模"], city["规模"]));
            double maximum;
            if (fiefs == null || residents == null || template == null || capacity == null || !ShopRules.TryNumber(capacity["容量"], out maximum))
                return GameResult.Reject(GameCodes.Unavailable, "原封地配置不完整");
            // The original allied-city button permits construction while Count <= 10.
            if (city.Value<string>("国家") != player["基础信息"].Value<string>("国家"))
                return GameResult.Reject(GameCodes.Forbidden, "只能在本国城池开辟封地");
            if (fiefs.Count > 10) return GameResult.Reject(GameCodes.Conflict, "开辟封地失败!");
            if (residents.OfType<JObject>().Any(entry => entry.Value<int>("第几个玩家") == playerIndex) ||
                fiefs.OfType<JObject>().Any(fief => fief["所在城池"] is JObject && fief["所在城池"].Value<int>("x") == cityX && fief["所在城池"].Value<int>("y") == cityY))
                return GameResult.Reject(GameCodes.Conflict, "本城已有你的封地");
            if (residents.Count >= maximum) return GameResult.Reject(GameCodes.WorldFull, "城池封地已满");
            int id = player.Value<int>("封地ID标识");
            if (id < 1 || id == int.MaxValue || fiefs.OfType<JObject>().Any(fief => fief.Value<int>("ID") == id))
                return GameResult.Reject(GameCodes.Unavailable, "原封地编号无效");
            var created = (JObject)template.DeepClone();
            created["ID"] = id;
            created["所在城池"] = new JObject { ["x"] = cityX, ["y"] = cityY };
            created["封地名字"] = id == 1 ? player["基础信息"].Value<string>("名字") + "基地" : city.Value<string>("名称") + "封地";
            fiefs.Add(created);
            residents.Add(new JObject { ["第几个玩家"] = playerIndex, ["封地ID标识"] = id });
            player["封地ID标识"] = id + 1;
            var result = GameResult.Success(new JObject { ["legacyFiefId"] = id });
            result.Message = "开辟封地成功!";
            return result;
        }

        public static GameResult MoveFief(WorldState state, string playerId, string stableFiefId, int cityX, int cityY)
        {
            var mapping = state.EntityMappings["fiefs"]?[stableFiefId] as JObject;
            if (mapping == null || mapping.Value<string>("playerId") != playerId)
                return GameResult.Reject(GameCodes.Forbidden, "封地不属于此角色");
            return MoveOriginalFief(state, state.ResolvePlayerIndex(playerId), mapping.Value<int>("legacyId"), cityX, cityY);
        }

        // Trusted capture/capital migration also relocates original residents without an account binding.
        public static GameResult MoveOriginalFief(WorldState state, int playerIndex, int legacyFiefId, int cityX, int cityY)
        {
            var players = state.Data["玩家列表"] as JArray;
            var target = City(state, cityX, cityY);
            if (players == null || playerIndex < 0 || playerIndex >= players.Count || target == null)
                return GameResult.Reject(GameCodes.NotFound, "原角色或目标城池不存在");
            var player = players[playerIndex] as JObject;
            var fief = (player?["封地信息表"] as JArray)?.OfType<JObject>().FirstOrDefault(item => item.Value<int>("ID") == legacyFiefId);
            var location = fief?["所在城池"] as JObject;
            var source = location == null ? null : City(state, location.Value<int>("x"), location.Value<int>("y"));
            var previous = source?["城池封地列表"] as JArray;
            var next = target["城池封地列表"] as JArray;
            if (fief == null || previous == null || next == null) return GameResult.Reject(GameCodes.Unavailable, "原封地位置数据不完整");
            var registered = previous.OfType<JObject>().Where(item => item.Value<int>("第几个玩家") == playerIndex && item.Value<int>("封地ID标识") == legacyFiefId).ToArray();
            if (registered.Length != 1) return GameResult.Reject(GameCodes.Conflict, "原封地城池登记不一致");
            if (ReferenceEquals(source, target)) return GameResult.Success();
            if (next.OfType<JObject>().Any(item => item.Value<int>("第几个玩家") == playerIndex && item.Value<int>("封地ID标识") == legacyFiefId))
                return GameResult.Reject(GameCodes.Conflict, "目标城池已有此封地登记");
            registered[0].Remove();
            next.Add(registered[0]);
            // The old method moved registrations but forgot this authoritative location.
            fief["所在城池"] = new JObject { ["x"] = cityX, ["y"] = cityY };
            return GameResult.Success();
        }
    }
}
