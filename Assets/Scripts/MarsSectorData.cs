using System;
using System.Collections.Generic;
using UnityEngine;

// 火星东西两半球有机策略版：共 104 座火星星港（每半球 52 座）+ 5 座跨星域星门（含 2 座三域超级枢纽）。
// 全半球覆盖 -61° ~ +61° 纬度，按火山、峡谷、盆地、极缘形成有疏有密的非均匀聚落，避开机械网格。
public partial class DemoBootstrap
{
    void PopulateMarsFourSectors()
    {
        BindOlympusGateToAllRegions();
        BindHongwanGateToMarsSectors();
        BindExistingMoonFarGatesToValles();

        BuildWestHemisphereStations();
        BuildEastHemisphereStations();
    }

    void PopulateMarsFourSectorSceneries()
    {
        BuildWestHemisphereSceneries();
        BuildEastHemisphereSceneries();
    }

    // 月火互通主门户：奥林匹斯星门连接「月之亮面 ↔ 火星·西半球」。
    void BindOlympusGateToAllRegions()
    {
        Station station;
        int index = FindStationIndexByExactName("奥林匹斯星门");
        if (index >= 0) station = stations[index];
        else
        {
            station = new Station("奥林匹斯星门", 0f, 0f);
            stations.Add(station);
        }

        const float lon = -134.5f, lat = 20.5f;
        station.region = "火星·西半球";
        station.city = "塔尔西斯星区";
        station.administrativeCode = 200101;
        station.lon = lon; station.lat = lat; station.hasGeo = true; station.roleHint = 4; station.county = false;
        station.portal = true; station.portalTo = "月之亮面"; station.portalTo2 = null;

        Vector2 marsPos = GeoToRegionCanvas("火星·西半球", lon, lat);
        station.x = marsPos.x; station.y = marsPos.y;
        station.BindRegionPos("火星·西半球", marsPos.x, marsPos.y);
        station.BindRegionPos("月之亮面", MoonNearX(31f), MoonNearY(18f));
        station.otherX = MoonNearX(31f); station.otherY = MoonNearY(18f);
        AddStationPortalRegion(station, "火星·西半球");
        AddStationPortalRegion(station, "月之亮面");
    }

    // 超级枢纽星门 ①：虹湾星门，联通 月之亮面 / 月背 / 火星·西半球 三大星域。
    void BindHongwanGateToMarsSectors()
    {
        int index = FindStationIndexByExactName("虹湾星门");
        if (index < 0) return;
        Station station = stations[index];

        const float lon = -86.0f, lat = 51.5f;
        station.lon = lon; station.lat = lat; station.hasGeo = true; station.roleHint = 4; station.county = false;
        station.portal = true;
        station.portalTo = "月背";
        station.portalTo2 = "火星·西半球";
        Vector2 westPos = GeoToRegionCanvas("火星·西半球", lon, lat);
        station.thirdX = westPos.x; station.thirdY = westPos.y;
        station.BindRegionPos("火星·西半球", westPos.x, westPos.y);
        AddStationPortalRegion(station, "月之亮面");
        AddStationPortalRegion(station, "月背");
        AddStationPortalRegion(station, "火星·西半球");
    }

    // 超级枢纽星门 ②：阿波罗星门，联通 月背 / 火星·西半球 / 火星·东半球 三大星域。
    void BindExistingMoonFarGatesToValles()
    {
        int index = FindStationIndexByExactName("阿波罗星门");
        if (index < 0) return;
        Station station = stations[index];

        const float lon = -2.5f, lat = -47.0f;
        station.lon = lon; station.lat = lat; station.hasGeo = true; station.roleHint = 4; station.county = false;
        station.portal = true;
        station.portalTo = "火星·西半球";
        station.portalTo2 = "火星·东半球";
        Vector2 westPos = GeoToRegionCanvas("火星·西半球", lon, lat);
        Vector2 eastPos = GeoToRegionCanvas("火星·东半球", lon, lat);
        station.otherX = westPos.x; station.otherY = westPos.y;
        station.thirdX = eastPos.x; station.thirdY = eastPos.y;
        station.BindRegionPos("火星·西半球", westPos.x, westPos.y);
        station.BindRegionPos("火星·东半球", eastPos.x, eastPos.y);
        AddStationPortalRegion(station, "月背");
        AddStationPortalRegion(station, "火星·西半球");
        AddStationPortalRegion(station, "火星·东半球");
    }

    void RegisterMarsStation(string primaryRegion, string name, string city, int code, float lon, float lat, int roleHint, params string[] portalExtraRegions)
    {
        if (string.IsNullOrEmpty(primaryRegion) || string.IsNullOrEmpty(name)) return;
        if (FindStationIndexByExactName(name) >= 0) return;

        bool hasPortal = portalExtraRegions != null && portalExtraRegions.Length > 0;
        var station = new Station(name, 0f, 0f)
        {
            region = primaryRegion,
            city = city,
            administrativeCode = code,
            county = roleHint == 0,
            lon = lon,
            lat = lat,
            hasGeo = true,
            roleHint = roleHint,
            portal = hasPortal,
            portalRegions = new List<string>()
        };

        Vector2 primaryPos = GeoToRegionCanvas(primaryRegion, lon, lat);
        station.x = primaryPos.x;
        station.y = primaryPos.y;
        station.BindRegionPos(primaryRegion, primaryPos.x, primaryPos.y);

        var extras = new List<string>();
        if (portalExtraRegions != null)
        {
            for (int i = 0; i < portalExtraRegions.Length; i++)
            {
                string region = portalExtraRegions[i];
                if (string.IsNullOrEmpty(region) || region == primaryRegion || extras.Contains(region)) continue;
                extras.Add(region);
            }
        }

        for (int i = 0; i < extras.Count; i++)
        {
            string region = extras[i];
            Vector2 pos = GeoToRegionCanvas(region, lon, lat);
            station.BindRegionPos(region, pos.x, pos.y);
            if (!station.portalRegions.Contains(region)) station.portalRegions.Add(region);
        }

        if (extras.Count > 0) station.portalTo = extras[0];
        if (extras.Count > 1) station.portalTo2 = extras[1];

        stations.Add(station);
    }

    void AddStationPortalRegion(Station station, string region)
    {
        if (station == null || string.IsNullOrEmpty(region)) return;
        if (station.portalRegions == null) station.portalRegions = new List<string>();
        if (!station.portalRegions.Contains(region)) station.portalRegions.Add(region);
    }

    // ---------- 1. 火星·西半球 (-186° ~ +6°) ----------
    // 格局：西北阿卡迪亚极缘 + 亚马逊荒漠孤站 + 门农尼亚南疆群 + 阿尔巴北极冠群 + 奥林匹斯三峰斜链 + 水手裂谷斜带 + 阿耳古瑞南极盆地环 + 北极阿西达利亚冰海
    void BuildWestHemisphereStations()
    {
        const string R = "火星·西半球";

        // 北极边缘 · 阿卡迪亚 - 阿尔巴 - 坦佩高纬带 (+44° ~ +61°)
        RegisterMarsStation(R, "阿卡迪亚西岸", "阿卡迪亚西界", 210122, -176.5f, 54.5f, 0);
        RegisterMarsStation(R, "阿卡迪亚北站", "阿卡迪亚区", 210113, -158.0f, 59.0f, 1);
        RegisterMarsStation(R, "阿刻戎堑沟", "阿刻戎区", 210105, -143.0f, 46.5f, 0);
        RegisterMarsStation(R, "阿尔巴山", "阿尔巴北区", 210109, -118.5f, 54.0f, 3);
        RegisterMarsStation(R, "塔尔西斯丘", "塔尔西斯北", 210125, -104.0f, 44.5f, 1);
        RegisterMarsStation(R, "坦佩台地", "坦佩区", 220111, -72.0f, 42.5f, 0);
        RegisterMarsStation(R, "卡塞谷", "卡塞北区", 220102, -57.5f, 53.0f, 0);
        RegisterMarsStation(R, "阿西达利亚南", "克律塞北区", 220115, -36.0f, 46.0f, 0);
        RegisterMarsStation(R, "阿西达利亚海", "阿西达利亚区", 220108, -21.5f, 58.5f, 1);
        RegisterMarsStation(R, "阿拉伯西岸", "阿拉伯西界", 220119, -3.5f, 52.0f, 0);

        // 西部亚马逊平原荒漠散落站（刻意拉出宽广间隔）
        RegisterMarsStation(R, "亚马逊平原", "亚马逊星区", 210101, -166.0f, 27.0f, 2);
        RegisterMarsStation(R, "戈尔迪背斜", "亚马逊东区", 210102, -153.5f, 8.5f, 0);

        // 西南门农尼亚 - 叙利亚南疆高地弧 (-59° ~ -24°)
        RegisterMarsStation(R, "曼加拉谷", "门农尼亚区", 210103, -159.0f, -24.5f, 1);
        RegisterMarsStation(R, "西绪福斯原", "门农尼亚南区", 210104, -167.0f, -45.0f, 0);
        RegisterMarsStation(R, "亚马逊西门", "亚马逊西界", 210121, -177.5f, -57.5f, 4, "火星·东半球");
        RegisterMarsStation(R, "比布利斯站", "塔尔西斯西区", 210115, -142.5f, -34.0f, 0);
        RegisterMarsStation(R, "叙利亚南港", "门农尼亚东", 210116, -135.5f, -56.0f, 1);

        // 中部奥林匹斯 - 塔尔西斯三峰斜向火山链（避开正竖列）
        RegisterMarsStation(R, "奥林西谷", "奥林匹斯北", 210114, -145.5f, 15.0f, 0);
        RegisterMarsStation(R, "米伦尼亚谷", "奥林匹斯南", 210124, -133.5f, -6.5f, 0);
        RegisterMarsStation(R, "朱维斯谷", "塔尔西斯中区", 210117, -116.0f, 27.5f, 0);
        RegisterMarsStation(R, "帕弗尼斯峰", "塔尔西斯中区", 210106, -121.0f, 3.5f, 2);
        RegisterMarsStation(R, "阿尔西亚峰", "塔尔西斯南区", 210107, -124.5f, -21.0f, 1);
        RegisterMarsStation(R, "伊卡里亚堑沟", "伊卡里亚区", 210108, -115.0f, -49.5f, 0);

        // 塔尔西斯东麓 - 诺克提斯 - 陶马西亚高地
        RegisterMarsStation(R, "乌拉纽斯山", "乌拉纽斯区", 210118, -94.0f, 24.5f, 0);
        RegisterMarsStation(R, "阿斯克雷乌斯", "塔尔西斯东区", 210110, -104.5f, 13.0f, 2);
        RegisterMarsStation(R, "诺克提斯迷宫", "诺克提斯区", 210123, -98.0f, -8.5f, 3);
        RegisterMarsStation(R, "克拉罗斯堑沟", "克拉罗斯区", 210111, -101.5f, -35.5f, 0);
        RegisterMarsStation(R, "陶马西亚高地", "陶马西亚区", 210112, -96.5f, -59.0f, 3);
        RegisterMarsStation(R, "乌拉纽斯谷", "乌拉纽斯中区", 210120, -83.5f, -26.5f, 1);
        RegisterMarsStation(R, "凤凰湖门户", "水手谷西南界", 210119, -76.5f, -51.5f, 2);

        // 水手大峡谷斜向裂谷聚落（自西向东微倾）
        RegisterMarsStation(R, "伊乌斯峡谷", "水手谷西区", 220101, -76.0f, -3.5f, 1);
        RegisterMarsStation(R, "提托努斯峡谷", "水手谷西麓", 220112, -68.5f, -19.5f, 0);
        RegisterMarsStation(R, "卢纳高原", "卢纳星区", 220113, -64.5f, 16.5f, 1);
        RegisterMarsStation(R, "奥菲尔峡谷", "水手谷北壁", 220121, -55.0f, 4.0f, 0);
        RegisterMarsStation(R, "坎多尔峡谷", "水手谷中区", 220103, -52.5f, -15.5f, 3);
        RegisterMarsStation(R, "科普剌塔斯", "水手谷东口", 220122, -41.5f, -9.0f, 1);
        RegisterMarsStation(R, "梅拉斯峡谷", "水手谷东区", 220106, -34.0f, -25.0f, 2);

        // 克律塞冲积平原与东缘门户
        RegisterMarsStation(R, "克律塞平原", "克律塞星区", 220105, -40.5f, 21.0f, 2);
        RegisterMarsStation(R, "马沃斯谷", "阿拉伯西区", 220116, -21.0f, 30.5f, 0);
        RegisterMarsStation(R, "希尔达谷", "克律塞东角", 220125, -7.5f, 22.0f, 0);
        RegisterMarsStation(R, "阿瑞斯谷", "克律塞东区", 220109, -18.5f, 6.5f, 3);
        RegisterMarsStation(R, "马里内利谷", "珍珠台地", 220123, -22.5f, -14.5f, 0);
        RegisterMarsStation(R, "子午线星门", "子午线西界", 220118, -1.5f, -6.5f, 4, "火星·东半球");

        // 南极边缘 · 西瑞努姆 - 阿耳古瑞巨盆环 (-60° ~ -36°)
        RegisterMarsStation(R, "西瑞努姆高地", "西瑞努姆区", 220104, -63.0f, -43.5f, 0);
        RegisterMarsStation(R, "博斯普鲁斯", "博斯普区", 220114, -50.0f, -56.5f, 0);
        RegisterMarsStation(R, "阿耳古瑞盆地", "阿耳古瑞区", 220107, -37.5f, -47.0f, 3);
        RegisterMarsStation(R, "卢罗斯谷", "水手谷南麓", 220110, -24.5f, -38.0f, 0);
        RegisterMarsStation(R, "查尔斯环", "诺亚西区", 220117, -23.0f, -58.5f, 0);
        RegisterMarsStation(R, "尼尔加尔谷", "珍珠南原", 220124, -10.5f, -27.5f, 0);
        RegisterMarsStation(R, "诺亚西谷", "诺亚西界", 220120, -6.0f, -60.5f, 0);
    }

    // ---------- 2. 火星·东半球 (-6° ~ +186°) ----------
    // 格局：北纬乌托邦-德特罗极冰弧 + 阿拉伯台地稀疏沙海 + 大瑟提斯斜向都会群 + 赫拉斯南极深盆环 + 艾律西昂火山群 + 远东孤岛群
    void BuildEastHemisphereStations()
    {
        const string R = "火星·东半球";

        // 北半球高纬 · 阿拉伯北岸 - 乌托邦极缘冰原带 (+42° ~ +61°)
        RegisterMarsStation(R, "贝克尔环", "贝克尔区", 230102, 12.5f, 54.0f, 0);
        RegisterMarsStation(R, "德特罗尼鲁斯", "德特罗区", 230105, 31.0f, 58.5f, 0);
        RegisterMarsStation(R, "普罗托尼鲁斯", "普罗托区", 230109, 51.5f, 51.0f, 0);
        RegisterMarsStation(R, "尼利堑沟", "尼利区", 230119, 69.5f, 44.5f, 0);
        RegisterMarsStation(R, "塞伯拉斯北口", "大瑟提斯北", 230122, 85.0f, 58.0f, 0);
        RegisterMarsStation(R, "乌托邦平原", "乌托邦东区", 240101, 106.5f, 53.5f, 3);
        RegisterMarsStation(R, "卡西乌斯港", "乌托邦北", 240115, 127.0f, 59.0f, 0);
        RegisterMarsStation(R, "赫卡忒火山", "赫卡忒区", 240117, 149.0f, 49.5f, 0);
        RegisterMarsStation(R, "弗莱格拉山脉", "弗莱格拉区", 240109, 169.5f, 53.0f, 0);

        // 西部阿拉伯台地（稀疏斜向分布，留出大块沙海走廊）
        RegisterMarsStation(R, "沙尔巴塔纳", "阿拉伯北岸", 230124, 5.5f, 35.0f, 0);
        RegisterMarsStation(R, "阿拉伯台地", "阿拉伯东区", 230101, 20.5f, 26.5f, 2);
        RegisterMarsStation(R, "克龙梅林环", "阿拉伯中区", 230114, 14.0f, 4.5f, 0);
        RegisterMarsStation(R, "子午线平原", "子午线东区", 230103, 12.5f, -21.0f, 1);
        RegisterMarsStation(R, "提柯尼亚", "提柯尼亚区", 230125, 29.0f, -6.5f, 0);
        RegisterMarsStation(R, "卡西尼环", "阿拉伯东界", 230115, 37.5f, 30.0f, 1);
        RegisterMarsStation(R, "萨巴亚高地", "萨巴亚区", 230106, 44.0f, 11.5f, 0);

        // 大瑟提斯 - 耶泽罗 - 伊希地西岸弧形聚落
        RegisterMarsStation(R, "安东尼亚迪", "安东尼亚区", 230117, 56.5f, 24.5f, 0);
        RegisterMarsStation(R, "惠更斯盆地", "惠更斯区", 230108, 51.5f, -9.5f, 1);
        RegisterMarsStation(R, "耶泽罗陨石坑", "耶泽罗区", 230112, 74.5f, 21.5f, 3);
        RegisterMarsStation(R, "大瑟提斯高原", "大瑟提斯区", 230111, 69.0f, 1.5f, 2);
        RegisterMarsStation(R, "伊希地西港", "伊希地西界", 230126, 86.5f, 14.0f, 1);
        RegisterMarsStation(R, "法蒂玛台地", "伊希地北", 230127, 94.5f, 36.0f, 0);

        // 南半球 · 诺亚高地 - 赫拉斯巨盆环 - 普罗米修斯极缘 (-61° ~ -24°)
        RegisterMarsStation(R, "诺亚高地", "诺亚东区", 230104, 14.0f, -49.5f, 3);
        RegisterMarsStation(R, "霍尔登环", "霍尔登区", 230107, 32.5f, -31.0f, 0);
        RegisterMarsStation(R, "达尔文南站", "南极西区", 230116, 36.0f, -57.5f, 0);
        RegisterMarsStation(R, "埃伯斯瓦尔德", "埃伯斯区", 230118, 50.5f, -33.5f, 0);
        RegisterMarsStation(R, "赫拉斯盆地", "赫拉斯区", 230110, 62.5f, -48.0f, 3);
        RegisterMarsStation(R, "哈德里亚库斯", "哈德里亚区", 230113, 76.0f, -24.5f, 0);
        RegisterMarsStation(R, "赫拉斯东港", "赫拉斯南区", 230123, 79.0f, -56.0f, 1);
        RegisterMarsStation(R, "伊希地东门", "伊希地东界", 230120, 92.5f, -33.5f, 2);
        RegisterMarsStation(R, "赫拉斯山麓", "赫拉斯东界", 230121, 98.5f, -60.5f, 0);

        // 中东 · 祝融 - 赫斯珀里亚 - 盖尔盆地
        RegisterMarsStation(R, "祝融着陆港", "乌托邦南区", 240102, 111.5f, 26.5f, 2);
        RegisterMarsStation(R, "阿蒙蒂斯平原", "阿蒙蒂斯区", 240103, 105.0f, -2.5f, 0);
        RegisterMarsStation(R, "赫斯珀里亚", "赫斯珀里亚", 240104, 113.0f, -24.0f, 1);
        RegisterMarsStation(R, "普罗米修斯", "普罗米修斯", 240105, 116.0f, -52.5f, 0);
        RegisterMarsStation(R, "赛克利俄斯", "赫斯珀里南", 240121, 123.5f, -8.5f, 0);
        RegisterMarsStation(R, "盖尔陨石坑", "盖尔区", 240108, 134.5f, -27.5f, 3);
        RegisterMarsStation(R, "埃里达尼亚", "埃里达尼区", 240116, 136.0f, -56.5f, 0);

        // 艾律西昂火山群（呈三角形斜落）
        RegisterMarsStation(R, "艾律西昂山", "艾律西昂峰", 240106, 133.5f, 29.0f, 3);
        RegisterMarsStation(R, "阿尔博尔火山", "阿尔博尔区", 240118, 148.5f, 22.0f, 0);
        RegisterMarsStation(R, "艾律西昂平原", "艾律西昂南", 240107, 137.0f, 4.5f, 2);
        RegisterMarsStation(R, "刻耳柏洛斯", "刻耳柏洛斯", 240110, 152.5f, -2.0f, 1);
        RegisterMarsStation(R, "仄费里亚平原", "仄费里亚区", 240111, 151.0f, -28.0f, 0);
        RegisterMarsStation(R, "马莱阿高原", "马莱阿区", 240114, 154.5f, -54.0f, 0);

        // 远东岸 · 俄耳枯斯 - 古瑟夫 - 阿波罗尼奥
        RegisterMarsStation(R, "埃律西昂东麓", "刻耳柏东", 240122, 163.5f, 14.5f, 0);
        RegisterMarsStation(R, "卡普里峡谷", "卡普里南", 240123, 162.0f, -40.5f, 0);
        RegisterMarsStation(R, "俄耳枯斯谷", "俄耳枯斯区", 240119, 174.5f, 25.0f, 0);
        RegisterMarsStation(R, "梅迪乌斯谷", "远东界北", 240124, 181.5f, 43.5f, 0);
        RegisterMarsStation(R, "古瑟夫陨石坑", "古瑟夫区", 240112, 173.5f, -8.5f, 3);
        RegisterMarsStation(R, "阿波利纳里斯", "阿波利纳区", 240113, 174.0f, -31.5f, 0);
        RegisterMarsStation(R, "马阿迪姆谷", "马阿迪姆区", 240120, 172.5f, -58.5f, 0);
        RegisterMarsStation(R, "阿波罗尼奥", "远东界南", 240125, 182.0f, -20.5f, 1);
    }

    // ---------- 东西半球奇观（紧贴最新非均匀聚落核心） ----------
    void BuildWestHemisphereSceneries()
    {
        const string R = "火星·西半球";
        AddMarsScenery(R, "奥林匹斯峰", "奥林匹斯星门", -134.5f, 20.5f, true, 0.036f, 0.036f);
        AddMarsScenery(R, "阿尔巴台地", "阿尔巴山", -118.5f, 54.0f, true, -0.036f, 0.036f);
        AddMarsScenery(R, "陶马西亚原", "陶马西亚高地", -96.5f, -59.0f, true, 0.036f, 0.034f);
        AddMarsScenery(R, "坎多尔光柱", "坎多尔峡谷", -52.5f, -15.5f, true, -0.036f, 0.036f);
        AddMarsScenery(R, "克律塞纪念", "克律塞平原", -40.5f, 21.0f, false, 0.036f, 0.036f);
        AddMarsScenery(R, "阿耳古瑞环", "阿耳古瑞盆地", -37.5f, -47.0f, true, -0.036f, -0.032f);
    }

    void BuildEastHemisphereSceneries()
    {
        const string R = "火星·东半球";
        AddMarsScenery(R, "诺亚方尖碑", "诺亚高地", 14.0f, -49.5f, false, -0.036f, 0.036f);
        AddMarsScenery(R, "大瑟提斯原", "大瑟提斯高原", 69.0f, 1.5f, true, -0.036f, 0.036f);
        AddMarsScenery(R, "赫拉斯深坑", "赫拉斯盆地", 62.5f, -48.0f, true, -0.036f, -0.032f);
        AddMarsScenery(R, "耶泽罗水岸", "耶泽罗陨石坑", 74.5f, 21.5f, true, 0.036f, 0.036f);
        AddMarsScenery(R, "艾律西昂峰", "艾律西昂山", 133.5f, 29.0f, true, 0.036f, 0.036f);
        AddMarsScenery(R, "乌托邦冰原", "乌托邦平原", 106.5f, 53.5f, true, -0.036f, 0.034f);
        AddMarsScenery(R, "盖尔着陆点", "盖尔陨石坑", 134.5f, -27.5f, false, -0.036f, -0.034f);
        AddMarsScenery(R, "古瑟夫陨坑", "古瑟夫陨石坑", 173.5f, -8.5f, true, 0.036f, 0.036f);
    }

    void AddMarsScenery(string region, string name, string triggerName, float lon, float lat, bool natural, float offsetX, float offsetY)
    {
        for (int i = 0; i < sceneries.Count; i++)
        {
            if (sceneries[i].name == name && sceneries[i].region == region) return;
        }

        Vector2 pos = GeoToRegionCanvas(region, lon, lat);
        RegionSpec spec = GetRegionSpec(region);
        float minX = spec != null ? spec.canvasOffsetX + 0.06f : 0.08f;
        float maxX = spec != null ? spec.canvasOffsetX + spec.canvasWidthScale - 0.06f : 0.92f;
        sceneries.Add(new Scenery
        {
            name = name,
            x = Mathf.Clamp(pos.x + offsetX, minX, maxX),
            y = Mathf.Clamp(pos.y + offsetY, 0.10f, 0.90f),
            natural = natural,
            triggerStation = triggerName,
            region = region
        });
    }
}
