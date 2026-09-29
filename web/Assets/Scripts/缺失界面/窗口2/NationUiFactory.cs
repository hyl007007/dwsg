using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using Dwsg.Window1;

namespace 缺失界面.窗口2
{
    // Copy only RectTransform/Image decoration. Never instantiate old game-logic components or callbacks.
    internal sealed class NationUiFactory
    {
        internal readonly Font Font;
        internal readonly Vector2 ButtonSize;
        private readonly int buttonFontSize;
        private readonly Button buttonTemplate;
        private readonly Sprite buttonSprite;
        private readonly Transform rowDecoration;
        private readonly Color bodyColor;
        private readonly Window1Style scrollStyle;
        private readonly Transform inputSkin;
        private static readonly Dictionary<Sprite, Sprite> inputSlices = new Dictionary<Sprite, Sprite>();
        internal Color BodyColor { get { return bodyColor; } }
        internal static readonly Color Ink = new Color(.94f, .94f, .77f);
        internal static readonly Color Gold = new Color(1f, .88f, .36f);
        internal static readonly Color Muted = new Color(.70f, .85f, .79f);

        internal NationUiFactory(Transform source)
        {
            scrollStyle = Window1Style.FromScene(source.gameObject.scene);
            foreach (Text text in source.GetComponentsInChildren<Text>(true)) if (text.font != null) { Font = text.font; break; }
            var body = source.Find("概况布局/信息列表布局/国名/显示");
            bodyColor = body != null && body.GetComponent<Text>() != null ? body.GetComponent<Text>().color : Ink;
            foreach (GameObject root in source.gameObject.scene.GetRootGameObjects())
            {
                if (root.name == "国家列表布局") rowDecoration = root.transform.Find("列表布局/显示区域/列表/国家1/通用透黑背景");
                var founding = root.GetComponent<建国脚本>();
                if (founding == null || founding.国名输入对象 == null) continue;
                var field = founding.国名输入对象.GetComponentInParent<InputField>(true);
                if (field != null) inputSkin = field.transform.parent.Find("输入框布局");
            }
            var button = source.Find("概况布局/征调兵马");
            if (button == null) button = source.Find("界面操作/返回");
            if (button != null && button.GetComponent<Image>() != null) buttonSprite = button.GetComponent<Image>().sprite;
            buttonTemplate = button == null ? null : button.GetComponent<Button>();
            ButtonSize = button == null ? new Vector2(97, 39) : ((RectTransform)button).rect.size;
            var caption = button == null ? null : button.GetComponentInChildren<Text>(true);
            buttonFontSize = caption == null ? 18 : caption.fontSize;
        }

        internal static RectTransform Rect(string name, Transform parent)
        {
            var gameObject = new GameObject(name, typeof(RectTransform)); gameObject.layer = 5;
            var rect = gameObject.GetComponent<RectTransform>(); rect.SetParent(parent, false); return rect;
        }

        internal static void Place(RectTransform rect, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = offsetMin; rect.offsetMax = offsetMax;
        }

        internal static void CopyRect(RectTransform from, RectTransform to)
        {
            to.anchorMin = from.anchorMin; to.anchorMax = from.anchorMax; to.pivot = from.pivot;
            to.anchoredPosition = from.anchoredPosition; to.sizeDelta = from.sizeDelta;
            to.localScale = new Vector3(from.localScale.x, from.localScale.y, 1);
            to.localRotation = from.localRotation;
        }

        // Keep a native panel's region when its decoration has an extra nesting level.
        internal static void CopyRegion(RectTransform from, RectTransform to)
        {
            var corners = new Vector3[4]; from.GetWorldCorners(corners);
            var parent = (RectTransform)to.parent;
            Vector2 min = parent.InverseTransformPoint(corners[0]);
            Vector2 max = parent.InverseTransformPoint(corners[2]);
            to.anchorMin = to.anchorMax = to.pivot = new Vector2(.5f, .5f);
            to.sizeDelta = max - min; to.anchoredPosition = (min + max) * .5f - parent.rect.center;
        }

        internal static string Amount(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) return "数据异常";
            double magnitude = Math.Abs(value);
            double unit = magnitude >= 1e12 ? 1e12 : magnitude >= 1e8 ? 1e8 : magnitude >= 1e4 ? 1e4 : 1;
            string suffix = unit == 1e12 ? "万亿" : unit == 1e8 ? "亿" : unit == 1e4 ? "万" : "";
            return (value / unit).ToString("0.##", CultureInfo.InvariantCulture) + suffix;
        }

        internal static VerticalLayoutGroup Vertical(RectTransform parent, int padding = 4, int spacing = 4)
        {
            var layout = parent.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(padding, padding, padding, padding); layout.spacing = spacing;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;
            return layout;
        }

        internal static void Height(Component component, float height, float flexible = 0)
        {
            var element = component.GetComponent<LayoutElement>();
            if (element == null) element = component.gameObject.AddComponent<LayoutElement>();
            element.minHeight = element.preferredHeight = height; element.flexibleHeight = flexible;
        }

        internal static RectTransform Decoration(Transform from, Transform parent, bool isTitle = false)
        {
            if (from == null || from.GetComponent<Button>() != null || from.GetComponent<Toggle>() != null || from.GetComponent<InputField>() != null) return null;
            var sourceRect = from as RectTransform; if (sourceRect == null) return null;
            isTitle = isTitle || from.name == "标题栏背景";
            var image = from.GetComponent<Image>();
            // The old central 国家 title is text painted into 8097.dat.png, not frame decoration.
            if (isTitle && image != null && image.sprite != null && (image.sprite.name == "8097.dat" || from.name == "8 (21)")) return null;
            var rect = Rect(from.name, parent); CopyRect(sourceRect, rect);
            if (image != null)
            {
                var copy = rect.gameObject.AddComponent<Image>(); copy.sprite = image.sprite; copy.color = image.color;
                copy.type = image.type; copy.preserveAspect = image.preserveAspect;
                copy.fillCenter = image.fillCenter; copy.raycastTarget = false;
            }
            foreach (Transform child in from) Decoration(child, rect, isTitle);
            return rect;
        }

        internal Text Text(string name, Transform parent, string value, int size = 18, Color? color = null)
        {
            var rect = Rect(name, parent); var text = rect.gameObject.AddComponent<Text>();
            text.font = Font; text.fontSize = size; text.color = color ?? bodyColor; text.text = value;
            text.supportRichText = false; text.raycastTarget = false; text.alignment = TextAnchor.MiddleLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Truncate;
            text.lineSpacing = 1; return text;
        }

        internal Button Button(string name, Transform parent, string label, UnityEngine.Events.UnityAction action)
        {
            var rect = Rect(name, parent); var image = rect.gameObject.AddComponent<Image>();
            image.sprite = buttonSprite; image.type = Image.Type.Sliced;
            image.color = buttonSprite == null ? new Color(.12f, .3f, .25f) : Color.white;
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            if (buttonTemplate != null)
            {
                button.transition = buttonTemplate.transition; button.colors = buttonTemplate.colors;
                button.spriteState = buttonTemplate.spriteState;
            }
            var text = Text("文字", rect, label, buttonFontSize, Gold); text.alignment = TextAnchor.MiddleCenter;
            StyleCaption(text);
            Place(text.rectTransform, Vector2.zero, Vector2.one, new Vector2(5, 3), new Vector2(-5, -3));
            var layout = rect.gameObject.AddComponent<LayoutElement>(); layout.minWidth = layout.preferredWidth = ButtonSize.x;
            layout.minHeight = layout.preferredHeight = ButtonSize.y; layout.flexibleHeight = layout.flexibleWidth = 0;
            button.onClick.AddListener(action); 界面窗口管理器.注册运行时按钮(button); return button;
        }

        internal static void StyleCaption(Text text)
        {
            text.lineSpacing = 1; 原界面文字样式.按钮(text);
        }

        internal Button Close(Transform source, Transform parent, UnityEngine.Events.UnityAction action)
        {
            var rect = Rect("关闭详情", parent); CopyRect((RectTransform)source, rect);
            rect.anchoredPosition += ((RectTransform)source.parent).anchoredPosition;
            var original = source.GetComponent<Image>(); var image = rect.gameObject.AddComponent<Image>();
            image.sprite = original.sprite; image.color = original.color; image.type = original.type;
            image.preserveAspect = original.preserveAspect;
            foreach (Transform child in source) Decoration(child, rect);
            var button = rect.gameObject.AddComponent<Button>(); var template = source.GetComponent<Button>();
            button.targetGraphic = image; button.transition = template.transition;
            button.colors = template.colors; button.spriteState = template.spriteState;
            button.onClick.AddListener(action); 界面窗口管理器.注册运行时按钮(button); return button;
        }

        internal RectTransform Scroll(Transform parent, out ScrollRect scroll)
        {
            var rect = Rect("可滚动详情", parent);
            Place(rect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Height(rect, 0, 1);
            var background = rect.gameObject.AddComponent<Image>(); background.color = new Color(0, 0, 0, .06f);
            scroll = rect.gameObject.AddComponent<ScrollRect>(); scroll.horizontal = false; scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 28; scroll.inertia = true;
            var viewport = Rect("裁切区域", rect); Place(viewport, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-20, 0));
            viewport.gameObject.AddComponent<RectMask2D>();
            var content = Rect("详情列表", viewport); content.anchorMin = new Vector2(0, 1); content.anchorMax = Vector2.one;
            content.pivot = new Vector2(.5f, 1); content.anchoredPosition = Vector2.zero; content.sizeDelta = Vector2.zero;
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>(); layout.spacing = 3; layout.padding = new RectOffset(4, 4, 2, 6);
            layout.childControlWidth = true; layout.childControlHeight = true; layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewport; scroll.content = content;
            scroll.verticalScrollbar = scrollStyle.VerticalScrollbar(rect);
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            return content;
        }

        internal RectTransform Row(Transform parent, float height = 42)
        {
            var rect = Rect("信息行", parent); var element = rect.gameObject.AddComponent<LayoutElement>();
            // 明确覆盖 HorizontalLayoutGroup 的最小高度，不能沿用首次尚未分配宽度的文字高度。
            element.minHeight = element.preferredHeight = height; element.flexibleHeight = 0;
            var layout = rect.gameObject.AddComponent<HorizontalLayoutGroup>(); layout.spacing = 12; layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlHeight = true; layout.childControlWidth = true; layout.childForceExpandHeight = false; layout.childForceExpandWidth = false;
            return rect;
        }

        internal static void MeasureContent(RectTransform content)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            foreach (Transform child in content)
            {
                var paragraph = child.GetComponent<Text>();
                var paragraphLayout = child.GetComponent<LayoutElement>();
                if (paragraph != null && paragraphLayout != null) paragraphLayout.preferredHeight = Mathf.Ceil(paragraph.preferredHeight + 4);
                if (child.GetComponent<HorizontalLayoutGroup>() == null) continue;
                var element = child.GetComponent<LayoutElement>(); if (element == null) continue;
                float height = Mathf.Max(0, element.minHeight);
                foreach (Transform item in child)
                {
                    var itemLayout = item.GetComponent<LayoutElement>();
                    if (itemLayout != null && itemLayout.ignoreLayout) continue;
                    var text = item.GetComponent<Text>();
                    if (text != null)
                    {
                        // 行内文字也要获得完整高度；只放大外层行，HorizontalLayoutGroup 仍会把文字取整裁短。
                        if (itemLayout == null) itemLayout = item.gameObject.AddComponent<LayoutElement>();
                        float textHeight = Mathf.Ceil(text.preferredHeight + 2);
                        itemLayout.minHeight = itemLayout.preferredHeight = textHeight;
                        itemLayout.flexibleHeight = 0;
                        height = Mathf.Max(height, textHeight + 4);
                    }
                    else height = Mathf.Max(height, LayoutUtility.GetMinHeight((RectTransform)item));
                }
                // 从本次文字测量重新赋值；不能用上次 preferredHeight 做下限，否则行只增不减。
                element.preferredHeight = Mathf.Ceil(height);
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        }

        internal void ListBackground(RectTransform row)
        {
            var background = Decoration(rowDecoration, row); if (background == null) return;
            background.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            Place(background, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero); background.SetAsFirstSibling();
        }

        internal void RowText(Transform row, string value, Color? color = null)
        {
            var text = Text("内容", row, value, 18, color); var layout = text.gameObject.AddComponent<LayoutElement>(); layout.flexibleWidth = 1; layout.minWidth = 80;
        }

        internal void Paragraph(Transform parent, string value, Color? color = null)
        {
            var text = Text("说明", parent, value, 18, color);
            text.alignment = TextAnchor.UpperLeft;
            text.gameObject.AddComponent<LayoutElement>().preferredHeight = 30;
        }

        internal InputField Input(Transform parent, string value, int limit, UnityEngine.Events.UnityAction<string> change)
        {
            var rect = Rect("正文输入", parent); var inputLayout = rect.gameObject.AddComponent<LayoutElement>();
            // InputField 的布局优先级也是1；长正文会覆盖同级的142高度，必须由容器显式固定输入区域。
            inputLayout.layoutPriority = 2; inputLayout.minHeight = inputLayout.preferredHeight = 142; inputLayout.flexibleHeight = 0;
            var image = rect.gameObject.AddComponent<Image>();
            image.color = ApplyInputSkin(rect) ? Color.clear : new Color(0, 0, 0, .25f);
            rect.gameObject.AddComponent<RectMask2D>();
            var field = rect.gameObject.AddComponent<InputField>(); field.targetGraphic = image;
            field.lineType = InputField.LineType.MultiLineNewline; field.characterLimit = limit;
            var text = Text("编辑内容", rect, "", 18); text.alignment = TextAnchor.UpperLeft;
            Place(text.rectTransform, Vector2.zero, Vector2.one, new Vector2(10, 10), new Vector2(-10, -10));
            field.textComponent = text; field.text = value; field.onValueChanged.AddListener(change); return field;
        }

        internal bool ApplyInputSkin(RectTransform target)
        {
            if (inputSkin == null) return false;
            var frame = Rect("原建国输入框背景", target);
            Place(frame, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var layout = frame.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = false; layout.childForceExpandHeight = true;
            foreach (Transform part in inputSkin)
            {
                var source = part.GetComponent<Image>(); if (source == null) continue;
                var copy = Decoration(part, frame); if (copy == null) continue;
                var image = copy.GetComponent<Image>(); image.type = Image.Type.Sliced;
                image.sprite = InputSlice(source.sprite); image.raycastTarget = false;
                var element = copy.gameObject.AddComponent<LayoutElement>();
                bool center = part.name == "中";
                element.minWidth = element.preferredWidth = center ? 0 : ((RectTransform)part).rect.width;
                element.flexibleWidth = center ? 1 : 0;
            }
            return true;
        }

        // Preserve the native end caps and top/bottom bevels in a multiline input.
        // No new texture is drawn: the original 8088/8089 sprite pixels supply the nine-slice.
        private static Sprite InputSlice(Sprite source)
        {
            if (source == null || source.packed || source.border != Vector4.zero) return source;
            Sprite slice; if (inputSlices.TryGetValue(source, out slice)) return slice;
            float edge = Mathf.Max(0, (source.rect.height - 2) * .5f);
            slice = Sprite.Create(source.texture, source.rect, new Vector2(.5f, .5f), source.pixelsPerUnit,
                0, SpriteMeshType.FullRect, new Vector4(0, edge, 0, edge));
            slice.name = source.name + "-输入框九宫格"; inputSlices.Add(source, slice); return slice;
        }
    }
}
