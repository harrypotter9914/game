using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using UnityEngine;
using Bable;
using Babel.Runtime.Core;
using Babel.Runtime.Combat;
using Babel.Runtime.Characters.Player;
using Babel.Runtime.Characters.Enemies;
public static class BableRevision47Tests {
 static readonly List<string> checks=new(),failures=new();
 static void Check(string name,bool ok){checks.Add(name);if(!ok)failures.Add(name);File.WriteAllText("../reference/revision47/tests.json",JsonUtility.ToJson(new BableVerification.Report{checks=checks.ToArray(),failures=failures.ToArray()},true));}
 static void Set(object o,string name,object value){for(var t=o.GetType();t!=null;t=t.BaseType){var f=t.GetField(name,BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.DeclaredOnly);if(f!=null){f.SetValue(o,value);return;}}throw new System.Exception(name);}
 static PlayerController2D p;static PlayerCombatController combat;static GameSession session;
 static void Position(Vector2 center, bool freeze=true){p.GetComponent<HitRecoil>().Cancel();var rb=p.GetComponent<Rigidbody2D>();rb.simulated=true;rb.constraints=freeze?RigidbodyConstraints2D.FreezeAll:RigidbodyConstraints2D.FreezeRotation;rb.gravityScale=0;rb.position=center;p.transform.position=center;rb.linearVelocity=Vector2.zero;p.SetTestInput(0,0);Physics2D.SyncTransforms();}
 static void EnemyPosition(EnemyControllerBase e,Vector2 at,bool freeY=false){e.GetComponent<HitRecoil>()?.Cancel();e.enabled=false;var rb=e.GetComponent<Rigidbody2D>();rb.simulated=true;rb.constraints=freeY?RigidbodyConstraints2D.FreezeRotation:RigidbodyConstraints2D.FreezePositionY|RigidbodyConstraints2D.FreezeRotation;rb.gravityScale=0;rb.position=at;e.transform.position=at;rb.linearVelocity=Vector2.zero;e.GetComponent<Collider2D>().enabled=true;var b=e as BossBrain;if(b!=null){Set(b,"<Engaged>k__BackingField",true);Set(b,"<State>k__BackingField","Recover");typeof(BossBrain).GetMethod("SyncBurrowBody",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(b,null);}var h=e.GetComponent<HealthComponent>();h.Invincible=false;h.Configure(100,100);Set(h,"invulnerableUntil",0f);Physics2D.SyncTransforms();}
 static string Label(EnemyControllerBase e)=>e is BossBrain b?"Boss "+b.profile.kind:e.GetType().Name;
 static void Ready(){Set(combat,"meleeReadyTime",0f);Set(combat,"shockwaveReadyTime",0f);Set(combat,"dashReadyTime",0f);}
 static void Hit(EnemyControllerBase e,Vector2 axis)=>e.TryReceiveDamage(new DamageInfo(1,p.transform.position,axis,p.gameObject,TeamAlignment.Player));
 public static void Run(){checks.Clear();failures.Clear();File.WriteAllText("../reference/revision47/complete.txt","RUNNING");new GameObject("Revision47 checks").AddComponent<BableTestHost>().StartCoroutine(Test());}
 static IEnumerator Test(){
  while(TowerLoading.Busy||BableGameUI.Instance==null||!BableGameUI.Instance.Ready)yield return null;
  TowerDialogue.AbortStory();BableGameUI.Instance.Resume();session=GameSession.Instance;session.ResetForNewGame();session.UnlockWeapon();session.UnlockAbility(AbilityId.Shockwave);session.UnlockAbility(AbilityId.CrystalDash);
  p=Object.FindFirstObjectByType<PlayerController2D>();combat=p.GetComponent<PlayerCombatController>();var ph=p.GetComponent<HealthComponent>();ph.Invincible=true;
  foreach(var s in Object.FindObjectsByType<BossSpeech>(FindObjectsSortMode.None))s.enabled=false;
  var all=Object.FindObjectsByType<EnemyControllerBase>(FindObjectsSortMode.None);foreach(var e in all){e.enabled=false;e.CancelInvoke();e.GetComponent<Rigidbody2D>().constraints=RigidbodyConstraints2D.FreezeAll;var be=e.GetComponent<BossEncounter>();if(be!=null)be.enabled=false;}
  var reps=all.GroupBy(Label).Select(g=>g.First()).ToArray();var small=reps.Where(e=>!(e is BossBrain)&&!(e is GiantEnemyController)).ToArray();var heavy=reps.Except(small).ToArray();
  Check("Only three small minion types have recoil",small.Length==3&&small.All(e=>e.GetComponent<HitRecoil>()!=null));Check("Five Bosses and giant have no recoil motor",heavy.Length==6&&heavy.All(e=>e.GetComponent<HitRecoil>()==null));
  var floor=new GameObject("Recoil verification floor");floor.transform.position=new Vector2(850,97.5f);floor.AddComponent<BoxCollider2D>().size=new Vector2(50,1);
  foreach(var e in small){
   foreach(int sign in new[]{1,-1}){
    EnemyPosition(e,new Vector2(850,100));Set(e,"<FacingSign>k__BackingField",sign);Position(new Vector2(850-sign,100));p.SetTestInput(sign,0);Ready();if(e is ShieldSentinelController)Set(e,"exposedUntil",Time.time+5);e.enabled=true;combat.PerformMeleeAttack();float hitDeadline=Time.time+1;while(e.GetComponent<HealthComponent>().CurrentHealth==100&&Time.time<hitDeadline)yield return null;yield return new WaitForSeconds(.12f);
    Check(Label(e)+" actual sword hit "+sign+" deals damage",e.GetComponent<HealthComponent>().CurrentHealth<100);
    Check(Label(e)+" recoils away "+sign+" despite active AI",sign*(e.transform.position.x-850)>.45f&&e.GetComponent<HitRecoil>().IsRecoiling);
    yield return new WaitForSeconds(.35f);e.enabled=false;Check(Label(e)+" recoil ends "+sign,!e.GetComponent<HitRecoil>().IsRecoiling);
   }
   EnemyPosition(e,new Vector2(850,100),true);Position(new Vector2(850,99));p.SetTestInput(0,1);Ready();combat.PerformMeleeAttack();yield return new WaitForSeconds(.24f);
   Check(Label(e)+" upward sword launches airborne small enemy",e.GetComponent<HealthComponent>().CurrentHealth<100&&e.GetComponent<Rigidbody2D>().linearVelocity.y>4&&e.transform.position.y>100.4f);yield return new WaitForSeconds(.25f);
   EnemyPosition(e,new Vector2(850,100));Position(new Vector2(850,102));Hit(e,Vector2.down);yield return new WaitForSeconds(.15f);
   Check(Label(e)+" downslash causes damage without enemy recoil",e.GetComponent<HealthComponent>().CurrentHealth==99&&!e.GetComponent<HitRecoil>().IsRecoiling&&(e.transform.position-new Vector3(850,100)).sqrMagnitude<.001f);yield return new WaitForSeconds(.25f);
   EnemyPosition(e,new Vector2(1000+System.Array.IndexOf(small,e)*8,100));
  }
  foreach(var e in heavy){EnemyPosition(e,new Vector2(850,100),true);Position(new Vector2(849,100));Set(e,"<FacingSign>k__BackingField",1);Hit(e,Vector2.right);yield return new WaitForSeconds(.15f);Check(Label(e)+" horizontal damage without displacement",e.GetComponent<HealthComponent>().CurrentHealth==99&&(e.transform.position-new Vector3(850,100)).sqrMagnitude<.001f);yield return new WaitForSeconds(.25f);Hit(e,Vector2.up);yield return new WaitForSeconds(.15f);Check(Label(e)+" upward damage cannot launch heavy target",e.GetComponent<HealthComponent>().CurrentHealth==98&&e.GetComponent<Rigidbody2D>().linearVelocity.sqrMagnitude<.001f);EnemyPosition(e,new Vector2(1100+System.Array.IndexOf(heavy,e)*10,100));}
  var shield=small.First(e=>e is ShieldSentinelController);EnemyPosition(shield,new Vector2(850,100));Set(shield,"<FacingSign>k__BackingField",-1);Position(new Vector2(849,100));Set(shield,"exposedUntil",0f);p.SetTestInput(1,0);Ready();combat.PerformMeleeAttack();yield return new WaitForSeconds(.3f);
  Check("Shield block has neither damage nor recoil",shield.GetComponent<HealthComponent>().CurrentHealth==100&&!shield.GetComponent<HitRecoil>().IsRecoiling&&Mathf.Abs(shield.transform.position.x-850)<.001f);EnemyPosition(shield,new Vector2(1000,100));
  var minion=small.First(e=>e is MeleeEnemyController);EnemyPosition(minion,new Vector2(850,100));Position(new Vector2(849,100));minion.GetComponent<HealthComponent>().Invincible=true;Hit(minion,Vector2.right);yield return new WaitForSeconds(.1f);Check("Invulnerable enemy has neither damage nor recoil",minion.GetComponent<HealthComponent>().CurrentHealth==100&&!minion.GetComponent<HitRecoil>().IsRecoiling);minion.GetComponent<HealthComponent>().Invincible=false;
  Hit(minion,Vector2.right);float velocity=minion.GetComponent<Rigidbody2D>().linearVelocity.x;int life=minion.GetComponent<HealthComponent>().CurrentHealth;Hit(minion,Vector2.left);Check("Rejected invulnerability-window hit cannot reverse recoil",minion.GetComponent<HealthComponent>().CurrentHealth==life&&minion.GetComponent<Rigidbody2D>().linearVelocity.x==velocity);yield return new WaitForSeconds(.4f);
  EnemyPosition(minion,new Vector2(850,98+minion.GetComponent<Collider2D>().bounds.extents.y+.02f),true);minion.GetComponent<Rigidbody2D>().gravityScale=3;yield return new WaitForSeconds(.2f);Position(new Vector2(850,97));float groundY=minion.transform.position.y;Hit(minion,Vector2.up);yield return new WaitForSeconds(.2f);Check("Upward hit does not launch grounded small enemy",minion.GetComponent<HealthComponent>().CurrentHealth==99&&!minion.GetComponent<HitRecoil>().IsRecoiling&&Mathf.Abs(minion.transform.position.y-groundY)<.03f);EnemyPosition(minion,new Vector2(1000,100));
  // Actual damage from every large actor still knocks the player away.
  ph.Invincible=false;foreach(var e in heavy){Position(new Vector2(850,100),false);EnemyPosition(e,new Vector2(848,100));ph.Configure(50,50);Set(ph,"invulnerableUntil",0f);p.SetTestInput(-1,0);ph.ReceiveDamage(new DamageInfo(1,e.transform.position,Vector2.right*3,e.gameObject,TeamAlignment.Enemy));yield return new WaitForSeconds(.12f);Check(Label(e)+" can damage and knock player against held input",ph.CurrentHealth==49&&p.transform.position.x>850.65f&&p.IsRecoiling);yield return new WaitForSeconds(.3f);EnemyPosition(e,new Vector2(1100+System.Array.IndexOf(heavy,e)*10,100));}
  Position(new Vector2(850,100),false);EnemyPosition(minion,new Vector2(852,100));ph.Configure(50,50);Set(ph,"invulnerableUntil",0f);ph.ReceiveDamage(new DamageInfo(1,minion.transform.position,Vector2.left*2,minion.gameObject,TeamAlignment.Enemy));yield return new WaitForSeconds(.12f);Check("Player struck from right recoils left",p.transform.position.x<849.35f&&p.IsRecoiling);Check("Recoil blocks immediate melee dash and new beam",!combat.PerformMeleeAttack()&&!combat.PerformCrystalDash()&&!combat.PerformShockwave());
  var saved=p.transform.position;BableGameUI.Instance.Pause();yield return new WaitForSecondsRealtime(.4f);Check("Pause freezes recoil position and timer",p.IsRecoiling&&(p.transform.position-saved).sqrMagnitude<.001f);BableGameUI.Instance.Resume();yield return new WaitForSeconds(.35f);Check("Recoil recovers after resume",!p.IsRecoiling);
  Position(new Vector2(850,100),false);EnemyPosition(minion,new Vector2(848,100));ph.Configure(50,50);Set(ph,"invulnerableUntil",0f);ph.Invincible=true;ph.ReceiveDamage(new DamageInfo(1,minion.transform.position,Vector2.right,minion.gameObject,TeamAlignment.Enemy));Check("Invulnerable player never recoils",ph.CurrentHealth==50&&!p.IsRecoiling);ph.Invincible=false;
  // A wall stops both recoil motion and its collision volume.
  var wall=new GameObject("Recoil solid wall");wall.transform.position=new Vector2(851,100);wall.AddComponent<BoxCollider2D>().size=new Vector2(.4f,6);Physics2D.SyncTransforms();ph.ReceiveDamage(new DamageInfo(1,minion.transform.position,Vector2.right,minion.gameObject,TeamAlignment.Enemy));yield return new WaitForSeconds(.35f);Check("Player recoil cannot penetrate solid wall",p.GetComponent<Collider2D>().bounds.max.x<=850.82f);Object.Destroy(wall);yield return null;
  Position(new Vector2(850,100),false);Ready();session.SetMana(session.MaxMana,session.MaxMana);combat.PerformShockwave();yield return new WaitForSeconds(.4f);Set(ph,"invulnerableUntil",0f);ph.ReceiveDamage(new DamageInfo(1,minion.transform.position,Vector2.right,minion.gameObject,TeamAlignment.Enemy));yield return null;Check("Actual player damage interrupts sustained beam",!combat.IsShockwaveActive&&Object.FindFirstObjectByType<DirectionalShockwaveVisual>()==null);yield return new WaitForSeconds(.35f);
  EnemyPosition(minion,new Vector2(850,100));Position(new Vector2(850,minion.GetComponent<Collider2D>().bounds.max.y+1),false);p.GetComponent<Rigidbody2D>().gravityScale=3;yield return new WaitForFixedUpdate();p.SetTestInput(0,-1);Ready();combat.PerformMeleeAttack();yield return new WaitForSeconds(.24f);Check("Real downslash still bounces player only",p.Velocity.y>3&&minion.GetComponent<HealthComponent>().CurrentHealth<100&&!minion.GetComponent<HitRecoil>().IsRecoiling);yield return new WaitForSeconds(.25f);
  Check("Body passthrough remains enabled",Physics2D.GetIgnoreLayerCollision(8,9));
  File.WriteAllText("../reference/revision47/complete.txt",failures.Count+" failures / "+checks.Count+" checks");
 }
}
