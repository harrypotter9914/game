using UnityEngine;
using Babel.Runtime.Characters.Enemies;
using Babel.Runtime.Combat;
namespace Bable {
    // The imported arrow's pivot is its tip, matching the swept hit collider.
    public sealed class MagicArrowVisual:MonoBehaviour {
        static Sprite arrow;
        public static EnemyProjectile Launch(GameObject owner,Vector2 position,Vector2 direction,float speed,int damage){
            if(arrow==null)arrow=Resources.Load<Sprite>("Bable/NewArt/MagicArrow40");
            if(arrow==null){Debug.LogError("Missing magic arrow sprite");return null;}
            var go=new GameObject("Magic arrow");go.transform.position=position;go.transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg);
            var sprite=go.AddComponent<SpriteRenderer>();sprite.sprite=arrow;sprite.sortingOrder=21;
            var circle=go.AddComponent<CircleCollider2D>();circle.radius=.10f;circle.isTrigger=true;
            go.AddComponent<Rigidbody2D>();var shot=go.AddComponent<EnemyProjectile>();shot.Launch(direction,damage,speed,owner,TeamAlignment.Enemy);
            go.AddComponent<MagicArrowVisual>();return shot;
        }
    }
}
