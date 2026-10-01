using System.Collections;
using System.Linq;
using UnityEngine;
namespace Bable
{
    public sealed class AnimatedTreasure:MonoBehaviour
    {
        public bool coin;
        SpriteRenderer visual;Sprite[] frames;Vector3 home,scale;bool collected;
        void Start(){PrepareVisual();}
        // Also called by the scene/prefab authoring pass. Editor and builds share the same art.
        public void PrepareVisual()
        {
            frames=Resources.LoadAll<Sprite>("Bable/NewArt/VotiveAtlas").OrderBy(s=>s.name).ToArray();
            if(frames.Length<16)return;
            var child=transform.Find("Treasure visual");
            if(child==null){var go=new GameObject("Treasure visual");go.transform.SetParent(transform,false);child=go.transform;}
            visual=child.GetComponent<SpriteRenderer>();if(visual==null)visual=child.gameObject.AddComponent<SpriteRenderer>();
            foreach(var sr in GetComponentsInChildren<SpriteRenderer>())if(sr!=visual)sr.enabled=false;
            visual.enabled=true;visual.color=Color.white;visual.sortingOrder=5;
            var art=child.gameObject;visual.sprite=frames[coin?0:4];float size=coin?1.5f:1.4f;art.transform.localScale=Vector3.one*size/visual.sprite.bounds.size.y;
            // Neutralise inherited pickup scale: the coin is always a small individual coin.
            art.transform.localScale=new Vector3(art.transform.localScale.x/transform.lossyScale.x,art.transform.localScale.y/transform.lossyScale.y,1);home=art.transform.localPosition;scale=art.transform.localScale;
        }
        void Update(){if(collected||visual==null||frames==null||frames.Length<16)return;visual.sprite=frames[(coin?0:4)+(int)(Time.time*(coin?8:5))%4];visual.transform.localPosition=home+Vector3.up*(Mathf.Sin(Time.time*2.5f)*.1f/transform.lossyScale.y);}
        public void Collect(){if(collected)return;collected=true;foreach(var c in GetComponentsInChildren<Collider2D>())c.enabled=false;StartCoroutine(Pickup());}
        IEnumerator Pickup(){for(float t=0;t<.4f;t+=Time.deltaTime){if(visual!=null&&frames.Length>=16){visual.sprite=frames[12+Mathf.Min(3,(int)(t/.1f))];visual.color=new Color(1,1,1,1-t/.4f);visual.transform.localScale=scale*(1+t);visual.transform.localPosition=home+Vector3.up*t;}yield return null;}Destroy(gameObject);}
    }
}
