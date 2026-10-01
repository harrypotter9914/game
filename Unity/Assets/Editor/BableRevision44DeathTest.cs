using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Bable;
using Babel.Runtime.Combat;
using Babel.Runtime.Characters.Enemies;
public static class BableRevision44DeathTest {
 public static void Run(){new GameObject("Death HUD verification").AddComponent<BableTestHost>().StartCoroutine(Test());}
 static IEnumerator Test(){while(TowerLoading.Busy||BableGameUI.Instance==null||!BableGameUI.Instance.Ready)yield return null;TowerDialogue.AbortStory();BableGameUI.Instance.Resume();foreach(var e in Object.FindObjectsByType<EnemyControllerBase>(FindObjectsSortMode.None))e.enabled=false;foreach(var s in Object.FindObjectsByType<BossSpeech>(FindObjectsSortMode.None))s.enabled=false;foreach(var e in Object.FindObjectsByType<BossEncounter>(FindObjectsSortMode.None))e.enabled=false;var b=Object.FindObjectsByType<BossBrain>(FindObjectsSortMode.None).First(b=>b.profile.kind==BossKind.Shockwave);var e1=b.GetComponent<BossEncounter>();typeof(BossEncounter).GetField("<Active>k__BackingField",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,e1);e1.Health.Invincible=false;yield return new WaitForSeconds(.4f);var hud=Object.FindFirstObjectByType<BossHealthHud>();bool full=hud.Visible&&hud.HealthFraction==1;e1.Health.ApplyDamage(999);yield return new WaitForSecondsRealtime(1);bool zero=hud.HealthFraction==0;BableGameUI.Instance.Resume();yield return new WaitForSecondsRealtime(.4f);bool hidden=!hud.Visible;File.WriteAllText("../reference/revision44/death-hud.txt",$"{(full&&zero&&hidden?"PASS":"FAIL")}: initially full={full}, zero after death/reward={zero}, hidden after returning={hidden}");}
}
