using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using Bable;
using Object=UnityEngine.Object;
public static class BableRevision41 {
    const string Art="Assets/Resources/Bable/NewArt/";
    public static void Apply(){
        if(EditorApplication.isPlaying)throw new Exception("Stop play first");
        Import("NeroSlams41","Nero",new[]{0,466,887},new[]{440,839},new[]{"slamright","slamleft"},140);
        Import("SkyAttacks41","SkyJudicator",new[]{0,427,739,1086},new[]{388,700,1030},new[]{"rising","airside","airdown"},127);
        Import("BossEffects41",null,new[]{0,380,786,1086},new[]{365,737,1015},null,100);
        var scene=EditorSceneManager.GetActiveScene();if(scene.name!="Gameplay_Main")scene=EditorSceneManager.OpenScene("Assets/Scenes/Gameplay_Main.unity");
        var bosses=Object.FindObjectsByType<BossBrain>(FindObjectsSortMode.None).OrderBy(b=>(int)b.profile.kind).ToArray();
        Rect[] rooms={new Rect(49,-61,20,14),new Rect(263,-60,12,16)};
        for(int i=0;i<2;i++){bosses[i].arenaCenter=rooms[i].center;bosses[i].arenaSize=rooms[i].size;Record(bosses[i]);}
        foreach(var p in Object.FindObjectsByType<DefeatedBossPassage>(FindObjectsSortMode.None)){
            foreach(var w in p.walls)if(w!=null){w.gameObject.SetActive(true);Record(w);}
            Object.DestroyImmediate(p);
        }
        var ground=GameObject.Find("GroundTilemap").GetComponent<Tilemap>();
        Gate(ground,"First guardian east exit",new RectInt(69,-61,2,6),"BlueCatacomb");
        Gate(ground,"Return shaft safety floor west",new RectInt(79,-63,6,2),"BlueCatacomb");
        Gate(ground,"Return shaft safety floor east",new RectInt(93,-63,2,2),"BlueCatacomb");
        Gate(ground,"Korah lower crossing",new RectInt(285,-65,2,4),"MossLabyrinth");
        Gate(ground,"Korah east crossing",new RectInt(293,-61,2,6),"MossLabyrinth");
        ground.RefreshAllTiles();Record(ground);
        foreach(var b in Object.FindObjectsByType<BossBrain>(FindObjectsSortMode.None)){b.profile.meleeRange=b.WeaponReach;EditorUtility.SetDirty(b.profile);}
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        File.WriteAllText("../reference/revision41/applied.txt",DateTime.Now.ToString("O"));
    }
    static void Record(Object o){EditorUtility.SetDirty(o);if(PrefabUtility.IsPartOfPrefabInstance(o))PrefabUtility.RecordPrefabInstancePropertyModifications(o);}
    static void Gate(Tilemap tm,string name,RectInt cells,string theme){
        var root=GameObject.Find("Revision 41 shockwave passages")??new GameObject("Revision 41 shockwave passages");
        var old=root.transform.Find(name);if(old!=null)Object.DestroyImmediate(old.gameObject);
        var group=new GameObject(name);group.transform.SetParent(root.transform);
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/RegionalTiles/"+theme+"Cracked.png");
        if(texture==null)throw new Exception("Missing regional fractured masonry: "+theme);
        var quarters=new Sprite[4];
        for(int i=0;i<4;i++){
            string path="Assets/Art/RegionalTiles/QuarterTiles/"+theme+"Cracked"+i+".asset";
            quarters[i]=AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if(quarters[i]==null){quarters[i]=Sprite.Create(texture,new Rect(i%2*texture.width/2,i/2*texture.height/2,texture.width/2,texture.height/2),new Vector2(.5f,.5f),texture.width/2,0,SpriteMeshType.FullRect);quarters[i].name=theme+"Cracked"+i;AssetDatabase.CreateAsset(quarters[i],path);}
        }
        foreach(var c in cells.allPositionsWithin){
            Vector2 centre=new Vector2(c.x+.5f,c.y+.5f);
            foreach(var oldWall in Object.FindObjectsByType<BreakableWall>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                if(oldWall.GetComponent<Collider2D>().bounds.Contains(centre))Object.DestroyImmediate(oldWall.gameObject);
            // Independent stone colliders replace the original tile, so shattering
            // cannot leave an invisible tile/composite collision behind.
            tm.SetTile(new Vector3Int(c.x,c.y,0),null);
            var go=new GameObject(name+" "+c.x+","+c.y);go.transform.SetParent(group.transform);go.transform.position=centre;
            var sr=go.AddComponent<SpriteRenderer>();sr.sprite=quarters[(c.x%2+2)%2+((c.y%2+2)%2)*2];sr.drawMode=SpriteDrawMode.Sliced;sr.size=Vector2.one;sr.sortingOrder=3;
            go.AddComponent<BoxCollider2D>().size=Vector2.one;var w=go.AddComponent<BreakableWall>();w.requiresShockwave=true;w.designReference="User annotated rooms, revision 41";
        }
    }
    static void Import(string sheet,string actor,int[] top,int[] feet,string[] actions,float ppu){
        string path=Art+sheet+".png";AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);
        var imp=(TextureImporter)AssetImporter.GetAtPath(path);imp.GetSourceTextureWidthAndHeight(out int width,out int height);
        // Hand-checked gutters: generation is not assumed to be a perfect grid.
        var meta=new List<SpriteMetaData>();int rows=top.Length-1;
        for(int r=0;r<rows;r++)for(int c=0;c<4;c++){
            int[] edges=sheet=="NeroSlams41"?(r==0?new[]{0,445,862,1328,width}:new[]{0,436,867,1324,width}):sheet=="SkyAttacks41"?(r==0?new[]{0,375,728,1108,width}:r==1?new[]{0,362,707,1113,width}:new[]{0,357,729,1098,width}):new[]{0,width/4,width/2,width*3/4,width};
            int x=edges[c],x2=edges[c+1],y=height-top[r+1],h=top[r+1]-top[r];
            float pivotY=actor==null&&r==0?.5f:(top[r+1]-feet[r])/(float)h;
            float pivotX=((c+.5f)*width/4-x)/(x2-x);
            meta.Add(new SpriteMetaData{name=sheet+"_"+(r*4+c).ToString("D2"),rect=new Rect(x,y,x2-x,h),pivot=new Vector2(pivotX,pivotY),alignment=9});
        }
        imp.textureType=TextureImporterType.Sprite;imp.spriteImportMode=SpriteImportMode.Multiple;imp.spritePixelsPerUnit=ppu;imp.filterMode=FilterMode.Point;imp.mipmapEnabled=false;imp.alphaIsTransparency=true;imp.textureCompression=TextureImporterCompression.Uncompressed;imp.maxTextureSize=4096;imp.spritesheet=meta.ToArray();imp.SaveAndReimport();
        if(actor==null)return;
        var sprites=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(s=>s.name).ToArray();
        var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Art/NativeAnimations/"+actor+".controller");
        for(int row=0;row<rows;row++){
            string cp="Assets/Art/NativeAnimations/"+actor+"_"+actions[row]+".anim";var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(cp);if(clip==null){clip=new AnimationClip();AssetDatabase.CreateAsset(clip,cp);}clip.frameRate=12;
            AnimationUtility.SetObjectReferenceCurve(clip,new EditorCurveBinding{path="",type=typeof(SpriteRenderer),propertyName="m_Sprite"},Enumerable.Range(0,5).Select(i=>new ObjectReferenceKeyframe{time=i/12f,value=sprites[row*4+Mathf.Min(i,3)]}).ToArray());
            var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=false;AnimationUtility.SetAnimationClipSettings(clip,settings);
            var sm=controller.layers[0].stateMachine;var state=sm.states.Select(x=>x.state).FirstOrDefault(x=>x.name=="right"+actions[row])??sm.AddState("right"+actions[row]);state.motion=clip;EditorUtility.SetDirty(clip);
        }EditorUtility.SetDirty(controller);
    }
}
