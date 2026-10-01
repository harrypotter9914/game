using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Babel.Runtime.Core;
namespace Bable
{
    // Shared subtitle and voice channel: combat lines never interrupt a story exchange.
    public sealed class TowerDialogue : MonoBehaviour
    {
        [Serializable] public class Line { public string id,speaker,text; }
        [Serializable] public class Script { public Line[] lines; }
        static TowerDialogue instance;
        readonly Dictionary<string,Line> lines=new Dictionary<string,Line>();
        Canvas canvas; RectTransform panel; Text label; AudioSource voice; Transform speaker;
        float expires; bool modal; string currentId;
        public static bool StoryActive {get;private set;}
        public static string CurrentId => instance!=null?instance.currentId:null;
        public static bool IsSpeaking => instance!=null&&Time.time<instance.expires;
        public static void AbortStory(){StoryActive=false;if(instance!=null){instance.expires=0;instance.voice.Stop();}}
        public static TowerDialogue Instance {
            get {if(instance==null)instance=new GameObject("Tower dialogue").AddComponent<TowerDialogue>();return instance;}
        }
        void Awake()
        {
            instance=this;
            var data=Resources.Load<TextAsset>("Bable/Dialogue/lines");
            if(data!=null)foreach(var line in JsonUtility.FromJson<Script>(data.text).lines)lines[line.id]=line;
            canvas=gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=150;
            var scale=gameObject.AddComponent<CanvasScaler>();scale.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scale.referenceResolution=new Vector2(1600,900);scale.matchWidthOrHeight=.5f;
            panel=new GameObject("Gilded speech parchment",typeof(RectTransform),typeof(Image),typeof(Outline)).GetComponent<RectTransform>();panel.SetParent(transform,false);panel.sizeDelta=new Vector2(480,120);
            panel.GetComponent<Image>().color=new Color(.055f,.055f,.085f,.97f);var edge=panel.GetComponent<Outline>();edge.effectColor=new Color(.7f,.53f,.25f);edge.effectDistance=new Vector2(2,-2);
            label=new GameObject("English dialogue",typeof(RectTransform),typeof(Text)).GetComponent<Text>();label.transform.SetParent(panel,false);var rt=label.rectTransform;rt.anchorMin=Vector2.zero;rt.anchorMax=Vector2.one;rt.offsetMin=new Vector2(18,12);rt.offsetMax=new Vector2(-18,-12);
            label.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");label.fontSize=21;label.supportRichText=true;label.color=new Color(.96f,.9f,.75f);label.alignment=TextAnchor.MiddleCenter;label.raycastTarget=false;panel.GetComponent<Image>().raycastTarget=false;
            voice=gameObject.AddComponent<AudioSource>();voice.volume=.85f;voice.spatialBlend=0;panel.gameObject.SetActive(false);
        }
        public static float Speak(string id,Transform who=null,bool story=false)
        {
            var self=Instance;if(StoryActive&&!story)return 0;
            if(!self.lines.TryGetValue(id,out var line))return 0;
            var clip=Resources.Load<AudioClip>("Bable/Dialogue/Voices/"+id);
            float duration=Mathf.Max(3.2f,line.text.Length*.047f,clip!=null?clip.length+.4f:0);
            self.currentId=id;self.speaker=who;self.modal=story;self.expires=Time.time+duration;
            self.label.text="<color=#DAB876>"+line.speaker.ToUpperInvariant()+"</color>\n"+line.text;
            self.panel.gameObject.SetActive(true);self.voice.Stop();self.voice.clip=clip;if(clip!=null)self.voice.Play();return duration;
        }
        public static IEnumerator Exchange(Transform who,params string[] ids)
        {
            StoryActive=true;
            foreach(string id in ids){float seconds=Speak(id,who,true);yield return new WaitForSeconds(seconds);}
            StoryActive=false;
        }
        void LateUpdate()
        {
            voice.volume=BableAudio.MasterVolume*GameSettings.Current.voice*.85f;label.fontSize=Mathf.RoundToInt(21*GameSettings.Current.textScale);
            bool paused=GameSession.Instance!=null&&GameSession.Instance.IsPaused;
            canvas.enabled=!paused;
            if(paused)voice.Pause();else if(voice.clip!=null&&!voice.isPlaying&&Time.time<expires)voice.UnPause();
            if(Time.time>=expires){panel.gameObject.SetActive(false);return;}
            var root=(RectTransform)canvas.transform;
            if(modal){panel.sizeDelta=new Vector2(1000,150);panel.anchoredPosition=new Vector2(0,speaker!=null&&speaker.GetComponent<BossBrain>()!=null?270:-305);return;}
            panel.sizeDelta=new Vector2(480,120);
            if(speaker==null||Camera.main==null)return;
            float head=speaker.position.y+2.5f;foreach(var sprite in speaker.GetComponentsInChildren<SpriteRenderer>())if(sprite.enabled)head=Mathf.Max(head,sprite.bounds.max.y);
            var screen=Camera.main.WorldToScreenPoint(new Vector3(speaker.position.x,head,0));
            RectTransformUtility.ScreenPointToLocalPointInRectangle(root,screen,null,out var point);
            point.y+=panel.sizeDelta.y*.5f+28;
            point.x=Mathf.Clamp(point.x,-root.rect.width*.5f+250,root.rect.width*.5f-250);
            point.y=Mathf.Clamp(point.y,-root.rect.height*.5f+160,root.rect.height*.5f-155);panel.anchoredPosition=point;
        }
        void OnDestroy(){if(instance==this){instance=null;StoryActive=false;}}
    }
}
