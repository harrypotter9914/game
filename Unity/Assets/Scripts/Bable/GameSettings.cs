using System;
using UnityEngine;
namespace Bable {
 [Serializable] public class SettingsData {public int version=1;public float master=.7f,music=1,effects=1,voice=1,shake=1,flash=1,textScale=1;public bool fullscreen=false,vsync=true;public int width=1280,height=720;}
 public static class GameSettings {
  public static SettingsData Current {get;private set;}=new SettingsData();
  public static string Error {get;private set;}="";
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]static void Boot(){Load();Apply(true);}
  public static void Load(){try{bool recovered;var json=LocalStorage.Read("settings.json",out recovered);Current=json==null?new SettingsData():JsonUtility.FromJson<SettingsData>(json);if(Current==null||Current.version!=1)Current=new SettingsData();Clamp();}catch{Current=new SettingsData();Error="Settings reset after an unreadable file.";}}
  static void Clamp(){var c=Current;c.master=Mathf.Clamp01(c.master);c.music=Mathf.Clamp01(c.music);c.effects=Mathf.Clamp01(c.effects);c.voice=Mathf.Clamp01(c.voice);c.shake=Mathf.Clamp01(c.shake);c.flash=Mathf.Clamp01(c.flash);c.textScale=Mathf.Clamp(c.textScale,.9f,1.25f);c.width=Mathf.Clamp(c.width,960,3840);c.height=Mathf.Clamp(c.height,540,2160);}
  public static bool Save(){try{Clamp();LocalStorage.Write("settings.json",JsonUtility.ToJson(Current));Error="";return true;}catch(Exception e){Error="Settings could not be saved: "+e.Message;return false;}}
  public static void Apply(bool display=false){Clamp();BableAudio.Volume(Current.master);QualitySettings.vSyncCount=Current.vsync?1:0;Application.targetFrameRate=Current.vsync?-1:120;if(display&&!Application.isBatchMode){Screen.SetResolution(Current.fullscreen?Display.main.systemWidth:Current.width,Current.fullscreen?Display.main.systemHeight:Current.height,Current.fullscreen?FullScreenMode.FullScreenWindow:FullScreenMode.Windowed);}}
  public static void Defaults(){Current=new SettingsData();Apply();Save();}
 }
}
