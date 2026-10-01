using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Bable;
using Babel.Runtime.Core;
using Babel.Runtime.Characters.Player;
using Babel.Runtime.Combat;

[InitializeOnLoad]
public static class BableRevision50Tests
{
    const string Flag="BableRevision50.Batch";
    static string Output=>Path.GetFullPath(Path.Combine(Application.dataPath,"../../reference/revision50"));
    static readonly List<string> checks=new(), failures=new();
    static double started;
    static BableRevision50Tests(){EditorApplication.playModeStateChanged+=ModeChanged;}
    public static void Batch()
    {
        Directory.CreateDirectory(Output);
        SessionState.SetBool(Flag,true);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        EditorApplication.EnterPlaymode();
    }
    static void ModeChanged(PlayModeStateChange state)
    {
        if(!SessionState.GetBool(Flag,false))return;
        if(state==PlayModeStateChange.EnteredPlayMode){started=EditorApplication.timeSinceStartup;EditorApplication.update+=Watchdog;new GameObject("Wall contact verification").AddComponent<BableTestHost>().StartCoroutine(Test());}
    }
    static void Watchdog(){if(EditorApplication.timeSinceStartup-started>90){File.WriteAllText(Path.Combine(Output,"timeout.txt"),"Wall test timed out");EditorApplication.Exit(2);}}
    static void Check(string name,bool pass){checks.Add(name);if(!pass)failures.Add(name);File.WriteAllText(Path.Combine(Output,"tests.json"),JsonUtility.ToJson(new BableVerification.Report{checks=checks.ToArray(),failures=failures.ToArray()},true));}
    static void Contacts(PlayerController2D p)=>typeof(PlayerController2D).GetMethod("UpdateContacts",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(p,null);
    static IEnumerator Test()
    {
        Time.timeScale=1;
        // This is an isolated physics fixture, without campaign UI for the
        // normal loading curtain to wait on. Campaign loading is smoke-tested.
        foreach(var loading in UnityEngine.Object.FindObjectsByType<TowerLoading>(FindObjectsSortMode.None))UnityEngine.Object.DestroyImmediate(loading.gameObject);
        var session=new GameObject("Session").AddComponent<GameSession>();
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Characters/Player/Player.prefab");
        var actor=UnityEngine.Object.Instantiate(prefab);
        var p=actor.GetComponent<PlayerController2D>();
        var body=actor.GetComponent<Rigidbody2D>();
        var collider=actor.GetComponent<BoxCollider2D>();
        var art=actor.GetComponent<CharacterPresentation>();
        var sprite=art.animator.GetComponent<SpriteRenderer>();
        var home=art.animator.transform.localPosition;
        var scale=art.animator.transform.localScale;
        var size=collider.size;
        session.UnlockWeapon();p.SetTestInput(0,0);body.gravityScale=0;
        var cam=new GameObject("Verification camera").AddComponent<Camera>();cam.tag="MainCamera";cam.orthographic=true;cam.orthographicSize=2.1f;cam.backgroundColor=new Color(.035f,.045f,.065f);cam.clearFlags=CameraClearFlags.SolidColor;
        var wall=new GameObject("Vertical stone face");wall.transform.position=new Vector3(4,0,0);var wallShape=wall.AddComponent<BoxCollider2D>();wallShape.size=new Vector2(2,30);
        var wallSprite=wall.AddComponent<SpriteRenderer>();wallSprite.sprite=Sprite.Create(Texture2D.whiteTexture,new Rect(0,0,1,1),new Vector2(.5f,.5f),1);wallSprite.drawMode=SpriteDrawMode.Sliced;wallSprite.size=wallShape.size;wallSprite.color=new Color(.23f,.25f,.30f);wallSprite.sortingOrder=0;
        body.position=new Vector2(3-collider.bounds.extents.x-.015f,4);body.linearVelocity=Vector2.down;Physics2D.SyncTransforms();Contacts(p);
        Check("Wall ability remains gated",!p.IsWallSliding);
        session.UnlockAbility(AbilityId.WallJump);
        yield return null;
        for(int direction=-1;direction<=1;direction+=2)
        {
            wall.transform.position=new Vector3(direction*4,0,0);
            body.position=new Vector2(direction*(3-collider.bounds.extents.x-.015f),4);
            actor.transform.position=body.position;body.linearVelocity=Vector2.down;
            p.SetTestInput(direction,0);art.ReleaseAction();Physics2D.SyncTransforms();Contacts(p);
            yield return new WaitForSeconds(.06f);
            Check(direction+" correct surface and wall-facing pose",p.IsWallSliding&&p.WallDirection==direction&&sprite.flipX==(direction<0)&&Mathf.Abs(p.WallSurfaceX-direction*3)<.03f);
            bool aligned=true,stable=true;
            for(int i=0;i<12;i++){
                yield return new WaitForSeconds(.045f);
                float edge=direction>0?sprite.bounds.max.x:sprite.bounds.min.x;
                aligned&=Mathf.Abs(edge-(direction*3-direction*.012f))<.025f;
                stable&=sprite.sprite.name=="PilgrimExtras_08";
            }
            Check(direction+" palm stays outside wall over a full old animation cycle",aligned);
            Check(direction+" no crouch or sword-through-wall frames",stable);
            Check(direction+" collider and artwork scale preserved",collider.size==size&&art.animator.transform.localScale==scale);
            cam.transform.position=new Vector3(direction*2.2f,actor.transform.position.y+.1f,-10);
            Capture(cam,direction<0?"wall-left.png":"wall-right.png");
            p.SetTestInput(-direction,0);yield return null;
            Check(direction+" input away cannot turn gripping hand into wall",!p.IsWallSliding||sprite.flipX==(direction<0));
            body.position=new Vector2(direction*(3-collider.bounds.extents.x-.015f),4);actor.transform.position=body.position;body.linearVelocity=Vector2.down;Physics2D.SyncTransforms();Contacts(p);
            p.SetTestInput(direction,0,true);yield return new WaitForSeconds(.045f);
            Check(direction+" wall jump pushes up and away: "+body.linearVelocity,body.linearVelocity.x*direction<0&&body.linearVelocity.y>0);
            p.SetTestInput(0,0);yield return new WaitForSeconds(.3f);
            Check(direction+" detached pose returns to original artwork anchor",!p.IsWallSliding&&Vector3.Distance(art.animator.transform.localPosition,home)<.01f);
        }
        wall.SetActive(false);body.position=new Vector2(0,4);actor.transform.position=body.position;body.linearVelocity=Vector2.down;
        var enemy=new GameObject("Actor is not a wall");enemy.transform.position=new Vector3(collider.bounds.extents.x+.51f,4,0);enemy.AddComponent<BoxCollider2D>();enemy.AddComponent<HealthComponent>();Physics2D.SyncTransforms();Contacts(p);
        Check("Enemy body never grants wall grip",!p.IsWallSliding&&p.WallDirection==0);
        UnityEngine.Object.Destroy(enemy);
        wall.SetActive(true);wall.transform.position=new Vector3(0,2.7f,0);wallShape.size=new Vector2(30,.2f);Physics2D.SyncTransforms();Contacts(p);
        Check("Floor is not classified as a side wall",p.WallDirection==0);
        SessionState.SetBool(Flag,false);EditorApplication.update-=Watchdog;
        File.WriteAllText(Path.Combine(Output,"complete.txt"),failures.Count+" failures / "+checks.Count+" checks");
        EditorApplication.Exit(failures.Count==0?0:1);
    }
    static void Capture(Camera camera,string filename){
        var target=new RenderTexture(960,720,24);var previous=RenderTexture.active;camera.targetTexture=target;camera.Render();RenderTexture.active=target;
        var image=new Texture2D(960,720,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,960,720),0,0);image.Apply();File.WriteAllBytes(Path.Combine(Output,filename),image.EncodeToPNG());
        camera.targetTexture=null;RenderTexture.active=previous;UnityEngine.Object.Destroy(image);UnityEngine.Object.Destroy(target);
    }
    public static void Build(){PlayerSettings.bundleVersion="0.50.0";AssetDatabase.SaveAssets();Babel.EditorTools.BabelSliceScaffolder.BuildWindowsPlayer();}
}
