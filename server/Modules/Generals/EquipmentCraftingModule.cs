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
	}
}
