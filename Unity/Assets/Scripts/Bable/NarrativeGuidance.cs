using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Babel.Runtime.Core;
using Babel.Runtime.Characters.Player;
namespace Bable
{
    // Story hints share a single queue and never compete with spoken dialogue.
    public sealed class NarrativeGuidance : MonoBehaviour
    {
        [Serializable] public class Entry { public string id,title,text,controls; public int stage=-1; public float x,y,width,height; }
        [Serializable] class Catalog { public Entry[] entries; }
        class Pending { public Entry entry; public float seconds; public bool moment; }
        static NarrativeGuidance instance;
        readonly Dictionary<string,Entry> entries=new();
        readonly HashSet<string> seen=new();
        readonly List<Pending> queue=new();
        readonly HashSet<AbilityId> known=new();
        Canvas canvas; CanvasGroup group; RectTransform panel; TMP_Text heading,body,keys;
        PlayerController2D player; GameSession session; PrincessRescue princess; ScriptedBridgeCollapse bridge;
        bool weapon,fallen,ended; float elapsed,scan; float panelY=170; Pending current;
        public static NarrativeGuidance Instance=>instance;
        float nextPromptTime;
        public bool CanDismiss=>current!=null&&group!=null&&group.alpha>.05f&&!Busy;
        public bool DismissCurrent(){if(!CanDismiss)return false;current=null;group.alpha=0;nextPromptTime=Time.unscaledTime+.35f;return true;}
        public string CurrentId=>current?.entry.id;
        public int PendingCount=>queue.Count;
        public bool HasSeen(string id)=>seen.Contains(id);
        public string[] ExportSeen(){var delivered=new HashSet<string>(seen);foreach(var waiting in queue)delivered.Remove(waiting.entry.id);var a=new string[delivered.Count];delivered.CopyTo(a);return a;}
        public void RestoreSeen(string[] ids){seen.Clear();foreach(var id in ids??Array.Empty<string>())seen.Add(id);queue.Clear();current=null;weapon=session!=null&&session.HasWeapon;known.Clear();if(session!=null)foreach(AbilityId a in Enum.GetValues(typeof(AbilityId)))if(session.HasAbility(a))known.Add(a);}
        public float PanelAlpha=>group!=null?group.alpha:0;
        public int Stage=>session==null||!session.HasWeapon?0:!session.HasAbility(AbilityId.Shockwave)?1:!session.HasAbility(AbilityId.WallJump)?2:!session.HasAbility(AbilityId.DoubleJump)?3:!session.HasAbility(AbilityId.CrystalDash)?4:5;
        public string ObjectiveId=>ended?"dawn":Stage==0?"seek_blade":Stage==1?"blade":Stage==2?"shockwave":Stage==3?"walljump":Stage==4?"doublejump":"dash";
        void Awake(){
            instance=this;
            var json=Resources.Load<TextAsset>("Bable/Guidance/entries");if(json!=null)foreach(var e in JsonUtility.FromJson<Catalog>(json.text).entries)entries[e.id]=e;
            var root=new GameObject("Babel story guidance",typeof(Canvas),typeof(CanvasScaler));root.transform.SetParent(transform,false);canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=110;
            var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1600,900);scaler.matchWidthOrHeight=.5f;
            panel=new GameObject("Transparent bronze frame",typeof(RectTransform),typeof(CanvasGroup)).GetComponent<RectTransform>();panel.SetParent(root.transform,false);panel.sizeDelta=new Vector2(960,300);panel.anchoredPosition=new Vector2(0,-255);group=panel.GetComponent<CanvasGroup>();group.alpha=0;group.blocksRaycasts=false;group.interactable=false;
            var shade=Image("Smoked glass",panel,new Vector2(0,-5),new Vector2(853,169));shade.color=new Color(.025f,.04f,.065f,.87f);
            var border=Image("Generated gilded border",panel,Vector2.zero,panel.sizeDelta);border.sprite=Resources.Load<Sprite>("Bable/NewArt/GuidanceFrame");border.color=Color.white;
            heading=Label("Heading",new Vector2(0,58),new Vector2(790,40),27,"CinzelDecorative-Regular",new Color(.91f,.75f,.45f));
            body=Label("Story",new Vector2(0,0),new Vector2(790,70),25,"CrimsonText-Regular",new Color(.96f,.92f,.83f));
            var dismiss=Label("Dismiss hint",new Vector2(320,98),new Vector2(170,24),16,"CrimsonText-Regular",new Color(.9f,.83f,.65f));dismiss.text="Q  Dismiss";LiveInputHint.Attach(dismiss,dismiss.text);
            keys=Label("Controls",new Vector2(0,-50),new Vector2(790,30),21,"CrimsonText-Regular",new Color(.52f,.81f,.83f));
        }
        static Image Image(string name,Transform parent,Vector2 position,Vector2 size){var im=new GameObject(name,typeof(RectTransform),typeof(Image)).GetComponent<Image>();im.transform.SetParent(parent,false);im.rectTransform.anchoredPosition=position;im.rectTransform.sizeDelta=size;im.raycastTarget=false;return im;}
        TMP_Text Label(string name,Vector2 position,Vector2 size,int fontSize,string font,Color color){var t=new GameObject(name,typeof(RectTransform),typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();t.transform.SetParent(panel,false);t.rectTransform.anchoredPosition=position;t.rectTransform.sizeDelta=size;t.font=font!=null?Resources.Load<TMP_FontAsset>("Bable/GuideFonts/"+font+" SDF"):TMP_Settings.defaultFontAsset;t.fontSize=fontSize;t.alignment=TextAlignmentOptions.Center;t.color=color;t.raycastTarget=false;t.enableAutoSizing=true;t.fontSizeMin=fontSize-3;t.fontSizeMax=fontSize;return t;}
        void Start(){session=GameSession.Instance;player=FindFirstObjectByType<PlayerController2D>();princess=FindFirstObjectByType<PrincessRescue>();bridge=FindFirstObjectByType<ScriptedBridgeCollapse>();if(session!=null){weapon=session.HasWeapon;foreach(AbilityId a in Enum.GetValues(typeof(AbilityId)))if(session.HasAbility(a))known.Add(a);session.StateChanged+=Changed;}}
        bool Campaign=>UnityEngine.SceneManagement.SceneManager.GetActiveScene().name=="Gameplay_Main";
        void Changed(){
            if(!Campaign||session==null)return;
            if(!session.HasWeapon&&weapon){seen.Clear();queue.Clear();current=null;known.Clear();fallen=ended=false;}
            if(session.HasWeapon&&!weapon)Enqueue("blade");weapon=session.HasWeapon;
            foreach(AbilityId a in Enum.GetValues(typeof(AbilityId)))if(session.HasAbility(a)&&known.Add(a))Enqueue(a==AbilityId.Shockwave?"shockwave":a==AbilityId.WallJump?"walljump":a==AbilityId.DoubleJump?"doublejump":"dash");
        }
        public bool Teach(string id,string title,string text,string controls){
            id="tutorial_"+id;if(seen.Contains(id))return false;seen.Add(id);
            queue.Add(new Pending{entry=new Entry{id=id,title=title,text=text,controls=controls},seconds=11});return true;
        }
        public bool Enqueue(string id,bool repeat=false){if(!entries.TryGetValue(id,out var e)||(!repeat&&seen.Contains(id)))return false;seen.Add(id); if(e.width<=0){if(current?.entry.id=="notice")current=null;queue.RemoveAll(p=>p.entry.id=="notice");queue.Insert(0,new Pending{entry=e,seconds=Mathf.Max(7,e.text.Length*.055f+2)});}else queue.Add(new Pending{entry=e,seconds=Mathf.Max(7,e.text.Length*.055f+2)});return true;}
        public void Recall(){if(current?.entry.id==ObjectiveId)return;queue.RemoveAll(p=>p.entry.id==ObjectiveId);if(entries.TryGetValue(ObjectiveId,out var e))queue.Insert(0,new Pending{entry=e,seconds=9});}
        public static bool Notice(string text,float duration,bool priority=false){
            if(instance==null)return false;
            if(text.StartsWith("Unlocked ")||text.StartsWith("BLADE AWAKENED")||text.StartsWith("A forgotten blade"))return instance.Campaign;
            // Routine gold/health notices should never displace the route the player is reading.
            if(instance.current!=null&&(text.Contains(" gold")||text.StartsWith("Recovered")))return true;
            var parts=text.Split(new[]{'\n'},2);var e=new Entry{id="notice",title=parts.Length>1?parts[0]:"AN ECHO OF BABEL",text=parts.Length>1?parts[1]:parts[0],controls="G  Recall your path"};
            var notice=new Pending{entry=e,seconds=Mathf.Clamp(duration,3,8),moment=true};if(priority){instance.current=null;instance.queue.Insert(0,notice);}else if(instance.queue.Count<4)instance.queue.Add(notice);return true;
        }
        bool Busy=>TowerLoading.Busy||BableGameUI.Instance!=null&&BableGameUI.Instance.Mode!="play"||BossEncounter.Active!=null||TowerDialogue.StoryActive||TowerDialogue.IsSpeaking||princess!=null&&princess.IsRunning;
        bool ActionScene=>player!=null&&player.GetComponent<WeaponAwakening>()?.IsRunning==true||bridge!=null&&bridge.IsRunning;
        void Update(){
            if(session==null||player==null)return;
            bool paused=session.IsPaused||BableGameUI.Instance!=null&&BableGameUI.Instance.Mode!="play";canvas.enabled=!paused;
            if(paused)return;
            if(Campaign&&Bable.GameInput.Down(Bable.GameAction.Recall))Recall();
            if(Campaign&&Time.time>=scan){scan=Time.time+.3f;Scan();}
            if(Busy){group.alpha=0;return;}
            if(Bable.GameInput.Down(Bable.GameAction.Back)&&DismissCurrent())return;
            if(Time.unscaledTime<nextPromptTime)return;
            if(current!=null&&current.entry.stage>=0&&current.entry.stage!=Stage)current=null;
            if(current==null){
                if(queue.Count==0){group.alpha=0;return;}
                int index=queue.FindIndex(p=>(p.entry.stage<0||p.entry.stage==Stage)&&(!ActionScene||p.moment));if(index<0){queue.RemoveAll(p=>p.entry.stage>=0&&p.entry.stage!=Stage);return;}
                current=queue[index];queue.RemoveAt(index);elapsed=0;heading.text=current.entry.title;body.text=current.entry.text;LiveInputHint.Attach(keys,string.IsNullOrEmpty(current.entry.controls)?"G  Recall your path":current.entry.controls);body.text=InputPrompts.Prose(current.entry.text);
            }
            if(ActionScene&&!current.moment){group.alpha=0;return;}
            elapsed+=Time.deltaTime;group.alpha=Mathf.Min(Mathf.Clamp01(elapsed/.4f),Mathf.Clamp01((current.seconds-elapsed)/.65f)); panel.anchoredPosition=new Vector2(0,panelY-8*(1-group.alpha));
            if(elapsed>=current.seconds){current=null;group.alpha=0;}
        }
        void Scan(){
            if(!weapon)Enqueue("seek_blade");
            if(bridge!=null&&bridge.HasTriggered&&!bridge.IsRunning&&!fallen){fallen=true;Enqueue("fallen");}
            if(princess!=null&&princess.unlocked&&!ended){ended=true;Enqueue("dawn");}
            if(ActionScene||Busy||BossEncounter.Active!=null)return;
            var p=(Vector2)player.transform.position;
            foreach(var e in entries.Values)if(e.width>0&&(e.stage<0||e.stage==Stage)&&new Rect(e.x,e.y,e.width,e.height).Contains(p))Enqueue(e.id);
        }
        void OnDestroy(){if(session!=null)session.StateChanged-=Changed;if(instance==this)instance=null;}
    }
}

