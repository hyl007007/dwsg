using UnityEngine;
using UnityEngine.UI;
using 缺失界面.窗口2;

public class 显示国库脚本 : MonoBehaviour
{
    public Text 铜钱显示文本;
    public Text 粮食显示文本;
    private InputField 捐献铜钱, 捐献粮食;
    private Text 可用资源, 操作反馈;
    private Button 捐献按钮;
    private string 打开时国家;
    private int 打开时角色 = -1;

    private void OnEnable()
    {
        打开时角色 = NationDataSource.Current.ActorId;
        var 概况 = transform.root.GetComponentInChildren<显示概况脚本>(true);
        打开时国家 = 概况 == null ? NationDataSource.Current.OwnNationCode : 概况.当前查看国号;
        安装捐献(); 捐献铜钱.text = 捐献粮食.text = ""; 操作反馈.text = "请输入要捐献的铜钱、粮食数量。";
        获取国家国库();
    }

    private void 安装捐献()
    {
        if (捐献按钮 != null) return;
        var ui = new NationUiFactory(transform.root);
        foreach (var 名称 in new[] { "铜钱", "铜钱描述", "粮食", "粮食描述", "铜钱背景", "粮食背景" })
        {
            var 项 = transform.Find(名称) as RectTransform; if (项 == null) continue;
            项.anchoredPosition = new Vector2(项.anchoredPosition.x, 名称.StartsWith("铜钱") ? 103 : 67);
        }
        foreach (var 名称 in new[] { "铜钱背景", "粮食背景" })
        { var 背景 = transform.Find(名称); if (背景 != null) 背景.gameObject.SetActive(false); }
        foreach (var 文本 in new[] { 铜钱显示文本, 粮食显示文本 })
        {
            if (文本 == null) continue;
            foreach (Image 背景 in 文本.GetComponentsInChildren<Image>(true)) 背景.gameObject.SetActive(false);
            设置正文(文本, ui.BodyColor);
        }
        foreach (var 名称 in new[] { "铜钱描述", "粮食描述" })
        {
            var 描述 = transform.Find(名称); if (描述 == null) continue;
            设置正文(描述.GetComponent<Text>(), NationUiFactory.Gold);
        }
        var 标题 = transform.Find("国库信息");
        if (标题 != null) 原界面文字样式.标题(标题.GetComponent<Text>());
        var 铜钱标题 = NationOriginalControls.Text(transform, "捐献铜钱标题", "捐献铜钱", 铜钱显示文本, -111, 15, 125, 30);
        var 粮食标题 = NationOriginalControls.Text(transform, "捐献粮食标题", "捐献粮食", 粮食显示文本, -111, -24, 125, 30);
        设置正文(铜钱标题, NationUiFactory.Gold); 设置正文(粮食标题, NationUiFactory.Gold);
        捐献铜钱 = NationOriginalControls.Amount(transform, "捐献铜钱输入", 铜钱显示文本, transform.Find("铜钱背景"), 83, 15);
        捐献粮食 = NationOriginalControls.Amount(transform, "捐献粮食输入", 粮食显示文本, transform.Find("粮食背景"), 83, -24);
        foreach (InputField 输入 in new[] { 捐献铜钱, 捐献粮食 })
        {
            if (ui.ApplyInputSkin((RectTransform)输入.transform))
            { var 背景 = 输入.GetComponent<Image>(); 背景.sprite = null; 背景.color = Color.clear; }
            设置正文(输入.textComponent, ui.BodyColor);
            var 占位 = 输入.placeholder as Text;
            if (占位 != null) { 设置正文(占位, ui.BodyColor); 占位.color *= new Color(1, 1, 1, .55f); }
        }
        可用资源 = NationOriginalControls.Text(transform, "本人可用资源", "", 铜钱显示文本, 15, -62.5f, 390, 44); 可用资源.fontSize = 16; 可用资源.lineSpacing = .85f;
        操作反馈 = NationOriginalControls.Text(transform, "捐献反馈", "", 铜钱显示文本, 15, -108, 390, 44); 操作反馈.fontSize = 16; 操作反馈.lineSpacing = .85f;
        捐献按钮 = ui.Button("确认捐献", transform, "捐献", 捐献国库);
        var 按钮区域 = 捐献按钮.GetComponent<RectTransform>(); 按钮区域.anchorMin = 按钮区域.anchorMax = new Vector2(.5f, .5f);
        按钮区域.anchoredPosition = new Vector2(133, -151); 按钮区域.sizeDelta = ui.ButtonSize;
        var 清空 = ui.Button("清空数量", transform, "清空", () => { 捐献铜钱.text = 捐献粮食.text = ""; 操作反馈.text = "未填写的资源按0计算。"; });
        var 清空区域 = 清空.GetComponent<RectTransform>(); 清空区域.anchorMin = 清空区域.anchorMax = new Vector2(.5f, .5f);
        清空区域.anchoredPosition = new Vector2(20, -151); 清空区域.sizeDelta = ui.ButtonSize;
    }

    private static void 设置正文(Text 文本, Color 颜色)
    {
        if (文本 == null) return;
        文本.fontSize = 18; 文本.fontStyle = FontStyle.Normal; 文本.color = 颜色;
        文本.resizeTextForBestFit = false; 文本.lineSpacing = 1; 文本.supportRichText = false;
        文本.horizontalOverflow = HorizontalWrapMode.Wrap; 文本.verticalOverflow = VerticalWrapMode.Truncate;
    }

    public void 获取国家国库()
    {
        var 数据 = NationDataSource.Current; var 国家 = 数据.ReadNation(打开时国家 ?? 数据.OwnNationCode);
        if (铜钱显示文本 != null) 铜钱显示文本.text = 国家 == null ? "无国家" : NationUiFactory.Amount(国家.Copper);
        if (粮食显示文本 != null) 粮食显示文本.text = 国家 == null ? "无国家" : NationUiFactory.Amount(国家.Grain);
        if (捐献按钮 != null)
        {
            bool 可操作 = 国家 != null && 国家.Code == 数据.OwnNationCode && 打开时角色 == 数据.ActorId;
            捐献按钮.interactable = 捐献铜钱.interactable = 捐献粮食.interactable = 可操作;
            var 玩家 = NationBasicActions.Current.Actor;
            可用资源.text = 玩家 == null || 玩家.财产信息 == null ? "当前角色资源未就绪。" :
                "可用铜钱 " + NationUiFactory.Amount(玩家.财产信息.铜钱) + "\n可用粮食 " + NationUiFactory.Amount(玩家.财产信息.粮食);
            if (!可操作) 操作反馈.text = 国家 == null ? "加入国家后可捐献铜钱、粮食。" : "请返回自己所属的国家操作。";
        }
        刷新官职();
    }

    public void 捐献国库()
    {
        if (捐献铜钱 == null || 捐献粮食 == null) return;
        var 结果 = NationBasicActions.Current.Donate(打开时国家, 捐献铜钱.text, 捐献粮食.text, 打开时角色);
        操作反馈.text = 结果.Message;
        if (结果.Success) 捐献铜钱.text = 捐献粮食.text = "";
        获取国家国库();
    }

    public void 领取俸禄()
    {
        var 数据 = NationDataSource.Current;
        var 概况 = transform.root.GetComponentInChildren<显示概况脚本>(true);
        var 结果 = 数据.ClaimSalary(概况 == null ? 数据.OwnNationCode : 概况.当前查看国号);
        if (全局变量.提示类 != null) 全局变量.提示类.显示信息(结果.Message);
        获取国家国库();
    }

    private void 刷新官职()
    {
        int 索引 = 全局变量.本机身份;
        if (索引 < 0 || 索引 >= 全局变量.所有玩家数据表.Count) return;
        var 玩家 = 全局变量.所有玩家数据表[索引]; if (玩家 == null || 玩家.基础信息 == null) return;
        var 国家 = NationDataSource.Current.ReadNation(玩家.基础信息.国家);
        玩家.基础信息.官职 = 国家 == null ? 玩家数据结构.官职信息.平民 : NationSalaryRules.Office(玩家.基础信息.战功, 国家.KingId == 玩家.基础信息.ID);
    }
}
