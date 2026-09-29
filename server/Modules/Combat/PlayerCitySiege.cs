using System;
using System.Linq;
using Dwsg.Shared;
using Dwsg.Shared.Combat;
using Newtonsoft.Json.Linq;

namespace Dwsg.Server.Modules.Combat
{
    public sealed partial class CombatModule
    {
        // 原城池身份按国家判断敌我；城主/国王使用服务器已有稳定映射。
        // 出征与到达共用此检查，到达时不信任出征时保存的owner。
        private static GameResult ValidateCitySiegeTarget(WorldState world, string attackerId, JObject city)
        {
            GameResult ownership = ValidateCitySiegeOwnership(world, city, out _);
            if (ownership.Code != GameCodes.Ok) return ownership;
            string owner = ResolveCityPlayer(world, city.Value<int>("城主"));
            if (owner == attackerId) return GameResult.Reject(GameCodes.Conflict, "不能进攻自己的城池");
            if (city.Value<string>("国家") == world.RequirePlayer(attackerId)["基础信息"].Value<string>("国家"))
                return GameResult.Reject(GameCodes.Conflict, "不能进攻本国城池");
            return GameResult.Success();
        }

        private static GameResult ValidateCitySiegeOwnership(WorldState world, JObject city, out string humanDefenderId)
        {
            humanDefenderId = null;
            if (city == null) return GameResult.Reject(GameCodes.NotFound, "城池不存在");
            try
            {
                if (city["城主"]?.Type != JTokenType.Integer || city["国家"]?.Type != JTokenType.String)
                    throw new InvalidOperationException("城池归属字段无效");
                string owner = ResolveCityPlayer(world, city.Value<int>("城主"));
                if (IsHumanCityPlayer(world, owner)) humanDefenderId = owner;
                string nationName = city.Value<string>("国家");
                if (!string.IsNullOrEmpty(nationName))
                {
                    JObject nation = (world.Data["国家列表"] as JArray)?.OfType<JObject>()
                        .SingleOrDefault(item => item.Value<string>("国号") == nationName);
                    if (nation?["国王"]?.Type != JTokenType.Integer || nation.Value<int>("国王") < 0)
                        throw new InvalidOperationException("所属国家或国王无效");
                    string king = ResolveCityPlayer(world, nation.Value<int>("国王"));
                    if (humanDefenderId == null && IsHumanCityPlayer(world, king)) humanDefenderId = king;
                }
                return GameResult.Success();
            }
            catch (InvalidOperationException) { return GameResult.Reject(GameCodes.Unavailable, "城池归属映射失效，操作未提交"); }
            catch (OverflowException) { return GameResult.Reject(GameCodes.Unavailable, "城池归属索引无效，操作未提交"); }
        }

        private static string ResolveCityPlayer(WorldState world, int index)
        {
            if (index == -1) return null; // 原无主城。
            JProperty mapping = (world.EntityMappings["players"] as JObject)?.Properties()
                .SingleOrDefault(item => item.Value.Type == JTokenType.Integer && item.Value.Value<int>() == index);
            if (mapping == null || world.ResolvePlayerIndex(mapping.Name) != index
                || (!OriginalNpc(world, index) && !IsHumanCityPlayer(world, mapping.Name)))
                throw new InvalidOperationException("城池引用了未绑定角色");
            return mapping.Name;
        }

        private static bool IsHumanCityPlayer(WorldState world, string playerId)
        {
            return playerId != null && world.EntityMappings["humanPlayers"]?[playerId]?.Type == JTokenType.Boolean
                && world.EntityMappings["humanPlayers"][playerId].Value<bool>();
        }

        private static string CityHumanDefender(WorldState world, JObject city)
        {
            GameResult result = ValidateCitySiegeOwnership(world, city, out string defender);
            if (result.Code != GameCodes.Ok) throw new InvalidOperationException(result.Message);
            return defender;
        }

        private static bool CanUseCityGuard(WorldState world, BanditBattle battle, string ownerId, JObject general)
        {
            if (ownerId == battle.PlayerId || general["详细信息"].Value<double>("状态") != 0) return false;
            int index = world.ResolvePlayerIndex(ownerId);
            if (!OriginalNpc(world, index) && (!IsHumanCityPlayer(world, ownerId)
                || world.RequirePlayer(ownerId)["基础信息"].Value<string>("国家") != City(world, battle.X, battle.Y).Value<string>("国家"))) return false;
            string id = ((JObject)world.EntityMappings["generals"]).Properties().Single(entry => entry.Value.Value<string>("playerId") == ownerId
                && entry.Value.Value<int>("legacyId") == general.Value<int>("ID")).Name;
            return world.EntityMappings["generalOccupancy"]?[id] == null;
        }

        private static JObject CityMilitiaPlayer(WorldState world, string playerId)
        {
            JObject player = world.RequirePlayer(playerId);
            // 原临时守军借国王数据计算，并临时覆盖统帅后复位为8。
            // 所有正式角色只在原数据副本上复用该规则，科技/将领属性不被临时兵生成改写。
            return (JObject)player.DeepClone();
        }
    }
}
