using System.Collections.Generic;
using System.Linq;
using Dwsg.Shared;
using Dwsg.Shared.Generals;
using Newtonsoft.Json.Linq;

namespace Dwsg.Server.Modules.Generals
{
	public sealed class CityDefenderReference
	{
		public string PlayerId { get; set; }
		public string GeneralId { get; set; }
	}

	public sealed partial class GeneralsModule
	{
		public static GameResult ReserveCityDefenders(WorldState candidate, string battleId, IEnumerable<CityDefenderReference> guards, out List<JObject> units)
		{
			units = null;
			try
			{
				StableId(new JValue(battleId));
				CityDefenderReference[] entries = guards?.ToArray();
				if (entries == null || entries.Any(g => g == null) || entries.Select(g => g.GeneralId).Distinct().Count() != entries.Length)
					throw new GeneralRuleException(GeneralFailure.InvalidArgument, "守将预留列表无效");
				WorldState working = candidate.Clone();
				JObject occupancy = Map(working, "generalOccupancy");
				List<JObject> selected = new List<JObject>();
				foreach (CityDefenderReference entry in entries)
				{
					JObject fief;
					JObject general = ResolveGeneral(working, entry.PlayerId, StableId(new JValue(entry.GeneralId)), out fief);
					RequireAvailable(working, entry.GeneralId, general);
					general["详细信息"]["状态"] = 1.0;
					occupancy[entry.GeneralId] = new JObject { ["playerId"] = entry.PlayerId, ["armyId"] = battleId };
					selected.Add(general);
				}
				candidate.Data = working.Data; candidate.EntityMappings = working.EntityMappings; units = selected;
				return GameResult.Success();
			}
			catch (GeneralRuleException failure) { return Rejection(failure); }
		}

		public static GameResult CaptureGeneral(WorldState candidate, string targetOwnerId, string targetGeneralId, string attackerPlayerId, string attackerGeneralId, string battleId, bool captured)
		{
			try
			{
				WorldState working = candidate.Clone();
				JObject targetFief, attackerFief;
				JObject target = ResolveGeneral(working, targetOwnerId, targetGeneralId, out targetFief);
				JObject attacker = ResolveGeneral(working, attackerPlayerId, attackerGeneralId, out attackerFief);
				JObject targetBinding = working.EntityMappings["generalOccupancy"]?[targetGeneralId] as JObject;
				JObject attackerBinding = working.EntityMappings["generalOccupancy"]?[attackerGeneralId] as JObject;
				if (targetBinding == null || targetBinding.Value<string>("playerId") != targetOwnerId || targetBinding.Value<string>("armyId") != battleId || attackerBinding?.Value<string>("playerId") != attackerPlayerId)
					throw new GeneralRuleException(GeneralFailure.Conflict, "捕将与当前军队占用不匹配");
				working.Data["玩家列表"] = GeneralCaptureRules.Apply(LegacyGenerals.Array(working.Data["玩家列表"]), working.ResolvePlayerIndex(targetOwnerId), LegacyGenerals.Integer(target["ID"]), working.ResolvePlayerIndex(attackerPlayerId), LegacyGenerals.Integer(attacker["ID"]), captured);
				candidate.Data = working.Data; candidate.EntityMappings = working.EntityMappings;
				return GameResult.Success(new JObject { ["generalId"] = targetGeneralId, ["captured"] = captured });
			}
			catch (GeneralRuleException failure) { return Rejection(failure); }
		}

		public static GameResult CaptureGeneral(WorldState candidate, string targetOwnerId, string targetGeneralId, string killerPlayerId, string killerGeneralId, string targetArmyId, string killerArmyId, bool captured)
		{
			try
			{
				StableId(new JValue(targetArmyId)); StableId(new JValue(killerArmyId));
				StableId(new JValue(targetGeneralId)); StableId(new JValue(killerGeneralId));
				JObject targetBinding = candidate.EntityMappings["generalOccupancy"]?[targetGeneralId] as JObject;
				JObject killerBinding = candidate.EntityMappings["generalOccupancy"]?[killerGeneralId] as JObject;
				if (targetBinding == null || killerBinding == null
					|| targetBinding.Value<string>("playerId") != targetOwnerId || targetBinding.Value<string>("armyId") != targetArmyId
					|| killerBinding.Value<string>("playerId") != killerPlayerId || killerBinding.Value<string>("armyId") != killerArmyId)
					throw new GeneralRuleException(GeneralFailure.Conflict, "捕将双方与当前军队占用不匹配");
				return CaptureGeneral(candidate, targetOwnerId, targetGeneralId, killerPlayerId, killerGeneralId, targetArmyId, captured);
			}
			catch (GeneralRuleException failure) { return Rejection(failure); }
		}

		public static GameResult ApplyCityDefenderOutcome(WorldState candidate, string targetOwnerId, string battleId, IEnumerable<GeneralOutcome> outcomes, bool completeArmy = true)
		{
			return ApplyOutcomes(candidate, targetOwnerId, battleId, outcomes, completeArmy, true);
		}
	}
}
