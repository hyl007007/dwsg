using Dwsg.Generals;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using 玩家数据结构;
using 缺失界面.窗口4;

public class 更换装备脚本 : MonoBehaviour
{
    public Image 已选中装备图片;
    public Text 已选中装备信息;
    public GameObject 装备列表对象;
    public 将领装备 已选中装备;
    public 将领装备 将领本来的装备信息;
    public int 第几个玩家;
    public int 第几个封地;
    public int 第几个将领;
    public int 第几个部位;
    public List<将领装备> 要显示的装备列表;
    public GameObject 将领界面UI对象;

    private 玩家数据 玩家快照;
    private 封地信息 封地快照;
    private 将领信息 将领快照;
    private readonly List<将领装备> 候选 = new List<将领装备>();
    private readonly Dictionary<Button, UnityAction> 监听 = new Dictionary<Button, UnityAction>();
    private float 原装备行高;
    private Canvas 装备画布;
    private bool 装备布局待更新;
    private int 装备布局请求帧;
    private int 上次屏幕宽, 上次屏幕高;
    private float 上次画布缩放 = -1;

    private void OnEnable()
    {
        装备画布 = GetComponentInParent<Canvas>();
        安排装备布局更新();
    }

    private void 安排装备布局更新()
    {
        装备布局待更新 = true;
        装备布局请求帧 = Time.frameCount;
    }

    private void LateUpdate()
    {
        int 宽 = Screen.width, 高 = Screen.height;
        float 缩放 = 装备画布 != null ? 装备画布.scaleFactor : 1;
        if (宽 != 上次屏幕宽 || 高 != 上次屏幕高 || Mathf.Abs(缩放 - 上次画布缩放) > .0001f)
        {
            上次屏幕宽 = 宽; 上次屏幕高 = 高; 上次画布缩放 = 缩放;
            安排装备布局更新();
        }
        if (!装备布局待更新 || Time.frameCount <= 装备布局请求帧) return;
        装备布局待更新 = false;
        更新装备展示布局();
    }

    private static float 边缘高度(RectTransform 框, Transform 父级, bool 顶部)
    {
        return 父级.InverseTransformPoint(框.TransformPoint(顶部 ? 框.rect.max : 框.rect.min)).y;
    }

    private static void 设置纵向范围(RectTransform 框, float 上界, float 下界)
    {
        float 高 = 上界 - 下界;
        float 原顶 = 边缘高度(框, 框.parent, true);
        var 位置 = 框.anchoredPosition;
        位置.y += 上界 - 原顶 - (高 - 框.rect.height) * (1 - 框.pivot.y);
        if (Mathf.Abs(高 - 框.rect.height) > .01f) 框.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 高);
        if (Mathf.Abs(位置.y - 框.anchoredPosition.y) > .01f) 框.anchoredPosition = 位置;
    }

    private void 更新装备展示布局()
    {
        if (装备列表对象 == null || 已选中装备信息 == null || 已选中装备图片 == null) return;
        var 格 = 装备列表对象.GetComponent<GridLayoutGroup>();
        var 列表 = transform.Find("装备列表布局") as RectTransform;
        var 滚动 = 列表 != null ? 列表.GetComponent<ScrollRect>() : null;
        if (格 == null || 滚动 == null || 滚动.viewport == null) return;
        if (原装备行高 <= 0) 原装备行高 = 格.cellSize.y;
        float 字高 = 0;
        foreach (Transform 行 in 装备列表对象.transform)
            if (行.gameObject.activeSelf)
                字高 = Mathf.Max(字高, Mathf.Ceil(行.GetChild(1).GetComponent<Text>().preferredHeight) + 2);
        float 行高 = Mathf.Max(原装备行高, 字高 + 8);
        if (Mathf.Abs(格.cellSize.y - 行高) > .01f) 格.cellSize = new Vector2(格.cellSize.x, 行高);
        foreach (Transform 行 in 装备列表对象.transform)
        {
            if (!行.gameObject.activeSelf) continue;
            var 字框 = 行.GetChild(1) as RectTransform;
            if (Mathf.Abs(字框.rect.height - 字高) > .01f) 字框.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 字高);
            if (Mathf.Abs(字框.anchoredPosition.y) > .01f)
            { var 字位置 = 字框.anchoredPosition; 字位置.y = 0; 字框.anchoredPosition = 字位置; }
            var 选中框 = 行.GetChild(0) as RectTransform;
            if (Mathf.Abs(选中框.rect.height - (行高 - 4)) > .01f)
                选中框.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 行高 - 4);
            foreach (RectTransform 图 in 选中框)
            {
                if (Mathf.Abs(图.rect.height - (行高 - 4)) > .01f)
                    图.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 行高 - 4);
                float 图高位置 = -(行高 - 4) / 2;
                if (Mathf.Abs(图.anchoredPosition.y - 图高位置) > .01f)
                { var 图位置 = 图.anchoredPosition; 图位置.y = 图高位置; 图.anchoredPosition = 图位置; }
            }
        }

        var 说明框 = 已选中装备信息.rectTransform;
        float 上界 = 边缘高度(已选中装备图片.rectTransform, 说明框.parent, true);
        float 下界 = 边缘高度(列表, 说明框.parent, false);
        float 高 = Mathf.Min(Mathf.Max(已选中装备图片.rectTransform.rect.height,
            Mathf.Ceil(已选中装备信息.preferredHeight) + 2), 上界 - 下界 - 行高 - 6);
        if (高 <= 0) return;
        设置纵向范围(说明框, 上界, 上界 - 高);
        float 列表顶 = 上界 - 高 - 6;
        float 位移 = 列表顶 - 边缘高度(列表, 说明框.parent, true);
        if (Mathf.Abs(位移) <= .01f) return;
        // Keep the original content at the viewport's upper edge without resetting
        // its current scroll offset when the description or resolution changes.
        var 内容 = 滚动.content;
        if (内容 != null && (内容.anchorMin.y != 1 || 内容.anchorMax.y != 1))
        {
            float 偏移 = 边缘高度(内容, 滚动.viewport, true) - 滚动.viewport.rect.yMax;
            内容.anchorMin = new Vector2(内容.anchorMin.x, 1);
            内容.anchorMax = new Vector2(内容.anchorMax.x, 1);
            var 位置 = 内容.anchoredPosition; 位置.y = 偏移; 内容.anchoredPosition = 位置;
        }
        float 原列表高 = 列表.rect.height;
        float 新列表高 = 列表顶 - 下界;
        foreach (string 名 in new[] { "通用透黑背景 (1)", "通用透黑背景 (2)" })
        {
            var 背景 = transform.Find(名) as RectTransform;
            if (背景 != null) 设置纵向范围(背景,
                边缘高度(背景, 背景.parent, true) + 位移, 边缘高度(背景, 背景.parent, false));
        }
        设置纵向范围(列表, 列表顶, 下界);
        if (Mathf.Abs(滚动.viewport.rect.height - 新列表高) > .01f)
            滚动.viewport.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 新列表高);
        if (滚动.verticalScrollbar != null)
        {
            var 滚条 = 滚动.verticalScrollbar.transform as RectTransform;
            float 滚条高 = Mathf.Max(0, 滚条.rect.height + 新列表高 - 原列表高);
            if (Mathf.Abs(滚条.rect.height - 滚条高) > .01f)
                滚条.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 滚条高);
        }
    }

    public void 刷新显示()
    {
        安排装备布局更新();
        玩家快照 = 军事缺口入口.当前玩家();
        封地快照 = 玩家快照 != null && 第几个封地 >= 0 && 第几个封地 < 玩家快照.封地信息表.Count ? 玩家快照.封地信息表[第几个封地] : null;
        将领快照 = 封地快照 != null && 第几个将领 >= 0 && 第几个将领 < 封地快照.将领信息表.Count ? 封地快照.将领信息表[第几个将领] : null;
        已选中装备 = null;
        候选.Clear();
        已选中装备图片.enabled = false;
        if (已选中装备图片.transform.childCount > 0) 已选中装备图片.transform.GetChild(0).gameObject.SetActive(false);
        foreach (Transform 行 in 装备列表对象.transform)
        {
            行.GetChild(0).gameObject.SetActive(false);
            var 勾选 = 行.GetComponent<Toggle>();
            if (勾选 != null) 勾选.SetIsOnWithoutNotify(false);
            行.gameObject.SetActive(false);
        }
        var 检查 = 军事本地规则.检查将领(玩家快照, 封地快照, 将领快照);
        if (!检查.成功) { 已选中装备信息.text = 检查.说明; return; }
        要显示的装备列表 = 玩家快照.背包装备列表.获取指定部位列表(第几个部位);
        if (要显示的装备列表 != null)
            foreach (var 装备 in 要显示的装备列表)
                if (装备 != null && 装备.装备信息 != null && 装备.将领ID == -1 && 装备.装备信息.名称 != "空") 候选.Add(装备);
        已选中装备信息.text = 候选.Count == 0 ? "背包没有此部位的闲置装备。" : "选择装备查看属性，再次点击穿戴。";
        for (int i = 0; i < 候选.Count; i++)
        {
            if (i >= 装备列表对象.transform.childCount)
            {
                var 新行 = Instantiate(装备列表对象.transform.GetChild(0).gameObject, 装备列表对象.transform, false);
                // 克隆仅继承模板的持久事件，不继承该行上次注册的运行时回调。
                新行.GetComponent<Button>().onClick.RemoveAllListeners();
            }
            var 行 = 装备列表对象.transform.GetChild(i);
            var 按钮 = 行.GetComponent<Button>();
            UnityAction 旧;
            if (监听.TryGetValue(按钮, out 旧)) 按钮.onClick.RemoveListener(旧);
            int 行号 = i;
            UnityAction 新 = () => 点击装备(行号);
            监听[按钮] = 新;
            按钮.onClick.AddListener(新);
            界面窗口管理器.注册运行时按钮(按钮);
            var 字 = 行.GetChild(1).GetComponent<Text>();
            字.text = 候选[i].获取装备简单信息文本();
            字.color = 候选[i].获取装备文字颜色();
            行.gameObject.SetActive(true);
            军事界面样式.限定名称(字);
        }
    }

    public void 获取选中装备()
    {
        for (int i = 0; i < 候选.Count; i++)
        {
            var 行 = 装备列表对象.transform.GetChild(i);
            var 勾选 = 行.GetComponent<Toggle>();
            if (行.gameObject.activeSelf && 勾选 != null && 勾选.isOn) { 已选中装备 = 候选[i]; return; }
        }
        已选中装备 = null;
    }

    public void 点击装备(int 第几个装备)
    {
        if (第几个装备 < 0 || 第几个装备 >= 候选.Count) return;
        if (!ReferenceEquals(玩家快照, 军事缺口入口.当前玩家())) { 全局变量.提示类.显示信息("角色已变更，请重新打开装备列表。"); return; }
        var 检查 = 军事本地规则.检查将领(玩家快照, 封地快照, 将领快照);
        if (!检查.成功) { 全局变量.提示类.显示信息(检查.说明); return; }
        var 装备 = 候选[第几个装备];
        var 行 = 装备列表对象.transform.GetChild(第几个装备);
        if (ReferenceEquals(已选中装备, 装备) && 行.GetChild(0).gameObject.activeSelf)
        {
            if (Dwsg.Network.GameNetwork.Enabled)
            {
                int inventoryIndex = GeneralLegacyAdapter.Equipment(玩家快照, 第几个部位).IndexOf(装备);
                GeneralsClientAdapter.Execute(全局变量.本机身份, 将领快照.ID, "generals.equip",
                    new JObject { ["equipmentSlot"] = 第几个部位, ["equipmentIndex"] = inventoryIndex, ["releaseTroops"] = 将领界面UI对象 != null && 将领界面UI对象.activeSelf }, () =>
                    {
                        if (this == null) return;
                        gameObject.SetActive(false);
                        var list = 将领界面UI对象 == null ? null : 将领界面UI对象.GetComponent<将领列表显示>();
                        if (list != null) { list.刷新将领属性信息(); list.刷新列表信息(); }
                    });
                return;
            }
            var 结果 = 将领流程规则.穿戴(玩家快照, 封地快照, 将领快照, 第几个部位, 装备);
            全局变量.提示类.显示信息(结果.说明);
            if (!结果.成功) return;
            将领流程规则.更新装备属性(玩家快照, 封地快照, 将领快照);
            gameObject.SetActive(false);
            return;
        }
        foreach (Transform 其他行 in 装备列表对象.transform) 其他行.GetChild(0).gameObject.SetActive(false);
        行.GetChild(0).gameObject.SetActive(true);
        已选中装备 = 装备;
        已选中装备图片.enabled = true;
        已选中装备图片.sprite = 装备.获取装备头像();
        if (已选中装备图片.transform.childCount > 0)
            已选中装备图片.transform.GetChild(0).gameObject.SetActive(装备.获取装备名字().Contains("尊"));
        将领本来的装备信息 = 玩家快照.背包装备列表.寻找指定将领的装备(第几个部位, 将领快照.ID);
        double 差 = 装备.获取装备加成数字() - (将领本来的装备信息 != null ? 将领本来的装备信息.获取装备加成数字() : 0);
        已选中装备信息.text = 装备.获取装备简单信息文本() + "\n" + 装备.获取装备强化效果文本() + (差 >= 0 ? "↑" : "↓") + System.Math.Abs(差).ToString("0.##") + "\n再次点击穿戴";
        已选中装备信息.color = 装备.获取装备文字颜色();
        安排装备布局更新();
    }
}
