using UnityEngine;
using Babel.Runtime.Characters.Enemies;
using Babel.Runtime.Combat;
namespace Bable
{
    public static class BableCombatFX
    {
        public static EnemyProjectile Projectile(GameObject source,Vector2 position,Vector2 direction,float speed,int damage,Color color,float size=.5f,string art="Shockwave")
        {
            var go=new GameObject(art+" projectile");go.transform.position=position;
            go.transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg);
            var sr=go.AddComponent<SpriteRenderer>();sr.sprite=CombatVfxFrames.Get(art,0);sr.color=color;sr.sortingOrder=20;
            var s=sr.sprite==null?1:size/sr.sprite.bounds.size.y;go.transform.localScale=Vector3.one*s;
            var collider=go.AddComponent<CircleCollider2D>();collider.radius=size/(2*s);collider.isTrigger=true;
            var body=go.AddComponent<Rigidbody2D>();body.bodyType=RigidbodyType2D.Kinematic;body.collisionDetectionMode=CollisionDetectionMode2D.Continuous;
            var projectile=go.AddComponent<EnemyProjectile>();projectile.effectArt=art;projectile.Launch(direction,damage,speed,source,TeamAlignment.Enemy);return projectile;
        }
    }
}
