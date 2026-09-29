using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using Dwsg.Network;
using Dwsg.Auxiliary;
using Dwsg.Shared;

public class 赌场脚本 : MonoBehaviour
{
    public Text 小金额;
    public Text 豹子金额;
    public Text 大金额;
    private readonly InputField[] 金额输入 = new InputField[3];
    private bool 联机下注中;
    private Text 联机结果文本;
    private long 上次联机显示秒 = -1;

    private void Awake()
    {
        Transform 下注布局 = transform.Find("赌场说明/下注布局");
        Button 容器按钮 = 下注布局 == null ? null : 下注布局.GetComponent<Button>();
        // 原布局容器没有点击动作，仅停用 Button，保留背景和真正的子按钮。
        if (容器按钮 != null && 容器按钮.onClick.GetPersistentEventCount() == 0)
            容器按钮.enabled = false;
        安装下注按钮居中(下注布局);
        安装金额输入();
        绑定结果显示();
    }

    private void OnEnable()
    {
        绑定结果显示();
        GameNetwork.SnapshotReceived -= 联机快照更新;
        GameNetwork.SnapshotReceived += 联机快照更新;
        联机快照更新(GameNetwork.CurrentSnapshot);
    }
    private void OnDisable() { GameNetwork.SnapshotReceived -= 联机快照更新; }
    private void 联机快照更新(WorldSnapshot snapshot)
    {
        if (!GameNetwork.Enabled || !isActiveAndEnabled || 联机结果文本 == null) return;
        string 内容 = AuxiliaryClient.CasinoSummary();
        if (联机结果文本.text != 内容) 联机结果文本.text = 内容;
    }

    private void Update()
    {
        if (!GameNetwork.Enabled) 赌场状态显示.刷新();
        else
        {
            long 秒 = (GameNetwork.CurrentSnapshot == null ? 0 : GameNetwork.CurrentSnapshot.ServerUtcMs) / 1000;
            if (上次联机显示秒 != 秒) { 上次联机显示秒 = 秒; 联机快照更新(GameNetwork.CurrentSnapshot); }
        }
    }

    private void 绑定结果显示()
    {
        Transform 结果 = transform.Find("赌场说明/结果显示");
        if (结果 == null) return;
        Text 文字 = 结果.GetComponent<Text>();
        if (文字 == null) return;
        联机结果文本 = 文字;
        RectTransform 框 = 文字.rectTransform;
        // 原15号字体六行结算实测需131高；保留上缘，向下扩展仍距金额标签24以上。
        float 增高 = Mathf.Max(0, 144 - 框.rect.height);
        if (增高 > 0)
        {
            框.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 框.rect.height + 增高);
            框.anchoredPosition -= new Vector2(0, 增高 * (1 - 框.pivot.y));
        }
        if (!GameNetwork.Enabled) 赌场状态显示.绑定(文字);
    }

    private static void 安装下注按钮居中(Transform 下注布局)
    {
        if (下注布局 == null) return;
        foreach (string 路径 in new[] { "下注按钮/Text (Legacy)", "下注按钮 (1)/Text (Legacy)", "下注按钮 (2)/Text (Legacy)" })
        {
            Transform 标签 = 下注布局.Find(路径);
            Text 文字 = 标签 == null ? null : 标签.GetComponent<Text>();
            // 只修正原绿色标签的字形几何，不改字色、字重、阴影或按钮布局。
            if (文字 != null && 文字.GetComponent<按钮字形垂直居中>() == null)
                文字.gameObject.AddComponent<按钮字形垂直居中>();
        }
    }

    private void 安装金额输入()
    {
        Text[] 显示列表 = { 小金额, 大金额, 豹子金额 };
        for (int i = 0; i < 显示列表.Length; i++)
        {
            if (金额输入[i] != null || 显示列表[i] == null) continue;
            // 原场景的金额显示是对应 InputField 的子对象，无需另建输入控件。
            InputField 输入 = 显示列表[i].GetComponentInParent<InputField>(true);
            if (输入 == null) continue;
            输入.contentType = InputField.ContentType.IntegerNumber;
            输入.characterValidation = InputField.CharacterValidation.Integer;
            输入.characterLimit = 10;
            Text 显示 = 显示列表[i];
            // InputField 自己的 Text 是唯一可见金额；旧 Text 留作原回调目标。
            if (输入.textComponent != null)
            {
                输入.textComponent.enabled = true;
                if (显示 != 输入.textComponent) 显示.enabled = false;
            }
            输入.onValueChanged.AddListener(值 => 显示.text = 值);
            显示.text = 输入.text;
            金额输入[i] = 输入;
        }
    }

    public void 开始下注(int id)
    {
        添加下注到赌场(id);
    }
    
    private void 添加下注到赌场(int type)
    {
        if (type < 0 || type >= 金额输入.Length)
        {
            全局变量.提示类.显示信息("下注类别无效");
            return;
        }
        if (GameNetwork.Enabled) { 添加联机下注(type); return; }
        if (全局变量.赌场是否开始)
        {
            全局变量.提示类.显示信息("已经开始无法下注");
            return;
        }
        for (int i = 0; i < 全局变量.当局赌场下注列表.Count; i++)
        {
            if (全局变量.当局赌场下注列表[i].下注类型 == type)
            {
                全局变量.提示类.显示信息("本局该类别已下注，请等待结算");
                return;
            }
        }
        安装金额输入();
        InputField 当前输入 = 金额输入[type];
        if (当前输入 == null)
        {
            全局变量.提示类.显示信息("金额输入不可用");
            return;
        }
        int 下注金额;
        // 直接读取当前输入，未回车或失焦时也不能使用旧显示金额。
        if (!int.TryParse(当前输入.text, NumberStyles.None, CultureInfo.InvariantCulture, out 下注金额) || 下注金额 <= 0)
        {
            全局变量.提示类.显示信息("下注金额须为1至2147483647的正整数");
            return;
        }
        if (!(全局变量.所有玩家数据表[全局变量.本机身份].财产信息.黄金 >= 下注金额))
        {
            全局变量.提示类.显示信息("余额不足下注失败");
            return;
        }
        全局变量.当局赌场下注列表.Add(new 赌场信息(type, 下注金额));
        全局变量.所有玩家数据表[全局变量.本机身份].财产信息.黄金 -= 下注金额;
        全局变量.提示类.显示信息($"下注成功{下注金额}");
        赌场状态显示.刷新();
    }
    private void 添加联机下注(int type)
    {
        if (联机下注中) return;
        安装金额输入();
        int 金额;
        if (金额输入[type] == null || !int.TryParse(金额输入[type].text, NumberStyles.None, CultureInfo.InvariantCulture, out 金额) || 金额 <= 0)
        { 全局变量.提示类.显示信息("下注金额须为1至2147483647的正整数"); return; }
        if (!GameNetwork.Connected) { 全局变量.提示类.显示信息("尚未连接服务器，请等待重连"); return; }
        联机下注中 = true; 设置下注交互(false);
        AuxiliaryClient.Bet(type, 金额, result => {
            if (this == null) return;
            联机下注中 = false; 设置下注交互(true);
            全局变量.提示类.显示信息(result.Code == GameCodes.Ok ? "下注成功" + 金额 : result.Message);
            联机快照更新(GameNetwork.CurrentSnapshot);
        });
    }
    private void 设置下注交互(bool 可用)
    {
        var 布局 = transform.Find("赌场说明/下注布局");
        if (布局 == null) return;
        var 交互 = 布局.GetComponent<CanvasGroup>();
        if (交互 == null) 交互 = 布局.gameObject.AddComponent<CanvasGroup>();
        交互.interactable = 可用;
    }
}

// 状态只由原开奖协程通知；关窗、重开和刷新文字不启动开奖或变动黄金。
internal static class 赌场状态显示
{
    private static readonly string[] 类别名称 = { "压小", "压大", "豹子" };
    private static readonly List<赌场信息> 本局投注 = new List<赌场信息>();
    private static readonly Dictionary<int, double> 本局返还 = new Dictionary<int, double>();
    private static Text 结果文本;
    private static float 下次检查时间 = -1;
    private static float 开奖结束时间;
    private static bool 正在开奖;
    private static string 最近结算 = string.Empty;

    internal static void 绑定(Text 文本)
    {
        if (文本 == null) return;
        if (结果文本 != 文本) 初始化(文本);
        else 刷新();
    }

    internal static void 初始化(Text 文本)
    {
        结果文本 = 文本;
        下次检查时间 = -1;
        正在开奖 = false;
        最近结算 = string.Empty;
        本局投注.Clear();
        本局返还.Clear();
        刷新();
    }

    internal static void 等待检查(float 秒数)
    {
        下次检查时间 = Time.time + 秒数;
        刷新();
    }

    internal static void 开始本局(float 秒数)
    {
        正在开奖 = true;
        开奖结束时间 = Time.time + 秒数;
        最近结算 = string.Empty;
        本局投注.Clear();
        本局返还.Clear();
        foreach (赌场信息 投注 in 全局变量.当局赌场下注列表)
            本局投注.Add(new 赌场信息(投注.下注类型, 投注.下注金额));
        刷新();
    }

    internal static void 记录返还(int 类别, double 金额)
    {
        if (!正在开奖) return;
        double 已返还;
        本局返还.TryGetValue(类别, out 已返还);
        本局返还[类别] = 已返还 + 金额;
    }

    internal static void 结束本局(int 骰子1, int 骰子2, int 骰子3)
    {
        if (!正在开奖) return;
        正在开奖 = false;
        var 文本 = new StringBuilder();
        文本.Append("已结算 · 开奖 ").Append(骰子1).Append('·').Append(骰子2).Append('·').Append(骰子3)
            .Append(骰子1 + 骰子2 + 骰子3 < 11 ? "（小" : "（大");
        if (骰子1 == 骰子2 && 骰子2 == 骰子3) 文本.Append("、豹子");
        文本.Append("）");
        long 总下注 = 0;
        double 总返还 = 0;
        foreach (赌场信息 投注 in 本局投注)
        {
            double 返还;
            本局返还.TryGetValue(投注.下注类型, out 返还);
            总下注 += 投注.下注金额;
            总返还 += 返还;
            文本.Append('\n').Append(类别名称[投注.下注类型]).Append(' ').Append(投注.下注金额);
            if (返还 > 0) 文本.Append(" · 返还 ").Append(钱数(返还));
            else 文本.Append(" · 输 ").Append(投注.下注金额);
        }
        文本.Append("\n合计下注 ").Append(总下注).Append(" · 返还 ").Append(钱数(总返还));
        double 净额 = Math.Round(总返还 - 总下注, 2, MidpointRounding.AwayFromZero);
        文本.Append('\n').Append(净额 > 0 ? "本局净赚 " : 净额 < 0 ? "本局净亏 " : "本局持平 ")
            .Append(钱数(Math.Abs(净额))).Append(" 黄金");
        最近结算 = 文本.ToString();
        刷新();
    }

    internal static void 刷新()
    {
        if (结果文本 == null) return;
        string 内容;
        if (全局变量.赌场是否开始)
        {
            string 状态 = 正在开奖 ? "开奖中 · 剩余 " + 剩余秒(开奖结束时间) + " 秒" : "开奖中";
            内容 = 投注说明(状态, 正在开奖 ? 本局投注 : 全局变量.当局赌场下注列表);
        }
        else if (全局变量.当局赌场下注列表.Count > 0)
        {
            string 状态 = 下次检查时间 >= 0 ? "等待开局 · 剩余 " + 剩余秒(下次检查时间) + " 秒" : "已下注 · 等待开局";
            内容 = 投注说明(状态, 全局变量.当局赌场下注列表);
        }
        else 内容 = string.IsNullOrEmpty(最近结算)
            ? "尚未下注\n输入金额后选择下注类别\n大小返还1.2倍 · 豹子返还1.5倍"
            : 最近结算;
        if (结果文本.text != 内容) 结果文本.text = 内容;
    }

    private static string 投注说明(string 状态, List<赌场信息> 投注列表)
    {
        var 文本 = new StringBuilder(状态);
        long 总下注 = 0;
        foreach (赌场信息 投注 in 投注列表)
        {
            总下注 += 投注.下注金额;
            文本.Append('\n').Append(类别名称[投注.下注类型]).Append(' ').Append(投注.下注金额);
        }
        return 文本.Append("\n本局已扣 ").Append(总下注).Append(" 黄金").ToString();
    }

    private static int 剩余秒(float 时间)
    { return (int)Math.Ceiling(Math.Max(0, 时间 - Time.time)); }
    private static string 钱数(double 金额)
    { return 金额.ToString("0.##", CultureInfo.InvariantCulture); }
}

public class 赌场信息
{
    public int 下注类型;
    public int 下注金额;

    public 赌场信息(int type,int money)
    {
        下注类型 = type;
        下注金额 = money;
    }
}
