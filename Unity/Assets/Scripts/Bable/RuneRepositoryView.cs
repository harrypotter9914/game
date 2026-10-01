using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using Babel.Runtime.Runes;
namespace Bable {
 public sealed class RuneRepositoryView:MonoBehaviour {
  RuneInventory inventory;Action close;readonly Image[] icons=new Image[12];readonly TMP_Text[] labels=new TMP_Text[12];readonly RuneDefinition[] displayed=new RuneDefinition[12];
  RectTransform selection;Image selectionImage;TMP_Text description,lore,detailTitle,status;Image detailIcon;int focused=4;float selectedAt;bool moving;TMP_Text controls;bool lastEditable;MenuInputSource lastInput;
  public bool CanEdit=>RuneAttunement.CanChange;
  public int FocusedCell=>focused;
  public RuneDefinition DisplayedRuneAt(int index)=>index>=0&&index<12?displayed[index]:null;
  static Vector2 Position(int i)=>i<4?new Vector2(-505+i*180,125):new Vector2(-505+(i-4)%4*180,-65-(i-4)/4*115);
  static RectTransform Rect(Transform parent,string name,Vector2 pos,Vector2 size){var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.anchoredPosition=pos;r.sizeDelta=size;return r;}
  static Image Image(Transform p,string name,Vector2 pos,Vector2 size,Sprite sprite,Color color){var i=Rect(p,name,pos,size).gameObject.AddComponent<Image>();i.sprite=sprite;i.color=color;i.preserveAspect=true;i.raycastTarget=false;return i;}
  static TMP_Text Text(Transform p,string name,string value,Vector2 pos,Vector2 size,float sizeText,bool heading=false){var t=Rect(p,name,pos,size).gameObject.AddComponent<TextMeshProUGUI>();t.text=value;t.font=Resources.Load<TMP_FontAsset>("Bable/GuideFonts/"+(heading?"CinzelDecorative-Regular":"CrimsonText-Regular")+" SDF");t.fontSize=sizeText;t.alignment=TextAlignmentOptions.Center;t.color=new Color(.96f,.89f,.72f,1f);t.fontStyle=FontStyles.Bold;t.raycastTarget=false;return t;}
  static Sprite Icon(RuneDefinition rune)=>rune==null?null:rune.Icon;
  public void Build(RuneInventory source,Action onClose){
   inventory=source;close=onClose;
   Text(transform,"Repository title","RUNE REPOSITORY",new Vector2(0,330),new Vector2(1100,65),38,true);
   Text(transform,"Socket heading","EQUIPPED SOCKETS  /  FOUR POWERS",new Vector2(-235,205),new Vector2(800,40),23,true);
   Text(transform,"Storage heading","RUNE STORAGE",new Vector2(-235,15),new Vector2(800,42),27,true);
   var ornament=Resources.Load<Sprite>("Bable/NewArt/Release53/RuneRing");
   for(int n=0;n<12;n++){
    int index=n;var pos=Position(n);
    Image(transform,"Socket engraving "+n,pos,new Vector2(104,104),ornament,new Color(.75f,.68f,.5f,.3f));
    icons[n]=Image(transform,"Rune icon "+n,pos,new Vector2(62,68),null,Color.white);
    labels[n]=Text(transform,"Rune label "+n,"",pos+Vector2.down*58,new Vector2(168,30),18);
    var hit=Rect(transform,"Rune cell "+n,pos,new Vector2(150,104));var im=hit.gameObject.AddComponent<Image>();im.color=Color.clear;var button=hit.gameObject.AddComponent<Button>();button.targetGraphic=im;button.navigation=new Navigation{mode=Navigation.Mode.None};button.onClick.AddListener(()=>ActivateCell(index));hit.gameObject.AddComponent<RuneCellHover>().Init(this,index);
   }
   selectionImage=Image(transform,"Moving ornamental selection",Position(focused),new Vector2(116,116),ornament,Color.white);selection=selectionImage.rectTransform;
   detailIcon=Image(transform,"Selected rune detail",new Vector2(465,155),new Vector2(110,110),null,Color.white);
   detailTitle=Text(transform,"Rune name","",new Vector2(465,60),new Vector2(430,65),30,true);
   description=Text(transform,"Rune description","",new Vector2(465,-32),new Vector2(420,120),22);
   lore=Text(transform,"Rune lore","",new Vector2(465,-141),new Vector2(420,88),19);
   status=Text(transform,"Repository status","",new Vector2(465,-229),new Vector2(430,60),19);
   controls=Text(transform,"Repository controls","",new Vector2(0,-310),new Vector2(1320,44),19);
   lastEditable=CanEdit;lastInput=GameInput.Source;UpdateControls();
   inventory.InventoryChanged+=Refresh;Refresh();SelectCell(4);
  }
  void UpdateControls(){
   string select=GameInput.UsingGamepad?"STICK / D-PAD  Select":GameInput.NavigationActive?"ARROWS  Select":"MOUSE  Select";
   string equip=CanEdit?"   ·   "+(GameInput.NavigationActive?GameInput.Hint(GameAction.Submit):"CLICK")+"  Equip / remove":"   ·   View only away from an altar";
   controls.text=select+equip+"   ·   "+GameInput.Hint(GameAction.Back)+" / "+GameInput.Hint(GameAction.Runes)+"  Close";
  }
  void OnDestroy(){if(inventory!=null)inventory.InventoryChanged-=Refresh;}
  void Refresh(){
   var available=inventory.CollectedRunes.Where(r=>!inventory.IsActive(r.RuneId)).ToArray();
   for(int i=0;i<12;i++){
    displayed[i]=i<4?inventory.ActiveSlots[i]:i-4<available.Length?available[i-4]:null;
    icons[i].sprite=Icon(displayed[i]);icons[i].enabled=displayed[i]!=null;
    labels[i].text=displayed[i]!=null?displayed[i].DisplayName:i<4?"EMPTY SOCKET":"";
   }
   ShowDetails();
  }
  void ShowDetails(){
   var rune=displayed[focused];detailIcon.sprite=Icon(rune);detailIcon.enabled=rune!=null;
   detailTitle.text=rune!=null?rune.DisplayName:focused<4?"EMPTY SOCKET":"UNRECOVERED";
   description.text=rune!=null?"<b>EFFECT</b>\n"+rune.EffectDescription:inventory.CollectedRunes.Count==0?"Your repository is empty. Seek the stars within the tower and press "+GameInput.Hint(GameAction.Interact)+" to recover a rune.":focused<4?"Select a rune below to place its power in an empty socket.":"Only recovered runes appear here. Their order follows the order in which you found them.";
   lore.text=rune!=null?"<i>"+rune.Lore+"</i>":"";
   status.text=inventory.CollectedRunes.Count+" / 8 RECOVERED\n"+inventory.ActiveSlots.Count(r=>r!=null)+" / 4 EQUIPPED";
   status.text+="\n"+(CanEdit?"ATTUNEMENT AVAILABLE":"VIEW ONLY  ·  RETURN TO AN ALTAR");
   if(rune!=null&&inventory.IsSpent(rune))status.text="SPENT  /  THIS JOURNEY\nThis rune cannot be equipped again.";
  }
  public void SelectCell(int index){if(index<0||index>=12)return;if(index!=focused)CombatAudio.UI("ui_select",.5f);focused=index;selectedAt=Time.unscaledTime;ShowDetails();}
  public bool ActivateCell(int index){
   if(moving||index<0||index>=12)return false;SelectCell(index);
   if(!CanEdit){status.text="VIEW ONLY\nReturn to an altar to equip or remove runes.";return false;}var rune=displayed[index];if(rune==null)return false;
   if(inventory.IsSpent(rune))return false;
   int destination;
   if(index<4){if(!inventory.TryDeactivateSlot(index))return false;destination=Array.IndexOf(displayed,rune,4);}
   else {destination=Array.FindIndex(inventory.ActiveSlots.ToArray(),r=>r==null);if(destination<0){status.text="ALL FOUR SOCKETS ARE FULL\nRemove a rune above to make room.";return false;}if(!inventory.TryActivateToEmptySlot(rune))return false;}
   SelectCell(destination);StartCoroutine(Transfer(rune,index,destination));CombatAudio.UI("ui_equip",.65f);return true;
  }
  IEnumerator Transfer(RuneDefinition rune,int from,int to){
   moving=true;var ghost=Image(transform,"Flying rune",Position(from),new Vector2(62,68),Icon(rune),Color.white);icons[to].enabled=false;
   for(float t=0;t<.28f;t+=Time.unscaledDeltaTime){float f=Mathf.SmoothStep(0,1,t/.28f);ghost.rectTransform.anchoredPosition=Vector2.Lerp(Position(from),Position(to),f)+Vector2.up*Mathf.Sin(f*Mathf.PI)*30;yield return null;}
   Destroy(ghost.gameObject);moving=false;Refresh();
  }
  void Update(){
   if(inventory==null)return;float dt=Time.unscaledDeltaTime;
   if(lastEditable!=CanEdit||lastInput!=GameInput.Source){lastEditable=CanEdit;lastInput=GameInput.Source;UpdateControls();ShowDetails();}
   selection.anchoredPosition=Vector2.Lerp(selection.anchoredPosition,Position(focused),1-Mathf.Exp(-24*dt));float age=Time.unscaledTime-selectedAt;
   selectionImage.color=new Color(1f,.95f,.8f,Mathf.Lerp(.3f,.9f,Mathf.Clamp01(age/.18f))*(.93f+.07f*Mathf.Sin(Time.unscaledTime*3)));
   selection.localScale=Vector3.one*(1+.018f*Mathf.Sin(Time.unscaledTime*3));
   int row=focused<4?0:1+(focused-4)/4,col=focused%4;
   if(Bable.GameInput.Down(Bable.GameAction.Left))SelectCell(row*4+Mathf.Max(0,col-1));
   if(Bable.GameInput.Down(Bable.GameAction.Right))SelectCell(row*4+Mathf.Min(3,col+1));
   if(Bable.GameInput.Down(Bable.GameAction.Up))SelectCell(Mathf.Max(0,row-1)*4+col);
   if(Bable.GameInput.Down(Bable.GameAction.Down))SelectCell(Mathf.Min(2,row+1)*4+col);
   if(Bable.GameInput.Down(Bable.GameAction.Submit))ActivateCell(focused);
   if(Bable.GameInput.Down(Bable.GameAction.Back))close?.Invoke();
  }
 }
 public sealed class RuneCellHover:MonoBehaviour,IPointerEnterHandler {
  RuneRepositoryView owner;int index;public void Init(RuneRepositoryView view,int cell){owner=view;index=cell;}
  public void OnPointerEnter(PointerEventData e){if(!GameInput.NavigationActive)owner.SelectCell(index);}
 }
}
