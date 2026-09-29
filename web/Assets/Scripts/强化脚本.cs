using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using 玩家数据结构;
using 缺失界面.窗口4;

public class 强化脚本 : MonoBehaviour
{
    public Image 装备头像;
    public Text 装备名字属性品质显示;
    public Text 需要材料;
    public Text 强化效果;
    public Text 强化成功率;
    public 将领装备 装备对象;

    public void 显示指定装备()
    {
        var 玩家 = 军事缺口入口.当前玩家();
        if (装备对象 == null || 装备对象.装备信息 == null || 玩家 == null)
        {
            装备头像.enabled = false;
            装备名字属性品质显示.text = "请先选择已穿戴的装备。";
            需要材料.text = 强化效果.text = 强化成功率.text = "";
            return;
        }
        装备头像.enabled = true;
        装备头像.sprite = 装备对象.获取装备头像();
        装备名字属性品质显示.text = 装备对象.获取装备名字() + "+" + 装备对象.强化等级.ToString("0") + "(" + 装备对象.获取装备等级().ToString("0") + "级)\n" + 装备对象.获取装备加成文本() + "\n" + 装备对象.获取装备品质文本();
        string 材料 = 装备对象.获取装备强化材料名字();
        double 数量 = 玩家.背包道具列表.获取指定道具数量(材料);
        需要材料.text = 材料 + " " + 数量.ToString("0") + "/" + (装备对象.强化等级 + 1).ToString("0");
        强化效果.text = "强化效果：" + 装备对象.获取装备强化效果文本() + "↑" + 装备对象.获取装备强化加成();
        强化成功率.text = "成功率：" + (装备对象.获取装备强化成功率() / 100).ToString("0.##") + "%  保底：" + 装备对象.已强化次数.ToString("0") + "/" + 装备对象.获取装备强化保底次数();
    }

    private bool 尝试强化(out bool 成功, out string 说明)
    {
        成功 = false;
        var 玩家 = 军事缺口入口.当前玩家();
        var 检查 = 将领流程规则.检查装备(玩家, 装备对象);
        if (检查.成功 && 装备对象.强化等级 >= 30)
            检查 = 军事结果.拒绝(军事错误.达到上限, "装备已强化至30级。");
        if (检查.成功)
            检查 = 将领流程规则.支付材料(玩家, new Dictionary<string, int> {
                { 装备对象.获取装备强化材料名字(), (int)装备对象.强化等级 + 1 } });
        说明 = 检查.说明;
        if (!检查.成功) return false;
        if (Random.Range(1, 10000) < 装备对象.获取装备强化成功率())
        {
            装备对象.强化1级装备();
            成功 = true;
            说明 = "强化成功。";
        }
        else if (装备对象.已强化次数 >= 装备对象.获取装备强化保底次数())
        {
            装备对象.强化1级装备();
            成功 = true;
            说明 = "保底强化成功。";
        }
        else
        {
            装备对象.已强化次数++;
            说明 = "强化失败，保底次数已增加。";
        }
        return true;
    }

    public void 强化装备()
    {
        bool 成功;
        string 说明;
        尝试强化(out 成功, out 说明);
        显示指定装备();
        全局变量.提示类.显示信息(说明);
    }

    public void 批量强化()
    {
        int 次数 = 0, 成功数 = 0;
        string 说明 = "";
        for (int i = 0; i < 10; i++)
        {
            bool 成功;
            if (!尝试强化(out 成功, out 说明)) break;
            次数++;
            if (成功) 成功数++;
        }
        显示指定装备();
        全局变量.提示类.显示信息(次数 == 0 ? 说明 : "强化" + 次数 + "次，成功" + 成功数 + "次。" + (次数 < 10 ? 说明 : ""));
    }
}
