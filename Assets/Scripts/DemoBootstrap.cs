
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public partial class DemoBootstrap : MonoBehaviour
{
    // ---------- design ----------
    const float W = 1080f, H = 1920f;
    const float MAP_X = 0f, MAP_Y = 173f, MAP_W = 1080f, MAP_H = 1574f;
    const float BAND_T = 0.09f, BAND_H = 0.82f;
    static Color Paper = new Color(0.965f, 0.937f, 0.890f);
    static Color Paper2 = new Color(0.925f, 0.886f, 0.816f);
    static Color CardBg = new Color(0.992f, 0.982f, 0.955f, 0.96f);
    static Color HeaderBg = new Color(0.952f, 0.922f, 0.870f);
    static Color Ink = new Color(0.184f, 0.165f, 0.149f);
    static Color InkSoft = new Color(0.42f, 0.39f, 0.35f);
    static Color Vermilion = new Color(0.784f, 0.333f, 0.239f);
    static Color Indigo = new Color(0.231f, 0.353f, 0.549f);
    static Color Matcha = new Color(0.435f, 0.627f, 0.353f);
    static Color Gold = new Color(0.851f, 0.663f, 0.306f);
    static Color Sakura = new Color(0.910f, 0.627f, 0.706f);
    static Color Sea = new Color(0.663f, 0.788f, 0.808f);
    static readonly Color[] LineColors = {
        new Color(0.78f,0.33f,0.24f), new Color(0.30f,0.62f,0.47f),
        new Color(0.24f,0.45f,0.72f), new Color(0.88f,0.52f,0.20f),
        new Color(0.52f,0.36f,0.65f), new Color(0.45f,0.60f,0.72f)
    };

    [Serializable] public class Station {
        public int id;
        public string name, customName; public float x, y; public int level; public bool built, closed; public int trafficEvent;
        public bool portal, county; public string portalTo, portalTo2, city; public int administrativeCode; public string region = "月之亮面"; public float otherX, otherY, thirdX, thirdY;
        public float riderBonus; public int facLevel;
        public Station(string n,float px,float py){name=n;x=px;y=py;level=1;}
    }
    [Serializable] public class Scenery { public string name; public float x,y; public bool natural; public bool found, unlocked; public string triggerStation; public string region = "月之亮面"; }
    [Serializable] public class Line {
        public int id; public string name; public Color color; public int skin = -1; public List<int> st = new List<int>();
        public int trains = 1;
        public bool hasBaseTrain = true;
        public int[] dispatch = { 1, 1, 1, 1 }; public float fare = 1.5f;
        public float todayGross, todayMaintenance, todayTrack;
        public List<int> recent = new List<int>(); // 最近几个结算日的实收
        public int todayRevenue;
    }

    // ---------- state ----------
    Font font;
    Sprite spriteRound, spriteCircle, spriteWhite, spriteDiamond, spriteRing, spriteStationRing, spriteHandStroke;
    Canvas canvas; RectTransform root, mapRect, lineLayer, stationLayer, trainLayer, floaterLayer, previewLayer;
    MapWheelZoom mapWheelZoom;
    GraphicRaycaster raycaster;
    double coins = 2378d;
    float diamonds = 20f, rp = 60f, progress = 0.08f;
    float income = 0f;
    List<Station> stations = new List<Station>();
    List<Scenery> sceneries = new List<Scenery>();
    List<Line> lines = new List<Line>();
    Dictionary<int, Image> stationImgs = new Dictionary<int, Image>();
    readonly Dictionary<int, Text> stationLabels = new Dictionary<int, Text>();
    readonly Dictionary<int, Text> stationDestinations = new Dictionary<int, Text>();
    readonly Dictionary<int, Image> stationLeaders = new Dictionary<int, Image>();
    readonly Dictionary<int, Image> pressRings = new Dictionary<int, Image>();
    Dictionary<int, LineVisual> lineVisuals = new Dictionary<int, LineVisual>();
    List<string> techDone = new List<string>();
    Text coinTxt, incomeTxt, diamondTxt, progTxt, rpTxt, dayTxt;
    string currentRegion = "月之亮面"; Text regionTitle; GameObject hkMapArt, macauMapArt, guangdongMapArt;
    bool macauUnlocked, guangdongUnlocked;
    const float MacauIncomeRequired = 600f, GuangdongIncomeRequired = 1200f;
    RectTransform progFill;
    GameObject activePanel, scrim, toastGo, panelShadow, zoomNotice;
    int stationFilter; Text stationFilterText;
    Text zoomNoticeTxt;
    Action panelClosed;
    int panelOpenedFrame = -1;
    string activePanelKey = "";
    Text toastTxt; float toastTimer;
    bool toastTrackingForecast;
    float forecastStartUnscaledTime, lastForecastDuration = 0.8f;
    bool dragging;
    List<int> draft = new List<int>();
    readonly List<float> draftSegNeeds = new List<float>();
    int draftCost; Image previewImg;
    int nextLineId = 2;
    List<Scenery> pendingCheckin = new List<Scenery>();
    GameObject sceneryBadge;
    RectTransform odArcLayer;
    readonly List<RectTransform> odArcDashes = new List<RectTransform>();
    readonly List<Image> odArcDashImgs = new List<Image>();
    readonly List<RectTransform> odArcMarkers = new List<RectTransform>();
    int odFocusStation = -1;
    float draftPreviewNeed = 0.5f;
    Color draftPreviewColor = new Color(0.95f, 0.58f, 0.32f, 0.90f);
    [Serializable]
    sealed class DailyReportSnapshot
    {
        public int settledDay, bottleneckStation = -1;
        public float gross, commercialRent, maintAndTax, netProfit, passengers;
        public float bestLineNet, worstLineNet, crowdLost, unreachedLost, elasticLost;
        public string bestLineName, worstLineName, bottleneckNote;
    }
    DailyReportSnapshot yesterdayReport;
    bool hasYesterdayReport;

    // 主题与 IAA 激励状态
    GameObject paperBgGo, topBarGo, stationFilterGo, solarTermRailGo, alertRailGo, alertRailBadge, speedBoardGo, noticeBoardGo, quickUpgradeGo, noticeUnreadDot, lineLoadLegendGo;
    Text solarTermRailTitle, solarTermRailSub, alertRailSub;
    int lastReadNoticeDay = -1;
    const string CurrentPatchNoteVersion = "2026.11.23";
    string readPatchNoteVersion = "";
    bool ringsDirty = true;
    bool trainsDirty = true;
    int lastDisplayedRp = int.MinValue;
    float lastResourceUnloadTime = -99f;
    Coroutine resourceTrimCo;
    readonly List<int> pulsingRingIndices = new List<int>();
    readonly List<Shadow> managedShadows = new List<Shadow>();
    RawImage hkMapRaw, macauMapRaw, guangdongMapRaw, topFadeImg, botFadeImg;
    RawImage[] mapEdgeFeathers;
    RectTransform topBarCoinRt, topBarCapRt, topBarDiaRt, topBarGearRt, topBarHelpRt;
    float lastRespW = -1f, lastRespH = -1f;
    Rect lastRespSafe;
    MapBackdropGraphic backdropGraphic;
    readonly List<GameObject> leftRailGos = new List<GameObject>();
    int currentTheme = 0;
    readonly HashSet<int> ownedThemes = new HashSet<int> { 0 };
    readonly Dictionary<string, int> adWatchCounts = new Dictionary<string, int>();
    readonly Dictionary<int, long> themeTrialUntil = new Dictionary<int, long>();
    string companyName = "深空轨社";
    int companySeal = 0;
    int companyPolicy = 0;
    int bondDaysLeft = 0;
    int lastPolicyDay = -1;
    int lastAdSubsidyDay = -1;
    int dailyTaskClaimedDay = -1;
    int bondPrincipal = 0;
    int bondDailyDue = 0;
    int bondsRepaid = 0;
    bool bondSubsidized = false;
    readonly List<int> boardPartners = new List<int>();
    int consultantId = -1;
    int consultantDaysLeft = 0;
    int economyCycle = 0;
    int nextEconomyCycle = 1;
    int turboDaysLeft = 0;
    int autoDispatchDaysLeft = 0;
    int lastAwardDay = 0;
    int companyTab = 0;
    int nextContractId = 1;
    [Serializable]
    class MunicipalContract
    {
        public int id, type, startValue, targetValue, rewardCoins, rewardRp, rewardDiamonds, deadlineDay;
        public bool claimed;
    }
    readonly List<MunicipalContract> activeContracts = new List<MunicipalContract>();

    class LineSegmentVisual
    {
        public int from, to;
        public Vector2 basePos, baseOffset;
        public GameObject casing, body, core, hit;
        public Image coreImg;
        public Color baseCoreColor;
        public void SetVisible(bool visible)
        {
            if (casing && casing.activeSelf != visible) casing.SetActive(visible);
            if (body && body.activeSelf != visible) body.SetActive(visible);
            if (core && core.activeSelf != visible) core.SetActive(visible);
            if (hit && hit.activeSelf != visible) hit.SetActive(visible);
        }
    }
    class CachedTrainSegment
    {
        public int stationA, stationB;
        public string region;
        public Vector2 posA, posB, baseOffset;
        public float length, angleDeg;
    }
    class LineVisual
    {
        public List<LineSegmentVisual> segments = new List<LineSegmentVisual>();
        public readonly List<CachedTrainSegment> cachedSegs = new List<CachedTrainSegment>();
        public float cachedTotalLength;
        public readonly List<GameObject> trains = new List<GameObject>();
        public readonly List<RectTransform> trainRts = new List<RectTransform>();
        public readonly List<bool> trainActive = new List<bool>();
        public readonly List<int> trainLastSt = new List<int>();
        public float t;
        public float credit;
    }
    // 地图放大时要按 1/zoom 反向补偿厚度的矩形（线路段、点击热区、草稿线）
    class ZoomRect { public RectTransform rt; public float baseWidth; public ZoomRect(RectTransform rt, float baseWidth) { this.rt = rt; this.baseWidth = baseWidth; } }
    readonly List<ZoomRect> zoomRects = new List<ZoomRect>();
    float overlayZoom = 1f;

    // ---------- boot ----------
    void OnEnable()
    {
        if (mapRect) mapWheelZoom = mapRect.GetComponent<MapWheelZoom>();
        // 关键防御：应对 Unity 热重载 (Domain Reload)
        if (stations == null || stations.Count == 0)
        {
            BuildData();
        }
        if (lines == null || lines.Count == 0)
        {
            RestoreGame();
        }
        saveBlocked = false;
        saveReady = true;
        if (!IsThemeAvailable(currentTheme)) currentTheme = 0;
        ApplyThemeColors(currentTheme);
        EnsureCompanyInitialized();
        if (canvas != null && lines != null && lines.Count > 0)
        {
            ApplyTheme(currentTheme, false);
            if (lineVisuals == null) lineVisuals = new Dictionary<int, LineVisual>();
            CreateLineVisuals();
            RefreshAllLines();
        }
    }
    void Start()
    {
        qualityTier = Mathf.Clamp(PlayerPrefs.GetInt("visual_quality", 1), 0, 2);
        selectedFrameRate = PlayerPrefs.GetInt("target_fps", 120);
        if (selectedFrameRate != 30 && selectedFrameRate != 60 && selectedFrameRate != 120 && selectedFrameRate != -1) selectedFrameRate = 120;
        fontSizeTier = Mathf.Clamp(PlayerPrefs.GetInt("font_size_tier", 1), 0, FontSizeScales.Length - 1);
        font = Resources.Load<Font>("Fonts/LXGWWenKai-Regular");
        if (font == null) { Debug.LogError("FONT MISSING"); font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); }
        spriteWhite = MakeWhite();
        spriteRound = MakeRounded(96, 24);
        spriteCircle = MakeCircle(128);
        spriteDiamond = MakeDiamond(96);
        spriteRing = MakeRing(96, 6f);
        spriteStationRing = MakeHandDrawnRing(96, 7f);
        spriteHandStroke = MakeHandDrawnStroke(192, 36);
        BuildData();
        bool restored = RestoreGame();
        if (!restored) dailyStationBaseline = DailyConnectedCount();
        EnsureCompanyInitialized();
        if (!IsThemeAvailable(currentTheme)) currentTheme = 0;
        ApplyThemeColors(currentTheme);
        BuildUI();
        ApplyTheme(currentTheme, false);
        ApplyVisualQuality();
        CheckSceneryOnLine(null);
        RecalcIncome();
        RefreshHUD();
        InitMobile();
        SyncStoryProgression(true);
        if (!string.IsNullOrEmpty(savedRegion) && savedRegion != currentRegion && RegionUnlocked(savedRegion)) SwitchRegion(savedRegion);
        saveReady = true;
        EnsureTapTapSdkBridge();
        if (saveBlocked) Toast("存档读取失败，原存档已保留");
        Debug.Log("COASTAL RAIL READY");
    }

    void BuildData()
    {
        stations.Clear();
        sceneries.Clear();
        // 月之亮面：真实月球正面经纬度 (-35°~+35°, -62°~+62°)
        AddMoonNear("静海基地", 23.5f, 8.5f);
        AddMoonNear("阿里亚德港", 17.3f, 6.5f);
        AddMoonNear("澄海枢纽", 17.5f, 26.0f);
        AddMoonNear("阿波罗11湾", 23.5f, 0.7f);
        AddMoonNear("马斯克林湾", 30.1f, 2.2f);
        AddMoonNear("危海西港", 34.0f, 16.1f);
        AddMoonNear("酒海前哨", 32.0f, -15.2f);
        AddMoonNear("皮科洛米尼", 32.2f, -29.7f);
        AddMoonNear("哥白尼环形山", -20.0f, 9.7f);
        AddMoonNear("艾拉托斯特尼", -11.3f, 14.5f);
        AddMoonNear("开普勒港", -33.0f, 8.1f);
        AddMoonNear("雨海前哨", -15.6f, 32.8f);
        AddMoonNear("阿基米德站", -4.0f, 29.7f);
        AddMoonNear("亚平宁岭", -3.7f, 18.9f);
        AddMoonNear("汽海枢纽", 3.6f, 13.3f);
        AddMoonNear("中央湾", 2.1f, 1.8f);
        AddMoonNear("知海站", -20.9f, -10.5f);
        AddMoonNear("云海港", -17.2f, -21.3f);
        AddMoonNear("第谷环港", -11.2f, -43.3f);
        AddMoonNear("沃尔特站", 1.0f, -33.2f);
        AddMoonNear("斯托弗勒", 6.0f, -41.1f);
        AddMoonNear("克拉维乌斯", -14.1f, -58.4f);
        AddMoonNear("高加索山", 10.0f, 38.4f);
        AddMoonNear("梦湖驿站", 32.8f, 38.0f);
        AddMoonNear("死湖前哨", 24.4f, 45.0f);
        AddMoonNear("阿尔卑斯谷", 3.2f, 48.5f);
        AddMoonNear("冷海北站", 1.4f, 56.0f);

        var luo = new Station("柏拉图跃迁港", MoonNearX(-9.3f), MoonNearY(51.6f));
        luo.portal = true; luo.portalTo = "火星"; luo.otherX = 0.70f; luo.otherY = 0.25f;
        stations.Add(luo);

        var hz = new Station("虹湾星门", MoonNearX(-31.5f), MoonNearY(44.1f));
        hz.portal = true; hz.portalTo = "火星"; hz.otherX = 0.535f; hz.otherY = 0.400f;
        hz.portalTo2 = "月背"; hz.thirdX = MoonFarX(163.9f); hz.thirdY = MoonFarY(50.8f);
        stations.Add(hz);

        // 月背：真实月球背面经纬度 (145°E~215°E, -62°~+62°)
        AddMoonFar("冯·卡门港", 176.2f, -44.8f);
        AddMoonFar("艾特肯深井", 173.4f, -16.8f);
        AddMoonFar("智海港", 168.0f, -12.3f);
        AddMoonFar("加加林环", 149.2f, -20.2f);
        AddMoonFar("巴甫洛夫站", 146.5f, -28.6f);
        AddMoonFar("伯克兰港", 173.9f, -30.2f);
        AddMoonFar("南极深井", 175.0f, -52.0f);
        AddMoonFar("莱曼前哨", 163.6f, -59.5f);
        AddMoonFar("普朗克站", 145.5f, -57.3f);
        AddMoonFar("德弗里斯", 183.3f, -19.9f);
        AddMoonFar("奥本海默", 193.7f, -35.2f);
        AddMoonFar("博尔曼湾", 211.8f, -38.8f);
        AddMoonFar("科罗廖夫环", 202.6f, -4.0f);
        AddMoonFar("马赫驿站", 210.7f, 18.5f);
        AddMoonFar("莫斯科海", 147.9f, 27.3f);
        AddMoonFar("白贝罗港", 165.6f, 36.7f);
        AddMoonFar("坎贝尔站", 151.4f, 45.3f);

        AddMoonFarGateway("达朗贝尔门", 163.9f, 50.8f, 0.466f, 0.416f);
        AddMoonFarGateway("阿波罗星门", 208.2f, -36.1f, 0.474f, 0.402f);

        ApplyGuangdongStations();
        var marsNorth = stations.Find(s => s.region == "火星" && s.name == "阿西达利亚海");
        if (marsNorth != null)
        {
            marsNorth.name = "阿西达利亚（月背门）"; marsNorth.portal = true; marsNorth.portalTo = "月背";
            marsNorth.otherX = MoonFarX(163.9f); marsNorth.otherY = MoonFarY(50.8f);
        }
        luo.otherX = 0.585f; luo.otherY = 0.466f; luo.administrativeCode = 200101; luo.city = "塔尔西斯星区";
        var marsPort = stations.Find(s => s.region == "火星" && s.name == "奥林匹斯港");
        if (marsPort != null)
        {
            marsPort.name = "奥林匹斯（月面门）"; marsPort.portal = true; marsPort.portalTo = "月之亮面";
            marsPort.otherX = MoonNearX(-9.3f); marsPort.otherY = MoonNearY(51.6f);
        }

        // 月之亮面奇观（9处）
        sceneries.Add(new Scenery{name="静海纪念碑", x=MoonNearX(23.5f), y=MoonNearY(0.7f), natural=false, triggerStation="阿波罗11湾"});
        sceneries.Add(new Scenery{name="哥白尼中央峰", x=MoonNearX(-20.0f), y=MoonNearY(9.7f), natural=true, triggerStation="哥白尼环形山"});
        sceneries.Add(new Scenery{name="第谷辐射纹", x=MoonNearX(-11.2f), y=MoonNearY(-43.3f), natural=true, triggerStation="第谷环港"});
        sceneries.Add(new Scenery{name="亚平宁绝壁", x=MoonNearX(-3.7f), y=MoonNearY(18.9f), natural=true, triggerStation="亚平宁岭"});
        sceneries.Add(new Scenery{name="阿尔卑斯月谷", x=MoonNearX(3.2f), y=MoonNearY(48.5f), natural=true, triggerStation="阿尔卑斯谷"});
        sceneries.Add(new Scenery{name="雨海熔岩海", x=MoonNearX(-15.6f), y=MoonNearY(32.8f), natural=true, triggerStation="雨海前哨"});
        sceneries.Add(new Scenery{name="虹湾金弧", x=MoonNearX(-31.5f), y=MoonNearY(44.1f), natural=true, triggerStation="虹湾星门"});
        sceneries.Add(new Scenery{name="克拉维乌斯穹顶", x=MoonNearX(-14.1f), y=MoonNearY(-58.4f), natural=false, triggerStation="克拉维乌斯"});
        sceneries.Add(new Scenery{name="柏拉图黑曜台", x=MoonNearX(-9.3f), y=MoonNearY(51.6f), natural=false, triggerStation="柏拉图跃迁港"});

        // 月背奇观（5处）
        sceneries.Add(new Scenery{name="南极艾特肯深渊", x=MoonFarX(173.4f), y=MoonFarY(-16.8f), natural=true, triggerStation="艾特肯深井", region="月背"});
        sceneries.Add(new Scenery{name="冯·卡门着陆碑", x=MoonFarX(176.2f), y=MoonFarY(-44.8f), natural=false, triggerStation="冯·卡门港", region="月背"});
        sceneries.Add(new Scenery{name="莫斯科海玄武原", x=MoonFarX(147.9f), y=MoonFarY(27.3f), natural=true, triggerStation="莫斯科海", region="月背"});
        sceneries.Add(new Scenery{name="科罗廖夫冰丘", x=MoonFarX(202.6f), y=MoonFarY(-4.0f), natural=true, triggerStation="科罗廖夫环", region="月背"});
        sceneries.Add(new Scenery{name="阿波罗环山观测塔", x=MoonFarX(208.2f), y=MoonFarY(-36.1f), natural=false, triggerStation="阿波罗星门", region="月背"});

        var l1 = new Line { id = 1, name = "1 号星轨", color = LineColors[0] };
        l1.st.Add(0); l1.st.Add(1); l1.st.Add(2);
        lines.Add(l1);
        foreach (int index in l1.st) stations[index].built = true;
        RebuildRegionStationIndexCache();
    }
    readonly Dictionary<string, List<int>> regionStationIndices = new Dictionary<string, List<int>>();
    void RebuildRegionStationIndexCache()
    {
        regionStationIndices.Clear();
        for (int i = 0; i < stations.Count; i++)
        {
            stations[i].id = i;
            AddStationToRegionCache(stations[i].region, i);
            if (stations[i].portal)
            {
                if (!string.IsNullOrEmpty(stations[i].portalTo)) AddStationToRegionCache(stations[i].portalTo, i);
                if (!string.IsNullOrEmpty(stations[i].portalTo2)) AddStationToRegionCache(stations[i].portalTo2, i);
            }
        }
    }
    void AddStationToRegionCache(string region, int stationIndex)
    {
        if (string.IsNullOrEmpty(region)) return;
        if (!regionStationIndices.TryGetValue(region, out var list))
        {
            list = new List<int>();
            regionStationIndices[region] = list;
        }
        if (!list.Contains(stationIndex)) list.Add(stationIndex);
    }
    List<int> GetRegionStationIndices(string region)
    {
        if (string.IsNullOrEmpty(region) || !regionStationIndices.TryGetValue(region, out var list)) return null;
        return list;
    }
    void Add(string n, float x, float y, string region = "月之亮面") { stations.Add(new Station(n, x, y) { region = region }); }
    // 月之亮面底图范围：经度 -35°~+35°，纬度 -62°~+62°。
    float MoonNearX(float longitude) { return Mathf.Clamp((longitude + 35.0f) / 70.0f, 0.06f, 0.94f); }
    float MoonNearY(float latitude) { return Mathf.Clamp((latitude + 62.0f) / 124.0f, 0.14f, 0.86f); }
    void AddMoonNear(string n, float longitude, float latitude) { Add(n, MoonNearX(longitude), MoonNearY(latitude), "月之亮面"); }

    // 月背底图范围：经度 145°E~215°E，纬度 -62°~+62°。
    float MoonFarX(float longitude) { return Mathf.Clamp((longitude - 145.0f) / 70.0f, 0.06f, 0.94f); }
    float MoonFarY(float latitude) { return Mathf.Clamp((latitude + 62.0f) / 124.0f, 0.14f, 0.86f); }
    void AddMoonFar(string n, float longitude, float latitude) { Add(n, MoonFarX(longitude), MoonFarY(latitude), "月背"); }

    [Serializable] sealed class GuangdongStationList { public GuangdongStationRecord[] stations; }
    [Serializable] sealed class GuangdongStationRecord { public string name, city; public int code; public float x, y; public bool legacy; }
    void ApplyGuangdongStations()
    {
        var file = Resources.Load<TextAsset>("MapData/mars_stations");
        if (!file) { Debug.LogError("火星星港站点数据缺失"); return; }
        var data = JsonUtility.FromJson<GuangdongStationList>(file.text);
        foreach (var record in data.stations)
        {
            var station = record.legacy ? stations.Find(s => s.region == "火星" && s.name == record.name) : null;
            if (station == null) { station = new Station(record.name, record.x, record.y) { region = "火星" }; stations.Add(station); }
            station.x = record.x; station.y = record.y; station.city = record.city;
            station.county = !record.legacy; station.administrativeCode = record.code;
        }
    }
    void SetMacauGateway(string name, float x, float y)
    {
        var station = stations.Find(s => s.region == "月背" && s.name == name);
        if (station == null) return;
        station.portal = true; station.portalTo = "火星"; station.otherX = x; station.otherY = y;
    }
    void AddMoonFarGateway(string name, float longitude, float latitude, float x, float y)
    {
        AddMoonFar(name, longitude, latitude); SetMacauGateway(name, x, y);
    }
    // 玩家界面统一使用别称。
    static string DisplayPlaceAliases(string text)
    {
        return text;
    }
    string StationDisplayName(int index, string region)
    {
        var station = stations[index];
        if (!string.IsNullOrEmpty(station.customName)) return station.customName;
        return station.name == "奥林匹斯（月面门）" && region == "月之亮面" ? "月面门（奥林匹斯）" : station.name;
    }
    string PortalSubtitle(Station station, string region)
    {
        if (station.name == "奥林匹斯（月面门）") return region == "月之亮面" ? "奥林匹斯港" : "月之亮面";
        if (station.name == "虹湾星门")
            return region == "月背" ? "火星/月之亮面" : region == "月之亮面" ? "火星/月背" : "月之亮面/月背";
        if (station.region == "月背") return region == "月背" ? "火星" : "月背";
        return region == station.region ? station.portalTo : station.region;
    }

    void BuildUI()
    {
        EnsureCamera();
        var cgo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvas = cgo.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        // 画布自适应：宽屏按高度、窄屏按宽度，保证 1080x1920 设计内容永远完整可见
        cgo.AddComponent<CanvasLetterbox>().designSize = new Vector2(W, H);
        raycaster = cgo.GetComponent<GraphicRaycaster>();
        if (UnityEngine.Object.FindAnyObjectByType<EventSystem>() == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        // 铺满整屏的海面底，letterbox 区域不露相机纯色
        var backdrop = Rect("Backdrop", cgo.transform, 0, 0, 0, 0);
        var backdropRt = backdrop.GetComponent<RectTransform>();
        backdropRt.anchorMin = Vector2.zero; backdropRt.anchorMax = Vector2.one;
        backdropRt.pivot = new Vector2(0.5f, 0.5f);
        backdropRt.offsetMin = Vector2.zero; backdropRt.offsetMax = Vector2.zero;
        backdropGraphic = backdrop.AddComponent<MapBackdropGraphic>();
        backdropGraphic.raycastTarget = false;

        // 主舞台自适应铺满整个 Canvas（兼容 9:16、20:9 全面屏、3:4 平板与刘海安全区）
        var stage = Rect("Stage", cgo.transform, 0, 0, W, H);
        var stageRt = stage.GetComponent<RectTransform>();
        stageRt.anchorMin = Vector2.zero; stageRt.anchorMax = Vector2.one;
        stageRt.pivot = new Vector2(0.5f, 0.5f);
        stageRt.offsetMin = Vector2.zero;
        stageRt.offsetMax = Vector2.zero;
        root = stageRt;

        var bg = Rect("Paper", root, 0, 0, W, H); Img(bg, Sea).raycastTarget = false;
        var bgRt = bg.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
        bgRt.pivot = new Vector2(0.5f, 0.5f);
        bgRt.offsetMin = Vector2.zero; bgRt.offsetMax = Vector2.zero;
        paperBgGo = bg;

        BuildMap();
        BuildStationViews();
        BuildSceneryViews();
        BuildTopBar();
        BuildZoomNotice();
        BuildLeftRail();
        BuildStationFilterButton();
        BuildLeftReminderEntries();
        BuildRightRail();
        BuildBottomNav();
        BuildUndoButton();
        BuildLineLoadLegend();
        BuildStoryObjectiveBanner();
        BuildDragCostBadge();
        CreateLineVisuals();
        RefreshAllLines();
        RefreshFeatureGateVisuals();
        Canvas.ForceUpdateCanvases();
        ApplyResponsiveLayout(true);

    }

    void EnsureCamera()
    {
        if (Camera.main != null) return;
        var camGo = new GameObject("UICamera", typeof(Camera));
        camGo.tag = "MainCamera";
        camGo.transform.SetParent(transform, false);
        var cam = camGo.GetComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.66f, 0.79f, 0.81f);
        cam.cullingMask = 0;
        cam.orthographic = true;
        cam.depth = -10;
        if (UnityEngine.Object.FindAnyObjectByType<AudioListener>() == null)
            camGo.AddComponent<AudioListener>();
    }

    static void AttachSubCanvas(GameObject go, bool interactive)
    {
        if (!go) return;
        var c = go.GetComponent<Canvas>();
        if (!c) c = go.AddComponent<Canvas>();
        c.overrideSorting = false;
        if (interactive && !go.GetComponent<GraphicRaycaster>())
            go.AddComponent<GraphicRaycaster>();
    }

    void RequestResourceTrim(bool force)
    {
        float now = Time.unscaledTime;
        if (!force && now - lastResourceUnloadTime < 8f) return;
        lastResourceUnloadTime = now;
        if (resourceTrimCo != null) StopCoroutine(resourceTrimCo);
        resourceTrimCo = StartCoroutine(CoResourceTrim());
    }

    System.Collections.IEnumerator CoResourceTrim()
    {
        yield return null;
        var op = Resources.UnloadUnusedAssets();
        while (op != null && !op.isDone) yield return null;
        resourceTrimCo = null;
    }

    void BuildTopBar()
    {
        var bar = Rect("Top", root, 0, 1610, W, 280); Img(bar, new Color(1,1,1,0)).raycastTarget = false;
        topBarGo = bar;
        AttachSubCanvas(bar, true);

        var c1 = Rect("Coin", bar.transform, 24, 46, 318, 92); Rounded(c1, CardBg); SoftShadow(c1);
        topBarCoinRt = c1.GetComponent<RectTransform>();
        var coinBtn = c1.AddComponent<Button>(); coinBtn.targetGraphic = c1.GetComponent<Image>();
        coinBtn.onClick.AddListener(OpenDailyBriefingPopup);
        var coinRing = Rect("CoinRing", c1.transform, 12, 18, 56, 56); Img(coinRing, new Color(0.25f,0.21f,0.17f,0.88f), spriteStationRing).raycastTarget = false;
        var coinIcon = Rect("Icon", coinRing.transform, 4, 4, 48, 48); Img(coinIcon, Gold, spriteCircle).raycastTarget = false;
        coinTxt = Txt(c1.transform, FormatCoins(coins), 42, Ink, 78, 44, 224, 42, TextAnchor.MiddleLeft);
        coinTxt.resizeTextForBestFit = true; coinTxt.resizeTextMinSize = 22; coinTxt.resizeTextMaxSize = ScaledFontSize(42, 1.15f); coinTxt.verticalOverflow = VerticalWrapMode.Truncate; coinTxt.raycastTarget = false;
        incomeTxt = Txt(c1.transform, "预计日利润 +240", 24, InkSoft, 78, 8, 232, 34, TextAnchor.MiddleLeft);
        incomeTxt.resizeTextForBestFit = true; incomeTxt.resizeTextMinSize = 18; incomeTxt.resizeTextMaxSize = ScaledFontSize(25, 1.15f); incomeTxt.verticalOverflow = VerticalWrapMode.Truncate; incomeTxt.raycastTarget = false;

        var cap = Rect("Cap", bar.transform, 354, 26, 400, 162); Rounded(cap, CardBg); SoftShadow(cap);
        topBarCapRt = cap.GetComponent<RectTransform>();
        var tape = Rect("CapTape", cap.transform, 130, 150, 140, 24);
        var tapeIm = Img(tape, new Color(0.88f, 0.78f, 0.62f, 0.75f), spriteRound); tapeIm.type = Image.Type.Sliced; tapeIm.raycastTarget = false;
        tape.transform.localRotation = Quaternion.Euler(0, 0, -1.5f);
        string regTitle = currentRegion == "月之亮面" ? "月之亮面" : currentRegion == "月背" ? "月 背" : "火 星";
        var title = Txt(cap.transform, regTitle, 48, Ink, 0, 106, 400, 52, TextAnchor.MiddleCenter);
        title.fontSize = ScaledFontSize(48, 1.22f);
        regionTitle = title;
        var regionEntry = cap.AddComponent<Button>(); regionEntry.targetGraphic = cap.GetComponent<Image>();
        regionEntry.onClick.AddListener(OpenRegionScreen);
        TextShadow(title, new Color(1,0.99f,0.96f,0.8f));
        timeTxt = Txt(cap.transform, "08:00", 29, InkSoft, 0, 56, 400, 38, TextAnchor.MiddleCenter);
        dayTxt = Txt(cap.transform, "第 1 日", 26, InkSoft, 0, 16, 400, 34, TextAnchor.MiddleCenter);

        var c3 = Rect("Dia", bar.transform, 780, 46, 244, 92); Rounded(c3, CardBg); SoftShadow(c3);
        topBarDiaRt = c3.GetComponent<RectTransform>();
        var diaIcon = Rect("Icon", c3.transform, 12, 18, 52, 52); Img(diaIcon, Sakura, spriteDiamond).raycastTarget = false;
        diamondTxt = Txt(c3.transform, "20", 42, Ink, 66, 19, 102, 54, TextAnchor.MiddleCenter);
        Btn(c3.transform, "DiamondInfo", "?", 32, 172, 14, 62, 64, Matcha, () => Toast("钻石可通过名胜打卡和机车里程碑获得"));

        var gb = Btn(bar.transform, "Gear", "设置", 27, 24, -32, 86, 68, CardBg, OpenAudioSettings);
        topBarGearRt = gb.GetComponent<RectTransform>();
        gb.GetComponentInChildren<Text>().color = Ink;
        Img(Rect("StampTop", gb.transform, 10, 61, 66, 5), Indigo, spriteRound).raycastTarget = false;
        SoftShadow(gb.gameObject);
        var hb = Btn(bar.transform, "Help", "帮助", 27, 122, -32, 86, 68, CardBg, () => HelpPopup("长按车站拖到下一站即可铺线，拖回起点成环。\n已有线路端点拖出可延线，中间站拖出可建新线；长按线路拖至新站可插站。\n点开图鉴大图即完成名胜打卡；交界站可跨区衔接。\n右上可调运行倍速（1× 下 6 分钟为一游戏日）。"));
        topBarHelpRt = hb.GetComponent<RectTransform>();
        hb.GetComponentInChildren<Text>().color = InkSoft;
        Img(Rect("StampTop", hb.transform, 10, 61, 66, 5), Matcha, spriteRound).raycastTarget = false;
        SoftShadow(hb.gameObject);
        var mb = Btn(bar.transform, "MetroMap", "线路图", 27, 220, -32, 108, 68, CardBg, OpenMetroMapScreen);
        mb.GetComponentInChildren<Text>().color = Indigo;
        Img(Rect("StampTop", mb.transform, 10, 61, 88, 5), Vermilion, spriteRound).raycastTarget = false;
        SoftShadow(mb.gameObject);
        lastDisplayedMinute = -1; lastDisplayedDay = -1;
        lastHudCoinsText = ""; lastHudIncomeText = ""; lastHudDiamonds = int.MinValue; lastHudProgress = int.MinValue;
    }

    void BuildZoomNotice()
    {
        // 顶部轻提示：不占画面中央，只做状态说明
        zoomNotice = Rect("ZoomNotice", root, 270, 1452, 540, 60);
        var nbg = Img(zoomNotice, CardBg, spriteRound);
        nbg.type = Image.Type.Sliced;
        nbg.raycastTarget = false;
        SoftShadow(zoomNotice);
        zoomNoticeTxt = Txt(zoomNotice.transform, "已隐藏 0 个站点 · 双指放大", 25, Ink, 12, 4, 516, 52, TextAnchor.MiddleCenter);
        zoomNoticeTxt.raycastTarget = false;
        zoomNotice.SetActive(false);
    }

    void BuildMap()
    {
        var map = Rect("Map", root, 0, 0, W, H);
        mapRect = map.GetComponent<RectTransform>();
        Img(map, new Color(1, 1, 1, 0));
        var mapZoom = map.AddComponent<MapWheelZoom>();
        mapWheelZoom = mapZoom;
        mapZoom.map = mapRect; mapZoom.viewport = root;
        mapZoom.canZoom = () => !dragging && !lineInsertActive && pageRoot == null && activePanel == null && sceneryViewer == null;
        mapZoom.maxZoom = () => currentRegion == "火星" ? 12f : 2.5f;
        mapZoom.onPanStart = () => { pressing = false; pressIdx = -1; HidePressRing(); };
        hkMapArt = Rect("HongKongArt", map.transform, 0, 0, W, H);
        BuildRasterMap(hkMapArt.transform);
        guangdongMapArt = BuildGuangdongMap(map.transform);
        macauMapArt = BuildMacauMap(map.transform);
        guangdongMapArt.SetActive(false); macauMapArt.SetActive(false);

        // 底图四缘海面渐隐羽化带：仅柔化底图矩形接缝到全屏 Paper 海面，不遮挡线网与站点
        var fade = MakeFadeTexture();
        var featherGo = Rect("MapEdgeFeather", mapRect, 0, 0, W, H);
        var featherCol = new Color(Sea.r, Sea.g, Sea.b, 0.90f);
        var themeFadeCol = Themes[Mathf.Clamp(currentTheme, 0, Themes.Length - 1)].fade;
        var featherTopCol = new Color(themeFadeCol.r, themeFadeCol.g, themeFadeCol.b, 0.85f);
        var fTop = Rect("FeatherTop", featherGo.transform, 0, H - 72f, W, 72f).AddComponent<RawImage>();
        fTop.texture = fade; fTop.color = featherTopCol; fTop.raycastTarget = false;
        var fBot = Rect("FeatherBot", featherGo.transform, 0, 0, W, 72f).AddComponent<RawImage>();
        fBot.texture = fade; fBot.color = featherCol; fBot.raycastTarget = false; fBot.uvRect = new Rect(0, 1, 1, -1);
        var fLeftGo = Rect("FeatherLeft", featherGo.transform, 0, H, H, 56f);
        fLeftGo.transform.localRotation = Quaternion.Euler(0, 0, -90f);
        var fLeft = fLeftGo.AddComponent<RawImage>();
        fLeft.texture = fade; fLeft.color = featherCol; fLeft.raycastTarget = false; fLeft.uvRect = new Rect(0, 1, 1, -1);
        var fRightGo = Rect("FeatherRight", featherGo.transform, W - 56f, H, H, 56f);
        fRightGo.transform.localRotation = Quaternion.Euler(0, 0, -90f);
        var fRight = fRightGo.AddComponent<RawImage>();
        fRight.texture = fade; fRight.color = featherCol; fRight.raycastTarget = false;
        mapEdgeFeathers = new[] { fTop, fBot, fLeft, fRight };

        lineLayer = Rect("Lines", mapRect, 0, 0, W, H).GetComponent<RectTransform>();
        AttachSubCanvas(lineLayer.gameObject, true);
        odArcLayer = Rect("OdArcs", mapRect, 0, 0, W, H).GetComponent<RectTransform>();
        AttachSubCanvas(odArcLayer.gameObject, false);
        previewLayer = Rect("Preview", mapRect, 0, 0, W, H).GetComponent<RectTransform>();
        AttachSubCanvas(previewLayer.gameObject, false);
        var pv = Rect("PreviewLine", previewLayer, 0, 0, 1, 14.5f);
        pv.GetComponent<RectTransform>().pivot = new Vector2(0, 0.5f);
        previewImg = Img(pv, new Color(0.95f,0.58f,0.32f,0.88f), spriteHandStroke);
        previewImg.type = Image.Type.Sliced;
        previewImg.raycastTarget = false;
        pv.SetActive(false);
        var tipGo = Rect("DragTip", previewLayer, 0, 0, 34, 34);
        tipGo.GetComponent<RectTransform>().pivot = new Vector2(0.5f,0.5f);
        dragTip = Img(tipGo, new Color(0.95f,0.45f,0.28f,0.95f), spriteCircle);
        dragTip.raycastTarget = false; tipGo.SetActive(false);
        stationLayer = Rect("Stations", mapRect, 0, 0, W, H).GetComponent<RectTransform>();
        AttachSubCanvas(stationLayer.gameObject, true);
        trainLayer = Rect("Trains", mapRect, 0, 0, W, H).GetComponent<RectTransform>();
        AttachSubCanvas(trainLayer.gameObject, false);
        floaterLayer = Rect("IncomeFloaters", mapRect, 0, 0, W, H).GetComponent<RectTransform>();
        AttachSubCanvas(floaterLayer.gameObject, false);

        Label(hkMapArt.GetComponent<RectTransform>(), "北 岭", 34, 0.55f, 0.90f, new Color(0.40f,0.38f,0.32f,0.75f), 180);
        Label(hkMapArt.GetComponent<RectTransform>(), "月之亮面本岛", 26, 0.50f, 0.170f, new Color(0.40f,0.38f,0.32f,0.72f), 150);
        Label(hkMapArt.GetComponent<RectTransform>(), "苍岚岛", 24, 0.20f, 0.105f, new Color(0.40f,0.38f,0.32f,0.70f), 140);
        Label(hkMapArt.GetComponent<RectTransform>(), "银潮湾", 18, 0.40f, 0.360f, new Color(0.33f,0.48f,0.53f,0.80f), 190);
        PortalLabel(hkMapArt.GetComponent<RectTransform>(), 0.80f, 0.925f, "→ 火星");
        PortalLabel(hkMapArt.GetComponent<RectTransform>(), 0.25f, 0.505f, "→ 火星 / 月背");

        // 顶部/底部奶油渐隐，让悬浮 UI 压得住地图
        Color fadeCol = Themes[Mathf.Clamp(currentTheme, 0, Themes.Length - 1)].fade;
        var top = Rect("TopFade", root, 0, H-320, W, 320);
        var tri = top.AddComponent<RawImage>(); tri.texture = fade; tri.color = new Color(fadeCol.r, fadeCol.g, fadeCol.b, 0.92f); tri.raycastTarget = false;
        topFadeImg = tri;
        var bot = Rect("BotFade", root, 0, 0, W, 240);
        var bri = bot.AddComponent<RawImage>(); bri.texture = fade; bri.color = new Color(fadeCol.r, fadeCol.g, fadeCol.b, 0.85f); bri.raycastTarget = false;
        bri.uvRect = new Rect(0, 1, 1, -1);
        botFadeImg = bri;
    }

    void SwitchRegion(string destination)
    {
        if (dragging || activePanel || sceneryViewer || !RegionUnlocked(destination)) return;
        if (destination == currentRegion) { ClosePage(); return; }
        FreezeRegionTraffic();
        ClearStationOdArcs();
        ClosePage(); CloseLineBubble(); CancelDraft(); ClearFloaters(); currentRegion = destination;
        hkMapArt.SetActive(currentRegion == "月之亮面"); macauMapArt.SetActive(currentRegion == "月背");
        guangdongMapArt.SetActive(currentRegion == "火星");
        SyncRegionMapTextureMemory();
        RequestResourceTrim(false);
        trainsDirty = true;
        mapRect.localScale = Vector3.one;
        mapRect.anchoredPosition = mapWheelZoom ? mapWheelZoom.CenterPosition(1f) : Vector2.zero;
        foreach (var kv in stationImgs)
        {
            int i = kv.Key; var marker = kv.Value.transform.parent;
            marker.gameObject.SetActive(StationInRegion(i, currentRegion));
            marker.localScale = Vector3.one;
            marker.GetComponent<RectTransform>().anchoredPosition = Pos(i);
            if (stationLabels.TryGetValue(i, out var nameLabel) && nameLabel) nameLabel.text = StationDisplayName(i, currentRegion);
            if (stations[i].portal)
            {
                var texts = marker.GetComponentsInChildren<Text>(true);
                if (texts.Length > 1) texts[1].text = DisplayPlaceAliases(PortalSubtitle(stations[i], currentRegion));
            }
        }
        RefreshSceneryViews();
        LayoutStationLabels();
        UpdateRegionalMarkers(true);
        regionTitle.text = DisplayPlaceAliases(currentRegion == "月之亮面" ? "月之亮面" : currentRegion == "月背" ? "月 背" : "火 星");
        CreateLineVisuals(); RefreshStationDots();
        RecalcIncome(); RefreshHUD();
        PlayRegionCurtain(currentRegion);
        SyncRegionalMusic(false);
        PlayPageOpenSfx();
        PlayWhistleSfx();
        MarkRingsDirty();
        Toast("已切换至" + currentRegion);
    }

    bool RegionUnlocked(string region)
    {
        return region == "月之亮面" || (region == "月背" && macauUnlocked) || (region == "火星" && guangdongUnlocked);
    }
    int ConnectedOrdinaryStations(string region)
    {
        var found = new HashSet<int>();
        foreach (var line in lines) foreach (int i in line.st)
            if (stations[i].region == region && !stations[i].portal && !stations[i].closed) found.Add(i);
        return found.Count;
    }
    void UpdateRegionUnlocks()
    {
        if (!macauUnlocked && income >= MacauIncomeRequired && ConnectedOrdinaryStations("月之亮面") >= 4)
        { macauUnlocked = true; Toast("月背区域已解锁"); QueueStory("macau_open", "海的另一边", "月背发来一封简短的邀请：客流已经准备好了，缺的只是一条可靠的线路。\n下一站，去海的另一边。"); }
        if (macauUnlocked && !guangdongUnlocked && income >= GuangdongIncomeRequired && ConnectedOrdinaryStations("月背") >= 2)
        { guangdongUnlocked = true; Toast("火星区域已解锁"); QueueStory("guangdong_open", "向北的邀请", "月背的站台刚刚热闹起来，火星的合作邀请便送到了桌上。\n这一次，你要把一座城市的故事，接到更远的地方。"); }
    }

    void BuildStationViews()
    {
        for (int i = 0; i < stations.Count; i++)
        {
            var s = stations[i];
            Vector2 stationPosition = Pos(i);
            var go = Rect("S_" + s.name, stationLayer, stationPosition.x, stationPosition.y, 56, 56);
            go.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0.5f);
            var hit = Img(go, new Color(1,1,1,0)); hit.raycastTarget = true;
            var ringGo = Rect("Ring", go.transform, -14, -14, 84, 84);
            var ring = Img(ringGo, new Color(1,1,1,0), spriteRing); ring.raycastTarget = false;
            stationRings.Add(ring);
            // 长按 0.42s 的进度环：只显示正在长按的站点
            var pressGo = Rect("PressRing", go.transform, -14, -14, 84, 84);
            var press = Img(pressGo, new Color(0.95f, 0.42f, 0.26f, 0.95f), spriteRing);
            press.type = Image.Type.Filled;
            press.fillMethod = Image.FillMethod.Radial360;
            press.fillOrigin = (int)Image.Origin360.Top;
            press.fillClockwise = true;
            press.fillAmount = 0f;
            press.raycastTarget = false;
            pressGo.SetActive(false);
            pressRings[i] = press;
            // 圆点按级别分级：门户最大、县区最小，缩小时画面才有层次
            float dotSize = s.portal ? 24f : s.county ? 13f : 20f;
            float ringSize = dotSize + 8f;
            var outline = Rect("DotRing", go.transform, (56f - ringSize) * 0.5f, (56f - ringSize) * 0.5f, ringSize, ringSize);
            Color ringColor = s.portal ? new Color(0.82f,0.28f,0.22f,0.92f) : new Color(0.24f,0.21f,0.18f,0.85f);
            Img(outline, ringColor, spriteStationRing).raycastTarget = false;
            var dotGo = Rect("DotFill", go.transform, (56f - dotSize) * 0.5f, (56f - dotSize) * 0.5f, dotSize, dotSize);
            var img = Img(dotGo, s.portal ? Vermilion : new Color(0.985f,0.97f,0.94f,0.98f), spriteCircle);
            img.raycastTarget = false;
            if (s.portal)
            {
                var innerDot = Rect("PortalCore", dotGo.transform, dotSize * 0.28f, dotSize * 0.28f, dotSize * 0.44f, dotSize * 0.44f);
                Img(innerDot, Color.white, spriteCircle).raycastTarget = false;
            }
            var sp = go.AddComponent<StationPress>(); sp.index = i; sp.boot = this;
            var label = Txt(go.transform, StationDisplayName(i, currentRegion), s.portal ? 24 : 26, new Color(0.16f,0.14f,0.11f,0.96f), -104, -48, 240, 38, TextAnchor.MiddleCenter);
            TextShadow(label, new Color(0.99f,0.98f,0.94f,0.95f));
            if (s.county) { label.resizeTextForBestFit = true; label.resizeTextMinSize = 17; label.resizeTextMaxSize = 26; label.verticalOverflow = VerticalWrapMode.Truncate; }
            stationLabels[i] = label;
            var leader = Img(Rect("NameLeader", go.transform, 0, 0, 1, 2), new Color(0.15f, 0.14f, 0.13f, 0.32f), spriteWhite);
            leader.raycastTarget = false; leader.gameObject.SetActive(false); stationLeaders[i] = leader;
            if (s.portal)
            {
                var pl = Txt(go.transform, PortalSubtitle(s, currentRegion), 21, new Color(0.38f,0.35f,0.31f,0.9f), -100, -80, 220, 30, TextAnchor.MiddleCenter);
                TextShadow(pl, new Color(1,0.99f,0.96f,0.95f));
                stationDestinations[i] = pl;
            }
            stationImgs[i] = img;
            go.SetActive(StationInRegion(i, currentRegion));
        }
        UpdateStationLabelSizes(mapRect ? mapRect.localScale.x : 1f);
        LayoutStationLabels();
    }

    static float StationZoomFontBoost(float scale)
    {
        if (scale <= 1.01f) return 1f;
        return 1f + Mathf.Clamp(Mathf.Log(scale, 2f) * 0.24f, 0f, 0.65f);
    }

    void UpdateStationLabelSizes(float scale)
    {
        float labelScale = Mathf.Min(2.05f, UiFontScale * StationZoomFontBoost(scale));
        for (int i = 0; i < stations.Count; i++)
        {
            var s = stations[i];
            if (stationLabels.TryGetValue(i, out var label) && label)
            {
                int baseSize = s.portal ? 22 : 24;
                int targetSize = Mathf.Clamp(Mathf.RoundToInt(baseSize * labelScale), 16, 52);
                label.fontSize = targetSize;
                if (s.county)
                {
                    label.resizeTextMinSize = Mathf.Clamp(Mathf.RoundToInt(targetSize * 0.65f), 14, targetSize);
                    label.resizeTextMaxSize = targetSize;
                }
            }
            if (stationDestinations.TryGetValue(i, out var dest) && dest)
                dest.fontSize = Mathf.Clamp(Mathf.RoundToInt(18 * labelScale), 13, 38);
        }
    }

    // 站名摆放：优先贴近圆点，挤不下就往外挪并用引导线连回去。
    // 只要圆点露出来就保留名字；拥挤时选择冲突最少的位置。
    readonly List<Rect> labelRects = new List<Rect>();

    bool PlaceStationLabel(int i, List<Rect> occupied)
    {
        if (!stationLabels.TryGetValue(i, out var label) || !label) return false;
        float scale = mapRect ? mapRect.localScale.x : 1f;
        float labelScale = Mathf.Min(2.05f, UiFontScale * StationZoomFontBoost(scale));
        float sizeMult = Mathf.Max(1f, labelScale);
        float viewW = RootW, viewH = RootH;
        Vector2 pan = mapRect ? mapRect.anchoredPosition : Vector2.zero;
        Vector2 center = pan + Pos(i) * scale;
        float subtitleWidth = stationDestinations.TryGetValue(i, out var subtitle) ? subtitle.preferredWidth + 14f : 0f;
        float width = Mathf.Clamp(Mathf.Max(label.preferredWidth + 14f, subtitleWidth), 56f, 220f * sizeMult);
        float height = Mathf.Ceil(32f * sizeMult);
        float destHeight = stationDestinations.ContainsKey(i) ? Mathf.Ceil(22f * sizeMult) : 0f;
        // 16 个候选位：前 8 个贴着圆点，后 8 个更远，用引导线连回
        var offsets = new List<Vector2> {
            new Vector2(-width/2, -20f-height-destHeight), new Vector2(-width/2, 22f),
            new Vector2(27f, -height/2), new Vector2(-width-27f, -height/2),
            new Vector2(42f, 24f), new Vector2(-width-42f, 24f),
            new Vector2(42f, -24f-height-destHeight), new Vector2(-width-42f, -24f-height-destHeight),
            new Vector2(-width/2, -56f-height-destHeight), new Vector2(-width/2, 58f),
            new Vector2(74f, -height/2), new Vector2(-width-74f, -height/2),
            new Vector2(96f, 58f), new Vector2(-width-96f, 58f),
            new Vector2(96f, -64f-height-destHeight), new Vector2(-width-96f, -64f-height-destHeight)
        };
        // 密集区域扩大搜索范围，优先找不重叠的位置。
        float radiusMult = Mathf.Max(1f, labelScale * 0.9f);
        for (int radius = 130; radius <= 310; radius += 90)
            for (int direction = 0; direction < 8; direction++)
            {
                float angle = direction * Mathf.PI / 4f;
                float r = radius * radiusMult;
                offsets.Add(new Vector2(Mathf.Cos(angle) * r - width / 2f,
                    Mathf.Sin(angle) * r - (height + destHeight) / 2f));
            }
        // 屏幕边缘的圆点也尽量把名称放进画面。
        offsets.Add(new Vector2(Mathf.Clamp(center.x - width / 2f, 4f, Mathf.Max(4f, viewW - width - 4f)) - center.x,
            Mathf.Clamp(center.y + 22f, 4f, Mathf.Max(4f, viewH - height - destHeight - 4f)) - center.y));
        float best = float.PositiveInfinity; int chosen = 0;
        for (int k = 0; k < offsets.Count; k++)
        {
            var rect = new Rect(center.x + offsets[k].x, center.y + offsets[k].y, width, height + destHeight);
            float penalty = k * 2f;
            if (rect.xMin < 4 || rect.xMax > viewW-4 || rect.yMin < 4 || rect.yMax > viewH-4) penalty += 10000;
            for (int q = 0; q < occupied.Count; q++) if (rect.Overlaps(occupied[q])) penalty += 1000;
            for (int j = 0; j < stations.Count; j++)
            {
                if (i == j || !StationInRegion(j, currentRegion) || !IsStationShown(j)) continue;
                Vector2 point = pan + Pos(j) * scale;
                if (rect.Overlaps(new Rect(point.x-20, point.y-20, 40, 40))) penalty += 1000;
            }
            if (penalty < best) { best = penalty; chosen = k; }
            if (penalty == k * 2f) break; // 最靠近的无冲突位置已找到。
        }
        label.gameObject.SetActive(true);
        stationLeaders.TryGetValue(i, out var lead);
        if (lead) lead.gameObject.SetActive(false);

        Vector2 offset = offsets[chosen];
        var rt = label.rectTransform; rt.anchoredPosition = new Vector2(28 + offset.x, 28 + offset.y + destHeight);
        rt.sizeDelta = new Vector2(width, height);
        if (stationDestinations.TryGetValue(i, out var destination))
        {
            destination.rectTransform.anchoredPosition = new Vector2(28 + offset.x, 28 + offset.y);
            destination.rectTransform.sizeDelta = new Vector2(width, destHeight);
        }
        occupied.Add(new Rect(center.x + offset.x, center.y + offset.y, width, height + destHeight));

        bool displaced = chosen >= 2;
        if (lead)
        {
            lead.gameObject.SetActive(displaced);
            if (displaced)
            {
                Vector2 edge = new Vector2(Mathf.Clamp(0, offset.x, offset.x+width), Mathf.Clamp(0, offset.y, offset.y+height+destHeight));
                Vector2 start = edge.normalized * 18f, delta = edge - start;
                var lr = lead.rectTransform; lr.pivot = new Vector2(0, 0.5f); lr.anchoredPosition = new Vector2(28,28) + start;
                lr.sizeDelta = new Vector2(delta.magnitude, 2f); lr.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg);
            }
        }
        return true;
    }

    void LayoutStationLabels()
    {
        bool haveVisibility = stationVisible.Length == stations.Count;
        labelRects.Clear();
        for (int pass = 0; pass < 5; pass++)
            for (int i = 0; i < stations.Count; i++)
            {
                if (!StationInRegion(i, currentRegion)) continue;
                if (haveVisibility && !stationVisible[i]) continue;   // 圆点没露出来就不用排站名
                if (MarkerRank(i) != pass - 2) continue;              // 重要的先占近位
                PlaceStationLabel(i, labelRects);
            }
    }

    float regionalMarkerScale = -1f;
    // 地图放大时，线路 / 草稿线按 1/zoom 反向补偿，和站点一样保持屏幕尺寸恒定。
    // 只改厚度与图标缩放，不改长度与位置——长度必须留在地图坐标系里。
    void ApplyZoomToOverlays(float scale)
    {
        float k = 1f / Mathf.Max(0.01f, scale);
        overlayZoom = k;
        int live = 0;
        for (int i = 0; i < zoomRects.Count; i++)
        {
            var zr = zoomRects[i];
            if (!zr.rt) continue;
            zr.rt.sizeDelta = new Vector2(zr.rt.sizeDelta.x, zr.baseWidth * k);
            zoomRects[live++] = zr;
        }
        if (live < zoomRects.Count) zoomRects.RemoveRange(live, zoomRects.Count - live);
        foreach (var lv in lineVisuals.Values)
        {
            foreach (var tr in lv.trains) if (tr) tr.transform.localScale = Vector3.one * k;
            foreach (var seg in lv.segments)
            {
                if (seg.baseOffset.sqrMagnitude < 0.0001f) continue;
                Vector2 p = seg.basePos + seg.baseOffset * k;
                if (seg.casing) seg.casing.GetComponent<RectTransform>().anchoredPosition = p;
                if (seg.body) seg.body.GetComponent<RectTransform>().anchoredPosition = p;
                if (seg.core) seg.core.GetComponent<RectTransform>().anchoredPosition = p;
                if (seg.hit) seg.hit.GetComponent<RectTransform>().anchoredPosition = p;
            }
        }
        if (previewImg) previewImg.rectTransform.sizeDelta = new Vector2(previewImg.rectTransform.sizeDelta.x, 14.5f * k);
        if (dragTip) dragTip.rectTransform.localScale = Vector3.one * k;
        foreach (var d in odArcDashes) if (d) d.sizeDelta = new Vector2(d.sizeDelta.x, 7f * k);
        foreach (var m in odArcMarkers) if (m) m.localScale = Vector3.one * k;
    }

    bool[] stationVisible = new bool[0];
    Vector2 regionalMarkerPos = new Vector2(float.NaN, float.NaN);
    readonly List<int> markerOrder = new List<int>();
    readonly List<Vector2> takenDots = new List<Vector2>();

    // 拖线站点优先，其次是青锦交界站，再是其他交界站和已开站点。
    int MarkerRank(int i)
    {
        if (dragging && (i == dragFrom || i == hoverStation)) return -2;
        if (currentRegion == "火星" && stations[i].name == "奥林匹斯（月面门）") return -1;
        if (IsOpen(i) || stations[i].portal) return 0;
        return stations[i].county ? 2 : 1;
    }

    bool MatchesStationFilter(int i)
    {
        var station = stations[i];
        // 月之亮面/月背使用站区而非县级行政区；保留所有交界站与本地站点。
        if (currentRegion != "火星" || station.portal || stationFilter == 0) return true;
        int code = station.administrativeCode;
        bool prefecture = code > 0 && code % 100 == 0;
        bool countyCity = code > 0 && code % 100 >= 81;
        return prefecture || (stationFilter == 2 && countyCity);
    }

    void CycleStationFilter()
    {
        if (dragging) { Toast("请先完成当前线路"); return; }
        stationFilter = (stationFilter + 1) % 3;
        string[] names = { "全部城市", "地级市以上", "除市辖区" };
        if (stationFilterText) stationFilterText.text = names[stationFilter];
        UpdateRegionalMarkers(true);
        Toast("站点筛选：" + names[stationFilter]);
    }

    void UpdateRegionalMarkers(bool force)
    {
        if (!mapRect) return;
        float scale = mapRect.localScale.x;
        Vector2 pan = mapRect.anchoredPosition;
        bool zoomChanged = Mathf.Abs(scale - regionalMarkerScale) >= 0.005f;
        // 缩放和解算平移都要触发重算：拖动地图后站点/线路的疏密会变
        if (!force && !zoomChanged && (pan - regionalMarkerPos).sqrMagnitude < 1f) return;
        regionalMarkerScale = scale;
        regionalMarkerPos = pan;
        ApplyZoomToOverlays(scale);
        if (stationImgs.Count == 0) { if (zoomNotice) zoomNotice.SetActive(false); return; }
        UpdateStationLabelSizes(scale);

        var regionIndices = GetRegionStationIndices(currentRegion);
        Vector3 invScale = Vector3.one / scale;
        if (regionIndices != null)
        {
            for (int r = 0; r < regionIndices.Count; r++)
            {
                int idx = regionIndices[r];
                if (stationImgs.TryGetValue(idx, out var stImg) && stImg) stImg.transform.parent.localScale = invScale;
            }
        }
        else
        {
            foreach (var kv in stationImgs) kv.Value.transform.parent.localScale = invScale;
        }

        // 屏幕空间碰撞过滤（1 代做法）：靠得太近的站点自动让位。
        // 拖线站点 > 青锦交界站 > 其他交界站及已开站点 > 普通 > 县区
        if (stationVisible.Length != stations.Count) stationVisible = new bool[stations.Count];
        for (int i = 0; i < stationVisible.Length; i++) stationVisible[i] = false;

        markerOrder.Clear();
        int scanCount = regionIndices != null ? regionIndices.Count : stations.Count;
        for (int pass = 0; pass < 5; pass++)
        {
            for (int r = 0; r < scanCount; r++)
            {
                int i = regionIndices != null ? regionIndices[r] : r;
                if (StationInRegion(i, currentRegion) && MatchesStationFilter(i) && MarkerRank(i) == pass - 2) markerOrder.Add(i);
            }
        }

        // 已被筛选排除的站点不能留下旧图标或接受吸附。
        for (int r = 0; r < scanCount; r++)
        {
            int i = regionIndices != null ? regionIndices[r] : r;
            if (StationInRegion(i, currentRegion) && !MatchesStationFilter(i) && stationImgs.TryGetValue(i, out var stImg) && stImg)
                stImg.transform.parent.gameObject.SetActive(false);
        }
        int hiddenCount = 0;
        takenDots.Clear();
        for (int k = 0; k < markerOrder.Count; k++)
        {
            int i = markerOrder[k];
            int rank = MarkerRank(i);
            Vector2 sp = pan + Pos(i) * scale;                 // 相对 Stage 左下角的屏幕坐标
            float viewW = RootW, viewH = RootH;
            bool inView = sp.x >= -48f && sp.x <= viewW + 48f && sp.y >= -48f && sp.y <= viewH + 48f;
            // 间距与圆点尺寸匹配：主干 44、普通 34、县区 26（屏幕像素），避免两点相切成一片
            float gap = rank <= 0 ? 44f : rank == 2 ? 26f : 34f;
            bool show = inView;
            if (show)
                for (int q = 0; q < takenDots.Count; q++)
                    if (Mathf.Abs(sp.x - takenDots[q].x) < gap && Mathf.Abs(sp.y - takenDots[q].y) < gap) { show = false; break; }
            if (show) takenDots.Add(sp);
            if (!show && inView) hiddenCount++;

            var marker = stationImgs[i].transform.parent;
            if (marker.gameObject.activeSelf != show) marker.gameObject.SetActive(show);
            stationVisible[i] = show;
            if (!show) continue;

        }

        // 站名统一在这一步排：圆点露出来就给名字，挤不下就外移并用引导线连回
        LayoutStationLabels();

        // 状态驱动：只要有站点被疏密挤掉就提示，全部显示出来就自动收起
        if (zoomNotice)
        {
            if (hiddenCount > 0)
            {
                if (zoomNoticeTxt) zoomNoticeTxt.text = "已隐藏 " + hiddenCount + " 个站点 · 双指放大";
                if (!zoomNotice.activeSelf) zoomNotice.SetActive(true);
            }
            else if (zoomNotice.activeSelf) zoomNotice.SetActive(false);
        }

        // 线路段跟随端点：至少一端可见才画，避免线条连到看不见的点
        foreach (var lv in lineVisuals.Values)
        {
            foreach (var segment in lv.segments)
            {
                bool show = (segment.from >= 0 && segment.from < stationVisible.Length && stationVisible[segment.from])
                         || (segment.to >= 0 && segment.to < stationVisible.Length && stationVisible[segment.to]);
                segment.SetVisible(show);
            }
        }
    }

    void BuildSceneryViews()
    {
        foreach (var scenery in sceneries) BuildSceneryMarker(scenery);
    }

    void BuildLeftRail()
    {
        leftRailGos.Clear();
        string[] names = { "任务", "图鉴", "排行" };
        string[] icons = { "手帐", "博物", "番位" };
        Color[] accents = { new Color(0.85f,0.48f,0.28f,0.95f), Matcha, Indigo };
        for (int i = 0; i < 3; i++)
        {
            int k = i;
            var b = Rect("L" + i, root, 0, 1080 - i * 118, 118, 104);
            leftRailGos.Add(b);
            Rounded(b, CardBg); SoftShadow(b);
            var ribbon = Rect("Ribbon", b.transform, 0, 0, 6, 104);
            Img(ribbon, accents[i], spriteWhite).raycastTarget = false;
            Txt(b.transform, names[i], 27, Ink, 8, 50, 104, 42, TextAnchor.MiddleCenter);
            var tag = Rect("Tag", b.transform, 14, 12, 90, 30);
            Rounded(tag, new Color(Paper2.r, Paper2.g, Paper2.b, 0.78f));
            tag.GetComponent<Image>().raycastTarget = false;
            Txt(tag.transform, icons[i], 20, InkSoft, 0, 0, 90, 30, TextAnchor.MiddleCenter);
            if (i == 1)
            {
                Img(Rect("BadgeRing", b.transform, 88, 76, 26, 26), CardBg, spriteCircle).raycastTarget = false;
                var dotGo = Rect("Badge", b.transform, 90, 78, 22, 22); Img(dotGo, Vermilion, spriteCircle).raycastTarget = false;
                sceneryBadge = dotGo;
                dotGo.SetActive(pendingCheckin.Count > 0 || CountClaimablePlaques() > 0);
            }
            var btn = b.AddComponent<Button>(); btn.targetGraphic = b.GetComponent<Image>();
            btn.onClick.AddListener(() => {
                PlayClickSfx();
                if (k == 1)
                {
                    if (!IsFeatureUnlocked(GameFeature.SceneryAndCommercial)) { PlayErrorSfx(); Toast(FeatureLockHint(GameFeature.SceneryAndCommercial)); return; }
                    OpenSceneryScreen();
                }
                else if (k == 0) OpenDailyTasks();
                else
                {
                    if (!IsFeatureUnlocked(GameFeature.Leaderboard)) { PlayErrorSfx(); Toast(FeatureLockHint(GameFeature.Leaderboard)); return; }
                    OpenLeaderboard();
                }
            });
        }
    }

    void BuildStationFilterButton()
    {
        var button = Rect("StationFilter", root, 0, 720, 118, 104);
        stationFilterGo = button;
        Rounded(button, CardBg); SoftShadow(button);
        var ribbon = Rect("Ribbon", button.transform, 0, 0, 6, 104);
        Img(ribbon, new Color(InkSoft.r, InkSoft.g, InkSoft.b, 0.90f), spriteWhite).raycastTarget = false;
        Txt(button.transform, "筛选", 27, Ink, 8, 50, 104, 42, TextAnchor.MiddleCenter);
        var tag = Rect("FilterTag", button.transform, 10, 12, 98, 30);
        Rounded(tag, new Color(Paper2.r, Paper2.g, Paper2.b, 0.78f));
        tag.GetComponent<Image>().raycastTarget = false;
        string[] filterShort = { "全部", "地级市以上", "除市辖区" };
        stationFilterText = Txt(tag.transform, filterShort[Mathf.Clamp(stationFilter, 0, 2)], 20, InkSoft, 4, 0, 90, 30, TextAnchor.MiddleCenter);
        stationFilterText.resizeTextForBestFit = true; stationFilterText.resizeTextMinSize = 15; stationFilterText.resizeTextMaxSize = ScaledFontSize(20);
        var control = button.AddComponent<Button>(); control.targetGraphic = button.GetComponent<Image>();
        control.onClick.AddListener(CycleStationFilter);
    }

    void BuildLeftReminderEntries()
    {
        // 节气入口
        var solarGo = Rect("SolarTermEntry", root, 0, 602, 118, 104);
        solarTermRailGo = solarGo;
        Rounded(solarGo, CardBg); SoftShadow(solarGo);
        Img(Rect("Ribbon", solarGo.transform, 0, 0, 6, 104), new Color(0.24f, 0.50f, 0.42f, 0.95f), spriteWhite).raycastTarget = false;
        solarTermRailTitle = Txt(solarGo.transform, CurrentSolarTerm().name, 26, Ink, 8, 50, 104, 42, TextAnchor.MiddleCenter);
        solarTermRailTitle.resizeTextForBestFit = true; solarTermRailTitle.resizeTextMinSize = 18; solarTermRailTitle.resizeTextMaxSize = 26;
        var solarTag = Rect("SolarTag", solarGo.transform, 10, 12, 98, 30);
        Rounded(solarTag, new Color(Paper2.r, Paper2.g, Paper2.b, 0.78f));
        solarTag.GetComponent<Image>().raycastTarget = false;
        solarTermRailSub = Txt(solarTag.transform, "节气 · " + SolarTermDaysRemaining() + "日", 19, InkSoft, 4, 0, 90, 30, TextAnchor.MiddleCenter);
        solarTermRailSub.resizeTextForBestFit = true; solarTermRailSub.resizeTextMinSize = 14; solarTermRailSub.resizeTextMaxSize = 19;
        var solarBtn = solarGo.AddComponent<Button>(); solarBtn.targetGraphic = solarGo.GetComponent<Image>();
        solarBtn.onClick.AddListener(() => { PlayClickSfx(); OpenSolarTermQuickPopup(); });

        // 急报入口
        var alertGo = Rect("CongestionAlertEntry", root, 0, 484, 118, 104);
        alertRailGo = alertGo;
        Rounded(alertGo, CardBg); SoftShadow(alertGo);
        Img(Rect("Ribbon", alertGo.transform, 0, 0, 6, 104), Vermilion, spriteWhite).raycastTarget = false;
        Txt(alertGo.transform, "急报", 27, Ink, 8, 50, 104, 42, TextAnchor.MiddleCenter);
        var alertTag = Rect("AlertTag", alertGo.transform, 10, 12, 98, 30);
        Rounded(alertTag, new Color(Paper2.r, Paper2.g, Paper2.b, 0.78f));
        alertTag.GetComponent<Image>().raycastTarget = false;
        alertRailSub = Txt(alertTag.transform, "畅通", 19, InkSoft, 4, 0, 90, 30, TextAnchor.MiddleCenter);
        alertRailSub.resizeTextForBestFit = true; alertRailSub.resizeTextMinSize = 14; alertRailSub.resizeTextMaxSize = 19;
        Img(Rect("BadgeRing", alertGo.transform, 88, 76, 26, 26), CardBg, spriteCircle).raycastTarget = false;
        alertRailBadge = Rect("Badge", alertGo.transform, 90, 78, 22, 22);
        Img(alertRailBadge, Vermilion, spriteCircle).raycastTarget = false;
        var alertBtn = alertGo.AddComponent<Button>(); alertBtn.targetGraphic = alertGo.GetComponent<Image>();
        alertBtn.onClick.AddListener(() => { PlayClickSfx(); OpenCongestionAlertQuickPopup(); });

        RefreshLeftRailReminders();
    }

    void RefreshLeftRailReminders()
    {
        if (solarTermRailTitle) solarTermRailTitle.text = CurrentSolarTerm().name;
        if (solarTermRailSub) solarTermRailSub.text = "节气 · " + SolarTermDaysRemaining() + "日";
        if (alertRailSub || alertRailBadge)
        {
            var hotSegs = CollectTopCongestedSegments(3);
            bool hasAlert = hotSegs.Count > 0;
            if (alertRailSub)
            {
                alertRailSub.text = hasAlert ? hotSegs.Count + "处高负荷" : "全网畅通";
                alertRailSub.color = hasAlert ? Vermilion : InkSoft;
            }
            if (alertRailBadge) alertRailBadge.SetActive(hasAlert);
        }
    }

    void BuildRightRail()
    {
        var go = Rect("SpeedBoard", root, 960, 1170, 112, 112);
        speedBoardGo = go;
        var shell = Img(go, new Color(Ink.r, Ink.g, Ink.b, 0.88f), spriteRound); shell.type = Image.Type.Sliced;
        SoftShadow(go);
        var inner = Rect("Inner", go.transform, 3, 3, 106, 106);
        var innerIm = Img(inner, CardBg, spriteRound); innerIm.type = Image.Type.Sliced; innerIm.raycastTarget = false;
        var topBar = Rect("TopBar", inner.transform, 10, 71, 86, 28);
        Rounded(topBar, new Color(Paper2.r, Paper2.g, Paper2.b, 0.88f));
        topBar.GetComponent<Image>().raycastTarget = false;
        Txt(topBar.transform, "倍速", 20, InkSoft, 0, 0, 86, 28, TextAnchor.MiddleCenter);
        speedTxt = Txt(inner.transform, timeSpeed == 0 ? "暂停" : timeSpeed + "×", 38, Ink, 0, 8, 106, 62, TextAnchor.MiddleCenter);
        lastDisplayedSpeed = -1;
        var btn = go.AddComponent<Button>(); btn.targetGraphic = shell; btn.onClick.AddListener(CycleSpeed);

        // 右侧公告入口：手帐索引书签风格，大触控面方便拇指点按
        var noticeGo = Rect("NoticeBoard", root, 960, 1048, 112, 104);
        noticeBoardGo = noticeGo;
        Rounded(noticeGo, CardBg); SoftShadow(noticeGo);
        var noticeRibbon = Rect("Ribbon", noticeGo.transform, 106, 0, 6, 104);
        Img(noticeRibbon, Vermilion, spriteWhite).raycastTarget = false;
        Txt(noticeGo.transform, "公告", 27, Ink, 4, 50, 98, 42, TextAnchor.MiddleCenter);
        var noticeTag = Rect("Tag", noticeGo.transform, 12, 12, 86, 30);
        Rounded(noticeTag, new Color(Paper2.r, Paper2.g, Paper2.b, 0.78f));
        noticeTag.GetComponent<Image>().raycastTarget = false;
        Txt(noticeTag.transform, "驿报", 20, InkSoft, 0, 0, 86, 30, TextAnchor.MiddleCenter);
        Img(Rect("BadgeRing", noticeGo.transform, 4, 74, 24, 24), CardBg, spriteCircle).raycastTarget = false;
        noticeUnreadDot = Rect("Badge", noticeGo.transform, 6, 76, 20, 20);
        Img(noticeUnreadDot, Vermilion, spriteCircle).raycastTarget = false;
        noticeUnreadDot.SetActive(HasUnreadBulletins());
        var noticeBtn = noticeGo.AddComponent<Button>(); noticeBtn.targetGraphic = noticeGo.GetComponent<Image>();
        noticeBtn.onClick.AddListener(() => { if (!dragging) OpenNoticeBoard(); });

        // 右侧公告栏下方：一键升级入口
        var upgGo = Rect("QuickUpgrade", root, 960, 926, 112, 104);
        quickUpgradeGo = upgGo;
        Rounded(upgGo, CardBg); SoftShadow(upgGo);
        var upgRibbon = Rect("Ribbon", upgGo.transform, 106, 0, 6, 104);
        Img(upgRibbon, Gold, spriteWhite).raycastTarget = false;
        Txt(upgGo.transform, "升级", 27, Ink, 4, 50, 98, 42, TextAnchor.MiddleCenter);
        var upgTag = Rect("Tag", upgGo.transform, 12, 12, 86, 30);
        Rounded(upgTag, new Color(Paper2.r, Paper2.g, Paper2.b, 0.78f));
        upgTag.GetComponent<Image>().raycastTarget = false;
        Txt(upgTag.transform, "一键", 20, InkSoft, 0, 0, 86, 30, TextAnchor.MiddleCenter);
        var upgBtn = upgGo.AddComponent<Button>(); upgBtn.targetGraphic = upgGo.GetComponent<Image>();
        upgBtn.onClick.AddListener(() => { if (!dragging) OpenQuickUpgradeBubble(); });
    }

    void BuildBottomNav()
    {
        dock = Rect("Dock", root, 22, 16, 1036, 156);
        Rounded(dock, CardBg); SoftShadow(dock);
        string[] names = { "线路", "列车", "公司", "科技" };
        string[] ens = { "LINE", "TRAIN", "COMPANY", "TECH" };
        Color[] cols = { Vermilion, Indigo, Matcha, Gold };
        for (int i = 0; i < 4; i++)
        {
            int k = i;
            var item = Rect("Tab" + i, dock.transform, 14 + i*254, 12, 240, 132);
            Img(item, new Color(1,1,1,0));
            var dot = Rect("Dot", item.transform, 110, 104, 20, 20); Img(dot, cols[i], spriteCircle).raycastTarget = false;
            Txt(item.transform, names[i], 38, Ink, 0, 44, 240, 52, TextAnchor.MiddleCenter);
            Txt(item.transform, ens[i], 20, InkSoft, 0, 10, 240, 30, TextAnchor.MiddleCenter);
            var btn = item.AddComponent<Button>(); btn.targetGraphic = item.GetComponent<Image>();
            btn.onClick.AddListener(() => OnTab(k));
        }
        Img(Rect("Sep1", dock.transform, 268, 30, 1, 96), new Color(InkSoft.r, InkSoft.g, InkSoft.b, 0.16f)).raycastTarget = false;
        Img(Rect("Sep2", dock.transform, 522, 30, 1, 96), new Color(InkSoft.r, InkSoft.g, InkSoft.b, 0.16f)).raycastTarget = false;
        Img(Rect("Sep3", dock.transform, 776, 30, 1, 96), new Color(InkSoft.r, InkSoft.g, InkSoft.b, 0.16f)).raycastTarget = false;
        RefreshFeatureGateVisuals();
    }

    void BuildLineLoadLegend()
    {
        GetSafeMarginsInRoot(out float safeL, out float safeR, out _, out float safeB);
        float safeW = Mathf.Max(320f, RootW - safeL - safeR);
        float w = Mathf.Clamp(safeW - 230f, 580f, 816f);
        const float h = 64f;
        var legend = Rect("LineLoadLegend", root, safeL + (safeW - w) * 0.5f, safeB + 190f, w, h);
        lineLoadLegendGo = legend;
        Rounded(legend, new Color(CardBg.r, CardBg.g, CardBg.b, 0.94f));
        SoftShadow(legend);
        legend.GetComponent<Image>().raycastTarget = false;

        float textW = Mathf.Round(w * 0.54f);
        var titleTxt = Txt(legend.transform, "越堵的线路色相越偏红", 24, Ink, 20f, 0f, textW, h, TextAnchor.MiddleLeft);
        FitSingleLine(titleTxt, 24);
        titleTxt.raycastTarget = false;

        string[] itemTexts = { "畅通", "偏忙", "拥堵" };
        Color[] itemColors =
        {
            new Color(0.28f, 0.56f, 0.72f, 0.92f),
            new Color(0.94f, 0.56f, 0.22f, 0.96f),
            new Color(0.95f, 0.24f, 0.20f, 0.98f)
        };
        float itemsLeft = textW + 18f;
        float itemW = (w - itemsLeft - 14f) / itemTexts.Length;
        for (int i = 0; i < itemTexts.Length; i++)
        {
            float ix = itemsLeft + i * itemW;
            var trackSlot = Rect("LegendTrack" + i, legend.transform, ix + 2f, (h - 12f) * 0.5f, 26f, 12f);
            var outer = Img(trackSlot, new Color(Ink.r, Ink.g, Ink.b, 0.26f), spriteRound);
            outer.type = Image.Type.Sliced; outer.raycastTarget = false;
            var core = Rect("Core", trackSlot.transform, 2f, 2f, 22f, 8f);
            var coreIm = Img(core, itemColors[i], spriteRound);
            coreIm.type = Image.Type.Sliced; coreIm.raycastTarget = false;

            var txt = Txt(legend.transform, itemTexts[i], 22, InkSoft, ix + 32f, 0f, itemW - 34f, h, TextAnchor.MiddleLeft);
            FitSingleLine(txt, 22);
            txt.raycastTarget = false;
        }
    }

    float RootW => root ? Mathf.Max(W, root.rect.width) : W;
    float RootH => root ? Mathf.Max(H, root.rect.height) : H;
    float PageSafeHeight
    {
        get
        {
            GetSafeMarginsInRoot(out _, out _, out float safeT, out float safeB);
            return Mathf.Max(1400f, RootH - safeT - safeB);
        }
    }

    void GetSafeMarginsInRoot(out float left, out float right, out float top, out float bottom)
    {
        left = right = top = bottom = 0f;
        if (!root) return;
        Rect r = root.rect;
        if (r.width < 1f || r.height < 1f) return;
        Rect safe = Screen.safeArea;
        if (safe.width <= 0f || safe.height <= 0f) return;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(root, new Vector2(safe.xMin, safe.yMin), null, out var pMin) ||
            !RectTransformUtility.ScreenPointToLocalPointInRectangle(root, new Vector2(safe.xMax, safe.yMax), null, out var pMax))
            return;
        left = Mathf.Clamp(pMin.x - r.xMin, 0f, r.width * 0.25f);
        right = Mathf.Clamp(r.xMax - pMax.x, 0f, r.width * 0.25f);
        bottom = Mathf.Clamp(pMin.y - r.yMin, 0f, r.height * 0.25f);
        top = Mathf.Clamp(r.yMax - pMax.y, 0f, r.height * 0.25f);
    }

    void EnsureResponsiveLayout()
    {
        if (!root) return;
        float rw = root.rect.width, rh = root.rect.height;
        Rect safe = Screen.safeArea;
        if (Mathf.Approximately(rw, lastRespW) && Mathf.Approximately(rh, lastRespH) && safe == lastRespSafe) return;
        ApplyResponsiveLayout(true);
    }

    void ApplyResponsiveLayout(bool recenterAtMinZoom = false)
    {
        if (!root) return;
        float rw = RootW, rh = RootH;
        Rect safe = Screen.safeArea;
        lastRespW = root.rect.width; lastRespH = root.rect.height; lastRespSafe = safe;
        GetSafeMarginsInRoot(out float safeL, out float safeR, out float safeT, out float safeB);
        float safeW = Mathf.Max(320f, rw - safeL - safeR);

        if (topFadeImg)
        {
            var rt = topFadeImg.rectTransform;
            rt.anchoredPosition = new Vector2(0f, rh - safeT - 320f);
            rt.sizeDelta = new Vector2(rw, safeT + 320f);
        }
        if (botFadeImg)
        {
            var rt = botFadeImg.rectTransform;
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(rw, safeB + 240f);
        }

        float barY = rh - Mathf.Max(310f, safeT + 226f);
        if (topBarGo)
        {
            var barRt = topBarGo.GetComponent<RectTransform>();
            barRt.anchoredPosition = new Vector2(0f, barY);
            barRt.sizeDelta = new Vector2(rw, 280f);
            float coinX, capX, diaX;
            if (safeW <= 1140f)
            {
                coinX = safeL + 24f;
                capX = safeL + 354f + (safeW - W) * 0.5f;
                diaX = safeL + safeW - 36f - 244f;
            }
            else
            {
                coinX = safeL + 40f;
                capX = safeL + (safeW - 400f) * 0.5f;
                diaX = safeL + safeW - 40f - 244f;
            }
            if (topBarCoinRt) topBarCoinRt.anchoredPosition = new Vector2(coinX, 46f);
            if (topBarCapRt) topBarCapRt.anchoredPosition = new Vector2(capX, 26f);
            if (topBarDiaRt) topBarDiaRt.anchoredPosition = new Vector2(diaX, 46f);
            if (topBarGearRt) topBarGearRt.anchoredPosition = new Vector2(coinX, -32f);
            if (topBarHelpRt) topBarHelpRt.anchoredPosition = new Vector2(coinX + 98f, -32f);
        }

        if (storyObjectiveBannerGo)
            storyObjectiveBannerGo.GetComponent<RectTransform>().anchoredPosition = new Vector2(safeL + (safeW - 680f) * 0.5f, barY - 76f);

        if (zoomNotice)
            zoomNotice.GetComponent<RectTransform>().anchoredPosition = new Vector2(safeL + (safeW - 540f) * 0.5f, barY - 158f);

        float dockY = safeB + 16f;
        if (dock)
            dock.GetComponent<RectTransform>().anchoredPosition = new Vector2(safeL + (safeW - 1036f) * 0.5f, dockY);

        if (undoButton)
            undoButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(rw - safeR - 118f, dockY + 174f);

        if (themePreviewBannerGo)
            themePreviewBannerGo.GetComponent<RectTransform>().anchoredPosition = new Vector2(safeL + (safeW - 860f) * 0.5f, dockY + 170f);

        if (lineLoadLegendGo)
        {
            var lrt = lineLoadLegendGo.GetComponent<RectTransform>();
            float lw = lrt.sizeDelta.x;
            float legendY = themePreviewBannerGo && themePreviewBannerGo.activeSelf ? dockY + 268f : dockY + 174f;
            lrt.anchoredPosition = new Vector2(safeL + (safeW - lw) * 0.5f, legendY);
        }

        float railMin = dockY + 190f;
        float railMax = Mathf.Max(railMin + 260f, barY - 46f);
        float railMidY = Mathf.Lerp(railMin, railMax, 0.48f);
        for (int i = 0; i < leftRailGos.Count; i++)
            if (leftRailGos[i])
                leftRailGos[i].GetComponent<RectTransform>().anchoredPosition = new Vector2(safeL, Mathf.Min(railMax - 112f, railMidY + 224f - i * 118f));
        float filterY = Mathf.Max(dockY + 184f, railMidY - 130f);
        if (stationFilterGo)
            stationFilterGo.GetComponent<RectTransform>().anchoredPosition = new Vector2(safeL, filterY);
        if (solarTermRailGo)
            solarTermRailGo.GetComponent<RectTransform>().anchoredPosition = new Vector2(safeL, filterY - 118f);
        if (alertRailGo)
            alertRailGo.GetComponent<RectTransform>().anchoredPosition = new Vector2(safeL, filterY - 236f);

        float rightRailX = rw - safeR - 120f;
        if (speedBoardGo)
            speedBoardGo.GetComponent<RectTransform>().anchoredPosition = new Vector2(rightRailX, Mathf.Min(railMax - 118f, railMidY + 324f));
        if (noticeBoardGo)
            noticeBoardGo.GetComponent<RectTransform>().anchoredPosition = new Vector2(rightRailX, Mathf.Min(railMax - 236f, railMidY + 202f));
        if (quickUpgradeGo)
            quickUpgradeGo.GetComponent<RectTransform>().anchoredPosition = new Vector2(rightRailX, Mathf.Min(railMax - 354f, railMidY + 84f));

        if (mapRect && mapWheelZoom)
        {
            float scale = mapRect.localScale.x;
            if (recenterAtMinZoom && scale <= 1.01f)
                mapRect.anchoredPosition = mapWheelZoom.CenterPosition(scale);
            else
                mapRect.anchoredPosition = mapWheelZoom.ClampPosition(mapRect.anchoredPosition, scale);
            UpdateRegionalMarkers(true);
        }

        if (mapEdgeFeathers != null && mapEdgeFeathers.Length == 4)
        {
            bool showVert = rh > H + 2f;
            bool showHoriz = rw > W + 2f;
            if (mapEdgeFeathers[0]) mapEdgeFeathers[0].gameObject.SetActive(showVert);
            if (mapEdgeFeathers[1]) mapEdgeFeathers[1].gameObject.SetActive(showVert);
            if (mapEdgeFeathers[2]) mapEdgeFeathers[2].gameObject.SetActive(showHoriz);
            if (mapEdgeFeathers[3]) mapEdgeFeathers[3].gameObject.SetActive(showHoriz);
        }
    }

    // ---------- tabs ----------
    void OnTab(int k)
    {
        if (k == 0) OpenLinesScreen();
        else if (k == 1)
        {
            if (!IsFeatureUnlocked(GameFeature.FleetPage)) { PlayErrorSfx(); Toast(FeatureLockHint(GameFeature.FleetPage)); return; }
            OpenGarageScreen();
        }
        else if (k == 2)
        {
            if (!IsFeatureUnlocked(GameFeature.CompanyCore)) { PlayErrorSfx(); Toast(FeatureLockHint(GameFeature.CompanyCore)); return; }
            OpenCompanyScreen();
        }
        else
        {
            if (!IsFeatureUnlocked(GameFeature.TechPage)) { PlayErrorSfx(); Toast(FeatureLockHint(GameFeature.TechPage)); return; }
            if (techTreeTab == 1 && IsFeatureUnlocked(GameFeature.CompanyCore)) OpenCompanyTechScreen();
            else OpenTechScreen(0);
        }
    }

    // ---------- long press draw ----------
    public void OnStationDown(int idx) { BeginPress(idx); }
    public void OnStationUp(int idx)
    {
        if (pressing && pressIdx == idx && pressTimer < 0.42f && !dragging)
        {
            pressing = false; pressIdx = -1;
            HidePressRing();
            ShowStationActions(idx);
        }
        else if (pressing)
        {
            pressing = false; pressIdx = -1;
            HidePressRing();
        }
    }
    float nextPortalEndHint;
    void ShowPortalEndHint()
    {
        if (Time.unscaledTime < nextPortalEndHint) return;
        nextPortalEndHint = Time.unscaledTime + 1.5f;
        Toast("交界站只能作为本区域线路的起点或终点");
    }
    bool TryConnect(int a, int b)
    {
        if (a == b || !StationInRegion(a, currentRegion) || !StationInRegion(b, currentRegion)) return false;
        // 交界站可像普通站一样过站与闭环，仅禁止两座交界站直接相连，避免跨图归属二义性。
        if (stations[a].portal && stations[b].portal) { Toast("请通过区域内普通站连接交界站"); return false; }
        bool closeLoop = draft.Count >= 3 && b == draft[0] && !IsLoop(draft);
        if (draft.Contains(b) && !closeLoop) { Toast("同一条线不能重复经过同一站"); return false; }
        draft.Add(b); draftCost = DraftCostOf(draft);
        TriggerHaptic(12); PlaySnapSfx();
        return true;
    }
    void FinishDraft()
    {
        if (draft.Count < 2) { CancelDraft(); return; }
        if (draftCost > coins) { Toast("金币不足：还差 ¥" + FormatShortfall(draftCost - coins)); CancelDraft(); return; }
        int cost = draftCost;
        var snapshot = new List<int>(draft);
        int from = snapshot[0];
        ShowConnectPopup(from, snapshot, cost, -1);
    }

    void ClearDraftState()
    {
        unchecked { connectPopupVersion++; }
        draft.Clear(); draftSegNeeds.Clear(); draftCost = 0;
        ClearDraftSegs(); ResetBuildVisuals();
    }

    int RegionStationCount(string region)
    {
        int count = 0;
        for (int i = 0; i < stations.Count; i++) if (StationInRegion(i, region)) count++;
        return count;
    }
    int RegionLineLimit(string region) { return RegionStationCount(region) / 2; }
    int LineLimit() { return RegionLineLimit(currentRegion); }
    bool PathTouchesRegion(List<int> path, string region)
    {
        for (int i = 0; i + 1 < path.Count; i++) if (SegmentRegion(path[i], path[i + 1]) == region) return true;
        return false;
    }
    int RegionLineCount(string region)
    {
        int count = 0; foreach (var line in lines) if (PathTouchesRegion(line.st, region)) count++;
        return count;
    }
    void CommitNewLine(List<int> snapshot, int cost)
    {
        if (RegionLineCount(currentRegion) >= LineLimit()) { Toast(currentRegion + "线路已达上限（" + LineLimit() + " 条）"); return; }
        BeginUndo("开通线路");
        ClosePanel(true);
        var line = new Line { id = nextLineId, name = nextLineId + " 号线", color = LineColors[(nextLineId-1) % LineColors.Length] };
        line.st = new List<int>(snapshot);
        lines.Add(line); coins -= cost;
        BuildStationsOnPath(snapshot);
        ClearDraftState();
        CreateLineVisuals(); RefreshAllLines(); RefreshStationDots(); RecalcIncome(); RefreshHUD();
        nextLineId++;
        CheckSceneryOnLine(snapshot);
        QueueStory("new_line", "不被看好的新线", "有人说这条线不会有乘客。首班车靠站时，等车的人却排到了街角。\n你把时刻表贴上墙：从今天起，这座城市多了一条路。");
        CheckLineStory(line);
        PlayBuildSfx(); TriggerHaptic(22);
        PlayStationPathSparkles(snapshot, line.color);
        SyncStoryProgression(true);
        EndUndo();
    }

    void CommitExtend(Line line, List<int> newPath, List<int> draftStations, int cost)
    {
        if (!PathTouchesRegion(line.st, currentRegion) && PathTouchesRegion(newPath, currentRegion) && RegionLineCount(currentRegion) >= LineLimit()) {
            Toast(currentRegion + "线路已达上限，暂不能跨区延长"); return;
        }
        BeginUndo("延长线路");
        ClosePanel(true);
        line.st = new List<int>(newPath);
        coins -= cost; BuildStationsOnPath(draftStations);
        ClearDraftState();
        CreateLineVisuals(); RefreshAllLines(); RefreshStationDots(); RecalcIncome(); RefreshHUD();
        CheckSceneryOnLine(draftStations);
        CheckLineStory(line);
        PlayBuildSfx(); TriggerHaptic(18);
        PlayStationPathSparkles(draftStations, line.color);
        SyncStoryProgression(true);
        EndUndo();
    }

    void CheckLineStory(Line line)
    {
        if (UniqueCount(line.st) >= 5) QueueStory("five_stations", "第五站的灯", "第五座站台亮灯时，最早质疑你的人也挤进了人群。\n“原来那条线，真的能通到这里。”\n你笑了笑，把下一段线路图展开。");
        if (IsLoop(line.st)) QueueStory("first_loop", "首尾相连", "最后一段轨道合拢，列车没有掉头，沿着城市的脉络继续前行。\n曾经分散的街区，终于连成了一个完整的圆。");
    }

    List<int> BuildExtendPath(Line line, int from, List<int> draftStations)
    {
        var tail = draftStations.GetRange(1, draftStations.Count - 1);
        if (tail.Count == 0) return null;
        int i = line.st.IndexOf(from);
        if (i < 0) return null;
        var res = new List<int>();
        if (i == 0)
        {
            for (int k = tail.Count - 1; k >= 0; k--) res.Add(tail[k]);
            res.AddRange(line.st);
        }
        else if (i == line.st.Count - 1)
        {
            res.AddRange(line.st);
            res.AddRange(tail);
        }
        else
        {
            for (int k = 0; k <= i; k++) res.Add(line.st[k]);
            res.AddRange(tail);
            for (int k = tail.Count - 1; k >= 0; k--) res.Add(tail[k]);
            res.Add(from);
            for (int k = i + 1; k < line.st.Count; k++) res.Add(line.st[k]);
        }
        return res;
    }

    int UniqueCount(List<int> st)
    {
        var set = new HashSet<int>();
        foreach (var i in st) set.Add(i);
        return set.Count;
    }

    float DailyOf(Line line, List<int> st)
    {
        if (ReferenceEquals(st, line.st) && traffic != null) return TrafficRevenue(line, traffic);
        return TrafficRevenue(line, CalculateTraffic(line, st));
    }
    float EstimateNetworkDelta(Line line, List<int> path, bool isNew)
    {
        // Bounded route-only estimate; settlement retains the complete network solver.
        var candidate = new Line { id = isNew ? nextLineId : line.id, st = path,
            trains = isNew ? 1 : line.trains, fare = isNew ? 1.5f : line.fare,
            skin = isNew ? -1 : line.skin };
        if (!isNew) candidate.dispatch = (int[])line.dispatch.Clone();
        float before = isNew ? 0f : EstimateLocalRouteProfit(line);
        return EstimateLocalRouteProfit(candidate) - before;
    }
    float EstimateLocalRouteProfit(Line line)
    {
        float profit = 0f, savedClock = dayClock;
        var seen = new HashSet<int>();
        float distance = EconomicPathLength(line.st) / Mathf.Max(1, line.st.Count - 1);
        float fare = line.fare + 0.0036f * distance;
        try
        {
            for (int slot = 0; slot < 4; slot++)
            {
                dayClock = ForecastSlotHours[slot] * GameDaySeconds / 24f;
                seen.Clear(); float demand = 0f;
                foreach (int station in line.st)
                    if (seen.Add(station) && !stations[station].closed) demand += StationBaseDemand(station);
                float carried = Mathf.Min(demand * 0.325f, FleetCapacity(line));
                float net = carried * fare / 10f - VehicleMaintenance(line) - TrackMaintenance(line);
                profit += (net - Mathf.Max(0f, net) * 0.1f) * ForecastSlotFractions[slot];
            }
        }
        finally { dayClock = savedClock; }
        return profit;
    }

    string UniquePathText(List<int> st)
    {
        var seen = new HashSet<int>(); string s = "";
        foreach (var i in st)
        {
            if (!seen.Add(i)) continue;
            s += (s.Length == 0 ? "" : " · ") + StationDisplayName(i, currentRegion);
        }
        return s;
    }

    List<Line> ValidExtendCandidates(int from, List<int> snapshot)
    {
        var res = new List<Line>();
        if (IsLoop(snapshot)) return res;
        foreach (var l in lines)
        {
            if (!l.st.Contains(from) || IsLoop(l.st)) continue;
            bool overlap = false;
            int opposite = from == l.st[0] ? l.st[l.st.Count - 1] : from == l.st[l.st.Count - 1] ? l.st[0] : -1;
            for (int k = 1; k < snapshot.Count; k++)
                if (l.st.Contains(snapshot[k]) && !(k == snapshot.Count - 1 && snapshot[k] == opposite)) { overlap = true; break; }
            if (!overlap)
            {
                var proposed = BuildExtendPath(l, from, snapshot);
                if (proposed != null && PortalEndsAreValid(proposed)) res.Add(l);
            }
        }
        return res;
    }

    // 交界站可作为普通站过站与闭环，但任意一段轨道不可直接连接两个交界站。
    bool PortalEndsAreValid(List<int> path)
    {
        for (int i = 0; i < path.Count - 1; i++)
        {
            if (stations[path[i]].portal && stations[path[i + 1]].portal) return false;
        }
        return true;
    }

    float LineLengthKm(List<int> path)
    {
        if (path == null || path.Count < 2) return 0f;
        float totalKm = 0f;
        for (int i = 0; i < path.Count - 1; i++)
        {
            string region = SegmentRegion(path[i], path[i + 1]);
            if (region == null) continue;
            float px = Vector2.Distance(PosInRegion(path[i], region), PosInRegion(path[i + 1], region));
            float kmPerPx = region == "火星" ? 0.68f : region == "月之亮面" ? 0.050f : 0.00954f;
            totalKm += px * kmPerPx;
        }
        return totalKm;
    }
    string FormatLineKm(List<int> path) => LineLengthKm(path).ToString("0.0") + " km";
    string RouteEndpointsSummary(List<int> st)
    {
        if (st == null || st.Count == 0) return "";
        int s0 = st[0], s1 = st[st.Count - 1];
        if (s0 < 0 || s0 >= stations.Count || s1 < 0 || s1 >= stations.Count) return "";
        string n0 = StationDisplayName(s0, stations[s0].region);
        if (IsLoop(st)) return n0 + " ↺ 环线";
        string n1 = StationDisplayName(s1, stations[s1].region);
        return n0 + " ⇄ " + n1;
    }

    bool IsLoop(List<int> path) { return path.Count >= 4 && path[0] == path[path.Count - 1]; }

    class ConnectOpt { public bool isNew; public Line line; public List<int> path; }

    int DefaultConnectOption(int from, List<ConnectOpt> opts)
    {
        // 交汇站可能同时是多条线路的端点和中间站，优先匹配端点。
        for (int i = 0; i < opts.Count; i++)
        {
            var line = opts[i].line;
            if (!opts[i].isNew && line != null && line.st.Count > 0 &&
                (line.st[0] == from || line.st[line.st.Count - 1] == from)) return i;
        }
        for (int i = 0; i < opts.Count; i++) if (opts[i].isNew) return i;
        return 0; // 已达线路上限时，只提供仍可用的延长选项。
    }

    int connectPopupVersion;
    void ShowConnectPopup(int from, List<int> snapshot, int cost, int selected)
    {
        StartCoroutine(ShowConnectPopupDeferred(from, snapshot, cost, selected, ++connectPopupVersion));
    }
    System.Collections.IEnumerator ShowConnectPopupDeferred(int from, List<int> snapshot, int cost, int selected, int version)
    {
        yield return null;
        if (version != connectPopupVersion || draft.Count < 2 || draft.Count != snapshot.Count) yield break;
        for (int i = 0; i < snapshot.Count; i++) if (draft[i] != snapshot[i]) yield break;
        try { ShowConnectPopupInternal(from, snapshot, cost, selected); }
        catch (Exception e) { Debug.LogError("EXT FAIL " + e); Toast("延长弹窗出错"); CancelDraft(); }
    }

    void ShowConnectPopupInternal(int from, List<int> snapshot, int cost, int selected)
    {
        var opts = new List<ConnectOpt>();
        foreach (var l in ValidExtendCandidates(from, snapshot))
        {
            var path = BuildExtendPath(l, from, snapshot);
            if (path != null && (PathTouchesRegion(l.st, currentRegion) || RegionLineCount(currentRegion) < LineLimit()))
                opts.Add(new ConnectOpt { isNew = false, line = l, path = path });
        }
        if (RegionLineCount(currentRegion) < LineLimit()) opts.Add(new ConnectOpt { isNew = true });
        if (opts.Count == 0) { Toast(currentRegion + "线路已达上限（" + LineLimit() + " 条）"); CancelDraft(); return; }
        if (selected < 0) selected = DefaultConnectOption(from, opts);
        selected = Mathf.Clamp(selected, 0, opts.Count - 1);
        var sel = opts[selected];
        string title = sel.isNew ? ("开通 " + nextLineId + " 号线") : ("延长 " + sel.line.name);
        const float rowStep = 106f, rowH = 96f, pathViewW = 750f;
        float viewHeight = Mathf.Min(opts.Count, 5) * rowStep;
        float h = 264f + viewHeight;
        var p = OpenBubble(title, h, 920, Pos(snapshot[snapshot.Count - 1]), "connect_opts_" + from);
        Txt(p.transform, "从 " + StationDisplayName(from, currentRegion) + " 起 · 新增 " + (UniqueCount(snapshot) - 1) + " 站 · 造价 ¥" + cost, 22, InkSoft, 30, h - 114, 820, 30, TextAnchor.MiddleLeft);
        var view = Rect("OptionsViewport", p.transform, 24, 122, 872, viewHeight);
        Img(view, new Color(1,1,1,0.01f)); var mask = view.AddComponent<Mask>(); mask.showMaskGraphic = false;
        var scroll = view.AddComponent<ScrollRect>(); scroll.horizontal = false; scroll.vertical = true; scroll.scrollSensitivity = 100f;
        scroll.viewport = view.GetComponent<RectTransform>();
        var content = Rect("Options", view.transform, 0, 0, 872, opts.Count * rowStep);
        var contentRt = content.GetComponent<RectTransform>(); contentRt.anchorMin = contentRt.anchorMax = new Vector2(0,1);
        contentRt.pivot = new Vector2(0,1); contentRt.anchoredPosition = Vector2.zero; scroll.content = contentRt;
        BindScrollPosition(scroll, "connect:" + from);
        for (int i = 0; i < opts.Count; i++)
        {
            var opt = opts[i]; int k = i;
            var row = Rect("Opt"+i, content.transform, 0, opts.Count * rowStep - (rowH + 5f) - i * rowStep, 872, rowH);
            Rounded(row, i == selected ? new Color(0.985f,0.948f,0.862f,1f) : new Color(1,1,1,0.94f));
            SoftShadow(row);
            Color accent = opt.isNew ? Vermilion : opt.line.color;
            Img(Rect("Bar", row.transform, 0, 20, 7, 56), accent, spriteRound);
            string t1 = opt.isNew ? "开通新线路" : ("延长 " + opt.line.name);
            string route = opt.isNew
                ? (nextLineId + " 号线 · " + RouteEndpointsSummary(snapshot) + " · " + UniqueCount(snapshot) + " 站")
                : (RouteEndpointsSummary(opt.path ?? opt.line.st) + " · +" + (snapshot.Count - 1) + " 站");
            int gain = i == selected ? Mathf.RoundToInt(EstimateNetworkDelta(opt.line, opt.isNew ? snapshot : opt.path, opt.isNew)) : 0;
            string t2 = route + (i == selected ? " · 粗估 " + (gain >= 0 ? "+" : "") + gain + "/天" : " · 选择后查看预计收益");
            Txt(row.transform, t1, 30, Ink, 28, 50, 520, 38, TextAnchor.MiddleLeft);
            var pathView = Rect("PathViewport", row.transform, 28, 11, pathViewW, 34);
            var pathHit = Img(pathView, new Color(1, 1, 1, 0.004f));
            pathView.AddComponent<RectMask2D>();
            var pathContent = Rect("PathContent", pathView.transform, 0, 0, pathViewW, 34);
            var pathContentRt = pathContent.GetComponent<RectTransform>();
            pathContentRt.anchorMin = new Vector2(0, 0.5f); pathContentRt.anchorMax = new Vector2(0, 0.5f);
            pathContentRt.pivot = new Vector2(0, 0.5f); pathContentRt.anchoredPosition = Vector2.zero;
            var pathLabel = Txt(pathContent.transform, t2, 23, InkSoft, 0, 0, pathViewW, 34, TextAnchor.MiddleLeft);
            pathLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
            pathLabel.verticalOverflow = VerticalWrapMode.Overflow;
            pathLabel.raycastTarget = false;
            float pathW = Mathf.Max(pathViewW, Mathf.Ceil(pathLabel.preferredWidth) + 12f);
            pathLabel.rectTransform.sizeDelta = new Vector2(pathW, 34);
            pathContentRt.sizeDelta = new Vector2(pathW, 34);
            bool needHScroll = pathW > pathViewW + 2f;
            pathHit.raycastTarget = needHScroll;
            if (needHScroll)
            {
                var pathScroll = pathView.AddComponent<ScrollRect>();
                pathScroll.horizontal = true; pathScroll.vertical = false;
                pathScroll.inertia = true; pathScroll.decelerationRate = 0.12f;
                pathScroll.scrollSensitivity = 60f;
                pathScroll.movementType = ScrollRect.MovementType.Elastic;
                pathScroll.viewport = pathView.GetComponent<RectTransform>();
                pathScroll.content = pathContentRt;
                var passClick = pathView.AddComponent<ConnectRowPathClick>();
                passClick.onClick = () => ShowConnectPopup(from, snapshot, cost, k);
                passClick.parentScroll = scroll;
                passClick.selfScroll = pathScroll;
            }
            if (i == selected) Txt(row.transform, "✓", 34, Gold, 800, 26, 48, 44, TextAnchor.MiddleCenter);
            var btn = row.AddComponent<Button>(); btn.targetGraphic = row.GetComponent<Image>();
            btn.onClick.AddListener(() => ShowConnectPopup(from, snapshot, cost, k));
        }
        if (opts.Count > 5) scroll.verticalNormalizedPosition = 1f - Mathf.Clamp01((float)selected / (opts.Count - 5));
        var ok = Btn(p.transform, "Ok", "确 认", 28, 24, 22, 422, 84, Vermilion, () => {
            if (sel.isNew) CommitNewLine(snapshot, cost);
            else CommitExtend(sel.line, sel.path, snapshot, cost);
        });
        TInk(ok, Color.white);
        var no = Btn(p.transform, "No", "取 消", 28, 474, 22, 422, 84, new Color(0.92f,0.89f,0.83f), () => { ClosePanel(); CancelDraft(); });
        TInk(no, Ink);
    }
    sealed class ConnectRowPathClick : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public Action onClick;
        public ScrollRect parentScroll, selfScroll;
        bool routeToParent;
        public void OnPointerClick(PointerEventData e) { if (!e.dragging) onClick?.Invoke(); }
        public void OnBeginDrag(PointerEventData e)
        {
            routeToParent = parentScroll && Mathf.Abs(e.delta.y) > Mathf.Abs(e.delta.x);
            if (routeToParent) { if (selfScroll) selfScroll.enabled = false; parentScroll.OnBeginDrag(e); }
        }
        public void OnDrag(PointerEventData e) { if (routeToParent && parentScroll) parentScroll.OnDrag(e); }
        public void OnEndDrag(PointerEventData e)
        {
            if (routeToParent && parentScroll) parentScroll.OnEndDrag(e);
            if (selfScroll) selfScroll.enabled = true;
            routeToParent = false;
        }
    }
    // ---------- draft drag visuals ----------
    List<GameObject> draftSegs = new List<GameObject>();
    void ClearDraftSegs()
    {
        foreach (var g in draftSegs) if (g) Destroy(g);
        draftSegs.Clear();
    }
    void AddDraftSeg(int a, int b, int segIdx = -1, bool animate = false)
    {
        var p1 = Pos(a); var p2 = Pos(b); var d = p2 - p1;
        var go = Rect("DSeg", lineLayer, p1.x, p1.y, d.magnitude, 14.5f * overlayZoom);
        var drt = go.GetComponent<RectTransform>(); drt.pivot = new Vector2(0, 0.5f);
        float need = segIdx >= 0 && segIdx < draftSegNeeds.Count ? draftSegNeeds[segIdx] : DraftPairNeedScore(a, b);
        var img = Img(go, NeedColorGradient(need), spriteHandStroke);
        img.type = Image.Type.Sliced; img.raycastTarget = false;
        drt.localRotation = Quaternion.Euler(0,0, Mathf.Atan2(d.y,d.x)*Mathf.Rad2Deg);
        zoomRects.Add(new ZoomRect(drt, 14.5f));
        draftSegs.Add(go);
        if (animate) go.AddComponent<DraftSegGrow>();
    }
    void RedrawDraft()
    {
        ClearDraftSegs();
        for (int i = 0; i < draft.Count-1; i++) AddDraftSeg(draft[i], draft[i+1], i, i == draft.Count - 2);
    }

    // ---------- line visuals ----------
    void CreateLineVisuals()
    {
        var oldVisuals = new Dictionary<int, LineVisual>(lineVisuals);
        foreach (var v in lineVisuals.Values) foreach (var tr in v.trains) if (tr) Destroy(tr);
        if (lineLayer) { for (int i = lineLayer.childCount - 1; i >= 0; i--) Destroy(lineLayer.GetChild(i).gameObject); }
        if (trainLayer) { for (int i = trainLayer.childCount - 1; i >= 0; i--) Destroy(trainLayer.GetChild(i).gameObject); }
        lineVisuals.Clear();
        zoomRects.Clear();
        trainsDirty = true;

        // 预计算共线片段映射，避免每条每段重复扫描全网线路。
        var sharedSegMap = new Dictionary<long, List<int>>();
        foreach (var l in lines)
        {
            if (l.st == null) continue;
            for (int k = 0; k < l.st.Count - 1; k++)
            {
                int la = l.st[k], lb = l.st[k + 1];
                string sr = SegmentRegion(la, lb);
                if (string.IsNullOrEmpty(sr)) continue;
                int regCode = sr == "月之亮面" ? 1 : sr == "月背" ? 2 : 3;
                int lo = Mathf.Min(la, lb), hi = Mathf.Max(la, lb);
                long key = ((long)regCode << 40) | ((long)lo << 20) | (uint)hi;
                if (!sharedSegMap.TryGetValue(key, out var list)) sharedSegMap[key] = list = new List<int>();
                if (!list.Contains(l.id)) list.Add(l.id);
            }
        }
        foreach (var list in sharedSegMap.Values) if (list.Count > 1) list.Sort();

        Vector2 ComputeCachedOffset(int lineId, int a, int b, string sr)
        {
            if (string.IsNullOrEmpty(sr)) return Vector2.zero;
            int regCode = sr == "月之亮面" ? 1 : sr == "月背" ? 2 : 3;
            int lo = Mathf.Min(a, b), hi = Mathf.Max(a, b);
            long key = ((long)regCode << 40) | ((long)lo << 20) | (uint)hi;
            if (!sharedSegMap.TryGetValue(key, out var list) || list.Count <= 1) return Vector2.zero;
            int rank = list.IndexOf(lineId);
            if (rank < 0) return Vector2.zero;
            Vector2 d = PosInRegion(hi, sr) - PosInRegion(lo, sr);
            if (d.sqrMagnitude < 0.001f) return Vector2.zero;
            Vector2 normal = new Vector2(-d.y, d.x).normalized;
            return normal * ((rank - (list.Count - 1) * 0.5f) * CollinearStepPx);
        }

        foreach (var line in lines)
        {
            var lv = new LineVisual();
            if (oldVisuals.TryGetValue(line.id, out var old)) { lv.t = old.t; lv.credit = old.credit; }
            int skin = GetLineSkin(line);
            Color trim = skin >= 0 ? LineSkinTrimColors[skin] : Color.Lerp(line.color, Color.white, 0.40f);
            Color coreColor = skin >= 0 ? Color.Lerp(trim, Color.white, 0.35f) : new Color(1f, 0.98f, 0.92f, 0.56f);
            coreColor.a = skin >= 0 ? 0.78f : 0.56f;
            float totalLen = 0f;
            for (int i = 0; i < line.st.Count - 1; i++)
            {
                string sr = SegmentRegion(line.st[i], line.st[i + 1]);
                Vector2 baseOffset = ComputeCachedOffset(line.id, line.st[i], line.st[i + 1], sr);
                if (sr != null)
                {
                    Vector2 pa = PosInRegion(line.st[i], sr), pb = PosInRegion(line.st[i + 1], sr);
                    float segLen = Vector2.Distance(pa, pb);
                    totalLen += segLen;
                    lv.cachedSegs.Add(new CachedTrainSegment {
                        stationA = line.st[i], stationB = line.st[i + 1], region = sr,
                        posA = pa, posB = pb, baseOffset = baseOffset,
                        length = segLen, angleDeg = Mathf.Atan2(pb.y - pa.y, pb.x - pa.x) * Mathf.Rad2Deg
                    });
                }
                if (sr != currentRegion) continue;
                Vector2 offset = baseOffset * overlayZoom;
                Vector2 basePos = Pos(line.st[i]);
                Color bodyColor = line.color;
                bodyColor.a = 0.94f;
                var segBody = AddSeg(line.st[i], line.st[i+1], bodyColor, 9.2f, lineLayer, offset, spriteHandStroke);
                var trackCore = AddSeg(line.st[i], line.st[i+1], coreColor, 2.5f, lineLayer, offset, spriteHandStroke);
                var hit = AddSeg(line.st[i], line.st[i+1], new Color(1,1,1,0f), 34, lineLayer, offset);
                var lt = hit.AddComponent<LineTapPress>(); lt.boot = this; lt.lineId = line.id; lt.segment = i;
                lv.segments.Add(new LineSegmentVisual { from = line.st[i], to = line.st[i + 1], basePos = basePos, baseOffset = baseOffset, body = segBody, core = trackCore, hit = hit, coreImg = trackCore.GetComponent<Image>(), baseCoreColor = coreColor });
            }
            lv.cachedTotalLength = totalLen;
            lineVisuals[line.id] = lv;
        }
        // 无描边手绘风格：按「手绘笔触主体 -> 线芯/热力芯 -> 点击热区」分层，去除生硬黑边
        foreach (var lv in lineVisuals.Values) foreach (var s in lv.segments) if (s.body) s.body.transform.SetAsLastSibling();
        foreach (var lv in lineVisuals.Values) foreach (var s in lv.segments) if (s.core) s.core.transform.SetAsLastSibling();
        foreach (var lv in lineVisuals.Values) foreach (var s in lv.segments) if (s.hit) s.hit.transform.SetAsLastSibling();
        foreach (var line in lines)
        {
            if (!lineVisuals.TryGetValue(line.id, out var lv)) continue;
            SyncLineTrainVisuals(line, lv);
        }
        ApplyZoomToOverlays(mapRect ? mapRect.localScale.x : 1f);
        UpdateRegionalMarkers(true);
        UpdateLineTrafficColors();
    }

    GameObject CreateTrainVisual(Line line, int carIndex)
    {
        int skin = GetLineSkin(line);
        Color trim = skin >= 0 ? LineSkinTrimColors[skin] : Color.Lerp(line.color, Color.white, 0.40f);
        // 星际列车车厢（硬朗修长造型，规避肉感胶囊）
        var t = Rect($"Train{line.id}_{carIndex}", trainLayer, 0, 0, 34, 14);
        t.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0.5f);
        t.transform.localScale = Vector3.one * overlayZoom;
        var chassis = Img(t, new Color(0.18f, 0.16f, 0.13f, 0.95f), spriteWhite); chassis.raycastTarget = false;
        var body = Rect("Body", t.transform, 1, 1, 32, 12);
        var bimg = Img(body, line.color, spriteWhite); bimg.raycastTarget = false;
        if (skin >= 0)
        {
            Color stripeColor = SkinCategory(skin) == 3 ? new Color(1f, 0.88f, 0.45f, 0.95f) : trim;
            Img(Rect("StripeL", body.transform, 1, 0.8f, 25, 1.8f), stripeColor, spriteWhite).raycastTarget = false;
            Img(Rect("StripeR", body.transform, 1, 9.4f, 25, 1.8f), stripeColor, spriteWhite).raycastTarget = false;
        }
        var roof = Rect("Roof", body.transform, 3, 2, 22, 8);
        Img(roof, trim, spriteWhite).raycastTarget = false;
        var ac = Rect("AC", roof.transform, 8, 1, 8, 6);
        Img(ac, new Color(0.22f, 0.20f, 0.18f, 0.85f), spriteWhite).raycastTarget = false;
        var cab = Rect("Cab", body.transform, 27, 1, 3.5f, 10);
        Img(cab, new Color(0.20f, 0.40f, 0.55f, 0.95f), spriteWhite).raycastTarget = false;
        var lightL = Rect("LightL", body.transform, 30.5f, 1.5f, 1.5f, 2.5f);
        Img(lightL, new Color(1f, 0.95f, 0.45f, 1f), spriteWhite).raycastTarget = false;
        var lightR = Rect("LightR", body.transform, 30.5f, 8f, 1.5f, 2.5f);
        Img(lightR, new Color(1f, 0.95f, 0.45f, 1f), spriteWhite).raycastTarget = false;
        return t;
    }

    void SyncLineTrainVisuals(Line line, LineVisual lv)
    {
        if (!trainLayer) return;
        int desired = RunningTrains(line) > 0 ? 1 : 0;
        while (lv.trains.Count > desired)
        {
            int last = lv.trains.Count - 1;
            if (lv.trains[last]) Destroy(lv.trains[last]);
            lv.trains.RemoveAt(last);
            if (lv.trainRts.Count > last) lv.trainRts.RemoveAt(last);
            if (lv.trainActive.Count > last) lv.trainActive.RemoveAt(last);
            if (lv.trainLastSt.Count > last) lv.trainLastSt.RemoveAt(last);
        }
        while (lv.trains.Count < desired)
        {
            var tr = CreateTrainVisual(line, lv.trains.Count);
            lv.trains.Add(tr);
            lv.trainRts.Add(tr ? tr.GetComponent<RectTransform>() : null);
            lv.trainActive.Add(tr ? tr.activeSelf : true);
            lv.trainLastSt.Add(-1);
        }
    }
    const float CollinearStepPx = 7.6f;
    Vector2 SegmentBaseVisualOffset(Line line, int segIdx, string region)
    {
        if (line == null || segIdx < 0 || segIdx >= line.st.Count - 1 || string.IsNullOrEmpty(region)) return Vector2.zero;
        int a = line.st[segIdx], b = line.st[segIdx + 1];
        int lo = Mathf.Min(a, b), hi = Mathf.Max(a, b);
        var sharedIds = new List<int>();
        foreach (var l in lines)
        {
            for (int k = 0; k < l.st.Count - 1; k++)
            {
                int la = l.st[k], lb = l.st[k + 1];
                if (Mathf.Min(la, lb) == lo && Mathf.Max(la, lb) == hi && SegmentRegion(la, lb) == region)
                {
                    if (!sharedIds.Contains(l.id)) sharedIds.Add(l.id);
                    break;
                }
            }
        }
        if (sharedIds.Count <= 1) return Vector2.zero;
        sharedIds.Sort();
        int rank = sharedIds.IndexOf(line.id);
        if (rank < 0) return Vector2.zero;
        Vector2 d = PosInRegion(hi, region) - PosInRegion(lo, region);
        if (d.sqrMagnitude < 0.001f) return Vector2.zero;
        Vector2 normal = new Vector2(-d.y, d.x).normalized;
        return normal * ((rank - (sharedIds.Count - 1) * 0.5f) * CollinearStepPx);
    }
    Vector2 SegmentVisualOffset(Line line, int segIdx, string region)
    {
        return SegmentBaseVisualOffset(line, segIdx, region) * overlayZoom;
    }
    static Color CongestionShiftedColor(Color baseCol, float load)
    {
        if (load <= 0.30f) return baseCol;
        float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((load - 0.30f) / 0.70f));
        Color.RGBToHSV(baseCol, out float h, out float s, out float v);
        float targetHue = h > 0.5f ? 1f : 0.01f;
        float shiftedHue = Mathf.Repeat(Mathf.Lerp(h, targetHue, t), 1f);
        float shiftedSat = Mathf.Lerp(s, 0.84f, t);
        float shiftedVal = Mathf.Lerp(v, 0.92f, t);
        Color shifted = Color.HSVToRGB(shiftedHue, shiftedSat, shiftedVal);
        shifted.a = Mathf.Lerp(baseCol.a, 0.98f, t);
        return shifted;
    }

    void UpdateLineTrafficColors()
    {
        if (traffic == null || traffic.lineSegLoad == null) return;
        foreach (var line in lines)
        {
            if (!lineVisuals.TryGetValue(line.id, out var lv)) continue;
            foreach (var seg in lv.segments)
            {
                var img = seg.coreImg;
                if (!img) continue;
                string key = line.id + ":" + Mathf.Min(seg.from, seg.to) + ":" + Mathf.Max(seg.from, seg.to);
                float load = traffic.lineSegLoad.TryGetValue(key, out float val) ? val : 0f;
                img.color = CongestionShiftedColor(seg.baseCoreColor, load);
            }
        }
    }

    void ClearStationOdArcs()
    {
        odFocusStation = -1;
        odArcDashes.Clear();
        odArcDashImgs.Clear();
        odArcMarkers.Clear();
        if (!odArcLayer) return;
        for (int i = odArcLayer.childCount - 1; i >= 0; i--)
            Destroy(odArcLayer.GetChild(i).gameObject);
    }

    void ShowStationOdArcs(int origin)
    {
        ClearStationOdArcs();
        if (!odArcLayer || origin < 0 || origin >= stations.Count || !StationInRegion(origin, currentRegion)) return;
        var dests = GetStationOdTopDests(origin, 3);
        if (dests.Count == 0) return;

        odFocusStation = origin;
        Vector2 p0 = Pos(origin);
        float k = overlayZoom > 0.01f ? overlayZoom : 1f;
        var placedBadges = new List<Vector2>();
        float[] tCandidates = { 0.50f, 0.36f, 0.64f, 0.28f, 0.72f };
        for (int idx = 0; idx < dests.Count; idx++)
        {
            var df = dests[idx];
            if (!StationInRegion(df.dest, currentRegion)) continue;
            Vector2 p2 = Pos(df.dest);
            Vector2 diff = p2 - p0;
            float dist = diff.magnitude;
            if (dist < 12f) continue;

            Color arcCol = !df.reachable
                ? new Color(0.88f, 0.28f, 0.24f, 0.88f)
                : df.crowded
                    ? new Color(0.95f, 0.64f, 0.16f, 0.92f)
                    : new Color(0.16f, 0.65f, 0.54f, 0.88f);
            string stateTag = !df.reachable ? "未联网" : df.crowded ? "运力不足" : "畅通";

            Vector2 dir = diff / dist;
            Vector2 perp = new Vector2(-dir.y, dir.x);
            float bend = Mathf.Clamp(dist * 0.20f, 28f, 88f) * (idx % 2 == 0 ? 1f : -1f);
            Vector2 p1 = (p0 + p2) * 0.5f + perp * bend;

            const int segments = 11;
            for (int s = 0; s < segments; s++)
            {
                float t0 = (s + 0.18f) / segments;
                float t1 = (s + 0.72f) / segments;
                Vector2 a = (1f - t0) * (1f - t0) * p0 + 2f * (1f - t0) * t0 * p1 + t0 * t0 * p2;
                Vector2 b = (1f - t1) * (1f - t1) * p0 + 2f * (1f - t1) * t1 * p1 + t1 * t1 * p2;
                Vector2 segD = b - a;
                var dashGo = Rect("OdDash_" + idx + "_" + s, odArcLayer, a.x, a.y, Mathf.Max(4f, segD.magnitude), 7f * k);
                var drt = dashGo.GetComponent<RectTransform>();
                drt.pivot = new Vector2(0f, 0.5f);
                drt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(segD.y, segD.x) * Mathf.Rad2Deg);
                var dim = Img(dashGo, arcCol, spriteRound); dim.type = Image.Type.Sliced; dim.raycastTarget = false;
                odArcDashes.Add(drt);
                odArcDashImgs.Add(dim);
            }

            Vector2 mid = 0.25f * p0 + 0.5f * p1 + 0.25f * p2;
            float minGap = 52f * k;
            foreach (float tc in tCandidates)
            {
                Vector2 cand = (1f - tc) * (1f - tc) * p0 + 2f * (1f - tc) * tc * p1 + tc * tc * p2;
                bool ok = Vector2.Distance(cand, p0) >= 34f * k && Vector2.Distance(cand, p2) >= 34f * k;
                if (ok) foreach (var prev in placedBadges) if (Vector2.Distance(cand, prev) < minGap) { ok = false; break; }
                if (ok) { mid = cand; break; }
            }
            foreach (var prev in placedBadges)
                if (Mathf.Abs(mid.y - prev.y) < 38f * k && Mathf.Abs(mid.x - prev.x) < 120f * k)
                    mid += new Vector2(0f, (idx % 2 == 0 ? 40f : -40f) * k);
            placedBadges.Add(mid);
            string badgeTxt = StationDisplayName(df.dest, currentRegion) + " · " + stateTag;
            float bw = Mathf.Clamp(badgeTxt.Length * 22f + 34f, 140f, 252f);
            var markerGo = Rect("OdBadge_" + idx, odArcLayer, mid.x - bw * 0.5f, mid.y - 21f, bw, 42f);
            var mrt = markerGo.GetComponent<RectTransform>();
            mrt.pivot = new Vector2(0.5f, 0.5f);
            mrt.anchoredPosition = mid;
            mrt.localScale = Vector3.one * k;
            var mBg = Img(markerGo, new Color(CardBg.r, CardBg.g, CardBg.b, 0.95f), spriteRound);
            mBg.type = Image.Type.Sliced; mBg.raycastTarget = false;
            var dot = Img(Rect("Dot", markerGo.transform, 11f, 14f, 14f, 14f), arcCol, spriteCircle);
            dot.raycastTarget = false;
            var lbl = Txt(markerGo.transform, badgeTxt, 20, Ink, 30f, 0f, bw - 38f, 42f, TextAnchor.MiddleLeft);
            lbl.raycastTarget = false;
            FitSingleLine(lbl, 20);
            odArcMarkers.Add(mrt);
        }
    }

    void FocusMapOnStation(int idx)
    {
        if (idx < 0 || idx >= stations.Count || !mapRect) return;
        string targetReg = stations[idx].region;
        if (!string.IsNullOrEmpty(targetReg) && targetReg != currentRegion && RegionUnlocked(targetReg))
            SwitchRegion(targetReg);
        if (!mapWheelZoom) mapWheelZoom = mapRect.GetComponent<MapWheelZoom>();
        float minFocused = currentRegion == "火星" ? 2.1f : 1.45f;
        float maxZ = mapWheelZoom != null && mapWheelZoom.maxZoom != null ? mapWheelZoom.maxZoom() : 2.5f;
        float scale = Mathf.Clamp(Mathf.Max(mapRect.localScale.x, minFocused), 1f, maxZ);
        mapRect.localScale = Vector3.one * scale;
        Vector2 stPos = Pos(idx);
        Vector2 viewCenter = new Vector2(root.rect.width * 0.5f, root.rect.height * 0.5f);
        Vector2 anchored = viewCenter - stPos * scale;
        mapRect.anchoredPosition = mapWheelZoom ? mapWheelZoom.ClampPosition(anchored, scale) : anchored;
        ApplyZoomToOverlays(scale);
        LayoutStationLabels();
        UpdateRegionalMarkers(true);
        PulseStation(idx);
        ShowStationActions(idx);
    }
    GameObject AddSeg(int a, int b, Color c, float width, RectTransform layer, Vector2 offset = default, Sprite strokeSprite = null)
    {
        var p1 = Pos(a) + offset; var p2 = Pos(b) + offset;
        var d = p2 - p1;
        var go = Rect("Seg", layer, p1.x, p1.y, d.magnitude, width);
        var rt = go.GetComponent<RectTransform>(); rt.pivot = new Vector2(0, 0.5f);
        var im = Img(go, c, strokeSprite ? strokeSprite : spriteWhite);
        if (strokeSprite != null) { im.type = Image.Type.Sliced; im.raycastTarget = false; }
        rt.localRotation = Quaternion.Euler(0,0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
        zoomRects.Add(new ZoomRect(rt, width));
        return go;
    }
    bool StationInRegion(int i, string region) { return stations[i].region == region || (stations[i].portal && (stations[i].portalTo == region || stations[i].portalTo2 == region)); }
    string SegmentRegion(int a, int b)
    {
        if (StationInRegion(a, "月之亮面") && StationInRegion(b, "月之亮面")) return "月之亮面";
        if (StationInRegion(a, "月背") && StationInRegion(b, "月背")) return "月背";
        if (StationInRegion(a, "火星") && StationInRegion(b, "火星")) return "火星";
        return null;
    }
    Vector2 PosInRegion(int i, string region)
    {
        var s = stations[i]; bool other = s.portal && s.portalTo == region;
        bool third = s.portal && s.portalTo2 == region;
        return new Vector2(MAP_X + (third ? s.thirdX : other ? s.otherX : s.x)*MAP_W,
            MAP_Y + (third ? s.thirdY : other ? s.otherY : s.y)*MAP_H);
    }
    Vector2 Pos(int i) { return PosInRegion(i, currentRegion); }

    void RefreshAllLines()
    {
        CreateLineVisuals();
        RefreshStationDots();
    }

    void Update()
    {
        EnsureResponsiveLayout();
        float dt = Time.deltaTime;
        TickAudioCrossfade(Time.unscaledDeltaTime);
        TickTapTapSdkBridge();
        TickStoryProgressionTimer(dt);
        TickAutosave();
        UpdateRegionalMarkers(false);
        TickPress(dt);
        if (lineInsertActive) { TickDragEdgeAutoPan(dt); UpdateLineInsert(Input.mousePosition); }
        TickClock(dt);
        TickStationTraffic(dt);
        TickStory();
        TickResearch(dt);
        TickProgressiveForecasts();
        TickFloaters(dt);
        UpdateMetroMapOverlay();
        bool mapVisible = pageRoot == null && sceneryViewer == null && metroMapPageRoot == null;
        if (mapVisible && (timeSpeed > 0 || trainsDirty)) UpdateTrains(dt * timeSpeed / 6f);
        if (mapVisible && odArcDashImgs.Count > 0)
        {
            float nowT = Time.unscaledTime;
            bool waveFlow = qualityTier >= 2;
            for (int i = 0; i < odArcDashImgs.Count; i++)
            {
                var im = odArcDashImgs[i];
                if (!im) continue;
                float shimmer = waveFlow
                    ? (0.58f + 0.42f * Mathf.Sin(nowT * 5.2f - i * 0.65f))
                    : (0.76f + 0.24f * Mathf.Sin(nowT * 3.8f));
                Color c = im.color; c.a = shimmer; im.color = c;
            }
        }
        RefreshHUD();
        if (pulseTimer > 0)
        {
            pulseTimer -= dt;
            if (pulseIdx >= 0 && stationImgs.ContainsKey(pulseIdx))
            {
                float k = 1f - Mathf.Clamp01(pulseTimer / 0.24f);
                float sc = 1f + 0.35f * Mathf.Sin(k * Mathf.PI);
                stationImgs[pulseIdx].rectTransform.localScale = new Vector3(sc, sc, 1);
            }
            if (pulseTimer <= 0)
            {
                if (pulseIdx >= 0 && stationImgs.ContainsKey(pulseIdx)) stationImgs[pulseIdx].rectTransform.localScale = Vector3.one;
                pulseIdx = -1;
            }
        }
        if (dragging && draft.Count > 0 && previewImg)
        {
            if (dragFrom < 0) dragFrom = draft[draft.Count-1];

            // 拖拽开线到边缘自动推镜 / 平滑滚屏
            TickDragEdgeAutoPan(dt);

            Vector2 local; RectTransformUtility.ScreenPointToLocalPointInRectangle(mapRect, Input.mousePosition, null, out local);
            float snapR = (currentRegion == "火星" || currentRegion == "月背") ? 44f / Mathf.Max(1f, mapRect.localScale.x) : 44f;
            int near = NearestStationAny(local, snapR);
            bool backtracked = HandleDraftRevisit(near);
            int hov = near >= 0 && !draft.Contains(near) && !IsLoop(draft) ? near : -1;
            Vector2 end = backtracked ? Pos(dragFrom) : (hov >= 0 ? Pos(hov) : local);
            var p1 = Pos(dragFrom); var d = end - p1;
            previewImg.rectTransform.anchoredPosition = p1;
            previewImg.rectTransform.sizeDelta = new Vector2(Mathf.Max(1, d.magnitude), 14.5f * overlayZoom);
            previewImg.rectTransform.localRotation = Quaternion.Euler(0,0, Mathf.Atan2(d.y,d.x)*Mathf.Rad2Deg);
            float targetNeed = hov >= 0 ? DraftPairNeedScore(dragFrom, hov) : EvaluateDragDirectionNeed(dragFrom, end);
            float blend = 1f - Mathf.Exp(-14f * dt);
            draftPreviewNeed = Mathf.Lerp(draftPreviewNeed, targetNeed, blend);
            draftPreviewColor = Color.Lerp(draftPreviewColor, NeedColorGradient(draftPreviewNeed), blend);
            previewImg.color = draftPreviewColor;
            if (dragTip) { dragTip.rectTransform.anchoredPosition = end; dragTip.color = draftPreviewColor; }
            hoverStation = hov;
            if (!backtracked && hov >= 0 && hov != dragFrom)
            {
                float committedNeed = DraftPairNeedScore(dragFrom, hov);
                if (TryConnect(dragFrom, hov))
                {
                    draftSegNeeds.Add(committedNeed);
                    dragFrom = hov; hoverStation = -1;
                    PrepareDraftNeedField(hov, draft);
                    RedrawDraft();
                    PulseStation(hov);
                    previewImg.rectTransform.anchoredPosition = Pos(hov);
                }
            }
            UpdateDragCostBadge(end, local, dt);
            if (mapVisible) UpdateRings();
        }
        else if (!dragging && mapVisible) UpdateRings();
        if (Input.GetMouseButtonUp(0) && dragging) EndDraw();
        if (Input.GetMouseButtonUp(0) && lineInsertActive) EndLineInsert(Input.mousePosition);
        if (toastTrackingForecast && toastGo && toastGo.activeSelf && toastTxt)
        {
            if (pendingForecastMask != 0)
            {
                int remSlots = CountMaskBits(pendingForecastMask);
                int doneSlots = Mathf.Clamp(4 - remSlots, 0, 3);
                float elapsed = Mathf.Max(0f, Time.unscaledTime - forecastStartUnscaledTime);
                float estTotal = doneSlots > 0
                    ? Mathf.Max(lastForecastDuration, elapsed * (4f / (doneSlots + 0.35f)))
                    : Mathf.Max(lastForecastDuration, elapsed + 0.4f);
                float remain = Mathf.Max(0.1f, estTotal - elapsed);
                toastTxt.text = $"运力更新有延迟（约 {remain:0.0}s）";
            }
            else
            {
                toastTrackingForecast = false;
                toastTxt.text = "运力已更新";
                toastTimer = 1.0f;
            }
        }
        if (toastTimer > 0) { toastTimer -= dt; if (toastTimer <= 0 && toastGo) { toastTrackingForecast = false; toastGo.SetActive(false); } }
    }

    void TickDragEdgeAutoPan(float dt)
    {
        if (!mapWheelZoom && mapRect) mapWheelZoom = mapRect.GetComponent<MapWheelZoom>();
        if (!mapWheelZoom || !mapRect || pageRoot != null || activePanel != null) return;

        Vector2 mouse = Input.mousePosition;
        Rect safe = Screen.safeArea;
        if (safe.width <= 0 || safe.height <= 0) return;

        // 边缘智能感应区缓冲区厚度 (水平约 110px，垂直约 140px，预留顶部栏与底栏)
        float marginX = Mathf.Min(110f, safe.width * 0.16f);
        float marginY = Mathf.Min(140f, safe.height * 0.16f);

        float dirX = 0f;
        float dirY = 0f;

        // 左边缘：视口向左探寻 -> 地图向右平移
        if (mouse.x < safe.xMin + marginX && mouse.x >= safe.xMin - 40f)
        {
            float f = Mathf.Clamp01((safe.xMin + marginX - mouse.x) / marginX);
            dirX = f * f; // 二次平滑推进曲线
        }
        // 右边缘：视口向右探寻 -> 地图向左平移
        else if (mouse.x > safe.xMax - marginX && mouse.x <= safe.xMax + 40f)
        {
            float f = Mathf.Clamp01((mouse.x - (safe.xMax - marginX)) / marginX);
            dirX = -f * f;
        }

        // 下边缘：视口向下探寻 -> 地图向上平移
        if (mouse.y < safe.yMin + marginY && mouse.y >= safe.yMin - 40f)
        {
            float f = Mathf.Clamp01((safe.yMin + marginY - mouse.y) / marginY);
            dirY = f * f;
        }
        // 上边缘：视口向上探寻 -> 地图向下平移
        else if (mouse.y > safe.yMax - marginY && mouse.y <= safe.yMax + 40f)
        {
            float f = Mathf.Clamp01((mouse.y - (safe.yMax - marginY)) / marginY);
            dirY = -f * f;
        }

        if (dirX != 0f || dirY != 0f)
        {
            // 基础巡航速度：460px/s，丝滑稳健，受 MapWheelZoom.ClampPosition 防穿帮锁保护
            float speed = 460f;
            Vector2 delta = new Vector2(dirX, dirY) * (speed * dt);
            mapWheelZoom.PanBy(delta);
        }
    }

    // 只吸附当前真正显示出来的站点；吸附半径按级别给，小点不该有大门户的吸附范围
    bool IsStationShown(int i)
    {
        if (i >= 0 && i < stationVisible.Length) return stationVisible[i];
        return stationImgs.TryGetValue(i, out var img) && img.transform.parent.gameObject.activeInHierarchy;
    }
    float SnapRadius(int i, float radius)
    {
        if (stations[i].county) return radius * 0.82f;
        if (stations[i].portal || IsOpen(i)) return radius * 1.1f;
        return radius;
    }
    int NearestStation(Vector2 p, float radius)
    {
        int best = -1; float bd = float.MaxValue;
        for (int i = 0; i < stations.Count; i++)
        {
            if (!StationInRegion(i, currentRegion) || !IsStationShown(i) || draft.Contains(i)) continue;
            float d = Vector2.Distance(p, Pos(i));
            if (d < SnapRadius(i, radius) && d < bd) { bd = d; best = i; }
        }
        return best;
    }
    int NearestStationAny(Vector2 p, float radius)
    {
        int best = -1; float bd = float.MaxValue;
        for (int i = 0; i < stations.Count; i++)
        {
            if (!StationInRegion(i, currentRegion) || !IsStationShown(i)) continue;
            float d = Vector2.Distance(p, Pos(i));
            if (d < SnapRadius(i, radius) && d < bd) { bd = d; best = i; }
        }
        return best;
    }
    int StationOpeningCost(int index)
    {
        var station = stations[index];
        if (station.built) return 0;
        bool cityHub = station.administrativeCode > 0 && station.administrativeCode % 100 == 0;
        int hubCost = station.portal ? 70 : cityHub ? 50 : 0;
        float policyMult = (companyPolicy == 1 ? 0.85f : 1f) * PartnerOpenCostFactor() * (economyCycle == 4 ? 0.80f : 1f) * CoTechOpenCostMult();
        return Mathf.RoundToInt((40f + StationBaselineDemand(index) * 0.7f + hubCost) * policyMult);
    }
    int StationEstimatedDemand(int index)
    {
        return Mathf.RoundToInt(StationBaselineDemand(index) * PassengerScale);
    }
    void BuildStationsOnPath(List<int> path)
    {
        foreach (int index in path) stations[index].built = true;
    }
    int DraftCostOf(List<int> path)
    {
        int c = 0;
        var counted = new HashSet<int>();
        foreach (int index in path) if (counted.Add(index)) c += StationOpeningCost(index);
        float trackFactor = TrackBuildCostFactor();
        for (int i = 0; i < path.Count-1; i++)
            c += Mathf.RoundToInt((30f + Vector2.Distance(Pos(path[i]), Pos(path[i+1])) * 0.75f) * trackFactor);
        return c;
    }
    bool HandleDraftRevisit(int near)
    {
        if (near < 0 || near == dragFrom || !draft.Contains(near)) return false;
        if (near == draft[0] && draft.Count >= 3 && !IsLoop(draft))
        {
            float loopNeed = DraftPairNeedScore(dragFrom, near);
            if (TryConnect(dragFrom, near))
            {
                draftSegNeeds.Add(loopNeed);
                dragFrom = near;
                PrepareDraftNeedField(near, draft);
                RedrawDraft();
                PulseStation(near);
            }
            return false;
        }
        return BacktrackDraft(near);
    }
    bool BacktrackDraft(int station)
    {
        int idx = draft.IndexOf(station);
        if (idx < 0 || idx >= draft.Count-1) return false;
        draft.RemoveRange(idx+1, draft.Count-idx-1);
        int keepSegs = Mathf.Max(0, draft.Count - 1);
        if (draftSegNeeds.Count > keepSegs)
            draftSegNeeds.RemoveRange(keepSegs, draftSegNeeds.Count - keepSegs);
        dragFrom = station; hoverStation = -1;
        draftCost = DraftCostOf(draft);
        PrepareDraftNeedField(station, draft);
        RedrawDraft(); UpdateRings();
        PulseStation(station); PlayTone(false);
        return true;
    }
    void MarkRingsDirty() { ringsDirty = true; }

    void UpdateRings()
    {
        float nowT = Time.unscaledTime;
        float pulse = 0.62f + 0.24f * Mathf.Sin(nowT * 4.2f);
        float warmPulse = 0.40f + 0.16f * Mathf.Sin(nowT * 2.6f);
        if (!dragging && !ringsDirty)
        {
            for (int k = 0; k < pulsingRingIndices.Count; k++)
            {
                int idx = pulsingRingIndices[k];
                if (idx >= 0 && idx < stationRings.Count && stationRings[idx])
                {
                    var cur = stationRings[idx].color;
                    stationRings[idx].color = new Color(cur.r, cur.g, cur.b, cur.r > 0.93f ? warmPulse : pulse);
                }
            }
            return;
        }
        ringsDirty = false;
        pulsingRingIndices.Clear();
        for (int i = 0; i < stationRings.Count; i++)
        {
            bool active = dragging && (i == dragFrom || i == hoverStation);
            bool inDraft = dragging && draft.Contains(i);
            if (active)
                stationRings[i].color = new Color(0.95f, 0.42f, 0.26f, 0.95f);
            else if (inDraft)
                stationRings[i].color = new Color(0.91f, 0.63f, 0.71f, 0.60f);
            else if (i < stations.Count && stations[i].built && !stations[i].closed && StationInRegion(i, currentRegion) && StationLineCount(i) > 0)
            {
                float ratio = StationWaitRatio(i);
                float loss = StationLossRatio(i);
                if (ratio >= 1.0f) { stationRings[i].color = new Color(0.92f, 0.28f, 0.22f, pulse); pulsingRingIndices.Add(i); }
                else if (ratio >= 0.72f)
                {
                    stationRings[i].color = new Color(0.95f, 0.68f, 0.18f, qualityTier >= 2 ? warmPulse : 0.48f);
                    if (qualityTier >= 2) pulsingRingIndices.Add(i);
                }
                else if (loss >= 0.28f) stationRings[i].color = new Color(0.45f, 0.38f, 0.86f, 0.56f);
                else stationRings[i].color = new Color(1, 1, 1, 0);
            }
            else
                stationRings[i].color = new Color(1, 1, 1, 0);
        }
    }
    void PulseStation(int i)
    {
        pulseIdx = i; pulseTimer = 0.24f;
        if (qualityTier >= 2 && i >= 0 && i < stations.Count && StationInRegion(i, currentRegion))
            SpawnMapRipple(Pos(i), Gold, 128f);
    }
    void ResetBuildVisuals()
    {
        dragging = false; dragFrom = -1; hoverStation = -1;
        if (dragCostBadge) dragCostBadge.SetActive(false);
        if (previewImg) previewImg.gameObject.SetActive(false);
        if (dragTip) dragTip.gameObject.SetActive(false);
        MarkRingsDirty();
        UpdateRings();
    }

    void UpdateTrains(float dt)
    {
        trainsDirty = false;
        foreach (var line in lines)
        {
            if (!lineVisuals.TryGetValue(line.id, out var lv)) continue;
            if (!PathTouchesRegion(line.st, currentRegion))
            {
                for (int k = 0; k < lv.trains.Count; k++)
                {
                    if (lv.trains[k] && lv.trainActive[k]) { lv.trains[k].SetActive(false); lv.trainActive[k] = false; }
                }
                continue;
            }
            SyncLineTrainVisuals(line, lv);
            int activeCount = lv.trains.Count;
            if (lv.cachedSegs.Count == 0 || lv.cachedTotalLength < 1f || activeCount <= 0)
            {
                for (int k = 0; k < activeCount; k++)
                {
                    if (lv.trains[k] && lv.trainActive[k]) { lv.trains[k].SetActive(false); lv.trainActive[k] = false; }
                }
                continue;
            }
            float total = lv.cachedTotalLength;
            lv.t += dt * 58f * FleetTrait(line, 2) / total;
            bool loop = IsLoop(line.st);
            float period = loop ? 1f : 2f;
            lv.t = Mathf.Repeat(lv.t, period);
            // 飘字只反馈已连续入账的票款，不再重复改变余额。
            if (traffic != null) lv.credit += TrafficRevenue(line, traffic) * dt / 60f;

            int segCount = lv.cachedSegs.Count;
            for (int k = 0; k < activeCount; k++)
            {
                float carT = Mathf.Repeat(lv.t + (float)k / activeCount * period, period);
                float phase = loop ? carT : (carT <= 1f ? carT : 2f - carT);
                float target = phase * total, acc = 0, ang = 0;
                Vector2 pos = Vector2.zero;
                CachedTrainSegment hitSeg = null;
                for (int i = 0; i < segCount; i++)
                {
                    var cs = lv.cachedSegs[i];
                    if (target <= acc + cs.length || i == segCount - 1)
                    {
                        pos = Vector2.Lerp(cs.posA, cs.posB, (target - acc) / Mathf.Max(0.01f, cs.length));
                        ang = cs.angleDeg;
                        hitSeg = cs;
                        break;
                    }
                    acc += cs.length;
                }
                var tr = lv.trains[k];
                if (tr && hitSeg != null)
                {
                    int trainA = hitSeg.stationA, trainB = hitSeg.stationB;
                    bool segShown = (trainA >= 0 && trainA < stationVisible.Length && stationVisible[trainA])
                                 || (trainB >= 0 && trainB < stationVisible.Length && stationVisible[trainB]);
                    bool shouldShow = hitSeg.region == currentRegion && segShown;
                    if (lv.trainActive[k] != shouldShow) { tr.SetActive(shouldShow); lv.trainActive[k] = shouldShow; }
                    if (shouldShow)
                    {
                        var trt = lv.trainRts[k];
                        if (trt)
                        {
                            trt.anchoredPosition = pos + hitSeg.baseOffset * overlayZoom;
                            float dir = loop || carT <= 1f ? ang : ang + 180f;
                            trt.localRotation = Quaternion.Euler(0, 0, dir);
                        }
                    }
                }
                if (k == 0 && hitSeg != null)
                {
                    int near = -1; float best = 34f;
                    int stA = hitSeg.stationA, stB = hitSeg.stationB;
                    if (!stations[stA].closed)
                    {
                        float dA = Vector2.Distance(pos, hitSeg.posA);
                        if (dA < best) { best = dA; near = stA; }
                    }
                    if (!stations[stB].closed)
                    {
                        float dB = Vector2.Distance(pos, hitSeg.posB);
                        if (dB < best) { near = stB; }
                    }
                    if (near >= 0 && near != lv.trainLastSt[0])
                    {
                        lv.trainLastSt[0] = near; int amount = Mathf.FloorToInt(lv.credit);
                        if (amount > 0) { lv.credit -= amount; if (hitSeg.region == currentRegion) SpawnFloater(amount, Pos(near) + new Vector2(0, 34)); }
                    }
                }
            }
        }
    }

    int RaycastStation(Vector2 screenPos)
    {
        if (EventSystem.current == null) return -1;
        var ped = new PointerEventData(EventSystem.current); ped.position = screenPos;
        var results = new List<RaycastResult>();
        raycaster.Raycast(ped, results);
        foreach (var r in results)
        {
            var sp = r.gameObject.GetComponent<StationPress>();
            if (sp != null) return sp.index;
        }
        return -1;
    }

    // ---------- income ----------
    float TechMult() { return Mathf.Max(0.1f, 1f + ResearchBonus()); }
    void RecalcIncome(bool immediate = false)
    {
        RebuildForecasts(!immediate);
        traffic = trafficSlots[DispatchSlot()];
        RebuildOperatingRates();
        float total = 0;
        foreach (var line in lines) total += ForecastFor(line).Profit;
        total += StationCommercialTotalDaily() + TourismDividendDaily() + CycleDailySubsidy() + CrossBaySynergyDailyBonus();
        income = total;
        if (pageRoot == null && sceneryViewer == null)
        {
            UpdateLineTrafficColors();
            MarkRingsDirty();
        }
        UpdateRegionUnlocks();
        progress = Mathf.Clamp01(lines.Count / 6f * 0.5f + FoundCount() / (float)Mathf.Max(1, sceneries.Count) * 0.5f);
    }
    int FoundCount() { int n = 0; foreach (var s in sceneries) if (s.found) n++; return n; }
    void RPTick() { rp += 1f; RefreshHUD(); }

    // ---------- HUD & 大数格式化 ----------
    public static string FormatCoins(double amount)
    {
        if (double.IsNaN(amount) || double.IsInfinity(amount)) return "0";
        string sign = amount < 0 ? "-" : "";
        double abs = Math.Abs(amount);
        if (abs < 100000d) return sign + Math.Floor(abs).ToString("N0");
        if (abs < 10000000d) return sign + (abs / 10000d).ToString("0.#") + "万";
        if (abs < 100000000d) return sign + Math.Floor(abs / 10000d).ToString("N0") + "万";
        if (abs < 1000000000000d)
        {
            double yi = abs / 100000000d;
            if (yi < 100d) return sign + yi.ToString("0.00") + "亿";
            if (yi < 1000d) return sign + yi.ToString("0.0") + "亿";
            return sign + Math.Floor(yi).ToString("N0") + "亿";
        }
        return sign + (abs / 1000000000000d).ToString("0.00") + "万亿";
    }
    public static string FormatSignedNumber(double amount)
    {
        double v = Math.Round(amount);
        return (v >= 0 ? "+" : "-") + FormatCoins(Math.Abs(v));
    }
    public static string FormatSignedCoins(double amount)
    {
        double v = Math.Round(amount);
        return (v >= 0 ? "+¥" : "-¥") + FormatCoins(Math.Abs(v));
    }
    public static string FormatShortfall(double diff)
    {
        return FormatCoins(Math.Ceiling(Math.Max(1d, diff)));
    }
    float ResearchScaleBonusRatio()
    {
        int tier = DailyConnectedCount() / 15;
        return Mathf.Min(0.60f, tier * 0.15f);
    }
    float ResearchSpeedMultiplier()
    {
        return (1f + ResearchScaleBonusRatio()) * (turboDaysLeft > 0 ? 1.5f : 1f) * CoTechRpMult();
    }
    float EstimatedDailyRpRate()
    {
        return (GameDaySeconds / 36f) * ResearchSpeedMultiplier();
    }

    string lastHudCoinsText = "", lastHudIncomeText = "";
    int lastHudDiamonds = int.MinValue, lastHudProgress = int.MinValue;
    void RefreshHUD()
    {
        int gems = Mathf.FloorToInt(diamonds), pct = Mathf.RoundToInt(progress * 100);
        string coinStr = FormatCoins(coins);
        if (coinTxt && coinStr != lastHudCoinsText) { coinTxt.text = coinStr; lastHudCoinsText = coinStr; }
        string incomeStr = "预计日利润 " + FormatSignedNumber(income);
        if (incomeTxt && incomeStr != lastHudIncomeText) { incomeTxt.text = incomeStr; lastHudIncomeText = incomeStr; }
        if (diamondTxt && gems != lastHudDiamonds) { diamondTxt.text = gems.ToString(); lastHudDiamonds = gems; }
        if (pct != lastHudProgress) {
            if (progTxt) progTxt.text = pct + "%";
            if (progFill) progFill.sizeDelta = new Vector2(180f * progress, 8f);
            lastHudProgress = pct;
        }
        if (historyRpText)
        {
            int curRp = Mathf.FloorToInt(rp);
            if (curRp != lastDisplayedRp) { historyRpText.text = "科研点  " + curRp + " RP"; lastDisplayedRp = curRp; }
        }
        else lastDisplayedRp = int.MinValue;
        RefreshLeftRailReminders();
    }
    // ---------- panels ----------
    void TInk(Button b, Color c) { var t = b.GetComponentInChildren<Text>(); if (t) t.color = c; }

    GameObject OpenBubble(string title, float h, float w, Vector2 anchor, string panelKey = null)
        => OpenModalCore(title, h, w, false, anchor, panelKey);

    GameObject OpenPopup(string title, float h, float w, string panelKey = null)
        => OpenModalCore(title, h, w, true, Vector2.zero, panelKey);

    GameObject OpenModalCore(string title, float h, float w, bool isPopup, Vector2 anchor, string panelKey)
    {
        CloseLineBubble();
        string effectiveKey = !string.IsNullOrEmpty(panelKey) ? panelKey : "";
        bool isRefresh = activePanel != null && !string.IsNullOrEmpty(effectiveKey) && activePanelKey == effectiveKey;

        GetSafeMarginsInRoot(out float safeL, out float safeR, out float safeT, out float safeB);
        float rw = RootW, rh = RootH;
        float safeW = Mathf.Max(320f, rw - safeL - safeR);
        float safeH = Mathf.Max(420f, rh - safeT - safeB);
        float scale = Mathf.Min(1f, (safeW - 36f) / Mathf.Max(1f, w), (safeH - 36f) / Mathf.Max(1f, h));

        float x, y;
        if (isPopup)
        {
            x = safeL + (safeW - w * scale) * 0.5f;
            y = safeB + (safeH - h * scale) * 0.5f;
        }
        else
        {
            if (Mathf.Abs(anchor.x - W * 0.5f) < 1f && Mathf.Abs(anchor.y - H * 0.5f) < 1f)
                anchor = new Vector2(safeL + safeW * 0.5f, safeB + safeH * 0.5f);
            float sw = w * scale, sh = h * scale;
            float minX = safeL + 20f, maxX = Mathf.Max(minX, rw - safeR - sw - 20f);
            float minY = safeB + 170f, maxY = Mathf.Max(minY, rh - safeT - sh - 180f);
            if (minY > rh - safeT - sh - 20f) minY = Mathf.Max(safeB + 18f, rh - safeT - sh - 20f);
            x = Mathf.Clamp(anchor.x - sw / 2f, minX, maxX);
            y = anchor.y + 60f;
            if (y + sh > rh - safeT - 180f) y = anchor.y - sh - 60f;
            y = Mathf.Clamp(y, minY, maxY);
        }

        Color panelBgCol = new Color(CardBg.r, CardBg.g, CardBg.b, isPopup ? 0.965f : 0.985f);
        Vector2 centerPos = new Vector2(x + w * scale * 0.5f, y + h * scale * 0.5f);

        if (isRefresh)
        {
            StopBubbleAnim();
            for (int i = activePanel.transform.childCount - 1; i >= 0; i--)
            {
                var ch = activePanel.transform.GetChild(i).gameObject;
                ch.SetActive(false);
                ch.transform.SetParent(null, false);
                Destroy(ch);
            }

            if (isPopup)
            {
                if (!panelShadow)
                {
                    panelShadow = Rect("PShadow", root, 0f, 0f, w, h);
                    Rounded(panelShadow, new Color(0.10f, 0.08f, 0.06f, 0.16f));
                    panelShadow.GetComponent<Image>().raycastTarget = false;
                }
                var shRt = panelShadow.GetComponent<RectTransform>();
                shRt.pivot = new Vector2(0.5f, 0.5f);
                shRt.sizeDelta = new Vector2(w, h);
                shRt.anchoredPosition = centerPos + new Vector2(8f * scale, -10f * scale);
                shRt.localScale = Vector3.one * scale;
                var shCg = panelShadow.GetComponent<CanvasGroup>();
                if (shCg) shCg.alpha = 1f;
            }
            else if (panelShadow)
            {
                Destroy(panelShadow);
                panelShadow = null;
            }

            var rt = activePanel.GetComponent<RectTransform>();
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = centerPos;
            rt.localScale = Vector3.one * scale;
            var img = activePanel.GetComponent<Image>();
            if (img) img.color = panelBgCol;
            var cg = activePanel.GetComponent<CanvasGroup>();
            if (cg) { cg.alpha = 1f; cg.blocksRaycasts = true; cg.interactable = true; }
            if (scrim) scrim.transform.SetAsLastSibling();
            if (panelShadow) panelShadow.transform.SetAsLastSibling();
            activePanel.transform.SetAsLastSibling();
            if (toastGo) toastGo.transform.SetAsLastSibling();
        }
        else
        {
            ClosePanel(true);
            scrim = Rect(isPopup ? "Scrim" : "BubbleDismiss", root, 0, 0, RootW, RootH);
            var srt = scrim.GetComponent<RectTransform>();
            srt.anchorMin = Vector2.zero; srt.anchorMax = Vector2.one;
            srt.offsetMin = Vector2.zero; srt.offsetMax = Vector2.zero;
            var bg = Img(scrim, isPopup ? new Color(0.12f, 0.10f, 0.09f, 0.30f) : new Color(0, 0, 0, 0));
            var sb = scrim.AddComponent<Button>();
            sb.targetGraphic = bg;
            sb.transition = Selectable.Transition.None;
            sb.onClick.AddListener(ClosePanel);

            if (isPopup)
            {
                panelShadow = Rect("PShadow", root, 0f, 0f, w, h);
                var shRt = panelShadow.GetComponent<RectTransform>();
                shRt.pivot = new Vector2(0.5f, 0.5f);
                shRt.anchoredPosition = centerPos + new Vector2(8f * scale, -10f * scale);
                shRt.localScale = Vector3.one * scale;
                Rounded(panelShadow, new Color(0.10f, 0.08f, 0.06f, 0.16f));
                panelShadow.GetComponent<Image>().raycastTarget = false;
            }

            activePanel = Rect(isPopup ? "Popup" : "Bubble", root, 0f, 0f, w, h);
            var rt = activePanel.GetComponent<RectTransform>();
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = centerPos;
            rt.localScale = Vector3.one * scale;
            Rounded(activePanel, panelBgCol);
            if (!isPopup || IsNeonThemeActive()) SoftShadow(activePanel);
        }

        activePanelKey = effectiveKey;
        var heading = Txt(activePanel.transform, title, isPopup ? 34 : 32, Ink, 26, h - 64, w - 110, 46, TextAnchor.MiddleLeft);
        FitSingleLine(heading, isPopup ? 34 : 32);
        var cb = Btn(activePanel.transform, "Close", "×", 28, w - 72, h - 68, 52, 52, Paper2, ClosePanel);
        TInk(cb, Ink);
        panelOpenedFrame = isRefresh ? Time.frameCount : Time.frameCount + 1;
        if (!isRefresh)
        {
            PlayBubbleOpenSfx();
            AnimateBubblePop(activePanel, panelShadow);
        }
        return activePanel;
    }

    void ClosePanel() { ClosePanel(false); }

    void ClosePanel(bool force)
    {
        CaptureScrollPositions();
        if (!force && Time.frameCount <= panelOpenedFrame) return;
        StopBubbleAnim();
        if (!force && activePanel) { PlayCloseSfx(); suppressNextBtnClickSfx = false; }
        if (activePanel) Destroy(activePanel);
        if (scrim) Destroy(scrim);
        if (panelShadow) Destroy(panelShadow);
        activePanel = null; scrim = null; panelShadow = null;
        activePanelKey = "";
        panelOpenedFrame = -1;
        panelForecastRefresh = null;
        ClearStationOdArcs();
        if (!dragging && draft.Count > 0) CancelDraft();
        var onClosed = panelClosed; panelClosed = null;
        onClosed?.Invoke();
    }

    int ConnectedCount() { var set = new HashSet<int>(); foreach (var l in lines) foreach (var station in l.st) set.Add(station); return set.Count; }

    // ---------- scenery ----------
    void CheckSceneryOnLine(List<int> changedPath)
    {
        // 解锁以绑定车站是否被任一线路连通为准；解锁后不会因拆线撤销。
        bool changed = false;
        foreach (var sc in sceneries)
        {
            if (sc.unlocked) continue;
            foreach (var line in lines)
            {
                bool linked = false;
                foreach (int index in line.st)
                    if (!stations[index].closed && stations[index].region == sc.region && stations[index].name == sc.triggerStation) { linked = true; break; }
                if (!linked) continue;
                sc.unlocked = true; changed = true;
                if (!sc.found && TryGetSceneryContent(sc, out _, out _)) pendingCheckin.Add(sc);
                break;
            }
        }
        if (changed) RefreshSceneryViews();
        RefreshPendingBadge();
    }
    void RefreshPendingBadge()
    {
        if (sceneryBadge) sceneryBadge.SetActive(pendingCheckin.Count > 0 || CountClaimablePlaques() > 0);
    }
    float DistToSeg(Vector2 p, Vector2 a, Vector2 b)
    {
        var ab = b-a; float t = Mathf.Clamp01(Vector2.Dot(p-a,ab)/Mathf.Max(0.001f,ab.sqrMagnitude));
        return Vector2.Distance(p, a + ab*t);
    }
    bool CompleteSceneryCheckin(Scenery sc)
    {
        if (sc == null || !sc.unlocked || sc.found || !TryGetSceneryContent(sc, out _, out _)) return false;
        BeginUndo("名胜打卡");
        sc.found = true; diamonds += 20;
        RefreshSceneryMarkerBadge(sc);
        pendingCheckin.Remove(sc);
        RecalcIncome(); RefreshHUD(); RefreshPendingBadge();
        PlayCoinSfx(); TriggerHaptic(18);
        SpawnConfettiBurst(new Vector2(RootW * 0.5f, RootH * 0.52f), Sakura, Gold, 14);
        QueueStory("first_scenery", "窗外的第一处风景", "列车靠站，车窗外的风景被旅客拍成了明信片。\n你忽然发现，大家乘车去的不只是下一站，也是自己期待已久的地方。");
        EndUndo();
        return true;
    }
    // ---------- helpers ----------
    void Toast(string msg)
    {
        toastTrackingForecast = false;
        if (toastGo) Destroy(toastGo);
        GetSafeMarginsInRoot(out float safeL, out float safeR, out _, out _);
        float safeW = Mathf.Max(320f, RootW - safeL - safeR);
        float barY = topBarGo ? topBarGo.GetComponent<RectTransform>().anchoredPosition.y : (RootH - 310f);
        float toastY = barY - (zoomNotice && zoomNotice.activeSelf ? 102f : 38f);
        toastGo = Rect("Toast", root, safeL + (safeW - 620f) * 0.5f, toastY, 620, 68);
        var toastCanvas = toastGo.AddComponent<Canvas>(); toastCanvas.overrideSorting = true; toastCanvas.sortingOrder = 200;
        Rounded(toastGo, new Color(0.17f,0.15f,0.13f,0.88f)); SoftShadow(toastGo);
        toastGo.GetComponent<Image>().raycastTarget = false;
        toastTxt = Txt(toastGo.transform, msg, 24, new Color(1,0.97f,0.9f), 16, 8, 588, 52, TextAnchor.MiddleCenter);
        toastTimer = 2.2f;
    }
    void ShowForecastDelayToast()
    {
        if (pendingForecastMask == 0)
        {
            Toast("运力已更新");
            return;
        }
        float est = Mathf.Max(0.2f, lastForecastDuration - Mathf.Max(0f, Time.unscaledTime - forecastStartUnscaledTime));
        Toast($"运力更新有延迟（约 {est:0.0}s）");
        toastTrackingForecast = true;
        toastTimer = 15f;
    }
    static int CountMaskBits(int mask)
    {
        int c = 0;
        while (mask != 0) { c += mask & 1; mask >>= 1; }
        return c;
    }
    void Confirm(string title, string body, Action ok, Action cancel = null)
    {
        float bodyHeight = Mathf.Clamp(MeasureTextHeight(body, 26, 792) + 10, 44, H - 440);
        var p = OpenPopup(title, bodyHeight + 240, 860);
        ScrollableText(p.transform, body, 26, InkSoft, 34, 150, 792, bodyHeight);
        Btn(p.transform, "Ok", "确 认", 32, 34, 36, 380, 92, Vermilion, () => { ClosePanel(); ok(); });
        var no = Btn(p.transform, "No", "取 消", 32, 446, 36, 380, 92, new Color(0.92f,0.89f,0.83f), () => { ClosePanel(); if (cancel != null) cancel(); });
        TInk(no, Ink);
    }

    List<int> CollectRedCapacityStationsInRegion()
    {
        var list = new List<int>();
        for (int i = 0; i < stations.Count; i++)
        {
            var st = stations[i];
            if (st == null || !st.built || st.closed || st.level >= 10) continue;
            if (!StationInRegion(i, currentRegion) || StationLineCount(i) <= 0) continue;
            if (StationWaitRatio(i) >= 1.0f) list.Add(i);
        }
        return list;
    }

    List<Line> CollectBoostableCongestedLinesInRegion()
    {
        var list = new List<Line>();
        int slot = DispatchSlot();
        for (int li = 0; li < lines.Count; li++)
        {
            var line = lines[li];
            if (line == null || !PathTouchesRegion(line.st, currentRegion)) continue;
            if (line.dispatch[slot] >= line.trains) continue;

            bool isCongested = false;
            for (int si = 0; si < line.st.Count - 1; si++)
            {
                if (SegmentLoadRatio(line, line.st[si], line.st[si + 1]) >= 0.78f)
                {
                    isCongested = true;
                    break;
                }
            }
            if (isCongested) list.Add(line);
        }
        return list;
    }

    void AttachAdBadge(Transform parent, float x, float y)
    {
        var badge = Rect("AdBadge", parent, x, y, 58f, 30f);
        Rounded(badge, Gold);
        badge.GetComponent<Image>().raycastTarget = false;
        var t = Txt(badge.transform, "AD", 18, Color.white, 0, 0, 58f, 30f, TextAnchor.MiddleCenter);
        t.fontStyle = FontStyle.Bold;
        t.raycastTarget = false;
    }

    void OpenQuickUpgradeBubble()
    {
        CloseLineBubble();
        var redStations = CollectRedCapacityStationsInRegion();
        var hotLines = CollectBoostableCongestedLinesInRegion();

        var p = OpenBubble("一键升级", 420f, 760f, new Vector2(W * 0.5f, H * 0.5f), "quick_upgrade");

        // 功能 1：一键升级红色容量城市（AD）
        var card1 = Rect("UpgradeRedCard", p.transform, 28f, 206f, 704f, 124f);
        Rounded(card1, Paper2);
        Txt(card1.transform, "一键升级红色容量城市", 26, Ink, 20f, 66f, 430f, 40f, TextAnchor.MiddleLeft);
        string sub1 = redStations.Count > 0 ? $"当前区域 {redStations.Count} 座红色爆满城市可免费升 1 级" : "当前区域暂无红色容量爆满城市";
        Txt(card1.transform, sub1, 20, InkSoft, 20f, 18f, 450f, 36f, TextAnchor.MiddleLeft);
        var btn1 = Btn(card1.transform, "BtnUpgradeRed", redStations.Count > 0 ? "立即升级" : "暂无目标", 23, 494f, 24f, 190f, 76f,
            redStations.Count > 0 ? Vermilion : new Color(0.75f, 0.72f, 0.68f), () =>
            {
                if (redStations.Count == 0) { Toast("当前区域暂无可升级的红色容量城市"); return; }
                ClosePanel(true);
                RequestTapTapRewardedAd("quick_upgrade_red_stations", "一键升级红色容量城市", () =>
                {
                    var targets = CollectRedCapacityStationsInRegion();
                    if (targets.Count == 0) { Toast("当前暂无需要升级的红色容量城市"); return; }
                    for (int i = 0; i < targets.Count; i++)
                    {
                        stations[targets[i]].level = Mathf.Min(10, stations[targets[i]].level + 1);
                    }
                    RefreshStationDots();
                    MarkRingsDirty();
                    RecalcIncome();
                    RefreshHUD();
                    SaveGame();
                    ShowForecastDelayToast();
                });
            });
        TInk(btn1, Color.white);
        AttachAdBadge(card1.transform, 622f, 88f);

        // 功能 2：一键为高拥堵线路增班（不调度机车）（AD）
        var card2 = Rect("BoostHotLineCard", p.transform, 28f, 58f, 704f, 124f);
        Rounded(card2, Paper2);
        Txt(card2.transform, "一键高拥堵线路增班", 26, Ink, 20f, 66f, 430f, 40f, TextAnchor.MiddleLeft);
        string sub2 = hotLines.Count > 0 ? $"当前 {hotLines.Count} 条高拥堵线路有空闲配车可增班" : "暂无可增班的高拥堵线路（不调度机车）";
        Txt(card2.transform, sub2, 20, InkSoft, 20f, 18f, 450f, 36f, TextAnchor.MiddleLeft);
        var btn2 = Btn(card2.transform, "BtnBoostHot", hotLines.Count > 0 ? "立即增班" : "暂无目标", 23, 494f, 24f, 190f, 76f,
            hotLines.Count > 0 ? Indigo : new Color(0.75f, 0.72f, 0.68f), () =>
            {
                if (hotLines.Count == 0) { Toast("暂无可增班的高拥堵线路（不调度机车）"); return; }
                ClosePanel(true);
                RequestTapTapRewardedAd("quick_boost_congested_lines", "一键高拥堵线路增班", () =>
                {
                    var targets = CollectBoostableCongestedLinesInRegion();
                    if (targets.Count == 0) { Toast("暂无可增班的高拥堵线路"); return; }
                    int slot = DispatchSlot();
                    bool hasPeak = techDone.Contains("dispatch_peak");
                    for (int i = 0; i < targets.Count; i++)
                    {
                        var l = targets[i];
                        if (!hasPeak)
                        {
                            for (int s = 0; s < l.dispatch.Length; s++) l.dispatch[s] = l.trains;
                        }
                        else
                        {
                            l.dispatch[slot] = l.trains;
                        }
                    }
                    RecalcIncome();
                    RefreshHUD();
                    SaveGame();
                    ShowForecastDelayToast();
                });
            });
        TInk(btn2, Color.white);
        AttachAdBadge(card2.transform, 622f, 88f);
    }

    GameObject Rect(string name, Transform parent, float x, float y, float w, float h)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0,0); rt.pivot = new Vector2(0,0);
        rt.anchoredPosition = new Vector2(x,y); rt.sizeDelta = new Vector2(w,h);
        return go;
    }
    Image Img(GameObject go, Color c, Sprite sp = null)
    {
        var im = go.AddComponent<Image>(); im.color = c; im.sprite = sp;
        return im;
    }
    void Rounded(GameObject go, Color c) { var im = Img(go, c, spriteRound); im.type = Image.Type.Sliced; }
    Text Txt(Transform p, string s, int size, Color c, float x, float y, float w, float h, TextAnchor a)
    {
        var go = Rect("T", p, x, y, w, h);
        var t = go.AddComponent<Text>(); t.font = font; t.fontSize = ScaledFontSize(size); t.color = c; t.text = DisplayPlaceAliases(s);
        t.alignment = a; t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        return t;
    }
    float MeasureTextHeight(string value, int size, float width)
    {
        var settings = new TextGenerationSettings { font = font, fontSize = ScaledFontSize(size), color = Color.white,
            scaleFactor = 1, lineSpacing = 1, richText = true, generationExtents = new Vector2(width, 0),
            horizontalOverflow = HorizontalWrapMode.Wrap, verticalOverflow = VerticalWrapMode.Overflow };
        return Mathf.Ceil(new TextGenerator().GetPreferredHeight(DisplayPlaceAliases(value), settings));
    }
    void FitSingleLine(Text text, int size)
    {
        int scaled = ScaledFontSize(size);
        text.resizeTextForBestFit = true; text.resizeTextMinSize = Mathf.Max(15, Mathf.RoundToInt(size * 0.68f));
        text.resizeTextMaxSize = scaled; text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
    }
    Text ScrollableText(Transform parent, string value, int size, Color color, float x, float y, float width, float height)
    {
        var view = Rect("TextViewport", parent, x, y, width, height);
        var bg = Img(view, Color.clear); bg.raycastTarget = true;
        view.AddComponent<RectMask2D>();
        var content = Rect("TextContent", view.transform, 0, 0, width, Mathf.Max(height, MeasureTextHeight(value, size, width) + 8));
        var rt = content.GetComponent<RectTransform>(); rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1); rt.anchoredPosition = Vector2.zero;
        var text = Txt(content.transform, value, size, color, 0, 0, width, rt.sizeDelta.y, TextAnchor.UpperLeft);
        var scroll = view.AddComponent<ScrollRect>(); scroll.viewport = view.GetComponent<RectTransform>();
        scroll.content = rt; scroll.horizontal = false; scroll.vertical = true; scroll.scrollSensitivity = 70;
        BindScrollPosition(scroll, "text:" + (value == null ? 0 : value.GetHashCode()));
        return text;
    }
    Button Btn(Transform p, string name, string label, int size, float x, float y, float w, float h, Color c, Action onClick)
    {
        var go = Rect(name, p, x, y, w, h);
        var im = Img(go, c, spriteRound); im.type = Image.Type.Sliced;
        int scaled = ScaledFontSize(size);
        var t = Txt(go.transform, label, size, new Color(1,0.98f,0.94f), 0, 0, w, h, TextAnchor.MiddleCenter);
        t.resizeTextForBestFit = true; t.resizeTextMinSize = Mathf.Max(15, Mathf.RoundToInt(size * 0.68f)); t.resizeTextMaxSize = scaled;
        t.verticalOverflow = VerticalWrapMode.Truncate;
        var b = go.AddComponent<Button>(); b.targetGraphic = im; b.onClick.AddListener(() => { PlayClickSfx(); onClick(); });
        if (qualityTier >= 2 && !go.GetComponent<FleetPressFeedback>()) go.AddComponent<FleetPressFeedback>();
        return b;
    }
    void SoftShadow(GameObject go)
    {
        if (IsNeonThemeActive())
        {
            var neonCyan = go.AddComponent<Shadow>();
            neonCyan.effectColor = new Color(0.06f, 0.94f, 0.92f, 0.48f);
            neonCyan.effectDistance = new Vector2(3f, -2.5f);
            neonCyan.enabled = qualityTier > 0;
            managedShadows.Add(neonCyan);

            if (qualityTier >= 2)
            {
                var neonPink = go.AddComponent<Shadow>();
                neonPink.effectColor = new Color(1.00f, 0.14f, 0.58f, 0.46f);
                neonPink.effectDistance = new Vector2(-3f, 2.5f);
                neonPink.enabled = true;
                managedShadows.Add(neonPink);
            }
            return;
        }
        var sh = go.AddComponent<Shadow>();
        sh.effectColor = new Color(0.16f,0.13f,0.10f,0.16f);
        sh.effectDistance = new Vector2(0, -5);
        sh.enabled = qualityTier > 0;
        managedShadows.Add(sh);
    }
    void TextShadow(Text text, Color color)
    {
        var shadow = text.gameObject.AddComponent<Shadow>(); shadow.effectColor = color; shadow.enabled = qualityTier > 0;
        managedShadows.Add(shadow);
    }
    void Label(RectTransform p, string s, int size, float x, float y, Color c, float w)
    {
        var t = Txt(p, s, size, c, MAP_X + x*MAP_W - w/2, MAP_Y + y*MAP_H, w, 40, TextAnchor.MiddleCenter);
        TextShadow(t, new Color(1,1,1,0.7f));
    }
    void PortalLabel(RectTransform p, float x, float y, string s)
    {
        var t = Txt(p, s, 22, Vermilion, MAP_X + x*MAP_W - 110, MAP_Y + y*MAP_H, 220, 32, TextAnchor.MiddleCenter);
        TextShadow(t, new Color(1,1,1,0.85f));
    }

    // ---------- sprites / texture ----------
    Sprite MakeWhite()
    {
        var t = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        t.wrapMode = TextureWrapMode.Clamp; t.filterMode = FilterMode.Bilinear;
        var px = new Color[16]; for (int i = 0; i < 16; i++) px[i] = Color.white;
        t.SetPixels(px); t.Apply(false, true);
        return Sprite.Create(t, new Rect(0, 0, 4, 4), new Vector2(0, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(0, 0, 0, 0));
    }
    Sprite MakeRounded(int s, int r)
    {
        var t = new Texture2D(s, s, TextureFormat.RGBA32, false);
        t.wrapMode = TextureWrapMode.Clamp; t.filterMode = FilterMode.Bilinear;
        var px = new Color[s * s];
        for (int y = 0; y < s; y++) for (int x = 0; x < s; x++)
        {
            float dx = Mathf.Max(0f, Mathf.Max(r + 0.5f - x, x - (s - 1.5f - r)));
            float dy = Mathf.Max(0f, Mathf.Max(r + 0.5f - y, y - (s - 1.5f - r)));
            float dist = (dx > 0f && dy > 0f) ? Mathf.Sqrt(dx * dx + dy * dy) - r : Mathf.Max(dx, dy) - r;
            float a = Mathf.Clamp01(0.65f - dist);
            px[y * s + x] = new Color(1f, 1f, 1f, a);
        }
        t.SetPixels(px); t.Apply(false, true);
        return Sprite.Create(t, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(14, 14, 14, 14));
    }
    Sprite MakeCircle(int s)
    {
        var t = new Texture2D(s, s, TextureFormat.RGBA32, false);
        t.wrapMode = TextureWrapMode.Clamp; t.filterMode = FilterMode.Bilinear;
        var px = new Color[s * s];
        float c = (s - 1) * 0.5f, radius = s * 0.5f - 1.5f;
        for (int y = 0; y < s; y++) for (int x = 0; x < s; x++)
        {
            float d = Vector2.Distance(new Vector2(x, y), new Vector2(c, c));
            float a = Mathf.Clamp01((radius - d + 0.7f) / 1.2f);
            px[y * s + x] = new Color(1f, 1f, 1f, a);
        }
        t.SetPixels(px); t.Apply(false, true);
        return Sprite.Create(t, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f);
    }
    Sprite MakeRing(int s, float th)
    {
        var t = new Texture2D(s, s, TextureFormat.RGBA32, false);
        t.wrapMode = TextureWrapMode.Clamp; t.filterMode = FilterMode.Bilinear;
        var px = new Color[s * s];
        float c = (s - 1) * 0.5f, outer = s * 0.5f - 1.0f, inner = outer - th;
        for (int y = 0; y < s; y++) for (int x = 0; x < s; x++)
        {
            float d = Vector2.Distance(new Vector2(x, y), new Vector2(c, c));
            float a = Mathf.Clamp01(outer - d + 0.65f) * Mathf.Clamp01(d - inner + 0.65f);
            px[y * s + x] = new Color(1f, 1f, 1f, a);
        }
        t.SetPixels(px); t.Apply(false, true);
        return Sprite.Create(t, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(0, 0, 0, 0));
    }
    Sprite MakeHandDrawnRing(int s, float th)
    {
        var t = new Texture2D(s, s, TextureFormat.RGBA32, false);
        t.wrapMode = TextureWrapMode.Clamp; t.filterMode = FilterMode.Bilinear;
        var px = new Color[s * s];
        Vector2 center = new Vector2((s - 1) * 0.5f, (s - 1) * 0.5f);
        float baseOuter = s * 0.5f - 1.5f;
        for (int y = 0; y < s; y++) for (int x = 0; x < s; x++)
        {
            Vector2 delta = new Vector2(x, y) - center;
            float d = delta.magnitude;
            float angle = Mathf.Atan2(delta.y, delta.x);
            float jitter = Mathf.Sin(angle * 3f) * 0.85f + Mathf.Cos(angle * 7f) * 0.45f;
            float outer = baseOuter + jitter;
            float inner = outer - th - Mathf.Sin(angle * 5f) * 0.35f;
            float a = Mathf.Clamp01(outer - d + 0.7f) * Mathf.Clamp01(d - inner + 0.7f);
            px[y * s + x] = new Color(1f, 1f, 1f, a);
        }
        t.SetPixels(px); t.Apply(false, true);
        return Sprite.Create(t, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(0, 0, 0, 0));
    }
    // 无描边手绘水彩笔触条（柔边+轻微手工抖动+左右圆润笔锋，支持 9-slice 横向拉伸）
    Sprite MakeHandDrawnStroke(int w, int h)
    {
        var t = new Texture2D(w, h, TextureFormat.RGBA32, false);
        t.wrapMode = TextureWrapMode.Clamp; t.filterMode = FilterMode.Bilinear;
        var px = new Color[w * h];
        float cy = (h - 1) * 0.5f;
        float baseHalf = h * 0.34f;
        float capPx = h * 0.42f;
        for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
        {
            float u = x / (float)(w - 1);
            float dx = Mathf.Max(0f, Mathf.Max(capPx - x, x - (w - 1 - capPx)));
            float dy = Mathf.Abs(y - cy);
            float midWeight = Mathf.Clamp01(1f - dx / Mathf.Max(1f, capPx));
            float wobble = (Mathf.Sin(u * Mathf.PI * 5f + 0.4f) * 0.78f + Mathf.Cos(u * Mathf.PI * 11f) * 0.42f) * midWeight;
            float dEdge = dx > 0f
                ? Mathf.Sqrt(dx * dx + dy * dy) - (baseHalf + wobble * 0.45f)
                : dy - (baseHalf + wobble);
            float alpha = Mathf.Clamp01(1.1f - dEdge / 1.65f);
            float grain = 0.94f + 0.06f * Mathf.Sin(u * 41f + y * 1.7f);
            px[y * w + x] = new Color(1f, 1f, 1f, alpha * grain);
        }
        t.SetPixels(px); t.Apply(false, true);
        int borderX = Mathf.RoundToInt(capPx);
        return Sprite.Create(t, new Rect(0, 0, w, h), new Vector2(0f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(borderX, 0, borderX, 0));
    }
    Sprite MakeDiamond(int s)
    {
        var t = new Texture2D(s, s, TextureFormat.RGBA32, false);
        t.wrapMode = TextureWrapMode.Clamp; t.filterMode = FilterMode.Bilinear;
        var px = new Color[s * s];
        float c = (s - 1) * 0.5f, limit = s * 0.44f;
        for (int y = 0; y < s; y++) for (int x = 0; x < s; x++)
        {
            float dx = Mathf.Abs(x - c), dy = Mathf.Abs(y - c);
            float dist = (dx + dy - limit) * 0.70710678f;
            float a = Mathf.Clamp01(0.75f - dist);
            px[y * s + x] = new Color(1f, 1f, 1f, a);
        }
        t.SetPixels(px); t.Apply(false, true);
        return Sprite.Create(t, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f);
    }

    Texture2D MakeFadeTexture()
    {
        int h = 64; var t = new Texture2D(1, h, TextureFormat.RGBA32, false);
        t.wrapMode = TextureWrapMode.Clamp; t.filterMode = FilterMode.Bilinear;
        var px = new Color[h];
        for (int y = 0; y < h; y++)
        {
            float a = Mathf.Pow(y / (float)(h - 1), 1.15f);
            px[y] = new Color(1f, 1f, 1f, a);
        }
        t.SetPixels(px); t.Apply(false, true);
        return t;
    }

    // ---------- 主题切换、实机预览与涂装流派体系 ----------
    int previewRestoreTheme = -1;
    GameObject themePreviewBannerGo;

    struct ThemeSpec
    {
        public string name, shortName, subtitle;
        public Color paper, paper2, cardBg, headerBg, ink, inkSoft, indigo, matcha, sea, mapTint, fade, backdropTop, backdropBottom;
        public bool isNeon;
    }
    static readonly ThemeSpec[] Themes = {
        new ThemeSpec {
            name = "和纸手帐", shortName = "和纸", subtitle = "默认 · 暖阳宣纸手绘质感，全天候舒适护眼",
            paper = new Color(0.965f, 0.937f, 0.890f), paper2 = new Color(0.925f, 0.886f, 0.816f),
            cardBg = new Color(0.992f, 0.982f, 0.955f, 0.96f), headerBg = new Color(0.952f, 0.922f, 0.870f),
            ink = new Color(0.184f, 0.165f, 0.149f), inkSoft = new Color(0.42f, 0.39f, 0.35f),
            indigo = new Color(0.231f, 0.353f, 0.549f), matcha = new Color(0.435f, 0.627f, 0.353f),
            sea = new Color(0.663f, 0.788f, 0.808f), mapTint = Color.white,
            fade = new Color(0.985f, 0.97f, 0.93f, 0.90f),
            backdropTop = new Color(0.74f, 0.86f, 0.87f, 1f), backdropBottom = new Color(0.52f, 0.70f, 0.77f, 1f)
        },
        new ThemeSpec {
            name = "工程蓝图", shortName = "蓝图", subtitle = "基建晒图 · 普鲁士蓝制图网格，硬核规划控最爱",
            paper = new Color(0.875f, 0.925f, 0.965f), paper2 = new Color(0.795f, 0.868f, 0.930f),
            cardBg = new Color(0.945f, 0.975f, 0.995f, 0.96f), headerBg = new Color(0.835f, 0.898f, 0.950f),
            ink = new Color(0.090f, 0.185f, 0.310f), inkSoft = new Color(0.260f, 0.390f, 0.540f),
            indigo = new Color(0.140f, 0.420f, 0.720f), matcha = new Color(0.220f, 0.560f, 0.520f),
            sea = new Color(0.420f, 0.610f, 0.760f), mapTint = new Color(0.82f, 0.92f, 1.00f, 1f),
            fade = new Color(0.88f, 0.94f, 0.98f, 0.90f),
            backdropTop = new Color(0.52f, 0.70f, 0.84f, 1f), backdropBottom = new Color(0.32f, 0.50f, 0.68f, 1f)
        },
        new ThemeSpec {
            name = "复古星轨报", shortName = "复古", subtitle = "1920s 特刊 · 怀旧牛皮纸褐配深铅字排印风",
            paper = new Color(0.925f, 0.865f, 0.765f), paper2 = new Color(0.855f, 0.780f, 0.665f),
            cardBg = new Color(0.965f, 0.925f, 0.845f, 0.96f), headerBg = new Color(0.895f, 0.825f, 0.715f),
            ink = new Color(0.165f, 0.115f, 0.085f), inkSoft = new Color(0.450f, 0.355f, 0.275f),
            indigo = new Color(0.460f, 0.285f, 0.195f), matcha = new Color(0.380f, 0.520f, 0.310f),
            sea = new Color(0.620f, 0.680f, 0.640f), mapTint = new Color(0.98f, 0.90f, 0.76f, 1f),
            fade = new Color(0.94f, 0.88f, 0.78f, 0.90f),
            backdropTop = new Color(0.72f, 0.74f, 0.68f, 1f), backdropBottom = new Color(0.52f, 0.56f, 0.50f, 1f)
        },
        new ThemeSpec {
            name = "青湾暮色", shortName = "暮色", subtitle = "都市薄暮 · 温柔紫靛夜航色调，伴随晚高峰灯火",
            paper = new Color(0.895f, 0.885f, 0.935f), paper2 = new Color(0.815f, 0.800f, 0.875f),
            cardBg = new Color(0.955f, 0.948f, 0.980f, 0.96f), headerBg = new Color(0.855f, 0.842f, 0.905f),
            ink = new Color(0.160f, 0.135f, 0.240f), inkSoft = new Color(0.390f, 0.355f, 0.490f),
            indigo = new Color(0.380f, 0.295f, 0.580f), matcha = new Color(0.340f, 0.560f, 0.480f),
            sea = new Color(0.420f, 0.495f, 0.620f), mapTint = new Color(0.86f, 0.84f, 0.95f, 1f),
            fade = new Color(0.90f, 0.89f, 0.94f, 0.90f),
            backdropTop = new Color(0.52f, 0.56f, 0.70f, 1f), backdropBottom = new Color(0.34f, 0.38f, 0.52f, 1f)
        },
        new ThemeSpec {
            name = "幻夜霓虹", shortName = "霓虹", subtitle = "3D 视界 · 护眼黑曜夜图配荧光青红立体撞色",
            paper = new Color(0.055f, 0.062f, 0.145f), paper2 = new Color(0.115f, 0.125f, 0.255f),
            cardBg = new Color(0.088f, 0.095f, 0.205f, 0.97f), headerBg = new Color(0.042f, 0.048f, 0.115f),
            ink = new Color(0.95f, 0.98f, 1.00f), inkSoft = new Color(0.62f, 0.74f, 0.96f),
            indigo = new Color(0.94f, 0.14f, 0.56f), matcha = new Color(0.08f, 0.84f, 0.76f),
            sea = new Color(0.045f, 0.065f, 0.220f), mapTint = new Color(0.46f, 0.52f, 0.78f, 1f),
            fade = new Color(0.040f, 0.050f, 0.140f, 0.92f),
            backdropTop = new Color(0.04f, 0.07f, 0.26f, 1f), backdropBottom = new Color(0.02f, 0.02f, 0.08f, 1f),
            isNeon = true
        }
    };

    bool IsNeonThemeActive()
    {
        return Themes[Mathf.Clamp(currentTheme, 0, Themes.Length - 1)].isNeon;
    }

    static long NowUnixSeconds() { return DateTimeOffset.UtcNow.ToUnixTimeSeconds(); }
    bool IsThemeTrialActive(int theme)
    {
        return themeTrialUntil.TryGetValue(theme, out long until) && until > NowUnixSeconds();
    }
    bool IsThemeAvailable(int theme)
    {
        if (theme <= 0 || theme >= Themes.Length) return theme == 0;
        return ownedThemes.Contains(theme) || IsThemeTrialActive(theme);
    }
    string ThemeTrialRemainingText(int theme)
    {
        if (!themeTrialUntil.TryGetValue(theme, out long until)) return "";
        long rem = until - NowUnixSeconds();
        if (rem <= 0) return "";
        long h = rem / 3600, m = (rem % 3600) / 60;
        return "试用中 " + h + "时" + m + "分";
    }
    void ApplyThemeColors(int theme)
    {
        theme = Mathf.Clamp(theme, 0, Themes.Length - 1);
        var spec = Themes[theme];
        Paper = spec.paper; Paper2 = spec.paper2; CardBg = spec.cardBg; HeaderBg = spec.headerBg;
        Ink = spec.ink; InkSoft = spec.inkSoft; Indigo = spec.indigo; Matcha = spec.matcha; Sea = spec.sea;
    }
    void ApplyTheme(int theme, bool rebuildUI = true, bool isPreview = false)
    {
        theme = Mathf.Clamp(theme, 0, Themes.Length - 1);
        if (!isPreview)
        {
            previewRestoreTheme = -1;
            DestroyThemePreviewBanner();
        }
        currentTheme = theme;
        ApplyThemeColors(theme);
        var spec = Themes[theme];
        if (Camera.main != null) Camera.main.backgroundColor = spec.sea;
        if (backdropGraphic)
        {
            backdropGraphic.top = spec.backdropTop;
            backdropGraphic.bottom = spec.backdropBottom;
            backdropGraphic.SetVerticesDirty();
        }
        if (paperBgGo)
        {
            var pi = paperBgGo.GetComponent<Image>();
            if (pi) pi.color = spec.sea;
        }
        if (hkMapRaw) hkMapRaw.color = spec.mapTint;
        if (macauMapRaw) macauMapRaw.color = spec.mapTint;
        if (guangdongMapRaw) guangdongMapRaw.color = spec.mapTint;
        if (topFadeImg) topFadeImg.color = new Color(spec.fade.r, spec.fade.g, spec.fade.b, 0.92f);
        if (botFadeImg) botFadeImg.color = new Color(spec.fade.r, spec.fade.g, spec.fade.b, 0.85f);
        if (mapEdgeFeathers != null)
        {
            var fc = new Color(spec.sea.r, spec.sea.g, spec.sea.b, 0.90f);
            var fcTop = new Color(spec.fade.r, spec.fade.g, spec.fade.b, 0.85f);
            for (int i = 0; i < mapEdgeFeathers.Length; i++)
                if (mapEdgeFeathers[i]) mapEdgeFeathers[i].color = i == 0 ? fcTop : fc;
        }
        RefreshMapThemeLabels();
        if (rebuildUI && root) RebuildChromeUI();
    }
    void RefreshMapThemeLabels()
    {
        bool neon = IsNeonThemeActive();
        Color lblColor = neon ? new Color(0.95f, 0.98f, 1.00f, 0.98f) : new Color(0.16f, 0.14f, 0.11f, 0.96f);
        Color lblShadow = neon ? new Color(0.03f, 0.04f, 0.16f, 0.94f) : new Color(0.99f, 0.98f, 0.94f, 0.95f);
        Color dstColor = neon ? new Color(1.00f, 0.62f, 0.86f, 0.96f) : new Color(0.38f, 0.35f, 0.31f, 0.90f);
        Color dstShadow = neon ? new Color(0.03f, 0.04f, 0.16f, 0.94f) : new Color(1f, 0.99f, 0.96f, 0.95f);
        Color leaderColor = neon ? new Color(0.45f, 0.88f, 1.00f, 0.45f) : new Color(0.15f, 0.14f, 0.13f, 0.32f);
        foreach (var kv in stationLabels)
        {
            if (!kv.Value) continue;
            kv.Value.color = lblColor;
            var sh = kv.Value.GetComponent<Shadow>();
            if (sh) sh.effectColor = lblShadow;
        }
        foreach (var kv in stationDestinations)
        {
            if (!kv.Value) continue;
            kv.Value.color = dstColor;
            var sh = kv.Value.GetComponent<Shadow>();
            if (sh) sh.effectColor = dstShadow;
        }
        foreach (var kv in stationLeaders) if (kv.Value) kv.Value.color = leaderColor;
    }
    void StartThemePreview(int theme)
    {
        theme = Mathf.Clamp(theme, 0, Themes.Length - 1);
        if (previewRestoreTheme < 0) previewRestoreTheme = currentTheme;
        ClosePage();
        ClosePanel(true);
        ApplyTheme(theme, true, true);
        ShowThemePreviewBanner(theme);
        Toast("正以「" + Themes[theme].name + "」实机预览，可自由缩放地图与翻阅各页面");
    }
    void EndThemePreview(bool reopenShop = false)
    {
        int restore = previewRestoreTheme;
        previewRestoreTheme = -1;
        DestroyThemePreviewBanner();
        if (restore >= 0)
        {
            if (!IsThemeAvailable(restore)) restore = 0;
            ApplyTheme(restore, true, false);
        }
        if (reopenShop) OpenThemeShopScreen();
    }
    void DestroyThemePreviewBanner()
    {
        if (themePreviewBannerGo) { Destroy(themePreviewBannerGo); themePreviewBannerGo = null; ApplyResponsiveLayout(false); }
    }
    void ShowThemePreviewBanner(int theme)
    {
        DestroyThemePreviewBanner();
        if (!root) return;
        float bw = 860f, bh = 86f;
        GetSafeMarginsInRoot(out float safeL, out float safeR, out _, out float safeB);
        float safeW = Mathf.Max(320f, RootW - safeL - safeR);
        themePreviewBannerGo = Rect("ThemePreviewBanner", root, safeL + (safeW - bw) * 0.5f, safeB + 186f, bw, bh);
        Rounded(themePreviewBannerGo, new Color(0.05f, 0.06f, 0.14f, 0.94f));
        SoftShadow(themePreviewBannerGo);
        var bImg = themePreviewBannerGo.GetComponent<Image>();
        if (bImg) bImg.raycastTarget = true;

        Img(Rect("Badge", themePreviewBannerGo.transform, 18f, 25f, 14f, 36f), Themes[theme].indigo, spriteRound).raycastTarget = false;
        var tipTxt = Txt(themePreviewBannerGo.transform, "实机预览中 · " + Themes[theme].name, 25, Color.white, 44f, 0f, 370f, bh, TextAnchor.MiddleLeft);
        FitSingleLine(tipTxt, 25);

        var unlockBtn = Btn(themePreviewBannerGo.transform, "PreviewToUnlockBtn", "回商店解锁", 22, 434f, 14f, 236f, 58f, new Color(0.08f, 0.80f, 0.72f), () => {
            EndThemePreview(true);
        });
        TInk(unlockBtn, new Color(0.04f, 0.06f, 0.14f));
        var exitBtn = Btn(themePreviewBannerGo.transform, "PreviewExitBtn", "退出预览", 22, 686f, 14f, 156f, 58f, new Color(0.24f, 0.26f, 0.40f), () => {
            EndThemePreview(false);
            Toast("已还原为「" + Themes[currentTheme].name + "」");
        });
        TInk(exitBtn, Color.white);
        EnsurePreviewBannerOnTop();
        ApplyResponsiveLayout(false);
    }
    void EnsurePreviewBannerOnTop()
    {
        if (!themePreviewBannerGo) return;
        themePreviewBannerGo.transform.SetAsLastSibling();
        if (scrim) scrim.transform.SetAsLastSibling();
        if (panelShadow) panelShadow.transform.SetAsLastSibling();
        if (activePanel) activePanel.transform.SetAsLastSibling();
        if (toastGo) toastGo.transform.SetAsLastSibling();
    }
    void RebuildChromeUI()
    {
        if (topBarGo) Destroy(topBarGo);
        foreach (var go in leftRailGos) if (go) Destroy(go);
        leftRailGos.Clear();
        if (stationFilterGo) Destroy(stationFilterGo);
        if (solarTermRailGo) Destroy(solarTermRailGo);
        if (alertRailGo) Destroy(alertRailGo);
        if (speedBoardGo) Destroy(speedBoardGo);
        if (noticeBoardGo) Destroy(noticeBoardGo);
        if (quickUpgradeGo) Destroy(quickUpgradeGo);
        if (dock) Destroy(dock);
        if (undoButton) Destroy(undoButton.gameObject);
        if (lineLoadLegendGo) Destroy(lineLoadLegendGo);
        if (storyObjectiveBannerGo) Destroy(storyObjectiveBannerGo);
        BuildTopBar();
        BuildLeftRail();
        BuildStationFilterButton();
        BuildLeftReminderEntries();
        BuildRightRail();
        BuildBottomNav();
        BuildUndoButton();
        BuildLineLoadLegend();
        BuildStoryObjectiveBanner();
        RefreshFeatureGateVisuals();
        ApplyResponsiveLayout(false);
        RefreshHUD();
        UpdateTimeText();
        RefreshPendingBadge();
        RefreshUndoButton();
        if (pageRoot) pageRoot.transform.SetAsLastSibling();
        if (themePreviewBannerGo) themePreviewBannerGo.transform.SetAsLastSibling();
        if (scrim) scrim.transform.SetAsLastSibling();
        if (panelShadow) panelShadow.transform.SetAsLastSibling();
        if (activePanel) activePanel.transform.SetAsLastSibling();
        if (toastGo) toastGo.transform.SetAsLastSibling();
    }

    static readonly Color[] LineSkinTrimColors = {
        new Color(0.95f,0.97f,0.99f), new Color(1.00f,0.94f,0.96f), new Color(0.99f,0.92f,0.68f),
        new Color(0.84f,0.95f,0.86f), new Color(0.98f,0.88f,0.62f), new Color(0.99f,0.86f,0.52f),
        new Color(0.92f,0.98f,0.98f), new Color(0.99f,0.86f,0.54f), new Color(0.90f,0.93f,0.99f),
        new Color(0.89f,0.96f,0.82f), new Color(0.91f,0.98f,0.95f), new Color(0.96f,0.86f,0.74f),
        new Color(0.86f,0.89f,0.91f), new Color(0.88f,0.77f,0.64f), new Color(0.98f,0.87f,0.58f)
    };
    int GetLineSkin(Line line)
    {
        if (line == null) return -1;
        if (line.skin >= 0 && line.skin < LineSkinNames.Length) return line.skin;
        for (int i = 0; i < LineSkinColors.Length; i++)
            if (Vector4.Distance(line.color, LineSkinColors[i]) < 0.02f) return i;
        return -1;
    }
    static int SkinCategory(int skin)
    {
        switch (skin)
        {
            case 0: case 1: case 6: return 0; // 观光专列
            case 2: case 4: case 8: return 1; // 干线急行
            case 3: case 9: case 10: return 2; // 轻轨节能
            case 5: case 7: case 14: return 3; // 星夜守候
            case 11: case 12: case 13: return 4; // 重载编组
            default: return -1;
        }
    }
    static string SkinCategoryName(int skin)
    {
        switch (SkinCategory(skin))
        {
            case 0: return "观光专列";
            case 1: return "干线急行";
            case 2: return "轻轨节能";
            case 3: return "星夜守候";
            case 4: return "重载编组";
            default: return "标准涂装";
        }
    }
    static string SkinPerkSummary(int skin)
    {
        switch (SkinCategory(skin))
        {
            case 0: return "票款收入 +6%";
            case 1: return "速度 +8% · 轨维 +4%";
            case 2: return "轨道维护 -12%";
            case 3: return "机维 -10% · 夜间再降 10%";
            case 4: return "单向区段运力 +8%";
            default: return "无加成";
        }
    }

    // ---------- company, policies & commercial ----------
    const int BondPrincipal = 2500;
    const int BondTermDays = 10;
    const int BondDailyPayment = 280;
    const int BondEarlyPayPerDay = 265;

    static readonly string[] CompanySealChars = { "轮", "海", "山", "茗", "星", "金", "御", "龙" };
    static readonly string[] CompanySealNames = { "星轨初心", "静海潮声", "南溟峻岳", "星港茗香", "深空星轨", "鎏金枢纽", "太阳系御玺", "鎏金龙印" };
    static readonly string[] CompanyPolicyNames = { "均衡发展", "惠民扩容", "精益运维", "文旅专线" };
    static readonly string[] CompanyPolicyDescs =
    {
        "稳步扩网，全网收支无额外增减",
        "客流 +12% · 开站费 -15% · 票款 -5%",
        "车/轨维护费 -12% · 全队车速 +10%",
        "名胜客流 +35% · 名胜线票款 +10% · 享景点日红利"
    };
    static readonly string[] StationFacilityNames = { "未开发空地", "站厅便利店", "站前商业街", "枢纽都会广场" };
    static readonly int[] StationFacilityCosts = { 3000, 24000, 160000, 0 };
    static readonly int[] StationFacilityRents = { 0, 180, 1200, 6500 };
    static readonly float[] StationFacilityDemandMults = { 1f, 1.25f, 1.75f, 2.60f };
    static readonly float[] StationFacilityLeisureMults = { 1f, 1.65f, 2.80f, 5.00f };
    float FacilityDemandFactor(Station s)
    {
        if (s == null || s.facLevel <= 0) return 1f;
        float mult = StationFacilityDemandMults[Mathf.Clamp(s.facLevel, 0, StationFacilityDemandMults.Length - 1)];
        return HasPartner(1) ? 1f + (mult - 1f) * 1.15f : mult;
    }
    float FacilityLeisureAttrFactor(Station s)
    {
        if (s == null || s.facLevel <= 0) return 1f;
        float mult = StationFacilityLeisureMults[Mathf.Clamp(s.facLevel, 0, StationFacilityLeisureMults.Length - 1)];
        return HasPartner(1) ? 1f + (mult - 1f) * 1.15f : mult;
    }

    static readonly string[] PartnerNames = { "陈志豪", "阿珍姐", "基建强叔", "林博士", "沈小姐", "何先生", "霍老督办" };
    static readonly string[] PartnerTitles = { "海湾精算师", "星港商业总监", "工程老总工", "曲速调度总监", "跨境招商官", "深空资本推手", "特聘传奇顾问" };
    static readonly string[] PartnerPros =
    {
        "车辆与轨道维护费 -12%",
        "站前商业租金 +25%，名胜站客流 +12%",
        "建线造价 -14%，拆线退款率升至 72%",
        "全队车速 +12%，线路票款收益 +8%",
        "全网客流需求 +12%",
        "债券额度与中标赏金 +15%，开站费 -10%",
        "客流+10% · 车速+8% · 建线-10% · 维保-10%"
    };
    static readonly string[] PartnerCons =
    {
        "新开车站费用 +10%",
        "全队车辆维护费 +6%",
        "列车平均速度 -5%",
        "全队车辆维护费 +10%",
        "全网轨道维护费 +8%",
        "常规票款收益 -4%",
        "无负面特质（仅限特聘第 4 席）"
    };

    static readonly string[] EconomyCycleNames = { "常态运营周", "海港商展客流季", "三域文旅嘉年华", "台风梅雨维护周", "基建低息窗口周" };
    static readonly string[] EconomyCycleDescs =
    {
        "全网客流、票价与基建造价处于基准水平",
        "商贸高峰：全网客流 +16%，票款收益 +8%",
        "景区大热：名胜站客流 +24%，名胜线票款 +10%",
        "防汛承压：轨道维护 +15%、客流 -6%，每日补贴 +¥600",
        "窗口期优惠：开站、铺轨与站前商业造价一律 -15%"
    };

    bool IsSealUnlocked(int sealIndex)
    {
        if (sealIndex <= 1) return true;
        if (sealIndex == 2) return DailyConnectedCount() >= 8;
        if (sealIndex == 3) return FoundCount() >= 3;
        if (sealIndex == 4) return techDone.Count >= 3;
        if (sealIndex == 5) return income >= 400f;
        if (sealIndex == 6 || sealIndex == 7)
            return adWatchCounts.TryGetValue("seal_" + sealIndex, out int cnt) && cnt >= 2;
        return false;
    }

    string SealUnlockHint(int sealIndex)
    {
        if (sealIndex == 6 || sealIndex == 7)
        {
            adWatchCounts.TryGetValue("seal_" + sealIndex, out int cnt);
            return "看激励片解锁 (" + Mathf.Min(cnt, 2) + "/2)";
        }
        switch (sealIndex)
        {
            case 2: return "连通 8 座车站解锁";
            case 3: return "打卡 3 处名胜解锁";
            case 4: return "完成 3 项科技解锁";
            case 5: return "预计日利润达 ¥400 解锁";
            default: return "初始解锁";
        }
    }

    string CompanyTitle()
    {
        int connected = DailyConnectedCount();
        int found = FoundCount();
        if (connected >= 128 && found >= 12 && income >= 200000f) return "全星系星轨与深空总署";
        if (connected >= 114 && income >= 100000f) return "东半球轨道枢密院";
        if (connected >= 98 && income >= 50000f) return "欧亚大陆桥控股";
        if (connected >= 80 && income >= 25000f) return "内太阳系星航局";
        if (connected >= 62 && found >= 8 && income >= 12000f) return "东亚路网财团";
        if (connected >= 44 && found >= 6 && income >= 5000f) return "南溟干线联合体";
        if (connected >= 28 && found >= 4 && income >= 2000f) return "行星际轨道总社";
        if (connected >= 16 && income >= 600f) return "地月航运社";
        if (connected >= 8 || lines.Count >= 3) return "城市干线局";
        return "初创轻轨社";
    }
    string NextCompanyRankHint()
    {
        int connected = DailyConnectedCount();
        int found = FoundCount();
        if (connected < 8 && lines.Count < 3) return "下阶「城市干线局」：连通 8 站或开通 3 条线路";
        if (connected < 16 || income < 600f) return "下阶「地月航运社」：连通 16 站且日利润 ¥600（解锁第 3 董事席）";
        if (connected < 28 || found < 4 || income < 2000f) return "下阶「行星际轨道总社」：28 站 · 4 名胜 · 日利润 ¥2,000";
        if (connected < 44 || found < 6 || income < 5000f) return "下阶「南溟干线联合体」：44 站 · 6 名胜 · 日利润 ¥5,000";
        if (connected < 62 || found < 8 || income < 12000f) return "下阶「东亚路网财团」：62 站 · 8 名胜 · 日利润 ¥1.2万";
        if (connected < 80 || income < 25000f) return "下阶「内太阳系星航局」：80 站 · 日利润 ¥2.5万";
        if (connected < 98 || income < 50000f) return "下阶「欧亚大陆桥控股」：98 站 · 日利润 ¥5万";
        if (connected < 114 || income < 100000f) return "下阶「东半球轨道枢密院」：114 站 · 日利润 ¥10万";
        if (connected < 128 || found < 12 || income < 200000f) return "终阶「全星系星轨与深空总署」：128 站 · 12 名胜 · 日利润 ¥20万";
        return "已加冕「全星系星轨与深空总署」最高荣誉";
    }

    bool HasPartner(int id)
    {
        return boardPartners.Contains(id) || (consultantDaysLeft > 0 && consultantId == id);
    }

    int BoardSeatCapacity()
    {
        int connected = DailyConnectedCount();
        int baseSeats = (connected >= 16 && income >= 600f) ? 3 : (connected >= 8 || lines.Count >= 3) ? 2 : 1;
        return Mathf.Min(5, baseSeats + CoTechExtraSeats());
    }

    float PartnerOpenCostFactor()
    {
        float m = 1f;
        if (HasPartner(0)) m *= MitigateCon(0.10f);
        if (HasPartner(5)) m *= 0.90f;
        return m;
    }

    float TrackBuildCostFactor()
    {
        float m = (economyCycle == 4 ? 0.85f : 1f) * CoTechTrackBuildMult();
        if (HasPartner(2)) m *= 0.86f;
        if (HasPartner(6)) m *= 0.90f;
        return Mathf.Max(0.62f, m);
    }

    float DemolishRefundRate()
    {
        return HasPartner(2) ? 0.72f : 0.60f;
    }

    float PartnerDemandFactor(bool isScenery)
    {
        float m = CoTechBaseDemandMult() * (isScenery ? CoTechSceneryDemandMult() : 1f);
        if (HasPartner(1) && isScenery) m *= 1.12f;
        if (HasPartner(4)) m *= 1.12f;
        if (HasPartner(6)) m *= 1.10f;
        return m;
    }

    float PartnerSpeedFactor()
    {
        float m = CoTechSpeedMult();
        if (HasPartner(2)) m *= MitigateCon(-0.05f);
        if (HasPartner(3)) m *= 1.12f;
        if (HasPartner(6)) m *= 1.08f;
        return m;
    }

    float PartnerVehicleMaintFactor()
    {
        float m = (autoDispatchDaysLeft > 0 ? 0.90f : 1f) * CoTechMaintMult();
        if (HasPartner(0)) m *= 0.88f;
        if (HasPartner(1)) m *= MitigateCon(0.06f);
        if (HasPartner(3)) m *= MitigateCon(0.10f);
        if (HasPartner(6)) m *= 0.90f;
        return m;
    }

    float PartnerTrackMaintFactor()
    {
        float m = (autoDispatchDaysLeft > 0 ? 0.90f : 1f) * CoTechMaintMult();
        if (HasPartner(0)) m *= 0.88f;
        if (HasPartner(4)) m *= MitigateCon(0.08f);
        if (HasPartner(6)) m *= 0.90f;
        return m;
    }

    float PartnerRevenueFactor()
    {
        float m = CoTechRevenueMult();
        if (HasPartner(3)) m *= 1.08f;
        if (HasPartner(5)) m *= MitigateCon(-0.04f);
        return m;
    }

    float PartnerContractBonusFactor()
    {
        return (HasPartner(5) ? 1.15f : 1f) * CoTechContractBonusMult();
    }

    int CycleDaysRemaining()
    {
        int rem = 7 - ((day - 1) % 7);
        return Mathf.Clamp(rem, 1, 7);
    }

    float CycleDemandFactor(bool isScenery)
    {
        float f = 1f;
        if (economyCycle == 1) f = 1.16f;
        else if (economyCycle == 2 && isScenery) f = 1.24f;
        else if (economyCycle == 3) f = 0.94f;
        return f * SolarDemandMult(isScenery) * CultureTicketDemandMult();
    }

    float CycleRevenueFactor(bool lineHasScenery)
    {
        float f = 1f;
        if (economyCycle == 1) f = 1.08f;
        else if (economyCycle == 2 && lineHasScenery) f = 1.10f;
        return f * SolarRevenueMult() * PlaqueRevenueMult();
    }

    float CycleTrackMaintFactor()
    {
        return (economyCycle == 3 ? 1.15f : 1f) * SolarMaintMult() * PlaqueMaintMult();
    }

    float CycleDailySubsidy()
    {
        return economyCycle == 3 ? 600f : 0f;
    }

    int CompanyCreditScore()
    {
        int s = Mathf.Min(35, DailyConnectedCount());
        s += income >= 800f ? 25 : income >= 300f ? 15 : income >= 100f ? 8 : 0;
        s += Mathf.Min(20, DevelopedCommercialCount() * 5);
        s += Mathf.Min(15, techDone.Count * 3);
        s += Mathf.Min(15, bondsRepaid * 8);
        if (loanOutstanding) s -= 15;
        return Mathf.Clamp(s, 0, 100);
    }

    int CompanyCreditTier()
    {
        int s = CompanyCreditScore();
        if (s >= 82) return 4; // AAA
        if (s >= 64) return 3; // AA
        if (s >= 45) return 2; // A
        if (s >= 26) return 1; // B
        return 0;              // C
    }

    static readonly string[] CreditTierLabels = { "C 级", "B 级", "A 级", "AA 级", "AAA 级" };
    static readonly int[] CreditBondAmounts = { 1800, 5000, 15000, 40000, 100000 };
    static readonly int[] CreditBondDailyDues = { 205, 560, 1650, 4320, 10500 };

    int CurrentCreditBondAmount(int tier)
    {
        int amt = CreditBondAmounts[Mathf.Clamp(tier, 0, CreditBondAmounts.Length - 1)];
        if (HasPartner(5)) amt = Mathf.RoundToInt(amt * 1.15f);
        amt = Mathf.RoundToInt(amt * CoTechBondCapMult());
        return amt;
    }

    int CurrentCreditBondDaily(int tier)
    {
        int due = CreditBondDailyDues[Mathf.Clamp(tier, 0, CreditBondDailyDues.Length - 1)];
        if (HasPartner(5)) due = Mathf.RoundToInt(due * 1.12f);
        due = Mathf.RoundToInt(due * CoTechBondDueMult());
        return due;
    }

    int EffectiveBondDailyPayment()
    {
        if (bondDaysLeft <= 0) return 0;
        int raw = bondDailyDue > 0 ? bondDailyDue : BondDailyPayment;
        return bondSubsidized ? Mathf.RoundToInt(raw * 0.85f) : raw;
    }

    void EnsureCompanyInitialized()
    {
        economyCycle = Mathf.Clamp(economyCycle, 0, EconomyCycleNames.Length - 1);
        nextEconomyCycle = Mathf.Clamp(nextEconomyCycle, 0, EconomyCycleNames.Length - 1);
        if (nextEconomyCycle == economyCycle) nextEconomyCycle = (economyCycle + 1) % EconomyCycleNames.Length;
        for (int i = activeContracts.Count - 1; i >= 0; i--)
        {
            var c = activeContracts[i];
            if (c == null || c.claimed || (day > c.deadlineDay && !ContractIsReady(c))) activeContracts.RemoveAt(i);
        }
        int cap = CoTechContractCap();
        while (activeContracts.Count < cap) activeContracts.Add(RollContract(activeContracts.Count));
    }

    void AdvanceEconomyCycle()
    {
        economyCycle = Mathf.Clamp(nextEconomyCycle, 0, EconomyCycleNames.Length - 1);
        int pick = (economyCycle + 1 + ((day + DailyConnectedCount()) % (EconomyCycleNames.Length - 1))) % EconomyCycleNames.Length;
        nextEconomyCycle = pick == economyCycle ? (economyCycle + 1) % EconomyCycleNames.Length : pick;
        EnsureCompanyInitialized();
        Toast("《深空星风周报》本周进入：" + EconomyCycleNames[economyCycle]);
    }

    MunicipalContract RollContract(int slotIdx)
    {
        int type = (day + slotIdx + nextContractId) % 4;
        var c = new MunicipalContract { id = nextContractId++, type = type, deadlineDay = day + 6 };
        float mult = PartnerContractBonusFactor();
        int scale = Mathf.Clamp(Mathf.RoundToInt(Mathf.Max(0f, income) * 0.35f), 0, 12000);
        if (type == 0)
        {
            c.startValue = DailyConnectedCount();
            c.targetValue = 3 + (DailyConnectedCount() >= 18 ? 1 : 0);
            c.rewardCoins = Mathf.RoundToInt((900 + scale) * mult);
            c.rewardRp = 16; c.rewardDiamonds = 3;
        }
        else if (type == 1)
        {
            c.startValue = DevelopedCommercialCount();
            c.targetValue = c.startValue + 2;
            c.rewardCoins = Mathf.RoundToInt((1100 + scale) * mult);
            c.rewardRp = 14; c.rewardDiamonds = 5;
        }
        else if (type == 2)
        {
            int curProfit = Mathf.Max(0, Mathf.RoundToInt(income));
            c.startValue = 0;
            c.targetValue = Mathf.Max(400, ((curProfit + Mathf.Max(300, Mathf.RoundToInt(curProfit * 0.25f))) / 100) * 100);
            c.rewardCoins = Mathf.RoundToInt((1300 + scale * 1.2f) * mult);
            c.rewardRp = 22; c.rewardDiamonds = 4;
        }
        else
        {
            c.startValue = FoundCount();
            c.targetValue = Mathf.Min(sceneries.Count, c.startValue + 1);
            c.rewardCoins = Mathf.RoundToInt((850 + scale * 0.9f) * mult);
            c.rewardRp = 15; c.rewardDiamonds = 6;
        }
        return c;
    }

    int ContractCurrentValue(MunicipalContract c)
    {
        if (c == null) return 0;
        switch (c.type)
        {
            case 0: return Mathf.Max(0, DailyConnectedCount() - c.startValue);
            case 1: return DevelopedCommercialCount();
            case 2: return Mathf.Max(0, Mathf.RoundToInt(income));
            default: return FoundCount();
        }
    }

    bool ContractIsReady(MunicipalContract c)
    {
        return c != null && !c.claimed && ContractCurrentValue(c) >= c.targetValue;
    }

    string ContractTitle(MunicipalContract c)
    {
        if (c == null) return "";
        switch (c.type)
        {
            case 0: return "市政连通扩网工程";
            case 1: return "站前商业繁华开发";
            case 2: return "海湾干线盈利冲刺";
            default: return "文旅名胜接驳专线";
        }
    }

    string ContractDesc(MunicipalContract c)
    {
        if (c == null) return "";
        int cur = ContractCurrentValue(c);
        switch (c.type)
        {
            case 0: return "较接标新增连通 " + c.targetValue + " 座车站（" + Mathf.Min(cur, c.targetValue) + "/" + c.targetValue + "）";
            case 1: return "全网累计建成 " + c.targetValue + " 处站前商业（" + Mathf.Min(cur, c.targetValue) + "/" + c.targetValue + "）";
            case 2: return "全网预计日利润达 ¥" + c.targetValue + "（当前 ¥" + cur + "）";
            default: return "名胜打卡累计达 " + c.targetValue + " 处（" + Mathf.Min(cur, c.targetValue) + "/" + c.targetValue + "）";
        }
    }

    int FacilityUpgradeCost(int curFac)
    {
        int raw = StationFacilityCosts[Mathf.Clamp(curFac, 0, StationFacilityCosts.Length - 1)];
        float mult = (economyCycle == 4 ? 0.85f : 1f) * CoTechOpenCostMult();
        return Mathf.RoundToInt(raw * mult / 10f) * 10;
    }

    void ComputeAnnualAward(out string medalName, out string reason, out int grantCoins, out int grantDiamonds)
    {
        int dev = DevelopedCommercialCount();
        int conn = DailyConnectedCount();
        int found = FoundCount();
        int profitBonus = Mathf.Clamp(Mathf.RoundToInt(Mathf.Max(0f, income) * 0.4f), 0, 8000);
        if (bondDaysLeft == 0 && !loanOutstanding && income >= 300f)
        {
            medalName = "零负债稳健总社";
            reason = "零坏账高现金流，财务结构稳健";
            grantCoins = 1600 + conn * 50 + profitBonus; grantDiamonds = 6;
        }
        else if (dev >= 4)
        {
            medalName = "星域地产金印";
            reason = "站前商业已达 " + dev + " 处，客流与物业双轮驱动";
            grantCoins = 1500 + dev * 350 + profitBonus; grantDiamonds = 8;
        }
        else if (found >= 5)
        {
            medalName = "三域文旅金招牌";
            reason = "已串联打卡 " + found + " 处地标风景";
            grantCoins = 1400 + found * 280 + profitBonus; grantDiamonds = 10;
        }
        else
        {
            medalName = "深空基建先锋局";
            reason = "全网持续扩张，已连通 " + conn + " 座车站";
            grantCoins = 1200 + conn * 55 + profitBonus; grantDiamonds = 6;
        }
        grantCoins = Mathf.RoundToInt(grantCoins * CoTechAwardMult());
    }

    bool StationHasScenery(int index)
    {
        if (index < 0 || index >= stations.Count) return false;
        var s = stations[index];
        foreach (var sc in sceneries)
            if (sc.triggerStation == s.name && s.region == sc.region) return true;
        return false;
    }

    bool LineHasSceneryStation(Line line)
    {
        if (line == null || line.st == null) return false;
        foreach (int idx in line.st)
            if (StationHasScenery(idx)) return true;
        return false;
    }

    float StationCommercialTotalDaily()
    {
        float sum = 0f;
        float mult = TechMult() * (HasPartner(1) ? 1.25f : 1f) * CoTechCommercialMult() * SolarCommercialMult() * PlaqueCommercialMult();
        for (int i = 0; i < stations.Count; i++)
        {
            var s = stations[i];
            if (!s.built || s.closed || s.facLevel <= 0) continue;
            float baseRent = StationFacilityRents[Mathf.Clamp(s.facLevel, 0, StationFacilityRents.Length - 1)];
            sum += baseRent * mult * StationTalentCommercialMult(i);
        }
        return sum;
    }

    int DevelopedCommercialCount()
    {
        int count = 0;
        foreach (var s in stations)
            if (s.facLevel > 0) count++;
        return count;
    }

    float TourismDividendDaily()
    {
        if (companyPolicy != 3) return 0f;
        return FoundCount() * 120f;
    }

    int AdTourismSubsidyAmount()
    {
        return Mathf.Min(5000, 500 + DailyConnectedCount() * 60);
    }

    int BondEarlyRedeemCost()
    {
        if (bondDaysLeft <= 0) return 0;
        if (bondPrincipal > 0)
        {
            float principalPerDay = bondPrincipal / (float)BondTermDays;
            float duePerDay = EffectiveBondDailyPayment();
            float interestHalf = Mathf.Max(0f, duePerDay - principalPerDay) * 0.5f;
            return Mathf.RoundToInt(bondDaysLeft * (principalPerDay + interestHalf));
        }
        return bondDaysLeft * BondEarlyPayPerDay;
    }
}

public class StationPress : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public int index; public DemoBootstrap boot;
    public void OnPointerDown(PointerEventData e) { boot.OnStationDown(index); }
    public void OnPointerUp(PointerEventData e) { boot.OnStationUp(index); }
}

/// <summary>
/// 允许玩家在屏幕安全区内自由拖动气泡弹窗，避免遮挡地图与 OD 弧线。
/// </summary>
public class DraggablePanel : MonoBehaviour, IBeginDragHandler, IDragHandler
{
    public RectTransform target;
    public RectTransform bounds;
    public Vector4 padding; // left, right, top, bottom
    Vector2 dragOffset;

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!target || !bounds) return;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(bounds, eventData.position, eventData.pressEventCamera, out var localPt))
            dragOffset = target.anchoredPosition - localPt;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!target || !bounds) return;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(bounds, eventData.position, eventData.pressEventCamera, out var localPt))
            return;
        Vector2 next = localPt + dragOffset;
        float bw = bounds.rect.width;
        float bh = bounds.rect.height;
        float halfW = target.rect.width * target.localScale.x * 0.5f;
        float halfH = target.rect.height * target.localScale.y * 0.5f;
        float minX = padding.x + halfW;
        float maxX = Mathf.Max(minX, bw - padding.y - halfW);
        float minY = padding.w + halfH;
        float maxY = Mathf.Max(minY, bh - padding.z - halfH);
        next.x = Mathf.Clamp(next.x, minX, maxX);
        next.y = Mathf.Clamp(next.y, minY, maxY);
        target.anchoredPosition = next;
    }
}
