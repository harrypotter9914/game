using System.Linq;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Bable;
using Babel.Runtime.Characters.Enemies;
using Object=UnityEngine.Object;
public static class BableRevision30Population {
 public static void Apply(){
  var scene=EditorSceneManager.OpenScene("Assets/Scenes/Gameplay_Main.unity");
  var names=new[]{"MeleeEnemy","ShieldEnemy","RangedEnemy","GiantEnemy"};var prefabs=names.Select(n=>AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Characters/Enemies/"+n+".prefab")).ToArray();
  var lines=new List<string>();
  foreach(var zone in Object.FindObjectsByType<SummoningEncounter>(FindObjectsSortMode.None)){
   bool early=zone.feet.y>-15&&zone.feet.x<70;
   int[] desired=early?new[]{0,1,2}:new[]{1,2,3};
   var sequence=desired.Select(i=>prefabs[i]).Where(p=>zone.IsSafeSpawn(p)).ToArray();
   if(sequence.Length==0){lines.Add("UNCHANGED no larger safe summon: "+zone.name);continue;}
   zone.wavePrefabs=sequence;zone.enemyPrefab=sequence[0];zone.waves=sequence.Length;EditorUtility.SetDirty(zone);
   lines.Add(zone.name+" "+zone.feet+" : "+string.Join(" -> ",sequence.Select(p=>p.name)));
  }
  var probeObject=new GameObject("Temporary placement check");var probe=probeObject.AddComponent<SummoningEncounter>();int conversion=0;
  foreach(var enemy in Object.FindObjectsByType<MeleeEnemyController>(FindObjectsSortMode.None).OrderBy(e=>e.transform.position.y).ThenBy(e=>e.transform.position.x).ToArray()){
   if(enemy.transform.position.x<80&&enemy.transform.position.y>-15)continue;
   var box=enemy.GetComponent<BoxCollider2D>();if(box==null)continue;probe.feet=(Vector2)enemy.transform.position+Vector2.up*((box.offset.y-box.size.y/2)*enemy.transform.localScale.y-.035f);
   int type=conversion%3==0?3:conversion%3==1?2:1;
   if(!probe.IsSafeSpawn(prefabs[type])){type=2;if(!probe.IsSafeSpawn(prefabs[type]))continue;}
   var go=(GameObject)PrefabUtility.InstantiatePrefab(prefabs[type]);go.transform.SetParent(enemy.transform.parent);var own=go.GetComponent<BoxCollider2D>();go.transform.position=probe.feet+Vector2.up*(-(own.offset.y-own.size.y/2)*go.transform.localScale.y+.035f);go.name="Rebalanced patrol "+(++conversion)+" - "+names[type];PrefabUtility.RecordPrefabInstancePropertyModifications(go.transform);
   lines.Add(enemy.name+" => "+go.name+" at "+probe.feet);Object.DestroyImmediate(enemy.gameObject);
  }
  Object.DestroyImmediate(probeObject);
  foreach(var name in names){int stat=Object.FindObjectsByType<EnemyControllerBase>(FindObjectsSortMode.None).Count(e=>PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(e.gameObject).EndsWith("/"+name+".prefab"));int waves=Object.FindObjectsByType<SummoningEncounter>(FindObjectsSortMode.None).Sum(z=>z.wavePrefabs!=null&&z.wavePrefabs.Length>0?z.wavePrefabs.Count(p=>p.name==name):z.enemyPrefab.name==name?z.waves:0);lines.Add(name+" static="+stat+" summoned="+waves);}
  EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);File.WriteAllLines("../reference/revision30/population-after.txt",lines);
 }
}
