using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;
using UnityEditor.SceneManagement;
using Bable;
namespace Babel.EditorTools
{
    public static class BableNarrativeRevision
    {
        public static void Apply()
        {
            string atlas="Assets/Resources/Bable/NewArt/VotiveAtlas.png";AssetDatabase.ImportAsset(atlas);
            var imp=(TextureImporter)AssetImporter.GetAtPath(atlas);imp.textureType=TextureImporterType.Sprite;imp.spriteImportMode=SpriteImportMode.Multiple;imp.spritePixelsPerUnit=100;imp.filterMode=FilterMode.Point;imp.mipmapEnabled=false;imp.alphaIsTransparency=true;imp.textureCompression=TextureImporterCompression.Uncompressed;imp.maxTextureSize=4096;
            var tex=new Texture2D(2,2);tex.LoadImage(File.ReadAllBytes(atlas));float w=tex.width/4f,h=tex.height/4f;
            imp.spritesheet=Enumerable.Range(0,16).Select(i=>new SpriteMetaData{name="Votive_"+i.ToString("D2"),rect=new Rect((i%4)*w,(3-i/4)*h,w,h),alignment=9,pivot=new Vector2(.5f,.5f)}).ToArray();imp.SaveAndReimport();Object.DestroyImmediate(tex);
            const string skyPath="Assets/Resources/Bable/NewArt/DawnSky.png";AssetDatabase.ImportAsset(skyPath);var skyImp=(TextureImporter)AssetImporter.GetAtPath(skyPath);skyImp.textureType=TextureImporterType.Sprite;skyImp.spriteImportMode=SpriteImportMode.Single;skyImp.filterMode=FilterMode.Point;skyImp.mipmapEnabled=false;skyImp.maxTextureSize=4096;skyImp.textureCompression=TextureImporterCompression.Uncompressed;skyImp.SaveAndReimport();
            foreach(var scene in EditorBuildSettings.scenes.Where(s=>s.enabled)){
                var loaded=EditorSceneManager.OpenScene(scene.path);
                foreach(var boss in Object.FindObjectsByType<BossBrain>(FindObjectsSortMode.None))if(boss.GetComponent<BossSpeech>()==null)boss.gameObject.AddComponent<BossSpeech>();
                if(scene.path.EndsWith("Gameplay_Main.unity")){
                    var finalBoss=Object.FindObjectsByType<BossBrain>(FindObjectsSortMode.None).First(b=>b.profile.kind==BossKind.Nero);
                    finalBoss.arenaCenter=new Vector2(194,91); // Include the original room floor at y=75.
                    PrefabUtility.RecordPrefabInstancePropertyModifications(finalBoss);EditorUtility.SetDirty(finalBoss);
                    foreach(var o in Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None).Where(s=>s.name=="Forest distant canopy"||s.name=="Forest approach canopy"||s.name.StartsWith("Dawn exterior")).ToArray())Object.DestroyImmediate(o.gameObject);
                    var forest=Resources.Load<Sprite>("Bable/NewArt/ForestBackdrop");
                    Background("Forest approach canopy",forest,new Vector2(0,21),new Vector2(48,32),-30).AddComponent<ForestAscentBackdrop>();
                    var sky=AssetDatabase.LoadAssetAtPath<Sprite>(skyPath);
                    Background("Dawn exterior - rescue road",sky,new Vector2(-43,89),new Vector2(90,45),-29).AddComponent<DawnRescueBackdrop>();
                    Background("Dawn exterior - western gate",sky,new Vector2(130,89),new Vector2(50,45),-29);
                    var princess=Object.FindFirstObjectByType<PrincessRescue>();princess.transform.position=new Vector3(-42,73.02f,0);princess.unlocked=false;princess.exitPosition=new Vector2(153,80);princess.arrival=new Vector2(-67,75.8f);
                    var old=GameObject.Find("Nero's western seal");if(old!=null)Object.DestroyImmediate(old);
                    var gate=new GameObject("Nero's western seal");int groundLayer=LayerMask.NameToLayer("Ground");gate.layer=groundLayer<0?0:groundLayer;gate.transform.position=new Vector3(161,80,0);var collider=gate.AddComponent<BoxCollider2D>();collider.size=new Vector2(2.5f,6);
                    var map=GameObject.Find("GroundTilemap").GetComponent<Tilemap>();var sprite=map.GetSprite(new Vector3Int(155,76,0));
                    for(int i=0;i<5;i++){var bar=Background("Gilded seal bar",sprite,new Vector2(160+i*.5f,80),new Vector2(.25f,6),10);bar.transform.SetParent(gate.transform,true);bar.GetComponent<SpriteRenderer>().color=new Color(.8f,.55f,.2f);}
                    princess.sealedGate=gate;
                }
                EditorSceneManager.MarkSceneDirty(loaded);EditorSceneManager.SaveScene(loaded);
            }
            EditorSceneManager.OpenScene("Assets/Scenes/Gameplay_Main.unity");AssetDatabase.SaveAssets();
        }
        static GameObject Background(string name,Sprite sprite,Vector2 center,Vector2 size,int order){var go=new GameObject(name);go.transform.position=center;var sr=go.AddComponent<SpriteRenderer>();sr.sprite=sprite;sr.sortingOrder=order;go.transform.localScale=new Vector3(size.x/sprite.bounds.size.x,size.y/sprite.bounds.size.y,1);return go;}
    }
}
