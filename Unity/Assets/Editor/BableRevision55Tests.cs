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
[InitializeOnLoad] public static class BableRevision55Tests {
 const string Flag="Bable55.Tests";static readonly List<string> checks=new(),failures=new();static double started;
 static PlayerController2D player;static HealthComponent hp;static Keyboard keyboard;static BossBrain[] bosses;static Vector2[] centers,sizes,positions;
 static BableRevision55Tests(){EditorApplication.playModeStateChanged+=Changed;}
 public static void Batch(){Directory.CreateDirectory("../reference/revision55");BableRevision55.Install();SessionState.SetBool(Flag,true);SessionState.SetBool("Bable.PracticePreview",false);EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");EditorApplication.EnterPlaymode();}
 static void Changed(PlayModeStateChange s){if(s!=PlayModeStateChange.EnteredPlayMode||!SessionState.GetBool(Flag,false))return;started=EditorApplication.timeSinceStartup;EditorApplication.update+=Watch;var host=new GameObject("Boss regression 55").AddComponent<BableTestHost>();Object.DontDestroyOnLoad(host);InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;host.StartCoroutine(Test());}
 static void Watch(){if(EditorApplication.timeSinceStartup-started>220){Check("Timeout",false);Finish();}}
 static void Check(string name,bool ok){checks.Add(name);if(!ok)failures.Add(name);Debug.Log("REV55 "+(ok?"PASS ":"FAIL ")+name);File.WriteAllText("../reference/revision55/tests.json",JsonUtility.ToJson(new BableVerification.Report{checks=checks.ToArray(),failures=failures.ToArray()},true));}
 static void Set(object o,string key,object value){for(Type t=o.GetType();t!=null;t=t.BaseType){var f=t.GetField(key,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.DeclaredOnly);if(f!=null){f.SetValue(o,value);return;}}throw new Exception(key);}
 static object Call(object o,string key,params object[] args)=>o.GetType().GetMethod(key,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(o,args);
 static IEnumerator Ready(){yield return null;while(TowerLoading.Busy||BableGameUI.Instance==null||!BableGameUI.Instance.Ready)yield return null;yield return new WaitForSecondsRealtime(.3f);}
 static void Position(GameObject o,Vector2 p){o.transform.position=p;var b=o.GetComponent<Rigidbody2D>();b.simulated=true;b.gravityScale=0;b.constraints=RigidbodyConstraints2D.FreezeAll;b.position=p;b.linearVelocity=Vector2.zero;Physics2D.SyncTransforms();}
 static void Hold(BossBrain b,float time=100)=>Call(b,"Change","Recover",time,"idle");
 static void Setup(BossBrain b,Vector2 delta){foreach(var other in bosses){other.enabled=false;other.GetComponent<Rigidbody2D>().simulated=false;}b.ResetEncounter();b.DialogueHold=false;b.arenaCenter=new Vector2(850,100);b.arenaSize=new Vector2(40,24);Position(b.gameObject,new Vector2(850,100));Position(player.gameObject,b.AttackCenter+delta);Set(b,"target",player.transform);Set(b,"<Engaged>k__BackingField",true);b.GetComponent<HealthComponent>().Invincible=false;b.enabled=true;Hold(b,0);hp.Configure(500,500);hp.Invincible=false;}
 static IEnumerator Test(){
  yield return Ready();keyboard=InputSystem.AddDevice<Keyboard>();GameInput.ResetBindings();BableGameUI.Instance.Begin();yield return null;Object.FindFirstObjectByType<TowerPrologue>().Skip();yield return Ready();
  player=Object.FindFirstObjectByType<PlayerController2D>();hp=player.GetComponent<HealthComponent>();GameSession.Instance.UnlockWeapon();foreach(AbilityId a in Enum.GetValues(typeof(AbilityId)))GameSession.Instance.UnlockAbility(a);
  foreach(var speech in Object.FindObjectsByType<BossSpeech>(FindObjectsSortMode.None))speech.enabled=false;TowerDialogue.AbortStory();
  var all=Object.FindObjectsByType<EnemyControllerBase>(FindObjectsSortMode.None);bosses=all.OfType<BossBrain>().OrderBy(b=>(int)b.profile.kind).ToArray();foreach(var e in all){e.enabled=false;e.GetComponent<Rigidbody2D>().simulated=false;var encounter=e.GetComponent<BossEncounter>();if(encounter!=null)encounter.enabled=false;}
  Check("Campaign contains original five Boss identities",bosses.Length==5);
  centers=bosses.Select(b=>b.arenaCenter).ToArray();sizes=bosses.Select(b=>b.arenaSize).ToArray();positions=bosses.Select(b=>(Vector2)b.transform.position).ToArray();
  // Actual controls drive vertical movement and the renderer, not a direct Animator.Play.
  for(int i=0;i<2;i++){
   Position(player.gameObject,new Vector2(850,100));player.InterruptForHit();player.GetComponent<HitRecoil>().Cancel();var body=player.GetComponent<Rigidbody2D>();body.constraints=RigidbodyConstraints2D.FreezeRotation;
   InputSystem.QueueStateEvent(keyboard,new KeyboardState(i==0?Key.UpArrow:Key.DownArrow,Key.LeftShift));float timeout=Time.time+1.4f;while(!player.IsDashing&&Time.time<timeout)yield return null;
   yield return null;var a=player.GetComponent<CharacterPresentation>().animator;string state=i==0?"rightdashup":"rightdashdown";
   Check(state+" selected by live directional input",player.IsDashing&&a.GetCurrentAnimatorStateInfo(0).IsName(state));
   Check(state+" uses authored sheet without transform rotation",a.GetComponent<SpriteRenderer>().sprite.name.StartsWith("PilgrimVerticalDash55")&&Quaternion.Angle(a.transform.localRotation,Quaternion.identity)<.01f);
   BableVerification.Capture("revision55/"+state+".png");InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return new WaitForSeconds(1);
  }
  player.InterruptForHit();player.enabled=false;player.GetComponent<HitRecoil>().enabled=false;
  var first=bosses[0];Setup(first,new Vector2(2,0));var attacks=new List<string>();int epoch=-1;float end=Time.time+12;
  while(Time.time<end){if(first.State=="Windup"&&first.AttackEpoch!=epoch){epoch=first.AttackEpoch;attacks.Add(first.CurrentAttack);}yield return null;}
  Check("Abaddon alternates heavy and quick against close stationary player",attacks.Contains("Heavy")&&attacks.Contains("Quick"));
  Check("Abaddon never repeats same close attack consecutively",attacks.Zip(attacks.Skip(1),(a,b)=>a!=b).All(x=>x));
  foreach(var b in new[]{bosses[0],bosses[4]}){
   Setup(b,Vector2.up*2.25f);yield return new WaitForSeconds(.2f);Check(b.profile.kind+" chooses overhead counter",b.State=="Windup"&&b.CurrentAttack=="AntiAir");int before=hp.CurrentHealth;
   yield return new WaitForSeconds(.35f);Check(b.profile.kind+" overhead telegraph is harmless",hp.CurrentHealth==before);
   Time.timeScale=0;float progress=b.StateProgress;yield return new WaitForSecondsRealtime(.2f);Check(b.profile.kind+" pause freezes windup",Mathf.Abs(progress-b.StateProgress)<.001f&&hp.CurrentHealth==before);Time.timeScale=1;
   yield return new WaitForSeconds(.65f);Check(b.profile.kind+" authored overhead blade hits elevated player",hp.CurrentHealth<before);Hold(b);
   Position(player.gameObject,b.AttackCenter+Vector2.up*4.5f);before=hp.CurrentHealth;Call(b,"Windup","AntiAir",.1f);yield return new WaitForSeconds(.65f);Check(b.profile.kind+" anti-air does not extend to distant player",hp.CurrentHealth==before);
   Hold(b);Position(player.gameObject,b.AttackCenter+Vector2.up*2.25f);var wall=new GameObject("Anti-air terrain guard");wall.transform.position=b.AttackCenter+Vector2.up;wall.AddComponent<BoxCollider2D>().size=new Vector2(7,.2f);Physics2D.SyncTransforms();before=hp.CurrentHealth;Call(b,"Windup","AntiAir",.1f);yield return new WaitForSeconds(.65f);Check(b.profile.kind+" anti-air blocked by terrain",hp.CurrentHealth==before);Object.Destroy(wall);yield return null;
  }
  Setup(first,new Vector2(5,0));yield return new WaitForSeconds(.2f);Check("Abaddon selects reachable visible shockwave at mid range",first.CurrentAttack=="Wave");first.enabled=false;
  var sky=bosses[2];Setup(sky,new Vector2(0,2));var choices=new HashSet<string>();epoch=-1;end=Time.time+10;
  while(Time.time<end){if(sky.State=="Windup"&&sky.AttackEpoch!=epoch){epoch=sky.AttackEpoch;choices.Add(sky.CurrentAttack);}yield return null;}
  Check("Aerial Boss mixes rising and jumping responses to overhead player",choices.Contains("Rising")&&choices.Count>=2);sky.enabled=false;
  var nero=bosses[4];Setup(nero,new Vector2(3,0));var nh=nero.GetComponent<HealthComponent>();nh.Configure(nero.profile.health,nero.profile.health/2+1);yield return null;Check("Nero remains phase one above half",!nero.PhaseTwo&&nero.State!="PhaseChange");
  yield return new WaitForSeconds(.3f);nero.TryReceiveDamage(new DamageInfo(999,player.transform.position,Vector2.zero,player.gameObject,TeamAlignment.Player));yield return null;
  Check("Large actual hit stops at half and starts phase change",nh.CurrentHealth==nero.profile.health/2&&nero.PhaseTwo&&nero.State=="PhaseChange");
  yield return new WaitForSeconds(.5f);Check("Nero plays dedicated transformation animation",nero.GetComponent<CharacterPresentation>().animator.GetCurrentAnimatorStateInfo(0).IsName("rightphasechange"));
  int phaseHealth=nh.CurrentHealth;nero.TryReceiveDamage(new DamageInfo(999,player.transform.position,Vector2.zero,player.gameObject,TeamAlignment.Player));Check("Transformation cannot be skipped by continued attacks",nh.CurrentHealth==phaseHealth&&nh.Invincible);
  yield return new WaitForSeconds(1.05f);Check("Transformation lasts at least 1.5 seconds",nero.State=="PhaseChange");yield return new WaitForSeconds(.45f);Check("Transformation ends and restores damageability",nero.State!="PhaseChange"&&!nh.Invincible);
  yield return new WaitForSeconds(.5f);Check("Phase two selects fan even against close player",nero.CurrentAttack=="Fan"&&nero.State=="Windup");
  foreach(var p in Object.FindObjectsByType<EnemyProjectile>(FindObjectsSortMode.None))Object.Destroy(p.gameObject);
  Position(player.gameObject,new Vector2(862,100));float limit=Time.time+3;while(nero.State=="Windup"&&Time.time<limit)yield return null;yield return new WaitForSeconds(.13f);
  Check("Phase two releases eleven visible projectiles",Object.FindObjectsByType<EnemyProjectile>(FindObjectsSortMode.None).Count(p=>p.effectArt=="ArcaneOrb")==11);
  Hold(nero);yield return new WaitForSeconds(.3f);int old=nh.CurrentHealth;nero.TryReceiveDamage(new DamageInfo(1,player.transform.position,Vector2.zero,player.gameObject,TeamAlignment.Player));yield return null;Check("Phase two takes damage and does not transform again",nh.CurrentHealth==old-1&&nero.State!="PhaseChange");
  nero.ResetEncounter();Check("Encounter reset restores phase one and health",!nero.PhaseTwo&&nh.CurrentHealth==nero.profile.health&&!nh.Invincible);
  foreach(var b in bosses){Setup(b,new Vector2(2,0));Hold(b);yield return new WaitForSeconds(.3f);Check(b.profile.kind+" body contact causes no damage",hp.CurrentHealth==500);Check(b.profile.kind+" has complete non-null animation sprite references",b.GetComponent<CharacterPresentation>().animator.runtimeAnimatorController.animationClips.All(c=>AnimationUtility.GetObjectReferenceCurveBindings(c).All(k=>AnimationUtility.GetObjectReferenceCurve(c,k).All(v=>v.value!=null))));if(b.profile.kind!=BossKind.Nero)Check(b.profile.kind+" retains original single-phase design",!b.PhaseTwo);}
  foreach(var b in bosses)b.enabled=false;
  // Exercise each authored campaign room (real terrain and room boundaries).
  for(int i=0;i<bosses.Length;i++){
   var b=bosses[i];b.arenaCenter=centers[i];b.arenaSize=sizes[i];b.ResetEncounter();b.DialogueHold=false;
   var bh=b.GetComponent<HealthComponent>();bh.Invincible=b.profile.kind==BossKind.Burrow;
   var bb=b.GetComponent<Rigidbody2D>();bb.constraints=RigidbodyConstraints2D.FreezeRotation;bb.gravityScale=b.profile.kind==BossKind.Crystal?0:3;
   var pos=positions[i]+Vector2.right*2;var box=player.GetComponent<BoxCollider2D>();
   Position(player.gameObject,pos);if(TerrainMotion.FindLanding(box,pos,out var floor))Position(player.gameObject,floor);
   var pb=player.GetComponent<Rigidbody2D>();pb.constraints=RigidbodyConstraints2D.FreezeRotation;pb.gravityScale=3;player.enabled=true;hp.Configure(500,500);hp.Invincible=true;
   Camera.main.GetComponent<CameraFollow2D>().SetTarget(b.transform);
   b.enabled=true;b.GetComponent<BossEncounter>().enabled=true;yield return new WaitForSeconds(1.2f);
   Check(b.profile.kind+" engages inside authored campaign room",b.Engaged);
   var states=new HashSet<string>();end=Time.time+4;while(Time.time<end){states.Add(b.State);yield return null;}
   Check(b.profile.kind+" advances attacks on actual campaign terrain",states.Count>=2);
   BableVerification.Capture("revision55/campaign-"+b.profile.kind+".png");
   b.enabled=false;Hold(b);var presentation=b.GetComponent<CharacterPresentation>();presentation.enabled=false;var animator=presentation.animator;animator.enabled=false;
   string[] clips=i==0?new[]{"quick","antiair"}:i==4?new[]{"antiair","phasechange","slamright","slamleft"}:i==2?new[]{"rising","airside","airdown"}:i==1?new[]{"attack","emerge"}:new[]{"cast"};
   Position(b.gameObject,positions[i]);if(b.profile.kind==BossKind.Burrow)animator.GetComponent<SpriteRenderer>().enabled=true;
   Camera.main.GetComponent<CameraFollow2D>().SetTarget(b.transform);Camera.main.orthographicSize=5;
   foreach(var action in clips){var clip=animator.runtimeAnimatorController.animationClips.First(c=>c.name==animator.runtimeAnimatorController.name+"_"+action);clip.SampleAnimation(animator.gameObject,clip.length*.5f);BableVerification.Capture("revision55/room-"+b.profile.kind+"-"+action+".png");}
   presentation.enabled=true;animator.enabled=true;b.GetComponent<BossEncounter>().enabled=false;b.ResetEncounter();b.enabled=false;bb.simulated=false;
  }
  Finish();
 }
 static void Finish(){Time.timeScale=1;SessionState.SetBool(Flag,false);EditorApplication.Exit(failures.Count==0?0:1);}
}
