using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 地形场、点阵画布、右下角自动寻路开关与平滑折线拐点（Waypoints）辅助模块。
/// </summary>
public partial class DemoBootstrap
{
    [Serializable]
    public class SegmentWaypoints
    {
        public List<Vector2> pts = new List<Vector2>();
    }

    readonly List<List<Vector2>> draftSegWaypoints = new List<List<Vector2>>();
    readonly Dictionary<string, GameObject> regionHazardOverlays = new Dictionary<string, GameObject>(StringComparer.Ordinal);
    bool hazardOverlayHighlighted;
    LinePolylineGraphic activePreviewPoly;
    readonly List<Vector2> scratchControlPts = new List<Vector2>(32);
    readonly List<int> scratchCtrlSegIndices = new List<int>(32);
    readonly List<Vector2> scratchPolyline = new List<Vector2>(128);
    readonly List<int> scratchSubSegIndices = new List<int>(128);
    readonly List<Vector2> scratchAutoWaypoints = new List<Vector2>(16);

    // 拖拽期 A* 路径缓存，避免鼠标同一格内抖动时重复寻路
    string cachedAutoRegion = "";
    Vector2 cachedAutoFrom = new Vector2(float.NaN, float.NaN);
    Vector2 cachedAutoToGrid = new Vector2(float.NaN, float.NaN);
    bool cachedAutoFound = true;
    readonly List<Vector2> cachedAutoWaypoints = new List<Vector2>(16);

    void InvalidateAutoRouteCache()
    {
        cachedAutoRegion = "";
        cachedAutoFrom = new Vector2(float.NaN, float.NaN);
        cachedAutoToGrid = new Vector2(float.NaN, float.NaN);
        cachedAutoWaypoints.Clear();
    }

    void SetHazardOverlayHighlight(bool active)
    {
        hazardOverlayHighlighted = active;
        foreach (var kv in regionHazardOverlays)
        {
            if (kv.Value) kv.Value.SetActive(active && kv.Key == currentRegion);
        }
    }

    void SyncTerrainTechState()
    {
        bool canCross = techDone != null && techDone.Contains("era20_steel_bridge");
        bool reducedCross = techDone != null && techDone.Contains("era20_bridge_big");
        bool reducedSlope = techDone != null && techDone.Contains("era20_concrete_sleeper");

        TerrainField.CanCrossHazard = canCross;
        TerrainField.HazardCrossPenaltyBonus = reducedCross ? 0.75f : 1.50f;
        TerrainField.TerrainPenaltyScale = reducedSlope ? 0.65f : 1.00f;
    }

    void SyncTerrainAndGridForRegion(string regionId)
    {
        if (string.IsNullOrEmpty(regionId)) return;
        TerrainField.PreloadRegion(regionId);
        SyncTerrainTechState();
        InvalidateAutoRouteCache();
        foreach (var kv in regionHazardOverlays)
        {
            if (kv.Value) kv.Value.SetActive(hazardOverlayHighlighted && kv.Key == regionId);
        }
    }

    void ClearDraftWaypoints()
    {
        draftSegWaypoints.Clear();
        InvalidateAutoRouteCache();
        ClearAutoPreviewSegs();
    }

    void ClearAutoPreviewSegs()
    {
        if (activePreviewPoly && activePreviewPoly.gameObject.activeSelf)
            activePreviewPoly.gameObject.SetActive(false);
    }

    void EnsureActivePreviewPoly()
    {
        if (activePreviewPoly || !previewLayer) return;
        var go = Rect("ActivePreviewPoly", previewLayer, 0, 0, W, H);
        activePreviewPoly = go.AddComponent<LinePolylineGraphic>();
        activePreviewPoly.raycastTarget = false;
        go.SetActive(false);
    }

    bool GetCachedAutoRouteWaypoints(Vector2 fromPos, Vector2 toPos, List<Vector2> outWaypoints)
    {
        outWaypoints.Clear();

        Vector2 fromSnapped = TerrainField.SnapToGrid(fromPos, currentRegion);
        Vector2 toSnapped = TerrainField.SnapToGrid(toPos, currentRegion);
        if (cachedAutoRegion == currentRegion &&
            (cachedAutoFrom - fromSnapped).sqrMagnitude < 1f &&
            (cachedAutoToGrid - toSnapped).sqrMagnitude < 1f)
        {
            outWaypoints.AddRange(cachedAutoWaypoints);
            return cachedAutoFound;
        }

        cachedAutoRegion = currentRegion;
        cachedAutoFrom = fromSnapped;
        cachedAutoToGrid = toSnapped;
        cachedAutoFound = TerrainField.FindShortestGridWaypoints(currentRegion, fromPos, toPos, cachedAutoWaypoints);
        outWaypoints.AddRange(cachedAutoWaypoints);
        return cachedAutoFound;
    }

    bool UpdateActiveDrawPreview(Vector2 p1, Vector2 end, Color col)
    {
        EnsureActivePreviewPoly();
        if (!activePreviewPoly || draft.Count == 0) return true;

        if (IsInEventSandbox())
        {
            var streetPath = FindSandboxStreetPathToPoint(activeSandboxEventId, dragFrom, end);
            if (streetPath.Count < 2) { activePreviewPoly.gameObject.SetActive(false); return true; }
            activePreviewPoly.gameObject.SetActive(true);
            activePreviewPoly.SetPolyline(streetPath, Vector2.zero, col, new Color(1f, 1f, 1f, 0.42f), 14f * overlayZoom, 2.5f * overlayZoom);
            return true;
        }

        scratchAutoWaypoints.Clear();
        bool ok = true;
        ok = GetCachedAutoRouteWaypoints(p1, end, scratchAutoWaypoints);

        scratchControlPts.Clear();
        scratchCtrlSegIndices.Clear();
        // 纳入同区前序草稿控制点共同计算切线，确保松手合并整线后拐点零漂移
        int spanFirstIdx = draft.Count - 1;
        while (spanFirstIdx > 0 && SegmentRegion(draft[spanFirstIdx - 1], draft[spanFirstIdx]) == currentRegion)
            spanFirstIdx--;
        scratchControlPts.Add(Pos(draft[spanFirstIdx]));
        for (int d = spanFirstIdx; d < draft.Count - 1; d++)
        {
            if (d < draftSegWaypoints.Count && draftSegWaypoints[d] != null)
            {
                var wp = draftSegWaypoints[d];
                for (int w = 0; w < wp.Count; w++)
                {
                    scratchControlPts.Add(wp[w]);
                    scratchCtrlSegIndices.Add(0);
                }
            }
            scratchControlPts.Add(Pos(draft[d + 1]));
            scratchCtrlSegIndices.Add(0);
        }

        if (scratchAutoWaypoints.Count > 0)
        {
            for (int i = 0; i < scratchAutoWaypoints.Count; i++)
            {
                scratchControlPts.Add(scratchAutoWaypoints[i]);
                scratchCtrlSegIndices.Add(1);
            }
        }
        scratchControlPts.Add(end);
        scratchCtrlSegIndices.Add(1);

        TerrainField.SampleCatmullRomPolyline(scratchControlPts, false, scratchPolyline, scratchSubSegIndices, scratchCtrlSegIndices);
        // 仅截取当前正在拖拽的这一段（logicSeg == 1）用于预览绘制与地形碰撞检测
        int curStartPt = 0;
        if (spanFirstIdx < draft.Count - 1)
        {
            curStartPt = scratchPolyline.Count - 1;
            for (int k = 0; k < scratchSubSegIndices.Count; k++)
            {
                if (scratchSubSegIndices[k] == 1) { curStartPt = k; break; }
            }
        }
        if (curStartPt > 0 && curStartPt < scratchPolyline.Count)
            scratchPolyline.RemoveRange(0, curStartPt);

        TerrainField.EvaluatePolyline(currentRegion, scratchPolyline, out var tr);
        bool blockedNow = !ok || (!TerrainField.CanCrossHazard && tr.blocked);
        Color previewCol = blockedNow ? new Color(0.96f, 0.24f, 0.22f, 0.95f) : col;

        if (!activePreviewPoly.gameObject.activeSelf) activePreviewPoly.gameObject.SetActive(true);
        activePreviewPoly.SetPolyline(scratchPolyline, Vector2.zero, previewCol, new Color(1f, 1f, 1f, 0.42f), 14.0f * overlayZoom, 2.5f * overlayZoom);
        return !blockedNow;
    }

    /// <summary>
    /// 展开已建线路某一段的原始控制点列（含起终点）。零 GC。
    /// </summary>
    void FillLineSegmentControlPoints(Line line, int segIdx, string region, List<Vector2> outPts)
    {
        outPts.Clear();
        if (line == null || line.st == null || segIdx < 0 || segIdx >= line.st.Count - 1) return;
        outPts.Add(PosInRegion(line.st[segIdx], region));
        if (line.segWaypoints != null && segIdx < line.segWaypoints.Count && line.segWaypoints[segIdx] != null)
        {
            var wp = line.segWaypoints[segIdx].pts;
            if (wp != null)
            {
                for (int i = 0; i < wp.Count; i++) outPts.Add(wp[i]);
            }
        }
        outPts.Add(PosInRegion(line.st[segIdx + 1], region));
    }

    /// <summary>
    /// 展开已建线路某一段的平滑折线（含起终点）。零 GC 写入 outPts。
    /// </summary>
    void FillLineSegmentPolyline(Line line, int segIdx, string region, List<Vector2> outPts)
    {
        FillLineSegmentControlPoints(line, segIdx, region, scratchControlPts);
        TerrainField.SampleCatmullRomPolyline(scratchControlPts, false, outPts);
    }


    /// <summary>
    /// 同一区域的连续控制点共同平滑，并保留每个子段对应的逻辑段索引。
    /// </summary>
    void FillRegionSpanPolyline(
        Line line,
        int segStart,
        int segEndInclusive,
        string region,
        List<Vector2> outPts,
        List<int> outSubSegIndices,
        int overrideSegIdx = -1,
        IReadOnlyList<Vector2> overrideWaypoints = null)
    {
        outPts.Clear();
        if (outSubSegIndices != null) outSubSegIndices.Clear();
        if (line == null || line.st == null || segStart < 0 || segEndInclusive >= line.st.Count - 1 || segStart > segEndInclusive)
            return;

        if (IsInEventSandbox())
        {
            for (int s = segStart; s <= segEndInclusive; s++)
            {
                var streetPath = FindSandboxStreetPath(activeSandboxEventId, line.st[s], line.st[s + 1]);
                if (streetPath.Count < 2) continue;
                for (int p = outPts.Count == 0 ? 0 : 1; p < streetPath.Count; p++)
                {
                    if (outPts.Count > 0 && outSubSegIndices != null) outSubSegIndices.Add(s);
                    outPts.Add(streetPath[p]);
                }
            }
            return;
        }

        scratchControlPts.Clear();
        scratchCtrlSegIndices.Clear();
        scratchControlPts.Add(PosInRegion(line.st[segStart], region));
        for (int s = segStart; s <= segEndInclusive; s++)
        {
            IReadOnlyList<Vector2> wp = s == overrideSegIdx && overrideWaypoints != null
                ? overrideWaypoints
                : (line.segWaypoints != null && s < line.segWaypoints.Count && line.segWaypoints[s] != null ? line.segWaypoints[s].pts : null);
            if (wp != null)
                for (int i = 0; i < wp.Count; i++) { scratchControlPts.Add(wp[i]); scratchCtrlSegIndices.Add(s); }
            scratchControlPts.Add(PosInRegion(line.st[s + 1], region));
            scratchCtrlSegIndices.Add(s);
        }
        bool loop = segStart == 0 && segEndInclusive == line.st.Count - 2 && IsLoop(line.st);
        TerrainField.SampleCatmullRomPolyline(scratchControlPts, loop, outPts, outSubSegIndices, scratchCtrlSegIndices);
    }

    /// <summary>
    /// 展开草稿某一段的平滑折线（含起终点）。零 GC 写入 outPts。
    /// </summary>
    void FillDraftSegmentPolyline(List<int> path, int segIdx, List<Vector2> outPts)
    {
        if (IsInEventSandbox())
        {
            outPts.Clear();
            if (path != null && segIdx >= 0 && segIdx < path.Count - 1)
                outPts.AddRange(FindSandboxStreetPath(activeSandboxEventId, path[segIdx], path[segIdx + 1]));
            return;
        }
        scratchControlPts.Clear();
        if (path == null || segIdx < 0 || segIdx >= path.Count - 1) { outPts.Clear(); return; }
        scratchControlPts.Add(Pos(path[segIdx]));
        if (segIdx < draftSegWaypoints.Count && draftSegWaypoints[segIdx] != null)
        {
            var wp = draftSegWaypoints[segIdx];
            for (int i = 0; i < wp.Count; i++) scratchControlPts.Add(wp[i]);
        }
        scratchControlPts.Add(Pos(path[segIdx + 1]));
        TerrainField.SampleCatmullRomPolyline(scratchControlPts, false, outPts);
    }

    void FillWholeDraftPolyline(List<int> path, List<Vector2> outPts)
    {
        if (IsInEventSandbox())
        {
            outPts.Clear();
            if (path == null) return;
            for (int s = 0; s + 1 < path.Count; s++)
            {
                var streetPath = FindSandboxStreetPath(activeSandboxEventId, path[s], path[s + 1]);
                if (streetPath.Count < 2) continue;
                for (int p = outPts.Count == 0 ? 0 : 1; p < streetPath.Count; p++) outPts.Add(streetPath[p]);
            }
            return;
        }
        scratchControlPts.Clear();
        if (path == null || path.Count < 2) { outPts.Clear(); return; }
        scratchControlPts.Add(Pos(path[0]));
        for (int s = 0; s < path.Count - 1; s++)
        {
            if (s < draftSegWaypoints.Count && draftSegWaypoints[s] != null)
            {
                var wp = draftSegWaypoints[s];
                for (int i = 0; i < wp.Count; i++) scratchControlPts.Add(wp[i]);
            }
            scratchControlPts.Add(Pos(path[s + 1]));
        }
        TerrainField.SampleCatmullRomPolyline(scratchControlPts, IsLoop(path), outPts);
    }

    int EvaluatePolylineTrackCost(string regionId, IReadOnlyList<Vector2> pts, float trackFactor, out TerrainField.SegmentTerrainResult tr)
    {
        TerrainField.EvaluatePolyline(regionId, pts, out tr);
        float mult = 1f + tr.penaltyRatio;
        return Mathf.RoundToInt((30f + tr.distance * 0.75f) * mult * trackFactor);
    }

    int EvaluateStationPairCost(int a, int b, float trackFactor, out TerrainField.SegmentTerrainResult tr)
    {
        scratchPolyline.Clear();
        string reg = SegmentRegion(a, b) ?? currentRegion;
        Vector2 from = PosInRegion(a, reg), to = PosInRegion(b, reg);
        scratchControlPts.Clear();
        scratchControlPts.Add(from);
        TerrainField.FindShortestGridWaypoints(reg, from, to, scratchAutoWaypoints);
        scratchControlPts.AddRange(scratchAutoWaypoints);
        scratchControlPts.Add(to);
        TerrainField.SampleCatmullRomPolyline(scratchControlPts, false, scratchPolyline);
        return EvaluatePolylineTrackCost(reg, scratchPolyline, trackFactor, out tr);
    }

    int EvaluateLineSegmentCost(Line line, int segIdx, float trackFactor, out TerrainField.SegmentTerrainResult tr)
    {
        tr = default;
        if (IsInEventSandbox()) return 0;
        if (line == null || line.st == null || segIdx < 0 || segIdx >= line.st.Count - 1) return 0;
        string reg = SegmentRegion(line.st[segIdx], line.st[segIdx + 1]) ?? currentRegion;
        FillLineSegmentPolyline(line, segIdx, reg, scratchPolyline);
        return EvaluatePolylineTrackCost(reg, scratchPolyline, trackFactor, out tr);
    }

    static List<SegmentWaypoints> CloneWaypointsList(List<SegmentWaypoints> src)
    {
        var dst = new List<SegmentWaypoints>();
        if (src == null) return dst;
        for (int i = 0; i < src.Count; i++)
        {
            var seg = new SegmentWaypoints();
            if (src[i] != null && src[i].pts != null) seg.pts.AddRange(src[i].pts);
            dst.Add(seg);
        }
        return dst;
    }

    static void SyncLineWaypointsCount(Line line)
    {
        if (line == null) return;
        if (line.segWaypoints == null) line.segWaypoints = new List<SegmentWaypoints>();
        int need = line.st != null ? Mathf.Max(0, line.st.Count - 1) : 0;
        while (line.segWaypoints.Count < need) line.segWaypoints.Add(new SegmentWaypoints());
        while (line.segWaypoints.Count > need) line.segWaypoints.RemoveAt(line.segWaypoints.Count - 1);
    }

    GameObject AddSegPos(Vector2 p1, Vector2 p2, Color c, float width, RectTransform layer, Vector2 offset = default, Sprite strokeSprite = null)
    {
        p1 += offset; p2 += offset;
        var d = p2 - p1;
        var go = Rect("Seg", layer, p1.x, p1.y, d.magnitude, width);
        var rt = go.GetComponent<RectTransform>(); rt.pivot = new Vector2(0, 0.5f);
        var im = Img(go, c, strokeSprite ? strokeSprite : spriteWhite);
        if (strokeSprite != null) { im.type = Image.Type.Sliced; im.raycastTarget = false; }
        rt.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
        zoomRects.Add(new ZoomRect(rt, width));
        return go;
    }

    /// <summary>
    /// 屏幕坐标预检：命中平滑曲线时回传最近线路与分段索引，供底图手势桥接做长按改线判定。
    /// 容差随缩放自适应（32f / zoom），放大后手感更精确。
    /// </summary>
    public bool TryFindLineHitAtScreen(Vector2 screen, out int lineId, out int seg)
    {
        lineId = -1; seg = -1;
        if (!mapRect) return false;

        Vector2 local;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(mapRect, screen, null, out local)) return false;

        float tolerance = 32f / Mathf.Max(0.45f, mapRect.localScale.x);
        var hits = FindLineHits(local, tolerance);
        if (hits == null || hits.Count == 0) return false;

        lineId = hits[0].line != null ? hits[0].line.id : -1;
        seg = hits[0].seg;
        return lineId >= 0;
    }

    /// <summary>
    /// 给底图命中区挂载统一线路手势桥接（长按改线 / 拖动平移 / 轻点查看），重复调用安全。
    /// </summary>
    public void AttachMapLineBridge(GameObject hitArea)
    {
        if (!hitArea) return;
        var bridge = hitArea.GetComponent<MapLinePointerBridge>();
        if (!bridge) bridge = hitArea.AddComponent<MapLinePointerBridge>();
        if (bridge) bridge.boot = this;
    }
}

/// <summary>
/// 底图统一线路手势桥接：单指短按点选线路，长按 0.22s 进入平滑改线拖拽，
/// 拖离阈值或非线路区域则退化为地图平移，避免与既有单线 LineTapPress 冲突。
/// </summary>
public class MapLinePointerBridge : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public DemoBootstrap boot;
    public int pendingLineId = -1;
    public int pendingSeg = -1;

    const float HoldDelay = 0.22f;
    const float HoldSlop = 14f;

    Vector2 startPos;
    float elapsed;
    bool pressing, inserting, panning, suppressClick;

    public void OnPointerDown(PointerEventData e)
    {
        startPos = e.position;
        elapsed = 0f;
        pressing = true; inserting = false; panning = false; suppressClick = false;
        pendingLineId = -1; pendingSeg = -1;
        if (boot) boot.TryFindLineHitAtScreen(e.position, out pendingLineId, out pendingSeg);
    }

    void Update()
    {
        if (!pressing || inserting || panning || pendingLineId < 0) return;
        if (Input.touchCount >= 2) { pressing = false; return; }

        elapsed += Time.unscaledDeltaTime;
        if (elapsed < HoldDelay) return;

        if (boot) boot.BeginLineInsert(pendingLineId, pendingSeg, startPos);
        inserting = true; suppressClick = true; pressing = false;
    }

    public void OnBeginDrag(PointerEventData e)
    {
        if (inserting)
        {
            if (boot) boot.UpdateLineInsert(e.position);
            return;
        }

        // 已按在曲线上且位移仍在阈值内：继续等待长按，不抢成平移
        if (pressing && pendingLineId >= 0 && Vector2.Distance(e.position, startPos) <= HoldSlop) return;

        pressing = false;
        panning = true;
        if (boot) boot.ForwardLinePanStart(e);
    }

    public void OnDrag(PointerEventData e)
    {
        if (!boot) return;
        if (inserting) boot.UpdateLineInsert(e.position);
        else if (panning) boot.ForwardLinePan(e);
    }

    public void OnPointerUp(PointerEventData e)
    {
        pressing = false;
        if (inserting && boot) boot.EndLineInsert(e.position);
        inserting = false;
    }

    public void OnEndDrag(PointerEventData e)
    {
        if (panning && boot) boot.ForwardLinePanEnd(e);
        panning = false;
    }

    public void OnPointerClick(PointerEventData e)
    {
        if (!suppressClick && !inserting && !panning && pendingLineId >= 0 && boot)
            boot.OnLineTap(e.position);
        suppressClick = false;
    }
}
