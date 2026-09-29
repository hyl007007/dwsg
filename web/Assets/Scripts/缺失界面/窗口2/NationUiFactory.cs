using UnityEngine;
using UnityEngine.UI;

namespace 缺失界面.窗口2
{
    // Copy only RectTransform/Image decoration. Never instantiate old game-logic components or callbacks.
    internal sealed class NationUiFactory
    {
        internal readonly Font Font;
        internal readonly Vector2 ButtonSize;
        private readonly Button buttonTemplate;
        private readonly Sprite buttonSprite;
        internal static readonly Color Ink = new Color(.94f, .94f, .77f);
        internal static readonly Color Gold = new Color(1f, .88f, .36f);
        internal static readonly Color Muted = new Color(.70f, .85f, .79f);

        internal NationUiFactory(Transform source)
        {
            foreach (Text text in source.GetComponentsInChildren<Text>(true)) if (text.font != null) { Font = text.font; break; }
            var button = source.Find("概况布局/征调兵马");
            if (button == null) button = source.Find("界面操作/返回");
            if (button != null && button.GetComponent<Image>() != null) buttonSprite = button.GetComponent<Image>().sprite;
            buttonTemplate = button == null ? null : button.GetComponent<Button>();
            ButtonSize = button == null ? new Vector2(97, 39) : ((RectTransform)button).rect.size;
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

        internal Text Text(string name, Transform parent, string value, int size = 16, Color? color = null)
        {
            var rect = Rect(name, parent); var text = rect.gameObject.AddComponent<Text>();
            text.font = Font; text.fontSize = size; text.color = color ?? Ink; text.text = value;
            text.supportRichText = false; text.raycastTarget = false; text.alignment = TextAnchor.MiddleLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Truncate;
            text.lineSpacing = 1.1f; return text;
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
            var text = Text("文字", rect, label, 16, Gold); text.alignment = TextAnchor.MiddleCenter;
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

        internal RectTransform Scroll(Transform parent)
        {
            var rect = Rect("可滚动详情", parent); Place(rect, new Vector2(0, .15f), new Vector2(1, .81f), new Vector2(2, 0), new Vector2(-2, 0));
            var background = rect.gameObject.AddComponent<Image>(); background.color = new Color(0, 0, 0, .06f);
            var scroll = rect.gameObject.AddComponent<ScrollRect>(); scroll.horizontal = false; scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 28; scroll.inertia = true;
            var viewport = Rect("裁切区域", rect); Place(viewport, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-12, 0));
            viewport.gameObject.AddComponent<RectMask2D>();
            var content = Rect("详情列表", viewport); content.anchorMin = new Vector2(0, 1); content.anchorMax = Vector2.one;
            content.pivot = new Vector2(.5f, 1); content.anchoredPosition = Vector2.zero; content.sizeDelta = Vector2.zero;
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>(); layout.spacing = 7; layout.padding = new RectOffset(4, 4, 2, 8);
            layout.childControlWidth = true; layout.childControlHeight = true; layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewport; scroll.content = content;
            var barRect = Rect("滚动条", rect); Place(barRect, new Vector2(1, 0), Vector2.one, new Vector2(-8, 0), Vector2.zero);
            barRect.gameObject.AddComponent<Image>().color = new Color(.03f, .12f, .1f, .8f);
            var bar = barRect.gameObject.AddComponent<Scrollbar>(); bar.direction = Scrollbar.Direction.BottomToTop;
            var handle = Rect("滑块", barRect); Place(handle, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            handle.sizeDelta = Vector2.zero; handle.localScale = Vector3.one;
            var handleImage = handle.gameObject.AddComponent<Image>(); handleImage.color = new Color(.58f, .66f, .38f);
            bar.handleRect = handle; bar.targetGraphic = handleImage; scroll.verticalScrollbar = bar;
            return content;
        }

        internal RectTransform Row(Transform parent, float height = 50)
        {
            var rect = Rect("信息行", parent); rect.gameObject.AddComponent<LayoutElement>().preferredHeight = height;
            var layout = rect.gameObject.AddComponent<HorizontalLayoutGroup>(); layout.spacing = 12; layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlHeight = true; layout.childControlWidth = true; layout.childForceExpandHeight = false; layout.childForceExpandWidth = false;
            return rect;
        }

        internal void RowText(Transform row, string value, Color? color = null)
        {
            var text = Text("内容", row, value, 16, color); var layout = text.gameObject.AddComponent<LayoutElement>(); layout.flexibleWidth = 1; layout.minWidth = 80;
        }

        internal void Paragraph(Transform parent, string value, Color? color = null)
        {
            var text = Text("说明", parent, value, 16, color);
            text.alignment = TextAnchor.UpperLeft;
            text.gameObject.AddComponent<LayoutElement>().preferredHeight = 30;
        }

        internal InputField Input(Transform parent, string value, int limit, UnityEngine.Events.UnityAction<string> change)
        {
            var rect = Rect("正文输入", parent); rect.gameObject.AddComponent<LayoutElement>().preferredHeight = 142;
            var image = rect.gameObject.AddComponent<Image>(); image.color = new Color(.035f, .13f, .105f);
            rect.gameObject.AddComponent<RectMask2D>();
            var field = rect.gameObject.AddComponent<InputField>(); field.targetGraphic = image;
            field.lineType = InputField.LineType.MultiLineNewline; field.characterLimit = limit;
            var text = Text("编辑内容", rect, "", 16); text.alignment = TextAnchor.UpperLeft;
            Place(text.rectTransform, Vector2.zero, Vector2.one, new Vector2(10, 10), new Vector2(-10, -10));
            field.textComponent = text; field.text = value; field.onValueChanged.AddListener(change); return field;
        }
    }
}
