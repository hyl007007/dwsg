using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Dwsg.Social
{
    // 从原城池面板复制矩形图像和遮罩，不克隆逻辑组件、旧标题字图或按钮事件。
    internal sealed class SocialUi
    {
        internal static readonly Color Ink = new Color(1f, .95f, .69f);
        internal static readonly Color Cyan = new Color(.32f, .91f, .81f);
        internal static readonly Color Muted = new Color(.77f, .86f, .77f);
        internal static readonly Color Gold = new Color(.63f, .53f, .30f);
        internal static readonly Color Green = new Color(.035f, .14f, .12f);
        internal static readonly Color TitleGold = new Color(1f, 1f, .65f);
        internal static readonly Color ButtonGold = new Color(.98f, .84f, .35f);
        internal static readonly Color PaperInk = new Color(.06f, .18f, .13f);
        private readonly Font font;
        private readonly RectTransform yellow, header, information, informationBorder;
        private readonly Transform titleGraphic;
        private readonly Image buttonImage, closeImage;
        private readonly Button buttonStyle, closeStyle;
        private readonly Toggle tabTemplate;
        private readonly InputField inputTemplate;
        private readonly RectTransform inputFrame;
        private readonly Text inputTextStyle;
        private static readonly Dictionary<Sprite, Sprite> inputSlices = new Dictionary<Sprite, Sprite>();
        internal readonly Sprite Avatar;
        internal SocialUi(Scene scene)
        {
            Transform city = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name == "城池信息界面UI") city = root.transform;
                if (root.name == "商城信息界面UI")
                {
                    var tab = root.transform.Find("道具切换布局/宝箱");
                    if (tab != null) tabTemplate = tab.GetComponent<Toggle>();
                }
                if (root.name == "建国界面UI")
                {
                    var field = root.transform.Find("国家信息布局列表/国名布局");
                    if (field != null)
                    {
                        inputTemplate = field.Find("InputField").GetComponent<InputField>();
                        inputFrame = field.Find("输入框布局") as RectTransform;
                        inputTextStyle = field.Find("文本显示").GetComponent<Text>();
                    }
                }
                if (root.name == "主界面UI")
                {
                    var portrait = root.transform.Find("主界面_信息显示布局/主界面_头像显示");
                    var image = portrait == null ? null : portrait.GetComponent<Image>();
                    if (image != null) Avatar = image.sprite;
                }
            }
            if (city != null)
            {
                yellow = city.Find("黄色背景图") as RectTransform;
                header = city.Find("标题栏背景") as RectTransform;
                titleGraphic = city.Find("标题栏背景/8 (21)");
                const string ownCity = "敌我城池背景布局/我方城池背景/";
                information = city.Find(ownCity + "通用界面背景图") as RectTransform;
                informationBorder = city.Find(ownCity + "通用界面边框") as RectTransform;
                var action = city.Find("城池信息布局/查看按钮");
                var close = city.Find("标题栏背景/关闭");
                buttonImage = action == null ? null : action.GetComponent<Image>();
                buttonStyle = action == null ? null : action.GetComponent<Button>();
                closeImage = close == null ? null : close.GetComponent<Image>();
                closeStyle = close == null ? null : close.GetComponent<Button>();
                var text = city.GetComponentInChildren<Text>(true);
                if (text != null) font = text.font;
            }
            if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (yellow == null || header == null || information == null || informationBorder == null || closeImage == null || buttonImage == null)
                Debug.LogWarning("社交面板缺少原城池视觉模板");
        }

        internal void Shell(RectTransform panel, string title, Action closed, out Text titleText)
        {
            Image(panel, "面板交互底", 0, 0, 700, 476, null, Color.clear, true);
            FitDecoration(yellow, panel, "黄色外框", 0, 32, 700, 444);
            var strip = Node(panel, "标题栏", 0, 0, 700, 43);
            Rect bounds = VisualBounds(header, titleGraphic, closeImage == null ? null : closeImage.transform);
            FitDecoration(header, strip, "原标题拼片", 0, 0, 700, 43, titleGraphic, closeImage == null ? null : closeImage.transform);
            titleText = Text(strip, title, 200, 6, 300, 34, 23, TitleGold, TextAnchor.MiddleCenter);
            原界面文字样式.标题(titleText);
            if (closeImage != null)
            {
                var source = closeImage.rectTransform;
                float sx = 700 / bounds.width, sy = 43 / bounds.height;
                var position = source.anchoredPosition;
                var size = source.rect.size;
                var image = Image(strip, "关闭", (position.x - size.x * source.pivot.x - bounds.xMin) * sx,
                    (bounds.yMax - position.y - size.y * (1 - source.pivot.y)) * sy, size.x * sx, size.y * sy, null, Color.white, true);
                CopyImage(closeImage, image);
                image.raycastTarget = true;
                var close = image.gameObject.AddComponent<Button>(); close.targetGraphic = image;
                CopyButtonStyle(closeStyle, close);
                if (closed != null) close.onClick.AddListener(() => closed());
                界面窗口管理器.注册运行时按钮(close);
            }
            else Button(strip, "关闭", 650, 3, 44, 37, closed);

            if (information != null)
            {
                var background = CopyVisuals(information, panel);
                Place(background, 20, 80, 660, 348);
            }
            else Image(panel, "信息背景", 20, 80, 660, 348, null, Green);
            if (informationBorder != null)
            {
                var border = CopyVisuals(informationBorder, panel);
                Place(border, 20, 80, 660, 348);
            }
        }

        private void FitDecoration(RectTransform source, Transform parent, string name, float x, float y, float w, float h, params Transform[] skipped)
        {
            if (source == null) return;
            Rect bounds = VisualBounds(source, skipped);
            var holder = Node(parent, name, x, y, w, h);
            var copy = CopyVisuals(source, holder, skipped);
            copy.anchorMin = copy.anchorMax = new Vector2(0, 1);
            copy.localScale = new Vector3(w / bounds.width, h / bounds.height, 1);
            copy.anchoredPosition = new Vector2(-bounds.xMin * copy.localScale.x, -bounds.yMax * copy.localScale.y);
        }

        private static Rect VisualBounds(RectTransform source, params Transform[] skipped)
        {
            if (source == null) return new Rect(0, 0, 700, 43);
            Vector2 min = new Vector2(float.MaxValue, float.MaxValue), max = new Vector2(float.MinValue, float.MinValue);
            var corners = new Vector3[4];
            foreach (var image in source.GetComponentsInChildren<Image>(true))
            {
                if (image.sprite == null || Array.IndexOf(skipped, image.transform) >= 0) continue;
                image.rectTransform.GetWorldCorners(corners);
                foreach (var corner in corners)
                {
                    Vector2 point = source.InverseTransformPoint(corner);
                    min = Vector2.Min(min, point); max = Vector2.Max(max, point);
                }
            }
            return min.x == float.MaxValue ? source.rect : Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        private RectTransform CopyVisuals(RectTransform source, Transform parent, params Transform[] skipped)
        {
            var copy = Node(parent, source.name, 0, 0, 0, 0);
            copy.anchorMin = source.anchorMin; copy.anchorMax = source.anchorMax; copy.pivot = source.pivot;
            copy.sizeDelta = source.sizeDelta; copy.anchoredPosition = source.anchoredPosition;
            copy.localRotation = source.localRotation;
            copy.localScale = new Vector3(source.localScale.x, source.localScale.y, 1);
            var image = source.GetComponent<Image>();
            if (image != null) CopyImage(image, copy.gameObject.AddComponent<Image>());
            var mask = source.GetComponent<Mask>();
            if (mask != null) copy.gameObject.AddComponent<Mask>().showMaskGraphic = mask.showMaskGraphic;
            for (int i = 0; i < source.childCount; i++)
            {
                var child = source.GetChild(i) as RectTransform;
                if (child != null && Array.IndexOf(skipped, child) < 0) CopyVisuals(child, copy, skipped);
            }
            return copy;
        }

        private static void CopyImage(Image source, Image target)
        {
            target.sprite = source.sprite; target.material = source.material; target.color = source.color;
            target.type = source.type; target.preserveAspect = source.preserveAspect; target.fillCenter = source.fillCenter;
            target.fillMethod = source.fillMethod; target.fillOrigin = source.fillOrigin; target.fillAmount = source.fillAmount;
            target.fillClockwise = source.fillClockwise; target.useSpriteMesh = source.useSpriteMesh;
            target.pixelsPerUnitMultiplier = source.pixelsPerUnitMultiplier; target.raycastTarget = false;
        }

        // 与原建国 8088/8089 拼片同源，只拉伸中部，保留上下斜边的原像素厚度。
        private static Sprite InputSlice(Sprite source)
        {
            if (source == null || source.packed || source.border != Vector4.zero) return source;
            Sprite slice;
            if (inputSlices.TryGetValue(source, out slice)) return slice;
            float edge = Mathf.Max(0, (source.rect.height - 2) * .5f);
            slice = Sprite.Create(source.texture, source.rect, new Vector2(.5f, .5f), source.pixelsPerUnit,
                0, SpriteMeshType.FullRect, new Vector4(0, edge, 0, edge));
            slice.name = source.name + "-输入框九宫格";
            inputSlices.Add(source, slice);
            return slice;
        }

        private static void Place(RectTransform rect, float x, float y, float w, float h)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(w, h);
            rect.localScale = Vector3.one;
        }

        private static void CopyButtonStyle(Button source, Button target)
        {
            if (source == null) return;
            target.transition = source.transition; target.colors = source.colors; target.spriteState = source.spriteState;
        }

        internal static void Outline(Text text, float distance = .8f)
        {
            var outline = text.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(.015f, .055f, .04f, 1);
            outline.effectDistance = new Vector2(distance, -distance);
        }
        internal RectTransform Node(Transform parent, string name, float x, float y, float width, float height)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(width, height);
            return rt;
        }
        internal Image Image(Transform parent, string name, float x, float y, float w, float h, Sprite sprite, Color color, bool hit = false)
        {
            var image = Node(parent, name, x, y, w, h).gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.type = sprite != null && sprite.border != Vector4.zero ? UnityEngine.UI.Image.Type.Sliced : UnityEngine.UI.Image.Type.Simple;
            image.color = color;
            image.raycastTarget = hit;
            return image;
        }
        internal void Border(Transform parent, float width, float height)
        {
            Image(parent, "上金线", 0, 0, width, 1, null, Gold);
            Image(parent, "下金线", 0, height - 1, width, 1, null, Gold);
            Image(parent, "左金线", 0, 0, 1, height, null, Gold);
            Image(parent, "右金线", width - 1, 0, 1, height, null, Gold);
        }
        internal Text Text(Transform parent, string value, float x, float y, float w, float h, int size = 17, Color? color = null, TextAnchor align = TextAnchor.MiddleLeft)
        {
            var text = Node(parent, "文字", x, y, w, h).gameObject.AddComponent<Text>();
            text.font = font; text.text = value; text.fontSize = size; text.color = color ?? Ink;
            text.alignment = align; text.supportRichText = false; text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }
        internal Button Button(Transform parent, string label, float x, float y, float w, float h, Action clicked, bool enabled = true)
        {
            var image = Image(parent, label, x, y, w, h, null, new Color(.14f, .31f, .26f), true);
            if (buttonImage != null) CopyImage(buttonImage, image);
            else Border(image.transform, w, h);
            image.raycastTarget = true;
            var button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            CopyButtonStyle(buttonStyle, button);
            button.interactable = enabled;
            var text = Text(image.transform, label, 2, 0, w - 4, h, 17, ButtonGold, TextAnchor.MiddleCenter);
            原界面文字样式.按钮(text);
            if (clicked != null) button.onClick.AddListener(() => clicked());
            界面窗口管理器.注册运行时按钮(button);
            return button;
        }
        internal InputField Input(Transform parent, string placeholder, float x, float y, float w, float h, int limit, string initial = "", bool multiline = false)
        {
            if (inputTemplate == null || inputFrame == null || inputTextStyle == null)
                throw new InvalidOperationException("社交输入框缺少原建国表单模板");
            // 在 inactive 容器中解除原建国回调，再启用完整 InputField；透明编辑框覆盖原三段拼片。
            var holder = Node(parent, "输入框布局", x, y, w, h);
            holder.gameObject.SetActive(false);
            var frame = Node(holder, "原输入框拼片", 0, 0, w, h);
            Stretch(frame);
            var layout = frame.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            foreach (var pieceName in new[] { "左", "中", "右" })
            {
                var source = inputFrame.Find(pieceName) as RectTransform;
                var piece = CopyVisuals(source, frame);
                var image = piece.GetComponent<Image>();
                image.sprite = InputSlice(image.sprite);
                image.type = UnityEngine.UI.Image.Type.Sliced;
                var sizing = piece.gameObject.AddComponent<LayoutElement>();
                sizing.minWidth = sizing.preferredWidth = pieceName == "中" ? 0 : source.rect.width;
                sizing.flexibleWidth = pieceName == "中" ? 1 : 0;
                sizing.minHeight = sizing.preferredHeight = h;
            }
            var input = UnityEngine.Object.Instantiate(inputTemplate, holder, false);
            input.name = "输入框";
            input.onValueChanged = new InputField.OnChangeEvent();
#if UNITY_6000_0_OR_NEWER
            input.onEndEdit = new InputField.EndEditEvent();
            input.onSubmit = new InputField.SubmitEvent();
#else
            input.onEndEdit = new InputField.SubmitEvent();
#endif
            input.onValidateInput = null;
            input.contentType = InputField.ContentType.Standard;
            input.lineType = multiline ? InputField.LineType.MultiLineNewline : InputField.LineType.SingleLine;
            input.characterLimit = limit;
            Stretch(input.transform as RectTransform);
            var hint = input.placeholder as Text;
            var paddingMin = inputTemplate.placeholder.rectTransform.offsetMin;
            var paddingMax = inputTemplate.placeholder.rectTransform.offsetMax;
            foreach (var text in new[] { input.textComponent, hint })
            {
                text.font = inputTextStyle.font; text.fontStyle = inputTextStyle.fontStyle;
                text.fontSize = text == hint ? 16 : 17;
                text.color = inputTextStyle.color;
                if (text == hint) { var color = text.color; color.a *= .65f; text.color = color; }
                text.supportRichText = false; text.resizeTextForBestFit = false; text.raycastTarget = false;
                text.alignment = multiline ? TextAnchor.UpperLeft : TextAnchor.MiddleLeft;
                text.alignByGeometry = !multiline;
                text.horizontalOverflow = multiline ? HorizontalWrapMode.Wrap : HorizontalWrapMode.Overflow;
                text.verticalOverflow = multiline ? VerticalWrapMode.Truncate : VerticalWrapMode.Overflow;
                Stretch(text.rectTransform);
                // 单行框使用完整高度，避免模板的上下留白把字体行高截掉；仍由原编辑框遮罩裁切。
                text.rectTransform.offsetMin = new Vector2(paddingMin.x, multiline ? paddingMin.y : 0);
                text.rectTransform.offsetMax = new Vector2(paddingMax.x, multiline ? paddingMax.y : 0);
            }
            if (input.GetComponent<RectMask2D>() == null) input.gameObject.AddComponent<RectMask2D>();
            input.targetGraphic.raycastTarget = true;
            input.customCaretColor = true; input.caretColor = inputTextStyle.color;
            hint.text = placeholder;
            input.SetTextWithoutNotify(initial ?? "");
            input.gameObject.SetActive(true); holder.gameObject.SetActive(true);
            return input;
        }

        internal void Tabs(Transform parent, string[] names, string current, Action<string> choose, float width, float y)
        {
            if (tabTemplate == null) throw new InvalidOperationException("社交页签缺少原商城 Toggle 模板");
            var originalRect = (RectTransform)tabTemplate.transform;
            var container = Node(parent, "页签", 0, y, width, originalRect.rect.height);
            container.gameObject.SetActive(false);
            var layout = container.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 4;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            var group = container.gameObject.AddComponent<ToggleGroup>();
            group.allowSwitchOff = false;
            foreach (string name in names)
            {
                var toggle = UnityEngine.Object.Instantiate(tabTemplate, container, false);
                toggle.name = name;
                toggle.onValueChanged = new Toggle.ToggleEvent();
                toggle.group = null;
                toggle.SetIsOnWithoutNotify(name == current);
                toggle.group = group;
                var oldLabel = toggle.transform.Find("Image");
                if (oldLabel != null) oldLabel.gameObject.SetActive(false);
                var label = Text(toggle.transform, name, 0, 0, originalRect.rect.width, originalRect.rect.height,
                    17, ButtonGold, TextAnchor.MiddleCenter);
                原界面文字样式.按钮(label); Stretch(label.rectTransform);
                var sizing = toggle.gameObject.AddComponent<LayoutElement>();
                sizing.minWidth = sizing.preferredWidth = Mathf.Max(originalRect.rect.width, label.preferredWidth + 24);
                sizing.minHeight = sizing.preferredHeight = originalRect.rect.height;
                var background = toggle.targetGraphic.rectTransform;
                float inset = Mathf.Max(0, (originalRect.rect.width - background.rect.width) * .5f);
                Stretch(background, inset, 0, inset, 0);
                Stretch(toggle.graphic.rectTransform);
                foreach (var graphic in toggle.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
                toggle.targetGraphic.raycastTarget = true;
                toggle.onValueChanged.AddListener(selected => { if (selected) choose(name); });
                toggle.gameObject.SetActive(true);
            }
            container.gameObject.SetActive(true);
        }

        private static void Stretch(RectTransform rect, float left = 0, float bottom = 0, float right = 0, float top = 0)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.pivot = new Vector2(.5f, .5f);
            rect.offsetMin = new Vector2(left, bottom); rect.offsetMax = new Vector2(-right, -top);
            rect.localScale = Vector3.one;
        }
        internal Toggle Toggle(Transform parent, string label, float x, float y, float width, bool selected, Action<bool> changed)
        {
            var root = Node(parent, label, x, y, width, 32);
            var image = Image(root, "选框", 0, 5, 22, 22, null, Green, true); Border(image.transform, 22, 22);
            var tick = Image(image.transform, "勾选", 4, 4, 14, 14, null, Cyan);
            Text(root, label, 30, 0, width - 30, 32, 16, Muted);
            var toggle = root.gameObject.AddComponent<Toggle>(); toggle.targetGraphic = image; toggle.graphic = tick;
            toggle.isOn = selected; if (changed != null) toggle.onValueChanged.AddListener(v => changed(v));
            return toggle;
        }
        internal SocialList List(Transform parent, float x, float y, float w, float h)
        {
            var viewport = Image(parent, "滚动视口", x, y, w, h, null, Color.clear, true).rectTransform;
            viewport.gameObject.AddComponent<RectMask2D>();
            var content = Node(viewport, "列表内容", 0, 0, w, 0);
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scroll = viewport.gameObject.AddComponent<ScrollRect>(); scroll.viewport = viewport; scroll.content = content;
            scroll.horizontal = false; scroll.vertical = true; scroll.scrollSensitivity = 28;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            return new SocialList(this, content, w, scroll);
        }
        internal static void Clear(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            { var go = parent.GetChild(i).gameObject; go.SetActive(false); UnityEngine.Object.Destroy(go); }
        }
    }
    internal sealed class SocialList
    {
        private readonly SocialUi ui;
        internal readonly RectTransform Content;
        internal readonly ScrollRect Scroll;
        private readonly float width;
        internal SocialList(SocialUi skin, RectTransform content, float w, ScrollRect scroll) { ui = skin; Content = content; width = w; Scroll = scroll; }
        internal RectTransform Row(string title, string detail, float h = 64, float textWidth = 380)
        {
            var row = ui.Node(Content, "条目", 0, 0, width, h);
            var column = ui.Node(row, "条目文字", 10, 3, textWidth, h - 6);
            var textLayout = column.gameObject.AddComponent<VerticalLayoutGroup>();
            textLayout.spacing = 2; textLayout.childAlignment = TextAnchor.UpperLeft;
            textLayout.childControlWidth = textLayout.childControlHeight = true;
            textLayout.childForceExpandWidth = true; textLayout.childForceExpandHeight = false;
            var titleText = ui.Text(column, title, 0, 0, textWidth, 28, 18, SocialUi.Ink);
            titleText.name = "称呼";
            SocialScreen.TruncateSingleLine(titleText);
            var titleSize = titleText.gameObject.AddComponent<LayoutElement>();
            titleSize.minHeight = titleSize.preferredHeight = 28;
            var detailText = ui.Text(column, detail, 0, 0, textWidth, h - 36, 15, SocialUi.Muted, TextAnchor.UpperLeft);
            detailText.name = "详细信息";
            var detailSize = detailText.gameObject.AddComponent<LayoutElement>();
            detailSize.minHeight = detailSize.preferredHeight = detailText.preferredHeight;
            h = Mathf.Max(h, 41 + detailSize.preferredHeight);
            column.sizeDelta = new Vector2(textWidth, h - 6);
            var layout = row.gameObject.AddComponent<LayoutElement>();
            layout.minHeight = layout.preferredHeight = h; layout.flexibleHeight = 0;
            ui.Image(row, "分隔金线", 8, h - 1, width - 16, 1, null, SocialUi.Gold);
            return row;
        }
        internal void Empty(string title, string instruction)
        { Row(title, instruction, 110, width - 24); }
    }
}
