using System;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Dwsg.Shared.Generals
{
	public static class TavernRules
	{
		public static JArray Generate(JArray configurations, int refreshType, Func<int, int, int> random, Func<string> name)
		{
			if (refreshType < 0 || refreshType > 3) throw new GeneralRuleException(GeneralFailure.InvalidArgument, "酒馆刷新类型无效");
			int type = refreshType == 0 ? 1 : refreshType;
			JArray result = new JArray();
			for (int i = 0; i < 5; i++)
			{
				random(0, 1000); // 原君王分支阈值为0，保留该次随机消费，原分支不可达。
				int templateId = random(1, type == 3 ? 9 : type == 2 ? 7 : 5);
				JObject template = configurations.OfType<JObject>().FirstOrDefault(c => LegacyGenerals.Integer(c["ID"]) == templateId);
				if (template == null) throw new GeneralRuleException(GeneralFailure.InvalidData, "原酒馆将领配置缺失");
				JObject general = GeneralCreationRules.CreateBanditGeneral(template, random);
				general["将领属性"]["初始属性"]["名字"] = name();
				result.Add(general);
			}
			return result;
		}

		public static JObject AddGeneral(JObject player, int fiefId, JObject general, long utcSeconds, bool enforceLimit = true)
		{
			JObject candidate = (JObject)player.DeepClone();
			JArray fiefs = LegacyGenerals.Array(candidate["封地信息表"]);
			JObject selected = null;
			int count = 0;
			int nextId = LegacyGenerals.Integer(candidate["将领ID标识"]);
			foreach (JObject fief in fiefs)
			{
				if (LegacyGenerals.Integer(fief["ID"]) == fiefId)
				{
					if (selected != null) throw new GeneralRuleException(GeneralFailure.InvalidData, "封地编号重复");
					selected = fief;
				}
				foreach (JObject existing in LegacyGenerals.Array(fief["将领信息表"]))
				{
					count++;
					if (LegacyGenerals.Integer(existing["ID"]) == nextId) throw new GeneralRuleException(GeneralFailure.InvalidData, "将领分配编号重复");
				}
			}
			if (selected == null) throw new GeneralRuleException(GeneralFailure.NotFound, "封地不存在或不属于当前角色");
			if (enforceLimit && count >= LegacyGenerals.Number(LegacyGenerals.Object(candidate["基础信息"])["将领数上限"]))
				throw new GeneralRuleException(GeneralFailure.Conflict, "招募失败,将领上限!");
			if (nextId < 0 || nextId == int.MaxValue) throw new GeneralRuleException(GeneralFailure.InvalidData, "将领分配编号无效");
			JObject added = (JObject)general.DeepClone();
			added["ID"] = nextId;
			LegacyGenerals.Array(selected["将领信息表"]).Add(added);
			candidate["将领ID标识"] = nextId + 1;
			if (GeneralAttributeRules.Recalculate(candidate, utcSeconds)) throw new GeneralRuleException(GeneralFailure.InvalidData, "属性数据异常");
			return candidate;
		}
	}
}
