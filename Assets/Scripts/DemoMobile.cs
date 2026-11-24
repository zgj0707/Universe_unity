using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;

public static class Haptic
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] static extern void VibrateJS(int ms);
#endif
    public static void Vibrate(int ms)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        try { VibrateJS(ms); } catch { }
#elif UNITY_ANDROID || UNITY_IOS
        Handheld.Vibrate();
#else
        _ = ms;
#endif
    }
}

public partial class DemoBootstrap
{
    // ---------- time / input / feedback ----------
    const float GameDaySeconds = 360f;
    float dayClock = 120f, researchClock; int day = 1; int timeSpeed = 1; int economicHour = -1;
    Text timeTxt, speedTxt;
    int pressIdx = -1; float pressTimer; bool pressing; Vector2 pressStart;
    AudioSource sfx, music; AudioClip toneLo, toneHi;
    bool musicEnabled = true;
    int qualityTier = 1;
    int selectedFrameRate = 120;
    static readonly int[] FrameRateOptions = { 30, 60, 120, -1 };
    void ApplyFrameRate()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = selectedFrameRate;
    }
    void SetFrameRate(int fps)
    {
        if (fps != 30 && fps != 60 && fps != 120 && fps != -1) return;
        selectedFrameRate = fps;
        PlayerPrefs.SetInt("target_fps", fps); PlayerPrefs.Save();
        ApplyFrameRate();
    }
    void ApplyVisualQuality()
    {
        ApplyFrameRate();
        QualitySettings.globalTextureMipmapLimit = qualityTier <= 0 ? 1 : 0;
        // 2D Screen Space Overlay 与 SDF 精灵无需硬件 MSAA，关闭可释放高刷性能与显存带宽。
        QualitySettings.antiAliasing = 0;
        bool enableShadow = qualityTier > 0;
        for (int i = managedShadows.Count - 1; i >= 0; i--)
        {
            var sh = managedShadows[i];
            if (!sh) { managedShadows.RemoveAt(i); continue; }
            sh.enabled = enableShadow;
        }
        SyncRegionMapTextureMemory();
        if (qualityTier <= 0) RequestResourceTrim(false);
    }
    void SetVisualQuality(int tier)
    {
        qualityTier = Mathf.Clamp(tier, 0, 2);
        PlayerPrefs.SetInt("visual_quality", qualityTier); PlayerPrefs.Save();
        ApplyVisualQuality();
    }
    int fontSizeTier = 1; // 0: 紧凑, 1: 标准, 2: 偏大, 3: 特大
    static readonly string[] FontSizeTierNames = { "紧凑", "标准", "偏大", "特大" };
    static readonly float[] FontSizeScales = { 1.04f, 1.16f, 1.32f, 1.48f };
    float UiFontScale => FontSizeScales[Mathf.Clamp(fontSizeTier, 0, FontSizeScales.Length - 1)];
    int ScaledFontSize(int baseSize, float maxScale = 1.48f)
        => Mathf.Max(18, Mathf.RoundToInt(baseSize * Mathf.Min(UiFontScale, maxScale)));
    void SetFontSizeTier(int tier)
    {
        fontSizeTier = Mathf.Clamp(tier, 0, FontSizeScales.Length - 1);
        PlayerPrefs.SetInt("font_size_tier", fontSizeTier); PlayerPrefs.Save();
        if (dragCostBadge) { Destroy(dragCostBadge); dragCostBadge = null; BuildDragCostBadge(); }
        if (zoomNoticeTxt) zoomNoticeTxt.fontSize = ScaledFontSize(25);
        if (root) RebuildChromeUI();
        UpdateStationLabelSizes(mapRect ? mapRect.localScale.x : 1f);
        UpdateRegionalMarkers(true);
    }
    Image dragTip; int dragFrom = -1, hoverStation = -1;
    GameObject dock; List<Image> stationRings = new List<Image>();
    int pulseIdx = -1; float pulseTimer;

    class Floater { public Text txt; public RectTransform rt; public float age; }
    readonly List<Floater> floaters = new List<Floater>();

    void InitMobile()
    {
        InitAudioEngine();
        RefreshStationDots();
        UpdateTimeText();
    }
    void OpenAudioSettings()
    {
        settingsTab = Mathf.Clamp(settingsTab, 0, 2);
        const float panelW = 940f, panelH = 1140f;
        var popup = OpenPopup("馆藏手记 · 系统设置", panelH, panelW, "settings");
        JournalTape(popup.transform, panelW * 0.5f - 82f, panelH - 18f, 164f);

        string[] tabLabels = { "画面风格", "声音手感", "档案引导" };
        const float pad = 40f;
        const float tabGap = 14f;
        float tabW = (panelW - pad * 2f - tabGap * (tabLabels.Length - 1)) / tabLabels.Length;
        for (int t = 0; t < tabLabels.Length; t++)
        {
            int idx = t;
            bool active = settingsTab == idx;
            var tb = Btn(popup.transform, "SetTab_" + idx, tabLabels[idx], 25, pad + t * (tabW + tabGap), 978f, tabW, 68f,
                active ? Indigo : Paper2, () => { settingsTab = idx; OpenAudioSettings(); });
            TInk(tb, active ? Color.white : Ink);
            if (!active) SoftShadow(tb.gameObject);
        }

        if (settingsTab == 0) BuildSettingsVisualTab(popup.transform);
        else if (settingsTab == 1) BuildSettingsAudioTab(popup.transform);
        else BuildSettingsArchiveTab(popup.transform);
    }

    void BuildSettingsVisualTab(Transform parent)
    {
        const float pad = 40f, innerW = 860f;
        Txt(parent, "画面质量（动效与粒子细致度）", 27, InkSoft, pad, 906f, innerW, 40f, TextAnchor.MiddleLeft);
        string[] levels = { "低 · 省电", "中 · 均衡", "高 · 华彩" };
        float qW = (innerW - 24f) / 3f;
        for (int i = 0; i < levels.Length; i++)
        {
            int selected = i;
            bool active = qualityTier == i;
            var quality = Btn(parent, "Quality" + i, levels[i] + (active ? " ✓" : ""), 25,
                pad + i * (qW + 12f), 816f, qW, 80f, active ? Indigo : Paper2, () => { SetVisualQuality(selected); OpenAudioSettings(); });
            TInk(quality, active ? Color.white : Ink);
        }

        Txt(parent, "帧率上限", 27, InkSoft, pad, 738f, innerW, 40f, TextAnchor.MiddleLeft);
        float fpsW = (innerW - 12f * Mathf.Max(0, FrameRateOptions.Length - 1)) / Mathf.Max(1, FrameRateOptions.Length);
        for (int i = 0; i < FrameRateOptions.Length; i++)
        {
            int fps = FrameRateOptions[i];
            bool active = selectedFrameRate == fps;
            string fpsLabel = (fps < 0 ? "不限" : fps + " FPS") + (active ? " ✓" : "");
            var choice = Btn(parent, "FrameRate" + fps, fpsLabel, 24,
                pad + i * (fpsW + 12f), 648f, fpsW, 80f, active ? Indigo : Paper2, () => { SetFrameRate(fps); OpenAudioSettings(); });
            TInk(choice, active ? Color.white : Ink);
        }
        Txt(parent, "高画质启用全量粒子、双环涟漪与 Q 弹动效；省电档降采样省 75% 贴图显存", 22, InkSoft, pad, 594f, innerW, 40f, TextAnchor.MiddleLeft);

        Txt(parent, "手帐字体字号（当前 " + Mathf.RoundToInt(UiFontScale * 100f) + "%）", 27, InkSoft, pad, 514f, innerW, 40f, TextAnchor.MiddleLeft);
        float fontGap = 12f;
        float fontBtnW = (innerW - fontGap * Mathf.Max(0, FontSizeTierNames.Length - 1)) / Mathf.Max(1, FontSizeTierNames.Length);
        for (int i = 0; i < FontSizeTierNames.Length; i++)
        {
            int selectedFont = i;
            bool activeFont = fontSizeTier == i;
            var fontBtn = Btn(parent, "FontSize" + i, FontSizeTierNames[i] + (activeFont ? " ✓" : ""), 25,
                pad + i * (fontBtnW + fontGap), 422f, fontBtnW, 82f, activeFont ? Indigo : Paper2, () => {
                    SetFontSizeTier(selectedFont);
                    OpenAudioSettings();
                    Toast("字体大小已切换为「" + FontSizeTierNames[selectedFont] + "」");
                });
            TInk(fontBtn, activeFont ? Color.white : Ink);
        }

        Txt(parent, "纸本手帐主题", 27, InkSoft, pad, 336f, 420f, 44f, TextAnchor.MiddleLeft);
        var moreTheme = Btn(parent, "ThemeShopLink", "主题馆藏与试用 ›", 24, pad + innerW - 308f, 330f, 308f, 56f, Paper2, () => { ClosePanel(true); OpenThemeShopScreen(); });
        TInk(moreTheme, Indigo);

        float themeGap = 12f;
        float themeBtnW = (innerW - themeGap * Mathf.Max(0, Themes.Length - 1)) / Mathf.Max(1, Themes.Length);
        for (int i = 0; i < Themes.Length; i++)
        {
            int themeIdx = i;
            bool avail = IsThemeAvailable(themeIdx);
            bool active = currentTheme == themeIdx && avail && previewRestoreTheme < 0;
            string label = Themes[themeIdx].shortName + (active ? " ✓" : avail ? "" : " · 锁");
            var tb = Btn(parent, "ThemeBtn" + i, label, 23,
                pad + i * (themeBtnW + themeGap), 210f, themeBtnW, 102f, active ? Indigo : avail ? Paper2 : new Color(Paper2.r, Paper2.g, Paper2.b, 0.55f), () => {
                    if (IsThemeAvailable(themeIdx))
                    {
                        ApplyTheme(themeIdx); SaveGame(); OpenAudioSettings();
                        Toast("已切换为「" + Themes[themeIdx].name + "」");
                    }
                    else { ClosePanel(true); OpenThemeShopScreen(); }
                });
            TInk(tb, active ? Color.white : Ink);
        }

        var doneBtn = Btn(parent, "SettingsOk", "收起手记", 26, pad, 40f, innerW, 84f, Matcha, ClosePanel);
        TInk(doneBtn, Color.white);
    }

    void BuildSettingsAudioTab(Transform parent)
    {
        const float pad = 40f, innerW = 860f, cardPad = 28f, cardInnerW = 804f;
        var bgmCard = Rect("BgmCard", parent, pad, 610f, innerW, 336f);
        Rounded(bgmCard, Color.Lerp(CardBg, Ink, 0.04f));
        Txt(bgmCard.transform, "原声配乐与播放模式（BGM）", 28, Ink, cardPad, 272f, 540f, 44f, TextAnchor.MiddleLeft);
        var bgmToggle = Btn(bgmCard.transform, "BgmSwitch", musicEnabled ? "已开启 ✓" : "已静音", 25, innerW - cardPad - 210f, 262f, 210f, 60f,
            musicEnabled ? Matcha : Paper2, () => { SetBgmEnabled(!musicEnabled); SaveGame(); OpenAudioSettings(); });
        TInk(bgmToggle, musicEnabled ? Color.white : Ink);
        var nowPlaying = Txt(bgmCard.transform, "正在播放：" + CurrentBgmDisplayTitle(), 24, Indigo, cardPad, 214f, cardInnerW, 40f, TextAnchor.MiddleLeft);
        FitSingleLine(nowPlaying, 24);
        string[] modeNames = { "场景联动", "经典长曲", "仅区域曲" };
        const float modeGap = 14f;
        float modeBtnW = (cardInnerW - modeGap * (modeNames.Length - 1)) / modeNames.Length;
        for (int m = 0; m < modeNames.Length; m++)
        {
            int modeVal = m;
            bool active = bgmPlayMode == modeVal;
            var mb = Btn(bgmCard.transform, "BgmMode_" + m, active ? modeNames[m] + " ✓" : modeNames[m], 24,
                cardPad + m * (modeBtnW + modeGap), 140f, modeBtnW, 64f, active ? Indigo : Paper2, () => { SetBgmPlayMode(modeVal); OpenAudioSettings(); });
            TInk(mb, active ? Color.white : Ink);
        }
        string modeDesc = bgmPlayMode == 1
            ? "始终播放经典长曲《Between the Bow and Breath》"
            : bgmPlayMode == 2
                ? "始终随「月之亮面 / 月背 / 火星」切换区域配乐"
                : "地图播区域曲，进入内页自动淡入经典长曲（记忆进度）";
        var modeHint = Txt(bgmCard.transform, modeDesc, 23, InkSoft, cardPad, 92f, cardInnerW, 38f, TextAnchor.MiddleLeft);
        FitSingleLine(modeHint, 23);
        BuildVolumeSlider(bgmCard.transform, "BgmVolSlider", cardPad, 26f, cardInnerW, bgmVolume, Matcha, SetBgmVolume);

        var sfxCard = Rect("SfxCard", parent, pad, 330f, innerW, 256f);
        Rounded(sfxCard, Color.Lerp(CardBg, Ink, 0.04f));
        Txt(sfxCard.transform, "触点与鸣笛音效（SFX）", 28, Ink, cardPad, 194f, 540f, 44f, TextAnchor.MiddleLeft);
        var sfxToggle = Btn(sfxCard.transform, "SfxSwitch", sfxEnabled ? "已开启 ✓" : "已关闭", 25, innerW - cardPad - 210f, 184f, 210f, 60f,
            sfxEnabled ? Indigo : Paper2, () => { SetSfxEnabled(!sfxEnabled); if (sfxEnabled) PlayTechSfx(); OpenAudioSettings(); });
        TInk(sfxToggle, sfxEnabled ? Color.white : Ink);
        Txt(sfxCard.transform, "包含：按压吸附、航线开通鸣笛、金币营收、研发共鸣", 24, InkSoft, cardPad, 128f, cardInnerW, 42f, TextAnchor.MiddleLeft);
        BuildVolumeSlider(sfxCard.transform, "SfxVolSlider", cardPad, 54f, cardInnerW - 190f, sfxVolume, Indigo, SetSfxVolume);
        var testSfxBtn = Btn(sfxCard.transform, "TestSfx", "试听", 24, innerW - cardPad - 166f, 48f, 166f, 60f, Gold, () => PlayBuildSfx());
        TInk(testSfxBtn, Color.white);

        var hapCard = Rect("HapticCard", parent, pad, 152f, innerW, 154f);
        Rounded(hapCard, Color.Lerp(CardBg, Ink, 0.04f));
        Txt(hapCard.transform, "星港吸附轻震动反馈", 27, Ink, cardPad, 86f, 560f, 42f, TextAnchor.MiddleLeft);
        Txt(hapCard.transform, "拖线吸附与完成里程碑时触发轻微震动提示", 23, InkSoft, cardPad, 26f, 560f, 44f, TextAnchor.MiddleLeft);
        var hapBtn = Btn(hapCard.transform, "HapSwitch", hapticEnabled ? "开启 ✓" : "关闭", 25, innerW - cardPad - 210f, 40f, 210f, 74f,
            hapticEnabled ? Matcha : Paper2, () => { SetHapticEnabled(!hapticEnabled); TriggerHaptic(18); OpenAudioSettings(); });
        TInk(hapBtn, hapticEnabled ? Color.white : Ink);

        var doneBtn = Btn(parent, "SettingsOk", "完成设置", 26, pad, 40f, innerW, 84f, Matcha, ClosePanel);
        TInk(doneBtn, Color.white);
    }

    void BuildSettingsArchiveTab(Transform parent)
    {
        const float pad = 40f, innerW = 860f, cardPad = 28f, cardInnerW = 804f;
        var summaryCard = Rect("ArchiveSummaryCard", parent, pad, 696f, innerW, 250f);
        Rounded(summaryCard, Color.Lerp(CardBg, Indigo, 0.05f));
        Txt(summaryCard.transform, $"{companyName} · 营运志档案", 29, Ink, cardPad, 188f, cardInnerW, 44f, TextAnchor.MiddleLeft);
        Txt(summaryCard.transform, $"当前第 {day} 日 · 运营 {lines.Count} 条航线 · 已连 {DailyConnectedCount()} 港\n工程科技 {EraTechDoneCount(0)}/{EraTechNodeCount(0)} 项 · 企业治理 {CoTechEraDoneCount(0)}/{CoTechEraNodeCount(0)} 项 · 人才 {talents.Count} 名", 24, InkSoft, cardPad, 90f, cardInnerW, 86f, TextAnchor.UpperLeft);
        Txt(summaryCard.transform, "游戏每 5 秒及关键操作时自动保存至本地档案", 23, Matcha, cardPad, 24f, cardInnerW, 40f, TextAnchor.MiddleLeft);

        var storyCardBox = Rect("StoryControlCard", parent, pad, 430f, innerW, 242f);
        Rounded(storyCardBox, Color.Lerp(CardBg, Gold, 0.07f));
        Txt(storyCardBox.transform, "章节剧情与星际航行引导", 28, Ink, cardPad, 180f, cardInnerW, 44f, TextAnchor.MiddleLeft);
        string guideStatus = tutorialSkipped ? "自由经营模式（全系统已开放）" : $"剧情进行中：第 {Mathf.Min(storyStage + 1, 5)}/5 章";
        Txt(storyCardBox.transform, guideStatus, 24, Indigo, cardPad, 128f, cardInnerW, 40f, TextAnchor.MiddleLeft);
        float halfBtnW = (cardInnerW - 24f) * 0.5f;
        var archiveBtn = Btn(storyCardBox.transform, "OpenStoryArchive", "剧情篇章回放", 25, cardPad, 26f, halfBtnW, 80f, Indigo, () => { ClosePanel(true); OpenStoryArchiveModal(); });
        TInk(archiveBtn, Color.white);
        var skipBtn = Btn(storyCardBox.transform, "ToggleGuideBtn", tutorialSkipped ? "已解锁全系统 ✓" : "跳过引导直通自由", 24, cardPad + halfBtnW + 24f, 26f, halfBtnW, 80f,
            tutorialSkipped ? Paper2 : Gold, () => {
                if (!tutorialSkipped)
                {
                    Confirm("跳过星际航行引导", "将立即解锁「星舰、名胜、公司、科技树」全部游戏系统并进入自由经营模式，剧情仍可在档案中回放。是否确认？", SkipTutorialFlow);
                }
                else Toast("当前已开放全部系统，可随时在「剧情篇章回放」重温故事");
            });
        TInk(skipBtn, tutorialSkipped ? InkSoft : Color.white);

        float curY = 40f;
        var doneBtn = Btn(parent, "SettingsOk", "收起设置", 26, pad, curY, innerW, 80f, Paper2, ClosePanel);
        TInk(doneBtn, Ink);
        curY += 98f;

#if UNITY_EDITOR
        bool syncOn = DevTestSaveSyncEnabled;
        var syncBtn = Btn(parent, "DevTestSaveSyncToggle",
            syncOn ? "[Editor开发] 持续写回仓库测试档：开" : "[Editor开发] 持续写回仓库测试档：关",
            24, pad, curY, innerW, 74f, syncOn ? Indigo : Paper2, () => {
                bool next = !DevTestSaveSyncEnabled;
                DevTestSaveSyncEnabled = next;
                OpenAudioSettings();
                Toast(next ? "已开启同步：将持续写回仓库测试档" : "已关闭同步：仅读写本机存档");
            });
        TInk(syncBtn, syncOn ? Color.white : Ink);
        curY += 90f;
#endif

        if (HasBundledTestSave())
        {
            var testBtn = Btn(parent, "LoadTestSave", "载入测试存档（" + DescribeBundledTestSave() + "）", 24, pad, curY, innerW, 80f, Gold, () => {
                Confirm("载入测试存档", "将覆盖当前本机进度，恢复至仓库内" + DescribeBundledTestSave() + "。是否继续？", () => { LoadBundledTestSave(); });
            });
            TInk(testBtn, Color.white);
            curY += 98f;
        }

        var resetBtn = Btn(parent, "ResetLocalSave", "开启新周目（清空本机存档）", 25, pad, curY, innerW, 82f, Vermilion, () => {
            Confirm("开启新周目", "确认从第 1 日序章《星港初笛》重新开始体验完整剧情引导吗？当前本机进度将被重置。", () => {
#if UNITY_EDITOR
                // A new local run isolates its source without overwriting the user sync preference.
                // Loading the bundled test save or toggling sync exits this mode.
                DevLocalOnlyRun = true;
#endif
                // Scene teardown calls OnDisable/OnApplicationQuit on the old run.
                // Block those writes before deleting the local save.
                saveReady = false; saveBlocked = true;
                PlayerPrefs.DeleteKey(SaveKey);
                PlayerPrefs.Save();
                UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
            });
        });
        TInk(resetBtn, Color.white);
    }

    AudioClip MakeTone(float freq, float dur)
    {
        int rate = 44100; int n = Mathf.CeilToInt(rate * dur);
        var data = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)rate;
            float env = Mathf.Min(1f, t / 0.004f) * Mathf.Min(1f, (dur - t) / 0.02f);
            data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * env * 0.30f;
        }
        var c = AudioClip.Create("tone", n, 1, rate, false);
        c.SetData(data, 0);
        return c;
    }
    void PlayTone(bool high)
    {
        if (sfx && sfxEnabled && sfxVolume > 0.001f)
            sfx.PlayOneShot(high ? toneHi : toneLo, sfxVolume);
    }

    // ---------- clock ----------
    void TickClock(float dt)
    {
        float remaining = Mathf.Max(0f, dt * timeSpeed);
        while (remaining > 0.0001f)
        {
            int hour = Mathf.FloorToInt(dayClock / 15f);
            if (economicHour != hour || traffic == null) { economicHour = hour; RecalcIncome(); }
            float step = Mathf.Min(remaining, (hour + 1) * 15f - dayClock);
            if (step <= 0.0001f) { dayClock = (hour + 1) * 15f; step = 0; }
            float fraction = step / GameDaySeconds;
            researchClock += step * ResearchSpeedMultiplier();
            while (researchClock >= 36f) { researchClock -= 36f; RPTick(); }
            foreach (var line in lines)
            {
                if (!operatingRates.TryGetValue(line, out var rate)) { RebuildOperatingRates(); rate = operatingRates[line]; }
                float gross = rate.gross * fraction;
                float vehicle = rate.vehicle * fraction, track = rate.track * fraction;
                line.todayGross += gross; line.todayMaintenance += vehicle; line.todayTrack += track;
                coins += gross - vehicle - track;
                line.todayRevenue = Mathf.RoundToInt(line.todayGross);
            }
            coins += StationCommercialTotalDaily() * fraction;
            dayClock += step; remaining -= step;
            if (dayClock >= GameDaySeconds - 0.0001f)
            {
                int finishedDay = day;
                RecordDailyReportSnapshot(finishedDay);
                dayClock = 0; day++; economicHour = -1;
                foreach (var line in lines)
                {
                    float beforeTax = line.todayGross - line.todayMaintenance - line.todayTrack;
                    float tax = Mathf.Max(0f, beforeTax) * 0.1f; coins -= tax;
                    line.recent.Add(Mathf.RoundToInt(beforeTax - tax));
                    if (line.recent.Count > 7) line.recent.RemoveAt(0);
                    line.todayGross = line.todayMaintenance = line.todayTrack = 0f; line.todayRevenue = 0;
                }
                if (hasYesterdayReport && bondDaysLeft == 0)
                {
                    int netInt = Mathf.RoundToInt(yesterdayReport.netProfit);
                    Toast("第 " + finishedDay + " 日结算：净" + (netInt >= 0 ? "盈 ¥" + netInt : "亏 ¥" + Mathf.Abs(netInt)) + "（点左上收益看日报）");
                }
                if (bondDaysLeft > 0)
                {
                    int targetDue = EffectiveBondDailyPayment();
                    if (coins >= targetDue)
                    {
                        coins -= targetDue;
                        bondDaysLeft--;
                        if (bondDaysLeft <= 0)
                        {
                            bondDaysLeft = 0; bondPrincipal = 0; bondDailyDue = 0; bondSubsidized = false; bondsRepaid++;
                            Toast("基建债券最后一期 ¥" + targetDue + " 已全额结清，企业信用上升！");
                        }
                        else Toast("基建债券扣款 ¥" + targetDue + "（剩 " + bondDaysLeft + " 天）");
                    }
                    else
                    {
                        double penalty = Math.Round(Math.Max(0d, coins) * 0.5d);
                        coins = Math.Max(0d, coins - penalty);
                        Toast("资金不足无法缴付今日债券 ¥" + targetDue + "，已扣除 ¥" + FormatCoins(penalty) + " 滞纳延息（本期顺延，仍剩 " + bondDaysLeft + " 天）");
                    }
                }
                if (companyPolicy == 3 && FoundCount() > 0)
                {
                    float dividend = TourismDividendDaily();
                    coins += dividend;
                    Toast("文旅专线红利 +¥" + Mathf.RoundToInt(dividend));
                }
                float cycleSub = CycleDailySubsidy();
                if (cycleSub > 0f)
                {
                    coins += cycleSub;
                    Toast("防汛防台运营补贴 +¥" + Mathf.RoundToInt(cycleSub));
                }
                float bayBonus = CrossBaySynergyDailyBonus();
                if (bayBonus > 0f)
                {
                    coins += bayBonus;
                    Toast("三地口岸联运补贴 +¥" + Mathf.RoundToInt(bayBonus));
                }
                if (consultantDaysLeft > 0)
                {
                    consultantDaysLeft--;
                    if (consultantDaysLeft <= 0) { consultantDaysLeft = 0; consultantId = -1; hasForecastSignature = false; Toast("特聘首席顾问聘期已满"); }
                }
                if (turboDaysLeft > 0)
                {
                    turboDaysLeft--;
                    if (turboDaysLeft <= 0) { turboDaysLeft = 0; if (timeSpeed > 3) timeSpeed = 3; Toast("4× 极速推演特权已到期"); }
                }
                if (autoDispatchDaysLeft > 0)
                {
                    autoDispatchDaysLeft--;
                    if (autoDispatchDaysLeft <= 0) { autoDispatchDaysLeft = 0; hasForecastSignature = false; Toast("AI 智能调度特权已到期"); }
                }
                if ((day - 1) % 7 == 0) AdvanceEconomyCycle();
                else EnsureCompanyInitialized();
                TickDailyTalents();
                EnsureTalentsInitialized(true);
                SyncStoryProgression(true);
                NotifySolarTermRollover();
                SyncCulturalCollection(true);
                if (day % 15 == 0 && lastAwardDay < day)
                    Toast("《年度星轨大赏》第 " + day + " 日颁奖开启，去「公司」领奖！");
                dailyStationBaseline = DailyConnectedCount();
                RollTrafficEvents();
                RefreshNoticeUnreadDot();
                SaveGame();
            }
        }
        UpdateTimeText();
    }
    int lastDisplayedMinute = -1, lastDisplayedDay = -1, lastDisplayedSpeed = -1, lastHudSolarTerm = -1;
    void UpdateTimeText()
    {
        if (!timeTxt) return;
        int mins = Mathf.FloorToInt(dayClock / GameDaySeconds * 1440f);
        if (mins != lastDisplayedMinute) { int hh = (mins / 60) % 24, mm = mins % 60; timeTxt.text = hh.ToString("00") + ":" + mm.ToString("00"); lastDisplayedMinute = mins; }
        int termIdx = CurrentSolarTermIndex();
        if (dayTxt && (day != lastDisplayedDay || termIdx != lastHudSolarTerm))
        {
            dayTxt.text = "第 " + day + " 日 · " + SolarTerms[termIdx].name;
            lastDisplayedDay = day;
            lastHudSolarTerm = termIdx;
        }
        if (speedTxt && timeSpeed != lastDisplayedSpeed) { speedTxt.text = timeSpeed == 0 ? "暂停" : timeSpeed + "×"; lastDisplayedSpeed = timeSpeed; }
    }
    void CycleSpeed()
    {
        int maxSpeed = turboDaysLeft > 0 ? 4 : 3;
        BeginUndo("切换倍速"); timeSpeed = timeSpeed >= maxSpeed ? 0 : timeSpeed + 1; EndUndo();
        UpdateTimeText();
    }

    // ---------- 客流与关键剧情 ----------
    float trafficRefresh;
    GameObject stationTrafficPanel;
    Text stationTrafficLabel;
    int stationTrafficIndex = -1;
    sealed class StoryEvent
    {
        public string chapterTag, speaker, title, text, objectiveHint;
        public bool allowSkip;
        // 预留立绘资源地址；当前只播放文字与 Unity 原生动效。
        public string portraitResource;
    }
    readonly Queue<StoryEvent> storyQueue = new Queue<StoryEvent>();
    readonly HashSet<string> storySeen = new HashSet<string>();
    GameObject storyCard;

    void TickStationTraffic(float dt)
    {
        trafficRefresh += dt;
        if (trafficRefresh < 2f) return;
        trafficRefresh = 0f;
        if (stationTrafficPanel && activePanel == stationTrafficPanel && stationTrafficLabel && stationTrafficIndex >= 0)
            {
            string description = StationTrafficDescription(stationTrafficIndex);
            stationTrafficLabel.text = DisplayPlaceAliases(description);
            var content = stationTrafficLabel.transform.parent.GetComponent<RectTransform>();
            float height = Mathf.Max(content.parent.GetComponent<RectTransform>().rect.height, MeasureTextHeight(description, 28, 884) + 8);
            content.sizeDelta = new Vector2(884, height); stationTrafficLabel.rectTransform.sizeDelta = new Vector2(884, height);
        }
        else { stationTrafficPanel = null; stationTrafficLabel = null; stationTrafficIndex = -1; }
    }
    int StationLineCount(int index)
    {
        int count = 0; foreach (var line in lines) if (line.st.Contains(index)) count++; return count;
    }
    string StationTrafficDescription(int index)
    {
        var station = stations[index]; int lineCount = StationLineCount(index);
        string state = !station.built ? "未开港 · 开港费用 ¥" + StationOpeningCost(index) : station.closed ? "已关停 · 星舰通过不停港" : lineCount == 0 ? "已开港 · 等待航线接入" : "运营中 · " + lineCount + " 条航线";
        string lineNames = "";
        foreach (var line in lines) if (line.st.Contains(index)) lineNames += (lineNames.Length == 0 ? "" : "、") + line.name;
        float demand = StationRequestedFlow(index), served = StationPassengerFlow(index), cap = StationCapacity(index);
        string pressure = demand > cap ? "星港候船爆满（" + Mathf.RoundToInt(demand / Mathf.Max(1f, cap) * 100f) + "%），建议扩建升级" : "星港负荷 " + Mathf.RoundToInt(demand / Mathf.Max(1f, cap) * 100f) + "%（运力宽裕）";
        string diag = StationFlowDiagnostics(index);
        return state + (lineNames.Length > 0 ? "\n途经：" + lineNames : "") + "\n日均需求 " + StationEstimatedDemand(index) + " 人 · 当前输送 " + Mathf.RoundToInt(served) + " / 需求 " + Mathf.RoundToInt(demand) + "\n" +
            "港内容量 " + Mathf.RoundToInt(cap) + " · " + pressure + "\n" +
            "站场动态：" + TrafficEventNames[station.trafficEvent] + "（需求 " +
            (TrafficEventFactors[station.trafficEvent] >= 1 ? "+" : "") + Mathf.RoundToInt((TrafficEventFactors[station.trafficEvent]-1)*100) + "%）\n" +
            StationAreaHint(index) + (string.IsNullOrEmpty(diag) ? "" : "\n" + diag);
    }
    bool OpenStation(int index)
    {
        var station = stations[index];
        int cost = StationOpeningCost(index);
        if (coins < cost) { Toast("金币不足：还差 ¥" + FormatShortfall(cost - coins)); return false; }
        if (station.built && !station.closed) return true;
        BeginUndo(station.built ? "重新开站" : "开站");
        coins -= cost; station.built = true; station.closed = false;
        RefreshStationDots(); RecalcIncome(); CheckSceneryOnLine(null);
        PlayBuildSfx(); TriggerHaptic(16);
        SpawnMapRipple(Pos(index), Matcha, 128f);
        SyncStoryProgression(true);
        EndUndo(); RefreshHUD();
        Toast(StationDisplayName(index, currentRegion) + " 已开港" + (cost > 0 ? " · ¥" + cost : ""));
        return true;
    }
    string StationIntroduction(int index)
    {
        var station = stations[index];
        switch (station.name)
        {
            case "南羊城": return "沧海之畔的商贸名城，街巷烟火与繁华市集交融。";
            case "奥林匹斯（月面门）": return "连接月之亮面与火星的跨区域交通节点。";
            case "阿西达利亚（月背门）": return "海岛与海湾相映的滨海城镇，邻接月背。";
            case "百川镇": return "水乡与手工作坊交织，城镇生活充满活力。";
            case "鹅山墟": return "星域相依，湖光与海岸风景相伴。";
            case "明珠镇": return "南溟水乡与传统手艺相映的城市。";
            case "锦溪墟": return "水网密布的滨海侨乡，延续着古朴文脉。";
            case "五岭口": return "山水环绕的北岭门户，奇峰丹崖风光闻名。";
            case "潮风港": return "滨海商埠，海派食肆与古港记忆交织。";
            case "银江墟": return "五津水乡，骑楼与老街故事相伴。";
            case "雷海埠": return "沧海半岛上的滨海港城，面海而兴。";
            case "荔枝城": return "西海之滨城市，海岸与百果林乡相伴。";
            case "砚山墟": return "西川之畔，山水与古砚文脉相映。";
            case "寒香镇": return "山间古驿之乡，围屋与乡村风物相连。";
            case "白浪港": return "依山面海的城市，渔港与海岸风光相伴。";
            case "东溪埠": return "山水相依的东溪上游城市。";
            case "银滩港": return "滨海山城，以海景与传统铁艺工坊闻名。";
            case "北山墟": return "北溪穿城而过，山水与温泉相伴。";
            case "金凤城": return "青溪之畔的古城，保留着传统古街风韵。";
            case "榕荫墟": return "海滨平原上的城镇，绿榕水穿城而过。";
            case "石鼓墟": return "群山之间的采石之乡，依西川而展。";
            case "柏拉图跃迁港": return "连接两地的口岸站，旅客往来频繁。";
            case "虹湾星门": return "跨越海湾的交通节点，连接三地。";
            case "静海基地": return "海港边的商业街区，都市与山景相邻。";
            case "翠竹咀": return "临海的繁华街区，可远眺帆影港。";
            case "东山涌": return "苍岚岛北岸的社区，邻近星域风景。";
            case "旺市角": return "街市与商铺密集，生活气息浓厚。";
            case "哥白尼环形山": return "老城街巷环绕，留存着历史建筑风貌。";
            case "莲心仔": return "海岛社区与街巷美食交融的区域。";
            case "木船厂": return "海岸与山林相伴，保留宁静的岛屿风貌。";
        }
        if (!string.IsNullOrEmpty(station.city))
            return "位于" + station.city + "，连接周边城镇与社区。";
        if (station.region == "月背") return "月背的沿线区域，星域古韵交融。";
        if (station.region == "月之亮面") return "月之亮面的沿线区域，星域与城市相伴。";
        return "沿线城镇与社区相连的地区。";
    }

    void ShowStationActions(int index)
    {
        if (index < 0 || index >= stations.Count || pageRoot || sceneryViewer) return;
        var station = stations[index];
        Vector3 world = mapRect.TransformPoint(Pos(index)); Vector2 anchor = root.InverseTransformPoint(world);
        string status = "预估客流 " + StationEstimatedDemand(index) + " 人/日";
        int lineCount = StationLineCount(index);
        if (!station.built) status += " · 开港 ¥" + StationOpeningCost(index);
        else if (station.closed) status += " · 已关停";
        else if (lineCount > 0) status += "\n当前客流 " + Mathf.RoundToInt(StationPassengerFlow(index)) + " 人/日";
        else status += " · 已开港";
        status += "\n" + StationAreaHint(index);
        float statusY = lineCount > 0 ? 190 : 104;
        float statusH = Mathf.Max(64, MeasureTextHeight(status, 24, 492) + 10);
        float introY = statusY + statusH + 10;
        float introH = Mathf.Max(38, MeasureTextHeight(StationIntroduction(index), 24, 492) + 8);
        float bubbleH = introY + introH + 80;
        var p = OpenBubble(StationDisplayName(index, currentRegion), bubbleH, 548, anchor);
        if (p)
        {
            var prt = p.GetComponent<RectTransform>();
            GetSafeMarginsInRoot(out float safeL, out float safeR, out float safeT, out float safeB);
            float sc = prt.localScale.x;
            float halfW = 548f * sc * 0.5f;
            float halfH = bubbleH * sc * 0.5f;
            float leftX = safeL + 20f + halfW;
            float topY = Mathf.Max(safeB + 180f + halfH, RootH - safeT - 245f - halfH);
            prt.anchoredPosition = new Vector2(leftX, topY);
            var bgImg = p.GetComponent<Image>();
            if (bgImg) bgImg.raycastTarget = true;
            var drag = p.AddComponent<DraggablePanel>();
            drag.target = prt;
            drag.bounds = root;
            drag.padding = new Vector4(safeL + 12f, safeR + 12f, safeT + 12f, safeB + 12f);
        }
        var intro = Txt(p.transform, StationIntroduction(index), 24, InkSoft, 28, introY, 492, introH, TextAnchor.MiddleLeft);
        intro.name = "StationIntroduction";
        intro.raycastTarget = false;
        var statusTxt = Txt(p.transform, status, 24, InkSoft, 28, statusY, 492, statusH, TextAnchor.UpperLeft);
        statusTxt.raycastTarget = false;
        if (lineCount > 0) {
            var remove = Btn(p.transform, "RemoveWaystation", "移除航线途经港", 25, 28, 102, 492, 74, Paper2, () => OpenRemoveStationLines(index, 0));
            TInk(remove, Ink);
        }
        bool canOpen = !station.built || station.closed;
        if (canOpen)
        {
            var open = Btn(p.transform, "OpenStation", station.built ? "重新开港" : "开港 ¥" + StationOpeningCost(index), 26, 28, 18, 238, 74, Matcha, () => { if (OpenStation(index)) { ClosePanel(true); ShowStationActions(index); } });
            TInk(open, Color.white);
        }
        var detail = Btn(p.transform, "StationInfo", "星港详情", 26, canOpen ? 282 : 28, 18, canOpen ? 238 : 492, 74, Indigo, () => OpenStationInfo(index));
        TInk(detail, Color.white);
        ShowStationOdArcs(index);
    }

    void RecordDailyReportSnapshot(int settledDay)
    {
        float gross = 0f, maintAndTax = 0f, bestNet = float.NegativeInfinity, worstNet = float.PositiveInfinity;
        string bestName = "—", worstName = "—";
        foreach (var line in lines)
        {
            float beforeTax = line.todayGross - line.todayMaintenance - line.todayTrack;
            float tax = Mathf.Max(0f, beforeTax) * 0.1f;
            float net = beforeTax - tax;
            gross += line.todayGross;
            maintAndTax += line.todayMaintenance + line.todayTrack + tax;
            if (net > bestNet) { bestNet = net; bestName = line.name; }
            if (net < worstNet) { worstNet = net; worstName = line.name; }
        }
        float commRent = StationCommercialTotalDaily();
        CollectTrafficAudit(out float crowdLost, out float unreachedLost, out float elasticLost, out int bnIdx, out _, out string bnNote);
        yesterdayReport = new DailyReportSnapshot
        {
            settledDay = settledDay,
            gross = gross,
            commercialRent = commRent,
            maintAndTax = maintAndTax,
            netProfit = gross + commRent - maintAndTax,
            passengers = traffic != null ? traffic.totalPassengers : 0f,
            bestLineName = bestName,
            bestLineNet = float.IsNegativeInfinity(bestNet) ? 0f : bestNet,
            worstLineName = worstName,
            worstLineNet = float.IsPositiveInfinity(worstNet) ? 0f : worstNet,
            crowdLost = crowdLost,
            unreachedLost = unreachedLost,
            elasticLost = elasticLost,
            bottleneckStation = bnIdx,
            bottleneckNote = bnNote
        };
        hasYesterdayReport = true;
    }

    void OpenDailyBriefingPopup()
    {
        if (dragging || pageRoot || sceneryViewer) return;
        RecalcIncome();
        DailyReportSnapshot rep;
        bool isLiveEstimate = !hasYesterdayReport;
        if (hasYesterdayReport)
        {
            rep = yesterdayReport;
        }
        else
        {
            float gross = 0f, maintTax = 0f, bestNet = float.NegativeInfinity, worstNet = float.PositiveInfinity;
            string bestName = "—", worstName = "—";
            foreach (var l in lines)
            {
                var f = ForecastFor(l);
                float net = f.Profit;
                gross += f.gross;
                maintTax += f.vehicle + f.track + f.tax;
                if (net > bestNet) { bestNet = net; bestName = l.name; }
                if (net < worstNet) { worstNet = net; worstName = l.name; }
            }
            float commRent = StationCommercialTotalDaily() + CrossBaySynergyDailyBonus();
            CollectTrafficAudit(out float cL, out float uL, out float eL, out int bn, out _, out string bnNote);
            rep = new DailyReportSnapshot
            {
                settledDay = day,
                gross = gross,
                commercialRent = commRent,
                maintAndTax = maintTax,
                netProfit = gross + commRent - maintTax,
                passengers = traffic != null ? traffic.totalPassengers : 0f,
                bestLineName = bestName,
                bestLineNet = float.IsNegativeInfinity(bestNet) ? 0f : bestNet,
                worstLineName = worstName,
                worstLineNet = float.IsPositiveInfinity(worstNet) ? 0f : worstNet,
                crowdLost = cL,
                unreachedLost = uL,
                elasticLost = eL,
                bottleneckStation = bn,
                bottleneckNote = bnNote
            };
        }

        bool hasBn = rep.bottleneckStation >= 0 && rep.bottleneckStation < stations.Count;
        float popH = hasBn ? 920f : 790f;
        string popTitle = isLiveEstimate ? "运营手帐日报 · 第 " + rep.settledDay + " 日实时预估" : "运营手帐日报 · 第 " + rep.settledDay + " 日实绩";
        var p = OpenPopup(popTitle, popH, 940, "daily_briefing");

        float y = popH - 166f;
        var cashCard = Rect("CashSummary", p.transform, 38f, y - 92f, 864f, 92f);
        Rounded(cashCard, Paper2);
        int netVal = Mathf.RoundToInt(rep.netProfit);
        Txt(cashCard.transform, "当前账户可用资金：¥" + Math.Floor(coins).ToString("N0"), 26, Ink, 24f, 46f, 520f, 38f, TextAnchor.MiddleLeft);
        Txt(cashCard.transform, (netVal >= 0 ? "净盈利 +¥" + netVal : "净亏损 -¥" + Mathf.Abs(netVal)), 30, netVal >= 0 ? Matcha : Vermilion, 550f, 44f, 290f, 42f, TextAnchor.MiddleRight);
        Txt(cashCard.transform, "票款 ¥" + Mathf.RoundToInt(rep.gross) + "  ·  物业/联运 ¥" + Mathf.RoundToInt(rep.commercialRent) + "  ·  运维税费 -¥" + Mathf.RoundToInt(rep.maintAndTax), 22, InkSoft, 24f, 8f, 816f, 34f, TextAnchor.MiddleLeft);
        y -= 114f;

        var lineCard = Rect("LinePerfCard", p.transform, 38f, y - 140f, 864f, 140f);
        Rounded(lineCard, Paper2);
        Txt(lineCard.transform, "航线效益风向", 24, Ink, 24f, 94f, 400f, 36f, TextAnchor.MiddleLeft);
        string lineText = lines.Count == 0
            ? "尚未开通任何运营航线，长按地图星港并拖向下一港即可开线。"
            : "领跑航线：" + rep.bestLineName + "（日净利 ¥" + Mathf.RoundToInt(rep.bestLineNet) + "）\n待优航线：" + rep.worstLineName + "（日净利 ¥" + Mathf.RoundToInt(rep.worstLineNet) + "）";
        Txt(lineCard.transform, lineText, 22, InkSoft, 24f, 12f, 816f, 80f, TextAnchor.UpperLeft);
        y -= 162f;

        var flowCard = Rect("TrafficAuditCard", p.transform, 38f, y - 188f, 864f, 188f);
        Rounded(flowCard, Paper2);
        Txt(flowCard.transform, "客流归因体检（全网多路径评估）", 24, Ink, 24f, 142f, 640f, 36f, TextAnchor.MiddleLeft);
        string flowSummary =
            "当前时段全网实载：" + Mathf.RoundToInt(rep.passengers) + " 人次\n" +
            "· 星港/星舱满载受阻：约 " + Mathf.RoundToInt(rep.crowdLost) + " 人次\n" +
            "· 热门目的尚无直达：约 " + Mathf.RoundToInt(rep.unreachedLost) + " 人次\n" +
            "· 绕行耗时/票价敏感流失：约 " + Mathf.RoundToInt(rep.elasticLost) + " 人次";
        Txt(flowCard.transform, flowSummary, 22, InkSoft, 24f, 12f, 816f, 126f, TextAnchor.UpperLeft);
        y -= 210f;

        if (hasBn)
        {
            int targetIdx = rep.bottleneckStation;
            string stName = StationDisplayName(targetIdx, stations[targetIdx].region);
            var bnCard = Rect("BottleneckCard", p.transform, 38f, y - 118f, 864f, 118f);
            Rounded(bnCard, new Color(0.98f, 0.92f, 0.86f));
            Txt(bnCard.transform, "首要瓶颈港：" + stName + "（" + DisplayPlaceAliases(stations[targetIdx].region) + "）", 24, Vermilion, 24f, 70f, 560f, 38f, TextAnchor.MiddleLeft);
            Txt(bnCard.transform, rep.bottleneckNote, 24, InkSoft, 24f, 12f, 560f, 58f, TextAnchor.UpperLeft);
            var locBtn = Btn(bnCard.transform, "FocusBottleneck", "定位查看", 24, 614f, 24f, 226f, 70f, Indigo, () => { ClosePanel(true); FocusMapOnStation(targetIdx); });
            TInk(locBtn, Color.white);
        }

        var closeBtn = Btn(p.transform, "BriefingClose", "收起手帐", 26, 38f, 32f, 864f, 82f, new Color(0.92f, 0.89f, 0.83f), () => ClosePanel(true));
        TInk(closeBtn, Ink);
    }
    void OpenStationInfo(int index)
    {
        if (index < 0 || index >= stations.Count || pageRoot || sceneryViewer) return;
        var station = stations[index];
        string description = StationTrafficDescription(index);
        float contentHeight = Mathf.Clamp(MeasureTextHeight(description, 28, 884) + 16, 100, H - 740);
        string titleText = StationDisplayName(index, currentRegion) + " · Lv." + station.level
            + (!string.IsNullOrEmpty(station.customName) ? "  <size=22><color=#787266>（原名：" + station.name + "）</color></size>" : "");
        var p = OpenPopup(titleText, contentHeight + 510, 960, "station_info_" + index);
        AttachPopupCurrencyChip(p, 960f, contentHeight + 510);
        stationTrafficPanel = p; stationTrafficIndex = index;
        stationTrafficLabel = ScrollableText(p.transform, description, 28, InkSoft, 38, 420, 884, contentHeight);
        float hubCostScale = Mathf.Clamp(Mathf.Sqrt(Mathf.Max(80f, StationBaselineDemand(index)) / 100f), 1f, 1.85f);
        int rawCost = Mathf.RoundToInt(320f * Mathf.Pow(2.05f, station.level - 1) * hubCostScale);
        int step = rawCost >= 5000 ? 100 : 10;
        int cost = Mathf.Max(10, Mathf.RoundToInt((float)rawCost / step) * step);
        var upgrade = Btn(p.transform, "UpgradeStation", station.level >= 10 ? "已满级" : "扩建升级  ¥" + cost, 27, 38, 275, 420, 92, Matcha, () => {
            if (!station.built) { Toast("请先开港"); return; }
            if (station.level >= 10) { Toast("星港已满级"); return; }
            if (coins < cost) { Toast("金币不足"); return; }
            BeginUndo("升级星港"); coins -= cost; station.level++; RecalcIncome(); EndUndo(); RefreshHUD(); OpenStationInfo(index); Toast("星港升级成功");
        }); TInk(upgrade, Color.white);
        bool needsOpening = !station.built || station.closed;
        string toggleLabel = !station.built ? "开港 ¥" + StationOpeningCost(index) : station.closed ? "重新开港" : "关停";
        var toggle = Btn(p.transform, "ToggleStation", toggleLabel, 27, 500, 275, 420, 92, needsOpening ? Indigo : Vermilion, () => {
            if (needsOpening) { if (!OpenStation(index)) return; }
            else { BeginUndo("关停星港"); station.closed = true; RefreshStationDots(); RecalcIncome(); EndUndo(); RefreshHUD(); Toast(StationDisplayName(index, currentRegion) + " 已关停"); }
            OpenStationInfo(index);
        }); TInk(toggle, Color.white);
        int curFac = Mathf.Clamp(station.facLevel, 0, StationFacilityNames.Length - 1);
        int facCost = FacilityUpgradeCost(curFac);
        int nextRent = curFac < 3 ? StationFacilityRents[curFac + 1] : 0;
        int curRent = StationFacilityRents[curFac];
        int nextPct = curFac < 3 ? Mathf.RoundToInt((StationFacilityDemandMults[curFac + 1] - 1f) * 100f) : 0;
        int curPct = Mathf.RoundToInt((FacilityDemandFactor(station) - 1f) * 100f);
        string facLabel = curFac < 3
            ? "港前物业 · 建「" + StationFacilityNames[curFac + 1] + "」(¥" + facCost + " · 客流+" + nextPct + "% · 租¥" + nextRent + ")"
            : "港前物业 · " + StationFacilityNames[curFac] + "（满级 · 客流+" + curPct + "% · 日租¥" + Mathf.RoundToInt(curRent * TechMult() * (HasPartner(1) ? 1.25f : 1f)) + "）";
        var facBtn = Btn(p.transform, "StationFacility", facLabel, 24, 38, 155, 882, 92, curFac < 3 ? new Color(0.76f, 0.50f, 0.16f) : new Color(0.86f, 0.84f, 0.78f), () => {
            if (!IsFeatureUnlocked(GameFeature.SceneryAndCommercial)) { PlayErrorSfx(); Toast(FeatureLockHint(GameFeature.SceneryAndCommercial)); return; }
            if (!station.built) { Toast("请先开港"); return; }
            if (curFac >= 3) { Toast("港前商业已达最高等级"); return; }
            if (coins < facCost) { PlayErrorSfx(); Toast("金币不足"); return; }
            BeginUndo("港前商业升级");
            coins -= facCost;
            station.facLevel = curFac + 1;
            station.riderBonus = StationFacilityRents[station.facLevel];
            RecalcIncome();
            PlayCoinSfx();
            EndUndo();
            RefreshHUD();
            OpenStationInfo(index);
            Toast("港前商业升至" + StationFacilityNames[station.facLevel]);
        });
        TInk(facBtn, curFac < 3 ? Color.white : InkSoft);
        var rename = Btn(p.transform, "StationRename", "修改港名", 26, 38, 36, 420, 80, Indigo, () => OpenStationRenamePopup(index));
        TInk(rename, Color.white);
        var close = Btn(p.transform, "StationClose", "关闭", 26, 500, 36, 420, 80, new Color(0.92f,0.89f,0.83f), () => { ClosePanel(true); ShowStationActions(index); });
        TInk(close, Ink);
    }
    void OpenStationRenamePopup(int index)
    {
        if (index < 0 || index >= stations.Count) return;
        var station = stations[index];
        var p = OpenPopup("修改星港名称", 430, 840, "station_rename_" + index);
        Txt(p.transform, "默认港名：" + station.name + "（最多 12 字，不影响航线与存档绑定）", 23, InkSoft, 36, 286, 768, 38, TextAnchor.MiddleLeft);
        var inputBox = Rect("StationNameInput", p.transform, 36, 174, 768, 86);
        Rounded(inputBox, new Color(1f, 0.99f, 0.97f)); SoftShadow(inputBox);
        var placeholder = Txt(inputBox.transform, "请输入新的星港名称", 28, new Color(InkSoft.r, InkSoft.g, InkSoft.b, 0.45f), 22, 8, 724, 70, TextAnchor.MiddleLeft);
        var inputText = Txt(inputBox.transform, "", 30, Ink, 22, 8, 724, 70, TextAnchor.MiddleLeft);
        inputText.supportRichText = false;
        var input = inputBox.AddComponent<InputField>();
        input.textComponent = inputText;
        input.placeholder = placeholder;
        input.characterLimit = 12;
        input.lineType = InputField.LineType.SingleLine;
        input.text = StationDisplayName(index, currentRegion);
        input.ActivateInputField();
        Action<string> applyName = raw => {
            string cleaned = string.IsNullOrWhiteSpace(raw) ? "" : raw.Replace("\r", "").Replace("\n", "").Replace(":", "").Trim();
            if (cleaned.Length > 12) cleaned = cleaned.Substring(0, 12);
            if (string.IsNullOrEmpty(cleaned)) { PlayErrorSfx(); Toast("港名不能为空"); return; }
            string nextCustom = cleaned == station.name ? "" : cleaned;
            if ((station.customName ?? "") == nextCustom) { OpenStationInfo(index); return; }
            BeginUndo("修改港名");
            station.customName = nextCustom;
            if (stationLabels.TryGetValue(index, out var lbl) && lbl) lbl.text = StationDisplayName(index, currentRegion);
            LayoutStationLabels();
            EndUndo();
            PlayClickSfx();
            OpenStationInfo(index);
            Toast("港名已更新为「" + StationDisplayName(index, currentRegion) + "」");
        };
        var ok = Btn(p.transform, "ConfirmRename", "确认改名", 26, 36, 42, 248, 84, Matcha, () => applyName(input.text));
        TInk(ok, Color.white);
        var reset = Btn(p.transform, "ResetRename", "恢复默认", 25, 296, 42, 248, 84, Gold, () => {
            if (string.IsNullOrEmpty(station.customName)) { OpenStationInfo(index); return; }
            BeginUndo("恢复默认港名");
            station.customName = "";
            if (stationLabels.TryGetValue(index, out var lbl) && lbl) lbl.text = StationDisplayName(index, currentRegion);
            LayoutStationLabels();
            EndUndo();
            OpenStationInfo(index);
            Toast("已恢复默认港名「" + station.name + "」");
        });
        TInk(reset, Color.white);
        var cancel = Btn(p.transform, "CancelRename", "取消", 25, 556, 42, 248, 84, Paper2, () => OpenStationInfo(index));
        TInk(cancel, Ink);
    }
    void QueueStory(string key, string title, string content)
    {
        if (!storySeen.Add(key)) return;
        storyQueue.Enqueue(new StoryEvent {
            chapterTag = "沿途风物",
            speaker = "星航手记",
            title = title,
            text = content,
            objectiveHint = "",
            allowSkip = false
        });
        SaveGame();
    }
    void TickStory()
    {
        if (storyCard || storyQueue.Count == 0 || pageRoot || activePanel || sceneryViewer || lineBubbleRoot || dragging) return;
        var story = storyQueue.Dequeue();
        bool hasObj = !string.IsNullOrEmpty(story.objectiveHint);
        float cardH = hasObj ? 450f : 390f;
        storyCard = Rect("StoryCard", root, 40f, 204f, 1000f, cardH);
        Rounded(storyCard, new Color(CardBg.r, CardBg.g, CardBg.b, 0.985f)); SoftShadow(storyCard);
        JournalTape(storyCard.transform, 428f, cardH - 16f, 144f);
        var accent = Img(Rect("Accent", storyCard.transform, 0f, 20f, 8f, cardH - 40f), Gold, spriteRound); accent.raycastTarget = false;

        string headerLine = string.IsNullOrEmpty(story.chapterTag) ? "沿途故事" : story.chapterTag;
        if (!string.IsNullOrEmpty(story.speaker)) headerLine += "  ·  " + story.speaker;
        Txt(storyCard.transform, headerLine, 22, Matcha, 34f, cardH - 52f, 700f, 34f, TextAnchor.MiddleLeft);
        Txt(storyCard.transform, story.title, 36, Ink, 34f, cardH - 106f, 920f, 50f, TextAnchor.MiddleLeft);
        Txt(storyCard.transform, story.text, 25, Ink, 34f, hasObj ? 168f : 108f, 932f, cardH - (hasObj ? 284f : 224f), TextAnchor.UpperLeft);

        if (hasObj)
        {
            var objBox = Rect("StoryObjective", storyCard.transform, 34f, 106f, 932f, 58f);
            Rounded(objBox, Color.Lerp(CardBg, Indigo, 0.08f));
            Txt(objBox.transform, story.objectiveHint, 24, Indigo, 18f, 0f, 896f, 58f, TextAnchor.MiddleLeft);
        }

        if (story.allowSkip)
        {
            var skipBtn = Btn(storyCard.transform, "SkipGuide", "跳过星际航行引导", 24, 34f, 22f, 246f, 70f, Paper2, () => {
                Confirm("跳过星际航行引导", "将直接解锁全部系统并进入自由经营，稍后仍可在设置中重温剧情。确认跳过？", SkipTutorialFlow);
            });
            TInk(skipBtn, InkSoft);
        }

        var close = Btn(storyCard.transform, "Continue", "继续运营", 25, 740f, 22f, 226f, 68f, Matcha, () => {
            if (storyCard) Destroy(storyCard);
            storyCard = null;
        });
        TInk(close, Color.white);
        if (undoButton) undoButton.transform.SetAsLastSibling();
        var cg = storyCard.AddComponent<CanvasGroup>(); cg.alpha = 0f;
        PlayStorySfx();
        StartCoroutine(AnimateStory(storyCard.GetComponent<RectTransform>(), cg));
    }
    System.Collections.IEnumerator AnimateStory(RectTransform rect, CanvasGroup group)
    {
        Vector2 end = rect.anchoredPosition, start = end + new Vector2(0, -35); float elapsed = 0;
        while (rect && group && elapsed < 0.3f)
        {
            elapsed += Time.unscaledDeltaTime; float t = Mathf.Clamp01(elapsed / 0.3f); float ease = 1f - (1f-t)*(1f-t)*(1f-t);
            rect.anchoredPosition = Vector2.Lerp(start, end, ease); group.alpha = ease; yield return null;
        }
        if (rect && group) { rect.anchoredPosition = end; group.alpha = 1; }
    }

    // 拖线造价跟随指针，实际已吸附部分与待吸附段的预估分开标示。
    GameObject dragCostBadge;
    Text dragCostHeading, dragCostNumber, dragCostSubText;
    float dragCostDisplayed, dragCostPulse;
    int dragCostLastCommitted, dragCostLastHov = -2, dragCostLastDraftCount = -1, dragCostAddedPassengers;
    void BuildDragCostBadge()
    {
        dragCostBadge = Rect("DragCostBadge", root, 0, 0, 308, 138);
        Rounded(dragCostBadge, CardBg);
        dragCostBadge.GetComponent<Image>().raycastTarget = false; SoftShadow(dragCostBadge);
        dragCostHeading = Txt(dragCostBadge.transform, "航线造价", 23, InkSoft, 14, 98, 280, 30, TextAnchor.MiddleCenter);
        FitSingleLine(dragCostHeading, 23);
        dragCostNumber = Txt(dragCostBadge.transform, "¥0", 40, Ink, 14, 42, 280, 52, TextAnchor.MiddleCenter);
        dragCostSubText = Txt(dragCostBadge.transform, "拖向星港查看预计客流", 20, Matcha, 14, 10, 280, 28, TextAnchor.MiddleCenter);
        FitSingleLine(dragCostSubText, 20);
        dragCostBadge.SetActive(false);
    }
    void UpdateDragCostBadge(Vector2 point, Vector2 pointer, float dt)
    {
        if (!dragCostBadge || draft.Count == 0) return;
        Vector2 current = Pos(draft[draft.Count - 1]);
        float remaining = Vector2.Distance(current, point);
        bool preview = !IsLoop(draft) && remaining > 24f;
        int value = draftCost + (preview ? Mathf.RoundToInt(30f + remaining * 0.75f) : 0);
        if (draftCost != dragCostLastCommitted) { dragCostLastCommitted = draftCost; dragCostPulse = 0.22f; }
        dragCostDisplayed = Mathf.Lerp(dragCostDisplayed, value, 1f - Mathf.Exp(-18f * dt));
        if (Mathf.Abs(dragCostDisplayed-value) < 0.5f) dragCostDisplayed = value;
        dragCostHeading.text = preview
            ? (draftPreviewNeed >= 0.72f ? "高需走廊 · 含拖拽段" : draftPreviewNeed >= 0.40f ? "适中需求 · 含拖拽段" : "客需较平 · 含拖拽段")
            : "已选造价";
        dragCostNumber.text = "¥" + Mathf.RoundToInt(dragCostDisplayed);
        dragCostNumber.color = value > coins ? Vermilion : Ink;
        if (dragCostSubText)
        {
            if (hoverStation != dragCostLastHov || draft.Count != dragCostLastDraftCount)
            {
                dragCostLastHov = hoverStation;
                dragCostLastDraftCount = draft.Count;
                dragCostAddedPassengers = hoverStation >= 0 ? EstimateDraftAddedPassengers(draft, hoverStation) : 0;
            }
            if (hoverStation >= 0 && dragCostAddedPassengers > 0)
            {
                dragCostSubText.text = "预计增客 +" + dragCostAddedPassengers + " 人/日";
                dragCostSubText.color = Matcha;
            }
            else if (preview)
            {
                dragCostSubText.text = draftPreviewNeed >= 0.72f ? "高潜直达方向 · 建议延伸" : draftPreviewNeed >= 0.40f ? "客需适中 · 可继续拖向星港" : "拖向目标港查看预计客流";
                dragCostSubText.color = draftPreviewNeed >= 0.72f ? Vermilion : InkSoft;
            }
            else
            {
                dragCostSubText.text = "松手即按草案成线（" + draft.Count + " 港）";
                dragCostSubText.color = Indigo;
            }
        }
        dragCostPulse = Mathf.Max(0, dragCostPulse-dt);
        float pop = 1f + 0.12f * Mathf.Sin((dragCostPulse / 0.22f) * Mathf.PI);
        dragCostNumber.rectTransform.localScale = Vector3.one * pop;
        // 方向优先取「已确定的最后一段」，只有刚起步时跟鼠标；避免气泡随鼠标乱晃
        Vector2 dir;
        if (draft.Count >= 2) dir = Pos(draft[draft.Count - 1]) - Pos(draft[draft.Count - 2]);
        else dir = point - current;
        if (dir.sqrMagnitude < 4f) dir = new Vector2(0f, 1f);
        dir.Normalize();
        Vector2 at = (Vector2)root.InverseTransformPoint(mapRect.TransformPoint(pointer)) - root.rect.min;
        float gap = 138f + Mathf.Abs(dir.x) * 52f;  // 水平方向再多留一段，避免气泡压住拖拽点
        Vector2 anchor = at - dir * gap;            // 放在拖拽前进方向的反方向
        GetSafeMarginsInRoot(out float safeL, out float safeR, out float safeT, out float safeB);
        float minX = safeL + 12f;
        float maxX = Mathf.Max(minX, RootW - safeR - 320f);
        float minY = safeB + 180f;
        float maxY = Mathf.Max(minY, RootH - safeT - 328f);
        float x = Mathf.Clamp(anchor.x - 154f, minX, maxX);
        float y = Mathf.Clamp(anchor.y - 64f, minY, maxY);
        var badgeRt = dragCostBadge.GetComponent<RectTransform>();
        var target = new Vector2(x, y);
        if (dt >= 0.5f) badgeRt.anchoredPosition = target;   // 刚出现时直接落位
        else badgeRt.anchoredPosition = Vector2.Lerp(badgeRt.anchoredPosition, target, 1f - Mathf.Exp(-14f * dt));
    }

    // 与一代收益飘字共用深色调色板，保持浅色描边以便在地图上辨认。
    static readonly Color32[] FloaterColors = {
        new Color32(150, 100, 10, 255), new Color32(26, 100, 46, 255),
        new Color32(160, 44, 36, 255), new Color32(30, 70, 130, 255),
        new Color32(120, 70, 20, 255)
    };
    readonly System.Random floaterColorRandom = new System.Random();
    // ---------- income floaters ----------
    void SpawnFloater(int amount, Vector2 at)
    {
        if (amount <= 0 || !floaterLayer || qualityTier <= 0) return;
        if (floaters.Count >= 8) return;
        double chance = qualityTier >= 2 ? 0.32 : 0.20;
        if (floaterColorRandom.NextDouble() > chance) return;
        var go = Rect("Float", floaterLayer, at.x - 100, at.y + 18, 200, 48);
        var rt = go.GetComponent<RectTransform>();
        rt.pivot = new Vector2(0.5f, 0.5f);
        if (qualityTier >= 2) rt.localScale = Vector3.one * 1.24f;
        var t = go.AddComponent<Text>();
        t.font = font; t.fontSize = ScaledFontSize(27); t.alignment = TextAnchor.MiddleCenter;
        t.horizontalOverflow = HorizontalWrapMode.Overflow; t.verticalOverflow = VerticalWrapMode.Overflow;
        t.color = FloaterColors[floaterColorRandom.Next(FloaterColors.Length)]; t.text = "+¥" + amount; t.raycastTarget = false;
        var sh = qualityTier >= 1 ? go.AddComponent<Shadow>() : null;
        if (sh) sh.effectColor = new Color(1, 0.99f, 0.95f, 0.85f);
        var f = new Floater { txt = t, rt = rt, age = 0f };
        floaters.Add(f);
    }
    void TickFloaters(float dt)
    {
        for (int i = floaters.Count - 1; i >= 0; i--)
        {
            var f = floaters[i];
            if (!f.rt || !f.txt) { floaters.RemoveAt(i); continue; }
            f.age += dt;
            f.rt.anchoredPosition += new Vector2(0, dt * 38f);
            if (qualityTier >= 2 && f.age < 0.22f)
            {
                float s = Mathf.Lerp(1.24f, 1f, 1f - Mathf.Pow(1f - Mathf.Clamp01(f.age / 0.22f), 3f));
                f.rt.localScale = new Vector3(s, s, 1f);
            }
            else if (f.rt.localScale.x > 1.001f)
            {
                f.rt.localScale = Vector3.one;
            }
            var c = f.txt.color;
            c.a = Mathf.Clamp01(1f - (f.age - 0.55f) / 0.7f);
            f.txt.color = c;
            if (c.a <= 0f) { Destroy(f.rt.gameObject); floaters.RemoveAt(i); }
        }
    }

    void ClearFloaters()
    {
        foreach (var floater in floaters) if (floater.rt) Destroy(floater.rt.gameObject);
        floaters.Clear();
    }

    // ---------- station dots ----------
    bool[] stationOpen;
    void RefreshStationDots()
    {
        if (stationOpen == null || stationOpen.Length != stations.Count) stationOpen = new bool[stations.Count];
        for (int i = 0; i < stationOpen.Length; i++) stationOpen[i] = stations[i].built && !stations[i].closed;
        foreach (var kv in stationImgs)
        {
            if (stations[kv.Key].portal) continue;
            kv.Value.color = stationOpen[kv.Key] ? Gold : Color.white;
        }
        UpdateRegionalMarkers(true);
    }
    bool IsOpen(int i) { return stationOpen != null && i >= 0 && i < stationOpen.Length && stationOpen[i]; }

    // ---------- long press / draw ----------
    void BeginPress(int idx)
    {
        if (dragging) return;
        ClearStationOdArcs();
        pressIdx = idx; pressTimer = 0f; pressing = true; pressStart = Input.mousePosition;
        HidePressRing();
    }
    void HidePressRing()
    {
        foreach (var kv in pressRings)
        {
            var ring = kv.Value;
            if (!ring) continue;
            if (ring.gameObject.activeSelf) ring.gameObject.SetActive(false);
            var rt = ring.rectTransform;
            rt.anchoredPosition = new Vector2(-14f, -14f);
            rt.localScale = Vector3.one;
        }
    }
    void TickPress(float dt)
    {
        if (!pressing) return;
        if (!Input.GetMouseButton(0))
        {
            pressing = false; pressIdx = -1;
            HidePressRing();
            return;
        }
        if (Vector2.Distance((Vector2)Input.mousePosition, pressStart) > 26f && pressTimer < 0.42f)
        {
            pressing = false; pressIdx = -1;
            HidePressRing();
            return;
        }
        pressTimer += dt;
        if (pressRings.TryGetValue(pressIdx, out var pressRing) && pressRing)
        {
            if (!pressRing.gameObject.activeSelf) pressRing.gameObject.SetActive(true);
            float p = Mathf.Clamp01(pressTimer / 0.42f);
            pressRing.fillAmount = p;
            pressRing.color = new Color(0.95f, 0.42f, 0.26f, Mathf.Lerp(0.62f, 0.98f, p));
            // 蓄力震颤：越接近长按完成，抖动越明显
            float t = Time.unscaledTime;
            float amp = Mathf.Lerp(0.5f, 2.8f, p * p);
            var rt = pressRing.rectTransform;
            rt.anchoredPosition = new Vector2(-14f + Mathf.Sin(t * 47f) * amp, -14f + Mathf.Cos(t * 61f) * amp);
            float sc = 1f + 0.035f * p * Mathf.Sin(t * 32f);
            rt.localScale = new Vector3(sc, sc, 1f);
        }
        if (pressTimer >= 0.42f)
        {
            HidePressRing();
            int idx = pressIdx; pressing = false; pressIdx = -1;
            StartDraw(idx);
        }
    }
    void StartDraw(int idx)
    {
        if (dragging || idx < 0 || idx >= stations.Count) return;
        // 航线已满时，从「不属于任何航线」的星港起手只能新开线 —— 在长按开始就拦住，别等拖完才说
        bool onLine = false;
        foreach (var line in lines) if (line.st.Contains(idx)) { onLine = true; break; }
        if (!onLine && RegionLineCount(currentRegion) >= LineLimit())
        {
            HidePressRing();
            Toast(currentRegion + "航线已达上限（" + LineLimit() + " 条）");
            return;
        }
        CloseLineBubble();
        draft.Clear(); draftSegNeeds.Clear(); draft.Add(idx); draftCost = DraftCostOf(draft);
        PrepareDraftNeedField(idx, draft);
        draftPreviewNeed = EvaluateDragDirectionNeed(idx, Pos(idx));
        draftPreviewColor = NeedColorGradient(draftPreviewNeed);
        dragFrom = idx; dragging = true; hoverStation = -1;
        dragCostDisplayed = 0; dragCostLastCommitted = 0; dragCostPulse = 0;
        if (dragCostBadge) { dragCostBadge.SetActive(true); UpdateDragCostBadge(Pos(idx), Pos(idx), 1f); }
        RedrawDraft();
        if (previewImg) { previewImg.color = draftPreviewColor; previewImg.gameObject.SetActive(true); }
        if (dragTip) { dragTip.color = draftPreviewColor; dragTip.gameObject.SetActive(true); }
        UpdateRings();
        Haptic.Vibrate(22); PlayTone(false);
    }
    void EndDraw()
    {
        if (!dragging) return;
        dragging = false; hoverStation = -1;
        if (dragCostBadge) dragCostBadge.SetActive(false);
        if (previewImg) previewImg.gameObject.SetActive(false);
        if (dragTip) dragTip.gameObject.SetActive(false);
        UpdateRings();
        if (draft.Count >= 2) FinishDraft();
        else CancelDraft();
    }
    void CancelDraft()
    {
        draft.Clear(); draftSegNeeds.Clear(); draftCost = 0;
        if (dragCostBadge) dragCostBadge.SetActive(false);
        ClearDraftSegs(); ResetBuildVisuals();
    }
}
