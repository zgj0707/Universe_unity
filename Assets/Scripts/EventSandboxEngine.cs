using System;
using System.Collections.Generic;
using UnityEngine;

public partial class DemoBootstrap
{
    public sealed class SandboxTaxiOrder
    {
        public int originStation, destStation, storyChapter;
        public float baseFare, patienceMinutes, elapsedMinutes;
        public bool isStoryPassenger;
    }

    const int AshSandboxTotalDays = 7;
    const int NeonSandboxTotalNights = 14;

    string activeSandboxEventId = "";
    readonly List<SandboxTaxiOrder> sandboxTaxiOrders = new List<SandboxTaxiOrder>();
    readonly HashSet<int> ashFallenStations = new HashSet<int>();
    float ashSandboxHourAccum;
    int ashHalfDayDelayUsed;
    float neonSandboxOrderAccum;
    int neonTonightDone, neonTonightTimeout, neonStorySpawnMask;
    float sandboxCoinSaveTimer;

    bool IsInEventSandbox() => !string.IsNullOrEmpty(activeSandboxEventId);

    int[] NormalizeSandboxDispatch(int[] values, int cars)
    {
        var result = new[] { 1, 1, 1, 1 };
        for (int i = 0; i < 4; i++) result[i] = Mathf.Clamp(values != null && i < values.Length ? values[i] : 1, 0, Mathf.Max(1, cars));
        return result;
    }

    void UpdateSandboxTraffic(SavedEventSandbox st)
    {
        if (traffic == null) traffic = new TrafficResult();
        traffic.lineSegLoad.Clear();
        foreach (var station in stations) station.waiting = 0f;
        if (activeSandboxEventId == EventIdNeonNight)
            foreach (var order in sandboxTaxiOrders)
                if (order.originStation >= 0 && order.originStation < stations.Count) stations[order.originStation].waiting += 1f;
        else
            for (int i = 0; i < stations.Count; i++)
                if (!AshSandboxStations[i].isSpecialA && !ashFallenStations.Contains(i))
                    stations[i].waiting = i < st.stationPop.Count ? st.stationPop[i] : 0f;
        foreach (var line in lines)
        {
            var saved = FindSandboxLine(line);
            float capacity = SandboxSavedLineCapacity(saved, st);
            for (int seg = 0; seg + 1 < line.st.Count; seg++)
            {
                int a = line.st[seg], b = line.st[seg + 1];
                if (a < 0 || b < 0 || a >= stations.Count || b >= stations.Count) continue;
                float demand = 0f;
                if (activeSandboxEventId == EventIdNeonNight)
                {
                    foreach (var order in sandboxTaxiOrders)
                        if (order.originStation == a || order.originStation == b || order.destStation == a || order.destStation == b) demand += 400f;
                }
                else demand = stations[a].waiting + stations[b].waiting;
                string key = line.id + ":" + Mathf.Min(a, b) + ":" + Mathf.Max(a, b);
                traffic.lineSegLoad[key] = Mathf.Clamp(capacity > 0f ? demand / capacity : (demand > 0f ? 1.35f : 0f), 0f, 1.35f);
            }
        }
        UpdateLineTrafficColors();
        RebuildSandboxStationVisuals();
    }

    void EnsureAshStationPopInitialized(SavedEventSandbox st)
    {
        if (st.stationPop == null) st.stationPop = new List<int>();
        if (st.ashClearedStations == null) st.ashClearedStations = new List<int>();
        while (st.stationPop.Count < AshSandboxStations.Length)
        {
            var def = AshSandboxStations[st.stationPop.Count];
            int[] populations = { 0, 680, 540, 460, 380, 300 };
            st.stationPop.Add(def.isSpecialA || def.isSpecialB ? 0 : populations[Mathf.Clamp(def.tierOrKind, 1, 5)]);
        }
    }

    bool AshCycleCompleted() => activeSandboxEventId == EventIdAshDome && GetOrCreateEventSandbox(EventIdAshDome).eventDay > AshSandboxTotalDays;

    void TickEventSandbox(float dt)
    {
        if (!IsInEventSandbox()) return;
        var st = GetOrCreateEventSandbox(activeSandboxEventId);
        UpdateAshAtmosphere(st, dt);
        if (AshCycleCompleted()) return;
        float speed = Mathf.Max(0f, timeSpeed);
        if (speed <= 0f) return;

        float step = dt * speed;
        if (activeSandboxEventId == EventIdAshDome) step = Mathf.Min(step, GameDaySeconds - st.eventClock);
        float hoursDelta = (step / GameDaySeconds) * 24f;
        st.eventClock += step;

        if (activeSandboxEventId == EventIdAshDome)
            TickAshSandbox(st, hoursDelta);
        else if (activeSandboxEventId == EventIdNeonNight)
            TickNeonSandbox(st, hoursDelta);
        UpdateSandboxTraffic(st);

        if (st.eventClock >= GameDaySeconds)
        {
            st.eventClock -= GameDaySeconds;
            st.eventDay = Mathf.Max(1, st.eventDay + 1);
            OnSandboxDayAdvanced(activeSandboxEventId, st);
        }
        sandboxCoinSaveTimer += dt;
        if (sandboxCoinSaveTimer >= 4f)
        {
            sandboxCoinSaveTimer = 0f;
            SaveSandboxStorage();
        }
        RefreshSandboxHudText();
    }

    void OnSandboxDayAdvanced(string eventId, SavedEventSandbox st)
    {
        if (eventId == EventIdAshDome)
        {
            if (st.eventDay == AshSandboxTotalDays + 1)
            {
                for (int i = 0; i < st.stationPop.Count && i < AshSandboxStations.Length; i++)
                    if (st.stationPop[i] > 0) { st.missedPop += st.stationPop[i]; st.stationPop[i] = 0; ashFallenStations.Add(i); }
                EvaluateSandboxAchievements(eventId);
                UpdateSandboxTraffic(st);
                SaveSandboxStorage();
                OpenAshCompletionReportPopup();
                return;
            }
            int delayTokens = (HasSandboxTreeNode(EventIdAshDome, "ash_sci_1") ? 1 : 0)
                + (HasSandboxTreeNode(EventIdAshDome, "ash_sci_2") ? 1 : 0)
                + (HasSandboxTreeNode(EventIdAshDome, "ash_def_2b") ? 1 : 0)
                + ((st.extraA & 0x2000) != 0 ? 1 : 0);
            int layer = Mathf.Clamp((int)(st.extraA & 0xFF), 1, 7);
            if (delayTokens > ashHalfDayDelayUsed)
            {
                ashHalfDayDelayUsed++;
                Toast("冷却剂与水闸加固生效，今日沙尘圈层暂缓外扩");
            }
            else if (st.eventDay >= 2 && layer < 6)
            {
                layer++;
                st.extraA = (st.extraA & ~0xFFL) | (uint)layer;
                SyncAshFallenStations(st);
                Toast($"阿卡迪亚老城第 {layer} 圈层进入重点转运状态");
            }
            if (st.eventDay <= 7 && st.storyStage < st.eventDay)
                AdvanceSandboxStoryTo(EventIdAshDome, st.eventDay);
        }
        else if (eventId == EventIdNeonNight)
        {
            if (neonTonightDone >= 40 && neonTonightTimeout == 0 && !st.achievements.Contains("evt_neon_allnight"))
                st.achievements.Add("evt_neon_allnight");
            neonTonightDone = 0;
            neonTonightTimeout = 0;
            sandboxTaxiOrders.Clear();
            if (st.eventDay == NeonSandboxTotalNights + 1)
                Toast($"十四夜执勤周期达成  可继续跑车或在指挥部更换执勤流派");
        }
        EvaluateSandboxAchievements(eventId);
        RefreshEventBadge();
        SaveSandboxStorage();
        RequestSaveGame(1.0f);
    }

    void SyncAshFallenStations(SavedEventSandbox st)
    {
        EnsureAshStationPopInitialized(st);
        ashFallenStations.Clear();
        int dangerLayer = Mathf.Clamp((int)(st.extraA & 0xFF), 1, 7);
        int fallenLayer = dangerLayer - 1 - Mathf.FloorToInt(GetAshExtraImmuneDays());
        for (int i = 0; i < AshSandboxStations.Length; i++)
        {
            var def = AshSandboxStations[i];
            if (def.isSpecialB || (!def.isSpecialA && def.tierOrKind > 0 && (def.tierOrKind <= fallenLayer || (st.eventDay > AshSandboxTotalDays && !st.ashClearedStations.Contains(i)))))
            {
                ashFallenStations.Add(i);
                if (!def.isSpecialB && st.stationPop[i] > 0)
                {
                    int rem = st.stationPop[i];
                    st.missedPop += rem; st.stationPop[i] = 0;
                    Toast($"{def.name} 已被沙暴封控，{rem} 名居民未能及时转运");
                }
            }
        }
        for (int li = 0; li < st.lines.Count; li++)
        {
            var l = st.lines[li];
            for (int si = 0; si < l.stations.Count; si++)
            {
                if (ashFallenStations.Contains(l.stations[si]) && !AshSandboxStations[l.stations[si]].isSpecialB)
                {
                    st.extraB++;
                    break;
                }
            }
        }
        RebuildSandboxStationVisuals();
    }

    float SandboxDirectiveCapMult(SavedEventSandbox st) => st.activeDirective == 1 ? 1.15f : 1.0f;
    float SandboxDirectiveCoinMult(SavedEventSandbox st) => st.activeDirective == 2 ? 1.25f : st.activeDirective == 0 ? 1.10f : 0.95f;

    float SandboxSavedLineCapacity(SavedEventLine line, SavedEventSandbox st)
    {
        if (line == null) return 0f;
        var v = GetSandboxVehicleDef(activeSandboxEventId, line.vehicleKey);
        float levels = 0f; int count = 0;
        var seen = new HashSet<int>();
        foreach (int i in line.stations)
            if (i >= 0 && i < stations.Count && seen.Add(i)) { levels += Mathf.Max(1, stations[i].level); count++; }
        float avg = count > 0 ? levels / count : 1f;
        float tree = activeSandboxEventId == EventIdAshDome ? GetAshCapMult() : 1f;
        return 4000f * Mathf.Max(1, line.cars) * (v != null ? v.capMult * v.speedMult : 1f)
            * (1f + (avg - 1f) * 0.25f) * tree * SandboxDirectiveCapMult(st) * ResearchFactor(ResearchEffect.Capacity);
    }

    SavedEventLine FindSandboxLine(Line line)
    {
        if (line == null || !IsInEventSandbox()) return null;
        return GetOrCreateEventSandbox(activeSandboxEventId).lines.Find(x => x != null && x.id == line.id);
    }

    void TickAshSandbox(SavedEventSandbox st, float hoursDelta)
    {
        if (st.eventDay > AshSandboxTotalDays) return;
        ashSandboxHourAccum += hoursDelta;
        if (ashSandboxHourAccum < 1f) return;
        int hrs = Mathf.FloorToInt(ashSandboxHourAccum);
        ashSandboxHourAccum -= hrs;

        EnsureAshStationPopInitialized(st);
        int rescued = 0;
        for (int hour = 0; hour < hrs; hour++)
        {
            var intake = new int[AshSandboxStations.Length];
            for (int i = 0; i < intake.Length; i++)
                if (AshSandboxStations[i].isSpecialA)
                    intake[i] = Mathf.FloorToInt((200f + (Mathf.Max(1, stations[i].level) - 1) * 50f) * GetAshRefugeCapMult());
            foreach (var line in st.lines)
            {
                if (line == null || line.stations == null || line.stations.Count < 2) continue;
                var vehicle = GetSandboxVehicleDef(EventIdAshDome, line.vehicleKey);
                int capacity = Mathf.FloorToInt(SandboxSavedLineCapacity(line, st) / 24f * (vehicle != null ? 1f + vehicle.deliveredBonus : 1f));
                // A fallen or forbidden station breaks the route; only the segment containing a haven can evacuate.
                int start = 0;
                while (start < line.stations.Count && capacity > 0)
                {
                    while (start < line.stations.Count && (line.stations[start] < 0 || line.stations[start] >= intake.Length || ashFallenStations.Contains(line.stations[start]))) start++;
                    int end = start;
                    while (end < line.stations.Count && line.stations[end] >= 0 && line.stations[end] < intake.Length && !ashFallenStations.Contains(line.stations[end])) end++;
                    for (int j = start; j < end && capacity > 0; j++)
                    {
                        int origin = line.stations[j];
                        if (AshSandboxStations[origin].isSpecialA || st.stationPop[origin] <= 0) continue;
                        for (int k = start; k < end && capacity > 0 && st.stationPop[origin] > 0; k++)
                        {
                            int haven = line.stations[k];
                            if (!AshSandboxStations[haven].isSpecialA || intake[haven] <= 0) continue;
                            int moved = Mathf.Min(capacity, Mathf.Min(intake[haven], st.stationPop[origin]));
                            capacity -= moved; intake[haven] -= moved; st.stationPop[origin] -= moved; rescued += moved;
                            if (st.stationPop[origin] == 0) { if (!st.ashClearedStations.Contains(origin)) st.ashClearedStations.Add(origin); st.tokens++; Toast($"{AshSandboxStations[origin].name} 居民已全部安全撤离 (+1 火种券)"); }
                        }
                    }
                    start = end + 1;
                }
            }
        }
        long prev = st.progress;
        st.progress += rescued;
        st.tokens += Mathf.Max(0, (int)(st.progress / 100L) - (int)(prev / 100L));
        double reward = Math.Round(rescued * 2.0 * CoTechRevenueMult() * ResearchFactor(ResearchEffect.Revenue) * GetAshOverflowScale() * SandboxDirectiveCoinMult(st));
        coins += reward; st.ashCycleRewards += reward;

        EvaluateSandboxAchievements(EventIdAshDome);
        RefreshHUD();
        RefreshEventBadge();
    }

    void TickNeonSandbox(SavedEventSandbox st, float hoursDelta)
    {
        float curHour = (st.eventClock / GameDaySeconds) * 24f;
        bool isNight = curHour >= 20f || curHour < 6f;
        if (!isNight) return;

        float dtMin = hoursDelta * 60f;
        neonSandboxOrderAccum += hoursDelta;
        if (neonSandboxOrderAccum >= 0.25f)
        {
            neonSandboxOrderAccum = 0f;
            SpawnSandboxTaxiOrders(st);
        }

        float speedMult = ResearchFactor(ResearchEffect.Speed) * CoTechSpeedMult();
        float revMult = CoTechRevenueMult() * ResearchFactor(ResearchEffect.Revenue) * SandboxDirectiveCoinMult(st);
        for (int i = sandboxTaxiOrders.Count - 1; i >= 0; i--)
        {
            var o = sandboxTaxiOrders[i];
            o.elapsedMinutes += dtMin;
            if (TryServeSandboxTaxiOrder(o, st, speedMult, revMult))
            {
                sandboxTaxiOrders.RemoveAt(i);
                continue;
            }
            if (o.elapsedMinutes >= o.patienceMinutes)
            {
                sandboxTaxiOrders.RemoveAt(i);
                neonTonightTimeout++;
                st.extraA = Math.Max(0L, st.extraA - Mathf.RoundToInt(5 * GetNeonRepLossMult()));
            }
        }
        EvaluateSandboxAchievements(EventIdNeonNight);
        RefreshHUD();
        RefreshEventBadge();
    }

    void SpawnSandboxTaxiOrders(SavedEventSandbox st)
    {
        int n = NeonSandboxStations.Length;
        int nextStory = Mathf.Clamp(st.storyStage + 1, 1, 7);
        if ((neonStorySpawnMask & (1 << nextStory)) == 0 && sandboxTaxiOrders.Count < 16)
        {
            neonStorySpawnMask |= 1 << nextStory;
            int a = UnityEngine.Random.Range(0, n), b = (a + UnityEngine.Random.Range(3, n - 1)) % n;
            sandboxTaxiOrders.Add(new SandboxTaxiOrder { originStation = a, destStation = b, baseFare = 160f, patienceMinutes = 90f * GetNeonPatienceMult(), isStoryPassenger = true, storyChapter = nextStory });
        }
        int batch = UnityEngine.Random.Range(2, 4);
        for (int k = 0; k < batch && sandboxTaxiOrders.Count < 24; k++)
        {
            int a = UnityEngine.Random.Range(0, n), b = UnityEngine.Random.Range(0, n);
            if (a == b) continue;
            Vector2 pA = SandboxUvToCanvas(NeonSandboxStations[a].u, NeonSandboxStations[a].v);
            Vector2 pB = SandboxUvToCanvas(NeonSandboxStations[b].u, NeonSandboxStations[b].v);
            float fare = Mathf.Clamp(65f + Vector2.Distance(pA, pB) * 0.14f, 75f, 320f) * GetNeonEntertainmentWeightMult();
            sandboxTaxiOrders.Add(new SandboxTaxiOrder { originStation = a, destStation = b, baseFare = fare, patienceMinutes = UnityEngine.Random.Range(25f, 45f) * GetNeonPatienceMult() });
        }
    }

    bool TryServeSandboxTaxiOrder(SandboxTaxiOrder o, SavedEventSandbox st, float speedMult, float revMult)
    {
        SavedEventLine servingLine = null;
        bool direct = false, transfer = false;
        for (int i = 0; i < st.lines.Count; i++)
        {
            var l = st.lines[i];
            if (l.stations.Contains(o.originStation) && l.stations.Contains(o.destStation)) { direct = true; servingLine = l; break; }
        }
        if (!direct && HasNeonNeighborPickup() && st.lines.Count > 0) { direct = true; servingLine = st.lines[0]; }
        if (!direct)
        {
            for (int i = 0; i < st.lines.Count && !transfer; i++)
            {
                var l1 = st.lines[i];
                if (!l1.stations.Contains(o.originStation)) continue;
                for (int j = 0; j < st.lines.Count; j++)
                {
                    if (i == j) continue;
                    var l2 = st.lines[j];
                    if (!l2.stations.Contains(o.destStation)) continue;
                    for (int k = 0; k < l1.stations.Count; k++)
                        if (l2.stations.Contains(l1.stations[k])) { transfer = true; servingLine = l1; break; }
                }
            }
        }
        if (!direct && !transfer) return false;

        var vDef = servingLine != null ? GetSandboxVehicleDef(EventIdNeonNight, servingLine.vehicleKey) : null;
        float carSpeed = speedMult * (vDef != null ? vDef.speedMult : 1f);
        float ratio = o.elapsedMinutes / Mathf.Max(1f, o.patienceMinutes * carSpeed);
        float tip = ratio <= 0.50f ? 0.30f : ratio <= 0.80f ? 0.10f : 0f;
        if (vDef != null && vDef.maxTipAlways) tip = 0.30f;
        tip += GetNeonTipBonus();

        float transferScale = (transfer && !HasNeonFreeTransfer()) ? 0.90f : 1.0f;
        float vipScale = (vDef != null && vDef.highFareOnly) ? 1.8f : (vDef != null ? vDef.capMult : 1.0f);
        int fleetMult = servingLine != null ? Mathf.Max(1, servingLine.cars) : 1;
        double earned = Math.Round(o.baseFare * transferScale * vipScale * (1f + tip) * revMult * (1f + (fleetMult - 1) * 0.15f));

        coins += earned;
        st.progress += 1;
        neonTonightDone += 1;
        st.tokens += Mathf.Max(1, Mathf.RoundToInt((float)(earned / 200.0)));
        st.extraA = Math.Min(500L, st.extraA + 2L);
        if (o.isStoryPassenger && o.storyChapter > 0)
        {
            st.extraB |= (1L << (o.storyChapter - 1));
            AdvanceSandboxStoryTo(EventIdNeonNight, o.storyChapter);
        }
        return true;
    }

    void ResetSandboxCycle(string eventId)
    {
        var st = GetOrCreateEventSandbox(eventId);
        st.cycleCount = Mathf.Max(1, st.cycleCount) + 1;
        st.eventDay = 1;
        st.eventClock = eventId == EventIdNeonNight ? (GameDaySeconds * 0.30f) : 90f;
        st.progress = 0;
        if (eventId == EventIdAshDome) { st.stationPop = new List<int>(); st.missedPop = 0; st.ashClearedStations = new List<int>(); st.ashCycleRewards = 0; EnsureAshStationPopInitialized(st); ashSandboxHourAccum = 0f; }
        st.activeBranch = -1;
        st.treeNodes.Clear();
        st.lines.Clear();
        st.extraA = eventId == EventIdNeonNight ? 450 : 1;
        st.extraB = 0;
        st.tokens = Mathf.Max(st.tokens, 0) + Mathf.Max(12, st.cycleCount * 6);
        ashFallenStations.Clear();
        ashHalfDayDelayUsed = 0;
        sandboxTaxiOrders.Clear();
        if (activeSandboxEventId == eventId)
        {
            if (eventId == EventIdAshDome) SyncAshFallenStations(st);
            else RebuildSandboxStationVisuals();
            RebuildSandboxLines();
            UpdateSandboxTraffic(st);
            RefreshSandboxHudText();
        }
        SaveSandboxStorage();
        var spec = GetEventSpec(eventId);
        Toast($"已开启新一轮执勤（第 {st.cycleCount} 轮）  获赠 {Mathf.Max(12, st.cycleCount * 6)} {spec?.tokenName ?? "代币"}");
    }
}
