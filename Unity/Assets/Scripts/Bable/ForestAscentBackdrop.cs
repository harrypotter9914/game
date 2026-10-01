using UnityEngine;
namespace Bable
{
    // One distant image follows the view, with a constant aspect ratio and no tiling.
    [ExecuteAlways,DefaultExecutionOrder(100)]
    public sealed class ForestAscentBackdrop:MonoBehaviour
    {
        SpriteRenderer visual;
        Transform traveller;
        float opacity=-1;
        public float Opacity=>Mathf.Max(0,opacity);
        public static float VisibilityAt(Vector2 p)=>Mathf.SmoothStep(1,0,Mathf.InverseLerp(145,169,p.x))*Mathf.SmoothStep(1,0,Mathf.InverseLerp(59,69,p.y))*Mathf.SmoothStep(0,1,Mathf.InverseLerp(-26,-13,p.y));
        public float width=48; // Retained for compatibility with the original scene builder.
        void OnEnable(){visual=GetComponent<SpriteRenderer>();}
        void LateUpdate(){
            var camera=Camera.main;if(camera==null||visual==null||visual.sprite==null)return;
            var p=camera.transform.position;
            if(Application.isPlaying&&traveller==null)traveller=FindFirstObjectByType<Babel.Runtime.Characters.Player.PlayerController2D>()?.transform;
            float desired=VisibilityAt(traveller!=null?traveller.position:p);
            opacity=opacity<0||!Application.isPlaying?desired:Mathf.MoveTowards(opacity,desired,Time.deltaTime*1.2f);
            visual.enabled=opacity>.001f;
            visual.color=new Color(1,1,1,opacity);
            float height=camera.orthographicSize*2.04f;
            float uniform=Mathf.Max(height/visual.sprite.bounds.size.y,height*camera.aspect/visual.sprite.bounds.size.x);
            transform.localScale=Vector3.one*uniform;
            transform.position=new Vector3(p.x,p.y,0);
        }
    }
}
