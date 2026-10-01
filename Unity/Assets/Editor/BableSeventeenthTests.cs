using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor;
using Bable;
using Babel.Runtime.Core;
using Babel.Runtime.Combat;
using Babel.Runtime.Characters.Player;
public static class BableSeventeenthTests {
 static List<string> checks=new(),failures=new();
 static void Check(string text,bool pass){checks.Add(text);if(!pass)failures.Add(text);File.WriteAllText("../reference/revision17/tests.json",JsonUtility.ToJson(new BableVerification.Report{checks=checks.ToArray(),failures=failures.ToArray()},true));}
 public static void Run(){checks.Clear();failures.Clear();new GameObject("Menu layer regression").AddComponent<BableTestHost>().StartCoroutine(Test());}
 static IEnumerator Test(){
  var ui=BableGameUI.Instance;var session=GameSession.Instance;
  Check("Fresh campaign opens on main menu",ui.Mode=="main"&&session.IsPaused);
  bool hidden=true;for(int i=0;i<25;i++){yield return new WaitForEndOfFrame();hidden&=!ui.GameplayHudVisible;}
  Check("Startup frames keep the whole gameplay layer hidden",hidden);Check("Main menu has its own order 400",ui.ActiveMenuOrder==400);BableVerification.Capture("revision17/start-menu.png");
  session.AddPermanentMaxHealth(1);session.AddPermanentMaxMana(1);yield return null;Check("Resource changes cannot expose HUD over start menu",!ui.GameplayHudVisible);
  var results=new List<RaycastResult>();var data=new PointerEventData(EventSystem.current){position=new Vector2(Screen.width*.5f,Screen.height*.5f)};EventSystem.current.RaycastAll(data,results);Check("Main menu owns the top pointer raycast",results.Count>0&&results[0].gameObject.transform.IsChildOf(GameObject.Find("main").transform));
  ui.Begin();Check("Begin enables gameplay HUD synchronously",ui.GameplayHudVisible);yield return new WaitForSeconds(.2f);Check("Vitals build inside gameplay canvas",GameObject.Find("Votive vitals")!=null&&GameObject.Find("Votive vitals").transform.parent.name=="Gameplay HUD");BableVerification.Capture("revision17/gameplay.png");
  ui.Pause();Check("Pause hides HUD before the next frame",!ui.GameplayHudVisible);Check("Pause has independent order 300",ui.ActiveMenuOrder==300);yield return null;BableVerification.Capture("revision17/pause-menu.png");
  session.AddPermanentMaxHealth(1);yield return null;Check("Paused stat changes do not recreate a visible blood bar",!ui.GameplayHudVisible);ui.Resume();yield return null;Check("Resume rebuilds resource display within gameplay layer",ui.GameplayHudVisible&&GameObject.Find("Votive vitals")!=null);
  ui.Runes();Check("Rune overlay suppresses HUD",!ui.GameplayHudVisible&&ui.ActiveMenuOrder==200);yield return null;Check("Rune overlay retains its own pointer raycaster",GameObject.Find("runes").GetComponent<GraphicRaycaster>()!=null);ui.Resume();
  ui.Journal();Check("Journal suppresses HUD",!ui.GameplayHudVisible);ui.Resume();ui.Practice();Check("Practice selection suppresses HUD",!ui.GameplayHudVisible);ui.Resume();
  var player=Object.FindFirstObjectByType<PlayerController2D>();player.GetComponent<HealthComponent>().Invincible=false;player.GetComponent<HealthComponent>().ApplyDamage(999);
  Check("Actual player death opens result layer and hides HUD immediately",ui.Mode=="death"&&!ui.GameplayHudVisible&&ui.ActiveMenuOrder==500);yield return null;BableVerification.Capture("revision17/death-menu.png");ui.Respawn();yield return new WaitForSeconds(1.3f);Check("Respawn returns to visible gameplay HUD",ui.Mode=="play"&&ui.GameplayHudVisible);
  ui.Victory();Check("Victory uses result layer without HUD",ui.ActiveMenuOrder==500&&!ui.GameplayHudVisible);yield return null;BableVerification.Capture("revision17/victory-menu.png");
  ui.MainMenu();yield return null;Check("Only current menu remains active",GameObject.Find("Bable Interface").GetComponentsInChildren<Canvas>().Count(c=>c.overrideSorting)==1);Check("Returning to main menu keeps HUD hidden",!ui.GameplayHudVisible);Debug.Log("Revision17 completed: "+checks.Count+" checks / "+failures.Count+" failures");
 }
}
