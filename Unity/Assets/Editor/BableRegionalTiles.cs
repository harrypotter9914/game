using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System.Linq;
namespace Babel.EditorTools
{
    public static class BableRegionalTiles
    {
        const string Folder="Assets/Art/RegionalTiles";
        static readonly string[] Names={"ForestRock","ForestGrass","BlueCatacomb","MossLabyrinth","Terracotta","GoldenGallery","GreyKeep"};
        static readonly int[,] Samples={{0,32,64},{0,96,0},{1,32,32},{2,80,0},{3,32,176},{4,160,32},{3,64,0}};
        [MenuItem("Bable/Art/Restore Regional Tile Palette")]
        public static void Apply()
        {
            Directory.CreateDirectory(Folder);
            var palette=new Tile[Names.Length];
            for(int i=0;i<Names.Length;i++){
                var source=new Texture2D(2,2);ImageConversion.LoadImage(source,File.ReadAllBytes("../reference/revision3/map-sample-"+Samples[i,0]+".png"));
                var tileImage=new Texture2D(32,32,TextureFormat.RGBA32,false);tileImage.SetPixels(source.GetPixels(Samples[i,1],source.height-Samples[i,2]-32,32,32));tileImage.Apply();
                string path=Folder+"/"+Names[i]+".png";File.WriteAllBytes(path,tileImage.EncodeToPNG());Object.DestroyImmediate(source);Object.DestroyImmediate(tileImage);
                AssetDatabase.ImportAsset(path);var imp=(TextureImporter)AssetImporter.GetAtPath(path);imp.textureType=TextureImporterType.Sprite;imp.spritePixelsPerUnit=32;imp.filterMode=FilterMode.Point;imp.textureCompression=TextureImporterCompression.Uncompressed;imp.mipmapEnabled=false;imp.SaveAndReimport();
                string asset=Folder+"/"+Names[i]+".asset";palette[i]=AssetDatabase.LoadAssetAtPath<Tile>(asset);if(palette[i]==null){palette[i]=ScriptableObject.CreateInstance<Tile>();AssetDatabase.CreateAsset(palette[i],asset);}palette[i].sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);palette[i].colliderType=Tile.ColliderType.Grid;EditorUtility.SetDirty(palette[i]);
            }
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/Gameplay_Main.unity");
            var ground=GameObject.Find("GroundTilemap").GetComponent<Tilemap>();
            var cells=new System.Collections.Generic.List<Vector3Int>();foreach(var c in ground.cellBounds.allPositionsWithin)if(ground.HasTile(c))cells.Add(c);
            foreach(var c in cells){int region=Region(c);if(region==0&&!ground.HasTile(c+Vector3Int.up))region=1;ground.SetTile(c,palette[region]);ground.SetTileFlags(c,TileFlags.None);ground.SetColor(c,Color.white);}
            var back=GameObject.Find("Background Masonry")?.GetComponent<Tilemap>();
            if(back!=null){foreach(var c in back.cellBounds.allPositionsWithin)if(back.HasTile(c)){back.SetTile(c,palette[Region(c)]);back.SetTileFlags(c,TileFlags.None);back.SetColor(c,Color.white);}back.color=new Color(.23f,.21f,.28f);}
            foreach(var wall in Object.FindObjectsByType<Bable.BreakableWall>(FindObjectsSortMode.None)){
                var sr=wall.GetComponent<SpriteRenderer>();sr.sprite=Cracked(Region(ground.WorldToCell(wall.transform.position)));sr.color=Color.white;
            }
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            File.WriteAllText("../reference/revision3/map-palette.txt","Original source layout preserved: "+cells.Count+" solid Unity cells. Reusable 32px crops from original map: "+string.Join(", ",Names)+". Region boundaries reconstructed from original map coordinates; placement diagram still requires separate verification.");
            Debug.Log("BABLE_REGION_TILES: "+cells.Count);
        }
        static int Region(Vector3Int c)
        {
            float px=(c.x/2f+83.983f)*32,py=(96.25f-c.y/2f)*32;
            if(px<4900&&py<3400)return 0;
            if(py>=3400)return px<5800?2:3;
            if(px<6300&&py<2200)return 5;
            if(px>=6300&&py<1800||px>8500)return 6;
            return 4;
        }
        static Sprite Cracked(int region)
        {
            string path=Folder+"/"+Names[region]+"Cracked.png";
            var t=new Texture2D(2,2,TextureFormat.RGBA32,false);ImageConversion.LoadImage(t,File.ReadAllBytes(Folder+"/"+Names[region]+".png"));
            for(int y=0;y<32;y++){
                int x=14+(y/4%2)*3;
                t.SetPixel(x,y,new Color(.07f,.05f,.1f));t.SetPixel(x+1,y,new Color(.08f,.055f,.11f));
                t.SetPixel(x-1,y,new Color(.65f,.55f,.39f));
                if(y>=12&&y<20){int b=x+(y-12);if(b<31)t.SetPixel(b,y,new Color(.08f,.055f,.11f));}
            }
            t.Apply();File.WriteAllBytes(path,t.EncodeToPNG());Object.DestroyImmediate(t);AssetDatabase.ImportAsset(path);
            var imp=(TextureImporter)AssetImporter.GetAtPath(path);imp.textureType=TextureImporterType.Sprite;imp.spritePixelsPerUnit=32;imp.filterMode=FilterMode.Point;imp.mipmapEnabled=false;imp.textureCompression=TextureImporterCompression.Uncompressed;imp.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
