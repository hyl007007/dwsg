using System.Collections.Generic;

namespace 玩家数据结构
{
	public class 道具分类列表
	{
		public List<道具信息> 宝物道具列表 = new List<道具信息>();

		public List<道具信息> 加速道具列表 = new List<道具信息>();

		public List<道具信息> 生产道具列表 = new List<道具信息>();

		public List<道具信息> 宝箱道具列表 = new List<道具信息>();

		public List<道具信息> 强化道具列表 = new List<道具信息>();

		public List<道具信息> 任务道具列表 = new List<道具信息>();

		public void 添加道具(string 名字, int 数量)
		{
			List<道具信息> list = 获取道具分类列表(名字);
			if (list == null)
			{
				return;
			}
			int num = 获取指定道具最小数量的索引(list, 名字);
			if (num != -1)
			{
				if (list[num].数量 + (double)数量 > 999.0)
				{
					double num2 = 999.0 - list[num].数量;
					list.Add(new 道具信息(名字, (double)数量 - num2));
					list[num].数量 = 999.0;
				}
				else
				{
					list[num].数量 = list[num].数量 + (double)数量;
				}
			}
			else
			{
				list.Add(new 道具信息(名字, 数量));
			}
		}

		public bool 扣除道具(string 道具名字, int 数量)
		{
			if (数量 <= 0 || 获取指定道具数量(道具名字) < 数量) return false;
			List<道具信息> 列表 = 获取道具分类列表(道具名字);
			if (列表 == null) return false;
			int 剩余 = 数量;
			for (int i = 列表.Count - 1; i >= 0 && 剩余 > 0; i--)
			{
				if (列表[i].名字 != 道具名字) continue;
				int 扣除 = System.Math.Min(剩余, (int)列表[i].数量);
				列表[i].数量 -= 扣除;
				剩余 -= 扣除;
				if (列表[i].数量 <= 0) 列表.RemoveAt(i);
			}
			return 剩余 == 0;
		}

		public bool 删除道具(string 道具名字)
		{
			List<道具信息> list = 获取道具分类列表(道具名字);
			if (list != null)
			{
				int count = list.Count;
				for (int i = 0; i < count; i++)
				{
					if (list[i].名字 == 道具名字)
					{
						list.RemoveAt(i);
						return true;
					}
				}
			}
			return false;
		}

		public string 批量使用道具(string 道具名字, int 使用数量, int 第几个封地, int 第几个将领)
		{
			string result = "";
			for (int i = 0; i < 使用数量; i++)
			{
				result = 使用道具(道具名字, 第几个封地, 第几个将领);
			}
			return result;
		}

		public string 使用道具(string 道具名字, int 第几个封地, int 第几个将领)
		{
			string text = "使用失败";
			List<道具信息> list = 获取道具分类列表(道具名字);
			if (list != null)
			{
				int num = 获取指定道具最小数量的索引(list, 道具名字);
				if (num != -1)
				{
					if (list[num].数量 > 0.0)
					{
						text = 全局道具库.使用道具(道具名字, 第几个封地, 第几个将领);
						if (text != "使用失败")
						{
							list[num].数量 -= 1.0;
						}
					}
					if (list[num].数量 <= 0.0)
					{
						list.RemoveAt(num);
					}
				}
			}
			return text;
		}

		public int 获取指定道具最小数量的索引(List<道具信息> 列表, string 道具名字)
		{
			double num = 2000.0;
			int result = -1;
			int count = 列表.Count;
			for (int i = 0; i < count; i++)
			{
				if (列表[i].名字 == 道具名字 && 列表[i].数量 < num)
				{
					num = 列表[i].数量;
					result = i;
				}
			}
			return result;
		}

		public int 获取指定道具的索引(List<道具信息> 列表, string 道具名字)
		{
			int count = 列表.Count;
			for (int i = 0; i < count; i++)
			{
				if (列表[i].名字 == 道具名字)
				{
					return i;
				}
			}
			return -1;
		}

		public double 获取指定道具数量(string 道具名字)
		{
			double num = 0.0;
			List<道具信息> list = 获取道具分类列表(道具名字);
			if (list != null)
			{
				int count = list.Count;
				for (int i = 0; i < count; i++)
				{
					if (list[i].名字 == 道具名字)
					{
						num += list[i].数量;
					}
				}
			}
			return num;
		}

		public List<道具信息> 获取道具分类列表(string 道具名字)
		{
			string 分类 = 获取道具分类(道具名字);
			return 获取指定分类列表(分类);
		}

		public string 获取道具分类(string 道具名字)
		{
			道具信息库类 道具信息库类 = 全局道具库.获取指定名字的道具(道具名字);
			if (道具信息库类 != null)
			{
				return 道具信息库类.分类;
			}
			return "未知";
		}

		public List<道具信息> 获取指定分类列表(string 分类)
		{
			if (分类 == "宝物")
			{
				return 宝物道具列表;
			}
			if (分类 == "加速")
			{
				return 加速道具列表;
			}
			if (分类 == "生产")
			{
				return 生产道具列表;
			}
			if (分类 == "宝箱")
			{
				return 宝箱道具列表;
			}
			if (分类 == "强化")
			{
				return 强化道具列表;
			}
			if (分类 == "任务")
			{
				return 任务道具列表;
			}
			return null;
		}
	}
}
