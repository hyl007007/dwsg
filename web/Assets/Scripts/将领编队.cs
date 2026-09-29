using Dwsg.Generals;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using 玩家数据结构;

public class 将领编队 : MonoBehaviour
{
	private int 第几个玩家 = 全局变量.本机身份;

	public int 显示第几个封地 = -1;

	public List<将领索引信息> 要显示的将领列表 = new List<将领索引信息>();

	private int 第几页将领;

	private float 总页数;

	private int 选中列表第几个将领;

	public Text 选择的封地显示对象;

	public GameObject 将领列表对象;

	public GameObject 编队列表对象;

	public Text 页数显示对象;

	public void 重置刷新将领列表()
	{
		获取要显示的将领列表();
		显示将领列表();
		显示编队列表();
	}

	public void 默认显示全部封地将领()
	{
		显示第几个封地 = -1;
	}

	public void 获取要显示的将领列表()
	{
		第几个玩家 = 全局变量.本机身份;
		int 本机身份 = 全局变量.本机身份;
		int num = 显示第几个封地;
		要显示的将领列表.Clear();
		选中列表第几个将领 = 0;
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
		第几页将领 = 0;
		总页数 = Mathf.Ceil((float)count4 / 5f);
		页数显示对象.text = (第几页将领 + 1).ToString() + "/" + 总页数.ToString();
	}

	public void 列表左翻页()
	{
		if (第几页将领 != 0)
		{
			第几页将领--;
			刷新编队();
		}
	}

	public void 列表右翻页()
	{
		if ((float)第几页将领 < 总页数 - 1f)
		{
			第几页将领++;
			刷新编队();
		}
	}

	public void 编队列表读写()
	{
		编队列表操作();
	}

	public void 刷新编队()
	{
		显示将领列表();
		显示编队列表();
	}

	public void 点击列表将领(int 点击第几个)
	{
		列表将领加入编队(点击第几个);
	}

	public void 点击加入选中将领到编队()
	{
		int num = 0;
		while (true)
		{
			if (num < 4)
			{
				if (将领列表对象.transform.GetChild(num).GetComponent<Toggle>().isOn)
				{
					break;
				}
				num++;
				continue;
			}
			return;
		}
		UnityEngine.Debug.Log("将领" + num.ToString() + "被单击");
		列表将领加入编队(num);
		刷新编队();
	}

	private void 显示将领列表()
	{
		int count = 要显示的将领列表.Count;
		总页数 = Mathf.Ceil((float)count / 4f);
		页数显示对象.text = (第几页将领 + 1).ToString() + "/" + 总页数.ToString();
		int num = 0;
		int num2 = 0;
		num2 = 第几页将领 * 4;
		Animator animator = null;
		Image image = null;
		double num3 = 0.0;
		string text = "";
		string text2 = "";
		for (int i = 0; i < 4; i++)
		{
			将领列表对象.transform.GetChild(i).gameObject.SetActive(value: false);
			num = num2 + i;
			if (num >= count)
			{
				continue;
			}
			将领列表对象.transform.GetChild(i).gameObject.SetActive(value: true);
			int 第几个封地 = 要显示的将领列表[num].第几个封地;
			int 第几个将领 = 要显示的将领列表[num].第几个将领;
			将领属性库类 将领属性库类 = 全局将领库.查询指定ID的将领数据(全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地].将领信息表[第几个将领].将领属性.初始属性.ID);
			if (将领属性库类 != null)
			{
				将领列表对象.transform.GetChild(i).GetChild(0).GetChild(0)
					.GetComponent<Image>()
					.sprite = 全局将领库.获取指定将领的头像(将领属性库类.名字);
					将领列表对象.transform.GetChild(i).GetChild(0).GetChild(1)
						.gameObject.SetActive(value: false);
						animator = 将领列表对象.transform.GetChild(i).GetChild(0).GetChild(1)
							.GetComponent<Animator>();
						animator.SetInteger("特效类型", 0);
						if (将领属性库类.头像特效 != 0.0)
						{
							将领列表对象.transform.GetChild(i).GetChild(0).GetChild(1)
								.gameObject.SetActive(value: true);
								animator.SetInteger("特效类型", (int)将领属性库类.头像特效);
							}
						}
						image = 将领列表对象.transform.GetChild(i).GetChild(1).GetComponent<Image>();
						将领列表对象.transform.GetChild(i).GetChild(1).gameObject.SetActive(value: false);
						if (全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地].将领信息表[第几个将领].详细信息.编队 != 0.0)
						{
							将领列表对象.transform.GetChild(i).GetChild(1).gameObject.SetActive(value: true);
							num3 = 全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地].将领信息表[第几个将领].详细信息.编队 - 1.0;
							image.sprite = 全局变量.将领编队图标资源表[(int)num3];
						}
						Text component = 将领列表对象.transform.GetChild(i).GetChild(2).GetComponent<Text>();
						component.text = 全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地].将领信息表[第几个将领].将领属性.初始属性.名字;
						component.color = 全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地].将领信息表[第几个将领].将领属性.初始属性.获取将领名字颜色();
						Text component2 = 将领列表对象.transform.GetChild(i).GetChild(3).GetComponent<Text>();
						text = 全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地].将领信息表[第几个将领].将领属性.成长点数.等级.ToString();
						text2 = 全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地].将领信息表[第几个将领].将领属性.初始属性.获取职业名字();
						component2.text = "(" + text + "级" + text2 + ")";
					}
				}

				private void 显示编队列表()
				{
					for (int i = 0; i < 5; i++)
					{
						for (int j = 0; j < 5; j++)
						{
							编队列表对象.transform.GetChild(i).GetChild(1).GetChild(j)
								.GetChild(1)
								.gameObject.SetActive(value: false);
								编队列表对象.transform.GetChild(i).GetChild(1).GetChild(j)
									.GetChild(2)
									.gameObject.SetActive(value: false);
									int iD标识 = 全局变量.所有玩家数据表[第几个玩家].编队信息表[i][j];
									返回将领索引 返回将领索引 = 全局变量.所有玩家数据表[第几个玩家].获取指定ID标识的将领索引(iD标识);
									if (返回将领索引.第几个封地 != -1)
									{
										double 等级 = 全局变量.所有玩家数据表[第几个玩家].封地信息表[返回将领索引.第几个封地].将领信息表[返回将领索引.第几个将领].将领属性.成长点数.等级;
										string text = 全局变量.所有玩家数据表[第几个玩家].封地信息表[返回将领索引.第几个封地].将领信息表[返回将领索引.第几个将领].将领属性.初始属性.获取职业名字();
										编队列表对象.transform.GetChild(i).GetChild(1).GetChild(j)
											.GetChild(1)
											.gameObject.SetActive(value: true);
											编队列表对象.transform.GetChild(i).GetChild(1).GetChild(j)
												.GetChild(2)
												.gameObject.SetActive(value: true);
												Text component = 编队列表对象.transform.GetChild(i).GetChild(1).GetChild(j)
													.GetChild(1)
													.GetComponent<Text>();
												Text component2 = 编队列表对象.transform.GetChild(i).GetChild(1).GetChild(j)
													.GetChild(2)
													.GetComponent<Text>();
												component.text = 全局变量.所有玩家数据表[第几个玩家].封地信息表[返回将领索引.第几个封地].将领信息表[返回将领索引.第几个将领].将领属性.初始属性.名字;
												component.color = 全局变量.所有玩家数据表[第几个玩家].封地信息表[返回将领索引.第几个封地].将领信息表[返回将领索引.第几个将领].将领属性.初始属性.获取将领名字颜色();
												component2.text = "(" + 等级.ToString() + "级" + text + ")";
											}
										}
									}
								}

	private void 列表将领加入编队(int 列表第几个将领)
	{
		选中列表第几个将领 = 第几页将领 * 4 + 列表第几个将领;
		if (选中列表第几个将领 < 0 || 选中列表第几个将领 >= 要显示的将领列表.Count) return;
		for (int team = 0; team < 5; team++)
		{
			if (!编队列表对象.transform.GetChild(team).GetChild(2).gameObject.activeSelf) continue;
			for (int slot = 0; slot < 5; slot++)
			{
				if (全局变量.所有玩家数据表[第几个玩家].编队信息表[team][slot] != -1) continue;
				写入编队(team, slot, false);
				return;
			}
			全局变量.提示类.显示信息("编队已满");
			return;
		}
	}

	private void 编队列表操作()
	{
		for (int team = 0; team < 5; team++)
			for (int slot = 0; slot < 5; slot++)
			{
				if (!编队列表对象.transform.GetChild(team).GetChild(1).GetChild(slot).GetChild(3).gameObject.activeSelf) continue;
				写入编队(team, slot, 全局变量.所有玩家数据表[第几个玩家].编队信息表[team][slot] != -1);
				return;
			}
	}
	private void 写入编队(int team, int slot, bool remove)
	{
		if (第几个玩家 < 0 || 第几个玩家 >= 全局变量.所有玩家数据表.Count) return;
		int? generalId = null;
		if (!remove)
		{
			if (选中列表第几个将领 < 0 || 选中列表第几个将领 >= 要显示的将领列表.Count) return;
			将领索引信息 index = 要显示的将领列表[选中列表第几个将领];
			玩家数据 player = 全局变量.所有玩家数据表[第几个玩家];
			if (index.第几个封地 < 0 || index.第几个封地 >= player.封地信息表.Count || index.第几个将领 < 0 || index.第几个将领 >= player.封地信息表[index.第几个封地].将领信息表.Count) return;
			generalId = player.封地信息表[index.第几个封地].将领信息表[index.第几个将领].ID;
		}
		GeneralsClientAdapter.Execute(第几个玩家, generalId, "generals.setFormation", new JObject { ["teamIndex"] = team, ["slotIndex"] = slot }, 刷新编队);
	}
}
