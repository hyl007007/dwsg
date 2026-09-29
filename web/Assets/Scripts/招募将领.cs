using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using 玩家数据结构;
using 缺失界面.窗口4;

public class 招募将领 : MonoBehaviour
{
	public Text 将领数对象;

	public Text 刷新时间对象;

	public Text 皇榜数量对象;

	public Text 招贤金榜数量对象;

	public Text 招贤数量令对象;

	public GameObject 将领列表对象;

	private bool 已初始化;
	private long 上次刷新 = -1;

	public List<将领信息> 将领列表 = new List<将领信息>();

	private void Start()
	{
		自动刷新招募将领();
	}

	private void OnEnable()
	{
		if (军事缺口入口.当前玩家() != null) 自动刷新招募将领();
	}

	private void 显示将领数量()
	{
		int 本机身份 = 全局变量.本机身份;
		double num = 全局变量.所有玩家数据表[本机身份].获取将领总数();
		double 将领数上限 = 全局变量.所有玩家数据表[本机身份].基础信息.将领数上限;
		将领数对象.text = "将领数:" + num.ToString() + "/" + 将领数上限.ToString();
	}

	private void 显示刷新时间()
	{
		刷新时间对象.text = TIME.ToTimeFormat(System.Math.Max(0, System.Math.Min(3600, 3600 - (TIME.getTime() - 全局变量.酒馆刷新时间))));
	}

	private void 显示道具数量()
	{
		int 本机身份 = 全局变量.本机身份;
		皇榜数量对象.text = 全局变量.所有玩家数据表[本机身份].背包道具列表.获取指定道具数量("皇榜").ToString() + "个";
		招贤金榜数量对象.text = "【招贤金榜】" + 全局变量.所有玩家数据表[本机身份].背包道具列表.获取指定道具数量("招贤金榜").ToString() + "个";
		招贤数量令对象.text = "【招贤令】" + 全局变量.所有玩家数据表[本机身份].背包道具列表.获取指定道具数量("招贤令").ToString() + "个";
	}

	public void 自动刷新招募将领()
	{
		if (军事缺口入口.当前玩家() == null) return;
		if (!已初始化 || TIME.getTime() - 全局变量.酒馆刷新时间 >= 3600) 随机5个将领(0);
		显示将领列表();
		显示刷新时间();
	}

	public void 招贤令刷新招募将领()
	{
		随机5个将领(1);
		显示将领列表();
	}

	public void 金榜刷新招募将领()
	{
		随机5个将领(2);
		显示将领列表();
	}

	public void 皇榜刷新招募将领()
	{
		随机5个将领(3);
		显示将领列表();
	}

	public void 招募选中将领()
	{
		添加将领到将领列表();
		显示将领列表();
	}

	public void 添加将领到将领列表()
	{
		int 本机身份 = 全局变量.本机身份;
		int 第几个封地 = 全局变量.第几个封地;
		var 玩家 = 军事缺口入口.当前玩家();
		if (玩家 == null || 第几个封地 < 0 || 第几个封地 >= 玩家.封地信息表.Count)
		{
			全局变量.提示类.显示信息("请先选择拥有的封地。");
			return;
		}
		int num = 0;
		while (true)
		{
			if (num < 5 && 将领列表对象.transform.GetChild(num).gameObject.activeSelf)
			{
				if (将领列表对象.transform.GetChild(num).GetChild(12).gameObject.activeSelf)
				{
					break;
				}
				num++;
				continue;
			}
			全局变量.提示类.显示信息("请先选择要招募的将领。");
			return;
		}
		if (num >= 将领列表.Count) return;
		if (全局变量.所有玩家数据表[本机身份].获取将领总数() < 全局变量.所有玩家数据表[本机身份].基础信息.将领数上限)
		{
			全局变量.所有玩家数据表[本机身份].添加将领信息到列表(第几个封地, 将领列表[num]);
			将领列表.RemoveAt(num);
			全局变量.提示类.显示信息("招募成功!");
		}
		else
		{
			全局变量.提示类.显示信息("招募失败,将领上限!");
		}
	}

	private void 显示将领列表()
	{
		if (军事缺口入口.当前玩家() == null) return;
		显示道具数量();
		显示将领数量();
		int count = 将领列表.Count;
		for (int i = 0; i < 5; i++)
		{
			var 行 = 将领列表对象.transform.GetChild(i);
			行.GetChild(12).gameObject.SetActive(false);
			var 勾选 = 行.GetComponent<Toggle>();
			if (勾选 != null) 勾选.SetIsOnWithoutNotify(false);
			将领列表对象.transform.GetChild(i).gameObject.SetActive(value: false);
			if (i >= count)
			{
				continue;
			}
			将领列表对象.transform.GetChild(i).gameObject.SetActive(value: true);
			将领属性库类 将领属性库类 = 全局将领库.查询指定ID的将领数据(将领列表[i].将领属性.初始属性.ID);
			if (将领属性库类 != null)
			{
				将领列表对象.transform.GetChild(i).GetChild(1).GetComponent<Image>()
					.sprite = 全局将领库.获取指定将领的头像(将领属性库类.名字);
				Animator component = 将领列表对象.transform.GetChild(i).GetChild(2).GetComponent<Animator>();
				将领列表对象.transform.GetChild(i).GetChild(2).gameObject.SetActive(value: false);
				if (将领属性库类.头像特效 != 0.0)
				{
					将领列表对象.transform.GetChild(i).GetChild(2).gameObject.SetActive(value: true);
					component.SetInteger("特效类型", (int)将领属性库类.头像特效);
				}
			}
			将领列表对象.transform.GetChild(i).GetChild(3).GetComponent<Text>()
				.text = 将领列表[i].将领属性.初始属性.名字 + "[" + 将领列表[i].将领属性.初始属性.获取职业名字() + "]";
			军事界面样式.限定名称(行.GetChild(3).GetComponent<Text>());
			Text component2 = 将领列表对象.transform.GetChild(i).GetChild(5).GetComponent<Text>();
			component2.text = 将领列表[i].将领属性.初始属性.成长.ToString();
			component2.color = 将领列表[i].将领属性.初始属性.获取酒馆将领名字颜色();
			将领列表对象.transform.GetChild(i).GetChild(7).GetComponent<Text>()
				.text = 将领列表[i].将领属性.初始属性.武力.ToString();
			将领列表对象.transform.GetChild(i).GetChild(9).GetComponent<Text>()
				.text = 将领列表[i].将领属性.初始属性.智力.ToString();
			将领列表对象.transform.GetChild(i).GetChild(11).GetComponent<Text>()
				.text = 将领列表[i].将领属性.初始属性.统帅.ToString();
			Text component3 = 将领列表对象.transform.GetChild(i).GetChild(13).GetComponent<Text>();
			component3.text = "(" + 将领列表[i].将领属性.初始属性.获取将领品质名称() + ")";
			component3.color = 将领列表[i].将领属性.初始属性.获取酒馆将领名字颜色();
		}
	}

	private void 随机5个将领(int 随机类型)
	{
		if (军事缺口入口.当前玩家() == null) return;
		int 本机身份 = 全局变量.本机身份;
		string a = "";
		switch (随机类型)
		{
			case 1:
				a = 全局变量.所有玩家数据表[本机身份].背包道具列表.使用道具("招贤令", 0, 0);
				break;
			case 2:
				a = 全局变量.所有玩家数据表[本机身份].背包道具列表.使用道具("招贤金榜", 0, 0);
				break;
			case 3:
				a = 全局变量.所有玩家数据表[本机身份].背包道具列表.使用道具("皇榜", 0, 0);
				break;
			case 0:
				随机类型 = 1;
				break;
		}
		if (a == "使用失败")
		{
			全局变量.提示类.显示信息("刷新道具不足。请选择其他刷新方式或等待自动刷新。");
			return;
		}
        全局变量.酒馆刷新时间 = TIME.getTime();
        已初始化 = true;
		将领列表.Clear();
		for (int i = 0; i < 5; i++)
		{
			将领信息 将领信息 = new 将领信息();
			if ((double)UnityEngine.Random.Range(0, 1000) < 0.0 && 随机类型 == 3)
			{
				将领属性库类 将领属性库类 = 全局将领库.查询指定名字的将领数据(全局将领库.随机获取一个君王名());
				if (将领属性库类 != null)
				{
					将领属性库类.获取随机属性();
					将领信息.生成将领数据(将领属性库类);
				}
			}
			else
			{
				将领属性库类 将领属性库类2 = new 将领属性库类();
				if (随机类型 == 3)
				{
					将领属性库类2 = 全局将领库.查询指定ID的将领数据(UnityEngine.Random.Range(1, 9));
				}
				if (随机类型 == 2)
				{
					将领属性库类2 = 全局将领库.查询指定ID的将领数据(UnityEngine.Random.Range(1, 7));
				}
				if (随机类型 == 1)
				{
					将领属性库类2 = 全局将领库.查询指定ID的将领数据(UnityEngine.Random.Range(1, 5));
				}
				将领属性库类2.获取随机属性();
				将领信息.生成将领数据(将领属性库类2);
				将领信息.将领属性.初始属性.名字 = 随机姓名.生成随机姓名();
			}
			将领信息.将领重置等级();
			将领列表.Add(将领信息);
		}
	}

	private void Update()
	{
		long 当前 = TIME.getTime();
		if (当前 == 上次刷新 || 军事缺口入口.当前玩家() == null) return;
		上次刷新 = 当前;
		if (!已初始化 || 当前 - 全局变量.酒馆刷新时间 >= 3600) 自动刷新招募将领();
		显示刷新时间();
		显示道具数量();
		显示将领数量();
	}
}
