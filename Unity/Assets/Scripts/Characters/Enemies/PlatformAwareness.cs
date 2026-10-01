using UnityEngine;
using UnityEngine.Tilemaps;
namespace Babel.Runtime.Characters.Enemies
{
    public sealed class PlatformAwareness : MonoBehaviour
    {
        Tilemap ground; Collider2D body;
        Bable.BossRoomBoundary[] rooms;
        void Awake(){ground=GameObject.Find("GroundTilemap")?.GetComponent<Tilemap>();body=GetComponent<Collider2D>();rooms=FindObjectsByType<Bable.BossRoomBoundary>(FindObjectsSortMode.None);}
        public bool FloorUnder(Vector2 point,out Vector3Int floor)
        {
            floor=default;if(ground==null)return false;
            var c=ground.WorldToCell(point);
            for(int i=0;i<16;i++){var probe=c+Vector3Int.down*i;if(ground.HasTile(probe)){floor=probe;return true;}}
            return false;
        }
        public bool SameReachablePlatform(Transform target)
        {
            if(target==null||ground==null||body==null)return false;
            foreach(var room in rooms)if(room!=null&&room.Contains(target.position))return false;
            var other=target.GetComponent<Collider2D>();if(other==null)return false;
            if(!FloorUnder(new Vector2(body.bounds.center.x,body.bounds.min.y+.15f),out var a)||!FloorUnder(new Vector2(other.bounds.center.x,other.bounds.min.y+.15f),out var b))return false;
            if(a.y!=b.y)return false;
            for(int x=Mathf.Min(a.x,b.x);x<=Mathf.Max(a.x,b.x);x++){
                if(!ground.HasTile(new Vector3Int(x,a.y,0)))return false;
                if(ground.HasTile(new Vector3Int(x,a.y+1,0))||ground.HasTile(new Vector3Int(x,a.y+2,0)))return false;
            }
            foreach(var hit in Physics2D.LinecastAll(body.bounds.center,other.bounds.center))
                if(hit.collider!=null&&!hit.collider.isTrigger&&hit.collider.GetComponent<Bable.BreakableWall>()!=null)return false;
            return true;
        }
        public bool CanStep(int direction)
        {
            if(ground==null||body==null)return true;
            var b=body.bounds;float x=b.center.x+direction*(b.extents.x+.35f);
            var foot=new Vector2(x,b.min.y-.2f);
            foreach(var room in rooms)if(room!=null&&room.Contains(new Vector2(x,b.center.y)))return false;
            bool floor=ground.HasTile(ground.WorldToCell(foot))||ground.HasTile(ground.WorldToCell(foot+Vector2.down*.4f));
            if(!floor)return false;
            for(float y=b.min.y+.2f;y<b.max.y-.1f;y+=.5f)if(ground.HasTile(ground.WorldToCell(new Vector2(x,y))))return false;
            foreach(var hit in Physics2D.RaycastAll(new Vector2(b.center.x,b.center.y),Vector2.right*direction,b.extents.x+.4f))
                if(hit.collider!=null&&!hit.collider.isTrigger&&hit.collider.GetComponent<Bable.BreakableWall>()!=null)return false;
            return true;
        }
    }
}
