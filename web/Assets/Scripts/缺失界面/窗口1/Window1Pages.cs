using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Dwsg.Window1
{
    public enum JournalPage { Growth, Daily, Achievement, Notices, Mail }

    // 仅在主场景装配一次；不克隆旧玩法组件，不扫描 Update，不修改场景文件。
    public sealed class Window1Installer : MonoBehaviour
    {
        private readonly List<Action> unbind = new List<Action>();
        private Window1Pages pages;
        private Text sidebar;
        private GameObject mainRoot;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            SceneManager.sceneLoaded -= OnScene;
            SceneManager.sceneLoaded += OnScene;
            OnScene(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }
        private static void OnScene(Scene scene, LoadSceneMode mode)
        {
            var roots = scene.GetRootGameObjects();
            if (roots.Any(r => r.GetComponent<Window1Installer>() != null)) return;
            var main = roots.FirstOrDefault(r => r.GetComponent<主界面UI脚本>() != null);
            if (main == null) return;
            var host = new GameObject("窗口1_任务成就邮件装配");
            SceneManager.MoveGameObjectToScene(host, scene);
            host.AddComponent<Window1Installer>().mainRoot = main;
        }
        private IEnumerator Start()
        {
            // 等待主界面的 Start 与共同导航初始化，不在运行期重复查找入口。
            yield return null;
            yield return null;
            for (int i = 0; i < 180 && Window1Module.Service == null; i++) yield return null;
            if (Window1Module.Service == null) { Debug.LogWarning("窗口1：世界未就绪，未装配任务邮件界面"); yield break; }
            string ignored; Window1Module.Service.Refresh(out ignored);
            var style = Window1Style.FromScene(gameObject.scene);
            pages = Window1Pages.Build(style);
            if (!界面窗口管理器.注册运行时窗口(pages.gameObject))
            { Debug.LogWarning("窗口1：共同导航未就绪，未绑定入口"); Destroy(pages.gameObject); yield break; }
            BindEntries();
            Window1Module.Changed += RefreshSidebar;
            RefreshSidebar();
            while (this != null)
            {
                // 侧栏是常驻可见摘要，只在主界面可见时查询数据；面板关闭不运行刷新循环。
                yield return new WaitForSecondsRealtime(5);
                if (mainRoot != null && mainRoot.activeInHierarchy && sidebar != null && sidebar.gameObject.activeInHierarchy) RefreshSidebar();
            }
        }
        private IEnumerable<Transform> SceneTransforms()
        { return gameObject.scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)); }
        private void Bind(Button button, UnityAction action)
        {
            if (button == null) return;
            button.onClick.AddListener(action);
            unbind.Add(() => { if (button != null) button.onClick.RemoveListener(action); });
            界面窗口管理器.注册运行时按钮(button);
        }
        private void BindEntries()
        {
            var transforms = SceneTransforms().ToArray();
            var task = transforms.FirstOrDefault(t => t.name == "主界面_任务图标" && t.parent != null && t.parent.gameObject == mainRoot);
            if (task != null)
            {
                var button = task.GetComponent<Button>() ?? task.gameObject.AddComponent<Button>();
                button.targetGraphic = task.GetComponent<Image>();
                Bind(button, () => pages.Open(JournalPage.Growth));
                sidebar = Window1Style.Text(task, "窗口1_任务摘要", "", pages.Style.Font, 9, Window1Style.Ink, TextAnchor.MiddleLeft);
                Window1Style.Anchors(sidebar.rectTransform, new Vector2(.06f, .08f), new Vector2(.94f, .53f));
            }
            foreach (var t in transforms)
            {
                if (t.name == "打开告示栏") Bind(t.GetComponent<Button>(), () => pages.Open(JournalPage.Notices));
                if (t.name == "打开邮件") Bind(t.GetComponent<Button>(), () => pages.Open(JournalPage.Mail));
            }
            var lord = transforms.FirstOrDefault(t => t.parent == null && t.name == "君主信息界面UI");
            var achievementLayout = lord == null ? null : lord.Find("成就信息布局");
            if (achievementLayout != null)
            {
                // 保留原君主页切换回调、列表模板与翻页组件，只补齐数据和详情动作。
                var achievements = achievementLayout.GetComponent<Window1Achievements>() ?? achievementLayout.gameObject.AddComponent<Window1Achievements>();
                if (achievements.Initialize(pages, lord)) pages.AttachAchievements(achievements);
            }
        }
        private void RefreshSidebar()
        {
            var s = Window1Module.Service;
            if (sidebar == null || s == null) return;
            var goals = s.Goals(GoalKind.Growth);
            var next = goals.FirstOrDefault(g => !g.Claimed);
            sidebar.text = next == null ? "成长已完成\n查看日常任务" : next.Definition.Title + "\n" + (next.Complete ? "奖励可领取" : next.Current.ToString("0") + "/" + next.Definition.Target.ToString("0"));
        }
        private void OnDestroy()
        {
            Window1Module.Changed -= RefreshSidebar;
            foreach (var remove in unbind) remove();
        }
    }

    // 仅复制 RectTransform/Image 的装饰属性，不复制 Button 回调和任何旧 MonoBehaviour。
    public sealed class Window1Style
    {
        public static readonly Color Ink = new Color(.84f, .95f, .87f);
        public static readonly Color Gold = new Color(.98f, .85f, .39f);
        public static readonly Color Muted = new Color(.64f, .84f, .78f);
        public static readonly Color Surface = new Color(.035f, .15f, .14f);
        public Font Font;
        private Sprite buttonSprite, iconSprite;
        private Button closeSource;
        private Transform title, background, information, frame;
        public static Window1Style FromScene(Scene scene)
        {
            var all = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToArray();
            var castle = all.FirstOrDefault(t => t.name == "城池信息界面UI");
            var source = castle == null ? all : castle.GetComponentsInChildren<Transform>(true);
            var result = new Window1Style
            {
                Font = all.Select(t => t.GetComponent<Text>()).Where(t => t != null && t.font != null).Select(t => t.font).FirstOrDefault(f => f.name.Contains("Noto")) ??
                    all.Select(t => t.GetComponent<Text>()).Where(t => t != null && t.font != null).Select(t => t.font).FirstOrDefault(),
                title = source.FirstOrDefault(t => t.name == "标题栏背景"),
                background = source.FirstOrDefault(t => t.name == "黄色背景图"),
                information = all.FirstOrDefault(t => t.name == "信息背景" && t.root.name == "君主信息界面UI") ?? all.FirstOrDefault(t => t.name == "信息背景"),
                frame = all.FirstOrDefault(t => t.name == "信息边框")
            };
            var button = source.Select(t => t.GetComponent<Button>()).FirstOrDefault(b => b != null && b.name == "返回" && b.GetComponent<Image>() != null);
            result.buttonSprite = button == null ? null : button.GetComponent<Image>().sprite;
            result.closeSource = source.Select(t => t.GetComponent<Button>()).FirstOrDefault(b => b != null && b.name == "关闭" && b.GetComponent<Image>() != null);
            var task = all.FirstOrDefault(t => t.name == "主界面_任务图标");
            result.iconSprite = task == null || task.GetComponent<Image>() == null ? null : task.GetComponent<Image>().sprite;
            return result;
        }
        public static RectTransform Rect(Transform parent, string name)
        { var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false); return (RectTransform)go.transform; }
        public static void Anchors(RectTransform rt, Vector2 min, Vector2 max)
        { rt.anchorMin = min; rt.anchorMax = max; rt.offsetMin = rt.offsetMax = Vector2.zero; }
        public static void Place(RectTransform rt, float x, float y, float width, float height)
        { rt.anchorMin = rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0, 1); rt.anchoredPosition = new Vector2(x, -y); rt.sizeDelta = new Vector2(width, height); }
        public static Image Image(Transform parent, string name, Color color, Sprite sprite = null)
        { var rt = Rect(parent, name); var image = rt.gameObject.AddComponent<Image>(); image.sprite = sprite; image.color = color; image.raycastTarget = false; return image; }
        public static Text Text(Transform parent, string name, string value, Font font, int size, Color color, TextAnchor alignment = TextAnchor.UpperLeft)
        {
            var rt = Rect(parent, name); var text = rt.gameObject.AddComponent<Text>(); text.font = font; text.fontSize = size;
            text.text = value; text.color = color; text.alignment = alignment; text.raycastTarget = false;
            text.supportRichText = false; text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Truncate; return text;
        }
        public Button Button(Transform parent, string name, string label, UnityAction click)
        {
            var image = Image(parent, name, buttonSprite == null ? new Color(.21f, .34f, .28f) : Color.white, buttonSprite);
            image.type = UnityEngine.UI.Image.Type.Sliced; image.raycastTarget = true;
            var button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            var colors = button.colors; colors.highlightedColor = new Color(1, 1, .78f); colors.pressedColor = new Color(.65f, .85f, .73f); colors.disabledColor = new Color(.5f, .55f, .5f); button.colors = colors;
            var text = Text(button.transform, "文字", label, Font, 16, Gold, TextAnchor.MiddleCenter);
            原界面文字样式.按钮(text);
            Anchors(text.rectTransform, new Vector2(.04f, .06f), new Vector2(.96f, .94f));
            if (click != null) button.onClick.AddListener(click);
            return button;
        }
        public Button CloseButton(Transform parent, UnityAction click)
        {
            if (closeSource == null) return Button(parent, "关闭", "×", click);
            var original = closeSource.GetComponent<Image>();
            var image = Image(parent, "关闭", original.color, original.sprite);
            image.type = original.type; image.preserveAspect = original.preserveAspect; image.raycastTarget = true;
            var button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            button.transition = closeSource.transition; button.colors = closeSource.colors; button.spriteState = closeSource.spriteState;
            button.onClick.AddListener(click);
            return button;
        }
        private static void Graphics(Transform source, RectTransform target, bool isTitle)
        {
            var root = source as RectTransform;
            if (root == null || root.rect.width <= 0 || root.rect.height <= 0) return;
            // 原信息背景用 Mask 裁剪大于容器的纹理，复制装饰时必须保留可视范围。
            var mask = source.GetComponent<Mask>();
            if (mask != null && mask.enabled && target.GetComponent<RectMask2D>() == null) target.gameObject.AddComponent<RectMask2D>();
            var corners = new Vector3[4];
            foreach (var graphic in source.GetComponentsInChildren<Image>(true))
            {
                if (graphic.GetComponentInParent<Button>(true) != null || graphic.GetComponent<Text>() != null) continue;
                var graphicMask = graphic.GetComponent<Mask>();
                if (graphicMask != null && graphicMask.enabled && !graphicMask.showMaskGraphic) continue;
                var rt = graphic.rectTransform;
                rt.GetLocalCorners(corners);
                // 部分旧 UI 的 z 缩放为 0；只在二维平面换算，避免逆奇异矩阵。
                var rotation = Quaternion.Inverse(root.rotation); var scale = root.lossyScale;
                Vector3 a = rotation * (rt.TransformPoint(corners[0]) - root.position);
                Vector3 b = rotation * (rt.TransformPoint(corners[2]) - root.position);
                a.x /= scale.x; a.y /= scale.y; b.x /= scale.x; b.y /= scale.y;
                // 场景标题文字是中心的小 Sprite，移除后用真正 Text 显示当前标题。
                if (isTitle && Mathf.Abs((a.x + b.x) / 2) < 90 && b.x - a.x < 170 && b.y - a.y < 35) continue;
                var image = Image(target, "装饰_" + graphic.name, graphic.color, graphic.sprite);
                image.type = graphic.type; image.preserveAspect = false;
                Anchors(image.rectTransform, new Vector2(Mathf.Min(a.x, b.x) / root.rect.width + root.pivot.x, Mathf.Min(a.y, b.y) / root.rect.height + root.pivot.y),
                    new Vector2(Mathf.Max(a.x, b.x) / root.rect.width + root.pivot.x, Mathf.Max(a.y, b.y) / root.rect.height + root.pivot.y));
                image.rectTransform.localScale = new Vector3(a.x > b.x ? -1 : 1, a.y > b.y ? -1 : 1, 1);
                image.rectTransform.localRotation = Quaternion.identity;
            }
        }
        public void Frame(RectTransform panel)
        {
            if (background != null)
            {
                var paper = Rect(panel, "黄色外框"); Place(paper, 0, 40, 800, 450);
                paper.gameObject.AddComponent<RectMask2D>();
                Graphics(background, paper, false);
            }
            var info = Rect(panel, "青绿信息底"); Place(info, 12, 92, 776, 330);
            var bg = Image(info, "信息底色", Surface); Anchors(bg.rectTransform, Vector2.zero, Vector2.one);
            if (information != null) Graphics(information, info, false);
            if (frame != null) Graphics(frame, info, false);
            var head = Rect(panel, "标题栏背景"); Place(head, 0, 0, 800, 46);
            if (title != null) Graphics(title, head, true);
            else { var bgTitle = Image(head, "标题底色", new Color(.1f, .34f, .29f)); Anchors(bgTitle.rectTransform, Vector2.zero, Vector2.one); }
        }
        public Image GoalIcon(Transform parent)
        { return Image(parent, "卷轴图标", Color.white, iconSprite); }
    }

    public sealed class Window1Pages : MonoBehaviour
    {
        private const int PageSize = 6;
        private static readonly Color FeedbackInk = new Color(.035f, .12f, .105f);
        public Window1Style Style { get; private set; }
        private JournalPage currentPage;
        private int pageIndex;
        private string selectedId, deletePendingId, signature;
        private bool ready, unreadOnly, achievementDetail;
        private Coroutine refreshLoop;
        private Text title, subtitle, pageLabel, detailTitle, detailMeta, detailBody, rewards, feedback, empty;
        private ScrollRect listScroll, detailScroll, rewardScroll;
        private Image progressFill, attachmentIcon, divider, pageBackground;
        private Button previous, next, claim, delete;
        private Toggle unreadToggle;
        private Window1Achievements achievements;
        private static readonly JournalPage[] TabPages = { JournalPage.Growth, JournalPage.Daily, JournalPage.Notices, JournalPage.Mail };
        private readonly List<Button> tabs = new List<Button>();
        private readonly List<Row> rows = new List<Row>();
        private List<GoalView> goalViews = new List<GoalView>();
        private List<LocalMail> mailViews = new List<LocalMail>();
        private List<LocalNotice> noticeViews = new List<LocalNotice>();
        private sealed class Row { public Button Button; public Text Title, Meta, Status; public Image Fill, Border; public string Id; }

        public static Window1Pages Build(Window1Style style)
        {
            var root = new GameObject("窗口1_任务成就告示邮件", typeof(RectTransform)); root.SetActive(false);
            var canvas = root.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 5;
            var scaler = root.AddComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(960, 540); scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            root.AddComponent<GraphicRaycaster>();
            var view = root.AddComponent<Window1Pages>(); view.Style = style; view.Construct(); view.ready = true;
            return view;
        }
        private Text Label(Transform p, string name, string value, int size, Color color, float x, float y, float w, float h, TextAnchor align = TextAnchor.UpperLeft)
        { var t = Window1Style.Text(p, name, value, Style.Font, size, color, align); Window1Style.Place(t.rectTransform, x, y, w, h); return t; }
        private Button ActionButton(Transform p, string name, string value, UnityAction action, float x, float y, float w, float h)
        { var b = Style.Button(p, name, value, action); Window1Style.Place(b.GetComponent<RectTransform>(), x, y, w, h); return b; }
        private static void Grid(RectTransform content, Vector2 cellSize, Vector2 spacing, int columns, bool fitHeight = false)
        {
            var grid = content.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = cellSize; grid.spacing = spacing;
            grid.childAlignment = TextAnchor.UpperLeft; grid.startCorner = GridLayoutGroup.Corner.UpperLeft; grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; grid.constraintCount = columns;
            if (fitHeight)
            {
                var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
                fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained; fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }
        }
        private ScrollRect Scroll(Transform parent, string name, float x, float y, float width, float height)
        {
            var rt = Window1Style.Rect(parent, name); Window1Style.Place(rt, x, y, width, height);
            var sr = rt.gameObject.AddComponent<ScrollRect>(); sr.horizontal = false; sr.movementType = ScrollRect.MovementType.Clamped; sr.inertia = false;
            var viewport = Window1Style.Rect(rt, "可视区域"); Window1Style.Anchors(viewport, Vector2.zero, Vector2.one); viewport.gameObject.AddComponent<RectMask2D>();
            var hit = viewport.gameObject.AddComponent<Image>(); hit.color = Color.clear; hit.raycastTarget = true;
            var content = Window1Style.Rect(viewport, "内容"); content.anchorMin = new Vector2(0, 1); content.anchorMax = Vector2.one;
            content.pivot = new Vector2(0, 1); content.anchoredPosition = Vector2.zero; content.sizeDelta = new Vector2(0, height);
            sr.viewport = viewport; sr.content = content; return sr;
        }
        private void Construct()
        {
            var backdrop = Window1Style.Image(transform, "背景遮罩", new Color(0, 0, 0, .5f)); Window1Style.Anchors(backdrop.rectTransform, Vector2.zero, Vector2.one); backdrop.raycastTarget = true;
            var panel = Window1Style.Rect(transform, "内容面板"); panel.anchorMin = panel.anchorMax = new Vector2(.5f, .5f); panel.sizeDelta = new Vector2(800, 490);
            var panelImage = panel.gameObject.AddComponent<Image>(); panelImage.color = Color.clear; panelImage.raycastTarget = true;
            Style.Frame(panel);
            title = Label(panel, "标题", "政务", 20, Window1Style.Gold, 180, 8, 440, 35, TextAnchor.MiddleCenter);
            原界面文字样式.标题(title);
            var close = Style.CloseButton(panel, () => gameObject.SetActive(false)); Window1Style.Place(close.GetComponent<RectTransform>(), 758, 3, 38, 38);
            var tabLayout = Window1Style.Rect(panel, "页签布局"); Window1Style.Place(tabLayout, 20, 51, 760, 35);
            Grid(tabLayout, new Vector2(184, 35), new Vector2(8, 0), 4);
            string[] labels = { "成长任务", "日常任务", "告示栏", "邮件" };
            for (int i = 0; i < labels.Length; i++)
            {
                var which = TabPages[i];
                tabs.Add(Style.Button(tabLayout, "页签_" + labels[i], labels[i], () => Open(which)));
            }
            subtitle = Label(panel, "列表说明", "", 13, Window1Style.Muted, 26, 99, 745, 22);
            listScroll = Scroll(panel, "条目列表", 26, 128, 321, 279);
            Grid(listScroll.content, new Vector2(319, 43), new Vector2(0, 3), 1, true);
            for (int i = 0; i < PageSize; i++)
            {
                var row = new Row();
                var border = Window1Style.Image(listScroll.content, "条目" + i, new Color(.45f, .48f, .27f)); row.Border = border;
                var inner = Window1Style.Image(border.transform, "底色", new Color(.055f, .22f, .19f)); Window1Style.Anchors(inner.rectTransform, Vector2.zero, Vector2.one); inner.rectTransform.offsetMin = Vector2.one; inner.rectTransform.offsetMax = -Vector2.one; inner.raycastTarget = true;
                row.Button = border.gameObject.AddComponent<Button>(); row.Button.targetGraphic = inner;
                // Noto CJK 在 1600/960 等非整数画布缩放下行高超过 21；保留字形余量，底边仍在进度 y=26 之前。
                row.Title = Label(border.transform, "名称", "", 15, Window1Style.Gold, 9, 1, 231, 24);
                row.Meta = Label(border.transform, "进度", "", 11, Window1Style.Ink, 9, 26, 230, 16);
                row.Status = Label(border.transform, "状态", "", 12, Window1Style.Ink, 248, 7, 62, 29, TextAnchor.MiddleCenter);
                row.Fill = Window1Style.Image(border.transform, "进度条", new Color(.34f, .72f, .56f)); Window1Style.Place(row.Fill.rectTransform, 9, 41, 300, 2);
                row.Button.onClick.AddListener(() => Select(row.Id)); rows.Add(row);
            }
            // 空态留在视口中，不参与条目网格或撑高滚动内容。
            empty = Label(listScroll.viewport, "空态", "", 16, Window1Style.Ink, 18, 45, 280, 168, TextAnchor.MiddleCenter);
            divider = Window1Style.Image(panel, "分隔线", new Color(.57f, .5f, .24f)); Window1Style.Place(divider.rectTransform, 357, 126, 1, 282);
            detailTitle = Label(panel, "详情名称", "选择一项查看详情", 20, Window1Style.Gold, 372, 129, 399, 30);
            detailMeta = Label(panel, "详情状态", "", 13, Window1Style.Muted, 372, 164, 399, 24);
            var progressBg = Window1Style.Image(panel, "详情进度底", new Color(.025f, .09f, .08f)); Window1Style.Place(progressBg.rectTransform, 372, 194, 398, 5);
            progressFill = Window1Style.Image(progressBg.transform, "详情进度条", new Color(.37f, .75f, .56f)); Window1Style.Anchors(progressFill.rectTransform, Vector2.zero, Vector2.one);
            detailScroll = Scroll(panel, "详情正文", 372, 212, 399, 116);
            detailBody = Label(detailScroll.content, "正文", "", 16, Window1Style.Ink, 0, 0, 393, 116); detailBody.lineSpacing = 1.15f;
            attachmentIcon = Window1Style.Image(panel, "附件头像", Color.white); Window1Style.Place(attachmentIcon.rectTransform, 372, 348, 34, 34); attachmentIcon.preserveAspect = true;
            rewardScroll = Scroll(panel, "奖励附件", 412, 334, 359, 73);
            rewards = Label(rewardScroll.content, "奖励预览", "", 14, Window1Style.Gold, 0, 0, 353, 73);
            unreadToggle = MakeToggle(panel);
            previous = ActionButton(panel, "上一页", "上一页", () => ChangePage(-1), 26, 428, 95, 34);
            pageBackground = Window1Style.Image(panel, "页码底", Window1Style.Surface); Window1Style.Place(pageBackground.rectTransform, 124, 428, 117, 34);
            pageLabel = Label(panel, "页码", "", 14, Window1Style.Gold, 124, 428, 117, 34, TextAnchor.MiddleCenter);
            next = ActionButton(panel, "下一页", "下一页", () => ChangePage(1), 244, 428, 95, 34);
            claim = ActionButton(panel, "领取", "领取奖励", Claim, 374, 428, 182, 34);
            delete = ActionButton(panel, "删除", "删除邮件", Delete, 568, 428, 203, 34);
            feedback = Label(panel, "操作结果", "", 12, FeedbackInk, 24, 467, 752, 20, TextAnchor.MiddleCenter);
        }
        private Toggle MakeToggle(Transform panel)
        {
            var rt = Window1Style.Rect(panel, "只看未读"); Window1Style.Place(rt, 636, 99, 135, 23);
            var toggle = rt.gameObject.AddComponent<Toggle>();
            var bg = Window1Style.Image(rt, "框", new Color(.28f, .45f, .37f)); Window1Style.Place(bg.rectTransform, 0, 2, 18, 18); bg.raycastTarget = true;
            var check = Window1Style.Image(bg.transform, "勾选", Window1Style.Gold); Window1Style.Anchors(check.rectTransform, Vector2.zero, Vector2.one); check.rectTransform.offsetMin = new Vector2(4, 4); check.rectTransform.offsetMax = new Vector2(-4, -4);
            toggle.targetGraphic = bg; toggle.graphic = check;
            Label(rt, "文字", "只看未读", 13, Window1Style.Ink, 25, 0, 110, 23, TextAnchor.MiddleLeft);
            toggle.onValueChanged.AddListener(on =>
            {
                if (currentPage != JournalPage.Mail || achievementDetail) return;
                unreadOnly = on; pageIndex = 0; selectedId = null; Refresh(true);
            });
            return toggle;
        }
        public void AttachAchievements(Window1Achievements existingList) { achievements = existingList; }
        public void Open(JournalPage page)
        {
            if (page == JournalPage.Achievement)
            { if (achievements != null) achievements.Open(); return; }
            achievementDetail = false; ApplyLayout();
            currentPage = page; pageIndex = 0; selectedId = null; deletePendingId = null; feedback.text = ""; feedback.color = FeedbackInk;
            if (!gameObject.activeSelf) gameObject.SetActive(true);
            Refresh(true);
        }
        public void OpenAchievementStep(int step)
        { if (achievements != null) { achievements.Open(); achievements.ChangePage(step); } }
        public void OpenAchievementDetail(string id)
        {
            var definition = GoalCatalog.Find(id);
            if (definition == null || definition.Kind != GoalKind.Achievement) return;
            achievementDetail = true; ApplyLayout();
            currentPage = JournalPage.Achievement; selectedId = id; deletePendingId = null; feedback.text = ""; feedback.color = FeedbackInk;
            if (!gameObject.activeSelf) gameObject.SetActive(true);
            Refresh(true);
        }
        private void ApplyLayout()
        {
            bool showList = !achievementDetail;
            foreach (var tab in tabs) tab.gameObject.SetActive(showList);
            subtitle.gameObject.SetActive(showList); listScroll.gameObject.SetActive(showList); divider.gameObject.SetActive(showList);
            previous.gameObject.SetActive(showList); next.gameObject.SetActive(showList);
            pageBackground.gameObject.SetActive(showList); pageLabel.gameObject.SetActive(showList);
            unreadToggle.gameObject.SetActive(false);
            Window1Style.Place(detailTitle.rectTransform, achievementDetail ? 40 : 372, achievementDetail ? 110 : 129, achievementDetail ? 720 : 399, achievementDetail ? 32 : 30);
            Window1Style.Place(detailMeta.rectTransform, achievementDetail ? 40 : 372, achievementDetail ? 147 : 164, achievementDetail ? 720 : 399, achievementDetail ? 26 : 24);
            Window1Style.Place((RectTransform)progressFill.transform.parent, achievementDetail ? 40 : 372, achievementDetail ? 182 : 194, achievementDetail ? 720 : 398, 5);
            Window1Style.Place(detailScroll.GetComponent<RectTransform>(), achievementDetail ? 40 : 372, achievementDetail ? 204 : 212, achievementDetail ? 720 : 399, achievementDetail ? 135 : 116);
            Window1Style.Place(attachmentIcon.rectTransform, achievementDetail ? 40 : 372, achievementDetail ? 353 : 348, 34, 34);
            Window1Style.Place(rewardScroll.GetComponent<RectTransform>(), achievementDetail ? 84 : 412, achievementDetail ? 345 : 334, achievementDetail ? 676 : 359, achievementDetail ? 62 : 73);
            detailBody.rectTransform.sizeDelta = new Vector2(achievementDetail ? 714 : 393, achievementDetail ? 135 : 116);
            rewards.rectTransform.sizeDelta = new Vector2(achievementDetail ? 670 : 353, achievementDetail ? 62 : 73);
            Window1Style.Place(claim.GetComponent<RectTransform>(), achievementDetail ? 294 : 374, 428, achievementDetail ? 212 : 182, 34);
        }
        private void OnEnable()
        { if (!ready) return; Window1Module.Changed += OnChanged; refreshLoop = StartCoroutine(VisibleRefresh()); }
        private void OnDisable()
        { Window1Module.Changed -= OnChanged; if (refreshLoop != null) StopCoroutine(refreshLoop); refreshLoop = null; deletePendingId = null; }
        private void OnChanged() { if (isActiveAndEnabled) Refresh(false); }
        private IEnumerator VisibleRefresh()
        { while (isActiveAndEnabled) { Refresh(false); yield return new WaitForSecondsRealtime(2); } }
        private void ChangePage(int step)
        { pageIndex += step; selectedId = null; deletePendingId = null; feedback.text = ""; Refresh(true); }
        private void Select(string id)
        {
            selectedId = id; deletePendingId = null; feedback.text = "";
            if (currentPage == JournalPage.Mail && Window1Module.Service != null) Window1Module.Service.ReadMail(id);
            if (currentPage == JournalPage.Notices && Window1Module.Service != null) Window1Module.Service.ReadNotice(id);
            Refresh(true);
        }
        private int Count { get { return currentPage == JournalPage.Mail ? mailViews.Count : currentPage == JournalPage.Notices ? noticeViews.Count : goalViews.Count; } }
        private void Refresh(bool force)
        {
            if (!ready || !gameObject.activeInHierarchy) return;
            var s = Window1Module.Service;
            string error = null;
            if (s == null || !s.Refresh(out error))
            { feedback.text = s == null ? "当前世界尚未就绪" : error; claim.interactable = false; delete.interactable = false; return; }
            if (currentPage == JournalPage.Mail)
            {
                // 正在阅读的信件暂留在未读筛选中，方便领取后再切走；刷新入口仍严格过滤已读。
                mailViews = s.Mails().Where(m => !unreadOnly || !m.Read || m.Id == selectedId).ToList();
            }
            else if (currentPage == JournalPage.Notices) noticeViews = Window1Module.告示列表();
            else goalViews = s.Goals((GoalKind)currentPage);
            string nextSignature = currentPage + ":" + pageIndex + ":" + selectedId + ":" +
                (currentPage == JournalPage.Mail ? string.Join("|", mailViews.Select(m => m.Id + m.Read + m.Claimed).ToArray()) :
                currentPage == JournalPage.Notices ? string.Join("|", noticeViews.Select(n => n.Id + n.Body + s.NoticeRead(n.Id)).ToArray()) :
                string.Join("|", goalViews.Select(g => g.Definition.Id + g.Current + g.Claimed).ToArray()));
            if (!force && signature == nextSignature) return;
            signature = nextSignature;
            if (achievementDetail)
            {
                title.text = "成就详情";
                RenderDetail(s, force);
                return;
            }
            string[] names = { "成长任务", "日常任务", "君主成就", "封地告示栏", "驿站邮件" };
            title.text = names[(int)currentPage];
            for (int i = 0; i < tabs.Count; i++) tabs[i].GetComponent<Image>().color = TabPages[i] == currentPage ? new Color(.77f, 1, .8f) : Color.white;
            unreadToggle.gameObject.SetActive(currentPage == JournalPage.Mail);
            subtitle.rectTransform.sizeDelta = new Vector2(currentPage == JournalPage.Mail ? 590 : 745, 22);
            subtitle.text = currentPage == JournalPage.Mail ? "本地收件箱 · 未连接网络邮件服务" : currentPage == JournalPage.Notices ? "本地告示 · 当前封地与玩法规则" :
                currentPage == JournalPage.Daily ? "本地规则 · " + s.DailyDate + " · 今日基线后的净增长" : "本地规则 · 达成记录保留 · 奖励仅可领取一次";
            pageIndex = JournalService.ClampPage(pageIndex, Count, PageSize);
            int first = pageIndex * PageSize;
            var ids = currentPage == JournalPage.Mail ? mailViews.Select(m => m.Id).ToList() : currentPage == JournalPage.Notices ? noticeViews.Select(n => n.Id).ToList() : goalViews.Select(g => g.Definition.Id).ToList();
            string previousSelection = selectedId;
            if (!ids.Contains(selectedId)) selectedId = ids.Skip(first).FirstOrDefault();
            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i]; int index = first + i; row.Button.gameObject.SetActive(index < Count);
                if (index >= Count) continue;
                row.Id = ids[index]; row.Border.color = row.Id == selectedId ? Window1Style.Gold : new Color(.38f, .46f, .3f);
                float ratio = 0;
                if (currentPage == JournalPage.Mail)
                {
                    var m = mailViews[index]; row.Title.text = ShortTitle(m.Title, 14); row.Meta.text = ShortTitle(m.Sender, 12) + " · " + DateLabel(m.SentUtcTicks); row.Status.text = m.Read ? "已读" : "未读";
                }
                else if (currentPage == JournalPage.Notices)
                { var n = noticeViews[index]; row.Title.text = ShortTitle(n.Title, 14); row.Meta.text = n.Pinned ? "置顶 · 本地说明" : "本地告示"; row.Status.text = s.NoticeRead(n.Id) ? "已读" : "未读"; }
                else
                { var g = goalViews[index]; row.Title.text = g.Definition.Title; row.Meta.text = "进度 " + g.Current.ToString("0") + " / " + g.Definition.Target.ToString("0"); row.Status.text = g.Status; ratio = (float)(g.Current / g.Definition.Target); }
                row.Fill.gameObject.SetActive(currentPage <= JournalPage.Achievement); row.Fill.rectTransform.sizeDelta = new Vector2(300 * Mathf.Clamp01(ratio), 2);
                row.Status.color = row.Status.text == "可领取" || row.Status.text == "未读" ? Window1Style.Gold : Window1Style.Muted;
            }
            if (force) listScroll.verticalNormalizedPosition = 1;
            empty.gameObject.SetActive(Count == 0);
            empty.text = currentPage == JournalPage.Mail ? (unreadOnly ? "没有未读邮件\n取消筛选可查看已读信件。" : "尚未收到邮件\n本地世界未连接网络邮件服务。\n实际本地通知投递后会显示在这里。") : "当前列表为空";
            previous.interactable = pageIndex > 0; next.interactable = pageIndex + 1 < JournalService.PageCount(Count, PageSize);
            pageLabel.text = (pageIndex + 1) + " / " + JournalService.PageCount(Count, PageSize);
            RenderDetail(s, force || previousSelection != selectedId);
        }
        private static string DateLabel(long ticks)
        { return ticks <= 0 ? "时间未提供" : new DateTime(ticks, DateTimeKind.Utc).ToLocalTime().ToString("MM-dd HH:mm", CultureInfo.InvariantCulture); }
        private static string ShortTitle(string value, int length)
        {
            var indices = StringInfo.ParseCombiningCharacters(value);
            return indices.Length <= length ? value : value.Substring(0, indices[length - 1]) + "…";
        }
        private void RenderDetail(JournalService service, bool resetScroll)
        {
            claim.gameObject.SetActive(currentPage != JournalPage.Notices); delete.gameObject.SetActive(currentPage == JournalPage.Mail);
            attachmentIcon.gameObject.SetActive(false); progressFill.transform.parent.gameObject.SetActive(currentPage <= JournalPage.Achievement);
            rewardScroll.gameObject.SetActive(currentPage != JournalPage.Notices);
            detailScroll.GetComponent<RectTransform>().sizeDelta = achievementDetail ? new Vector2(720, 135) : new Vector2(399, currentPage == JournalPage.Notices || currentPage == JournalPage.Mail ? 195 : 116);
            if (selectedId == null || (achievementDetail && !goalViews.Any(g => g.Definition.Id == selectedId)))
            {
                detailTitle.text = currentPage == JournalPage.Mail ? "收件箱为空" : "选择一项查看详情";
                detailMeta.text = ""; detailBody.text = currentPage == JournalPage.Mail ? "这里记录实际收到的本地信件。附件先校验后领取，未领取附件的邮件不能删除。" : ""; rewards.text = "";
                claim.interactable = false; delete.interactable = false; SetBodyHeight(resetScroll); return;
            }
            if (currentPage == JournalPage.Mail)
            {
                service.ReadMail(selectedId);
                var m = mailViews.First(x => x.Id == selectedId);
                detailTitle.text = ShortTitle(m.Title, 19); detailMeta.text = "本地来信 · " + ShortTitle(m.Sender, 10) + " · " + DateLabel(m.SentUtcTicks);
                detailBody.text = "发信者：" + m.Sender + "\n主题：" + m.Title + "\n\n" + m.Body;
                rewards.text = "附件：" + m.Attachment.Preview();
                claim.GetComponentInChildren<Text>().text = m.Claimed ? "附件已领取" : "领取附件";
                claim.interactable = m.Attachment.HasAnything && !m.Claimed;
                delete.interactable = !m.Attachment.HasAnything || m.Claimed;
                delete.GetComponentInChildren<Text>().text = deletePendingId == selectedId ? "确认删除" : "删除邮件";
                // 邮件正文预留附件行，不与可滚动正文重叠。
                detailScroll.GetComponent<RectTransform>().sizeDelta = new Vector2(399, 116);
                SetAttachment(m.Attachment);
            }
            else if (currentPage == JournalPage.Notices)
            {
                service.ReadNotice(selectedId);
                var n = noticeViews.First(x => x.Id == selectedId); detailTitle.text = ShortTitle(n.Title, 19);
                detailMeta.text = n.Pinned ? "置顶 · 本地说明" : "本地告示 · " + DateLabel(n.PublishedUtcTicks);
                detailBody.text = n.Title + "\n\n" + n.Body; rewards.text = "";
            }
            else
            {
                var g = goalViews.First(x => x.Definition.Id == selectedId);
                detailTitle.text = g.Definition.Title; detailMeta.text = g.Status + " · 进度 " + g.Current.ToString("0") + "/" + g.Definition.Target.ToString("0");
                progressFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01((float)(g.Current / g.Definition.Target)), 1);
                detailBody.text = g.Definition.Description + "\n\n" + (g.Definition.Kind == GoalKind.Daily ? "当地日期每日刷新；进度为当天基线后的净增加。" : "条件达成后保留记录，领取奖励不会扣除进度。") + "\n精确条件与奖励为本地规则。";
                rewards.text = "奖励预览\n" + g.Definition.Reward.Preview();
                claim.GetComponentInChildren<Text>().text = g.Claimed ? "奖励已领取" : g.Complete ? "领取奖励" : "条件未达成";
                claim.interactable = g.Complete && !g.Claimed; SetAttachment(g.Definition.Reward);
            }
            SetBodyHeight(resetScroll);
        }
        private void SetAttachment(Reward reward)
        {
            if (reward.Items.Count == 0) return;
            var data = 全局道具库.获取指定名字的道具(reward.Items[0].Name);
            if (data == null || 全局变量.所有道具头像资源表 == null) return;
            attachmentIcon.sprite = 全局道具库.获取道具头像(data.头像); attachmentIcon.gameObject.SetActive(attachmentIcon.sprite != null);
        }
        private void SetBodyHeight(bool resetScroll)
        {
            float bodyPosition = detailScroll.verticalNormalizedPosition, rewardPosition = rewardScroll.verticalNormalizedPosition;
            float height = Mathf.Max(detailScroll.viewport.rect.height, detailBody.preferredHeight + 8);
            detailBody.rectTransform.sizeDelta = new Vector2(achievementDetail ? 714 : 393, height); detailScroll.content.sizeDelta = new Vector2(0, height); detailScroll.verticalNormalizedPosition = resetScroll ? 1 : Mathf.Clamp01(bodyPosition);
            float rewardHeight = Mathf.Max(achievementDetail ? 62 : 73, rewards.preferredHeight + 8);
            rewards.rectTransform.sizeDelta = new Vector2(achievementDetail ? 670 : 353, rewardHeight); rewardScroll.content.sizeDelta = new Vector2(0, rewardHeight); rewardScroll.verticalNormalizedPosition = resetScroll ? 1 : Mathf.Clamp01(rewardPosition);
        }
        private void Claim()
        {
            var s = Window1Module.Service; if (s == null || selectedId == null) return;
            string error; bool ok = currentPage == JournalPage.Mail ? s.ClaimMail(selectedId, out error) : s.ClaimGoal(selectedId, out error);
            feedback.text = ok ? "奖励已存入当前君主资源与背包。" : error; feedback.color = ok ? FeedbackInk : new Color(.36f, .035f, .02f);
            deletePendingId = null; Refresh(true); if (ok) Window1Module.Signal();
        }
        private void Delete()
        {
            if (selectedId == null || Window1Module.Service == null) return;
            feedback.color = FeedbackInk;
            if (deletePendingId != selectedId) { deletePendingId = selectedId; feedback.text = "再次点击“确认删除”移除此信；已领取记录仍会保存。"; Refresh(true); return; }
            string error; bool ok = Window1Module.Service.DeleteMail(selectedId, out error);
            feedback.text = ok ? "信件已删除。" : error; if (ok) selectedId = null; deletePendingId = null; Refresh(true); if (ok) Window1Module.Signal();
        }
    }
}
