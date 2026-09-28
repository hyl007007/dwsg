using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class 国家信息库类
{
	public int ID;

	public string 国名 = "";

	public string 国号 = "";

	public int 国都x;

	public int 国都y;

	public int 国王 = -1;

	public List<坐标> 城池列表 = new List<坐标>();

	public List<int> 成员列表 = new List<int>();

	public double 效率 = 120.0;

	public double 科技等级 = 1.0;

	public double 科技积分 = 1.0;

	public double 攻击科技 = 0.0;

	public double 防御科技 = 0.0;

	public double 资源科技 = 0.0;

	public double 民生值 = 5000.0;

	public double 铜钱 = 5000.0;

	public double 粮食 = 5000.0;

	public string 公告 = "";

	public string 宣言 = "";

	public int 大都督 = -1;

	public int 丞相 = -1;

	public int 奋武将军 = -1;

	public int 征东将军 = -1;

	public int 都尉 = -1;

	public int 侍郎 = -1;

	public long 上次轮选时间 = TIME.getTime();

	public long 轮选时间间隔 = 300L;

	public void 初始化国家(string 国名0, string 国号0, int 国都x0, int 国都y0)
	{
		国名 = 国名0;
		国号 = 国号0;
		国都x = 国都x0;
		国都y = 国都y0;
		全局变量.国家ID记录++;
		ID = 全局变量.国家ID记录;
		刷新科技信息();

    }

	public int 新建国家(string 国名0, string 国号0, int 国都x0, int 国都y0, int 国王0)
	{
		国名 = 国名0;
		国号 = 国号0;
		国都x = 国都x0;
		国都y = 国都y0;
		国王 = 国王0;
		全局变量.国家ID记录++;
		ID = 全局变量.国家ID记录;
		城池信息库类 城池信息库类 = 所有城池界面脚本.根据坐标获取指定城池(国都x, 国都y);
		if (城池信息库类.城主 == 国王)
		{
			if (城池信息库类.规模 == 2)
			{
				城池信息库类.清空所有封地();
				城池信息库类.国家 = 国号;
				城池信息库类.生成指定规模城池数据(4);
				全局变量.国家ID记录++;
				ID = 全局变量.国家ID记录;
				return 1;
			}
			return -3;
		}
		return -2;
	}

	public string 获取国家规模名称()
	{
		int count = 城池列表.Count;
		if (count >= 15)
		{
			return "侯国";
		}
		if (count >= 100)
		{
			return "公国";
		}
		if (count >= 200)
		{
			return "王国";
		}
		if (count >= 300)
		{
			return "帝国";
		}
		return "小国";
	}

	public void 获取国家城池列表()
	{
		城池列表.Clear();
		int count = 全局变量.所有城池列表.Count;
		for (int i = 0; i < count; i++)
		{
			if (全局变量.所有城池列表[i].国家 == 国号)
			{
				城池列表.Add(new 坐标(全局变量.所有城池列表[i].坐标x, 全局变量.所有城池列表[i].坐标y));
			}
		}
	}

	public Sprite 获取国家头像()
	{
		int num = 全局变量.所有国家头像资源表.Length;
		for (int i = 0; i < num; i++)
		{
			if (全局变量.所有国家头像资源表[i].name == 国号)
			{
				return 全局变量.所有国家头像资源表[i];
			}
		}
		return 全局变量.自建国头像;
	}

	public bool 迁移都城()
	{
		获取国家城池列表();
		int count = 城池列表.Count;
		if (count > 0)
		{
			城池信息库类 城池信息库类 = 所有城池界面脚本.根据坐标获取指定城池(国都x, 国都y);
			int index = UnityEngine.Random.Range(0, count);
			国都x = 城池列表[index].x;
			国都y = 城池列表[index].y;
			城池信息库类 城池信息库类2 = 所有城池界面脚本.根据坐标获取指定城池(国都x, 国都y);
			城池信息库类2.更换归属(国王);
			城池信息库类2.生成指定规模城池数据(4);
			int count2 = 城池信息库类.城池封地列表.Count;
			for (int i = 0; i < count2; i++)
			{
				全局变量.所有玩家数据表[城池信息库类.城池封地列表[i].第几个玩家].移动封地(城池信息库类.城池封地列表[i].封地ID标识, new 坐标(国都x, 国都y));
			}
			城池信息库类.城池封地列表.Clear();
			UnityEngine.Debug.Log(国名 + "国都从" + 城池信息库类.名称 + " 迁移到 " + 城池信息库类2.名称);
			return true;
		}
		return false;
	}
    public void 刷新科技信息()
    {
        科技等级 = 防御科技 + 攻击科技 + 资源科技;
    }
}
