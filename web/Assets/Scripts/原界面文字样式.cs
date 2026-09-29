using UnityEngine;
using UnityEngine.UI;

// 字色沿用战斗速度设置；小按钮沿用原军情可点击文字的普通字重与单向阴影。
public static class 原界面文字样式
{
    public static void 对齐单行标签(Text 标签, RectTransform 输入区域)
    {
        if (标签 == null || 输入区域 == null || 标签.transform.parent != 输入区域.parent) return;
        var 区域 = 标签.rectTransform;
        区域.anchorMin = new Vector2(区域.anchorMin.x, 输入区域.anchorMin.y);
        区域.anchorMax = new Vector2(区域.anchorMax.x, 输入区域.anchorMax.y);
        区域.pivot = new Vector2(区域.pivot.x, 输入区域.pivot.y);
        区域.anchoredPosition = new Vector2(区域.anchoredPosition.x, 输入区域.anchoredPosition.y);
        标签.alignment = TextAnchor.MiddleLeft;
        标签.alignByGeometry = true;
    }

    // 旧输入框用极小的输入文字和独立显示文字叠放，编辑时无法显示字形、光标和选择范围。
    // 直接复用原 InputField、字体和背景，让输入组件负责裁切与实时显示。
    public static void 单行输入(Text 输入, Text 显示, TextAnchor 对齐, int 字号)
    {
        if (输入 == null) return;
        var 编辑框 = 输入.GetComponentInParent<InputField>(true);
        if (编辑框 == null) return;
        输入.fontSize = 字号;
        输入.fontStyle = FontStyle.Normal;
        输入.supportRichText = false;
        输入.horizontalOverflow = HorizontalWrapMode.Overflow;
        输入.verticalOverflow = VerticalWrapMode.Overflow;
        输入.alignment = 对齐;
        输入.alignByGeometry = true;
        if (编辑框.placeholder != null)
        {
            // 保留原占位文字的左右留白；单行框的完整高度用于字形居中。
            var 区域 = 输入.rectTransform;
            var 占位区域 = 编辑框.placeholder.rectTransform;
            区域.anchorMin = new Vector2(占位区域.anchorMin.x, 区域.anchorMin.y);
            区域.anchorMax = new Vector2(占位区域.anchorMax.x, 区域.anchorMax.y);
            区域.offsetMin = new Vector2(占位区域.offsetMin.x, 区域.offsetMin.y);
            区域.offsetMax = new Vector2(占位区域.offsetMax.x, 区域.offsetMax.y);
        }
        if (显示 != null && 显示 != 输入)
        {
            输入.font = 显示.font;
            输入.color = 显示.color;
            显示.gameObject.SetActive(false);
        }
        if (编辑框.GetComponent<RectMask2D>() == null) 编辑框.gameObject.AddComponent<RectMask2D>();
        编辑框.ForceLabelUpdate();
    }

    public static void 标题(Text text)
    {
        if (text == null) return;
        text.color = new Color(.884f, .894f, .66f);
        text.fontStyle = FontStyle.Bold;
        var outline = text.GetComponent<Outline>();
        if (outline == null) outline = text.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(.06f, .07f, .04f, .95f);
        outline.effectDistance = new Vector2(1.6f, -1.6f);
        outline.useGraphicAlpha = true;
    }

    public static void 按钮(Text text)
    {
        if (text == null) return;
        text.color = new Color(.847f, .835f, .584f);
        text.fontStyle = FontStyle.Normal;
        // 原军情中 15 号可点击文字使用 Shadow(-1,-1)，避免小字加粗和四向描边挤满笔画。
        Shadow shadow = null;
        foreach (var effect in text.GetComponents<Shadow>())
        {
            if (effect is Outline) effect.enabled = false;
            else shadow = effect;
        }
        if (shadow == null) shadow = text.gameObject.AddComponent<Shadow>();
        shadow.effectColor = Color.black;
        shadow.effectDistance = new Vector2(-1, -1);
        shadow.useGraphicAlpha = true;
        居中按钮文字(text);
    }

    // 复用原按钮时只修正字形基线，不覆盖原字色、字重和阴影。
    public static void 居中按钮文字(Text text)
    {
        if (text == null || (text.alignment != TextAnchor.MiddleCenter &&
            text.alignment != TextAnchor.MiddleLeft && text.alignment != TextAnchor.MiddleRight)) return;
        if (text.GetComponent<按钮字形垂直居中>() == null)
            text.gameObject.AddComponent<按钮字形垂直居中>();
    }
}
