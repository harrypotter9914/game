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
public static class BableTwelfthTests {
 static List<string> checks=new(),failures=new();static List<string> trace=new();
 static void Check(string name,bool pass){checks.Add(name);if(!pass)failures.Add(name);File.WriteAllText("../reference/revision12/tests.json",JsonUtility.ToJson(new BableVerification.Report{checks=checks.ToArray(),failures=failures.ToArray()},true));}
 static bool Clear(BoxCollider2D shape){foreach(var c in Physics2D.OverlapBoxAll(shape.bounds.center,shape.bounds.size-Vector3.one*.08f,0))if(c!=shape&&TerrainMotion.Solid(c)){var d=Physics2D.Distance(shape,c);if(d.isOverlapped&&d.distance<-.075f)return false;}return true;}
 static float FloorGap(BoxCollider2D shape){float gap=10;var feet=shape.transform.TransformPoint(new Vector3(shape.offset.x,shape.offset.y-shape.size.y/2,0));var origin=(Vector2)feet+Vector2.up*.2f;foreach(var h in Physics2D.RaycastAll(origin,Vector2.down,1))if(TerrainMotion.Solid(h.collider))gap=Mathf.Min(gap,feet.y-h.point.y);return gap;}
 public static void Run(){if(!UnityEditor.EditorApplication.isPlaying)throw new System.Exception("Play mode required");checks.Clear();failures.Clear();trace.Clear();new GameObject("Animation and terrain audit").AddComponent<BableTestHost>().StartCoroutine(Test());}
 static IEnumerator Test(){
  BableGameUI.Instance.Begin();yield return new WaitForSeconds(.5f);var p=Object.FindFirstObjectByType<PlayerController2D>();var rb=p.GetComponent<Rigidbody2D>();var box=p.GetComponent<BoxCollider2D>();p.SetTestInput(0,0);p.GetComponent<HealthComponent>().Invincible=true;
  foreach(var s in Object.FindObjectsByType<BossSpeech>(FindObjectsSortMode.None))s.enabled=false;TowerDialogue.AbortStory();
  var enemies=Object.FindObjectsByType<EnemyControllerBase>(FindObjectsSortMode.None);foreach(var e in enemies){e.enabled=false;e.GetComponent<Rigidbody2D>().simulated=false;}
  p.transform.position=new Vector3(-22,-.8f,0);rb.linearVelocity=Vector2.zero;Physics2D.SyncTransforms();p.GetComponent<WeaponAwakening>().Begin();float end=Time.time+6;bool groundedReceive=true;int receive=0;
  while(p.GetComponent<WeaponAwakening>().IsRunning&&Time.time<end){yield return new WaitForEndOfFrame();if(!rb.simulated){receive++;groundedReceive&=Mathf.Abs(FloorGap(box))<.09f&&Clear(box);if(receive==8)BableVerification.Capture("revision12/sword-grounded.png");}}
  Check("Airborne sword pickup settles before receiving pose",receive>5&&groundedReceive);Check("Sword scene restores physics and input",rb.simulated&&p.enabled&&GameSession.Instance.HasWeapon);
  var bridge=Object.FindFirstObjectByType<ScriptedBridgeCollapse>();p.transform.position=new Vector3(195,14,0);rb.linearVelocity=Vector2.zero;Physics2D.SyncTransforms();bridge.Trigger(p);var phases=new HashSet<string>();var shots=new HashSet<string>();bool impactGround=true,collision=true;end=Time.time+10;
  while(bridge.IsRunning&&Time.time<end){yield return new WaitForEndOfFrame();phases.Add(bridge.Phase);if(bridge.Phase=="Impact"||bridge.Phase=="Rise"){impactGround&=Mathf.Abs(FloorGap(box))<.09f;trace.Add("Impact gap="+FloorGap(box)+" position="+p.transform.position);collision&=Clear(box);}if((bridge.Phase=="Impact"||bridge.Phase=="Rise"||bridge.Phase=="Approach")&&shots.Add(bridge.Phase))BableVerification.Capture("revision12/bridge-"+bridge.Phase+".png");}
  Check("Bridge uses stumble fall approach impact and rise",new[]{"Stumble","Fall","Approach","Impact","Rise"}.All(phases.Contains));Check("Two-foot impact and recovery remain on real ground",impactGround&&collision);Check("Bridge restores input after recovery",!bridge.IsRunning&&p.enabled&&rb.simulated);Check("Bridge floor remains removed",!GameObject.Find("GroundTilemap").GetComponent<UnityEngine.Tilemaps.Tilemap>().HasTile(bridge.firstCell));
  rb.simulated=false;
  foreach(var b in enemies.OfType<BossBrain>()){
   b.ResetEncounter();var br=b.GetComponent<Rigidbody2D>();var bc=b.GetComponent<BoxCollider2D>();b.enabled=true;if(b.profile.kind!=BossKind.Burrow)br.simulated=true;
   p.transform.position=b.transform.position+Vector3.right*5;var states=new HashSet<string>();bool clear=true,inside=true,burrowVisible=true,aligned=true;int samples=0,emerges=0;float travel=0;Vector2 previous=b.transform.position;end=Time.time+13;
   while(Time.time<end){yield return new WaitForFixedUpdate();samples++;states.Add(b.State);if(bc.enabled&&br.simulated)clear&=Clear(bc);inside&=b.InArena(b.transform.position);travel+=Vector2.Distance(previous,b.transform.position);previous=b.transform.position;
    if(samples==200){p.transform.position=b.transform.position+Vector3.left*6;b.GetComponent<HealthComponent>().Invincible=false;b.GetComponent<HealthComponent>().ApplyDamage(Mathf.Max(1,b.GetComponent<HealthComponent>().CurrentHealth/2+1));}
    if(b.State=="Burrow")burrowVisible&=b.GetComponent<BurrowPresentation>().TrailVisible;
    if(b.State=="EmergeWarning"&&b.StateProgress>.8f){emerges++;aligned&=Mathf.Abs(FloorGap(bc))<.1f;}
   }
   Check(b.profile.kind+" combat never embeds collider in terrain",clear&&samples>100);Check(b.profile.kind+" remains inside arena",inside);Check(b.profile.kind+" has active attacks and transitions",states.Count>=3);
   if(b.profile.kind==BossKind.Burrow){Check("Underground movement stays visible",burrowVisible&&states.Contains("Burrow"));Check("Emergence stays on supporting floor",aligned&&emerges>0);}
   if(b.profile.kind==BossKind.Crystal){Check("Flying boss travels and replans around platforms",travel>5&&b.GetComponent<BossFlightRoute>().PlansMade>1);}
   trace.Add(b.profile.kind+" samples="+samples+" states="+string.Join(",",states)+" travel="+travel);BableVerification.Capture("revision12/boss-"+b.profile.kind+".png");b.enabled=false;br.simulated=false;
  }
  foreach(var e in enemies.Where(e=>!(e is BossBrain)).GroupBy(e=>e.GetComponent<CharacterPresentation>().animator.runtimeAnimatorController.name).Select(g=>g.First())){
   var eb=e.GetComponent<Rigidbody2D>();var ec=e.GetComponent<BoxCollider2D>();e.enabled=true;eb.simulated=true;p.transform.position=e.transform.position+Vector3.right*2;bool clear=true;int grounded=0;var states=new HashSet<string>();end=Time.time+5;
   while(Time.time<end){yield return new WaitForFixedUpdate();clear&=Clear(ec);if(Mathf.Abs(FloorGap(ec))<.1f)grounded++;states.Add(e.BehaviourState);}
   Check(e.GetComponent<CharacterPresentation>().animator.runtimeAnimatorController.name+" fighting stays clear and grounded",clear&&grounded>30);trace.Add(e.name+" states="+string.Join(",",states));BableVerification.Capture("revision12/minion-"+e.GetComponent<CharacterPresentation>().animator.runtimeAnimatorController.name+".png");e.enabled=false;eb.simulated=false;
  }
  var flyer=enemies.OfType<BossBrain>().First(b=>b.profile.kind==BossKind.Crystal);flyer.ResetEncounter();flyer.arenaCenter=new Vector2(710,105);flyer.arenaSize=new Vector2(22,20);flyer.transform.position=new Vector3(703,105,0);flyer.GetComponent<Rigidbody2D>().position=flyer.transform.position;flyer.GetComponent<Rigidbody2D>().simulated=true;flyer.enabled=true;
  var obstacle=new GameObject("Dynamic flight obstacle");obstacle.transform.position=new Vector3(710,105,0);var ob=obstacle.AddComponent<BoxCollider2D>();ob.size=new Vector2(1,8);p.transform.position=new Vector3(716,106,0);Physics2D.SyncTransforms();bool avoids=true,crossed=false;end=Time.time+10;
  while(Time.time<end){yield return new WaitForFixedUpdate();avoids&=Clear(flyer.GetComponent<BoxCollider2D>());crossed|=flyer.transform.position.x>711.5f;}
  Check("Flying pursuit routes around a new solid obstacle",avoids&&crossed);flyer.enabled=false;flyer.GetComponent<Rigidbody2D>().simulated=false;Object.Destroy(obstacle);
  foreach(var dir in new[]{Vector2.right,Vector2.left,Vector2.up,Vector2.down}){
   Vector2 origin=new Vector2(850,100);var wall=new GameObject("Projectile stop wall");wall.transform.position=origin+dir*3;wall.AddComponent<BoxCollider2D>().size=Mathf.Abs(dir.x)>.5f?new Vector2(.3f,4):new Vector2(4,.3f);
   var target=new GameObject("Protected target");target.transform.position=origin+dir*4;target.AddComponent<CircleCollider2D>().radius=.2f;var probe=target.AddComponent<BableWaveProbe>();
   var shot=new GameObject("Projectile init test");shot.transform.position=origin;shot.AddComponent<SpriteRenderer>().sprite=p.GetComponent<CharacterPresentation>().animator.GetComponent<SpriteRenderer>().sprite;shot.AddComponent<CircleCollider2D>().isTrigger=true;shot.GetComponent<CircleCollider2D>().radius=.1f;var projectile=shot.AddComponent<EnemyProjectile>();projectile.Launch(dir,1,10,p.gameObject,TeamAlignment.Player);Physics2D.SyncTransforms();yield return new WaitForSeconds(.22f);
   Check("Projectile initializes rigidbody and travels "+dir,shot!=null&&shot.GetComponent<Rigidbody2D>()!=null&&Vector2.Distance(origin,shot.transform.position)>.5f);yield return new WaitForSeconds(.5f);Check("Projectile hits wall and dissolves without hitting behind "+dir,shot==null&&probe.damage==0);Object.Destroy(wall);Object.Destroy(target);yield return null;
  }
  File.WriteAllLines("../reference/revision12/runtime-trace.txt",trace);File.WriteAllText("../reference/revision12/finished.txt",failures.Count+" failures / "+checks.Count+" checks");
 }
}
