#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 项目里只有一个场景 Assets/Scenes/Main.unity。
/// 打开工程时如果当前没有有效场景，自动打开它；按 Play 时也固定从它启动。
/// 菜单：星轨纪行 / 打开主场景 Main
/// </summary>
[InitializeOnLoad]
static class DemoSceneTools
{
    const string ScenePath = "Assets/Scenes/Main.unity";
    const string SessionKey = "Railway2.SceneChecked";
    const string TestSavePath = "Assets/Resources/TestSaves/railway2_local_v1.json";

    static DemoSceneTools()
    {
        EditorApplication.delayCall += EnsureMainScene;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode && DemoBootstrap.DevTestSaveDirty)
        {
            DemoBootstrap.DevTestSaveDirty = false;
            if (File.Exists(TestSavePath))
            {
                AssetDatabase.ImportAsset(TestSavePath, ImportAssetOptions.ForceUpdate);
                Debug.Log("[Universe] 已同步开发测试存档至 " + TestSavePath + "（可提交 Git 跨设备同步）");
            }
        }
    }

    static void EnsureMainScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (SessionState.GetBool(SessionKey, false)) return;
        SessionState.SetBool(SessionKey, true);

        var main = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
        if (main == null)
        {
            Debug.LogWarning("[Universe] 找不到 " + ScenePath);
            return;
        }
        EditorSceneManager.playModeStartScene = main;

        var active = SceneManager.GetActiveScene();
        bool hasRealScene = active.IsValid() && active.isLoaded && !string.IsNullOrEmpty(active.path);
        if (!hasRealScene && File.Exists(ScenePath))
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
    }



    [MenuItem("星轨纪行/选择 Bootstrap（查看编辑说明）", false, 3)]
    static void SelectBootstrap()
    {
        var go = GameObject.Find("Bootstrap");
        if (go != null) { Selection.activeGameObject = go; EditorGUIUtility.PingObject(go); }
    }

    [MenuItem("星轨纪行/打开 UI 脚本", false, 4)]
    static void OpenUIScripts()
    {
        var asset = AssetDatabase.LoadAssetAtPath<MonoScript>("Assets/Scripts/DemoBootstrap.cs");
        if (asset != null) AssetDatabase.OpenAsset(asset);
    }

    [MenuItem("星轨纪行/开始游戏（Play）", false, 0)]
    static void PlayMain()
    {
        if (EditorApplication.isPlaying) return;
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            EditorApplication.isPlaying = true;
        }
    }

    [MenuItem("星轨纪行/打开主场景 Main", false, 1)]
    static void OpenMainScene()
    {
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
    }

    [MenuItem("星轨纪行/设为 Play 启动场景", false, 2)]
    static void SetPlayScene()
    {
        var main = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
        if (main == null) return;
        EditorSceneManager.playModeStartScene = main;
        Debug.Log("[Universe] Play 启动场景 = " + ScenePath);
    }

    [MenuItem("星轨纪行/同步测试存档（跨设备开发）", false, 20)]
    static void ToggleDevTestSaveSync()
    {
        bool next = !DemoBootstrap.DevTestSaveSyncEnabled;
        DemoBootstrap.DevTestSaveSyncEnabled = next;
        Debug.Log("[Universe] 开发测试存档自动同步已" + (next ? "开启（启动优先读取并持续写回 " + TestSavePath + "）" : "关闭（仅使用本机 PlayerPrefs）"));
    }

    [MenuItem("星轨纪行/同步测试存档（跨设备开发）", true, 20)]
    static bool ValidateToggleDevTestSaveSync()
    {
        Menu.SetChecked("星轨纪行/同步测试存档（跨设备开发）", DemoBootstrap.DevTestSaveSyncEnabled);
        return true;
    }

    [MenuItem("星轨纪行/打开测试存档 JSON", false, 21)]
    static void OpenDevTestSaveJson()
    {
        var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(TestSavePath);
        if (asset != null) AssetDatabase.OpenAsset(asset);
        else Debug.LogWarning("[Universe] 找不到 " + TestSavePath);
    }
}
#endif
