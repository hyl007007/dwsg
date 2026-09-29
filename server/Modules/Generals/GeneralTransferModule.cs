using System;
using System.Linq;
using Dwsg.Shared;
using Dwsg.Shared.Generals;
using Newtonsoft.Json.Linq;

namespace Dwsg.Server.Modules.Generals
{
	public sealed partial class GeneralsModule
	{
		public static GameResult TransferGenerals(WorldState candidate, string sourcePlayerId, string sourceFiefId, string targetPlayerId, string targetFiefId, string battleId, long? utcSeconds = null)
		{
			try
			{
				StableId(new JValue(battleId));
				if (sourceFiefId == targetFiefId) throw new GeneralRuleException(GeneralFailure.InvalidArgument, "不能转移到同一封地");
				WorldState working = candidate.Clone();
				JObject sourceBinding = RequireOwnedMapping(working, "fiefs", sourceFiefId, sourcePlayerId);
				JObject targetBinding = RequireOwnedMapping(working, "fiefs", targetFiefId, targetPlayerId);
				JObject source = working.RequirePlayer(sourcePlayerId), target = working.RequirePlayer(targetPlayerId);
				JObject sourceFief = LegacyGenerals.Array(source["封地信息表"]).OfType<JObject>().Single(f => LegacyGenerals.Integer(f["ID"]) == LegacyGenerals.Integer(sourceBinding["legacyId"]));
				JObject targetFief = LegacyGenerals.Array(target["封地信息表"]).OfType<JObject>().Single(f => LegacyGenerals.Integer(f["ID"]) == LegacyGenerals.Integer(targetBinding["legacyId"]));
				JArray from = LegacyGenerals.Array(sourceFief["将领信息表"]), moved = new JArray();
				for (int i = from.Count - 1; i >= 0; i--)
				{
					JObject general = LegacyGenerals.Object(from[i]);
					moved.Add(TransferGeneral(working, sourcePlayerId, targetPlayerId, source, target, sourceFief, targetFief, general));
				}
				if (moved.Count > 0 && GeneralAttributeRules.Recalculate(target, utcSeconds ?? DateTimeOffset.UtcNow.ToUnixTimeSeconds()))
					throw new GeneralRuleException(GeneralFailure.InvalidData, "转移后的将领属性异常");
				candidate.Data = working.Data; candidate.EntityMappings = working.EntityMappings;
				return GameResult.Success(new JObject { ["generalIds"] = moved });
			}
			catch (GeneralRuleException failure) { return Rejection(failure); }
			catch (InvalidOperationException) { return GameResult.Reject(GameCodes.NotFound, "转移封地或实例不存在"); }
		}

		static string TransferGeneral(WorldState working, string sourceId, string targetId, JObject source, JObject target,
			JObject sourceFief, JObject targetFief, JObject general)
		{
			int oldId = LegacyGenerals.Integer(general["ID"]), nextId = LegacyGenerals.Integer(target["将领ID标识"]);
			string stableId = FindGeneralId(working, sourceId, oldId);
			if (working.EntityMappings["generalOccupancy"]?[stableId] != null || LegacyGenerals.Number(general["详细信息"]["状态"]) == 1.0)
				throw new GeneralRuleException(GeneralFailure.Conflict, "仍在军队中占用的将领不能转移");
			if (nextId < 0 || nextId == int.MaxValue || LegacyGenerals.Array(target["封地信息表"]).SelectMany(f => LegacyGenerals.Array(f["将领信息表"]).OfType<JObject>()).Any(g => LegacyGenerals.Integer(g["ID"]) == nextId))
				throw new GeneralRuleException(GeneralFailure.InvalidData, "接收角色将领编号无效或重复");
			TransferWornEquipment(working, sourceId, targetId, source, target, oldId, nextId);
			for (int team = 0; team < 5; team++)
				for (int slot = 0; slot < 5; slot++) if (LegacyGenerals.Integer(LegacyGenerals.Formation(source, team)[slot]) == oldId) LegacyGenerals.Formation(source, team)[slot] = -1;
			general["详细信息"]["编队"] = 0.0;
			LegacyGenerals.Array(sourceFief["将领信息表"]).Remove(general);
			general["ID"] = nextId; LegacyGenerals.Array(targetFief["将领信息表"]).Add(general); target["将领ID标识"] = nextId + 1;
			JObject mapping = (JObject)working.EntityMappings["generals"][stableId]; mapping["playerId"] = targetId; mapping["legacyId"] = nextId;
			int sourceIndex = working.ResolvePlayerIndex(sourceId), targetIndex = working.ResolvePlayerIndex(targetId);
			// 原封地俘虏/驻防保存实例编号；城池驻防同名字段保存配置编号。
			foreach (JObject player in LegacyGenerals.Array(working.Data["玩家列表"]))
				foreach (JObject fief in LegacyGenerals.Array(player["封地信息表"]))
					foreach (string field in new[] { "俘虏信息表", "驻防信息表" })
						foreach (JObject reference in LegacyGenerals.Array(fief[field]))
							if (LegacyGenerals.Integer(reference["第几个玩家"]) == sourceIndex && LegacyGenerals.Integer(reference["将领ID标识"]) == oldId)
							{ reference["第几个玩家"] = targetIndex; reference["将领ID标识"] = nextId; }
			return stableId;
		}

		static void TransferWornEquipment(WorldState working, string sourceId, string targetId, JObject source, JObject target, int oldGeneralId, int newGeneralId)
		{
			JObject equipment = Map(working, "equipment");
			for (int slot = 0; slot < 4; slot++)
			{
				JObject worn = LegacyGenerals.WornEquipment(source, slot, oldGeneralId);
				if (worn == null) continue;
				worn["将领ID"] = newGeneralId;
				if (sourceId == targetId) continue;
				JArray original = LegacyGenerals.Equipment(source, slot), received = LegacyGenerals.Equipment(target, slot);
				int oldIndex = original.IndexOf(worn);
				JProperty mapping = equipment.Properties().SingleOrDefault(p => p.Value.Value<string>("playerId") == sourceId && p.Value.Value<int>("slot") == slot && p.Value.Value<int>("legacyIndex") == oldIndex);
				if (mapping == null) throw new GeneralRuleException(GeneralFailure.NotFound, "穿戴装备稳定实例不存在");
				mapping.Value["playerId"] = targetId; mapping.Value["legacyIndex"] = received.Count;
				original.Remove(worn); received.Add(worn);
				foreach (JProperty remaining in equipment.Properties())
					if (remaining.Value.Value<string>("playerId") == sourceId && remaining.Value.Value<int>("slot") == slot && remaining.Value.Value<int>("legacyIndex") > oldIndex)
						remaining.Value["legacyIndex"] = remaining.Value.Value<int>("legacyIndex") - 1;
			}
		}
	}
}
