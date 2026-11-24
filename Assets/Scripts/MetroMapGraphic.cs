using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 城市星轨图背景底纹绘制器：渐变底色 + 可开关的均匀工程网格。
/// </summary>
[RequireComponent(typeof(CanvasRenderer))]
public class MetroBackdropGridGraphic : MaskableGraphic
{
    public Color topColor = new Color(0.975f, 0.978f, 0.985f, 1f);
    public Color bottomColor = new Color(0.935f, 0.945f, 0.960f, 1f);
    public bool showGrid = true;
    public Color gridColor = new Color(0.78f, 0.82f, 0.88f, 0.35f);
    public float gridSpacing = 72f;

    public void ApplyStyle(MetroStyleProfile style)
    {
        if (style == null) return;
        topColor = style.bgTopColor;
        bottomColor = style.bgBottomColor;
        showGrid = style.showGrid;
        gridColor = style.gridColor;
        gridSpacing = Mathf.Max(24f, style.gridSpacing);
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect r = GetPixelAdjustedRect();
        if (r.width <= 0f || r.height <= 0f) return;

        // 1. 渐变背景底色
        int v0 = vh.currentVertCount;
        vh.AddVert(new Vector3(r.xMin, r.yMin), bottomColor, Vector2.zero);
        vh.AddVert(new Vector3(r.xMax, r.yMin), bottomColor, Vector2.zero);
        vh.AddVert(new Vector3(r.xMax, r.yMax), topColor, Vector2.zero);
        vh.AddVert(new Vector3(r.xMin, r.yMax), topColor, Vector2.zero);
        vh.AddTriangle(v0, v0 + 1, v0 + 2);
        vh.AddTriangle(v0, v0 + 2, v0 + 3);

        // 2. 可选工程网格线
        if (!showGrid || gridColor.a <= 0.005f) return;
        float step = Mathf.Max(24f, gridSpacing);
        float halfW = 0.75f;

        for (float x = r.xMin + step * 0.5f; x < r.xMax; x += step)
        {
            AddRect(vh, x - halfW, r.yMin, x + halfW, r.yMax, gridColor);
        }
        for (float y = r.yMin + step * 0.5f; y < r.yMax; y += step)
        {
            AddRect(vh, r.xMin, y - halfW, r.xMax, y + halfW, gridColor);
        }
    }

    static void AddRect(VertexHelper vh, float x0, float y0, float x1, float y1, Color col)
    {
        int idx = vh.currentVertCount;
        vh.AddVert(new Vector3(x0, y0), col, Vector2.zero);
        vh.AddVert(new Vector3(x1, y0), col, Vector2.zero);
        vh.AddVert(new Vector3(x1, y1), col, Vector2.zero);
        vh.AddVert(new Vector3(x0, y1), col, Vector2.zero);
        vh.AddTriangle(idx, idx + 1, idx + 2);
        vh.AddTriangle(idx, idx + 2, idx + 3);
    }
}

/// <summary>
/// 城市星轨图全量线路矢量绘制器：单 DrawCall 批量绘制外描边与彩色线网。
/// </summary>
[RequireComponent(typeof(CanvasRenderer))]
public class MetroMapGraphic : MaskableGraphic
{
    readonly List<MetroPolylineLayout> polylines = new List<MetroPolylineLayout>();
    MetroStyleProfile style = MetroStyleProfile.CreatePreset(MetroPresetTheme.MinimalWhite);

    public void SetData(List<MetroPolylineLayout> lines, MetroStyleProfile profile)
    {
        polylines.Clear();
        if (lines != null) polylines.AddRange(lines);
        if (profile != null) style = profile;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if (polylines.Count == 0 || style == null) return;

        Rect r = GetPixelAdjustedRect();
        Vector2 origin = new Vector2(r.xMin, r.yMin);
        float w = Mathf.Max(2f, style.lineWidth);

        // 先画外描边层（Halo），保证交叉线路清晰分离
        if (style.drawLineHalo && style.haloColor.a > 0.01f)
        {
            float haloW = w + 4.5f;
            for (int i = 0; i < polylines.Count; i++)
            {
                DrawPolyline(vh, polylines[i].points, origin, haloW, style.haloColor);
            }
        }

        // 再画主体色带层
        for (int i = 0; i < polylines.Count; i++)
        {
            DrawPolyline(vh, polylines[i].points, origin, w, polylines[i].color);
        }
    }

    static void DrawPolyline(VertexHelper vh, List<Vector2> pts, Vector2 origin, float width, Color col)
    {
        if (pts == null || pts.Count < 2 || width <= 0.1f) return;
        float halfW = width * 0.5f;

        for (int i = 0; i < pts.Count - 1; i++)
        {
            Vector2 a = origin + pts[i];
            Vector2 b = origin + pts[i + 1];
            Vector2 d = b - a;
            float len = d.magnitude;
            if (len < 0.05f) continue;

            Vector2 dir = d / len;
            Vector2 n = new Vector2(-dir.y, dir.x) * halfW;

            int v = vh.currentVertCount;
            vh.AddVert(a - n, col, Vector2.zero);
            vh.AddVert(a + n, col, Vector2.zero);
            vh.AddVert(b + n, col, Vector2.zero);
            vh.AddVert(b - n, col, Vector2.zero);
            vh.AddTriangle(v, v + 1, v + 2);
            vh.AddTriangle(v, v + 2, v + 3);

            // 在连接点补圆帽，确保平滑无缺口
            if (i == 0) AddDisc(vh, a, halfW, col, 8);
            AddDisc(vh, b, halfW, col, 8);
        }
    }

    static void AddDisc(VertexHelper vh, Vector2 center, float radius, Color col, int segments)
    {
        if (radius <= 0.2f || segments < 4) return;
        int centerIdx = vh.currentVertCount;
        vh.AddVert(center, col, Vector2.zero);
        float step = Mathf.PI * 2f / segments;
        for (int i = 0; i <= segments; i++)
        {
            float ang = i * step;
            Vector2 p = center + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * radius;
            vh.AddVert(p, col, Vector2.zero);
        }
        for (int i = 1; i <= segments; i++)
        {
            vh.AddTriangle(centerIdx, centerIdx + i, centerIdx + i + 1);
        }
    }
}
