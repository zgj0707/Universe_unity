using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;
public static class SaveRoundTripAudit
{
 const BindingFlags F=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;
 static Type T=typeof(DemoBootstrap),D=T.GetNestedType("SaveData",F);
 static JsonSerializerOptions O;
 static object Get(object o,string n)=>o.GetType().GetField(n,F).GetValue(o);
 static void Set(object o,string n,object v)=>o.GetType().GetField(n,F).SetValue(o,v);
 static object Call(object o,string n,params object[] a)=>T.GetMethod(n,F).Invoke(o,a);
 static void Check(bool b,string m){if(!b)throw new Exception(m);}
 static object NewDemo(JsonObject fixture)
 {
  var d=RuntimeHelpers.GetUninitializedObject(T);
  foreach(var f in T.GetFields(F))if(!f.IsStatic&&f.FieldType.IsGenericType&&!f.FieldType.IsAbstract&&f.FieldType.GetConstructor(Type.EmptyTypes)!=null)f.SetValue(d,Activator.CreateInstance(f.FieldType));
  Set(d,"previewRestoreTheme",-1);Set(d,"currentRegion",fixture["region"].GetValue<string>());
  foreach(var s in fixture["stations"].AsArray()) {var key=s["key"].GetValue<string>().Split(':');((IList)Get(d,"stations")).Add(new DemoBootstrap.Station(key[1],.3f,.4f){region=key[0]});}
  foreach(var s in fixture["scenery"].AsArray()) {var key=s["key"].GetValue<string>().Split(':');((IList)Get(d,"sceneries")).Add(new DemoBootstrap.Scenery{name=key[1],region=key[0]});}
  return d;
 }
 static JsonNode Node(object d)=>JsonNode.Parse(JsonSerializer.Serialize(d,D,O));
 public static void Run()
 {
  var resolver=new DefaultJsonTypeInfoResolver();resolver.Modifiers.Add(info=>{for(int i=info.Properties.Count-1;i>=0;i--)if(info.Properties[i].AttributeProvider is PropertyInfo)info.Properties.RemoveAt(i);});
  O=new JsonSerializerOptions{IncludeFields=true,TypeInfoResolver=resolver};
  var fixture=JsonNode.Parse(File.ReadAllText("Temp/spec-save-20261117/railway2_local_v1.before.json")).AsObject();
  fixture["lastDisplayedSolarTerm"]=17;fixture["nextContractId"]=987;
  var d=NewDemo(fixture);Call(d,"ApplySaveData",JsonSerializer.Deserialize(fixture.ToJsonString(),D,O));
  // Populate every system, independently of the user's current progress.
  ((IList)Get(d,"companyTechDone")).Clear();foreach(var n in (Array)T.GetField("CompanyTechNodes",F).GetValue(null))((IList)Get(d,"companyTechDone")).Add(Get(n,"key"));
  ((IList)Get(d,"techDone")).Clear();((IList)Get(d,"techDone")).Add("piston");((IList)Get(d,"techDone")).Add("rail");
  ((IList)Get(d,"talents")).Add(new DemoBootstrap.SavedTalent{id=101,name="往返人才",role=2,rarity=3,level=4,xp=83,assignKind=0,flawIndex=2,flawCleansed=true,bio="测试传记"});
  ((IList)Get(d,"talentPool")).Add(new DemoBootstrap.SavedTalent{id=102,name="候聘人才",role=1,rarity=2,level=2,xp=31});
  ((IList)Get(d,"earnedPlaques")).Add("audit_plaque");((IList)Get(d,"earnedMemorialTickets")).Add("audit_ticket");
  ((IDictionary)Get(d,"adWatchCounts"))["audit_ad"]=7;((IDictionary)Get(d,"themeTrialUntil"))[1]=DateTimeOffset.UtcNow.AddDays(30).ToUnixTimeSeconds();
  Set(d,"retiredVehiclesCount",19);Set(d,"nextTalentId",200);Set(d,"lastTalentRefreshDay",(int)Get(d,"day"));
  Set(d,"companyName","存档往返社");Set(d,"lastReadNoticeDay",23);Set(d,"lastPolicyDay",24);Set(d,"lastAdSubsidyDay",25);Set(d,"dailyTaskClaimedDay",26);
  object expected=Call(d,"CaptureSaveData");string json=JsonSerializer.Serialize(expected,D,O);
  string path="Temp/spec-save-20261117/sandbox/Assets/Resources/TestSaves/railway2_local_v1.json";
  Call(null,"WriteSaveJsonAtomically",path,json);Call(null,"WriteSaveJsonAtomically",path,json);
  Check(!File.Exists(path+".tmp"),"atomic write temp leaked");
  var cleared=NewDemo(fixture);Call(cleared,"ResetLoadedSessionState");Call(cleared,"ApplySaveData",JsonSerializer.Deserialize(File.ReadAllText(path),D,O));
  object actual=Call(cleared,"CaptureSaveData");var before=Node(expected);var after=Node(actual);
  foreach(var f in D.GetFields(BindingFlags.Public|BindingFlags.Instance))Check(JsonNode.DeepEquals(before[f.Name],after[f.Name]),"round-trip mismatch: "+f.Name+" before="+before[f.Name]+" after="+after[f.Name]);
  // Legitimate empty networks retain technology instead of resurrecting old lines.
  ((IList)Get(cleared,"lines")).Clear();((IList)Get(cleared,"researchVehicles")).Clear();
  object empty=Call(cleared,"CaptureSaveData");var again=NewDemo(fixture);Call(again,"ApplySaveData",JsonSerializer.Deserialize(JsonSerializer.Serialize(empty,D,O),D,O));
  Check(((IList)Get(again,"lines")).Count==0&&((IList)Get(again,"companyTechDone")).Count==105,"empty network lost progress");
  // Missing old fields preserve migration defaults; ancestors restored immediately.
  fixture["coTech"]=new JsonArray("co_fin_1");fixture.Remove("lastDisplayedSolarTerm");fixture.Remove("nextContractId");
  var legacy=NewDemo(fixture);Call(legacy,"ApplySaveData",JsonSerializer.Deserialize(fixture.ToJsonString(),D,O));
  Check(((IList)Get(legacy,"companyTechDone")).Contains("co_charter_1801"),"legacy DAG ancestors absent");
  Check((int)Get(legacy,"lastDisplayedSolarTerm")==-1,"legacy solar default");
  Console.WriteLine("SAVE_ROUND_TRIP_PASS fields="+D.GetFields(BindingFlags.Public|BindingFlags.Instance).Length+" coTech=105 all_nested_fields=EXACT fresh_memory=PASS empty_network=PASS legacy_migration=PASS atomic_disk_path=PASS serializer=managed_public_fields_adapter");
 }
}
