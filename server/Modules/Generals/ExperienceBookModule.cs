using Dwsg.Shared;
using Dwsg.Shared.Generals;
using Newtonsoft.Json.Linq;

namespace Dwsg.Server.Modules.Generals
{
	public sealed partial class GeneralsModule
	{
		static GameResult UseExperienceBook(WorldState working, string playerId, JObject player, JObject payload, long now)
		{
			if (payload.Count != 3 || payload["generalId"] == null || payload["itemName"]?.Type != JTokenType.String || payload["count"] == null)
				throw new GeneralRuleException(GeneralFailure.InvalidArgument, "经验书只接受将领稳定编号、原书名和数量");
			string id = StableId(payload["generalId"]);
			JObject fief;
			JObject general = ResolveGeneral(working, playerId, id, out fief);
			GameResult result;
			JObject updated = GeneralExperienceBookRules.Execute(player, LegacyGenerals.Integer(general["ID"]),
				payload.Value<string>("itemName"), LegacyGenerals.Integer(payload["count"]), working.Data["道具配置"] as JArray, now / 1000, out result);
			((JArray)working.Data["玩家列表"])[working.ResolvePlayerIndex(playerId)] = updated;
			result.Data["generalId"] = id;
			return result;
		}
	}
}
