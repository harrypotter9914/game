using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;
using Bable;
public static class BableRevision54 {
 public static readonly Vector2[] ApproachPoints={new(65.5f,-47),new(256.5f,-47),new(314.5f,-27),new(359.5f,29),new(158.5f,77)};
 public static void Install(){
  var scene=EditorSceneManager.OpenScene("Assets/Scenes/Gameplay_Main.unity");
  var map=GameObject.Find("GroundTilemap").GetComponent<Tilemap>();
  foreach(var boss in Object.FindObjectsByType<BossBrain>(FindObjectsSortMode.None)){
   var point=ApproachPoints[(int)boss.profile.kind];var cell=Vector3Int.FloorToInt(point);
   for(int x=cell.x-1;x<=cell.x+1;x++){
    if(!map.HasTile(new Vector3Int(x,cell.y-1,0)))throw new System.Exception("No permanent altar floor: "+boss.profile.kind);
    for(int y=cell.y;y<cell.y+3;y++)if(map.HasTile(new Vector3Int(x,y,0)))throw new System.Exception("Altar obstructed: "+boss.profile.kind);
   }
   if(boss.InArena(point+Vector2.up))throw new System.Exception("Altar inside encounter: "+boss.profile.kind);
   string name="Approach altar - "+boss.profile.kind;var go=GameObject.Find(name)??new GameObject(name);go.transform.position=point;
   if(go.GetComponent<Babel.Runtime.World.CheckpointMarker>()==null)go.AddComponent<Babel.Runtime.World.CheckpointMarker>();
   var box=go.GetComponent<BoxCollider2D>();if(box==null)box=go.AddComponent<BoxCollider2D>();box.isTrigger=true;box.size=new Vector2(2.4f,2);box.offset=Vector2.up;
   var altar=go.GetComponent<AltarCheckpoint>();if(altar==null)altar=go.AddComponent<AltarCheckpoint>();altar.PrepareVisual();
  }
  EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);BablePlayerBuilds.Configure();
 }
 public static void Build(){BablePlayerBuilds.Both();}
 public static void Audit(){
  EditorSceneManager.OpenScene("Assets/Scenes/Gameplay_Main.unity");
  var map=GameObject.Find("GroundTilemap").GetComponent<Tilemap>();var s=new StringBuilder();
  foreach(var a in Object.FindObjectsByType<AltarCheckpoint>(FindObjectsSortMode.None))s.AppendLine("ALTAR "+a.name+" "+a.transform.position);
  foreach(var b in Object.FindObjectsByType<BossBrain>(FindObjectsSortMode.None).OrderBy(b=>b.profile.kind)){
   s.AppendLine("BOSS "+b.profile.kind+" position="+b.transform.position+" arena="+b.arenaCenter+" size="+b.arenaSize);
   int left=Mathf.FloorToInt(b.arenaCenter.x-b.arenaSize.x/2)-12,right=Mathf.CeilToInt(b.arenaCenter.x+b.arenaSize.x/2)+12;
   int top=Mathf.CeilToInt(b.arenaCenter.y+b.arenaSize.y/2)+10,bottom=Mathf.FloorToInt(b.arenaCenter.y-b.arenaSize.y/2)-5;
   s.AppendLine("x from "+left+" to "+right);
   for(int y=top;y>=bottom;y--){s.Append(y.ToString().PadLeft(4)+" ");for(int x=left;x<=right;x++)s.Append(map.HasTile(new Vector3Int(x,y,0))?'#':'.');s.AppendLine();}
  }
  Directory.CreateDirectory("../reference/revision54");File.WriteAllText("../reference/revision54/arena-audit.txt",s.ToString());
 }
}
