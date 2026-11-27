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
    readonly Dictionary<int, float> ashLineRate = new Dictionary<int, float>();
    readonly Dictionary<int, float> ashStationOutflow = new Dictionary<int, float>();
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
            int[] populations = { 0, 1100, 880, 720, 560, 420 };
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

    void SyncAshSkinMilestones(SavedEventSandbox st, bool toast = true)
    {
        for (int skinId = 1; skinId <= 4; skinId++)
            if (st.totalDelivered >= skinId * 1000L) UnlockMainTrainSkin(skinId, toast);
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
            SyncAshFallenStations(st);
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

    float AshElapsedHours(SavedEventSandbox st) => (st.eventDay - 1) * 24f + st.eventClock / GameDaySeconds * 24f;

    float AshStormDelayHours(SavedEventSandbox st)
    {
        st.ashDelayHours = GetAshDelayHours() + ((st.extraA & 0x2000) != 0 ? 24f : 0f);
        return st.ashDelayHours;
    }

    public float GetAshStationUrgencyHours(int idx)
    {
        if (idx < 0 || idx >= AshSandboxStations.Length || AshSandboxStations[idx].isSpecialB) return 0f;
        var def = AshSandboxStations[idx];
        if (def.isSpecialA) return float.PositiveInfinity;
        var st = GetOrCreateEventSandbox(EventIdAshDome);
        if (ashFallenStations.Contains(idx) || st.ashCountedFallenStations != null && st.ashCountedFallenStations.Contains(idx)) return 0f;
        float vehicleHours = 0f;
        foreach (var line in st.lines)
        {
            if (line == null || line.stations == null || line.stations.Count < 2 || !line.stations.Contains(idx)) continue;
            var v = GetSandboxVehicleDef(EventIdAshDome, line.vehicleKey);
            if (v != null) vehicleHours = Mathf.Max(vehicleHours, v.extraImmuneDays * 24f);
        }
        return Mathf.Max(0f, Mathf.Min(AshSandboxTotalDays * 24f, def.tierOrKind * 24f + AshStormDelayHours(st) + GetAshExtraImmuneDays() * 24f + vehicleHours) - AshElapsedHours(st));
    }

    void SyncAshFallenStations(SavedEventSandbox st)
    {
        EnsureAshStationPopInitialized(st);
        if (st.ashCountedFallenStations == null) st.ashCountedFallenStations = new List<int>();
        // Old saves have already applied their line penalties; mark previously fallen stations once.
        if (!st.ashFallenMaskInitialized)
        {
            int oldLayer = Mathf.Clamp((int)(st.extraA & 0xFF), 1, 7) - 1 - Mathf.FloorToInt(GetAshExtraImmuneDays());
            if (st.eventDay > 1 && st.ashCountedFallenStations.Count == 0)
                for (int i = 0; i < AshSandboxStations.Length; i++)
                    if (!AshSandboxStations[i].isSpecialA && !AshSandboxStations[i].isSpecialB && AshSandboxStations[i].tierOrKind <= oldLayer)
                        st.ashCountedFallenStations.Add(i);
            st.ashFallenMaskInitialized = true;
        }
        ashFallenStations.Clear();
        int layer = Mathf.Clamp(1 + Mathf.FloorToInt(Mathf.Max(0f, AshElapsedHours(st) - AshStormDelayHours(st)) / 24f), 1, 6);
        st.extraA = (st.extraA & ~0xFFL) | (uint)layer;
        for (int i = 0; i < AshSandboxStations.Length; i++)
        {
            var def = AshSandboxStations[i];
            if (def.isSpecialB) { ashFallenStations.Add(i); continue; }
            if (def.isSpecialA) continue;
            bool counted = st.ashCountedFallenStations.Contains(i);
            if (!counted && GetAshStationUrgencyHours(i) > 0f && st.eventDay <= AshSandboxTotalDays) continue;
            ashFallenStations.Add(i);
            if (counted) continue;
            st.ashCountedFallenStations.Add(i);
            int rem = st.stationPop[i];
            if (rem > 0) { st.missedPop += rem; st.stationPop[i] = 0; Toast($"{def.name} 已被沙暴封控，{rem} 名居民未能及时转运"); }
        }
        RebuildSandboxStationVisuals();
    }

    public float GetAshLineHourlyRate(Line line)
    { float value; return line != null && ashLineRate.TryGetValue(line.id, out value) ? value : 0f; }
    public float GetAshStationHourlyOutflow(int idx)
    { float value; return ashStationOutflow.TryGetValue(idx, out value) ? value : 0f; }

    float SandboxDirectiveCapMult(SavedEventSandbox st) => st.activeDirective == 1 ? 1.15f : 1.0f;
    float SandboxDirectiveCoinMult(SavedEventSandbox st) => st.activeDirective == 2 ? 1.25f : st.activeDirective == 0 ? 1.10f : 0.95f;

    float SandboxSavedLineCapacity(SavedEventLine line, SavedEventSandbox st)
    {
        if (line == null) return 0f;
        var v = GetSandboxVehicleDef(activeSandboxEventId, line.vehicleKey);
        if (activeSandboxEventId == EventIdAshDome)
            return 480f * Mathf.Max(1, line.cars) * (v != null ? v.capMult * v.speedMult * (1f + v.deliveredBonus) : 1f) * GetAshCapMult() * GetAshSpeedMult();
        float levels = 0f; int count = 0;
        var seen = new HashSet<int>();
        foreach (int i in line.stations)
            if (i >= 0 && i < stations.Count && seen.Add(i)) { levels += Mathf.Max(1, stations[i].level); count++; }
        float avg = count > 0 ? levels / count : 1f;
        return 4000f * Mathf.Max(1, line.cars) * (v != null ? v.capMult * v.speedMult : 1f)
            * (1f + (avg - 1f) * 0.25f) * SandboxDirectiveCapMult(st) * ResearchFactor(ResearchEffect.Capacity);
    }

    SavedEventLine FindSandboxLine(Line line)
    {
        if (line == null || !IsInEventSandbox()) return null;
        return GetOrCreateEventSandbox(activeSandboxEventId).lines.Find(x => x != null && x.id == line.id);
    }

    float AshRouteEfficiency(SavedEventLine line)
    {
        float distance = 0f;
        for (int i = 1; i < line.stations.Count; i++)
        {
            var path = FindSandboxStreetPath(EventIdAshDome, line.stations[i - 1], line.stations[i]);
            for (int j = 1; j < path.Count; j++) distance += Vector2.Distance(path[j - 1], path[j]);
            if (path.Count < 2 && line.stations[i - 1] >= 0 && line.stations[i - 1] < AshSandboxStations.Length && line.stations[i] >= 0 && line.stations[i] < AshSandboxStations.Length)
            {
                var a = AshSandboxStations[line.stations[i - 1]]; var b = AshSandboxStations[line.stations[i]];
                distance += Vector2.Distance(SandboxUvToCanvas(a.u, a.v), SandboxUvToCanvas(b.u, b.v));
            }
        }
        return Mathf.Clamp(420f / Mathf.Max(260f, distance), 0.52f, 1.15f);
    }

    void GrantAshTokens(SavedEventSandbox st, float amount)
    {
        st.ashTokenCredit += amount * GetAshTokenMult();
        int whole = Mathf.FloorToInt(st.ashTokenCredit + 0.0001f);
        st.tokens += whole; st.ashTokenCredit -= whole;
    }

    void TickAshSandbox(SavedEventSandbox st, float hoursDelta)
    {
        if (st.eventDay > AshSandboxTotalDays) return;
        ashSandboxHourAccum += hoursDelta;
        int hrs = Mathf.FloorToInt(ashSandboxHourAccum);
        ashSandboxHourAccum -= hrs;
        EnsureAshStationPopInitialized(st);
        float savedClock = st.eventClock;
        for (int hour = 0; hour < hrs; hour++)
        {
            st.eventClock = savedClock - (ashSandboxHourAccum + hrs - hour) * GameDaySeconds / 24f;
            SyncAshFallenStations(st);
            ashLineRate.Clear(); ashStationOutflow.Clear();
            bool lastTrain = st.eventDay >= 6 && st.eventDay <= 7 && st.eventClock / GameDaySeconds * 24f >= 20f;
            int rescued = 0;
            // Direct routes first, then feed shared hubs; staged residents depart in a later hour.
            var segments = new List<List<int>>(); var owners = new List<SavedEventLine>();
            foreach (var line in st.lines)
            {
                if (line == null || line.stations == null || line.stations.Count < 2) continue;
                var seg = new List<int>();
                foreach (int idx in line.stations)
                {
                    if (idx < 0 || idx >= AshSandboxStations.Length || ashFallenStations.Contains(idx))
                    { if (seg.Count > 1) { segments.Add(seg); owners.Add(line); } seg = new List<int>(); }
                    else seg.Add(idx);
                }
                if (seg.Count > 1) { segments.Add(seg); owners.Add(line); }
            }
            var remaining = new Dictionary<int, int>();
            foreach (var line in st.lines)
                if (line != null && line.stations != null && line.stations.Count >= 2)
                {
                    var v = GetSandboxVehicleDef(EventIdAshDome, line.vehicleKey);
                    remaining[line.id] = Mathf.FloorToInt(SandboxSavedLineCapacity(line, st) / 24f * AshRouteEfficiency(line) * (lastTrain ? 2f : 1f));
                }
            for (int pass = 0; pass < 2; pass++)
                for (int n = 0; n < segments.Count; n++)
                {
                    var seg = segments[n]; var line = owners[n];
                    bool direct = seg.Exists(i => AshSandboxStations[i].isSpecialA);
                    if ((pass == 0) != direct || remaining[line.id] <= 0) continue;
                    int destination = -1;
                    if (!direct)
                        foreach (int hub in seg)
                        {
                            for (int k = 0; k < segments.Count; k++)
                                if (owners[k].id != line.id && segments[k].Contains(hub) && segments[k].Exists(i => AshSandboxStations[i].isSpecialA)) { destination = hub; break; }
                            if (destination >= 0) break;
                        }
                    if (!direct && destination < 0) continue;
                    foreach (int origin in seg)
                    {
                        if (remaining[line.id] <= 0) break;
                        if (origin == destination || AshSandboxStations[origin].isSpecialA || st.stationPop[origin] <= 0) continue;
                        if (direct) destination = seg.Find(i => AshSandboxStations[i].isSpecialA);
                        int moved = Mathf.Min(remaining[line.id], st.stationPop[origin]);
                        if (!direct && HasSandboxTreeNode(EventIdAshDome, "ash_res_2")) moved = Mathf.Min(st.stationPop[origin], Mathf.FloorToInt(remaining[line.id] * 1.2f));
                        if (moved <= 0) continue;
                        remaining[line.id] -= !direct && HasSandboxTreeNode(EventIdAshDome, "ash_res_2") ? Mathf.CeilToInt(moved / 1.2f) : moved; st.stationPop[origin] -= moved;
                        if (direct) rescued += moved;
                        else st.stationPop[destination] += moved;
                        float rate; ashLineRate.TryGetValue(line.id, out rate); ashLineRate[line.id] = rate + moved;
                        float outflow; ashStationOutflow.TryGetValue(origin, out outflow); ashStationOutflow[origin] = outflow + moved;
                        if (st.stationPop[origin] == 0 && !st.ashClearedStations.Contains(origin))
                        { st.ashClearedStations.Add(origin); Toast($"{AshSandboxStations[origin].name} 居民已全部转运"); }
                    }
                }
            st.progress += rescued;
            if (rescued > 0) { st.totalDelivered += rescued; SyncAshSkinMilestones(st); }
            if (rescued > 0 && st.missedPop == 0 && st.stationPop.TrueForAll(pop => pop <= 0))
                GrantSandboxCompletionRewards(EventIdAshDome);
                GrantAshTokens(st, rescued / 250f);
            double reward = Math.Round(rescued * 20.0 * GetAshOverflowScale() * SandboxDirectiveCoinMult(st) * (lastTrain ? 2f : 1f));
            coins += reward; st.ashCycleRewards += reward;
        }
        st.eventClock = savedClock;
        if (hrs > 0) SyncAshFallenStations(st);
        if (hrs > 0) { EvaluateSandboxAchievements(EventIdAshDome); RefreshHUD(); RefreshEventBadge(); }
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
        if (eventId == EventIdAshDome)
        {
            coins += GetAshStartFundsBonus();
            st.stationPop = new List<int>(); st.missedPop = 0; st.ashClearedStations = new List<int>(); st.ashCycleRewards = 0;
            st.tokens = 0; st.nextLineId = 1; st.ownedVehicles = new List<string> { "veh_ash_scavenger", "veh_ash_scavenger" };
            EnsureAshStationPopInitialized(st); ashSandboxHourAccum = st.eventClock / GameDaySeconds * 24f % 1f;
        }
        else { st.activeBranch = -1; st.treeNodes.Clear(); }
        st.lines.Clear();
        st.extraA = eventId == EventIdNeonNight ? 450 : 1;
        st.extraB = 0;
        if (eventId != EventIdAshDome) st.tokens = Mathf.Max(st.tokens, 0) + Mathf.Max(12, st.cycleCount * 6);
        ashFallenStations.Clear();
        st.ashDelayHours = 0f; st.ashTokenCredit = 0f; st.ashCountedFallenStations = new List<int>(); st.ashFallenMaskInitialized = true;
        sandboxTaxiOrders.Clear();
        if (activeSandboxEventId == eventId)
        {
            if (eventId == EventIdAshDome)
            {
                lines.Clear();
                SyncAshFallenStations(st);
            }
            else RebuildSandboxStationVisuals();
            RebuildSandboxLines();
            UpdateSandboxTraffic(st);
            RefreshSandboxHudText();
        }
        SaveSandboxStorage();
        var spec = GetEventSpec(eventId);
        Toast(eventId == EventIdAshDome ? $"已回到第 {st.cycleCount} 轮清晨六点" : $"已开启新一轮执勤（第 {st.cycleCount} 轮）  获赠 {Mathf.Max(12, st.cycleCount * 6)} {spec?.tokenName ?? "代币"}");
        if (activeSandboxEventId == eventId && eventId == EventIdAshDome)
        {
            ClosePage(true); ClosePanel(true); CloseLineBubble();
            OpenSandboxOnboardingStory(eventId, 0);
        }
    }
}
