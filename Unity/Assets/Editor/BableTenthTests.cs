using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEngine.Tilemaps;
using Babel.Runtime.Core;
using Babel.Runtime.World;
using Babel.Runtime.Characters.Player;
using Babel.Runtime.Characters.Enemies;
using Babel.Runtime.Combat;
using Bable;
public sealed class BableWaveProbe:MonoBehaviour,IDamageable {public int damage;public TeamAlignment Alignment=>TeamAlignment.Enemy;public bool CanReceiveDamage(TeamAlignment a)=>a==TeamAlignment.Player;public void ReceiveDamage(DamageInfo d){damage++;}}
public static class BableTenthTests
{
    static List<string> checks=new(),failures=new();
    static void Check(string name,bool pass){checks.Add(name);if(!pass)failures.Add(name);File.WriteAllText("../reference/revision10/tests.json",JsonUtility.ToJson(new BableVerification.Report{checks=checks.ToArray(),failures=failures.ToArray()},true));Debug.Log("REV10 "+(pass?"PASS ":"FAIL ")+name);}
    public static void Run(){if(!UnityEditor.EditorApplication.isPlaying)throw new System.Exception("Play Mode required");checks.Clear();failures.Clear();new GameObject("Tenth revision tests").AddComponent<BableTestHost>().StartCoroutine(Test());}
    static void Ready(PlayerCombatController c,string field){typeof(PlayerCombatController).GetField(field,System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(c,0f);}
    static GameObject Block(Vector2 p,Vector2 size,bool destructible){var go=new GameObject("Test blocker");go.transform.position=p;go.AddComponent<BoxCollider2D>().size=size;if(destructible)go.AddComponent<BreakableWall>();return go;}
    static BableWaveProbe Probe(Vector2 p){var go=new GameObject("Wave reach probe");go.transform.position=p;go.AddComponent<CircleCollider2D>().radius=.12f;return go.AddComponent<BableWaveProbe>();}
    static IEnumerator Test(){
        BableGameUI.Instance.Begin();yield return new WaitForSeconds(.4f);
        var player=Object.FindFirstObjectByType<PlayerController2D>();var body=player.GetComponent<Rigidbody2D>();var combat=player.GetComponent<PlayerCombatController>();var hp=player.GetComponent<HealthComponent>();var session=GameSession.Instance;var art=player.GetComponent<CharacterPresentation>();var map=GameObject.Find("GroundTilemap").GetComponent<Tilemap>();player.SetTestInput(0,0);hp.Invincible=true;
        foreach(var s in Object.FindObjectsByType<BossSpeech>(FindObjectsSortMode.None))s.enabled=false;
        foreach(var e in Object.FindObjectsByType<EnemyControllerBase>(FindObjectsSortMode.None))e.enabled=false;
        Check("New campaign begins without a weapon",!session.HasWeapon&&!combat.PerformMeleeAttack());
        yield return new WaitForSeconds(.3f);Check("Unarmed hero actually displays new sprites",art.animator.GetComponent<SpriteRenderer>().sprite.texture.name=="PilgrimUnarmed");BableVerification.Capture("revision10/unarmed-start.png");
        var blade=Object.FindObjectsByType<CollectiblePickup>(FindObjectsSortMode.None).First(p=>p.name=="hoxi Sword");player.transform.position=blade.transform.position;body.linearVelocity=Vector2.zero;Physics2D.SyncTransforms();yield return new WaitForSeconds(.4f);Check("Weapon star triggers acquisition scene",player.GetComponent<WeaponAwakening>().IsRunning);BableVerification.Capture("revision10/weapon-awakening.png");yield return new WaitForSeconds(2.2f);Check("Acquisition unlocks sword and restores controls",session.HasWeapon&&player.enabled&&combat.enabled&&!player.GetComponent<WeaponAwakening>().IsRunning);Check("Armed hero switches back to armed sheet",art.animator.GetComponent<SpriteRenderer>().sprite.texture.name!="PilgrimUnarmed");
        Check("All active pickups use the current atlas",Object.FindObjectsByType<CollectiblePickup>(FindObjectsSortMode.None).All(p=>p.GetComponentsInChildren<SpriteRenderer>().Where(s=>s.enabled).All(s=>s.sprite!=null&&s.sprite.texture.name=="VotiveAtlas")));
        var boss=Object.FindObjectsByType<BossBrain>(FindObjectsSortMode.None).First(b=>b.profile.kind==BossKind.Burrow);
        Check("Wall-jump boss occupies screenshot 1 large chamber",boss.arenaCenter==BableTenthRevision.KorahRoom.center&&boss.arenaSize==BableTenthRevision.KorahRoom.size&&BableTenthRevision.KorahRoom.Contains(boss.transform.position));
        Check("Wall-jump room has no ordinary enemies or summon zones",Object.FindObjectsByType<EnemyControllerBase>(FindObjectsSortMode.None).Where(e=>!(e is BossBrain)).All(e=>!BableTenthRevision.KorahRoom.Contains(e.transform.position))&&Object.FindObjectsByType<SummoningEncounter>(FindObjectsSortMode.None).All(e=>!BableTenthRevision.KorahRoom.Contains(e.feet)));
        body.simulated=false;player.transform.position=new Vector3(267,-57.8f,0);boss.enabled=true;float start=Time.time;bool inside=true;var states=new HashSet<string>();while(Time.time-start<9){inside&=boss.InArena(boss.transform.position);states.Add(boss.State);yield return new WaitForSeconds(.1f);}Check("Moved boss burrows and emerges within its room",inside&&states.Contains("Burrow")&&states.Contains("Recover"));BableVerification.Capture("revision10/korah-correct-room.png");boss.enabled=false;boss.ResetEncounter();
        var bridge=Object.FindFirstObjectByType<ScriptedBridgeCollapse>();Check("Bridge begins intact across the entire crossing",Enumerable.Range(167,60).All(x=>map.HasTile(new Vector3Int(x,11,0))&&map.HasTile(new Vector3Int(x,12,0))));
        body.simulated=true;body.linearVelocity=Vector2.zero;player.transform.position=new Vector3(192,14,0);player.SetTestInput(1,0);Physics2D.SyncTransforms();yield return new WaitForSeconds(1.2f);Check("Walking from left triggers bridge collapse",bridge.HasTriggered);player.SetTestInput(0,0);BableVerification.Capture("revision10/bridge-collapse.png");float bridgeLimit=Time.time+10;while(bridge.IsRunning&&Time.time<bridgeLimit)yield return null;player.SetTestInput(0,0);Check("Bridge opening remains physically destroyed",Enumerable.Range(193,12).All(x=>!map.HasTile(new Vector3Int(x,11,0))&&!map.HasTile(new Vector3Int(x,12,0))));File.AppendAllText("../reference/revision10/traces.txt","BRIDGE "+player.transform.position+" running="+bridge.IsRunning+" enabled="+player.enabled+"\n");Check("Fall scene lands safely and restores control",!bridge.IsRunning&&player.enabled&&Vector2.Distance(player.transform.position,bridge.landing)<4);
        session.UnlockAbility(AbilityId.Shockwave);session.UnlockAbility(AbilityId.CrystalDash);body.simulated=false;
        Vector2 origin=new Vector2(600,10);var directions=new[]{Vector2.right,Vector2.up,Vector2.left,Vector2.down};
        foreach(var dir in directions){
            player.transform.position=origin;body.linearVelocity=Vector2.zero;player.SetTestInput(dir.x,dir.y);yield return null;session.RestoreMana(3);Ready(combat,"shockwaveReadyTime");var wall=Block(origin+dir*2.5f,Mathf.Abs(dir.y)>.5f?new Vector2(6,1):new Vector2(1,6),false);var front=Probe(origin+dir);var behind=Probe(origin+dir*3.5f);Physics2D.SyncTransforms();int mana=session.CurrentMana;
            Check("Cardinal shockwave cast "+dir,combat.PerformShockwave()&&session.CurrentMana==mana-1);yield return new WaitForSeconds(.6f);
            File.AppendAllText("../reference/revision10/traces.txt","WAVE "+dir+" distance="+combat.LastShockwaveDistance+" front="+front.damage+" behind="+behind.damage+"\n");Check("Wave stops at ground in direction "+dir,combat.LastShockwaveAxis==dir&&combat.LastShockwaveDistance>1.8f&&combat.LastShockwaveDistance<2.1f&&front.damage>0&&behind.damage==0);
            Object.Destroy(wall);Object.Destroy(front.gameObject);Object.Destroy(behind.gameObject);yield return null;
            player.transform.position=origin;body.simulated=true;Ready(combat,"dashReadyTime");Check("Cardinal dash starts "+dir,combat.PerformCrystalDash());yield return new WaitForSeconds(.12f);Vector2 travel=(Vector2)player.transform.position-origin;Check("Dash and trail follow chosen axis "+dir,Vector2.Dot(travel,dir)>1&&Mathf.Abs(Vector2.Dot(travel,new Vector2(-dir.y,dir.x)))<.2f);if(dir==Vector2.up)BableVerification.Capture("revision10/upward-dash.png");yield return new WaitForSeconds(.8f);body.simulated=false;
        }
        player.transform.position=origin;player.SetTestInput(1,0);yield return null;var first=Block(origin+Vector2.right*2.5f,new Vector2(1,3),true);var second=Block(origin+Vector2.right*3.5f,new Vector2(1,3),true);Physics2D.SyncTransforms();session.RestoreMana(3);Ready(combat,"shockwaveReadyTime");combat.PerformShockwave();yield return new WaitForSeconds(.6f);Check("Impact breaks first blocking layer without passing through next",first==null&&second!=null);Ready(combat,"shockwaveReadyTime");combat.PerformShockwave();yield return new WaitForSeconds(.6f);Check("Repeated shockwave opens the deeper layer",second==null);
        Check("Eight added passage groups exist",GameObject.Find("Revision 10 ability passages").transform.childCount>=8);
        File.WriteAllText("../reference/revision10/finished.txt",failures.Count+" failures / "+checks.Count+" checks");
    }
}

