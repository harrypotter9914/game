using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;
using Bable;
using Babel.Runtime.Characters.Player;
using Babel.Runtime.Characters.Enemies;
using Babel.Runtime.Core;
using Babel.Runtime.Combat;
using Babel.Runtime.Shop;
public static class BableRevisionFourTests
{
    static List<string> checks=new List<string>(),failures=new List<string>();
    const BindingFlags Hidden=BindingFlags.NonPublic|BindingFlags.Instance;
    static void Check(string name,bool pass){checks.Add(name);if(!pass)failures.Add(name);File.WriteAllText("../reference/revision4/gameplay-tests.json",JsonUtility.ToJson(new BableVerification.Report{checks=checks.ToArray(),failures=failures.ToArray()},true));Debug.Log("REV4 "+(pass?"PASS ":"FAIL ")+name);}
    public static void Run(){checks.Clear();failures.Clear();new GameObject("Revision four test host").AddComponent<BableTestHost>().StartCoroutine(Test());}
    static IEnumerator Test()
    {
        BableGameUI.Instance.Begin();yield return null;var session=GameSession.Instance;var player=Object.FindFirstObjectByType<PlayerController2D>();var pb=player.GetComponent<Rigidbody2D>();var hp=player.GetComponent<HealthComponent>();hp.Invincible=true;player.SetTestInput(0,0);var saved=player.transform.position;
        var originals=Object.FindObjectsByType<EnemyControllerBase>(FindObjectsSortMode.None);foreach(var e in originals)e.enabled=false;
        Check("Campaign includes at least 35 non-boss enemies",originals.Count(e=>!(e is BossBrain))>=35);
        Check("Five merchants across the campaign",Object.FindObjectsByType<ShopkeeperController>(FindObjectsSortMode.None).Length==5);
        Check("All five boss identities preserve distinct ability profiles",originals.OfType<BossBrain>().Select(b=>b.profile.kind).Distinct().Count()==5);
        var map=GameObject.Find("GroundTilemap").GetComponent<Tilemap>();TileBase tile=null;foreach(var c in map.cellBounds.allPositionsWithin)if(map.HasTile(c)){tile=map.GetTile(c);break;}
        for(int x=1000;x<=1040;x++){map.SetTile(new Vector3Int(x,997,0),tile);map.SetTile(new Vector3Int(x,991,0),tile);}map.GetComponent<TilemapCollider2D>().ProcessTilemapChanges();map.GetComponent<CompositeCollider2D>()?.GenerateGeometry();
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Characters/Enemies/MeleeEnemy.prefab");var enemy=Object.Instantiate(prefab,new Vector3(1006,999,0),Quaternion.identity).GetComponent<EnemyControllerBase>();var ally=Object.Instantiate(prefab,new Vector3(1030,999,0),Quaternion.identity).GetComponent<EnemyControllerBase>();ally.enabled=false;
        pb.position=new Vector2(1012,999);pb.linearVelocity=Vector2.zero;yield return new WaitForSeconds(.4f);var awareness=enemy.GetComponent<PlatformAwareness>();
        Check("Enemy acquires player on continuous same platform",awareness.SameReachablePlatform(player.transform));
        float before=enemy.transform.position.x;yield return new WaitForSeconds(.4f);Check("Locked enemy advances toward player",enemy.transform.position.x>before+.1f);
        pb.simulated=false;player.transform.position=new Vector3(1012,993,0);Physics2D.SyncTransforms();Check("Lower layer cannot trigger distance-only aggro",!awareness.SameReachablePlatform(player.transform));
        player.transform.position=new Vector3(1012,999,0);map.SetTile(new Vector3Int(1010,998,0),tile);map.SetTile(new Vector3Int(1010,999,0),tile);Physics2D.SyncTransforms();Check("Solid wall interrupts reachable-platform aggro",!awareness.SameReachablePlatform(player.transform));map.SetTile(new Vector3Int(1010,998,0),null);map.SetTile(new Vector3Int(1010,999,0),null);
        player.transform.position=new Vector3(1012,1002,0);Physics2D.SyncTransforms();yield return new WaitForSeconds(.15f);Check("Jumping above same platform retains attention",enemy.BehaviourState!="Patrol"&&awareness.SameReachablePlatform(player.transform));
        pb.simulated=true;pb.position=new Vector2(1012,999);yield return new WaitForSeconds(1);int flips=0,last=enemy.LookDirection;for(int i=0;i<30;i++){yield return new WaitForSeconds(.06f);if(enemy.LookDirection!=last)flips++;last=enemy.LookDirection;}
        Check("Stationary target does not cause repeated left-right head shaking",flips<=1);Check("Enemy holds attack distance instead of crossing player",Mathf.Abs(enemy.transform.position.x-player.transform.position.x)>.5f);
        Check("Enemy colliders do not shove one another",Physics2D.GetIgnoreCollision(enemy.GetComponent<Collider2D>(),ally.GetComponent<Collider2D>()));Check("Enemies reject allied damage",!ally.GetComponent<HealthComponent>().CanReceiveDamage(TeamAlignment.Enemy));
        enemy.enabled=false;enemy.GetComponent<Rigidbody2D>().simulated=false;enemy.transform.position=new Vector3(1040.6f,998.8f,0);Physics2D.SyncTransforms();Check("Ledge probe refuses to step into empty space",!awareness.CanStep(1));
        Object.Destroy(enemy.gameObject);Object.Destroy(ally.gameObject);pb.position=saved;pb.linearVelocity=Vector2.zero;yield return new WaitForSeconds(.3f);
        var keeper=Object.FindObjectsByType<ShopkeeperController>(FindObjectsSortMode.None).OrderBy(s=>s.transform.position.x).First();var stock=(ShopItemDefinition[])typeof(ShopkeeperController).GetField("stock",Hidden).GetValue(keeper);var shop=Object.FindFirstObjectByType<ShopMenuController>();
        Check("Seven wares have assigned item artwork",stock.Length==7&&stock.All(i=>i.Icon!=null));
        player.transform.position=keeper.transform.position+new Vector3(-2,1,0);pb.linearVelocity=Vector2.zero;yield return new WaitForSeconds(.4f);var sr=keeper.transform.Find("Merchant visual").GetComponent<SpriteRenderer>();Check("Merchant turns toward customer on the left",sr.flipX);
        BableVerification.Capture("revision4/merchant-grounding.png");player.SetTestInput(1,0);yield return new WaitForSeconds(.18f);BableVerification.Capture("revision4/player-running.png");player.SetTestInput(0,0);session.SetHealth(session.MaxHealth-3,session.MaxHealth);session.AddGold(100);shop.Open(stock);yield return null;BableVerification.Capture("revision4/shop.png");
        int gold=session.CurrentGold,health=session.CurrentHealth;typeof(ShopMenuController).GetMethod("TryBuySelected",Hidden).Invoke(shop,null);Check("Healing provision restores health and deducts listed price",session.CurrentHealth==health+3&&session.CurrentGold==gold-stock[0].Price);
        gold=session.CurrentGold;typeof(ShopMenuController).GetMethod("TryBuySelected",Hidden).Invoke(shop,null);Check("Full health does not waste gold on a provision",session.CurrentGold==gold);
        typeof(ShopMenuController).GetMethod("SelectItem",Hidden).Invoke(shop,new object[]{2});int max=session.MaxHealth;typeof(ShopMenuController).GetMethod("TryBuySelected",Hidden).Invoke(shop,null);Check("Permanent heart upgrade increases maximum health",session.MaxHealth==max+1);gold=session.CurrentGold;shop.Close();shop.Open(stock);typeof(ShopMenuController).GetMethod("SelectItem",Hidden).Invoke(shop,new object[]{2});typeof(ShopMenuController).GetMethod("TryBuySelected",Hidden).Invoke(shop,null);Check("Purchased relic stays sold out on reopening another shop",session.CurrentGold==gold);shop.Close();
        var run=AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Art/NativeAnimations/Pilgrim_run.anim");var binding=AnimationUtility.GetObjectReferenceCurveBindings(run)[0];var keys=AnimationUtility.GetObjectReferenceCurve(run,binding);Check("Run cycle plays eight evenly timed poses without a repeated closure",keys.Length==8&&Mathf.Approximately(run.length,8f/12));
        foreach(string action in new[]{"takeoff","jump","fall","land"})Check("Player has distinct "+action+" animation",AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Art/NativeAnimations/Pilgrim_"+action+".anim")!=null);
        player.transform.position=new Vector3(188.5f,14.2f,0);pb.linearVelocity=Vector2.zero;Physics2D.SyncTransforms();yield return new WaitForSeconds(.5f);var collapse=Object.FindFirstObjectByType<ScriptedBridgeCollapse>();Check("Entry bridge triggers a staged collapse",collapse.HasTriggered&&collapse.IsRunning);BableVerification.Capture("revision4/collapse.png");yield return new WaitForSeconds(1.8f);
        Check("Collapse clears the marked supporting tiles",!map.HasTile(new Vector3Int(188,12,0)));Check("Collapse returns control at the lower route",!collapse.IsRunning&&player.enabled&&Vector2.Distance(player.transform.position,collapse.landing)<2);
        player.transform.position=saved;pb.linearVelocity=Vector2.zero;player.SetTestInput(0,0);foreach(var e in originals)if(e!=null)e.enabled=true;hp.Invincible=false;
        Debug.Log("BABLE_REV4_TESTS_FINISHED "+(checks.Count-failures.Count)+"/"+checks.Count);
    }
}
