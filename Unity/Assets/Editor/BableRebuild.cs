using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using Babel.Runtime.Characters.Player;
using Babel.Runtime.Characters.Enemies;
using Babel.Runtime.Combat;
using Babel.Runtime.Core;
using Babel.Runtime.World;
using Babel.Runtime.Runes;
using Babel.Runtime.UI;
using Bable;
using Object = UnityEngine.Object;

namespace Babel.EditorTools
{
    public static partial class BabelSliceScaffolder
    {
        [Serializable] class Frame { public int x,y,width,height; }
        [Serializable] class ActionData { public string name; public float fps; public int[] frames; }
        [Serializable] class Sheet { public Frame[] frames; public ActionData[] actions; }
        [Serializable] class Crop { public string name;public int x,y,width,height; }
        [Serializable] class CropList { public Crop[] items; }
        static Dictionary<string,RuntimeAnimatorController> animators = new();
        static Dictionary<string,Sprite[]> frames = new();
        const string Original = "Assets/Resources/Bable/";

        public static void RebuildLegacyDraft()
        {
            EnsureFolders(); EnsureFolder("Assets/Art/NativeAnimations"); EnsureFolder("Assets/Resources/Bable/Runes");
            ImportOriginalAssets();
            var playerDef = CreatePlayerDefinition();
            var pso=new SerializedObject(playerDef); pso.FindProperty("crystalDashSpeed").floatValue=26; pso.FindProperty("crystalDashDuration").floatValue=.7f; pso.ApplyModifiedPropertiesWithoutUndo();
            var defs=CreateEnemyDefinitions(); var runes=CreateRuneDefinitions();
            foreach(var rune in runes) { string path=AssetDatabase.GetAssetPath(rune); string target=Original+"Runes/"+Path.GetFileName(path); if(path!=target && AssetDatabase.LoadAssetAtPath<RuneDefinition>(target)==null) AssetDatabase.CopyAsset(path,target); }
            runes=AssetDatabase.FindAssets("t:RuneDefinition",new[]{Original+"Runes"}).Select(x=>AssetDatabase.LoadAssetAtPath<RuneDefinition>(AssetDatabase.GUIDToAssetPath(x))).ToList();
            var white=CreateWhiteTileAsset(); var playerPrefab=CreatePlayerPrefab(playerDef); var coin=CreateCoinPickupPrefab();
            var enemies=CreateEnemyPrefabs(defs,coin); var checkpoint=CreateCheckpointPrefab(); var runePrefab=CreateRunePickupPrefab(runes[0]); var shop=CreateShopkeeperPrefab(CreateShopItems());
            SkinPrefab(playerPrefab,"chrac",true);
            string[] sheets={"enemy1","enemy2","enemy3","enemy4","boss1","boss2","bossfinal","enemy4"};
            int k=0; foreach(var entry in enemies) SkinPrefab(entry.Value,sheets[k++],false);
            SkinStaticPrefab(coin,"star",.48f); SkinStaticPrefab(checkpoint,"hoxi",1.3f); SkinStaticPrefab(shop,"MaoQi",1.9f);
            var data=NewTowerLayout();
            CreateGameplayScene(data,white,playerPrefab,enemies,checkpoint,runePrefab,runes,coin,shop);
            var gameRoot=GameObject.Find("GameRoot");
            var sessionSo=new SerializedObject(gameRoot.GetComponent<GameSession>()); sessionSo.FindProperty("defaultRespawnPoint").vector2Value=data.RecommendedSpawn; sessionSo.ApplyModifiedPropertiesWithoutUndo();
            var bootSo=new SerializedObject(gameRoot.GetComponent<GameBootstrap>()); bootSo.FindProperty("dontDestroyOnLoad").boolValue=false; bootSo.ApplyModifiedPropertiesWithoutUndo();
            gameRoot.AddComponent<BableAudio>(); gameRoot.AddComponent<BableGameUI>();gameRoot.AddComponent<BableWorldGeometry>();
            var hud=GameObject.Find("HUD"); Object.DestroyImmediate(hud.GetComponent<PauseMenuController>()); Object.DestroyImmediate(hud.GetComponent<RuneMenuController>());
            foreach(string n in new[]{"HealthPanel","ManaPanel","GoldPanel","HealthFill","ManaFill","HealthLabel","ManaLabel","GoldLabel","RunePanel","PausePanel"}) {var child=hud.transform.Find(n); if(child!=null) child.gameObject.SetActive(false);}
            var hudScale=hud.GetComponent<CanvasScaler>(); hudScale.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize; hudScale.referenceResolution=new Vector2(1600,900);
            var camera=Camera.main; camera.orthographicSize=7f; camera.backgroundColor=new Color(.035f,.038f,.065f); camera.gameObject.AddComponent<AudioListener>();
            var camSo=new SerializedObject(camera.GetComponent<CameraFollow2D>()); camSo.FindProperty("offset").vector3Value=new Vector3(0,2,-10);camSo.ApplyModifiedPropertiesWithoutUndo();
            PaintTower(data);
            Gate("Prophecy Seal",62,6,AbilityId.Shockwave,true,1,12);
            Gate("Cloister Seal",29,47,AbilityId.CrystalDash,false,1,12);
            AddCheckpoint(checkpoint,38,1); AddCheckpoint(checkpoint,102,1); AddCheckpoint(checkpoint,105,31); AddCheckpoint(checkpoint,60,43); AddCheckpoint(checkpoint,4,43); AddCheckpoint(checkpoint,35,83);
            AddRunesAndScrolls(runes,runePrefab);
            foreach(var enemy in Object.FindObjectsByType<EnemyControllerBase>(FindObjectsSortMode.None))
            {
                var nameplate=enemy.transform.Find("Nameplate");if(nameplate!=null)Object.DestroyImmediate(nameplate.gameObject);
                if(enemy.GetComponent<BossAbilityReward>() != null || enemy is FinalBossController)
                { var boss=enemy.gameObject.AddComponent<BossEncounter>();boss.title=enemy.name.Replace('_',' ').ToUpperInvariant();boss.final=enemy is FinalBossController; }
            }
            AddLabel("PILGRIM'S GATE",0,6); AddLabel("THE PROPHECY",45,8); AddLabel("BURIED TEMPLE",87,7); AddLabel("WALL JUMP ASCENT",112,17);
            AddLabel("THE BELFRY",87,37); AddLabel("DOUBLE JUMP",68,43); AddLabel("CRYSTAL CLOISTER",43,50); AddLabel("SHIFT: CROSS THE VOID",21,47); AddLabel("NERO'S CROWN",53,89);
            Physics2D.gravity=new Vector2(0,-9.81f);
            PlayerSettings.productName="bable"; PlayerSettings.companyName="Yibo Wang"; PlayerSettings.defaultScreenWidth=1600; PlayerSettings.defaultScreenHeight=900; PlayerSettings.fullScreenMode=FullScreenMode.Windowed; PlayerSettings.runInBackground=true;
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(),"Assets/Scenes/Gameplay_Main.unity");
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Scenes/Gameplay_Main.unity",true)};
            AssetDatabase.SaveAssets(); Debug.Log("BABLE_REBUILD_COMPLETE: original artwork, native animations, modular tower, five bosses, eight runes, nine scrolls.");
        }
        static void ImportOriginalAssets()
        {
            foreach(var file in Directory.GetFiles(Original+"images").Where(x=>x.EndsWith(".png")||x.EndsWith(".jpg"))) ImportSprite(file,100);
            foreach(var crop in JsonUtility.FromJson<CropList>(File.ReadAllText(Original+"ui-crops.json")).items)
            {var ti=(TextureImporter)AssetImporter.GetAtPath(Original+"images/"+crop.name+".png");ti.spriteImportMode=SpriteImportMode.Multiple;ti.spritesheet=new[]{new SpriteMetaData{name=crop.name,rect=new Rect(crop.x,crop.y,crop.width,crop.height),pivot=new Vector2(.5f,.5f),alignment=0}};ti.SaveAndReimport();}
            foreach(var file in Directory.GetFiles("Assets/Art/KenneyTinyDungeon/Tiles","*.png")) ImportSprite(file,16);
            foreach(var file in Directory.GetFiles(Original+"animations","*.json"))
            {
                var name=Path.GetFileNameWithoutExtension(file); if(name=="avatar")continue;
                var sheet=JsonUtility.FromJson<Sheet>(File.ReadAllText(file)); var texturePath=Original+"animations/"+name+".png";
                var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
                var importer=(TextureImporter)AssetImporter.GetAtPath(texturePath); importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Multiple;importer.filterMode=FilterMode.Point;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=8192; importer.spritePixelsPerUnit=100;
                importer.GetSourceTextureWidthAndHeight(out int sourceWidth,out int sourceHeight);
                var meta=new SpriteMetaData[sheet.frames.Length];
                for(int i=0;i<meta.Length;i++){var f=sheet.frames[i];meta[i]=new SpriteMetaData{name=$"{name}_{i:D3}",rect=new Rect(f.x,sourceHeight-f.y-f.height,f.width,f.height),pivot=new Vector2(.5f,.5f),alignment=0};}
                importer.spritesheet=meta;importer.SaveAndReimport();
                var sprites=AssetDatabase.LoadAllAssetsAtPath(texturePath).OfType<Sprite>().OrderBy(x=>x.name).ToArray();frames[name]=sprites;
                if(sprites.Length!=sheet.frames.Length)throw new Exception($"{name}: imported {sprites.Length}, expected {sheet.frames.Length} sprite rectangles");
                string ap="Assets/Art/NativeAnimations/"+name+".controller";
                var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(ap);if(controller==null)controller=AnimatorController.CreateAnimatorControllerAtPath(ap);
                var sm=controller.layers[0].stateMachine;foreach(var state in sm.states)sm.RemoveState(state.state);
                foreach(var a in sheet.actions)
                {
                    string cp="Assets/Art/NativeAnimations/"+name+"_"+a.name+".anim";var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(cp);if(clip==null){clip=new AnimationClip();AssetDatabase.CreateAsset(clip,cp);}clip.frameRate=a.fps;
                    var keys=a.frames.Select((f,i)=>new ObjectReferenceKeyframe{time=i/a.fps,value=sprites[f]}).ToList(); keys.Add(new ObjectReferenceKeyframe{time=a.frames.Length/a.fps,value=sprites[a.frames.Last()]});
                    AnimationUtility.SetObjectReferenceCurve(clip,new EditorCurveBinding{path="",type=typeof(SpriteRenderer),propertyName="m_Sprite"},keys.ToArray());var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=!a.name.Contains("dead");AnimationUtility.SetAnimationClipSettings(clip,settings);
                    var state=sm.AddState(a.name);state.motion=clip;if(a.name=="rightidle")sm.defaultState=state;
                }
                animators[name]=controller;
            }
        }
        static void ImportSprite(string file,float ppu)
        {
            var importer=AssetImporter.GetAtPath(file.Replace('\\','/')) as TextureImporter;if(importer==null)return;importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.spritePixelsPerUnit=ppu;importer.filterMode=FilterMode.Point;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=8192;importer.alphaIsTransparency=true;importer.SaveAndReimport();
        }
        static Sprite OriginalSprite(string name)=>AssetDatabase.LoadAssetAtPath<Sprite>(Original+"images/"+name+".png") ?? AssetDatabase.LoadAssetAtPath<Sprite>(Original+"images/"+name+".jpg");
        static void SkinPrefab(GameObject prefab,string sheet,bool player)
        {
            var path=AssetDatabase.GetAssetPath(prefab);var root=PrefabUtility.LoadPrefabContents(path);root.GetComponent<SpriteRenderer>().enabled=false;
            var visual=new GameObject("Original Artwork");visual.transform.SetParent(root.transform,false);var sr=visual.AddComponent<SpriteRenderer>();sr.sprite=frames[sheet][0];sr.sortingOrder=10;
            float h=root.transform.localScale.y*1.35f;float s=h/sr.sprite.bounds.size.y;visual.transform.localScale=new Vector3(s/root.transform.localScale.x,s/root.transform.localScale.y,1);
            var animator=visual.AddComponent<Animator>();animator.runtimeAnimatorController=animators[sheet];var presenter=root.AddComponent<CharacterPresentation>();presenter.animator=animator;presenter.player=player;
            PrefabUtility.SaveAsPrefabAsset(root,path);PrefabUtility.UnloadPrefabContents(root);
        }
        static void SkinStaticPrefab(GameObject prefab,string art,float height)
        {
            var path=AssetDatabase.GetAssetPath(prefab);var root=PrefabUtility.LoadPrefabContents(path);SkinStatic(root,art,height);PrefabUtility.SaveAsPrefabAsset(root,path);PrefabUtility.UnloadPrefabContents(root);
        }
        static void SkinStatic(GameObject root,string art,float height)
        {
            var sr=root.GetComponent<SpriteRenderer>();sr.sprite=OriginalSprite(art);sr.color=Color.white;sr.sortingOrder=8;if(sr.sprite!=null){float s=height/sr.sprite.bounds.size.y;root.transform.localScale=new Vector3(s,s,1);}
        }
        static LegacySceneData NewTowerLayout()
        {
            var d=new LegacySceneData{RecommendedSpawn=new Vector2(0,2),PlayerStart=new Vector2(0,2)};
            void Fill(int x,int y,int w,int h){for(int i=x;i<x+w;i++)for(int j=y;j<y+h;j++)d.GroundTiles.Add(new Vector3Int(i,j,0));}
            Fill(-10,-3,132,3);Fill(-10,0,2,12);Fill(120,0,2,92);
            Fill(111,5,2,25);Fill(117,0,3,31);Fill(72,28,41,2);
            Fill(66,35,5,1);Fill(60,41,5,1);Fill(30,40,30,2);
            Fill(-2,40,14,2);
            // A real dash gap; no image-based collision. Catch ledges below allow retries.
            Fill(12,31,7,1);Fill(22,31,7,1);Fill(28,34,3,1);Fill(29,37,3,1);
            for(int i=0;i<10;i++)Fill(i%2==0 ? 0:6,46+i*3,5,1);
            Fill(0,79,80,3);Fill(0,82,2,12);Fill(79,82,2,12);
            // Early optional platforms and backtracking rewards.
            Fill(18,2,4,1);Fill(25,5,4,1);Fill(34,2,4,1);Fill(76,3,4,1);
            void E(string id,string binding,float x,float y)=>d.EnemySpawns.Add(new LegacyEnemySpawn{Id=id,BindingType=binding,Position=new Vector2(x,y)});
            E("gatekeeper","EnemyPrefabBinding",17,1);E("shield_sentinel","Enemy1PrefabBinding",31,1);E("boneslinger","Enemy2PrefabBinding",73,1);E("temple_guard","Enemy3PrefabBinding",87,1);
            E("shockwave_boss","Enemy4PrefabBinding",49,1);E("burrow_boss","Enemy5PrefabBinding",99,1);E("sky_judicator","Enemy7PrefabBinding",91,31);E("giant_penitent","Enemy3PrefabBinding",43,43);E("final_nero","Enemy6PrefabBinding",58,83);
            return d;
        }
        static Tile TileFor(int n)
        {
            string p=$"Assets/Art/Generated/Dungeon_{n}.asset";var tile=AssetDatabase.LoadAssetAtPath<Tile>(p);if(tile==null){tile=ScriptableObject.CreateInstance<Tile>();AssetDatabase.CreateAsset(tile,p);}tile.sprite=AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Art/KenneyTinyDungeon/Tiles/tile_{n:D4}.png");tile.colliderType=Tile.ColliderType.Grid;EditorUtility.SetDirty(tile);return tile;
        }
        static void PaintTower(LegacySceneData data)
        {
            var ground=GameObject.Find("GroundTilemap").GetComponent<Tilemap>();var top=TileFor(28);var inner=TileFor(40);ground.GetComponent<TilemapRenderer>().sortingOrder=1;
            foreach(var cell in data.GroundTiles) ground.SetTile(cell,data.GroundTiles.Contains(cell+Vector3Int.up)?inner:top);
            var bg=new GameObject("Background Masonry",typeof(Tilemap),typeof(TilemapRenderer));bg.transform.SetParent(ground.transform.parent,false);var tm=bg.GetComponent<Tilemap>();bg.GetComponent<TilemapRenderer>().sortingOrder=-20;tm.color=new Color(.32f,.29f,.37f);
            var brick=TileFor(40);for(int x=-10;x<122;x++)for(int y=-3;y<95;y++)tm.SetTile(new Vector3Int(x,y,0),brick);
            // Individual arches, doors, pillars and lamps remain editable objects.
            for(int level=0;level<4;level++)for(int x=-3;x<120;x+=8)
            {
                int y=level*28+4;
                for(int col=0;col<3;col++){Decor(9+col,x+(col-1)*1.5f,y+2.25f,1.5f,new Color(.6f,.5f,.55f));Decor(21+col,x+(col-1)*1.5f,y+.75f,1.5f,new Color(.55f,.5f,.6f));Decor(33+col,x+(col-1)*1.5f,y-.75f,1.5f,new Color(.55f,.5f,.6f));}
                Decor(29,x+3.2f,y+1,.9f,Color.white);
            }
        }
        static void Decor(int tile,float x,float y,float size,Color color)
        {var go=new GameObject("Dungeon Detail "+tile);go.transform.position=new Vector3(x,y,0);go.transform.localScale=Vector3.one*size;var sr=go.AddComponent<SpriteRenderer>();sr.sprite=AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Art/KenneyTinyDungeon/Tiles/tile_{tile:D4}.png");sr.color=color;sr.sortingOrder=-10;}
        static void Gate(string name,float x,float y,AbilityId ability,bool shock,float w,float h)
        {
            var go=new GameObject(name);go.transform.position=new Vector3(x,y,0);var sr=go.AddComponent<SpriteRenderer>();sr.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Generated/WhiteTile.png");sr.color=new Color(.25f,.8f,.75f,.75f);go.transform.localScale=new Vector3(w,h,1);go.AddComponent<BoxCollider2D>();go.AddComponent<Rigidbody2D>().bodyType=RigidbodyType2D.Static;var gate=go.AddComponent<AbilityGate>();gate.required=ability;gate.shockwaveOnly=shock;
        }
        static void AddCheckpoint(GameObject prefab,float x,float y) {var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab);go.name="Shrine Checkpoint";go.transform.position=new Vector3(x,y,0);AddLabel("CHECKPOINT",x,y+2);}
        static void AddRunesAndScrolls(List<RuneDefinition> runes,GameObject prefab)
        {
            Vector2[] pos={new(5,1),new(27,6),new(58,1),new(80,4),new(107,31),new(66,36),new(9,43),new(31,83)};
            for(int i=0;i<runes.Count;i++){var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab);go.name="Rune "+runes[i].RuneId;go.transform.position=pos[i];go.GetComponent<CollectiblePickup>().ConfigureRune(runes[i]);SkinStatic(go,runes[i].RuneId.ToString().ToLowerInvariant(),.8f);}
            var arts=Directory.GetFiles(Original+"images","hoxi *builders.png").Concat(Directory.GetFiles(Original+"images","hoxi *travellers.png")).OrderBy(x=>x).ToArray();
            Vector2[] storyPos={new(3,1),new(24,1),new(40,1),new(68,1),new(104,1),new(103,31),new(55,43),new(6,64),new(40,83)};
            for(int i=0;i<arts.Length;i++){var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab);go.name="Memory "+i;go.transform.position=storyPos[i%storyPos.Length];go.GetComponent<CollectiblePickup>().ConfigureStory("memory"+i,"Memory recovered","A voice from the tower's past.");SkinStatic(go,"hoxi",1f);go.AddComponent<OriginalScroll>().art=Path.GetFileNameWithoutExtension(arts[i]);}
        }
        static void AddLabel(string text,float x,float y){var go=new GameObject(text);go.transform.position=new Vector3(x,y,0);var tm=go.AddComponent<TMPro.TextMeshPro>();tm.text=text;tm.fontSize=3;tm.alignment=TMPro.TextAlignmentOptions.Center;tm.color=new Color(.85f,.73f,.48f);tm.rectTransform.sizeDelta=new Vector2(20,2);}
        [MenuItem("Bable/Build Windows Player")]
        public static void BuildWindowsPlayer()
        {
            BablePlayerBuilds.Campaign();
        }
    }
}

