using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using Bable;
using Babel.Runtime.Characters.Player;
using Babel.Runtime.Characters.Enemies;
using Babel.Runtime.Combat;
using Babel.Runtime.Core;
using Babel.Runtime.World;
public static class BableTimingTests
{
    static readonly List<string> checks=new List<string>(),failures=new List<string>();
    public static void Run(){checks.Clear();failures.Clear();new GameObject("Timing test host").AddComponent<BableTestHost>().StartCoroutine(Test());}
    static void Check(string label,bool ok){checks.Add(label);if(!ok)failures.Add(label);File.WriteAllText("../reference/revision3/timing-tests.json",JsonUtility.ToJson(new BableVerification.Report{checks=checks.ToArray(),failures=failures.ToArray()},true));Debug.Log("TIMING "+(ok?"PASS ":"FAIL ")+label);}
    static HealthComponent Dummy(Vector2 p){var go=new GameObject("Damage timing target");go.transform.position=p;go.AddComponent<BoxCollider2D>().size=Vector2.one*.4f;var hp=go.AddComponent<HealthComponent>();hp.Configure(100,100);return hp;}
    static IEnumerator Test()
    {
        var ui=BableGameUI.Instance;ui.Begin();yield return null;
        var player=Object.FindFirstObjectByType<PlayerController2D>();var combat=player.GetComponent<PlayerCombatController>();var body=player.GetComponent<Rigidbody2D>();var session=GameSession.Instance;
        foreach(var e in Object.FindObjectsByType<EnemyControllerBase>(FindObjectsSortMode.None))e.enabled=false;
        Vector3 saved=player.transform.position;float gravity=body.gravityScale;body.gravityScale=0;body.simulated=false;player.transform.position=new Vector3(1000,1000,0);player.SetTestInput(1,0);yield return null;player.SetTestInput(0,0);
        var near=Dummy((Vector2)player.transform.position+Vector2.right*player.Definition.MeleeRange*.65f);Physics2D.SyncTransforms();
        Check("Melee begins without premature damage",combat.PerformMeleeAttack()&&near.CurrentHealth==100);
        yield return new WaitForSeconds(.22f);Check("Melee active animation frame hits nearby target",near.CurrentHealth<100);
        int health=near.CurrentHealth;yield return new WaitForSeconds(.5f);Check("One swing cannot repeatedly damage one target",near.CurrentHealth==health);Object.Destroy(near.gameObject);
        session.UnlockAbility(AbilityId.Shockwave);session.SetMana(3,3);
        var far=Dummy((Vector2)player.transform.position+Vector2.right*player.Definition.ShockwaveRange*.9f);Physics2D.SyncTransforms();
        Check("Shockwave does not hit its endpoint on input",combat.PerformShockwave()&&far.CurrentHealth==100);
        yield return new WaitForSeconds(.2f);Check("Distant target survives formation stage",far.CurrentHealth==100);
        yield return new WaitForSeconds(1.7f);Check("Moving shockwave reaches and damages endpoint",far.CurrentHealth<100);Object.Destroy(far.gameObject);
        session.UnlockAbility(AbilityId.CrystalDash);body.simulated=true;body.gravityScale=0;player.transform.position=new Vector3(1000,1000,0);body.linearVelocity=Vector2.zero;
        var wall=new GameObject("Dash blocking wall");wall.transform.position=new Vector3(1003,1000,0);wall.AddComponent<BoxCollider2D>().size=new Vector2(1,8);
        var beyond=Dummy(new Vector2(1006,1000));Physics2D.SyncTransforms();combat.PerformCrystalDash();yield return new WaitForSeconds(player.Definition.CrystalDashDuration+.2f);
        Check("Dash stops at physical wall",player.transform.position.x<1003);
        Check("Dash cannot damage target behind blocking wall",beyond.CurrentHealth==100);Object.Destroy(wall);Object.Destroy(beyond.gameObject);
        body.gravityScale=gravity;player.transform.position=saved;body.linearVelocity=Vector2.zero;yield return new WaitForSeconds(.6f);
        var mask=Camera.main.GetComponent<PassageVisibility>();Check("Camera has upper and lower current-passage limits",mask!=null&&mask.VisibleBottom<player.transform.position.y&&mask.VisibleTop>player.transform.position.y);
        BableVerification.Capture("revision3/current-passage.png");
        Check("Four VFX families each have formation and dissolve frames",Resources.LoadAll<Sprite>("Bable/NewArt/CombatVfxAtlas").Length==16);
        Check("TMP essential font imported",TMPro.TMP_Settings.defaultFontAsset!=null);
        var ground=GameObject.Find("GroundTilemap").GetComponent<UnityEngine.Tilemaps.Tilemap>();
        UnityEngine.Tilemaps.TileBase tile=null;foreach(var c in ground.cellBounds.allPositionsWithin)if(ground.HasTile(c)){tile=ground.GetTile(c);break;}
        for(int x=1000;x<=1030;x++)for(int y=998;y<=1010;y++)if(x==1000||x==1030||y==998||y==1010)ground.SetTile(new Vector3Int(x,y,0),tile);
        body.simulated=false;player.transform.position=new Vector3(1002,1000,0);yield return new WaitForSeconds(1.3f);
        var camera=Camera.main;float halfWidth=camera.orthographicSize*camera.aspect;
        Check("Left wall keeps viewport in room instead of centering player",camera.transform.position.x-halfWidth>=999.5f&&camera.WorldToViewportPoint(player.transform.position).x<.3f);
        Check("Viewport balances top and bottom room boundaries",camera.transform.position.y-camera.orthographicSize>=996f&&camera.transform.position.y+camera.orthographicSize<=1011.5f);
        BableVerification.Capture("revision3/camera-left-wall-test.png");
        player.transform.position=new Vector3(1028,1000,0);yield return new WaitForSeconds(1.3f);halfWidth=camera.orthographicSize*camera.aspect;
        Check("Right wall keeps viewport in room instead of centering player",camera.transform.position.x+halfWidth<=1031.5f&&camera.WorldToViewportPoint(player.transform.position).x>.7f);
        BableVerification.Capture("revision3/camera-right-wall-test.png");
        body.simulated=true;player.transform.position=saved;body.linearVelocity=Vector2.zero;
        var princess=Object.FindFirstObjectByType<PrincessRescue>();BossBrain final=null;
        foreach(var boss in Object.FindObjectsByType<BossBrain>(FindObjectsSortMode.None))if(boss.profile.kind==BossKind.Nero)final=boss;
        if(princess!=null&&final!=null){
            final.GetComponent<HealthComponent>().Invincible=false;final.GetComponent<HealthComponent>().ApplyDamage(999);yield return null;
            Check("Defeating Nero unlocks princess without skipping rescue",princess.unlocked&&ui.Mode=="play");
            player.transform.position=new Vector3(princess.exitPosition.x,princess.exitPosition.y,0);yield return new WaitForSeconds(2.5f);
            player.transform.position=princess.transform.position+new Vector3(-2,1.5f,0);yield return new WaitForSeconds(2);
            Check("Reaching princess begins voiced rescue before victory",princess.Arrived&&princess.IsRunning&&ui.Mode=="play");
        }else Check("Princess rescue encounter exists",false);
        Debug.Log("BABLE_TIMING_TESTS_FINISHED");
    }
}
