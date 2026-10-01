using UnityEngine;
using Babel.Runtime.Characters.Player;

namespace Babel.Runtime.Combat
{
    [DefaultExecutionOrder(300)]
    public sealed class HitRecoil : MonoBehaviour
    {
        Rigidbody2D body; Collider2D shape; HealthComponent health; PlayerController2D player;
        readonly RaycastHit2D[] contacts = new RaycastHit2D[24];
        float started, duration, horizontalSpeed;
        bool active;
        public bool IsRecoiling => active && Time.time < started + duration;

        void Awake()
        {
            body=GetComponent<Rigidbody2D>();shape=GetComponent<Collider2D>();
            health=GetComponent<HealthComponent>();player=GetComponent<PlayerController2D>();
            health.Damaged+=OnDamage;health.Died+=Cancel;
        }
        bool Grounded()
        {
            int count=shape.Cast(Vector2.down,contacts,.08f);
            for(int i=0;i<count;i++)
                if(Bable.TerrainMotion.Solid(contacts[i].collider)&&contacts[i].normal.y>.65f)return true;
            return false;
        }
        void OnDamage(DamageInfo info)
        {
            if(!enabled||!body.simulated)return;
            if(health.CurrentHealth<=0){if(player!=null){player.InterruptForHit();GetComponent<PlayerCombatController>()?.InterruptForHit();}return;}
            // Administrative damage/respawn tests have no attack source or impulse.
            if(info.Source==null&&info.Force.sqrMagnitude<.001f)return;
            bool verticalStrike=info.SourceTeam==TeamAlignment.Player&&Mathf.Abs(info.Force.y)>Mathf.Abs(info.Force.x);
            if(player==null&&verticalStrike)
            {
                // Downslash keeps the player's pogo. Upward strikes lift only airborne small enemies.
                if(info.Force.y<=0||Grounded())return;
                horizontalSpeed=body.linearVelocity.x;
                body.linearVelocity=new Vector2(horizontalSpeed,Mathf.Max(body.linearVelocity.y,8));
                Begin(.24f);return;
            }
            float side;
            if(Mathf.Abs(info.Force.x)>.01f)side=Mathf.Sign(info.Force.x);
            else {
                Vector2 from=info.Source!=null?(Vector2)info.Source.transform.position:info.Point;
                float delta=shape.bounds.center.x-from.x;
                side=Mathf.Abs(delta)>.02f?Mathf.Sign(delta):Mathf.Abs(info.Force.x)>.01f?Mathf.Sign(info.Force.x):player!=null?-player.FacingSign:1;
            }
            horizontalSpeed=side*(player!=null?9.5f:9f);
            if(player!=null){player.InterruptForHit();GetComponent<PlayerCombatController>()?.InterruptForHit();}
            Begin(player!=null?.28f:.3f);
            body.linearVelocity=new Vector2(horizontalSpeed,body.linearVelocity.y);
        }
        void Begin(float seconds){started=Time.time;duration=seconds;active=true;}
        void FixedUpdate()
        {
            if(!active)return;
            if(!body.simulated||health.CurrentHealth<=0||Bable.TowerLoading.Busy){Cancel();return;}
            if(Time.timeScale==0||Babel.Runtime.Core.GameSession.Instance!=null&&Babel.Runtime.Core.GameSession.Instance.IsPaused)return;
            if(!IsRecoiling){Cancel();return;}
            float speed=horizontalSpeed*(1-Mathf.Clamp01((Time.time-started)/duration));
            if(Mathf.Abs(speed)>.01f){
                Vector2 direction=Vector2.right*Mathf.Sign(speed);float distance=Mathf.Abs(speed)*Time.fixedDeltaTime;
                int count=shape.Cast(direction,contacts,distance+.025f);
                for(int i=0;i<count;i++)if(Bable.TerrainMotion.Solid(contacts[i].collider)&&Vector2.Dot(contacts[i].normal,direction)<-.1f)
                    distance=Mathf.Min(distance,Mathf.Max(0,contacts[i].distance-.025f));
                speed=Mathf.Sign(speed)*distance/Time.fixedDeltaTime;
            }
            body.linearVelocity=new Vector2(speed,body.linearVelocity.y);
        }
        public void Cancel(){if(active&&body!=null)body.linearVelocity=new Vector2(0,body.linearVelocity.y);active=false;}
        void OnDisable(){Cancel();}
        void OnDestroy(){if(health!=null){health.Damaged-=OnDamage;health.Died-=Cancel;}}
    }
}
