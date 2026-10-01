using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Bable;
using Babel.Runtime.Core;
using Babel.Runtime.Combat;
using Babel.Runtime.Characters.Player;
using Babel.Runtime.Characters.Enemies;
using Babel.Runtime.World;
public static class BableRevision47Followup {
 static readonly List<string> checks=new(),failures=new();
 static void Check(string n,bool ok){checks.Add(n);if(!ok)failures.Add(n);File.WriteAllText("../reference/revision47/followup.json",JsonUtility.ToJson(new BableVerification.Report{checks=checks.ToArray(),failures=failures.ToArray()},true));}
 static void Set(object o,string n,object v)=>o.GetType().GetField(n,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(o,v);
 static PlayerController2D p;static PlayerCombatController combat;static GameSession session;
 static void Position(Vector2 v,bool frozen=true){p.GetComponent<HitRecoil>().Cancel();var b=p.GetComponent<Rigidbody2D>();b.constraints=frozen?RigidbodyConstraints2D.FreezeAll:RigidbodyConstraints2D.FreezeRotation;b.gravityScale=frozen?0:3;b.position=v;p.transform.position=v;b.linearVelocity=Vector2.zero;p.SetTestInput(0,0);Physics2D.SyncTransforms();}
 static void Ready(){Set(combat,"shockwaveReadyTime",0f);session.SetMana(session.MaxMana,session.MaxMana);}
 public static void Run(){checks.Clear();failures.Clear();File.WriteAllText("../reference/revision47/followup-complete.txt","RUNNING");new GameObject("Muzzle checks").AddComponent<BableTestHost>().StartCoroutine(Test());}
 static IEnumerator Test(){
  while(TowerLoading.Busy||BableGameUI.Instance==null||!BableGameUI.Instance.Ready)yield return null;
  BableGameUI.Instance.Resume();TowerDialogue.AbortStory();session=GameSession.Instance;session.ResetForNewGame();session.UnlockWeapon();session.UnlockAbility(AbilityId.Shockwave);session.UnlockAbility(AbilityId.CrystalDash);
  p=Object.FindFirstObjectByType<PlayerController2D>();combat=p.GetComponent<PlayerCombatController>();var hp=p.GetComponent<HealthComponent>();hp.Invincible=true;
  foreach(var e in Object.FindObjectsByType<EnemyControllerBase>(FindObjectsSortMode.None)){e.enabled=false;e.CancelInvoke();e.GetComponent<Rigidbody2D>().constraints=RigidbodyConstraints2D.FreezeAll;}
  foreach(var s in Object.FindObjectsByType<BossSpeech>(FindObjectsSortMode.None))s.enabled=false;
  foreach(var axis in new[]{Vector2.right,Vector2.left,Vector2.up,Vector2.down}){
   combat.InterruptForHit();Position(new Vector2(850,100));p.SetTestInput(axis.x,axis.y);Ready();combat.PerformShockwave();yield return new WaitForSeconds(.7f);yield return new WaitForEndOfFrame();
   var sr=p.GetComponent<CharacterPresentation>().animator.GetComponent<SpriteRenderer>();var fx=Object.FindFirstObjectByType<DirectionalShockwaveVisual>();
   Check(axis+" uses clean extended-palm release pose",sr.sprite.name=="PilgrimExtras_24");
   Check(axis+" effect and damage start at same hand socket",fx!=null&&Vector2.Distance(fx.transform.position,combat.ShockwaveOrigin)<.005f);
   Check(axis+" beam originates away from torso",Vector2.Distance(combat.ShockwaveOrigin,p.GetComponent<Collider2D>().bounds.center)>.75f);
   Check(axis+" full ten-unit range from hand",Mathf.Abs(combat.LastShockwaveDistance-10)<.01f);
   BableRevision47Visual.Map("hand-"+(axis==Vector2.right?"right":axis==Vector2.left?"left":axis==Vector2.up?"up":"down"),new Vector2(850,101)+axis*3,5,1400,1000);
   while(combat.IsShockwaveActive)yield return null;
  }
  Position(new Vector2(60,-60.18f),false);yield return new WaitForSeconds(.35f);p.SetTestInput(1,0);p.SetTestInput(0,0);Ready();combat.PerformShockwave();yield return new WaitForSeconds(.6f);
  yield return new WaitForEndOfFrame();BableRevision47Visual.Map("hand-cast-game",new Vector2(65,-58.8f),4,1600,900);p.SetTestInput(-1,0);yield return new WaitForSeconds(.5f);yield return new WaitForEndOfFrame();
  var moving=p.GetComponent<CharacterPresentation>().animator.GetComponent<SpriteRenderer>();Check("Moving cast keeps full run cycle and locked aim",moving.sprite.name.StartsWith("Pilgrim_run_")&&combat.LastShockwaveAxis==Vector2.right&&p.Velocity.x<0);
  var movingFx=Object.FindFirstObjectByType<DirectionalShockwaveVisual>();Check("Running beam remains attached to current gauntlet",movingFx!=null&&Vector2.Distance(movingFx.transform.position,combat.ShockwaveOrigin)<.005f);
  BableRevision47Visual.Map("hand-cast-moving",new Vector2(p.transform.position.x+4,-58.8f),4,1600,900);yield return new WaitForSeconds(1.75f);p.SetTestInput(0,0);
  Position(new Vector2(850,100));p.SetTestInput(1,0);var wall=new GameObject("Hand-path solid wall");wall.transform.position=new Vector2(850.8f,100.6f);wall.AddComponent<BoxCollider2D>().size=new Vector2(.2f,4);
  var target=new GameObject("Protected target");target.transform.position=new Vector2(853,100.6f);target.AddComponent<BoxCollider2D>();var targetHp=target.AddComponent<HealthComponent>();targetHp.Configure(100,100);Physics2D.SyncTransforms();Ready();combat.PerformShockwave();yield return new WaitForSeconds(.65f);
  Check("Hand beyond ordinary wall cannot bypass obstruction",combat.LastShockwaveDistance==0&&targetHp.CurrentHealth==100);yield return new WaitForSeconds(2.15f);Object.Destroy(wall);Object.Destroy(target);yield return null;
  Position(new Vector2(850,100),false);hp.Invincible=false;Set(hp,"invulnerableUntil",0f);var attacker=new GameObject("Moved projectile owner");attacker.transform.position=new Vector2(855,100);
  hp.ReceiveDamage(new DamageInfo(1,new Vector2(849.6f,100),Vector2.right*9,attacker,TeamAlignment.Enemy));yield return new WaitForSeconds(.1f);Check("Projectile direction wins over owner's later location",p.Velocity.x>0&&p.IsRecoiling);yield return new WaitForSeconds(.3f);
  Position(new Vector2(850,100),false);Set(hp,"invulnerableUntil",0f);p.StartDash(Vector2.right,26,.7f);hp.ReceiveDamage(new DamageInfo(1,attacker.transform.position,Vector2.left,attacker,TeamAlignment.Enemy));Check("Actual damage interrupts dash and restores gravity",!p.IsDashing&&p.GetComponent<Rigidbody2D>().gravityScale==3&&p.IsRecoiling);yield return new WaitForSeconds(.4f);
  Position(new Vector2(850,100));Ready();combat.PerformShockwave();yield return new WaitForSeconds(.35f);Set(hp,"invulnerableUntil",0f);hp.ReceiveDamage(new DamageInfo(999,attacker.transform.position,Vector2.left,attacker,TeamAlignment.Enemy));yield return null;yield return null;
  Check("Fatal hit cancels attacks without post-death recoil",!combat.IsShockwaveActive&&!p.IsRecoiling&&Object.FindFirstObjectByType<DirectionalShockwaveVisual>()==null);BableGameUI.Instance.Resume();p.GetComponent<PlayerRespawnController>().Respawn(false);Check("Respawn has no residual knockback",!p.IsRecoiling&&Mathf.Abs(p.Velocity.x)<.01f);
  Object.Destroy(attacker);File.WriteAllText("../reference/revision47/followup-complete.txt",failures.Count+" failures / "+checks.Count+" checks");
 }
}
