using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Bable;
using Babel.Runtime.Characters.Player;
using Babel.Runtime.Core;
using Object=UnityEngine.Object;
public static class BableLoadingTests {
 static readonly List<string> checks=new List<string>();
 static void Check(string name,bool ok){checks.Add((ok?"PASS ":"FAIL ")+name);File.WriteAllText("../reference/revision25/tests.txt",string.Join("\n",checks));}
 public static void Run(){checks.Clear();File.WriteAllText("../reference/revision25/complete.txt","RUNNING");var h=new GameObject("Loading tests");Object.DontDestroyOnLoad(h);h.AddComponent<BableTestHost>().StartCoroutine(Test(h));}
 static IEnumerator Wait(){float end=Time.realtimeSinceStartup+25;while(TowerLoading.Busy&&Time.realtimeSinceStartup<end)yield return null;Check("Transition completes",!TowerLoading.Busy);}
 static IEnumerator Test(GameObject host){
  yield return Wait();
  string[] scenes={"Gameplay_Main","Boss_Test_1","Boss_Test_2","Boss_Test_3","Boss_Test_4","Boss_Test_5","Rune_Combat_Lab"};
  foreach(var scene in scenes){
   TowerLoading.Load(scene);var curtain=Object.FindFirstObjectByType<TowerLoading>();Check(scene+" opaque immediately",curtain!=null&&curtain.GetComponent<CanvasGroup>().alpha==1&&curtain.GetComponent<Canvas>().sortingOrder==30000);
   if(scene=="Gameplay_Main"){yield return new WaitForSecondsRealtime(.15f);BableVerification.Capture("revision25/loading.png");}
   yield return Wait();var p=Object.FindFirstObjectByType<PlayerController2D>();Check(scene+" grounded before control",p!=null&&p.IsGrounded&&Mathf.Abs(p.Velocity.y)<.2f);Check(scene+" resumes time",Time.timeScale==1&&BableGameUI.Instance.Mode=="play");
  }
  BableGameUI.Instance.Respawn();Check("Respawn starts covered",TowerLoading.Busy);yield return Wait();var rebirth=Object.FindFirstObjectByType<AltarRebirth>();Check("Rebirth begins after reveal",rebirth!=null&&rebirth.IsRunning);yield return new WaitForSecondsRealtime(1.2f);Check("Rebirth restores physics",Object.FindFirstObjectByType<PlayerController2D>().GetComponent<Rigidbody2D>().simulated);
  TowerLoading.Load("Gameplay_Main");yield return Wait();var before=Object.FindFirstObjectByType<PlayerController2D>().transform.position;TowerLoading.Load("Gameplay_Main");yield return Wait();Check("Restart has stable spawn",Vector3.Distance(before,Object.FindFirstObjectByType<PlayerController2D>().transform.position)<.1f);
  BableGameUI.Instance.MainMenu();yield return Wait();Check("Return to title has no HUD",BableGameUI.Instance.IsTitleScene&&!BableGameUI.Instance.GameplayHudVisible&&GameSession.Instance==null);
  File.WriteAllText("../reference/revision25/complete.txt",checks.Count+" checks; "+checks.FindAll(x=>x.StartsWith("FAIL")).Count+" failures");Object.Destroy(host);
 }
}
