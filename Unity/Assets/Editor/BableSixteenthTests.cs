using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEditor;
using Bable;
using Babel.Runtime.World;
using Babel.Runtime.Characters.Player;
using Babel.Runtime.Characters.Enemies;
using Babel.Runtime.Combat;
using Babel.Runtime.Core;
using Babel.Runtime.Runes;
using Babel.Runtime.Shop;
public static class BableSixteenthTests {
 static List<string> checks=new(),failures=new(),trace=new();static string output;
 static void Check(string name,bool pass){checks.Add(name);if(!pass)failures.Add(name);File.WriteAllText("../reference/revision16/"+output+".json",JsonUtility.ToJson(new BableVerification.Report{checks=checks.ToArray(),failures=failures.ToArray()},true));}
 static void Save(){File.WriteAllLines("../reference/revision16/"+output+"-trace.txt",trace);}
 public static void Run(){checks.Clear();failures.Clear();trace.Clear();output="main-tests";new GameObject("Revision16 audit").AddComponent<BableTestHost>().StartCoroutine(Main());}
 public static void RunLab(){checks.Clear();failures.Clear();trace.Clear();output="lab-tests";new GameObject("Revision16 lab audit").AddComponent<BableTestHost>().StartCoroutine(Lab());}
 static bool Clear(Collider2D shape){return !Physics2D.OverlapBoxAll(shape.bounds.center,shape.bounds.size-Vector3.one*.08f,0).Any(c=>c!=shape&&TerrainMotion.Solid(c)&&Physics2D.Distance(shape,c).distance<-.06f);}
 static IEnumerator Main(){
  var ui=BableGameUI.Instance;ui.Begin();yield return new WaitForSeconds(.4f);var session=GameSession.Instance;var p=Object.FindFirstObjectByType<PlayerController2D>();var rb=p.GetComponent<Rigidbody2D>();p.SetTestInput(0,0);p.GetComponent<HealthComponent>().Invincible=true;
  Check("Campaign starts without granted runes",session.RuneInventory.CollectedRunes.Count==0);
  foreach(var e in Object.FindObjectsByType<EnemyControllerBase>(FindObjectsSortMode.None)){e.enabled=false;e.GetComponent<Rigidbody2D>().simulated=false;}foreach(var s in Object.FindObjectsByType<BossSpeech>(FindObjectsSortMode.None))s.enabled=false;TowerDialogue.AbortStory();
  ui.Runes();yield return new WaitForSecondsRealtime(.5f);var view=Object.FindFirstObjectByType<RuneRepositoryView>();Check("Empty repository opens with twelve independent cells",view!=null&&Enumerable.Range(0,12).All(i=>view.DisplayedRuneAt(i)==null));BableVerification.Capture("revision16/runes-empty.png");
  var runes=Resources.LoadAll<RuneDefinition>("Bable/Runes").Reverse().ToArray();foreach(var r in runes)session.CollectRune(r);yield return null;
  Check("Eight unique recovered runes, no automatic equipment",session.RuneInventory.CollectedRunes.Count==8&&session.RuneInventory.ActiveSlots.All(r=>r==null));Check("Storage follows acquisition order",Enumerable.Range(0,8).All(i=>view.DisplayedRuneAt(i+4)==runes[i]));Check("Duplicate acquisition is rejected",!session.CollectRune(runes[0]));
  var hover=GameObject.Find("Rune cell 7");ExecuteEvents.Execute(hover,new PointerEventData(EventSystem.current),ExecuteEvents.pointerEnterHandler);Check("Pointer hover moves selection to corresponding cell",view.FocusedCell==7);yield return new WaitForSecondsRealtime(.5f);BableVerification.Capture("revision16/runes-all.png");
  for(int i=0;i<4;i++){GameObject.Find("Rune cell 4").GetComponent<Button>().onClick.Invoke();yield return new WaitForSecondsRealtime(.35f);Check("Click equips next available socket "+i,session.RuneInventory.ActiveSlots[i]==runes[i]);}
  Check("Four occupied sockets reject a fifth rune",!view.ActivateCell(4)&&session.RuneInventory.ActiveSlots.Count(r=>r!=null)==4);BableVerification.Capture("revision16/runes-equipped.png");
  Check("Clicking equipped rune removes it",view.ActivateCell(1));yield return new WaitForSecondsRealtime(.35f);Check("Removed rune returns at acquisition-order position",view.DisplayedRuneAt(4)==runes[1]&&session.RuneInventory.ActiveSlots[1]==null);Check("Re-equipping fills vacant socket",view.ActivateCell(4));yield return new WaitForSecondsRealtime(.35f);Check("No duplicated runes across both sections",Enumerable.Range(0,12).Select(view.DisplayedRuneAt).Where(r=>r!=null).Distinct().Count()==8);
  ui.Resume();session.RuneInventory.ResetInventory();yield return null;
  var altars=Object.FindObjectsByType<AltarCheckpoint>(FindObjectsSortMode.None);Check("All six campaign checkpoints are altars",altars.Length==6);Check("Altars have no star pickup components",altars.All(a=>a.GetComponent<CollectiblePickup>()==null&&a.GetComponent<AnimatedTreasure>()==null));
  foreach(var a in altars){
   p.GetComponent<HealthComponent>().Configure(session.MaxHealth,2);session.SetHealth(2,session.MaxHealth);session.SetMana(1,session.MaxMana);p.GetComponent<PlayerRuntimeState>().SynchronizeFromSession();
   p.transform.position=a.PlayerPosition(p);rb.position=p.transform.position;rb.linearVelocity=Vector2.zero;Physics2D.SyncTransforms();Camera.main.GetComponent<CameraFollow2D>().SetTarget(p.transform);yield return new WaitForSeconds(2.8f);
   var shape=p.GetComponent<Collider2D>();var vp=Camera.main.WorldToViewportPoint(p.transform.position);Check("Auto altar activation at "+a.transform.position,a.IsActive&&Object.FindFirstObjectByType<CheckpointService>().CurrentCheckpoint==a.Marker);Check("Activation keeps health and spirit at "+a.transform.position,session.CurrentHealth==2&&session.CurrentMana==1);Check("Altar landing clear at "+a.transform.position,Clear(shape)&&Mathf.Abs(shape.bounds.min.y-a.transform.position.y)<.09f);trace.Add("Altar "+a.transform.position+" feet="+shape.bounds.min.y+" screenY="+vp.y+" camera="+Camera.main.transform.position);
   Check("Player not pinned to lower screen at "+a.transform.position,vp.y>.27f&&vp.y<.76f);
  }
  var last=altars.Last();last.Activate();p.GetComponent<PlayerRespawnController>().Respawn();yield return new WaitForSeconds(.15f);Check("Rebirth plays with controls locked and feet on altar",p.GetComponent<AltarRebirth>().IsRunning&&!p.enabled&&Mathf.Abs(p.GetComponent<Collider2D>().bounds.min.y-last.transform.position.y)<.09f);BableVerification.Capture("revision16/altar-rebirth.png");yield return new WaitForSeconds(1.2f);Check("Rebirth restores controls and physics",!p.GetComponent<AltarRebirth>().IsRunning&&p.enabled&&rb.simulated);Check("Rebirth restores configured health",session.CurrentHealth==session.MaxHealth);BableVerification.Capture("revision16/altar-active.png");
  foreach(var family in new[]{"FirstPenitent","BuriedOne","SkyJudicator","Nero","MeleeGuard","ShieldGuard","GiantGuard","RangedGuard"}){
   var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Art/NativeAnimations/"+family+"_run.anim");var binding=AnimationUtility.GetObjectReferenceCurveBindings(clip).First();var frames=AnimationUtility.GetObjectReferenceCurve(clip,binding).Select(k=>(Sprite)k.value).ToArray();Check(family+" uses sixteen distinct alternating frames",frames.Length==16&&frames.Distinct().Count()==16);Check(family+" preserves a consistent foot pivot",frames.All(f=>f.pivot==frames[0].pivot&&f.pixelsPerUnit==frames[0].pixelsPerUnit));
  }
  Check("Main map still has five bosses",Object.FindObjectsByType<BossBrain>(FindObjectsSortMode.None).Length==5);Save();Debug.Log("Revision16 main finished: "+checks.Count+" checks / "+failures.Count+" failed");
 }
 static IEnumerator Lab(){
  yield return new WaitForSeconds(.8f);var lab=Object.FindFirstObjectByType<RuneCombatLab>();var s=GameSession.Instance;Check("Dedicated laboratory ready",lab!=null&&lab.Ready);Check("All eight runes ready for manual equipment",s.RuneInventory.CollectedRunes.Count==8&&s.RuneInventory.ActiveSlots.All(r=>r==null));Check("Weapon and all movement abilities available",s.HasWeapon&&System.Enum.GetValues(typeof(AbilityId)).Cast<AbilityId>().All(s.HasAbility));s.TrySpendGold(500000);Check("Gold instantly replenishes after purchase cost",s.CurrentGold==999999);Check("Four enemy archetypes spawned on combat floor",Object.FindObjectsByType<EnemyControllerBase>(FindObjectsSortMode.None).Length==4);
  Check("No campaign bosses or collapsing bridge in laboratory",Object.FindObjectsByType<BossBrain>(FindObjectsSortMode.None).Length==0&&Object.FindFirstObjectByType<ScriptedBridgeCollapse>()==null);
  var p=Object.FindFirstObjectByType<PlayerController2D>();var altar=Object.FindFirstObjectByType<AltarCheckpoint>();yield return new WaitForSeconds(2);Check("Spawn altar activates automatically",altar.IsActive);Check("Laboratory spawn stands on real ground",Clear(p.GetComponent<Collider2D>())&&p.IsGrounded);BableVerification.Capture("revision16/lab.png");
  var shop=Object.FindFirstObjectByType<ShopMenuController>();var merchant=Object.FindFirstObjectByType<ShopkeeperController>();var stock=(ShopItemDefinition[])typeof(ShopkeeperController).GetField("stock",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(merchant);Check("Merchant has complete item stock",stock.Length>=6);var item=stock.First(x=>x.EffectType==ShopItemEffectType.MaxHealth);int old=s.MaxHealth;shop.Open(new[]{item});typeof(ShopMenuController).GetMethod("TryBuySelected",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(shop,null);Check("Actual shop purchase applies effect with unlimited gold",s.MaxHealth>old&&s.CurrentGold==999999);BableVerification.Capture("revision16/lab-shop.png");shop.Close();
  BableGameUI.Instance.Runes();yield return new WaitForSecondsRealtime(.4f);Check("Laboratory repository opens",Object.FindFirstObjectByType<RuneRepositoryView>()!=null);BableGameUI.Instance.Resume();Save();Debug.Log("Revision16 lab finished: "+checks.Count+" checks / "+failures.Count+" failed");
 }
}
