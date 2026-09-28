using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class 显示商城列表 : MonoBehaviour
{
	public GameObject 商品列表对象;

	public Text 页数显示;

	private List<商品属性类> 要显示的物品列表;

	public GameObject 切换列表对象;

	public 购买道具脚本 购买道具脚本对象;

	public Text 黄金显示;

	public Text 白银显示;

	private int 显示第几页 = 1;

	private float 总页数;

	public void 切换热卖()
	{
		if (切换列表对象.transform.GetChild(0).GetComponent<Toggle>().isOn)
		{
			要显示的物品列表 = 全局商城库.热卖商品列表;
			显示第几页 = 1;
			刷新显示();
		}
	}

	public void 切换特价()
	{
		if (切换列表对象.transform.GetChild(1).GetComponent<Toggle>().isOn)
		{
			要显示的物品列表 = 全局商城库.特价商品列表;
			显示第几页 = 1;
			刷新显示();
		}
	}

	public void 切换装备()
	{
		if (切换列表对象.transform.GetChild(2).GetComponent<Toggle>().isOn)
		{
			要显示的物品列表 = 全局商城库.装备商品列表;
			显示第几页 = 1;
			刷新显示();
		}
	}

	public void 切换生产()
	{
		if (切换列表对象.transform.GetChild(3).GetComponent<Toggle>().isOn)
		{
			要显示的物品列表 = 全局商城库.生产商品列表;
			显示第几页 = 1;
			刷新显示();
		}
	}

	public void 切换加速()
	{
		if (切换列表对象.transform.GetChild(4).GetComponent<Toggle>().isOn)
		{
			要显示的物品列表 = 全局商城库.加速商品列表;
			显示第几页 = 1;
			刷新显示();
		}
	}

	public void 切换宝物()
	{
		if (切换列表对象.transform.GetChild(5).GetComponent<Toggle>().isOn)
		{
			要显示的物品列表 = 全局商城库.宝物商品列表;
			显示第几页 = 1;
			刷新显示();
		}
	}

	public void 切换宝箱()
	{
		if (切换列表对象.transform.GetChild(6).GetComponent<Toggle>().isOn)
		{
			要显示的物品列表 = 全局商城库.宝箱商品列表;
			显示第几页 = 1;
			刷新显示();
		}
	}

	public void 切换其他()
	{
		if (切换列表对象.transform.GetChild(7).GetComponent<Toggle>().isOn)
		{
			要显示的物品列表 = 全局商城库.其他商品列表;
			显示第几页 = 1;
			刷新显示();
		}
	}

	public void 刷新显示()
	{
		int childCount = 商品列表对象.transform.childCount;
		for (int i = 0; i < childCount; i++)
		{
			商品列表对象.transform.GetChild(i).GetChild(1).gameObject.SetActive(value: false);
			商品列表对象.transform.GetChild(i).GetChild(2).gameObject.SetActive(value: false);
			商品列表对象.transform.GetChild(i).GetChild(3).gameObject.SetActive(value: false);
			商品列表对象.transform.GetChild(i).GetComponent<EventTrigger>().triggers.Clear();
			商品列表对象.transform.GetChild(i).GetChild(4).gameObject.SetActive(value: false);
		}
		int num = (显示第几页 - 1) * 12;
		int count = 要显示的物品列表.Count;
		for (int j = 0; j < 12; j++)
		{
			if (num + j < count)
			{
				商品列表对象.transform.GetChild(j).GetChild(1).gameObject.SetActive(value: true);
				Image component = 商品列表对象.transform.GetChild(j).GetChild(1).GetComponent<Image>();
				商品列表对象.transform.GetChild(j).GetChild(2).gameObject.SetActive(value: true);
				商品列表对象.transform.GetChild(j).GetChild(2).GetComponent<Text>()
					.text = 要显示的物品列表[num + j].道具名;
					商品列表对象.transform.GetChild(j).GetChild(3).gameObject.SetActive(value: true);
					商品列表对象.transform.GetChild(j).GetChild(3).GetChild(0)
						.GetChild(1)
						.GetComponent<Text>()
						.text = 要显示的物品列表[num + j].黄金售价.ToString();
						Text component2 = 商品列表对象.transform.GetChild(j).GetChild(3).GetChild(1)
							.GetChild(1)
							.GetComponent<Text>();
						if (要显示的物品列表[num + j].白银售价 > 0.0)
						{
							商品列表对象.transform.GetChild(j).GetChild(3).GetChild(1)
								.gameObject.SetActive(value: true);
								component2.text = 要显示的物品列表[num + j].白银售价.ToString();
							}
							else
							{
								商品列表对象.transform.GetChild(j).GetChild(3).GetChild(1)
									.gameObject.SetActive(value: false);
								}
								道具信息库类 道具信息库类 = 全局道具库.获取指定名字的道具(要显示的物品列表[num + j].道具名);
								if (道具信息库类 != null)
								{
									string 道具名字 = 要显示的物品列表[num + j].道具名;
									int 第几个 = j;
									注册事件(商品列表对象.transform.GetChild(j).GetComponent<EventTrigger>(), EventTriggerType.PointerDown, delegate
									{
										商品列表对象.transform.GetChild(第几个).GetChild(4).gameObject.SetActive(value: true);
									});
									注册事件(商品列表对象.transform.GetChild(j).GetComponent<EventTrigger>(), EventTriggerType.PointerUp, delegate
									{
										商品列表对象.transform.GetChild(第几个).GetChild(4).gameObject.SetActive(value: false);
									});
									注册事件(商品列表对象.transform.GetChild(j).GetComponent<EventTrigger>(), EventTriggerType.PointerClick, delegate
									{
										点击购买道具(道具名字);
									});
									component.sprite = 全局道具库.获取道具头像(道具信息库类.头像);
								}
							}
						}
						int 本机身份 = 全局变量.本机身份;
						黄金显示.text = (全局变量.所有玩家数据表[本机身份].财产信息.黄金.ToString() ?? "");
						白银显示.text = (全局变量.所有玩家数据表[本机身份].财产信息.白银.ToString() ?? "");
						总页数 = Mathf.Ceil((float)count / 12f);
						页数显示.text = 显示第几页.ToString() + "/" + 总页数.ToString();
					}

					public void 点击购买道具(string 道具名字)
					{
						UnityEngine.Debug.Log("打开购买:" + 道具名字);
						购买道具脚本对象.道具名字 = 道具名字;
						购买道具脚本对象.打开购买界面();
						购买道具脚本对象.刷新显示();
					}

					public void 左翻页()
					{
						if (显示第几页 != 1)
						{
							显示第几页--;
							刷新显示();
						}
					}

					public void 右翻页()
					{
						if ((float)显示第几页 < 总页数)
						{
							显示第几页++;
							刷新显示();
						}
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
