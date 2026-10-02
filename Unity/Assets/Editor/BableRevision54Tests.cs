using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Babel.Runtime.Characters.Player;
using Babel.Runtime.Characters.Enemies;
using Babel.Runtime.Combat;
using Babel.Runtime.Core;
using Babel.Runtime.World;
using Bable;
using Object=UnityEngine.Object;
[InitializeOnLoad] public static class BableRevision54Tests {
 const string Flag="Bable54.Tests";static readonly List<string> checks=new(),failures=new();static double started;
 static PlayerController2D player;static PlayerCombatController combat;static Rigidbody2D body;static GameSession session;static Keyboard keyboard;static Gamepad pad;
 static BableRevision54Tests(){EditorApplication.playModeStateChanged+=Changed;}
 public static void Batch(){BableRevision54.Install();SessionState.SetBool(Flag,true);SessionState.SetBool("Bable.PracticePreview",false);EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");EditorApplication.EnterPlaymode();}
 static void Changed(PlayModeStateChange state){if(state!=PlayModeStateChange.EnteredPlayMode||!SessionState.GetBool(Flag,false))return;started=EditorApplication.timeSinceStartup;EditorApplication.update+=Watch;var host=new GameObject("Directional combat regression").AddComponent<BableTestHost>();Object.DontDestroyOnLoad(host);InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;host.StartCoroutine(Test());}
 static void Watch(){if(EditorApplication.timeSinceStartup-started>240){Check("Timeout",false);Finish();}}
 static void Check(string name,bool ok){checks.Add(name);if(!ok)failures.Add(name);Debug.Log("REV54 "+(ok?"PASS ":"FAIL ")+name);File.WriteAllText("../reference/revision54/tests.json",JsonUtility.ToJson(new BableVerification.Report{checks=checks.ToArray(),failures=failures.ToArray()},true));}
 static void Set(object o,string key,object value){o.GetType().GetField(key,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(o,value);}
 static IEnumerator Ready(){yield return null;while(TowerLoading.Busy||BableGameUI.Instance==null||!BableGameUI.Instance.Ready)yield return null;yield return new WaitForSecondsRealtime(.4f);}
 static void Keys(params Key[] keys){InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));}
 static void Pad(params GamepadButton[] buttons){var state=new GamepadState();foreach(var b in buttons)state=state.WithButton(b);InputSystem.QueueStateEvent(pad,state);}
 static readonly Key[] directionKeys={Key.RightArrow,Key.LeftArrow,Key.UpArrow,Key.DownArrow};
 static readonly GamepadButton[] directionButtons={GamepadButton.DpadRight,GamepadButton.DpadLeft,GamepadButton.DpadUp,GamepadButton.DpadDown};
 static readonly Vector2[] axes={Vector2.right,Vector2.left,Vector2.up,Vector2.down};
 static void Input(bool gamepad,int direction,int attack){if(gamepad)Pad(directionButtons[direction],attack==0?GamepadButton.West:attack==1?GamepadButton.North:GamepadButton.RightShoulder);else Keys(directionKeys[direction],attack==0?Key.X:attack==1?Key.Z:Key.LeftShift);}
 static void Neutral(){Keys();Pad();}
 static void Place(Vector2 position,bool frozen){player.InterruptForHit();combat.InterruptForHit();player.GetComponent<HitRecoil>().Cancel();body.constraints=frozen?RigidbodyConstraints2D.FreezeAll:RigidbodyConstraints2D.FreezeRotation;body.gravityScale=frozen?0:3;body.position=position;player.transform.position=position;body.linearVelocity=Vector2.zero;Physics2D.SyncTransforms();}
 static IEnumerator Test(){
  yield return Ready();keyboard=InputSystem.AddDevice<Keyboard>();pad=InputSystem.AddDevice<Gamepad>();var mouse=InputSystem.AddDevice<Mouse>();
  LocalStorage.Write("bindings.json",JsonUtility.ToJson(new GameInput.BindingsFile{entries=new[]{new GameInput.BindingEntry{action="Up",index=0,path="<Mouse>/leftButton"},new GameInput.BindingEntry{action="Attack",index=0,path="<Keyboard>/f"}}}));GameInput.ReloadBindings();
  Check("Repair mistaken mouse direction without resetting customized attack",GameInput.Action(GameAction.Up).bindings[0].effectivePath=="<Keyboard>/upArrow"&&GameInput.Action(GameAction.Attack).bindings[0].effectivePath=="<Keyboard>/f");
  GameInput.ReloadBindings();Check("Direction repair survives reload",GameInput.Action(GameAction.Up).bindings[0].effectivePath=="<Keyboard>/upArrow");GameInput.ResetBindings();
  bool completed=false;GameInput.Rebind(GameAction.Up,false,()=>completed=true);yield return new WaitForSecondsRealtime(.25f);
  InputSystem.QueueStateEvent(mouse,new MouseState().WithButton(MouseButton.Left));yield return null;InputSystem.QueueStateEvent(mouse,new MouseState());yield return null;
  Check("Opening a direction binding cannot assign mouse click",GameInput.Rebinding&&!completed);Keys(Key.W);yield return null;Keys();yield return new WaitForSecondsRealtime(.3f);Check("Directions accept deliberate keyboard remap",completed&&GameInput.Action(GameAction.Up).bindings[0].effectivePath=="<Keyboard>/w");GameInput.ResetBindings();
  GameInput.Rebind(GameAction.Attack,false,()=>{});InputSystem.QueueStateEvent(mouse,new MouseState().WithButton(MouseButton.Left));yield return null;InputSystem.QueueStateEvent(mouse,new MouseState());yield return new WaitForSecondsRealtime(.25f);Check("Activation click is ignored during capture guard",GameInput.Rebinding);Keys(Key.F);yield return null;Keys();yield return new WaitForSecondsRealtime(.3f);Check("Deliberate attack binding still works",GameInput.Action(GameAction.Attack).bindings[0].effectivePath=="<Keyboard>/f");GameInput.ResetBindings();
  GameInput.Rebind(GameAction.Attack,true,()=>{});yield return new WaitForSecondsRealtime(.25f);Pad(GamepadButton.RightStick);yield return null;Pad();yield return new WaitForSecondsRealtime(.3f);Check("Controller capture still works",!GameInput.Rebinding&&GameInput.Action(GameAction.Attack).bindings[1].effectivePath=="<Gamepad>/rightStickPress");GameInput.ResetBindings();
  BableGameUI.Instance.Begin();yield return null;Object.FindFirstObjectByType<TowerPrologue>().Skip();yield return Ready();
  player=Object.FindFirstObjectByType<PlayerController2D>();combat=player.GetComponent<PlayerCombatController>();body=player.GetComponent<Rigidbody2D>();session=GameSession.Instance;session.UnlockWeapon();foreach(AbilityId a in Enum.GetValues(typeof(AbilityId)))session.UnlockAbility(a);
  foreach(var speech in Object.FindObjectsByType<BossSpeech>(FindObjectsSortMode.None))speech.enabled=false;
  var enemies=Object.FindObjectsByType<EnemyControllerBase>(FindObjectsSortMode.None);foreach(var e in enemies){e.enabled=false;e.CancelInvoke();e.GetComponent<Rigidbody2D>().constraints=RigidbodyConstraints2D.FreezeAll;var encounter=e.GetComponent<BossEncounter>();if(encounter!=null)encounter.enabled=false;}
  var enemy=enemies.First(e=>e is MeleeEnemyController);var health=enemy.GetComponent<HealthComponent>();var box=enemy.GetComponent<BoxCollider2D>();box.size=Vector2.one*.6f;box.offset=Vector2.zero;
  player.GetComponent<HealthComponent>().Invincible=true;
  for(int device=0;device<2;device++)for(int kind=0;kind<3;kind++)for(int direction=0;direction<4;direction++){
   Neutral();Place(new Vector2(850,100),true);Set(combat,"meleeReadyTime",0f);Set(combat,"shockwaveReadyTime",0f);Set(combat,"dashReadyTime",0f);session.RestoreMana(100);health.Configure(100,100);health.Invincible=false;enemy.GetComponent<HitRecoil>()?.Cancel();
   Vector2 target=(Vector2)player.transform.position+axes[direction]*(kind==0?1.05f:kind==1?4f:2f);if(kind==1&&Mathf.Abs(axes[direction].y)>.5f)target.x+=player.FacingSign*.85f;
   enemy.transform.position=target;enemy.GetComponent<Rigidbody2D>().position=target;Physics2D.SyncTransforms();yield return new WaitForFixedUpdate();yield return null;
   if(kind==2){body.constraints=RigidbodyConstraints2D.FreezeRotation;body.gravityScale=0;}
   int mana=session.CurrentMana;Vector2 start=body.position;Input(device==1,direction,kind);yield return null;
   string name=(device==0?"Keyboard":"Controller")+" "+(kind==0?"melee":kind==1?"shockwave":"dash")+" "+axes[direction];
   if(kind==0){Neutral();yield return new WaitForSeconds(.4f);Check(name+" damages actual enemy",health.CurrentHealth<100);}
   else if(kind==1){yield return new WaitForSeconds(.25f);Neutral();Check(name+" locks intended direction",combat.IsShockwaveActive&&combat.LastShockwaveAxis==axes[direction]);yield return new WaitForSeconds(.5f);Check(name+" reaches enemy and consumes one spirit",health.CurrentHealth<100&&session.CurrentMana==mana-1&&combat.LastShockwaveDistance>4);combat.InterruptForHit();}
   else {float until=Time.time+1.2f;while(!player.IsDashing&&Time.time<until)yield return null;Check(name+" launches in intended direction",player.IsDashing&&Vector2.Dot(body.linearVelocity.normalized,axes[direction])>.95f);Neutral();yield return new WaitForSeconds(.25f);Check(name+" moves and damages enemy",Vector2.Dot(body.position-start,axes[direction])>1&&health.CurrentHealth<100);}
   Neutral();yield return new WaitForSeconds(.3f);
  }
  // The checkpoint trigger, persisted location and actual respawn must all agree.
  enemy.transform.position=new Vector2(950,100);Neutral();yield return null;
  var altars=Object.FindObjectsByType<AltarCheckpoint>(FindObjectsSortMode.None).Where(a=>a.name.StartsWith("Approach altar - ")).ToArray();Check("All five Bosses have approach altars",altars.Length==5);
  foreach(var altar in altars){Place(altar.PlayerPosition(player),false);yield return new WaitForSeconds(.45f);var boss=enemies.OfType<BossBrain>().First(b=>altar.name.EndsWith(b.profile.kind.ToString()));
   Check(altar.name+" activates before encounter on solid ground",player.IsGrounded&&altar.IsActive&&Object.FindFirstObjectByType<CheckpointService>().CurrentCheckpoint==altar.Marker&&!boss.InArena(player.transform.position));
   var saved=JsonUtility.FromJson<CampaignStore.Data>(LocalStorage.ReadFile(Path.Combine(LocalStorage.Root,"campaign.json")));Check(altar.name+" immediately persists respawn point",Vector2.Distance(saved.player.checkpoint,altar.transform.position)<.05f);
   Place((Vector2)player.transform.position+Vector2.right*4,false);player.GetComponent<PlayerRespawnController>().Respawn(false);yield return new WaitForSeconds(.15f);Check(altar.name+" respawns safely",player.IsGrounded&&Vector2.Distance(player.transform.position,altar.PlayerPosition(player))<.2f);
   Camera.main.GetComponent<CameraFollow2D>()?.SetTarget(player.transform);BableVerification.Capture("revision54/altar-"+boss.profile.kind+".png");
  }
  Finish();
 }
 static void Finish(){SessionState.SetBool(Flag,false);EditorApplication.Exit(failures.Count==0?0:1);}
}
