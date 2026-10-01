using System;using System.Collections;using System.Collections.Generic;using System.IO;using System.Linq;using System.Reflection;
using UnityEngine;using UnityEditor;using Bable;using Babel.Runtime.Combat;using Babel.Runtime.Core;using Babel.Runtime.Characters.Player;using Babel.Runtime.Characters.Enemies;
public static class BableRevision48Tests {
 static readonly List<string> checks=new(),failures=new();
 static void Check(string n,bool ok){checks.Add(n);if(!ok)failures.Add(n);File.WriteAllText("../reference/revision48/tests.json",JsonUtility.ToJson(new BableVerification.Report{checks=checks.ToArray(),failures=failures.ToArray()},true));}
 static void Set(object o,string n,object v){for(Type t=o.GetType();t!=null;t=t.BaseType){var f=t.GetField(n,BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.DeclaredOnly);if(f!=null){f.SetValue(o,v);return;}}throw new Exception(n);}
 static object Call(object o,string n,params object[] v)=>o.GetType().GetMethod(n,BindingFlags.NonPublic|BindingFlags.Instance).Invoke(o,v);
 public static void Run(){checks.Clear();failures.Clear();File.WriteAllText("../reference/revision48/complete.txt","RUNNING");new GameObject("Revision48 checks").AddComponent<BableTestHost>().StartCoroutine(Test());}
 static IEnumerator Test(){
  while(TowerLoading.Busy||BableGameUI.Instance==null||!BableGameUI.Instance.Ready)yield return null;
  TowerDialogue.AbortStory();BableGameUI.Instance.Resume();var session=GameSession.Instance;session.ResetForNewGame();session.UnlockWeapon();session.UnlockAbility(AbilityId.Shockwave);session.UnlockAbility(AbilityId.DoubleJump);
  var p=UnityEngine.Object.FindFirstObjectByType<PlayerController2D>();var health=p.GetComponent<HealthComponent>();var combat=p.GetComponent<PlayerCombatController>();var feedback=p.GetComponent<PlayerHitFeedback>();
  foreach(var e in UnityEngine.Object.FindObjectsByType<EnemyControllerBase>(FindObjectsSortMode.None)){e.enabled=false;e.CancelInvoke();e.GetComponent<Rigidbody2D>().constraints=RigidbodyConstraints2D.FreezeAll;var en=e.GetComponent<BossEncounter>();if(en!=null)en.enabled=false;}
  foreach(var s in UnityEngine.Object.FindObjectsByType<BossSpeech>(FindObjectsSortMode.None))s.enabled=false;
  p.GetComponent<Rigidbody2D>().constraints=RigidbodyConstraints2D.FreezeAll;p.SetTestInput(0,0);p.transform.position=new Vector3(850,100);p.GetComponent<Rigidbody2D>().position=p.transform.position;Camera.main.GetComponent<Babel.Runtime.World.CameraFollow2D>().SettleAtTarget();Physics2D.SyncTransforms();yield return null;
  var clips=Resources.LoadAll<AudioClip>("Bable/Sfx48");Check("All 84 authored cues imported",clips.Length==84);
  foreach(var c in clips)Check("Audio "+c.name+" loads at 48k with nonzero duration",c.frequency==48000&&c.length>.015f&&CombatAudio.Load(c.name)!=null);
  Check("Player feedback installed",feedback!=null);int before=feedback.HitCount;health.Invincible=true;health.ReceiveDamage(new DamageInfo(1,p.transform.position,Vector2.right,p.gameObject,TeamAlignment.Enemy));Check("Invulnerable player produces no screen response",feedback.HitCount==before);
  health.Invincible=false;health.Configure(30,30);Set(health,"invulnerableUntil",0f);health.ReceiveDamage(new DamageInfo(1,p.transform.position-Vector3.right,Vector2.right,p.gameObject,TeamAlignment.Enemy));
  Check("Accepted damage flashes screen",feedback.HitCount==before+1&&feedback.FlashAlpha>.1f);Check("Accepted damage shakes camera",PlayerHitFeedback.CameraOffset.sqrMagnitude>0);Check("Player hit sound plays",CombatAudio.LastCue.StartsWith("player_hurt_"));
  health.ReceiveDamage(new DamageInfo(1,p.transform.position,Vector2.left,p.gameObject,TeamAlignment.Enemy));Check("Invulnerability-window rejected hit does not retrigger",feedback.HitCount==before+1);yield return new WaitForSeconds(.35f);Check("Flash and shake settle to zero",feedback.FlashAlpha==0&&PlayerHitFeedback.CameraOffset==Vector3.zero);
  health.Invincible=true;
  var enemy=UnityEngine.Object.FindFirstObjectByType<MeleeEnemyController>();enemy.transform.position=p.transform.position+Vector3.right*2;enemy.GetComponent<Rigidbody2D>().position=enemy.transform.position;var eh=enemy.GetComponent<HealthComponent>();eh.Invincible=false;eh.Configure(30,30);Set(eh,"invulnerableUntil",0f);
  enemy.TryReceiveDamage(new DamageInfo(1,p.transform.position,Vector2.right,p.gameObject,TeamAlignment.Player));Check("Enemy hit has material impact audio",CombatAudio.LastCue.StartsWith("melee_hurt_"));Check("Enemy hit does not flash player screen",feedback.HitCount==before+1&&feedback.FlashAlpha==0);Check("Local hit sparks created",UnityEngine.Object.FindObjectsByType<CombatImpact>(FindObjectsSortMode.None).Length>0);
  yield return new WaitForSeconds(.2f);BableGameUI.Instance.Pause();int count=CombatAudio.PlayedCount;CombatAudio.Play("sword",p.transform.position);Check("World cues blocked while paused",count==CombatAudio.PlayedCount);CombatAudio.UI("ui_select");Check("Menu cue audible during pause",CombatAudio.PlayedCount==count+1&&CombatAudio.LastCue=="ui_select");
  var audio=UnityEngine.Object.FindFirstObjectByType<BableAudio>();yield return null;Check("UI has independent live audio channel",audio.GetComponents<AudioSource>()[2].isPlaying);BableGameUI.Instance.Resume();
  session.SetMana(session.MaxMana,session.MaxMana);Set(combat,"shockwaveReadyTime",0f);p.SetTestInput(1,0);combat.PerformShockwave();yield return new WaitForSeconds(.5f);
  var loop=UnityEngine.Object.FindFirstObjectByType<CombatSoundLoop>();Check("Beam sustain loops only after release",loop!=null&&loop.GetComponent<AudioSource>().loop&&loop.GetComponent<AudioSource>().isPlaying);
  BableGameUI.Instance.Pause();yield return new WaitForSecondsRealtime(.12f);Check("Beam loop pauses with game",loop!=null&&!loop.GetComponent<AudioSource>().isPlaying);BableGameUI.Instance.Resume();yield return new WaitForSeconds(.1f);Check("Beam loop resumes",loop!=null&&loop.GetComponent<AudioSource>().isPlaying);
  combat.InterruptForHit();yield return null;Check("Interrupted beam removes loop immediately",UnityEngine.Object.FindFirstObjectByType<CombatSoundLoop>()==null);Check("Beam has discharge tail",CombatAudio.LastCue=="beam_end");
  foreach(var boss in UnityEngine.Object.FindObjectsByType<BossBrain>(FindObjectsSortMode.None)){
   boss.transform.position=p.transform.position+Vector3.right*4;boss.GetComponent<Rigidbody2D>().position=boss.transform.position;Set(boss,"target",p.transform);Call(boss,"Windup","Heavy",.8f);Check(boss.profile.kind+" windup emits unique identity cue",CombatAudio.LastCue==CombatAudio.Actor(boss.gameObject)+"_windup");
   yield return new WaitForSeconds(.1f);Call(boss,"Strike",150f,2f,1);Check(boss.profile.kind+" heavy release has distinct audio",CombatAudio.LastCue==CombatAudio.Actor(boss.gameObject)+"_heavy");
  }
  foreach(string cue in new[]{"stone_break","beam_hit","beam_contact","wall_jump","double_jump","drink_start","drink_finish","ui_purchase","ui_deny","ui_equip","trap_warn","trap_fire","projectile_wall"}){yield return new WaitForSeconds(.1f);CombatAudio.Play(cue,p.transform.position);Check(cue+" runtime dispatch",CombatAudio.LastCue==cue);}
  File.WriteAllText("../reference/revision48/complete.txt",failures.Count+" failures / "+checks.Count+" checks");
 }
}
