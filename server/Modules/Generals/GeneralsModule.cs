using System;
using System.Collections.Generic;
using System.Linq;
using Dwsg.Shared;
using Dwsg.Shared.Generals;
using Newtonsoft.Json.Linq;

namespace Dwsg.Server.Modules.Generals
{
	public sealed class GeneralOutcome
	{
		public string GeneralId { get; set; }
		public JObject General { get; set; }
		public int Remaining { get; set; }
		public int Wounded { get; set; }
	}

	public sealed partial class GeneralsModule : IGameModule, IGameTickModule
	{
		public IReadOnlyCollection<string> CommandTypes { get { return GeneralRules.CommandTypes.Concat(new[] { "generals.refreshTavern", "generals.recruit", "generals.dismiss", "generals.healWounded" }).ToArray(); } }

		public GameResult Execute(WorldState candidate, CommandContext context, GameCommand command)
		{
			if (command.Type == "generals.refreshTavern" || command.Type == "generals.recruit" || command.Type == "generals.dismiss" || command.Type == "generals.healWounded")
				return ExecuteRoster(candidate, context, command);
			if (context?.Actor == null || context.Actor.IsSystem || string.IsNullOrEmpty(context.Actor.PlayerId))
				return GameResult.Reject(GameCodes.Unauthenticated, "请先登录角色");
			if (context.Actor.WorldId != candidate.WorldId || command.WorldId != candidate.WorldId)
				return GameResult.Reject(GameCodes.Forbidden, "不能操作其他世界");
			try
			{
				if (command.Payload == null) throw new GeneralRuleException(GeneralFailure.InvalidArgument, "操作参数不完整");
				WorldState working = candidate.Clone();
				string playerId = context.Actor.PlayerId;
				JObject player = working.RequirePlayer(playerId);
				JObject arguments = (JObject)command.Payload.DeepClone();
				JToken requestedGeneral = arguments["generalId"];
				if (requestedGeneral != null && requestedGeneral.Type != JTokenType.Null)
				{
					string id = StableId(requestedGeneral);
					JObject fief;
					JObject general = ResolveGeneral(working, playerId, id, out fief);
					RequireAvailable(working, id, general);
					arguments["generalId"] = LegacyGenerals.Integer(general["ID"]);
				}
				else if (command.Type != "generals.setFormation") throw new GeneralRuleException(GeneralFailure.InvalidArgument, "缺少将领编号");
				if (command.Type == "generals.setFormation")
				{
					int team = LegacyGenerals.Integer(arguments["teamIndex"]);
					int slot = LegacyGenerals.Integer(arguments["slotIndex"]);
					if (slot < 0 || slot >= 5) throw new GeneralRuleException(GeneralFailure.InvalidArgument, "编队槽位无效");
					int previous = LegacyGenerals.Integer(LegacyGenerals.Formation(player, team)[slot]);
					if (previous != -1)
					{
						string previousId = FindGeneralId(working, playerId, previous);
						JObject fief;
						RequireAvailable(working, previousId, LegacyGenerals.General(player, previous, out fief));
					}
				}
				if (command.Type == "generals.equip")
				{
					JObject mapping = RequireOwnedMapping(working, "equipment", StableId(arguments["equipmentId"]), playerId);
					arguments["equipmentSlot"] = LegacyGenerals.Integer(mapping["slot"]);
					arguments["equipmentIndex"] = LegacyGenerals.Integer(mapping["legacyIndex"]);
					if (arguments["releaseTroops"] != null && arguments["releaseTroops"].Type != JTokenType.Boolean)
						throw new GeneralRuleException(GeneralFailure.InvalidArgument, "解除配兵参数无效");
				}
				JObject updated = GeneralRules.Execute(player, command.Type, arguments, working.Data["兵种配置"] as JArray, context.ServerUtcMs / 1000);
				((JArray)working.Data["玩家列表"])[working.ResolvePlayerIndex(playerId)] = updated;
				candidate.Data = working.Data;
				candidate.EntityMappings = working.EntityMappings;
				GameResult result = GameResult.Success(new JObject { ["generalId"] = requestedGeneral?.DeepClone() });
				result.Events.Add(new GameEvent { WorldId = candidate.WorldId, Type = command.Type, ServerUtcMs = context.ServerUtcMs,
					AudiencePlayerIds = new[] { playerId }, Data = (JObject)result.Data.DeepClone() });
				return result;
			}
			catch (GeneralRuleException failure) { return Rejection(failure); }
			catch (InvalidOperationException) { return GameResult.Reject(GameCodes.NotFound, "角色数据不存在"); }
		}

		public static void EnsureMappings(WorldState candidate)
		{
			JObject generals = Map(candidate, "generals");
			JObject equipment = Map(candidate, "equipment");
			JObject fiefs = Map(candidate, "fiefs");
			Map(candidate, "generalOccupancy");
			JObject players = LegacyGenerals.Object(candidate.EntityMappings["players"]);
			foreach (JProperty binding in players.Properties())
			{
				JObject player = candidate.RequirePlayer(binding.Name);
				foreach (JObject fief in LegacyGenerals.Array(player["封地信息表"]))
				{
					int legacyFiefId = LegacyGenerals.Integer(fief["ID"]);
					if (!fiefs.Properties().Any(p => p.Value.Value<string>("playerId") == binding.Name && p.Value.Value<int>("legacyId") == legacyFiefId))
						fiefs[Guid.NewGuid().ToString("N")] = new JObject { ["playerId"] = binding.Name, ["legacyId"] = legacyFiefId };
				}
				foreach (JToken fief in LegacyGenerals.Array(player["封地信息表"]))
					foreach (JToken general in LegacyGenerals.Array(fief["将领信息表"]))
					{
						int legacyId = LegacyGenerals.Integer(general["ID"]);
						if (!generals.Properties().Any(p => p.Value.Value<string>("playerId") == binding.Name && p.Value.Value<int>("legacyId") == legacyId))
							generals[Guid.NewGuid().ToString("N")] = new JObject { ["playerId"] = binding.Name, ["legacyId"] = legacyId };
					}
				for (int slot = 0; slot < 4; slot++)
					for (int index = 0; index < LegacyGenerals.Equipment(player, slot).Count; index++)
						if (!equipment.Properties().Any(p => p.Value.Value<string>("playerId") == binding.Name && p.Value.Value<int>("slot") == slot && p.Value.Value<int>("legacyIndex") == index))
							equipment[Guid.NewGuid().ToString("N")] = new JObject { ["playerId"] = binding.Name, ["slot"] = slot, ["legacyIndex"] = index };
			}
		}

		public static JObject ResolveGeneral(WorldState candidate, string playerId, string generalId, out JObject fief)
		{
			generalId = StableId(new JValue(generalId));
			JObject mapping = RequireOwnedMapping(candidate, "generals", generalId, playerId);
			return LegacyGenerals.General(candidate.RequirePlayer(playerId), LegacyGenerals.Integer(mapping["legacyId"]), out fief);
		}

		public static GameResult TryOccupy(WorldState candidate, string playerId, string armyId, IEnumerable<string> generalIds, out List<JObject> generals)
		{
			generals = null;
			try
			{
				if (string.IsNullOrWhiteSpace(armyId)) throw new GeneralRuleException(GeneralFailure.InvalidArgument, "军队编号无效");
				string[] ids = generalIds?.ToArray();
				if (ids == null || ids.Length < 1 || ids.Length > 5 || ids.Distinct().Count() != ids.Length)
					throw new GeneralRuleException(GeneralFailure.InvalidArgument, "出征需一至五位不同将领");
				WorldState working = candidate.Clone();
				List<int> legacyIds = new List<int>();
				foreach (string id in ids)
				{
					JObject fief;
					JObject general = ResolveGeneral(working, playerId, id, out fief);
					RequireAvailable(working, id, general);
					legacyIds.Add(LegacyGenerals.Integer(general["ID"]));
				}
				JObject updated = GeneralRules.Occupy(working.RequirePlayer(playerId), legacyIds.ToArray());
				((JArray)working.Data["玩家列表"])[working.ResolvePlayerIndex(playerId)] = updated;
				JObject occupancy = Map(working, "generalOccupancy");
				generals = new List<JObject>();
				foreach (string id in ids)
				{
					occupancy[id] = new JObject { ["playerId"] = playerId, ["armyId"] = armyId };
					JObject fief;
					generals.Add(ResolveGeneral(working, playerId, id, out fief));
				}
				candidate.Data = working.Data;
				candidate.EntityMappings = working.EntityMappings;
				return GameResult.Success();
			}
			catch (GeneralRuleException failure) { return Rejection(failure); }
		}

		public static GameResult ApplyOutcome(WorldState candidate, string playerId, string armyId, IEnumerable<GeneralOutcome> outcomes, bool completeArmy = true)
		{
			try
			{
				GeneralOutcome[] entries = outcomes?.ToArray();
				if (entries == null || entries.Length < 1 || entries.Select(o => o.GeneralId).Distinct().Count() != entries.Length)
					throw new GeneralRuleException(GeneralFailure.InvalidArgument, "战斗结算将领无效");
				WorldState working = candidate.Clone();
				JObject occupancy = Map(working, "generalOccupancy");
				string[] reserved = occupancy.Properties().Where(p => p.Value.Value<string>("playerId") == playerId && p.Value.Value<string>("armyId") == armyId).Select(p => p.Name).ToArray();
				if (completeArmy ? reserved.Length != entries.Length || !reserved.OrderBy(s => s).SequenceEqual(entries.Select(e => e.GeneralId).OrderBy(s => s))
					: entries.Any(e => !reserved.Contains(e.GeneralId)))
					throw new GeneralRuleException(GeneralFailure.Conflict, "军队与将领占用不匹配或已结算");
				foreach (GeneralOutcome outcome in entries)
				{
					JObject fief;
					JObject general = ResolveGeneral(working, playerId, outcome.GeneralId, out fief);
					JObject assigned = LegacyGenerals.Object(general["将领配兵"]);
					int original = LegacyGenerals.Quantity(assigned["数量"]);
					if (outcome.General == null || outcome.Remaining < 0 || outcome.Wounded < 0 || (long)outcome.Remaining + outcome.Wounded > original
						|| LegacyGenerals.Integer(outcome.General["ID"]) != LegacyGenerals.Integer(general["ID"])
						|| LegacyGenerals.Integer(outcome.General["将领配兵"]["ID"]) != LegacyGenerals.Integer(assigned["ID"]))
						throw new GeneralRuleException(GeneralFailure.InvalidData, "战斗结算兵力或将领不匹配");
					JObject growth = LegacyGenerals.Object(outcome.General["将领属性"]["成长点数"]);
					JObject final = LegacyGenerals.Object(outcome.General["将领属性"]["最终属性"]);
					general["将领属性"]["成长点数"] = growth.DeepClone();
					general["将领属性"]["最终属性"] = final.DeepClone();
					foreach (string field in new[] { "经验", "升级需要经验", "剩余体力" })
						general["详细信息"][field] = LegacyGenerals.Number(outcome.General["详细信息"][field]);
					assigned["数量"] = (double)outcome.Remaining;
					general["详细信息"]["剩余兵力"] = (double)outcome.Remaining;
					general["详细信息"]["状态"] = 0.0;
					if (outcome.Wounded > 0)
					{
						JArray wounded = LegacyGenerals.Array(fief["伤兵信息表"]);
						int troopId = LegacyGenerals.Integer(assigned["ID"]);
						JObject pool = wounded.OfType<JObject>().SingleOrDefault(w => LegacyGenerals.Integer(w["ID"]) == troopId);
						if (pool == null) { pool = new JObject { ["ID"] = troopId, ["数量"] = 0.0 }; wounded.Add(pool); }
						long merged = (long)LegacyGenerals.Quantity(pool["数量"]) + outcome.Wounded;
						if (merged > int.MaxValue) throw new GeneralRuleException(GeneralFailure.InvalidData, "伤兵数量超出范围");
						pool["数量"] = (double)merged;
					}
					occupancy.Remove(outcome.GeneralId);
				}
				candidate.Data = working.Data;
				candidate.EntityMappings = working.EntityMappings;
				return GameResult.Success();
			}
			catch (GeneralRuleException failure) { return Rejection(failure); }
		}

		static JObject Map(WorldState world, string name)
		{
			if (world.EntityMappings[name] == null) world.EntityMappings[name] = new JObject();
			return LegacyGenerals.Object(world.EntityMappings[name]);
		}
		static JObject RequireOwnedMapping(WorldState world, string map, string id, string playerId)
		{
			JObject mapping = world.EntityMappings[map]?[id] as JObject;
			if (mapping == null || mapping.Value<string>("playerId") != playerId)
				throw new GeneralRuleException(GeneralFailure.NotFound, "实例不存在或不属于当前角色");
			return mapping;
		}
		static void RequireAvailable(WorldState world, string id, JObject general)
		{
			LegacyGenerals.RequireIdle(general);
			if (world.EntityMappings["generalOccupancy"]?[id] != null) throw new GeneralRuleException(GeneralFailure.Conflict, "将领已在出征军队中");
		}
		static string FindGeneralId(WorldState world, string playerId, int legacyId)
		{
			JProperty mapping = LegacyGenerals.Object(world.EntityMappings["generals"]).Properties().SingleOrDefault(p => p.Value.Value<string>("playerId") == playerId && p.Value.Value<int>("legacyId") == legacyId);
			if (mapping == null) throw new GeneralRuleException(GeneralFailure.NotFound, "将领实例映射不存在");
			return mapping.Name;
		}
		static string StableId(JToken token)
		{
			if (token == null || token.Type != JTokenType.String || string.IsNullOrWhiteSpace(token.Value<string>()) || token.Value<string>().Length > 128)
				throw new GeneralRuleException(GeneralFailure.InvalidArgument, "实例编号无效");
			return token.Value<string>();
		}
		static GameResult Rejection(GeneralRuleException failure)
		{
			return GameResult.Reject(failure.Failure == GeneralFailure.Conflict ? GameCodes.Conflict : failure.Failure == GeneralFailure.NotFound ? GameCodes.NotFound : GameCodes.InvalidArgument, failure.Message);
		}
	}
}
