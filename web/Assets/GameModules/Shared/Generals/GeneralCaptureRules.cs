using System.Linq;
using Newtonsoft.Json.Linq;

namespace Dwsg.Shared.Generals
{
	public static class GeneralCaptureRules
	{
		// 概率与战斗随机由原Combat规则计算，这里只应用原捕将/未捕忠诚变化。
		public static JArray Apply(JArray players, int targetPlayerIndex, int targetGeneralId, int attackerPlayerIndex, int attackerGeneralId, bool captured)
		{
			if (targetGeneralId == 0) throw new GeneralRuleException(GeneralFailure.InvalidArgument, "原临时民兵不参与捕将");
			if (targetPlayerIndex < 0 || targetPlayerIndex >= players.Count || attackerPlayerIndex < 0 || attackerPlayerIndex >= players.Count || targetPlayerIndex == attackerPlayerIndex)
				throw new GeneralRuleException(GeneralFailure.InvalidArgument, "捕将身份无效");
			JArray candidate = (JArray)players.DeepClone();
			JObject targetFief, attackerFief;
			JObject target = LegacyGenerals.General(LegacyGenerals.Object(candidate[targetPlayerIndex]), targetGeneralId, out targetFief);
			JObject attacker = LegacyGenerals.General(LegacyGenerals.Object(candidate[attackerPlayerIndex]), attackerGeneralId, out attackerFief);
			if (LegacyGenerals.Number(attacker["详细信息"]["状态"]) != 1.0) throw new GeneralRuleException(GeneralFailure.Conflict, "捕将军队已离开战斗");
			JObject details = LegacyGenerals.Object(target["详细信息"]);
			bool alreadyCaptured = LegacyGenerals.Number(details["状态"]) == 3.0 && LegacyGenerals.Integer(details["俘虏玩家"]) == attackerPlayerIndex;
			if (!alreadyCaptured && LegacyGenerals.Number(details["状态"]) != 1.0) throw new GeneralRuleException(GeneralFailure.Conflict, "守将不在当前战斗中");
			if (alreadyCaptured && !captured) throw new GeneralRuleException(GeneralFailure.Conflict, "守将已经被俘虏");
			if (captured)
			{
				details["状态"] = 3.0;
				details["俘虏玩家"] = (double)attackerPlayerIndex;
				JArray prisoners = LegacyGenerals.Array(attackerFief["俘虏信息表"]);
				if (!prisoners.OfType<JObject>().Any(p => LegacyGenerals.Integer(p["第几个玩家"]) == targetPlayerIndex && LegacyGenerals.Integer(p["将领ID标识"]) == targetGeneralId))
					prisoners.Add(new JObject { ["第几个玩家"] = targetPlayerIndex, ["将领ID标识"] = targetGeneralId });
			}
			else details["忠诚"] = LegacyGenerals.Number(details["忠诚"]) - 1.0;
			return candidate;
		}
	}
}
