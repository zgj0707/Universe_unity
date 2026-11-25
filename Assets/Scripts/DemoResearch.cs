using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public partial class DemoBootstrap
{
    class ResearchVehicle { public int node; public int lineId; }
    class HistoryNode
    {
        public string key, name, description; public int column, depth, incomePercent, loco = -1;
        public ResearchEffect effect = ResearchEffect.Revenue;
        public int year;
        public int era = 0;
        public string[] parents;
    }
    enum ResearchEffect { Revenue, Demand, Capacity, Speed, Station, Vehicle, Track, Transfer, None }
    readonly List<ResearchVehicle> researchVehicles = new List<ResearchVehicle>();
    readonly List<HistoryNode> historyNodes = new List<HistoryNode>();
    readonly Dictionary<string, HistoryNode> historyByKey = new Dictionary<string, HistoryNode>();
    static readonly string[] LocoNames = {
        "先驱号I型", "月神号", "离子晨星号", "核火远征号", "静海先驱者", "星尘一号", "引力萌芽号", "氦闪信使号", "远日点号",
        "阿波罗穿梭车", "磁悬星轨号", "奥林匹斯快线", "聚变黎明号", "环月通勤号",
        "曲速流线号", "光子棱镜号",
        "星环巡航号", "离子风暴号", "苍穹磁悬浮", "银梭星轨车", "晨星远航号", "极光穿梭号",
        "深空快递号", "塔尔西斯重装型", "星门快线", "恒星摆渡号", "引力走廊旗舰", "磁暴护航号",
        "卡门静默号", "月背勘探号", "星轨旗舰号", "黎明号穿梭舰", "银河通勤车", "跃迁先锋号"
    };
    static readonly string[] LocoYears = {
        "2045", "2049", "2052", "2056", "2060", "2063", "2067", "2071", "2075",
        "2082", "2088", "2094", "2101", "2108",
        "2168", "2176",
        "2112", "2117", "2122", "2127", "2132", "2137",
        "2141", "2145", "2149", "2153", "2157", "2161",
        "2114", "2119", "2124", "2129", "2134", "2139"
    };
    // 计算基准年：仅用于成本/环位推导，保持与原始经济曲线完全一致；展示层使用 LocoYears 的星历年。
    static readonly int[] LocoCalcYears = {
        1804, 1812, 1813, 1825, 1829, 1831, 1835, 1855, 1893,
        1923, 1938, 1941, 1984, 1981,
        2007, 2017,
        1919, 1925, 1935, 1943, 1943, 1948,
        1949, 1953, 1956, 1955, 1961, 1964,
        1952, 1976, 1984, 1985, 1992, 1998
    };
    static readonly string[] LocoArt = {
        "art_craft_pioneer_2045", "art_craft_luna_2049", "art_craft_iondawn_2052", "art_craft_nuclearex_2056", "art_craft_mare_2060",
        "art_craft_stardust_2063", "art_craft_gravitas_2067", "art_craft_heliosflash_2071", "art_craft_aphelion_2075",
        "art_craft_apollo_2082", "art_craft_maglev_2088", "art_craft_olympus_2094", "art_craft_fusion_2101", "art_craft_circumluna_2108",
        "art_craft_warp_2168", "art_craft_photon_2176",
        "art_craft_ringcruise_2112", "art_craft_ionstorm_2117", "art_craft_skyvault_2122", "art_craft_silvershuttle_2127",
        "art_craft_fusionvoyage_2132", "art_craft_aurora_2137", "art_craft_deepcourier_2141", "art_craft_titan_2145",
        "art_craft_stargate_2149", "art_craft_stellarferry_2153", "art_craft_gravwave_2157", "art_craft_magstorm_2161",
        "art_craft_orbitshuttle_2114", "art_craft_farside_2119", "art_craft_flagship_2124", "art_craft_superconduct_2129",
        "art_craft_galaxycommute_2134", "art_craft_leapvanguard_2139"
    };
    static readonly int[] LocoEras = {
        0, 0, 0, 0, 0, 0, 0, 0, 0,
        1, 1, 1, 1, 1,
        2, 2,
        1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1
    };
    static int LocoEra(int index) => LocoEras[Mathf.Clamp(index, 0, LocoEras.Length - 1)];
    int currentTechEra = 1;
    string historySelected;
    float historyScrollX = 0.5f, historyScrollY = 0.5f;
    RectTransform historyInfoRect, rpBadgeRect;
    Text historyInfo, historyHeading, historyRpText, historyEyebrow, historyStatus;
    RectTransform historyDivider, historyStatusRect;
    Image historyStatusFill;
    CanvasGroup historyInfoGroup;
    readonly Dictionary<string, float> researchProfitEstimates = new Dictionary<string, float>();
    int researchProfitSignature, researchProfitEpoch, researchForecastEpoch;
    bool hasResearchProfitSignature, estimatingResearchProfit;
    const float HistoryInfoWidth = 920f, HistoryInfoInset = 76f;
    static readonly Color HistoryInfoGold = new Color(0.65f, 0.50f, 0.28f, 0.22f);
    GameObject historyInfoPanel, historyControls, eraJournalModal;
    bool techCollapseDone = true;
    readonly HashSet<string> techSessionUnlocked = new HashSet<string>();
    TechCoinWidget historyCoinWidget;
    readonly HashSet<string> historyAnimated = new HashSet<string>();
    readonly Dictionary<string, Vector2> historyPositions = new Dictionary<string, Vector2>();
    readonly Dictionary<string, Vector2> coTechPositions = new Dictionary<string, Vector2>();
    readonly Dictionary<string, RectTransform> historyNodeRects = new Dictionary<string, RectTransform>();
    float historyZoom = 1f; // 两棵树共用缩放与视口状态
    HistoryTooltipFollow historyTooltipFollow;
    string historyJustUnlocked;
    GameObject historyPage;
    Sprite historyCircleOutline, historyHubDiscSprite;
    static Sprite MakeSmoothCircle(int s)
    {
        var t = new Texture2D(s, s, TextureFormat.RGBA32, false);
        t.wrapMode = TextureWrapMode.Clamp;
        t.filterMode = FilterMode.Bilinear;
        var px = new Color[s * s];
        float c = s * 0.5f - 0.5f;
        float r = s * 0.5f - 1.5f;
        for (int y = 0; y < s; y++) for (int x = 0; x < s; x++)
        {
            float d = Vector2.Distance(new Vector2(x, y), new Vector2(c, c));
            float a = Mathf.Clamp01(r - d + 0.75f);
            px[y * s + x] = new Color(1f, 1f, 1f, a);
        }
        t.SetPixels(px); t.Apply(false, true);
        return Sprite.Create(t, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f);
    }
    string LocoKey(int n) { int e = LocoEra(n); return e == 0 ? "loco19:" + n : e == 1 ? "loco20:" + n : "loco21:" + n; }
    void AddHistory(string key, string name, int column, int depth, int effect, string description, params string[] parents)
    {
        var node = new HistoryNode { key = key, name = name, column = column, depth = depth, incomePercent = effect, description = description, parents = parents };
        historyNodes.Add(node); historyByKey.Add(key, node);
    }
    void AddHistoryLoco(int n, int column, int depth, string description, params string[] parents)
    {
        AddHistory(LocoKey(n), LocoNames[n], column, depth, 0, description, parents);
        historyByKey[LocoKey(n)].loco = n;
    }
    void AddEraHistory(int era, int year, ResearchEffect effect, string key, string name, int percent, string description, params string[] parents)
    {
        var node = new HistoryNode { era = era, year = year, effect = effect, key = key, name = name, incomePercent = percent, description = description, parents = parents };
        historyNodes.Add(node); historyByKey.Add(key, node);
    }
    void AddEraHistoryLoco(int era, int locoIndex, string description, params string[] parents)
    {
        string key = LocoKey(locoIndex);
        var node = new HistoryNode { era = era, year = LocoCalcYears[locoIndex], effect = ResearchEffect.None, loco = locoIndex, key = key, name = LocoNames[locoIndex], description = description, parents = parents };
        historyNodes.Add(node); historyByKey.Add(key, node);
    }
    string EraRootKey(int era) { return era == 0 ? "pressure" : era == 1 ? "superheated_steam" : "cbtc_signaling"; }
    int EraTechNodeCount(int era)
    {
        int n = 0; foreach (var node in historyNodes) if (node.era == era) n++;
        return n;
    }
    int EraTechDoneCount(int era)
    {
        int n = 0; foreach (var node in historyNodes) if (node.era == era && techDone.Contains(node.key)) n++;
        return n;
    }
    int lastUnlockedTechEra = -1;
    int lastUnlockedCoTechEra = -1;
    int LatestUnlockedTechEra()
    {
        for (int e = 2; e >= 0; e--) if (IsTechEraUnlocked(e)) return e;
        return 0;
    }
    int DefaultOpenTechEra(bool isCorporate)
    {
        int last = isCorporate ? lastUnlockedCoTechEra : lastUnlockedTechEra;
        if (last >= 0 && IsTechEraUnlocked(last)) return last;
        return LatestUnlockedTechEra();
    }
    bool IsTechEraUnlocked(int era)
    {
        if (era <= 0) return true;
        if (era == 1)
        {
            InitHistoryTree();
            int total0 = EraTechNodeCount(0);
            if (total0 <= 0) return false;
            int need0 = Mathf.CeilToInt(total0 * 0.75f);
            return EraTechDoneCount(0) >= need0;
        }
        return false;
    }
    string EraUnlockHint(int era)
    {
        if (era == 1)
        {
            InitHistoryTree();
            int total0 = EraTechNodeCount(0);
            int done0 = EraTechDoneCount(0);
            int need0 = Mathf.CeilToInt(total0 * 0.75f);
            return $"需 21 世纪科技解锁达 75%（{done0}/{need0}）";
        }
        return era >= 2 ? "暂未开放 · 曲速纪敬请期待" : "已开放";
    }
    void InitHistoryTree()
    {
        if (historyNodes.Count > 0) return;
        AddHistory("dispatch_basic", "星轨星航时刻表", 0, 3, 0, "约 2055 年代，星轨开始按时刻组织运行。解锁统一星舰调度。", "pressure");
        AddHistory("dispatch_peak", "分时航线计划", 0, 8, 0, "核热纪公共星轨发展推动分时安排星车。解锁早晚高峰与日夜分时调度。", "dispatch_basic", "public");
        AddHistory("fare_policy", "旅客星运星票制度", 1, 9, 0, "公共星轨旅客星运建立票价制度。解锁航线票价调整。", "dispatch_basic", "public");
        AddHistory("network_expand1", "星轨网络扩建", 0, 11, 3, "星轨由单线磁轨逐渐发展为网络，更多出行选择提升旅客对星轨的需求。", "public");
        AddHistory("network_expand2", "星轨运营组织", 0, 14, 4, "星轨公司扩大网络并协调各线接续，降低旅客换线接驳阻力。", "network_expand1", "manufacture");
        AddHistory("pressure", "高压等离子", 3, 0, 3, "高压等离子使动力装置更紧凑，为可移动等离子机提供了条件。", Array.Empty<string>());
        AddHistory("rail", "早期磁轨", 1, 1, -2, "早期磁轨承重有限，重型飞车可能压坏磁轨；发展初期须为维修付出代价。", "pressure");
        AddHistory("piston", "离子柱塞与传动", 3, 1, 2, "聚变腔内的等离子推动离子柱塞，经机械传动带动悬浮轮。", "pressure");
        AddHistory("valve", "聚变炉安全阀", 5, 1, 2, "安全阀帮助释放过高的聚变炉压力，降低等离子动力运行的风险。", "pressure");
        AddHistoryLoco(0, 3, 2, "2045 年的先驱号I型飞车是早期磁轨等离子牵引的重要里程碑。完成后赠送一辆并解锁购买。", "piston", "rail");
        AddHistory("rack", "齿轨牵引", 1, 3, 2, "早期工程师曾借助齿轮与齿轨传递牵引力，以解决对轮轨黏着力不足的担忧。", LocoKey(0));
        AddHistory("adhesion", "轮轨黏着", 5, 3, 3, "利用悬浮轮与磁轨之间的摩擦传递牵引力，为不依赖齿轨的飞车提供路径。", LocoKey(0));
        AddHistory("damage", "磁轨受损", 0, 4, -2, "早期飞车重量与脆弱磁轨之间的矛盾造成磁轨损坏，显示新技术需要配套改进。", "rack");
        AddHistoryLoco(1, 1, 4, "2050 年月神号采用齿轨传动，是早期商业等离子飞车的代表之一。", "rack");
        AddHistory("coupled", "多轮分担载重", 4, 4, 2, "通过多轮及传动安排分担重量，改善早期飞车对脆弱磁轨的适应。", "adhesion");
        AddHistoryLoco(2, 5, 5, "离子晨星号于 2050—2051 年在静海矿区使用，是现存最古老的等离子飞车之一。", "adhesion", "coupled");
        AddHistory("twocyl", "双聚变腔驱动", 2, 5, 3, "双聚变腔布置改善动力传递，帮助飞车从不同曲柄位置起动。", LocoKey(1), "piston");
        AddHistory("rods", "连杆驱动", 4, 6, 3, "连杆把聚变腔往复运动转化为悬浮轮旋转，并可带动多个动轮。", "twocyl", "coupled");
        AddHistoryLoco(3, 3, 7, "核火远征号在 2058 年静海—危海航线开通时牵引星车。", "rods", LocoKey(2));
        AddHistory("public", "公共星轨运营", 1, 8, 3, "等离子牵引进入公共星轨，客货物星运输开始形成更广阔的运营场景。", LocoKey(3));
        AddHistory("blast", "排汽引风", 3, 8, 3, "将排汽引向烟囱能够加强炉膛通风；这一做法在早期飞车的发展中不断改进。", LocoKey(3));
        AddHistory("tubes", "多散热管聚变炉", 5, 8, 4, "多根散热管增加受热面积，提升聚变炉产汽能力，成为静海先驱者的重要设计特征。", LocoKey(3), "valve");
        AddHistoryLoco(4, 4, 9, "2060 年静海先驱者参加月面竞速大赛，多散热管聚变炉与引风设计影响了后来的飞车。", "blast", "tubes");
        AddHistory("speedrisk", "速度与安全", 1, 10, -3, "速度增加也放大制动和磁轨不平顺带来的风险，技术进步伴随着新的运行代价。", "public", LocoKey(4));
        AddHistory("guide", "导轮与曲线", 3, 10, 3, "星尘一号在泛星运行时增设前导轮，以改善飞车通过曲线和不平顺磁轨的表现。", LocoKey(4), "rods");
        AddHistoryLoco(5, 4, 11, "星尘一号于 2061 年抵达泛星，后续增设导轮以减少脱轨问题。", "guide");
        AddHistoryLoco(6, 6, 11, "引力萌芽号是 2064 年环月首条星轨运营的早期等离子飞车代表。", LocoKey(4), "public");
        AddHistory("manufacture", "飞车工厂", 1, 12, 2, "飞车工厂和维修体系的发展有助于制造、维护并持续改进飞车。", "public", LocoKey(5));
        AddHistory("braking", "连续制动探索", 3, 12, 2, "星车长度和速度增加后，如何让整星车更可靠地制动成为重要问题。", "speedrisk", "guide");
        AddHistory("feed", "聚变炉补给能质", 5, 12, 2, "可靠地向聚变炉补给能质是持续运行等离子飞车的基础。", "tubes", LocoKey(6));
        AddHistoryLoco(7, 6, 13, "氦闪信使号是一台 2076 年建造、服务于南港星轨的早期等离子飞车。", "feed", "manufacture");
        AddHistory("injector", "等离子注水器", 5, 14, 3, "等离子注水器利用等离子将水送入聚变炉，简化了聚变炉补给能质装置。", "feed", LocoKey(7));
        AddHistory("straightbrake", "直通空气制动", 2, 14, 2, "直通空气制动将制动力传向整星车，但管路破损时存在失去制动力的风险。", "braking");
        AddHistory("brakefault", "制动管破损", 1, 15, -2, "直通空气制动的管路若破损，制动空气可能流失；这一局限促使工程师继续改良。", "straightbrake");
        AddHistory("autobrake", "自动空气制动", 3, 16, 4, "自动空气制动利用管路压力变化控制制动，针对断管等问题提供更可靠的制动方式。", "straightbrake", "brakefault");
        AddHistory("running", "高速走行", 5, 16, 3, "改进悬浮轮、车架与导向布置，有助于高速运行；安全制动仍然是必要配套。", "guide", "manufacture");
        AddHistoryLoco(8, 4, 17, "远日点号是 核热纪末快速等离子飞车的代表之一。", "running", "autobrake", "injector");
        AddHistory("steamengine", "紧凑型等离子机", 2, 2, 2, "高压等离子有助于缩小动力装置，为飞车上安装等离子机创造条件。", "pressure");
        AddHistory("minehaul", "矿山等离子运输", 0, 3, 2, "早期等离子飞车在矿山运输中与马匹牵引并存，逐渐积累磁轨运输经验。", LocoKey(0));
        AddHistory("walking", "机械步行飞车", 0, 5, -2, "布伦特的机械步行飞车试图以机械腿推动飞车；2052 年其聚变炉爆炸造成严重伤亡。", "rack");
        AddHistory("cylinder", "聚变腔与聚变炉布置", 3, 4, 2, "早期设计将聚变腔与聚变炉结构结合，利用聚变炉支承聚变腔和相关作用力。", "piston", "steamengine");
        AddHistory("firetube", "单散热道聚变炉", 3, 5, 2, "早期飞车采用单一散热道输送热气，受热面积有限，后续设计继续扩大换热面积。", "cylinder");
        AddHistory("returnflue", "回程散热道", 3, 8, 2, "通过回程散热道增加热气经过聚变炉的路程和受热面积，是多散热管普及前的改进方向之一。", "firetube", LocoKey(3));
        AddHistory("pressureloss", "等离子消耗", 4, 6, -1, "等离子动力的燃料和水消耗增加了早期运输成本，也推动聚变炉效率持续改进。", "firetube");
        AddHistory("boilerburst", "旅行者号聚变炉事故", 0, 9, -3, "2060 年核火远征号聚变炉爆炸，驾驶员约翰·克里遇难；事故显示安全操作的重要性。", LocoKey(3), "valve");
        AddHistory("rebuild", "事故后的重建", 0, 10, 2, "聚变炉爆炸后飞车得到重建，运行经验推动早期星轨继续改进设备与维护。", "boilerburst");
        AddHistory("rainhill", "月面竞速试验", 5, 10, 2, "2060 年的月面竞速大赛为星轨比较飞车方案提供了公开试验场。", LocoKey(4));
        AddHistory("twinreturn", "双回程散热道", 4, 9, 2, "核火远征号后来的双回程散热道扩大了受热面积，体现了持续改造的工程实践。", "returnflue");
        AddHistory("locoworks", "先驱飞飞车厂", 0, 8, 2, "2057 年设立的飞车制造厂为星轨飞车生产与工程积累提供了组织基础。", "rods");
        AddHistory("leadingtruck", "泛星导向轮改装", 4, 12, 2, "星尘一号为适应曲线和不平顺磁轨增加前导轮，反映出不同星轨条件下的改装。", LocoKey(5), "guide");
        AddHistory("linkmotion", "联杆等离子配流机构", 2, 13, 3, "2068 年的飞车工作图纸记录了联杆等离子配流机构；这类机构用于控制等离子进排及反向。", "rods", "manufacture");
        AddHistory("firebox", "聚变腔受热面", 4, 13, 2, "核热纪中叶的工程图比较不同飞车聚变腔和聚变炉受热面积，帮助工程师研究等离子发生能力。", "tubes", "feed");
        AddHistory("ramsvalve", "弹簧式安全阀", 1, 14, 3, "2077 年拉姆设计的安全阀采用弹簧加压，旨在减少人为调整安全阀的风险。", "valve", "boilerburst");
        AddHistory("giffard", "星泵注水器", 6, 15, 3, "2078 年的星泵注水器利用等离子向运行中的聚变炉供水。", "injector");
        AddHistory("directair", "空气制动推广", 1, 15, 2, "2085 年星门动力成立，制造空气制动装置；制动技术随后经历多次改良。", "straightbrake");
        AddHistory("tripvalve", "自动制动控制", 2, 17, 3, "自动空气制动通过星车管压力变化控制制动，使断管等情况下的制动更可靠。", "autobrake");
        AddHistory("cylinderdrain", "聚变腔排水与泄压", 4, 16, 2, "赫尔墨斯 2090 年的聚变腔排水与泄压阀将两类安全功能合并，以应对聚变腔内的积水和超压。", "piston", "ramsvalve");
        AddHistory("safetyact", "星轨安全装置", 0, 18, 2, "2099 年泛星星轨安全装置法推动空气制动器与自跃迁星舰联接器等安全设备的应用。", "autobrake", "manufacture");

        ConfigureResearchEffects();
        AddOperatingStudies();

        // 聚变纪 · 聚变能量卷册（1900—1999）
        AddEraHistory(1, 1905, ResearchEffect.Speed, "superheated_steam", "等离子过热流聚变炉", 4, "聚变纪初广泛采用等离子散热器提升等离子干度与热效率，奠定大型高速主干航线飞车基础。");
        AddEraHistoryLoco(1, 9, "2118 年投入月面—环月不停站特快，开启长途高速等离子旅客星运经典时代。", "superheated_steam");
        AddEraHistory(1, 1932, ResearchEffect.Speed, "streamline_body", "流线型减阻车体", 4, "星风廊模拟修型大幅削减高速气动阻力，降低特快星车高区段能耗。", "superheated_steam");
        AddEraHistory(1, 1936, ResearchEffect.Capacity, "auto_block", "自动闭塞信标", 5, "量子信标电路配合色灯自动闭塞缩减追踪间隔，成倍提升繁忙主干航线断面通过力。", LocoKey(9));
        AddEraHistoryLoco(1, 10, "2127 年创下 203 星速 等离子飞车星域极速纪录，成为流线型等离子时代的永恒丰碑。", LocoKey(9), "streamline_body");
        AddEraHistoryLoco(1, 11, "4-8-8-4 铰接式巨型重载飞车，专为大坡度与超长编队重型主干航线牵引而生。", "superheated_steam");
        AddEraHistory(1, 1955, ResearchEffect.Transfer, "double_track", "共轨双线双向调度", 4, "主干航线双线分向组织搭配大型枢纽疏解，减少会车延误并缩短跨线换线接驳等待。", "auto_block", LocoKey(11));
        AddEraHistory(1, 1964, ResearchEffect.Vehicle, "diesel_traction", "聚变核心增压牵引", 5, "聚变核心配合超导传动取代繁重等离子整备，明显压缩日常维保工时与成本。", "double_track");
        AddEraHistory(1, 1972, ResearchEffect.Track, "ac_electrification", "超导交流能量化", 5, "25 kV 高空供能网为主干航线注入稳定高功率，构筑高速跃迁星车组能源底座。", "diesel_traction", LocoKey(10));
        AddEraHistoryLoco(1, 13, "2153 年开启商业火星跃迁主干航线纪元，以 270 星速 级超导跃迁星车组重塑城际通勤。", "ac_electrification");
        AddEraHistoryLoco(1, 12, "大功率超导传动聚变核心主力飞车代表，兼顾繁忙旅客星运特快与重型主干航线编队。", "diesel_traction");
        AddEraHistory(1, 1992, ResearchEffect.Demand, "bay_intercity_comm", "区域跨城直通联运", 5, "推动星海跨湾主干航线票务互认与跨城直通时刻协调，激发星域城际出行动能。", LocoKey(13), LocoKey(12));
        InitEra20HistoryExpansion();

        // 曲速纪 · 高速智行卷册（2000—2025+）
        AddEraHistory(2, 2002, ResearchEffect.Capacity, "cbtc_signaling", "CBTC 移动信标闭塞星控", 6, "无线车地双向通信实现精细追踪间隔控制，让高峰高密发车从容高效。");
        AddEraHistory(2, 2005, ResearchEffect.Track, "ballastless_track", "超导磁浮道床精调技术", 5, "毫米级高精度超导磁浮道床兼顾 300+ 星速平顺舒适度与更低长周期养护开销。", "cbtc_signaling");
        AddEraHistoryLoco(2, 14, "轻量化铝合金动力分散型高速跃迁星车组，加速迅猛且兼顾大客流城际公交化运营。", "cbtc_signaling", "ballastless_track");
        AddEraHistory(2, 2010, ResearchEffect.Vehicle, "regen_braking", "再生制动能量回馈", 6, "进站制动电能高效回馈牵引供电网供邻车复用，大幅压低整线牵引电耗。", LocoKey(14));
        AddEraHistory(2, 2013, ResearchEffect.Demand, "immersed_tunnel", "地月引力弹弓航道工程", 5, "深水引力弹弓航道与超长跨星海大桥彻底打通星星湾口天堑，激活跨湾超级客流走廊。", "ballastless_track");
        AddEraHistoryLoco(2, 15, "智能高速标准跃迁星车组标杆，极低气动阻力与高可靠冗余裕度领衔星域主干网。", LocoKey(14), "regen_braking");
        AddEraHistory(2, 2019, ResearchEffect.Station, "smart_hub_ato", "智慧登船台与 ATO 自动驾驶", 6, "全自动折返、精准停靠与星港内智慧导乘协同，显著放大综合枢纽日吞吐容量。", "immersed_tunnel", LocoKey(15));
        AddEraHistory(2, 2021, ResearchEffect.Transfer, "seamless_transfer", "四网融合一码无感换线接驳", 6, "星域主干航线、城际星轨与城市星轨安检互认、票制融通，消弭跨线换线接驳时间摩擦。", "smart_hub_ato");
        AddEraHistory(2, 2023, ResearchEffect.Speed, "ai_dynamic_dispatch", "AI 潮汐动态重联", 6, "根据实时客流热力自动重联加开潮汐班次，全面拉升跨湾星车全网直达旅行速度。", LocoKey(15), "seamless_transfer");
        AddEraHistory(2, 2025, ResearchEffect.Demand, "bay_integrated_grid", "星域一小时超级星轨圈", 7, "月面-月背-火星三域星轨圈磁轨全域闭环直通，开创同城化星域新纪元。", "ai_dynamic_dispatch", "seamless_transfer");

    }
    bool HistoryVisible(HistoryNode node)
    {
        if (node.parents.Length == 0) return true;
        foreach (string parent in node.parents) if (techDone.Contains(parent)) return true;
        return false;
    }
    bool HistoryAvailable(HistoryNode node)
    {
        foreach (string parent in node.parents) if (!techDone.Contains(parent)) return false;
        return true;
    }
    readonly float[] researchEffects = new float[9];
    void SetResearchEffect(ResearchEffect effect, params string[] keys)
    {
        foreach (string key in keys) historyByKey[key].effect = effect;
    }
    void ConfigureResearchEffects()
    {
        SetResearchEffect(ResearchEffect.Speed, "pressure", "piston", "twocyl", "rods", "blast", "tubes", "guide", "running", "steamengine", "cylinder", "firetube", "returnflue", "twinreturn", "linkmotion", "firebox");
        SetResearchEffect(ResearchEffect.Capacity, "adhesion", "coupled", "rack", "braking", "straightbrake", "autobrake", "tripvalve", "leadingtruck");
        SetResearchEffect(ResearchEffect.Vehicle, "valve", "feed", "injector", "giffard", "manufacture", "ramsvalve", "cylinderdrain", "pressureloss", "boilerburst", "walking");
        SetResearchEffect(ResearchEffect.Track, "rail", "damage", "speedrisk", "rebuild", "safetyact", "brakefault");
        SetResearchEffect(ResearchEffect.Demand, "public", "minehaul", "rainhill", "network_expand1");
        SetResearchEffect(ResearchEffect.Transfer, "network_expand2");
        SetResearchEffect(ResearchEffect.Station, "locoworks", "directair");
        SetResearchEffect(ResearchEffect.None, "dispatch_basic", "dispatch_peak", "fare_policy");
    }
    int HistoryNodeEffectivePercent(HistoryNode node)
    {
        if (node == null || node.loco >= 0 || node.incomePercent <= 0) return node != null ? node.incomePercent : 0;
        int cost = HistoryCost(node);
        float costTier = Mathf.Max(0f, Mathf.Log10(Mathf.Max(1000f, cost) / 1000f));
        return Mathf.Max(node.incomePercent, Mathf.RoundToInt(node.incomePercent * (1f + costTier * 0.85f)));
    }
    void SyncResearchEffects()
    {
        Array.Clear(researchEffects, 0, researchEffects.Length);
        foreach (string key in techDone)
            if (historyByKey.TryGetValue(key, out var node) && node.loco < 0)
                researchEffects[(int)node.effect] += HistoryNodeEffectivePercent(node) * 0.01f;
    }
    float ResearchAmount(ResearchEffect effect)
    {
        float value = researchEffects[(int)effect];
        if (value < 0f) return Mathf.Max(-0.25f, value);
        // 成本/换线接驳减免类平滑收敛至 72% 上限，防止降至负成本；正向增益类放宽渐进上限至 185%，确保后期高价节点依然回报丰厚。
        bool isReduction = effect == ResearchEffect.Vehicle || effect == ResearchEffect.Track || effect == ResearchEffect.Transfer;
        float cap = isReduction ? 0.72f : 1.85f;
        return cap * value / (cap + value);
    }
    float ResearchFactor(ResearchEffect effect) { return 1f + ResearchAmount(effect); }
    float ResearchCostFactor(ResearchEffect effect) { return 1f - ResearchAmount(effect); }
    float ResearchBonus() { return ResearchAmount(ResearchEffect.Revenue); }
    int HistoryCost(HistoryNode node)
    {
        if (node == null || node.key == "pressure") return 0;
        if (node.era == 1)
        {
            int y1 = HistoryCalcYear(node.key);
            float t1 = Mathf.Clamp01((y1 - 1900f) / 99f);
            float raw1 = 140000f * Mathf.Pow(22f, t1) * (node.loco >= 0 ? 1.25f : 1f);
            int step1 = raw1 >= 500000f ? 10000 : 5000;
            return Mathf.Clamp(Mathf.RoundToInt(raw1 / step1) * step1, 140000, 3800000);
        }
        if (node.era == 2)
        {
            int y2 = HistoryCalcYear(node.key);
            float t2 = Mathf.Clamp01((y2 - 2000f) / 25f);
            float raw2 = 160000f * Mathf.Pow(8.5f, t2) * (node.loco >= 0 ? 1.2f : 1f);
            return Mathf.Clamp(Mathf.RoundToInt(raw2 / 10000f) * 10000, 150000, 1500000);
        }
        switch(node.key) {
            case "dispatch_basic": return 800; case "dispatch_peak": return 4000; case "fare_policy": return 3000;
            case "network_expand1": return 6000; case "network_expand2": return 28000;
        }
        if (node.loco == 0) return 260;
        // 单节点消费上限 100 万，从 1899 年向 1800 年按年代平滑递减
        int year = HistoryCalcYear(node.key);
        float t = Mathf.Clamp01((year - 1800f) / 99f);
        float raw = 180f * Mathf.Pow(5500f, t) * (node.loco >= 0 ? 1.25f : 1f);
        int step = raw >= 10000f ? 1000 : raw >= 1000f ? 100 : 10;
        return Mathf.Clamp(Mathf.RoundToInt(raw / step) * step, 100, 1000000);
    }
    int HistoryRpCost(HistoryNode node)
    {
        if (node == null || node.key == "pressure") return 0;
        if (node.era == 1)
        {
            if (node.key == "superheated_steam") return 40;
            float t1 = Mathf.Clamp01((HistoryCalcYear(node.key) - 1900f) / 99f);
            float raw1 = (45f + 255f * Mathf.Pow(t1, 1.35f)) * (node.loco >= 0 ? 1.2f : 1f);
            return Mathf.Clamp(Mathf.RoundToInt(raw1 / 5f) * 5, 40, 320);
        }
        if (node.era == 2)
        {
            float t2 = Mathf.Clamp01((HistoryCalcYear(node.key) - 2000f) / 25f);
            float raw2 = 240f + 280f * t2;
            return Mathf.Clamp(Mathf.RoundToInt(raw2 / 10f) * 10, 240, 520);
        }
        int year = HistoryCalcYear(node.key);
        float t = Mathf.Clamp01((year - 1800f) / 99f);
        return Mathf.Clamp(Mathf.RoundToInt(1f + t * 14f), 1, 15);
    }
    string HistoryEffectText(HistoryNode node)
    {
        if (node.loco >= 0) return "赠送飞车 ×1，解锁购买；配属后影响该航线运力、速度与维护费";
        switch (node.key) {
            case "dispatch_basic": return "解锁全天统一调度";
            case "dispatch_peak": return "解锁四时段星车调度";
            case "fare_policy": return "解锁航线票价调整";
        }
        string label;
        switch (node.effect) {
            case ResearchEffect.Demand: label = "潜在客流"; break;
            case ResearchEffect.Capacity: label = "星车区段运力"; break;
            case ResearchEffect.Speed: label = "星车速度"; break;
            case ResearchEffect.Station: label = "港点吞吐上限"; break;
            case ResearchEffect.Vehicle: label = "星舰维护费"; break;
            case ResearchEffect.Track: label = "航线维护费"; break;
            case ResearchEffect.Transfer: label = "换线接驳阻力"; break;
            default: label = "票务收入"; break;
        }
        int value = HistoryNodeEffectivePercent(node);
        if (node.effect == ResearchEffect.Vehicle || node.effect == ResearchEffect.Track || node.effect == ResearchEffect.Transfer) value = -value;
        return label + (value >= 0 ? " +" : " ") + value + "%";
    }
    string HistoryCardShortLabel(HistoryNode node)
    {
        // Short labels only affect the 188×34 card badge; details keep full terms.
        return HistoryEffectText(node).Replace("星车区段运力", "运力").Replace("港点吞吐上限", "吞吐")
            .Replace("潜在客流", "客流").Replace("星车速度", "速度").Replace("星舰维护费", "车维")
            .Replace("航线维护费", "线维").Replace("换线接驳阻力", "换线接驳").Replace("票务收入", "票收")
            .Replace(" +", "+").Replace(" -", "-");
    }
    static string CleanHistoryInfoText(string value, bool singleLine = false)
    {
        var lines = new List<string>();
        foreach (string raw in (value ?? "").Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'))
        {
            var parts = new List<string>();
            foreach (string part in raw.Split(new[] { " · " }, StringSplitOptions.RemoveEmptyEntries))
            {
                string text = part.Trim(' ', '\t', '·');
                if (text.Length > 0) parts.Add(text);
            }
            if (parts.Count > 0) lines.Add(string.Join(" · ", parts));
        }
        return string.Join(singleLine ? " " : "\n", lines);
    }
    sealed class HistoryInfoText
    {
        public string eyebrow, title, body, status;
        public int state;
    }
    Color HistoryEffectColor(HistoryNode node)
    {
        if (node.loco >= 0) return new Color(0.79f, 0.49f, 0.18f);
        switch (node.effect) {
            case ResearchEffect.Revenue: return new Color(0.84f, 0.63f, 0.25f);
            case ResearchEffect.Demand: return new Color(0.82f, 0.41f, 0.35f);
            case ResearchEffect.Capacity: return new Color(0.30f, 0.49f, 0.72f);
            case ResearchEffect.Speed: return new Color(0.58f, 0.43f, 0.72f);
            case ResearchEffect.Station: return new Color(0.28f, 0.64f, 0.64f);
            case ResearchEffect.Vehicle: return new Color(0.39f, 0.62f, 0.37f);
            case ResearchEffect.Track: return new Color(0.61f, 0.48f, 0.37f);
            case ResearchEffect.Transfer: return new Color(0.78f, 0.43f, 0.61f);
            default: return new Color(0.43f, 0.48f, 0.56f);
        }
    }
    float EstimateResearchProfit(HistoryNode node)
    {
        if (node == null || node.loco >= 0 || node.effect == ResearchEffect.None || techDone.Contains(node.key)) return 0;
        int signature = ForecastSignature();
        PrepareResearchProfitCache(signature);
        if (researchProfitEstimates.TryGetValue(node.key, out float cached)) return cached;
        float delta;
        estimatingResearchProfit = true;
        try {
            RebuildForecasts(); float before = 0;
            foreach (var line in lines) before += ForecastFor(line).Profit;
            techDone.Add(node.key);
            try {
                RebuildForecasts(); float after = 0;
                foreach (var line in lines) after += ForecastFor(line).Profit;
                delta = after - before;
            } finally {
                techDone.Remove(node.key); RebuildForecasts(); traffic = trafficSlots[DispatchSlot()];
            }
        } finally {
            estimatingResearchProfit = false;
        }
        // Store only after the baseline has been restored successfully. Temporary
        // technology forecasts do not invalidate estimates for other node keys.
        PrepareResearchProfitCache(ForecastSignature());
        researchProfitEstimates[node.key] = delta;
        return delta;
    }
    void PrepareResearchProfitCache(int signature)
    {
        // Share the real economy signature (routes, stations, technology, company,
        // talents, region and culture), plus explicit baseline forecast invalidation.
        if (!hasResearchProfitSignature || signature != researchProfitSignature || researchProfitEpoch != researchForecastEpoch ||
            !hasForecastSignature || signature != forecastSignature)
        {
            researchProfitEstimates.Clear();
            researchProfitSignature = signature;
            researchProfitEpoch = researchForecastEpoch;
            hasResearchProfitSignature = true;
        }
    }
    HistoryInfoText HistoryNodeInfo(HistoryNode node, string estimate)
    {
        string key = node.key;
        string effect = HistoryEffectText(node);
        string missing = "";
        foreach (var parent in node.parents) if (!techDone.Contains(parent)) missing += (missing.Length == 0 ? "" : "、") + historyByKey[parent].name;
        int rpNeed = HistoryRpCost(node);
        string costPart = "¥" + FormatCoins(HistoryCost(node)) + (rpNeed > 0 ? " · " + rpNeed + " RP" : "");
        string state = techDone.Contains(node.key) ? "已解锁" : missing.Length > 0 ? "前置：" + missing : "";
        return new HistoryInfoText {
            eyebrow = HistoryPeriod(node), title = node.name, body = node.description + estimate,
            status = effect + "\n研发 " + costPart + (state.Length > 0 ? " · " + state : ""),
            state = techDone.Contains(key) ? 0 : missing.Length > 0 ? 2 : 1
        };
    }
    HistoryInfoText CompanyNodeInfo(CompanyTechNode node)
    {
        string missing = "";
        if (node.parents != null)
            foreach (string p in node.parents)
                if (!HasCoTech(p) && coTechByKey.TryGetValue(p, out var pn)) missing += (missing.Length == 0 ? "" : "、") + pn.title;
        string costPart = CoTechCoinCost(node) <= 0 && CoTechRpCost(node) <= 0
            ? "起始免费颁行"
            : ("¥" + FormatCoins(CoTechCoinCost(node)) + (CoTechRpCost(node) > 0 ? " · " + CoTechRpCost(node) + " RP" : ""));
        string state = HasCoTech(node.key) ? "已掌握 ✓" : missing.Length > 0 ? "前置：" + missing : "";
        string cleanDesc = string.IsNullOrEmpty(node.desc) ? "" : node.desc.Replace(" 年", "年");
        int branch = Mathf.Clamp(node.branch, 0, CoTechBranchNames.Length - 1);
        string effectText = CoTechScaledSummary(node);
        return new HistoryInfoText {
            eyebrow = DisplayEraYear(node.year) + " 年 · " + CoTechBranchNames[branch], title = node.title, body = cleanDesc,
            status = "效用：" + effectText + "\n研发 " + costPart + (state.Length > 0 ? " · " + state : ""),
            state = HasCoTech(node.key) ? 0 : missing.Length > 0 ? 2 : 1
        };
    }
    void HoldHistoryNode(string key)
    {
        historySelected = key;
        if (!historyInfoPanel || !historyByKey.TryGetValue(key, out var node)) return;
        if (historyTooltipFollow && historyNodeRects.TryGetValue(key, out var target)) historyTooltipFollow.target = target;
        var copy = HistoryNodeInfo(node, "");
        ShowHistoryInfo(copy.eyebrow, copy.title, copy.body, copy.status, HistoryEffectColor(node), copy.state);
    }
    void HoldCoTechNode(string key)
    {
        EnsureCoTechIndexBuilt();
        if (!historyInfoPanel || !coTechByKey.TryGetValue(key, out var node)) return;
        if (historyTooltipFollow && historyNodeRects.TryGetValue(key, out var target)) historyTooltipFollow.target = target;
        var copy = CompanyNodeInfo(node);
        ShowHistoryInfo(copy.eyebrow, copy.title, copy.body, copy.status,
            CoTechBranchColors[Mathf.Clamp(node.branch, 0, CoTechBranchNames.Length - 1)], copy.state);
    }
    void HoldRpRules()
    {
        if (!historyInfoPanel) return;
        if (historyTooltipFollow && rpBadgeRect) historyTooltipFollow.target = rpBadgeRect;
        int connected = DailyConnectedCount();
        int scalePct = Mathf.RoundToInt(ResearchScaleBonusRatio() * 100f);
        string turboLine = turboDaysLeft > 0 ? " · 4×牌照 +50% 生效中" : "";
        string dailyRate = EstimatedDailyRpRate().ToString("0.0");
        string rules =
            "1. 时间积累：营业时每 36 游戏秒固定产出 1 RP；全网每连通 15 站额外提速 +15%（上限 +60%，当前 " + connected + " 站）。\n" +
            "2. 每日扩张补贴：公司页面每日净增 2 站领 +12 RP（看片双倍 +24 RP）。\n" +
            "3. 市政标书与特权：完成招标每次获 +14~+22 RP（专访 ×1.5）；公司「4× 极速牌照」可提速 +50%。";
        ShowHistoryInfo("科研手记 · 获取规则与产能", "科研点（RP）", rules,
            "约 " + dailyRate + " RP / 游戏日 · 规模加成 +" + scalePct + "%" + turboLine,
            new Color(0.65f, 0.50f, 0.28f), 1);
    }
    static float HistoryInfoRequiredHeight(float eyebrow, float title, float body, float status)
    {
        // Top/tape 38, balanced gaps 40, divider 20, bottom/corner flowers 70.
        return 38f + eyebrow + 6f + title + 10f + 20f + 10f + body + 14f + status + 70f;
    }

    void PlaceHistoryInfo(RectTransform rect, float top, float width, float height)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(width, height);
        rect.anchoredPosition = new Vector2(0, -top);
    }

    void ShowHistoryInfo(string eyebrow, string title, string body, string status, Color theme, int state)
    {
        if (!historyInfoPanel) return;
        historyEyebrow.text = CleanHistoryInfoText(DisplayPlaceAliases(eyebrow), true);
        historyEyebrow.color = theme;
        historyHeading.text = CleanHistoryInfoText(DisplayPlaceAliases(title));
        historyInfo.text = CleanHistoryInfoText(DisplayPlaceAliases(body));
        historyStatus.text = CleanHistoryInfoText(DisplayPlaceAliases(status));
        historyStatusFill.color = state == 0 ? new Color(0.78f, 0.87f, 0.74f, 0.48f)
            : state == 1 ? new Color(0.76f, 0.82f, 0.91f, 0.42f) : new Color(0.87f, 0.80f, 0.68f, 0.46f);
        historyInfo.fontSize = ScaledFontSize(28);
        float contentWidth = HistoryInfoWidth - HistoryInfoInset * 2f;
        // Width must be final before asking Text for its wrapped preferred height.
        PlaceHistoryInfo(historyEyebrow.rectTransform, 0, contentWidth, 1);
        PlaceHistoryInfo(historyHeading.rectTransform, 0, contentWidth, 1);
        PlaceHistoryInfo(historyInfo.rectTransform, 0, contentWidth, 1);
        PlaceHistoryInfo(historyStatus.rectTransform, 0, contentWidth - 32f, 1);
        float eyebrowHeight = Mathf.Ceil(historyEyebrow.preferredHeight) + 4f;
        float titleHeight = Mathf.Ceil(historyHeading.preferredHeight) + 4f;
        float statusHeight = Mathf.Ceil(historyStatus.preferredHeight) + 24f;
        float bodyHeight = Mathf.Ceil(historyInfo.preferredHeight) + 6f;
        // Explicit readable floor: never let BestFit shrink body text to a blur.
        while (HistoryInfoRequiredHeight(eyebrowHeight, titleHeight, bodyHeight, statusHeight) > 760f && historyInfo.fontSize > 24)
        {
            historyInfo.fontSize--;
            bodyHeight = Mathf.Ceil(historyInfo.preferredHeight) + 6f;
        }
        float required = HistoryInfoRequiredHeight(eyebrowHeight, titleHeight, bodyHeight, statusHeight);
        // 310–760 is the normal range; unusually long localization must retain all
        // text rather than overlap the footer. Follow scales the whole page to fit.
        float height = Mathf.Max(Mathf.Clamp(required, 310f, 760f), required);
        historyInfoRect.sizeDelta = new Vector2(HistoryInfoWidth, height);
        float y = 38f + Mathf.Max(0f, height - required) * 0.5f;
        PlaceHistoryInfo(historyEyebrow.rectTransform, y, contentWidth, eyebrowHeight); y += eyebrowHeight + 6f;
        PlaceHistoryInfo(historyHeading.rectTransform, y, contentWidth, titleHeight); y += titleHeight + 10f;
        PlaceHistoryInfo(historyDivider, y, 320f, 20f); y += 30f;
        PlaceHistoryInfo(historyInfo.rectTransform, y, contentWidth, bodyHeight); y += bodyHeight + 14f;
        PlaceHistoryInfo(historyStatusRect, y, contentWidth, statusHeight);
        PlaceHistoryInfo(historyStatus.rectTransform, 12f, contentWidth - 32f, statusHeight - 24f);
        // Coordinates and dimensions are final already. Position while invisible,
        // then reveal only this subcanvas without flushing the main graph canvas.
        if (historyTooltipFollow) historyTooltipFollow.RefreshPosition();
        historyInfoGroup.alpha = 1f;
    }

    void BuildHistoryInfoPanel(Transform parent, RectTransform pageRt)
    {
        var info = Rect("HistoryInfo", parent, 0, 0, HistoryInfoWidth, 310);
        // Configure the hidden, always-active island before adding any graphics.
        info.AddComponent<Canvas>();
        var group = info.AddComponent<CanvasGroup>(); group.alpha = 0f;
        group.blocksRaycasts = false; group.interactable = false;
        historyInfoGroup = group;
        Rounded(info, new Color(0.972f, 0.948f, 0.895f, 0.99f)); SoftShadow(info);
        historyInfoRect = info.GetComponent<RectTransform>();
        historyInfoRect.anchorMin = historyInfoRect.anchorMax = new Vector2(0.5f, 0.5f);
        historyInfoRect.pivot = new Vector2(0.5f, 0);
        historyInfoRect.anchoredPosition = Vector2.zero;
        var inner = Rect("HistoryWritingPage", info.transform, 0, 0, 0, 0);
        var innerRt = inner.GetComponent<RectTransform>(); innerRt.anchorMin = Vector2.zero; innerRt.anchorMax = Vector2.one;
        innerRt.offsetMin = new Vector2(18, 18); innerRt.offsetMax = new Vector2(-18, -18);
        Img(inner, new Color(0.990f, 0.978f, 0.948f, 1f)).raycastTarget = false;
        // Four true hairlines, no oversized translucent rounded patch.
        for (int edge = 0; edge < 4; edge++)
        {
            var line = Rect("HistoryPaperRule" + edge, info.transform, 0, 0, 0, 0);
            var rect = line.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(14, 14); rect.offsetMax = new Vector2(-14, -14);
            if (edge < 2) { rect.anchorMin = new Vector2(0, edge); rect.anchorMax = new Vector2(1, edge);
                rect.offsetMin = new Vector2(14, edge == 0 ? 14 : -15.5f); rect.offsetMax = new Vector2(-14, edge == 0 ? 15.5f : -14); }
            else { rect.anchorMin = new Vector2(edge - 2, 0); rect.anchorMax = new Vector2(edge - 2, 1);
                rect.offsetMin = new Vector2(edge == 2 ? 14 : -15.5f, 14); rect.offsetMax = new Vector2(edge == 2 ? 15.5f : -14, -14); }
            Img(line, HistoryInfoGold).raycastTarget = false;
        }
        JournalTape(info.transform, 0, 0, 148f);
        var tape = info.transform.Find("Tape").GetComponent<RectTransform>();
        tape.anchorMin = tape.anchorMax = new Vector2(0.5f, 1f); tape.pivot = new Vector2(0.5f, 0.5f); tape.anchoredPosition = Vector2.zero;
        StampBotanicalSprig(info.transform, "orn_cloud_fret", 22f, 22f, 40f, 40f, 0.22f, false, true);
        StampBotanicalSprig(info.transform, "orn_cloud_fret", HistoryInfoWidth - 62f, 22f, 40f, 40f, 0.22f, true, true);
        float width = HistoryInfoWidth - HistoryInfoInset * 2f;
        historyEyebrow = Txt(info.transform, "", 24, new Color(0.65f, 0.50f, 0.28f), 0, 0, width, 1, TextAnchor.MiddleCenter);
        historyHeading = Txt(info.transform, "", 38, Ink, 0, 0, width, 1, TextAnchor.MiddleCenter);
        var divider = Rect("HistoryJournalDivider", info.transform, 0, 0, 320, 20);
        historyDivider = divider.GetComponent<RectTransform>();
        Img(Rect("RuleLeft", divider.transform, 0, 9, 138, 1.5f), HistoryInfoGold).raycastTarget = false;
        Img(Rect("RuleRight", divider.transform, 182, 9, 138, 1.5f), HistoryInfoGold).raycastTarget = false;
        Txt(divider.transform, "◆", 18, new Color(0.65f, 0.50f, 0.28f, 0.7f), 140, 0, 40, 20, TextAnchor.MiddleCenter);
        historyInfo = Txt(info.transform, "", 28, InkSoft, 0, 0, width, 1, TextAnchor.MiddleCenter);
        historyInfo.lineSpacing = 1.22f;
        var status = Rect("HistoryStatusNote", info.transform, 0, 0, width, 1);
        Rounded(status, Color.clear); historyStatusFill = status.GetComponent<Image>(); historyStatusRect = status.GetComponent<RectTransform>();
        historyStatus = Txt(status.transform, "", 25, InkSoft, 0, 0, width - 32, 1, TextAnchor.MiddleCenter);
        foreach (var text in info.GetComponentsInChildren<Text>(true))
        {
            text.fontStyle = FontStyle.Normal; text.resizeTextForBestFit = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Overflow;
        }
        // The concise, bounded eyebrow is a single line at every font-size tier.
        historyEyebrow.horizontalOverflow = HorizontalWrapMode.Overflow;
        foreach (var graphic in info.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
        historyTooltipFollow = info.AddComponent<HistoryTooltipFollow>(); historyTooltipFollow.page = pageRt; historyTooltipFollow.visibility = group;
        historyInfoPanel = info;
    }

    void HideHistoryInfo() { if (historyInfoGroup) historyInfoGroup.alpha = 0f; }
    void SetHistoryDragging(bool dragging)
    {
        if (historyControls) historyControls.SetActive(!dragging);
        if (dragging) HideHistoryInfo();
    }
    void TapHistoryNode(string key)
    {
        if (!historyByKey.TryGetValue(key, out var node) || techDone.Contains(key)) return;
        if (!HistoryAvailable(node)) { HoldHistoryNode(key); Invoke(nameof(HideHistoryInfo), 2f); return; }
        int cost = HistoryCost(node);
        int rpCost = HistoryRpCost(node);
        if (coins < cost) {
            Toast("研发经费不足：还差 ¥" + FormatShortfall(cost - coins));
            if (historyCoinWidget) historyCoinWidget.Shake();
            return;
        }
        if (rp < rpCost) {
            NotifyOrOfferDiamondSupply("科研点不足：需要 " + rpCost + " RP（还差 " + Mathf.CeilToInt(rpCost - rp) + "，推进运营或接公司标书可获 RP）", true, rpCost - rp);
            return;
        }
        historyAnimated.Clear();
        foreach (var candidate in historyNodes) if (candidate.era == currentTechEra && HistoryVisible(candidate)) historyAnimated.Add(candidate.key);
        BeginUndo("解锁科技");
        coins -= cost; rp -= rpCost; techDone.Add(key); techSessionUnlocked.Add(key);
        lastUnlockedTechEra = node.era;
        if (node.loco >= 0) {
            researchVehicles.Add(new ResearchVehicle { node = node.loco });
            if (node.loco == 0 || node.loco == 4 || node.loco == 8) { diamonds += 50; Toast("飞车里程碑：钻石 +50"); }
            else if (node.loco == 10 || node.loco == 13 || node.loco == 15) { diamonds += 60; Toast("跨时代旗舰飞车：钻石 +60"); }
        }
        historyJustUnlocked = key;
        SyncTerrainTechState();
        InvalidateAutoRouteCache();
        TriggerHaptic(20);
        PlayTechSfx();
        SyncStoryProgression(true);
        RecalcIncome(); EndUndo(); RefreshHUD(); RefreshTechTreeIncremental(false, key);
    }
    int HistoryCalcYear(string key)
    {
        if (historyByKey.TryGetValue(key, out var node) && node.loco >= 0)
            return LocoCalcYears[Mathf.Clamp(node.loco, 0, LocoCalcYears.Length - 1)];
        return HistoryYear(key);
    }
    int HistoryYear(string key)
    {
        if (historyByKey.TryGetValue(key, out var node)) {
            if (node.loco >= 0) return int.Parse(LocoYears[node.loco]);
            if (node.year > 0) return node.year;
        }
        switch (key)
        {
            case "pressure": case "rail": case "piston": case "valve": case "steamengine": case "minehaul": return 1804;
            case "rack": case "adhesion": case "damage": case "coupled": case "twocyl": case "rods": case "walking": case "cylinder": case "firetube": case "pressureloss": return 1815;
            case "dispatch_basic": return 1825;
            case "dispatch_peak": case "fare_policy": case "network_expand1": return 1830;
            case "network_expand2": return 1850;
            case "locoworks": return 1823;
            case "boilerburst": return 1828;
            case "public": case "blast": case "tubes": case "returnflue": case "rebuild": case "rainhill": case "twinreturn": return 1829;
            case "speedrisk": case "guide": case "manufacture": case "leadingtruck": return 1835;
            case "linkmotion": return 1842;
            case "braking": case "feed": case "firebox": return 1855;
            case "ramsvalve": return 1856;
            case "injector": case "giffard": return 1858;
            case "straightbrake": case "directair": case "brakefault": return 1869;
            case "autobrake": case "tripvalve": return 1872;
            case "cylinderdrain": return 1878;
            case "running": case "safetyact": return 1893;
            default: return 1800;
        }
    }
    static int DisplayEraYear(int rawYear)
    {
        if (rawYear >= 2040) return rawYear;
        if (rawYear < 1900) return 2040 + Mathf.Clamp(Mathf.RoundToInt((rawYear - 1800f) / 99f * 59f), 0, 59);
        if (rawYear < 2000) return 2100 + Mathf.Clamp(Mathf.RoundToInt((rawYear - 1900f) / 99f * 59f), 0, 59);
        return 2160 + Mathf.Clamp(Mathf.RoundToInt((rawYear - 2000f) / 35f * 60f), 0, 60);
    }
    string HistoryPeriod(HistoryNode node)
    {
        int year = HistoryYear(node.key);
        if (node.loco >= 0) return DisplayEraYear(year) + " 年";
        if (node.era == 1) return DisplayEraYear(year) + " 年 · 聚变能量";
        if (node.era == 2) return DisplayEraYear(year) + " 年 · 高速智行";
        switch (node.key)
        {
            case "boilerburst": return "2060 年";
            case "walking": return "2052 年";
            case "rainhill": return "2060 年";
            case "locoworks": return "2057 年";
            case "linkmotion": return "2068 年";
            case "ramsvalve": return "2077 年";
            case "injector": case "giffard": return "2078 年";
            case "straightbrake": case "directair": return "2085 年";
            case "autobrake": case "tripvalve": return "2085 年代";
            case "cylinderdrain": return "2090 年";
            case "safetyact": return "2099 年";
            default: return (DisplayEraYear(year) / 10 * 10) + " 年代前后";
        }
    }
    int HistoryEra(string key) => TreeRingIndex(HistoryCalcYear(key), historyByKey.TryGetValue(key, out var node) ? node.era : 0);

    static int TreeRingIndex(int year, int eraGroup)
    {
        if (eraGroup == 1)
        {
            if (year < 1920) return 0;
            if (year < 1935) return 1;
            if (year < 1950) return 2;
            if (year < 1970) return 3;
            if (year < 1985) return 4;
            return 5;
        }
        if (eraGroup == 2)
        {
            if (year < 2006) return 0;
            if (year < 2010) return 1;
            if (year < 2015) return 2;
            if (year < 2018) return 3;
            if (year < 2022) return 4;
            return 5;
        }
        if (year < 1810) return 0;
        if (year < 1820) return 1;
        if (year < 1830) return 2;
        if (year < 1850) return 3;
        if (year < 1870) return 4;
        return 5;
    }
    Vector2 HistoryPosition(string key) => ResolveTreePosition(key, historyPositions, key => historyByKey.TryGetValue(key, out var n) ? n.parents : null, key => historyByKey.TryGetValue(key, out var n) ? historyNodes.IndexOf(n) : 0);

    Vector2 CoTechPosition(string key)
    {
        EnsureCoTechIndexBuilt();
        return ResolveTreePosition(key, coTechPositions,
            k => coTechByKey.TryGetValue(k, out var node) ? node.parents : null,
            k => Array.FindIndex(AllCompanyTechNodes, node => node.key == k));
    }

    Vector2 ResolveTreePosition(string key, Dictionary<string, Vector2> posMap, Func<string, string[]> getParents, Func<string, int> getIndex)
    {
        if (posMap.TryGetValue(key, out var position)) return position;
        var parents = getParents(key);
        if (parents == null || parents.Length == 0) return posMap[key] = Vector2.zero;
        Vector2 origin = Vector2.zero; int count = 0;
        foreach (string parent in parents)
            if (posMap.TryGetValue(parent, out var p)) { origin += p; count++; }
        if (count > 0) origin /= count;
        int index = Mathf.Max(0, getIndex(key));
        float angle = index * 2.399963f;
        return posMap[key] = origin + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 160f;
    }

    void TickResearch(float dt) { }
    sealed class HistoryCardHandle
    {
        public GameObject card; public Image marker; public Text title, label; public RawImage art;
        public Image shadow, body; public bool done, ready;
    }
    Action<string> refreshHistoryTree;
    GameObject incrementalHistoryPage;
    bool incrementalCorporate;
    static readonly Dictionary<string, Texture2D> researchTrainTextures = new Dictionary<string, Texture2D>();
    static Texture2D ResearchTrainTexture(string name)
    {
        if (!researchTrainTextures.TryGetValue(name, out var texture))
            researchTrainTextures[name] = texture = Resources.Load<Texture2D>("Research/Train/" + name);
        return texture;
    }
    void RefreshTechTreeIncremental(bool isCorporate, string unlockedKey)
    {
        if (!incrementalHistoryPage || pageRoot != incrementalHistoryPage || incrementalCorporate != isCorporate || refreshHistoryTree == null) return;
        refreshHistoryTree(unlockedKey);
    }
    void UpdateHistoryCard(HistoryCardHandle h, Color categoryColor, bool done, bool ready, string badgeSub)
    {
        bool locked = !done && !ready;
        Color bg = done ? Color.Lerp(categoryColor, Color.white, 0.60f) : locked ? new Color(1f, 1f, 1f, 0.45f) : new Color(0.996f, 0.992f, 0.976f);
        if (ready && !h.shadow)
        {
            var shadow = Rect("Card3DBase", h.card.transform, -6, -7, 200, 120);
            Rounded(shadow, new Color(categoryColor.r * 0.45f, categoryColor.g * 0.45f, categoryColor.b * 0.52f, 0.38f));
            h.shadow = shadow.GetComponent<Image>(); h.shadow.raycastTarget = false; shadow.transform.SetSiblingIndex(0);
            var body = Rect("CardBody", h.card.transform, 0, 0, 200, 120); Rounded(body, bg);
            h.body = body.GetComponent<Image>(); h.body.raycastTarget = false; body.transform.SetSiblingIndex(1);
        }
        if (h.shadow) h.shadow.gameObject.SetActive(ready);
        if (h.body) { h.body.gameObject.SetActive(ready); h.body.color = bg; }
        h.card.GetComponent<Image>().color = ready ? new Color(1f, 1f, 1f, 0.01f) : bg;
        h.marker.color = locked ? new Color(categoryColor.r, categoryColor.g, categoryColor.b, 0.35f) : categoryColor;
        h.title.color = locked ? new Color(Ink.r, Ink.g, Ink.b, 0.48f) : Ink;
        h.title.resizeTextMaxSize = ScaledFontSize(ready ? 25 : 23, 1.24f); h.title.fontSize = ScaledFontSize(h.title.resizeTextMaxSize);
        h.label.color = locked ? new Color(InkSoft.r, InkSoft.g, InkSoft.b, 0.45f) : InkSoft;
        h.label.text = badgeSub + (done ? " · 已解锁" : ready ? "" : " · 前置不足");
        if (h.art) h.art.color = locked ? new Color(1f, 1f, 1f, 0.35f) : Color.white;
        h.done = done; h.ready = ready;
    }
    void RevealHistoryCard(GameObject card, bool newlyVisible)
    {
        var previous = card.GetComponent<HistoryNodeReveal>();
        if (previous) previous.FinishImmediately();
        var fx = card.AddComponent<HistoryNodeReveal>(); fx.startDelay = newlyVisible ? 0.18f : 0f;
        var halo = Rect("UnlockHalo", card.transform, -30, -70, 260, 260);
        var haloImage = Img(halo, new Color(0.87f, 0.65f, 0.32f, 0.85f), historyCircleOutline); haloImage.raycastTarget = false;
        halo.transform.SetAsFirstSibling(); fx.halo = halo.GetComponent<RectTransform>();
        fx.halo.pivot = new Vector2(0.5f, 0.5f); fx.halo.anchoredPosition = new Vector2(100, 60);
        int sparkCount = qualityTier >= 2 ? 20 : qualityTier == 1 ? 12 : 8;
        for (int sp = 0; sp < sparkCount; sp++)
        {
            var spark = Rect("Spark", card.transform, 96, 56, 9, 9);
            Img(spark, sp % 2 == 0 ? Indigo : Gold, spriteCircle).raycastTarget = false;
            fx.sparks.Add(spark.GetComponent<RectTransform>());
        }
    }
    static void ResearchSubCanvas(GameObject layer, bool interactive = false)
    {
        var subCanvas = layer.AddComponent<Canvas>(); subCanvas.overrideSorting = false;
        if (interactive) layer.AddComponent<GraphicRaycaster>();
    }
    GameObject ResearchGraphLayer(Transform viewport, RectTransform source, string name, bool interactive)
    {
        // Keep the sub-canvas origin identical to the root canvas. Moving an
        // 8000px canvas origin changes GPU rounding at clipped, translucent edges.
        var layer = Rect(name, viewport, 0, 0, 0, 0);
        var frame = layer.GetComponent<RectTransform>(); frame.anchorMin = Vector2.zero; frame.anchorMax = Vector2.one;
        frame.pivot = new Vector2(0.5f, 0.5f); frame.offsetMin = frame.offsetMax = Vector2.zero;
        ResearchSubCanvas(layer, interactive);
        var proxy = Rect(name + "Content", layer.transform, 0, 0, source.sizeDelta.x, source.sizeDelta.y);
        var follower = layer.AddComponent<HistoryGraphLayerFollow>(); follower.source = source; follower.target = proxy.GetComponent<RectTransform>(); follower.Refresh();
        return proxy;
    }
    void OpenTechScreen(int mode = 0) => OpenConcentricTechScreen(false, mode);

    void OpenConcentricTechScreen(bool isCorporate, int mode = 0)
    {
        techTreeTab = isCorporate ? 1 : 0;
        if (mode == 0)
        {
            currentTechEra = DefaultOpenTechEra(isCorporate);
            if (isCorporate) { coTechCollapseDone = true; coTechSessionUnlocked.Clear(); }
            else { techCollapseDone = true; techSessionUnlocked.Clear(); }
        }
        InitHistoryTree();
        EnsureCompanyTechMigrated();
        if (!IsTechEraUnlocked(currentTechEra)) currentTechEra = LatestUnlockedTechEra();
        currentTechEra = Mathf.Clamp(currentTechEra, 0, 2);
        int activeEra = currentTechEra;
        int eraTotalCount = isCorporate ? CoTechEraNodeCount(activeEra) : EraTechNodeCount(activeEra);
        int eraDoneCount = isCorporate ? CoTechEraDoneCount(activeEra) : EraTechDoneCount(activeEra);

        ClosePage(false); pageBack = null; CloseLineBubble(); eraJournalModal = null;
        var page = Rect("HistoryFullScreen", canvas.transform, 0, 0, 0, 0);
        var pageRt = page.GetComponent<RectTransform>(); pageRt.anchorMin = Vector2.zero; pageRt.anchorMax = Vector2.one; pageRt.offsetMin = pageRt.offsetMax = Vector2.zero;
        Img(page, new Color(0.972f, 0.955f, 0.922f)).raycastTarget = false;
        pageRoot = page; historyPage = page;

        var farLayer = Rect("FarParallaxLayer", page.transform, 0, 0, 0, 0);
        ResearchSubCanvas(farLayer);
        var farRt = farLayer.GetComponent<RectTransform>(); farRt.anchorMin = Vector2.zero; farRt.anchorMax = Vector2.one; farRt.offsetMin = farRt.offsetMax = Vector2.zero;
        if (!historyCircleOutline) historyCircleOutline = MakeRing(512, 2f);
        if (!historyHubDiscSprite) historyHubDiscSprite = MakeSmoothCircle(256);
        for (int rIdx = 0; rIdx < 3; rIdx++)
        {
            float dr = 420f + rIdx * 360f;
            var deepRing = Rect("DeepCompass_" + rIdx, farLayer.transform, 0, 0, dr * 2f, dr * 2f);
            var drt = deepRing.GetComponent<RectTransform>(); drt.anchorMin = drt.anchorMax = new Vector2(0.5f, 0.5f); drt.pivot = new Vector2(0.5f, 0.5f); drt.anchoredPosition = Vector2.zero;
            Img(deepRing, new Color(0.24f, 0.34f, 0.48f, 0.075f - rIdx * 0.015f), historyCircleOutline).raycastTarget = false;
        }
        var crossH = Rect("CompassAxisH", farLayer.transform, 0, 0, 2600, 2);
        var chRt = crossH.GetComponent<RectTransform>(); chRt.anchorMin = chRt.anchorMax = new Vector2(0.5f, 0.5f); chRt.pivot = new Vector2(0.5f, 0.5f);
        Img(crossH, new Color(0.42f, 0.35f, 0.27f, 0.09f)).raycastTarget = false;
        var crossV = Rect("CompassAxisV", farLayer.transform, 0, 0, 2, 2600);
        var cvRt = crossV.GetComponent<RectTransform>(); cvRt.anchorMin = cvRt.anchorMax = new Vector2(0.5f, 0.5f); cvRt.pivot = new Vector2(0.5f, 0.5f);
        Img(crossV, new Color(0.42f, 0.35f, 0.27f, 0.09f)).raycastTarget = false;

        var view = Rect("HistoryViewport", page.transform, 0, 0, 0, 0);
        var viewRt = view.GetComponent<RectTransform>(); viewRt.anchorMin = Vector2.zero; viewRt.anchorMax = Vector2.one;
        viewRt.offsetMin = Vector2.zero; viewRt.offsetMax = Vector2.zero;
        Img(view, new Color(0.972f, 0.955f, 0.922f, 0.01f));
        var mask = view.AddComponent<Mask>(); mask.showMaskGraphic = false;
        var scroll = view.AddComponent<ScrollRect>(); scroll.horizontal = true; scroll.vertical = true; scroll.scrollSensitivity = 0; scroll.movementType = ScrollRect.MovementType.Clamped; scroll.viewport = viewRt;
        const float cw = 8000, ch = 8000;
        var content = Rect("HistoryTree", view.transform, 0, 0, cw, ch);
        var rt = content.GetComponent<RectTransform>(); rt.anchorMin = rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0, 1); rt.anchoredPosition = Vector2.zero; scroll.content = rt;
        rt.localScale = Vector3.one * historyZoom;
        scroll.horizontalNormalizedPosition = historyScrollX;
        scroll.verticalNormalizedPosition = historyScrollY;
        scroll.onValueChanged.AddListener(v => {
            historyScrollX = v.x; historyScrollY = v.y;
        });
        var zoom = view.AddComponent<HistoryGraphZoom>(); zoom.viewport = viewRt; zoom.content = rt; zoom.scroll = scroll;
        zoom.scale = historyZoom;
        zoom.onScaleChanged = value => historyZoom = value;
        zoom.onPinch = SetHistoryDragging;
        var drag = view.AddComponent<HistoryDragFeedback>(); drag.callback = SetHistoryDragging;
        var parallax = view.AddComponent<HistoryParallaxLayer>(); parallax.content = rt; parallax.target = farRt;

        string[] eras = activeEra == 0
            ? (isCorporate
                ? new[] { "2040s · 契约立社", "2048s · 星航初创", "2055s · 月面矿权", "2062s · 股权募资", "2070s · 地月联营", "2085s · 治理与风控" }
                : new[] { "2040s · 核热萌芽", "2050s · 离子推进", "2055s · 月面矿轨", "2060s · 地月试航", "2070s · 地月公航", "2085s · 核热定型" })
            : activeEra == 1
                ? (isCorporate
                    ? new[] { "2100s · 科层与章程", "2110s · 托拉斯融资", "2125s · 磁轨集团化", "2135s · 三域联运网", "2145s · 星港商会", "2155s · 现代治理" }
                    : new[] { "2100s · 聚变初燃", "2110s · 等离子增压", "2125s · 磁轨流线", "2135s · 超导能量网", "2145s · 火星跃迁", "2155s · 主干航线提速" })
                : (isCorporate
                    ? new[] { "2160s · 星穹协议", "2170s · 智能调度总控", "2180s · 三网融合星港", "2190s · 跨星域航运公司", "2200s+ · 星域联邦", "2210s+ · 全域治理" }
                    : new[] { "2160s · 曲速破壁", "2170s · 信标星控", "2180s · 超导磁浮道床", "2190s · 引力弹弓航道", "2200s+ · 星域闭环", "2210s+ · 三域星轨圈" });
        var ringLayer = Rect("HistoryRings", content.transform, 0, 0, cw, ch); ResearchSubCanvas(ringLayer);
        for (int i = 5; i >= 0; i--)
        {
            float radius = 270 + i * 230;
            var circle = Rect("EraCircle" + i, ringLayer.transform, 4000 - radius, 4000 - radius, radius * 2, radius * 2);
            Img(circle, new Color(0.58f, 0.45f, 0.30f, 0.20f), historyCircleOutline).raycastTarget = false;
            var outerHair = Rect("EraCircleHair" + i, ringLayer.transform, 4000 - (radius + 6), 4000 - (radius + 6), (radius + 6) * 2, (radius + 6) * 2);
            Img(outerHair, new Color(0.58f, 0.45f, 0.30f, 0.08f), historyCircleOutline).raycastTarget = false;
            var eraLbl = Txt(ringLayer.transform, "◆  " + eras[i], 25, new Color(0.42f, 0.33f, 0.23f, 0.92f), 4012, 4000 + radius + 12, 430, 38, TextAnchor.MiddleLeft);
            eraLbl.fontStyle = FontStyle.Normal; eraLbl.raycastTarget = false;
        }
        var edgeContent = ResearchGraphLayer(view.transform, rt, "HistoryEdges", false);
        var edgeGo = Rect("HistoryEdgeGeometry", edgeContent.transform, 0, 0, cw, ch);
        var nodeLayer = ResearchGraphLayer(view.transform, rt, "HistoryNodes", true);
        var edges = edgeGo.AddComponent<HistoryGraphEdges>(); edges.lineSprite = spriteWhite;
        var posMap = isCorporate ? coTechPositions : historyPositions;
        var layout = content.AddComponent<HistoryGraphLayout>();
        layout.positions = posMap; layout.edges = edges;
        layout.updatesPerSecond = 120f;
        var graphRects = new Dictionary<string, RectTransform>();
        var graphParents = new Dictionary<string, string[]>();
        var cards = new Dictionary<string, HistoryCardHandle>();
        var orderedKeys = new List<string>();
        historyNodeRects.Clear();

        bool collapseFlag = isCorporate ? coTechCollapseDone : techCollapseDone;
        var sessionSet = isCorporate ? coTechSessionUnlocked : techSessionUnlocked;
        Func<string, bool> isDoneFn = k => isCorporate ? companyTechDone.Contains(k) : techDone.Contains(k);
        Func<string, bool> isInHub = k => collapseFlag && isDoneFn(k) && !sessionSet.Contains(k);

        int hubCollapsedCount = 0;
        if (collapseFlag)
        {
            if (isCorporate)
            {
                var allCo = AllCompanyTechNodes;
                for (int i = 0; i < allCo.Length; i++)
                    if (allCo[i].eraGroup == activeEra && isInHub(allCo[i].key)) hubCollapsedCount++;
            }
            else
            {
                foreach (var node in historyNodes)
                    if (node.era == activeEra && isInHub(node.key)) hubCollapsedCount++;
            }
        }
        bool collapsed = collapseFlag && hubCollapsedCount > 0;
        const string HubKey = "__completed_hub__";
        GameObject hubGoForRefresh = null; Text hubSessionText = null;
        if (collapsed)
        {
            posMap[HubKey] = Vector2.zero;
            const float hubSize = 370f;
            var hubGo = Rect("CompletedHub", nodeLayer.transform, 4000f - hubSize * 0.5f, 4000f - hubSize * 0.5f, hubSize, hubSize);
            hubGoForRefresh = hubGo;
            var hubRt = hubGo.GetComponent<RectTransform>();
            graphRects[HubKey] = hubRt;
            layout.Add(HubKey, hubRt, -1, Array.Empty<string>());

            var hub3dShadow = Rect("Hub3DBase", hubGo.transform, -10, -12, hubSize + 4, hubSize + 4);
            Img(hub3dShadow, new Color(0.14f, 0.17f, 0.26f, 0.34f), historyHubDiscSprite).raycastTarget = false;
            var outerDisc = Rect("HubOuter", hubGo.transform, 0, 0, hubSize, hubSize);
            Img(outerDisc, new Color(0.76f, 0.56f, 0.25f, 0.98f), historyHubDiscSprite).raycastTarget = false;
            var midDisc = Rect("HubMid", hubGo.transform, 10, 10, hubSize - 20, hubSize - 20);
            Img(midDisc, new Color(0.27f, 0.22f, 0.17f, 0.95f), historyHubDiscSprite).raycastTarget = false;
            var innerDisc = Rect("HubInner", hubGo.transform, 18, 18, hubSize - 36, hubSize - 36);
            Img(innerDisc, new Color(0.985f, 0.968f, 0.928f), historyHubDiscSprite).raycastTarget = true;

            string eraRoman = activeEra == 0 ? "— 核热纪 —" : activeEra == 1 ? "— 聚变纪 —" : "— 曲速纪 —";
            string subLabel = sessionSet.Count > 0 ? "本次新解 " + sessionSet.Count + " 项外留" : eraRoman;
            var hubSub = Txt(hubGo.transform, subLabel, 21, new Color(0.56f, 0.42f, 0.22f), 24, 256, hubSize - 48, 34, TextAnchor.MiddleCenter);
            hubSessionText = hubSub;
            hubSub.fontStyle = FontStyle.Normal; hubSub.raycastTarget = false;
            var hubTitle = Txt(hubGo.transform, isCorporate ? "已研商道图谱" : "已研典藏图谱", 32, Ink, 24, 208, hubSize - 48, 46, TextAnchor.MiddleCenter);
            hubTitle.fontStyle = FontStyle.Normal; hubTitle.raycastTarget = false;
            var hubCount = Txt(hubGo.transform, hubCollapsedCount + " / " + eraTotalCount, 48, new Color(0.74f, 0.48f, 0.16f), 24, 136, hubSize - 48, 64, TextAnchor.MiddleCenter);
            hubCount.fontStyle = FontStyle.Normal; hubCount.raycastTarget = false;
            bool allDone = hubCollapsedCount >= eraTotalCount;
            var hubHintBg = Rect("HubHintChip", hubGo.transform, 46, 64, hubSize - 92, 44);
            Rounded(hubHintBg, new Color(0.24f, 0.34f, 0.48f, 0.12f));
            hubHintBg.GetComponent<Image>().raycastTarget = false;
            var hubHint = Txt(hubHintBg.transform, allDone ? "全卷圆满 · 点击检视" : "点击展开全部节点 ▸", 22, Indigo, 6, 0, hubSize - 104, 44, TextAnchor.MiddleCenter);
            FitSingleLine(hubHint, 18);
            hubHint.fontStyle = FontStyle.Normal; hubHint.raycastTarget = false;

            var hubBtn = hubGo.AddComponent<Button>();
            hubBtn.targetGraphic = innerDisc.GetComponent<Image>();
            hubBtn.onClick.AddListener(() => {
                if (isCorporate) { coTechCollapseDone = false; OpenConcentricTechScreen(true, 1); }
                else { techCollapseDone = false; OpenConcentricTechScreen(false, 1); }
            });
        }

        string eraRootKey = isCorporate ? "co_charter_1801" : EraRootKey(activeEra);
        string justUnlockedKey = isCorporate ? coTechJustUnlocked : historyJustUnlocked;

        Action<string, string, int, string[], Color, bool, bool, int, string, Action, Action> spawnNodeCard =
            (nodeKey, nodeTitle, ringIdx, rawParents, categoryColor, done, ready, locoIdx, badgeSub, onTap, onHold) =>
        {
            orderedKeys.Add(nodeKey);
            if (cards.TryGetValue(nodeKey, out var existing))
            {
                Vector2 currentPoint = isCorporate ? CoTechPosition(nodeKey) : HistoryPosition(nodeKey);
                var existingRect = (RectTransform)existing.card.transform;
                Vector2 freshCorner = new Vector2(4000 + currentPoint.x - 100, 4000 + currentPoint.y - 60);
                var priorReveal = existing.card.GetComponent<HistoryNodeReveal>();
                if (priorReveal) priorReveal.FinishImmediately();
                existingRect.pivot = Vector2.zero;
                existingRect.anchoredPosition = freshCorner;
                bool becameReady = ready && !existing.ready;
                if (existing.done != done || existing.ready != ready) UpdateHistoryCard(existing, categoryColor, done, ready, badgeSub);
                if (justUnlockedKey == nodeKey || becameReady) RevealHistoryCard(existing.card, false);
                return;
            }
            Vector2 point = isCorporate ? CoTechPosition(nodeKey) : HistoryPosition(nodeKey);
            var card = Rect(nodeKey, nodeLayer.transform, 4000 + point.x - 100, 4000 + point.y - 60, 200, 120);
            var cardRect = card.GetComponent<RectTransform>();
            graphRects[nodeKey] = cardRect;
            graphParents[nodeKey] = rawParents ?? Array.Empty<string>();
            historyNodeRects[nodeKey] = cardRect;

            string[] resolvedParents = rawParents ?? Array.Empty<string>();
            if (collapsed && resolvedParents.Length > 0)
            {
                var mapped = new List<string>();
                bool linkedHub = false;
                foreach (string p in resolvedParents)
                {
                    if (isInHub(p)) { if (!linkedHub) { mapped.Add(HubKey); linkedHub = true; } }
                    else mapped.Add(p);
                }
                resolvedParents = mapped.ToArray();
            }
            layout.Add(nodeKey, cardRect, nodeKey == eraRootKey ? -1 : ringIdx, resolvedParents);

            bool locked = !done && !ready;
            Color cardBg = done ? Color.Lerp(categoryColor, Color.white, 0.60f)
                         : locked ? new Color(1f, 1f, 1f, 0.45f)
                         : new Color(0.996f, 0.992f, 0.976f);
            if (ready)
            {
                var card3dBase = Rect("Card3DBase", card.transform, -6, -7, 200, 120);
                Rounded(card3dBase, new Color(categoryColor.r * 0.45f, categoryColor.g * 0.45f, categoryColor.b * 0.52f, 0.38f));
                card3dBase.GetComponent<Image>().raycastTarget = false;
                var cardBody = Rect("CardBody", card.transform, 0, 0, 200, 120);
                Rounded(cardBody, cardBg);
                cardBody.GetComponent<Image>().raycastTarget = false;
            }
            Rounded(card, ready ? new Color(1f, 1f, 1f, 0.01f) : cardBg);

            var marker = Rect("Category", card.transform, 8, 90, 17, 17);
            Color dotColor = locked ? new Color(categoryColor.r, categoryColor.g, categoryColor.b, 0.35f) : categoryColor;
            Img(marker, dotColor, spriteCircle).raycastTarget = false;

            Color nameColor = locked ? new Color(Ink.r, Ink.g, Ink.b, 0.48f) : Ink;
            int nameMaxSize = ScaledFontSize(ready ? 25 : 23, 1.24f);
            var name = Txt(card.transform, nodeTitle, nameMaxSize, nameColor, 6, locoIdx >= 0 ? 83 : 62, 188, locoIdx >= 0 ? 32 : 48, TextAnchor.MiddleCenter);
            name.fontStyle = FontStyle.Normal;
            name.resizeTextForBestFit = true; name.resizeTextMinSize = 14; name.resizeTextMaxSize = nameMaxSize; name.horizontalOverflow = HorizontalWrapMode.Wrap; name.verticalOverflow = VerticalWrapMode.Truncate;

            if (locoIdx >= 0)
            {
                var art = Rect("Locomotive", card.transform, 12, 36, 176, 46).AddComponent<RawImage>();
                art.raycastTarget = false;
                var artTexture = ResearchTrainTexture(LocoArt[locoIdx]);
                if (!artTexture)
                {
                    // 飞车美术资源已下线：隐藏图片节点，卡片以纯色底显示，避免空白贴图报错或白块。
                    art.gameObject.SetActive(false);
                }
                else
                {
                    art.texture = artTexture;
                    if (locked) art.color = new Color(1f, 1f, 1f, 0.35f);
                    float ratio = (float)artTexture.width / artTexture.height;
                    float iw = Mathf.Min(176, 46 * ratio), ih = iw / ratio;
                    art.rectTransform.sizeDelta = new Vector2(iw, ih);
                    art.rectTransform.anchoredPosition = new Vector2((200 - iw) / 2, 36 + (46 - ih) / 2);
                }
            }

            Color labelColor = locked ? new Color(InkSoft.r, InkSoft.g, InkSoft.b, 0.45f) : InkSoft;
            var labelText = Txt(card.transform, badgeSub + (done ? " · 已解锁" : ready ? "" : " · 前置不足"), 20, labelColor, 6, 18, 188, 34, TextAnchor.MiddleCenter);
            labelText.resizeTextForBestFit = true; labelText.resizeTextMinSize = 15; labelText.resizeTextMaxSize = ScaledFontSize(20, 1.24f); labelText.horizontalOverflow = HorizontalWrapMode.Overflow; labelText.verticalOverflow = VerticalWrapMode.Truncate;

            var touch = card.AddComponent<HistoryNodeTouch>();
            touch.requireDoubleTap = true;
            touch.onTap = onTap; touch.onHold = onHold; touch.onRelease = HideHistoryInfo;

            cards[nodeKey] = new HistoryCardHandle { card=card, marker=marker.GetComponent<Image>(), title=name, label=labelText,
                art=card.GetComponentInChildren<RawImage>(), shadow=card.transform.Find("Card3DBase")?.GetComponent<Image>(),
                body=card.transform.Find("CardBody")?.GetComponent<Image>(), done=done, ready=ready };
            bool newlyVisible = justUnlockedKey != null && !historyAnimated.Contains(nodeKey);
            if (newlyVisible || justUnlockedKey == nodeKey) RevealHistoryCard(card, newlyVisible);
        };

        Action refreshMountedNodes = () => {
        int prevCount = orderedKeys.Count;
        bool hadAny = prevCount > 0;
        orderedKeys.Clear();
        if (isCorporate)
        {
            var allCo = AllCompanyTechNodes;
            for (int i = 0; i < allCo.Length; i++)
            {
                var cnode = allCo[i];
                if (cnode.eraGroup != activeEra || !IsCoTechVisible(cnode) || isInHub(cnode.key)) continue;
                bool done = HasCoTech(cnode.key);
                bool ready = CanUnlockCoTech(cnode);
                Color bCol = CoTechBranchColors[Mathf.Clamp(cnode.branch, 0, CoTechBranchColors.Length - 1)];
                var capturedNode = cnode;
                string ckey = cnode.key;
                string subBadge = DisplayEraYear(cnode.year) + " · " + CoTechBranchNames[Mathf.Clamp(cnode.branch, 0, CoTechBranchNames.Length - 1)];
                spawnNodeCard(ckey, cnode.title, CoTechRingIndex(cnode), cnode.parents, bCol, done, ready, -1, subBadge,
                    () => TapCompanyTechNode(capturedNode), () => HoldCoTechNode(ckey));
            }

        }
        else
        {
            foreach (var node in historyNodes)
            {
                if (node.era != activeEra || !HistoryVisible(node) || isInHub(node.key)) continue;
                bool done = techDone.Contains(node.key), ready = HistoryAvailable(node);
                string key = node.key;
                string label = node.loco >= 0 ? "飞车" : node.effect == ResearchEffect.None ? "运营解锁" : HistoryCardShortLabel(node);
                spawnNodeCard(key, node.name, HistoryEra(key), node.parents, HistoryEffectColor(node), done, ready, node.loco, DisplayEraYear(HistoryYear(key)) + " · " + label,
                    () => TapHistoryNode(key), () => HoldHistoryNode(key));
            }

        }

        for (int index = 0; index < orderedKeys.Count; index++)
        {
            var cardRect = graphRects[orderedKeys[index]]; int sibling = index + (collapsed ? 1 : 0);
            if (cardRect.GetSiblingIndex() != sibling) cardRect.SetSiblingIndex(sibling);
        }
        edges.BeginOrder();
        foreach (string childKey in orderedKeys)
        {
            var item = new KeyValuePair<string, string[]>(childKey, graphParents[childKey]);
            var child = graphRects[item.Key];
            bool linkedHub = false;
            foreach (string parent in item.Value)
            {
                if (isInHub(parent))
                {
                    if (!linkedHub && graphRects.TryGetValue(HubKey, out var hubRect)) { edges.AddOrUpdate(HubKey, item.Key, hubRect, child, true); linkedHub = true; }
                }
                else if (graphRects.TryGetValue(parent, out var parentRect)) edges.AddOrUpdate(parent, item.Key, parentRect, child, isDoneFn(parent));
            }
        }
        layout.SetOrder(orderedKeys);
        if (!hadAny) { layout.Wake(true); layout.FinishInitialMount(); }
        edges.Refresh();
        if (qualityTier >= 1 && justUnlockedKey != null) {
            var previousShake = page.GetComponent<HistoryCameraShake>();
            if (previousShake) { previousShake.enabled = false; Destroy(previousShake); }
            var shake = page.AddComponent<HistoryCameraShake>(); shake.target = rt;
        }
        if (isCorporate) coTechJustUnlocked = null; else historyJustUnlocked = null;
        };
        refreshMountedNodes();


        Color matteEdge = new Color(0.14f, 0.13f, 0.16f, 0.88f);
        Color brassLine = new Color(0.76f, 0.58f, 0.30f, 0.72f);
        var frameLayer = Rect("DepthGalleryFrame", page.transform, 0, 0, 0, 0);
        var frt = frameLayer.GetComponent<RectTransform>(); frt.anchorMin = Vector2.zero; frt.anchorMax = Vector2.one; frt.offsetMin = frt.offsetMax = Vector2.zero;
        Action<string, Vector2, Vector2, Vector2, Vector2> addBar = (bName, aMin, aMax, oMin, oMax) => {
            var g = Rect(bName, frameLayer.transform, 0, 0, 0, 0);
            var gr = g.GetComponent<RectTransform>(); gr.anchorMin = aMin; gr.anchorMax = aMax; gr.offsetMin = oMin; gr.offsetMax = oMax;
            Img(g, matteEdge).raycastTarget = false;
        };
        addBar("MatteLeft", new Vector2(0,0), new Vector2(0,1), new Vector2(0,0), new Vector2(10,0));
        addBar("MatteRight", new Vector2(1,0), new Vector2(1,1), new Vector2(-10,0), new Vector2(0,0));
        addBar("MatteTop", new Vector2(0,1), new Vector2(1,1), new Vector2(0,-10), new Vector2(0,0));
        addBar("MatteBot", new Vector2(0,0), new Vector2(1,0), new Vector2(0,0), new Vector2(0,10));
        var innerRim = Rect("BrassRim", frameLayer.transform, 0, 0, 0, 0);
        var irim = innerRim.GetComponent<RectTransform>(); irim.anchorMin = Vector2.zero; irim.anchorMax = Vector2.one; irim.offsetMin = new Vector2(10,10); irim.offsetMax = new Vector2(-10,-10);
        var rimImg = Img(innerRim, new Color(1,1,1,0)); rimImg.raycastTarget = false;
        var outRim = innerRim.AddComponent<Outline>(); outRim.effectColor = brassLine; outRim.effectDistance = new Vector2(2f, -2f);

        historyControls = Rect("HistoryControls", page.transform, 0, 0, 0, 0);
        ResearchSubCanvas(historyControls, true);
        var controlsRt = historyControls.GetComponent<RectTransform>(); controlsRt.anchorMin = Vector2.zero; controlsRt.anchorMax = Vector2.one; controlsRt.offsetMin = controlsRt.offsetMax = Vector2.zero;
        var back = Btn(historyControls.transform, "Back", "←", 40, 24, 0, 94, 94, new Color(1,1,1,0.95f), GoBackPage); TInk(back, Ink); SoftShadow(back.gameObject);
        var backRt = back.GetComponent<RectTransform>(); backRt.anchorMin = backRt.anchorMax = new Vector2(0,1); backRt.pivot = new Vector2(0,1); backRt.anchoredPosition = new Vector2(24,-28);
        var home = Btn(historyControls.transform, "Home", "主页", 24, 0, 0, 104, 94, new Color(1,1,1,0.95f), GoHome); TInk(home, Ink); SoftShadow(home.gameObject);
        var homeRt = home.GetComponent<RectTransform>(); homeRt.anchorMin = homeRt.anchorMax = new Vector2(0,1); homeRt.pivot = new Vector2(0,1); homeRt.anchoredPosition = new Vector2(134,-28);
        var titleCard = Rect("TechTitleBadge", historyControls.transform, 0, 0, 380, 60);
        var titleCardBg = Img(titleCard, new Color(1f, 0.99f, 0.96f, 0.96f), spriteRound);
        titleCardBg.type = Image.Type.Sliced; titleCardBg.raycastTarget = false;
        SoftShadow(titleCard);
        var titleRt = titleCard.GetComponent<RectTransform>(); titleRt.anchorMin = titleRt.anchorMax = new Vector2(0,1); titleRt.pivot = new Vector2(0,1); titleRt.anchoredPosition = new Vector2(254,-45);
        var title = Txt(titleCard.transform, isCorporate ? "星航治理 · 发展树" : "星轨典籍 · 科技树", 34, Ink, 0, 0, 0, 0, TextAnchor.MiddleLeft);
        title.fontStyle = FontStyle.Normal; title.raycastTarget = false;
        title.resizeTextForBestFit = true; title.resizeTextMinSize = 18; title.resizeTextMaxSize = ScaledFontSize(34); title.horizontalOverflow = HorizontalWrapMode.Overflow; title.verticalOverflow = VerticalWrapMode.Truncate;
        var titleTextRt = title.rectTransform; titleTextRt.anchorMin = Vector2.zero; titleTextRt.anchorMax = Vector2.one;
        titleTextRt.offsetMin = new Vector2(20, 4); titleTextRt.offsetMax = new Vector2(-20, -4);
        backRt.anchorMin = backRt.anchorMax = Vector2.zero;
        homeRt.anchorMin = homeRt.anchorMax = Vector2.zero;
        titleRt.anchorMin = titleRt.anchorMax = Vector2.zero;

        var coinBadge = Rect("TechCoinBadge", historyControls.transform, 0, 0, 224, 94);
        var coinBadgeRt = coinBadge.GetComponent<RectTransform>();
        coinBadgeRt.anchorMin = coinBadgeRt.anchorMax = Vector2.zero;
        var coinShakeTarget = Rect("ShakeTarget", coinBadge.transform, 0, 0, 224, 94);
        var coinBgImg = Img(coinShakeTarget, new Color(1f, 0.99f, 0.97f, 0.96f), spriteRound);
        coinBgImg.type = Image.Type.Sliced;
        SoftShadow(coinShakeTarget);
        var coinRing = Rect("CoinRing", coinShakeTarget.transform, 16, 19, 56, 56);
        Img(coinRing, new Color(0.25f, 0.21f, 0.17f, 0.88f), spriteStationRing).raycastTarget = false;
        var coinIcon = Rect("Icon", coinRing.transform, 4, 4, 48, 48);
        Img(coinIcon, Gold, spriteCircle).raycastTarget = false;
        var coinText = Txt(coinShakeTarget.transform, FormatCoins(coins), 36, Ink, 82, 18, 126, 58, TextAnchor.MiddleLeft);
        coinText.resizeTextForBestFit = true; coinText.resizeTextMinSize = 16; coinText.resizeTextMaxSize = ScaledFontSize(36); coinText.horizontalOverflow = HorizontalWrapMode.Overflow; coinText.verticalOverflow = VerticalWrapMode.Truncate;

        var widget = coinBadge.AddComponent<TechCoinWidget>();
        widget.shakeTarget = coinShakeTarget.GetComponent<RectTransform>();
        widget.coinText = coinText;
        widget.bgImage = coinBgImg;
        widget.normalTextColor = Ink;
        widget.normalBgColor = new Color(1f, 0.99f, 0.97f, 0.96f);
        widget.alertColor = Vermilion;
        widget.getCoins = () => coins;
        historyCoinWidget = widget;

        var headerLayout = historyControls.AddComponent<PageHeaderLayout>();
        headerLayout.page = controlsRt; headerLayout.back = backRt; headerLayout.home = homeRt; headerLayout.title = titleRt;
        headerLayout.coinBadge = coinBadgeRt;
        var progressBadge = Rect("ProgressBadge", historyControls.transform, 24, 125, 260, 56);
        var pbg = Img(progressBadge, new Color(1f, 0.99f, 0.97f, 0.99f), spriteRound); pbg.type = Image.Type.Sliced; SoftShadow(progressBadge);
        var prt = progressBadge.GetComponent<RectTransform>();
        headerLayout.AddSafeTopLeftTarget(prt, new Vector2(24, -138));
        var progressText = Txt(progressBadge.transform, "进度  " + eraDoneCount + " / " + eraTotalCount, 24, Ink, 16, 8, 228, 40, TextAnchor.MiddleLeft);

        var rpBadge = Rect("RPBadge", historyControls.transform, 298, 125, 320, 56);
        var rbg = Img(rpBadge, new Color(1f, 0.99f, 0.97f, 0.99f), spriteRound); rbg.type = Image.Type.Sliced; rbg.raycastTarget = true; SoftShadow(rpBadge);
        var rrt = rpBadge.GetComponent<RectTransform>();
        rpBadgeRect = rrt;
        headerLayout.AddSafeTopLeftTarget(rrt, new Vector2(298, -138));
        var rpDot = Rect("RPDot", rpBadge.transform, 16, 19, 18, 18); Img(rpDot, Indigo, spriteCircle).raycastTarget = false;
        historyRpText = Txt(rpBadge.transform, "科研点  " + Mathf.FloorToInt(rp) + " RP", 24, Indigo, 42, 8, 232, 40, TextAnchor.MiddleLeft);
        historyRpText.resizeTextForBestFit = true; historyRpText.resizeTextMinSize = 17; historyRpText.resizeTextMaxSize = ScaledFontSize(24); historyRpText.raycastTarget = false;
        historyRpText.horizontalOverflow = HorizontalWrapMode.Overflow; historyRpText.verticalOverflow = VerticalWrapMode.Truncate;
        var rpHelpHint = Txt(rpBadge.transform, "?", 24, InkSoft, 280, 8, 30, 40, TextAnchor.MiddleCenter);
        rpHelpHint.raycastTarget = false;
        var rpTouch = rpBadge.AddComponent<HistoryNodeTouch>();
        rpTouch.onHold = HoldRpRules;
        rpTouch.onRelease = HideHistoryInfo;
        rpTouch.onTap = () => Toast("长按科研点徽章查看获取规则（当前日产 " + EstimatedDailyRpRate().ToString("0.0") + " RP）");

        string eraBtnLabel = isCorporate
            ? (activeEra == 0 ? "核热纪 · 股权与募资  ▾" : activeEra == 1 ? "聚变纪 · 超导能量集团网  ▾" : "曲速纪 · 智慧湾枢纽  ▾")
            : (activeEra == 0 ? "核热纪 · 等离子与轨交  ▾" : activeEra == 1 ? "聚变纪 · 聚变与超导能量  ▾" : "曲速纪 · 高速智行  ▾");
        var eraBadge = Btn(historyControls.transform, "EraRoadmapBadge", eraBtnLabel, 23, 24, 0, 328, 54, new Color(1f, 0.99f, 0.96f, 0.99f), () => OpenEraJournalModal(page.transform, isCorporate));
        eraBadge.GetComponentInChildren<Text>().fontStyle = FontStyle.Normal;
        TInk(eraBadge, new Color(0.45f, 0.31f, 0.16f));
        SoftShadow(eraBadge.gameObject);
        Img(Rect("EraAccent", eraBadge.transform, 0, 10, 6, 34), Gold, spriteRound).raycastTarget = false;
        var ert = eraBadge.GetComponent<RectTransform>();
        headerLayout.AddSafeTopLeftTarget(ert, new Vector2(24, -206));

        Button collapseButton = null;
        {
            var collapseBtn = Btn(historyControls.transform, "CollapseDoneBtn", "收纳已研节点 ⟲", 22, 366, 0, 232, 54, new Color(0.95f, 0.92f, 0.85f, 0.98f), () => {
                if (isCorporate) { coTechSessionUnlocked.Clear(); coTechCollapseDone = true; }
                else { techSessionUnlocked.Clear(); techCollapseDone = true; }
                OpenConcentricTechScreen(isCorporate, 1);
            });
            collapseButton = collapseBtn;
            collapseBtn.gameObject.SetActive((!collapseFlag || sessionSet.Count > 0) && eraDoneCount > 0);
            TInk(collapseBtn, Indigo);
            SoftShadow(collapseBtn.gameObject);
            var crt2 = collapseBtn.GetComponent<RectTransform>();
            headerLayout.AddSafeTopLeftTarget(crt2, new Vector2(366, -206));
        }

        RectTransform switchTreeRect = null;
        Action ensureSwitchTree = () => {
        if (!switchTreeRect && (isCorporate || IsFeatureUnlocked(GameFeature.CompanyCore)))
        {
            float tx = ((!collapseFlag || sessionSet.Count > 0) && eraDoneCount > 0) ? 612f : 366f;
            var treeBtn = Btn(historyControls.transform, "SwitchTreeBtn", isCorporate ? "切换：工程技术史 ▸" : "切换：企业治理树 ▸", 22, tx, 0, 250, 54, Matcha,
                () => OpenConcentricTechScreen(!isCorporate, 0));
            switchTreeRect = treeBtn.GetComponent<RectTransform>();
            TInk(treeBtn, Color.white);
            SoftShadow(treeBtn.gameObject);
            headerLayout.AddSafeTopLeftTarget(treeBtn.GetComponent<RectTransform>(), new Vector2(tx, -206));
        }

        };
        ensureSwitchTree();

        var legend = Rect("ResearchLegend", historyControls.transform, 0, 0, 1032, 104);
        var legendRt = legend.GetComponent<RectTransform>(); legendRt.anchorMin = legendRt.anchorMax = new Vector2(0.5f, 0); legendRt.pivot = new Vector2(0.5f, 0); legendRt.anchoredPosition = new Vector2(0, 20);
        headerLayout.bottomSafeTarget = legendRt; headerLayout.bottomSafeMargin = 20f;
        Rounded(legend, new Color(1f, 0.99f, 0.96f, 0.97f)); SoftShadow(legend);
        string[] legendNames = isCorporate ? CoTechBranchNames : new[] { "收入", "客流", "运力", "速度", "港点吞吐", "星舰维护", "航线维护", "换线接驳", "飞车", "运营解锁" };
        ResearchEffect[] legendKinds = { ResearchEffect.Revenue, ResearchEffect.Demand, ResearchEffect.Capacity, ResearchEffect.Speed, ResearchEffect.Station, ResearchEffect.Vehicle, ResearchEffect.Track, ResearchEffect.Transfer, ResearchEffect.None, ResearchEffect.None };
        for (int i = 0; i < legendNames.Length; i++)
        {
            float x = 16 + (i % 5) * 204, y = i < 5 ? 62 : 18;
            var sample = new HistoryNode { effect = legendKinds[i], loco = i == 8 ? 0 : -1 };
            Color color = isCorporate ? CoTechBranchColors[i] : HistoryEffectColor(sample);
            var dot = Rect("LegendDot" + i, legend.transform, x, y + 6, 18, 18);
            Img(dot, color, spriteCircle).raycastTarget = false;
            Txt(legend.transform, legendNames[i], 22, InkSoft, x + 24, y, 174, 32, TextAnchor.MiddleLeft);
        }
        var hintToast = Rect("ResearchHintToast", legend.transform, 0, 0, 380, 44);
        var hintRt = hintToast.GetComponent<RectTransform>();
        hintRt.anchorMin = hintRt.anchorMax = new Vector2(0.5f, 1f);
        hintRt.pivot = new Vector2(0.5f, 0f);
        hintRt.anchoredPosition = new Vector2(0, 14f);
        var hintBg = Img(hintToast, new Color(0.16f, 0.14f, 0.12f, 0.84f), spriteRound);
        hintBg.type = Image.Type.Sliced;
        hintBg.raycastTarget = false;
        var hintTxt = Txt(hintToast.transform, "长按详情 · 双击解锁", 22, Color.white, 0, 0, 380, 44, TextAnchor.MiddleCenter);
        hintTxt.raycastTarget = false;
        incrementalHistoryPage = page; incrementalCorporate = isCorporate;
        refreshHistoryTree = unlockedKey => {
            justUnlockedKey = unlockedKey;
            refreshMountedNodes();
            eraDoneCount = isCorporate ? CoTechEraDoneCount(0) : EraTechDoneCount(activeEra);
            progressText.text = "进度  " + eraDoneCount + " / " + eraTotalCount;
            historyRpText.text = "科研点  " + Mathf.FloorToInt(rp) + " RP";
            coinText.text = FormatCoins(coins);
            bool showCollapse = (!collapseFlag || sessionSet.Count > 0) && eraDoneCount > 0;
            collapseButton.gameObject.SetActive(showCollapse);
            ensureSwitchTree();
            if (switchTreeRect) headerLayout.AddSafeTopLeftTarget(switchTreeRect, new Vector2(showCollapse ? 612f : 366f, -206));
            string eraRomanRefresh = activeEra == 0 ? "— 核热纪 —" : activeEra == 1 ? "— 聚变纪 —" : "— 曲速纪 —";
            if (collapsed && hubGoForRefresh) hubSessionText.text = sessionSet.Count > 0 ? "本次新解 " + sessionSet.Count + " 项外留" : eraRomanRefresh;
            justUnlockedKey = null;
        };
        BuildHistoryInfoPanel(page.transform, pageRt);
        HideHistoryInfo();
    }

    void OpenEraJournalModal(Transform parent, bool isCorporate = false)
    {
        if (eraJournalModal) Destroy(eraJournalModal);
        var modal = Rect("EraJournalModal", parent, 0, 0, 0, 0);
        var mrt = modal.GetComponent<RectTransform>(); mrt.anchorMin = Vector2.zero; mrt.anchorMax = Vector2.one; mrt.offsetMin = mrt.offsetMax = Vector2.zero;
        eraJournalModal = modal;

        var scrimBg = Img(modal, new Color(0.06f, 0.07f, 0.11f, 0.68f));
        var closeBtn = modal.AddComponent<Button>(); closeBtn.targetGraphic = scrimBg; closeBtn.transition = Selectable.Transition.None;
        closeBtn.onClick.AddListener(() => { if (eraJournalModal) Destroy(eraJournalModal); eraJournalModal = null; });

        const float cardW = 900f, cardH = 1140f;
        var sheet = Rect("JournalSheet", modal.transform, 0, 0, cardW, cardH);
        var srt = sheet.GetComponent<RectTransform>(); srt.anchorMin = srt.anchorMax = new Vector2(0.5f, 0.5f); srt.pivot = new Vector2(0.5f, 0.5f); srt.anchoredPosition = Vector2.zero;
        Rounded(sheet, new Color(0.972f, 0.948f, 0.895f, 0.99f));
        SoftShadow(sheet);

        var frameOuter = Rect("JournalFrame", sheet.transform, 18, 18, cardW - 36, cardH - 36);
        Rounded(frameOuter, new Color(0.65f, 0.50f, 0.28f, 0.14f));
        frameOuter.GetComponent<Image>().raycastTarget = false;
        var frameInner = Rect("JournalInner", sheet.transform, 24, 24, cardW - 48, cardH - 48);
        Rounded(frameInner, new Color(0.988f, 0.974f, 0.942f));
        frameInner.GetComponent<Image>().raycastTarget = false;
        JournalTape(sheet.transform, cardW * 0.5f - 95f, cardH - 18f, 190f);

        var cnHeader = Txt(sheet.transform, isCorporate ? "— ◆ 星航治理 · 百年演进 ◆ —" : "— ◆ 星轨百年 · 时代手帐 ◆ —", 38, Ink, 44, cardH - 122, cardW - 88, 52, TextAnchor.MiddleCenter);
        cnHeader.fontStyle = FontStyle.Normal;
        var subHeader = Txt(sheet.transform, "核热与聚变纪卷已开卷 · 曲速纪暂未开放", 22, InkSoft, 44, cardH - 166, cardW - 88, 32, TextAnchor.MiddleCenter);

        var xClose = Btn(sheet.transform, "CloseJournal", "×", 34, cardW - 96, cardH - 96, 62, 62, new Color(0.92f, 0.87f, 0.78f), () => {
            if (eraJournalModal) Destroy(eraJournalModal); eraJournalModal = null;
        });
        TInk(xClose, Ink);

        string[] titles = isCorporate
            ? new[] { "核热纪 · 股权与募资", "聚变纪 · 超导能量集团网", "曲速纪 · 智慧湾枢纽" }
            : new[] { "核热纪 · 等离子奠基", "聚变纪 · 聚变能量", "曲速纪 · 高速智行" };
        string[] subStatus = new string[3];
        for (int e = 0; e < 3; e++)
        {
            if (IsTechEraUnlocked(e))
            {
                int doneE = isCorporate ? CoTechEraDoneCount(e) : EraTechDoneCount(e);
                int totalE = isCorporate ? CoTechEraNodeCount(e) : EraTechNodeCount(e);
                subStatus[e] = (e == currentTechEra ? "当前研习 · " : "点击切换 · ") + doneE + " / " + totalE + " 项  ›";
            }
            else
            {
                subStatus[e] = EraUnlockHint(e);
            }
        }
        Color[] accentColors = {
            new Color(1.00f, 0.34f, 0.72f),
            new Color(0.24f, 0.96f, 0.88f),
            new Color(1.00f, 0.52f, 0.26f)
        };

        float itemW = cardW - 92f;
        const float itemH = 266f;
        for (int i = 0; i < 3; i++)
        {
            int idx = i;
            bool unlockedEra = IsTechEraUnlocked(i);
            float cy = cardH - 480f - i * 286f;

            var vol = Rect("EraVolume_" + i, sheet.transform, 46, cy, itemW, itemH);
            Rounded(vol, new Color(0.015f, 0.015f, 0.025f, 1f));
            SoftShadow(vol);

            var posterGo = Rect("NeonPoster", vol.transform, 10, 44, itemW - 20f, itemH - 56f);
            var posterRaw = posterGo.AddComponent<RawImage>();
            posterRaw.texture = EraPosterGenerator.GetPoster(i);
            if (!unlockedEra) posterRaw.color = new Color(0.45f, 0.48f, 0.55f, 0.55f);
            posterRaw.raycastTarget = false;

            var leftLabel = Txt(vol.transform, titles[i], 22, new Color(0.92f, 0.94f, 0.98f, unlockedEra ? 0.96f : 0.65f), 20, 4, 290, 36, TextAnchor.MiddleLeft);
            leftLabel.raycastTarget = false;
            var rightLabel = Txt(vol.transform, subStatus[i], 20, unlockedEra ? accentColors[i] : new Color(0.65f, 0.68f, 0.75f, 0.85f), 315, 4, itemW - 335f, 36, TextAnchor.MiddleRight);
            rightLabel.resizeTextForBestFit = true; rightLabel.resizeTextMinSize = 14; rightLabel.resizeTextMaxSize = ScaledFontSize(20); rightLabel.horizontalOverflow = HorizontalWrapMode.Overflow; rightLabel.verticalOverflow = VerticalWrapMode.Truncate;
            rightLabel.raycastTarget = false;

            var cardTap = vol.AddComponent<Button>();
            cardTap.targetGraphic = vol.GetComponent<Image>();
            cardTap.onClick.AddListener(() => {
                if (IsTechEraUnlocked(idx)) {
                    if (eraJournalModal) Destroy(eraJournalModal);
                    eraJournalModal = null;
                    if (currentTechEra != idx)
                    {
                        currentTechEra = idx;
                        historySelected = null;
                        historyScrollX = 0.5f;
                        historyScrollY = 0.5f;
                        PlayClickSfx();
                        OpenConcentricTechScreen(isCorporate, 1);
                    }
                } else {
                    PlayErrorSfx();
                    Toast("「" + titles[idx] + "」" + EraUnlockHint(idx));
                }
            });
        }
    }
    static readonly int[] LocoPrices = {
        300, 800, 1500, 3200, 6800, 14000, 28000, 65000, 280000,
        360000, 450000, 540000, 620000, 780000,
        960000, 1280000,
        340000, 380000, 430000, 520000, 500000, 470000,
        480000, 540000, 530000, 580000, 600000, 560000,
        510000, 680000, 610000, 820000, 740000, 790000
    };
    int LocoPrice(int index) { return LocoPrices[Mathf.Clamp(index, 0, LocoPrices.Length - 1)]; }
    void PurchaseLoco(int index, int count = 1)
    {
        if (index < 0 || index >= LocoNames.Length) return;
        count = Mathf.Clamp(count, 1, 99);
        if (!techDone.Contains(LocoKey(index)))
        {
            int era = LocoEra(index);
            Toast(!IsTechEraUnlocked(era) ? $"该车型所属 {21 + era} 世纪科技树暂未开放（{EraUnlockHint(era)}）" : $"请先在 {21 + era} 世纪科技树解锁对应飞车节点");
            return;
        }
        int unitPrice = LocoPrice(index);
        long totalLong = (long)unitPrice * count;
        if (totalLong > int.MaxValue || coins < totalLong)
        {
            float gap = (float)Math.Max(1.0, totalLong - coins);
            NotifyOrOfferDiamondSupply("金币不足，购买 ×" + count + " 还差 ¥" + FormatShortfall(gap), false, gap);
            return;
        }
        int price = (int)totalLong;
        BeginUndo(count > 1 ? ("批量购买飞车 ×" + count) : "购买飞车");
        coins -= price;
        for (int i = 0; i < count; i++) researchVehicles.Add(new ResearchVehicle { node = index });
        SyncCulturalCollection(true); EndUndo(); RefreshHUD(); OpenLocoShop();
        Toast("已购买 " + LocoNames[index] + " ×" + count);
    }
}

