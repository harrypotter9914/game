using UnityEngine;
using UnityEngine.UI;
using TMPro;
namespace Bable {
 public sealed class BossHealthHud:MonoBehaviour {
  static readonly string[] Names={"Abaddon","Korah","Azazel","Bel","Nero"};
  // Measured apertures of the five generated frames (bottom-origin normalized rectangles).
  static readonly Rect[] Apertures={new Rect(349f/2172,250f/724,1477f/2172,90f/724),new Rect(326f/2172,280f/724,1520f/2172,83f/724),new Rect(345f/2172,290f/724,1483f/2172,92f/724),new Rect(283f/2172,241f/724,1608f/2172,103f/724),new Rect(308f/2172,237f/724,1557f/2172,92f/724)};
  static readonly Color[] Colors={new Color(.71f,.12f,.065f),new Color(.59f,.13f,.10f),new Color(.73f,.16f,.17f),new Color(.65f,.10f,.24f),new Color(.80f,.075f,.085f)};
  RectTransform root,channel,fill,echo,gleam;Image frame;TMP_Text title;CanvasGroup group;BossEncounter current;float fraction=1,delayed=1,delayUntil,deathUntil;float width;
  Babel.Runtime.Combat.HealthComponent boundHealth;
  void Unbind(){if(boundHealth!=null)boundHealth.Died-=TargetDied;boundHealth=null;}
  void OnDestroy(){Unbind();}
  void TargetDied(){fraction=0;delayUntil=Time.time+.1f;deathUntil=Time.time+.65f;SetWidths();}
  public float HealthFraction=>fraction;
  public string FrameName=>frame!=null&&frame.sprite!=null?frame.sprite.name:"";
  public bool Visible=>group!=null&&group.alpha>.01f&&gameObject.activeInHierarchy;
  public RectTransform FrameRect=>root;
  void Awake(){
   root=Rect("Boss health",transform,new Vector2(0,-330),new Vector2(900,180));group=root.gameObject.AddComponent<CanvasGroup>();group.blocksRaycasts=false;group.alpha=0;
   channel=Rect("Health channel",root,Vector2.zero,Vector2.one);Paint(channel,new Color(.055f,.012f,.015f,.97f));
   echo=Strip("Recent damage",channel,new Color(.87f,.61f,.31f));fill=Strip("Remaining vitality",channel,Colors[0]);
   var shade=Strip("Lower bevel",fill,new Color(.18f,.01f,.015f,.48f));shade.anchorMin=new Vector2(0,0);shade.anchorMax=new Vector2(1,.28f);shade.offsetMin=shade.offsetMax=Vector2.zero;
   gleam=Strip("Upper glint",fill,new Color(1,.75f,.53f,.45f));gleam.anchorMin=new Vector2(0,.8f);gleam.anchorMax=new Vector2(1,1);gleam.offsetMin=gleam.offsetMax=Vector2.zero;
   frame=Paint(Rect("Guardian ornament",root,Vector2.zero,root.sizeDelta),Color.white);
   var label=Rect("Guardian title",root,new Vector2(0,102),new Vector2(1000,34));title=label.gameObject.AddComponent<TextMeshProUGUI>();title.font=Resources.Load<TMP_FontAsset>("Bable/GuideFonts/CinzelDecorative-Regular SDF");title.fontSize=21;title.fontStyle=FontStyles.Bold;title.alignment=TextAlignmentOptions.Center;title.color=new Color(.95f,.86f,.64f);title.outlineWidth=.18f;title.outlineColor=new Color(.05f,.035f,.03f);title.raycastTarget=false;
  }
  static RectTransform Rect(string n,Transform parent,Vector2 p,Vector2 size){var rt=new GameObject(n,typeof(RectTransform)).GetComponent<RectTransform>();rt.SetParent(parent,false);rt.anchoredPosition=p;rt.sizeDelta=size;return rt;}
  static Image Paint(RectTransform rt,Color c){var i=rt.gameObject.AddComponent<Image>();i.color=c;i.raycastTarget=false;return i;}
  static RectTransform Strip(string n,RectTransform parent,Color c){var rt=Rect(n,parent,Vector2.zero,parent.sizeDelta);rt.anchorMin=new Vector2(0,.5f);rt.anchorMax=new Vector2(0,.5f);rt.pivot=new Vector2(0,.5f);Paint(rt,c);return rt;}
  void Bind(BossEncounter b){Unbind();current=b;boundHealth=b.Health;boundHealth.Died+=TargetDied;var brain=b.GetComponent<BossBrain>();int index=brain!=null?(int)brain.profile.kind:0;index=Mathf.Clamp(index,0,4);frame.sprite=Resources.Load<Sprite>("Bable/NewArt/BossHud44_"+Names[index]);var r=Apertures[index];channel.anchoredPosition=new Vector2((r.center.x-.5f)*900,(r.center.y-.5f)*180);channel.sizeDelta=new Vector2(r.width*900,r.height*180);width=channel.sizeDelta.x;fill.GetComponent<Image>().color=Colors[index];title.text=b.title;fraction=delayed=Mathf.Clamp01(b.Health.CurrentHealth/(float)Mathf.Max(1,b.Health.MaxHealth));deathUntil=0;SetWidths();}
  void SetWidths(){fill.sizeDelta=new Vector2(width*fraction,channel.sizeDelta.y);echo.sizeDelta=new Vector2(width*delayed,channel.sizeDelta.y);}
  void Update(){
   var active=BossEncounter.Active;
   if(active!=null&&active.Health!=null){if(active!=current)Bind(active);float next=Mathf.Clamp01(active.Health.CurrentHealth/(float)Mathf.Max(1,active.Health.MaxHealth));if(next<fraction)delayUntil=Time.time+.25f;if(next>fraction)delayed=next;fraction=next;deathUntil=0;group.alpha=Mathf.MoveTowards(group.alpha,1,Time.unscaledDeltaTime*4);}
   else if(current!=null&&current.Health!=null&&current.Health.CurrentHealth==0){if(deathUntil==0)deathUntil=Time.time+.65f;fraction=0;if(Time.time>deathUntil)group.alpha=Mathf.MoveTowards(group.alpha,0,Time.unscaledDeltaTime*4);}
   else {group.alpha=0;current=null;}
   if(current!=null){var brain=current.GetComponent<BossBrain>();if(brain!=null){title.text=current.title+(brain.PhaseTwo?"  ·  II":"");if(brain.PhaseTwo)fill.GetComponent<Image>().color=new Color(.94f,.27f,.065f);}}
   if(Time.time>delayUntil)delayed=Mathf.MoveTowards(delayed,fraction,Time.deltaTime*1.7f);SetWidths();
  }
 }
}

