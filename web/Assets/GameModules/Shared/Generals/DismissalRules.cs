using Newtonsoft.Json.Linq;

namespace Dwsg.Shared.Generals
{
	public sealed class GeneralDismissal
	{
		public JArray Players { get; set; }
		public JObject RemovedGeneral { get; set; }
		public bool ReturnedToNature { get; set; }
		public int ReturnedLegacyId { get; set; }
	}

	public static class DismissalRules
	{
		public static GeneralDismissal Execute(JArray players, int playerIndex, int generalId, long utcSeconds)
		{
			if (playerIndex < 0 || playerIndex >= players.Count) throw new GeneralRuleException(GeneralFailure.NotFound, "角色不存在");
			JArray candidate = (JArray)players.DeepClone();
			JObject owner = LegacyGenerals.Object(candidate[playerIndex]);
			JObject fief;
			JObject original = LegacyGenerals.General(owner, generalId, out fief);
			LegacyGenerals.RequireIdle(original, true);
			bool renowned = original["将领属性"]["初始属性"].Value<string>("系列") == "名将";
			if (renowned && (candidate.Count <= 2 || LegacyGenerals.Array(candidate[2]["封地信息表"]).Count == 0 || playerIndex == 2))
				throw new GeneralRuleException(GeneralFailure.NotFound, "名将回归封地不存在");
			JObject updated = GeneralRules.PrepareDismissal(owner, generalId, utcSeconds);
			JObject removed = LegacyGenerals.General(updated, generalId, out fief);
			LegacyGenerals.Array(fief["将领信息表"]).Remove(removed);
			candidate[playerIndex] = updated;
			GeneralDismissal result = new GeneralDismissal { Players = candidate, RemovedGeneral = removed, ReturnedToNature = renowned };
			if (renowned)
			{
				removed["详细信息"]["忠诚"] = 100.0;
				JObject nature = LegacyGenerals.Object(candidate[2]);
				int nextId = LegacyGenerals.Integer(nature["将领ID标识"]);
				candidate[2] = TavernRules.AddGeneral(nature, LegacyGenerals.Integer(nature["封地信息表"][0]["ID"]), removed, utcSeconds, false);
				result.ReturnedLegacyId = nextId;
				result.RemovedGeneral = LegacyGenerals.General(LegacyGenerals.Object(candidate[2]), nextId, out fief);
			}
			return result;
		}
	}
}
