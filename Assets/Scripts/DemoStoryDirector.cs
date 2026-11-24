using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class DemoBootstrap
{
    // ---------- M3: 剧情导演、章节目标与系统渐进式解锁 ----------
    public enum GameFeature
    {
        LinesPage,
        DailyTasks,
        FleetPage,
        TechPage,
        SceneryAndCommercial,
        CompanyCore,
        CompanyFinanceAndBoard,
        Leaderboard
    }

    int storyStage = 0; // 0: 星港初笛, 1: 跃迁与航历, 2: 月之亮面拓荒, 3: 月背深空连接, 4: 火星跨星域贯通, 5: 全章圆满
    bool tutorialSkipped = false;
    GameObject storyObjectiveBannerGo;
    Text storyObjectiveText;
    float storyCheckTimer;

    sealed class StoryChapterSpec
    {
        public int stage;
        public string key, chapterTag, speaker, title, body, objective;
    }

    static readonly StoryChapterSpec[] StoryChapters = {
        new StoryChapterSpec {
            stage = 0, key = "ch0_harbor_whistle",
            chapterTag = "序章 · 星港初笛", speaker = "星航署嘱托",
            title = "第一张摊开的星图",
            body = "星尘还未落定，你把月之亮面星港群的手绘星图摊在全息桌上。\n这片星域每天都在跃迁，却还缺一条真正连贯的星际航线。\n长按地图上任意两座相邻星港相连，试着点亮我们的首班星舰吧！",
            objective = "目标：铺设首条航线或接入 2 座星港（解锁「星舰」与「科技」）"
        },
        new StoryChapterSpec {
            stage = 1, key = "ch1_steam_and_time",
            chapterTag = "第一章 · 跃迁与航历", speaker = "航务督办 · 黄工头",
            title = "核热点火与星航典籍",
            body = "星轨刚一接通，星港里就挤满了拎着公文包与补给袋的早班旅客。\n黄工头抹了把汗笑道：『单靠人工航历撑不住客流！去看看「科技树」与「星舰舰坞」，研习新型飞车、调配四时段航班，这航线才能真正赚大钱！』",
            objective = "目标：连通 4 座星港或解锁 1 项科技（解锁「名胜图鉴」与「港前物业」）"
        },
        new StoryChapterSpec {
            stage = 2, key = "ch2_harbor_life",
            chapterTag = "第二章 · 月之亮面拓荒", speaker = "文史编辑 · 陈素心",
            title = "舷窗外的人间四时",
            body = "一位女摄影师拿着手帐本走进中心星港，里面夹着刚刚印出的景点明信片。\n『每当航线路过名胜港，旅人总会在星港多停留十分钟——点开左栏「星图」留影打卡，再点星港「详情」添置茶室书局吧。』",
            objective = "目标：连通 6 座星港（筹备总社挂牌，解锁「公司经营」与「人才招募」）"
        },
        new StoryChapterSpec {
            stage = 3, key = "ch3_company_founded",
            chapterTag = "第三章 · 月背深空连接", speaker = "执行总长 · 林先生",
            title = "星轨跨越月背深空",
            body = "今日红绸落下，你的星际航运公司正式挂牌！\n底栏「公司」总台与「企业治理树」现已全面启用：\n任命高管、猎聘才俊并派驻大港或骨干航线，让专业团队替你撑起整张深空星轨网。\n月背深空连接航线已贯通，星轨向更远处延伸。",
            objective = "目标：扩展运营并冲击日利润 ¥600（开拓月背深空与全套「金融招投标」）"
        },
        new StoryChapterSpec {
            stage = 4, key = "ch4_greater_bay",
            chapterTag = "第四章 · 火星跨星域贯通", speaker = "星域联席特使",
            title = "跨越星域，抵达火星",
            body = "月之亮面的灯火已连成诗行，月背的深空航迹也早已贯通，更北方的火星名城正发出邀约。\n『债券融资、市政招标、跨区接驳与排行榜』现已全面解禁——用星轨把整片星域连成一个温暖的圆吧！火星跨星域航线全线贯通！",
            objective = "自由经营阶段：连结三地门户，建设你的百年星际航运！"
        }
    };

    bool IsFeatureUnlocked(GameFeature feature)
    {
        if (tutorialSkipped || storyStage >= 5) return true;
        switch (feature)
        {
            case GameFeature.LinesPage:
            case GameFeature.DailyTasks:
                return true;
            case GameFeature.FleetPage:
            case GameFeature.TechPage:
                return storyStage >= 1 || lines.Count >= 1 || DailyConnectedCount() >= 2;
            case GameFeature.SceneryAndCommercial:
                return storyStage >= 2 || DailyConnectedCount() >= 4 || techDone.Count >= 1;
            case GameFeature.CompanyCore:
                return storyStage >= 3 || DailyConnectedCount() >= 6 || day >= 4;
            case GameFeature.CompanyFinanceAndBoard:
            case GameFeature.Leaderboard:
                return storyStage >= 4 || macauUnlocked || DailyConnectedCount() >= 9 || companyTechDone.Count >= 2;
            default:
                return true;
        }
    }

    string FeatureLockHint(GameFeature feature)
    {
        switch (feature)
        {
            case GameFeature.FleetPage:
            case GameFeature.TechPage:
                return "完成「序章 · 开通首条航线（连通 2 港）」后解锁！";
            case GameFeature.SceneryAndCommercial:
                return "推进至「第二章 · 月之亮面拓荒（连通 4 港或研发 1 项科技）」后解锁！";
            case GameFeature.CompanyCore:
                return "推进至「第三章 · 月背深空连接（连通 6 座星港）」后解锁！";
            case GameFeature.CompanyFinanceAndBoard:
            case GameFeature.Leaderboard:
                return "推进至「第四章 · 火星跨星域贯通（连通 9 港或开放月背）」后解锁！";
            default:
                return "继续扩展星轨网后解锁";
        }
    }

    void SyncStoryProgression(bool allowTriggerPopups)
    {
        // 兼容既有/高级测试存档（例如第 79 天存档）：自动平滑解禁对应章节与所有功能，不再强行弹老旧新手教学
        if (!tutorialSkipped && (day >= 5 || lines.Count >= 3 || macauUnlocked || guangdongUnlocked || techDone.Count >= 4))
        {
            storyStage = 5;
            tutorialSkipped = true;
            foreach (var ch in StoryChapters) storySeen.Add(ch.key);
            RefreshFeatureGateVisuals();
            UpdateStoryObjectiveBanner();
            if (allowTriggerPopups) SaveGame();
            return;
        }

        if (tutorialSkipped || storyStage >= 5)
        {
            RefreshFeatureGateVisuals();
            UpdateStoryObjectiveBanner();
            return;
        }

        int stageBefore = storyStage;
        // 新手期按里程碑流转推进
        int conn = DailyConnectedCount();
        int nextTargetStage = storyStage;
        if (storyStage < 1 && (lines.Count >= 1 || conn >= 2)) nextTargetStage = 1;
        if (nextTargetStage >= 1 && storyStage < 2 && (conn >= 4 || techDone.Count >= 1)) nextTargetStage = 2;
        if (nextTargetStage >= 2 && storyStage < 3 && (conn >= 6 || day >= 4)) nextTargetStage = 3;
        if (nextTargetStage >= 3 && storyStage < 4 && (macauUnlocked || conn >= 9 || companyTechDone.Count >= 2)) nextTargetStage = 4;

        if (!allowTriggerPopups)
        {
            if (nextTargetStage > storyStage) storyStage = nextTargetStage;
            RefreshFeatureGateVisuals();
            UpdateStoryObjectiveBanner();
            return;
        }

        if (storyStage == 0 && !storySeen.Contains(StoryChapters[0].key))
        {
            QueueChapterStory(StoryChapters[0]);
        }

        while (storyStage < nextTargetStage)
        {
            storyStage++;
            if (storyStage < StoryChapters.Length)
                QueueChapterStory(StoryChapters[storyStage]);
        }

        RefreshFeatureGateVisuals();
        UpdateStoryObjectiveBanner();
        if (storyStage != stageBefore) SaveGame();
    }

    void TickStoryProgressionTimer(float dt)
    {
        storyCheckTimer += dt;
        if (storyCheckTimer < 1.0f) return;
        storyCheckTimer = 0f;
        if (storyStage < 5 && !tutorialSkipped)
            SyncStoryProgression(true);
    }

    void QueueChapterStory(StoryChapterSpec ch)
    {
        if (ch == null || !storySeen.Add(ch.key)) return;
        storyQueue.Enqueue(new StoryEvent {
            chapterTag = ch.chapterTag,
            speaker = ch.speaker,
            title = ch.title,
            text = ch.body,
            objectiveHint = ch.objective,
            allowSkip = storyStage < 4
        });
        SaveGame();
    }

    void SkipTutorialFlow()
    {
        tutorialSkipped = true;
        storyStage = 5;
        foreach (var ch in StoryChapters) storySeen.Add(ch.key);
        storyQueue.Clear();
        if (storyCard) { Destroy(storyCard); storyCard = null; }
        RefreshFeatureGateVisuals();
        UpdateStoryObjectiveBanner();
        SaveGame();
        Toast("已开启自由经营模式，全游戏系统现已开放！");
    }

    void BuildStoryObjectiveBanner()
    {
        if (!root) return;
        if (storyObjectiveBannerGo) Destroy(storyObjectiveBannerGo);
        GetSafeMarginsInRoot(out float safeL, out float safeR, out _, out _);
        float safeW = Mathf.Max(320f, RootW - safeL - safeR);
        float barY = topBarGo ? topBarGo.GetComponent<RectTransform>().anchoredPosition.y : (RootH - 310f);
        storyObjectiveBannerGo = Rect("StoryObjectiveBanner", root, safeL + (safeW - 680f) * 0.5f, barY - 76f, 680f, 64f);
        Rounded(storyObjectiveBannerGo, new Color(CardBg.r, CardBg.g, CardBg.b, 0.94f));
        SoftShadow(storyObjectiveBannerGo);
        Img(Rect("TagBar", storyObjectiveBannerGo.transform, 0f, 12f, 7f, 40f), Gold, spriteRound).raycastTarget = false;
        storyObjectiveText = Txt(storyObjectiveBannerGo.transform, "", 24, Ink, 22f, 4f, 636f, 56f, TextAnchor.MiddleLeft);
        storyObjectiveText.resizeTextForBestFit = true; storyObjectiveText.resizeTextMinSize = 17; storyObjectiveText.resizeTextMaxSize = ScaledFontSize(24);
        storyObjectiveText.raycastTarget = false;
        var btn = storyObjectiveBannerGo.AddComponent<Button>(); btn.targetGraphic = storyObjectiveBannerGo.GetComponent<Image>();
        btn.onClick.AddListener(OpenStoryArchiveModal);
        UpdateStoryObjectiveBanner();
    }

    void UpdateStoryObjectiveBanner()
    {
        if (!storyObjectiveBannerGo || !storyObjectiveText) return;
        bool show = !tutorialSkipped && storyStage < 5;
        storyObjectiveBannerGo.SetActive(show);
        if (!show) return;
        var cur = StoryChapters[Mathf.Clamp(storyStage, 0, StoryChapters.Length - 1)];
        storyObjectiveText.text = $"{cur.chapterTag}  ·  {cur.objective}";
    }

    void RefreshFeatureGateVisuals()
    {
        if (dock)
        {
            GameFeature[] tabFeats = { GameFeature.LinesPage, GameFeature.FleetPage, GameFeature.CompanyCore, GameFeature.TechPage };
            string[] tabBaseNames = { "航线", "星舰", "公司", "科技" };
            for (int i = 0; i < 4; i++)
            {
                var tabTf = dock.transform.Find("Tab" + i);
                if (!tabTf) continue;
                bool open = IsFeatureUnlocked(tabFeats[i]);
                var texts = tabTf.GetComponentsInChildren<Text>(true);
                if (texts.Length > 0) texts[0].text = open ? tabBaseNames[i] : tabBaseNames[i] + " 🔒";
                if (texts.Length > 0) texts[0].color = open ? Ink : new Color(InkSoft.r, InkSoft.g, InkSoft.b, 0.65f);
            }
        }
        if (leftRailGos.Count >= 3)
        {
            bool sceneryOpen = IsFeatureUnlocked(GameFeature.SceneryAndCommercial);
            bool rankOpen = IsFeatureUnlocked(GameFeature.Leaderboard);
            SetLeftRailLock(leftRailGos[1], "星图", sceneryOpen);
            SetLeftRailLock(leftRailGos[2], "排行", rankOpen);
        }
    }

    void SetLeftRailLock(GameObject go, string label, bool unlocked)
    {
        if (!go) return;
        var texts = go.GetComponentsInChildren<Text>(true);
        if (texts.Length > 0)
        {
            texts[0].text = unlocked ? label : label + "🔒";
            texts[0].color = unlocked ? Ink : new Color(InkSoft.r, InkSoft.g, InkSoft.b, 0.65f);
        }
    }

    void OpenStoryArchiveModal()
    {
        var pg = OpenPage("星航手帐 · 剧情档案", "重温星际拓荒关键篇章与各阶段引导目标，亦可随时自由切换剧情引导开关。", "PageArt/album_watercolor");
        float totalH = StoryChapters.Length * 294f + 160f;
        var content = FleetScroll(pg.transform, 36f, PgH - 250f, totalH, scrollKey: "story:archive");
        float topY = content.sizeDelta.y;
        float cardW = W - 72f;

        for (int i = 0; i < StoryChapters.Length; i++)
        {
            var ch = StoryChapters[i];
            bool unlocked = tutorialSkipped || storyStage >= ch.stage || storySeen.Contains(ch.key);
            float y = topY - 12f - (i + 1) * 286f;
            var card = Rect("StoryArchive_" + i, content, 4f, y, cardW - 8f, 268f);
            Rounded(card, unlocked ? CardBg : Color.Lerp(CardBg, Ink, 0.05f)); SoftShadow(card);
            Img(Rect("Accent", card.transform, 0f, 28f, 7f, 212f), unlocked ? Gold : InkSoft, spriteRound).raycastTarget = false;
            Txt(card.transform, $"{ch.chapterTag}  ·  {ch.speaker}", 24, unlocked ? Matcha : InkSoft, 28f, 212f, 640f, 36f, TextAnchor.MiddleLeft);
            Txt(card.transform, unlocked ? ch.title : "？？？ （随星轨网扩展后开启）", 31, unlocked ? Ink : InkSoft, 28f, 160f, 680f, 46f, TextAnchor.MiddleLeft);
            Txt(card.transform, unlocked ? ch.body : ch.objective, 24, InkSoft, 28f, 18f, cardW - 252f, 134f, TextAnchor.UpperLeft);

            if (unlocked)
            {
                var replayBtn = Btn(card.transform, "Replay_" + i, "重温此章", 24, cardW - 216f, 92f, 186f, 84f, Indigo, () => {
                    ClosePage();
                    storyQueue.Enqueue(new StoryEvent {
                        chapterTag = ch.chapterTag,
                        speaker = ch.speaker,
                        title = ch.title,
                        text = ch.body,
                        objectiveHint = ch.objective,
                        allowSkip = false
                    });
                });
                TInk(replayBtn, Color.white);
            }
        }
    }
}
