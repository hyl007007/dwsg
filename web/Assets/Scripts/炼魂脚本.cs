using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using 玩家数据结构;

public class 炼魂脚本 : MonoBehaviour
{
	public Image 装备头像;

	public Text 装备信息显示;

	public GameObject 炼魂列表对象;

	public 将领装备 装备对象;

	public Text 炼魂材料显示;

	public Text 锁定材料显示;

	private int 锁定数量;

	private double 消耗材料数量;

	private double 凝魂晶石材料数量;

	public Toggle 普通炼魂选中;

	public Toggle 高级炼魂选中;

	public Toggle 高级炼魂选中1;

	private bool refreshing;

	private void Start()
	{
	}

	public void 显示装备所有信息()
	{
		refreshing = true;
		try
		{
			锁定数量 = 0;
			装备头像.sprite = 装备对象.获取装备头像();
			装备信息显示.text = 装备对象.获取装备名字() + "+" + 装备对象.强化等级.ToString() + "(" + 装备对象.获取装备等级().ToString() + "级) " + 装备对象.获取装备品质文本() + "\n" + 装备对象.获取装备加成文本();
			装备信息显示.color = 装备对象.获取装备文字颜色();
			int count = 装备对象.炼魂属性.Count;
			for (int i = 0; i < 6; i++)
			{
				炼魂列表对象.transform.GetChild(i).gameObject.SetActive(value: false);
				炼魂列表对象.transform.GetChild(i).GetChild(0).gameObject.SetActive(value: false);
				炼魂列表对象.transform.GetChild(i).GetChild(4).gameObject.SetActive(value: false);
				if (i < count)
				{
					Text component = 炼魂列表对象.transform.GetChild(i).GetChild(1).GetComponent<Text>();
					component.text = 装备对象.炼魂属性[i].获取炼魂加成文本();
					component.color = 装备对象.获取装备文字颜色();
					炼魂列表对象.transform.GetChild(i).GetChild(2).GetComponent<Text>()
						.color = 装备对象.获取装备文字颜色();
					炼魂列表对象.transform.GetChild(i).gameObject.SetActive(value: true);
					if (装备对象.炼魂属性[i].锁定)
					{
						炼魂列表对象.transform.GetChild(i).GetChild(0).gameObject.SetActive(value: true);
						炼魂列表对象.transform.GetChild(i).GetChild(4).gameObject.SetActive(value: true);
						锁定数量++;
					}
					else
					{
						炼魂列表对象.transform.GetChild(i).GetComponent<Toggle>().isOn = false;
					}
				}
			}
			int 本机身份 = 全局变量.本机身份;
			string text = 装备对象.获取装备炼魂材料名字();
			消耗材料数量 = 全局变量.所有玩家数据表[本机身份].背包道具列表.获取指定道具数量(text);
			凝魂晶石材料数量 = 全局变量.所有玩家数据表[本机身份].背包道具列表.获取指定道具数量("凝魂晶石");
			炼魂材料显示.text = text + 消耗材料数量.ToString() + "/" + 装备对象.品质.ToString();
			锁定材料显示.text = "凝魂晶石" + 凝魂晶石材料数量.ToString() + "/" + 锁定数量.ToString();
		}
		finally { refreshing = false; }
	}

	List<int> 选中锁定槽位()
	{
		List<int> indices = new List<int>();
		for (int i = 0; i < 装备对象.炼魂属性.Count; i++)
			if (炼魂列表对象.transform.GetChild(i).GetChild(4).gameObject.activeSelf) indices.Add(i);
		return indices;
	}

	public void 锁定指定炼魂()
	{
		if (refreshing) return;
		Dwsg.Generals.GeneralsClientAdapter.SetSoulLocks(装备对象, 选中锁定槽位(), 显示装备所有信息);
	}

	void 炼魂(int count)
	{
		int mode = 高级炼魂选中1.isOn ? 2 : 高级炼魂选中.isOn ? 1 : 0;
		Dwsg.Generals.GeneralsClientAdapter.RefineEquipment(装备对象, mode, count, 选中锁定槽位(), 显示装备所有信息);
	}

	public void 开始炼魂() { 炼魂(1); }

	public List<炼魂属性> 开始炼魂一次()
	{
		if (选中锁定槽位().Count == 6) 全局变量.提示类.显示信息("已经六条属性了");
		else 开始炼魂();
		return new List<炼魂属性>(装备对象.炼魂属性);
	}

	public void 开始炼魂30次() { 炼魂(30); }
}
