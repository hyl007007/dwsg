using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class 显示建国城池列表 : MonoBehaviour
{
	public GameObject 显示列表对象;

	private List<城池信息库类> 要显示的列表;

	public 建国脚本 建国脚本对象;

	public void 刷新显示()
	{
		int childCount = 显示列表对象.transform.childCount;
		for (int i = 0; i < childCount; i++)
		{
			显示列表对象.transform.GetChild(i).gameObject.SetActive(value: false);
		}
		int 本机身份 = 全局变量.本机身份;
		要显示的列表 = 所有城池界面脚本.获取指定玩家县以上的城池列表(本机身份);
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
			Text component3 = gameObject.transform.GetChild(3).GetComponent<Text>();
			Text component4 = gameObject.transform.GetChild(4).GetComponent<Text>();
			component.sprite = 全局变量.城池规模头像资源表[要显示的列表[j].规模];
			component2.text = 要显示的列表[j].名称;
			component3.text = "规模:" + 要显示的列表[j].获取规模名称();
			component4.text = "坐标(" + 要显示的列表[j].坐标x.ToString() + "," + 要显示的列表[j].坐标y.ToString() + ")";
		}
	}

	public void 确定选择城池()
	{
		int childCount = 显示列表对象.transform.childCount;
		int num = 0;
		while (true)
		{
			if (num < childCount)
			{
				if (显示列表对象.transform.GetChild(num).GetChild(5).gameObject.activeSelf)
				{
					break;
				}
				num++;
				continue;
			}
			return;
		}
		建国脚本对象.国都城池信息 = 要显示的列表[num];
		建国脚本对象.国都对象.text = 要显示的列表[num].名称;
		base.gameObject.SetActive(value: false);
	}
}
