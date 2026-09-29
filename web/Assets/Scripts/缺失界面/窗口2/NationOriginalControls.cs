using UnityEngine;
using UnityEngine.UI;

namespace 缺失界面.窗口2
{
    internal static class NationOriginalControls
    {
        internal sealed class ListMetrics
        {
            internal float Height, NameHeight, FrameHeight, NameTop, NameTopGrowth, NameAnchorY, SpacingY;
            internal int PaddingTop;
            internal Vector2[] Positions, Sizes;
            internal ListMetrics(Transform template)
            {
                Height = ((RectTransform)template).sizeDelta.y;
                Positions = new Vector2[template.childCount]; Sizes = new Vector2[template.childCount];
                for (int i = 0; i < template.childCount; i++)
                { var rect = (RectTransform)template.GetChild(i); Positions[i] = rect.anchoredPosition; Sizes[i] = rect.sizeDelta; }
                NameHeight = Sizes[2].y; FrameHeight = Height;
                for (int i = 0; i < template.childCount; i++)
                    if (template.GetChild(i).name == "通用透黑背景" || template.GetChild(i).name == "选中背景")
                        FrameHeight = Mathf.Max(FrameHeight, Sizes[i].y);
                var name = (RectTransform)template.GetChild(2);
                NameTopGrowth = 1 - name.pivot.y; NameTop = Positions[2].y + NameHeight * NameTopGrowth;
                NameAnchorY = name.anchorMax.y;
                var grid = template.parent.GetComponent<GridLayoutGroup>();
                if (grid != null) { SpacingY = grid.spacing.y; PaddingTop = grid.padding.top; }
            }
        }

        internal static void FitListRow(Transform row, ListMetrics metrics)
        {
            for (int i = 0; i < metrics.Positions.Length; i++)
            { var rect = (RectTransform)row.GetChild(i); rect.anchoredPosition = metrics.Positions[i]; rect.sizeDelta = metrics.Sizes[i]; }
            var name = row.GetChild(2).GetComponent<Text>(); name.resizeTextForBestFit = false;
            name.supportRichText = false; name.horizontalOverflow = HorizontalWrapMode.Wrap; name.verticalOverflow = VerticalWrapMode.Truncate;
            float extra = Mathf.Max(0, name.preferredHeight + 2 - metrics.NameHeight);
            var nameRect = name.rectTransform; nameRect.sizeDelta = new Vector2(nameRect.sizeDelta.x, metrics.NameHeight + extra);
            for (int i = 0; i < metrics.Positions.Length; i++)
            {
                var child = row.GetChild(i); var rect = (RectTransform)child;
                if (i > 2 && child.GetComponent<Text>() != null) rect.anchoredPosition -= new Vector2(0, extra / 2);
                if (child.name == "通用透黑背景" || child.name == "选中背景") rect.sizeDelta += new Vector2(0, extra);
            }
            var rowRect = (RectTransform)row; rowRect.sizeDelta = new Vector2(rowRect.sizeDelta.x, metrics.Height + extra);
            var element = row.GetComponent<LayoutElement>(); if (element == null) element = row.gameObject.AddComponent<LayoutElement>();
            element.minHeight = element.preferredHeight = metrics.Height + extra; element.flexibleHeight = 0;
        }

        internal static void FitListGrid(Transform list, ListMetrics metrics)
        {
            var grid = list.GetComponent<GridLayoutGroup>(); if (grid == null) return;
            float extra = 0;
            foreach (Transform row in list)
            {
                if (!row.gameObject.activeSelf) continue;
                var element = row.GetComponent<LayoutElement>();
                if (element != null) extra = Mathf.Max(extra, element.preferredHeight - metrics.Height);
            }
            // 原 Grid 不读取各行 preferredHeight；统一容纳原选中框及扩展后的文字。
            float height = Mathf.Ceil(metrics.FrameHeight + extra);
            int top = Mathf.CeilToInt(Mathf.Max(0, metrics.NameTop + extra * metrics.NameTopGrowth - height * (1 - metrics.NameAnchorY)));
            float spacing = Mathf.Max(metrics.SpacingY, top);
            if (!Mathf.Approximately(grid.cellSize.y, height)) grid.cellSize = new Vector2(grid.cellSize.x, height);
            if (!Mathf.Approximately(grid.spacing.y, spacing)) grid.spacing = new Vector2(grid.spacing.x, spacing);
            int paddingTop = Mathf.Max(metrics.PaddingTop, top);
            if (grid.padding.top != paddingTop)
                grid.padding = new RectOffset(grid.padding.left, grid.padding.right, paddingTop, grid.padding.bottom);
        }

        internal static Text Text(Transform parent, string name, string value, Text template, float x, float y, float width, float height)
        {
            var rect = NationUiFactory.Rect(name, parent);
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f); rect.sizeDelta = new Vector2(width, height); rect.anchoredPosition = new Vector2(x, y);
            var text = rect.gameObject.AddComponent<Text>();
            if (template != null) { text.font = template.font; text.color = template.color; }
            text.text = value; text.fontSize = 18; text.lineSpacing = 1; text.fontStyle = FontStyle.Normal;
            text.supportRichText = false; text.raycastTarget = false; text.alignment = TextAnchor.MiddleLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        internal static void Caption(Button button, string value, Text template)
        {
            if (button == null) return;
            var caption = button.transform.Find("国家操作文字");
            if (caption == null)
            {
                foreach (Transform child in button.transform) if (child.GetComponent<Image>() != null) child.gameObject.SetActive(false);
                caption = Text(button.transform, "国家操作文字", "", template, 0, 0, ((RectTransform)button.transform).rect.width - 6, ((RectTransform)button.transform).rect.height - 4).transform;
                原界面文字样式.按钮(caption.GetComponent<Text>());
                caption.GetComponent<Text>().alignment = TextAnchor.MiddleCenter;
            }
            caption.GetComponent<Text>().text = value;
            原界面文字样式.居中按钮文字(caption.GetComponent<Text>());
        }

        internal static InputField Amount(Transform parent, string name, Text template, Transform frame, float x, float y)
        {
            var rect = NationUiFactory.Rect(name, parent); rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.sizeDelta = new Vector2(222, 30); rect.anchoredPosition = new Vector2(x, y);
            var source = frame == null ? null : frame.GetComponent<Image>(); var image = rect.gameObject.AddComponent<Image>();
            if (source != null) { image.sprite = source.sprite; image.type = source.type; image.color = source.color; }
            else image.color = new Color(0, 0, 0, .25f);
            rect.gameObject.AddComponent<RectMask2D>();
            var text = Text(rect, "数量", "", template, 0, 0, 202, 28);
            var field = rect.gameObject.AddComponent<InputField>(); field.targetGraphic = image; field.textComponent = text;
            field.characterLimit = 16; field.contentType = InputField.ContentType.IntegerNumber;
            var placeholder = Text(rect, "提示", "0", template, 0, 0, 202, 28); placeholder.color *= new Color(1, 1, 1, .55f);
            field.placeholder = placeholder; field.text = ""; return field;
        }

        internal static Text Empty(Transform list, Text template)
        {
            var empty = list.parent.Find("国家列表空状态"); if (empty != null) return empty.GetComponent<Text>();
            var text = Text(list.parent, "国家列表空状态", "", template, 0, 0, 340, 100);
            text.alignment = TextAnchor.MiddleCenter; return text;
        }

        internal static void ReserveListStatus(Transform list)
        {
            var scroll = list.GetComponentInParent<ScrollRect>(true); if (scroll == null) return;
            scroll.horizontal = false; scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 24;
            var rect = (RectTransform)scroll.transform; float top = rect.anchoredPosition.y + rect.sizeDelta.y / 2;
            rect.sizeDelta = new Vector2(rect.sizeDelta.x, 266); rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, top - 133);
            var viewport = list.parent as RectTransform;
            NationUiFactory.Place(viewport, Vector2.zero, Vector2.one, new Vector2(4, 4), new Vector2(-24, -4));
            scroll.viewport = viewport;
            if (scroll.verticalScrollbar != null)
                NationUiFactory.Place((RectTransform)scroll.verticalScrollbar.transform, new Vector2(1, 0), Vector2.one, new Vector2(-20, 4), new Vector2(0, -4));
            scroll.StopMovement(); scroll.verticalNormalizedPosition = 1;
        }

        internal static void ReturnToNation(MonoBehaviour source, string code)
        {
            foreach (GameObject root in source.gameObject.scene.GetRootGameObjects())
            {
                var installer = root.GetComponent<NationUiInstaller>();
                if (installer != null && installer.Window != null && installer.Window.OpenExistingNation(code)) return;
            }
            foreach (GameObject root in source.gameObject.scene.GetRootGameObjects())
            {
                if (root.name != "国家信息界面UI") continue;
                var overview = root.GetComponentInChildren<显示概况脚本>(true);
                if (overview != null) { overview.查看国号 = code; overview.刷新显示(); }
                root.SetActive(true); return;
            }
        }

        internal static void Fit(Text text)
        {
            if (text == null) return;
            text.supportRichText = false; text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Truncate;
            text.resizeTextForBestFit = true; text.resizeTextMinSize = 14; text.resizeTextMaxSize = Mathf.Max(14, text.fontSize);
        }

        internal static void SingleLine(Text text, string suffix = "")
        {
            if (text == null || text.font == null) return;
            text.supportRichText = false; text.fontSize = 18; text.lineSpacing = 1;
            text.resizeTextForBestFit = false; text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            string value = text.text.Replace('\n', ' ').Replace('\r', ' ');
            text.text = value;
            // 原行高约 21，现用字体的 18 号单行约 26.4；保留字号并扩展文字自身的行框。
            float height = Mathf.Ceil(text.preferredHeight + 2);
            if (text.rectTransform.rect.height < height)
                text.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            float width = text.rectTransform.rect.width;
            if (width <= 0 || text.preferredWidth <= width) return;
            if (suffix.Length > 0 && value.EndsWith(suffix)) value = value.Substring(0, value.Length - suffix.Length);
            while (value.Length > 0 && text.preferredWidth > width)
            {
                int count = value.Length - 1;
                if (count > 0 && char.IsLowSurrogate(value[count]) && char.IsHighSurrogate(value[count - 1])) count--;
                value = value.Substring(0, count); text.text = value + "…" + suffix;
            }
            if (text.preferredWidth > width) text.text = "…";
        }
    }
}
