using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class 编队将领上坑 : MonoBehaviour
{
	public int 设置攻守方;

	public List<等待队列表信息> 等待上坑队列表;

	private 战斗系统 战斗系统脚本对象;

	private void Start()
	{
		战斗系统脚本对象 = base.transform.parent.gameObject.transform.GetComponent<战斗系统>();
		等待上坑队列表 = new List<等待队列表信息>();
		StartCoroutine(开始判断上坑());
	}

	private IEnumerator 开始判断上坑()
	{
		while (true)
		{
			if (等待上坑队列表.Count > 0 && 等待上坑队列表[0].编队将领列表.Count > 0)
			{
				int num = 全局兵种库.查询指定ID的索引(等待上坑队列表[0].编队将领列表[0].将领配兵.ID);
				if (num > -1)
				{
					int num2 = 获取可上坑位(全局兵种库.属性表[num].兵种);
					if (num2 > -1 && 等待上坑队列表[0].编队对象.transform.childCount > 0)
					{
						GameObject gameObject = 等待上坑队列表[0].编队对象.transform.GetChild(0).gameObject;
						if (gameObject != null)
						{
							if (全局兵种库.属性表[num].兵种 > 2.0)
							{
								int num3 = 获取可挤坑位(全局兵种库.属性表[num].兵种);
								if (num3 > -1)
								{
									bool flag = false;
									if (num2 < 5)
									{
										if (num3 >= 5)
										{
											flag = true;
										}
									}
									else if (num2 < 10 && num3 >= 10)
									{
										flag = true;
									}
									if (flag)
									{
										int num4 = 0;
										if (设置攻守方 == 0)
										{
											num4 = 战斗系统脚本对象.攻方坑位对象.transform.GetChild(num3).childCount;
										}
										else if (设置攻守方 == 1)
										{
											num4 = 战斗系统脚本对象.守方坑位对象.transform.GetChild(num3).childCount;
										}
										if (num4 > 0)
										{
											GameObject 将领对象 = null;
											if (设置攻守方 == 0)
											{
												将领对象 = 战斗系统脚本对象.攻方坑位对象.transform.GetChild(num3).GetChild(0).gameObject;
											}
											else if (设置攻守方 == 1)
											{
												将领对象 = 战斗系统脚本对象.守方坑位对象.transform.GetChild(num3).GetChild(0).gameObject;
											}
											将领进坑上位(将领对象, num2);
										}
										num2 = num3;
									}
								}
							}
							将领进坑上位(gameObject, num2);
							将领功能 component = gameObject.GetComponent<将领功能>();
							component.将领显示星星();
							component.将领显示血条();
							component.开始战斗();
							等待上坑队列表[0].编队将领列表.RemoveAt(0);
							if (等待上坑队列表[0].编队将领列表.Count <= 0)
							{
								UnityEngine.Debug.Log("编队将领已全部上坑 ,清理!");
								等待上坑队列表.RemoveAt(0);
							}
						}
					}
				}
			}
			yield return null;
		}
	}

	private int 获取可上坑位(double 兵种)
	{
		List<int> list = new List<int>
		{
			2,
			1,
			3,
			0,
			4,
			7,
			6,
			8,
			5,
			9,
			12,
			11,
			13,
			10,
			14
		};
		if (兵种 == 3.0)
		{
			list = new List<int>
			{
				7,
				6,
				8,
				5,
				9,
				12,
				11,
				13,
				10,
				14,
				2,
				1,
				3,
				0,
				4
			};
		}
		else if (兵种 == 4.0)
		{
			list = new List<int>
			{
				12,
				11,
				13,
				10,
				14,
				7,
				6,
				8,
				5,
				9,
				2,
				1,
				3,
				0,
				4
			};
		}
		int num = 0;
		for (int i = 0; i < 15; i++)
		{
			if (设置攻守方 == 0)
			{
				num = 战斗系统脚本对象.攻方坑位对象.transform.GetChild(list[i]).childCount;
			}
			else if (设置攻守方 == 1)
			{
				num = 战斗系统脚本对象.守方坑位对象.transform.GetChild(list[i]).childCount;
			}
			if (num == 0)
			{
				return list[i];
			}
		}
		return -1;
	}

	private int 获取可挤坑位(double 兵种)
	{
		if (兵种 < 3.0)
		{
			return -1;
		}
		List<int> list = new List<int>();
		if (兵种 == 3.0)
		{
			list = new List<int>
			{
				7,
				6,
				8,
				5,
				9,
				12,
				11,
				13,
				10,
				14,
				2,
				1,
				3,
				0,
				4
			};
		}
		else if (兵种 == 4.0)
		{
			list = new List<int>
			{
				12,
				11,
				13,
				10,
				14,
				7,
				6,
				8,
				5,
				9,
				2,
				1,
				3,
				0,
				4
			};
		}
		int num = 0;
		for (int i = 0; i < 10; i++)
		{
			GameObject gameObject = null;
			if (设置攻守方 == 0)
			{
				gameObject = 战斗系统脚本对象.攻方坑位对象.transform.GetChild(list[i]).gameObject;
				num = gameObject.transform.childCount;
			}
			else if (设置攻守方 == 1)
			{
				gameObject = 战斗系统脚本对象.守方坑位对象.transform.GetChild(list[i]).gameObject;
				num = gameObject.transform.childCount;
			}
			if (num > 0)
			{
				int num2 = 全局兵种库.查询指定ID的索引(gameObject.transform.GetChild(0).GetComponent<将领功能>().本将领信息.将领配兵.ID);
				if (num2 != -1 && 全局兵种库.属性表[num2].兵种 < 兵种)
				{
					return list[i];
				}
				continue;
			}
			return list[i];
		}
		return -1;
	}

	private void 将领进坑上位(GameObject 将领对象, int 第几个坑位)
	{
		GameObject gameObject = 战斗系统脚本对象.攻方坑位对象;
		if (设置攻守方 == 1)
		{
			gameObject = 战斗系统脚本对象.守方坑位对象;
		}
		if (gameObject.transform.GetChild(第几个坑位).childCount == 0)
		{
			将领对象.transform.SetParent(gameObject.transform.GetChild(第几个坑位).transform);
			switch (第几个坑位)
			{
			case 1:
			case 6:
			case 11:
				将领对象.transform.GetChild(2).GetComponent<SpriteRenderer>().sortingOrder = 3;
				break;
			case 0:
			case 5:
			case 10:
				将领对象.transform.GetChild(2).GetComponent<SpriteRenderer>().sortingOrder = 2;
				break;
			case 2:
			case 7:
			case 12:
				将领对象.transform.GetChild(2).GetComponent<SpriteRenderer>().sortingOrder = 4;
				break;
			case 3:
			case 8:
			case 13:
				将领对象.transform.GetChild(2).GetComponent<SpriteRenderer>().sortingOrder = 5;
				break;
			case 4:
			case 9:
			case 14:
				将领对象.transform.GetChild(2).GetComponent<SpriteRenderer>().sortingOrder = 6;
				break;
			}
			StartCoroutine(移动将领对象(将领对象));
		}
		else
		{
			StartCoroutine(移动将领对象(将领对象));
		}
	}

	private IEnumerator 移动将领对象(GameObject 将领对象)
	{
		while (true)
		{
			float maxDistanceDelta = 35f * Time.deltaTime;
			if (!将领对象)
			{
				break;
			}
			if (将领对象.transform.localPosition.x != 0f)
			{
				将领对象.transform.localPosition = Vector2.MoveTowards(将领对象.transform.localPosition, new Vector2(0f, 3.15f), maxDistanceDelta);
				yield return null;
				continue;
			}
			yield break;
		}
		UnityEngine.Debug.Log("移动目标不存在");
	}
}
