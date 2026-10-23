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

[InitializeOnLoad] public static class BableRevision56Tests {
 const string Flag="Bable56.Tests";
 static readonly List<string> checks=new(),failures=new();
 static PlayerController2D player;static PlayerCombatController combat;static GameSession session;static Rigidbody2D body;
 static Keyboard keyboard;static Gamepad pad;static double started;
 static BableRevision56Tests(){EditorApplication.playModeStateChanged+=Changed;}
 public static void Batch(){Directory.CreateDirectory("../reference/revision56");SessionState.SetBool(Flag,true);SessionState.SetBool("Bable.PracticePreview",false);EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");EditorApplication.EnterPlaymode();}
 static void Changed(PlayModeStateChange state){if(state!=PlayModeStateChange.EnteredPlayMode||!SessionState.GetBool(Flag,false))return;started=EditorApplication.timeSinceStartup;EditorApplication.update+=Watch;var host=new GameObject("Dash Spirit regression").AddComponent<BableTestHost>();Object.DontDestroyOnLoad(host);InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;host.StartCoroutine(Test());}
 static void Watch(){if(EditorApplication.timeSinceStartup-started>180){Check("Timeout",false);Finish();}}
 static void Check(string name,bool ok){checks.Add(name);if(!ok)failures.Add(name);Debug.Log("REV56 "+(ok?"PASS ":"FAIL ")+name);File.WriteAllText("../reference/revision56/tests.json",JsonUtility.ToJson(new BableVerification.Report{checks=checks.ToArray(),failures=failures.ToArray()},true));}
 static void Set(object o,string name,object value)=>o.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(o,value);
 static void Keys(params Key[] keys)=>InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));
 static void Pad(params GamepadButton[] buttons){var s=new GamepadState();foreach(var b in buttons)s=s.WithButton(b);InputSystem.QueueStateEvent(pad,s);}
 static void Neutral(){Keys();Pad();}
 static IEnumerator Ready(){yield return null;while(TowerLoading.Busy||BableGameUI.Instance==null||!BableGameUI.Instance.Ready)yield return null;yield return new WaitForSecondsRealtime(.4f);}
 static IEnumerator Reset(int mana){Neutral();player.InterruptForHit();combat.InterruptForHit();player.GetComponent<HitRecoil>().Cancel();Set(combat,"dashReadyTime",0f);session.SetMana(mana,session.MaxMana);body.constraints=RigidbodyConstraints2D.FreezeRotation;body.gravityScale=0;body.position=new Vector2(850,100);player.transform.position=body.position;body.linearVelocity=Vector2.zero;Physics2D.SyncTransforms();yield return null;yield return null;}
 static IEnumerator Test(){
  yield return Ready();keyboard=InputSystem.AddDevice<Keyboard>();pad=InputSystem.AddDevice<Gamepad>();GameInput.ResetBindings();
  BableGameUI.Instance.Begin();yield return null;Object.FindFirstObjectByType<TowerPrologue>().Skip();yield return Ready();
  player=Object.FindFirstObjectByType<PlayerController2D>();combat=player.GetComponent<PlayerCombatController>();body=player.GetComponent<Rigidbody2D>();session=GameSession.Instance;
  session.UnlockWeapon();foreach(AbilityId a in Enum.GetValues(typeof(AbilityId)))session.UnlockAbility(a);
  foreach(var e in Object.FindObjectsByType<EnemyControllerBase>(FindObjectsSortMode.None)){e.enabled=false;e.CancelInvoke();e.GetComponent<Rigidbody2D>().constraints=RigidbodyConstraints2D.FreezeAll;var encounter=e.GetComponent<BossEncounter>();if(encounter!=null)encounter.enabled=false;}
  foreach(var speech in Object.FindObjectsByType<BossSpeech>(FindObjectsSortMode.None))speech.enabled=false;
  player.GetComponent<HealthComponent>().Invincible=true;
  Check("Shared player definition costs one Spirit",player.Definition.CrystalDashManaCost==1);
  var dirs=new[]{Key.RightArrow,Key.LeftArrow,Key.UpArrow,Key.DownArrow};var buttons=new[]{GamepadButton.DpadRight,GamepadButton.DpadLeft,GamepadButton.DpadUp,GamepadButton.DpadDown};var axes=new[]{Vector2.right,Vector2.left,Vector2.up,Vector2.down};
  for(int device=0;device<2;device++)for(int d=0;d<4;d++){
   yield return Reset(1);if(device==0)Keys(dirs[d],Key.LeftShift);else Pad(buttons[d],GamepadButton.RightShoulder);
   yield return new WaitForSeconds(.3f);Check($"{device}/{d} charging reserves no Spirit",player.IsCharging&&session.CurrentMana==1);
   float deadline=Time.time+1;while(!player.IsDashing&&Time.time<deadline)yield return null;
   Check($"{device}/{d} launch spends exactly one and keeps direction",player.IsDashing&&session.CurrentMana==0&&Vector2.Dot(body.linearVelocity.normalized,axes[d])>.95f);
   yield return new WaitForSeconds(2);Check($"{device}/{d} held input cannot auto-repeat or go negative",!player.IsDashing&&!player.IsCharging&&session.CurrentMana==0);Neutral();
  }
  yield return Reset(2);Keys(Key.LeftShift);yield return new WaitForSeconds(.25f);Neutral();yield return new WaitForSeconds(.7f);Check("Early release cancels freely",!player.IsDashing&&!player.IsCharging&&session.CurrentMana==2);
  yield return Reset(0);Keys(Key.LeftShift);yield return new WaitForSeconds(.2f);Check("Empty meter rejects charge and shows HUD feedback",!player.IsCharging&&!player.IsDashing&&session.CurrentMana==0&&Object.FindFirstObjectByType<VotiveHud>().SpiritWarningVisible);Neutral();
  yield return Reset(1);Keys(Key.LeftShift);yield return new WaitForSeconds(.3f);session.TrySpendMana(1);yield return new WaitForSeconds(.5f);Check("Spirit is rechecked at launch",!player.IsDashing&&session.CurrentMana==0);Neutral();
  yield return Reset(2);Check("Direct launch spends once",combat.PerformCrystalDash()&&session.CurrentMana==1);Check("Active dash rejection is free",!combat.PerformCrystalDash()&&session.CurrentMana==1);player.InterruptForHit();Check("Cooldown rejection is free",!combat.PerformCrystalDash()&&session.CurrentMana==1);
  yield return Reset(2);Keys(Key.LeftShift);yield return new WaitForSeconds(.2f);combat.InterruptForHit();yield return new WaitForSeconds(.8f);Check("Interrupted charge is free and needs release",!player.IsCharging&&!player.IsDashing&&session.CurrentMana==2);Neutral();
  yield return Reset(2);session.SetPaused(true);Check("Paused launch is rejected without charge",!combat.PerformCrystalDash()&&session.CurrentMana==2);session.SetPaused(false);
  yield return Reset(2);player.enabled=false;Check("Disabled controller launch is free",!combat.PerformCrystalDash()&&session.CurrentMana==2);player.enabled=true;
  yield return Reset(2);combat.PerformCrystalDash();CampaignStore.SaveNow();var saved=JsonUtility.FromJson<CampaignStore.Data>(LocalStorage.ReadFile(Path.Combine(LocalStorage.Root,"campaign.json")));Check("Campaign save keeps spent Spirit",saved.player.mana==1);
  yield return Reset(0);player.GetComponent<PlayerRespawnController>().Respawn(false);yield return new WaitForSeconds(.2f);Check("Checkpoint return restores Spirit for recovery",session.CurrentMana==session.MaxMana);
  int cap=session.MaxMana;Check("Boss capacity reward still applies once",session.GrantBossMana("dash-cost-test")&&session.MaxMana==cap+1&&!session.GrantBossMana("dash-cost-test")&&session.MaxMana==cap+1);
  // Real campaign terrain and a live HUD capture; the test save remains isolated.
  Neutral();player.InterruptForHit();combat.InterruptForHit();Set(combat,"dashReadyTime",0f);body.gravityScale=3;body.constraints=RigidbodyConstraints2D.FreezeRotation;body.position=new Vector2(6,-10.17f);player.transform.position=body.position;session.RestoreMana(99);Physics2D.SyncTransforms();Camera.main.GetComponent<CameraFollow2D>()?.SetTarget(player.transform);yield return new WaitForSeconds(.5f);
  BableVerification.Capture("revision56/dash-before.png");int before=session.CurrentMana;Keys(Key.RightArrow,Key.LeftShift);float end=Time.time+1.3f;while(!player.IsDashing&&Time.time<end)yield return null;Neutral();yield return new WaitForSeconds(.1f);Check("Campaign terrain dash spends one",session.CurrentMana==before-1);BableVerification.Capture("revision56/dash-after.png");
  yield return new WaitForSeconds(1.5f);session.SetMana(0,session.MaxMana);Keys(Key.LeftShift);yield return new WaitForSeconds(.2f);Neutral();BableVerification.Capture("revision56/dash-empty.png");Finish();
 }
 static void Finish(){SessionState.SetBool(Flag,false);EditorApplication.Exit(failures.Count==0?0:1);}
}
