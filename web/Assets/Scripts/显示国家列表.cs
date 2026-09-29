using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using 缺失界面.窗口2;

public class 显示国家列表 : MonoBehaviour
{
    public GameObject 显示列表对象;
    public GameObject 建国按钮对象;
    public GameObject 更换加入对象;
    private readonly List<string> 显示国号 = new List<string>();
    private string 选中国号, 待确认国号;
    private int 打开时角色 = -1;
    private Text 状态, 空状态;
    private Button 加入按钮;
    private Button 查看按钮;
    private NationOriginalControls.ListMetrics 行尺寸;
    private readonly Dictionary<Toggle, UnityAction<bool>> 选择监听 = new Dictionary<Toggle, UnityAction<bool>>();
    private readonly Dictionary<Button, UnityAction> 按钮监听 = new Dictionary<Button, UnityAction>();

    private void OnEnable() { 选中国号 = 待确认国号 = null; 打开时角色 = NationDataSource.Current.ActorId; 刷新显示(); }

    public void 刷新显示()
    {
        if (显示列表对象 == null) return;
        var 列表 = 显示列表对象.transform;
        if (列表.childCount == 0) return;
        清理选择监听();
        var 模板 = 列表.GetChild(0).gameObject;
        if (行尺寸 == null) 行尺寸 = new NationOriginalControls.ListMetrics(模板.transform);
        var 文本模板 = 模板.transform.GetChild(2).GetComponent<Text>();
        if (状态 == null)
        {
            NationOriginalControls.ReserveListStatus(列表);
            状态 = NationOriginalControls.Text(transform, "选择国家提示", "", 文本模板, 15, -148, 394, 54); 状态.fontSize = 16;
            空状态 = NationOriginalControls.Empty(列表, 文本模板);
            安装查看按钮();
        }
        if (打开时角色 != NationDataSource.Current.ActorId) { 打开时角色 = NationDataSource.Current.ActorId; 选中国号 = 待确认国号 = null; }
        加入按钮 = 更换加入对象 == null ? null : 更换加入对象.GetComponent<Button>();
        var 建国按钮 = 建国按钮对象 == null ? null : 建国按钮对象.GetComponent<Button>();
        if (加入按钮 != null) 加入按钮.interactable = NationBasicActions.Current.CanFound;
        if (建国按钮 != null) 建国按钮.interactable = NationBasicActions.Current.CanFound;
        if (建国按钮对象 != null) 建国按钮对象.SetActive(true);
        if (更换加入对象 != null) 更换加入对象.SetActive(true);
        显示国号.Clear(); var 唯一国号 = new HashSet<string>();
        foreach (var 国家 in 全局变量.所有国家列表)
        {
            if (国家 == null || string.IsNullOrEmpty(国家.国号) || !唯一国号.Add(国家.国号)) continue;
            int 索引 = 显示国号.Count; 显示国号.Add(国家.国号);
            var 行 = 索引 < 列表.childCount ? 列表.GetChild(索引).gameObject : Instantiate(模板, 列表, false);
            行.SetActive(true);
            var 图标 = 行.transform.GetChild(1).GetComponent<Image>();
            if (图标 != null) 图标.sprite = 全局变量.所有国家头像资源表 == null ? 全局变量.自建国头像 : 国家.获取国家头像();
            var 名称 = 行.transform.GetChild(2).GetComponent<Text>(); 名称.text = 国家.国名 + "（" + 国家.国号 + "）"; NationOriginalControls.Fit(名称);
            int 数量 = 0; foreach (var 城池 in 全局变量.所有城池列表) if (城池 != null && 城池.国家 == 国家.国号) 数量++;
            var 城池数 = 行.transform.GetChild(3).GetComponent<Text>(); 城池数.text = "城池数量：" + 数量; NationOriginalControls.Fit(城池数);
            NationOriginalControls.FitListRow(行.transform, 行尺寸);
            绑定选择(行.transform, 国家.国号);
        }
        for (int i = 显示国号.Count; i < 列表.childCount; i++) 列表.GetChild(i).gameObject.SetActive(false);
        NationOriginalControls.FitListGrid(列表, 行尺寸);
        if (!显示国号.Contains(选中国号)) 选中国号 = null;
        空状态.gameObject.SetActive(显示国号.Count == 0); 空状态.text = "当前没有可加入的国家。\n拥有县城以上城池后，可使用建国入口。";
        if (查看按钮 != null) 查看按钮.interactable = 显示国号.Count > 0;
        刷新选择();
        var 玩家 = NationBasicActions.Current.Actor;
        if (状态 != null) 状态.text = 玩家 == null || 玩家.基础信息 == null ? "当前角色数据未就绪，暂时只能查看国家。" :
            !NationBasicActions.Current.CanFound ? "国王可查看国家；不能换国或再次建国。" : 选中国号 == null ? "请选择一个国家。\n换国会重置战功、归并基地，并归还其他城池。" : "已选择：" + 选中国号;
    }

    private void 安装查看按钮()
    {
        var 操作 = transform.Find("操作"); if (操作 == null) return;
        Transform 原国家页 = null;
        foreach (var 根 in gameObject.scene.GetRootGameObjects()) if (根.name == "国家信息界面UI") 原国家页 = 根.transform;
        if (原国家页 == null) return;
        var ui = new NationUiFactory(原国家页); 查看按钮 = ui.Button("查看国家", 操作, "查看", 查看国家);
        var 区域 = 查看按钮.GetComponent<RectTransform>(); 区域.anchorMin = 区域.anchorMax = new Vector2(.5f, .5f); 区域.sizeDelta = ui.ButtonSize; 区域.anchoredPosition = new Vector2(-52, 0);
        if (建国按钮对象 != null) ((RectTransform)建国按钮对象.transform).anchoredPosition = new Vector2(-156, 0);
        if (更换加入对象 != null) ((RectTransform)更换加入对象.transform).anchoredPosition = new Vector2(52, 0);
        var 返回 = 操作.Find("返回");
        if (返回 != null)
        {
            var 返回区域 = (RectTransform)返回;
            返回区域.anchorMin = 返回区域.anchorMax = new Vector2(.5f, .5f);
            返回区域.anchoredPosition = new Vector2(156, 0);
        }
    }

    public void 查看国家()
    {
        if (string.IsNullOrEmpty(选中国号)) { 提示("请先选择要查看的国家。"); return; }
        if (NationBasicActions.Current.FindNation(选中国号) == null) { 提示("所选国家已不存在，请刷新列表。"); return; }
        gameObject.SetActive(false); NationOriginalControls.ReturnToNation(this, 选中国号);
    }

    private void 选择国家(string 国号)
    {
        选中国号 = 国号; 待确认国号 = null; 刷新选择();
        var 国家 = NationBasicActions.Current.FindNation(国号);
        if (状态 != null) 状态.text = 国家 == null ? "该国已不存在，请刷新列表。" : "已选择：" + 国家.国名 + "（" + 国号 + "）\n换国会重置战功、归并基地，并归还其他城池。";
    }

    private void 清理选择监听()
    {
        // 只移除本列表添加的监听，保留原 Toggle 的高亮和其他业务回调。
        foreach (var 项 in 选择监听) if (项.Key != null) 项.Key.onValueChanged.RemoveListener(项.Value);
        foreach (var 项 in 按钮监听) if (项.Key != null) 项.Key.onClick.RemoveListener(项.Value);
        选择监听.Clear(); 按钮监听.Clear();
    }

    private void 绑定选择(Transform 行, string 国号)
    {
        var 勾选 = 行.GetComponent<Toggle>();
        if (勾选 != null)
        {
            UnityAction<bool> 监听 = 选中 => { if (选中) 选择国家(国号); };
            选择监听.Add(勾选, 监听); 勾选.onValueChanged.AddListener(监听);
            return;
        }
        var 按钮 = 行.GetComponent<Button>();
        if (按钮 == null) return;
        UnityAction 点击 = () => 选择国家(国号);
        按钮监听.Add(按钮, 点击); 按钮.onClick.AddListener(点击);
        界面窗口管理器.注册运行时按钮(按钮);
    }

    private void 刷新选择()
    {
        var 列表 = 显示列表对象.transform; var 组 = new HashSet<ToggleGroup>();
        for (int i = 0; i < 列表.childCount; i++)
        {
            var 勾选 = 列表.GetChild(i).GetComponent<Toggle>();
            if (勾选 != null && 勾选.group != null) 组.Add(勾选.group);
        }
        // 初开允许空选择，避免 ToggleGroup 在 Start/OnEnable 自动补选一个不可见行。
        // 用户选择后恢复单选约束，静默刷新不能触发换国选择回调。
        foreach (var 勾选组 in 组)
        {
            勾选组.allowSwitchOff = string.IsNullOrEmpty(选中国号);
            勾选组.SetAllTogglesOff(false);
        }
        for (int i = 0; i < 列表.childCount; i++)
        {
            var 行 = 列表.GetChild(i); bool 选中 = i < 显示国号.Count && 显示国号[i] == 选中国号;
            var 勾选 = 行.GetComponent<Toggle>(); if (勾选 != null) 勾选.SetIsOnWithoutNotify(选中);
            行.GetChild(4).gameObject.SetActive(选中);
        }
        NationOriginalControls.Caption(加入按钮, 待确认国号 != null ? "确认换国" : string.IsNullOrEmpty(NationDataSource.Current.OwnNationCode) ? "加入国家" : "更换国家", 状态);
    }

    public void 更换国家()
    {
        if (string.IsNullOrEmpty(选中国号)) { 提示("请先选择一个国家。"); return; }
        var 玩家 = NationBasicActions.Current.Actor;
        if (玩家 == null || 玩家.基础信息 == null) { 提示("当前角色数据未就绪。"); return; }
        if (打开时角色 != 玩家.基础信息.ID) { 提示("当前角色已变化，请重新打开列表。"); return; }
        if (!NationBasicActions.Current.CanFound) { 提示("国王不能换国。"); return; }
        if (玩家.基础信息.国家 == 选中国号) { 提示("已在本国。"); return; }
        bool 有城池 = false; foreach (var 城池 in 全局变量.所有城池列表) if (城池 != null && 城池.城主 == 玩家.基础信息.ID) 有城池 = true;
        if (待确认国号 != 选中国号 && (!string.IsNullOrEmpty(玩家.基础信息.国家) || (玩家.封地信息表 != null && 玩家.封地信息表.Count > 1) || 有城池))
        {
            待确认国号 = 选中国号; 刷新选择(); 提示("换国将重置战功、归并基地、归还其他城池。\n再次点击“确认换国”继续；返回可取消。"); return;
        }
        if (Dwsg.Network.GameNetwork.Enabled)
        {
            string 目标国家 = 选中国号;
            NationClient.Join(目标国家, response =>
            {
                if (this == null || 打开时角色 != NationDataSource.Current.ActorId) return;
                提示(response?.Message ?? "服务器未确认换国，请重试");
                if (response == null || response.Code != Dwsg.Shared.GameCodes.Ok) { 待确认国号 = null; 刷新选择(); return; }
                gameObject.SetActive(false); NationOriginalControls.ReturnToNation(this, 目标国家);
            });
            return;
        }
        var 结果 = NationBasicActions.Current.Join(选中国号, 打开时角色); 提示(结果.Message);
        if (!结果.Success) { 待确认国号 = null; 刷新选择(); return; }
        gameObject.SetActive(false); NationOriginalControls.ReturnToNation(this, 选中国号);
    }

    private void 提示(string 内容)
    {
        if (状态 != null) 状态.text = 内容;
        聊天系统.播报(内容);
    }
}
