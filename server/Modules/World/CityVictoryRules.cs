using System;
using System.Linq;
using Dwsg.Server.Modules.Generals;
using Dwsg.Shared;
using Dwsg.Shared.Economy;
using Newtonsoft.Json.Linq;

namespace Dwsg.Server.World
{
    internal static class CityVictoryRules
    {
        public static GameResult Apply(WorldState candidate, string attackerId, int x, int y, string battleId, Func<int, int, int> random)
        {
            if (string.IsNullOrWhiteSpace(attackerId) || string.IsNullOrWhiteSpace(battleId) || battleId.Length > 128 || random == null)
                return GameResult.Reject(GameCodes.InvalidArgument, "服务器城池胜利参数无效");
            var previous = candidate.Clone();
            GameResult result;
            try { result = ApplyOriginal(candidate, attackerId, x, y, battleId, random); }
            catch (InvalidOperationException) { result = GameResult.Reject(GameCodes.Unavailable, "原城池、国家或实体映射数据不完整"); }
            catch (InvalidCastException) { result = GameResult.Reject(GameCodes.Unavailable, "原城池数值数据无效"); }
            catch (OverflowException) { result = GameResult.Reject(GameCodes.Unavailable, "原城池数值超出范围"); }
            catch (ArgumentException) { result = GameResult.Reject(GameCodes.Unavailable, "原城池索引数据无效"); }
            if (result.Code != GameCodes.Ok)
            {
                candidate.Data = previous.Data;
                candidate.EntityMappings = previous.EntityMappings;
            }
            return result;
        }

        private static GameResult ApplyOriginal(WorldState state, string attackerId, int x, int y, string battleId, Func<int, int, int> random)
        {
            var ledger = state.Data["城池占领结算"] as JObject;
            var saved = ledger?[battleId] as JObject;
            if (saved != null)
                return saved.Value<string>("attackerPlayerId") == attackerId && saved.Value<int>("cityX") == x && saved.Value<int>("cityY") == y
                    ? GameResult.Success((JObject)saved.DeepClone()) : GameResult.Reject(GameCodes.Conflict, "此战斗已结算另一城池或角色");
            var city = TerritoryRules.City(state, x, y);
            if (city == null) return GameResult.Reject(GameCodes.NotFound, "城池不存在");
            var attacker = state.RequirePlayer(attackerId);
            string nationName = attacker["基础信息"].Value<string>("国家");
            if (TerritoryRules.Nation(state, nationName) == null)
                return GameResult.Reject(GameCodes.Conflict, "角色所属国家不存在");
            string previousNation = city.Value<string>("国家");
            bool capital = city.Value<int>("规模") == 4;
            city["城主"] = state.ResolvePlayerIndex(attackerId);
            city["国家"] = nationName;
            var data = new JObject { ["attackerPlayerId"] = attackerId, ["cityX"] = x, ["cityY"] = y,
                ["previousNation"] = previousNation, ["nation"] = nationName, ["movedFiefCount"] = 0 };
            if (!capital)
            {
                // Removal changes the original list; enumerate the entire original resident snapshot.
                var residents = ((JArray)city["城池封地列表"]).OfType<JObject>().ToArray();
                foreach (var resident in residents)
                {
                    int ownerIndex = resident.Value<int>("第几个玩家");
                    var owner = (JObject)state.Data["玩家列表"][ownerIndex];
                    var nation = TerritoryRules.Nation(state, owner["基础信息"].Value<string>("国家"));
                    // The original random relocation does nothing for members of an extinct nation.
                    if (nation == null) continue;
                    var towns = RefreshNationCities(state, nation);
                    if (towns.Length == 0) return GameResult.Reject(GameCodes.Unavailable, "原所属国家没有可迁移城池");
                    var target = towns[Draw(random, 0, towns.Length)];
                    var moved = TerritoryRules.MoveOriginalFief(state, ownerIndex, resident.Value<int>("封地ID标识"), target.Value<int>("坐标x"), target.Value<int>("坐标y"));
                    if (moved.Code != GameCodes.Ok) return moved;
                    if (!ReferenceEquals(city, target)) data["movedFiefCount"] = data.Value<int>("movedFiefCount") + 1;
                }
            }
            else
            {
                SetCapitalScale(city, false, random);
                var formerNation = TerritoryRules.Nation(state, previousNation);
                if (formerNation != null)
                {
                    var towns = RefreshNationCities(state, formerNation);
                    if (towns.Length > 0)
                    {
                        var oldCapital = TerritoryRules.City(state, formerNation.Value<int>("国都x"), formerNation.Value<int>("国都y"));
                        var target = towns[Draw(random, 0, towns.Length)];
                        int kingIndex = formerNation.Value<int>("国王");
                        var king = (JObject)state.Data["玩家列表"][kingIndex];
                        target["城主"] = kingIndex; target["国家"] = king["基础信息"].Value<string>("国家");
                        SetCapitalScale(target, true, random);
                        formerNation["国都x"] = target.Value<int>("坐标x"); formerNation["国都y"] = target.Value<int>("坐标y");
                        foreach (var resident in ((JArray)oldCapital["城池封地列表"]).OfType<JObject>().ToArray())
                        {
                            var moved = TerritoryRules.MoveOriginalFief(state, resident.Value<int>("第几个玩家"), resident.Value<int>("封地ID标识"), target.Value<int>("坐标x"), target.Value<int>("坐标y"));
                            if (moved.Code != GameCodes.Ok) return moved;
                            data["movedFiefCount"] = data.Value<int>("movedFiefCount") + 1;
                        }
                        data["capitalX"] = formerNation["国都x"].DeepClone(); data["capitalY"] = formerNation["国都y"].DeepClone();
                    }
                    else
                    {
                        int kingIndex = formerNation.Value<int>("国王");
                        string kingId = PlayerId(state, kingIndex), neutralId = PlayerId(state, 2);
                        string kingFiefId = FirstFiefId(state, kingId), neutralFiefId = FirstFiefId(state, neutralId);
                        var kingGenerals = (JArray)state.RequirePlayer(kingId)["封地信息表"][0]["将领信息表"];
                        var neutralGenerals = (JArray)state.RequirePlayer(neutralId)["封地信息表"][0]["将领信息表"];
                        var allCities = ((JArray)state.Data["城池列表"]).OfType<JObject>().ToArray();
                        // Draw before TransferGenerals replaces the candidate document. A battle-bound
                        // random callback therefore keeps all of its persisted RandomState advances.
                        var destinations = Enumerable.Range(0, neutralGenerals.Count + kingGenerals.Count)
                            .Select(_ => Draw(random, 0, allCities.Length)).ToArray();
                        formerNation.Remove();
                        var transferred = GeneralsModule.TransferGenerals(state, kingId, kingFiefId, neutralId, neutralFiefId, battleId);
                        if (transferred.Code != GameCodes.Ok) return transferred;
                        var seal = GrantOriginalSeal(state.RequirePlayer(attackerId), state);
                        if (seal.Code != GameCodes.Ok) return seal;
                        allCities = ((JArray)state.Data["城池列表"]).OfType<JObject>().ToArray();
                        neutralGenerals = (JArray)state.RequirePlayer(neutralId)["封地信息表"][0]["将领信息表"];
                        // Original task dispatch reads these as configuration ID -> name -> real general.
                        for (int i = 0; i < neutralGenerals.Count; i++)
                            ((JArray)allCities[destinations[i]]["城池驻防列表"]).Add(new JObject
                                { ["第几个玩家"] = 2, ["将领ID标识"] = neutralGenerals[i]["将领属性"]["初始属性"].Value<int>("ID") });
                        data["destroyedNation"] = previousNation;
                    }
                }
            }
            // Refresh the existing original country city caches after ownership and capital changes.
            foreach (var nation in ((JArray)state.Data["国家列表"]).OfType<JObject>()) RefreshNationCities(state, nation);
            ledger = state.Data["城池占领结算"] as JObject;
            if (ledger == null) state.Data["城池占领结算"] = ledger = new JObject();
            ledger[battleId] = data.DeepClone();
            return GameResult.Success(data);
        }

        private static JObject[] RefreshNationCities(WorldState state, JObject nation)
        {
            var cities = ((JArray)state.Data["城池列表"]).OfType<JObject>().Where(city => city.Value<string>("国家") == nation.Value<string>("国号")).ToArray();
            nation["城池列表"] = new JArray(cities.Select(city => new JObject { ["x"] = city["坐标x"].DeepClone(), ["y"] = city["坐标y"].DeepClone() }));
            return cities;
        }

        private static int Draw(Func<int, int, int> random, int minimum, int maximum)
        {
            if (maximum <= minimum) throw new InvalidOperationException("Original random range is empty.");
            int value = random(minimum, maximum);
            if (value < minimum || value >= maximum) throw new InvalidOperationException("Server random value is out of range.");
            return value;
        }

        private static void SetCapitalScale(JObject city, bool capital, Func<int, int, int> random)
        {
            city["规模"] = capital ? 4 : 0; city["城墙"] = capital ? 2000000.0 : 200000.0; city["战功"] = capital ? 10000 : 500;
            city["协防数量f"] = capital ? 50.0 : 1.0; city["协防数量m"] = capital ? 100.0 : 7.0;
            city["协防几率"] = capital ? 100 : Draw(random, 10, 20);
            city["城主征收_铜"] = city["国家征收_铜"] = capital ? 0.0 : 20000.0;
            city["城主征收_粮"] = city["国家征收_粮"] = capital ? 0.0 : 40000.0;
        }

        private static string PlayerId(WorldState state, int index)
        {
            return ((JObject)state.EntityMappings["players"]).Properties().Single(p => p.Value.Value<int>() == index).Name;
        }

        private static string FirstFiefId(WorldState state, string playerId)
        {
            int id = state.RequirePlayer(playerId)["封地信息表"][0].Value<int>("ID");
            return ((JObject)state.EntityMappings["fiefs"]).Properties().Single(p => p.Value.Value<string>("playerId") == playerId && p.Value.Value<int>("legacyId") == id).Name;
        }

        private static GameResult GrantOriginalSeal(JObject player, WorldState state)
        {
            var definition = (state.Data["道具配置"] as JArray)?.OfType<JObject>().SingleOrDefault(item => item.Value<string>("名字") == "玉玺");
            var items = player["背包道具列表"]?[definition?.Value<string>("分类") + "道具列表"] as JArray;
            if (definition == null || items == null || items.Any(item => !(item is JObject) || item["名字"]?.Type != JTokenType.String ||
                !ShopRules.TryNumber(item["数量"], out double quantity) || quantity < 0 || quantity != Math.Truncate(quantity)))
                return GameResult.Reject(GameCodes.Unavailable, "原玉玺配置或背包数据无效");
            // Original 添加道具 has no charge and does not reject a full bag for a battle award.
            ItemStackRules.Add<JToken>(items, "玉玺", 1, item => item.Value<string>("名字"), item => item.Value<double>("数量"),
                (item, count) => item["数量"] = count, (name, count) => new JObject { ["名字"] = name, ["ID"] = 0, ["数量"] = count });
            return GameResult.Success();
        }
    }
}
