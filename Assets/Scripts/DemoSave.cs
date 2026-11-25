using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public partial class DemoBootstrap
{
    const string SaveKey = "starline_moon_v1";
    const string BundledTestSaveResource = "TestSaves/railway2_local_v1";
#if UNITY_EDITOR
    const string DevTestSaveSyncPref = "Railway2.DevTestSaveSync";
    const string BundledTestSaveAssetPath = "Assets/Resources/TestSaves/railway2_local_v1.json";
    static bool DevLocalOnlyRun
    {
        get { return UnityEditor.EditorPrefs.GetBool("Railway2.DevLocalOnlyRun", false); }
        set { UnityEditor.EditorPrefs.SetBool("Railway2.DevLocalOnlyRun", value); }
    }
    public static bool DevTestSaveSyncEnabled
    {
        get { return !DevLocalOnlyRun && UnityEditor.EditorPrefs.GetBool(DevTestSaveSyncPref, true); }
        set { DevLocalOnlyRun = false; UnityEditor.EditorPrefs.SetBool(DevTestSaveSyncPref, value); }
    }
    public static bool DevTestSaveDirty;

    static string ReadDevTestSaveFromDisk()
    {
        try
        {
            if (File.Exists(BundledTestSaveAssetPath))
                return File.ReadAllText(BundledTestSaveAssetPath, System.Text.Encoding.UTF8);
        }
        catch (Exception e)
        {
            Debug.LogWarning("[Railway2] 读取开发测试存档文件失败：" + e.Message);
        }
        return null;
    }

    static void WriteDevTestSaveToDisk(SaveData data)
    {
        if (data == null) return;
        try
        {
            string prettyJson = JsonUtility.ToJson(data, true) + "\n";
            string dir = Path.GetDirectoryName(BundledTestSaveAssetPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
            if (File.Exists(BundledTestSaveAssetPath))
            {
                string prev = File.ReadAllText(BundledTestSaveAssetPath, System.Text.Encoding.UTF8);
                if (prev == prettyJson) return;
            }
            WriteSaveJsonAtomically(BundledTestSaveAssetPath, prettyJson);
            DevTestSaveDirty = true;
            if (!Application.isPlaying) UnityEditor.AssetDatabase.ImportAsset(BundledTestSaveAssetPath);
            else UnityEditor.EditorApplication.delayCall += () => {
                if (!Application.isPlaying && File.Exists(BundledTestSaveAssetPath))
                    UnityEditor.AssetDatabase.ImportAsset(BundledTestSaveAssetPath);
            };
        }
        catch (Exception e)
        {
            Debug.LogError("[Railway2] 写入开发测试存档文件失败：" + e.Message);
        }
    }
#endif
    static void WriteSaveJsonAtomically(string path, string json)
    {
        string directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
        string temporary = path + ".tmp";
        try
        {
            File.WriteAllText(temporary, json, new System.Text.UTF8Encoding(false));
            if (File.Exists(path)) File.Replace(temporary, path, null);
            else File.Move(temporary, path);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
    [Serializable] sealed class SavedStation { public string key, customName; public int level, trafficEvent; public bool built, closed; public float riderBonus; public int facLevel; }
    [Serializable] sealed class SavedScenery { public string key; public bool found, unlocked; }
    [Serializable] sealed class SavedLine { public int id, trains, todayRevenue; public int skin = -1; public bool baseRemoved; public string name; public Color color; public List<string> stations; public List<SegmentWaypoints> segWaypoints; public int[] dispatch; public float fare, todayGross, todayMaintenance, todayTrack; public List<int> recent; }
    [Serializable] sealed class SavedVehicle { public int node, lineId; }
    [Serializable] sealed class SavedFrozenLine { public int lineId; public float[] revenue, demand, served, peak; public bool[] valid; }
    [Serializable] sealed class SaveData {
        public int version=1, day, speed, nextId, dailyBaseline, theme; public double coins; public float diamonds, rp, clock, researchClock;
        public bool moonFar, marsWest, loan, music; public string region;
        public List<SavedStation> stations = new List<SavedStation>(); public List<SavedScenery> scenery = new List<SavedScenery>();
        public List<SavedLine> lines = new List<SavedLine>(); public List<SavedVehicle> vehicles = new List<SavedVehicle>();
        public List<string> tech = new List<string>(), story = new List<string>(); public List<int> skins = new List<int>();
        public List<int> themes = new List<int>();
        public List<string> adKeys = new List<string>(); public List<int> adCounts = new List<int>();
        public List<int> trialThemes = new List<int>(); public List<long> trialEnds = new List<long>();
        public string companyName = "深空轨社";
        public int companySeal, companyPolicy, bondDaysLeft, lastPolicyDay = -1, lastAdSubsidyDay = -1, dailyTaskClaimedDay = -1;
        public int bondPrincipal, bondDailyDue, bondsRepaid, consultantId = -1, consultantDaysLeft, economyCycle, nextEconomyCycle = 1, turboDaysLeft, autoDispatchDaysLeft, lastAwardDay;
        public bool bondSubsidized;
        public List<int> boardPartners = new List<int>();
        public List<MunicipalContract> contracts = new List<MunicipalContract>();
        // M1-M3 新增向后兼容字段
        public int storyStage;
        public bool tutorialSkipped;
        public List<string> coTech = new List<string>();
        public List<SavedTalent> talents = new List<SavedTalent>();
        public List<SavedTalent> talentPool = new List<SavedTalent>();
        public int nextTalentId = 1, lastTalentRefreshDay = -1;
        public int lastReadNoticeDay = -1;
        public string readPatchNoteVersion = "";
        public List<string> memorialPlaques = new List<string>();
        public List<string> memorialTickets = new List<string>();
        public int retiredVehiclesCount;
        public int lastDisplayedSolarTerm = -1, nextContractId = 1;
        public bool hasYesterdayReport;
        public DailyReportSnapshot yesterdayReport;
        public List<SavedFrozenLine> frozenLines = new List<SavedFrozenLine>();
        public List<string> unlockedRegions = new List<string>();
    }
    bool saveReady, saveBlocked; float autosaveTimer;
    string StationSaveKey(Station station) { return station.region + ":" + station.name; }
    string ScenerySaveKey(Scenery scenery) { return scenery.region + ":" + scenery.name; }
    static readonly Dictionary<string, string> LegacyPlaceNames = new Dictionary<string, string> { };
    static string NormalizePlaceName(string value)
    {
        return value != null && LegacyPlaceNames.TryGetValue(value, out var current) ? current : value;
    }
    static string NormalizePlaceKey(string key)
    {
        if (string.IsNullOrEmpty(key)) return key;
        int colon = key.IndexOf(':');
        if (colon < 0) return NormalizePlaceName(key);
        string region = NormalizePlaceName(key.Substring(0, colon));
        string name = NormalizePlaceName(key.Substring(colon + 1));
        return region + ":" + name;
    }
    static void NormalizeSavedPlaces(SaveData data)
    {
        data.region = NormalizePlaceName(data.region);
        if (data.stations != null) foreach (var item in data.stations) if (item != null) item.key = NormalizePlaceKey(item.key);
        if (data.scenery != null) foreach (var item in data.scenery) if (item != null) item.key = NormalizePlaceKey(item.key);
        if (data.lines != null) foreach (var line in data.lines)
            if (line != null && line.stations != null) for (int i = 0; i < line.stations.Count; i++) line.stations[i] = NormalizePlaceKey(line.stations[i]);
        if (data.talents != null) foreach (var item in data.talents) if (item != null) item.assignStationKey = NormalizePlaceKey(item.assignStationKey);
        if (data.talentPool != null) foreach (var item in data.talentPool) if (item != null) item.assignStationKey = NormalizePlaceKey(item.assignStationKey);
    }
    bool RestoreGame()
    {
        string rawJson = null;
#if UNITY_EDITOR
        if (DevTestSaveSyncEnabled)
        {
            string diskJson = ReadDevTestSaveFromDisk();
            if (!string.IsNullOrEmpty(diskJson))
            {
                rawJson = diskJson;

            }
        }
#endif
        if (string.IsNullOrEmpty(rawJson))
        {
            if (!PlayerPrefs.HasKey(SaveKey)) return false;
            rawJson = PlayerPrefs.GetString(SaveKey);
        }
        if (stations == null || stations.Count == 0) BuildData();
        try {
            var data = JsonUtility.FromJson<SaveData>(rawJson);
            if (data == null || data.version != 1 || data.stations == null || data.lines == null || data.scenery == null || data.tech == null || data.vehicles == null || data.story == null || data.skins == null) throw new Exception("invalid save");
            ApplySaveData(data);
            EnsureCompanyInitialized();
            EnsureTalentsInitialized();
            SyncStoryProgression(false);
            SyncCulturalCollection(false);
            lastPolicyDay = data.lastPolicyDay; lastAdSubsidyDay = data.lastAdSubsidyDay; dailyTaskClaimedDay = data.dailyTaskClaimedDay;
            savedRegion=data.region;
            PlayerPrefs.SetString(SaveKey, rawJson);
            PlayerPrefs.Save();
            saveBlocked = false;
            saveReady = true;
            return true;
        } catch (Exception e) { saveBlocked=true; Debug.LogError("本地存档读取失败，已保留原档：" + e.Message); return false; }
    }
    // Data restoration is shared by disk loading and the managed round-trip audit.
    void ApplySaveData(SaveData data)
    {
        NormalizeSavedPlaces(data);
        var byKey = new Dictionary<string,int>(); for (int i=0;i<stations.Count;i++) byKey[StationSaveKey(stations[i])] = i;
        var restoredLines = new List<Line>(); var ids = new HashSet<int>();
        foreach (var record in data.lines) {
            if(record == null || record.stations == null || record.stations.Count < 2 || record.dispatch == null || record.dispatch.Length != 4 || !ids.Add(record.id)) continue;
            var line = new Line { id=record.id, name=record.name, color=record.color, skin=record.skin, trains=record.trains, hasBaseTrain=!record.baseRemoved, dispatch=record.dispatch, fare=record.fare, todayRevenue=record.todayRevenue, todayGross=record.todayGross, todayMaintenance=record.todayMaintenance, todayTrack=record.todayTrack, recent=record.recent ?? new List<int>() };
            if (record.segWaypoints != null) line.segWaypoints = CloneWaypointsList(record.segWaypoints);
            bool valid = true;
            foreach (string key in record.stations) {
                if(!byKey.TryGetValue(key,out int index)) { valid = false; break; }
                line.st.Add(index);
            }
            if (valid && line.st.Count >= 2)
            {
                // 清洗历史脏数据中的锐角折返尖刺拐点
                if (line.segWaypoints != null)
                {
                    for (int s = 0; s < line.segWaypoints.Count && s < line.st.Count - 1; s++)
                    {
                        var sw = line.segWaypoints[s];
                        if (sw == null || sw.pts == null || sw.pts.Count == 0) continue;
                        string reg = SegmentRegion(line.st[s], line.st[s + 1]) ?? currentRegion;
                        Vector2 a = PosInRegion(line.st[s], reg);
                        Vector2 b = PosInRegion(line.st[s + 1], reg);
                        for (int k = sw.pts.Count - 1; k >= 0; k--)
                        {
                            Vector2 prev = k > 0 ? sw.pts[k - 1] : a;
                            Vector2 next = k + 1 < sw.pts.Count ? sw.pts[k + 1] : b;
                            Vector2 d1 = (sw.pts[k] - prev).normalized;
                            Vector2 d2 = (next - sw.pts[k]).normalized;
                            if (Vector2.Dot(d1, d2) < -0.35f || Vector2.Distance(sw.pts[k], prev) < 16f || Vector2.Distance(sw.pts[k], next) < 16f)
                                sw.pts.RemoveAt(k);
                        }
                    }
                }
                restoredLines.Add(line);
            }
        }
        foreach(var record in data.stations) {
            if (record != null && byKey.TryGetValue(record.key, out int sIdx)) {
                var station=stations[sIdx]; station.level=record.level; station.trafficEvent=record.trafficEvent; station.built=record.built; station.closed=record.closed; station.riderBonus=record.riderBonus; station.facLevel=record.facLevel; station.customName=string.IsNullOrWhiteSpace(record.customName) ? "" : record.customName.Trim();
            }
        }
        var sceneryByKey = new Dictionary<string,Scenery>(); foreach(var item in sceneries) sceneryByKey[ScenerySaveKey(item)] = item;
        foreach(var record in data.scenery) if(record != null && sceneryByKey.TryGetValue(record.key,out var item)) { item.found=record.found; item.unlocked=record.unlocked; }
        pendingCheckin.Clear();
        foreach(var sc in sceneries) if(sc.unlocked && !sc.found && TryGetSceneryContent(sc, out _, out _)) pendingCheckin.Add(sc);
        lines.Clear(); lines.AddRange(restoredLines);
        frozenTraffic.Clear();
        if (data.frozenLines != null)
        {
            foreach (var fl in data.frozenLines)
            {
                if (fl == null) continue;
                var line = restoredLines.Find(l => l.id == fl.lineId);
                if (line == null) continue;
                var ft = new FrozenLineTraffic();
                if (fl.revenue != null && fl.revenue.Length == 4) Array.Copy(fl.revenue, ft.revenue, 4);
                if (fl.demand != null && fl.demand.Length == 4) Array.Copy(fl.demand, ft.demand, 4);
                if (fl.served != null && fl.served.Length == 4) Array.Copy(fl.served, ft.served, 4);
                if (fl.peak != null && fl.peak.Length == 4) Array.Copy(fl.peak, ft.peak, 4);
                if (fl.valid != null && fl.valid.Length == 4) Array.Copy(fl.valid, ft.valid, 4);
                frozenTraffic[line] = ft;
            }
        }
        foreach (var l in restoredLines) foreach (int stIdx in l.st) if (stIdx >= 0 && stIdx < stations.Count) stations[stIdx].built = true;
        techDone.Clear(); techDone.AddRange(data.tech);
        researchVehicles.Clear(); foreach(var item in data.vehicles)
            if (item != null && item.node >= 0 && item.node < LocoNames.Length)
                researchVehicles.Add(new ResearchVehicle { node=item.node, lineId=restoredLines.Exists(l => l.id == item.lineId) ? item.lineId : 0 });
        foreach (var l in restoredLines) SyncLineFleet(l);
        storySeen.Clear(); foreach(var item in data.story) storySeen.Add(item); ownedLineSkins.Clear(); foreach(int item in data.skins) ownedLineSkins.Add(item);
        ownedThemes.Clear(); ownedThemes.Add(0);
        if (data.themes != null) foreach (int item in data.themes) if (item >= 0 && item < Themes.Length) ownedThemes.Add(item);
        adWatchCounts.Clear();
        if (data.adKeys != null && data.adCounts != null)
            for (int i = 0; i < Mathf.Min(data.adKeys.Count, data.adCounts.Count); i++)
                if (!string.IsNullOrEmpty(data.adKeys[i]) && data.adCounts[i] > 0) adWatchCounts[data.adKeys[i]] = data.adCounts[i];
        themeTrialUntil.Clear();
        if (data.trialThemes != null && data.trialEnds != null)
            for (int i = 0; i < Mathf.Min(data.trialThemes.Count, data.trialEnds.Count); i++)
                if (data.trialEnds[i] > 0) themeTrialUntil[data.trialThemes[i]] = data.trialEnds[i];
        currentTheme = Mathf.Clamp(data.theme, 0, Themes.Length - 1);
        if (!IsThemeAvailable(currentTheme)) currentTheme = 0;
        coins=data.coins; diamonds=data.diamonds; rp=data.rp; day=Mathf.Max(1,data.day); dayClock=data.clock; researchClock=data.researchClock; timeSpeed=data.speed;
        nextLineId=data.nextId; dailyStationBaseline=data.dailyBaseline; moonFarUnlocked=data.moonFar; marsWestUnlocked=data.marsWest; loanOutstanding=data.loan; musicEnabled=data.music;
        unlockedRegions.Clear();
        unlockedRegions.Add("月之亮面");
        if (moonFarUnlocked) unlockedRegions.Add("月背");
        if (marsWestUnlocked) unlockedRegions.Add("火星·西半球");
        if (data.unlockedRegions != null)
            for (int ri = 0; ri < data.unlockedRegions.Count; ri++)
                SetRegionUnlocked(data.unlockedRegions[ri], true);
        companyName = string.IsNullOrWhiteSpace(data.companyName) ? "深空轨社" : data.companyName.Trim();
        companySeal = Mathf.Clamp(data.companySeal, 0, CompanySealChars.Length - 1);
        companyPolicy = Mathf.Clamp(data.companyPolicy, 0, CompanyPolicyNames.Length - 1);
        bondDaysLeft = Mathf.Max(0, data.bondDaysLeft);
        bondPrincipal = bondDaysLeft > 0 ? (data.bondPrincipal > 0 ? data.bondPrincipal : BondPrincipal) : 0;
        bondDailyDue = bondDaysLeft > 0 ? (data.bondDailyDue > 0 ? data.bondDailyDue : BondDailyPayment) : 0;
        bondsRepaid = Mathf.Max(0, data.bondsRepaid);
        bondSubsidized = data.bondSubsidized;
        consultantId = data.consultantDaysLeft > 0 ? Mathf.Clamp(data.consultantId, 0, PartnerNames.Length - 1) : -1;
        consultantDaysLeft = Mathf.Max(0, data.consultantDaysLeft);
        economyCycle = Mathf.Clamp(data.economyCycle, 0, EconomyCycleNames.Length - 1);
        nextEconomyCycle = Mathf.Clamp(data.nextEconomyCycle, 0, EconomyCycleNames.Length - 1);
        turboDaysLeft = Mathf.Max(0, data.turboDaysLeft);
        autoDispatchDaysLeft = Mathf.Max(0, data.autoDispatchDaysLeft);
        lastAwardDay = Mathf.Max(0, data.lastAwardDay);
        boardPartners.Clear();
        if (data.boardPartners != null) foreach (int pid in data.boardPartners) if (pid >= 0 && pid < 6 && !boardPartners.Contains(pid)) boardPartners.Add(pid);
        nextContractId = Mathf.Max(1, data.nextContractId);
        activeContracts.Clear();
        if (data.contracts != null)
        {
            foreach (var mc in data.contracts)
                if (mc != null && !mc.claimed) { activeContracts.Add(mc); nextContractId = Mathf.Max(nextContractId, mc.id + 1); }
        }
        companyTechDone.Clear();
        if (data.coTech != null) foreach (string ct in data.coTech) if (!string.IsNullOrEmpty(ct) && !companyTechDone.Contains(ct)) companyTechDone.Add(ct);
        EnsureCompanyTechMigrated();
        talents.Clear();
        if (data.talents != null) foreach (var t in data.talents) if (t != null && !string.IsNullOrEmpty(t.name)) talents.Add(t);
        talentPool.Clear();
        if (data.talentPool != null) foreach (var tp in data.talentPool) if (tp != null && !string.IsNullOrEmpty(tp.name)) talentPool.Add(tp);
        nextTalentId = Mathf.Max(1, data.nextTalentId);
        lastTalentRefreshDay = data.lastTalentRefreshDay;
        lastReadNoticeDay = data.lastReadNoticeDay;
        readPatchNoteVersion = data.readPatchNoteVersion ?? "";
        earnedPlaques.Clear();
        if (data.memorialPlaques != null) foreach (string pk in data.memorialPlaques) if (!string.IsNullOrEmpty(pk) && !earnedPlaques.Contains(pk)) earnedPlaques.Add(pk);
        earnedMemorialTickets.Clear();
        if (data.memorialTickets != null) foreach (string tk in data.memorialTickets) if (!string.IsNullOrEmpty(tk) && !earnedMemorialTickets.Contains(tk)) earnedMemorialTickets.Add(tk);
        retiredVehiclesCount = Mathf.Max(0, data.retiredVehiclesCount);
        storyStage = Mathf.Max(0, data.storyStage);
        tutorialSkipped = data.tutorialSkipped;
        lastDisplayedSolarTerm = data.lastDisplayedSolarTerm;
        hasYesterdayReport = data.hasYesterdayReport && data.yesterdayReport != null;
        yesterdayReport = hasYesterdayReport ? CopyDailyReport(data.yesterdayReport) : null;
        lastPolicyDay = data.lastPolicyDay; lastAdSubsidyDay = data.lastAdSubsidyDay; dailyTaskClaimedDay = data.dailyTaskClaimedDay;
        savedRegion = NormalizeLegacyRegionId(data.region);
    }
    string savedRegion;
    static DailyReportSnapshot CopyDailyReport(DailyReportSnapshot src)
    {
        if (src == null) return null;
        return new DailyReportSnapshot {
            settledDay = src.settledDay, bottleneckStation = src.bottleneckStation,
            gross = src.gross, commercialRent = src.commercialRent, maintAndTax = src.maintAndTax, netProfit = src.netProfit, passengers = src.passengers,
            bestLineNet = src.bestLineNet, worstLineNet = src.worstLineNet, crowdLost = src.crowdLost, unreachedLost = src.unreachedLost, elasticLost = src.elasticLost,
            bestLineName = src.bestLineName, worstLineName = src.worstLineName, bottleneckNote = src.bottleneckNote
        };
    }
    SaveData CaptureSaveData()
    {
        var data=new SaveData { coins=coins, diamonds=diamonds, rp=rp, day=day, clock=dayClock, researchClock=researchClock, speed=timeSpeed, nextId=nextLineId, dailyBaseline=dailyStationBaseline, moonFar=moonFarUnlocked, marsWest=marsWestUnlocked, loan=loanOutstanding, music=musicEnabled, region=currentRegion, theme=(previewRestoreTheme >= 0 ? previewRestoreTheme : currentTheme), companyName=companyName, companySeal=companySeal, companyPolicy=companyPolicy, bondDaysLeft=bondDaysLeft, bondPrincipal=bondPrincipal, bondDailyDue=bondDailyDue, bondsRepaid=bondsRepaid, bondSubsidized=bondSubsidized, consultantId=consultantId, consultantDaysLeft=consultantDaysLeft, economyCycle=economyCycle, nextEconomyCycle=nextEconomyCycle, turboDaysLeft=turboDaysLeft, autoDispatchDaysLeft=autoDispatchDaysLeft, lastAwardDay=lastAwardDay, lastPolicyDay=lastPolicyDay, lastAdSubsidyDay=lastAdSubsidyDay, dailyTaskClaimedDay=dailyTaskClaimedDay, storyStage=storyStage, tutorialSkipped=tutorialSkipped, nextTalentId=nextTalentId, lastTalentRefreshDay=lastTalentRefreshDay, lastReadNoticeDay=lastReadNoticeDay, readPatchNoteVersion=readPatchNoteVersion, retiredVehiclesCount=retiredVehiclesCount, lastDisplayedSolarTerm=lastDisplayedSolarTerm, nextContractId=nextContractId, hasYesterdayReport=(hasYesterdayReport && yesterdayReport != null), yesterdayReport=(hasYesterdayReport && yesterdayReport != null ? CopyDailyReport(yesterdayReport) : null) };
        foreach (var kv in frozenTraffic)
        {
            if (kv.Key == null || kv.Value == null) continue;
            data.frozenLines.Add(new SavedFrozenLine {
                lineId = kv.Key.id,
                revenue = (float[])kv.Value.revenue.Clone(),
                demand = (float[])kv.Value.demand.Clone(),
                served = (float[])kv.Value.served.Clone(),
                peak = (float[])kv.Value.peak.Clone(),
                valid = (bool[])kv.Value.valid.Clone()
            });
        }
        data.memorialPlaques.AddRange(earnedPlaques);
        data.memorialTickets.AddRange(earnedMemorialTickets);
        data.boardPartners.AddRange(boardPartners);
        foreach (var c in activeContracts) data.contracts.Add(new MunicipalContract {
            id=c.id, type=c.type, startValue=c.startValue, targetValue=c.targetValue,
            rewardCoins=c.rewardCoins, rewardRp=c.rewardRp, rewardDiamonds=c.rewardDiamonds,
            deadlineDay=c.deadlineDay, claimed=c.claimed });
        data.coTech.AddRange(companyTechDone);
        foreach (var t in talents) data.talents.Add(CopySaveTalent(t));
        foreach (var t in talentPool) data.talentPool.Add(CopySaveTalent(t));
        foreach(var s in stations) data.stations.Add(new SavedStation { key=StationSaveKey(s), customName=string.IsNullOrEmpty(s.customName) ? "" : s.customName, level=s.level, trafficEvent=s.trafficEvent, built=s.built, closed=s.closed, riderBonus=s.riderBonus, facLevel=s.facLevel });
        foreach(var s in sceneries) data.scenery.Add(new SavedScenery { key=ScenerySaveKey(s), found=s.found, unlocked=s.unlocked });
        foreach(var l in lines) { var record=new SavedLine { id=l.id, name=l.name, color=l.color, skin=l.skin, trains=l.trains, baseRemoved=!l.hasBaseTrain, dispatch=(int[])l.dispatch.Clone(), fare=l.fare, todayGross=l.todayGross, todayMaintenance=l.todayMaintenance, todayTrack=l.todayTrack, todayRevenue=l.todayRevenue, recent=new List<int>(l.recent), stations=new List<string>(), segWaypoints=CloneWaypointsList(l.segWaypoints) }; foreach(int i in l.st) record.stations.Add(StationSaveKey(stations[i])); data.lines.Add(record); }
        foreach(var v in researchVehicles) data.vehicles.Add(new SavedVehicle { node=v.node, lineId=v.lineId });
        data.tech.AddRange(techDone); data.story.AddRange(storySeen); data.skins.AddRange(ownedLineSkins);
        foreach (var ur in unlockedRegions) data.unlockedRegions.Add(ur);
        data.themes.AddRange(ownedThemes);
        foreach (var kv in adWatchCounts) { data.adKeys.Add(kv.Key); data.adCounts.Add(kv.Value); }
        foreach (var kv in themeTrialUntil) { data.trialThemes.Add(kv.Key); data.trialEnds.Add(kv.Value); }
        return data;
    }
    static SavedTalent CopySaveTalent(SavedTalent t)
    {
        return new SavedTalent { id=t.id, name=t.name, role=t.role, rarity=t.rarity,
            level=t.level, xp=t.xp, assignKind=t.assignKind, assignLineId=t.assignLineId,
            assignStationKey=t.assignStationKey, flawIndex=t.flawIndex,
            flawCleansed=t.flawCleansed, bio=t.bio };
    }
    sealed class AutosavePayload { public string json, pretty; }
    System.Threading.Tasks.Task<AutosavePayload> autosaveSerialization;
    AutosavePayload pendingAutosavePayload;
    int saveRevision, autosaveRevision;
    float saveIdleSeconds;
    bool saveDirty;
    float saveDebounceTimer;
    void RequestSaveGame(float delay = 0.85f)
    {
        if (!saveReady || saveBlocked) return;
        unchecked { saveRevision++; }
        saveDirty = true;
        saveDebounceTimer = delay;
    }
    void SaveGame()
    {
        if(!saveReady || saveBlocked) return;
        unchecked { saveRevision++; }
        pendingAutosavePayload = null;
        saveDirty = false;
        saveDebounceTimer = 0f;
        autosaveTimer = 0f;
        try {
            var data = CaptureSaveData();
            PlayerPrefs.SetString(SaveKey,JsonUtility.ToJson(data)); PlayerPrefs.Save();
#if UNITY_EDITOR
            if (DevTestSaveSyncEnabled) WriteDevTestSaveToDisk(data);
#endif
        } catch(Exception e) { Debug.LogError("本地存档保存失败：" + e.Message); }
    }
    void TickAutosave()
    {
        if (!saveReady || saveBlocked) return;
        float dt = Time.unscaledDeltaTime;
        bool interacting = draft.Count > 0 || lineInsertActive || Input.GetMouseButton(0) || Input.touchCount > 0;
        saveIdleSeconds = interacting ? 0f : saveIdleSeconds + dt;
        bool busy = interacting || saveIdleSeconds < 1f || pendingForecastMask != 0;
        saveDebounceTimer -= dt;
        autosaveTimer += dt;
        if (autosaveSerialization != null)
        {
            if (!autosaveSerialization.IsCompleted) return;
            if (autosaveSerialization.IsFaulted)
                Debug.LogError("本地存档序列化失败：" + autosaveSerialization.Exception.GetBaseException().Message);
            else if (autosaveRevision == saveRevision) pendingAutosavePayload = autosaveSerialization.Result;
            autosaveSerialization = null;
            return; // Do not combine snapshot/serialization/flush in one frame.
        }
        if (pendingAutosavePayload != null)
        {
            if (autosaveRevision != saveRevision) { pendingAutosavePayload = null; return; }
            if (busy) return;
            try
            {
                PlayerPrefs.SetString(SaveKey, pendingAutosavePayload.json);
                PlayerPrefs.Save();
#if UNITY_EDITOR
                if (DevTestSaveSyncEnabled)
                {
                    WriteSaveJsonAtomically(BundledTestSaveAssetPath, pendingAutosavePayload.pretty);
                    DevTestSaveDirty = true;
                }
#endif
                saveDirty = false; saveDebounceTimer = 0f; autosaveTimer = 0f;
            }
            catch (Exception e) { Debug.LogError("本地存档保存失败：" + e.Message); }
            pendingAutosavePayload = null;
            return;
        }
        if (busy || (saveDirty ? saveDebounceTimer > 0f : autosaveTimer < 5f)) return;
        try
        {
            // Capture on the main thread; WebGL runs without managed worker threads.
            var data = CaptureSaveData();
            autosaveRevision = saveRevision;
#if UNITY_WEBGL && !UNITY_EDITOR
            pendingAutosavePayload = new AutosavePayload {
                json = JsonUtility.ToJson(data), pretty = null
            };
#else
            bool needPretty = false;
#if UNITY_EDITOR
            needPretty = DevTestSaveSyncEnabled;
#endif
            autosaveSerialization = System.Threading.Tasks.Task.Run(() => new AutosavePayload {
                json = JsonUtility.ToJson(data), pretty = needPretty ? JsonUtility.ToJson(data, true) : null
            });
#endif
        }
        catch (Exception e) { Debug.LogError("本地存档快照失败：" + e.Message); }
    }
    string ReadBundledTestSaveRaw()
    {
#if UNITY_EDITOR
        string diskJson = ReadDevTestSaveFromDisk();
        if (!string.IsNullOrEmpty(diskJson)) return diskJson;
#endif
#if UNITY_EDITOR || TRAILER_PREVIEW
        var asset = Resources.Load<TextAsset>(BundledTestSaveResource);
        return asset ? asset.text : null;
#else
        return null;
#endif
    }
    bool HasBundledTestSave() { return !string.IsNullOrEmpty(ReadBundledTestSaveRaw()); }
    string DescribeBundledTestSave()
    {
        string raw = ReadBundledTestSaveRaw();
        if (string.IsNullOrEmpty(raw)) return "预置测试存档";
        try {
            var check = JsonUtility.FromJson<SaveData>(raw);
            if (check != null && check.version == 1)
            {
                string reg = string.IsNullOrEmpty(check.region) ? "月之亮面" : DisplayPlaceAliases(check.region);
                int lc = check.lines != null ? check.lines.Count : 0;
                int tc = check.tech != null ? check.tech.Count : 0;
                return "第 " + Mathf.Max(1, check.day) + " 日" + reg + "测试存档（" + lc + " 条线路 · " + tc + " 项科技 · " + FormatCoins(check.coins) + " 资金）";
            }
        } catch {}
        return "预置测试存档";
    }
    void ResetLoadedSessionState()
    {
        unchecked { saveRevision++; }
        pendingAutosavePayload = null;
        saveDirty = false; saveDebounceTimer = 0f;
        if (forecastWork != null) { forecastWork.Dispose(); forecastWork = null; }
        pendingForecastMask = 0; hasStagedTopoSig = false; hasTrafficTopologySig = false;
        for (int slot = 0; slot < 4; slot++)
        {
            if (stagedSlots != null && slot < stagedSlots.Length) stagedSlots[slot] = null;
            if (trafficSlots != null && slot < trafficSlots.Length) trafficSlots[slot] = null;
        }
        techCollapseDone = true; techSessionUnlocked.Clear(); historyJustUnlocked = null;
        coTechCollapseDone = true; coTechSessionUnlocked.Clear(); coTechJustUnlocked = null;
        researchProfitEstimates.Clear(); hasResearchProfitSignature = false;
        hasYesterdayReport = false; yesterdayReport = null;
        estimatingResearchProfit = false; hasForecastSignature = false;
        frozenTraffic.Clear(); lineForecasts.Clear(); operatingRates.Clear();
        pendingCheckin.Clear(); storyQueue.Clear(); ClearNarrativeBubbles();
        scrollPositions.Clear(); scrollViews.Clear();
        // Keep generations monotonic so an old same-frame callback cannot match a new binding.
        foreach (var key in new List<string>(scrollBindingVersions.Keys)) NextScrollBinding(key);
        lineFleetBackAction = null; lineFleetOriginFleetBack = null; lineFleetShopReturn = null;
        lineFleetLineId = 0; lineFleetSlot = -1; lineFleetAmount = 0;
        nextContractId = 1; hasYesterdayReport = false; yesterdayReport = null;
        autosaveTimer = 0f;
    }
    bool ApplyLoadedSaveJson(string raw, string sourceLabel, bool fromBundledTestSave)
    {
        if (string.IsNullOrEmpty(raw)) { Toast("未找到" + sourceLabel); return false; }
        try {
            var check = JsonUtility.FromJson<SaveData>(raw);
            if (check == null || check.version != 1 || check.lines == null || check.stations == null)
            {
                Toast(sourceLabel + "格式校验失败");
                return false;
            }
        } catch (Exception e) {
            Toast(sourceLabel + "解析失败：" + e.Message);
            return false;
        }

        saveReady = false;
        ClosePanel(true); CloseLineBubble(); ClosePage(); CancelDraft(); ClearFloaters();
        EndThemePreview(false);
        ClearUndoHistory();
        ResetLoadedSessionState();
        if (storyCard) { Destroy(storyCard); storyCard = null; }
        storyCheckTimer = 0f;
#if UNITY_EDITOR
        if (fromBundledTestSave)
        {
            // Loading the shared test run explicitly resumes shared test-save synchronization.
            DevTestSaveSyncEnabled = true;
        }
#endif
        lines.Clear();
        BuildData();
        PlayerPrefs.SetString(SaveKey, raw);
        PlayerPrefs.Save();
        saveBlocked = false;
        saveReady = false;
        if (!RestoreGame()) { Toast(sourceLabel + "载入失败"); return false; }

        EnsureCompanyInitialized();
        if (!IsThemeAvailable(currentTheme)) currentTheme = 0;
        string dest = (!string.IsNullOrEmpty(savedRegion) && RegionUnlocked(savedRegion)) ? savedRegion : "月之亮面";
        FreezeRegionTraffic();
        currentRegion = dest;
        SetActiveRegionMapArt(currentRegion);
        SyncRegionMapTextureMemory();
        var curSpec = GetRegionSpec(currentRegion);
        float initZoom = curSpec != null ? curSpec.initZoom : 1f;
        if (mapRect)
        {
            mapRect.localScale = Vector3.one * initZoom;
            mapRect.anchoredPosition = mapWheelZoom ? mapWheelZoom.CenterPosition(initZoom) : Vector2.zero;
        }
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
        if (regionTitle) regionTitle.text = curSpec != null ? curSpec.spacedTitle : currentRegion;
        RefreshSceneryViews();
        UpdateStationLabelSizes(1f);
        LayoutStationLabels();
        ApplyTheme(currentTheme, false);
        CreateLineVisuals();
        RefreshAllLines();
        RefreshStationDots();
        CheckSceneryOnLine(null);
        RecalcIncome();
        UpdateRegionalMarkers(true);
        UpdateTimeText();
        RefreshHUD();
        RefreshPendingBadge();
        RefreshNoticeUnreadDot();
        RefreshUndoButton();
        SyncStoryProgression(false);
        UpdateStoryObjectiveBanner();
        SyncRegionalMusic(true);
        saveReady = true;
        SaveGame();
        Toast("已载入" + sourceLabel + "：第 " + day + " 日 · " + DisplayPlaceAliases(currentRegion));
        return true;
    }

    bool LoadBundledTestSave()
    {
        return ApplyLoadedSaveJson(ReadBundledTestSaveRaw(), "测试存档", true);
    }
    void OnApplicationPause(bool paused) { if(paused) SaveGame(); }
    void OnApplicationFocus(bool focused)
    {
        if (!focused) { if (lineInsertActive) CancelLineInsert(); SaveGame(); }
        else ApplyFrameRate();
    }
    void OnApplicationQuit() { SaveGame(); }
    void OnDisable() { SaveGame(); }
}


