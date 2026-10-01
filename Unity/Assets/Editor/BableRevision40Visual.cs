using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Bable;
using Babel.Runtime.Characters.Enemies;
using Babel.Runtime.Characters.Player;
using Babel.Runtime.Combat;
using Object=UnityEngine.Object;
public static class BableRevision40Visual {
    public static void Run(){new GameObject("Arrow visual review").AddComponent<BableTestHost>().StartCoroutine(Record());}
    static IEnumerator Record(){
        while(TowerLoading.Busy)yield return null;
        foreach(var s in Object.FindObjectsByType<BossSpeech>(FindObjectsSortMode.None))s.enabled=false;TowerDialogue.AbortStory();
        var p=Object.FindFirstObjectByType<PlayerController2D>();p.SetTestInput(0,0);p.enabled=false;p.GetComponent<HealthComponent>().Invincible=true;
        var enemies=Object.FindObjectsByType<EnemyControllerBase>(FindObjectsSortMode.None);foreach(var e in enemies)e.enabled=false;
        var bow=enemies.OfType<RangedEnemyController>().First();
        // Stage the existing actor on the opening forest floor for readable captures.
        float floor=p.GetComponent<Collider2D>().bounds.min.y;
        Vector3 start=p.transform.position;
        Place(bow.gameObject,start.x-4,floor);Place(p.gameObject,start.x+4,floor);
        var camera=Camera.main;
        foreach(var component in camera.GetComponents<MonoBehaviour>())component.enabled=false;
        camera.orthographicSize=4;camera.transform.position=new Vector3(start.x, floor+3.1f,-10);
        bow.enabled=true;yield return null;
        typeof(EnemyControllerBase).GetMethod("FaceTarget",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(bow,new object[]{8f});
        typeof(RangedEnemyController).GetMethod("AttackTarget",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(bow,null);
        for(int i=0;i<18;i++){yield return new WaitForSeconds(.10f);yield return new WaitForEndOfFrame();BableVerification.Capture("revision40/arrow-"+i.ToString("00")+".png");}
        bow.enabled=false;File.WriteAllText("../reference/revision40/visual-complete.txt","18 in-engine arrow frames captured");
    }
    static void Place(GameObject o,float x,float floor){var c=o.GetComponent<Collider2D>();var pos=o.transform.position;pos.x=x;pos.y+=floor-c.bounds.min.y;o.transform.position=pos;var r=o.GetComponent<Rigidbody2D>();r.position=pos;r.gravityScale=0;r.linearVelocity=Vector2.zero;r.constraints=RigidbodyConstraints2D.FreezeAll;Physics2D.SyncTransforms();}
}
