using Dwsg.Shared;
using Dwsg.Shared.Generals;
using Newtonsoft.Json.Linq;

namespace Dwsg.Server.Modules.Generals
{
	public sealed partial class GeneralsModule
	{
		static GameResult HealWounded(WorldState working, string playerId, JObject player, JObject payload)
		{
			string fiefId = StableId(payload["fiefId"]);
			JObject fief = RequireOwnedMapping(working, "fiefs", fiefId, playerId);
			int troopTypeId = LegacyGenerals.Integer(payload["troopTypeId"]), count = LegacyGenerals.Integer(payload["count"]);
			TroopTreatment change = TroopTreatmentRules.Execute(player, LegacyGenerals.Integer(fief["legacyId"]), troopTypeId, count, working.Data["兵种配置"] as JArray);
			((JArray)working.Data["玩家列表"])[working.ResolvePlayerIndex(playerId)] = change.Player;
			return GameResult.Success(new JObject { ["fiefId"] = fiefId, ["troopTypeId"] = troopTypeId, ["count"] = count,
				["copperCost"] = change.CopperCost, ["foodCost"] = change.FoodCost });
		}
	}
}
