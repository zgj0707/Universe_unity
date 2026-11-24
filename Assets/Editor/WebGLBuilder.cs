using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

public static class WebGLBuilder
{
    static readonly string[] Scenes = { "Assets/Scenes/Main.unity" };

    static void RunWithoutTestSaves(Action buildAction)
    {
        string srcDir = Path.GetFullPath("Assets/Resources/TestSaves");
        string srcMeta = srcDir + ".meta";
        string tmpDir = Path.GetFullPath("Temp/_TestSaves_Excluded");
        string tmpMeta = tmpDir + ".meta";
        bool movedDir = false, movedMeta = false;
        try
        {
            if (Directory.Exists(srcDir))
            {
                if (Directory.Exists(tmpDir)) Directory.Delete(tmpDir, true);
                Directory.CreateDirectory(Path.GetDirectoryName(tmpDir));
                Directory.Move(srcDir, tmpDir);
                movedDir = true;
            }
            if (File.Exists(srcMeta))
            {
                if (File.Exists(tmpMeta)) File.Delete(tmpMeta);
                File.Move(srcMeta, tmpMeta);
                movedMeta = true;
            }
            if (movedDir || movedMeta) AssetDatabase.Refresh();
            buildAction();
        }
        finally
        {
            if (movedDir && Directory.Exists(tmpDir) && !Directory.Exists(srcDir))
                Directory.Move(tmpDir, srcDir);
            if (movedMeta && File.Exists(tmpMeta) && !File.Exists(srcMeta))
                File.Move(tmpMeta, srcMeta);
            if (movedDir || movedMeta) AssetDatabase.Refresh();
        }
    }

    public static void BuildWebGL()
    {
        RunWithoutTestSaves(() =>
        {
            string outDir = Path.GetFullPath("Build/WebGL");
            Directory.CreateDirectory(outDir);
            var opts = new BuildPlayerOptions
            {
                scenes = Scenes,
                locationPathName = outDir,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            };
            BuildReport report = BuildPipeline.BuildPlayer(opts);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new Exception("WebGL build failed: " + report.summary.result + " errors=" + report.summary.totalErrors);
            }
        });
    }

    public static void BuildTrailerWebGL()
    {
        string outDir = Path.GetFullPath("Build/_TrailerWebGL");
        Directory.CreateDirectory(outDir);
        string prevDefines = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.WebGL);
        try
        {
            string nextDefines = string.IsNullOrEmpty(prevDefines)
                ? "TRAILER_PREVIEW"
                : (prevDefines.Contains("TRAILER_PREVIEW") ? prevDefines : prevDefines + ";TRAILER_PREVIEW");
            PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.WebGL, nextDefines);
            var opts = new BuildPlayerOptions
            {
                scenes = Scenes,
                locationPathName = outDir,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            };
            BuildReport report = BuildPipeline.BuildPlayer(opts);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new Exception("Trailer WebGL build failed: " + report.summary.result + " errors=" + report.summary.totalErrors);
            }
        }
        finally
        {
            PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.WebGL, prevDefines);
        }
    }

    public static void BuildWindows()
    {
        RunWithoutTestSaves(() =>
        {
            string outDir = Path.GetFullPath("Build/Windows");
            Directory.CreateDirectory(outDir);
            string exePath = Path.Combine(outDir, "StarlineMoon.exe");
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.defaultScreenWidth = 720;
            PlayerSettings.defaultScreenHeight = 1280;
            PlayerSettings.resizableWindow = true;
            var opts = new BuildPlayerOptions
            {
                scenes = Scenes,
                locationPathName = exePath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };
            BuildReport report = BuildPipeline.BuildPlayer(opts);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new Exception("Windows build failed: " + report.summary.result + " errors=" + report.summary.totalErrors);
            }
        });
    }

    public static void BuildWebGLAndWindows()
    {
        BuildWebGL();
        BuildWindows();
    }

    [MenuItem("Build/Export Android APK")]
    public static void BuildAndroid()
    {
        RunWithoutTestSaves(() =>
        {
            string outDir = Path.GetFullPath("Build/Android");
            Directory.CreateDirectory(outDir);
            string apkPath = Path.Combine(outDir, "StarlineMoon-v1.0.0.apk");
            EditorUserBuildSettings.buildAppBundle = false;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.starrail.cosmosline");
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            var opts = new BuildPlayerOptions
            {
                scenes = Scenes,
                locationPathName = apkPath,
                target = BuildTarget.Android,
                options = BuildOptions.None
            };
            BuildReport report = BuildPipeline.BuildPlayer(opts);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new Exception("Android APK build failed: " + report.summary.result + " errors=" + report.summary.totalErrors);
            }
        });
    }
}
