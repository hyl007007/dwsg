using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using 玩家数据结构;
using 缺失界面.窗口4;
using Dwsg.Window3;


public class 选择出征将领 : MonoBehaviour
{
	private int 第几个玩家 = 全局变量.本机身份;

	private int 当前选中封地 = 全局变量.第几个封地;

	public int index;

	public int 山贼坐标x;

	public int 山贼坐标y;

	public int 城池坐标x;

	public int 城池坐标y;

	public GameObject 山贼信息对象;

	public GameObject 编队切换对象;

	public GameObject 编队将领对象;

	public Text 精准到达_时;

	public Text 精准到达_分;

	public Text 精准到达_秒;

	private List<GameObject> 所有列表将领对象 = new List<GameObject>();

	private List<返回将领索引> 已显示将领列表 = new List<返回将领索引>();

	private List<返回将领索引> 已选中将领列表 = new List<返回将领索引>();
    private readonly Dictionary<返回将领索引, 将领信息> 选择快照 = new Dictionary<返回将领索引, 将领信息>();
    private 玩家数据 选择玩家;
    private Text 空将领提示;
    private bool 和平驻防模式;
    private string 资源点标识;
    private long 资源选择载入号;
    private int 资源前城池x, 资源前城池y, 资源前Index;
    private int 资源目标x, 资源目标y;
    private Image 资源头像;
    private Sprite 资源前头像;
    private bool 资源前头像启用;
    private Text 派遣按钮文字;
    private string 原派遣文字;
    private GameObject 派遣按钮原图字;
    private bool 原派遣图字启用;

    private sealed class 出征标题副本
    {
        public string 原文;
        public Vector2 尺寸 = new Vector2(-1, -1);
    }
    private readonly Dictionary<Text, 出征标题副本> 出征标题原文 = new Dictionary<Text, 出征标题副本>();
    private readonly HashSet<RectTransform> 已适配出征装饰 = new HashSet<RectTransform>();
    private readonly Dictionary<Text, string> 驻防前详情文字 = new Dictionary<Text, string>();
    private GameObject 驻防原标题图字, 驻防精准到达布局;
    private Text 驻防界面标题, 驻防规则文字;
    private bool 已记录驻防外观, 原驻防标题启用, 原精准到达启用;
    private float 原驻防规则高度;
    private long 上次驻防信息刷新 = -1;
    private sealed class 资源布局副本
    {
        public Vector2 小锚, 大锚, 支点, 位置, 尺寸;
        public Text 字;
        public HorizontalWrapMode 换行;
        public VerticalWrapMode 溢出;
        public TextAnchor 对齐;
        public bool 富文本;
    }
    private readonly Dictionary<RectTransform, 资源布局副本> 资源原布局 = new Dictionary<RectTransform, 资源布局副本>();
    private readonly string[] 资源概况全文 = new string[3];
    private readonly Text[] 资源概况文字 = new Text[3];
    private RectTransform 资源概况容器, 资源规则视口, 资源规则内容;
    private Canvas 资源画布;
    private Vector2 上次资源概况尺寸 = new Vector2(-1, -1), 上次资源视口尺寸 = new Vector2(-1, -1);
    private float 上次资源画布比例 = -1;
    private bool 资源概况待更新, 资源规则待更新;
    private ScrollRect 资源说明滚动;
    private bool 原资源横向滚动, 原资源纵向滚动;
    private LayoutElement 资源内容高度;
    private bool 新增资源内容高度, 原资源内容高度启用;
    private float 原资源内容首选高度;
    private int 原资源内容布局优先级;

    public void 设置和平驻防模式(bool 驻防)
    {
        更新和平驻防展示(false);
        if (资源点标识 != null)
        {
            // 打开另一座真实城池的调用者可能已设置新坐标，不能用旧备份覆盖新目标。
            if (城池坐标x == 资源目标x && 城池坐标y == 资源目标y)
            { 城池坐标x = 资源前城池x; 城池坐标y = 资源前城池y; index = 资源前Index; }
            if (资源头像 != null) { 资源头像.sprite = 资源前头像; 资源头像.gameObject.SetActive(资源前头像启用); }
        }
        资源点标识 = null;
        资源选择载入号 = 0;
        和平驻防模式 = 驻防;
        if (派遣按钮文字 == null)
            foreach (var 按钮 in GetComponentsInChildren<Button>(true))
                for (int i = 0; i < 按钮.onClick.GetPersistentEventCount(); i++)
                    if (按钮.onClick.GetPersistentTarget(i) == this && 按钮.onClick.GetPersistentMethodName(i) == "城池_出征选中将领")
                    {
                        派遣按钮文字 = 按钮.GetComponentInChildren<Text>(true);
                        原派遣文字 = 派遣按钮文字 != null ? 派遣按钮文字.text : "攻占";
                        if (派遣按钮文字 == null)
                        {
                            // 原按钮的“攻占”是子图字，保留金底、按下效果和原攻城外观。
                            var 图字 = 按钮.transform.Find("Image");
                            if (图字 != null)
                            {
                                派遣按钮原图字 = 图字.gameObject;
                                原派遣图字启用 = 派遣按钮原图字.activeSelf;
                            }
                            var 样式 = new 军事界面样式(transform);
                            派遣按钮文字 = 样式.文本(按钮.transform, "派遣文字", "派遣", Vector2.zero, Vector2.zero, 18);
                            军事界面样式.拉伸(派遣按钮文字.rectTransform, 4);
                            派遣按钮文字.alignment = TextAnchor.MiddleCenter;
                            原界面文字样式.按钮(派遣按钮文字);
                        }
                    }
        if (派遣按钮原图字 != null)
        {
            派遣按钮原图字.SetActive(!驻防 && 原派遣图字启用);
            派遣按钮文字.gameObject.SetActive(驻防);
        }
        if (派遣按钮文字 != null)
        {
            派遣按钮文字.text = 驻防 ? "派遣" : 原派遣文字;
            原界面文字样式.居中按钮文字(派遣按钮文字);
        }
        更新和平驻防展示(驻防);
    }

    public void 设置资源点模式(string ID)
    {
        设置和平驻防模式(false);
        var 点 = 资源点规则.本地.查询().Find(x => x.标识 == ID);
        if (点 == null) { 全局变量.提示类.显示信息("资源点已变更，请重新选择。"); return; }
        资源前城池x = 城池坐标x; 资源前城池y = 城池坐标y; 资源前Index = index;
        资源点标识 = ID; 资源选择载入号 = 资源点规则.本地.载入号;
        资源目标x = 点.坐标x; 资源目标y = 点.坐标y;
        城池坐标x = 点.坐标x; 城池坐标y = 点.坐标y; index = 0;
        资源头像 = 山贼信息对象.transform.GetChild(0).GetChild(0).GetComponent<Image>();
        资源前头像 = 资源头像.sprite; 资源前头像启用 = 资源头像.gameObject.activeSelf;
        if (全局变量.山贼头像资源表 != null && 全局变量.山贼头像资源表.Length > 0)
            资源头像.sprite = 全局变量.山贼头像资源表[0];
        else 资源头像.gameObject.SetActive(false);
        if (派遣按钮原图字 != null) 派遣按钮原图字.SetActive(false);
        if (派遣按钮文字 != null) { 派遣按钮文字.gameObject.SetActive(true); 派遣按钮文字.text = "出征"; }
        已选中将领列表.Clear(); 选择快照.Clear();
        更新和平驻防展示(true);
        显示编队将领列表();
    }

    private void 更新和平驻防展示(bool 驻防)
    {
        if (!驻防)
        {
            恢复资源展示布局();
            if (!已记录驻防外观) return;
            if (驻防原标题图字 != null) 驻防原标题图字.SetActive(原驻防标题启用);
            if (驻防界面标题 != null) 驻防界面标题.gameObject.SetActive(false);
            if (驻防精准到达布局 != null) 驻防精准到达布局.SetActive(原精准到达启用);
            foreach (var 项 in 驻防前详情文字) if (项.Key != null) 项.Key.text = 项.Value;
            驻防前详情文字.Clear();
            if (驻防规则文字 != null) 驻防规则文字.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 原驻防规则高度);
            已记录驻防外观 = false;
            上次驻防信息刷新 = -1;
            return;
        }
        if (!已记录驻防外观)
        {
            var 图字 = transform.Find("出征界面背景布局/标题栏背景/8 (21)") as RectTransform;
            if (图字 != null)
            {
                驻防原标题图字 = 图字.gameObject;
                原驻防标题启用 = 驻防原标题图字.activeSelf;
                if (驻防界面标题 == null)
                {
                    var 样式 = new 军事界面样式(transform);
                    驻防界面标题 = 样式.文本(图字.parent, "驻防界面标题", "派遣驻防", 图字.sizeDelta, 图字.anchoredPosition, 22);
                    var 框 = 驻防界面标题.rectTransform;
                    框.anchorMin = 图字.anchorMin; 框.anchorMax = 图字.anchorMax; 框.pivot = 图字.pivot;
                    原界面文字样式.标题(驻防界面标题);
                    框.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Max(图字.rect.height, 出征单行高度(驻防界面标题) + 2));
                    驻防界面标题.alignment = TextAnchor.MiddleCenter;
                }
            }
            var 时间 = transform.Find("时间设置布局");
            驻防精准到达布局 = 时间 != null ? 时间.gameObject : null;
            原精准到达启用 = 驻防精准到达布局 != null && 驻防精准到达布局.activeSelf;
            驻防规则文字 = 山贼信息对象.transform.GetChild(2).GetChild(3).GetChild(0).GetComponent<Text>();
            原驻防规则高度 = 驻防规则文字.rectTransform.rect.height;
            已记录驻防外观 = true;
        }
        if (驻防原标题图字 != null) 驻防原标题图字.SetActive(false);
        if (驻防界面标题 != null)
        {
            驻防界面标题.text = 资源点标识 != null ? "占领资源点" : "派遣驻防";
            驻防界面标题.gameObject.SetActive(true);
        }
        if (驻防精准到达布局 != null) 驻防精准到达布局.SetActive(false);
        刷新和平驻防信息();
    }

    private void 驻防详情(Text 字, string 内容)
    {
        if (!驻防前详情文字.ContainsKey(字)) 驻防前详情文字.Add(字, 字.text);
        字.text = 内容;
    }

    private void 刷新和平驻防信息()
    {
        if ((!和平驻防模式 && 资源点标识 == null) || !已记录驻防外观) return;
        if (资源点标识 != null) { 刷新资源出征信息(); return; }
        var 玩家 = 军事缺口入口.当前玩家();
        var 地 = 玩家 != null && 当前选中封地 >= 0 && 当前选中封地 < 玩家.封地信息表.Count ? 玩家.封地信息表[当前选中封地] : null;
        var 城 = 所有城池界面脚本.根据坐标获取指定城池(城池坐标x, 城池坐标y);
        var 概况 = 山贼信息对象.transform.GetChild(0);
        var 详情 = 山贼信息对象.transform.GetChild(2);
        string 本国 = 玩家 != null && 玩家.基础信息 != null ? 玩家.基础信息.国家 : "角色未载入";
        驻防详情(概况.GetChild(1).GetComponent<Text>(), 城 != null ? 城.名称 + "(" + 城.坐标x + "," + 城.坐标y + ")" : "驻防目标已变更");
        驻防详情(概况.GetChild(2).GetComponent<Text>(), "本国:" + 本国 + (城 != null ? "  目标:" + 城.获取国家名字() : ""));
        驻防详情(概况.GetChild(3).GetComponent<Text>(), "出发封地:" + (地 != null ? 地.封地名字 : "未选择有效封地"));
        double 上限 = 城 != null ? 城.获取驻防上限() : 0;
        double 已占 = 城 != null ? 和平驻防规则.已占兵力(城) : 0;
        驻防详情(详情.GetChild(0).GetComponent<Text>(), "可用驻防:" + Math.Max(0, 上限 - 已占).ToString("0") + "/" + 上限.ToString("0"));
        驻防详情(详情.GetChild(1).GetComponent<Text>(), "已用(含在途):" + 已占.ToString("0"));
        long 秒 = 地 != null && 地.所在城池 != null && 城 != null ? 和平驻防规则.计算行军秒数(地.所在城池.x, 地.所在城池.y, 城.坐标x, 城.坐标y) : -1;
        驻防详情(详情.GetChild(2).GetComponent<Text>(), "预计行军:" + (秒 >= 0 ? TIME.ToTimeFormat(秒) : "无法计算"));
        double 已选兵力 = 0;
        foreach (var 项 in 已选中将领列表)
        {
            将领信息 将;
            if (选择快照.TryGetValue(项, out 将) && 将 != null && 将.将领配兵 != null) 已选兵力 += Math.Max(0, 将.将领配兵.数量);
        }
        驻防详情(驻防规则文字, "【驻防规则】\n已选兵力:" + 已选兵力.ToString("0") + "\n容量含驻守及在途部队。\n【撤回】\n前往/驻守时在军情中撤回。\n参战时先结束战斗或撤退。\n返程到达后恢复空闲，保留配兵。");
        // 沿用原详情滚动区，仅让原内容Text容纳完整规则。
        if (驻防规则文字.rectTransform.rect.width > 0)
            驻防规则文字.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Max(原驻防规则高度, 驻防规则文字.preferredHeight + 2));
        上次驻防信息刷新 = TIME.getTime();
    }

    private void 刷新资源出征信息()
    {
        var 规则 = 资源点规则.本地;
        var 点 = 规则.载入号 == 资源选择载入号 ? 规则.查询().Find(x => x.标识 == 资源点标识) : null;
        var 玩家 = 军事缺口入口.当前玩家();
        var 地 = 玩家 != null && 当前选中封地 >= 0 && 当前选中封地 < 玩家.封地信息表.Count ? 玩家.封地信息表[当前选中封地] : null;
        初始化资源展示布局();
        var 详情 = 山贼信息对象.transform.GetChild(2);
        更新资源概况(0, 点 != null ? 点.类型 + "(" + 点.坐标x + "," + 点.坐标y + ")" : "资源目标已变更，请重新打开");
        更新资源概况(1, 点 == null ? "世界已切换" : 点.占领玩家ID >= 0 ? "已占领" : 点.剩余库存 > 0 ? "中立" : "采尽，恢复中");
        更新资源概况(2, "出发封地:" + (地 != null ? 地.封地名字 : "未选择有效封地"));
        驻防详情(详情.GetChild(0).GetComponent<Text>(), "基础时产:" + (点 != null ? 点.时产.ToString() : "--") + "/小时");
        驻防详情(详情.GetChild(1).GetComponent<Text>(), "剩余库存:" + (点 != null ? 点.剩余库存 + "/" + 点.时产 : "--"));
        long 秒 = 地 != null && 地.所在城池 != null && 点 != null ? 和平驻防规则.计算行军秒数(地.所在城池.x, 地.所在城池.y, 点.坐标x, 点.坐标y) : -1;
        驻防详情(详情.GetChild(2).GetComponent<Text>(), "预计行军:" + (秒 >= 0 ? TIME.ToTimeFormat(秒) : "无法计算"));
        int 已占 = 玩家 != null && 玩家.基础信息 != null ? 规则.已占数量(玩家.基础信息.ID) : 0;
        int 在途 = 玩家 != null && 玩家.基础信息 != null ? 规则.军情().FindAll(x => x.所属玩家ID == 玩家.基础信息.ID).Count : 0;
        string 说明 = "【资源点规则】\n已占:" + 已占 + "  出征中:" + 在途 + "  上限:2\n守军：1级山贼。击败守军后占领。\n胜利后部队归队，自动采集。\n铜矿按1级大厅、牧场按1级农场的基础产量采集。\n每轮库存为一小时基础产量，采尽后十分钟恢复。\n【撤回】\n行军时可在军情中撤回，保留配兵；已用5点体力不退。\n参战后可在战场撤退，失败或撤退不占领。\n【出发封地】\n" + (地 != null ? 地.封地名字 : "未选择有效封地") + (点 != null ? "\n" + 资源点界面适配.状态文字(点) : "\n资源目标已变更，请重新打开。");
        if (驻防规则文字.text != 说明) { 驻防详情(驻防规则文字, 说明); 资源规则待更新 = true; }
        检查资源展示尺寸();
        上次驻防信息刷新 = TIME.getTime();
    }

    private void 记录资源布局(RectTransform 框)
    {
        if (框 == null || 资源原布局.ContainsKey(框)) return;
        var 字 = 框.GetComponent<Text>();
        资源原布局.Add(框, new 资源布局副本 {
            小锚 = 框.anchorMin, 大锚 = 框.anchorMax, 支点 = 框.pivot, 位置 = 框.anchoredPosition, 尺寸 = 框.sizeDelta,
            字 = 字, 换行 = 字 != null ? 字.horizontalOverflow : HorizontalWrapMode.Wrap,
            溢出 = 字 != null ? 字.verticalOverflow : VerticalWrapMode.Truncate,
            对齐 = 字 != null ? 字.alignment : TextAnchor.MiddleLeft, 富文本 = 字 != null && 字.supportRichText
        });
    }

    private static void 设置资源矩形(RectTransform 框, Vector2 小锚, Vector2 大锚, Vector2 支点, Vector2 位置, Vector2 尺寸)
    {
        if (框.anchorMin != 小锚) 框.anchorMin = 小锚;
        if (框.anchorMax != 大锚) 框.anchorMax = 大锚;
        if (框.pivot != 支点) 框.pivot = 支点;
        if (框.sizeDelta != 尺寸) 框.sizeDelta = 尺寸;
        if (框.anchoredPosition != 位置) 框.anchoredPosition = 位置;
    }

    private void 更新资源概况(int 序, string 内容)
    {
        if (资源概况全文[序] == 内容) return;
        资源概况全文[序] = 内容;
        驻防详情(资源概况文字[序], 内容);
        资源概况待更新 = true;
    }

    private void 初始化资源展示布局()
    {
        if (资源概况容器 != null) return;
        资源概况容器 = 山贼信息对象.transform.GetChild(0) as RectTransform;
        for (int i = 0; i < 资源概况文字.Length; i++)
        {
            var 字 = 资源概况容器.GetChild(i + 1).GetComponent<Text>();
            资源概况文字[i] = 字; 记录资源布局(字.rectTransform);
            if (字.horizontalOverflow != HorizontalWrapMode.Wrap) 字.horizontalOverflow = HorizontalWrapMode.Wrap;
            if (字.verticalOverflow != VerticalWrapMode.Truncate) 字.verticalOverflow = VerticalWrapMode.Truncate;
            if (字.alignment != TextAnchor.MiddleLeft) 字.alignment = TextAnchor.MiddleLeft;
            if (字.supportRichText) 字.supportRichText = false;
        }
        if (驻防界面标题 != null) 记录资源布局(驻防界面标题.rectTransform);
        资源规则内容 = 驻防规则文字.rectTransform;
        资源规则视口 = 资源规则内容.parent as RectTransform;
        资源说明滚动 = 资源规则视口.GetComponent<ScrollRect>();
        记录资源布局(资源规则视口); 记录资源布局(资源规则内容);
        if (资源说明滚动 != null)
        {
            原资源横向滚动 = 资源说明滚动.horizontal; 原资源纵向滚动 = 资源说明滚动.vertical;
            if (资源说明滚动.horizontal) 资源说明滚动.horizontal = false;
            if (!资源说明滚动.vertical) 资源说明滚动.vertical = true;
            资源说明滚动.StopMovement();
        }
        float 边距 = (资源规则视口.parent as RectTransform).rect.width / 2 + 资源规则视口.anchoredPosition.x - 资源规则视口.rect.width / 2;
        设置资源矩形(资源规则视口, new Vector2(0, .5f), new Vector2(1, .5f), new Vector2(.5f, .5f),
            new Vector2(0, 资源规则视口.anchoredPosition.y), new Vector2(-Mathf.Max(0, 边距) * 2, 资源规则视口.rect.height));
        设置资源矩形(资源规则内容, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), Vector2.zero,
            new Vector2(0, 资源规则内容.sizeDelta.y));
        if (驻防规则文字.horizontalOverflow != HorizontalWrapMode.Wrap) 驻防规则文字.horizontalOverflow = HorizontalWrapMode.Wrap;
        if (驻防规则文字.verticalOverflow != VerticalWrapMode.Truncate) 驻防规则文字.verticalOverflow = VerticalWrapMode.Truncate;
        if (驻防规则文字.alignment != TextAnchor.UpperLeft) 驻防规则文字.alignment = TextAnchor.UpperLeft;
        if (驻防规则文字.supportRichText) 驻防规则文字.supportRichText = false;
        // 只向原 ContentSizeFitter 提供首选高度，尺寸仍由原布局器写入。
        资源内容高度 = 资源规则内容.GetComponent<LayoutElement>();
        新增资源内容高度 = 资源内容高度 == null;
        if (新增资源内容高度) 资源内容高度 = 资源规则内容.gameObject.AddComponent<LayoutElement>();
        原资源内容高度启用 = 资源内容高度.enabled; 原资源内容首选高度 = 资源内容高度.preferredHeight;
        原资源内容布局优先级 = 资源内容高度.layoutPriority;
        if (!资源内容高度.enabled) 资源内容高度.enabled = true;
        if (资源内容高度.layoutPriority < 1) 资源内容高度.layoutPriority = 1;
        资源画布 = 驻防规则文字.canvas != null ? 驻防规则文字.canvas.rootCanvas : null;
        资源概况待更新 = 资源规则待更新 = true;
    }

    private void 检查资源展示尺寸()
    {
        if (资源概况容器 == null || 资源规则视口 == null) return;
        var 概况尺寸 = 资源概况容器.rect.size; var 视口尺寸 = 资源规则视口.rect.size;
        float 比例 = 资源画布 != null ? 资源画布.scaleFactor : 1;
        bool 尺寸变化 = 概况尺寸 != 上次资源概况尺寸 || 视口尺寸 != 上次资源视口尺寸 || !Mathf.Approximately(比例, 上次资源画布比例);
        if (!尺寸变化 && !资源概况待更新 && !资源规则待更新) return;
        if (概况尺寸.x <= 0 || 视口尺寸.x <= 0) return;
        适配资源展示布局(概况尺寸, 视口尺寸, 比例, 尺寸变化);
    }

    private void 适配资源展示布局(Vector2 概况尺寸, Vector2 视口尺寸, float 比例, bool 尺寸变化)
    {
        if (尺寸变化)
        {
            float 行高 = 0;
            foreach (var 字 in 资源概况文字) 行高 = Mathf.Max(行高, 出征单行高度(字));
            for (int i = 0; i < 资源概况文字.Length; i++)
            {
                var 框 = 资源概况文字[i].rectTransform;
                设置资源矩形(框, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(.5f, .5f),
                    new Vector2(资源原布局[框].位置.x, (1 - i) * 行高), new Vector2(资源原布局[框].尺寸.x, 行高));
            }
            if (驻防界面标题 != null)
            {
                if (驻防界面标题.horizontalOverflow != HorizontalWrapMode.Overflow) 驻防界面标题.horizontalOverflow = HorizontalWrapMode.Overflow;
                var 框 = 驻防界面标题.rectTransform; var 原 = 资源原布局[框];
                设置资源矩形(框, 原.小锚, 原.大锚, 原.支点, 原.位置,
                    new Vector2(Mathf.Max(原.尺寸.x, Mathf.Ceil(驻防界面标题.preferredWidth) + 4), Mathf.Max(原.尺寸.y, 出征单行高度(驻防界面标题) + 2)));
            }
        }
        if (尺寸变化 || 资源概况待更新)
            for (int i = 0; i < 资源概况文字.Length; i++) 限定出征标题副本(资源概况文字[i], 资源概况全文[i]);
        if (尺寸变化 || 资源规则待更新)
        {
            float 高 = Mathf.Max(原驻防规则高度, Mathf.Ceil(驻防规则文字.preferredHeight) + 8);
            if (!Mathf.Approximately(资源内容高度.preferredHeight, 高)) 资源内容高度.preferredHeight = 高;
        }
        上次资源概况尺寸 = 概况尺寸; 上次资源视口尺寸 = 视口尺寸; 上次资源画布比例 = 比例;
        资源概况待更新 = 资源规则待更新 = false;
    }

    private void 恢复资源展示布局()
    {
        foreach (var 项 in 资源原布局)
        {
            var 框 = 项.Key; var 原 = 项.Value; if (框 == null) continue;
            设置资源矩形(框, 原.小锚, 原.大锚, 原.支点, 原.位置, 原.尺寸);
            if (原.字 == null) continue;
            if (原.字.horizontalOverflow != 原.换行) 原.字.horizontalOverflow = 原.换行;
            if (原.字.verticalOverflow != 原.溢出) 原.字.verticalOverflow = 原.溢出;
            if (原.字.alignment != 原.对齐) 原.字.alignment = 原.对齐;
            if (原.字.supportRichText != 原.富文本) 原.字.supportRichText = 原.富文本;
        }
        if (资源内容高度 != null)
        {
            if (新增资源内容高度)
            {
                // 保留禁用的原生组件供再次打开复用，避免帧末销毁影响同帧重开。
                if (资源内容高度.enabled) 资源内容高度.enabled = false;
                if (资源内容高度.preferredHeight != -1) 资源内容高度.preferredHeight = -1;
            }
            else
            {
                if (资源内容高度.preferredHeight != 原资源内容首选高度) 资源内容高度.preferredHeight = 原资源内容首选高度;
                if (资源内容高度.layoutPriority != 原资源内容布局优先级) 资源内容高度.layoutPriority = 原资源内容布局优先级;
                if (资源内容高度.enabled != 原资源内容高度启用) 资源内容高度.enabled = 原资源内容高度启用;
            }
        }
        if (资源说明滚动 != null)
        {
            if (资源说明滚动.horizontal != 原资源横向滚动) 资源说明滚动.horizontal = 原资源横向滚动;
            if (资源说明滚动.vertical != 原资源纵向滚动) 资源说明滚动.vertical = 原资源纵向滚动;
        }
        资源原布局.Clear(); 资源说明滚动 = null; 资源内容高度 = null;
        资源概况容器 = 资源规则视口 = 资源规则内容 = null; 资源画布 = null;
        上次资源概况尺寸 = 上次资源视口尺寸 = new Vector2(-1, -1); 上次资源画布比例 = -1;
        资源概况待更新 = 资源规则待更新 = false;
        Array.Clear(资源概况全文, 0, 资源概况全文.Length);
        Array.Clear(资源概况文字, 0, 资源概况文字.Length);
    }

    private void OnDisable() { 设置和平驻防模式(false); }

    private void LateUpdate()
    {
        if ((和平驻防模式 || 资源点标识 != null) && 上次驻防信息刷新 != TIME.getTime()) 刷新和平驻防信息();
        if (资源点标识 != null && 已记录驻防外观) 检查资源展示尺寸();
        foreach (var 项 in 出征标题原文)
        {
            var 字 = 项.Key;
            if (字 == null || !字.gameObject.activeInHierarchy) continue;
            var 尺寸 = 字.rectTransform.rect.size;
            if (尺寸.x <= 0 || 尺寸.y <= 0 || 项.Value.尺寸 == 尺寸) continue;
            项.Value.尺寸 = 尺寸;
            限定出征标题副本(字, 项.Value.原文);
        }
    }

    private static float 出征单行高度(Text 字)
    {
        var 设置 = 字.GetGenerationSettings(new Vector2(10000, 0));
        return Mathf.Ceil(Mathf.Max(字.fontSize, 字.cachedTextGeneratorForLayout.GetPreferredHeight("国", 设置) / 字.pixelsPerUnit));
    }

    private void 设置出征行布局(Transform 行)
    {
        const float 边距 = 4, 间距 = 2;
        var 格 = 编队将领对象.GetComponent<GridLayoutGroup>();
        var 名字 = 行.GetChild(2).GetComponent<Text>();
        var 兵力 = 行.GetChild(4).GetComponent<Text>();
        var 体力 = 行.GetChild(6).GetComponent<Text>();
        float 名高 = 出征单行高度(名字) * 2 + 2, 兵高 = 出征单行高度(兵力) + 2, 体高 = 出征单行高度(体力) + 2;
        float 行高 = 边距 * 2 + 名高 + 兵高 + 体高 + 间距 * 2;
        格.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        格.constraintCount = 1;
        格.cellSize = new Vector2(格.cellSize.x, 行高);
        var 头像 = 行.GetChild(1) as RectTransform;
        float 左 = 头像.rect.width + 边距 * 3;
        设置出征行区域(名字.rectTransform, 左, 边距, 边距, 名高);
        float 兵顶 = 边距 + 名高 + 间距, 体顶 = 兵顶 + 兵高 + 间距;
        var 兵图 = 行.GetChild(3) as RectTransform;
        var 体图 = 行.GetChild(5) as RectTransform;
        设置出征行区域(兵力.rectTransform, 左 + 兵图.rect.width + 间距, 边距, 兵顶, 兵高);
        设置出征行区域(体力.rectTransform, 左 + 兵图.rect.width + 间距, 边距, 体顶, 体高);
        头像.anchorMin = 头像.anchorMax = new Vector2(0, .5f); 头像.anchoredPosition = new Vector2(边距 + 头像.rect.width / 2, 0);
        设置出征行图标(兵图, 左, 兵顶, 兵高);
        设置出征行图标(体图, 左, 体顶, 体高);
        foreach (var 字 in new[] { 名字, 兵力, 体力 })
        {
            字.horizontalOverflow = HorizontalWrapMode.Wrap; 字.verticalOverflow = VerticalWrapMode.Truncate;
            字.alignment = TextAnchor.MiddleLeft;
        }
        // 原九片装饰与选中/禁用边框改为相对锚点，不缩放角片和图标。
        foreach (int 号 in new[] { 0, 8, 9 })
        {
            var 装饰 = 行.GetChild(号) as RectTransform;
            if (已适配出征装饰.Add(装饰)) 适配出征行装饰(装饰);
            军事界面样式.拉伸(装饰, 0);
        }
        var 按钮 = 行.Find("选中按钮") as RectTransform;
        if (按钮 != null) 军事界面样式.拉伸(按钮, 0);
    }

    private static void 设置出征行区域(RectTransform 框, float 左, float 右, float 顶, float 高)
    {
        框.anchorMin = new Vector2(0, 1); 框.anchorMax = new Vector2(1, 1); 框.pivot = new Vector2(0, 1);
        框.offsetMin = new Vector2(左, -顶 - 高); 框.offsetMax = new Vector2(-右, -顶);
    }

    private static void 设置出征行图标(RectTransform 图, float 左, float 顶, float 高)
    {
        图.anchorMin = 图.anchorMax = new Vector2(0, 1); 图.pivot = new Vector2(0, .5f);
        图.anchoredPosition = new Vector2(左, -顶 - 高 / 2);
    }

    private static void 适配出征行装饰(RectTransform 装饰)
    {
        var 区域 = 装饰.rect;
        foreach (RectTransform 片 in 装饰)
        {
            var 最小 = (Vector2)装饰.InverseTransformPoint(片.TransformPoint(片.rect.min));
            var 最大 = (Vector2)装饰.InverseTransformPoint(片.TransformPoint(片.rect.max));
            var 小锚 = Vector2.zero; var 大锚 = Vector2.zero;
            for (int 轴 = 0; 轴 < 2; 轴++)
            {
                if (最大[轴] - 最小[轴] > 区域.size[轴] / 2) 大锚[轴] = 1;
                else 小锚[轴] = 大锚[轴] = Mathf.Abs((最大[轴] + 最小[轴]) / 2 - 区域.center[轴]) < 1 ? .5f :
                    (最大[轴] + 最小[轴]) / 2 < 区域.center[轴] ? 0 : 1;
            }
            片.anchorMin = 小锚; 片.anchorMax = 大锚;
            片.offsetMin = 最小 - 区域.min - Vector2.Scale(区域.size, 小锚);
            片.offsetMax = 最大 - 区域.min - Vector2.Scale(区域.size, 大锚);
        }
    }

    private static void 限定出征标题副本(Text 字, string 原文)
    {
        字.text = 原文;
        float 高 = 字.rectTransform.rect.height;
        if (字.preferredHeight <= 高 + .5f) return;
        字.text = "…";
        if (字.preferredHeight > 高 + .5f) { 字.text = ""; return; }
        int 左 = 0, 右 = 原文.Length, 保留 = 0;
        while (左 <= 右)
        {
            int 中 = 左 + (右 - 左) / 2, 长度 = 中;
            if (长度 > 0 && char.IsHighSurrogate(原文[长度 - 1])) 长度--;
            字.text = 原文.Substring(0, 长度) + "…";
            if (字.preferredHeight <= 高 + .5f) { 保留 = 长度; 左 = 中 + 1; }
            else 右 = 中 - 1;
        }
        字.text = 原文.Substring(0, 保留) + "…";
    }

    public void 切换出征封地(int 号)
    {
        var 玩家 = 军事缺口入口.当前玩家();
        if (玩家 == null || 号 < 0 || 号 >= 玩家.封地信息表.Count) return;
        第几个玩家 = 全局变量.本机身份;
        当前选中封地 = 号;
        已选中将领列表.Clear();
        选择快照.Clear();
        显示编队将领列表();
    }

    private bool 校验选中将领(out List<将领信息> 将领表)
    {
        将领表 = new List<将领信息>();
        var 玩家 = 军事缺口入口.当前玩家();
        if (玩家 == null) { 全局变量.提示类.显示信息("本地角色尚未载入。"); return false; }
        foreach (var 项 in 已选中将领列表)
        {
            if (项.第几个封地 < 0 || 项.第几个封地 >= 玩家.封地信息表.Count) return 出征选择已变更();
            var 封地 = 玩家.封地信息表[项.第几个封地];
            if (项.第几个将领 < 0 || 项.第几个将领 >= 封地.将领信息表.Count) return 出征选择已变更();
            将领信息 快照;
            var 将 = 封地.将领信息表[项.第几个将领];
            if (!选择快照.TryGetValue(项, out 快照) || !ReferenceEquals(快照, 将) || 项.第几个封地 != 当前选中封地) return 出征选择已变更();
            将领表.Add(将);
        }
        var 结果 = 军事本地规则.检查出征(玩家, 将领表);
        if (!结果.成功) 全局变量.提示类.显示信息(结果.说明);
        return 结果.成功;
    }

    private bool 出征选择已变更()
    {
        已选中将领列表.Clear();
        选择快照.Clear();
        显示编队将领列表();
        全局变量.提示类.显示信息("将领或封地已变更，请重新选择。");
        return false;
    }

    private void OnEnable()
    {
        第几个玩家 = 全局变量.本机身份;
        var 玩家 = 军事缺口入口.当前玩家();
        int 号 = 玩家 != null && 玩家.封地信息表.Count > 0 ? Mathf.Clamp(全局变量.第几个封地, 0, 玩家.封地信息表.Count - 1) : -1;
        bool 保留 = ReferenceEquals(选择玩家, 玩家) && 当前选中封地 == 号;
        foreach (var 项 in 已选中将领列表)
        {
            将领信息 将;
            if (玩家 == null || !选择快照.TryGetValue(项, out 将) || 项.第几个封地 != 号 ||
                号 < 0 || 项.第几个将领 < 0 || 项.第几个将领 >= 玩家.封地信息表[号].将领信息表.Count ||
                !ReferenceEquals(将, 玩家.封地信息表[号].将领信息表[项.第几个将领]) ||
                !军事本地规则.检查出征(玩家, new List<将领信息> { 将 }).成功) 保留 = false;
        }
        当前选中封地 = 号;
        选择玩家 = 玩家;
        if (!保留) { 已选中将领列表.Clear(); 选择快照.Clear(); }
        显示编队将领列表();
    }

    private void 标记出征(List<将领信息> 将领表, int 阵营)
    {
        foreach (var 将 in 将领表) { 将.详细信息.坑位颜色 = 阵营; 将.详细信息.状态 = 1; }
        已选中将领列表.Clear();
        选择快照.Clear();
        显示编队将领列表();
    }

	public GameObject 战斗界面UI;

	public 战斗系统 战斗系统对象;

	public 军情信息 加入军情队列(int 战场类型, int 坐标x, int 坐标y, long 到达时间, List<将领信息> 要加入的将领列表,bool 是否为增援=false)
	{
		军情信息 军情信息 = new 军情信息();
		军情信息.战场类型 = 战场类型;
		军情信息.坐标x = 坐标x;
		军情信息.坐标y = 坐标y;
		军情信息.到达时间 = 到达时间;
		军情信息.队列将领列表 = 要加入的将领列表;
		军情信息.已进入战场 = 是否为增援;
		全局变量.军情列表.Add(军情信息);
		return 军情信息;
	}

	public void 山贼_出征选中将领()
	{
        List<将领信息> 将领表;
        if (附近山贼.获取指定坐标的山贼(山贼坐标x, 山贼坐标y) == null) { 全局变量.提示类.显示信息("山贼目标已变更。"); return; }
        if (!校验选中将领(out 将领表)) return;
        加入军情队列(0, 山贼坐标x, 山贼坐标y, TIME.getTime() + 10, 将领表);
        标记出征(将领表, 0);
        全局变量.提示类.显示信息("部队已出征，可在军情查看行军。");
    }

    private static string 读取到达时间(Text 字)
    {
        if (字 == null) return "";
        var 输入 = 字.GetComponentInParent<InputField>();
        return (输入 != null ? 输入.text : 字.text).Trim();
    }

	public void 城池_出征选中将领()
	{
        if (资源点标识 != null) { 资源点_出征选中将领(); return; }
        if (和平驻防模式) { 城池_驻防选中将领(); return; }
        List<将领信息> 将领表;
        if (所有城池界面脚本.根据坐标获取指定城池(城池坐标x, 城池坐标y) == null) { 全局变量.提示类.显示信息("城池目标已变更。"); return; }
        if (!校验选中将领(out 将领表)) return;
        long 到达 = TIME.getTime() + (index == 1 ? 5 : 10);
        string 时文 = 读取到达时间(精准到达_时);
        string 分文 = 读取到达时间(精准到达_分);
        string 秒文 = 读取到达时间(精准到达_秒);
        if (index != 1 && (时文.Length + 分文.Length + 秒文.Length > 0))
        {
            int 时, 分, 秒;
            if (!int.TryParse(时文, out 时) || !int.TryParse(分文, out 分) || !int.TryParse(秒文, out 秒) || 时 < 0 || 时 > 23 || 分 < 0 || 分 > 59 || 秒 < 0 || 秒 > 59)
            { 全局变量.提示类.显示信息("精准到达时间无效，请完整填写时、分、秒，或全部留空。"); return; }
            var 日期 = TIME.TimeStampToDateTime(到达);
            var 指定 = new DateTime(日期.Year, 日期.Month, 日期.Day, 时, 分, 秒);
            long 时间 = TIME.DateTimeToTimeStamp(指定);
            if (时间 < 到达) { 全局变量.提示类.显示信息("精准到达时间必须至少晚于当前时间10秒。"); return; }
            到达 = 时间;
        }
        if (index == 1)
            全局变量.军情列表.Add(new 军情信息 { 战场类型 = 1, 坐标x = 城池坐标x, 坐标y = 城池坐标y, 到达时间 = 到达, 队列将领列表 = 将领表, 身份 = 78, 任务用途 = 军事任务用途.守方援军 });
        else 加入军情队列(1, 城池坐标x, 城池坐标y, 到达, 将领表);
        标记出征(将领表, index == 1 ? 1 : 0);
        全局变量.提示类.显示信息("部队已出征，可在军情查看行军。");
    }

    private void 资源点_出征选中将领()
    {
        if (资源选择载入号 != 资源点规则.本地.载入号) { 全局变量.提示类.显示信息("世界已切换，请重新选择资源点。"); return; }
        List<将领信息> 将领表;
        if (!校验选中将领(out 将领表)) return;
        var 玩家 = 军事缺口入口.当前玩家();
        var 地 = 玩家 != null && 当前选中封地 >= 0 && 当前选中封地 < 玩家.封地信息表.Count ? 玩家.封地信息表[当前选中封地] : null;
        资源点军情信息 情;
        var 结果 = 资源点规则.本地.派遣(资源点标识, 地 != null ? 地.ID : -1, 将领表, TIME.getTime(), 资源选择载入号, out 情);
        全局变量.提示类.显示信息(结果.Message);
        if (!结果.Success) return;
        // 资源规则已经扣体力并占用同一批实际将领，不再调用标记出征或另加军情。
        已选中将领列表.Clear(); 选择快照.Clear(); 显示编队将领列表();
    }

    public void 城池_驻防选中将领()
    {
        List<将领信息> 将领表;
        if (!校验选中将领(out 将领表)) return;
        var 玩家 = 军事缺口入口.当前玩家();
        var 封地 = 玩家 != null && 当前选中封地 >= 0 && 当前选中封地 < 玩家.封地信息表.Count ? 玩家.封地信息表[当前选中封地] : null;
        var 城 = 所有城池界面脚本.根据坐标获取指定城池(城池坐标x, 城池坐标y);
        var 结果 = 和平驻防规则.派遣(玩家, 封地, 城, 将领表, TIME.getTime());
        全局变量.提示类.显示信息(结果.说明);
        if (!结果.成功) return;
        已选中将领列表.Clear();
        选择快照.Clear();
        显示编队将领列表();
    }

    public void 增援()
	{
        if (战斗界面UI == null) return;
        var 界面 = 战斗界面UI.GetComponent<战斗界面UI脚本>();
        战斗系统对象 = 界面 != null ? 界面.战斗系统脚本对象 : null;
        if (战斗系统对象 == null || 战斗系统对象.战斗结束 || 战斗系统对象.等待销毁战场 || 战斗系统对象.攻身份 != 全局变量.本机身份)
        { 全局变量.提示类.显示信息("当前战场不允许本机增援。"); return; }
        List<将领信息> 将领表;
        if (!校验选中将领(out 将领表)) return;
        var 增援军情 = 加入军情队列(战斗系统对象.战场类型, 战斗系统对象.坐标x, 战斗系统对象.坐标y, TIME.getTime(), 将领表, true);
        战斗系统对象.攻方要渲染的编队将领列表.Add(将领表);
        战斗系统对象.登记参战军情(增援军情);
        标记出征(将领表, 0);
        全局变量.提示类.显示信息("增援部队已加入战场。");
    }


	public void 批量补兵()
	{
		选中将领批量补兵();
		显示编队将领列表();
	}

	public void 刷新山贼()
	{
        设置和平驻防模式(false);
        已选中将领列表.Clear();
        选择快照.Clear();
        if (附近山贼.获取指定坐标的山贼(山贼坐标x, 山贼坐标y) != null) 显示山贼详情();
        显示编队将领列表();
    }

	public void 刷新城池()
	{
        设置和平驻防模式(false);
        已选中将领列表.Clear();
        选择快照.Clear();
        if (所有城池界面脚本.根据坐标获取指定城池(城池坐标x, 城池坐标y) != null) 显示城池详情();
        显示编队将领列表();
    }

	private void 显示城池详情()
	{
		城池信息库类 城池信息库类 = 所有城池界面脚本.根据坐标获取指定城池(城池坐标x, 城池坐标y);
		山贼信息对象.transform.GetChild(0).GetChild(0).GetComponent<Image>()
			.sprite = 全局变量.城池规模头像资源表[城池信息库类.规模];
		山贼信息对象.transform.GetChild(0).GetChild(1).GetComponent<Text>()
			.text = 城池信息库类.名称 + "(" + 城池信息库类.获取规模名称() + "城" + 城池信息库类.坐标x.ToString() + "," + 城池信息库类.坐标y.ToString() + ")  国家:" + 城池信息库类.获取国家名字();
		山贼信息对象.transform.GetChild(0).GetChild(2).GetComponent<Text>()
			.text = "天赋:" + 城池信息库类.获取天赋类型名称() + "+" + 城池信息库类.天赋加成.ToString() + "%  封地:" + 城池信息库类.城池封地列表.Count.ToString() + "/" + 城池信息库类.获取封地上限().ToString();
		山贼信息对象.transform.GetChild(0).GetChild(3).GetComponent<Text>()
			.text = "城主:" + 城池信息库类.获取城主名字();
		山贼信息对象.transform.GetChild(2).GetChild(0).GetComponent<Text>()
			.text = "玩家驻防兵力:" + 和平驻防规则.已占兵力(城池信息库类).ToString("0") + "/" + 城池信息库类.获取驻防上限().ToString("0");
		山贼信息对象.transform.GetChild(2).GetChild(1).GetComponent<Text>()
			.text = "道路:" + 城池信息库类.获取道路上限().ToString();
		山贼信息对象.transform.GetChild(2).GetChild(2).GetComponent<Text>()
			.text = "城墙:" + 城池信息库类.城墙.ToString();
		山贼信息对象.transform.GetChild(2).GetChild(3).GetChild(0)
			.GetComponent<Text>()
			.text = "【名将协防】:\r\n名将协防几率:" + 城池信息库类.协防几率.ToString() + "%\r\n名将数量: " + 城池信息库类.协防数量f.ToString() + "-" + 城池信息库类.协防数量m.ToString() + "\r\n【攻克奖励】\r\n战功 + " + 城池信息库类.战功.ToString() + "\r\n国库铜钱 + 100000\r\n国库粮食 + 200000";
	}

	private void 显示山贼详情()
	{
		山贼属性信息 山贼属性信息 = 附近山贼.获取指定坐标的山贼(山贼坐标x, 山贼坐标y);
		if (山贼属性信息 != null)
		{
			double 等级 = 山贼属性信息.等级;
			Image component = 山贼信息对象.transform.GetChild(0).GetChild(0).GetComponent<Image>();
			int num = (int)等级 - 1;
			component.sprite = 全局变量.山贼头像资源表[num];
			山贼信息对象.transform.GetChild(0).GetChild(1).GetComponent<Text>()
				.text = 等级.ToString() + "级山贼 (" + 山贼坐标x.ToString() + "," + 山贼坐标y.ToString() + ")";
			int count = 山贼属性信息.将领数据列表.Count;
			string text = "";
			double num2 = 0.0;
			for (int i = 0; i < count; i++)
			{
				double iD = 山贼属性信息.将领数据列表[i].将领配兵.ID;
				double 数量 = 山贼属性信息.将领数据列表[i].将领配兵.数量;
				num2 += 数量;
				int num3 = 全局兵种库.查询指定ID的索引(iD);
				text = ((num3 == -1) ? (text + "未知 " + 数量.ToString() + "\n") : (text + 全局兵种库.属性表[num3].名称 + " " + 数量.ToString() + "\n"));
			}
			山贼信息对象.transform.GetChild(0).GetChild(2).GetComponent<Text>()
				.text = "兵力:" + num2.ToString();
			string text2 = "奖励资源";
			if (山贼属性信息.掉落宝物 == 1.0)
			{
				text2 += "、宝物";
			}
			if (山贼属性信息.掉落宝箱 == 1.0)
			{
				text2 += "、宝箱";
			}
			if (山贼属性信息.掉落装备 == 1.0)
			{
				text2 += "、装备";
			}
			山贼信息对象.transform.GetChild(0).GetChild(3).GetComponent<Text>()
				.text = text2;
			山贼信息对象.transform.GetChild(2).GetChild(0).GetComponent<Text>()
				.text = "敌军等级:" + 山贼属性信息.将领数据列表[0].将领属性.成长点数.等级.ToString() + "级";
			山贼信息对象.transform.GetChild(2).GetChild(1).GetComponent<Text>()
				.text = "敌军规模:" + count.ToString() + "名";
			山贼信息对象.transform.GetChild(2).GetChild(3).GetChild(0)
				.GetComponent<Text>()
				.text = text;
		}
	}

	private void 显示编队将领列表()
    {
        第几个玩家 = 全局变量.本机身份;
        隐藏所有列表将领对象();
        已显示将领列表.Clear();
        出征标题原文.Clear();
        if (编队将领对象.transform.childCount > 0) 编队将领对象.transform.GetChild(0).gameObject.SetActive(false);
        if (空将领提示 == null)
        {
            var 样式 = new 军事界面样式(transform);
            空将领提示 = 样式.文本(编队将领对象.transform.parent, "出征空将领提示", "暂无将领，请先招募并配兵。", Vector2.zero, Vector2.zero, 16);
            军事界面样式.拉伸(空将领提示.rectTransform, 12);
            空将领提示.alignment = TextAnchor.MiddleCenter;
        }
        空将领提示.gameObject.SetActive(true);
        if (军事缺口入口.当前玩家() == null) return;
		int count = 全局变量.所有玩家数据表[第几个玩家].封地信息表.Count;
		int num = 0;
		for (int i = 0; i < count; i++)
		{
			int count2 = 全局变量.所有玩家数据表[第几个玩家].封地信息表[i].将领信息表.Count;
			for (int j = 0; j < count2; j++)
			{
				double 编队 = 全局变量.所有玩家数据表[第几个玩家].封地信息表[i].将领信息表[j].详细信息.编队;
				double iD2 = 全局变量.所有玩家数据表[第几个玩家].封地信息表[i].将领信息表[j].将领属性.初始属性.ID;
				bool flag = false;
				if (编队切换对象.transform.GetChild(0).GetComponent<Toggle>().isOn)
				{
					if (i == 当前选中封地)
					{
						flag = true;
					}
				}
				else if (编队切换对象.transform.GetChild(1).GetComponent<Toggle>().isOn)
				{
					if (编队 == 1.0)
					{
						flag = true;
					}
				}
				else if (编队切换对象.transform.GetChild(2).GetComponent<Toggle>().isOn)
				{
					if (编队 == 2.0)
					{
						flag = true;
					}
				}
				else if (编队切换对象.transform.GetChild(3).GetComponent<Toggle>().isOn)
				{
					if (编队 == 3.0)
					{
						flag = true;
					}
				}
				else if (编队切换对象.transform.GetChild(4).GetComponent<Toggle>().isOn)
				{
					if (编队 == 4.0)
					{
						flag = true;
					}
				}
				else if (编队切换对象.transform.GetChild(5).GetComponent<Toggle>().isOn && 编队 == 5.0)
				{
					flag = true;
				}
				if (!flag)
				{
					continue;
				}
				GameObject gameObject;
				if (所有列表将领对象.Count <= num)
				{
					gameObject = UnityEngine.Object.Instantiate(编队将领对象.transform.GetChild(0).gameObject);
					gameObject.transform.SetParent(编队将领对象.transform);
					gameObject.transform.localScale = new Vector3(1f, 1f, 1f);
					所有列表将领对象.Add(gameObject);
                    foreach (var 按 in gameObject.GetComponentsInChildren<Button>(true)) 界面窗口管理器.注册运行时按钮(按);
				}
				else
				{
					gameObject = 所有列表将领对象[num];
				}
				gameObject.gameObject.SetActive(value: true);
                设置出征行布局(gameObject.transform);
				gameObject.transform.GetChild(8).gameObject.SetActive(value: false);
				将领属性库类 将领属性库类 = 全局将领库.查询指定ID的将领数据(全局变量.所有玩家数据表[第几个玩家].封地信息表[i].将领信息表[j].将领属性.初始属性.ID);
				if (将领属性库类 != null)
				{
					gameObject.transform.GetChild(1).GetChild(0).GetComponent<Image>()
						.sprite = 全局将领库.获取指定将领的头像(将领属性库类.名字);
					Animator component = gameObject.transform.GetChild(1).GetChild(1).GetComponent<Animator>();
					gameObject.transform.GetChild(1).GetChild(1).gameObject.SetActive(value: false);
					if (将领属性库类.头像特效 != 0.0)
					{
						gameObject.transform.GetChild(1).GetChild(1).gameObject.SetActive(value: true);
						component.SetInteger("特效类型", (int)将领属性库类.头像特效);
					}
				}
				Text component2 = gameObject.transform.GetChild(2).GetComponent<Text>();
				string 名字 = 全局变量.所有玩家数据表[第几个玩家].封地信息表[i].将领信息表[j].将领属性.初始属性.名字;
				double 等级 = 全局变量.所有玩家数据表[第几个玩家].封地信息表[i].将领信息表[j].将领属性.成长点数.等级;
				string text = 全局变量.所有玩家数据表[第几个玩家].封地信息表[i].将领信息表[j].将领属性.初始属性.获取职业名字();
				component2.text = 名字 + "(" + 等级.ToString() + "级" + text + ")";
                component2.supportRichText = false;
                出征标题原文[component2] = new 出征标题副本 { 原文 = component2.text };
				Text component3 = gameObject.transform.GetChild(4).GetComponent<Text>();
				double iD = 全局变量.所有玩家数据表[第几个玩家].封地信息表[i].将领信息表[j].将领配兵.ID;
				if (iD != 0.0)
				{
					double 数量 = 全局变量.所有玩家数据表[第几个玩家].封地信息表[i].将领信息表[j].将领配兵.数量;
					double 统兵 = 全局变量.所有玩家数据表[第几个玩家].封地信息表[i].将领信息表[j].将领属性.最终属性.统兵;
					string text2 = "未知";
					if (全局兵种库.查询指定ID的索引(iD) != -1)
					{
						text2 = 全局兵种库.属性表[全局兵种库.查询指定ID的索引(iD)].名称;
						Image component4 = gameObject.transform.GetChild(3).GetComponent<Image>();
						int num2 = 全局兵种库.查询指定兵种的图标(text2);
						if (num2 != -1)
						{
							component4.sprite = 全局变量.所有兵种图标资源表[num2];
						}
					}
					component3.text = 数量.ToString() + "/" + 统兵.ToString() + "(" + text2 + ")";
				}
				else
				{
					gameObject.transform.GetChild(8).gameObject.SetActive(value: true);
					component3.text = "未配兵";
				}
				Text component5 = gameObject.transform.GetChild(6).GetComponent<Text>();
				component5.text = 全局变量.所有玩家数据表[第几个玩家].封地信息表[i].将领信息表[j].详细信息.剩余体力.ToString() + "/" + 全局变量.所有玩家数据表[第几个玩家].封地信息表[i].将领信息表[j].将领属性.最终属性.体力上限.ToString();
				if (全局变量.所有玩家数据表[第几个玩家].封地信息表[i].将领信息表[j].详细信息.剩余体力 < 5.0)
				{
					gameObject.transform.GetChild(8).gameObject.SetActive(value: true);
				}
				if (全局变量.所有玩家数据表[第几个玩家].封地信息表[i].将领信息表[j].详细信息.状态 != 0.0)
				{
					gameObject.transform.GetChild(8).gameObject.SetActive(value: true);
				}
				if (i != 当前选中封地)
				{
					gameObject.transform.GetChild(8).gameObject.SetActive(value: true);
				}
				gameObject.transform.GetChild(7).gameObject.SetActive(value: false);
				if (!gameObject.transform.GetChild(8).gameObject.activeSelf)
				{
					if (gameObject.transform.GetChild(9).gameObject.activeSelf)
					{
						component2.color = 颜色类.GetColor("#49FBDA");
						component3.color = 颜色类.GetColor("#2DBE5A");
						component5.color = 颜色类.GetColor("#2DBE5A");
					}
					else
					{
						component2.color = 颜色类.GetColor("#C8C8C8");
						component3.color = 颜色类.GetColor("#C8C8C8");
						component5.color = 颜色类.GetColor("#C8C8C8");
					}
					bool flag2 = false;
					for (int k = 0; k < 已选中将领列表.Count; k++)
					{
						if (已选中将领列表[k].第几个将领 == j && 已选中将领列表[k].第几个封地 == i)
						{
							flag2 = true;
							break;
						}
					}
					if (flag2)
					{
						gameObject.transform.GetChild(7).gameObject.SetActive(value: true);
					}
				}
				已显示将领列表.Add(new 返回将领索引(i, j));
				if (已选中将领列表.Count > 4 && !gameObject.transform.GetChild(7).gameObject.activeSelf)
				{
					gameObject.transform.GetChild(8).gameObject.SetActive(value: true);
				}
				num++;
			}
		}
		空将领提示.gameObject.SetActive(num == 0);
		空将领提示.text = 当前选中封地 < 0 ? "暂无封地，请先建立封地。" : "当前没有可显示的将领，请先招募或切换编队。";
        var 格 = 编队将领对象.GetComponent<GridLayoutGroup>();
        var 内容框 = 编队将领对象.transform as RectTransform;
        if (格 != null && 内容框 != null)
        {
            内容框.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 格.padding.vertical + num * 格.cellSize.y + Mathf.Max(0, num - 1) * 格.spacing.y);
            LayoutRebuilder.ForceRebuildLayoutImmediate(内容框);
        }
        刷新和平驻防信息();
	}

	public void 输出列表()
	{
		for (int i = 0; i < 已选中将领列表.Count; i++)
		{
			UnityEngine.Debug.Log("已选：" + 已选中将领列表[i]?.ToString());
		}
		for (int j = 0; j < 已显示将领列表.Count; j++)
		{
			UnityEngine.Debug.Log("已显示" + 已显示将领列表[j]?.ToString());
		}
	}

	private void 隐藏所有列表将领对象()
	{
		已显示将领列表.Clear();
		int count = 所有列表将领对象.Count;
		for (int i = 0; i < count; i++)
		{
			所有列表将领对象[i].gameObject.SetActive(value: false);
		}
	}

    public void 勾选出征将领(int 勾选类型)
    {
        bool 单选 = 勾选类型 == 1;
        string 失败 = "";
        for (int i = 0; i < 所有列表将领对象.Count && i < 已显示将领列表.Count; i++)
        {
            var 行 = 所有列表将领对象[i];
            if (!行.activeSelf || (单选 && !行.transform.GetChild(9).gameObject.activeSelf)) continue;
            if (!单选 && 编队切换对象.transform.GetChild(0).GetComponent<Toggle>().isOn) continue;
            var 项 = 已显示将领列表[i];
            int 原位置 = 已选中将领列表.FindIndex(x => x.第几个封地 == 项.第几个封地 && x.第几个将领 == 项.第几个将领);
            if (原位置 >= 0)
            {
                选择快照.Remove(已选中将领列表[原位置]);
                已选中将领列表.RemoveAt(原位置);
                continue;
            }
            var 玩家 = 军事缺口入口.当前玩家();
            if (玩家 == null || 项.第几个封地 != 当前选中封地 || 项.第几个封地 < 0 || 项.第几个封地 >= 玩家.封地信息表.Count)
            { 失败 = "请在当前出发封地选择将领。"; continue; }
            var 地 = 玩家.封地信息表[项.第几个封地];
            if (项.第几个将领 < 0 || 项.第几个将领 >= 地.将领信息表.Count) { 失败 = "将领已变更，请重新选择。"; continue; }
            var 将 = 地.将领信息表[项.第几个将领];
            var 检查 = 军事本地规则.检查出征(玩家, new List<将领信息> { 将 });
            if (!检查.成功) { 失败 = 检查.说明; continue; }
            if (已选中将领列表.Count >= 5) { 失败 = "每次最多出征5名将领。"; continue; }
            已选中将领列表.Add(项);
            选择快照[项] = 将;
        }
        显示编队将领列表();
        if (单选 && 失败.Length > 0) 全局变量.提示类.显示信息(失败);
    }

    private void 选中将领批量补兵()
    {
        var 玩家 = 军事缺口入口.当前玩家();
        if (玩家 == null) return;
        if (已选中将领列表.Count == 0) { 全局变量.提示类.显示信息("请先选择要补兵的将领。"); return; }
        int 新增 = 0;
        string 失败 = "";
        foreach (var 项 in 已选中将领列表)
        {
            将领信息 将;
            if (!选择快照.TryGetValue(项, out 将) || 项.第几个封地 < 0 || 项.第几个封地 >= 玩家.封地信息表.Count)
            { 失败 = "将领或封地已变更。"; continue; }
            var 封地 = 玩家.封地信息表[项.第几个封地];
            int 兵种 = (int)将.将领配兵.ID;
            if (兵种 <= 0) { 失败 = "未配兵将领需先在将领页选择兵种。"; continue; }
            int 原数 = (int)将.将领配兵.数量;
            int 数量 = (int)Math.Floor(Math.Min(将.将领属性.最终属性.统兵, 将.将领配兵.数量 + 军事本地规则.闲兵数量(封地, 兵种)));
            var 结果 = 军事本地规则.配兵(玩家, 封地, 将, 兵种, 数量);
            if (结果.成功) 新增 += Math.Max(0, 数量 - 原数);
            else 失败 = 结果.说明;
        }
        全局变量.提示类.显示信息(新增 > 0 ? "已补充" + 新增 + "名士兵。" : 失败.Length > 0 ? 失败 : "当前兵力已满或本封地没有可用闲兵。");
    }

	public void 切换编队()
	{
		已选中将领列表.Clear();
        选择快照.Clear();
		隐藏所有列表将领对象();
		显示编队将领列表();
		勾选出征将领(0);
	}
}
