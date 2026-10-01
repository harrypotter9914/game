using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Bable;
public static class BableRevision31Review {
 public static void Run(){var h=new GameObject("Menu review");Object.DontDestroyOnLoad(h);h.AddComponent<BableTestHost>().StartCoroutine(Review(h));}
 static IEnumerator Ready(){while(TowerLoading.Busy||BableGameUI.Instance==null||!BableGameUI.Instance.Ready)yield return null;yield return new WaitForSecondsRealtime(.4f);}
 static IEnumerator Capture(string name){
 var buttons=Object.FindObjectsByType<ManuscriptMenuButton>(FindObjectsSortMode.None).OrderByDescending(b=>b.transform.position.y).ToArray();
 foreach(var b in buttons){b.OnPointerExit(null);b.OnDeselect(null);}
 buttons[0].OnPointerEnter(null);yield return new WaitForSecondsRealtime(.4f);
 BableVerification.Capture("revision31/"+name+".png");
 File.AppendAllText("../reference/revision31/results.txt",name+": transparent button targets="+buttons.All(b=>b.GetComponent<Image>().color.a==0)+", no hover wash="+(GameObject.Find("Deep hover ink")==null)+"\n");
 }
 static IEnumerator Review(GameObject h){File.WriteAllText("../reference/revision31/results.txt","");yield return Ready();yield return Capture("main");TowerLoading.Load("Gameplay_Main");yield return Ready();BableGameUI.Instance.Pause();yield return Capture("pause");BableGameUI.Instance.Death();yield return Capture("death");BableGameUI.Instance.MainMenu();yield return Ready();File.AppendAllText("../reference/revision31/results.txt","Complete\n");Object.Destroy(h);}
}
