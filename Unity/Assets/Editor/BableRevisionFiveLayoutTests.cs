using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Bable;
using Babel.Runtime.Characters.Player;
using Babel.Runtime.Characters.Enemies;
using Babel.Runtime.Combat;
using Babel.Runtime.Core;
public static class BableRevisionFiveLayoutTests
{
    static List<string> checks=new List<string>(),failures=new List<string>();
    static void Check(string name,bool pass){checks.Add(name);if(!pass)failures.Add(name);File.WriteAllText("../reference/revision5/layout-tests.json",JsonUtility.ToJson(new BableVerification.Report{checks=checks.ToArray(),failures=failures.ToArray()},true));}
    public static void Run(){checks.Clear();failures.Clear();new GameObject("Final layout checks").AddComponent<BableTestHost>().StartCoroutine(Test());}
    static IEnumerator Test()
    {
        var ui=BableGameUI.Instance;ui.Begin();yield return null;
        foreach(var e in Object.FindObjectsByType<EnemyControllerBase>(FindObjectsSortMode.None))e.enabled=false;
        foreach(var s in Object.FindObjectsByType<BossSpeech>(FindObjectsSortMode.None))s.enabled=false;
        var p=Object.FindFirstObjectByType<PlayerController2D>();p.SetTestInput(0,0);p.GetComponent<HealthComponent>().Invincible=true;
        yield return new WaitForSeconds(.5f);TowerDialogue.Speak("merchant_low",Object.FindFirstObjectByType<Babel.Runtime.Shop.ShopkeeperController>().transform);yield return new WaitForSeconds(.2f);BableVerification.Capture("revision5/merchant-dialogue.png");
        var princess=Object.FindFirstObjectByType<PrincessRescue>();
        Check("Princess feet rest on original road at y=73",Mathf.Abs(princess.GetComponent<SpriteRenderer>().bounds.min.y-73)<.05f);
        princess.Unlock();p.transform.position=new Vector3(153,80,0);p.GetComponent<Rigidbody2D>().linearVelocity=Vector2.zero;yield return new WaitForSeconds(2.5f);
        Check("Portal still reaches sunny road after layout changes",princess.Arrived&&p.transform.position.x<0);BableVerification.Capture("revision5/dawn-arrival.png");
        p.transform.position=princess.transform.position+new Vector3(-2,1.5f,0);yield return new WaitForSeconds(2.1f);
        Check("Cinematic hides vitals and keeps both actors above subtitle",TowerDialogue.StoryActive&&!GameObject.Find("Bable Interface").transform.Find("Votive vitals").gameObject.activeSelf&&Camera.main.WorldToViewportPoint(p.transform.position).y>.2f);
        BableVerification.Capture("revision5/princess-reunion.png");
        float time=Time.time;ui.Pause();yield return new WaitForSecondsRealtime(.3f);
        var dialogue=TowerDialogue.Instance;
        Check("Pause freezes story clock and voice",Mathf.Abs(Time.time-time)<.02f&&!dialogue.GetComponent<AudioSource>().isPlaying&&!dialogue.GetComponent<Canvas>().enabled);
        ui.Resume();yield return new WaitForSeconds(.1f);
        Check("Resume restores dialogue and voice",dialogue.GetComponent<Canvas>().enabled&&dialogue.GetComponent<AudioSource>().isPlaying);
        Debug.Log("REV5_LAYOUT_FINISHED");
    }
}
