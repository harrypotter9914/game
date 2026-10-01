using System.Collections;using System.IO;using System.Linq;using System.Reflection;using UnityEngine;using Bable;using Babel.Runtime.Core;using Babel.Runtime.Combat;using Babel.Runtime.Characters.Player;using Babel.Runtime.Characters.Enemies;
public static class BableRevision48Visual {
 public static void Run(){new GameObject("Feedback visual review").AddComponent<BableTestHost>().StartCoroutine(Test());}
 static IEnumerator Test(){
  while(TowerLoading.Busy||BableGameUI.Instance==null||!BableGameUI.Instance.Ready)yield return null;
  TowerDialogue.AbortStory();BableGameUI.Instance.Resume();GameSession.Instance.ResetForNewGame();GameSession.Instance.UnlockWeapon();
  foreach(var e in Object.FindObjectsByType<EnemyControllerBase>(FindObjectsSortMode.None)){e.enabled=false;e.CancelInvoke();e.GetComponent<Rigidbody2D>().constraints=RigidbodyConstraints2D.FreezeAll;var be=e.GetComponent<BossEncounter>();if(be!=null)be.enabled=false;}
  foreach(var s in Object.FindObjectsByType<BossSpeech>(FindObjectsSortMode.None))s.enabled=false;
  var p=Object.FindFirstObjectByType<PlayerController2D>();var body=p.GetComponent<Rigidbody2D>();p.SetTestInput(0,0);body.position=new Vector2(60,-60.18f);p.transform.position=body.position;
  Camera.main.GetComponent<Babel.Runtime.World.CameraFollow2D>().SettleAtTarget();yield return new WaitForSeconds(.4f);
  ScreenCapture.CaptureScreenshot("../reference/revision48/before-hit.png");yield return new WaitForSeconds(.2f);
  var hp=p.GetComponent<HealthComponent>();hp.Invincible=false;typeof(HealthComponent).GetField("invulnerableUntil",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(hp,0f);
  hp.ReceiveDamage(new DamageInfo(1,p.transform.position+Vector3.right,Vector2.left,p.gameObject,TeamAlignment.Enemy));
  ScreenCapture.CaptureScreenshot("../reference/revision48/player-hit.png");yield return new WaitForSeconds(.4f);
  ScreenCapture.CaptureScreenshot("../reference/revision48/after-hit.png");
  var enemy=Object.FindFirstObjectByType<MeleeEnemyController>();enemy.transform.position=p.transform.position+Vector3.right*2;enemy.GetComponent<Rigidbody2D>().position=enemy.transform.position;var eh=enemy.GetComponent<HealthComponent>();eh.Invincible=false;eh.Configure(30,30);
  yield return new WaitForSeconds(.3f);enemy.TryReceiveDamage(new DamageInfo(1,p.transform.position,Vector2.right,p.gameObject,TeamAlignment.Player));
  yield return new WaitForEndOfFrame();BableRevision47Visual.Map("../revision48/enemy-hit",p.transform.position+Vector3.right,3,1200,700);
  var flash=Object.FindFirstObjectByType<ActorHitFlash>();File.WriteAllText("../reference/revision48/visual-check.txt","Local silhouette present: "+(flash!=null)+"; shader supported: "+Resources.Load<Shader>("Bable/NewArt/HitFlash48").isSupported+"; enemy damage screen alpha: "+p.GetComponent<PlayerHitFeedback>().FlashAlpha);
 }
}
