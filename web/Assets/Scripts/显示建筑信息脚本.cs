using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using 玩家数据结构;
using Dwsg.Window3;

public class 显示建筑信息脚本 : MonoBehaviour
{
    public Image 头像对象;
    public Text 名字等级对象, 建筑效果对象, 封地产钱对象, 总产钱对象;
    public 建筑信息 建筑信息对象;
    public int 第几个封地, 第几个建筑;
    public void 显示建筑信息()
    {
        var p = FiefActions.Player(全局变量.本机身份); var f = FiefActions.Fief(全局变量.本机身份, 第几个封地);
        if (p == null || f == null || 建筑信息对象 == null) return;
        var b = 建筑信息对象;
        var layout = transform.Find("建筑信息布局");
        if (layout != null)
        {
            var effectText = layout.Find("建筑效果显示");
            var fiefText = layout.Find("建筑效果显示 (1)");
            var totalText = layout.Find("建筑效果显示 (2)");
            if (effectText != null) 建筑效果对象 = effectText.GetComponent<Text>();
            if (fiefText != null) 封地产钱对象 = fiefText.GetComponent<Text>();
            总产钱对象 = totalText == null ? null : totalText.GetComponent<Text>();
        }
        var portraits = b.类型 == 2 ? 全局变量.房屋头像资源表 : b.类型 == 3 ? 全局变量.农田头像资源表 : 全局变量.大厅头像资源表;
        int avatar = b.获取建筑头像索引();
        if (头像对象 != null && avatar < portraits.Length) 头像对象.sprite = portraits[avatar];
        if (名字等级对象 != null) 名字等级对象.text = b.获取建筑等级文本();
        string effect = b.类型 == 0 ? "基础铜钱产量 +" + (b.等级 * 15) + "/小时" :
            b.类型 == 2 ? "基础人口上限 +" + (b.等级 * 250 + p.科技信息.安置 * 40).ToString("0") : "基础粮食产量 +" + (b.等级 * 25);
        if (b.类型 == 3) effect += "/小时";
        if (建筑效果对象 != null) { 建筑效果对象.text = effect; 建筑效果对象.fontSize = 18; }
        double copper = f.建筑信息表.Where(x => x.类型 == 0).Sum(x => x.等级 * 15.0);
        copper += Mathf.Floor((float)(copper * p.科技信息.市场贸易 * .05000000074505806));
        double food = f.建筑信息表.Where(x => x.类型 == 3).Sum(x => x.等级 * 25.0);
        food += Mathf.Floor((float)(food * p.科技信息.种植技术 * .05000000074505806));
        if (封地产钱对象 != null) 封地产钱对象.text = b.类型 == 0 ? copper.ToString("0") + "/小时" : b.类型 == 3 ? food.ToString("0") + "/小时" : p.获取已占用人口().ToString("0") + "/" + p.获取人口上限().ToString("0");
        if (总产钱对象 != null) 总产钱对象.text = (b.类型 == 3 ? p.获取粮食产量() : p.获取铜钱产量()).ToString("0") + "/小时";
        string error = FiefActions.UpgradeError(f, 第几个建筑);
        foreach (var text in GetComponentsInChildren<Text>(true))
        {
            if (text.name == "建筑说明")
            {
                text.fontSize = 18;
                text.text = error ?? "升至" + (b.等级 + 1) + "级需要铜钱" + FiefActions.BuildingCost(b.等级, 2).ToString("0") + "、粮食" + FiefActions.BuildingCost(b.等级, 4).ToString("0") + "。";
            }
            else if (text.name == "封地产钱标题") text.text = b.类型 == 3 ? "【本封地粮食产量】" : b.类型 == 2 ? "【人口占用状态】" : "【本封地铜钱产量】";
            else if (text.name.StartsWith("封地总产钱标题") || text.name == "封地产钱标题 (1)") text.text = b.类型 == 3 ? "【粮食总产量】" : "【铜钱总产量】";
            else if (text.name == "建筑效果") text.text = b.类型 == 3 ? "粮食生产" : b.类型 == 2 ? "增加人口上限" : "行政与铜钱生产";
        }
    }
}
