using System.Collections.Generic;
using UnityEngine;

namespace 玩家数据结构
{
	public class 初始属性
	{
		public double ID;

		public string 名字 = "";

		public double 头像特效;

		public int 职业;

		public double 类型;

		public double 成长;

		public double 突围;

		public double 武力;

		public double 智力;

		public double 统帅;

		public double 体力上限;

		public string 系列;

		public Color 获取将领名字颜色()
		{
			if (成长 >= 95.0)
			{
				return 颜色类.GetColor("#F07800");
			}
			if (成长 >= 90.0)
			{
				return 颜色类.GetColor("#F000ED");
			}
			if (成长 >= 85.0)
			{
				return 颜色类.GetColor("#F00000");
			}
			if (成长 >= 80.0)
			{
				return 颜色类.GetColor("#F0DE00");
			}
			return 颜色类.GetColor("#C8C8C8");
		}

		public Color 获取酒馆将领名字颜色()
		{
			if (成长 >= 95.0)
			{
				return 颜色类.GetColor("#F07800");
			}
			if (成长 >= 90.0)
			{
				return 颜色类.GetColor("#F000ED");
			}
			if (成长 >= 85.0)
			{
				return 颜色类.GetColor("#F00000");
			}
			if (成长 >= 80.0)
			{
				return 颜色类.GetColor("#F0DE00");
			}
			if (成长 >= 70.0)
			{
				return 颜色类.GetColor("#FF9497");
			}
			if (成长 >= 60.0)
			{
				return 颜色类.GetColor("#00EFEF");
			}
			return 颜色类.GetColor("#78FABE");
		}

		public string 获取将领品质名称()
		{
			if (成长 < 60.0)
			{
				return "普通";
			}
			if (成长 < 70.0)
			{
				return "良好";
			}
			if (成长 < 80.0)
			{
				return "优秀";
			}
			if (成长 < 85.0)
			{
				return "卓越";
			}
			if (成长 < 90.0)
			{
				return "极品";
			}
			return "稀有";
		}

		public string 获取职业名字()
		{
			return new List<string>
			{
				"骑将",
				"步将",
				"勇将",
				"弓将"
			}[职业 - 1];
		}
	}
}
