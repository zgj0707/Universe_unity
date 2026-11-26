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

    DotGridGraphic dotGridGraphic;
    bool dotGridHighlighted;
    readonly Dictionary<string, GameObject> regionHazardOverlays = new Dictionary<string, GameObject>(StringComparer.Ordinal);
    bool autoRouteEnabled = true; // 默认开启：自动寻找两站点间最近可行路线
    GameObject autoRouteBtn;
    Image autoRouteBtnBg;
    Text autoRouteBtnTxt;

    readonly List<List<Vector2>> draftSegWaypoints = new List<List<Vector2>>();
    readonly List<Vector2> activeSegWaypoints = new List<Vector2>();
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

    void InitDotGridLayer()
    {
        if (!mapRect || dotGridGraphic != null) return;
        var go = Rect("DotGrid", mapRect, 0, 0, W, H);
        dotGridGraphic = go.AddComponent<DotGridGraphic>();
        dotGridGraphic.raycastTarget = false;
        dotGridGraphic.dotRadius = 2.45f;
        dotGridGraphic.SetActiveHighlight(false);
        if (lineLayer) go.transform.SetSiblingIndex(lineLayer.GetSiblingIndex());
        SyncTerrainAndGridForRegion(currentRegion);
        go.SetActive(false);
    }

    void SetDotGridHighlight(bool active)
    {
        dotGridHighlighted = active;
        if (dotGridGraphic)
        {
            dotGridGraphic.gameObject.SetActive(active);
            if (active) dotGridGraphic.SetActiveHighlight(true);
        }
        foreach (var kv in regionHazardOverlays)
        {
            if (kv.Value) kv.Value.SetActive(active && kv.Key == currentRegion);
        }
    }

    void BuildAutoRouteToggle()
    {
        if (!root || autoRouteBtn != null) return;
        GetSafeMarginsInRoot(out float safeL, out _, out _, out float safeB);
        const float size = 78f;
        float x = safeL + 16f;
        float y = safeB + 190f;
        autoRouteBtn = Rect("AutoRouteToggle", root, x, y, size, size);
        autoRouteBtnBg = Img(autoRouteBtn, Indigo, spriteRound);
        SoftShadow(autoRouteBtn);
        autoRouteBtnTxt = Txt(autoRouteBtn.transform, "绕障\n开", 20, Color.white, 6, 6, size - 12f, size - 12f, TextAnchor.MiddleCenter);
        autoRouteBtnTxt.raycastTarget = false;
        var btn = autoRouteBtn.AddComponent<Button>();
        btn.onClick.AddListener(ToggleAutoRoute);
        RefreshAutoRouteToggleVisual();
    }

    void ToggleAutoRoute()
    {
        autoRouteEnabled = !autoRouteEnabled;
        InvalidateAutoRouteCache();
        RefreshAutoRouteToggleVisual();
        PlayTone(autoRouteEnabled);
        Toast(autoRouteEnabled ? "已开启：自动寻找最近可行路线" : "已关闭：纯手动点阵吸附开线");
    }

    void RefreshAutoRouteToggleVisual()
    {
        if (!autoRouteBtnBg || !autoRouteBtnTxt) return;
        autoRouteBtnBg.color = autoRouteEnabled ? Indigo : new Color(0.92f, 0.90f, 0.86f, 0.95f);
        autoRouteBtnTxt.text = autoRouteEnabled ? "绕障\n开" : "绕障\n关";
        autoRouteBtnTxt.color = autoRouteEnabled ? Color.white : Ink;
    }

    void InvalidateAutoRouteCache()
    {
        cachedAutoRegion = "";
        cachedAutoFrom = new Vector2(float.NaN, float.NaN);
        cachedAutoToGrid = new Vector2(float.NaN, float.NaN);
        cachedAutoWaypoints.Clear();
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
        if (!dotGridGraphic) return;

        bool darkTheme = currentTheme == 1;
        Color normalCol = darkTheme
            ? new Color(0.75f, 0.94f, 1.00f, 0.72f)
            : new Color(0.96f, 0.98f, 1.00f, 0.76f);
        Color steepCol = new Color(1.00f, 0.74f, 0.24f, 0.88f);
        Color blockedCol = new Color(1.00f, 0.30f, 0.26f, 0.95f);
        dotGridGraphic.Configure(regionId, TerrainField.DefaultGridSpacing, normalCol, steepCol, blockedCol);
        dotGridGraphic.gameObject.SetActive(dotGridHighlighted);
        if (dotGridHighlighted) dotGridGraphic.SetActiveHighlight(true);
        foreach (var kv in regionHazardOverlays)
        {
            if (kv.Value) kv.Value.SetActive(dotGridHighlighted && kv.Key == regionId);
        }
    }

    void ClearDraftWaypoints()
    {
        draftSegWaypoints.Clear();
        activeSegWaypoints.Clear();
        InvalidateAutoRouteCache();
        ClearActiveWaypointSegs();
        ClearAutoPreviewSegs();
    }

    void ClearActiveWaypointSegs()
    {
        if (activePreviewPoly && activePreviewPoly.gameObject.activeSelf)
            activePreviewPoly.gameObject.SetActive(false);
    }

    void ClearAutoPreviewSegs()
    {
        if (activePreviewPoly && activePreviewPoly.gameObject.activeSelf)
            activePreviewPoly.gameObject.SetActive(false);
    }

    void RedrawActiveWaypointSegs()
    {
        // 统一由 UpdateActiveDrawPreview 每帧合并绘制，无需独立对象
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
        if (!autoRouteEnabled) return !TerrainField.IsSegmentBlocked(currentRegion, fromPos, toPos);

        Vector2 toSnapped = TerrainField.SnapToGrid(toPos, currentRegion);
        if (cachedAutoRegion == currentRegion &&
            (cachedAutoFrom - fromPos).sqrMagnitude < 1f &&
            (cachedAutoToGrid - toSnapped).sqrMagnitude < 1f)
        {
            outWaypoints.AddRange(cachedAutoWaypoints);
            return cachedAutoFound;
        }

        cachedAutoRegion = currentRegion;
        cachedAutoFrom = fromPos;
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
        if (autoRouteEnabled && Vector2.Distance(p1, end) >= 48f)
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

        Vector2 segStart = Pos(draft[draft.Count - 1]);
        for (int i = 0; i < activeSegWaypoints.Count; i++)
        {
            scratchControlPts.Add(activeSegWaypoints[i]);
            scratchCtrlSegIndices.Add(1);
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

    readonly List<Vector2> scratchSegPolyTmp = new List<Vector2>(64);

    /// <summary>
    /// 逐段独立展开同一区域内连续多段的平滑折线：每两站之间仅根据本段锚点独立平滑，新增或改动某段绝不影响原有段。零 GC。
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

        for (int s = segStart; s <= segEndInclusive; s++)
        {
            scratchControlPts.Clear();
            scratchControlPts.Add(PosInRegion(line.st[s], region));

            IReadOnlyList<Vector2> wp = null;
            if (s == overrideSegIdx && overrideWaypoints != null)
                wp = overrideWaypoints;
            else if (line.segWaypoints != null && s < line.segWaypoints.Count && line.segWaypoints[s] != null)
                wp = line.segWaypoints[s].pts;

            if (wp != null)
                for (int i = 0; i < wp.Count; i++) scratchControlPts.Add(wp[i]);

            scratchControlPts.Add(PosInRegion(line.st[s + 1], region));
            TerrainField.SampleCatmullRomPolyline(scratchControlPts, false, scratchSegPolyTmp);
            if (scratchSegPolyTmp.Count < 2) continue;

            int startIdx = outPts.Count == 0 ? 0 : 1;
            for (int p = startIdx; p < scratchSegPolyTmp.Count; p++)
            {
                if (outPts.Count > 0 && outSubSegIndices != null)
                    outSubSegIndices.Add(s);
                outPts.Add(scratchSegPolyTmp[p]);
            }
        }
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
        scratchPolyline.Add(PosInRegion(a, reg));
        scratchPolyline.Add(PosInRegion(b, reg));
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

    bool TryAddActiveWaypoint(Vector2 snappedPos)
    {
        if (IsInEventSandbox()) return false;
        if (!dragging || draft.Count == 0 || IsLoop(draft)) return false;
        if (TerrainField.IsBlocked(currentRegion, snappedPos))
        {
            ShowConnectHint("该点位于裂谷/绝壁禁建区，请选择周围点阵");
            return false;
        }
        Vector2 prev = activeSegWaypoints.Count > 0
            ? activeSegWaypoints[activeSegWaypoints.Count - 1]
            : Pos(draft[draft.Count - 1]);
        if (Vector2.Distance(prev, snappedPos) < 10f) return false;

        if (!autoRouteEnabled && TerrainField.IsSegmentBlocked(currentRegion, prev, snappedPos))
        {
            ShowConnectHint("前方穿越禁建天堑，请先在绕行侧点阵落拐点");
            return false;
        }
        if (activeSegWaypoints.Count >= 16)
        {
            ShowConnectHint("单段最多添加 16 个拐点");
            return false;
        }

        if (autoRouteEnabled && TerrainField.IsSegmentBlocked(currentRegion, prev, snappedPos))
        {
            if (!TerrainField.FindShortestGridWaypoints(currentRegion, prev, snappedPos, scratchAutoWaypoints))
            {
                ShowConnectHint("无法找到绕开禁建区的可行路径");
                return false;
            }
            for (int i = 0; i < scratchAutoWaypoints.Count && activeSegWaypoints.Count < 16; i++)
                activeSegWaypoints.Add(scratchAutoWaypoints[i]);
        }

        activeSegWaypoints.Add(snappedPos);
        InvalidateAutoRouteCache();
        RedrawActiveWaypointSegs();
        TriggerHaptic(8);
        PlaySnapSfx();
        return true;
    }

    bool TryPopActiveWaypoint()
    {
        if (IsInEventSandbox()) return false;
        if (activeSegWaypoints.Count == 0) return false;
        activeSegWaypoints.RemoveAt(activeSegWaypoints.Count - 1);
        InvalidateAutoRouteCache();
        RedrawActiveWaypointSegs();
        TriggerHaptic(6);
        return true;
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
