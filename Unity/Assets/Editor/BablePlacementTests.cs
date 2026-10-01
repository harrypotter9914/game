using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;
using Babel.EditorTools;
using Babel.Runtime.World;
using Babel.Runtime.Characters.Player;
using Babel.Runtime.Characters.Enemies;
using Babel.Runtime.Combat;
using Babel.Runtime.Core;
using Bable;
using Object=UnityEngine.Object;

public static class BablePlacementTests
{
    static string outputFolder="revision7";
    static List<string> checks=new(),failures=new();
    static void Check(string name,bool pass){checks.Add(name);if(!pass)failures.Add(name);File.WriteAllText("../reference/"+outputFolder+"/campaign-tests.json",JsonUtility.ToJson(new BableVerification.Report{checks=checks.ToArray(),failures=failures.ToArray()},true));Debug.Log("REV7 "+(pass?"PASS ":"FAIL ")+name);}
    public static void Run(string folder="revision7"){outputFolder=folder;Directory.CreateDirectory("../reference/"+folder);checks.Clear();failures.Clear();new GameObject("Documented campaign tests").AddComponent<BableTestHost>().StartCoroutine(Test());}
    static IEnumerator Test()
    {
        BableGameUI.Instance.Begin();yield return new WaitForSeconds(.5f);
        var player=Object.FindFirstObjectByType<PlayerController2D>();var body=player.GetComponent<Rigidbody2D>();var combat=player.GetComponent<PlayerCombatController>();var session=GameSession.Instance;player.SetTestInput(0,0);player.GetComponent<HealthComponent>().Invincible=true;
        foreach(var s in Object.FindObjectsByType<BossSpeech>(FindObjectsSortMode.None))s.enabled=false;
        var enemies=Object.FindObjectsByType<EnemyControllerBase>(FindObjectsSortMode.None);foreach(var e in enemies)e.enabled=false;
        var map=GameObject.Find("GroundTilemap").GetComponent<Tilemap>();var groundCollider=map.GetComponent<CompositeCollider2D>();
        var bosses=enemies.OfType<BossBrain>().OrderBy(b=>(int)b.profile.kind).ToArray();
        Check("Five campaign bosses match July 6 room rectangles",bosses.Length==5&&bosses.All(b=>b.arenaCenter==BablePlacementRevision.Rooms[(int)b.profile.kind].center&&b.arenaSize==BablePlacementRevision.Rooms[(int)b.profile.kind].size));
        Check("No enemies in decorative eyes/mouth or outside roof",enemies.All(e=>!BablePlacementRevision.Forbidden(e.transform.position)));
        Check("No bosses in the bottom narrow maze",bosses.All(b=>!(b.transform.position.x>250&&b.transform.position.y<-63)));
        Check("No ordinary enemies occupy authored boss rooms",enemies.Where(e=>!(e is BossBrain)).All(e=>bosses.All(b=>!b.InArena(e.transform.position))));
        Check("All eighteen source stars persist new art in runtime",Object.FindObjectsByType<CollectiblePickup>(FindObjectsSortMode.None).Where(p=>p.name.StartsWith("hoxi")).Count(p=>p.GetComponentsInChildren<SpriteRenderer>().Count(s=>s.enabled&&s.sprite!=null&&s.sprite.texture.name=="VotiveAtlas")==1)==18);
        Check("Eight runes all remain collectible",Object.FindObjectsByType<CollectiblePickup>(FindObjectsSortMode.None).Count(p=>new SerializedObject(p).FindProperty("pickupKind").enumValueIndex==2)==8);
        Vector2[] targets={new(53,-59.8f),new(301,-59.8f),new(322,-39.8f),new(379,28.2f),new(201,76.2f)};
        for(int i=0;i<5;i++){
            var boss=bosses[i];body.simulated=false;player.transform.position=targets[i];Physics2D.SyncTransforms();boss.enabled=true;
            var states=new HashSet<string>();float start=Time.time;Vector2 min=boss.transform.position,max=min;bool inside=true;int embedded=0;
            while(Time.time-start<12){states.Add(boss.State);Vector2 p=boss.transform.position;min=Vector2.Min(min,p);max=Vector2.Max(max,p);if(!boss.InArena(p))inside=false;
                var collider=boss.GetComponent<Collider2D>();if(collider.enabled&&boss.GetComponent<Rigidbody2D>().simulated){var distance=collider.Distance(groundCollider);if(distance.isOverlapped&&distance.distance<-.15f)embedded++;}
                yield return new WaitForSeconds(.1f);}
            Check(boss.profile.kind+" attacks in the documented campaign room",boss.Engaged&&states.Count>=2);
            Check(boss.profile.kind+" remains within its room",inside);
            Check(boss.profile.kind+" does not embed in solid tiles",embedded<4);
            if(i==1||i==2||i==3)Check(boss.profile.kind+" moves through more than one standing position",(max-min).magnitude>2);
            File.AppendAllText("../reference/"+outputFolder+"/ai-traces.txt",boss.profile.kind+" states="+string.Join(",",states)+" travel="+(max-min)+" embedded="+embedded+"\n");
            boss.enabled=false;boss.ResetEncounter();
        }
        body.simulated=true;body.linearVelocity=Vector2.zero;player.transform.position=new Vector3(392,28.5f,0);player.SetTestInput(0,0);Physics2D.SyncTransforms();yield return new WaitForSeconds(.5f);
        Check("Screenshot corridor approach has solid ground",player.IsGrounded);
        Check("Shockwave cannot be cast before the ability is earned",!combat.PerformShockwave());
        var walls=Object.FindObjectsByType<BreakableWall>(FindObjectsSortMode.None);Check("All new blockage pieces reject melee",walls.Where(w=>w.designReference.Contains("Screenshot 1")).All(w=>!w.CanReceiveDamage(TeamAlignment.Player)));
        session.UnlockAbility(AbilityId.Shockwave);session.RestoreMana(3);int count=walls.Length;float y0=player.transform.position.y;player.SetTestInput(.01f,0,true);yield return new WaitForSeconds(.1f);player.SetTestInput(0,0);
        Check("Player jumps to the upper obstruction",player.transform.position.y>y0+1);Check("Jumping player casts earned Shockwave",combat.PerformShockwave());
        yield return new WaitForSeconds(.17f);File.AppendAllText("../reference/"+outputFolder+"/ai-traces.txt","WAVE origin="+player.transform.position+" facing="+player.FacingSign+" combat="+combat.enabled+" count="+count+"\n");yield return new WaitForSeconds(1);Check("Shockwave destroys actual screenshot blockage tiles",Object.FindObjectsByType<BreakableWall>(FindObjectsSortMode.None).Length<count);
        Check("Bottom support floor survives the attack",Enumerable.Range(395,36).All(x=>map.HasTile(new Vector3Int(x,25,0))));
        BableVerification.Capture(outputFolder+"/shockwave-runtime.png");
        player.SetTestInput(0,0);File.WriteAllText("../reference/"+outputFolder+"/tests-finished.txt",failures.Count+" failures / "+checks.Count+" checks");
    }
}
