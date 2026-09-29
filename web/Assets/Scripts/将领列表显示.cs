using Dwsg.Generals;
using Dwsg.Network;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using 玩家数据结构;

public class 将领列表显示 : MonoBehaviour
{
	private int 第几个玩家;

	public int 显示第几个封地 = -1;

	public Text 页数显示;

	public Text 将领数量显示;

	public List<GameObject> 将领列表对象;

	public Toggle 将领1选中开关;

	public Text 选择的封地显示对象;

	public GameObject 将领详情头像显示对象;

	public GameObject 将领成长信息对象;

	public GameObject 将领属性信息对象;

	public GameObject 将领装备信息对象;

	public GameObject 将领配兵信息对象;

	public GameObject 将领培养信息对象;

	public 更换装备脚本 更换装备脚本对象;

	public 使用道具脚本 使用道具脚本对象;

	public List<将领索引信息> 要显示的将领列表 = new List<将领索引信息>();

	private float 总页数;

	private int 第几页将领;

	private float 闲兵总页数;

	private int 第几页闲兵;

	private 将领信息 培养选择将领;

	private double 培养选择次数;

	public 强化脚本 强化脚本对象;

	public 炼魂脚本 炼魂脚本对象;

	public 将领改名脚本 将领改名脚本对象;

	private void Start()
	{
	}

	public void 默认显示全部封地将领()
	{
		显示第几个封地 = -1;
	}

	public void 重置刷新将领列表()
	{
		第几页将领 = 0;
		获取要显示的将领列表();
		刷新列表信息();
		刷新将领属性信息();
	}

	public void 列表左翻页()
	{
		if (第几页将领 != 0)
		{
			第几页将领--;
			将领1选中开关.isOn = true;
			刷新列表信息();
			刷新将领属性信息();
		}
	}

	public void 列表右翻页()
	{
		if ((float)第几页将领 < 总页数 - 1f)
		{
			第几页将领++;
			将领1选中开关.isOn = true;
			刷新列表信息();
			刷新将领属性信息();
		}
	}

	public void 刷新将领属性信息()
	{
		显示选中将领详细信息();
	}

	public void 刷新列表信息()
	{
		列表显示5个将领();
	}

	public void 一键加忠()
	{
		int 总黄金 = 0;
		int num = 0;
		for (int i = 0; i < 全局变量.所有玩家数据表[全局变量.本机身份].封地信息表[0].将领信息表.Count; i++)
		{
			int 需要增加的忠诚 = 99 - (int)全局变量.所有玩家数据表[全局变量.本机身份].封地信息表[0].将领信息表[i].详细信息.忠诚;
			if (需要增加的忠诚 > 0)
			{
				int 黄金 = 100 * 需要增加的忠诚;
				if (全局变量.所有玩家数据表[全局变量.本机身份].财产信息.黄金 >= 黄金)
				{
					全局变量.所有玩家数据表[全局变量.本机身份].封地信息表[0].将领信息表[i].详细信息.忠诚 = 99;
					全局变量.所有玩家数据表[全局变量.本机身份].财产信息.黄金 -= 黄金;
					总黄金 += 黄金;
					num++;
				}
				else
					break;
			}

		}
		if (总黄金 > 0)
		{
			if (全局变量.所有玩家数据表[全局变量.本机身份].封地信息表[0].将领信息表.Count - num > 0)
				全局变量.提示类.显示信息($"本次增加了{num}个将领忠诚\n一共消耗了{总黄金}黄金\n还剩{全局变量.所有玩家数据表[全局变量.本机身份].封地信息表[0].将领信息表.Count - num}个将领忠诚未增加");
			else if (num > 0)
				全局变量.提示类.显示信息($"本次增加了{num}个将领忠诚\n一共消耗了{总黄金}黄金\n");
			else
				全局变量.提示类.显示信息($"黄金不足");

		}
		else
			全局变量.提示类.显示信息($"增加失败");
	}
	public void 解雇将领()
	{
		if (GameNetwork.Enabled)
		{
			执行将领操作("generals.dismiss", new JObject());
			return;
		}
		if (删除指定将领())
		{
			重置刷新将领列表();
		}
	}

	public void 将领增加经验()
	{
		将领获得经验计算(1000000.0);
		刷新将领属性信息();
		刷新列表信息();
	}

	public void 将领武力加点()
	{
		将领分配加点("武力", 0);
	}

	public void 将领武力全加()
	{
		将领分配加点("武力", 1);
	}

	public void 将领智力加点()
	{
		将领分配加点("智力", 0);
	}

	public void 将领智力全加()
	{
		将领分配加点("智力", 1);
	}

	public void 将领统帅加点()
	{
		将领分配加点("统帅", 0);
	}

	public void 将领统帅全加()
	{
		将领分配加点("统帅", 1);
	}

	public void 将领洗点()
	{
		将领清空分配加点();
	}

	public void 解除配兵()
	{
		将领解除配兵();
	}

	public void 补满配兵()
	{
		将领补满配兵();
	}

	public void 点击第1个配兵()
	{
		将领指定配兵(0);
	}

	public void 点击第2个配兵()
	{
		将领指定配兵(1);
	}

	public void 点击第3个配兵()
	{
		将领指定配兵(2);
	}

	public void 点击第4个配兵()
	{
		将领指定配兵(3);
	}

	public void 点击第5个配兵()
	{
		将领指定配兵(4);
	}

	public void 点击第6个配兵()
	{
		将领指定配兵(5);
	}

	public void 配兵左翻页()
	{
		if (第几页闲兵 != 0)
		{
			第几页闲兵--;
			刷新列表信息();
			刷新将领属性信息();
		}
	}

	public void 配兵右翻页()
	{
		if ((float)第几页闲兵 < 闲兵总页数 - 1f)
		{
			第几页闲兵++;
			刷新列表信息();
			刷新将领属性信息();
		}
	}

	public void 培养增加次数()
	{
		将领加减培养次数(0);
		刷新列表信息();
		刷新将领属性信息();
	}

	public void 培养减少次数()
	{
		将领加减培养次数(1);
		刷新列表信息();
		刷新将领属性信息();
	}

	public void 培养将领()
	{
		将领培养计算();
	}

	public void 获取要显示的将领列表()
	{
		第几个玩家 = 全局变量.本机身份;
		int 本机身份 = 全局变量.本机身份;
		int num = 显示第几个封地;
		要显示的将领列表.Clear();
		if (num != -1)
		{
			int count = 全局变量.所有玩家数据表[本机身份].封地信息表[num].将领信息表.Count;
			for (int i = 0; i < count; i++)
			{
				要显示的将领列表.Add(new 将领索引信息(num, i));
			}
			选择的封地显示对象.text = 全局变量.所有玩家数据表[本机身份].封地信息表[num].封地名字;
		}
		else if (num == -1)
		{
			int count2 = 全局变量.所有玩家数据表[本机身份].封地信息表.Count;
			for (int j = 0; j < count2; j++)
			{
				int count3 = 全局变量.所有玩家数据表[本机身份].封地信息表[j].将领信息表.Count;
				for (int k = 0; k < count3; k++)
				{
					要显示的将领列表.Add(new 将领索引信息(j, k));
				}
			}
			选择的封地显示对象.text = "全部";
		}
		int count4 = 要显示的将领列表.Count;
		总页数 = Mathf.Ceil((float)count4 / 5f);
		将领数量显示.text = "将领数" + 全局变量.所有玩家数据表[本机身份].获取将领总数().ToString() + "/" + 全局变量.所有玩家数据表[本机身份].基础信息.将领数上限.ToString();
	}

	private void 隐藏所有将领()
	{
		for (int i = 0; i < 5; i++)
		{
			将领列表对象[i].SetActive(value: false);
		}
	}

	private void 列表显示5个将领()
	{
		隐藏所有将领();
		列表页数更新显示();
		int count = 要显示的将领列表.Count;
		int num = 第几页将领 * 5;
		int num2 = num + 5;
		if (num2 >= count)
		{
			num2 = count;
		}
		int num3 = 0;
		for (int i = num; i < num2; i++)
		{
			int 第几个封地 = 要显示的将领列表[i].第几个封地;
			int 第几个将领 = 要显示的将领列表[i].第几个将领;
			将领列表对象[num3].SetActive(value: true);
			Image component = 将领列表对象[num3].transform.GetChild(2).GetComponent<Image>();
			string text = 全局将领库.查询指定ID的名字(全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地].将领信息表[第几个将领].将领属性.初始属性.ID);
			if (text != "未知")
			{
				component.sprite = 全局将领库.获取指定将领的头像(text);
			}
			else
			{
				component.sprite = 全局变量.未知头像;
			}
			Animator component2 = 将领列表对象[num3].transform.GetChild(2).GetChild(0).GetComponent<Animator>();
			将领列表对象[num3].transform.GetChild(2).GetChild(0).gameObject.SetActive(value: false);
			int num4 = 全局将领库.查询指定ID的头像特效(全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地].将领信息表[第几个将领].将领属性.初始属性.ID);
			if (num4 != 0)
			{
				将领列表对象[num3].transform.GetChild(2).GetChild(0).gameObject.SetActive(value: true);
				component2.SetInteger("特效类型", num4);
			}
			else
			{
				将领列表对象[num3].transform.GetChild(2).GetChild(0).gameObject.SetActive(value: false);
			}
			将领列表对象[num3].transform.GetChild(3).GetComponent<Text>().text = 全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地].将领信息表[第几个将领].将领属性.成长点数.等级.ToString();
			Text component3 = 将领列表对象[num3].transform.GetChild(4).GetComponent<Text>();
			component3.text = 全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地].将领信息表[第几个将领].将领属性.初始属性.名字;
			component3.color = 全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地].将领信息表[第几个将领].将领属性.初始属性.获取将领名字颜色();
			Text component4 = 将领列表对象[num3].transform.GetChild(5).GetComponent<Text>();
			component4.text = "未配兵";
			component4.color = new Color(1f, 0f, 0f);
			Text component5 = 将领列表对象[num3].transform.GetChild(6).GetComponent<Text>();
			将领列表对象[num3].transform.GetChild(6).gameObject.SetActive(value: false);
			if (全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地].将领信息表[第几个将领].将领配兵.数量 > 0.0)
			{
				int num5 = 全局兵种库.查询指定ID的索引(全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地].将领信息表[第几个将领].将领配兵.ID);
				if (num5 != -1)
				{
					将领列表对象[num3].transform.GetChild(6).gameObject.SetActive(value: true);
					component4.text = 全局兵种库.属性表[num5].名称;
					component5.text = 全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地].将领信息表[第几个将领].将领配兵.数量.ToString() + "/" + 全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地].将领信息表[第几个将领].将领属性.最终属性.统兵.ToString();
					component4.color = new Color(1f, 1f, 1f);
				}
			}
			将领列表对象[num3].transform.GetChild(7).GetComponent<Text>().text = 全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地].将领信息表[第几个将领].将领属性.初始属性.获取职业名字();
			将领列表对象[num3].transform.GetChild(8).GetComponent<Image>().sprite = 全局变量.将领状态图标资源表[(int)全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地].将领信息表[第几个将领].详细信息.状态];
			num3++;
		}
	}

	private int 获取选中将领索引()
	{
		int count = 要显示的将领列表.Count;
		int num = 0;
		int num2 = 0;
		num2 = 第几页将领 * 5;
		for (int i = 0; i < 5; i++)
		{
			num = num2 + i;
			if (num < count && 将领列表对象[i].transform.GetChild(0).gameObject.activeSelf)
			{
				return num;
			}
		}
		return -1;
	}

	private void 显示选中将领详细信息()
	{
		if (!GameNetwork.Enabled) 全局变量.所有玩家数据表[第几个玩家].计算最终属性();
		int num = 获取选中将领索引();
		if (num == -1)
		{
			return;
		}
		int 第几个封地 = 要显示的将领列表[num].第几个封地;
		int 第几个将领 = 要显示的将领列表[num].第几个将领;
		将领属性库类 将领属性库类 = 全局将领库.查询指定ID的将领数据(全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地].将领信息表[第几个将领].将领属性.初始属性.ID);
		if (将领属性库类 != null)
		{
			将领详情头像显示对象.transform.GetChild(0).GetComponent<Image>().sprite = 全局将领库.获取指定将领的头像(将领属性库类.名字);
			将领详情头像显示对象.transform.GetChild(0).GetChild(0).gameObject.SetActive(value: false);
			Animator component = 将领详情头像显示对象.transform.GetChild(0).GetChild(0).GetComponent<Animator>();
			component.SetInteger("特效类型", 0);
			if (将领属性库类.头像特效 != 0.0)
			{
				将领详情头像显示对象.transform.GetChild(0).GetChild(0).gameObject.SetActive(value: true);
				component.SetInteger("特效类型", (int)将领属性库类.头像特效);
			}
		}
		将领成长信息对象.transform.GetChild(5).GetComponent<Text>().text = 全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地].将领信息表[第几个将领].将领属性.初始属性.成长.ToString();
		将领成长信息对象.transform.GetChild(7).GetComponent<Text>().text = 全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地].将领信息表[第几个将领].将领属性.初始属性.突围.ToString();
		将领成长信息对象.transform.GetChild(9).GetComponent<Text>().text = 全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地].将领信息表[第几个将领].详细信息.忠诚.ToString();
		if (将领属性信息对象.gameObject.activeSelf)
		{
			将领属性信息对象.transform.GetChild(1).GetChild(3).GetComponent<Text>()
				.text = 全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地].封地名字;
			将领属性信息对象.transform.GetChild(1).GetChild(7).GetComponent<Text>()
				.text = 全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地].将领信息表[第几个将领].详细信息.经验.ToString() + "/" + 全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地].将领信息表[第几个将领].详细信息.升级需要经验.ToString();
			将领属性信息对象.transform.GetChild(1).GetChild(8).gameObject.SetActive(value: false);
			if (全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地].将领信息表[第几个将领].详细信息.经验 < 全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地].将领信息表[第几个将领].获取当前等级升级需要经验(99.0))
			{
				将领属性信息对象.transform.GetChild(1).GetChild(8).gameObject.SetActive(value: true);
			}
			RectTransform component2 = 将领属性信息对象.transform.GetChild(1).GetChild(6).gameObject.GetComponent<RectTransform>();
			double num2 = 全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地].将领信息表[第几个将领].详细信息.经验 / 全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地].将领信息表[第几个将领].详细信息.升级需要经验;
			component2.sizeDelta = new Vector2(170f * (float)num2, 14f);
			将领属性信息对象.transform.GetChild(1).GetChild(11).GetComponent<Text>()
				.text = 全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地].将领信息表[第几个将领].详细信息.剩余体力.ToString() + "/" + 全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地].将领信息表[第几个将领].将领属性.最终属性.体力上限.ToString();
			将领属性信息对象.transform.GetChild(1).GetChild(15).GetComponent<Text>()
				.text = 全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地].将领信息表[第几个将领].详细信息.俸禄.ToString();
			将领属性信息对象.transform.GetChild(2).GetChild(2).GetComponent<Text>()
				.text = 全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地].将领信息表[第几个将领].将领属性.最终属性.武力.ToString();
			GameObject gameObject = 将领属性信息对象.transform.GetChild(2).GetChild(3).gameObject;
			GameObject gameObject2 = 将领属性信息对象.transform.GetChild(2).GetChild(4).gameObject;
			if (全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地].将领信息表[第几个将领].将领属性.成长点数.总分配点数 > 0.0)
			{
				gameObject.SetActive(value: true);
				gameObject2.SetActive(value: true);
			}
			else
			{
				gameObject.SetActive(value: false);
				gameObject2.SetActive(value: false);
			}
			将领属性信息对象.transform.GetChild(2).GetChild(7).GetComponent<Text>()
				.text = 全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地].将领信息表[第几个将领].将领属性.最终属性.智力.ToString();
			GameObject gameObject3 = 将领属性信息对象.transform.GetChild(2).GetChild(8).gameObject;
			GameObject gameObject4 = 将领属性信息对象.transform.GetChild(2).GetChild(9).gameObject;
			if (全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地].将领信息表[第几个将领].将领属性.成长点数.总分配点数 > 0.0)
			{
				gameObject3.SetActive(value: true);
				gameObject4.SetActive(value: true);
			}
			else
			{
				gameObject3.SetActive(value: false);
				gameObject4.SetActive(value: false);
			}
			将领属性信息对象.transform.GetChild(2).GetChild(12).GetComponent<Text>()
				.text = 全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地].将领信息表[第几个将领].将领属性.最终属性.统帅.ToString();
			GameObject gameObject5 = 将领属性信息对象.transform.GetChild(2).GetChild(13).gameObject;
			GameObject gameObject6 = 将领属性信息对象.transform.GetChild(2).GetChild(14).gameObject;
			if (全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地].将领信息表[第几个将领].将领属性.成长点数.总分配点数 > 0.0)
			{
				gameObject5.SetActive(value: true);
				gameObject6.SetActive(value: true);
			}
			else
			{
				gameObject5.SetActive(value: false);
				gameObject6.SetActive(value: false);
			}
			将领属性信息对象.transform.GetChild(2).GetChild(17).GetComponent<Text>()
				.text = 全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地].将领信息表[第几个将领].将领属性.成长点数.总分配点数.ToString();
			将领属性信息对象.transform.GetChild(2).GetChild(21).GetComponent<Text>()
				.text = 全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地].将领信息表[第几个将领].将领属性.最终属性.攻击.ToString();
			将领属性信息对象.transform.GetChild(2).GetChild(24).GetComponent<Text>()
				.text = 全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地].将领信息表[第几个将领].将领属性.最终属性.防御.ToString();
			StartCoroutine(刷新统帅加成时间());
		}
		else if (将领装备信息对象.gameObject.activeSelf)
		{
			将领装备信息对象.transform.GetChild(6).GetComponent<Text>().text = 全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地].将领信息表[第几个将领].将领属性.最终属性.攻击.ToString();
			将领装备信息对象.transform.GetChild(8).GetComponent<Text>().text = 全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地].将领信息表[第几个将领].将领属性.最终属性.防御.ToString();
			将领装备信息对象.transform.GetChild(10).GetComponent<Text>().text = 全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地].将领信息表[第几个将领].将领属性.最终属性.统兵.ToString();
			Text component3 = 将领装备信息对象.transform.GetChild(11).GetChild(0).GetComponent<Text>();
			GameObject gameObject7 = 将领装备信息对象.transform.GetChild(12).gameObject;
			GameObject gameObject8 = 将领装备信息对象.transform.GetChild(13).gameObject;
			component3.color = 颜色类.GetColor("#78FFC8");
			component3.text = "请先换上装备";
			gameObject7.SetActive(value: true);
			gameObject8.SetActive(value: false);
			for (int i = 0; i < 4; i++)
			{
				Image component4 = 将领装备信息对象.transform.GetChild(1).GetChild(i).GetChild(0)
					.GetComponent<Image>();
				Image component5 = 将领装备信息对象.transform.GetChild(1).GetChild(i).GetChild(1)
					.GetComponent<Image>();
				将领装备信息对象.transform.GetChild(1).GetChild(i).GetChild(3)
					.gameObject.SetActive(value: false);
				component5.gameObject.SetActive(value: false);
				将领装备信息对象.transform.GetChild(1).GetChild(i).GetChild(0)
					.GetChild(0)
					.gameObject.SetActive(value: false);
				将领装备 将领装备 = 全局变量.所有玩家数据表[第几个玩家].背包装备列表.寻找指定将领的装备(i, 全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地].将领信息表[第几个将领].ID);
				if (将领装备 != null)
				{
					component4.sprite = 将领装备.获取装备头像();
					if (将领装备.品质 > 1.0)
					{
						component5.gameObject.SetActive(value: true);
						component5.sprite = 全局变量.装备品质图片资源表[(int)将领装备.品质];
					}
					if (将领装备.装备信息.名称.IndexOf("尊") > -1)
					{
						将领装备信息对象.transform.GetChild(1).GetChild(i).GetChild(0)
							.GetChild(0)
							.gameObject.SetActive(value: true);
					}
					if (将领装备信息对象.transform.GetChild(1).GetChild(i).GetChild(2)
						.gameObject.activeSelf)
					{
						gameObject7.SetActive(value: false);
						gameObject8.SetActive(value: true);
						component3.color = 将领装备.获取装备文字颜色();
						component3.text = 将领装备.获取装属性说明文本();
					}
					double 强化等级 = 将领装备.强化等级;
					if (强化等级 != 0.0)
					{
						将领装备信息对象.transform.GetChild(1).GetChild(i).GetChild(3)
							.gameObject.SetActive(value: true);
						将领装备信息对象.transform.GetChild(1).GetChild(i).GetChild(3)
							.GetComponent<Text>()
							.text = "+" + 强化等级.ToString();
					}
				}
				else
				{
					component4.sprite = 全局变量.装备初始图片[i];
					component5.gameObject.SetActive(value: false);
				}
			}
			将领装备信息对象.transform.GetChild(11).GetComponent<ScrollRect>().normalizedPosition = new Vector2(1f, 1f);
		}
		else if (将领配兵信息对象.gameObject.activeSelf)
		{
			将领配兵信息对象.transform.GetChild(0).GetChild(1).gameObject.SetActive(value: false);
			Image component6 = 将领配兵信息对象.transform.GetChild(0).GetChild(1).GetChild(0)
				.GetComponent<Image>();
			int num3 = 全局兵种库.查询指定ID的索引(全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地].将领信息表[第几个将领].将领配兵.ID);
			if (num3 != -1)
			{
				将领配兵信息对象.transform.GetChild(0).GetChild(1).gameObject.SetActive(value: true);
				int num4 = 全局兵种库.查询指定兵种的图片(全局兵种库.属性表[num3].名称);
				component6.sprite = 全局变量.所有兵种图片资源表[num4];
				将领配兵信息对象.transform.GetChild(0).GetChild(1).GetChild(1)
					.GetComponent<Text>()
					.text = 全局兵种库.属性表[num3].名称;
				将领配兵信息对象.transform.GetChild(0).GetChild(1).GetChild(2)
					.GetChild(1)
					.GetComponent<Text>()
					.text = 全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地].将领信息表[第几个将领].将领配兵.数量.ToString() + "/" + 全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地].将领信息表[第几个将领].将领属性.最终属性.统兵.ToString();
			}
			for (int j = 0; j < 全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地].闲兵信息表.Count; j++)
			{
				if (全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地].闲兵信息表[j].数量 < 1.0)
				{
					全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地].闲兵信息表.RemoveAt(j);
					break;
				}
			}
			int count = 全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地].闲兵信息表.Count;
			闲兵总页数 = Mathf.Ceil((float)count / 6f);
			int num5 = 0;
			int num6 = 0;
			num6 = 第几页闲兵 * 6;
			for (int k = 0; k < 6; k++)
			{
				int index = k;
				Transform child = 将领配兵信息对象.transform.GetChild(1).GetChild(2).GetChild(index)
					.GetChild(1);
				Transform child2 = 将领配兵信息对象.transform.GetChild(1).GetChild(2).GetChild(index)
					.GetChild(2);
				Transform child3 = 将领配兵信息对象.transform.GetChild(1).GetChild(2).GetChild(index)
					.GetChild(3);
				child.gameObject.SetActive(value: false);
				child2.gameObject.SetActive(value: false);
				child3.gameObject.SetActive(value: false);
				num5 = num6 + k;
				if (num5 < count)
				{
					double 要查询的兵种ID = 全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地].闲兵信息表[num5].ID;
					double 数量 = 全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地].闲兵信息表[num5].数量;
					int num7 = 全局兵种库.查询指定ID的索引(要查询的兵种ID);
					if (num7 != -1)
					{
						int num8 = 全局兵种库.查询指定兵种的图片(全局兵种库.属性表[num7].名称);
						child.gameObject.SetActive(value: true);
						child2.gameObject.SetActive(value: true);
						child3.gameObject.SetActive(value: true);
						child.GetComponent<Image>().sprite = 全局变量.所有兵种图片资源表[num8];
						child2.GetComponent<Text>().text = 全局兵种库.属性表[num7].名称;
						child3.GetComponent<Text>().text = 数量.ToString();
					}
				}
			}
			将领配兵信息对象.transform.GetChild(2).GetChild(2).GetChild(1)
				.GetComponent<Text>()
				.text = (第几页闲兵 + 1).ToString() + "/" + 闲兵总页数.ToString();
		}
		else if (将领培养信息对象.gameObject.activeSelf)
		{
			将领培养信息对象.transform.GetChild(2).GetChild(2).GetComponent<Text>()
				.text = 全局变量.所有玩家数据表[第几个玩家].背包道具列表.获取指定道具数量("将神魂").ToString();
			将领培养信息对象.transform.GetChild(3).GetChild(1).GetChild(1)
				.GetComponent<Text>()
				.text = 全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地].将领信息表[第几个将领].将领培养.保底次数.ToString() + "/" + 全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地].将领信息表[第几个将领].将领培养.保底上限.ToString();
			将领培养信息对象.transform.GetChild(4).GetChild(3).GetChild(1)
				.GetComponent<Text>()
				.text = 获取培养次数(全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地].将领信息表[第几个将领]).ToString();
		}
	}

	public void 显示要强化的装备()
	{
		int index = 获取选中将领索引();
		int 第几个封地 = 要显示的将领列表[index].第几个封地;
		int 第几个将领 = 要显示的将领列表[index].第几个将领;
		int num = 0;
		将领装备 将领装备;
		while (true)
		{
			if (num >= 4)
			{
				return;
			}
			if (将领装备信息对象.transform.GetChild(1).GetChild(num).GetChild(2)
				.gameObject.activeSelf)
			{
				将领装备 = 全局变量.所有玩家数据表[第几个玩家].背包装备列表.寻找指定将领的装备(num, 全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地].将领信息表[第几个将领].ID);
				if (将领装备 != null)
				{
					break;
				}
			}
			num++;
		}
		强化脚本对象.装备对象 = 将领装备;
		强化脚本对象.显示指定装备();
	}

	public void 显示要炼魂的装备()
	{
		int index = 获取选中将领索引();
		int 第几个封地 = 要显示的将领列表[index].第几个封地;
		int 第几个将领 = 要显示的将领列表[index].第几个将领;
		int num = 0;
		将领装备 将领装备;
		while (true)
		{
			if (num >= 4)
			{
				return;
			}
			if (将领装备信息对象.transform.GetChild(1).GetChild(num).GetChild(2)
				.gameObject.activeSelf)
			{
				将领装备 = 全局变量.所有玩家数据表[第几个玩家].背包装备列表.寻找指定将领的装备(num, 全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地].将领信息表[第几个将领].ID);
				if (将领装备 != null)
				{
					break;
				}
			}
			num++;
		}
		炼魂脚本对象.装备对象 = 将领装备;
		炼魂脚本对象.显示装备所有信息();
	}

	public void 全部穿戴装备()
	{
		执行将领操作("generals.equipBest", new JObject());
	}

	public void 全部卸载装备()
	{
		执行将领操作("generals.unequipAll", new JObject());
	}

	public void 将领穿戴装备()
	{
		int index = 获取选中将领索引();
		int 第几个封地 = 要显示的将领列表[index].第几个封地;
		int 第几个将领 = 要显示的将领列表[index].第几个将领;
		int num = 0;
		while (true)
		{
			if (num < 4)
			{
				if (将领装备信息对象.transform.GetChild(1).GetChild(num).GetChild(2)
					.gameObject.activeSelf)
				{
					break;
				}
				num++;
				continue;
			}
			return;
		}
		更换装备脚本对象.要显示的装备列表 = 全局变量.所有玩家数据表[第几个玩家].背包装备列表.获取指定部位列表(num);
		更换装备脚本对象.第几个玩家 = 第几个玩家;
		更换装备脚本对象.第几个封地 = 第几个封地;
		更换装备脚本对象.第几个将领 = 第几个将领;
		更换装备脚本对象.第几个部位 = num;
		更换装备脚本对象.刷新显示();
	}

	public void 将领卸载选中装备()
	{
		for (int slot = 0; slot < 4; slot++)
		{
			if (将领装备信息对象.transform.GetChild(1).GetChild(slot).GetChild(2).gameObject.activeSelf)
			{
				执行将领操作("generals.unequip", new JObject { ["equipmentSlot"] = slot });
				return;
			}
		}
	}

	private bool 删除指定将领()
	{
		int num = 获取选中将领索引();
		if (num < 0 || num >= 要显示的将领列表.Count || 第几个玩家 < 0 || 第几个玩家 >= 全局变量.所有玩家数据表.Count) return false;
		int fief = 要显示的将领列表[num].第几个封地;
		int general = 要显示的将领列表[num].第几个将领;
		玩家数据 player = 全局变量.所有玩家数据表[第几个玩家];
		if (fief < 0 || fief >= player.封地信息表.Count || general < 0 || general >= player.封地信息表[fief].将领信息表.Count) return false;
		return GeneralsClientAdapter.DismissLegacy(第几个玩家, player.封地信息表[fief].将领信息表[general].ID);
	}

	public void 将领数扩容()
	{
		int 本机身份 = 全局变量.本机身份;
		if (全局变量.所有玩家数据表[本机身份].财产信息.黄金 > 1000000.0)
		{
			全局变量.所有玩家数据表[本机身份].财产信息.黄金 = 全局变量.所有玩家数据表[本机身份].财产信息.黄金 - 1000000.0;
			全局变量.所有玩家数据表[本机身份].将领数扩容();
			全局变量.提示类.显示信息("扩容成功,将位+1");
			获取要显示的将领列表();
		}
		else
		{
			全局变量.提示类.显示信息("黄金不足,需要100w黄金!");
		}
	}

	public void 使用统帅加成道具()
	{
		int num = 获取选中将领索引();
		if (num != -1)
		{
			使用道具脚本对象.第几个封地 = 要显示的将领列表[num].第几个封地;
			使用道具脚本对象.第几个将领 = 要显示的将领列表[num].第几个将领;
			使用道具脚本对象.gameObject.SetActive(value: true);
			使用道具脚本对象.要显示的列表 = 全局道具库.获取指定类型的道具列表("统帅道具");
			使用道具脚本对象.刷新显示();
		}
	}

	public void 将领打开修改名字界面()
	{
		int num = 获取选中将领索引();
		if (num != -1)
		{
			int 第几个封地 = 要显示的将领列表[num].第几个封地;
			int 第几个将领 = 要显示的将领列表[num].第几个将领;
			if (全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地].将领信息表[第几个将领].将领属性.初始属性.系列 != "名将")
			{
				将领改名脚本对象.原将领名字.text = 全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地].将领信息表[第几个将领].将领属性.初始属性.名字;
				将领改名脚本对象.gameObject.SetActive(value: true);
			}
			else
			{
				全局变量.提示类.显示信息("名将不可改名!");
			}
		}
	}

	public void 将领修改名字(string 要修改的名字)
	{
		int num = 获取选中将领索引();
		if (num != -1)
		{
			int 第几个封地 = 要显示的将领列表[num].第几个封地;
			int 第几个将领 = 要显示的将领列表[num].第几个将领;
			if (全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地].将领信息表[第几个将领].将领属性.初始属性.系列 != "名将")
			{
				全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地].将领信息表[第几个将领].将领属性.初始属性.名字 = 要修改的名字;
				刷新列表信息();
				刷新将领属性信息();
			}
		}
	}

	public void 将领使用经验书()
	{
		int num = 获取选中将领索引();
		if (num != -1)
		{
			使用道具脚本对象.第几个封地 = 要显示的将领列表[num].第几个封地;
			使用道具脚本对象.第几个将领 = 要显示的将领列表[num].第几个将领;
			使用道具脚本对象.要显示的列表 = 全局道具库.获取指定类型的道具列表("经验书");
			使用道具脚本对象.刷新显示();
		}
	}

	private void 将领获得经验计算(double 经验值)
	{
		int num = 获取选中将领索引();
		if (num != -1)
		{
			int 第几个封地 = 要显示的将领列表[num].第几个封地;
			int 第几个将领 = 要显示的将领列表[num].第几个将领;
			全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地].将领信息表[第几个将领].将领获取经验值(经验值);
		}
	}

	private void 将领清空分配加点()
	{
		执行将领操作("generals.resetPoints", new JObject());
	}

	private void 将领分配加点(string 加点类型, int 加点方式)
	{
		JObject arguments = new JObject { ["attribute"] = 加点类型 };
		if (加点方式 != 1) arguments["count"] = 1;
		执行将领操作("generals.allocatePoints", arguments);
	}

	public void 将领增加忠诚()
	{
		int num = 获取选中将领索引();
		if (num == -1)
		{
			return;
		}
		int 第几个封地 = 要显示的将领列表[num].第几个封地;
		int 第几个将领 = 要显示的将领列表[num].第几个将领;
		if (全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地].将领信息表[第几个将领].详细信息.忠诚 < 99)
		{
			int 当前忠诚 = (int)全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地].将领信息表[第几个将领].详细信息.忠诚;
			int 需要增加的忠诚 = 99 - 当前忠诚;
			if (需要增加的忠诚 > 0)
			{
				if (全局变量.所有玩家数据表[第几个玩家].财产信息.黄金 >= 需要增加的忠诚 * 100)
				{
					全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地].将领信息表[第几个将领].详细信息.忠诚 = 99;
					刷新列表信息();
					刷新将领属性信息();
					全局变量.所有玩家数据表[第几个玩家].财产信息.黄金 -= 需要增加的忠诚 * 100;
					全局变量.提示类.显示信息($"增加忠诚{需要增加的忠诚}共消耗了{需要增加的忠诚 * 100}黄金");
				}
				else
				{
					全局变量.提示类.显示信息("黄金不足");
				}
			}

		}

	}

	private void 执行将领操作(string type, JObject arguments)
	{
		int index = 获取选中将领索引();
		if (index < 0 || index >= 要显示的将领列表.Count || 第几个玩家 < 0 || 第几个玩家 >= 全局变量.所有玩家数据表.Count) return;
		玩家数据 player = 全局变量.所有玩家数据表[第几个玩家];
		int fief = 要显示的将领列表[index].第几个封地;
		int general = 要显示的将领列表[index].第几个将领;
		if (fief < 0 || fief >= player.封地信息表.Count || general < 0 || general >= player.封地信息表[fief].将领信息表.Count) return;
		GeneralsClientAdapter.Execute(第几个玩家, player.封地信息表[fief].将领信息表[general].ID, type, arguments, () =>
		{
			if (type == "generals.dismiss") 重置刷新将领列表();
			else
			{
				刷新将领属性信息();
				刷新列表信息();
			}
		});
	}
	private void 将领补满配兵()
	{
		执行将领操作("generals.refillTroops", new JObject());
	}

	private void 将领指定配兵(int 点击第几个)
	{
		int index = 获取选中将领索引();
		if (index < 0 || index >= 要显示的将领列表.Count) return;
		int fief = 要显示的将领列表[index].第几个封地;
		if (第几个玩家 < 0 || 第几个玩家 >= 全局变量.所有玩家数据表.Count || fief < 0 || fief >= 全局变量.所有玩家数据表[第几个玩家].封地信息表.Count) return;
		List<闲兵信息> pool = 全局变量.所有玩家数据表[第几个玩家].封地信息表[fief].闲兵信息表;
		int troopIndex = 第几页闲兵 * 6 + 点击第几个;
		if (troopIndex < 0 || troopIndex >= pool.Count) return;
		执行将领操作("generals.allocateTroops", new JObject { ["troopTypeId"] = pool[troopIndex].ID });
	}

	private void 将领解除配兵()
	{
		执行将领操作("generals.releaseTroops", new JObject());
	}

	private 将领信息 获取选中培养将领()
	{
		int index = 获取选中将领索引();
		if (index < 0 || index >= 要显示的将领列表.Count || 第几个玩家 < 0 || 第几个玩家 >= 全局变量.所有玩家数据表.Count) return null;
		玩家数据 player = 全局变量.所有玩家数据表[第几个玩家];
		int fief = 要显示的将领列表[index].第几个封地;
		int general = 要显示的将领列表[index].第几个将领;
		if (fief < 0 || fief >= player.封地信息表.Count || general < 0 || general >= player.封地信息表[fief].将领信息表.Count) return null;
		return player.封地信息表[fief].将领信息表[general];
	}

	private double 获取培养次数(将领信息 general)
	{
		if (!GameNetwork.Enabled) return general.将领培养.培养次数;
		if (!ReferenceEquals(培养选择将领, general))
		{
			培养选择将领 = general;
			培养选择次数 = general.将领培养.培养次数;
		}
		return 培养选择次数;
	}

	private void 将领加减培养次数(int 加减类型)
	{
		将领信息 general = 获取选中培养将领();
		if (general == null) return;
		double count = 获取培养次数(general) + (加减类型 == 0 ? 1 : -1);
		if (count < 1 || count > 100 || (加减类型 == 0 && count > 全局变量.所有玩家数据表[第几个玩家].背包道具列表.获取指定道具数量("将神魂"))) return;
		if (GameNetwork.Enabled) 培养选择次数 = count;
		else general.将领培养.培养次数 = count;
	}

	private void 将领培养计算()
	{
		将领信息 general = 获取选中培养将领();
		if (general == null) return;
		执行将领操作("generals.cultivate", new JObject { ["count"] = 获取培养次数(general) });
	}

	private IEnumerator 刷新统帅加成时间()
	{
		while (将领属性信息对象.gameObject.activeSelf)
		{
			int num = 获取选中将领索引();
			if (num == -1)
			{
				break;
			}
			int 第几个封地 = 要显示的将领列表[num].第几个封地;
			int 第几个将领 = 要显示的将领列表[num].第几个将领;
			将领属性信息对象.transform.GetChild(2).GetChild(27).GetComponent<Text>()
				.text = 全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地].将领信息表[第几个将领].将领属性.最终属性.统兵.ToString();
			将领属性信息对象.transform.GetChild(4).gameObject.SetActive(value: false);
			long num2 = 全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地].将领信息表[第几个将领].将领属性.最终属性.获取统帅加成剩余时间();
			if (num2 > 0)
			{
				将领属性信息对象.transform.GetChild(4).gameObject.SetActive(value: true);
				将领属性信息对象.transform.GetChild(4).GetChild(1).GetComponent<Text>()
					.text = TIME.ToTimeFormat(num2);
			}
			yield return null;
		}
	}

	private void 列表页数更新显示()
	{
		页数显示.text = (第几页将领 + 1).ToString() + "/" + 总页数.ToString();
	}

	public void 将领排序()
	{

		int num = 全局变量.所有玩家数据表[全局变量.本机身份].封地信息表[0].将领信息表.Count;
		if (num > 0)
		{
			for (int i = 0; i < num; i++)
			{
				for (int j = 0; j < num - i - 1; j++)
				{
					将领信息 将领1 = 全局变量.所有玩家数据表[全局变量.本机身份].封地信息表[0].将领信息表[j];
					将领信息 将领2 = 全局变量.所有玩家数据表[全局变量.本机身份].封地信息表[0].将领信息表[j + 1];
					if (将领1.将领属性.初始属性.成长 < 将领2.将领属性.初始属性.成长)
					{
						将领信息 temp = 全局变量.所有玩家数据表[全局变量.本机身份].封地信息表[0].将领信息表[j];
						全局变量.所有玩家数据表[全局变量.本机身份].封地信息表[0].将领信息表[j] = 全局变量.所有玩家数据表[全局变量.本机身份].封地信息表[0].将领信息表[j + 1];
						全局变量.所有玩家数据表[全局变量.本机身份].封地信息表[0].将领信息表[j + 1] = temp;
					}
					else if (将领1.将领属性.初始属性.成长 == 将领2.将领属性.初始属性.成长)
					{
						if (将领1.将领属性.初始属性.突围 < 将领2.将领属性.初始属性.突围)
						{
							将领信息 temp = 全局变量.所有玩家数据表[全局变量.本机身份].封地信息表[0].将领信息表[j];
							全局变量.所有玩家数据表[全局变量.本机身份].封地信息表[0].将领信息表[j] = 全局变量.所有玩家数据表[全局变量.本机身份].封地信息表[0].将领信息表[j + 1];
							全局变量.所有玩家数据表[全局变量.本机身份].封地信息表[0].将领信息表[j + 1] = temp;
						}
					}
				}
			}
		}



		//if (全局变量.所有玩家数据表[全局变量.本机身份].封地信息表[0].将领信息表.Count > 0)
		//{
		//	全局变量.所有玩家数据表[全局变量.本机身份].封地信息表[0].将领信息表.Sort((将领信息 将领1, 将领信息 将领2) =>
		//	{
		//		if (将领1.将领属性.初始属性.成长 < 将领2.将领属性.初始属性.成长)
		//			return 1;
		//		else if (将领1.将领属性.初始属性.成长 == 将领2.将领属性.初始属性.成长)
		//			return 将领1.将领属性.初始属性.突围 < 将领2.将领属性.初始属性.突围 ? 1 : -1;
		//		return -1;
		//	});
		//}
	}
}
