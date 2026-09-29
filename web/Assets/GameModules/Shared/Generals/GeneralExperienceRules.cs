using System;
using Newtonsoft.Json.Linq;

namespace Dwsg.Shared.Generals
{
	public static class GeneralExperienceRules
	{
		public static double RequiredForLevel(double level) { return 15.0 * level * level * level; }
		public static double TotalForLevel(double level)
		{
			double total = 0.0;
			for (int i = 0; (double)i < level; i++) total += (double)(15 * i * i * i);
			return total;
		}

		public static void Add(JObject general, double experience)
		{
			if (double.IsNaN(experience) || double.IsInfinity(experience) || experience < 0.0)
				throw new GeneralRuleException(GeneralFailure.InvalidArgument, "经验值无效");
			JObject points = LegacyGenerals.Object(LegacyGenerals.Object(general["将领属性"])["成长点数"]);
			JObject details = LegacyGenerals.Object(general["详细信息"]);
			double level = LegacyGenerals.Number(points["等级"]);
			double current = LegacyGenerals.Number(details["经验"]);
			double assigned = LegacyGenerals.Number(points["总分配点数"]);
			if (level < 1.0 || level > 99.0 || level != Math.Truncate(level) || current < 0.0)
				throw new GeneralRuleException(GeneralFailure.InvalidData, "将领经验数据无效");
			double remaining = current + experience;
			if (double.IsInfinity(remaining))
				throw new GeneralRuleException(GeneralFailure.InvalidArgument, "经验值无效");
			while (level < 99.0 && remaining >= RequiredForLevel(level))
			{
				// 已有经验与本次获得的经验统一结算，每级只扣除一次。
				remaining -= RequiredForLevel(level);
				level += 1.0;
				assigned += 1.0;
			}
			points["等级"] = level;
			points["总分配点数"] = assigned;
			details["升级需要经验"] = RequiredForLevel(level);
			details["经验"] = Math.Min(remaining, RequiredForLevel(level));
		}
	}
}
