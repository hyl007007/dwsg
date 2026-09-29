using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Dwsg.Shared.Generals
{
	public static class GeneralRules
	{
		public static readonly string[] CommandTypes = {
			"generals.allocateTroops", "generals.refillTroops", "generals.releaseTroops", "generals.setFormation",
			"generals.equip", "generals.unequip", "generals.equipBest", "generals.unequipAll"
		};

		// 参数中的旧编号只供原客户端及已完成身份映射的服务端内部使用。
		public static JObject Execute(JObject player, string type, JObject arguments, JArray troopConfiguration, long utcSeconds)
		{
			JObject candidate = (JObject)player.DeepClone();
			if (type == "generals.setFormation")
			{
				SetFormation(candidate, LegacyGenerals.Integer(arguments["teamIndex"]), LegacyGenerals.Integer(arguments["slotIndex"]),
					arguments["generalId"] == null || arguments["generalId"].Type == JTokenType.Null ? (int?)null : LegacyGenerals.Integer(arguments["generalId"]));
				return candidate;
			}
			int generalId = LegacyGenerals.Integer(arguments["generalId"]);
			JObject fief;
			JObject general = LegacyGenerals.General(candidate, generalId, out fief);
			LegacyGenerals.RequireIdle(general);
			switch (type)
			{
				case "generals.allocateTroops":
					Allocate(fief, general, LegacyGenerals.Integer(arguments["troopTypeId"]), arguments["count"] == null ? (int?)null : LegacyGenerals.Integer(arguments["count"]), troopConfiguration);
					break;
				case "generals.refillTroops":
					int troopId = LegacyGenerals.Integer(LegacyGenerals.Object(general["将领配兵"])["ID"]);
					if (troopId != 0) Allocate(fief, general, troopId, null, troopConfiguration);
					break;
				case "generals.releaseTroops": Release(fief, general); break;
				case "generals.equip":
					Equip(candidate, general, LegacyGenerals.Integer(arguments["equipmentSlot"]), LegacyGenerals.Integer(arguments["equipmentIndex"]));
					if (arguments.Value<bool?>("releaseTroops") == true) Release(fief, general);
					break;
				case "generals.unequip": Unequip(candidate, generalId, LegacyGenerals.Integer(arguments["equipmentSlot"])); break;
				case "generals.equipBest": EquipBest(candidate, general); break;
				case "generals.unequipAll": for (int slot = 0; slot < 4; slot++) Unequip(candidate, generalId, slot); break;
				default: throw new GeneralRuleException(GeneralFailure.InvalidArgument, "将领操作无效");
			}
			if (GeneralAttributeRules.Recalculate(candidate, utcSeconds)) throw new GeneralRuleException(GeneralFailure.InvalidData, "属性数据异常");
			return candidate;
		}

		public static JObject PrepareDismissal(JObject player, int generalId, long utcSeconds)
		{
			JObject candidate = (JObject)player.DeepClone();
			JObject fief;
			JObject general = LegacyGenerals.General(candidate, generalId, out fief);
			LegacyGenerals.RequireIdle(general, true);
			for (int slot = 0; slot < 4; slot++) Unequip(candidate, generalId, slot);
			Release(fief, general);
			for (int team = 0; team < 5; team++)
			{
				JArray formation = LegacyGenerals.Formation(candidate, team);
				for (int slot = 0; slot < 5; slot++) if (LegacyGenerals.Integer(formation[slot]) == generalId) formation[slot] = -1;
			}
			general["详细信息"]["编队"] = 0.0;
			if (GeneralAttributeRules.Recalculate(candidate, utcSeconds)) throw new GeneralRuleException(GeneralFailure.InvalidData, "属性数据异常");
			return candidate;
		}

		static JObject Pool(JObject fief, int troopId, bool create)
		{
			JObject found = null;
			foreach (JToken token in LegacyGenerals.Array(fief["闲兵信息表"]))
			{
				JObject entry = LegacyGenerals.Object(token);
				LegacyGenerals.Quantity(entry["数量"]);
				if (LegacyGenerals.Integer(entry["ID"]) != troopId) continue;
				if (found != null) throw new GeneralRuleException(GeneralFailure.InvalidData, "闲兵兵种记录重复");
				found = entry;
			}
			if (found == null && create)
			{
				found = new JObject { ["ID"] = troopId, ["数量"] = 0.0 };
				LegacyGenerals.Array(fief["闲兵信息表"]).Add(found);
			}
			return found;
		}

		static void RequireTroop(JArray configuration, int troopId)
		{
			if (troopId <= 0 || configuration == null) throw new GeneralRuleException(GeneralFailure.InvalidArgument, "兵种无效");
			foreach (JToken entry in configuration)
				if (LegacyGenerals.Integer(LegacyGenerals.Object(entry)["ID"]) == troopId) return;
			throw new GeneralRuleException(GeneralFailure.InvalidArgument, "兵种不在原配置中");
		}

		static void Allocate(JObject fief, JObject general, int troopId, int? count, JArray configuration)
		{
			RequireTroop(configuration, troopId);
			JObject assigned = LegacyGenerals.Object(general["将领配兵"]);
			int previousId = LegacyGenerals.Integer(assigned["ID"]);
			int previous = LegacyGenerals.Quantity(assigned["数量"]);
			int capacity = LegacyGenerals.Quantity(LegacyGenerals.Object(LegacyGenerals.Object(general["将领属性"])["最终属性"])["统兵"]);
			if (count.HasValue && (count.Value < 0 || count.Value > capacity)) throw new GeneralRuleException(GeneralFailure.InvalidArgument, "配兵数量超过统兵上限或为负数");
			JObject pool = Pool(fief, troopId, false);
			long available = (pool == null ? 0 : LegacyGenerals.Quantity(pool["数量"])) + (long)(previousId == troopId ? previous : 0);
			int target = count ?? (int)Math.Min(capacity, available);
			if (target > available) throw new GeneralRuleException(GeneralFailure.Conflict, "闲兵不足");
			if (previousId != troopId) Release(fief, general);
			long remaining = available - target;
			if (remaining > int.MaxValue) throw new GeneralRuleException(GeneralFailure.InvalidData, "闲兵数量超出范围");
			if (pool == null && remaining > 0) pool = Pool(fief, troopId, true);
			if (pool != null) pool["数量"] = (double)remaining;
			assigned["ID"] = (double)troopId;
			assigned["数量"] = (double)target;
		}

		static void Release(JObject fief, JObject general)
		{
			JObject assigned = LegacyGenerals.Object(general["将领配兵"]);
			int troopId = LegacyGenerals.Integer(assigned["ID"]);
			int quantity = LegacyGenerals.Quantity(assigned["数量"]);
			if (quantity > 0)
			{
				if (troopId <= 0) throw new GeneralRuleException(GeneralFailure.InvalidData, "已配兵兵种无效");
				JObject pool = Pool(fief, troopId, true);
				long merged = (long)LegacyGenerals.Quantity(pool["数量"]) + quantity;
				if (merged > int.MaxValue) throw new GeneralRuleException(GeneralFailure.InvalidData, "闲兵数量超出范围");
				pool["数量"] = (double)merged;
			}
			assigned["ID"] = 0.0;
			assigned["数量"] = 0.0;
		}

		static void SetFormation(JObject player, int team, int slot, int? generalId)
		{
			if (slot < 0 || slot >= 5) throw new GeneralRuleException(GeneralFailure.InvalidArgument, "编队槽位无效");
			JArray target = LegacyGenerals.Formation(player, team);
			int previous = LegacyGenerals.Integer(target[slot]);
			JObject fief;
			if (!generalId.HasValue)
			{
				if (previous == -1) return;
				JObject removed = LegacyGenerals.General(player, previous, out fief);
				LegacyGenerals.RequireIdle(removed);
				LegacyGenerals.Object(removed["详细信息"])["编队"] = 0.0;
				target[slot] = -1;
				return;
			}
			JObject selected = LegacyGenerals.General(player, generalId.Value, out fief);
			LegacyGenerals.RequireIdle(selected);
			if (previous == generalId.Value) return;
			if (previous != -1) throw new GeneralRuleException(GeneralFailure.Conflict, "编队槽位已有将领");
			for (int i = 0; i < 5; i++)
				foreach (JToken id in LegacyGenerals.Formation(player, i))
					if (LegacyGenerals.Integer(id) == generalId.Value) throw new GeneralRuleException(GeneralFailure.Conflict, "将领已在编队中");
			if (LegacyGenerals.Number(LegacyGenerals.Object(selected["详细信息"])["编队"]) != 0.0)
				throw new GeneralRuleException(GeneralFailure.Conflict, "将领已在编队中");
			target[slot] = generalId.Value;
			LegacyGenerals.Object(selected["详细信息"])["编队"] = (double)(team + 1);
		}

		static void Equip(JObject player, JObject general, int slot, int equipmentIndex)
		{
			JArray inventory = LegacyGenerals.Equipment(player, slot);
			if (equipmentIndex < 0 || equipmentIndex >= inventory.Count) throw new GeneralRuleException(GeneralFailure.NotFound, "装备不存在");
			JObject item = LegacyGenerals.Object(inventory[equipmentIndex]);
			int generalId = LegacyGenerals.Integer(general["ID"]);
			int owner = LegacyGenerals.Integer(item["将领ID"]);
			if (owner != -1 && owner != generalId) throw new GeneralRuleException(GeneralFailure.Conflict, "装备已被其他将领穿戴");
			JObject info = LegacyGenerals.Object(item["装备信息"]);
			string[] categories = { "头盔", "武器", "铠甲", "坐骑" };
			if (info.Value<string>("类型") != categories[slot]) throw new GeneralRuleException(GeneralFailure.InvalidData, "装备部位不匹配");
			double level = LegacyGenerals.Number(LegacyGenerals.Object(LegacyGenerals.Object(general["将领属性"])["成长点数"])["等级"]);
			if (level < LegacyGenerals.Number(info["等级"])) throw new GeneralRuleException(GeneralFailure.Conflict, "穿戴失败,等级未到");
			JObject worn = LegacyGenerals.WornEquipment(player, slot, generalId);
			if (worn != null) worn["将领ID"] = -1;
			item["将领ID"] = generalId;
		}

		static void Unequip(JObject player, int generalId, int slot)
		{
			JObject worn = LegacyGenerals.WornEquipment(player, slot, generalId);
			if (worn != null) worn["将领ID"] = -1;
		}

		static void EquipBest(JObject player, JObject general)
		{
			double level = LegacyGenerals.Number(LegacyGenerals.Object(LegacyGenerals.Object(general["将领属性"])["成长点数"])["等级"]);
			int generalId = LegacyGenerals.Integer(general["ID"]);
			for (int slot = 0; slot < 4; slot++)
			{
				JArray inventory = LegacyGenerals.Equipment(player, slot);
				JObject current = LegacyGenerals.WornEquipment(player, slot, generalId);
				double highest = current == null ? 0.0 : LegacyGenerals.Number(LegacyGenerals.Object(current["装备信息"])["基础值"]) + LegacyGenerals.Number(current["强化值"]);
				int selected = -1;
				for (int i = 0; i < inventory.Count; i++)
				{
					JObject item = LegacyGenerals.Object(inventory[i]);
					JObject info = LegacyGenerals.Object(item["装备信息"]);
					if (LegacyGenerals.Integer(item["将领ID"]) != -1 || LegacyGenerals.Number(info["等级"]) > level) continue;
					double value = LegacyGenerals.Number(info["基础值"]) + LegacyGenerals.Number(item["强化值"]);
					if (value > highest) { highest = value; selected = i; }
				}
				if (selected != -1) Equip(player, general, slot, selected);
			}
		}

		public static JObject Occupy(JObject player, int[] generalIds)
		{
			if (generalIds == null || generalIds.Length < 1 || generalIds.Length > 5) throw new GeneralRuleException(GeneralFailure.InvalidArgument, "出征需选择一至五位将领");
			JObject candidate = (JObject)player.DeepClone();
			HashSet<int> seen = new HashSet<int>();
			JObject selectedFief = null;
			foreach (int id in generalIds)
			{
				if (!seen.Add(id)) throw new GeneralRuleException(GeneralFailure.InvalidArgument, "出征将领重复");
				JObject fief;
				JObject general = LegacyGenerals.General(candidate, id, out fief);
				if (selectedFief != null && selectedFief != fief) throw new GeneralRuleException(GeneralFailure.Conflict, "出征将领须来自同一封地");
				selectedFief = fief;
				LegacyGenerals.RequireIdle(general);
				JObject details = LegacyGenerals.Object(general["详细信息"]);
				if (LegacyGenerals.Quantity(LegacyGenerals.Object(general["将领配兵"])["数量"]) <= 0) throw new GeneralRuleException(GeneralFailure.Conflict, "将领尚未配兵");
				if (LegacyGenerals.Number(details["剩余体力"]) < 5.0) throw new GeneralRuleException(GeneralFailure.Conflict, "将领体力不足");
				details["状态"] = 1.0;
			}
			return candidate;
		}
	}
}
