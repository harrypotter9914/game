using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Bable
{
    // Runs in the menu scene: no player, physics, session or HUD exists yet.
    public sealed class TowerPrologue : MonoBehaviour
    {
        [Serializable] public class Page { public string heading,text,art; public float seconds=12; }
        [Serializable] class Book { public Page[] pages; }
        Page[] pages;
        Action complete;
        TMP_Text heading,body,progress;
        RawImage art;
        CanvasGroup words;
        float elapsed;
        bool finished;
        AudioSource voice;
        float duration;
        public static bool IsNarrating {get;private set;}
        public int PageIndex {get;private set;}
        public int PageCount => pages.Length;
        public bool Finished => finished;
        public void Build(Action onComplete)
        {
            complete=onComplete;pages=JsonUtility.FromJson<Book>(Resources.Load<TextAsset>("Bable/Guidance/prologue").text).pages;
            voice=gameObject.AddComponent<AudioSource>();voice.playOnAwake=false;voice.spatialBlend=0;IsNarrating=true;
            art=Rect("Moving illustration",Vector2.zero,new Vector2(1600,900)).gameObject.AddComponent<RawImage>();art.raycastTarget=false;
            var shade=Rect("Caption veil",new Vector2(0,-334),new Vector2(1600,240)).gameObject.AddComponent<IlluminatedMenuPanel>();shade.color=new Color(.012f,.014f,.022f,.88f);shade.raycastTarget=false;
            for(int side=-1;side<=1;side+=2){var ornament=Rect("Quiet manuscript tracery",new Vector2(side*713,-335),new Vector2(152,152)).gameObject.AddComponent<Image>();ornament.sprite=Resources.Load<Sprite>("Bable/NewArt/Release51/RuneStar");ornament.color=new Color(.36f,.29f,.2f,.28f);ornament.raycastTarget=false;}
            var panel=Rect("Opening text",Vector2.zero,new Vector2(1600,900));words=panel.gameObject.AddComponent<CanvasGroup>();words.blocksRaycasts=false;
            heading=Text(panel,"Chapter",new Vector2(0,-250),new Vector2(1320,40),24,true);
            body=Text(panel,"Story",new Vector2(0,-315),new Vector2(1260,90),28,false);
            progress=Text(transform,"Progress",new Vector2(-530,-395),new Vector2(330,30),17,false);
            Control("ESC   SKIP OPENING",new Vector2(530,-395),Skip);
            Show();
        }
        RectTransform Rect(string name,Vector2 pos,Vector2 size){var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(transform,false);r.anchorMin=r.anchorMax=new Vector2(.5f,.5f);r.anchoredPosition=pos;r.sizeDelta=size;return r;}
        TMP_Text Text(Transform parent,string name,Vector2 pos,Vector2 size,int fontSize,bool title){var r=Rect(name,pos,size);r.SetParent(parent,false);var t=r.gameObject.AddComponent<TextMeshProUGUI>();t.font=Resources.Load<TMP_FontAsset>("Bable/GuideFonts/"+(title?"CinzelDecorative-Regular":"CrimsonText-Regular")+" SDF");t.fontSize=fontSize;t.text="";t.color=new Color(1,.93f,.76f);t.alignment=TextAlignmentOptions.Center;t.raycastTarget=false;return t;}
        void Control(string label,Vector2 pos,UnityEngine.Events.UnityAction action){var r=Rect(label,pos,new Vector2(315,52));var image=r.gameObject.AddComponent<Image>();image.color=new Color(.09f,.08f,.065f,1);var b=r.gameObject.AddComponent<Button>();b.onClick.AddListener(action);var hint=Text(r,label,Vector2.zero,new Vector2(310,48),19,false);hint.text=label;LiveInputHint.Attach(hint,label);}
        void Show()
        {
            elapsed=0;words.alpha=0;var p=pages[PageIndex];heading.text=p.heading;body.text=p.text;progress.text=$"PROLOGUE   {PageIndex+1} / {pages.Length}";
            voice.Stop();voice.clip=Resources.Load<AudioClip>("Bable/Dialogue/Voices/prologue_"+(PageIndex+1));voice.volume=BableAudio.MasterVolume*GameSettings.Current.voice;duration=voice.clip!=null?Mathf.Max(p.seconds,voice.clip.length+1.4f):p.seconds;if(voice.clip!=null)voice.PlayDelayed(.65f);
            var s=Resources.Load<Sprite>("Bable/"+p.art);art.texture=s.texture;
            art.uvRect=new Rect(0,0,1,1);
            // Fit source proportions; the slow zoom never stretches the artwork.
            float ratio=s.texture.width*art.uvRect.width/(s.texture.height*art.uvRect.height);
            float imageHeight=Mathf.Min(900,1600/ratio);art.rectTransform.sizeDelta=new Vector2(imageHeight*ratio,imageHeight);art.color=Color.white;
        }
        void Update(){if(finished||pages==null)return;elapsed+=Time.unscaledDeltaTime;voice.volume=BableAudio.MasterVolume*GameSettings.Current.voice;words.alpha=Mathf.SmoothStep(0,1,elapsed/.7f)*Mathf.SmoothStep(0,1,(duration-elapsed)/.6f);art.color=new Color(1,1,1,words.alpha);art.rectTransform.localScale=Vector3.one*(1+.012f*Mathf.Clamp01(elapsed/duration));art.rectTransform.anchoredPosition=new Vector2(Mathf.Sin(elapsed*.04f)*3,0);if(elapsed>.35f&&(Bable.GameInput.Down(Bable.GameAction.Pause)||Bable.GameInput.Down(Bable.GameAction.Back))){Skip();return;}if(elapsed>=duration)Advance();}
        void Advance(){if(finished)return;if(PageIndex+1>=pages.Length){Skip();return;}PageIndex++;Show();}
        public void Skip(){if(finished)return;finished=true;IsNarrating=false;voice.Stop();complete?.Invoke();}
        void OnDestroy(){IsNarrating=false;}
    }
}
