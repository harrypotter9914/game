using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
using Bable;
using Babel.Runtime.Characters.Player;
using Babel.Runtime.Characters.Enemies;
using Babel.Runtime.World;
using Babel.Runtime.Core;
using Babel.Runtime.Combat;
public static class BableThirteenthTests {
 static List<string> checks=new(),failures=new(),trace=new();
 static void Check(string label,bool passed){checks.Add(label);if(!passed)failures.Add(label);File.WriteAllText("../reference/revision13/tests.json",JsonUtility.ToJson(new BableVerification.Report{checks=checks.ToArray(),failures=failures.ToArray()},true));}
 public static void Run(){checks.Clear();failures.Clear();trace.Clear();new GameObject("Movement and pickup regression").AddComponent<BableTestHost>().StartCoroutine(Test());}
 static IEnumerator Test(){
  var ui=BableGameUI.Instance;ui.Begin();yield return new WaitForSeconds(.3f);
  var p=Object.FindFirstObjectByType<PlayerController2D>();var body=p.GetComponent<Rigidbody2D>();var health=p.GetComponent<HealthComponent>();var session=GameSession.Instance;
  foreach(var speech in Object.FindObjectsByType<BossSpeech>(FindObjectsSortMode.None))speech.enabled=false;TowerDialogue.AbortStory();
  foreach(var e in Object.FindObjectsByType<EnemyControllerBase>(FindObjectsSortMode.None)){e.enabled=false;e.GetComponent<Rigidbody2D>().simulated=false;}
  p.SetTestInput(0,0);health.Invincible=true;session.UnlockWeapon();
  p.transform.position=new Vector3(10,-10.1f,0);body.position=p.transform.position;body.linearVelocity=Vector2.zero;Physics2D.SyncTransforms();Camera.main.GetComponent<CameraFollow2D>().SetTarget(p.transform);
  yield return new WaitForSeconds(2);
  p.SetTestInput(1,0);float minY=100,maxY=-100,minSpeed=100,maxCameraStep=0,maxZoomStep=0;int grounded=0,total=0;float end=Time.time+3;Vector3 last=Camera.main.transform.position;float zoom=Camera.main.orthographicSize;
  while(Time.time<end){yield return new WaitForEndOfFrame();total++;grounded+=p.IsGrounded?1:0;minY=Mathf.Min(minY,body.position.y);maxY=Mathf.Max(maxY,body.position.y);if(total>15)minSpeed=Mathf.Min(minSpeed,body.linearVelocity.x);maxCameraStep=Mathf.Max(maxCameraStep,Vector3.Distance(last,Camera.main.transform.position)/Mathf.Max(.001f,Time.deltaTime));maxZoomStep=Mathf.Max(maxZoomStep,Mathf.Abs(zoom-Camera.main.orthographicSize)/Mathf.Max(.001f,Time.deltaTime));last=Camera.main.transform.position;zoom=Camera.main.orthographicSize;}
  trace.Add("Walk y variation="+(maxY-minY)+" minimum speed="+minSpeed+" grounded="+grounded+"/"+total+" camera speed="+maxCameraStep+" zoom rate="+maxZoomStep);
  Check("Flat ground walking has stable height",maxY-minY<.09f);Check("Ground contacts do not alternate during running",grounded>=total*.97f);Check("No repeated horizontal stalls on flat floor",minSpeed>p.Definition.MoveSpeed*.85f);Check("Camera follow remains rate limited",maxCameraStep<18.1f);Check("Camera zoom is gradual",maxZoomStep<.82f);
  p.SetTestInput(0,0);yield return new WaitForSeconds(.4f);p.SetTestInput(0,0,true);float maxJumpCamera=0;last=Camera.main.transform.position;end=Time.time+2;
  while(Time.time<end){yield return new WaitForEndOfFrame();maxJumpCamera=Mathf.Max(maxJumpCamera,Vector3.Distance(last,Camera.main.transform.position)/Mathf.Max(.001f,Time.deltaTime));last=Camera.main.transform.position;}
  Check("Jump does not hard switch the camera",maxJumpCamera<18.1f);BableVerification.Capture("revision13/walk-view.png");
  var sword=Object.FindObjectsByType<CollectiblePickup>(FindObjectsSortMode.None).First(c=>c.name.Contains("Sword"));p.transform.position=sword.transform.position;body.position=p.transform.position;body.linearVelocity=Vector2.zero;Physics2D.SyncTransforms();yield return new WaitForSeconds(.5f);
  Check("Star contact does not auto collect",sword!=null&&sword.RequiresInteraction&&ui.Mode=="play");
  var interactor=Object.FindFirstObjectByType<PickupInteractor>();interactor.RefreshTarget();Check("Nearby star has a selectable interaction target",interactor.Target==sword);
  Check("All eight original rune parchments load",new[]{"Romance","rings","Star","Sun","Flight","Woman","Harvest","Moon"}.All(id=>Resources.Load<Sprite>("Bable/images/hoxi "+id)!=null));
  sword.Interact(p.GetComponent<PlayerRuntimeState>());yield return null;yield return null;
  Check("Explicit interaction opens original sword parchment",ui.Mode=="scroll"&&ui.CurrentScrollId=="hoxi Sword");BableVerification.Capture("revision13/original-sword-scroll.png");ui.Resume();yield return new WaitForSeconds(.5f);
  var story=Object.FindObjectsByType<CollectiblePickup>(FindObjectsSortMode.None).First(c=>c.GetComponent<OriginalScroll>()!=null&&ScrollLibrary.TryGet(c.GetComponent<OriginalScroll>().art,out _));string storyId=story.GetComponent<OriginalScroll>().art;
  story.Interact(p.GetComponent<PlayerRuntimeState>());yield return null;yield return null;
  Check("Recovered story opens its own authored scroll",ui.CurrentScrollId==storyId&&ui.Mode=="scroll");Check("Story is recorded in journal",Object.FindFirstObjectByType<ScrollJournal>().recovered.Contains(storyId));BableVerification.Capture("revision13/original-story-scroll.png");ui.Resume();yield return new WaitForSeconds(.5f);
  var bridge=Object.FindFirstObjectByType<ScriptedBridgeCollapse>();session.SetHealth(2,session.MaxHealth);session.SetMana(1,session.MaxMana);p.transform.position=new Vector3(195,14,0);body.position=p.transform.position;body.linearVelocity=Vector2.zero;Physics2D.SyncTransforms();bridge.Trigger(p);end=Time.time+12;
  while(bridge.IsRunning&&Time.time<end)yield return null;
  Check("Bridge restores player control",!bridge.IsRunning&&p.enabled&&body.simulated);Check("Bridge preserves damaged health",health.CurrentHealth==2&&session.CurrentHealth==2);Check("Bridge preserves mana",session.CurrentMana==1);trace.Add("Bridge HP="+session.CurrentHealth+" MP="+session.CurrentMana+" position="+body.position);
  p.SetTestInput(1,0);yield return new WaitForSeconds(1.8f);p.SetTestInput(1,0,true);float jumpStart=body.position.y;yield return new WaitForSeconds(.25f);Check("Player can jump while pushing against the east seal",body.position.y>jumpStart+.5f);Check("Side wall contact is not mistaken for grounded feet",!p.IsGrounded);p.SetTestInput(0,0);yield return new WaitForSeconds(1);
  Object.FindFirstObjectByType<CheckpointService>().RegisterCheckpoint(Object.FindFirstObjectByType<CheckpointMarker>());Check("Registering a checkpoint does not refill resources",session.CurrentHealth==2&&session.CurrentMana==1);
  BableVerification.Capture("revision13/bridge-exit.png");
  File.WriteAllLines("../reference/revision13/runtime-trace.txt",trace);File.WriteAllText("../reference/revision13/finished.txt",failures.Count+" failures / "+checks.Count+" checks");
 }
}
