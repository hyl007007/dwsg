using UnityEngine;
using UnityEngine.UI;

public class 附近山贼界面脚本 : MonoBehaviour
{
	public int 起始坐标x = 10;

	public int 起始坐标y = 10;

	public int 显示数量 = 5;

	public int 已选中坐标x = 10;

	public int 已选中坐标y = 10;

	public GameObject 山贼列表对象;

	public GameObject 山贼信息对象;

	public GameObject 选择出征将领界面对象;

	private void Start()
	{
	}

	public void 刷新山贼列表()
	{
		显示山贼列表到UI上();
		显示选中山贼详情();
	}

	public void 刷新显示选中山贼()
	{
		显示选中山贼详情();
	}

	public void 往上翻页()
	{
		int num = 起始坐标y - 5;
		if (num > -1)
		{
			起始坐标y = num;
		}
		刷新山贼列表();
	}

	public void 往下翻页()
	{
		int num = 起始坐标y + 5;
		if (num < 全局变量.竖向山贼数量)
		{
			起始坐标y = num;
		}
		刷新山贼列表();
	}

	public void 往左翻页()
	{
		int num = 起始坐标x - 1;
		if (num > -1)
		{
			起始坐标x = num;
		}
		刷新山贼列表();
	}

	public void 往右翻页()
	{
		int num = 起始坐标x + 1;
		if (num < 全局变量.横向山贼数量)
		{
			起始坐标x = num;
		}
		刷新山贼列表();
	}

	public void 出征剿灭选中山贼()
	{
		选择出征将领 component = 选择出征将领界面对象.GetComponent<选择出征将领>();
		component.山贼坐标x = 已选中坐标x;
		component.山贼坐标y = 已选中坐标y;
		选择出征将领界面对象.SetActive(value: true);
		component.刷新山贼();
	}

	private void 显示山贼列表到UI上()
	{
		int x = 起始坐标x;
		int num = 起始坐标y;
		for (int i = 0; i < 显示数量; i++)
		{
			山贼列表对象.transform.GetChild(i).gameObject.SetActive(value: false);
		}
		UnityEngine.Debug.Log("山贼数量:" + 全局变量.所有山贼数据列表.Count.ToString());
		int num2 = 0;
		for (int j = 0; j < 显示数量; j++)
		{
			num = 起始坐标y + j;
			山贼属性信息 山贼属性信息 = 附近山贼.获取指定坐标的山贼(x, num);
			if (山贼属性信息 != null)
			{
				山贼列表对象.transform.GetChild(num2).gameObject.SetActive(value: true);
				Image component = 山贼列表对象.transform.GetChild(num2).GetChild(1).GetComponent<Image>();
				int num3 = (int)山贼属性信息.等级 - 1;
				component.sprite = 全局变量.山贼头像资源表[num3];
				山贼列表对象.transform.GetChild(num2).GetChild(2).GetComponent<Text>()
					.text = 山贼属性信息.等级.ToString() + "级山贼 (" + x.ToString() + "," + num.ToString() + ")";
					int count = 山贼属性信息.将领数据列表.Count;
					double num4 = 0.0;
					for (int k = 0; k < count; k++)
					{
						double iD = 山贼属性信息.将领数据列表[k].将领配兵.ID;
						double 数量 = 山贼属性信息.将领数据列表[k].将领配兵.数量;
						num4 += 数量;
					}
					山贼列表对象.transform.GetChild(num2).GetChild(3).GetComponent<Text>()
						.text = "兵力:" + num4.ToString();
						string text = "资源";
						if (山贼属性信息.掉落宝物 == 1.0)
						{
							text += "、宝物";
						}
						if (山贼属性信息.掉落宝箱 == 1.0)
						{
							text += "、宝箱";
						}
						if (山贼属性信息.掉落装备 == 1.0)
						{
							text += "、装备";
						}
						山贼列表对象.transform.GetChild(num2).GetChild(4).GetComponent<Text>()
							.text = text;
							num2++;
						}
					}
				}

				private void 显示选中山贼详情()
				{
					int x = 起始坐标x;
					int num = 起始坐标y;
					for (int i = 0; i < 显示数量; i++)
					{
						num = 起始坐标y + i;
						if (!山贼列表对象.transform.GetChild(i).GetChild(5).gameObject.activeSelf)
						{
							continue;
						}
						山贼属性信息 山贼属性信息 = 附近山贼.获取指定坐标的山贼(x, num);
						if (山贼属性信息 == null)
						{
							continue;
						}
						山贼信息对象.transform.gameObject.SetActive(value: true);
						已选中坐标x = x;
						已选中坐标y = num;
						double 等级 = 山贼属性信息.等级;
						Image component = 山贼信息对象.transform.GetChild(0).GetChild(0).GetComponent<Image>();
						int num2 = (int)等级 - 1;
						component.sprite = 全局变量.山贼头像资源表[num2];
						山贼信息对象.transform.GetChild(0).GetChild(1).GetComponent<Text>()
							.text = 等级.ToString() + "级山贼 (" + x.ToString() + "," + num.ToString() + ")";
							int count = 山贼属性信息.将领数据列表.Count;
							string text = "";
							double num3 = 0.0;
							for (int j = 0; j < count; j++)
							{
								double iD = 山贼属性信息.将领数据列表[j].将领配兵.ID;
								double 数量 = 山贼属性信息.将领数据列表[j].将领配兵.数量;
								num3 += 数量;
								int num4 = 全局兵种库.查询指定ID的索引(iD);
								text = ((num4 == -1) ? (text + "未知 " + 数量.ToString() + "\n") : (text + 全局兵种库.属性表[num4].名称 + " " + 数量.ToString() + "\n"));
								山贼信息对象.transform.GetChild(0).GetChild(2).GetComponent<Text>()
									.text = "兵力:" + num3.ToString();
									string text2 = "资源";
									if (山贼属性信息.掉落宝物 == 1.0)
									{
										text2 += "、宝物";
									}
									if (山贼属性信息.掉落宝箱 == 1.0)
									{
										text2 += "、宝箱";
									}
									if (山贼属性信息.掉落装备 == 1.0)
									{
										text2 += "、装备";
									}
									山贼信息对象.transform.GetChild(0).GetChild(3).GetComponent<Text>()
										.text = text2;
										山贼信息对象.transform.GetChild(2).GetChild(0).GetComponent<Text>()
											.text = "敌军等级:" + 山贼属性信息.将领数据列表[j].将领属性.成长点数.等级.ToString() + "级";
											山贼信息对象.transform.GetChild(2).GetChild(1).GetComponent<Text>()
												.text = "敌军规模:" + count.ToString() + "名";
												山贼信息对象.transform.GetChild(2).GetChild(3).GetChild(0)
													.GetComponent<Text>()
													.text = text;
												}
											}
										}
									}
