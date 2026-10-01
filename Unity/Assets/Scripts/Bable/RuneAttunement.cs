using System.Linq;
using UnityEngine;
using Babel.Runtime.Characters.Player;
namespace Bable {
 public static class RuneAttunement {
  public static bool CanChange {
   get {
    if(CampaignStore.IsPractice)return true;
    var player=Object.FindFirstObjectByType<PlayerController2D>();
    if(player==null||!player.IsGrounded||TowerDialogue.StoryActive)return false;
    if(Object.FindObjectsByType<BossBrain>(FindObjectsSortMode.None).Any(b=>b.Engaged&&b.InArena(player.transform.position)))return false;
    var body=player.GetComponent<Collider2D>();if(body==null)return false;
    return Object.FindObjectsByType<AltarCheckpoint>(FindObjectsSortMode.None).Any(a=>
     Mathf.Abs(player.transform.position.x-a.transform.position.x)<1.8f&&Mathf.Abs(body.bounds.min.y-a.transform.position.y)<.5f);
   }
  }
 }
}
