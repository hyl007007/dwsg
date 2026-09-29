using System.Linq;
using Dwsg.Shared.Economy;
using Newtonsoft.Json.Linq;

namespace Dwsg.Shared.Generals
{
	public static class GeneralExperienceBookRules
	{
		public const string ItemName = "太平要术";
		public const double ExperiencePerBook = 5000000.0;

		public static JObject Execute(JObject player, int generalId, string itemName, int count,
			JArray itemDefinitions, long utcSeconds, out GameResult result)
		{
			if (count <= 0) throw new GeneralRuleException(GeneralFailure.InvalidArgument, "使用数量必须为正整数");
			if (itemName != ItemName) throw new GeneralRuleException(GeneralFailure.InvalidArgument, "请选择原经验书");
			JObject definition = itemDefinitions?.OfType<JObject>().SingleOrDefault(item => item.Value<string>("名字") == itemName);
			if (definition == null || definition.Value<string>("类型") != "经验书" || definition.Value<string>("分类") != "宝物")
				throw new GeneralRuleException(GeneralFailure.InvalidData, "原经验书配置缺失");
			JObject candidate = (JObject)player.DeepClone(), fief;
			JObject general = LegacyGenerals.General(candidate, generalId, out fief);
			int consumed = 0;
			for (int i = 0; i < count; i++)
			{
				// 原书效果按当前经验判断，99级但经验未满仍能使用；不限制状态或军队占用。
				if (LegacyGenerals.Number(general["详细信息"]["经验"]) >= GeneralExperienceRules.RequiredForLevel(99.0)) break;
				GameResult cost = InventoryRules.ConsumeOne(candidate, itemDefinitions, itemName);
				if (cost.Code != GameCodes.Ok)
				{
					if (cost.Code != GameCodes.Conflict) throw new GeneralRuleException(GeneralFailure.InvalidData, cost.Message);
					break;
				}
				GeneralExperienceRules.Add(general, ExperiencePerBook);
				consumed++;
			}
			if (consumed == 0) throw new GeneralRuleException(GeneralFailure.Conflict, "使用失败");
			if (GeneralAttributeRules.Recalculate(candidate, utcSeconds)) throw new GeneralRuleException(GeneralFailure.InvalidData, "属性数据异常");
			result = GameResult.Success(new JObject { ["itemName"] = itemName, ["requestedCount"] = count, ["consumedCount"] = consumed });
			result.Message = consumed == count ? "将领获得5000000点经验" : "已使用" + consumed + "本经验书，后续使用失败";
			return candidate;
		}
	}
}
