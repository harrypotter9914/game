using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Bable;
using Babel.Runtime.Characters.Enemies;
using Babel.Runtime.Characters.Player;
using Babel.Runtime.Combat;
using Babel.Runtime.Core;
using Object=UnityEngine.Object;
public static class BableRevision40Tests {
    static readonly List<string> checks=new(),failures=new(),trace=new();
    static void Check(string name,bool ok){checks.Add(name);if(!ok)failures.Add(name);File.WriteAllText("../reference/revision40/tests.json",JsonUtility.ToJson(new BableVerification.Report{checks=checks.ToArray(),failures=failures.ToArray()},true));}
    static object Call(object o,string name,params object[] args){var t=o.GetType();MethodInfo m=null;while(t!=null&&m==null){m=t.GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.DeclaredOnly);t=t.BaseType;}return m.Invoke(o,args);}
    static void Move(GameObject o,Vector2 p){var c=o.GetComponent<Collider2D>();o.transform.position+=(Vector3)(p-(Vector2)c.bounds.center);var r=o.GetComponent<Rigidbody2D>();r.simulated=true;r.gravityScale=0;r.constraints=RigidbodyConstraints2D.FreezeAll;r.position=o.transform.position;r.linearVelocity=Vector2.zero;Physics2D.SyncTransforms();}
    public static void Run(){checks.Clear();failures.Clear();trace.Clear();File.WriteAllText("../reference/revision40/complete.txt","RUNNING");new GameObject("Revision 40 combat audit").AddComponent<BableTestHost>().StartCoroutine(Test());}
    static IEnumerator Test(){
        while(TowerLoading.Busy)yield return null;
        foreach(var s in Object.FindObjectsByType<BossSpeech>(FindObjectsSortMode.None))s.enabled=false;
        TowerDialogue.AbortStory();
        var p=Object.FindFirstObjectByType<PlayerController2D>();p.enabled=false;p.SetTestInput(0,0);var hp=p.GetComponent<HealthComponent>();hp.Configure(200,200);hp.Invincible=true;
        var all=Object.FindObjectsByType<EnemyControllerBase>(FindObjectsSortMode.None);
        foreach(var e in all){e.enabled=false;e.GetComponent<Rigidbody2D>().simulated=false;Check(e.name+" has one AI controller",e.GetComponents<EnemyControllerBase>().Length==1);}
        Move(p.gameObject,new Vector2(853,100));
        foreach(var group in all.Where(e=>!(e is BossBrain)).GroupBy(e=>e.GetType())){
            var e=group.First();Move(e.gameObject,new Vector2(850,100));e.enabled=true;yield return null;Call(e,"FaceTarget",3f);
            float windup=(float)e.GetType().GetField("telegraphDuration",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(e);
            Call(e,"AttackTarget");yield return new WaitForSeconds(windup*.7f);yield return new WaitForEndOfFrame();
            var a=e.GetComponent<CharacterPresentation>().animator;
            Check(e.GetType().Name+" holds anticipation before release",a.GetCurrentAnimatorStateInfo(0).normalizedTime<.51f||e is GiantEnemyController);
            yield return new WaitForSeconds(windup*.3f+.05f);yield return new WaitForEndOfFrame();
            Check(e.GetType().Name+" continues into release instead of restarting",a.GetCurrentAnimatorStateInfo(0).normalizedTime>=.5f);
            if(e is RangedEnemyController){
                var shots=Object.FindObjectsByType<MagicArrowVisual>(FindObjectsSortMode.None);
                Check("Archer produces one arrow",shots.Length==1);
                if(shots.Length==1){var shot=shots[0];Check("Arrow uses dedicated enchanted-arrow sprite",shot.GetComponent<SpriteRenderer>()!=null&&shot.GetComponent<SpriteRenderer>().sprite.name=="MagicArrow40");Check("Arrow starts ahead of archer body",shot.transform.position.x>e.GetComponent<Collider2D>().bounds.center.x+.2f);Check("Arrow has a long shaft",shot.GetComponent<SpriteRenderer>().sprite.bounds.size.x>1.3f);}
            }
            yield return new WaitForSeconds(.5f);
            Check(e.GetType().Name+" produces no floating callouts",!Object.FindObjectsByType<TMPro.TMP_Text>(FindObjectsSortMode.None).Any(t=>t.gameObject.name.Contains("Clone")&&(t.text=="Aim"||t.text=="Slash"||t.text=="Heavy Slam"||t.text=="Shield Bash"||t.text=="Boneslinger")));
            // Hitting a charging enemy must cancel the old delayed strike.
            foreach(var shot in Object.FindObjectsByType<EnemyProjectile>(FindObjectsSortMode.None))Object.Destroy(shot.gameObject);
            Call(e,"AttackTarget");yield return new WaitForSeconds(.05f);e.GetComponent<HealthComponent>().ApplyDamage(1);hp.Invincible=false;int before=hp.CurrentHealth;
            yield return new WaitForSeconds(windup+.1f);Check(e.GetType().Name+" interrupted windup has no hidden hit",hp.CurrentHealth==before&&Object.FindObjectsByType<MagicArrowVisual>(FindObjectsSortMode.None).Length==0);hp.Invincible=true;
            e.enabled=false;e.GetComponent<Rigidbody2D>().simulated=false;Move(e.gameObject,new Vector2(820,100));e.GetComponent<Rigidbody2D>().simulated=false;
        }
        foreach(var b in all.OfType<BossBrain>().OrderBy(b=>b.profile.kind)){
            var reward=b.GetComponent<BossAbilityReward>();
            if(b.profile.kind!=BossKind.Nero){var actual=(AbilityId)typeof(BossAbilityReward).GetField("abilityId",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(reward);var expected=new[]{AbilityId.Shockwave,AbilityId.WallJump,AbilityId.DoubleJump,AbilityId.CrystalDash}[(int)b.profile.kind];Check(b.profile.kind+" awards "+expected,actual==expected);}
            else Check("Nero alone is final boss",b.GetComponent<BossEncounter>().final&&all.OfType<BossBrain>().Count(x=>x.GetComponent<BossEncounter>().final)==1);
            b.arenaCenter=new Vector2(850,100);b.arenaSize=new Vector2(70,40);Move(b.gameObject,new Vector2(850,100));Move(p.gameObject,new Vector2(853,100));b.enabled=true;yield return null;Call(b,"Change","Recover",100f,"idle");b.FaceDialogueTarget(p.transform);
            string[] attacks=b.profile.kind==BossKind.Shockwave?new[]{"Heavy","Quick","Wave"}:b.profile.kind==BossKind.Aerial?new[]{"Rising","Spin","Dash"}:b.profile.kind==BossKind.Nero?new[]{"Combo","Fan","Dash"}:new string[0];
            foreach(string attack in attacks){
                foreach(var shot in Object.FindObjectsByType<EnemyProjectile>(FindObjectsSortMode.None))Object.Destroy(shot.gameObject);
                Call(b,"Windup",attack,.25f);yield return new WaitForSeconds(.24f);
                Check(b.profile.kind+" "+attack+" has no pre-release projectile",Object.FindObjectsByType<EnemyProjectile>(FindObjectsSortMode.None).Length==0);
                int maxActive=0;float end=Time.time+.8f;bool renderer=false;
                while(Time.time<end){yield return null;var windows=Object.FindObjectsByType<BossStrikeEffect>(FindObjectsSortMode.None).Where(x=>x.Owner==b&&x.enabled).ToArray();maxActive=Mathf.Max(maxActive,windows.Length);renderer|=windows.Any(x=>x.GetComponent<Renderer>()!=null);}
                Check(b.profile.kind+" "+attack+" never overlaps melee windows",maxActive<=1);Check(b.profile.kind+" "+attack+" has no duplicated blade renderer",!renderer);
                Call(b,"Change","Recover",100f,"idle");yield return new WaitForSeconds(.5f);
            }
            Check(b.profile.kind+" has one animated character renderer",b.GetComponentsInChildren<Animator>().Length==1);
            b.enabled=false;b.GetComponent<Rigidbody2D>().simulated=false;
        }
        // Real swept arrow collision: solid wall, pause and target hit.
        foreach(var shot in Object.FindObjectsByType<EnemyProjectile>(FindObjectsSortMode.None))Object.Destroy(shot.gameObject);
        Move(p.gameObject,new Vector2(905,100));hp.Invincible=false;int health=hp.CurrentHealth;
        var owner=all.First().gameObject;var wall=new GameObject("Arrow audit wall");wall.transform.position=new Vector3(902,100);wall.AddComponent<BoxCollider2D>().size=new Vector2(.3f,8);Physics2D.SyncTransforms();
        var arrow=MagicArrowVisual.Launch(owner,new Vector2(900,100),Vector2.right,10,1);yield return new WaitForSeconds(.8f);Check("Magic arrow stops at a solid wall",arrow==null&&hp.CurrentHealth==health);Object.Destroy(wall);yield return null;
        arrow=MagicArrowVisual.Launch(owner,new Vector2(900,100),Vector2.right,10,1);yield return new WaitForSeconds(.2f);Vector3 position=arrow.transform.position;BableGameUI.Instance.Pause();yield return new WaitForSecondsRealtime(.2f);Check("Pause freezes flying arrow",arrow!=null&&arrow.transform.position==position);BableGameUI.Instance.Resume();yield return new WaitForSeconds(.8f);Check("Arrow deals one hit then disappears",hp.CurrentHealth==health-1&&arrow==null);
        File.WriteAllLines("../reference/revision40/trace.txt",trace);File.WriteAllText("../reference/revision40/complete.txt",checks.Count+" checks; "+failures.Count+" failures");
    }
}
