using UnityEngine;
using UnityEngine.UI;
using 缺失界面.窗口2;

public class 国家科技 : MonoBehaviour
{
    public Text 攻击科技等级对象;
    public Text 防御科技等级对象;
    public Text 资源科技等级对象;
    public GameObject 概况脚本;
    private int 打开时角色 = -1;
    private string 打开时国家;
    private Text 消耗说明;

    private void OnEnable()
    {
        打开时角色 = NationDataSource.Current.ActorId;
        var 概况 = 概况脚本 == null ? null : 概况脚本.GetComponent<显示概况脚本>();
        打开时国家 = 概况 == null ? NationDataSource.Current.OwnNationCode : 概况.当前查看国号;
        刷新显示信息();
    }

    public void 刷新显示信息()
    {
        var 数据 = NationBasicActions.Current;
        var 国家 = 数据.FindNation(打开时国家 ?? NationDataSource.Current.OwnNationCode);
        设置(攻击科技等级对象, 国家 == null ? "无国家" : NationDataSource.Number(国家.攻击科技));
        设置(防御科技等级对象, 国家 == null ? "无国家" : NationDataSource.Number(国家.防御科技));
        设置(资源科技等级对象, 国家 == null ? "无国家" : NationDataSource.Number(国家.资源科技));
        if (消耗说明 == null)
        {
            var ui = new NationUiFactory(transform.root);
            var 标题模板 = transform.root.Find("官职说明/官职信息") as RectTransform;
            if (标题模板 != null)
            {
                var 标题 = ui.Text("国家科技标题", transform, "国家科技", 18);
                NationUiFactory.CopyRect(标题模板, 标题.rectTransform);
                标题.alignment = TextAnchor.MiddleCenter; 原界面文字样式.标题(标题);
            }
            消耗说明 = NationOriginalControls.Text(transform, "升级消耗", "", 攻击科技等级对象, 15, -120, 400, 62);
            消耗说明.color = ui.BodyColor;
            foreach (var 名称 in new[] { "攻击", "防御", "资源" })
            {
                var 行 = transform.Find(名称) as RectTransform; if (行 == null) continue;
                行.anchoredPosition = new Vector2(17, 名称 == "攻击" ? 88 : 名称 == "防御" ? 18 : -52);
                行.sizeDelta = new Vector2(410, 65);
                foreach (Transform 子项 in 行)
                {
                    var 文本 = 子项.GetComponent<Text>(); if (文本 == null) continue;
                    ((RectTransform)子项).sizeDelta = new Vector2(((RectTransform)子项).sizeDelta.x, 58);
                    文本.fontSize = 18; 文本.fontStyle = FontStyle.Normal; 文本.lineSpacing = 1;
                    文本.color = 子项.name == "标题" ? NationUiFactory.Gold : ui.BodyColor;
                    文本.resizeTextForBestFit = false;
                }
            }
        }
        var 消耗 = 数据.TechnologyCost(国家 == null ? "" : 国家.国号);
        消耗说明.text = 国家 == null ? "加入国家后可升级科技。" : 消耗 == null ? "科技数据异常，请返回刷新。" :
            "每次提升1级 · 黄金 " + NationUiFactory.Amount(消耗.Gold) + "\n粮食 " + NationUiFactory.Amount(消耗.Grain) + " · 铜钱 " + NationUiFactory.Amount(消耗.Copper);
        foreach (var 按钮 in GetComponentsInChildren<Button>(true))
            if (按钮.transform.parent.name == "攻击" || 按钮.transform.parent.name == "防御" || 按钮.transform.parent.name == "资源")
                按钮.interactable = 国家 != null && 国家.国号 == NationDataSource.Current.OwnNationCode && 打开时角色 == NationDataSource.Current.ActorId && 消耗 != null;
        var 概况 = 概况脚本 == null ? null : 概况脚本.GetComponent<显示概况脚本>();
        if (概况 != null) 概况.刷新显示();
    }

    public void 升级攻击科技() { 提升科技("攻击"); }
    public void 升级防御科技() { 提升科技("防御"); }
    public void 升级资源科技() { 提升科技("资源"); }

    private void 提升科技(string 类型)
    {
        if (Dwsg.Network.GameNetwork.Enabled)
        {
            if (打开时国家 != NationDataSource.Current.OwnNationCode || 打开时角色 != NationDataSource.Current.ActorId) return;
            NationClient.Research(类型 + "科技", result =>
            {
                if (this == null) return;
                if (全局变量.提示类 != null) 全局变量.提示类.显示信息(result.Message);
                刷新显示信息();
            });
            return;
        }
        var 结果 = NationBasicActions.Current.UpgradeTechnology(打开时国家 ?? NationDataSource.Current.OwnNationCode, 类型, 打开时角色);
        if (全局变量.提示类 != null) 全局变量.提示类.显示信息(结果.Message);
        刷新显示信息();
    }

    private static void 设置(Text 文本, string 内容)
    {
        if (文本 == null) return;
        文本.text = 内容; 文本.supportRichText = false; 文本.horizontalOverflow = HorizontalWrapMode.Wrap;
        文本.verticalOverflow = VerticalWrapMode.Truncate; 文本.resizeTextForBestFit = false;
    }
}
