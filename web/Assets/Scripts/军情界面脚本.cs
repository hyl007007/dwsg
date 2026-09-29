using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using 玩家数据结构;
using 缺失界面.窗口4;
using Dwsg.Window3;

public class 军情界面脚本 : MonoBehaviour
{
    public GameObject 战斗地图列表;
    public GameObject 军情列表;
    public Text 页数显示;
    public AudioSource 背景音乐对象;
    public AudioClip 山贼背景音乐;
    public AudioClip 城池背景音乐;

    private int 第几页军情;
    private int 总页数 = 1;
    private long 上次刷新 = -1;
    private readonly List<军情信息> 可见军情 = new List<军情信息>();
    private readonly List<军情信息> 行快照 = new List<军情信息>();
    private Text 空状态;
    private string 分类 = "全部军情";
    private bool 已绑定;
    private Button 进入按钮;
    private Text 进入按钮文字;
    private sealed class 军情标题
    {
        public string 原文;
        public string 简文;
        public float 宽度 = -1;
    }
    private readonly Dictionary<Text, 军情标题> 完整军情标题 = new Dictionary<Text, 军情标题>();

    private void OnEnable()
    {
        if (!已绑定)
        {
            已绑定 = true;
            var 标签组 = transform.Find("军情信息切换");
            if (标签组 != null)
                foreach (Transform 标签 in 标签组)
                {
                    var 勾选 = 标签.GetComponent<Toggle>();
                    if (勾选 == null) continue;
                    string 名 = 标签.name;
                    if (勾选.isOn) 分类 = 名;
                    勾选.onValueChanged.AddListener(选中 => {
                        if (!选中) return;
                        分类 = 名;
                        第几页军情 = 0;
                        清除选择();
                        显示军情列表();
                    });
                }
            var 进入 = transform.Find("将领界面操作/进入");
            进入按钮 = 进入 != null ? 进入.GetComponent<Button>() : null;
            适配进入按钮文字();
            foreach (Transform 行 in 军情列表.transform)
            {
                var 勾选 = 行.GetComponent<Toggle>();
                if (勾选 != null) 勾选.onValueChanged.AddListener(_ => 刷新操作按钮());
            }
            var 背景 = transform.Find("将领列表背景");
            if (背景 != null)
            {
                var 样式 = new 军事界面样式(transform);
                空状态 = 样式.文本(背景, "军情空状态", "", Vector2.zero, Vector2.zero, 18);
                军事界面样式.拉伸(空状态.rectTransform, 16);
                空状态.alignment = TextAnchor.MiddleCenter;
            }
        }
        清除选择();
        显示军情列表();
    }

    private void 适配进入按钮文字()
    {
        if (进入按钮 == null) return;
        进入按钮文字 = 进入按钮.GetComponentInChildren<Text>(true);
        var 原字图 = 进入按钮.transform.Find("Image") as RectTransform;
        if (进入按钮文字 == null)
        {
            var 样式 = new 军事界面样式(transform);
            进入按钮文字 = 样式.文本(进入按钮.transform, "军情操作文字", "进入",
                原字图 != null ? 原字图.sizeDelta : new Vector2(46, 18), Vector2.zero, 15);
            var 框 = 进入按钮文字.rectTransform;
            if (原字图 != null)
            {
                框.anchorMin = 原字图.anchorMin; 框.anchorMax = 原字图.anchorMax; 框.pivot = 原字图.pivot;
                框.anchoredPosition = new Vector2(原字图.anchoredPosition.x, 0);
            }
            // 位图字的18高不足以容纳字体行框；仅扩展文字框，原73×39按钮不变。
            var 设置 = 进入按钮文字.GetGenerationSettings(new Vector2(1000, 0));
            float 行高 = 进入按钮文字.cachedTextGeneratorForLayout.GetPreferredHeight("返回中", 设置) / 进入按钮文字.pixelsPerUnit;
            框.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Max(框.rect.height, Mathf.Ceil(行高) + 4));
        }
        if (原字图 != null && (进入按钮.targetGraphic == null || 原字图.gameObject != 进入按钮.targetGraphic.gameObject)) 原字图.gameObject.SetActive(false);
        进入按钮文字.raycastTarget = false;
        进入按钮文字.alignment = TextAnchor.MiddleCenter;
        进入按钮文字.horizontalOverflow = HorizontalWrapMode.Overflow;
        原界面文字样式.按钮(进入按钮文字);
    }

    private void 清除选择()
    {
        foreach (Transform 行 in 军情列表.transform)
        {
            if (行.childCount > 5) 行.GetChild(5).gameObject.SetActive(false);
            var 勾选 = 行.GetComponent<Toggle>();
            if (勾选 != null) 勾选.SetIsOnWithoutNotify(false);
        }
    }

    public void 列表左翻页()
    {
        if (第几页军情 > 0) { 第几页军情--; 清除选择(); 显示军情列表(); }
    }
    public void 列表右翻页()
    {
        if (第几页军情 < 总页数 - 1) { 第几页军情++; 清除选择(); 显示军情列表(); }
    }

    private bool 我方军情(军情信息 情, 玩家数据 玩家)
    {
        if (玩家 == null || 情.队列将领列表 == null) return false;
        foreach (var 地 in 玩家.封地信息表)
            foreach (var 将 in 情.队列将领列表)
                if (将 != null && (地.将领信息表.Contains(将) ||
                    (将.详细信息 != null && 将.详细信息.身份 == 全局变量.本机身份 && 地.将领信息表.Exists(x => x != null && x.ID == 将.ID)))) return true;
        return false;
    }

    public void 显示军情列表()
    {
        和平驻防规则.推进(TIME.getTime());
        军情信息 选中 = null;
        for (int i = 0; i < 行快照.Count && i < 军情列表.transform.childCount; i++)
            if (军情列表.transform.GetChild(i).GetChild(5).gameObject.activeSelf) { 选中 = 行快照[i]; break; }
        可见军情.Clear();
        var 玩家 = 军事缺口入口.当前玩家();
        if (全局变量.军情列表 != null)
            foreach (var 情 in 全局变量.军情列表)
            {
                if (情 == null || 情.队列将领列表 == null || 情.队列将领列表.Count == 0) continue;
                bool 我方 = 我方军情(情, 玩家);
                bool 驻防 = 和平驻防规则.是驻防军情(情);
                var 城 = 情.战场类型 == 1 ? 所有城池界面脚本.根据坐标获取指定城池(情.坐标x, 情.坐标y) : null;
                bool 警情 = !驻防 && !我方 && 城 != null && 玩家 != null && 玩家.基础信息 != null &&
                    !string.IsNullOrEmpty(玩家.基础信息.国家) && 城.国家 == 玩家.基础信息.国家;
                bool 匹配 = 分类 == "出征军情" ? 我方 && !驻防 && 情.身份 != 78 : 分类 == "驻守军情" ? 我方 && (驻防 || 情.身份 == 78)
                    : 分类 == "警情军情" ? 警情 : 我方 || 警情;
                if (匹配) 可见军情.Add(情);
            }
        总页数 = Mathf.Max(1, Mathf.CeilToInt(可见军情.Count / 5f));
        第几页军情 = Mathf.Clamp(第几页军情, 0, 总页数 - 1);
        页数显示.text = (第几页军情 + 1) + "/" + 总页数;
        行快照.Clear();
        完整军情标题.Clear();
        int 显示数 = 0;
        for (int i = 0; i < 5 && i < 军情列表.transform.childCount; i++)
        {
            var 行 = 军情列表.transform.GetChild(i);
            int 索引 = 第几页军情 * 5 + i;
            bool 存在 = 索引 < 可见军情.Count;
            行.gameObject.SetActive(存在);
            行.GetChild(3).gameObject.SetActive(false);
            行.GetChild(4).gameObject.SetActive(false);
            行.GetChild(5).gameObject.SetActive(false);
            if (!存在) continue;
            var 情 = 可见军情[索引];
            行快照.Add(情);
            行.GetChild(5).gameObject.SetActive(ReferenceEquals(选中, 情));
            var 名字 = 行.GetChild(1).GetComponent<Text>();
            var 将 = 情.队列将领列表[0];
            string 将名 = 将 != null && 将.将领属性 != null ? 将.将领属性.初始属性.名字 : "将领";
            string 目标 = "(" + 情.坐标x + "," + 情.坐标y + ")";
            if (情.战场类型 == 0)
            {
                var 贼 = 附近山贼.获取指定坐标的山贼(情.坐标x, 情.坐标y);
                目标 = (贼 != null ? 贼.等级.ToString("0") + "级山贼" : "山贼目标已变更") + 目标;
            }
            else if (资源点战斗适配.是资源军情(情))
            {
                var 资源 = 情 as 资源点军情信息;
                var 点 = 资源点规则.本地.查询().Find(x => 资源 != null ? x.标识 == 资源.资源点标识 : x.坐标x == 情.坐标x && x.坐标y == 情.坐标y);
                目标 = (点 != null ? 点.类型.ToString() : "资源点目标已变更") + 目标;
            }
            else
            {
                var 城 = 所有城池界面脚本.根据坐标获取指定城池(情.坐标x, 情.坐标y);
                目标 = (城 != null ? 城.名称 : "城池目标已变更") + 目标;
            }
            名字.supportRichText = false;
            var 驻 = 情 as 驻防军情信息;
            string 用途 = "【" + (资源点战斗适配.是资源军情(情) ? "占领资源点" : 驻 != null ? 驻.阶段 == 驻防任务阶段.撤回 ? "驻防撤回" : "驻防" : 情.身份 == 78 ? "守方增援" : "出征") + "】";
            设置军情标题(名字, 用途 + 将名 + " · " + 目标, 用途 + 将名 + " · (" + 情.坐标x + "," + 情.坐标y + ")");
            var 状态 = 行.GetChild(2).GetComponent<Text>();
            if (驻 != null)
            {
                状态.text = 和平驻防规则.状态说明(驻);
                if (驻.阶段 == 驻防任务阶段.前往 || 驻.阶段 == 驻防任务阶段.撤回)
                {
                    行.GetChild(3).gameObject.SetActive(true);
                    行.GetChild(3).GetComponent<Text>().text = "剩余 " + TIME.ToTimeFormat(System.Math.Max(0, 情.到达时间 - TIME.getTime()));
                }
                else if (驻.阶段 == 驻防任务阶段.参战) 行.GetChild(4).gameObject.SetActive(true);
            }
            else if (情.已进入战场)
            {
                状态.text = "战斗中";
                行.GetChild(4).gameObject.SetActive(true);
            }
            else if (情.到达时间 <= TIME.getTime()) 状态.text = 情 is 资源点军情信息 ? "已到达，等待战场" : "已到达，等待结算";
            else
            {
                状态.text = "行军中";
                行.GetChild(3).gameObject.SetActive(true);
                行.GetChild(3).GetComponent<Text>().text = "剩余 " + TIME.ToTimeFormat(情.到达时间 - TIME.getTime());
            }
            显示数++;
        }
        if (空状态 != null)
        {
            空状态.text = 分类 == "驻守军情" ? "当前没有驻防或守方增援军情。" : 分类 == "警情军情" ? "当前没有本国城池受到进攻的警情。"
                : 分类 == "出征军情" ? "当前没有出征军情，请先选择将领出征。" : "当前没有军情。";
            空状态.gameObject.SetActive(显示数 == 0);
        }
        刷新操作按钮();
        上次刷新 = TIME.getTime();
    }

    private void 刷新操作按钮()
    {
        if (进入按钮 == null) return;
        军情信息 情 = null;
        for (int i = 0; i < 行快照.Count && i < 军情列表.transform.childCount; i++)
            if (军情列表.transform.GetChild(i).GetChild(5).gameObject.activeSelf) { 情 = 行快照[i]; break; }
        var 驻 = 情 as 驻防军情信息;
        var 资源 = 情 as 资源点军情信息;
        if (进入按钮文字 != null) 进入按钮文字.text = 资源 != null ? 资源.已进入战场 ? "观战" : "撤回" : 驻 == null || 驻.阶段 == 驻防任务阶段.参战 ? "进入" : 驻.阶段 == 驻防任务阶段.撤回 ? "返回中" : "撤回";
        进入按钮.interactable = 情 != null && 全局变量.军情列表.Contains(情) && (资源 != null ? 资源.阶段 != 资源出征阶段.已结束 : 驻 != null ? 驻.阶段 != 驻防任务阶段.撤回 : 情.已进入战场);
    }

    public void 进入战场()
    {
        for (int i = 0; i < 行快照.Count; i++)
        {
            var 行 = 军情列表.transform.GetChild(i);
            if (!行.gameObject.activeSelf || !行.GetChild(5).gameObject.activeSelf) continue;
            var 情 = 行快照[i];
            进入指定军情(情);
            return;
        }
        全局变量.提示类.显示信息("请选择正在战斗的军情。");
    }

    public void 进入指定军情(军情信息 情)
    {
            if (情 == null) return;
            if (!全局变量.军情列表.Contains(情)) { 显示军情列表(); 全局变量.提示类.显示信息("军情已变更，请重新选择。"); return; }
            var 资源 = 情 as 资源点军情信息;
            if (资源 != null)
            {
                if (!资源.已进入战场)
                {
                    if (Dwsg.Network.GameNetwork.Enabled)
                    {
                        Dwsg.Combat.ResourceClient.Withdraw(资源, result => { if (this == null) return; 全局变量.提示类.显示信息(result.Message); 显示军情列表(); });
                        return;
                    }
                    全局变量.提示类.显示信息(资源点规则.本地.撤回(资源).Message);
                    显示军情列表();
                    return;
                }
                var 检查 = Dwsg.Network.GameNetwork.Enabled ? CityResult.Ok("") : 资源点规则.本地.校验军情(资源);
                if (!检查.Success) { 全局变量.提示类.显示信息(检查.Message); return; }
            }
            var 驻 = 情 as 驻防军情信息;
            if (驻 != null && 驻.阶段 != 驻防任务阶段.参战)
            {
                if (Dwsg.Network.GameNetwork.Enabled)
                {
                    Dwsg.Combat.PeaceGarrisonClientBridge.Withdraw(驻, 结果 => { if (this == null) return; 全局变量.提示类.显示信息(结果.说明); 显示军情列表(); });
                    return;
                }
                全局变量.提示类.显示信息(和平驻防规则.撤回(军事缺口入口.当前玩家(), 驻, TIME.getTime()).说明);
                显示军情列表();
                return;
            }
            if (!情.已进入战场) { 全局变量.提示类.显示信息("部队尚未进入战场。"); return; }
            foreach (Transform 地图 in 战斗地图列表.transform)
            {
                var 系统 = 地图.GetComponentInChildren<战斗系统>(true);
                if (系统 == null || 情.坐标x != 系统.坐标x || 情.坐标y != 系统.坐标y || 情.战场类型 != 系统.战场类型) continue;
                if (资源 != null && (Dwsg.Network.GameNetwork.Enabled ? 系统.服务器战场ID != 资源.服务器战场ID : !资源点战斗适配.匹配军情(系统, 资源))) continue;
                if (驻 != null && Dwsg.Network.GameNetwork.Enabled && 系统.服务器战场ID != 驻.服务器战场ID) continue;
                var 界面 = 全局变量.战斗界面UI对象.GetComponent<战斗界面UI脚本>();
                界面.准备战场();
                全局变量.战斗地图相机.transform.SetParent(地图);
                全局变量.战斗地图相机.transform.localPosition = new Vector3(0, 0, -10);
                界面.战斗地图对象 = 地图.gameObject;
                界面.获取脚本对象();
                界面.开始显示兵力 = true;
                全局变量.主相机.SetActive(false);
                全局变量.战斗地图相机.SetActive(true);
                全局变量.主界面UI对象.SetActive(false);
                全局变量.战斗界面UI对象.SetActive(true);
                全局变量.大地图布局对象.SetActive(false);
                全局变量.封地布局对象.SetActive(false);
                系统.正在观战 = true;
                var 主界面 = 全局变量.主界面UI对象.GetComponent<主界面UI脚本>();
                if (主界面 != null && 主界面.背景音乐对象 != null) 主界面.背景音乐对象.Stop();
                var 音乐 = 界面.GetComponent<AudioSource>();
                if (音乐 == null) 音乐 = 界面.gameObject.AddComponent<AudioSource>();
                if (主界面 != null && 主界面.背景音乐对象 != null) 音乐.mute = 主界面.背景音乐对象.mute;
                音乐.loop = true;
                音乐.clip = 系统.战场类型 == 1 ? 城池背景音乐 : 山贼背景音乐;
                音乐.Play();
                return;
            }
            全局变量.提示类.显示信息("战场已结束或尚未载入。");
    }

    private void FixedUpdate()
    {
        if (TIME.getTime() != 上次刷新) 显示军情列表();
    }

    private void 设置军情标题(Text 名字, string 原文, string 简文)
    {
        // 原行高62；标题留足字体行高，下方仍显示状态及倒计时。
        var 框 = 名字.rectTransform;
        框.anchorMin = new Vector2(0, 1);
        框.anchorMax = new Vector2(1, 1);
        框.pivot = new Vector2(.5f, 1);
        框.anchoredPosition = new Vector2(0, -3);
        框.sizeDelta = new Vector2(-24, Mathf.Max(28, 名字.fontSize * 名字.lineSpacing + 12));
        名字.horizontalOverflow = HorizontalWrapMode.Wrap;
        名字.verticalOverflow = VerticalWrapMode.Truncate;
        名字.text = 原文;
        完整军情标题[名字] = new 军情标题 { 原文 = 原文, 简文 = 简文 };
    }

    private void LateUpdate()
    {
        if (完整军情标题.Count == 0) return;
        bool 需要测量 = false;
        foreach (var 项 in 完整军情标题)
            if (项.Key && 项.Key.gameObject.activeInHierarchy && !Mathf.Approximately(项.Value.宽度, 项.Key.rectTransform.rect.width))
            { 需要测量 = true; break; }
        if (!需要测量) return;
        // OnEnable和分辨率切换时，先完成Canvas布局再测量；不以零尺寸破坏源文。
        Canvas.ForceUpdateCanvases();
        foreach (var 项 in 完整军情标题)
        {
            var 字 = 项.Key;
            if (!字 || !字.gameObject.activeInHierarchy) continue;
            float 宽 = 字.rectTransform.rect.width;
            if (宽 <= 0 || Mathf.Approximately(项.Value.宽度, 宽)) continue;
            项.Value.宽度 = 宽;
            字.text = 项.Value.原文;
            // 先省略目标名称；简文中的将领名仍可能超长，需再次测宽。
            if (字.preferredWidth > 宽) 字.text = 项.Value.简文;
            截断军情标题(字, 宽);
        }
    }

    private static void 截断军情标题(Text 字, float 宽)
    {
        if (字.preferredWidth <= 宽) return;
        string 文本 = 字.text;
        int 分隔 = 文本.LastIndexOf(" · (", System.StringComparison.Ordinal);
        string 前文 = 分隔 >= 0 ? 文本.Substring(0, 分隔) : 文本;
        string 坐标 = 分隔 >= 0 ? 文本.Substring(分隔) : "";
        字.text = "…" + 坐标;
        if (字.preferredWidth > 宽)
        {
            坐标 = "";
            字.text = "…";
            if (字.preferredWidth > 宽) { 字.text = ""; return; }
        }
        // 只截断显示副本，原文和简文保存在标题记录中；窗口变宽时可恢复。
        int 左 = 0, 右 = 前文.Length, 保留 = 0;
        while (左 <= 右)
        {
            int 中 = 左 + (右 - 左) / 2;
            int 长度 = 中;
            if (长度 > 0 && char.IsHighSurrogate(前文[长度 - 1])) 长度--;
            字.text = 前文.Substring(0, 长度) + "…" + 坐标;
            if (字.preferredWidth <= 宽) { 保留 = 长度; 左 = 中 + 1; }
            else 右 = 中 - 1;
        }
        字.text = 前文.Substring(0, 保留) + "…" + 坐标;
    }
}
