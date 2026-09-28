using UnityEngine;

namespace 玩家数据结构
{
	public class 炼魂属性
	{
		public double 类型;

		public double 炼魂值;

		public bool 锁定;

		public string 获取炼魂加成文本()
		{
			if (类型 == 1.0)
			{
				if (炼魂值 > 0.0)
				{
					return "增加将领" + 炼魂值.ToString() + "点攻击力";
				}
				return "减少将领" + Mathf.Abs((float)炼魂值).ToString() + "点攻击力";
			}
			if (类型 == 2.0)
			{
				if (炼魂值 > 0.0)
				{
					return "增加将领" + 炼魂值.ToString() + "点防御力";
				}
				return "减少将领" + Mathf.Abs((float)炼魂值).ToString() + "点防御力";
			}
			if (类型 == 3.0)
			{
				if (炼魂值 > 0.0)
				{
					return "增加统帅部队" + 炼魂值.ToString() + "点生命值";
				}
				return "减少统帅部队" + Mathf.Abs((float)炼魂值).ToString() + "点生命值";
			}
			if (类型 == 4.0)
			{
				if (炼魂值 > 0.0)
				{
					return "增加将领" + 炼魂值.ToString() + "个统兵数";
				}
				return "减少将领" + Mathf.Abs((float)炼魂值).ToString() + "个统兵数";
			}
			if (类型 == 5.0)
			{
				if (炼魂值 > 0.0)
				{
					return "增加将领" + 炼魂值.ToString() + "点体力上限";
				}
				return "减少将领" + Mathf.Abs((float)炼魂值).ToString() + "点体力上限";
			}
			return "未知属性" + 炼魂值.ToString();
		}
	}
}
