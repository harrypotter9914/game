using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using Bable;
using Babel.Runtime.Characters.Player;
using Babel.Runtime.Characters.Enemies;
using Babel.Runtime.Combat;
using Babel.Runtime.Core;
public static class BableRevision34Live {
 public static void Run(){var host=new GameObject("Chase gait verification");Object.DontDestroyOnLoad(host);host.AddComponent<BableTestHost>().StartCoroutine(Check(host));}
 static IEnumerator Check(GameObject host){
  TowerLoading.Load("Gameplay_Main");while(TowerLoading.Busy||BableGameUI.Instance==null||!BableGameUI.Instance.Ready)yield return null;
  yield return new WaitForSecondsRealtime(.6f);GameSession.Instance.UnlockWeapon();
  var p=Object.FindFirstObjectByType<PlayerController2D>();p.GetComponent<HealthComponent>().Invincible=true;var start=p.transform.position;
  foreach(var e in Object.FindObjectsByType<EnemyControllerBase>(FindObjectsSortMode.None))e.enabled=false;
  var report=new List<string>();
  foreach(var name in new[]{"MeleeEnemy","ShieldEnemy","RangedEnemy","GiantEnemy"}){
   var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Characters/Enemies/"+name+".prefab");
   if(prefab==null){report.Add("FAIL missing prefab "+name);continue;}
   p.SetTestInput(0,0);p.GetComponent<Rigidbody2D>().position=start;yield return new WaitForSeconds(.2f);
   var enemy=Object.Instantiate(prefab,start+Vector3.left*(name=="RangedEnemy"?5.5f:4f),Quaternion.identity);Physics2D.SyncTransforms();
   var ec=enemy.GetComponent<Collider2D>();var pc=p.GetComponent<Collider2D>();var rb=enemy.GetComponent<Rigidbody2D>();rb.position+=Vector2.up*(pc.bounds.min.y-ec.bounds.min.y+.02f);
   var brain=enemy.GetComponent<EnemyControllerBase>();var art=enemy.GetComponent<CharacterPresentation>();var seen=new HashSet<string>();int chasing=0;
   p.SetTestInput(1,0);
   for(int i=0;i<24;i++){yield return new WaitForSeconds(.04f);if(brain.BehaviourState=="Pursue"){chasing++;seen.Add(art.animator.GetComponent<SpriteRenderer>().sprite.name);}BableVerification.Capture("revision34/chase-"+name+"-"+i.ToString("D2")+".png");}
   report.Add((chasing>8&&seen.Count>6?"PASS ":"FAIL ")+name+" real pursuit samples="+chasing+" distinct frames="+seen.Count+" visualScale="+art.animator.transform.lossyScale);
   Object.Destroy(enemy);p.SetTestInput(0,0);yield return null;
  }
  File.WriteAllLines("../reference/revision34/live-tests.txt",report);BableGameUI.Instance.MainMenu();Object.Destroy(host);
 }
}
