using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.SceneManagement;
using Babel.Runtime.World;
using Babel.Runtime.Core;
using Babel.Runtime.Shop;
using Babel.Runtime.Characters.Player;
using Babel.Runtime.Characters.Enemies;
using Bable;
public static class BableSixteenthAuthor {
 static void Unpack(GameObject go){if(PrefabUtility.IsPartOfPrefabInstance(go)){var root=PrefabUtility.GetOutermostPrefabInstanceRoot(go);PrefabUtility.UnpackPrefabInstance(root,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);}}
 static void ConvertAltars(){
  foreach(var map in UnityEngine.Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.None)){map.RefreshAllTiles();var tc=map.GetComponent<TilemapCollider2D>();if(tc!=null){tc.enabled=false;tc.enabled=true;tc.ProcessTilemapChanges();}var composite=map.GetComponent<CompositeCollider2D>();if(composite!=null)composite.GenerateGeometry();}Physics2D.SyncTransforms();
  foreach(var marker in UnityEngine.Object.FindObjectsByType<CheckpointMarker>(FindObjectsSortMode.None)){
   var go=marker.gameObject;Unpack(go);var serialized=new SerializedObject(marker);serialized.FindProperty("setAsSpawnOnAwake").boolValue=false;serialized.ApplyModifiedPropertiesWithoutUndo();
   foreach(var c in go.GetComponents<Component>())if(c is CollectiblePickup||c is AnimatedTreasure||c is RelicPickupVisual||c is SpriteRenderer)UnityEngine.Object.DestroyImmediate(c);
   foreach(Transform child in go.transform.Cast<Transform>().ToArray())if(child.name!="Altar artwork")UnityEngine.Object.DestroyImmediate(child.gameObject);
   // Pick the nearest surface below the existing checkpoint, with room for the entire shrine.
   Physics2D.SyncTransforms();var original=go.transform.position;if(SceneManager.GetActiveScene().name=="Gameplay_Main"&&original.x>40&&original.x<55&&original.y<-20)original=new Vector3(44.5f,-10,0);bool placed=false;
   var ground=GameObject.Find("GroundTilemap").GetComponent<Tilemap>();float best=float.MaxValue;Vector3 chosen=original;
   for(int x=Mathf.FloorToInt(original.x)-16;x<=Mathf.CeilToInt(original.x)+16;x++)for(int y=Mathf.FloorToInt(original.y)-55;y<=Mathf.CeilToInt(original.y)+1;y++){
    if(!ground.HasTile(new Vector3Int(x,y-1,0)))continue;bool free=true;
    for(int xx=x-1;xx<=x+1;xx++){if(!ground.HasTile(new Vector3Int(xx,y-1,0)))free=false;for(int yy=y;yy<y+3;yy++)if(ground.HasTile(new Vector3Int(xx,yy,0)))free=false;}
    if(!free)continue;Vector3 candidate=new Vector3(x+.5f,y+.01f,0);float score=(candidate-original).sqrMagnitude;if(score>=best)continue;best=score;chosen=candidate;placed=true;
   }
   if(placed)go.transform.position=chosen;
   if(!placed)throw new Exception("No clear altar surface: "+go.name+" "+original);
   var box=go.GetComponent<BoxCollider2D>();if(box==null)box=go.AddComponent<BoxCollider2D>();box.isTrigger=true;box.size=new Vector2(2.4f,2);box.offset=new Vector2(0,1);
   var altar=go.GetComponent<AltarCheckpoint>()??go.AddComponent<AltarCheckpoint>();altar.PrepareVisual();EditorUtility.SetDirty(go);
  }
  foreach(var t in UnityEngine.Object.FindObjectsByType<TMPro.TextMeshPro>(FindObjectsSortMode.None))if(t.text=="CHECKPOINT")UnityEngine.Object.DestroyImmediate(t.gameObject);
 }
 public static string Apply(){
  BableSixteenthArtImport.Apply();
  foreach(var path in EditorBuildSettings.scenes.Select(s=>s.path).Where(p=>p.EndsWith("Gameplay_Main.unity")||Path.GetFileName(p).StartsWith("Boss_Test_")).ToArray()){
   if(!path.EndsWith("Gameplay_Main.unity")){string backup="../reference/revision16/backup/"+Path.GetFileName(path);if(!File.Exists(backup))File.Copy(path,backup);}
   var scene=EditorSceneManager.OpenScene(path);ConvertAltars();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
  }
  var main=EditorBuildSettings.scenes.First(s=>s.path.EndsWith("Gameplay_Main.unity")).path;EditorSceneManager.OpenScene(main);MakeLab(main);EditorSceneManager.OpenScene(main);return "Altars converted; independent rune and combat lab saved.";
 }
 static void MakeLab(string main){
  string labPath=Path.GetDirectoryName(main).Replace('\\','/')+"/Rune_Combat_Lab.unity";EditorSceneManager.SaveScene(SceneManager.GetActiveScene(),labPath,true);var scene=EditorSceneManager.OpenScene(labPath);
  var lab=new GameObject("Rune and combat laboratory").AddComponent<RuneCombatLab>();
  var merchant=UnityEngine.Object.FindObjectsByType<ShopkeeperController>(FindObjectsSortMode.None).First();var shop=UnityEngine.Object.Instantiate(merchant.gameObject);shop.name="Lab merchant";shop.transform.SetParent(null);Unpack(shop);shop.transform.position=new Vector3(8,0,0);
  var enemies=UnityEngine.Object.FindObjectsByType<EnemyControllerBase>(FindObjectsSortMode.None).Where(e=>!(e is BossBrain)).GroupBy(e=>e.GetType()).Select(g=>g.First()).Take(4).ToArray();if(enemies.Length!=4)throw new Exception("Expected four enemy archetypes, found "+enemies.Length);
  lab.enemyTemplates=enemies.Select(e=>{var g=UnityEngine.Object.Instantiate(e.gameObject);g.name=e.GetType().Name;g.SetActive(false);g.transform.SetParent(lab.transform);g.transform.position=new Vector3(0,-1000,0);return g;}).ToArray();
  var firstAltar=UnityEngine.Object.FindObjectsByType<AltarCheckpoint>(FindObjectsSortMode.None).OrderBy(a=>a.transform.position.x).First();var shrine=UnityEngine.Object.Instantiate(firstAltar.gameObject);shrine.name="Lab altar";shrine.transform.position=Vector3.zero;
  string[] keep={"GameRoot","SceneServices","Grid","Main Camera","Player","HUD"};
  foreach(var root in scene.GetRootGameObjects())if(!keep.Contains(root.name)&&root!=lab.gameObject&&root!=shop&&root!=shrine)UnityEngine.Object.DestroyImmediate(root);
  var ground=GameObject.Find("GroundTilemap").GetComponent<Tilemap>();TileBase tile=null;foreach(var cell in ground.cellBounds.allPositionsWithin)if(ground.HasTile(cell)){tile=ground.GetTile(cell);break;}
  var backdrop=GameObject.Find("Background Masonry").GetComponent<Tilemap>();TileBase backTile=null;foreach(var c in backdrop.cellBounds.allPositionsWithin)if(backdrop.HasTile(c)){backTile=backdrop.GetTile(c);break;}
  foreach(var map in UnityEngine.Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.None))map.ClearAllTiles();
  if(backTile!=null)for(int x=-18;x<=113;x++)for(int y=0;y<18;y++)backdrop.SetTile(new Vector3Int(x,y,0),backTile);
  for(int x=-18;x<=113;x++)for(int y=-5;y<0;y++)ground.SetTile(new Vector3Int(x,y,0),tile);
  for(int y=0;y<14;y++){ground.SetTile(new Vector3Int(-18,y,0),tile);ground.SetTile(new Vector3Int(113,y,0),tile);}
  ground.RefreshAllTiles();var tc=ground.GetComponent<TilemapCollider2D>();tc.ProcessTilemapChanges();ground.GetComponent<CompositeCollider2D>()?.GenerateGeometry();
  var player=UnityEngine.Object.FindFirstObjectByType<PlayerController2D>();Unpack(player.gameObject);player.transform.position=new Vector3(0,1.2f,0);Camera.main.transform.position=new Vector3(0,3,-10);
  GameObject.Find("GameRoot").GetComponent<BableGameUI>().startAtMenu=false;
  var data=new SerializedObject(GameObject.Find("GameRoot").GetComponent<GameSession>());data.FindProperty("defaultRespawnPoint").vector2Value=new Vector2(0,1.2f);data.ApplyModifiedPropertiesWithoutUndo();
  Physics2D.SyncTransforms();var visual=shop.transform.Find("Merchant visual")?.GetComponent<SpriteRenderer>();if(visual!=null)shop.transform.position+=Vector3.up*(-visual.bounds.min.y-99f/visual.sprite.pixelsPerUnit*visual.transform.lossyScale.y+.02f);
  EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);if(!EditorBuildSettings.scenes.Any(s=>s.path==labPath))EditorBuildSettings.scenes=EditorBuildSettings.scenes.Concat(new[]{new EditorBuildSettingsScene(labPath,true)}).ToArray();
 }
}
