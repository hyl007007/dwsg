using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// 沿用原改名窗口的分块皮肤；边角保持原尺寸，内容和按钮由布局组件排布。
// 只装配一次，不复制原窗口的业务脚本、按钮回调或动画。
public sealed class 原界面小弹窗 : MonoBehaviour
{
    public RectTransform 内容 { get; private set; }
    public RectTransform 操作 { get; private set; }
    public Button 关闭 { get; private set; }
    private Button 按钮参考;
    private Font 字体;
    private UnityAction 取消;
    private int 屏幕宽, 屏幕高;
    private float 父画布缩放;

    public void 初始化(Transform 原窗口, string 标题, UnityAction 取消操作, float 高度 = 210)
    {
        取消 = 取消操作;
        字体 = 原窗口.Find("原名字").GetComponent<Text>().font;
        按钮参考 = 原窗口.Find("道具操作/返回").GetComponent<Button>();
        var 原皮肤 = 原窗口.Find("通用小弹窗背景");
        var 根框 = (RectTransform)transform;
        根框.anchorMin = 根框.anchorMax = new Vector2(.5f, .5f);
        根框.anchoredPosition = Vector2.zero;
        var 遮罩 = GetComponent<Image>() ?? gameObject.AddComponent<Image>();
        遮罩.sprite = null;
        遮罩.color = new Color(0, 0, 0, .35f);
        遮罩.raycastTarget = true;
        var 画布 = gameObject.AddComponent<Canvas>();
        画布.overrideSorting = true;
        画布.sortingOrder = 10;
        gameObject.AddComponent<GraphicRaycaster>();

        var 框 = 矩形("弹窗", transform);
        框.anchorMin = 框.anchorMax = new Vector2(.5f, .5f);
        框.sizeDelta = new Vector2(400, 高度);
        框.gameObject.AddComponent<Image>().color = Color.clear;
        // 原背景由纸纹角块和侧边组成；平铺保留边线厚度，不拉伸整张窗口。
        for (int 侧 = 0; 侧 < 2; 侧++)
        {
            var 半框 = 矩形(侧 == 0 ? "左纸边" : "右纸边", 框);
            拉伸(半框, new Vector2(侧 * .5f, 0), new Vector2((侧 + 1) * .5f, 1));
            if (侧 == 1) 半框.localScale = new Vector3(-1, 1, 1);
            var 中 = 图片(原皮肤.Find("通用界面背景/右边"), 半框, "纸纹");
            中.type = Image.Type.Tiled;
            拉伸(中.rectTransform, Vector2.zero, Vector2.one, new Vector2(0, 34), new Vector2(0, -34));
            var 上 = 图片(原皮肤.Find("通用界面背景/左上"), 半框, "上纸边");
            上.type = Image.Type.Tiled;
            拉伸(上.rectTransform, Vector2.up, Vector2.one, new Vector2(0, -34), Vector2.zero);
            var 下 = 图片(原皮肤.Find("通用界面背景/左上"), 半框, "下纸边");
            下.type = Image.Type.Tiled;
            拉伸(下.rectTransform, Vector2.zero, Vector2.right, Vector2.zero, new Vector2(0, 34));
            下.rectTransform.localScale = new Vector3(1, -1, 1);
        }

        var 信息 = 图片(原皮肤.Find("通用绿色背景/Image"), 框, "信息背景");
        拉伸(信息.rectTransform, Vector2.zero, Vector2.one, new Vector2(12, 61), new Vector2(-12, -43));
        // 该原节点只有矩形和八块 Image 装饰，保留其角点锚定，不带入其他逻辑。
        var 边框 = Instantiate(原皮肤.Find("通用背景边框"), 框, false) as RectTransform;
        拉伸(边框, Vector2.zero, Vector2.one, new Vector2(12, 61), new Vector2(-12, -43));
        foreach (var 图 in 边框.GetComponentsInChildren<Image>()) 图.raycastTarget = false;

        var 栏 = 矩形("标题栏", 框);
        拉伸(栏, Vector2.up, Vector2.one, new Vector2(0, -35), Vector2.zero);
        var 左标题 = 图片(原皮肤.Find("标题栏背景/左上"), 栏, "左上");
        拉伸(左标题.rectTransform, Vector2.zero, Vector2.up, Vector2.zero, new Vector2(73, 0));
        var 中标题 = 图片(原皮肤.Find("标题栏背景/中"), 栏, "中");
        拉伸(中标题.rectTransform, Vector2.zero, Vector2.one, new Vector2(73, 0), Vector2.zero);
        var 标题字 = 文本(栏, "标题", 标题, 19);
        拉伸(标题字.rectTransform, Vector2.zero, Vector2.one, new Vector2(40, 0), new Vector2(-40, 0));
        原界面文字样式.标题(标题字);
        原界面文字样式.居中按钮文字(标题字);
        var 原关闭 = 原窗口.Find("道具操作/关闭按钮").GetComponent<Button>();
        var 关闭图 = 图片(原关闭.transform, 栏, "关闭");
        拉伸(关闭图.rectTransform, Vector2.right, Vector2.one, new Vector2(-34, 1), new Vector2(-1, -1));
        关闭图.raycastTarget = true;
        关闭 = 关闭图.gameObject.AddComponent<Button>();
        复制按钮状态(原关闭, 关闭);
        关闭.onClick.AddListener(取消操作);

        内容 = 矩形("内容", 框);
        拉伸(内容, Vector2.zero, Vector2.one, new Vector2(29, 72), new Vector2(-29, -54));
        var 纵排 = 内容.gameObject.AddComponent<VerticalLayoutGroup>();
        纵排.childAlignment = TextAnchor.MiddleCenter;
        纵排.spacing = 6;
        纵排.childControlWidth = 纵排.childControlHeight = true;
        纵排.childForceExpandWidth = true;
        纵排.childForceExpandHeight = false;
        操作 = 矩形("操作", 框);
        拉伸(操作, Vector2.zero, Vector2.right, new Vector2(22, 13), new Vector2(-22, 52));
        var 横排 = 操作.gameObject.AddComponent<HorizontalLayoutGroup>();
        横排.childAlignment = TextAnchor.MiddleCenter;
        横排.spacing = 54;
        横排.childControlWidth = 横排.childControlHeight = true;
        横排.childForceExpandWidth = 横排.childForceExpandHeight = false;
        界面窗口动画.接入(gameObject);
    }

    public Text 添加说明(string 名称, string 文案, int 字号 = 18)
    {
        var 字 = 文本(内容, 名称, 文案, 字号);
        return 字;
    }

    public void 使用原按钮(Button 按钮)
    {
        按钮.transform.SetParent(操作, false);
        界面窗口动画.接入按钮(按钮);
        var 图 = 按钮.GetComponent<Image>();
        var 原图 = 按钮参考.GetComponent<Image>();
        图.sprite = 原图.sprite;
        图.type = 原图.type;
        图.color = 原图.color;
        复制按钮状态(按钮参考, 按钮);
        var 排版 = 按钮.gameObject.AddComponent<LayoutElement>();
        排版.minWidth = 排版.preferredWidth = 97;
        排版.minHeight = 排版.preferredHeight = 39;
        var 字 = 按钮.GetComponentInChildren<Text>(true);
        字.font = 字体;
        字.fontSize = 18;
        字.raycastTarget = false;
        原界面文字样式.按钮(字);
    }

    public Button 添加按钮(string 名称, string 文案, UnityAction 点击)
    {
        var 框 = 矩形(名称, 操作);
        框.gameObject.AddComponent<Image>();
        var 按钮 = 框.gameObject.AddComponent<Button>();
        var 字 = 文本(框, "文字", 文案, 18);
        拉伸(字.rectTransform, Vector2.zero, Vector2.one, new Vector2(4, 2), new Vector2(-4, -2));
        使用原按钮(按钮);
        按钮.onClick.AddListener(点击);
        return 按钮;
    }

    private static void 复制按钮状态(Button 来源, Button 目标)
    {
        目标.targetGraphic = 目标.GetComponent<Image>();
        目标.transition = 来源.transition;
        目标.colors = 来源.colors;
        目标.spriteState = 来源.spriteState;
    }

    private Text 文本(Transform 父级, string 名称, string 文案, int 字号)
    {
        var 字 = 矩形(名称, 父级).gameObject.AddComponent<Text>();
        字.font = 字体;
        字.text = 文案;
        字.fontSize = 字号;
        字.color = new Color(.7843137f, .7843137f, .7843137f);
        字.alignment = TextAnchor.MiddleCenter;
        字.supportRichText = false;
        字.raycastTarget = false;
        原界面文字样式.居中按钮文字(字);
        return 字;
    }

    private static RectTransform 矩形(string 名称, Transform 父级)
    {
        var 节点 = new GameObject(名称, typeof(RectTransform));
        节点.layer = 5;
        节点.transform.SetParent(父级, false);
        return (RectTransform)节点.transform;
    }

    private static Image 图片(Transform 来源, Transform 父级, string 名称)
    {
        var 原图 = 来源.GetComponent<Image>();
        var 图 = 矩形(名称, 父级).gameObject.AddComponent<Image>();
        图.sprite = 原图.sprite;
        图.type = 原图.type;
        图.color = 原图.color;
        图.raycastTarget = false;
        return 图;
    }

    private static void 拉伸(RectTransform 框, Vector2 最小, Vector2 最大, Vector2 起点 = default(Vector2), Vector2 终点 = default(Vector2))
    {
        框.anchorMin = 最小;
        框.anchorMax = 最大;
        框.pivot = new Vector2(.5f, .5f);
        框.offsetMin = 起点;
        框.offsetMax = 终点;
    }

    private void OnEnable() { 适配屏幕(); }
    private void OnRectTransformDimensionsChange() { 适配屏幕(); }
    private void 适配屏幕()
    {
        if (transform.parent == null || !gameObject.activeInHierarchy) return;
        float 父缩放 = Mathf.Abs(transform.parent.lossyScale.y);
        if (父缩放 <= 0) return;
        float 屏幕缩放 = Screen.height / 540f;
        float 缩放 = 屏幕缩放 / 父缩放;
        var 框 = (RectTransform)transform;
        if (Mathf.Abs(框.localScale.y - 缩放) > .001f) 框.localScale = new Vector3(缩放, 缩放, 1);
        var 尺寸 = new Vector2(Screen.width / 屏幕缩放, 540);
        if ((框.sizeDelta - 尺寸).sqrMagnitude > .01f) 框.sizeDelta = 尺寸;
    }

    private void Update()
    {
        // 分辨率和父 CanvasScaler 并非同一时刻更新；两者稳定后才是最终全屏尺寸。
        float 当前父缩放 = transform.parent == null ? 1 : Mathf.Abs(transform.parent.lossyScale.y);
        if (屏幕宽 != Screen.width || 屏幕高 != Screen.height || Mathf.Abs(当前父缩放 - 父画布缩放) > .001f)
        {
            屏幕宽 = Screen.width;
            屏幕高 = Screen.height;
            父画布缩放 = 当前父缩放;
            适配屏幕();
        }
        if (Input.GetKeyDown(KeyCode.Escape) && 取消 != null && 安卓输入适配.小弹窗处理返回(this)) 取消();
    }

    private void OnDisable()
    {
        if (取消 != null) 取消();
    }
}
