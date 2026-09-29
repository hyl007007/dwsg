using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using 玩家数据结构;
using 缺失界面.窗口4;

public class 炼魂脚本 : MonoBehaviour
{
    public Image 装备头像;
    public Text 装备信息显示;
    public GameObject 炼魂列表对象;
    public 将领装备 装备对象;
    public Text 炼魂材料显示;
    public Text 锁定材料显示;
    public Toggle 普通炼魂选中;
    public Toggle 高级炼魂选中;
    public Toggle 高级炼魂选中1;
    private bool 材料按钮已绑定;
    private Text 炼魂反馈显示;
    private 将领装备 反馈装备;

    private void OnEnable()
    {
        准备炼魂反馈();
        清除炼魂反馈();
        对齐批量炼魂文字();
        if (!材料按钮已绑定)
        {
            绑定材料按钮("炼魂材料信息布局/需要材料布局/材料1加号", 打开炼魂材料);
            绑定材料按钮("炼魂材料信息布局/需要材料布局/材料2加号", 打开锁定材料);
            材料按钮已绑定 = true;
        }
        if (高级炼魂选中1 == null) return;
        bool 原玉石 = 高级炼魂选中1.isOn;
        高级炼魂选中1.SetIsOnWithoutNotify(false);
        高级炼魂选中1.interactable = false;
        var 标签 = 高级炼魂选中1.GetComponentInChildren<Text>(true);
        if (标签 != null) 标签.text = "玉石炼魂（暂无材料）";
        if (原玉石 && 普通炼魂选中 != null) 普通炼魂选中.isOn = true;
    }

    private void OnDisable()
    {
        清除炼魂反馈();
    }

    private static float 区域边缘(RectTransform 框, Transform 参考, bool 上边)
    {
        return 参考.InverseTransformPoint(框.TransformPoint(上边 ? 框.rect.max : 框.rect.min)).y;
    }

    private static void 设置纵向区域(RectTransform 框, float 上边, float 高)
    {
        float 原高 = 框.rect.height;
        float 原顶 = 区域边缘(框, 框.parent, true);
        var 位置 = 框.anchoredPosition;
        位置.y += 上边 - 原顶 - (高 - 原高) * (1 - 框.pivot.y);
        if (Mathf.Abs(原高 - 高) > .01f) 框.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 高);
        if (Mathf.Abs(框.anchoredPosition.y - 位置.y) > .01f) 框.anchoredPosition = 位置;
    }

    private void 准备炼魂反馈()
    {
        if (炼魂反馈显示 != null) return;
        var 列表 = transform.Find("炼魂信息布局") as RectTransform;
        var 滚动 = 列表 != null ? 列表.GetComponent<ScrollRect>() : null;
        var 可视区 = 滚动 != null ? 滚动.viewport : null;
        var 分隔 = transform.Find("分隔条--------") as RectTransform;
        var 原字 = 普通炼魂选中 != null ? 普通炼魂选中.GetComponentInChildren<Text>(true) : null;
        var 根 = transform as RectTransform;
        if (可视区 == null || 分隔 == null || 原字 == null || 根 == null) return;

        const float 间隔 = 4, 反馈高 = 52;
        float 列表顶 = 区域边缘(可视区, transform, true);
        float 反馈底 = 区域边缘(分隔, transform, true) + 间隔;
        float 新高 = 列表顶 - 反馈底 - 反馈高 - 间隔;
        if (新高 < 78) return; // 保留至少两行原字号的属性，更多属性沿用原滚动列表。
        float 缩减 = 可视区.rect.height - 新高;
        float 原列表顶 = 区域边缘(列表, 列表.parent, true);
        float 可视顶边距 = 列表.rect.yMax - 区域边缘(可视区, 列表, true);
        var 滚动条 = 滚动.verticalScrollbar != null ? 滚动.verticalScrollbar.transform as RectTransform : null;
        float 条顶边距 = 滚动条 != null ? 区域边缘(可视区, 列表, true) - 区域边缘(滚动条, 列表, true) : 0;
        float 条底边距 = 滚动条 != null ? 区域边缘(滚动条, 列表, false) - 区域边缘(可视区, 列表, false) : 0;
        float 中心X = transform.InverseTransformPoint(可视区.TransformPoint(可视区.rect.center)).x;

        var 内容 = 滚动.content;
        if (内容 != null)
        {
            float 内容顶 = 区域边缘(内容, 可视区, true);
            内容.anchorMin = new Vector2(内容.anchorMin.x, 1);
            内容.anchorMax = new Vector2(内容.anchorMax.x, 1);
            var 位置 = 内容.anchoredPosition;
            位置.y += 内容顶 - 区域边缘(内容, 可视区, true);
            内容.anchoredPosition = 位置;
        }
        // 缩小原带遮罩的列表，固定上边；反馈作为其同级文字，不进入属性遮罩。
        设置纵向区域(列表, 原列表顶, 列表.rect.height - 缩减);
        float 新可视顶 = 列表.rect.yMax - 可视顶边距;
        设置纵向区域(可视区, 新可视顶, 新高);
        if (滚动条 != null)
            设置纵向区域(滚动条, 新可视顶 - 条顶边距, Mathf.Max(1, 新高 - 条顶边距 - 条底边距));

        var 对象 = new GameObject("炼魂结果反馈", typeof(RectTransform), typeof(Text));
        对象.layer = gameObject.layer;
        var 框 = 对象.GetComponent<RectTransform>();
        框.SetParent(transform, false);
        框.anchorMin = 框.anchorMax = new Vector2(.5f, .5f);
        框.pivot = new Vector2(.5f, 1);
        框.sizeDelta = new Vector2(可视区.rect.width, 反馈高);
        框.anchoredPosition = new Vector2(中心X - 根.rect.center.x, 反馈底 + 反馈高 - 根.rect.center.y);
        炼魂反馈显示 = 对象.GetComponent<Text>();
        炼魂反馈显示.font = 原字.font;
        炼魂反馈显示.fontSize = 原字.fontSize;
        炼魂反馈显示.fontStyle = 原字.fontStyle;
        炼魂反馈显示.color = 原字.color;
        炼魂反馈显示.lineSpacing = 原字.lineSpacing;
        炼魂反馈显示.supportRichText = 原字.supportRichText;
        炼魂反馈显示.alignment = 原字.alignment;
        炼魂反馈显示.resizeTextForBestFit = false;
        炼魂反馈显示.horizontalOverflow = HorizontalWrapMode.Wrap;
        炼魂反馈显示.verticalOverflow = VerticalWrapMode.Truncate;
        炼魂反馈显示.raycastTarget = false;
        炼魂反馈显示.text = "";
    }

    private void 清除炼魂反馈()
    {
        if (炼魂反馈显示 != null) 炼魂反馈显示.text = "";
        反馈装备 = 装备对象;
    }

    private void 显示炼魂反馈(string 信息)
    {
        聊天系统.播报(信息);
        if (!isActiveAndEnabled) return;
        准备炼魂反馈();
        if (炼魂反馈显示 == null) return;
        反馈装备 = 装备对象;
        炼魂反馈显示.text = 信息 ?? "";
        军事界面样式.限定文本(炼魂反馈显示);
    }

    private void 对齐批量炼魂文字()
    {
        var 按钮 = transform.Find("炼魂界面操作/炼魂 (1)");
        var 字 = 按钮 != null ? 按钮.GetComponentInChildren<Text>(true) : null;
        if (字 == null) return;
        军事界面样式.拉伸(字.rectTransform, 4);
        字.alignment = TextAnchor.MiddleCenter;
        原界面文字样式.居中按钮文字(字);
    }

    private void 更新装备说明布局()
    {
        if (装备信息显示 == null) return;
        var 框 = 装备信息显示.rectTransform;
        var 背景 = transform.Find("装备信息布局/通用透黑背景 (1)") as RectTransform;
        var 下一段 = transform.Find("炼魂信息布局/炼魂可视区域") as RectTransform;
        if (背景 == null || 下一段 == null) return;
        float 上界 = 框.parent.InverseTransformPoint(背景.TransformPoint(背景.rect.max)).y;
        float 下界 = 框.parent.InverseTransformPoint(下一段.TransformPoint(下一段.rect.max)).y;
        if (上界 <= 下界) return;
        float 原高 = 框.rect.height;
        float 高 = Mathf.Min(上界 - 下界, Mathf.Max(原高, Mathf.Ceil(装备信息显示.preferredHeight) + 2));
        float 原顶 = 框.parent.InverseTransformPoint(框.TransformPoint(框.rect.max)).y;
        float 新顶 = Mathf.Clamp(原顶, 下界 + 高, 上界);
        var 位置 = 框.anchoredPosition;
        位置.y += 新顶 - 原顶 - (高 - 原高) * (1 - 框.pivot.y);
        框.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 高);
        框.anchoredPosition = 位置;
    }

    private void 绑定材料按钮(string 路径, UnityAction 动作)
    {
        var 位置 = transform.Find(路径);
        var 按钮 = 位置 != null ? 位置.GetComponent<Button>() : null;
        if (按钮 == null) return;
        // 旧场景把材料加号误接到经验方法；保留窗口管理器已有的运行时监听。
        for (int i = 0; i < 按钮.onClick.GetPersistentEventCount(); i++)
            按钮.onClick.SetPersistentListenerState(i, UnityEventCallState.Off);
        按钮.onClick.AddListener(动作);
        界面窗口管理器.注册运行时按钮(按钮);
    }

    public void 打开炼魂材料() { 打开材料购买(false); }
    public void 打开锁定材料() { 打开材料购买(true); }

    private void 打开材料购买(bool 锁定材料)
    {
        var 玩家 = 军事缺口入口.当前玩家();
        var 检查 = 将领流程规则.检查装备(玩家, 装备对象);
        if (!检查.成功) { 材料提示(检查.说明); return; }
        string 名字 = 锁定材料 ? "凝魂晶石" : 装备对象.获取装备炼魂材料名字();
        var 商品 = 全局商城库.获取指定名字的道具(名字);
        if (string.IsNullOrEmpty(名字) || 名字 == "未知" || 全局道具库.获取指定名字的道具(名字) == null || 商品 == null)
        {
            材料提示("当前没有该炼魂材料的可用获取资料。");
            return;
        }
        var 商城 = UnityEngine.Object.FindObjectOfType<显示商城列表>(true);
        if (商城 == null || 商城.购买道具脚本对象 == null)
        {
            材料提示("材料购买界面暂不可用，请从主界面商城查看" + 名字 + "。");
            return;
        }
        商城.点击购买道具(名字);
    }

    private void 材料提示(string 信息)
    {
        显示炼魂反馈(信息);
    }

    public void 显示装备所有信息()
    {
        if (!object.ReferenceEquals(反馈装备, 装备对象)) 清除炼魂反馈();
        var 玩家 = 军事缺口入口.当前玩家();
        bool 有装备 = 装备对象 != null && 装备对象.装备信息 != null && 玩家 != null;
        装备头像.enabled = 有装备;
        if (有装备)
        {
            装备头像.sprite = 装备对象.获取装备头像();
            装备信息显示.text = 装备对象.获取装备名字() + "+" + 装备对象.强化等级.ToString("0") + "(" + 装备对象.获取装备等级().ToString("0") + "级) " + 装备对象.获取装备品质文本() + "\n" + 装备对象.获取装备加成文本();
            装备信息显示.color = 装备对象.获取装备文字颜色();
            if (装备对象.炼魂属性 == null) 装备对象.炼魂属性 = new List<炼魂属性>();
        }
        else 装备信息显示.text = "请先选择已穿戴的装备。";
        更新装备说明布局();
        int 锁定数 = 0;
        for (int i = 0; i < 炼魂列表对象.transform.childCount && i < 6; i++)
        {
            var 行 = 炼魂列表对象.transform.GetChild(i);
            bool 存在 = 有装备 && i < 装备对象.炼魂属性.Count && 装备对象.炼魂属性[i] != null;
            bool 锁定 = 存在 && 装备对象.炼魂属性[i].锁定;
            行.gameObject.SetActive(存在);
            行.GetChild(0).gameObject.SetActive(锁定);
            行.GetChild(4).gameObject.SetActive(锁定);
            var 勾选 = 行.GetComponent<Toggle>();
            if (勾选 != null) 勾选.SetIsOnWithoutNotify(锁定);
            if (!存在) continue;
            var 字 = 行.GetChild(1).GetComponent<Text>();
            字.text = 装备对象.炼魂属性[i].获取炼魂加成文本();
            字.color = 装备对象.获取装备文字颜色();
            行.GetChild(2).GetComponent<Text>().color = 字.color;
            if (锁定) 锁定数++;
        }
        if (!有装备) { 炼魂材料显示.text = 锁定材料显示.text = ""; return; }
        string 材料 = 装备对象.获取装备炼魂材料名字();
        炼魂材料显示.text = 材料 + " " + 玩家.背包道具列表.获取指定道具数量(材料).ToString("0") + "/" + 装备对象.品质.ToString("0");
        锁定材料显示.text = "凝魂晶石 " + 玩家.背包道具列表.获取指定道具数量("凝魂晶石").ToString("0") + "/" + 锁定数;
    }

    public void 锁定指定炼魂()
    {
        if (Dwsg.Network.GameNetwork.Enabled)
        {
            if (装备对象 == null || 装备对象.炼魂属性 == null) return;
            Dwsg.Generals.GeneralsClientAdapter.SetSoulLocks(装备对象, 选中锁定槽位(), 显示装备所有信息);
            return;
        }
        if (装备对象 == null || 装备对象.炼魂属性 == null) return;
        for (int i = 0; i < 装备对象.炼魂属性.Count && i < 炼魂列表对象.transform.childCount && i < 6; i++)
            if (装备对象.炼魂属性[i] != null)
                装备对象.炼魂属性[i].锁定 = 炼魂列表对象.transform.GetChild(i).GetChild(4).gameObject.activeSelf;
        显示装备所有信息();
    }

    private bool 尝试炼魂(out List<炼魂属性> 结果, out string 说明)
    {
        结果 = new List<炼魂属性>();
        bool 高级 = 高级炼魂选中 != null && 高级炼魂选中.isOn;
        var 检查 = 将领流程规则.支付炼魂(军事缺口入口.当前玩家(), 装备对象, 高级,
            高级炼魂选中1 != null && 高级炼魂选中1.isOn);
        说明 = 检查.说明;
        if (!检查.成功) return false;
        if (装备对象.炼魂属性 != null)
            foreach (var 魂 in 装备对象.炼魂属性) if (魂 != null && 魂.锁定) 结果.Add(魂);
        int 空槽 = 6 - 结果.Count;
        int 数量 = Random.Range(高级 ? Mathf.Min(3, 空槽) : 1, 空槽 + 1);
        for (int i = 0; i < 数量; i++)
        {
            int 类型 = Random.Range(1, 6);
            int 值 = (int)Random.Range(1f, (float)装备对象.获取炼魂加成上限(类型) + 1f);
            if (!高级 && Random.Range(1, 101) < 30) 值 = -值;
            结果.Add(new 炼魂属性 { 类型 = 类型, 炼魂值 = 值, 锁定 = false });
        }
        说明 = "炼魂完成。";
        return true;
    }

    public void 开始炼魂()
    {
        if (Dwsg.Network.GameNetwork.Enabled) { 炼魂(1); return; }
        List<炼魂属性> 结果;
        string 说明;
        if (尝试炼魂(out 结果, out 说明)) 装备对象.炼魂属性 = 结果;
        显示装备所有信息();
        显示炼魂反馈(说明);
    }

    public List<炼魂属性> 开始炼魂一次()
    {
        if (Dwsg.Network.GameNetwork.Enabled) { 炼魂(1); return 装备对象 == null ? new List<炼魂属性>() : new List<炼魂属性>(装备对象.炼魂属性); }
        List<炼魂属性> 结果;
        string 说明;
        if (尝试炼魂(out 结果, out 说明)) return 结果;
        显示炼魂反馈(说明);
        return 装备对象 != null && 装备对象.炼魂属性 != null ? new List<炼魂属性>(装备对象.炼魂属性) : 结果;
    }

    public void 开始炼魂30次()
    {
        if (Dwsg.Network.GameNetwork.Enabled) { 炼魂(30); return; }
        int 次数 = 0;
        string 说明 = "";
        for (int i = 0; i < 30; i++)
        {
            List<炼魂属性> 结果;
            if (!尝试炼魂(out 结果, out 说明)) break;
            foreach (var 魂 in 结果)
                if (魂.炼魂值 >= 50 || (魂.类型 == 2 && 魂.炼魂值 > 20)) 魂.锁定 = true;
            装备对象.炼魂属性 = 结果;
            次数++;
        }
        显示装备所有信息();
        显示炼魂反馈(次数 == 0 ? 说明 : "炼魂完成" + 次数 + "次。" + (次数 < 30 ? 说明 : ""));
    }

    private List<int> 选中锁定槽位()
	{
		List<int> indices = new List<int>();
		for (int i = 0; i < 装备对象.炼魂属性.Count; i++)
			if (炼魂列表对象.transform.GetChild(i).GetChild(4).gameObject.activeSelf) indices.Add(i);
		return indices;
	}

    private void 炼魂(int count)
	{
		int mode = 高级炼魂选中1.isOn ? 2 : 高级炼魂选中.isOn ? 1 : 0;
		Dwsg.Generals.GeneralsClientAdapter.RefineEquipment(装备对象, mode, count, 选中锁定槽位(), 显示装备所有信息);
	}
}
