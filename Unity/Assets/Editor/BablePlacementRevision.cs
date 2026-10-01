using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;
using UnityEditor.SceneManagement;
using Babel.Runtime.World;
using Babel.Runtime.Characters.Enemies;
using Babel.Runtime.Shop;
using Bable;
using Object=UnityEngine.Object;

namespace Babel.EditorTools
{
    public static class BablePlacementRevision
    {
        public const string Source="https://m7722tyzm4.feishu.cn/wiki/BBnVwAwbqiNYQMksSepcYztEnYg";
        // Matched by the distinctive solid platforms in the five 7.6 room screenshots.
        public static readonly Rect[] Rooms={new Rect(49,-61,20,14),new Rect(263,-60,12,16),new Rect(315,-41,20,18),new Rect(363,27,32,28),new Rect(161,75,66,30)};
        public static bool Forbidden(Vector2 p)=>new Rect(259,51,86,48).Contains(p)||p.x>=350&&p.y>=55||p.y>123;
        [Serializable] class SourceObject {public string id;public float x,y;}
        [Serializable] class Layout {public SourceObject[] pickups;}
        static Tilemap ground;
        static readonly List<string> log=new();
        static void Record(Component c){EditorUtility.SetDirty(c);if(PrefabUtility.IsPartOfPrefabInstance(c))PrefabUtility.RecordPrefabInstancePropertyModifications(c);}
        public static bool Clear(Vector2 center,Vector2 size){for(int x=Mathf.FloorToInt(center.x-size.x/2+.02f);x<=Mathf.FloorToInt(center.x+size.x/2-.02f);x++)for(int y=Mathf.FloorToInt(center.y-size.y/2+.02f);y<=Mathf.FloorToInt(center.y+size.y/2-.02f);y++)if(ground.HasTile(new Vector3Int(x,y,0)))return false;return true;}
        static Vector2 Floor(Vector2 wanted,Rect room,BoxCollider2D box)
        {
            Vector2 best=Vector2.zero;float score=float.MaxValue;
            foreach(var c in ground.cellBounds.allPositionsWithin){if(!ground.HasTile(c))continue;var p=new Vector2(c.x+.5f,c.y+1-box.offset.y+box.size.y/2+.04f);
                if(!room.Contains(p)||!Clear(p+box.offset,box.size))continue;
                bool support=true;for(int x=Mathf.FloorToInt(p.x-box.size.x/2);x<=Mathf.FloorToInt(p.x+box.size.x/2);x++)if(!ground.HasTile(new Vector3Int(x,c.y,0)))support=false;
                if(!support)continue;float s=(p-wanted).sqrMagnitude;if(s<score){score=s;best=p;}}
            if(score==float.MaxValue)throw new Exception("No safe floor inside authored room "+room);return best;
        }
        static Vector2 NearbyAir(Vector2 wanted)
        {
            if(Clear(wanted,new Vector2(.9f,.9f))&&!Forbidden(wanted))return wanted;
            Vector2 best=wanted;float score=float.MaxValue;
            for(int dx=-5;dx<=5;dx++)for(int dy=-5;dy<=5;dy++){var p=wanted+new Vector2(dx,dy);if(Forbidden(p)||!Clear(p,Vector2.one))continue;float s=dx*dx+dy*dy;if(s<score){score=s;best=p;}}
            if(score==float.MaxValue)throw new Exception("Source pickup inside solid terrain: "+wanted);return best;
        }
        static void Treasure(GameObject root)
        {
            foreach(var pickup in root.GetComponentsInChildren<CollectiblePickup>(true)){
                int kind=new SerializedObject(pickup).FindProperty("pickupKind").enumValueIndex;
                if(kind!=2&&kind!=3&&kind!=5&&!(kind==6&&pickup.name.StartsWith("hoxi")))continue;
                var art=pickup.GetComponent<AnimatedTreasure>();if(art==null)art=pickup.gameObject.AddComponent<AnimatedTreasure>();art.coin=kind==5;art.PrepareVisual();Record(art);
                foreach(var sr in pickup.GetComponentsInChildren<SpriteRenderer>(true))Record(sr);
            }
        }
        static void Gate(string name,RectInt cells,string tile,bool onlyExisting)
        {
            var root=new GameObject(name);root.transform.SetParent(GameObject.Find("Authored shockwave passages").transform);
            var sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/RegionalTiles/"+tile+"Cracked.png");
            if(sprite==null)sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/RegionalTiles/GreyKeepCracked.png");
            if(sprite==null)throw new Exception("Cracked masonry sprite missing");
            int count=0;
            // Each piece is one real grid cell. A hit removes exactly that piece and its original solid cell.
            foreach(var c in cells.allPositionsWithin){var cell=new Vector3Int(c.x,c.y,0);if(onlyExisting&&!ground.HasTile(cell))continue;
                var go=new GameObject("Fractured tile "+c.x+","+c.y);go.transform.SetParent(root.transform);go.transform.position=new Vector3(c.x+.5f,c.y+.5f,0);
                var sr=go.AddComponent<SpriteRenderer>();sr.sprite=sprite;sr.sortingOrder=3;sr.drawMode=SpriteDrawMode.Sliced;sr.size=Vector2.one;sr.color=new Color(.84f,.87f,.95f);
                go.AddComponent<BoxCollider2D>().size=Vector2.one;var wall=go.AddComponent<BreakableWall>();wall.requiresShockwave=true;wall.designReference=name+" / "+Source;count++;
            }log.Add("BREAKABLE "+name+" "+cells+" pieces="+count);
        }
        [MenuItem("Bable/Revision 7/Apply documented placements")]
        public static void Apply()
        {
            if(EditorApplication.isPlaying)throw new Exception("Stop play mode first");
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/Gameplay_Main.unity");ground=GameObject.Find("GroundTilemap").GetComponent<Tilemap>();
            Directory.CreateDirectory("../reference/revision7");if(!File.Exists("../reference/revision7/Gameplay_Main.before.unity"))File.Copy(scene.path,"../reference/revision7/Gameplay_Main.before.unity");
            log.Clear();log.Add("Room source: "+Source);log.Add("Coordinates: old RigidBody/Transform Y is inverted, collider centreY is already physics-up.");
            var bosses=Object.FindObjectsByType<BossBrain>(FindObjectsSortMode.None).OrderBy(b=>(int)b.profile.kind).ToArray();
            Vector2[] desired={new(57.5f,-59.15f),new(270,-58),new(325,-39),new(379,42),new(191.5f,76.85f)};
            foreach(var b in bosses){int i=(int)b.profile.kind;b.arenaCenter=Rooms[i].center;b.arenaSize=Rooms[i].size;
                b.transform.position=i==3?desired[i]:Floor(desired[i],Rooms[i],b.GetComponent<BoxCollider2D>());Record(b);Record(b.transform);log.Add("BOSS "+b.profile.kind+" spawn="+b.transform.position+" room="+Rooms[i]);}
            var old=GameObject.Find("Boss room exclusions");if(old!=null)Object.DestroyImmediate(old);var exclusions=new GameObject("Boss room exclusions");
            for(int i=0;i<5;i++){var go=new GameObject(bosses[i].profile.displayName+" - authored room");go.transform.SetParent(exclusions.transform);var r=Rooms[i];r.xMin-=1;r.xMax+=1;r.yMin-=1;r.yMax+=1;go.AddComponent<BossRoomBoundary>().area=r;}
            var rooms=Object.FindObjectsByType<BossRoomBoundary>(FindObjectsSortMode.None);
            foreach(var e in Object.FindObjectsByType<EnemyControllerBase>(FindObjectsSortMode.None).Where(e=>!(e is BossBrain)).ToArray())
                if(Forbidden(e.transform.position)||rooms.Any(r=>r.area.Contains(e.transform.position))||e.transform.position.x>250&&e.transform.position.y<-63){log.Add("REMOVED misplaced minion "+e.name+" "+e.transform.position);Object.DestroyImmediate(e.gameObject);}
            foreach(var z in Object.FindObjectsByType<SummoningEncounter>(FindObjectsSortMode.None).ToArray())if(Forbidden(z.feet)||rooms.Any(r=>r.area.Contains(z.feet))||z.feet.x>250&&z.feet.y<-63){log.Add("REMOVED invalid summon "+z.name);Object.DestroyImmediate(z.gameObject);}
            foreach(var p in Object.FindObjectsByType<CollectiblePickup>(FindObjectsSortMode.None).ToArray())if(Forbidden(p.transform.position)&&!p.name.StartsWith("hoxi")){log.Add("REMOVED inaccessible pickup "+p.name);Object.DestroyImmediate(p.gameObject);}
            foreach(var merchant in Object.FindObjectsByType<ShopkeeperController>(FindObjectsSortMode.None))if(Forbidden(merchant.transform.position)){merchant.transform.position=new Vector3(272,-36.97f,0);Record(merchant.transform);log.Add("MOVED merchant out of decorative mouth");}
            var layout=JsonUtility.FromJson<Layout>(Resources.Load<TextAsset>("Bable/original-layout").text);
            foreach(var source in layout.pickups){var go=GameObject.Find(source.id);if(go==null)throw new Exception("Missing original pickup "+source.id);var wanted=new Vector2(source.x*2,-source.y*2);go.transform.position=NearbyAir(wanted);Record(go.transform);log.Add("SOURCE PICKUP "+source.id+" source="+wanted+" actual="+go.transform.position);}
            var checkpoints=Object.FindObjectsByType<CollectiblePickup>(FindObjectsSortMode.None).Where(p=>p.name=="Shrine Checkpoint").OrderBy(p=>p.transform.position.x).ToArray();
            Vector2[] checkpointPoints={new(47,-30),new(150,14),new(290,-37),new(305,-27),new(350,28)};
            for(int i=0;i<checkpoints.Length;i++){checkpoints[i].transform.position=NearbyAir(checkpointPoints[i%checkpointPoints.Length]);Record(checkpoints[i].transform);}
            old=GameObject.Find("Authored shockwave passages");if(old!=null)Object.DestroyImmediate(old);new GameObject("Authored shockwave passages");
            Gate("Screenshot 1 - fractured raised corridor",new RectInt(395,27,36,4),"GreyKeep",true);
            Gate("Screenshot 1 - jump and shockwave upper blockage",new RectInt(395,33,36,10),"GreyKeep",true);
            Gate("Korah side chamber entrance",new RectInt(313,-51,4,4),"MossLabyrinth",false);
            Gate("Azazel chamber entrance",new RectInt(315,-29,4,4),"MossLabyrinth",true);
            Gate("Bel five-platform chamber entrance",new RectInt(361,29,2,4),"GreyKeep",false);
            foreach(var root in scene.GetRootGameObjects())Treasure(root);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            foreach(var guid in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Prefabs"})){string path=AssetDatabase.GUIDToAssetPath(guid);var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(prefab.GetComponentsInChildren<CollectiblePickup>(true).Length==0)continue;var root=PrefabUtility.LoadPrefabContents(path);Treasure(root);PrefabUtility.SaveAsPrefabAsset(root,path);PrefabUtility.UnloadPrefabContents(root);}
            for(int i=1;i<=5;i++){var practice=EditorSceneManager.OpenScene("Assets/Scenes/Boss_Test_"+i+".unity");foreach(var root in practice.GetRootGameObjects())Treasure(root);EditorSceneManager.MarkSceneDirty(practice);EditorSceneManager.SaveScene(practice);}
            AssetDatabase.SaveAssets();EditorSceneManager.OpenScene("Assets/Scenes/Gameplay_Main.unity");File.WriteAllLines("../reference/revision7/placements.txt",log);
        }
    }
}
