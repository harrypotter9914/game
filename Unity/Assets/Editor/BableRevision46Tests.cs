using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using Bable;
using Babel.Runtime.Core;
using Babel.Runtime.Combat;
using Babel.Runtime.Characters.Player;
using Babel.Runtime.Characters.Enemies;
public static class BableRevision46Tests {
 static readonly List<string> checks=new(),failures=new();
 static void Check(string name,bool ok){checks.Add(name);if(!ok)failures.Add(name);File.WriteAllText("../reference/revision46/tests.json",JsonUtility.ToJson(new BableVerification.Report{checks=checks.ToArray(),failures=failures.ToArray()},true));}
 static void Set(object o,string name,object value){for(var t=o.GetType();t!=null;t=t.BaseType){var f=t.GetField(name,BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.DeclaredOnly);if(f!=null){f.SetValue(o,value);return;}}throw new System.Exception(name);}
 static PlayerController2D p;static PlayerCombatController combat;static GameSession session;
 static void Position(Vector2 center,bool freezeY=false){var rb=p.GetComponent<Rigidbody2D>();rb.simulated=true;rb.constraints=freezeY?RigidbodyConstraints2D.FreezePositionY|RigidbodyConstraints2D.FreezeRotation:RigidbodyConstraints2D.FreezeRotation;rb.gravityScale=3;rb.position=center;p.transform.position=center;rb.linearVelocity=Vector2.zero;p.SetTestInput(0,0);Physics2D.SyncTransforms();}
 static void EnemyPosition(EnemyControllerBase e,Vector2 at,bool freeX=false){var rb=e.GetComponent<Rigidbody2D>();rb.simulated=true;rb.constraints=freeX?RigidbodyConstraints2D.FreezePositionY|RigidbodyConstraints2D.FreezeRotation:RigidbodyConstraints2D.FreezeAll;rb.position=at;e.transform.position=at;rb.linearVelocity=Vector2.zero;e.GetComponent<Collider2D>().enabled=true;var b=e as BossBrain;if(b!=null){Set(b,"<Engaged>k__BackingField",true);Set(b,"<State>k__BackingField","Recover");typeof(BossBrain).GetMethod("SyncBurrowBody",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(b,null);}e.GetComponent<HealthComponent>().Invincible=false;Physics2D.SyncTransforms();}
 static string Label(EnemyControllerBase e)=>e is BossBrain b?"Boss "+b.profile.kind:e.GetType().Name;
 public static void Run(){checks.Clear();failures.Clear();File.WriteAllText("../reference/revision46/complete.txt","RUNNING");new GameObject("Revision46 checks").AddComponent<BableTestHost>().StartCoroutine(Test());}
 static IEnumerator Test(){
  while(TowerLoading.Busy||BableGameUI.Instance==null||!BableGameUI.Instance.Ready)yield return null;
  TowerDialogue.AbortStory();BableGameUI.Instance.Resume();session=GameSession.Instance;session.ResetForNewGame();session.UnlockWeapon();
  p=Object.FindFirstObjectByType<PlayerController2D>();combat=p.GetComponent<PlayerCombatController>();var playerHealth=p.GetComponent<HealthComponent>();playerHealth.Invincible=false;
  foreach(var s in Object.FindObjectsByType<BossSpeech>(FindObjectsSortMode.None))s.enabled=false;
  var all=Object.FindObjectsByType<EnemyControllerBase>(FindObjectsSortMode.None);foreach(var e in all){e.enabled=false;e.CancelInvoke();e.GetComponent<Rigidbody2D>().constraints=RigidbodyConstraints2D.FreezeAll;var be=e.GetComponent<BossEncounter>();if(be!=null)be.enabled=false;}
  var reps=all.GroupBy(Label).Select(g=>g.First()).ToArray();
  Check("Four minion types and five Bosses present",reps.Length==9);
  Check("Every authored enemy assigned EnemyBody",all.All(e=>e.gameObject.layer==ActorBodyCollision.EnemyBodyLayer));
  Check("Player and enemy layers ignore physical body contact",p.gameObject.layer==8&&Physics2D.GetIgnoreLayerCollision(8,9));
  Check("Terrain and projectile layers still collide with player",!Physics2D.GetIgnoreLayerCollision(8,0));
  var floor=new GameObject("Verification floor");floor.transform.position=new Vector2(850,97.5f);floor.AddComponent<BoxCollider2D>().size=new Vector2(35,1);
  foreach(var e in reps){
   string name=Label(e);EnemyPosition(e,new Vector2(850,100),true);var enemyBody=e.GetComponent<Rigidbody2D>();var enemyHealth=e.GetComponent<HealthComponent>();enemyHealth.Configure(100,100);
   Position(new Vector2(846,100),true);p.SetTestInput(1,0);int hp=playerHealth.CurrentHealth;yield return new WaitForSeconds(1.45f);p.SetTestInput(0,0);
   Check(name+" player runs through body",p.transform.position.x>853);
   Check(name+" no pushing or contact damage",Mathf.Abs(enemyBody.position.x-850)<.02f&&playerHealth.CurrentHealth==hp);
   EnemyPosition(e,new Vector2(850,100));Position(new Vector2(850,e.GetComponent<Collider2D>().bounds.max.y+3));yield return new WaitForSeconds(1.1f);
   Check(name+" falls through head onto actual floor",p.IsGrounded&&Mathf.Abs(p.GetComponent<Collider2D>().bounds.min.y-98)<.1f);
   // A real downward attack, from just above the head, must damage and then bounce.
   EnemyPosition(e,new Vector2(850,100));enemyHealth.Configure(100,100);Position(new Vector2(850,e.GetComponent<Collider2D>().bounds.max.y+1));yield return new WaitForFixedUpdate();p.SetTestInput(0,-1);Set(combat,"meleeReadyTime",0f);Check(name+" downward strike starts",combat.PerformMeleeAttack());yield return new WaitForSeconds(.24f);
   Check(name+" downward hit damages and bounces",enemyHealth.CurrentHealth<100&&p.Velocity.y>3);
   yield return new WaitForSeconds(.2f);EnemyPosition(e,new Vector2(930+System.Array.IndexOf(reps,e)*8,100));
  }
  var minion=reps.First(e=>e is MeleeEnemyController);EnemyPosition(minion,new Vector2(850,100));minion.GetComponent<HealthComponent>().Invincible=true;
  Position(new Vector2(850,minion.GetComponent<Collider2D>().bounds.max.y+1));yield return new WaitForFixedUpdate();p.SetTestInput(0,-1);Set(combat,"meleeReadyTime",0f);combat.PerformMeleeAttack();yield return new WaitForSeconds(.24f);
  Check("Immune enemy does not trigger false pogo",p.Velocity.y<0&&minion.GetComponent<HealthComponent>().CurrentHealth>0);yield return new WaitForSeconds(.2f);
  EnemyPosition(minion,new Vector2(950,100));Position(new Vector2(850,104));yield return new WaitForFixedUpdate();p.SetTestInput(0,-1);Set(combat,"meleeReadyTime",0f);combat.PerformMeleeAttack();yield return new WaitForSeconds(.24f);Check("Missed downslash cannot bounce on air",p.Velocity.y<0);
  var korah=(BossBrain)reps.First(e=>e is BossBrain b&&b.profile.kind==BossKind.Burrow);EnemyPosition(korah,new Vector2(850,100));Set(korah,"<State>k__BackingField","Burrow");typeof(BossBrain).GetMethod("SyncBurrowBody",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(korah,null);yield return null;EnemyPosition(korah,new Vector2(850,100),true);Position(new Vector2(846,100),true);p.SetTestInput(1,0);yield return new WaitForSeconds(1.45f);p.SetTestInput(0,0);
  Check("Korah collider reactivation keeps passthrough",p.transform.position.x>853&&Mathf.Abs(korah.transform.position.x-850)<.02f);EnemyPosition(korah,new Vector2(980,100));
  // Runtime clones use the same Awake assignment without scene or prefab rebaking.
  var spawned=Object.Instantiate(minion.gameObject,new Vector3(850,100),Quaternion.identity).GetComponent<EnemyControllerBase>();spawned.enabled=false;EnemyPosition(spawned,new Vector2(850,100),true);Position(new Vector2(846,100),true);p.SetTestInput(1,0);yield return new WaitForSeconds(1.45f);p.SetTestInput(0,0);
  Check("Newly spawned enemies inherit passthrough",spawned.gameObject.layer==9&&p.transform.position.x>853&&Mathf.Abs(spawned.transform.position.x-850)<.02f);Object.Destroy(spawned.gameObject);yield return null;
  Position(new Vector2(850,100),true);EnemyPosition(minion,new Vector2(851,100));int before=playerHealth.CurrentHealth;
  typeof(EnemyControllerBase).GetMethod("TryDamageCircle",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(minion,new object[]{new Vector2(850,100),1.2f,1,Vector2.left});
  Check("Enemy attack query still hurts player",playerHealth.CurrentHealth==before-1);EnemyPosition(minion,new Vector2(950,100));yield return new WaitForSeconds(.3f);
  var arrow=new GameObject("Verification magic arrow");arrow.transform.position=new Vector2(847,100);arrow.AddComponent<CircleCollider2D>().isTrigger=true;var shot=arrow.AddComponent<EnemyProjectile>();before=playerHealth.CurrentHealth;shot.Launch(Vector2.right,1,9,minion.gameObject,TeamAlignment.Enemy);yield return new WaitForSeconds(.65f);Check("Enemy projectiles still hit player",playerHealth.CurrentHealth==before-1);
  // Regular terrain remains solid, including while player bodies are excluded.
  Position(new Vector2(850,101));yield return new WaitForSeconds(.8f);Check("Player still lands on ordinary terrain",p.IsGrounded&&Mathf.Abs(p.GetComponent<Collider2D>().bounds.min.y-98)<.1f);
  File.WriteAllText("../reference/revision46/complete.txt",failures.Count+" failures / "+checks.Count+" checks");
 }
}
