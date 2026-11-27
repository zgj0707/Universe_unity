#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Unity CLI (-batchmode) 自动化可视化巡检与控制台异常抓取工具（支持跨 PlayMode Domain Reload）。
/// </summary>
[InitializeOnLoad]
public static class UniverseVisualAudit
{
    const string KeyActive = "UniverseAudit_Active";
    const string KeyPromoMode = "UniverseAudit_PromoMode";
    const string KeyOutDir = "UniverseAudit_OutDir";
    const string KeyStartTicks = "UniverseAudit_StartTicks";

    static readonly List<string> Errors = new List<string>();
    static readonly List<string> Warnings = new List<string>();
    static DemoBootstrap Boot;
    static int Step = -1;
    static double LastStepTime;

    static UniverseVisualAudit()
    {
        if (!SessionState.GetBool(KeyActive, false)) return;
        Application.logMessageReceived -= OnLog;
        Application.logMessageReceived += OnLog;
        EditorApplication.update -= OnEditorUpdate;
        EditorApplication.update += OnEditorUpdate;
    }

    public static void RunVisualAudit()
    {
        string argDir = GetArg("-auditOut");
        string outDir = !string.IsNullOrEmpty(argDir)
            ? argDir
            : Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/VisualAudit"));
        Directory.CreateDirectory(outDir);
        Screen.SetResolution(1080, 1920, false);

        SessionState.SetBool(KeyActive, true);
        SessionState.SetBool(KeyPromoMode, false);
        SessionState.SetString(KeyOutDir, outDir);
        SessionState.SetString(KeyStartTicks, DateTime.UtcNow.Ticks.ToString());

        Errors.Clear();
        Warnings.Clear();
        Step = -1;
        LastStepTime = 0;
        Boot = null;

        Application.logMessageReceived -= OnLog;
        Application.logMessageReceived += OnLog;
        EditorApplication.update -= OnEditorUpdate;
        EditorApplication.update += OnEditorUpdate;

        if (!EditorSceneManager.OpenScene("Assets/Scenes/Main.unity", OpenSceneMode.Single).IsValid())
        {
            Debug.LogError("[VisualAudit] 无法打开 Main.unity");
            FinishAudit(2);
            return;
        }

        EditorApplication.isPlaying = true;
    }

    public static void CapturePromoShots()
    {
        string argDir = GetArg("-auditOut");
        string outDir = !string.IsNullOrEmpty(argDir)
            ? argDir
            : Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/PromoShots"));
        Directory.CreateDirectory(outDir);
        Screen.SetResolution(1080, 1920, false);

        SessionState.SetBool(KeyActive, true);
        SessionState.SetBool(KeyPromoMode, true);
        SessionState.SetString(KeyOutDir, outDir);
        SessionState.SetString(KeyStartTicks, DateTime.UtcNow.Ticks.ToString());

        Errors.Clear();
        Warnings.Clear();
        Step = -1;
        LastStepTime = 0;
        Boot = null;

        Application.logMessageReceived -= OnLog;
        Application.logMessageReceived += OnLog;
        EditorApplication.update -= OnEditorUpdate;
        EditorApplication.update += OnEditorUpdate;

        if (!EditorSceneManager.OpenScene("Assets/Scenes/Main.unity", OpenSceneMode.Single).IsValid())
        {
            Debug.LogError("[VisualAudit] 无法打开 Main.unity");
            FinishAudit(2);
            return;
        }

        EditorApplication.isPlaying = true;
    }

    static string GetArg(string name)
    {
        var args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
            if (args[i] == name) return args[i + 1];
        return null;
    }

    static void OnLog(string condition, string stackTrace, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            Errors.Add($"[{type}] {condition}\n{stackTrace}");
        else if (type == LogType.Warning)
            Warnings.Add($"[Warning] {condition}");
    }

    static void OnEditorUpdate()
    {
        if (!SessionState.GetBool(KeyActive, false)) return;
        if (long.TryParse(SessionState.GetString(KeyStartTicks, "0"), out long ticks) && ticks > 0)
        {
            if ((DateTime.UtcNow - new DateTime(ticks, DateTimeKind.Utc)).TotalSeconds > 45.0)
            {
                Errors.Add("[Timeout] VisualAudit exceeded 45s UTC watchdog.");
                FinishAudit(4);
                return;
            }
        }

        if (!EditorApplication.isPlaying) return;

        if (Boot == null)
        {
            Boot = UnityEngine.Object.FindAnyObjectByType<DemoBootstrap>();
            if (Boot == null) return;
            LastStepTime = EditorApplication.timeSinceStartup;
            Step = 0;
            return;
        }

        if (EditorApplication.timeSinceStartup - LastStepTime < 0.45) return;

        try
        {
            if (SessionState.GetBool(KeyPromoMode, false))
                RunPromoStep(Step);
            else
                RunStep(Step);
            Step++;
            LastStepTime = EditorApplication.timeSinceStartup;
        }
        catch (Exception ex)
        {
            Errors.Add("[VisualAudit Exception] " + ex);
            FinishAudit(5);
        }
    }

    static void RunStep(int s)
    {
        switch (s)
        {
            case 0:
                Capture("01_moon_nearside.png");
                Invoke(Boot, "ShowStationActions", 0);
                break;
            case 1:
                Capture("02_station_bubble_commercial.png");
                Invoke(Boot, "ClosePanel", true);
                int resIdx = FindStation(Boot, "开普勒港");
                if (resIdx >= 0) Invoke(Boot, "ShowStationActions", resIdx);
                break;
            case 2:
                Capture("03_station_bubble_residential.png");
                Invoke(Boot, "ClosePanel", true);
                SetRegionUnlockedAndSwitch(Boot, "月背");
                break;
            case 3:
                Invoke(Boot, "ClosePanel", true);
                Capture("04_moon_farside.png");
                SetRegionUnlockedAndSwitch(Boot, "火星");
                break;
            case 4:
                Invoke(Boot, "ClosePanel", true);
                Capture("05_mars.png");
                SetRegionUnlockedAndSwitch(Boot, "月之亮面");
                Invoke(Boot, "ClosePanel", true);
                Invoke(Boot, "OpenCompanyTechScreen", 0);
                break;
            case 5:
                Capture("06_tech_tree.png");
                Invoke(Boot, "OpenCompanyTechScreen", 1);
                break;
            case 6:
                Capture("07_company_tree.png");
                Invoke(Boot, "ClosePage", null);
                Invoke(Boot, "OpenCompanyScreen");
                break;
            case 7:
                Capture("08_company_screen.png");
                Invoke(Boot, "OpenFleetTab", 1);
                break;
            case 8:
                Capture("09_fleet_research.png");
                Invoke(Boot, "OpenSceneryScreen");
                break;
            case 9:
                Capture("10_scenery_album.png");
                Invoke(Boot, "ClosePage", true);
                Invoke(Boot, "OpenEventScreen");
                break;
            case 10:
                Capture("11_events_hub.png");
                Invoke(Boot, "OpenEventSandbox", "ashdome");
                break;
            case 11:
                var existingAvg = GameObject.Find("SandboxAvg");
                if (existingAvg) { existingAvg.SetActive(false); UnityEngine.Object.Destroy(existingAvg); }
                Invoke(Boot, "OpenSandboxOnboardingStory", "ashdome", 0);
                break;
            case 12:
                Capture("12_event_ashdome_avg.png");
                Invoke(Boot, "ClosePanel", true);
                var avgObj = GameObject.Find("SandboxAvg");
                if (avgObj) UnityEngine.Object.Destroy(avgObj);
                break;
            case 13:
                Capture("12_event_ashdome_map.png");
                Invoke(Boot, "ShowStationActions", 1);
                break;
            case 14:
                Capture("12_event_ashdome_station.png");
                Invoke(Boot, "ClosePanel", true);
                Invoke(Boot, "OpenSandboxTreeScreen", "ashdome");
                break;
            case 15:
                Capture("14_event_tree.png");
                Invoke(Boot, "ClosePage", true);
                Invoke(Boot, "CloseEventSandbox");
                Invoke(Boot, "OpenEventSandbox", "neon");
                break;
            case 16:
                Capture("13_event_neon.png");
                Invoke(Boot, "CloseEventSandbox");
                FinishAudit(Errors.Count > 0 ? 1 : 0);
                break;
        }
    }

    static void RunPromoStep(int s)
    {
        var flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        var toastField = typeof(DemoBootstrap).GetField("toastGo", flags);
        Action hideToast = () => {
            var tg = toastField?.GetValue(Boot) as GameObject;
            if (tg) tg.SetActive(false);
        };
        switch (s)
        {
            case 0:
                Screen.SetResolution(1080, 1920, false);
                var allC = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                foreach (var c in allC)
                {
                    if (!c.isRootCanvas) continue;
                    var sc = c.GetComponent<CanvasScaler>();
                    if (sc) { sc.referenceResolution = new Vector2(1080f, 1920f); sc.matchWidthOrHeight = 0.5f; }
                }
                hideToast();
                break;
            case 1:
                // 居中大星网并缩放至全貌（西至开普勒/东至危海全收）
                var mapRt = typeof(DemoBootstrap).GetField("mapRect", flags)?.GetValue(Boot) as RectTransform;
                if (mapRt)
                {
                    mapRt.localScale = Vector3.one * 0.72f;
                    mapRt.anchoredPosition = new Vector2(-180f, 150f);
                }
                hideToast();
                break;
            case 2:
                hideToast();
                Capture("01_moon_network.png");
                Invoke(Boot, "OpenMetroMapScreen");
                break;
            case 3:
                hideToast();
                foreach (var btn in UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                {
                    var txt = btn.GetComponentInChildren<Text>();
                    if (txt != null && txt.text == "居中构图") { btn.onClick.Invoke(); break; }
                }
                break;
            case 4:
                hideToast();
                Capture("08_metro_map.png");
                Invoke(Boot, "CloseMetroMapScreen");
                FinishAudit(Errors.Count > 0 ? 1 : 0);
                break;
        }
    }

    static void Capture(string filename)
    {
        string outDir = SessionState.GetString(KeyOutDir, "");
        if (string.IsNullOrEmpty(outDir)) return;
        var cam = Camera.main;
        if (cam == null)
        {
            Errors.Add("[VisualAudit] Camera.main is null during " + filename);
            return;
        }
        cam.cullingMask = ~0;

        int w = 1080, h = 1920;
        var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
        var prevTarget = cam.targetTexture;
        var prevActive = RenderTexture.active;
        float prevAspect = cam.aspect;
        cam.targetTexture = rt;
        cam.aspect = (float)w / h;

        var allCanvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        var prevModes = new Dictionary<Canvas, RenderMode>();
        var prevCams = new Dictionary<Canvas, Camera>();
        foreach (var c in allCanvases)
        {
            if (!c.isRootCanvas) continue;
            prevModes[c] = c.renderMode;
            prevCams[c] = c.worldCamera;
            c.renderMode = RenderMode.ScreenSpaceCamera;
            c.worldCamera = cam;
            c.planeDistance = 10f;
            var scaler = c.GetComponent<CanvasScaler>();
            if (scaler)
            {
                scaler.referenceResolution = new Vector2(1080f, 1920f);
                scaler.matchWidthOrHeight = 0.5f;
            }
        }

        Canvas.ForceUpdateCanvases();
        foreach (var ph in UnityEngine.Object.FindObjectsByType<PageHeaderLayout>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)) ph.Refresh();
        Canvas.ForceUpdateCanvases();
        cam.Render();

        RenderTexture.active = rt;
        var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        tex.Apply();

        cam.targetTexture = prevTarget;
        cam.aspect = prevAspect;
        RenderTexture.active = prevActive;
        UnityEngine.Object.Destroy(rt);

        File.WriteAllBytes(Path.Combine(outDir, filename), tex.EncodeToPNG());
        UnityEngine.Object.Destroy(tex);

        foreach (var kv in prevModes)
        {
            if (!kv.Key) continue;
            kv.Key.renderMode = kv.Value;
            kv.Key.worldCamera = prevCams[kv.Key];
        }
    }

    static void Invoke(object target, string method, params object[] args)
    {
        var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        foreach (var m in target.GetType().GetMethods(flags))
        {
            if (m.Name != method) continue;
            var ps = m.GetParameters();
            if (args == null || args.Length == 0)
            {
                if (ps.Length == 0 || ps[0].HasDefaultValue)
                {
                    object[] callArgs = new object[ps.Length];
                    for (int i = 0; i < ps.Length; i++) callArgs[i] = ps[i].DefaultValue;
                    m.Invoke(target, callArgs);
                    return;
                }
            }
            else if (ps.Length >= args.Length)
            {
                object[] callArgs = new object[ps.Length];
                for (int i = 0; i < args.Length; i++) callArgs[i] = args[i];
                for (int i = args.Length; i < ps.Length; i++) callArgs[i] = ps[i].DefaultValue;
                m.Invoke(target, callArgs);
                return;
            }
        }
        Debug.LogWarning("[VisualAudit] Method not found: " + method);
    }

    static int FindStation(DemoBootstrap boot, string name)
    {
        var f = typeof(DemoBootstrap).GetField("stations", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        if (f == null) return -1;
        var list = f.GetValue(boot) as System.Collections.IList;
        if (list == null) return -1;
        for (int i = 0; i < list.Count; i++)
        {
            var nf = list[i].GetType().GetField("name");
            if (nf != null && (string)nf.GetValue(list[i]) == name) return i;
        }
        return -1;
    }

    static void SetRegionUnlockedAndSwitch(DemoBootstrap boot, string region)
    {
        var flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        var f1 = typeof(DemoBootstrap).GetField("moonFarUnlocked", flags);
        var f2 = typeof(DemoBootstrap).GetField("marsWestUnlocked", flags);
        if (f1 != null) f1.SetValue(boot, true);
        if (f2 != null) f2.SetValue(boot, true);
        Invoke(Boot, "SwitchRegion", region);
    }

    static void FinishAudit(int exitCode)
    {
        SessionState.SetBool(KeyActive, false);
        EditorApplication.update -= OnEditorUpdate;
        Application.logMessageReceived -= OnLog;
        string outDir = SessionState.GetString(KeyOutDir, "");
        if (!string.IsNullOrEmpty(outDir))
        {
            string reportPath = Path.Combine(outDir, "audit_report.txt");
            using (var sw = new StreamWriter(reportPath, false, System.Text.Encoding.UTF8))
            {
                sw.WriteLine($"ERRORS={Errors.Count}");
                sw.WriteLine($"WARNINGS={Warnings.Count}");
                foreach (var e in Errors) sw.WriteLine(e);
                sw.WriteLine("---WARNINGS---");
                foreach (var w in Warnings) sw.WriteLine(w);
            }
        }
        Debug.Log($"[VisualAudit] 完成：Errors={Errors.Count}, Warnings={Warnings.Count}");
        EditorApplication.Exit(exitCode);
    }
}
#endif
