using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Bable;
using Babel.Runtime.Core;
using Babel.Runtime.Characters.Player;
using Babel.Runtime.World;
using Babel.Runtime.Runes;
using Babel.Runtime.Combat;

[InitializeOnLoad]
public static class BableRevision51Tests {
 const string Flag="Bable51.Tests";
 static string Output=>Path.GetFullPath(Path.Combine(Application.dataPath,"../../reference/revision51"));
 static List<string> checks=new(),failures=new();static double started;static BableTestHost host;
 static BableRevision51Tests(){EditorApplication.playModeStateChanged+=ModeChanged;}
 public static void Reload(){SessionState.SetBool("Bable51.Reload",true);Batch();}
 public static void Batch(){Directory.CreateDirectory(Output);SessionState.SetBool(Flag,true);EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");EditorApplication.EnterPlaymode();}
 static void ModeChanged(PlayModeStateChange state){if(!SessionState.GetBool(Flag,false))return;if(state==PlayModeStateChange.EnteredPlayMode){started=EditorApplication.timeSinceStartup;EditorApplication.update+=Watchdog;host=new GameObject("Revision51 regression").AddComponent<BableTestHost>();UnityEngine.Object.DontDestroyOnLoad(host);InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;host.StartCoroutine(SessionState.GetBool("Bable51.Reload",false)?ReloadTest():Test());}}
 static void Watchdog(){if(EditorApplication.timeSinceStartup-started>240){File.WriteAllText(Path.Combine(Output,"timeout.txt"),"Timed out; last check: "+checks.LastOrDefault());EditorApplication.Exit(2);}}
 static void Check(string name,bool pass){checks.Add(name);if(!pass)failures.Add(name);Debug.Log("REV51 "+(pass?"PASS ":"FAIL ")+name);File.WriteAllText(Path.Combine(Output,SessionState.GetBool("Bable51.Reload",false)?"reload-tests.json":"main-tests.json"),JsonUtility.ToJson(new BableVerification.Report{checks=checks.ToArray(),failures=failures.ToArray()},true));}
 static IEnumerator Ready(){yield return null;while(TowerLoading.Busy||BableGameUI.Instance==null||!BableGameUI.Instance.Ready)yield return null;yield return new WaitForSecondsRealtime(.3f);}
 static Button FindButton(string name)=>UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None).FirstOrDefault(b=>b.name==name&&b.IsActive());
 static void Click(string name){var b=FindButton(name);if(b==null)throw new Exception("Missing button "+name);b.onClick.Invoke();}
 static void Pad(Gamepad pad,GamepadButton? button=null){InputSystem.QueueStateEvent(pad,button.HasValue?new GamepadState().WithButton(button.Value):new GamepadState());}
 static void PressKey(Keyboard keyboard,Key? key=null){InputSystem.QueueStateEvent(keyboard,key.HasValue?new KeyboardState(key.Value):new KeyboardState());}
 static IEnumerator Test(){
  yield return Ready();
  Check("Title is isolated from gameplay HUD",BableGameUI.Instance.IsTitleScene&&GameSession.Instance==null&&UnityEngine.Object.FindFirstObjectByType<VotiveHud>()==null);
  Check("First launch has no Continue button",!CampaignStore.HasSave&&FindButton("CONTINUE JOURNEY")==null);
  Capture("title");
  var pad=InputSystem.AddDevice<Gamepad>();Pad(pad,GamepadButton.DpadDown);yield return null;
  Check("Virtual controller direction and device hint",GameInput.Held(GameAction.Down)&&GameInput.UsingGamepad);Pad(pad);
  BableGameUI.Instance.OpenSettings();yield return null;
  FindButton("MUSIC VALUE").GetComponent<SettingChoice>().Adjust(-1);Check("Independent music volume",GameSettings.Current.music<1&&GameSettings.Current.voice==1);
  GameSettings.Load();Check("Settings persisted across reload",GameSettings.Current.music<1);Capture("settings-sound");
  Click("DISPLAY & COMFORT");yield return null;Capture("settings-display");
  Click("SCREEN MODE VALUE");yield return null;Check("Display change requires confirmation",BableGameUI.Instance.Mode=="confirm");Click("CANCEL");yield return null;Check("Display cancel restores mode",!GameSettings.Current.fullscreen);
  Click("CONTROLS");yield return null;Capture("settings-controls");
  var keyboard=InputSystem.AddDevice<Keyboard>();bool rebound=false;
  GameInput.Rebind(GameAction.Attack,false,()=>rebound=true);PressKey(keyboard,Key.F);yield return null;PressKey(keyboard);yield return new WaitForSecondsRealtime(.3f);
  Check("Interactive keyboard remap persisted",rebound&&GameInput.Action(GameAction.Attack).bindings[0].effectivePath=="<Keyboard>/f"&&File.Exists(Path.Combine(LocalStorage.Root,"bindings.json")));
  PressKey(keyboard,Key.F);yield return null;Debug.Log("REMAP enabled="+GameInput.Action(GameAction.Attack).enabled+" value="+GameInput.Action(GameAction.Attack).ReadValue<float>()+" key="+keyboard.fKey.isPressed);Check("Remapped attack fires",GameInput.Held(GameAction.Attack));PressKey(keyboard);
  GameInput.Rebind(GameAction.Attack,false,()=>{});PressKey(keyboard,Key.A);yield return null;PressKey(keyboard);yield return new WaitForSecondsRealtime(.3f);
  Check("Duplicate binding rejected",GameInput.Action(GameAction.Attack).bindings[0].effectivePath=="<Keyboard>/f");
  GameInput.ReloadBindings();Check("Remap survives bindings file reload",GameInput.Action(GameAction.Attack).bindings[0].effectivePath=="<Keyboard>/f");GameInput.ResetBindings();Check("Controls reset",GameInput.Action(GameAction.Attack).bindings[0].effectivePath=="<Keyboard>/x");
  Click("RETURN");yield return null;
  BableGameUI.Instance.Begin();yield return null;Check("New journey opens automatic prologue",BableGameUI.Instance.Mode=="prologue");UnityEngine.Object.FindFirstObjectByType<TowerPrologue>().Skip();
  yield return Ready();
  Check("Campaign loads and settles",CampaignStore.Ready&&UnityEngine.Object.FindFirstObjectByType<PlayerController2D>().IsGrounded);
  var s=GameSession.Instance;var p=UnityEngine.Object.FindFirstObjectByType<PlayerController2D>();var hp=p.GetComponent<HealthComponent>();
  s.UnlockWeapon();s.UnlockAbility(AbilityId.Shockwave);s.AddPermanentMaxMana(2);s.AddPermanentMaxHealth(1);s.AddGold(147);s.GrantBossMana("Shockwave");
  var runes=Resources.LoadAll<RuneDefinition>("Bable/Runes");foreach(var rune in runes)s.CollectRune(rune);
  var order=new[]{RuneId.Moon,RuneId.Sun,RuneId.Harvest,RuneId.Star};for(int i=0;i<4;i++)s.RuneInventory.EquipAt(runes.First(r=>r.RuneId==order[i]),i);
  hp.Configure(s.MaxHealth,3);s.SetHealth(3,s.MaxHealth);s.SetMana(2,s.MaxMana);p.GetComponent<PlayerRuntimeState>().SynchronizeFromSession();
  var pickup=UnityEngine.Object.FindObjectsByType<CollectiblePickup>(FindObjectsSortMode.None).First();string pickupId=pickup.PersistentId;CampaignStore.MarkRemoved(pickupId);
  var wall=UnityEngine.Object.FindObjectsByType<BreakableWall>(FindObjectsSortMode.None).First();string wallId=CampaignStore.Identity(wall);wall.Break();
  CampaignStore.Purchase("test-relic");
  UnityEngine.Object.FindFirstObjectByType<ScrollJournal>().Recover("hoxi 4.23 travellers",false);
  NarrativeGuidance.Instance.RestoreSeen(new[]{"tutorial_move","tutorial_heal"});
  var explored=ExploredAtlas.Current.Export();
  Check("Save checkpoint transaction",CampaignStore.SaveNow());
  var saved=CampaignStore.Capture();File.WriteAllText(Path.Combine(Output,"expected.json"),JsonUtility.ToJson(saved,true));
  Check("Rune artwork uses all eight original icons",runes.Length==8&&runes.All(r=>r.Icon!=null&&AssetDatabase.GetAssetPath(r.Icon).Contains("/Release51/")));
  BableGameUI.Instance.Pause();yield return null;Capture("pause");
  Check("Pause preserves resources and freezes simulation",s.CurrentHealth==3&&s.CurrentMana==2&&Time.timeScale==0);
  var pauseShade=GameObject.Find("pause").GetComponent<Image>();Check("Pause remains translucent",pauseShade.color.a<.4f);
  BableGameUI.Instance.Resume();yield return new WaitForSecondsRealtime(.25f);
  Pad(pad,GamepadButton.Start);yield return null;Pad(pad);yield return null;
  Check("Controller opens pause",BableGameUI.Instance.Mode=="pause");
  BableGameUI.Instance.Resume();yield return new WaitForSecondsRealtime(.25f);Pad(pad,GamepadButton.DpadRight);yield return null;Pad(pad);InputSystem.RemoveDevice(pad);yield return null;
  Check("Disconnecting active controller pauses safely",BableGameUI.Instance.Mode=="pause");
  BableGameUI.Instance.Runes();yield return null;Capture("runes");
  var repo=UnityEngine.Object.FindFirstObjectByType<RuneRepositoryView>();Check("Four equipped slots retain exact order",Enumerable.Range(0,4).All(i=>repo.DisplayedRuneAt(i).RuneId==order[i]));
  BableGameUI.Instance.Resume();yield return new WaitForSecondsRealtime(.25f);
  BableGameUI.Instance.OpenLab();yield return Ready();
  Check("Lab has independent cheats",GameSession.Instance.CurrentGold==999999&&GameSession.Instance.RuneInventory.CollectedRunes.Count==8);
  bool read=CampaignStore.TryRead(out var fromDisk);Check("Lab cannot overwrite campaign",read&&fromDisk.player.gold==saved.player.gold&&fromDisk.player.mana==saved.player.mana);
  BableGameUI.Instance.ReturnToCampaign();yield return Ready();s=GameSession.Instance;
  Check("Continue restores gold, health and mana",s.CurrentGold==saved.player.gold&&s.CurrentHealth==3&&s.CurrentMana==2);
  Check("Continue restores abilities and permanent upgrades",s.HasWeapon&&s.HasAbility(AbilityId.Shockwave)&&s.MaxMana==saved.player.bonusMana+3&&s.MaxHealth==saved.player.bonusHealth+6);
  Check("Continue restores rune order",Enumerable.Range(0,4).All(i=>s.RuneInventory.ActiveSlots[i]?.RuneId==order[i]));
  Check("Collected object remains absent",!UnityEngine.Object.FindObjectsByType<CollectiblePickup>(FindObjectsSortMode.None).Any(o=>o.PersistentId==pickupId));
  Check("Broken wall remains open",!UnityEngine.Object.FindObjectsByType<BreakableWall>(FindObjectsSortMode.None).Any(o=>CampaignStore.Identity(o)==wallId&&o.GetComponent<Collider2D>().enabled));
  Check("Defeated boss does not respawn",!UnityEngine.Object.FindObjectsByType<BossBrain>(FindObjectsSortMode.None).Any(b=>b.profile.kind==BossKind.Shockwave));
  Check("One-time purchase and journal retained",CampaignStore.Purchased("test-relic")&&UnityEngine.Object.FindFirstObjectByType<ScrollJournal>().recovered.Contains("hoxi 4.23 travellers"));
  Check("Tutorials and exploration retained",NarrativeGuidance.Instance.HasSeen("tutorial_move")&&explored.All(c=>ExploredAtlas.Current.Export().Contains(c)));
  var worldHp=UnityEngine.Object.FindFirstObjectByType<PlayerController2D>().GetComponent<HealthComponent>();
  worldHp.Invincible=false;worldHp.ApplyDamage(999);yield return null;
  Check("Death opens isolated death menu",BableGameUI.Instance.Mode=="death"&&!BableGameUI.Instance.GameplayHudVisible);Capture("death");
  BableGameUI.Instance.Respawn();yield return Ready();Check("Respawn restores full resources",s.CurrentHealth==s.MaxHealth&&s.CurrentMana==s.MaxMana);
  BableGameUI.Instance.Pause();BableGameUI.Instance.MainMenu();yield return Ready();
  Check("Return to title exposes Continue",FindButton("CONTINUE JOURNEY")!=null&&GameSession.Instance==null);
  BableGameUI.Instance.Begin();yield return null;Check("New game cannot silently overwrite save",BableGameUI.Instance.Mode=="confirm");Click("CANCEL");
  // Exercise checksum recovery using an isolated fixture, never the user's real save.
  LocalStorage.Write("fixture.json","first");LocalStorage.Write("fixture.json","second");File.WriteAllText(Path.Combine(LocalStorage.Root,"fixture.json"),"truncated");
  bool recovered;var recoveredData=LocalStorage.Read("fixture.json",out recovered);
  Check("Corruption recovers and repairs previous backup",recovered&&recoveredData=="first"&&LocalStorage.ReadFile(Path.Combine(LocalStorage.Root,"fixture.json"))=="first");
  Check("Default 16:9 framing handles wide and tall displays",Mathf.Abs(DisplayFrame.Calculate(2560,1080).width-.75f)<.001f&&DisplayFrame.Calculate(1024,1024).height<.57f);
  GameInput.Action(GameAction.Attack).ApplyBindingOverride(0,"<Keyboard>/f");GameInput.SaveBindings();
  File.WriteAllText(Path.Combine(Output,"complete.txt"),"Checks: "+checks.Count+" Failures: "+failures.Count+"\n"+string.Join("\n",failures));
  SessionState.SetBool(Flag,false);EditorApplication.Exit(failures.Count==0?0:1);
 }
 static IEnumerator ReloadTest(){
  yield return Ready();
  Check("Fresh process discovers existing save",CampaignStore.HasSave&&FindButton("CONTINUE JOURNEY")!=null);
  Check("Fresh process restores remapping by stable action name",GameInput.Action(GameAction.Attack).bindings[0].effectivePath=="<Keyboard>/f");
  Check("Fresh process restores independent audio setting",Mathf.Abs(GameSettings.Current.music-.9f)<.01f);
  CampaignStore.TryRead(out var expected);Click("CONTINUE JOURNEY");yield return Ready();
  var s=GameSession.Instance;
  Check("Fresh process continues without prologue",!BableGameUI.Instance.IsTitleScene&&UnityEngine.Object.FindFirstObjectByType<TowerPrologue>()==null);
  Check("Fresh process restores campaign state",s.CurrentGold==expected.player.gold&&s.CurrentMana==expected.player.mana&&s.CurrentHealth==expected.player.health&&s.RuneInventory.CollectedRunes.Count==8&&s.HasWeapon&&s.HasAbility(AbilityId.Shockwave));
  var p=UnityEngine.Object.FindFirstObjectByType<PlayerController2D>();p.GetComponent<HealthComponent>().Invincible=true;
  var pad=InputSystem.AddDevice<Gamepad>();
  InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(GamepadButton.South));
  yield return null;yield return new WaitForSeconds(.08f);
  Check("Controller jump drives actual character",!p.IsGrounded&&p.Velocity.y>0);
  Pad(pad);yield return new WaitForSeconds(.8f);
  BableGameUI.Instance.Runes();yield return new WaitForSecondsRealtime(.25f);
  var repo=UnityEngine.Object.FindFirstObjectByType<RuneRepositoryView>();int oldCount=s.RuneInventory.ActiveSlots.Count(r=>r!=null);repo.SelectCell(0);
  InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(GamepadButton.South));yield return null;yield return new WaitForSecondsRealtime(.35f);Pad(pad);
  Check("Controller confirm removes exactly one rune",s.RuneInventory.ActiveSlots.Count(r=>r!=null)==oldCount-1);
  InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(GamepadButton.East));yield return null;Pad(pad);yield return null;
  Check("Controller back closes repository without equipping",BableGameUI.Instance.Mode=="play"&&s.RuneInventory.ActiveSlots.Count(r=>r!=null)==oldCount-1);
  BableGameUI.Instance.Pause();BableGameUI.Instance.OpenSettings();Click("DISPLAY & COMFORT");Click("SCREEN MODE VALUE");
  yield return new WaitForSecondsRealtime(15.5f);
  Check("Display timeout automatically restores previous mode",BableGameUI.Instance.Mode=="settings"&&!GameSettings.Current.fullscreen);
  Click("RETURN");BableGameUI.Instance.Resume();yield return new WaitForSecondsRealtime(.25f);
  s.SetHealth(3,s.MaxHealth);p.GetComponent<PlayerRuntimeState>().SynchronizeFromSession();
  var shop=UnityEngine.Object.FindFirstObjectByType<Babel.Runtime.Shop.ShopMenuController>();
  var stock=Resources.LoadAll<Babel.Runtime.Shop.ShopItemDefinition>("Bable/Shop");
  if(stock.Length==0)stock=Resources.LoadAll<Babel.Runtime.Shop.ShopItemDefinition>("");
  shop.Open(stock);yield return null;
  Check("Shop opens with controller-compatible navigation",shop.IsOpen&&s.IsPaused);
  InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(GamepadButton.East));yield return null;Pad(pad);yield return null;
  Check("Controller back exits shop",!shop.IsOpen&&!s.IsPaused);
  BableGameUI.Instance.Pause();BableGameUI.Instance.Practice();yield return null;Click(TowerLore.GuardianTitles[0]);yield return Ready();
  Check("Boss practice is isolated from campaign",CampaignStore.IsPractice&&GameSession.Instance!=s);
  Check("Boss practice equips a usable weapon",GameSession.Instance.HasWeapon);
  BableGameUI.Instance.Pause();Click("CHAMBER TOOLS");yield return null;Check("Controller-accessible chamber tools",BableGameUI.Instance.Mode=="tools"&&FindButton("INVULNERABILITY: OFF")!=null);Capture("chamber-tools");
  CampaignStore.TryRead(out var afterPractice);Check("Boss practice did not grant campaign powers",afterPractice.player.gold==s.CurrentGold);
  BableGameUI.Instance.ReturnToCampaign();yield return Ready();Check("Return from boss practice restores journey",CampaignStore.IsCampaign&&GameSession.Instance.CurrentGold==expected.player.gold);
  BableGameUI.Instance.MainMenu();yield return Ready();
  // Unsupported future schemas must stay intact even when a level is opened directly.
  LocalStorage.Write("campaign.json","{\"version\":99}");string protectedFile=File.ReadAllText(Path.Combine(LocalStorage.Root,"campaign.json"));
  Check("Unsupported future save is refused",!CampaignStore.TryRead(out _));
  TowerLoading.Load("Gameplay_Main");yield return Ready();Check("Unreadable save cannot be overwritten by autosave",!CampaignStore.SaveNow()&&File.ReadAllText(Path.Combine(LocalStorage.Root,"campaign.json"))==protectedFile);
  // Restore this test fixture for subsequent verification.
  LocalStorage.Write("campaign.json",JsonUtility.ToJson(expected));
  File.WriteAllText(Path.Combine(Output,"reload-tests.json"),JsonUtility.ToJson(new BableVerification.Report{checks=checks.ToArray(),failures=failures.ToArray()},true));
  File.WriteAllText(Path.Combine(Output,"reload-complete.txt"),"Checks: "+checks.Count+" Failures: "+failures.Count+"\n"+string.Join("\n",failures));
  SessionState.SetBool("Bable51.Reload",false);SessionState.SetBool(Flag,false);EditorApplication.Exit(failures.Count==0?0:1);
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
