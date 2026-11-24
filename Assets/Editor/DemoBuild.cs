
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System.IO;

public class DemoBuild
{
    [MenuItem("Demo/Build WebGL")]
    public static void BuildWebGL()
    {
        Directory.CreateDirectory("Assets/Scenes");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var go = new GameObject("Bootstrap");
        go.AddComponent<DemoBootstrap>();
        var scenePath = "Assets/Scenes/Main.unity";
        EditorSceneManager.SaveScene(scene, scenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(scenePath, true) };

        PlayerSettings.companyName = "深空轨社";
        PlayerSettings.productName = "星轨纪行：出发去月球！";
        PlayerSettings.defaultScreenWidth = 1080;
        PlayerSettings.defaultScreenHeight = 1920;
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback = true;
        PlayerSettings.WebGL.template = "APPLICATION:Default";
        PlayerSettings.runInBackground = true;

        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { scenePath },
            locationPathName = "Build/WebGL",
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        });
        Debug.Log("BUILD RESULT " + report.summary.result + " size=" + report.summary.totalSize);
        if (report.summary.result != BuildResult.Succeeded) EditorApplication.Exit(1);

        var idx = "Build/WebGL/index.html";
        var tpl = "Assets/Editor/WebGLTemplate.html";
        if (File.Exists(tpl))
        {
            File.Copy(tpl, idx, true);
            Debug.Log("INDEX TEMPLATE APPLIED");
        }
    }
}
