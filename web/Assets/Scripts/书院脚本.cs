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
		for (int i = 0; i < Dwsg.Shared.Economy.TechnologyRules.Count; i++)
		{
			if (!书院列表对象.transform.GetChild(i).GetChild(5).gameObject.activeSelf) continue;
			int 请求玩家 = 第几个玩家, 请求封地 = 第几个封地, 请求建筑 = 第几个建筑;
			TechnologyClient.Upgrade(请求玩家, 请求封地, 请求建筑, i, 结果 =>
			{
				if (this == null) return;
				全局变量.提示类.显示信息(结果?.Message ?? "服务器未确认科技升级，请重试");
				if (结果 != null && 结果.Code == Dwsg.Shared.GameCodes.Ok && 第几个玩家 == 请求玩家 && 第几个封地 == 请求封地 && 第几个建筑 == 请求建筑)
					显示科技列表();
			});
			return;
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

}
