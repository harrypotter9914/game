using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.IO;
using UnityEngine;
using Bable;
using Babel.Runtime.Characters.Enemies;
using Babel.Runtime.Characters.Player;
using Babel.Runtime.Combat;
using Babel.Runtime.Core;
public static class BableRevision41Tests {
 static readonly List<string> checks=new(),failures=new();
 static void Check(string n,bool ok){checks.Add(n);if(!ok)failures.Add(n);File.WriteAllText("../reference/revision41/tests.json",JsonUtility.ToJson(new BableVerification.Report{checks=checks.ToArray(),failures=failures.ToArray()},true));}
 static object Call(object o,string method,params object[] a){var t=o.GetType();MethodInfo m=null;while(t!=null&&m==null){m=t.GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.DeclaredOnly);t=t.BaseType;}return m.Invoke(o,a);}
 static void Set(object o,string f,object v)=>o.GetType().GetField(f,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(o,v);
 static void Position(GameObject o,Vector2 p){o.transform.position=p;var b=o.GetComponent<Rigidbody2D>();b.simulated=true;b.gravityScale=0;b.constraints=RigidbodyConstraints2D.FreezeAll;b.position=p;b.linearVelocity=Vector2.zero;Physics2D.SyncTransforms();}
 static void Hold(BossBrain b)=>Call(b,"Change","Recover",100f,"idle");
 public static void Run(){checks.Clear();failures.Clear();File.WriteAllText("../reference/revision41/complete.txt","RUNNING");new GameObject("Revision41 tests").AddComponent<BableTestHost>().StartCoroutine(Test());}
 static IEnumerator Test(){
  BableGameUI.Instance.Begin();while(TowerLoading.Busy)yield return null;
  foreach(var s in Object.FindObjectsByType<BossSpeech>(FindObjectsSortMode.None))s.enabled=false;TowerDialogue.AbortStory();
  var player=Object.FindFirstObjectByType<PlayerController2D>();player.enabled=false;var hp=player.GetComponent<HealthComponent>();hp.Configure(200,200);hp.Invincible=false;
  var combat=player.GetComponent<PlayerCombatController>();GameSession.Instance.UnlockAbility(AbilityId.Shockwave);GameSession.Instance.UnlockWeapon();
  var all=Object.FindObjectsByType<EnemyControllerBase>(FindObjectsSortMode.None);foreach(var e in all){e.enabled=false;e.GetComponent<Rigidbody2D>().simulated=false;}
  var bosses=all.OfType<BossBrain>().OrderBy(b=>(int)b.profile.kind).ToArray();
  Check("Five existing boss identities retained",bosses.Length==5);
  Check("First arena ends at its east exit",bosses[0].arenaCenter.x+bosses[0].arenaSize.x/2==69);
  Check("Korah arena excludes eastern narrow passage",bosses[1].arenaCenter.x+bosses[1].arenaSize.x/2==275);
  Position(player.gameObject,new Vector2(57,-39));Check("No dialogue in entrance shaft",!bosses[0].CanStartEncounter(player));
  var gates=GameObject.Find("Revision 41 shockwave passages").GetComponentsInChildren<BreakableWall>();
  Check("Five annotated passage groups exist",GameObject.Find("Revision 41 shockwave passages").transform.childCount==5);
  foreach(var group in gates.GroupBy(w=>w.transform.parent.name)){
   Check(group.Key+" is active and Shockwave-only",group.All(w=>w.gameObject.activeInHierarchy&&w.requiresShockwave&&!w.CanReceiveDamage(TeamAlignment.Player)));
   var w=group.First();w.ReceiveDamage(new DamageInfo(99,w.transform.position,Vector2.zero,player.gameObject,TeamAlignment.Player));yield return null;Check(group.Key+" resists a sword",w!=null);
   w.HitByShockwave();yield return null;Check(group.Key+" shatters by Shockwave",w==null);
  }
  var gateRoot=GameObject.Find("Revision 41 shockwave passages");
  Vector2[] castPoints={new Vector2(67,-58.5f),new Vector2(81.5f,-59.5f),new Vector2(94,-59.5f),new Vector2(285.5f,-60),new Vector2(291.5f,-60)};
  Vector2[] castAxes={Vector2.right,Vector2.down,Vector2.down,Vector2.down,Vector2.right};
  for(int g=0;g<gateRoot.transform.childCount;g++){
   var group=gateRoot.transform.GetChild(g);int before=group.GetComponentsInChildren<BreakableWall>().Length;
   Position(player.gameObject,castPoints[g]);combat.StartCoroutine((IEnumerator)Call(combat,"ShockwaveSequence",castAxes[g]));yield return new WaitForSeconds(.9f);
   Check(group.name+" breaks from actual directional Shockwave",group.GetComponentsInChildren<BreakableWall>().Length<before);
  }
  for(int i=0;i<5;i++){
   var b=bosses[i];b.arenaCenter=new Vector2(860,100);b.arenaSize=new Vector2(80,35);Position(b.gameObject,new Vector2(850,100));Position(player.gameObject,new Vector2(850.4f,100));
   Set(b,"<Engaged>k__BackingField",true);Hold(b);b.enabled=true;yield return null;Hold(b);
   int before=hp.CurrentHealth;yield return new WaitForSeconds(.5f);Check(b.profile.kind+" resting body contact never damages",hp.CurrentHealth==before);
   var bh=b.GetComponent<HealthComponent>();bh.Invincible=false;int prior=bh.CurrentHealth;
   b.TryReceiveDamage(new DamageInfo(1,player.transform.position,Vector2.zero,player.gameObject,TeamAlignment.Player));Check(b.profile.kind+" exposed body loses health",bh.CurrentHealth==prior-1);
   Position(player.gameObject,new Vector2(848.8f,100));yield return new WaitForSeconds(.3f);prior=bh.CurrentHealth;combat.PerformMeleeAttack();yield return new WaitForSeconds(.4f);Check(b.profile.kind+" can be hit by player sword input",bh.CurrentHealth<prior);
   b.enabled=false;b.GetComponent<Rigidbody2D>().simulated=false;
  }
  var first=bosses[0];Position(first.gameObject,new Vector2(850,100));Position(player.gameObject,new Vector2(852,100));first.enabled=true;Hold(first);first.FaceDialogueTarget(player.transform);yield return new WaitForSeconds(.3f);
  int old=hp.CurrentHealth;Call(first,"Windup","Heavy",1.5f);yield return new WaitForSeconds(1.3f);Check("Heavy charge is harmless",hp.CurrentHealth==old);yield return new WaitForSeconds(.55f);Check("Blade sweep damages reachable target",hp.CurrentHealth==old-2);Hold(first);
  Position(player.gameObject,new Vector2(854.2f,100));old=hp.CurrentHealth;Call(first,"Windup","Heavy",.1f);yield return new WaitForSeconds(.65f);Check("Old oversized melee radius removed",hp.CurrentHealth==old);Hold(first);
  Position(player.gameObject,new Vector2(852,100));var wall=new GameObject("Test solid wall");wall.transform.position=new Vector3(851,100,0);wall.AddComponent<BoxCollider2D>().size=new Vector2(.2f,8);Physics2D.SyncTransforms();old=hp.CurrentHealth;Call(first,"Strike",150f,first.WeaponReach,2);yield return new WaitForSeconds(.5f);Check("Melee cannot hit through terrain",hp.CurrentHealth==old);Object.Destroy(wall);yield return null;
  Position(player.gameObject,new Vector2(859,100));Hold(first);first.FaceDialogueTarget(player.transform);Call(first,"Wave",Vector2.right);yield return new WaitForSeconds(.15f);var shot=Object.FindObjectsByType<EnemyProjectile>(FindObjectsSortMode.None).FirstOrDefault(x=>x.effectArt=="BossWave41");
  Check("First boss wave uses new visible art",shot!=null&&shot.GetComponent<SpriteRenderer>().sprite.name.StartsWith("BossEffects41"));Vector2 sp=shot!=null?(Vector2)shot.transform.position:Vector2.zero;
  Check("Wave starts in front of body",shot!=null&&sp.x>851);yield return new WaitForSeconds(.16f);Check("Wave advances visibly",shot!=null&&shot.transform.position.x>sp.x+.5f);yield return new WaitForSeconds(.65f);Check("Wave stops at authored range",shot==null);
  first.enabled=false;
  var korah=bosses[1];Position(korah.gameObject,new Vector2(850,100));Position(player.gameObject,new Vector2(853,100));korah.enabled=true;Call(korah,"Change","Burrow",10f,"burrow");yield return null;var kh=korah.GetComponent<HealthComponent>();int kold=kh.CurrentHealth;korah.TryReceiveDamage(new DamageInfo(1,player.transform.position,Vector2.zero,player.gameObject,TeamAlignment.Player));Check("Underground Korah is invulnerable",kh.CurrentHealth==kold&&!korah.GetComponent<Collider2D>().enabled);
  Call(korah,"Change","EmergeWarning",.8f,"emerge");yield return new WaitForSeconds(.4f);Check("Visible emergence re-enables hurtbox",korah.BurrowVulnerable&&korah.GetComponent<Collider2D>().enabled&&!kh.Invincible);korah.TryReceiveDamage(new DamageInfo(1,player.transform.position,Vector2.zero,player.gameObject,TeamAlignment.Player));Check("Visible emergence takes damage",kh.CurrentHealth==kold-1);korah.enabled=false;
  var sky=bosses[2];Position(sky.gameObject,new Vector2(850,100));Position(player.gameObject,new Vector2(852,100));sky.enabled=true;Hold(sky);sky.FaceDialogueTarget(player.transform);Call(sky,"Windup","AirSides",.1f);yield return new WaitForSeconds(.34f);Check("Aerial boss uses separate left/right attacks",sky.State=="AirSides");yield return new WaitForSeconds(.4f);Check("Air attack changes side",sky.LookDirection==-1);sky.enabled=false;
  var bel=bosses[3];Position(bel.gameObject,new Vector2(850,100));Set(bel,"<Engaged>k__BackingField",true);Position(player.gameObject,new Vector2(854,100));old=hp.CurrentHealth;
  var trap=BossGroundHazard.Create(bel,new Vector2(854,99),true,.65f,1);yield return new WaitForSeconds(.5f);Check("Crystal trap warns before damage",hp.CurrentHealth==old&&!trap.Armed);Time.timeScale=0;yield return new WaitForSecondsRealtime(.4f);Check("Pause freezes trap warning and damage",trap!=null&&!trap.Armed&&hp.CurrentHealth==old);Time.timeScale=1;yield return new WaitForSeconds(.35f);Check("Crystal trap erupts and damages",hp.CurrentHealth==old-1);yield return new WaitForSeconds(.8f);Check("Crystal trap expires",trap==null);
  var nero=bosses[4];Position(nero.gameObject,new Vector2(850,100));Position(player.gameObject,new Vector2(853,100));nero.enabled=true;Hold(nero);nero.FaceDialogueTarget(player.transform);Call(nero,"Windup","Combo",.1f);yield return new WaitForSeconds(.78f);yield return new WaitForEndOfFrame();var anim=nero.GetComponent<CharacterPresentation>().animator;
  Check("Nero combo plays dedicated right slam",anim.GetCurrentAnimatorStateInfo(0).IsName("rightslamright"));yield return new WaitForSeconds(.65f);yield return new WaitForEndOfFrame();Check("Nero combo plays dedicated left slam",anim.GetCurrentAnimatorStateInfo(0).IsName("rightslamleft"));yield return new WaitForSeconds(.75f);Hold(nero);
  Position(player.gameObject,new Vector2(850.5f,100));old=hp.CurrentHealth;Set(nero,"comboReady",Time.time+5);Call(nero,"Windup","Dash",.1f);yield return new WaitForSeconds(.4f);Check("Nero travel pose cannot deal body-contact damage",hp.CurrentHealth==old);Hold(nero);
  var nh=nero.GetComponent<HealthComponent>();nh.Configure(44,21);yield return null;Check("Nero retains phase two transition",nero.PhaseTwo&&nero.State=="PhaseChange");yield return new WaitForSeconds(1.2f);Hold(nero);Call(nero,"Windup","Fan",.1f);yield return new WaitForSeconds(.25f);Check("Phase two emits 11 fan projectiles",Object.FindObjectsByType<EnemyProjectile>(FindObjectsSortMode.None).Count(x=>x.effectArt=="ArcaneOrb")==11);nero.enabled=false;
  foreach(var b in bosses){b.GetComponent<Rigidbody2D>().simulated=false;Hold(b);b.GetComponent<HealthComponent>().Invincible=false;yield return new WaitForSeconds(.3f);b.GetComponent<HealthComponent>().ReceiveDamage(new DamageInfo(999,player.transform.position,Vector2.zero,player.gameObject,TeamAlignment.Player));yield return null;Check(b==null?"Boss destroyed on death":"Boss death health",b==null||b.GetComponent<HealthComponent>().CurrentHealth==0);}
  File.WriteAllText("../reference/revision41/complete.txt",failures.Count+" failures / "+checks.Count+" checks");
 }
}
