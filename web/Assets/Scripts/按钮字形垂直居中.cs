using System;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Text))]
public sealed class 按钮字形垂直居中 : BaseMeshEffect
{
    public override void ModifyMesh(VertexHelper vh)
    {
        var text = graphic as Text;
        if (!IsActive() || text == null || text.font == null || vh.currentVertCount == 0) return;
        float pixels = text.pixelsPerUnit;
        if (pixels <= 0 || float.IsNaN(pixels) || float.IsInfinity(pixels)) return;

        // 原字形缓存不含 Shadow/Outline 扩展；组件先后次序不会改变测量依据。
        var verts = text.cachedTextGenerator.verts;
        float minY = float.PositiveInfinity, maxY = float.NegativeInfinity;
        for (int i = 0; i + 3 < verts.Count; i += 4)
        {
            float left = float.PositiveInfinity, right = float.NegativeInfinity;
            float bottom = float.PositiveInfinity, top = float.NegativeInfinity;
            float uMin = float.PositiveInfinity, uMax = float.NegativeInfinity;
            float vMin = float.PositiveInfinity, vMax = float.NegativeInfinity;
            for (int j = 0; j < 4; j++)
            {
                var vertex = verts[i + j];
                left = Math.Min(left, vertex.position.x); right = Math.Max(right, vertex.position.x);
                bottom = Math.Min(bottom, vertex.position.y); top = Math.Max(top, vertex.position.y);
                uMin = Math.Min(uMin, vertex.uv0.x); uMax = Math.Max(uMax, vertex.uv0.x);
                vMin = Math.Min(vMin, vertex.uv0.y); vMax = Math.Max(vMax, vertex.uv0.y);
            }
            // 空白和退化四边形不能把基线或原点算作可见字形边界。
            if (right <= left || top <= bottom || uMax <= uMin || vMax <= vMin) continue;
            minY = Math.Min(minY, bottom); maxY = Math.Max(maxY, top);
        }
        if (float.IsInfinity(minY) || float.IsInfinity(maxY)) return;

        // 与 Text.OnPopulateMesh 使用相同的单位及像素校正换算。
        float units = 1 / pixels;
        var first = new Vector2(verts[0].position.x, verts[0].position.y) * units;
        float rounding = text.PixelAdjustPoint(first).y - first.y;
        float delta = text.rectTransform.rect.center.y - (minY + maxY) * .5f * units - rounding;
        if (delta == 0 || float.IsNaN(delta) || float.IsInfinity(delta)) return;
        UIVertex current = default(UIVertex);
        for (int i = 0; i < vh.currentVertCount; i++)
        {
            vh.PopulateUIVertex(ref current, i);
            var position = current.position;
            position.y += delta;
            current.position = position;
            vh.SetUIVertex(current, i);
        }
    }
}
