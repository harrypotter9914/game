using UnityEngine;
using Babel.Runtime.Core;
using Babel.Runtime.Characters.Player;
namespace Bable {
 // One shared scan; all authored fracture groups use the same visual/tutorial language.
 public sealed class FracturedStoneGuide:MonoBehaviour {
  BreakableWall[] walls; PlayerController2D player; float next;
  void Start(){walls=FindObjectsByType<BreakableWall>(FindObjectsSortMode.None);player=FindFirstObjectByType<PlayerController2D>();}
  void Update(){
   if(Time.time<next||player==null)return;next=Time.time+.35f;
   var session=GameSession.Instance;var guide=NarrativeGuidance.Instance;
   if(session==null||guide==null||session.IsPaused||TowerLoading.Busy||TowerDialogue.StoryActive||BableGameUI.Instance?.Mode!="play")return;
   bool power=session.HasAbility(AbilityId.Shockwave);string id=power?"fractured_stone_power":"fractured_stone_locked";
   if(guide.HasSeen("tutorial_"+id))return;
   foreach(var wall in walls){if(wall==null||!wall.requiresShockwave||!wall.gameObject.activeInHierarchy)continue;var c=wall.GetComponent<Collider2D>();if(c==null)continue;
    Vector2 p=player.transform.position,near=c.ClosestPoint(p);if((near-p).sqrMagnitude>8)continue;
    // A solid intervening wall must not reveal a hidden route.
    bool hidden=false;foreach(var h in Physics2D.LinecastAll(p,near)){if(h.collider==c||h.collider.isTrigger||h.rigidbody!=null&&h.rigidbody.bodyType!=RigidbodyType2D.Static)continue;if(h.collider!=null){hidden=true;break;}}if(hidden)continue;
    guide.Teach(id,"FRACTURED STONE",power?"These split stones yield to Shockwave. Aim into the cracks to open a passage.":"Deep fractures split this stone. An ordinary blade cannot break it. The first guardian holds the force you need.",power?"ARROWS  Aim    Z  Shockwave    Q  Dismiss":"Seek the first guardian    Q  Dismiss");break;
   }
  }
 }
}

