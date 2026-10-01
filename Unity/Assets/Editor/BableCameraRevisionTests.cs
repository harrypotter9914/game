using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Tilemaps;
using Bable;
using Babel.Runtime.World;
using Babel.Runtime.Core;
using Babel.Runtime.Characters.Player;
using Object=UnityEngine.Object;
public static class BableCameraRevisionTests {
 static List<string> checks=new();
 static void Check(string text,bool value){checks.Add((value?"PASS ":"FAIL ")+text);File.WriteAllLines("../reference/revision27/tests.txt",checks);}
 public static void Run(){checks.Clear();var g=new GameObject("Camera revision tests");Object.DontDestroyOnLoad(g);g.AddComponent<BableTestHost>().StartCoroutine(Test(g));}
 static IEnumerator Wait(){while(TowerLoading.Busy||BableGameUI.Instance==null||!BableGameUI.Instance.Ready)yield return null;yield return new WaitForSecondsRealtime(.3f);}
 static IEnumerator Test(GameObject host){yield return Wait();TowerLoading.Load("Gameplay_Main");yield return Wait();var p=Object.FindFirstObjectByType<PlayerController2D>();var camera=Camera.main;var follow=camera.GetComponent<CameraFollow2D>();var zoom=camera.GetComponent<PassageVisibility>();
  Check("No world fog object",GameObject.Find("Unexplored passage curtain")==null);Check("Zoom controller retained",zoom!=null);yield return new WaitForSecondsRealtime(.3f);BableVerification.Capture("revision27/forest.png");
  var count=GameObject.Find("Gold amount").GetComponent<Text>();var coin=GameObject.Find("Rotating gold coin").GetComponent<RectTransform>();float gap=count.rectTransform.anchoredPosition.x-count.rectTransform.rect.width-(coin.anchoredPosition.x+coin.rect.width*.5f);Check("Coin gap is six reference pixels",Mathf.Abs(gap-6)<.1f);GameSession.Instance.AddGold(12345);yield return null;gap=count.rectTransform.anchoredPosition.x-count.rectTransform.rect.width-(coin.anchoredPosition.x+coin.rect.width*.5f);Check("Coin follows wider number",Mathf.Abs(gap-6)<.1f);
  p.enabled=false;p.GetComponent<Rigidbody2D>().simulated=false;var start=p.transform.position;float wide=camera.orthographicSize;p.transform.position+=Vector3.right*4;var previous=camera.transform.position;yield return null;Check("Camera follows without teleport",Vector3.Distance(previous,camera.transform.position)<3.9f);yield return new WaitForSecondsRealtime(1.5f);var centered=camera.WorldToViewportPoint(p.transform.position);Check("Player recenters horizontally and vertically",Mathf.Abs(centered.x-.5f)<.01f&&Mathf.Abs(centered.y-.5f)<.01f);
  var tm=GameObject.Find("GroundTilemap").GetComponent<Tilemap>();bool found=false;Vector3 spot=Vector3.zero;for(int y=-90;y<-35&&!found;y++)for(int x=265;x<380&&!found;x++){var c=new Vector3Int(x,y,0);if(!tm.HasTile(c)&&!tm.HasTile(c+Vector3Int.up)&&!tm.HasTile(c+Vector3Int.left)&&!tm.HasTile(c+Vector3Int.right)&&tm.HasTile(c+Vector3Int.down)&&(tm.HasTile(c+Vector3Int.up*3)||tm.HasTile(c+Vector3Int.up*4))){spot=tm.GetCellCenterWorld(c);found=true;}}
  Check("Found narrow lower-right passage",found);if(found){p.transform.position=spot;follow.SetTarget(p.transform);yield return new WaitForSecondsRealtime(.4f);Check("Narrow passage zooms closer",camera.orthographicSize<wide&&camera.orthographicSize>=zoom.minimumSize);Check("Narrow passage has no fog",GameObject.Find("Unexplored passage curtain")==null);BableVerification.Capture("revision27/narrow-passage.png");}
  BableGameUI.Instance.MainMenu();yield return Wait();File.WriteAllText("../reference/revision27/complete.txt",checks.Count+" checks; "+checks.FindAll(x=>x.StartsWith("FAIL")).Count+" failures");Object.Destroy(host);
 }
}
