using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;
using Babel.Runtime.Characters.Player;
using Babel.Runtime.Combat;
namespace Bable
{
    public sealed class ScriptedBridgeCollapse:MonoBehaviour
    {
        public Vector3Int firstCell=new Vector3Int(185,11,0);
        public Vector2Int size=new Vector2Int(8,2);
        public Vector2 landing=new Vector2(190,-35.8f);
        public bool HasTriggered {get;private set;}
        public bool IsRunning {get;private set;}
        public string Phase {get;private set;}="Ready";
        public void RestoreCompleted(){HasTriggered=true;IsRunning=false;Phase="Complete";var map=GameObject.Find("GroundTilemap")?.GetComponent<Tilemap>();if(map==null)return;for(int x=0;x<size.x;x++)for(int y=0;y<size.y;y++)map.SetTile(firstCell+new Vector3Int(x,y,0),null);map.GetComponent<TilemapCollider2D>()?.ProcessTilemapChanges();map.GetComponent<CompositeCollider2D>()?.GenerateGeometry();}
        public void Trigger(PlayerController2D player){if(!HasTriggered)StartCoroutine(Sequence(player));}
        void OnTriggerEnter2D(Collider2D other){var player=other.GetComponentInParent<PlayerController2D>();if(player!=null&&!HasTriggered&&player.transform.position.x<transform.position.x+1)StartCoroutine(Sequence(player));}
        IEnumerator Sequence(PlayerController2D player)
        {
            HasTriggered=true;IsRunning=true;
            var body=player.GetComponent<Rigidbody2D>();var combat=player.GetComponent<PlayerCombatController>();var health=player.GetComponent<HealthComponent>();var presentation=player.GetComponent<CharacterPresentation>();
            var interpolation=body.interpolation;body.interpolation=RigidbodyInterpolation2D.None;
            bool wasInvincible=health.Invincible;health.Invincible=true;player.enabled=false;if(combat!=null)combat.enabled=false;body.linearVelocity=Vector2.zero;
            Phase="Stumble";presentation.Act("bridgestumble",.55f);
            Babel.Runtime.UI.ScreenMessagePresenter.ShowCenter("The ancient stone gives way...",2);BableAudio.Sfx("21_orc_damage_1");
            yield return new WaitForSeconds(.55f);
            var fallingPieces=new System.Collections.Generic.List<GameObject>();
            var map=GameObject.Find("GroundTilemap").GetComponent<Tilemap>();
            for(int x=0;x<size.x;x++)for(int y=0;y<size.y;y++){
                var c=firstCell+new Vector3Int(x,y,0);var sprite=map.GetSprite(c);if(sprite==null)continue;
                var piece=new GameObject("Crumbling masonry");fallingPieces.Add(piece);piece.transform.position=map.GetCellCenterWorld(c);var sr=piece.AddComponent<SpriteRenderer>();sr.sprite=sprite;sr.sortingOrder=9;sr.color=map.GetColor(c);piece.transform.localScale=Vector3.one/sprite.bounds.size.x;
                var rb=piece.AddComponent<Rigidbody2D>();rb.gravityScale=2;rb.linearVelocity=new Vector2((x-size.x*.5f)*.5f,1+y);rb.angularVelocity=(x%2==0?1:-1)*70;Destroy(piece,2.5f);map.SetTile(c,null);
            }
            map.GetComponent<TilemapCollider2D>()?.ProcessTilemapChanges();map.GetComponent<CompositeCollider2D>()?.GenerateGeometry();
            // The shaft is open all the way to the lower floor. Keep the actual
            // body and camera travelling through it instead of hiding a teleport.
            Phase="Fall";presentation.Act("bridgefall",8f);body.linearVelocity=Vector2.down*3;
            var box=player.GetComponent<BoxCollider2D>();
            float startY=body.position.y, elapsed=0;
            bool landed=false;
            while(elapsed<8f){
                yield return new WaitForFixedUpdate();
                elapsed+=Time.fixedDeltaTime;
                body.linearVelocity=new Vector2(0,Mathf.Max(body.linearVelocity.y,-18f));
                float floorDistance=float.PositiveInfinity;
                foreach(var hit in Physics2D.BoxCastAll(box.bounds.center,TerrainMotion.Size(box)-Vector2.one*.025f,0,Vector2.down,10f))
                    if(TerrainMotion.Solid(hit.collider)&&hit.normal.y>.6f)floorDistance=Mathf.Min(floorDistance,hit.distance);
                if(body.position.y<startY-3&&floorDistance<8f)Phase="Approach";
                if(body.position.y<startY-3&&floorDistance<.075f&&Mathf.Abs(body.linearVelocity.y)<.2f){landed=true;break;}
            }
            if(!landed){
                Debug.LogError("Bridge descent did not reach a supporting floor; controls restored without teleporting.");
                body.interpolation=interpolation;health.Invincible=wasInvincible;player.enabled=true;if(combat!=null)combat.enabled=true;presentation.ReleaseAction();IsRunning=false;Phase="Interrupted";yield break;
            }
            Vector2 grounded=body.position;
            body.simulated=false;body.linearVelocity=Vector2.zero;
            Phase="Impact";presentation.Act("bridgeimpact",.65f);CombatAudio.Play("land",grounded,.95f);
            RelicFX.Burst("Shockwave",new Vector2(grounded.x,grounded.y+box.offset.y-box.size.y*.5f),Vector2.right,new Vector2(3.2f,.35f),.4f);
            yield return new WaitForSeconds(.65f);
            Phase="Rise";presentation.Act("bridgerise",.5f);yield return new WaitForSeconds(.5f);
            body.simulated=true;body.interpolation=interpolation;health.Invincible=wasInvincible;player.enabled=true;if(combat!=null)combat.enabled=true;presentation.ReleaseAction();IsRunning=false;Phase="Complete";
        }
    }
}
