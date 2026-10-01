using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor;
using Bable;
using Babel.Runtime.Characters.Player;
using Babel.Runtime.Characters.Enemies;
using Babel.Runtime.Combat;
using Babel.Runtime.Core;
using Object=UnityEngine.Object;
public static class BableRevision30Tests {
 static List<string> checks=new();
 static void Check(string label,bool ok){checks.Add((ok?"PASS ":"FAIL ")+label);File.WriteAllLines("../reference/revision30/tests.txt",checks);}
 public static void Run(){checks.Clear();var go=new GameObject("Revision30 checks");Object.DontDestroyOnLoad(go);go.AddComponent<BableTestHost>().StartCoroutine(Test(go));}
 static IEnumerator Ready(){while(TowerLoading.Busy||BableGameUI.Instance==null||!BableGameUI.Instance.Ready)yield return null;yield return new WaitForSecondsRealtime(.35f);}
 static IEnumerator Menu(string name){
  yield return null;var buttons=Object.FindObjectsByType<ManuscriptMenuButton>(FindObjectsSortMode.None);Check(name+" unified text buttons",buttons.Length>0&&buttons.All(b=>b.GetComponentInChildren<Text>().font==MenuTypography.Font));Check(name+" no old image lettering",GameObject.Find("Original lettering")==null);
  var backing=GameObject.Find("Ink vellum").GetComponent<RectTransform>();var outer=new Vector3[4];backing.GetWorldCorners(outer);bool safe=true;
  foreach(var b in buttons){b.OnPointerEnter(null);yield return new WaitForSecondsRealtime(.32f);var c=new Vector3[4];b.lettering.GetWorldCorners(c);safe&=c[0].x>outer[0].x+20&&c[0].y>outer[0].y+20&&c[2].x<outer[2].x-20&&c[2].y<outer[2].y-20;b.OnPointerExit(null);}
  Check(name+" hovered buttons stay inside border",safe);buttons.Last().OnPointerEnter(null);yield return new WaitForSecondsRealtime(.35f);BableVerification.Capture("revision30/"+name+".png");
 }
 static IEnumerator Test(GameObject host){
  yield return Ready();BableDisplayTests.Size(1600,900);yield return Menu("main");TowerLoading.Load("Gameplay_Main");yield return Ready();
  var p=Object.FindFirstObjectByType<PlayerController2D>();var session=GameSession.Instance;session.UnlockWeapon();p.SetTestInput(0,0);p.GetComponent<HealthComponent>().Invincible=true;
  var art=p.GetComponent<CharacterPresentation>();
  foreach(var action in new[]{"idle","run","jump","attack","bridgefall","bridgeimpact","bridgerise"}){art.Act(action,1);yield return new WaitForSeconds(.3f);BableVerification.Capture("revision30/hero-"+action+".png");}art.ReleaseAction();
  var combat=p.GetComponent<PlayerCombatController>();foreach(float direction in new[]{0f,1f,-1f}){p.SetTestInput(0,direction);if(direction<0){p.GetComponent<Rigidbody2D>().position+=Vector2.up*2;yield return new WaitForFixedUpdate();}Check("Attack accepted "+direction,combat.PerformMeleeAttack());yield return new WaitForSeconds(.23f);Check("No extra player SwordArc "+direction,!Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None).Any(s=>s.gameObject.name.Contains("SwordArc")));yield return new WaitForSeconds(.8f);}p.SetTestInput(0,0);
  BableGameUI.Instance.Pause();yield return Menu("pause");BableGameUI.Instance.Resume();BableGameUI.Instance.Death();yield return Menu("death");BableGameUI.Instance.Resume();
  var zones=Object.FindObjectsByType<SummoningEncounter>(FindObjectsSortMode.None);foreach(var z in zones)foreach(var prefab in z.wavePrefabs??new[]{z.enemyPrefab})Check(z.name+" has clearance for "+prefab.name,z.IsSafeSpawn(prefab));
  var all=Object.FindObjectsByType<EnemyControllerBase>(FindObjectsSortMode.None).Where(e=>!(e is BossBrain)).ToArray();foreach(var type in new[]{typeof(MeleeEnemyController),typeof(ShieldSentinelController),typeof(RangedEnemyController),typeof(GiantEnemyController)})Check(type.Name+" exists",all.Any(e=>e.GetType()==type));
  foreach(var e in all){var a=e.GetComponent<CharacterPresentation>()?.animator;Check(e.name+" separate walk/run",a!=null&&a.HasState(0,Animator.StringToHash("rightwalk"))&&a.HasState(0,Animator.StringToHash("rightrun")));}
  var target=zones.First(z=>z.name.StartsWith("Ambush 11 "));foreach(var z in zones)if(z!=target)z.enabled=false;
  p.GetComponent<Rigidbody2D>().position=target.feet+new Vector2(-2,1);Physics2D.SyncTransforms();yield return new WaitForSeconds(1.2f);float end=Time.realtimeSinceStartup+30;int observed=0;
  while(!target.Completed&&Time.realtimeSinceStartup<end){if(!target.IsSummoning&&target.SpawnedCount>observed&&target.ActiveEnemy!=null){observed=target.SpawnedCount;Check("Mixed wave "+observed+" correct type",target.ActiveEnemy.GetType()==target.wavePrefabs[observed-1].GetComponent<EnemyControllerBase>().GetType());BableVerification.Capture("revision30/wave-"+observed+".png");target.ActiveEnemy.GetComponent<HealthComponent>().ApplyDamage(999);}yield return null;}
  Check("Mixed encounter completes in sequence",target.Completed&&observed==target.wavePrefabs.Length);
  BableGameUI.Instance.MainMenu();yield return Ready();File.WriteAllText("../reference/revision30/complete.txt",checks.Count+" checks; "+checks.Count(s=>s.StartsWith("FAIL"))+" failures");Object.Destroy(host);
 }
}
