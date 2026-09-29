using Dwsg.Administration;
using Dwsg.Network;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Events;
using Dwsg.Window1;

namespace Dwsg.Window3
{
    public enum CityPage { Civic, Scout, Bookmarks, Confirm, Lord, Nation }

    public static class CityNavigation
    {
        // Window 2 receives a nation code or stable player ID; return true when its UI opens.
        public static Func<string, bool> ShowNation;
        public static Func<int, bool> ShowLord;
        public static T Find<T>() where T : Component
        {
            return Find<T>(null);
        }
        public static T Find<T>(Func<T, bool> ready) where T : Component
        {
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                foreach (var component in root.GetComponentsInChildren<T>(true))
                    if (ready == null || ready(component)) return component;
            }
            return null;
        }
        public static bool ManagedRoot(Component component)
        {
            var canvas = component.transform.root.GetComponent<Canvas>();
            return canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay && canvas.sortingOrder >= 2 && canvas.sortingOrder < 10;
        }
    }

    public sealed class CityPanels
    {
        private readonly Dictionary<CityPage, CityPanel> pages = new Dictionary<CityPage, CityPanel>();
        internal readonly 城池信息显示脚本 View;
        internal readonly Font Font;
        internal readonly Window1Style NativeStyle;
        internal readonly Sprite ButtonSprite, CloseSprite, InfoSprite;
        internal readonly RectTransform TitleDecor, FrameDecor, InfoDecor, BorderDecor, CloseDecor;
        private readonly Sprite oldTitleText;
        internal readonly 所有城池界面脚本 Map;
        private readonly 主界面UI脚本 main;

        public CityPanels(城池信息显示脚本 view)
        {
            View = view; Font = view.城池名字坐标.font;
            NativeStyle = Window1Style.FromScene(view.gameObject.scene);
            TitleDecor = DecorNamed("标题栏背景"); FrameDecor = DecorNamed("黄色背景图");
            InfoDecor = DecorNamed("通用界面背景图"); BorderDecor = DecorNamed("通用界面边框");
            var infoImage = InfoDecor == null ? null : InfoDecor.GetComponentsInChildren<Image>(true).FirstOrDefault(i => i.sprite != null);
            InfoSprite = infoImage == null ? null : infoImage.sprite;
            // This is the old title's raster lettering; all new titles are real UGUI Text.
            var titleText = TitleDecor == null ? null : TitleDecor.Find("8 (21)");
            oldTitleText = titleText == null || titleText.GetComponent<Image>() == null ? null : titleText.GetComponent<Image>().sprite;
            var button = view.GetComponentsInChildren<Button>(true).FirstOrDefault(b => b.name == "修筑城池");
            ButtonSprite = button == null ? null : button.GetComponent<Image>().sprite;
            var close = TitleDecor == null ? null : TitleDecor.GetComponentsInChildren<Button>(true).FirstOrDefault(b => b.name == "关闭");
            CloseDecor = close == null ? null : (RectTransform)close.transform;
            var closeImage = close == null ? null : close.GetComponent<Image>();
            CloseSprite = closeImage == null ? null : closeImage.sprite;
            Map = CityNavigation.Find<所有城池界面脚本>(m => m.城池信息界面UI == view.gameObject && m.滑动对象 != null);
            main = CityNavigation.Find<主界面UI脚本>(m => m.camera1 != null && m.camera2 != null && m.camera3 != null && m.大地图布局对象 != null && m.封地布局对象 != null);
        }
        private RectTransform DecorNamed(string name)
        {
            return View.GetComponentsInChildren<RectTransform>(true).FirstOrDefault(r => r.name == name);
        }
        private Rect BoundsInView(RectTransform source)
        {
            var corners = new Vector3[4]; source.GetWorldCorners(corners);
            Vector2 min = View.transform.InverseTransformPoint(corners[0]), max = min;
            for (int i = 1; i < corners.Length; i++)
            {
                Vector2 point = View.transform.InverseTransformPoint(corners[i]);
                min = Vector2.Min(min, point); max = Vector2.Max(max, point);
            }
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }
        internal void WindowDecor(RectTransform target, RectTransform title, RectTransform close)
        {
            if (FrameDecor == null || TitleDecor == null) return;
            Rect frameBounds = BoundsInView(FrameDecor), titleBounds = BoundsInView(TitleDecor);
            Rect bounds = Rect.MinMaxRect(Mathf.Min(frameBounds.xMin, titleBounds.xMin), Mathf.Min(frameBounds.yMin, titleBounds.yMin),
                Mathf.Max(frameBounds.xMax, titleBounds.xMax), Mathf.Max(frameBounds.yMax, titleBounds.yMax));
            if (bounds.width <= 0 || bounds.height <= 0) return;
            Vector2 scale = new Vector2(target.rect.width / bounds.width, target.rect.height / bounds.height);
            var skin = (RectTransform)new GameObject("原城池框架", typeof(RectTransform)).transform; skin.SetParent(target, false);
            skin.SetAsFirstSibling(); skin.anchorMin = skin.anchorMax = new Vector2(.5f, .5f);
            var nativeView = (RectTransform)View.transform; skin.pivot = nativeView.pivot; skin.sizeDelta = nativeView.rect.size;
            skin.anchoredPosition = -Vector2.Scale(bounds.center, scale); skin.localScale = new Vector3(scale.x, scale.y, 1);
            // Both roots belong to the native view. Keep that coordinate space so
            // their anchors, sliced borders and title overlap share one transform.
            CopyDecor(FrameDecor, skin); CopyDecor(TitleDecor, skin);
            title.sizeDelta = Vector2.Scale(titleBounds.size, scale);
            title.anchoredPosition = Vector2.Scale(titleBounds.center - bounds.center, scale);
            if (CloseDecor != null)
            {
                Rect closeBounds = BoundsInView(CloseDecor);
                close.sizeDelta = Vector2.Scale(closeBounds.size, scale);
                close.anchoredPosition = Vector2.Scale(closeBounds.center - bounds.center, scale);
            }
        }
        internal void Decor(RectTransform source, RectTransform target)
        {
            if (source == null) return;
            var root = CopyDecor(source, target);
            if (root == null) return;
            Vector2 size = source.rect.size;
            if (size.x <= 0 || size.y <= 0) size = target.rect.size;
            root.anchorMin = root.anchorMax = new Vector2(.5f, .5f); root.anchoredPosition = Vector2.zero;
            root.sizeDelta = size; root.localScale = new Vector3(target.rect.width / size.x, target.rect.height / size.y, 1);
        }
        private RectTransform CopyDecor(RectTransform source, Transform parent)
        {
            if (source.GetComponent<Selectable>() != null || source.GetComponent<Text>() != null) return null;
            var image = source.GetComponent<Image>();
            if (image != null && image.sprite != null && image.sprite == oldTitleText) return null;
            var go = new GameObject("装饰_" + source.name, typeof(RectTransform)); var r = (RectTransform)go.transform; r.SetParent(parent, false);
            r.anchorMin = source.anchorMin; r.anchorMax = source.anchorMax; r.pivot = source.pivot;
            r.sizeDelta = source.sizeDelta; r.anchoredPosition = source.anchoredPosition; r.localRotation = source.localRotation; r.localScale = source.localScale;
            if (image != null && image.sprite != null)
            {
                var copy = go.AddComponent<Image>(); copy.sprite = image.sprite; copy.type = image.type; copy.color = image.color;
                copy.preserveAspect = image.preserveAspect; copy.fillCenter = image.fillCenter; copy.fillMethod = image.fillMethod;
                copy.fillAmount = image.fillAmount; copy.fillOrigin = image.fillOrigin; copy.fillClockwise = image.fillClockwise;
                copy.pixelsPerUnitMultiplier = image.pixelsPerUnitMultiplier; copy.raycastTarget = false;
            }
            foreach (Transform child in source) if (child is RectTransform) CopyDecor((RectTransform)child, r);
            return r;
        }
        private CityPanel Get(CityPage page)
        {
            CityPanel panel;
            if (pages.TryGetValue(page, out panel) && panel != null) return panel;
            var root = new GameObject("窗口3_" + page, typeof(RectTransform)); root.SetActive(false);
            var canvas = root.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 7;
            var scaler = root.AddComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(960, 540); scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            root.AddComponent<GraphicRaycaster>(); panel = root.AddComponent<CityPanel>(); panel.Build(this, page);
            if (!界面窗口管理器.注册运行时窗口(root)) { UnityEngine.Object.Destroy(root); return null; }
            pages[page] = panel; return panel;
        }
        public void Open(CityPage page, int x, int y, int tab = 0)
        {
            var panel = Get(page); if (panel == null) { Notice("窗口管理器尚未就绪，请重新打开城池。"); return; }
            panel.X = x; panel.Y = y; panel.Tab = page == CityPage.Civic ? Mathf.Clamp(tab, 1, 4) : tab;
            if (panel.gameObject.activeSelf) panel.Render(); else panel.gameObject.SetActive(true);
        }
        public void Confirm(int x, int y, string heading, string detail, Action<Action<CityResult>> command)
        {
            var panel = Get(CityPage.Confirm); if (panel == null) return;
            panel.X = x; panel.Y = y; panel.ConfirmationTitle = heading; panel.ConfirmationDetail = detail; panel.Command = command;
            if (panel.gameObject.activeSelf) panel.Render(); else panel.gameObject.SetActive(true);
        }
        public void Notice(string text) { if (全局变量.提示类 != null) 全局变量.提示类.显示信息(text); }
        public CityResult Locate(int x, int y)
        {
            if (CityLocalAdapter.City(x, y) == null) return CityResult.Fail("收藏坐标已不存在，请移除该项。");
            if (Map == null || Map.滑动对象 == null || main == null) return CityResult.Fail("地图尚未就绪，请稍后重试。");
            main.加载大地图场景(); Map.定位地图到指定位置(x, y);
            if (Map.当前城池坐标显示 != null) Map.当前城池坐标显示.text = x + "," + y;
            return CityResult.Ok("已定位到城池坐标。");
        }
        internal void OpenCity(int x, int y)
        {
            int index = 全局变量.所有城池列表.FindIndex(c => c.坐标x == x && c.坐标y == y);
            if (index < 0) { Notice("城池已不存在。"); return; }
            View.显示城池信息(index); View.gameObject.SetActive(true);
        }
    }

    public sealed class CityPanel : MonoBehaviour
    {
        private CityPanels ui;
        private CityPage page;
        private Text heading, subtitle, feedback;
        private RectTransform content, footer, tabs;
        private ScrollRect scroll;
        private LayoutElement contentMinimum;
        private readonly List<Button> tabButtons = new List<Button>();
        private readonly List<GameObject> rows = new List<GameObject>();
        private Coroutine ticker;
        private Text progress;
        private CityRepairOrder displayedRepair;
        public int X, Y, Tab;
        internal string ConfirmationTitle, ConfirmationDetail;
        internal Action<Action<CityResult>> Command;
        private bool submitting;
        private int viewGeneration;
        private static readonly Color Ink = new Color(.96f, .94f, .73f);
        private static readonly Color Gold = new Color(.9f, .75f, .33f);
        private static readonly Color ButtonGold = new Color(.847f, .835f, .584f);
        private static readonly Color TitleGold = new Color(.884f, .894f, .66f);
        private static readonly Color Quiet = new Color(.57f, .86f, .77f);

        private RectTransform Box(string name, Transform parent, Vector2 size, Vector2 position)
        {
            var go = new GameObject(name, typeof(RectTransform)); var r = (RectTransform)go.transform; r.SetParent(parent, false);
            r.anchorMin = r.anchorMax = new Vector2(.5f, .5f); r.sizeDelta = size; r.anchoredPosition = position; return r;
        }
        private Image Image(RectTransform rect, Sprite sprite, Color color)
        {
            var image = rect.gameObject.AddComponent<Image>(); image.sprite = sprite; image.color = color;
            image.type = sprite != null && sprite.border.sqrMagnitude > 0 ? UnityEngine.UI.Image.Type.Sliced : UnityEngine.UI.Image.Type.Simple;
            image.raycastTarget = false; return image;
        }
        private Text Label(string name, Transform parent, string value, Vector2 size, Vector2 position, int fontSize = 16, Color? color = null)
        {
            var r = Box(name, parent, size, position); var text = r.gameObject.AddComponent<Text>(); text.font = ui.Font;
            text.fontSize = fontSize; text.color = color ?? Ink; text.text = value; text.supportRichText = false;
            text.alignment = TextAnchor.MiddleLeft; text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Truncate; text.raycastTarget = false;
            return text;
        }
        private Button Button(string name, Transform parent, string label, Vector2 size, Vector2 position, UnityAction action)
        {
            var r = Box(name, parent, size, position); var image = Image(r, ui.ButtonSprite, ui.ButtonSprite == null ? new Color(.33f, .26f, .12f) : Color.white);
            image.raycastTarget = true; var button = r.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            var colors = button.colors; colors.highlightedColor = new Color(1f, .94f, .71f); colors.pressedColor = new Color(.71f, .66f, .42f); colors.disabledColor = new Color(.42f, .48f, .44f); button.colors = colors;
            var text = Label("文字", r, label, size - new Vector2(8, 4), Vector2.zero, 16, ButtonGold); text.alignment = TextAnchor.MiddleCenter;
            原界面文字样式.按钮(text);
            if (action != null) button.onClick.AddListener(action);
            界面窗口管理器.注册运行时按钮(button); return button;
        }
        private void SetEnabled(Button button, bool enabled)
        {
            button.interactable = enabled;
        }
        internal void Build(CityPanels owner, CityPage kind)
        {
            ui = owner; page = kind;
            var shade = Box("遮罩", transform, Vector2.zero, Vector2.zero); shade.anchorMin = Vector2.zero; shade.anchorMax = Vector2.one;
            Image(shade, null, new Color(0, 0, 0, .16f)).raycastTarget = true;
            var pane = Box("城池窗口", transform, new Vector2(720, 492), Vector2.zero);
            Image(pane, null, Color.clear).raycastTarget = true;
            var title = Box("标题栏背景", pane, new Vector2(716, 43), new Vector2(0, 224));
            Image(title, null, Color.clear);
            var closeRect = Box("关闭", pane, new Vector2(43, 38), new Vector2(330, 224));
            ui.WindowDecor(pane, title, closeRect);
            heading = Label("标题", pane, "城池内政", new Vector2(565, 38), title.anchoredPosition, 21, TitleGold); heading.alignment = TextAnchor.MiddleCenter;
            原界面文字样式.标题(heading);
            var closeImage = Image(closeRect, ui.CloseSprite, Color.white); closeImage.type = UnityEngine.UI.Image.Type.Simple;
            closeImage.preserveAspect = true; closeImage.raycastTarget = true;
            var closeButton = closeRect.gameObject.AddComponent<Button>(); closeButton.targetGraphic = closeImage;
            closeButton.onClick.AddListener(() => gameObject.SetActive(false)); 界面窗口管理器.注册运行时按钮(closeButton);
            var info = Box("信息背景", pane, new Vector2(682, 354), new Vector2(0, 16));
            // The old forest child is larger than its masked parent. Fit the actual local
            // background sprite to this information area instead of copying its oversize rect.
            Image(info, ui.InfoSprite, ui.InfoSprite == null ? new Color(.04f, .17f, .14f) : Color.white);
            ui.Decor(ui.BorderDecor, Box("信息边框", pane, new Vector2(682, 354), new Vector2(0, 16)));
            subtitle = Label("城池坐标与状态", pane, "城池状态", new Vector2(636, 38), new Vector2(0, 161), 14, Quiet);
            tabs = Box("分页", pane, new Vector2(644, 33), new Vector2(0, 121));
            var tabLayout = tabs.gameObject.AddComponent<GridLayoutGroup>();
            tabLayout.cellSize = new Vector2(150, 32); tabLayout.spacing = new Vector2(6, 0);
            tabLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount; tabLayout.constraintCount = 4;
            tabLayout.childAlignment = TextAnchor.MiddleCenter;
            string[] names = { "驻防", "封地", "修筑", "政务" };
            for (int i = 0; i < names.Length; i++)
            {
                int index = i + 1; tabButtons.Add(Button("页签_" + names[i], tabs, names[i], new Vector2(150, 32), Vector2.zero, () => { Tab = index; Render(); }));
            }
            var viewport = Box("滚动视口", pane, new Vector2(640, 218), new Vector2(-5, -13));
            Image(viewport, null, new Color(.04f, .13f, .10f, .01f)).raycastTarget = true;
            viewport.gameObject.AddComponent<RectMask2D>();
            scroll = viewport.gameObject.AddComponent<ScrollRect>(); scroll.horizontal = false; scroll.vertical = true; scroll.viewport = viewport;
            scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 28;
            content = Box("列表", viewport, new Vector2(640, 0), Vector2.zero); content.anchorMin = new Vector2(0, 1); content.anchorMax = new Vector2(1, 1); content.pivot = new Vector2(.5f, 1); content.sizeDelta = Vector2.zero; content.anchoredPosition = Vector2.zero;
            var listLayout = content.gameObject.AddComponent<VerticalLayoutGroup>(); listLayout.childAlignment = TextAnchor.UpperCenter;
            listLayout.childControlWidth = true; listLayout.childForceExpandWidth = true;
            listLayout.childControlHeight = true; listLayout.childForceExpandHeight = false;
            contentMinimum = content.gameObject.AddComponent<LayoutElement>(); contentMinimum.minHeight = 218;
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>(); fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = content;
            var bar = ui.NativeStyle.VerticalScrollbar(pane); var barRect = (RectTransform)bar.transform;
            barRect.anchorMin = barRect.anchorMax = new Vector2(.5f,.5f); barRect.pivot = new Vector2(.5f,.5f);
            barRect.sizeDelta = new Vector2(12,218); barRect.anchoredPosition = new Vector2(329,-13); scroll.verticalScrollbar = bar;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            feedback = Label("操作反馈", pane, "", new Vector2(642, 35), new Vector2(0, -141), 13, Quiet);
            footer = Box("操作栏", pane, new Vector2(644, 39), new Vector2(0, -195));
        }
        private void OnEnable()
        {
            if (ui == null) return;
            Render(); ticker = StartCoroutine(Tick());
        }
        private void OnDisable() { if (ticker != null) StopCoroutine(ticker); ticker = null; }
        private IEnumerator Tick()
        {
            while (gameObject.activeInHierarchy)
            {
                yield return new WaitForSecondsRealtime(1);
                CityLocalAdapter.Local.Settle();
                if (progress != null)
                {
                    var pending = CityLocalAdapter.Local.Pending(X, Y);
                    if (displayedRepair != null && pending == null)
                    {
                        var finished = displayedRepair; var city = CityLocalAdapter.City(X, Y);
                        bool cancelled = city == null || city.正在交战 || city.城主 != finished.Owner || city.国家 != finished.Nation;
                        Render(); Result(cancelled ? CityResult.Fail("修筑已取消，费用已退还。") : CityResult.Ok((finished.Kind == CityRepairKind.Wall ? "城墙" : "道路") + "修筑完成。"));
                    }
                    else progress.text = ProgressText();
                }
            }
        }
        private void Clear()
        {
            foreach (var row in rows) if (row != null) { row.SetActive(false); Destroy(row); }
            rows.Clear(); progress = null;
            foreach (Transform child in footer) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
        }
        private RectTransform Row(string text, float rowHeight = 44, float reserve = 0)
        {
            var r = Box("城池信息行", content, new Vector2(640, rowHeight), Vector2.zero);
            r.pivot = new Vector2(.5f, 1); rows.Add(r.gameObject);
            var label = Label("信息", r, text, new Vector2(620 - reserve, rowHeight - 8), new Vector2(-reserve / 2, 0), 16);
            var rowLayout = r.gameObject.AddComponent<LayoutElement>(); rowLayout.minHeight = rowHeight;
            rowLayout.preferredHeight = Mathf.Max(rowHeight, label.preferredHeight + 12); rowLayout.flexibleHeight = 0;
            label.rectTransform.anchorMin = new Vector2(.5f, 0); label.rectTransform.anchorMax = new Vector2(.5f, 1);
            label.rectTransform.sizeDelta = new Vector2(620 - reserve, -8);
            var separator = Box("分隔线", r, new Vector2(620, 1), Vector2.zero);
            separator.anchorMin = separator.anchorMax = new Vector2(.5f, 0);
            Image(separator, null, new Color(.56f, .47f, .21f, .45f));
            return r;
        }
        private void ActionRow(string text, string button, UnityAction action, bool enabled = true, float rowHeight = 52)
        {
            var row = Row(text, rowHeight, 132);
            SetEnabled(Button(button, row, button, new Vector2(112, 35), new Vector2(250, 0), action), enabled);
        }
        private void Footer(string name, float x, UnityAction action, bool enabled = true)
        {
            // Original city return button: 97x39, flush with the 644-wide operation row.
            SetEnabled(Button(name, footer, name, new Vector2(97, 39), new Vector2(x * (273.5f / 240f), 0), action), enabled);
        }
        private void Run(Action<Action<CityResult>> command, bool refresh = true)
        {
            if (submitting) return;
            submitting = true; int x = X, y = Y, tab = Tab, generation = viewGeneration;
            Result(CityResult.Ok("正在提交，请稍候。"));
            command(result =>
            {
                if (this == null) return;
                submitting = false;
                if (!gameObject.activeInHierarchy || generation != viewGeneration || X != x || Y != y || Tab != tab) return;
                if (refresh) Render(); Result(result);
            });
        }
        private void Result(CityResult result)
        {
            feedback.text = result.Message; feedback.color = result.Success ? Quiet : new Color(1, .77f, .47f);
        }
        private static string Number(double v) { return v.ToString("N0"); }
        public void Render()
        {
            if (ui == null) return;
            viewGeneration++;
            Clear(); CityLocalAdapter.Local.Settle(); var c = CityLocalAdapter.City(X, Y);
            tabs.gameObject.SetActive(page == CityPage.Civic);
            float viewportHeight = page == CityPage.Civic ? 218 : 258;
            float viewportY = page == CityPage.Civic ? -13 : 7;
            scroll.viewport.sizeDelta = new Vector2(640, viewportHeight);
            scroll.viewport.anchoredPosition = new Vector2(-5, viewportY);
            contentMinimum.minHeight = viewportHeight;
            var barRect = (RectTransform)scroll.verticalScrollbar.transform;
            barRect.sizeDelta = new Vector2(12, viewportHeight); barRect.anchoredPosition = new Vector2(329, viewportY);
            heading.text = page == CityPage.Civic ? "城池内政" : page == CityPage.Scout ? "侦查结果" : page == CityPage.Bookmarks ? "城池收藏册" : page == CityPage.Confirm ? ConfirmationTitle : page == CityPage.Lord ? "本城城主" : "本城归属";
            string cityName = c == null ? "" : c.名称 ?? "";
            subtitle.text = c == null ? "城池收藏" : (cityName.Length > 24 ? cityName.Substring(0, 24) + "…" : cityName) + "（" + X + "," + Y + "）  · " + (c.正在交战 ? "交战中" : "和平");
            feedback.text = ""; feedback.color = Quiet;
            for (int i = 0; i < tabButtons.Count; i++) SetEnabled(tabButtons[i], i + 1 != Tab);
            if (page == CityPage.Bookmarks) RenderBookmarks();
            else if (c == null) Row("这座城池已不存在。返回地图后重新选择城池。", 70);
            else if (page == CityPage.Confirm) RenderConfirm();
            else if (page == CityPage.Scout) RenderScout();
            else if (page == CityPage.Lord) RenderLord(c);
            else if (page == CityPage.Nation) RenderNation(c);
            else
            {
                if (Tab == 1) RenderDefenders(c);
                else if (Tab == 2) RenderFiefs(c);
                else if (Tab == 3) RenderRepairs(c);
                else RenderGovernment(c);
                Footer("侦查", -240, () => ui.Open(CityPage.Scout, X, Y));
                Footer("收藏册", -80, () => ui.Open(CityPage.Bookmarks, X, Y));
                Footer(CityLocalAdapter.Local.IsBookmarked(X, Y) ? "取消收藏" : "收藏本城", 80, () => Run(done => AdministrationClient.Bookmark(X, Y, !CityLocalAdapter.Local.IsBookmarked(X, Y), done)));
                Footer("返回", 240, () => gameObject.SetActive(false));
            }
            content.anchoredPosition = Vector2.zero;
            Canvas.ForceUpdateCanvases(); scroll.StopMovement(); scroll.verticalNormalizedPosition = 1;
        }
        private void RenderDefenders(城池信息库类 c)
        {
            if (!CityLocalAdapter.Friendly(c)) { Row("驻防详情仅对本国城池开放。", 96); return; }
            var data = CityLocalAdapter.DefenderRows(c);
            Row("驻防将领：" + data.Count + "    驻防上限：" + Number(c.获取驻防上限()), 48);
            foreach (var entry in data) Row(entry, 62);
            if (data.Count == 0) Row("暂无驻防将领。可选择将领派遣驻防。", 88);
            ActionRow(c.正在交战 ? "选择将领，派往本城支援" : "选择将领，派往本城驻防", c.正在交战 ? "援军驻防" : "派遣驻防", ui.View.出征驻防本城池);
        }
        private void RenderFiefs(城池信息库类 c)
        {
            if (GameNetwork.Enabled)
            {
                var projected = AdministrationClient.PublicCity(X, Y);
                Row("本城封地：" + (projected?.Value<int>("封地数量") ?? 0) + "/" + c.获取封地上限(), 58);
                var residents = projected?["居民封地"] as JArray;
                if (residents != null) foreach (JObject entry in residents)
                {
                    int index = AdministrationClient.PlayerIndex(entry.Value<string>("playerId")); var owner = CityLocalAdapter.Player(index);
                    string text = entry.Value<string>("name") + "  · " + (owner == null ? "君主" : owner.基础信息.名字);
                    if (index == 全局变量.本机身份) ActionRow(text, "进入封地", ui.View.进入封地, true, 58); else Row(text, 58);
                }
                if (residents == null) Row("敌方封地只公开数量。", 72);
                else if (residents.Count == 0) Row("暂无封地。", 72);
                if (CityLocalAdapter.Friendly(c) && !c.是否有我的封地()) ActionRow("在本城建立封地", "开辟封地", () => { ui.View.开辟封地(); Render(); });
                return;
            }
            Row("本城封地：" + c.城池封地列表.Count + "/" + c.获取封地上限(), 58);
            foreach (var index in c.城池封地列表)
            {
                var p = index == null ? null : CityLocalAdapter.Player(index.第几个玩家);
                var f = p == null ? null : p.封地信息表.FirstOrDefault(v => v.ID == index.封地ID标识);
                if (f == null) { Row("封地已迁出。", 52); continue; }
                if (p == CityLocalAdapter.Me) ActionRow(f.封地名字 + "  · 我的封地", "进入封地", ui.View.进入封地, true, 58);
                else Row(f.封地名字 + "  · " + p.基础信息.名字, 58);
            }
            if (c.城池封地列表.Count == 0) Row("暂无封地。每位君主最多10座，同城只能开辟1座。", 72);
            if (CityLocalAdapter.Friendly(c) && !c.是否有我的封地()) ActionRow("在本城建立封地", "开辟封地", () => { ui.View.开辟封地(); Render(); });
        }
        private string ProgressText()
        {
            var p = CityLocalAdapter.Local.Pending(X, Y);
            if (p == null) return "暂无修筑任务";
            var seconds = Math.Max(0, p.EndsUtc - CityLocalAdapter.Local.UtcNow());
            return (p.Kind == CityRepairKind.Wall ? "城墙" : "道路") + "修筑：" + Number(p.Amount) + "  进度 " + Number(Math.Max(0, Math.Min(100, (30 - seconds) * 100.0 / 30))) + "%  · 剩余 " + seconds + "秒";
        }
        private void RenderRepairs(城池信息库类 c)
        {
            Row("修筑耗时30秒，同城同时可进行1项。", 44);
            Row("城墙：" + Number(c.城墙) + "/" + Number(c.获取城墙上限()) + "    道路：" + Number(c.道路) + "/" + Number(c.获取道路上限()), 60);
            displayedRepair = CityLocalAdapter.Local.Pending(X, Y);
            var row = Row("", 48); progress = row.GetComponentInChildren<Text>(); progress.text = ProgressText();
            foreach (CityRepairKind kind in Enum.GetValues(typeof(CityRepairKind)))
            {
                var quote = CityLocalAdapter.Local.Quote(X, Y, kind); var captured = kind;
                string label = kind == CityRepairKind.Wall ? "修筑城墙" : "修筑道路";
                ActionRow(label + "：+" + Number(quote.Amount) + " · 铜钱" + Number(quote.Copper) + " / 粮食" + Number(quote.Food) + (quote.Allowed ? "" : "\n" + quote.Error),
                    "确认修筑", () => PrepareRepair(captured), quote.Allowed, 80);
            }
            Row("限自己或本国城池。交战或归属变化时取消并退费。", 52);
            ActionRow("更新设施和进度", "刷新", Render);
        }
        private void PrepareRepair(CityRepairKind kind)
        {
            var quote = CityLocalAdapter.Local.Quote(X, Y, kind);
            if (!quote.Allowed) { Result(CityResult.Fail(quote.Error)); return; }
            string request = Guid.NewGuid().ToString("N");
            ui.Confirm(X, Y, kind == CityRepairKind.Wall ? "确认修筑城墙" : "确认修筑道路",
                "修复 " + Number(quote.Amount) + "\n费用：铜 " + Number(quote.Copper) + " / 粮 " + Number(quote.Food) + "\n耗时：30秒；同城只可进行一个任务。",
                done => AdministrationClient.Repair(quote, request, done));
        }
        private void RenderGovernment(城池信息库类 c)
        {
            Row("公告：" + (string.IsNullOrEmpty(c.公告) ? "本城尚未发布公告。" : c.公告), 64);
            Row("每类税收间隔24小时。城主收入归个人，国家收入归国库。", 60);
            foreach (CityTaxKind kind in Enum.GetValues(typeof(CityTaxKind)))
            {
                var captured = kind; string permission = CityLocalAdapter.Local.TaxPermission(X, Y, kind);
                string name = kind == CityTaxKind.Lord ? "城主征收" : "国家征收";
                ActionRow(name + "：铜钱" + Number(kind == CityTaxKind.Lord ? c.城主征收_铜 : c.国家征收_铜) + " / 粮食" + Number(kind == CityTaxKind.Lord ? c.城主征收_粮 : c.国家征收_粮) + (permission == null ? "" : "\n" + permission), name,
                    () => PrepareTax(captured), permission == null, 64);
            }
            string candidate = CityLocalAdapter.Local.CandidatePermission(X, Y);
            ActionRow(candidate ?? "在本城有封地的本国君主可登记候选，免费。", "竞选登记", () => Run(done => AdministrationClient.Apply(X, Y, done)), candidate == null, 64);
            Row("城主由国王从候选中任命。", 44);
            var n = 全局方法类.获取指定名字的国家(c.国家);
            var list = CityLocalAdapter.Local.Candidates(X, Y).Select(a => new { Record = a, Player = 全局变量.所有玩家数据表.FirstOrDefault(p => p.基础信息.ID == a.PlayerId) }).Where(a => a.Player != null).OrderByDescending(a => a.Player.基础信息.贡献).ToList();
            foreach (var a in list)
            {
                int id = a.Record.PlayerId, expectedOwner = c.城主; string expectedNation = c.国家; string name = a.Player.基础信息.名字; string request = Guid.NewGuid().ToString("N");
                ActionRow(name + " · 贡献 " + Number(a.Player.基础信息.贡献), "任命",
                    () => ui.Confirm(X, Y, "确认任命城主", "任命" + name + "\n将替换当前城主。", done => AdministrationClient.AppointCity(X, Y, id, expectedOwner, expectedNation, request, done)),
                    n != null && CityLocalAdapter.Me != null && n.国王 == CityLocalAdapter.Me.基础信息.ID && CityLocalAdapter.Me.基础信息.国家 == c.国家, 62);
            }
            if (list.Count == 0) Row("暂无候选。先登记，再由国王任命。", 72);
        }
        public void PrepareTax(CityTaxKind kind)
        {
            var c = CityLocalAdapter.City(X, Y); string permission = CityLocalAdapter.Local.TaxPermission(X, Y, kind);
            if (permission != null) { Result(CityResult.Fail(permission)); return; }
            string request = Guid.NewGuid().ToString("N");
            double copper = kind == CityTaxKind.Lord ? c.城主征收_铜 : c.国家征收_铜;
            double food = kind == CityTaxKind.Lord ? c.城主征收_粮 : c.国家征收_粮;
            int owner = c.城主, actor = CityLocalAdapter.Me.基础信息.ID; string nation = c.国家;
            ui.Confirm(X, Y, kind == CityTaxKind.Lord ? "确认城主征收" : "确认国家征收", "征收铜钱 " + Number(copper) + " / 粮 " + Number(food) + "\n所得进入" + (kind == CityTaxKind.Lord ? "个人财产" : "国家国库") + "。下次征收需等待24小时。", done =>
            {
                var current = CityLocalAdapter.City(X, Y);
                if (current == null || CityLocalAdapter.Me == null || CityLocalAdapter.Me.基础信息.ID != actor || current.城主 != owner || current.国家 != nation ||
                    (kind == CityTaxKind.Lord ? current.城主征收_铜 : current.国家征收_铜) != copper || (kind == CityTaxKind.Lord ? current.城主征收_粮 : current.国家征收_粮) != food)
                { done(CityResult.Fail("城池归属或征收额度已变化，请重新确认。")); return; }
                AdministrationClient.Collect(X, Y, kind, owner, nation, copper, food, request, done);
            });
        }
        private void RenderConfirm()
        {
            Row(ConfirmationDetail ?? "请重新选择操作。", 185);
            var confirm = Button("确认操作", footer, "确认", new Vector2(140, 36), new Vector2(80, 0), null);
            confirm.onClick.AddListener(() =>
            {
                SetEnabled(confirm, false); var command = Command; Command = null;
                if (command == null) Result(CityResult.Fail("操作已经提交，请返回查看结果。"));
                else Run(command, false);
            });
            界面窗口管理器.注册运行时按钮(confirm);
            Footer("返回", 240, () => gameObject.SetActive(false));
        }
        private void RenderScout()
        {
            var report = CityLocalAdapter.Local.Scout(X, Y);
            Row(report.Connection, 72);
            Row("国家：" + report.Nation + "    城主：" + report.Lord, 54);
            Row("城墙：" + Number(report.Wall) + "/" + Number(report.WallLimit) + "    道路：" + Number(report.Road) + "/" + Number(report.RoadLimit), 65);
            Row("观察时间：" + DateTimeOffset.FromUnixTimeSeconds(report.ObservedUtc).ToLocalTime().ToString("MM-dd HH:mm:ss") + "    封地：" + report.Fiefs, 64);
            if (!report.CanReadDefenders) Row("仅显示地图公开信息。敌方兵力和资源未公开。", 94);
            else if (report.Defenders.Count == 0) Row("暂无驻防将领。", 54);
            else foreach (string entry in report.Defenders) Row(entry, 62);
            Footer("刷新观察", -80, Render); Footer("返回", 240, () => gameObject.SetActive(false));
        }
        private void RenderBookmarks()
        {
            Row("已收藏城池：最多128座。点“定位”跳转地图。", 48);
            var list = CityLocalAdapter.Local.Bookmarks();
            foreach (var b in list)
            {
                int x = b.X, y = b.Y; var c = CityLocalAdapter.City(x, y);
                var row = Row((c == null ? "失效城池" : c.名称) + "（" + x + "," + y + "）", 58, 250);
                SetEnabled(Button("定位收藏", row, "定位", new Vector2(70, 34), new Vector2(107, 0), () => Result(ui.Locate(x, y))), c != null);
                SetEnabled(Button("查看收藏", row, "查看", new Vector2(70, 34), new Vector2(183, 0), () => ui.OpenCity(x, y)), c != null);
                Button("移除收藏", row, "移除", new Vector2(70, 34), new Vector2(259, 0), () => Run(done => AdministrationClient.Bookmark(x, y, false, done)));
            }
            if (list.Count == 0) Row("暂无收藏。点“收藏本城”加入此城。", 90);
            Footer("收藏本城", -80, () => Run(done => AdministrationClient.Bookmark(X, Y, true, done)), CityLocalAdapter.City(X, Y) != null);
            Footer("返回", 240, () => gameObject.SetActive(false));
        }
        private void RenderLord(城池信息库类 c)
        {
            var p = CityLocalAdapter.Player(c.城主);
            if (p == null) Row("本城暂无城主。所属国家的君主可在政务页查看竞选资格。", 96);
            else
            {
                Row("城主：" + p.基础信息.名字 + "    国家：" + p.基础信息.国家, 60);
                Row("等级：" + p.基础信息.等级 + "    称号：" + p.基础信息.称号名, 55);
                Row("官职：" + p.基础信息.官职 + "    贡献：" + Number(p.基础信息.贡献), 55);
                Row("战功：" + Number(p.基础信息.战功) + "    声望：" + Number(p.基础信息.声望), 55);

            }
            Footer("返回", 240, () => gameObject.SetActive(false));
        }
        private void RenderNation(城池信息库类 c)
        {
            var n = 全局方法类.获取指定名字的国家(c.国家);
            if (n == null) Row("本城暂无所属国家。", 84);
            else
            {
                Row("本城所属：" + n.国名 + "（" + n.国号 + "）", 60);
                var king = 全局变量.所有玩家数据表.FirstOrDefault(p => p.基础信息.ID == n.国王);
                Row("国王：" + (king == null ? "记录失效" : king.基础信息.名字) + "    国都坐标：" + n.国都x + "," + n.国都y, 64);
                Row("国家公告：" + (string.IsNullOrEmpty(n.公告) ? "所属国家尚未发布公告。" : n.公告), 104);
                if (CityLocalAdapter.Me != null && CityLocalAdapter.Me.基础信息.国家 == c.国家)
                    ActionRow("查看国家详情", "进入国家", () =>
                    {
                        if (!ui.View.尝试打开本国界面()) Result(CityResult.Fail("本国信息界面未就绪。"));
                    });
            }
            Footer("返回", 240, () => gameObject.SetActive(false));
        }
    }
}
