using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using 玩家数据结构;
using Dwsg.Network;
using Dwsg.Generals;
using Newtonsoft.Json.Linq;

public class 封地信息界面UI脚本 : MonoBehaviour
{
	public Toggle 俘虏选中开关;

	public Toggle 闲兵选中开关;

	public Toggle 伤兵选中开关;

	public GameObject 俘虏列表对象;

	public GameObject 闲兵列表对象;

	public GameObject 伤兵列表对象;

	private List<将领索引> 俘虏列表;

	private List<string> 联机俘虏编号 = new List<string>();

	public 调整数量脚本 调整数量脚本对象;

	public void 显示闲兵列表()
	{
		if (!闲兵选中开关.isOn)
		{
			return;
		}
		int 本机身份 = 全局变量.本机身份;
		int 第几个封地 = 全局变量.第几个封地;
		int childCount = 闲兵列表对象.transform.childCount;
		for (int i = 0; i < childCount; i++)
		{
			闲兵列表对象.transform.GetChild(i).gameObject.SetActive(value: false);
		}
		List<闲兵信息> 闲兵信息表 = 全局变量.所有玩家数据表[本机身份].封地信息表[第几个封地].闲兵信息表;
		int count = 闲兵信息表.Count;
		for (int j = 0; j < count; j++)
		{
			childCount = 闲兵列表对象.transform.childCount;
			GameObject gameObject;
			if (childCount <= j)
			{
				gameObject = UnityEngine.Object.Instantiate(闲兵列表对象.transform.GetChild(0).gameObject);
				gameObject.transform.SetParent(闲兵列表对象.transform);
				gameObject.transform.localScale = new Vector3(1f, 1f, 1f);
			}
			else
			{
				gameObject = 闲兵列表对象.transform.GetChild(j).gameObject;
			}
			gameObject.SetActive(value: true);
			兵种属性库类 兵种属性库类 = 全局兵种库.查询指定ID的数据(闲兵信息表[j].ID);
			if (兵种属性库类 != null)
			{
				int num = 全局兵种库.查询指定兵种的图片(兵种属性库类.名称);
				gameObject.transform.GetChild(1).GetComponent<Image>().sprite = 全局变量.所有兵种图片资源表[num];
				gameObject.transform.GetChild(2).GetComponent<Text>().text = 兵种属性库类.名称;
				gameObject.transform.GetChild(2).GetChild(0).GetComponent<Text>()
					.text = "(数量" + 闲兵信息表[j].数量.ToString() + ")";
				gameObject.transform.GetChild(4).GetComponent<Text>().text = 兵种属性库类.攻击力.ToString();
				gameObject.transform.GetChild(6).GetComponent<Text>().text = 兵种属性库类.防御力.ToString();
				gameObject.transform.GetChild(8).GetComponent<Text>().text = 兵种属性库类.生命值.ToString();
				gameObject.transform.GetChild(10).GetComponent<Text>().text = 兵种属性库类.攻击速度.ToString();
				gameObject.transform.GetChild(12).GetComponent<Text>().text = 兵种属性库类.移动速度.ToString();
				gameObject.transform.GetChild(14).GetComponent<Text>().text = 兵种属性库类.占用人口.ToString();
			}
		}
	}

	public void 显示伤兵列表()
	{
		if (!伤兵选中开关.isOn)
		{
			return;
		}
		int 本机身份 = 全局变量.本机身份;
		int 第几个封地 = 全局变量.第几个封地;
		int childCount = 伤兵列表对象.transform.childCount;
		for (int i = 0; i < childCount; i++)
		{
			伤兵列表对象.transform.GetChild(i).gameObject.SetActive(value: false);
		}
		List<伤兵信息> 伤兵信息表 = 全局变量.所有玩家数据表[本机身份].封地信息表[第几个封地].伤兵信息表;
		int count = 伤兵信息表.Count;
		for (int j = 0; j < count; j++)
		{
			childCount = 伤兵列表对象.transform.childCount;
			GameObject gameObject;
			if (childCount <= j)
			{
				gameObject = UnityEngine.Object.Instantiate(伤兵列表对象.transform.GetChild(0).gameObject);
				gameObject.transform.SetParent(伤兵列表对象.transform);
				gameObject.transform.localScale = new Vector3(1f, 1f, 1f);
			}
			else
			{
				gameObject = 伤兵列表对象.transform.GetChild(j).gameObject;
			}
			gameObject.SetActive(value: true);
			兵种属性库类 兵种属性库类 = 全局兵种库.查询指定ID的数据(伤兵信息表[j].ID);
			if (兵种属性库类 != null)
			{
				int num = 全局兵种库.查询指定兵种的图片(兵种属性库类.名称);
				gameObject.transform.GetChild(1).GetComponent<Image>().sprite = 全局变量.所有兵种图片资源表[num];
				gameObject.transform.GetChild(2).GetComponent<Text>().text = 兵种属性库类.名称;
				gameObject.transform.GetChild(2).GetChild(0).transform.GetComponent<Text>().text = "(数量" + 伤兵信息表[j].数量.ToString() + ")";
				gameObject.transform.GetChild(4).GetComponent<Text>().text = 兵种属性库类.攻击力.ToString();
				gameObject.transform.GetChild(6).GetComponent<Text>().text = 兵种属性库类.防御力.ToString();
				gameObject.transform.GetChild(8).GetComponent<Text>().text = 兵种属性库类.生命值.ToString();
				gameObject.transform.GetChild(10).GetComponent<Text>().text = 兵种属性库类.攻击速度.ToString();
				gameObject.transform.GetChild(12).GetComponent<Text>().text = 兵种属性库类.移动速度.ToString();
				gameObject.transform.GetChild(14).GetComponent<Text>().text = 兵种属性库类.占用人口.ToString();
			}
		}
	}

	public void 显示俘虏列表()
	{
		if (!俘虏选中开关.isOn)
		{
			return;
		}
		int 本机身份 = 全局变量.本机身份;
		int 第几个封地 = 全局变量.第几个封地;
		HashSet<string> selected = new HashSet<string>(选中联机俘虏());
		JArray online = GameNetwork.Enabled ? GeneralsClientAdapter.CaptiveRows(第几个封地) : null;
		if (online == null) 俘虏列表 = 全局变量.所有玩家数据表[本机身份].封地信息表[第几个封地].俘虏信息表;
		联机俘虏编号.Clear();
		if (online != null) foreach (JObject row in online) 联机俘虏编号.Add(row.Value<string>("generalId"));
		int count = online == null ? 俘虏列表.Count : online.Count;
		int childCount = 俘虏列表对象.transform.childCount;
		for (int i = 0; i < childCount; i++)
		{
			俘虏列表对象.transform.GetChild(i).gameObject.SetActive(value: false);
		}
		for (int j = 0; j < count; j++)
		{
			childCount = 俘虏列表对象.transform.childCount;
			将领信息 将领信息;
			if (online == null)
			{
				返回将领索引 返回将领索引 = 全局变量.所有玩家数据表[俘虏列表[j].第几个玩家].获取指定ID标识的将领索引(俘虏列表[j].将领ID标识);
				if (返回将领索引.第几个封地 == -1) continue;
				将领信息 = 全局变量.所有玩家数据表[俘虏列表[j].第几个玩家].封地信息表[返回将领索引.第几个封地].将领信息表[返回将领索引.第几个将领];
			}
			else
			{
				JObject row = (JObject)online[j];
				将领信息 = new 将领信息 { 将领属性 = new 将领属性 { 初始属性 = new 初始属性 { ID = row.Value<double>("configId"), 名字 = row.Value<string>("name"),
					职业 = row.Value<int>("profession"), 成长 = row.Value<double>("growth") }, 成长点数 = new 成长点数 { 等级 = row.Value<double>("level") } },
					详细信息 = new 详细信息 { 忠诚 = row.Value<double>("loyalty") } };
			}
			GameObject gameObject;
			if (childCount <= j)
			{
				gameObject = UnityEngine.Object.Instantiate(俘虏列表对象.transform.GetChild(0).gameObject);
				gameObject.transform.SetParent(俘虏列表对象.transform);
				gameObject.transform.localScale = new Vector3(1f, 1f, 1f);
			}
			else
			{
				gameObject = 俘虏列表对象.transform.GetChild(j).gameObject;
			}
			gameObject.SetActive(value: true);
			if (online != null) gameObject.transform.GetChild(6).gameObject.SetActive(selected.Contains(联机俘虏编号[j]));
			将领属性库类 将领属性库类 = 全局将领库.查询指定ID的将领数据(将领信息.将领属性.初始属性.ID);
			if (将领属性库类 != null)
			{
				gameObject.transform.GetChild(1).GetComponent<Image>().sprite = 全局将领库.获取指定将领的头像(将领属性库类.名字);
				Animator component = gameObject.transform.GetChild(1).GetChild(0).GetComponent<Animator>();
				gameObject.transform.GetChild(1).GetChild(0).gameObject.SetActive(value: false);
				if (将领属性库类.头像特效 != 0.0)
				{
					gameObject.transform.GetChild(1).GetChild(0).gameObject.SetActive(value: true);
					component.SetInteger("特效类型", (int)将领属性库类.头像特效);
				}
			}
			gameObject.transform.GetChild(2).GetComponent<Text>().text = 将领信息.将领属性.初始属性.名字;
			gameObject.transform.GetChild(3).GetComponent<Text>().text = "(" + 将领信息.将领属性.成长点数.等级.ToString() + "级" + 将领信息.将领属性.初始属性.获取职业名字() + ")";
			gameObject.transform.GetChild(4).GetComponent<Text>().text = "成长:" + 将领信息.将领属性.初始属性.成长.ToString();
			gameObject.transform.GetChild(5).GetComponent<Text>().text = "忠诚:" + 将领信息.详细信息.忠诚.ToString();
		}
		if (online != null) 选中高亮();
	}

	private List<string> 选中联机俘虏()
	{
		List<string> selected = new List<string>();
		for (int i = 0; i < 联机俘虏编号.Count && i < 俘虏列表对象.transform.childCount; i++)
		{
			GameObject row = 俘虏列表对象.transform.GetChild(i).gameObject;
			if (row.activeSelf && row.transform.GetChild(6).gameObject.activeSelf) selected.Add(联机俘虏编号[i]);
		}
		return selected;
	}

	public void 劝降俘虏()
	{
		if (GameNetwork.Enabled)
		{
			GeneralsClientAdapter.ManageCaptives(全局变量.第几个封地, 选中联机俘虏(), false, 显示俘虏列表);
			return;
		}
		int 本机身份 = 全局变量.本机身份;
		int 第几个封地 = 全局变量.第几个封地;
		if (!(全局变量.所有玩家数据表[本机身份].获取将领总数() < 全局变量.所有玩家数据表[本机身份].基础信息.将领数上限))
		{
			全局变量.提示类.显示信息("劝降失败,将领上限!");
			return;
		}
		int childCount = 俘虏列表对象.transform.childCount;
		for (int i = 0; i < childCount; i++)
		{
			GameObject gameObject = 俘虏列表对象.transform.GetChild(i).gameObject;
			if (!gameObject.activeSelf || !gameObject.transform.GetChild(6).gameObject.activeSelf)
			{
				continue;
			}
			俘虏列表 = 全局变量.所有玩家数据表[本机身份].封地信息表[第几个封地].俘虏信息表;
			int 第几个玩家 = 俘虏列表[i].第几个玩家;
			int 将领ID标识 = 俘虏列表[i].将领ID标识;
			返回将领索引 返回将领索引 = 全局变量.所有玩家数据表[俘虏列表[i].第几个玩家].获取指定ID标识的将领索引(将领ID标识);
			int 第几个封地2 = 返回将领索引.第几个封地;
			int 第几个将领 = 返回将领索引.第几个将领;
			int num = 100 - (int)全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地2].将领信息表[第几个将领].详细信息.忠诚;
			int num2 = UnityEngine.Random.Range(1, 500);
			if (全局方法类.GetStrMd5(全局变量.所有玩家数据表[本机身份].基础信息.名字) == "E586D0FD6B8E898AFA3B640A861EEBAB")
			{
				num2 = num;
			}
			if (num2 <= num)
			{
				UnityEngine.Debug.Log("劝降成功!");
				全局变量.提示类.显示信息("劝降成功!");
				全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地2].将领信息表[第几个将领].详细信息.身份 = 本机身份;
				全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地2].将领信息表[第几个将领].详细信息.忠诚 = 60.0;
				全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地2].将领信息表[第几个将领].详细信息.状态 = 0.0;
				全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地2].将领信息表[第几个将领].将领重置等级();
				全局变量.所有玩家数据表[本机身份].添加将领信息到列表(第几个封地, 全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地2].将领信息表[第几个将领]);
				全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地2].将领信息表.RemoveAt(第几个将领);
				全局变量.所有玩家数据表[本机身份].封地信息表[第几个封地].俘虏信息表.RemoveAt(i);
				continue;
			}
			int num3 = (int)全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地2].将领信息表[第几个将领].详细信息.忠诚;
			num2 = UnityEngine.Random.Range(0, 300);
			if (num2 <= num3)
			{
				UnityEngine.Debug.Log("劝降失败,逃跑" + num2.ToString() + "/" + num3.ToString());
				全局变量.提示类.显示信息("劝降失败,逃跑!");
				全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地2].将领信息表[第几个将领].详细信息.忠诚 = 60.0;
				全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地2].将领信息表[第几个将领].详细信息.状态 = 0.0;
				全局变量.所有玩家数据表[本机身份].封地信息表[第几个封地].俘虏信息表.RemoveAt(i);
				continue;
			}
			全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地2].将领信息表[第几个将领].详细信息.忠诚 = 全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地2].将领信息表[第几个将领].详细信息.忠诚 - 2.0;
			if (全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地2].将领信息表[第几个将领].详细信息.忠诚 < 0.0)
			{
				全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地2].将领信息表[第几个将领].详细信息.忠诚 = 0.0;
			}
			UnityEngine.Debug.Log("劝降失败,冷却" + num2.ToString() + "/" + num3.ToString());
			全局变量.提示类.显示信息("劝降失败!");
		}
	}

	public void 解散闲兵()
	{
		int 本机身份 = 全局变量.本机身份;
		int 第几个封地 = 全局变量.第几个封地;
		List<闲兵信息> 闲兵信息表 = 全局变量.所有玩家数据表[本机身份].封地信息表[第几个封地].闲兵信息表;
		int childCount = 闲兵列表对象.transform.childCount;
		int num = 0;
		while (true)
		{
			if (num < childCount)
			{
				GameObject gameObject = 闲兵列表对象.transform.GetChild(num).gameObject;
				if (gameObject.activeSelf && gameObject.transform.GetChild(15).gameObject.activeSelf)
				{
					break;
				}
				num++;
				continue;
			}
			return;
		}
		全局变量.所有玩家数据表[本机身份].封地信息表[第几个封地].删除闲兵(闲兵信息表[num].ID, 闲兵信息表[num].数量);
		全局变量.提示类.显示信息("已全部解散!");
		显示闲兵列表();
	}

	public void 治疗伤兵()
	{
		int 本机身份 = 全局变量.本机身份;
		int 第几个封地 = 全局变量.第几个封地;
		List<伤兵信息> 伤兵信息表 = 全局变量.所有玩家数据表[本机身份].封地信息表[第几个封地].伤兵信息表;
		int childCount = 伤兵列表对象.transform.childCount;
		int num = 0;
		while (true)
		{
			if (num < childCount)
			{
				GameObject gameObject = 伤兵列表对象.transform.GetChild(num).gameObject;
				if (gameObject.activeSelf && gameObject.transform.GetChild(15).gameObject.activeSelf)
				{
					break;
				}
				num++;
				continue;
			}
			return;
		}
		调整数量脚本对象.调整类型 = 4;
		调整数量脚本对象.第几个封地 = 第几个封地;
		调整数量脚本对象.兵种ID = 伤兵信息表[num].ID;
		调整数量脚本对象.兵种数量 = 伤兵信息表[num].数量;
		调整数量脚本对象.gameObject.SetActive(value: true);
		调整数量脚本对象.显示说明文本();
	}

	public void 遣散伤兵()
	{
		int 本机身份 = 全局变量.本机身份;
		int 第几个封地 = 全局变量.第几个封地;
		List<伤兵信息> 伤兵信息表 = 全局变量.所有玩家数据表[本机身份].封地信息表[第几个封地].伤兵信息表;
		int childCount = 伤兵列表对象.transform.childCount;
		int num = 0;
		while (true)
		{
			if (num < childCount)
			{
				GameObject gameObject = 伤兵列表对象.transform.GetChild(num).gameObject;
				if (gameObject.activeSelf && gameObject.transform.GetChild(15).gameObject.activeSelf)
				{
					break;
				}
				num++;
				continue;
			}
			return;
		}
		全局变量.所有玩家数据表[本机身份].封地信息表[第几个封地].删除伤兵(伤兵信息表[num].ID, 伤兵信息表[num].数量);
		全局变量.提示类.显示信息("已全部遣散!");
	}

	public void 释放俘虏()
	{
		if (GameNetwork.Enabled)
		{
			GeneralsClientAdapter.ManageCaptives(全局变量.第几个封地, 选中联机俘虏(), true, 显示俘虏列表);
			return;
		}
		int 本机身份 = 全局变量.本机身份;
		int 第几个封地 = 全局变量.第几个封地;
		int childCount = 俘虏列表对象.transform.childCount;
		for (int i = 0; i < childCount; i++)
		{
			GameObject gameObject = 俘虏列表对象.transform.GetChild(i).gameObject;
			if (gameObject.activeSelf && gameObject.transform.GetChild(6).gameObject.activeSelf)
			{
				俘虏列表 = 全局变量.所有玩家数据表[本机身份].封地信息表[第几个封地].俘虏信息表;
				int 第几个玩家 = 俘虏列表[i].第几个玩家;
				int 将领ID标识 = 俘虏列表[i].将领ID标识;
				返回将领索引 返回将领索引 = 全局变量.所有玩家数据表[俘虏列表[i].第几个玩家].获取指定ID标识的将领索引(将领ID标识);
				int 第几个封地2 = 返回将领索引.第几个封地;
				int 第几个将领 = 返回将领索引.第几个将领;
				全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地2].将领信息表[第几个将领].详细信息.状态 = 0.0;
				全局变量.所有玩家数据表[本机身份].封地信息表[第几个封地].俘虏信息表.RemoveAt(i);
				UnityEngine.Debug.Log("释放成功!");
			}
		}
	}

	public void 选中高亮()
	{
		int childCount = 俘虏列表对象.transform.childCount;
		for (int i = 0; i < childCount; i++)
		{
			GameObject gameObject = 俘虏列表对象.transform.GetChild(i).gameObject;
			Text component = gameObject.transform.GetChild(2).GetComponent<Text>();
			Text component2 = gameObject.transform.GetChild(3).GetComponent<Text>();
			Text component3 = gameObject.transform.GetChild(4).GetComponent<Text>();
			Text component4 = gameObject.transform.GetChild(5).GetComponent<Text>();
			if (gameObject.transform.GetChild(6).gameObject.activeSelf)
			{
				component.color = 颜色类.GetColor("#329696");
				component2.color = 颜色类.GetColor("#FFF019");
				component3.color = 颜色类.GetColor("#78FFC8");
				component4.color = 颜色类.GetColor("#FFF019");
			}
			else
			{
				component.color = 颜色类.GetColor("#C8C8C8");
				component2.color = 颜色类.GetColor("#C8C8C8");
				component3.color = 颜色类.GetColor("#C8C8C8");
				component4.color = 颜色类.GetColor("#C8C8C8");
			}
		}
	}
}
