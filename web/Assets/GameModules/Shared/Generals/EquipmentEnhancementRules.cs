using System;
using Dwsg.Shared.Economy;
using Newtonsoft.Json.Linq;

namespace Dwsg.Shared.Generals
{
	public static class EquipmentEnhancementRules
	{
		public const double SuccessRate = 5000.0;
		public static int Pity(double level) { return level < 10 ? 20 : level < 20 ? 50 : level < 25 ? 80 : 100; }
		public static int Bonus(string type, double quality)
		{
			int index = quality == 1 ? 0 : quality == 2 ? 1 : quality == 3 ? 2 : quality == 4 ? 3 : -1;
			if (index < 0) return 1;
			return type == "头盔" || type == "武器" ? new[] { 2, 5, 7, 10 }[index]
				: type == "铠甲" ? new[] { 1, 2, 3, 5 }[index] : type == "坐骑" ? new[] { 4, 8, 12, 20 }[index] : 1;
		}
		public static string Material(string type, double quality)
		{
			int index = quality == 1 ? 0 : quality == 2 ? 1 : quality == 3 ? 2 : quality == 4 ? 3 : -1;
			return index < 0 ? "未知" : type == "坐骑" ? new[] { "浆果", "灵草", "玉露", "仙芝" }[index] : new[] { "镔铁", "水晶", "玄铁", "冰玉" }[index];
		}
		public static JObject Equipment(JObject player, int slot, int index)
		{
			JArray list = LegacyGenerals.Equipment(player, slot);
			if (index < 0 || index >= list.Count) throw new GeneralRuleException(GeneralFailure.NotFound, "装备实例不存在");
			JObject equipment = LegacyGenerals.Object(list[index]), info = LegacyGenerals.Object(equipment["装备信息"]);
			int quality = LegacyGenerals.Integer(equipment["品质"]);
			if (info.Value<string>("名称") == "空" || string.IsNullOrEmpty(info.Value<string>("名称")) || quality < 1 || quality > 4
				|| info.Value<string>("类型") != new[] { "头盔", "武器", "铠甲", "坐骑" }[slot])
				throw new GeneralRuleException(GeneralFailure.InvalidData, "装备类型或品质无效");
			return equipment;
		}

		public static JObject Execute(JObject player, int slot, int index, int count, JArray itemDefinitions,
			Func<int, int, int> random, long utcSeconds, out GameResult result)
		{
			if (count != 1 && count != 10) throw new GeneralRuleException(GeneralFailure.InvalidArgument, "请选择原单次或十次强化");
			JObject candidate = (JObject)player.DeepClone();
			JObject equipment = Equipment(candidate, slot, index);
			int level = LegacyGenerals.Quantity(equipment["强化等级"]), failures = LegacyGenerals.Quantity(equipment["已强化次数"]);
			if (level > 30 || random == null) throw new GeneralRuleException(GeneralFailure.InvalidData, "装备强化数据无效");
			string type = equipment["装备信息"].Value<string>("类型"), material = Material(type, equipment.Value<double>("品质"));
			int attempts = 0, consumed = 0, upgrades = 0, guaranteed = 0, missed = 0, status = 0;
			for (int i = 0; i < count; i++)
			{
				if (level == 30) { status = 4; break; }
				GameResult cost = InventoryRules.Consume(candidate, itemDefinitions, material, level + 1);
				if (cost.Code != GameCodes.Ok)
				{
					if (cost.Code != GameCodes.Conflict && cost.Code != GameCodes.NotFound) throw new GeneralRuleException(GeneralFailure.InvalidData, cost.Message);
					status = 3; break;
				}
				int roll = random(1, 10000);
				if (roll < 1 || roll >= 10000) throw new GeneralRuleException(GeneralFailure.InvalidData, "强化随机结果超出原范围");
				attempts++; consumed += level + 1;
				if (roll < SuccessRate || failures >= Pity(level))
				{
					status = roll < SuccessRate ? 1 : 2;
					if (status == 2) guaranteed++;
					level++; failures = 0; upgrades++;
					equipment["强化值"] = level * (double)Bonus(type, equipment.Value<double>("品质"));
				}
				else
				{
					if (failures == int.MaxValue) throw new GeneralRuleException(GeneralFailure.InvalidData, "强化次数超出范围");
					failures++; missed++; status = 0;
				}
			}
			string message = status == 1 ? "强化成功!" : status == 2 ? "保底强化成功!" : status == 3 ? "材料不足!" : status == 4 ? "已满级,不可强化!" : "强化失败!";
			if (attempts == 0) throw new GeneralRuleException(GeneralFailure.Conflict, message);
			equipment["强化等级"] = (double)level; equipment["已强化次数"] = (double)failures;
			RefreshAttributes(candidate, equipment, utcSeconds);
			result = GameResult.Success(new JObject { ["requestedCount"] = count, ["attempts"] = attempts, ["materialName"] = material,
				["consumedCount"] = consumed, ["upgradedCount"] = upgrades, ["guaranteedCount"] = guaranteed, ["failedCount"] = missed, ["level"] = level, ["status"] = status });
			result.Message = message;
			return candidate;
		}

		public static void RefreshAttributes(JObject player, JObject equipment, long utcSeconds)
		{
			// 原装备面板只刷新装备文字；派生属性更新不额外重置军情或体力。
			int owner = LegacyGenerals.Integer(equipment["将领ID"]);
			if (owner == -1) return;
			JObject derived = (JObject)player.DeepClone();
			if (GeneralAttributeRules.Recalculate(derived, utcSeconds)) throw new GeneralRuleException(GeneralFailure.InvalidData, "属性数据异常");
			JObject fief;
			LegacyGenerals.General(player, owner, out fief)["将领属性"]["最终属性"] = LegacyGenerals.General(derived, owner, out fief)["将领属性"]["最终属性"].DeepClone();
		}
	}
}
