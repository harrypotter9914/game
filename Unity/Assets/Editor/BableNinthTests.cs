using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEngine.Tilemaps;
using Babel.Runtime.World;
using Babel.Runtime.Characters.Player;
using Babel.Runtime.Characters.Enemies;
using Babel.Runtime.Combat;
using Bable;
public static class BableNinthTests
{
    static List<string> checks=new(),failures=new();
    static void Check(string name,bool result){checks.Add(name);if(!result)failures.Add(name);File.WriteAllText("../reference/revision9/tests.json",JsonUtility.ToJson(new BableVerification.Report{checks=checks.ToArray(),failures=failures.ToArray()},true));}
    public static void Run(){if(!UnityEditor.EditorApplication.isPlaying)throw new System.InvalidOperationException("Enter Play Mode before running tests");checks.Clear();failures.Clear();new GameObject("Masonry visibility verification").AddComponent<BableTestHost>().StartCoroutine(Test());}
    static IEnumerator Test(){
        BableGameUI.Instance.Begin();yield return new WaitForSeconds(.5f);
        var player=Object.FindFirstObjectByType<PlayerController2D>();player.SetTestInput(0,0);player.GetComponent<Rigidbody2D>().simulated=false;player.GetComponent<HealthComponent>().Invincible=true;
        foreach(var s in Object.FindObjectsByType<BossSpeech>(FindObjectsSortMode.None))s.enabled=false;
        foreach(var e in Object.FindObjectsByType<EnemyControllerBase>(FindObjectsSortMode.None))e.enabled=false;
        var camera=Camera.main;var mask=camera.GetComponent<PassageVisibility>();var follow=camera.GetComponent<CameraFollow2D>();
        var positions=new[]{new Vector2(-26,-2),new Vector2(301,-59.8f),new Vector2(392,28.5f),new Vector2(324,-39.8f)};
        var names=new[]{"forest-floor","burrow-room","thin-passage","aerial-room"};
        for(int i=0;i<positions.Length;i++){player.transform.position=positions[i];yield return new WaitForSeconds(1.2f);BableVerification.Capture("revision9/"+names[i]+".png");}
        var ground=GameObject.Find("GroundTilemap").GetComponent<Tilemap>();TileBase tile=null;foreach(var c in ground.cellBounds.allPositionsWithin)if(ground.HasTile(c)){tile=ground.GetTile(c);break;}
        for(int x=1000;x<=1013;x++){
        for(int y=998;y<=1001;y++)ground.SetTile(new Vector3Int(x,y,0),tile);
            for(int y=1006;y<=1008;y++)ground.SetTile(new Vector3Int(x,y,0),tile);
            ground.SetTile(new Vector3Int(x,996,0),tile);
        }
        for(int y=1002;y<=1005;y++){ground.SetTile(new Vector3Int(1000,y,0),tile);ground.SetTile(new Vector3Int(1001,y,0),tile);ground.SetTile(new Vector3Int(1013,y,0),tile);}
        follow.enabled=false;mask.minimumSize=mask.maximumSize=9;camera.orthographicSize=9;player.transform.position=new Vector3(1007.5f,1003.5f,0);camera.transform.position=new Vector3(1007.5f,1003.5f,-10);
        yield return new WaitForSeconds(.3f);
        Check("Current passage stays visible",mask.IsCellVisible(new Vector3Int(1008,1003,0)));
        Check("All four layers of thick floor remain visible",Enumerable.Range(998,4).All(y=>mask.IsCellVisible(new Vector3Int(1007,y,0))));
        Check("All three layers of ceiling remain visible",Enumerable.Range(1006,3).All(y=>mask.IsCellVisible(new Vector3Int(1007,y,0))));
        Check("Both layers of side wall remain visible",mask.IsCellVisible(new Vector3Int(1000,1003,0))&&mask.IsCellVisible(new Vector3Int(1001,1003,0)));
        Check("One-tile wall remains visible",mask.IsCellVisible(new Vector3Int(1013,1003,0)));
        Check("No lower passage is exposed",!mask.IsCellVisible(new Vector3Int(1007,997,0)));
        Check("No upper passage is exposed",!mask.IsCellVisible(new Vector3Int(1007,1009,0)));
        Check("No left passage is exposed",!mask.IsCellVisible(new Vector3Int(999,1003,0)));
        Check("No right passage behind thin wall is exposed",!mask.IsCellVisible(new Vector3Int(1014,1003,0)));
        Check("Masonry in the next room remains concealed",!mask.IsCellVisible(new Vector3Int(1007,996,0)));
        BableVerification.Capture("revision9/controlled-thickness-test.png");
        var gate=new GameObject("Visibility test breakable barrier");gate.transform.position=new Vector3(1009.5f,1004,0);gate.AddComponent<BoxCollider2D>().size=new Vector2(1,4);var breakable=gate.AddComponent<BreakableWall>();Physics2D.SyncTransforms();
        yield return new WaitForSeconds(1.2f);
        Check("Closed breakable wall conceals passage behind it",mask.IsCellVisible(new Vector3Int(1009,1003,0))&&!mask.IsCellVisible(new Vector3Int(1010,1003,0)));
        breakable.HitByShockwave();yield return new WaitForSeconds(.2f);
        Check("Destroying the barrier opens visibility immediately",mask.IsCellVisible(new Vector3Int(1010,1003,0)));
        for(int y=998;y<=1001;y++)ground.SetTile(new Vector3Int(1007,y,0),null);
        yield return new WaitForSeconds(.2f);
        Check("Stair opening does not reveal lower floor prematurely",!mask.IsCellVisible(new Vector3Int(1007,997,0)));
        player.transform.position=new Vector3(1007.5f,997.5f,0);camera.transform.position=new Vector3(1007.5f,997.5f,-10);yield return new WaitForSeconds(.2f);
        Check("Entering the lower floor reveals the occupied passage",mask.IsCellVisible(new Vector3Int(1007,997,0)));
        mask.enabled=false;yield return null;Check("Disabling visibility hides its curtain",!GameObject.Find("Unexplored passage curtain"));mask.enabled=true;
        File.WriteAllText("../reference/revision9/finished.txt",failures.Count+" failures / "+checks.Count+" checks");
    }
}
