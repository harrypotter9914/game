using System.Collections;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Bable;
using Babel.Runtime.Core;
using Babel.Runtime.Characters.Player;
using Babel.Runtime.Shop;
using Object=UnityEngine.Object;
public static class BableRevision29Tests {
 static List<string> checks=new();
 static void Check(string name,bool ok){checks.Add((ok?"PASS ":"FAIL ")+name);File.WriteAllLines("../reference/revision29/tests.txt",checks);}
 public static void Run(){checks.Clear();var go=new GameObject("Revision29 verification");Object.DontDestroyOnLoad(go);go.AddComponent<BableTestHost>().StartCoroutine(Test(go));}
 static IEnumerator Ready(){while(TowerLoading.Busy||BableGameUI.Instance==null||!BableGameUI.Instance.Ready)yield return null;yield return new WaitForSecondsRealtime(.4f);}
 static IEnumerator Test(GameObject host){
  yield return Ready();BableDisplayTests.Size(1600,900);yield return null;
  var canvas=new GameObject("Prologue test",typeof(Canvas));canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;canvas.GetComponent<Canvas>().sortingOrder=200;
  var prologue=canvas.AddComponent<TowerPrologue>();prologue.Build(()=>{});yield return new WaitForSecondsRealtime(1.2f);
  var skip=canvas.transform.Find("ESC   SKIP OPENING").GetComponent<RectTransform>();var progress=canvas.transform.Find("Progress").GetComponent<RectTransform>();
  Check("Opening skip has bottom safe margin",skip.anchoredPosition.y-skip.rect.height/2>=-422);
  Check("Opening progress has bottom safe margin",progress.anchoredPosition.y-progress.rect.height/2>=-422);
  BableVerification.Capture("revision29/opening.png");Object.Destroy(canvas);yield return null;
  TowerLoading.Load("Gameplay_Main");yield return Ready();var p=Object.FindFirstObjectByType<PlayerController2D>();var session=GameSession.Instance;
  var shop=Object.FindFirstObjectByType<ShopMenuController>();var merchant=Object.FindObjectsByType<ShopkeeperController>(FindObjectsSortMode.None).First();
  var stock=(ShopItemDefinition[])typeof(ShopkeeperController).GetField("stock",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(merchant);shop.Open(stock);yield return new WaitForSecondsRealtime(.3f);
  var leave=GameObject.Find("Leave").GetComponent<RectTransform>();Check("Shop leave moved clear of lower ornament",-leave.anchoredPosition.y+leave.rect.height<=489);
  BableVerification.Capture("revision29/shop.png");shop.Close();
  var corner=Object.FindObjectsByType<ShopkeeperController>(FindObjectsSortMode.None).First(m=>m.name=="Wayfarer merchant 5");Check("Merchant clear of corner",Mathf.Abs(corner.transform.position.x-270.5f)<.01f);
  session.UnlockWeapon();var art=p.GetComponent<CharacterPresentation>();p.SetTestInput(0,0);yield return new WaitForSeconds(1);
  foreach(var action in new[]{"idle","run","jump","attack","heal","bridgeimpact"}){
   art.Act(action,2);yield return new WaitForSeconds(.18f);BableVerification.Capture("revision29/player-"+action+".png");
  }
  art.ReleaseAction();
  var clips=AssetDatabase.FindAssets("t:AnimationClip",new[]{"Assets/Art/NativeAnimations"}).Select(g=>AssetDatabase.LoadAssetAtPath<AnimationClip>(AssetDatabase.GUIDToAssetPath(g))).Where(c=>c.name.StartsWith("Pilgrim_")).ToArray();
  foreach(var clip in clips){var bindings=AnimationUtility.GetObjectReferenceCurveBindings(clip);var keys=bindings.SelectMany(b=>AnimationUtility.GetObjectReferenceCurve(clip,b)).ToArray();Check(clip.name+" all sprite frames resolve",keys.Length>0&&keys.All(k=>k.value is Sprite));}
  BableGameUI.Instance.MainMenu();yield return Ready();File.WriteAllText("../reference/revision29/complete.txt",checks.Count+" checks; "+checks.Count(s=>s.StartsWith("FAIL"))+" failures");Object.Destroy(host);
 }
}
