using System;
using Newtonsoft.Json.Linq;

namespace Dwsg.Shared.Generals
{
	public static class GeneralCreationRules
	{
		public static bool IsOrdinarySeries(string series)
		{
			return series == "普通" || series == "良好" || series == "优秀" || series == "卓越";
		}

		public static JObject RandomizeOrdinary(JObject configuration, Func<int, int, int> random)
		{
			if (configuration == null || random == null) throw new ArgumentNullException();
			string series = configuration.Value<string>("系列");
			if (!IsOrdinarySeries(series)) throw new GeneralRuleException(GeneralFailure.InvalidArgument, "此随机规则仅用于原四种普通系列");
			JObject result = (JObject)configuration.DeepClone();
			int type = random(1, 5);
			result["类型"] = (double)type;
			result["职业"] = random(1, 5);
			int growth = random(series == "普通" ? 40 : series == "良好" ? 60 : series == "优秀" ? 70 : 80,
				series == "普通" ? 60 : series == "良好" ? 70 : series == "优秀" ? 80 : 90);
			result["成长"] = (double)growth;
			result["突围"] = series == "卓越" ? 90.0 : random(series == "普通" ? 60 : series == "良好" ? 70 : 80, series == "普通" ? 71 : series == "良好" ? 81 : 89);
			int low = series == "优秀" ? (growth >= 85 ? 90 : growth >= 80 ? 85 : growth >= 75 ? 80 : growth >= 70 ? 75 : 70) : series == "卓越" ? 80 : 60;
			if (type == 1)
			{
				int value = random(series == "普通" || series == "良好" ? 70 : low, series == "普通" ? 81 : series == "良好" ? 91 : series == "优秀" ? 96 : low + 20);
				result["武力"] = result["智力"] = result["统帅"] = (double)value;
			}
			else
			{
				string[] fields = { "武力", "智力", "统帅" };
				for (int i = 0; i < fields.Length; i++)
				{
					bool major = type == i + 2;
					int min = series == "普通" || series == "良好" ? (major ? 70 : 60) : (major ? low + 10 : low);
					int max = series == "普通" || series == "良好" ? (major ? 81 : 71) : (major ? low + 20 : low + 10);
					result[fields[i]] = (double)random(min, max);
				}
			}
			return result;
		}

		public static JObject CreateBanditGeneral(JObject configuration, Func<int, int, int> random)
		{
			return Create(RandomizeOrdinary(configuration, random));
		}

		public static JObject Create(JObject configuration)
		{
			JObject initial = new JObject();
			foreach (string field in new[] { "ID", "名字", "头像特效", "职业", "类型", "成长", "突围", "武力", "智力", "统帅", "系列" })
			{
				if (configuration[field] == null) throw new GeneralRuleException(GeneralFailure.InvalidData, "原将领配置不完整");
				initial[field] = configuration[field].DeepClone();
			}
			initial["体力上限"] = 100.0;
			JObject points = new JObject { ["等级"] = 1.0 };
			foreach (string field in new[] { "爆点数", "副属性爆点", "总分配点数", "武力分配点", "智力分配点", "统帅分配点" }) points[field] = 0.0;
			JObject final = new JObject();
			foreach (string field in new[] { "武力", "攻击", "智力", "防御", "统帅", "统兵", "生命值", "体力上限", "统帅加成" }) final[field] = 0.0;
			final["统帅加成剩余时间"] = 0L;
			JObject details = new JObject { ["忠诚"] = 61.0, ["所属封地"] = 1.0, ["升级需要经验"] = 15.0, ["攻击模式"] = 1.0, ["将领叛逃计时器"] = 60.0 };
			foreach (string field in new[] { "经验", "俸禄", "剩余体力", "剩余兵力", "状态", "俘虏玩家", "身份", "坑位颜色", "编队" }) details[field] = 0.0;
			return new JObject {
				["ID"] = 0,
				["将领属性"] = new JObject { ["初始属性"] = initial, ["成长点数"] = points, ["最终属性"] = final },
				["详细信息"] = details,
				["将领配兵"] = new JObject { ["ID"] = 0.0, ["数量"] = 0.0 },
				["将领装备表"] = new JArray(),
				["将领培养"] = new JObject { ["保底上限"] = 50.0, ["保底次数"] = 0.0, ["培养次数"] = 1.0 }
			};
		}
	}
}
