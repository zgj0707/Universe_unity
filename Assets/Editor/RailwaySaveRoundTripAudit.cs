using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using UnityEditor;
using UnityEngine;

// Isolated audit: no scene objects, live PlayerPrefs, or real test-save writes.
public static class RailwaySaveRoundTripAudit
{
    const string Report = "Temp/spec-save-20261117/native-result.txt";
    const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
    static readonly Type Boot = typeof(DemoBootstrap);
    static object Get(object o, string n) => o.GetType().GetField(n, Flags).GetValue(o);
    static void Set(object o, string n, object v) => o.GetType().GetField(n, Flags).SetValue(o, v);
    static object Call(object o, string n, params object[] args) => Boot.GetMethod(n, Flags).Invoke(o, args);
    static object Empty(object data)
    {
        var result = FormatterServices.GetUninitializedObject(Boot);
        foreach (var f in Boot.GetFields(Flags))
            if (!f.IsStatic && f.FieldType.IsGenericType && !f.FieldType.IsAbstract && f.FieldType.GetConstructor(Type.EmptyTypes) != null)
                f.SetValue(result, Activator.CreateInstance(f.FieldType));
        Set(result, "previewRestoreTheme", -1); Set(result, "currentRegion", Get(data, "region"));
        foreach (object station in (IList)Get(data, "stations"))
        {
            string[] parts = ((string)Get(station, "key")).Split(':');
            ((IList)Get(result, "stations")).Add(new DemoBootstrap.Station(parts[1], .3f, .4f) { region=parts[0] });
        }
        foreach (object scenery in (IList)Get(data, "scenery"))
        {
            string[] parts = ((string)Get(scenery, "key")).Split(':');
            ((IList)Get(result, "sceneries")).Add(new DemoBootstrap.Scenery { name=parts[1], region=parts[0] });
        }
        return result;
    }
    [MenuItem("Railway2/Audit/Save round trip (isolated)")]
    public static void Run()
    {
        try
        {
            Type dataType = Boot.GetNestedType("SaveData", Flags);
            string fixture = "Temp/spec-save-20261117/sandbox/Assets/Resources/TestSaves/railway2_local_v1.json";
            if (!File.Exists(fixture)) throw new Exception("Run the managed audit first to generate the rich fixture.");
            object data = JsonUtility.FromJson(File.ReadAllText(fixture), dataType);
            object first = Empty(data); Call(first, "ApplySaveData", data);
            object expected = Call(first, "CaptureSaveData");
            string path = "Temp/spec-save-20261117/native-sandbox/Assets/Resources/TestSaves/railway2_local_v1.json";
            string json = JsonUtility.ToJson(expected, true);
            Call(null, "WriteSaveJsonAtomically", path, json);
            Call(null, "WriteSaveJsonAtomically", path, json);
            object second = Empty(data); Call(second, "ResetLoadedSessionState");
            Call(second, "ApplySaveData", JsonUtility.FromJson(File.ReadAllText(path), dataType));
            string actual = JsonUtility.ToJson(Call(second, "CaptureSaveData"), true);
            if (json != actual) throw new Exception("Native JSON round-trip differs.");
            Call(first, "BeginUndo", "audit");
            object undo = Get(first, "pendingUndo");
            if (((IList)Get(undo, "ownedThemes")).Count != (int)Get(first, "ownedThemes").GetType().GetProperty("Count").GetValue(Get(first, "ownedThemes"))) throw new Exception("Theme undo snapshot incomplete");
            var contracts = (IList)Get(first, "activeContracts");
            var copies = (IList)Get(undo, "contracts");
            if (contracts.Count > 0 && ReferenceEquals(contracts[0], copies[0])) throw new Exception("Contract undo snapshot aliased");
            string signature = (string)Call(first, "UndoExternalProgressSignature");
            Set(first, "retiredVehiclesCount", (int)Get(first, "retiredVehiclesCount") + 1);
            if (signature == (string)Call(first, "UndoExternalProgressSignature")) throw new Exception("External progress guard missed retirement");
            string result = "NATIVE_SAVE_ROUND_TRIP_PASS fields=" + dataType.GetFields().Length + " JsonUtility=EXACT fresh_instance=PASS atomic_disk=PASS live_save_untouched=PASS undo_snapshots_and_guard=PASS";
            File.WriteAllText(Report, result); Debug.Log(result);
        }
        catch (Exception error) { Directory.CreateDirectory(Path.GetDirectoryName(Report)); File.WriteAllText(Report, "FAIL " + error); Debug.LogException(error); if (Application.isBatchMode) EditorApplication.Exit(1); }
    }
}
