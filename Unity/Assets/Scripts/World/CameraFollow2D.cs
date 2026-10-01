using UnityEngine;
namespace Babel.Runtime.World {
 public sealed class CameraFollow2D:MonoBehaviour {
  [SerializeField] Transform target;
  [SerializeField] Vector3 offset=new Vector3(0,0,-10);
  [SerializeField] float followSpeed=18;
  Vector3 dampVelocity,hitOffset;
  Vector3 Destination=>target.position+offset+Vector3.up*(GetComponent<Camera>().orthographicSize*.16f);
  void Start(){if(!enabled)return;var zoom=GetComponent<PassageVisibility>()??gameObject.AddComponent<PassageVisibility>();zoom.target=target;}
  void LateUpdate(){if(target==null)return;transform.position-=hitOffset;var desired=Destination;transform.position=Vector3.SmoothDamp(transform.position,desired,ref dampVelocity,.22f,Mathf.Infinity,Time.deltaTime);hitOffset=Bable.PlayerHitFeedback.CameraOffset;transform.position+=hitOffset;}
  public void SettleAtTarget(){if(target==null)return;dampVelocity=Vector3.zero;hitOffset=Vector3.zero;transform.position=Destination;}
  public void SetTarget(Transform value){target=value;dampVelocity=Vector3.zero;var zoom=GetComponent<PassageVisibility>();if(zoom!=null){zoom.target=target;zoom.ResetTracking();}SettleAtTarget();}
 }
}
