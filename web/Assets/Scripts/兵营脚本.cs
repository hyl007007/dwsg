using UnityEngine;
using UnityEngine.UI;

public class 兵营脚本 : MonoBehaviour
{
	public GameObject 标题列表对象;

	public Image 头像对象;

	public Text 名字等级对象;

	public GameObject 兵种列表对象;

	public 调整数量脚本 调整招募数量脚本对象;

	public int 第几个玩家;

	public int 第几个封地;

	public int 第几个建筑;

	public int 兵营类型 = 4;

	public void 刷新显示()
	{
		标题列表对象.transform.GetChild(兵营类型 - 4).GetComponent<Toggle>().isOn = true;
		int num = 全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地].建筑信息表[第几个建筑].获取建筑头像索引();
		头像对象.sprite = 全局变量.书院头像资源表[num];
		名字等级对象.text = 全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地].建筑信息表[第几个建筑].获取建筑等级文本();
		for (int i = 0; i < 4; i++)
		{
			int num2 = (兵营类型 - 3) * 100 + (i + 1);
			兵种属性库类 兵种属性库类 = 全局兵种库.查询指定ID的数据(num2);
			if (兵种属性库类 != null)
			{
				int num3 = 全局兵种库.查询指定兵种的图片(兵种属性库类.名称);
				兵种列表对象.transform.GetChild(i).GetChild(1).GetComponent<Image>()
					.sprite = 全局变量.所有兵种图片资源表[num3];
					double num4 = 全局变量.所有玩家数据表[第几个玩家].获取指定兵种ID总数(num2);
					兵种列表对象.transform.GetChild(i).GetChild(2).GetComponent<Text>()
						.text = 兵种属性库类.名称;
						GameObject gameObject = 兵种列表对象.transform.GetChild(i).GetChild(2).GetChild(0)
							.gameObject;
							GameObject gameObject2 = 兵种列表对象.transform.GetChild(i).GetChild(2).GetChild(1)
								.gameObject;
								gameObject2.SetActive(value: false);
								gameObject.SetActive(value: false);
								if (全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地].建筑信息表[第几个建筑].等级 > i * 3)
								{
									gameObject.SetActive(value: true);
									gameObject.transform.GetComponent<Text>().text = "(数量" + num4.ToString() + ")";
									兵种列表对象.transform.GetChild(i).GetChild(15).gameObject.SetActive(value: false);
								}
								else
								{
									gameObject2.SetActive(value: true);
									兵种列表对象.transform.GetChild(i).GetChild(15).gameObject.SetActive(value: true);
								}
								兵种列表对象.transform.GetChild(i).GetChild(4).GetComponent<Text>()
									.text = 兵种属性库类.攻击力.ToString();
									兵种列表对象.transform.GetChild(i).GetChild(6).GetComponent<Text>()
										.text = 兵种属性库类.防御力.ToString();
										兵种列表对象.transform.GetChild(i).GetChild(8).GetComponent<Text>()
											.text = 兵种属性库类.生命值.ToString();
											兵种列表对象.transform.GetChild(i).GetChild(10).GetComponent<Text>()
												.text = 兵种属性库类.攻击速度.ToString();
												兵种列表对象.transform.GetChild(i).GetChild(12).GetComponent<Text>()
													.text = 兵种属性库类.移动速度.ToString();
													兵种列表对象.transform.GetChild(i).GetChild(14).GetComponent<Text>()
														.text = 兵种属性库类.占用人口.ToString();
													}
												}
											}

											public void 招募选中兵种()
											{
												int num = 0;
												for (int i = 0; i < 4; i++)
												{
													if (兵种列表对象.transform.GetChild(i).GetChild(16).gameObject.activeSelf)
													{
														num = (兵营类型 - 3) * 100 + (i + 1);
														UnityEngine.Debug.Log("选中ID" + num.ToString());
														break;
													}
												}
												if (num != 0)
												{
													调整招募数量脚本对象.第几个封地 = 第几个封地;
													调整招募数量脚本对象.第几个玩家 = 第几个玩家;
													调整招募数量脚本对象.调整类型 = 1;
													调整招募数量脚本对象.兵种ID = num;
													调整招募数量脚本对象.显示调整界面();
													调整招募数量脚本对象.显示说明文本();
												}
											}
										}
