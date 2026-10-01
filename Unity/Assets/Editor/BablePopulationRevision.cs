using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;
using UnityEditor.SceneManagement;
using Babel.Runtime.Characters.Enemies;
using Babel.Runtime.Shop;
using Babel.Runtime.World;
using Bable;
using Object=UnityEngine.Object;
namespace Babel.EditorTools
{
    public static class BablePopulationRevision
    {
        class Platform {public int left,right,y;public int Width=>right-left+1;public Vector2 Middle=>new Vector2((left+right+1)*.5f,y+1);}
        static readonly string[] Names={"Abaddon, Herald of Ruin","Korah, the Earthbound","Azazel, the Exiled Watcher","Bel, the Gilded Idol","Nero, Heir of Nimrod"};
        static List<Platform> Platforms(Tilemap map){var list=new List<Platform>();var bounds=map.cellBounds;for(int y=bounds.yMin;y<bounds.yMax;y++){
            Platform run=null;for(int x=bounds.xMin;x<bounds.xMax;x++){
                bool clear=map.HasTile(new Vector3Int(x,y,0));for(int k=1;k<=4&&clear;k++)clear=!map.HasTile(new Vector3Int(x,y+k,0));
                if(clear){if(run==null)run=new Platform{left=x,right=x,y=y};else run.right=x;}else if(run!=null){if(run.Width>=12)list.Add(run);run=null;}
            }if(run!=null&&run.Width>=12)list.Add(run);
        }return list;}
        static ShopItemDefinition Item(string file,string name,string description,ShopItemEffectType effect,int price,float magnitude,bool once,string icon){
            string path="Assets/Data/Shop/"+file+".asset";var item=AssetDatabase.LoadAssetAtPath<ShopItemDefinition>(path);if(item==null){item=ScriptableObject.CreateInstance<ShopItemDefinition>();AssetDatabase.CreateAsset(item,path);}var so=new SerializedObject(item);
            so.FindProperty("displayName").stringValue=name;so.FindProperty("description").stringValue=description;so.FindProperty("effectType").enumValueIndex=(int)effect;so.FindProperty("price").intValue=price;so.FindProperty("magnitude").floatValue=magnitude;so.FindProperty("oneTimePurchase").boolValue=once;so.FindProperty("icon").objectReferenceValue=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Bable/images/"+icon+".png");so.ApplyModifiedPropertiesWithoutUndo();return item;
        }
        static void Stock(ShopkeeperController shop,ShopItemDefinition[] stock){var so=new SerializedObject(shop);var p=so.FindProperty("stock");p.arraySize=stock.Length;for(int i=0;i<stock.Length;i++)p.GetArrayElementAtIndex(i).objectReferenceValue=stock[i];so.FindProperty("shopMenu").objectReferenceValue=Object.FindFirstObjectByType<ShopMenuController>();so.ApplyModifiedPropertiesWithoutUndo();}
        static void AlignMerchant(ShopkeeperController shop,float floor){shop.transform.position=new Vector3(shop.transform.position.x,floor+.03f,0);var child=shop.transform.Find("Merchant visual");if(child==null)return;var sprite=child.GetComponent<SpriteRenderer>();float scale=2.5f/(1093f/100);child.localScale=Vector3.one*scale;child.localPosition=new Vector3(0,(sprite.sprite.pivot.y-99)/100f*scale,0);sprite.flipX=false;var box=shop.GetComponent<BoxCollider2D>();if(box!=null){box.offset=new Vector2(0,1.1f);box.size=new Vector2(5.5f,3.2f);}}
        [MenuItem("Bable/Revision/Populate Safe Platforms and Shops")]
        public static void Apply()
        {
            if(EditorApplication.isPlaying)throw new Exception("Stop play mode first");
            Directory.CreateDirectory("../reference/revision4");string scenePath="Assets/Scenes/Gameplay_Main.unity";if(!File.Exists("../reference/revision4/Gameplay_Main.before.unity"))File.Copy(scenePath,"../reference/revision4/Gameplay_Main.before.unity");
            var scene=EditorSceneManager.OpenScene(scenePath);var map=GameObject.Find("GroundTilemap").GetComponent<Tilemap>();var platforms=Platforms(map);var bosses=Object.FindObjectsByType<BossBrain>(FindObjectsSortMode.None);
            foreach(var boss in bosses){int i=(int)boss.profile.kind;boss.profile.displayName=Names[i];EditorUtility.SetDirty(boss.profile);boss.name=Names[i];boss.GetComponent<BossEncounter>().title=Names[i];}
            var stock=new[]{
                Item("HealingProvision","Pilgrim's Bread","Restores 3 health. A warm meal before the next trial.",ShopItemEffectType.RestoreHealth,4,3,false,"harvest"),
                Item("ManaProvision","Moonwell Draught","Restores 2 mana for healing and shockwaves.",ShopItemEffectType.RestoreMana,4,2,false,"moon"),
                Item("PilgrimBread","Votive Heart","Permanently increases maximum health by 1.",ShopItemEffectType.MaxHealth,12,1,true,"woman"),
                Item("LanternOil","Sanctuary Oil","Permanently increases maximum mana by 1.",ShopItemEffectType.MaxMana,14,1,true,"sun"),
                Item("PenitentNeedle","Penitent's Needle","Permanently increases weapon damage by 20%.",ShopItemEffectType.DamageBoost,18,.2f,true,"man"),
                Item("QuickSilver","Wayfarer's Ring","Reduces the time between sword attacks by 10%.",ShopItemEffectType.AttackSpeed,16,.9f,true,"rings"),
                Item("LongReach","Oath of Reach","Increases the reach of the blade by 15%.",ShopItemEffectType.AttackRange,16,1.15f,true,"flight")};
            var previous=GameObject.Find("Campaign population");if(previous!=null)Object.DestroyImmediate(previous);
            foreach(var enemy in Object.FindObjectsByType<EnemyControllerBase>(FindObjectsSortMode.None))if(!(enemy is BossBrain))Object.DestroyImmediate(enemy.gameObject);
            foreach(var shop in Object.FindObjectsByType<ShopkeeperController>(FindObjectsSortMode.None))Object.DestroyImmediate(shop.gameObject);
            var root=new GameObject("Campaign population");var log=new List<string>();var merchantPoints=new List<Vector2>();
            var merchantPrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/World/Shopkeeper.prefab");
            foreach(var wish in new[]{new Vector2(10,-11),new Vector2(113,-39),new Vector2(230,-37),new Vector2(353,27),new Vector2(290,53)}){
                var p=platforms.Where(v=>!bosses.Any(b=>b.InArena(v.Middle+Vector2.up))).OrderBy(v=>(v.Middle-wish).sqrMagnitude).First();float x=Mathf.Clamp(wish.x,p.left+3,p.right-3);
                var go=(GameObject)PrefabUtility.InstantiatePrefab(merchantPrefab);go.transform.SetParent(root.transform);go.name="Wayfarer merchant "+(merchantPoints.Count+1);go.transform.position=new Vector3(x,p.y+1,0);var shop=go.GetComponent<ShopkeeperController>();Stock(shop,stock);AlignMerchant(shop,p.y+1);merchantPoints.Add(new Vector2(x,p.y+1));log.Add("MERCHANT "+go.transform.position);
            }
            var prefabs=new[]{"MeleeEnemy","ShieldEnemy","RangedEnemy","GiantEnemy"}.Select(n=>AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Characters/Enemies/"+n+".prefab")).ToArray();
            int count=0,coins=0;var random=new System.Random(731);var coinPrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/World/CoinPickup.prefab");
            foreach(var p in platforms.Where(p=>p.y<80&&p.left>=-19)){
                int amount=Mathf.Clamp(p.Width/24,1,3);for(int n=0;n<amount;n++){
                    float x=Mathf.Lerp(p.left+4,p.right-4,(n+.5f)/amount);Vector2 feet=new Vector2(x,p.y+1);
                    if(bosses.Any(b=>b.InArena(feet+Vector2.up))||merchantPoints.Any(m=>(m-feet).sqrMagnitude<64)||feet.sqrMagnitude<180||(x>169&&x<201&&p.y==12))continue;
                    int difficulty=p.y<0?(x<145?0:x<260?1:2):x<155?0:3;int kind=difficulty==0?0:random.Next(0,difficulty+1);
                    var go=(GameObject)PrefabUtility.InstantiatePrefab(prefabs[kind]);go.transform.SetParent(root.transform);go.name="Patrol "+(++count)+" - "+prefabs[kind].name;
                    var box=go.GetComponent<BoxCollider2D>();float footOffset=(box.offset.y-box.size.y*.5f)*go.transform.localScale.y;go.transform.position=new Vector3(x,p.y+1-footOffset+.05f,0);
                    var so=new SerializedObject(go.GetComponent<EnemyControllerBase>());so.FindProperty("patrolDistance").floatValue=Mathf.Min(5,Mathf.Min(x-p.left-2,p.right-x-2));so.ApplyModifiedPropertiesWithoutUndo();log.Add("ENEMY "+go.name+" "+go.transform.position);
                }
                if(p.y>77)continue;int cluster=Mathf.Clamp(p.Width/12,2,6);for(int k=0;k<cluster;k++){
                    float x=Mathf.Lerp(p.left+3,p.right-3,(k+.5f)/cluster);Vector2 pos=new Vector2(x,p.y+2.1f);
                    if(bosses.Any(b=>b.InArena(pos))||(x>180&&x<196&&p.y==12))continue;
                    var go=(GameObject)PrefabUtility.InstantiatePrefab(coinPrefab);go.transform.SetParent(root.transform);go.name="Wayfarer gold "+(++coins);go.transform.position=pos;go.GetComponent<CollectiblePickup>().ConfigureGoldAmount(1);
                }
            }
            if(Object.FindFirstObjectByType<ScriptedBridgeCollapse>()==null){var go=new GameObject("Entry bridge - scripted collapse");go.transform.position=new Vector3(188.5f,14.3f,0);var trigger=go.AddComponent<BoxCollider2D>();trigger.isTrigger=true;trigger.size=new Vector2(4,2.5f);go.AddComponent<ScriptedBridgeCollapse>();}
            log.Add("TOTAL enemies="+count+" coins="+coins+" merchants="+merchantPoints.Count);File.WriteAllLines("../reference/revision4/population.txt",log);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            for(int i=0;i<5;i++){
                string path="Assets/Prefabs/Bosses/Boss_"+(i+1)+".prefab";var prefab=PrefabUtility.LoadPrefabContents(path);prefab.GetComponent<BossEncounter>().title=Names[i];PrefabUtility.SaveAsPrefabAsset(prefab,path);PrefabUtility.UnloadPrefabContents(prefab);
                var practice=EditorSceneManager.OpenScene("Assets/Scenes/Boss_Test_"+(i+1)+".unity");foreach(var b in Object.FindObjectsByType<BossBrain>(FindObjectsSortMode.None)){b.GetComponent<BossEncounter>().title=Names[(int)b.profile.kind];b.name=Names[(int)b.profile.kind];}
                var tm=GameObject.Find("GroundTilemap").GetComponent<Tilemap>();var spots=Platforms(tm);foreach(var keeper in Object.FindObjectsByType<ShopkeeperController>(FindObjectsSortMode.None)){Stock(keeper,stock);var p=spots.OrderBy(v=>(v.Middle-(Vector2)keeper.transform.position).sqrMagnitude).First();AlignMerchant(keeper,p.y+1);}
                EditorSceneManager.SaveScene(practice);
            }
            AssetDatabase.SaveAssets();EditorSceneManager.OpenScene(scenePath);Debug.Log(log.Last());
        }
    }
}
