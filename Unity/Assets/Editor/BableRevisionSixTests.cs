using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;
using Bable;
using Babel.Runtime.Characters.Player;
using Babel.Runtime.Characters.Enemies;
using Babel.Runtime.Combat;
using Babel.Runtime.Core;
public static class BableRevisionSixTests
{
    static List<string> checks=new List<string>(),failures=new List<string>();
    static void Check(string name,bool pass){checks.Add(name);if(!pass)failures.Add(name);File.WriteAllText("../reference/revision6/gameplay-tests.json",JsonUtility.ToJson(new BableVerification.Report{checks=checks.ToArray(),failures=failures.ToArray()},true));Debug.Log("REV6 "+(pass?"PASS ":"FAIL ")+name);}
    public static void Run(){checks.Clear();failures.Clear();new GameObject("Revision six tests").AddComponent<BableTestHost>().StartCoroutine(Test());}
    static IEnumerator Test()
    {
        BableGameUI.Instance.Begin();yield return null;
        var player=Object.FindFirstObjectByType<PlayerController2D>();var body=player.GetComponent<Rigidbody2D>();player.SetTestInput(0,0);player.GetComponent<HealthComponent>().Invincible=true;
        var rooms=Object.FindObjectsByType<BossRoomBoundary>(FindObjectsSortMode.None);
        var enemies=Object.FindObjectsByType<EnemyControllerBase>(FindObjectsSortMode.None).Where(e=>!(e is BossBrain)).ToArray();
        Check("Five persistent boss exclusion rooms",rooms.Length==5);
        Check("No minion body occupies a boss room",enemies.All(e=>rooms.All(r=>!r.Contains(e.GetComponent<Collider2D>().bounds.center)&&!r.Contains(e.GetComponent<Collider2D>().bounds.min))));
        foreach(var enemy in Object.FindObjectsByType<EnemyControllerBase>(FindObjectsSortMode.None))enemy.enabled=false;
        foreach(var speech in Object.FindObjectsByType<BossSpeech>(FindObjectsSortMode.None))speech.enabled=false;
        var zones=Object.FindObjectsByType<SummoningEncounter>(FindObjectsSortMode.None);
        Check("Twelve safe flat summon sites",zones.Length==12&&zones.All(z=>z.IsSafeSpawn()));
        Check("One, two and three wave encounters exist",zones.Any(z=>z.waves==1)&&zones.Any(z=>z.waves==2)&&zones.Any(z=>z.waves==3));
        var zone=zones.First(z=>z.waves==2&&z.feet.y<0);body.position=zone.feet+new Vector2(-5,.85f);body.linearVelocity=Vector2.zero;Physics2D.SyncTransforms();
        float limit=Time.time+4;while(!zone.IsSummoning&&Time.time<limit)yield return null;
        Check("Approach activates summon telegraph",zone.Triggered&&zone.IsSummoning);
        yield return new WaitForSeconds(.8f);
        Check("Apparition cannot attack or collide before landing",zone.ActiveEnemy!=null&&!zone.ActiveEnemy.enabled&&!zone.ActiveEnemy.GetComponent<Collider2D>().enabled);
        BableVerification.Capture("revision6/summoning.png");
        yield return new WaitForSeconds(1.2f);
        Check("First wave lands before enabling AI",zone.SpawnedCount==1&&zone.ActiveEnemy!=null&&zone.ActiveEnemy.enabled&&zone.ActiveEnemy.GetComponent<Collider2D>().enabled);
        if(zone.ActiveEnemy!=null)zone.ActiveEnemy.GetComponent<HealthComponent>().ApplyDamage(999);
        yield return new WaitForSeconds(4);
        Check("Defeating first enemy creates exactly one reinforcement",zone.SpawnedCount==2&&zone.ActiveEnemy!=null);
        if(zone.ActiveEnemy!=null)zone.ActiveEnemy.GetComponent<HealthComponent>().ApplyDamage(999);
        yield return new WaitForSeconds(2.2f);Check("Finite wave encounter completes",zone.Completed&&zone.SpawnedCount==2);
        foreach(var boss in Object.FindObjectsByType<BossBrain>(FindObjectsSortMode.None)){
            player.transform.position=boss.transform.position+Vector3.left*4;Physics2D.SyncTransforms();boss.FaceDialogueTarget(player.transform);Check(boss.profile.displayName+" faces player left",boss.LookDirection==-1);
            player.transform.position=boss.transform.position+Vector3.right*4;Physics2D.SyncTransforms();boss.FaceDialogueTarget(player.transform);Check(boss.profile.displayName+" faces player right",boss.LookDirection==1);
        }
        body.simulated=false;player.transform.position=new Vector3(10,-9,0);yield return new WaitForSeconds(.8f);
        var merchant=Object.FindObjectsByType<Babel.Runtime.Shop.ShopkeeperController>(FindObjectsSortMode.None).OrderBy(m=>(m.transform.position-player.transform.position).sqrMagnitude).First();
        TowerDialogue.Speak("merchant_low",merchant.transform);yield return null;BableVerification.Capture("revision6/merchant-dialogue.png");
        var panel=GameObject.Find("Gilded speech parchment").GetComponent<RectTransform>();var corners=new Vector3[4];panel.GetWorldCorners(corners);
        float head=merchant.GetComponentsInChildren<SpriteRenderer>().Where(s=>s.enabled).Max(s=>s.bounds.max.y);
        Check("Merchant bubble bottom is above the head",corners[0].y>Camera.main.WorldToScreenPoint(new Vector3(merchant.transform.position.x,head,0)).y);
        var coin=Object.FindObjectsByType<AnimatedTreasure>(FindObjectsSortMode.None).First(a=>a.coin);var sprite=coin.GetComponentsInChildren<SpriteRenderer>().First(s=>s.enabled);
        Check("Enlarged coin remains behind the player",sprite.bounds.size.y>=1.4f&&sprite.bounds.size.y<=1.6f&&sprite.sortingOrder<8);
        var data=JsonUtility.FromJson<TowerDialogue.Script>(Resources.Load<TextAsset>("Bable/Dialogue/lines").text);
        Check("Sixty five English dialogue lines",data.lines.Length==65&&data.lines.Select(l=>l.id).Distinct().Count()==65);
        Check("Every combat taunt ID resolves",Enumerable.Range(1,5).All(b=>Enumerable.Range(1,4).All(i=>data.lines.Any(l=>l.id=="boss"+b+"_taunt"+i)&&data.lines.Any(l=>l.id=="boss"+b+"_attack"+i))));
        var cues=new[]{"sword","jump","dash","shockwave","drink","heal","coin","star","arrow","magic","crystal","summon","burrow","emerge","phase","player_hurt","melee_hurt","shield_hurt","ranged_hurt","giant_hurt","boss1_hurt","boss2_hurt","boss3_hurt","boss4_hurt","boss5_hurt"};
        Check("All core combat sound cues import with audio data",cues.All(c=>{var clip=Resources.Load<AudioClip>("Bable/Sfx/"+c);return clip!=null&&clip.samples>100;}));
        int before=CombatAudio.PlayedCount;CombatAudio.Play("dash",player.transform.position);yield return null;Check("Sound router plays a nearby cue",CombatAudio.PlayedCount==before+1);
        before=CombatAudio.PlayedCount;CombatAudio.Play("phase",player.transform.position+Vector3.right*100);Check("Distant enemies cannot flood audible mix",CombatAudio.PlayedCount==before);
        Debug.Log("REV6_FINISHED "+checks.Count+" checks, "+failures.Count+" failures");
    }
}


