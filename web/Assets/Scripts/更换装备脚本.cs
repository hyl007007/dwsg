using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using 玩家数据结构;

public class 更换装备脚本 : MonoBehaviour
{
	public Image 已选中装备图片;

	public Text 已选中装备信息;

	public GameObject 装备列表对象;

	public 将领装备 已选中装备;

	public 将领装备 将领本来的装备信息;

	public int 第几个玩家;

	public int 第几个封地;

	public int 第几个将领;

	public int 第几个部位;

	public List<将领装备> 要显示的装备列表;

	public GameObject 将领界面UI对象;

	private int 起始滑动位置;

	private void Start()
	{
	}

	public void 刷新显示()
	{
		int childCount = 装备列表对象.transform.childCount;
		for (int i = 0; i < childCount; i++)
		{
			装备列表对象.transform.GetChild(i).GetChild(0).gameObject.SetActive(value: false);
			装备列表对象.transform.GetChild(i).gameObject.SetActive(value: false);
			装备列表对象.transform.GetChild(i).GetComponent<Button>().onClick.RemoveAllListeners();
		}
		int count = 要显示的装备列表.Count;
		for (int j = 0; j < count; j++)
		{
			if (要显示的装备列表[j].将领ID == -1)
			{
				int childCount2 = 装备列表对象.transform.childCount;
				GameObject gameObject;
				if (childCount <= j)
				{
					gameObject = UnityEngine.Object.Instantiate(装备列表对象.transform.GetChild(0).gameObject);
					gameObject.transform.SetParent(装备列表对象.transform);
					gameObject.transform.localScale = new Vector3(1f, 1f, 1f);
				}
				else
				{
					gameObject = 装备列表对象.transform.GetChild(j).gameObject;
				}
				gameObject.SetActive(value: true);
				int 点击第几个 = j;
				gameObject.transform.GetComponent<Button>().onClick.AddListener(delegate
				{
					点击装备(点击第几个);
				});
				Text component = gameObject.transform.GetChild(1).GetComponent<Text>();
				component.text = 要显示的装备列表[j].获取装备简单信息文本();
				component.color = 要显示的装备列表[j].获取装备文字颜色();
			}
		}
	}

	public void 获取选中装备()
	{
		int childCount = 装备列表对象.transform.childCount;
		int num = 0;
		while (true)
		{
			if (num < childCount)
			{
				if (装备列表对象.transform.GetChild(num).GetComponent<Toggle>().isOn)
				{
					break;
				}
				num++;
				continue;
			}
			return;
		}
		已选中装备 = 要显示的装备列表[num];
	}

	public void 点击装备(int 第几个装备)
	{
		UnityEngine.Debug.Log("点击" + 第几个装备.ToString());
		将领装备 将领装备 = 全局变量.所有玩家数据表[第几个玩家].背包装备列表.寻找指定将领的装备(第几个部位, 全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地].将领信息表[第几个将领].ID);
		if (装备列表对象.transform.GetChild(第几个装备).GetChild(0).gameObject.activeSelf)
		{
			if (全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地].将领信息表[第几个将领].将领属性.成长点数.等级 >= 已选中装备.获取装备等级())
			{
				if (将领装备 != null)
				{
					将领装备.将领ID = -1;
				}
				已选中装备.将领ID = 全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地].将领信息表[第几个将领].ID;
				全局变量.提示类.显示信息("穿戴成功!");
			}
			else
			{
				全局变量.提示类.显示信息("将领等级不足!");
			}
			base.gameObject.SetActive(value: false);
			if (将领界面UI对象.activeSelf)
			{
				将领界面UI对象.GetComponent<将领列表显示>().解除配兵();
			}
			return;
		}
		int childCount = 装备列表对象.transform.childCount;
		for (int i = 0; i < childCount; i++)
		{
			装备列表对象.transform.GetChild(i).GetChild(0).gameObject.SetActive(value: false);
		}
		装备列表对象.transform.GetChild(第几个装备).GetChild(0).gameObject.SetActive(value: true);
		已选中装备 = 要显示的装备列表[第几个装备];
		已选中装备图片.sprite = 已选中装备.获取装备头像();
		if (已选中装备.获取装备名字().IndexOf("尊") > -1)
		{
			已选中装备图片.transform.GetChild(0).gameObject.SetActive(value: true);
		}
		else
		{
			已选中装备图片.transform.GetChild(0).gameObject.SetActive(value: false);
		}
		double num = 0.0;
		if (将领装备 != null)
		{
			num = 将领装备.获取装备加成数字();
		}
		double num2 = 已选中装备.获取装备加成数字();
		string text = "";
		text = ((!(num > num2)) ? ("↑" + (num2 - num).ToString()) : ("↓" + (num - num2).ToString()));
		已选中装备信息.text = 已选中装备.获取装备简单信息文本() + "\n" + 已选中装备.获取装备强化效果文本() + text;
		已选中装备信息.color = 已选中装备.获取装备文字颜色();
	}

	private void 注册事件(EventTrigger 事件系统, EventTriggerType 事件类型, UnityAction<BaseEventData> 绑定方法)
	{
		EventTrigger.Entry entry = new EventTrigger.Entry();
		entry.eventID = 事件类型;
		entry.callback = new EventTrigger.TriggerEvent();
		UnityAction<BaseEventData> call = 绑定方法.Invoke;
		entry.callback.AddListener(call);
		事件系统.triggers.Add(entry);
	}
}
