using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using 玩家数据结构;
using 缺失界面.窗口4;


public class 选择出征将领 : MonoBehaviour
{
	private int 第几个玩家 = 全局变量.本机身份;

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
    private readonly Dictionary<返回将领索引, 将领信息> 选择快照 = new Dictionary<返回将领索引, 将领信息>();

    public void 切换出征封地(int 号)
    {
        var 玩家 = 军事缺口入口.当前玩家();
        if (玩家 == null || 号 < 0 || 号 >= 玩家.封地信息表.Count) return;
        第几个玩家 = 全局变量.本机身份;
        当前选中封地 = 号;
        已选中将领列表.Clear();
        选择快照.Clear();
        显示编队将领列表();
    }

    private bool 校验选中将领(out List<将领信息> 将领表)
    {
        将领表 = new List<将领信息>();
        var 玩家 = 军事缺口入口.当前玩家();
        if (玩家 == null) { 全局变量.提示类.显示信息("本地角色尚未载入。"); return false; }
        foreach (var 项 in 已选中将领列表)
        {
            if (项.第几个封地 < 0 || 项.第几个封地 >= 玩家.封地信息表.Count) return 出征选择已变更();
            var 封地 = 玩家.封地信息表[项.第几个封地];
            if (项.第几个将领 < 0 || 项.第几个将领 >= 封地.将领信息表.Count) return 出征选择已变更();
            将领信息 快照;
            var 将 = 封地.将领信息表[项.第几个将领];
            if (!选择快照.TryGetValue(项, out 快照) || !ReferenceEquals(快照, 将) || 项.第几个封地 != 当前选中封地) return 出征选择已变更();
            将领表.Add(将);
        }
        var 结果 = 军事本地规则.检查出征(玩家, 将领表);
        if (!结果.成功) 全局变量.提示类.显示信息(结果.说明);
        return 结果.成功;
    }

    private bool 出征选择已变更()
    {
        已选中将领列表.Clear();
        选择快照.Clear();
        显示编队将领列表();
        全局变量.提示类.显示信息("将领或封地已变更，请重新选择。");
        return false;
    }

    private void OnEnable()
    {
        第几个玩家 = 全局变量.本机身份;
        var 玩家 = 军事缺口入口.当前玩家();
        当前选中封地 = 玩家 != null && 玩家.封地信息表.Count > 0 ? Mathf.Clamp(全局变量.第几个封地, 0, 玩家.封地信息表.Count - 1) : -1;
        已选中将领列表.Clear();
        选择快照.Clear();
        if (玩家 != null) 显示编队将领列表();
    }

    private void 标记出征(List<将领信息> 将领表, int 阵营)
    {
        foreach (var 将 in 将领表) { 将.详细信息.坑位颜色 = 阵营; 将.详细信息.状态 = 1; }
        已选中将领列表.Clear();
        选择快照.Clear();
        显示编队将领列表();
    }

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
        List<将领信息> 将领表;
        if (附近山贼.获取指定坐标的山贼(山贼坐标x, 山贼坐标y) == null) { 全局变量.提示类.显示信息("山贼目标已变更。"); return; }
        if (!校验选中将领(out 将领表)) return;
        加入军情队列(0, 山贼坐标x, 山贼坐标y, TIME.getTime() + 10, 将领表);
        标记出征(将领表, 0);
        全局变量.提示类.显示信息("本地出征成功。");
    }

    private static string 读取到达时间(Text 字)
    {
        if (字 == null) return "";
        var 输入 = 字.GetComponentInParent<InputField>();
        return (输入 != null ? 输入.text : 字.text).Trim();
    }

	public void 城池_出征选中将领()
	{
        List<将领信息> 将领表;
        if (所有城池界面脚本.根据坐标获取指定城池(城池坐标x, 城池坐标y) == null) { 全局变量.提示类.显示信息("城池目标已变更。"); return; }
        if (!校验选中将领(out 将领表)) return;
        long 到达 = TIME.getTime() + (index == 1 ? 5 : 10);
        string 时文 = 读取到达时间(精准到达_时);
        string 分文 = 读取到达时间(精准到达_分);
        string 秒文 = 读取到达时间(精准到达_秒);
        if (index != 1 && (时文.Length + 分文.Length + 秒文.Length > 0))
        {
            int 时, 分, 秒;
            if (!int.TryParse(时文, out 时) || !int.TryParse(分文, out 分) || !int.TryParse(秒文, out 秒) || 时 < 0 || 时 > 23 || 分 < 0 || 分 > 59 || 秒 < 0 || 秒 > 59)
            { 全局变量.提示类.显示信息("精准到达时间无效，请完整填写时、分、秒，或全部留空。"); return; }
            var 日期 = TIME.TimeStampToDateTime(到达);
            var 指定 = new DateTime(日期.Year, 日期.Month, 日期.Day, 时, 分, 秒);
            long 时间 = TIME.DateTimeToTimeStamp(指定);
            if (时间 < 到达) { 全局变量.提示类.显示信息("精准到达时间必须至少晚于当前时间10秒。"); return; }
            到达 = 时间;
        }
        if (index == 1)
            全局变量.军情列表.Add(new 军情信息 { 战场类型 = 1, 坐标x = 城池坐标x, 坐标y = 城池坐标y, 到达时间 = 到达, 队列将领列表 = 将领表, 身份 = 78 });
        else 加入军情队列(1, 城池坐标x, 城池坐标y, 到达, 将领表);
        标记出征(将领表, index == 1 ? 1 : 0);
        全局变量.提示类.显示信息("本地出征成功。");
    }

    public void 城池_驻防选中将领()
    {
        全局变量.提示类.显示信息("驻防派遣/撤回结算尚未接入，未占用将领或创建军情。");
    }

    public void 增援()
	{
        if (战斗界面UI == null) return;
        var 界面 = 战斗界面UI.GetComponent<战斗界面UI脚本>();
        战斗系统对象 = 界面 != null ? 界面.战斗系统脚本对象 : null;
        if (战斗系统对象 == null || 战斗系统对象.攻身份 != 全局变量.本机身份)
        { 全局变量.提示类.显示信息("当前战场不允许本机增援。"); return; }
        List<将领信息> 将领表;
        if (!校验选中将领(out 将领表)) return;
        加入军情队列(战斗系统对象.战场类型, 战斗系统对象.坐标x, 战斗系统对象.坐标y, 0, 将领表, true);
        战斗系统对象.攻方要渲染的编队将领列表.Add(将领表);
        标记出征(将领表, 0);
        全局变量.提示类.显示信息("本地增援成功。");
    }


	public void 批量补兵()
	{
		选中将领批量补兵();
		显示编队将领列表();
	}

	public void 刷新山贼()
	{
        已选中将领列表.Clear();
        选择快照.Clear();
        if (附近山贼.获取指定坐标的山贼(山贼坐标x, 山贼坐标y) != null) 显示山贼详情();
        显示编队将领列表();
    }

	public void 刷新城池()
	{
        已选中将领列表.Clear();
        选择快照.Clear();
        if (所有城池界面脚本.根据坐标获取指定城池(城池坐标x, 城池坐标y) != null) 显示城池详情();
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
			.text = "天赋:" + 城池信息库类.获取天赋类型名称() + "+" + 城池信息库类.天赋加成.ToString() + "%  封地:" + 城池信息库类.城池封地列表.Count.ToString() + "/" + 城池信息库类.获取封地上限().ToString();
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
        第几个玩家 = 全局变量.本机身份;
        隐藏所有列表将领对象();
        已显示将领列表.Clear();
        if (军事缺口入口.当前玩家() == null) return;
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
                    foreach (var 按 in gameObject.GetComponentsInChildren<Button>(true)) 界面窗口管理器.注册运行时按钮(按);
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
						if (已选中将领列表[k].第几个将领 == j && 已选中将领列表[k].第几个封地 == i)
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
					选择快照.Remove(已选中将领列表[j]);
                    已选中将领列表.RemoveAt(j);
					flag2 = false;
					break;
				}
			}
			if (flag2 && count2 < 5)
			{
				var 项 = 已显示将领列表[i];
                var 玩家 = 军事缺口入口.当前玩家();
                if (玩家 == null || 项.第几个封地 != 当前选中封地) continue;
                var 将 = 玩家.封地信息表[项.第几个封地].将领信息表[项.第几个将领];
                if (!军事本地规则.检查出征(玩家, new List<将领信息> { 将 }).成功) continue;
                已选中将领列表.Add(项);
                选择快照[项] = 将;
			}
		}
		显示编队将领列表();
	}

	private void 选中将领批量补兵()
	{
        var 玩家 = 军事缺口入口.当前玩家();
        if (玩家 == null) return;
        foreach (var 项 in 已选中将领列表)
        {
            将领信息 将;
            if (!选择快照.TryGetValue(项, out 将) || 项.第几个封地 < 0 || 项.第几个封地 >= 玩家.封地信息表.Count) continue;
            var 封地 = 玩家.封地信息表[项.第几个封地];
            int 兵种 = (int)将.将领配兵.ID;
            if (兵种 <= 0) continue;
            int 数量 = (int)Math.Floor(Math.Min(将.将领属性.最终属性.统兵, 将.将领配兵.数量 + 军事本地规则.闲兵数量(封地, 兵种)));
            var 结果 = 军事本地规则.配兵(玩家, 封地, 将, 兵种, 数量);
            if (!结果.成功) 全局变量.提示类.显示信息(结果.说明);
        }
    }

	public void 切换编队()
	{
		已选中将领列表.Clear();
        选择快照.Clear();
		隐藏所有列表将领对象();
		显示编队将领列表();
		勾选出征将领(0);
	}
}
