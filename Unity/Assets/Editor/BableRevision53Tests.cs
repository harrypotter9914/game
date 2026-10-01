using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Bable;
using Babel.Runtime.Core;
using Babel.Runtime.Runes;
using Babel.Runtime.Characters.Player;
using Babel.Runtime.Combat;

[InitializeOnLoad] public static class BableRevision53Tests {
 const string Flag="Bable53.Tests";
 static string Output=>Path.GetFullPath(Path.Combine(Application.dataPath,"../../reference/revision53"));
 static double started;static readonly List<string> checks=new(),failures=new();
 static BableRevision53Tests(){EditorApplication.playModeStateChanged+=Changed;}
 public static void Batch(){Directory.CreateDirectory(Output);BableRevision53.Install();EditorBuildSettings.scenes=BablePlayerBuilds.CampaignScenes.Concat(BablePlayerBuilds.PracticeScenes.Skip(1)).Select(p=>new EditorBuildSettingsScene(p,true)).ToArray();SessionState.SetBool(Flag,true);SessionState.SetBool("Bable.PracticePreview",false);EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");EditorApplication.EnterPlaymode();}
 static void Changed(PlayModeStateChange state){if(state!=PlayModeStateChange.EnteredPlayMode||!SessionState.GetBool(Flag,false))return;started=EditorApplication.timeSinceStartup;EditorApplication.update+=Watchdog;var host=new GameObject("Responsive menu checks").AddComponent<BableTestHost>();UnityEngine.Object.DontDestroyOnLoad(host);InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;host.StartCoroutine(Test());}
 static void Watchdog(){if(EditorApplication.timeSinceStartup-started>240){Check("Watchdog timeout",false);Finish();}}
 static void Check(string name,bool pass){checks.Add(name);if(!pass)failures.Add(name);Debug.Log("REV53 "+(pass?"PASS ":"FAIL ")+name);File.WriteAllText(Path.Combine(Output,"tests.json"),JsonUtility.ToJson(new BableVerification.Report{checks=checks.ToArray(),failures=failures.ToArray()},true));}
 static IEnumerator Ready(){yield return null;while(TowerLoading.Busy||BableGameUI.Instance==null||!BableGameUI.Instance.Ready)yield return null;yield return new WaitForSecondsRealtime(.4f);}
 static Button Find(string name)=>UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None).FirstOrDefault(b=>b.name==name&&b.IsActive());
 static void Click(string name){var b=Find(name);if(b==null)throw new Exception("Missing "+name);b.onClick.Invoke();}
 static void Pad(Gamepad pad,GamepadButton? button=null){InputSystem.QueueStateEvent(pad,button.HasValue?new GamepadState().WithButton(button.Value):new GamepadState());}
 static void Key(Keyboard keyboard,UnityEngine.InputSystem.Key? key=null){InputSystem.QueueStateEvent(keyboard,key.HasValue?new KeyboardState(key.Value):new KeyboardState());}
 static IEnumerator Test(){
  yield return Ready();var mouse=InputSystem.AddDevice<Mouse>();var keyboard=InputSystem.AddDevice<Keyboard>();var pad=InputSystem.AddDevice<Gamepad>();
  InputSystem.QueueStateEvent(mouse,new MouseState{position=new Vector2(2,2)});yield return null;GameInput.UsePointer();yield return new WaitForSecondsRealtime(.25f);
  var newButton=Find("NEW JOURNEY");var motion=newButton.GetComponent<ManuscriptMenuButton>();
  Check("Mouse startup has no forced selected item",EventSystem.current.currentSelectedGameObject==null&&motion.Highlight<.01f);
  motion.OnPointerEnter(new PointerEventData(EventSystem.current));yield return new WaitForSecondsRealtime(.25f);Check("Mouse hover illuminates",motion.Highlight>.99f);
  motion.OnPointerExit(new PointerEventData(EventSystem.current));motion.OnSelect(new BaseEventData(EventSystem.current));yield return new WaitForSecondsRealtime(.25f);Check("Mouse selection cannot stick after exit",motion.Highlight<.01f);
  Key(keyboard,UnityEngine.InputSystem.Key.DownArrow);yield return null;Key(keyboard);yield return new WaitForSecondsRealtime(.25f);
  Check("Keyboard navigation automatically takes focus",GameInput.Source==MenuInputSource.Keyboard&&EventSystem.current.currentSelectedGameObject!=null&&EventSystem.current.currentSelectedGameObject.GetComponent<ManuscriptMenuButton>().Highlight>.9f);
  Pad(pad,GamepadButton.DpadUp);yield return null;Pad(pad);yield return new WaitForSecondsRealtime(.25f);
  Check("Controller automatically retains visible focus",GameInput.UsingGamepad&&EventSystem.current.currentSelectedGameObject!=null&&EventSystem.current.currentSelectedGameObject.GetComponent<ManuscriptMenuButton>().Highlight>.9f);
  InputSystem.QueueDeltaStateEvent(mouse.position,new Vector2(10,10));yield return null;InputSystem.QueueDeltaStateEvent(mouse.delta,new Vector2(8,8));yield return null;yield return new WaitForSecondsRealtime(.25f);
  Check("Mouse movement clears navigation selection",GameInput.Source==MenuInputSource.Pointer&&EventSystem.current.currentSelectedGameObject==null);
  foreach(var size in new[]{new Vector2Int(1600,900),new Vector2Int(1024,768),new Vector2Int(1280,800),new Vector2Int(2560,1080),new Vector2Int(640,480),new Vector2Int(900,1200)})Capture("title-"+size.x+"x"+size.y,size.x,size.y);
  BableGameUI.Instance.OpenSettings();yield return null;Click("CONTROLS");yield return new WaitForSecondsRealtime(.25f);
  Check("Only the active settings tab remains marked",Find("CONTROLS").GetComponent<ManuscriptMenuButton>().ActiveTab&&!Find("SOUND").GetComponent<ManuscriptMenuButton>().ActiveTab&&Find("SOUND").GetComponent<ManuscriptMenuButton>().Highlight<.01f);
  Capture("controls-mouse",1600,900);
  Pad(pad,GamepadButton.DpadDown);yield return null;Pad(pad);yield return new WaitForSecondsRealtime(.25f);
  Check("Controls follow active controller automatically",Find("CONTROLLER")!=null&&GameInput.UsingGamepad);
  Click("Binding Attack");yield return null;Check("Controller rebind captures input",GameInput.Rebinding);
  Pad(pad,GamepadButton.RightStick);yield return null;Pad(pad);yield return new WaitForSecondsRealtime(.4f);
  Check("Controller rebind assigns new button",!GameInput.Rebinding&&GameInput.Action(GameAction.Attack).bindings[1].effectivePath=="<Gamepad>/rightStickPress");
  Check("Rebind returns focus to same action",EventSystem.current.currentSelectedGameObject==Find("Binding Attack").gameObject);Capture("controls-pad",1600,900);
  Click("Binding Attack");yield return null;Pad(pad,GamepadButton.East);yield return null;Pad(pad);yield return new WaitForSecondsRealtime(.4f);
  Check("Controller can cancel binding without keyboard",!GameInput.Rebinding&&EventSystem.current.currentSelectedGameObject==Find("Binding Attack").gameObject);
  GameInput.ResetBindings();Click("SOUND");yield return null;EventSystem.current.SetSelectedGameObject(Find("MUSIC VALUE").gameObject);float before=GameSettings.Current.music;
  Pad(pad,GamepadButton.DpadLeft);yield return null;Pad(pad);yield return new WaitForSecondsRealtime(.3f);
  Check("Controller adjusts focused sound row",GameSettings.Current.music<before&&EventSystem.current.currentSelectedGameObject==Find("MUSIC VALUE").gameObject);
  Capture("sound-pad",1024,768);Click("RETURN");yield return null;
  BableGameUI.Instance.Begin();yield return null;UnityEngine.Object.FindFirstObjectByType<TowerPrologue>().Skip();yield return Ready();
  var player=UnityEngine.Object.FindFirstObjectByType<PlayerController2D>();var session=GameSession.Instance;
  Check("Gameplay uses whole viewport",Camera.main.rect==new Rect(0,0,1,1));
  foreach(var r in Resources.LoadAll<RuneDefinition>("Bable/Runes"))session.CollectRune(r);
  var altar=UnityEngine.Object.FindObjectsByType<AltarCheckpoint>(FindObjectsSortMode.None).OrderBy(a=>Vector2.Distance(a.transform.position,player.transform.position)).First();
  player.transform.position=altar.PlayerPosition(player);player.GetComponent<Rigidbody2D>().linearVelocity=Vector2.zero;Physics2D.SyncTransforms();yield return new WaitForSeconds(.25f);
  BableGameUI.Instance.Runes();yield return null;var repo=UnityEngine.Object.FindFirstObjectByType<RuneRepositoryView>();
  Check("Altar allows rune changes",repo.CanEdit&&repo.ActivateCell(4));yield return new WaitForSecondsRealtime(.35f);
  Check("Altar equip applies exactly one rune",session.RuneInventory.ActiveSlots.Count(r=>r!=null)==1);
  Capture("runes-altar",1600,900);Capture("runes-4x3",1024,768);Capture("runes-small",640,480);
  BableGameUI.Instance.Resume();player.transform.position+=Vector3.right*12;Physics2D.SyncTransforms();yield return new WaitForSeconds(.3f);
  BableGameUI.Instance.Runes();yield return null;repo=UnityEngine.Object.FindFirstObjectByType<RuneRepositoryView>();
  Check("Repository opens anywhere but cannot remove away from altar",!repo.CanEdit&&!repo.ActivateCell(0)&&session.RuneInventory.ActiveSlots.Count(r=>r!=null)==1);
  Check("Cannot equip away from altar",!repo.ActivateCell(4));Capture("runes-readonly",1600,900);
  BableGameUI.Instance.Resume();var hp=player.GetComponent<HealthComponent>();hp.ApplyDamage(1);var feedback=player.GetComponent<PlayerHitFeedback>();
  Check("Stronger flash follows accepted damage",feedback.HitCount>0&&feedback.FlashAlpha>.2f);GameSettings.Current.flash=0;Check("Flash accessibility control still works",feedback.FlashAlpha==0);GameSettings.Current.flash=1;
  BableGameUI.Instance.Pause();yield return null;Capture("pause-wide",2560,1080);Capture("pause-4x3",1024,768);Capture("pause-small",640,480);
  BableGameUI.Instance.Death();yield return null;Capture("death-wide",2560,1080);Capture("death-4x3",1024,768);Capture("death-small",640,480);
  BableGameUI.Instance.Resume();BableGameUI.Instance.MainMenu();yield return Ready();Capture("title-continue",1600,900);
  var buttons=new[]{"CONTINUE JOURNEY","NEW JOURNEY","SETTINGS","QUIT GAME"}.Select(Find).ToArray();
  Check("Continue layout has equal spacing",buttons.All(b=>b!=null)&&Enumerable.Range(0,3).All(i=>Mathf.Abs(((RectTransform)buttons[i].transform).anchoredPosition.y-((RectTransform)buttons[i+1].transform).anchoredPosition.y-100)<.1f));
  BableGameUI.Instance.OpenLab();yield return Ready();BableGameUI.Instance.Runes();yield return null;repo=UnityEngine.Object.FindFirstObjectByType<RuneRepositoryView>();
  Check("Standalone practice rules allow free equipment",CampaignStore.IsPractice&&repo.CanEdit&&repo.ActivateCell(4));
  Finish();
 }
 static void Finish(){SessionState.SetBool(Flag,false);EditorBuildSettings.scenes=BablePlayerBuilds.CampaignScenes.Select(p=>new EditorBuildSettingsScene(p,true)).ToArray();EditorApplication.Exit(failures.Count==0?0:1);}
 static void Capture(string name,int width,int height){
  DisplayFrame.CaptureSize=new Vector2Int(width,height);var rt=new RenderTexture(width,height,24);var old=RenderTexture.active;
  var main=Camera.main;if(main!=null){var prior=main.targetTexture;main.targetTexture=rt;main.rect=new Rect(0,0,1,1);main.Render();main.targetTexture=prior;}
  var cg=new GameObject("UI capture camera");var camera=cg.AddComponent<Camera>();camera.clearFlags=main!=null?CameraClearFlags.Depth:CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.015f,.02f,.025f);camera.orthographic=true;camera.cullingMask=1<<31;camera.targetTexture=rt;camera.transform.position=new Vector3(0,0,-100);
  var canvases=UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c=>c.isRootCanvas&&c.renderMode==RenderMode.ScreenSpaceOverlay).ToArray();var layers=new Dictionary<GameObject,int>();
  foreach(var c in canvases){foreach(var t in c.GetComponentsInChildren<Transform>(true)){layers[t.gameObject]=t.gameObject.layer;t.gameObject.layer=31;}c.renderMode=RenderMode.ScreenSpaceCamera;c.worldCamera=camera;c.planeDistance=5;c.scaleFactor=Mathf.Min(width/1600f,height/900f);}
  Canvas.ForceUpdateCanvases();foreach(var art in UnityEngine.Object.FindObjectsByType<FullBleedMenuArt>(FindObjectsSortMode.None))art.Fit();Canvas.ForceUpdateCanvases();
  bool bounds=true;foreach(var button in UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Where(b=>b.IsActive())){var corners=new Vector3[4];((RectTransform)button.transform).GetWorldCorners(corners);foreach(var corner in corners){var p=camera.WorldToScreenPoint(corner);bounds&=p.x>=-1&&p.y>=-1&&p.x<=width+1&&p.y<=height+1;}}
  Check(name+" buttons fit screen",bounds);Check(name+" full viewport",DisplayFrame.Calculate(width,height)==new Rect(0,0,1,1));
  camera.Render();RenderTexture.active=rt;var texture=new Texture2D(width,height,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,width,height),0,0);texture.Apply();File.WriteAllBytes(Path.Combine(Output,name+".png"),texture.EncodeToPNG());
  foreach(var c in canvases){c.renderMode=RenderMode.ScreenSpaceOverlay;c.worldCamera=null;}foreach(var l in layers)l.Key.layer=l.Value;
  RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(texture);UnityEngine.Object.DestroyImmediate(cg);rt.Release();UnityEngine.Object.DestroyImmediate(rt);DisplayFrame.CaptureSize=Vector2Int.zero;
 }
}
