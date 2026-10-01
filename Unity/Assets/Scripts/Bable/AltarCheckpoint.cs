using System.Collections;
using UnityEngine;
using Babel.Runtime.Characters.Player;
using Babel.Runtime.Combat;
using Babel.Runtime.Core;
using Babel.Runtime.World;
namespace Bable {
 [RequireComponent(typeof(CheckpointMarker))]
 public sealed class AltarCheckpoint:MonoBehaviour {
  public float visualHeight=3;public bool IsActive{get;private set;}
  SpriteRenderer shrine;Mesh flameMesh;GameObject flame;float activatedAt;Material flameMaterial;
  public CheckpointMarker Marker=>GetComponent<CheckpointMarker>();
  void Awake(){PrepareVisual();}
  public void PrepareVisual(){
   var child=transform.Find("Altar artwork");if(child==null){child=new GameObject("Altar artwork").transform;child.SetParent(transform,false);}
   shrine=child.GetComponent<SpriteRenderer>();if(shrine==null)shrine=child.gameObject.AddComponent<SpriteRenderer>();shrine.sprite=Resources.Load<Sprite>("Bable/NewArt/CheckpointAltar");shrine.sortingOrder=4;shrine.color=new Color(.55f,.57f,.62f,1);child.localPosition=Vector3.zero;child.localScale=Vector3.one*visualHeight/3;
  }
  void Start(){
   flame=new GameObject("Altar votive flame",typeof(MeshFilter),typeof(MeshRenderer));flame.transform.SetParent(transform,false);flame.transform.localPosition=Vector3.up*visualHeight*.42f;
   flameMesh=new Mesh{name="Living altar flame"};flame.GetComponent<MeshFilter>().sharedMesh=flameMesh;flameMaterial=new Material(Shader.Find("Sprites/Default"));var r=flame.GetComponent<MeshRenderer>();r.sharedMaterial=flameMaterial;r.sortingOrder=6;flame.SetActive(false);
  }
  void OnTriggerStay2D(Collider2D other){var player=other.GetComponentInParent<PlayerController2D>();if(player==null||!player.IsGrounded||GameSession.Instance==null||GameSession.Instance.IsPaused||TowerDialogue.StoryActive)return;var b=player.GetComponent<Collider2D>().bounds;if(Mathf.Abs(b.min.y-transform.position.y)<.3f&&Mathf.Abs(player.transform.position.x-transform.position.x)<1.2f)Activate();}
  public void Activate(){var service=FindFirstObjectByType<CheckpointService>();if(service==null)return;if(IsActive&&service.CurrentCheckpoint==Marker)return;IsActive=true;activatedAt=Time.time;service.RegisterCheckpoint(Marker);CombatAudio.Play("star",transform.position,.5f);NarrativeGuidance.Notice("ALTAR AWAKENED\nYour return point is bound to this flame. Your vitality and spirit are unchanged.",5,true);}
  void Update(){
   if(shrine==null)return;float f=IsActive?Mathf.Clamp01((Time.time-activatedAt)/.65f):0;shrine.color=Color.Lerp(new Color(.55f,.57f,.62f,1),Color.white,f);
   if(flame==null)return;flame.SetActive(IsActive);if(!IsActive)return;
   const int count=20;var vertices=new Vector3[count+1];var colors=new Color[count+1];var triangles=new int[count*3];vertices[0]=new Vector3(0,.25f,0);colors[0]=new Color(.92f,.91f,.6f,.95f*f);
   for(int i=0;i<count;i++){float a=i*Mathf.PI*2/count;float y=(Mathf.Sin(a)+1)*.33f;float width=.17f*(1-y/.85f);vertices[i+1]=new Vector3(Mathf.Cos(a)*width+Mathf.Sin(Time.time*4+y*8)*.025f*y,y,0);colors[i+1]=new Color(.25f,.75f,1,.15f*f);triangles[i*3]=0;triangles[i*3+1]=i+1;triangles[i*3+2]=(i+1)%count+1;}
   flameMesh.Clear();flameMesh.vertices=vertices;flameMesh.colors=colors;flameMesh.triangles=triangles;flameMesh.RecalculateBounds();
  }
  public Vector2 PlayerPosition(PlayerController2D player){var shape=player.GetComponent<BoxCollider2D>();float offset=shape.offset.y*player.transform.lossyScale.y;return (Vector2)transform.position+Vector2.up*(shape.bounds.extents.y-offset+.025f);}
  void OnDestroy(){if(flameMesh!=null)Destroy(flameMesh);if(flameMaterial!=null)Destroy(flameMaterial);}
 }
 public sealed class AltarRebirth:MonoBehaviour {
  public bool IsRunning{get;private set;}
  public void Begin(bool alignCamera=true){if(!IsRunning)StartCoroutine(Rise(alignCamera));}
  IEnumerator Rise(bool alignCamera){
   IsRunning=true;var player=GetComponent<PlayerController2D>();var combat=GetComponent<PlayerCombatController>();var heal=GetComponent<PlayerHealChannelController>();var body=GetComponent<Rigidbody2D>();var health=GetComponent<HealthComponent>();var art=GetComponent<CharacterPresentation>();
   bool oldPlayer=player.enabled,oldCombat=combat.enabled,oldHeal=heal.enabled,oldInvulnerable=health.Invincible;
   player.enabled=false;combat.enabled=false;heal.enabled=false;body.linearVelocity=Vector2.zero;body.simulated=false;health.Invincible=true;Physics2D.SyncTransforms();
   if(alignCamera)Camera.main.GetComponent<CameraFollow2D>()?.SetTarget(transform);art.Act("bridgeimpact",.35f);CombatAudio.Play("heal",transform.position,.6f);yield return new WaitForSeconds(.35f);art.Act("bridgerise",.65f);yield return new WaitForSeconds(.65f);
   art.ReleaseAction();body.simulated=true;player.enabled=oldPlayer;combat.enabled=oldCombat;heal.enabled=oldHeal;health.Invincible=oldInvulnerable;IsRunning=false;
  }
 }
}
