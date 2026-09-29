using System.Security.Cryptography;
using Dwsg.Shared;
using Dwsg.Shared.Generals;
using Newtonsoft.Json.Linq;

namespace Dwsg.Server.Modules.Generals
{
	public sealed partial class GeneralsModule
	{
		static GameResult Cultivate(WorldState working, string playerId, JObject player, JObject payload, long now)
		{
			string id = StableId(payload["generalId"]);
			JObject fief;
			JObject general = ResolveGeneral(working, playerId, id, out fief);
			GameResult result;
			JObject updated = GeneralCultivationRules.Execute(player, LegacyGenerals.Integer(general["ID"]),
				LegacyGenerals.Integer(payload["count"]), working.Data["道具配置"] as JArray,
				RandomNumberGenerator.GetInt32, now / 1000, out result);
			((JArray)working.Data["玩家列表"])[working.ResolvePlayerIndex(playerId)] = updated;
			result.Data["generalId"] = id;
			return result;
		}
	}
}
