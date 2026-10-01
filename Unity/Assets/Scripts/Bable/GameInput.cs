using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
namespace Bable {
 public enum GameAction { Left,Right,Up,Down,Jump,Attack,Shockwave,Heal,Dash,Interact,Runes,Map,Recall,Pause,Back,Submit }
 [DefaultExecutionOrder(-1000)]
 public sealed class GameInput:MonoBehaviour {
  static GameInput instance;static float suppressUntil;static InputActionMap map;static InputActionRebindingExtensions.RebindingOperation rebind;
  public static bool UsingGamepad {get;private set;}
  public static void ConsumeTransition(){suppressUntil=Time.unscaledTime+.16f;}
  public static bool Rebinding=>rebind!=null;
  public static string Message {get;private set;}="";
  [Serializable] public class BindingsFile {public int version=1;public BindingEntry[] entries;}
  [Serializable] public class BindingEntry {public string action,path;public int index;}
  public static void SaveBindings(){var entries=new List<BindingEntry>();foreach(var a in map.actions)for(int i=0;i<a.bindings.Count;i++)if(a.bindings[i].overridePath!=null)entries.Add(new BindingEntry{action=a.name,index=i,path=a.bindings[i].overridePath});LocalStorage.Write("bindings.json",JsonUtility.ToJson(new BindingsFile{entries=entries.ToArray()}));}
  public static void ReloadBindings(){
   bool recovered;var json=LocalStorage.Read("bindings.json",out recovered);map.RemoveAllBindingOverrides();if(string.IsNullOrEmpty(json))return;
   var file=JsonUtility.FromJson<BindingsFile>(json);if(file==null||file.version!=1)throw new System.IO.InvalidDataException("Unsupported controls file");
   foreach(var entry in file.entries??Array.Empty<BindingEntry>()){var a=map.FindAction(entry.action);if(a!=null&&entry.index>=0&&entry.index<a.bindings.Count&&entry.path!=null)a.ApplyBindingOverride(entry.index,entry.path);}
  }
  public static readonly GameAction[] Remappable={GameAction.Left,GameAction.Right,GameAction.Up,GameAction.Down,GameAction.Jump,GameAction.Attack,GameAction.Shockwave,GameAction.Heal,GameAction.Dash,GameAction.Interact,GameAction.Runes,GameAction.Map,GameAction.Recall};
  static readonly string[] keys={"leftArrow","rightArrow","upArrow","downArrow","space","x","z","a","leftShift","e","tab","m","g","escape","q","enter"};
  static readonly string[] pads={"leftStick/left","leftStick/right","leftStick/up","leftStick/down","buttonSouth","buttonWest","buttonNorth","leftShoulder","rightShoulder","buttonEast","rightTrigger","leftTrigger","select","start","buttonEast","buttonSouth"};
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void Reset(){map?.Dispose();map=null;instance=null;rebind=null;suppressUntil=0;UsingGamepad=false;Message="";}
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]static void Boot(){Ensure();var g=new GameObject("Unified input and menu navigation");DontDestroyOnLoad(g);instance=g.AddComponent<GameInput>();InputSystem.onDeviceChange+=DeviceChanged;}
  static void Ensure(){if(map!=null)return;map=new InputActionMap("Babel");foreach(GameAction action in Enum.GetValues(typeof(GameAction))){var a=map.AddAction(action.ToString(),InputActionType.Button);a.AddBinding("<Keyboard>/"+keys[(int)action]);a.AddBinding("<Gamepad>/"+pads[(int)action]);if((int)action<4)a.AddBinding("<Gamepad>/dpad/"+action.ToString().ToLowerInvariant());a.performed+=c=>{if(c.control.device is Gamepad)UsingGamepad=true;else if(c.control.device is Keyboard)UsingGamepad=false;};}map["Jump"].AddBinding("<Keyboard>/c");map["Shockwave"].AddBinding("<Keyboard>/v");map["Dash"].AddBinding("<Keyboard>/rightShift");try{ReloadBindings();}catch(Exception e){Debug.LogWarning("Input bindings reset: "+e.Message);}map.Enable();}
  public static InputAction Action(GameAction action){Ensure();return map[action.ToString()];}
  public static bool Held(GameAction action)=>Time.unscaledTime>=suppressUntil&&!Rebinding&&Action(action).IsPressed();
  public static bool Down(GameAction action){
   if(Time.unscaledTime<suppressUntil||Rebinding)return false;
   var a=Action(action);if(!a.WasPressedThisFrame())return false;
   if(a.activeControl?.device is Mouse&&EventSystem.current!=null){
    var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=Mouse.current.position.ReadValue()},hits);
    if(hits.Any(hit=>hit.gameObject.GetComponentInParent<Button>()!=null))return false;
   }
   return true;
  }
  public static string Hint(GameAction action){Ensure();return map[action.ToString()].GetBindingDisplayString(UsingGamepad?1:0);}
  public static string KeyHint(GameAction action,bool pad)=>Action(action).GetBindingDisplayString(pad?1:0);
  public static void Rebind(GameAction action,bool pad,Action completed){
   if(Rebinding)return;var a=Action(action);int index=pad?1:0;string old=a.bindings[index].overridePath;
   a.Disable();Message="Press a "+(pad?"controller button":"key or mouse button")+". Escape cancels.";
   rebind=a.PerformInteractiveRebinding(index).WithControlsHavingToMatchPath(pad?"<Gamepad>":"<Keyboard>").WithExpectedControlType("Button").WithCancelingThrough("<Keyboard>/escape").OnCancel(op=>{Message="Binding unchanged.";Finish();completed?.Invoke();}).OnComplete(op=>{
    string path=a.bindings[index].effectivePath;var conflict=Remappable.FirstOrDefault(other=>other!=action&&Action(other).bindings.Any(b=>b.effectivePath==path));
    bool duplicate=Remappable.Any(other=>other!=action&&Action(other).bindings.Any(b=>b.effectivePath==path));
    if(duplicate||path=="<Keyboard>/escape"||path=="<Keyboard>/q"||path=="<Keyboard>/enter"||path=="<Gamepad>/start"){if(old==null)a.RemoveBindingOverride(index);else a.ApplyBindingOverride(index,old);Message=duplicate?"Already assigned to "+conflict+".":"Escape is reserved for menus.";}
    else {for(int i=2;i<a.bindings.Count;i++)if(a.bindings[i].path.StartsWith("<Keyboard>")&&!pad)a.ApplyBindingOverride(i,"");try{SaveBindings();Message="Binding saved.";}catch(Exception e){Message="Unable to save binding: "+e.Message;}}
    Finish();completed?.Invoke();
   });if(!pad)rebind.WithControlsHavingToMatchPath("<Mouse>");rebind.Start();
  }
  static void Finish(){suppressUntil=Time.unscaledTime+.25f;var op=rebind;rebind=null;op?.Dispose();foreach(var a in map.actions)a.Enable();}
  public static void CancelRebind(){rebind?.Cancel();}
  public static void ResetBindings(){Ensure();map.RemoveAllBindingOverrides();try{SaveBindings();Message="Default controls restored.";}catch(Exception e){Message="Controls reset for this session, but could not be saved: "+e.Message;}}
  static void DeviceChanged(InputDevice device,InputDeviceChange change){if(device is Gamepad&&(change==InputDeviceChange.Disconnected||change==InputDeviceChange.Removed)&&UsingGamepad){Message="Controller disconnected. Reconnect or use the keyboard.";if(BableGameUI.Instance!=null&&BableGameUI.Instance.Ready&&!TowerLoading.Busy&&BableGameUI.Instance.Mode=="play"&&Babel.Runtime.Core.GameSession.Instance?.IsPaused!=true)BableGameUI.Instance.Pause();}}
  float nextMove;Vector2 lastMove;
  void Update(){
   if(Mouse.current!=null&&(Mouse.current.delta.ReadValue().sqrMagnitude>2||Mouse.current.leftButton.wasPressedThisFrame))UsingGamepad=false;
   var es=EventSystem.current;if(es==null)return;
   var old=es.GetComponent<StandaloneInputModule>();if(old!=null){old.enabled=false;Destroy(old);}
   var module=es.GetComponent<InputSystemUIInputModule>();if(module==null){module=es.gameObject.AddComponent<InputSystemUIInputModule>();module.AssignDefaultActions();}
   es.sendNavigationEvents=false;
   if(Rebinding||TowerLoading.Busy)return;
   if(FindFirstObjectByType<RuneRepositoryView>()!=null||FindFirstObjectByType<Babel.Runtime.Shop.ShopMenuController>()?.IsOpen==true)return;
   var ui=BableGameUI.Instance;if(ui==null||ui.Mode=="play"||ui.Mode=="prologue")return;
   var current=es.currentSelectedGameObject;
   if(current==null||!current.activeInHierarchy||current.GetComponent<Selectable>()?.IsInteractable()==false){var first=FindObjectsByType<Button>(FindObjectsSortMode.None).Where(b=>b.IsActive()&&b.IsInteractable()).OrderByDescending(b=>b.transform.position.y).ThenBy(b=>b.transform.position.x).FirstOrDefault();if(first!=null)es.SetSelectedGameObject(first.gameObject);current=es.currentSelectedGameObject;}
   if(current==null)return;
   Vector2 move=new Vector2((Held(GameAction.Right)?1:0)-(Held(GameAction.Left)?1:0),(Held(GameAction.Up)?1:0)-(Held(GameAction.Down)?1:0));
   if(move!=Vector2.zero&&(move!=lastMove||Time.unscaledTime>=nextMove)){
    var data=new AxisEventData(es){moveVector=move,moveDir=Mathf.Abs(move.y)>0?(move.y>0?MoveDirection.Up:MoveDirection.Down):(move.x>0?MoveDirection.Right:MoveDirection.Left)};
    var choice=current.GetComponent<SettingChoice>();if(choice!=null&&move.y==0)choice.Adjust(move.x>0?1:-1);else ExecuteEvents.Execute(current,data,ExecuteEvents.moveHandler);
    nextMove=Time.unscaledTime+(move!=lastMove?.32f:.12f);
   }
   lastMove=move;if(Down(GameAction.Submit))ExecuteEvents.Execute(current,new BaseEventData(es),ExecuteEvents.submitHandler);
  }
  void OnDestroy(){InputSystem.onDeviceChange-=DeviceChanged;rebind?.Dispose();rebind=null;}
 }
 public sealed class SettingChoice:MonoBehaviour {public Action<int> change;public void Adjust(int direction){change?.Invoke(direction);CombatAudio.UI("ui_select",.45f);}}
}
