using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MapBackdropGraphic : MaskableGraphic
{
    public Color top = new Color(0.74f, 0.86f, 0.87f, 1f);
    public Color bottom = new Color(0.52f, 0.70f, 0.77f, 1f);

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        var r = GetPixelAdjustedRect();
        vh.AddVert(new Vector3(r.xMin, r.yMin), bottom, Vector2.zero);
        vh.AddVert(new Vector3(r.xMax, r.yMin), bottom, Vector2.zero);
        vh.AddVert(new Vector3(r.xMax, r.yMax), top, Vector2.zero);
        vh.AddVert(new Vector3(r.xMin, r.yMax), top, Vector2.zero);
        vh.AddTriangle(0, 1, 2);
        vh.AddTriangle(0, 2, 3);
    }
}
