using System.Collections.Generic;

namespace 玩家数据结构
{
	public class 装备分类列表
	{
		public List<将领装备> 武器装备列表 = new List<将领装备>();

		public List<将领装备> 头盔装备列表 = new List<将领装备>();

		public List<将领装备> 铠甲装备列表 = new List<将领装备>();

		public List<将领装备> 坐骑装备列表 = new List<将领装备>();

		public List<将领装备> 获取指定部位列表(int 装备部位)
		{
			List<将领装备> result = null;
			switch (装备部位)
			{
			case 0:
				result = 头盔装备列表;
				break;
			case 1:
				result = 武器装备列表;
				break;
			case 2:
				result = 铠甲装备列表;
				break;
			case 3:
				result = 坐骑装备列表;
				break;
			}
			return result;
		}

		public 将领装备 寻找指定将领的装备(int 装备部位, int 将领ID)
		{
			List<将领装备> list = 获取指定部位列表(装备部位);
			int count = list.Count;
			for (int i = 0; i < count; i++)
			{
				if (list[i].将领ID == 将领ID)
				{
					return list[i];
				}
			}
			return null;
		}

		public 将领装备 获取最高属性装备(int 装备部位, double 已穿的装备加成, double 将领等级)
		{
			List<将领装备> list = 获取指定部位列表(装备部位);
			将领装备 result = null;
			double num = 已穿的装备加成;
			int count = list.Count;
			for (int i = 0; i < count; i++)
			{
				if (将领等级 >= list[i].获取装备等级() && list[i].将领ID == -1)
				{
					double num2 = list[i].获取装备加成数字();
					if (num2 > num)
					{
						result = list[i];
						num = num2;
					}
				}
			}
			return result;
		}

		public bool 删除装备(将领装备 装备信息, int 索引)
		{
			if (装备信息.将领ID != -1)
			{
				return false;
			}
			if (装备信息.装备信息.类型 == "头盔")
			{
				头盔装备列表.RemoveAt(索引);
			}
			else if (装备信息.装备信息.类型 == "武器")
			{
				武器装备列表.RemoveAt(索引);
			}
			else if (装备信息.装备信息.类型 == "铠甲")
			{
				铠甲装备列表.RemoveAt(索引);
			}
			else if (装备信息.装备信息.类型 == "坐骑")
			{
				坐骑装备列表.RemoveAt(索引);
			}
			return true;
		}

		public void 添加指定名字的装备(string 名字, double 品质2)
		{
			将领装备 将领装备 = new 将领装备();
			装备属性库类 装备属性库类 = 全局装备库.获取指定名字的装备(名字);
			将领装备.品质 = 品质2;
			将领装备.装备信息 = 装备属性库类;
			if (装备属性库类.类型 == "头盔")
			{
				头盔装备列表.Add(将领装备);
			}
			else if (装备属性库类.类型 == "武器")
			{
				武器装备列表.Add(将领装备);
			}
			else if (装备属性库类.类型 == "铠甲")
			{
				铠甲装备列表.Add(将领装备);
			}
			else if (装备属性库类.类型 == "坐骑")
			{
				坐骑装备列表.Add(将领装备);
			}
		}

		public void 添加指定装备数据到背包(装备属性库类 装备信息, double 品质2)
		{
			将领装备 将领装备 = new 将领装备();
			将领装备.品质 = 品质2;
			将领装备.装备信息 = 装备信息;
			if (装备信息.类型 == "头盔")
			{
				头盔装备列表.Add(将领装备);
			}
			else if (装备信息.类型 == "武器")
			{
				武器装备列表.Add(将领装备);
			}
			else if (装备信息.类型 == "铠甲")
			{
				铠甲装备列表.Add(将领装备);
			}
			else if (装备信息.类型 == "坐骑")
			{
				坐骑装备列表.Add(将领装备);
			}
		}
	}
}
