using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 主画布点阵网格绘制器：单 DrawCall 渲染可吸附点阵，并按地形数据区分平地/陡坡/禁建区点色。
/// </summary>
[RequireComponent(typeof(CanvasRenderer))]
public class DotGridGraphic : MaskableGraphic
{
    public string regionId = "月之亮面";
    public float spacing = TerrainField.DefaultGridSpacing;
    public float dotRadius = 1.65f;
    public Color normalDotColor = new Color(0.86f, 0.92f, 1.0f, 0.24f);
    public Color steepDotColor = new Color(0.96f, 0.72f, 0.34f, 0.36f);
    public Color blockedDotColor = new Color(0.94f, 0.28f, 0.24f, 0.42f);
    public float minX = 0f;
    public float maxX = 1080f;
    public float minY = 173f;
    public float maxY = 1747f;
    public float intensity = 1f;

    public void SetActiveHighlight(bool active)
    {
        float target = active ? 1f : 0.28f;
        if (Mathf.Approximately(intensity, target)) return;
        intensity = target;
        SetVerticesDirty();
    }

    public void Configure(string region, float gridSpacing, Color normalCol, Color steepCol, Color blockedCol)
    {
        regionId = region;
        TerrainField.GetRegionHorizontalBounds(region, out minX, out maxX);
        spacing = Mathf.Max(16f, gridSpacing);
        normalDotColor = normalCol;
        steepDotColor = steepCol;
        blockedDotColor = blockedCol;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect r = GetPixelAdjustedRect();
        if (r.width <= 0f || r.height <= 0f) return;

        float s = Mathf.Max(16f, spacing);
        float half = Mathf.Max(0.8f, dotRadius);
        bool hasTerrain = TerrainField.HasTerrain(regionId);

        float startX = minX + s * 0.5f;
        float endX = maxX - s * 0.5f;
        float startY = Mathf.Ceil((minY + s * 0.5f) / s) * s;
        float endY = maxY - s * 0.5f;

        // 限制最大顶点数安全线，防止极端配置触发 UGUI 65535 顶点上限
        for (float y = startY; y <= endY; y += s)
        {
            for (float x = startX; x <= endX; x += s)
            {
                if (vh.currentVertCount >= 60000) return;

                Color col = normalDotColor;
                if (hasTerrain)
                {
                    byte flags = TerrainField.SampleFlags(regionId, new Vector2(x, y));
                    if ((flags & TerrainField.FlagBlocked) != 0)
                        col = blockedDotColor;
                    else if ((flags & TerrainField.FlagSteep) != 0)
                        col = steepDotColor;
                }

                if (col.a <= 0.01f) continue;
                col.a *= intensity;
                if (col.a <= 0.008f) continue;

                // 菱形点阵：仅 4 顶点 + 2 三角形，比方形更柔和精致
                int idx = vh.currentVertCount;
                vh.AddVert(new Vector3(x, y - half), col, Vector2.zero);
                vh.AddVert(new Vector3(x + half, y), col, Vector2.zero);
                vh.AddVert(new Vector3(x, y + half), col, Vector2.zero);
                vh.AddVert(new Vector3(x - half, y), col, Vector2.zero);
                vh.AddTriangle(idx, idx + 1, idx + 2);
                vh.AddTriangle(idx, idx + 2, idx + 3);
            }
        }
    }
}
