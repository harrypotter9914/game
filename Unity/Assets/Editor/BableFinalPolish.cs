using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System.Linq;
using Bable;
public static class BableFinalPolish
{
    public static void Apply()
    {
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/Gameplay_Main.unity");var map=GameObject.Find("GroundTilemap").GetComponent<Tilemap>();var report=new System.Collections.Generic.List<string>();
        foreach(var merchant in Object.FindObjectsByType<Babel.Runtime.Shop.ShopkeeperController>(FindObjectsSortMode.None)){
            var visual=merchant.transform.Find("Merchant visual");var sprite=visual.GetComponent<SpriteRenderer>().sprite;var image=new Texture2D(2,2);ImageConversion.LoadImage(image,File.ReadAllBytes(AssetDatabase.GetAssetPath(sprite)));var pixels=image.GetPixels32();int minY=image.height,maxY=0;for(int y=0;y<image.height;y++)for(int x=0;x<image.width;x++)if(pixels[y*image.width+x].a>100){minY=Mathf.Min(minY,y);maxY=Mathf.Max(maxY,y);}Object.DestroyImmediate(image);
            float scale=2.5f/((maxY-minY+1)/sprite.pixelsPerUnit);visual.localScale=Vector3.one*scale;visual.localPosition=new Vector3(0,(sprite.pivot.y-minY)/sprite.pixelsPerUnit*scale,0);
            var cell=map.WorldToCell(merchant.transform.position);float floor=merchant.transform.position.y-.03f;for(int y=cell.y+1;y>=cell.y-5;y--){var c=new Vector3Int(cell.x,y,0);if(map.HasTile(c)&&!map.HasTile(c+Vector3Int.up)){floor=map.GetCellCenterWorld(c).y+.5f;break;}}
            merchant.transform.position=new Vector3(merchant.transform.position.x,floor+.03f,0);PrefabUtility.RecordPrefabInstancePropertyModifications(merchant.transform);PrefabUtility.RecordPrefabInstancePropertyModifications(visual);EditorUtility.SetDirty(visual);report.Add(merchant.name+" opaque feet="+(floor+.03f)+" visual offset="+visual.localPosition.y);
        }
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        foreach(var settings in EditorBuildSettings.scenes.Where(s=>s.enabled&&s.path.Contains("Boss_Test_"))){var practice=EditorSceneManager.OpenScene(settings.path);int minions=0;foreach(var enemy in Object.FindObjectsByType<Babel.Runtime.Characters.Enemies.EnemyControllerBase>(FindObjectsSortMode.None))if(!(enemy is BossBrain)){Object.DestroyImmediate(enemy.gameObject);minions++;}
            foreach(var sr in Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None).Where(s=>s.name.StartsWith("Dungeon Detail ")).ToArray())Object.DestroyImmediate(sr.gameObject);
            report.Add(settings.path+" minions removed="+minions);EditorSceneManager.MarkSceneDirty(practice);EditorSceneManager.SaveScene(practice);
        }
        EditorSceneManager.OpenScene("Assets/Scenes/Gameplay_Main.unity");File.WriteAllLines("../reference/revision6/final-polish.txt",report);
    }
    public static void Gallery()
    {
        var cam=Camera.main;var pos=cam.transform.position;float size=cam.orthographicSize;
        var points=new[]{new Vector3(10,-6,8),new Vector3(67,-55,9),new Vector3(271,8,12),new Vector3(281,-46,10),new Vector3(355,27,9),new Vector3(-43,79,9)};
        string[] names={"forest-detail","crypt-detail","climbable-lamps","vines-detail","castle-detail","dawn-road"};
        for(int i=0;i<points.Length;i++){cam.transform.position=new Vector3(points[i].x,points[i].y,-10);cam.orthographicSize=points[i].z;BableVerification.Capture("revision6/"+names[i]+".png");}
        cam.transform.position=pos;cam.orthographicSize=size;
    }
}
