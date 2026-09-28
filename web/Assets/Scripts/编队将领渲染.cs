using System.Collections.Generic;
using UnityEngine;
using 玩家数据结构;

public class 编队将领渲染 : MonoBehaviour
{
	public int 设置攻守方;

	private List<List<将领信息>> 要判断的编队列表;

	private void 开始渲染编队将领()
	{
		战斗系统 component = base.transform.parent.gameObject.transform.GetComponent<战斗系统>();
		if (设置攻守方 == 0)
		{
			要判断的编队列表 = component.攻方要渲染的编队将领列表;
		}
		else if (设置攻守方 == 1)
		{
			要判断的编队列表 = component.守方要渲染的编队将领列表;
		}
		if (要判断的编队列表 == null)
		{
			return;
		}
		int count = 要判断的编队列表.Count;
		for (int i = 0; i < count; i++)
		{
			int count2 = 要判断的编队列表[i].Count;
			GameObject gameObject = UnityEngine.Object.Instantiate(全局变量.编队对象pre);
			gameObject.transform.SetParent(base.transform);
			if (设置攻守方 == 0)
			{
				if (component.战场类型 == 1)
				{
					gameObject.transform.localPosition = new Vector2(-40.25f, 0f);
				}
				else
				{
					gameObject.transform.localPosition = new Vector2(-24.25f, 0f);
				}
			}
			else if (设置攻守方 == 1)
			{
				if (component.战场类型 == 1)
				{
					gameObject.transform.localPosition = new Vector2(-24f, 0f);
				}
				else
				{
					gameObject.transform.localPosition = new Vector2(24.25f, 0f);
				}
			}
			编队将领移动 component2 = gameObject.GetComponent<编队将领移动>();
			component2.编队将领信息 = new List<将领信息>();
			float num = 0f;
			for (int j = 0; j < count2; j++)
			{
				component2.编队将领信息.Add(要判断的编队列表[i][j]);
				GameObject gameObject2 = UnityEngine.Object.Instantiate(全局变量.将领对象pre);
				gameObject2.transform.SetParent(gameObject.transform);
				将领功能 component3 = gameObject2.GetComponent<将领功能>();
				component3.本将领信息 = 要判断的编队列表[i][j];
				component3.开始渲染将领();
				int num2 = 全局兵种库.查询指定ID的索引(要判断的编队列表[i][j].将领配兵.ID);
				if (num2 != -1)
				{
					if (num == 0f)
					{
						num = 全局兵种库.属性表[num2].移动速度;
					}
					else if (全局兵种库.属性表[num2].移动速度 < num)
					{
						num = 全局兵种库.属性表[num2].移动速度;
					}
				}
				if (设置攻守方 == 0)
				{
					component.攻身份 = (int)component.攻方要渲染的编队将领列表[0][0].详细信息.身份;
					component.攻方兵力 += 要判断的编队列表[i][j].将领配兵.数量;
				}
				else if (设置攻守方 == 1)
				{
					component.守身份 = (int)component.守方要渲染的编队将领列表[0][0].详细信息.身份;
					component.守方兵力 += 要判断的编队列表[i][j].将领配兵.数量;
				}
			}
			component2.开始移动编队(设置攻守方, num);
			component.开始检测战斗结果 = true;
		}
		if (设置攻守方 == 0)
		{
			component.攻方要渲染的编队将领列表.Clear();
		}
		else if (设置攻守方 == 1)
		{
			component.守方要渲染的编队将领列表.Clear();
		}
	}

	private void Update()
	{
		开始渲染编队将领();
	}
}
