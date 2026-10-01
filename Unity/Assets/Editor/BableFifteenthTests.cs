using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEngine.Tilemaps;
using Bable;
using Babel.Runtime.World;
using Babel.Runtime.Characters.Player;
using Babel.Runtime.Characters.Enemies;
using Babel.Runtime.Core;
using Babel.Runtime.Combat;
public static class BableFifteenthTests {
 static List<string> checks=new(),failures=new(),trace=new();
 static void Check(string name,bool passed){checks.Add(name);if(!passed)failures.Add(name);File.WriteAllText("../reference/revision15/tests.json",JsonUtility.ToJson(new BableVerification.Report{checks=checks.ToArray(),failures=failures.ToArray()},true));}
 static GameObject Wall(Vector2 at,Vector2 size){var o=new GameObject("Audit terrain");o.transform.position=at;o.AddComponent<BoxCollider2D>().size=size;return o;}
 public static void Run(){checks.Clear();failures.Clear();trace.Clear();new GameObject("Whole-map placement and contact audit").AddComponent<BableTestHost>().StartCoroutine(Test());}
 static IEnumerator Test(){
  var initialPlayer=Object.FindFirstObjectByType<PlayerController2D>();var initialBody=initialPlayer.GetComponent<Rigidbody2D>();
  initialBody.simulated=false;initialPlayer.transform.position=new Vector3(800,100,0);initialBody.position=initialPlayer.transform.position;
  BableGameUI.Instance.Begin();yield return new WaitForSeconds(.5f);
  var p=Object.FindFirstObjectByType<PlayerController2D>();var body=p.GetComponent<Rigidbody2D>();p.SetTestInput(0,0);p.GetComponent<HealthComponent>().Invincible=true;
  foreach(var speech in Object.FindObjectsByType<BossSpeech>(FindObjectsSortMode.None))speech.enabled=false;TowerDialogue.AbortStory();
  foreach(var e in Object.FindObjectsByType<EnemyControllerBase>(FindObjectsSortMode.None)){e.enabled=false;e.GetComponent<Rigidbody2D>().simulated=false;}
  var all=Object.FindObjectsByType<CollectiblePickup>(FindObjectsSortMode.None);
  Check("All 112 authored pickups are inspected before collection",all.Length==112);
  foreach(var pickup in all){
   var visual=pickup.transform.Find("Treasure visual").GetComponent<SpriteRenderer>();var bounds=visual.bounds;var local=visual.transform.localPosition;
   var center=pickup.transform.TransformPoint(new Vector3(local.x,0,local.z));Vector2 size=(Vector2)bounds.size+Vector2.up*.2f;
   bool clear=!Physics2D.OverlapBoxAll(center,size-Vector2.one*.02f,0).Any(TerrainMotion.Solid);
   Check("Pickup clearance: "+pickup.name,clear);if(!clear)trace.Add("Embedded pickup "+pickup.name+" "+center);
  }
  Check("No duplicate authored pickup positions",!all.GroupBy(x=>x.transform.position).Any(g=>g.Count()>1));
  Check("All six entrance coins share floor clearance",Enumerable.Range(1,6).All(i=>Mathf.Abs(GameObject.Find("Coin_"+i).transform.position.y+9.9f)<.001f));
  var composite=GameObject.Find("GroundTilemap").GetComponent<CompositeCollider2D>();Check("Tilemap uses generated continuous collision contours",composite.pathCount>0&&composite.pointCount>0);
  foreach(Vector2 dir in new[]{Vector2.right,Vector2.left,Vector2.up,Vector2.down}){
   Vector2 from=new Vector2(800,100);var obstacle=Wall(from+dir*3,Mathf.Abs(dir.x)>.5f?new Vector2(1,8):new Vector2(8,1));Physics2D.SyncTransforms();
   Vector2 moved=TreasureClearance.Move(from,from+dir*8,8);
   Check("Magnet stops at terrain "+dir,Vector2.Distance(from,moved)<2&&Vector2.Distance(from,moved)>1.5f&&TreasureClearance.Free(moved));
   Check("Collection sight blocked through terrain "+dir,!CombatGeometry.Clear(from,from+dir*6));
   Object.Destroy(obstacle);yield return null;
  }
  var floor=Wall(new Vector2(800,98),new Vector2(20,2));Physics2D.SyncTransforms();Vector2 drop;
  Check("Enemy gold finds a clear position above the floor",TreasureClearance.FindDrop(new Vector2(800,99.4f),out drop)&&TreasureClearance.Free(drop));
  trace.Add("Safe drop="+drop);Object.Destroy(floor);yield return null;
  body.simulated=true;p.transform.position=new Vector3(800,100,0);body.position=p.transform.position;body.linearVelocity=Vector2.zero;
  var smallFloor=Wall(new Vector2(805,97),new Vector2(30,2));var smallStep=Wall(new Vector2(805,98.1f),new Vector2(6,.2f));Physics2D.SyncTransforms();yield return new WaitForSeconds(1);
  p.SetTestInput(1,0);yield return new WaitForSeconds(1.2f);Check("Walks over a 0.2-unit contact step without jumping",body.position.x>804&&body.position.y>98.85f);trace.Add("Small step endpoint="+body.position);
  p.SetTestInput(0,0);Object.Destroy(smallStep);var fullStep=Wall(new Vector2(814,98.5f),new Vector2(2,1));Physics2D.SyncTransforms();p.SetTestInput(1,0);yield return new WaitForSeconds(1.8f);Check("Full-height map tiles still require a jump",body.position.x<813);p.SetTestInput(0,0);
  Object.Destroy(smallFloor);Object.Destroy(fullStep);yield return null;
  var regions=new[]{new Vector2(-5,-10),new Vector2(58,-59),new Vector2(270,-58),new Vector2(325,-39),new Vector2(379,42),new Vector2(190,77)};
  var camera=Camera.main;var fog=camera.GetComponent<PassageVisibility>();var method=typeof(PassageVisibility).GetMethod("LateUpdate",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
  for(int i=0;i<regions.Length;i++){
   body.simulated=false;p.transform.position=regions[i];body.position=regions[i];camera.GetComponent<CameraFollow2D>().SetTarget(p.transform);yield return new WaitForSeconds(1.2f);
   var sw=System.Diagnostics.Stopwatch.StartNew();for(int j=0;j<10;j++)method.Invoke(fog,null);sw.Stop();trace.Add("Region "+i+" fog ms="+sw.Elapsed.TotalMilliseconds/10);
   var tm=GameObject.Find("GroundTilemap").GetComponent<Tilemap>();
   Check("Room mask resets after crossing into region "+i,fog.IsCellVisible(tm.WorldToCell(p.transform.position)));
   BableVerification.Capture("revision15/region-"+i+".png");
  }
  File.WriteAllLines("../reference/revision15/runtime-trace.txt",trace);File.WriteAllText("../reference/revision15/finished.txt",failures.Count+" failures / "+checks.Count+" checks");
 }
}
