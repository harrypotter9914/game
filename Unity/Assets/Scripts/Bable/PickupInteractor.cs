using UnityEngine;
using UnityEngine.UI;
using Babel.Runtime.Characters.Player;
using Babel.Runtime.World;
using Babel.Runtime.Core;
namespace Bable {
 [DefaultExecutionOrder(-100)]
 public sealed class PickupInteractor:MonoBehaviour {
  public static bool HasTarget {get;private set;}
  static int consumed=-1;
  public static bool ConsumedThisFrame=>consumed==Time.frameCount;
  public CollectiblePickup Target {get;private set;}
  PlayerRuntimeState player;Text prompt;Canvas canvas;float scan;
  CollectiblePickup dismissed;
  void Start(){
   player=FindFirstObjectByType<PlayerRuntimeState>();
   var go=new GameObject("Nearby relic prompt",typeof(Canvas),typeof(CanvasScaler));go.transform.SetParent(transform,false);
   canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=120;
   var scaler=go.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1600,900);
   var panel=new GameObject("Prompt",typeof(RectTransform),typeof(Image));panel.transform.SetParent(go.transform,false);
   var rt=panel.GetComponent<RectTransform>();rt.anchoredPosition=new Vector2(0,-325);rt.sizeDelta=new Vector2(720,54);panel.GetComponent<Image>().color=new Color(.045f,.035f,.055f,.88f);panel.GetComponent<Image>().raycastTarget=false;
   var label=new GameObject("E interact",typeof(RectTransform),typeof(Text));label.transform.SetParent(panel.transform,false);
   prompt=label.GetComponent<Text>();prompt.rectTransform.sizeDelta=new Vector2(690,50);prompt.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");prompt.fontSize=22;prompt.alignment=TextAnchor.MiddleCenter;prompt.color=new Color(.96f,.86f,.62f);prompt.raycastTarget=false;
   canvas.enabled=false;
  }
  public void RefreshTarget(){
   Target=null;if(player==null)return;
   float best=2.7f*2.7f;
   foreach(var pickup in FindObjectsByType<CollectiblePickup>(FindObjectsSortMode.None)){
    if(!pickup.RequiresInteraction)continue;
    Vector2 delta=pickup.transform.position-player.transform.position;
    if(delta.sqrMagnitude>=best)continue;
    bool blocked=false;foreach(var hit in Physics2D.LinecastAll(player.transform.position,pickup.transform.position))if(TerrainMotion.Solid(hit.collider)){blocked=true;break;}
    if(blocked)continue;best=delta.sqrMagnitude;Target=pickup;
   }
  }
  void Update(){
   bool available=!TowerLoading.Busy&&player!=null&&player.GetComponent<PlayerController2D>().enabled&&GameSession.Instance!=null&&!GameSession.Instance.IsPaused&&!TowerDialogue.StoryActive&&(BableGameUI.Instance==null||BableGameUI.Instance.Mode=="play");
   if(!available){HasTarget=false;if(canvas!=null)canvas.enabled=false;return;}
   scan-=Time.deltaTime;if(scan<=0){scan=.1f;RefreshTarget();}
   if(Target!=dismissed)dismissed=null;
   HasTarget=Target!=null&&Target.RequiresInteraction;
   if(canvas==null)return;canvas.enabled=HasTarget&&Target!=dismissed;
   if(!HasTarget)return;
   NarrativeGuidance.Instance?.Teach("interact","A memory within reach","Stars hold weapons, runes and memories. Open one deliberately, then close its parchment when you have finished reading.","E  Interact     Q  Close parchment");
   prompt.text=GameInput.Hint(GameAction.Interact)+"  "+Target.DisplayTitle+"    /    "+GameInput.Hint(GameAction.Back)+"  Cancel";
   if(Bable.GameInput.Down(Bable.GameAction.Back)&&!(NarrativeGuidance.Instance!=null&&NarrativeGuidance.Instance.CanDismiss)){dismissed=Target;canvas.enabled=false;return;}
   if(Bable.GameInput.Down(Bable.GameAction.Interact)){consumed=Time.frameCount;Target.Interact(player);Target=null;HasTarget=false;canvas.enabled=false;}
  }
  void OnDisable(){HasTarget=false;if(canvas!=null)canvas.enabled=false;}
 }
}
