using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 全星域通用地形场采样与路径造价积分引擎（零 GC 运行时查询）。
/// 支持 TRN1 紧凑二进制格式：R=高程(0..255), G=标志位(bit0 陡坡惩罚, bit1 硬性禁建区)。
/// </summary>
public static class TerrainField
{
    public const byte FlagSteep = 1;
    public const byte FlagBlocked = 2;

    public const float CanvasWidth = 1080f;
    public const float CanvasHeight = 1920f;
    public const float DefaultGridSpacing = 27f; // 1080 / 40 = 27px，整除画布宽度

    public struct SegmentTerrainResult
    {
        public float distance;
        public float climbSum;
        public float steepRatio;
        public float hazardRatio;
        public float penaltyRatio; // 综合造价惩罚倍率加成（如 0.35 表示额外 +35%）
        public bool blocked;
        public bool crossedHazard;
        public Vector2 firstBlockedPos;
    }

    // 科技树实时调节参数（由 DemoBootstrap.SyncTerrainTechState 同步）
    public static bool CanCrossHazard = false;
    public static float TerrainPenaltyScale = 1f;
    public static float HazardCrossPenaltyBonus = 1.50f;

    sealed class RegionData
    {
        public int width;
        public int height;
        public float canvasMinX;
        public float canvasWidth;
        public byte[] interleaved; // [elev, flags] * width * height
    }

    static readonly Dictionary<string, RegionData> cache = new Dictionary<string, RegionData>(StringComparer.Ordinal);
    static readonly Dictionary<string, string> defaultResourcePaths = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        { "月之亮面", "MapData/moon_nearside_terrain" },
        { "月背", "MapData/moon_farside_terrain" },
        { "火星·西半球", "MapData/mars_west_terrain" },
        { "火星·东半球", "MapData/mars_east_terrain" },
    };

    public static bool HasTerrain(string regionId)
    {
        return TryGetRegion(regionId, out _);
    }

    public static void GetRegionHorizontalBounds(string regionId, out float minX, out float maxX)
    {
        if (regionId == "火星·西半球" || regionId == "火星·东半球")
        {
            minX = -0.70f * CanvasWidth;
            maxX = minX + 2.40f * CanvasWidth;
            return;
        }
        minX = 0f;
        maxX = CanvasWidth;
    }

    public static void PreloadRegion(string regionId, string resourcePath = null)
    {
        if (string.IsNullOrEmpty(regionId)) return;
        if (cache.ContainsKey(regionId)) return;

        if (string.IsNullOrEmpty(resourcePath) && !defaultResourcePaths.TryGetValue(regionId, out resourcePath))
            return;

        var asset = Resources.Load<TextAsset>(resourcePath);
        if (!asset || asset.bytes == null || asset.bytes.Length < 8)
        {
            cache[regionId] = null;
            return;
        }

        byte[] raw = asset.bytes;
        if (raw[0] != (byte)'T' || raw[1] != (byte)'R' || raw[2] != (byte)'N' || raw[3] != (byte)'1')
        {
            Debug.LogWarning($"[TerrainField] Invalid TRN1 header for {regionId}");
            cache[regionId] = null;
            return;
        }

        int w = raw[4] | (raw[5] << 8);
        int h = raw[6] | (raw[7] << 8);
        int expected = 8 + w * h * 2;
        if (w <= 0 || h <= 0 || raw.Length < expected)
        {
            Debug.LogWarning($"[TerrainField] Truncated TRN1 payload for {regionId}");
            cache[regionId] = null;
            return;
        }

        GetRegionHorizontalBounds(regionId, out float minX, out float maxX);
        var data = new byte[w * h * 2];
        Buffer.BlockCopy(raw, 8, data, 0, data.Length);
        cache[regionId] = new RegionData
        {
            width = w,
            height = h,
            canvasMinX = minX,
            canvasWidth = maxX - minX,
            interleaved = data
        };
    }

    static bool TryGetRegion(string regionId, out RegionData data)
    {
        data = null;
        if (string.IsNullOrEmpty(regionId)) return false;
        if (!cache.TryGetValue(regionId, out data))
        {
            PreloadRegion(regionId);
            cache.TryGetValue(regionId, out data);
        }
        return data != null;
    }

    public static float SampleElevation(string regionId, Vector2 canvasPos)
    {
        if (!TryGetRegion(regionId, out var d)) return 0f;
        float gx = Mathf.Clamp((canvasPos.x - d.canvasMinX) / d.canvasWidth, 0f, 1f) * (d.width - 1);
        float gy = Mathf.Clamp(canvasPos.y / CanvasHeight, 0f, 1f) * (d.height - 1);

        int x0 = (int)gx;
        int y0 = (int)gy;
        int x1 = x0 + 1 < d.width ? x0 + 1 : x0;
        int y1 = y0 + 1 < d.height ? y0 + 1 : y0;
        float tx = gx - x0;
        float ty = gy - y0;

        float e00 = d.interleaved[(y0 * d.width + x0) * 2];
        float e10 = d.interleaved[(y0 * d.width + x1) * 2];
        float e01 = d.interleaved[(y1 * d.width + x0) * 2];
        float e11 = d.interleaved[(y1 * d.width + x1) * 2];

        float ex0 = e00 + (e10 - e00) * tx;
        float ex1 = e01 + (e11 - e01) * tx;
        return (ex0 + (ex1 - ex0) * ty) / 255f;
    }

    public static byte SampleFlags(string regionId, Vector2 canvasPos)
    {
        if (!TryGetRegion(regionId, out var d)) return 0;
        int x = Mathf.Clamp(Mathf.RoundToInt((canvasPos.x - d.canvasMinX) / d.canvasWidth * (d.width - 1)), 0, d.width - 1);
        int y = Mathf.Clamp(Mathf.RoundToInt(canvasPos.y / CanvasHeight * (d.height - 1)), 0, d.height - 1);
        return d.interleaved[(y * d.width + x) * 2 + 1];
    }

    public static bool IsBlocked(string regionId, Vector2 canvasPos)
    {
        return (SampleFlags(regionId, canvasPos) & FlagBlocked) != 0;
    }

    /// <summary>
    /// 沿单段直线离散采样，零 GC 评估长度、高程起伏、陡坡占比及天堑阻断/跨越惩罚。
    /// </summary>
    public static void EvaluateSegment(string regionId, Vector2 a, Vector2 b, out SegmentTerrainResult result)
    {
        result = default;
        float dist = Vector2.Distance(a, b);
        result.distance = dist;
        if (dist < 0.01f || !TryGetRegion(regionId, out _)) return;

        const float stepPx = 5.5f;
        int steps = Mathf.Max(2, Mathf.CeilToInt(dist / stepPx));
        float prevElev = SampleElevation(regionId, a);
        int steepHits = 0;
        int hazardHits = 0;

        for (int i = 0; i <= steps; i++)
        {
            float t = (float)i / steps;
            Vector2 p = Vector2.Lerp(a, b, t);
            byte flags = SampleFlags(regionId, p);
            if ((flags & FlagBlocked) != 0)
            {
                hazardHits++;
                result.crossedHazard = true;
                if (!CanCrossHazard && !result.blocked)
                {
                    result.blocked = true;
                    result.firstBlockedPos = p;
                }
            }
            if ((flags & FlagSteep) != 0) steepHits++;

            if (i > 0)
            {
                float elev = SampleElevation(regionId, p);
                result.climbSum += Mathf.Abs(elev - prevElev);
                prevElev = elev;
            }
        }

        result.steepRatio = (float)steepHits / (steps + 1);
        result.hazardRatio = (float)hazardHits / (steps + 1);
        float basePenalty = (result.steepRatio * 0.45f + result.climbSum * 0.80f) * TerrainPenaltyScale;
        float hazardBonus = result.crossedHazard ? Mathf.Max(0.35f, Mathf.Clamp01(result.hazardRatio * 3.5f)) * HazardCrossPenaltyBonus : 0f;
        result.penaltyRatio = basePenalty + hazardBonus;
    }

    public static void EvaluatePolyline(string regionId, IReadOnlyList<Vector2> pts, out SegmentTerrainResult total)
    {
        total = default;
        if (pts == null || pts.Count < 2) return;

        float weightedPenalty = 0f;
        float weightedSteep = 0f;
        float weightedHazard = 0f;
        for (int i = 0; i < pts.Count - 1; i++)
        {
            EvaluateSegment(regionId, pts[i], pts[i + 1], out var seg);
            total.distance += seg.distance;
            total.climbSum += seg.climbSum;
            weightedPenalty += seg.penaltyRatio * seg.distance;
            weightedSteep += seg.steepRatio * seg.distance;
            weightedHazard += seg.hazardRatio * seg.distance;
            if (seg.crossedHazard) total.crossedHazard = true;
            if (seg.blocked && !total.blocked)
            {
                total.blocked = true;
                total.firstBlockedPos = seg.firstBlockedPos;
            }
        }

        if (total.distance > 0.01f)
        {
            total.steepRatio = weightedSteep / total.distance;
            total.hazardRatio = weightedHazard / total.distance;
            total.penaltyRatio = weightedPenalty / total.distance;
        }
    }

    public static Vector2 SnapToGrid(Vector2 canvasPos, string regionId = null, float spacing = DefaultGridSpacing, float minY = 173f, float maxY = 1747f)
    {
        GetRegionHorizontalBounds(regionId, out float minX, out float maxX);
        float s = Mathf.Max(12f, spacing);
        float x = Mathf.Round(canvasPos.x / s) * s;
        float y = Mathf.Round(canvasPos.y / s) * s;
        x = Mathf.Clamp(x, minX + s * 0.5f, maxX - s * 0.5f);
        y = Mathf.Clamp(y, minY + s * 0.5f, maxY - s * 0.5f);
        return new Vector2(x, y);
    }

    public static Vector2 SnapToGrid(Vector2 canvasPos, float spacing, float minY = 173f, float maxY = 1747f)
    {
        return SnapToGrid(canvasPos, null, spacing, minY, maxY);
    }

    // ---------- 零 GC 点阵最短可行路径 A*（兼容月球 1080 与火星宽幅 2592） ----------
    const float RouteStep = 18f;
    const int AStarCols = 144; // 2592 / 18 = 144
    const int AStarRows = 107; // 1920 / 18 ≈ 106.6
    const int AStarNodes = AStarCols * AStarRows;
    static readonly float[] astarG = new float[AStarNodes];
    static readonly int[] astarParent = new int[AStarNodes];
    static readonly byte[] astarState = new byte[AStarNodes];
    static readonly int[] astarHeap = new int[AStarNodes * 2];
    static readonly float[] astarHeapF = new float[AStarNodes * 2];
    static readonly int[] astarRawPath = new int[AStarNodes];
    static readonly List<Vector2> astarTmpA = new List<Vector2>(96);
    static readonly List<Vector2> astarTmpB = new List<Vector2>(160);
    static readonly int[] dirDx = { 1, -1, 0, 0, 1, 1, -1, -1 };
    static readonly int[] dirDy = { 0, 0, 1, -1, 1, -1, 1, -1 };
    static readonly float[] dirCost = { 1f, 1f, 1f, 1f, 1.41421356f, 1.41421356f, 1.41421356f, 1.41421356f };

    static Vector2 GridNodeToCanvas(float minX, float maxX, int gx, int gy)
    {
        return new Vector2(
            Mathf.Clamp(minX + (gx + 0.5f) * RouteStep, minX + RouteStep * 0.5f, maxX - RouteStep * 0.5f),
            Mathf.Clamp((gy + 0.5f) * RouteStep, 173f, 1747f)
        );
    }

    static void CanvasToGridNode(float minX, int activeCols, Vector2 p, out int gx, out int gy)
    {
        gx = Mathf.Clamp(Mathf.FloorToInt((p.x - minX) / RouteStep), 0, activeCols - 1);
        gy = Mathf.Clamp(Mathf.FloorToInt(p.y / RouteStep), 8, AStarRows - 9);
    }

    public static bool IsSegmentBlocked(string regionId, Vector2 a, Vector2 b)
    {
        if (CanCrossHazard || !TryGetRegion(regionId, out _)) return false;
        float dist = Vector2.Distance(a, b);
        if (dist < 0.01f) return IsBlocked(regionId, a);
        int steps = Mathf.Max(2, Mathf.CeilToInt(dist / 6f));
        for (int i = 0; i <= steps; i++)
        {
            if (IsBlocked(regionId, Vector2.Lerp(a, b, (float)i / steps))) return true;
        }
        return false;
    }

    public static bool FindShortestGridWaypoints(string regionId, Vector2 startPos, Vector2 endPos, List<Vector2> outWaypoints)
    {
        outWaypoints.Clear();
        if (!TryGetRegion(regionId, out var reg)) return true;
        if (!IsSegmentBlocked(regionId, startPos, endPos))
            return true;

        float minX = reg.canvasMinX;
        float maxX = minX + reg.canvasWidth;
        int activeCols = Mathf.Clamp(Mathf.CeilToInt(reg.canvasWidth / RouteStep), 1, AStarCols);

        CanvasToGridNode(minX, activeCols, startPos, out int sx, out int sy);
        CanvasToGridNode(minX, activeCols, endPos, out int ex, out int ey);
        int startIdx = sy * activeCols + sx;
        int endIdx = ey * activeCols + ex;
        if (startIdx == endIdx) return true;

        int activeTotal = activeCols * AStarRows;
        Array.Clear(astarState, 0, activeTotal);
        for (int i = 0; i < activeTotal; i++) astarG[i] = float.MaxValue;

        int heapSize = 0;
        astarG[startIdx] = 0f;
        astarParent[startIdx] = -1;
        astarState[startIdx] = 1;
        HeapPush(ref heapSize, startIdx, Vector2.Distance(GridNodeToCanvas(minX, maxX, sx, sy), GridNodeToCanvas(minX, maxX, ex, ey)));

        bool found = false;
        int iterations = 0;
        while (heapSize > 0 && iterations++ < 24576)
        {
            int cur = HeapPop(ref heapSize);
            if (cur == endIdx) { found = true; break; }
            if (astarState[cur] == 2) continue;
            astarState[cur] = 2;

            int cx = cur % activeCols;
            int cy = cur / activeCols;
            for (int d = 0; d < 8; d++)
            {
                int nx = cx + dirDx[d];
                int ny = cy + dirDy[d];
                if (nx < 0 || nx >= activeCols || ny < 8 || ny >= AStarRows - 9) continue;
                int nIdx = ny * activeCols + nx;
                if (astarState[nIdx] == 2) continue;

                Vector2 nPos = GridNodeToCanvas(minX, maxX, nx, ny);
                float steepBias = 1f;
                if (nIdx != endIdx)
                {
                    byte nFlags = SampleFlags(regionId, nPos);
                    if ((nFlags & FlagBlocked) != 0)
                    {
                        if (!CanCrossHazard) continue;
                        steepBias = 4.2f; // 已解锁跨越天堑：仍优先绕近路，必要时允许强行跨越
                    }
                    else if (!CanCrossHazard && d >= 4)
                    {
                        if (IsBlocked(regionId, GridNodeToCanvas(minX, maxX, cx + dirDx[d], cy)) &&
                            IsBlocked(regionId, GridNodeToCanvas(minX, maxX, cx, cy + dirDy[d])))
                            continue;
                    }
                    if ((nFlags & FlagSteep) != 0) steepBias *= 1.16f;
                }

                float ng = astarG[cur] + dirCost[d] * RouteStep * steepBias;
                if (ng < astarG[nIdx])
                {
                    astarG[nIdx] = ng;
                    astarParent[nIdx] = cur;
                    astarState[nIdx] = 1;
                    float h = Mathf.Sqrt((nx - ex) * (nx - ex) + (ny - ey) * (ny - ey)) * RouteStep;
                    if (heapSize < astarHeap.Length)
                        HeapPush(ref heapSize, nIdx, ng + h * 1.001f);
                }
            }
        }

        if (!found) return false;

        int rawCount = 0;
        int p = endIdx;
        while (p != -1 && rawCount < activeTotal)
        {
            astarRawPath[rawCount++] = p;
            p = astarParent[p];
        }

        astarTmpA.Clear();
        astarTmpA.Add(startPos);
        for (int i = rawCount - 2; i >= 1; i--)
        {
            int idx = astarRawPath[i];
            astarTmpA.Add(GridNodeToCanvas(minX, maxX, idx % activeCols, idx / activeCols));
        }
        astarTmpA.Add(endPos);

        int anchor = 0;
        Vector2 anchorPos = startPos;
        while (anchor < astarTmpA.Count - 1)
        {
            int furthest = anchor + 1;
            for (int probe = astarTmpA.Count - 1; probe > anchor + 1; probe--)
            {
                if (!IsSegmentBlocked(regionId, anchorPos, astarTmpA[probe]))
                {
                    furthest = probe;
                    break;
                }
            }
            if (furthest < astarTmpA.Count - 1)
            {
                Vector2 rawWp = astarTmpA[furthest];
                Vector2 snappedWp = SnapToGrid(rawWp, regionId);
                Vector2 chosen = ((CanCrossHazard || !IsBlocked(regionId, snappedWp)) && !IsSegmentBlocked(regionId, anchorPos, snappedWp))
                    ? snappedWp
                    : rawWp;
                if (Vector2.Distance(anchorPos, chosen) >= 20f && Vector2.Distance(chosen, endPos) >= 20f)
                {
                    Vector2 segDir = (chosen - anchorPos).normalized;
                    Vector2 fwdDir = (endPos - anchorPos).normalized;
                    if (Vector2.Dot(segDir, fwdDir) > -0.15f)
                    {
                        outWaypoints.Add(chosen);
                        anchorPos = chosen;
                    }
                }
            }
            anchor = furthest;
        }

        for (int i = outWaypoints.Count - 1; i >= 0; i--)
        {
            Vector2 prev = i > 0 ? outWaypoints[i - 1] : startPos;
            Vector2 next = i + 1 < outWaypoints.Count ? outWaypoints[i + 1] : endPos;
            Vector2 d1 = (outWaypoints[i] - prev).normalized;
            Vector2 d2 = (next - outWaypoints[i]).normalized;
            if (Vector2.Dot(d1, d2) < -0.42f && !IsSegmentBlocked(regionId, prev, next))
                outWaypoints.RemoveAt(i);
        }
        return true;
    }

    static void HeapPush(ref int size, int node, float f)
    {
        int i = size++;
        astarHeap[i] = node;
        astarHeapF[i] = f;
        while (i > 0)
        {
            int parent = (i - 1) >> 1;
            if (astarHeapF[parent] <= astarHeapF[i]) break;
            (astarHeap[parent], astarHeap[i]) = (astarHeap[i], astarHeap[parent]);
            (astarHeapF[parent], astarHeapF[i]) = (astarHeapF[i], astarHeapF[parent]);
            i = parent;
        }
    }

    static int HeapPop(ref int size)
    {
        int top = astarHeap[0];
        int last = --size;
        if (last > 0)
        {
            astarHeap[0] = astarHeap[last];
            astarHeapF[0] = astarHeapF[last];
            int i = 0;
            while (true)
            {
                int left = (i << 1) + 1;
                if (left >= size) break;
                int right = left + 1;
                int best = (right < size && astarHeapF[right] < astarHeapF[left]) ? right : left;
                if (astarHeapF[i] <= astarHeapF[best]) break;
                (astarHeap[i], astarHeap[best]) = (astarHeap[best], astarHeap[i]);
                (astarHeapF[i], astarHeapF[best]) = (astarHeapF[best], astarHeapF[i]);
                i = best;
            }
        }
        return top;
    }

    /// <summary>
    /// Chaikin 两轮切角平滑：将带拐点的控制点列平滑为顺滑曲线，保留首尾站点精确坐标，零 GC。
    /// </summary>
    public static void SmoothPolylineChaikin(IReadOnlyList<Vector2> controlPts, List<Vector2> outPts, int iterations = 2)
    {
        outPts.Clear();
        if (controlPts == null || controlPts.Count == 0) return;
        if (controlPts.Count <= 2 || iterations <= 0)
        {
            for (int i = 0; i < controlPts.Count; i++) outPts.Add(controlPts[i]);
            return;
        }

        astarTmpB.Clear();
        for (int i = 0; i < controlPts.Count; i++) astarTmpB.Add(controlPts[i]);

        for (int it = 0; it < iterations; it++)
        {
            outPts.Clear();
            outPts.Add(astarTmpB[0]);
            for (int i = 0; i < astarTmpB.Count - 1; i++)
            {
                Vector2 a = astarTmpB[i];
                Vector2 b = astarTmpB[i + 1];
                if (i > 0) outPts.Add(Vector2.Lerp(a, b, 0.25f));
                if (i < astarTmpB.Count - 2) outPts.Add(Vector2.Lerp(a, b, 0.75f));
            }
            outPts.Add(astarTmpB[astarTmpB.Count - 1]);

            if (it + 1 < iterations)
            {
                astarTmpB.Clear();
                astarTmpB.AddRange(outPts);
            }
        }
    }

    /// <summary>
    /// 整线向心 Catmull-Rom 样条采样（α = 0.5）：100% 精准穿过每一个站点与拐点，不打结、支持闭环首尾连续。
    /// 若提供 ctrlSegIndices（每个控制点所属逻辑段索引），同步输出 outSubSegIndices 供整线分段热力着色。零 GC。
    /// </summary>
    public static void SampleCatmullRomPolyline(
        IReadOnlyList<Vector2> controlPts,
        bool isLoop,
        List<Vector2> outPts,
        List<int> outSubSegIndices = null,
        IReadOnlyList<int> ctrlSegIndices = null)
    {
        outPts.Clear();
        if (outSubSegIndices != null) outSubSegIndices.Clear();
        if (controlPts == null || controlPts.Count == 0) return;
        int n = controlPts.Count;
        if (n == 1)
        {
            outPts.Add(controlPts[0]);
            return;
        }
        if (n == 2)
        {
            outPts.Add(controlPts[0]);
            outPts.Add(controlPts[1]);
            if (outSubSegIndices != null)
                outSubSegIndices.Add(ctrlSegIndices != null && ctrlSegIndices.Count > 0 ? ctrlSegIndices[0] : 0);
            return;
        }

        // 闭环时 controlPts 末尾与首点重合，有效环点数为 n - 1
        bool closed = isLoop && n >= 4 && Vector2.Distance(controlPts[0], controlPts[n - 1]) < 1f;
        int segCount = n - 1;
        outPts.Add(controlPts[0]);

        for (int i = 0; i < segCount; i++)
        {
            Vector2 p1 = controlPts[i];
            Vector2 p2 = controlPts[i + 1];
            Vector2 p0 = i > 0
                ? controlPts[i - 1]
                : (closed ? controlPts[n - 2] : p1 + (p1 - p2));
            Vector2 p3 = (i + 2 < n)
                ? controlPts[i + 2]
                : (closed ? controlPts[1] : p2 + (p2 - p1));

            // 锐角保护：若前后段形成大角度折返（夹角 > 105°），收敛切线防止样条甩出大弧圈
            Vector2 dirSeg = (p2 - p1).normalized;
            if (Vector2.Dot((p1 - p0).normalized, dirSeg) < -0.25f) p0 = p1 - (p2 - p1);
            if (Vector2.Dot(dirSeg, (p3 - p2).normalized) < -0.25f) p3 = p2 + (p2 - p1);

            // 臂长夹紧：避免超长相邻段牵引短距拐点发生过冲漂移
            float segLen = Vector2.Distance(p1, p2);
            float maxArm = Mathf.Max(segLen * 1.6f, 48f);
            if (Vector2.Distance(p0, p1) > maxArm) p0 = p1 + (p0 - p1).normalized * maxArm;
            if (Vector2.Distance(p3, p2) > maxArm) p3 = p2 + (p3 - p2).normalized * maxArm;

            int subDiv = Mathf.Clamp(Mathf.CeilToInt(segLen / 34f), 3, 8);
            int logicSeg = ctrlSegIndices != null && i < ctrlSegIndices.Count ? ctrlSegIndices[i] : i;

            // 向心 Catmull-Rom 节点参数 (α = 0.5)，加最小距离下限防极近点过冲甩圈
            float t0 = 0f;
            float t1 = t0 + Mathf.Pow(Mathf.Max(6f, Vector2.Distance(p0, p1)), 0.5f);
            float t2 = t1 + Mathf.Pow(Mathf.Max(6f, Vector2.Distance(p1, p2)), 0.5f);
            float t3 = t2 + Mathf.Pow(Mathf.Max(6f, Vector2.Distance(p2, p3)), 0.5f);

            for (int s = 1; s <= subDiv; s++)
            {
                float u = (float)s / subDiv;
                float t = Mathf.Lerp(t1, t2, u);
                Vector2 pt = (s == subDiv) ? p2 : EvalCentripetalCatmullRom(p0, p1, p2, p3, t0, t1, t2, t3, t);
                outPts.Add(pt);
                if (outSubSegIndices != null) outSubSegIndices.Add(logicSeg);
            }
        }
    }

    static Vector2 EvalCentripetalCatmullRom(
        Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3,
        float t0, float t1, float t2, float t3, float t)
    {
        Vector2 A1 = ((t1 - t) / Mathf.Max(0.0001f, t1 - t0)) * p0 + ((t - t0) / Mathf.Max(0.0001f, t1 - t0)) * p1;
        Vector2 A2 = ((t2 - t) / Mathf.Max(0.0001f, t2 - t1)) * p1 + ((t - t1) / Mathf.Max(0.0001f, t2 - t1)) * p2;
        Vector2 A3 = ((t3 - t) / Mathf.Max(0.0001f, t3 - t2)) * p2 + ((t - t2) / Mathf.Max(0.0001f, t3 - t2)) * p3;

        Vector2 B1 = ((t2 - t) / Mathf.Max(0.0001f, t2 - t0)) * A1 + ((t - t0) / Mathf.Max(0.0001f, t2 - t0)) * A2;
        Vector2 B2 = ((t3 - t) / Mathf.Max(0.0001f, t3 - t1)) * A2 + ((t - t1) / Mathf.Max(0.0001f, t3 - t1)) * A3;

        return ((t2 - t) / Mathf.Max(0.0001f, t2 - t1)) * B1 + ((t - t1) / Mathf.Max(0.0001f, t2 - t1)) * B2;
    }
}
