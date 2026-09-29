using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Dwsg.Window3
{
    // 只在原定位画面和势力图留白区补信息，原地图、输入、返回按钮保持原布局。
    internal static class CityMapPresentation
    {
        internal static Rect Bounds(RectTransform source, Transform parent)
        {
            var corners = new Vector3[4]; source.GetWorldCorners(corners);
            Vector2 min = parent.InverseTransformPoint(corners[0]), max = min;
            for (int i = 1; i < corners.Length; i++)
            {
                Vector2 point = parent.InverseTransformPoint(corners[i]);
                min = Vector2.Min(min, point); max = Vector2.Max(max, point);
            }
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        private static RectTransform Area(string name, RectTransform parent, Rect bounds)
        {
            var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rect.gameObject.layer = parent.gameObject.layer;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = parent.pivot;
            rect.sizeDelta = bounds.size; rect.anchoredPosition = bounds.center;
            return rect;
        }

        internal static Text CopyText(Text original, Transform parent, string name)
        {
            var text = Object.Instantiate(original, parent, false); text.name = name;
            text.gameObject.SetActive(true); text.enabled = true; text.raycastTarget = false;
            // 原浮动提示自行按内容缩放；放入布局组后由父槽统一控制宽高。
            var fitter = text.GetComponent<ContentSizeFitter>();
            if (fitter != null) fitter.enabled = false;
            text.supportRichText = false; text.resizeTextForBestFit = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Truncate;
            text.rectTransform.localScale = Vector3.one;
            return text;
        }

        internal static void SingleLineField(Text text)
        {
            text.alignment = (TextAnchor)(3 + (int)text.alignment % 3);
            text.alignByGeometry = true;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            原界面文字样式.居中按钮文字(text);
        }

        internal static Text LocatorFeedback(RectTransform locator, RectTransform query, RectTransform locate,
            RectTransform powers, Text original)
        {
            Rect input = Bounds(query, locator), button = Bounds(locate, locator), lower = Bounds(powers, locator);
            float gap = original.fontSize * .5f;
            var area = Area("定位反馈区域", locator, Rect.MinMaxRect(Mathf.Min(input.xMin, button.xMin), lower.yMax + gap,
                Mathf.Max(input.xMax, button.xMax), Mathf.Min(input.yMin, button.yMin) - gap));
            var layout = area.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = true;
            var feedback = CopyText(original, area, "定位操作反馈");
            feedback.alignment = TextAnchor.UpperLeft;
            feedback.text = "";
            return feedback;
        }

        internal static Color OwnershipColor(int identity)
        {
            return 颜色类.GetColor(identity == 0 ? "#00FF00" : identity == 1 ? "#FF0000" : "#908E90");
        }

        internal static void PowerLegend(GameObject markers, IList<城池信息库类> plotted)
        {
            var map = markers.transform.parent;
            if (map == null) return;
            var band = map.Find("操作布局") as RectTransform;
            var back = map.Find("使用皇榜按钮 (2)") as RectTransform;
            var view = CityNavigation.Find<城池信息显示脚本>(v => v.国家显示 != null);
            var dot = markers.transform.childCount == 0 ? null : markers.transform.GetChild(0).GetComponent<Image>();
            if (band == null || back == null || view == null || dot == null) return;
            var old = band.Find("势力图说明");
            if (old != null) { old.gameObject.SetActive(false); Object.Destroy(old.gameObject); }
            // 原操作栏两侧空白由原返回按钮的边界划分，不移动返回，也不侵入地图点位。
            var root = Area("势力图说明", band, band.rect);
            // 原按钮先渲染点阵、后启用地图 Canvas；两侧说明等最终矩形有效后再显示。
            root.anchorMin = Vector2.zero; root.anchorMax = Vector2.one;
            root.offsetMin = root.offsetMax = Vector2.zero;
            var template = view.国家显示;
            float padding = template.fontSize;
            var left = Area("归属图例", root, Rect.zero);
            var right = Area("国家说明", root, Rect.zero);
            left.gameObject.SetActive(false); right.gameObject.SetActive(false);
            var legend = left.gameObject.AddComponent<VerticalLayoutGroup>();
            legend.childAlignment = TextAnchor.MiddleLeft;
            legend.childControlWidth = legend.childControlHeight = true;
            legend.childForceExpandWidth = legend.childForceExpandHeight = true;
            int[] count = new int[3]; var otherNations = new HashSet<string>();
            foreach (var city in plotted)
            {
                int identity = city.获取城池身份();
                count[identity == 0 ? 0 : identity == 1 ? 1 : 2]++;
                if (identity == 1 && !string.IsNullOrEmpty(city.国家)) otherNations.Add(city.国家);
            }
            string[] names = { "我方", "其他国家", "无归属" };
            for (int i = 0; i < names.Length; i++)
            {
                var row = Area(names[i], left, new Rect(Vector2.zero, Vector2.zero));
                var horizontal = row.gameObject.AddComponent<HorizontalLayoutGroup>();
                horizontal.childAlignment = TextAnchor.MiddleLeft; horizontal.spacing = padding * .5f;
                horizontal.childControlWidth = horizontal.childControlHeight = true;
                horizontal.childForceExpandWidth = horizontal.childForceExpandHeight = false;
                var swatch = Object.Instantiate(dot, row, false); swatch.name = "原城池色块";
                swatch.gameObject.SetActive(true); swatch.raycastTarget = false;
                swatch.color = OwnershipColor(i == 2 ? 3 : i);
                var size = swatch.gameObject.AddComponent<LayoutElement>();
                size.minWidth = size.preferredWidth = dot.rectTransform.rect.width;
                size.minHeight = size.preferredHeight = dot.rectTransform.rect.height;
                var label = CopyText(template, row, "归属与城池数"); label.alignment = TextAnchor.MiddleLeft;
                var labelSlot = label.gameObject.AddComponent<LayoutElement>();
                labelSlot.flexibleWidth = labelSlot.flexibleHeight = 1;
                label.text = names[i] + "：" + count[i] + "城";
            }
            var nation = CityLocalAdapter.Me == null ? "" : CityLocalAdapter.Me.基础信息.国家;
            var data = string.IsNullOrEmpty(nation) ? null : 全局方法类.获取指定名字的国家(nation);
            var summary = CopyText(template, right, "当前国家数据");
            var rect = summary.rectTransform; rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero; summary.alignment = TextAnchor.MiddleLeft;
            summary.text = "本国：" + (string.IsNullOrEmpty(nation) ? "未加入国家" : data == null ? nation : data.国名 + "（" + data.国号 + "）") +
                "\n我方含本人城池及本国城池\n其他国家：" + otherNations.Count + "国 · 已绘制" + plotted.Count + "城";
            root.gameObject.AddComponent<PowerLegendLayout>().Initialize(band, back, left, right, padding);
        }
    }
}
