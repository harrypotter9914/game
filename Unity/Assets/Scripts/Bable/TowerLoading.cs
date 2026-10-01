using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using Babel.Runtime.Core;
using Babel.Runtime.Characters.Player;
using Babel.Runtime.World;

namespace Bable {
 public sealed class TowerLoading : MonoBehaviour {
  static TowerLoading instance;
  public static bool Busy => instance!=null;
  CanvasGroup veil; Image bar; RectTransform seal; TMP_Text caption; float progress;
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
  static void Boot(){instance=null;Create().StartCoroutine(Initial());}
  static IEnumerator Initial(){yield return null;yield return instance.Reveal(false);}
  static TowerLoading Create(){var g=new GameObject("Tower loading curtain");DontDestroyOnLoad(g);instance=g.AddComponent<TowerLoading>();instance.Build();return instance;}
  public static void Load(string scene){if(Busy)return;Create().StartCoroutine(instance.Change(scene));}
  public static void Respawn(){if(Busy||BableGameUI.Instance!=null&&BableGameUI.Instance.CinematicActive)return;Create().StartCoroutine(instance.Return());}
  RectTransform Rect(Transform parent,string name,Vector2 position,Vector2 size){var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=new Vector2(.5f,.5f);r.anchoredPosition=position;r.sizeDelta=size;return r;}
  void Build(){
   var c=gameObject.AddComponent<Canvas>();c.renderMode=RenderMode.ScreenSpaceOverlay;c.sortingOrder=30000;gameObject.AddComponent<GraphicRaycaster>();var s=gameObject.AddComponent<CanvasScaler>();s.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;s.referenceResolution=new Vector2(1600,900);
   veil=gameObject.AddComponent<CanvasGroup>();var back=Rect(transform,"Black loading curtain",Vector2.zero,Vector2.zero);back.anchorMin=Vector2.zero;back.anchorMax=Vector2.one;back.offsetMin=back.offsetMax=Vector2.zero;back.gameObject.AddComponent<Image>().color=new Color(.012f,.015f,.024f,1);
   var plate=Rect(transform,"Manuscript cartouche",new Vector2(0,-230),new Vector2(660,230));IlluminatedMenuPanel.Frame(plate,.6f);
   seal=Rect(plate,"Turning illuminated seal",new Vector2(0,35),new Vector2(62,62));var im=seal.gameObject.AddComponent<Image>();im.sprite=Resources.Load<Sprite>("Bable/NewArt/Release51/RuneStar");im.color=new Color(.85f,.68f,.37f);im.preserveAspect=true;
   caption=Rect(plate,"Loading inscription",new Vector2(0,-6),new Vector2(540,40)).gameObject.AddComponent<TextMeshProUGUI>();caption.font=Resources.Load<TMP_FontAsset>("Bable/GuideFonts/CrimsonText-Regular SDF");caption.fontSize=25;caption.alignment=TextAlignmentOptions.Center;caption.color=new Color(.94f,.85f,.65f);caption.text="LOADING THE TOWER";
   var track=Rect(plate,"Gilded progress rule",new Vector2(0,-42),new Vector2(430,4));track.gameObject.AddComponent<Image>().color=new Color(.27f,.23f,.16f);bar=Rect(track,"Progress",Vector2.zero,new Vector2(430,4)).gameObject.AddComponent<Image>();bar.color=new Color(.88f,.71f,.4f);bar.rectTransform.pivot=new Vector2(0,.5f);bar.rectTransform.anchoredPosition=new Vector2(-215,0);
  }
  void Update(){seal.localRotation=Quaternion.Euler(0,0,Time.unscaledTime*-24);seal.localScale=Vector3.one*(1+.045f*Mathf.Sin(Time.unscaledTime*2));bar.rectTransform.sizeDelta=new Vector2(430*progress,4);}
  IEnumerator Change(string scene){
   Time.timeScale=0;yield return null;yield return null;
   if(GameSession.Instance!=null)Destroy(GameSession.Instance.gameObject);
   var op=SceneManager.LoadSceneAsync(scene);op.allowSceneActivation=false;
   while(op.progress<.9f){progress=Mathf.Clamp01(op.progress/.9f)*.7f;yield return null;}
   progress=.7f;yield return new WaitForSecondsRealtime(.25f);op.allowSceneActivation=true;while(!op.isDone)yield return null;
   yield return Reveal(false);
  }
  IEnumerator Return(){
   caption.text="RETURNING TO THE FLAME";Time.timeScale=0;yield return null;yield return new WaitForSecondsRealtime(.25f);
   BableGameUI.Instance.Resume();FindFirstObjectByType<PlayerRespawnController>()?.Respawn(false);yield return Reveal(true);
  }
  IEnumerator Reveal(bool rebirth){
   while(BableGameUI.Instance==null||!BableGameUI.Instance.Ready)yield return null;
   if(!rebirth)yield return CampaignStore.RestoreScene();
   Time.timeScale=1;var p=FindFirstObjectByType<PlayerController2D>();
   var hp=p!=null?p.GetComponent<Babel.Runtime.Combat.HealthComponent>():null;bool inv=hp!=null&&hp.Invincible;if(hp!=null)hp.Invincible=true;
   float stable=0,elapsed=0;bool ready=p==null;
   while(!ready&&elapsed<5){elapsed+=Time.unscaledDeltaTime;stable=p.IsGrounded&&Mathf.Abs(p.Velocity.y)<.15f?stable+Time.unscaledDeltaTime:0;ready=stable>.18f;progress=Mathf.Lerp(.72f,.94f,Mathf.Clamp01(elapsed));yield return null;}
   if(!ready){Debug.LogError("Loading: player failed to settle on terrain within five seconds.");}
   if(p!=null)Camera.main?.GetComponent<CameraFollow2D>()?.SetTarget(p.transform);
   yield return new WaitForSecondsRealtime(.35f);Camera.main?.GetComponent<CameraFollow2D>()?.SettleAtTarget();yield return null;progress=1;caption.text="";
   // Freeze the established pose while revealing it. Input remains blocked.
   Time.timeScale=0;
   for(float t=0;t<.65f;t+=Time.unscaledDeltaTime){veil.alpha=1-Mathf.SmoothStep(0,1,t/.65f);yield return null;}
   veil.alpha=0;Time.timeScale=1;if(hp!=null)hp.Invincible=inv;
   instance=null;
   if(rebirth&&p!=null)(p.GetComponent<AltarRebirth>()??p.gameObject.AddComponent<AltarRebirth>()).Begin(false);
   Destroy(gameObject);
  }
 }
}
