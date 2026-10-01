using System;using System.Collections;using System.Collections.Generic;using System.Linq;using System.IO;using System.Reflection;using UnityEngine;
using Bable;using Babel.Runtime.Core;using Babel.Runtime.Combat;using Babel.Runtime.Characters.Player;using Babel.Runtime.Characters.Enemies;
public static class BableRevision49Tests {
 static readonly List<string> checks=new(),failures=new();
 static void Check(string n,bool ok){checks.Add(n);if(!ok)failures.Add(n);File.WriteAllText("../reference/revision49/tests.json",JsonUtility.ToJson(new BableVerification.Report{checks=checks.ToArray(),failures=failures.ToArray()},true));}
 static void Set(object o,string n,object v){for(Type t=o.GetType();t!=null;t=t.BaseType){var f=t.GetField(n,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.DeclaredOnly);if(f!=null){f.SetValue(o,v);return;}}throw new Exception(n);}
 static object Call(object o,string n,params object[] args)=>o.GetType().GetMethod(n,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(o,args);
 static void Stand(GameObject actor,float x){var rb=actor.GetComponent<Rigidbody2D>();var c=actor.GetComponent<Collider2D>();c.enabled=true;rb.simulated=true;rb.gravityScale=0;rb.constraints=RigidbodyConstraints2D.FreezeRotation;Vector2 pos=new Vector2(x,actor.transform.position.y+(98.02f-c.bounds.min.y));rb.position=pos;actor.transform.position=pos;rb.linearVelocity=Vector2.zero;Physics2D.SyncTransforms();}
 public static void Run(){checks.Clear();failures.Clear();File.WriteAllText("../reference/revision49/complete.txt","RUNNING");new GameObject("Revision49 checks").AddComponent<BableTestHost>().StartCoroutine(Test());}
 static IEnumerator Test(){
  while(TowerLoading.Busy||BableGameUI.Instance==null||!BableGameUI.Instance.Ready)yield return null;
  TowerDialogue.AbortStory();BableGameUI.Instance.Resume();var session=GameSession.Instance;session.ResetForNewGame();session.UnlockWeapon();
  var p=UnityEngine.Object.FindFirstObjectByType<PlayerController2D>();p.GetComponent<HealthComponent>().Invincible=true;
  var enemies=UnityEngine.Object.FindObjectsByType<EnemyControllerBase>(FindObjectsSortMode.None);
  foreach(var e in enemies){e.enabled=false;e.CancelInvoke();e.GetComponent<Rigidbody2D>().constraints=RigidbodyConstraints2D.FreezeAll;e.GetComponent<CharacterPresentation>().enabled=false;var en=e.GetComponent<BossEncounter>();if(en!=null)en.enabled=false;}
  foreach(var s in UnityEngine.Object.FindObjectsByType<BossSpeech>(FindObjectsSortMode.None))s.enabled=false;
  var floor=new GameObject("Movement-audio test floor");floor.transform.position=new Vector2(850,97.5f);floor.AddComponent<BoxCollider2D>().size=new Vector2(60,1);
  Stand(p.gameObject,850);p.GetComponent<Rigidbody2D>().gravityScale=3;p.SetTestInput(0,0);Camera.main.GetComponent<Babel.Runtime.World.CameraFollow2D>().SettleAtTarget();yield return new WaitForSeconds(.25f);
  var clips=Resources.LoadAll<AudioClip>("Bable/Sfx49");Check("19 movement assets imported",clips.Length==19);foreach(var c in clips)Check(c.name+" resolves and loads",CombatAudio.Load(c.name)!=null&&c.length>.01f&&c.frequency==48000);
  var pa=p.GetComponent<ActorMovementAudio>();int count=pa.StepCount;p.SetTestInput(1,0);yield return new WaitForSeconds(1);Check("Real player run produces gait-linked footsteps",pa.StepCount>count&&CombatAudio.LastCue.StartsWith("step_player"));
  p.SetTestInput(0,0);yield return new WaitForSeconds(.3f);count=pa.StepCount;yield return new WaitForSeconds(.4f);Check("Stopped player is silent",count==pa.StepCount);
  p.SetTestInput(1,0,true);yield return new WaitForSeconds(.1f);count=pa.StepCount;yield return new WaitForSeconds(.2f);Check("Airborne player has no footsteps",!p.IsGrounded&&count==pa.StepCount);p.SetTestInput(0,0);yield return new WaitForSeconds(1);
  BableGameUI.Instance.Pause();count=CombatAudio.PlayedCount;CombatAudio.Movement("step_heavy_0",p.transform.position,.5f,14);Check("Paused movement produces no sound",CombatAudio.PlayedCount==count);BableGameUI.Instance.Resume();
  count=CombatAudio.PlayedCount;CombatAudio.Movement("step_heavy_0",p.transform.position+Vector3.right*100,.5f,14);Check("Distant footsteps inaudible",CombatAudio.PlayedCount==count);CombatAudio.Play("boss1_windup",p.transform.position);Check("Attack telegraph ducks movement sound",CombatAudio.MovementMix==.4f);yield return new WaitForSeconds(.4f);Check("Movement mix recovers",CombatAudio.MovementMix==1);
  foreach(var e in enemies.Where(e=>e is GiantEnemyController||e is MeleeEnemyController||e is BossBrain b&&(b.profile.kind==BossKind.Nero||b.profile.kind==BossKind.Shockwave)).GroupBy(e=>e.GetType().Name+(e is BossBrain b?b.profile.kind.ToString():"")).Select(g=>g.First())){
   Stand(e.gameObject,p.transform.position.x+3);var ma=e.GetComponent<ActorMovementAudio>();ma.Tick("idle",0);int before=ma.StepCount;
   foreach(float phase in new[]{.05f,.3f,.6f,.85f,.1f}){e.transform.position+=Vector3.right*.1f;e.GetComponent<Rigidbody2D>().position=e.transform.position;e.GetComponent<Rigidbody2D>().linearVelocity=Vector2.right;Physics2D.SyncTransforms();ma.Tick("run",phase);yield return new WaitForSeconds(.16f);}
   Check(e.name+" two planted footfalls in cycle",ma.StepCount==before+2);before=ma.StepCount;e.GetComponent<Rigidbody2D>().linearVelocity=Vector2.zero;ma.Tick("attack",.7f);Check(e.name+" weapon attack is not a footstep",ma.StepCount==before);
   e.transform.position+=Vector3.up*.5f;e.GetComponent<Rigidbody2D>().position=e.transform.position;e.GetComponent<Rigidbody2D>().linearVelocity=Vector2.down*4;Physics2D.SyncTransforms();ma.Tick("fall",.1f);yield return new WaitForSeconds(.16f);int lands=ma.LandingCount;Stand(e.gameObject,p.transform.position.x+3);ma.Tick("idle",.2f);Check(e.name+" true landing has weight cue",ma.LandingCount==lands+1);
   e.GetComponent<Rigidbody2D>().constraints=RigidbodyConstraints2D.FreezeAll;
  }
  foreach(var boss in enemies.OfType<BossBrain>().Where(b=>b.profile.kind==BossKind.Crystal||b.profile.kind==BossKind.Burrow)){
   boss.enabled=true;Set(boss,"<Engaged>k__BackingField",true);Set(boss,"<State>k__BackingField",boss.profile.kind==BossKind.Burrow?"Burrow":"Flight");boss.GetComponent<Rigidbody2D>().linearVelocity=Vector2.right;boss.GetComponent<ActorMovementAudio>().Tick("run",.6f);
   string cue=boss.profile.kind==BossKind.Burrow?"burrow_move":"crystal_flight";Check(cue+" starts only for active movement",boss.GetComponentsInChildren<CombatSoundLoop>().Any(l=>l.name.StartsWith(cue)));
   boss.DialogueHold=true;boss.GetComponent<ActorMovementAudio>().Tick("idle",0);yield return null;Check(cue+" stops for dialogue",!boss.GetComponentsInChildren<CombatSoundLoop>().Any());boss.enabled=false;boss.DialogueHold=false;
  }
  var nero=enemies.OfType<BossBrain>().First(b=>b.profile.kind==BossKind.Nero);Stand(nero.gameObject,p.transform.position.x-4);nero.enabled=true;Set(nero,"<Engaged>k__BackingField",true);Set(nero,"<State>k__BackingField","Recover");Set(nero,"deadline",Time.time+10);Set(nero,"target",p.transform);nero.arenaCenter=p.transform.position;nero.arenaSize=new Vector2(40,20);
  nero.StartCoroutine((IEnumerator)Call(nero,"GroundSlam",1));yield return new WaitForSeconds(.1f);Check("Nero slam does not sound before contact time",CombatAudio.LastCue!="boss5_slam");yield return new WaitForSeconds(.25f);Check("Nero slam sounds at impact",CombatAudio.LastCue=="boss5_slam");nero.enabled=false;
  var shock=enemies.OfType<BossBrain>().First(b=>b.profile.kind==BossKind.Shockwave);Set(shock,"attack","Quick");Call(shock,"Strike",270f,2.6f,1);Check("Abaddon quick strike has fast cue",CombatAudio.LastCue=="boss1_quick");
  var aerial=enemies.OfType<BossBrain>().First(b=>b.profile.kind==BossKind.Aerial);aerial.transform.position=p.transform.position+Vector3.right*4;Call(aerial,"StrikeDirected",110f,1.9f,1,55f);Check("Aerial upward strike distinct",CombatAudio.LastCue=="boss3_rising");Call(aerial,"StrikeDirected",110f,1.9f,1,-50f);Check("Aerial downward strike distinct",CombatAudio.LastCue=="boss3_downcut");
  Stand(shock.gameObject,p.transform.position.x+3);shock.enabled=true;Set(shock,"target",p.transform);shock.arenaCenter=p.transform.position;shock.arenaSize=new Vector2(40,20);Set(shock,"<Engaged>k__BackingField",true);Set(shock,"<State>k__BackingField","Recover");Set(shock,"deadline",Time.time+10);Set(shock,"attack","Heavy");
  Call(shock,"Strike",150f,2.6f,2);yield return new WaitForSeconds(.08f);Check("Abaddon heavy swing has no premature ground impact",CombatAudio.LastCue=="boss1_heavy");yield return new WaitForSeconds(.28f);Check("Abaddon blade touching ground produces impact",CombatAudio.LastCue=="boss1_slam");shock.enabled=false;
  var giant=enemies.OfType<GiantEnemyController>().First();Stand(giant.gameObject,p.transform.position.x-2);Set(giant,"target",p.transform);Call(giant,"AttackTarget");Check("Giant lift has warning cue",CombatAudio.LastCue=="giant_windup");yield return new WaitForSeconds(1.15f);Check("Giant release has heavy ground impact",CombatAudio.LastCue=="giant_slam");
  File.WriteAllText("../reference/revision49/complete.txt",failures.Count+" failures / "+checks.Count+" checks");
 }
}
