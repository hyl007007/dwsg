using System;
using Newtonsoft.Json.Linq;

namespace Dwsg.Shared.Generals
{
	public static class GeneralAttributeRules
	{
		// 保留原公式的单精度舍入、装备顺序与属性阈值；属性刷新不恢复已消耗体力。
		public static bool Recalculate(JObject player, long utcSeconds)
		{
			bool abnormal = false;
			JObject basics = LegacyGenerals.Object(player["基础信息"]);
			JObject technology = LegacyGenerals.Object(player["科技信息"]);
			foreach (JToken fiefToken in LegacyGenerals.Array(player["封地信息表"]))
			{
				foreach (JToken generalToken in LegacyGenerals.Array(LegacyGenerals.Object(fiefToken)["将领信息表"]))
				{
					JObject general = LegacyGenerals.Object(generalToken);
					JObject attributes = LegacyGenerals.Object(general["将领属性"]);
					JObject initial = LegacyGenerals.Object(attributes["初始属性"]);
					JObject points = LegacyGenerals.Object(attributes["成长点数"]);
					JObject final = LegacyGenerals.Object(attributes["最终属性"]);
					JObject details = LegacyGenerals.Object(general["详细信息"]);
					double previousMaximum, previousStamina;
					if (!Dwsg.Shared.Economy.ShopRules.TryNumber(final["体力上限"], out previousMaximum)) previousMaximum = 0;
					if (!Dwsg.Shared.Economy.ShopRules.TryNumber(details["剩余体力"], out previousStamina)) previousStamina = 0;
					double breakout = LegacyGenerals.Number(initial["突围"]);
					double growth = LegacyGenerals.Number(initial["成长"]);
					double level = LegacyGenerals.Number(points["等级"]);
					int type = LegacyGenerals.Integer(initial["类型"]);
					string series = initial.Value<string>("系列");
					double major;
					double minor = 0.0;
					if (breakout >= 99.0)
					{
						if (type == 1)
						{
							major = series == "尊将" ? growth * 2.5 + 9.0 : series == "战将" ? growth * 2.4 + 8.0 : series == "君王" ? growth * 2.3 + 7.0 : growth * 2.2 + 6.0;
						}
						else
						{
							major = series == "尊将" ? growth * 2.8 + 8.0 : series == "战将" ? growth * 2.7 + 7.0 : series == "君王" ? growth * 2.6 + 6.0 : growth * 2.5 + 5.0;
							minor = series == "尊将" ? growth * 2.3 + 5.0 : series == "战将" ? growth * 2.2 + 4.0 : series == "君王" ? growth * 2.1 + 3.0 : growth * 2.0 + 2.0;
						}
					}
					else
					{
						if (type == 1)
							major = breakout >= 96.0 ? growth * 2.0 + 4.0 : breakout >= 94.0 ? growth * 1.9 + 3.0 : breakout >= 92.0 ? growth * 1.8 + 2.0 : breakout >= 90.0 ? growth * 1.7 + 1.0 : growth * 1.6;
						else
						{
							major = breakout >= 96.0 ? growth * 2.4 + 4.0 : breakout >= 94.0 ? growth * 2.3 + 3.0 : breakout >= 92.0 ? growth * 2.2 + 2.0 : breakout >= 90.0 ? growth * 2.1 + 1.0 : growth * 2.0;
							minor = breakout >= 96.0 ? growth * 1.9 + 1.0 : breakout >= 94.0 ? growth * 1.8 : breakout >= 92.0 ? growth * 1.7 - 1.0 : breakout >= 90.0 ? growth * 1.6 - 2.0 : growth * 1.5 - 3.0;
						}
					}
					major = Math.Round((float)(major * (level - 1.0) / 98.0), MidpointRounding.ToEven);
					minor = Math.Round((float)(minor * (level - 1.0) / 98.0), MidpointRounding.ToEven);
					if (type < 1 || type > 4) throw new GeneralRuleException(GeneralFailure.InvalidData, "将领类型无效");
					final["武力"] = LegacyGenerals.Number(initial["武力"]) + (type == 1 || type == 2 ? major : minor) + LegacyGenerals.Number(points["武力分配点"]);
					final["智力"] = LegacyGenerals.Number(initial["智力"]) + (type == 1 || type == 3 ? major : minor) + LegacyGenerals.Number(points["智力分配点"]);
					double leadership = LegacyGenerals.Number(initial["统帅"]) + (type == 1 || type == 4 ? major : minor) + LegacyGenerals.Number(points["统帅分配点"]);
					if (LegacyGenerals.Number(final["统帅加成剩余时间"]) > utcSeconds)
						leadership = Math.Floor((float)leadership * (1f + (float)(LegacyGenerals.Number(final["统帅加成"]) / 100.0)));
					final["统帅"] = leadership;
					points["爆点数"] = major;
					final["体力上限"] = LegacyGenerals.Number(initial["体力上限"]) + level * 5.0;
					details["身份"] = LegacyGenerals.Number(basics["ID"]);
					final["生命值"] = 0.0;
					final["攻击"] = final["武力"].DeepClone();
					final["防御"] = final["智力"].DeepClone();
					final["统兵"] = 20.0 * (level - 1.0) + (leadership * 3.0 - 30.0);
					int generalId = LegacyGenerals.Integer(general["ID"]);
					for (int slot = 0; slot < 4; slot++)
					{
						JObject equipment = LegacyGenerals.WornEquipment(player, slot, generalId);
						if (equipment == null) continue;
						double value = LegacyGenerals.Number(LegacyGenerals.Object(equipment["装备信息"])["基础值"]) + LegacyGenerals.Number(equipment["强化值"]);
						string field = slot == 0 ? "生命值" : slot == 1 ? "攻击" : slot == 2 ? "防御" : "统兵";
						final[field] = LegacyGenerals.Number(final[field]) + value;
						foreach (JToken soulToken in LegacyGenerals.Array(equipment["炼魂属性"]))
						{
							JObject soul = LegacyGenerals.Object(soulToken);
							int soulType = LegacyGenerals.Integer(soul["类型"]);
							string soulField = soulType == 1 ? "攻击" : soulType == 2 ? "防御" : soulType == 3 ? "生命值" : soulType == 4 ? "统兵" : soulType == 5 ? "体力上限" : null;
							if (soulField != null) final[soulField] = LegacyGenerals.Number(final[soulField]) + LegacyGenerals.Number(soul["炼魂值"]);
						}
					}
					string title = basics.Value<string>("称号名");
					double maximumStamina = Math.Max(0, LegacyGenerals.Number(final["体力上限"]));
					details["剩余体力"] = previousMaximum <= 0 ? maximumStamina : Math.Min(maximumStamina, Math.Max(0, previousStamina));
					double titleBonus = title == "武圣" ? 20.0 : title == "霸主" || title == "天子" || title == "暴君" ? 10.0 : 0.0;
					final["统兵"] = Math.Floor((float)(LegacyGenerals.Number(final["统兵"]) * (1.0 + LegacyGenerals.Number(technology["统帅能力"]) * 5.0 / 100.0 + titleBonus / 100.0)));
					string[] limited = { "攻击", "防御", "生命值", "统兵" };
					double[] limits = { 1500.0, 1300.0, 3000.0, 13000.0 };
					for (int i = 0; i < limited.Length; i++)
						if (LegacyGenerals.Number(final[limited[i]]) > limits[i]) { final[limited[i]] = 100.0; abnormal = true; }
				}
			}
			return abnormal;
		}
	}
}
