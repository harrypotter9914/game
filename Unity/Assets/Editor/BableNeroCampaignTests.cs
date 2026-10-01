using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Bable;
using Babel.Runtime.Characters.Player;
using Babel.Runtime.Combat;
using Babel.Runtime.Core;
public static class BableNeroCampaignTests
{
    static List<string> checks=new List<string>(),failures=new List<string>();
    static void Check(string name,bool pass){checks.Add(name);if(!pass)failures.Add(name);File.WriteAllText("../reference/revision5/nero-campaign-tests.json",JsonUtility.ToJson(new BableVerification.Report{checks=checks.ToArray(),failures=failures.ToArray()},true));}
    public static void Run(){checks.Clear();failures.Clear();new GameObject("Nero campaign test").AddComponent<BableTestHost>().StartCoroutine(Test());}
    static IEnumerator Test()
    {
        var ui=BableGameUI.Instance;ui.Begin();yield return null;var p=Object.FindFirstObjectByType<PlayerController2D>();var hp=p.GetComponent<HealthComponent>();hp.Invincible=true;p.SetTestInput(0,0);p.transform.position=new Vector3(180,77,0);p.GetComponent<Rigidbody2D>().linearVelocity=Vector2.zero;
        var boss=Object.FindObjectsByType<BossBrain>(FindObjectsSortMode.None).First(b=>b.profile.kind==BossKind.Nero);
        yield return new WaitForSeconds(.8f);Check("Nero starts his main-map opening dialogue",TowerDialogue.StoryActive&&boss.DialogueHold&&TowerDialogue.CurrentId=="boss5_intro");BableVerification.Capture("revision5/nero-introduction.png");
        var guards=Object.FindObjectsByType<Babel.Runtime.Characters.Enemies.EnemyControllerBase>(FindObjectsSortMode.None).Where(e=>!(e is BossBrain)).ToArray();var positions=guards.Select(g=>g.transform.position.x).ToArray();yield return new WaitForSeconds(.4f);Check("Nearby guards stop during opening dialogue",guards.Select((g,i)=>Mathf.Abs(g.transform.position.x-positions[i])<.05f).All(v=>v));
        float deadline=Time.time+40;while(TowerDialogue.StoryActive&&Time.time<deadline)yield return null;
        hp.Invincible=true;yield return new WaitForSeconds(.5f);Check("Main-map player stands inside Nero arena",p.IsGrounded&&boss.InArena(p.transform.position));
        var states=new HashSet<string>();bool stayed=true;deadline=Time.time+8;while(Time.time<deadline){states.Add(boss.State);stayed&=boss.Engaged;yield return null;}
        Check("Nero remains engaged when player stays on original floor",stayed&&boss.Engaged);Check("Nero attacks after main-map dialogue",states.Count>=2&&p.enabled&&!boss.DialogueHold);
        BableVerification.Capture("revision5/nero-combat.png");Debug.Log("NERO_CAMPAIGN_TESTS_FINISHED");
    }
}
