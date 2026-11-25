using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 主地图单 DrawCall 平滑折线绘制器：一次绘制整条连续平滑线路的主体、分段热力内芯与圆帽，
/// 彻底消除平滑曲线细分带来的 UI GameObject 膨胀与 Canvas Rebuild 卡顿。
/// </summary>
[RequireComponent(typeof(CanvasRenderer))]
public class LinePolylineGraphic : MaskableGraphic
{
    readonly List<Vector2> points = new List<Vector2>(64);
    readonly List<int> subSegLogicIndices = new List<int>(64);
    readonly Dictionary<int, Color> logicSegColors = new Dictionary<int, Color>();

    public float bodyWidth = 9.2f;
    public float coreWidth = 2.5f;
    public Color bodyColor = Color.white;
    public Color coreColor = new Color(1f, 0.98f, 0.92f, 0.6f);

    public void SetPolyline(
        IReadOnlyList<Vector2> pts,
        Vector2 offset,
        Color bodyCol,
        Color coreCol,
        float bodyW,
        float coreW,
        IReadOnlyList<int> segLogicIndices = null)
    {
        points.Clear();
        subSegLogicIndices.Clear();
        if (pts != null)
        {
            for (int i = 0; i < pts.Count; i++) points.Add(pts[i] + offset);
        }
        if (segLogicIndices != null)
        {
            for (int i = 0; i < segLogicIndices.Count; i++) subSegLogicIndices.Add(segLogicIndices[i]);
        }
        bodyColor = bodyCol;
        coreColor = coreCol;
        bodyWidth = Mathf.Max(1.5f, bodyW);
        coreWidth = Mathf.Max(0.5f, coreW);
        SetVerticesDirty();
    }

    public void SetCoreColor(Color col)
    {
        if (coreColor == col && logicSegColors.Count == 0) return;
        coreColor = col;
        logicSegColors.Clear();
        SetVerticesDirty();
    }

    public void SetLogicSegmentCoreColor(int logicSegIdx, Color col, bool markDirty = true)
    {
        if (logicSegColors.TryGetValue(logicSegIdx, out var prev) && prev == col) return;
        logicSegColors[logicSegIdx] = col;
        if (markDirty) SetVerticesDirty();
    }

    public void SetWidths(float bodyW, float coreW)
    {
        if (Mathf.Approximately(bodyWidth, bodyW) && Mathf.Approximately(coreWidth, coreW)) return;
        bodyWidth = Mathf.Max(1.5f, bodyW);
        coreWidth = Mathf.Max(0.5f, coreW);
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if (points.Count < 2) return;

        DrawRibbon(vh, points, bodyWidth, bodyColor, false);
        if (coreColor.a > 0.01f && coreWidth > 0.2f)
            DrawRibbon(vh, points, coreWidth, coreColor, logicSegColors.Count > 0 && subSegLogicIndices.Count >= points.Count - 1);
    }

    void DrawRibbon(VertexHelper vh, List<Vector2> pts, float width, Color defaultCol, bool usePerSegColor)
    {
        float halfW = width * 0.5f;
        for (int i = 0; i < pts.Count - 1; i++)
        {
            Vector2 a = pts[i];
            Vector2 b = pts[i + 1];
            Vector2 d = b - a;
            float len = d.magnitude;
            if (len < 0.05f) continue;

            Color col = defaultCol;
            if (usePerSegColor && i < subSegLogicIndices.Count && logicSegColors.TryGetValue(subSegLogicIndices[i], out var segCol))
                col = segCol;

            Vector2 dir = d / len;
            Vector2 n = new Vector2(-dir.y, dir.x) * halfW;

            int v = vh.currentVertCount;
            vh.AddVert(a - n, col, Vector2.zero);
            vh.AddVert(a + n, col, Vector2.zero);
            vh.AddVert(b + n, col, Vector2.zero);
            vh.AddVert(b - n, col, Vector2.zero);
            vh.AddTriangle(v, v + 1, v + 2);
            vh.AddTriangle(v, v + 2, v + 3);

            if (i == 0) AddDisc(vh, a, halfW, col, 6);
            AddDisc(vh, b, halfW, col, 6);
        }
    }

    static void AddDisc(VertexHelper vh, Vector2 center, float radius, Color col, int segments)
    {
        int centerIdx = vh.currentVertCount;
        vh.AddVert(center, col, Vector2.zero);
        float step = Mathf.PI * 2f / segments;
        for (int i = 0; i <= segments; i++)
        {
            float ang = i * step;
            vh.AddVert(center + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * radius, col, Vector2.zero);
        }
        for (int i = 1; i <= segments; i++)
            vh.AddTriangle(centerIdx, centerIdx + i, centerIdx + i + 1);
    }
}
