using UnityEngine;
using UnityEngine.Tilemaps;
namespace Bable {
 public static class TerrainMotion {
  public static bool Solid(Collider2D c){return c!=null&&c.enabled&&!c.isTrigger&&(c.GetComponent<Tilemap>()!=null||c.GetComponent<BreakableWall>()!=null||(c.attachedRigidbody==null||c.attachedRigidbody.bodyType==RigidbodyType2D.Static)&&c.GetComponentInParent<Babel.Runtime.Combat.HealthComponent>()==null);}
  public static Vector2 Size(BoxCollider2D shape)=>Vector2.Scale(shape.size,new Vector2(Mathf.Abs(shape.transform.lossyScale.x),Mathf.Abs(shape.transform.lossyScale.y)));
  public static bool Sweep(BoxCollider2D shape,Vector2 movement,out float distance){distance=movement.magnitude;bool hit=false;if(distance<.0001f)return false;foreach(var h in Physics2D.BoxCastAll(shape.bounds.center,Size(shape)-Vector2.one*.025f,0,movement.normalized,distance+.055f)){if(h.collider==shape||!Solid(h.collider)||h.distance<=.001f)continue;if(h.distance-.045f<distance){distance=Mathf.Max(0,h.distance-.045f);hit=true;}}return hit;}
  public static bool FindLanding(BoxCollider2D shape,Vector2 near,out Vector2 root){root=near;Vector2 offset=shape.transform.TransformVector(shape.offset);float best=float.MaxValue;foreach(var h in Physics2D.BoxCastAll(near+Vector2.up*2+offset,Size(shape)-Vector2.one*.02f,0,Vector2.down,10)){if(!Solid(h.collider)||h.distance<=.001f||h.distance>=best)continue;best=h.distance;root=near+Vector2.up*(2-h.distance+.025f);}return best<float.MaxValue;}
 }
 [DefaultExecutionOrder(150)]
 public sealed class ActorTerrainSweep:MonoBehaviour {
  Rigidbody2D body;BoxCollider2D shape;
  static PhysicsMaterial2D frictionless;
  void Awake(){
   body=GetComponent<Rigidbody2D>();shape=GetComponent<BoxCollider2D>();
   if(body!=null)body.collisionDetectionMode=CollisionDetectionMode2D.Continuous;
   if(frictionless==null)frictionless=new PhysicsMaterial2D("Actor sliding contact"){friction=0,bounciness=0};
   if(shape!=null)shape.sharedMaterial=frictionless;
  }
  // Dynamic bodies use the physics solver's continuous contacts. Shortening the
  // entire velocity at a floor contact also stopped horizontal travel every step.
  // Flying kinematic routing retains its explicit TerrainMotion.Sweep checks.
 }
}
