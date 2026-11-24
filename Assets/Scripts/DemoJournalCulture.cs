using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class DemoBootstrap
{
    // ---------- 星图志 · 星域节候、荣誉星牌、复古纪念星票与全星域拥堵速览 ----------

    int sceneryCollectionTab = 0; // 0: 名胜风光, 1: 纪念票册, 2: 荣誉星牌, 3: 二十四候
    static readonly Dictionary<string, Texture2D> ornamentTexCache = new Dictionary<string, Texture2D>();

    Texture2D LoadOrnamentTex(string key)
    {
        if (string.IsNullOrEmpty(key)) return null;
        if (ornamentTexCache.TryGetValue(key, out var cached)) return cached;
        var tex = Resources.Load<Texture2D>("PageArt/Ornaments/" + key);
        ornamentTexCache[key] = tex;
        return tex;
    }

    /// <summary>在指定容器内铺设星际辉光/星云纹样，自动关闭 raycastTarget 以免遮挡交互。</summary>
    RawImage StampBotanicalSprig(Transform parent, string key, float x, float y, float w, float h,
        float alpha = 0.72f, bool flipX = false, bool flipY = false, float rotZ = 0f, Color? tint = null)
    {
        var tex = LoadOrnamentTex(key);
        if (parent == null || tex == null) return null;
        var go = Rect("Orn_" + key, parent, x, y, w, h);
        var raw = go.AddComponent<RawImage>();
        raw.texture = tex;
        Color baseCol = tint ?? Color.white;
        raw.color = new Color(baseCol.r, baseCol.g, baseCol.b, Mathf.Clamp01(alpha));
        raw.raycastTarget = false;
        raw.uvRect = new Rect(flipX ? 1f : 0f, flipY ? 1f : 0f, flipX ? -1f : 1f, flipY ? -1f : 1f);
        if (!Mathf.Approximately(rotZ, 0f)) go.transform.localRotation = Quaternion.Euler(0f, 0f, rotZ);
        return raw;
    }

    /// <summary>在矩形卡片四角对称盖印工笔回纹祥云角花。</summary>
    void StampCloudCornerSet(Transform parent, float w, float h, float size = 48f, float inset = 8f,
        float alpha = 0.56f, Color? tint = null)
    {
        StampBotanicalSprig(parent, "orn_cloud_fret", inset, h - inset - size, size, size, alpha, false, false, 0f, tint);
        StampBotanicalSprig(parent, "orn_cloud_fret", w - inset - size, h - inset - size, size, size, alpha, true, false, 0f, tint);
        StampBotanicalSprig(parent, "orn_cloud_fret", inset, inset, size, size, alpha, false, true, 0f, tint);
        StampBotanicalSprig(parent, "orn_cloud_fret", w - inset - size, inset, size, size, alpha, true, true, 0f, tint);
    }

    /// <summary>居中放置星际辉光饰带。</summary>
    void StampFloralDivider(Transform parent, float centerX, float y, float w, float h = 38f,
        float alpha = 0.76f, Color? tint = null)
    {
        StampBotanicalSprig(parent, "orn_floral_divider", centerX - w * 0.5f, y, w, h, alpha, false, false, 0f, tint);
    }

    /// <summary>为手帐卡片或荣誉星牌边框绘制一圈精细内框线。</summary>
    void StampFineRuleFrame(Transform parent, float w, float h, float inset = 10f, Color? borderColor = null)
    {
        if (parent == null || w <= inset * 2f + 4f || h <= inset * 2f + 4f) return;
        Color col = borderColor ?? new Color(InkSoft.r, InkSoft.g, InkSoft.b, 0.24f);
        float t = 1.5f;
        Img(Rect("FrameTop", parent, inset, h - inset - t, w - inset * 2f, t), col).raycastTarget = false;
        Img(Rect("FrameBot", parent, inset, inset, w - inset * 2f, t), col).raycastTarget = false;
        Img(Rect("FrameL", parent, inset, inset, t, h - inset * 2f), col).raycastTarget = false;
        Img(Rect("FrameR", parent, w - inset - t, inset, t, h - inset * 2f), col).raycastTarget = false;
    }

    /// <summary>切入全屏「星图鉴」并直接定位到指定页签（0: 名胜, 1: 票册, 2: 荣誉星牌, 3: 节候）。</summary>
    void OpenSceneryTab(int tab)
    {
        sceneryCollectionTab = Mathf.Clamp(tab, 0, 3);
        ClosePanel(true);
        OpenSceneryScreen();
    }

    sealed class SolarTermSpec
    {
        public string name, season, poem, effectHint;
        public float demandMult, sceneryDemandMult, revenueMult, maintMult, commercialMult;
    }

    sealed class CharterPlaqueSpec
    {
        public string key, plaqueText, title, conditionText, rewardHint, motto;
        public int rewardCoins, rewardRp;
        public Func<bool> checkReady;
    }

    sealed class MemorialTicketSpec
    {
        public string key, serial, title, routeText, stampText, regionTag, motto;
        public Func<bool> checkEarned;
    }

    sealed class CongestedSegmentInfo
    {
        public Line line;
        public int segIndex, stA, stB;
        public float loadRatio;
        public string region;
    }

    static readonly SolarTermSpec[] SolarTerms =
    {
        new SolarTermSpec {
            name = "立春", season = "春 · 启蛰", poem = "东郊迎春柳丝长，首舰初发跃星岗。",
            effectHint = "全网日常客流 +4%",
            demandMult = 1.04f, sceneryDemandMult = 1.04f, revenueMult = 1.00f, maintMult = 1.00f, commercialMult = 1.00f
        },
        new SolarTermSpec {
            name = "清明", season = "春 · 踏青", poem = "星尘轻雾拂舷窗，远客踏春登星台。",
            effectHint = "名胜站客流 +10%",
            demandMult = 1.00f, sceneryDemandMult = 1.10f, revenueMult = 1.00f, maintMult = 1.00f, commercialMult = 1.00f
        },
        new SolarTermSpec {
            name = "谷雨", season = "春 · 新茗", poem = "雨前茶香出火星，一笛清风入月之亮面。",
            effectHint = "全网星票款收益 +6%",
            demandMult = 1.00f, sceneryDemandMult = 1.02f, revenueMult = 1.06f, maintMult = 1.00f, commercialMult = 1.00f
        },
        new SolarTermSpec {
            name = "端午", season = "夏 · 竞渡", poem = "鼓声十里红旗动，三地游人共趁墟。",
            effectHint = "名胜客流 +12% · 维保微增 +4%",
            demandMult = 1.02f, sceneryDemandMult = 1.12f, revenueMult = 1.00f, maintMult = 1.04f, commercialMult = 1.04f
        },
        new SolarTermSpec {
            name = "小暑", season = "夏 · 荫街", poem = "星港廊街竹影荫茶座，星风穿过港前街。",
            effectHint = "港前商用物业日租 +10%",
            demandMult = 1.00f, sceneryDemandMult = 1.00f, revenueMult = 1.00f, maintMult = 1.02f, commercialMult = 1.10f
        },
        new SolarTermSpec {
            name = "立秋", season = "秋 · 商埠", poem = "一叶知秋商旅早，货栈灯火接黎明。",
            effectHint = "全网星票款 +5% · 日常客流 +3%",
            demandMult = 1.03f, sceneryDemandMult = 1.03f, revenueMult = 1.05f, maintMult = 1.00f, commercialMult = 1.00f
        },
        new SolarTermSpec {
            name = "白露", season = "秋 · 清爽", poem = "蒹葭白露风清朗，鸣笛清越不沾尘。",
            effectHint = "秋高护路 · 星轨与星舰维保 -6%",
            demandMult = 1.00f, sceneryDemandMult = 1.00f, revenueMult = 1.00f, maintMult = 0.94f, commercialMult = 1.00f
        },
        new SolarTermSpec {
            name = "中秋", season = "秋 · 团圆", poem = "月悬星海千尺镜，家书随轨到天涯。",
            effectHint = "全网客流 +8% · 名胜与探亲大热 +14%",
            demandMult = 1.08f, sceneryDemandMult = 1.14f, revenueMult = 1.03f, maintMult = 1.00f, commercialMult = 1.06f
        },
        new SolarTermSpec {
            name = "霜降", season = "秋 · 丰市", poem = "满城金橘秋收旺，星港市声暖融融。",
            effectHint = "物业租金 +10% · 客流 +4%",
            demandMult = 1.04f, sceneryDemandMult = 1.04f, revenueMult = 1.00f, maintMult = 1.00f, commercialMult = 1.10f
        },
        new SolarTermSpec {
            name = "立冬", season = "冬 · 夜墟", poem = "老街砂锅烟火暖，夜市归人乘末班。",
            effectHint = "全网星票款收益 +7%",
            demandMult = 1.02f, sceneryDemandMult = 1.02f, revenueMult = 1.07f, maintMult = 1.00f, commercialMult = 1.04f
        },
        new SolarTermSpec {
            name = "冬至", season = "冬 · 岁宴", poem = "冬至大过年，星轨万里共盏茶。",
            effectHint = "全网客流 +10% · 维保微增 +4%",
            demandMult = 1.10f, sceneryDemandMult = 1.10f, revenueMult = 1.04f, maintMult = 1.04f, commercialMult = 1.00f
        },
        new SolarTermSpec {
            name = "迎春", season = "冬 · 花市", poem = "水仙年橘装满舱，千灯照路待新春。",
            effectHint = "名胜花市客流 +14% · 物业日租 +10%",
            demandMult = 1.05f, sceneryDemandMult = 1.14f, revenueMult = 1.04f, maintMult = 1.00f, commercialMult = 1.10f
        }
    };

    readonly List<string> earnedPlaques = new List<string>();
    readonly List<string> earnedMemorialTickets = new List<string>();
    int retiredVehiclesCount;
    int lastDisplayedSolarTerm = -1;

    CharterPlaqueSpec[] BuildCharterPlaques()
    {
        return new[]
        {
            new CharterPlaqueSpec {
                key = "plaque_tongqu", plaqueText = "四通八达", title = "第一阶段 · 星港通衢考成",
                conditionText = "全网运营满 4 条航线，且已连通至少 12 座星港",
                rewardHint = "奖金 ¥1,600 · 12 RP · 永久票务收益 +2%",
                motto = "星轨初成络脉通，晨曦鸣笛动星关。",
                rewardCoins = 1600, rewardRp = 12,
                checkReady = () => lines.Count >= 4 && DailyConnectedCount() >= 12
            },
            new CharterPlaqueSpec {
                key = "plaque_shanhai", plaqueText = "星域通济", title = "第二阶段 · 三星锁钥考成",
                conditionText = "激活至少 2 条跨星域口岸走廊，并打卡 5 处名胜",
                rewardHint = "奖金 ¥3,600 · 18 RP · 永久口岸联运补贴 +15%",
                motto = "星港连城水天一色，三地同轨共兴。",
                rewardCoins = 3600, rewardRp = 18,
                checkReady = () => CrossBaySynergyLinkCount() >= 2 && FoundCount() >= 5
            },
            new CharterPlaqueSpec {
                key = "plaque_baiye", plaqueText = "百业兴隆", title = "第三阶段 · 星港百业考成",
                conditionText = "落成 4 处港前商业物业，且星舰拥有量与功勋退役合计 ≥ 6 艘",
                rewardHint = "奖金 ¥6,000 · 24 RP · 永久港前物业日租 +12%",
                motto = "港前茶楼书肆立，客来财聚岁年丰。",
                rewardCoins = 6000, rewardRp = 24,
                checkReady = () => DevelopedCommercialCount() >= 4 && (researchVehicles.Count + retiredVehiclesCount) >= 6
            },
            new CharterPlaqueSpec {
                key = "plaque_nantian", plaqueText = "南天柱石", title = "终阶段 · 星轨垂范大考",
                conditionText = "三地口岸 3/3 全贯通，且星际商道公司树已研满 15 项",
                rewardHint = "奖金 ¥12,000 · 36 RP · 永久全网维保费用 -5%",
                motto = "百年商道兼工程，深空星脉永流芳。",
                rewardCoins = 12000, rewardRp = 36,
                checkReady = () => CrossBaySynergyLinkCount() >= 3 && CoTechEraDoneCount(0) >= 15
            }
        };
    }

    MemorialTicketSpec[] BuildMemorialTicketSpecs()
    {
        return new[]
        {
            new MemorialTicketSpec {
                key = "tkt_first_line", serial = "NO.2045", title = "月面初航 · 首班登舰票",
                routeText = "静海基地 ━━▶ 阿里亚德港", stampText = "首班\n启航", regionTag = "月之亮面",
                motto = "第一道星轨落笔成线，拓荒者怀票登舰。",
                checkEarned = () => lines.Count >= 1
            },
            new MemorialTicketSpec {
                key = "tkt_hk_6", serial = "NO.2058", title = "静海鸣笛 · 亮面联线票",
                routeText = "静海基地 ━━▶ 哥白尼环形山", stampText = "亮面\n巡航", regionTag = "月之亮面",
                motto = "星港连珠六站成网，真空引擎与信标微光相和。",
                checkEarned = () => RegionConnectedCount("月之亮面") >= 6
            },
            new MemorialTicketSpec {
                key = "tkt_macau", serial = "NO.2082", title = "静默远征 · 月背深空票",
                routeText = "冯·卡门港 ━━▶ 莫斯科海", stampText = "月背\n静默", regionTag = "月背",
                motto = "越过月面边界踏入无线电静默区，艾特肯深井灯火长明。",
                checkEarned = () => macauUnlocked && RegionConnectedCount("月背") >= 2
            },
            new MemorialTicketSpec {
                key = "tkt_gd", serial = "NO.2110", title = "赤砂远航 · 火星跨星域票",
                routeText = "奥林匹斯星门 ━━▶ 米拉斯深谷", stampText = "火星\n赤砂", regionTag = "火星",
                motto = "跨越地月引力井直抵水手大峡谷，行星际干线雏形已现。",
                checkEarned = () => RegionUnlocked("火星·西半球") && (RegionConnectedCount("火星·西半球") + RegionConnectedCount("火星·东半球")) >= 3
            },
            new MemorialTicketSpec {
                key = "tkt_bay", serial = "NO.2135", title = "三星金钥 · 跨星域联运票",
                routeText = "月之亮面 ━━▶ 月背 / 火星", stampText = "三星\n联运", regionTag = "星域环线",
                motto = "一票可跨月面、月背与火星，深空天堑皆为通途。",
                checkEarned = () => CrossBaySynergyLinkCount() >= 1
            },
            new MemorialTicketSpec {
                key = "tkt_retire", serial = "NO.2160", title = "功勋星舰 · 荣归纪念票",
                routeText = "星港船坞 ━━▶ 星航陈列馆", stampText = "功勋\n陈列", regionTag = "总部",
                motto = "曾推动星纪元的初代飞车静静卸甲，核热余温长存。",
                checkEarned = () => retiredVehiclesCount >= 1 || researchVehicles.Count >= 4
            }
        };
    }

    int RegionConnectedCount(string region)
    {
        var connected = new HashSet<int>();
        for (int li = 0; li < lines.Count; li++)
        {
            var st = lines[li].st;
            for (int i = 0; i < st.Count; i++)
            {
                int idx = st[i];
                if (idx >= 0 && idx < stations.Count && stations[idx].built && !stations[idx].closed && stations[idx].region == region)
                    connected.Add(idx);
            }
        }
        return connected.Count;
    }

    int CurrentSolarTermIndex()
    {
        return ((Mathf.Max(1, day) - 1) / 3) % SolarTerms.Length;
    }

    SolarTermSpec CurrentSolarTerm() => SolarTerms[CurrentSolarTermIndex()];

    SolarTermSpec NextSolarTerm() => SolarTerms[(CurrentSolarTermIndex() + 1) % SolarTerms.Length];

    int SolarTermDaysRemaining()
    {
        int rem = 3 - ((Mathf.Max(1, day) - 1) % 3);
        return Mathf.Clamp(rem, 1, 3);
    }

    float SolarDemandMult(bool isScenery)
    {
        var term = CurrentSolarTerm();
        return isScenery ? term.sceneryDemandMult : term.demandMult;
    }

    float SolarRevenueMult() => CurrentSolarTerm().revenueMult;
    float SolarMaintMult() => CurrentSolarTerm().maintMult;
    float SolarCommercialMult() => CurrentSolarTerm().commercialMult;

    float CultureTicketDemandMult()
    {
        int cnt = earnedMemorialTickets.Count;
        return cnt >= 6 ? 1.04f : cnt >= 3 ? 1.02f : 1f;
    }

    float PlaqueRevenueMult() => earnedPlaques.Contains("plaque_tongqu") ? 1.02f : 1f;
    float PlaqueBayBonusMult() => earnedPlaques.Contains("plaque_shanhai") ? 1.15f : 1f;
    float PlaqueCommercialMult() => earnedPlaques.Contains("plaque_baiye") ? 1.12f : 1f;
    float PlaqueMaintMult() => earnedPlaques.Contains("plaque_nantian") ? 0.95f : 1f;

    string PlayerPlaqueHonorTag()
    {
        if (earnedPlaques.Contains("plaque_nantian")) return " · 「南天柱石」";
        if (earnedPlaques.Contains("plaque_baiye")) return " · 「百业兴隆」";
        if (earnedPlaques.Contains("plaque_shanhai")) return " · 「星域通济」";
        if (earnedPlaques.Contains("plaque_tongqu")) return " · 「四通八达」";
        return "";
    }

    /// <summary>同步检查纪念星票解锁与新节气变更提示。</summary>
    void SyncCulturalCollection(bool notify)
    {
        var specs = BuildMemorialTicketSpecs();
        int added = 0;
        string lastTitle = "";
        for (int i = 0; i < specs.Length; i++)
        {
            if (earnedMemorialTickets.Contains(specs[i].key)) continue;
            if (specs[i].checkEarned != null && specs[i].checkEarned())
            {
                earnedMemorialTickets.Add(specs[i].key);
                lastTitle = specs[i].title;
                added++;
            }
        }
        if (added > 0 && notify) SaveGame();
        if (added > 0 && notify)
        {
            hasForecastSignature = false;
            operatingRates.Clear();
            RefreshPendingBadge();
            Toast("星图票册新藏纪念星票：「" + lastTitle + "」（" + earnedMemorialTickets.Count + "/" + specs.Length + "）");
        }
    }

    void NotifySolarTermRollover()
    {
        int idx = CurrentSolarTermIndex();
        if (lastDisplayedSolarTerm >= 0 && idx != lastDisplayedSolarTerm)
        {
            var term = SolarTerms[idx];
            hasForecastSignature = false;
            operatingRates.Clear();
            RecalcIncome();
            Toast("节气入【" + term.name + "】· " + term.effectHint);
        }
        if (lastDisplayedSolarTerm != idx) { lastDisplayedSolarTerm = idx; SaveGame(); }
    }

    bool TryClaimCharterPlaque(CharterPlaqueSpec spec)
    {
        if (spec == null || earnedPlaques.Contains(spec.key)) return false;
        if (spec.checkReady == null || !spec.checkReady()) return false;
        earnedPlaques.Add(spec.key);
        coins += spec.rewardCoins;
        rp += spec.rewardRp;
        hasForecastSignature = false;
        operatingRates.Clear();
        RecalcIncome();
        PlayCoinSfx();
        SpawnConfettiBurst(new Vector2(RootW * 0.5f, RootH * 0.48f), Vermilion, Gold, 18);
        SaveGame();
        RefreshHUD();
        RefreshPendingBadge();
        Toast("授予荣誉星牌【" + spec.plaqueText + "】！获 +¥" + spec.rewardCoins + " · +" + spec.rewardRp + " RP");
        return true;
    }

    int CountClaimablePlaques()
    {
        var list = BuildCharterPlaques();
        int n = 0;
        for (int i = 0; i < list.Length; i++)
            if (!earnedPlaques.Contains(list[i].key) && list[i].checkReady()) n++;
        return n;
    }

    /// <summary>在舰坞中办理闲置老旧星舰「功勋退役」，返还部分购置金与研学点。</summary>
    void PromptRetireVehicle(ResearchVehicle v)
    {
        if (v == null || !researchVehicles.Contains(v)) return;
        if (v.lineId != 0)
        {
            Toast("该星舰正在航线上执勤，请先从航线卸下再办理功勋退役");
            return;
        }
        int idx = Mathf.Clamp(v.node, 0, LocoNames.Length - 1);
        int refundCoins = Mathf.Max(80, Mathf.RoundToInt(LocoPrice(idx) * 0.55f));
        int refundRp = idx <= 3 ? 2 : idx <= 6 ? 3 : 4;
        string body = "将闲置的「" + LocoNames[idx] + "（" + LocoYears[idx] + "）」迁入百年星舰陈列馆，记录功勋并拆解留存测绘档案：\n\n"
            + "· 折价回笼资金：+¥" + refundCoins + "\n"
            + "· 拆舰研学残稿：+" + refundRp + " RP";
        Confirm("办理星舰功勋退役", body, () => {
            if (!researchVehicles.Contains(v) || v.lineId != 0) return;
            researchVehicles.Remove(v);
            coins += refundCoins;
            rp += refundRp;
            retiredVehiclesCount++;
            SyncCulturalCollection(true);
            PlayCoinSfx();
            SaveGame();
            RefreshHUD();
            OpenFleetTab(1);
            Toast("「" + LocoNames[idx] + "」已荣归星舰陈列馆 · +¥" + refundCoins + " · +" + refundRp + " RP");
        });
    }

    /// <summary>扫描当前全网负荷最高的繁忙区段（默认返回前 maxCount 条 >= 75% 负荷区段）。</summary>
    List<CongestedSegmentInfo> CollectTopCongestedSegments(int maxCount = 3, string regionFilter = null)
    {
        var list = new List<CongestedSegmentInfo>();
        if (traffic == null || traffic.lineSegLoad == null) return list;
        for (int li = 0; li < lines.Count; li++)
        {
            var line = lines[li];
            if (line == null || line.st == null || line.st.Count < 2) continue;
            for (int si = 0; si < line.st.Count - 1; si++)
            {
                int a = line.st[si], b = line.st[si + 1];
                string segReg = SegmentRegion(a, b);
                if (!string.IsNullOrEmpty(regionFilter) && segReg != regionFilter) continue;
                float load = SegmentLoadRatio(line, a, b);
                if (load < 0.75f) continue;
                list.Add(new CongestedSegmentInfo {
                    line = line, segIndex = si, stA = a, stB = b, loadRatio = load, region = segReg
                });
            }
        }
        list.Sort((x, y) => y.loadRatio.CompareTo(x.loadRatio));
        if (list.Count > maxCount) list.RemoveRange(maxCount, list.Count - maxCount);
        return list;
    }

    /// <summary>收起全部浮层并在地图居中定位至拥堵区段，随后弹出快捷疏导气泡。</summary>
    void FocusAndBoostSegment(CongestedSegmentInfo seg)
    {
        if (seg == null || seg.line == null || !lines.Contains(seg.line)) return;
        ClosePanel(true);
        ClosePage();
        void FocusCurrent()
        {
            Vector2 mid = (Pos(seg.stA) + Pos(seg.stB)) * 0.5f;
            float z = 1.45f;
            if (mapRect != null)
            {
                mapRect.localScale = Vector3.one * z;
                if (mapWheelZoom != null && mapWheelZoom.viewport != null)
                {
                    Vector2 vpPt = mapWheelZoom.viewport.InverseTransformPoint(mapRect.TransformPoint(mid));
                    Vector2 delta = mapWheelZoom.viewport.rect.center - vpPt;
                    mapRect.anchoredPosition = mapWheelZoom.ClampPosition(mapRect.anchoredPosition + delta, z);
                }
            }
            RefreshAllLines(); RefreshStationDots();
            Vector2 at = mid;
            if (mapRect != null && root != null)
            {
                var rootRt = (RectTransform)root;
                Vector2 rootLocal = rootRt.InverseTransformPoint(mapRect.TransformPoint(mid));
                at = rootLocal - rootRt.rect.min;
            }
            ShowLineBubbles(at, new List<LineHit> { new LineHit { line = seg.line, seg = seg.segIndex, dist = 0f } });
            Toast("已定位至「" + seg.line.name + "」繁忙区段（负荷 " + Mathf.RoundToInt(seg.loadRatio * 100f) + "%）");
        }
        if (!string.IsNullOrEmpty(seg.region) && seg.region != currentRegion && RegionUnlocked(seg.region))
        {
            SwitchRegion(seg.region);
            FocusCurrent();
            return;
        }
        FocusCurrent();
    }

    /// <summary>兼容旧调用入口，统一跳转至全屏「星图鉴」对应收集页签（0->荣誉星牌, 1->票册, 2->节候）。</summary>
    void OpenCultureJournalModal(int tab = 0)
    {
        int target = tab == 0 ? 2 : tab == 1 ? 1 : 3;
        OpenSceneryTab(target);
    }

    /// <summary>星图鉴 · 星际航行纪念票册。</summary>
    void BuildSceneryTicketsTab(Transform page)
    {
        SyncCulturalCollection(false);
        var specs = BuildMemorialTicketSpecs();
        float viewW = W - 56f;
        float heroH = 124f;
        float divH = 36f;
        float itemH = 224f, gap = 20f;
        float totalH = 8f + heroH + 10f + divH + 14f + specs.Length * (itemH + gap) + 20f;

        var content = FleetScroll(page, 48f, PgH - 310f, totalH, scrollKey: "scenery:tickets");
        float topY = content.sizeDelta.y;

        float heroBottom = topY - 8f - heroH;
        var intro = Rect("TicketIntro", content, 0f, heroBottom, viewW, heroH);
        Rounded(intro, Paper2); SoftShadow(intro);
        StampFineRuleFrame(intro.transform, viewW, heroH, 8f, new Color(Gold.r, Gold.g, Gold.b, 0.45f));
        StampCloudCornerSet(intro.transform, viewW, heroH, 44f, 8f, 0.55f);
        StampBotanicalSprig(intro.transform, "orn_kapok_bamboo", viewW - 122f, 4f, 114f, 114f, 0.34f, true, false);
        Txt(intro.transform, "星际航行纪念星票集邮册 · 已集 [" + earnedMemorialTickets.Count + " / " + specs.Length + "] 枚",
            27, Ink, 28f, 64f, viewW - 160f, 40f, TextAnchor.MiddleLeft);
        Txt(intro.transform, "全网每段星轨风尘皆印于卡纸票根；集满 3 枚赠「风雅客潮」全网客流 +2%，集齐 6 枚升级 +4%。",
            21, Indigo, 28f, 16f, viewW - 160f, 42f, TextAnchor.MiddleLeft);

        float divBottom = heroBottom - 10f - divH;
        StampFloralDivider(content, viewW * 0.5f, divBottom, 660f, divH, 0.78f);

        float yCursor = divBottom - 14f;
        for (int i = 0; i < specs.Length; i++)
        {
            var spec = specs[i];
            bool earned = earnedMemorialTickets.Contains(spec.key);
            yCursor -= itemH;
            var card = Rect("TicketCard_" + i, content, 0f, yCursor, viewW, itemH);
            Rounded(card, earned ? new Color(0.992f, 0.972f, 0.918f, 0.99f) : new Color(0.92f, 0.90f, 0.85f, 0.76f));
            SoftShadow(card);
            StampFineRuleFrame(card.transform, viewW, itemH, 8f,
                earned ? new Color(0.72f, 0.28f, 0.22f, 0.28f) : new Color(InkSoft.r, InkSoft.g, InkSoft.b, 0.16f));

            // 右侧两角淡雅云纹边饰
            StampBotanicalSprig(card.transform, "orn_cloud_fret", viewW - 46f, itemH - 46f, 36f, 36f, earned ? 0.42f : 0.18f, true, false);
            StampBotanicalSprig(card.transform, "orn_cloud_fret", viewW - 46f, 10f, 36f, 36f, earned ? 0.42f : 0.18f, true, true);

            // 左侧合金板票边彩条 + 验票齿孔 + 垂直虚线撕票线
            Color stripCol = earned ? new Color(0.74f, 0.27f, 0.20f, 0.92f) : new Color(0.68f, 0.66f, 0.61f, 0.62f);
            Img(Rect("StubLeft", card.transform, 0f, 14f, 18f, itemH - 28f), stripCol, spriteRound).raycastTarget = false;
            for (int nh = 0; nh < 5; nh++)
                Img(Rect("Notch_" + nh, card.transform, -7f, 34f + nh * 38f, 14f, 14f), Paper, spriteCircle).raycastTarget = false;
            for (int d = 0; d < 7; d++)
                Img(Rect("Perf_" + d, card.transform, 206f, 22f + d * 27f, 2f, 16f),
                    new Color(InkSoft.r, InkSoft.g, InkSoft.b, 0.28f)).raycastTarget = false;

            // 朱红星港戳圆印
            var sealDisc = Rect("StationStamp", card.transform, 42f, 48f, 128f, 128f);
            Img(sealDisc, earned ? new Color(0.80f, 0.24f, 0.18f, 0.92f) : new Color(0.68f, 0.66f, 0.60f, 0.45f), spriteCircle).raycastTarget = false;
            sealDisc.transform.localRotation = Quaternion.Euler(0, 0, (i % 2 == 0) ? -8f : 7f);
            var innerDisc = Rect("StampInner", sealDisc.transform, 8f, 8f, 112f, 112f);
            Img(innerDisc, earned ? new Color(0.992f, 0.972f, 0.918f) : new Color(0.92f, 0.90f, 0.85f), spriteCircle).raycastTarget = false;
            Txt(innerDisc.transform, earned ? spec.stampText : "虚位\n待戳", 26, earned ? Vermilion : InkSoft, 4f, 4f, 104f, 104f, TextAnchor.MiddleCenter).raycastTarget = false;

            Txt(card.transform, spec.serial + "  ·  " + spec.regionTag, 22, earned ? Indigo : InkSoft, 228f, 162f, 520f, 32f, TextAnchor.MiddleLeft);
            var stPill = Rect("TicketState", card.transform, viewW - 166f, 158f, 130f, 38f);
            Rounded(stPill, earned ? new Color(0.24f, 0.52f, 0.36f, 0.16f) : new Color(0.84f, 0.82f, 0.76f, 0.72f));
            Txt(stPill.transform, earned ? "已归档 ✓" : "待盖戳", 20, earned ? Matcha : InkSoft, 0f, 0f, 130f, 38f, TextAnchor.MiddleCenter);

            Txt(card.transform, spec.title, 31, earned ? Ink : InkSoft, 228f, 114f, viewW - 260f, 42f, TextAnchor.MiddleLeft);
            Txt(card.transform, "航行区间：" + spec.routeText, 23, earned ? new Color(0.55f, 0.36f, 0.18f) : InkSoft, 228f, 74f, viewW - 260f, 34f, TextAnchor.MiddleLeft);
            var mottoTxt = Txt(card.transform, earned ? "「" + spec.motto + "」" : "尚待达成对应历程后自动盖戳归档",
                21, InkSoft, 228f, 20f, viewW - 260f, 46f, TextAnchor.MiddleLeft);
            mottoTxt.resizeTextForBestFit = true; mottoTxt.resizeTextMinSize = 17; mottoTxt.resizeTextMaxSize = ScaledFontSize(21);
            yCursor -= gap;
        }
    }

    /// <summary>星图鉴 · 荣誉星牌陈列馆。</summary>
    void BuildSceneryPlaquesTab(Transform page)
    {
        var plaques = BuildCharterPlaques();
        float viewW = W - 56f;
        float heroH = 132f;
        float divH = 36f;
        float itemH = 276f, gap = 22f;
        float totalH = 8f + heroH + 10f + divH + 14f + plaques.Length * (itemH + gap) + 20f;

        var content = FleetScroll(page, 48f, PgH - 310f, totalH, scrollKey: "scenery:plaques");
        float topY = content.sizeDelta.y;

        float heroBottom = topY - 8f - heroH;
        var intro = Rect("PlaqueIntro", content, 0f, heroBottom, viewW, heroH);
        Rounded(intro, Paper2); SoftShadow(intro);
        StampFineRuleFrame(intro.transform, viewW, heroH, 8f, new Color(Gold.r, Gold.g, Gold.b, 0.50f));
        StampCloudCornerSet(intro.transform, viewW, heroH, 48f, 8f, 0.62f);
        StampBotanicalSprig(intro.transform, "orn_bauhinia_lotus", viewW - 134f, 4f, 124f, 124f, 0.32f, true, false);
        Txt(intro.transform, "考核阶段大考 · 累计已授 [" + earnedPlaques.Count + " / " + plaques.Length + "] 枚荣誉星牌",
            28, Ink, 30f, 70f, viewW - 170f, 42f, TextAnchor.MiddleLeft);
        Txt(intro.transform, "达成阶段统筹指标后即可领取奖金、研学点与永久经营加成；荣誉头衔将登录于「星际群雄榜」。",
            21, InkSoft, 30f, 18f, viewW - 170f, 44f, TextAnchor.MiddleLeft);

        float divBottom = heroBottom - 10f - divH;
        StampFloralDivider(content, viewW * 0.5f, divBottom, 660f, divH, 0.78f);

        float yCursor = divBottom - 14f;
        for (int i = 0; i < plaques.Length; i++)
        {
            var spec = plaques[i];
            bool earned = earnedPlaques.Contains(spec.key);
            bool ready = !earned && spec.checkReady != null && spec.checkReady();
            yCursor -= itemH;

            var card = Rect("PlaqueCard_" + i, content, 0f, yCursor, viewW, itemH);
            Rounded(card, earned ? Color.Lerp(CardBg, Gold, 0.14f) : ready ? Color.Lerp(CardBg, Matcha, 0.10f) : CardBg);
            SoftShadow(card);
            StampFineRuleFrame(card.transform, viewW, itemH, 9f,
                earned ? new Color(Gold.r, Gold.g, Gold.b, 0.52f) : new Color(InkSoft.r, InkSoft.g, InkSoft.b, 0.22f));
            StampCloudCornerSet(card.transform, viewW, itemH, 46f, 8f, earned ? 0.78f : 0.42f);

            // 金字两行四字竖方荣誉星牌徽记
            var wood = Rect("PlaqueBoard", card.transform, 28f, 40f, 228f, 196f);
            Rounded(wood, earned ? new Color(0.42f, 0.16f, 0.12f, 0.99f) : new Color(0.78f, 0.75f, 0.69f, 0.75f));
            var goldTrim = Rect("GoldTrim", wood.transform, 10f, 10f, 208f, 176f);
            Rounded(goldTrim, earned ? new Color(0.56f, 0.22f, 0.15f, 0.99f) : new Color(0.87f, 0.84f, 0.78f, 0.90f));
            StampCloudCornerSet(wood.transform, 228f, 196f, 34f, 6f, earned ? 0.90f : 0.36f);
            string gridPlaque = spec.plaqueText.Length == 4 ? (spec.plaqueText.Substring(0, 2) + "\n" + spec.plaqueText.Substring(2, 2)) : spec.plaqueText;
            var boardTxt = Txt(goldTrim.transform, gridPlaque, 46, earned ? new Color(0.99f, 0.87f, 0.54f) : InkSoft,
                8f, 44f, 192f, 118f, TextAnchor.MiddleCenter);
            boardTxt.lineSpacing = 1.06f; boardTxt.raycastTarget = false;
            Txt(goldTrim.transform, earned ? "荣誉铭刻" : ready ? "待领认证" : "尚未登记",
                19, earned ? new Color(0.96f, 0.80f, 0.45f) : InkSoft, 8f, 10f, 192f, 28f, TextAnchor.MiddleCenter).raycastTarget = false;

            Txt(card.transform, spec.title, 29, Ink, 276f, 208f, viewW - 316f, 40f, TextAnchor.MiddleLeft);
            var condTxt = Txt(card.transform, "指标：" + spec.conditionText, 22, InkSoft, 276f, 148f, viewW - 316f, 54f, TextAnchor.MiddleLeft);
            condTxt.resizeTextForBestFit = true; condTxt.resizeTextMinSize = 18; condTxt.resizeTextMaxSize = ScaledFontSize(22);
            var rwTxt = Txt(card.transform, spec.rewardHint, 22, earned ? Matcha : Indigo, 276f, 104f, viewW - 316f, 38f, TextAnchor.MiddleLeft);
            FitSingleLine(rwTxt, 18);
            var mottoTxt = Txt(card.transform, "「" + spec.motto + "」", 21, InkSoft, 276f, 22f, viewW - 510f, 68f, TextAnchor.MiddleLeft);
            mottoTxt.resizeTextForBestFit = true; mottoTxt.resizeTextMinSize = 17; mottoTxt.resizeTextMaxSize = ScaledFontSize(21);

            if (ready)
            {
                var captured = spec;
                var claimBtn = Btn(card.transform, "ClaimPlaque", "请颁授匾", 24, viewW - 218f, 26f, 186f, 60f, Vermilion, () => {
                    if (TryClaimCharterPlaque(captured)) OpenSceneryTab(2);
                });
                TInk(claimBtn, Color.white);
            }
            else
            {
                var badge = Rect("PlaqueStatus", card.transform, viewW - 218f, 26f, 186f, 60f);
                Rounded(badge, earned ? new Color(0.24f, 0.52f, 0.36f, 0.16f) : new Color(0.86f, 0.84f, 0.78f, 0.68f));
                Txt(badge.transform, earned ? "已列入荣誉榜 ✓" : "考核进行中", 21, earned ? Matcha : InkSoft, 0f, 0f, 186f, 60f, TextAnchor.MiddleCenter);
            }
            yCursor -= gap;
        }
    }

    /// <summary>星图鉴 · 星域二十四候风物历（两栏手帐陈列）。</summary>
    void BuildScenerySolarTab(Transform page)
    {
        int curIdx = CurrentSolarTermIndex();
        var cur = CurrentSolarTerm();
        float viewW = W - 56f;
        float heroH = 228f;
        float divH = 36f;
        int rows = Mathf.CeilToInt(SolarTerms.Length / 2f);
        float cardW = 500f, gapX = 24f, cardH = 226f, gapY = 18f;
        float totalH = 8f + heroH + 10f + divH + 14f + rows * (cardH + gapY) + 20f;

        var content = FleetScroll(page, 48f, PgH - 310f, totalH, scrollKey: "scenery:solar");
        float topY = content.sizeDelta.y;

        // 顶部当令大卡 + 花枝水彩绘饰
        float heroBottom = topY - 8f - heroH;
        var hero = Rect("SolarIntro", content, 0f, heroBottom, viewW, heroH);
        Rounded(hero, Color.Lerp(CardBg, Matcha, 0.10f));
        SoftShadow(hero);
        StampFineRuleFrame(hero.transform, viewW, heroH, 9f, new Color(Matcha.r, Matcha.g, Matcha.b, 0.36f));
        StampCloudCornerSet(hero.transform, viewW, heroH, 48f, 8f, 0.54f, Matcha);
        string heroFloral = curIdx < 6 ? "orn_kapok_bamboo" : "orn_bauhinia_lotus";
        StampBotanicalSprig(hero.transform, heroFloral, viewW - 200f, 16f, 184f, 184f, 0.52f, true, false);

        var curPill = Rect("HeroTermPill", hero.transform, 28f, 146f, 136f, 54f);
        Rounded(curPill, Matcha);
        Txt(curPill.transform, cur.name, 31, Color.white, 0f, 0f, 136f, 54f, TextAnchor.MiddleCenter);
        var sTitle = Txt(hero.transform, cur.season + " · 距下节气【" + NextSolarTerm().name + "】尚余 " + SolarTermDaysRemaining() + " 日",
            27, Ink, 182f, 148f, viewW - 390f, 50f, TextAnchor.MiddleLeft);
        FitSingleLine(sTitle, 20);
        Txt(hero.transform, "「" + cur.poem + "」", 24, Ink, 28f, 96f, viewW - 230f, 40f, TextAnchor.MiddleLeft);
        Txt(hero.transform, "当期节候物候：" + cur.effectHint, 23, Vermilion, 28f, 54f, viewW - 230f, 36f, TextAnchor.MiddleLeft);
        var noteTxt = Txt(hero.transform, "星域风物历每 3 游戏日一候 · 实时牵引全网客流、名胜、商铺与维保。", 19, InkSoft, 28f, 16f, viewW - 220f, 30f, TextAnchor.MiddleLeft);
        FitSingleLine(noteTxt, 16);

        float divBottom = heroBottom - 10f - divH;
        StampFloralDivider(content, viewW * 0.5f, divBottom, 660f, divH, 0.78f);

        float startY = divBottom - 14f;
        for (int i = 0; i < SolarTerms.Length; i++)
        {
            var term = SolarTerms[i];
            bool isCur = i == curIdx;
            int col = i % 2;
            int row = i / 2;
            float cx = col * (cardW + gapX);
            float cy = startY - (row + 1) * cardH - row * gapY;

            var card = Rect("SolarCard_" + i, content, cx, cy, cardW, cardH);
            Rounded(card, isCur ? new Color(1f, 0.99f, 0.94f, 0.99f) : CardBg);
            if (isCur) SoftShadow(card);
            StampFineRuleFrame(card.transform, cardW, cardH, 8f,
                isCur ? new Color(Matcha.r, Matcha.g, Matcha.b, 0.48f) : new Color(InkSoft.r, InkSoft.g, InkSoft.b, 0.16f));
            string sprKey = i < 6 ? "orn_kapok_bamboo" : "orn_bauhinia_lotus";
            StampBotanicalSprig(card.transform, sprKey, cardW - 118f, 6f, 110f, 110f, isCur ? 0.30f : 0.14f, true, true);
            if (isCur) StampCloudCornerSet(card.transform, cardW, cardH, 34f, 6f, 0.52f, Matcha);

            var badge = Rect("TermPill", card.transform, 18f, cardH - 86f, 126f, 68f);
            Rounded(badge, isCur ? Matcha : new Color(0.88f, 0.85f, 0.78f));
            Txt(badge.transform, term.name, 28, isCur ? Color.white : Ink, 0f, 26f, 126f, 38f, TextAnchor.MiddleCenter);
            Txt(badge.transform, term.season, 16, isCur ? new Color(0.92f, 0.98f, 0.92f) : InkSoft, 0f, 4f, 126f, 22f, TextAnchor.MiddleCenter);

            Txt(card.transform, isCur ? "当前生效 · 余 " + SolarTermDaysRemaining() + " 日" : "第 " + (i * 3 + 1) + "~" + (i * 3 + 3) + " 日候",
                20, isCur ? Matcha : InkSoft, 154f, cardH - 68f, cardW - 174f, 36f, TextAnchor.MiddleRight);

            var poemTxt = Txt(card.transform, term.poem, 22, Ink, 20f, 94f, cardW - 44f, 40f, TextAnchor.MiddleLeft);
            poemTxt.resizeTextForBestFit = true; poemTxt.resizeTextMinSize = 18; poemTxt.resizeTextMaxSize = ScaledFontSize(22);

            var fxTxt = Txt(card.transform, "物候：" + term.effectHint, 21, isCur ? Vermilion : Indigo, 20f, 44f, cardW - 122f, 44f, TextAnchor.MiddleLeft);
            fxTxt.resizeTextForBestFit = true; fxTxt.resizeTextMinSize = 17; fxTxt.resizeTextMaxSize = ScaledFontSize(21);

            Txt(card.transform, "十二节候第 " + (i + 1) + " 候", 18, InkSoft, 20f, 14f, 240f, 24f, TextAnchor.MiddleLeft);
        }
    }
}
