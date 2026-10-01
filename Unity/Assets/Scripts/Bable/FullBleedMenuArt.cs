using UnityEngine;
using UnityEngine.UI;
namespace Bable {
 // Art covers the window; menu controls remain in the safe, uniformly scaled content area.
 [RequireComponent(typeof(Image))]
 public sealed class FullBleedMenuArt:MonoBehaviour {
  Image art;RectTransform rect;
  void Awake(){art=GetComponent<Image>();rect=(RectTransform)transform;art.raycastTarget=false;art.preserveAspect=false;}
  void LateUpdate(){Fit();}
  public void Fit(){
   if(art==null||art.sprite==null||transform.parent==null)return;
   var size=((RectTransform)transform.parent).rect.size;var source=art.sprite.rect.size;
   float scale=Mathf.Max(size.x/source.x,size.y/source.y);
   rect.sizeDelta=source*scale;rect.anchoredPosition=Vector2.zero;
  }
 }
}
