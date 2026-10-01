using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using Bable;
using Babel.Runtime.Characters.Player;
using Babel.Runtime.Core;
public static class BableRevision35Live {
 public static void Run(){var host=new GameObject("Gait verification");Object.DontDestroyOnLoad(host);host.AddComponent<BableTestHost>().StartCoroutine(Check(host));}
 static IEnumerator Check(GameObject host){
  TowerLoading.Load("Gameplay_Main");while(TowerLoading.Busy||BableGameUI.Instance==null||!BableGameUI.Instance.Ready)yield return null;
  yield return new WaitForSecondsRealtime(.6f);GameSession.Instance.UnlockWeapon();
  var p=Object.FindFirstObjectByType<PlayerController2D>();var art=p.GetComponent<CharacterPresentation>();art.ReleaseAction();
  var sr=art.animator.GetComponent<SpriteRenderer>();var report=new List<string>();var sprites=new HashSet<string>();
  foreach(int direction in new[]{1,-1}){p.SetTestInput(direction,0);float start=p.transform.position.x;for(int i=0;i<24;i++){yield return new WaitForSeconds(.05f);sprites.Add(AssetDatabase.GetAssetPath(sr.sprite)+"/"+sr.sprite.name);if(i%4==0)BableVerification.Capture("revision35/live-"+direction+"-"+i.ToString("D2")+".png");}report.Add(((p.transform.position.x-start)*direction>.5f?"PASS ":"FAIL ")+"actual horizontal travel "+direction);}
  bool onlyNew=true;foreach(var s in sprites)if(!s.Contains("Gait35_run"))onlyNew=false;
  report.Add((onlyNew?"PASS ":"FAIL ")+"movement uses new run atlas");report.Add((sprites.Count>=12?"PASS ":"FAIL ")+"distinct movement frames: "+sprites.Count);
  p.SetTestInput(0,0);yield return new WaitForSeconds(.5f);report.Add(((art.animator.GetCurrentAnimatorStateInfo(0).IsName("rightidle")||art.animator.GetCurrentAnimatorStateInfo(0).IsName("leftidle"))?"PASS ":"FAIL ")+"returns to idle: "+sr.sprite.name);
  File.WriteAllLines("../reference/revision35/live-tests.txt",report);BableGameUI.Instance.MainMenu();Object.Destroy(host);
 }
}


