using UnityEngine;

namespace 玩家数据结构
{
	public class 称号信息
	{
		public string 名字;

		public int 状态;

		public int 类型;

		public int 等级;

		public 称号信息(string str, int dj, int lx)
		{
			名字 = str;
			类型 = lx;
			等级 = dj;
			状态 = 1;
		}

		public Color 获取称号颜色()
		{
			if (状态 == 0)
			{
				return 颜色类.GetColor("#C8C8C8");
			}
			if (等级 == 1)
			{
				return 颜色类.GetColor("#00FF00");
			}
			if (等级 == 2)
			{
				return 颜色类.GetColor("#FDA400");
			}
			if (等级 == 3)
			{
				return 颜色类.GetColor("#F000ED");
			}
			if (等级 == 4)
			{
				return 颜色类.GetColor("#FF0000");
			}
			return 颜色类.GetColor("#C8C8C8");
		}
	}
}
