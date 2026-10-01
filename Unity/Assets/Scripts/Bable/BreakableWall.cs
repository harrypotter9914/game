using UnityEngine;
using Babel.Runtime.Combat;
namespace Bable
{
    public sealed class BreakableWall : MonoBehaviour, IDamageable
    {
        public bool requiresShockwave=true;
        public string designReference;
        public Color stoneTint = Color.clear;
        bool broken;
        void Awake(){RefreshFractureArt();}
        public void RefreshFractureArt(){
            if(!requiresShockwave)return;
            var sr=GetComponent<SpriteRenderer>();var box=GetComponent<BoxCollider2D>();if(sr==null||box==null)return;
            if(stoneTint.a==0){string old=sr.sprite!=null?sr.sprite.name:"";stoneTint=old.Contains("Moss")||transform.position.x>=195&&transform.position.y<0?new Color(.65f,.68f,.43f):transform.position.y<0?new Color(.61f,.58f,.80f):new Color(.72f,.70f,.65f);}
            int x=Mathf.FloorToInt(transform.position.x),y=Mathf.FloorToInt(transform.position.y);
            int part=box.size.x>1.1f||box.size.y>1.1f?4:(x%2+2)%2+((y%2+2)%2)*2;
            var sprite=Resources.Load<Sprite>("Bable/NewArt/FracturedStone44_"+part);if(sprite==null)return;
            sr.sprite=sprite;sr.drawMode=SpriteDrawMode.Tiled;sr.size=box.size;sr.color=stoneTint;sr.sortingOrder=3;
        }
        public TeamAlignment Alignment=>TeamAlignment.Neutral;
        public bool CanReceiveDamage(TeamAlignment team)=>team==TeamAlignment.Player && !requiresShockwave;
        public void ReceiveDamage(DamageInfo damage){if(CanReceiveDamage(damage.SourceTeam))Break();}
        public void HitByShockwave(){Break();}
        public void RestoreBroken(){Break(true);}
        public void Break(){Break(false);}
        void Break(bool silent)
        {
            if(broken)return;broken=true;CampaignStore.MarkRemoved(CampaignStore.Identity(this));
            var ground=GameObject.Find("GroundTilemap")?.GetComponent<UnityEngine.Tilemaps.Tilemap>();
            var collider=GetComponent<Collider2D>();
            if(ground!=null&&collider!=null){
                var min=ground.WorldToCell(collider.bounds.min+Vector3.one*.01f);
                var max=ground.WorldToCell(collider.bounds.max-Vector3.one*.01f);
                for(int x=min.x;x<=max.x;x++)for(int y=min.y;y<=max.y;y++)ground.SetTile(new Vector3Int(x,y,0),null);
            }
            if(collider!=null)collider.enabled=false;
            var tileCollider=ground!=null?ground.GetComponent<UnityEngine.Tilemaps.TilemapCollider2D>():null;
            if(tileCollider!=null&&tileCollider.hasTilemapChanges)tileCollider.ProcessTilemapChanges();
            if(!silent)StoneChips.Burst(transform.position,stoneTint.a>0?stoneTint:Color.gray);
            if(!silent)CombatAudio.Play("stone_break",transform.position,.8f);Destroy(gameObject);
        }
    }
}
