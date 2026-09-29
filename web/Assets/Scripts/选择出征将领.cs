using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using 玩家数据结构;


public class 选择出征将领 : MonoBehaviour
{
	private int 第几个玩家 => 全局变量.本机身份;

	private int 当前选中封地 = 全局变量.第几个封地;

	public int index;

	public int 山贼坐标x;

	public int 山贼坐标y;

	public int 城池坐标x;

	public int 城池坐标y;

	public GameObject 山贼信息对象;

	public GameObject 编队切换对象;

	public GameObject 编队将领对象;

	public Text 精准到达_时;

	public Text 精准到达_分;

	public Text 精准到达_秒;

	private List<GameObject> 所有列表将领对象 = new List<GameObject>();

	private List<返回将领索引> 已显示将领列表 = new List<返回将领索引>();

	private List<返回将领索引> 已选中将领列表 = new List<返回将领索引>();

	private bool 正在提交山贼出征;
	private bool 正在提交补兵;

	public GameObject 战斗界面UI;

	public 战斗系统 战斗系统对象;

	public void 加入军情队列(int 战场类型, int 坐标x, int 坐标y, long 到达时间, List<将领信息> 要加入的将领列表,bool 是否为增援=false)
	{
		军情信息 军情信息 = new 军情信息();
		军情信息.战场类型 = 战场类型;
		军情信息.坐标x = 坐标x;
		军情信息.坐标y = 坐标y;
		军情信息.到达时间 = 到达时间;
		军情信息.队列将领列表 = 要加入的将领列表;
		军情信息.已进入战场 = 是否为增援;
		全局变量.军情列表.Add(军情信息);
	}

	public void 山贼_出征选中将领()
	{
		if (Dwsg.Network.GameNetwork.Enabled)
		{
			if (正在提交山贼出征 || 已选中将领列表.Count == 0) return;
			Newtonsoft.Json.Linq.JArray 将领ID列表 = new Newtonsoft.Json.Linq.JArray();
			foreach (返回将领索引 选中 in 已选中将领列表)
			{
				将领信息 将领 = 全局变量.所有玩家数据表[全局变量.本机身份].封地信息表[选中.第几个封地].将领信息表[选中.第几个将领];
				string 将领ID = Dwsg.Combat.CombatClient.GeneralId(将领.ID);
				if (将领ID == null) { 全局变量.提示类.显示信息("请等待将领同步后出征"); return; }
				将领ID列表.Add(将领ID);
			}
			正在提交山贼出征 = true;
			Dwsg.Network.GameNetwork.SendCommand("combat.bandit.dispatch", new Newtonsoft.Json.Linq.JObject {
				["x"] = 山贼坐标x, ["y"] = 山贼坐标y, ["generalIds"] = 将领ID列表 }, 结果 => {
				正在提交山贼出征 = false;
				if (结果.Code != Dwsg.Shared.GameCodes.Ok) { 全局变量.提示类.显示信息(结果.Message); return; }
				已选中将领列表.Clear(); 显示编队将领列表(); 全局变量.提示类.显示信息("出征成功!");
			});
			return;
		}
		List<将领信息> list = new List<将领信息>();
		int count = 已选中将领列表.Count;
		for (int i = 0; i < count; i++)
		{
			返回将领索引 返回将领索引 = 已选中将领列表[i];
			全局变量.所有玩家数据表[全局变量.本机身份].封地信息表[返回将领索引.第几个封地].将领信息表[返回将领索引.第几个将领].详细信息.坑位颜色 = 0.0;
			全局变量.所有玩家数据表[全局变量.本机身份].封地信息表[返回将领索引.第几个封地].将领信息表[返回将领索引.第几个将领].详细信息.状态 = 1.0;
			list.Add(全局变量.所有玩家数据表[全局变量.本机身份].封地信息表[返回将领索引.第几个封地].将领信息表[返回将领索引.第几个将领]);
		}
		if (count != 0)
		{
			long 到达时间 = TIME.getTime() + 10;
			加入军情队列(0, 山贼坐标x, 山贼坐标y, 到达时间, list);
			已选中将领列表.Clear();
			显示编队将领列表();
			全局变量.提示类.显示信息("出征成功!");
		}
	}

	public void 城池_出征选中将领()
	{
		if (Dwsg.Network.GameNetwork.Enabled)
		{
			全局变量.提示类.显示信息("城池出征尚未接入服务器，操作未提交");
			return;
		}
		if (index==1)
		{
            List<将领信息> list = new List<将领信息>();
            int count = 已选中将领列表.Count;
            for (int i = 0; i < count; i++)
            {
                返回将领索引 返回将领索引 = 已选中将领列表[i];
                全局变量.所有玩家数据表[全局变量.本机身份].封地信息表[返回将领索引.第几个封地].将领信息表[返回将领索引.第几个将领].详细信息.坑位颜色 = 1.0;
                全局变量.所有玩家数据表[全局变量.本机身份].封地信息表[返回将领索引.第几个封地].将领信息表[返回将领索引.第几个将领].详细信息.状态 = 1.0;
                list.Add(全局变量.所有玩家数据表[全局变量.本机身份].封地信息表[返回将领索引.第几个封地].将领信息表[返回将领索引.第几个将领]);
            }
            if (count != 0)
            {
                long num = TIME.getTime() + 5;
                军情信息 军情信息 = new 军情信息();
                军情信息.战场类型 = 1;
                军情信息.坐标x = 城池坐标x;
                军情信息.坐标y = 城池坐标y;
                军情信息.到达时间 = num;
                军情信息.队列将领列表 = list;
                军情信息.身份 = 78;
                全局变量.军情列表.Add(军情信息);
                已选中将领列表.Clear();
                显示编队将领列表();
                全局变量.提示类.显示信息("出征成功!");
            }
        }
		else
		{
            List<将领信息> list = new List<将领信息>();
            int count = 已选中将领列表.Count;
            for (int i = 0; i < count; i++)
            {
                返回将领索引 返回将领索引 = 已选中将领列表[i];
                全局变量.所有玩家数据表[全局变量.本机身份].封地信息表[返回将领索引.第几个封地].将领信息表[返回将领索引.第几个将领].详细信息.坑位颜色 = 0.0;
                全局变量.所有玩家数据表[全局变量.本机身份].封地信息表[返回将领索引.第几个封地].将领信息表[返回将领索引.第几个将领].详细信息.状态 = 1.0;
                list.Add(全局变量.所有玩家数据表[全局变量.本机身份].封地信息表[返回将领索引.第几个封地].将领信息表[返回将领索引.第几个将领]);
            }
            if (count != 0)
            {
                long num = TIME.getTime() + 10;
                if (精准到达_时.text != "" && 精准到达_分.text != "" && 精准到达_秒.text != "")
                {
                    DateTime dateTime = TIME.TimeStampToDateTime(num);
                    num = TIME.DateTimeToTimeStamp(new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, int.Parse(精准到达_时.text), int.Parse(精准到达_分.text), int.Parse(精准到达_秒.text)));
                }
                加入军情队列(1, 城池坐标x, 城池坐标y, num, list);
                已选中将领列表.Clear();
                显示编队将领列表();
                全局变量.提示类.显示信息("出征成功!");
            }
        }
		
	}

    public void 城池_驻防选中将领()
    {
        
    }

    public void 增援()
	{

        战斗系统对象 = 战斗界面UI.GetComponent<战斗界面UI脚本>().战斗地图对象.transform.GetChild(0).GetChild(0).GetChild(0)
            .GetChild(0)
            .GetChild(0)
            .GetComponent<战斗系统>();
		if (战斗系统对象.攻身份 != 全局变量.本机身份)
			return;
		if (Dwsg.Network.GameNetwork.Enabled && 战斗系统对象.服务器战场)
		{
			if (正在提交山贼出征 || 已选中将领列表.Count == 0) return;
			Newtonsoft.Json.Linq.JArray 将领ID列表 = new Newtonsoft.Json.Linq.JArray();
			foreach (返回将领索引 选中 in 已选中将领列表)
			{
				将领信息 将领 = 全局变量.所有玩家数据表[第几个玩家].封地信息表[选中.第几个封地].将领信息表[选中.第几个将领];
				string 将领ID = Dwsg.Combat.CombatClient.GeneralId(将领.ID);
				if (将领ID == null) { 全局变量.提示类.显示信息("请等待将领同步后增援"); return; }
				将领ID列表.Add(将领ID);
			}
			正在提交山贼出征 = true;
			Dwsg.Network.GameNetwork.SendCommand("combat.bandit.reinforce", new Newtonsoft.Json.Linq.JObject {
				["battleId"] = 战斗系统对象.服务器战场ID, ["generalIds"] = 将领ID列表 }, 结果 => {
				正在提交山贼出征 = false;
				if (结果.Code != Dwsg.Shared.GameCodes.Ok) { 全局变量.提示类.显示信息(结果.Message); return; }
				已选中将领列表.Clear(); 显示编队将领列表(); 全局变量.提示类.显示信息("增援成功!");
			});
			return;
		}
		if (Dwsg.Network.GameNetwork.Enabled && 战斗系统对象.战场类型 == 1)
		{
			全局变量.提示类.显示信息("城池增援尚未接入服务器，操作未提交");
			return;
		}
		List<将领信息> list = new List<将领信息>();
		int count = 已选中将领列表.Count;
		for (int i = 0; i < count; i++)
		{
			返回将领索引 返回将领索引 = 已选中将领列表[i];
			全局变量.所有玩家数据表[全局变量.本机身份].封地信息表[返回将领索引.第几个封地].将领信息表[返回将领索引.第几个将领].详细信息.坑位颜色 = 0.0;
			全局变量.所有玩家数据表[全局变量.本机身份].封地信息表[返回将领索引.第几个封地].将领信息表[返回将领索引.第几个将领].详细信息.状态 = 1.0;
			list.Add(全局变量.所有玩家数据表[全局变量.本机身份].封地信息表[返回将领索引.第几个封地].将领信息表[返回将领索引.第几个将领]);
		}


		if (战斗系统对象.战场类型 == 1)
		{
			if (count != 0)
			{
                加入军情队列(1, 战斗系统对象.坐标x, 战斗系统对象.坐标y, 0, list, true);
                战斗系统对象.攻方要渲染的编队将领列表.Add(list);
                已选中将领列表.Clear();
				显示编队将领列表();
				全局变量.提示类.显示信息("增援成功!");

			}
		}
		else if (战斗系统对象.战场类型 == 0)
		{
			if (count != 0)
			{
                加入军情队列(0, 战斗系统对象.坐标x, 战斗系统对象.坐标y, 0, list,true);
                战斗系统对象.攻方要渲染的编队将领列表.Add(list);
                已选中将领列表.Clear();
				显示编队将领列表();
				全局变量.提示类.显示信息("增援成功!");
			}
		}
	}


	public void 批量补兵()
	{
		选中将领批量补兵();
		显示编队将领列表();
	}

	public void 刷新山贼()
	{
		已选中将领列表.Clear();
		显示山贼详情();
		显示编队将领列表();
	}

	public void 刷新城池()
	{
		已选中将领列表.Clear();
		显示城池详情();
		显示编队将领列表();
	}

	private void 显示城池详情()
	{
		城池信息库类 城池信息库类 = 所有城池界面脚本.根据坐标获取指定城池(城池坐标x, 城池坐标y);
		山贼信息对象.transform.GetChild(0).GetChild(0).GetComponent<Image>()
			.sprite = 全局变量.城池规模头像资源表[城池信息库类.规模];
		山贼信息对象.transform.GetChild(0).GetChild(1).GetComponent<Text>()
			.text = 城池信息库类.名称 + "(" + 城池信息库类.获取规模名称() + "城" + 城池信息库类.坐标x.ToString() + "," + 城池信息库类.坐标y.ToString() + ")  国家:" + 城池信息库类.获取国家名字();
		山贼信息对象.transform.GetChild(0).GetChild(2).GetComponent<Text>()
			.text = "天赋:" + 城池信息库类.获取天赋类型名称() + "+" + 城池信息库类.天赋加成.ToString() + "%  封地:" + Dwsg.Network.GameNetwork.GetCityFiefCount(全局变量.所有城池列表.IndexOf(城池信息库类)).ToString() + "/" + 城池信息库类.获取封地上限().ToString();
		山贼信息对象.transform.GetChild(0).GetChild(3).GetComponent<Text>()
			.text = "城主:" + 城池信息库类.获取城主名字();
		山贼信息对象.transform.GetChild(2).GetChild(0).GetComponent<Text>()
			.text = "驻防将领:" + 城池信息库类.城池驻防列表.Count.ToString() + "/" + 城池信息库类.获取驻防上限().ToString();
		山贼信息对象.transform.GetChild(2).GetChild(1).GetComponent<Text>()
			.text = "道路:" + 城池信息库类.获取道路上限().ToString();
		山贼信息对象.transform.GetChild(2).GetChild(2).GetComponent<Text>()
			.text = "城墙:" + 城池信息库类.城墙.ToString();
		山贼信息对象.transform.GetChild(2).GetChild(3).GetChild(0)
			.GetComponent<Text>()
			.text = "【名将协防】:\r\n名将协防几率:" + 城池信息库类.协防几率.ToString() + "%\r\n名将数量: " + 城池信息库类.协防数量f.ToString() + "-" + 城池信息库类.协防数量m.ToString() + "\r\n【攻克奖励】\r\n战功 + " + 城池信息库类.战功.ToString() + "\r\n国库铜钱 + 100000\r\n国库粮食 + 200000";
	}

	private void 显示山贼详情()
	{
		山贼属性信息 山贼属性信息 = 附近山贼.获取指定坐标的山贼(山贼坐标x, 山贼坐标y);
		if (山贼属性信息 != null)
		{
			double 等级 = 山贼属性信息.等级;
			Image component = 山贼信息对象.transform.GetChild(0).GetChild(0).GetComponent<Image>();
			int num = (int)等级 - 1;
			component.sprite = 全局变量.山贼头像资源表[num];
			山贼信息对象.transform.GetChild(0).GetChild(1).GetComponent<Text>()
				.text = 等级.ToString() + "级山贼 (" + 山贼坐标x.ToString() + "," + 山贼坐标y.ToString() + ")";
			int count = 山贼属性信息.将领数据列表.Count;
			string text = "";
			double num2 = 0.0;
			for (int i = 0; i < count; i++)
			{
				double iD = 山贼属性信息.将领数据列表[i].将领配兵.ID;
				double 数量 = 山贼属性信息.将领数据列表[i].将领配兵.数量;
				num2 += 数量;
				int num3 = 全局兵种库.查询指定ID的索引(iD);
				text = ((num3 == -1) ? (text + "未知 " + 数量.ToString() + "\n") : (text + 全局兵种库.属性表[num3].名称 + " " + 数量.ToString() + "\n"));
			}
			山贼信息对象.transform.GetChild(0).GetChild(2).GetComponent<Text>()
				.text = "兵力:" + num2.ToString();
			string text2 = "奖励资源";
			if (山贼属性信息.掉落宝物 == 1.0)
			{
				text2 += "、宝物";
			}
			if (山贼属性信息.掉落宝箱 == 1.0)
			{
				text2 += "、宝箱";
			}
			if (山贼属性信息.掉落装备 == 1.0)
			{
				text2 += "、装备";
			}
			山贼信息对象.transform.GetChild(0).GetChild(3).GetComponent<Text>()
				.text = text2;
			山贼信息对象.transform.GetChild(2).GetChild(0).GetComponent<Text>()
				.text = "敌军等级:" + 山贼属性信息.将领数据列表[0].将领属性.成长点数.等级.ToString() + "级";
			山贼信息对象.transform.GetChild(2).GetChild(1).GetComponent<Text>()
				.text = "敌军规模:" + count.ToString() + "名";
			山贼信息对象.transform.GetChild(2).GetChild(3).GetChild(0)
				.GetComponent<Text>()
				.text = text;
		}
	}

	private void 显示编队将领列表()
	{
		已显示将领列表.Clear();
		int count = 全局变量.所有玩家数据表[第几个玩家].封地信息表.Count;
		int num = 0;
		for (int i = 0; i < count; i++)
		{
			int count2 = 全局变量.所有玩家数据表[第几个玩家].封地信息表[i].将领信息表.Count;
			for (int j = 0; j < count2; j++)
			{
				double 编队 = 全局变量.所有玩家数据表[第几个玩家].封地信息表[i].将领信息表[j].详细信息.编队;
				double iD2 = 全局变量.所有玩家数据表[第几个玩家].封地信息表[i].将领信息表[j].将领属性.初始属性.ID;
				bool flag = false;
				if (编队切换对象.transform.GetChild(0).GetComponent<Toggle>().isOn)
				{
					if (i == 当前选中封地)
					{
						flag = true;
					}
				}
				else if (编队切换对象.transform.GetChild(1).GetComponent<Toggle>().isOn)
				{
					if (编队 == 1.0)
					{
						flag = true;
					}
				}
				else if (编队切换对象.transform.GetChild(2).GetComponent<Toggle>().isOn)
				{
					if (编队 == 2.0)
					{
						flag = true;
					}
				}
				else if (编队切换对象.transform.GetChild(3).GetComponent<Toggle>().isOn)
				{
					if (编队 == 3.0)
					{
						flag = true;
					}
				}
				else if (编队切换对象.transform.GetChild(4).GetComponent<Toggle>().isOn)
				{
					if (编队 == 4.0)
					{
						flag = true;
					}
				}
				else if (编队切换对象.transform.GetChild(5).GetComponent<Toggle>().isOn && 编队 == 5.0)
				{
					flag = true;
				}
				if (!flag)
				{
					continue;
				}
				GameObject gameObject;
				if (所有列表将领对象.Count <= num)
				{
					gameObject = UnityEngine.Object.Instantiate(编队将领对象.transform.GetChild(0).gameObject);
					gameObject.transform.SetParent(编队将领对象.transform);
					gameObject.transform.localScale = new Vector3(1f, 1f, 1f);
					所有列表将领对象.Add(gameObject);
				}
				else
				{
					gameObject = 所有列表将领对象[num];
				}
				gameObject.gameObject.SetActive(value: true);
				gameObject.transform.GetChild(8).gameObject.SetActive(value: false);
				将领属性库类 将领属性库类 = 全局将领库.查询指定ID的将领数据(全局变量.所有玩家数据表[第几个玩家].封地信息表[i].将领信息表[j].将领属性.初始属性.ID);
				if (将领属性库类 != null)
				{
					gameObject.transform.GetChild(1).GetChild(0).GetComponent<Image>()
						.sprite = 全局将领库.获取指定将领的头像(将领属性库类.名字);
					Animator component = gameObject.transform.GetChild(1).GetChild(1).GetComponent<Animator>();
					gameObject.transform.GetChild(1).GetChild(1).gameObject.SetActive(value: false);
					if (将领属性库类.头像特效 != 0.0)
					{
						gameObject.transform.GetChild(1).GetChild(1).gameObject.SetActive(value: true);
						component.SetInteger("特效类型", (int)将领属性库类.头像特效);
					}
				}
				Text component2 = gameObject.transform.GetChild(2).GetComponent<Text>();
				string 名字 = 全局变量.所有玩家数据表[第几个玩家].封地信息表[i].将领信息表[j].将领属性.初始属性.名字;
				double 等级 = 全局变量.所有玩家数据表[第几个玩家].封地信息表[i].将领信息表[j].将领属性.成长点数.等级;
				string text = 全局变量.所有玩家数据表[第几个玩家].封地信息表[i].将领信息表[j].将领属性.初始属性.获取职业名字();
				component2.text = 名字 + "(" + 等级.ToString() + "级" + text + ")";
				Text component3 = gameObject.transform.GetChild(4).GetComponent<Text>();
				double iD = 全局变量.所有玩家数据表[第几个玩家].封地信息表[i].将领信息表[j].将领配兵.ID;
				if (iD != 0.0)
				{
					double 数量 = 全局变量.所有玩家数据表[第几个玩家].封地信息表[i].将领信息表[j].将领配兵.数量;
					double 统兵 = 全局变量.所有玩家数据表[第几个玩家].封地信息表[i].将领信息表[j].将领属性.最终属性.统兵;
					string text2 = "未知";
					if (全局兵种库.查询指定ID的索引(iD) != -1)
					{
						text2 = 全局兵种库.属性表[全局兵种库.查询指定ID的索引(iD)].名称;
						Image component4 = gameObject.transform.GetChild(3).GetComponent<Image>();
						int num2 = 全局兵种库.查询指定兵种的图标(text2);
						if (num2 != -1)
						{
							component4.sprite = 全局变量.所有兵种图标资源表[num2];
						}
					}
					component3.text = 数量.ToString() + "/" + 统兵.ToString() + "(" + text2 + ")";
				}
				else
				{
					gameObject.transform.GetChild(8).gameObject.SetActive(value: true);
					component3.text = "未配兵";
				}
				Text component5 = gameObject.transform.GetChild(6).GetComponent<Text>();
				component5.text = 全局变量.所有玩家数据表[第几个玩家].封地信息表[i].将领信息表[j].详细信息.剩余体力.ToString() + "/" + 全局变量.所有玩家数据表[第几个玩家].封地信息表[i].将领信息表[j].将领属性.最终属性.体力上限.ToString();
				if (全局变量.所有玩家数据表[第几个玩家].封地信息表[i].将领信息表[j].详细信息.剩余体力 < 5.0)
				{
					gameObject.transform.GetChild(8).gameObject.SetActive(value: true);
				}
				if (全局变量.所有玩家数据表[第几个玩家].封地信息表[i].将领信息表[j].详细信息.状态 != 0.0)
				{
					gameObject.transform.GetChild(8).gameObject.SetActive(value: true);
				}
				if (i != 当前选中封地)
				{
					gameObject.transform.GetChild(8).gameObject.SetActive(value: true);
				}
				gameObject.transform.GetChild(7).gameObject.SetActive(value: false);
				if (!gameObject.transform.GetChild(8).gameObject.activeSelf)
				{
					if (gameObject.transform.GetChild(9).gameObject.activeSelf)
					{
						component2.color = 颜色类.GetColor("#49FBDA");
						component3.color = 颜色类.GetColor("#2DBE5A");
						component5.color = 颜色类.GetColor("#2DBE5A");
					}
					else
					{
						component2.color = 颜色类.GetColor("#C8C8C8");
						component3.color = 颜色类.GetColor("#C8C8C8");
						component5.color = 颜色类.GetColor("#C8C8C8");
					}
					bool flag2 = false;
					for (int k = 0; k < 已选中将领列表.Count; k++)
					{
						if (已选中将领列表[k].第几个将领 == j)
						{
							flag2 = true;
							break;
						}
					}
					if (flag2)
					{
						gameObject.transform.GetChild(7).gameObject.SetActive(value: true);
					}
				}
				已显示将领列表.Add(new 返回将领索引(i, j));
				if (已选中将领列表.Count > 4 && !gameObject.transform.GetChild(7).gameObject.activeSelf)
				{
					gameObject.transform.GetChild(8).gameObject.SetActive(value: true);
				}
				num++;
			}
		}
	}

	public void 输出列表()
	{
		for (int i = 0; i < 已选中将领列表.Count; i++)
		{
			UnityEngine.Debug.Log("已选：" + 已选中将领列表[i]?.ToString());
		}
		for (int j = 0; j < 已显示将领列表.Count; j++)
		{
			UnityEngine.Debug.Log("已显示" + 已显示将领列表[j]?.ToString());
		}
	}

	private void 隐藏所有列表将领对象()
	{
		已选中将领列表.Clear();
		已显示将领列表.Clear();
		int count = 所有列表将领对象.Count;
		for (int i = 0; i < count; i++)
		{
			所有列表将领对象[i].gameObject.SetActive(value: false);
		}
	}

	public void 勾选出征将领(int 勾选类型)
	{
		int count = 所有列表将领对象.Count;
		for (int i = 0; i < count; i++)
		{
			if (!所有列表将领对象[i].gameObject.activeSelf || 所有列表将领对象[i].transform.GetChild(8).gameObject.activeSelf)
			{
				continue;
			}
			bool flag = false;
			if (所有列表将领对象[i].transform.GetChild(9).gameObject.activeSelf && 勾选类型 == 1)
			{
				flag = true;
			}
			if (!编队切换对象.transform.GetChild(0).GetComponent<Toggle>().isOn && 勾选类型 == 0)
			{
				flag = true;
			}
			if (!flag)
			{
				continue;
			}
			bool flag2 = true;
			int count2 = 已选中将领列表.Count;
			for (int j = 0; j < 已选中将领列表.Count; j++)
			{
				if (已选中将领列表[j].第几个将领 == 已显示将领列表[i].第几个将领 && 已选中将领列表[j].第几个封地 == 已显示将领列表[i].第几个封地)
				{
					已选中将领列表.RemoveAt(j);
					flag2 = false;
					break;
				}
			}
			if (flag2 && count2 < 5)
			{
				已选中将领列表.Add(已显示将领列表[i]);
			}
		}
		显示编队将领列表();
	}

	private void 选中将领批量补兵()
	{
		if (Dwsg.Network.GameNetwork.Enabled)
		{
			if (正在提交补兵 || 已选中将领列表.Count == 0) return;
			Queue<string> 将领队列 = new Queue<string>();
			foreach (返回将领索引 选中 in 已选中将领列表)
			{
				将领信息 将领 = 全局变量.所有玩家数据表[第几个玩家].封地信息表[选中.第几个封地].将领信息表[选中.第几个将领];
				string 将领ID = Dwsg.Combat.CombatClient.GeneralId(将领.ID);
				if (将领ID == null) { 全局变量.提示类.显示信息("请等待将领同步后补兵"); return; }
				将领队列.Enqueue(将领ID);
			}
			正在提交补兵 = true;
			Action 补下一个 = null;
			补下一个 = () => {
				if (将领队列.Count == 0) { 正在提交补兵 = false; 显示编队将领列表(); return; }
				Dwsg.Network.GameNetwork.SendCommand("generals.refillTroops", new Newtonsoft.Json.Linq.JObject { ["generalId"] = 将领队列.Dequeue() }, 结果 => {
					if (结果.Code != Dwsg.Shared.GameCodes.Ok) 全局变量.提示类.显示信息(结果.Message);
					补下一个();
				});
			};
			补下一个();
			return;
		}
		int count = 已选中将领列表.Count;
		for (int i = 0; i < count; i++)
		{
			int 第几个将领 = 已选中将领列表[i].第几个将领;
			int 第几个封地 = 已选中将领列表[i].第几个封地;
			int count2 = 全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地].闲兵信息表.Count;
			for (int j = 0; j < count2; j++)
			{
				double 数量 = 全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地].闲兵信息表[j].数量;
				if (全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地].将领信息表[第几个将领].将领配兵.ID == (double)全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地].闲兵信息表[j].ID)
				{
					double num = 全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地].将领信息表[第几个将领].将领属性.最终属性.统兵 - 全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地].将领信息表[第几个将领].将领配兵.数量;
					if (全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地].闲兵信息表[j].数量 >= num)
					{
						全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地].将领信息表[第几个将领].将领配兵.数量 = 全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地].将领信息表[第几个将领].将领配兵.数量 + num;
						全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地].闲兵信息表[j].数量 = 全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地].闲兵信息表[j].数量 - num;
					}
					else
					{
						全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地].将领信息表[第几个将领].将领配兵.数量 = 全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地].将领信息表[第几个将领].将领配兵.数量 + 全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地].闲兵信息表[j].数量;
						全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地].闲兵信息表[j].数量 = 0.0;
					}
				}
			}
		}
	}

	public void 切换编队()
	{
		隐藏所有列表将领对象();
		显示编队将领列表();
		勾选出征将领(0);
	}
}
