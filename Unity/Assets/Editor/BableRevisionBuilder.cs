using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using Babel.Runtime.Core;
using Babel.Runtime.Characters.Player;
using Babel.Runtime.Characters.Enemies;
using Babel.Runtime.Combat;
using Babel.Runtime.World;
using Babel.Runtime.Runes;
using Bable;
using Object=UnityEngine.Object;
namespace Babel.EditorTools
{
    public static partial class BabelSliceScaffolder
    {
        [Serializable] class SourceCell {public int x,y;}
        [Serializable] class SliceFrame {public int x,y,width,height;public float pivotX,pivotY;}
        [Serializable] class SliceItem {public string name;public SliceFrame[] frames;public int referenceHeight;}
        [Serializable] class SliceCatalog {public SliceItem[] items;}
        [Serializable] class SourceObject {public string id,binding; public float x,y;}
        [Serializable] class SourceLayout {public string sha256;public SourceCell[] cells;public SourceObject[] enemies,pickups;}
        static string[] bossNames={"Abaddon, Herald of Ruin","Korah, the Earthbound","Azazel, the Exiled Watcher","Bel, the Gilded Idol","Nero, Heir of Nimrod"};
        static SourceLayout source;
        [MenuItem("Bable/Rebuild Complete Game")]
        public static void RebuildCompleteGame()
        {
            if(EditorApplication.isPlaying)throw new Exception("Stop play mode before rebuilding.");
            EnsureFolders();EnsureFolder("Assets/Data/Bosses");EnsureFolder("Assets/Prefabs/Bosses");EnsureFolder("Assets/Art/NativeAnimations");
            ImportOriginalAssets();
            foreach(var f in Directory.GetFiles(Original+"NewArt","*.png"))ImportSprite(f,100);
            ImportGeneratedBoss("CrystalBishop",4,4);ImportGeneratedBoss("Nero",4,5);
            foreach(var entry in new Dictionary<string,string[]>{
                {"Pilgrim",new[]{"idle","run","jump","attack","upattack","downattack","heal","dead"}},
                {"FirstPenitent",new[]{"idle","run","heavy","quick","shockwave","dead"}},
                {"BuriedOne",new[]{"idle","run","burrow","emerge","attack","dead"}},
                {"SkyJudicator",new[]{"idle","run","jump","attack","special","dead"}},
                {"MeleeGuard",new[]{"idle","run","attack","dead"}},
                {"ShieldGuard",new[]{"idle","run","attack","dead"}},
                {"RangedGuard",new[]{"idle","run","attack","dead"}},
                {"GiantGuard",new[]{"idle","run","attack","dead"}}
            })ImportGeneratedBoss(entry.Key,4,entry.Value.Length,entry.Value);
            var playerDef=CreatePlayerDefinition();var ps=new SerializedObject(playerDef);ps.FindProperty("crystalDashSpeed").floatValue=26;ps.FindProperty("crystalDashDuration").floatValue=.7f;ps.ApplyModifiedPropertiesWithoutUndo();var player=CreatePlayerPrefab(playerDef);SkinPrefab(player,"chrac",true);
            var coin=CreateCoinPickupPrefab();SetRelicPrefab(coin,.5f);
            var enemyDefs=CreateEnemyDefinitions();var enemies=CreateEnemyPrefabs(enemyDefs,coin);
            string[] sheets={"enemy1","enemy2","enemy3","enemy4","boss1","boss2","bossfinal","bossfinal"};int sheetIndex=0;
            foreach(var e in enemies)SkinPrefab(e.Value,sheets[sheetIndex++],false);
            ReplaceGeneratedSkin(player,"Pilgrim",2.1f);
            string[] smallBindings={"EnemyPrefabBinding","Enemy1PrefabBinding","Enemy2PrefabBinding","Enemy3PrefabBinding"};string[] smallSheets={"MeleeGuard","ShieldGuard","RangedGuard","GiantGuard"};for(int i=0;i<4;i++)ReplaceGeneratedSkin(enemies[smallBindings[i]],smallSheets[i],i==3?3.6f:2.4f);
            var checkpoint=CreateCheckpointPrefab();SkinStaticPrefab(checkpoint,"hoxi",1.3f);
            var runes=CreateRuneDefinitions();EnsureFolder(Original+"Runes");
            // Keep one canonical set of rune assets: Resources entries are refreshed in-place.
            foreach(var r in runes){string p=Original+"Runes/"+Path.GetFileName(AssetDatabase.GetAssetPath(r));var old=AssetDatabase.LoadAssetAtPath<RuneDefinition>(p);if(old==null)AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(r),p);else{EditorUtility.CopySerialized(r,old);EditorUtility.SetDirty(old);}}
            runes=Resources.LoadAll<RuneDefinition>("Bable/Runes").ToList();
            var runePrefab=CreateRunePickupPrefab(runes[0]);SetRelicPrefab(runePrefab,.85f);
            var shop=CreateShopkeeperPrefab(CreateShopItems());
            var shopRoot=PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(shop));shopRoot.transform.localScale=Vector3.one;shopRoot.GetComponent<SpriteRenderer>().enabled=false;var merchant=new GameObject("Merchant visual");merchant.transform.SetParent(shopRoot.transform,false);var merchantSprite=merchant.AddComponent<SpriteRenderer>();merchantSprite.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(Original+"NewArt/Merchant.png");merchantSprite.sortingOrder=8;merchant.transform.localScale=Vector3.one*2.5f/merchantSprite.sprite.bounds.size.y;merchant.transform.localPosition=Vector3.up*.2f;PrefabUtility.SaveAsPrefabAsset(shopRoot,AssetDatabase.GetAssetPath(shop));PrefabUtility.UnloadPrefabContents(shopRoot);
            source=JsonUtility.FromJson<SourceLayout>(File.ReadAllText(Original+"original-layout.json"));
            var data=OriginalTower();
            var bosses=new List<GameObject>();
            for(int i=0;i<5;i++)bosses.Add(BuildBossPrefab(i,enemies));
            CreateGameplayScene(data,CreateWhiteTileAsset(),player,enemies,checkpoint,runePrefab,runes,coin,shop);
            SetupInterface(data,true);PaintOriginal(data);
            // July 6 individual room diagrams supersede the rough July 5 arrows.
            Vector2[] preferred={new(28.75f,-29.78f),new(154.25f,-30.03f),new(162.25f,-20.03f),new(189.5f,21),new(95.75f,38.22f)};
            Vector2[] centers=BablePlacementRevision.Rooms.Select(r=>r.center/2).ToArray();
            Vector2[] sizes=BablePlacementRevision.Rooms.Select(r=>r.size/2).ToArray();
            for(int i=0;i<5;i++)
            {
                var p=preferred[i]*2;
                var go=(GameObject)PrefabUtility.InstantiatePrefab(bosses[i]);go.transform.position=p;var brain=go.GetComponent<BossBrain>();brain.arenaCenter=centers[i]*2;brain.arenaSize=sizes[i]*2;
                AddCheckpoint(checkpoint,SafeAnchor(data,centers[i]*2+Vector2.left*(sizes[i].x+3),1).x,SafeAnchor(data,centers[i]*2+Vector2.left*(sizes[i].x+3),1).y);
            }
            AddSourcePickups(data,runes,runePrefab);
            AddOriginalBreakables();
            foreach(var e in Object.FindObjectsByType<EnemyControllerBase>(FindObjectsSortMode.None)){var label=e.transform.Find("Nameplate");if(label!=null)Object.DestroyImmediate(label.gameObject);if(!(e is BossBrain)){var reward=e.GetComponent<BossAbilityReward>();if(reward!=null)Object.DestroyImmediate(reward);}}
            AddLabel("THE FOREST APPROACH",2,-3);AddLabel("THE PROPHECY",62,-43);AddLabel("THE BURIED LABYRINTH",276,-65);AddLabel("THE LAST SUPPER",430,49);AddLabel("NERO'S CROWN",193,108);
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(),"Assets/Scenes/Gameplay_Main.unity");
            for(int i=0;i<5;i++)BuildPractice(i,bosses[i],player,enemies,checkpoint,runePrefab,runes,coin,shop);
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Scenes/Gameplay_Main.unity",true)}.Concat(Enumerable.Range(1,5).Select(i=>new EditorBuildSettingsScene($"Assets/Scenes/Boss_Test_{i}.unity",true))).ToArray();
            PlayerSettings.productName="bable";PlayerSettings.companyName="Yibo Wang";PlayerSettings.defaultScreenWidth=1600;PlayerSettings.defaultScreenHeight=900;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.runInBackground=true;
            AssetDatabase.SaveAssets();EditorSceneManager.OpenScene("Assets/Scenes/Gameplay_Main.unity");Debug.Log("BABLE_REVISION_BUILT: original 7901 source cells, five shared bosses, five practice rooms, explicit scroll IDs.");
        }
        static LegacySceneData OriginalTower()
        {
            var d=new LegacySceneData{PlayerStart=new Vector2(2,2),RecommendedSpawn=new Vector2(2,2)};
            foreach(var c in source.cells)for(int x=0;x<2;x++)for(int y=0;y<2;y++)d.GroundTiles.Add(new Vector3Int(c.x*2-1+x,c.y*2-1+y,0));
            foreach(var e in source.enemies.Where(e=>!e.id.Contains("boss")&&e.id!="final_nero"))d.EnemySpawns.Add(new LegacyEnemySpawn{Id=e.id,BindingType=e.binding,Position=SafeAnchor(d,new Vector2(e.x,-e.y)*2,1)});
            d.RecommendedSpawn=SafeAnchor(d,d.PlayerStart,1);return d;
        }
        static Vector2 SafeAnchor(LegacySceneData d,Vector2 preferred,float halfHeight)
        {
            Vector2 best=preferred;float score=float.MaxValue;
            foreach(var c in d.GroundTiles)
            {
                if(d.GroundTiles.Contains(c+Vector3Int.up)||d.GroundTiles.Contains(c+Vector3Int.up*2)||d.GroundTiles.Contains(c+Vector3Int.up*3)||d.GroundTiles.Contains(c+Vector3Int.up+Vector3Int.right))continue;
                var p=new Vector2(c.x+.5f,c.y+1+halfHeight+.05f);float s=(p-preferred).sqrMagnitude;
                if(s<score){best=p;score=s;}
            }
            return best;
        }
        static void SetupInterface(LegacySceneData d,bool menu)
        {
            var root=GameObject.Find("GameRoot");var so=new SerializedObject(root.GetComponent<GameSession>());so.FindProperty("defaultRespawnPoint").vector2Value=d.RecommendedSpawn;so.ApplyModifiedPropertiesWithoutUndo();
            so=new SerializedObject(root.GetComponent<GameBootstrap>());so.FindProperty("dontDestroyOnLoad").boolValue=false;so.ApplyModifiedPropertiesWithoutUndo();
            root.AddComponent<BableAudio>();root.AddComponent<ScrollJournal>();root.AddComponent<BableGameUI>().startAtMenu=menu;root.AddComponent<BableWorldGeometry>();
            var hud=GameObject.Find("HUD");Object.DestroyImmediate(hud.GetComponent<Babel.Runtime.UI.PauseMenuController>());Object.DestroyImmediate(hud.GetComponent<RuneMenuController>());
            foreach(string n in new[]{"HealthPanel","ManaPanel","GoldPanel","HealthFill","ManaFill","HealthLabel","ManaLabel","GoldLabel","RunePanel","PausePanel"}){var t=hud.transform.Find(n);if(t!=null)t.gameObject.SetActive(false);}
            var camera=Camera.main;camera.orthographicSize=8;camera.backgroundColor=new Color(.025f,.035f,.06f);camera.gameObject.AddComponent<AudioListener>();
            Physics2D.gravity=new Vector2(0,-9.81f);
        }
        static void PaintOriginal(LegacySceneData d)
        {
            var ground=GameObject.Find("GroundTilemap").GetComponent<Tilemap>();ground.GetComponent<TilemapRenderer>().sortingOrder=1;
            var inner=TileFor(40);var top=TileFor(28);
            foreach(var c in d.GroundTiles)ground.SetTile(c,d.GroundTiles.Contains(c+Vector3Int.up)?inner:top);
            foreach(var c in d.GroundTiles){ground.SetTileFlags(c,TileFlags.None);ground.SetColor(c,c.x<155&&c.y>-15?new Color(.46f,.52f,.42f):c.y<-50?new Color(.48f,.5f,.59f):new Color(.63f,.59f,.62f));}
            ground.CompressBounds();
            var obj=new GameObject("Background Masonry",typeof(Tilemap),typeof(TilemapRenderer));obj.transform.SetParent(ground.transform.parent,false);var tm=obj.GetComponent<Tilemap>();obj.GetComponent<TilemapRenderer>().sortingOrder=-20;tm.color=new Color(.25f,.23f,.32f);
            var b=ground.cellBounds;for(int x=b.xMin;x<b.xMax;x++)for(int y=b.yMin;y<b.yMax;y++)
            { if(x<155&&y>-12)continue;tm.SetTile(new Vector3Int(x,y,0),inner); }
            for(int x=b.xMin+4;x<b.xMax;x+=12)for(int y=b.yMin+4;y<b.yMax;y+=10)
            {var c=new Vector3Int(x,y,0);if(d.GroundTiles.Contains(c)||d.GroundTiles.Contains(c+Vector3Int.up*2))continue;if(x<155&&y>-12)continue;Decor(29,x,y,1.1f,new Color(.7f,.5f,.3f));}
            var forest=AssetDatabase.LoadAssetAtPath<Sprite>(Original+"NewArt/ForestBackdrop.png");
            for(int x=b.xMin;x<155;x+=36){var go=new GameObject("Forest distant canopy");go.transform.position=new Vector3(x+18,0,0);var sr=go.AddComponent<SpriteRenderer>();sr.sprite=forest;sr.sortingOrder=-30;go.transform.localScale=new Vector3(36/forest.bounds.size.x,32/forest.bounds.size.y,1);}
            // Architectural decoration is separate from the source collision map.
            for(int x=164;x<b.xMax;x+=16)for(int y=b.yMin+8;y<b.yMax-4;y+=14)
            {var c=new Vector3Int(x,y,0);bool clear=true;for(int dx=-3;dx<=3;dx++)for(int dy=-2;dy<=4;dy++)if(d.GroundTiles.Contains(c+new Vector3Int(dx,dy,0)))clear=false;if(!clear)continue;
                for(int col=0;col<3;col++){Decor(9+col,x+(col-1)*1.5f,y+2.25f,1.5f,new Color(.45f,.4f,.45f));Decor(21+col,x+(col-1)*1.5f,y+.75f,1.5f,new Color(.4f,.4f,.48f));Decor(33+col,x+(col-1)*1.5f,y-.75f,1.5f,new Color(.4f,.4f,.48f));}}
        }
        static void SetRelicPrefab(GameObject prefab,float height){var path=AssetDatabase.GetAssetPath(prefab);var root=PrefabUtility.LoadPrefabContents(path);SetRelic(root,height);PrefabUtility.SaveAsPrefabAsset(root,path);PrefabUtility.UnloadPrefabContents(root);}
        static void SetRelic(GameObject root,float height)
        {
            root.transform.localScale=Vector3.one;root.GetComponent<SpriteRenderer>().enabled=false;
            var old=root.transform.Find("Relic Visual");if(old!=null)Object.DestroyImmediate(old.gameObject);
            var visual=new GameObject("Relic Visual");visual.transform.SetParent(root.transform,false);var sr=visual.AddComponent<SpriteRenderer>();sr.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(Original+"NewArt/RelicStar.png");sr.sortingOrder=8;visual.transform.localScale=Vector3.one*height/sr.sprite.bounds.size.y;
            if(root.TryGetComponent<CircleCollider2D>(out var c))c.radius=height*.65f;
            if(root.GetComponent<RelicPickupVisual>()==null)root.AddComponent<RelicPickupVisual>();
        }
        static void AddSourcePickups(LegacySceneData d,List<RuneDefinition> runes,GameObject prefab)
        {
            foreach(var p in source.pickups)
            {
                var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab);go.name=p.id;go.transform.position=new Vector2(p.x,-p.y)*2;
                var pickup=go.GetComponent<CollectiblePickup>();var tag=p.id.Replace("hoxi","").Trim();
                var rune=runes.FirstOrDefault(r=>r.RuneId.ToString().Equals(tag,StringComparison.OrdinalIgnoreCase));
                if(rune!=null)pickup.ConfigureRune(rune);
                else if(ScrollLibrary.TryGet(p.id,out var scroll)){pickup.ConfigureStory(p.id,scroll.title,"Memory recovered. Open the Journal to read it again.");go.AddComponent<OriginalScroll>().art=scroll.id;}
                else pickup.ConfigureStory("sword","A forgotten blade","The tower's first guardian holds the secret of Shockwave.");
            }
        }
        static GameObject BuildBossPrefab(int index,Dictionary<string,GameObject> enemies)
        {
            string[] bindings={"Enemy4PrefabBinding","Enemy5PrefabBinding","Enemy7PrefabBinding","Enemy6PrefabBinding","Enemy6PrefabBinding"};
            var root=PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(enemies[bindings[index]]));
            var old=root.GetComponent<EnemyControllerBase>();var def=old.Definition;Object.DestroyImmediate(old);
            root.name=bossNames[index];var brain=root.AddComponent<BossBrain>();var so=new SerializedObject(brain);so.FindProperty("definition").objectReferenceValue=def;so.ApplyModifiedPropertiesWithoutUndo();
            string profilePath=$"Assets/Data/Bosses/Boss_{index+1}.asset";var profile=AssetDatabase.LoadAssetAtPath<BossProfile>(profilePath);
            if(profile==null){profile=ScriptableObject.CreateInstance<BossProfile>();profile.kind=(BossKind)index;profile.displayName=bossNames[index];profile.health=new[]{24,20,22,28,44}[index];AssetDatabase.CreateAsset(profile,profilePath);}brain.profile=profile;
            var hp=new SerializedObject(root.GetComponent<HealthComponent>());hp.FindProperty("destroyOnDeath").boolValue=true;hp.ApplyModifiedPropertiesWithoutUndo();
            root.transform.localScale=Vector3.one;
            var coll=root.GetComponent<BoxCollider2D>();if(coll!=null)coll.size=index==1||index==2?new Vector2(.9f,1.8f):new Vector2(1.8f,2.8f);
            var visual=root.GetComponent<CharacterPresentation>();
            string sheet=new[]{"FirstPenitent","BuriedOne","SkyJudicator","CrystalBishop","Nero"}[index];
            if(animators.ContainsKey(sheet))ApplyGeneratedSkin(root,sheet,index==1||index==2?2.8f:4.2f);
            if(index<4)root.AddComponent<BossAbilityReward>().Configure(new[]{AbilityId.Shockwave,AbilityId.WallJump,AbilityId.DoubleJump,AbilityId.CrystalDash}[index],"A new path opens through Babel.");
            var encounter=root.AddComponent<BossEncounter>();encounter.title=bossNames[index];encounter.final=index==4;
            var prefab=PrefabUtility.SaveAsPrefabAsset(root,$"Assets/Prefabs/Bosses/Boss_{index+1}.prefab");PrefabUtility.UnloadPrefabContents(root);return prefab;
        }
        static void BuildPractice(int i,GameObject boss,GameObject player,Dictionary<string,GameObject> enemies,GameObject checkpoint,GameObject rune,List<RuneDefinition> runes,GameObject coin,GameObject shop)
        {
            var d=new LegacySceneData{RecommendedSpawn=new Vector2(-12,2),PlayerStart=new Vector2(-12,2)};
            for(int x=-24;x<=24;x++)for(int y=-3;y<=18;y++)if(y<0||x==-24||x==24||y==18)d.GroundTiles.Add(new Vector3Int(x,y,0));
            CreateGameplayScene(d,CreateWhiteTileAsset(),player,enemies,checkpoint,rune,runes,coin,shop);SetupInterface(d,false);PaintOriginal(d);
            var go=(GameObject)PrefabUtility.InstantiatePrefab(boss);go.transform.position=new Vector3(7,2,0);var brain=go.GetComponent<BossBrain>();brain.arenaCenter=new Vector2(0,8);brain.arenaSize=new Vector2(47,20);go.GetComponent<BossEncounter>().final=false;
            var practice=GameObject.Find("GameRoot").AddComponent<BossPractice>();practice.bossIndex=i+1;
            Camera.main.GetComponent<CameraFollow2D>().enabled=false;Camera.main.transform.position=new Vector3(0,7,-10);Camera.main.orthographicSize=14;
            AddLabel(bossNames[i],0,14);EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(),$"Assets/Scenes/Boss_Test_{i+1}.unity");
        }
        static void AddOriginalBreakables()
        {
            // Cells reconstructed from the outlined blocks in the 7.5 source diagram.
            // Those sensor-like annotations were omitted by the old solid-cell export.
            void Group(string name,int x,int y,int w,int h,bool wave){for(int i=0;i<w;i++)for(int j=0;j<h;j++){var go=new GameObject(name);go.transform.position=new Vector3((x+i)*2,(y+j)*2,0);var sr=go.AddComponent<SpriteRenderer>();sr.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Generated/WhiteTile.png");sr.color=wave?new Color(.3f,.7f,.75f):new Color(.65f,.5f,.3f);go.transform.localScale=new Vector3(2,2,1);go.AddComponent<BoxCollider2D>();var b=go.AddComponent<BreakableWall>();b.requiresShockwave=wave;b.designReference="7.5 level diagram: outlined cell";}}
            Group("Cracked passage / lower-left",35,-30,1,3,true);
            Group("Cracked passage / long corridor",99,-21,1,3,true);
            Group("Cracked passage / lower bridge",111,-24,1,2,true);
            Group("Cracked passage / upper gallery",190,11,1,2,true);
        }
        static Material ChromaMaterial()
        {
            const string p="Assets/Art/Generated/ChromaSprite.mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(p);if(mat==null){mat=new Material(Shader.Find("Bable/Chroma Sprite"));AssetDatabase.CreateAsset(mat,p);}return mat;
        }
        static void ApplyGeneratedSkin(GameObject root,string sheet,float height)
        {
            var p=root.GetComponent<CharacterPresentation>();p.mirrorRightFrames=true;p.animator.runtimeAnimatorController=animators[sheet];var sr=p.animator.GetComponent<SpriteRenderer>();sr.sprite=frames[sheet][0];sr.sharedMaterial=ChromaMaterial();
            var scale=root.transform.localScale;p.animator.transform.localScale=new Vector3(height/sr.sprite.bounds.size.y/scale.x,height/sr.sprite.bounds.size.y/scale.y,1);
            var box=root.GetComponent<BoxCollider2D>();
            if(box!=null)p.animator.transform.localPosition=new Vector3(box.offset.x,box.offset.y-box.size.y*.5f,0);
        }
        static void ReplaceGeneratedSkin(GameObject prefab,string sheet,float height)
        {if(!animators.ContainsKey(sheet))return;string path=AssetDatabase.GetAssetPath(prefab);var root=PrefabUtility.LoadPrefabContents(path);ApplyGeneratedSkin(root,sheet,height);PrefabUtility.SaveAsPrefabAsset(root,path);PrefabUtility.UnloadPrefabContents(root);}
        static void ImportGeneratedBoss(string name,int columns,int rows,string[] actionNames=null)
        {
            string path=Original+"NewArt/"+name+".png";var imp=AssetImporter.GetAtPath(path) as TextureImporter;if(imp==null)return;
            imp.GetSourceTextureWidthAndHeight(out int width,out int height);imp.spriteImportMode=SpriteImportMode.Multiple;imp.filterMode=FilterMode.Point;imp.spritePixelsPerUnit=100;
            var slices=JsonUtility.FromJson<SliceCatalog>(File.ReadAllText(Original+"NewArt/character-slices.json")).items.First(s=>s.name==name);
            var meta=new List<SpriteMetaData>();for(int i=0;i<slices.frames.Length;i++){var f=slices.frames[i];meta.Add(new SpriteMetaData{name=$"{name}_{i:D2}",rect=new Rect(f.x,f.y,f.width,f.height),pivot=new Vector2(.5f,0),alignment=9});}imp.spritesheet=meta.ToArray();
            EditorUtility.SetDirty(imp);
            AssetDatabase.WriteImportSettingsIfDirty(path);
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);
            var sprites=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(s=>s.name).ToArray();frames[name]=sprites;
            string ap="Assets/Art/NativeAnimations/"+name+".controller";var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(ap);if(controller==null)controller=AnimatorController.CreateAnimatorControllerAtPath(ap);var sm=controller.layers[0].stateMachine;foreach(var s in sm.states)sm.RemoveState(s.state);
            string[] actions=actionNames??(rows==5?new[]{"idle","run","attack","cast","dead"}:new[]{"idle","run","cast","dead"});
            for(int row=0;row<rows;row++){string cp=$"Assets/Art/NativeAnimations/{name}_{actions[row]}.anim";var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(cp);if(clip==null){clip=new AnimationClip();AssetDatabase.CreateAsset(clip,cp);}clip.frameRate=7;var keys=Enumerable.Range(0,columns).Select(i=>new ObjectReferenceKeyframe{time=i/7f,value=sprites[row*columns+i]}).ToArray();AnimationUtility.SetObjectReferenceCurve(clip,new EditorCurveBinding{path="",type=typeof(SpriteRenderer),propertyName="m_Sprite"},keys);var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=actions[row]=="idle"||actions[row]=="run";AnimationUtility.SetAnimationClipSettings(clip,settings);var st=sm.AddState("right"+actions[row]);st.motion=clip;if(row==0)sm.defaultState=st;}
            var aliases=new Dictionary<string,string>{{"sufferattack","idle"},{"attack",actions.Contains("heavy")?"heavy":actions.Contains("cast")?"cast":"idle"},{"cast",actions.Contains("shockwave")?"shockwave":actions.Contains("attack")?"attack":"idle"},{"special",actions.Contains("attack")?"attack":"run"}};
            foreach(var alias in aliases)if(!actions.Contains(alias.Key)){var state=sm.AddState("right"+alias.Key);state.motion=sm.states.First(s=>s.state.name=="right"+alias.Value).state.motion;}
            animators[name]=controller;
        }
    }
}
