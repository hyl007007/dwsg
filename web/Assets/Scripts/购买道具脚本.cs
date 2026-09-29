using UnityEngine;
using UnityEngine.UI;
using Dwsg.Window1;

public class 购买道具脚本 : MonoBehaviour
{
	public Image 道具头像对象;

	public Text 道具名字对象;

	public Text 道具说明对象;

	public Text 黄金售价对象;

	public GameObject 白银售价对象;

	public Text 黄金资产对象;

	public Text 白银资产对象;

	public Text 购买数量对象;

	public Slider 数量滑条对象;

	public GameObject 商城界面对象;

	public Text 限购数量;

	public string 道具名字;

	private double 购买数量;
	private Button 黄金购买按钮, 黄金卖出按钮, 白银购买按钮, 白银卖出按钮;
	private string 上次道具;
	private Text 交易反馈文字;
	private ScrollRect 交易反馈滚动;
	private string 反馈道具;
	private RectTransform 反馈页面, 反馈信息区域;
	private RectTransform[] 反馈操作边界, 反馈滑条边界;
	private readonly Vector3[] 反馈角点 = new Vector3[4];
	private Canvas 反馈Canvas;
	private string 反馈布局道具;
	private bool 反馈布局待刷新, 反馈待置顶;
	private int 反馈屏幕宽, 反馈屏幕高;
	private Vector2 反馈页面尺寸;
	private Vector3 反馈页面缩放;
	private float 反馈Canvas缩放;

	private void OnEnable() { 清除交易反馈(); 刷新显示(); }
	private void OnDisable() { 清除交易反馈(); }
	private void LateUpdate()
	{
		if (交易反馈滚动 == null || !交易反馈滚动.gameObject.activeSelf || 反馈页面 == null) return;
		// 静止页只比较轻量尺寸信号；不查找控件、扫描子树、测量文字或改写Rect。
		int 宽 = Screen.width, 高 = Screen.height;
		Vector2 尺寸 = 反馈页面.rect.size;
		Vector3 缩放 = 反馈页面.lossyScale;
		float Canvas缩放 = 反馈Canvas == null ? 1 : 反馈Canvas.scaleFactor;
		if (宽 != 反馈屏幕宽 || 高 != 反馈屏幕高 || 尺寸 != 反馈页面尺寸 || 缩放 != 反馈页面缩放 ||
			!Mathf.Approximately(Canvas缩放, 反馈Canvas缩放)) 反馈布局待刷新 = true;
		if (!反馈布局待刷新) return;
		反馈布局待刷新 = false;
		反馈屏幕宽 = 宽; 反馈屏幕高 = 高; 反馈页面尺寸 = 尺寸;
		反馈页面缩放 = 缩放; 反馈Canvas缩放 = Canvas缩放;
		调整交易反馈布局();
		if (反馈待置顶)
		{
			反馈待置顶 = false;
			交易反馈滚动.StopMovement();
			if (!Mathf.Approximately(交易反馈滚动.verticalNormalizedPosition, 1)) 交易反馈滚动.verticalNormalizedPosition = 1;
		}
	}

	private void 清除交易反馈()
	{
		反馈道具 = null;
		反馈布局待刷新 = 反馈待置顶 = false;
		if (交易反馈文字 != null && 交易反馈文字.text.Length != 0) 交易反馈文字.text = string.Empty;
		if (交易反馈滚动 == null) return;
		交易反馈滚动.StopMovement();
		if (交易反馈滚动.gameObject.activeSelf) 交易反馈滚动.gameObject.SetActive(false);
	}

	private void 显示交易反馈(string 内容)
	{
		if (string.IsNullOrEmpty(内容)) return;
		聊天系统.播报(内容);
		if (交易反馈滚动 == null)
		{
			反馈页面 = transform as RectTransform;
			反馈信息区域 = transform.Find("道具信息布局") as RectTransform;
			var 操作 = transform.Find("道具操作");
			var 按钮 = 操作 == null ? new Button[0] : 操作.GetComponentsInChildren<Button>(true);
			反馈操作边界 = new RectTransform[按钮.Length];
			for (int i = 0; i < 按钮.Length; i++) 反馈操作边界[i] = 按钮[i].GetComponent<RectTransform>();
			反馈滑条边界 = 数量滑条对象.GetComponentsInChildren<RectTransform>(true);
			var canvas = GetComponentInParent<Canvas>(true);
			反馈Canvas = canvas == null ? null : canvas.rootCanvas;
			// 只复用本页原数量文字；透明裁切区域不添加面板或背景图。
			var 区域 = new GameObject("交易反馈", typeof(RectTransform), typeof(ScrollRect), typeof(RectMask2D));
			区域.transform.SetParent(transform, false);
			交易反馈滚动 = 区域.GetComponent<ScrollRect>();
			交易反馈文字 = UnityEngine.Object.Instantiate(购买数量对象, 区域.transform, false);
			交易反馈文字.name = "交易反馈文字";
			交易反馈文字.resizeTextForBestFit = false;
			交易反馈文字.horizontalOverflow = HorizontalWrapMode.Wrap;
			交易反馈文字.verticalOverflow = VerticalWrapMode.Overflow;
			var rt = 交易反馈文字.rectTransform;
			rt.anchorMin = new Vector2(0, 1); rt.anchorMax = Vector2.one; rt.pivot = new Vector2(0, 1);
			rt.anchoredPosition = Vector2.zero; rt.sizeDelta = Vector2.zero;
			交易反馈滚动.viewport = 区域.GetComponent<RectTransform>();
			交易反馈滚动.viewport.anchorMin = 交易反馈滚动.viewport.anchorMax = 交易反馈滚动.viewport.pivot = Vector2.zero;
			交易反馈滚动.content = rt;
			交易反馈滚动.horizontal = false;
			交易反馈滚动.inertia = false;
			交易反馈滚动.movementType = ScrollRect.MovementType.Clamped;
			交易反馈滚动.scrollSensitivity = 交易反馈文字.fontSize * 2;
		}
		反馈道具 = 道具名字;
		if (交易反馈文字.text != 内容) 交易反馈文字.text = 内容;
		if (!交易反馈滚动.gameObject.activeSelf) 交易反馈滚动.gameObject.SetActive(true);
		if (!交易反馈文字.gameObject.activeSelf) 交易反馈文字.gameObject.SetActive(true);
		反馈布局待刷新 = 反馈待置顶 = true;
	}

	private void 调整交易反馈布局()
	{
		if (反馈页面 == null || 反馈信息区域 == null || 数量滑条对象 == null) return;
		var 信息边界 = 页面内边界(反馈页面, 反馈信息区域);
		float 滑条下缘 = float.PositiveInfinity;
		foreach (var rt in 反馈滑条边界)
			if (rt != null && rt.gameObject.activeInHierarchy) 滑条下缘 = Mathf.Min(滑条下缘, 页面内边界(反馈页面, rt).min.y);
		if (float.IsPositiveInfinity(滑条下缘)) return;
		float 操作上缘 = 反馈页面.rect.yMin;
		foreach (var rt in 反馈操作边界)
			if (rt != null && rt.gameObject.activeInHierarchy) 操作上缘 = Mathf.Max(操作上缘, 页面内边界(反馈页面, rt).max.y);
		float 横边距 = 6 / Mathf.Max(.001f, Mathf.Abs(反馈页面缩放.x));
		float 纵边距 = 6 / Mathf.Max(.001f, Mathf.Abs(反馈页面缩放.y));
		var 区域 = 交易反馈滚动.viewport;
		var 位置 = new Vector2(信息边界.min.x + 横边距 - 反馈页面.rect.xMin, 操作上缘 + 纵边距 - 反馈页面.rect.yMin);
		var 尺寸 = new Vector2(Mathf.Max(1, 信息边界.size.x - 横边距 * 2), Mathf.Max(1, 滑条下缘 - 操作上缘 - 纵边距 * 2));
		if (区域.anchoredPosition != 位置) 区域.anchoredPosition = 位置;
		if (区域.sizeDelta != 尺寸) 区域.sizeDelta = 尺寸;
		// 长结果保留原字号，换行后可在空白区上下滑动或滚轮阅读，不溢出到输入与按钮。
		float 高度 = Mathf.Max(区域.rect.height, 交易反馈文字.preferredHeight);
		if (!Mathf.Approximately(交易反馈文字.rectTransform.rect.height, 高度))
			交易反馈文字.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 高度);
		bool 长反馈 = 高度 > 区域.rect.height + .5f;
		var 对齐 = 长反馈 ? TextAnchor.UpperLeft : TextAnchor.MiddleLeft;
		if (交易反馈文字.alignment != 对齐) 交易反馈文字.alignment = 对齐;
		if (交易反馈文字.raycastTarget != 长反馈) 交易反馈文字.raycastTarget = 长反馈;
		if (交易反馈滚动.vertical != 长反馈) 交易反馈滚动.vertical = 长反馈;
	}

	private Bounds 页面内边界(RectTransform 页面, RectTransform 目标)
	{
		目标.GetWorldCorners(反馈角点);
		var bounds = new Bounds(页面.InverseTransformPoint(反馈角点[0]), Vector3.zero);
		for (int i = 1; i < 反馈角点.Length; i++) bounds.Encapsulate(页面.InverseTransformPoint(反馈角点[i]));
		return bounds;
	}
	private void 装配交易按钮()
	{
		if (黄金购买按钮 != null) return;
		var 操作 = transform.Find("道具操作");
		if (操作 == null) return;
		黄金购买按钮 = 操作.Find("黄金购买").GetComponent<Button>();
		黄金卖出按钮 = 操作.Find("黄金卖出").GetComponent<Button>();
		原界面文字样式.居中按钮文字(黄金购买按钮.GetComponentInChildren<Text>(true));
		原界面文字样式.居中按钮文字(黄金卖出按钮.GetComponentInChildren<Text>(true));
		白银购买按钮 = 创建白银按钮(黄金购买按钮, "白银购买", -74, 白银购买);
		白银卖出按钮 = 创建白银按钮(黄金卖出按钮, "白银卖出", 84, 白银卖出);
	}
	private Button 创建白银按钮(Button 来源, string 名字, float x, UnityEngine.Events.UnityAction 回调)
	{
		var 图 = 来源.GetComponent<Image>();
		var image = Window1Style.Image(来源.transform.parent, 名字, 图.color, 图.sprite); image.type = 图.type; image.raycastTarget = true;
		var rt = image.rectTransform; rt.anchorMin = rt.anchorMax = new Vector2(.5f, .5f); rt.sizeDelta = 来源.GetComponent<RectTransform>().sizeDelta; rt.anchoredPosition = new Vector2(x, 0);
		var button = rt.gameObject.AddComponent<Button>(); button.targetGraphic = image; button.colors = 来源.colors; button.transition = 来源.transition; button.spriteState = 来源.spriteState;
		var label = Window1Style.Text(rt, "文字", 名字, 道具说明对象.font, 14, Window1Style.Gold, TextAnchor.MiddleCenter);
		Window1Style.Anchors(label.rectTransform, new Vector2(.03f, .05f), new Vector2(.97f, .95f)); 原界面文字样式.按钮(label);
		button.onClick.AddListener(回调); button.onClick.AddListener(刷新商城显示); 界面窗口管理器.注册运行时按钮(button);
		return button;
	}

	public void 刷新显示()
	{
		if (反馈道具 != null && 反馈道具 != 道具名字) 清除交易反馈();
		if (反馈布局道具 != 道具名字) { 反馈布局道具 = 道具名字; 反馈布局待刷新 = true; }
		装配交易按钮();
		调整道具信息布局();
		道具信息库类 道具信息库类 = 全局道具库.获取指定名字的道具(道具名字);
		var 商品 = 全局商城库.获取指定名字的道具(道具名字);
		var 当前 = ExistingWorldAdapter.CurrentPlayer;
		if (道具信息库类 != null && 商品 != null && 当前 != null)
		{
			道具名字对象.text = 道具名字;
			道具说明对象.text = 道具信息库类.说明;
			道具头像对象.sprite = 全局变量.所有道具头像资源表 == null ? null : 全局道具库.获取道具头像(道具信息库类.头像);
			商品属性类 商品属性类 = 全局商城库.获取指定名字的道具(道具名字);
			黄金售价对象.text = 商品属性类.黄金售价.ToString();
			if (商品属性类.限购数量 != -1)
				限购数量.text = "库存: " + 商品属性类.限购数量;
			else
				限购数量.text = "库存: 无限量";
            白银售价对象.SetActive(value: false);
			if (商品属性类.白银售价 > 0.0)
			{
				白银售价对象.SetActive(value: true);
				白银售价对象.transform.GetChild(2).GetComponent<Text>().text = 商品属性类.白银售价.ToString();
			}
			int 本机身份 = 全局变量.本机身份;
			黄金资产对象.text = 全局变量.所有玩家数据表[本机身份].财产信息.黄金.ToString();
			白银资产对象.text = 全局变量.所有玩家数据表[本机身份].财产信息.白银.ToString();
			var 玩家 = 全局变量.所有玩家数据表[本机身份];
            double 黄金数量 = 商品属性类.黄金售价 > 0 ? System.Math.Floor(玩家.财产信息.黄金 / 商品属性类.黄金售价) : 0;
            double 白银数量 = 商品属性类.白银售价 > 0 ? System.Math.Floor(玩家.财产信息.白银 / 商品属性类.白银售价) : 0;
            double 可购买 = System.Math.Max(黄金数量, 白银数量);
            if (商品属性类.限购数量 != -1) 可购买 = System.Math.Min(可购买, 商品属性类.限购数量);
            double num = System.Math.Min(100, System.Math.Max(可购买, 玩家.背包道具列表.获取指定道具数量(道具名字)));
			if (double.IsNaN(num) || double.IsInfinity(num)) num = 0;
			数量滑条对象.wholeNumbers = true;
            num = System.Math.Max(1, System.Math.Floor(num));
            数量滑条对象.minValue = 1f;
			数量滑条对象.maxValue = (float)num;
			if (上次道具 != 道具名字) 数量滑条对象.value = num > 0 ? 1 : 0;
			改变购买数量();
			InventoryUi.调整长文(道具说明对象, 上次道具 != 道具名字);
			上次道具 = 道具名字;
		}
		else
		{
			道具名字对象.text = "商品不可用"; 道具说明对象.text = "请返回商城重新选择。";
			道具头像对象.sprite = null; 黄金售价对象.text = "--"; 白银售价对象.SetActive(false); 限购数量.text = "--";
			黄金资产对象.text = 当前 == null ? "--" : 当前.财产信息.黄金.ToString(); 白银资产对象.text = 当前 == null ? "--" : 当前.财产信息.白银.ToString();
			数量滑条对象.minValue = 数量滑条对象.maxValue = 0; 改变购买数量();
		}
		刷新交易按钮();
	}

	private void 调整道具信息布局()
	{
		var scroll = 道具说明对象 == null ? null : 道具说明对象.GetComponentInParent<ScrollRect>(true);
		if (scroll == null || scroll.verticalScrollbar == null) return;
		foreach (var text in new[] { 道具名字对象, 道具说明对象 })
		{
			if (text == null || text.rectTransform.parent == null) continue;
			var rt = text.rectTransform;
			var parent = rt.parent;
			// 包含原滑条及上下箭头，留出至少20个屏幕像素；只收右边界，保留文字左边和纵向位置。
			var bar = RectTransformUtility.CalculateRelativeRectTransformBounds(parent, scroll.verticalScrollbar.transform);
			var corners = new Vector3[4]; rt.GetWorldCorners(corners);
			float left = parent.InverseTransformPoint(corners[0]).x;
			float gap = 20f / Mathf.Max(.001f, Mathf.Abs(parent.lossyScale.x));
			float width = Mathf.Max(1f, bar.min.x - gap - left);
			if (rt.rect.width <= width) continue;
			var right = rt.offsetMax;
			right.x -= rt.rect.width - width;
			rt.offsetMax = right;
		}
	}

	private void 刷新交易按钮()
	{
		var 商品 = 全局商城库.获取指定名字的道具(道具名字); var 玩家 = ExistingWorldAdapter.CurrentPlayer;
		int 数量 = double.IsNaN(购买数量) || double.IsInfinity(购买数量) ? 0 : (int)购买数量;
		string error;
		if (黄金购买按钮 != null)
		{
			bool 可买 = InventoryTrade.CanBuy(玩家, 商品, 数量, false, out error);
			显示购买状态(黄金购买按钮, 可买, false, error);
		}
		if (黄金卖出按钮 != null) 黄金卖出按钮.interactable = InventoryTrade.CanSell(玩家, 商品, 数量, false, out error);
		if (白银购买按钮 != null)
		{
			bool 支持白银 = 商品 != null && 商品.白银售价 > 0;
			if (白银购买按钮.gameObject.activeSelf != 支持白银 || 白银卖出按钮.gameObject.activeSelf != 支持白银)
			{
				if (白银购买按钮.gameObject.activeSelf != 支持白银) 白银购买按钮.gameObject.SetActive(支持白银);
				if (白银卖出按钮.gameObject.activeSelf != 支持白银) 白银卖出按钮.gameObject.SetActive(支持白银);
				反馈布局待刷新 = true;
			}
			bool 可买 = InventoryTrade.CanBuy(玩家, 商品, 数量, true, out error);
			显示购买状态(白银购买按钮, 可买, true, error);
			白银卖出按钮.interactable = InventoryTrade.CanSell(玩家, 商品, 数量, true, out error);
		}
	}
	private void 显示购买状态(Button 按钮, bool 可买, bool 使用白银, string 原因)
	{
		按钮.interactable = 可买;
		var label = 按钮.GetComponentInChildren<Text>(true);
		if (label == null) return;
		label.text = 可买 ? (使用白银 ? "白银购买" : "黄金购买") : 原因 == "余额不足" ? (使用白银 ? "白银不足" : "黄金不足") :
			原因 != null && 原因.StartsWith("背包空间不足") ? "背包已满" : 原因 != null && 原因.StartsWith("库存不足") ? "库存不足" : "暂不可购";
	}

    private bool 获取交易数量(out int 数量)
    {
        数量 = 0;
        if (double.IsNaN(购买数量) || double.IsInfinity(购买数量) ||
            购买数量 < 1 || 购买数量 > 100 || 购买数量 != System.Math.Truncate(购买数量))
        {
            显示交易反馈("请选择 1 到 100 个整数数量");
            return false;
        }
        数量 = (int)购买数量;
        return true;
    }

    private void 购买(bool 使用白银)
    {
        int 数量;
        if (!获取交易数量(out 数量)) return;
        var 商品 = 全局商城库.获取指定名字的道具(道具名字);
        if (商品 == null || 全局道具库.获取指定名字的道具(道具名字) == null) return;
        string error;
        if (!InventoryTrade.Buy(ExistingWorldAdapter.CurrentPlayer, 商品, 数量, 使用白银, out error))
        {
            显示交易反馈(error); 刷新显示();
            return;
        }
        显示交易反馈("已购买 " + 道具名字 + " ×" + 数量);
        刷新显示();
    }

    private void 卖出(bool 使用白银)
    {
        int 数量;
        if (!获取交易数量(out 数量)) return;
        var 商品 = 全局商城库.获取指定名字的道具(道具名字);
        if (商品 == null) return;
        double 单价 = 使用白银 ? 商品.白银售价 : 商品.黄金售价;
        string error;
        if (!InventoryTrade.Sell(ExistingWorldAdapter.CurrentPlayer, 商品, 数量, 使用白银, out error))
        {
            显示交易反馈(error); 刷新显示();
            return;
        }
        double 金额 = 单价 * 数量;
        显示交易反馈("卖出" + 道具名字 + 数量 + "个成功，获得" + (使用白银 ? "白银" : "黄金") + 金额);
        刷新显示();
    }

    public void 黄金购买() { 购买(false); }
    public void 白银购买() { 购买(true); }
    public void 黄金卖出() { 卖出(false); }
    public void 白银卖出() { 卖出(true); }

    public void 打开购买界面()
    {
        清除交易反馈();
        gameObject.SetActive(true);
    }

	public void 改变购买数量()
	{
		购买数量对象.text = 数量滑条对象.value.ToString();
		购买数量 = 数量滑条对象.value;
		刷新交易按钮();
	}

	public void 刷新商城显示()
	{
		if (商城界面对象 != null)
		{
			商城界面对象.GetComponent<显示商城列表>().刷新显示();
		}
	}
}
