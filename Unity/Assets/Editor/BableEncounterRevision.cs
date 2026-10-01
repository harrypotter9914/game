using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;
using UnityEditor.SceneManagement;
using Bable;
using Babel.Runtime.Characters.Enemies;
using Object=UnityEngine.Object;
namespace Babel.EditorTools
{
    public static class BableEncounterRevision
    {
        public static void Apply()
        {
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/Gameplay_Main.unity");var map=GameObject.Find("GroundTilemap").GetComponent<Tilemap>();
            Directory.CreateDirectory("../reference/revision6");if(!File.Exists("../reference/revision6/Gameplay_Main.before.unity"))File.Copy(scene.path,"../reference/revision6/Gameplay_Main.before.unity");
            var population=GameObject.Find("Campaign population");
            foreach(var zone in Object.FindObjectsByType<SummoningEncounter>(FindObjectsSortMode.None)){
                var restored=(GameObject)PrefabUtility.InstantiatePrefab(zone.enemyPrefab);restored.transform.SetParent(population.transform);var box=restored.GetComponent<BoxCollider2D>();float offset=(box.offset.y-box.size.y/2)*restored.transform.localScale.y;restored.transform.position=new Vector3(zone.feet.x,zone.feet.y-offset+.035f,0);
            }
            var previous=GameObject.Find("Campaign ambushes");if(previous!=null)Object.DestroyImmediate(previous);
            previous=GameObject.Find("Boss room exclusions");if(previous!=null)Object.DestroyImmediate(previous);
            var exclusions=new GameObject("Boss room exclusions");var rooms=new List<Rect>();
            foreach(var boss in Object.FindObjectsByType<BossBrain>(FindObjectsSortMode.None)){
                Vector2 size=boss.arenaSize+Vector2.one*10;var room=new Rect(boss.arenaCenter-size/2,size);rooms.Add(room);var go=new GameObject(boss.name+" - no minions");go.transform.SetParent(exclusions.transform);go.AddComponent<BossRoomBoundary>().area=room;
            }
            int removed=0,aligned=0;var log=new List<string>();
            foreach(var enemy in Object.FindObjectsByType<EnemyControllerBase>(FindObjectsSortMode.None).Where(e=>!(e is BossBrain)).ToArray()){
                var box=enemy.GetComponent<BoxCollider2D>();float offset=(box.offset.y-box.size.y/2)*enemy.transform.localScale.y;Vector2 feet=(Vector2)enemy.transform.position+Vector2.up*offset;
                if(rooms.Any(r=>r.Contains(feet)||r.Contains(feet+Vector2.up*2))){log.Add("REMOVED from boss room: "+enemy.name+" "+feet);Object.DestroyImmediate(enemy.gameObject);removed++;continue;}
                var cell=map.WorldToCell(feet+Vector2.up*.2f);bool found=false;
                for(int y=cell.y+2;y>=cell.y-8;y--){var c=new Vector3Int(cell.x,y,0);if(!map.HasTile(c)||map.HasTile(c+Vector3Int.up)||map.HasTile(c+Vector3Int.up*2)||map.HasTile(c+Vector3Int.up*3))continue;feet=new Vector2(enemy.transform.position.x,map.GetCellCenterWorld(c).y+.5f);found=true;break;}
                if(!found){Object.DestroyImmediate(enemy.gameObject);removed++;continue;}
                enemy.transform.position=new Vector3(feet.x,feet.y-offset+.035f,0);PrefabUtility.RecordPrefabInstancePropertyModifications(enemy.transform);aligned++;
            }
            var ambushes=new GameObject("Campaign ambushes");int zones=0;
            var candidates=Object.FindObjectsByType<EnemyControllerBase>(FindObjectsSortMode.None).Where(e=>!(e is BossBrain)).OrderBy(e=>e.transform.position.y).ThenBy(e=>e.transform.position.x).ToArray();
            for(int i=0;i<candidates.Length;i++){
                if(i%2!=0||zones>=12)continue;var enemy=candidates[i];var box=enemy.GetComponent<BoxCollider2D>();float offset=(box.offset.y-box.size.y/2)*enemy.transform.localScale.y;Vector2 feet=(Vector2)enemy.transform.position+Vector2.up*(offset-.035f);var cell=map.WorldToCell(feet+Vector2.down*.1f);
                bool safe=true;for(int x=-5;x<=5;x++){if(!map.HasTile(cell+new Vector3Int(x,0,0)))safe=false;for(int y=1;y<=4;y++)if(map.HasTile(cell+new Vector3Int(x,y,0)))safe=false;}if(!safe)continue;
                string path=PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(enemy.gameObject);var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(prefab==null)continue;
                var zone=new GameObject("Ambush "+(++zones)+(zones%3==0?" - three waves":zones%3==1?" - arrival":" - reinforcement"));zone.transform.SetParent(ambushes.transform);zone.transform.position=new Vector3(feet.x-4,feet.y+1,0);var trigger=zone.AddComponent<BoxCollider2D>();trigger.isTrigger=true;trigger.size=new Vector2(6,3);
                var encounter=zone.AddComponent<SummoningEncounter>();encounter.enemyPrefab=prefab;encounter.feet=feet;encounter.waves=zones%3==0?3:zones%3==1?1:2;
                log.Add("SUMMON "+zone.name+" "+feet+" "+prefab.name);Object.DestroyImmediate(enemy.gameObject);
            }
            log.Add("Removed invalid/arena minions="+removed+" grounded="+aligned+" summon zones="+zones+" static patrols="+Object.FindObjectsByType<EnemyControllerBase>(FindObjectsSortMode.None).Count(e=>!(e is BossBrain)));
            File.WriteAllLines("../reference/revision6/encounters.txt",log);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        }
    }
}
