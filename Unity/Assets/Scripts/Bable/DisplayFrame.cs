using UnityEngine;
using UnityEngine.UI;
using TMPro;
namespace Bable {
 [DefaultExecutionOrder(30000)]
 public sealed class DisplayFrame:MonoBehaviour {
  public static Rect Viewport {get;private set;}=new Rect(0,0,1,1);
  public static float UIScale {get;private set;}=1;
  #if UNITY_EDITOR
  public static Vector2Int CaptureSize;
  #endif
  static DisplayFrame instance;float scan;
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
  static void Install(){instance=null;var g=new GameObject("Responsive presentation");DontDestroyOnLoad(g);instance=g.AddComponent<DisplayFrame>();instance.Build();}
  void Build(){Canvas.willRenderCanvases+=Fit;Fit();}
  void OnDestroy(){Canvas.willRenderCanvases-=Fit;}
  void LateUpdate(){Fit();if(Input.GetKeyDown(KeyCode.F11)||(Input.GetKey(KeyCode.LeftAlt)&&Input.GetKeyDown(KeyCode.Return))){GameSettings.Current.fullscreen=!Screen.fullScreen;GameSettings.Apply(true);GameSettings.Save();}}
  public static Rect Calculate(int width,int height)=>new Rect(0,0,1,1);
  void Fit(){
   int width=Screen.width,height=Screen.height;
   #if UNITY_EDITOR
   if(CaptureSize.x>0){width=CaptureSize.x;height=CaptureSize.y;}
   #endif
   Viewport=Calculate(width,height);UIScale=Mathf.Max(.01f,Mathf.Min(width/1600f,height/900f));
   foreach(var c in Camera.allCameras)if(c.targetTexture==null&&c.CompareTag("MainCamera"))c.rect=Viewport;
   foreach(var c in FindObjectsByType<Canvas>(FindObjectsSortMode.None)){
    if(!c.isRootCanvas||c.renderMode==RenderMode.WorldSpace)continue;
    var scaler=c.GetComponent<CanvasScaler>();if(scaler!=null)scaler.enabled=false;c.scaleFactor=UIScale;
   }
   if(Time.unscaledTime<scan)return;scan=Time.unscaledTime+.2f;
   foreach(var b in FindObjectsByType<Button>(FindObjectsSortMode.None)){
    foreach(var t in b.GetComponentsInChildren<Text>(true)){t.font=MenuTypography.Font;t.resizeTextForBestFit=true;t.resizeTextMinSize=Mathf.Min(15,t.fontSize);t.resizeTextMaxSize=t.fontSize;}
    foreach(var t in b.GetComponentsInChildren<TMP_Text>(true)){t.font=MenuTypography.Sdf;t.enableAutoSizing=true;t.fontSizeMin=Mathf.Min(15,t.fontSize);t.fontSizeMax=t.fontSize;}
   }
  }
 }
 public static class MenuTypography {
  static Font font;static TMP_FontAsset sdf;
  public static Font Font=>font!=null?font:(font=Resources.Load<Font>("Bable/GuideFonts/UncialAntiqua-Regular"));
  public static TMP_FontAsset Sdf {get{if(sdf==null){sdf=TMP_FontAsset.CreateFontAsset(Font);sdf.name="Babel Uncial menu";sdf.isMultiAtlasTexturesEnabled=true;}return sdf;}}
 }
}
