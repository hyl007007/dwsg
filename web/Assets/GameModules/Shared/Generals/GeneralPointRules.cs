using Newtonsoft.Json.Linq;

namespace Dwsg.Shared.Generals
{
	public static class GeneralPointRules
	{
		public static void Allocate(JObject general, string attribute, int? requestedCount)
		{
			if (attribute != "武力" && attribute != "智力" && attribute != "统帅")
				throw new GeneralRuleException(GeneralFailure.InvalidArgument, "请选择武力、智力或统帅");
			JObject points = LegacyGenerals.Object(general["将领属性"]["成长点数"]);
			int budget = Level(points) - 1;
			int available = LegacyGenerals.Quantity(points["总分配点数"]);
			long assigned = (long)LegacyGenerals.Quantity(points["武力分配点"]) + LegacyGenerals.Quantity(points["智力分配点"]) + LegacyGenerals.Quantity(points["统帅分配点"]);
			if (assigned + available > budget) throw new GeneralRuleException(GeneralFailure.InvalidData, "将领分配点数超出等级上限");
			int count = requestedCount ?? available;
			if (count <= 0) throw new GeneralRuleException(GeneralFailure.InvalidArgument, "没有可分配点数或分配数量无效");
			if (count > available) throw new GeneralRuleException(GeneralFailure.Conflict, "可分配点数不足");
			string field = attribute + "分配点";
			points[field] = (double)(LegacyGenerals.Quantity(points[field]) + count);
			points["总分配点数"] = (double)(available - count);
		}

		public static void Reset(JObject general)
		{
			JObject points = LegacyGenerals.Object(general["将领属性"]["成长点数"]);
			int level = Level(points);
			points["武力分配点"] = 0.0;
			points["智力分配点"] = 0.0;
			points["统帅分配点"] = 0.0;
			points["总分配点数"] = (double)(level - 1);
		}

		static int Level(JObject points)
		{
			int level = LegacyGenerals.Integer(points["等级"]);
			if (level < 1 || level > 99) throw new GeneralRuleException(GeneralFailure.InvalidData, "将领等级无效");
			return level;
		}
	}
}
