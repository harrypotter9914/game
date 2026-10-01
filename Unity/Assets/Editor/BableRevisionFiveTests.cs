using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Bable;
using Babel.Runtime.Characters.Player;
using Babel.Runtime.Characters.Enemies;
using Babel.Runtime.Combat;
using Babel.Runtime.Core;
using Babel.Runtime.World;
public static class BableRevisionFiveTests
{
    static List<string> checks=new List<string>(),failures=new List<string>();
    static void Check(string name,bool pass){checks.Add(name);if(!pass)failures.Add(name);File.WriteAllText("../reference/revision6/end-flow-tests.json",JsonUtility.ToJson(new BableVerification.Report{checks=checks.ToArray(),failures=failures.ToArray()},true));Debug.Log("REV5 "+(pass?"PASS ":"FAIL ")+name);}
    public static void Run(){checks.Clear();failures.Clear();new GameObject("Revision five tests").AddComponent<BableTestHost>().StartCoroutine(Test());}
    static IEnumerator Test()
    {
        var ui=BableGameUI.Instance;ui.Begin();yield return null;var player=Object.FindFirstObjectByType<PlayerController2D>();var body=player.GetComponent<Rigidbody2D>();var hp=player.GetComponent<HealthComponent>();var session=GameSession.Instance;var heal=player.GetComponent<PlayerHealChannelController>();
        foreach(var enemy in Object.FindObjectsByType<EnemyControllerBase>(FindObjectsSortMode.None))enemy.enabled=false;
        foreach(var speech in Object.FindObjectsByType<BossSpeech>(FindObjectsSortMode.None))speech.enabled=false;
        hp.Invincible=true;player.SetTestInput(0,0);yield return new WaitForSeconds(.5f);
        session.SetHealth(2,6);session.SetMana(3,3);heal.TestHold=true;yield return new WaitForSeconds(1.3f);
        Check("Healing channels with animation synced to hold progress",heal.IsChanneling&&heal.Progress>.3f&&heal.Progress<.6f);
        BableVerification.Capture("revision6/healing.png");heal.TestHold=false;yield return null;yield return null;
        Check("Early release cancels immediately without spending",!heal.IsChanneling&&heal.Progress==0&&session.CurrentHealth==2&&session.CurrentMana==3);
        var animator=player.GetComponent<CharacterPresentation>().animator;
        Check("Release leaves heal animation",!animator.GetCurrentAnimatorStateInfo(0).IsName("rightheal"));
        heal.TestHold=true;yield return new WaitForSeconds(3.2f);
        Check("Three second hold spends exactly one mana for one health",session.CurrentHealth==3&&session.CurrentMana==2&&!heal.IsChanneling);
        yield return new WaitForSeconds(3.3f);Check("Continued hold does not repeat healing",session.CurrentHealth==3&&session.CurrentMana==2&&!heal.IsChanneling);
        heal.TestHold=false;yield return null;heal.TestHold=true;yield return new WaitForSeconds(3.2f);heal.TestHold=false;yield return null;
        Check("Release and press permits a second heal",session.CurrentHealth==4&&session.CurrentMana==1);
        Check("HUD uses newly drawn socket atlas",Object.FindFirstObjectByType<VotiveHud>()!=null&&Resources.LoadAll<Sprite>("Bable/NewArt/VotiveAtlas").Length==16);
        Check("All 65 English lines have audio",Resources.LoadAll<AudioClip>("Bable/Dialogue/Voices").Length==65);
        var enemyReward=Object.FindObjectsByType<EnemyDeathReward>(FindObjectsSortMode.None).First(e=>e.GetComponent<BossBrain>()==null);int count=Object.FindObjectsByType<CollectiblePickup>(FindObjectsSortMode.None).Length;
        enemyReward.GetComponent<HealthComponent>().ApplyDamage(999);yield return null;yield return null;
        Check("Defeated small enemy restores combat mana",session.CurrentMana==2);
        Check("Defeated small enemy drops physical gold",Object.FindObjectsByType<CollectiblePickup>(FindObjectsSortMode.None).Length>count);
        var treasure=Object.FindObjectsByType<AnimatedTreasure>(FindObjectsSortMode.None).First(a=>a.coin);var sr=treasure.GetComponentsInChildren<SpriteRenderer>().First(s=>s.enabled);var frame=sr.sprite;yield return new WaitForSeconds(.15f);
        Check("Coins rotate through drawn frames",sr.sprite!=frame);
        Check("Coin is smaller than player and renders behind player",sr.bounds.size.y<1.6f&&sr.sortingOrder<player.GetComponent<CharacterPresentation>().animator.GetComponent<SpriteRenderer>().sortingOrder);
        var star=Object.FindObjectsByType<AnimatedTreasure>(FindObjectsSortMode.None).First(a=>!a.coin);var starVisual=star.GetComponentsInChildren<SpriteRenderer>().First(s=>s.enabled);
        Check("Star renders behind player",starVisual.sortingOrder<player.GetComponent<CharacterPresentation>().animator.GetComponent<SpriteRenderer>().sortingOrder);
        body.simulated=false;player.transform.position=new Vector3(138,51,0);yield return new WaitForSeconds(.8f);BableVerification.Capture("revision6/upper-forest.png");
        Check("Forest covers upper approach to height 63",Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None).Any(s=>s.name=="Forest approach canopy"&&s.bounds.Contains(new Vector3(138,51,0))));
        var princess=Object.FindFirstObjectByType<PrincessRescue>();player.transform.position=new Vector3(153,80,0);yield return new WaitForSeconds(.3f);
        Check("Western gate remains sealed before Nero dies",princess.sealedGate.activeSelf&&princess.sealedGate.GetComponent<Collider2D>().enabled&&!princess.Arrived);
        BableVerification.Capture("revision6/sealed-exit.png");
        var nero=Object.FindObjectsByType<BossBrain>(FindObjectsSortMode.None).First(b=>b.profile.kind==BossKind.Nero);nero.GetComponent<HealthComponent>().ApplyDamage(999);yield return new WaitForSeconds(2.5f);
        Check("Nero death opens original exit and teleports only through corridor",princess.unlocked&&princess.Arrived&&!princess.sealedGate.activeSelf&&player.transform.position.x<0&&ui.Mode=="play");
        BableVerification.Capture("revision6/dawn-arrival.png");
        player.transform.position=princess.transform.position+new Vector3(-2,1.5f,0);body.linearVelocity=Vector2.zero;yield return new WaitForSeconds(2);
        Check("Princess meeting starts a staged dialogue instead of instant victory",princess.IsRunning&&TowerDialogue.StoryActive&&ui.Mode=="play");
        BableVerification.Capture("revision6/princess-reunion.png");
        float deadline=Time.realtimeSinceStartup+80;while(!princess.Rescued&&Time.realtimeSinceStartup<deadline)yield return null;
        Check("Full voiced rescue sequence reaches ending",princess.Rescued&&ui.Mode=="victory");
        BableVerification.Capture("revision6/ending.png");Debug.Log("REV5_FINISHED "+checks.Count+" checks, "+failures.Count+" failures");
    }
}


