using System;
using System.Collections.Generic;
using System.Linq;
using Dwsg.Shared.Economy;
using Newtonsoft.Json.Linq;

namespace Dwsg.Shared.Generals
{
	public static class EquipmentSoulRules
	{
		public static string Material(string type)
		{
			return type == "头盔" ? "天魂灵石" : type == "武器" ? "昆仑玄铁" : type == "铠甲" ? "地魄灵石" : type == "坐骑" ? "山海精华" : "未知";
		}
		public static double Limit(string equipmentType, double soulType)
		{
			if (soulType == 1) return equipmentType == "武器" ? 60 : 20;
			if (soulType == 2) return equipmentType == "铠甲" ? 30 : 10;
			if (soulType == 3) return equipmentType == "头盔" ? 60 : 20;
			if (soulType == 4) return equipmentType == "坐骑" ? 60 : 20;
			return 1; // 原第五类没有命中重复的第四类分支。
		}
		static JArray Souls(JObject equipment)
		{
			JArray souls = LegacyGenerals.Array(equipment["炼魂属性"]);
			if (souls.Count > 6) throw new GeneralRuleException(GeneralFailure.InvalidData, "炼魂属性超出原六槽");
			foreach (JToken token in souls)
			{
				JObject soul = LegacyGenerals.Object(token);
				int type = LegacyGenerals.Integer(soul["类型"]);
				LegacyGenerals.Integer(soul["炼魂值"]);
				if (type < 1 || type > 5 || soul["锁定"]?.Type != JTokenType.Boolean)
					throw new GeneralRuleException(GeneralFailure.InvalidData, "炼魂属性数据无效");
			}
			return souls;
		}
		static void SetLocks(JObject equipment, JArray indices)
		{
			JArray souls = Souls(equipment);
			if (indices == null) throw new GeneralRuleException(GeneralFailure.InvalidArgument, "请选择原炼魂锁定槽位");
			HashSet<int> selected = new HashSet<int>();
			foreach (JToken entry in indices)
			{
				int index = LegacyGenerals.Integer(entry);
				if (index < 0 || index >= souls.Count || !selected.Add(index))
					throw new GeneralRuleException(GeneralFailure.InvalidArgument, "炼魂锁定槽位不存在或重复");
			}
			for (int i = 0; i < souls.Count; i++) souls[i]["锁定"] = selected.Contains(i);
		}
		public static JObject Lock(JObject player, int slot, int index, JArray indices)
		{
			JObject candidate = (JObject)player.DeepClone();
			SetLocks(EquipmentEnhancementRules.Equipment(candidate, slot, index), indices);
			return candidate;
		}
		static void AutoLock(JObject equipment)
		{
			foreach (JObject soul in Souls(equipment))
				if (soul.Value<int>("炼魂值") >= 50 || soul.Value<int>("类型") == 2 && soul.Value<int>("炼魂值") > 20) soul["锁定"] = true;
		}
		static int Draw(Func<int, int, int> random, int min, int max)
		{
			int value = random(min, max);
			if (value < min || value >= max) throw new GeneralRuleException(GeneralFailure.InvalidData, "炼魂随机结果超出原范围");
			return value;
		}
		public static JObject Execute(JObject player, int slot, int index, int mode, int count, JArray lockedIndices,
			JArray itemDefinitions, Func<int, int, int> random, Func<float, float, float> randomFloat, long utcSeconds, out GameResult result)
		{
			if (mode < 0 || mode > 2 || count != 1 && count != 30)
				throw new GeneralRuleException(GeneralFailure.InvalidArgument, "请选择原三档及单次或三十次炼魂");
			if (random == null || randomFloat == null) throw new GeneralRuleException(GeneralFailure.InvalidData, "炼魂随机源不存在");
			JObject candidate = (JObject)player.DeepClone(), equipment = EquipmentEnhancementRules.Equipment(candidate, slot, index);
			JArray originalSouls = (JArray)Souls(equipment).DeepClone();
			SetLocks(equipment, lockedIndices);
			string type = equipment["装备信息"].Value<string>("类型"), material = Material(type);
			int quality = LegacyGenerals.Integer(equipment["品质"]), attempts = 0, refined = 0, failed = 0, status = 0;
			int consumed = 0, crystals = 0; double goldCost = 0;
			for (int i = 0; i < count; i++)
			{
				equipment = EquipmentEnhancementRules.Equipment(candidate, slot, index);
				int locked = Souls(equipment).Count(s => s.Value<bool>("锁定"));
				if (count == 30 && locked == 6) { status = 4; break; }
				JObject paid = (JObject)candidate.DeepClone();
				GameResult primary = InventoryRules.Consume(paid, itemDefinitions, material, quality);
				GameResult secondary = locked == 0 ? GameResult.Success() : InventoryRules.Consume(paid, itemDefinitions, "凝魂晶石", locked);
				if (primary.Code != GameCodes.Ok || secondary.Code != GameCodes.Ok)
				{
					GameResult rejection = primary.Code != GameCodes.Ok ? primary : secondary;
					if (rejection.Code != GameCodes.Conflict && rejection.Code != GameCodes.NotFound) throw new GeneralRuleException(GeneralFailure.InvalidData, rejection.Message);
					if (count == 30) AutoLock(equipment);
					status = 3; break; // 使用当前背包，避免原批量缓存放行已耗尽的材料。
				}
				candidate = paid; equipment = EquipmentEnhancementRules.Equipment(candidate, slot, index);
				attempts++; consumed += quality; crystals += locked;
				JObject property = LegacyGenerals.Object(candidate["财产信息"]);
				double gold = LegacyGenerals.Number(property["黄金"]);
				if (mode != 0 && gold < (mode == 1 ? 5 : 10))
				{
					failed++; status = 2;
					if (count == 30) AutoLock(equipment);
					continue; // 原先消费材料，再检查黄金；此次真实失败仍保存消费。
				}
				if (mode != 0) { property["黄金"] = gold - 5; goldCost += 5; }
				JArray rows = new JArray(Souls(equipment).Where(s => s.Value<bool>("锁定")).Select(s => s.DeepClone()));
				if (rows.Count < 6)
				{
					int min = mode == 0 ? 1 : Math.Min(3, 6 - rows.Count), added = Draw(random, min, 7 - rows.Count);
					for (int row = 0; row < added; row++)
					{
						int soulType = Draw(random, 1, 6); float upper = (float)Limit(type, soulType) + 1f;
						float raw = randomFloat(1f, upper);
						if (float.IsNaN(raw) || float.IsInfinity(raw) || raw < 1f || raw > upper)
							throw new GeneralRuleException(GeneralFailure.InvalidData, "炼魂随机数值超出原范围");
						int value = (int)raw;
						if (mode == 0 && Draw(random, 1, 101) < 30) value = -value;
						rows.Add(new JObject { ["类型"] = (double)soulType, ["炼魂值"] = (double)value, ["锁定"] = false });
					}
					equipment["炼魂属性"] = rows;
					refined++;
				}
				status = 1;
				if (count == 30) AutoLock(equipment);
			}
			equipment = EquipmentEnhancementRules.Equipment(candidate, slot, index);
			string message = status == 2 || status == 3 ? "材料不足" : status == 4 ? "已经六条属性了" : null;
			if (attempts == 0 && JToken.DeepEquals(originalSouls, Souls(equipment))) throw new GeneralRuleException(GeneralFailure.Conflict, message ?? "未执行炼魂");
			if (refined > 0) EquipmentEnhancementRules.RefreshAttributes(candidate, equipment, utcSeconds);
			result = GameResult.Success(new JObject { ["requestedCount"] = count, ["attempts"] = attempts, ["refinedCount"] = refined,
				["failedCount"] = failed, ["actualFailure"] = failed > 0 || status == 3, ["status"] = status,
				["materials"] = new JObject { [material] = consumed, ["凝魂晶石"] = crystals }, ["goldCost"] = goldCost });
			result.Message = message;
			return candidate;
		}
	}
}
