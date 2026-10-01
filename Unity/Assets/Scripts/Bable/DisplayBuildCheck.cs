using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Babel.Runtime.Shop;
namespace Bable {
 public sealed class DisplayBuildCheck:MonoBehaviour {
  string path;readonly List<string> checks=new();
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
  static void Install(){var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-bableDisplayReport");if(i>=0&&i+1<args.Length){var g=new GameObject("Explicit display build check");DontDestroyOnLoad(g);g.AddComponent<DisplayBuildCheck>().path=args[i+1];}}
  void Check(string name,bool ok){checks.Add((ok?"PASS ":"FAIL ")+name);File.WriteAllLines(path,checks);}
  IEnumerator Start(){while(TowerLoading.Busy||BableGameUI.Instance==null||!BableGameUI.Instance.Ready)yield return null;TowerLoading.Load("Gameplay_Main");while(TowerLoading.Busy)yield return null;
   int[,] sizes={{1280,720},{1024,768},{1600,900}};
   for(int i=0;i<3;i++){int w=sizes[i,0],h=sizes[i,1];Screen.SetResolution(w,h,FullScreenMode.Windowed);yield return new WaitForSecondsRealtime(1);var v=DisplayFrame.Viewport;Check(w+"x"+h+" window resized",Screen.width==w&&Screen.height==h);Check(w+"x"+h+" safe aspect",Mathf.Abs(Screen.width*v.width/(Screen.height*v.height)-16f/9)<.01f);}
   Screen.SetResolution(Display.main.systemWidth,Display.main.systemHeight,FullScreenMode.FullScreenWindow);yield return new WaitForSecondsRealtime(1);Check("Fullscreen enabled",Screen.fullScreen);var rect=DisplayFrame.Viewport;Check("Fullscreen safe aspect",Mathf.Abs(Screen.width*rect.width/(Screen.height*rect.height)-16f/9)<.01f);
   Screen.SetResolution(1280,720,FullScreenMode.Windowed);yield return new WaitForSecondsRealtime(1);Check("Return to window",!Screen.fullScreen&&Screen.width==1280&&Screen.height==720);
   var shop=FindFirstObjectByType<ShopMenuController>();var merchant=FindFirstObjectByType<ShopkeeperController>();var stock=(ShopItemDefinition[])typeof(ShopkeeperController).GetField("stock",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(merchant);shop.Open(stock);yield return new WaitForSecondsRealtime(.6f);ScreenCapture.CaptureScreenshot(Path.Combine(Path.GetDirectoryName(path),"build-shop.png"));yield return new WaitForSecondsRealtime(.5f);Check("Shop opened with wares",shop.IsOpen&&stock.Length>0);shop.Close();
   Check("No loading curtain remains",!TowerLoading.Busy);File.AppendAllText(path,"\n"+checks.Count+" checks; "+checks.FindAll(x=>x.StartsWith("FAIL")).Count+" failures\n");Application.Quit(checks.Exists(x=>x.StartsWith("FAIL"))?1:0);
  }
 }
}
