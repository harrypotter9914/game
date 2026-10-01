using System.Collections;
using UnityEngine;
using Babel.Runtime.Core;
using Babel.Runtime.Characters.Player;
using Babel.Runtime.Runes;
namespace Bable {
 public sealed class BossPractice:MonoBehaviour {
  public int bossIndex;
  public bool Invulnerable {get;set;}
  void LateUpdate(){
   if(TowerLoading.Busy||BableGameUI.Instance==null||BableGameUI.Instance.CinematicActive)return;
   var player=FindFirstObjectByType<PlayerController2D>();if(player==null||player.GetComponent<AltarRebirth>()?.IsRunning==true)return;
   player.GetComponent<Babel.Runtime.Combat.HealthComponent>().Invincible=Invulnerable;
  }
  IEnumerator Start(){
   while(GameSession.Instance==null||BableGameUI.Instance==null||!BableGameUI.Instance.Ready)yield return null;
   var s=GameSession.Instance;s.UnlockWeapon();
   foreach(AbilityId a in System.Enum.GetValues(typeof(AbilityId)))s.UnlockAbility(a);
   foreach(var r in Resources.LoadAll<RuneDefinition>("Bable/Runes"))s.RuneInventory.Collect(r);
  }
 }
}
