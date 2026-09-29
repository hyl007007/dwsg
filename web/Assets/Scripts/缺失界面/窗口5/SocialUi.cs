using System;
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
        internal readonly Sprite Avatar;
        internal SocialUi(Scene scene)
        {
            Transform city = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name == "城池信息界面UI") city = root.transform;
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
            var image = Image(parent, "输入框", x, y, w, h, null, new Color(.015f, .055f, .045f), true);
            Border(image.transform, w, h);
            var input = image.gameObject.AddComponent<InputField>();
            input.targetGraphic = image; input.lineType = multiline ? InputField.LineType.MultiLineNewline : InputField.LineType.SingleLine; input.characterLimit = limit;
            input.textComponent = Text(image.transform, "", 8, 2, w - 16, h - 4, 17, Cyan);
            input.placeholder = Text(image.transform, placeholder, 8, 2, w - 16, h - 4, 16, Muted);
            input.textComponent.alignment = multiline ? TextAnchor.UpperLeft : TextAnchor.MiddleLeft;
            input.textComponent.horizontalOverflow = multiline ? HorizontalWrapMode.Wrap : HorizontalWrapMode.Overflow;
            ((Text)input.placeholder).alignment = input.textComponent.alignment;
            input.customCaretColor = true; input.caretColor = Cyan;
            input.selectionColor = new Color(.145f, .4f, .353f, .6f);
            input.text = initial ?? "";
            return input;
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
            var titleText = ui.Text(row, title, 10, 3, textWidth, 28, 18, SocialUi.Ink);
            float titleHeight = Mathf.Max(28, titleText.preferredHeight);
            titleText.rectTransform.sizeDelta = new Vector2(textWidth, titleHeight);
            var detailText = ui.Text(row, detail, 10, titleHeight + 5, textWidth, h - titleHeight - 8, 15, SocialUi.Muted, TextAnchor.UpperLeft);
            h = Mathf.Max(h, titleHeight + 13 + detailText.preferredHeight);
            var layout = row.gameObject.AddComponent<LayoutElement>();
            layout.minHeight = layout.preferredHeight = h; layout.flexibleHeight = 0;
            detailText.rectTransform.sizeDelta = new Vector2(textWidth, h - titleHeight - 9);
            ui.Image(row, "分隔金线", 8, h - 1, width - 16, 1, null, SocialUi.Gold);
            return row;
        }
        internal void Empty(string title, string instruction)
        { Row(title, instruction, 110, width - 24); }
    }
}
