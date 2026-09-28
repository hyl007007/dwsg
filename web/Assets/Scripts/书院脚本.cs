using UnityEngine;
using UnityEngine.UI;

public class 书院脚本 : MonoBehaviour
{
	public GameObject 书院列表对象;

	public GameObject 书院信息对象;

	public int 第几个玩家;

	public int 第几个封地;

	public int 第几个建筑;

	public void 显示书院建筑信息()
	{
		float num = 全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地].建筑信息表[第几个建筑].等级;
		int num2 = 全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地].建筑信息表[第几个建筑].获取建筑头像索引();
		书院信息对象.transform.GetChild(1).GetComponent<Image>().sprite = 全局变量.书院头像资源表[num2];
		书院信息对象.transform.GetChild(2).GetComponent<Text>().text = "书院(" + num.ToString() + "级)";
	}

	public void 显示科技列表()
	{
		double 工程设计 = 全局变量.所有玩家数据表[第几个玩家].科技信息.工程设计;
		Text component = 书院列表对象.transform.GetChild(0).GetChild(3).GetComponent<Text>();
		Text component2 = 书院列表对象.transform.GetChild(0).GetChild(4).GetComponent<Text>();
		component.text = "(" + 工程设计.ToString() + "级)";
		if (工程设计 < 10.0)
		{
			component2.text = "升级:人口上限增加" + ((工程设计 + 1.0) * 5.0).ToString() + "%";
		}
		else
		{
			component2.text = "已满级:人口上限增加" + (工程设计 * 5.0).ToString() + "%";
		}
		工程设计 = 全局变量.所有玩家数据表[第几个玩家].科技信息.征召技巧;
		Text component3 = 书院列表对象.transform.GetChild(1).GetChild(3).GetComponent<Text>();
		component2 = 书院列表对象.transform.GetChild(1).GetChild(4).GetComponent<Text>();
		component3.text = "(" + 工程设计.ToString() + "级)";
		if (工程设计 < 10.0)
		{
			component2.text = "升级:士兵招募速度加快" + ((工程设计 + 1.0) * 5.0).ToString() + "%";
		}
		else
		{
			component2.text = "已满级:士兵招募速度加快" + (工程设计 * 5.0).ToString() + "%";
		}
		工程设计 = 全局变量.所有玩家数据表[第几个玩家].科技信息.种植技术;
		Text component4 = 书院列表对象.transform.GetChild(2).GetChild(3).GetComponent<Text>();
		component2 = 书院列表对象.transform.GetChild(2).GetChild(4).GetComponent<Text>();
		component4.text = "(" + 工程设计.ToString() + "级)";
		if (工程设计 < 10.0)
		{
			component2.text = "升级:粮食产量增加" + ((工程设计 + 1.0) * 10.0).ToString() + "%";
		}
		else
		{
			component2.text = "已满级:粮食产量增加" + (工程设计 * 10.0).ToString() + "%";
		}
		工程设计 = 全局变量.所有玩家数据表[第几个玩家].科技信息.行军技巧;
		Text component5 = 书院列表对象.transform.GetChild(3).GetChild(3).GetComponent<Text>();
		component2 = 书院列表对象.transform.GetChild(3).GetChild(4).GetComponent<Text>();
		component5.text = "(" + 工程设计.ToString() + "级)";
		if (工程设计 < 10.0)
		{
			component2.text = "升级:军队行军速度加快" + ((工程设计 + 1.0) * 5.0).ToString() + "%";
		}
		else
		{
			component2.text = "已满级:军队行军速度加快" + (工程设计 * 5.0).ToString() + "%";
		}
		工程设计 = 全局变量.所有玩家数据表[第几个玩家].科技信息.市场贸易;
		Text component6 = 书院列表对象.transform.GetChild(4).GetChild(3).GetComponent<Text>();
		component2 = 书院列表对象.transform.GetChild(4).GetChild(4).GetComponent<Text>();
		component6.text = "(" + 工程设计.ToString() + "级)";
		if (工程设计 < 10.0)
		{
			component2.text = "升级:铜钱产量增加" + ((工程设计 + 1.0) * 5.0).ToString() + "%";
		}
		else
		{
			component2.text = "已满级:铜钱产量增加" + (工程设计 * 5.0).ToString() + "%";
		}
		工程设计 = 全局变量.所有玩家数据表[第几个玩家].科技信息.建筑学;
		Text component7 = 书院列表对象.transform.GetChild(5).GetChild(3).GetComponent<Text>();
		component2 = 书院列表对象.transform.GetChild(5).GetChild(4).GetComponent<Text>();
		component7.text = "(" + 工程设计.ToString() + "级)";
		if (工程设计 < 10.0)
		{
			component2.text = "升级:建筑升级建造拆除速度加快" + ((工程设计 + 1.0) * 5.0).ToString() + "%";
		}
		else
		{
			component2.text = "已满级:建筑升级建造拆除速度加快" + (工程设计 * 5.0).ToString() + "%";
		}
		工程设计 = 全局变量.所有玩家数据表[第几个玩家].科技信息.铸铁技术;
		Text component8 = 书院列表对象.transform.GetChild(6).GetChild(3).GetComponent<Text>();
		component2 = 书院列表对象.transform.GetChild(6).GetChild(4).GetComponent<Text>();
		component8.text = "(" + 工程设计.ToString() + "级)";
		if (工程设计 < 10.0)
		{
			component2.text = "升级:士兵攻击增加" + ((工程设计 + 1.0) * 4.0).ToString() + "%";
		}
		else
		{
			component2.text = "已满级:士兵攻击增加" + (工程设计 * 4.0).ToString() + "%";
		}
		工程设计 = 全局变量.所有玩家数据表[第几个玩家].科技信息.甲胄制造;
		Text component9 = 书院列表对象.transform.GetChild(7).GetChild(3).GetComponent<Text>();
		component2 = 书院列表对象.transform.GetChild(7).GetChild(4).GetComponent<Text>();
		component9.text = "(" + 工程设计.ToString() + "级)";
		if (工程设计 < 10.0)
		{
			component2.text = "升级:士兵的防御增加" + ((工程设计 + 1.0) * 3.0).ToString() + "%";
		}
		else
		{
			component2.text = "已满级:士兵的防御增加" + (工程设计 * 3.0).ToString() + "%";
		}
		工程设计 = 全局变量.所有玩家数据表[第几个玩家].科技信息.药草研究;
		Text component10 = 书院列表对象.transform.GetChild(8).GetChild(3).GetComponent<Text>();
		component2 = 书院列表对象.transform.GetChild(8).GetChild(4).GetComponent<Text>();
		component10.text = "(" + 工程设计.ToString() + "级)";
		if (工程设计 < 10.0)
		{
			component2.text = "升级:士兵的生命增加" + ((工程设计 + 1.0) * 5.0).ToString() + "%";
		}
		else
		{
			component2.text = "已满级:士兵的生命增加" + (工程设计 * 5.0).ToString() + "%";
		}
		工程设计 = 全局变量.所有玩家数据表[第几个玩家].科技信息.阵法技巧;
		Text component11 = 书院列表对象.transform.GetChild(9).GetChild(3).GetComponent<Text>();
		component2 = 书院列表对象.transform.GetChild(9).GetChild(4).GetComponent<Text>();
		component11.text = "(" + 工程设计.ToString() + "级)";
		if (工程设计 < 10.0)
		{
			component2.text = "升级:步兵的防御增加" + ((工程设计 + 1.0) * 5.0).ToString() + "%";
		}
		else
		{
			component2.text = "已满级:步兵的防御增加" + (工程设计 * 5.0).ToString() + "%";
		}
		工程设计 = 全局变量.所有玩家数据表[第几个玩家].科技信息.抛射技巧;
		Text component12 = 书院列表对象.transform.GetChild(10).GetChild(3).GetComponent<Text>();
		component2 = 书院列表对象.transform.GetChild(10).GetChild(4).GetComponent<Text>();
		component12.text = "(" + 工程设计.ToString() + "级)";
		if (工程设计 < 10.0)
		{
			component2.text = "升级:弓兵的攻击增加" + ((工程设计 + 1.0) * 6.0).ToString() + "%";
		}
		else
		{
			component2.text = "已满级:弓兵的攻击增加" + (工程设计 * 6.0).ToString() + "%";
		}
		工程设计 = 全局变量.所有玩家数据表[第几个玩家].科技信息.驾驭技巧;
		Text component13 = 书院列表对象.transform.GetChild(11).GetChild(3).GetComponent<Text>();
		component2 = 书院列表对象.transform.GetChild(11).GetChild(4).GetComponent<Text>();
		component13.text = "(" + 工程设计.ToString() + "级)";
		if (工程设计 < 10.0)
		{
			component2.text = "升级:骑兵的攻防增加" + ((工程设计 + 1.0) * 3.0).ToString() + "%";
		}
		else
		{
			component2.text = "已满级:骑兵的攻防增加" + (工程设计 * 3.0).ToString() + "%";
		}
		工程设计 = 全局变量.所有玩家数据表[第几个玩家].科技信息.战车设计;
		Text component14 = 书院列表对象.transform.GetChild(12).GetChild(3).GetComponent<Text>();
		component2 = 书院列表对象.transform.GetChild(12).GetChild(4).GetComponent<Text>();
		component14.text = "(" + 工程设计.ToString() + "级)";
		if (工程设计 < 10.0)
		{
			component2.text = "升级:战车的移速+" + ((工程设计 + 1.0) * 10.0).ToString() + "%,攻速+" + ((工程设计 + 1.0) * 3.0).ToString() + "%";
		}
		else
		{
			component2.text = "已满级:战车的移速+" + (工程设计 * 10.0).ToString() + "%,攻速+" + (工程设计 * 3.0).ToString() + "%";
		}
		工程设计 = 全局变量.所有玩家数据表[第几个玩家].科技信息.统帅能力;
		Text component15 = 书院列表对象.transform.GetChild(13).GetChild(3).GetComponent<Text>();
		component2 = 书院列表对象.transform.GetChild(13).GetChild(4).GetComponent<Text>();
		component15.text = "(" + 工程设计.ToString() + "级)";
		if (工程设计 < 10.0)
		{
			component2.text = "升级:将领的统兵数量增加" + ((工程设计 + 1.0) * 5.0).ToString() + "%";
		}
		else
		{
			component2.text = "已满级:将领的统兵数量增加" + (工程设计 * 5.0).ToString() + "%";
		}
		工程设计 = 全局变量.所有玩家数据表[第几个玩家].科技信息.信仰;
		Text component16 = 书院列表对象.transform.GetChild(14).GetChild(3).GetComponent<Text>();
		component2 = 书院列表对象.transform.GetChild(14).GetChild(4).GetComponent<Text>();
		component16.text = "(" + 工程设计.ToString() + "级)";
		if (工程设计 < 10.0)
		{
			component2.text = "升级:减少将领提升忠诚度费用的" + ((工程设计 + 1.0) * 5.0).ToString() + "%";
		}
		else
		{
			component2.text = "已满级:减少将领提升忠诚度费用的" + (工程设计 * 5.0).ToString() + "%";
		}
		工程设计 = 全局变量.所有玩家数据表[第几个玩家].科技信息.仓储;
		Text component17 = 书院列表对象.transform.GetChild(15).GetChild(3).GetComponent<Text>();
		component2 = 书院列表对象.transform.GetChild(15).GetChild(4).GetComponent<Text>();
		component17.text = "(" + 工程设计.ToString() + "级)";
		if (工程设计 < 5.0)
		{
			component2.text = "升级:遭受掠夺时减少" + ((工程设计 + 1.0) * 10.0).ToString() + "%的资源损失";
		}
		else
		{
			component2.text = "已满级:遭受掠夺时减少" + (工程设计 * 10.0).ToString() + "%的资源损失";
		}
		工程设计 = 全局变量.所有玩家数据表[第几个玩家].科技信息.安置;
		Text component18 = 书院列表对象.transform.GetChild(16).GetChild(3).GetComponent<Text>();
		component2 = 书院列表对象.transform.GetChild(16).GetChild(4).GetComponent<Text>();
		component18.text = "(" + 工程设计.ToString() + "级)";
		if (工程设计 < 5.0)
		{
			component2.text = "升级:每个房屋增加" + ((工程设计 + 1.0) * 40.0).ToString() + "个人口上限";
		}
		else
		{
			component2.text = "已满级:每个房屋增加" + (工程设计 * 40.0).ToString() + "个人口上限";
		}
		工程设计 = 全局变量.所有玩家数据表[第几个玩家].科技信息.格斗;
		Text component19 = 书院列表对象.transform.GetChild(17).GetChild(3).GetComponent<Text>();
		component2 = 书院列表对象.transform.GetChild(17).GetChild(4).GetComponent<Text>();
		component19.text = "(" + 工程设计.ToString() + "级)";
		if (工程设计 < 5.0)
		{
			component2.text = "升级:步兵格挡骑兵或步兵攻击的几率" + ((工程设计 + 1.0) * 3.0).ToString() + "%";
		}
		else
		{
			component2.text = "已满级:步兵格挡骑兵或步兵攻击的几率" + (工程设计 * 3.0).ToString() + "%";
		}
		工程设计 = 全局变量.所有玩家数据表[第几个玩家].科技信息.精准;
		Text component20 = 书院列表对象.transform.GetChild(18).GetChild(3).GetComponent<Text>();
		component2 = 书院列表对象.transform.GetChild(18).GetChild(4).GetComponent<Text>();
		component20.text = "(" + 工程设计.ToString() + "级)";
		if (工程设计 < 5.0)
		{
			component2.text = "升级:弓兵攻击时穿透的几率" + ((工程设计 + 1.0) * 6.0).ToString() + "%无视对方防御";
		}
		else
		{
			component2.text = "已满级:弓兵攻击时穿透的几率" + (工程设计 * 6.0).ToString() + "%无视对方防御";
		}
		工程设计 = 全局变量.所有玩家数据表[第几个玩家].科技信息.驯马;
		Text component21 = 书院列表对象.transform.GetChild(19).GetChild(3).GetComponent<Text>();
		component2 = 书院列表对象.transform.GetChild(19).GetChild(4).GetComponent<Text>();
		component21.text = "(" + 工程设计.ToString() + "级)";
		if (工程设计 < 5.0)
		{
			component2.text = "升级:骑兵闪避弓兵或战车攻击的几率" + ((工程设计 + 1.0) * 7.0).ToString() + "%";
		}
		else
		{
			component2.text = "已满级:骑兵闪避弓兵或战车攻击的几率" + (工程设计 * 7.0).ToString() + "%";
		}
		工程设计 = 全局变量.所有玩家数据表[第几个玩家].科技信息.精工;
		Text component22 = 书院列表对象.transform.GetChild(20).GetChild(3).GetComponent<Text>();
		component2 = 书院列表对象.transform.GetChild(20).GetChild(4).GetComponent<Text>();
		component22.text = "(" + 工程设计.ToString() + "级)";
		if (工程设计 < 5.0)
		{
			component2.text = "升级:招募战车资源时间-" + ((工程设计 + 1.0) * 5.0).ToString() + "%,人口占用-" + ((工程设计 + 1.0) * 10.0).ToString() + "%";
		}
		else
		{
			component2.text = "已满级:招募战车资源时间-" + (工程设计 * 5.0).ToString() + "%,人口占用-" + (工程设计 * 10.0).ToString() + "%";
		}
	}

	public void 升级选中科技()
	{
		int 等级 = 全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地].建筑信息表[第几个建筑].等级;
		if (书院列表对象.transform.GetChild(0).GetChild(5).gameObject.activeSelf)
		{
			double 工程设计 = 全局变量.所有玩家数据表[第几个玩家].科技信息.工程设计;
			if ((double)等级 > 工程设计 && 工程设计 < 15.0)
			{
				double num = 获取升级需要铜钱(2.0, 工程设计);
				if (全局变量.所有玩家数据表[第几个玩家].财产信息.扣除铜钱(num))
				{
					全局变量.所有玩家数据表[第几个玩家].科技信息.工程设计 += 1.0;
					全局变量.提示类.显示信息("升级成功,消耗铜钱:" + (num / 10000.0).ToString() + "W");
				}
				else
				{
					全局变量.提示类.显示信息("铜钱不足 " + (num / 10000.0).ToString() + "W 升级失败!");
				}
			}
		}
		else if (书院列表对象.transform.GetChild(1).GetChild(5).gameObject.activeSelf)
		{
			double 征召技巧 = 全局变量.所有玩家数据表[第几个玩家].科技信息.征召技巧;
			if ((double)等级 > 征召技巧 && 征召技巧 < 10.0)
			{
				double num2 = 获取升级需要铜钱(2.0, 征召技巧);
				if (全局变量.所有玩家数据表[第几个玩家].财产信息.扣除铜钱(num2))
				{
					全局变量.所有玩家数据表[第几个玩家].科技信息.征召技巧 += 1.0;
					全局变量.提示类.显示信息("升级成功,消耗铜钱:" + (num2 / 10000.0).ToString() + "W");
				}
				else
				{
					全局变量.提示类.显示信息("铜钱不足 " + (num2 / 10000.0).ToString() + "W 升级失败!");
				}
			}
		}
		else if (书院列表对象.transform.GetChild(2).GetChild(5).gameObject.activeSelf)
		{
			double 种植技术 = 全局变量.所有玩家数据表[第几个玩家].科技信息.种植技术;
			if ((double)等级 > 种植技术 && 种植技术 < 10.0)
			{
				double num3 = 获取升级需要铜钱(0.24, 种植技术);
				if (全局变量.所有玩家数据表[第几个玩家].财产信息.扣除铜钱(num3))
				{
					全局变量.所有玩家数据表[第几个玩家].科技信息.种植技术 += 1.0;
					全局变量.提示类.显示信息("升级成功,消耗铜钱:" + (num3 / 10000.0).ToString() + "W");
				}
				else
				{
					全局变量.提示类.显示信息("铜钱不足 " + (num3 / 10000.0).ToString() + "W 升级失败!");
				}
			}
		}
		else if (书院列表对象.transform.GetChild(3).GetChild(5).gameObject.activeSelf)
		{
			double 行军技巧 = 全局变量.所有玩家数据表[第几个玩家].科技信息.行军技巧;
			if ((double)等级 > 行军技巧 && 行军技巧 < 10.0)
			{
				double num4 = 获取升级需要铜钱(2.2, 行军技巧);
				if (全局变量.所有玩家数据表[第几个玩家].财产信息.扣除铜钱(num4))
				{
					全局变量.所有玩家数据表[第几个玩家].科技信息.行军技巧 += 1.0;
					全局变量.提示类.显示信息("升级成功,消耗铜钱:" + (num4 / 10000.0).ToString() + "W");
				}
				else
				{
					全局变量.提示类.显示信息("铜钱不足 " + (num4 / 10000.0).ToString() + "W 升级失败!");
				}
			}
		}
		else if (书院列表对象.transform.GetChild(4).GetChild(5).gameObject.activeSelf)
		{
			double 市场贸易 = 全局变量.所有玩家数据表[第几个玩家].科技信息.市场贸易;
			if ((double)等级 > 市场贸易 && 市场贸易 < 10.0)
			{
				double num5 = 获取升级需要铜钱(0.24, 市场贸易);
				if (全局变量.所有玩家数据表[第几个玩家].财产信息.扣除铜钱(num5))
				{
					全局变量.所有玩家数据表[第几个玩家].科技信息.市场贸易 += 1.0;
					全局变量.提示类.显示信息("升级成功,消耗铜钱:" + (num5 / 10000.0).ToString() + "W");
				}
				else
				{
					全局变量.提示类.显示信息("铜钱不足 " + (num5 / 10000.0).ToString() + "W 升级失败!");
				}
			}
		}
		else if (书院列表对象.transform.GetChild(5).GetChild(5).gameObject.activeSelf)
		{
			double 建筑学 = 全局变量.所有玩家数据表[第几个玩家].科技信息.建筑学;
			if ((double)等级 > 建筑学 && 建筑学 < 10.0)
			{
				double num6 = 获取升级需要铜钱(0.24, 建筑学);
				if (全局变量.所有玩家数据表[第几个玩家].财产信息.扣除铜钱(num6))
				{
					全局变量.所有玩家数据表[第几个玩家].科技信息.建筑学 += 1.0;
					全局变量.提示类.显示信息("升级成功,消耗铜钱:" + (num6 / 10000.0).ToString() + "W");
				}
				else
				{
					全局变量.提示类.显示信息("铜钱不足 " + (num6 / 10000.0).ToString() + "W 升级失败!");
				}
			}
		}
		else if (书院列表对象.transform.GetChild(6).GetChild(5).gameObject.activeSelf)
		{
			double 铸铁技术 = 全局变量.所有玩家数据表[第几个玩家].科技信息.铸铁技术;
			if ((double)等级 > 铸铁技术 && 铸铁技术 < 10.0)
			{
				double num7 = 获取升级需要铜钱(2.8, 铸铁技术);
				if (全局变量.所有玩家数据表[第几个玩家].财产信息.扣除铜钱(num7))
				{
					全局变量.所有玩家数据表[第几个玩家].科技信息.铸铁技术 += 1.0;
					全局变量.提示类.显示信息("升级成功,消耗铜钱:" + (num7 / 10000.0).ToString() + "W");
				}
				else
				{
					全局变量.提示类.显示信息("铜钱不足 " + (num7 / 10000.0).ToString() + "W 升级失败!");
				}
			}
		}
		else if (书院列表对象.transform.GetChild(7).GetChild(5).gameObject.activeSelf)
		{
			double 甲胄制造 = 全局变量.所有玩家数据表[第几个玩家].科技信息.甲胄制造;
			if ((double)等级 > 甲胄制造 && 甲胄制造 < 10.0)
			{
				double num8 = 获取升级需要铜钱(2.5, 甲胄制造);
				if (全局变量.所有玩家数据表[第几个玩家].财产信息.扣除铜钱(num8))
				{
					全局变量.所有玩家数据表[第几个玩家].科技信息.甲胄制造 += 1.0;
					全局变量.提示类.显示信息("升级成功,消耗铜钱:" + (num8 / 10000.0).ToString() + "W");
				}
				else
				{
					全局变量.提示类.显示信息("铜钱不足 " + (num8 / 10000.0).ToString() + "W 升级失败!");
				}
			}
		}
		else if (书院列表对象.transform.GetChild(8).GetChild(5).gameObject.activeSelf)
		{
			double 药草研究 = 全局变量.所有玩家数据表[第几个玩家].科技信息.药草研究;
			if ((double)等级 > 药草研究 && 药草研究 < 10.0)
			{
				double num9 = 获取升级需要铜钱(2.5, 药草研究);
				if (全局变量.所有玩家数据表[第几个玩家].财产信息.扣除铜钱(num9))
				{
					全局变量.所有玩家数据表[第几个玩家].科技信息.药草研究 += 1.0;
					全局变量.提示类.显示信息("升级成功,消耗铜钱:" + (num9 / 10000.0).ToString() + "W");
				}
				else
				{
					全局变量.提示类.显示信息("铜钱不足 " + (num9 / 10000.0).ToString() + "W 升级失败!");
				}
			}
		}
		else if (书院列表对象.transform.GetChild(9).GetChild(5).gameObject.activeSelf)
		{
			double 阵法技巧 = 全局变量.所有玩家数据表[第几个玩家].科技信息.阵法技巧;
			if ((double)等级 > 阵法技巧 && 阵法技巧 < 10.0)
			{
				double num10 = 获取升级需要铜钱(2.0, 阵法技巧);
				if (全局变量.所有玩家数据表[第几个玩家].财产信息.扣除铜钱(num10))
				{
					全局变量.所有玩家数据表[第几个玩家].科技信息.阵法技巧 += 1.0;
					全局变量.提示类.显示信息("升级成功,消耗铜钱:" + (num10 / 10000.0).ToString() + "W");
				}
				else
				{
					全局变量.提示类.显示信息("铜钱不足 " + (num10 / 10000.0).ToString() + "W 升级失败!");
				}
			}
		}
		else if (书院列表对象.transform.GetChild(10).GetChild(5).gameObject.activeSelf)
		{
			double 抛射技巧 = 全局变量.所有玩家数据表[第几个玩家].科技信息.抛射技巧;
			if ((double)等级 > 抛射技巧 && 抛射技巧 < 10.0)
			{
				double num11 = 获取升级需要铜钱(2.0, 抛射技巧);
				if (全局变量.所有玩家数据表[第几个玩家].财产信息.扣除铜钱(num11))
				{
					全局变量.所有玩家数据表[第几个玩家].科技信息.抛射技巧 += 1.0;
					全局变量.提示类.显示信息("升级成功,消耗铜钱:" + (num11 / 10000.0).ToString() + "W");
				}
				else
				{
					全局变量.提示类.显示信息("铜钱不足 " + (num11 / 10000.0).ToString() + "W 升级失败!");
				}
			}
		}
		else if (书院列表对象.transform.GetChild(11).GetChild(5).gameObject.activeSelf)
		{
			double 驾驭技巧 = 全局变量.所有玩家数据表[第几个玩家].科技信息.驾驭技巧;
			if ((double)等级 > 驾驭技巧 && 驾驭技巧 < 10.0)
			{
				double num12 = 获取升级需要铜钱(2.0, 驾驭技巧);
				if (全局变量.所有玩家数据表[第几个玩家].财产信息.扣除铜钱(num12))
				{
					全局变量.所有玩家数据表[第几个玩家].科技信息.驾驭技巧 += 1.0;
					全局变量.提示类.显示信息("升级成功,消耗铜钱:" + (num12 / 10000.0).ToString() + "W");
				}
				else
				{
					全局变量.提示类.显示信息("铜钱不足 " + (num12 / 10000.0).ToString() + "W 升级失败!");
				}
			}
		}
		else if (书院列表对象.transform.GetChild(12).GetChild(5).gameObject.activeSelf)
		{
			double 战车设计 = 全局变量.所有玩家数据表[第几个玩家].科技信息.战车设计;
			if ((double)等级 > 战车设计 && 战车设计 < 10.0)
			{
				double num13 = 获取升级需要铜钱(2.0, 战车设计);
				if (全局变量.所有玩家数据表[第几个玩家].财产信息.扣除铜钱(num13))
				{
					全局变量.所有玩家数据表[第几个玩家].科技信息.战车设计 += 1.0;
					全局变量.提示类.显示信息("升级成功,消耗铜钱:" + (num13 / 10000.0).ToString() + "W");
				}
				else
				{
					全局变量.提示类.显示信息("铜钱不足 " + (num13 / 10000.0).ToString() + "W 升级失败!");
				}
			}
		}
		else if (书院列表对象.transform.GetChild(13).GetChild(5).gameObject.activeSelf)
		{
			double 统帅能力 = 全局变量.所有玩家数据表[第几个玩家].科技信息.统帅能力;
			if ((double)等级 > 统帅能力 && 统帅能力 < 10.0)
			{
				double num14 = 获取升级需要铜钱(3.0, 统帅能力);
				if (全局变量.所有玩家数据表[第几个玩家].财产信息.扣除铜钱(num14))
				{
					全局变量.所有玩家数据表[第几个玩家].科技信息.统帅能力 += 1.0;
					全局变量.提示类.显示信息("升级成功,消耗铜钱:" + (num14 / 10000.0).ToString() + "W");
				}
				else
				{
					全局变量.提示类.显示信息("铜钱不足 " + (num14 / 10000.0).ToString() + "W 升级失败!");
				}
			}
		}
		else if (书院列表对象.transform.GetChild(14).GetChild(5).gameObject.activeSelf)
		{
			double 信仰 = 全局变量.所有玩家数据表[第几个玩家].科技信息.信仰;
			if ((double)等级 > 信仰 && 信仰 < 10.0)
			{
				double num15 = 获取升级需要铜钱(0.4, 信仰);
				if (全局变量.所有玩家数据表[第几个玩家].财产信息.扣除铜钱(num15))
				{
					全局变量.所有玩家数据表[第几个玩家].科技信息.信仰 += 1.0;
					全局变量.提示类.显示信息("升级成功,消耗铜钱:" + (num15 / 10000.0).ToString() + "W");
				}
				else
				{
					全局变量.提示类.显示信息("铜钱不足 " + (num15 / 10000.0).ToString() + "W 升级失败!");
				}
			}
		}
		else if (书院列表对象.transform.GetChild(15).GetChild(5).gameObject.activeSelf)
		{
			double 仓储 = 全局变量.所有玩家数据表[第几个玩家].科技信息.仓储;
			if ((double)等级 > 10.0 + 仓储)
			{
				if (仓储 < 5.0)
				{
					double num16 = 获取升级需要铜钱(5.0, 仓储);
					if (全局变量.所有玩家数据表[第几个玩家].财产信息.扣除黄金(num16))
					{
						全局变量.所有玩家数据表[第几个玩家].科技信息.仓储 += 1.0;
						全局变量.提示类.显示信息("升级成功,消耗黄金:" + (num16 / 10000.0).ToString() + "W");
					}
					else
					{
						全局变量.提示类.显示信息("黄金不足 " + (num16 / 10000.0).ToString() + "W 升级失败!");
					}
				}
			}
			else
			{
				全局变量.提示类.显示信息("书院等级不足!");
			}
		}
		else if (书院列表对象.transform.GetChild(16).GetChild(5).gameObject.activeSelf)
		{
			double 安置 = 全局变量.所有玩家数据表[第几个玩家].科技信息.安置;
			if ((double)等级 > 10.0 + 安置)
			{
				if (安置 < 5.0)
				{
					double num17 = 获取升级需要铜钱(5.0, 安置);
					if (全局变量.所有玩家数据表[第几个玩家].财产信息.扣除黄金(num17))
					{
						全局变量.所有玩家数据表[第几个玩家].科技信息.安置 += 1.0;
						全局变量.提示类.显示信息("升级成功,消耗黄金:" + (num17 / 10000.0).ToString() + "W");
					}
					else
					{
						全局变量.提示类.显示信息("黄金不足 " + (num17 / 10000.0).ToString() + "W 升级失败!");
					}
				}
			}
			else
			{
				全局变量.提示类.显示信息("书院等级不足!");
			}
		}
		else if (书院列表对象.transform.GetChild(17).GetChild(5).gameObject.activeSelf)
		{
			double 格斗 = 全局变量.所有玩家数据表[第几个玩家].科技信息.格斗;
			if ((double)等级 > 10.0 + 格斗)
			{
				if (格斗 < 5.0)
				{
					double num18 = 获取升级需要铜钱(5.0, 格斗);
					if (全局变量.所有玩家数据表[第几个玩家].财产信息.扣除黄金(num18))
					{
						全局变量.所有玩家数据表[第几个玩家].科技信息.格斗 += 1.0;
						全局变量.提示类.显示信息("升级成功,消耗黄金:" + (num18 / 10000.0).ToString() + "W");
					}
					else
					{
						全局变量.提示类.显示信息("黄金不足 " + (num18 / 10000.0).ToString() + "W 升级失败!");
					}
				}
			}
			else
			{
				全局变量.提示类.显示信息("书院等级不足!");
			}
		}
		else if (书院列表对象.transform.GetChild(18).GetChild(5).gameObject.activeSelf)
		{
			double 精准 = 全局变量.所有玩家数据表[第几个玩家].科技信息.精准;
			if ((double)等级 > 10.0 + 精准)
			{
				if (精准 < 5.0)
				{
					double num19 = 获取升级需要铜钱(5.0, 精准);
					if (全局变量.所有玩家数据表[第几个玩家].财产信息.扣除黄金(num19))
					{
						全局变量.所有玩家数据表[第几个玩家].科技信息.精准 += 1.0;
						全局变量.提示类.显示信息("升级成功,消耗黄金:" + (num19 / 10000.0).ToString() + "W");
					}
					else
					{
						全局变量.提示类.显示信息("黄金不足 " + (num19 / 10000.0).ToString() + "W 升级失败!");
					}
				}
			}
			else
			{
				全局变量.提示类.显示信息("书院等级不足!");
			}
		}
		else if (书院列表对象.transform.GetChild(19).GetChild(5).gameObject.activeSelf)
		{
			double 驯马 = 全局变量.所有玩家数据表[第几个玩家].科技信息.驯马;
			if ((double)等级 > 10.0 + 驯马)
			{
				if (驯马 < 5.0)
				{
					double num20 = 获取升级需要铜钱(5.0, 驯马);
					if (全局变量.所有玩家数据表[第几个玩家].财产信息.扣除黄金(num20))
					{
						全局变量.所有玩家数据表[第几个玩家].科技信息.驯马 += 1.0;
						全局变量.提示类.显示信息("升级成功,消耗黄金:" + (num20 / 10000.0).ToString() + "W");
					}
					else
					{
						全局变量.提示类.显示信息("黄金不足 " + (num20 / 10000.0).ToString() + "W 升级失败!");
					}
				}
			}
			else
			{
				全局变量.提示类.显示信息("书院等级不足!");
			}
		}
		else if (书院列表对象.transform.GetChild(20).GetChild(5).gameObject.activeSelf)
		{
			double 精工 = 全局变量.所有玩家数据表[第几个玩家].科技信息.精工;
			if ((double)等级 > 10.0 + 精工)
			{
				if (精工 < 5.0)
				{
					double num21 = 获取升级需要铜钱(5.0, 精工);
					if (全局变量.所有玩家数据表[第几个玩家].财产信息.扣除黄金(num21))
					{
						全局变量.所有玩家数据表[第几个玩家].科技信息.精工 += 1.0;
						全局变量.提示类.显示信息("升级成功,消耗黄金:" + (num21 / 10000.0).ToString() + "W");
					}
					else
					{
						全局变量.提示类.显示信息("黄金不足 " + (num21 / 10000.0).ToString() + "W 升级失败!");
					}
				}
			}
			else
			{
				全局变量.提示类.显示信息("书院等级不足!");
			}
		}
		显示科技列表();
	}

	public void 选中高亮()
	{
		int childCount = 书院列表对象.transform.childCount;
		for (int i = 0; i < childCount; i++)
		{
			GameObject gameObject = 书院列表对象.transform.GetChild(i).gameObject;
			Text component = gameObject.transform.GetChild(2).GetComponent<Text>();
			Text component2 = gameObject.transform.GetChild(3).GetComponent<Text>();
			Text component3 = gameObject.transform.GetChild(4).GetComponent<Text>();
			if (gameObject.transform.GetChild(5).gameObject.activeSelf)
			{
				component.color = 颜色类.GetColor("#329696");
				component2.color = 颜色类.GetColor("#FFF019");
				component3.color = 颜色类.GetColor("#78FFC8");
			}
			else
			{
				component.color = 颜色类.GetColor("#C8C8C8");
				component2.color = 颜色类.GetColor("#C8C8C8");
				component3.color = 颜色类.GetColor("#C8C8C8");
			}
		}
	}

	private double 获取升级需要铜钱(double 基数, double 等级)
	{
		double num = 基数;
		for (int i = 0; (double)i < 等级; i++)
		{
			num *= 2.0;
		}
		num *= 10000.0;
		if (num > 15000000.0)
		{
			num = 15000000.0;
		}
		return num;
	}
}
