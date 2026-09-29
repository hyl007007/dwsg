using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class 使用道具脚本 : MonoBehaviour
{
	public GameObject 显示列表对象;

	public List<道具信息库类> 要显示的列表;

	public int 第几个封地;

	public int 第几个将领;

	public GameObject 将领列表对象;

	public GameObject 君主状态列表对象;

	private Vector2 pos;

	private RectTransform 滚动信息;

	private RectTransform 滚动区域;

	private float 结束位置;

	private bool 开始滚动;

	public void 刷新显示()
	{
		int childCount = 显示列表对象.transform.childCount;
		for (int i = 0; i < childCount; i++)
		{
			显示列表对象.transform.GetChild(i).gameObject.SetActive(value: false);
		}
		int count = 要显示的列表.Count;
		for (int j = 0; j < count; j++)
		{
			int childCount2 = 显示列表对象.transform.childCount;
			GameObject gameObject;
			if (childCount <= j)
			{
				gameObject = UnityEngine.Object.Instantiate(显示列表对象.transform.GetChild(0).gameObject);
				gameObject.transform.SetParent(显示列表对象.transform);
				gameObject.transform.localScale = new Vector3(1f, 1f, 1f);
			}
			else
			{
				gameObject = 显示列表对象.transform.GetChild(j).gameObject;
			}
			gameObject.SetActive(value: true);
			Image component = gameObject.transform.GetChild(1).GetComponent<Image>();
			Text component2 = gameObject.transform.GetChild(2).GetComponent<Text>();
			Text component3 = gameObject.transform.GetChild(2).GetChild(0).GetComponent<Text>();
			Text component4 = gameObject.transform.GetChild(3).GetChild(0).GetComponent<Text>();
			component2.text = 要显示的列表[j].名字 + " ";
			int 本机身份 = 全局变量.本机身份;
			component3.text = "(" + 全局变量.所有玩家数据表[本机身份].背包道具列表.获取指定道具数量(要显示的列表[j].名字).ToString() + ")";
			component4.text = 要显示的列表[j].说明;
			component.sprite = 全局道具库.获取道具头像(要显示的列表[j].头像);
		}
	}

	public void 使用选中道具()
	{
		int childCount = 显示列表对象.transform.childCount;
		for (int i = 0; i < childCount; i++)
		{
			if (!显示列表对象.transform.GetChild(i).GetComponent<Toggle>().isOn)
			{
				continue;
			}
			UnityEngine.Debug.Log("使用道具:" + 要显示的列表[i].名字);
			if (Dwsg.Shared.Economy.MaterialPackRules.MaterialName(要显示的列表[i].名字) != null)
			{
				if (Dwsg.Economy.MaterialPackClient.Pending)
				{
					if (全局变量.提示类 != null) 全局变量.提示类.显示信息("正在使用材料包，请稍候");
					return;
				}
				Dwsg.Economy.MaterialPackClient.Use(要显示的列表[i].名字, 1, result =>
				{
					if (this == null) return;
					if (全局变量.提示类 != null) 全局变量.提示类.显示信息(result.Code == Dwsg.Shared.GameCodes.Ok ? "使用成功:\n" + result.Message : result.Message);
					刷新显示();
				});
				return;
			}
			int 本机身份 = 全局变量.本机身份;
			string text = 全局变量.所有玩家数据表[本机身份].背包道具列表.使用道具(要显示的列表[i].名字, 第几个封地, 第几个将领);
			if (text != "使用失败")
			{
				全局变量.提示类.显示信息("使用成功:\n" + text);
				if (将领列表对象.activeSelf)
				{
					将领列表显示 component = 将领列表对象.GetComponent<将领列表显示>();
					component.刷新列表信息();
					component.刷新将领属性信息();
				}
				else if (君主状态列表对象.activeSelf)
				{
					君主状态列表对象.GetComponent<显示状态信息>().刷新显示();
				}
				break;
			}
			全局变量.提示类.显示信息("使用失败!");
		}
		刷新显示();
	}

	public void 选中高亮()
	{
		int childCount = 显示列表对象.transform.childCount;
		int 本机身份 = 全局变量.本机身份;
		开始滚动 = false;
		for (int i = 0; i < childCount; i++)
		{
			if (显示列表对象.transform.GetChild(i).GetComponent<Toggle>().isOn)
			{
				显示列表对象.transform.GetChild(i).GetChild(2).GetComponent<Text>()
					.color = 颜色类.GetColor("#DBD98A");
				显示列表对象.transform.GetChild(i).GetChild(2).GetChild(0)
					.GetComponent<Text>()
					.color = 颜色类.GetColor("#FDA400");
				显示列表对象.transform.GetChild(i).GetChild(2).GetChild(0)
					.GetChild(0)
					.GetComponent<Text>()
					.color = 颜色类.GetColor("#DBD98A");
				显示列表对象.transform.GetChild(i).GetChild(3).GetChild(0)
					.GetComponent<Text>()
					.color = 颜色类.GetColor("#329696");
				滚动信息 = 显示列表对象.transform.GetChild(i).GetChild(3).GetChild(0)
					.GetComponent<RectTransform>();
				float width = 滚动信息.rect.width;
				float width2 = 显示列表对象.transform.GetChild(i).GetChild(3).GetComponent<RectTransform>()
					.rect.width;
				if (width > width2)
				{
					结束位置 = 0f - width;
					Invoke("开启滚动", 1f);
				}
			}
			else
			{
				显示列表对象.transform.GetChild(i).GetChild(2).GetComponent<Text>()
					.color = 颜色类.GetColor("#C8C8C8");
				显示列表对象.transform.GetChild(i).GetChild(2).GetChild(0)
					.GetComponent<Text>()
					.color = 颜色类.GetColor("#C8C8C8");
				显示列表对象.transform.GetChild(i).GetChild(2).GetChild(0)
					.GetChild(0)
					.GetComponent<Text>()
					.color = 颜色类.GetColor("#C8C8C8");
				显示列表对象.transform.GetChild(i).GetChild(3).GetChild(0)
					.GetComponent<Text>()
					.color = 颜色类.GetColor("#C8C8C8");
				显示列表对象.transform.GetChild(i).GetChild(3).GetChild(0)
					.GetComponent<Text>()
					.color = 颜色类.GetColor("#C8C8C8");
				显示列表对象.transform.GetChild(i).GetChild(3).GetChild(0)
					.GetComponent<RectTransform>()
					.anchoredPosition = new Vector2(0f, 0f);
			}
		}
	}

	private void 开启滚动()
	{
		开始滚动 = true;
	}

	private void FixedUpdate()
	{
		if (开始滚动)
		{
			pos = new Vector2(50f * Time.fixedDeltaTime, 0f);
			if (滚动信息.anchoredPosition.x < 结束位置)
			{
				滚动信息.anchoredPosition = new Vector2(0f, 滚动信息.anchoredPosition.y);
			}
			else
			{
				滚动信息.anchoredPosition += -pos;
			}
		}
	}
}
