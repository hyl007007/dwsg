using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using 玩家数据结构;

public class 编队将领移动 : MonoBehaviour
{
	public List<将领信息> 编队将领信息;

	private float 编队入场结束位置x = 24.25f;

	private float 移动速度;

	private bool 是否移动;

	private int 设置攻守方;

	public void 开始移动编队(int 传递的攻守方, float 传递的移动速度)
	{
		设置攻守方 = 传递的攻守方;
		移动速度 = 传递的移动速度;
		if (设置攻守方 == 0)
		{
			编队入场结束位置x = 24.25f;
		}
		else if (设置攻守方 == 1)
		{
			编队入场结束位置x = -24.25f;
		}
		int count = 编队将领信息.Count;
		for (int i = 0; i < count; i++)
		{
			将领功能 component = base.transform.GetChild(i).GetComponent<将领功能>();
			component.设置将领模型图层(3 + i);
			if (移动速度 < 15f)
			{
				component.设置走动状态();
			}
			else
			{
				component.设置跑路状态();
			}
		}
		移动速度 = 传递的移动速度 * 0.5f;
		UnityEngine.Debug.Log("编队开始移动,移动速度：" + 移动速度.ToString());
		是否移动 = false;
		StartCoroutine(协程_编队移动());
	}

	private IEnumerator 协程_编队移动()
	{
		while (base.transform.localPosition.x != 编队入场结束位置x)
		{
			float maxDistanceDelta = 移动速度 * Time.deltaTime;
			base.transform.localPosition = Vector2.MoveTowards(base.transform.localPosition, new Vector2(编队入场结束位置x, base.transform.localPosition.y), maxDistanceDelta);
			yield return null;
		}
		是否移动 = false;
		编队将领上坑 component = base.transform.parent.gameObject.transform.GetComponent<编队将领上坑>();
		等待队列表信息 item = new 等待队列表信息
		{
			编队对象 = base.gameObject,
			编队将领列表 = 编队将领信息
		};
		component.等待上坑队列表.Add(item);
		int count = 编队将领信息.Count;
		for (int i = 0; i < count; i++)
		{
			base.transform.GetChild(i).GetComponent<将领功能>().设置等待状态();
		}
	}

	private void FixedUpdate()
	{
		if (!是否移动)
		{
			return;
		}
		if (base.transform.localPosition.x != 编队入场结束位置x)
		{
			float maxDistanceDelta = 移动速度 * Time.deltaTime;
			base.transform.localPosition = Vector2.MoveTowards(base.transform.localPosition, new Vector2(编队入场结束位置x, base.transform.localPosition.y), maxDistanceDelta);
			return;
		}
		是否移动 = false;
		编队将领上坑 component = base.transform.parent.gameObject.transform.GetComponent<编队将领上坑>();
		等待队列表信息 item = new 等待队列表信息
		{
			编队对象 = base.gameObject,
			编队将领列表 = 编队将领信息
		};
		component.等待上坑队列表.Add(item);
		int count = 编队将领信息.Count;
		for (int i = 0; i < count; i++)
		{
			base.transform.GetChild(i).GetComponent<将领功能>().设置等待状态();
		}
	}
}
