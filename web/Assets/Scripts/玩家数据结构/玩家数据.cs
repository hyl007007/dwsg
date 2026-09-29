using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace 玩家数据结构
{
	public class 玩家数据
	{
		public 基础信息 基础信息 = new 基础信息();

		public 财产信息 财产信息 = new 财产信息();

		public 科技信息 科技信息 = new 科技信息();

		public List<称号信息> 称号信息表 = new List<称号信息>();

		public List<状态信息> 道具状态表 = new List<状态信息>();

		public List<封地信息> 封地信息表 = new List<封地信息>();

		public 道具分类列表 背包道具列表 = new 道具分类列表();

		public 装备分类列表 背包装备列表 = new 装备分类列表();

		public List<List<int>> 编队信息表 = new List<List<int>>();

		public int 将领ID标识 = 1;

		public int 封地ID标识 = 1;

		public void 初始化一个玩家(string 玩家名, string 国家名)
		{
			基础信息.ID = 全局变量.所有玩家数据表.Count;
			基础信息.名字 = 玩家名;
			基础信息.性别 = "男";
			基础信息.头像 = 1;
			基础信息.等级 = 1f;
			基础信息.称号名 = "无";
			基础信息.贡献 = 0.0;
			基础信息.官阶 = "平民";
			基础信息.战功 = 1.0;
			基础信息.声望 = 1.0;
			基础信息.国家 = 国家名;
			财产信息.铜钱 = 99999.0;
			财产信息.粮食 = 99999.0;
			财产信息.白银 = 99999.0;
			财产信息.黄金 = 99999.0;
			编队信息表.Add(new List<int>
			{
				-1,
				-1,
				-1,
				-1,
				-1
			});
			编队信息表.Add(new List<int>
			{
				-1,
				-1,
				-1,
				-1,
				-1
			});
			编队信息表.Add(new List<int>
			{
				-1,
				-1,
				-1,
				-1,
				-1
			});
			编队信息表.Add(new List<int>
			{
				-1,
				-1,
				-1,
				-1,
				-1
			});
			编队信息表.Add(new List<int>
			{
				-1,
				-1,
				-1,
				-1,
				-1
			});
			称号信息表.Add(new 称号信息("滑头", 1, 0));
			称号信息表.Add(new 称号信息("寨主", 1, 0));
			称号信息表.Add(new 称号信息("福星", 1, 0));
			称号信息表.Add(new 称号信息("学士", 1, 0));
			称号信息表.Add(new 称号信息("先锋", 2, 0));
			称号信息表.Add(new 称号信息("护军", 2, 0));
			称号信息表.Add(new 称号信息("破军", 2, 0));
			称号信息表.Add(new 称号信息("猛将", 2, 0));
			称号信息表.Add(new 称号信息("贪狼", 2, 0));
			称号信息表.Add(new 称号信息("财主", 2, 0));
			称号信息表.Add(new 称号信息("地主", 2, 0));
			称号信息表.Add(new 称号信息("名士", 2, 0));
			称号信息表.Add(new 称号信息("劳模", 2, 0));
			称号信息表.Add(new 称号信息("善人", 2, 0));
			称号信息表.Add(new 称号信息("明君", 2, 0));
			称号信息表.Add(new 称号信息("贤君", 2, 0));
			称号信息表.Add(new 称号信息("卧龙", 3, 0));
			称号信息表.Add(new 称号信息("凤雏", 3, 0));
			称号信息表.Add(new 称号信息("冢虎", 3, 0));
			称号信息表.Add(new 称号信息("神捕", 3, 0));
			称号信息表.Add(new 称号信息("侠盗", 3, 1));
			称号信息表.Add(new 称号信息("谋士", 3, 1));
			称号信息表.Add(new 称号信息("马贼", 3, 1));
			称号信息表.Add(new 称号信息("锦鲤", 3, 1));
			称号信息表.Add(new 称号信息("义士", 3, 1));
			称号信息表.Add(new 称号信息("英雄", 3, 1));
			称号信息表.Add(new 称号信息("镖师", 3, 1));
			称号信息表.Add(new 称号信息("巧匠", 3, 1));
			称号信息表.Add(new 称号信息("达人", 3, 0));
			称号信息表.Add(new 称号信息("无双", 3, 0));
			称号信息表.Add(new 称号信息("宗师", 3, 0));
			称号信息表.Add(new 称号信息("神工", 3, 0));
			称号信息表.Add(new 称号信息("枭雄", 4, 0));
			称号信息表.Add(new 称号信息("霸主", 4, 0));
			称号信息表.Add(new 称号信息("武圣", 4, 0));
			称号信息表.Add(new 称号信息("飞将", 4, 0));
			称号信息表.Add(new 称号信息("大神", 4, 0));
			称号信息表.Add(new 称号信息("天子", 4, 0));
			称号信息表.Add(new 称号信息("军师", 4, 0));
			称号信息表.Add(new 称号信息("暴君", 4, 0));
			称号信息表.Add(new 称号信息("帝王", 4, 0));
			称号信息表.Add(new 称号信息("君王", 4, 0));
			道具状态表.Add(new 状态信息("休战", 0.0));
			道具状态表.Add(new 状态信息("攻击", 10.0));
			道具状态表.Add(new 状态信息("防御", 10.0));
			道具状态表.Add(new 状态信息("抓将几率", 0.0));
			道具状态表.Add(new 状态信息("资源声望", 50.0));
			道具状态表.Add(new 状态信息("将领经验", 50.0));
			道具状态表.Add(new 状态信息("封地破坏", 0.0));
			道具状态表.Add(new 状态信息("掠夺降忠", 0.0));
			道具状态表.Add(new 状态信息("掠夺收益", 0.0));
			道具状态表.Add(new 状态信息("俘虏玩家", 0.0));
			道具状态表.Add(new 状态信息("攻击速度", 5.0));
			if (玩家名 != "玩家")
			{
				科技信息.升级全部科技(5);
			}
			if (玩家名 == "山贼")
			{
				科技信息.升级全部科技(1);
			}
		}

		public bool 是否格挡(double 兵种, double 对方兵种)
		{
			double num = 0.0;
			num = 科技信息.格斗 * 3.0;
			double num2 = 0.0;
			if (基础信息.称号名 == "帝王")
			{
				num2 = 0.1;
			}
			num *= 1.0 + num2;
			if (兵种 == 2.0 && (对方兵种 == 2.0 || 对方兵种 == 1.0) && (double)Random.Range(1, 101) <= num)
			{
				return true;
			}
			return false;
		}

		public bool 是否穿透(double 兵种)
		{
			double num = 0.0;
			num = 科技信息.精准 * 6.0;
			double num2 = 0.0;
			if (基础信息.称号名 == "帝王")
			{
				num2 = 0.1;
			}
			num *= 1.0 + num2;
			if (兵种 == 3.0 && (double)Random.Range(1, 101) <= num)
			{
				return true;
			}
			return false;
		}

		public bool 是否闪避(double 兵种, double 对方兵种)
		{
			double num = 0.0;
			num = 科技信息.驯马 * 7.0;
			double num2 = 0.0;
			if (基础信息.称号名 == "帝王")
			{
				num2 = 0.1;
			}
			num *= 1.0 + num2;
			if (兵种 == 1.0 && (对方兵种 == 3.0 || 对方兵种 == 4.0) && (double)Random.Range(1, 101) <= num)
			{
				return true;
			}
			return false;
		}

		public 状态信息 获取指定状态加成信息(string 要获取的状态)
		{
			int count = 道具状态表.Count;
			for (int i = 0; i < count; i++)
			{
				if (道具状态表[i].名字 == 要获取的状态)
				{
					return 道具状态表[i];
				}
			}
			return null;
		}

		public double 获取指定状态加成(string 要获取的状态)
		{
			int count = 道具状态表.Count;
			for (int i = 0; i < count; i++)
			{
				if (道具状态表[i].名字 == 要获取的状态)
				{
					if (道具状态表[i].获取状态剩余时间() > 0)
					{
						return 道具状态表[i].加成;
					}
					return 0.0;
				}
			}
			return 0.0;
		}

		public string 获取已佩戴称号()
		{
			int count = 称号信息表.Count;
			for (int i = 0; i < count; i++)
			{
				if (称号信息表[i].状态 == 2)
				{
					基础信息.称号名 = 称号信息表[i].名字;
					return "<" + 称号信息表[i].名字 + ">";
				}
			}
			return "<无>";
		}

		public int 获取称号状态(string 称号名)
		{
			int count = 称号信息表.Count;
			for (int i = 0; i < count; i++)
			{
				if (称号信息表[i].名字 == 称号名)
				{
					return 称号信息表[i].状态;
				}
			}
			return 0;
		}

		public bool 设置称号状态(string 称号名, int 状态)
		{
			int count = 称号信息表.Count;
			for (int i = 0; i < count; i++)
			{
				if (称号信息表[i].名字 == 称号名)
				{
					称号信息表[i].状态 = 状态;
					return true;
				}
			}
			return false;
		}

		public double 获取背包物品数量()
		{
			return 0.0 + (double)背包道具列表.宝物道具列表.Count + (double)背包道具列表.加速道具列表.Count + (double)背包道具列表.生产道具列表.Count + (double)背包道具列表.宝箱道具列表.Count + (double)背包道具列表.强化道具列表.Count + (double)背包道具列表.任务道具列表.Count + (double)背包装备列表.武器装备列表.Count + (double)背包装备列表.头盔装备列表.Count + (double)背包装备列表.铠甲装备列表.Count + (double)背包装备列表.坐骑装备列表.Count;
		}

		public double 获取已占用人口()
		{
			double num = 0.0;
			int count = 封地信息表.Count;
			for (int i = 0; i < count; i++)
			{
				int count2 = 封地信息表[i].闲兵信息表.Count;
				for (int j = 0; j < count2; j++)
				{
					int num2 = 全局兵种库.查询指定ID的索引(封地信息表[i].闲兵信息表[j].ID);
					if (num2 != -1)
					{
						num += 封地信息表[i].闲兵信息表[j].数量 * 全局兵种库.属性表[num2].占用人口;
					}
				}
				int count3 = 封地信息表[i].伤兵信息表.Count;
				for (int k = 0; k < count3; k++)
				{
					int num3 = 全局兵种库.查询指定ID的索引(封地信息表[i].伤兵信息表[k].ID);
					if (num3 != -1)
					{
						num += 封地信息表[i].伤兵信息表[k].数量 * 全局兵种库.属性表[num3].占用人口;
					}
				}
				int count4 = 封地信息表[i].将领信息表.Count;
				for (int l = 0; l < count4; l++)
				{
					if (封地信息表[i].将领信息表[l] != null)
					{
						int num4 = 全局兵种库.查询指定ID的索引(封地信息表[i].将领信息表[l].将领配兵.ID);
						if (num4 != -1)
						{
							num += 封地信息表[i].将领信息表[l].将领配兵.数量 * 全局兵种库.属性表[num4].占用人口;
						}
					}
				}
			}
			return num;
		}

		public double 获取人口上限()
		{
			double num = 0.0;
			int count = 封地信息表.Count;
			for (int i = 0; i < count; i++)
			{
				int count2 = 封地信息表[i].建筑信息表.Count;
				for (int j = 0; j < count2; j++)
				{
					if (封地信息表[i].建筑信息表[j].类型 == 0)
					{
						num += (double)(封地信息表[i].建筑信息表[j].等级 * 500);
					}
					else if (封地信息表[i].建筑信息表[j].类型 == 2)
					{
						num = num + (double)(封地信息表[i].建筑信息表[j].等级 * 250) + (double)((int)科技信息.安置 * 40);
					}
				}
			}
			double num2 = Mathf.Floor((float)(num * 科技信息.工程设计 * 0.05000000074505806));
			return num + num2;
		}

		public double 获取铜钱产量()
		{
			double num = 0.0;
			int count = 封地信息表.Count;
			for (int i = 0; i < count; i++)
			{
				int count2 = 封地信息表[i].建筑信息表.Count;
				for (int j = 0; j < count2; j++)
				{
					if (封地信息表[i].建筑信息表[j].类型 == 0)
					{
						num += (double)(封地信息表[i].建筑信息表[j].等级 * 15);
					}
				}
			}
			double num2 = Mathf.Floor((float)(num * 科技信息.市场贸易 * 0.05000000074505806));
			return num + num2;
		}

		public double 获取粮食产量()
		{
			double num = 0.0;
			int count = 封地信息表.Count;
			for (int i = 0; i < count; i++)
			{
				int count2 = 封地信息表[i].建筑信息表.Count;
				for (int j = 0; j < count2; j++)
				{
					if (封地信息表[i].建筑信息表[j].类型 == 3)
					{
						num += (double)(封地信息表[i].建筑信息表[j].等级 * 25);
					}
				}
			}
			double num2 = Mathf.Floor((float)(num * 科技信息.种植技术 * 0.05000000074505806));
			return num + num2;
		}

		public int 获取指定ID标识的封地索引(int ID标识)
		{
			int count = 封地信息表.Count;
			for (int i = 0; i < count; i++)
			{
				if (封地信息表[i].ID == ID标识)
				{
					return i;
				}
			}
			return -1;
		}

		public double 获取伤兵总数()
		{
			double num = 0.0;
			int count = 封地信息表.Count;
			for (int i = 0; i < count; i++)
			{
				int count2 = 封地信息表[i].伤兵信息表.Count;
				for (int j = 0; j < count2; j++)
				{
					num += 封地信息表[i].伤兵信息表[j].数量;
				}
			}
			return num;
		}

		public double 获取兵力总数()
		{
			double num = 0.0;
			int count = 封地信息表.Count;
			for (int i = 0; i < count; i++)
			{
				int count2 = 封地信息表[i].闲兵信息表.Count;
				for (int j = 0; j < count2; j++)
				{
					num += 封地信息表[i].闲兵信息表[j].数量;
				}
				count2 = 封地信息表[i].将领信息表.Count;
				for (int k = 0; k < count2; k++)
				{
					num += 封地信息表[i].将领信息表[k].将领配兵.数量;
				}
			}
			return num;
		}

		public double 获取指定兵种ID总数(int 兵种ID)
		{
			double num = 0.0;
			int count = 封地信息表.Count;
			for (int i = 0; i < count; i++)
			{
				int count2 = 封地信息表[i].闲兵信息表.Count;
				for (int j = 0; j < count2; j++)
				{
					if (封地信息表[i].闲兵信息表[j].ID == 兵种ID)
					{
						num += 封地信息表[i].闲兵信息表[j].数量;
					}
				}
			}
			return num;
		}

		public double 重置将领状态()
		{
			int num = 0;
			int count = 封地信息表.Count;
			for (int i = 0; i < count; i++)
			{
				int count2 = 封地信息表[i].将领信息表.Count;
				for (int j = 0; j < count2; j++)
				{
					if (封地信息表[i].将领信息表[j].详细信息.状态 == 1.0)
					{
						封地信息表[i].将领信息表[j].详细信息.状态 = 0.0;
					}
				}
			}
			return num;
		}

		public void 将领数扩容()
		{
			基础信息.将领数扩容数量 += 1.0;
			基础信息.将领数上限 = 25.0 + 基础信息.将领数扩容数量;
		}

		public double 获取将领总数()
		{
			int num = 0;
			int count = 封地信息表.Count;
			for (int i = 0; i < count; i++)
			{
				int count2 = 封地信息表[i].将领信息表.Count;
				for (int j = 0; j < count2; j++)
				{
					num++;
				}
			}
			if (num > 25 && (double)num > 25.0 + 基础信息.将领数扩容数量)
			{
				Debug.LogWarning("将领数量超过上限，请整理将领后再招募。");
			}
			return num;
		}

		public double 获取俘虏总数()
		{
			int num = 0;
			int count = 封地信息表.Count;
			for (int i = 0; i < count; i++)
			{
				int count2 = 封地信息表[i].俘虏信息表.Count;
				for (int j = 0; j < count2; j++)
				{
					num++;
				}
			}
			return num;
		}

		public 返回将领索引 获取指定ID标识的将领索引(int ID标识)
		{
			int count = 封地信息表.Count;
			for (int i = 0; i < count; i++)
			{
				int count2 = 封地信息表[i].将领信息表.Count;
				for (int j = 0; j < count2; j++)
				{
					if (封地信息表[i].将领信息表[j].ID == ID标识)
					{
						return new 返回将领索引(i, j);
					}
				}
			}
			return new 返回将领索引(-1, -1);
		}

		public void 封地派遣将领(int 第几个封地, int 派遣到第几个封地, List<将领信息> 派遣将领列表)
		{
		}

		public void 移动封地(int 封地ID标识, 坐标 城池坐标)
		{
			int index = 获取指定ID标识的封地索引(封地ID标识);
			所有城池界面脚本.根据坐标获取指定城池(封地信息表[index].所在城池.x, 封地信息表[index].所在城池.y).删除指定封地(基础信息.ID, 封地ID标识);
			所有城池界面脚本.根据坐标获取指定城池(城池坐标.x, 城池坐标.y).城池封地列表.Add(new 封地索引(基础信息.ID, 封地ID标识));
		}

		public void 删除封地(int 第几个封地)
		{
			if (第几个封地 != 0)
			{
				全局变量.第几个封地 = 0;
				int count = 封地信息表[第几个封地].将领信息表.Count;
				for (int i = 0; i < count; i++)
				{
					将领信息 item = 封地信息表[第几个封地].将领信息表[i];
					封地信息表[0].将领信息表.Add(item);
				}
				int count2 = 封地信息表[第几个封地].闲兵信息表.Count;
				for (int j = 0; j < count2; j++)
				{
					闲兵信息 item2 = 封地信息表[第几个封地].闲兵信息表[j];
					封地信息表[0].闲兵信息表.Add(item2);
				}
				int count3 = 封地信息表[第几个封地].伤兵信息表.Count;
				for (int k = 0; k < count3; k++)
				{
					伤兵信息 item3 = 封地信息表[第几个封地].伤兵信息表[k];
					封地信息表[0].伤兵信息表.Add(item3);
				}
				int count4 = 封地信息表[第几个封地].俘虏信息表.Count;
				for (int l = 0; l < count4; l++)
				{
					将领索引 将领索引 = 封地信息表[第几个封地].俘虏信息表[l];
					返回将领索引 返回将领索引 = 全局变量.所有玩家数据表[将领索引.第几个玩家].获取指定ID标识的将领索引(将领索引.将领ID标识);
					全局变量.所有玩家数据表[将领索引.第几个玩家].封地信息表[返回将领索引.第几个封地].将领信息表[返回将领索引.第几个将领].详细信息.状态 = 0.0;
				}
				int count5 = 封地信息表[第几个封地].驻防信息表.Count;
				for (int m = 0; m < count5; m++)
				{
					将领索引 将领索引2 = 封地信息表[第几个封地].驻防信息表[m];
					返回将领索引 返回将领索引2 = 全局变量.所有玩家数据表[将领索引2.第几个玩家].获取指定ID标识的将领索引(将领索引2.将领ID标识);
					全局变量.所有玩家数据表[将领索引2.第几个玩家].封地信息表[返回将领索引2.第几个封地].将领信息表[返回将领索引2.第几个将领].详细信息.状态 = 0.0;
				}
				封地信息表.RemoveAt(第几个封地);
			}
		}

		public void 随机迁移封地(int 封地ID标识)
		{
			国家信息库类 国家信息库类 = 全局方法类.获取指定名字的国家(基础信息.国家);
			if (国家信息库类 != null)
			{
				国家信息库类.获取国家城池列表();
				int count = 国家信息库类.城池列表.Count;
				int index = Random.Range(0, count);
				移动封地(封地ID标识, 国家信息库类.城池列表[index]);
			}
		}

		public bool 加入指定国家(string 要加入的国家名)
		{
			if (封地信息表.Count == 0)
			{
				国家信息库类 国家信息库类 = 全局方法类.获取指定名字的国家(要加入的国家名);
				if (国家信息库类 != null)
				{
					国家信息库类.成员列表.Add(基础信息.ID);
					所有城池界面脚本.根据坐标获取指定城池(国家信息库类.国都x, 国家信息库类.国都y).新建封地(基础信息.ID);
					基础信息.国家 = 要加入的国家名;
					return true;
				}
				return false;
			}
			更换指定国家(要加入的国家名);
			return true;
		}

		public void 更换指定国家(string 要更换的国家名)
		{
			国家信息库类 国家信息库类 = 全局方法类.获取指定名字的国家(基础信息.国家);
			国家信息库类.成员列表.Remove(基础信息.ID);
			国家信息库类 国家信息库类2 = 全局方法类.获取指定名字的国家(要更换的国家名);
			所有城池界面脚本.根据坐标获取指定城池(封地信息表[0].所在城池.x, 封地信息表[0].所在城池.y)?.删除指定封地(基础信息.ID, 封地信息表[0].ID);
			封地信息表[0].所在城池 = new 坐标(国家信息库类2.国都x, 国家信息库类2.国都y);
			所有城池界面脚本.根据坐标获取指定城池(国家信息库类2.国都x, 国家信息库类2.国都y).城池封地列表.Add(new 封地索引(基础信息.ID, 封地信息表[0].ID));
			int count = 封地信息表.Count;
			for (int i = 0; i < count; i++)
			{
				if (封地信息表.Count > 1)
				{
					城池信息库类 城池信息库类 = 所有城池界面脚本.根据坐标获取指定城池(封地信息表[1].所在城池.x, 封地信息表[1].所在城池.y);
					if (城池信息库类 != null)
					{
						城池信息库类.删除指定封地(基础信息.ID, 封地信息表[1].ID);
						删除封地(1);
					}
				}
			}
			int count2 = 全局变量.所有城池列表.Count;
			for (int j = 0; j < count2; j++)
			{
				if (全局变量.所有城池列表[j].是否属于我的城池())
				{
					全局变量.所有城池列表[j].更换归属(国家信息库类.国王);
				}
			}
			国家信息库类2.成员列表.Add(基础信息.ID);
			基础信息.国家 = 要更换的国家名;
		}

		public bool 添加指定将领到列表(string 要添加的将领名字)
		{
			将领属性库类 将领属性库类 = 全局将领库.查询指定名字的将领数据(要添加的将领名字);
			if (将领属性库类 != null)
			{
				将领属性库类.获取随机属性();
				将领信息 将领信息 = new 将领信息();
				将领信息.生成将领数据(将领属性库类);
				添加将领信息到列表(0, 将领信息);
				return true;
			}
			return false;
		}

		public bool 添加指定ID的将领到列表(int 要添加的将领ID)
		{
			将领属性库类 将领属性库类 = 全局将领库.查询指定ID的将领数据(要添加的将领ID);
			if (将领属性库类 != null)
			{
				将领属性库类.获取随机属性();
				将领信息 将领信息 = new 将领信息();
				将领信息.生成将领数据(将领属性库类);
				if (要添加的将领ID < 10)
				{
					将领信息.将领属性.初始属性.名字 = 随机姓名.生成随机姓名();
				}
				添加将领信息到列表(0, 将领信息);
				return true;
			}
			return false;
		}

		public void 添加一个俘虏到列表(将领索引 将领索引)
		{
			封地信息表[0].俘虏信息表.Add(将领索引);
		}

		public void 添加指定国家全部名将(string 国家名)
		{
			国家信息库类 国家信息库类 = 全局方法类.获取指定名字的国家(国家名);
			if (国家信息库类 != null)
			{
				国家信息库类.国王 = 基础信息.ID;
			}
			List<将领属性库类> list = 全局将领库.获取指定国家的所有名将(国家名);
			int count = list.Count;
			for (int i = 0; i < count; i++)
			{
				将领信息 将领信息 = new 将领信息();
				将领信息.生成将领数据(list[i]);
				将领信息.将领获取经验值(将领信息.获取升级需要经验(99.0));
				将领信息.详细信息.忠诚 = 100.0;
				添加将领信息到列表(0, 将领信息);
			}
			count = Random.Range(0, 5);
			for (int j = 0; j < count; j++)
			{
				将领属性库类 将领属性库类 = 全局将领库.查询指定名字的将领数据(全局将领库.随机获取一个战将名());
				if (将领属性库类 != null)
				{
					将领属性库类.获取随机属性();
					将领信息 将领信息2 = new 将领信息();
					将领信息2.生成将领数据(将领属性库类);
					将领信息2.将领获取经验值(将领信息2.获取升级需要经验(99.0));
					将领信息2.详细信息.忠诚 = 100.0;
					添加将领信息到列表(0, 将领信息2);
				}
			}
			count = Random.Range(0, 5);
			for (int k = 0; k < count; k++)
			{
				将领属性库类 将领属性库类2 = 全局将领库.查询指定名字的将领数据(全局将领库.随机获取一个尊将名());
				if (将领属性库类2 != null)
				{
					将领属性库类2.获取随机属性();
					将领信息 将领信息3 = new 将领信息();
					将领信息3.生成将领数据(将领属性库类2);
					将领信息3.将领获取经验值(将领信息3.获取升级需要经验(99.0));
					将领信息3.详细信息.忠诚 = 100.0;
					添加将领信息到列表(0, 将领信息3);
				}
			}
			count = Random.Range(1, 5);
			for (int l = 0; l < count; l++)
			{
				将领属性库类 将领属性库类3 = 全局将领库.查询指定名字的将领数据(全局将领库.随机获取一个禧将名());
				if (将领属性库类3 != null)
				{
					将领属性库类3.获取随机属性();
					将领信息 将领信息4 = new 将领信息();
					将领信息4.生成将领数据(将领属性库类3);
					将领信息4.将领获取经验值(将领信息4.获取升级需要经验(99.0));
					将领信息4.详细信息.忠诚 = 100.0;
					添加将领信息到列表(0, 将领信息4);
				}
			}
			计算最终属性();
			count = 封地信息表[0].将领信息表.Count;
			for (int m = 0; m < count; m++)
			{
				封地信息表[0].将领信息表[m].将领配兵.ID = 104.0;
				封地信息表[0].将领信息表[m].将领配兵.数量 = 封地信息表[0].将领信息表[m].将领属性.最终属性.统兵;
			}
		}

		public void 添加一个将领到列表(double 要添加的将领ID)
		{
			将领信息 将领信息 = new 将领信息();
			将领信息.生成指定ID将领(要添加的将领ID);
			添加将领信息到列表(0, 将领信息);
		}

		public void 添加将领信息到列表(int 第几个封地, 将领信息 要添加的将领)
		{
			要添加的将领.ID = 将领ID标识;
			封地信息表[第几个封地].将领信息表.Add(要添加的将领);
			计算最终属性();
			将领ID标识++;
		}

		public void 计算最终属性()
		{
            if (Dwsg.Generals.GeneralLegacyAdapter.RecalculateLegacy(this))
            {
                全局变量.提示类.显示信息("属性数据异常,退出!");
                Application.Quit();
            }
        }
	}
}
