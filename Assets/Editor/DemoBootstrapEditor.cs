using UnityEditor;
using UnityEngine;

/// <summary>
/// 让用户在 Unity 里一眼看懂：Main 场景只有 Bootstrap，UI 全部由代码在运行时生成。
/// 编辑入口是 Assets/Scripts 下的脚本，不是 Scene 视图里的拖拽对象。
/// </summary>
[CustomEditor(typeof(DemoBootstrap))]
public class DemoBootstrapEditor : Editor
{
    public override void OnInspectorGUI()
    {
        EditorGUILayout.HelpBox(
            "Main 场景里只有 Bootstrap（+ 运行时生成的 Canvas/EventSystem）。\n\n" +
            "地图、HUD、弹窗、页面全部由代码生成，所以 Scene 视图本来没有可拖拽的 UI。\n" +
            "要改界面/数值/玩法：编辑 Assets/Scripts 下的脚本，保存后按 Play 预览。\n\n" +
            "如果正在 Play 模式，请先点顶部 Play 按钮退出，再编辑。",
            MessageType.Info);

        if (GUILayout.Button("打开 DemoBootstrap.cs（HUD/地图/弹窗）"))
            OpenScript("Assets/Scripts/DemoBootstrap.cs");
        if (GUILayout.Button("打开 DemoScreens.cs（线路/车库/科技页面）"))
            OpenScript("Assets/Scripts/DemoScreens.cs");
        if (GUILayout.Button("打开 Mobile 输入脚本"))
            OpenScript("Assets/Scripts/DemoMobile.cs");

        GUILayout.Space(8);
        if (GUILayout.Button("进入 Play 预览"))
        {
            if (!EditorApplication.isPlaying)
            {
                var main = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/Main.unity");
                if (main != null) UnityEditor.SceneManagement.EditorSceneManager.playModeStartScene = main;
                EditorApplication.isPlaying = true;
            }
        }
        if (GUILayout.Button("打开主场景 Main"))
        {
            if (UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Main.unity");
        }
    }

    static void OpenScript(string path)
    {
        var asset = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
        if (asset != null) AssetDatabase.OpenAsset(asset);
        else Debug.LogWarning("[Universe] 找不到脚本 " + path);
    }
}
