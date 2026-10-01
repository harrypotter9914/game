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
using Babel.Runtime.Runes;
public static class BableRevision45Followup {
 static readonly List<string> checks=new(),failures=new();
 static void Check(string name,bool ok){checks.Add(name);if(!ok)failures.Add(name);File.WriteAllText("../reference/revision45/followup.json",JsonUtility.ToJson(new BableVerification.Report{checks=checks.ToArray(),failures=failures.ToArray()},true));}
 static void Set(object o,string name,object value)=>o.GetType().GetField(name,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(o,value);
 static PlayerController2D p;static PlayerCombatController combat;static GameSession session;
 static void Position(Vector2 center,Vector2 aim){var rb=p.GetComponent<Rigidbody2D>();rb.simulated=true;rb.constraints=RigidbodyConstraints2D.FreezeAll;rb.gravityScale=0;rb.position=center;p.transform.position=center;rb.linearVelocity=Vector2.zero;p.SetTestInput(aim.x,aim.y);Physics2D.SyncTransforms();}
 static void Ready(){Set(combat,"shockwaveReadyTime",0f);session.SetMana(session.MaxMana,session.MaxMana);}
 public static void Run(){checks.Clear();failures.Clear();File.WriteAllText("../reference/revision45/followup-complete.txt","RUNNING");new GameObject("Revision45 followup").AddComponent<BableTestHost>().StartCoroutine(Test());}
 static IEnumerator Test(){
  while(TowerLoading.Busy||BableGameUI.Instance==null||!BableGameUI.Instance.Ready)yield return null;
  TowerDialogue.AbortStory();BableGameUI.Instance.Resume();session=GameSession.Instance;session.ResetForNewGame();session.UnlockWeapon();session.UnlockAbility(AbilityId.Shockwave);
  p=Object.FindFirstObjectByType<PlayerController2D>();combat=p.GetComponent<PlayerCombatController>();p.GetComponent<HealthComponent>().Invincible=true;
  foreach(var speech in Object.FindObjectsByType<BossSpeech>(FindObjectsSortMode.None))speech.enabled=false;
  foreach(var e in Object.FindObjectsByType<EnemyControllerBase>(FindObjectsSortMode.None)){e.enabled=false;e.CancelInvoke();e.GetComponent<Rigidbody2D>().constraints=RigidbodyConstraints2D.FreezeAll;}
  var bosses=Object.FindObjectsByType<BossBrain>(FindObjectsSortMode.None).OrderBy(b=>(int)b.profile.kind).ToArray();foreach(var b in bosses)b.GetComponent<BossEncounter>().enabled=false;
  foreach(var b in bosses){
   Position(new Vector2(850,100),Vector2.right);var rb=b.GetComponent<Rigidbody2D>();rb.position=new Vector2(855,100);b.transform.position=rb.position;rb.constraints=RigidbodyConstraints2D.FreezeAll;
   Set(b,"<Engaged>k__BackingField",true);Set(b,"<State>k__BackingField","Recover");typeof(BossBrain).GetMethod("SyncBurrowBody",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(b,null);
   var h=b.GetComponent<HealthComponent>();h.Invincible=false;h.Configure(100,100);Physics2D.SyncTransforms();Ready();combat.PerformShockwave();yield return new WaitForSeconds(2.9f);
   Check(b.profile.kind+" takes repeated sustained beam damage",h.CurrentHealth<=86&&h.CurrentHealth>=82);
   if(b.profile.kind==BossKind.Burrow){Set(b,"<State>k__BackingField","Burrow");typeof(BossBrain).GetMethod("SyncBurrowBody",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(b,null);h.Configure(100,100);Ready();combat.PerformShockwave();yield return new WaitForSeconds(2.9f);Check("Korah remains immune while underground",h.CurrentHealth==100);Set(b,"<State>k__BackingField","Recover");}
   rb.position=new Vector2(900+(int)b.profile.kind*10,100);b.transform.position=rb.position;
  }
  var sun=AssetDatabase.LoadAssetAtPath<RuneDefinition>("Assets/Data/Runes/Sun.asset");var star=AssetDatabase.LoadAssetAtPath<RuneDefinition>("Assets/Data/Runes/Star.asset");session.RuneInventory.Collect(sun);session.RuneInventory.Collect(star);session.RuneInventory.TryActivateToEmptySlot(sun);session.RuneInventory.TryActivateToEmptySlot(star);
  Position(new Vector2(850,100),Vector2.right);Ready();combat.PerformShockwave();yield return new WaitForSeconds(1.7f);
  Check("Sun preserves 1.4x range: fourteen world units",Mathf.Abs(combat.LastShockwaveDistance-14)<.01f);
  Check("Star cannot create overlapping sustained casts",!combat.PerformShockwave()&&session.CurrentMana==session.MaxMana-1);yield return new WaitForSeconds(1.2f);Check("New cast is available after beam recovers",combat.PerformShockwave());combat.enabled=false;yield return null;combat.enabled=true;session.RuneInventory.ResetInventory();
  var group=GameObject.Find("Screenshot 1 - jump and shockwave upper blockage");var before=group.GetComponentsInChildren<BreakableWall>().Where(w=>Mathf.Abs(w.transform.position.x-420)<.9f).ToArray();
  Position(new Vector2(420,32.1f),Vector2.up);Ready();combat.PerformShockwave();yield return new WaitForSeconds(.85f);
  BableRevision45Visual.Map("late-thick-wall-upward",new Vector2(420,37),7,1200,1200);
  yield return new WaitForSeconds(2.05f);Check("Late ten-unit-thick barrier clears full five-brick depth in one cast",before.Length>=10&&before.All(w=>w==null));
  Check("Late excavation still costs one Spirit",session.CurrentMana==session.MaxMana-1);
  // Verify the original tile collision really disappears through the excavated column.
  bool obstruction=Physics2D.BoxCastAll(new Vector2(420,32.15f),new Vector2(.7f,.02f),0,Vector2.up,10.7f).Any(h=>TerrainMotion.Solid(h.collider));
  Check("Excavated late passage has no invisible tile collision",!obstruction);
  Position(new Vector2(60,-59.7f),Vector2.right);p.SetTestInput(0,0);var photoBody=p.GetComponent<Rigidbody2D>();photoBody.constraints=RigidbodyConstraints2D.FreezeRotation;photoBody.gravityScale=3;yield return new WaitForSeconds(.5f);Ready();combat.PerformShockwave();yield return new WaitForSeconds(.7f);BableRevision45Visual.Map("beam-grounded",new Vector2(65,-58.6f),4,1600,900);yield return new WaitForSeconds(2.2f);
  File.WriteAllText("../reference/revision45/followup-complete.txt",failures.Count+" failures / "+checks.Count+" checks");
 }
}
