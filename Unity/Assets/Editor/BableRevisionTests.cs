using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.SceneManagement;
using UnityEditor;
using Bable;
using Babel.Runtime.Core;
using Babel.Runtime.Combat;
using Babel.Runtime.Characters.Player;
using Babel.Runtime.Characters.Enemies;
using Babel.Runtime.Runes;
using Object=UnityEngine.Object;

public sealed class BableRevisionTests
{
    readonly List<string> checks=new(),failures=new();
    [MenuItem("Bable/Verify Revision")]
    public static void Run(){if(!EditorApplication.isPlaying)throw new Exception("Play first");var host=new GameObject("Revision tests").AddComponent<BableTestHost>();Object.DontDestroyOnLoad(host.gameObject);host.StartCoroutine(new BableRevisionTests().RunAll(host));}
    void Check(string name,bool pass){checks.Add(name);if(!pass)failures.Add(name);Debug.Log("REVISION "+(pass?"PASS ":"FAIL ")+name);Save();}
    void Save(){File.WriteAllText(Path.Combine(BableVerification.Output,"revision-validation.json"),JsonUtility.ToJson(new BableVerification.Report{checks=checks.ToArray(),failures=failures.ToArray()},true));}
    [Serializable] class Cell {public int x,y;}
    [Serializable] class Layout {public Cell[] cells;}
    IEnumerator RunAll(BableTestHost host)
    {
        yield return new WaitForSecondsRealtime(1);
        var ui=BableGameUI.Instance;var s=GameSession.Instance;
        Check("Main menu pauses",ui.Mode=="main"&&s.IsPaused);BableVerification.Capture("revision-menu.png");ui.Begin();
        var player=Object.FindFirstObjectByType<PlayerController2D>();var body=player.GetComponent<Rigidbody2D>();var hp=player.GetComponent<HealthComponent>();hp.Invincible=true;player.SetTestInput(0,0);
        var ground=GameObject.Find("GroundTilemap").GetComponent<Tilemap>();
        var layout=JsonUtility.FromJson<Layout>(Resources.Load<TextAsset>("Bable/original-layout").text);
        var expected=new HashSet<Vector3Int>();foreach(var c in layout.cells)for(int x=0;x<2;x++)for(int y=0;y<2;y++)expected.Add(new Vector3Int(c.x*2-1+x,c.y*2-1+y,0));
        int actual=0;foreach(var c in ground.cellBounds.allPositionsWithin)if(ground.HasTile(c))actual++;
        Check("All 7901 original map cells preserved exactly",actual==expected.Count&&expected.All(c=>ground.HasTile(c)));
        Check("Five campaign bosses",Object.FindObjectsByType<BossBrain>(FindObjectsSortMode.None).Length==5);
        foreach(var e in Object.FindObjectsByType<EnemyControllerBase>(FindObjectsSortMode.None))e.enabled=false;
        foreach(var speech in Object.FindObjectsByType<BossSpeech>(FindObjectsSortMode.None))speech.enabled=false;
        yield return new WaitForSeconds(1);Check("Main spawn rests on native collision",player.IsGrounded&&player.transform.position.y>-110);
        BableVerification.Capture("revision-forest.png");
        float x0=player.transform.position.x;player.SetTestInput(1,0);yield return new WaitForSeconds(.4f);player.SetTestInput(0,0);Check("Player fixed-step movement",player.transform.position.x>x0+1);
        float y0=player.transform.position.y;player.SetTestInput(0,0,true);yield return new WaitForSeconds(.2f);Check("Player jumps upward",player.transform.position.y>y0+1);
        ui.Pause();var pos=player.transform.position;yield return new WaitForSecondsRealtime(.3f);Check("Pause freezes physics",(player.transform.position-pos).sqrMagnitude<.001f);ui.Resume();
        var inv=s.RuneInventory;inv.ResetInventory();var runes=Resources.LoadAll<RuneDefinition>("Bable/Runes");foreach(var r in runes)inv.Collect(r);
        Check("Eight rune definitions, four equipped slots",runes.Length==8&&inv.ActiveSlots.Count==4);
        var displaced=inv.ActiveSlots[0];Check("Full inventory replaces selected slot",inv.EquipAt(runes[4],0)&&inv.ActiveSlots[0]==runes[4]&&!inv.IsActive(displaced.RuneId));
        var other=inv.ActiveSlots[1];inv.EquipAt(runes[4],1);Check("Equipped rune exchanges slots without duplicates",inv.ActiveSlots[0]==other&&inv.ActiveSlots[1]==runes[4]&&inv.ActiveSlots.Where(r=>r!=null).Distinct().Count()==4);
        inv.TryDeactivateSlot(1);Check("Unequip selected slot",inv.ActiveSlots[1]==null);inv.EquipAt(runes[4],1);
        ui.Runes();yield return null;BableVerification.Capture("revision-runes.png");ui.Resume();
        var scrolls=Object.FindObjectsByType<OriginalScroll>(FindObjectsSortMode.None);var journal=Object.FindFirstObjectByType<ScrollJournal>();
        Check("Nine unique scroll triggers",scrolls.Length==9&&scrolls.Select(c=>c.art).Distinct().Count()==9);
        foreach(var scroll in scrolls){string id=scroll.art;player.transform.position=scroll.transform.position;body.linearVelocity=Vector2.zero;Physics2D.SyncTransforms();yield return new WaitForSecondsRealtime(.22f);Check("Scroll trigger "+id,journal.recovered.Contains(id)&&ui.CurrentScrollId==id&&ui.Mode=="scroll");ui.Resume();yield return null;}
        Check("Journal rejects duplicate memories",!journal.Recover(ScrollLibrary.Entries[0].id));
        ui.Scroll(ScrollLibrary.Entries[7].id);yield return null;BableVerification.Capture("revision-scroll.png");ui.Resume();
        foreach(AbilityId a in Enum.GetValues(typeof(AbilityId)))s.UnlockAbility(a);
        var wall=Object.FindFirstObjectByType<BreakableWall>();Check("Outlined blocks resist ordinary sword",wall.requiresShockwave&&!wall.CanReceiveDamage(TeamAlignment.Player));wall.HitByShockwave();yield return null;Check("Shockwave removes block collider",wall==null);
        for(int index=1;index<=5;index++)
        {
            SceneManager.LoadScene("Boss_Test_"+index);yield return null;yield return null;yield return null;yield return new WaitForSecondsRealtime(1);
            ui=BableGameUI.Instance;s=GameSession.Instance;player=Object.FindFirstObjectByType<PlayerController2D>();body=player.GetComponent<Rigidbody2D>();hp=player.GetComponent<HealthComponent>();hp.Invincible=true;player.SetTestInput(0,0);
            var boss=Object.FindFirstObjectByType<BossBrain>();
            Check("Boss "+index+" shares editable campaign profile",AssetDatabase.GetAssetPath(boss.profile)=="Assets/Data/Bosses/Boss_"+index+".asset");
            Check("Boss "+index+" animations exist",boss.GetComponent<CharacterPresentation>().animator.runtimeAnimatorController.animationClips.Length>=4);
            Check("Boss "+index+" test room starts unlocked",s.HasAbility(AbilityId.CrystalDash)&&s.RuneInventory.CollectedRunes.Count==8);
            float introTimeout=Time.time+40;while(TowerDialogue.StoryActive&&Time.time<introTimeout)yield return null;
            Check("Boss "+index+" intro releases player and AI",!TowerDialogue.StoryActive&&player.enabled&&!boss.DialogueHold);
            hp.Invincible=true;
            var states=new HashSet<string>();float end=Time.time+14;bool locked=false;Vector2 lockPoint=default;bool stable=true;bool firstWarningEnded=false;
            while(Time.time<end){states.Add(boss.State);if(index==2&&boss.State=="EmergeWarning"&&!firstWarningEnded){if(!locked){locked=true;lockPoint=boss.LockedTarget;player.transform.position=new Vector3(12,2,0);body.linearVelocity=Vector2.zero;}else if((boss.LockedTarget-lockPoint).sqrMagnitude>.01f)stable=false;}if(locked&&boss.State!="EmergeWarning")firstWarningEnded=true;yield return null;}
            Check("Boss "+index+" engages and cycles attacks",boss.Engaged&&states.Count>=2);
            if(index==2){Check("Burrow has dig, underground, warning, recovery",states.Contains("Dig")&&states.Contains("Burrow")&&states.Contains("EmergeWarning")&&states.Contains("Recover"));Check("Burrow target locks before emergence",locked&&stable);}
            if(index==3)Check("Aerial boss uses leap and dive",states.Contains("Leap")&&states.Contains("Dive"));
            if(index==4)Check("Flying boss casts while orbiting",states.Contains("FlightAim")&&states.Contains("Flight")&&boss.GetComponent<Rigidbody2D>().gravityScale==0);
            if(index==5){boss.GetComponent<HealthComponent>().Configure(boss.profile.health,boss.profile.health/2);Check("Nero second phase at half health",boss.PhaseTwo);}
            BableVerification.Capture("revision-boss-"+index+".png");
            ui.Pause();var state=boss.State;var bpos=boss.transform.position;yield return new WaitForSecondsRealtime(.3f);Check("Boss "+index+" pauses attack clock",boss.State==state&&(boss.transform.position-bpos).sqrMagnitude<.001f);ui.Resume();
            boss.enabled=false;player.transform.position=new Vector3(-12,2,0);body.linearVelocity=Vector2.zero;yield return new WaitForSeconds(.5f);
            if(index==1){boss.transform.position=new Vector3(20,2,0);var combat=player.GetComponent<PlayerCombatController>();s.SetMana(0,3);x0=player.transform.position.x;bool dash=combat.PerformCrystalDash();yield return new WaitForSeconds(.75f);Check("Crystal dash travels with zero mana",dash&&s.CurrentMana==0&&Mathf.Abs(player.transform.position.x-x0)>12);Check("Dash restores gravity",body.gravityScale>0);
                player.transform.position=new Vector3(23.4f,6,0);body.linearVelocity=Vector2.down*2;player.SetTestInput(1,0);yield return new WaitForSeconds(.2f);Check("Wall slide on room boundary",player.IsWallSliding);y0=player.transform.position.y;player.SetTestInput(1,0,true);yield return new WaitForSeconds(.08f);Check("Wall jump pushes away and upward",body.linearVelocity.x<0&&player.transform.position.y>y0);player.SetTestInput(0,0);yield return new WaitForSeconds(.35f);player.SetTestInput(0,0,true);yield return new WaitForSeconds(.08f);Check("Double jump restores vertical speed",body.linearVelocity.y>10);
            }
            boss.GetComponent<HealthComponent>().Invincible=false;boss.GetComponent<HealthComponent>().ApplyDamage(999);yield return null;Check("Boss "+index+" death removes combatant",boss==null);Check("Practice victory remains in room",ui.Mode=="play");
        }
        Debug.Log("BABLE_REVISION_TESTS_COMPLETE "+(checks.Count-failures.Count)+"/"+checks.Count);Save();SceneManager.LoadScene("Gameplay_Main");Object.Destroy(host.gameObject);
    }
}
