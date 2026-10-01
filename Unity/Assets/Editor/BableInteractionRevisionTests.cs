using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
using Bable;
using Babel.Runtime.Shop;
using Babel.Runtime.Core;
using Babel.Runtime.Characters.Player;
using Object=UnityEngine.Object;
public static class BableInteractionRevisionTests {
 static readonly List<string> checks=new();
 static void Check(string name,bool ok){checks.Add((ok?"PASS ":"FAIL ")+name);File.WriteAllLines("../reference/revision28/tests.txt",checks);}
 public static void Run(){checks.Clear();var h=new GameObject("Interaction verification");Object.DontDestroyOnLoad(h);h.AddComponent<BableTestHost>().StartCoroutine(Test(h));}
 static IEnumerator Ready(){while(TowerLoading.Busy||BableGameUI.Instance==null||!BableGameUI.Instance.Ready)yield return null;yield return new WaitForSecondsRealtime(.3f);}
 static IEnumerator Test(GameObject h){yield return Ready();TowerLoading.Load("Gameplay_Main");yield return Ready();var p=Object.FindFirstObjectByType<PlayerController2D>();var merchant=Object.FindObjectsByType<ShopkeeperController>(FindObjectsSortMode.None).OrderBy(x=>Vector3.Distance(x.transform.position,p.transform.position)).First();var body=p.GetComponent<Rigidbody2D>();p.SetTestInput(0,0);var at=p.transform.position;at.x=merchant.GetComponent<Collider2D>().bounds.center.x;body.position=at;p.transform.position=at;Physics2D.SyncTransforms();yield return new WaitForSeconds(.25f);TowerDialogue.AbortStory();var shop=Object.FindFirstObjectByType<ShopMenuController>();Check("Physical approach enables merchant",merchant.TryTrade());shop.HandleInput(false,false,false,true);Check("Opening E cannot also close shop",shop.IsOpen);yield return null;Check("Shop remains open on following frame",shop.IsOpen);
  GameSession.Instance.AddGold(100);var hp=p.GetComponent<Babel.Runtime.Combat.HealthComponent>();hp.ApplyDamage(1);int gold=GameSession.Instance.CurrentGold;shop.HandleInput(false,false,true,false);Check("Purchase spends gold",GameSession.Instance.CurrentGold<gold);yield return null;shop.HandleInput(false,false,false,true);Check("Second E closes shop",!shop.IsOpen);Check("Closing E cannot immediately reopen",!merchant.TryTrade());yield return null;Check("Subsequent E can reopen",merchant.TryTrade());yield return null;shop.Close();yield return null;
  var n=NarrativeGuidance.Instance;Check("Trade lesson recorded once",n.HasSeen("tutorial_trade"));int queued=n.PendingCount;Check("Repeated trade lesson rejected",!n.Teach("trade","Trade","Again","E")&&queued==n.PendingCount);n.Teach("map","Remember the road","Only explored paths are mapped.","M  Map");queued=n.PendingCount;Check("Road lesson cannot requeue",!n.Teach("map","Road","Again","M")&&queued==n.PendingCount);
  yield return new WaitForSecondsRealtime(.6f);Check("Guidance can be dismissed",n.CanDismiss&&n.DismissCurrent());Check("Dismiss clears visible guidance immediately",n.CurrentId==null&&n.PanelAlpha==0);yield return new WaitForSecondsRealtime(.6f);Check("Later guidance remains available",n.CurrentId!=null);
  yield return new WaitForSecondsRealtime(1.3f);var v=Camera.main.WorldToViewportPoint(p.transform.position);Check("Player slightly below screen center",v.y>.39f&&v.y<.45f);var panel=GameObject.Find("Transparent bronze frame").GetComponent<RectTransform>();Check("Guidance moved above player",panel.anchoredPosition.y>155);BableVerification.Capture("revision28/merchant-and-guide.png");BableGameUI.Instance.Runes();Check("Runes open",BableGameUI.Instance.Mode=="runes");BableGameUI.Instance.Resume();BableGameUI.Instance.Map();Check("Atlas opens",BableGameUI.Instance.Mode=="map");BableGameUI.Instance.Resume();BableGameUI.Instance.Pause();Check("Pause stops gameplay",GameSession.Instance.IsPaused);BableGameUI.Instance.Resume();Check("Resume restores gameplay",!GameSession.Instance.IsPaused);
  BableGameUI.Instance.MainMenu();yield return Ready();File.WriteAllText("../reference/revision28/complete.txt",checks.Count+" checks; "+checks.FindAll(x=>x.StartsWith("FAIL")).Count+" failures");Object.Destroy(h);
 }
}
