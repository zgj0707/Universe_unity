using System;
using UnityEngine;

public partial class DemoBootstrap
{
    // ---------- 星域口岸联运协同（3 座 1 对 1 常规星门 + 2 座三域超级枢纽） ----------
    static readonly string[] CrossBayCorridorNames = {
        "亮面—西半球走廊",
        "阿波罗三域超级枢纽",
        "虹湾三域超级枢纽",
        "本初子午线跨球动脉",
        "±180°日界线贯通走廊"
    };
    static readonly string[] CrossBayCorridorDescs = {
        "「奥林匹斯星门」两侧同时接入月之亮面与火星·西半球",
        "「阿波罗星门」在月背、火星·西半球、火星·东半球任意两域以上均有接驳",
        "「虹湾星门」在月之亮面、月背、火星·西半球任意两域以上均有接驳",
        "「子午线星门」两侧同时贯通火星·西半球与火星·东半球",
        "「亚马逊西门」经 ±180° 经线贯通火星·东半球与西半球"
    };

    bool HasPortalServiceInRegion(int portalIdx, string region)
    {
        if (portalIdx < 0 || portalIdx >= stations.Count) return false;
        foreach (var line in lines)
        {
            if (RunningTrains(line) <= 0 || !line.st.Contains(portalIdx)) continue;
            foreach (int stIdx in line.st)
            {
                if (stIdx == portalIdx || stIdx < 0 || stIdx >= stations.Count) continue;
                if (!stations[stIdx].closed && stations[stIdx].region == region) return true;
            }
        }
        return false;
    }

    bool HasPortalServiceInAnyMars(int portalIdx)
    {
        for (int i = 0; i < MarsRegions.Length; i++)
            if (HasPortalServiceInRegion(portalIdx, MarsRegions[i])) return true;
        return false;
    }

    bool HasMarsBorderService(string regA, string regB, params string[] gateNames)
    {
        for (int i = 0; i < gateNames.Length; i++)
        {
            int idx = FindStationIndexByExactName(gateNames[i]);
            if (idx >= 0 && HasPortalServiceInRegion(idx, regA) && HasPortalServiceInRegion(idx, regB))
                return true;
        }
        return false;
    }

    int FindStationIndexByExactName(string exactName)
    {
        for (int i = 0; i < stations.Count; i++)
            if (stations[i].name == exactName) return i;
        return -1;
    }

    bool CrossBayCorridorActive(int index)
    {
        switch (index)
        {
            case 0: return HasMarsBorderService("月之亮面", "火星·西半球", "奥林匹斯星门");
            case 1:
            {
                int hq = FindStationIndexByExactName("阿波罗星门");
                int cnt = (HasPortalServiceInRegion(hq, "月背") ? 1 : 0)
                        + (HasPortalServiceInRegion(hq, "火星·西半球") ? 1 : 0)
                        + (HasPortalServiceInRegion(hq, "火星·东半球") ? 1 : 0);
                return cnt >= 2;
            }
            case 2:
            {
                int hz = FindStationIndexByExactName("虹湾星门");
                int cnt = (HasPortalServiceInRegion(hz, "月之亮面") ? 1 : 0)
                        + (HasPortalServiceInRegion(hz, "月背") ? 1 : 0)
                        + (HasPortalServiceInRegion(hz, "火星·西半球") ? 1 : 0);
                return cnt >= 2;
            }
            case 3: return HasMarsBorderService("火星·西半球", "火星·东半球", "子午线星门");
            case 4: return HasMarsBorderService("火星·东半球", "火星·西半球", "亚马逊西门");
            default:
                return false;
        }
    }

    int CrossBaySynergyLinkCount()
    {
        int count = 0;
        for (int i = 0; i < CrossBayCorridorNames.Length; i++) if (CrossBayCorridorActive(i)) count++;
        return count;
    }

    void CheckCrossBayMilestoneBubbles()
    {
        if (CrossBayCorridorActive(0)) QueueMilestoneBubble("M-05", OpenCompanyScreen);
        if (CrossBayCorridorActive(3)) QueueMilestoneBubble("M-06", OpenCompanyScreen);
        if (CrossBayCorridorActive(4)) QueueMilestoneBubble("M-07", OpenCompanyScreen);
        if (CrossBayCorridorActive(2)) QueueMilestoneBubble("M-08", OpenCompanyScreen);
        if (CrossBayCorridorActive(1)) QueueMilestoneBubble("M-09", OpenCompanyScreen);
    }

    float CrossBaySynergyDailyBonus()
    {
        int links = CrossBaySynergyLinkCount();
        if (links <= 0) return 0f;
        float perLink = 260f + Mathf.Min(360f, DailyConnectedCount() * 10f);
        return links * perLink * CoTechRevenueMult() * PlaqueBayBonusMult();
    }

    float CrossBayDemandMult(int stationIdx)
    {
        int links = CrossBaySynergyLinkCount();
        if (links <= 0 || stationIdx < 0 || stationIdx >= stations.Count) return 1f;
        return stations[stationIdx].portal ? (1f + links * 0.12f) : (1f + links * 0.04f);
    }

    string CrossBaySynergySummary()
    {
        int links = CrossBaySynergyLinkCount();
        if (links <= 0) return "尚未贯通跨区联运走廊（接入任一口岸两地线路即享联运补贴与客流加成）";
        float bonus = CrossBaySynergyDailyBonus();
        return $"已贯通 {links} / {CrossBayCorridorNames.Length} 条跨星域走廊 · 每日联运补贴 +¥{Mathf.RoundToInt(bonus)} · 全网需求 +{links * 4}%（口岸 +{links * 12}%）";
    }
}
