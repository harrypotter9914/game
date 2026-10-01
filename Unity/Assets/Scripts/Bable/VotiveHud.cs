using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Babel.Runtime.Core;
namespace Bable
{
    public sealed class VotiveHud:MonoBehaviour
    {
        Image[] hp,mp;Sprite[] art;int maxHp=-1,maxMp=-1;Transform root;
        void Start(){art=Resources.LoadAll<Sprite>("Bable/NewArt/VotiveAtlas").OrderBy(s=>s.name).ToArray();root=transform;}
        void Update()
        {
            var s=GameSession.Instance;if(s==null||art==null||art.Length<16)return;
            if(maxHp!=s.MaxHealth||maxMp!=s.MaxMana){
                foreach(Transform child in transform)if(child.name=="Votive vitals")Destroy(child.gameObject);
                root=new GameObject("Votive vitals",typeof(RectTransform)).transform;root.SetParent(transform,false);
                maxHp=s.MaxHealth;maxMp=s.MaxMana;hp=Row(maxHp,385,art[8],art[9]);mp=Row(maxMp,317,art[10],art[11]);
                Caption("VITALITY",new Vector2(-623,421));Caption("SPIRIT",new Vector2(-623,357));
                for(int i=0;i<hp.Length;i++)hp[i].fillAmount=i<s.CurrentHealth?1:0;
                for(int i=0;i<mp.Length;i++)mp[i].fillAmount=i<s.CurrentMana?1:0;
            }
            Animate(hp,s.CurrentHealth,new Color(1,.4f,.32f));Animate(mp,s.CurrentMana,new Color(.45f,.8f,1));
        }
        Image[] Row(int count,float y,Sprite empty,Sprite full)
        {
            var result=new Image[count];float step=Mathf.Min(53,440f/Mathf.Max(1,count));
            for(int i=0;i<count;i++){
                Make("Empty socket",empty,new Vector2(-746+i*step,y),new Vector2(58,58));
                var fill=Make("Living essence",full,new Vector2(-746+i*step,y),new Vector2(58,58));fill.type=Image.Type.Filled;fill.fillMethod=Image.FillMethod.Vertical;fill.fillOrigin=0;result[i]=fill;
            }
            return result;
        }
        Image Make(string name,Sprite sprite,Vector2 pos,Vector2 size){var image=new GameObject(name,typeof(RectTransform),typeof(Image)).GetComponent<Image>();image.transform.SetParent(root,false);image.rectTransform.anchoredPosition=pos;image.rectTransform.sizeDelta=size;image.sprite=sprite;image.preserveAspect=true;image.raycastTarget=false;return image;}
        void Caption(string text,Vector2 pos){var label=new GameObject(text,typeof(RectTransform),typeof(Text)).GetComponent<Text>();label.transform.SetParent(root,false);label.rectTransform.anchoredPosition=pos;label.rectTransform.sizeDelta=new Vector2(280,22);label.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");label.text=text;label.fontSize=14;label.color=new Color(.85f,.72f,.45f);label.raycastTarget=false;}
        void Animate(Image[] images,int value,Color flash){for(int i=0;i<images.Length;i++){float target=i<value?1:0;bool changing=Mathf.Abs(images[i].fillAmount-target)>.01f;images[i].fillAmount=Mathf.MoveTowards(images[i].fillAmount,target,Time.deltaTime*2.5f);images[i].color=changing?Color.Lerp(Color.white,flash,.4f):Color.white;images[i].transform.localScale=Vector3.one*(changing?1.06f:1);}}
    }
}
