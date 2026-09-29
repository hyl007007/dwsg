using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class 封地界面脚本 : MonoBehaviour
{
	public Transform 封地建筑列表对象;

	public Transform 建造界面UI对象;

	public GameObject 书院详情UI对象;

	public GameObject 大厅详情UI对象;

	public GameObject 房屋详情UI对象;

	public GameObject 农场详情UI对象;

	public GameObject 兵营详情UI对象;

	private int 第几个玩家 = 全局变量.本机身份;

	public int 第几个封地;

	private int 已打开第几个建筑;

	public Text 当前封地显示对象;

	public void 建造建筑()
	{
		List<int> list = new List<int>
		{
			2,
			3,
			1,
			5,
			6,
			7,
			4
		};
		int num = 0;
		while (true)
		{
			if (num < 7)
			{
				if (建造界面UI对象.GetChild(1).GetChild(1).GetChild(num)
					.GetComponent<Toggle>()
					.isOn)
				{
					break;
				}
				num++;
				continue;
			}
			return;
		}
		UnityEngine.Debug.Log("类型" + num.ToString());
		请求建筑操作("fief.construct", list[num]);
	}

	private double 获取升级需要铜钱(double 基数, double 等级)
	{
		return Dwsg.Shared.Economy.BuildingRules.UpgradeCost(基数, 等级);
	}

	public void 升级建筑()
	{
		请求建筑操作("fief.upgrade", null);
	}

	public void 拆除建筑()
	{
		请求建筑操作("fief.demolish", null);
	}

	private void 请求建筑操作(string 类型, int? 建筑类型)
	{
		ProductionClient.Execute(类型, 全局变量.所有玩家数据表[第几个玩家], 第几个封地, 已打开第几个建筑, 建筑类型, 结果 =>
		{
			if (this == null) return;
			if (结果 == null || 结果.Code != Dwsg.Shared.GameCodes.Ok)
			{
				全局变量.提示类.显示信息(结果?.Message ?? "服务器未确认建筑操作，请重试");
				return;
			}
			if (类型 == "fief.upgrade") 全局变量.提示类.显示信息(结果.Message);
			显示封地所有建筑();
		});
	}

	public void 显示建造建筑信息()
	{
		for (int i = 0; i < 7; i++)
		{
			if (建造界面UI对象.GetChild(1).GetChild(1).GetChild(i)
				.GetComponent<Toggle>()
				.isOn)
			{
				UnityEngine.Debug.Log("类型" + i.ToString());
				Image component = 建造界面UI对象.GetChild(4).GetChild(1).GetComponent<Image>();
				Text component2 = 建造界面UI对象.GetChild(4).GetChild(2).GetComponent<Text>();
				Text component3 = 建造界面UI对象.GetChild(4).GetChild(4).GetComponent<Text>();
				switch (i)
				{
					case 0:
						component.sprite = 全局变量.房屋头像资源表[0];
						component2.text = "增加人口的上限,让你可以招募更多的军队.";
						component3.text = "人口加成 20";
						break;
					case 1:
						component.sprite = 全局变量.农田头像资源表[0];
						component2.text = "农场可以生产粮食";
						component3.text = "粮食产量 25";
						break;
					case 2:
						component.sprite = 全局变量.书院头像资源表[0];
						component2.text = "书院中可以研究各种科技,研究好的科技可以在各个封地中共享.";
						component3.text = "提升科技等级";
						break;
					case 3:
						component.sprite = 全局变量.步兵营头像资源表[0];
						component2.text = "用于招募步兵,提升等级可以招募更强的兵种.";
						component3.text = "可招:民兵";
						break;
					case 4:
						component.sprite = 全局变量.弓兵营头像资源表[0];
						component2.text = "用于招募弓箭部队,提升等级可以招募更强的兵种.";
						component3.text = "可招:弓兵";
						break;
					case 5:
						component.sprite = 全局变量.战车营头像资源表[0];
						component2.text = "用于招募各种大型的器械战车.提升等级可以招募更强的兵种.";
						component3.text = "可招:弩车";
						break;
					case 6:
						component.sprite = 全局变量.骑兵营头像资源表[0];
						component2.text = "用于招募骑兵,提升等级可以招募更强的兵种.";
						component3.text = "可招:轻骑兵";
						break;
				}
			}
		}
	}

	public void 封地建筑打开操作(int 第几个建筑)
	{
		int 类型 = 全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地].建筑信息表[第几个建筑].类型;
		UnityEngine.Debug.Log("建筑" + 第几个建筑.ToString() + "类型:" + 类型.ToString());
		已打开第几个建筑 = 第几个建筑;
		if (类型 == -1)
		{
			建造界面UI对象.gameObject.SetActive(value: true);
			建造界面UI对象.GetChild(0).gameObject.SetActive(value: true);
		}
		else if (类型 == 0)
		{
			大厅详情UI对象.gameObject.SetActive(value: true);
			显示建筑信息脚本 component = 大厅详情UI对象.transform.GetComponent<显示建筑信息脚本>();
			component.建筑信息对象 = 全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地].建筑信息表[第几个建筑];
			component.显示建筑信息();
		}
		else if (类型 == 1)
		{
			书院详情UI对象.SetActive(value: true);
			书院脚本 component2 = 书院详情UI对象.transform.GetComponent<书院脚本>();
			component2.第几个玩家 = 全局变量.本机身份;
			component2.第几个封地 = 第几个封地;
			component2.第几个建筑 = 第几个建筑;
			component2.显示书院建筑信息();
			component2.显示科技列表();
		}
		else if (类型 == 2)
		{
			房屋详情UI对象.gameObject.SetActive(value: true);
			显示建筑信息脚本 component3 = 房屋详情UI对象.transform.GetComponent<显示建筑信息脚本>();
			component3.建筑信息对象 = 全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地].建筑信息表[第几个建筑];
			component3.显示建筑信息();
		}
		else if (类型 == 3)
		{
			农场详情UI对象.gameObject.SetActive(value: true);
			显示建筑信息脚本 component4 = 农场详情UI对象.transform.GetComponent<显示建筑信息脚本>();
			component4.建筑信息对象 = 全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地].建筑信息表[第几个建筑];
			component4.显示建筑信息();
		}
		else if (类型 >= 4)
		{
			兵营详情UI对象.gameObject.SetActive(value: true);
			兵营脚本 component5 = 兵营详情UI对象.transform.GetComponent<兵营脚本>();
			component5.第几个玩家 = 全局变量.本机身份;
			component5.第几个封地 = 第几个封地;
			component5.第几个建筑 = 第几个建筑;
			component5.兵营类型 = 类型;
			component5.刷新显示();
		}
	}

	public void 显示封地所有建筑()
	{
		if (第几个封地 >= 全局变量.所有玩家数据表[第几个玩家].封地信息表.Count)
		{
			第几个封地 = 0;
		}
		UnityEngine.Debug.Log("显示封地" + 第几个封地.ToString() + "的建筑");
		int count = 全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地].建筑信息表.Count;
		for (int i = 0; i < count; i++)
		{
			if (封地建筑列表对象.GetChild(i).GetChild(0).childCount > 0)
			{
				UnityEngine.Object.Destroy(封地建筑列表对象.GetChild(i).GetChild(0).GetChild(0)
					.gameObject);
			}
			封地建筑列表对象.GetChild(i).GetChild(1).gameObject.SetActive(value: false);
			显示封地指定建筑(i);
		}
		当前封地显示对象.text = 全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地].封地名字;
	}

	public void 显示封地指定建筑(int 第几个建筑)
	{
		int 类型 = 全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地].建筑信息表[第几个建筑].类型;
		if (类型 != -1)
		{
			int num = 全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地].建筑信息表[第几个建筑].获取建筑头像索引();
			GameObject gameObject = UnityEngine.Object.Instantiate(全局变量.封地所有建筑模型[类型][num]);
			gameObject.transform.parent = 封地建筑列表对象.GetChild(第几个建筑).GetChild(0).transform;
			gameObject.transform.localPosition = new Vector2(0f, 0f);
			gameObject.transform.localScale = new Vector3(1f, 1f, 1f);
			封地建筑列表对象.GetChild(第几个建筑).GetChild(1).gameObject.SetActive(value: true);
			封地建筑列表对象.GetChild(第几个建筑).GetChild(1).GetChild(2)
				.GetComponent<Text>()
				.text = 全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地].建筑信息表[第几个建筑].等级.ToString();
			封地建筑列表对象.GetChild(第几个建筑).GetChild(1).GetChild(3)
				.GetComponent<Image>()
				.sprite = 全局变量.封地所有建筑名字[类型];
		}
	}
}
