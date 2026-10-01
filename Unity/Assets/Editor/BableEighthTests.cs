using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEngine.Tilemaps;
using Bable;
using Babel.Runtime.Characters.Player;
using Babel.Runtime.Characters.Enemies;
using Babel.Runtime.Combat;
public static class BableEighthTests
{
    static List<string> checks=new(),failures=new();
    static void Check(string label,bool pass){checks.Add(label);if(!pass)failures.Add(label);File.WriteAllText("../reference/revision8/tests.json",JsonUtility.ToJson(new BableVerification.Report{checks=checks.ToArray(),failures=failures.ToArray()},true));}
    public static void Run(){checks.Clear();failures.Clear();new GameObject("Eighth revision tests").AddComponent<BableTestHost>().StartCoroutine(Test());}
    static IEnumerator Test(){
        BableGameUI.Instance.Begin();yield return new WaitForSeconds(.5f);
        var player=Object.FindFirstObjectByType<PlayerController2D>();player.SetTestInput(0,0);player.GetComponent<HealthComponent>().Invincible=true;var rb=player.GetComponent<Rigidbody2D>();rb.simulated=false;
        foreach(var speech in Object.FindObjectsByType<BossSpeech>(FindObjectsSortMode.None))speech.enabled=false;
        foreach(var enemy in Object.FindObjectsByType<EnemyControllerBase>(FindObjectsSortMode.None))enemy.enabled=false;
        var forest=Object.FindObjectsByType<ForestAscentBackdrop>(FindObjectsSortMode.None);
        Check("Exactly one forest image in campaign",forest.Length==1);
        player.transform.position=new Vector3(-26,0,0);yield return new WaitForSeconds(1);
        var camera=Camera.main;var f=forest[0];var firstViewport=camera.WorldToViewportPoint(f.transform.position);
        Check("Forest uses uniform scale",Mathf.Abs(f.transform.localScale.x-f.transform.localScale.y)<.001f);
        BableVerification.Capture("revision8/forest-ground.png");
        player.transform.position+=new Vector3(18,14,0);yield return new WaitForSeconds(1.5f);
        Check("Single forest tracks both camera axes",Vector2.Distance(firstViewport,camera.WorldToViewportPoint(f.transform.position))<.01f);
        Check("Forest covers the camera without repetition",f.GetComponent<SpriteRenderer>().bounds.size.y>=camera.orthographicSize*2&&f.GetComponent<SpriteRenderer>().bounds.size.x>=camera.orthographicSize*2*camera.aspect);
        BableVerification.Capture("revision8/forest-ascent.png");
        var boss=Object.FindObjectsByType<BossBrain>(FindObjectsSortMode.None).First(b=>b.profile.kind==BossKind.Burrow);
        player.transform.position=new Vector3(301,-59.8f,0);boss.enabled=true;yield return null;
        float start=Time.time;bool visible=true,smooth=true,aligned=true,transition=true;int samples=0;float travel=0;Vector2 previous=boss.transform.position;string previousState=boss.State;var states=new HashSet<string>();var shots=new HashSet<string>();
        var tiles=GameObject.Find("GroundTilemap").GetComponent<Tilemap>();
        while(Time.time-start<18){
            if(Time.time-start>7)player.transform.position=new Vector3(310,-59.8f,0);
            yield return new WaitForEndOfFrame();
            states.Add(boss.State);
            if(boss.State=="Burrow"){
                samples++;visible&=boss.GetComponent<BurrowPresentation>().TrailVisible;
                if(previousState=="Burrow"){float delta=Vector2.Distance(previous,boss.transform.position);travel+=delta;smooth&=delta<=boss.profile.speed*Time.deltaTime+.05f;}
            }
            if(boss.State=="EmergeWarning"){
                float foot=boss.transform.position.y+boss.FootOffset;var cell=tiles.WorldToCell(new Vector3(boss.transform.position.x,foot-.05f,0));
                if(boss.StateProgress>.4f)transition&=boss.GetComponent<CharacterPresentation>().animator.GetComponent<SpriteRenderer>().enabled;
                float top=tiles.GetCellCenterWorld(cell).y+.5f;aligned&=tiles.HasTile(cell)&&Mathf.Abs(foot-top)<.04f;
            }
            string shot=boss.State=="EmergeWarning"?(boss.StateProgress<.3f?"warning":boss.StateProgress<.7f?"rising":"emerged"):boss.State;
            if((shot=="Burrow"||shot=="warning"||shot=="rising"||shot=="emerged"||shot=="Recover")&&!shots.Contains(shot)){
                if(shot!="Burrow"||Time.time-start>1){BableVerification.Capture("revision8/korah-"+shot+".png");shots.Add(shot);}
            }
            previous=boss.transform.position;previousState=boss.State;
        }
        Check("Burrow has continuous visible surface trail",visible&&samples>100);
        Check("Burrow moves continuously instead of teleporting",smooth&&travel>8);
        Check("Emergence feet align within 4 cm of solid tile",aligned&&states.Contains("EmergeWarning"));
        Check("Body remains visible throughout late emergence",transition);
        Check("Burrow completes repeated dig and emerge cycles",states.Contains("Dig")&&states.Contains("Recover"));
        boss.enabled=false;boss.ResetEncounter();
        foreach(var b in Object.FindObjectsByType<BossBrain>(FindObjectsSortMode.None)){
            b.enabled=true;b.ResetEncounter();player.transform.position=b.transform.position+Vector3.right*2;
            yield return new WaitForSeconds(.15f);
            if(b.profile.kind!=BossKind.Burrow){player.transform.position=b.transform.position+Vector3.left*2;yield return new WaitForSeconds(.2f);Check(b.profile.kind+" turns toward crossed player during recovery",b.LookDirection==-1);}
            b.enabled=false;b.ResetEncounter();
        }
        File.WriteAllText("../reference/revision8/finished.txt",failures.Count+" failures / "+checks.Count+" checks; underground samples="+samples+" travel="+travel);
    }
}
