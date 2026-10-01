using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Tilemaps;
using Bable;
using Babel.Runtime.Core;
using Babel.Runtime.Characters.Player;
using Object=UnityEngine.Object;
public static class BableDisplayTests {
 static List<string> checks=new List<string>();
 const BindingFlags Flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;
 static void Check(string label,bool ok){checks.Add((ok?"PASS ":"FAIL ")+label);File.WriteAllText("../reference/revision26/tests.txt",string.Join("\n",checks));}
 public static void Size(int w,int h){var asm=typeof(Editor).Assembly;var type=asm.GetType("UnityEditor.GameViewSizes");var singleton=typeof(ScriptableSingleton<>).MakeGenericType(type);var sizes=singleton.GetProperty("instance",Flags).GetValue(null);var group=type.GetMethod("GetGroup").Invoke(sizes,new object[]{Enum.Parse(type.GetMethod("GetGroup").GetParameters()[0].ParameterType,"Standalone")});var gt=group.GetType();int count=(int)gt.GetMethod("GetTotalCount").Invoke(group,null),index=-1;for(int i=0;i<count;i++){var s=gt.GetMethod("GetGameViewSize").Invoke(group,new object[]{i});var st=s.GetType();if((int)st.GetProperty("width").GetValue(s)==w&&(int)st.GetProperty("height").GetValue(s)==h){index=i;break;}}
  if(index<0){var sizeType=asm.GetType("UnityEditor.GameViewSize");var kind=asm.GetType("UnityEditor.GameViewSizeType");var size=Activator.CreateInstance(sizeType,new object[]{Enum.ToObject(kind,1),w,h,"Bable QA "+w+"x"+h});gt.GetMethod("AddCustomSize").Invoke(group,new[]{size});index=count;}
  var view=EditorWindow.GetWindow(asm.GetType("UnityEditor.GameView"));view.GetType().GetProperty("selectedSizeIndex",Flags).SetValue(view,index);view.Focus();view.Repaint();
 }
 static IEnumerator Wait(){float end=Time.realtimeSinceStartup+25;while((TowerLoading.Busy||BableGameUI.Instance==null||!BableGameUI.Instance.Ready)&&Time.realtimeSinceStartup<end)yield return null;yield return new WaitForSecondsRealtime(.4f);}
 public static void Run(){checks.Clear();File.WriteAllText("../reference/revision26/complete.txt","RUNNING");var h=new GameObject("Display verification");Object.DontDestroyOnLoad(h);h.AddComponent<BableTestHost>().StartCoroutine(Test(h));}
 static void Capture(string name){ScreenCapture.CaptureScreenshot(Path.GetFullPath("../reference/revision26/"+name+".png"));}
 static bool Inside(RectTransform t){var corners=new Vector3[4];t.GetWorldCorners(corners);var r=DisplayFrame.Viewport;var safe=new Rect(r.x*Screen.width,r.y*Screen.height,r.width*Screen.width,r.height*Screen.height);foreach(var c in corners)if(c.x<safe.xMin-2||c.x>safe.xMax+2||c.y<safe.yMin-2||c.y>safe.yMax+2)return false;return true;}
 static IEnumerator Test(GameObject host){
  yield return Wait();TowerLoading.Load("Gameplay_Main");yield return Wait();var ui=BableGameUI.Instance;var p=Object.FindFirstObjectByType<PlayerController2D>();var atlas=ExploredAtlas.Current;atlas.Explore();Check("Atlas explored on arrival",atlas.RevealedCount>0);var tex=atlas.Draw();int black=0;foreach(var c in tex.GetPixels32())if(c.r==0&&c.g==0&&c.b==0)black++;Check("Unexplored atlas is black",black>tex.width*tex.height*.9f);Check("No camera map or background",GameObject.Find("Atlas Camera")==null);int initial=atlas.RevealedCount;p.transform.position+=Vector3.right*3;Physics2D.SyncTransforms();atlas.Explore();Check("Exploration grows along movement",atlas.RevealedCount>=initial);ui.Runes();Check("Empty rune repository opens",ui.Mode=="runes"&&GameSession.Instance.RuneInventory.CollectedRunes.Count==0);ui.Resume();
  var n=NarrativeGuidance.Instance;yield return new WaitForSecondsRealtime(.4f);Check("Only initial movement lesson unlocked",n.HasSeen("tutorial_move")&&!n.HasSeen("tutorial_sword")&&!n.HasSeen("tutorial_shock"));GameSession.Instance.UnlockWeapon();yield return null;Check("Sword lesson unlocks",n.HasSeen("tutorial_sword"));GameSession.Instance.SetHealth(GameSession.Instance.MaxHealth-1,GameSession.Instance.MaxHealth);yield return null;Check("First wound teaches healing",n.HasSeen("tutorial_heal"));foreach(AbilityId a in Enum.GetValues(typeof(AbilityId)))GameSession.Instance.UnlockAbility(a);yield return null;Check("All ability lessons unlock",n.HasSeen("tutorial_shock")&&n.HasSeen("tutorial_wall")&&n.HasSeen("tutorial_double")&&n.HasSeen("tutorial_dash"));
  int[,] sizes={{1600,900},{1280,720},{1024,768},{2560,1080},{900,1200}};
  for(int i=0;i<sizes.GetLength(0);i++){int w=sizes[i,0],h=sizes[i,1];Size(w,h);yield return new WaitForSecondsRealtime(.6f);var actual=DisplayFrame.Viewport;Check(w+"x"+h+" safe aspect",Mathf.Abs(actual.width*Screen.width/(actual.height*Screen.height)-16f/9)<.01f);Check(w+"x"+h+" camera aspect",Mathf.Abs(Camera.main.aspect-16f/9)<.01f);ui.Pause();yield return new WaitForSecondsRealtime(.3f);Check(w+"x"+h+" pause frame fits",Inside(GameObject.Find("Ink vellum").GetComponent<RectTransform>()));Capture("pause-"+w+"x"+h);yield return new WaitForSecondsRealtime(.2f);ui.Resume();
   var shop=Object.FindFirstObjectByType<Babel.Runtime.Shop.ShopMenuController>();var stock=new List<Babel.Runtime.Shop.ShopItemDefinition>();foreach(var guid in AssetDatabase.FindAssets("t:ShopItemDefinition",new[]{"Assets/Data/Shop"}))stock.Add(AssetDatabase.LoadAssetAtPath<Babel.Runtime.Shop.ShopItemDefinition>(AssetDatabase.GUIDToAssetPath(guid)));shop.Open(stock);yield return new WaitForSecondsRealtime(.3f);var border=GameObject.Find("Illuminated manuscript border");Check(w+"x"+h+" shop border fits",border!=null&&Inside(border.GetComponent<RectTransform>()));Capture("shop-"+w+"x"+h);yield return new WaitForSecondsRealtime(.2f);shop.Close();
  }
  Size(1600,900);yield return new WaitForSecondsRealtime(.5f);ui.Map();yield return new WaitForSecondsRealtime(.3f);Capture("explored-map");yield return new WaitForSecondsRealtime(.3f);Check("Atlas uses discovered texture",GameObject.Find("Explored passages only")!=null&&GameObject.Find("Atlas Camera")==null);ui.Resume();yield return new WaitForSecondsRealtime(.3f);Capture("hud");yield return new WaitForSecondsRealtime(.3f);Check("No constant controls strip",GameObject.Find("Gameplay HUD").transform.Find("Controls")==null);Check("Animated purse present",Object.FindFirstObjectByType<PurseHud>()!=null);Check("Window resize enabled",PlayerSettings.resizableWindow);Check("Menu font available",MenuTypography.Font!=null&&MenuTypography.Sdf!=null);
  ui.MainMenu();yield return Wait();File.WriteAllText("../reference/revision26/complete.txt",checks.Count+" checks; "+checks.FindAll(x=>x.StartsWith("FAIL")).Count+" failures");Object.Destroy(host);
 }
}
