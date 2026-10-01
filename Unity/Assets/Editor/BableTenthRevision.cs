using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Animations;
using Bable;
using Babel.Runtime.World;
using Babel.Runtime.Characters.Enemies;
using Babel.Runtime.Characters.Player;
using Object=UnityEngine.Object;
public static class BableTenthRevision
{
    static Tilemap ground;static Transform gates;static List<string> log=new();
    public static readonly Rect KorahRoom=new Rect(259,-61,36,18);
    static void Record(Component c){EditorUtility.SetDirty(c);if(PrefabUtility.IsPartOfPrefabInstance(c))PrefabUtility.RecordPrefabInstancePropertyModifications(c);}
    static bool Clear(Vector2 p,Vector2 size){for(int x=Mathf.FloorToInt(p.x-size.x/2+.02f);x<=Mathf.FloorToInt(p.x+size.x/2-.02f);x++)for(int y=Mathf.FloorToInt(p.y-size.y/2+.02f);y<=Mathf.FloorToInt(p.y+size.y/2-.02f);y++)if(ground.HasTile(new Vector3Int(x,y,0)))return false;return true;}
    static Vector2 Floor(BossBrain boss){var box=boss.GetComponent<BoxCollider2D>();Vector2 best=default;float score=float.MaxValue;
        foreach(var c in ground.cellBounds.allPositionsWithin){if(!ground.HasTile(c))continue;Vector2 p=ground.GetCellCenterWorld(c);p.y+=.5f-box.offset.y+box.size.y/2+.015f;if(!KorahRoom.Contains(p)||!Clear(p+box.offset,box.size))continue;float s=(p-new Vector2(270,-58)).sqrMagnitude;if(s<score){score=s;best=p;}}
        if(score==float.MaxValue)throw new Exception("No Korah standing location");return best;
    }
    static void Gate(string name,RectInt area,string region="MossLabyrinth"){
        var root=new GameObject(name);root.transform.SetParent(gates);var sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/RegionalTiles/"+region+"Cracked.png");
        foreach(var p in area.allPositionsWithin){var go=new GameObject("Fractured masonry "+p);go.transform.SetParent(root.transform);go.transform.position=new Vector3(p.x+.5f,p.y+.5f,0);var sr=go.AddComponent<SpriteRenderer>();sr.sprite=sprite;sr.sortingOrder=3;sr.drawMode=SpriteDrawMode.Sliced;sr.size=Vector2.one;go.AddComponent<BoxCollider2D>().size=Vector2.one;go.AddComponent<BreakableWall>().designReference="Revision 10 / "+name;}
        log.Add(name+" "+area);
    }
    static void ImportArt(){
        string path="Assets/Resources/Bable/NewArt/PilgrimUnarmed.png";AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);var imp=(TextureImporter)AssetImporter.GetAtPath(path);imp.textureType=TextureImporterType.Sprite;imp.spriteImportMode=SpriteImportMode.Multiple;imp.spritePixelsPerUnit=116;imp.filterMode=FilterMode.Point;imp.textureCompression=TextureImporterCompression.Uncompressed;imp.mipmapEnabled=false;imp.alphaIsTransparency=true;imp.maxTextureSize=4096;
        imp.spritesheet=Enumerable.Range(0,32).Select(i=>new SpriteMetaData{name="Unarmed_"+i.ToString("D2"),rect=new Rect(i%8*320,(3-i/8)*288,320,288),alignment=9,pivot=new Vector2(.5f,27f/288)}).ToArray();imp.SaveAndReimport();
        var frames=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(s=>s.name).ToArray();var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Art/NativeAnimations/Pilgrim.controller");
        Clip(controller,"run",frames.Take(8).ToArray(),12,true);Clip(controller,"idle",frames.Skip(8).Take(8).ToArray(),8,true);Clip(controller,"takeoff",frames.Skip(16).Take(2).ToArray(),12,false);Clip(controller,"jump",frames.Skip(18).Take(2).ToArray(),8,false);Clip(controller,"fall",frames.Skip(20).Take(2).ToArray(),8,false);Clip(controller,"land",frames.Skip(22).Take(2).ToArray(),10,false);Clip(controller,"receive",frames.Skip(24).Take(8).ToArray(),4,false);
        path="Assets/Resources/Bable/NewArt/ConsecratedBlade.png";AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);imp=(TextureImporter)AssetImporter.GetAtPath(path);imp.textureType=TextureImporterType.Sprite;imp.spriteImportMode=SpriteImportMode.Single;imp.filterMode=FilterMode.Point;imp.textureCompression=TextureImporterCompression.Uncompressed;imp.alphaIsTransparency=true;imp.SaveAndReimport();
    }
    static void Clip(AnimatorController controller,string action,Sprite[] frames,float fps,bool loop){string path="Assets/Art/NativeAnimations/Pilgrim_unarmed"+action+".anim";var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);if(clip==null){clip=new AnimationClip();AssetDatabase.CreateAsset(clip,path);}clip.frameRate=fps;AnimationUtility.SetObjectReferenceCurve(clip,new EditorCurveBinding{path="",type=typeof(SpriteRenderer),propertyName="m_Sprite"},frames.Select((s,i)=>new ObjectReferenceKeyframe{time=i/fps,value=s}).ToArray());var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=loop;settings.stopTime=frames.Length/fps;AnimationUtility.SetAnimationClipSettings(clip,settings);var sm=controller.layers[0].stateMachine;var state=sm.states.Select(s=>s.state).FirstOrDefault(s=>s.name=="rightunarmed"+action);if(state==null)state=sm.AddState("rightunarmed"+action);state.motion=clip;EditorUtility.SetDirty(controller);}
    public static void Apply(){
        if(EditorApplication.isPlaying)throw new Exception("Stop Play Mode");ImportArt();log.Clear();
        foreach(var entry in EditorBuildSettings.scenes.Where(s=>s.enabled)){
            var scene=EditorSceneManager.OpenScene(entry.path);ground=GameObject.Find("GroundTilemap").GetComponent<Tilemap>();string backup="../reference/revision10/"+scene.name+".before.unity";if(!File.Exists(backup))File.Copy(entry.path,backup);
            foreach(var player in Object.FindObjectsByType<PlayerController2D>(FindObjectsSortMode.None)){var awakening=player.GetComponent<WeaponAwakening>();if(awakening==null)awakening=player.gameObject.AddComponent<WeaponAwakening>();Record(awakening);}
            foreach(var pickup in Object.FindObjectsByType<CollectiblePickup>(FindObjectsSortMode.None)){var art=pickup.GetComponent<AnimatedTreasure>();if(art==null)art=pickup.gameObject.AddComponent<AnimatedTreasure>();art.coin=new SerializedObject(pickup).FindProperty("pickupKind").enumValueIndex==5;if(!art.coin){for(int step=0;step<16&&!Clear(pickup.transform.position,new Vector2(.8f,1.4f));step++)pickup.transform.position+=Vector3.up*.25f;Record(pickup.transform);}art.PrepareVisual();Record(art);foreach(var sr in pickup.GetComponentsInChildren<SpriteRenderer>(true))Record(sr);}
            if(scene.name=="Gameplay_Main"){
                ground=GameObject.Find("GroundTilemap").GetComponent<Tilemap>();var boss=Object.FindObjectsByType<BossBrain>(FindObjectsSortMode.None).First(b=>b.profile.kind==BossKind.Burrow);boss.arenaCenter=KorahRoom.center;boss.arenaSize=KorahRoom.size;boss.transform.position=Floor(boss);Record(boss);Record(boss.transform);log.Add("Korah "+boss.transform.position+" arena "+KorahRoom);
                foreach(var boundary in Object.FindObjectsByType<BossRoomBoundary>(FindObjectsSortMode.None))if(boundary.name.Contains("Korah")){boundary.area=new Rect(258,-62,38,20);Record(boundary);}
                foreach(var e in Object.FindObjectsByType<EnemyControllerBase>(FindObjectsSortMode.None).Where(e=>!(e is BossBrain)).ToArray())if(KorahRoom.Contains(e.transform.position)){log.Add("Removed minion "+e.name);Object.DestroyImmediate(e.gameObject);}
                foreach(var z in Object.FindObjectsByType<SummoningEncounter>(FindObjectsSortMode.None).ToArray())if(KorahRoom.Contains(z.feet)){log.Add("Removed summon "+z.name);Object.DestroyImmediate(z.gameObject);}
                var old=GameObject.Find("Revision 10 ability passages");if(old!=null)Object.DestroyImmediate(old);gates=new GameObject("Revision 10 ability passages").transform;
                Gate("Image 3 - lower vine cross passage",new RectInt(299,-63,4,2));
                Gate("Image 4 - builders star upward entry",new RectInt(309,-51,4,2));
                Gate("Image 5 - upper room threshold",new RectInt(377,25,4,2),"GreyKeep");
                Gate("Image 6 - upward journal alcove",new RectInt(391,17,2,2),"GreyKeep");
                Gate("Image 7 - lower east return shortcut",new RectInt(199,-49,2,10));
                Gate("Image 7 - upper descent threshold",new RectInt(199,-39,4,2));
                Gate("Image 8 - Moon alcove upward seal",new RectInt(255,-61,4,2));
                Gate("Vine labyrinth second crossing",new RectInt(285,-65,2,2));
                var bridge=Object.FindFirstObjectByType<ScriptedBridgeCollapse>();var sample=ground.GetTile(new Vector3Int(183,11,0));
                for(int x=167;x<227;x++)for(int y=11;y<=12;y++)if(!ground.HasTile(new Vector3Int(x,y,0)))ground.SetTile(new Vector3Int(x,y,0),sample);
                bridge.firstCell=new Vector3Int(193,11,0);bridge.size=new Vector2Int(12,2);bridge.landing=new Vector2(190,-35.8f);bridge.transform.position=new Vector3(197.5f,15,0);var trigger=bridge.GetComponent<BoxCollider2D>();trigger.isTrigger=true;trigger.size=new Vector2(3,6);Record(bridge);Record(bridge.transform);Record(trigger);
                EditorUtility.SetDirty(ground);
            }
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        }
        AssetDatabase.SaveAssets();EditorSceneManager.OpenScene("Assets/Scenes/Gameplay_Main.unity");File.WriteAllLines("../reference/revision10/placements.txt",log);
    }
}
