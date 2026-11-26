using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public partial class DemoBootstrap
{
    sealed class MainSessionSnapshot
    {
        public List<Station> stations; public List<Line> lines;
        public double coins; public int nextLineId; public string currentRegion;
        public int timeSpeed, stationFilter; public float income; public Vector3 mapScale; public Vector2 mapPos;
        public List<UndoState> undo; public UndoState pendingUndo;
    }
    MainSessionSnapshot mainSnapshot;
    RawImage sandboxMapRaw;
    RawImage ashVignette;
    Texture2D ashVignetteTexture;
    readonly List<RectTransform> ashDust = new List<RectTransform>();
    readonly List<Image> ashDustImages = new List<Image>();
    readonly Dictionary<int, Image> ashStationEffects = new Dictionary<int, Image>();
    float ashEffectTime;

    readonly Dictionary<GameObject, bool> mainHudObjects = new Dictionary<GameObject, bool>();

    void ClearStageViews()
    {
        CancelDraft(); ClearStationOdArcs(); ClearFloaters();
        lineVisuals.Clear(); stationImgs.Clear(); stationLabels.Clear();
        stationDestinations.Clear(); stationLeaders.Clear(); pressRings.Clear(); stationRings.Clear();
        foreach (var layer in new[] { lineLayer, stationLayer, trainLayer, odArcLayer })
            if (layer) for (int i = layer.childCount - 1; i >= 0; i--) { var child = layer.GetChild(i).gameObject; child.SetActive(false); Destroy(child); }
    }

    void LoadSandboxLines(SavedEventSandbox state)
    {
        lines = new List<Line>();
        foreach (var sl in state.lines)
            if (sl != null) { sl.dispatch = NormalizeSandboxDispatch(sl.dispatch, sl.cars); lines.Add(new Line { id = sl.id, name = sl.name, color = sl.color, st = new List<int>(sl.stations), trains = sl.cars, dispatch = (int[])sl.dispatch.Clone(), hasBaseTrain = false }); }
        nextLineId = Mathf.Max(1, state.nextLineId);
    }

    GameObject eventSandboxRoot, sandboxAchBadgeGo, sandboxOrderBadgeGo;
    Text sandboxClockTxt, sandboxDayTxt, sandboxStatusTxt, sandboxDraftTxt, sandboxSpeedTxt;

    void OpenEventScreen()
    {
        if (!CanEnterEvents()) { PlayErrorSfx(); Toast("主线开通首条线路后即可前往活动城市"); return; }
        var pg = OpenPage("限时活动", "去另一座城走走，收获的收益与奖励会带回主线公司", "PageArt/line_watercolor", "events");
        const float cardH = 470f, cardGap = 32f, topPad = 24f, botPad = 36f;
        var content = FleetScroll(pg.transform, 32f, PgH - 188f, topPad + EventSpecs.Length * cardH + cardGap + botPad, scrollKey: "events");
        float topY = content.sizeDelta.y, cardX = 4f, innerW = content.sizeDelta.x - 8f;
        for (int i = 0; i < EventSpecs.Length; i++)
        {
            float cy = topY - topPad - (i + 1) * cardH - i * cardGap;
            BuildEventShowcaseCard(content, EventSpecs[i], i, cardX, cy, innerW, cardH);
        }
    }

    void BuildEventShowcaseCard(Transform parent, EventSpec spec, int index, float x, float y, float w, float h)
    {
        var st = GetOrCreateEventSandbox(spec.id);
        bool active = IsEventActive(spec.id);
        var card = Rect("Event_" + spec.id, parent, x, y, w, h);
        Rounded(card, active ? new Color(0.985f, 0.972f, 0.942f, 0.98f) : new Color(0.90f, 0.88f, 0.83f, 0.95f));
        SoftShadow(card);

        float pad = 7f, artW = w - pad * 2f, artH = h - pad * 2f;
        var artClip = Rect("ArtClip", card.transform, pad, pad, artW, artH);
        var clipImg = Img(artClip, new Color(0.96f, 0.94f, 0.89f, 1f), spriteRound);
        clipImg.type = Image.Type.Sliced; clipImg.raycastTarget = false;
        artClip.AddComponent<Mask>().showMaskGraphic = true;

        var bgTex = Resources.Load<Texture2D>(spec.cardBgResource) ?? Resources.Load<Texture2D>(spec.mapTexResource);
        if (bgTex)
        {
            var bgRaw = Rect("Backdrop", artClip.transform, 0, 0, artW, artH).AddComponent<RawImage>();
            bgRaw.texture = bgTex; bgRaw.raycastTarget = false;
            bgRaw.color = active ? new Color(1f, 1f, 1f, 0.90f) : new Color(0.75f, 0.73f, 0.70f, 0.50f);
        }

        var banner = Rect("TitleBanner", artClip.transform, (artW - 560f) * 0.5f, (artH - 130f) * 0.55f, 560f, 130f);
        Rounded(banner, new Color(0.08f, 0.10f, 0.14f, 0.74f));
        banner.GetComponent<Image>().raycastTarget = false;
        Txt(banner.transform, spec.displayTitle, 54, Color.white, 0f, 44f, 560f, 76f, TextAnchor.MiddleCenter).raycastTarget = false;
        Txt(banner.transform, spec.subtitle, 22, new Color(0.92f, 0.94f, 0.96f), 12f, 8f, 536f, 34f, TextAnchor.MiddleCenter).raycastTarget = false;

        var badge = Rect("Badge", artClip.transform, artW - 258f, artH - 68f, 236f, 46f);
        Rounded(badge, active ? spec.accentColor : new Color(0.45f, 0.43f, 0.39f, 0.88f));
        badge.GetComponent<Image>().raycastTarget = false;
        Txt(badge.transform, active ? $"进行中  余 {EventRemainingDays(spec.id)} 天" : "已结束", 19, Color.white, 0, 0, 236f, 46f, TextAnchor.MiddleCenter).raycastTarget = false;

        string infoText = spec.id == EventIdAshDome
            ? $"第 {Mathf.Max(1, st.cycleCount)} 轮  第 {Mathf.Min(st.eventDay, AshSandboxTotalDays)}/{AshSandboxTotalDays} 日  撤离 {st.progress:N0}  {spec.tokenName} {st.tokens}"
            : $"第 {Mathf.Max(1, st.cycleCount)} 轮  第 {st.eventDay} 夜  订单 {st.progress:N0}  {spec.tokenName} {st.tokens}";
        var pill = Rect("StatusPill", artClip.transform, 24f, 22f, 480f, 54f);
        Rounded(pill, new Color(0.99f, 0.975f, 0.945f, 0.94f));
        pill.GetComponent<Image>().raycastTarget = false;
        FitSingleLine(Txt(pill.transform, infoText, 22, Ink, 16f, 0f, 448f, 54f, TextAnchor.MiddleLeft), 22);

        float qBtnW = 114f, qGap = 10f, qStartX = artW - 24f - qBtnW * 3f - qGap * 2f;
        TInk(Btn(artClip.transform, "TreeBtn", "专精树", 21, qStartX, 22f, qBtnW, 54f, Indigo, () => OpenSandboxTreeScreen(spec.id)), Color.white);
        TInk(Btn(artClip.transform, "CmdBtn", "指挥部", 21, qStartX + qBtnW + qGap, 22f, qBtnW, 54f, Matcha, () => OpenSandboxCommandScreen(spec.id)), Color.white);
        int claimable = CountClaimableSandboxAchievements(spec.id);
        TInk(Btn(artClip.transform, "AchBtn", claimable > 0 ? $"成就({claimable})" : "成就", 21, qStartX + (qBtnW + qGap) * 2f, 22f, qBtnW, 54f,
            claimable > 0 ? Vermilion : new Color(0.76f, 0.50f, 0.16f), () => OpenSandboxAchievementsScreen(spec.id)), Color.white);

        var btn = card.AddComponent<Button>(); btn.targetGraphic = card.GetComponent<Image>();
        btn.onClick.AddListener(() => {
            if (!active) { PlayErrorSfx(); Toast("活动已结束"); return; }
            OpenEventSandbox(spec.id);
        });
        if (!card.GetComponent<FleetPressFeedback>()) card.AddComponent<FleetPressFeedback>();
        card.AddComponent<FleetCardEntrance>().delay = index * 0.035f;
    }

    void OpenEventSandbox(string eventId)
    {
        var spec = GetEventSpec(eventId);
        if (spec == null || mainSnapshot != null) return;
        SaveGame();
        mainSnapshot = new MainSessionSnapshot { stations = new List<Station>(stations), lines = new List<Line>(lines),
            coins = coins, nextLineId = nextLineId, currentRegion = currentRegion, timeSpeed = timeSpeed,
            mapScale = mapRect.localScale, mapPos = mapRect.anchoredPosition, stationFilter = stationFilter, income = income,
            undo = new List<UndoState>(undoStack), pendingUndo = pendingUndo };
        undoStack.Clear(); pendingUndo = null; stationFilter = 0;
        ClosePage(true); ClosePanel(true); CloseLineBubble();
        ClearStageViews();
        var state = GetOrCreateEventSandbox(eventId);
        activeSandboxEventId = eventId; currentRegion = eventId;
        traffic = new TrafficResult();
        if (eventId == EventIdAshDome) { ashSandboxHourAccum = state.eventClock / GameDaySeconds * 24f % 1f; EnsureAshStationPopInitialized(state); SyncAshFallenStations(state); }
        stations = new List<Station>();
        foreach (var def in GetSandboxStationDefs(eventId))
        {
            var p = SandboxUvToCanvas(def.u, def.v);
            stations.Add(new Station(def.name, p.x, p.y) { id = stations.Count, region = eventId, built = true,
                closed = eventId == EventIdAshDome && def.isSpecialB, portal = def.isSpecialA });
        }
        for (int i = 0; i < stations.Count; i++)
            if (state.stationLevels != null && i < state.stationLevels.Count) stations[i].level = Mathf.Clamp(state.stationLevels[i], 1, 5);
        LoadSandboxLines(state);
        foreach (var art in regionMapArts.Values) if (art) art.SetActive(false);
        if (moonNearMapArt) moonNearMapArt.SetActive(false);
        if (moonFarMapArt) moonFarMapArt.SetActive(false);
        if (marsWestMapArt) marsWestMapArt.SetActive(false);
        sandboxMapRaw = Rect("SandboxMapArt", mapRect, 0, 0, W, H).AddComponent<RawImage>();
        sandboxMapRaw.texture = Resources.Load<Texture2D>(spec.mapTexResource);
        sandboxMapRaw.raycastTarget = false; sandboxMapRaw.transform.SetAsFirstSibling();
        mainHudObjects.Clear();
        foreach (Transform child in root)
        {
            var go = child.gameObject;
            if (mapRect.IsChildOf(child) || go == toastGo || go == scrim || go == activePanel) continue;
            mainHudObjects[go] = go.activeSelf; go.SetActive(false);
        }
        eventSandboxRoot = new GameObject("EventSandboxCanvas", typeof(RectTransform));
        eventSandboxRoot.transform.SetParent(root, false);
        var rt = eventSandboxRoot.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        if (eventId == EventIdAshDome) BuildAshAtmosphere();
        BuildSandboxHudChrome(spec);
        BuildStationViews(); RefreshStationDots(); RebuildSandboxStationVisuals();
        CreateLineVisuals();
        UpdateSandboxTraffic(state);
        mapRect.localScale = Vector3.one * 1.05f;
        mapRect.anchoredPosition = mapWheelZoom ? mapWheelZoom.CenterPosition(1.05f) : Vector2.zero;
        UpdateRegionalMarkers(true); RefreshSandboxHudText();
        if (eventId == EventIdAshDome) UpdateAshAtmosphere(state, 0f);
        TriggerSandboxEntryStoryIfNeeded(eventId);
    }

    void CloseEventSandbox()
    {
        if (mainSnapshot == null) return;
        SyncMainLinesToSandbox(); SaveSandboxStorage();
        ClosePage(true); ClosePanel(true); CloseLineBubble(); ClearStageViews();
        if (eventSandboxRoot) { eventSandboxRoot.SetActive(false); Destroy(eventSandboxRoot); }
        if (sandboxMapRaw) { sandboxMapRaw.gameObject.SetActive(false); Destroy(sandboxMapRaw.gameObject); }
        if (ashVignetteTexture) Destroy(ashVignetteTexture);
        ashVignetteTexture = null; ashVignette = null;
        ashDust.Clear(); ashDustImages.Clear(); ashStationEffects.Clear(); ashEffectTime = 0f;
        sandboxClockTxt = null; sandboxDayTxt = null;
        eventSandboxRoot = null; sandboxMapRaw = null; activeSandboxEventId = "";
        stations = mainSnapshot.stations; lines = mainSnapshot.lines;
        nextLineId = mainSnapshot.nextLineId; currentRegion = mainSnapshot.currentRegion; timeSpeed = mainSnapshot.timeSpeed;
        stationFilter = mainSnapshot.stationFilter; income = mainSnapshot.income;
        undoStack.Clear(); undoStack.AddRange(mainSnapshot.undo); pendingUndo = mainSnapshot.pendingUndo; RefreshUndoButton();
        var scale = mainSnapshot.mapScale; var pos = mainSnapshot.mapPos; mainSnapshot = null;
        SetActiveRegionMapArt(currentRegion); BuildStationViews(); RefreshStationDots(); CreateLineVisuals();
        RecalcIncome();
        mapRect.localScale = scale; mapRect.anchoredPosition = pos; UpdateRegionalMarkers(true);
        foreach (var entry in mainHudObjects) if (entry.Key) entry.Key.SetActive(entry.Value);
        mainHudObjects.Clear(); RefreshHUD(); RefreshEventBadge(); SaveGame();
    }

    void BuildAshAtmosphere()
    {
        var layer = Rect("AshAtmosphere", eventSandboxRoot.transform, 0f, 0f, 0f, 0f);
        var layerRect = layer.GetComponent<RectTransform>();
        layerRect.anchorMin = Vector2.zero; layerRect.anchorMax = Vector2.one;
        layerRect.offsetMin = Vector2.zero; layerRect.offsetMax = Vector2.zero;
        ashVignetteTexture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
        ashVignetteTexture.name = "AshVignetteTexture";
        ashVignetteTexture.wrapMode = TextureWrapMode.Clamp;
        var pixels = new Color32[64 * 64];
        for (int y = 0; y < 64; y++)
            for (int x = 0; x < 64; x++)
            {
                float nx = Mathf.Abs((x + 0.5f) / 32f - 1f);
                float ny = Mathf.Abs((y + 0.5f) / 32f - 1f);
                float edge = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.35f, 1f, Mathf.Max(nx, ny)));
                pixels[y * 64 + x] = new Color(0.38f, 0.22f, 0.12f, edge);
            }
        ashVignetteTexture.SetPixels32(pixels); ashVignetteTexture.Apply();
        ashVignette = Rect("Vignette", layer.transform, 0f, 0f, 0f, 0f).AddComponent<RawImage>();
        ashVignette.rectTransform.anchorMin = Vector2.zero; ashVignette.rectTransform.anchorMax = Vector2.one;
        ashVignette.rectTransform.offsetMin = Vector2.zero; ashVignette.rectTransform.offsetMax = Vector2.zero;
        ashVignette.texture = ashVignetteTexture; ashVignette.raycastTarget = false;
        for (int i = 0; i < 20; i++)
        {
            var mote = Rect("Dust_" + i, layer.transform, 0f, 0f, 5f + i % 4 * 2f, 3f + i % 3);
            mote.transform.localRotation = Quaternion.Euler(0f, 0f, -25f);
            var img = Img(mote, Color.white, spriteCircle); img.raycastTarget = false;
            ashDust.Add(mote.GetComponent<RectTransform>()); ashDustImages.Add(img);
        }
    }

    void UpdateAshAtmosphere(SavedEventSandbox st, float dt)
    {
        if (activeSandboxEventId != EventIdAshDome || !ashVignette) return;
        ashEffectTime += dt;
        int layer = Mathf.Clamp((int)(st.extraA & 0xFF), 1, 6);
        float intensity = (layer - 1f) / 5f;
        float finalDays = st.eventDay >= 6 ? 1f : 0f;
        ashVignette.color = new Color(1f, 1f, 1f, Mathf.Clamp(0.08f + intensity * 0.12f + finalDays * 0.04f, 0.08f, 0.24f));
        for (int i = 0; i < ashDust.Count; i++)
        {
            float travel = ashEffectTime * (0.012f + intensity * 0.014f) * (0.7f + (i % 5) * 0.13f);
            float x = Mathf.Repeat(i * 0.618034f + travel, 1.08f) - 0.04f;
            float y = Mathf.Repeat(i * 0.381966f - travel * 0.65f, 1.08f) - 0.04f;
            ashDust[i].anchorMin = ashDust[i].anchorMax = new Vector2(x, y);
            ashDust[i].anchoredPosition = Vector2.zero;
            ashDustImages[i].color = new Color(0.83f, 0.68f, 0.48f, 0.10f + intensity * 0.14f);
        }
        float pulse = 0.5f + 0.5f * Mathf.Sin(ashEffectTime * 3f);
        foreach (var effect in ashStationEffects.Values)
            if (effect && effect.gameObject.activeSelf && effect.name == "AshDangerRing")
            {
                effect.color = new Color(Vermilion.r, Vermilion.g, Vermilion.b, 0.45f + 0.45f * pulse);
                effect.rectTransform.localScale = Vector3.one * (0.95f + 0.15f * pulse);
            }
        bool dusk = st.eventDay >= AshSandboxTotalDays - 1;
        bool lastTrain = st.eventDay == AshSandboxTotalDays && st.eventClock >= GameDaySeconds * (20f / 24f);
        float duskStrength = dusk ? (lastTrain ? 0.48f : 0.30f) : 0f;
        if (sandboxMapRaw) sandboxMapRaw.color = Color.Lerp(Color.white, new Color(0.98f, 0.86f, 0.70f), duskStrength);
        if (sandboxClockTxt) sandboxClockTxt.color = dusk ? Color.Lerp(Vermilion, Gold, pulse) : Indigo;
        if (sandboxDayTxt) sandboxDayTxt.color = dusk ? Color.Lerp(Vermilion, Gold, pulse) : InkSoft;
    }

    void BuildSandboxHudChrome(EventSpec spec)
    {
        GetSafeMarginsInRoot(out float safeL, out float safeR, out float safeT, out float safeB);
        float safeW = Mathf.Max(320f, RootW - safeL - safeR);
        float barW = Mathf.Min(W - 40f, safeW - 32f);
        float barX = safeL + (safeW - barW) * 0.5f;

        // 1. 顶栏（严格贴齐 safeT 下方）：左侧返回按钮，中间标题/时钟/两行指标，右侧倍率按钮
        float topY = RootH - safeT - 172f;
        var topBar = Rect("TopBar", eventSandboxRoot.transform, barX, topY, barW, 152f);
        Rounded(topBar, CardBg); SoftShadow(topBar);

        TInk(Btn(topBar.transform, "BackMain", "← 星图", 21, 16f, 44f, 118f, 64f, Paper2, CloseEventSandbox), Ink);

        float titleX = 148f;
        Txt(topBar.transform, spec.displayTitle, 32, Ink, titleX, 92f, 290f, 44f, TextAnchor.MiddleLeft);

        var clockPill = Rect("ClockPill", topBar.transform, titleX + 270f, 95f, 120f, 38f);
        Rounded(clockPill, Color.Lerp(Paper2, Indigo, 0.12f));
        sandboxClockTxt = Txt(clockPill.transform, "06:00", 24, Indigo, 0f, 0f, 120f, 38f, TextAnchor.MiddleCenter);

        sandboxDayTxt = Txt(topBar.transform, "第 1 日", 21, InkSoft, titleX, 50f, barW - titleX - 150f, 34f, TextAnchor.MiddleLeft);
        sandboxStatusTxt = Txt(topBar.transform, "", 20, Ink, titleX, 12f, barW - titleX - 150f, 34f, TextAnchor.MiddleLeft);
        FitSingleLine(sandboxStatusTxt, 20);

        // 右侧：倍率按钮（文案即当前倍率）
        var spdBtn = Btn(topBar.transform, "Speed", timeSpeed == 0 ? "暂停" : $"{timeSpeed}×", 24, barW - 134f, 44f, 118f, 64f, Matcha, () => {
            timeSpeed = timeSpeed == 0 ? 1 : timeSpeed == 1 ? 2 : timeSpeed == 2 ? 3 : 0;
            Toast(timeSpeed == 0 ? "列车暂时停靠" : $"运行倍率 {timeSpeed}×");
            RefreshSandboxHudText();
        });
        TInk(spdBtn, Color.white);
        sandboxSpeedTxt = spdBtn.GetComponentInChildren<Text>();

        // 2. 地图左侧手帐功能栏（like 主线左栏：荣誉 + 手记/车单）
        float leftRailTop = topY - 140f;
        var achRail = Rect("AchRail", eventSandboxRoot.transform, safeL + 16f, leftRailTop, 116f, 102f);
        Rounded(achRail, CardBg); SoftShadow(achRail);
        Txt(achRail.transform, "荣誉", 26, Ink, 6f, 48f, 104f, 42f, TextAnchor.MiddleCenter).raycastTarget = false;
        var achTag = Rect("Tag", achRail.transform, 14f, 12f, 88f, 28f);
        Rounded(achTag, new Color(Paper2.r, Paper2.g, Paper2.b, 0.78f)); achTag.GetComponent<Image>().raycastTarget = false;
        Txt(achTag.transform, "成就", 19, InkSoft, 0, 0, 88f, 28f, TextAnchor.MiddleCenter).raycastTarget = false;
        Img(Rect("AchBadgeRing", achRail.transform, 86f, 74f, 24f, 24f), CardBg, spriteCircle).raycastTarget = false;
        sandboxAchBadgeGo = Rect("AchBadge", achRail.transform, 88f, 76f, 20f, 20f);
        Img(sandboxAchBadgeGo, Vermilion, spriteCircle).raycastTarget = false;
        sandboxAchBadgeGo.SetActive(CountClaimableSandboxAchievements(spec.id) > 0);
        var achBtn = achRail.AddComponent<Button>(); achBtn.targetGraphic = achRail.GetComponent<Image>();
        achBtn.onClick.AddListener(() => OpenSandboxAchievementsScreen(activeSandboxEventId));

        var noteRail = Rect("NoteRail", eventSandboxRoot.transform, safeL + 16f, leftRailTop - 116f, 116f, 102f);
        Rounded(noteRail, CardBg); SoftShadow(noteRail);
        Txt(noteRail.transform, activeSandboxEventId == EventIdNeonNight ? "车单" : "手记", 26, Ink, 6f, 48f, 104f, 42f, TextAnchor.MiddleCenter).raycastTarget = false;
        var noteTag = Rect("Tag", noteRail.transform, 14f, 12f, 88f, 28f);
        Rounded(noteTag, new Color(Paper2.r, Paper2.g, Paper2.b, 0.78f)); noteTag.GetComponent<Image>().raycastTarget = false;
        Txt(noteTag.transform, activeSandboxEventId == EventIdNeonNight ? "实时" : "档案", 19, InkSoft, 0, 0, 88f, 28f, TextAnchor.MiddleCenter).raycastTarget = false;
        Img(Rect("OrderBadgeRing", noteRail.transform, 86f, 74f, 24f, 24f), CardBg, spriteCircle).raycastTarget = false;
        sandboxOrderBadgeGo = Rect("OrderBadge", noteRail.transform, 88f, 76f, 20f, 20f);
        Img(sandboxOrderBadgeGo, Vermilion, spriteCircle).raycastTarget = false;
        sandboxOrderBadgeGo.SetActive(activeSandboxEventId == EventIdNeonNight && sandboxTaxiOrders.Count > 0);
        var noteBtn = noteRail.AddComponent<Button>(); noteBtn.targetGraphic = noteRail.GetComponent<Image>();
        noteBtn.onClick.AddListener(() => {
            if (activeSandboxEventId == EventIdNeonNight) OpenSandboxOrdersPopup();
            else OpenSandboxStoryScreen(EventIdAshDome);
        });

        // 3. 底栏（严格贴齐 safeB 上方，4 个大按钮对齐主线底栏）
        var botBar = Rect("BotBar", eventSandboxRoot.transform, barX, safeB + 18f, barW, 148f);
        Rounded(botBar, CardBg); SoftShadow(botBar);
        sandboxDraftTxt = Txt(botBar.transform, "按住站台拖向下一站，沿街铺线  滚轮缩放，拖动画布", 21, InkSoft, 22f, 90f, barW - 44f, 40f, TextAnchor.MiddleLeft);
        FitSingleLine(sandboxDraftTxt, 21);

        float bw = (barW - 44f - 36f) / 4f;
        TInk(Btn(botBar.transform, "LinesBtn", spec.id == EventIdAshDome ? "专线" : "航线", 23, 22f, 16f, bw, 62f, Matcha, () => OpenSandboxLinesScreen(activeSandboxEventId)), Color.white);
        TInk(Btn(botBar.transform, "FleetBtn", spec.id == EventIdAshDome ? "救援车" : "车库", 23, 22f + (bw + 12f), 16f, bw, 62f, new Color(0.24f, 0.50f, 0.56f), () => OpenSandboxFleetAndSkinScreen(activeSandboxEventId)), Color.white);
        TInk(Btn(botBar.transform, "TreeBtn", "专精树", 23, 22f + (bw + 12f) * 2f, 16f, bw, 62f, Indigo, () => OpenSandboxTreeScreen(activeSandboxEventId)), Color.white);
        TInk(Btn(botBar.transform, "CmdBtn", "指挥部", 23, 22f + (bw + 12f) * 3f, 16f, bw, 62f, new Color(0.56f, 0.40f, 0.24f), () => OpenSandboxCommandScreen(activeSandboxEventId)), Color.white);
    }

    void RebuildSandboxStationVisuals()
    {
        if (string.IsNullOrEmpty(activeSandboxEventId)) return;
        var defs = GetSandboxStationDefs(activeSandboxEventId);
        var st = GetOrCreateEventSandbox(activeSandboxEventId);
        int dangerLayer = Mathf.Clamp((int)(st.extraA & 0xFF), 1, 7);
        for (int i = 0; i < defs.Length; i++)
        {
            if (!stationImgs.TryGetValue(i, out var img)) continue;
            if (!img) continue;
            if (activeSandboxEventId == EventIdAshDome)
            {
                if (defs[i].isSpecialB) img.color = Color.black;
                else if (defs[i].isSpecialA) img.color = Matcha;
                else if (ashFallenStations.Contains(i)) img.color = new Color(0.45f, 0.45f, 0.45f, 0.85f);
                else if (i < st.stationPop.Count && st.stationPop[i] == 0) img.color = new Color(0.58f, 0.76f, 0.72f, 0.88f);
                else if (defs[i].tierOrKind == dangerLayer) img.color = Vermilion;
                else img.color = new Color(0.24f, 0.40f, 0.65f, 1f);
                bool fallen = ashFallenStations.Contains(i);
                bool danger = defs[i].tierOrKind == dangerLayer && !fallen && !defs[i].isSpecialA && !defs[i].isSpecialB;
                if (!ashStationEffects.TryGetValue(i, out var effect) || !effect)
                {
                    var effectGo = Rect("AshStationEffect", img.transform.parent, 9f, 9f, 38f, 38f);
                    var effectRect = effectGo.GetComponent<RectTransform>();
                    effectRect.pivot = new Vector2(0.5f, 0.5f); effectRect.anchoredPosition = new Vector2(28f, 28f);
                    effect = Img(effectGo, Color.white, spriteRing); effect.raycastTarget = false;
                    effectGo.transform.SetAsFirstSibling(); ashStationEffects[i] = effect;
                }
                effect.gameObject.SetActive(fallen || danger);
                effect.name = danger ? "AshDangerRing" : "AshFallenHalo";
                effect.sprite = danger ? spriteRing : spriteCircle;
                if (fallen) { effect.color = new Color(0.48f, 0.47f, 0.44f, 0.24f); effect.rectTransform.localScale = Vector3.one; }
                if (stationLabels.TryGetValue(i, out var label) && label)
                    label.color = fallen ? new Color(0.68f, 0.66f, 0.62f, 0.9f) : Color.white;
            }
            else
            {
                img.color = defs[i].isSpecialA ? Indigo : new Color(0.92f, 0.52f, 0.16f, 1f);
            }
            if (!defs[i].isSpecialA && !defs[i].isSpecialB && (activeSandboxEventId != EventIdAshDome || !ashFallenStations.Contains(i)) && i < stations.Count && (activeSandboxEventId == EventIdNeonNight ? stations[i].waiting >= 3f : stations[i].waiting >= 2500f))
                img.color = Color.Lerp(img.color, Vermilion, 0.3f);
        }
    }

    void RebuildSandboxLines()
    {
        if (!IsInEventSandbox()) return;
        LoadSandboxLines(GetOrCreateEventSandbox(activeSandboxEventId));
        CreateLineVisuals();
    }

    void RefreshSandboxHudText()
    {
        if (!IsInEventSandbox()) return;
        var st = GetOrCreateEventSandbox(activeSandboxEventId);
        int mins = Mathf.FloorToInt(st.eventClock / GameDaySeconds * 1440f);
        int hh = (mins / 60) % 24, mm = mins % 60;
        if (sandboxClockTxt) sandboxClockTxt.text = $"{hh:00}:{mm:00}";
        if (sandboxSpeedTxt) sandboxSpeedTxt.text = timeSpeed == 0 ? "暂停" : $"{timeSpeed}×";
        if (sandboxAchBadgeGo) sandboxAchBadgeGo.SetActive(CountClaimableSandboxAchievements(activeSandboxEventId) > 0);
        if (sandboxOrderBadgeGo) sandboxOrderBadgeGo.SetActive(activeSandboxEventId == EventIdNeonNight && sandboxTaxiOrders.Count > 0);
        if (sandboxDayTxt)
        {
            if (activeSandboxEventId == EventIdAshDome)
                sandboxDayTxt.text = $"第 {Mathf.Max(1, st.cycleCount)} 轮   第 {Mathf.Min(st.eventDay, AshSandboxTotalDays)} / {AshSandboxTotalDays} 日";
            else
                sandboxDayTxt.text = $"第 {Mathf.Max(1, st.cycleCount)} 轮   第 {st.eventDay} 夜   " + ((hh >= 20 || hh < 6) ? "营业中" : "交班中");
        }
        if (sandboxStatusTxt)
        {
            if (activeSandboxEventId == EventIdAshDome)
                sandboxStatusTxt.text = $"警戒区 L{Mathf.Clamp((int)(st.extraA & 0xFF), 1, 7)}   已转运 {st.progress:N0} 人   火种券 {st.tokens}   滞留车 {st.extraB}";
            else
                sandboxStatusTxt.text = $"候车 {sandboxTaxiOrders.Count} 单   载客 {st.progress:N0}   口碑 {st.extraA / 100f:0.0}★   里程券 {st.tokens}";
        }
    }

    void OpenSandboxOrdersPopup()
    {
        var st = GetOrCreateEventSandbox(EventIdNeonNight);
        var panel = OpenPopup($"不夜港实时订单（口碑 {st.extraA / 100f:0.0}★）", 760f, 880f);
        if (!panel) return;
        if (sandboxTaxiOrders.Count == 0)
            Txt(panel.transform, "当前为交班时段或暂无待接订单（营业时间 20:00–06:00）。\n按住站台拖向下一站铺设夜班巡游线，夜班开始后自动接单！", 24, InkSoft, 40f, 320f, 800f, 160f, TextAnchor.MiddleCenter);
        else
        {
            for (int i = 0; i < Mathf.Min(6, sandboxTaxiOrders.Count); i++)
            {
                var o = sandboxTaxiOrders[i];
                var row = Rect("Order_" + i, panel.transform, 36f, 548f - i * 82f, 808f, 72f);
                Rounded(row, o.isStoryPassenger ? Color.Lerp(CardBg, Gold, 0.22f) : Paper2);
                string tag = o.isStoryPassenger ? "★ 故事乘客  " : "";
                Txt(row.transform, $"{tag}{NeonSandboxStations[o.originStation].name} → {NeonSandboxStations[o.destStation].name}", 23, Ink, 18f, 0f, 470f, 72f, TextAnchor.MiddleLeft);
                Txt(row.transform, $"¥{Mathf.RoundToInt(o.baseFare)}   剩 {Mathf.CeilToInt(Mathf.Max(0f, o.patienceMinutes - o.elapsedMinutes))} 分", 21, Indigo, 495f, 0f, 295f, 72f, TextAnchor.MiddleRight);
            }
        }
        TInk(Btn(panel.transform, "NeonStory", "查看夜班档案手记", 23, 36f, 28f, 390f, 68f, Matcha, () => { ClosePanel(true); OpenSandboxStoryScreen(EventIdNeonNight); }), Color.white);
        TInk(Btn(panel.transform, "NeonOk", "关闭", 23, 454f, 28f, 390f, 68f, Indigo, ClosePanel), Color.white);
    }

    bool SandboxConnected(int a, int b, out int cars)
    {
        var lines = GetOrCreateEventSandbox(activeSandboxEventId).lines; var reached = new HashSet<int> { a }; cars = 0;
        bool changed = true; while (changed) { changed = false; foreach (var line in lines) { bool touches = false; foreach (int x in line.stations) if (reached.Contains(x)) { touches = true; break; } if (!touches) continue; foreach (int x in line.stations) if (reached.Add(x)) changed = true; } }
        foreach (var line in lines) if (line.stations.Contains(a) || line.stations.Contains(b)) cars += Mathf.Max(1, line.cars);
        return reached.Contains(b);
    }
    bool HasAnyEventBadge()
    {
        if (!CanEnterEvents()) return false;
        for (int i = 0; i < EventSpecs.Length; i++)
            if (CountClaimableSandboxAchievements(EventSpecs[i].id) > 0 || HasAffordableSandboxTreeNode(EventSpecs[i].id)) return true;
        return false;
    }

    void RefreshEventBadge() { if (eventBadgeGo) eventBadgeGo.SetActive(HasAnyEventBadge()); }
}

