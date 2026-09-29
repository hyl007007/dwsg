using System;
using System.Linq;
using System.Security.Cryptography;
using Dwsg.Shared;
using Dwsg.Shared.Generals;
using Newtonsoft.Json.Linq;

namespace Dwsg.Server.Modules.Generals
{
	public sealed partial class GeneralsModule
	{
		static GameResult ManageCaptive(WorldState working, string playerId, JObject player, JObject payload, string type, long now)
		{
			if (payload.Count != 2 || payload["fiefId"] == null || payload["captiveGeneralId"] == null)
				throw new GeneralRuleException(GeneralFailure.InvalidArgument, "俘虏操作只接受封地和将领稳定编号");
			string fiefId = StableId(payload["fiefId"]), id = StableId(payload["captiveGeneralId"]), sourceId;
			JObject holdingFief, sourceFief, reference;
			JObject general = ResolveCaptive(working, playerId, fiefId, id, out sourceId, out holdingFief, out sourceFief, out reference);
			if (working.EntityMappings["generalOccupancy"]?[id] != null)
				throw new GeneralRuleException(GeneralFailure.Conflict, "被俘将领的战斗尚未结算");
			GameResult result;
			if (type == "generals.releaseCaptive")
			{
				general["详细信息"]["状态"] = 0.0;
				result = GameResult.Success(new JObject { ["outcome"] = "released", ["loyalty"] = general["详细信息"]["忠诚"].DeepClone() });
				result.Message = "释放成功!";
			}
			else result = GeneralPersuasionRules.Execute(player, general, working.ResolvePlayerIndex(playerId), RandomNumberGenerator.GetInt32);
			if (result.Data.Value<string>("outcome") != "retained") reference.Remove();
			if (result.Data.Value<string>("outcome") == "recruited")
			{
				TransferGeneral(working, sourceId, playerId, working.RequirePlayer(sourceId), player, sourceFief, holdingFief, general);
				GeneralPersuasionRules.RefreshRecruitedAttributes(player, LegacyGenerals.Integer(general["ID"]), now / 1000);
			}
			result.Data["captiveGeneralId"] = id; result.Data["fiefId"] = fiefId;
			return result;
		}

		static JObject ResolveCaptive(WorldState state, string playerId, string fiefId, string generalId,
			out string sourceId, out JObject holdingFief, out JObject sourceFief, out JObject reference)
		{
			JObject player = state.RequirePlayer(playerId), fiefBinding = RequireOwnedMapping(state, "fiefs", fiefId, playerId);
			holdingFief = LegacyGenerals.Array(player["封地信息表"]).OfType<JObject>().SingleOrDefault(f => LegacyGenerals.Integer(f["ID"]) == LegacyGenerals.Integer(fiefBinding["legacyId"]));
			if (holdingFief == null) throw new GeneralRuleException(GeneralFailure.NotFound, "收押封地不存在");
			JObject mapping = state.EntityMappings["generals"]?[generalId] as JObject;
			if (mapping == null) throw new GeneralRuleException(GeneralFailure.NotFound, "俘虏稳定实例不存在");
			sourceId = StableId(mapping["playerId"]);
			if (sourceId == playerId) throw new GeneralRuleException(GeneralFailure.Conflict, "不能操作自己的将领作为俘虏");
			int sourceIndex = state.ResolvePlayerIndex(sourceId), legacyId = LegacyGenerals.Integer(mapping["legacyId"]);
			reference = LegacyGenerals.Array(holdingFief["俘虏信息表"]).OfType<JObject>().SingleOrDefault(p => LegacyGenerals.Integer(p["第几个玩家"]) == sourceIndex && LegacyGenerals.Integer(p["将领ID标识"]) == legacyId);
			if (reference == null) throw new GeneralRuleException(GeneralFailure.NotFound, "俘虏不存在或不在自己的收押封地");
			int references = LegacyGenerals.Array(player["封地信息表"]).Sum(f => LegacyGenerals.Array(f["俘虏信息表"]).OfType<JObject>().Count(p => LegacyGenerals.Integer(p["第几个玩家"]) == sourceIndex && LegacyGenerals.Integer(p["将领ID标识"]) == legacyId));
			if (references != 1) throw new GeneralRuleException(GeneralFailure.InvalidData, "俘虏收押引用重复");
			JObject general = ResolveGeneral(state, sourceId, generalId, out sourceFief), details = LegacyGenerals.Object(general["详细信息"]);
			if (LegacyGenerals.Number(details["状态"]) != 3.0 || LegacyGenerals.Integer(details["俘虏玩家"]) != state.ResolvePlayerIndex(playerId))
				throw new GeneralRuleException(GeneralFailure.Conflict, "将领已不在当前俘虏列表");
			return general;
		}

		public static JArray ReadCaptives(WorldState state, string playerId)
		{
			JArray visible = new JArray();
			if (string.IsNullOrWhiteSpace(playerId)) return visible;
			JObject player = state.RequirePlayer(playerId);
			foreach (JObject fief in LegacyGenerals.Array(player["封地信息表"]))
				foreach (JObject reference in LegacyGenerals.Array(fief["俘虏信息表"]).OfType<JObject>())
					try
					{
						JProperty fiefBinding = (state.EntityMappings["fiefs"] as JObject)?.Properties().SingleOrDefault(p => p.Value.Value<string>("playerId") == playerId && p.Value.Value<int>("legacyId") == LegacyGenerals.Integer(fief["ID"]));
						JProperty source = (state.EntityMappings["players"] as JObject)?.Properties().SingleOrDefault(p => LegacyGenerals.Integer(p.Value) == LegacyGenerals.Integer(reference["第几个玩家"]));
						if (fiefBinding == null || source == null) continue;
						string id = FindGeneralId(state, source.Name, LegacyGenerals.Integer(reference["将领ID标识"])), ignored;
						JObject holding, sourceFief, capturedReference;
						JObject general = ResolveCaptive(state, playerId, fiefBinding.Name, id, out ignored, out holding, out sourceFief, out capturedReference);
						JObject initial = LegacyGenerals.Object(general["将领属性"]["初始属性"]);
						visible.Add(new JObject { ["generalId"] = id, ["fiefId"] = fiefBinding.Name,
							["configId"] = LegacyGenerals.Number(initial["ID"]), ["name"] = initial.Value<string>("名字"),
							["level"] = LegacyGenerals.Number(general["将领属性"]["成长点数"]["等级"]), ["profession"] = LegacyGenerals.Integer(initial["职业"]),
							["growth"] = LegacyGenerals.Number(initial["成长"]), ["loyalty"] = LegacyGenerals.Number(general["详细信息"]["忠诚"]) });
					}
					catch (GeneralRuleException) { }
					catch (InvalidOperationException) { }
			return visible;
		}
	}
}
