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
    bool metroLegendCollapsed = false;
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

        string initial = string.IsNullOrEmpty(currentRegion) ? "月之亮面" : currentRegion;
        // 旧存档仍以 "火星" 作为单一区域，直接落到全境拼接视图，避免下拉无对应项
        if (string.Equals(initial, "火星", StringComparison.Ordinal)) initial = "火星·全境";
        metroRegionFilter = initial;
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

        // 2. 可缩放/平移的城市星轨图视口
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

        metroScopeTxt = Txt(topBar.transform, "月之亮面城市星轨图", 32, Ink, 148, 12, 410, 68, TextAnchor.MiddleLeft);
        metroScopeTxt.resizeTextForBestFit = true;
        metroScopeTxt.resizeTextMinSize = 20;
        metroScopeTxt.resizeTextMaxSize = 32;
        metroScopeTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
        metroScopeTxt.verticalOverflow = VerticalWrapMode.Truncate;

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

        // 区域下拉选项浮层：按选项数量自适应高度与逐行 Y 坐标，7 项全量点选不裁切
        string[] regions = { "火星·全境", "月之亮面", "月背", "火星·西半球", "火星·东半球" };
        const float optH = 56f;
        const float optGap = 8f;
        const float listPad = 10f;
        const float listW = 220f;
        float listH = listPad * 2f + regions.Length * optH + (regions.Length - 1) * optGap;
        metroRegionDropdownList = Rect("MetroRegionList", topBar.transform, 580f, -(listH + 6f), listW, listH);
        Rounded(metroRegionDropdownList, CardBg);
        SoftShadow(metroRegionDropdownList);
        for (int i = 0; i < regions.Length; i++)
        {
            string rName = regions[i];
            float iy = listPad + (regions.Length - 1 - i) * (optH + optGap);
            var optBtn = Btn(metroRegionDropdownList.transform, "Opt_" + rName, rName, 24, 8, iy, listW - 16f, optH, Paper2, () =>
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

        // 按当前视图建成港点数动态放大布局画布，留出充裕站距后再缩放一屏全览
        bool isMarsAll = string.Equals(metroRegionFilter, "火星·全境", StringComparison.Ordinal)
                      || string.Equals(metroRegionFilter, "火星", StringComparison.Ordinal);
        int builtInRegion = 0;
        for (int i = 0; i < stations.Count; i++)
        {
            var st = stations[i];
            if (st == null || !st.built || st.closed) continue;
            bool serves = isMarsAll
                ? MetroLayoutBuilder.StationIsMars(st)
                : MetroLayoutBuilder.StationServesRegion(st, metroRegionFilter);
            if (serves) builtInRegion++;
        }
        float densityBase = isMarsAll ? 24f : 14f;
        float canvasBoost = Mathf.Clamp(
            Mathf.Sqrt(Mathf.Max(12, builtInRegion) / densityBase),
            isMarsAll ? 1.25f : 1.15f,
            isMarsAll ? 2.10f : 2.35f);
        Vector2 baseSize = metroLandscapeMode
            ? (isMarsAll ? new Vector2(H * 1.25f, W * 1.05f) : new Vector2(H, W))
            : (isMarsAll ? new Vector2(H * 1.25f, W * 1.05f) : new Vector2(W, H));
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
            metroScopeTxt.text = isMarsAll
                ? "大火星·全境拼接星轨图"
                : metroLastLayout.scopeTitle + "城市星轨图";
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

        // MetroMan 风格：三域超级枢纽叠金色双菱星徽，换线接驳站为大号黑框白芯环，普通站为白芯线色环
        if (node.isSuperHub && spriteDiamond)
        {
            float hubSizeA = metroStyle.stationRadius * 4.2f;
            float hubSizeB = metroStyle.stationRadius * 3.4f;
            var starA = Rect("SuperStarA_" + node.stationId, metroStationLayer, node.pos.x - hubSizeA * 0.5f, node.pos.y - hubSizeA * 0.5f, hubSizeA, hubSizeA);
            Img(starA, new Color(0.95f, 0.72f, 0.16f, 0.96f), spriteDiamond).raycastTarget = false;
            var starB = Rect("SuperStarB_" + node.stationId, metroStationLayer, node.pos.x, node.pos.y, hubSizeB, hubSizeB);
            var starBRt = starB.GetComponent<RectTransform>();
            starBRt.pivot = new Vector2(0.5f, 0.5f);
            starBRt.anchoredPosition = node.pos;
            starB.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            Img(starB, new Color(0.88f, 0.58f, 0.14f, 0.92f), spriteDiamond).raycastTarget = false;
        }
        float size = node.isSuperHub ? (metroStyle.stationRadius * 3.1f)
            : node.isTransfer ? (metroStyle.stationRadius * 2.85f)
            : (metroStyle.stationRadius * 1.95f);
        var outerGo = Rect("St_" + node.stationId, metroStationLayer, node.pos.x - size * 0.5f, node.pos.y - size * 0.5f, size, size);
        Sprite shapeSpr = node.isSuperHub && spriteDiamond ? spriteDiamond
            : (metroStyle.stationShape == MetroStationShape.Square ? spriteRound : spriteCircle);
        Color outerCol = node.isSuperHub ? new Color(0.76f, 0.22f, 0.20f, 1f)
            : (node.isTransfer ? metroStyle.transferRingColor : node.primaryColor);
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
        float marginX = 32f;
        float marginTop = 138f;
        float marginBot = 138f;
        bool isRight = metroStyle.legendCorner == MetroLegendCorner.TopRight || metroStyle.legendCorner == MetroLegendCorner.BottomRight;
        bool isTop = metroStyle.legendCorner == MetroLegendCorner.TopLeft || metroStyle.legendCorner == MetroLegendCorner.TopRight;

        metroLegendRt.anchorMin = new Vector2(isRight ? 1f : 0f, isTop ? 1f : 0f);
        metroLegendRt.anchorMax = metroLegendRt.anchorMin;
        metroLegendRt.pivot = metroLegendRt.anchorMin;
        metroLegendRt.anchoredPosition = new Vector2(isRight ? -marginX : marginX, isTop ? -marginTop : marginBot);

        var bg = metroLegendRt.GetComponent<Image>() ?? metroLegendRt.gameObject.AddComponent<Image>();
        bg.color = metroStyle.cardBgColor;
        bg.raycastTarget = false;

        var outline = metroLegendRt.GetComponent<Outline>() ?? metroLegendRt.gameObject.AddComponent<Outline>();
        outline.effectColor = metroStyle.cardBorderColor;
        outline.effectDistance = new Vector2(1.5f, -1.5f);

        if (metroLegendCollapsed)
        {
            const float dotSize = 74f;
            metroLegendRt.sizeDelta = new Vector2(dotSize, dotSize);
            bg.sprite = spriteCircle;
            bg.type = Image.Type.Simple;

            var dotBtnGo = Rect("LegendDotBtn", metroLegendRt, 0f, 0f, dotSize, dotSize);
            var dotImg = Img(dotBtnGo, new Color(metroStyle.cardBgColor.r, metroStyle.cardBgColor.g, metroStyle.cardBgColor.b, 0.98f), spriteCircle);
            var dotBtn = dotBtnGo.AddComponent<Button>();
            dotBtn.targetGraphic = dotImg;
            dotBtn.onClick.AddListener(() => { metroLegendCollapsed = false; RefreshMetroLegend(); });

            // 小圆点内三色图例微缩线徽记 + 图例小字
            Color c0 = metroLastLayout.legends.Count > 0 ? metroLastLayout.legends[0].color : Indigo;
            Color c1 = metroLastLayout.legends.Count > 1 ? metroLastLayout.legends[1].color : Gold;
            Img(Rect("DotBar0", dotBtnGo.transform, 19f, 43f, 36f, 6f), c0, spriteRound).raycastTarget = false;
            Img(Rect("DotBar1", dotBtnGo.transform, 19f, 31f, 36f, 6f), c1, spriteRound).raycastTarget = false;
            CreateCrispUiText(dotBtnGo.transform, "DotLbl", "图例", 18, metroStyle.labelColor, 6f, 6f, dotSize - 12f, 22f, TextAnchor.MiddleCenter, true);
            return;
        }

        int showCount = Mathf.Min(12, metroLastLayout.legends.Count);
        const float cardW = 420f;
        const float rowStep = 44f;
        float cardH = 88f + showCount * rowStep + (showCount == 0 ? 48f : 18f);
        metroLegendRt.sizeDelta = new Vector2(cardW, cardH);
        bg.sprite = spriteRound;
        bg.type = Image.Type.Sliced;

        string legendTitle = string.IsNullOrEmpty(metroLastLayout.legendTitle)
            ? metroLastLayout.scopeTitle + "线路"
            : metroLastLayout.legendTitle;
        var titleTxt = CreateCrispUiText(metroLegendRt, "LegendTitle", legendTitle, 32, metroStyle.labelColor, 22, cardH - 64f, cardW - 92f, 44f, TextAnchor.MiddleLeft, true);
        if (titleTxt) { titleTxt.horizontalOverflow = HorizontalWrapMode.Overflow; titleTxt.verticalOverflow = VerticalWrapMode.Truncate; }

        // 右上角一键收纳成小圆点按钮
        var minBtnGo = Rect("CollapseLegendBtn", metroLegendRt, cardW - 60f, cardH - 60f, 44f, 44f);
        var minImg = Img(minBtnGo, new Color(metroStyle.labelColor.r, metroStyle.labelColor.g, metroStyle.labelColor.b, 0.12f), spriteCircle);
        var minBtn = minBtnGo.AddComponent<Button>();
        minBtn.targetGraphic = minImg;
        minBtn.onClick.AddListener(() => { metroLegendCollapsed = true; RefreshMetroLegend(); });
        CreateCrispUiText(minBtnGo.transform, "MinGlyph", "—", 24, metroStyle.labelColor, 0f, 0f, 44f, 44f, TextAnchor.MiddleCenter, true);

        if (showCount == 0)
        {
            CreateCrispUiText(metroLegendRt, "LegendEmpty", "当前区域暂未开通线路", 26, metroStyle.labelColor, 22, 24, cardW - 44f, 38f, TextAnchor.MiddleLeft, false);
            return;
        }

        float rowY = cardH - 112f;
        for (int i = 0; i < showCount; i++)
        {
            var item = metroLastLayout.legends[i];
            var barGo = Rect("Bar_" + i, metroLegendRt, 22f, rowY + 12f, 38f, 14f);
            var bImg = Img(barGo, item.color, spriteRound);
            bImg.raycastTarget = false;

            var rowTxt = CreateCrispUiText(metroLegendRt, "LegendItem_" + i, item.lineName, 28, metroStyle.labelColor, 72f, rowY, cardW - 90f, 38f, TextAnchor.MiddleLeft, false);
            if (rowTxt) { rowTxt.horizontalOverflow = HorizontalWrapMode.Overflow; rowTxt.verticalOverflow = VerticalWrapMode.Truncate; }
            rowY -= rowStep;
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

        bool isMarsAll = string.Equals(metroRegionFilter, "火星·全境", StringComparison.Ordinal)
                      || string.Equals(metroRegionFilter, "火星", StringComparison.Ordinal);
        float fitScale = (isMarsAll && !rotated)
            ? Mathf.Min((vw * 1.55f) / effW, (vh * 0.72f) / effH)
            : Mathf.Min((vw * 0.88f) / effW, (vh * 0.78f) / effH);
        float scale = Mathf.Clamp(fitScale, (isMarsAll && !rotated) ? 0.52f : 0.22f, 3.2f);
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
/// 城市星轨图专用自由缩放拖拽控制器：允许放大后将画布任意一侧拖至屏幕中央，不会锁死边缘。
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
