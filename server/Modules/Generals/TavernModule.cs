using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using Dwsg.Shared;
using Dwsg.Shared.Combat;
using Dwsg.Shared.Economy;
using Dwsg.Shared.Generals;
using Newtonsoft.Json.Linq;

namespace Dwsg.Server.Modules.Generals
{
	public sealed partial class GeneralsModule
	{
		public IEnumerable<GameCommand> CollectDueCommands(WorldState state, long serverUtcMs)
		{
			JObject taverns = state.EntityMappings["taverns"] as JObject;
			if (taverns == null) yield break;
			foreach (JProperty tavern in taverns.Properties())
			{
				long refreshed = tavern.Value.Value<long>("lastRefreshUtcMs");
				if (serverUtcMs / 1000 - refreshed / 1000 <= 3600) continue;
				yield return new GameCommand { WorldId = state.WorldId, Type = "generals.refreshTavern",
					RequestId = "tavern-refresh-" + tavern.Name + "-" + refreshed,
					Payload = new JObject { ["refreshType"] = 0, ["playerId"] = tavern.Name } };
			}
		}

		GameResult ExecuteRoster(WorldState candidate, CommandContext context, GameCommand command)
		{
			if (context?.Actor == null) return GameResult.Reject(GameCodes.Unauthenticated, "请先登录角色");
			if (context.Actor.WorldId != candidate.WorldId || command.WorldId != candidate.WorldId) return GameResult.Reject(GameCodes.Forbidden, "不能操作其他世界");
			try
			{
				if (command.Payload == null) throw new GeneralRuleException(GeneralFailure.InvalidArgument, "操作参数不完整");
				WorldState working = candidate.Clone();
				string playerId = context.Actor.PlayerId;
				if (context.Actor.IsSystem)
				{
					if (command.Type != "generals.refreshTavern" || LegacyGenerals.Integer(command.Payload["refreshType"]) != 0)
						return GameResult.Reject(GameCodes.Forbidden, "系统仅可自动刷新酒馆");
					playerId = StableId(command.Payload["playerId"]);
				}
				else if (string.IsNullOrWhiteSpace(playerId)) return GameResult.Reject(GameCodes.Unauthenticated, "请先登录角色");
				JObject player = working.RequirePlayer(playerId);
				GameResult result;
				if (command.Type == "generals.refreshTavern") result = RefreshTavern(working, playerId, player, command.Payload, context.ServerUtcMs);
				else if (command.Type == "generals.recruit") result = Recruit(working, playerId, player, command.Payload, context.ServerUtcMs);
				else if (command.Type == "generals.healWounded") result = HealWounded(working, playerId, player, command.Payload);
				else if (command.Type == "generals.cultivate") result = Cultivate(working, playerId, player, command.Payload, context.ServerUtcMs);
				else if (command.Type == "generals.useExperienceBook") result = UseExperienceBook(working, playerId, player, command.Payload, context.ServerUtcMs);
				else if (command.Type == "generals.enhanceEquipment") result = EnhanceEquipment(working, playerId, player, command.Payload, context.ServerUtcMs);
				else if (command.Type == "generals.refineEquipment" || command.Type == "generals.setSoulLocks") result = RefineEquipment(working, playerId, player, command.Payload, command.Type, context.ServerUtcMs);
				else if (command.Type == "generals.persuadeCaptive" || command.Type == "generals.releaseCaptive") result = ManageCaptive(working, playerId, player, command.Payload, command.Type, context.ServerUtcMs);
				else result = Dismiss(working, playerId, player, command.Payload, context.ServerUtcMs);
				if (result.Code != GameCodes.Ok) return result;
				candidate.Data = working.Data;
				candidate.EntityMappings = working.EntityMappings;
				result.Events.Add(new GameEvent { WorldId = candidate.WorldId, Type = command.Type, ServerUtcMs = context.ServerUtcMs,
					AudiencePlayerIds = new[] { playerId }, Data = (JObject)result.Data.DeepClone() });
				return result;
			}
			catch (GeneralRuleException failure) { return Rejection(failure); }
			catch (InvalidOperationException) { return GameResult.Reject(GameCodes.NotFound, "角色或实例数据不存在"); }
		}

		static GameResult RefreshTavern(WorldState working, string playerId, JObject player, JObject payload, long now)
		{
			int type = LegacyGenerals.Integer(payload["refreshType"]);
			if (type < 0 || type > 3) throw new GeneralRuleException(GeneralFailure.InvalidArgument, "酒馆刷新类型无效");
			JObject taverns = Map(working, "taverns");
			JObject previous = taverns[playerId] as JObject;
			if (type == 0 && previous != null && now / 1000 - previous.Value<long>("lastRefreshUtcMs") / 1000 <= 3600)
				return GameResult.Success(new JObject { ["tavern"] = previous.DeepClone() });
			if (type != 0)
			{
				GameResult cost = InventoryRules.ConsumeOne(player, working.Data["道具配置"] as JArray, type == 1 ? "招贤令" : type == 2 ? "招贤金榜" : "皇榜");
				if (cost.Code != GameCodes.Ok) return cost;
			}
			JArray generated = TavernRules.Generate(LegacyGenerals.Array(working.Data["将领配置"]), type, RandomNumberGenerator.GetInt32,
				() => BanditGenerator.GenerateName(LegacyGenerals.Object(working.Data["姓名配置"]), RandomNumberGenerator.GetInt32));
			JArray candidates = new JArray();
			foreach (JToken general in generated) candidates.Add(new JObject { ["id"] = Guid.NewGuid().ToString("N"), ["general"] = general });
			JObject tavern = new JObject { ["lastRefreshUtcMs"] = now / 1000 * 1000, ["candidates"] = candidates };
			taverns[playerId] = tavern;
			return GameResult.Success(new JObject { ["tavern"] = tavern.DeepClone() });
		}

		static GameResult Recruit(WorldState working, string playerId, JObject player, JObject payload, long now)
		{
			string candidateId = StableId(payload["candidateId"]);
			JObject fief = RequireOwnedMapping(working, "fiefs", StableId(payload["fiefId"]), playerId);
			JObject tavern = working.EntityMappings["taverns"]?[playerId] as JObject;
			if (tavern == null || now / 1000 - tavern.Value<long>("lastRefreshUtcMs") / 1000 > 3600)
				throw new GeneralRuleException(GeneralFailure.Conflict, "酒馆将领已刷新，请重新选择");
			JArray candidates = LegacyGenerals.Array(tavern["candidates"]);
			JObject selected = candidates.OfType<JObject>().SingleOrDefault(c => c.Value<string>("id") == candidateId);
			if (selected == null) throw new GeneralRuleException(GeneralFailure.NotFound, "候选将领不存在或已被招募");
			int nextId = LegacyGenerals.Integer(player["将领ID标识"]);
			JObject updated = TavernRules.AddGeneral(player, LegacyGenerals.Integer(fief["legacyId"]), LegacyGenerals.Object(selected["general"]), now / 1000);
			((JArray)working.Data["玩家列表"])[working.ResolvePlayerIndex(playerId)] = updated;
			candidates.Remove(selected);
			Map(working, "generals")[candidateId] = new JObject { ["playerId"] = playerId, ["legacyId"] = nextId };
			return GameResult.Success(new JObject { ["generalId"] = candidateId, ["tavern"] = tavern.DeepClone() });
		}

		static GameResult Dismiss(WorldState working, string playerId, JObject player, JObject payload, long now)
		{
			string id = StableId(payload["generalId"]);
			JObject fief;
			JObject original = ResolveGeneral(working, playerId, id, out fief);
			if (working.EntityMappings["generalOccupancy"]?[id] != null) throw new GeneralRuleException(GeneralFailure.Conflict, "将领已在出征军队中");
			GeneralDismissal change = DismissalRules.Execute(LegacyGenerals.Array(working.Data["玩家列表"]), working.ResolvePlayerIndex(playerId), LegacyGenerals.Integer(original["ID"]), now / 1000);
			if (change.ReturnedToNature)
			{
				JProperty nature = LegacyGenerals.Object(working.EntityMappings["players"]).Properties().SingleOrDefault(p => LegacyGenerals.Integer(p.Value) == 2);
				if (nature == null) throw new GeneralRuleException(GeneralFailure.NotFound, "名将回归角色映射不存在");
				Map(working, "generals")[id] = new JObject { ["playerId"] = nature.Name, ["legacyId"] = change.ReturnedLegacyId };
			}
			else Map(working, "generals").Remove(id);
			working.Data["玩家列表"] = change.Players;
			return GameResult.Success(new JObject { ["generalId"] = id, ["returnedToNature"] = change.ReturnedToNature });
		}
	}
}
