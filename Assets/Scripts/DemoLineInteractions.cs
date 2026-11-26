using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public partial class DemoBootstrap
{
    class LineHit { public Line line; public int seg; public float dist; }
    GameObject lineBubbleRoot, lineBubbleScrim;

    public void OnLineTap(Vector2 screen)
    {
        if (AshCycleCompleted()) return;
        if (dragging || pageRoot != null || activePanel != null) return;
        Vector2 local;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(mapRect, screen, null, out local)) return;
        var hits = FindLineHits(local, 24f);
        if (hits.Count == 0) { Toast("点在航线上可以拆除或查看运营"); return; }
        ShowLineBubbles(local, hits);
    }

    List<LineHit> FindLineHits(Vector2 point, float tolerance)
    {
        var best = new Dictionary<int, LineHit>();
        foreach (var line in lines)
        {
            if (line.st == null || line.st.Count < 2) continue;
            int segCount = line.st.Count - 1;
            int spanStart = 0;
            while (spanStart < segCount)
            {
                string sr = SegmentRegion(line.st[spanStart], line.st[spanStart + 1]);
                if (string.IsNullOrEmpty(sr) || sr != currentRegion) { spanStart++; continue; }
                int spanEnd = spanStart;
                while (spanEnd + 1 < segCount && SegmentRegion(line.st[spanEnd + 1], line.st[spanEnd + 2]) == sr)
                    spanEnd++;

                // 用整段连续 span 的平滑曲线做命中，而非逐段直线
                FillRegionSpanPolyline(line, spanStart, spanEnd, currentRegion, scratchPolyline, scratchSubSegIndices);
                int curSpanStart = spanStart;
                spanStart = spanEnd + 1;
                float d = float.MaxValue;
                int hitSeg = -1;
                for (int k = 0; k < scratchPolyline.Count - 1; k++)
                {
                    float sd = DistToSeg(point, scratchPolyline[k], scratchPolyline[k + 1]);
                    if (sd < d)
                    {
                        d = sd;
                        hitSeg = k < scratchSubSegIndices.Count ? scratchSubSegIndices[k] : curSpanStart;
                    }
                }
                if (d > tolerance || hitSeg < 0 || hitSeg >= segCount) continue;
                LineHit h;
                if (!best.TryGetValue(line.id, out h) || d < h.dist)
                    best[line.id] = new LineHit { line = line, seg = hitSeg, dist = d };
            }
        }
        var list = new List<LineHit>(best.Values);
        list.Sort((a, b) => a.line.id.CompareTo(b.line.id));
        return list;
    }

    float SegmentLoadRatio(Line line, int a, int b)
    {
        if (line == null || traffic == null || traffic.lineSegLoad == null) return 0f;
        string sk = line.id + ":" + Mathf.Min(a, b) + ":" + Mathf.Max(a, b);
        return traffic.lineSegLoad.TryGetValue(sk, out float load) ? load : 0f;
    }

    void QuickBoostLineSegment(Line line)
    {
        if (line == null || !lines.Contains(line)) return;
        if (!PathTouchesRegion(line.st, currentRegion)) { Toast("请先切换到该航线所在区域调整飞车"); return; }
        int slot = DispatchSlot();
        if (line.dispatch[slot] < line.trains)
        {
            BeginUndo("热力快捷增班");
            line.dispatch[slot] = Mathf.Min(line.trains, line.dispatch[slot] + 1);
            operatingRates.Clear();
            RecalcIncome(); EndUndo(); SaveGame(); RefreshHUD();
            ShowForecastDelayToast();
            return;
        }
        if (line.trains >= 8) { OpenLineFleet(line, () => GoHome()); return; }
        foreach (var v in researchVehicles)
        {
            if (v.lineId == 0)
            {
                string error = LineFleetMoveError(line, v, true);
                if (error != null) { Toast(error); return; }
                BeginUndo("热力快捷调配飞车");
                MoveLineFleetVehicle(line, v, true);
                line.dispatch[slot] = line.trains;
                operatingRates.Clear();
                RecalcIncome(); EndUndo(); SaveGame(); RefreshHUD();
                ShowForecastDelayToast();
                return;
            }
        }
        OpenLineFleet(line, () => GoHome());
    }

    void ShowLineBubbles(Vector2 at, List<LineHit> hits)
    {
        CloseLineBubble();
        lineBubbleScrim = Rect("LineScrim", root, 0, 0, W, H);
        var simg = Img(lineBubbleScrim, new Color(0.10f,0.09f,0.08f,0.035f));
        var sb = lineBubbleScrim.AddComponent<Button>();
        sb.transition = Selectable.Transition.None; sb.targetGraphic = simg;
        sb.onClick.AddListener(CloseLineBubble);
        lineBubbleRoot = Rect("LineBubbles", lineBubbleScrim.transform, 0, 0, W, H);
        lineBubbleRoot.transform.SetAsLastSibling();

        const float bw = 556f, gap = 14f;
        var heights = new float[hits.Count];
        float total = (hits.Count - 1) * gap;
        for (int i = 0; i < hits.Count; i++)
        {
            var hHit = hits[i];
            int ha = hHit.line.st[hHit.seg], hb = hHit.line.st[hHit.seg + 1];
            float load = SegmentLoadRatio(hHit.line, ha, hb);
            bool showBoost = IsInEventSandbox() || load >= 0.72f;
            heights[i] = showBoost ? 236f : 172f;
            total += heights[i];
        }
        float x = Mathf.Clamp(at.x - bw / 2f, 16f, W - bw - 16f);
        float above = at.y + 24f;
        float groupY = above + total <= H - 250f ? above : at.y - total - 24f;
        groupY = Mathf.Clamp(groupY, 210f, H - 240f - total);
        float cursorTop = groupY + total;

        for (int i = 0; i < hits.Count; i++)
        {
            var hit = hits[i];
            var line = hit.line;
            if (!lines.Contains(line)) continue;
            int a = line.st[hit.seg], b = line.st[hit.seg + 1];
            float load = SegmentLoadRatio(line, a, b);
            bool showBoost = IsInEventSandbox() || load >= 0.72f;
            float bh = heights[i];
            float y = cursorTop - bh;
            cursorTop = y - gap;

            var card = Rect("LBubble" + line.id, lineBubbleRoot.transform, x, y, bw, bh);
            Rounded(card, new Color(1f, 0.985f, 0.95f, 0.985f)); SoftShadow(card);
            Img(Rect("Accent", card.transform, 0, 12, 6, bh - 24), line.color, spriteRound).raycastTarget = false;
            Txt(card.transform, line.name, 28, Ink, 20, bh - 54, 170, 38, TextAnchor.MiddleLeft);
            Txt(card.transform, StationDisplayName(a, currentRegion) + " ⇄ " + StationDisplayName(b, currentRegion), 23, InkSoft, 194, bh - 52, 342, 36, TextAnchor.MiddleLeft);

            if (IsInEventSandbox())
            {
                var saved = FindSandboxLine(line);
                var vehicle = saved != null ? GetSandboxVehicleDef(activeSandboxEventId, saved.vehicleKey) : null;
                string status = load >= 0.95f ? "爆满拥挤" : load >= 0.72f ? "运力吃紧" : "运行通畅";
                Txt(card.transform, $"运力 {Mathf.RoundToInt(SandboxSavedLineCapacity(saved, GetOrCreateEventSandbox(activeSandboxEventId)))} 人/日  负荷 {Mathf.RoundToInt(load * 100f)}%  {status}", 20, load >= 0.95f ? Vermilion : load >= 0.72f ? Gold : Matcha, 20, 115, 516, 30, TextAnchor.MiddleLeft);
                Txt(card.transform, $"{(vehicle != null ? vehicle.name : "基础车型")}  载客 ×{(vehicle != null ? vehicle.capMult : 1f):0.##}  速度 ×{(vehicle != null ? vehicle.speedMult : 1f):0.##}", 18, InkSoft, 20, 90, 516, 25, TextAnchor.MiddleLeft);
            }
            else if (showBoost)
            {
                int pct = Mathf.RoundToInt(load * 100f);
                Color loadCol = load >= 0.95f ? Vermilion : load >= 0.72f ? new Color(0.84f, 0.56f, 0.12f) : Matcha;
                string statusText = load >= 0.95f ? "负荷 " + pct + "%  爆满拥挤" : load >= 0.72f ? "负荷 " + pct + "%  运力吃紧" : "负荷 " + pct + "%  运行通畅";
                Txt(card.transform, statusText + "（发车 " + RunningTrains(line) + "/" + line.trains + "）", 21, loadCol, 20, 114, 326, 40, TextAnchor.MiddleLeft);

                int slot = DispatchSlot();
                bool canAddRun = line.dispatch[slot] < line.trains;
                string boostLabel = canAddRun ? "⚡ 增班+1" : "⚡ 调配飞车";
                Color boostBg = canAddRun ? Matcha : (load >= 0.72f ? Vermilion : Indigo);
                var boostBtn = Btn(card.transform, "QuickBoost", boostLabel, 22, 354, 108, 182, 52, boostBg, () => {
                    CloseLineBubble();
                    QuickBoostLineSegment(line);
                });
                TInk(boostBtn, Color.white);
            }

            var b1 = Btn(card.transform, "CutSeg", "拆该段", 24, 20, 16, 160, 78, new Color(0.965f,0.895f,0.85f), () => {
                CloseLineBubble();
                Confirm("拆除该段", SegmentConfirmText(line, hit.seg), () => DoDemolishSegment(line, hit.seg));
            });
            TInk(b1, Vermilion);
            var b2 = Btn(card.transform, "CutLine", "拆整线", 24, 198, 16, 160, 78, new Color(0.86f,0.38f,0.29f), () => {
                CloseLineBubble();
                Confirm("拆除 " + line.name, LineConfirmText(line), () => DoDemolishLine(line));
            });
            TInk(b2, Color.white);
            var b3 = Btn(card.transform, "Info", "详 情", 24, 376, 16, 160, 78, Indigo, () => {
                CloseLineBubble();
                OpenLineInfo(line, hit.seg);
            });
            TInk(b3, Color.white);
        }
    }

    string SegmentConfirmText(Line line, int seg)
    {
        if (line == null || seg < 0 || seg >= line.st.Count - 1) return "";
        int a = line.st[seg], b = line.st[seg + 1];
        int refund = Mathf.RoundToInt(EvaluateLineSegmentCost(line, seg, TrackBuildCostFactor(), out _) * DemolishRefundRate());
        string tail = IsInEventSandbox() ? "\n活动线路可随时免费调整。" : line.st.Count <= 2 ? "\n该段拆除后，这条航线会消失。" : "\n拆除后退还 ¥" + refund + "。";
        return StationDisplayName(a, currentRegion) + " ⇄ " + StationDisplayName(b, currentRegion) + tail;
    }
    string LineConfirmText(Line line)
    {
        if (line == null) return "";
        int refund = Mathf.RoundToInt(120f * (UniqueCount(line.st) - 1) * TrackBuildCostFactor() * DemolishRefundRate());
        return UniqueCount(line.st) + " 港  " + line.trains + " 星舰\n" + (IsInEventSandbox() ? "活动线路可随时免费调整。" : "拆除后退还 ¥" + refund + "。");
    }

    void DoDemolishSegment(Line line, int seg)
    {
        if (line == null || !lines.Contains(line) || seg < 0 || seg >= line.st.Count - 1) return;
        int n = line.st.Count;
        if (IsInEventSandbox() && !IsLoop(line.st) && n > 3 && seg > 0 && seg < n - 2 && lines.Count >= LineLimit()) { Toast("航线已达上限，无法拆分该段"); return; }
        if (!IsInEventSandbox() && !IsLoop(line.st) && n > 3 && seg > 0 && seg < n - 2) {
            var head = line.st.GetRange(0, seg + 1); var tailPath = line.st.GetRange(seg + 1, n - seg - 1);
            for (int r = 0; r < RegionOrder.Length; r++) {
                string region = RegionOrder[r];
                int after = RegionLineCount(region) - (PathTouchesRegion(line.st, region) ? 1 : 0)
                    + (PathTouchesRegion(head, region) ? 1 : 0) + (PathTouchesRegion(tailPath, region) ? 1 : 0);
                if (after > RegionLineLimit(region)) { Toast(region + "航线已达上限，无法拆分该段"); return; }
            }
        }
        int a = line.st[seg], b = line.st[seg + 1];
        int refund = Mathf.RoundToInt(EvaluateLineSegmentCost(line, seg, TrackBuildCostFactor(), out _) * DemolishRefundRate());
        BeginUndo("拆除航线段");
        SyncLineWaypointsCount(line);
        string msg;

        if (IsLoop(line.st))
        {
            var opened = new List<int>();
            var openedWp = new List<SegmentWaypoints>();
            int segCount = n - 1;
            for (int i = 0; i < segCount; i++) opened.Add(line.st[(seg + 1 + i) % segCount]);
            for (int i = 0; i < segCount - 1; i++)
            {
                int srcIdx = (seg + 1 + i) % segCount;
                openedWp.Add(new SegmentWaypoints { pts = new List<Vector2>(line.segWaypoints[srcIdx].pts) });
            }
            line.st = opened;
            line.segWaypoints = openedWp;
            if (!IsInEventSandbox()) coins += refund;
            msg = "已拆除一段，环线变为普通航线";
        }
        else if (n <= 2)
        {
            if (!IsInEventSandbox()) { SettleRemovedLineTax(line); ReleaseLineFleet(line); } lines.Remove(line);
            if (!IsInEventSandbox()) coins += refund;
            msg = "已拆除 " + line.name;
        }
        else if (seg == 0)
        {
            line.st.RemoveAt(0);
            line.segWaypoints.RemoveAt(0);
            if (!IsInEventSandbox()) coins += refund;
            msg = "已拆除一段：" + StationDisplayName(a, currentRegion) + " ⇄ " + StationDisplayName(b, currentRegion);
        }
        else if (seg == n - 2)
        {
            line.st.RemoveAt(n - 1);
            line.segWaypoints.RemoveAt(line.segWaypoints.Count - 1);
            if (!IsInEventSandbox()) coins += refund;
            msg = "已拆除一段：" + StationDisplayName(a, currentRegion) + " ⇄ " + StationDisplayName(b, currentRegion);
        }
        else
        {
            var tail = line.st.GetRange(seg + 1, n - seg - 1);
            var tailWp = CloneWaypointsList(line.segWaypoints.GetRange(seg + 1, line.segWaypoints.Count - (seg + 1)));
            line.st.RemoveRange(seg + 1, n - seg - 1);
            line.segWaypoints.RemoveRange(seg, line.segWaypoints.Count - seg);
            if (!IsInEventSandbox()) coins += refund;
            var nl = new Line {
                id = nextLineId,
                name = nextLineId + " 号线",
                color = LineColors[(nextLineId - 1) % LineColors.Length]
            };
            nl.st = tail;
            nl.segWaypoints = tailWp;
            SyncLineWaypointsCount(nl);
            lines.Add(nl);
            nextLineId++;
            msg = "航线拆分为 " + line.name + " / " + nl.name;
        }

        CreateLineVisuals(); SyncMainLinesToSandbox(); if (IsInEventSandbox()) SaveSandboxStorage(); RefreshStationDots(); RecalcIncome(); RefreshHUD();
        EndUndo(); Toast(msg);
    }

    void DoDemolishLine(Line line)
    {
        if (line == null || !lines.Contains(line)) return;
        BeginUndo("拆除整条航线");
        float refund = 120f * (UniqueCount(line.st) - 1) * TrackBuildCostFactor() * DemolishRefundRate();
        if (!IsInEventSandbox()) coins += refund; if (!IsInEventSandbox()) { SettleRemovedLineTax(line); ReleaseLineFleet(line); } lines.Remove(line);
        CreateLineVisuals(); SyncMainLinesToSandbox(); if (IsInEventSandbox()) SaveSandboxStorage(); RefreshStationDots(); RecalcIncome(); RefreshHUD();
        EndUndo(); Toast("已拆除 " + line.name);
    }

    void OpenLineInfo(Line line, int seg)
    {
        if (line == null || !lines.Contains(line)) return;
        CloseLineBubble();
        if (IsInEventSandbox()) { OpenMainSandboxLineInfo(line, seg); return; }
        bool isLoop = IsLoop(line.st);
        string pathText = isLoop
            ? "环线  具体途经站请查看「航线详情」"
            : "沿线 " + UniquePathText(line.st);
        float pathHeight = Mathf.Clamp(MeasureTextHeight(pathText, 19, 860) + 6, 48, 280);
        float extra = pathHeight - 48;
        float h = 1180f + extra;
        var p = OpenPopup("", h, 960, "line_info_" + line.id);
        panelForecastRefresh = () => { if (activePanel != null && activePanelKey == "line_info_" + line.id) OpenLineInfo(line, seg); };
        BuildLineNameEditor(p.transform, line, h);
        Img(Rect("LineColor", p.transform, 36, h - 124, 888, 8), line.color, spriteRound).raycastTarget = false;

        int unique = UniqueCount(line.st);
        int recentSum = 0;
        for (int i = 0; i < line.recent.Count; i++) recentSum += line.recent[i];

        var forecast = ForecastFor(line);
        string pendingTag = pendingForecastMask != 0 ? " 测算中" : "";
        StatBox(p.transform, "预计日收入" + pendingTag, Mathf.RoundToInt(forecast.gross) + "", 34, h - 300, 430, 150, Gold);
        StatBox(p.transform, "预计日利润" + pendingTag, Mathf.RoundToInt(forecast.Profit) + "", 496, h - 300, 430, 150, Matcha);
        StatBox(p.transform, "客 流", Mathf.RoundToInt(LinePassengerFlow(line)) + "", 34, h - 470, 430, 150, Indigo);
        StatBox(p.transform, "单向区段运力", Mathf.RoundToInt(LineCapacity(line)) + "", 496, h - 470, 430, 150, Sakura);

        Txt(p.transform, "近 7 日净利润", 24, Ink, 36, 675 + extra, 420, 34, TextAnchor.MiddleLeft);
        if (line.recent.Count == 0)
        {
            for (int i = 0; i < 7; i++)
            {
                var ghost = Rect("GhostBar", p.transform, 60 + i * 120, 505 + extra, 64, 135);
                Img(ghost, new Color(0.90f, 0.87f, 0.81f, 0.42f), spriteRound).raycastTarget = false;
            }
            Img(Rect("ChartBase", p.transform, 36, 500 + extra, 888, 2), new Color(0.55f,0.50f,0.42f,0.28f)).raycastTarget = false;
            Txt(p.transform, "首日运营中  今日收入 ¥" + line.todayRevenue + "\n日结后生成近 7 日利润曲线", 23, InkSoft, 36, 540 + extra, 860, 74, TextAnchor.UpperLeft);
        }
        else
        {
            Txt(p.transform, "已结算 " + line.recent.Count + " 日  日均 ¥" + (recentSum / line.recent.Count), 22, InkSoft, 450, 678 + extra, 460, 34, TextAnchor.MiddleRight);
            int max = 1;
            foreach (int v in line.recent) if (Mathf.Abs(v) > max) max = Mathf.Abs(v);
            int start = 7 - line.recent.Count;
            for (int i = 0; i < 7; i++)
            {
                float bx = 60 + i * 120;
                Img(Rect("BarBg", p.transform, bx, 505 + extra, 64, 135), new Color(0.90f, 0.87f, 0.81f, 0.55f), spriteRound).raycastTarget = false;
                if (i >= start)
                {
                    int v = line.recent[i - start];
                    float bh = Mathf.Max(3f, 67f * Mathf.Abs(v) / max);
                    Img(Rect("Bar", p.transform, bx, (v >= 0 ? 572 : 572-bh) + extra, 64, bh), v >= 0 ? line.color : Vermilion, spriteRound).raycastTarget = false;
                }
            }
        }

        Txt(p.transform, "运 营", 26, Ink, 36, 470 + extra, 300, 36, TextAnchor.MiddleLeft);
        Txt(p.transform, "星港 " + unique + "  星舰 " + line.trains + "/8  航线长度 " + FormatLineKm(line.st), 23, Ink, 36, 420 + extra, 860, 36, TextAnchor.MiddleLeft);
        Txt(p.transform, "最高区段需求 " + Mathf.RoundToInt(LinePeakSectionDemand(line)) + " 人次/日  平均速度 " + Mathf.RoundToInt(FleetTrait(line, 2)*100) + "%", 23, InkSoft, 36, 382 + extra, 860, 34, TextAnchor.MiddleLeft);
        if (seg >= 0 && seg < line.st.Count - 1)
            Txt(p.transform, "选中段：" + stations[line.st[seg]].name + " ⇄ " + stations[line.st[seg + 1]].name, 22, line.color, 36, 344 + extra, 860, 34, TextAnchor.MiddleLeft);
        ScrollableText(p.transform, pathText, 22, InkSoft, 36, 296, 860, pathHeight);

        Txt(p.transform, "预计成本：星舰 ¥" + Mathf.RoundToInt(forecast.vehicle) + "  航线 ¥" + Mathf.RoundToInt(forecast.track) + "  税 ¥" + Mathf.RoundToInt(forecast.tax), 23, InkSoft, 36, 244, 860, 38, TextAnchor.MiddleLeft);
        var planning = Btn(p.transform, "Operations", "票价与调度", 25, 34, 132, 892, 70, Indigo, () => { ClosePanel(true); OpenLineOperations(line); }); TInk(planning, Color.white);
        Btn(p.transform, "LineDetail", "航线详情 / 星舰", 26, 34, 34, 430, 82, Indigo, () => {
            ClosePanel(); OpenLineScreen(line.id, () => OpenLineInfo(line, seg));
        });
        var close = Btn(p.transform, "InfoClose", "关 闭", 26, 496, 34, 430, 82, new Color(0.92f,0.89f,0.83f), ClosePanel);
        TInk(close, Ink);
    }

    void OpenMainSandboxLineInfo(Line line, int seg)
    {
        var p = OpenPopup("线路信息", 540, 960, "sandbox_main_line_info_" + line.id);
        BuildLineNameEditor(p.transform, line, 540);
        Txt(p.transform, UniquePathText(line.st), 22, InkSoft, 36, 340, 860, 50, TextAnchor.MiddleLeft);
        var st = GetOrCreateEventSandbox(activeSandboxEventId);
        var spec = GetEventSpec(activeSandboxEventId);
        var saved = FindSandboxLine(line);
        var vehicle = saved != null ? GetSandboxVehicleDef(activeSandboxEventId, saved.vehicleKey) : null;
        float load = line.st.Count > 1 ? SegmentLoadRatio(line, line.st[Mathf.Clamp(seg, 0, line.st.Count - 2)], line.st[Mathf.Clamp(seg, 0, line.st.Count - 2) + 1]) : 0f;
        string status = load >= 0.95f ? "爆满拥挤" : load >= 0.72f ? "运力吃紧" : "运行通畅";
        Txt(p.transform, $"车辆 {line.trains}  运力 {Mathf.RoundToInt(SandboxSavedLineCapacity(saved, st))} 人/日  负荷 {Mathf.RoundToInt(load * 100f)}%  {status}", 22, Ink, 36, 292, 860, 38, TextAnchor.MiddleLeft);
        Txt(p.transform, $"{(vehicle != null ? vehicle.name : "基础车型")}  载客 ×{(vehicle != null ? vehicle.capMult : 1f):0.##}  速度 ×{(vehicle != null ? vehicle.speedMult : 1f):0.##}    {spec.tokenName}：{st.tokens}", 20, Indigo, 36, 258, 860, 32, TextAnchor.MiddleLeft);
        var add = Btn(p.transform, "AddCar", "+1 辆（4 券）", 24, 36, 200, 210, 65, Matcha, () => {
            if (line.trains >= 4) { Toast("车辆已满"); return; }
            if (st.tokens < 4) { Toast(spec.tokenName + "不足"); return; }
            st.tokens -= 4; line.trains++; CreateLineVisuals(); SyncMainLinesToSandbox(); RefreshSandboxHudText(); SaveSandboxStorage();
            ClosePanel(true); OpenMainSandboxLineInfo(line, seg);
        }); TInk(add, Color.white);
        var remove = Btn(p.transform, "RemoveCar", "减少车辆", 24, 260, 200, 210, 65, Vermilion, () => {
            if (line.trains <= 1) { Toast("至少保留 1 辆车"); return; }
            line.trains--; CreateLineVisuals(); SyncMainLinesToSandbox(); SaveSandboxStorage();
            ClosePanel(true); OpenMainSandboxLineInfo(line, seg);
        }); TInk(remove, Color.white);
        for (int i = 0; i < LineColors.Length; i++) {
            Color color = LineColors[i];
            var button = Btn(p.transform, "Color" + i, "", 20, 36 + i * 100, 115, 80, 60, color, () => {
                line.color = color; CreateLineVisuals(); SyncMainLinesToSandbox(); SaveSandboxStorage();
                ClosePanel(true); OpenMainSandboxLineInfo(line, seg);
            });
        }
        var close = Btn(p.transform, "Close", "关闭", 24, 680, 20, 240, 65, Paper2, ClosePanel); TInk(close, Ink);
    }

    void BuildLineNameEditor(Transform parent, Line line, float h)
    {
        var nameButton = Btn(parent, "RenameLine", "", 44, 28, h - 82, 430, 68,
            new Color(0.94f, 0.91f, 0.85f, 0.45f), () => { });
        var label = nameButton.GetComponentInChildren<Text>();
        label.text = DisplayPlaceAliases(line.name) + "  ✎";
        label.color = Ink;
        label.alignment = TextAnchor.MiddleLeft;
        label.resizeTextForBestFit = true;
        label.resizeTextMinSize = 24;
        label.resizeTextMaxSize = ScaledFontSize(44);
        label.horizontalOverflow = HorizontalWrapMode.Overflow;
        label.verticalOverflow = VerticalWrapMode.Truncate;
        Txt(parent, "运营详情", 25, InkSoft, 480, h - 77, 200, 50, TextAnchor.MiddleLeft);

        var editor = Rect("RenameEditor", parent, 28, h - 88, 740, 76);
        Rounded(editor, new Color(0.99f, 0.98f, 0.95f, 1f));
        var inputBox = Rect("NameInput", editor.transform, 0, 0, 470, 76);
        Rounded(inputBox, new Color(1, 1, 1, 1));
        var inputText = Txt(inputBox.transform, "", 36, Ink, 14, 4, 442, 68, TextAnchor.MiddleLeft);
        inputText.supportRichText = false;
        inputText.horizontalOverflow = HorizontalWrapMode.Overflow;
        var input = inputBox.AddComponent<InputField>();
        input.targetGraphic = inputBox.GetComponent<Image>();
        input.textComponent = inputText;
        input.lineType = InputField.LineType.SingleLine;
        input.characterLimit = 12;
        input.text = DisplayPlaceAliases(line.name);
        var save = Btn(editor.transform, "Save", "确定", 26, 482, 0, 120, 76, Matcha, () => {
            string value = input.text.Trim();
            if (string.IsNullOrWhiteSpace(value)) { Toast("航线名称不能为空"); return; }
            if (line.name != value) { BeginUndo("修改航线名称"); line.name = value; SyncMainLinesToSandbox(); if (IsInEventSandbox()) SaveSandboxStorage(); EndUndo(); }
            label.text = DisplayPlaceAliases(line.name) + "  ✎";
            editor.SetActive(false);
            nameButton.gameObject.SetActive(true);
            Toast("航线名称已修改");
        });
        TInk(save, Color.white);
        var cancel = Btn(editor.transform, "Cancel", "取消", 26, 612, 0, 120, 76,
            new Color(0.92f, 0.89f, 0.83f), () => {
                editor.SetActive(false);
                nameButton.gameObject.SetActive(true);
            });
        TInk(cancel, Ink);
        editor.SetActive(false);
        nameButton.onClick.RemoveAllListeners();
        nameButton.onClick.AddListener(() => {
            input.text = DisplayPlaceAliases(line.name);
            nameButton.gameObject.SetActive(false);
            editor.SetActive(true);
            input.ActivateInputField();
        });
    }

    float PathLength(List<int> path)
    {
        float total = 0;
        for (int i = 0; i < path.Count - 1; i++) { string region = SegmentRegion(path[i], path[i+1]); if (region != null) total += Vector2.Distance(PosInRegion(path[i],region), PosInRegion(path[i+1],region)); }
        return total;
    }

    bool lineInsertActive;
    Line insertingLine;
    int insertingSegment = -1, insertTarget = -1;
    int insertEditWpIndex = -1;
    readonly List<Vector2> insertTempWaypoints = new List<Vector2>(16);
    bool insertHasGridPoint, insertGridBlocked;
    Vector2 insertGridPos;
    LinePolylineGraphic insertPreviewPoly;
    Image insertPreviewA, insertPreviewB;
    GameObject insertBadge;
    Text insertBadgeText;
    readonly List<GameObject> insertWpHandles = new List<GameObject>();
    readonly List<Vector2> scratchInsertWps = new List<Vector2>(32);
    LinePolylineGraphic insertDimmedPoly;

    public bool BeginLineInsert(int lineId, int segment, Vector2 screen)
    {
        if (dragging || lineInsertActive || pageRoot || activePanel || sceneryViewer) return false;
        var line = lines.Find(item => item.id == lineId);
        if (line == null || segment < 0 || segment >= line.st.Count - 1 ||
            SegmentRegion(line.st[segment], line.st[segment + 1]) != currentRegion) return false;
        CloseLineBubble();
        insertingLine = line; insertingSegment = segment; insertTarget = -1;
        insertHasGridPoint = false; insertGridBlocked = false;
        insertTempWaypoints.Clear();
        if (line.segWaypoints != null && segment < line.segWaypoints.Count && line.segWaypoints[segment] != null && line.segWaypoints[segment].pts != null)
            insertTempWaypoints.AddRange(line.segWaypoints[segment].pts);

        insertEditWpIndex = -1;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(mapRect, screen, null, out var startLocal))
        {
            float wpPickR = 36f / Mathf.Max(0.45f, mapRect.localScale.x);
            float bestWpDist = wpPickR;
            for (int k = 0; k < insertTempWaypoints.Count; k++)
            {
                float d = Vector2.Distance(startLocal, insertTempWaypoints[k]);
                if (d < bestWpDist) { bestWpDist = d; insertEditWpIndex = k; }
            }
            if (insertEditWpIndex < 0)
            {
                // 未按在现有拐点上，判断按在哪两个控制点之间并插入新拐点占位
                scratchControlPts.Clear();
                scratchControlPts.Add(Pos(line.st[segment]));
                for (int k = 0; k < insertTempWaypoints.Count; k++) scratchControlPts.Add(insertTempWaypoints[k]);
                scratchControlPts.Add(Pos(line.st[segment + 1]));
                int bestSub = 0;
                float bestSegDist = float.MaxValue;
                for (int k = 0; k < scratchControlPts.Count - 1; k++)
                {
                    float sd = DistToSeg(startLocal, scratchControlPts[k], scratchControlPts[k + 1]);
                    if (sd < bestSegDist) { bestSegDist = sd; bestSub = k; }
                }
                insertEditWpIndex = Mathf.Clamp(bestSub, 0, insertTempWaypoints.Count);
                insertTempWaypoints.Insert(insertEditWpIndex, TerrainField.SnapToGrid(startLocal, currentRegion));
            }
        }
        else
        {
            insertEditWpIndex = insertTempWaypoints.Count;
            insertTempWaypoints.Add(Pos(line.st[segment]));
        }

        lineInsertActive = true;
        // 预览时淡化原线所在区域整段（同一 span 的控制点段共用一个 poly），避免新旧线重叠难辨
        insertDimmedPoly = null;
        if (lineVisuals != null && lineVisuals.TryGetValue(line.id, out var lvInsert) && lvInsert != null)
        {
            for (int i = 0; i < lvInsert.segments.Count; i++)
            {
                var segv = lvInsert.segments[i];
                if (segv.logicSegIndex == segment && segv.poly != null)
                {
                    insertDimmedPoly = segv.poly;
                    insertDimmedPoly.bodyColor.a = 0.22f;
                    insertDimmedPoly.SetVerticesDirty();
                    break;
                }
            }
        }
        SetDotGridHighlight(true);
        var polyGo = Rect("InsertPreviewPoly", previewLayer, 0, 0, W, H);
        insertPreviewPoly = polyGo.AddComponent<LinePolylineGraphic>();
        insertPreviewPoly.raycastTarget = false;
        insertBadge = Rect("InsertCostBadge", root, 0, 0, 380, 62);
        Rounded(insertBadge, new Color(1f, 0.98f, 0.92f, 0.96f));
        insertBadge.GetComponent<Image>().raycastTarget = false;
        insertBadgeText = Txt(insertBadge.transform, "拖至新星港或点阵拐点", 22, Ink, 12, 5, 356, 52, TextAnchor.MiddleCenter);
        Haptic.Vibrate(22); PlayTone(false); UpdateLineInsert(screen);
        RefreshInsertWaypointHandles();
        return true;
    }
    bool CanInsertStation(Line line, int segment, int target)
    {
        if (line == null || !lines.Contains(line) || segment < 0 || segment >= line.st.Count - 1 ||
            target < 0 || target >= stations.Count || line.st.Contains(target) || (IsInEventSandbox() && stations[target].closed)) return false;
        int a = line.st[segment], b = line.st[segment + 1];
        if (stations[target].portal && (stations[a].portal || stations[b].portal)) return false;
        return StationInRegion(target, currentRegion) && SegmentRegion(a, b) == currentRegion &&
            SegmentRegion(a, target) == currentRegion && SegmentRegion(target, b) == currentRegion;
    }
    int InsertStationCost(Line line, int segment, int target)
    {
        int a = line.st[segment], b = line.st[segment + 1];
        float f = TrackBuildCostFactor();
        int before = EvaluateLineSegmentCost(line, segment, f, out _);
        int after = EvaluateStationPairCost(a, target, f, out _) + EvaluateStationPairCost(target, b, f, out _);
        return Mathf.Max(0, after - before) + StationOpeningCost(target);
    }
    void EvaluateSegmentWaypointPreview(Line line, int segment, IReadOnlyList<Vector2> candidateWps, out int beforePct, out int afterPct, out bool blocked)
    {
        int a = line.st[segment], b = line.st[segment + 1];
        float f = TrackBuildCostFactor();
        EvaluateLineSegmentCost(line, segment, f, out var beforeTr);
        beforePct = Mathf.RoundToInt(beforeTr.penaltyRatio * 100f);
        scratchControlPts.Clear();
        scratchControlPts.Add(Pos(a));
        if (candidateWps != null)
            for (int i = 0; i < candidateWps.Count; i++) scratchControlPts.Add(candidateWps[i]);
        scratchControlPts.Add(Pos(b));
        TerrainField.SampleCatmullRomPolyline(scratchControlPts, false, scratchPolyline);
        TerrainField.EvaluatePolyline(currentRegion, scratchPolyline, out var afterTr);
        afterPct = Mathf.RoundToInt(afterTr.penaltyRatio * 100f);
        blocked = afterTr.blocked;
    }

    static void CleanupRedundantWaypoints(List<Vector2> wps, Vector2 startPos, Vector2 endPos)
    {
        // 第一轮：移除与前后点距离过近的拐点
        for (int i = wps.Count - 1; i >= 0; i--)
        {
            Vector2 prev = i > 0 ? wps[i - 1] : startPos;
            Vector2 next = i + 1 < wps.Count ? wps[i + 1] : endPos;
            if (Vector2.Distance(wps[i], prev) < 18f || Vector2.Distance(wps[i], next) < 18f)
                wps.RemoveAt(i);
        }
        // 第二轮：移除几乎拖直的拐点（进/出方向 dot > 0.990，夹角 > 172°）
        for (int i = wps.Count - 1; i >= 0; i--)
        {
            Vector2 prev = i > 0 ? wps[i - 1] : startPos;
            Vector2 next = i + 1 < wps.Count ? wps[i + 1] : endPos;
            Vector2 inDir = (wps[i] - prev).normalized;
            Vector2 outDir = (next - wps[i]).normalized;
            if (inDir.sqrMagnitude < 0.0001f || outDir.sqrMagnitude < 0.0001f) continue;
            if (Vector2.Dot(inDir, outDir) > 0.990f)
                wps.RemoveAt(i);
        }
    }

    public void UpdateLineInsert(Vector2 screen)
    {
        if (!lineInsertActive || insertingLine == null || !lines.Contains(insertingLine) ||
            insertingSegment < 0 || insertingSegment >= insertingLine.st.Count - 1) return;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(mapRect, screen, null, out var local)) return;
        float snap = 44f / Mathf.Max(1f, mapRect.localScale.x);
        int near = NearestStationAny(local, snap);
        if (IsInEventSandbox() && (near < 0 || stations[near].closed || !CanInsertStation(insertingLine, insertingSegment, near)))
        {
            near = -1; float best = float.MaxValue;
            for (int i = 0; i < stations.Count; i++) {
                if (stations[i].closed || !IsStationShown(i) || !CanInsertStation(insertingLine, insertingSegment, i)) continue;
                float distance = Vector2.Distance(local, Pos(i));
                if (distance < SnapRadius(i, snap) && distance < best) { best = distance; near = i; }
            }
        }
        insertTarget = CanInsertStation(insertingLine, insertingSegment, near) ? near : -1;

        Vector2 pa = Pos(insertingLine.st[insertingSegment]);
        Vector2 pb = Pos(insertingLine.st[insertingSegment + 1]);
        Color previewCol = insertingLine.color;
        if (insertTarget >= 0)
        {
            insertHasGridPoint = false;
            insertGridBlocked = false;
            Vector2 point = Pos(insertTarget);
            SplitWaypointsForInsertedStation(insertingLine, insertingSegment, pa, pb, point, out var leftWps, out var rightWps);

            // 该段替换为 pa -> leftWps -> point -> rightWps -> pb，其余段保留原拐点
            scratchInsertWps.Clear();
            for (int i = 0; i < leftWps.Count; i++) scratchInsertWps.Add(leftWps[i]);
            scratchInsertWps.Add(point);
            for (int i = 0; i < rightWps.Count; i++) scratchInsertWps.Add(rightWps[i]);

            // 与整线一致：找出插入段所在 currentRegion 的连续 [spanStart, spanEnd]，
            // 展开整线控制点并用 spanIsLoop 做连续平滑预览（替换段走上面拼好的链）
            int segCount = insertingLine.st.Count - 1;
            int spanStart = insertingSegment;
            while (spanStart > 0 && SegmentRegion(insertingLine.st[spanStart - 1], insertingLine.st[spanStart]) == currentRegion) spanStart--;
            int spanEnd = insertingSegment;
            while (spanEnd + 1 < segCount && SegmentRegion(insertingLine.st[spanEnd + 1], insertingLine.st[spanEnd + 2]) == currentRegion) spanEnd++;
            FillRegionSpanPolyline(insertingLine, spanStart, spanEnd, currentRegion, scratchPolyline, scratchSubSegIndices, insertingSegment, scratchInsertWps);

            if (insertBadgeText) insertBadgeText.text = IsInEventSandbox() ? "插入途经站" : "插入造价 ¥" + InsertStationCost(insertingLine, insertingSegment, insertTarget);
        }
        else if (IsInEventSandbox())
        {
            insertHasGridPoint = false; insertGridBlocked = false;
            scratchPolyline.Clear();
            if (insertBadgeText) insertBadgeText.text = "拖至可用站点";
        }
        else
        {
            insertHasGridPoint = true;
            insertGridPos = TerrainField.SnapToGrid(local, currentRegion);
            if (insertEditWpIndex >= 0 && insertEditWpIndex < insertTempWaypoints.Count)
                insertTempWaypoints[insertEditWpIndex] = insertGridPos;

            if ((Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Space)) && insertTempWaypoints.Count < 16)
            {
                int nextIdx = Mathf.Clamp(insertEditWpIndex + 1, 0, insertTempWaypoints.Count);
                insertTempWaypoints.Insert(nextIdx, insertGridPos);
                insertEditWpIndex = nextIdx;
                TriggerHaptic(8); PlaySnapSfx();
            }
            else if (Input.GetKeyDown(KeyCode.Backspace) && insertTempWaypoints.Count > 0)
            {
                int rmIdx = Mathf.Clamp(insertEditWpIndex, 0, insertTempWaypoints.Count - 1);
                insertTempWaypoints.RemoveAt(rmIdx);
                insertEditWpIndex = Mathf.Clamp(rmIdx - 1, -1, insertTempWaypoints.Count - 1);
                TriggerHaptic(6);
            }

            EvaluateSegmentWaypointPreview(insertingLine, insertingSegment, insertTempWaypoints, out int beforePct, out int afterPct, out insertGridBlocked);

            // 用整条线做连续平滑预览
            int segCount = insertingLine.st.Count - 1;
            int spanStart = insertingSegment;
            while (spanStart > 0 && SegmentRegion(insertingLine.st[spanStart - 1], insertingLine.st[spanStart]) == currentRegion) spanStart--;
            int spanEnd = insertingSegment;
            while (spanEnd + 1 < segCount && SegmentRegion(insertingLine.st[spanEnd + 1], insertingLine.st[spanEnd + 2]) == currentRegion) spanEnd++;
            FillRegionSpanPolyline(insertingLine, spanStart, spanEnd, currentRegion, scratchPolyline, scratchSubSegIndices, insertingSegment, insertTempWaypoints);

            if (insertGridBlocked) previewCol = new Color(0.96f, 0.24f, 0.22f, 0.95f);
            if (insertBadgeText)
            {
                if (insertGridBlocked)
                    insertBadgeText.text = "禁建天堑  请拖至安全点阵";
                else if (afterPct != beforePct)
                    insertBadgeText.text = $"地形惩罚 +{afterPct}%（原 +{beforePct}%）  {insertTempWaypoints.Count}拐点";
                else
                    insertBadgeText.text = $"地形惩罚 +{afterPct}%  {insertTempWaypoints.Count}拐点 (右键加点)";
            }
        }
        if (insertPreviewPoly)
            insertPreviewPoly.SetPolyline(scratchPolyline, Vector2.zero, previewCol, new Color(1f, 1f, 1f, 0.55f), 11f * overlayZoom, 2.5f * overlayZoom);
        RefreshInsertWaypointHandles();

        if (insertBadge && RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screen, null, out var badgePoint)) {
            var rect = insertBadge.GetComponent<RectTransform>();
            badgePoint -= root.rect.min;
            rect.anchoredPosition = new Vector2(Mathf.Clamp(badgePoint.x - 190f, 12f, W - 392f), Mathf.Clamp(badgePoint.y + 55f, 190f, H - 290f));
        }
        RefreshInsertWaypointHandles();
    }
    public void EndLineInsert(Vector2 screen)
    {
        if (!lineInsertActive) return;
        UpdateLineInsert(screen);
        var line = insertingLine; int segment = insertingSegment, target = insertTarget;
        bool useGrid = insertHasGridPoint, gridBlocked = insertGridBlocked;
        var committedWps = new List<Vector2>(insertTempWaypoints);
        CancelLineInsert();
        if (IsInEventSandbox()) { if (CanInsertStation(line, segment, target)) CommitLineInsertion(line, segment, target); return; }
        if (CanInsertStation(line, segment, target))
        {
            int cost = InsertStationCost(line, segment, target);
            string details = line.name + "  " + StationDisplayName(target, currentRegion) + "\n造价 ¥" + cost + "（含必要开港费用）";
            float detailsHeight = Mathf.Clamp(MeasureTextHeight(details, 25, 620) + 8, 54, H - 410);
            var p = OpenPopup("插入途经港", detailsHeight + 200, 680);
            ScrollableText(p.transform, details, 25, InkSoft, 28, 102, 620, detailsHeight);
            var confirm = Btn(p.transform, "ConfirmInsert", "确认 ¥" + cost, 25, 28, 20, 300, 66, Matcha, () => CommitLineInsertion(line, segment, target)); TInk(confirm, Color.white);
            var cancel = Btn(p.transform, "CancelInsert", "取消", 25, 350, 20, 300, 66, Paper2, ClosePanel); TInk(cancel, Ink);
            return;
        }
        if (useGrid && line != null && lines.Contains(line) && segment >= 0 && segment < line.st.Count - 1)
        {
            if (gridBlocked) { Toast("路径穿越裂谷/绝壁禁建区，未修改"); return; }
            var origPts = (line.segWaypoints != null && segment < line.segWaypoints.Count && line.segWaypoints[segment] != null)
                ? line.segWaypoints[segment].pts : null;
            // 若玩家仅点击未实质改变任何拐点（例如新增的占位格点清理后与原列表一致），不覆盖原路径
            CleanupRedundantWaypoints(committedWps, Pos(line.st[segment]), Pos(line.st[segment + 1]));
            bool unchanged = (origPts == null ? committedWps.Count == 0 : origPts.Count == committedWps.Count);
            if (unchanged && origPts != null)
            {
                for (int i = 0; i < origPts.Count; i++)
                    if (Vector2.Distance(origPts[i], committedWps[i]) > 2f) { unchanged = false; break; }
            }
            if (unchanged) return;
            EvaluateSegmentWaypointPreview(line, segment, committedWps, out _, out int afterPct, out bool finalBlocked);
            if (finalBlocked) { Toast("路径穿越裂谷/绝壁禁建区，未修改"); return; }
            BeginUndo("调整点阵拐点");
            SyncLineWaypointsCount(line);
            line.segWaypoints[segment].pts.Clear();
            line.segWaypoints[segment].pts.AddRange(committedWps);
            CreateLineVisuals(); RecalcIncome(); RefreshHUD();
            EndUndo();
            Toast($"已平滑整线（{committedWps.Count}拐点）  当前段地形惩罚 +{afterPct}%");
            return;
        }
        Toast("请拖到本区域内星港或安全点阵格点");
    }
    void SplitWaypointsForInsertedStation(
        Line line, int segment, Vector2 pa, Vector2 pb, Vector2 targetPos,
        out List<Vector2> leftWps, out List<Vector2> rightWps)
    {
        leftWps = new List<Vector2>();
        rightWps = new List<Vector2>();
        var oldPts = (line != null && line.segWaypoints != null && segment >= 0 && segment < line.segWaypoints.Count && line.segWaypoints[segment] != null)
            ? line.segWaypoints[segment].pts
            : null;

        if (oldPts != null && oldPts.Count > 0)
        {
            // 计算插入站点离原控制点链哪一段最近，将原拐点保留拆分给前后两段
            var chain = new List<Vector2>(oldPts.Count + 2) { pa };
            chain.AddRange(oldPts);
            chain.Add(pb);
            int splitSub = 0;
            float bestD = float.MaxValue;
            for (int k = 0; k < chain.Count - 1; k++)
            {
                float d = DistToSeg(targetPos, chain[k], chain[k + 1]);
                if (d < bestD) { bestD = d; splitSub = k; }
            }
            for (int k = 0; k < oldPts.Count; k++)
            {
                if (Vector2.Distance(oldPts[k], targetPos) < 12f) continue;
                if (k < splitSub) leftWps.Add(oldPts[k]);
                else rightWps.Add(oldPts[k]);
            }
        }

        if (autoRouteEnabled)
        {
            if (leftWps.Count == 0 && TerrainField.IsSegmentBlocked(currentRegion, pa, targetPos))
                TerrainField.FindShortestGridWaypoints(currentRegion, pa, targetPos, leftWps);
            if (rightWps.Count == 0 && TerrainField.IsSegmentBlocked(currentRegion, targetPos, pb))
                TerrainField.FindShortestGridWaypoints(currentRegion, targetPos, pb, rightWps);
        }
    }

    void CommitLineInsertion(Line line, int segment, int target)
    {
        if (!CanInsertStation(line, segment, target)) { ClosePanel(true); Toast("航线或星港已变化，请重新操作"); return; }
        int cost = InsertStationCost(line, segment, target);
        if (!IsInEventSandbox() && coins < cost) { Toast("金币不足：还差 ¥" + FormatShortfall(cost - coins)); return; }
        BeginUndo("插入途经站"); ClosePanel(true);
        SyncLineWaypointsCount(line);
        Vector2 pa = Pos(line.st[segment]);
        Vector2 pb = Pos(line.st[segment + 1]);
        Vector2 pt = Pos(target);
        SplitWaypointsForInsertedStation(line, segment, pa, pb, pt, out var leftWps, out var rightWps);
        if (!IsInEventSandbox()) coins -= cost; line.st.Insert(segment + 1, target);
        if (segment >= 0 && segment < line.segWaypoints.Count)
        {
            line.segWaypoints[segment].pts.Clear();
            line.segWaypoints[segment].pts.AddRange(leftWps);
            line.segWaypoints.Insert(segment + 1, new SegmentWaypoints { pts = rightWps });
        }
        if (!IsInEventSandbox()) { BuildStationsOnPath(new List<int> { target }); stations[target].closed = false; }
        CreateLineVisuals(); SyncMainLinesToSandbox(); if (IsInEventSandbox()) SaveSandboxStorage();
        RefreshStationDots(); RecalcIncome(); RefreshHUD(); if (!IsInEventSandbox()) CheckSceneryOnLine(new List<int> { target });
        EndUndo(); Toast("已插入 " + StationDisplayName(target, currentRegion));
    }
    bool TryRemoveWaystation(Line line, int station, out List<int> path, out int previous, out int next, out string reason)
    {
        path = null; previous = next = -1; reason = "航线已发生变化，请重新选择";
        if (line == null || !lines.Contains(line) || station < 0 || station >= stations.Count) return false;
        bool loop = IsLoop(line.st);
        int count = line.st.Count - (loop ? 1 : 0), index = -1, occurrences = 0;
        for (int i = 0; i < count; i++) if (line.st[i] == station) { index = i; occurrences++; }
        if (occurrences != 1) { reason = "该港在航线中出现多次，暂不能直接移除"; return false; }
        if (!loop && (index == 0 || index == count - 1)) { reason = "这是航线端点，请使用拆除航线段"; return false; }
        if (loop && count <= 3) { reason = "环线至少需要保留 3 座星港"; return false; }
        previous = line.st[(index - 1 + count) % count]; next = line.st[(index + 1) % count];
        string region = SegmentRegion(previous, station);
        if (region != currentRegion || SegmentRegion(station, next) != region || SegmentRegion(previous, next) != region) {
            reason = "移除后无法在当前区域连接前后星港"; return false;
        }
        path = line.st.GetRange(0, count); path.RemoveAt(index);
        if (loop) path.Add(path[0]);
        if (!PortalEndsAreValid(path)) { path = null; reason = "移除后会破坏交界港的连接"; return false; }
        return true;
    }
    int WaystationChangeCost(int previous, int station, int next)
    {
        float refund = DemolishRefundRate();
        float f = TrackBuildCostFactor();
        int first = Mathf.RoundToInt(EvaluateStationPairCost(previous, station, f, out _) * refund);
        int second = Mathf.RoundToInt(EvaluateStationPairCost(station, next, f, out _) * refund);
        int replacement = EvaluateStationPairCost(previous, next, f, out _);
        return replacement - first - second;
    }
    void OpenRemoveStationLines(int station, int page)
    {
        var options = new List<Line>();
        foreach (var line in lines) if (line.st.Contains(station) && PathTouchesRegion(line.st, currentRegion)) options.Add(line);
        if (options.Count == 0) { ClosePanel(true); Toast("该港暂无途经航线"); return; }
        const int pageSize = 5;
        int pages = (options.Count + pageSize - 1) / pageSize; page = Mathf.Clamp(page, 0, pages - 1);
        int count = Mathf.Min(pageSize, options.Count - page * pageSize);
        float bottom = pages > 1 ? 104 : 24;
        float h = bottom + count * 82 + 124;
        var p = OpenPopup("选择要调整的航线", h, 680, "remove_station_lines_" + station);
        Txt(p.transform, StationDisplayName(station, currentRegion) + "  仅从所选航线移除", 24, InkSoft, 28, h - 112, 620, 38, TextAnchor.MiddleLeft);
        for (int i = 0; i < count; i++) {
            var selected = options[page * pageSize + i];
            var button = Btn(p.transform, "RemoveFromLine" + selected.id, selected.name, 26, 28, bottom + (count - 1 - i) * 82, 624, 68, Paper2,
                () => PreviewRemoveWaystation(selected, station));
            TInk(button, Ink);
        }
        if (pages > 1) {
            int current = page;
            var before = Btn(p.transform, "PreviousPage", "上一页", 24, 28, 20, 196, 66, Paper2, () => OpenRemoveStationLines(station, current - 1)); TInk(before, Ink);
            Txt(p.transform, (page + 1) + " / " + pages, 24, InkSoft, 240, 20, 200, 66, TextAnchor.MiddleCenter);
            var after = Btn(p.transform, "NextPage", "下一页", 24, 456, 20, 196, 66, Paper2, () => OpenRemoveStationLines(station, current + 1)); TInk(after, Ink);
        }
    }
    void PreviewRemoveWaystation(Line line, int station)
    {
        if (!TryRemoveWaystation(line, station, out var path, out int previous, out int next, out string reason)) { Toast(reason); return; }
        int cost = IsInEventSandbox() ? 0 : WaystationChangeCost(previous, station, next);
        var forecast = CalculateTraffic(line, path);
        float projected = forecast.lineServed.TryGetValue(line, out float served) ? served : 0f;
        string money = cost > 0 ? "需支付 ¥" + cost : cost < 0 ? "退还 ¥" + (-cost) : "无需费用";
        string body = line.name + "：" + StationDisplayName(previous, currentRegion) + " → " + StationDisplayName(station, currentRegion) + " → " + StationDisplayName(next, currentRegion)
            + "\n调整为：" + StationDisplayName(previous, currentRegion) + " → " + StationDisplayName(next, currentRegion)
            + "\n" + money + "（拆除旧航线并连接新航线）"
            + "\n当前时段客流：" + Mathf.RoundToInt(LinePassengerFlow(line)) + " → " + Mathf.RoundToInt(projected) + " 人/日"
            + "\n星港及其他航线保持不变。";
        Confirm("移除途经港", body, () => CommitRemoveWaystation(line, station, previous, next));
    }
    void CommitRemoveWaystation(Line line, int station, int expectedPrevious, int expectedNext)
    {
        if (!TryRemoveWaystation(line, station, out var path, out int previous, out int next, out string reason) || previous != expectedPrevious || next != expectedNext) {
            Toast("航线已发生变化，请重新操作"); return;
        }
        int cost = IsInEventSandbox() ? 0 : WaystationChangeCost(previous, station, next);
        if (!IsInEventSandbox() && cost > coins) { Toast("金币不足：还差 ¥" + FormatShortfall(cost - coins)); return; }
        BeginUndo("移除途经港");
        SyncLineWaypointsCount(line);
        bool wasLoop = IsLoop(line.st);
        int oldCount = line.st.Count - (wasLoop ? 1 : 0);
        int idx = line.st.IndexOf(station);
        if (idx >= 0 && idx < oldCount)
        {
            if (wasLoop && idx == 0)
            {
                if (line.segWaypoints.Count > 0) line.segWaypoints.RemoveAt(0);
                if (line.segWaypoints.Count > 0) line.segWaypoints[line.segWaypoints.Count - 1].pts.Clear();
            }
            else if (idx >= 1 && idx < line.segWaypoints.Count)
            {
                line.segWaypoints[idx - 1].pts.Clear();
                line.segWaypoints.RemoveAt(idx);
            }
        }
        coins -= cost; line.st = path; SyncLineWaypointsCount(line);
        if (autoRouteEnabled && line.segWaypoints.Count > 0)
        {
            int mergedSeg = (wasLoop && idx == 0) ? line.segWaypoints.Count - 1 : idx - 1;
            if (mergedSeg >= 0 && mergedSeg < line.segWaypoints.Count
                && TerrainField.IsSegmentBlocked(currentRegion, Pos(previous), Pos(next)))
            {
                TerrainField.FindShortestGridWaypoints(currentRegion, Pos(previous), Pos(next), line.segWaypoints[mergedSeg].pts);
            }
        }
        CreateLineVisuals(); SyncMainLinesToSandbox(); if (IsInEventSandbox()) SaveSandboxStorage(); RefreshStationDots(); RecalcIncome(); RefreshHUD(); EndUndo();
        Toast("已从 " + line.name + " 移除 " + StationDisplayName(station, currentRegion));
    }
    void RefreshInsertWaypointHandles()
    {
        for (int i = 0; i < insertWpHandles.Count; i++)
            if (insertWpHandles[i]) Destroy(insertWpHandles[i]);
        insertWpHandles.Clear();
        if (!lineInsertActive || previewLayer == null || spriteRing == null) return;

        float size = 26f * overlayZoom;
        Vector2 centeredPivot = new Vector2(0.5f, 0.5f);
        // 当前拖动的拐点：亮金色并融入线色突出；其余：半透明白圆环
        Color activeColor = new Color(1f, 0.83f, 0.24f, 1f);
        if (insertingLine != null) activeColor = Color.Lerp(activeColor, insertingLine.color, 0.30f);
        activeColor.a = 1f;
        Color idleColor = new Color(1f, 1f, 1f, 0.55f);

        for (int i = 0; i < insertTempWaypoints.Count; i++)
        {
            var go = Rect("InsertWpHandle_" + i, previewLayer, insertTempWaypoints[i].x, insertTempWaypoints[i].y, size, size);
            go.GetComponent<RectTransform>().pivot = centeredPivot;
            var img = Img(go, i == insertEditWpIndex ? activeColor : idleColor, spriteRing);
            img.raycastTarget = false;
            insertWpHandles.Add(go);
        }
    }

    void CancelLineInsert()
    {
        lineInsertActive = false; insertingLine = null; insertingSegment = insertTarget = -1;
        insertEditWpIndex = -1; insertTempWaypoints.Clear();
        insertHasGridPoint = false; insertGridBlocked = false;
        SetDotGridHighlight(false);
        // 恢复预览时被淡化的原线透明度（poly 可能已被 CreateLineVisuals 重建，用隐式 bool 规避悬空引用）
        if (insertDimmedPoly)
        {
            insertDimmedPoly.bodyColor.a = 0.94f;
            insertDimmedPoly.SetVerticesDirty();
            insertDimmedPoly = null;
        }
        if (insertPreviewPoly) Destroy(insertPreviewPoly.gameObject);
        if (insertPreviewA) Destroy(insertPreviewA.gameObject); if (insertPreviewB) Destroy(insertPreviewB.gameObject);
        if (insertBadge) Destroy(insertBadge);
        for (int i = 0; i < insertWpHandles.Count; i++)
            if (insertWpHandles[i]) Destroy(insertWpHandles[i]);
        insertWpHandles.Clear();
        insertPreviewPoly = null; insertPreviewA = insertPreviewB = null; insertBadge = null; insertBadgeText = null;
    }
    public void ForwardLinePanStart(PointerEventData e) { if (mapWheelZoom) mapWheelZoom.OnBeginDrag(e); }
    public void ForwardLinePan(PointerEventData e) { if (mapWheelZoom) mapWheelZoom.OnDrag(e); }
    public void ForwardLinePanEnd(PointerEventData e) { if (mapWheelZoom) mapWheelZoom.OnEndDrag(e); }

    void CloseLineBubble()
    {
        if (lineBubbleRoot) Destroy(lineBubbleRoot);
        if (lineBubbleScrim) Destroy(lineBubbleScrim);
        lineBubbleRoot = null; lineBubbleScrim = null;
    }
}

public class LineTapPress : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public DemoBootstrap boot;
    public int lineId, segment;
    bool pressing, inserting, panning, suppressClick;
    float elapsed;
    Vector2 start;
    public void OnPointerDown(PointerEventData e) { pressing = true; inserting = panning = suppressClick = false; elapsed = 0f; start = e.position; }
    void Update()
    {
        if (!pressing || !boot) return;
        if (Input.touchCount >= 2) { pressing = false; return; }
        elapsed += Time.unscaledDeltaTime;
        if (elapsed < 0.22f) return;
        pressing = false; inserting = boot.BeginLineInsert(lineId, segment, start); suppressClick = inserting;
    }
    public void OnBeginDrag(PointerEventData e)
    {
        if (inserting) { boot.UpdateLineInsert(e.position); return; }
        // 14px 内微抖视为手指落点误差，不立即打断长按
        if (pressing && Vector2.Distance(e.position, start) <= 14f) return;
        pressing = false; suppressClick = true; panning = true;
        if (boot) boot.ForwardLinePanStart(e);
    }
    public void OnDrag(PointerEventData e)
    {
        if (!boot) return;
        if (inserting) { boot.UpdateLineInsert(e.position); return; }
        // 微抖被 OnBeginDrag 容忍后，若继续拖出容差则转为平移，避免地图失去响应
        if (pressing)
        {
            if (Vector2.Distance(e.position, start) <= 14f) return;
            pressing = false; suppressClick = true; panning = true;
            boot.ForwardLinePanStart(e);
        }
        if (panning) boot.ForwardLinePan(e);
    }
    public void OnPointerUp(PointerEventData e) { pressing = false; if (inserting && boot) { inserting = false; boot.EndLineInsert(e.position); } }
    public void OnEndDrag(PointerEventData e) { if (panning && boot) boot.ForwardLinePanEnd(e); panning = false; }
    public void OnPointerClick(PointerEventData e) { if (!suppressClick && boot) boot.OnLineTap(e.position); suppressClick = false; }
}
