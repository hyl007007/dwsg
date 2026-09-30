using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 只过渡原窗口的透明度和整体变换，不复制界面、不改变层级或布局尺寸。
[DisallowMultipleComponent]
[DefaultExecutionOrder(10000)]
public sealed class 界面窗口动画 : MonoBehaviour
{
    private const float 弹出秒数 = .16f, 收起秒数 = .12f, 最小比例 = .98f;
    private struct 原变换
    {
        public RectTransform 对象;
        public Vector2 位置, 中心偏移;
        public Vector3 比例;
    }
    private readonly List<原变换> 原布局 = new List<原变换>();
    private CanvasGroup 画布组;
    private float 原透明度, 当前比例 = 1, 起始透明度, 起始比例, 开始时间;
    private Vector2 布局尺寸;
    private bool 原交互, 待弹出, 过渡中;
    private Action 完成关闭;
    public bool 正在收起 { get { return 完成关闭 != null; } }

    // 可由启动环境或本地偏好关闭动效；不依赖战斗倍速，也不影响原关闭行为。
    public static bool 启用动画
    {
        get { return PlayerPrefs.GetInt("DWSG.WindowAnimations", 1) != 0 &&
            Environment.GetEnvironmentVariable("DWSG_REDUCE_MOTION") != "1"; }
    }

    private void Awake()
    {
        画布组 = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
    }

    private void OnEnable()
    {
        原透明度 = 画布组.alpha;
        原交互 = 画布组.interactable;
        待弹出 = 启用动画;
        if (待弹出) 画布组.alpha = 0;
    }

    private void 记录布局()
    {
        原布局.Clear();
        var 根 = transform as RectTransform;
        if (根 != null) 布局尺寸 = 根.rect.size;
        // 根布局器拥有子节点位置时只淡入淡出，避免抢写布局器驱动的属性。
        if (根 == null || GetComponent<LayoutGroup>() != null) return;
        foreach (Transform 子 in transform)
        {
            var 矩形 = 子 as RectTransform;
            if (矩形 == null) continue;
            // 全屏遮罩始终覆盖屏幕，不能随内容缩小而留下可穿透的边缘。
            if (矩形.anchorMin == Vector2.zero && 矩形.anchorMax == Vector2.one &&
                矩形.offsetMin == Vector2.zero && 矩形.offsetMax == Vector2.zero) continue;
            原布局.Add(new 原变换 { 对象 = 矩形, 位置 = 矩形.anchoredPosition,
                中心偏移 = (Vector2)矩形.localPosition - 根.rect.center, 比例 = 矩形.localScale });
        }
    }

    private void LateUpdate()
    {
        if (待弹出)
        {
            待弹出 = false;
            记录布局();
            起始透明度 = 0; 起始比例 = 最小比例; 开始时间 = Time.realtimeSinceStartup; 过渡中 = true;
            应用(起始透明度, 起始比例);
        }
        if (!过渡中) return;
        var 根 = transform as RectTransform;
        // 旋转或分辨率变化交还给原适配脚本；本次剩余过渡仅改变透明度。
        if ((根 != null && 根.rect.size != 布局尺寸) || 布局被更新()) 还原变换();
        float 进度 = Mathf.Clamp01((Time.realtimeSinceStartup - 开始时间) / (正在收起 ? 收起秒数 : 弹出秒数));
        float 缓动 = 1 - Mathf.Pow(1 - 进度, 3);
        应用(Mathf.Lerp(起始透明度, 正在收起 ? 0 : 原透明度, 缓动),
            Mathf.Lerp(起始比例, 正在收起 ? 最小比例 : 1, 缓动));
        if (进度 < 1) return;
        var 关闭 = 完成关闭;
        还原();
        if (关闭 != null) 关闭();
    }

    private void 应用(float 透明度, float 比例)
    {
        画布组.alpha = 透明度; 当前比例 = 比例;
        foreach (var 原 in 原布局)
        {
            if (原.对象 == null) continue;
            原.对象.localScale = 原.比例 * 比例;
            原.对象.anchoredPosition = 原.位置 + 原.中心偏移 * (比例 - 1);
        }
    }

    private void 还原变换()
    {
        foreach (var 原 in 原布局)
        {
            if (原.对象 == null) continue;
            // 原排版在动画中途刷新时，保留它写入的新值，不用旧快照覆盖。
            if (是动画比例(原)) 原.对象.localScale = 原.比例;
            if (是动画位置(原)) 原.对象.anchoredPosition = 原.位置;
        }
        原布局.Clear();
    }

    private bool 布局被更新()
    {
        foreach (var 原 in 原布局)
            if (原.对象 != null && (!是动画比例(原) || !是动画位置(原))) return true;
        return false;
    }

    // RectTransform 在画布缩放换算后有浮点尾差，不能将其当作业务重新排版。
    private bool 是动画位置(原变换 原)
    {
        return (原.对象.anchoredPosition - 原.位置 - 原.中心偏移 * (当前比例 - 1)).sqrMagnitude < .0001f;
    }
    private bool 是动画比例(原变换 原)
    {
        return (原.对象.localScale - 原.比例 * 当前比例).sqrMagnitude < .00000001f;
    }

    private void 还原()
    {
        if (画布组 != null) { 画布组.alpha = 原透明度; 画布组.interactable = 原交互; }
        还原变换();
        当前比例 = 1; 待弹出 = 过渡中 = false; 完成关闭 = null;
    }

    private void OnDisable() { 还原(); }

    private void 收起(Action 关闭)
    {
        if (正在收起) return;
        if (!启用动画 || !isActiveAndEnabled) { 关闭(); return; }
        if (待弹出) { 待弹出 = false; 记录布局(); }
        else if (!过渡中) 记录布局();
        完成关闭 = 关闭; 起始透明度 = 画布组.alpha; 起始比例 = 当前比例;
        开始时间 = Time.realtimeSinceStartup; 过渡中 = true;
        // 收起期间阻止重复执行按钮，但保留射线遮挡，避免点到背后的地图。
        画布组.interactable = false;
    }

    public static void 关闭(GameObject 对象) { 设置显示(对象, false); }

    public static void 设置显示(GameObject 对象, bool 显示)
    {
        if (对象 == null) return;
        var 动画 = 对象.GetComponent<界面窗口动画>();
        if (动画 == null) 动画 = 对象.GetComponentInChildren<界面窗口动画>();
        if (显示)
        {
            if (动画 != null && 动画.正在收起) 动画.还原();
            对象.SetActive(true);
            return;
        }
        if (!对象.activeInHierarchy) { 对象.SetActive(false); return; }
        // 画册等原窗口的关闭按钮挂在画布分组上，仍关闭原来的目标对象。
        if (动画 == null) 对象.SetActive(false);
        else 动画.收起(() => { if (对象 != null) 对象.SetActive(false); });
    }

    public static void 接入(GameObject 对象)
    {
        if (对象.GetComponent<界面窗口动画>() == null) 对象.AddComponent<界面窗口动画>();
    }

    // 原场景把 GameObject.SetActive(bool) 存在 UnityEvent 中；仅转接窗口目标，
    // 保留其他业务回调、原按钮与图标。不修改场景资产或用反射调用私有运行逻辑。
    public static void 接入按钮(Button 按钮)
    {
        转接(按钮.onClick);
    }

    public static void 接入事件(EventTrigger 事件)
    {
        foreach (var 项 in 事件.triggers)
        {
            转接(项.callback);
        }
    }

    private static void 转接(UnityEventBase 事件)
    {
        JObject 数据 = null;
        JArray 调用表 = null;
        int 首次修改 = -1;
        for (int i = 0; i < 事件.GetPersistentEventCount(); i++)
        {
            if (事件.GetPersistentListenerState(i) == UnityEventCallState.Off ||
                事件.GetPersistentMethodName(i) != "SetActive") continue;
            var 目标 = 事件.GetPersistentTarget(i) as GameObject;
            if (目标 == null || 目标.GetComponentInChildren<界面窗口动画>(true) == null) continue;
            if (数据 == null)
            {
                数据 = JObject.Parse(JsonUtility.ToJson(事件));
                调用表 = 数据["m_PersistentCalls"]?["m_Calls"] as JArray;
            }
            var 调用 = 调用表 != null && i < 调用表.Count ? 调用表[i] : null;
            if (调用 == null || 调用.Value<int>("m_Mode") != 6 || 调用["m_Arguments"]?["m_BoolArgument"] == null) continue;
            var 开关 = 目标.GetComponent<界面窗口开关>() ?? 目标.AddComponent<界面窗口开关>();
            调用["m_Target"] = new JObject { ["instanceID"] = 开关.GetInstanceID() };
            调用["m_TargetAssemblyTypeName"] = typeof(界面窗口开关).AssemblyQualifiedName;
            // 原位替换目标，保留 SetActive 的参数、启用状态及与业务回调的先后顺序。
            首次修改 = i;
        }
        if (首次修改 >= 0)
        {
            JsonUtility.FromJsonOverwrite(数据.ToString(), 事件);
            // 公共 API 使 Unity 清除之前缓存的持久回调；运行时监听不受影响。
            事件.SetPersistentListenerState(首次修改, 事件.GetPersistentListenerState(首次修改));
        }
    }
}
