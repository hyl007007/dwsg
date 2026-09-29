using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;
using 玩家数据结构;

[Serializable]
public class 城池信息库类
{
	public string 名称 = "";

	public int 坐标x;

	public int 坐标y;

	public int 规模;

	public string 国家 = "";

	public int 城主 = -1;

	[JsonIgnore]
	public bool 正在交战;

	public int 天赋类型;

	public double 天赋加成 = 61.0;

	public double 税率 = 10.0;

	public int 图腾类型;

	public double 图腾加成;

	public double 城主征收_铜 = 10000.0;

	public double 城主征收_粮 = 20000.0;

	public double 国家征收_铜 = 20000.0;

	public double 国家征收_粮 = 40000.0;

	public double 城墙 = 100000.0;

	public double 道路 = 100000.0;

	public double 炮塔 = 100000.0;

	public double 协防几率 = 10.0;

	public float 协防数量f = 1f;

	public float 协防数量m = 6f;

	public double 战功 = 200.0;

	public List<封地索引> 城池封地列表 = new List<封地索引>();

	public List<将领索引> 城池驻防列表 = new List<将领索引>();

	public List<将领信息> 城池玩家驻防列表 = new List<将领信息>();

	public string 公告;

	public void 生成指定规模城池数据(int 指定的规模)
	{
		规模 = 指定的规模;
		城墙 = 获取城墙上限();
		战功 = 获取攻打战功();
		协防几率 = 获取名将驻防几率();
		switch (指定的规模)
		{
		case 0:
			城主征收_铜 = 20000.0;
			城主征收_粮 = 40000.0;
			国家征收_铜 = 20000.0;
			国家征收_粮 = 40000.0;
			break;
		case 1:
			城主征收_铜 = 40000.0;
			城主征收_粮 = 80000.0;
			国家征收_铜 = 40000.0;
			国家征收_粮 = 80000.0;
			break;
		case 2:
			城主征收_铜 = 80000.0;
			城主征收_粮 = 160000.0;
			国家征收_铜 = 80000.0;
			国家征收_粮 = 160000.0;
			break;
		case 3:
			城主征收_铜 = 160000.0;
			城主征收_粮 = 320000.0;
			国家征收_铜 = 160000.0;
			国家征收_粮 = 320000.0;
			break;
		case 4:
			城主征收_铜 = 0.0;
			城主征收_粮 = 0.0;
			国家征收_铜 = 0.0;
			国家征收_粮 = 0.0;
			break;
		}
	}

	public void 攻下城池(int 第几个玩家)
	{
		UnityEngine.Debug.Log("玩家" + 第几个玩家.ToString() + "攻下了 " + 获取规模名称() + "城 " + 名称);
		if (规模 == 4)
		{
			string 名字 = 国家;
			更换归属(第几个玩家);
			生成指定规模城池数据(0);
			国家信息库类 国家信息库类 = 全局方法类.获取指定名字的国家(名字);
			if (国家信息库类 != null && !国家信息库类.迁移都城())
			{
				全局变量.提示类.显示信息(国家信息库类.国号 + "已灭亡!");
				UnityEngine.Debug.Log(国家信息库类.国号 + "被灭国");
				全局方法类.删除指定国家(国家信息库类.国号);
				int count = 全局变量.所有玩家数据表[国家信息库类.国王].封地信息表[0].将领信息表.Count;
				for (int i = count-1; i >= 0; i--)
				{
					全局变量.所有玩家数据表[2].添加将领信息到列表(0, 全局变量.所有玩家数据表[国家信息库类.国王].封地信息表[0].将领信息表[i]);
					全局变量.所有玩家数据表[国家信息库类.国王].封地信息表[0].将领信息表.RemoveAt(i);
				}
				全局变量.所有玩家数据表[全局变量.本机身份].背包道具列表.添加道具("玉玺", 1);
                for (int i = 0; i < 全局变量.所有玩家数据表[2].封地信息表[0].将领信息表.Count; i++)
                {
                    int id = (int)全局变量.所有玩家数据表[2].封地信息表[0].将领信息表[i].将领属性.初始属性.ID;
                    int index = UnityEngine.Random.Range(0, 全局变量.所有城池列表.Count);
                    全局变量.所有城池列表[index].城池驻防列表.Add(new 将领索引(2, id));
                }
                全局变量.提示类.显示信息(国家信息库类.国号 + "已灭亡!世界发生了一些变化");
            }
		}
		else
		{
			更换归属(第几个玩家);
			清空所有封地();
		}
	}

	public void 更换归属(int 新城主)
	{
		城主 = 新城主;
		国家 = 全局变量.所有玩家数据表[新城主].基础信息.国家;
	}

	public void 刷新名将驻防()
	{
		for (int i = 城池驻防列表.Count-1; i >= 0; i--)
		{	
            全局变量.所有城池列表[UnityEngine.Random.Range(0, 全局变量.所有城池列表.Count)].城池驻防列表.Add(城池驻防列表[i]);
			城池驻防列表.RemoveAt(i);
        }
	}

    public void 更换指定城主(string 城主名字)
	{
		玩家数据 玩家数据 = 全局方法类.获取指定名字的玩家(城主名字);
		if (玩家数据 != null)
		{
			城主 = 玩家数据.基础信息.ID;
			国家 = 玩家数据.基础信息.国家;
		}
	}

	public void 清空所有封地()
	{
		for (int i = 0; i < 城池封地列表.Count; i++)
		{
			全局变量.所有玩家数据表[城池封地列表[i].第几个玩家].随机迁移封地(城池封地列表[i].封地ID标识);
		}
	}

	public bool 新建封地(int 第几个玩家)
	{
		int count = 城池封地列表.Count;
		double num = 获取封地上限();
		if ((double)count < num)
		{
			封地信息 封地信息 = new 封地信息();
			封地信息.ID = 全局变量.所有玩家数据表[第几个玩家].封地ID标识;
			封地信息.初始化封地信息();
			封地信息.初始化建筑列表();
			封地信息.所在城池 = new 坐标(坐标x, 坐标y);
			城池封地列表.Add(new 封地索引(第几个玩家, 封地信息.ID));
			if (封地信息.ID == 1)
			{
				封地信息.封地名字 = 全局变量.所有玩家数据表[第几个玩家].基础信息.名字 + "基地";
			}
			else
			{
				封地信息.封地名字 = 名称 + "封地";
			}
			全局变量.所有玩家数据表[第几个玩家].封地信息表.Add(封地信息);
			全局变量.所有玩家数据表[第几个玩家].封地ID标识 = 全局变量.所有玩家数据表[第几个玩家].封地ID标识 + 1;
			return true;
		}
		return false;
	}

	public bool 删除指定封地(int 第几个玩家, int 封地ID标识)
	{
		for (int i = 0; i < 城池封地列表.Count; i++)
		{
			if (城池封地列表[i].第几个玩家 == 第几个玩家 && 城池封地列表[i].封地ID标识 == 封地ID标识)
			{
				城池封地列表.RemoveAt(i);
				return true;
			}
		}
		return false;
	}

	public bool 是否有我的封地()
	{
		for (int i = 0; i < 城池封地列表.Count; i++)
		{
			if (城池封地列表[i].第几个玩家 == 全局变量.本机身份)
			{
				return true;
			}
		}
		return false;
	}

	public int 获取我的封地ID()
	{
		for (int i = 0; i < 城池封地列表.Count; i++)
		{
			if (城池封地列表[i].第几个玩家 == 全局变量.本机身份)
			{
				return 城池封地列表[i].封地ID标识;
			}
		}
		return -1;
	}

	public bool 是否属于我的城池()
	{
		if (城主 == 0)
		{
			return true;
		}
		return false;
	}

	public string 获取国家名字()
	{
		国家信息库类 国家信息库类 = 全局方法类.获取指定名字的国家(国家);
		if (国家信息库类 != null)
		{
			return 国家信息库类.国名;
		}
		return "无";
	}

	public string 获取国家国号()
	{
		国家信息库类 国家信息库类 = 全局方法类.获取指定名字的国家(国家);
		if (国家信息库类 != null)
		{
			return 国家信息库类.国号;
		}
		return "无";
	}

	public string 获取城主名字()
	{
		if (城主 == -1)
		{
			return "无";
		}
		string text = "";
		text = 全局变量.所有玩家数据表[城主].基础信息.名字;
		if (全局变量.所有玩家数据表[城主].基础信息.称号名 != "无")
		{
			text = text + "<" + 全局变量.所有玩家数据表[城主].基础信息.名字 + ">";
		}
		return text;
	}

	public string 获取天赋类型名称()
	{
		return (new string[3]
		{
			"产粮",
			"产钱",
			"征兵"
		})[天赋类型];
	}

	public string 获取图腾类型名称()
	{
		return (new string[4]
		{
			"无",
			"神农",
			"蚩尤",
			"风后"
		})[图腾类型];
	}

	public string 获取规模名称()
	{
		return (new string[5]
		{
			"小",
			"县",
			"郡",
			"州",
			"都"
		})[规模];
	}

	public int 获取名将驻防几率()
	{
		if (规模 == 0)
		{
			协防数量f = 1f;
			协防数量m = 7f;
			return UnityEngine.Random.Range(10, 20);
		}
		if (规模 == 1)
		{
			协防数量f = 7f;
			协防数量m = 15f;
			return UnityEngine.Random.Range(35, 50);
		}
		if (规模 == 2)
		{
			协防数量f = 15f;
			协防数量m = 20f;
			return UnityEngine.Random.Range(50, 70);
		}
		if (规模 == 3)
		{
			协防数量f = 20f;
			协防数量m = 50f;
			return UnityEngine.Random.Range(70, 90);
		}
		if (规模 == 4)
		{
			协防数量f = 50f;
			协防数量m = 100f;
			return 100;
		}
		return 1;
	}

	public float 获取名将驻防数量()
	{
		return UnityEngine.Random.Range(协防数量f, 协防数量m);
	}

	public int 获取攻打战功()
	{
		if (规模 == 0)
		{
			return 500;
		}
		if (规模 == 1)
		{
			return 1000;
		}
		if (规模 == 2)
		{
			return 1500;
		}
		if (规模 == 3)
		{
			return 2000;
		}
		if (规模 == 4)
		{
			return 10000;
		}
		return 1;
	}

	public double 获取城墙上限()
	{
		if (规模 == 0)
		{
			return 200000.0;
		}
		if (规模 == 1)
		{
			return 400000.0;
		}
		if (规模 == 2)
		{
			return 800000.0;
		}
		if (规模 == 3)
		{
			return 1600000.0;
		}
		if (规模 == 4)
		{
			return 2000000.0;
		}
		return 0.0;
	}

	public double 获取封地上限()
	{
		if (规模 == 0)
		{
			return 20.0;
		}
		if (规模 == 1)
		{
			return 100.0;
		}
		if (规模 == 2)
		{
			return 500.0;
		}
		if (规模 == 3)
		{
			return 3000.0;
		}
		if (规模 == 4)
		{
			return 9000.0;
		}
		return 0.0;
	}

	public double 获取驻防上限()
	{
		if (规模 == 0)
		{
			return 1000.0;
		}
		if (规模 == 1)
		{
			return 2000.0;
		}
		if (规模 == 2)
		{
			return 3000.0;
		}
		if (规模 == 3)
		{
			return 5000.0;
		}
		if (规模 == 4)
		{
			return 8000.0;
		}
		return 0.0;
	}

	public double 获取道路上限()
	{
		return 100000.0;
	}

	public int 获取城池身份()
	{
		if (全局变量.所有玩家数据表[全局变量.本机身份].基础信息.国家 == 国家)
		{
			return 0;
		}
		if (国家 == "")
		{
			return 3;
		}
		return 1;
	}

	public void 获取城池名称坐标(int 第几个城池)
	{
		int num = 0;
		for (int i = 0; i < 全局大地图库.大地图表.GetLength(0); i++)
		{
			for (int j = 0; j < 全局大地图库.大地图表.GetLength(1); j++)
			{
				if (全局大地图库.大地图表[i, j] >= 2)
				{
					if (num == 第几个城池)
					{
						名称 = 全局城池库.城池名称[第几个城池];
						坐标x = j + 1;
						坐标y = i + 1;
						return;
					}
					num++;
				}
			}
		}
	}
}
