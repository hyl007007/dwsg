using System;
using Dwsg.Shared.Economy;
using Newtonsoft.Json.Linq;

namespace Dwsg.Shared.Generals
{
	public static class GeneralCultivationRules
	{
		public static JObject Execute(JObject player, int generalId, int count, JArray itemDefinitions,
			Func<int, int, int> random, long utcSeconds, out GameResult result)
		{
			if (count < 1 || count > 100) throw new GeneralRuleException(GeneralFailure.InvalidArgument, "培养次数必须为1至100的整数");
			JObject candidate = (JObject)player.DeepClone();
			JObject fief;
			JObject general = LegacyGenerals.General(candidate, generalId, out fief);
			// 原培养按钮与将神魂道具均不限制将领状态或等级。
			JObject initial = LegacyGenerals.Object(general["将领属性"]["初始属性"]);
			JObject cultivation = LegacyGenerals.Object(general["将领培养"]);
			double growth = LegacyGenerals.Number(initial["成长"]);
			int failures = LegacyGenerals.Quantity(cultivation["保底次数"]);
			int ceiling = LegacyGenerals.Quantity(cultivation["保底上限"]);
			if (growth < 0 || growth > 99 || ceiling < 1 || random == null)
				throw new GeneralRuleException(GeneralFailure.InvalidData, "将领培养数据无效");
			int consumed = 0, increases = 0, status = 0;
			for (int i = 0; i < count; i++)
			{
				if (growth >= 99) { status = 3; break; }
				GameResult cost = InventoryRules.ConsumeOne(candidate, itemDefinitions, "将神魂");
				if (cost.Code != GameCodes.Ok)
				{
					if (cost.Code != GameCodes.Conflict) throw new GeneralRuleException(GeneralFailure.InvalidData, cost.Message);
					status = 2; break;
				}
				int roll = random(0, 1001);
				if (roll < 0 || roll > 1000) throw new GeneralRuleException(GeneralFailure.InvalidData, "培养随机结果超出原范围");
				consumed++;
				if (roll <= 10 || failures >= ceiling)
				{
					growth += 1;
					failures = 0;
					ceiling = growth >= 95 ? 200 : growth >= 90 ? 150 : growth >= 85 ? 100 : growth >= 80 ? 50 : 10;
					increases++; status = 1;
				}
				else
				{
					if (failures == int.MaxValue) throw new GeneralRuleException(GeneralFailure.InvalidData, "将领保底次数超出范围");
					failures++;
				}
			}
			string message = status == 1 ? "培养成功!" : status == 2 ? "缺少将神魂!" : status == 3 ? "培养已上限!" : "培养失败!";
			if (consumed == 0) throw new GeneralRuleException(GeneralFailure.Conflict, message);
			initial["成长"] = growth;
			cultivation["保底次数"] = (double)failures;
			cultivation["保底上限"] = (double)ceiling;
			cultivation["培养次数"] = (double)count;
			if (GeneralAttributeRules.Recalculate(candidate, utcSeconds)) throw new GeneralRuleException(GeneralFailure.InvalidData, "属性数据异常");
			result = GameResult.Success(new JObject { ["requestedCount"] = count, ["consumedCount"] = consumed,
				["growthIncreases"] = increases, ["growth"] = growth, ["status"] = status });
			result.Message = message;
			return candidate;
		}
	}
}
