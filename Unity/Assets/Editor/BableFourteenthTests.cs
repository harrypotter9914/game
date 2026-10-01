using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.IO;
using UnityEngine;
using Bable;
using Babel.Runtime.Characters.Player;
using Babel.Runtime.Characters.Enemies;
using Babel.Runtime.Combat;
using Babel.Runtime.Core;
public static class BableFourteenthTests {
 static readonly List<string> checks=new(),failures=new(),trace=new();
 static void Check(string name,bool pass){checks.Add(name);if(!pass)failures.Add(name);File.WriteAllText("../reference/revision14/tests.json",JsonUtility.ToJson(new BableVerification.Report{checks=checks.ToArray(),failures=failures.ToArray()},true));}
 static object Call(object obj,string method,params object[] args)=>obj.GetType().GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(obj,args);
 static void Move(GameObject go,Vector2 center){var col=go.GetComponent<Collider2D>();Vector2 delta=center-(Vector2)col.bounds.center;go.transform.position+=(Vector3)delta;var body=go.GetComponent<Rigidbody2D>();if(body!=null){body.position=go.transform.position;body.linearVelocity=Vector2.zero;}Physics2D.SyncTransforms();}
 static void Hold(BossBrain boss)=>Call(boss,"Change","Recover",100f,"idle");
 public static void Run(){checks.Clear();failures.Clear();trace.Clear();new GameObject("Boss attack contract audit").AddComponent<BableTestHost>().StartCoroutine(Test());}
 static IEnumerator Test(){
  BableGameUI.Instance.Begin();yield return new WaitForSeconds(.4f);
  foreach(var s in Object.FindObjectsByType<BossSpeech>(FindObjectsSortMode.None))s.enabled=false;TowerDialogue.AbortStory();
  var p=Object.FindFirstObjectByType<PlayerController2D>();var hp=p.GetComponent<HealthComponent>();var rb=p.GetComponent<Rigidbody2D>();p.SetTestInput(0,0);p.enabled=false;hp.Invincible=false;hp.Configure(30,30);rb.gravityScale=0;rb.constraints=RigidbodyConstraints2D.FreezeAll;
  var all=Object.FindObjectsByType<EnemyControllerBase>(FindObjectsSortMode.None);foreach(var e in all){e.enabled=false;e.GetComponent<Rigidbody2D>().simulated=false;}
  var boss=all.OfType<BossBrain>().First(b=>b.profile.kind==BossKind.Shockwave);var passage=Object.FindFirstObjectByType<DefeatedBossPassage>();
  Check("Authored first-boss passage is absent before victory",passage!=null&&!passage.IsRevealed&&passage.walls.All(w=>!w.gameObject.activeSelf));
  var tm=GameObject.Find("GroundTilemap").GetComponent<UnityEngine.Tilemaps.Tilemap>();Check("Hidden victory passage also removes terrain collision tiles",!tm.HasTile(tm.WorldToCell(new Vector3(70,-58,0))));
  var guard=all.First(e=>!(e is BossBrain));var gb=guard.GetComponent<Rigidbody2D>();gb.simulated=true;gb.gravityScale=0;gb.constraints=RigidbodyConstraints2D.FreezeAll;
  var contact=p.gameObject.AddComponent<BableContactAudit>();Move(p.gameObject,new Vector2(850,100));Move(guard.gameObject,new Vector2(850.3f,100));int before=hp.CurrentHealth;yield return new WaitForSeconds(.8f);
  Check("Physical enemy body contact is actually exercised",contact.contacts>0);Check("Enemy body contact causes zero damage",hp.CurrentHealth==before);gb.simulated=false;
  boss.arenaCenter=new Vector2(860,100);boss.arenaSize=new Vector2(90,40);var bb=boss.GetComponent<Rigidbody2D>();bb.simulated=true;bb.gravityScale=0;bb.constraints=RigidbodyConstraints2D.FreezeAll;boss.enabled=true;Move(boss.gameObject,new Vector2(850,100));Move(p.gameObject,new Vector2(852.5f,100));yield return null;Hold(boss);boss.FaceDialogueTarget(p.transform);
  before=hp.CurrentHealth;Call(boss,"Windup","Heavy",1.5f);yield return new WaitForSeconds(1.2f);Check("Heavy anticipation has no damage",hp.CurrentHealth==before);yield return new WaitForSeconds(.7f);Check("Heavy swing deals authored two health",hp.CurrentHealth==before-2);Hold(boss);before=hp.CurrentHealth;yield return new WaitForSeconds(.5f);Check("Recovery has no lingering attack damage",hp.CurrentHealth==before);
  before=hp.CurrentHealth;Call(boss,"Strike",150f,4f,2);BableGameUI.Instance.Pause();yield return new WaitForSecondsRealtime(.4f);Check("Pause freezes a live attack instead of deleting it",hp.CurrentHealth==before&&Object.FindObjectsByType<BossStrikeEffect>(FindObjectsSortMode.None).Length>0);BableGameUI.Instance.Resume();yield return new WaitForSeconds(.5f);Check("Resuming preserves the pending attack window",hp.CurrentHealth==before-2);Hold(boss);
  var wall=new GameObject("Combat audit solid wall");wall.transform.position=new Vector3(851.2f,100,0);wall.AddComponent<BoxCollider2D>().size=new Vector2(.35f,10);Physics2D.SyncTransforms();before=hp.CurrentHealth;Call(boss,"Strike",150f,4f,2);yield return new WaitForSeconds(.5f);Check("Melee cannot damage through ordinary walls",hp.CurrentHealth==before);
  wall.AddComponent<BreakableWall>();before=hp.CurrentHealth;Call(boss,"Wave",Vector2.right);yield return new WaitForSeconds(.8f);Check("Boss wave cannot pass through shockwave-only breakable walls",hp.CurrentHealth==before&&Object.FindObjectsByType<EnemyProjectile>(FindObjectsSortMode.None).Length==0);
  Object.Destroy(wall);yield return null;Physics2D.SyncTransforms();hp.Configure(30,30);yield return new WaitForSeconds(.3f);Hold(boss);boss.FaceDialogueTarget(p.transform);Call(boss,"Windup","Quick",.5f);int maxShots=0;float end=Time.time+1.7f;while(Time.time<end){yield return null;maxShots=Mathf.Max(maxShots,Object.FindObjectsByType<EnemyProjectile>(FindObjectsSortMode.None).Length);}
  Check("First boss quick double slash never creates magic projectiles",maxShots==0);Check("Quick slash hits during its visible attack",hp.CurrentHealth<30);Hold(boss);
  hp.Configure(30,30);Move(p.gameObject,new Vector2(860,100));boss.FaceDialogueTarget(p.transform);Call(boss,"Wave",Vector2.right);yield return new WaitForSeconds(.2f);var shot=Object.FindFirstObjectByType<EnemyProjectile>();Vector2 shotStart=shot!=null?(Vector2)shot.transform.position:Vector2.zero;
  Check("Wave forms at body height above the floor",shot!=null&&Mathf.Abs(shot.transform.position.y-boss.AttackCenter.y)<.05f);yield return new WaitForSeconds(.15f);Check("Wave visibly advances instead of dying at spawn",shot!=null&&shot.transform.position.x>shotStart.x+.7f);yield return new WaitForSeconds(.8f);trace.Add("wave end: alive="+(shot!=null)+" hp="+hp.CurrentHealth+" start="+shotStart);Check("First boss wave ends within authored 2.5 tiles",shot==null&&hp.CurrentHealth==30);
  Move(p.gameObject,new Vector2(852.5f,100));Hold(boss);boss.FaceDialogueTarget(p.transform);before=hp.CurrentHealth;Call(boss,"Strike",150f,4f,2);boss.ResetEncounter();yield return new WaitForSeconds(.5f);Check("Reset cancels pending melee damage",hp.CurrentHealth==before);
  boss.enabled=false;bb.simulated=false;
  foreach(var dir in new[]{Vector2.left,Vector2.right,Vector2.up,Vector2.down}){
   Vector2 origin=new Vector2(950,100);Move(p.gameObject,origin+dir*4);var obstacle=new GameObject("Breakable projectile audit");obstacle.transform.position=origin+dir*2;obstacle.AddComponent<BoxCollider2D>().size=Mathf.Abs(dir.x)>.5f?new Vector2(.3f,5):new Vector2(5,.3f);obstacle.AddComponent<BreakableWall>();Physics2D.SyncTransforms();before=hp.CurrentHealth;BableCombatFX.Projectile(boss.gameObject,origin,dir,12,1,Color.white,.6f,"ArcaneOrb");yield return new WaitForSeconds(.7f);Check("Breakable terrain blocks enemy projectile "+dir,hp.CurrentHealth==before);Object.Destroy(obstacle);yield return null;
  }
  // A victory gate must not materialize inside the player.
  Move(p.gameObject,new Vector2(70,-58));boss.GetComponent<HealthComponent>().Invincible=false;boss.GetComponent<HealthComponent>().ApplyDamage(999);yield return new WaitForSeconds(1);
  Check("Victory gate waits while player occupies it",!passage.IsRevealed);Move(p.gameObject,new Vector2(75,-58));yield return new WaitForSeconds(.3f);Check("Victory reveals the three authored breakable blocks",passage.IsRevealed&&passage.walls.All(w=>w!=null&&w.gameObject.activeSelf));
  var testWall=passage.walls[0];testWall.HitByShockwave();yield return null;Check("Post-victory block remains destructible by Shockwave",testWall==null);
  File.WriteAllLines("../reference/revision14/trace.txt",trace);File.WriteAllText("../reference/revision14/finished.txt",failures.Count+" failures / "+checks.Count+" checks");
 }
}
public sealed class BableContactAudit:MonoBehaviour {public int contacts;void OnCollisionStay2D(Collision2D c){if(c.collider.GetComponent<EnemyControllerBase>()!=null)contacts++;}}


