using UnityEngine;
using System.Linq;
namespace Bable {
 [DefaultExecutionOrder(200)]
 public sealed class BurrowPresentation:MonoBehaviour {
  BossBrain brain;SpriteRenderer visual,mound;Vector3 home,scale;Sprite[] frames;
  public bool TrailVisible=>mound!=null&&mound.enabled;
  public string TrailArt=>mound!=null&&mound.sprite!=null?mound.sprite.name:"";
  void Start(){brain=GetComponent<BossBrain>();visual=GetComponent<CharacterPresentation>()?.animator?.GetComponent<SpriteRenderer>();if(visual!=null){home=visual.transform.localPosition;scale=visual.transform.localScale;}var go=new GameObject("Korah - heaving masonry");go.transform.SetParent(transform,false);mound=go.AddComponent<SpriteRenderer>();mound.sortingOrder=7;frames=Resources.LoadAll<Sprite>("Bable/NewArt/BurrowRubble44").OrderBy(s=>s.name).ToArray();}
  void LateUpdate(){
   if(brain==null||visual==null)return;
   bool live=GetComponent<Babel.Runtime.Combat.HealthComponent>().CurrentHealth>0;
   bool underground=brain.State=="Burrow",dig=brain.State=="Dig",emerge=brain.State=="EmergeWarning";
   mound.enabled=live&&!brain.DialogueHold&&brain.Engaged&&(underground||dig||emerge)&&frames.Length==4;
   if(frames.Length==4)mound.sprite=frames[Mathf.FloorToInt(Time.time*10)%4];
   // Match the real floor, keep a constant body scale and only animate the rubble frames.
   mound.transform.position=new Vector3(transform.position.x,transform.position.y+brain.FootOffset-.015f,transform.position.z);
   mound.transform.localScale=new Vector3(1/transform.lossyScale.x,1/transform.lossyScale.y,1);mound.color=new Color(.78f,.80f,.68f,1);
   float amount=brain.DialogueHold?1:underground||brain.State=="Dormant"?0:dig?1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(0,.85f,brain.StateProgress)):emerge?Mathf.SmoothStep(0,1,Mathf.InverseLerp(.3f,1,brain.StateProgress)):1;
   visual.enabled=live&&amount>.005f;
   visual.transform.localScale=new Vector3(scale.x,scale.y*Mathf.Max(.005f,amount),scale.z);
   float foot=transform.InverseTransformPoint(transform.position+Vector3.up*brain.FootOffset).y;
   visual.transform.localPosition=new Vector3(home.x,Mathf.Lerp(foot,home.y,amount),home.z);var tint=visual.color;tint.a=amount;visual.color=tint;
  }
 }
}
