using UnityEngine;
namespace Bable
{
    public sealed class RelicFX : MonoBehaviour
    {
        float started,lifetime;
        SpriteRenderer sprite;
        Vector3 originalScale;
        string artName;
        public static GameObject Burst(string art,Vector2 position,Vector2 direction,Vector2 size,float duration)
        {
            var asset=CombatVfxFrames.Get(art,0);if(asset==null)return null;
            var go=new GameObject(art+" effect");go.transform.position=position;
            go.transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg);
            go.transform.localScale=new Vector3(size.x/asset.bounds.size.x,size.y/asset.bounds.size.y,1);
            var s=go.AddComponent<SpriteRenderer>();s.sprite=asset;s.sortingOrder=25;
            var fx=go.AddComponent<RelicFX>();fx.artName=art;fx.sprite=s;fx.started=Time.time;fx.lifetime=duration;fx.originalScale=go.transform.localScale;return go;
        }
        public static void Dash(GameObject source,float duration)
        {var trail=source.GetComponent<DashAfterimages>();if(trail==null)trail=source.AddComponent<DashAfterimages>();trail.Begin(duration);}
        void Update()
        {
            float t=(Time.time-started)/Mathf.Max(.01f,lifetime);
            if(t>=1){Destroy(gameObject);return;}
            sprite.sprite=CombatVfxFrames.Get(artName,t<.18f?0:t<.35f?1:t<.75f?2:3);
            float formed=Mathf.SmoothStep(0,1,Mathf.Clamp01(t/.18f));
            float faded=1-Mathf.SmoothStep(0,1,Mathf.Clamp01((t-.65f)/.35f));
            sprite.color=new Color(1,1,1,formed*faded);
            transform.localScale=Vector3.Scale(originalScale,new Vector3(Mathf.Lerp(.72f,1,formed),Mathf.Lerp(.6f,1,formed),1));
        }
    }
}
