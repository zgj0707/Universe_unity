using System;
using UnityEngine;

public partial class DemoBootstrap
{
    // ---------- 三域口岸联运协同 ----------
    static readonly string[] CrossBayCorridorNames = { "双渡口双枢纽走廊", "汀屿北关走廊", "三域虹湾星门走廊" };
    static readonly string[] CrossBayCorridorDescs = {
        "月之亮面与火星线路同时接入「柏拉图跃迁港」或「奥林匹斯（月面门）」",
        "月背与火星线路同时接入「阿西达利亚（月背门）」「达朗贝尔门」或「阿波罗星门」",
        "「虹湾星门」门户在任意两域以上均有运行线路接驳"
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
            case 0:
            {
                int luo = FindStationIndexByExactName("柏拉图跃迁港");
                int sz = FindStationIndexByExactName("奥林匹斯（月面门）");
                bool hkSide = HasPortalServiceInRegion(luo, "月之亮面") || HasPortalServiceInRegion(sz, "月之亮面");
                bool gdSide = HasPortalServiceInRegion(luo, "火星") || HasPortalServiceInRegion(sz, "火星");
                return hkSide && gdSide;
            }
            case 1:
            {
                int zh = FindStationIndexByExactName("阿西达利亚（月背门）");
                int zha = FindStationIndexByExactName("达朗贝尔门");
                int hq = FindStationIndexByExactName("阿波罗星门");
                bool moSide = HasPortalServiceInRegion(zh, "月背") || HasPortalServiceInRegion(zha, "月背") || HasPortalServiceInRegion(hq, "月背");
                bool gdSide = HasPortalServiceInRegion(zh, "火星") || HasPortalServiceInRegion(zha, "火星") || HasPortalServiceInRegion(hq, "火星");
                return moSide && gdSide;
            }
            case 2:
            {
                int hzm = FindStationIndexByExactName("虹湾星门");
                int regions = (HasPortalServiceInRegion(hzm, "月之亮面") ? 1 : 0)
                            + (HasPortalServiceInRegion(hzm, "月背") ? 1 : 0)
                            + (HasPortalServiceInRegion(hzm, "火星") ? 1 : 0);
                return regions >= 2;
            }
            default:
                return false;
        }
    }

    int CrossBaySynergyLinkCount()
    {
        int count = 0;
        for (int i = 0; i < 3; i++) if (CrossBayCorridorActive(i)) count++;
        return count;
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
        return $"已贯通 {links} / 3 条跨海走廊 · 每日联运补贴 +¥{Mathf.RoundToInt(bonus)} · 全网需求 +{links * 4}%（口岸 +{links * 12}%）";
    }
}
