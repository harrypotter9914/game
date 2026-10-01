using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Bable;
using Babel.Runtime.Core;
using Babel.Runtime.Characters.Player;
using Object=UnityEngine.Object;

public static class BableTwentyThirdTests
{
    static List<string> checks=new List<string>(),failures=new List<string>();
    static void Check(string name,bool ok){checks.Add((ok?"PASS ":"FAIL ")+name);if(!ok)failures.Add(name);File.WriteAllText("../reference/revision23/tests.txt",string.Join("\n",checks.ToArray()));}
    public static void Run(){checks.Clear();failures.Clear();var host=new GameObject("Revision23 tests");Object.DontDestroyOnLoad(host);host.AddComponent<BableTestHost>().StartCoroutine(Test(host));}
    static IEnumerator WaitMode(string mode){float end=Time.realtimeSinceStartup+25;while(Time.realtimeSinceStartup<end&&(BableGameUI.Instance==null||BableGameUI.Instance.Mode!=mode||!BableGameUI.Instance.Ready))yield return null;yield return new WaitForSecondsRealtime(.8f);}
    static IEnumerator Test(GameObject host)
    {
        yield return WaitMode("main");var ui=BableGameUI.Instance;
        Check("Startup has no world or HUD",GameSession.Instance==null&&!ui.GameplayHudVisible);
        var button=GameObject.Find("START GAME");var rect=button.GetComponent<RectTransform>();var origin=rect.anchoredPosition;var motion=button.GetComponent<ManuscriptMenuButton>();
        motion.OnPointerEnter(new PointerEventData(EventSystem.current));yield return new WaitForSecondsRealtime(.35f);
        Check("Hover rises lettering six pixels with a stable hit target",motion.Highlight>.99f&&motion.lettering.anchoredPosition.y>5.9f&&rect.anchoredPosition==origin);
        BableVerification.Capture("revision23/main-menu-hover.png");motion.OnPointerExit(new PointerEventData(EventSystem.current));yield return new WaitForSecondsRealtime(.35f);
        Check("Hover leaves smoothly",motion.Highlight<.01f&&motion.lettering.anchoredPosition.y<.1f);
        ui.Begin();ui.Begin();yield return new WaitForSecondsRealtime(1);
        var opening=Object.FindFirstObjectByType<TowerPrologue>();Check("Only one opening and only skip control",Object.FindObjectsByType<TowerPrologue>(FindObjectsSortMode.None).Length==1&&opening.GetComponentsInChildren<Button>().Length==1&&GameSession.Instance==null);
        BableVerification.Capture("revision23/prologue-1.png");opening.Skip();opening.Skip();yield return WaitMode("play");
        Check("Skip loads one clean game and stops narration",!TowerPrologue.IsNarrating&&Object.FindObjectsByType<GameSession>(FindObjectsSortMode.None).Length==1&&BableGameUI.Instance.GameplayHudVisible);
        ui=BableGameUI.Instance;var s=GameSession.Instance;s.SetHealth(3,s.MaxHealth);s.AddGold(71);ui.Pause();yield return null;
        var cont=GameObject.Find("CONTINUE").GetComponent<ManuscriptMenuButton>();cont.OnPointerEnter(new PointerEventData(EventSystem.current));yield return new WaitForSecondsRealtime(.35f);
        Check("Paused hover works on unscaled time",Time.timeScale==0&&cont.Highlight>.99f&&!ui.GameplayHudVisible);BableVerification.Capture("revision23/pause.png");
        ui.Resume();Check("Resume keeps health and gold",s.CurrentHealth==3&&s.CurrentGold==71);
        ui.Death();yield return new WaitForSecondsRealtime(.3f);BableVerification.Capture("revision23/death.png");Check("Death has independent exit and no HUD",GameObject.Find("EXIT").GetComponent<ManuscriptMenuButton>()!=null&&!ui.GameplayHudVisible);
        ui.MainMenu();yield return WaitMode("main");BableGameUI.Instance.Begin();yield return null;opening=Object.FindFirstObjectByType<TowerPrologue>();
        bool allVoices=true;for(int i=1;i<=7;i++)allVoices&=Resources.Load<AudioClip>("Bable/Dialogue/Voices/prologue_"+i)!=null;Check("All seven local narration clips imported",allVoices);
        bool allDialogue=true;for(int i=1;i<=5;i++)allDialogue&=Resources.Load<AudioClip>("Bable/Dialogue/Voices/boss"+i+"_allegory")!=null;allDialogue&=Resources.Load<AudioClip>("Bable/Dialogue/Voices/princess_promise")!=null;Check("Five guardian and princess additions are voiced",allDialogue);
        int last=-1;float deadline=Time.realtimeSinceStartup+270;bool silentWorld=true;
        while(opening!=null&&!opening.Finished&&Time.realtimeSinceStartup<deadline){silentWorld&=GameSession.Instance==null&&!BableGameUI.Instance.GameplayHudVisible;if(opening.PageIndex!=last){last=opening.PageIndex;yield return new WaitForSecondsRealtime(1.2f);if(opening!=null&&!opening.Finished)BableVerification.Capture("revision23/prologue-"+(last+1)+".png");}yield return null;}
        yield return WaitMode("play");Check("Automatic opening reaches all seven pages without user input",last==6&&BableGameUI.Instance.Mode=="play"&&silentWorld&&!TowerPrologue.IsNarrating);
        Check("Campaign guardians share allegorical titles",Object.FindObjectsByType<BossEncounter>(FindObjectsSortMode.None).Length==5);
        BableGameUI.Instance.MainMenu();yield return WaitMode("main");Check("Return to menu releases narration and gameplay",!TowerPrologue.IsNarrating&&GameSession.Instance==null);
        File.WriteAllText("../reference/revision23/tests-complete.txt",checks.Count+" checks; "+failures.Count+" failures");Object.Destroy(host);
    }
}
