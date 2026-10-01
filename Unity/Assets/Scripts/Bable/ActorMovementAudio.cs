using UnityEngine;
using Babel.Runtime.Combat;
using Babel.Runtime.Characters.Player;
using Babel.Runtime.Characters.Enemies;
namespace Bable {
    public sealed class ActorMovementAudio:MonoBehaviour {
        Collider2D shape;Rigidbody2D body;HealthComponent health;BossBrain boss;PlayerController2D player;
        readonly RaycastHit2D[] hits=new RaycastHit2D[16];
        bool initialized,grounded,gaitValid;float previousPhase,airSince,fallSpeed,nextStep;Vector2 previousPosition;int foot;
        CombatSoundLoop movementLoop;string loopCue;
        public int StepCount {get;private set;} public int LandingCount {get;private set;}
        void Awake(){shape=GetComponent<Collider2D>();body=GetComponent<Rigidbody2D>();health=GetComponent<HealthComponent>();boss=GetComponent<BossBrain>();player=GetComponent<PlayerController2D>();}
        bool OnGround(){if(player!=null)return player.IsGrounded;if(shape==null||!shape.enabled||!body.simulated)return false;int n=shape.Cast(Vector2.down,hits,.12f);for(int i=0;i<n;i++)if(TerrainMotion.Solid(hits[i].collider)&&hits[i].normal.y>.65f)return true;return false;}
        string Family=>player!=null?"player":boss!=null&&boss.profile.kind==BossKind.Nero?"royal":boss!=null||GetComponent<GiantEnemyController>()!=null?"heavy":"light";
        public void Tick(string state,float phase){
            if(Time.timeScale==0)return;
            if(health==null||health.CurrentHealth<=0||TowerLoading.Busy||TowerDialogue.StoryActive){StopLoop();initialized=false;gaitValid=false;return;}
            string desiredLoop=null;
            if(boss!=null&&boss.isActiveAndEnabled&&boss.Engaged&&!boss.DialogueHold){if(boss.State=="Burrow")desiredLoop="burrow_move";else if(boss.profile.kind==BossKind.Crystal&&body.linearVelocity.sqrMagnitude>.09f)desiredLoop="crystal_flight";}
            if(desiredLoop!=loopCue){StopLoop();loopCue=desiredLoop;if(loopCue!=null)movementLoop=CombatSoundLoop.Begin(transform,loopCue,.18f,12,true);}
            bool now=OnGround();Vector2 position=transform.position;float distance=Vector2.Distance(position,previousPosition);
            bool invisible=boss!=null&&(boss.profile.kind==BossKind.Crystal||boss.profile.kind==BossKind.Burrow);
            if(!initialized){initialized=true;grounded=now;airSince=Time.time;previousPosition=position;previousPhase=phase;return;}
            if(distance>3){grounded=now;gaitValid=false;fallSpeed=0;airSince=Time.time;}
            if(!now){if(grounded)airSince=Time.time;fallSpeed=Mathf.Max(fallSpeed,-body.linearVelocity.y);}
            if(now&&!grounded){if(player==null&&!invisible&&Time.time-airSince>.12f&&fallSpeed>2){CombatAudio.Movement("land_"+Family,position,Family=="light"?.25f:.5f,14);LandingCount++;}fallSpeed=0;}
            bool walking=now&&!invisible&&(state=="run"||state=="walk")&&distance>.0001f&&distance<3&&Mathf.Abs(body.linearVelocity.x)>.25f;
            if(walking&&gaitValid&&Time.time>=nextStep){
                int half=Mathf.FloorToInt(Mathf.Repeat(phase+.08f,1)*2),oldHalf=Mathf.FloorToInt(Mathf.Repeat(previousPhase+.08f,1)*2);
                if(half!=oldHalf){string family=Family;float gain=family=="player"?.18f:family=="light"?.13f:family=="royal"?.30f:.38f;
                    CombatAudio.Movement("step_"+family+"_"+(foot++%2),position,gain,family=="player"?9:family=="light"?10:14);StepCount++;nextStep=Time.time+.14f;}
            }
            grounded=now;gaitValid=walking;previousPosition=position;previousPhase=phase;
        }
        void StopLoop(){if(movementLoop!=null)movementLoop.Stop();movementLoop=null;loopCue=null;}
        void OnDisable(){StopLoop();}
    }
}
