using UnityEngine;
namespace Bable
{
    [DefaultExecutionOrder(110)]
    public sealed class DawnRescueBackdrop:MonoBehaviour
    {
        SpriteRenderer visual;
        Transform traveller;
        bool island;
        float opacity=-1;
        public float Opacity=>Mathf.Max(0,opacity);
        public static float VisibilityAt(Vector2 p,bool island)=>Mathf.SmoothStep(0,1,Mathf.InverseLerp(64,74,p.y))*(island?(p.x<0?1:0):(p.x>=0?Mathf.SmoothStep(1,0,Mathf.InverseLerp(156,178,p.x)):0));
        void Awake(){visual=GetComponent<SpriteRenderer>();island=transform.position.x<0;}
        void LateUpdate(){var c=Camera.main;if(c==null||visual==null||visual.sprite==null)return;
            if(traveller==null)traveller=FindFirstObjectByType<Babel.Runtime.Characters.Player.PlayerController2D>()?.transform;
            float desired=VisibilityAt(traveller!=null?traveller.position:c.transform.position,island);
            opacity=opacity<0?desired:Mathf.MoveTowards(opacity,desired,Time.deltaTime*1.2f);
            visual.enabled=opacity>.001f;visual.color=new Color(1,1,1,opacity);
            float height=c.orthographicSize*2.04f;float uniform=Mathf.Max(height/visual.sprite.bounds.size.y,height*c.aspect/visual.sprite.bounds.size.x);
            transform.localScale=Vector3.one*uniform;transform.position=new Vector3(c.transform.position.x,c.transform.position.y,0);}
    }
}
