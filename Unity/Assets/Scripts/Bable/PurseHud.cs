using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Babel.Runtime.Core;
namespace Bable {
 public sealed class PurseHud:MonoBehaviour {
  Sprite[] frames;Image coin;Text count;
  void Start(){frames=Resources.LoadAll<Sprite>("Bable/NewArt/VotiveAtlas").OrderBy(s=>s.name).Take(4).ToArray();coin=new GameObject("Rotating gold coin",typeof(RectTransform),typeof(Image)).GetComponent<Image>();coin.transform.SetParent(transform,false);coin.rectTransform.anchoredPosition=new Vector2(575,395);coin.rectTransform.sizeDelta=new Vector2(48,48);coin.preserveAspect=true;coin.raycastTarget=false;
   count=new GameObject("Gold amount",typeof(RectTransform),typeof(Text)).GetComponent<Text>();count.transform.SetParent(transform,false);count.rectTransform.anchoredPosition=new Vector2(687,395);count.rectTransform.sizeDelta=new Vector2(154,44);count.font=Resources.Load<Font>("Bable/GuideFonts/CrimsonText-Regular");count.fontSize=30;count.color=new Color(1,.86f,.5f);count.alignment=TextAnchor.MiddleRight;count.raycastTarget=false;count.rectTransform.pivot=new Vector2(1,.5f);count.rectTransform.anchoredPosition=new Vector2(764,395);count.gameObject.AddComponent<Shadow>().effectDistance=new Vector2(2,-2);
  }
  void Update(){if(frames.Length>0)coin.sprite=frames[(int)(Time.unscaledTime*8)%frames.Length];var s=GameSession.Instance;if(s!=null)count.text=FindFirstObjectByType<RuneCombatLab>()!=null?"∞":s.CurrentGold.ToString();float width=Mathf.Clamp(count.preferredWidth,14,154);count.rectTransform.sizeDelta=new Vector2(width,44);coin.rectTransform.anchoredPosition=new Vector2(764-width-6-24,395);}
 }
}
