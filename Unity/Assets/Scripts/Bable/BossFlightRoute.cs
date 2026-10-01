using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
namespace Bable {
 public sealed class BossFlightRoute:MonoBehaviour {
  BossBrain boss;Rigidbody2D body;BoxCollider2D shape;Tilemap map;Transform target;
  readonly Queue<Vector2> path=new();readonly List<Bounds> blockers=new();int corner;float retry,nextPlan;Vector2 plannedTarget;
  public int PlansMade{get;private set;}public int AvoidedObstacles{get;private set;}
  void Awake(){boss=GetComponent<BossBrain>();body=GetComponent<Rigidbody2D>();shape=GetComponent<BoxCollider2D>();map=GameObject.Find("GroundTilemap")?.GetComponent<Tilemap>();}
  public void ResetRoute(){path.Clear();corner=0;retry=nextPlan=0;}
  bool Free(Vector2 root){Vector2 p=root+Vector2.Scale(shape.offset,shape.transform.lossyScale),half=TerrainMotion.Size(shape)/2+Vector2.one*.06f;if(Mathf.Abs(p.x-boss.arenaCenter.x)+half.x>boss.arenaSize.x/2||Mathf.Abs(p.y-boss.arenaCenter.y)+half.y>boss.arenaSize.y/2)return false;for(int x=Mathf.FloorToInt(p.x-half.x);x<=Mathf.FloorToInt(p.x+half.x);x++)for(int y=Mathf.FloorToInt(p.y-half.y);y<=Mathf.FloorToInt(p.y+half.y);y++)if(map.HasTile(new Vector3Int(x,y,0)))return false;var bounds=new Bounds(p,half*2);foreach(var b in blockers)if(b.Intersects(bounds))return false;return true;}
  Vector2 Point(Vector2Int c)=>new Vector2(c.x+.5f,c.y+.5f);
  void Plan(){
   path.Clear();blockers.Clear();foreach(var c in FindObjectsByType<Collider2D>(FindObjectsSortMode.None))if(TerrainMotion.Solid(c)&&c.GetComponent<Tilemap>()==null&&c.GetComponent<CompositeCollider2D>()==null)blockers.Add(c.bounds);
   var corners=new[]{new Vector2(-5,4),new Vector2(5,4),new Vector2(5,-1),new Vector2(-5,-1)};Vector2 focus=target!=null?(Vector2)target.position:boss.arenaCenter;plannedTarget=focus;Vector2 goal=focus+corners[corner%4];
   Vector2Int start=default;float best=float.MaxValue;for(int x=Mathf.FloorToInt(boss.arenaCenter.x-boss.arenaSize.x/2);x<boss.arenaCenter.x+boss.arenaSize.x/2;x++)for(int y=Mathf.FloorToInt(boss.arenaCenter.y-boss.arenaSize.y/2);y<boss.arenaCenter.y+boss.arenaSize.y/2;y++){var c=new Vector2Int(x,y);if(!Free(Point(c)))continue;float d=(Point(c)-(Vector2)transform.position).sqrMagnitude;if(d<best){best=d;start=c;}}
   if(best==float.MaxValue)return;var open=new Queue<Vector2Int>();var parents=new Dictionary<Vector2Int,Vector2Int>();open.Enqueue(start);parents[start]=start;var end=start;best=(Point(start)-goal).sqrMagnitude;var directions=new[]{Vector2Int.up,Vector2Int.right,Vector2Int.down,Vector2Int.left};while(open.Count>0){var c=open.Dequeue();float d=(Point(c)-goal).sqrMagnitude;if(d<best){best=d;end=c;}foreach(var dir in directions){var n=c+dir;if(parents.ContainsKey(n)||!Free(Point(n)))continue;parents[n]=c;open.Enqueue(n);}}
   var reverse=new List<Vector2>();var node=end;while(node!=start){reverse.Add(Point(node));node=parents[node];}reverse.Add(Point(start));reverse.Reverse();foreach(var p in reverse)path.Enqueue(p);corner=(corner+1)%4;PlansMade++;nextPlan=Time.time+1.2f;
  }
  public void Move(float speed,Transform follow=null){if(map==null)return;target=follow;if(target!=null&&Time.time>=nextPlan&&Vector2.Distance(plannedTarget,target.position)>2)path.Clear();while(path.Count>0&&Vector2.Distance(transform.position,path.Peek())<.16f)path.Dequeue();if(path.Count==0&&Time.time>=retry){Plan();retry=Time.time+.3f;}if(path.Count==0){body.linearVelocity=Vector2.zero;return;}Vector2 delta=path.Peek()-(Vector2)transform.position;Vector2 movement=delta.normalized*Mathf.Min(speed*Time.fixedDeltaTime,delta.magnitude);if(TerrainMotion.Sweep(shape,movement,out var distance)){AvoidedObstacles++;path.Clear();body.linearVelocity=Vector2.zero;retry=Time.time+.15f;return;}body.linearVelocity=delta.normalized*Mathf.Min(speed,delta.magnitude/Time.fixedDeltaTime);}
 }
}
