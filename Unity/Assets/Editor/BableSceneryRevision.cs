using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;
namespace Babel.EditorTools
{
    public static class BableSceneryRevision
    {
        const string Folder="Assets/Art/RegionalTiles";
        static string[] names={"ForestRock","ForestGrass","BlueCatacomb","MossLabyrinth","Terracotta","GoldenGallery","GreyKeep","DawnGrass"};
        static Sprite Import(string path,float ppu=128)
        {
            AssetDatabase.ImportAsset(path);var imp=(TextureImporter)AssetImporter.GetAtPath(path);imp.textureType=TextureImporterType.Sprite;imp.spriteImportMode=SpriteImportMode.Single;imp.spritePixelsPerUnit=ppu;var settings=new TextureImporterSettings();imp.ReadTextureSettings(settings);settings.spriteMeshType=SpriteMeshType.FullRect;imp.SetTextureSettings(settings);imp.filterMode=FilterMode.Point;imp.textureCompression=TextureImporterCompression.Uncompressed;imp.mipmapEnabled=false;imp.alphaIsTransparency=true;imp.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        static Texture2D Read(string path){var t=new Texture2D(2,2,TextureFormat.RGBA32,false);ImageConversion.LoadImage(t,File.ReadAllBytes(path));return t;}
        static Sprite Cut(Texture2D source,RectInt rect,string name,bool trim)
        {
            var pixels=source.GetPixels(rect.x,source.height-rect.y-rect.height,rect.width,rect.height);
            int minX=0,minY=0,maxX=rect.width-1,maxY=rect.height-1;
            if(trim){minX=rect.width;minY=rect.height;maxX=maxY=0;for(int y=0;y<rect.height;y++)for(int x=0;x<rect.width;x++)if(pixels[y*rect.width+x].a>.1f){minX=Mathf.Min(minX,x);maxX=Mathf.Max(maxX,x);minY=Mathf.Min(minY,y);maxY=Mathf.Max(maxY,y);}}
            int w=maxX-minX+1,h=maxY-minY+1;var t=new Texture2D(w,h,TextureFormat.RGBA32,false);var colors=new Color[w*h];for(int y=0;y<h;y++)for(int x=0;x<w;x++)colors[y*w+x]=pixels[(y+minY)*rect.width+x+minX];t.SetPixels(colors);t.Apply();string path=Folder+"/"+name+".png";File.WriteAllBytes(path,t.EncodeToPNG());Object.DestroyImmediate(t);return Import(path,trim?128:w);
        }
        static int Region(Vector3Int c){float px=(c.x/2f+83.983f)*32,py=(96.25f-c.y/2f)*32;if(px<4900&&py<3400)return 0;if(py>=3400)return px<5800?2:3;if(px<6300&&py<2200)return 5;if(px>=6300&&py<1800||px>8500)return 6;return 4;}
        static Sprite[] decor;
        static Tile[,] Quarters()
        {
            var result=new Tile[8,4];Directory.CreateDirectory(Folder+"/QuarterTiles");
            for(int r=0;r<8;r++){var source=Read(Folder+"/"+names[r]+".png");for(int q=0;q<4;q++){int w=source.width/2,h=source.height/2;string name="QuarterTiles/"+names[r]+q;var sprite=Cut(source,new RectInt(q%2*w,q/2*h,w,h),name,false);string path=Folder+"/"+name+".asset";var tile=AssetDatabase.LoadAssetAtPath<Tile>(path);if(tile==null){tile=ScriptableObject.CreateInstance<Tile>();AssetDatabase.CreateAsset(tile,path);}tile.sprite=sprite;tile.colliderType=Tile.ColliderType.Grid;tile.transform=Matrix4x4.Scale(new Vector3(1,1/sprite.bounds.size.y,1));EditorUtility.SetDirty(tile);result[r,q]=tile;}Object.DestroyImmediate(source);}return result;
        }
        static int Quarter(Vector3Int cell,int region){int x=(cell.x%2+2)%2,y=(cell.y%2+2)%2;return x+((region==1||region==7)?0:(1-y)*2);}
        static Sprite Cracked(int region)
        {
            var image=Read(Folder+"/"+names[region]+".png");int w=image.width,h=image.height;
            for(int y=0;y<h;y++){int x=w/2+Mathf.RoundToInt(Mathf.Sin(y*.06f)*w*.06f);for(int d=-2;d<=2;d++)image.SetPixel(x+d,y,d==-2?new Color(.7f,.6f,.43f):new Color(.035f,.025f,.05f));if(y>h/3&&y<h*2/3){int branch=x+(y-h/3)/2;image.SetPixel(Mathf.Min(w-1,branch),y,new Color(.035f,.025f,.05f));}}
            image.Apply();string path=Folder+"/"+names[region]+"Cracked.png";File.WriteAllBytes(path,image.EncodeToPNG());Object.DestroyImmediate(image);return Import(path,w);
        }
        static GameObject Place(Transform parent,string name,Sprite sprite,Vector2 position,float height,int order=-2)
        {
            var go=new GameObject(name);go.transform.SetParent(parent);go.transform.position=position;var sr=go.AddComponent<SpriteRenderer>();sr.sprite=sprite;sr.sortingOrder=order;go.transform.localScale=Vector3.one*height/sprite.bounds.size.y;return go;
        }
        public static void Apply()
        {
            var atlas=Read(Folder+"/Source/RegionalAtlas.png");var tiles=new Tile[8];
            for(int i=0;i<8;i++){
                int x=Mathf.RoundToInt(i%4*atlas.width/4f),y=Mathf.RoundToInt(i/4*atlas.height/2f),w=Mathf.RoundToInt((i%4+1)*atlas.width/4f)-x,h=Mathf.RoundToInt((i/4+1)*atlas.height/2f)-y;
                var sprite=Cut(atlas,new RectInt(x+1,y+1,w-2,h-2),names[i],false);string path=Folder+"/"+names[i]+".asset";tiles[i]=AssetDatabase.LoadAssetAtPath<Tile>(path);if(tiles[i]==null){tiles[i]=ScriptableObject.CreateInstance<Tile>();AssetDatabase.CreateAsset(tiles[i],path);}tiles[i].sprite=sprite;tiles[i].colliderType=Tile.ColliderType.Grid;EditorUtility.SetDirty(tiles[i]);
            }Object.DestroyImmediate(atlas);
            var art=Read(Folder+"/Source/DecorAtlas.png");
            var rects=new[]{new RectInt(140,10,210,545),new RectInt(490,20,310,535),new RectInt(885,15,300,540),new RectInt(1290,20,350,540),new RectInt(60,560,340,330),new RectInt(450,565,440,325),new RectInt(965,565,235,325),new RectInt(1260,595,370,295)};
            decor=new Sprite[8];for(int i=0;i<8;i++)decor[i]=Cut(art,rects[i],"Decor/Detail"+i,true);Object.DestroyImmediate(art);
            var bannerSource=Read(Folder+"/Source/BannerAtlas.png");var banners=new[]{Cut(bannerSource,new RectInt(0,0,bannerSource.width/2,bannerSource.height),"Decor/RedrawnHorseBanner",true),Cut(bannerSource,new RectInt(bannerSource.width/2,0,bannerSource.width/2,bannerSource.height),"Decor/RedrawnSwordBanner",true)};Object.DestroyImmediate(bannerSource);
            var quarters=Quarters();var scene=EditorSceneManager.OpenScene("Assets/Scenes/Gameplay_Main.unity");var map=GameObject.Find("GroundTilemap").GetComponent<Tilemap>();
            foreach(var detail in Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None).Where(s=>s.name.StartsWith("Dungeon Detail ")).ToArray())Object.DestroyImmediate(detail.gameObject);
            var allCells=new List<Vector3Int>();foreach(var cell in map.cellBounds.allPositionsWithin)if(map.HasTile(cell))allCells.Add(cell);var cells=allCells.ToArray();var before=new HashSet<Vector3Int>(cells);
            foreach(var c in cells){int r=Region(c);if(r==0&&!map.HasTile(c+Vector3Int.up))r=c.x<0&&c.y>65?7:1;map.SetTile(c,quarters[r,Quarter(c,r)]);map.SetTileFlags(c,TileFlags.None);map.SetColor(c,Color.white);}
            var back=GameObject.Find("Background Masonry")?.GetComponent<Tilemap>();if(back!=null){foreach(var c in back.cellBounds.allPositionsWithin)if(back.HasTile(c)){int r=Region(c);back.SetTile(c,quarters[r,Quarter(c,r)]);back.SetTileFlags(c,TileFlags.None);back.SetColor(c,Color.white);}back.color=new Color(.34f,.32f,.4f);}
            var cracks=new Dictionary<int,Sprite>();foreach(var wall in Object.FindObjectsByType<Bable.BreakableWall>(FindObjectsSortMode.None)){var sr=wall.GetComponent<SpriteRenderer>();int r=Region(map.WorldToCell(wall.transform.position));if(!cracks.ContainsKey(r))cracks[r]=Cracked(r);sr.sprite=cracks[r];sr.color=new Color(.8f,.73f,.87f);}
            var old=GameObject.Find("Regional scenery revision");if(old!=null)Object.DestroyImmediate(old);var parent=new GameObject("Regional scenery revision").transform;int statues=0,vines=0,flags=0,lamps=0,brackets=0;
            var placed=new List<Vector2>();
            foreach(var c in cells.OrderBy(c=>c.y).ThenBy(c=>c.x)){
                if(c.y>110||map.HasTile(c+Vector3Int.up)||!map.HasTile(c+Vector3Int.left)||!map.HasTile(c+Vector3Int.right))continue;
                bool clear=true;for(int yy=1;yy<=7;yy++)for(int xx=-1;xx<=1;xx++)if(map.HasTile(c+new Vector3Int(xx,yy,0)))clear=false;if(!clear)continue;
                Vector2 feet=(Vector2)map.GetCellCenterWorld(c)+Vector2.up*.5f;int region=Region(c);
                if(placed.Any(p=>Vector2.Distance(p,feet)<18))continue;
                if(region==2){Place(parent,"Crypt memorial statue",decor[statues%2],feet+Vector2.up*2.6f,5.2f);statues++;}
                else if(region==3){Place(parent,"Rooted forest relic",decor[7],feet+Vector2.up*1.1f,2.2f);Place(parent,"Hanging labyrinth vine",decor[2+vines%2],feet+new Vector2(3,5.5f),6);vines++;}
                else if(region==4||region==5||region==6){var flag=Place(parent,"Redrawn heraldic banner",banners[flags%2],feet+Vector2.up*4.2f,3.5f);flag.GetComponent<SpriteRenderer>().color=new Color(.88f,.83f,.85f);flag.AddComponent<Bable.SceneryMotion>().banner=true;flags++;Place(parent,"Castle wall light",decor[6],feet+new Vector2(4,3.4f),2.1f).AddComponent<Bable.SceneryMotion>();lamps++;}
                else if(region==0&&c.y<65){Place(parent,"Forest ground roots",decor[7],feet+Vector2.up*.65f,1.3f);}
                placed.Add(feet);
            }
            // The original isolated footholds retain their exact tile collider geometry.
            foreach(var c in cells.OrderBy(c=>c.y).ThenBy(c=>c.x)){
                bool climbing=c.x>=245&&c.x<=300&&c.y>=-32&&c.y<=25;
                if(c.x<145||c.x>305||c.y< -45||c.y>68||map.HasTile(c+Vector3Int.left)||(!climbing&&map.HasTile(c+Vector3Int.down*2))||map.HasTile(c+Vector3Int.up))continue;
                int width=1;while(width<11&&map.HasTile(c+Vector3Int.right*width)&&!map.HasTile(c+Vector3Int.right*width+Vector3Int.up)&&(climbing||!map.HasTile(c+Vector3Int.right*width+Vector3Int.down*2)))width++;
                if(width>(climbing?10:5)||map.HasTile(c+Vector3Int.right*width))continue;
                var sprite=decor[width<=2?4:5];float height=width*sprite.bounds.size.y/sprite.bounds.size.x;Vector2 top=(Vector2)map.CellToWorld(c)+new Vector2(width*.5f,1);
                // Bracket ledge is below the finial: align its top surface to authored floor.
                var go=Place(parent,"Climbable lamp - original foothold",sprite,top+Vector2.down*(height*.5f-height*.23f),height,2);go.AddComponent<Bable.SceneryMotion>();
                for(int x=0;x<width;x++)for(int yy=0;yy<2;yy++)if(map.HasTile(c+Vector3Int.right*x+Vector3Int.down*yy)){map.SetTileFlags(c+Vector3Int.right*x+Vector3Int.down*yy,TileFlags.None);map.SetColor(c+Vector3Int.right*x+Vector3Int.down*yy,new Color(1,1,1,0));}brackets++;
            }
            int after=0;foreach(var cell in map.cellBounds.allPositionsWithin)if(map.HasTile(cell))after++;if(after!=before.Count)throw new System.Exception("Ground layout changed");
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            File.WriteAllText("../reference/revision6/scenery-report.txt","Ground cells preserved="+after+"; original screenshot crops replaced with new 442px textures. Statues="+statues+" vines="+vines+" banners="+flags+" sconces="+lamps+" original climbable lamp footholds="+brackets+". Banners/statues/vines have no colliders; lamp footholds keep original Tilemap colliders.");
        }
    }
}



