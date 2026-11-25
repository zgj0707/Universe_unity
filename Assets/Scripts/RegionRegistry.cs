using System;
using System.Collections.Generic;
using UnityEngine;

// 区域注册表：集中维护月面与火星四分区的画布范围、缩放、经济和解锁参数。
// 区域 id 直接采用中文原名作为唯一键；旧存档中的 "火星" 在读取时迁移为 "火星·塔尔西斯"。
public partial class DemoBootstrap
{
    public sealed class RegionSpec
    {
        public string id, displayTitle, spacedTitle, planet, texResource, cardBgResource, titleMaskResource, bgmResource, prevRegion;
        public float lonMin, lonMax, latMin, latMax, canvasWidthScale, canvasOffsetX, minZoom, maxZoom, initZoom, kmPerPx, economicScale, unlockIncome;
        public int order, unlockStations;
        public Vector2 panMargin;
        public Color accentColor, neonInk, dayInk;
        public (string text, int size, float lon, float lat)[] terrainLabels;

        public RegionSpec()
        {
            terrainLabels = new (string text, int size, float lon, float lat)[0];
            panMargin = new Vector2(0.25f, 0.25f);
        }
    }

    static readonly Dictionary<string, RegionSpec> RegionSpecs = new Dictionary<string, RegionSpec>(StringComparer.Ordinal)
    {
        {
            "月之亮面", new RegionSpec
            {
                id = "月之亮面", displayTitle = "月之亮面", spacedTitle = "月之亮面", planet = "Moon",
                texResource = "MapArt/moon_nearside", cardBgResource = "PageArt/region_moon_near_bg",
                titleMaskResource = "PageArt/region_title_moon_near", bgmResource = "Audio/bgm_moon_near", prevRegion = "",
                lonMin = -35f, lonMax = 35f, latMin = -62f, latMax = 62f,
                canvasWidthScale = 1f, canvasOffsetX = 0f,
                minZoom = 1f, maxZoom = 6f, initZoom = 1f, kmPerPx = 2.00f, economicScale = 0.4f,
                unlockIncome = 0f, order = 0, unlockStations = 0,
                panMargin = new Vector2(0.25f, 0.25f),
                accentColor = new Color(0.36f, 0.56f, 0.34f, 1f),
                dayInk = new Color(0.15f, 0.12f, 0.10f, 1f),
                neonInk = new Color(1f, 0.90f, 0.48f, 1f)
            }
        },
        {
            "月背", new RegionSpec
            {
                id = "月背", displayTitle = "月背", spacedTitle = "月 背", planet = "Moon",
                texResource = "MapArt/moon_farside", cardBgResource = "PageArt/region_moon_far_bg",
                titleMaskResource = "PageArt/region_title_moon_far", bgmResource = "Audio/bgm_moon_far", prevRegion = "月之亮面",
                lonMin = 145f, lonMax = 215f, latMin = -62f, latMax = 62f,
                canvasWidthScale = 1f, canvasOffsetX = 0f,
                minZoom = 1f, maxZoom = 6f, initZoom = 1f, kmPerPx = 2.00f, economicScale = 0.4f,
                unlockIncome = 600f, order = 1, unlockStations = 4,
                panMargin = new Vector2(0.25f, 0.25f),
                accentColor = new Color(0.24f, 0.43f, 0.66f, 1f),
                dayInk = new Color(0.10f, 0.22f, 0.37f, 1f),
                neonInk = new Color(0.55f, 0.92f, 1f, 1f)
            }
        },
        {
            "火星·西半球", new RegionSpec
            {
                id = "火星·西半球", displayTitle = "火星·西半球", spacedTitle = "西 半 球", planet = "Mars",
                texResource = "MapArt/mars_west", cardBgResource = "PageArt/region_mars_west",
                titleMaskResource = "PageArt/region_title_mars_west", bgmResource = "Audio/bgm_mars", prevRegion = "月背",
                lonMin = -186f, lonMax = 6f, latMin = -68f, latMax = 68f,
                canvasWidthScale = 2.40f, canvasOffsetX = -0.70f,
                minZoom = 0.85f, maxZoom = 12f, initZoom = 0.96f, kmPerPx = 4.60f, economicScale = 0.92f,
                unlockIncome = 1200f, order = 2, unlockStations = 2,
                panMargin = new Vector2(1.65f, 0.25f),
                accentColor = new Color(0.75f, 0.30f, 0.22f, 1f),
                dayInk = new Color(0.98f, 0.94f, 0.88f, 1f),
                neonInk = new Color(1f, 0.74f, 0.52f, 1f),
                terrainLabels = new (string text, int size, float lon, float lat)[]
                {
                    ("塔尔西斯高原", 30, -112f, 18f),
                    ("奥林匹斯山脉", 26, -135f, 24f),
                    ("水手大峡谷", 30, -56f, -12f),
                    ("亚马逊平原", 25, -162f, 14f),
                    ("克律塞平原", 25, -38f, 18f),
                    ("阿耳古瑞盆地", 24, -38f, -48f)
                }
            }
        },
        {
            "火星·东半球", new RegionSpec
            {
                id = "火星·东半球", displayTitle = "火星·东半球", spacedTitle = "东 半 球", planet = "Mars",
                texResource = "MapArt/mars_east", cardBgResource = "PageArt/region_mars_east",
                titleMaskResource = "PageArt/region_title_mars_east", bgmResource = "Audio/bgm_mars", prevRegion = "火星·西半球",
                lonMin = -6f, lonMax = 186f, latMin = -68f, latMax = 68f,
                canvasWidthScale = 2.40f, canvasOffsetX = -0.70f,
                minZoom = 0.85f, maxZoom = 12f, initZoom = 0.96f, kmPerPx = 4.60f, economicScale = 0.92f,
                unlockIncome = 3800f, order = 3, unlockStations = 4,
                panMargin = new Vector2(1.65f, 0.25f),
                accentColor = new Color(0.70f, 0.36f, 0.26f, 1f),
                dayInk = new Color(0.98f, 0.94f, 0.88f, 1f),
                neonInk = new Color(1f, 0.80f, 0.54f, 1f),
                terrainLabels = new (string text, int size, float lon, float lat)[]
                {
                    ("阿拉伯台地", 28, 22f, 24f),
                    ("大瑟提斯高地", 28, 70f, 4f),
                    ("赫拉斯大盆地", 28, 68f, -44f),
                    ("乌托邦平原", 26, 110f, 44f),
                    ("艾律西昂高原", 28, 144f, 22f),
                    ("赫斯珀里亚", 24, 114f, -24f)
                }
            }
        }
    };

    static readonly string[] RegionOrder = { "月之亮面", "月背", "火星·西半球", "火星·东半球" };
    static readonly string[] MarsRegions = { "火星·西半球", "火星·东半球" };

    static RegionSpec GetRegionSpec(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;

        string key = NormalizeLegacyRegionId(id);
        RegionSpec spec;
        return RegionSpecs.TryGetValue(key, out spec) ? spec : null;
    }

    static bool IsMarsRegion(string id)
    {
        if (string.IsNullOrEmpty(id)) return false;
        if (id == "火星") return true;
        for (int i = 0; i < MarsRegions.Length; i++)
            if (MarsRegions[i] == id) return true;
        return false;
    }

    // 旧存档中的 "火星" 及四分区名统一迁移到东西半球，避免旧档丢失进度。
    static string NormalizeLegacyRegionId(string id)
    {
        if (string.IsNullOrEmpty(id)) return id;
        if (id == "火星" || id == "火星·塔尔西斯" || id == "火星·水手谷") return "火星·西半球";
        if (id == "火星·大瑟提斯" || id == "火星·艾律西昂") return "火星·东半球";
        return id;
    }

    // 经纬度 -> 分区画布归一化坐标。火星东西半球跨 ±180° 时先折返再映射。
    static Vector2 GeoToRegionCanvas(string regionId, float lon, float lat)
    {
        string key = NormalizeLegacyRegionId(regionId);
        RegionSpec spec = GetRegionSpec(key);
        if (spec == null) return Vector2.zero;
        if (Mathf.Approximately(spec.lonMax, spec.lonMin) || Mathf.Approximately(spec.latMax, spec.latMin)) return Vector2.zero;

        float adjustedLon = lon;
        if (key == "火星·西半球" && adjustedLon > 6f) adjustedLon -= 360f;
        else if (key == "火星·东半球" && adjustedLon < -6f) adjustedLon += 360f;

        float u = (adjustedLon - spec.lonMin) / (spec.lonMax - spec.lonMin);
        float v = (lat - spec.latMin) / (spec.latMax - spec.latMin);
        return new Vector2(spec.canvasOffsetX + u * spec.canvasWidthScale, Mathf.Clamp(v, 0.04f, 0.96f));
    }

    // 火星全球底图归一化坐标，用于跨分区总览与航线绘制。
    public static Vector2 MarsGlobalNormalized(float lon, float lat)
    {
        float wrappedLon = Mathf.Repeat(lon + 180f, 360f) - 180f;
        float x = 0.04f + ((wrappedLon + 180f) / 360f) * 0.92f;
        float y = 0.06f + ((Mathf.Clamp(lat, -68f, 68f) + 68f) / 136f) * 0.88f;
        return new Vector2(x, y);
    }

    readonly HashSet<string> unlockedRegions = new HashSet<string> { "月之亮面" };

    bool RegionUnlocked(string region)
    {
        if (string.IsNullOrEmpty(region)) return false;

        string normalized = NormalizeLegacyRegionId(region);
        if (unlockedRegions.Contains(region) || unlockedRegions.Contains(normalized)) return true;

        // 迁移期兼容：旧存档的月背/火星解锁位分别对应月背与首块火星区西半球，东半球保持逐级解锁。
        if (normalized == "月背") return moonFarUnlocked;
        if (normalized == "火星·西半球") return marsWestUnlocked;
        return false;
    }

    void SetRegionUnlocked(string region, bool unlocked)
    {
        if (string.IsNullOrEmpty(region)) return;

        string normalized = NormalizeLegacyRegionId(region);
        // 起始区域永不允许被锁死，避免存档损坏后无区域可进。
        if (normalized == "月之亮面")
        {
            unlockedRegions.Add(normalized);
            return;
        }

        if (unlocked) unlockedRegions.Add(normalized);
        else unlockedRegions.Remove(normalized);

        // 迁移期兼容：维持旧解锁开关与首区注册表同步。
        if (normalized == "月背") moonFarUnlocked = unlocked;
        if (normalized == "火星·西半球") marsWestUnlocked = unlocked;
    }
}
