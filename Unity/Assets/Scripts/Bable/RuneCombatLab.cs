using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Babel.Runtime.Core;
using Babel.Runtime.Runes;
using Babel.Runtime.Characters.Player;
using Babel.Runtime.Combat;
namespace Bable {
 public sealed class RuneCombatLab:MonoBehaviour {
  public GameObject[] enemyTemplates;readonly List<GameObject> opponents=new();GameSession session;PlayerController2D player;bool funding;
  Canvas toolsCanvas;
  public bool Ready{get;private set;}
  IEnumerator Start(){
   // Async scene activation may take more than a fixed pair of frames.
   while(GameSession.Instance==null||FindFirstObjectByType<PlayerController2D>()==null||BableGameUI.Instance==null||!BableGameUI.Instance.Ready)yield return null;
   session=GameSession.Instance;player=FindFirstObjectByType<PlayerController2D>();session.StateChanged+=Fund;GrantRunes();session.UnlockWeapon();foreach(AbilityId a in Enum.GetValues(typeof(AbilityId)))session.UnlockAbility(a);Fund();Restore();RespawnEnemies();BuildTools();Ready=true;
  }
  void OnDestroy(){if(session!=null)session.StateChanged-=Fund;}
  void Fund(){if(session==null||funding||session.CurrentGold>=999999)return;funding=true;session.AddGold(999999-session.CurrentGold);funding=false;}
  public void GrantRunes(){var s=GameSession.Instance;if(s==null)return;foreach(var rune in Resources.LoadAll<RuneDefinition>("Bable/Runes"))s.CollectRune(rune);}
  public void Restore(){if(player==null)player=FindFirstObjectByType<PlayerController2D>();player.GetComponent<HealthComponent>().Heal(999);GameSession.Instance.RestoreMana(999);player.GetComponent<PlayerRuntimeState>().SynchronizeFromSession();}
  public void RespawnEnemies(){foreach(var e in opponents)if(e!=null)Destroy(e);opponents.Clear();for(int i=0;i<enemyTemplates.Length;i++){var go=Instantiate(enemyTemplates[i],new Vector3(30+i*18,3,0),Quaternion.identity);go.name="Lab opponent "+enemyTemplates[i].name;go.SetActive(true);var shape=go.GetComponent<Collider2D>();Physics2D.SyncTransforms();go.transform.position+=Vector3.up*(-shape.bounds.min.y+.025f);var body=go.GetComponent<Rigidbody2D>();if(body!=null)body.position=go.transform.position;opponents.Add(go);}}
  static RectTransform Rect(Transform parent,string name,Vector2 position,Vector2 size){var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.anchoredPosition=position;r.sizeDelta=size;return r;}
  static TMP_Text Label(Transform parent,string text,Vector2 position,Vector2 size,int fontSize){var label=Rect(parent,text,position,size).gameObject.AddComponent<TextMeshProUGUI>();label.font=Resources.Load<TMP_FontAsset>("Bable/GuideFonts/CrimsonText-Regular SDF");label.text=text;label.fontSize=fontSize;label.alignment=TextAlignmentOptions.Center;label.color=new Color(.95f,.87f,.69f);label.raycastTarget=false;return label;}
  void BuildTools(){} // Chamber tools are reached through the shared pause menu on every device.
  void Update(){if(toolsCanvas!=null)toolsCanvas.enabled=Ready&&BableGameUI.Instance!=null&&BableGameUI.Instance.Mode=="play"&&!GameSession.Instance.IsPaused;}
 }
}
