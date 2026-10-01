using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Bable;
[InitializeOnLoad] public static class BableRevision52Tests {
 const string Flag="Bable52.Tests";
 static string Output=>Path.GetFullPath(Path.Combine(Application.dataPath,"../../reference/revision52"));
 static double started;static readonly List<string> checks=new(),failures=new();
 static BableRevision52Tests(){EditorApplication.playModeStateChanged+=Changed;}
 public static void Batch(){Directory.CreateDirectory(Output);SessionState.SetBool(Flag,true);SessionState.SetBool("Bable.PracticePreview",false);EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");EditorApplication.EnterPlaymode();}
 static void Changed(PlayModeStateChange state){if(state!=PlayModeStateChange.EnteredPlayMode||!SessionState.GetBool(Flag,false))return;started=EditorApplication.timeSinceStartup;EditorApplication.update+=Watchdog;var host=new GameObject("Build isolation checks").AddComponent<BableTestHost>();UnityEngine.Object.DontDestroyOnLoad(host);host.StartCoroutine(Test());}
 static void Watchdog(){if(EditorApplication.timeSinceStartup-started>150){SessionState.SetBool(Flag,false);EditorApplication.Exit(2);}}
 static void Check(string name,bool pass){checks.Add(name);if(!pass)failures.Add(name);Debug.Log("REV52 "+(pass?"PASS ":"FAIL ")+name);}
 static IEnumerator Ready(){yield return null;while(TowerLoading.Busy||BableGameUI.Instance==null||!BableGameUI.Instance.Ready)yield return null;yield return new WaitForSecondsRealtime(.4f);}
 static bool ButtonExists(string name)=>UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Any(b=>b.name==name&&b.IsActive());
 static IEnumerator Test(){
  yield return Ready();Capture("campaign-title");
  Check("Campaign title has no practice",ButtonExists("NEW JOURNEY")&&!ButtonExists("PRACTICE CHAMBERS")&&!ButtonExists("RUNE & COMBAT LAB"));
  BableGameUI.Instance.Begin();yield return null;UnityEngine.Object.FindFirstObjectByType<TowerPrologue>().Skip();yield return Ready();
  BableGameUI.Instance.Pause();yield return null;Capture("campaign-pause");
  Check("Campaign pause has no practice",ButtonExists("SAVE JOURNEY")&&!ButtonExists("BOSS PRACTICE")&&!ButtonExists("RUNE & COMBAT LAB")&&!ButtonExists("CHAMBER TOOLS"));
  BableGameUI.Instance.Resume();CampaignStore.SaveNow();string save=File.ReadAllText(Path.Combine(LocalStorage.Root,"campaign.json"));
  BableGameUI.Instance.MainMenu();yield return Ready();save=File.ReadAllText(Path.Combine(LocalStorage.Root,"campaign.json"));
  Check("Continue remains available",ButtonExists("CONTINUE JOURNEY"));
  SessionState.SetBool("Bable.PracticePreview",true);BableGameUI.Instance.MainMenu();yield return null;Capture("practice-title");
  Check("Practice title has no campaign",ButtonExists("TRIALS OF THE GUARDIANS")&&ButtonExists("RUNE & COMBAT LAB")&&!ButtonExists("NEW JOURNEY")&&!ButtonExists("CONTINUE JOURNEY"));
  Check("Practice cannot read existing campaign",!CampaignStore.TryRead(out _));
  CampaignStore.RequestNew();CampaignStore.SaveNow();Check("Practice cannot replace existing campaign",File.ReadAllText(Path.Combine(LocalStorage.Root,"campaign.json"))==save);
  BableGameUI.Instance.Practice();yield return null;Capture("guardian-selection");
  Check("All five guardians listed",TowerLore.GuardianTitles.All(ButtonExists));
  SessionState.SetBool("Bable.PracticePreview",false);SessionState.SetBool(Flag,false);
  File.WriteAllText(Path.Combine(Output,"menu-tests.json"),JsonUtility.ToJson(new BableVerification.Report{checks=checks.ToArray(),failures=failures.ToArray()},true));
  EditorApplication.Exit(failures.Count==0?0:1);
 }
 static void Capture(string name){
  DisplayFrame.CaptureSize=new Vector2Int(1600,900);
  var rt=new RenderTexture(1600,900,24);var old=RenderTexture.active;
  var main=Camera.main;if(main!=null){var prior=main.targetTexture;main.targetTexture=rt;main.rect=new Rect(0,0,1,1);main.Render();main.targetTexture=prior;}
  var cg=new GameObject("UI capture camera");var camera=cg.AddComponent<Camera>();camera.clearFlags=main!=null?CameraClearFlags.Depth:CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.015f,.02f,.025f);camera.orthographic=true;camera.cullingMask=1<<31;camera.targetTexture=rt;camera.transform.position=new Vector3(0,0,-100);
  var canvases=UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c=>c.isRootCanvas&&c.renderMode==RenderMode.ScreenSpaceOverlay).ToArray();
  var layers=new Dictionary<GameObject,int>();
  foreach(var c in canvases){foreach(var t in c.GetComponentsInChildren<Transform>(true)){layers[t.gameObject]=t.gameObject.layer;t.gameObject.layer=31;}c.renderMode=RenderMode.ScreenSpaceCamera;c.worldCamera=camera;c.planeDistance=5;c.scaleFactor=1;}
  Canvas.ForceUpdateCanvases();Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=rt;
  var texture=new Texture2D(1600,900,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,1600,900),0,0);texture.Apply();File.WriteAllBytes(Path.Combine(Output,name+".png"),texture.EncodeToPNG());
  foreach(var c in canvases){c.renderMode=RenderMode.ScreenSpaceOverlay;c.worldCamera=null;}foreach(var l in layers)l.Key.layer=l.Value;
  RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(texture);UnityEngine.Object.DestroyImmediate(cg);rt.Release();UnityEngine.Object.DestroyImmediate(rt);DisplayFrame.CaptureSize=Vector2Int.zero;
 }
}
