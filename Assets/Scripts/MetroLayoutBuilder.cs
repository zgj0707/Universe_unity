using System;
using System.Collections.Generic;
using UnityEngine;

public class MetroNodeLayout
{
    public int stationId;
    public string name;
    public Vector2 pos;
    public Vector2 labelOffset;
    public int degree;
    public bool isTransfer;
    public bool isPortalHint;
    public Color primaryColor = new Color(0.22f, 0.25f, 0.30f, 1f);
}

public class MetroPolylineLayout
{
    public int lineId;
    public string lineName;
    public Color color;
    public List<Vector2> points = new List<Vector2>();
}

public class MetroLegendItem
{
    public int lineId;
    public string lineName;
    public Color color;
    public int stationCount;
}

public class MetroLayoutResult
{
    public string scopeTitle;
    public int stationCount;
    public int lineCount;
    public Vector2 boundsMin;
    public Vector2 boundsMax;
    public readonly List<MetroNodeLayout> nodes = new List<MetroNodeLayout>();
    public readonly List<MetroPolylineLayout> polylines = new List<MetroPolylineLayout>();
    public readonly List<MetroLegendItem> legends = new List<MetroLegendItem>();
}

/// <summary>
/// 区域地铁拓扑图布局引擎（LOOM / Stott 严格保序八方向网格优化 + Rail Map Painter 135° 顺向折线）：
/// 1. 按区域提取已建成子图并保留跨区出口提示；
/// 2. 加密离散整数网格量化（先用单调保序分布铺开密集区，再量化落格）；
/// 3. 带“东西南北相对方位不翻转”硬约束的 Stott 局部爬山优化，确保不乱跑、不拧麻花；
/// 4. 顺向 0°/45°/90° 走线 + 135° 圆弧转角 + 共线平行偏移 + 3 圈无重叠标签排版。
/// </summary>
public static class MetroLayoutBuilder
{
    static readonly Vector2Int[] Neighbor8 =
    {
        new Vector2Int(1, 0), new Vector2Int(-1, 0),
        new Vector2Int(0, 1), new Vector2Int(0, -1),
        new Vector2Int(1, 1), new Vector2Int(1, -1),
        new Vector2Int(-1, 1), new Vector2Int(-1, -1)
    };

    // 16 色柔和印刷级地铁色板（控制饱和度，白底久看不晃眼且区分度高）
    static readonly Color[] DistinctPalette =
    {
        new Color(0.76f, 0.34f, 0.32f, 1f), // 砖红
        new Color(0.27f, 0.49f, 0.72f, 1f), // 普鲁士蓝
        new Color(0.30f, 0.58f, 0.44f, 1f), // 松石绿
        new Color(0.80f, 0.54f, 0.28f, 1f), // 焦糖橙
        new Color(0.55f, 0.42f, 0.68f, 1f), // 莫兰迪紫
        new Color(0.24f, 0.58f, 0.62f, 1f), // 瓷青
        new Color(0.74f, 0.40f, 0.54f, 1f), // 豆沙玫
        new Color(0.49f, 0.57f, 0.31f, 1f), // 苔藓绿
        new Color(0.58f, 0.45f, 0.35f, 1f), // 暖咖棕
        new Color(0.32f, 0.38f, 0.62f, 1f), // 靛青
        new Color(0.76f, 0.63f, 0.28f, 1f), // 芥末金
        new Color(0.38f, 0.64f, 0.56f, 1f), // 灰薄荷
        new Color(0.64f, 0.31f, 0.42f, 1f), // 梅子红
        new Color(0.42f, 0.57f, 0.76f, 1f), // 雾霾蓝
        new Color(0.64f, 0.50f, 0.70f, 1f), // 丁香紫
        new Color(0.41f, 0.46f, 0.50f, 1f)  // 石墨灰
    };

    public static MetroLayoutResult Build(
        IList<DemoBootstrap.Station> allStations,
        IList<DemoBootstrap.Line> allLines,
        string regionFilter,
        string cityFilter,
        Vector2 canvasSize,
        MetroStyleProfile style)
    {
        var result = new MetroLayoutResult();
        if (allStations == null || style == null) return result;

        string targetRegion = string.IsNullOrEmpty(regionFilter) ? "月之亮面" : regionFilter;
        bool filterByCity = !string.IsNullOrEmpty(cityFilter) && cityFilter != "全区";
        result.scopeTitle = filterByCity ? cityFilter : targetRegion;

        // 1. 收集区域内已建成站点
        var includedMap = new Dictionary<int, DemoBootstrap.Station>();
        var stationById = new Dictionary<int, DemoBootstrap.Station>();
        for (int i = 0; i < allStations.Count; i++)
        {
            var st = allStations[i];
            if (st == null) continue;
            stationById[st.id] = st;
            if (!st.built || st.closed) continue;
            if (!string.Equals(st.region, targetRegion, StringComparison.Ordinal)) continue;
            if (filterByCity && !string.Equals(st.city, cityFilter, StringComparison.Ordinal)) continue;
            includedMap[st.id] = st;
        }

        if (includedMap.Count == 0)
        {
            result.boundsMin = Vector2.zero;
            result.boundsMax = canvasSize;
            return result;
        }

        // 2. 筛选有效线路并生成跨区出口提示点
        var activeLines = new List<(DemoBootstrap.Line line, List<int> seq, Color color)>();
        var portalNodes = new Dictionary<string, MetroNodeLayout>();
        var rawPositions = new Dictionary<int, Vector2>();
        var nodeLineSet = new Dictionary<int, HashSet<int>>();

        foreach (var kv in includedMap)
        {
            rawPositions[kv.Key] = new Vector2(kv.Value.x, kv.Value.y);
            nodeLineSet[kv.Key] = new HashSet<int>();
        }

        int virtualIdSeed = -1000;
        if (allLines != null)
        {
            for (int li = 0; li < allLines.Count; li++)
            {
                var line = allLines[li];
                if (line == null || line.st == null || line.st.Count < 2) continue;

                var subSeq = new List<int>();
                for (int si = 0; si < line.st.Count; si++)
                {
                    int sid = line.st[si];
                    if (includedMap.ContainsKey(sid))
                    {
                        subSeq.Add(sid);
                        nodeLineSet[sid].Add(line.id);
                    }
                    else if (subSeq.Count > 0 && stationById.TryGetValue(sid, out var outsideSt))
                    {
                        int prevId = subSeq[subSeq.Count - 1];
                        if (includedMap.TryGetValue(prevId, out var prevSt))
                        {
                            string pKey = prevId + "->" + (string.IsNullOrEmpty(outsideSt.city) ? outsideSt.region : outsideSt.city);
                            if (!portalNodes.TryGetValue(pKey, out var pNode))
                            {
                                Vector2 dir = new Vector2(outsideSt.x - prevSt.x, outsideSt.y - prevSt.y);
                                if (dir.sqrMagnitude < 1e-6f) dir = Vector2.right;
                                dir.Normalize();

                                int vid = virtualIdSeed--;
                                string destLabel = !string.IsNullOrEmpty(outsideSt.city) && outsideSt.city != prevSt.city
                                    ? outsideSt.city
                                    : outsideSt.name;
                                pNode = new MetroNodeLayout
                                {
                                    stationId = vid,
                                    name = "往 " + destLabel,
                                    isPortalHint = true,
                                    degree = 1
                                };
                                portalNodes[pKey] = pNode;
                                rawPositions[vid] = new Vector2(prevSt.x, prevSt.y) + dir * 0.045f;
                                nodeLineSet[vid] = new HashSet<int> { line.id };
                            }
                            subSeq.Add(pNode.stationId);
                        }
                        break;
                    }
                }

                if (subSeq.Count >= 2)
                {
                    activeLines.Add((line, subSeq, line.color));
                }
            }
        }

        // 2.5 为有效线路分配柔和且不撞色的色板
        AssignDistinctLineColors(activeLines);

        // 3. 加密整数网格量化（先用单调保序撑开密集区再落格，彻底防止核心区挤成麻花）
        var ids = new List<int>(rawPositions.Keys);
        Dictionary<int, float> warpedX = WarpAxisMonotonic(ids, rawPositions, true);
        Dictionary<int, float> warpedY = WarpAxisMonotonic(ids, rawPositions, false);

        float minX = float.MaxValue, maxX = float.MinValue;
        float minY = float.MaxValue, maxY = float.MinValue;
        for (int i = 0; i < ids.Count; i++)
        {
            int id = ids[i];
            float wx = warpedX[id], wy = warpedY[id];
            if (wx < minX) minX = wx;
            if (wx > maxX) maxX = wx;
            if (wy < minY) minY = wy;
            if (wy > maxY) maxY = wy;
        }

        float spanX = Mathf.Max(0.01f, maxX - minX);
        float spanY = Mathf.Max(0.01f, maxY - minY);
        bool landscape = canvasSize.x > canvasSize.y;
        int baseCols = Mathf.Clamp(Mathf.RoundToInt(Mathf.Sqrt(ids.Count) * (landscape ? 3.2f : 2.2f)), 14, 28);
        int baseRows = Mathf.Clamp(Mathf.RoundToInt(baseCols * (spanY / spanX)), 12, 34);

        var gridPos = new Dictionary<int, Vector2Int>(ids.Count);
        var originGrid = new Dictionary<int, Vector2Int>(ids.Count);
        var occupied = new HashSet<Vector2Int>();

        for (int i = 0; i < ids.Count; i++)
        {
            int id = ids[i];
            int gx = Mathf.RoundToInt(((warpedX[id] - minX) / spanX) * baseCols);
            int gy = Mathf.RoundToInt(((warpedY[id] - minY) / spanY) * baseRows);
            Vector2Int ideal = new Vector2Int(gx, gy);
            Vector2Int g = FindNearestFreeCell(ideal, occupied);
            occupied.Add(g);
            gridPos[id] = g;
            originGrid[id] = ideal;
        }

        // 构建邻接表与同线三点序列
        var neighbors = new Dictionary<int, List<int>>(ids.Count);
        for (int i = 0; i < ids.Count; i++) neighbors[ids[i]] = new List<int>();
        var triples = new List<(int a, int b, int c)>();

        for (int li = 0; li < activeLines.Count; li++)
        {
            var seq = activeLines[li].seq;
            for (int si = 0; si < seq.Count - 1; si++)
            {
                int u = seq[si], v = seq[si + 1];
                if (!neighbors[u].Contains(v)) neighbors[u].Add(v);
                if (!neighbors[v].Contains(u)) neighbors[v].Add(u);
                if (si < seq.Count - 2)
                {
                    triples.Add((seq[si], seq[si + 1], seq[si + 2]));
                }
            }
        }

        // 4. 带“相对方位不翻转”硬约束的 Stott 多准则爬山优化
        if (ids.Count > 1)
        {
            for (int pass = 0; pass < 16; pass++)
            {
                bool improvedAny = false;
                for (int i = 0; i < ids.Count; i++)
                {
                    int id = ids[i];
                    if (neighbors[id].Count == 0) continue;

                    Vector2Int curr = gridPos[id];
                    float bestScore = EvaluateNodeScore(id, curr, gridPos, originGrid, neighbors, triples);
                    Vector2Int bestCell = curr;

                    for (int n = 0; n < Neighbor8.Length; n++)
                    {
                        Vector2Int cand = curr + Neighbor8[n];
                        if (occupied.Contains(cand)) continue;
                        if ((cand - originGrid[id]).sqrMagnitude > 6) continue;
                        if (ViolatesNeighborOrder(id, cand, rawPositions, gridPos, neighbors[id])) continue;

                        float s = EvaluateNodeScore(id, cand, gridPos, originGrid, neighbors, triples);
                        if (s < bestScore - 0.01f)
                        {
                            bestScore = s;
                            bestCell = cand;
                        }
                    }

                    if (bestCell != curr)
                    {
                        occupied.Remove(curr);
                        occupied.Add(bestCell);
                        gridPos[id] = bestCell;
                        improvedAny = true;
                    }
                }
                if (!improvedAny) break;
            }
        }

        // 5. 映射到画布居中像素坐标
        int gMinX = int.MaxValue, gMaxX = int.MinValue;
        int gMinY = int.MaxValue, gMaxY = int.MinValue;
        foreach (var g in gridPos.Values)
        {
            if (g.x < gMinX) gMinX = g.x;
            if (g.x > gMaxX) gMaxX = g.x;
            if (g.y < gMinY) gMinY = g.y;
            if (g.y > gMaxY) gMaxY = g.y;
        }

        float gSpanX = Mathf.Max(1f, gMaxX - gMinX);
        float gSpanY = Mathf.Max(1f, gMaxY - gMinY);
        float padX = canvasSize.x * 0.12f;
        float padY = canvasSize.y * 0.14f;
        float cellSize = Mathf.Clamp(Mathf.Min((canvasSize.x - padX * 2f) / gSpanX, (canvasSize.y - padY * 2f) / gSpanY), 84f, 165f);
        float totalW = gSpanX * cellSize;
        float totalH = gSpanY * cellSize;
        float startX = (canvasSize.x - totalW) * 0.5f;
        float startY = (canvasSize.y - totalH) * 0.5f;

        var screenPos = new Dictionary<int, Vector2>(gridPos.Count);
        for (int i = 0; i < ids.Count; i++)
        {
            int id = ids[i];
            Vector2Int g = gridPos[id];
            screenPos[id] = new Vector2(
                startX + (g.x - gMinX) * cellSize,
                startY + (g.y - gMinY) * cellSize
            );
        }

        // 6. 统计共线区间，用于平行线等距偏移
        var edgeLines = new Dictionary<long, List<int>>();
        for (int li = 0; li < activeLines.Count; li++)
        {
            var seq = activeLines[li].seq;
            for (int si = 0; si < seq.Count - 1; si++)
            {
                long key = MakeEdgeKey(seq[si], seq[si + 1]);
                if (!edgeLines.TryGetValue(key, out var list))
                {
                    list = new List<int>();
                    edgeLines[key] = list;
                }
                if (!list.Contains(activeLines[li].line.id))
                {
                    list.Add(activeLines[li].line.id);
                }
            }
        }

        // 7. 生成顺向 0°/45°/90° 拓扑折线 + 平行偏移 + 135° 圆弧转角
        float stepPitch = style.lineWidth + style.parallelGap;
        for (int li = 0; li < activeLines.Count; li++)
        {
            var item = activeLines[li];
            var poly = new MetroPolylineLayout
            {
                lineId = item.line.id,
                lineName = string.IsNullOrEmpty(item.line.name) ? (item.line.id + "号线") : item.line.name,
                color = item.color
            };

            var rawPts = new List<Vector2>();
            Vector2 prevDir = Vector2.zero;
            for (int si = 0; si < item.seq.Count - 1; si++)
            {
                int aId = item.seq[si];
                int bId = item.seq[si + 1];
                Vector2 pA = screenPos[aId];
                Vector2 pB = screenPos[bId];

                long edgeKey = MakeEdgeKey(aId, bId);
                var sharing = edgeLines[edgeKey];
                int count = sharing.Count;
                if (count > 1)
                {
                    int rank = sharing.IndexOf(item.line.id);
                    Vector2 baseDir = aId < bId ? (pB - pA) : (pA - pB);
                    if (baseDir.sqrMagnitude < 1e-4f) baseDir = Vector2.right;
                    baseDir.Normalize();
                    Vector2 normal = new Vector2(-baseDir.y, baseDir.x);
                    float laneOffset = (rank - (count - 1) * 0.5f) * stepPitch;
                    pA += normal * laneOffset;
                    pB += normal * laneOffset;
                }

                List<Vector2> segPts = BuildSmartOctilinearSegment(pA, pB, prevDir);
                if (segPts.Count >= 2)
                {
                    Vector2 endDir = segPts[segPts.Count - 1] - segPts[segPts.Count - 2];
                    if (endDir.sqrMagnitude > 0.1f) prevDir = endDir.normalized;
                }

                for (int k = 0; k < segPts.Count; k++)
                {
                    if (rawPts.Count == 0 || (rawPts[rawPts.Count - 1] - segPts[k]).sqrMagnitude > 0.25f)
                    {
                        rawPts.Add(segPts[k]);
                    }
                }
            }

            rawPts = SimplifyCollinear(rawPts);
            poly.points = style.cornerRadius > 1f
                ? SmoothCorners(rawPts, style.cornerRadius)
                : rawPts;

            result.polylines.Add(poly);

            int realCount = 0;
            for (int si = 0; si < item.seq.Count; si++)
            {
                if (includedMap.ContainsKey(item.seq[si])) realCount++;
            }
            result.legends.Add(new MetroLegendItem
            {
                lineId = item.line.id,
                lineName = poly.lineName,
                color = poly.color,
                stationCount = realCount
            });
        }

        // 8. 组装节点与主线色
        var nodePrimaryColor = new Dictionary<int, Color>();
        for (int li = 0; li < activeLines.Count; li++)
        {
            var seq = activeLines[li].seq;
            Color lCol = activeLines[li].color;
            for (int si = 0; si < seq.Count; si++)
            {
                if (!nodePrimaryColor.ContainsKey(seq[si]))
                {
                    nodePrimaryColor[seq[si]] = lCol;
                }
            }
        }

        float bMinX = float.MaxValue, bMaxX = float.MinValue;
        float bMinY = float.MaxValue, bMaxY = float.MinValue;

        foreach (var kv in includedMap)
        {
            Vector2 p = screenPos[kv.Key];
            int deg = nodeLineSet[kv.Key].Count;
            string dispName = string.IsNullOrEmpty(kv.Value.customName) ? kv.Value.name : kv.Value.customName;
            Color pCol = nodePrimaryColor.TryGetValue(kv.Key, out var lc) ? lc : style.stationStrokeColor;
            var node = new MetroNodeLayout
            {
                stationId = kv.Key,
                name = dispName,
                pos = p,
                degree = deg,
                isTransfer = deg >= 2,
                isPortalHint = false,
                primaryColor = pCol
            };
            result.nodes.Add(node);
            ExpandBounds(p, ref bMinX, ref bMaxX, ref bMinY, ref bMaxY);
        }

        foreach (var pNode in portalNodes.Values)
        {
            pNode.pos = screenPos[pNode.stationId];
            pNode.primaryColor = nodePrimaryColor.TryGetValue(pNode.stationId, out var lc) ? lc : style.stationStrokeColor;
            result.nodes.Add(pNode);
            ExpandBounds(pNode.pos, ref bMinX, ref bMaxX, ref bMinY, ref bMaxY);
        }

        ComputeLabelOffsets(result.nodes, result.polylines, style);

        result.stationCount = includedMap.Count;
        result.lineCount = result.polylines.Count;
        result.boundsMin = new Vector2(bMinX - 100f, bMinY - 100f);
        result.boundsMax = new Vector2(bMaxX + 100f, bMaxY + 100f);
        return result;
    }

    static Dictionary<int, float> WarpAxisMonotonic(List<int> ids, Dictionary<int, Vector2> rawPositions, bool useX)
    {
        var sorted = new List<int>(ids);
        sorted.Sort((a, b) =>
        {
            float va = useX ? rawPositions[a].x : rawPositions[a].y;
            float vb = useX ? rawPositions[b].x : rawPositions[b].y;
            return va.CompareTo(vb);
        });

        var warped = new Dictionary<int, float>(sorted.Count);
        float accum = 0f;
        warped[sorted[0]] = 0f;

        for (int i = 1; i < sorted.Count; i++)
        {
            float prev = useX ? rawPositions[sorted[i - 1]].x : rawPositions[sorted[i - 1]].y;
            float curr = useX ? rawPositions[sorted[i]].x : rawPositions[sorted[i]].y;
            float rawGap = Mathf.Max(0f, curr - prev);
            if (rawGap < 1e-4f)
            {
                warped[sorted[i]] = accum;
            }
            else
            {
                float mappedGap = 0.008f + Mathf.Pow(rawGap, 0.65f) * 0.14f;
                accum += mappedGap;
                warped[sorted[i]] = accum;
            }
        }
        return warped;
    }

    static Vector2Int FindNearestFreeCell(Vector2Int target, HashSet<Vector2Int> occupied)
    {
        if (!occupied.Contains(target)) return target;
        for (int r = 1; r <= 8; r++)
        {
            for (int dx = -r; dx <= r; dx++)
            {
                for (int dy = -r; dy <= r; dy++)
                {
                    if (Mathf.Abs(dx) != r && Mathf.Abs(dy) != r) continue;
                    var cand = new Vector2Int(target.x + dx, target.y + dy);
                    if (!occupied.Contains(cand)) return cand;
                }
            }
        }
        return target;
    }

    /// <summary>
    /// 硬约束：禁止爬山移动导致与相连站点的东西/南北相对方位发生翻转（东侧站点绝对不会跑到西侧）。
    /// </summary>
    static bool ViolatesNeighborOrder(
        int nodeId,
        Vector2Int cand,
        Dictionary<int, Vector2> rawPositions,
        Dictionary<int, Vector2Int> gridPos,
        List<int> nbs)
    {
        Vector2 rawSelf = rawPositions[nodeId];
        for (int i = 0; i < nbs.Count; i++)
        {
            int nbId = nbs[i];
            Vector2 rawNb = rawPositions[nbId];
            Vector2Int gNb = gridPos[nbId];

            float rawDx = rawSelf.x - rawNb.x;
            float rawDy = rawSelf.y - rawNb.y;
            int gDx = cand.x - gNb.x;
            int gDy = cand.y - gNb.y;

            if (rawDx > 0.006f && gDx < 0) return true;
            if (rawDx < -0.006f && gDx > 0) return true;
            if (rawDy > 0.006f && gDy < 0) return true;
            if (rawDy < -0.006f && gDy > 0) return true;
        }
        return false;
    }

    /// <summary>
    /// Stott 多准则代价函数：
    /// 1. 强烈追求每条边落在 0° / 45° / 90° 上（这样绝大多数相邻站连线直接成直线，不再绕路）；
    /// 2. 强力惩罚同线折角与锐角回折（治好环线拧麻花）；
    /// 3. 控制单边格距均衡。
    /// </summary>
    static float EvaluateNodeScore(
        int nodeId,
        Vector2Int candPos,
        Dictionary<int, Vector2Int> gridPos,
        Dictionary<int, Vector2Int> originGrid,
        Dictionary<int, List<int>> neighbors,
        List<(int a, int b, int c)> triples)
    {
        float cost = (candPos - originGrid[nodeId]).sqrMagnitude * 0.8f;

        var nbs = neighbors[nodeId];
        for (int i = 0; i < nbs.Count; i++)
        {
            Vector2Int other = gridPos[nbs[i]];
            int dx = Mathf.Abs(other.x - candPos.x);
            int dy = Mathf.Abs(other.y - candPos.y);
            int minD = Mathf.Min(dx, dy);
            int maxD = Mathf.Max(dx, dy);
            int octErr = Mathf.Min(minD, Mathf.Abs(dx - dy));
            cost += octErr * 16f;
            if (maxD > 4) cost += (maxD - 4) * 3.5f;
        }

        for (int i = 0; i < triples.Count; i++)
        {
            var t = triples[i];
            if (t.a != nodeId && t.b != nodeId && t.c != nodeId) continue;
            Vector2Int pa = t.a == nodeId ? candPos : gridPos[t.a];
            Vector2Int pb = t.b == nodeId ? candPos : gridPos[t.b];
            Vector2Int pc = t.c == nodeId ? candPos : gridPos[t.c];

            Vector2 v1 = ((Vector2)(pb - pa)).normalized;
            Vector2 v2 = ((Vector2)(pc - pb)).normalized;
            float dot = Vector2.Dot(v1, v2);
            cost += (1f - dot) * 11f;
            if (dot < -0.1f) cost += 40f; // 重罚锐角折返，杜绝拧麻花
        }

        return cost;
    }

    static List<Vector2> BuildSmartOctilinearSegment(Vector2 a, Vector2 b, Vector2 prevDir)
    {
        Vector2 d = b - a;
        float ax = Mathf.Abs(d.x);
        float ay = Mathf.Abs(d.y);
        if (ax < 3f) return new List<Vector2> { a, new Vector2(a.x, b.y) };
        if (ay < 3f) return new List<Vector2> { a, new Vector2(b.x, a.y) };
        if (Mathf.Abs(ax - ay) < 3f) return new List<Vector2> { a, b };

        float sx = Mathf.Sign(d.x);
        float sy = Mathf.Sign(d.y);
        float diag = Mathf.Min(ax, ay);

        Vector2 midDiagFirst = a + new Vector2(sx * diag, sy * diag);
        Vector2 midOrthoFirst = ax > ay
            ? new Vector2(b.x - sx * diag, a.y)
            : new Vector2(a.x, b.y - sy * diag);

        if (prevDir.sqrMagnitude < 0.01f)
        {
            Vector2 chosen = ax >= ay ? midOrthoFirst : midDiagFirst;
            return new List<Vector2> { a, chosen, b };
        }

        Vector2 dir1 = (midDiagFirst - a).normalized;
        Vector2 dir2 = (midOrthoFirst - a).normalized;
        Vector2 chosenMid = Vector2.Dot(prevDir, dir1) >= Vector2.Dot(prevDir, dir2)
            ? midDiagFirst
            : midOrthoFirst;

        return new List<Vector2> { a, chosenMid, b };
    }

    static void AssignDistinctLineColors(List<(DemoBootstrap.Line line, List<int> seq, Color color)> activeLines)
    {
        if (activeLines == null || activeLines.Count <= 1) return;

        var lineStations = new List<HashSet<int>>(activeLines.Count);
        for (int i = 0; i < activeLines.Count; i++)
        {
            lineStations.Add(new HashSet<int>(activeLines[i].seq));
        }

        var usedColors = new List<Color>(activeLines.Count);
        for (int i = 0; i < activeLines.Count; i++)
        {
            Color preferred = SoftenColor(activeLines[i].color);
            bool conflict = false;

            for (int j = 0; j < i; j++)
            {
                bool sharesStation = SharesAnyStation(lineStations[i], lineStations[j]);
                float minRequired = sharesStation ? 0.34f : 0.20f;
                if (ColorDistance(preferred, usedColors[j]) < minRequired)
                {
                    conflict = true;
                    break;
                }
            }

            Color chosen = preferred;
            if (conflict)
            {
                float bestMinDist = -1f;
                for (int p = 0; p < DistinctPalette.Length; p++)
                {
                    Color cand = DistinctPalette[p];
                    float minDist = float.MaxValue;
                    for (int j = 0; j < i; j++)
                    {
                        bool sharesStation = SharesAnyStation(lineStations[i], lineStations[j]);
                        float d = ColorDistance(cand, usedColors[j]) * (sharesStation ? 0.72f : 1.0f);
                        if (d < minDist) minDist = d;
                    }
                    if (minDist > bestMinDist)
                    {
                        bestMinDist = minDist;
                        chosen = cand;
                    }
                }
            }

            usedColors.Add(chosen);
            activeLines[i] = (activeLines[i].line, activeLines[i].seq, chosen);
        }
    }

    static Color SoftenColor(Color c)
    {
        Color.RGBToHSV(c, out float h, out float s, out float v);
        s = Mathf.Min(s, 0.58f);
        v = Mathf.Clamp(v, 0.42f, 0.80f);
        Color res = Color.HSVToRGB(h, s, v);
        res.a = 1f;
        return res;
    }

    static bool SharesAnyStation(HashSet<int> a, HashSet<int> b)
    {
        foreach (int id in a)
        {
            if (b.Contains(id)) return true;
        }
        return false;
    }

    static float ColorDistance(Color a, Color b)
    {
        float dr = a.r - b.r;
        float dg = a.g - b.g;
        float db = a.b - b.b;
        return Mathf.Sqrt(dr * dr + dg * dg + db * db);
    }

    static long MakeEdgeKey(int a, int b)
    {
        uint min = (uint)Mathf.Min(a, b);
        uint max = (uint)Mathf.Max(a, b);
        return ((long)min << 32) | max;
    }

    static void ExpandBounds(Vector2 p, ref float minX, ref float maxX, ref float minY, ref float maxY)
    {
        if (p.x < minX) minX = p.x;
        if (p.x > maxX) maxX = p.x;
        if (p.y < minY) minY = p.y;
        if (p.y > maxY) maxY = p.y;
    }

    static List<Vector2> SimplifyCollinear(List<Vector2> pts)
    {
        if (pts == null || pts.Count < 3) return pts;
        var res = new List<Vector2>(pts.Count) { pts[0] };
        for (int i = 1; i < pts.Count - 1; i++)
        {
            Vector2 prev = res[res.Count - 1];
            Vector2 curr = pts[i];
            Vector2 next = pts[i + 1];
            Vector2 d1 = (curr - prev).normalized;
            Vector2 d2 = (next - curr).normalized;
            if (Vector2.Dot(d1, d2) < 0.996f)
            {
                res.Add(curr);
            }
        }
        res.Add(pts[pts.Count - 1]);
        return res;
    }

    static List<Vector2> SmoothCorners(List<Vector2> pts, float radius)
    {
        if (pts == null || pts.Count < 3) return pts;
        var outPts = new List<Vector2>(pts.Count * 5) { pts[0] };

        for (int i = 1; i < pts.Count - 1; i++)
        {
            Vector2 prev = pts[i - 1];
            Vector2 curr = pts[i];
            Vector2 next = pts[i + 1];

            Vector2 d1 = prev - curr;
            Vector2 d2 = next - curr;
            float l1 = d1.magnitude;
            float l2 = d2.magnitude;
            if (l1 < 2f || l2 < 2f) continue;

            Vector2 n1 = d1 / l1;
            Vector2 n2 = d2 / l2;
            float dot = Vector2.Dot(-n1, n2);
            if (dot > 0.995f)
            {
                outPts.Add(curr);
                continue;
            }

            float cut = Mathf.Min(radius, Mathf.Min(l1, l2) * 0.36f);
            Vector2 pEnter = curr + n1 * cut;
            Vector2 pExit = curr + n2 * cut;
            outPts.Add(pEnter);
            for (int s = 1; s <= 3; s++)
            {
                float t = s / 4f;
                Vector2 q = Vector2.Lerp(Vector2.Lerp(pEnter, curr, t), Vector2.Lerp(curr, pExit, t), t);
                outPts.Add(q);
            }
            outPts.Add(pExit);
        }

        outPts.Add(pts[pts.Count - 1]);
        return outPts;
    }

    static void ComputeLabelOffsets(List<MetroNodeLayout> nodes, List<MetroPolylineLayout> lines, MetroStyleProfile style)
    {
        var order = new List<int>(nodes.Count);
        for (int i = 0; i < nodes.Count; i++) order.Add(i);
        order.Sort((a, b) => nodes[b].degree.CompareTo(nodes[a].degree));

        float r = style.stationRadius + 14f;
        var placedBoxes = new List<Rect>(nodes.Count);

        for (int oi = 0; oi < order.Count; oi++)
        {
            int idx = order[oi];
            var node = nodes[idx];
            int charCount = string.IsNullOrEmpty(node.name) ? 2 : node.name.Length;
            float estW = Mathf.Clamp(charCount * style.labelFontSize * 1.05f + 16f, 56f, 220f);
            float estH = style.labelFontSize * 1.38f + 8f;
            float halfW = estW * 0.5f;

            float dxNear = r + halfW * 0.72f + 10f;
            float dyNear = r + 16f;
            float dxMid = r + halfW * 0.92f + 22f;
            float dyMid = r + 38f;
            float dxFar = r + halfW + 42f;
            float dyFar = r + 62f;

            Vector2[] candidates =
            {
                new Vector2(0f, dyNear),
                new Vector2(0f, -dyNear),
                new Vector2(dxNear, 0f),
                new Vector2(-dxNear, 0f),
                new Vector2(dxNear * 0.85f, dyNear * 0.85f),
                new Vector2(-dxNear * 0.85f, dyNear * 0.85f),
                new Vector2(dxNear * 0.85f, -dyNear * 0.85f),
                new Vector2(-dxNear * 0.85f, -dyNear * 0.85f),
                new Vector2(0f, dyMid),
                new Vector2(0f, -dyMid),
                new Vector2(dxMid, 0f),
                new Vector2(-dxMid, 0f),
                new Vector2(dxMid * 0.85f, dyMid * 0.85f),
                new Vector2(-dxMid * 0.85f, dyMid * 0.85f),
                new Vector2(dxMid * 0.85f, -dyMid * 0.85f),
                new Vector2(-dxMid * 0.85f, -dyMid * 0.85f),
                new Vector2(0f, dyFar),
                new Vector2(0f, -dyFar),
                new Vector2(dxFar, 0f),
                new Vector2(-dxFar, 0f),
                new Vector2(dxFar * 0.85f, dyFar * 0.85f),
                new Vector2(-dxFar * 0.85f, dyFar * 0.85f),
                new Vector2(dxFar * 0.85f, -dyFar * 0.85f),
                new Vector2(-dxFar * 0.85f, -dyFar * 0.85f)
            };

            Vector2 bestOffset = candidates[0];
            float bestPenalty = float.MaxValue;

            for (int c = 0; c < candidates.Length; c++)
            {
                Vector2 center = node.pos + candidates[c];
                var box = new Rect(center.x - halfW, center.y - estH * 0.5f, estW, estH);
                int ring = c / 8;
                float penalty = ring * 18f + (c % 8) * 1.2f;

                for (int b = 0; b < placedBoxes.Count; b++)
                {
                    if (box.Overlaps(placedBoxes[b])) penalty += 5000f;
                }

                for (int j = 0; j < nodes.Count; j++)
                {
                    Vector2 np = nodes[j].pos;
                    float pad = style.stationRadius + 10f;
                    var nodeBox = new Rect(np.x - pad, np.y - pad, pad * 2f, pad * 2f);
                    if (box.Overlaps(nodeBox))
                    {
                        penalty += (j == idx ? 3000f : 2200f);
                    }
                }

                for (int li = 0; li < lines.Count; li++)
                {
                    var pts = lines[li].points;
                    for (int pi = 0; pi < pts.Count - 1; pi++)
                    {
                        float distSeg = DistancePointToSegment(center, pts[pi], pts[pi + 1]);
                        float safeDist = style.lineWidth + 18f;
                        if (distSeg < safeDist)
                        {
                            penalty += (safeDist - distSeg) * 28f;
                        }
                    }
                }

                if (penalty < bestPenalty)
                {
                    bestPenalty = penalty;
                    bestOffset = candidates[c];
                    if (ring == 0 && penalty < 10f) break;
                }
            }

            node.labelOffset = bestOffset;
            Vector2 finalCenter = node.pos + bestOffset;
            placedBoxes.Add(new Rect(finalCenter.x - halfW, finalCenter.y - estH * 0.5f, estW, estH));
        }
    }

    static float DistancePointToSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float lenSq = ab.sqrMagnitude;
        if (lenSq < 1e-4f) return Vector2.Distance(p, a);
        float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / lenSq);
        return Vector2.Distance(p, a + ab * t);
    }
}
