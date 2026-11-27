using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class DemoBootstrap
{
    GameObject pageRoot;
    RectTransform pageContentRect;
    Action pageBack;
    Action pageForecastRefresh;
    Action panelForecastRefresh;
    string activePageKey = "";
    float PgH => pageContentRect ? Mathf.Max(1400f, pageContentRect.rect.height) : PageSafeHeight;

    GameObject OpenPage(string title, string help, string backgroundPath = null, string pageKey = null)
    {
        string effectiveKey = !string.IsNullOrEmpty(pageKey) ? pageKey : title;
        bool isRefresh = pageRoot != null && !string.IsNullOrEmpty(effectiveKey) && activePageKey == effectiveKey;

        ClosePage(false);
        pageBack = null;
        CloseLineBubble();
        ClearStationOdArcs();

        pageRoot = Rect("Page", root, 0, 0, RootW, RootH);
        activePageKey = effectiveKey ?? "";
        var pageRootRt = pageRoot.GetComponent<RectTransform>();
        pageRootRt.anchorMin = Vector2.zero; pageRootRt.anchorMax = Vector2.one;
        pageRootRt.pivot = new Vector2(0.5f, 0.5f);
        pageRootRt.offsetMin = Vector2.zero; pageRootRt.offsetMax = Vector2.zero;
        Img(pageRoot, Paper);

        GetSafeMarginsInRoot(out float safeL, out float safeR, out float safeT, out float safeB);
        float safeW = Mathf.Max(320f, RootW - safeL - safeR);
        float safeH = Mathf.Max(1400f, RootH - safeT - safeB);
        float contentX = safeL + (safeW - W) * 0.5f;
        var pageContent = Rect("PageContent", pageRoot.transform, contentX, safeB, W, safeH);
        pageContentRect = pageContent.GetComponent<RectTransform>();

        var pTop = Rect("PTop", pageRoot.transform, 0, RootH - safeT - 160f, RootW, safeT + 160f);
        Img(pTop, HeaderBg);
        var pDiv = Rect("PDiv", pageRoot.transform, 0, RootH - safeT - 160f, RootW, 3);
        Img(pDiv, new Color(InkSoft.r, InkSoft.g, InkSoft.b, 0.16f)).raycastTarget = false;

        var bb = Btn(pageRoot.transform, "PBack", "←", 40, 24, 0, 94, 94, CardBg, GoBackPage);
        TInk(bb, Ink); SoftShadow(bb.gameObject);
        var home = Btn(pageRoot.transform, "PHome", "主页", 24, 134, 0, 104, 94, CardBg, GoHome);
        TInk(home, Ink); SoftShadow(home.gameObject);
        var heading = Txt(pageRoot.transform, title, 44, Ink, 0, 0, 600, 60, TextAnchor.MiddleLeft);
        heading.resizeTextForBestFit = true; heading.resizeTextMinSize = 24; heading.resizeTextMaxSize = ScaledFontSize(44); heading.horizontalOverflow = HorizontalWrapMode.Overflow; heading.verticalOverflow = VerticalWrapMode.Truncate;
        RectTransform helpRect = null;
        if (!string.IsNullOrEmpty(help))
        {
            var hb = Btn(pageRoot.transform, "PHelp", "?", 32, 0, 0, 64, 64, CardBg, () => HelpPopup(help));
            TInk(hb, InkSoft); SoftShadow(hb.gameObject); helpRect = hb.GetComponent<RectTransform>();
        }
        if (!string.IsNullOrEmpty(backgroundPath)) AddPageBackground(pageRoot, backgroundPath);
        var layout = pageRoot.AddComponent<PageHeaderLayout>();
        layout.page = pageRootRt;
        layout.topBarBg = pTop.GetComponent<RectTransform>();
        layout.topDivider = pDiv.GetComponent<RectTransform>();
        layout.back = bb.GetComponent<RectTransform>();
        layout.home = home.GetComponent<RectTransform>();
        layout.title = heading.rectTransform;
        layout.help = helpRect;
        layout.Refresh();
        EnsurePreviewBannerOnTop();
        if (!isRefresh)
        {
            PlayPageOpenSfx();
            AnimatePageEnter(pageRoot);
        }
        return pageContent;
    }

    void AttachPageCurrencyBadge(bool showRp)
    {
        if (!pageRoot) return;
        var layout = pageRoot.GetComponent<PageHeaderLayout>();
        float bw = showRp ? 276f : 234f;
        var badge = Rect("PageCurrencyBadge", pageRoot.transform, 0f, 0f, bw, 94f);
        var rt = badge.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = Vector2.zero;
        Rounded(badge, new Color(CardBg.r, CardBg.g, CardBg.b, 0.96f));
        SoftShadow(badge);
        badge.GetComponent<Image>().raycastTarget = false;

        if (showRp)
        {
            var coinDot = Rect("CoinDot", badge.transform, 18f, 61f, 18f, 18f);
            Img(coinDot, Gold, spriteCircle).raycastTarget = false;
            var coinLbl = Txt(badge.transform, "资金 ¥" + FormatCoins(coins), 24, Ink, 44f, 50f, bw - 58f, 36f, TextAnchor.MiddleLeft);
            FitSingleLine(coinLbl, 24);

            var rpDot = Rect("RpDot", badge.transform, 18f, 19f, 18f, 18f);
            Img(rpDot, Indigo, spriteCircle).raycastTarget = false;
            var rpLbl = Txt(badge.transform, "科研 " + Mathf.FloorToInt(rp) + " RP", 23, Indigo, 44f, 10f, bw - 58f, 34f, TextAnchor.MiddleLeft);
            FitSingleLine(rpLbl, 23);
        }
        else
        {
            var ring = Rect("CoinRing", badge.transform, 16f, 24f, 46f, 46f);
            Img(ring, new Color(0.25f, 0.21f, 0.17f, 0.85f), spriteStationRing).raycastTarget = false;
            Img(Rect("Icon", ring.transform, 4f, 4f, 38f, 38f), Gold, spriteCircle).raycastTarget = false;
            var coinLbl = Txt(badge.transform, "¥" + FormatCoins(coins), 29, Ink, 72f, 18f, bw - 88f, 58f, TextAnchor.MiddleLeft);
            FitSingleLine(coinLbl, 29);
        }

        if (layout) { layout.coinBadge = rt; layout.Refresh(); }
    }

    void AttachPopupCurrencyChip(GameObject popup, float w, float h, bool showRp = false)
    {
        if (!popup) return;
        float cw = showRp ? 304f : 210f;
        var chip = Rect("PopupBalanceChip", popup.transform, w - cw - 84f, h - 66f, cw, 48f);
        Rounded(chip, Color.Lerp(CardBg, Gold, 0.14f));
        chip.GetComponent<Image>().raycastTarget = false;
        string txt = showRp
            ? $"¥{FormatCoins(coins)} · {Mathf.FloorToInt(rp)}RP"
            : $"余额 ¥{FormatCoins(coins)}";
        var lbl = Txt(chip.transform, txt, 21, Ink, 12f, 0f, cw - 24f, 48f, TextAnchor.MiddleCenter);
        FitSingleLine(lbl, 21);
    }
    void AddPageBackground(GameObject page, string path)
    {
        var texture = Resources.Load<Texture2D>(path);
        bool fallback = !texture;
        if (fallback) texture = Resources.Load<Texture2D>("MapArt/moon_nearside");
        if (!texture) return;
        var background = Rect("PageBackground", page.transform, 0, 0, RootW, RootH);
        var art = background.AddComponent<RawImage>(); art.texture = texture;
        art.color = fallback
            ? new Color(1f, 1f, 1f, 0.14f)
            : (IsNeonThemeActive() ? new Color(0.28f, 0.38f, 0.86f, 0.16f) : new Color(1, 1, 1, 0.26f));
        art.raycastTarget = false;
        background.transform.SetAsFirstSibling();
        var fit = background.AddComponent<FleetBackgroundFit>(); fit.page = page.GetComponent<RectTransform>(); fit.image = art;
        var header = page.transform.Find("PTop").GetComponent<Image>();
        var headerColor = header.color; headerColor.a = 0.35f; header.color = headerColor;
    }
    void GoBackPage()
    {
        PlayCloseSfx();
        var b = pageBack; pageBack = null;
        ClosePage(false);
        if (b != null) b();
    }
    void ClosePage() => ClosePage(true);
    void ClosePage(bool trimResources)
    {
        refreshHistoryTree = null; incrementalHistoryPage = null;
        pageForecastRefresh = null;
        CaptureScrollPositions();
        StopPageAnim();
        bool hadHeavyPage = pageRoot != null && (activePageKey == "fleet_shop" || activePageKey == "company" || activePageKey == "scenery" || activePageKey == "line_info" || activePageKey == "tech");
        if (pageRoot) Destroy(pageRoot);
        pageRoot = null; pageContentRect = null; activePageKey = "";
        if (hadHeavyPage && trimResources) RequestResourceTrim(false);
        if (pageRoot == null && sceneryViewer == null)
        {
            MarkRingsDirty();
            trainsDirty = true;
        }
    }
    void GoHome()
    {
        PlayCloseSfx();
        pageBack = null; fleetBack = null; lineFleetBackAction = null; lineFleetOriginFleetBack = null; lineFleetLineId = 0; lineFleetSlot = -1; lineFleetAmount = 0; lineFleetShopReturn = null;
        if (sceneryViewer) Destroy(sceneryViewer);
        sceneryViewer = null;
        panelClosed = null;
        ClosePanel(true); CloseLineBubble(); ClosePage();
        RequestResourceTrim(false);
    }

    void HelpPopup(string text)
    {
        float bodyHeight = Mathf.Clamp(MeasureTextHeight(text, 27, 652) + 10, 60, Mathf.Max(320f, PgH - 420f));
        var p = OpenPopup("说明", bodyHeight + 220, 720);
        ScrollableText(p.transform, text, 27, InkSoft, 34, 128, 652, bodyHeight);
        var ok = Btn(p.transform, "Ok", "知道了", 30, 34, 28, 652, 80, new Color(0.92f,0.89f,0.83f), ClosePanel);
        TInk(ok, Ink);
    }

    void StatBox(Transform p, string label, string value, float x, float y, float w, float h, Color c)
    {
        var g = Rect("Stat", p, x, y, w, h); Rounded(g, CardBg); SoftShadow(g); g.GetComponent<Image>().raycastTarget = false;
        Img(Rect("Bar", g.transform, 0, 24, 7, h-48), c, spriteRound).raycastTarget = false;
        Txt(g.transform, label, 26, InkSoft, 30, h-66, w-56, 34, TextAnchor.MiddleLeft);
        Txt(g.transform, value, 46, Ink, 30, 26, w-56, 54, TextAnchor.MiddleLeft);
    }

    // ---------- 区域选择 ----------
    void OpenRegionScreen()
    {
        var pg = OpenPage("切换区域", "各区依次解锁；四大火星星区可在「线路图 → 火星·全境」拼合呈现。", "PageArt/line_watercolor", "regions");
        const float cardH = 440f, cardGap = 30f, topPad = 24f, botPad = 34f;
        float totalH = topPad + RegionOrder.Length * cardH + (RegionOrder.Length - 1) * cardGap + botPad;
        var content = FleetScroll(pg.transform, 32f, PgH - 188f, totalH, scrollKey: "regions");
        float topY = content.sizeDelta.y, cardX = 4f, innerW = content.sizeDelta.x - 8f;
        for (int i = 0; i < RegionOrder.Length; i++)
        {
            var spec = GetRegionSpec(RegionOrder[i]);
            if (spec == null) continue;
            float cy = topY - topPad - (i + 1) * cardH - i * cardGap;
            BuildRegionShowcaseCard(content, spec, i, cardX, cy, innerW, cardH);
        }
    }

    void BuildRegionShowcaseCard(Transform parent, RegionSpec spec, int index, float x, float y, float w, float h)
    {
        string region = spec.id;
        bool available = RegionUnlocked(region), active = available && region == currentRegion, neon = IsNeonThemeActive();
        var card = Rect("Region_" + region, parent, x, y, w, h);
        Color frameColor = active ? Gold : available
            ? (neon ? new Color(0.20f, 0.26f, 0.44f, 0.96f) : new Color(0.985f, 0.972f, 0.942f, 0.98f))
            : (neon ? new Color(0.15f, 0.17f, 0.26f, 0.92f) : new Color(0.90f, 0.88f, 0.83f, 0.95f));
        Rounded(card, frameColor); SoftShadow(card);

        float pad = active ? 9f : 7f, artW = w - pad * 2f, artH = h - pad * 2f;
        var artClip = Rect("ArtClip", card.transform, pad, pad, artW, artH);
        var clipImg = Img(artClip, neon ? new Color(0.08f, 0.10f, 0.20f, 1f) : new Color(0.96f, 0.94f, 0.89f, 1f), spriteRound);
        clipImg.type = Image.Type.Sliced; clipImg.raycastTarget = false;
        artClip.AddComponent<Mask>().showMaskGraphic = true;

        var bgTex = Resources.Load<Texture2D>(spec.cardBgResource);
        if (bgTex)
        {
            var bgRaw = Rect("Backdrop", artClip.transform, 0, 0, artW, artH).AddComponent<RawImage>();
            bgRaw.texture = bgTex; bgRaw.raycastTarget = false;
            float imgAspect = (float)bgTex.width / Mathf.Max(1, bgTex.height), boxAspect = artW / Mathf.Max(1f, artH);
            if (imgAspect < boxAspect)
            {
                float visH = imgAspect / boxAspect;
                bgRaw.uvRect = new Rect(0f, (1f - visH) * 0.36f, 1f, visH);
            }
            bgRaw.color = neon
                ? (available ? new Color(0.36f, 0.44f, 0.84f, 0.88f) : new Color(0.26f, 0.30f, 0.48f, 0.52f))
                : (available ? Color.white : new Color(0.76f, 0.74f, 0.70f, 0.56f));
        }

        // 居中星际铭牌大标题（优先全彩透明艺术铭牌，缺省时用高对比大标题）
        var titleTex = !string.IsNullOrEmpty(spec.titleMaskResource) ? Resources.Load<Texture2D>(spec.titleMaskResource) : null;
        float titleW = 940f, titleH = 282f, titleX = (artW - titleW) * 0.5f, titleY = (artH - titleH) * 0.5f;
        if (titleTex)
        {
            var glyph = Rect("TitleCalligraphy", artClip.transform, titleX, titleY, titleW, titleH).AddComponent<RawImage>();
            glyph.texture = titleTex; glyph.raycastTarget = false;
            glyph.color = available
                ? (neon ? new Color(0.90f, 0.96f, 1f, 0.96f) : Color.white)
                : new Color(1f, 1f, 1f, 0.68f);
        }
        else
        {
            string spaced = spec.displayTitle;
            Color haloCol = new Color(0.06f, 0.04f, 0.03f, 0.72f);
            Color inkCol = neon ? spec.neonInk : (available ? spec.dayInk : Color.Lerp(spec.dayInk, InkSoft, 0.35f));
            Txt(artClip.transform, spaced, 88, haloCol, 42f, titleY - 3f, artW - 84f, titleH, TextAnchor.MiddleCenter).raycastTarget = false;
            Txt(artClip.transform, spaced, 88, inkCol, 40f, titleY, artW - 84f, titleH, TextAnchor.MiddleCenter).raycastTarget = false;
        }

        // 右上角精巧状态邮戳标签
        string badgeText = active ? "当前驻扎" : available ? "已开放" : "待解锁";
        Color badgeBg = active ? Gold : available ? spec.accentColor : new Color(0.45f, 0.43f, 0.39f, 0.88f);
        var badge = Rect("Badge", artClip.transform, artW - 186f, artH - 68f, 162f, 46f);
        Rounded(badge, badgeBg); badge.GetComponent<Image>().raycastTarget = false;
        Txt(badge.transform, badgeText, 22, Color.white, 0, 0, 162f, 46f, TextAnchor.MiddleCenter);

        // 底部居中半透磨砂信息条：区域航线 */* 营运星港 */*
        int totalOrdinary = 0;
        for (int sIdx = 0; sIdx < stations.Count; sIdx++)
        {
            var st = stations[sIdx];
            if (st.region == region && !st.portal && !st.closed) totalOrdinary++;
        }
        string prevName = string.IsNullOrEmpty(spec.prevRegion) ? "月之亮面" : spec.prevRegion;
        string infoText = available
            ? "区域航线 " + RegionLineCount(region) + "/" + RegionLineLimit(region) + " 营运星港 " + ConnectedOrdinaryStations(region) + "/" + totalOrdinary
            : "解锁条件：先通 " + prevName + "（普通港 " + Mathf.Min(spec.unlockStations, ConnectedOrdinaryStations(prevName)) + "/" + spec.unlockStations + "） · 日利润 ¥" + FormatCoins(income) + "/" + Mathf.RoundToInt(spec.unlockIncome).ToString("N0");
        float pillW = available ? 540f : 890f;
        var pill = Rect("StatusPill", artClip.transform, (artW - pillW) * 0.5f, 20f, pillW, 52f);
        Rounded(pill, neon ? new Color(0.08f, 0.11f, 0.24f, 0.88f) : new Color(0.99f, 0.975f, 0.945f, 0.92f));
        pill.GetComponent<Image>().raycastTarget = false;
        Color infoCol = active ? (neon ? Gold : new Color(0.56f, 0.40f, 0.08f, 1f)) : available ? (neon ? Color.white : Ink) : (neon ? new Color(0.76f, 0.80f, 0.88f) : InkSoft);
        var infoTxt = Txt(pill.transform, infoText, 23, infoCol, 20f, 0f, pillW - 40f, 52f, TextAnchor.MiddleCenter);
        FitSingleLine(infoTxt, 23);

        var btn = card.AddComponent<Button>(); btn.targetGraphic = card.GetComponent<Image>();
        btn.onClick.AddListener(() => {
            if (RegionUnlocked(region)) SwitchRegion(region);
            else Toast("尚未达到" + region + "的解锁条件");
        });
        if (!card.GetComponent<FleetPressFeedback>()) card.AddComponent<FleetPressFeedback>();
        var entry = card.AddComponent<FleetCardEntrance>(); entry.delay = index * 0.035f;
    }

    // ---------- 航线 ----------
    int linesRegionFilter = -1;
    int linesSortMode;
    string linesFilterAutoRegion;
    void OpenLinesScreen()
    {
        if (linesRegionFilter < 0 || linesFilterAutoRegion != currentRegion)
        {
            linesFilterAutoRegion = currentRegion;
            int idx = Array.IndexOf(DispatchRegionFilters, currentRegion);
            linesRegionFilter = idx >= 0 ? idx : 0;
        }

        var pg = OpenPage("航线", "在地图按住星港拖向下一港即可开线；自端点拖出为延线，自中间港拖出则开新线。", "PageArt/line_watercolor", "lines_overview");
        pageForecastRefresh = () => { if (pageRoot != null && activePageKey == "lines_overview") OpenLinesScreen(); };
        AttachPageCurrencyBadge(false);
        StatBox(pg.transform, "预计日利润", FormatSignedNumber(income), 28, PgH-400, 320, 210, Gold);
        StatBox(pg.transform, "已连通", ConnectedCount() + "/" + stations.Count, 380, PgH-400, 320, 210, Matcha);
        StatBox(pg.transform, "钻石", Mathf.FloorToInt(diamonds).ToString(), 732, PgH-400, 320, 210, Sakura);
        var regionalLines = lines.FindAll(line => PathTouchesRegion(line.st, currentRegion));
        Txt(pg.transform, currentRegion + "航线 " + regionalLines.Count + " / " + LineLimit() + " · 当前时段日化旅次 " + Mathf.RoundToInt(traffic != null ? traffic.totalPassengers : 0f), 24, InkSoft, 28, PgH-465, 1000, 40, TextAnchor.MiddleLeft);
        var regionBusy = CollectTopCongestedSegments(3, currentRegion);
        float barY = PgH - 548;
        if (regionBusy.Count > 0)
        {
            var topSeg = regionBusy[0];
            int peakPct = Mathf.RoundToInt(topSeg.loadRatio * 100f);
            var alertBar = Btn(pg.transform, "RegionCongestionBar",
                "繁忙急报：" + topSeg.line.name + " 等 " + regionBusy.Count + " 处区段负荷达 " + peakPct + "%    直飞疏导 ▸",
                22, 28, PgH - 526, W - 56, 48, Color.Lerp(CardBg, Vermilion, 0.14f), () => FocusAndBoostSegment(topSeg));
            TInk(alertBar, Vermilion);
            barY = PgH - 606;
        }

        Button regBtn = null, sortBtn = null;
        regBtn = Btn(pg.transform, "LinesRegionFilter", "归属：" + DispatchRegionFilters[linesRegionFilter] + "  ▾", 22, 28, barY, 498, 64,
            new Color(0.93f, 0.90f, 0.84f), () => ShowFleetDropdown(pg.transform, regBtn.transform, DispatchRegionFilters, linesRegionFilter, value => {
                if (linesRegionFilter != value) { linesRegionFilter = value; ResetScrollPosition("lines:" + currentRegion); }
                OpenLinesScreen();
            }));
        sortBtn = Btn(pg.transform, "LinesSortMode", "排序：" + DispatchSortOptions[linesSortMode] + "  ▾", 22, 554, barY, 498, 64,
            new Color(0.93f, 0.90f, 0.84f), () => ShowFleetDropdown(pg.transform, sortBtn.transform, DispatchSortOptions, linesSortMode, value => {
                if (linesSortMode != value) { linesSortMode = value; ResetScrollPosition("lines:" + currentRegion); }
                OpenLinesScreen();
            }));
        TInk(regBtn, Ink); TInk(sortBtn, Ink);
        if (!regBtn.GetComponent<FleetPressFeedback>()) regBtn.gameObject.AddComponent<FleetPressFeedback>();
        if (!sortBtn.GetComponent<FleetPressFeedback>()) sortBtn.gameObject.AddComponent<FleetPressFeedback>();

        var rows = CollectFilteredSortedLines(linesRegionFilter, linesSortMode);
        var list = FleetScroll(pg.transform, 36, barY - 14f, Mathf.Max(1, rows.Count) * 188f + 24, scrollKey: "lines:" + currentRegion);
        float top = list.sizeDelta.y - 184f;
        if (rows.Count == 0)
        {
            string emptyMsg = lines.Count == 0 ? "还没有运营中的航线" : "当前筛选下暂无航线";
            Txt(list, emptyMsg, 30, InkSoft, 20, list.sizeDelta.y - 180f, W - 96, 80, TextAnchor.MiddleCenter);
        }
        for (int i = 0; i < rows.Count; i++)
        {
            var item = rows[i];
            var line = item.line; int id = line.id;
            var card = Rect("LC"+i, list, 0, top - i*188, W-56, 172);
            Rounded(card, new Color(1,1,1,0.95f)); SoftShadow(card);
            var dot = Rect("D", card.transform, 26, 64, 44, 44); Img(dot, line.color, spriteCircle);
            Txt(card.transform, line.name, 40, Ink, 92, 100, 340, 52, TextAnchor.MiddleLeft);
            var sub = Txt(card.transform, item.startRegion + " · " + UniqueCount(line.st) + " 站 · " + item.km.ToString("0.0") + " km · 预计 " + Mathf.RoundToInt(item.daily) + "/天", 24, InkSoft, 94, 48, 510, 36, TextAnchor.MiddleLeft);
            FitSingleLine(sub, 24);
            var locBtn = Btn(card.transform, "Locate", "定位", 28, W-56-384, 42, 138, 88, Paper2, () => JumpToLineStartStation(line));
            TInk(locBtn, Indigo);
            var gb = Btn(card.transform, "Go", "查看", 30, W-56-228, 42, 196, 88, new Color(0.92f,0.89f,0.83f), () => OpenLineScreen(id, OpenLinesScreen));
            TInk(gb, Ink);
        }
    }

    void JumpToLineStartStation(Line line)
    {
        if (line == null || line.st == null || line.st.Count == 0) return;
        int stIdx = line.st[0];
        if (stIdx < 0 || stIdx >= stations.Count) return;
        string targetReg = stations[stIdx].region;
        ClosePanel(true);
        ClosePage();
        if (!string.IsNullOrEmpty(targetReg) && targetReg != currentRegion && RegionUnlocked(targetReg))
            SwitchRegion(targetReg);
        float z = IsMarsRegion(targetReg) ? 2.4f : 1.55f;
        Vector2 pt = Pos(stIdx);
        if (mapRect != null)
        {
            mapRect.localScale = Vector3.one * z;
            if (mapWheelZoom != null && mapWheelZoom.viewport != null)
            {
                Vector2 vpPt = mapWheelZoom.viewport.InverseTransformPoint(mapRect.TransformPoint(pt));
                Vector2 delta = mapWheelZoom.viewport.rect.center - vpPt;
                mapRect.anchoredPosition = mapWheelZoom.ClampPosition(mapRect.anchoredPosition + delta, z);
            }
        }
        RefreshAllLines();
        RefreshStationDots();
        PulseStation(stIdx);
        Toast("已定位至「" + line.name + "」起点站：" + StationDisplayName(stIdx, targetReg));
    }

    float LineDaily(Line line) { return ForecastFor(line).Profit; }

    // ---------- 航线详情 ----------
    bool lineScreenFleetExpanded;
    bool lineScreenOpsExpanded;
    void OpenLineScreen(int id, Action back = null)
    {
        var line = lines.Find(l => l.id == id);
        if (line == null) { OpenLinesScreen(); return; }
        var pg = OpenPage(line.name, "沿线客流 · 运力配属 · 日盈亏总览", "PageArt/line_watercolor", "line_screen_" + line.id);
        AttachPageCurrencyBadge(false);
        pageBack = back ?? OpenLinesScreen;
        string pendingTag = pendingForecastMask != 0 ? " 测算中" : "";
        StatBox(pg.transform, "客流", Mathf.RoundToInt(LinePassengerFlow(line)) + "", 28, PgH-440, 320, 200, Matcha);
        StatBox(pg.transform, "单向区段运力", Mathf.RoundToInt(LineCapacity(line)) + "", 380, PgH-440, 320, 200, Indigo);
        StatBox(pg.transform, "预计日利润" + pendingTag, Mathf.RoundToInt(LineDaily(line)) + "", 732, PgH-440, 320, 200, Gold);

        Action refreshSelf = () => OpenLineScreen(id, back);
        pageForecastRefresh = () => { if (pageRoot != null && activePageKey == "line_screen_" + line.id) OpenLineScreen(id, back); };

        var assignedGroups = FleetGroupsByLine(line.id);
        var idleGroups = FleetGroupsByLine(0);
        var idleEras = new HashSet<int>();
        foreach (var group in idleGroups) idleEras.Add(LineFleetEra(group.node));

        bool basic = techDone.Contains("dispatch_basic"), peak = techDone.Contains("dispatch_peak");
        int opRows = peak ? 4 : 1;

        float summaryH = 344f;
        float skinH = 84f;
        float fleetHeaderH = 92f;
        float fleetBodyH = lineScreenFleetExpanded
            ? (16f + 46f + (line.hasBaseTrain ? 132f : 0f) + assignedGroups.Count * 144f + 56f + (idleGroups.Count == 0 ? 100f : idleEras.Count * 46f + idleGroups.Count * 144f) + 12f)
            : 0f;
        float opsHeaderH = 92f;
        float opsBodyH = lineScreenOpsExpanded ? (16f + 140f + 44f + opRows * 124f + 12f) : 0f;
        float totalH = 18f + summaryH + 18f + skinH + 22f + fleetHeaderH + fleetBodyH + 18f + opsHeaderH + opsBodyH + 24f;

        var content = FleetScroll(pg.transform, 198, PgH - 462, totalH, scrollKey: "line:screen:" + line.id);
        float width = content.sizeDelta.x;
        float y = content.sizeDelta.y - 18f;

        // 1. 概览与沿线摘要卡
        var summaryCard = Rect("LineSummaryCard", content, 0, y - summaryH, width, summaryH);
        Rounded(summaryCard, new Color(1, 1, 1, 0.94f)); SoftShadow(summaryCard);
        float peakRatio = LinePeakLoadRatio(line);
        string loadTag = peakRatio >= 0.98f ? " · 峰段爆满 " + Mathf.RoundToInt(peakRatio * 100f) + "%"
            : peakRatio >= 0.78f ? " · 峰段繁忙 " + Mathf.RoundToInt(peakRatio * 100f) + "%"
            : " · 峰段负荷 " + Mathf.RoundToInt(peakRatio * 100f) + "%";
        var fleetMeta = Txt(summaryCard.transform, "配属 " + line.trains + "/8 列 · 平均速度 " + Mathf.RoundToInt(FleetTrait(line, 2) * 100) + "% · " + FormatLineKm(line.st) + loadTag, 24, Ink, 24, summaryH - 58f, width - 48f, 40f, TextAnchor.MiddleLeft);
        FitSingleLine(fleetMeta, 24);
        var forecast = ForecastFor(line);
        var costMeta = Txt(summaryCard.transform, $"维护成本 -¥{Mathf.RoundToInt(forecast.vehicle + forecast.track)}（星舰 ¥{Mathf.RoundToInt(forecast.vehicle)} · 航线 ¥{Mathf.RoundToInt(forecast.track)}） · 税收成本 -¥{Mathf.RoundToInt(forecast.tax)}", 22, InkSoft, 24, summaryH - 100f, width - 48f, 34f, TextAnchor.MiddleLeft);
        FitSingleLine(costMeta, 22);
        Txt(summaryCard.transform, "沿线", 22, InkSoft, 24, summaryH - 142f, 120f, 32f, TextAnchor.MiddleLeft);
        Txt(summaryCard.transform, UniquePathText(line.st), 23, Ink, 24, 18f, width - 48f, summaryH - 166f, TextAnchor.UpperLeft);
        y -= summaryH + 18f;

        // 2. 航线涂装入口
        int curSkin = GetLineSkin(line);
        string skinBtnText = curSkin >= 0
            ? "航线涂装 · " + LineSkinNames[curSkin] + "（" + SkinCategoryName(curSkin) + " · " + SkinPerkSummary(curSkin) + "）"
            : "航线涂装 · 解锁外观与流派加成";
        var colors = Btn(content, "LineSkins", skinBtnText, 24, 0, y - skinH, width, skinH, Paper2, () => OpenLineSkins(line)); TInk(colors, Ink);
        y -= skinH + 22f;

        // 3. 星舰配属下拉收纳面板
        var fleetHdr = Rect("FleetDropdownHeader", content, 0, y - fleetHeaderH, width, fleetHeaderH);
        Rounded(fleetHdr, lineScreenFleetExpanded ? new Color(0.97f, 0.95f, 0.90f, 0.98f) : CardBg); SoftShadow(fleetHdr);
        var fhBtn = fleetHdr.AddComponent<Button>(); fhBtn.targetGraphic = fleetHdr.GetComponent<Image>();
        fhBtn.onClick.AddListener(() => { lineScreenFleetExpanded = !lineScreenFleetExpanded; refreshSelf(); });
        Txt(fleetHdr.transform, "星舰配属", 28, Ink, 24, 42f, 360f, 40f, TextAnchor.MiddleLeft);
        Txt(fleetHdr.transform, "已配属 " + line.trains + " / 8 艘 · 舰坞可用 " + idleGroups.Count + " 型", 22, InkSoft, 24, 10f, width - 160f, 32f, TextAnchor.MiddleLeft);
        Txt(fleetHdr.transform, lineScreenFleetExpanded ? "▴ 收起" : "▾ 展开", 24, Indigo, width - 156f, 22f, 132f, 48f, TextAnchor.MiddleRight);
        y -= fleetHeaderH;

        if (lineScreenFleetExpanded)
        {
            y -= 16f;
            Txt(content, "本线已配属星舰", 24, Ink, 12, y - 38f, width - 24f, 36f, TextAnchor.MiddleLeft); y -= 46f;
            if (line.hasBaseTrain)
            {
                var basicCard = Rect("InlineBaseVehicle", content, 0, y - 122f, width, 122f); Rounded(basicCard, CardBg);
                bool canWithdrawBase = line.trains > 1;
                float textW = canWithdrawBase ? (width - 250f) : (width - 44f);
                Txt(basicCard.transform, "航线基础星舰 ×1", 26, Ink, 22, 68f, textW, 38f, TextAnchor.MiddleLeft);
                Txt(basicCard.transform, canWithdrawBase ? "本线不止 1 艘星舰，可撤下基础星舰腾出编队。" : "当前仅此 1 艘，调入其他星舰后可撤下。", 21, InkSoft, 22, 18f, textW, 34f, TextAnchor.MiddleLeft);
                if (canWithdrawBase)
                {
                    var wb = Btn(basicCard.transform, "WithdrawBase", "撤回", 23, width - 204f, 26f, 182f, 70f, Paper2, () => WithdrawBaseTrain(line, refreshSelf));
                    TInk(wb, Ink);
                }
                y -= 132f;
            }

            foreach (var group in assignedGroups)
            {
                var item = group.first; int model = group.node;
                var card = Rect("InlineAssigned" + model, content, 0, y - 134f, width, 134f); Rounded(card, CardBg);
                var t = Txt(card.transform, LocoNames[model] + " ×" + group.count, 27, Ink, 22, 76f, width - 250f, 40f, TextAnchor.MiddleLeft); FitSingleLine(t, 27);
                var s = Txt(card.transform, LineFleetVehicleStats(model), 21, InkSoft, 22, 22f, width - 250f, 38f, TextAnchor.MiddleLeft); FitSingleLine(s, 21);
                var withdraw = Btn(card.transform, "Withdraw", "撤回", 23, width - 204f, 31f, 182f, 70f, Paper2, () => ChangeLineFleetVehicle(line, item, false, refreshSelf)); TInk(withdraw, Ink);
                y -= 144f;
            }

            Txt(content, "舰坞空闲星舰", 24, Ink, 12, y - 44f, width - 24f, 38f, TextAnchor.MiddleLeft); y -= 56f;
            if (idleGroups.Count == 0)
            {
                Txt(content, "暂无空闲星舰，可从其他航线撤回或前往下方「星舰商店」。", 22, InkSoft, 20, y - 80f, width - 40f, 70f, TextAnchor.MiddleCenter);
                y -= 100f;
            }
            int prevEra = -1;
            string[] eraNames = { "I代 · 核热推进", "II代 · 聚变磁轨", "III代 · 曲速流线" };
            foreach (var group in idleGroups)
            {
                int era = LineFleetEra(group.node);
                if (era != prevEra) { Txt(content, eraNames[era], 22, Indigo, 16, y - 36f, width - 32f, 34f, TextAnchor.MiddleLeft); y -= 46f; prevEra = era; }
                var item = group.first;
                var card = Rect("InlineIdle" + group.node, content, 0, y - 134f, width, 134f); Rounded(card, CardBg);
                var t = Txt(card.transform, LocoNames[group.node] + " · 可用 " + group.count + " 艘", 26, Ink, 22, 76f, width - 250f, 40f, TextAnchor.MiddleLeft); FitSingleLine(t, 26);
                var s = Txt(card.transform, LineFleetVehicleStats(group.node), 21, InkSoft, 22, 22f, width - 250f, 38f, TextAnchor.MiddleLeft); FitSingleLine(s, 21);
                var assign = Btn(card.transform, "Assign", "调入本线", 23, width - 204f, 31f, 182f, 70f, Indigo, () => ChangeLineFleetVehicle(line, item, true, refreshSelf)); TInk(assign, Color.white);
                y -= 144f;
            }
            y -= 12f;
        }
        y -= 18f;

        // 4. 票价与分时调度下拉收纳面板
        var opsHdr = Rect("OpsDropdownHeader", content, 0, y - opsHeaderH, width, opsHeaderH);
        Rounded(opsHdr, lineScreenOpsExpanded ? new Color(0.97f, 0.95f, 0.90f, 0.98f) : CardBg); SoftShadow(opsHdr);
        var ohBtn = opsHdr.AddComponent<Button>(); ohBtn.targetGraphic = opsHdr.GetComponent<Image>();
        ohBtn.onClick.AddListener(() => { lineScreenOpsExpanded = !lineScreenOpsExpanded; refreshSelf(); });
        Txt(opsHdr.transform, "票价与调度", 28, Ink, 24, 42f, 360f, 40f, TextAnchor.MiddleLeft);
        Txt(opsHdr.transform, "起步票价 ¥" + line.fare.ToString("0.0") + " · 当前开航 " + RunningTrains(line) + " 列", 22, InkSoft, 24, 10f, width - 160f, 32f, TextAnchor.MiddleLeft);
        Txt(opsHdr.transform, lineScreenOpsExpanded ? "▴ 收起" : "▾ 展开", 24, Indigo, width - 156f, 22f, 132f, 48f, TextAnchor.MiddleRight);
        y -= opsHeaderH;

        if (lineScreenOpsExpanded)
        {
            y -= 16f;
            var fare = Rect("InlineFare", content, 0, y - 130f, width, 130f); Rounded(fare, new Color(1, 1, 1, 0.94f));
            Txt(fare.transform, "起步票价  ¥" + line.fare.ToString("0.0") + " / 人", 28, Ink, 22, 68f, 540f, 40f, TextAnchor.MiddleLeft);
            Txt(fare.transform, techDone.Contains("fare_policy") ? "起步价叠加跨站里程费 · 星票款按 10:1 计入金币" : "研究「旅客星运票价制度」后可自主调价", 21, InkSoft, 22, 18f, 640f, 36f, TextAnchor.MiddleLeft);
            var fm = Btn(fare.transform, "FareMinus", "−", 32, width - 252f, 27f, 104f, 76f, Paper2, () => ChangeLineFare(line, -0.1f, refreshSelf)); TInk(fm, Ink);
            var fp = Btn(fare.transform, "FarePlus", "+", 32, width - 128f, 27f, 104f, 76f, Paper2, () => ChangeLineFare(line, 0.1f, refreshSelf)); TInk(fp, Ink);
            y -= 140f;

            string schedHint = !basic ? "研究「星航星航时刻表」解锁统一调度" : !peak ? "统一调度已就绪；研究「分时航班计划」可分时段增减" : "分时调度 · 高峰与夜间独立起航";
            Txt(content, schedHint, 22, InkSoft, 12, y - 34f, width - 24f, 34f, TextAnchor.MiddleLeft); y -= 44f;
            for (int i = 0; i < opRows; i++)
            {
                int slot = i;
                var row = Rect("InlineDispatchSlot" + i, content, 0, y - 112f, width, 112f); Rounded(row, new Color(1, 1, 1, 0.94f));
                Txt(row.transform, peak ? DispatchNames[i] : "全天统一", 25, Ink, 22, 64f, 380f, 36f, TextAnchor.MiddleLeft);
                Txt(row.transform, line.dispatch[i] + " 列", 28, Indigo, width - 380f, 40f, 114f, 40f, TextAnchor.MiddleCenter);
                int sampleSlot = peak ? i : DispatchSlot();
                float oldClock = dayClock;
                float[] sampleHours = { 8f, 13f, 18f, 1f };
                try
                {
                    dayClock = sampleHours[sampleSlot] * GameDaySeconds / 24f;
                    var sample = trafficSlots[sampleSlot];
                    float requested = sample != null && sample.linePeakDemand.TryGetValue(line, out var d) ? d : 0f;
                    float gross = sample != null ? TrafficRevenue(line, sample) : 0f;
                    var rowSub = Txt(row.transform, "最高区段需求 " + Mathf.RoundToInt(requested) + " · 运力 " + Mathf.RoundToInt(LineCapacity(line)) + " · 收入 ¥" + Mathf.RoundToInt(gross), 21, InkSoft, 22, 12f, width - 290f, 34f, TextAnchor.MiddleLeft);
                    FitSingleLine(rowSub, 21);
                }
                finally { dayClock = oldClock; }
                var minus = Btn(row.transform, "Minus", "−", 32, width - 252f, 18f, 104f, 76f, Paper2, () => SetLineDispatch(line, slot, line.dispatch[slot] - 1, refreshSelf)); TInk(minus, Ink);
                var plus = Btn(row.transform, "Plus", "+", 32, width - 128f, 18f, 104f, 76f, Paper2, () => SetLineDispatch(line, slot, line.dispatch[slot] + 1, refreshSelf)); TInk(plus, Ink);
                y -= 124f;
            }
        }

        Btn(pg.transform, "Add", "星舰商店", 30, 28, 56, 490, 124, Indigo, () => { fleetBack = () => OpenLineScreen(id, back); OpenLocoShop(); });
        Btn(pg.transform, "Del", "拆除航线", 30, 544, 56, 508, 124, Vermilion, () => {
            int refund = Mathf.RoundToInt(120f * (UniqueCount(line.st) - 1) * TrackBuildCostFactor() * DemolishRefundRate());
            Confirm("拆除 " + line.name, "退还 ¥" + refund, () => {
                BeginUndo("拆除整条航线"); coins += refund; SettleRemovedLineTax(line); ReleaseLineFleet(line); lines.Remove(line);
                CreateLineVisuals(); RefreshStationDots(); RecalcIncome(); EndUndo(); RefreshHUD(); ClosePanel();
                ClosePage(); OpenLinesScreen();
            });
        });
    }

    // ---------- 调度 / 舰坞 / 商店 ----------
    int fleetPowerFilter, fleetUseFilter, fleetRegionFilter, shopCategory;
    Action fleetBack;
    Action lineFleetBackAction, lineFleetOriginFleetBack, lineFleetShopReturn;
    int lineFleetLineId, lineFleetSlot = -1, lineFleetAmount;
    static readonly string[] FleetPowerFilters = { "全部动力", "核热", "聚变", "电磁", "离子", "混动", "其他" };
    static readonly string[] FleetUseFilters = { "全部用途", "旅客星运优先", "货物星运优先", "客货兼顾" };
    static readonly string[] FleetRegionFilters = { "全部星域", "地球", "月球", "火星", "木星", "土星", "深空域" };
    // 分类沿用目录用途划分；星域与动力覆盖三代共 16 款星舰。
    static readonly int[] FleetPowers =  { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 3, 3, 3 };
    static readonly int[] FleetUses =    { 3, 2, 3, 3, 3, 2, 3, 3, 1, 1, 1, 2, 3, 1, 1, 1 };
    static readonly int[] FleetRegions = { 1, 1, 1, 1, 1, 2, 3, 4, 2, 1, 1, 2, 6, 5, 6, 6 };
    void OpenGarageScreen() { fleetBack = null; OpenFleetTab(0); }
    void OpenResearchInventory() { OpenFleetTab(1); }
    void OpenLocoShop() { shopCategory = 0; OpenFleetTab(2); }
    void OpenThemeShopScreen() { fleetBack = null; shopCategory = 2; OpenFleetTab(2); }
    bool FleetMatches(int index)
    {
        if (index < 0 || index >= FleetPowers.Length) return false;
        return (fleetPowerFilter == 0 || FleetPowers[index] == fleetPowerFilter) &&
               (fleetUseFilter == 0 || FleetUses[index] == fleetUseFilter) &&
               (fleetRegionFilter == 0 || FleetRegions[index] == fleetRegionFilter);
    }
    void OpenFleetTab(int tab)
    {
        tab = Mathf.Clamp(tab, 0, 2);
        string[] titles = { "调度", "舰坞", "商店" };
        string[] help = {
            "统筹全网各航线配舰与分时航班，优化客流运力比。",
            "管理在库星舰资产，随时调拨或增配至主航线。",
            "选购星舰、共轨双线涂装与全局手账主题；多款外观可在星图映像馆抢先试用。"
        };
        string[] backgrounds = { "dispatch_watercolor", "garage_watercolor", "shop_watercolor" };
        var pg = OpenPage(titles[tab], help[tab], "FleetArt/" + backgrounds[tab], "fleet_shop");
        AttachPageCurrencyBadge(false);
        pageBack = fleetBack;
        if (tab == 0) BuildFleetDispatch(pg.transform);
        else if (tab == 1) BuildFleetCollection(pg.transform, tab);
        else
        {
            if (shopCategory == 0) BuildFleetCollection(pg.transform, 2);
            else if (shopCategory == 1) BuildShopLiveries(pg.transform);
            else if (shopCategory == 2) BuildShopThemes(pg.transform);
            else BuildShopTrainSkins(pg.transform);
            BuildShopSidebar(pg.transform);
        }
        BuildFleetNavigation(pg.transform, tab);
    }
    void BuildShopSidebar(Transform page)
    {
        var rail = Rect("ShopSidebar", page, 28, PgH - 648, 136, 402);
        Rounded(rail, CardBg); SoftShadow(rail);
        string[] cats = { "星舰", "涂装", "风格", "车皮" };
        Color activeBg = new Color(Matcha.r, Matcha.g, Matcha.b, 0.25f);
        for (int i = 0; i < cats.Length; i++)
        {
            int cat = i;
            bool selected = shopCategory == i;
            var btn = Btn(rail.transform, "ShopCat" + i, cats[i], 25, 6, 304 - i * 98, 124, 88,
                selected ? activeBg : new Color(Paper2.r, Paper2.g, Paper2.b, 0.72f),
                () => { shopCategory = cat; OpenFleetTab(2); });
            TInk(btn, selected ? Ink : InkSoft);
        }
    }
    void BuildFleetNavigation(Transform page, int selected)
    {
        Transform host = pageRoot ? pageRoot.transform : page;
        GetSafeMarginsInRoot(out float safeL, out float safeR, out _, out float safeB);
        float safeW = Mathf.Max(320f, RootW - safeL - safeR);
        float navSlotX = pageRoot ? (safeL + (safeW - W) * 0.5f) : 0f;
        float navBaseY = pageRoot ? safeB : 0f;

        var bar = Rect("FleetBottomNav", host, 0, navBaseY, RootW, 154);
        Rounded(bar, CardBg); SoftShadow(bar);
        if (navBaseY > 0.5f)
        {
            var skirt = Rect("BottomSafeSkirt", bar.transform, 0, -navBaseY, RootW, navBaseY + 12f);
            Img(skirt, CardBg).raycastTarget = false;
        }
        if (!pageRoot) bar.AddComponent<FleetBottomEdge>();
        string[] labels = { "调度", "舰坞", "商店" };
        string[] subtitles = { "DISPATCH", "HANGAR", "SHOP" };
        for (int i = 0; i < labels.Length; i++)
        {
            int target = i;
            var item = Rect("Nav" + i, bar.transform, navSlotX + i * 360, 0, 360, 154);
            var hit = Img(item, new Color(1, 1, 1, 0));
            if (i == selected) Img(Rect("Selected", item.transform, 105, 144, 150, 6), Gold, spriteRound).raycastTarget = false;
            Txt(item.transform, labels[i], 34, i == selected ? Ink : InkSoft, 0, 57, 360, 52, TextAnchor.MiddleCenter);
            Txt(item.transform, subtitles[i], 20, InkSoft, 0, 22, 360, 30, TextAnchor.MiddleCenter);
            var button = item.AddComponent<Button>(); button.targetGraphic = hit;
            button.onClick.AddListener(() => { if (target != selected) OpenFleetTab(target); });
            item.AddComponent<FleetPressFeedback>();
        }
    }
    readonly Dictionary<string, float> scrollPositions = new Dictionary<string, float>();
    readonly Dictionary<string, ScrollRect> scrollViews = new Dictionary<string, ScrollRect>();
    readonly Dictionary<string, int> scrollBindingVersions = new Dictionary<string, int>();
    static float ClampedScrollY(float position, float height, float viewHeight)
        => float.IsNaN(position) || float.IsInfinity(position) ? 0f : Mathf.Clamp(position, 0f, Mathf.Max(0f, height - viewHeight));
    float RememberScrollPosition(string key, float position, float height, float viewHeight)
    {
        float y = ClampedScrollY(position, height, viewHeight);
        scrollPositions[key] = y;
        return y;
    }
    float SavedScrollPosition(string key, float height, float viewHeight)
        => ClampedScrollY(scrollPositions.TryGetValue(key, out float y) ? y : 0f, height, viewHeight);
    void RestoreScrollPosition(ScrollRect scroll, string key)
    {
        if (!scroll || !scroll.content || !scroll.viewport) return;
        float y = SavedScrollPosition(key, scroll.content.rect.height, scroll.viewport.rect.height);
        scroll.StopMovement();
        scroll.content.anchoredPosition = new Vector2(scroll.content.anchoredPosition.x, y);
        scrollPositions[key] = y;
    }
    void BindScrollPosition(ScrollRect scroll, string key)
    {
        if (string.IsNullOrEmpty(key)) throw new ArgumentException("Scroll memory needs a stable key", nameof(key));
        int version = NextScrollBinding(key);
        scrollViews[key] = scroll;
        RestoreScrollPosition(scroll, key);
        scroll.onValueChanged.AddListener(_ => {
            // A destroyed/replaced view can still emit once in the same frame.
            if (scroll && scroll.content && scroll.viewport && IsCurrentScrollBinding(key, version))
                RememberScrollPosition(key, scroll.content.anchoredPosition.y, scroll.content.rect.height, scroll.viewport.rect.height);
        });
    }
    int NextScrollBinding(string key)
    {
        int version = scrollBindingVersions.TryGetValue(key, out int previous) ? previous + 1 : 1;
        scrollBindingVersions[key] = version;
        return version;
    }
    bool IsCurrentScrollBinding(string key, int version)
        => scrollBindingVersions.TryGetValue(key, out int current) && current == version;
    void CaptureScrollPositions()
    {
        foreach (var entry in scrollViews)
        {
            var scroll = entry.Value;
            if (scroll && scroll.content && scroll.viewport)
                RememberScrollPosition(entry.Key, scroll.content.anchoredPosition.y, scroll.content.rect.height, scroll.viewport.rect.height);
        }
    }
    void ResetScrollPosition(string key)
    {
        scrollPositions[key] = 0f;
        if (scrollViews.TryGetValue(key, out var scroll) && scroll) RestoreScrollPosition(scroll, key);
    }
    string FleetCollectionScrollKey(int tab) => "fleet:collection:" + tab;
    void ResetFleetFilterScrolls()
    {
        // These filters are shared by both the garage and locomotive shop.
        ResetScrollPosition(FleetCollectionScrollKey(1));
        ResetScrollPosition(FleetCollectionScrollKey(2));
    }
    RectTransform FleetScroll(Transform page, float bottom, float top, float requiredHeight, float leftInset = 0f, string scrollKey = null)
    {
        float viewHeight = top - bottom;
        float width = W - 56 - leftInset;
        var view = Rect("FleetViewport", page, 28 + leftInset, bottom, width, viewHeight);
        Img(view, new Color(1, 1, 1, 0.01f));
        var mask = view.AddComponent<Mask>(); mask.showMaskGraphic = false;
        var scroll = view.AddComponent<ScrollRect>(); scroll.horizontal = false; scroll.vertical = true;
        scroll.inertia = true; scroll.decelerationRate = 0.12f; scroll.scrollSensitivity = 110f; scroll.movementType = ScrollRect.MovementType.Elastic;
        scroll.viewport = view.GetComponent<RectTransform>();
        float height = Mathf.Max(viewHeight, requiredHeight);
        var content = Rect("FleetContent", view.transform, 0, 0, width, height);
        var rt = content.GetComponent<RectTransform>(); rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1); rt.anchoredPosition = Vector2.zero; scroll.content = rt;
        BindScrollPosition(scroll, scrollKey ?? (activePageKey + ":" + page.name));
        return rt;
    }
    int fleetDispatchRegionFilter;
    int fleetDispatchSortMode;
    static readonly string[] DispatchRegionFilters = { "全部", "月之亮面", "月背", "火星·西半球", "火星·东半球" };
    static readonly string[] DispatchSortOptions = { "默认编号", "日利润降序", "日利润升序", "里程降序", "里程升序" };
    List<(Line line, string startRegion, float daily, float km)> CollectFilteredSortedLines(int regionFilter, int sortMode)
    {
        var rows = new List<(Line line, string startRegion, float daily, float km)>();
        string wantReg = (regionFilter <= 0 || regionFilter >= DispatchRegionFilters.Length) ? null : DispatchRegionFilters[regionFilter];
        for (int i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            if (line == null || line.st == null || line.st.Count == 0) continue;
            int st0 = line.st[0];
            string startReg = (st0 >= 0 && st0 < stations.Count) ? stations[st0].region : "";
            if (wantReg != null && startReg != wantReg) continue;
            rows.Add((line, startReg, LineDaily(line), LineLengthKm(line.st)));
        }
        if (sortMode == 1) rows.Sort((a, b) => b.daily.CompareTo(a.daily));
        else if (sortMode == 2) rows.Sort((a, b) => a.daily.CompareTo(b.daily));
        else if (sortMode == 3) rows.Sort((a, b) => b.km.CompareTo(a.km));
        else if (sortMode == 4) rows.Sort((a, b) => a.km.CompareTo(b.km));
        return rows;
    }
    void BuildFleetDispatch(Transform page)
    {
        Button regBtn = null, sortBtn = null;
        float barY = PgH - 270;
        regBtn = Btn(page, "DispatchRegionFilter", "归属：" + DispatchRegionFilters[fleetDispatchRegionFilter] + "  ▾", 22, 28, barY, 498, 72,
            new Color(0.93f, 0.90f, 0.84f), () => ShowFleetDropdown(page, regBtn.transform, DispatchRegionFilters, fleetDispatchRegionFilter, value => {
                if (fleetDispatchRegionFilter != value) { fleetDispatchRegionFilter = value; ResetScrollPosition("fleet:dispatch:" + currentRegion); }
                OpenFleetTab(0);
            }));
        sortBtn = Btn(page, "DispatchSortMode", "排序：" + DispatchSortOptions[fleetDispatchSortMode] + "  ▾", 22, 554, barY, 498, 72,
            new Color(0.93f, 0.90f, 0.84f), () => ShowFleetDropdown(page, sortBtn.transform, DispatchSortOptions, fleetDispatchSortMode, value => {
                if (fleetDispatchSortMode != value) { fleetDispatchSortMode = value; ResetScrollPosition("fleet:dispatch:" + currentRegion); }
                OpenFleetTab(0);
            }));
        TInk(regBtn, Ink); TInk(sortBtn, Ink);
        if (!regBtn.GetComponent<FleetPressFeedback>()) regBtn.gameObject.AddComponent<FleetPressFeedback>();
        if (!sortBtn.GetComponent<FleetPressFeedback>()) sortBtn.gameObject.AddComponent<FleetPressFeedback>();

        var rows = CollectFilteredSortedLines(fleetDispatchRegionFilter, fleetDispatchSortMode);

        var content = FleetScroll(page, 172, PgH - 284, Mathf.Max(1, rows.Count) * 360f + 24, scrollKey: "fleet:dispatch:" + currentRegion);
        float height = content.sizeDelta.y;
        if (rows.Count == 0)
        {
            string emptyMsg = lines.Count == 0 ? "还没有运营中的航线" : "当前筛选下暂无航线";
            Txt(content, emptyMsg, 30, InkSoft, 20, height - 180, W - 96, 80, TextAnchor.MiddleCenter);
        }
        for (int i = 0; i < rows.Count; i++)
        {
            var item = rows[i];
            var line = item.line;
            var card = Rect("Dispatch" + i, content, 0, height - 350 - i * 360, W - 56, 340);
            Rounded(card, CardBg); SoftShadow(card);
            Img(Rect("Dot", card.transform, 28, 262, 38, 38), line.color, spriteCircle).raycastTarget = false;
            Txt(card.transform, line.name, 36, Ink, 84, 260, 620, 48, TextAnchor.MiddleLeft);
            var locBtn = Btn(card.transform, "Locate", "定位起点 ▸", 22, W - 56 - 204, 256, 176, 56, Paper2, () => JumpToLineStartStation(line));
            TInk(locBtn, Indigo);
            var lineStats = Txt(card.transform, item.startRegion + " · " + UniqueCount(line.st) + " 站 · " + item.km.ToString("0.0") + " km · " + line.trains + " 列 · 预计日利润 " + Mathf.RoundToInt(item.daily), 23, InkSoft, 28, 205, 960, 38, TextAnchor.MiddleLeft);
            FitSingleLine(lineStats, 23);
            var assigned = researchVehicles.Find(v => v.lineId == line.id);
            Txt(card.transform, "配置：" + (assigned == null ? "默认飞车" : LocoNames[assigned.node]), 24, InkSoft, 28, 156, 940, 34, TextAnchor.MiddleLeft);
            var fleetButton = Btn(card.transform, "Fleet", "星舰配属", 25, 28, 38, 450, 88, Indigo, () => OpenLineFleet(line, () => OpenFleetTab(0))); TInk(fleetButton, Color.white);
            var operations = Btn(card.transform, "Operations", "票价与调度", 25, 495, 38, 450, 88, Indigo, () => OpenLineOperations(line)); TInk(operations, Color.white);
            { var entry = card.AddComponent<FleetCardEntrance>(); entry.delay = Mathf.Min(i, 6) * 0.032f; }
        }
    }
    void BuildFleetFilters(Transform page, int tab)
    {
        Button power = null, use = null, region = null;
        float left = tab == 2 ? 178 : 28, width = tab == 2 ? 282 : 326, gap = tab == 2 ? 14 : 23;
        power = Btn(page, "PowerFilter", "动力：" + FleetPowerFilters[fleetPowerFilter] + "  ▾", 22, left, PgH - 285, width, 72,
            new Color(0.93f, 0.90f, 0.84f), () => ShowFleetDropdown(page, power.transform, FleetPowerFilters, fleetPowerFilter, value => { if (fleetPowerFilter != value) { fleetPowerFilter = value; ResetFleetFilterScrolls(); } OpenFleetTab(tab); }));
        use = Btn(page, "UseFilter", "用途：" + FleetUseFilters[fleetUseFilter] + "  ▾", 22, left + width + gap, PgH - 285, width, 72,
            new Color(0.93f, 0.90f, 0.84f), () => ShowFleetDropdown(page, use.transform, FleetUseFilters, fleetUseFilter, value => { if (fleetUseFilter != value) { fleetUseFilter = value; ResetFleetFilterScrolls(); } OpenFleetTab(tab); }));
        region = Btn(page, "RegionFilter", "地区：" + FleetRegionFilters[fleetRegionFilter] + "  ▾", 22, left + 2 * (width + gap), PgH - 285, width, 72,
            new Color(0.93f, 0.90f, 0.84f), () => ShowFleetDropdown(page, region.transform, FleetRegionFilters, fleetRegionFilter, value => { if (fleetRegionFilter != value) { fleetRegionFilter = value; ResetFleetFilterScrolls(); } OpenFleetTab(tab); }));
        TInk(power, Ink); TInk(use, Ink); TInk(region, Ink);
        if (!power.GetComponent<FleetPressFeedback>()) power.gameObject.AddComponent<FleetPressFeedback>();
        if (!use.GetComponent<FleetPressFeedback>()) use.gameObject.AddComponent<FleetPressFeedback>();
        if (!region.GetComponent<FleetPressFeedback>()) region.gameObject.AddComponent<FleetPressFeedback>();
    }
    void ShowFleetDropdown(Transform page, Transform source, string[] options, int selected, Action<int> choose)
    {
        var old = page.Find("FleetDropdownOverlay");
        if (old) Destroy(old.gameObject);
        var sourceRect = (RectTransform)source;
        var pageRect = (RectTransform)page;
        var corners = new Vector3[4]; sourceRect.GetWorldCorners(corners);
        var screen = RectTransformUtility.WorldToScreenPoint(null, corners[0]);
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(pageRect, screen, null, out var localPt)) return;
        Vector2 point = localPt - pageRect.rect.min;
        float ph = Mathf.Max(PgH, pageRect.rect.height);
        float pw = Mathf.Max(W, pageRect.rect.width);
        var overlay = Rect("FleetDropdownOverlay", page, 0, 0, pw, ph);
        var overlayImage = Img(overlay, new Color(0, 0, 0, 0));
        var dismiss = overlay.AddComponent<Button>(); dismiss.targetGraphic = overlayImage;
        dismiss.transition = Selectable.Transition.None; dismiss.onClick.AddListener(() => Destroy(overlay));
        float width = 326, rowHeight = 70, height = options.Length * rowHeight + 16;
        float x = Mathf.Clamp(point.x, 12, pw - width - 12);
        float y = Mathf.Clamp(point.y - height - 8, 160, ph - height - 150);
        var list = Rect("Options", overlay.transform, x, y, width, height);
        Rounded(list, new Color(0.99f, 0.98f, 0.95f, 1)); SoftShadow(list);
        for (int i = 0; i < options.Length; i++)
        {
            int index = i;
            var color = i == selected ? new Color(0.91f, 0.91f, 0.83f, 1) : new Color(1, 1, 1, 0.98f);
            var button = Btn(list.transform, "Option" + i, options[i] + (i == selected ? "  ✓" : ""), 24,
                8, height - 8 - (i + 1) * rowHeight, width - 16, rowHeight - 4, color, () => { Destroy(overlay); choose(index); });
            TInk(button, Ink); if (!button.GetComponent<FleetPressFeedback>()) button.gameObject.AddComponent<FleetPressFeedback>();
        }
    }
    int shopLocoBatchCount = 1;
    static readonly int[] ShopLocoBatchOptions = { 1, 5, 10 };
    readonly HashSet<int> garageExpandedModels = new HashSet<int>();
    sealed class GarageModelGroup
    {
        public int model;
        public List<ResearchVehicle> items;
        public RectTransform cardRect;
        public Text toggleLabel;
        public bool expanded;
        public float subBlockHeight;
        public readonly List<RectTransform> subRects = new List<RectTransform>();
    }
    void BuildFleetCollection(Transform page, int tab)
    {
        BuildFleetFilters(page, tab);
        var visibleItems = new List<ResearchVehicle>();
        var visibleModels = new List<int>();
        if (tab == 1)
        {
            foreach (var item in researchVehicles) if (FleetMatches(item.node)) visibleItems.Add(item);
        }
        else for (int i = 0; i < LocoNames.Length; i++) if (FleetMatches(i)) visibleModels.Add(i);
        int count = tab == 1 ? visibleItems.Count : visibleModels.Count;
        float inset = tab == 2 ? 150 : 0;
        Txt(page, tab == 1 ? "已拥有 " + count + " 艘 · 功勋退役 " + retiredVehiclesCount + " 艘" : "舰型  " + count + " 款 · 点击星舰卡查看详情", 23, InkSoft, 28 + inset, PgH - 338, 560 - inset, 34, TextAnchor.MiddleLeft);
        if (tab == 1)
        {
            var albumBtn = Btn(page, "OpenCultureFromGarage", "星图 · 票册 " + earnedMemorialTickets.Count + "/6 ▸", 20, W - 294f, PgH - 344, 266f, 44f,
                new Color(0.93f, 0.89f, 0.81f), () => OpenSceneryTab(1));
            TInk(albumBtn, Ink);
        }
        else
        {
            for (int bIdx = 0; bIdx < ShopLocoBatchOptions.Length; bIdx++)
            {
                int batch = ShopLocoBatchOptions[bIdx];
                bool sel = shopLocoBatchCount == batch;
                var bBtn = Btn(page, "Batch" + batch, "×" + batch, 21, W - 296f + bIdx * 92f, PgH - 344, 84f, 44f,
                    sel ? Indigo : new Color(0.93f, 0.89f, 0.81f), () => {
                        if (shopLocoBatchCount != batch) { shopLocoBatchCount = batch; OpenFleetTab(2); }
                    });
                TInk(bBtn, sel ? Color.white : Ink);
            }
        }

        if (tab == 1)
        {
            // 按型号聚合成有序分组（保持首次出现顺序）；默认全部收起，仅手动展开的型号显示逐艘子卡。
            var grouped = new Dictionary<int, List<ResearchVehicle>>();
            var modelOrder = new List<int>();
            foreach (var item in visibleItems)
            {
                int model = item.node;
                if (!grouped.TryGetValue(model, out var bucket))
                {
                    bucket = new List<ResearchVehicle>();
                    grouped.Add(model, bucket);
                    modelOrder.Add(model);
                }
                bucket.Add(item);
            }
            float requiredHeight = 0f;
            foreach (int model in modelOrder)
            {
                requiredHeight += 218f; // 主卡 198 + 间距 20
                if (garageExpandedModels.Contains(model)) requiredHeight += grouped[model].Count * 120f + 8f;
            }
            float garageViewHeight = (PgH - 354f) - 172f;
            var garageContent = FleetScroll(page, 172, PgH - 354, Mathf.Max(1f, requiredHeight), inset, scrollKey: FleetCollectionScrollKey(tab));
            float gHeight = garageContent.sizeDelta.y;
            float gCardWidth = garageContent.sizeDelta.x, gArtWidth = 270f;
            float gTextX = 304f, gActionWidth = 234f;
            float gActionX = gCardWidth - gActionWidth - 28f, gInfoWidth = gActionX - gTextX - 12f;
            if (visibleItems.Count == 0)
            {
                Txt(garageContent, "暂无符合条件的星舰\n解锁科技后可获得星舰", 29, InkSoft, 40, gHeight - 240, gCardWidth - 80, 150, TextAnchor.MiddleCenter);
                return;
            }
            RectTransform CreateGarageSubCard(int modelId, int idxInModel, ResearchVehicle item, float subTopPos, float subWidth)
            {
                var assigned = lines.Find(l => l.id == item.lineId);
                if (assigned == null) item.lineId = 0;
                var sub = Rect("GarageVehicle" + modelId + "_" + idxInModel, garageContent, 24f, subTopPos - 108f, subWidth, 108f);
                Rounded(sub, Paper2);
                var itemCaptured = item;
                int refundCoins = Mathf.Max(80, Mathf.RoundToInt(LocoPrice(modelId) * 0.55f));
                var label = Txt(sub.transform, $"#{idxInModel + 1} · " + (assigned == null ? "舰坞待命" : "已配置至 " + assigned.name),
                    22, Ink, 20, 34, subWidth - (assigned == null ? 292f : 182f), 40, TextAnchor.MiddleLeft);
                FitSingleLine(label, 22);
                if (assigned == null)
                {
                    var assignBtn = Btn(sub.transform, "Action", "配置航线", 21, subWidth - 140f, 18, 124f, 72f, new Color(0.92f, 0.89f, 0.83f), () => ChooseResearchVehicleLine(itemCaptured));
                    TInk(assignBtn, Ink); if (!assignBtn.GetComponent<FleetPressFeedback>()) assignBtn.gameObject.AddComponent<FleetPressFeedback>();
                    var retireBtn = Btn(sub.transform, "Retire", "功勋退役 +¥" + refundCoins, 18, subWidth - 274f, 18, 124f, 72f, Color.Lerp(CardBg, Gold, 0.24f), () => PromptRetireVehicle(itemCaptured));
                    TInk(retireBtn, new Color(0.50f, 0.32f, 0.10f)); if (!retireBtn.GetComponent<FleetPressFeedback>()) retireBtn.gameObject.AddComponent<FleetPressFeedback>();
                }
                else
                {
                    var changeBtn = Btn(sub.transform, "Action", "更换航线", 22, subWidth - 166f, 18, 150f, 72f, new Color(0.92f, 0.89f, 0.83f), () => ChooseResearchVehicleLine(itemCaptured));
                    TInk(changeBtn, Ink); if (!changeBtn.GetComponent<FleetPressFeedback>()) changeBtn.gameObject.AddComponent<FleetPressFeedback>();
                }
                return sub.GetComponent<RectTransform>();
            }
            void ShiftGarageRectY(RectTransform rt, float dy)
            {
                if (rt == null || Mathf.Approximately(dy, 0f)) return;
                // 若入场动画尚未结束，先立即收尾并销毁组件，防止 Update 将位置拉回旧坐标。
                var entrance = rt.GetComponent<FleetCardEntrance>();
                if (entrance != null)
                {
                    var cg = rt.GetComponent<CanvasGroup>();
                    if (cg != null) cg.alpha = 1f;
                    Destroy(entrance);
                }
                var p = rt.anchoredPosition;
                p.y += dy;
                rt.anchoredPosition = p;
            }
            var groupEntries = new List<GarageModelGroup>(modelOrder.Count);
            void ToggleGarageGroupInPlace(int groupIdx)
            {
                if (groupIdx < 0 || groupIdx >= groupEntries.Count) return;
                var entry = groupEntries[groupIdx];
                bool nowExpanded = !entry.expanded;
                entry.expanded = nowExpanded;
                if (nowExpanded) garageExpandedModels.Add(entry.model);
                else garageExpandedModels.Remove(entry.model);

                float deltaGroup = nowExpanded ? entry.subBlockHeight : -entry.subBlockHeight;
                requiredHeight += deltaGroup;
                float oldH = garageContent.sizeDelta.y;
                float newH = Mathf.Max(garageViewHeight, requiredHeight);
                float deltaH = newH - oldH;
                if (!Mathf.Approximately(deltaH, 0f))
                    garageContent.sizeDelta = new Vector2(garageContent.sizeDelta.x, newH);

                for (int j = 0; j < groupEntries.Count; j++)
                {
                    float dy = j <= groupIdx ? deltaH : (deltaH - deltaGroup);
                    if (Mathf.Approximately(dy, 0f)) continue;
                    ShiftGarageRectY(groupEntries[j].cardRect, dy);
                    for (int s = 0; s < groupEntries[j].subRects.Count; s++)
                        ShiftGarageRectY(groupEntries[j].subRects[s], dy);
                }

                if (nowExpanded)
                {
                    float subTop = entry.cardRect.anchoredPosition.y - 8f;
                    float subWidth = gCardWidth - 24f;
                    for (int k = 0; k < entry.items.Count; k++)
                    {
                        entry.subRects.Add(CreateGarageSubCard(entry.model, k, entry.items[k], subTop, subWidth));
                        subTop -= 120f;
                    }
                }
                else
                {
                    for (int k = 0; k < entry.subRects.Count; k++)
                        if (entry.subRects[k] != null) Destroy(entry.subRects[k].gameObject);
                    entry.subRects.Clear();
                }

                if (entry.toggleLabel != null)
                    entry.toggleLabel.text = nowExpanded ? "▴ 收起" : "▾ 展开";

                float maxScroll = Mathf.Max(0f, newH - garageViewHeight);
                var cPos = garageContent.anchoredPosition;
                if (cPos.y > maxScroll)
                    garageContent.anchoredPosition = new Vector2(cPos.x, maxScroll);
            }

            float top = gHeight - 10f;
            for (int gi = 0; gi < modelOrder.Count; gi++)
            {
                int model = modelOrder[gi];
                var gItems = grouped[model];
                bool expanded = garageExpandedModels.Contains(model);
                int assignedCount = 0;
                foreach (var it in gItems)
                {
                    var line = lines.Find(l => l.id == it.lineId);
                    if (line == null) it.lineId = 0; else assignedCount++;
                }
                int idleCount = gItems.Count - assignedCount;

                var card = Rect("GarageModel" + model, garageContent, 0, top - 198f, gCardWidth, 198f);
                Rounded(card, CardBg); SoftShadow(card);
                StampFineRuleFrame(card.transform, gCardWidth, 198f, 6f, new Color(InkSoft.r, InkSoft.g, InkSoft.b, 0.14f));
                var cardButton = card.AddComponent<Button>(); cardButton.targetGraphic = card.GetComponent<Image>();
                cardButton.onClick.AddListener(() => OpenVehicleDetails(model));
                var art = Rect("TrainArt", card.transform, 16, 35, gArtWidth, 116).AddComponent<RawImage>();
                art.texture = Resources.Load<Texture2D>("Research/Train/" + LocoArt[model]); art.raycastTarget = false;
                if (art.texture)
                {
                    float ratio = (float)art.texture.width / art.texture.height;
                    float aw = Mathf.Min(gArtWidth, 116 * ratio), ah = aw / ratio;
                    art.rectTransform.sizeDelta = new Vector2(aw, ah);
                    art.rectTransform.anchoredPosition = new Vector2(16 + (gArtWidth - aw) / 2, 35 + (116 - ah) / 2);
                    art.color = Color.white;
                }
                else
                {
                    // 已删除 Research/Train/ 资源：隐藏空 RawImage，回退为纯色星舰徽记，避免空贴图与空引用。
                    art.gameObject.SetActive(false);
                    var fallback = Rect("TrainArtFallback", card.transform, 16, 35, gArtWidth, 116);
                    var fb = Img(fallback, new Color(0.36f, 0.46f, 0.66f, 0.55f), spriteCircle);
                    fb.raycastTarget = false;
                    Txt(fallback.transform, LocoNames[model], 20, Color.white, 0f, 0f, gArtWidth, 116, TextAnchor.MiddleCenter).raycastTarget = false;
                }
                var name = Txt(card.transform, LocoNames[model], 31, Ink, gTextX, 138, gInfoWidth, 44, TextAnchor.MiddleLeft);
                FitSingleLine(name, 31);
                var stats = Txt(card.transform, LocoYears[model] + " · " + FormatLocoRealStats(model), 22, InkSoft, gTextX, 94, gInfoWidth, 36, TextAnchor.MiddleLeft);
                FitSingleLine(stats, 22);
                var summaryLbl = Txt(card.transform, $"共 {gItems.Count} 艘 · 已配置 {assignedCount} 艘 · 待命 {idleCount} 艘", 22, InkSoft, gTextX, 48, gInfoWidth, 32, TextAnchor.MiddleLeft);
                FitSingleLine(summaryLbl, 22);
                int capturedGroupIdx = gi;
                var toggle = Btn(card.transform, "Expand", expanded ? "▴ 收起" : "▾ 展开", 23,
                    gActionX, 54, gActionWidth, 90, new Color(0.92f, 0.89f, 0.83f), () => ToggleGarageGroupInPlace(capturedGroupIdx));
                TInk(toggle, Ink);
                if (!toggle.GetComponent<FleetPressFeedback>()) toggle.gameObject.AddComponent<FleetPressFeedback>();
                { var entry = card.AddComponent<FleetCardEntrance>(); entry.delay = Mathf.Min(gi, 6) * 0.032f; }

                var groupEntry = new GarageModelGroup
                {
                    model = model,
                    items = gItems,
                    cardRect = card.GetComponent<RectTransform>(),
                    toggleLabel = toggle.GetComponentInChildren<Text>(),
                    expanded = expanded,
                    subBlockHeight = gItems.Count * 120f + 8f
                };
                groupEntries.Add(groupEntry);

                float nextTop = top - 198f;
                if (expanded)
                {
                    float subTop = nextTop - 8f;
                    float subWidth = gCardWidth - 24f;
                    for (int k = 0; k < gItems.Count; k++)
                    {
                        groupEntry.subRects.Add(CreateGarageSubCard(model, k, gItems[k], subTop, subWidth));
                        subTop -= 120f;
                    }
                    nextTop = subTop;
                }
                top = nextTop - 20f;
            }
            return;
        }

        var content = FleetScroll(page, 172, PgH - 354, Mathf.Max(1, count) * 218f + 20, inset, scrollKey: FleetCollectionScrollKey(tab));
        float height = content.sizeDelta.y;
        float cardWidth = content.sizeDelta.x, artWidth = tab == 2 ? 210f : 270f;
        float textX = tab == 2 ? 240f : 304f, actionWidth = tab == 2 ? 204f : 234f;
        float actionX = cardWidth - actionWidth - 28f, infoWidth = actionX - textX - 12f;
        if (count == 0)
        {
            Txt(content, tab == 1 ? "暂无符合条件的星舰\n解锁科技后可获得星舰" : "暂无符合条件的舰型", 29, InkSoft, 40, height - 240, cardWidth - 80, 150, TextAnchor.MiddleCenter);
            return;
        }
        for (int i = 0; i < count; i++)
        {
            var item = tab == 1 ? visibleItems[i] : null;
            int index = tab == 1 ? item.node : visibleModels[i];
            bool unlocked = techDone.Contains(LocoKey(index));
            var assigned = item == null ? null : lines.Find(l => l.id == item.lineId);
            if (item != null && assigned == null) item.lineId = 0;
            var card = Rect("Vehicle" + i, content, 0, height - 208 - i * 218, cardWidth, 198);
            Rounded(card, CardBg); SoftShadow(card);
            StampFineRuleFrame(card.transform, cardWidth, 198f, 6f, new Color(InkSoft.r, InkSoft.g, InkSoft.b, 0.14f));
            var cardButton = card.AddComponent<Button>(); cardButton.targetGraphic = card.GetComponent<Image>();
            cardButton.onClick.AddListener(() => OpenVehicleDetails(index));
            var art = Rect("TrainArt", card.transform, 16, 35, artWidth, 116).AddComponent<RawImage>();
            art.texture = Resources.Load<Texture2D>("Research/Train/" + LocoArt[index]); art.raycastTarget = false;
            if (art.texture)
            {
                float ratio = (float)art.texture.width / art.texture.height;
                float aw = Mathf.Min(artWidth, 116 * ratio), ah = aw / ratio;
                art.rectTransform.sizeDelta = new Vector2(aw, ah);
                art.rectTransform.anchoredPosition = new Vector2(16 + (artWidth - aw) / 2, 35 + (116 - ah) / 2);
                art.color = tab == 2 && !unlocked ? new Color(0.65f, 0.65f, 0.65f, 0.75f) : Color.white;
            }
            else
            {
                // 已删除 Research/Train/ 资源：隐藏空 RawImage，回退为纯色星舰徽记，避免空贴图与空引用。
                art.gameObject.SetActive(false);
                var fallback = Rect("TrainArtFallback", card.transform, 16, 35, artWidth, 116);
                var fb = Img(fallback, new Color(0.36f, 0.46f, 0.66f, 0.55f), spriteCircle);
                fb.raycastTarget = false;
                Txt(fallback.transform, LocoNames[index], 20, Color.white, 0f, 0f, artWidth, 116, TextAnchor.MiddleCenter).raycastTarget = false;
            }
            var name = Txt(card.transform, LocoNames[index], 31, Ink, textX, 138, infoWidth, 44, TextAnchor.MiddleLeft); name.resizeTextForBestFit = true; name.resizeTextMinSize = 20; name.resizeTextMaxSize = ScaledFontSize(31); name.horizontalOverflow = HorizontalWrapMode.Overflow; name.verticalOverflow = VerticalWrapMode.Truncate;
            var stats = Txt(card.transform, LocoYears[index] + " · " + FormatLocoRealStats(index), 22, InkSoft, textX, 94, infoWidth, 36, TextAnchor.MiddleLeft);
            stats.resizeTextForBestFit = true; stats.resizeTextMinSize = 16; stats.resizeTextMaxSize = ScaledFontSize(22); stats.horizontalOverflow = HorizontalWrapMode.Overflow; stats.verticalOverflow = VerticalWrapMode.Truncate;
            int locoEra = LocoEra(index);
            bool eraClosed = tab == 2 && !unlocked && !IsTechEraUnlocked(locoEra);
            string state = tab == 1 ? (assigned == null ? "未配置" : "已配置至 " + assigned.name)
                : unlocked ? "已解锁"
                : eraClosed ? "III代暂未开放"
                : $"{(locoEra == 0 ? "I" : locoEra == 1 ? "II" : "III")}代科技树未解锁";
            Txt(card.transform, state, 22, eraClosed ? new Color(0.56f, 0.45f, 0.34f) : InkSoft, textX, 48, infoWidth, 32, TextAnchor.MiddleLeft);
            bool canRetire = tab == 1 && assigned == null;
            if (canRetire)
            {
                var itemCaptured = item;
                int refundCoins = Mathf.Max(80, Mathf.RoundToInt(LocoPrice(index) * 0.55f));
                var assignBtn = Btn(card.transform, "Action", "配置航线", 23, actionX, 104, actionWidth, 64, new Color(0.92f, 0.89f, 0.83f), () => ChooseResearchVehicleLine(itemCaptured));
                TInk(assignBtn, Ink); if (!assignBtn.GetComponent<FleetPressFeedback>()) assignBtn.gameObject.AddComponent<FleetPressFeedback>();
                var retireBtn = Btn(card.transform, "Retire", "功勋退役 +¥" + refundCoins, 20, actionX, 26, actionWidth, 64, Color.Lerp(CardBg, Gold, 0.24f), () => PromptRetireVehicle(itemCaptured));
                TInk(retireBtn, new Color(0.50f, 0.32f, 0.10f)); if (!retireBtn.GetComponent<FleetPressFeedback>()) retireBtn.gameObject.AddComponent<FleetPressFeedback>();
            }
            else
            {
                string shopLbl = unlocked
                    ? (shopLocoBatchCount > 1 ? "购×" + shopLocoBatchCount + " ¥" : "购买 ¥") + FormatCoins(LocoPrice(index) * shopLocoBatchCount)
                    : eraClosed ? "敬请期待" : "未解锁";
                var button = Btn(card.transform, "Action", tab == 1 ? "更换航线" : shopLbl, 22,
                    actionX, 54, actionWidth, 90, new Color(0.92f, 0.89f, 0.83f), () => {
                        if (tab == 1) ChooseResearchVehicleLine(item);
                        else PurchaseLoco(index, shopLocoBatchCount);
                    });
                TInk(button, Ink);
                var btnTxt = button.GetComponentInChildren<Text>();
                if (btnTxt) FitSingleLine(btnTxt, 22);
                if (!button.GetComponent<FleetPressFeedback>()) button.gameObject.AddComponent<FleetPressFeedback>();
            }
            { var entry = card.AddComponent<FleetCardEntrance>(); entry.delay = Mathf.Min(i, 6) * 0.032f; }
        }
    }
    void ChooseResearchVehicleLine(ResearchVehicle item)
    {
        var availableLines = lines.FindAll(line => PathTouchesRegion(line.st, currentRegion));
        int rows = availableLines.Count + (item.lineId != 0 ? 1 : 0);
        float h = 230 + Mathf.Max(1, Mathf.Min(rows, 8)) * 78;
        var bubble = OpenPopup("配置 " + LocoNames[item.node], h, 760);
        var view = Rect("LineViewport", bubble.transform, 30, 70, 700, h - 200);
        Img(view, new Color(1,1,1,0.01f)); var mask = view.AddComponent<Mask>(); mask.showMaskGraphic = false;
        var scroll = view.AddComponent<ScrollRect>(); scroll.horizontal = false; scroll.vertical = true; scroll.scrollSensitivity = 90f;
        scroll.viewport = view.GetComponent<RectTransform>();
        float contentHeight = Mathf.Max(h - 200, rows * 78f);
        var content = Rect("LineOptions", view.transform, 0, 0, 700, contentHeight);
        var contentRt = content.GetComponent<RectTransform>(); contentRt.anchorMin = contentRt.anchorMax = new Vector2(0,1);
        contentRt.pivot = new Vector2(0,1); contentRt.anchoredPosition = Vector2.zero; scroll.content = contentRt;
        BindScrollPosition(scroll, "fleet:choose-line");
        if (item.lineId != 0)
        {
            var unassign = Btn(content.transform, "Unassign", "返回舰坞待命", 25, 0, contentHeight - (availableLines.Count + 1) * 78, 700, 66, Paper2, () => {
                var previous = lines.Find(x => x.id == item.lineId);
                if (previous != null && !PathTouchesRegion(previous.st, currentRegion)) { Toast("请先切换到该航线所在区域调整飞车"); return; }
                BeginUndo("星舰返回舰坞");
                item.lineId = 0; if (previous != null) SyncLineFleet(previous); RecalcIncome(); EndUndo();
                ClosePanel(); OpenResearchInventory(); Toast("星舰已返回舰坞，相关调度已调整");
            }); TInk(unassign, Ink);
        }
        if (availableLines.Count == 0) Txt(content.transform, "当前区域还没有航线", 27, InkSoft, 0, contentHeight - 65, 700, 50, TextAnchor.MiddleCenter);
        for (int i = 0; i < availableLines.Count; i++)
        {
            var target = availableLines[i];
            var button = Btn(content.transform, "Line" + target.id, target.name, 25, 0, contentHeight - (i + 1) * 78, 700, 66, new Color(0.92f, 0.89f, 0.83f), () => {
                if (item.lineId != target.id && target.trains >= 8) { Toast("该航线最多配属 8 艘星舰"); return; }
                var previous = lines.Find(x => x.id == item.lineId);
                if (!PathTouchesRegion(target.st, currentRegion) || (previous != null && !PathTouchesRegion(previous.st, currentRegion))) { Toast("请先切换到相关航线所在区域调整飞车"); return; }
                BeginUndo("配置星舰");
                item.lineId = target.id; if (previous != null) SyncLineFleet(previous); SyncLineFleet(target);
                RecalcIncome(); EndUndo(); ClosePanel(); OpenResearchInventory(); Toast("已配置到 " + target.name);
            });
            TInk(button, Ink);
        }
    }

    static readonly string[] DispatchNames = { "早高峰 06–10", "日间 10–16", "晚高峰 16–20", "夜间 20–06" };
    void SetAllDispatchToTrains(Line line)
    {
        if (line == null || !lines.Contains(line)) return;
        if (!PathTouchesRegion(line.st, currentRegion)) { Toast("请先切换到该航线所在区域调整调度"); return; }
        if (!techDone.Contains("dispatch_basic")) { Toast("请先研究星航星航时刻表"); return; }
        BeginUndo("全时段满班");
        for (int i = 0; i < line.dispatch.Length; i++) line.dispatch[i] = line.trains;
        RecalcIncome(); EndUndo(); RefreshHUD();
        OpenLineOperations(line);
        ShowForecastDelayToast();
    }
    void SetLineDispatch(Line line, int slot, int amount, Action onDone = null)
    {
        if (!PathTouchesRegion(line.st, currentRegion)) { Toast("请先切换到该航线所在区域调整调度"); return; }
        if (!techDone.Contains("dispatch_basic")) { Toast("请先研究星航星航时刻表"); return; }
        amount = Mathf.Clamp(amount, 0, 8);
        if (amount > line.trains)
        {
            if (onDone != null) Toast("当前仅配属 " + line.trains + " 艘，请先在上方「星舰配属」调入星舰");
            else OpenLineFleet(line, () => OpenLineOperations(line), slot, amount);
            return;
        }
        BeginUndo("调整调度");
        if (!techDone.Contains("dispatch_peak")) for (int i=0;i<4;i++) line.dispatch[i]=amount;
        else line.dispatch[slot]=amount;
        RecalcIncome(); EndUndo(); RefreshHUD();
        if (onDone != null) onDone(); else OpenLineOperations(line);
        ShowForecastDelayToast();
    }
    void ChangeLineFare(Line line, float delta, Action onDone = null)
    {
        if (!PathTouchesRegion(line.st, currentRegion)) { Toast("请先切换到该航线所在区域调整票价"); return; }
        if (!techDone.Contains("fare_policy")) { Toast("请先研究旅客星运票价制度"); return; }
        BeginUndo("调整票价"); line.fare = Mathf.Clamp(Mathf.Round((line.fare + delta)*10f)/10f,0.5f,3f);
        RecalcIncome(); EndUndo(); RefreshHUD();
        if (onDone != null) onDone(); else OpenLineOperations(line);
        ShowForecastDelayToast();
    }
    void OpenLineOperations(Line line)
    {
        if (line == null || !lines.Contains(line)) { OpenLinesScreen(); return; }
        var page = OpenPage("票价与调度", "定价权衡客座率与营收；分时调配星舰削减闲置损耗。", "PageArt/line_watercolor", "line_ops_" + line.id);
        pageBack = () => OpenLineScreen(line.id);
        pageForecastRefresh = () => { if (pageRoot != null && activePageKey == "line_ops_" + line.id) OpenLineOperations(line); };
        var forecast = ForecastFor(line);
        string pendingTag = pendingForecastMask != 0 ? "（测算中…）" : "";
        Txt(page.transform, line.name + " · 配属 " + line.trains + " 列 · 当前开航 " + RunningTrains(line) + " 列", 28, Ink, 32, PgH-300, 1000, 45, TextAnchor.MiddleLeft);
        Txt(page.transform, "预计日收入 ¥" + Mathf.RoundToInt(forecast.gross) + "    成本 ¥" + Mathf.RoundToInt(forecast.vehicle+forecast.track+forecast.tax) + "    利润 ¥" + Mathf.RoundToInt(forecast.Profit) + pendingTag, 24, InkSoft, 32, PgH-360, 1000, 45, TextAnchor.MiddleLeft);
        var fare = Rect("Fare",page.transform,28,PgH-535,W-56,145); Rounded(fare,new Color(1,1,1,0.94f));
        Txt(fare.transform, "起步票价  ¥" + line.fare.ToString("0.0") + " / 人", 30, Ink, 26, 74, 570, 44, TextAnchor.MiddleLeft);
        Txt(fare.transform, techDone.Contains("fare_policy") ? "起步价叠加跨站里程费 · 星票款按 10:1 计入金币" : "研究「旅客星运票价制度」后可自主调价", 22, InkSoft, 26, 22, 660, 38, TextAnchor.MiddleLeft);
        var fm=Btn(fare.transform,"FareMinus","−",35,720,30,110,80,Paper2,()=>ChangeLineFare(line,-0.1f));TInk(fm,Ink);
        var fp=Btn(fare.transform,"FarePlus","+",35,850,30,110,80,Paper2,()=>ChangeLineFare(line,0.1f));TInk(fp,Ink);
        bool basic=techDone.Contains("dispatch_basic"), peak=techDone.Contains("dispatch_peak");
        Txt(page.transform, !basic ? "研究「星航星航时刻表」解锁统一调度" : !peak ? "统一调度已就绪；研究「分时航班计划」可分时段增减" : "分时调度 · 高峰与夜间独立起航", 24, InkSoft, 32, PgH-590, 720, 38, TextAnchor.MiddleLeft);
        if (basic)
        {
            var fullDispatchBtn = Btn(page.transform, "FullDispatch", "全时段满班", 22, W - 248, PgH - 596, 220, 48, Indigo, () => SetAllDispatchToTrains(line));
            TInk(fullDispatchBtn, Color.white);
        }
        int rows=peak?4:1;
        for(int i=0;i<rows;i++) {
            int slot=i; var row=Rect("DispatchSlot"+i,page.transform,28,PgH-740-i*130,W-56,115); Rounded(row,new Color(1,1,1,0.94f));
            Txt(row.transform,peak?DispatchNames[i]:"全天统一",26,Ink,24,68,400,36,TextAnchor.MiddleLeft);
            Txt(row.transform,line.dispatch[i]+" 列",30,Indigo,510,44,130,40,TextAnchor.MiddleCenter);
            int sampleSlot = peak ? i : DispatchSlot();
            float oldClock = dayClock;
            float[] sampleHours = {8f,13f,18f,1f};
            try {
                dayClock = sampleHours[sampleSlot] * GameDaySeconds / 24f;
                var sample = trafficSlots[sampleSlot];
                float requested = sample != null && sample.linePeakDemand.TryGetValue(line, out var d) ? d : 0f;
                float gross = sample != null ? TrafficRevenue(line,sample) : 0f;
                Txt(row.transform, "最高区段需求 " + Mathf.RoundToInt(requested) + " · 运力 " + Mathf.RoundToInt(LineCapacity(line)) + " · 收入 ¥" + Mathf.RoundToInt(gross) + " · 运维 ¥" + Mathf.RoundToInt(VehicleMaintenance(line)+TrackMaintenance(line)), 22, InkSoft, 24, 10, 680, 38, TextAnchor.MiddleLeft);
            } finally { dayClock = oldClock; }
            var minus=Btn(row.transform,"Minus","−",34,720,18,110,80,Paper2,()=>SetLineDispatch(line,slot,line.dispatch[slot]-1)); TInk(minus,Ink);
            var plus=Btn(row.transform,"Plus","+",34,850,18,110,80,Paper2,()=>SetLineDispatch(line,slot,line.dispatch[slot]+1)); TInk(plus,Ink);
        }
        Txt(page.transform, "高阶星舰优先上线；各时段数据按全日口径折算，税费于每日结算扣除。", 22, InkSoft, 32, Mathf.Max(70f, PgH-1320), 1000, 100, TextAnchor.UpperLeft);
    }
    void ConfigureLineFleetContext(Line line, Action returnAction, int slot, int amount)
    {
        if (returnAction != null || lineFleetLineId != line.id || lineFleetBackAction == null)
        {
            lineFleetOriginFleetBack = lineFleetShopReturn != null && fleetBack == lineFleetShopReturn ? lineFleetOriginFleetBack : fleetBack;
            lineFleetBackAction = returnAction ?? (() => OpenLineScreen(line.id));
            lineFleetLineId = line.id; lineFleetSlot = -1; lineFleetAmount = 0;
        }
        if (slot >= 0 && slot < 4 && amount > 0)
        {
            lineFleetSlot = slot; lineFleetAmount = Mathf.Clamp(amount, 1, 8);
        }
        // Returning from the shop restores the caller's navigation context; its
        // temporary shop callback must not become the garage's own back action.
        fleetBack = lineFleetOriginFleetBack;
    }
    string LineFleetMoveError(Line line, ResearchVehicle vehicle, bool assign)
    {
        if (line == null || !lines.Contains(line) || vehicle == null || !researchVehicles.Contains(vehicle)) return "星舰或航线已变化，请重新进入";
        if (!PathTouchesRegion(line.st, currentRegion)) return "请先切换到该航线所在区域调整飞车";
        if (assign)
        {
            if (vehicle.lineId != 0) return "该星舰已配属其他航线";
            if (line.trains >= 8) return line.hasBaseTrain ? "该航线已满 8 艘，可先撤下基础星舰再调入" : "该航线最多配属 8 艘星舰";
        }
        else if (vehicle.lineId != line.id) return "该星舰已不在本航线";
        return null;
    }
    void ApplyPendingLineFleetDispatch(Line line)
    {
        if (line.id != lineFleetLineId || lineFleetSlot < 0 || !techDone.Contains("dispatch_basic")) return;
        int amount = Mathf.Clamp(lineFleetAmount, 0, line.trains);
        if (!techDone.Contains("dispatch_peak")) for (int i = 0; i < 4; i++) line.dispatch[i] = amount;
        else line.dispatch[lineFleetSlot] = amount;
        if (line.trains >= lineFleetAmount) { lineFleetSlot = -1; lineFleetAmount = 0; }
    }
    void ReturnFromLineFleet()
    {
        lineFleetSlot = -1; lineFleetAmount = 0;
        lineFleetBackAction?.Invoke();
    }
    bool MoveLineFleetVehicle(Line line, ResearchVehicle vehicle, bool assign)
    {
        if (LineFleetMoveError(line, vehicle, assign) != null) return false;
        vehicle.lineId = assign ? line.id : 0;
        SyncLineFleet(line);
        if (assign) ApplyPendingLineFleetDispatch(line);
        operatingRates.Clear();
        return true;
    }
    void ChangeLineFleetVehicle(Line line, ResearchVehicle vehicle, bool assign, Action onDone = null)
    {
        string error = LineFleetMoveError(line, vehicle, assign);
        if (error != null) { Toast(error); return; }
        BeginUndo(assign ? "调入星舰" : "撤回星舰");
        MoveLineFleetVehicle(line, vehicle, assign);
        RecalcIncome(); EndUndo(); SaveGame(); RefreshHUD();
        if (onDone != null) onDone(); else OpenLineFleet(line);
        ShowForecastDelayToast();
    }
    void FillLineFleetVehicles(Line line, int model, Action onDone = null)
    {
        if (line == null || !lines.Contains(line)) return;
        if (!PathTouchesRegion(line.st, currentRegion)) { Toast("请先切换到该航线所在区域调整飞车"); return; }
        int slotsLeft = 8 - line.trains;
        if (slotsLeft <= 0) { Toast(line.hasBaseTrain ? "该航线已满 8 艘，可先撤下基础星舰再调入" : "该航线最多配属 8 艘星舰"); return; }

        BeginUndo("批量调入星舰");
        int moved = 0;
        for (int i = 0; i < researchVehicles.Count && moved < slotsLeft; i++)
        {
            var v = researchVehicles[i];
            if (v.lineId == 0 && v.node == model)
            {
                v.lineId = line.id;
                moved++;
            }
        }
        if (moved <= 0)
        {
            pendingUndo = null;
            Toast("暂无可用同型星舰");
            return;
        }
        SyncLineFleet(line);
        RecalcIncome(); EndUndo(); SaveGame(); RefreshHUD();
        if (onDone != null) onDone(); else OpenLineFleet(line);
        ShowForecastDelayToast();
    }
    void WithdrawBaseTrain(Line line, Action onDone = null)
    {
        if (line == null || !lines.Contains(line)) return;
        if (!PathTouchesRegion(line.st, currentRegion)) { Toast("请先切换到该航线所在区域调整飞车"); return; }
        if (!line.hasBaseTrain || line.trains <= 1) { Toast("航线不止 1 艘星舰时才能撤下基础星舰"); return; }
        BeginUndo("撤回基础星舰");
        line.hasBaseTrain = false;
        SyncLineFleet(line);
        RecalcIncome(); EndUndo(); RefreshHUD();
        if (onDone != null) onDone(); else OpenLineFleet(line);
        ShowForecastDelayToast();
    }
    sealed class FleetModelGroup
    {
        public int node, count;
        public ResearchVehicle first;
    }
    List<FleetModelGroup> FleetGroupsByLine(int targetLineId)
    {
        var groups = new SortedDictionary<int, FleetModelGroup>();
        foreach (var vehicle in researchVehicles)
        {
            if (vehicle.lineId != targetLineId || vehicle.node < 0 || vehicle.node >= LocoNames.Length) continue;
            if (!groups.TryGetValue(vehicle.node, out var group))
                groups[vehicle.node] = group = new FleetModelGroup { node = vehicle.node, first = vehicle };
            group.count++;
        }
        return new List<FleetModelGroup>(groups.Values);
    }
    static int LineFleetEra(int node) => LocoEra(node);
    static float LineFleetListHeight(bool hasBaseTrain, int assigned, int models, int eras)
        => 24f + 60f + (hasBaseTrain ? 150f : 0f) + assigned * 166f + 72f + (models == 0 ? 130f : eras * 54f + models * 166f) + 24f;
    static int LocoRealCapacity(int node) => Mathf.RoundToInt(4000f * FleetCapacityTraits[Mathf.Clamp(node, 0, FleetCapacityTraits.Length - 1)]);
    static int LocoRealDailyMaint(int node) => Mathf.RoundToInt(55f * FleetMaintenanceTraits[Mathf.Clamp(node, 0, FleetMaintenanceTraits.Length - 1)]);
    static int LocoRealSpeedKmh(int node) => Mathf.RoundToInt(3200f * FleetSpeedTraits[Mathf.Clamp(node, 0, FleetSpeedTraits.Length - 1)]);
    static string FormatLocoRealStats(int node)
        => "运力 " + LocoRealCapacity(node) + "/日 · 日耗 ¥" + LocoRealDailyMaint(node) + " · " + LocoRealSpeedKmh(node) + " km/h";
    string LineFleetVehicleStats(int node) => FormatLocoRealStats(node);
    void OpenLineFleet(Line line, Action returnAction = null, int slot = -1, int amount = 0)
    {
        if (line == null || !lines.Contains(line)) { OpenLinesScreen(); return; }
        SyncLineFleet(line);
        ConfigureLineFleetContext(line, returnAction, slot, amount);
        var page = OpenPage("调配飞车", "本线星舰与舰坞闲舰统一调配；新舰请前往星舰商店购买。", "FleetArt/garage_watercolor", "line_fleet");
        pageBack = ReturnFromLineFleet;
        Txt(page.transform, line.name + " · 已配属 " + line.trains + " / 8 列", 32, Ink, 32, PgH - 258, 1000, 46, TextAnchor.MiddleLeft);
        string summary = "单向区段运力 " + Mathf.RoundToInt(FleetCapacity(line)) + " 人/日 · "
            + Mathf.RoundToInt(3200f * FleetTrait(line, 2)) + " km/h · 日均能耗 ¥" + Mathf.RoundToInt(VehicleMaintenance(line));
        if (lineFleetSlot >= 0) summary = "补充至 " + lineFleetAmount + " 列后，自动回写" + (techDone.Contains("dispatch_peak") ? DispatchNames[lineFleetSlot] : "全天统一调度");
        var summaryText = Txt(page.transform, summary, 23, InkSoft, 32, PgH - 312, 1000, 40, TextAnchor.MiddleLeft); FitSingleLine(summaryText, 23);
        var assignedGroups = FleetGroupsByLine(line.id);
        var groups = FleetGroupsByLine(0); var eras = new HashSet<int>();
        foreach (var group in groups) eras.Add(LineFleetEra(group.node));
        var content = FleetScroll(page.transform, 168, PgH - 340, LineFleetListHeight(line.hasBaseTrain, assignedGroups.Count, groups.Count, eras.Count), scrollKey: "line:fleet:" + line.id);
        float width = content.sizeDelta.x, y = content.sizeDelta.y - 24f;
        Txt(content, "分组一 · 本线已配属星舰", 28, Ink, 16, y - 46, width - 32, 44, TextAnchor.MiddleLeft); y -= 60;
        if (line.hasBaseTrain)
        {
            var basic = Rect("BaseVehicle", content, 0, y - 140, width, 140); Rounded(basic, CardBg);
            bool canWithdrawBase = line.trains > 1;
            float textW = canWithdrawBase ? (width - 270f) : (width - 48f);
            Txt(basic.transform, "航线基础星舰 ×1", 29, Ink, 24, 82, textW, 42, TextAnchor.MiddleLeft);
            Txt(basic.transform, canWithdrawBase ? "本线不止 1 艘星舰，可撤下基础星舰腾出编队。" : "当前仅此 1 艘，调入其他星舰后可撤下。", 23, InkSoft, 24, 24, textW, 40, TextAnchor.MiddleLeft);
            if (canWithdrawBase)
            {
                var withdrawBase = Btn(basic.transform, "WithdrawBaseVehicle", "撤回", 25, width - 218, 32, 194, 76, Paper2, () => WithdrawBaseTrain(line));
                TInk(withdrawBase, Ink);
            }
            y -= 150;
        }
        foreach (var group in assignedGroups)
        {
            var item = group.first; int model = group.node;
            var card = Rect("AssignedModel" + model, content, 0, y - 156, width, 156); Rounded(card, CardBg);
            var title = Txt(card.transform, LocoNames[model] + " ×" + group.count, 30, Ink, 24, 94, width - 270, 44, TextAnchor.MiddleLeft); FitSingleLine(title, 30);
            var stats = Txt(card.transform, LineFleetVehicleStats(model), 23, InkSoft, 24, 30, width - 270, 44, TextAnchor.MiddleLeft); FitSingleLine(stats, 23);
            var withdraw = Btn(card.transform, "WithdrawVehicle", "撤回", 25, width - 218, 39, 194, 76, Paper2, () => ChangeLineFleetVehicle(line, item, false)); TInk(withdraw, Ink);
            y -= 166;
        }
        Txt(content, "分组二 · 舰坞空闲星舰", 28, Ink, 16, y - 58, width - 32, 46, TextAnchor.MiddleLeft); y -= 72;
        if (groups.Count == 0) Txt(content, "暂无空闲星舰\n可撤回其他航线星舰，或点击下方「购买星舰」。", 25, InkSoft, 24, y - 120, width - 48, 100, TextAnchor.MiddleCenter);
        int previousEra = -1;
        string[] eraNames = { "I代 · 核热推进", "II代 · 聚变磁轨", "III代 · 曲速流线" };
        foreach (var group in groups)
        {
            int era = LineFleetEra(group.node);
            if (era != previousEra) { Txt(content, eraNames[era], 24, Indigo, 20, y - 44, width - 40, 38, TextAnchor.MiddleLeft); y -= 54; previousEra = era; }
            var item = group.first;
            var card = Rect("IdleModel" + group.node, content, 0, y - 156, width, 156); Rounded(card, CardBg);
            bool canBatchFill = group.count > 1 && (8 - line.trains) > 1;
            float idleTextW = canBatchFill ? (width - 390f) : (width - 270f);
            var title = Txt(card.transform, LocoNames[group.node] + " · 可用 " + group.count + " 艘", 29, Ink, 24, 94, idleTextW, 44, TextAnchor.MiddleLeft); FitSingleLine(title, 29);
            var stats = Txt(card.transform, LineFleetVehicleStats(group.node), 23, InkSoft, 24, 30, idleTextW, 44, TextAnchor.MiddleLeft); FitSingleLine(stats, 23);
            if (canBatchFill)
            {
                int modelNode = group.node;
                var fillBtn = Btn(card.transform, "FillVehicle", "填满", 24, width - 356, 39, 134, 76, Matcha, () => FillLineFleetVehicles(line, modelNode)); TInk(fillBtn, Color.white);
                var assignBtn = Btn(card.transform, "AssignVehicle", "调入", 24, width - 210, 39, 186, 76, Indigo, () => ChangeLineFleetVehicle(line, item, true)); TInk(assignBtn, Color.white);
            }
            else
            {
                var assignBtn = Btn(card.transform, "AssignVehicle", "调入", 25, width - 218, 39, 194, 76, Indigo, () => ChangeLineFleetVehicle(line, item, true)); TInk(assignBtn, Color.white);
            }
            y -= 166;
        }
        var footer = Rect("LineFleetActions", page.transform, 0, 0, W, 148); Rounded(footer, CardBg);
        var buy = Btn(footer.transform, "BuyLocomotive", "购买星舰 ▸", 30, 28, 28, W - 56, 88, Indigo, () => {
            shopCategory = 0; lineFleetShopReturn = () => OpenLineFleet(line); fleetBack = lineFleetShopReturn; OpenFleetTab(2);
        }); TInk(buy, Color.white);
    }

    readonly HashSet<int> ownedLineSkins = new HashSet<int>();
    bool loanOutstanding;
    static readonly string[] LineSkinNames = { "海风蓝", "樱花粉", "金色晨光", "松林绿", "朱砂红", "暮云紫", "湖水青", "焦糖橙", "靛青", "草木绿", "青瓷绿", "珊瑚红", "雾霭灰", "墨棕", "浅紫苑" };
    static readonly Color[] LineSkinColors = {
        new Color(0.31f,0.58f,0.68f), new Color(0.84f,0.52f,0.58f), new Color(0.82f,0.63f,0.29f),
        new Color(0.32f,0.52f,0.40f), new Color(0.75f,0.29f,0.25f), new Color(0.53f,0.39f,0.63f),
        new Color(0.29f,0.64f,0.60f), new Color(0.80f,0.48f,0.26f), new Color(0.24f,0.38f,0.60f),
        new Color(0.53f,0.63f,0.32f), new Color(0.42f,0.65f,0.51f), new Color(0.85f,0.40f,0.36f),
        new Color(0.48f,0.54f,0.56f), new Color(0.48f,0.37f,0.30f), new Color(0.64f,0.50f,0.70f)
    };
    static readonly int[] LineSkinPrices = {
        28000, 36000, 48000, 58000, 68000,
        88000, 118000, 148000, 188000, 228000,
        268000, 288000, 168000, 328000, 480000
    };

    bool IsSkinFreeByScenery(int skin)
    {
        return (skin == 10 || skin == 11) && FoundCount() >= 6;
    }

    void ApplySkinToLine(Line line, int skin, bool payIfNeeded)
    {
        if (line == null || !lines.Contains(line) || skin < 0 || skin >= LineSkinNames.Length) return;
        bool alreadyOwned = ownedLineSkins.Contains(skin);
        bool freeByScenery = IsSkinFreeByScenery(skin);
        if (!alreadyOwned && payIfNeeded && !freeByScenery && coins < LineSkinPrices[skin])
        {
            Toast("金币不足，还差 ¥" + FormatShortfall(LineSkinPrices[skin] - coins) + "（可点击看片免费领）");
            return;
        }
        BeginUndo("航线涂装");
        if (!alreadyOwned && payIfNeeded)
        {
            if (!freeByScenery) coins -= LineSkinPrices[skin];
            ownedLineSkins.Add(skin);
        }
        else ownedLineSkins.Add(skin);
        line.skin = skin;
        line.color = LineSkinColors[skin];
        CreateLineVisuals();
        RecalcIncome();
        EndUndo();
        RefreshHUD();
        Toast("已换装：" + LineSkinNames[skin] + "（" + SkinPerkSummary(skin) + "）");
    }

    void PromptRewardedAd(string adKey, int requiredCount, string itemName, Action onPermanentUnlock, Action onTrialUnlock = null)
    {
        int watched = adWatchCounts.TryGetValue(adKey, out int c) ? c : 0;
        string desc = requiredCount <= 1
            ? "观看 1 段赞助星航宣传短片，即可免费永久解锁\n「" + itemName + "」"
            : "观看赞助宣传片可立即免费试用「" + itemName + "」24小时\n累计观影进度：" + watched + " / " + requiredCount + " 次（满次即永久拥有）";
        var bubble = OpenPopup("星图映像馆 · 赞助放映", 400, 740);
        Txt(bubble.transform, desc, 25, InkSoft, 34, 146, 672, 160, TextAnchor.MiddleLeft);
        var watch = Btn(bubble.transform, "WatchAd", "观看赞助短片（领权益）", 26, 34, 34, 420, 84, Matcha, () => {
            ClosePanel(true);
            RequestTapTapRewardedAd(adKey, itemName, () => {
                int next = (adWatchCounts.TryGetValue(adKey, out int cur) ? cur : 0) + 1;
                adWatchCounts[adKey] = next;
                PlayTone(true);
                SaveGame();
                if (next >= requiredCount) onPermanentUnlock?.Invoke();
                else onTrialUnlock?.Invoke();
            });
        });
        TInk(watch, Color.white);
        var cancel = Btn(bubble.transform, "CancelAd", "暂不观看", 25, 474, 34, 232, 84, Paper2, ClosePanel);
        TInk(cancel, Ink);
    }

    void OpenLineSkins(Line line)
    {
        if (line == null || !lines.Contains(line)) return;
        const float h = 920f, rowHeight = 118f;
        var bubble = OpenPopup("航线涂装", h, 800);
        AttachPopupCurrencyChip(bubble, 800f, h);
        Txt(bubble.transform, "双色专属涂装 · 附带流派加成 · 全网通用", 22, InkSoft, 30, h - 126, 740, 36, TextAnchor.MiddleLeft);
        var view = Rect("SkinViewport", bubble.transform, 28, 36, 744, 725);
        Img(view, new Color(1, 1, 1, 0.01f)); var mask = view.AddComponent<Mask>(); mask.showMaskGraphic = false;
        var scroll = view.AddComponent<ScrollRect>(); scroll.horizontal = false; scroll.vertical = true;
        scroll.scrollSensitivity = 100f; scroll.movementType = ScrollRect.MovementType.Elastic;
        scroll.viewport = view.GetComponent<RectTransform>();
        var content = Rect("SkinOptions", view.transform, 0, 0, 744, LineSkinNames.Length * rowHeight);
        var contentRt = content.GetComponent<RectTransform>(); contentRt.anchorMin = contentRt.anchorMax = new Vector2(0, 1);
        contentRt.pivot = new Vector2(0, 1); contentRt.anchoredPosition = Vector2.zero; scroll.content = contentRt;
        BindScrollPosition(scroll, "line:skins:" + line.id);
        int activeSkin = GetLineSkin(line);
        for (int i = 0; i < LineSkinNames.Length; i++)
        {
            int skin = i;
            bool owned = ownedLineSkins.Contains(skin);
            bool selected = activeSkin == skin;
            bool freeScenery = !owned && IsSkinFreeByScenery(skin);
            var row = Rect("SkinRow" + i, content.transform, 0, LineSkinNames.Length * rowHeight - (i + 1) * rowHeight, 744, 108);
            Rounded(row, CardBg);
            Img(Rect("Swatch", row.transform, 18, 26, 56, 56), LineSkinColors[i], spriteCircle).raycastTarget = false;
            Img(Rect("SwatchTrim", row.transform, 33, 41, 26, 26), LineSkinTrimColors[i], spriteCircle).raycastTarget = false;
            Txt(row.transform, LineSkinNames[i] + " · " + SkinCategoryName(i), 25, Ink, 88, 56, 360, 38, TextAnchor.MiddleLeft);
            Txt(row.transform, SkinPerkSummary(i), 22, InkSoft, 88, 16, 360, 34, TextAnchor.MiddleLeft);

            if (owned)
            {
                var btn = Btn(row.transform, "Skin" + i, selected ? "使用中 ✓" : "换装", 25, 520, 18, 204, 74, selected ? Matcha : Paper2, () => {
                    if (selected) { Toast("当前已使用此涂装"); return; }
                    ApplySkinToLine(line, skin, false);
                    ClosePanel(true); OpenLineScreen(line.id); OpenLineSkins(line);
                });
                TInk(btn, selected ? Color.white : Ink);
            }
            else
            {
                var buyBtn = Btn(row.transform, "SkinBuy" + i, freeScenery ? "名胜免费领" : "¥" + FormatCoins(LineSkinPrices[i]), 22, 444, 18, 148, 74, freeScenery ? Matcha : Paper2, () => {
                    ApplySkinToLine(line, skin, true);
                    if (ownedLineSkins.Contains(skin)) { ClosePanel(true); OpenLineScreen(line.id); OpenLineSkins(line); }
                });
                TInk(buyBtn, freeScenery ? Color.white : Ink);
                var adBtn = Btn(row.transform, "SkinAd" + i, "看片领", 23, 602, 18, 122, 74, Indigo, () => {
                    PromptRewardedAd("skin_" + skin, 1, LineSkinNames[skin], () => {
                        ownedLineSkins.Add(skin);
                        ApplySkinToLine(line, skin, false);
                        OpenLineScreen(line.id); OpenLineSkins(line);
                    });
                });
                TInk(adBtn, Color.white);
            }
        }
    }

    void ChooseLineForSkin(int skin)
    {
        if (lines.Count == 0) { Toast("请先开通一条星际航线"); return; }
        float h = 230 + Mathf.Clamp(lines.Count, 1, 7) * 84f;
        var bubble = OpenPopup("将「" + LineSkinNames[skin] + "」应用于", h, 720);
        var view = Rect("LineViewport", bubble.transform, 28, 36, 664, h - 140);
        Img(view, new Color(1, 1, 1, 0.01f)); var mask = view.AddComponent<Mask>(); mask.showMaskGraphic = false;
        var scroll = view.AddComponent<ScrollRect>(); scroll.horizontal = false; scroll.vertical = true;
        scroll.viewport = view.GetComponent<RectTransform>();
        var content = Rect("Lines", view.transform, 0, 0, 664, lines.Count * 84f);
        var crt = content.GetComponent<RectTransform>(); crt.anchorMin = crt.anchorMax = new Vector2(0, 1);
        crt.pivot = new Vector2(0, 1); crt.anchoredPosition = Vector2.zero; scroll.content = crt;
        BindScrollPosition(scroll, "skin:choose-line:" + skin);
        for (int i = 0; i < lines.Count; i++)
        {
            var target = lines[i];
            int cur = GetLineSkin(target);
            string desc = target.name + (cur == skin ? "（已佩戴）" : cur >= 0 ? "（" + LineSkinNames[cur] + "）" : "");
            var row = Btn(content.transform, "ApplyLine" + i, desc, 24, 0, lines.Count * 84f - (i + 1) * 84f, 664, 72, Paper2, () => {
                ClosePanel(true);
                ApplySkinToLine(target, skin, false);
                OpenFleetTab(2);
            });
            TInk(row, Ink);
        }
    }

    void AddTrainSkinArt(Transform parent, MainTrainSkinSpec spec, float x, float y, float width, float height, bool modelFallback)
    {
        var frame = Rect("SkinArt", parent, x, y, width, height);
        var texture = Resources.Load<Texture2D>(spec.previewArt);
        if (texture == null && modelFallback) texture = Resources.Load<Texture2D>("Research/Train/" + LocoArt[spec.locoNode]);
        if (texture == null) { Img(frame, spec.hull).raycastTarget = false; return; }
        var raw = Rect("Preview", frame.transform, 0, 0, width, height).AddComponent<RawImage>();
        raw.texture = texture; raw.raycastTarget = false;
        var fit = raw.gameObject.AddComponent<AspectRatioFitter>();
        fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fit.aspectRatio = (float)texture.width / texture.height;
    }

    void BuildShopTrainSkins(Transform page)
    {
        Txt(page, "主线车皮  纯外观，不影响属性", 23, InkSoft, 178, PgH - 235, 860, 38, TextAnchor.MiddleLeft);
        var content = FleetScroll(page, 172, PgH - 255, (MainTrainSkinCount - 1) * 174f + 24, 150f, scrollKey: "shop:trainskins");
        float height = content.sizeDelta.y, width = content.sizeDelta.x;
        for (int i = 1; i < MainTrainSkinCount; i++)
        {
            int id = i; var spec = MainTrainSkins[i]; bool owned = ownedTrainSkins.Contains(i);
            var card = Rect("TrainSkin" + i, content, 0, height - 164 - (i - 1) * 174, width, 158);
            Rounded(card, CardBg); SoftShadow(card);
            AddTrainSkinArt(card.transform, spec, 16, 28, 132, 100, false);
            Txt(card.transform, spec.name, 25, Ink, 160, 114, width - 390, 34, TextAnchor.MiddleLeft);
            Txt(card.transform, "适用车型：" + LocoNames[spec.locoNode], 19, Ink, 160, 78, width - 390, 30, TextAnchor.MiddleLeft);
            Txt(card.transform, owned ? "已解锁" : spec.source, 18, InkSoft, 160, 14, width - 390, 60, TextAnchor.MiddleLeft);
            TInk(Btn(card.transform, "Preview", "预览 / 装配", 21, width - 210, 42, 186, 72, Matcha, () => OpenTrainSkinPreviewPopup(id)), Color.white);
        }
    }

    void OpenPureImagePreview(string title, string resourcePath)
    {
        OpenPureImagePreview(title, Resources.Load<Texture2D>(resourcePath));
    }

    void OpenPureImagePreview(string title, Texture2D tex)
    {
        CloseLineBubble();
        ClosePanel(true);
        float pw = 960f, ph = 540f;
        if (tex != null) ph = Mathf.Clamp(Mathf.Round(pw * tex.height / Mathf.Max(1f, (float)tex.width)), 420f, 720f);
        GetSafeMarginsInRoot(out float safeL, out float safeR, out float safeT, out float safeB);
        float safeW = Mathf.Max(320f, RootW - safeL - safeR);
        float safeH = Mathf.Max(420f, RootH - safeT - safeB);
        float scale = Mathf.Min(1f, (safeW - 36f) / pw, (safeH - 36f) / ph);

        scrim = Rect("PreviewScrim", root, 0f, 0f, RootW, RootH);
        var srt = scrim.GetComponent<RectTransform>();
        srt.anchorMin = Vector2.zero; srt.anchorMax = Vector2.one;
        srt.offsetMin = Vector2.zero; srt.offsetMax = Vector2.zero;
        var bg = Img(scrim, new Color(0.12f, 0.10f, 0.09f, 0.30f));
        var dismiss = scrim.AddComponent<Button>();
        dismiss.targetGraphic = bg; dismiss.transition = Selectable.Transition.None;
        dismiss.onClick.AddListener(() => ClosePanel(true));

        activePanel = Rect("PureImagePreview", root, 0f, 0f, pw, ph);
        var rt = activePanel.GetComponent<RectTransform>();
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(safeL + safeW * 0.5f, safeB + safeH * 0.5f);
        rt.localScale = Vector3.one * scale;
        Rounded(activePanel, Color.white); SoftShadow(activePanel);
        var clip = Rect("ImageClip", activePanel.transform, 0f, 0f, pw, ph);
        Rounded(clip, Color.white);
        var mask = clip.AddComponent<Mask>(); mask.showMaskGraphic = false;
        var image = Rect("Art", clip.transform, 0f, 0f, pw, ph);
        var artRt = image.GetComponent<RectTransform>();
        artRt.anchorMin = Vector2.zero; artRt.anchorMax = Vector2.one;
        artRt.offsetMin = Vector2.zero; artRt.offsetMax = Vector2.zero;
        var art = image.AddComponent<RawImage>(); art.texture = tex; art.raycastTarget = false;
        var subtitle = Rect("Subtitle", clip.transform, 0f, 0f, pw, 76f);
        Img(subtitle, new Color(0.06f, 0.05f, 0.04f, 0.64f)).raycastTarget = false;
        Txt(subtitle.transform, title, 30, Color.white, 0f, 0f, pw, 76f, TextAnchor.MiddleCenter).raycastTarget = false;
        TInk(Btn(activePanel.transform, "Close", "×", 28, pw - 60f, ph - 60f, 48f, 48f,
            new Color(0.10f, 0.09f, 0.08f, 0.50f), () => ClosePanel(true)), Color.white);
        activePanelKey = "pure_image_preview";
        panelOpenedFrame = Time.frameCount + 1;
        if (toastGo) toastGo.transform.SetAsLastSibling();
    }

    void OpenTrainSkinPreviewPopup(int skinId, bool purePreview = false)
    {
        if (skinId <= 0 || skinId >= MainTrainSkinCount) return;
        var spec = MainTrainSkins[skinId]; bool owned = ownedTrainSkins.Contains(skinId);
        if (purePreview || IsInEventSandbox() || !owned)
        {
            var tex = Resources.Load<Texture2D>(spec.previewArt);
            if (tex == null) tex = Resources.Load<Texture2D>("Research/Train/" + LocoArt[spec.locoNode]);
            OpenPureImagePreview(spec.name, tex);
            return;
        }
        var bubble = OpenPopup(spec.name, 1120, 820, "train_skin_preview");
        AddTrainSkinArt(bubble.transform, spec, 90, 680, 640, 360, true);
        TInk(Btn(bubble.transform, "ApplyAll", "应用到全部「" + LocoNames[spec.locoNode] + "」", 19, 40, 550, 450, 68, Matcha, () => { ApplyTrainSkinToAllOfModel(skinId); OpenTrainSkinPreviewPopup(skinId); }), Color.white);
        TInk(Btn(bubble.transform, "RestoreAll", "全部恢复原皮", 20, 505, 550, 275, 68, Indigo, () => { RestoreOriginalSkinForAllOfModel(spec.locoNode); OpenTrainSkinPreviewPopup(skinId); }), Color.white);
        var vehicles = researchVehicles.FindAll(v => v.node == spec.locoNode);
        if (vehicles.Count == 0)
        {
            var note = Txt(bubble.transform, "尚未购置「" + LocoNames[spec.locoNode] + "」，可在星舰商店购置后换装", 21, InkSoft, 40, 420, 740, 100, TextAnchor.MiddleLeft);
            note.horizontalOverflow = HorizontalWrapMode.Wrap;
            return;
        }
        var view = Rect("VehicleViewport", bubble.transform, 40, 60, 740, 470);
        Img(view, new Color(1, 1, 1, 0.01f)); var mask = view.AddComponent<Mask>(); mask.showMaskGraphic = false;
        var scroll = view.AddComponent<ScrollRect>(); scroll.horizontal = false; scroll.vertical = true; scroll.scrollSensitivity = 90f; scroll.viewport = view.GetComponent<RectTransform>();
        float h = Mathf.Max(470, vehicles.Count * 90f);
        var list = Rect("Vehicles", view.transform, 0, 0, 740, h);
        var rt = list.GetComponent<RectTransform>(); rt.anchorMin = rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0, 1); rt.anchoredPosition = Vector2.zero; scroll.content = rt;
        for (int i = 0; i < vehicles.Count; i++)
        {
            var vehicle = vehicles[i]; var line = lines.Find(l => l.id == vehicle.lineId);
            var row = Rect("Vehicle" + i, list.transform, 0, h - (i + 1) * 90, 740, 82); Rounded(row, Paper2);
            string status = vehicle.skinId > 0 && vehicle.skinId < MainTrainSkinCount ? MainTrainSkins[vehicle.skinId].name : "原皮";
            Txt(row.transform, "#" + (i + 1) + (line != null ? " 正在运行：" + line.name : " 车库待命") + "\n当前：" + status, 19, Ink, 16, 4, 490, 74, TextAnchor.MiddleLeft);
            bool equipped = vehicle.skinId == skinId;
            TInk(Btn(row.transform, "Apply", equipped ? "恢复原皮" : "穿上此皮", 20, 515, 10, 205, 62, equipped ? Indigo : Matcha, () => { ApplyTrainSkinToSingleVehicle(vehicle, equipped ? 0 : skinId); OpenTrainSkinPreviewPopup(skinId); }), Color.white);
        }
    }

    void BuildShopLiveries(Transform page)
    {
        Txt(page, "航线涂装馆 · 15 款双色涂装（含流派增益 · 可看片免费领）", 23, InkSoft, 178, PgH - 235, 860, 38, TextAnchor.MiddleLeft);
        var content = FleetScroll(page, 172, PgH - 255, LineSkinNames.Length * 158f + 24, 150f, scrollKey: "shop:liveries");
        float height = content.sizeDelta.y;
        float cardWidth = content.sizeDelta.x;
        for (int i = 0; i < LineSkinNames.Length; i++)
        {
            int skin = i;
            bool owned = ownedLineSkins.Contains(skin);
            bool freeScenery = !owned && IsSkinFreeByScenery(skin);
            var card = Rect("LiveryShop" + i, content, 0, height - 148 - i * 158, cardWidth, 142);
            Rounded(card, CardBg); SoftShadow(card);
            Img(Rect("Swatch", card.transform, 20, 41, 60, 60), LineSkinColors[i], spriteCircle).raycastTarget = false;
            Img(Rect("Trim", card.transform, 36, 57, 28, 28), LineSkinTrimColors[i], spriteCircle).raycastTarget = false;
            Txt(card.transform, LineSkinNames[i] + "  ·  " + SkinCategoryName(i), 27, Ink, 98, 76, 430, 40, TextAnchor.MiddleLeft);
            Txt(card.transform, SkinPerkSummary(i) + (freeScenery ? "（已达成名胜打卡免费领）" : ""), 23, InkSoft, 98, 26, 430, 38, TextAnchor.MiddleLeft);
            if (owned)
            {
                var apply = Btn(card.transform, "Apply" + i, "装配到航线", 23, cardWidth - 220, 33, 196, 76, Matcha, () => ChooseLineForSkin(skin));
                TInk(apply, Color.white);
            }
            else
            {
                var buy = Btn(card.transform, "Buy" + i, freeScenery ? "免费领" : "¥" + FormatCoins(LineSkinPrices[i]), 21, cardWidth - 336, 33, 150, 76, freeScenery ? Matcha : Paper2, () => {
                    if (!freeScenery && coins < LineSkinPrices[skin]) { Toast("金币不足，还差 ¥" + FormatShortfall(LineSkinPrices[skin] - coins) + "（可看片解锁）"); return; }
                    BeginUndo("解锁航线涂装");
                    if (!freeScenery) coins -= LineSkinPrices[skin];
                    ownedLineSkins.Add(skin);
                    EndUndo(); RefreshHUD(); OpenFleetTab(2); Toast("已解锁涂装「" + LineSkinNames[skin] + "」");
                });
                TInk(buy, freeScenery ? Color.white : Ink);
                var ad = Btn(card.transform, "Ad" + i, "看片解锁", 22, cardWidth - 174, 33, 150, 76, Indigo, () => {
                    PromptRewardedAd("skin_" + skin, 1, LineSkinNames[skin], () => {
                        ownedLineSkins.Add(skin); SaveGame(); OpenFleetTab(2); Toast("已永久解锁「" + LineSkinNames[skin] + "」");
                    });
                });
                TInk(ad, Color.white);
            }
        }
    }

    void BuildShopThemes(Transform page)
    {
        Txt(page, "全局界面风格馆 · 更换纸面、夜图与 UI 色系；购买前可免费实机预览", 22, InkSoft, 178, PgH - 235, 860, 36, TextAnchor.MiddleLeft);
        var content = FleetScroll(page, 172, PgH - 255, Themes.Length * 256f + 24, 150f, scrollKey: "shop:themes");
        float height = content.sizeDelta.y;
        float cardWidth = content.sizeDelta.x;
        for (int i = 0; i < Themes.Length; i++)
        {
            int t = i;
            var spec = Themes[i];
            bool owned = ownedThemes.Contains(t);
            bool trial = !owned && IsThemeTrialActive(t);
            bool avail = owned || trial;
            bool active = currentTheme == t && avail && previewRestoreTheme < 0;

            var card = Rect("ThemeShop" + i, content, 0, height - 240 - i * 256, cardWidth, 232);
            Rounded(card, CardBg); SoftShadow(card);
            // 主题色条与4色调色板圆点预览
            Img(Rect("Accent", card.transform, 0, 22, 8, 188), spec.indigo, spriteRound).raycastTarget = false;
            Color[] chips = { spec.paper, spec.sea, spec.indigo, spec.matcha };
            for (int c = 0; c < chips.Length; c++)
                Img(Rect("Chip" + c, card.transform, 28 + c * 38, 152, 28, 28), chips[c], spriteCircle).raycastTarget = false;

            string statusTag = owned ? (active ? "使用中 ✓" : "已拥有") : trial ? ThemeTrialRemainingText(t) : "未解锁";
            Txt(card.transform, spec.name, 31, Ink, 200, 148, 340, 44, TextAnchor.MiddleLeft);

            if (spec.isNeon)
            {
                var miniTex = EraPosterGenerator.GetCustomPoster("shop_theme_3d", "3D", 0, 4, 256, 112, 0);
                CreateNeonPosterFrame(card.transform, "NeonThemeThumb", cardWidth - 224f, 100f, 196f, 88f, miniTex, 4f);
                Txt(card.transform, statusTag, 23, active ? Matcha : InkSoft, cardWidth - 450, 152, 210, 38, TextAnchor.MiddleRight);
                Txt(card.transform, spec.subtitle, 23, InkSoft, 28, 94, cardWidth - 266, 42, TextAnchor.MiddleLeft);
            }
            else
            {
                Txt(card.transform, statusTag, 23, active ? Matcha : InkSoft, cardWidth - 270, 152, 240, 38, TextAnchor.MiddleRight);
                Txt(card.transform, spec.subtitle, 23, InkSoft, 28, 94, cardWidth - 56, 42, TextAnchor.MiddleLeft);
            }

            if (avail)
            {
                float btnW = owned ? cardWidth - 56 : 360f;
                var useBtn = Btn(card.transform, "UseTheme" + i, active ? "当前正在使用" : "立即应用此风格", 25, 28, 16, btnW, 68, active ? Matcha : Indigo, () => {
                    if (active) { Toast("当前已经是该风格"); return; }
                    ApplyTheme(t); SaveGame(); OpenFleetTab(2); Toast("已切换为「" + spec.name + "」");
                });
                TInk(useBtn, Color.white);
            }

            if (!owned)
            {
                string adKey = "theme_" + t;
                int req = t >= 3 ? 5 : 3;
                int watched = adWatchCounts.TryGetValue(adKey, out int wc) ? wc : 0;

                if (!avail)
                {
                    float pvW = 176f;
                    var pvBtn = Btn(card.transform, "PreviewTheme" + i, "预览效果", 23, 28, 16, pvW, 68, new Color(0.12f, 0.50f, 0.64f), () => {
                        StartThemePreview(t);
                    });
                    TInk(pvBtn, Color.white);

                    float adX = 28f + pvW + 14f;
                    float adW = 344f;
                    string adTxt = "看片试用24h (" + watched + "/" + req + " 永久)";
                    var adBtn = Btn(card.transform, "AdTheme" + i, adTxt, 22, adX, 16, adW, 68, Indigo, () => {
                        PromptRewardedAd(adKey, req, spec.name,
                            () => {
                                ownedThemes.Add(t);
                                ApplyTheme(t); SaveGame(); OpenFleetTab(2);
                                Toast("已永久解锁「" + spec.name + "」");
                            },
                            () => {
                                themeTrialUntil[t] = NowUnixSeconds() + 86400;
                                ApplyTheme(t); SaveGame(); OpenFleetTab(2);
                                Toast("已激活「" + spec.name + "」24小时免费试用！");
                            });
                    });
                    TInk(adBtn, Color.white);

                    float altX = adX + adW + 14f;
                    float altW = cardWidth - altX - 28f;
                    bool techFree = t == 2 && techDone.Count >= 12;
                    int diamondCost = t == 4 ? 50 : 30;
                    int coinCost = t == 1 ? 68000 : t == 2 ? 158000 : t == 3 ? 280000 : 580000;
                    string altLabel = t == 1
                        ? "¥6.8万 解锁"
                        : t == 2
                            ? (techFree ? "免费领(科技≥12)" : "¥15.8万/12科技")
                            : (diamondCost + "钻 / ¥" + FormatCoins(coinCost));
                    var altBtn = Btn(card.transform, "AltTheme" + i, altLabel, 21, altX, 16, altW, 68, techFree ? Matcha : Paper2, () => {
                        if (t == 1)
                        {
                            if (coins < coinCost) { Toast("金币不足 ¥" + FormatCoins(coinCost) + "（可先看片试用或集赞解锁）"); return; }
                            BeginUndo("解锁界面风格"); coins -= coinCost; ownedThemes.Add(t); EndUndo();
                        }
                        else if (t == 2)
                        {
                            if (techDone.Count < 12)
                            {
                                if (coins < coinCost) { Toast("研究满 12 项科技可免费领，或花 ¥" + FormatCoins(coinCost) + " 解锁"); return; }
                                BeginUndo("解锁界面风格"); coins -= coinCost; ownedThemes.Add(t); EndUndo();
                            }
                            else { ownedThemes.Add(t); }
                        }
                        else
                        {
                            if (diamonds >= diamondCost)
                            {
                                BeginUndo("解锁界面风格"); diamonds -= diamondCost; ownedThemes.Add(t); EndUndo();
                            }
                            else if (coins >= coinCost)
                            {
                                BeginUndo("解锁界面风格"); coins -= coinCost; ownedThemes.Add(t); EndUndo();
                            }
                            else
                            {
                                Toast("需 " + diamondCost + " 钻或 ¥" + FormatCoins(coinCost) + "（推荐看片免费试用）");
                                return;
                            }
                        }
                        ApplyTheme(t); RefreshHUD(); SaveGame(); OpenFleetTab(2);
                        Toast("已永久解锁「" + spec.name + "」");
                    });
                    TInk(altBtn, techFree ? Color.white : Ink);
                }
                else
                {
                    float adX = 404f;
                    float adW = cardWidth - 432f;
                    string adTxt = "看片续期/永久 (" + watched + "/" + req + ")";
                    var adBtn = Btn(card.transform, "AdTheme" + i, adTxt, 23, adX, 16, adW, 68, Indigo, () => {
                        PromptRewardedAd(adKey, req, spec.name,
                            () => {
                                ownedThemes.Add(t);
                                ApplyTheme(t); SaveGame(); OpenFleetTab(2);
                                Toast("已永久解锁「" + spec.name + "」");
                            },
                            () => {
                                themeTrialUntil[t] = NowUnixSeconds() + 86400;
                                ApplyTheme(t); SaveGame(); OpenFleetTab(2);
                                Toast("已续期「" + spec.name + "」24小时试用！");
                            });
                    });
                    TInk(adBtn, Color.white);
                }
            }
        }
    }
    int dailyStationBaseline;
    int DailyConnectedCount()
    {
        var connected = new HashSet<int>();
        foreach (var line in lines) foreach (int index in line.st)
            if (index >= 0 && index < stations.Count && stations[index].built && !stations[index].closed) connected.Add(index);
        return connected.Count;
    }
    void JournalTape(Transform parent, float x, float y, float width)
    {
        var tape = Rect("Tape", parent, x, y, width, 25);
        Img(tape, new Color(0.82f,0.73f,0.54f,0.55f), spriteRound).raycastTarget = false;
        tape.transform.localRotation = Quaternion.Euler(0,0,-3);
    }
    void OpenDailyTasks()
    {
        var page = OpenPage("每日任务", "每日零点刷新；今日净增连通 2 站即可领奖。", "PageArt/album_watercolor");
        Txt(page.transform, "今日旅程", 38, Ink, 36, PgH-245, 600, 54, TextAnchor.MiddleLeft);
        Txt(page.transform, "第 " + day + " 日  ·  每日零点刷新", 22, InkSoft, 38, PgH-287, 700, 34, TextAnchor.MiddleLeft);
        var card = Rect("DailyTaskCard", page.transform, 28, PgH-760, W-56, 420);
        Rounded(card, CardBg); SoftShadow(card); JournalTape(card.transform, 440, 403, 144);
        int progress = Mathf.Clamp(DailyConnectedCount() - dailyStationBaseline, 0, 2);
        bool claimedToday = dailyTaskClaimedDay == day;
        Txt(card.transform, "让旅程更远一点", 38, Ink, 38, 324, 760, 50, TextAnchor.MiddleLeft);
        Txt(card.transform, "今日净增连通 2 座星港（赏金：+¥600 · +12 RP · +2 钻）", 25, InkSoft, 40, 270, 940, 38, TextAnchor.MiddleLeft);
        Img(Rect("Track", card.transform, 40, 212, 780, 16), new Color(0.89f,0.87f,0.81f), spriteRound).raycastTarget = false;
        if (progress > 0) Img(Rect("Progress", card.transform, 40, 212, 780 * progress / 2f, 16), Matcha, spriteRound).raycastTarget = false;
        Txt(card.transform, progress + " / 2", 30, Ink, 840, 196, 140, 45, TextAnchor.MiddleRight);
        string statusText = claimedToday ? "今日任务已完成，明日零点再来" : progress >= 2 ? "目标达成！可领取建设补贴" : "向下一港延伸航线网吧";
        Txt(card.transform, statusText, 24, (claimedToday || progress >= 2) ? Matcha : InkSoft, 40, 148, 900, 36, TextAnchor.MiddleLeft);
        if (claimedToday)
        {
            var doneBtn = Btn(card.transform, "ClaimedDone", "今日补贴已领取  ✓", 28, 40, 34, W-136, 90, new Color(0.84f, 0.85f, 0.81f), () => Toast("今日补贴已领取，明日零点刷新"));
            TInk(doneBtn, InkSoft);
        }
        else if (progress < 2)
        {
            var todoBtn = Btn(card.transform, "ClaimLocked", "再连通 " + (2 - progress) + " 座星港即可领奖", 27, 40, 34, W-136, 90, new Color(0.89f, 0.87f, 0.82f), () => Toast("还差 " + (2 - progress) + " 座新增连通星港"));
            TInk(todoBtn, InkSoft);
        }
        else
        {
            var claimBtn = Btn(card.transform, "ClaimNormal", "领取  +¥600 · +12 RP · +2 钻", 24, 40, 34, 444, 90, Matcha, () => {
                if (dailyTaskClaimedDay == day) return;
                coins += 600; rp += 12; diamonds += 2; dailyTaskClaimedDay = day;
                PlayCoinSfx(); SpawnConfettiBurst(new Vector2(RootW * 0.5f, RootH * 0.45f), Matcha, Gold, 12);
                SaveGame(); RefreshHUD(); OpenDailyTasks();
                Toast("每日补贴到账 +¥600 · +12 RP · +2 钻");
            });
            TInk(claimBtn, Color.white);
            var dblBtn = Btn(card.transform, "ClaimDouble", "看片双倍  +¥1,200 · +24 RP · +4 钻", 22, 504, 34, 480, 90, new Color(0.76f, 0.50f, 0.16f), () => {
                PromptRewardedAd("daily_double_" + day, 1, "每日任务双倍补贴 (+¥1,200 · +24 RP · +4 钻)", () => {
                    if (dailyTaskClaimedDay == day) return;
                    coins += 1200; rp += 24; diamonds += 4; dailyTaskClaimedDay = day;
                    SaveGame(); RefreshHUD(); OpenDailyTasks();
                    Toast("双倍每日补贴到账 +¥1,200 · +24 RP · +4 钻");
                });
            });
            TInk(dblBtn, Color.white);
        }

        // 星图志手帐风物卡：展示当令星候、荣誉星牌考成与复古纪念星票入口
        SyncCulturalCollection(false);
        var term = CurrentSolarTerm();
        float culY = PgH - 1210f;
        var culCard = Rect("CultureJournalCard", page.transform, 28f, culY, W - 56f, 412f);
        Rounded(culCard, CardBg); SoftShadow(culCard); JournalTape(culCard.transform, 440f, 395f, 144f);
        StampFineRuleFrame(culCard.transform, W - 56f, 412f, 8f, new Color(Matcha.r, Matcha.g, Matcha.b, 0.24f));
        StampCloudCornerSet(culCard.transform, W - 56f, 412f, 40f, 8f, 0.45f);
        StampBotanicalSprig(culCard.transform, "orn_kapok_bamboo", W - 56f - 148f, 268f, 134f, 134f, 0.30f, true, false);
        var seasonBadge = Rect("SeasonPill", culCard.transform, 38f, 316f, 128f, 54f);
        Rounded(seasonBadge, Matcha);
        Txt(seasonBadge.transform, term.name, 29, Color.white, 0, 0, 128f, 54f, TextAnchor.MiddleCenter);
        Txt(culCard.transform, "星图志 · " + term.season + "（剩 " + SolarTermDaysRemaining() + " 日）", 32, Ink, 184f, 320f, 700f, 46f, TextAnchor.MiddleLeft);
        Txt(culCard.transform, "「" + term.poem + "」", 24, InkSoft, 38f, 258f, W - 180f, 40f, TextAnchor.MiddleLeft);
        Txt(culCard.transform, "当期节候物候：" + term.effectHint, 24, Vermilion, 38f, 212f, W - 132f, 38f, TextAnchor.MiddleLeft);
        int readyPlaques = CountClaimablePlaques();
        string collectSummary = "已收录入【星图鉴】：荣誉星牌 " + earnedPlaques.Count + "/4"
            + (readyPlaques > 0 ? "（" + readyPlaques + "方待领）" : "")
            + " · 纪念票 " + earnedMemorialTickets.Count + "/6"
            + " · 功勋退役 " + retiredVehiclesCount + "艘";
        var csTxt = Txt(culCard.transform, collectSummary, 22, readyPlaques > 0 ? Vermilion : Indigo, 38f, 152f, W - 132f, 42f, TextAnchor.MiddleLeft);
        FitSingleLine(csTxt, 18);
        float cBtnW = (W - 132f - 28f) / 3f;
        var bPlaque = Btn(culCard.transform, "BtnPlaque", readyPlaques > 0 ? "星图 · 荣誉星牌 (可领)" : "星图 · 荣誉星牌", 23, 38f, 36f, cBtnW, 86f,
            readyPlaques > 0 ? Vermilion : Indigo, () => OpenSceneryTab(2));
        TInk(bPlaque, Color.white);
        var bTicket = Btn(culCard.transform, "BtnTicket", "星图 · 票册", 23, 38f + cBtnW + 14f, 36f, cBtnW, 86f,
            new Color(0.76f, 0.50f, 0.16f), () => OpenSceneryTab(1));
        TInk(bTicket, Color.white);
        var bSolar = Btn(culCard.transform, "BtnSolar", "星图 · 候历", 23, 38f + 2f * (cBtnW + 14f), 36f, cBtnW, 86f,
            Matcha, () => OpenSceneryTab(3));
        TInk(bSolar, Color.white);
    }
    GameObject CreateNeonPosterFrame(Transform parent, string name, float x, float y, float w, float h, Texture2D posterTex, float pad = 8f)
    {
        var frame = Rect(name, parent, x, y, w, h);
        Rounded(frame, new Color(0.025f, 0.025f, 0.045f, 1f));
        SoftShadow(frame);
        frame.GetComponent<Image>().raycastTarget = false;
        var rawGo = Rect("PosterRaw", frame.transform, pad, pad, w - pad * 2f, h - pad * 2f);
        var raw = rawGo.AddComponent<RawImage>();
        raw.texture = posterTex;
        raw.color = Color.white;
        raw.raycastTarget = false;
        return frame;
    }

    // 排行榜与深空星轨公报已迁至 DemoBulletin.cs。
    void OpenVehicleDetails(int index)
    {
        if (index < 0 || index >= LocoNames.Length) return;
        InitHistoryTree();
        string intro = historyByKey.TryGetValue(LocoKey(index), out var node) ? node.description : "暂无车型简介。";
        float bodyHeight = Mathf.Clamp(MeasureTextHeight(intro, 25, 888) + 10, 54, H - 1100);
        float artY = 340 + bodyHeight;
        var panel = OpenPopup(LocoNames[index] + " · " + LocoYears[index], artY + 580, 960);
        var backdrop = Rect("VehicleArtFrame", panel.transform, 30, artY, 900, 490);
        Rounded(backdrop, new Color(0.93f,0.92f,0.85f,0.75f));
        var art = Rect("VehicleArt", backdrop.transform, 40, 40, 820, 410).AddComponent<RawImage>();
        art.texture = Resources.Load<Texture2D>("Research/Train/" + LocoArt[index]); art.raycastTarget = false;
        if (art.texture) FitVehicleImage(art, 820, 410);
        else
        {
            // 资源缺失时隐藏空贴图，改用纯色星舰徽记占位，FitVehicleImage 内部已有 texture 判空。
            art.gameObject.SetActive(false);
            var fb = Img(Rect("VehicleArtFallback", backdrop.transform, 40, 40, 820, 410), new Color(0.36f, 0.46f, 0.66f, 0.38f), spriteCircle);
            fb.raycastTarget = false;
        }
        Txt(panel.transform, "舰型简介", 31, Ink, 36, 290 + bodyHeight, 860, 44, TextAnchor.MiddleLeft);
        ScrollableText(panel.transform, intro, 25, InkSoft, 36, 280, 888, bodyHeight);
        Txt(panel.transform, FormatLocoRealStats(index), 24, InkSoft, 36, 230, 888, 42, TextAnchor.MiddleLeft);
        var fullscreen = Btn(panel.transform, "Fullscreen", "横屏查看大图", 30, 36, 46, 888, 90, Indigo, () => OpenVehicleImage(index)); TInk(fullscreen, Color.white);
    }
    void FitVehicleImage(RawImage image, float width, float height)
    {
        if (!image.texture) return;
        float ratio = (float)image.texture.width / image.texture.height;
        float w = Mathf.Min(width, height * ratio), h = w / ratio;
        image.rectTransform.sizeDelta = new Vector2(w, h);
        image.rectTransform.anchoredPosition = new Vector2(40f + (width - w) / 2f, 40f + (height - h) / 2f);
    }
    void OpenVehicleImage(int index)
    {
        var texture = Resources.Load<Texture2D>("Research/Train/" + LocoArt[index]);
        if (!texture) { Toast("星舰图片暂不可用"); return; }
        ClosePanel(true);
        if (sceneryViewer) Destroy(sceneryViewer);
        sceneryViewer = new GameObject("VehicleViewer", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster), typeof(SceneryViewerLayout));
        var viewerCanvas = sceneryViewer.GetComponent<Canvas>(); viewerCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        viewerCanvas.overrideSorting = true; viewerCanvas.sortingOrder = 100;
        var bg = Rect("Backdrop", sceneryViewer.transform, 0, 0, 0, 0);
        var bgRt = bg.GetComponent<RectTransform>(); bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one; bgRt.offsetMin = bgRt.offsetMax = Vector2.zero;
        Img(bg, new Color(0.94f,0.92f,0.87f));
        var stage = Rect("Landscape", sceneryViewer.transform, 0, 0, 1920, 1080);
        var stageRt = stage.GetComponent<RectTransform>(); stageRt.anchorMin = stageRt.anchorMax = new Vector2(0.5f,0.5f); stageRt.pivot = new Vector2(0.5f,0.5f);
        stageRt.anchoredPosition = Vector2.zero; sceneryViewer.GetComponent<SceneryViewerLayout>().stage = stageRt;
        var art = Rect("VehicleImage", stage.transform, 95, 160, 1730, 760).AddComponent<RawImage>(); art.texture = texture; art.raycastTarget = false;
        float ratio = (float)texture.width / texture.height, width = Mathf.Min(1730, 760 * ratio), height = width / ratio;
        art.rectTransform.sizeDelta = new Vector2(width,height); art.rectTransform.anchoredPosition = new Vector2(95 + (1730-width)/2f,160 + (760-height)/2f);
        var back = Btn(stage.transform, "Back", "←", 40, 24, 958, 94, 94, Color.white, () => { Destroy(sceneryViewer); sceneryViewer = null; OpenVehicleDetails(index); }); TInk(back, Ink);
        var home = Btn(stage.transform, "Home", "主页", 24, 134, 958, 104, 94, Color.white, GoHome); TInk(home, Ink);
        Txt(stage.transform, LocoNames[index] + " · " + LocoYears[index], 44, Ink, 254, 975, 800, 60, TextAnchor.MiddleLeft);
    }
    void OpenCompanyScreen()
    {
        EnsureCompanyInitialized();
        EnsureTalentsInitialized();
        companyTab = Mathf.Clamp(companyTab, 0, 3);
        var pg = OpenPage("公司", "总部运营台 · 统筹理事任命、人才猎聘培养、周期风向、债券招标与效能特权。", "PageArt/company_watercolor", "company_screen");
        AttachPageCurrencyBadge(true);

        string[] tabNames = { "总览 · 企业", "高管 · 人才", "资本 · 招投标", "特权 · 效率厅" };
        for (int i = 0; i < tabNames.Length; i++)
        {
            int tabIdx = i;
            bool active = companyTab == tabIdx;
            bool tabLocked = tabIdx == 2 && !IsFeatureUnlocked(GameFeature.CompanyFinanceAndBoard);
            string label = tabLocked ? tabNames[i] + " 🔒" : tabNames[i];
            var tBtn = Btn(pg.transform, "CompanyTab_" + i, label, 22, 28f + i * 260f, PgH - 252f, 244f, 64f,
                active ? Indigo : CardBg, () => {
                    if (tabIdx == 2 && !IsFeatureUnlocked(GameFeature.CompanyFinanceAndBoard))
                    {
                        PlayErrorSfx();
                        Toast(FeatureLockHint(GameFeature.CompanyFinanceAndBoard));
                        return;
                    }
                    companyTab = tabIdx; OpenCompanyScreen();
                });
            TInk(tBtn, active ? Color.white : tabLocked ? InkSoft : Ink);
            if (!active) SoftShadow(tBtn.gameObject);
        }

        float cardW = W - 56f;
        if (companyTab == 0) BuildCompanyOverviewTab(pg.transform, cardW);
        else if (companyTab == 1) BuildCompanyBoardOrTalentTab(pg.transform, cardW);
        else if (companyTab == 2) BuildCompanyFinanceTab(pg.transform, cardW);
        else BuildCompanyPrivilegeTab(pg.transform, cardW);
    }

    void BuildCompanyBoardOrTalentTab(Transform page, float cardW)
    {
        companyBoardSubTab = Mathf.Clamp(companyBoardSubTab, 0, 1);
        float subY = PgH - 332f;
        var b1 = Btn(page, "BoardSub0", "核心理事会 (" + boardPartners.Count + "/" + BoardSeatCapacity() + ")", 22, 28f, subY, 502f, 58f,
            companyBoardSubTab == 0 ? Matcha : CardBg, () => { companyBoardSubTab = 0; OpenCompanyScreen(); });
        TInk(b1, companyBoardSubTab == 0 ? Color.white : Ink);
        if (companyBoardSubTab != 0) SoftShadow(b1.gameObject);

        var b2 = Btn(page, "BoardSub1", "才俊招募与培养 (" + talents.Count + "/" + TalentRosterCapacity() + ")", 22, 550f, subY, 502f, 58f,
            companyBoardSubTab == 1 ? Matcha : CardBg, () => { companyBoardSubTab = 1; OpenCompanyScreen(); });
        TInk(b2, companyBoardSubTab == 1 ? Color.white : Ink);
        if (companyBoardSubTab != 1) SoftShadow(b2.gameObject);

        if (companyBoardSubTab == 0) BuildCompanyBoardTab(page, cardW);
        else BuildTalentManagementView(page, cardW);
    }

    void BuildCompanyOverviewTab(Transform page, float cardW)
    {
        float bayH = 98f + CrossBayCorridorNames.Length * 56f;
        float totalH = 2074f + (bayH - 266f);
        var content = FleetScroll(page, 36f, PgH - 270f, totalH, scrollKey: "company:overview");
        float topY = content.sizeDelta.y;
        float cardX = 4f, innerW = cardW - 8f, cW = innerW - 64f;

        // 1. 品牌名片卡（公司徽章 + 头衔 + 在线改名 + 升阶坐标 + 核心损益）
        float y1 = topY - 12f - 366f;
        var brandCard = Rect("CompanyBrandCard", content, cardX, y1, innerW, 366f);
        Rounded(brandCard, CardBg); SoftShadow(brandCard); JournalTape(brandCard.transform, innerW * 0.5f - 72f, 349f, 144f);

        int sealIdx = Mathf.Clamp(companySeal, 0, CompanySealChars.Length - 1);
        Color sealBg = sealIdx >= 6 ? new Color(0.76f, 0.50f, 0.16f) : Vermilion;
        var sealBtn = Btn(brandCard.transform, "SealButton", CompanySealChars[sealIdx], 68, 32f, 182f, 138f, 138f, sealBg, OpenCompanySealPicker);
        TInk(sealBtn, Color.white);
        Txt(brandCard.transform, "换徽", 23, sealBg, 32f, 142f, 138f, 34f, TextAnchor.MiddleCenter);

        Txt(brandCard.transform, "头衔 · " + CompanyTitle() + "  ·  " + CreditTierLabels[CompanyCreditTier()] + "信用（已连 " + DailyConnectedCount() + " 站）",
            22, Matcha, 196f, 298f, 788f, 36f, TextAnchor.MiddleLeft);

        var nameBox = Rect("CompanyNameInputBox", brandCard.transform, 196f, 222f, 620f, 66f);
        Rounded(nameBox, Color.Lerp(CardBg, Ink, 0.06f));
        var nameTxt = Txt(nameBox.transform, companyName, 31, Ink, 18f, 0f, 584f, 66f, TextAnchor.MiddleLeft);
        nameTxt.supportRichText = false;
        var nameInput = nameBox.AddComponent<InputField>();
        nameInput.textComponent = nameTxt;
        nameInput.lineType = InputField.LineType.SingleLine;
        nameInput.characterLimit = 12;
        nameInput.text = companyName;
        nameInput.onEndEdit.AddListener(val => {
            string trimmed = string.IsNullOrWhiteSpace(val) ? "深空轨社" : val.Trim();
            companyName = trimmed;
            nameInput.text = companyName;
            SaveGame();
            Toast("企业字号已更名为「" + companyName + "」");
        });
        var renameBtn = Btn(brandCard.transform, "RenameBtn", "改名", 24, 832f, 222f, 152f, 66f, Indigo, () => {
            nameInput.Select();
            nameInput.ActivateInputField();
        });
        TInk(renameBtn, Color.white);

        int activePartners = boardPartners.Count + (consultantDaysLeft > 0 ? 1 : 0);
        Txt(brandCard.transform, "高管 " + activePartners + " 人   ·   运营 " + lines.Count + " 线   ·   名胜 " + FoundCount() + " 处   ·   科技 " + techDone.Count + " 项",
            22, InkSoft, 196f, 160f, 788f, 38f, TextAnchor.MiddleLeft);
        Txt(brandCard.transform, NextCompanyRankHint(), 23, Indigo, 196f, 118f, 788f, 38f, TextAnchor.MiddleLeft);

        float halfStatW = (cW - 20f) * 0.5f;
        var statLeft = Rect("StatProfit", brandCard.transform, 32f, 24f, halfStatW, 68f);
        Rounded(statLeft, Color.Lerp(CardBg, Ink, 0.04f));
        Txt(statLeft.transform, "预期日利润  " + FormatSignedCoins(income), 24, income >= 0f ? Matcha : Vermilion, 16f, 0f, halfStatW - 32f, 68f, TextAnchor.MiddleLeft);

        var statRight = Rect("StatProperty", brandCard.transform, 32f + halfStatW + 20f, 24f, halfStatW, 68f);
        Rounded(statRight, Color.Lerp(CardBg, Ink, 0.04f));
        Txt(statRight.transform, "港前物业  " + DevelopedCommercialCount() + " 处（+¥" + FormatCoins(StationCommercialTotalDaily()) + "/日）", 23, Indigo, 16f, 0f, halfStatW - 32f, 68f, TextAnchor.MiddleLeft);

        // 2. 《深空星风周报》周期剪报卡
        float y2 = y1 - 22f - 252f;
        var newsCard = Rect("EconomyCycleCard", content, cardX, y2, innerW, 252f);
        Rounded(newsCard, CardBg); SoftShadow(newsCard); JournalTape(newsCard.transform, innerW * 0.5f - 72f, 235f, 144f);
        int weekNum = ((day - 1) / 7) + 1;
        Txt(newsCard.transform, "《深空航行周报》第 " + weekNum + " 周 · " + EconomyCycleNames[economyCycle], 28, Ink, 32f, 186f, 670f, 44f, TextAnchor.MiddleLeft);
        Txt(newsCard.transform, "下周切换还剩 " + CycleDaysRemaining() + " 日", 22, Vermilion, 704f, 188f, 280f, 38f, TextAnchor.MiddleRight);
        Txt(newsCard.transform, EconomyCycleDescs[economyCycle], 23, InkSoft, 32f, 114f, cW, 62f, TextAnchor.UpperLeft);

        var previewBar = Rect("NextCyclePreview", newsCard.transform, 32f, 22f, cW, 74f);
        Rounded(previewBar, Color.Lerp(CardBg, Indigo, 0.08f));
        Txt(previewBar.transform, "下周风向：" + EconomyCycleNames[nextEconomyCycle] + " — " + EconomyCycleDescs[nextEconomyCycle],
            20, Indigo, 16f, 6f, cW - 32f, 62f, TextAnchor.MiddleLeft);

        // 3. 总裁经营方针（4 选 1 全局策略卡）
        float y3 = y2 - 22f - 436f;
        var policyCard = Rect("CompanyPolicyCard", content, cardX, y3, innerW, 436f);
        Rounded(policyCard, CardBg); SoftShadow(policyCard);
        Txt(policyCard.transform, "总裁经营方针", 30, Ink, 32f, 372f, 420f, 44f, TextAnchor.MiddleLeft);
        bool policyLockedToday = lastPolicyDay == day;
        Txt(policyCard.transform, policyLockedToday ? "今日已定策 · 明日零点可调" : "每日限调 1 次", 22, policyLockedToday ? Vermilion : Matcha, 460f, 374f, 524f, 40f, TextAnchor.MiddleRight);

        for (int i = 0; i < CompanyPolicyNames.Length; i++)
        {
            int idx = i;
            bool active = companyPolicy == idx;
            float px = 32f + (i % 2) * (halfStatW + 20f);
            float py = (i < 2) ? 194f : 22f;
            Color pBg = active ? Color.Lerp(CardBg, Indigo, 0.18f) : Color.Lerp(CardBg, Ink, 0.05f);
            var pBtn = Btn(policyCard.transform, "Policy_" + idx, "", 20, px, py, halfStatW, 158f, pBg, () => {
                if (companyPolicy == idx) { Toast("当前正实行「" + CompanyPolicyNames[idx] + "」"); return; }
                if (lastPolicyDay == day) { Toast("经营方针每日限变更 1 次"); return; }
                BeginUndo("切换经营方针");
                companyPolicy = idx;
                lastPolicyDay = day;
                operatingRates.Clear();
                RecalcIncome();
                EndUndo(); SaveGame(); RefreshHUD(); OpenCompanyScreen();
                Toast("已切换为「" + CompanyPolicyNames[idx] + "」方针");
            });
            var titleTxt = Txt(pBtn.transform, CompanyPolicyNames[idx] + (active ? "  ✓ 生效中" : ""), 26, active ? Indigo : Ink, 18f, 104f, halfStatW - 36f, 42f, TextAnchor.MiddleLeft);
            titleTxt.raycastTarget = false;
            var descTxt = Txt(pBtn.transform, CompanyPolicyDescs[idx], 22, InkSoft, 18f, 10f, halfStatW - 36f, 90f, TextAnchor.UpperLeft);
            descTxt.raycastTarget = false;
        }

        // 4. 年度星轨大赏 + 港前商业物业导览
        float y4 = y3 - 22f - 344f;
        var honorCard = Rect("CompanyHonorAndProp", content, cardX, y4, innerW, 344f);
        Rounded(honorCard, CardBg); SoftShadow(honorCard);

        int awardCycle = (day / 15) * 15;
        bool awardReady = day >= 15 && lastAwardDay < awardCycle;
        var awardRow = Rect("AnnualAwardRow", honorCard.transform, 32f, 212f, cW, 106f);
        Rounded(awardRow, awardReady ? Color.Lerp(CardBg, new Color(0.95f, 0.72f, 0.20f), 0.25f) : Color.Lerp(CardBg, Ink, 0.04f));
        Txt(awardRow.transform, "年度星轨大赏（每 15 日授勋）", 25, Ink, 18f, 58f, 620f, 38f, TextAnchor.MiddleLeft);
        string awardHint = awardReady
            ? "第 " + awardCycle + " 日大赏揭晓，贺礼待领取"
            : "距下届评比还有 " + (15 - (day % 15)) + " 日";
        Txt(awardRow.transform, awardHint, 23, awardReady ? Vermilion : InkSoft, 18f, 12f, 620f, 38f, TextAnchor.MiddleLeft);
        var awardBtn = Btn(awardRow.transform, "OpenAwardBtn", awardReady ? "立即领赏" : "查看评比", 24, cW - 280f, 17f, 260f, 72f,
            awardReady ? new Color(0.76f, 0.50f, 0.16f) : Indigo, OpenAnnualAwardPopup);
        TInk(awardBtn, Color.white);

        int devCount = DevelopedCommercialCount();
        Txt(honorCard.transform, "港前物业（已落成 " + devCount + " 处）", 27, Ink, 32f, 154f, 800f, 40f, TextAnchor.MiddleLeft);
        string propSummary = "";
        int shown = 0;
        for (int i = 0; i < stations.Count; i++)
        {
            var st = stations[i];
            if (st.facLevel <= 0) continue;
            if (shown < 3)
            {
                if (shown > 0) propSummary += "、";
                int lvl = Mathf.Clamp(st.facLevel, 0, StationFacilityNames.Length - 1);
                int baseRent = StationFacilityRents[lvl];
                propSummary += StationDisplayName(i, currentRegion) + "(" + StationFacilityNames[lvl] + " ¥" + Mathf.RoundToInt(baseRent * TechMult() * (HasPartner(1) ? 1.25f : 1f)) + "/日)";
            }
            shown++;
        }
        if (shown > 3) propSummary += " 等共 " + shown + " 处";
        if (shown == 0) propSummary = "暂未开发物业。点选地图星港「详情」可投资商铺、商业街或都会广场。";
        Txt(honorCard.transform, propSummary, 23, InkSoft, 32f, 92f, cW, 58f, TextAnchor.UpperLeft);

        var goMapBtn = Btn(honorCard.transform, "GoBuildCommercial", "返回地图挑选星港开发物业", 25, 32f, 20f, cW, 66f, Matcha, () => {
            ClosePage();
            Toast("点开任意已建星港「详情」即可投资港前物业");
        });
        TInk(goMapBtn, Color.white);

        // 5. 星域口岸联运协同卡
        float bayCardH = 98f + CrossBayCorridorNames.Length * 56f;
        float y5 = y4 - 22f - bayCardH;
        var bayCard = Rect("CrossBaySynergyCard", content, cardX, y5, innerW, bayCardH);
        Rounded(bayCard, CardBg); SoftShadow(bayCard); JournalTape(bayCard.transform, innerW * 0.5f - 72f, bayCardH - 17f, 144f);
        int bayLinks = CrossBaySynergyLinkCount();
        Txt(bayCard.transform, "星域口岸联运协同（" + bayLinks + " / " + CrossBayCorridorNames.Length + " 走廊）", 28, Ink, 32f, bayCardH - 64f, 620f, 42f, TextAnchor.MiddleLeft);
        Txt(bayCard.transform, bayLinks > 0 ? "+¥" + Mathf.RoundToInt(CrossBaySynergyDailyBonus()) + "/日" : "待贯通", 24, bayLinks > 0 ? Matcha : InkSoft, 640f, bayCardH - 62f, 344f, 38f, TextAnchor.MiddleRight);
        for (int b = 0; b < CrossBayCorridorNames.Length; b++)
        {
            bool active = CrossBayCorridorActive(b);
            float ry = bayCardH - 126f - b * 56f;
            var rowBg = Rect("BayRow_" + b, bayCard.transform, 32f, ry, cW, 48f);
            Rounded(rowBg, active ? Color.Lerp(CardBg, Matcha, 0.14f) : Color.Lerp(CardBg, Ink, 0.04f));
            Txt(rowBg.transform, (active ? "✓ " : "○ ") + CrossBayCorridorNames[b], 21, active ? Matcha : Ink, 16f, 0f, 250f, 48f, TextAnchor.MiddleLeft);
            Txt(rowBg.transform, CrossBayCorridorDescs[b], 19, InkSoft, 272f, 0f, cW - 288f, 48f, TextAnchor.MiddleLeft);
        }

        // 6. 星图志（统一入口直达星图鉴）
        SyncCulturalCollection(false);
        float y6 = y5 - 22f - 262f;
        var culOverview = Rect("CompanyCultureCard", content, cardX, y6, innerW, 262f);
        Rounded(culOverview, CardBg); SoftShadow(culOverview); JournalTape(culOverview.transform, innerW * 0.5f - 72f, 245f, 144f);
        StampFineRuleFrame(culOverview.transform, innerW, 262f, 8f, new Color(Gold.r, Gold.g, Gold.b, 0.28f));
        StampCloudCornerSet(culOverview.transform, innerW, 262f, 36f, 8f, 0.42f);
        StampBotanicalSprig(culOverview.transform, "orn_bauhinia_lotus", innerW - 118f, 142f, 108f, 108f, 0.28f, true, false);
        var curTerm = CurrentSolarTerm();
        int readyPlaqueCount = CountClaimablePlaques();
        Txt(culOverview.transform, "星图鉴 · 荣誉星牌与票册总览", 28, Ink, 32f, 198f, 600f, 42f, TextAnchor.MiddleLeft);
        Txt(culOverview.transform, "当令【" + curTerm.name + "】· 剩 " + SolarTermDaysRemaining() + " 日", 22, Matcha, 610f, 200f, 340f, 38f, TextAnchor.MiddleRight);
        Txt(culOverview.transform, "「" + curTerm.poem + "」  " + curTerm.effectHint, 22, InkSoft, 32f, 152f, cW - 80f, 38f, TextAnchor.MiddleLeft);
        Txt(culOverview.transform, "已获荣誉星牌 " + earnedPlaques.Count + "/4   ·   星航纪念票 " + earnedMemorialTickets.Count + "/6   ·   功勋退役 " + retiredVehiclesCount + " 台",
            22, readyPlaqueCount > 0 ? Vermilion : Indigo, 32f, 108f, cW, 36f, TextAnchor.MiddleLeft);
        float coBtnW = (cW - 24f) / 3f;
        var oPlaque = Btn(culOverview.transform, "OverviewPlaqueBtn", readyPlaqueCount > 0 ? "星图 · 荣誉星牌 (可领" + readyPlaqueCount + ")" : "星图 · 荣誉星牌", 22, 32f, 22f, coBtnW, 70f,
            readyPlaqueCount > 0 ? Vermilion : Indigo, () => OpenSceneryTab(2));
        TInk(oPlaque, Color.white);
        var oTicket = Btn(culOverview.transform, "OverviewTicketBtn", "星图 · 票册 " + earnedMemorialTickets.Count + "/6", 22, 32f + coBtnW + 12f, 22f, coBtnW, 70f,
            new Color(0.76f, 0.50f, 0.16f), () => OpenSceneryTab(1));
        TInk(oTicket, Color.white);
        var oSolar = Btn(culOverview.transform, "OverviewSolarBtn", "星图 · 候历", 22, 32f + 2f * (coBtnW + 12f), 22f, coBtnW, 70f,
            Matcha, () => OpenSceneryTab(3));
        TInk(oSolar, Color.white);
    }

    void BuildCompanyBoardTab(Transform page, float cardW)
    {
        float totalH = 1412f;
        var content = FleetScroll(page, 36f, PgH - 350f, totalH, scrollKey: "company:board");
        float topY = content.sizeDelta.y;
        float cardX = 4f, innerW = cardW - 8f, cW = innerW - 64f;

        // 1. 理事会席位总览（随企业治理树可扩充至最多 5 常驻席 + 1 IAA 特聘席）
        float y1 = topY - 12f - 246f;
        var seatCard = Rect("BoardSeatSummary", content, cardX, y1, innerW, 246f);
        Rounded(seatCard, CardBg); SoftShadow(seatCard);
        int seats = BoardSeatCapacity();
        Txt(seatCard.transform, "深空轨社理事会 · 常驻席位 " + boardPartners.Count + " / " + seats, 30, Ink, 32f, 182f, 580f, 44f, TextAnchor.MiddleLeft);
        Txt(seatCard.transform, "头衔与企业治理树可扩充席位", 23, InkSoft, 584f, 186f, 400f, 38f, TextAnchor.MiddleRight);

        int permSlots = Mathf.Max(3, seats);
        int totalCols = permSlots + 1;
        float gap = 12f;
        float slotW = (cW - gap * (totalCols - 1)) / totalCols;
        for (int i = 0; i < totalCols; i++)
        {
            float sx = 32f + i * (slotW + gap);
            if (i < permSlots)
            {
                bool unlocked = i < seats;
                bool occupied = i < boardPartners.Count;
                int pid = occupied ? boardPartners[i] : -1;
                Color bg = !unlocked ? Color.Lerp(CardBg, Ink, 0.08f) : occupied ? Color.Lerp(CardBg, Indigo, 0.14f) : Color.Lerp(CardBg, Matcha, 0.10f);
                var sBtn = Btn(seatCard.transform, "SeatSlot_" + i, "", 22, sx, 22f, slotW, 140f, bg, () => {
                    if (!unlocked) { Toast(i == 1 ? "升至「星港干线局」（8港或3线）解锁" : i == 2 ? "升至「跨星域航运公司」（16港且日利润¥600）解锁" : "可在「企业治理树」继续扩充董事席位"); return; }
                    if (occupied)
                    {
                        BeginUndo("卸任高管");
                        boardPartners.RemoveAt(i);
                        hasForecastSignature = false; operatingRates.Clear(); RecalcIncome();
                        EndUndo(); SaveGame(); OpenCompanyScreen();
                        Toast(PartnerNames[pid] + " 已卸任");
                    }
                    else Toast("请在下方核心理事档案选择任命人选");
                });
                string topT = !unlocked ? "未解锁" : occupied ? PartnerNames[pid] : "空缺常驻席";
                string subT = !unlocked ? (i == 1 ? "干线局解锁" : i == 2 ? "跨星海公司解锁" : "治理树解锁") : occupied ? PartnerTitles[pid] + "\n(点击卸任)" : "待从下方任命";
                var t1 = Txt(sBtn.transform, topT, permSlots > 3 ? 22 : 25, !unlocked ? InkSoft : occupied ? Indigo : Matcha, 6f, 84f, slotW - 12f, 42f, TextAnchor.MiddleCenter);
                FitSingleLine(t1, permSlots > 3 ? 22 : 25); t1.raycastTarget = false;
                var t2 = Txt(sBtn.transform, subT, permSlots > 3 ? 19 : 21, InkSoft, 6f, 10f, slotW - 12f, 70f, TextAnchor.UpperCenter);
                t2.raycastTarget = false;
            }
            else
            {
                bool hasCons = consultantDaysLeft > 0 && consultantId >= 0 && consultantId < PartnerNames.Length;
                Color cBg = hasCons ? Color.Lerp(CardBg, new Color(0.95f, 0.70f, 0.15f), 0.25f) : Color.Lerp(CardBg, new Color(0.76f, 0.50f, 0.16f), 0.12f);
                var cBtn = Btn(seatCard.transform, "ConsultantSlot", "", 22, sx, 22f, slotW, 140f, cBg, () => {
                    PromptRewardedAd("hire_huo_" + day, 1, "特聘传奇顾问·霍老首席（3日全能零负面）", () => {
                        consultantId = 6;
                        consultantDaysLeft += 3;
                        hasForecastSignature = false; operatingRates.Clear(); RecalcIncome();
                        SaveGame(); OpenCompanyScreen();
                        Toast("霍老首席已赴第 4 特聘席（剩余 " + consultantDaysLeft + " 天）");
                    });
                });
                var ct1 = Txt(cBtn.transform, hasCons ? PartnerNames[consultantId] : "特聘席", permSlots > 3 ? 22 : 25, new Color(0.65f, 0.38f, 0.05f), 6f, 84f, slotW - 12f, 42f, TextAnchor.MiddleCenter);
                FitSingleLine(ct1, permSlots > 3 ? 22 : 25); ct1.raycastTarget = false;
                var ct2 = Txt(cBtn.transform, hasCons ? "驻席加持\n剩 " + consultantDaysLeft + " 天" : "看片空降霍老\n全增益0负面", permSlots > 3 ? 19 : 21, InkSoft, 6f, 10f, slotW - 12f, 70f, TextAnchor.UpperCenter);
                ct2.raycastTarget = false;
            }
        }

        // 2. 星轨拓荒资深理事列表（0~6）
        float y2 = y1 - 22f - 1114f;
        var listCard = Rect("PartnerRosterCard", content, cardX, y2, innerW, 1114f);
        Rounded(listCard, CardBg); SoftShadow(listCard);
        Txt(listCard.transform, "星轨拓荒档案（按特质自由搭配）", 28, Ink, 32f, 1052f, 800f, 42f, TextAnchor.MiddleLeft);

        Color[] avatarColors = {
            Indigo, Matcha, new Color(0.72f, 0.45f, 0.22f),
            new Color(0.28f, 0.52f, 0.65f), Vermilion, new Color(0.55f, 0.36f, 0.62f),
            new Color(0.76f, 0.50f, 0.16f)
        };
        for (int i = 0; i < PartnerNames.Length; i++)
        {
            int pid = i;
            float ry = 900f - i * 146f;
            bool isSpecial = pid == 6;
            bool hired = isSpecial ? (consultantDaysLeft > 0 && consultantId == 6) : boardPartners.Contains(pid);
            var row = Rect("PartnerRow_" + pid, listCard.transform, 32f, ry, cW, 134f);
            Rounded(row, hired ? Color.Lerp(CardBg, Indigo, 0.10f) : Color.Lerp(CardBg, Ink, 0.04f));

            var avatar = Rect("Avatar", row.transform, 16f, 21f, 92f, 92f);
            Rounded(avatar, avatarColors[pid]);
            Txt(avatar.transform, PartnerNames[pid].Substring(0, 1), 44, Color.white, 0f, 0f, 92f, 92f, TextAnchor.MiddleCenter);

            Txt(row.transform, PartnerNames[pid] + " · " + PartnerTitles[pid], 25, Ink, 126f, 90f, 560f, 36f, TextAnchor.MiddleLeft);
            Txt(row.transform, "✓ " + PartnerPros[pid], 23, Matcha, 126f, 50f, 560f, 36f, TextAnchor.MiddleLeft);
            Txt(row.transform, "• " + PartnerCons[pid], 22, isSpecial ? Indigo : Vermilion, 126f, 12f, 560f, 34f, TextAnchor.MiddleLeft);

            if (isSpecial)
            {
                var pBtn = Btn(row.transform, "HireSpecial", hired ? "在岗 " + consultantDaysLeft + "天(延)" : "看片特聘 3 天", 23, cW - 252f, 31f, 232f, 72f, new Color(0.76f, 0.50f, 0.16f), () => {
                    PromptRewardedAd("hire_huo_" + day, 1, "特聘传奇顾问·霍老首席（3日全能零负面）", () => {
                        consultantId = 6;
                        consultantDaysLeft += 3;
                        hasForecastSignature = false; operatingRates.Clear(); RecalcIncome();
                        SaveGame(); OpenCompanyScreen();
                        Toast("霍老首席已赴第 4 特聘席（剩余 " + consultantDaysLeft + " 天）");
                    });
                });
                TInk(pBtn, Color.white);
            }
            else
            {
                bool seatsFull = !hired && boardPartners.Count >= seats;
                string btnTxt = hired ? "卸任高管" : seatsFull ? "席位已满" : "任命上岗";
                Color bCol = hired ? Vermilion : seatsFull ? new Color(0.78f, 0.76f, 0.71f) : Matcha;
                var pBtn = Btn(row.transform, "Hire_" + pid, btnTxt, 22, cW - 252f, 31f, 232f, 72f, bCol, () => {
                    if (hired)
                    {
                        BeginUndo("卸任高管");
                        boardPartners.Remove(pid);
                        hasForecastSignature = false; operatingRates.Clear(); RecalcIncome();
                        EndUndo(); SaveGame(); OpenCompanyScreen();
                        Toast(PartnerNames[pid] + " 已卸任");
                    }
                    else if (seatsFull) Toast("常驻席位已满（" + seats + " 席），可先卸任再任命或提升头衔");
                    else
                    {
                        BeginUndo("任命高管");
                        boardPartners.Add(pid);
                        hasForecastSignature = false; operatingRates.Clear(); RecalcIncome();
                        EndUndo(); SaveGame(); OpenCompanyScreen();
                        Toast("已任命「" + PartnerNames[pid] + "」进入理事会");
                    }
                });
                TInk(pBtn, seatsFull ? InkSoft : Color.white);
            }
        }
    }

    void BuildCompanyFinanceTab(Transform page, float cardW)
    {
        float totalH = 1280f;
        var content = FleetScroll(page, 36f, PgH - 270f, totalH, scrollKey: "company:finance");
        float topY = content.sizeDelta.y;
        float cardX = 4f, innerW = cardW - 8f, cW = innerW - 64f;

        // 1. 企业信用评级与动态融资卡
        float y1 = topY - 12f - 548f;
        var finCard = Rect("CompanyCreditCard", content, cardX, y1, innerW, 548f);
        Rounded(finCard, CardBg); SoftShadow(finCard);

        int score = CompanyCreditScore();
        int tier = CompanyCreditTier();
        int maxBond = CurrentCreditBondAmount(tier);
        int stdDaily = CurrentCreditBondDaily(tier);

        Txt(finCard.transform, "企业信用评级：" + CreditTierLabels[tier] + "  （" + score + "/100）", 30, Ink, 32f, 484f, 670f, 44f, TextAnchor.MiddleLeft);
        Txt(finCard.transform, "已结清债券 " + bondsRepaid + " 笔", 22, Matcha, 704f, 486f, 280f, 38f, TextAnchor.MiddleRight);
        Txt(finCard.transform, "随连通港数、预期利润、港前物业、科技与履约记录成长；信用越优授信越高。",
            20, InkSoft, 32f, 424f, cW, 52f, TextAnchor.UpperLeft);

        // 1.1 动态基建债券区
        var bondBox = Rect("DynamicBondBox", finCard.transform, 32f, 166f, cW, 244f);
        Rounded(bondBox, Color.Lerp(CardBg, Indigo, 0.06f));
        if (bondDaysLeft > 0)
        {
            int curDaily = EffectiveBondDailyPayment();
            int redeemCost = BondEarlyRedeemCost();
            Txt(bondBox.transform, "在还基建债券（本金 ¥" + (bondPrincipal > 0 ? bondPrincipal : BondPrincipal) + "）", 26, Ink, 20f, 184f, 880f, 40f, TextAnchor.MiddleLeft);
            Txt(bondBox.transform, "剩 " + bondDaysLeft + " 期 · 每晚扣款 ¥" + curDaily + (bondSubsidized ? "（已享 IAA 贴息 85 折 ✓）" : "（余额不足将扣半延息顺延）"),
                21, bondSubsidized ? Matcha : InkSoft, 20f, 134f, 880f, 38f, TextAnchor.MiddleLeft);

            var redeemBtn = Btn(bondBox.transform, "RedeemBondBtn", "提前赎回 ¥" + redeemCost + "（省半息）", 22, 20f, 26f, 446f, 84f, Vermilion, () => {
                int cost = BondEarlyRedeemCost();
                if (coins < cost) { Toast("提前赎回需 ¥" + cost + "，金币不足"); return; }
                BeginUndo("提前赎回债券");
                coins -= cost;
                bondDaysLeft = 0; bondPrincipal = 0; bondDailyDue = 0; bondSubsidized = false; bondsRepaid++;
                EndUndo(); SaveGame(); RefreshHUD(); OpenCompanyScreen();
                Toast("债券已提前全额赎回，信用评分提升");
            });
            TInk(redeemBtn, Color.white);

            var subBtn = Btn(bondBox.transform, "BondSubsidyBtn", bondSubsidized ? "已享 85 折贴息 ✓" : "看片获银行贴息 · 日供减 15%", 22, 486f, 26f, 446f, 84f,
                bondSubsidized ? new Color(0.84f, 0.85f, 0.81f) : new Color(0.76f, 0.50f, 0.16f), () => {
                    if (bondSubsidized) { Toast("当前债券已享 15% 银行贴息"); return; }
                    PromptRewardedAd("bond_sub_" + day + "_" + bondDaysLeft, 1, "银行贴息批文（剩余期数日供 -15%）", () => {
                        bondSubsidized = true;
                        SaveGame(); OpenCompanyScreen();
                        Toast("贴息批文生效：每期降至 ¥" + EffectiveBondDailyPayment());
                    });
                });
            TInk(subBtn, bondSubsidized ? InkSoft : Color.white);
        }
        else
        {
            Txt(bondBox.transform, "「" + CreditTierLabels[tier] + "」授信可发基建债券：¥" + maxBond, 28, Indigo, 20f, 178f, 880f, 44f, TextAnchor.MiddleLeft);
            Txt(bondBox.transform, "期限 10 日 · 每晚扣款 ¥" + stdDaily + "（按期履约可升 AAA 最高 ¥100,000）",
                21, InkSoft, 20f, 126f, 880f, 40f, TextAnchor.MiddleLeft);
            var issueBtn = Btn(bondBox.transform, "IssueCreditBondBtn", "发行 ¥" + maxBond + " 基建债券", 25, 20f, 24f, cW - 40f, 84f, Matcha, () => {
                BeginUndo("发行信用债券");
                coins += maxBond;
                bondPrincipal = maxBond;
                bondDailyDue = stdDaily;
                bondDaysLeft = BondTermDays;
                bondSubsidized = false;
                EndUndo(); SaveGame(); RefreshHUD(); OpenCompanyScreen();
                Toast("已发行 " + CreditTierLabels[tier] + " 债券 +¥" + maxBond);
            });
            TInk(issueBtn, Color.white);
        }

        // 1.2 无息应急周转金
        var loanRow = Rect("LoanRow", finCard.transform, 32f, 28f, cW, 114f);
        Rounded(loanRow, Color.Lerp(CardBg, Ink, 0.04f));
        Txt(loanRow.transform, "无息应急周转金（破产保底）", 25, Ink, 18f, 66f, 620f, 36f, TextAnchor.MiddleLeft);
        Txt(loanRow.transform, loanOutstanding ? "尚欠周转金 ¥500（归还即恢复信用分）" : "金币低于 ¥100 且未盈利时可领 ¥500", 23, InkSoft, 18f, 14f, 636f, 42f, TextAnchor.MiddleLeft);
        var loanBtn = Btn(loanRow.transform, "LoanBtn", loanOutstanding ? "归还 ¥500" : "申请 ¥500", 24, cW - 282f, 21f, 262f, 72f, Indigo, () => {
            if (loanOutstanding)
            {
                if (coins < 500) { Toast("金币不足，暂无法还款"); return; }
                BeginUndo("归还周转金"); coins -= 500; loanOutstanding = false;
            }
            else
            {
                if (coins >= 100 || income > 0) { Toast("需金币低于 ¥100 且日利润未转正方可申请"); return; }
                BeginUndo("领取周转金"); coins += 500; loanOutstanding = true;
            }
            EndUndo(); SaveGame(); RefreshHUD(); OpenCompanyScreen();
            Toast(loanOutstanding ? "已领无息周转金 ¥500" : "周转金已还清，信用分恢复");
        });
        TInk(loanBtn, Color.white);

        // 2. 「市政特急招标合约」
        float y2 = y1 - 22f - 680f;
        var contractCard = Rect("ContractsCard", content, cardX, y2, innerW, 680f);
        Rounded(contractCard, CardBg); SoftShadow(contractCard);
        Txt(contractCard.transform, "市政特急招标合约", 30, Ink, 32f, 612f, 520f, 46f, TextAnchor.MiddleLeft);

        int rc = adWatchCounts.TryGetValue("refresh_cnt_" + day, out int rcVal) ? rcVal : 0;
        var refreshBtn = Btn(contractCard.transform, "RefreshContractsBtn", rc >= 2 ? "今日换标已上限" : "看片重投标书 ↻ (" + rc + "/2)", 22, 700f, 604f, 284f, 58f,
            rc >= 2 ? new Color(0.84f, 0.85f, 0.81f) : new Color(0.76f, 0.50f, 0.16f), () => {
                if (rc >= 2) { Toast("今日换标已达 2 次上限"); return; }
                PromptRewardedAd("refresh_cnt_" + day, 1, "重投 2 份市政招标书", () => {
                    activeContracts.Clear();
                    nextContractId += 2;
                    EnsureCompanyInitialized();
                    SaveGame(); OpenCompanyScreen();
                    Toast("已换取两份新市政标书");
                });
            });
        TInk(refreshBtn, rc >= 2 ? InkSoft : Color.white);

        for (int i = 0; i < activeContracts.Count && i < 2; i++)
        {
            var c = activeContracts[i];
            float cy = i == 0 ? 318f : 26f;
            bool ready = ContractIsReady(c);
            var box = Rect("ContractBox_" + i, contractCard.transform, 32f, cy, cW, 268f);
            Rounded(box, ready ? Color.Lerp(CardBg, Matcha, 0.12f) : Color.Lerp(CardBg, Ink, 0.04f));

            Txt(box.transform, ContractTitle(c), 26, Ink, 20f, 212f, 580f, 40f, TextAnchor.MiddleLeft);
            int daysRem = Mathf.Max(0, c.deadlineDay - day);
            Txt(box.transform, ready ? "✓ 待验收" : "剩 " + daysRem + " 天截止", 23, ready ? Matcha : Vermilion, 616f, 214f, 316f, 38f, TextAnchor.MiddleRight);
            Txt(box.transform, ContractDesc(c), 23, InkSoft, 20f, 162f, 900f, 40f, TextAnchor.MiddleLeft);

            float ratio = c.targetValue > 0 ? Mathf.Clamp01(ContractCurrentValue(c) / (float)c.targetValue) : 1f;
            Img(Rect("CTrack", box.transform, 20f, 134f, 912f, 14f), new Color(0.86f, 0.84f, 0.78f), spriteRound).raycastTarget = false;
            if (ratio > 0f) Img(Rect("CFill", box.transform, 20f, 134f, 912f * ratio, 14f), ready ? Matcha : Indigo, spriteRound).raycastTarget = false;

            Txt(box.transform, "中标赏金：+¥" + c.rewardCoins + " · +" + c.rewardRp + " RP · +" + c.rewardDiamonds + " 钻",
                23, Indigo, 20f, 88f, 900f, 38f, TextAnchor.MiddleLeft);

            if (ready)
            {
                var claimBtn = Btn(box.transform, "ClaimContract_" + c.id, "验收领奖", 24, 20f, 16f, 446f, 64f, Matcha, () => {
                    if (c.claimed) return;
                    c.claimed = true;
                    coins += c.rewardCoins; rp += c.rewardRp; diamonds += c.rewardDiamonds;
                    activeContracts.Remove(c);
                    EnsureCompanyInitialized();
                    SaveGame(); RefreshHUD(); OpenCompanyScreen();
                    Toast("中标赏金到账 +¥" + c.rewardCoins);
                });
                TInk(claimBtn, Color.white);

                int xCoins = Mathf.RoundToInt(c.rewardCoins * 1.5f);
                int xRp = Mathf.RoundToInt(c.rewardRp * 1.5f);
                int xGems = Mathf.RoundToInt(c.rewardDiamonds * 1.5f);
                var vipBtn = Btn(box.transform, "ClaimContractVip_" + c.id, "看片专访 · ×1.5 领 ¥" + xCoins, 23, 486f, 16f, 446f, 64f, new Color(0.76f, 0.50f, 0.16f), () => {
                    PromptRewardedAd("contract_vip_" + c.id, 1, "媒体头版专访（赏金/RP/钻石全额 ×1.5）", () => {
                        if (c.claimed) return;
                        c.claimed = true;
                        coins += xCoins; rp += xRp; diamonds += xGems;
                        activeContracts.Remove(c);
                        EnsureCompanyInitialized();
                        SaveGame(); RefreshHUD(); OpenCompanyScreen();
                        Toast("专访赏金到账 +¥" + xCoins + " · +" + xGems + "钻");
                    });
                });
                TInk(vipBtn, Color.white);
            }
            else
            {
                var todoBtn = Btn(box.transform, "ContractTodo_" + c.id, "工程推进中（达标后回来验收）", 23, 20f, 16f, 912f, 64f, new Color(0.86f, 0.85f, 0.80f), () => Toast("达成标书目标后即可在此验收领奖"));
                TInk(todoBtn, InkSoft);
            }
        }
    }

    void BuildCompanyPrivilegeTab(Transform page, float cardW)
    {
        float totalH = 1210f;
        var content = FleetScroll(page, 36f, PgH - 270f, totalH, scrollKey: "company:privileges");
        float topY = content.sizeDelta.y;
        float cardX = 4f, innerW = cardW - 8f, cW = innerW - 64f;

        // 1. 效率特权一：AI 智能调度总控台 + 维保 -10%
        float y1 = topY - 12f - 246f;
        var dispCard = Rect("AutoDispatchPrivCard", content, cardX, y1, innerW, 246f);
        Rounded(dispCard, CardBg); SoftShadow(dispCard);
        bool hasAuto = autoDispatchDaysLeft > 0;
        Txt(dispCard.transform, "AI 智能调度总控台 · 一键黄金配班", 29, Ink, 32f, 184f, 640f, 42f, TextAnchor.MiddleLeft);
        Txt(dispCard.transform, hasAuto ? "剩余 " + autoDispatchDaysLeft + " 天" : "未激活", 22, hasAuto ? Matcha : Vermilion, 694f, 186f, 290f, 38f, TextAnchor.MiddleRight);
        Txt(dispCard.transform, "依据全网真实客流一键优化四时段配舰，消灭空跑亏钱，并享全网维护费 -10%。",
            23, InkSoft, 32f, 112f, cW, 60f, TextAnchor.UpperLeft);

        if (hasAuto)
        {
            var runBtn = Btn(dispCard.transform, "RunAutoDispatch", "立即一键优化全线配班", 24, 32f, 22f, 610f, 76f, Matcha, () => {
                BeginUndo("AI一键黄金配班");
                int n = OptimizeAllLinesDispatch();
                EndUndo(); RefreshHUD();
                Toast(n > 0 ? "AI 已重新平衡 " + n + " 条航线配班，预期日利润 ¥" + Mathf.RoundToInt(income) : "当前全线已处于最优配班");
            });
            TInk(runBtn, Color.white);

            var extBtn = Btn(dispCard.transform, "ExtendAutoDispatch", "观影续签 +4 天", 22, 662f, 22f, 322f, 76f, Indigo, () => {
                PromptRewardedAd("priv_autodisp_" + day + "_" + autoDispatchDaysLeft, 1, "延长 AI 调度总控台 +4 天", () => {
                    autoDispatchDaysLeft += 4;
                    SaveGame(); OpenCompanyScreen();
                    Toast("AI 智能调度已续期至 " + autoDispatchDaysLeft + " 天");
                });
            });
            TInk(extBtn, Color.white);
        }
        else
        {
            var actBtn = Btn(dispCard.transform, "ActivateAutoDispatch", "观影签批 4 天特权（一键优化 + 全网维保 -10%）", 23, 32f, 22f, cW, 76f, new Color(0.76f, 0.50f, 0.16f), () => {
                PromptRewardedAd("priv_autodisp_" + day, 1, "AI 智能调度总控台（4天 · 维保-10%）", () => {
                    autoDispatchDaysLeft += 4;
                    int n = OptimizeAllLinesDispatch();
                    SaveGame(); RefreshHUD(); OpenCompanyScreen();
                    Toast("AI 总控台已激活 4 天，已优化 " + n + " 条航线");
                });
            });
            TInk(actBtn, Color.white);
        }

        // 2. 效率特权二：4× 超极速推演 + 科研加速 +50%
        float y2 = y1 - 22f - 226f;
        var turboCard = Rect("TurboPrivCard", content, cardX, y2, innerW, 226f);
        Rounded(turboCard, CardBg); SoftShadow(turboCard);
        bool hasTurbo = turboDaysLeft > 0;
        Txt(turboCard.transform, "4× 极速推演牌照 · 科研加速 +50%", 29, Ink, 32f, 164f, 650f, 42f, TextAnchor.MiddleLeft);
        Txt(turboCard.transform, hasTurbo ? "剩余 " + turboDaysLeft + " 天" : "未激活", 22, hasTurbo ? Matcha : Vermilion, 694f, 166f, 290f, 38f, TextAnchor.MiddleRight);
        Txt(turboCard.transform, "开放顶栏「4× 极速」运行档位，并使科研点 RP 生成速率提升 50%。",
            21, InkSoft, 32f, 108f, cW, 44f, TextAnchor.MiddleLeft);

        var turboBtn = Btn(turboCard.transform, "TurboBtn",
            hasTurbo ? "观影续签 +4 天（顶栏 4× 档位已就绪）" : "观影签批 4 天极速牌照（4× 倍速 + 科研+50%）",
            23, 32f, 22f, cW, 72f, hasTurbo ? Indigo : new Color(0.76f, 0.50f, 0.16f), () => {
                PromptRewardedAd("priv_turbo_" + day + "_" + turboDaysLeft, 1, "4× 极速推演牌照（科研+50% · 4天）", () => {
                    turboDaysLeft += 4;
                    timeSpeed = 4;
                    UpdateTimeText();
                    SaveGame(); OpenCompanyScreen();
                    Toast("4× 极速推演已开启（剩 " + turboDaysLeft + " 天）");
                });
            });
        TInk(turboBtn, Color.white);

        // 3. 财政补贴与传奇外援双卡（并排）
        float y3 = y2 - 22f - 246f;
        float halfW = (innerW - 20f) * 0.5f;

        var subCard = Rect("SubsidyPrivCard", content, cardX, y3, halfW, 246f);
        Rounded(subCard, CardBg); SoftShadow(subCard);
        int grant = AdTourismSubsidyAmount();
        bool adClaimed = lastAdSubsidyDay == day;
        Txt(subCard.transform, "每日文旅财政补贴", 26, Ink, 26f, 186f, halfW - 52f, 38f, TextAnchor.MiddleLeft);
        var subBody = Txt(subCard.transform, "基础¥500+连通" + DailyConnectedCount() + "港×¥60（封顶¥5,000）\n今日可领 ¥" + grant, 21, InkSoft, 26f, 92f, halfW - 52f, 88f, TextAnchor.UpperLeft);
        subBody.resizeTextForBestFit = true; subBody.resizeTextMinSize = 17; subBody.resizeTextMaxSize = 21;
        var subBtn = Btn(subCard.transform, "SubsidyQuickBtn", adClaimed ? "今日专款已领 ✓" : "观影申领 ¥" + grant, 24, 26f, 18f, halfW - 52f, 64f,
            adClaimed ? new Color(0.84f, 0.85f, 0.81f) : Matcha, () => {
                if (lastAdSubsidyDay == day) { Toast("今日补贴已领，明日再来"); return; }
                PromptRewardedAd("company_subsidy_" + day, 1, "文旅专项财政补贴 ¥" + grant, () => {
                    if (lastAdSubsidyDay == day) return;
                    coins += grant; lastAdSubsidyDay = day;
                    SaveGame(); RefreshHUD(); OpenCompanyScreen();
                    Toast("文旅专项补贴到账 +¥" + grant);
                });
            });
        TInk(subBtn, adClaimed ? InkSoft : Color.white);

        var huoCard = Rect("HuoPrivCard", content, cardX + halfW + 20f, y3, halfW, 246f);
        Rounded(huoCard, CardBg); SoftShadow(huoCard);
        bool hasHuo = consultantDaysLeft > 0 && consultantId == 6;
        Txt(huoCard.transform, "特聘传奇 · 霍老首席", 26, Ink, 26f, 186f, halfW - 52f, 38f, TextAnchor.MiddleLeft);
        var huoBody = Txt(huoCard.transform, "独立第 4 高管席：客流+10% · 车速+8%\n建线-10% · 维保-10%（零负面特质）", 21, InkSoft, 26f, 92f, halfW - 52f, 88f, TextAnchor.UpperLeft);
        huoBody.resizeTextForBestFit = true; huoBody.resizeTextMinSize = 17; huoBody.resizeTextMaxSize = 21;
        var huoBtn = Btn(huoCard.transform, "HuoQuickBtn", hasHuo ? "已在岗(剩" + consultantDaysLeft + "天)续聘" : "观影礼聘 3 天", 23, 26f, 18f, halfW - 52f, 64f,
            new Color(0.76f, 0.50f, 0.16f), () => {
                PromptRewardedAd("hire_huo_" + day, 1, "特聘传奇顾问·霍老首席（3日全能零负面）", () => {
                    consultantId = 6; consultantDaysLeft += 3;
                    hasForecastSignature = false; operatingRates.Clear(); RecalcIncome();
                    SaveGame(); OpenCompanyScreen();
                    Toast("霍老首席已赴第 4 特聘席");
                });
            });
        TInk(huoBtn, Color.white);

        // 4. 外观定制工坊直通航
        float y4 = y3 - 22f - 398f;
        var lookCard = Rect("LookPrivCard", content, cardX, y4, innerW, 398f);
        Rounded(lookCard, CardBg); SoftShadow(lookCard);
        Txt(lookCard.transform, "外观定制工坊 · 企业徽记与线网涂装", 28, Ink, 32f, 336f, 920f, 42f, TextAnchor.MiddleLeft);
        Txt(lookCard.transform, "共轨共轨双线支持双色涂装并排呈现；典藏星徽、线网涂装与手账主题均支持星图映像馆解锁。",
            23, InkSoft, 32f, 274f, cW, 52f, TextAnchor.MiddleLeft);

        float halfLookW = (cW - 20f) * 0.5f;
        for (int s = 6; s <= 7; s++)
        {
            int sealIndex = s;
            bool owned = IsSealUnlocked(sealIndex);
            float sx = 32f + (s - 6) * (halfLookW + 20f);
            Color scBg = Color.Lerp(CardBg, new Color(0.76f, 0.50f, 0.16f), 0.12f);
            var sBtn = Btn(lookCard.transform, "SealVipBtn_" + s, "", 22, sx, 130f, halfLookW, 124f, scBg, () => {
                if (owned)
                {
                    companySeal = sealIndex; SaveGame(); companyTab = 0; OpenCompanyScreen();
                    Toast("已换用徽章「" + CompanySealNames[sealIndex] + "」");
                    return;
                }
                PromptRewardedAd("seal_" + sealIndex, 2, "典藏星徽「" + CompanySealNames[sealIndex] + "」", () => {
                    companySeal = sealIndex; SaveGame(); OpenCompanyScreen();
                    Toast("典藏星徽「" + CompanySealNames[sealIndex] + "」已解锁并佩戴");
                }, () => {
                    OpenCompanyScreen();
                    Toast("「" + CompanySealNames[sealIndex] + "」观影进度 1/2");
                });
            });
            var stamp = Rect("Stamp", sBtn.transform, 16f, 18f, 88f, 88f);
            Rounded(stamp, new Color(0.76f, 0.50f, 0.16f));
            Txt(stamp.transform, CompanySealChars[sealIndex], 46, Color.white, 0f, 0f, 88f, 88f, TextAnchor.MiddleCenter).raycastTarget = false;
            Txt(sBtn.transform, CompanySealNames[sealIndex] + (companySeal == sealIndex ? " ✓" : ""), 25, Ink, 122f, 68f, halfLookW - 140f, 38f, TextAnchor.MiddleLeft).raycastTarget = false;
            Txt(sBtn.transform, owned ? "已永久解锁 · 点此佩戴" : SealUnlockHint(sealIndex), 22, owned ? Matcha : Vermilion, 122f, 20f, halfLookW - 140f, 40f, TextAnchor.MiddleLeft).raycastTarget = false;
        }

        var liveryBtn = Btn(lookCard.transform, "GoLiveryShop", "进入「涂装工坊」（15 款双色共轨双线涂装）", 23, 32f, 24f, halfLookW, 82f, Indigo, () => {
            ClosePage();
            fleetBack = () => { companyTab = 3; OpenCompanyScreen(); };
            shopCategory = 1;
            OpenFleetTab(2);
        });
        TInk(liveryBtn, Color.white);

        var themeBtn = Btn(lookCard.transform, "GoThemeShop", "进入「主题工坊」（" + Themes.Length + " 套全局风格）", 23, 32f + halfLookW + 20f, 24f, halfLookW, 82f, Vermilion, () => {
            ClosePage();
            fleetBack = () => { companyTab = 3; OpenCompanyScreen(); };
            shopCategory = 2;
            OpenFleetTab(2);
        });
        TInk(themeBtn, Color.white);
    }

    void OpenAnnualAwardPopup()
    {
        int awardCycle = (day / 15) * 15;
        bool canClaim = day >= 15 && lastAwardDay < awardCycle;
        ComputeAnnualAward(out string medalName, out string reason, out int grantCoins, out int grantDiamonds);

        var p = OpenPopup("年度星轨大赏", 760f, 920f);
        string posterTitle = awardCycle >= 15 ? ("DAY" + awardCycle) : "AWARD";
        int awardPalette = (awardCycle / 15) % 4;
        var awardTex = EraPosterGenerator.GetCustomPoster("annual_" + posterTitle, posterTitle, awardPalette, 3, 768, 248, 0);
        var posterFrame = CreateNeonPosterFrame(p.transform, "AwardNeonPoster", 36f, 396f, 848f, 258f, awardTex, 8f);
        var cornerTag = Rect("AwardCyclePill", posterFrame.transform, 578f, 16f, 252f, 38f);
        Rounded(cornerTag, new Color(0.04f, 0.04f, 0.08f, 0.78f));
        cornerTag.GetComponent<Image>().raycastTarget = false;
        Txt(cornerTag.transform, awardCycle >= 15 ? ("第 " + awardCycle + " 日纪念刊") : "首届大赏筹备", 21, new Color(1f, 0.95f, 0.86f, 0.96f), 0f, 0f, 252f, 38f, TextAnchor.MiddleCenter).raycastTarget = false;

        var banner = Rect("AwardBanner", p.transform, 36f, 286f, 848f, 96f);
        Rounded(banner, Color.Lerp(CardBg, new Color(0.95f, 0.72f, 0.20f), 0.16f));
        Img(Rect("Accent", banner.transform, 0, 12, 8, 72), new Color(0.86f, 0.56f, 0.12f), spriteRound).raycastTarget = false;
        Txt(banner.transform, "本期拟授勋：" + medalName, 30, Ink, 24f, 48f, 800f, 40f, TextAnchor.MiddleLeft);
        Txt(banner.transform, reason, 23, InkSoft, 24f, 8f, 800f, 38f, TextAnchor.MiddleLeft);

        Txt(p.transform, "评比贺礼：+¥" + grantCoins + "   ·   +" + grantDiamonds + " 钻", 25, Indigo, 36f, 230f, 848f, 40f, TextAnchor.MiddleCenter);
        Txt(p.transform, canClaim
            ? "第 " + awardCycle + " 日大赏名单已揭晓，可领发展金或接受专访翻倍。"
            : (day < 15 ? "第 15 日正式开启首次授勋（当前第 " + day + " 日）。" : "第 " + awardCycle + " 日贺礼已发，下届第 " + (awardCycle + 15) + " 日再会。"),
            22, InkSoft, 36f, 140f, 848f, 80f, TextAnchor.UpperCenter);

        if (canClaim)
        {
            var normal = Btn(p.transform, "ClaimAwardNormal", "直接领赏 +¥" + grantCoins, 24, 36f, 38f, 408f, 86f, Matcha, () => {
                if (lastAwardDay >= awardCycle) return;
                lastAwardDay = awardCycle;
                coins += grantCoins; diamonds += grantDiamonds;
                ClosePanel(true); SaveGame(); RefreshHUD(); OpenCompanyScreen();
                Toast("获颁「" + medalName + "」+¥" + grantCoins + " · +" + grantDiamonds + "钻");
            });
            TInk(normal, Color.white);

            var vip = Btn(p.transform, "ClaimAwardVip", "看片专访 · 双倍 +¥" + (grantCoins * 2), 24, 476f, 38f, 408f, 86f, new Color(0.76f, 0.50f, 0.16f), () => {
                PromptRewardedAd("annual_award_" + awardCycle, 1, "星轨大赏头版专访（赏金与钻石双倍）", () => {
                    if (lastAwardDay >= awardCycle) return;
                    lastAwardDay = awardCycle;
                    coins += grantCoins * 2; diamonds += grantDiamonds * 2;
                    ClosePanel(true); SaveGame(); RefreshHUD(); OpenCompanyScreen();
                    Toast("专访贺礼到账 +¥" + (grantCoins * 2) + " · +" + (grantDiamonds * 2) + "钻");
                });
            });
            TInk(vip, Color.white);
        }
        else
        {
            var ok = Btn(p.transform, "CloseAward", "返回公司总览", 25, 36f, 38f, 848f, 86f, Indigo, ClosePanel);
            TInk(ok, Color.white);
        }
    }

    void OpenCompanySealPicker()
    {
        var panel = OpenPopup("选择公司徽章", 720f, 960f, "company_seal_picker");
        Txt(panel.transform, "随公司成就或激励视频解锁 8 枚专属徽章，点选立即生效", 23, InkSoft, 34f, 616f, 892f, 38f, TextAnchor.MiddleLeft);
        for (int i = 0; i < CompanySealChars.Length; i++)
        {
            int idx = i;
            int col = i % 4;
            int row = i / 4;
            float x = 34f + col * 226f;
            float y = row == 0 ? 336f : 64f;
            bool unlocked = IsSealUnlocked(idx);
            bool selected = companySeal == idx;
            bool isVip = idx >= 6;
            Color sealColor = unlocked ? (isVip ? new Color(0.76f, 0.50f, 0.16f) : Vermilion) : new Color(0.65f, 0.63f, 0.59f);
            Color cardColor = selected ? Color.Lerp(CardBg, sealColor, 0.16f) : (unlocked ? CardBg : Color.Lerp(CardBg, Ink, 0.08f));
            var btn = Btn(panel.transform, "SealChoice_" + idx, "", 20, x, y, 212f, 248f, cardColor, () => {
                if (!unlocked)
                {
                    if (isVip)
                    {
                        PromptRewardedAd("seal_" + idx, 2, "典藏星徽「" + CompanySealNames[idx] + "」", () => {
                            companySeal = idx; SaveGame(); ClosePanel(true); OpenCompanyScreen();
                            Toast("已永久解锁并佩戴「" + CompanySealNames[idx] + "」");
                        }, () => {
                            OpenCompanySealPicker();
                            Toast("「" + CompanySealNames[idx] + "」观影进度 1/2，再看 1 次即可解锁！");
                        });
                    }
                    else Toast("未解锁：" + SealUnlockHint(idx));
                    return;
                }
                companySeal = idx;
                SaveGame();
                ClosePanel(true);
                OpenCompanyScreen();
                Toast("已换用徽章「" + CompanySealNames[idx] + "」");
            });
            var stamp = Rect("Stamp", btn.transform, 52f, 114f, 108f, 108f);
            Rounded(stamp, sealColor);
            Txt(stamp.transform, CompanySealChars[idx], 56, Color.white, 0f, 0f, 108f, 108f, TextAnchor.MiddleCenter).raycastTarget = false;
            Txt(btn.transform, CompanySealNames[idx] + (selected ? " ✓" : ""), 24, unlocked ? Ink : InkSoft, 8f, 62f, 196f, 38f, TextAnchor.MiddleCenter).raycastTarget = false;
            var sealSub = Txt(btn.transform, unlocked ? (selected ? "当前徽章" : "点击选用") : SealUnlockHint(idx), 21, unlocked ? (selected ? sealColor : Matcha) : (isVip ? new Color(0.76f, 0.50f, 0.16f) : InkSoft), 8f, 12f, 196f, 44f, TextAnchor.MiddleCenter);
            sealSub.resizeTextForBestFit = true; sealSub.resizeTextMinSize = 16; sealSub.resizeTextMaxSize = ScaledFontSize(21); sealSub.raycastTarget = false;
            sealSub.horizontalOverflow = HorizontalWrapMode.Overflow; sealSub.verticalOverflow = VerticalWrapMode.Truncate;
        }
        string activeInscription = GetCompanySealInscription(companySeal);
        if (!string.IsNullOrEmpty(activeInscription))
        {
            Txt(panel.transform, "「" + CompanySealNames[Mathf.Clamp(companySeal, 0, CompanySealNames.Length - 1)] + "」铭文：" + activeInscription,
                20, InkSoft, 36f, 14f, 892f, 44f, TextAnchor.MiddleLeft).raycastTarget = false;
        }
    }

    // ---------- 星图 ----------
    int sceneryCategory;
    int sceneryRegionFilter;
    static readonly string[] SceneryCategories = { "全部名胜", "自然景观", "人文景观" };
    static readonly string[] SceneryRegions = { "全部", "月之亮面", "月背", "火星·西半球", "火星·东半球" };
    void OpenSceneryScreen()
    {
        SyncCulturalCollection(false);
        var pg = OpenPage("星图鉴", "星舰通达之处皆入博物手帐：囊括沿途名胜、星航纪念票、荣誉星牌与星域二十四候。", "PageArt/album_watercolor", "scenery_album");
        StampBotanicalSprig(pg.transform, "orn_kapok_bamboo", W - 164f, PgH - 320f, 152f, 152f, 0.16f, true, false);
        StampBotanicalSprig(pg.transform, "orn_bauhinia_lotus", 8f, 6f, 176f, 176f, 0.14f, false, true);

        int foundTotal = 0;
        for (int i = 0; i < sceneries.Count; i++) if (sceneries[i].found) foundTotal++;
        int readyPlaques = CountClaimablePlaques();
        string[] shelfLabels = {
            "名胜风光 " + foundTotal + "/" + sceneries.Count,
            "纪念票册 " + earnedMemorialTickets.Count + "/6",
            "荣誉星牌 " + earnedPlaques.Count + "/4",
            "二十四候 · " + CurrentSolarTerm().name
        };
        float tabW = 244f, tabGap = 16f;
        for (int i = 0; i < shelfLabels.Length; i++)
        {
            int t = i;
            bool active = sceneryCollectionTab == t;
            Color bg = active ? Indigo : new Color(0.95f, 0.92f, 0.85f, 0.98f);
            var tabBtn = Btn(pg.transform, "AlbumShelfTab_" + i, shelfLabels[i], 23, 28f + i * (tabW + tabGap), PgH - 252f, tabW, 72f, bg, () => {
                if (sceneryCollectionTab != t) { sceneryCollectionTab = t; OpenSceneryScreen(); }
            });
            TInk(tabBtn, active ? Color.white : Ink);
            if ((i == 0 && pendingCheckin.Count > 0) || (i == 2 && readyPlaques > 0))
            {
                var redDot = Rect("TabBadge", tabBtn.transform, tabW - 28f, 44f, 18f, 18f);
                Img(redDot, Vermilion, spriteCircle).raycastTarget = false;
            }
        }
        StampFloralDivider(pg.transform, W * 0.5f, PgH - 294f, 680f, 36f, 0.72f);

        if (sceneryCollectionTab == 1) { BuildSceneryTicketsTab(pg.transform); return; }
        if (sceneryCollectionTab == 2) { BuildSceneryPlaquesTab(pg.transform); return; }
        if (sceneryCollectionTab == 3) { BuildScenerySolarTab(pg.transform); return; }

        Txt(pg.transform, "沿途记忆", 34, Ink, 34, PgH - 344, 360, 44, TextAnchor.MiddleLeft);
        int regionTotal = 0, regionFound = 0;
        foreach (var item in sceneries) if (sceneryRegionFilter == 0 || item.region == SceneryRegions[sceneryRegionFilter]) { regionTotal++; if (item.found) regionFound++; }
        Txt(pg.transform, "已收录 " + regionFound + " / " + regionTotal, 22, InkSoft, 36, PgH - 376, 420, 30, TextAnchor.MiddleLeft);
        if (regionTotal > 0 && regionFound == regionTotal)
        {
            var clearTex = EraPosterGenerator.GetCustomPoster("scenery_clear_" + sceneryRegionFilter, "全览", sceneryRegionFilter % 4, 5, 512, 184, 0);
            var clearSeal = CreateNeonPosterFrame(pg.transform, "SceneryClearSeal", 456f, PgH - 368f, 200f, 70f, clearTex, 4f);
            clearSeal.transform.localRotation = Quaternion.Euler(0, 0, -3.5f);
        }
        Button filter = null;
        filter = Btn(pg.transform, "SceneryFilter", SceneryCategories[sceneryCategory] + "  ▾", 24, 694, PgH - 366, 330, 64,
            new Color(0.91f,0.88f,0.80f,0.98f), () => ShowFleetDropdown(pg.transform, filter.transform, SceneryCategories, sceneryCategory, value => { if (sceneryCategory != value) { sceneryCategory = value; ResetScrollPosition("scenery:places"); } OpenSceneryScreen(); }));
        TInk(filter, Ink); if (!filter.GetComponent<FleetPressFeedback>()) filter.gameObject.AddComponent<FleetPressFeedback>();
        for (int i = 0; i < SceneryRegions.Length; i++) {
            int region = i;
            var tab = Btn(pg.transform, "SceneryRegion" + i, SceneryRegions[i], 23, 28 + i * 258, PgH - 440, 246, 58,
                i == sceneryRegionFilter ? new Color(0.75f,0.82f,0.68f,0.98f) : new Color(0.96f,0.94f,0.88f,0.98f),
                () => { if (sceneryRegionFilter != region) { sceneryRegionFilter = region; ResetScrollPosition("scenery:places"); } OpenSceneryScreen(); });
            TInk(tab, Ink);
        }
        int count = 0;
        foreach (var item in sceneries)
            if ((sceneryRegionFilter == 0 || item.region == SceneryRegions[sceneryRegionFilter]) &&
                (sceneryCategory == 0 || (sceneryCategory == 1 && item.natural) || (sceneryCategory == 2 && !item.natural))) count++;
        var content = FleetScroll(pg.transform, 72, PgH - 456, Mathf.CeilToInt(count / 2f) * 440f + 20, scrollKey: "scenery:places");
        float contentHeight = content.sizeDelta.y;
        int visible = 0;
        if (count == 0) Txt(content, "这里还没有旅行记忆\n换个分类看看吧", 30, InkSoft, 40, contentHeight - 240, W - 136, 150, TextAnchor.MiddleCenter);
        for (int i = 0; i < sceneries.Count; i++)
        {
            var sc = sceneries[i];
            if (sceneryRegionFilter != 0 && sc.region != SceneryRegions[sceneryRegionFilter]) continue;
            if (sceneryCategory == 1 && !sc.natural || sceneryCategory == 2 && sc.natural) continue;
            bool pending = pendingCheckin.Contains(sc);
            bool hasPicture = TryGetSceneryContent(sc, out string picturePath, out _);
            var card = Rect("EC"+i, content, (visible%2)*516, contentHeight - 420 - (visible/2)*440, 500, 410);
            visible++;
            Rounded(card, new Color(1f,0.985f,0.95f,0.97f)); SoftShadow(card);
            StampFineRuleFrame(card.transform, 500f, 410f, 8f, new Color(InkSoft.r, InkSoft.g, InkSoft.b, 0.14f));
            string scOrn = (sc.region == "月之亮面" || sc.region == "月背") ? "orn_bauhinia_lotus" : "orn_kapok_bamboo";
            StampBotanicalSprig(card.transform, scOrn, 412f, 8f, 76f, 76f, sc.found ? 0.18f : 0.10f, true, true);
            var photo = Rect("PostcardPhoto", card.transform, 20, 168, 460, 218);
            var photoBg = Img(photo, new Color(0.88f,0.91f,0.84f,1f), spriteRound); photoBg.type = Image.Type.Sliced; photoBg.raycastTarget = false;
            if (sc.found && hasPicture) {
                var image = Rect("CheckedInPhoto", photo.transform, 6, 6, 448, 206).AddComponent<RawImage>();
                image.color = Color.clear; image.raycastTarget = false;
                var lazy = image.gameObject.AddComponent<JournalLazyPhoto>(); lazy.image = image;
                lazy.viewport = content.parent as RectTransform; lazy.resourcePath = picturePath;
            } else {
                var wash = Rect("Watercolor", photo.transform, 6, 6, 448, 206).AddComponent<RawImage>();
                var washTex = Resources.Load<Texture2D>("PageArt/album_watercolor");
                bool washFallback = !washTex;
                if (washFallback) washTex = Resources.Load<Texture2D>("MapArt/moon_nearside");
                wash.texture = washTex;
                wash.color = washFallback ? new Color(1,1,1,0.14f) : new Color(1,1,1,sc.unlocked ? 0.38f : 0.18f); wash.raycastTarget = false;
                var icon = Rect("Illustration", photo.transform, 170, 53, 120, 112).AddComponent<RawImage>();
                var iconTex = Resources.Load<Texture2D>(sc.natural ? "MapIcons/scenery_nature" : "MapIcons/scenery_culture");
                if (iconTex)
                {
                    icon.texture = iconTex;
                    icon.color = sc.unlocked ? Color.white : new Color(0.62f,0.68f,0.58f,0.72f); icon.raycastTarget = false;
                }
                else
                {
                    // 插画图标已移除：以 spriteCircle 色块徽章回退，避免卡片留白框
                    icon.gameObject.SetActive(false);
                    Img(Rect("CategoryBadge", photo.transform, 170, 53, 120, 112),
                        sc.natural ? new Color(0.45f, 0.68f, 0.50f, 0.85f) : new Color(0.66f, 0.55f, 0.72f, 0.85f),
                        spriteCircle).raycastTarget = false;
                }
                Txt(photo.transform, sc.unlocked ? "沿途风景" : "待收集", 23, InkSoft, 90, 16, 280, 34, TextAnchor.MiddleCenter);
            }
            var tape = Rect("Tape", card.transform, 190, 379, 120, 25); Img(tape, new Color(0.82f,0.73f,0.54f,0.55f), spriteRound).raycastTarget = false;
            tape.transform.localRotation = Quaternion.Euler(0, 0, (i % 2 == 0) ? -4 : 4);
            Txt(card.transform, sc.name, 34, Ink, 24, 115, 330, 44, TextAnchor.MiddleLeft);
            Txt(card.transform, sc.region + (sc.natural ? "  ·  自然" : "  ·  人文"), 22, InkSoft, 26, 79, 310, 30, TextAnchor.MiddleLeft);
            string status = sc.found ? "已打卡" : !RegionUnlocked(sc.region) ? "先解锁" + sc.region : !sc.unlocked ? "连通" + sc.triggerStation + "港" : hasPicture ? "点击打卡" : "图片筹备中";
            Txt(card.transform, status, 22, sc.found ? Matcha : pending ? Vermilion : InkSoft, 24, 28, 440, 35, TextAnchor.MiddleLeft);
            var stamp = Rect("Stamp", card.transform, 354, 86, 118, 64); Rounded(stamp, sc.found ? new Color(0.72f,0.82f,0.67f,0.65f) : pending ? new Color(0.96f,0.78f,0.72f,0.75f) : new Color(0.89f,0.87f,0.81f,0.85f));
            stamp.GetComponent<Image>().raycastTarget = false; stamp.transform.localRotation = Quaternion.Euler(0,0,7);
            Txt(stamp.transform, sc.found ? "已到访" : pending ? "待打卡" : "未到访", 23, sc.found ? Matcha : pending ? Vermilion : InkSoft, 0, 0, 118, 64, TextAnchor.MiddleCenter);
            if (sc.unlocked && hasPicture)
            {
                var scLocal = sc;
                var btn = card.AddComponent<Button>(); btn.targetGraphic = card.GetComponent<Image>();
                btn.onClick.AddListener(() => OpenSceneryViewer(scLocal, () => { ClosePage(); OpenSceneryScreen(); }));
            }
            { var entry = card.AddComponent<FleetCardEntrance>(); entry.delay = Mathf.Min(visible - 1, 5) * 0.025f; }
        }
    }

    void OpenDiamondExchangePopup()
    {
        var popup = OpenPopup("星钻补给 · 兑换中心", 760, 880, "diamond_exchange");
        int gems = Mathf.FloorToInt(diamonds);
        float pw = 880f, pad = 36f, cw = pw - pad * 2f;

        // 顶部当前资产汇总条
        var wallet = Rect("WalletCard", popup.transform, pad, 536f, cw, 92f);
        Rounded(wallet, Paper2);
        var walletLbl = Txt(wallet.transform,
            $"当前星钻 {gems}  ·  科研点 {Mathf.FloorToInt(rp)} RP  ·  资金 ¥{FormatCoins(coins)}",
            28, Ink, 24f, 14f, cw - 48f, 64f, TextAnchor.MiddleCenter);
        FitSingleLine(walletLbl, 28);

        void TryExchangeRp(int amount)
        {
            int curGems = Mathf.FloorToInt(diamonds);
            if (amount <= 0 || curGems < amount)
            {
                PlayErrorSfx();
                Toast(amount <= 0 ? "当前暂无可兑换的星钻" : $"星钻不足（需要 {amount}，当前 {curGems}）");
                return;
            }
            BeginUndo("星钻购买科研点");
            diamonds -= amount;
            rp += amount;
            RecalcIncome(); EndUndo(); RefreshHUD();
            PlayCoinSfx();
            Toast($"已消耗 {amount} 星钻，获得 {amount} RP");
            OpenDiamondExchangePopup();
        }

        void TryExchangeCoins(int diamondCost)
        {
            int curGems = Mathf.FloorToInt(diamonds);
            if (diamondCost <= 0 || curGems < diamondCost)
            {
                PlayErrorSfx();
                Toast(diamondCost <= 0 ? "当前暂无可兑换的星钻" : $"星钻不足（需要 {diamondCost}，当前 {curGems}）");
                return;
            }
            float gain = diamondCost * 10000f;
            BeginUndo("星钻兑换资金");
            diamonds -= diamondCost;
            coins += gain;
            RecalcIncome(); EndUndo(); RefreshHUD();
            PlayCoinSfx();
            Toast($"已消耗 {diamondCost} 星钻，兑换资金 ¥{FormatCoins(gain)}");
            OpenDiamondExchangePopup();
        }

        // 区块一：1:1 购买 RP 科研点
        var rpCard = Rect("RpExchangeCard", popup.transform, pad, 316f, cw, 198f);
        Rounded(rpCard, CardBg);
        var rpTitle = Txt(rpCard.transform, "科研点补给（1 星钻 = 1 RP）", 28, Indigo, 24f, 134f, cw - 48f, 44f, TextAnchor.MiddleLeft);
        FitSingleLine(rpTitle, 28);
        var rpSub = Txt(rpCard.transform, "直接注入星舰与公司治理研发，加速解锁跨时代科技。", 22, InkSoft, 24f, 98f, cw - 48f, 34f, TextAnchor.MiddleLeft);
        FitSingleLine(rpSub, 22);

        int[] rpAmounts = { 10, 50, 100, Mathf.Max(1, gems) };
        string[] rpLabels = { "+10 RP\n10 钻", "+50 RP\n50 钻", "+100 RP\n100 钻", gems > 0 ? $"全部兑换\n{gems} 钻" : "全部兑换\n0 钻" };
        float bw = (cw - 48f - 36f) * 0.25f;
        for (int i = 0; i < 4; i++)
        {
            int amt = rpAmounts[i];
            bool enough = gems >= amt && gems > 0;
            var btn = Btn(rpCard.transform, "BuyRp_" + i, rpLabels[i], 21, 24f + i * (bw + 12f), 16f, bw, 74f,
                enough ? Indigo : new Color(0.76f, 0.74f, 0.70f, 0.85f), () => TryExchangeRp(amt));
            TInk(btn, Color.white);
        }

        // 区块二：1:10000 兑换资金
        var coinCard = Rect("CoinExchangeCard", popup.transform, pad, 98f, cw, 198f);
        Rounded(coinCard, CardBg);
        var coinTitle = Txt(coinCard.transform, "星航资金兑换（1 星钻 = ¥10,000）", 28, new Color(0.56f, 0.36f, 0.08f), 24f, 134f, cw - 48f, 44f, TextAnchor.MiddleLeft);
        FitSingleLine(coinTitle, 28);
        var coinSub = Txt(coinCard.transform, "补充航线铺设、购置星舰与扩建枢纽星港的现金流。", 22, InkSoft, 24f, 98f, cw - 48f, 34f, TextAnchor.MiddleLeft);
        FitSingleLine(coinSub, 22);

        int[] coinCosts = { 1, 5, 10, 50 };
        string[] coinLabels = { "+¥1万\n1 钻", "+¥5万\n5 钻", "+¥10万\n10 钻", "+¥50万\n50 钻" };
        for (int i = 0; i < 4; i++)
        {
            int cost = coinCosts[i];
            bool enough = gems >= cost;
            var btn = Btn(coinCard.transform, "BuyCoins_" + i, coinLabels[i], 21, 24f + i * (bw + 12f), 16f, bw, 74f,
                enough ? Gold : new Color(0.76f, 0.74f, 0.70f, 0.85f), () => TryExchangeCoins(cost));
            TInk(btn, enough ? new Color(0.24f, 0.16f, 0.04f) : Color.white);
        }

        // 底部来源提示
        var tip = Txt(popup.transform, "星钻来源：名胜打卡（+20）、科技飞车里程碑（+50/+60）、每日任务及公司标书", 21, InkSoft, pad, 34f, cw, 40f, TextAnchor.MiddleCenter);
        FitSingleLine(tip, 21);
    }

    void NotifyOrOfferDiamondSupply(string fallbackToast, bool forRp, float shortfall)
    {
        int gems = Mathf.FloorToInt(diamonds);
        if (gems <= 0 || shortfall <= 0f)
        {
            Toast(fallbackToast);
            return;
        }
        int needGems = forRp ? Mathf.CeilToInt(shortfall) : Mathf.CeilToInt(shortfall / 10000f);
        var pop = OpenPopup(forRp ? "科研点不足 · 星钻补给" : "资金不足 · 星钻补给", 350, 740, "diamond_shortfall");
        string gapStr = forRp ? (Mathf.CeilToInt(shortfall) + " RP") : ("¥" + FormatShortfall(shortfall));
        string hint = $"还差 {gapStr}。\n当前拥有 {gems} 颗星钻（需约 {needGems} 颗），可立即前往兑换补给！";
        Txt(pop.transform, hint, 25, InkSoft, 34f, 128f, 672f, 118f, TextAnchor.MiddleLeft);
        var cancel = Btn(pop.transform, "CancelSupply", "暂不兑换", 24, 34f, 30f, 220f, 76f, Paper2, () => ClosePanel(true));
        TInk(cancel, Ink);
        var go = Btn(pop.transform, "OpenSupply", "打开星钻兑换 ▸", 24, 274f, 30f, 432f, 76f, forRp ? Indigo : Gold, () => {
            ClosePanel(false);
            OpenDiamondExchangePopup();
        });
        TInk(go, forRp ? Color.white : new Color(0.24f, 0.16f, 0.04f));
    }
}
