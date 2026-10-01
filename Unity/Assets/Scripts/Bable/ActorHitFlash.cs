using UnityEngine;
namespace Bable {
    public sealed class ActorHitFlash:MonoBehaviour {
        SpriteRenderer source,overlay;Material material;float until;
        public static void Show(SpriteRenderer source){
            if(source==null||!source.enabled)return;var existing=source.GetComponentInChildren<ActorHitFlash>();
            if(existing!=null){existing.until=Time.time+.09f;return;}
            var shader=Resources.Load<Shader>("Bable/NewArt/HitFlash48");if(shader==null)return;
            var go=new GameObject("Confirmed hit silhouette");go.transform.SetParent(source.transform,false);
            var flash=go.AddComponent<ActorHitFlash>();flash.source=source;flash.overlay=go.AddComponent<SpriteRenderer>();
            flash.material=new Material(shader);flash.overlay.sharedMaterial=flash.material;flash.until=Time.time+.09f;flash.Sync();
        }
        void Sync(){overlay.sprite=source.sprite;overlay.flipX=source.flipX;overlay.flipY=source.flipY;overlay.sortingLayerID=source.sortingLayerID;overlay.sortingOrder=source.sortingOrder+1;overlay.color=new Color(1,1,1,Mathf.Clamp01((until-Time.time)/.09f)*.8f);}
        void LateUpdate(){if(source==null||Time.time>=until){Destroy(gameObject);return;}Sync();}
        void OnDestroy(){if(material!=null)Destroy(material);}
    }
}
