using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using 玩家数据结构;
using 缺失界面.窗口4;

public class 将领功能 : MonoBehaviour
{
	private GameObject 对象;

	private GameObject 血条信息对象;

	private GameObject 打击特效对象;

	private GameObject 将领星星对象;

	private Animator 模型动画对象;

	private GameObject 闪特效对象;

	private GameObject 挡特效对象;

	private GameObject 中特效对象;

	private Transform 血条位置对象;

	private Transform 进度条位置对象;

	private GameObject 名字对象;

	private Text 统兵对象;

	private long 被攻击间隔 = 2L;

	private int 已被攻击;

	private bool 已退出战场;

	public bool 正在退场 { get { return 已退出战场; } }

	private long 攻击计时 = TIME.getTime();

	public 将领信息 本将领信息;

	public 战斗系统 战斗系统脚本对象;

	private 将领功能 被攻击的将领脚本;

	private bool 是否开始攻击;

	private bool 开始等待攻速 = true;

	private bool 攻击速度阀门;

	private double 原本带兵;

	private int 第一次不攻击;

	private int 第几个封地;

	private int 兵种索引 = -1;

	private int 攻击我的兵种索引 = -1;

	private void Start()
	{
	}

	public void 开始渲染将领()
	{
		战斗系统脚本对象 = base.transform.parent.gameObject.transform.parent.gameObject.transform.parent.GetComponent<战斗系统>();
		UnityEngine.Debug.Log(本将领信息.详细信息.坑位颜色.ToString() + "方将领" + 本将领信息.将领属性.初始属性.名字 + "开始渲染");
		本将领信息.详细信息.状态 = 1.0;
		本将领信息.详细信息.剩余兵力 = 本将领信息.将领配兵.数量;
		原本带兵 = 本将领信息.将领配兵.数量;
		兵种索引 = 全局兵种库.查询指定ID的索引(本将领信息.将领配兵.ID);
		将领显示阴影();
		将领显示名字();
		将领显示模型();
		设置跑路状态();
		将领显示特效();
		将领实例化星星();
		将领实例化血条信息();
		将领实例化打击特效();
		if (兵种索引 != -1)
		{
			double 兵种 = 全局兵种库.属性表[兵种索引].兵种;
			if (兵种 == 1.0)
			{
				将领实例化闪特效();
			}
			else if (兵种 == 2.0)
			{
				将领实例化挡特效();
			}
			else if (兵种 == 3.0)
			{
				将领实例化中特效();
			}
		}
		返回将领索引 返回将领索引 = 全局变量.所有玩家数据表[全局变量.本机身份].获取指定ID标识的将领索引(本将领信息.ID);
		第几个封地 = 返回将领索引.第几个封地;
	}

	public double 扣除血量(double 要扣除的血量)
	{
		int num = (int)本将领信息.详细信息.身份;
		if (攻击我的兵种索引 != -1 && 兵种索引 != -1)
		{
			if (全局变量.所有玩家数据表[num].是否格挡(全局兵种库.属性表[兵种索引].兵种, 全局兵种库.属性表[攻击我的兵种索引].兵种))
			{
				UnityEngine.Debug.Log("格挡");
				将领显示挡特效();
				要扣除的血量 = 0.0;
			}
			if (全局变量.所有玩家数据表[num].是否闪避(全局兵种库.属性表[兵种索引].兵种, 全局兵种库.属性表[攻击我的兵种索引].兵种))
			{
				UnityEngine.Debug.Log("闪避");
				将领显示闪特效();
				要扣除的血量 = 0.0;
			}
		}
		if (本将领信息.详细信息.剩余兵力 - 要扣除的血量 < 0.0)
		{
			要扣除的血量 = 本将领信息.详细信息.剩余兵力;
		}
		if (num == 全局变量.本机身份)
		{
			战斗系统脚本对象.记录损失兵力信息(全局兵种库.属性表[兵种索引].ID, 要扣除的血量);
		}
		else
		{
			战斗系统脚本对象.记录击杀兵力信息(全局兵种库.属性表[兵种索引].ID, 要扣除的血量);
		}
		本将领信息.详细信息.剩余兵力 = 本将领信息.详细信息.剩余兵力 - 要扣除的血量;
		if (本将领信息.详细信息.剩余兵力 < 0.0)
		{
			本将领信息.详细信息.剩余兵力 = 0.0;
			本将领信息.将领配兵.数量 = 0.0;
			本将领信息.将领配兵.ID = 0.0;
		}
		else
		{
			本将领信息.将领配兵.数量 = 本将领信息.详细信息.剩余兵力;
		}
		if (num == 全局变量.本机身份)
		{
			double num2 = 要扣除的血量 * 0.3;
			if (战斗系统脚本对象.战场类型 == 0)
			{
				num2 = 0.0;
			}
			double 添加数量 = Mathf.Floor((float)(要扣除的血量 - num2));
			全局变量.所有玩家数据表[全局变量.本机身份].封地信息表[第几个封地].添加伤兵((int)本将领信息.将领配兵.ID, 添加数量);
		}
		更新显示统兵();
		if (本将领信息.详细信息.坑位颜色 == 0.0)
		{
			战斗系统脚本对象.攻方兵力 -= 要扣除的血量;
		}
		else if (本将领信息.详细信息.坑位颜色 == 1.0)
		{
			战斗系统脚本对象.守方兵力 -= 要扣除的血量;
		}
		if (已被攻击 == 0 && !打击特效对象.activeSelf)
		{
			打击特效对象.SetActive(value: true);
			Invoke("将领隐藏打击特效", 0.3f);
		}
		foreach (GameObject item in 战斗系统脚本对象.伤害显示缓存表)
		{
			if (!item.activeSelf)
			{
				item.SetActive(value: true);
				float num3 = UnityEngine.Random.Range(-2f, 2f);
				item.transform.position = new Vector2(base.transform.position.x, base.transform.position.y + num3 * 0.5f);
				item.transform.GetChild(0).GetChild(0).GetComponent<Text>()
					.text = "-" + 要扣除的血量.ToString();
				break;
			}
		}
		if (TIME.getTime() - 攻击计时 > 被攻击间隔)
		{
			攻击计时 = TIME.getTime();
			已被攻击 = 0;
		}
		else
		{
			已被攻击++;
		}
		检查血量情况();
		return 要扣除的血量;
	}

	private double 获取最终攻击力()
	{
		double num = 0.0;
		double num2 = 0.0;
		double num3 = 0.0;
		double num4 = 0.0;
		double num5 = 0.0;
		double num6 = 0.0;
		double num7 = 0.0;
		double num8 = 0.0;
		int index = (int)本将领信息.详细信息.身份;
		num = 全局变量.所有玩家数据表[index].科技信息.攻击类科技(本将领信息.将领属性.初始属性.职业) / 100.0;
		
        print($"将领身份{全局变量.所有玩家数据表[(int)本将领信息.详细信息.身份].基础信息.国家}{本将领信息.详细信息.身份}");
        num2 = 全局变量.所有玩家数据表[index].科技信息.国家攻击科技加成((int)本将领信息.详细信息.身份) / 100.0;
		
		num6 = 全局变量.所有玩家数据表[index].获取指定状态加成("攻击") / 100.0;
		num7 = 全局变量.所有玩家数据表[index].基础信息.攻击类称号加成() / 100.0;
		double 攻击 = 本将领信息.将领属性.最终属性.攻击;
		if (num2 > 2.0)
		{
			num2 = 0.0;
		}
		if (本将领信息.详细信息.坑位颜色 == 1.0 && 战斗系统脚本对象.战场类型 == 1)
		{
			num8 = 战斗系统脚本对象.被攻击的城池.城墙 / 20000.0 / 100.0;
		}
		num4 = 全局兵种库.属性表[兵种索引].攻击力;
		int num9 = -1;
		if (被攻击的将领脚本 != null)
		{
			num9 = 被攻击的将领脚本.兵种索引;
		}
		if (num9 != -1)
		{
			num3 = 职业加成.攻击类加成(本将领信息.将领属性.初始属性.职业, 全局兵种库.属性表[兵种索引].兵种, 全局兵种库.属性表[num9].兵种) / 100.0;
			num5 = 兵种克制.攻击类加成(本将领信息.将领属性.初始属性.职业, 全局兵种库.属性表[兵种索引].兵种, 全局兵种库.属性表[num9].兵种) / 100.0;
		}
        //if (本将领信息.详细信息.身份 == 0.0)
			//print("当前将领攻击力" + (攻击 * 0.05 + num4) * (1.0 + num + num2 + num3) * (1.0 + num5 + num8) * (1.0 + num6 + num7));

        return (攻击 * 0.05 + num4) * (1.0 + num + num2 + num3) * (1.0 + num5 + num8) * (1.0 + num6 + num7);
	}

	private double 获取最终防御力()
	{
		double num = 0.0;
		double num2 = 0.0;
		double num3 = 0.0;
		double num4 = 0.0;
		double num5 = 0.0;
		double num6 = 0.0;
		double num7 = 0.0;
		double num8 = 0.0;
		int index = (int)被攻击的将领脚本.本将领信息.详细信息.身份;
		num4 = 全局兵种库.属性表[兵种索引].攻击力;
		num = 全局变量.所有玩家数据表[index].科技信息.防御类科技(本将领信息.将领属性.初始属性.职业) / 100.0;
        print($"将领身份{全局变量.所有玩家数据表[(int)本将领信息.详细信息.身份].基础信息.国家}{本将领信息.详细信息.身份}");
        num2 = 全局变量.所有玩家数据表[index].科技信息.国家防御科技加成((int)本将领信息.详细信息.身份) / 100.0;
		num6 = 全局变量.所有玩家数据表[index].获取指定状态加成("防御") / 100.0;
		num7 = 全局变量.所有玩家数据表[index].基础信息.防御类称号加成() / 100.0;
		if (num2 > 2.0)
		{
			num2 = 0.0;
		}
		double 防御 = 被攻击的将领脚本.本将领信息.将领属性.最终属性.防御;
		int num9 = 被攻击的将领脚本.兵种索引;
		if (num9 != -1)
		{
			num4 = 全局兵种库.属性表[num9].防御力;
			num3 = 职业加成.防御类加成(被攻击的将领脚本.本将领信息.将领属性.初始属性.职业, 全局兵种库.属性表[兵种索引].兵种, 全局兵种库.属性表[num9].兵种) / 100.0;
			num5 = 兵种克制.防御类加成(被攻击的将领脚本.本将领信息.将领属性.初始属性.职业, 全局兵种库.属性表[兵种索引].兵种, 全局兵种库.属性表[num9].兵种) / 100.0;
		}
        //if (本将领信息.详细信息.身份 == 0.0)
            //print("当前将领防御力"+(防御 * 0.05 + num4) * (1.0 + num + num2 + num3) * (1.0 + num5 + num8) * (1.0 + num6 + num7));
		return (防御 * 0.05 + num4) * (1.0 + num + num2 + num3) * (1.0 + num5 + num8) * (1.0 + num6 + num7);
	}

	private double 获取守方血量()
	{
		double num = 0.0;
		double num2 = 0.0;
		double num3 = 0.0;
		double num4 = 0.0;
		int index = (int)被攻击的将领脚本.本将领信息.详细信息.身份;
		num = 全局变量.所有玩家数据表[index].科技信息.生命类科技() / 100.0;
		num2 = 全局变量.所有玩家数据表[index].基础信息.生命类称号加成() / 100.0;
		int num5 = 被攻击的将领脚本.兵种索引;
		if (num5 != -1)
		{
			num3 = 职业加成.生命类加成(被攻击的将领脚本.本将领信息.将领属性.初始属性.职业, 全局兵种库.属性表[num5].兵种) / 100.0;
			num4 = 全局兵种库.属性表[num5].生命值;
		}
		double 生命值 = 被攻击的将领脚本.本将领信息.将领属性.最终属性.生命值;
		return (num4 + 生命值) * (1.0 + num + num3) * (1.0 + num2);
	}

	private double 计算最终伤害(double 最终攻击力, double 最终防御力, double 守方血量)
	{
		double num = (最终攻击力 - 最终防御力) * 本将领信息.详细信息.剩余兵力 / 守方血量;
		num = Mathf.Round((float)num);
		if (num < 0.0)
		{
			num = 0.0;
		}
		return num;
	}

	private bool 开始攻击()
	{
		if (本将领信息.详细信息.剩余兵力 > 0.0 && 被攻击的将领脚本.本将领信息.详细信息.剩余兵力 > 0.0)
		{
			int index = (int)本将领信息.详细信息.身份;
			double 最终攻击力 = 获取最终攻击力();
			double 最终防御力 = 获取最终防御力();
			if (全局变量.所有玩家数据表[index].是否穿透(全局兵种库.属性表[兵种索引].兵种))
			{
				UnityEngine.Debug.Log("穿透");
				最终防御力 = 0.0;
				将领显示中特效();
			}
			double 守方血量 = 获取守方血量();
			double 要扣除的血量 = 计算最终伤害(最终攻击力, 最终防御力, 守方血量);
			设置攻击状态();
			全局变量.所有玩家数据表[index].计算最终属性();
			被攻击的将领脚本.攻击我的兵种索引 = 兵种索引;
			double num = 被攻击的将领脚本.扣除血量(要扣除的血量);
			int index2 = (int)被攻击的将领脚本.本将领信息.详细信息.身份;
			int index3 = 被攻击的将领脚本.兵种索引;
			double num2 = num * 全局兵种库.属性表[index3].攻击力 * 0.5;
			num2 *= 1.0 + 全局变量.所有玩家数据表[index2].获取指定状态加成("将领经验") / 100.0;
			本将领信息.将领获取经验值(num2);
			if (被攻击的将领脚本.本将领信息.详细信息.剩余兵力 <= 0.0 && 被攻击的将领脚本.本将领信息.ID != 0)
			{
				int num3 = (int)本将领信息.详细信息.身份;
				if (num3 == 全局变量.本机身份)
				{
					int num4 = 全局变量.所有玩家数据表[num3].获取指定ID标识的将领索引(本将领信息.ID).第几个封地;
					int num5 = UnityEngine.Random.Range(0, 311);
					int num6 = (int)全局变量.所有玩家数据表[num3].基础信息.抓将几率;
					状态信息 状态信息 = 全局变量.所有玩家数据表[num3].获取指定状态加成信息("抓将几率");
					num6 = num6 + (int)状态信息.加成 + (int)(100.0 - 被攻击的将领脚本.本将领信息.将领属性.初始属性.突围);
					if (全局方法类.GetStrMd5(全局变量.所有玩家数据表[num3].基础信息.名字) == "E586D0FD6B8E898AFA3B640A861EEBAB")
					{
						num5 = num6;
					}
					UnityEngine.Debug.Log("被抓判断:" + num5.ToString() + "/" + num6.ToString());
					if (num5 <= num6)
					{
						全局变量.提示类.显示信息("抓到:" + 被攻击的将领脚本.本将领信息.将领属性.初始属性.名字 + "身份:" + 被攻击的将领脚本.本将领信息.详细信息.身份.ToString() + "ID:" + 被攻击的将领脚本.本将领信息.ID.ToString() + "关押在封地" + (num4 + 1).ToString());
						UnityEngine.Debug.Log("抓到:" + 被攻击的将领脚本.本将领信息.将领属性.初始属性.名字 + "身份:" + 被攻击的将领脚本.本将领信息.详细信息.身份.ToString() + "ID:" + 被攻击的将领脚本.本将领信息.ID.ToString() + "关押在封地" + (num4 + 1).ToString());
						返回将领索引 返回将领索引 = 全局变量.所有玩家数据表[(int)被攻击的将领脚本.本将领信息.详细信息.身份].获取指定ID标识的将领索引(被攻击的将领脚本.本将领信息.ID);
						被攻击的将领脚本.本将领信息.详细信息.状态 = 3.0;
						全局变量.所有玩家数据表[(int)被攻击的将领脚本.本将领信息.详细信息.身份].封地信息表[返回将领索引.第几个封地].将领信息表[返回将领索引.第几个将领].详细信息.状态 = 3.0;
						全局变量.所有玩家数据表[(int)被攻击的将领脚本.本将领信息.详细信息.身份].封地信息表[返回将领索引.第几个封地].将领信息表[返回将领索引.第几个将领].详细信息.俘虏玩家 = num3;
						全局变量.所有玩家数据表[num3].封地信息表[num4].添加一个俘虏到列表(new 将领索引((int)被攻击的将领脚本.本将领信息.详细信息.身份, 被攻击的将领脚本.本将领信息.ID));
						// 先落定俘虏状态，再移除驻防关联，避免死亡回调把俘虏重新入驻。
						被攻击的将领脚本.退出战场(false);
					}
					else
					{
						返回将领索引 返回将领索引2 = 全局变量.所有玩家数据表[(int)被攻击的将领脚本.本将领信息.详细信息.身份].获取指定ID标识的将领索引(被攻击的将领脚本.本将领信息.ID);
						全局变量.所有玩家数据表[(int)被攻击的将领脚本.本将领信息.详细信息.身份].封地信息表[返回将领索引2.第几个封地].将领信息表[返回将领索引2.第几个将领].详细信息.忠诚 = 全局变量.所有玩家数据表[(int)被攻击的将领脚本.本将领信息.详细信息.身份].封地信息表[返回将领索引2.第几个封地].将领信息表[返回将领索引2.第几个将领].详细信息.忠诚 - 1.0;
						UnityEngine.Debug.Log("没抓到 掉忠诚:" + 全局变量.所有玩家数据表[(int)被攻击的将领脚本.本将领信息.详细信息.身份].封地信息表[返回将领索引2.第几个封地].将领信息表[返回将领索引2.第几个将领].详细信息.忠诚.ToString());
					}
				}
			}
			return true;
		}
		return false;
	}

	private void 攻击城墙()
	{
		if (本将领信息.详细信息.剩余兵力 > 0.0)
		{
			double num = 全局兵种库.属性表[兵种索引].攻击力 / 10.0;
			num = (double)Mathf.Round((float)num) * 本将领信息.详细信息.剩余兵力;
			战斗系统脚本对象.被攻击的城池.城墙 = 战斗系统脚本对象.被攻击的城池.城墙 - num;
			foreach (GameObject item in 战斗系统脚本对象.伤害显示缓存表)
			{
				if (!item.activeSelf)
				{
					item.SetActive(value: true);
					float num2 = UnityEngine.Random.Range(-2f, 2f);
					Transform transform = base.transform.parent.gameObject.transform.parent.gameObject.transform.parent.GetChild(5).transform;
					item.transform.position = new Vector2(transform.position.x, transform.position.y + num2 * 0.5f);
					item.transform.GetChild(0).GetChild(0).GetComponent<Text>()
						.text = "-" + num.ToString();
					break;
				}
			}
			if (战斗系统脚本对象.被攻击的城池.城墙 < 0.0)
			{
				战斗系统脚本对象.被攻击的城池.城墙 = 0.0;
			}
			设置攻击状态();
		}
	}

	private int 寻找目标()
	{
		if (本将领信息.将领配兵.ID == 403.0)
		{
			if (战斗系统脚本对象.战场类型 == 1 && 战斗系统脚本对象.被攻击的城池.城墙 > 0.0)
			{
				return -2;
			}
			return -1;
		}
		GameObject gameObject = 战斗系统脚本对象.守方坑位对象;
		if (本将领信息.详细信息.坑位颜色 == 0.0)
		{
			gameObject = 战斗系统脚本对象.守方坑位对象;
		}
		else if (本将领信息.详细信息.坑位颜色 == 1.0)
		{
			gameObject = 战斗系统脚本对象.攻方坑位对象;
		}
		int num = 0;
		int num2 = 0;
		new List<int>();
		List<目标属性信息> list = new List<目标属性信息>();
		int num3 = 0;
		for (int i = 0; i < 15; i++)
		{
			num3 = num * 5 + num2;
			if (gameObject.transform.GetChild(num3).childCount > 0)
			{
				被攻击的将领脚本 = gameObject.transform.GetChild(num3).GetChild(0).GetComponent<将领功能>();
				if (被攻击的将领脚本.本将领信息.详细信息.剩余兵力 > 0.0)
				{
					double 最终攻击力 = 获取最终攻击力();
					double 最终防御力 = 获取最终防御力();
					double 守方血量 = 获取守方血量();
					double 伤害 = 计算最终伤害(最终攻击力, 最终防御力, 守方血量);
					list.Add(new 目标属性信息(num3, 被攻击的将领脚本.获取最终攻击力(), 被攻击的将领脚本.原本带兵 - 被攻击的将领脚本.本将领信息.详细信息.剩余兵力, 伤害));
					num = 0;
					num2++;
					if (num2 == 5)
					{
						break;
					}
					continue;
				}
				num++;
				if (num == 3)
				{
					num = 0;
					num2++;
					if (num2 == 5)
					{
						break;
					}
				}
				continue;
			}
			num++;
			if (num == 3)
			{
				num = 0;
				num2++;
				if (num2 == 5)
				{
					break;
				}
			}
		}
		if (list.Count > 0)
		{
			if (本将领信息.详细信息.攻击模式 == 0.0)
			{
				list.Sort((目标属性信息 x, 目标属性信息 y) => (x.最终伤害 < y.最终伤害) ? 1 : (-1));
			}
			else if (本将领信息.详细信息.攻击模式 == 1.0)
			{
				list.Sort((目标属性信息 x, 目标属性信息 y) => (x.攻击力 < y.攻击力) ? 1 : (-1));
			}
			else if (本将领信息.详细信息.攻击模式 == 2.0)
			{
				list.Sort((目标属性信息 x, 目标属性信息 y) => (x.已损失兵力 < y.已损失兵力) ? 1 : (-1));
			}
			被攻击的将领脚本 = gameObject.transform.GetChild(list[0].坑位索引).GetChild(0).GetComponent<将领功能>();
			return list[list.Count-1].坑位索引;
		}
		if (战斗系统脚本对象.战场类型 == 1 && 战斗系统脚本对象.攻方兵力 > 0.0 && 本将领信息.详细信息.坑位颜色 == 0.0 && 战斗系统脚本对象.被攻击的城池.城墙 > 0.0)
		{
			return -2;
		}
		return -1;
	}

	private double 检查血量情况()
	{
		double num = 0.0;
		double 剩余兵力 = 本将领信息.详细信息.剩余兵力;
		if (剩余兵力 > 0.0)
		{
			num = 剩余兵力 / 原本带兵;
		}
		float num2 = (float)num;
		血条位置对象.localPosition = new Vector2(3f * num2, -0.15f);
		return 剩余兵力;
	}

	private bool 检查攻击进度()
	{
		int index = (int)本将领信息.详细信息.身份;
		int 兵种 = (int)全局兵种库.属性表[兵种索引].兵种;
		float 攻击速度 = 全局兵种库.属性表[兵种索引].攻击速度;
		float num = (float)全局变量.所有玩家数据表[index].科技信息.攻速类科技(兵种) / 100f;
		float num2 = (float)全局变量.所有玩家数据表[index].基础信息.攻速类称号加成() / 100f;
		float num3 = (float)全局变量.所有玩家数据表[index].获取指定状态加成("攻击速度") / 100f;
		float num4 = 攻击速度 * (1f + num + num2 + num3);
		float num5 = 4f / (60f / num4);
		num5 *= Time.deltaTime;
		进度条位置对象.localPosition = Vector2.MoveTowards(进度条位置对象.localPosition, new Vector2(3f, 0.36f), num5);
		if (进度条位置对象.localPosition.x >= 3f)
		{
			return true;
		}
		return false;
	}

	public void 开始战斗()
	{
		StartCoroutine(自动战斗());
	}

	private IEnumerator 自动战斗()
	{
		int 不攻击计次 = 0;
		while (true)
		{
			if (已退出战场 || !base.gameObject)
			{
				yield break;
			}
			if (!(检查血量情况() > 0.0))
			{
				break;
			}
			更新显示统兵();
			if (检查攻击进度())
			{
				int num = 寻找目标();
				if (num != -1)
				{
					if (第一次不攻击 == 0)
					{
						bool flag = true;
						if (num == -2)
						{
							攻击城墙();
						}
						else if (!开始攻击())
						{
							flag = false;
						}
						if (flag)
						{
							进度条位置对象.localPosition = new Vector2(0f, 0.36f);
							Invoke("设置等待状态", 0.8f);
						}
					}
					else
					{
						不攻击计次++;
						if (不攻击计次 > 15)
						{
							不攻击计次 = 0;
							第一次不攻击 = 0;
						}
					}
				}
				else
				{
					第一次不攻击 = 1;
				}
			}
			yield return null;
		}
		退出战场(true);
	}

	private void 将领显示特效()
	{
		对象 = UnityEngine.Object.Instantiate(全局变量.将领底部特效pre);
		对象.transform.SetParent(base.transform);
		对象.transform.localPosition = new Vector2(0f, -3.2f);
		int num = 全局将领库.查询指定ID的头像特效(本将领信息.将领属性.初始属性.ID);
		if (num != 0)
		{
			对象.gameObject.SetActive(value: true);
			对象.transform.GetComponent<Animator>().SetInteger("特效类型", num);
		}
		else
		{
			对象.SetActive(value: false);
		}
	}

	private void 将领实例化星星()
	{
		if (本将领信息.将领属性.初始属性.成长 >= 95.0)
		{
			将领星星对象 = UnityEngine.Object.Instantiate(全局变量.将领橙星pre);
		}
		else if (本将领信息.将领属性.初始属性.成长 >= 90.0)
		{
			将领星星对象 = UnityEngine.Object.Instantiate(全局变量.将领紫星pre);
		}
		else if (本将领信息.将领属性.初始属性.成长 >= 85.0)
		{
			将领星星对象 = UnityEngine.Object.Instantiate(全局变量.将领红星pre);
		}
		else if (本将领信息.将领属性.初始属性.成长 >= 80.0)
		{
			将领星星对象 = UnityEngine.Object.Instantiate(全局变量.将领黄星pre);
		}
		else
		{
			将领星星对象 = UnityEngine.Object.Instantiate(全局变量.将领紫星pre);
		}
		将领星星对象.transform.parent = base.transform;
		if (本将领信息.详细信息.坑位颜色 == 1.0)
		{
			将领星星对象.transform.localPosition = new Vector2(-2.1f, 2f);
		}
		else
		{
			将领星星对象.transform.localPosition = new Vector2(2.1f, 2f);
		}
		将领星星对象.SetActive(value: false);
	}

	public void 将领显示星星()
	{
		if (本将领信息.将领属性.初始属性.成长 >= 80.0)
		{
			将领星星对象.SetActive(value: true);
		}
	}

	private void 将领实例化血条信息()
	{
		血条信息对象 = UnityEngine.Object.Instantiate(全局变量.将领血条信息pre);
		血条信息对象.transform.parent = base.transform;
		血条信息对象.transform.localPosition = new Vector2(0f, 2f);
		统兵对象 = 血条信息对象.transform.GetChild(0).GetChild(0).GetComponent<Text>();
		if (统兵对象) 统兵对象.text = 本将领信息.详细信息.剩余兵力.ToString();
		血条位置对象 = 血条信息对象.transform.GetChild(3);
		血条位置对象.localPosition = new Vector2(3f, -0.15f);
		进度条位置对象 = 血条信息对象.transform.GetChild(5);
		血条信息对象.SetActive(value: false);
	}

	public void 将领显示血条()
	{
		血条信息对象.SetActive(value: true);
		更新名字位置();
	}

	private void 将领实例化闪特效()
	{
		闪特效对象 = UnityEngine.Object.Instantiate(全局变量.闪pre);
		闪特效对象.transform.parent = base.transform;
		闪特效对象.transform.localPosition = new Vector2(0f, 0f);
		闪特效对象.SetActive(value: false);
	}

	private void 将领实例化挡特效()
	{
		挡特效对象 = UnityEngine.Object.Instantiate(全局变量.挡pre);
		挡特效对象.transform.parent = base.transform;
		挡特效对象.transform.localPosition = new Vector2(0f, 0f);
		挡特效对象.SetActive(value: false);
	}

	private void 将领实例化中特效()
	{
		中特效对象 = UnityEngine.Object.Instantiate(全局变量.中pre);
		中特效对象.transform.parent = base.transform;
		中特效对象.transform.localPosition = new Vector2(0f, 0f);
		中特效对象.SetActive(value: false);
	}

	public void 将领显示闪特效()
	{
		闪特效对象.SetActive(value: true);
	}

	public void 将领隐藏闪特效()
	{
		闪特效对象.SetActive(value: false);
	}

	public void 将领显示挡特效()
	{
		挡特效对象.SetActive(value: true);
	}

	public void 将领隐藏中特效()
	{
		中特效对象.SetActive(value: false);
	}

	public void 将领显示中特效()
	{
		中特效对象.SetActive(value: true);
	}

	public void 将领隐藏挡特效()
	{
		挡特效对象.SetActive(value: false);
	}

	private void 将领实例化打击特效()
	{
		打击特效对象 = UnityEngine.Object.Instantiate(全局变量.打击特效pre);
		打击特效对象.transform.parent = base.transform;
		打击特效对象.transform.localPosition = new Vector2(0f, 0f);
		打击特效对象.SetActive(value: false);
	}

	public void 将领显示打击特效()
	{
		打击特效对象.SetActive(value: true);
	}

	public void 将领隐藏打击特效()
	{
		打击特效对象.SetActive(value: false);
	}

	private void 将领显示阴影()
	{
		GameObject gameObject = UnityEngine.Object.Instantiate(全局变量.模型阴影pre);
		gameObject.transform.parent = base.transform;
		gameObject.transform.localPosition = new Vector2(0f, -3f);
	}

	private void 将领显示模型()
	{
		int num = 全局兵种库.查询指定兵种的模型(全局兵种库.属性表[兵种索引].名称);
		if (num != -1)
		{
			对象 = UnityEngine.Object.Instantiate(全局变量.所有兵种模型[num]);
			对象.transform.parent = base.transform;
			对象.transform.localPosition = new Vector2(0f, 0f);
			if (本将领信息.详细信息.坑位颜色 == 1.0)
			{
				对象.transform.localScale = new Vector2(-1f, 1f);
			}
			模型动画对象 = 对象.GetComponent<Animator>();
			设置等待状态();
		}
	}

	public void 设置将领模型图层(int 第几层)
	{
		base.transform.GetChild(2).GetComponent<SpriteRenderer>().sortingOrder = 第几层;
	}

	private void 更新显示统兵()
	{
		if (统兵对象) 统兵对象.text = 本将领信息.详细信息.剩余兵力.ToString();
	}

	private void 更新名字位置()
	{
		名字对象.transform.localPosition = new Vector2(0f, 3.4f);
	}

	private void 将领显示名字()
	{
		名字对象 = UnityEngine.Object.Instantiate(全局变量.将领名字pre);
		名字对象.transform.parent = base.transform;
		名字对象.transform.localPosition = new Vector2(0f, 2.5f);
		Text component = 名字对象.transform.GetChild(0).GetChild(0).GetComponent<Text>();
		component.text = 本将领信息.将领属性.初始属性.名字;
		switch (本将领信息.详细信息.判断身份())
		{
			case 0:
				component.color = 颜色类.GetColor("#0A83FF");
				break;
			case 1:
				component.color = 颜色类.GetColor("#FFC847");
				break;
			case 2:
				component.color = new Color(0.94f, 0.09f, 0.05f, 1f);
				break;
		}
	}

	private void 设置攻击状态()
	{
		if ((bool)模型动画对象)
		{
			模型动画对象.SetInteger("状态", 3);
		}
	}

	public void 设置死亡状态()
	{
		if ((bool)模型动画对象)
		{
			模型动画对象.SetInteger("状态", 4);
		}
	}

	public void 设置走动状态()
	{
		if ((bool)模型动画对象)
		{
			模型动画对象.SetInteger("状态", 1);
		}
	}

	public void 设置跑路状态()
	{
		if ((bool)模型动画对象)
		{
			模型动画对象.SetInteger("状态", 2);
		}
	}

	public void 设置等待状态()
	{
		if ((bool)模型动画对象)
		{
			模型动画对象.SetInteger("状态", 0);
		}
	}

	// 自动死亡、战斗结束和原单将撤退按钮共用同一退场路径。
	public void 退出战场(bool 主动撤退 = false, bool 播放死亡动画 = true)
	{
		if (已退出战场) return;
		已退出战场 = true;
		if (本将领信息 != null && 本将领信息.详细信息 != null)
		{
			double 剩余 = 本将领信息.详细信息.剩余兵力;
			if (!double.IsNaN(剩余) && !double.IsInfinity(剩余))
			{
				剩余 = System.Math.Max(0, 剩余);
				本将领信息.详细信息.剩余兵力 = 剩余;
				if (本将领信息.将领配兵 != null)
					本将领信息.将领配兵.数量 = System.Math.Min(本将领信息.将领配兵.数量, 剩余);
			}
			else 剩余 = 0;
			bool 驻防接管 = 和平驻防规则.接管战后返回(本将领信息, TIME.getTime(), 主动撤退);
			if (!驻防接管 && 本将领信息.详细信息.状态 == 1.0)
				本将领信息.详细信息.状态 = 0.0;
			if (战斗系统脚本对象)
			{
				if (本将领信息.详细信息.坑位颜色 == 0.0)
					战斗系统脚本对象.攻方兵力 = System.Math.Max(0, 战斗系统脚本对象.攻方兵力 - 剩余);
				else if (本将领信息.详细信息.坑位颜色 == 1.0)
					战斗系统脚本对象.守方兵力 = System.Math.Max(0, 战斗系统脚本对象.守方兵力 - 剩余);
			}
			更新显示统兵();
		}
		if (播放死亡动画) 设置死亡状态();
		UnityEngine.Object.Destroy(base.gameObject, 播放死亡动画 ? 0.5f : 0f);
	}

	private void FixedUpdate()
	{
		if (已退出战场 || !战斗系统脚本对象 || 本将领信息 == null || 本将领信息.详细信息 == null) return;
		bool 攻方撤退 = 战斗系统脚本对象.全军撤退 && 本将领信息.详细信息.坑位颜色 == 0.0;
		if (战斗系统脚本对象.全军撤退 || 战斗系统脚本对象.战斗结束)
			退出战场(攻方撤退, false);
	}
}
