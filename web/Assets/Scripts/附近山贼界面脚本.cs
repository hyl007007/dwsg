using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using 缺失界面.窗口4;

public class 附近山贼界面脚本 : MonoBehaviour
{
    public int 起始坐标x = 10;
    public int 起始坐标y = 10;
    public int 显示数量 = 5;
    public int 已选中坐标x = -1;
    public int 已选中坐标y = -1;
    public GameObject 山贼列表对象;
    public GameObject 山贼信息对象;
    public GameObject 选择出征将领界面对象;

    private readonly List<山贼属性信息> 显示列表 = new List<山贼属性信息>();
    private 山贼属性信息 选中;
    private int 等级筛选;
    private int 第几页;
    private bool 查找模式;
    private bool 已绑定;
    private Text 空状态;
    private GameObject 查找面板;
    private InputField 等级输入;
    private Text 查找反馈;

    private void OnEnable()
    {
        if (!已绑定)
        {
            已绑定 = true;
            var 查找按钮 = transform.Find("剿灭山贼界面操作/查找");
            if (查找按钮 != null)
            {
                var 按钮 = 查找按钮.GetComponent<Button>();
                if (按钮 != null) { 按钮.onClick.AddListener(打开查找); 界面窗口管理器.注册运行时按钮(按钮); }
            }
            var 样式 = new 军事界面样式(transform);
            空状态 = 样式.文本(山贼信息对象.transform.parent, "山贼空状态", "请选择山贼查看详情。", Vector2.zero, Vector2.zero, 18);
            var 原框 = 山贼信息对象.transform as RectTransform;
            空状态.rectTransform.anchorMin = 原框.anchorMin;
            空状态.rectTransform.anchorMax = 原框.anchorMax;
            空状态.rectTransform.pivot = 原框.pivot;
            空状态.rectTransform.anchoredPosition = 原框.anchoredPosition;
            空状态.rectTransform.sizeDelta = 原框.sizeDelta;
            空状态.alignment = TextAnchor.MiddleCenter;
        }
        刷新山贼列表();
    }

    private void OnDisable() { if (查找面板 != null) 查找面板.SetActive(false); }

    public void 刷新山贼列表()
    {
        显示列表.Clear();
        int 行数 = Mathf.Min(Mathf.Max(1, 显示数量), 山贼列表对象.transform.childCount);
        起始坐标x = Mathf.Clamp(起始坐标x, 0, Mathf.Max(0, 全局变量.横向山贼数量 - 1));
        起始坐标y = Mathf.Clamp(起始坐标y, 0, Mathf.Max(0, 全局变量.竖向山贼数量 - 1));
        if (查找模式)
        {
            var 结果 = 山贼查找规则.查询(全局变量.所有山贼数据列表, 等级筛选, 起始坐标x, 起始坐标y);
            第几页 = Mathf.Clamp(第几页, 0, Mathf.Max(0, (结果.Count - 1) / 行数));
            显示列表.AddRange(结果.Skip(第几页 * 行数).Take(行数));
        }
        else
            for (int i = 0; i < 行数; i++)
            {
                var 贼 = 附近山贼.获取指定坐标的山贼(起始坐标x, 起始坐标y + i);
                if (贼 != null) 显示列表.Add(贼);
            }
        if (!显示列表.Contains(选中)) 选中 = null;
        for (int i = 0; i < 山贼列表对象.transform.childCount; i++)
        {
            var 行 = 山贼列表对象.transform.GetChild(i);
            bool 有数据 = i < 显示列表.Count;
            行.gameObject.SetActive(有数据);
            行.GetChild(5).gameObject.SetActive(有数据 && ReferenceEquals(显示列表[i], 选中));
            var 勾选 = 行.GetComponent<Toggle>();
            if (勾选 != null) 勾选.SetIsOnWithoutNotify(有数据 && ReferenceEquals(显示列表[i], 选中));
            if (!有数据) continue;
            var 贼 = 显示列表[i];
            int 头像号 = (int)贼.等级 - 1;
            var 头像 = 行.GetChild(1).GetComponent<Image>();
            头像.enabled = 全局变量.山贼头像资源表 != null && 头像号 >= 0 && 头像号 < 全局变量.山贼头像资源表.Length;
            if (头像.enabled) 头像.sprite = 全局变量.山贼头像资源表[头像号];
            行.GetChild(2).GetComponent<Text>().text = 贼.等级.ToString("0") + "级山贼 (" + 贼.坐标x + "," + 贼.坐标y + ")";
            行.GetChild(3).GetComponent<Text>().text = "兵力：" + 兵力(贼).ToString("0");
            行.GetChild(4).GetComponent<Text>().text = 掉落(贼);
        }
        显示选中详情();
    }

    private static double 兵力(山贼属性信息 贼)
    {
        return 贼.将领数据列表 == null ? 0 : 贼.将领数据列表.Where(x => x != null && x.将领配兵 != null).Sum(x => System.Math.Max(0, x.将领配兵.数量));
    }
    private static string 掉落(山贼属性信息 贼)
    {
        return "资源" + (贼.掉落宝物 == 1 ? "、宝物" : "") + (贼.掉落宝箱 == 1 ? "、宝箱" : "") + (贼.掉落装备 == 1 ? "、装备" : "");
    }

    public void 刷新显示选中山贼()
    {
        for (int i = 0; i < 显示列表.Count; i++)
            if (山贼列表对象.transform.GetChild(i).GetChild(5).gameObject.activeSelf) { 选中 = 显示列表[i]; break; }
        显示选中详情();
    }

    private void 显示选中详情()
    {
        bool 有数据 = 选中 != null && 全局变量.所有山贼数据列表.Contains(选中);
        山贼信息对象.SetActive(有数据);
        if (空状态 != null)
        {
            空状态.text = 显示列表.Count == 0 ? "没有符合条件的山贼，请重新查找。" : "请选择山贼查看详情。";
            空状态.gameObject.SetActive(!有数据);
        }
        if (!有数据) { 已选中坐标x = 已选中坐标y = -1; return; }
        已选中坐标x = 选中.坐标x;
        已选中坐标y = 选中.坐标y;
        int 头像号 = (int)选中.等级 - 1;
        var 头像 = 山贼信息对象.transform.GetChild(0).GetChild(0).GetComponent<Image>();
        头像.enabled = 全局变量.山贼头像资源表 != null && 头像号 >= 0 && 头像号 < 全局变量.山贼头像资源表.Length;
        if (头像.enabled) 头像.sprite = 全局变量.山贼头像资源表[头像号];
        山贼信息对象.transform.GetChild(0).GetChild(1).GetComponent<Text>().text = 选中.等级.ToString("0") + "级山贼 (" + 已选中坐标x + "," + 已选中坐标y + ")";
        山贼信息对象.transform.GetChild(0).GetChild(2).GetComponent<Text>().text = "兵力：" + 兵力(选中).ToString("0");
        山贼信息对象.transform.GetChild(0).GetChild(3).GetComponent<Text>().text = 掉落(选中);
        var 阵容 = new List<string>();
        double 最高等级 = 0;
        if (选中.将领数据列表 != null)
            foreach (var 将 in 选中.将领数据列表)
            {
                if (将 == null || 将.将领配兵 == null || 将.将领属性 == null) continue;
                var 兵 = 全局兵种库.查询指定ID的数据(将.将领配兵.ID);
                阵容.Add((兵 != null ? 兵.名称 : "未知兵种") + " " + 将.将领配兵.数量.ToString("0"));
                最高等级 = System.Math.Max(最高等级, 将.将领属性.成长点数.等级);
            }
        山贼信息对象.transform.GetChild(2).GetChild(0).GetComponent<Text>().text = "敌军等级：" + 最高等级.ToString("0") + "级";
        山贼信息对象.transform.GetChild(2).GetChild(1).GetComponent<Text>().text = "敌军规模：" + 阵容.Count + "名";
        山贼信息对象.transform.GetChild(2).GetChild(3).GetChild(0).GetComponent<Text>().text = 阵容.Count > 0 ? string.Join("\n", 阵容) : "暂无部队";
    }

    private void 翻页(int 横, int 竖)
    {
        选中 = null;
        if (查找模式) 第几页 = Mathf.Max(0, 第几页 + 横 + 竖);
        else
        {
            起始坐标x += 横;
            起始坐标y += 竖 * Mathf.Max(1, 显示数量);
        }
        刷新山贼列表();
    }
    public void 往上翻页() { 翻页(0, -1); }
    public void 往下翻页() { 翻页(0, 1); }
    public void 往左翻页() { 翻页(-1, 0); }
    public void 往右翻页() { 翻页(1, 0); }

    public void 出征剿灭选中山贼()
    {
        if (选中 == null || !全局变量.所有山贼数据列表.Contains(选中) || 选中.将领数据列表 == null || 选中.将领数据列表.Count == 0)
        { 全局变量.提示类.显示信息("请先选择有部队的山贼。"); return; }
        var 脚本 = 选择出征将领界面对象.GetComponent<选择出征将领>();
        脚本.山贼坐标x = 选中.坐标x;
        脚本.山贼坐标y = 选中.坐标y;
        选择出征将领界面对象.SetActive(true);
        脚本.切换出征封地(全局变量.第几个封地);
        脚本.刷新山贼();
    }

    public void 打开查找()
    {
        if (查找面板 == null)
        {
            var 原窗口 = 军事界面样式.原改名窗口(transform);
            if (原窗口 == null)
            {
                全局变量.提示类.显示信息("查找窗口暂时无法打开。");
                return;
            }
            var 样式 = new 军事界面样式(transform);
            var 遮罩 = 军事界面样式.矩形("山贼等级查找", transform, Vector2.zero, Vector2.zero);
            查找面板 = 遮罩.gameObject;
            查找面板.SetActive(false);
            var 弹窗 = 查找面板.AddComponent<原界面小弹窗>();
            弹窗.初始化(原窗口, "查找山贼", () => 查找面板.SetActive(false), 260);
            var 行 = 军事界面样式.矩形("等级区域", 弹窗.内容, new Vector2(342, 38), Vector2.zero);
            行.gameObject.AddComponent<LayoutElement>().preferredHeight = 38;
            var 横排 = 行.gameObject.AddComponent<HorizontalLayoutGroup>();
            横排.childAlignment = TextAnchor.MiddleCenter;
            横排.spacing = 8;
            横排.childControlWidth = 横排.childControlHeight = true;
            横排.childForceExpandWidth = 横排.childForceExpandHeight = false;
            var 标签 = 弹窗.添加说明("等级标签", "等级（1–10，留空全部）", 16);
            标签.transform.SetParent(行, false);
            标签.alignment = TextAnchor.MiddleLeft;
            var 标签尺寸 = 标签.gameObject.AddComponent<LayoutElement>();
            标签尺寸.minWidth = 0;
            标签尺寸.preferredWidth = 268;
            标签尺寸.flexibleWidth = 1;
            var 输入框 = 军事界面样式.矩形("等级输入", 行, new Vector2(66, 34), Vector2.zero);
            var 输入尺寸 = 输入框.gameObject.AddComponent<LayoutElement>();
            输入尺寸.minWidth = 输入尺寸.preferredWidth = 66;
            输入尺寸.minHeight = 输入尺寸.preferredHeight = 34;
            等级输入 = 样式.原样输入(输入框, "", true, 2);
            查找反馈 = 弹窗.添加说明("反馈", "", 16);
            查找反馈.gameObject.AddComponent<LayoutElement>().minHeight = 50;
            var 查找按钮 = 弹窗.添加按钮("查找", "查找", () => {
                int 等级 = 0;
                string 文 = 等级输入.text.Trim();
                if (文.Length > 0 && (!int.TryParse(文, out 等级) || 等级 < 1 || 等级 > 10))
                { 查找反馈.text = "请输入1至10的等级，或留空查看全部。"; return; }
                等级筛选 = 等级;
                查找模式 = true;
                第几页 = 0;
                选中 = null;
                刷新山贼列表();
                查找面板.SetActive(false);
                全局变量.提示类.显示信息(显示列表.Count > 0 ? "已显示查找结果，可用方向按钮翻页。" : "没有符合等级的山贼。");
            });
            var 取消按钮 = 弹窗.添加按钮("取消", "取消", () => 查找面板.SetActive(false));
            界面窗口管理器.注册运行时按钮(查找按钮);
            界面窗口管理器.注册运行时按钮(取消按钮);
            界面窗口管理器.注册运行时按钮(弹窗.关闭);
        }
        等级输入.SetTextWithoutNotify(等级筛选 == 0 ? "" : 等级筛选.ToString());
        查找反馈.text = "按等级筛选当前世界的山贼。";
        查找面板.SetActive(true);
        查找面板.transform.SetAsLastSibling();
    }
}
