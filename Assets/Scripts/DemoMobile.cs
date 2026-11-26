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
        var bgmCard = Rect("BgmCard", parent, pad, 600f, innerW, 346f);
        Rounded(bgmCard, Color.Lerp(CardBg, Ink, 0.04f));
        Txt(bgmCard.transform, "原声配乐与曲目选择（BGM）", 28, Ink, cardPad, 282f, 540f, 44f, TextAnchor.MiddleLeft);
        var bgmToggle = Btn(bgmCard.transform, "BgmSwitch", musicEnabled ? "已开启 ✓" : "已静音", 25, innerW - cardPad - 210f, 272f, 210f, 60f,
            musicEnabled ? Matcha : Paper2, () => { SetBgmEnabled(!musicEnabled); SaveGame(); OpenAudioSettings(); });
        TInk(bgmToggle, musicEnabled ? Color.white : Ink);

        var nowPlaying = Txt(bgmCard.transform, "正在播放：" + CurrentBgmDisplayTitle(), 24, Indigo, cardPad, 224f, cardInnerW, 38f, TextAnchor.MiddleLeft);
        FitSingleLine(nowPlaying, 24);

        var dropBtn = Btn(bgmCard.transform, "BgmDropdownBtn", "", 24, cardPad, 148f, cardInnerW, 66f, Paper2, () => OpenBgmDropdownMenu(parent));
        var dropLabel = Txt(dropBtn.transform, CurrentBgmDropdownLabel(), 24, Ink, 22f, 0f, cardInnerW - 78f, 66f, TextAnchor.MiddleLeft);
        FitSingleLine(dropLabel, 24);
        Txt(dropBtn.transform, "▾", 26, Indigo, cardInnerW - 52f, 0f, 38f, 66f, TextAnchor.MiddleCenter);

        var lyricTxt = Txt(bgmCard.transform, "「" + CurrentBgmChineseLyric() + "」", 22, InkSoft, cardPad, 88f, cardInnerW, 48f, TextAnchor.MiddleLeft);
        lyricTxt.horizontalOverflow = HorizontalWrapMode.Wrap;
        lyricTxt.verticalOverflow = VerticalWrapMode.Truncate;

        BuildVolumeSlider(bgmCard.transform, "BgmVolSlider", cardPad, 26f, cardInnerW, bgmVolume, Matcha, SetBgmVolume);

        var sfxCard = Rect("SfxCard", parent, pad, 326f, innerW, 250f);
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

    void OpenBgmDropdownMenu(Transform settingsPopup)
    {
        if (!settingsPopup) return;
        var existing = settingsPopup.Find("BgmDropdownOverlay");
        if (existing) { Destroy(existing.gameObject); return; }

        var overlay = Rect("BgmDropdownOverlay", settingsPopup, 0f, 0f, 0f, 0f);
        var overlayRt = overlay.GetComponent<RectTransform>();
        overlayRt.anchorMin = Vector2.zero;
        overlayRt.anchorMax = Vector2.one;
        overlayRt.offsetMin = Vector2.zero;
        overlayRt.offsetMax = Vector2.zero;

        var backdropImg = overlay.AddComponent<Image>();
        backdropImg.color = new Color(0f, 0f, 0f, 0.22f);
        var backdropBtn = overlay.AddComponent<Button>();
        backdropBtn.transition = Selectable.Transition.None;
        backdropBtn.onClick.AddListener(() => { PlayCloseSfx(); Destroy(overlay); });

        const float menuX = 68f, menuY = 230f, menuW = 804f, menuH = 510f;
        var menuBox = Rect("BgmDropdownBox", overlay.transform, menuX, menuY, menuW, menuH);
        Rounded(menuBox, CardBg);
        SoftShadow(menuBox);

        var viewport = Rect("Viewport", menuBox.transform, 14f, 14f, menuW - 28f, menuH - 28f);
        viewport.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.001f);
        viewport.AddComponent<RectMask2D>();

        int itemCount = 2 + BgmCatalog.Length;
        const float itemH = 62f, itemGap = 8f;
        float contentH = Mathf.Max(menuH - 28f, itemCount * (itemH + itemGap) + 6f);
        var content = Rect("Content", viewport.transform, 0f, 0f, menuW - 28f, contentH);
        var contentRt = content.GetComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0f, 1f);
        contentRt.anchorMax = new Vector2(1f, 1f);
        contentRt.pivot = new Vector2(0.5f, 1f);
        contentRt.anchoredPosition = Vector2.zero;
        contentRt.sizeDelta = new Vector2(0f, contentH);

        var scroll = menuBox.AddComponent<ScrollRect>();
        scroll.viewport = viewport.GetComponent<RectTransform>();
        scroll.content = contentRt;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 38f;

        void AddDropdownOption(int orderIdx, string label, bool selected, Action onPick)
        {
            float yPos = contentH - 6f - (orderIdx + 1) * itemH - orderIdx * itemGap;
            var opt = Btn(content.transform, "BgmOpt_" + orderIdx, "", 23, 4f, yPos, menuW - 36f, itemH,
                selected ? Indigo : Paper2, () => { Destroy(overlay); onPick?.Invoke(); OpenAudioSettings(); });
            var lbl = Txt(opt.transform, selected ? label + " ✓" : label, 23, selected ? Color.white : Ink,
                20f, 0f, menuW - 76f, itemH, TextAnchor.MiddleLeft);
            FitSingleLine(lbl, 23);
        }

        AddDropdownOption(0, $"随机播放 · 全馆藏轮播（{BgmCatalog.Length} 首）", bgmPlayMode == 0, () => SetBgmPlayMode(0));
        AddDropdownOption(1, "跟随区域 · 随月面 / 月背 / 火星切换", bgmPlayMode == 2, () => SetBgmPlayMode(2));
        for (int i = 0; i < BgmCatalog.Length; i++)
        {
            int idx = i;
            bool isPicked = bgmPlayMode == 1 && selectedBgmTrackIndex == idx;
            AddDropdownOption(2 + i, "单曲 · " + BgmCatalog[i].cnTitle, isPicked, () => SetBgmPlayMode(1, idx));
        }
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
            syncOn ? "[Editor] 测试档同步：开" : "[Editor] 测试档同步：关",
            22, pad, curY, halfBtnW, 74f, syncOn ? Indigo : Paper2, () => {
                bool next = !DevTestSaveSyncEnabled;
                DevTestSaveSyncEnabled = next;
                OpenAudioSettings();
                Toast(next ? "已开启同步：将持续写回仓库测试档" : "已关闭同步：仅读写本机存档");
            });
        TInk(syncBtn, syncOn ? Color.white : Ink);
        var forceSaveBtn = Btn(parent, "ForceDevTestSave", "[Editor] 立即写入测试档", 22,
            pad + halfBtnW + 24f, curY, halfBtnW, 74f, Indigo, () => {
                ForceSaveToBundledTestSave();
                OpenAudioSettings();
            });
        TInk(forceSaveBtn, Color.white);
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
        var clip = high ? toneHi : toneLo;
        if (!clip)
        {
            toneLo = MakeTone(620f, 0.07f);
            toneHi = MakeTone(940f, 0.055f);
            clip = high ? toneHi : toneLo;
        }
        if (sfx && clip && sfxEnabled && sfxVolume > 0.001f)
            sfx.PlayOneShot(clip, sfxVolume);
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
                    Toast("尘暴应急运营补贴 +¥" + Mathf.RoundToInt(cycleSub));
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
        string pressure = demand > cap ? "星港候航爆满（" + Mathf.RoundToInt(demand / Mathf.Max(1f, cap) * 100f) + "%），建议扩建升级" : "星港负荷 " + Mathf.RoundToInt(demand / Mathf.Max(1f, cap) * 100f) + "%（运力宽裕）";
        string diag = StationFlowDiagnostics(index);
        return state + (lineNames.Length > 0 ? "\n途经：" + lineNames : "") + "\n日均需求 " + StationEstimatedDemand(index) + " 人 · 当前输送 " + Mathf.RoundToInt(served) + " / 需求 " + Mathf.RoundToInt(demand) + "\n" +
            "港内容量 " + Mathf.RoundToInt(cap) + " · " + pressure + "\n" +
            "星港场动态：" + TrafficEventNames[station.trafficEvent] + "（需求 " +
            (TrafficEventFactors[station.trafficEvent] >= 1 ? "+" : "") + Mathf.RoundToInt((TrafficEventFactors[station.trafficEvent]-1)*100) + "%）\n" +
            StationAreaHint(index) + (string.IsNullOrEmpty(diag) ? "" : "\n" + diag);
    }
    bool OpenStation(int index)
    {
        var station = stations[index];
        int cost = StationOpeningCost(index);
        if (coins < cost) { Toast("金币不足：还差 ¥" + FormatShortfall(cost - coins)); return false; }
        if (station.built && !station.closed) return true;
        BeginUndo(station.built ? "重新开港" : "开港");
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
        int kind = StationKind(index);
        string prefix = "";
        switch (station.name)
        {
            case "静海基地": prefix = "月球正面最早建立的氦-3 交易与联合体调度中心。"; break;
            case "阿里亚德港": prefix = "静海西缘的金融与精密器件贸易枢纽。"; break;
            case "澄海枢纽": prefix = "月面东北环线的能源精炼与商贸中心。"; break;
            case "哥白尼环形山": prefix = "中央峰矿业总部与轨道科研办公集群所在地。"; break;
            case "第谷环港": prefix = "南半球高地最大的资源集散与科研商务港。"; break;
            case "冯·卡门港": prefix = "月背南极-艾特肯盆地核心科研与氦-3 商业中心。"; break;
            case "莫斯科海": prefix = "月背北部的玄武岩工业与深空探测指挥枢纽。"; break;
            case "艾特肯深井": prefix = "深入月幔矿层的重工业与深空补给办公区。"; break;
            case "奥林匹斯星门": prefix = "高耸于塔尔西斯高原的火星商贸核心，唯一连通月之亮面的超级星门。"; break;
            case "柏拉图环": prefix = "北雨海边缘的黑曜台高地科研与矿业星港。"; break;
            case "虹湾星门": prefix = "【三域超级枢纽】贯穿月之亮面、月背与火星·西半球的星海大枢。"; break;
            case "达朗贝尔环": prefix = "月背北极高地的深空探测前哨站。"; break;
            case "阿波罗星门": prefix = "【三域超级枢纽】连接月背、火星·西半球与东半球的南疆星枢。"; break;
            case "艾斯克雷尔斯": prefix = "塔尔西斯三大巨型火山北峰的氦-3 精炼与商贸枢纽。"; break;
            case "科普剌塔斯": prefix = "水手谷东口最重要的工业加工与联合体商业分部。"; break;
            case "米拉斯深谷": prefix = "水手大峡谷中段最繁华的商业采矿与轨道办公枢纽。"; break;
            case "克律塞平原": prefix = "火星北部平原的太空电梯基座与跨星区贸易中心。"; break;
            case "开普勒港": case "云星港": case "叙利亚高原": case "阿尔卡迪亚": case "加加林环":
                prefix = "大型生态穹顶生活圈，每日清晨向核心枢纽输送稳定通勤客流。"; break;
        }
        string roleDesc;
        switch (kind)
        {
            case 1:
                roleDesc = "【居住穹顶大站】密集的开拓者生态居住区，早高峰向外输送大量通勤客流、晚间集中归巢，适合直连商业核心站。";
                break;
            case 2:
                roleDesc = "【商业核心大站】汇集联合体总部、能源交易与办公区，早高峰海量通勤客流涌入、晚高峰集中返程，高运力干线必连。";
                break;
            case 3:
                roleDesc = "【星际奇观景点】毗邻罕见的星球地貌奇观，白天观光与科研访客源源不断，接入观光航线可大幅提升收益。";
                break;
            case 4:
                roleDesc = "【跨星域门户】横跨地月与火星航道的超空间接驳枢纽，多条星轨在此交会可触发高额跨区联运加成。";
                break;
            default:
                roleDesc = !string.IsNullOrEmpty(station.city)
                    ? "【" + station.city + "中继港】承担星区日常通勤与物资接驳，客流稳定，适合编入支线或环线串联。"
                    : "【星港中继站】周边为常规补给与科研聚落，客流均衡，适合连接邻近大站实现客流互补。";
                break;
        }
        string flavor = GetStationFlavorSnippet(station);
        string baseIntro = string.IsNullOrEmpty(prefix) ? roleDesc : prefix + "\n" + roleDesc;
        return string.IsNullOrEmpty(flavor) ? baseIntro : baseIntro + "\n" + flavor;
    }

    void ShowStationActions(int index)
    {
        if (index < 0 || index >= stations.Count || pageRoot || sceneryViewer) return;
        if (IsInEventSandbox())
        {
            ShowStationOdArcs(index);
            var evtSt = GetOrCreateEventSandbox(activeSandboxEventId);
            var spec = GetEventSpec(activeSandboxEventId);
            var stationDef = GetSandboxStationDefs(activeSandboxEventId)[index];
            var evtStation = stations[index];
            var connected = new List<string>();
            foreach (var line in lines) if (line.st.Contains(index)) connected.Add(line.name);
            string tag = stationDef.isSpecialB ? "禁区" : stationDef.isSpecialA ? (activeSandboxEventId == EventIdAshDome ? "避风港" : "换乘热点") : "区域 " + stationDef.tierOrKind;
            if (activeSandboxEventId == EventIdAshDome && !stationDef.isSpecialA && !stationDef.isSpecialB)
                tag += "  " + (ashFallenStations.Contains(index) ? "已封控失联" : evtSt.stationPop[index] == 0 ? "已全部撤空" : $"待撤离人数：{evtSt.stationPop[index]} 人");
            int cost = 2 + evtStation.level;
            Vector2 sandboxAnchor = root.InverseTransformPoint(mapRect.TransformPoint(Pos(index)));
            var panel = OpenBubble(evtStation.name, 420, 548, sandboxAnchor);
            Txt(panel.transform, tag + "  等级 " + evtStation.level + " / 5", 25, Ink, 28, 320, 492, 40, TextAnchor.MiddleLeft);
            var routes = Txt(panel.transform, "连接线路：" + (connected.Count > 0 ? string.Join("、", connected) : "暂无"), 21, InkSoft, 28, 245, 492, 70, TextAnchor.UpperLeft);
            routes.horizontalOverflow = HorizontalWrapMode.Wrap;
            Txt(panel.transform, $"当前{spec.tokenName}：{evtSt.tokens}", 22, Indigo, 28, 205, 492, 36, TextAnchor.MiddleLeft);
            if (AshCycleCompleted()) return;
            TInk(Btn(panel.transform, "UpgradeStation", evtStation.level >= 5 ? "已满级" : $"升级  {cost} {spec.tokenName}", 23, 28, 112, 492, 74, Matcha, () => {
                if (evtStation.level >= 5) { Toast("站点已满级"); return; }
                if (evtSt.tokens < cost) { Toast(spec.tokenName + "不足"); return; }
                evtSt.tokens -= cost; evtStation.level++;
                if (evtSt.stationLevels == null) evtSt.stationLevels = new List<int>();
                while (evtSt.stationLevels.Count < stations.Count) evtSt.stationLevels.Add(1);
                evtSt.stationLevels[index] = evtStation.level;
                RefreshSandboxHudText(); SaveSandboxStorage(); ClosePanel(true); ShowStationActions(index);
            }), Color.white);
            if (connected.Count > 0) TInk(Btn(panel.transform, "RemoveWaystation", "移除航线途经站", 23, 28, 24, 492, 74, Paper2, () => OpenRemoveStationLines(index, 0)), Ink);
            return;
        }
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
            float topY = Mathf.Max(safeB + 180f + halfH, RootH - safeT - 315f - halfH);
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
        Txt(cashCard.transform, "星票款 ¥" + Mathf.RoundToInt(rep.gross) + "  ·  物业/联运 ¥" + Mathf.RoundToInt(rep.commercialRent) + "  ·  运维税费 -¥" + Mathf.RoundToInt(rep.maintAndTax), 22, InkSoft, 24f, 8f, 816f, 34f, TextAnchor.MiddleLeft);
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
        float textH = Mathf.Max(80f, MeasureTextHeight(story.text, 25, 932f) + 14f);
        float objH = hasObj ? Mathf.Max(58f, MeasureTextHeight(story.objectiveHint, 24, 896f) + 22f) : 0f;
        float objY = 108f;
        float textY = hasObj ? (objY + objH + 16f) : 108f;
        float cardH = textY + textH + 124f;
        storyCard = Rect("StoryCard", root, 40f, 204f, 1000f, cardH);
        Rounded(storyCard, new Color(CardBg.r, CardBg.g, CardBg.b, 0.985f)); SoftShadow(storyCard);
        JournalTape(storyCard.transform, 428f, cardH - 16f, 144f);
        var accent = Img(Rect("Accent", storyCard.transform, 0f, 20f, 8f, cardH - 40f), Gold, spriteRound); accent.raycastTarget = false;

        string headerLine = string.IsNullOrEmpty(story.chapterTag) ? "沿途故事" : story.chapterTag;
        if (!string.IsNullOrEmpty(story.speaker)) headerLine += "  ·  " + story.speaker;
        Txt(storyCard.transform, headerLine, 22, Matcha, 34f, cardH - 52f, 700f, 34f, TextAnchor.MiddleLeft);
        Txt(storyCard.transform, story.title, 36, Ink, 34f, cardH - 106f, 920f, 50f, TextAnchor.MiddleLeft);
        Txt(storyCard.transform, story.text, 25, Ink, 34f, textY, 932f, textH, TextAnchor.UpperLeft);

        if (hasObj)
        {
            var objBox = Rect("StoryObjective", storyCard.transform, 34f, objY, 932f, objH);
            Rounded(objBox, Color.Lerp(CardBg, Indigo, 0.08f));
            Txt(objBox.transform, story.objectiveHint, 24, Indigo, 18f, 0f, 896f, objH, TextAnchor.MiddleLeft);
        }

        if (story.allowSkip)
        {
            var skipBtn = Btn(storyCard.transform, "SkipGuide", "跳过星际航行引导", 24, 34f, 22f, 246f, 70f, Paper2, () => {
                Confirm("跳过星际航行引导", "将直接解锁全部系统并进入自由经营，稍后仍可在设置中重温剧情。确认跳过？", SkipTutorialFlow);
            });
            TInk(skipBtn, InkSoft);
        }

        var close = Btn(storyCard.transform, "Continue", "继续运营", 25, 708f, 22f, 226f, 68f, Matcha, () => {
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
        Vector2 anchorStart = activeSegWaypoints.Count > 0 ? activeSegWaypoints[activeSegWaypoints.Count - 1] : current;
        scratchControlPts.Clear();
        scratchControlPts.Add(current);
        for (int i = 0; i < activeSegWaypoints.Count; i++) scratchControlPts.Add(activeSegWaypoints[i]);
        bool autoOk = true;
        if (autoRouteEnabled && Vector2.Distance(anchorStart, point) >= 48f)
        {
            autoOk = GetCachedAutoRouteWaypoints(anchorStart, point, scratchAutoWaypoints);
            for (int i = 0; i < scratchAutoWaypoints.Count; i++) scratchControlPts.Add(scratchAutoWaypoints[i]);
        }
        scratchControlPts.Add(point);
        TerrainField.SampleCatmullRomPolyline(scratchControlPts, false, scratchPolyline);
        int previewCost = EvaluatePolylineTrackCost(currentRegion, scratchPolyline, TrackBuildCostFactor(), out var previewTr);
        bool blockedNow = !autoOk || (previewTr.blocked && !autoRouteEnabled);
        bool preview = !IsLoop(draft) && (previewTr.distance > 24f || activeSegWaypoints.Count > 0);
        int value = draftCost + (preview ? previewCost : 0);
        if (draftCost != dragCostLastCommitted) { dragCostLastCommitted = draftCost; dragCostPulse = 0.22f; }
        dragCostDisplayed = Mathf.Lerp(dragCostDisplayed, value, 1f - Mathf.Exp(-18f * dt));
        if (Mathf.Abs(dragCostDisplayed-value) < 0.5f) dragCostDisplayed = value;
        if (preview && blockedNow)
        {
            dragCostHeading.text = "禁建天堑 · 请绕行";
            dragCostNumber.text = "禁止穿越";
            dragCostNumber.color = Vermilion;
        }
        else
        {
            string terrainTag = preview && previewTr.crossedHazard
                ? $" (跨天堑+{Mathf.RoundToInt(previewTr.penaltyRatio * 100f)}%)"
                : (preview && previewTr.penaltyRatio >= 0.08f
                    ? $" (含地形+{Mathf.RoundToInt(previewTr.penaltyRatio * 100f)}%)"
                    : "");
            dragCostHeading.text = preview
                ? (draftPreviewNeed >= 0.72f ? "高需走廊" + terrainTag : draftPreviewNeed >= 0.40f ? "适中需求" + terrainTag : "客需较平" + terrainTag)
                : "已选造价";
            dragCostNumber.text = "¥" + Mathf.RoundToInt(dragCostDisplayed);
            dragCostNumber.color = value > coins ? Vermilion : Ink;
        }
        if (IsInEventSandbox()) { dragCostHeading.text = "街道线路"; dragCostNumber.text = draft.Count + " 站  沿街铺线"; dragCostNumber.color = Ink; }
        if (dragCostSubText)
        {
            if (IsInEventSandbox()) { dragCostSubText.text = "拖向站点连接线路"; dragCostSubText.color = InkSoft; }
            else if (hoverStation != dragCostLastHov || draft.Count != dragCostLastDraftCount)
            {
                dragCostLastHov = hoverStation;
                dragCostLastDraftCount = draft.Count;
                dragCostAddedPassengers = hoverStation >= 0 ? EstimateDraftAddedPassengers(draft, hoverStation) : 0;
            }
            if (IsInEventSandbox()) { }
            else if (preview && blockedNow)
            {
                dragCostSubText.text = "右键 / 空格在点阵落拐点绕行";
                dragCostSubText.color = Vermilion;
            }
            else if (hoverStation >= 0 && dragCostAddedPassengers > 0)
            {
                dragCostSubText.text = "预计增客 +" + dragCostAddedPassengers + " 人/日";
                dragCostSubText.color = Matcha;
            }
            else if (preview)
            {
                dragCostSubText.text = activeSegWaypoints.Count > 0
                    ? $"已落 {activeSegWaypoints.Count} 个拐点（Backspace 撤销）"
                    : (autoRouteEnabled && scratchAutoWaypoints.Count > 0
                        ? "已自动规划最近可行绕行路线"
                        : "拖向星港，右键/空格可吸附点阵落拐点");
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
        dt = Mathf.Min(dt, 0.05f);
        if (!pressing) return;
        if (!Input.GetMouseButton(0))
        {
            pressing = false; pressIdx = -1;
            HidePressRing();
            return;
        }
        if (Vector2.Distance((Vector2)Input.mousePosition, pressStart) > 26f && pressTimer < 0.42f)
        {
            if (pressTimer >= 0.14f) ShowConnectHint("长按星港蓄满进度环后再拖拽即可开线");
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
        // 航线已满时，若起手港既不在任何非环线端点又无法新开线，直接给出清晰指引
        bool onLine = false;
        bool hasExtendableEnd = false;
        foreach (var line in lines)
        {
            if (!line.st.Contains(idx)) continue;
            onLine = true;
            if (!IsLoop(line.st) && (line.st[0] == idx || line.st[line.st.Count - 1] == idx))
            {
                hasExtendableEnd = true;
                break;
            }
        }
        if (RegionLineCount(currentRegion) >= LineLimit())
        {
            QueueGuideBubble("G-06");
            if (!onLine)
            {
                HidePressRing();
                Toast(currentRegion + "航线已达上限（" + LineLimit() + " 条），请从既有航线两端连接试试");
                return;
            }
            if (!hasExtendableEnd)
            {
                HidePressRing();
                Toast("该星港所在航线已闭环或为途经站，请从航线另一端连接试试");
                return;
            }
        }
        CloseLineBubble();
        ClearDraftWaypoints();
        draft.Clear(); draftSegNeeds.Clear(); draft.Add(idx); draftCost = DraftCostOf(draft);
        PrepareDraftNeedField(idx, draft);
        draftPreviewNeed = EvaluateDragDirectionNeed(idx, Pos(idx));
        draftPreviewColor = NeedColorGradient(draftPreviewNeed);
        dragFrom = idx; dragging = true; hoverStation = -1;
        dragCostDisplayed = 0; dragCostLastCommitted = 0; dragCostPulse = 0;
        if (dragCostBadge) { dragCostBadge.SetActive(true); UpdateDragCostBadge(Pos(idx), Pos(idx), 1f); }
        RedrawDraft();
        if (previewImg) { previewImg.color = draftPreviewColor; previewImg.enabled = false; previewImg.gameObject.SetActive(true); }
        if (dragTip) { dragTip.color = draftPreviewColor; dragTip.gameObject.SetActive(true); }
        SetDotGridHighlight(true);
        UpdateRings();
        Haptic.Vibrate(22); PlayTone(false);
    }
    void EndDraw()
    {
        if (!dragging) return;
        dragging = false; hoverStation = -1;
        SetDotGridHighlight(false);
        ClearAutoPreviewSegs();
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
        SetDotGridHighlight(false);
        ClearDraftWaypoints();
        if (dragCostBadge) dragCostBadge.SetActive(false);
        ClearDraftSegs(); ResetBuildVisuals();
    }
}
