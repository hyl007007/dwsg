using System.Collections.Generic;
using UnityEngine;

namespace 玩家数据结构
{
	public class 将领装备
	{
		public int 将领ID = -1;

		public double 强化等级;

		public double 品质 = 1.0;

		public double 强化值;

		public double 保底次数;

		public double 已强化次数;

		public 装备属性库类 装备信息 = new 装备属性库类("空", "", 0.0, 0.0);

		public List<炼魂属性> 炼魂属性 = new List<炼魂属性>();

		public void 生成指定装备(string 装备名字)
		{
			装备信息 = 全局装备库.获取指定名字的装备(装备名字);
		}

		public Sprite 获取装备头像()
		{
			int num = 全局装备库.查询指定装备的图片(装备信息.名称);
			if (num != -1)
			{
				return 全局变量.所有装备资源表[num];
			}
			return 全局变量.装备初始图片[0];
		}

		public string 获取装备名字()
		{
			return 装备信息.名称;
		}

		public double 获取装备等级()
		{
			return 装备信息.等级;
		}

		public string 获取装备简单信息文本()
		{
			return 获取装备名字() + "+" + 强化等级.ToString() + "(" + 获取装备等级().ToString() + "级" + 获取装备品质名称() + ")";
		}

		public Color 获取装备文字颜色()
		{
			if (品质 == 2.0)
			{
				return 颜色类.GetColor("#28BEE6");
			}
			if (品质 == 3.0)
			{
				return 颜色类.GetColor("#00FF00");
			}
			if (品质 == 4.0)
			{
				return 颜色类.GetColor("#FFFF00 ");
			}
			return 颜色类.GetColor("#C8C8C8");
		}

		public double 获取装备加成数字()
		{
			return 装备信息.基础值 + 强化值;
		}

		public string 获取装备加成文本()
		{
			double 基础值 = 装备信息.基础值;
			string result = "";
			if (装备信息.类型 == "头盔")
			{
				result = "增加统帅部队" + (基础值 + 强化值).ToString() + "点生命值";
			}
			else if (装备信息.类型 == "武器")
			{
				result = "增加将领" + (基础值 + 强化值).ToString() + "点攻击力";
			}
			else if (装备信息.类型 == "铠甲")
			{
				result = "增加将领" + (基础值 + 强化值).ToString() + "点防御力";
			}
			else if (装备信息.类型 == "坐骑")
			{
				result = "增加将领" + (基础值 + 强化值).ToString() + "个统兵数";
			}
			return result;
		}

		public string 获取装备强化效果文本()
		{
			if (装备信息.类型 == "头盔")
			{
				return "生命";
			}
			if (装备信息.类型 == "武器")
			{
				return "攻击";
			}
			if (装备信息.类型 == "铠甲")
			{
				return "防御";
			}
			if (装备信息.类型 == "坐骑")
			{
				return "统兵";
			}
			return "未知";
		}

		public int 获取装备强化加成()
		{
			return Dwsg.Shared.Generals.EquipmentEnhancementRules.Bonus(装备信息.类型, 品质);
		}

		public int 获取装备强化保底次数()
		{
			return Dwsg.Shared.Generals.EquipmentEnhancementRules.Pity(强化等级);
		}

		public double 获取装备强化成功率()
		{
			return Dwsg.Shared.Generals.EquipmentEnhancementRules.SuccessRate;
		}

		public string 获取装备品质名称()
		{
			return new List<string>
			{
				"未知",
				"普通",
				"良好",
				"优秀",
				"卓越"
			}[(int)品质];
		}

		public string 获取装备品质文本()
		{
			string text = "";
			for (int i = 0; (double)i < 品质; i++)
			{
				text += "★";
			}
			return "品质:" + text + "(" + 获取装备品质名称() + ")";
		}

		public string 获取装属性说明文本()
		{
			double 基础值 = 装备信息.基础值;
			string str = 装备信息.名称 + "+" + 强化等级.ToString() + "（ " + 装备信息.等级.ToString() + "级 ）\n" + 获取装备品质文本() + "\n";
			string str2 = 获取装备加成文本();
			string str3 = "(基础值" + 基础值.ToString() + "+强化值" + 强化值.ToString() + ")\n";
			string text = "";
			int count = 炼魂属性.Count;
			for (int i = 0; i < count; i++)
			{
				text = text + 炼魂属性[i].获取炼魂加成文本() + "\n";
			}
			return str + str2 + str3 + text;
		}

		public string 获取装备强化材料名字()
		{
			return Dwsg.Shared.Generals.EquipmentEnhancementRules.Material(装备信息.类型, 品质);
		}

		public double 获取炼魂加成上限(double 类型)
		{
			if (类型 == 1.0)
			{
				if (装备信息.类型 == "武器")
				{
					return 60.0;
				}
				return 20.0;
			}
			if (类型 == 2.0)
			{
				if (装备信息.类型 == "铠甲")
				{
					return 30.0;
				}
				return 10.0;
			}
			if (类型 == 3.0)
			{
				if (装备信息.类型 == "头盔")
				{
					return 60.0;
				}
				return 20.0;
			}
			if (类型 == 4.0)
			{
				if (装备信息.类型 == "坐骑")
				{
					return 60.0;
				}
				return 20.0;
			}
			if (类型 == 4.0)
			{
				return 50.0;
			}
			return 1.0;
		}

		public string 获取装备炼魂材料名字()
		{
			if (装备信息.类型 == "坐骑")
			{
				return "山海精华";
			}
			if (装备信息.类型 == "武器")
			{
				return "昆仑玄铁";
			}
			if (装备信息.类型 == "头盔")
			{
				return "天魂灵石";
			}
			if (装备信息.类型 == "铠甲")
			{
				return "地魄灵石";
			}
			return "未知";
		}

		public double 获取装备强化消耗数量()
		{
			return 强化等级 + 1.0;
		}

		public void 强化1级装备()
		{
			强化等级 += 1.0;
			强化值 = 强化等级 * (double)获取装备强化加成();
			已强化次数 = 0.0;
		}

		public void 强化满级装备()
		{
			强化等级 = 30.0;
			强化值 = 强化等级 * (double)获取装备强化加成();
			已强化次数 = 0.0;
		}

		public void 装备炼魂全满()
		{
			if (装备信息.类型 == "坐骑")
			{
				for (int i = 0; i < 6; i++)
				{
					炼魂属性 炼魂属性 = new 炼魂属性();
					炼魂属性.类型 = 4.0;
					炼魂属性.炼魂值 = 60.0;
					this.炼魂属性.Add(炼魂属性);
				}
			}
			else if (装备信息.类型 == "武器")
			{
				for (int j = 0; j < 6; j++)
				{
					炼魂属性 炼魂属性2 = new 炼魂属性();
					炼魂属性2.类型 = 1.0;
					炼魂属性2.炼魂值 = 60.0;
					this.炼魂属性.Add(炼魂属性2);
				}
			}
			else if (装备信息.类型 == "头盔")
			{
				for (int k = 0; k < 6; k++)
				{
					炼魂属性 炼魂属性3 = new 炼魂属性();
					炼魂属性3.类型 = 3.0;
					炼魂属性3.炼魂值 = 60.0;
					this.炼魂属性.Add(炼魂属性3);
				}
			}
			else if (装备信息.类型 == "铠甲")
			{
				for (int l = 0; l < 6; l++)
				{
					炼魂属性 炼魂属性4 = new 炼魂属性();
					炼魂属性4.类型 = 2.0;
					炼魂属性4.炼魂值 = 50.0;
					this.炼魂属性.Add(炼魂属性4);
				}
			}
		}

		public double 获取总属性()
		{
			return 装备信息.基础值 + 强化值;
		}
	}
}
