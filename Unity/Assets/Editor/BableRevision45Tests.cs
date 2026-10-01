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
using Babel.Runtime.World;
public static class BableRevision45Tests {
 static readonly List<string> checks=new(),failures=new();
 static void Check(string name,bool ok){checks.Add(name);if(!ok)failures.Add(name);File.WriteAllText("../reference/revision45/tests.json",JsonUtility.ToJson(new BableVerification.Report{checks=checks.ToArray(),failures=failures.ToArray()},true));}
 static void Set(object o,string name,object value)=>o.GetType().GetField(name,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(o,value);
 static PlayerController2D p;static PlayerCombatController combat;static GameSession session;static GameObject fixture;
 static void Position(Vector2 center,Vector2 aim){var rb=p.GetComponent<Rigidbody2D>();rb.simulated=true;rb.constraints=RigidbodyConstraints2D.FreezeAll;rb.gravityScale=0;rb.position=center;p.transform.position=center;rb.linearVelocity=Vector2.zero;p.SetTestInput(aim.x,aim.y);Physics2D.SyncTransforms();}
 static GameObject Box(string name,Vector2 at,Vector2 size){var go=new GameObject(name);go.transform.SetParent(fixture.transform);go.transform.position=at;go.AddComponent<BoxCollider2D>().size=size;return go;}
 static HealthComponent Target(Vector2 at){var go=Box("Damage target",at,new Vector2(.4f,.4f));var h=go.AddComponent<HealthComponent>();h.Configure(100,100);var child=new GameObject("Second hurt collider");child.transform.SetParent(go.transform,false);child.AddComponent<BoxCollider2D>().size=new Vector2(.2f,.2f);return h;}
 static Vector2 Size(Vector2 axis,float length,float breadth)=>Mathf.Abs(axis.y)>.5f?new Vector2(breadth,length):new Vector2(length,breadth);
 static void ReadyCast(){Set(combat,"shockwaveReadyTime",0f);session.SetMana(session.MaxMana,session.MaxMana);}
 public static void Run(){checks.Clear();failures.Clear();File.WriteAllText("../reference/revision45/complete.txt","RUNNING");new GameObject("Revision45 checks").AddComponent<BableTestHost>().StartCoroutine(Test());}
 static IEnumerator Test(){
  while(TowerLoading.Busy||BableGameUI.Instance==null||!BableGameUI.Instance.Ready)yield return null;
  TowerDialogue.AbortStory();BableGameUI.Instance.Resume();session=GameSession.Instance;session.ResetForNewGame();session.UnlockWeapon();
  p=Object.FindFirstObjectByType<PlayerController2D>();combat=p.GetComponent<PlayerCombatController>();p.GetComponent<HealthComponent>().Invincible=true;
  foreach(var speech in Object.FindObjectsByType<BossSpeech>(FindObjectsSortMode.None))speech.enabled=false;
  var bosses=Object.FindObjectsByType<BossBrain>(FindObjectsSortMode.None).OrderBy(b=>(int)b.profile.kind).ToArray();
  foreach(var e in Object.FindObjectsByType<EnemyControllerBase>(FindObjectsSortMode.None)){e.enabled=false;e.CancelInvoke();e.GetComponent<Rigidbody2D>().constraints=RigidbodyConstraints2D.FreezeAll;}
  foreach(var b in bosses)b.GetComponent<BossEncounter>().enabled=false;
  Check("No cast before ability unlock",!combat.PerformShockwave()&&session.CurrentMana==3);session.UnlockAbility(AbilityId.Shockwave);
  Check("Definition: five bricks, 2.5 seconds, 0.3 second damage interval",p.Definition.ShockwaveRange==10&&p.Definition.ShockwaveDuration==2.5f&&p.Definition.ShockwaveTickInterval==.3f);
  Check("Generated column is packaged",Resources.Load<Texture2D>("Bable/NewArt/PlayerBeam45")!=null);
  foreach(var axis in new[]{Vector2.right,Vector2.left,Vector2.up,Vector2.down}){
   fixture=new GameObject("Beam fixtures "+axis);Vector2 origin=new Vector2(850,100);Position(origin,axis);ReadyCast();
   var breakables=new List<BreakableWall>();for(int n=0;n<5;n++)breakables.Add(Box("Successive fracture "+n,origin+axis*(2+n),Size(axis,1,2)).AddComponent<BreakableWall>());
   Box("Ordinary masonry",origin+axis*8,Size(axis,.5f,3));var protectedBrick=Box("Protected fracture",origin+axis*9,Size(axis,1,2)).AddComponent<BreakableWall>();
   var victim=Target(origin+axis*.95f);var behind=Target(origin+axis*9.8f);Physics2D.SyncTransforms();
   Check(axis+" cast accepted",combat.PerformShockwave());Check(axis+" one Spirit only",session.CurrentMana==session.MaxMana-1);
   Check(axis+" repeated input cannot overlap or spend again",!combat.PerformShockwave()&&session.CurrentMana==session.MaxMana-1);
   yield return new WaitForSeconds(.8f);
   Check(axis+" one cast breaks all five successive layers",breakables.All(w=>w==null));
   Check(axis+" beam clips at ordinary masonry",Mathf.Abs(combat.LastShockwaveDistance-7.74f)<.06f);
   Check(axis+" ordinary wall protects further blocks and enemies",protectedBrick!=null&&behind.CurrentHealth==100);
   int hp=victim.CurrentHealth;yield return new WaitForSeconds(1.5f);
   Check(axis+" still active at 2.3 seconds",combat.IsShockwaveActive);
   Check(axis+" continuous damage resumes without duplicate collider hits",victim.CurrentHealth<hp&&victim.CurrentHealth>=82);
   yield return new WaitForSeconds(.6f);
   Check(axis+" ends after 2.5 second active duration",!combat.IsShockwaveActive&&Object.FindFirstObjectByType<DirectionalShockwaveVisual>()==null);
   Check(axis+" exactly nine base damage ticks",victim.CurrentHealth==82);
   Object.Destroy(fixture);yield return null;
  }
  fixture=new GameObject("Pause and movement");Position(new Vector2(850,100),Vector2.right);ReadyCast();var pauseTarget=Target(new Vector2(851,100));combat.PerformShockwave();yield return new WaitForSeconds(.6f);
  int pausedHp=pauseTarget.CurrentHealth;float reach=combat.LastShockwaveDistance;BableGameUI.Instance.Pause();yield return new WaitForSecondsRealtime(.8f);
  Check("Pause freezes ongoing beam damage and duration",pauseTarget.CurrentHealth==pausedHp&&combat.IsShockwaveActive&&combat.LastShockwaveDistance==reach);
  BableGameUI.Instance.Resume();yield return new WaitForSeconds(.5f);Check("Resume continues damage",pauseTarget.CurrentHealth<pausedHp);yield return new WaitForSeconds(2);Object.Destroy(fixture);yield return null;
  Position(new Vector2(850,100),Vector2.right);ReadyCast();combat.PerformShockwave();yield return new WaitForSeconds(.4f);
  var body=p.GetComponent<Rigidbody2D>();body.constraints=RigidbodyConstraints2D.FreezeRotation;body.gravityScale=0;p.SetTestInput(-1,0);float x=body.position.x;yield return new WaitForSeconds(.6f);
  var fx=Object.FindFirstObjectByType<DirectionalShockwaveVisual>();
  Check("Player can move and turn while firing",body.position.x<x-1&&p.FacingSign==-1);
  Check("Beam direction stays locked despite turning",combat.LastShockwaveAxis==Vector2.right&&fx!=null&&fx.Axis==Vector2.right);
  Check("Beam follows moving player",fx!=null&&Vector2.Distance(fx.transform.position,p.GetComponent<Collider2D>().bounds.center)<.2f);
  combat.enabled=false;yield return null;Check("Disabling combat cancels beam and damage",!combat.IsShockwaveActive&&Object.FindFirstObjectByType<DirectionalShockwaveVisual>()==null);combat.enabled=true;
  Position(new Vector2(60,-59),Vector2.right);ReadyCast();combat.PerformShockwave();yield return new WaitForSeconds(.6f);BableRevision45Visual.Map("beam-in-boss-room",new Vector2(65,-58),4,1600,900);yield return new WaitForSeconds(2.3f);
  // Actual map: use the public cast entry point and one Spirit for each thick seal.
  string[] targets={"Return shaft upper seal","Korah lower crossing","Korah east crossing","Vine shaft upper seal","Azazel upper entry seal"};
  Vector2[] positions={new Vector2(90,-49.8f),new Vector2(285.5f,-60),new Vector2(291.5f,-60),new Vector2(303,-35),new Vector2(313.5f,-25)};
  Vector2[] axes={Vector2.down,Vector2.down,Vector2.right,Vector2.down,Vector2.right};
  for(int i=0;i<targets.Length;i++){var group=GameObject.Find(targets[i]);int before=group.GetComponentsInChildren<BreakableWall>().Length;Position(positions[i],axes[i]);ReadyCast();Check(targets[i]+" accepts cast",combat.PerformShockwave());yield return new WaitForSeconds(2.9f);Check(targets[i]+" multiple map blocks broken for one Spirit",group.GetComponentsInChildren<BreakableWall>().Length<=before-2&&session.CurrentMana==session.MaxMana-1);}
  Position(new Vector2(850,100),Vector2.right);session.SetMana(0,session.MaxMana);Set(combat,"shockwaveReadyTime",0f);Check("Empty Spirit rejects cast",!combat.PerformShockwave());
  session.SetMana(session.MaxMana,session.MaxMana);combat.PerformShockwave();yield return new WaitForSeconds(.4f);p.GetComponent<HealthComponent>().ApplyDamage(999);yield return null;yield return null;
  Check("Death immediately removes beam even when death menu pauses",!combat.IsShockwaveActive&&Object.FindFirstObjectByType<DirectionalShockwaveVisual>()==null);
  BableGameUI.Instance.Resume();p.GetComponent<PlayerRespawnController>().Respawn(false);Position(new Vector2(850,100),Vector2.right);
  foreach(var b in bosses){int before=session.MaxMana;var hp=b.GetComponent<HealthComponent>();hp.Invincible=false;hp.ApplyDamage(999);yield return null;Check(b.profile.kind+" actual death grants one maximum Spirit",session.MaxMana==before+1);Check(b.profile.kind+" duplicate reward rejected",!session.GrantBossMana(b.profile.kind.ToString())&&session.MaxMana==before+1);}
  Check("Five first victories grow Spirit from 3 to 8",session.MaxMana==8&&session.BossManaUpgrades==5);
  session.AddPermanentMaxMana(1);Check("Existing shop upgrade stacks without replacing Boss growth",session.MaxMana==9);
  foreach(var rune in AssetDatabase.FindAssets("t:RuneDefinition").Select(g=>AssetDatabase.LoadAssetAtPath<Babel.Runtime.Runes.RuneDefinition>(AssetDatabase.GUIDToAssetPath(g)))){session.RuneInventory.Collect(rune);session.RuneInventory.TryActivateToEmptySlot(rune);}
  Check("Rune recalculation preserves permanent maximum Spirit",session.MaxMana>=9);
  session.SetMana(0,session.MaxMana);p.GetComponent<PlayerRespawnController>().Respawn(false);Check("Respawn retains expanded capacity and refills it",session.MaxMana>=9&&session.CurrentMana==session.MaxMana&&p.GetComponent<ManaComponent>().MaxMana==session.MaxMana);
  session.ResetForNewGame();Check("New game clears first-defeat rewards and resets capacity",session.MaxMana==3&&session.BossManaUpgrades==0);
  File.WriteAllText("../reference/revision45/complete.txt",failures.Count+" failures / "+checks.Count+" checks");
 }
}
