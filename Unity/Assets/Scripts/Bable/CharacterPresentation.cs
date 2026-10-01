using UnityEngine;
using Babel.Runtime.Characters.Player;
using Babel.Runtime.Characters.Enemies;
using Babel.Runtime.Combat;

namespace Bable
{
    public sealed class CharacterPresentation : MonoBehaviour
    {
        public Animator animator;
        public bool player;
        public bool mirrorRightFrames;
        private Rigidbody2D body;
        private PlayerController2D controller;
        private HealthComponent health;
        private string action = "idle", current;
        private float until;
        private float actionDuration;
        private bool segment; private float segmentFrom,segmentTo,segmentStarted;
        private float hurtUntil;
        private int facing = 1;
        private Vector3 previousPosition;
        private bool wasGrounded;
        private Vector3 visualHome;
        private float smoothedMotion;
        private float runPhase;
        private float strideLength;
        private ActorMovementAudio movementAudio;
        private float wallVisualOffset;
        void Awake()
        {
            body = GetComponent<Rigidbody2D>(); controller = GetComponent<PlayerController2D>();
            health = GetComponent<HealthComponent>();
            movementAudio=GetComponent<ActorMovementAudio>()??gameObject.AddComponent<ActorMovementAudio>();
            if(player&&GetComponent<PlayerHitFeedback>()==null)gameObject.AddComponent<PlayerHitFeedback>();
            previousPosition=transform.position;
            if(GetComponent<ActorTerrainSweep>()==null)gameObject.AddComponent<ActorTerrainSweep>();
            if(animator!=null){visualHome=animator.transform.localPosition;var sprite=animator.GetComponent<SpriteRenderer>();strideLength=Mathf.Max(1.4f,sprite.bounds.size.y*2.1f);}
            health.Damaged += OnDamage;
            health.Died += OnDeath;
        }
        void OnDestroy() { if (health != null) {health.Damaged -= OnDamage;health.Died -= OnDeath;} }
        void OnDeath()
        {
            CombatAudio.Play("death",transform.position,.65f);
            if(player || animator==null)return;
            var ghost=Instantiate(animator.gameObject,animator.transform.position,Quaternion.identity);ghost.name="Fallen warrior";ghost.transform.localScale=animator.transform.lossyScale;
            string key=(facing<0&&!mirrorRightFrames?"left":"right")+"dead";var a=ghost.GetComponent<Animator>();a.speed=1;if(a.HasState(0,Animator.StringToHash(key)))a.Play(key,0,0);Destroy(ghost,1.1f);
        }
        void OnDamage(DamageInfo info) { hurtUntil=Time.time+.25f;var boss=GetComponent<BossBrain>();if(boss==null||boss.State=="Recover"||boss.State=="Dormant")Act("sufferattack", .25f); CombatAudio.Hurt(gameObject);if(!player&&animator!=null)ActorHitFlash.Show(animator.GetComponent<SpriteRenderer>()); }
        public void Face(int direction) { if(direction!=0)facing=direction; }
        public void Act(string next, float duration) { segment=false; action = next; actionDuration=duration; until = Time.time + duration; current=null; }
        public void ActSegment(string next,float duration,float from,float to){Act(next,duration);segment=true;segmentFrom=from;segmentTo=to;segmentStarted=Time.time;}
        public void ReleaseAction(){segment=false;until=0;current=null;}
        // Distance represented by one complete left/right cycle, measured from the authored foot trajectory.
        float GaitCycleDistance(string state)
        {
            if(player)return 2.5201f;
            string actor=animator.runtimeAnimatorController!=null?animator.runtimeAnimatorController.name:"";
            bool walk=state=="walk";
            switch(actor){
                case "MeleeGuard":return walk?.9473f:3.5595f;
                case "ShieldGuard":return walk?.7751f:2.9126f;
                case "RangedGuard":return walk?.961f:3.6112f;
                case "GiantGuard":return walk?.8979f:3.3738f;
                default:return strideLength*(walk?.36f:.50f);
            }
        }
        void LateUpdate()
        {
            if (animator == null || body == null) return;
            animator.GetComponent<SpriteRenderer>().color=Time.time<hurtUntil?Color.Lerp(new Color(1,.25f,.18f),Color.white,Mathf.Clamp01((hurtUntil-Time.time)/.25f)):Color.white;
            float motion=Mathf.Abs(body.linearVelocity.x);previousPosition=transform.position;
            if(Time.deltaTime<=0)return;
            smoothedMotion=Mathf.Lerp(smoothedMotion,motion<100?motion:0,1-Mathf.Exp(-18*Time.deltaTime));motion=smoothedMotion;
            if (controller != null) facing = controller.FacingSign;
            else {var enemy=GetComponent<EnemyControllerBase>();if(enemy!=null)facing=enemy.LookDirection;}
            var combat=player?GetComponent<PlayerCombatController>():null;
            bool casting=combat!=null&&combat.IsShockwaveActive;
            if(casting)facing=combat.ShockwaveFacing;
            if(player&&controller.IsGrounded&&!wasGrounded&&Time.time>=until)Act("land",.18f);
            if(player&&!controller.IsGrounded&&wasGrounded&&body.linearVelocity.y>1&&Time.time>=until)Act("takeoff",.12f);
            if(player)wasGrounded=controller.IsGrounded;
            var state = Time.time < until ? action : (motion>.25f&&motion<100 ? "run" : "idle");
            var minion=GetComponent<EnemyControllerBase>();
            if(!player&&!(minion is BossBrain)&&state=="run"&&minion!=null&&minion.BehaviourState=="Patrol")state="walk";
            if (player && Time.time >= until && !controller.IsGrounded) state = controller.IsWallSliding ? "wallslide" : body.linearVelocity.y < -.2f ? "fall" : "jump";
            if (player && controller.IsCharging) state = "charge";
            if (player && controller.IsDashing) state = "crystaldash";
            var channel = player ? GetComponent<PlayerHealChannelController>() : null;
            if (channel != null && channel.IsChanneling && Time.time >= until) state = "heal";
            if(casting)state=controller.IsGrounded&&motion>.25f?"run":"shockwave";
            var recoil=GetComponent<HitRecoil>();
            if(recoil!=null&&recoil.IsRecoiling)state="sufferattack";
            if (health.CurrentHealth == 0) state = "dead";
            // A gripping pose must face the wall even during the first frame of
            // input away from it. Jumping and other actions keep normal facing.
            if(player && state=="wallslide")facing=controller.WallDirection;
            if(mirrorRightFrames)animator.GetComponent<SpriteRenderer>().flipX=facing<0;
            var key = (facing < 0 && !mirrorRightFrames ? "left" : "right") + state;
            if(player&&Babel.Runtime.Core.GameSession.Instance!=null&&!Babel.Runtime.Core.GameSession.Instance.HasWeapon){
                key="rightunarmed"+state;
                if(state.StartsWith("bridge"))key="rightunarmed"+(state=="bridgefall"?"fall":state=="bridgestumble"?"takeoff":"land");
                if(!animator.HasState(0,Animator.StringToHash(key)))key="rightunarmedidle";
            }
            if(player){
                float angle=controller.IsDashing&&body.linearVelocity.sqrMagnitude>1?Mathf.Atan2(body.linearVelocity.y,body.linearVelocity.x)*Mathf.Rad2Deg-(facing<0?180:0):0;
                animator.transform.localRotation=Quaternion.Euler(0,0,angle);animator.transform.localPosition=Quaternion.Euler(0,0,angle)*visualHome;
            }
            if (!animator.HasState(0, Animator.StringToHash(key))) key = (facing < 0 && !mirrorRightFrames ? "left" : "right") + "idle";
            if (key != current && animator.HasState(0, Animator.StringToHash(key))) { animator.speed=1;animator.Play(key, 0, 0);animator.Update(0);if(Time.time<until&&actionDuration>0)animator.speed=Mathf.Clamp(animator.GetCurrentAnimatorStateInfo(0).length/actionDuration,.1f,4);current = key; }
            if(segment&&Time.time<until){animator.speed=0;animator.Play(key,0,Mathf.Min(.999f,Mathf.Lerp(segmentFrom,segmentTo,Mathf.Clamp01((Time.time-segmentStarted)/Mathf.Max(.01f,segmentFrom>=.25f?Mathf.Min(.26f,actionDuration):actionDuration)))));}
            // Advance gait by travel distance, independent of render rate and clip FPS.
            // Stopping or changing facing does not restart the same leading foot.
            if((state=="run"||state=="walk")&&Time.time>=until){float stride=GaitCycleDistance(state);if(player||!(minion is BossBrain))stride*=Mathf.Abs(animator.transform.lossyScale.x);float cadence=Mathf.Clamp(motion/Mathf.Max(.5f,stride),.05f,5f);runPhase=Mathf.Repeat(runPhase+cadence*Time.deltaTime,1);animator.speed=0;animator.Play(key,0,runPhase);}
            if(state=="heal" && channel!=null){animator.speed=0;animator.Play(key,0,Mathf.Min(.999f,channel.Progress));}
            if(casting&&state=="shockwave"){animator.speed=0;animator.Play(key,0,0);animator.Update(0);}
            if(casting&&state=="run"&&Time.time>=until&&body.linearVelocity.x*facing<0){runPhase=Mathf.Repeat(runPhase-2*Mathf.Abs(body.linearVelocity.x)/Mathf.Max(.5f,GaitCycleDistance("run")*Mathf.Abs(animator.transform.lossyScale.x))*Time.deltaTime,1);animator.Play(key,0,runPhase);animator.Update(0);}
            if(player && state=="wallslide"){
                // The opening contact pose has a planted palm and no sword in
                // front of it. Later authored frames are a crouch/withdrawal,
                // not a looping grip, and must not be played while attached.
                animator.speed=0;animator.Play(key,0,0);animator.Update(0);
                var sprite=animator.GetComponent<SpriteRenderer>();
                float edge=facing>0?sprite.bounds.max.x:sprite.bounds.min.x;
                wallVisualOffset=controller.WallSurfaceX-facing*.012f-edge;
            }
            else wallVisualOffset=Mathf.MoveTowards(wallVisualOffset,0,Time.deltaTime*14);
            if(player)animator.transform.position+=Vector3.right*wallVisualOffset;
            movementAudio.Tick(state,runPhase);
        }
    }
}



