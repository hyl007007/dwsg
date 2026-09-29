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
		Dwsg.Network.GameNetwork.SnapshotApplied += 接收酒馆状态;
		自动刷新招募将领();
	}

	private void OnEnable()
	{
		if (军事缺口入口.当前玩家() != null) 自动刷新招募将领();
	}

	private void OnDestroy()
	{
		Dwsg.Network.GameNetwork.SnapshotApplied -= 接收酒馆状态;
	}

	private void 接收酒馆状态(Dwsg.Shared.WorldSnapshot snapshot)
	{
		if (!Dwsg.Network.GameNetwork.Enabled || snapshot.PrivatePlayer["tavern"] == null) return;
		Dwsg.Generals.TavernClientAdapter.Apply(将领列表);
		显示将领列表();
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
        if (Dwsg.Network.GameNetwork.Enabled) { 随机5个将领(0); return; }
		if (军事缺口入口.当前玩家() == null) return;
		if (!已初始化 || TIME.getTime() - 全局变量.酒馆刷新时间 >= 3600) 随机5个将领(0);
		显示将领列表();
		显示刷新时间();
	}

	public void 招贤令刷新招募将领()
	{
		随机5个将领(1);
	}

	public void 金榜刷新招募将领()
	{
		随机5个将领(2);
	}

	public void 皇榜刷新招募将领()
	{
		随机5个将领(3);
	}

	public void 招募选中将领()
	{
		添加将领到将领列表();
	}

	public void 添加将领到将领列表()
	{
		for (int index = 0; index < 5 && index < 将领列表.Count; index++)
		{
			if (将领列表对象.transform.GetChild(index).gameObject.activeSelf && 将领列表对象.transform.GetChild(index).GetChild(12).gameObject.activeSelf)
			{
				Dwsg.Generals.TavernClientAdapter.Recruit(index, 将领列表, 显示将领列表);
				return;
			}
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
        Dwsg.Generals.TavernClientAdapter.Refresh(随机类型, 将领列表, () => { 已初始化 = true; 显示将领列表(); 显示刷新时间(); });
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
