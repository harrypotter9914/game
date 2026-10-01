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
public static class BableRevision44Tests {
 static readonly List<string> checks=new(),failures=new();
 static void Check(string name,bool ok){checks.Add(name);if(!ok)failures.Add(name);File.WriteAllText("../reference/revision44/tests.json",JsonUtility.ToJson(new BableVerification.Report{checks=checks.ToArray(),failures=failures.ToArray()},true));}
 static void Set(object o,string name,object value)=>o.GetType().GetField(name,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(o,value);
 static object Call(object o,string name,params object[] args)=>o.GetType().GetMethod(name,BindingFlags.NonPublic|BindingFlags.Instance).Invoke(o,args);
 static void Active(BossEncounter b)=>typeof(BossEncounter).GetField("<Active>k__BackingField",BindingFlags.NonPublic|BindingFlags.Static).SetValue(null,b);
 static void Position(PlayerController2D p,Vector2 v,bool freeze=false){var rb=p.GetComponent<Rigidbody2D>();rb.simulated=true;rb.constraints=freeze?RigidbodyConstraints2D.FreezeAll:RigidbodyConstraints2D.FreezeRotation;rb.gravityScale=3;rb.position=v;p.transform.position=v;rb.linearVelocity=Vector2.zero;p.SetTestInput(0,0);Physics2D.SyncTransforms();}
 static void Capture(string name)=>ScreenCapture.CaptureScreenshot(Path.GetFullPath("../reference/revision44/"+name+".png"));
 public static void Run(){checks.Clear();failures.Clear();File.WriteAllText("../reference/revision44/complete.txt","RUNNING");new GameObject("Revision 44 verification").AddComponent<BableTestHost>().StartCoroutine(Test());}
 static IEnumerator Test(){
  while(TowerLoading.Busy||BableGameUI.Instance==null||!BableGameUI.Instance.Ready)yield return null;
  TowerDialogue.AbortStory();BableGameUI.Instance.Resume();var session=GameSession.Instance;session.ResetForNewGame();session.UnlockWeapon();
  var p=Object.FindFirstObjectByType<PlayerController2D>();p.GetComponent<HealthComponent>().Invincible=true;
  foreach(var speech in Object.FindObjectsByType<BossSpeech>(FindObjectsSortMode.None))speech.enabled=false;
  foreach(var e in Object.FindObjectsByType<EnemyControllerBase>(FindObjectsSortMode.None)){e.enabled=false;e.CancelInvoke();e.GetComponent<Rigidbody2D>().simulated=false;}
  var bosses=Object.FindObjectsByType<BossBrain>(FindObjectsSortMode.None).OrderBy(b=>(int)b.profile.kind).ToArray();foreach(var b in bosses)b.GetComponent<BossEncounter>().enabled=false;
  Check("No wall jump or double jump in initial test",!session.HasAbility(AbilityId.WallJump)&&!session.HasAbility(AbilityId.DoubleJump)&&!session.HasAbility(AbilityId.Shockwave));
  var walls=Object.FindObjectsByType<BreakableWall>(FindObjectsSortMode.None);
  Check("Every campaign shockwave block uses new fracture material",walls.All(w=>!w.requiresShockwave||w.GetComponent<SpriteRenderer>().sprite.name.StartsWith("FracturedStone44")));
  Check("Upper shaft has twelve solid blocks",GameObject.Find("Return shaft upper seal").GetComponentsInChildren<BreakableWall>().Length==12);
  foreach(float x in new[]{87.6f,90f,92.4f}){Position(p,new Vector2(x,-46));yield return new WaitForSeconds(1.1f);Check("No-ability drop lands on upper seal x="+x,p.IsGrounded&&p.GetComponent<Collider2D>().bounds.min.y>=-51.08f);}
  Capture("sealed-shaft-game");
  Position(p,new Vector2(90,-49.8f));yield return new WaitForSeconds(.3f);p.SetTestInput(0,0,true);yield return new WaitForSeconds(.4f);p.SetTestInput(1,0);yield return new WaitForSeconds(.27f);p.SetTestInput(0,0);yield return new WaitForSeconds(.95f);Check("Ordinary jump can return to right ledge",p.IsGrounded&&p.GetComponent<Collider2D>().bounds.min.y>=-47.08f);
  Position(p,new Vector2(201,-34));yield return new WaitForSeconds(1);Check("Central descent is sealed before Shockwave",p.IsGrounded&&p.GetComponent<Collider2D>().bounds.min.y>=-37.08f);
  Position(p,new Vector2(303,-34));yield return new WaitForSeconds(1);Check("Vine shaft blocks premature descent",p.IsGrounded&&p.GetComponent<Collider2D>().bounds.min.y>=-37.08f);
  var groups=walls.GroupBy(w=>w.transform.parent==null?w.name:w.transform.parent.name).ToArray();
  foreach(var g in groups){var w=g.First();w.ReceiveDamage(new DamageInfo(99,w.transform.position,Vector2.zero,p.gameObject,TeamAlignment.Player));yield return null;Check(g.Key+" rejects ordinary sword",w!=null&&w.GetComponent<Collider2D>().enabled);}
  session.UnlockAbility(AbilityId.Shockwave);var combat=p.GetComponent<PlayerCombatController>();
  string[] targets={"Return shaft upper seal","Korah lower crossing","Korah east crossing","Image 7 - upper descent threshold","Vine shaft upper seal","Azazel upper entry seal"};
  Vector2[] positions={new Vector2(90,-49),new Vector2(285.5f,-60),new Vector2(291.5f,-60),new Vector2(201,-35),new Vector2(303,-35),new Vector2(313.5f,-25)};
  Vector2[] axes={Vector2.down,Vector2.down,Vector2.right,Vector2.down,Vector2.down,Vector2.right};
  for(int i=0;i<targets.Length;i++){var group=GameObject.Find(targets[i]);int before=group.GetComponentsInChildren<BreakableWall>().Length;Position(p,positions[i],true);combat.StartCoroutine((IEnumerator)Call(combat,"ShockwaveSequence",axes[i]));yield return new WaitForSeconds(1);Check(targets[i]+" opens with actual directional Shockwave",group.GetComponentsInChildren<BreakableWall>().Length<before);}
  // All other authored groups use the same damage and tile-clear contract.
  foreach(var g in groups){var w=g.FirstOrDefault(w=>w!=null);if(w==null)continue;var bounds=w.GetComponent<Collider2D>().bounds;w.HitByShockwave();yield return null;Check(g.Key+" destroys breakable collider",w==null);}
  Position(p,new Vector2(60,-59),true);var hud=Object.FindFirstObjectByType<BossHealthHud>();
  foreach(var b in bosses){var encounter=b.GetComponent<BossEncounter>();var hp=encounter.Health;hp.Invincible=false;hp.Configure(b.profile.health,b.profile.health);Active(encounter);yield return new WaitForSeconds(.4f);Check(b.profile.kind+" full visual health",hud.Visible&&Mathf.Abs(hud.HealthFraction-1)<.001f);hp.ApplyDamage(b.profile.health/2);yield return new WaitForSeconds(.7f);Check(b.profile.kind+" health bar follows actual damage",Mathf.Abs(hud.HealthFraction-hp.CurrentHealth/(float)hp.MaxHealth)<.001f);Capture("hud-"+b.profile.kind);yield return new WaitForSeconds(.15f);Time.timeScale=0;float saved=hud.HealthFraction;yield return new WaitForSecondsRealtime(.2f);Check(b.profile.kind+" pause preserves health",hud.HealthFraction==saved);Time.timeScale=1;hp.Configure(b.profile.health,b.profile.health);yield return null;Check(b.profile.kind+" reset restores full bar",hud.HealthFraction==1);}
  var distinct=new HashSet<string>();foreach(var b in bosses){Active(b.GetComponent<BossEncounter>());yield return null;distinct.Add(hud.FrameName);}Check("Five independently generated distinct frames",distinct.Count==5&&!distinct.Contains(""));
  foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1024,768),new Vector2Int(1920,1080)}){BableDisplayTests.Size(size.x,size.y);yield return new WaitForSecondsRealtime(.3f);var corners=new Vector3[4];hud.FrameRect.GetWorldCorners(corners);var v=DisplayFrame.Viewport;Check("Boss frame inside safe viewport "+size,corners.All(c=>c.x>=v.xMin*Screen.width&&c.x<=v.xMax*Screen.width&&c.y>=v.yMin*Screen.height&&c.y<=v.yMax*Screen.height));}BableDisplayTests.Size(1600,900);yield return new WaitForSecondsRealtime(.3f);
  BableGameUI.Instance.Pause();yield return null;Check("Pause hides gameplay health bar",!hud.Visible);BableGameUI.Instance.Resume();yield return null;Active(null);yield return null;Check("No boss bar outside encounter",!hud.Visible);
  var korah=bosses[1];Set(korah,"<Engaged>k__BackingField",true);Call(korah,"Change","Burrow",30f,"burrow");Position(p,new Vector2(270,-58),true);yield return new WaitForSeconds(.3f);var burrow=korah.GetComponent<BurrowPresentation>();Check("Burrowing uses animated physical rubble",burrow.TrailVisible&&burrow.TrailArt.StartsWith("BurrowRubble44"));string previous=burrow.TrailArt;yield return new WaitForSeconds(.11f);Check("Burrow rubble animates",previous!=burrow.TrailArt);yield return new WaitForSeconds(.9f);BableRevision44Visual.Map("burrow-close",(Vector2)korah.transform.position+Vector2.up*.5f,4,1200,700);Capture("burrow-game");
  File.WriteAllText("../reference/revision44/complete.txt",failures.Count+" failures / "+checks.Count+" checks");
 }
}


