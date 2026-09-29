using UnityEngine;
using UnityEngine.UI;

// 字色沿用战斗速度设置；小按钮沿用原军情可点击文字的普通字重与单向阴影。
public static class 原界面文字样式
{
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
