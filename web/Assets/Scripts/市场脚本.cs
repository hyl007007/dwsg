using UnityEngine;
using UnityEngine.UI;

public class 市场脚本 : MonoBehaviour
{
	public Toggle 购买资源选项;

	public Toggle 转换资源选项;

	public Text 黄金数量;

	public Text 白银数量;

	public Text 铜钱购买提示;

	public Text 粮食购买提示;

	public Text 铜钱数量;

	public Text 粮食数量;

	public Text 铜钱转换提示;

	public Text 粮食转换提示;

	public double 铜钱单价;

	public double 粮食单价;

	public 调整数量脚本 调整数量脚本对象;

	public void 刷新显示()
	{
		int 本机身份 = 全局变量.本机身份;
		var 报价 = Dwsg.Economy.MarketClient.GetQuote();
		铜钱单价 = Dwsg.Shared.Economy.MarketRules.Rate(报价, 5);
		粮食单价 = Dwsg.Shared.Economy.MarketRules.Rate(报价, 6);
		if (购买资源选项.isOn)
		{
			黄金数量.text = 全局变量.所有玩家数据表[本机身份].财产信息.黄金.ToString();
			白银数量.text = 全局变量.所有玩家数据表[本机身份].财产信息.白银.ToString();
			铜钱购买提示.text = 报价 == null ? "正在获取市场报价" : "1黄金=" + 铜钱单价.ToString() + "铜钱";
			粮食购买提示.text = 报价 == null ? "正在获取市场报价" : "1黄金=" + 粮食单价.ToString() + "粮食";
		}
		else if (转换资源选项.isOn)
		{
			铜钱数量.text = 全局变量.所有玩家数据表[本机身份].财产信息.铜钱.ToString();
			粮食数量.text = 全局变量.所有玩家数据表[本机身份].财产信息.粮食.ToString();
		}
	}

	public void 黄金购买铜钱()
	{
		显示资源兑换(5);
	}

	public void 黄金购买粮食()
	{
		显示资源兑换(6);
	}

	public void 铜钱转换粮食()
	{
		显示资源兑换(7);
	}

	public void 粮食转换铜钱()
	{
		显示资源兑换(8);
	}

	private void 显示资源兑换(int 类型)
	{
		调整数量脚本对象.第几个玩家 = 全局变量.本机身份;
		调整数量脚本对象.调整类型 = 类型;
		调整数量脚本对象.gameObject.SetActive(value: true);
		调整数量脚本对象.显示说明文本();
	}
}
