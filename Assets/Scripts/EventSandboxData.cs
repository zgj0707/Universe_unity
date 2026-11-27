using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public partial class DemoBootstrap
{
    public const string EventIdAshDome = "ashdome";
    public const string EventIdNeonNight = "neon";
    const string SandboxSavePrefKey = "starline_event_sandbox_v1";
    const string SandboxSaveFileName = "event_sandbox_save.json";
    const int CurrentEventSeasonId = 1;

    public sealed class EventVehicleDef
    {
        public string key, name, desc;
        public int costTokens, skinFallbackIndex, extraImmuneDays, maxOrdersPerCar;
        public float capMult, speedMult, deliveredBonus;
        public bool highFareOnly, maxTipAlways;
    }

    public sealed class EventSpec
    {
        public string id, displayTitle, subtitle, mapTexResource, cardBgResource, roadGraphResource, tokenName;
        public long startUtcTicks, endUtcTicks;
        public Color accentColor;
        public int lineLimit;
        public EventVehicleDef[] vehicles;
    }

    [Serializable]
    public sealed class SavedEventLine
    {
        public int id;
        public string name;
        public Color color;
        public List<int> stations = new List<int>();
        public int cars = 1;
        public int[] dispatch = { 1, 1, 1, 1 };
        public string vehicleKey = "";
        public int skinIndex = -1;
    }

    [Serializable]
    public sealed class SavedEventSandbox
    {
        public string id;
        public int seasonId = CurrentEventSeasonId;
        public int nextLineId = 1;
        public int eventDay = 1;
        public float eventClock = 90f;
        public int tokens;
        public long progress;
        public long totalDelivered;
        public int storyStage;
        public int cycleCount = 1;
        public int activeBranch = -1;
        public int activeDirective; // 0: 稳健均衡, 1: 全力加车, 2: 远郊补贴
        public List<string> treeNodes = new List<string>();
        public List<string> ownedVehicles = new List<string>();
        public int ashRosterVersion;
        public List<int> ownedSkins = new List<int>();
        public List<string> achievements = new List<string>();
        public List<string> claimed = new List<string>();
        public List<SavedEventLine> lines = new List<SavedEventLine>();
        public List<int> stationLevels = new List<int>();
        public List<int> stationPop = new List<int>();
        public int missedPop;
        public float ashDelayHours, ashTokenCredit;
        public List<int> ashCountedFallenStations = new List<int>();
        public bool ashFallenMaskInitialized;
        public List<int> ashClearedStations = new List<int>();
        public double ashCycleRewards;
        public long extraA;
        public long extraB;
    }

    [Serializable]
    sealed class SavedEventSandboxStorage
    {
        public int version = 1;
        public List<SavedEventSandbox> entries = new List<SavedEventSandbox>();
    }

    static readonly DateTime EventSeasonStartUtc = new DateTime(2026, 11, 20, 0, 0, 0, DateTimeKind.Utc);
    static readonly DateTime EventSeasonEndUtc = new DateTime(2026, 12, 31, 23, 59, 59, DateTimeKind.Utc);

    static readonly EventSpec[] EventSpecs =
    {
        new EventSpec
        {
            id = EventIdAshDome,
            displayTitle = "赤尘撤离志",
            subtitle = "七天里，把老城的街坊送往避风港",
            mapTexResource = "MapArt/event_ashdome",
            cardBgResource = "PageArt/event_ashdome_card",
            roadGraphResource = "MapData/event_ashdome_roads",
            tokenName = "火种券",
            startUtcTicks = EventSeasonStartUtc.Ticks,
            endUtcTicks = EventSeasonEndUtc.Ticks,
            accentColor = new Color(0.76f, 0.32f, 0.22f, 1f),
            lineLimit = 12,
            vehicles = new[]
            {
                new EventVehicleDef { key = "veh_ash_scavenger", name = "「老伙计」改装篷车", desc = "车队的老伙伴，穿过老城街巷总是稳稳当当", costTokens = 5, skinFallbackIndex = 12, capMult = 1.0f, speedMult = 1.0f },
                new EventVehicleDef { key = "veh_ash_iron", name = "「铁壁」防风护航车", desc = "装上双层防风板，运力 +25%，还能多撑一阵沙尘", costTokens = 8, skinFallbackIndex = 13, capMult = 1.25f, speedMult = 1.05f, extraImmuneDays = 1 },
                new EventVehicleDef { key = "veh_ash_dove", name = "「白鸽」医疗专列", desc = "护工和急救舱随车出发，转运人数额外 +20%", costTokens = 9, skinFallbackIndex = 10, capMult = 1.15f, speedMult = 1.10f, deliveredBonus = 0.20f },
                new EventVehicleDef { key = "veh_ash_ark", name = "「晨曦号」避风旗舰", desc = "密封的双层车厢能多载些人，运力 +60%，跑得也快", costTokens = 12, skinFallbackIndex = 2, capMult = 1.6f, speedMult = 1.25f, extraImmuneDays = 2 }
            }
        },
        new EventSpec
        {
            id = EventIdNeonNight,
            displayTitle = "长夜车行",
            subtitle = "沿着澄海的灯火，送夜归人回家",
            mapTexResource = "MapArt/event_neon",
            cardBgResource = "PageArt/event_neon_card",
            roadGraphResource = "MapData/event_neon_roads",
            tokenName = "里程券",
            startUtcTicks = EventSeasonStartUtc.Ticks,
            endUtcTicks = EventSeasonEndUtc.Ticks,
            accentColor = new Color(0.26f, 0.44f, 0.78f, 1f),
            lineLimit = 11,
            vehicles = new[]
            {
                new EventVehicleDef { key = "veh_neon_firefly", name = "「萤火」黄顶巡游车", desc = "澄海街头常见的黄顶车，陪你跑过一个个夜晚", costTokens = 0, skinFallbackIndex = 7, capMult = 1.0f, speedMult = 1.0f, maxOrdersPerCar = 1 },
                new EventVehicleDef { key = "veh_neon_owl", name = "「夜燕」双座小巴", desc = "车厢宽敞，可同时接两单，常往返大桥和码头", costTokens = 14, skinFallbackIndex = 14, capMult = 1.30f, speedMult = 1.05f, maxOrdersPerCar = 2 },
                new EventVehicleDef { key = "veh_neon_amethyst", name = "「紫星」礼宾专车", desc = "安静舒适的礼宾车，适合接高价长途订单", costTokens = 16, skinFallbackIndex = 5, capMult = 1.10f, speedMult = 1.12f, maxOrdersPerCar = 1, highFareOnly = true },
                new EventVehicleDef { key = "veh_neon_zero", name = "「零号老的士」", desc = "郑师傅留下的黄铜编号老车，跑得快，每单都能拿满小费", costTokens = 26, skinFallbackIndex = 8, capMult = 1.25f, speedMult = 1.30f, maxOrdersPerCar = 1, maxTipAlways = true }
            }
        }
    };

    public readonly struct SandboxStationDef
    {
        public readonly string name;
        public readonly float u, v;
        public readonly int roadNode;
        public readonly int tierOrKind;
        public readonly bool isSpecialA; // Ash: 避风港 | Neon: 商业/换乘热点
        public readonly bool isSpecialB; // Ash: 地热核心禁止区

        public SandboxStationDef(string name, float u, float v, int roadNode, int tierOrKind, bool isSpecialA = false, bool isSpecialB = false)
        {
            this.name = name; this.u = u; this.v = v; this.roadNode = roadNode;
            this.tierOrKind = tierOrKind; this.isSpecialA = isSpecialA; this.isSpecialB = isSpecialB;
        }
    }

    static readonly SandboxStationDef[] AshSandboxStations =
    {
        new SandboxStationDef("地热沉降井", 0.47243f, 0.46818f, 442, 0, false, true),
        new SandboxStationDef("钟楼广场", 0.42057f, 0.58122f, 848, 1, false, false),
        new SandboxStationDef("南岸老邮局", 0.58227f, 0.45197f, 304, 1, false, false),
        new SandboxStationDef("工坊渡口", 0.45607f, 0.52828f, 545, 2, false, false),
        new SandboxStationDef("红砖学堂", 0.66446f, 0.62842f, 218, 2, false, false),
        new SandboxStationDef("温室长廊", 0.46267f, 0.29421f, 612, 2, false, false),
        new SandboxStationDef("东堤高架", 0.68625f, 0.49790f, 202, 3, false, false),
        new SandboxStationDef("北门枢纽", 0.51747f, 0.69195f, 581, 3, false, false),
        new SandboxStationDef("蓄水石塔", 0.28154f, 0.60795f, 1052, 3, false, false),
        new SandboxStationDef("白鸽医馆", 0.30616f, 0.33320f, 891, 4, false, false),
        new SandboxStationDef("风车观景台", 0.71326f, 0.69590f, 329, 4, false, false),
        new SandboxStationDef("旧档案局", 0.70576f, 0.28760f, 457, 4, false, false),
        new SandboxStationDef("西关货栈", 0.16238f, 0.47538f, 1350, 5, false, false),
        new SandboxStationDef("北桥驿站", 0.50990f, 0.78418f, 437, 5, false, false),
        new SandboxStationDef("南麓水闸", 0.48707f, 0.15550f, 716, 5, false, false),
        new SandboxStationDef("西河农庄", 0.09549f, 0.63920f, 1129, 5, false, false),
        new SandboxStationDef("西北林场", 0.09752f, 0.85727f, 843, 5, false, false),
        new SandboxStationDef("北郊煤气厂", 0.27954f, 0.88427f, 845, 5, false, false),
        new SandboxStationDef("北湾船坞", 0.45171f, 0.90235f, 590, 5, false, false),
        new SandboxStationDef("东北铁路库", 0.69592f, 0.90305f, 149, 5, false, false),
        new SandboxStationDef("东湾新村", 0.87462f, 0.86721f, 65, 5, false, false),
        new SandboxStationDef("东郊果园", 0.92087f, 0.66942f, 361, 5, false, false),
        new SandboxStationDef("东门砖窑", 0.87418f, 0.41917f, 286, 5, false, false),
        new SandboxStationDef("东南盐田", 0.90250f, 0.28667f, 568, 5, false, false),
        new SandboxStationDef("南郊牧场", 0.76094f, 0.11378f, 696, 5, false, false),
        new SandboxStationDef("南湾灯塔", 0.43842f, 0.09814f, 932, 5, false, false),
        new SandboxStationDef("西南苗圃", 0.31374f, 0.07905f, 1024, 5, false, false),
        new SandboxStationDef("西郊磨坊", 0.09769f, 0.13835f, 1260, 5, false, false),
        new SandboxStationDef("西堤老街", 0.07170f, 0.34368f, 1246, 5, false, false),
        new SandboxStationDef("运河中桥", 0.35322f, 0.59455f, 950, 2, false, false),
        new SandboxStationDef("老船长酒馆", 0.51053f, 0.62903f, 397, 2, false, false),
        new SandboxStationDef("石拱桥北", 0.42916f, 0.77429f, 535, 3, false, false),
        new SandboxStationDef("老织布厂", 0.61582f, 0.30021f, 516, 3, false, false),
        new SandboxStationDef("南河菜市", 0.33743f, 0.23323f, 980, 4, false, false),
        new SandboxStationDef("东湖公园", 0.81181f, 0.57481f, 141, 4, false, false),
        new SandboxStationDef("西堤避风港", 0.15345f, 0.81810f, 944, 6, true, false),
        new SandboxStationDef("东湾避风港", 0.79759f, 0.73957f, 120, 6, true, false),
        new SandboxStationDef("南丘避风港", 0.83790f, 0.14439f, 607, 6, true, false),
        new SandboxStationDef("西南修车厂", 0.18782f, 0.26167f, 1199, 4, false, false),
        new SandboxStationDef("南岸苇塘", 0.63646f, 0.16237f, 664, 5, false, false),
        new SandboxStationDef("东郊水塔", 0.93791f, 0.51834f, 242, 5, false, false),
        new SandboxStationDef("东南角岗哨", 0.92221f, 0.06322f, 713, 5, false, false),
        new SandboxStationDef("西南谷仓", 0.20149f, 0.09403f, 1135, 5, false, false),
        new SandboxStationDef("西堤渡口", 0.05264f, 0.50050f, 1344, 5, false, false),
        new SandboxStationDef("东湾盐仓", 0.94940f, 0.16793f, 704, 5, false, false),
        new SandboxStationDef("东郊晒场", 0.79653f, 0.34472f, 351, 4, false, false)
    };

    static readonly SandboxStationDef[] NeonSandboxStations =
    {
        new SandboxStationDef("澄海总站", 0.50141f, 0.50061f, 445, 0, true),
        new SandboxStationDef("月光广场", 0.42245f, 0.57796f, 327, 2, false),
        new SandboxStationDef("星象观景台", 0.57774f, 0.64149f, 750, 2, false),
        new SandboxStationDef("旧轨老站", 0.34035f, 0.39471f, 285, 3, true),
        new SandboxStationDef("东巷茶室", 0.59446f, 0.46338f, 765, 0, false),
        new SandboxStationDef("制图工坊", 0.41028f, 0.66830f, 749, 1, false),
        new SandboxStationDef("河湾酒肆", 0.58294f, 0.52485f, 605, 2, false),
        new SandboxStationDef("夜灯长街", 0.48713f, 0.38182f, 295, 1, false),
        new SandboxStationDef("北港码头", 0.29571f, 0.72669f, 651, 3, true),
        new SandboxStationDef("南苑里", 0.29153f, 0.34827f, 506, 0, false),
        new SandboxStationDef("西桥面馆", 0.19098f, 0.53919f, 41, 2, false),
        new SandboxStationDef("银顶钟楼", 0.68054f, 0.67790f, 949, 1, false),
        new SandboxStationDef("商行大厦", 0.72158f, 0.52526f, 1219, 1, false),
        new SandboxStationDef("湾口夜市", 0.58876f, 0.28535f, 403, 2, false),
        new SandboxStationDef("中转站", 0.38363f, 0.52591f, 79, 3, true),
        new SandboxStationDef("郑师傅巷口", 0.19700f, 0.19049f, 1481, 0, false),
        new SandboxStationDef("北郊新村", 0.47054f, 0.82211f, 1575, 0, false),
        new SandboxStationDef("东门驿", 0.79213f, 0.37293f, 1348, 3, true),
        new SandboxStationDef("露天剧场", 0.65650f, 0.77853f, 1490, 2, false),
        new SandboxStationDef("西岸货港", 0.15501f, 0.66605f, 307, 3, true),
        new SandboxStationDef("静谧花园", 0.78060f, 0.23318f, 1047, 0, false),
        new SandboxStationDef("西郊旧窑", 0.07142f, 0.19299f, 1482, 0, false),
        new SandboxStationDef("西山公园", 0.08592f, 0.31252f, 897, 2, false),
        new SandboxStationDef("西岭道口", 0.08310f, 0.56223f, 196, 3, true),
        new SandboxStationDef("西北果园", 0.07847f, 0.74302f, 725, 0, false),
        new SandboxStationDef("北郊车库", 0.19391f, 0.91991f, 1477, 3, true),
        new SandboxStationDef("北环高架", 0.36116f, 0.91937f, 2013, 1, false),
        new SandboxStationDef("北岭天文台", 0.58493f, 0.91669f, 1503, 2, false),
        new SandboxStationDef("东北湿地", 0.78164f, 0.88916f, 1771, 0, false),
        new SandboxStationDef("东郊书院", 0.90324f, 0.79342f, 1997, 2, false),
        new SandboxStationDef("东岸船厂", 0.91013f, 0.57846f, 1907, 3, true),
        new SandboxStationDef("东岭茶园", 0.89213f, 0.43993f, 2125, 0, false),
        new SandboxStationDef("东南货场", 0.89888f, 0.21944f, 1519, 3, true),
        new SandboxStationDef("南湾渡口", 0.87576f, 0.10807f, 1857, 2, false),
        new SandboxStationDef("南郊农市", 0.66110f, 0.09582f, 1252, 0, false),
        new SandboxStationDef("南桥路口", 0.43250f, 0.06309f, 2033, 3, true),
        new SandboxStationDef("西南木屋区", 0.31966f, 0.13526f, 1718, 0, false),
        new SandboxStationDef("河心小岛", 0.52366f, 0.24402f, 921, 2, false),
        new SandboxStationDef("钟表巷", 0.36311f, 0.28160f, 830, 1, false),
        new SandboxStationDef("白杨林荫道", 0.21176f, 0.79815f, 855, 0, false),
        new SandboxStationDef("东湾小学", 0.76015f, 0.66825f, 1286, 0, false),
        new SandboxStationDef("南岸书局", 0.70681f, 0.38509f, 1090, 1, false),
        new SandboxStationDef("西坡老茶棚", 0.14497f, 0.42716f, 201, 1, false),
        new SandboxStationDef("东郊信号塔", 0.93682f, 0.32996f, 1700, 0, false),
        new SandboxStationDef("西岸旧书铺", 0.30413f, 0.60954f, 325, 1, false),
        new SandboxStationDef("南湾轮渡口", 0.53349f, 0.10989f, 1568, 3, true),
        new SandboxStationDef("东郊温室", 0.78807f, 0.77593f, 1654, 2, false),
        new SandboxStationDef("东湾灯塔", 0.87207f, 0.68686f, 1653, 0, false),
        new SandboxStationDef("南岸夜排档", 0.66976f, 0.21146f, 886, 2, false),
        new SandboxStationDef("西南渔村", 0.42336f, 0.17234f, 1586, 0, false)
    };

    // ---------- 真实街道拓扑图与 Dijkstra 沿街寻路 ----------
    sealed class EventRoadGraph
    {
        public Vector2[] nodes;
        public List<(int to, float w, List<Vector2> pts)>[] adj;
    }

    readonly Dictionary<string, EventRoadGraph> loadedRoadGraphs = new Dictionary<string, EventRoadGraph>();
    readonly List<SavedEventSandbox> eventSandboxes = new List<SavedEventSandbox>();
    bool eventSandboxesLoaded;
    GameObject eventBadgeGo;

    static string SandboxSaveFilePath => Path.Combine(Application.persistentDataPath, SandboxSaveFileName);

    void LoadSandboxStorageIfNeeded()
    {
        if (eventSandboxesLoaded) return;
        eventSandboxesLoaded = true;
        eventSandboxes.Clear();
        string raw = "";
        try
        {
            if (File.Exists(SandboxSaveFilePath))
                raw = File.ReadAllText(SandboxSaveFilePath, System.Text.Encoding.UTF8);
        }
        catch { }
        LoadSandboxEntries(raw);
        if (eventSandboxes.Count == 0)
            LoadSandboxEntries(PlayerPrefs.GetString(SandboxSavePrefKey, ""));
    }

    void LoadSandboxEntries(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return;
        try
        {
            var storage = JsonUtility.FromJson<SavedEventSandboxStorage>(raw);
            if (storage == null || storage.entries == null) return;
            foreach (var entry in storage.entries)
            {
                if (entry == null || string.IsNullOrEmpty(entry.id)) continue;
                entry.nextLineId = Mathf.Max(1, entry.nextLineId);
                if (entry.lines != null)
                    foreach (var line in entry.lines)
                        if (line != null) { line.dispatch = NormalizeSandboxDispatch(line.dispatch, line.cars); entry.nextLineId = Mathf.Max(entry.nextLineId, line.id + 1); }
                eventSandboxes.Add(entry);
            }
        }
        catch { }
    }

    void SaveSandboxStorage()
    {
        LoadSandboxStorageIfNeeded();
        var storage = new SavedEventSandboxStorage { entries = new List<SavedEventSandbox>(eventSandboxes) };
        string raw = JsonUtility.ToJson(storage, false);
        try { WriteSaveJsonAtomically(SandboxSaveFilePath, raw); }
        catch { }
        PlayerPrefs.SetString(SandboxSavePrefKey, raw);
        PlayerPrefs.Save();
    }

    static EventSpec GetEventSpec(string eventId)
    {
        for (int i = 0; i < EventSpecs.Length; i++)
            if (EventSpecs[i].id == eventId) return EventSpecs[i];
        return null;
    }

    static EventVehicleDef GetSandboxVehicleDef(string eventId, string vehicleKey)
    {
        var spec = GetEventSpec(eventId);
        if (spec == null || spec.vehicles == null || spec.vehicles.Length == 0) return null;
        for (int i = 0; i < spec.vehicles.Length; i++)
            if (spec.vehicles[i].key == vehicleKey) return spec.vehicles[i];
        return spec.vehicles[0];
    }

    static SandboxStationDef[] GetSandboxStationDefs(string eventId)
        => eventId == EventIdAshDome ? AshSandboxStations : NeonSandboxStations;

    SavedEventSandbox GetOrCreateEventSandbox(string eventId)
    {
        LoadSandboxStorageIfNeeded();
        for (int i = 0; i < eventSandboxes.Count; i++)
        {
            var e = eventSandboxes[i];
            if (e != null && e.id == eventId)
            {
                if (e.totalDelivered < e.progress) e.totalDelivered = e.progress;
                if (e.ownedVehicles == null) e.ownedVehicles = new List<string>();
                if (e.ownedVehicles.Count == 0) { e.ownedVehicles.Add(eventId == EventIdAshDome ? "veh_ash_scavenger" : "veh_neon_firefly"); if (eventId == EventIdAshDome) e.ownedVehicles.Add("veh_ash_scavenger"); }
                if (eventId == EventIdAshDome && e.ashRosterVersion == 0) {
                    int starters = 0; foreach (var key in e.ownedVehicles) if (key == "veh_ash_scavenger") starters++;
                    while (starters++ < 2) e.ownedVehicles.Add("veh_ash_scavenger");
                    e.ashRosterVersion = 1;
                }
                if (e.ownedSkins == null) e.ownedSkins = new List<int>();
                return e;
            }
        }

        var fresh = new SavedEventSandbox
        {
            id = eventId,
            seasonId = CurrentEventSeasonId,
            eventDay = 1,
            eventClock = eventId == EventIdNeonNight ? (GameDaySeconds * 0.30f) : 90f,
            cycleCount = 1,
            activeBranch = -1,
            tokens = eventId == EventIdAshDome ? 0 : 8,
            extraA = eventId == EventIdNeonNight ? 450 : 1
        };
        if (eventId == EventIdAshDome) { fresh.ownedVehicles.Add("veh_ash_scavenger"); fresh.ownedVehicles.Add("veh_ash_scavenger"); fresh.ashRosterVersion = 1; EnsureAshStationPopInitialized(fresh); }
        else if (eventId == EventIdNeonNight) fresh.ownedVehicles.Add("veh_neon_firefly");
        eventSandboxes.Add(fresh);
        return fresh;
    }

    bool CanEnterEvents() => lines.Count >= 1;

    bool IsEventActive(string eventId)
    {
        var spec = GetEventSpec(eventId);
        if (spec == null) return false;
        long now = DateTime.UtcNow.Ticks;
        return now >= spec.startUtcTicks && now <= spec.endUtcTicks;
    }

    int EventRemainingDays(string eventId)
    {
        var spec = GetEventSpec(eventId);
        if (spec == null) return 0;
        long left = spec.endUtcTicks - DateTime.UtcNow.Ticks;
        return Mathf.Max(0, (int)Math.Ceiling(TimeSpan.FromTicks(left).TotalDays));
    }

    Vector2 SandboxUvToCanvas(float u, float v) => new Vector2(u * W, v * H);

    EventRoadGraph GetOrLoadSandboxRoadGraph(string eventId)
    {
        if (loadedRoadGraphs.TryGetValue(eventId, out var g)) return g;
        var spec = GetEventSpec(eventId);
        if (spec == null) return null;
        var ta = Resources.Load<TextAsset>(spec.roadGraphResource);
        if (!ta) return null;
        try
        {
            g = BuildGraphFromRawJson(ta.text);
            loadedRoadGraphs[eventId] = g;
            return g;
        }
        catch { return null; }
    }

    static EventRoadGraph BuildGraphFromRawJson(string raw)
    {
        var g = new EventRoadGraph();
        int nodesStart = raw.IndexOf("\"nodes\"", StringComparison.Ordinal);
        int edgesStart = raw.IndexOf("\"edges\"", StringComparison.Ordinal);
        string nodesSeg = raw.Substring(nodesStart, edgesStart - nodesStart);
        var nodeList = new List<Vector2>();
        int pos = 0;
        while ((pos = nodesSeg.IndexOf("\"x\":", pos, StringComparison.Ordinal)) >= 0)
        {
            pos += 4;
            int comma = nodesSeg.IndexOf(',', pos);
            float x = float.Parse(nodesSeg.Substring(pos, comma - pos), System.Globalization.CultureInfo.InvariantCulture);
            int yPos = nodesSeg.IndexOf("\"y\":", comma, StringComparison.Ordinal) + 4;
            int endB = nodesSeg.IndexOf('}', yPos);
            float y = float.Parse(nodesSeg.Substring(yPos, endB - yPos), System.Globalization.CultureInfo.InvariantCulture);
            nodeList.Add(new Vector2(x, y));
            pos = endB;
        }
        g.nodes = nodeList.ToArray();
        g.adj = new List<(int, float, List<Vector2>)>[g.nodes.Length];
        for (int i = 0; i < g.adj.Length; i++) g.adj[i] = new List<(int, float, List<Vector2>)>();

        pos = edgesStart;
        while ((pos = raw.IndexOf("\"a\":", pos, StringComparison.Ordinal)) >= 0)
        {
            pos += 4;
            int ca = raw.IndexOf(',', pos);
            int a = int.Parse(raw.Substring(pos, ca - pos));
            int bPos = raw.IndexOf("\"b\":", ca, StringComparison.Ordinal) + 4;
            int cb = raw.IndexOf(',', bPos);
            int b = int.Parse(raw.Substring(bPos, cb - bPos));
            int wPos = raw.IndexOf("\"w\":", cb, StringComparison.Ordinal) + 4;
            int cw = raw.IndexOf(',', wPos);
            float w = float.Parse(raw.Substring(wPos, cw - wPos), System.Globalization.CultureInfo.InvariantCulture);
            int ptsStart = raw.IndexOf("[[", cw, StringComparison.Ordinal);
            int ptsEnd = raw.IndexOf("]]", ptsStart, StringComparison.Ordinal);
            var pts = ParseEdgePts(raw.Substring(ptsStart, ptsEnd - ptsStart + 2));
            if (a >= 0 && a < g.adj.Length && b >= 0 && b < g.adj.Length)
            {
                g.adj[a].Add((b, w, pts));
                var rev = new List<Vector2>(pts);
                rev.Reverse();
                g.adj[b].Add((a, w, rev));
            }
            pos = ptsEnd;
        }
        return g;
    }

    static List<Vector2> ParseEdgePts(string seg)
    {
        var pts = new List<Vector2>();
        int i = 0;
        while ((i = seg.IndexOf('[', i + 1)) >= 0)
        {
            if (i + 1 < seg.Length && seg[i + 1] == '[') continue;
            int close = seg.IndexOf(']', i);
            if (close < 0) break;
            int comma = seg.IndexOf(',', i);
            if (comma > i && comma < close)
            {
                float u = float.Parse(seg.Substring(i + 1, comma - i - 1), System.Globalization.CultureInfo.InvariantCulture);
                float v = float.Parse(seg.Substring(comma + 1, close - comma - 1), System.Globalization.CultureInfo.InvariantCulture);
                pts.Add(new Vector2(u, v));
            }
            i = close;
        }
        return pts;
    }

    List<Vector2> FindSandboxStreetPath(string eventId, int stA, int stB)
    {
        var defs = GetSandboxStationDefs(eventId);
        var result = new List<Vector2>();
        if (stA < 0 || stB < 0 || stA >= defs.Length || stB >= defs.Length) return result;

        Vector2 cA = SandboxUvToCanvas(defs[stA].u, defs[stA].v);
        Vector2 cB = SandboxUvToCanvas(defs[stB].u, defs[stB].v);
        return FindSandboxStreetPathBetween(eventId, cA, cB, defs[stA].roadNode, defs[stB].roadNode);
    }

    List<Vector2> FindSandboxStreetPathToPoint(string eventId, int station, Vector2 point)
    {
        var defs = GetSandboxStationDefs(eventId);
        var g = GetOrLoadSandboxRoadGraph(eventId);
        if (station < 0 || station >= defs.Length || g == null || g.nodes.Length == 0) return new List<Vector2>();
        int nearest = 0; float best = float.MaxValue;
        for (int i = 0; i < g.nodes.Length; i++)
        {
            float d = (SandboxUvToCanvas(g.nodes[i].x, g.nodes[i].y) - point).sqrMagnitude;
            if (d < best) { best = d; nearest = i; }
        }
        return FindSandboxStreetPathBetween(eventId, SandboxUvToCanvas(defs[station].u, defs[station].v),
            SandboxUvToCanvas(g.nodes[nearest].x, g.nodes[nearest].y), defs[station].roadNode, nearest);
    }

    List<Vector2> FindSandboxStreetPathBetween(string eventId, Vector2 cA, Vector2 cB, int start, int goal)
    {
        var result = new List<Vector2>();
        var g = GetOrLoadSandboxRoadGraph(eventId);
        if (g == null || start < 0 || goal < 0 || start >= g.nodes.Length || goal >= g.nodes.Length) return result;
        if (start == goal) { result.Add(cA); result.Add(cB); return result; }

        int n = g.nodes.Length;
        var dist = new float[n];
        var prev = new int[n];
        var visited = new bool[n];
        for (int i = 0; i < n; i++) { dist[i] = float.MaxValue; prev[i] = -1; }
        dist[start] = 0f;
        var open = new List<int> { start };

        while (open.Count > 0)
        {
            int bestIdx = 0;
            for (int i = 1; i < open.Count; i++) if (dist[open[i]] < dist[open[bestIdx]]) bestIdx = i;
            int u = open[bestIdx];
            open.RemoveAt(bestIdx);
            if (visited[u]) continue;
            visited[u] = true;
            if (u == goal) break;
            var edges = g.adj[u];
            for (int i = 0; i < edges.Count; i++)
            {
                int v = edges[i].to;
                float nd = dist[u] + edges[i].w;
                if (nd < dist[v]) { dist[v] = nd; prev[v] = u; if (!visited[v]) open.Add(v); }
            }
        }

        if (prev[goal] < 0) return result;

        var nodeSeq = new List<int>();
        for (int cur = goal; cur >= 0; cur = prev[cur]) nodeSeq.Add(cur);
        nodeSeq.Reverse();

        result.Add(cA);
        for (int s = 0; s + 1 < nodeSeq.Count; s++)
        {
            int u = nodeSeq[s], v = nodeSeq[s + 1];
            var edges = g.adj[u];
            for (int i = 0; i < edges.Count; i++)
            {
                if (edges[i].to == v)
                {
                    var pts = edges[i].pts;
                    for (int p = 0; p < pts.Count; p++)
                    {
                        Vector2 cp = SandboxUvToCanvas(pts[p].x, pts[p].y);
                        if (Vector2.Distance(result[result.Count - 1], cp) > 3f) result.Add(cp);
                    }
                    break;
                }
            }
        }
        if (Vector2.Distance(result[result.Count - 1], cB) > 2f) result.Add(cB);
        return SmoothStreetPolyline(result);
    }

    static List<Vector2> SmoothStreetPolyline(List<Vector2> src)
    {
        if (src == null || src.Count < 3) return src;
        var clean = new List<Vector2>(src.Count) { src[0] };
        for (int i = 1; i < src.Count - 1; i++)
            if (Vector2.Distance(clean[clean.Count - 1], src[i]) > 10f) clean.Add(src[i]);
        clean.Add(src[src.Count - 1]);
        if (clean.Count < 3) return clean;

        var res = new List<Vector2>(clean.Count * 2) { clean[0] };
        for (int i = 0; i + 1 < clean.Count; i++)
        {
            Vector2 p0 = clean[i], p1 = clean[i + 1];
            if (i > 0) res.Add(Vector2.Lerp(p0, p1, 0.25f));
            if (i + 1 < clean.Count - 1) res.Add(Vector2.Lerp(p0, p1, 0.75f));
        }
        res.Add(clean[clean.Count - 1]);
        return res;
    }
}
