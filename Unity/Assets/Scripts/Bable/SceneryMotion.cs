using UnityEngine;
namespace Bable
{
    public sealed class SceneryMotion:MonoBehaviour
    {
        public bool banner;
        SpriteRenderer visual;Color tint;Vector3 scale;float phase;
        void Awake(){visual=GetComponent<SpriteRenderer>();tint=visual.color;scale=transform.localScale;phase=transform.position.x*.73f;}
        void Update(){if(banner)transform.localScale=new Vector3(scale.x*(1+Mathf.Sin(Time.time*1.5f+phase)*.025f),scale.y,scale.z);else{float f=.96f+.04f*Mathf.Sin(Time.time*5+phase);visual.color=new Color(tint.r*f,tint.g*f,tint.b*f,tint.a);}}
    }
}
