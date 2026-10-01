using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Bable;
using Babel.Runtime.Characters.Player;
using Babel.Runtime.Characters.Enemies;
using Babel.Runtime.Combat;
using Babel.Runtime.World;
using Object=UnityEngine.Object;

public static class BableRevision39EndingTests {
    static readonly List<string> checks=new(),failures=new();
    static void Check(string name,bool ok){checks.Add(name);if(!ok)failures.Add(name);File.WriteAllText("../reference/revision39/ending-tests.json",JsonUtility.ToJson(new BableVerification.Report{checks=checks.ToArray(),failures=failures.ToArray()},true));}
    public static void Run(){checks.Clear();failures.Clear();new GameObject("Ending regression").AddComponent<BableTestHost>().StartCoroutine(Test());}
    static IEnumerator Test(){
        while(TowerLoading.Busy||BableGameUI.Instance==null||!BableGameUI.Instance.Ready)yield return null;
        var p=Object.FindFirstObjectByType<PlayerController2D>();p.SetTestInput(0,0);p.GetComponent<HealthComponent>().Invincible=true;
        foreach(var b in Object.FindObjectsByType<BossSpeech>(FindObjectsSortMode.None))b.enabled=false;
        foreach(var e in Object.FindObjectsByType<EnemyControllerBase>(FindObjectsSortMode.None))e.enabled=false;
        foreach(var z in Object.FindObjectsByType<SummoningEncounter>(FindObjectsSortMode.None))z.enabled=false;
        TowerDialogue.AbortStory();
        var princess=Object.FindFirstObjectByType<PrincessRescue>();
        Check("Western exit sealed before final victory",!princess.unlocked&&princess.sealedGate.activeSelf&&princess.sealedGate.GetComponent<Collider2D>().enabled);
        var nero=Object.FindObjectsByType<BossBrain>(FindObjectsSortMode.None).First(b=>b.profile.kind==BossKind.Nero);nero.GetComponent<HealthComponent>().ApplyDamage(999);
        yield return new WaitForSeconds(1.4f);Check("Final victory unlocks western exit",princess.unlocked&&!princess.sealedGate.activeSelf);
        p.transform.position=princess.exitPosition;p.GetComponent<Rigidbody2D>().position=princess.exitPosition;Physics2D.SyncTransforms();Camera.main.GetComponent<CameraFollow2D>().SetTarget(p.transform);
        float end=Time.time+8;while(!princess.Arrived&&Time.time<end)yield return null;while(princess.IsRunning&&Time.time<end)yield return null;
        Check("Exit transports player to princess road",princess.Arrived&&p.transform.position.x<0&&p.transform.position.y>65);
        yield return new WaitForSeconds(1.2f);
        var sky=Object.FindObjectsByType<DawnRescueBackdrop>(FindObjectsSortMode.None).First(x=>x.name.Contains("rescue road"));
        Check("Princess road has full dawn sky",sky.Opacity>.99f&&sky.GetComponent<SpriteRenderer>().enabled);
        Check("Dawn image keeps aspect ratio",Mathf.Abs(sky.transform.localScale.x-sky.transform.localScale.y)<.001f);
        BableVerification.Capture("revision39/princess-road.png");
        p.transform.position=princess.transform.position+Vector3.left*2;p.GetComponent<Rigidbody2D>().position=p.transform.position;Physics2D.SyncTransforms();
        end=Time.time+90;while(!princess.Rescued&&Time.time<end)yield return null;
        Check("Princess conversation automatically reaches ending",princess.Rescued&&BableGameUI.Instance.Mode=="victory");
        BableVerification.Capture("revision39/ending.png");File.WriteAllText("../reference/revision39/ending-complete.txt",checks.Count+" checks; "+failures.Count+" failures");
    }
}
