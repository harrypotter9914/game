using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using Bable;
using Babel.Runtime.Core;
using Babel.Runtime.Characters.Player;
using Babel.Runtime.Combat;
public static class BableTwentySecondTests
{
    static readonly List<string> checks=new(),failures=new();
    static void Check(string text,bool pass){checks.Add(text);if(!pass)failures.Add(text);File.WriteAllText("../reference/revision22/menu-tests.json",JsonUtility.ToJson(new BableVerification.Report{checks=checks.ToArray(),failures=failures.ToArray()},true));}
    static bool NoWorld()=>GameSession.Instance==null&&Object.FindFirstObjectByType<PlayerController2D>()==null&&Object.FindObjectsByType<VotiveHud>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length==0&&GameObject.Find("GroundTilemap")==null;
    static IEnumerator WaitScene(string scene){float until=Time.realtimeSinceStartup+20;while(Time.realtimeSinceStartup<until&&(SceneManager.GetActiveScene().name!=scene||BableGameUI.Instance==null||BableGameUI.Instance.Mode=="loading"||!BableGameUI.Instance.Ready||scene=="Rune_Combat_Lab"&&(Object.FindFirstObjectByType<RuneCombatLab>()==null||!Object.FindFirstObjectByType<RuneCombatLab>().Ready)))yield return null;yield return new WaitForSecondsRealtime(.8f);Check("Scene ready: "+scene,SceneManager.GetActiveScene().name==scene&&BableGameUI.Instance!=null);}
    public static void Run(){checks.Clear();failures.Clear();var go=new GameObject("Scene lifecycle audit");Object.DontDestroyOnLoad(go);go.AddComponent<BableTestHost>().StartCoroutine(Test(go));}
    static IEnumerator Test(GameObject host)
    {
        yield return WaitScene("MainMenu");var ui=BableGameUI.Instance;
        bool absent=true;for(int i=0;i<30;i++){absent&=NoWorld();yield return null;}
        Check("Thirty startup frames contain no world, player, session or HUD",absent);
        Check("Title menu owns layer 400",ui.Mode=="main"&&ui.ActiveMenuOrder==400&&!ui.GameplayHudVisible);BableVerification.Capture("revision22/main-menu.png");
        ui.Practice();yield return null;Check("Practice selector from title has no gameplay HUD",ui.Mode=="practice"&&NoWorld());ui.Resume();Check("Practice back returns to title, not nonexistent gameplay",ui.Mode=="main"&&NoWorld());
        ui.Begin();ui.Begin();yield return WaitScene("Gameplay_Main");ui=BableGameUI.Instance;var s=GameSession.Instance;var p=Object.FindFirstObjectByType<PlayerController2D>();var hp=p.GetComponent<HealthComponent>();var ground=GameObject.Find("GroundTilemap");var handle=SceneManager.GetActiveScene().handle;
        Check("Start loads playable campaign and HUD",ui.Mode=="play"&&ui.GameplayHudVisible&&!s.IsPaused&&Time.timeScale==1&&ground!=null);
        Check("Repeated start click does not create duplicate sessions",Object.FindObjectsByType<GameSession>(FindObjectsSortMode.None).Length==1&&Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length==1&&Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length==1);
        s.AddGold(137);s.SetHealth(3,s.MaxHealth);s.SetMana(1,s.MaxMana);p.SetTestInput(0,0);yield return new WaitForSeconds(.3f);ui.Pause();var position=p.transform.position;
        Check("Pause hides HUD synchronously and uses layer 300",!ui.GameplayHudVisible&&ui.ActiveMenuOrder==300&&s.IsPaused&&Time.timeScale==0);yield return new WaitForSecondsRealtime(.3f);
        Check("Pause freezes the existing world",p.transform.position==position&&GameObject.Find("GroundTilemap")==ground&&SceneManager.GetActiveScene().handle==handle);BableVerification.Capture("revision22/pause.png");
        ui.Resume();Check("Resume retains health, mana and gold",s.CurrentHealth==3&&s.CurrentMana==1&&s.CurrentGold==137&&ui.GameplayHudVisible&&SceneManager.GetActiveScene().handle==handle);
        hp.Invincible=false;hp.ApplyDamage(999);Check("Actual death uses layer 500 without HUD",ui.Mode=="death"&&!ui.GameplayHudVisible&&ui.ActiveMenuOrder==500&&Time.timeScale==0);yield return null;BableVerification.Capture("revision22/death.png");
        ui.Respawn();yield return new WaitForSecondsRealtime(1.3f);Check("Respawn restores life in same world without reload",hp.CurrentHealth==s.MaxHealth&&ui.Mode=="play"&&ui.GameplayHudVisible&&SceneManager.GetActiveScene().handle==handle&&GameObject.Find("GroundTilemap")==ground);
        ui.Pause();GameObject.Find("MAIN MENU").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();yield return WaitScene("MainMenu");Check("Return from pause unloads the entire gameplay world",NoWorld()&&Time.timeScale==1&&BableGameUI.Instance.Mode=="main");
        BableGameUI.Instance.Begin();yield return WaitScene("Gameplay_Main");ui=BableGameUI.Instance;s=GameSession.Instance;p=Object.FindFirstObjectByType<PlayerController2D>();Check("New campaign starts clean",s.CurrentGold==0&&s.RuneInventory.CollectedRunes.Count==0&&s.CurrentHealth==s.MaxHealth&&!s.HasWeapon);
        p.GetComponent<HealthComponent>().ApplyDamage(999);GameObject.Find("MAIN MENU").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();yield return WaitScene("MainMenu");Check("Return from death leaves no HUD or session",NoWorld());
        BableGameUI.Instance.OpenLab();yield return WaitScene("Rune_Combat_Lab");var lab=Object.FindFirstObjectByType<RuneCombatLab>();File.WriteAllText("../reference/revision22/lab-entry.txt","lab="+(lab!=null)+" ready="+(lab!=null&&lab.Ready)+" session="+(GameSession.Instance!=null)+" runes="+(GameSession.Instance!=null?GameSession.Instance.RuneInventory.CollectedRunes.Count:-1)+" mode="+BableGameUI.Instance.Mode+" hud="+BableGameUI.Instance.GameplayHudVisible+" paused="+(GameSession.Instance!=null&&GameSession.Instance.IsPaused));Check("Title opens laboratory directly",lab!=null&&lab.Ready&&GameSession.Instance.RuneInventory.CollectedRunes.Count==8&&BableGameUI.Instance.GameplayHudVisible);
        BableGameUI.Instance.MainMenu();yield return WaitScene("MainMenu");Check("Returning from lab also unloads its HUD and world",NoWorld());
        Check("Final title has one camera listener and one input system",Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length==1&&Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length==1);BableVerification.Capture("revision22/main-menu-return.png");
        Debug.Log("Revision22 complete: "+checks.Count+" checks, "+failures.Count+" failures");Object.Destroy(host);
    }
}
