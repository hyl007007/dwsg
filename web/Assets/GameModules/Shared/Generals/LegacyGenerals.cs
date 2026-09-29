using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Dwsg.Shared.Generals
{
	public enum GeneralFailure { InvalidArgument, NotFound, Conflict, InvalidData }

	public sealed class GeneralRuleException : Exception
	{
		public GeneralFailure Failure { get; private set; }
		public GeneralRuleException(GeneralFailure failure, string message) : base(message) { Failure = failure; }
	}

	public static class LegacyGenerals
	{
		public static readonly string[] EquipmentLists = { "头盔装备列表", "武器装备列表", "铠甲装备列表", "坐骑装备列表" };

		public static JObject Object(JToken token)
		{
			JObject value = token as JObject;
			if (value == null) throw new GeneralRuleException(GeneralFailure.InvalidData, "将领数据结构不完整");
			return value;
		}

		public static JArray Array(JToken token)
		{
			JArray value = token as JArray;
			if (value == null) throw new GeneralRuleException(GeneralFailure.InvalidData, "将领列表数据不完整");
			return value;
		}

		public static double Number(JToken token)
		{
			if (token == null || (token.Type != JTokenType.Integer && token.Type != JTokenType.Float))
				throw new GeneralRuleException(GeneralFailure.InvalidData, "将领数值格式无效");
			double value = token.Value<double>();
			if (double.IsNaN(value) || double.IsInfinity(value)) throw new GeneralRuleException(GeneralFailure.InvalidData, "将领数值无效");
			return value;
		}

		public static int Integer(JToken token)
		{
			double value = Number(token);
			if (value < int.MinValue || value > int.MaxValue || value != Math.Truncate(value))
				throw new GeneralRuleException(GeneralFailure.InvalidData, "将领编号或数量必须是整数");
			return (int)value;
		}

		public static int Quantity(JToken token)
		{
			int value = Integer(token);
			if (value < 0) throw new GeneralRuleException(GeneralFailure.InvalidData, "兵力不能为负数");
			return value;
		}

		public static JObject General(JObject player, int generalId, out JObject fief)
		{
			JObject found = null;
			fief = null;
			foreach (JToken fiefToken in Array(player["封地信息表"]))
			{
				JObject current = Object(fiefToken);
				foreach (JToken generalToken in Array(current["将领信息表"]))
				{
					JObject general = Object(generalToken);
					if (Integer(general["ID"]) != generalId) continue;
					if (found != null) throw new GeneralRuleException(GeneralFailure.InvalidData, "将领实例编号重复");
					found = general;
					fief = current;
				}
			}
			if (found == null) throw new GeneralRuleException(GeneralFailure.NotFound, "将领不存在或不属于当前角色");
			return found;
		}

		public static JArray Equipment(JObject player, int slot)
		{
			if (slot < 0 || slot >= 4) throw new GeneralRuleException(GeneralFailure.InvalidArgument, "装备部位无效");
			return Array(Object(player["背包装备列表"])[EquipmentLists[slot]]);
		}

		public static JObject WornEquipment(JObject player, int slot, int generalId)
		{
			JObject found = null;
			foreach (JToken token in Equipment(player, slot))
			{
				JObject item = Object(token);
				if (Integer(item["将领ID"]) != generalId) continue;
				if (found != null) throw new GeneralRuleException(GeneralFailure.InvalidData, "同一将领的装备部位重复");
				found = item;
			}
			return found;
		}

		public static void RequireIdle(JObject general, bool allowCaptured = false)
		{
			double state = Number(Object(general["详细信息"])["状态"]);
			if (state != 0.0 && !(allowCaptured && state == 3.0))
				throw new GeneralRuleException(GeneralFailure.Conflict, "状态非空闲!");
		}

		public static JArray Formation(JObject player, int teamIndex)
		{
			JArray teams = Array(player["编队信息表"]);
			if (teamIndex < 0 || teamIndex >= 5) throw new GeneralRuleException(GeneralFailure.InvalidArgument, "编队编号无效");
			if (teams.Count != 5) throw new GeneralRuleException(GeneralFailure.InvalidData, "编队数据必须保留原五队");
			JArray slots = Array(teams[teamIndex]);
			if (slots.Count != 5) throw new GeneralRuleException(GeneralFailure.InvalidData, "编队数据必须保留原五槽");
			return slots;
		}
	}
}
