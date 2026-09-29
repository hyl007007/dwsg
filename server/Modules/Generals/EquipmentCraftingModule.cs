using System.Security.Cryptography;
using Dwsg.Shared;
using Dwsg.Shared.Generals;
using Newtonsoft.Json.Linq;

namespace Dwsg.Server.Modules.Generals
{
	public sealed partial class GeneralsModule
	{
		static GameResult EnhanceEquipment(WorldState working, string playerId, JObject player, JObject payload, long now)
		{
			string id = StableId(payload["equipmentId"]);
			JObject mapping = RequireOwnedMapping(working, "equipment", id, playerId);
			GameResult result;
			JObject updated = EquipmentEnhancementRules.Execute(player, LegacyGenerals.Integer(mapping["slot"]), LegacyGenerals.Integer(mapping["legacyIndex"]),
				LegacyGenerals.Integer(payload["count"]), working.Data["道具配置"] as JArray, RandomNumberGenerator.GetInt32, now / 1000, out result);
			((JArray)working.Data["玩家列表"])[working.ResolvePlayerIndex(playerId)] = updated;
			result.Data["equipmentId"] = id;
			return result;
		}

		static float SoulRandomFloat(float min, float max)
		{
			// 原 Unity float Range 包含两个端点，整数 Range 则不包含上界。
			return min + (max - min) * (RandomNumberGenerator.GetInt32(0, 16777217) / 16777216f);
		}

		static GameResult RefineEquipment(WorldState working, string playerId, JObject player, JObject payload, string type, long now)
		{
			string id = StableId(payload["equipmentId"]);
			JObject mapping = RequireOwnedMapping(working, "equipment", id, playerId);
			int slot = LegacyGenerals.Integer(mapping["slot"]), index = LegacyGenerals.Integer(mapping["legacyIndex"]);
			JArray locks = payload["lockedIndices"] as JArray;
			GameResult result;
			JObject updated;
			if (type == "generals.setSoulLocks")
			{
				updated = EquipmentSoulRules.Lock(player, slot, index, locks);
				result = GameResult.Success();
			}
			else updated = EquipmentSoulRules.Execute(player, slot, index, LegacyGenerals.Integer(payload["mode"]), LegacyGenerals.Integer(payload["count"]),
				locks, working.Data["道具配置"] as JArray, RandomNumberGenerator.GetInt32, SoulRandomFloat, now / 1000, out result);
			((JArray)working.Data["玩家列表"])[working.ResolvePlayerIndex(playerId)] = updated;
			result.Data["equipmentId"] = id;
			return result;
		}
	}
}
