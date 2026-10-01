using System.Collections;
using System.IO;
using UnityEngine;
using Bable;
using Babel.Runtime.Characters.Player;
using Babel.Runtime.Characters.Enemies;
using Babel.Runtime.Combat;
public static class BableBridgeReel {
 public static void Run(){new GameObject("Bridge recording").AddComponent<BableTestHost>().StartCoroutine(Record());}
 static IEnumerator Record(){BableGameUI.Instance.Begin();yield return new WaitForSeconds(.5f);foreach(var s in Object.FindObjectsByType<BossSpeech>(FindObjectsSortMode.None))s.enabled=false;foreach(var e in Object.FindObjectsByType<EnemyControllerBase>(FindObjectsSortMode.None))e.enabled=false;TowerDialogue.AbortStory();var p=Object.FindFirstObjectByType<PlayerController2D>();p.GetComponent<HealthComponent>().Invincible=true;p.SetTestInput(0,0);Babel.Runtime.Core.GameSession.Instance.UnlockWeapon();p.transform.position=new Vector3(195,14,0);p.GetComponent<Rigidbody2D>().linearVelocity=Vector2.zero;Physics2D.SyncTransforms();var bridge=Object.FindFirstObjectByType<ScriptedBridgeCollapse>();bridge.Trigger(p);Directory.CreateDirectory("../reference/revision12/reel");int i=0;float until=Time.time+9;while(bridge.IsRunning&&Time.time<until){yield return new WaitForSeconds(.08f);BableVerification.Capture("revision12/reel/"+(i++).ToString("D3")+".png");if(bridge.Phase=="Impact"&&i%2==0)BableVerification.Capture("revision12/bridge-heavy-landing.png");}yield return new WaitForSeconds(.2f);BableVerification.Capture("revision12/reel/"+(i++).ToString("D3")+".png");File.WriteAllText("../reference/revision12/reel-finished.txt",i+" frames");}
}
