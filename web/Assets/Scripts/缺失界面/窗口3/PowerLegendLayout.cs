using UnityEngine;

namespace Dwsg.Window3
{
    // 等原 Canvas 启用后读取真实底栏/返回矩形，仅在几何变化时更新说明区域。
    [DisallowMultipleComponent]
    public sealed class PowerLegendLayout : MonoBehaviour
    {
        private RectTransform root, footer, back, left, right;
        private Rect lastFooter, lastButton;
        private float padding;
        private bool pending = true;

        internal void Initialize(RectTransform footer, RectTransform back, RectTransform left, RectTransform right, float padding)
        {
            root = (RectTransform)transform;
            this.footer = footer; this.back = back; this.left = left; this.right = right; this.padding = padding;
            pending = true;
        }

        private void OnEnable() { pending = true; }
        private void OnRectTransformDimensionsChange() { pending = true; }

        private void LateUpdate()
        {
            if (root == null || footer == null || back == null || left == null || right == null) return;
            Rect band = CityMapPresentation.Bounds(footer, root), button = CityMapPresentation.Bounds(back, root);
            if (band.width <= 0 || band.height <= 0 || button.width <= 0 || button.height <= 0) return;
            if (!pending && band == lastFooter && button == lastButton) return;
            Place(left, Rect.MinMaxRect(band.xMin + padding, band.yMin + padding,
                button.xMin - padding, band.yMax - padding));
            Place(right, Rect.MinMaxRect(button.xMax + padding, band.yMin + padding,
                band.xMax - padding, band.yMax - padding));
            left.gameObject.SetActive(true); right.gameObject.SetActive(true);
            lastFooter = band; lastButton = button; pending = false;
        }

        private void Place(RectTransform area, Rect bounds)
        {
            area.anchorMin = area.anchorMax = root.pivot;
            area.sizeDelta = bounds.size; area.anchoredPosition = bounds.center;
        }
    }
}
