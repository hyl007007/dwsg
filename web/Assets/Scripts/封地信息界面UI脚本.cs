using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using 玩家数据结构;
using 缺失界面.窗口4;
using Dwsg.Window3;
using Dwsg.Window1;
using System.Linq;
using Dwsg.Network;
using Dwsg.Generals;
using Newtonsoft.Json.Linq;

public class 封地信息界面UI脚本 : MonoBehaviour
{
	public Toggle 俘虏选中开关;

	public Toggle 闲兵选中开关;

	public Toggle 伤兵选中开关;

	public GameObject 俘虏列表对象;

	public GameObject 闲兵列表对象;

	public GameObject 伤兵列表对象;

	private List<将领索引> 俘虏列表;

	private List<string> 联机俘虏编号 = new List<string>();

	public 调整数量脚本 调整数量脚本对象;

    private bool 信息页已绑定;
    private bool 正在同步分类;
    private bool 封地名称布局已调整;
    private readonly Dictionary<string, Text> 页文字 = new Dictionary<string, Text>();
    private readonly Dictionary<string, UnityEngine.Events.UnityAction> 页按钮动作 = new Dictionary<string, UnityEngine.Events.UnityAction>();
    private Button 原本页按钮;
    private Window1Style 原界面样式;
    private RectTransform 建筑行布局;
    private readonly List<建筑操作行> 建筑操作行表 = new List<建筑操作行>();

    private sealed class 建筑操作行
    {
        public GameObject 对象;
        public Button 按钮;
        public Text 文字;
        public Text 按钮文字;
        public int 索引;
    }

    private void OnEnable()
    {
        if (!信息页已绑定)
        {
            if (原界面样式 == null) 原界面样式 = Window1Style.FromScene(gameObject.scene);
            var tabs = transform.Find("封地信息选项列表/建筑信息切换");
            if (tabs != null) foreach (Transform child in tabs)
            {
                var toggle = child.GetComponent<Toggle>(); if (toggle == null) continue;
                string name = child.name;
                toggle.onValueChanged.AddListener(on => { if (on) 显示信息分类(name); });
            }
            var select = transform.Find("封地操作/切换封地");
            var button = select == null ? null : select.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.AddListener(() =>
                {
                    var picker = CityNavigation.Find<选择封地界面脚本>(p => p.name == "选择封地界面UI");
                    if (picker != null) picker.打开选择封地列表(4);
                });
                界面窗口管理器.注册运行时按钮(button);
            }
            信息页已绑定 = true;
        }
        绑定当前封地();
    }

    public void 绑定当前封地()
    {
        var player = FiefActions.Player(全局变量.本机身份);
        if (player != null && player.封地信息表.Count > 0 && (全局变量.第几个封地 < 0 || 全局变量.第几个封地 >= player.封地信息表.Count)) 全局变量.第几个封地 = 0;
        var fief = FiefActions.Fief(全局变量.本机身份, 全局变量.第几个封地);
        var label = transform.Find("封地操作/将领封地显示");
        if (label != null) 显示封地名称(label.GetComponent<Text>(), fief == null ? "暂无封地" : fief.封地名字);
        if (gameObject.activeInHierarchy) 显示信息分类("信息");
    }

    private void 显示封地名称(Text text, string name)
    {
        if (text == null) return;
        var rect = text.rectTransform;
        if (!封地名称布局已调整)
        {
            float left = rect.anchoredPosition.x - rect.sizeDelta.x * rect.pivot.x;
            float top = rect.anchoredPosition.y + rect.sizeDelta.y * (1 - rect.pivot.y);
            rect.pivot = new Vector2(0, .5f);
            rect.anchoredPosition = new Vector2(left, top - 14);
            rect.sizeDelta = new Vector2(320, 28);
            封地名称布局已调整 = true;
        }
        text.resizeTextForBestFit = false; text.supportRichText = false;
        text.horizontalOverflow = HorizontalWrapMode.Overflow; text.verticalOverflow = VerticalWrapMode.Truncate;
        text.alignment = TextAnchor.MiddleLeft;
        string caption = (name ?? "").Replace("\r", " ").Replace("\n", " ");
        text.text = caption;
        while (caption.Length > 0 && text.preferredWidth > rect.rect.width)
        {
            caption = caption.Substring(0, caption.Length - 1);
            text.text = caption + "…";
        }
    }

    private Transform 信息布局(string name) { return transform.Find("封地信息界面详情布局列表/" + name + "布局"); }
    private Text 信息文字(string name)
    {
        Text text; if (页文字.TryGetValue(name, out text) && text != null) return text;
        var layout = 信息布局(name); if (layout == null) return null;
        var go = new GameObject("封地" + name + "内容", typeof(RectTransform)); go.transform.SetParent(layout, false);
        var rect = (RectTransform)go.transform; rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(14, name == "闲兵" ? 60 : 12); rect.offsetMax = new Vector2(-14, -12);
        text = go.AddComponent<Text>(); var source = transform.Find("封地操作/将领封地标题").GetComponent<Text>();
        text.font = source.font; text.fontSize = 20; text.color = new Color(.96f, .94f, .73f); text.lineSpacing = 1.12f;
        text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Truncate;
        text.alignment = TextAnchor.UpperLeft; text.raycastTarget = false; 页文字[name] = text; return text;
    }

    private void 显示信息分类(string name)
    {
        if (正在同步分类) return;
        同步信息分类(name);
        foreach (string kind in new[] { "信息", "建筑", "驻防", "闲兵", "伤兵", "俘虏" })
        { var layout = 信息布局(kind); if (layout != null) layout.gameObject.SetActive(kind == name); }
        var player = FiefActions.Player(全局变量.本机身份);
        var fief = FiefActions.Fief(全局变量.本机身份, 全局变量.第几个封地);
        if (name == "闲兵" || name == "伤兵" || name == "俘虏")
        {
            int count = fief == null ? 0 : name == "闲兵" ? fief.闲兵信息表.Count : name == "伤兵" ? fief.伤兵信息表.Count : fief.俘虏信息表.Count;
            var list = name == "闲兵" ? 闲兵列表对象 : name == "伤兵" ? 伤兵列表对象 : 俘虏列表对象;
            if (list != null && count == 0) foreach (Transform row in list.transform) row.gameObject.SetActive(false);
            var empty = 信息文字(name); if (empty != null)
            { empty.gameObject.SetActive(count == 0); empty.text = name == "闲兵" ? "暂无闲兵。建造兵营后可选择兵种招募。" : name == "伤兵" ? "暂无伤兵。战斗受伤的士兵可在此治疗。" : "暂无俘虏。战斗俘获将领后可在此招降。"; }
            if (name == "闲兵") 添加本页按钮(name, "前往建设", () => 显示信息分类("建筑"), count == 0);
            return;
        }
        var content = 信息文字(name); if (content == null) return;
        if (fief == null || player == null) { content.text = "暂无封地。请先在本国城池开辟封地。"; return; }
        var city = fief.所在城池 == null ? null : CityLocalAdapter.City(fief.所在城池.x, fief.所在城池.y);
        if (name == "信息")
        {
            content.text = fief.封地名字 + " · " + (city == null ? "城池已迁移" : city.名称 + "（" + city.坐标x + "," + city.坐标y + "）") +
                "\n人口：" + player.获取已占用人口().ToString("0") + "/" + player.获取人口上限().ToString("0") +
                "\n铜钱：" + player.财产信息.铜钱.ToString("0") + "　粮食：" + player.财产信息.粮食.ToString("0") +
                "\n铜钱总产量：" + player.获取铜钱产量().ToString("0") + "　粮食总产量：" + player.获取粮食产量().ToString("0") +
                "\n本封地将领：" + fief.将领信息表.Count + "　闲兵：" + fief.闲兵信息表.Sum(x => x.数量).ToString("0") +
                "\n伤兵：" + fief.伤兵信息表.Sum(x => x.数量).ToString("0") + "　俘虏：" + fief.俘虏信息表.Count +
                "\n建设、升级请切换“建筑”；招募后的士兵进入“闲兵”。";
        }
        else if (name == "驻防")
        {
            var rows = city == null || !CityLocalAdapter.Friendly(city) ? new List<string>() : CityLocalAdapter.DefenderRows(city);
            content.text = rows.Count == 0 ? "所在城池暂无驻防将领。\n可从城池详情选择将领派遣驻防。" : string.Join("\n", rows.ToArray());
            驻防文字滚动(content);
            添加本页按钮(name, "查看城池", 打开当前封地城池, city != null);
        }
        else
        {
            content.gameObject.SetActive(false); 显示建筑操作列表(fief);
        }
    }

    private void 同步信息分类(string name)
    {
        var tabs = transform.Find("封地信息选项列表/建筑信息切换"); if (tabs == null) return;
        var selected = tabs.Find(name); var target = selected == null ? null : selected.GetComponent<Toggle>();
        if (target == null) return;
        正在同步分类 = true;
        try
        {
            target.SetIsOnWithoutNotify(true);
            foreach (Transform child in tabs)
            {
                var toggle = child.GetComponent<Toggle>(); if (toggle == null) continue;
                toggle.SetIsOnWithoutNotify(toggle == target);
                if (toggle.graphic != null)
                {
                    toggle.graphic.gameObject.SetActive(toggle.isOn);
                    toggle.graphic.canvasRenderer.SetAlpha(toggle.isOn ? 1 : 0);
                }
            }
            foreach (Transform child in tabs)
            {
                var toggle = child.GetComponent<Toggle>();
                if (toggle != null) toggle.onValueChanged.Invoke(toggle.isOn);
            }
        }
        finally { 正在同步分类 = false; }
    }

    private void 驻防文字滚动(Text text)
    {
        var layout = 信息布局("驻防"); if (layout == null || layout.Find("驻防滚动区域") != null) return;
        var go = new GameObject("驻防滚动区域", typeof(RectTransform)); go.transform.SetParent(layout, false);
        var viewport = (RectTransform)go.transform; viewport.anchorMin = Vector2.zero; viewport.anchorMax = Vector2.one;
        viewport.offsetMin = new Vector2(12,60); viewport.offsetMax = new Vector2(-12,-12);
        var hit = go.AddComponent<Image>(); hit.color = new Color(0,0,0,.001f); go.AddComponent<RectMask2D>();
        var scroll = go.AddComponent<ScrollRect>(); scroll.viewport = viewport; scroll.horizontal = false; scroll.vertical = true; scroll.scrollSensitivity = 28;
        text.transform.SetParent(viewport,false); var rect = text.rectTransform;
        rect.anchorMin = new Vector2(0,1); rect.anchorMax = Vector2.one; rect.pivot = new Vector2(.5f,1); rect.sizeDelta = Vector2.zero; rect.anchoredPosition = Vector2.zero;
        text.verticalOverflow = VerticalWrapMode.Overflow; text.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.content = rect;
    }

    private void 添加本页按钮(string page, string caption, UnityEngine.Events.UnityAction action, bool visible)
    {
        var layout = 信息布局(page); if (layout == null) return;
        string key = page + "/" + caption;
        页按钮动作[key] = action;
        var old = layout.Find("操作_" + caption);
        if (old != null) { old.gameObject.SetActive(visible); return; }
        var button = 创建本页按钮(layout, "操作_" + caption, caption, TextAnchor.MiddleCenter);
        var rect = (RectTransform)button.transform; rect.sizeDelta = new Vector2(130, 35); rect.anchoredPosition = new Vector2(225, -116);
        button.onClick.AddListener(() =>
        {
            UnityEngine.Events.UnityAction current;
            if (页按钮动作.TryGetValue(key, out current) && current != null) current();
        });
        界面窗口管理器.注册运行时按钮(button);
        button.gameObject.SetActive(visible);
    }

    private Button 创建本页按钮(Transform parent, string name, string caption, TextAnchor alignment)
    {
        if (原本页按钮 == null) 原本页按钮 = GetComponentsInChildren<Button>(true).FirstOrDefault(b => b.name == "返回");
        var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
        var image = go.AddComponent<Image>(); var originalImage = 原本页按钮 == null ? null : 原本页按钮.GetComponent<Image>();
        if (originalImage != null)
        {
            image.sprite = originalImage.sprite; image.type = originalImage.type; image.color = originalImage.color;
            image.material = originalImage.material; image.maskable = originalImage.maskable;
            image.preserveAspect = originalImage.preserveAspect; image.pixelsPerUnitMultiplier = originalImage.pixelsPerUnitMultiplier;
            image.fillCenter = originalImage.fillCenter; image.fillMethod = originalImage.fillMethod; image.fillAmount = originalImage.fillAmount;
            image.fillOrigin = originalImage.fillOrigin; image.fillClockwise = originalImage.fillClockwise; image.useSpriteMesh = originalImage.useSpriteMesh;
        }
        else image.color = new Color(.33f,.26f,.12f);
        var button = go.AddComponent<Button>(); button.targetGraphic = image;
        if (原本页按钮 != null)
        {
            button.transition = 原本页按钮.transition; button.colors = 原本页按钮.colors;
            button.spriteState = 原本页按钮.spriteState; button.navigation = 原本页按钮.navigation;
            button.animationTriggers = new AnimationTriggers
            {
                normalTrigger = 原本页按钮.animationTriggers.normalTrigger, highlightedTrigger = 原本页按钮.animationTriggers.highlightedTrigger,
                pressedTrigger = 原本页按钮.animationTriggers.pressedTrigger, selectedTrigger = 原本页按钮.animationTriggers.selectedTrigger,
                disabledTrigger = 原本页按钮.animationTriggers.disabledTrigger
            };
        }
        var label = new GameObject("文字", typeof(RectTransform)); label.transform.SetParent(go.transform,false);
        var textRect = (RectTransform)label.transform; textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(8,0); textRect.offsetMax = new Vector2(-8,0);
        var text = label.AddComponent<Text>(); text.font = transform.Find("封地操作/将领封地标题").GetComponent<Text>().font;
        text.fontSize = 18; text.alignment = alignment; text.text = caption; text.raycastTarget = false; 原界面文字样式.按钮(text);
        return button;
    }

    private void 打开当前封地城池()
    {
        var fief = FiefActions.Fief(全局变量.本机身份, 全局变量.第几个封地);
        var city = fief == null || fief.所在城池 == null ? null : CityLocalAdapter.City(fief.所在城池.x, fief.所在城池.y);
        int index = city == null ? -1 : 全局变量.所有城池列表.IndexOf(city);
        if (index < 0) return;
        var view = CityNavigation.Find<城池信息显示脚本>(v => v.城池名字坐标 != null);
        var main = CityNavigation.Find<主界面UI脚本>(v => v.大地图布局对象 != null);
        if (view == null || main == null) return;
        gameObject.SetActive(false); main.加载大地图场景(); view.显示城池信息(index); view.gameObject.SetActive(true);
    }

    private void 打开当前封地建筑(int index)
    {
        var fief = FiefActions.Fief(全局变量.本机身份, 全局变量.第几个封地);
        if (fief == null || index < 0 || index >= fief.建筑信息表.Count) return;
        var view = CityNavigation.Find<封地界面脚本>(v => v.封地建筑列表对象 != null);
        var main = CityNavigation.Find<主界面UI脚本>(v => v.封地布局对象 != null);
        if (view == null || main == null) return;
        view.第几个封地 = 全局变量.第几个封地; gameObject.SetActive(false); main.加载封地场景();
        view.显示封地所有建筑(); view.封地建筑打开操作(index);
    }

    private void 显示建筑操作列表(封地信息 fief)
    {
        var layout = 信息布局("建筑"); if (layout == null) return;
        if (建筑行布局 == null)
        {
            建筑操作行表.Clear();
            var root = new GameObject("建筑操作列表", typeof(RectTransform)); root.transform.SetParent(layout, false);
            var rect = (RectTransform)root.transform; rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
            var hit = root.AddComponent<Image>(); hit.color = new Color(0,0,0,.001f);
            var view = new GameObject("滚动视口", typeof(RectTransform)); view.transform.SetParent(root.transform, false);
            var viewport = (RectTransform)view.transform; viewport.anchorMin = Vector2.zero; viewport.anchorMax = Vector2.one;
            viewport.offsetMin = Vector2.zero; viewport.offsetMax = new Vector2(-20,0); view.AddComponent<RectMask2D>();
            var scroll = root.AddComponent<ScrollRect>(); scroll.horizontal = false; scroll.vertical = true; scroll.viewport = viewport; scroll.scrollSensitivity = 28;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            if (原界面样式 == null) 原界面样式 = Window1Style.FromScene(gameObject.scene);
            scroll.verticalScrollbar = 原界面样式.VerticalScrollbar(rect);
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            var rows = new GameObject("建筑行", typeof(RectTransform)); rows.transform.SetParent(viewport, false);
            建筑行布局 = (RectTransform)rows.transform; 建筑行布局.anchorMin = new Vector2(0,1); 建筑行布局.anchorMax = Vector2.one;
            建筑行布局.pivot = new Vector2(.5f,1); 建筑行布局.sizeDelta = Vector2.zero;
            scroll.content = 建筑行布局; var group = rows.AddComponent<VerticalLayoutGroup>(); group.spacing = 3; group.padding = new RectOffset(10,10,6,6);
            group.childControlHeight = group.childControlWidth = true; group.childForceExpandHeight = false;
            rows.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }
        for (int i = 0; i < fief.建筑信息表.Count; i++)
        {
            if (i >= 建筑操作行表.Count)
            {
                var go = new GameObject("建筑" + i, typeof(RectTransform)); go.transform.SetParent(建筑行布局, false);
                go.AddComponent<LayoutElement>().preferredHeight = 44;
                var background = go.AddComponent<Image>(); background.raycastTarget = false;
                var original = 闲兵列表对象 == null || 闲兵列表对象.transform.childCount == 0 ? null : 闲兵列表对象.transform.GetChild(0).Find("通用透黑背景");
                var originalImage = original == null ? null : original.GetComponent<Image>();
                if (originalImage != null)
                {
                    background.sprite = originalImage.sprite; background.type = originalImage.type; background.color = originalImage.color;
                    background.material = originalImage.material; background.pixelsPerUnitMultiplier = originalImage.pixelsPerUnitMultiplier;
                }
                else background.color = new Color(.12f,.18f,.10f,.86f);
                var label = new GameObject("名称等级", typeof(RectTransform)); label.transform.SetParent(go.transform, false);
                var labelRect = (RectTransform)label.transform; labelRect.anchorMin = Vector2.zero; labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = new Vector2(12,0); labelRect.offsetMax = new Vector2(-95,0);
                var text = label.AddComponent<Text>(); text.font = transform.Find("封地操作/将领封地标题").GetComponent<Text>().font;
                text.fontSize = 18; text.color = new Color(.96f,.94f,.73f); text.alignment = TextAnchor.MiddleLeft;
                text.horizontalOverflow = HorizontalWrapMode.Overflow; text.verticalOverflow = VerticalWrapMode.Truncate; text.raycastTarget = false;
                var button = 创建本页按钮(go.transform, "操作", "", TextAnchor.MiddleCenter);
                var buttonRect = (RectTransform)button.transform; buttonRect.anchorMin = buttonRect.anchorMax = new Vector2(1,.5f);
                buttonRect.pivot = new Vector2(1,.5f); buttonRect.sizeDelta = new Vector2(73,35); buttonRect.anchoredPosition = new Vector2(-8,0);
                var row = new 建筑操作行 { 对象 = go, 按钮 = button, 文字 = text, 按钮文字 = button.GetComponentsInChildren<Text>(true).FirstOrDefault(), 索引 = i };
                button.onClick.AddListener(() => 打开当前封地建筑(row.索引));
                界面窗口管理器.注册运行时按钮(button); 建筑操作行表.Add(row);
            }
            var current = 建筑操作行表[i]; var b = fief.建筑信息表[i];
            current.索引 = i; current.对象.SetActive(true);
            current.文字.text = b.类型 < 0 ? "空地" + i : b.获取建筑等级文本();
            current.按钮文字.text = b.类型 < 0 ? "建造" : "查看";
        }
        for (int i = fief.建筑信息表.Count; i < 建筑操作行表.Count; i++) 建筑操作行表[i].对象.SetActive(false);
    }

    private bool 封地有效()
    {
        var 玩家 = 军事缺口入口.当前玩家();
        return 玩家 != null && 全局变量.第几个封地 >= 0 && 全局变量.第几个封地 < 玩家.封地信息表.Count;
    }

    public void 打开兵员分类(string 类型)
    {
        if (!封地有效()) return;
        显示信息分类(类型 == "俘虏" || 类型 == "伤兵" ? 类型 : "闲兵");
    }

    private static void 注册列表按钮(GameObject 行, int 选中子级)
    {
        if (行.transform.childCount > 选中子级) 行.transform.GetChild(选中子级).gameObject.SetActive(false);
        foreach (var 按 in 行.GetComponentsInChildren<Button>(true))
        {
            foreach (var 字 in 按.GetComponentsInChildren<Text>(true))
                if (字.GetComponentInParent<Selectable>(true) == 按) 原界面文字样式.居中按钮文字(字);
            界面窗口管理器.注册运行时按钮(按);
        }
    }

	public void 显示闲兵列表()
	{
        if (!封地有效()) return;
		if (!闲兵选中开关.isOn)
		{
			return;
		}
		int 本机身份 = 全局变量.本机身份;
		int 第几个封地 = 全局变量.第几个封地;
		int childCount = 闲兵列表对象.transform.childCount;
		for (int i = 0; i < childCount; i++)
		{
			闲兵列表对象.transform.GetChild(i).gameObject.SetActive(value: false);
		}
		List<闲兵信息> 闲兵信息表 = 全局变量.所有玩家数据表[本机身份].封地信息表[第几个封地].闲兵信息表;
		int count = 闲兵信息表.Count;
		for (int j = 0; j < count; j++)
		{
			childCount = 闲兵列表对象.transform.childCount;
			GameObject gameObject;
			if (childCount <= j)
			{
				gameObject = UnityEngine.Object.Instantiate(闲兵列表对象.transform.GetChild(0).gameObject);
				gameObject.transform.SetParent(闲兵列表对象.transform);
				gameObject.transform.localScale = new Vector3(1f, 1f, 1f);
			}
			else
			{
				gameObject = 闲兵列表对象.transform.GetChild(j).gameObject;
			}
			gameObject.SetActive(value: true);
            注册列表按钮(gameObject, 15);
			兵种属性库类 兵种属性库类 = 全局兵种库.查询指定ID的数据(闲兵信息表[j].ID);
			if (兵种属性库类 != null)
			{
				int num = 全局兵种库.查询指定兵种的图片(兵种属性库类.名称);
				gameObject.transform.GetChild(1).GetComponent<Image>().sprite = 全局变量.所有兵种图片资源表[num];
				gameObject.transform.GetChild(2).GetComponent<Text>().text = 兵种属性库类.名称;
				gameObject.transform.GetChild(2).GetChild(0).GetComponent<Text>()
					.text = "(数量" + 闲兵信息表[j].数量.ToString() + ")";
				gameObject.transform.GetChild(4).GetComponent<Text>().text = 兵种属性库类.攻击力.ToString();
				gameObject.transform.GetChild(6).GetComponent<Text>().text = 兵种属性库类.防御力.ToString();
				gameObject.transform.GetChild(8).GetComponent<Text>().text = 兵种属性库类.生命值.ToString();
				gameObject.transform.GetChild(10).GetComponent<Text>().text = 兵种属性库类.攻击速度.ToString();
				gameObject.transform.GetChild(12).GetComponent<Text>().text = 兵种属性库类.移动速度.ToString();
				gameObject.transform.GetChild(14).GetComponent<Text>().text = 兵种属性库类.占用人口.ToString();
			}
		}
	}

	public void 显示伤兵列表()
	{
        if (!封地有效()) return;
		if (!伤兵选中开关.isOn)
		{
			return;
		}
		int 本机身份 = 全局变量.本机身份;
		int 第几个封地 = 全局变量.第几个封地;
		int childCount = 伤兵列表对象.transform.childCount;
		for (int i = 0; i < childCount; i++)
		{
			伤兵列表对象.transform.GetChild(i).gameObject.SetActive(value: false);
		}
		List<伤兵信息> 伤兵信息表 = 全局变量.所有玩家数据表[本机身份].封地信息表[第几个封地].伤兵信息表;
		int count = 伤兵信息表.Count;
		for (int j = 0; j < count; j++)
		{
			childCount = 伤兵列表对象.transform.childCount;
			GameObject gameObject;
			if (childCount <= j)
			{
				gameObject = UnityEngine.Object.Instantiate(伤兵列表对象.transform.GetChild(0).gameObject);
				gameObject.transform.SetParent(伤兵列表对象.transform);
				gameObject.transform.localScale = new Vector3(1f, 1f, 1f);
			}
			else
			{
				gameObject = 伤兵列表对象.transform.GetChild(j).gameObject;
			}
			gameObject.SetActive(value: true);
            注册列表按钮(gameObject, 15);
			兵种属性库类 兵种属性库类 = 全局兵种库.查询指定ID的数据(伤兵信息表[j].ID);
			if (兵种属性库类 != null)
			{
				int num = 全局兵种库.查询指定兵种的图片(兵种属性库类.名称);
				gameObject.transform.GetChild(1).GetComponent<Image>().sprite = 全局变量.所有兵种图片资源表[num];
				gameObject.transform.GetChild(2).GetComponent<Text>().text = 兵种属性库类.名称;
				gameObject.transform.GetChild(2).GetChild(0).transform.GetComponent<Text>().text = "(数量" + 伤兵信息表[j].数量.ToString() + ")";
				gameObject.transform.GetChild(4).GetComponent<Text>().text = 兵种属性库类.攻击力.ToString();
				gameObject.transform.GetChild(6).GetComponent<Text>().text = 兵种属性库类.防御力.ToString();
				gameObject.transform.GetChild(8).GetComponent<Text>().text = 兵种属性库类.生命值.ToString();
				gameObject.transform.GetChild(10).GetComponent<Text>().text = 兵种属性库类.攻击速度.ToString();
				gameObject.transform.GetChild(12).GetComponent<Text>().text = 兵种属性库类.移动速度.ToString();
				gameObject.transform.GetChild(14).GetComponent<Text>().text = 兵种属性库类.占用人口.ToString();
			}
		}
	}

	public void 显示俘虏列表()
	{
        if (!封地有效()) return;
		if (!俘虏选中开关.isOn)
		{
			return;
		}
		int 本机身份 = 全局变量.本机身份;
		int 第几个封地 = 全局变量.第几个封地;
		HashSet<string> selected = new HashSet<string>(选中联机俘虏());
		JArray online = GameNetwork.Enabled ? GeneralsClientAdapter.CaptiveRows(第几个封地) : null;
		if (online == null) 俘虏列表 = 全局变量.所有玩家数据表[本机身份].封地信息表[第几个封地].俘虏信息表;
		联机俘虏编号.Clear();
		if (online != null) foreach (JObject row in online) 联机俘虏编号.Add(row.Value<string>("generalId"));
		int count = online == null ? 俘虏列表.Count : online.Count;
		int childCount = 俘虏列表对象.transform.childCount;
		for (int i = 0; i < childCount; i++)
		{
			俘虏列表对象.transform.GetChild(i).gameObject.SetActive(value: false);
		}
		for (int j = 0; j < count; j++)
		{
			childCount = 俘虏列表对象.transform.childCount;
			将领信息 将领信息;
			if (online == null)
			{
				if (俘虏列表[j].第几个玩家 < 0 || 俘虏列表[j].第几个玩家 >= 全局变量.所有玩家数据表.Count) continue;
				返回将领索引 返回将领索引 = 全局变量.所有玩家数据表[俘虏列表[j].第几个玩家].获取指定ID标识的将领索引(俘虏列表[j].将领ID标识);
				if (返回将领索引 == null || 返回将领索引.第几个封地 < 0 || 返回将领索引.第几个将领 < 0) continue;
				将领信息 = 全局变量.所有玩家数据表[俘虏列表[j].第几个玩家].封地信息表[返回将领索引.第几个封地].将领信息表[返回将领索引.第几个将领];
			}
			else
			{
				JObject row = (JObject)online[j];
				将领信息 = new 将领信息 { 将领属性 = new 将领属性 { 初始属性 = new 初始属性 { ID = row.Value<double>("configId"), 名字 = row.Value<string>("name"),
					职业 = row.Value<int>("profession"), 成长 = row.Value<double>("growth") }, 成长点数 = new 成长点数 { 等级 = row.Value<double>("level") } },
					详细信息 = new 详细信息 { 忠诚 = row.Value<double>("loyalty") } };
			}
			GameObject gameObject;
			if (childCount <= j)
			{
				gameObject = UnityEngine.Object.Instantiate(俘虏列表对象.transform.GetChild(0).gameObject);
				gameObject.transform.SetParent(俘虏列表对象.transform);
				gameObject.transform.localScale = new Vector3(1f, 1f, 1f);
			}
			else
			{
				gameObject = 俘虏列表对象.transform.GetChild(j).gameObject;
			}
			gameObject.SetActive(value: true);
            注册列表按钮(gameObject, 6);
			if (online != null) gameObject.transform.GetChild(6).gameObject.SetActive(selected.Contains(联机俘虏编号[j]));
			将领属性库类 将领属性库类 = 全局将领库.查询指定ID的将领数据(将领信息.将领属性.初始属性.ID);
			if (将领属性库类 != null)
			{
				gameObject.transform.GetChild(1).GetComponent<Image>().sprite = 全局将领库.获取指定将领的头像(将领属性库类.名字);
				Animator component = gameObject.transform.GetChild(1).GetChild(0).GetComponent<Animator>();
				gameObject.transform.GetChild(1).GetChild(0).gameObject.SetActive(value: false);
				if (将领属性库类.头像特效 != 0.0)
				{
					gameObject.transform.GetChild(1).GetChild(0).gameObject.SetActive(value: true);
					component.SetInteger("特效类型", (int)将领属性库类.头像特效);
				}
			}
			gameObject.transform.GetChild(2).GetComponent<Text>().text = 将领信息.将领属性.初始属性.名字;
			gameObject.transform.GetChild(3).GetComponent<Text>().text = "(" + 将领信息.将领属性.成长点数.等级.ToString() + "级" + 将领信息.将领属性.初始属性.获取职业名字() + ")";
			gameObject.transform.GetChild(4).GetComponent<Text>().text = "成长:" + 将领信息.将领属性.初始属性.成长.ToString();
			gameObject.transform.GetChild(5).GetComponent<Text>().text = "忠诚:" + 将领信息.详细信息.忠诚.ToString();
		}
		if (online != null) 选中高亮();
	}

	private List<string> 选中联机俘虏()
	{
		List<string> selected = new List<string>();
		for (int i = 0; i < 联机俘虏编号.Count && i < 俘虏列表对象.transform.childCount; i++)
		{
			GameObject row = 俘虏列表对象.transform.GetChild(i).gameObject;
			if (row.activeSelf && row.transform.GetChild(6).gameObject.activeSelf) selected.Add(联机俘虏编号[i]);
		}
		return selected;
	}

	public void 劝降俘虏()
	{
        if (!封地有效()) return;
		if (GameNetwork.Enabled)
		{
			GeneralsClientAdapter.ManageCaptives(全局变量.第几个封地, 选中联机俘虏(), false, 显示俘虏列表);
			return;
		}
		int 本机身份 = 全局变量.本机身份;
		int 第几个封地 = 全局变量.第几个封地;
		if (!(全局变量.所有玩家数据表[本机身份].获取将领总数() < 全局变量.所有玩家数据表[本机身份].基础信息.将领数上限))
		{
			全局变量.提示类.显示信息("劝降失败,将领上限!");
			return;
		}
		int childCount = 俘虏列表对象.transform.childCount;
		for (int i = childCount - 1; i >= 0; i--)
		{
			GameObject gameObject = 俘虏列表对象.transform.GetChild(i).gameObject;
			if (!gameObject.activeSelf || !gameObject.transform.GetChild(6).gameObject.activeSelf)
			{
				continue;
			}
			if (全局变量.所有玩家数据表[本机身份].获取将领总数() >= 全局变量.所有玩家数据表[本机身份].基础信息.将领数上限) break;
            俘虏列表 = 全局变量.所有玩家数据表[本机身份].封地信息表[第几个封地].俘虏信息表;
			if (i >= 俘虏列表.Count) continue;
            int 第几个玩家 = 俘虏列表[i].第几个玩家;
            if (第几个玩家 < 0 || 第几个玩家 >= 全局变量.所有玩家数据表.Count) continue;
			int 将领ID标识 = 俘虏列表[i].将领ID标识;
			返回将领索引 返回将领索引 = 全局变量.所有玩家数据表[俘虏列表[i].第几个玩家].获取指定ID标识的将领索引(将领ID标识);
			if (返回将领索引 == null || 返回将领索引.第几个封地 < 0 || 返回将领索引.第几个将领 < 0) continue;
            int 第几个封地2 = 返回将领索引.第几个封地;
			int 第几个将领 = 返回将领索引.第几个将领;
			int num = 100 - (int)全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地2].将领信息表[第几个将领].详细信息.忠诚;
			int num2 = UnityEngine.Random.Range(1, 500);
			if (全局方法类.GetStrMd5(全局变量.所有玩家数据表[本机身份].基础信息.名字) == "E586D0FD6B8E898AFA3B640A861EEBAB")
			{
				num2 = num;
			}
			if (num2 <= num)
			{
				UnityEngine.Debug.Log("劝降成功!");
				全局变量.提示类.显示信息("劝降成功!");
				全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地2].将领信息表[第几个将领].详细信息.身份 = 本机身份;
				全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地2].将领信息表[第几个将领].详细信息.忠诚 = 60.0;
				全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地2].将领信息表[第几个将领].详细信息.状态 = 0.0;
				全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地2].将领信息表[第几个将领].将领重置等级();
				全局变量.所有玩家数据表[本机身份].添加将领信息到列表(第几个封地, 全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地2].将领信息表[第几个将领]);
				全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地2].将领信息表.RemoveAt(第几个将领);
				全局变量.所有玩家数据表[本机身份].封地信息表[第几个封地].俘虏信息表.RemoveAt(i);
				continue;
			}
			int num3 = (int)全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地2].将领信息表[第几个将领].详细信息.忠诚;
			num2 = UnityEngine.Random.Range(0, 300);
			if (num2 <= num3)
			{
				UnityEngine.Debug.Log("劝降失败,逃跑" + num2.ToString() + "/" + num3.ToString());
				全局变量.提示类.显示信息("劝降失败,逃跑!");
				全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地2].将领信息表[第几个将领].详细信息.忠诚 = 60.0;
				全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地2].将领信息表[第几个将领].详细信息.状态 = 0.0;
				全局变量.所有玩家数据表[本机身份].封地信息表[第几个封地].俘虏信息表.RemoveAt(i);
				continue;
			}
			全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地2].将领信息表[第几个将领].详细信息.忠诚 = 全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地2].将领信息表[第几个将领].详细信息.忠诚 - 2.0;
			if (全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地2].将领信息表[第几个将领].详细信息.忠诚 < 0.0)
			{
				全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地2].将领信息表[第几个将领].详细信息.忠诚 = 0.0;
			}
			UnityEngine.Debug.Log("劝降失败,冷却" + num2.ToString() + "/" + num3.ToString());
			全局变量.提示类.显示信息("劝降失败!");
		}
	    显示俘虏列表();
	}

	public void 解散闲兵()
	{
        if (!封地有效()) return;
		int 本机身份 = 全局变量.本机身份;
		int 第几个封地 = 全局变量.第几个封地;
		List<闲兵信息> 闲兵信息表 = 全局变量.所有玩家数据表[本机身份].封地信息表[第几个封地].闲兵信息表;
		int childCount = 闲兵列表对象.transform.childCount;
		int num = 0;
		while (true)
		{
			if (num < childCount)
			{
				GameObject gameObject = 闲兵列表对象.transform.GetChild(num).gameObject;
				if (gameObject.activeSelf && gameObject.transform.GetChild(15).gameObject.activeSelf)
				{
					break;
				}
				num++;
				continue;
			}
			return;
		}
		if (num >= 闲兵信息表.Count) return;
        全局变量.所有玩家数据表[本机身份].封地信息表[第几个封地].删除闲兵(闲兵信息表[num].ID, 闲兵信息表[num].数量);
		全局变量.提示类.显示信息("已全部解散!");
		显示闲兵列表();
	}

	public void 治疗伤兵()
	{
        if (!封地有效()) return;
		int 本机身份 = 全局变量.本机身份;
		int 第几个封地 = 全局变量.第几个封地;
		List<伤兵信息> 伤兵信息表 = 全局变量.所有玩家数据表[本机身份].封地信息表[第几个封地].伤兵信息表;
		int childCount = 伤兵列表对象.transform.childCount;
		int num = 0;
		while (true)
		{
			if (num < childCount)
			{
				GameObject gameObject = 伤兵列表对象.transform.GetChild(num).gameObject;
				if (gameObject.activeSelf && gameObject.transform.GetChild(15).gameObject.activeSelf)
				{
					break;
				}
				num++;
				continue;
			}
			return;
		}
		if (num >= 伤兵信息表.Count) return;
        调整数量脚本对象.调整类型 = 4;
		调整数量脚本对象.第几个玩家 = 本机身份;
		调整数量脚本对象.封地信息界面UI脚本对象 = this;
		调整数量脚本对象.第几个封地 = 第几个封地;
		调整数量脚本对象.兵种ID = 伤兵信息表[num].ID;
		调整数量脚本对象.兵种数量 = 伤兵信息表[num].数量;
		调整数量脚本对象.gameObject.SetActive(value: true);
		调整数量脚本对象.显示说明文本();
	}

	public void 遣散伤兵()
	{
        if (!封地有效()) return;
		int 本机身份 = 全局变量.本机身份;
		int 第几个封地 = 全局变量.第几个封地;
		List<伤兵信息> 伤兵信息表 = 全局变量.所有玩家数据表[本机身份].封地信息表[第几个封地].伤兵信息表;
		int childCount = 伤兵列表对象.transform.childCount;
		int num = 0;
		while (true)
		{
			if (num < childCount)
			{
				GameObject gameObject = 伤兵列表对象.transform.GetChild(num).gameObject;
				if (gameObject.activeSelf && gameObject.transform.GetChild(15).gameObject.activeSelf)
				{
					break;
				}
				num++;
				continue;
			}
			return;
		}
		if (num >= 伤兵信息表.Count) return;
        全局变量.所有玩家数据表[本机身份].封地信息表[第几个封地].删除伤兵(伤兵信息表[num].ID, 伤兵信息表[num].数量);
        显示伤兵列表();
		全局变量.提示类.显示信息("已全部遣散!");
	}

	public void 释放俘虏()
	{
        if (!封地有效()) return;
		if (GameNetwork.Enabled)
		{
			GeneralsClientAdapter.ManageCaptives(全局变量.第几个封地, 选中联机俘虏(), true, 显示俘虏列表);
			return;
		}
		int 本机身份 = 全局变量.本机身份;
		int 第几个封地 = 全局变量.第几个封地;
		int childCount = 俘虏列表对象.transform.childCount;
		for (int i = childCount - 1; i >= 0; i--)
		{
			GameObject gameObject = 俘虏列表对象.transform.GetChild(i).gameObject;
			if (gameObject.activeSelf && gameObject.transform.GetChild(6).gameObject.activeSelf)
			{
				俘虏列表 = 全局变量.所有玩家数据表[本机身份].封地信息表[第几个封地].俘虏信息表;
				if (i >= 俘虏列表.Count) continue;
            int 第几个玩家 = 俘虏列表[i].第几个玩家;
            if (第几个玩家 < 0 || 第几个玩家 >= 全局变量.所有玩家数据表.Count) continue;
				int 将领ID标识 = 俘虏列表[i].将领ID标识;
				返回将领索引 返回将领索引 = 全局变量.所有玩家数据表[俘虏列表[i].第几个玩家].获取指定ID标识的将领索引(将领ID标识);
				if (返回将领索引 == null || 返回将领索引.第几个封地 < 0 || 返回将领索引.第几个将领 < 0) continue;
            int 第几个封地2 = 返回将领索引.第几个封地;
				int 第几个将领 = 返回将领索引.第几个将领;
				全局变量.所有玩家数据表[第几个玩家].封地信息表[第几个封地2].将领信息表[第几个将领].详细信息.状态 = 0.0;
				全局变量.所有玩家数据表[本机身份].封地信息表[第几个封地].俘虏信息表.RemoveAt(i);
				UnityEngine.Debug.Log("释放成功!");
			}
		}
	    显示俘虏列表();
	}

	public void 选中高亮()
	{
		int childCount = 俘虏列表对象.transform.childCount;
		for (int i = 0; i < childCount; i++)
		{
			GameObject gameObject = 俘虏列表对象.transform.GetChild(i).gameObject;
			Text component = gameObject.transform.GetChild(2).GetComponent<Text>();
			Text component2 = gameObject.transform.GetChild(3).GetComponent<Text>();
			Text component3 = gameObject.transform.GetChild(4).GetComponent<Text>();
			Text component4 = gameObject.transform.GetChild(5).GetComponent<Text>();
			if (gameObject.transform.GetChild(6).gameObject.activeSelf)
			{
				component.color = 颜色类.GetColor("#329696");
				component2.color = 颜色类.GetColor("#FFF019");
				component3.color = 颜色类.GetColor("#78FFC8");
				component4.color = 颜色类.GetColor("#FFF019");
			}
			else
			{
				component.color = 颜色类.GetColor("#C8C8C8");
				component2.color = 颜色类.GetColor("#C8C8C8");
				component3.color = 颜色类.GetColor("#C8C8C8");
				component4.color = 颜色类.GetColor("#C8C8C8");
			}
		}
	}
}
