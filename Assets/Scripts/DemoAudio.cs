using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public partial class DemoBootstrap
{
    // ---------- M1: 区域 BGM 交叉淡化与全场景音效引擎 ----------
    const string PrefBgmVol = "bgm_volume";
    const string PrefSfxVol = "sfx_volume";
    const string PrefSfxOn = "sfx_enabled";
    const string PrefHapticOn = "haptic_enabled";
    const string PrefBgmOn = "bgm_enabled";
    const string PrefBgmMode = "bgm_mode";
    const string PrefBgmTrack = "bgm_track_index";
    const string ClassicBgmResource = "Audio/Between_the_Bow_and_Breath";

    public readonly struct BgmTrackInfo
    {
        public readonly string resPath;
        public readonly string clipName;
        public readonly string cnTitle;
        public readonly string cnLyric;

        public BgmTrackInfo(string resPath, string clipName, string cnTitle, string cnLyric)
        {
            this.resPath = resPath;
            this.clipName = clipName;
            this.cnTitle = cnTitle;
            this.cnLyric = cnLyric;
        }
    }

    static readonly BgmTrackInfo[] BgmCatalog =
    {
        new BgmTrackInfo("Audio/Between_the_Bow_and_Breath", "Between_the_Bow_and_Breath", "《弓弦与呼吸之间》（经典长曲）", "琴弓轻落，弦音随呼吸起伏，记录每一段缓慢而温柔的星海时光。"),
        new BgmTrackInfo("Audio/bgm_moon_near", "bgm_moon_near", "《月之亮面茶室晨曦》", "晨光漫过亮面穹顶，茶烟袅袅，笔尖在手帐上写下第一条月面航线。"),
        new BgmTrackInfo("Audio/bgm_moon_far", "bgm_moon_far", "《月背石板微风》", "银色月辉落满古老石板，微风掠过静默环山，星辰在远方悄然守望。"),
        new BgmTrackInfo("Audio/bgm_mars", "bgm_mars", "《火星红砂星海》", "红色沙丘绵延向天际，铁轨穿过风沙与星海，载着开拓者的期盼前行。"),
        new BgmTrackInfo("Audio/bgm_morning_light_moon", "bgm_morning_light_moon", "《月面晨光》", "微光苏醒在环形山巅，柔和琴键唤醒沉睡的星港与早班列车。"),
        new BgmTrackInfo("Audio/bgm_silver_courtyard", "bgm_silver_courtyard", "《银庭独步》", "独自漫步于银白静谧的庭院，只听见心底回响的宇宙低语。"),
        new BgmTrackInfo("Audio/bgm_martian_sunset", "bgm_martian_sunset", "《火星铁道落日》", "暮色将红砂染成琥珀，远航列车沿着落日余晖缓缓驶向下一座港湾。"),
        new BgmTrackInfo("Audio/bgm_celestial_voyage", "bgm_celestial_voyage", "《星汉漫歌》", "跨越星汉与陨石带，漫天繁星为每一段旅途轻轻唱和。"),
        new BgmTrackInfo("Audio/bgm_archive_memories", "bgm_archive_memories", "《记忆档案馆》", "一页页泛黄的营运手记与车票，珍藏着岁月与相遇的温度。"),
        new BgmTrackInfo("Audio/bgm_felt_woodwinds", "bgm_felt_woodwinds", "《毡音木管协奏》", "温暖毡音钢琴与清澈木管交织，像午后茶室里最安宁的一场重逢。"),
        new BgmTrackInfo("Audio/bgm_midnight_orbital", "bgm_midnight_orbital", "《夜轨星茫》", "午夜轨道列车穿行在无垠星茫之中，窗外灯火点亮深邃夜空。"),
        new BgmTrackInfo("Audio/bgm_midnight_orbital_lofi", "bgm_midnight_orbital_lofi", "《星茫夜轨 · Lo-Fi 版》", "黑胶微噪伴着温柔节拍，轨道列车在午夜星海里不紧不慢地摇晃前行。"),
        new BgmTrackInfo("Audio/bgm_pipa_classical_cn", "bgm_pipa_classical_cn", "《星夜琵琶 · 国风雅韵》", "琵琶清弹落玉盘，山水余韵伴着月色与星轨缓缓流淌。")
    };

    AudioSource musicB;
    readonly Dictionary<string, AudioClip> regionalBgmCache = new Dictionary<string, AudioClip>();
    readonly Dictionary<string, float> bgmSavedPositions = new Dictionary<string, float>();
    string activeBgmTrackKey = "";
    string currentRandomBgmRes = "";
    bool bgmPausedByToggle;
    float bgmFadeProgress = 1f;
    float bgmCrossfadeDuration = 1.4f;

    float bgmVolume = 0.34f;
    float sfxVolume = 0.72f;
    bool sfxEnabled = true;
    bool hapticEnabled = true;
    int bgmPlayMode = 0; // 0: 随机播放, 1: 指定单曲循环, 2: 跟随区域切换
    int selectedBgmTrackIndex = 0;
    int settingsTab = 0;
    bool suppressNextBtnClickSfx;

    AudioClip sfxClickClip, sfxSoftTickClip, sfxBubbleOpenClip, sfxPageOpenClip, sfxCloseClip;
    AudioClip sfxSnapClip, sfxBuildClip, sfxCoinClip, sfxTechClip, sfxErrorClip, sfxStoryClip;
    AudioClip sfxPaperFlipClip, sfxWhistleClip;
    static readonly Dictionary<string, bool> customBgmAvailability = new Dictionary<string, bool>();

    static string RegionBgmResource(string region)
    {
        if (region == "月背") return "Audio/bgm_moon_far";
        if (IsMarsRegion(region)) return "Audio/bgm_mars";
        return "Audio/bgm_moon_near";
    }

    static int FindBgmCatalogIndexByClipName(string clipName)
    {
        for (int i = 0; i < BgmCatalog.Length; i++)
        {
            if (BgmCatalog[i].clipName == clipName) return i;
        }
        return 0;
    }

    static string RegionShortBgmTitle(string region)
    {
        if (region == "月背") return HasCustomBgm("月背") ? "《月背石板微风》" : BgmCatalog[0].cnTitle;
        if (IsMarsRegion(region)) return HasCustomBgm(region) ? "《火星红砂星海》" : BgmCatalog[0].cnTitle;
        return HasCustomBgm("月之亮面") ? "《月之亮面茶室晨曦》" : BgmCatalog[0].cnTitle;
    }

    static string BgmTitleByResourceName(string clipName)
        => BgmCatalog[FindBgmCatalogIndexByClipName(clipName)].cnTitle;

    static string BgmLyricByResourceName(string clipName)
        => BgmCatalog[FindBgmCatalogIndexByClipName(clipName)].cnLyric;

    string CurrentBgmClipName()
    {
        if (music && music.clip) return music.clip.name;
        if (bgmPlayMode == 1)
            return BgmCatalog[Mathf.Clamp(selectedBgmTrackIndex, 0, BgmCatalog.Length - 1)].clipName;
        if (bgmPlayMode == 0 && !string.IsNullOrEmpty(currentRandomBgmRes))
        {
            for (int i = 0; i < BgmCatalog.Length; i++)
                if (BgmCatalog[i].resPath == currentRandomBgmRes) return BgmCatalog[i].clipName;
        }
        if (bgmPlayMode == 2)
        {
            if (currentRegion == "月背") return "bgm_moon_far";
            if (IsMarsRegion(currentRegion)) return "bgm_mars";
            return "bgm_moon_near";
        }
        return BgmCatalog[0].clipName;
    }

    string CurrentBgmDisplayTitle()
    {
        if (bgmPlayMode == 1)
            return BgmCatalog[Mathf.Clamp(selectedBgmTrackIndex, 0, BgmCatalog.Length - 1)].cnTitle;
        if (bgmPlayMode == 0)
            return BgmTitleByResourceName(CurrentBgmClipName());
        return RegionShortBgmTitle(currentRegion);
    }

    string CurrentBgmChineseLyric() => BgmLyricByResourceName(CurrentBgmClipName());

    string CurrentBgmDropdownLabel()
    {
        if (bgmPlayMode == 0) return "随机播放 · 全馆藏轮播";
        if (bgmPlayMode == 2) return "跟随区域 · 随月面/火星切换";
        return "单曲循环 · " + BgmCatalog[Mathf.Clamp(selectedBgmTrackIndex, 0, BgmCatalog.Length - 1)].cnTitle;
    }

    static bool HasCustomBgm(string region)
    {
        if (string.IsNullOrEmpty(region)) return false;
        if (customBgmAvailability.TryGetValue(region, out bool avail)) return avail;
        avail = Resources.Load<AudioClip>(RegionBgmResource(region)) != null;
        customBgmAvailability[region] = avail;
        return avail;
    }

    void InitAudioPreferences()
    {
        bgmVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(PrefBgmVol, 0.34f));
        sfxVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(PrefSfxVol, 0.72f));
        sfxEnabled = PlayerPrefs.GetInt(PrefSfxOn, 1) != 0;
        hapticEnabled = PlayerPrefs.GetInt(PrefHapticOn, 1) != 0;
        bgmPlayMode = Mathf.Clamp(PlayerPrefs.GetInt(PrefBgmMode, 0), 0, 2);
        selectedBgmTrackIndex = Mathf.Clamp(PlayerPrefs.GetInt(PrefBgmTrack, 0), 0, BgmCatalog.Length - 1);
        if (PlayerPrefs.HasKey(PrefBgmOn))
            musicEnabled = PlayerPrefs.GetInt(PrefBgmOn, 1) != 0;
    }

    void SaveAudioPreferences()
    {
        PlayerPrefs.SetFloat(PrefBgmVol, bgmVolume);
        PlayerPrefs.SetFloat(PrefSfxVol, sfxVolume);
        PlayerPrefs.SetInt(PrefSfxOn, sfxEnabled ? 1 : 0);
        PlayerPrefs.SetInt(PrefHapticOn, hapticEnabled ? 1 : 0);
        PlayerPrefs.SetInt(PrefBgmMode, bgmPlayMode);
        PlayerPrefs.SetInt(PrefBgmTrack, selectedBgmTrackIndex);
        PlayerPrefs.SetInt(PrefBgmOn, musicEnabled ? 1 : 0);
        PlayerPrefs.Save();
    }

    AudioClip LoadBgmClip(string resPath)
    {
        if (string.IsNullOrEmpty(resPath)) resPath = ClassicBgmResource;
        if (regionalBgmCache.TryGetValue(resPath, out var cached) && cached) return cached;
        var clip = Resources.Load<AudioClip>(resPath);
        if (!clip && resPath != ClassicBgmResource)
            clip = Resources.Load<AudioClip>(ClassicBgmResource);
        if (clip) regionalBgmCache[resPath] = clip;
        return clip;
    }

    string PickNextRandomBgmResource(string excludeRes)
    {
        var candidates = new List<string>(BgmCatalog.Length);
        var loadedClips = new HashSet<AudioClip>();
        for (int i = 0; i < BgmCatalog.Length; i++)
        {
            string res = BgmCatalog[i].resPath;
            var clip = Resources.Load<AudioClip>(res);
            if (!clip || loadedClips.Contains(clip)) continue;
            loadedClips.Add(clip);
            regionalBgmCache[res] = clip;
            if (res != excludeRes) candidates.Add(res);
        }
        if (candidates.Count > 0)
            return candidates[UnityEngine.Random.Range(0, candidates.Count)];
        return !string.IsNullOrEmpty(excludeRes) && regionalBgmCache.ContainsKey(excludeRes)
            ? excludeRes
            : ClassicBgmResource;
    }

    AudioClip ResolveTargetBgm(out string targetKey)
    {
        string resPath;
        if (bgmPlayMode == 0)
        {
            if (string.IsNullOrEmpty(currentRandomBgmRes))
                currentRandomBgmRes = PickNextRandomBgmResource("");
            resPath = currentRandomBgmRes;
        }
        else if (bgmPlayMode == 1)
        {
            int idx = Mathf.Clamp(selectedBgmTrackIndex, 0, BgmCatalog.Length - 1);
            resPath = BgmCatalog[idx].resPath;
        }
        else
        {
            resPath = RegionBgmResource(currentRegion);
        }
        AudioClip clip = LoadBgmClip(resPath);
        if (!clip) { targetKey = ""; return null; }
        if (bgmPlayMode == 0)
        {
            targetKey = "random_" + clip.name;
            return clip;
        }
        targetKey = bgmPlayMode == 1 ? ("track_" + clip.name) : (clip.name + "_" + currentRegion);
        return clip;
    }

    void InitAudioEngine()
    {
        InitAudioPreferences();

        var sfxGo = new GameObject("Sfx", typeof(AudioSource));
        sfxGo.transform.SetParent(transform, false);
        sfx = sfxGo.GetComponent<AudioSource>();
        sfx.playOnAwake = false;

        var musicGoA = new GameObject("BackgroundMusicA", typeof(AudioSource));
        musicGoA.transform.SetParent(transform, false);
        music = musicGoA.GetComponent<AudioSource>();
        music.loop = true; music.playOnAwake = false; music.volume = 0f;

        var musicGoB = new GameObject("BackgroundMusicB", typeof(AudioSource));
        musicGoB.transform.SetParent(transform, false);
        musicB = musicGoB.GetComponent<AudioSource>();
        musicB.loop = true; musicB.playOnAwake = false; musicB.volume = 0f;

        toneLo = MakeTone(620f, 0.07f);
        toneHi = MakeTone(940f, 0.055f);
        BuildProceduralSfxBank();

        SyncRegionalMusic(true);
    }

    void SyncRegionalMusic(bool immediate = false)
    {
        if (!music) return;
        if (!musicEnabled || bgmVolume <= 0.001f)
        {
            bgmPausedByToggle = true;
            if (music.isPlaying) music.Pause();
            if (musicB && musicB.isPlaying) musicB.Pause();
            return;
        }

        bgmPausedByToggle = false;
        bool isRandomMode = bgmPlayMode == 0;
        music.loop = !isRandomMode;
        if (musicB) musicB.loop = !isRandomMode;

        AudioClip target = ResolveTargetBgm(out string targetKey);
        if (!target) return;

        if (music.clip == target)
        {
            if (!music.isPlaying) music.Play();
            if (musicB && musicB.isPlaying && immediate) musicB.Stop();
            if (immediate || bgmFadeProgress >= 1f) music.volume = bgmVolume;
            activeBgmTrackKey = targetKey;
            return;
        }

        if (music.clip && !string.IsNullOrEmpty(activeBgmTrackKey) && !activeBgmTrackKey.StartsWith("random_"))
            bgmSavedPositions[activeBgmTrackKey] = music.time;

        float resumePos = !isRandomMode && bgmSavedPositions.TryGetValue(targetKey, out float savedTime) && savedTime < target.length - 0.5f
            ? Mathf.Max(0f, savedTime)
            : 0f;

        if (immediate || !music.isPlaying || !musicB)
        {
            music.clip = target;
            music.time = resumePos;
            music.volume = bgmVolume;
            music.Play();
            if (musicB && musicB.isPlaying) musicB.Stop();
            bgmFadeProgress = 1f;
            activeBgmTrackKey = targetKey;
            return;
        }

        // 双音轨平滑交叉淡化（非随机模式延续曲目进度）
        musicB.clip = music.clip;
        musicB.time = music.time < (musicB.clip ? musicB.clip.length : 0f) ? music.time : 0f;
        musicB.volume = music.volume;
        musicB.Play();

        music.clip = target;
        music.time = resumePos;
        music.volume = 0f;
        music.Play();
        bgmFadeProgress = 0f;
        activeBgmTrackKey = targetKey;
    }

    void TickAudioCrossfade(float dt)
    {
        if (!music) return;
        if (!musicEnabled || bgmVolume <= 0.001f)
        {
            bgmPausedByToggle = true;
            if (music.isPlaying) music.Pause();
            if (musicB && musicB.isPlaying) musicB.Pause();
            return;
        }

        if (bgmFadeProgress < 1f)
        {
            bgmFadeProgress = Mathf.Min(1f, bgmFadeProgress + dt / Mathf.Max(0.2f, bgmCrossfadeDuration));
            float smooth = bgmFadeProgress * bgmFadeProgress * (3f - 2f * bgmFadeProgress);
            music.volume = bgmVolume * smooth;
            if (musicB && musicB.isPlaying)
            {
                musicB.volume = bgmVolume * (1f - smooth);
                if (bgmFadeProgress >= 1f) musicB.Stop();
            }
        }
        else if (music.isPlaying)
        {
            music.volume = bgmVolume;
        }
        else if (bgmPlayMode == 0 && !bgmPausedByToggle && music.clip != null && Application.isFocused)
        {
            // 随机播放模式下当前曲目播完，自动切到下一首不同曲目
            string nextRes = PickNextRandomBgmResource(currentRandomBgmRes);
            if (nextRes == currentRandomBgmRes)
            {
                music.time = 0f;
                music.volume = bgmVolume;
                music.Play();
            }
            else
            {
                currentRandomBgmRes = nextRes;
                SyncRegionalMusic(false);
            }
        }
    }

    void SetBgmEnabled(bool enabled)
    {
        musicEnabled = enabled;
        SaveAudioPreferences();
        SyncRegionalMusic(true);
    }

    void SetBgmPlayMode(int mode, int trackIndex = -1)
    {
        int clamped = Mathf.Clamp(mode, 0, 2);
        if (clamped == 0)
            currentRandomBgmRes = PickNextRandomBgmResource(currentRandomBgmRes);
        else if (clamped == 1 && trackIndex >= 0)
            selectedBgmTrackIndex = Mathf.Clamp(trackIndex, 0, BgmCatalog.Length - 1);
        bgmPlayMode = clamped;
        SaveAudioPreferences();
        SyncRegionalMusic(false);
    }

    void SetBgmVolume(float vol)
    {
        bgmVolume = Mathf.Clamp01(vol);
        if (bgmVolume > 0.01f && !musicEnabled) musicEnabled = true;
        SaveAudioPreferences();
        if (music && musicEnabled)
        {
            if (!music.isPlaying && music.clip) music.Play();
            music.volume = bgmVolume * Mathf.Clamp01(bgmFadeProgress);
        }
    }

    void SetSfxEnabled(bool enabled)
    {
        sfxEnabled = enabled;
        SaveAudioPreferences();
    }

    void SetSfxVolume(float vol)
    {
        sfxVolume = Mathf.Clamp01(vol);
        if (sfxVolume > 0.01f && !sfxEnabled) sfxEnabled = true;
        SaveAudioPreferences();
    }

    void SetHapticEnabled(bool enabled)
    {
        hapticEnabled = enabled;
        SaveAudioPreferences();
    }

    void TriggerHaptic(int ms)
    {
        if (hapticEnabled) Haptic.Vibrate(ms);
    }

    // ---------- 音色合成库 ----------
    void BuildProceduralSfxBank()
    {
        sfxClickClip = SynthChord("sfx_click", 0.048f, new[] { 780f, 1560f }, new[] { 0.22f, 0.07f }, 38f);
        sfxSoftTickClip = SynthChord("sfx_tick", 0.036f, new[] { 980f }, new[] { 0.14f }, 48f);
        sfxBubbleOpenClip = SynthArp("sfx_bubble", 0.055f, new[] { 523.25f, 659.25f }, 0.20f);
        sfxPageOpenClip = SynthArp("sfx_page", 0.068f, new[] { 440f, 554.37f, 659.25f }, 0.18f);
        sfxCloseClip = SynthChord("sfx_close", 0.055f, new[] { 460f, 345f }, new[] { 0.16f, 0.10f }, 32f);
        sfxSnapClip = SynthChord("sfx_snap", 0.052f, new[] { 880f, 1320f }, new[] { 0.22f, 0.09f }, 42f);
        sfxBuildClip = SynthArp("sfx_build", 0.11f, new[] { 392f, 493.88f, 587.33f, 783.99f }, 0.24f);
        sfxCoinClip = SynthArp("sfx_coin", 0.065f, new[] { 1046.5f, 1318.5f, 1567.98f }, 0.19f);
        sfxTechClip = SynthArp("sfx_tech", 0.09f, new[] { 523.25f, 659.25f, 783.99f, 1046.5f }, 0.22f);
        sfxErrorClip = SynthChord("sfx_error", 0.12f, new[] { 220f, 207.65f }, new[] { 0.24f, 0.18f }, 18f);
        sfxStoryClip = SynthArp("sfx_story", 0.095f, new[] { 440f, 554.37f, 659.25f, 880f }, 0.19f);
        sfxPaperFlipClip = SynthNoiseBrush("sfx_paper_flip", 0.068f, 0.16f);
        sfxWhistleClip = SynthChord("sfx_whistle", 0.24f, new[] { 587.33f, 739.99f, 880f }, new[] { 0.15f, 0.12f, 0.09f }, 10f);
    }

    AudioClip SynthNoiseBrush(string clipName, float dur, float amp)
    {
        const int rate = 44100;
        int n = Mathf.Max(64, Mathf.CeilToInt(rate * dur));
        var data = new float[n];
        uint seed = 0x5A17F321u;
        float lp = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)rate;
            seed = seed * 1664525u + 1013904223u;
            float white = ((seed >> 8) & 0xFFFF) / 32768f - 1f;
            lp = Mathf.Lerp(lp, white, 0.24f);
            float env = Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / dur));
            data[i] = Mathf.Clamp(lp * env * amp, -0.8f, 0.8f);
        }
        var clip = AudioClip.Create(clipName, n, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    AudioClip SynthChord(string clipName, float dur, float[] freqs, float[] amps, float decay)
    {
        const int rate = 44100;
        int n = Mathf.Max(64, Mathf.CeilToInt(rate * dur));
        var data = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)rate;
            float env = Mathf.Min(1f, t / 0.003f) * Mathf.Exp(-t * decay);
            float sample = 0f;
            for (int f = 0; f < freqs.Length; f++)
            {
                float amp = f < amps.Length ? amps[f] : 0.15f;
                sample += Mathf.Sin(2f * Mathf.PI * freqs[f] * t) * amp;
            }
            data[i] = Mathf.Clamp(sample * env, -0.95f, 0.95f);
        }
        var clip = AudioClip.Create(clipName, n, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    AudioClip SynthArp(string clipName, float stepDur, float[] notes, float amp)
    {
        const int rate = 44100;
        float tail = stepDur * 1.4f;
        float totalDur = stepDur * Mathf.Max(1, notes.Length) + tail;
        int n = Mathf.Max(64, Mathf.CeilToInt(rate * totalDur));
        var data = new float[n];
        for (int idx = 0; idx < notes.Length; idx++)
        {
            int startSample = Mathf.RoundToInt(idx * stepDur * rate);
            float freq = notes[idx];
            int len = n - startSample;
            for (int j = 0; j < len; j++)
            {
                float t = j / (float)rate;
                float env = Mathf.Min(1f, t / 0.004f) * Mathf.Exp(-t * 14f);
                float tone = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.78f + Mathf.Sin(4f * Mathf.PI * freq * t) * 0.22f;
                data[startSample + j] += tone * env * amp;
            }
        }
        for (int i = 0; i < n; i++) data[i] = Mathf.Clamp(data[i], -0.95f, 0.95f);
        var clip = AudioClip.Create(clipName, n, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    void PlaySfxClip(AudioClip clip, float scale = 1f)
    {
        if (!sfx || !sfxEnabled || sfxVolume <= 0.001f || !clip) return;
        sfx.PlayOneShot(clip, Mathf.Clamp01(sfxVolume * scale));
    }

    void PlayClickSfx()
    {
        if (suppressNextBtnClickSfx) { suppressNextBtnClickSfx = false; return; }
        PlaySfxClip(sfxClickClip, 0.9f);
    }

    void PlaySoftTickSfx() => PlaySfxClip(sfxSoftTickClip, 0.85f);
    void PlayBubbleOpenSfx() => PlaySfxClip(sfxBubbleOpenClip, 0.92f);
    void PlayPageOpenSfx() => PlaySfxClip(sfxPageOpenClip, 0.90f);
    void PlayCloseSfx() => PlaySfxClip(sfxCloseClip, 0.82f);
    void PlaySnapSfx() => PlaySfxClip(sfxSnapClip, 0.95f);
    void PlayBuildSfx() => PlaySfxClip(sfxBuildClip, 1.0f);
    void PlayCoinSfx() => PlaySfxClip(sfxCoinClip, 0.95f);
    void PlayTechSfx() => PlaySfxClip(sfxTechClip, 1.0f);
    void PlayErrorSfx() => PlaySfxClip(sfxErrorClip, 0.90f);
    void PlayStorySfx() => PlaySfxClip(sfxStoryClip, 0.92f);
    void PlayPaperFlipSfx() => PlaySfxClip(sfxPaperFlipClip, 0.88f);
    void PlayWhistleSfx() => PlaySfxClip(sfxWhistleClip, 0.78f);

    // ---------- 手帐交互滑条控件 ----------
    void BuildVolumeSlider(Transform parent, string name, float x, float y, float w, float initialValue, Color accentCol, Action<float> onChanged)
    {
        var track = Rect(name, parent, x, y, w, 40f);
        Rounded(track, Color.Lerp(Paper2, Ink, 0.08f));

        var fill = Rect("Fill", track.transform, 4f, 6f, Mathf.Max(14f, (w - 8f) * Mathf.Clamp01(initialValue)), 28f);
        Rounded(fill, accentCol);
        fill.GetComponent<Image>().raycastTarget = false;

        var knob = Rect("Knob", track.transform, 0f, 2f, 36f, 36f);
        Rounded(knob, CardBg);
        knob.GetComponent<Image>().raycastTarget = false;
        SoftShadow(knob);

        var valTxt = Txt(track.transform, Mathf.RoundToInt(initialValue * 100f) + "%", 23, Ink, w - 90f, 0f, 78f, 40f, TextAnchor.MiddleRight);
        valTxt.raycastTarget = false;

        var fillRt = fill.GetComponent<RectTransform>();
        var knobRt = knob.GetComponent<RectTransform>();
        float usableW = Mathf.Max(20f, w - 44f);
        knobRt.anchoredPosition = new Vector2(4f + usableW * Mathf.Clamp01(initialValue), 2f);

        var slider = track.AddComponent<JournalSliderControl>();
        slider.trackRect = track.GetComponent<RectTransform>();
        slider.onValueChanged = v => {
            float c = Mathf.Clamp01(v);
            fillRt.sizeDelta = new Vector2(Mathf.Max(10f, (w - 8f) * c), 28f);
            knobRt.anchoredPosition = new Vector2(4f + usableW * c, 2f);
            valTxt.text = Mathf.RoundToInt(c * 100f) + "%";
            onChanged?.Invoke(c);
        };
        slider.onDragEnd = () => PlaySoftTickSfx();
    }
}

public sealed class JournalSliderControl : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public RectTransform trackRect;
    public Action<float> onValueChanged;
    public Action onDragEnd;

    void ApplyPointer(PointerEventData eventData)
    {
        if (!trackRect) return;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(trackRect, eventData.position, eventData.pressEventCamera, out var local))
            return;
        float w = Mathf.Max(1f, trackRect.rect.width);
        float t = Mathf.Clamp01(local.x / w);
        onValueChanged?.Invoke(t);
    }

    public void OnPointerDown(PointerEventData eventData) { ApplyPointer(eventData); }
    public void OnDrag(PointerEventData eventData) { ApplyPointer(eventData); }
    public void OnPointerUp(PointerEventData eventData) { ApplyPointer(eventData); onDragEnd?.Invoke(); }
}
