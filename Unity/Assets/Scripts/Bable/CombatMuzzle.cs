using UnityEngine;
namespace Bable {
    // Sockets are measured on the release poses, not on the physics body's centre.
    public static class CombatMuzzle {
        public static bool TryResolve(GameObject actor,int facing,bool raisedBow,out Vector2 origin) {
            var sr=actor.GetComponent<CharacterPresentation>()?.animator?.GetComponent<SpriteRenderer>();
            var col=actor.GetComponent<Collider2D>();
            Vector2 centre=col!=null?(Vector2)col.bounds.center:(Vector2)actor.transform.position;
            origin=centre;
            if(sr==null||sr.sprite==null)return false;
            var boss=actor.GetComponent<BossBrain>();
            Vector2 socket=boss==null?(raisedBow?new Vector2(.85f,.75f):new Vector2(.89f,.60f)):
                boss.profile.kind==BossKind.Shockwave?new Vector2(.60f,.16f):
                boss.profile.kind==BossKind.Nero?new Vector2(.86f,.59f):new Vector2(.84f,.61f);
            var bounds=sr.sprite.bounds;
            Vector3 local=bounds.min+Vector3.Scale(bounds.size,new Vector3(socket.x,socket.y,0));
            if(sr.flipX)local.x=-local.x;
            origin=sr.transform.TransformPoint(local);
            // A ground-slam wave clears the floor by its half-height.
            if(boss!=null&&boss.profile.kind==BossKind.Shockwave&&col!=null){
                origin.x=centre.x+facing*(col.bounds.extents.x+.7f);
                origin.y=col.bounds.min.y+.90f;
            }
            Vector2 delta=origin-centre;
            if(delta.sqrMagnitude<.001f)return false;
            foreach(var hit in Physics2D.CircleCastAll(centre,.13f,delta.normalized,delta.magnitude))
                if(TerrainMotion.Solid(hit.collider))return false;
            return true;
        }
    }
}
