using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
namespace Bable {
 public static class InputPrompts {
  public static string Prose(string text)=>string.IsNullOrEmpty(text)?text:text.Replace("hold A", "hold "+GameInput.Hint(GameAction.Heal)).Replace("hold Shift", "hold "+GameInput.Hint(GameAction.Dash)).Replace("press E", "press "+GameInput.Hint(GameAction.Interact)).Replace("an arrow key", "a direction").Replace("with the arrows", "with the directional controls");
  public static string Format(string text){
   if(string.IsNullOrEmpty(text))return text;
   text=text.Replace("SPACE or C","SPACE").Replace("SPACE / C","SPACE").Replace("E / ESC","Q");
   text=text.Replace("LEFT / RIGHT",GameInput.Hint(GameAction.Left)+" / "+GameInput.Hint(GameAction.Right));
   text=text.Replace("ARROWS",GameInput.UsingGamepad?"STICK / D-PAD":"DIRECTIONS");
   var tokens=new[]{"SPACE","ENTER","SHIFT","TAB","ESC","X","Z","A","E","Q","M","G"};
   var actions=new[]{GameAction.Jump,GameAction.Submit,GameAction.Dash,GameAction.Runes,GameAction.Pause,GameAction.Attack,GameAction.Shockwave,GameAction.Heal,GameAction.Interact,GameAction.Back,GameAction.Map,GameAction.Recall};
   // Match once, avoiding cascading substitution when a rebound key is another token.
   return Regex.Replace(text,@"\b(SPACE|ENTER|SHIFT|TAB|ESC|X|Z|A|E|Q|M|G)\b",m=>GameInput.Hint(actions[System.Array.IndexOf(tokens,m.Value)]));
  }
 }
 public sealed class LiveInputHint:MonoBehaviour {
  public string template;Text text;TMP_Text tmp;
  void Awake(){text=GetComponent<Text>();tmp=GetComponent<TMP_Text>();}
  void Update(){if(text!=null)text.text=InputPrompts.Format(template);if(tmp!=null)tmp.text=InputPrompts.Format(template);}
  public static void Attach(Component target,string source){if(target==null)return;var c=target.GetComponent<LiveInputHint>()??target.gameObject.AddComponent<LiveInputHint>();c.template=source;}
 }
}
