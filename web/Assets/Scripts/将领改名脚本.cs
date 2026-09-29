using UnityEngine;
using UnityEngine.UI;
using 玩家数据结构;
using 缺失界面.窗口4;

public class 将领改名脚本 : MonoBehaviour
{
    public Text 原将领名字;
    public Text 现将领名字;
    public 将领列表显示 将领列表显示脚本;
    public Text 输入名字对象;
    private 将领信息 目标将领;
    private 玩家数据 目标玩家;
    private 封地信息 目标封地;

    public void 准备改名(将领信息 将)
    {
        目标玩家 = 军事缺口入口.当前玩家();
        目标将领 = 将;
        目标封地 = null;
        if (目标玩家 != null)
            foreach (var 地 in 目标玩家.封地信息表)
                if (地.将领信息表.Contains(将)) { 目标封地 = 地; break; }
        原将领名字.supportRichText = 现将领名字.supportRichText = false;
        原将领名字.text = 将 != null ? 将.将领属性.初始属性.名字 : "";
        现将领名字.text = "";
        var 输入 = 输入名字对象.GetComponentInParent<InputField>();
        if (输入 != null) 输入.SetTextWithoutNotify("");
        else 输入名字对象.text = "";
    }

    private string 读取输入()
    {
        var 输入 = 输入名字对象.GetComponentInParent<InputField>();
        return 输入 != null ? 输入.text : 输入名字对象.text;
    }

    public void 更新输入结果() { 现将领名字.text = (读取输入() ?? "").Trim(); }

    public void 确定改名()
    {
        if (!ReferenceEquals(目标玩家, 军事缺口入口.当前玩家()))
        { 全局变量.提示类.显示信息("角色已变更，请重新打开改名。"); return; }
        var 结果 = 将领流程规则.改名(目标玩家, 目标封地, 目标将领, 读取输入());
        全局变量.提示类.显示信息(结果.说明);
        if (!结果.成功) return;
        gameObject.SetActive(false);
    }
}
