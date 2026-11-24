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
            for (int i = 0; i < line.st.Count - 1; i++)
            {
                if (SegmentRegion(line.st[i], line.st[i+1]) != currentRegion) continue;
                float d = DistToSeg(point, Pos(line.st[i]), Pos(line.st[i+1]));
                if (d > tolerance) continue;
                LineHit h;
                if (!best.TryGetValue(line.id, out h) || d < h.dist)
                    best[line.id] = new LineHit { line = line, seg = i, dist = d };
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
            RecalcIncome(); EndUndo(); RefreshHUD();
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
                RecalcIncome(); EndUndo(); RefreshHUD();
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
            bool showBoost = load >= 0.72f;
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
            bool showBoost = load >= 0.72f;
            float bh = heights[i];
            float y = cursorTop - bh;
            cursorTop = y - gap;

            var card = Rect("LBubble" + line.id, lineBubbleRoot.transform, x, y, bw, bh);
            Rounded(card, new Color(1f, 0.985f, 0.95f, 0.985f)); SoftShadow(card);
            Img(Rect("Accent", card.transform, 0, 12, 6, bh - 24), line.color, spriteRound).raycastTarget = false;
            Txt(card.transform, line.name, 28, Ink, 20, bh - 54, 170, 38, TextAnchor.MiddleLeft);
            Txt(card.transform, StationDisplayName(a, currentRegion) + " ⇄ " + StationDisplayName(b, currentRegion), 23, InkSoft, 194, bh - 52, 342, 36, TextAnchor.MiddleLeft);

            if (showBoost)
            {
                int pct = Mathf.RoundToInt(load * 100f);
                Color loadCol = load >= 0.95f ? Vermilion : load >= 0.72f ? new Color(0.84f, 0.56f, 0.12f) : Matcha;
                string statusText = load >= 0.95f ? "负荷 " + pct + "% · 爆满拥挤" : load >= 0.72f ? "负荷 " + pct + "% · 运力吃紧" : "负荷 " + pct + "% · 运行通畅";
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
        float d = Vector2.Distance(Pos(a), Pos(b));
        int refund = Mathf.RoundToInt((30f + d * 0.75f) * TrackBuildCostFactor() * DemolishRefundRate());
        string tail = line.st.Count <= 2 ? "\n该段拆除后，这条航线会消失。" : "\n拆除后退还 ¥" + refund + "。";
        return StationDisplayName(a, currentRegion) + " ⇄ " + StationDisplayName(b, currentRegion) + tail;
    }
    string LineConfirmText(Line line)
    {
        if (line == null) return "";
        int refund = Mathf.RoundToInt(120f * (UniqueCount(line.st) - 1) * TrackBuildCostFactor() * DemolishRefundRate());
        return UniqueCount(line.st) + " 港 · " + line.trains + " 星舰\n拆除后退还 ¥" + refund + "。";
    }

    void DoDemolishSegment(Line line, int seg)
    {
        if (line == null || !lines.Contains(line) || seg < 0 || seg >= line.st.Count - 1) return;
        int n = line.st.Count;
        if (!IsLoop(line.st) && n > 3 && seg > 0 && seg < n - 2) {
            var head = line.st.GetRange(0, seg + 1); var tailPath = line.st.GetRange(seg + 1, n - seg - 1);
            foreach (string region in new[] { "月之亮面", "月背", "火星" }) {
                int after = RegionLineCount(region) - (PathTouchesRegion(line.st, region) ? 1 : 0)
                    + (PathTouchesRegion(head, region) ? 1 : 0) + (PathTouchesRegion(tailPath, region) ? 1 : 0);
                if (after > RegionLineLimit(region)) { Toast(region + "航线已达上限，无法拆分该段"); return; }
            }
        }
        int a = line.st[seg], b = line.st[seg + 1];
        float d = Vector2.Distance(Pos(a), Pos(b));
        int refund = Mathf.RoundToInt((30f + d * 0.75f) * TrackBuildCostFactor() * DemolishRefundRate());
        BeginUndo("拆除航线段");
        string msg;

        if (IsLoop(line.st))
        {
            var opened = new List<int>();
            for (int i = 0; i < n - 1; i++) opened.Add(line.st[(seg + 1 + i) % (n - 1)]);
            line.st = opened;
            coins += refund;
            msg = "已拆除一段，环线变为普通航线";
        }
        else if (n <= 2)
        {
            SettleRemovedLineTax(line); ReleaseLineFleet(line); lines.Remove(line);
            coins += refund;
            msg = "已拆除 " + line.name;
        }
        else if (seg == 0)
        {
            line.st.RemoveAt(0);
            coins += refund;
            msg = "已拆除一段：" + StationDisplayName(a, currentRegion) + " ⇄ " + StationDisplayName(b, currentRegion);
        }
        else if (seg == n - 2)
        {
            line.st.RemoveAt(n - 1);
            coins += refund;
            msg = "已拆除一段：" + StationDisplayName(a, currentRegion) + " ⇄ " + StationDisplayName(b, currentRegion);
        }
        else
        {
            var tail = line.st.GetRange(seg + 1, n - seg - 1);
            line.st.RemoveRange(seg + 1, n - seg - 1);
            coins += refund;
            var nl = new Line {
                id = nextLineId,
                name = nextLineId + " 号线",
                color = LineColors[(nextLineId - 1) % LineColors.Length]
            };
            nl.st = tail;
            lines.Add(nl);
            nextLineId++;
            msg = "航线拆分为 " + line.name + " / " + nl.name;
        }

        CreateLineVisuals(); RefreshStationDots(); RecalcIncome(); RefreshHUD();
        EndUndo(); Toast(msg);
    }

    void DoDemolishLine(Line line)
    {
        if (line == null || !lines.Contains(line)) return;
        BeginUndo("拆除整条航线");
        float refund = 120f * (UniqueCount(line.st) - 1) * TrackBuildCostFactor() * DemolishRefundRate();
        coins += refund; SettleRemovedLineTax(line); ReleaseLineFleet(line); lines.Remove(line);
        CreateLineVisuals(); RefreshStationDots(); RecalcIncome(); RefreshHUD();
        EndUndo(); Toast("已拆除 " + line.name);
    }

    void OpenLineInfo(Line line, int seg)
    {
        if (line == null || !lines.Contains(line)) return;
        CloseLineBubble();
        string pathText = "沿线 " + UniquePathText(line.st);
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
        StatBox(p.transform, "单向区段运力", Mathf.RoundToInt(LineCapacity(line)) + " 人次/日", 496, h - 470, 430, 150, Sakura);

        Txt(p.transform, "近 7 日净利润", 24, Ink, 36, 675 + extra, 420, 34, TextAnchor.MiddleLeft);
        if (line.recent.Count == 0)
        {
            for (int i = 0; i < 7; i++)
            {
                var ghost = Rect("GhostBar", p.transform, 60 + i * 120, 505 + extra, 64, 135);
                Img(ghost, new Color(0.90f, 0.87f, 0.81f, 0.42f), spriteRound).raycastTarget = false;
            }
            Img(Rect("ChartBase", p.transform, 36, 500 + extra, 888, 2), new Color(0.55f,0.50f,0.42f,0.28f)).raycastTarget = false;
            Txt(p.transform, "首日运营中 · 今日收入 ¥" + line.todayRevenue + "\n日结后生成近 7 日利润曲线", 23, InkSoft, 36, 540 + extra, 860, 74, TextAnchor.UpperLeft);
        }
        else
        {
            Txt(p.transform, "已结算 " + line.recent.Count + " 日 · 日均 ¥" + (recentSum / line.recent.Count), 22, InkSoft, 450, 678 + extra, 460, 34, TextAnchor.MiddleRight);
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
        Txt(p.transform, "星港 " + unique + " · 星舰 " + line.trains + "/8 · 航线长度 " + FormatLineKm(line.st), 23, Ink, 36, 420 + extra, 860, 36, TextAnchor.MiddleLeft);
        Txt(p.transform, "最高区段需求 " + Mathf.RoundToInt(LinePeakSectionDemand(line)) + " 人次/日 · 平均速度 " + Mathf.RoundToInt(FleetTrait(line, 2)*100) + "%", 23, InkSoft, 36, 382 + extra, 860, 34, TextAnchor.MiddleLeft);
        if (seg >= 0 && seg < line.st.Count - 1)
            Txt(p.transform, "选中段：" + stations[line.st[seg]].name + " ⇄ " + stations[line.st[seg + 1]].name, 22, line.color, 36, 344 + extra, 860, 34, TextAnchor.MiddleLeft);
        ScrollableText(p.transform, pathText, 22, InkSoft, 36, 296, 860, pathHeight);

        Txt(p.transform, "预计成本：星舰 ¥" + Mathf.RoundToInt(forecast.vehicle) + " · 航线 ¥" + Mathf.RoundToInt(forecast.track) + " · 税 ¥" + Mathf.RoundToInt(forecast.tax), 23, InkSoft, 36, 244, 860, 38, TextAnchor.MiddleLeft);
        var planning = Btn(p.transform, "Operations", "票价与调度", 25, 34, 132, 892, 70, Indigo, () => { ClosePanel(true); OpenLineOperations(line); }); TInk(planning, Color.white);
        Btn(p.transform, "LineDetail", "航线详情 / 星舰", 26, 34, 34, 430, 82, Indigo, () => {
            ClosePanel(); OpenLineScreen(line.id, () => OpenLineInfo(line, seg));
        });
        var close = Btn(p.transform, "InfoClose", "关 闭", 26, 496, 34, 430, 82, new Color(0.92f,0.89f,0.83f), ClosePanel);
        TInk(close, Ink);
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
            if (line.name != value) { BeginUndo("修改航线名称"); line.name = value; EndUndo(); }
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
    Image insertPreviewA, insertPreviewB;
    GameObject insertBadge;
    Text insertBadgeText;

    public bool BeginLineInsert(int lineId, int segment, Vector2 screen)
    {
        if (dragging || lineInsertActive || pageRoot || activePanel || sceneryViewer) return false;
        var line = lines.Find(item => item.id == lineId);
        if (line == null || segment < 0 || segment >= line.st.Count - 1 ||
            SegmentRegion(line.st[segment], line.st[segment + 1]) != currentRegion) return false;
        CloseLineBubble();
        insertingLine = line; insertingSegment = segment; insertTarget = -1; lineInsertActive = true;
        insertPreviewA = Img(Rect("InsertPreviewA", previewLayer, 0, 0, 1, 10), line.color, spriteRound);
        insertPreviewB = Img(Rect("InsertPreviewB", previewLayer, 0, 0, 1, 10), line.color, spriteRound);
        foreach (var image in new[] { insertPreviewA, insertPreviewB }) {
            image.raycastTarget = false; image.rectTransform.pivot = new Vector2(0, 0.5f);
        }
        insertBadge = Rect("InsertCostBadge", root, 0, 0, 330, 62);
        Rounded(insertBadge, new Color(1f, 0.98f, 0.92f, 0.96f));
        insertBadge.GetComponent<Image>().raycastTarget = false;
        insertBadgeText = Txt(insertBadge.transform, "拖至新星港", 23, Ink, 12, 5, 306, 52, TextAnchor.MiddleCenter);
        Haptic.Vibrate(22); PlayTone(false); UpdateLineInsert(screen);
        return true;
    }
    bool CanInsertStation(Line line, int segment, int target)
    {
        if (line == null || !lines.Contains(line) || segment < 0 || segment >= line.st.Count - 1 ||
            target < 0 || target >= stations.Count || line.st.Contains(target)) return false;
        int a = line.st[segment], b = line.st[segment + 1];
        if (stations[target].portal && (stations[a].portal || stations[b].portal)) return false;
        return StationInRegion(target, currentRegion) && SegmentRegion(a, b) == currentRegion &&
            SegmentRegion(a, target) == currentRegion && SegmentRegion(target, b) == currentRegion;
    }
    int InsertStationCost(Line line, int segment, int target)
    {
        int a = line.st[segment], b = line.st[segment + 1];
        float f = TrackBuildCostFactor();
        int before = Mathf.RoundToInt((30f + Vector2.Distance(Pos(a), Pos(b)) * 0.75f) * f);
        int after = Mathf.RoundToInt((30f + Vector2.Distance(Pos(a), Pos(target)) * 0.75f) * f)
            + Mathf.RoundToInt((30f + Vector2.Distance(Pos(target), Pos(b)) * 0.75f) * f);
        return Mathf.Max(0, after - before) + StationOpeningCost(target);
    }
    void SetInsertPreview(Image image, Vector2 a, Vector2 b)
    {
        if (!image) return;
        Vector2 delta = b - a; var rt = image.rectTransform;
        rt.anchoredPosition = a; rt.sizeDelta = new Vector2(Mathf.Max(1f, delta.magnitude), 10f * overlayZoom);
        rt.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
    }
    public void UpdateLineInsert(Vector2 screen)
    {
        if (!lineInsertActive || insertingLine == null || !lines.Contains(insertingLine) ||
            insertingSegment < 0 || insertingSegment >= insertingLine.st.Count - 1) return;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(mapRect, screen, null, out var local)) return;
        float snap = 44f / Mathf.Max(1f, mapRect.localScale.x);
        int near = NearestStationAny(local, snap);
        insertTarget = CanInsertStation(insertingLine, insertingSegment, near) ? near : -1;
        Vector2 point = insertTarget >= 0 ? Pos(insertTarget) : local;
        SetInsertPreview(insertPreviewA, Pos(insertingLine.st[insertingSegment]), point);
        SetInsertPreview(insertPreviewB, point, Pos(insertingLine.st[insertingSegment + 1]));
        if (insertBadgeText) insertBadgeText.text = insertTarget >= 0
            ? "插入造价 ¥" + InsertStationCost(insertingLine, insertingSegment, insertTarget) : "拖至可插入的新星港";
        if (insertBadge && RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screen, null, out var badgePoint)) {
            var rect = insertBadge.GetComponent<RectTransform>();
            badgePoint -= root.rect.min;
            rect.anchoredPosition = new Vector2(Mathf.Clamp(badgePoint.x - 165f, 12f, W - 342f), Mathf.Clamp(badgePoint.y + 55f, 190f, H - 290f));
        }
    }
    public void EndLineInsert(Vector2 screen)
    {
        if (!lineInsertActive) return;
        UpdateLineInsert(screen);
        var line = insertingLine; int segment = insertingSegment, target = insertTarget;
        CancelLineInsert();
        if (!CanInsertStation(line, segment, target)) { Toast("请拖到本区域内未经过的普通星港"); return; }
        int cost = InsertStationCost(line, segment, target);
        string details = line.name + " · " + StationDisplayName(target, currentRegion) + "\n造价 ¥" + cost + "（含必要开站费用）";
        float detailsHeight = Mathf.Clamp(MeasureTextHeight(details, 25, 620) + 8, 54, H - 410);
        var p = OpenPopup("插入途经港", detailsHeight + 200, 680);
        ScrollableText(p.transform, details, 25, InkSoft, 28, 102, 620, detailsHeight);
        var confirm = Btn(p.transform, "ConfirmInsert", "确认 ¥" + cost, 25, 28, 20, 300, 66, Matcha, () => CommitLineInsertion(line, segment, target)); TInk(confirm, Color.white);
        var cancel = Btn(p.transform, "CancelInsert", "取消", 25, 350, 20, 300, 66, Paper2, ClosePanel); TInk(cancel, Ink);
    }
    void CommitLineInsertion(Line line, int segment, int target)
    {
        if (!CanInsertStation(line, segment, target)) { ClosePanel(true); Toast("航线或星港已变化，请重新操作"); return; }
        int cost = InsertStationCost(line, segment, target);
        if (coins < cost) { Toast("金币不足：还差 ¥" + FormatShortfall(cost - coins)); return; }
        BeginUndo("插入途经站"); ClosePanel(true);
        coins -= cost; line.st.Insert(segment + 1, target); BuildStationsOnPath(new List<int> { target }); stations[target].closed = false;
        CreateLineVisuals(); RefreshStationDots(); RecalcIncome(); RefreshHUD(); CheckSceneryOnLine(new List<int> { target });
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
        int first = Mathf.RoundToInt((30f + Vector2.Distance(Pos(previous), Pos(station)) * 0.75f) * f * refund);
        int second = Mathf.RoundToInt((30f + Vector2.Distance(Pos(station), Pos(next)) * 0.75f) * f * refund);
        int replacement = Mathf.RoundToInt((30f + Vector2.Distance(Pos(previous), Pos(next)) * 0.75f) * f);
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
        Txt(p.transform, StationDisplayName(station, currentRegion) + " · 仅从所选航线移除", 24, InkSoft, 28, h - 112, 620, 38, TextAnchor.MiddleLeft);
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
        int cost = WaystationChangeCost(previous, station, next);
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
        int cost = WaystationChangeCost(previous, station, next);
        if (cost > coins) { Toast("金币不足：还差 ¥" + FormatShortfall(cost - coins)); return; }
        BeginUndo("移除途经港"); coins -= cost; line.st = path;
        CreateLineVisuals(); RefreshStationDots(); RecalcIncome(); RefreshHUD(); EndUndo();
        Toast("已从 " + line.name + " 移除 " + StationDisplayName(station, currentRegion));
    }
    void CancelLineInsert()
    {
        lineInsertActive = false; insertingLine = null; insertingSegment = insertTarget = -1;
        if (insertPreviewA) Destroy(insertPreviewA.gameObject); if (insertPreviewB) Destroy(insertPreviewB.gameObject);
        if (insertBadge) Destroy(insertBadge);
        insertPreviewA = insertPreviewB = null; insertBadge = null; insertBadgeText = null;
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
        if (elapsed < 0.42f) return;
        pressing = false; inserting = boot.BeginLineInsert(lineId, segment, start); suppressClick = inserting;
    }
    public void OnBeginDrag(PointerEventData e)
    {
        if (inserting) { boot.UpdateLineInsert(e.position); return; }
        pressing = false; suppressClick = true; panning = true;
        if (boot) boot.ForwardLinePanStart(e);
    }
    public void OnDrag(PointerEventData e) { if (!boot) return; if (inserting) boot.UpdateLineInsert(e.position); else if (panning) boot.ForwardLinePan(e); }
    public void OnPointerUp(PointerEventData e) { pressing = false; if (inserting && boot) { inserting = false; boot.EndLineInsert(e.position); } }
    public void OnEndDrag(PointerEventData e) { if (panning && boot) boot.ForwardLinePanEnd(e); panning = false; }
    public void OnPointerClick(PointerEventData e) { if (!suppressClick && boot) boot.OnLineTap(e.position); suppressClick = false; }
}
