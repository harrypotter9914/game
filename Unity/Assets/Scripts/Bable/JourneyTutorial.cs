using UnityEngine;
using Babel.Runtime.Core;
using Babel.Runtime.Characters.Player;
namespace Bable {
 public sealed class JourneyTutorial:MonoBehaviour {
  GameSession session;PlayerController2D player;Vector2 start;
  void Start(){session=GameSession.Instance;player=FindFirstObjectByType<PlayerController2D>();if(player!=null)start=player.transform.position;}
  void Update(){
   if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name!="Gameplay_Main"||TowerLoading.Busy||session==null||player==null||NarrativeGuidance.Instance==null)return;
   var n=NarrativeGuidance.Instance;
   n.Teach("move","Your first steps","Find your footing beneath the tower.","LEFT / RIGHT  Move     SPACE or C  Jump");
   if(session.HasWeapon)n.Teach("sword","Wield the awakened blade","Hold an arrow key as you strike to aim. Downward strikes are made in the air.","X  Attack     ARROWS + X  Aim your strike");
   if(session.CurrentHealth>0&&session.CurrentHealth<session.MaxHealth)n.Teach("heal","Mend your wounds","Stand still and hold A for 3 seconds. One Spirit restores one Vitality. Releasing early cancels; release before healing again.","HOLD A  Mend     1 Spirit → 1 Vitality");
   if(session.HasAbility(AbilityId.Shockwave))n.Teach("shock","The power to break stone","Aim in any cardinal direction. The wave stops at solid walls and shatters fractured seals.","ARROWS + Z  Shockwave     Cost: 1 Spirit");
   if(session.HasAbility(AbilityId.WallJump))n.Teach("wall","Rise between the walls","Press toward a wall while airborne, then jump to kick away from it.","HOLD toward wall + SPACE / C  Wall Jump");
   if(session.HasAbility(AbilityId.DoubleJump))n.Teach("double","A second step in the air","Jump once, then press jump again before you land.","SPACE / C, then SPACE / C again  Double Jump");
   if(session.HasAbility(AbilityId.CrystalDash))n.Teach("dash","Ride the crystal current","Aim with the arrows and hold Shift for 0.7 seconds to launch. Releasing early cancels the charge.","ARROWS + HOLD SHIFT 0.7 s  Crystal Dash");
   if(session.RuneInventory.CollectedRunes.Count>0)n.Teach("runes","Carry your chosen signs","Open the repository and select a rune to equip it. Select an equipped rune to remove it; carry up to four.","TAB  Rune repository     MOUSE / ARROWS  Select");
   if(Vector2.Distance(start,player.transform.position)>12)n.Teach("map","Remember the road","Your atlas records only the passages you have explored. Unvisited paths remain black.","M  Open / close atlas     G  Recall your path");
  }
 }
}
