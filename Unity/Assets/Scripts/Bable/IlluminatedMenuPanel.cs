using UnityEngine;
using UnityEngine.UI;
namespace Bable
{
    // Translucent ink washes soften into the illustration rather than covering it.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class IlluminatedMenuPanel : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=rectTransform.rect;const int n=20;
            for(int y=0;y<=n;y++)for(int x=0;x<=n;x++){
                float u=x/(float)n,v=y/(float)n;
                float edge=Mathf.Min(Mathf.Min(u,1-u)*r.width,Mathf.Min(v,1-v)*r.height);
                var c=color;c.a*=Mathf.SmoothStep(0,1,edge/34);
                vh.AddVert(new Vector3(r.xMin+u*r.width,r.yMin+v*r.height),c,new Vector2(u,v));
            }
            for(int y=0;y<n;y++)for(int x=0;x<n;x++){int i=y*(n+1)+x;vh.AddTriangle(i,i+n+1,i+1);vh.AddTriangle(i+1,i+n+1,i+n+2);}
        }
        static Sprite border;
        public static void Frame(RectTransform host,float alpha=.8f)
        {
            if(border==null){var s=Resources.Load<Sprite>("Bable/NewArt/MerchantIlluminatedFrame");border=Sprite.Create(s.texture,s.rect,new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,new Vector4(190,190,190,190));}
            var r=new GameObject("Illuminated gold and lapis border",typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(host,false);r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;
            var image=r.gameObject.AddComponent<Image>();image.sprite=border;image.type=Image.Type.Sliced;image.pixelsPerUnitMultiplier=2.8f;image.color=new Color(.9f,.85f,.75f,alpha);image.raycastTarget=false;
        }
    }
}
