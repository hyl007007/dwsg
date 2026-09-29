using UnityEngine;
using UnityEngine.UI;

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
        if (double.IsNaN(购买数量) || double.IsInfinity(购买数量) ||
            购买数量 < 1 || 购买数量 > 100 || 购买数量 != System.Math.Truncate(购买数量))
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
        var 商品 = 全局商城库.获取指定名字的道具(道具名字);
        if (商品 == null || 全局道具库.获取指定名字的道具(道具名字) == null) return;
        double 单价 = 使用白银 ? 商品.白银售价 : 商品.黄金售价;
        if (单价 <= 0 || double.IsNaN(单价) || double.IsInfinity(单价))
        {
            全局变量.提示类.显示信息("该商品不支持此货币");
            return;
        }
        if (商品.限购数量 != -1 && 商品.限购数量 < 数量)
        {
            全局变量.提示类.显示信息("购买失败，超出购买限制");
            return;
        }
        var 玩家 = 全局变量.所有玩家数据表[全局变量.本机身份];
        double 金额 = 单价 * 数量;
        double 余额 = 使用白银 ? 玩家.财产信息.白银 : 玩家.财产信息.黄金;
        if (余额 < 金额 || double.IsNaN(余额))
        {
            全局变量.提示类.显示信息("余额不足");
            return;
        }
        int 新增格数 = 玩家.背包道具列表.获取添加道具所需格数(道具名字, 数量);
        if (新增格数 > 0 && 玩家.获取背包物品数量() + 新增格数 > 玩家.基础信息.背包容量上限)
        {
            全局变量.提示类.显示信息("购买失败，背包容量不足");
            return;
        }
        玩家.背包道具列表.添加道具(道具名字, 数量);
        if (使用白银) 玩家.财产信息.白银 -= 金额;
        else 玩家.财产信息.黄金 -= 金额;
        if (商品.限购数量 != -1) 商品.限购数量 -= 数量;
        全局变量.提示类.显示信息("购买成功!");
        刷新显示();
    }

    private void 卖出(bool 使用白银)
    {
        int 数量;
        if (!获取交易数量(out 数量)) return;
        var 商品 = 全局商城库.获取指定名字的道具(道具名字);
        if (商品 == null) return;
        double 单价 = 使用白银 ? 商品.白银售价 : 商品.黄金售价;
        if (单价 <= 0 || double.IsNaN(单价) || double.IsInfinity(单价))
        {
            全局变量.提示类.显示信息("该商品不支持此货币");
            return;
        }
        var 玩家 = 全局变量.所有玩家数据表[全局变量.本机身份];
        if (!玩家.背包道具列表.扣除道具(道具名字, 数量))
        {
            全局变量.提示类.显示信息("卖出失败，数量不足");
            return;
        }
        double 金额 = 单价 * 数量;
        if (使用白银) 玩家.财产信息.白银 += 金额;
        else 玩家.财产信息.黄金 += 金额;
        全局变量.提示类.显示信息("卖出" + 道具名字 + 数量 + "个成功，获得" + (使用白银 ? "白银" : "黄金") + 金额);
        刷新显示();
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
