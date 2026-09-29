using UnityEngine;
using UnityEngine.UI;
using Dwsg.Window3;

public class 市场脚本 : MonoBehaviour
{
    public Toggle 购买资源选项, 转换资源选项;
    public Text 黄金数量, 白银数量, 铜钱购买提示, 粮食购买提示;
    public Text 铜钱数量, 粮食数量, 铜钱转换提示, 粮食转换提示;
    public double 铜钱单价, 粮食单价;
    public 调整数量脚本 调整数量脚本对象;
    private void OnEnable() { 刷新显示(); }
    public void 刷新显示()
    {
        var p = FiefActions.Player(全局变量.本机身份); if (p == null) return;
        var 报价 = Dwsg.Economy.MarketClient.GetQuote();
        铜钱单价 = Dwsg.Shared.Economy.MarketRules.Rate(报价, 5); 粮食单价 = Dwsg.Shared.Economy.MarketRules.Rate(报价, 6);
        if (黄金数量 != null) 黄金数量.text = p.财产信息.黄金.ToString("0.##");
        if (白银数量 != null) 白银数量.text = p.财产信息.白银.ToString("0.##");
        if (铜钱数量 != null) 铜钱数量.text = p.财产信息.铜钱.ToString("0.##");
        if (粮食数量 != null) 粮食数量.text = p.财产信息.粮食.ToString("0.##");
        if (铜钱购买提示 != null) 铜钱购买提示.text = 报价 == null ? "正在获取市场报价" : "1黄金=" + 铜钱单价.ToString("0") + "铜钱";
        if (粮食购买提示 != null) 粮食购买提示.text = 报价 == null ? "正在获取市场报价" : "1黄金=" + 粮食单价.ToString("0") + "粮食";
        if (铜钱转换提示 != null) 铜钱转换提示.text = "1铜钱=2.5粮食";
        if (粮食转换提示 != null) 粮食转换提示.text = "10粮食=3铜钱";
    }
    private void 打开交易(int type)
    {
        if (调整数量脚本对象 == null || FiefActions.Player(全局变量.本机身份) == null) return;
        刷新显示();
        调整数量脚本对象.第几个玩家 = 全局变量.本机身份;
        调整数量脚本对象.市场脚本对象 = this;
        调整数量脚本对象.调整类型 = type;
        调整数量脚本对象.显示调整界面(); 调整数量脚本对象.显示说明文本();
    }
    public void 黄金购买铜钱() { 打开交易(5); }
    public void 黄金购买粮食() { 打开交易(6); }
    public void 铜钱转换粮食() { 打开交易(7); }
    public void 粮食转换铜钱() { 打开交易(8); }
}
