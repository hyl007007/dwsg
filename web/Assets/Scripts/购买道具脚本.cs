using UnityEngine;
using UnityEngine.UI;
using Dwsg.Shared;
using Dwsg.Shared.Economy;
using Newtonsoft.Json.Linq;

public class 购买道具脚本 : MonoBehaviour
{
	public Image 道具头像对象;

	public Text 道具名字对象;

	public Text 道具说明对象;

	public Text 黄金售价对象;

	public GameObject 白银售价对象;

	public Text 黄金资产对象;

	public Text 白银资产对象;

	public Text 购买数量对象;

	public Slider 数量滑条对象;

	public GameObject 商城界面对象;

	public Text 限购数量;

	public string 道具名字;

	private double 购买数量;

	public void 刷新显示()
	{
		道具信息库类 道具信息库类 = 全局道具库.获取指定名字的道具(道具名字);
		if (道具信息库类 != null)
		{
			道具名字对象.text = 道具名字;
			道具说明对象.text = 道具信息库类.说明;
			道具头像对象.sprite = 全局道具库.获取道具头像(道具信息库类.头像);
			商品属性类 商品属性类 = 全局商城库.获取指定名字的道具(道具名字);
			黄金售价对象.text = 商品属性类.黄金售价.ToString();
			if (商品属性类.限购数量 != -1)
				限购数量.text = 商品属性类.限购数量.ToString();
			else
				限购数量.text = "库存: 无限量";
            白银售价对象.SetActive(value: false);
			if (商品属性类.白银售价 > 0.0)
			{
				白银售价对象.SetActive(value: true);
				白银售价对象.transform.GetChild(2).GetComponent<Text>().text = 商品属性类.白银售价.ToString();
			}
			int 本机身份 = 全局变量.本机身份;
			黄金资产对象.text = 全局变量.所有玩家数据表[本机身份].财产信息.黄金.ToString();
			白银资产对象.text = 全局变量.所有玩家数据表[本机身份].财产信息.白银.ToString();
			var 玩家 = 全局变量.所有玩家数据表[本机身份];
            double 黄金数量 = 商品属性类.黄金售价 > 0 ? System.Math.Floor(玩家.财产信息.黄金 / 商品属性类.黄金售价) : 0;
            double 白银数量 = 商品属性类.白银售价 > 0 ? System.Math.Floor(玩家.财产信息.白银 / 商品属性类.白银售价) : 0;
            double 可购买 = System.Math.Max(黄金数量, 白银数量);
            if (商品属性类.限购数量 != -1) 可购买 = System.Math.Min(可购买, 商品属性类.限购数量);
            double num = System.Math.Min(100, System.Math.Max(可购买, 玩家.背包道具列表.获取指定道具数量(道具名字)));
			数量滑条对象.wholeNumbers = true;
            num = System.Math.Max(0, System.Math.Floor(num));
            数量滑条对象.minValue = num > 0 ? 1f : 0f;
            数量滑条对象.maxValue = (float)num;
		}
	}

    private bool 获取交易数量(out int 数量)
    {
        数量 = 0;
        if (!ShopRules.ValidQuantity(购买数量))
        {
            全局变量.提示类.显示信息("请选择 1 到 100 个整数数量");
            return false;
        }
        数量 = (int)购买数量;
        return true;
    }

    private void 购买(bool 使用白银)
    {
        int 数量;
        if (!获取交易数量(out 数量)) return;
        if (请求服务器交易("shop.purchase", 使用白银, 数量)) return;
        var 商品 = 全局商城库.获取指定名字的道具(道具名字);
        var 配置 = 全局道具库.获取指定名字的道具(道具名字);
        if (商品 == null || 配置 == null) return;
        var 玩家 = 全局变量.所有玩家数据表[全局变量.本机身份];
        var 结果 = EconomyClient.PurchaseOffline(玩家, 商品, 配置.分类, 使用白银 ? "白银" : "黄金", 数量);
        全局变量.提示类.显示信息(结果.Message);
        if (结果.Code == GameCodes.Ok) 刷新显示();
    }

    private bool 请求服务器交易(string 类型, bool 使用白银, int 数量)
    {
        if (Dwsg.Network.GameNetwork.Enabled)
        {
            var 快照 = Dwsg.Network.GameNetwork.CurrentSnapshot;
            Dwsg.Network.GameNetwork.SendCommand(类型, new JObject
            {
                ["itemName"] = 道具名字,
                ["currency"] = 使用白银 ? "白银" : "黄金",
                ["quantity"] = 数量,
                ["catalogVersion"] = 快照?.PublicWorld.Value<int?>("商城配置版本") ?? 1
            }, 结果 =>
            {
                if (this == null) return;
                全局变量.提示类.显示信息(结果?.Message ?? "服务器未确认交易，请重试");
                if (Dwsg.Network.GameNetwork.HasRole)
                {
                    刷新显示();
                    刷新商城显示();
                }
            });
            return true;
        }
        return false;
    }

    private void 卖出(bool 使用白银)
    {
        int 数量;
        if (!获取交易数量(out 数量)) return;
        if (请求服务器交易("shop.sell", 使用白银, 数量)) return;
        var 商品 = 全局商城库.获取指定名字的道具(道具名字);
        var 配置 = 全局道具库.获取指定名字的道具(道具名字);
        if (商品 == null || 配置 == null) return;
        var 玩家 = 全局变量.所有玩家数据表[全局变量.本机身份];
        var 结果 = EconomyClient.SellOffline(玩家, 商品, 配置.分类, 使用白银 ? "白银" : "黄金", 数量);
        全局变量.提示类.显示信息(结果.Message);
        if (结果.Code == GameCodes.Ok) 刷新显示();
    }

    public void 黄金购买() { 购买(false); }
    public void 白银购买() { 购买(true); }
    public void 黄金卖出() { 卖出(false); }
    public void 白银卖出() { 卖出(true); }

    public void 打开购买界面()
    {
        gameObject.SetActive(true);
    }

	public void 改变购买数量()
	{
		购买数量对象.text = 数量滑条对象.value.ToString();
		购买数量 = 数量滑条对象.value;
	}

	public void 刷新商城显示()
	{
		if (商城界面对象 != null)
		{
			商城界面对象.GetComponent<显示商城列表>().刷新显示();
		}
	}
}
