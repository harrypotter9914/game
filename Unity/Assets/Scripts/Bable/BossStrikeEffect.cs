using UnityEngine;
using Babel.Runtime.Combat;
namespace Bable {
    // Damage follows the authored blade arc; the sprite owns the visible sweep.
    public sealed class BossStrikeEffect : MonoBehaviour {
        BossBrain owner; Transform target; float started,angle,range,axis; int damage,facing,epoch;
        bool landed,groundStrike,groundSound;
        public BossBrain Owner=>owner;
        public static void Begin(BossBrain boss,Transform victim,float degrees,float reach,int amount,int sign,float directionAngle=0) {
            var effect=new GameObject("Physical blade sweep").AddComponent<BossStrikeEffect>();
            effect.axis=directionAngle;effect.owner=boss;effect.target=victim;effect.angle=degrees;effect.range=reach;effect.damage=amount;effect.facing=sign;effect.epoch=boss.AttackEpoch;effect.started=Time.time;
            effect.groundStrike=boss.profile.kind==BossKind.Shockwave&&boss.CurrentAttack=="Heavy";
            foreach(var previous in FindObjectsByType<BossStrikeEffect>(FindObjectsSortMode.None))
                if(previous!=effect&&previous.owner==boss){previous.enabled=false;Destroy(previous.gameObject);}

        }
        void Update(){
            if(Babel.Runtime.Core.GameSession.Instance!=null&&Babel.Runtime.Core.GameSession.Instance.IsPaused)return;
            if(owner==null||!owner.AttackValid(epoch)){Destroy(gameObject);return;}
            float age=Time.time-started;if(age>=.42f){Destroy(gameObject);return;}
            float progress=Mathf.Clamp01((age-.06f)/.25f),previous=Mathf.Clamp01((age-Time.deltaTime-.06f)/.25f);
            Vector2 origin=owner.AttackCenter;transform.position=origin;
            bool rising=owner.CurrentAttack=="AntiAir"||owner.CurrentAttack=="Rising";
            float start=angle*(rising?-.5f:.5f),end=-start;
            float head=Mathf.Lerp(start,end,progress);
            if(groundStrike&&!groundSound&&age>=.16f){
                float a=(axis+head)*Mathf.Deg2Rad;Vector2 dir=new Vector2(Mathf.Cos(a)*facing,Mathf.Sin(a));
                foreach(var h in Physics2D.RaycastAll(origin,dir,Mathf.Min(range,owner.WeaponReach))){
                    if(!TerrainMotion.Solid(h.collider))continue;
                    if(h.normal.y>.5f){groundSound=true;CombatAudio.Boss(owner.gameObject,"slam",.8f);CombatImpact.Spawn(h.point,Vector2.up,false);}break;
                }
            }
            if(landed||age<.06f||age>.31f||target==null||!owner.InArena(target.position))return;
            var collider=target.GetComponent<Collider2D>();var hp=target.GetComponent<HealthComponent>();
            Vector2 point=collider!=null?(Vector2)collider.bounds.center:(Vector2)target.position;
            Vector2 delta=point-origin;
            // Sweep the blade itself. A target's large diagonal radius must not expand
            // the entire attack sector into an invisible body-contact damage zone.
            bool crossing=false;
            float reach=Mathf.Min(range,owner.WeaponReach);
            float oldHead=Mathf.Lerp(start,end,previous);
            int samples=Mathf.Max(1,Mathf.CeilToInt(Mathf.Abs(oldHead-head)/8));
            for(int i=0;i<=samples&&!crossing;i++){
                float a=(axis+Mathf.Lerp(oldHead,head,i/(float)samples))*Mathf.Deg2Rad;
                Vector2 direction=new Vector2(Mathf.Cos(a)*facing,Mathf.Sin(a));
                Vector2 hilt=origin+direction*.35f;
                foreach(var contact in Physics2D.CircleCastAll(hilt,.12f,direction,Mathf.Max(0,reach-.35f)))
                    if(contact.collider==collider){crossing=CombatGeometry.Clear(hilt,contact.point);break;}
            }
            if(crossing&&CombatGeometry.Clear(origin,point)&&hp!=null&&hp.CanReceiveDamage(TeamAlignment.Enemy)){
                landed=true;hp.ReceiveDamage(new DamageInfo(damage,origin,delta.normalized*3,owner.gameObject,TeamAlignment.Enemy));

            }
        }

    }
}
