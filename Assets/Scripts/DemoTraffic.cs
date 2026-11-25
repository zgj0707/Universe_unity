using System;
using System.Collections.Generic;
using UnityEngine;

public partial class DemoBootstrap
{
    // 多目的动态客流仿真：通勤/观光/休闲三目的分层 OD + 发班间隔等待成本 + 两轮拥堵感知多路径分流 + 流失归因。
    sealed class TrafficDestFlow
    {
        public int dest;
        public float desired, served;
        public bool reachable, crowded;
    }
    sealed class TrafficResult
    {
        public float[] demand, served;
        public Dictionary<Line, float> lineDemand = new Dictionary<Line, float>();
        public Dictionary<Line, float> lineServed = new Dictionary<Line, float>();
        public Dictionary<Line, float> lineRevenue = new Dictionary<Line, float>();
        public Dictionary<Line, float> linePeakDemand = new Dictionary<Line, float>();
        public Dictionary<string, float> lineSegLoad = new Dictionary<string, float>();
        public float[] sectionDemand, sectionServed, sectionCapacity, originBudget, originDemand;
        public float[] stationUnreached, stationElasticLost, stationCrowdLost;
        public float[,] stationPurposeServed;
        public readonly Dictionary<int, List<TrafficDestFlow>> stationTopDests = new Dictionary<int, List<TrafficDestFlow>>();
        public float totalPassengers, totalRevenue;
    }
    sealed class TrafficEdge { public int target, section; public float cost, distance; }
    struct LineDistPart { public int line; public float dist; }
    readonly List<int> reusableTmpSections = new List<int>(64);
    readonly List<int> reusableTmpStations = new List<int>(16);
    readonly List<int> reusableTmpLineIds = new List<int>(8);
    readonly List<float> reusableTmpLineDists = new List<float>(8);
    readonly List<int> reusableFlatStPool = new List<int>(8192);
    readonly List<int> reusableFlatSecPool = new List<int>(16384);
    readonly List<LineDistPart> reusableFlatPartPool = new List<LineDistPart>(4096);
    readonly TrafficHeap reusableDijkHeap = new TrafficHeap();
    sealed class TrafficTrip
    {
        public int origin, dest, purpose;
        public float amount, fare, bonus;
        public int stOff, stLen, secOff, secLen, partOff, partLen;
    }
    struct TrafficPathSolution
    {
        public int target;
        public float cost, pathDistance;
        public int stOff, stLen, secOff, secLen, partOff, partLen;
    }
    sealed class TrafficHeap
    {
        struct Entry { public int node; public float cost; public Entry(int n,float c){node=n;cost=c;} }
        readonly List<Entry> heap = new List<Entry>();
        public int Count { get { return heap.Count; } }
        public void Clear() { heap.Clear(); }
        public void Push(int node,float cost)
        {
            int i=heap.Count; heap.Add(new Entry(node,cost));
            while(i>0) { int parent=(i-1)/2; if(heap[parent].cost<=cost) break; heap[i]=heap[parent]; i=parent; }
            heap[i]=new Entry(node,cost);
        }
        public int Pop()
        {
            int node=heap[0].node, last=heap.Count-1; var tail=heap[last]; heap.RemoveAt(last);
            if(last>0) { int i=0; while(true) { int left=i*2+1; if(left>=last)break; int right=left+1, child=right<last && heap[right].cost<heap[left].cost ? right : left; if(heap[child].cost>=tail.cost)break; heap[i]=heap[child]; i=child; } heap[i]=tail; }
            return node;
        }
    }
    TrafficResult traffic;
    readonly System.Random trafficRandom = new System.Random();
    static readonly string[] TrafficEventNames = { "日常运营", "穹顶庆典", "星旅旺季", "轨道检修", "太阳耀斑预警" };
    static readonly float[] TrafficEventFactors = { 1f, 1.25f, 1.35f, 0.80f, 0.70f };

    // 每个模拟客流单位对应 100 人次；收入仍使用原经济尺度。
    const float PassengerScale = 100f;
    float CityDemandWeight(Station station)
    {
        if (station.portal || station.roleHint == 4) return 1.6f;
        if (station.region == "月之亮面") return 1.25f;
        if (station.region == "月背") return 0.75f;
        if (station.roleHint == 2) return 1.95f;
        if (station.roleHint == 3) return 1.65f;
        if (station.roleHint == 1) return 1.45f;
        switch (station.city)
        {
            case "塔尔西斯星区": case "克律塞星区": case "大瑟提斯区": case "艾律西昂火山区": return 1.90f;
            case "水手谷中区": case "水手谷西区": case "阿拉伯东区": case "赫拉斯区": return 1.65f;
            default: return station.county ? 1.05f : 1.35f;
        }
    }
    float StationBaselineDemand(int i)
    {
        var s = stations[i];
        float baseValue = 80f;
        switch (s.name)
        {
            // 居住穹顶大站（早高峰出发、晚高峰返程）
            case "开普勒港": case "云星港": case "知海站": case "中央湾": case "阿基米德站": case "高加索山": case "斯托弗勒": case "沃尔特站": case "阿尔卑斯谷":
            case "加加林环": case "智星港": case "德弗里斯": case "科罗廖夫环": case "莱曼前哨":
            case "叙利亚高原": case "阿尔卡迪亚": case "索利斯高原": case "卡塞伊谷": case "提托尼乌姆": case "陶马西亚高地": case "门农尼亚港": case "萨巴高地":
                baseValue *= 1.25f; break;
            // 商业与能源核心大站（早高峰涌入、晚高峰疏散）
            case "静海基地": case "阿里亚德港": case "澄海枢纽": case "哥白尼环形山": case "第谷环港":
            case "冯·卡门港": case "莫斯科海": case "艾特肯深井":
            case "奥林匹斯星门": case "米拉斯深谷": case "克律塞平原": case "艾斯克雷尔斯": case "科普剌塔斯": case "亚马逊平原": case "子午线湾": case "阿拉伯台地":
                baseValue *= 1.30f; break;
        }
        foreach (var sc in sceneries) if (sc.triggerStation == s.name && s.region == sc.region) { baseValue *= 1.15f; break; }
        return baseValue * CityDemandWeight(s);
    }
    float StationBaseDemand(int i)
    {
        var s = stations[i];
        float hour = dayClock / GameDaySeconds * 24f;
        float morning = Mathf.Exp(-Mathf.Pow((hour - 8.5f) / 2f, 2f));
        float evening = Mathf.Exp(-Mathf.Pow((hour - 18f) / 2.4f, 2f));
        float leisure = Mathf.Exp(-Mathf.Pow((hour - 14f) / 4f, 2f));
        bool isScenery = StationHasScenery(i);
        float policyDemandMult = (companyPolicy == 1 ? 1.12f : 1f) * (companyPolicy == 3 && isScenery ? 1.35f : 1f);
        float companyMult = PartnerDemandFactor(isScenery) * CycleDemandFactor(isScenery) * StationTalentDemandMult(i) * CrossBayDemandMult(i);
        return StationBaselineDemand(i) * (0.72f + 0.30f * morning + 0.30f * evening + 0.14f * leisure) * TrafficEventFactors[s.trafficEvent] * ResearchFactor(ResearchEffect.Demand) * policyDemandMult * companyMult * FacilityDemandFactor(s);
    }
    int StationKind(int i)
    {
        string name = stations[i].name;
        switch (name)
        {
            case "开普勒港": case "云星港": case "知海站": case "中央湾": case "阿基米德站": case "高加索山": case "斯托弗勒": case "沃尔特站": case "阿尔卑斯谷":
            case "加加林环": case "智星港": case "德弗里斯": case "科罗廖夫环": case "莱曼前哨":
            case "叙利亚高原": case "阿尔卡迪亚": case "索利斯高原": case "卡塞伊谷": case "提托尼乌姆": case "陶马西亚高地": case "门农尼亚港": case "萨巴高地":
                return 1;
            case "静海基地": case "阿里亚德港": case "澄海枢纽": case "哥白尼环形山": case "第谷环港":
            case "冯·卡门港": case "莫斯科海": case "艾特肯深井":
            case "奥林匹斯星门": case "米拉斯深谷": case "克律塞平原": case "艾斯克雷尔斯": case "科普剌塔斯": case "亚马逊平原": case "子午线湾": case "阿拉伯台地":
                return 2;
        }
        if (stations[i].roleHint > 0) return stations[i].roleHint;
        foreach (var sc in sceneries) if (sc.triggerStation == name && sc.region == stations[i].region) return 3;
        return stations[i].portal ? 4 : 0;
    }
    string StationAreaHint(int i)
    {
        switch (StationKind(i)) {
            case 1: return "该站附近多为居住区，客流早出晚归";
            case 2: return "周边商业与办公集中，早高峰到达、晚高峰出发较多";
            case 3: return "周边景点较多，白天游客较集中";
            case 4: return "区域交界枢纽，适合衔接跨区航线";
            default: return "周边以日常出行为主，客流取决于航线连接";
        }
    }
    float DirectionFactor(int index, bool origin)
    {
        float hour = dayClock / GameDaySeconds * 24f;
        float morning = Mathf.Exp(-Mathf.Pow((hour - 8f) / 2f, 2f));
        float evening = Mathf.Exp(-Mathf.Pow((hour - 18f) / 2f, 2f));
        float noon = Mathf.Exp(-Mathf.Pow((hour - 13f) / 4f, 2f));
        int kind = StationKind(index);
        if (kind == 1) return 0.72f + (origin ? 0.9f * morning + 0.12f * evening : 0.12f * morning + 0.9f * evening);
        if (kind == 2) return 0.72f + (origin ? 0.12f * morning + 0.9f * evening : 0.9f * morning + 0.12f * evening);
        if (kind == 3) return 0.65f + 0.6f * noon;
        return 0.85f + 0.25f * (morning + evening);
    }
    void PurposeMixForOrigin(int origin, out float commuteShare, out float tourismShare, out float leisureShare)
    {
        float hour = dayClock / GameDaySeconds * 24f;
        float morning = Mathf.Exp(-Mathf.Pow((hour - 8.2f) / 2.2f, 2f));
        float evening = Mathf.Exp(-Mathf.Pow((hour - 18.0f) / 2.3f, 2f));
        float noon = Mathf.Exp(-Mathf.Pow((hour - 13.5f) / 3.8f, 2f));
        int kind = StationKind(origin);
        float wCommute = kind == 1 ? (0.45f + 1.35f * morning + 0.30f * evening)
            : kind == 2 ? (0.45f + 0.25f * morning + 1.35f * evening)
            : kind == 4 ? (0.45f + 0.55f * (morning + evening))
            : (0.40f + 0.50f * (morning + evening));
        float wTourism = (kind == 4 ? 0.55f : 0.28f) + (StationHasScenery(origin) ? 0.25f : 0.42f) * noon;
        float wLeisure = (0.38f + 0.40f * noon + 0.35f * evening) * (1f + Mathf.Clamp(stations[origin].facLevel, 0, 3) * 0.25f);
        float sum = Mathf.Max(0.01f, wCommute + wTourism + wLeisure);
        commuteShare = wCommute / sum;
        tourismShare = wTourism / sum;
        leisureShare = wLeisure / sum;
    }
    float OdDistanceDenominator(string regA, string regB, float direct, int purpose)
    {
        bool bothMars = IsMarsRegion(regA) && IsMarsRegion(regB);
        if (purpose == 1) return bothMars ? (180f + direct * 0.35f) : (95f + direct * 0.55f);
        if (purpose == 2) return bothMars ? (150f + direct * 0.42f) : (48f + direct);
        return bothMars ? (160f + direct * 0.42f) : (55f + direct);
    }
    float PurposeDestWeight(int origin, int dest, int purpose, float direct, float[] baseDemand, float[] destDir)
    {
        int dKind = StationKind(dest);
        float baseD = baseDemand[dest];
        float distDenom = OdDistanceDenominator(stations[origin].region, stations[dest].region, direct, purpose);
        if (purpose == 0)
        {
            float hour = dayClock / GameDaySeconds * 24f;
            float morning = Mathf.Exp(-Mathf.Pow((hour - 8.2f) / 2.2f, 2f));
            float evening = Mathf.Exp(-Mathf.Pow((hour - 18.0f) / 2.3f, 2f));
            float kindMult = dKind == 2 ? (0.75f + 1.65f * morning + 0.25f * evening)
                : dKind == 1 ? (0.65f + 0.20f * morning + 1.60f * evening)
                : dKind == 3 ? 0.35f : 0.85f;
            return baseD * destDir[dest] * kindMult / distDenom;
        }
        if (purpose == 1)
        {
            bool hasScenery = StationHasScenery(dest);
            float sceneryMult = hasScenery ? 2.85f : (dKind == 4 ? 1.15f : 0.30f);
            float crossMult = stations[origin].region != stations[dest].region ? 1.35f : 1f;
            return baseD * sceneryMult * crossMult / distDenom;
        }
        float leisureMult = (dKind == 2 ? 1.35f : dKind == 3 ? 1.15f : dKind == 4 ? 1.25f : 0.90f) * FacilityLeisureAttrFactor(stations[dest]);
        return baseD * destDir[dest] * leisureMult / distDenom;
    }
    int DispatchSlot() { float h = dayClock / GameDaySeconds * 24f; return h >= 6f && h < 10f ? 0 : h >= 10f && h < 16f ? 1 : h >= 16f && h < 20f ? 2 : 3; }
    int RunningTrains(Line line) { return Mathf.Clamp(line.dispatch[DispatchSlot()], 0, line.trains); }
    // 默认舰为经济型；研究实物按特性影响维护、运力与长线收益（0..15 为初代基础舰型，16..33 为二代扩展舰型）。
    static readonly float[] FleetMaintenanceTraits = {
        0.75f, 0.85f, 0.9f, 1f, 1.12f, 1.05f, 1.1f, 1.3f, 4.5f,
        2.2f, 2.6f, 3.1f, 1.95f, 2.8f,
        3.2f, 3.6f,
        1.85f, 2.15f, 2.45f, 2.95f, 2.35f, 2.30f, 1.95f, 2.10f, 2.65f, 2.25f, 2.20f, 1.90f, 2.05f, 2.45f, 1.88f, 2.85f, 2.25f, 2.35f
    };
    static readonly float[] FleetCapacityTraits = {
        0.9f,  1.15f, 1.25f, 1.55f, 1.8f, 1.8f, 2f, 2.8f, 2.2f,
        3.2f, 3.4f, 4.4f, 3.8f, 4.2f,
        4.8f, 5.6f,
        3.05f, 3.25f, 3.35f, 4.25f, 3.90f, 3.60f, 3.70f, 3.85f, 4.35f, 3.80f, 3.95f, 3.85f, 3.75f, 4.15f, 4.10f, 4.45f, 4.25f, 4.40f
    };
    static readonly float[] FleetSpeedTraits = {
        0.65f, 0.72f, 0.76f, 0.85f, 1.05f, 0.95f, 1f, 1.1f, 1.65f,
        1.75f, 1.95f, 1.55f, 1.72f, 2.35f,
        2.65f, 3.05f,
        1.45f, 1.78f, 1.92f, 1.58f, 1.88f, 1.82f, 1.68f, 1.84f, 1.52f, 2.15f, 1.98f, 1.70f, 1.80f, 2.25f, 1.66f, 2.42f, 2.12f, 2.22f
    };
    float FleetTrait(Line line, int trait, bool runningOnly = false)
    {
        int needed = runningOnly ? RunningTrains(line) : line.trains;
        if (needed <= 0) return 1f;
        float total = 0f; int count = 0;
        // 已配属的研究星舰优先运行；不足的名额由航线基础星舰补足。
        foreach (var v in researchVehicles) if (v.lineId == line.id && v.node >= 0 && v.node < LocoNames.Length && count < needed) {
            total += trait == 0 ? FleetMaintenanceTraits[v.node] : trait == 1 ? FleetCapacityTraits[v.node] : FleetSpeedTraits[v.node]; count++;
        }
        while (count < needed) { total += 1f; count++; }
        float speedMult = trait == 2 ? ResearchFactor(ResearchEffect.Speed) * (SkinCategory(GetLineSkin(line)) == 1 ? 1.08f : 1f) * (companyPolicy == 2 ? 1.10f : 1f) * PartnerSpeedFactor() * LineTalentSpeedMult(line) : 1f;
        return total / needed * speedMult;
    }
    void SettleRemovedLineTax(Line line)
    {
        float profit=line.todayGross-line.todayMaintenance-line.todayTrack;
        coins-=Mathf.Max(0f,profit)*0.1f;
    }
    void ReleaseLineFleet(Line line)
    {
        foreach (var v in researchVehicles) if (v.lineId == line.id) v.lineId = 0;
        frozenTraffic.Remove(line);
    }
    void SyncLineFleet(Line line)
    {
        int assigned = 0; foreach (var v in researchVehicles) if (v.lineId == line.id) assigned++;
        if (assigned == 0) line.hasBaseTrain = true;
        int count = (line.hasBaseTrain ? 1 : 0) + assigned;
        line.trains = count;
        for (int i=0;i<line.dispatch.Length;i++) line.dispatch[i]=Mathf.Min(line.dispatch[i],count);
    }
    float StationCapacity(int i)
    {
        int lvl = Mathf.Clamp(stations[i].level, 1, 10);
        // 随等级递增步长（Lv.1=180, Lv.5=1200, Lv.10=4500），匹配指数级扩建成本投入。
        float levelCap = 180f + 120f * (lvl - 1) + 45f * (lvl - 1) * (lvl - 2);
        // 高客流枢纽与跨区口岸自带更大的港场规模底盘，避免满级仍必然爆满。
        float hubScale = Mathf.Clamp(Mathf.Sqrt(Mathf.Max(80f, StationBaselineDemand(i)) / 100f), 1f, 1.85f);
        return levelCap * hubScale * PassengerScale * ResearchFactor(ResearchEffect.Station) * CoTechStationCapMult() * StationTalentCapMult(i);
    }
    // 单方向单区段的日化运力；不同区段可由不同乘客复用。
    float FleetCapacity(Line line) { return 4000f * RunningTrains(line) * FleetTrait(line, 1, true) * ResearchFactor(ResearchEffect.Capacity) * (SkinCategory(GetLineSkin(line)) == 4 ? 1.08f : 1f) * LineTalentCapMult(line); }
    float LineCapacity(Line line) { return FleetCapacity(line); }
    int moonNearEconomicAnchor = -2, moonFarEconomicAnchor = -2;
    Vector2 EconomicPosition(int index)
    {
        var station = stations[index];
        if (StationInRegion(index, currentRegion)) return PosInRegion(index, currentRegion);
        if (IsMarsRegion(station.region))
        {
            if (station.hasGeo)
            {
                Vector2 g = MarsGlobalNormalized(station.lon, station.lat);
                return new Vector2(MAP_X + g.x * MAP_W, MAP_Y + g.y * MAP_H);
            }
            return PosInRegion(index, station.region);
        }
        if (station.region == "月之亮面") {
            if (moonNearEconomicAnchor == -2) moonNearEconomicAnchor = stations.FindIndex(x=>x.name=="奥林匹斯星门");
            int anchor = moonNearEconomicAnchor;
            string marsReg = anchor >= 0 ? stations[anchor].region : "火星·西半球";
            Vector2 origin=anchor>=0 ? PosInRegion(anchor, marsReg) : new Vector2(615,900);
            Vector2 localAnchor=anchor>=0 ? PosInRegion(anchor,"月之亮面") : new Vector2(590,865);
            return origin+(PosInRegion(index,"月之亮面")-localAnchor)*0.4f;
        }
        if (station.region == "月背") {
            if (moonFarEconomicAnchor == -2) moonFarEconomicAnchor = stations.FindIndex(x=>x.name=="阿波罗星门");
            int anchor = moonFarEconomicAnchor;
            string marsReg = "火星·西半球";
            Vector2 origin=anchor>=0 ? PosInRegion(anchor, marsReg) : new Vector2(540,850);
            Vector2 localAnchor=anchor>=0 ? PosInRegion(anchor,"月背") : new Vector2(540,900);
            return origin+(PosInRegion(index,"月背")-localAnchor)*0.4f;
        }
        return Pos(index);
    }
    float EconomicSegmentDistance(int a,int b,string region)
    {
        var spec = GetRegionSpec(region);
        float scale = spec != null ? spec.economicScale : (IsMarsRegion(region) ? 1f : 0.4f);
        return Vector2.Distance(PosInRegion(a,region),PosInRegion(b,region))*scale;
    }
    float EconomicPathLength(List<int> path)
    {
        float total=0; for(int i=0;i<path.Count-1;i++) {string region=SegmentRegion(path[i],path[i+1]); if(region!=null) total+=EconomicSegmentDistance(path[i],path[i+1],region);}
        return total;
    }
    void RollTrafficEvents()
    {
        for (int i = 0; i < stations.Count; i++)
            if (StationInRegion(i, currentRegion))
                stations[i].trafficEvent = trafficRandom.NextDouble() < 0.16 ? trafficRandom.Next(1, TrafficEventNames.Length) : 0;
    }
    sealed class FrozenLineTraffic
    {
        public readonly float[] revenue = new float[4], demand = new float[4], served = new float[4], peak = new float[4];
        public readonly bool[] valid = new bool[4];
        public FrozenLineTraffic Copy() {
            var copy = new FrozenLineTraffic();
            Array.Copy(revenue, copy.revenue, 4); Array.Copy(demand, copy.demand, 4); Array.Copy(served, copy.served, 4);
            Array.Copy(peak, copy.peak, 4); Array.Copy(valid, copy.valid, 4); return copy;
        }
    }
    readonly Dictionary<Line, FrozenLineTraffic> frozenTraffic = new Dictionary<Line, FrozenLineTraffic>();
    float[,] frozenStationDemand, frozenStationOrigin, frozenStationDestination;
    bool[,] frozenStationValid;
    void MergeFrozenTraffic(TrafficResult result, List<Line> active)
    {
        int slot = DispatchSlot();
        foreach (var line in lines) if (!active.Contains(line)) {
            result.lineRevenue[line] = 0; result.lineDemand[line] = 0; result.lineServed[line] = 0; result.linePeakDemand[line] = 0;
            if (!frozenTraffic.TryGetValue(line, out var frozen) || !frozen.valid[slot]) continue;
            result.lineRevenue[line] = frozen.revenue[slot]; result.lineDemand[line] = frozen.demand[slot];
            result.lineServed[line] = frozen.served[slot]; result.linePeakDemand[line] = frozen.peak[slot];
        }
    }
    void SaveActiveTraffic(TrafficResult result, int slot)
    {
        foreach (var line in lines) if (PathTouchesRegion(line.st, currentRegion)) {
            if (!frozenTraffic.TryGetValue(line, out var saved)) frozenTraffic[line] = saved = new FrozenLineTraffic();
            saved.revenue[slot] = TrafficRevenue(line, result);
            saved.demand[slot] = result.lineDemand.TryGetValue(line, out float demand) ? demand : 0f;
            saved.served[slot] = result.lineServed.TryGetValue(line, out float served) ? served : 0f;
            saved.peak[slot] = result.linePeakDemand.TryGetValue(line, out float peak) ? peak : 0f;
            saved.valid[slot] = true;
        }
    }
    void FreezeRegionTraffic()
    {
        // 跨区离开时使用无事件的四时段快照，避免将短期活动永久锁定在后台。
        var events = new int[stations.Count]; float savedClock = dayClock;
        for (int i = 0; i < stations.Count; i++) { events[i] = stations[i].trafficEvent; if (StationInRegion(i, currentRegion)) stations[i].trafficEvent = 0; }
        float[] hours = { 8f, 13f, 18f, 1f };
        try {
            for (int slot = 0; slot < 4; slot++) {
                dayClock = hours[slot] * GameDaySeconds / 24f;
                SaveActiveTraffic(CalculateTraffic(), slot);
            }
        } finally {
            dayClock = savedClock;
            for (int i = 0; i < stations.Count; i++)
                stations[i].trafficEvent = StationInRegion(i, currentRegion) ? 0 : events[i];
        }
    }
    IEnumerable<object> RunDijkstraPass(
        int n, int lineCount, int origin, List<Line> serviceLines, int[,] node, List<List<TrafficEdge>> edges,
        float[] sectionPenalty, float[] stationPenalty, int[] nodeStation,
        TrafficHeap heap, bool[] done, float[] dist, int[] prev, int[] prevSection, float[] prevDistance)
    {
        heap.Clear();
        for (int k = 0; k < n; k++)
        {
            if ((k & 63) == 0) yield return null;
            dist[k] = float.PositiveInfinity;
            prev[k] = -1;
            prevSection[k] = -1;
            prevDistance[k] = 0f;
            done[k] = false;
        }
        float originPen = stationPenalty != null ? stationPenalty[origin] : 0f;
        for (int l = 0; l < lineCount; l++)
        {
            if (node[l, origin] < 0) continue;
            int source = node[l, origin];
            float headwayWait = 3.6f / Mathf.Max(1, RunningTrains(serviceLines[l]));
            dist[source] = headwayWait + serviceLines[l].fare * 0.4f + originPen;
            heap.Push(source, dist[source]);
        }
        while (heap.Count > 0)
        {
            yield return null;
            int u = heap.Pop();
            if (done[u]) continue;
            done[u] = true;
            foreach (var e in edges[u])
            {
                if (done[e.target]) continue;
                float extra = 0f;
                if (e.section >= 0 && sectionPenalty != null) extra += sectionPenalty[e.section];
                if (stationPenalty != null) extra += stationPenalty[nodeStation[e.target]];
                float nextCost = dist[u] + e.cost + extra;
                if (nextCost < dist[e.target])
                {
                    dist[e.target] = nextCost;
                    prev[e.target] = u;
                    prevSection[e.target] = e.section;
                    prevDistance[e.target] = e.distance;
                    heap.Push(e.target, nextCost);
                }
            }
        }
    }

    bool TryExtractPath(
        int origin, int dest, int lineCount, int[,] node,
        float[] dist, int[] prev, int[] prevSection, float[] prevDistance,
        int[] nodeLine, int[] nodeStation,
        List<int> tmpSections, List<int> tmpStations, List<int> tmpLineIds, List<float> tmpLineDists,
        List<int> flatStPool, List<int> flatSecPool, List<LineDistPart> flatPartPool,
        out TrafficPathSolution sol)
    {
        sol = default;
        int target = -1; float best = float.PositiveInfinity;
        for (int l = 0; l < lineCount; l++)
        {
            int idx = node[l, dest];
            if (idx >= 0 && dist[idx] < best) { target = idx; best = dist[idx]; }
        }
        if (target < 0) return false;
        tmpSections.Clear();
        tmpStations.Clear();
        tmpStations.Add(origin);
        if (dest != origin) tmpStations.Add(dest);
        tmpLineIds.Clear();
        tmpLineDists.Clear();
        float pathDistance = 0f;
        for (int u = target; prev[u] >= 0; u = prev[u])
        {
            int v = prev[u], section = prevSection[u];
            if (section >= 0)
            {
                tmpSections.Add(section);
                int l = nodeLine[u];
                float d = prevDistance[u];
                int found = -1;
                for (int k = 0; k < tmpLineIds.Count; k++) if (tmpLineIds[k] == l) { found = k; break; }
                if (found >= 0) tmpLineDists[found] += d;
                else { tmpLineIds.Add(l); tmpLineDists.Add(d); }
                pathDistance += d;
            }
            else if (nodeStation[u] == nodeStation[v])
            {
                int st = nodeStation[u];
                if (!tmpStations.Contains(st)) tmpStations.Add(st);
            }
        }
        if (pathDistance <= 0f || tmpLineIds.Count == 0) return false;
        int stOff = flatStPool.Count, stLen = tmpStations.Count;
        for (int k = 0; k < stLen; k++) flatStPool.Add(tmpStations[k]);
        int secOff = flatSecPool.Count, secLen = tmpSections.Count;
        for (int k = 0; k < secLen; k++) flatSecPool.Add(tmpSections[k]);
        int partOff = flatPartPool.Count, partLen = tmpLineIds.Count;
        for (int k = 0; k < partLen; k++) flatPartPool.Add(new LineDistPart { line = tmpLineIds[k], dist = tmpLineDists[k] });
        sol = new TrafficPathSolution
        {
            target = target, cost = best, pathDistance = pathDistance,
            stOff = stOff, stLen = stLen, secOff = secOff, secLen = secLen, partOff = partOff, partLen = partLen
        };
        return true;
    }

    static bool SameRouteSections(List<int> secPool, int offA, int lenA, int offB, int lenB)
    {
        if (lenA != lenB) return false;
        for (int i = 0; i < lenA; i++) if (secPool[offA + i] != secPool[offB + i]) return false;
        return true;
    }

    void RegisterTripToResult(
        TrafficResult result, List<TrafficTrip> trips, List<Line> serviceLines,
        List<int> flatStPool, List<int> flatSecPool, List<LineDistPart> flatPartPool,
        int origin, int dest, int purpose, float desiredSub, float directDistance,
        TrafficPathSolution sol, Dictionary<int, TrafficDestFlow> destTracker, bool trackElastic)
    {
        float detour = sol.pathDistance / Mathf.Max(30f, directDistance);
        float detourFactor = detour <= 1.25f ? 1f : Mathf.Clamp(1f - 1.2f * (detour - 1.25f), 0.15f, 1f);
        float fareBase = 0f, speed = 0f;
        for (int k = 0; k < sol.partLen; k++)
        {
            var part = flatPartPool[sol.partOff + k];
            float share = part.dist / sol.pathDistance;
            fareBase += serviceLines[part.line].fare * share;
            speed += FleetTrait(serviceLines[part.line], 2) * share;
        }
        // 票价基于起终点直达距离，绕远不能凭空增加星票款。
        float fare = fareBase + 0.0036f * directDistance;
        float priceFactor = Mathf.Clamp(0.65f * Mathf.Exp(-0.85f * (fare / (1.5f + 0.0036f * directDistance) - 1f)), 0.1f, 1f);
        float realized = detourFactor * priceFactor / (1f + sol.cost * 0.036f);
        float amount = desiredSub * realized;
        if (trackElastic)
        {
            result.stationElasticLost[origin] += Mathf.Max(0f, desiredSub * 0.35f - amount);
            result.originDemand[origin] += amount;
            if (destTracker != null && destTracker.TryGetValue(dest, out var df))
                df.desired += desiredSub;
        }
        if (amount <= 0.001f) return;
        float bonus = 1f + Mathf.Max(0f, speed - 1f) * Mathf.Clamp01(directDistance / 1200f) * 0.5f;
        for (int k = 0; k < sol.stLen; k++) result.demand[flatStPool[sol.stOff + k]] += amount;
        for (int k = 0; k < sol.secLen; k++) result.sectionDemand[flatSecPool[sol.secOff + k]] += amount;
        for (int k = 0; k < sol.partLen; k++) result.lineDemand[serviceLines[flatPartPool[sol.partOff + k].line]] += amount;
        trips.Add(new TrafficTrip
        {
            origin = origin, dest = dest, purpose = purpose,
            amount = amount, fare = fare, bonus = bonus,
            stOff = sol.stOff, stLen = sol.stLen, secOff = sol.secOff, secLen = sol.secLen, partOff = sol.partOff, partLen = sol.partLen
        });
    }

    sealed class PendingOdPair
    {
        public int origin, dest;
        public float directDistance;
        public float d0, d1, d2;
        public TrafficPathSolution sol0;
    }

    TrafficResult CalculateTraffic(Line replacement = null, List<int> replacementPath = null, Line addition = null)
    {
        TrafficResult result = null;
        var work = CalculateTrafficSteps(value => result = value, replacement, replacementPath, addition);
        using (work) { while (work.MoveNext()) { } }
        return result;
    }
    System.Collections.Generic.IEnumerator<object> CalculateTrafficSteps(Action<TrafficResult> completed,
        Line replacement = null, List<int> replacementPath = null, Line addition = null, bool isolatedWorkspace = false)
    {
        var serviceLines = new List<Line>();
        foreach (var line in lines) if (PathTouchesRegion(line == replacement ? replacementPath : line.st, currentRegion)) serviceLines.Add(line);
        if (addition != null && PathTouchesRegion(addition.st, currentRegion)) serviceLines.Add(addition);
        int count = stations.Count, lineCount = serviceLines.Count;
        var result = new TrafficResult
        {
            demand = new float[count], served = new float[count],
            originBudget = new float[count], originDemand = new float[count],
            stationUnreached = new float[count], stationElasticLost = new float[count],
            stationCrowdLost = new float[count], stationPurposeServed = new float[count, 3]
        };
        int[,] node = new int[lineCount, count]; for (int l = 0; l < lineCount; l++) for (int i = 0; i < count; i++) node[l, i] = -1;
        var nodeStation = new List<int>(); var nodeLine = new List<int>(); var edges = new List<List<TrafficEdge>>();
        var capacity = new List<float>(); var sectionOwners = new List<int>(); var sectionKeys = new List<string>();
        for (int l = 0; l < lineCount; l++)
        {
            yield return null;
            var line = serviceLines[l]; var path = line == replacement ? replacementPath : line.st;
            result.lineDemand[line] = 0; result.lineServed[line] = 0; result.lineRevenue[line] = 0; result.linePeakDemand[line] = 0;
            if (RunningTrains(line) == 0) continue;
            foreach (int st in path) if (node[l, st] < 0) { node[l, st] = edges.Count; nodeStation.Add(st); nodeLine.Add(l); edges.Add(new List<TrafficEdge>()); }
            for (int k = 0; k < path.Count - 1; k++)
            {
                yield return null;
                int a = path[k], b = path[k + 1]; string region = SegmentRegion(a, b); if (region == null) continue;
                float distance = Mathf.Max(1f, EconomicSegmentDistance(a, b, region));
                // 火星长途星际航道采用更高巡航时速（约 2.6 倍），避免因行星尺度的物理距离放大而被误扣成超高耗时流失
                float cruiseMult = IsMarsRegion(region) ? 2.6f : 1f;
                float travel = distance / (58f * cruiseMult * FleetTrait(line, 2)) + distance * 0.002f * (IsMarsRegion(region) ? 0.12f : 0.3f); int na = node[l, a], nb = node[l, b];
                string edgeKey = line.id + ":" + Mathf.Min(a, b) + ":" + Mathf.Max(a, b);
                int forward = capacity.Count; capacity.Add(FleetCapacity(line)); sectionOwners.Add(l); sectionKeys.Add(edgeKey);
                int reverse = capacity.Count; capacity.Add(FleetCapacity(line)); sectionOwners.Add(l); sectionKeys.Add(edgeKey);
                edges[na].Add(new TrafficEdge { target = nb, cost = travel, section = forward, distance = distance });
                edges[nb].Add(new TrafficEdge { target = na, cost = travel, section = reverse, distance = distance });
            }
        }
        for (int st = 0; st < count; st++) if (!stations[st].closed)
            for (int a = 0; a < lineCount; a++) if (node[a, st] >= 0)
                for (int b = a + 1; b < lineCount; b++) if (node[b, st] >= 0)
                {
                    yield return null;
                    float wait = 3.2f / Mathf.Max(1, RunningTrains(serviceLines[b]));
                    float reverse = 3.2f / Mathf.Max(1, RunningTrains(serviceLines[a]));
                    edges[node[a, st]].Add(new TrafficEdge { target = node[b, st], cost = (1.8f + wait) * ResearchCostFactor(ResearchEffect.Transfer) * CoTechTransferMult() + serviceLines[b].fare * 0.4f, section = -1 });
                    edges[node[b, st]].Add(new TrafficEdge { target = node[a, st], cost = (1.8f + reverse) * ResearchCostFactor(ResearchEffect.Transfer) * CoTechTransferMult() + serviceLines[a].fare * 0.4f, section = -1 });
                }
        result.sectionCapacity = capacity.ToArray(); result.sectionDemand = new float[capacity.Count]; result.sectionServed = new float[capacity.Count];
        if (edges.Count == 0) { MergeFrozenTraffic(result, serviceLines); completed(result); yield break; }
        int n = edges.Count;
        var baseDemand = new float[count]; var stationCap = new float[count];
        var originDirection = new float[count]; var destinationDirection = new float[count]; var positions = new Vector2[count];
        if (frozenStationDemand == null || frozenStationDemand.GetLength(0) != count)
        {
            frozenStationDemand = new float[count, 4]; frozenStationOrigin = new float[count, 4];
            frozenStationDestination = new float[count, 4]; frozenStationValid = new bool[count, 4];
        }
        int currentSlot = DispatchSlot();
        for (int i = 0; i < count; i++)
        {
            yield return null;
            bool activeStation = StationInRegion(i, currentRegion);
            if (!activeStation) for (int l = 0; l < lineCount; l++) if (node[l, i] >= 0) { activeStation = true; break; }
            if (activeStation || !frozenStationValid[i, currentSlot])
            {
                frozenStationDemand[i, currentSlot] = StationBaseDemand(i);
                frozenStationOrigin[i, currentSlot] = DirectionFactor(i, true);
                frozenStationDestination[i, currentSlot] = DirectionFactor(i, false);
                frozenStationValid[i, currentSlot] = true;
            }
            baseDemand[i] = frozenStationDemand[i, currentSlot]; originDirection[i] = frozenStationOrigin[i, currentSlot];
            destinationDirection[i] = frozenStationDestination[i, currentSlot];
            stationCap[i] = StationCapacity(i); positions[i] = EconomicPosition(i);
        }

        var nodeStArr = nodeStation.ToArray();
        var nodeLineArr = nodeLine.ToArray();
        var pending = new List<PendingOdPair>(count * 4);
        var originPendingStart = new int[count];
        var originPendingEnd = new int[count];
        var originServedFlags = new bool[count];
        var pass1SecDemand = new float[capacity.Count];
        var pass1StDemand = new float[count];
        var destTrackers = new Dictionary<int, Dictionary<int, TrafficDestFlow>>();

        // 预计算各站目的吸引力常数与所属区域，避免 O(N^2) 循环内重复调用 Exp 与景区线性查找。
        float simHour = dayClock / GameDaySeconds * 24f;
        float simMorning = Mathf.Exp(-Mathf.Pow((simHour - 8.2f) / 2.2f, 2f));
        float simEvening = Mathf.Exp(-Mathf.Pow((simHour - 18.0f) / 2.3f, 2f));
        var destCommAttr = new float[count];
        var destTourAttr = new float[count];
        var destLeisAttr = new float[count];
        var stationRegArr = new string[count];
        for (int i = 0; i < count; i++)
        {
            yield return null;
            stationRegArr[i] = stations[i].region;
            if (stations[i].closed) continue;
            int dKind = StationKind(i);
            float baseD = baseDemand[i];
            float destDir = destinationDirection[i];
            float commMult = dKind == 2 ? (0.75f + 1.65f * simMorning + 0.25f * simEvening)
                : dKind == 1 ? (0.65f + 0.20f * simMorning + 1.60f * simEvening)
                : dKind == 3 ? 0.35f : 0.85f;
            destCommAttr[i] = baseD * destDir * commMult;
            float sceneryMult = StationHasScenery(i) ? 2.85f : (dKind == 4 ? 1.15f : 0.30f);
            destTourAttr[i] = baseD * sceneryMult;
            float leisureMult = (dKind == 2 ? 1.35f : dKind == 3 ? 1.15f : dKind == 4 ? 1.25f : 0.90f) * FacilityLeisureAttrFactor(stations[i]);
            destLeisAttr[i] = baseD * destDir * leisureMult;
        }

        // 复用单套 Dijkstra 缓冲区与路径提取工作区，消除按出发站/OD 对重复分配容器。
        var stationServed = new bool[count];
        for (int i = 0; i < count; i++)
            for (int l = 0; l < lineCount; l++) if (node[l, i] >= 0) { stationServed[i] = true; break; }

        var dijkHeap = isolatedWorkspace ? new TrafficHeap() : reusableDijkHeap; dijkHeap.Clear();
        var dijkDone = new bool[n];
        var dijkDist = new float[n];
        var dijkPrev = new int[n];
        var dijkPrevSec = new int[n];
        var dijkPrevDist = new float[n];
        var w0 = new float[count];
        var w1 = new float[count];
        var w2 = new float[count];
        var tmpSections = isolatedWorkspace ? new List<int>(64) : reusableTmpSections; tmpSections.Clear();
        var tmpStations = isolatedWorkspace ? new List<int>(16) : reusableTmpStations; tmpStations.Clear();
        var tmpLineIds = isolatedWorkspace ? new List<int>(8) : reusableTmpLineIds; tmpLineIds.Clear();
        var tmpLineDists = isolatedWorkspace ? new List<float>(8) : reusableTmpLineDists; tmpLineDists.Clear();
        var flatStPool = isolatedWorkspace ? new List<int>(8192) : reusableFlatStPool; flatStPool.Clear();
        var flatSecPool = isolatedWorkspace ? new List<int>(16384) : reusableFlatSecPool; flatSecPool.Clear();
        var flatPartPool = isolatedWorkspace ? new List<LineDistPart>(4096) : reusableFlatPartPool; flatPartPool.Clear();

        // 第 1 轮：三目的分层 OD（通勤 / 观光 / 休闲商业）+ 无拥堵基准最短路
        for (int origin = 0; origin < count; origin++)
        {
            yield return null;
            originPendingStart[origin] = pending.Count;
            originPendingEnd[origin] = pending.Count;
            if (stations[origin].closed || !stationServed[origin]) continue;
            originServedFlags[origin] = true;

            foreach (var step in RunDijkstraPass(n, lineCount, origin, serviceLines, node, edges, null, null, nodeStArr,
                dijkHeap, dijkDone, dijkDist, dijkPrev, dijkPrevSec, dijkPrevDist)) yield return step;

            PurposeMixForOrigin(origin, out float pComm, out float pTour, out float pLeis);
            float tot0 = 0f, tot1 = 0f, tot2 = 0f;
            string originReg = stationRegArr[origin];
            Vector2 originPos = positions[origin];
            for (int dest = 0; dest < count; dest++)
            {
                yield return null;
                if (dest == origin || stations[dest].closed) continue;
                float direct = Vector2.Distance(originPos, positions[dest]);
                // 抑制两个交界港之间的直达需求，防止多条航线绕经交界港互相刷客流。
                float portalPairMult = (stations[origin].portal && stations[dest].portal) ? 0.12f : 1f;
                string destReg = stationRegArr[dest];
                // 未解锁外区不再空分走大量本地通勤权重；同分区拥有更高归属吸引，确保局部成网也能兑现合理客流
                float regionReachWeight = RegionUnlocked(destReg) ? ((IsMarsRegion(originReg) && destReg == originReg) ? 1.85f : 1f) : 0.06f;
                w0[dest] = destCommAttr[dest] * portalPairMult * regionReachWeight / OdDistanceDenominator(originReg, destReg, direct, 0); tot0 += w0[dest];
                float crossMult = originReg != destReg ? 1.35f : 1f;
                w1[dest] = destTourAttr[dest] * crossMult * portalPairMult * regionReachWeight / OdDistanceDenominator(originReg, destReg, direct, 1); tot1 += w1[dest];
                w2[dest] = destLeisAttr[dest] * portalPairMult * regionReachWeight / OdDistanceDenominator(originReg, destReg, direct, 2); tot2 += w2[dest];
            }
            if (tot0 + tot1 + tot2 <= 0f) continue;

            float originBudget = baseDemand[origin] * PassengerScale * 3.1f * originDirection[origin];
            result.originBudget[origin] = originBudget;
            var dMap = new Dictionary<int, TrafficDestFlow>();
            destTrackers[origin] = dMap;

            for (int dest = 0; dest < count; dest++)
            {
                yield return null;
                if (dest == origin || stations[dest].closed) continue;
                float d0 = tot0 > 0f ? originBudget * pComm * (w0[dest] / tot0) : 0f;
                float d1 = tot1 > 0f ? originBudget * pTour * (w1[dest] / tot1) : 0f;
                float d2 = tot2 > 0f ? originBudget * pLeis * (w2[dest] / tot2) : 0f;
                float dSum = d0 + d1 + d2;
                if (dSum <= 0.001f) continue;

                float direct = Vector2.Distance(originPos, positions[dest]);
                TrafficPathSolution sol0 = default;
                bool reachable = stationServed[dest] && TryExtractPath(
                    origin, dest, lineCount, node, dijkDist, dijkPrev, dijkPrevSec, dijkPrevDist,
                    nodeLineArr, nodeStArr, tmpSections, tmpStations, tmpLineIds, tmpLineDists,
                    flatStPool, flatSecPool, flatPartPool, out sol0);
                if (!reachable)
                {
                    bool relevant = StationInRegion(dest, currentRegion) || stations[dest].built || stations[dest].portal;
                    if (relevant)
                    {
                        result.stationUnreached[origin] += dSum * 0.35f;
                        dMap[dest] = new TrafficDestFlow { dest = dest, desired = dSum, served = 0f, reachable = false };
                    }
                    continue;
                }

                dMap[dest] = new TrafficDestFlow { dest = dest, desired = 0f, served = 0f, reachable = true };
                float detour = sol0.pathDistance / Mathf.Max(30f, direct);
                float detourF = detour <= 1.25f ? 1f : Mathf.Clamp(1f - 1.2f * (detour - 1.25f), 0.15f, 1f);
                float fareB = 0f;
                for (int k = 0; k < sol0.partLen; k++)
                {
                    yield return null;
                    var part = flatPartPool[sol0.partOff + k];
                    fareB += serviceLines[part.line].fare * (part.dist / sol0.pathDistance);
                }
                float estFare = fareB + 0.0036f * direct;
                float priceF = Mathf.Clamp(0.65f * Mathf.Exp(-0.85f * (estFare / (1.5f + 0.0036f * direct) - 1f)), 0.1f, 1f);
                float estFlow = dSum * detourF * priceF / (1f + sol0.cost * 0.036f);
                for (int k = 0; k < sol0.stLen; k++) pass1StDemand[flatStPool[sol0.stOff + k]] += estFlow;
                for (int k = 0; k < sol0.secLen; k++) pass1SecDemand[flatSecPool[sol0.secOff + k]] += estFlow;

                pending.Add(new PendingOdPair { origin = origin, dest = dest, directDistance = direct, d0 = d0, d1 = d1, d2 = d2, sol0 = sol0 });
            }
            originPendingEnd[origin] = pending.Count;
        }

        // 检查是否存在高负荷区段或星港；存在拥堵时按起始港流式复用缓冲，执行第 2 轮拥堵感知多路径分流（MSA）。
        float maxLoadRatio = 0f;
        for (int s = 0; s < capacity.Count; s++)
            if (capacity[s] > 0f) maxLoadRatio = Mathf.Max(maxLoadRatio, pass1SecDemand[s] / capacity[s]);
        for (int i = 0; i < count; i++)
            if (stationCap[i] > 0f) maxLoadRatio = Mathf.Max(maxLoadRatio, pass1StDemand[i] / stationCap[i]);

        bool runSecondPass = maxLoadRatio > 0.72f && pending.Count > 0;
        float splitAlpha = runSecondPass ? Mathf.Clamp((maxLoadRatio - 0.72f) * 0.65f, 0.22f, 0.48f) : 0f;
        var trips = new List<TrafficTrip>(pending.Count);

        if (runSecondPass)
        {
            var secPenalty = new float[capacity.Count];
            for (int s = 0; s < capacity.Count; s++)
            {
                yield return null;
                float r = capacity[s] > 0f ? pass1SecDemand[s] / capacity[s] : 0f;
                if (r > 0.70f) secPenalty[s] = 3.5f * Mathf.Pow(Mathf.Min(r - 0.70f, 2.2f), 1.6f);
            }
            var stPenalty = new float[count];
            for (int i = 0; i < count; i++)
            {
                yield return null;
                float r = stationCap[i] > 0f ? pass1StDemand[i] / stationCap[i] : 0f;
                if (r > 0.85f) stPenalty[i] = 2.2f * Mathf.Min(r - 0.85f, 2.0f);
            }
            for (int origin = 0; origin < count; origin++)
            {
                yield return null;
                int start = originPendingStart[origin], end = originPendingEnd[origin];
                if (!originServedFlags[origin] || end <= start) continue;
                foreach (var step in RunDijkstraPass(n, lineCount, origin, serviceLines, node, edges, secPenalty, stPenalty, nodeStArr,
                    dijkHeap, dijkDone, dijkDist, dijkPrev, dijkPrevSec, dijkPrevDist)) yield return step;
                var dMap = destTrackers[origin];
                for (int idx = start; idx < end; idx++)
                {
                    yield return null;
                    var od = pending[idx];
                    int saveSt = flatStPool.Count, saveSec = flatSecPool.Count, savePart = flatPartPool.Count;
                    bool extractedAlt = TryExtractPath(od.origin, od.dest, lineCount, node, dijkDist, dijkPrev, dijkPrevSec, dijkPrevDist,
                        nodeLineArr, nodeStArr, tmpSections, tmpStations, tmpLineIds, tmpLineDists,
                        flatStPool, flatSecPool, flatPartPool, out TrafficPathSolution solAlt);
                    bool splitValid = extractedAlt &&
                        !SameRouteSections(flatSecPool, od.sol0.secOff, od.sol0.secLen, solAlt.secOff, solAlt.secLen);
                    if (extractedAlt && !splitValid)
                    {
                        if (flatStPool.Count > saveSt) flatStPool.RemoveRange(saveSt, flatStPool.Count - saveSt);
                        if (flatSecPool.Count > saveSec) flatSecPool.RemoveRange(saveSec, flatSecPool.Count - saveSec);
                        if (flatPartPool.Count > savePart) flatPartPool.RemoveRange(savePart, flatPartPool.Count - savePart);
                    }
                    float p0Share = splitValid ? 1f - splitAlpha : 1f;
                    for (int p = 0; p < 3; p++)
                    {
                        yield return null;
                        float dp = p == 0 ? od.d0 : (p == 1 ? od.d1 : od.d2);
                        if (dp <= 0.0005f) continue;
                        if (splitValid)
                        {
                            RegisterTripToResult(result, trips, serviceLines, flatStPool, flatSecPool, flatPartPool, od.origin, od.dest, p, dp * p0Share, od.directDistance, od.sol0, dMap, true);
                            RegisterTripToResult(result, trips, serviceLines, flatStPool, flatSecPool, flatPartPool, od.origin, od.dest, p, dp * splitAlpha, od.directDistance, solAlt, dMap, true);
                        }
                        else
                        {
                            RegisterTripToResult(result, trips, serviceLines, flatStPool, flatSecPool, flatPartPool, od.origin, od.dest, p, dp, od.directDistance, od.sol0, dMap, true);
                        }
                    }
                }
            }
        }
        else
        {
            for (int idx = 0; idx < pending.Count; idx++)
            {
                yield return null;
                var od = pending[idx];
                var dMap = destTrackers[od.origin];
                for (int p = 0; p < 3; p++)
                {
                    yield return null;
                    float dp = p == 0 ? od.d0 : (p == 1 ? od.d1 : od.d2);
                    if (dp <= 0.0005f) continue;
                    RegisterTripToResult(result, trips, serviceLines, flatStPool, flatSecPool, flatPartPool, od.origin, od.dest, p, dp, od.directDistance, od.sol0, dMap, true);
                }
            }
        }

        for (int section = 0; section < sectionOwners.Count; section++)
        {
            yield return null;
            var line = serviceLines[sectionOwners[section]];
            result.linePeakDemand[line] = Mathf.Max(result.linePeakDemand[line], result.sectionDemand[section]);
            string sk = sectionKeys[section];
            float loadRatio = result.sectionCapacity[section] > 0f ? result.sectionDemand[section] / result.sectionCapacity[section] : 0f;
            if (!result.lineSegLoad.TryGetValue(sk, out float prevLoad) || loadRatio > prevLoad)
                result.lineSegLoad[sk] = loadRatio;
        }
        float revenueMultiplier = TechMult() * PartnerRevenueFactor();
        for (int idx = 0; idx < trips.Count; idx++)
        {
            yield return null;
            var trip = trips[idx];
            float factor = 1f;
            for (int k = 0; k < trip.stLen; k++)
            {
                yield return null;
                int st = flatStPool[trip.stOff + k];
                if (result.demand[st] > 0) factor = Mathf.Min(factor, stationCap[st] / result.demand[st]);
            }
            for (int k = 0; k < trip.secLen; k++)
            {
                yield return null;
                int sec = flatSecPool[trip.secOff + k];
                if (result.sectionDemand[sec] > 0) factor = Mathf.Min(factor, result.sectionCapacity[sec] / result.sectionDemand[sec]);
            }
            factor = Mathf.Clamp01(factor);
            float served = trip.amount * factor;
            float crowdCut = trip.amount - served;
            if (crowdCut > 0f)
            {
                result.stationCrowdLost[trip.origin] += crowdCut;
                if (destTrackers.TryGetValue(trip.origin, out var dm) && dm.TryGetValue(trip.dest, out var df))
                    df.crowded = true;
            }
            if (served <= 0f) continue;

            for (int k = 0; k < trip.stLen; k++) result.served[flatStPool[trip.stOff + k]] += served;
            for (int k = 0; k < trip.secLen; k++) result.sectionServed[flatSecPool[trip.secOff + k]] += served;
            result.stationPurposeServed[trip.origin, trip.purpose] += served;
            if (destTrackers.TryGetValue(trip.origin, out var dTracker) && dTracker.TryGetValue(trip.dest, out var destFlow))
                destFlow.served += served;

            float distance = 0f;
            for (int k = 0; k < trip.partLen; k++) distance += flatPartPool[trip.partOff + k].dist;
            float revenue = served * trip.fare / 10f * trip.bonus * revenueMultiplier;
            float tripTotalRev = 0f;
            for (int k = 0; k < trip.partLen; k++)
            {
                yield return null;
                var part = flatPartPool[trip.partOff + k];
                var line = serviceLines[part.line];
                bool lineHasScenery = LineHasSceneryStation(line);
                float policyRevMult = (companyPolicy == 1 ? 0.95f : (companyPolicy == 3 && lineHasScenery ? 1.10f : 1f)) * CycleRevenueFactor(lineHasScenery) * (lineHasScenery ? CoTechTourismLineMult() : 1f) * LineTalentRevenueMult(line);
                float partRev = revenue * part.dist / distance * (SkinCategory(GetLineSkin(line)) == 0 ? 1.06f : 1f) * policyRevMult;
                result.lineServed[line] += served; result.lineRevenue[line] += partRev; tripTotalRev += partRev;
            }
            result.totalPassengers += served; result.totalRevenue += tripTotalRev;
        }

        // 整理每座星港的热门去向 Top 3（O(K) 线性选拔替代全量排序）
        foreach (var kv in destTrackers)
        {
            TrafficDestFlow t0 = null, t1 = null, t2 = null;
            foreach (var df in kv.Value.Values)
            {
                if (t0 == null || df.desired > t0.desired) { t2 = t1; t1 = t0; t0 = df; }
                else if (t1 == null || df.desired > t1.desired) { t2 = t1; t1 = df; }
                else if (t2 == null || df.desired > t2.desired) { t2 = df; }
            }
            var top = new List<TrafficDestFlow>(3);
            if (t0 != null) top.Add(t0);
            if (t1 != null) top.Add(t1);
            if (t2 != null) top.Add(t2);
            result.stationTopDests[kv.Key] = top;
        }

        MergeFrozenTraffic(result, serviceLines);
        completed(result); yield break;
    }

    float StationWaitRatio(int index)
    {
        if (traffic == null || index < 0 || index >= stations.Count) return 0f;
        float cap = StationCapacity(index);
        return cap > 0f ? traffic.demand[index] / cap : 0f;
    }

    float LinePeakLoadRatio(Line line)
    {
        if (line == null || traffic == null || line.st == null) return 0f;
        float peak = 0f;
        for (int i = 0; i < line.st.Count - 1; i++)
        {
            string key = line.id + ":" + Mathf.Min(line.st[i], line.st[i + 1]) + ":" + Mathf.Max(line.st[i], line.st[i + 1]);
            if (traffic.lineSegLoad != null && traffic.lineSegLoad.TryGetValue(key, out float load) && load > peak) peak = load;
            if (trafficSlots != null)
            {
                for (int s = 0; s < trafficSlots.Length; s++)
                {
                    var slot = trafficSlots[s];
                    if (slot == null || slot.lineSegLoad == null) continue;
                    if (slot.lineSegLoad.TryGetValue(key, out float slotLoad) && slotLoad > peak) peak = slotLoad;
                }
            }
        }
        return peak;
    }

    float StationLossRatio(int index)
    {
        if (traffic == null || index < 0 || index >= stations.Count) return 0f;
        if (traffic.originBudget == null || index >= traffic.originBudget.Length) return 0f;
        float budget = traffic.originBudget[index];
        if (budget <= 1f) return 0f;
        float lost = 0f;
        if (traffic.stationCrowdLost != null && index < traffic.stationCrowdLost.Length) lost += traffic.stationCrowdLost[index];
        if (traffic.stationUnreached != null && index < traffic.stationUnreached.Length) lost += traffic.stationUnreached[index] * 0.65f;
        return lost / budget;
    }

    HashSet<int> ComputeNetReachable(int origin)
    {
        var netReachable = new HashSet<int>();
        if (origin < 0 || origin >= stations.Count || stations[origin].closed) return netReachable;
        var queue = new Queue<int>();
        netReachable.Add(origin);
        queue.Enqueue(origin);
        while (queue.Count > 0)
        {
            int cur = queue.Dequeue();
            for (int l = 0; l < lines.Count; l++)
            {
                var line = lines[l];
                if (RunningTrains(line) <= 0 || line.st == null || !line.st.Contains(cur)) continue;
                for (int k = 0; k < line.st.Count; k++)
                {
                    int nxt = line.st[k];
                    if (nxt >= 0 && nxt < stations.Count && !stations[nxt].closed && netReachable.Add(nxt))
                        queue.Enqueue(nxt);
                }
            }
        }
        return netReachable;
    }

    List<TrafficDestFlow> GetStationOdTopDests(int origin, int maxCount = 3)
    {
        var list = new List<TrafficDestFlow>();
        if (origin < 0 || origin >= stations.Count || stations[origin].closed) return list;
        var netReachable = ComputeNetReachable(origin);
        bool originCrowded = StationWaitRatio(origin) >= 1f;
        var seen = new HashSet<int>();
        if (traffic != null && traffic.stationTopDests != null &&
            traffic.stationTopDests.TryGetValue(origin, out var existing) && existing != null && existing.Count > 0)
        {
            for (int i = 0; i < existing.Count && list.Count < maxCount; i++)
            {
                var item = existing[i];
                if (StationInRegion(item.dest, currentRegion) && seen.Add(item.dest))
                {
                    bool realReach = netReachable.Contains(item.dest);
                    bool realCrowd = realReach && (item.crowded || originCrowded || StationWaitRatio(item.dest) >= 1f);
                    list.Add(new TrafficDestFlow
                    {
                        dest = item.dest,
                        desired = item.desired,
                        served = item.served,
                        reachable = realReach,
                        crowded = realCrowd
                    });
                }
            }
            if (list.Count >= maxCount) return list;
        }

        // 未接入星轨网（或跨区港名额不足）时，补充本区潜在意愿最高的 OD 目标以便在地图预览
        int count = stations.Count;
        var baseD = new float[count];
        var destDir = new float[count];
        for (int i = 0; i < count; i++)
        {
            baseD[i] = StationBaseDemand(i);
            destDir[i] = DirectionFactor(i, false);
        }
        PurposeMixForOrigin(origin, out float pComm, out float pTour, out float pLeis);
        Vector2 originPos = EconomicPosition(origin);
        float originBudget = baseD[origin] * PassengerScale * 3.1f * DirectionFactor(origin, true);
        var w0 = new float[count]; var w1 = new float[count]; var w2 = new float[count];
        float tot0 = 0f, tot1 = 0f, tot2 = 0f;
        for (int dest = 0; dest < count; dest++)
        {
            if (dest == origin || stations[dest].closed || !StationInRegion(dest, currentRegion)) continue;
            float direct = Vector2.Distance(originPos, EconomicPosition(dest));
            w0[dest] = PurposeDestWeight(origin, dest, 0, direct, baseD, destDir); tot0 += w0[dest];
            w1[dest] = PurposeDestWeight(origin, dest, 1, direct, baseD, destDir); tot1 += w1[dest];
            w2[dest] = PurposeDestWeight(origin, dest, 2, direct, baseD, destDir); tot2 += w2[dest];
        }
        if (tot0 + tot1 + tot2 <= 0f) return list;
        var extra = new List<TrafficDestFlow>();
        for (int dest = 0; dest < count; dest++)
        {
            if (dest == origin || stations[dest].closed || !StationInRegion(dest, currentRegion) || seen.Contains(dest)) continue;
            float d = (tot0 > 0f ? originBudget * pComm * (w0[dest] / tot0) : 0f)
                    + (tot1 > 0f ? originBudget * pTour * (w1[dest] / tot1) : 0f)
                    + (tot2 > 0f ? originBudget * pLeis * (w2[dest] / tot2) : 0f);
            if (d <= 0.01f) continue;
            bool isReachable = netReachable.Contains(dest);
            bool isCrowded = isReachable && (originCrowded || StationWaitRatio(dest) >= 1f);
            extra.Add(new TrafficDestFlow { dest = dest, desired = d, served = 0f, reachable = isReachable, crowded = isCrowded });
        }
        extra.Sort((a, b) => b.desired.CompareTo(a.desired));
        for (int i = 0; i < extra.Count && list.Count < maxCount; i++) list.Add(extra[i]);
        return list;
    }

    void CollectTrafficAudit(out float crowdLost, out float unreachedLost, out float elasticLost, out int bottleneckStation, out float bottleneckScore, out string bottleneckNote)
    {
        crowdLost = 0f; unreachedLost = 0f; elasticLost = 0f;
        bottleneckStation = -1; bottleneckScore = 0f; bottleneckNote = "";
        if (traffic == null) return;
        int n = stations.Count;
        for (int i = 0; i < n; i++)
        {
            float c = traffic.stationCrowdLost != null && i < traffic.stationCrowdLost.Length ? traffic.stationCrowdLost[i] : 0f;
            float u = traffic.stationUnreached != null && i < traffic.stationUnreached.Length ? traffic.stationUnreached[i] : 0f;
            float e = traffic.stationElasticLost != null && i < traffic.stationElasticLost.Length ? traffic.stationElasticLost[i] : 0f;
            crowdLost += c; unreachedLost += u; elasticLost += e;
            if (!stations[i].built || stations[i].closed || StationLineCount(i) <= 0) continue;
            float wait = StationWaitRatio(i);
            float regionMult = StationInRegion(i, currentRegion) ? 1.35f : 0.75f;
            float score = (c * 1.4f + Mathf.Max(0f, wait - 0.72f) * 450f + u * 0.25f) * regionMult;
            if (score > bottleneckScore)
            {
                bottleneckScore = score;
                bottleneckStation = i;
                if (c >= u && c >= 25f)
                    bottleneckNote = "星港或星舱超载，日均流失约 " + Mathf.RoundToInt(c) + " 人次（建议加舰/扩容）";
                else if (wait >= 0.72f)
                    bottleneckNote = "星港候航负荷达 " + Mathf.RoundToInt(wait * 100f) + "%，已接近排队上限";
                else if (u >= 30f)
                    bottleneckNote = "尚有约 " + Mathf.RoundToInt(u) + " 人次热门去向未接入星轨网";
                else
                    bottleneckNote = "绕行耗时或票价偏高流失约 " + Mathf.RoundToInt(e) + " 人次";
            }
        }
    }

    string StationFlowDiagnostics(int index)
    {
        if (index < 0 || index >= stations.Count) return "";
        string region = stations[index].region;
        var linesOut = new System.Text.StringBuilder();

        if (traffic != null && traffic.stationPurposeServed != null)
        {
            float c = traffic.stationPurposeServed[index, 0];
            float tu = traffic.stationPurposeServed[index, 1];
            float le = traffic.stationPurposeServed[index, 2];
            float sum = c + tu + le;
            if (sum <= 0.1f)
            {
                PurposeMixForOrigin(index, out c, out tu, out le);
                sum = 1f;
            }
            linesOut.Append("客流构成：通勤 ")
                .Append(Mathf.RoundToInt(c / sum * 100f)).Append("% · 观光 ")
                .Append(Mathf.RoundToInt(tu / sum * 100f)).Append("% · 商业 ")
                .Append(Mathf.RoundToInt(le / sum * 100f)).Append("%");
        }
        if (stations[index].facLevel > 0)
        {
            int fl = Mathf.Clamp(stations[index].facLevel, 0, StationFacilityNames.Length - 1);
            int pct = Mathf.RoundToInt((FacilityDemandFactor(stations[index]) - 1f) * 100f);
            float leisX = FacilityLeisureAttrFactor(stations[index]);
            linesOut.Append("\n港前商业：").Append(StationFacilityNames[fl])
                .Append("（客流 +").Append(pct).Append("% · 商业虹吸 ").Append(leisX.ToString("0.0")).Append("x）");
        }

        var dests = StationLineCount(index) > 0 ? GetStationOdTopDests(index, 3) : null;
        if (dests != null && dests.Count > 0)
        {
            linesOut.Append("\n热门去向：");
            for (int i = 0; i < dests.Count; i++)
            {
                if (i > 0) linesOut.Append("、");
                var df = dests[i];
                string tag = !df.reachable ? "未联网" : df.crowded ? "运力不足" : "运力通畅";
                linesOut.Append(StationDisplayName(df.dest, currentRegion)).Append("（").Append(tag).Append("）");
            }
        }
        else
        {
            string compass = StationCompassHint(index);
            if (!string.IsNullOrEmpty(compass)) linesOut.Append("\n热门客流方向：").Append(compass);
        }

        if (traffic != null && StationLineCount(index) > 0)
        {
            float crowd = traffic.stationCrowdLost != null ? traffic.stationCrowdLost[index] : 0f;
            float unreached = traffic.stationUnreached != null ? traffic.stationUnreached[index] : 0f;
            float elastic = traffic.stationElasticLost != null ? traffic.stationElasticLost[index] : 0f;
            if (crowd >= 20f && crowd >= unreached * 0.5f)
            {
                float waitR = StationWaitRatio(index);
                linesOut.Append(waitR >= 1f
                    ? "\n流失原因：星港超载，约 " + Mathf.RoundToInt(crowd) + " 人次/日滞留（建议扩建星港）"
                    : "\n流失原因：星舰满载，约 " + Mathf.RoundToInt(crowd) + " 人次/日难以登舰（建议增舰或分流）");
            }
            else if (unreached >= 40f && unreached >= elastic)
            {
                linesOut.Append("\n流失原因：约 " + Mathf.RoundToInt(unreached) + " 人次/日想去的热门港尚未联网");
            }
            else if (elastic >= 40f)
            {
                linesOut.Append("\n流失原因：绕行耗时或票价偏高，约 " + Mathf.RoundToInt(elastic) + " 人次/日改走他途（可设直达快线）");
            }
        }
        return linesOut.ToString();
    }
    float StationPassengerFlow(int index) { return traffic == null ? 0 : traffic.served[index]; }
    float StationRequestedFlow(int index) { return traffic == null ? 0 : traffic.demand[index]; }
    float LinePassengerFlow(Line line) { return traffic != null && traffic.lineServed.TryGetValue(line,out float value) ? value : 0f; }
    float LinePeakSectionDemand(Line line) { return traffic != null && traffic.linePeakDemand.TryGetValue(line,out float value) ? value : 0f; }
    sealed class LineForecastData { public float gross, vehicle, track, tax; public float Profit { get { return gross - vehicle - track - tax; } } }
    readonly Dictionary<Line, LineForecastData> lineForecasts = new Dictionary<Line, LineForecastData>();
    readonly TrafficResult[] trafficSlots = new TrafficResult[4];
    int forecastSignature; bool hasForecastSignature;
    int ForecastSignature()
    {
        unchecked {
            int hash = 17; hash = hash * 31 + currentRegion.GetHashCode(); hash = hash * 31 + companyPolicy;
            hash = hash * 31 + economyCycle; hash = hash * 31 + CurrentSolarTermIndex();
            hash = hash * 31 + earnedPlaques.Count * 13 + earnedMemorialTickets.Count * 7;
            hash = hash * 31 + (consultantDaysLeft > 0 ? consultantId + 100 : 0);
            hash = hash * 31 + (autoDispatchDaysLeft > 0 ? 1 : 0);
            foreach (int pid in boardPartners) hash = hash * 31 + (pid + 7);
            foreach (var line in lines) {
                hash=hash*31+line.GetHashCode(); hash=hash*31+line.trains; hash=hash*31+(line.hasBaseTrain?1:0); hash=hash*31+line.fare.GetHashCode(); hash=hash*31+GetLineSkin(line);
                foreach (int v in line.st) hash=hash*31+v;
                foreach (int v in line.dispatch) hash=hash*31+v;
            }
            foreach (var station in stations) { hash=hash*31+station.level; hash=hash*31+(station.closed?1:0); hash=hash*31+station.trafficEvent; hash=hash*31+station.facLevel; hash=hash*31+station.riderBonus.GetHashCode(); }
            foreach (var v in researchVehicles) { hash=hash*31+v.node; hash=hash*31+v.lineId; }
            foreach (string key in techDone) hash=hash*31+key.GetHashCode();
            foreach (string ck in companyTechDone) hash=hash*31+ck.GetHashCode();
            foreach (var tl in talents) {
                hash = hash * 31 + tl.id; hash = hash * 31 + tl.level; hash = hash * 31 + (tl.flawCleansed ? 1 : 0);
                hash = hash * 31 + tl.assignKind; hash = hash * 31 + tl.assignLineId;
                if (!string.IsNullOrEmpty(tl.assignStationKey)) hash = hash * 31 + tl.assignStationKey.GetHashCode();
            }
            hash = hash * 31 + FoundCount();
            hash = hash * 31 + CrossBaySynergyLinkCount();
            return hash;
        }
    }
    int pendingForecastMask;
    int stagedInputSignature;
    System.Collections.Generic.IEnumerator<object> forecastWork;
    TrafficResult forecastWorkResult;

    int lastTrafficTopologySig;
    bool hasTrafficTopologySig;
    int stagedTopoSig;
    bool hasStagedTopoSig;
    readonly TrafficResult[] stagedSlots = new TrafficResult[4];
    static readonly float[] ForecastSlotHours = { 8f, 13f, 18f, 1f };
    static readonly float[] ForecastSlotFractions = { 4f / 24f, 6f / 24f, 4f / 24f, 10f / 24f };
    int TrafficTopologySignature()
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + (currentRegion != null ? currentRegion.GetHashCode() : 0);
            hash = hash * 31 + companyPolicy;
            hash = hash * 31 + economyCycle;
            hash = hash * 31 + (consultantDaysLeft > 0 ? consultantId + 100 : 0);
            foreach (int pid in boardPartners) hash = hash * 31 + (pid + 7);
            foreach (var line in lines)
            {
                hash = hash * 31 + line.GetHashCode(); hash = hash * 31 + line.trains; hash = hash * 31 + (line.hasBaseTrain ? 1 : 0); hash = hash * 31 + line.fare.GetHashCode();
                hash = hash * 31 + GetLineSkin(line);
                foreach (int v in line.st) hash = hash * 31 + v;
                foreach (int v in line.dispatch) hash = hash * 31 + v;
            }
            foreach (var station in stations)
            {
                hash = hash * 31 + station.level; hash = hash * 31 + (station.closed ? 1 : 0);
                hash = hash * 31 + station.trafficEvent;
            }
            foreach (var v in researchVehicles) if (v.lineId != 0) { hash = hash * 31 + v.node; hash = hash * 31 + v.lineId; }
            foreach (string key in techDone)
                if (historyByKey.TryGetValue(key, out var hn) && hn.loco < 0 && hn.effect != ResearchEffect.Vehicle && hn.effect != ResearchEffect.Track && hn.effect != ResearchEffect.None)
                    hash = hash * 31 + key.GetHashCode();
            foreach (string ck in companyTechDone)
                if (coTechByKey.TryGetValue(ck, out var cn) && (cn.effKind == "Speed" || cn.effKind == "Transfer" || cn.effKind == "Demand" || cn.effKind == "SceneryDemand" || cn.effKind == "StationCap" || cn.effKind == "Revenue" || cn.effKind == "Tourism" || string.IsNullOrEmpty(cn.effKind)))
                    hash = hash * 31 + ck.GetHashCode();
            foreach (var tl in talents)
            {
                hash = hash * 31 + tl.id; hash = hash * 31 + tl.level; hash = hash * 31 + (tl.flawCleansed ? 1 : 0);
                hash = hash * 31 + tl.assignKind; hash = hash * 31 + tl.assignLineId;
                if (!string.IsNullOrEmpty(tl.assignStationKey)) hash = hash * 31 + tl.assignStationKey.GetHashCode();
            }
            hash = hash * 31 + FoundCount();
            hash = hash * 31 + CrossBaySynergyLinkCount();
            return hash;
        }
    }
    TrafficResult ComputeSlotResult(int slot)
    {
        float savedClock = dayClock;
        try
        {
            dayClock = ForecastSlotHours[slot] * GameDaySeconds / 24f;
            return CalculateTraffic();
        }
        finally { dayClock = savedClock; }
    }
    void RecomputeForecastSlot(int slot)
    {
        var projected = ComputeSlotResult(slot);
        trafficSlots[slot] = projected;
        SaveActiveTraffic(projected, slot);
    }
    void SummarizeLineForecastsFromSlots()
    {
        lineForecasts.Clear();
        foreach (var line in lines) lineForecasts[line] = new LineForecastData();
        float savedClock = dayClock;
        try
        {
            for (int slot = 0; slot < 4; slot++)
            {
                var projected = trafficSlots[slot];
                if (projected == null) continue;
                dayClock = ForecastSlotHours[slot] * GameDaySeconds / 24f;
                float frac = ForecastSlotFractions[slot];
                foreach (var line in lines)
                {
                    var f = lineForecasts[line];
                    f.gross += TrafficRevenue(line, projected) * frac;
                    f.vehicle += VehicleMaintenance(line) * frac;
                    f.track += TrackMaintenance(line) * frac;
                }
            }
        }
        finally { dayClock = savedClock; }
        foreach (var f in lineForecasts.Values) f.tax = Mathf.Max(0f, f.gross - f.vehicle - f.track) * 0.1f;
    }
    void TickProgressiveForecasts()
    {
        if (pendingForecastMask == 0 || !hasStagedTopoSig) return;
        if (stagedInputSignature != ForecastSignature())
        {
            if (forecastWork != null) { forecastWork.Dispose(); forecastWork = null; }
            // Inputs changed while suspended. Discard all partial slots.
            pendingForecastMask = 0; hasStagedTopoSig = false;
            RebuildForecasts(true); return;
        }
        for (int slot = 0; slot < 4; slot++)
        {
            int bit = 1 << slot;
            if ((pendingForecastMask & bit) == 0) continue;
            if (forecastWork == null)
            {
                forecastWorkResult = null;
                forecastWork = CalculateTrafficSteps(value => forecastWorkResult = value, isolatedWorkspace: true);
            }
            var budget = System.Diagnostics.Stopwatch.StartNew();
            bool more;
            float savedClock = dayClock;
            try
            {
                dayClock = ForecastSlotHours[slot] * GameDaySeconds / 24f;
                do { more = forecastWork.MoveNext(); }
                while (more && budget.Elapsed.TotalMilliseconds < 5.5);
            }
            finally { dayClock = savedClock; }
            if (more) return;
            forecastWork.Dispose(); forecastWork = null;
            pendingForecastMask &= ~bit;
            stagedSlots[slot] = forecastWorkResult;
            if (pendingForecastMask == 0)
            {
                for (int s = 0; s < 4; s++)
                {
                    if (stagedSlots[s] != null)
                    {
                        trafficSlots[s] = stagedSlots[s];
                        SaveActiveTraffic(stagedSlots[s], s);
                        stagedSlots[s] = null;
                    }
                }
                lastTrafficTopologySig = stagedTopoSig;
                hasTrafficTopologySig = true;
                lastForecastDuration = Mathf.Clamp(Time.unscaledTime - forecastStartUnscaledTime, 0.25f, 8f);
                forecastSignature = ForecastSignature();
                hasForecastSignature = true;
                SummarizeLineForecastsFromSlots();
                traffic = trafficSlots[DispatchSlot()];
                RebuildOperatingRates();
                float total = 0f;
                foreach (var line in lines) total += ForecastFor(line).Profit;
                total += StationCommercialTotalDaily() + TourismDividendDaily() + CycleDailySubsidy() + CrossBaySynergyDailyBonus();
                income = total;
                RefreshHUD();
                pageForecastRefresh?.Invoke();
                panelForecastRefresh?.Invoke();
                if (pageRoot == null && sceneryViewer == null)
                {
                    UpdateLineTrafficColors();
                    MarkRingsDirty();
                }
            }
            break;
        }
    }
    void RebuildForecasts(bool allowProgressive = false)
    {
        InitHistoryTree(); SyncResearchEffects(); EnsureCoTechIndexBuilt();
        int signature = ForecastSignature();
        if (hasForecastSignature && signature == forecastSignature && pendingForecastMask == 0) return;
        int topoSig = TrafficTopologySignature();
        bool topoSame = hasTrafficTopologySig && topoSig == lastTrafficTopologySig && trafficSlots[DispatchSlot()] != null && pendingForecastMask == 0;
        if (topoSame)
        {
            // 仅维护/造价/商业设施等纯经济参数变化：0 次 Dijkstra，毫秒级重算利润！
            SummarizeLineForecastsFromSlots();
            forecastSignature = signature; hasForecastSignature = true;
            if (!estimatingResearchProfit) unchecked { researchForecastEpoch++; }
            return;
        }
        bool progressive = allowProgressive && Application.isPlaying && !estimatingResearchProfit && hasTrafficTopologySig;
        if (progressive)
        {
            if (hasStagedTopoSig && stagedTopoSig == topoSig && stagedInputSignature == signature && pendingForecastMask != 0)
            {
                // 同一拓扑版本正在分帧推进中（如连续点击维护类科技），保持已有暂存进度，不丢弃待算时段！
                SummarizeLineForecastsFromSlots();
                if (!estimatingResearchProfit) unchecked { researchForecastEpoch++; }
                return;
            }
            if (forecastWork != null) { forecastWork.Dispose(); forecastWork = null; }
            stagedInputSignature = signature;
            stagedTopoSig = topoSig;
            hasStagedTopoSig = true;
            pendingForecastMask = 15;
            forecastStartUnscaledTime = Time.unscaledTime;
            for (int s = 0; s < 4; s++) stagedSlots[s] = null;
            SummarizeLineForecastsFromSlots();
            // 注意：此时不把主 forecastSignature / lastTrafficTopologySig 标成新版本，等 4 时段全齐后再原子提交！
            if (!estimatingResearchProfit) unchecked { researchForecastEpoch++; }
            return;
        }
        if (forecastWork != null) { forecastWork.Dispose(); forecastWork = null; }
        pendingForecastMask = 0;
        hasStagedTopoSig = false;
        for (int slot = 0; slot < 4; slot++) RecomputeForecastSlot(slot);
        SummarizeLineForecastsFromSlots();
        forecastSignature = signature; hasForecastSignature = true;
        lastTrafficTopologySig = topoSig; hasTrafficTopologySig = true;
        // Explicit rebuilds can follow invalidations beyond the signature fields.
        // Speculative research/restore passes must not evict other node estimates.
        if (!estimatingResearchProfit) unchecked { researchForecastEpoch++; }
    }
    LineForecastData ForecastFor(Line line) { return lineForecasts.TryGetValue(line,out var f) ? f : new LineForecastData(); }
    struct OperatingRates { public float gross, vehicle, track; }
    readonly Dictionary<Line, OperatingRates> operatingRates = new Dictionary<Line, OperatingRates>();
    void RebuildOperatingRates()
    {
        operatingRates.Clear();
        foreach (var line in lines) operatingRates[line] = new OperatingRates {
            gross = TrafficRevenue(line, traffic), vehicle = VehicleMaintenance(line), track = TrackMaintenance(line)
        };
    }
    float VehicleMaintenance(Line line) {
        float skinMult = SkinCategory(GetLineSkin(line)) == 3 ? (DispatchSlot() == 3 ? 0.81f : 0.90f) : 1f;
        float policyMult = (companyPolicy == 2 ? 0.88f : 1f) * PartnerVehicleMaintFactor() * LineTalentMaintMult(line);
        float lenFactor = 1f + EconomicPathLength(line.st) / 1200f;
        return (line.trains * 10f + RunningTrains(line) * 45f) * lenFactor * FleetTrait(line, 0, false) * ResearchCostFactor(ResearchEffect.Vehicle) * skinMult * policyMult;
    }
    float TrackMaintenance(Line line) {
        int cat = SkinCategory(GetLineSkin(line));
        float skinMult = cat == 2 ? 0.88f : cat == 1 ? 1.04f : 1f;
        float policyMult = (companyPolicy == 2 ? 0.88f : 1f) * PartnerTrackMaintFactor() * CycleTrackMaintFactor() * LineTalentMaintMult(line);
        float len = EconomicPathLength(line.st);
        int stops = UniqueCount(line.st);
        float baseTrack = 32f + len * 0.30f + stops * 8f;
        float longHaul = Mathf.Pow(Mathf.Max(0f, len - 250f), 1.38f) * 0.18f + Mathf.Max(0, stops - 4) * 35f;
        float networkScale = 1f + Mathf.Clamp(lines.Count - 2, 0, 20) * 0.03f;
        return (baseTrack + longHaul) * networkScale * ResearchCostFactor(ResearchEffect.Track) * skinMult * policyMult;
    }
    float TrafficRevenue(Line line, TrafficResult result)
    {
        return result != null && result.lineRevenue.TryGetValue(line,out float value) ? value : 0f;
    }
    int OptimizeAllLinesDispatch()
    {
        int adjusted = 0;
        if (lines.Count == 0) return 0;
        var backup = new Dictionary<Line, int[]>();
        foreach (var l in lines) backup[l] = (int[])l.dispatch.Clone();
        float savedClock = dayClock;
        float[] hours = { 8f, 13f, 18f, 1f };
        try
        {
            foreach (var l in lines)
                if (PathTouchesRegion(l.st, currentRegion))
                    for (int s = 0; s < 4; s++) l.dispatch[s] = Mathf.Max(1, l.trains);

            for (int slot = 0; slot < 4; slot++)
            {
                dayClock = hours[slot] * GameDaySeconds / 24f;
                var proj = CalculateTraffic();
                foreach (var l in lines)
                {
                    if (!PathTouchesRegion(l.st, currentRegion) || l.trains <= 0) continue;
                    int maxT = l.trains;
                    float peak = proj.linePeakDemand.TryGetValue(l, out float p) ? p : 0f;
                    float perTrainCap = FleetCapacity(l) / Mathf.Max(1, RunningTrains(l));
                    int needed = peak <= 1f ? 1 : Mathf.Clamp(Mathf.CeilToInt(peak / Mathf.Max(1f, perTrainCap)), 1, maxT);
                    l.dispatch[slot] = needed;
                }
            }
            foreach (var l in lines)
            {
                if (!PathTouchesRegion(l.st, currentRegion)) continue;
                var old = backup[l];
                bool changed = false;
                for (int s = 0; s < 4; s++) if (old[s] != l.dispatch[s]) { changed = true; break; }
                if (changed) adjusted++;
            }
        }
        finally { dayClock = savedClock; }
        hasForecastSignature = false;
        operatingRates.Clear();
        RecalcIncome();
        return adjusted;
    }
    string StationCompassHint(int index)
    {
        if (index < 0 || index >= stations.Count) return "";
        string region = stations[index].region;
        var p1 = PosInRegion(index, region);
        int bestA = -1, bestB = -1;
        float scoreA = -1f, scoreB = -1f;
        for (int j = 0; j < stations.Count; j++)
        {
            if (j == index || stations[j].region != region || stations[j].closed) continue;
            float dist = Mathf.Max(24f, Vector2.Distance(p1, PosInRegion(j, region)));
            float sc = StationBaselineDemand(j) / (36f + dist);
            if (!stations[j].built) sc *= 1.15f;
            if (sc > scoreA) { scoreB = scoreA; bestB = bestA; scoreA = sc; bestA = j; }
            else if (sc > scoreB) { scoreB = sc; bestB = j; }
        }
        if (bestA < 0) return "";
        string res = StationDisplayName(bestA, region);
        if (bestB >= 0) res += "、" + StationDisplayName(bestB, region);
        return res;
    }

    float[] draftStationNeed;
    int draftNeedAnchor = -1;
    string draftNeedRegion = "";
    readonly HashSet<int> draftNeedExcluded = new HashSet<int>();

    // 根据锚点星港的 OD 欲望、未直达缺口、星港饱和度与既有共线拥挤度，预计算同区各候选港的铺航线需求强度 [0, 1]。
    void PrepareDraftNeedField(int anchorIdx, List<int> currentDraft)
    {
        int n = stations.Count;
        if (draftStationNeed == null || draftStationNeed.Length != n) draftStationNeed = new float[n];
        for (int i = 0; i < n; i++) draftStationNeed[i] = -1f;
        draftNeedExcluded.Clear();
        if (currentDraft != null)
            for (int i = 0; i < currentDraft.Count; i++) draftNeedExcluded.Add(currentDraft[i]);
        draftNeedAnchor = anchorIdx;
        draftNeedRegion = currentRegion;
        if (anchorIdx < 0 || anchorIdx >= n) return;

        if (traffic != null && traffic.stationTopDests != null)
            traffic.stationTopDests.TryGetValue(anchorIdx, out anchorOdMapTemp);

        Vector2 anchorPos = PosInRegion(anchorIdx, currentRegion);
        float baseAnchor = Mathf.Max(0.4f, StationBaselineDemand(anchorIdx));
        float anchorPressure = Mathf.Clamp01(StationLossRatio(anchorIdx) * 0.7f + Mathf.Max(0f, StationWaitRatio(anchorIdx) - 0.5f) * 0.6f);
        var raw = new float[n];
        float minRaw = float.MaxValue, maxRaw = float.MinValue;
        int validCount = 0;

        for (int j = 0; j < n; j++)
        {
            bool loopStart = currentDraft != null && currentDraft.Count >= 3 && j == currentDraft[0];
            if (j == anchorIdx || stations[j].closed || (draftNeedExcluded.Contains(j) && !loopStart)) continue;
            if (!StationInRegion(j, currentRegion) && SegmentRegion(anchorIdx, j) == null) continue;

            float dist = Vector2.Distance(anchorPos, PosInRegion(j, currentRegion));
            if (dist < 12f) continue;
            float baseJ = Mathf.Max(0.25f, StationBaselineDemand(j));
            float gravity = Mathf.Sqrt(baseAnchor * baseJ) / (46f + dist * 0.42f);

            float unmetOd = 0f;
            if (anchorOdMapTemp != null)
            {
                for (int t = 0; t < anchorOdMapTemp.Count; t++)
                {
                    var df = anchorOdMapTemp[t];
                    if (df.dest != j) continue;
                    unmetOd = (!df.reachable ? 0.80f : df.crowded ? 0.60f : 0.08f) * Mathf.Clamp01(df.desired / 380f);
                    break;
                }
            }

            float waitJ = StationWaitRatio(j);
            float lossJ = StationLossRatio(j);
            int stLinesJ = StationLineCount(j);
            float gapBonus = (!stations[j].built || stLinesJ == 0) ? (0.34f + Mathf.Clamp01(baseJ / 16f) * 0.34f) : 0f;
            float hubBonus = (StationKind(j) == 2 || StationKind(j) == 4) ? 0.16f : (StationHasScenery(j) ? 0.11f : 0f);
            float crowdBonus = Mathf.Clamp01(Mathf.Max(waitJ - 0.35f, 0f) * 0.72f + lossJ * 0.85f) * 0.55f + anchorPressure * 0.16f;

            bool hasDirect = false;
            float maxSharedLoad = 0f;
            for (int l = 0; l < lines.Count; l++)
            {
                var line = lines[l];
                for (int k = 0; k < line.st.Count - 1; k++)
                {
                    int a = line.st[k], b = line.st[k + 1];
                    if (!((a == anchorIdx && b == j) || (a == j && b == anchorIdx))) continue;
                    hasDirect = true;
                    if (traffic != null && traffic.lineSegLoad != null)
                    {
                        string sk = line.id + ":" + Mathf.Min(a, b) + ":" + Mathf.Max(a, b);
                        if (traffic.lineSegLoad.TryGetValue(sk, out float loadVal) && loadVal > maxSharedLoad)
                            maxSharedLoad = loadVal;
                    }
                }
            }

            float score = gravity * 1.45f + unmetOd + gapBonus + hubBonus + crowdBonus;
            if (stations[anchorIdx].portal && stations[j].portal) score *= 0.12f;
            if (hasDirect)
            {
                float relief = maxSharedLoad >= 0.78f ? Mathf.Lerp(0.75f, 1.35f, Mathf.Clamp01((maxSharedLoad - 0.78f) / 0.35f)) : 0.24f;
                score *= relief;
            }
            raw[j] = score;
            if (score < minRaw) minRaw = score;
            if (score > maxRaw) maxRaw = score;
            validCount++;
        }

        if (validCount <= 0) return;
        float span = Mathf.Max(0.18f, maxRaw - minRaw);
        for (int j = 0; j < n; j++)
        {
            if (raw[j] <= 0f && (!StationInRegion(j, currentRegion) || j == anchorIdx)) continue;
            if (raw[j] <= 0f) continue;
            float absScore = Mathf.Clamp01((raw[j] - 0.10f) / 1.15f);
            float relScore = Mathf.Clamp01((raw[j] - minRaw) / span);
            draftStationNeed[j] = Mathf.Clamp01(relScore * 0.62f + absScore * 0.38f);
        }
        anchorOdMapTemp = null;
    }

    List<TrafficDestFlow> anchorOdMapTemp;

    float DraftPairNeedScore(int fromIdx, int toIdx)
    {
        if (fromIdx != draftNeedAnchor || draftNeedRegion != currentRegion || draftStationNeed == null || draftStationNeed.Length != stations.Count)
            PrepareDraftNeedField(fromIdx, draft);
        if (toIdx >= 0 && toIdx < draftStationNeed.Length && draftStationNeed[toIdx] >= 0f)
            return draftStationNeed[toIdx];
        return 0.25f;
    }

    // 轻量估算连接候选星港后可新增的日均直达客流（人次/日），用于拖拽徽标即时决策提示。
    int EstimateDraftAddedPassengers(List<int> currentDraft, int candToIdx)
    {
        if (candToIdx < 0 || candToIdx >= stations.Count || currentDraft == null || currentDraft.Count == 0) return 0;
        int anchorIdx = currentDraft[currentDraft.Count - 1];
        if (anchorIdx < 0 || anchorIdx >= stations.Count || anchorIdx == candToIdx) return 0;

        float totalGain = 0f;
        float toWeight = Mathf.Max(20f, StationBaseDemand(candToIdx));
        for (int i = 0; i < currentDraft.Count; i++)
        {
            int src = currentDraft[i];
            if (src < 0 || src >= stations.Count || src == candToIdx) continue;
            float dist = Mathf.Max(26f, Vector2.Distance(PosInRegion(src, currentRegion), PosInRegion(candToIdx, currentRegion)));
            float srcWeight = Mathf.Max(20f, StationBaseDemand(src));
            float basePair = Mathf.Sqrt(srcWeight * toWeight) * (240f / (64f + dist * 0.55f));
            if (stations[src].portal && stations[candToIdx].portal) basePair *= 0.12f;

            bool directExists = false;
            float sharedLoad = 0f;
            for (int l = 0; l < lines.Count; l++)
            {
                var line = lines[l];
                bool hasS = false, hasT = false;
                for (int k = 0; k < line.st.Count; k++)
                {
                    if (line.st[k] == src) hasS = true;
                    if (line.st[k] == candToIdx) hasT = true;
                }
                if (hasS && hasT)
                {
                    directExists = true;
                    float peak = LinePeakSectionDemand(line);
                    float cap = Mathf.Max(1f, FleetCapacity(line));
                    sharedLoad = Mathf.Max(sharedLoad, peak / cap);
                }
            }
            float factor = directExists ? (sharedLoad >= 0.72f ? Mathf.Lerp(0.35f, 0.85f, Mathf.Clamp01(sharedLoad - 0.72f)) : 0.14f) : 1f;
            if (!stations[candToIdx].built) factor *= 1.25f;
            totalGain += basePair * factor;
        }
        return Mathf.Clamp(Mathf.RoundToInt(totalGain / 10f) * 10, 20, 9900);
    }

    // 随玩家拖拽方向连续插值计算走廊客流需求热度：偏冷清方向呈翠绿，高缺口/繁忙走廊呈赤红。
    float EvaluateDragDirectionNeed(int anchorIdx, Vector2 dragPos)
    {
        if (anchorIdx < 0 || anchorIdx >= stations.Count) return 0.35f;
        if (anchorIdx != draftNeedAnchor || draftNeedRegion != currentRegion || draftStationNeed == null || draftStationNeed.Length != stations.Count)
            PrepareDraftNeedField(anchorIdx, draft);

        Vector2 anchorPos = PosInRegion(anchorIdx, currentRegion);
        Vector2 delta = dragPos - anchorPos;
        float dragDist = delta.magnitude;
        float anchorFallback = Mathf.Clamp(0.22f + StationLossRatio(anchorIdx) * 0.45f + Mathf.Max(0f, StationWaitRatio(anchorIdx) - 0.4f) * 0.35f, 0.14f, 0.75f);
        if (dragDist < 10f) return anchorFallback;

        Vector2 dragDir = delta / dragDist;
        float targetReach = Mathf.Max(14f, dragDist);
        float distScale = Mathf.Clamp(targetReach * 0.52f, 28f, 150f);
        float wSum = 0f, valSum = 0f;
        for (int j = 0; j < draftStationNeed.Length; j++)
        {
            float stNeed = draftStationNeed[j];
            if (stNeed < 0f) continue;
            Vector2 dJ = PosInRegion(j, currentRegion) - anchorPos;
            float distJ = dJ.magnitude;
            if (distJ < 12f) continue;
            float cos = Vector2.Dot(dragDir, dJ / distJ);
            if (cos <= 0.15f) continue;
            float dirWeight = Mathf.Pow(Mathf.Clamp01((cos - 0.15f) / 0.85f), 4.8f);
            float reachWeight = Mathf.Exp(-Mathf.Abs(distJ - targetReach) / distScale) / (18f + distJ * 0.12f);
            float w = dirWeight * reachWeight;
            if (w <= 0.0001f) continue;
            wSum += w;
            valSum += w * stNeed;
        }
        if (wSum <= 0.0001f) return 0.10f;
        float dirNeed = valSum / wSum;
        float lengthConfidence = Mathf.Clamp01((dragDist - 4f) / 24f);
        return Mathf.Clamp01(Mathf.Lerp(anchorFallback, dirNeed, lengthConfidence));
    }

    static Color NeedColorGradient(float need)
    {
        float t = Mathf.Clamp01(need);
        Color green = new Color(0.18f, 0.72f, 0.44f, 0.94f);
        Color amber = new Color(0.96f, 0.72f, 0.18f, 0.95f);
        Color red = new Color(0.94f, 0.25f, 0.20f, 0.98f);
        return t < 0.5f
            ? Color.Lerp(green, amber, t * 2f)
            : Color.Lerp(amber, red, (t - 0.5f) * 2f);
    }
}
