using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using 缺失界面.窗口2;

public class 显示建国城池列表 : MonoBehaviour
{
    public GameObject 显示列表对象;
    public 建国脚本 建国脚本对象;
    private readonly List<城池信息库类> 要显示的列表 = new List<城池信息库类>();
    private 城池信息库类 选中城池;
    private int 打开时角色 = -1;
    private Text 状态, 空状态;
    private NationOriginalControls.ListMetrics 行尺寸;
    private readonly Dictionary<Toggle, UnityAction<bool>> 选择监听 = new Dictionary<Toggle, UnityAction<bool>>();
    private readonly Dictionary<Button, UnityAction> 按钮监听 = new Dictionary<Button, UnityAction>();

    private void OnEnable() { 打开时角色 = NationDataSource.Current.ActorId; 选中城池 = null; 配置底栏(); 刷新显示(); }

    private void 配置底栏()
    {
        var 根 = transform as RectTransform;
        var 底框 = transform.Find("通用小弹窗背景/通用背景边框") as RectTransform;
        var 确定区域 = transform.Find("确定") as RectTransform;
        var 返回区域 = transform.Find("返回") as RectTransform;
        if (根 == null || 底框 == null || 确定区域 == null || 返回区域 == null) return;
        var 确定位置 = 确定区域.anchoredPosition;
        确定区域.anchorMin = 确定区域.anchorMax = new Vector2(.5f, .5f);
        确定区域.anchoredPosition = 确定位置;
        float 中线 = transform.InverseTransformPoint(底框.TransformPoint(底框.rect.center)).x - 根.rect.center.x;
        返回区域.anchorMin = 返回区域.anchorMax = 确定区域.anchorMax;
        返回区域.anchoredPosition = new Vector2(2 * 中线 - 确定位置.x, 确定位置.y);
    }

    public void 刷新显示()
    {
        if (显示列表对象 == null || 显示列表对象.transform.childCount == 0) return;
        var 列表 = 显示列表对象.transform; var 模板 = 列表.GetChild(0).gameObject;
        清理选择监听();
        if (打开时角色 != NationDataSource.Current.ActorId) { 打开时角色 = NationDataSource.Current.ActorId; 选中城池 = null; }
        if (行尺寸 == null) 行尺寸 = new NationOriginalControls.ListMetrics(模板.transform);
        var 文本模板 = 模板.transform.GetChild(2).GetComponent<Text>();
        if (状态 == null)
        {
            NationOriginalControls.ReserveListStatus(列表);
            状态 = NationOriginalControls.Text(transform, "选择国都提示", "", 文本模板, 15, -148, 394, 54); 状态.fontSize = 16;
            空状态 = NationOriginalControls.Empty(列表, 文本模板);
        }
        要显示的列表.Clear();
        foreach (var 城池 in 全局变量.所有城池列表) if (NationBasicActions.Current.CanUseCapital(城池)) 要显示的列表.Add(城池);
        for (int i = 0; i < 要显示的列表.Count; i++)
        {
            var 城池 = 要显示的列表[i]; var 行 = i < 列表.childCount ? 列表.GetChild(i).gameObject : Instantiate(模板, 列表, false); 行.SetActive(true);
            var 图标 = 行.transform.GetChild(1).GetComponent<Image>();
            if (图标 != null && 全局变量.城池规模头像资源表 != null && 城池.规模 >= 0 && 城池.规模 < 全局变量.城池规模头像资源表.Length) 图标.sprite = 全局变量.城池规模头像资源表[城池.规模];
            var 名称 = 行.transform.GetChild(2).GetComponent<Text>(); 名称.text = 城池.名称; NationOriginalControls.Fit(名称);
            var 规模 = 行.transform.GetChild(3).GetComponent<Text>(); 规模.text = "规模：" + 城池.获取规模名称();
            var 坐标 = 行.transform.GetChild(4).GetComponent<Text>(); 坐标.text = "坐标（" + 城池.坐标x + "，" + 城池.坐标y + "）";
            NationOriginalControls.FitListRow(行.transform, 行尺寸);
            绑定选择(行.transform, 城池);
        }
        for (int i = 要显示的列表.Count; i < 列表.childCount; i++) 列表.GetChild(i).gameObject.SetActive(false);
        NationOriginalControls.FitListGrid(列表, 行尺寸);
        if (!要显示的列表.Contains(选中城池)) 选中城池 = null;
        空状态.gameObject.SetActive(要显示的列表.Count == 0); 空状态.text = "暂无可作为国都的城池。\n需拥有未交战的县城以上城池，现有国都除外。";
        状态.text = 选中城池 == null ? "请选择一座城池作为国都。\n确认建国时会再次检查城池归属。" :
            "已选择：" + 选中城池.名称 + "（" + 选中城池.坐标x + "，" + 选中城池.坐标y + "）"; 刷新选择();
        var 确定 = transform.Find("确定"); if (确定 != null && 确定.GetComponent<Button>() != null) 确定.GetComponent<Button>().interactable = 要显示的列表.Count > 0;
    }

    private void 选择城池(城池信息库类 城池)
    {
        选中城池 = 城池; 刷新选择(); 状态.text = "已选择：" + 城池.名称 + "（" + 城池.坐标x + "，" + 城池.坐标y + "）";
    }

    private void 清理选择监听()
    {
        // 只移除本列表添加的监听，保留原 Toggle 的高亮和其他业务回调。
        foreach (var 项 in 选择监听) if (项.Key != null) 项.Key.onValueChanged.RemoveListener(项.Value);
        foreach (var 项 in 按钮监听) if (项.Key != null) 项.Key.onClick.RemoveListener(项.Value);
        选择监听.Clear(); 按钮监听.Clear();
    }

    private void 绑定选择(Transform 行, 城池信息库类 城池)
    {
        var 勾选 = 行.GetComponent<Toggle>();
        if (勾选 != null)
        {
            UnityAction<bool> 监听 = 选中 => { if (选中) 选择城池(城池); };
            选择监听.Add(勾选, 监听); 勾选.onValueChanged.AddListener(监听);
            return;
        }
        var 按钮 = 行.GetComponent<Button>(); if (按钮 == null) return;
        UnityAction 点击 = () => 选择城池(城池);
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
        foreach (var 勾选组 in 组) 勾选组.SetAllTogglesOff(false);
        for (int i = 0; i < 列表.childCount; i++)
        {
            var 行 = 列表.GetChild(i); bool 选中 = i < 要显示的列表.Count && 要显示的列表[i] == 选中城池;
            var 勾选 = 行.GetComponent<Toggle>(); if (勾选 != null) 勾选.SetIsOnWithoutNotify(选中);
            行.GetChild(5).gameObject.SetActive(选中);
        }
    }

    public void 确定选择城池()
    {
        if (打开时角色 != NationDataSource.Current.ActorId || !NationBasicActions.Current.CanUseCapital(选中城池))
        { 提示(选中城池 == null ? "请先选择一座城池。" : "角色或城池归属已变化，请重新选择。"); return; }
        if (建国脚本对象 == null || 建国脚本对象.国都对象 == null) { 提示("建国页面未就绪，请返回重试。"); return; }
        建国脚本对象.国都城池信息 = 选中城池; 建国脚本对象.国都对象.text = 选中城池.名称;
        NationOriginalControls.Fit(建国脚本对象.国都对象); gameObject.SetActive(false);
    }

    private void 提示(string 内容) { if (状态 != null) 状态.text = 内容; if (全局变量.提示类 != null) 全局变量.提示类.显示信息(内容); }
}
