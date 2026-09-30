using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Dwsg.Window3;

public class 所有城池界面脚本 : MonoBehaviour
{
	public ScrollRect 滑动对象;

	public GameObject 城池信息界面UI;

	public static int 地图W;

	public static int 地图H;

	public Text 当前城池坐标显示;

	public GameObject 势力列表对象;

	private void Start()
	{
		显示所有城池();
		刷新所有城池();
		定位当前封地位置();
	}

	public void 渲染势力地图()
	{
		if (势力列表对象 == null || 势力列表对象.transform.childCount == 0 || 全局变量.所有城池列表 == null) return;
		var plotted = new List<城池信息库类>();
		int num = 0;
		float num2 = -465f;
		float num3 = 260f;
		float num4 = 5f;
		float num5 = 9.5f;
		int childCount = 势力列表对象.transform.childCount;
		for (int i = 0; i < childCount; i++)
		{
			势力列表对象.transform.GetChild(i).gameObject.SetActive(value: false);
		}
		for (int j = 0; j < 地图H; j++)
		{
			for (int k = 0; k < 地图W; k++)
			{
				if (全局大地图库.大地图表[j, k] >= 2 && num < 全局变量.所有城池列表.Count)
				{
					GameObject gameObject;
					if (childCount <= num)
					{
						gameObject = UnityEngine.Object.Instantiate(势力列表对象.transform.GetChild(0).gameObject);
						gameObject.transform.SetParent(势力列表对象.transform);
						gameObject.transform.localScale = new Vector3(1f, 1f, 1f);
					}
					else
					{
						gameObject = 势力列表对象.transform.GetChild(num).gameObject;
					}
					gameObject.transform.GetComponent<RectTransform>().sizeDelta = new Vector2(8f, 8f);
					gameObject.transform.localPosition = new Vector2(num2 + (float)k * num4, num3 - (float)j * num5);
					势力列表对象.transform.GetChild(num).gameObject.SetActive(value: true);
					var city = 全局变量.所有城池列表[num];
					gameObject.SetActive(city != null);
					if (city != null)
					{
						gameObject.GetComponent<Image>().color = CityMapPresentation.OwnershipColor(city.获取城池身份());
						plotted.Add(city);
					}
					num++;
				}
			}
		}
		CityMapPresentation.PowerLegend(势力列表对象, plotted);
	}

	public void 定位当前封地位置()
	{
		int 本机身份 = 全局变量.本机身份;
		int 第几个封地 = 全局变量.第几个封地;
		坐标 所在城池 = 全局变量.所有玩家数据表[本机身份].封地信息表[第几个封地].所在城池;
		定位地图到指定位置(所在城池.x, 所在城池.y);
		当前城池坐标显示.text = 所在城池.x.ToString() + "," + 所在城池.y.ToString();
	}

	public void 定位地图到指定位置(int x, int y)
	{
		if (滑动对象 == null || x < 1 || y < 1 || x > 地图W || y > 地图H) return;
		float num = 0.0054f;
		float num2 = 0.0222f;
		滑动对象.normalizedPosition = new Vector2((float)x * num, 1f - (float)y * num2);
		if (当前城池坐标显示 != null) 当前城池坐标显示.text = x + "," + y;
	}

	public static void 初始化城池列表()
	{
		int num = 0;
		地图W = 全局大地图库.大地图表.GetLength(1);
		地图H = 全局大地图库.大地图表.GetLength(0);
		for (int i = 0; i < 地图H; i++)
		{
			for (int j = 0; j < 地图W; j++)
			{
				if (全局大地图库.大地图表[i, j] >= 2)
				{
					城池信息库类 城池信息库类 = new 城池信息库类();
					if (num < 全局城池库.城池名称.Length)
					{
						城池信息库类.名称 = 全局城池库.城池名称[num];
					}
					else
					{
						城池信息库类.名称 = (num + 1).ToString();
					}
					城池信息库类.国家 = "";
					城池信息库类.城主 = -1;
					城池信息库类.坐标x = j + 1;
					城池信息库类.坐标y = i + 1;
					城池信息库类.生成指定规模城池数据(全局大地图库.大地图表[i, j] - 2);
					全局变量.所有城池列表.Add(城池信息库类);
					num++;
				}
			}
		}
	}

	public int 获取城池距离(int x0, int y0, int x1, int y1)
	{
		return -1;
	}

	public string 获取城池右边路径(int 坐标x, int 坐标y)
	{
		for (int i = 1; i < 10; i++)
		{
			int num = 坐标x + i;
			if (num < 地图W)
			{
				if (全局大地图库.大地图表[坐标y, num] == 0)
				{
					break;
				}
				if (全局大地图库.大地图表[坐标y, num] >= 2)
				{
					return i.ToString() + "_0";
				}
			}
		}
		return "0_0";
	}

	public string 获取城池下边路径(int 坐标x, int 坐标y)
	{
		int num = 坐标y + 1;
		if (num < 地图H)
		{
			if (坐标x - 1 >= 0 && 全局大地图库.大地图表[num, 坐标x - 1] == 1)
			{
				for (int i = 1; i < 4; i++)
				{
					if (全局大地图库.大地图表[num, 坐标x - i] == 1)
					{
						if (坐标x - i - 1 >= 0 && 全局大地图库.大地图表[num + 1, 坐标x - i - 1] >= 2)
						{
							return "-" + (i + 1).ToString() + "_2";
						}
						if (全局大地图库.大地图表[num + 1, 坐标x - i] >= 2)
						{
							return "-" + i.ToString() + "_2";
						}
					}
				}
			}
			if (全局大地图库.大地图表[num, 坐标x] == 1)
			{
				return "0_2";
			}
			if (坐标x + 1 < 地图W && 全局大地图库.大地图表[num, 坐标x + 1] == 1)
			{
				for (int j = 1; j < 4; j++)
				{
					if (坐标x + j < 地图W && 全局大地图库.大地图表[num, 坐标x + j] == 1)
					{
						if (坐标x + j + 1 < 地图W && 全局大地图库.大地图表[num + 1, 坐标x + j + 1] >= 2)
						{
							return (j + 1).ToString() + "_2";
						}
						if (坐标x + j < 地图W && 全局大地图库.大地图表[num + 1, 坐标x + j] >= 2)
						{
							return j.ToString() + "_2";
						}
					}
				}
			}
		}
		return "0_0";
	}

	public static int 获取指定玩家的城池数量(int 玩家索引)
	{
		int num = 0;
		int count = 全局变量.所有城池列表.Count;
		for (int i = 0; i < count; i++)
		{
			if (全局变量.所有城池列表[i].城主 == 玩家索引)
			{
				num++;
			}
		}
		return num;
	}

	public static List<城池信息库类> 获取指定玩家县以上的城池列表(int 玩家索引)
	{
		List<城池信息库类> list = new List<城池信息库类>();
		int count = 全局变量.所有城池列表.Count;
		for (int i = 0; i < count; i++)
		{
			if (全局变量.所有城池列表[i].城主 == 玩家索引 && 全局变量.所有城池列表[i].规模 > 0)
			{
				list.Add(全局变量.所有城池列表[i]);
			}
		}
		return list;
	}

	public static void 重置城池状态()
	{
		int count = 全局变量.所有城池列表.Count;
		for (int i = 0; i < count; i++)
		{
			全局变量.所有城池列表[i].正在交战 = false;
		}
	}

	public static 城池信息库类 根据坐标获取指定城池(int 坐标x, int 坐标y)
	{
		return 全局变量.所有城池列表.Find(城池 => 城池 != null && 城池.坐标x == 坐标x && 城池.坐标y == 坐标y);
	}

	public void 显示所有城池()
	{
		float num = -16.8f;
		float num2 = 7.2f;
		float num3 = 0.7f;
		int num4 = 0;
		for (int i = 0; i < 地图H; i++)
		{
			for (int j = 0; j < 地图W; j++)
			{
				if (全局大地图库.大地图表[i, j] < 2)
				{
					continue;
				}
				GameObject gameObject = UnityEngine.Object.Instantiate(全局变量.城池信息布局pre);
				gameObject.transform.SetParent(base.transform);
				gameObject.transform.localPosition = new Vector2(num + (float)j * num3, num2 - (float)i * num3);
				Text component = gameObject.transform.GetChild(1).GetComponent<Text>();
				if (num4 < 全局城池库.城池名称.Length)
				{
					component.text = 全局城池库.城池名称[num4];
				}
				else
				{
					component.text = (num4 + 1).ToString();
				}
				string text = 获取城池右边路径(j, i);
				if (text != "0_0")
				{
					for (int k = 0; k < 全局变量.所有城池路径pre.Length; k++)
					{
						if (全局变量.所有城池路径pre[k].name == text)
						{
							GameObject gameObject2 = UnityEngine.Object.Instantiate(全局变量.所有城池路径pre[k]);
							gameObject2.transform.SetParent(gameObject.transform);
							gameObject2.transform.localPosition = new Vector2(0f, 0f);
							break;
						}
					}
				}
				string text2 = 获取城池下边路径(j, i);
				if (text2 != "0_0")
				{
					for (int l = 0; l < 全局变量.所有城池路径pre.Length; l++)
					{
						if (全局变量.所有城池路径pre[l].name == text2)
						{
							GameObject gameObject3 = UnityEngine.Object.Instantiate(全局变量.所有城池路径pre[l]);
							gameObject3.transform.SetParent(gameObject.transform);
							gameObject3.transform.localPosition = new Vector2(0f, 0f);
							break;
						}
					}
				}
				num4++;
			}
		}
	}

	public void 打开城池信息(int 第几个城池)
	{
		if (城池信息界面UI == null || 第几个城池 < 0 || 第几个城池 >= 全局变量.所有城池列表.Count) return;
		城池信息界面UI.GetComponent<城池信息显示脚本>().显示城池信息(第几个城池);
		界面窗口动画.设置显示(城池信息界面UI, true);
	}

	public void 刷新所有城池()
	{
		int count = 全局变量.所有城池列表.Count;
		int childCount = base.transform.childCount;
		for (int i = 0; i < count; i++)
		{
			显示指定城池(i);
		}
	}

	private void 显示指定城池(int 第几个城池)
	{
		Text component = base.transform.GetChild(第几个城池).GetChild(1).GetComponent<Text>();
		base.transform.GetChild(第几个城池).GetChild(2).GetComponent<SpriteRenderer>()
			.sprite = 全局变量.城池归属图标资源表[全局变量.所有城池列表[第几个城池].获取城池身份()];
			Text component2 = base.transform.GetChild(第几个城池).GetChild(3).GetComponent<Text>();
			if (全局变量.所有城池列表[第几个城池].国家 != "")
			{
				国家信息库类 国家信息库类 = 全局方法类.获取指定名字的国家(全局变量.所有城池列表[第几个城池].国家);
				if (国家信息库类 != null)
				{
					component2.text = 国家信息库类.国号;
					if (全局变量.所有城池列表[第几个城池].获取城池身份() == 0)
					{
						component2.color = 颜色类.GetColor("#0A83FF");
						component.color = 颜色类.GetColor("#0A83FF");
					}
					else if (全局变量.所有城池列表[第几个城池].获取城池身份() == 1)
					{
						component2.color = new Color(0.94f, 0.09f, 0.05f, 1f);
						component.color = new Color(0.94f, 0.09f, 0.05f, 1f);
					}
					else if (全局变量.所有城池列表[第几个城池].获取城池身份() == 2)
					{
						component2.color = 颜色类.GetColor("#FFC847");
						component.color = 颜色类.GetColor("#FFC847");
					}
				}
			}
			else
			{
				component2.text = "";
				component2.color = 颜色类.GetColor("#908E90");
				component.color = 颜色类.GetColor("#908E90");
			}
			base.transform.GetChild(第几个城池).GetChild(4).GetComponent<SpriteRenderer>()
				.sprite = 全局变量.城池规模图片资源表[全局变量.所有城池列表[第几个城池].规模];
				base.transform.GetChild(第几个城池).GetChild(5).gameObject.SetActive(全局变量.所有城池列表[第几个城池].是否属于我的城池());
				base.transform.GetChild(第几个城池).GetChild(6).gameObject.SetActive(全局变量.所有城池列表[第几个城池].是否有我的封地());
				base.transform.GetChild(第几个城池).GetChild(7).gameObject.SetActive(全局变量.所有城池列表[第几个城池].正在交战);
			}
		}
