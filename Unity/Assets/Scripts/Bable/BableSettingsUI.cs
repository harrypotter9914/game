using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace Bable {
 public sealed partial class BableGameUI {
  Action cancelMenu;string settingsOrigin;int settingsPage;bool controllerBindings;int controlsPage;
  bool lastControlsPad;
  TMPro.TMP_Text settingsInputHint;
  bool displayPending;SettingsData displayBefore;float displayDeadline;TMPro.TMP_Text displayCountdown;
  bool HandleMenuBack(){
   if(displayPending&&Time.unscaledTime>=displayDeadline){RevertDisplay();return true;}
   if(displayPending&&displayCountdown!=null)displayCountdown.text="Reverting in "+Mathf.CeilToInt(displayDeadline-Time.unscaledTime)+" seconds";
   if(GameInput.Rebinding)return false;
   if((Mode=="settings"||Mode=="confirm"||Mode=="notice")&&(GameInput.Down(GameAction.Back)||GameInput.Down(GameAction.Pause))){cancelMenu?.Invoke();return true;}
   if(Mode!="play"&&Mode!="main"&&Mode!="prologue"&&Mode!="death"&&Mode!="victory"&&Mode!="scroll"&&Mode!="runes"&&GameInput.Down(GameAction.Back)){Resume();return true;}
   return false;
  }
  void Confirm(string title,string detail,Action yes,Action no){
   Panel("confirm",title,"NewArt/Release51/Panel");cancelMenu=no;
   Label(overlay,detail,new Vector2(0,80),new Vector2(1040,150),26);
   Button(overlay,"CANCEL",new Vector2(-265,-130),()=>no(),430,70);
   Button(overlay,"CONFIRM",new Vector2(265,-130),()=>yes(),430,70);
   EventSystem.current?.SetSelectedGameObject(overlay.Find("CANCEL").gameObject);
  }
  void Notice(string text,Action back){
   Panel("notice","THE PILGRIM'S JOURNAL","NewArt/Release51/Panel");cancelMenu=back;
   Label(overlay,text,new Vector2(0,50),new Vector2(1000,220),27);
   Button(overlay,"RETURN",new Vector2(0,-180),()=>back(),460,65);
  }
  public void OpenSettings(){settingsOrigin=Mode;settingsPage=0;controllerBindings=GameInput.UsingGamepad;lastControlsPad=GameInput.UsingGamepad;SettingsPanel();}
  void SettingsBack(){
   if(GameInput.Rebinding){GameInput.CancelRebind();return;}
   GameSettings.Save();
   if(settingsOrigin=="main")MainMenu();else if(settingsOrigin=="death")Death();else Pause();
  }
  public void SettingsPanel(){
   lastControlsPad=GameInput.UsingGamepad;
   Panel("settings","SETTINGS","NewArt/Release51/Panel");cancelMenu=SettingsBack;
   string[] pages={"SOUND","DISPLAY & COMFORT","CONTROLS"};
   for(int i=0;i<3;i++){int n=i;Button(overlay,pages[i],new Vector2(-450+i*450,230),()=>{settingsPage=n;SettingsPanel();},420,58);overlay.Find(pages[i]).GetComponent<ManuscriptMenuButton>().ActiveTab=settingsPage==i;}
   var c=GameSettings.Current;
   if(settingsPage==0){
    Setting("MASTER",110,()=>Percent(c.master),d=>c.master=Step(c.master,d));
    Setting("MUSIC",30,()=>Percent(c.music),d=>c.music=Step(c.music,d));
    Setting("SOUND EFFECTS",-50,()=>Percent(c.effects),d=>c.effects=Step(c.effects,d));
    Setting("VOICES",-130,()=>Percent(c.voice),d=>c.voice=Step(c.voice,d));
    Label(overlay,"Volumes are saved independently.",new Vector2(0,-220),new Vector2(1000,50),22);
   } else if(settingsPage==1){
    Setting("SCREEN MODE",135,()=>c.fullscreen?"FULLSCREEN":"WINDOWED",d=>PreviewDisplay(()=>c.fullscreen=!c.fullscreen),false);
    Setting("WINDOW SIZE",65,()=>c.width+" × "+c.height,d=>PreviewDisplay(()=>{var sizes=new[]{new Vector2Int(960,540),new Vector2Int(1024,768),new Vector2Int(1280,720),new Vector2Int(1280,800),new Vector2Int(1600,900),new Vector2Int(1920,1080),new Vector2Int(2560,1080)};int i=Array.IndexOf(sizes,new Vector2Int(c.width,c.height));i=(Mathf.Max(0,i)+d+sizes.Length)%sizes.Length;c.width=sizes[i].x;c.height=sizes[i].y;}),false);
    Setting("VERTICAL SYNC",-5,()=>c.vsync?"ON":"OFF",d=>c.vsync=!c.vsync);
    Setting("CAMERA SHAKE",-75,()=>Percent(c.shake),d=>c.shake=Step(c.shake,d,.25f));
    Setting("DAMAGE FLASH",-145,()=>Percent(c.flash),d=>c.flash=Step(c.flash,d,.25f));
    Setting("DIALOGUE SIZE",-215,()=>Percent(c.textScale),d=>c.textScale=Mathf.Clamp(c.textScale+d*.05f,.9f,1.25f));
   } else {
    Button(overlay,controllerBindings?"CONTROLLER":"KEYBOARD / MOUSE",new Vector2(-350,150),()=>{controllerBindings=!controllerBindings;SettingsPanel();},520,48);
    Button(overlay,controlsPage==0?"MOVEMENT  →":"ACTIONS  →",new Vector2(350,150),()=>{controlsPage=1-controlsPage;SettingsPanel();},520,48);
    for(int i=controlsPage*7;i<Mathf.Min(GameInput.Remappable.Length,controlsPage*7+7);i++){
     var action=GameInput.Remappable[i];int row=i%7;
     Button(overlay,action.ToString().ToUpperInvariant()+"   ·   "+GameInput.KeyHint(action,controllerBindings),new Vector2(0,85-row*49),()=>StartRebind(action),900,44);
     overlay.GetChild(overlay.childCount-1).name="Binding "+action;
    }
    Label(overlay,GameInput.Message,new Vector2(0,-270),new Vector2(1100,44),19);
   }
   Button(overlay,"RESTORE DEFAULTS",new Vector2(-320,-318),()=>Confirm("RESTORE DEFAULTS?","Reset this settings page to its original values.",ResetSettingsPage,SettingsPanel),520,60);
   Button(overlay,"RETURN",new Vector2(320,-318),SettingsBack,520,60);
   settingsInputHint=Label(overlay,"",new Vector2(0,-398),new Vector2(1300,32),16);RefreshSettingsHint();
  }
  static string Percent(float value)=>Mathf.RoundToInt(value*100)+"%";
  static float Step(float value,int direction,float step=.1f)=>Mathf.Clamp01(Mathf.Round((value+direction*step)/step)*step);
  void Setting(string name,float y,Func<string> read,Action<int> change,bool apply=true){
   Action<int> adjust=d=>{change(d);if(apply){GameSettings.Apply();GameSettings.Save();}};
   Button(overlay,name+" VALUE",new Vector2(0,y),()=>adjust(1),1000,56);
   var row=overlay.Find(name+" VALUE");var t=row.GetComponentInChildren<TMPro.TMP_Text>();t.text=name+"     ‹ "+read()+" ›";
   Action<int> update=d=>{adjust(d);if(t!=null)t.text=name+"     ‹ "+read()+" ›";};
   var button=row.GetComponent<Button>();button.onClick.RemoveAllListeners();button.onClick.AddListener(()=>update(1));
   row.gameObject.AddComponent<SettingChoice>().change=update;
   Button(overlay,"−",new Vector2(-585,y),()=>update(-1),100,52);
   Button(overlay,"+",new Vector2(585,y),()=>update(1),100,52);
  }
  void StartRebind(GameAction action){
   GameInput.Rebind(action,controllerBindings,()=>{controllerBindings=GameInput.UsingGamepad;SettingsPanel();FocusBinding(action);});
   Panel("settings","ASSIGN "+action.ToString().ToUpperInvariant(),"NewArt/Release51/Panel");
   Label(overlay,GameInput.Message,new Vector2(0,50),new Vector2(1150,120),30);
   Button(overlay,"CANCEL",new Vector2(0,-170),GameInput.CancelRebind,430,60);
  }
  void FocusBinding(GameAction action){
   if(GameInput.NavigationActive){var row=overlay.Find("Binding "+action);if(row!=null)EventSystem.current?.SetSelectedGameObject(row.gameObject);}
  }
  void UpdateSettingsDevice(){
   RefreshSettingsHint();
   if(Mode=="settings"&&settingsPage==2&&!GameInput.Rebinding&&lastControlsPad!=GameInput.UsingGamepad){
    string selected=EventSystem.current?.currentSelectedGameObject?.name;
    controllerBindings=GameInput.UsingGamepad;SettingsPanel();
    var row=selected==null?null:overlay.Find(selected);if(GameInput.NavigationActive&&row!=null)EventSystem.current?.SetSelectedGameObject(row.gameObject);
   }
  }
  void RefreshSettingsHint(){
   if(settingsInputHint==null)return;
   string select=GameInput.UsingGamepad?"Stick / D-pad: select and adjust":GameInput.NavigationActive?"Directions: select and adjust":"Mouse: select and click";
   settingsInputHint.text=GameSettings.Error.Length>0?GameSettings.Error:select+" · Confirm: "+GameInput.Hint(GameAction.Submit)+" · Back: "+GameInput.Hint(GameAction.Back);
  }
  void ResetSettingsPage(){
   var c=GameSettings.Current;
   if(settingsPage==2)GameInput.ResetBindings();
   else if(settingsPage==0){c.master=.7f;c.music=c.effects=c.voice=1;}
   else {c.vsync=true;c.shake=c.flash=c.textScale=1;}
   GameSettings.Apply();GameSettings.Save();SettingsPanel();
  }
  void PreviewDisplay(Action change){
   displayBefore=JsonUtility.FromJson<SettingsData>(JsonUtility.ToJson(GameSettings.Current));
   change();GameSettings.Apply(true);displayPending=true;displayDeadline=Time.unscaledTime+15;
   Confirm("KEEP THESE DISPLAY SETTINGS?","If the display is unreadable, the previous mode returns automatically.",()=>{displayPending=false;GameSettings.Save();SettingsPanel();},RevertDisplay);
   displayCountdown=Label(overlay,"",new Vector2(0,-230),new Vector2(950,50),22);
  }
  void RevertDisplay(){
   if(displayBefore!=null){var c=GameSettings.Current;c.fullscreen=displayBefore.fullscreen;c.width=displayBefore.width;c.height=displayBefore.height;}
   displayPending=false;GameSettings.Apply(true);SettingsPanel();
  }
 }
}

