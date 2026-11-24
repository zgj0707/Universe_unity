using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public partial class DemoBootstrap
{
    GameObject metroMapPageRoot;
    RectTransform metroViewportRt;
    RectTransform metroPanContainerRt;
    RectTransform metroCanvasRt;
    MetroPanZoomController metroPanZoom;
    MetroBackdropGridGraphic metroBackdropGraphic;
    MetroMapGraphic metroMapGraphic;
    RectTransform metroStationLayer;
    RectTransform metroLabelLayer;
    RectTransform metroLegendRt;
    CanvasGroup metroHudGroup;
    GameObject metroRegionDropdownList;
    Text metroRegionBtnTxt;
    Text metroOrientBtnTxt;
    Text metroScopeTxt;
    Text metroCleanToastTxt;
    CanvasGroup metroCleanToastGroup;

    string metroRegionFilter = "月之亮面";
    bool metroLandscapeMode = false;
    readonly MetroStyleProfile metroStyle = MetroStyleProfile.CreatePreset(MetroPresetTheme.MinimalWhite);
    MetroLayoutResult metroLastLayout;
    bool metroCleanMode;
    float metroCleanToastTimer;

    public void OpenMetroMapScreen()
    {
        CloseMetroMapScreen();
        ClosePage(false);
        ClosePanel();
        CloseLineBubble();
        ClearStationOdArcs();

        metroRegionFilter = string.IsNullOrEmpty(currentRegion) ? "月之亮面" : currentRegion;
        metroLandscapeMode = false;
        metroCleanMode = false;

        Transform hostParent = canvas != null ? canvas.transform : root;
        metroMapPageRoot = new GameObject("MetroMapPage", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
        metroMapPageRoot.transform.SetParent(hostParent, false);
        var pageRt = metroMapPageRoot.GetComponent<RectTransform>();
        pageRt.anchorMin = Vector2.zero;
        pageRt.anchorMax = Vector2.one;
        pageRt.pivot = new Vector2(0.5f, 0.5f);
        pageRt.offsetMin = Vector2.zero;
        pageRt.offsetMax = Vector2.zero;

        var pageCanvas = metroMapPageRoot.GetComponent<Canvas>();
        pageCanvas.overrideSorting = true;
        pageCanvas.sortingOrder = 120;

        // 1. 全屏纯白底纹（清屏截图无黑边）
        var bgGo = new GameObject("MetroBackdrop", typeof(RectTransform), typeof(CanvasRenderer), typeof(MetroBackdropGridGraphic));
        bgGo.transform.SetParent(metroMapPageRoot.transform, false);
        var bgRt = bgGo.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;
        metroBackdropGraphic = bgGo.GetComponent<MetroBackdropGridGraphic>();
        metroBackdropGraphic.raycastTarget = true;

        // 2. 可缩放/平移的地铁图视口
        var vpGo = new GameObject("MetroViewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Mask));
        vpGo.transform.SetParent(metroMapPageRoot.transform, false);
        metroViewportRt = vpGo.GetComponent<RectTransform>();
        metroViewportRt.anchorMin = Vector2.zero;
        metroViewportRt.anchorMax = Vector2.one;
        metroViewportRt.offsetMin = Vector2.zero;
        metroViewportRt.offsetMax = Vector2.zero;
        var vpImg = vpGo.GetComponent<Image>();
        vpImg.color = new Color(1, 1, 1, 0.002f);
        vpGo.GetComponent<Mask>().showMaskGraphic = false;

        // 挂载点击监听（清屏下轻触恢复界面；正常态点击空白关闭下拉菜单）
        var tapCatcher = vpGo.AddComponent<MetroCleanTapCatcher>();
        tapCatcher.onTap = () =>
        {
            if (metroCleanMode)
            {
                SetMetroCleanMode(false);
            }
            else if (metroRegionDropdownList && metroRegionDropdownList.activeSelf)
            {
                metroRegionDropdownList.SetActive(false);
            }
        };

        var panGo = new GameObject("MetroPanContainer", typeof(RectTransform));
        panGo.transform.SetParent(vpGo.transform, false);
        metroPanContainerRt = panGo.GetComponent<RectTransform>();
        metroPanContainerRt.anchorMin = new Vector2(0, 0);
        metroPanContainerRt.anchorMax = new Vector2(0, 0);
        metroPanContainerRt.pivot = new Vector2(0f, 0f);
        metroPanContainerRt.sizeDelta = new Vector2(W, H);
        metroPanContainerRt.anchoredPosition = Vector2.zero;

        var canvasGo = new GameObject("MetroCanvas", typeof(RectTransform));
        canvasGo.transform.SetParent(metroPanContainerRt, false);
        metroCanvasRt = canvasGo.GetComponent<RectTransform>();
        metroCanvasRt.anchorMin = new Vector2(0.5f, 0.5f);
        metroCanvasRt.anchorMax = new Vector2(0.5f, 0.5f);
        metroCanvasRt.pivot = new Vector2(0.5f, 0.5f);
        metroCanvasRt.sizeDelta = new Vector2(W, H);
        metroCanvasRt.anchoredPosition = Vector2.zero;

        metroPanZoom = vpGo.AddComponent<MetroPanZoomController>();
        metroPanZoom.map = metroPanContainerRt;
        metroPanZoom.viewport = metroViewportRt;
        metroPanZoom.canInteract = () => metroMapPageRoot != null;

        // 3. 矢量线路层 + 站徽层 + 站名标签层
        var lineGo = new GameObject("MetroLines", typeof(RectTransform), typeof(CanvasRenderer), typeof(MetroMapGraphic));
        lineGo.transform.SetParent(metroCanvasRt, false);
        var lineRt = lineGo.GetComponent<RectTransform>();
        lineRt.anchorMin = Vector2.zero;
        lineRt.anchorMax = Vector2.one;
        lineRt.offsetMin = Vector2.zero;
        lineRt.offsetMax = Vector2.zero;
        metroMapGraphic = lineGo.GetComponent<MetroMapGraphic>();
        metroMapGraphic.raycastTarget = false;

        metroStationLayer = Rect("MetroStations", metroCanvasRt, 0, 0, W, H).GetComponent<RectTransform>();
        metroLabelLayer = Rect("MetroLabels", metroCanvasRt, 0, 0, W, H).GetComponent<RectTransform>();

        // 4. 控制 HUD 组（清屏时整体隐藏，包括角标图例）
        BuildMetroHud();

        // 5. 画布内嵌角标（挂在 metroHudGroup 下，清屏截图时一并隐藏）
        var legendGo = Rect("MetroLegend", metroHudGroup.transform, 32, H - 360, 340, 260);
        metroLegendRt = legendGo.GetComponent<RectTransform>();

        // 6. 清屏轻提示（进入清屏时闪现 1.35 秒后自动淡出）
        var toastObj = new GameObject("MetroCleanToast", typeof(RectTransform), typeof(CanvasRenderer));
        toastObj.transform.SetParent(metroMapPageRoot.transform, false);
        var toastRt = toastObj.GetComponent<RectTransform>();
        toastRt.anchorMin = new Vector2(0.5f, 0f);
        toastRt.anchorMax = new Vector2(0.5f, 0f);
        toastRt.pivot = new Vector2(0.5f, 0.5f);
        toastRt.anchoredPosition = new Vector2(0f, 150f);
        toastRt.sizeDelta = new Vector2(560f, 64f);
        Rounded(toastObj, new Color(0.12f, 0.14f, 0.18f, 0.82f));
        metroCleanToastGroup = toastObj.AddComponent<CanvasGroup>();
        metroCleanToastGroup.alpha = 0f;
        metroCleanToastGroup.blocksRaycasts = false;
        metroCleanToastTxt = Txt(toastObj.transform, "已清屏，轻点画面恢复", 24, Color.white, 16, 4, 528, 56, TextAnchor.MiddleCenter);
        metroCleanToastTxt.raycastTarget = false;

        Canvas.ForceUpdateCanvases();

        // 7. 首次生成并自动居中
        RebuildMetroMap(true);
    }

    void BuildMetroHud()
    {
        var hudGo = new GameObject("MetroHud", typeof(RectTransform), typeof(CanvasGroup));
        hudGo.transform.SetParent(metroMapPageRoot.transform, false);
        var hudRt = hudGo.GetComponent<RectTransform>();
        hudRt.anchorMin = Vector2.zero;
        hudRt.anchorMax = Vector2.one;
        hudRt.offsetMin = Vector2.zero;
        hudRt.offsetMax = Vector2.zero;
        metroHudGroup = hudGo.GetComponent<CanvasGroup>();

        // 顶部控制栏（顶部居中锚点）
        var topBar = Rect("MetroTopBar", hudGo.transform, 0, 0, W - 48, 92);
        var topRt = topBar.GetComponent<RectTransform>();
        topRt.anchorMin = new Vector2(0.5f, 1f);
        topRt.anchorMax = new Vector2(0.5f, 1f);
        topRt.pivot = new Vector2(0.5f, 1f);
        topRt.anchoredPosition = new Vector2(0, -28f);
        Rounded(topBar, CardBg);
        SoftShadow(topBar);

        var backBtn = Btn(topBar.transform, "MetroBack", "返回", 28, 14, 14, 120, 64, Paper2, CloseMetroMapScreen);
        backBtn.GetComponentInChildren<Text>().color = Ink;

        metroScopeTxt = Txt(topBar.transform, "月之亮面地铁图", 32, Ink, 148, 12, 410, 68, TextAnchor.MiddleLeft);
        metroScopeTxt.resizeTextForBestFit = true;
        metroScopeTxt.resizeTextMinSize = 20;
        metroScopeTxt.resizeTextMaxSize = 32;

        // 区域下拉按钮
        var regBtn = Btn(topBar.transform, "MetroRegionDropdown", metroRegionFilter + " ▼", 26, 580, 14, 210, 64, Paper2, () =>
        {
            if (metroRegionDropdownList != null)
            {
                metroRegionDropdownList.SetActive(!metroRegionDropdownList.activeSelf);
            }
        });
        metroRegionBtnTxt = regBtn.GetComponentInChildren<Text>();
        metroRegionBtnTxt.color = Ink;

        // 区域下拉选项浮层
        metroRegionDropdownList = Rect("MetroRegionList", topBar.transform, 580, -206, 210, 200);
        Rounded(metroRegionDropdownList, CardBg);
        SoftShadow(metroRegionDropdownList);
        string[] regions = { "月之亮面", "月背", "火星" };
        for (int i = 0; i < regions.Length; i++)
        {
            string rName = regions[i];
            float iy = 136f - i * 64f;
            var optBtn = Btn(metroRegionDropdownList.transform, "Opt_" + rName, rName, 26, 8, iy, 194, 56, Paper2, () =>
            {
                metroRegionFilter = rName;
                if (metroRegionBtnTxt != null) metroRegionBtnTxt.text = rName + " ▼";
                metroRegionDropdownList.SetActive(false);
                RebuildMetroMap(true);
            });
            optBtn.GetComponentInChildren<Text>().color = Ink;
        }
        metroRegionDropdownList.SetActive(false);

        // 横屏 / 竖屏切换按钮
        var orientBtn = Btn(topBar.transform, "MetroOrient", "横屏", 26, 806, 14, 210, 64, Indigo, () =>
        {
            metroLandscapeMode = !metroLandscapeMode;
            if (metroOrientBtnTxt != null) metroOrientBtnTxt.text = metroLandscapeMode ? "竖屏" : "横屏";
            RebuildMetroMap(true);
        });
        metroOrientBtnTxt = orientBtn.GetComponentInChildren<Text>();
        metroOrientBtnTxt.color = Color.white;

        // 底部操作栏（居中构图 + 清屏截图）
        var botBar = Rect("MetroBotBar", hudGo.transform, 0, 0, W - 48, 92);
        var botRt = botBar.GetComponent<RectTransform>();
        botRt.anchorMin = new Vector2(0.5f, 0f);
        botRt.anchorMax = new Vector2(0.5f, 0f);
        botRt.pivot = new Vector2(0.5f, 0f);
        botRt.anchoredPosition = new Vector2(0, 28f);
        Rounded(botBar, CardBg);
        SoftShadow(botBar);

        var fitBtn = Btn(botBar.transform, "MetroFit", "居中构图", 28, 16, 14, 280, 64, Paper2, FitMetroToScreen);
        fitBtn.GetComponentInChildren<Text>().color = Ink;

        var cleanBtn = Btn(botBar.transform, "MetroClean", "清屏截图", 28, 316, 14, W - 48 - 332, 64, Vermilion, () => SetMetroCleanMode(true));
        cleanBtn.GetComponentInChildren<Text>().color = Color.white;
    }

    void RebuildMetroMap(bool autoFit)
    {
        if (metroMapPageRoot == null) return;

        // 按当前区域建成站点数动态放大布局画布，留出充裕的站间空隙，再通过缩放一屏全览
        int builtInRegion = 0;
        for (int i = 0; i < stations.Count; i++)
        {
            var st = stations[i];
            if (st != null && st.built && !st.closed && string.Equals(st.region, metroRegionFilter, StringComparison.Ordinal))
            {
                builtInRegion++;
            }
        }
        float canvasBoost = Mathf.Clamp(Mathf.Sqrt(Mathf.Max(12, builtInRegion) / 14f), 1.15f, 2.35f);
        Vector2 baseSize = metroLandscapeMode ? new Vector2(H, W) : new Vector2(W, H);
        Vector2 layoutSize = baseSize * canvasBoost;
        metroCanvasRt.sizeDelta = layoutSize;
        metroStationLayer.sizeDelta = layoutSize;
        metroLabelLayer.sizeDelta = layoutSize;

        // 竖屏屏幕开启横屏模式时，将画布顺时针旋转 90° 以便横握满屏观看；外层缩放容器保持正置尺寸
        bool viewportIsPortrait = metroViewportRt != null && metroViewportRt.rect.height > metroViewportRt.rect.width;
        bool rotated = metroLandscapeMode && viewportIsPortrait;
        metroCanvasRt.localEulerAngles = rotated ? new Vector3(0f, 0f, -90f) : Vector3.zero;
        if (metroPanContainerRt != null)
        {
            metroPanContainerRt.sizeDelta = rotated ? new Vector2(layoutSize.y, layoutSize.x) : layoutSize;
        }

        metroBackdropGraphic.ApplyStyle(metroStyle);
        metroLastLayout = MetroLayoutBuilder.Build(stations, lines, metroRegionFilter, null, layoutSize, metroStyle);
        metroMapGraphic.SetData(metroLastLayout.polylines, metroStyle);

        if (metroScopeTxt != null)
        {
            metroScopeTxt.text = metroLastLayout.scopeTitle + "地铁图";
        }
        if (metroRegionBtnTxt != null)
        {
            metroRegionBtnTxt.text = metroRegionFilter + " ▼";
        }

        ClearChildren(metroStationLayer);
        ClearChildren(metroLabelLayer);

        for (int i = 0; i < metroLastLayout.nodes.Count; i++)
        {
            var node = metroLastLayout.nodes[i];
            CreateMetroStationNode(node);
            CreateMetroStationLabel(node);
        }

        RefreshMetroLegend();
        if (autoFit) FitMetroToScreen();
    }

    void CreateMetroStationNode(MetroNodeLayout node)
    {
        if (node.isPortalHint)
        {
            var hintGo = Rect("Portal_" + node.stationId, metroStationLayer, node.pos.x - 6f, node.pos.y - 6f, 12f, 12f);
            var hImg = Img(hintGo, node.primaryColor, spriteCircle);
            hImg.raycastTarget = false;
            return;
        }

        // MetroMan 风格：换乘站为大号黑框白芯环，普通站为白芯线色环
        float size = node.isTransfer ? (metroStyle.stationRadius * 2.85f) : (metroStyle.stationRadius * 1.95f);
        var outerGo = Rect("St_" + node.stationId, metroStationLayer, node.pos.x - size * 0.5f, node.pos.y - size * 0.5f, size, size);
        Sprite shapeSpr = metroStyle.stationShape == MetroStationShape.Square ? spriteRound : spriteCircle;
        Color outerCol = node.isTransfer ? metroStyle.transferRingColor : node.primaryColor;
        var outerImg = Img(outerGo, outerCol, shapeSpr);
        outerImg.raycastTarget = false;

        if (metroStyle.stationShape != MetroStationShape.SolidDot)
        {
            float innerPad = node.isTransfer ? 5.2f : 3.6f;
            float innerSize = Mathf.Max(4f, size - innerPad * 2f);
            var innerGo = Rect("Inner", outerGo.transform, innerPad, innerPad, innerSize, innerSize);
            var innerImg = Img(innerGo, metroStyle.stationFillColor, shapeSpr);
            innerImg.raycastTarget = false;
        }
    }

    void CreateMetroStationLabel(MetroNodeLayout node)
    {
        // 采用 2.5× 超采样光栅化：字体图集以 50px+ 高清生成再缩回 0.4×，缩放全览与特写皆清晰锐利
        const float oversample = 2.5f;
        Vector2 p = node.pos + node.labelOffset;
        int baseFs = node.isPortalHint
            ? Mathf.Max(16, metroStyle.labelFontSize - 3)
            : (node.isTransfer ? metroStyle.labelFontSize + 3 : metroStyle.labelFontSize);

        Color col = node.isPortalHint
            ? new Color(metroStyle.labelColor.r, metroStyle.labelColor.g, metroStyle.labelColor.b, 0.72f)
            : metroStyle.labelColor;

        var go = new GameObject("Lbl_" + node.stationId, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        go.transform.SetParent(metroLabelLayer, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(Mathf.Round(p.x), Mathf.Round(p.y));
        rt.sizeDelta = new Vector2(240f * oversample, 44f * oversample);
        rt.localScale = Vector3.one / oversample;

        var txt = go.GetComponent<Text>();
        txt.font = font;
        txt.fontSize = Mathf.RoundToInt(baseFs * oversample);
        txt.color = col;
        txt.text = DisplayPlaceAliases(node.name);
        txt.alignment = TextAnchor.MiddleCenter;
        txt.horizontalOverflow = HorizontalWrapMode.Overflow;
        txt.verticalOverflow = VerticalWrapMode.Overflow;
        txt.raycastTarget = false;
        if (node.isTransfer) txt.fontStyle = FontStyle.Bold;

        // 极细纯白护边（超采样下等效约 1.2px），防止文字偶遇交叉线时发灰断笔
        var outline = go.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 1f, 1f, 0.92f);
        outline.effectDistance = new Vector2(1.4f * oversample, -1.4f * oversample);
    }

    void RefreshMetroLegend()
    {
        if (metroLegendRt == null || metroLastLayout == null) return;
        ClearChildren(metroLegendRt);

        if (metroStyle.legendCorner == MetroLegendCorner.Hidden)
        {
            metroLegendRt.gameObject.SetActive(false);
            return;
        }

        metroLegendRt.gameObject.SetActive(true);
        int showCount = Mathf.Min(10, metroLastLayout.legends.Count);
        float cardW = 300f;
        float cardH = 66f + showCount * 32f + (showCount == 0 ? 34f : 12f);

        float marginX = 32f;
        float marginTop = 138f;
        float marginBot = 138f;
        bool isRight = metroStyle.legendCorner == MetroLegendCorner.TopRight || metroStyle.legendCorner == MetroLegendCorner.BottomRight;
        bool isTop = metroStyle.legendCorner == MetroLegendCorner.TopLeft || metroStyle.legendCorner == MetroLegendCorner.TopRight;

        metroLegendRt.anchorMin = new Vector2(isRight ? 1f : 0f, isTop ? 1f : 0f);
        metroLegendRt.anchorMax = metroLegendRt.anchorMin;
        metroLegendRt.pivot = metroLegendRt.anchorMin;
        metroLegendRt.anchoredPosition = new Vector2(isRight ? -marginX : marginX, isTop ? -marginTop : marginBot);
        metroLegendRt.sizeDelta = new Vector2(cardW, cardH);

        var bg = metroLegendRt.GetComponent<Image>() ?? metroLegendRt.gameObject.AddComponent<Image>();
        bg.sprite = spriteRound;
        bg.type = Image.Type.Sliced;
        bg.color = metroStyle.cardBgColor;
        bg.raycastTarget = false;

        var outline = metroLegendRt.GetComponent<Outline>() ?? metroLegendRt.gameObject.AddComponent<Outline>();
        outline.effectColor = metroStyle.cardBorderColor;
        outline.effectDistance = new Vector2(1.5f, -1.5f);

        CreateCrispUiText(metroLegendRt, "LegendTitle", metroLastLayout.scopeTitle + "线路", 25, metroStyle.labelColor, 18, cardH - 46, cardW - 36, 36, TextAnchor.MiddleLeft, true);

        if (showCount == 0)
        {
            CreateCrispUiText(metroLegendRt, "LegendEmpty", "当前区域暂未开通线路", 20, metroStyle.labelColor, 18, 20, cardW - 36, 30, TextAnchor.MiddleLeft, false);
            return;
        }

        float rowY = cardH - 82f;
        for (int i = 0; i < showCount; i++)
        {
            var item = metroLastLayout.legends[i];
            var barGo = Rect("Bar_" + i, metroLegendRt, 18, rowY + 8, 28, 10);
            var bImg = Img(barGo, item.color, spriteRound);
            bImg.raycastTarget = false;

            CreateCrispUiText(metroLegendRt, "LegendItem_" + i, item.lineName, 20, metroStyle.labelColor, 56, rowY, cardW - 72, 28, TextAnchor.MiddleLeft, false);
            rowY -= 32f;
        }
    }

    void FitMetroToScreen()
    {
        if (metroPanContainerRt == null || metroCanvasRt == null || metroViewportRt == null || metroLastLayout == null) return;
        float vw = metroViewportRt.rect.width > 10f ? metroViewportRt.rect.width : W;
        float vh = metroViewportRt.rect.height > 10f ? metroViewportRt.rect.height : H;

        Vector2 min = metroLastLayout.boundsMin;
        Vector2 max = metroLastLayout.boundsMax;
        float bw = Mathf.Max(220f, max.x - min.x);
        float bh = Mathf.Max(220f, max.y - min.y);

        bool rotated = Mathf.Abs(Mathf.DeltaAngle(metroCanvasRt.localEulerAngles.z, 0f)) > 45f;
        float effW = rotated ? bh : bw;
        float effH = rotated ? bw : bh;

        float scale = Mathf.Clamp(Mathf.Min((vw * 0.88f) / effW, (vh * 0.78f) / effH), 0.22f, 3.2f);
        metroPanContainerRt.localScale = Vector3.one * scale;

        Vector2 layoutSize = metroCanvasRt.sizeDelta;
        Vector2 localOffsetFromCanvasCenter = (min + max) * 0.5f - layoutSize * 0.5f;
        Vector2 rotatedOffset = rotated
            ? new Vector2(localOffsetFromCanvasCenter.y, -localOffsetFromCanvasCenter.x)
            : localOffsetFromCanvasCenter;

        Vector2 panSize = metroPanContainerRt.sizeDelta;
        Vector2 targetInPan = panSize * 0.5f + rotatedOffset;
        Vector2 pos = new Vector2(vw * 0.5f, vh * 0.5f) - targetInPan * scale;
        metroPanContainerRt.anchoredPosition = pos;
    }

    void SetMetroCleanMode(bool clean)
    {
        metroCleanMode = clean;
        if (metroRegionDropdownList != null && clean)
        {
            metroRegionDropdownList.SetActive(false);
        }
        if (metroHudGroup != null)
        {
            metroHudGroup.alpha = clean ? 0f : 1f;
            metroHudGroup.interactable = !clean;
            metroHudGroup.blocksRaycasts = !clean;
        }

        if (clean)
        {
            metroCleanToastTimer = 1.35f;
            if (metroCleanToastGroup != null) metroCleanToastGroup.alpha = 1f;
        }
        else
        {
            metroCleanToastTimer = 0f;
            if (metroCleanToastGroup != null) metroCleanToastGroup.alpha = 0f;
        }
    }

    void UpdateMetroMapOverlay()
    {
        if (metroMapPageRoot == null || metroCleanToastGroup == null) return;
        if (metroCleanToastTimer > 0f)
        {
            metroCleanToastTimer -= Time.unscaledDeltaTime;
            if (metroCleanToastTimer <= 0.35f)
            {
                metroCleanToastGroup.alpha = Mathf.Clamp01(metroCleanToastTimer / 0.35f);
            }
        }
    }

    void CloseMetroMapScreen()
    {
        metroCleanMode = false;
        metroCleanToastTimer = 0f;
        if (metroMapPageRoot != null)
        {
            Destroy(metroMapPageRoot);
            metroMapPageRoot = null;
        }
        metroViewportRt = null;
        metroPanContainerRt = null;
        metroCanvasRt = null;
        metroPanZoom = null;
        metroBackdropGraphic = null;
        metroMapGraphic = null;
        metroStationLayer = null;
        metroLabelLayer = null;
        metroLegendRt = null;
        metroHudGroup = null;
        metroRegionDropdownList = null;
        metroRegionBtnTxt = null;
        metroOrientBtnTxt = null;
    }

    Text CreateCrispUiText(Transform parent, string name, string content, int visualSize, Color color, float x, float y, float w, float h, TextAnchor align, bool bold)
    {
        const float oversample = 2.0f;
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.zero;
        rt.pivot = Vector2.zero;
        rt.anchoredPosition = new Vector2(Mathf.Round(x), Mathf.Round(y));
        rt.sizeDelta = new Vector2(w * oversample, h * oversample);
        rt.localScale = Vector3.one / oversample;

        var t = go.GetComponent<Text>();
        t.font = font;
        t.fontSize = Mathf.RoundToInt(visualSize * oversample);
        t.color = color;
        t.text = DisplayPlaceAliases(content);
        t.alignment = align;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Truncate;
        t.raycastTarget = false;
        if (bold) t.fontStyle = FontStyle.Bold;
        return t;
    }

    static void ClearChildren(Transform t)
    {
        if (!t) return;
        for (int i = t.childCount - 1; i >= 0; i--)
        {
            Destroy(t.GetChild(i).gameObject);
        }
    }
}

/// <summary>
/// 仅在非拖拽点击时触发回调，避免清屏状态下缩放/平移误退出。
/// </summary>
public class MetroCleanTapCatcher : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public Action onTap;
    Vector2 downPos;

    public void OnPointerDown(PointerEventData eventData)
    {
        downPos = eventData.position;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if ((eventData.position - downPos).sqrMagnitude <= 144f)
        {
            onTap?.Invoke();
        }
    }
}

/// <summary>
/// 地铁图专用自由缩放拖拽控制器：允许放大后将画布任意一侧拖至屏幕中央，不会锁死边缘。
/// </summary>
public class MetroPanZoomController : MonoBehaviour, IScrollHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public RectTransform map, viewport;
    public Func<bool> canInteract;
    const float MinZoom = 0.22f;
    const float MaxZoom = 4.5f;

    bool dragging;
    Vector2 prevLocal;
    float pinchDistance;

    public Vector2 ClampPosition(Vector2 pos, float scale)
    {
        if (!map || !viewport) return pos;
        float vw = viewport.rect.width;
        float vh = viewport.rect.height;
        float scaledW = map.rect.width * scale;
        float scaledH = map.rect.height * scale;

        // 允许画布任一边缘拖到视口中心附近（留出半屏余量），既不会拖丢，也绝不锁死左右两侧
        float padX = vw * 0.45f;
        float padY = vh * 0.45f;
        pos.x = Mathf.Clamp(pos.x, padX - scaledW, vw - padX);
        pos.y = Mathf.Clamp(pos.y, padY - scaledH, vh - padY);
        return pos;
    }

    void ZoomAt(Vector2 screenPoint, float next)
    {
        if (!map || !viewport) return;
        float current = map.localScale.x;
        next = Mathf.Clamp(next, MinZoom, MaxZoom);
        if (Mathf.Approximately(next, current)) return;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(map, screenPoint, null, out var local)) return;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport, screenPoint, null, out var desired)) return;

        map.localScale = Vector3.one * next;
        Vector2 actual = viewport.InverseTransformPoint(map.TransformPoint(local));
        map.anchoredPosition = ClampPosition(map.anchoredPosition + desired - actual, next);
    }

    public void OnScroll(PointerEventData e)
    {
        if (!map || !viewport || (canInteract != null && !canInteract()) || Mathf.Approximately(e.scrollDelta.y, 0f)) return;
        ZoomAt(e.position, map.localScale.x * Mathf.Pow(1.12f, e.scrollDelta.y));
    }

    public void OnBeginDrag(PointerEventData e)
    {
        if (!map || !viewport || (canInteract != null && !canInteract()) || Input.touchCount >= 2) return;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport, e.position, e.pressEventCamera, out prevLocal))
        {
            dragging = true;
        }
    }

    public void OnDrag(PointerEventData e)
    {
        if (!dragging || !map || !viewport || Input.touchCount >= 2) return;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport, e.position, e.pressEventCamera, out var current)) return;
        Vector2 delta = current - prevLocal;
        prevLocal = current;
        map.anchoredPosition = ClampPosition(map.anchoredPosition + delta, map.localScale.x);
    }

    public void OnEndDrag(PointerEventData e) => dragging = false;

    void Update()
    {
        if (!map || !viewport || (canInteract != null && !canInteract()) || Input.touchCount < 2)
        {
            pinchDistance = 0f;
            return;
        }
        dragging = false;
        Touch a = Input.GetTouch(0), b = Input.GetTouch(1);
        float d = Vector2.Distance(a.position, b.position);
        if (pinchDistance > 0.01f && d > 0.01f)
        {
            ZoomAt((a.position + b.position) * 0.5f, map.localScale.x * (d / pinchDistance));
        }
        pinchDistance = d;
    }
}
