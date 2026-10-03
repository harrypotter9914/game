using UnityEngine;
using UnityEngine.Tilemaps;
using Babel.Runtime.Characters.Enemies;
using Babel.Runtime.Combat;
using Babel.Runtime.Characters.Player;
namespace Bable
{
    // One controller and one profile per boss, shared by campaign and laboratories.
    // All transitions use scaled game time: pause freezes windups and attacks together.
    public sealed class BossBrain : EnemyControllerBase
    {
        public BossProfile profile;
        public Vector2 arenaCenter;
        public Vector2 arenaSize=new(34,18);
        public string State {get;private set;}="Dormant";
        public bool Engaged {get;private set;}
        public bool DialogueHold;
        public void FaceDialogueTarget(Transform player){if(player==null)return;FaceTarget(player.position.x-transform.position.x);GetComponent<CharacterPresentation>()?.Face(FacingSign);}
        public void SetDialoguePose(bool active){DialogueHold=active;if(profile!=null&&profile.kind==BossKind.Burrow&&visual!=null)visual.enabled=active||Engaged&&State!="Burrow";if(active){Body.linearVelocity=Vector2.zero;GetComponent<CharacterPresentation>()?.Act("idle",60);}else GetComponent<CharacterPresentation>()?.ReleaseAction();}
        public bool PhaseTwo => profile!=null && profile.kind==BossKind.Nero && (phasePresented || Health.CurrentHealth<=profile.health*.5f);
        public Vector2 LockedTarget {get;private set;}
        float deadline,fanReady,comboReady,nextShot,orbit,gravity, stateStarted, decisionReady, waveReady;
        int comboStep,airStep,flightShots; float nextTrap;
        public float WeaponReach => profile.kind==BossKind.Shockwave?2.6f:profile.kind==BossKind.Nero?2.4f:profile.kind==BossKind.Aerial?1.9f:1.5f;
        public bool BurrowVulnerable => profile.kind!=BossKind.Burrow || DialogueHold || State=="Recover" || State=="IntroEmerge" || State=="EmergeWarning" && StateProgress>=.3f || State=="Dig" && StateProgress<.85f;
        BurrowPresentation burrowVisual;
        readonly System.Collections.Generic.List<Vector2> standingPoints=new();
        public float StateProgress => Mathf.Clamp01((Time.time-stateStarted)/Mathf.Max(.001f,deadline-stateStarted));
        public float FootOffset {get;private set;}
        public string CurrentAttack => attack;
        public int AttackEpoch {get;private set;}
        public Vector2 AttackCenter=>bodyCollider!=null?(Vector2)bodyCollider.bounds.center:(Vector2)transform.position;
        public bool AttackValid(int epoch)=>epoch==AttackEpoch&&isActiveAndEnabled&&Engaged&&!DialogueHold&&Health!=null&&Health.CurrentHealth>0&&TargetTransform!=null&&!TowerDialogue.StoryActive;
        string AttackAnimation(string name)=>name=="AntiAir"?"antiair":name=="Heavy"?"heavy":name=="Quick"?"quick":name=="Wave"?"shockwave":name=="Leap"?"jump":name=="AirSides"?"airside":name=="Rising"?"rising":name=="Dash"?"special":name=="Combo"?"attack":"cast";
        void Pose(string name,float duration,float from,float to)=>GetComponent<CharacterPresentation>()?.ActSegment(name,duration,from,to);
        int pattern;
        float antiAirReady;
        int repeatedAttacks;
        bool phasePending;
        string attack;
        bool hit;
        bool phasePresented;
        Collider2D bodyCollider;
        SpriteRenderer visual;
        Vector2 dashDirection;
        Vector2 spawn;
        BossFlightRoute flightRoute;
        protected override void Awake()
        {
            base.Awake(); spawn=transform.position;gravity=Body.gravityScale;
            bodyCollider=GetComponent<Collider2D>();visual=GetComponent<CharacterPresentation>()?.animator?.GetComponent<SpriteRenderer>();
            if(profile!=null&&profile.kind==BossKind.Crystal){gravity=0;Body.gravityScale=0;flightRoute=GetComponent<BossFlightRoute>();if(flightRoute==null)flightRoute=gameObject.AddComponent<BossFlightRoute>();}
            if(profile!=null)Health.Configure(profile.health,profile.health);
            Health.Died+=Cleanup;
            if(profile!=null&&profile.kind==BossKind.Burrow)Health.Invincible=true;
            var box=bodyCollider as BoxCollider2D;
            FootOffset=box!=null?(box.offset.y-box.size.y*.5f)*transform.lossyScale.y:-1;
            if(profile!=null&&profile.kind==BossKind.Burrow){CacheFloors();burrowVisual=gameObject.AddComponent<BurrowPresentation>();}
            if(profile!=null&&profile.kind==BossKind.Burrow){if(visual!=null)visual.enabled=false;Body.simulated=false;bodyCollider.enabled=false;Health.Invincible=true;}
        }
        protected override void OnDestroy(){base.OnDestroy();if(Health!=null)Health.Died-=Cleanup;Cleanup();}
        void Cleanup(){foreach(var strike in FindObjectsByType<BossStrikeEffect>(FindObjectsSortMode.None))if(strike.Owner==this){strike.enabled=false;Destroy(strike.gameObject);}}
        public bool InArena(Vector2 p)=>Mathf.Abs(p.x-arenaCenter.x)<arenaSize.x*.5f && Mathf.Abs(p.y-arenaCenter.y)<arenaSize.y*.5f;
        public bool CanStartEncounter(PlayerController2D player){
            if(player==null||!InArena(player.transform.position))return false;
            var c=player.GetComponent<Collider2D>();
            bool landed=profile.kind==BossKind.Crystal||player.IsGrounded;
            return landed&&(c==null||c.bounds.min.y>=arenaCenter.y-arenaSize.y*.5f-.15f);
        }
        void SyncBurrowBody(){
            if(profile.kind!=BossKind.Burrow)return;
            bool exposed=BurrowVulnerable;
            bodyCollider.enabled=exposed;Body.simulated=exposed;Health.Invincible=!exposed;
        }
        void LateUpdate(){
            if(profile==null||Body==null||bodyCollider==null)return;
            if(profile.kind==BossKind.Burrow)SyncBurrowBody();
            if(Engaged&&Body.simulated){
                float half=bodyCollider.bounds.extents.x+.08f;
                float x=Mathf.Clamp(Body.position.x,arenaCenter.x-arenaSize.x*.5f+half,arenaCenter.x+arenaSize.x*.5f-half);
                if(Mathf.Abs(x-Body.position.x)>.001f){Body.position=new Vector2(x,Body.position.y);Body.linearVelocity=new Vector2(0,Body.linearVelocity.y);}
            }
        }
        public void ResetEncounter()
        {
            AttackEpoch++;StopAllCoroutines();Cleanup();Body.simulated=true;bodyCollider.enabled=true;Body.gravityScale=gravity;
            Health.Invincible=false;if(visual!=null)visual.enabled=true;
            transform.position=spawn;Body.linearVelocity=Vector2.zero;Health.Configure(profile.health,profile.health);
            State="Dormant";Engaged=false;pattern=comboStep=airStep=flightShots=0;nextTrap=0;fanReady=comboReady=nextShot=0;
            phasePresented=phasePending=false;decisionReady=waveReady=antiAirReady=0;repeatedAttacks=0;attack=null;
            if(flightRoute!=null)flightRoute.ResetRoute();
            if(profile.kind==BossKind.Burrow){Body.simulated=false;bodyCollider.enabled=false;Health.Invincible=true;if(visual!=null)visual.enabled=false;}
        }
        protected override void TickAI()
        {
            if(DialogueHold){Body.linearVelocity=Vector2.zero;return;}
            if(profile==null)return;
            if(!InArena(TargetTransform.position)) {if(Engaged)ResetEncounter();return;}
            if(!Engaged){if(!CanStartEncounter(TargetTransform.GetComponent<PlayerController2D>()))return;Engaged=true;if(profile.kind==BossKind.Burrow){LockedTarget=SafeFloor(transform.position);Change("IntroEmerge",.8f,"emerge");SyncBurrowBody();}else Change("Recover",.8f);}
            if(Session!=null && Session.CurrentHealth<=0){Body.linearVelocity=Vector2.zero;return;}
            if((phasePending||PhaseTwo)&&!phasePresented){
                AttackEpoch++;StopAllCoroutines();phasePresented=true;phasePending=false;Cleanup();Health.Invincible=true;
                Body.gravityScale=gravity;Body.linearVelocity=Vector2.zero;fanReady=comboReady=antiAirReady=0;
                Change("PhaseChange",1.8f,"phasechange");return;
            }
            if(State=="IntroEmerge"){MoveHorizontally(0);if(Time.time>=deadline)Change("Recover",1);return;}
            if(State=="PhaseChange"){MoveHorizontally(0);if(Time.time>=deadline){Health.Invincible=false;Change("Recover",.5f);}return;}
            if(State=="Reposition"){if(Time.time>=deadline||!CanReposition(FacingSign))Change("Recover",.25f);else MoveHorizontally(FacingSign*profile.speed);return;}
            if(State=="Burrow")
            {
                var destination=SafeFloor(new Vector2(TargetTransform.position.x,transform.position.y));
                transform.position=Vector2.MoveTowards(transform.position,destination,profile.speed*Time.deltaTime);
                FaceDialogueTarget(TargetTransform);
                if(Time.time>=deadline){LockedTarget=SafeFloor(new Vector2(TargetTransform.position.x,transform.position.y));transform.position=LockedTarget;Change("EmergeWarning",profile.emergeSeconds,"emerge");}
                return;
            }
            if(State=="Dig")
            {MoveHorizontally(0);if(Time.time>=deadline){Change("Burrow",profile.burrowTrackSeconds);Body.linearVelocity=Vector2.zero;Body.simulated=false;bodyCollider.enabled=false;Health.Invincible=true;if(visual!=null)visual.enabled=false;}return;}
            if(State=="EmergeWarning")
            {
                SyncBurrowBody();
                if(Time.time>=deadline){transform.position=LockedTarget;Body.simulated=true;bodyCollider.enabled=true;Health.Invincible=false;if(visual!=null)visual.enabled=true;Cleanup();Strike(150,WeaponReach,1);Change("Recover",profile.recoverySeconds);Pose("attack",.36f,.25f,.999f);}
                return;
            }
            if(profile.kind==BossKind.Crystal){TickFlight();return;}
            if(State=="Windup") {MoveHorizontally(0);if(Time.time>=deadline)ExecuteAttack();return;}
            if(State=="Dash" || State=="Dive")
            {
                // Nero rushes into melee range with his sword held behind him.
                // That travel pose is not a forward sword hit or body-contact attack.
                if(profile.kind==BossKind.Nero&&GetTargetDelta().x*FacingSign<=WeaponReach+.15f){Body.linearVelocity=Vector2.zero;deadline=Time.time;}
                if(profile.kind!=BossKind.Nero&&!hit && GetTargetDelta().magnitude<WeaponReach+.2f){if(State=="Dive")StrikeDirected(130,profile.meleeRange,1,-55);else Strike(100,WeaponReach,1);hit=true;}
                if(Time.time>=deadline){Body.gravityScale=gravity;MoveHorizontally(0);if(profile.kind==BossKind.Nero&&Time.time>=comboReady){StartCombo();}else Change("Recover",.7f);}
                return;
            }
            if(State=="Leap")
            {
                if(Time.time>=deadline){
                    FaceDialogueTarget(TargetTransform);
                    StrikeDirected(110,WeaponReach,1,-50);Change("AirRecover",.5f);Pose("airdown",.5f,.25f,.999f);
                }return;
            }
            if(State=="AirSides"){
                if(Time.time>=nextShot&&airStep<2){
                    int sign=airStep==0?FacingSign:-FacingSign;
                    CombatAudio.Boss(gameObject,"swing");
                    BossStrikeEffect.Begin(this,TargetTransform,100,WeaponReach,1,sign);
                    GetComponent<CharacterPresentation>()?.Face(sign);FacingSign=sign;
                    Pose("airside",.36f,.25f,.999f);airStep++;nextShot=Time.time+.38f;
                }
                if(Time.time>=deadline)Change("AirRecover",.45f);return;
            }
            if(State=="AirRecover"){if(Time.time>=deadline)Change("Recover",.45f);return;}
            if(State=="QuickFollow") {
                MoveHorizontally(0);
                if(Time.time>=deadline){FacingSign=-FacingSign;Strike(270,WeaponReach,1);Change("Recover",profile.recoverySeconds);Pose("quick",.4f,.25f,.999f);}
                return;
            }
            if(State=="Combo")
            {
                MoveHorizontally(0);
                if(Time.time>=nextShot&&comboStep<3){
                    if(comboStep==0){Strike(150,WeaponReach,PhaseTwo?2:1);Pose("attack",.48f,.25f,.999f);}
                    else {string action=comboStep==1?"slamright":"slamleft";Pose(action,.58f,0,.999f);StartCoroutine(GroundSlam(comboStep==1?FacingSign:-FacingSign));}
                    comboStep++;nextShot=stateStarted+(comboStep==1?.52f:1.2f);
                }
                if(Time.time>=deadline)Change("Recover",1);return;
            }
            if(State=="Recover" && Time.time<deadline){if(Time.time-stateStarted>.45f)FaceDialogueTarget(TargetTransform);MoveHorizontally(0);return;}
            FaceTarget(GetTargetDelta().x);GetComponent<CharacterPresentation>()?.Face(FacingSign);
            Vector2 delta=GetTargetDelta();float distance=delta.magnitude;
            if(Time.time<decisionReady){MoveHorizontally(0);return;}
            switch(profile.kind)
            {
                case BossKind.Burrow:
                    Change("Dig",.6f,"burrow");break;
                case BossKind.Shockwave:
                    if(delta.y>.9f&&Mathf.Abs(delta.x)<WeaponReach){
                        if(distance<=WeaponReach+1&&Time.time>=antiAirReady)Windup("AntiAir",.7f);
                        else Reposition();
                    }
                    else if(distance>WeaponReach+.25f&&distance<=2.5f*profile.tileSize+1.2f&&Mathf.Abs(delta.y)<1.8f&&Time.time>=waveReady&&attack!="Wave")Windup("Wave",profile.waveWindup);
                    else if(distance<=WeaponReach+.55f){
                        var targetBody=TargetTransform.GetComponent<Rigidbody2D>();
                        bool quick=attack=="Heavy"||(attack!="Quick"&&targetBody!=null&&targetBody.linearVelocity.magnitude>3);
                        Windup(quick?"Quick":"Heavy",quick?profile.quickWindup:profile.heavyWindup);
                    }
                    else MoveHorizontally(FacingSign*profile.speed);
                    break;
                case BossKind.Aerial:
                    // Choose an attack that can reach the target; avoid repeating a failed charge.
                    if(delta.y>1&&distance<=WeaponReach+2&&!(attack=="Rising"&&repeatedAttacks>=2))Windup("Rising",.5f);
                    else if(distance<=WeaponReach+1.2f&&attack!="AirSides")Windup("AirSides",.5f);
                    else if(Mathf.Abs(delta.y)>2||attack=="Dash"||distance<6)Windup(attack=="Leap"?"AirSides":"Leap",.65f);
                    else Windup("Dash",.5f);break;
                case BossKind.Nero:
                    if((distance>7||PhaseTwo&&attack!="Fan"||delta.y>WeaponReach)&&Time.time>=fanReady)Windup("Fan",profile.fanWindup);
                    else if(delta.y>.9f&&Mathf.Abs(delta.x)<WeaponReach){if(distance<=WeaponReach+1&&Time.time>=antiAirReady)Windup("AntiAir",.65f);else Reposition();}
                    else if(distance<WeaponReach+.65f && Time.time>=comboReady)Windup("Combo",.7f);
                    else if(distance>=WeaponReach+.65f&&Mathf.Abs(delta.y)<3&&!(attack=="Dash"&&repeatedAttacks>=2))Windup("Dash",profile.dashWindup);
                    else if(distance>WeaponReach+.65f)MoveHorizontally(FacingSign*profile.speed);else Reposition();break;
            }
        }
        void Windup(string name,float seconds)
        {
            repeatedAttacks=attack==name?repeatedAttacks+1:1;
            AttackEpoch++;attack=name;LockedTarget=TargetTransform.position;dashDirection=(LockedTarget-(Vector2)transform.position).normalized;
            Change("Windup",seconds);
            CombatAudio.Boss(gameObject,"windup",.65f);
            Pose(AttackAnimation(name),seconds,0,.24f);

        }
        void ExecuteAttack()
        {
            Cleanup();GetComponent<CharacterPresentation>()?.Face(FacingSign);
            switch(attack)
            {
                case "AntiAir":antiAirReady=Time.time+2.4f;StrikeDirected(110,WeaponReach,PhaseTwo?2:1,90);Change("Recover",.9f);Pose("antiair",.4f,.25f,.999f);break;
                case "Wave":waveReady=Time.time+3;StartCoroutine(ReleaseWave(Vector2.right*FacingSign));Change("Recover",1);Pose("shockwave",.36f,.25f,.999f);break;
                case "Heavy":Strike(150,WeaponReach,2);Change("Recover",1);Pose("heavy",.38f,.25f,.999f);break;
                case "Quick":Strike(270,WeaponReach,1);Change("QuickFollow",.4f);Pose("quick",.4f,.25f,.999f);break;
                case "Rising":StrikeDirected(110,WeaponReach,1,55);Change("Recover",.8f);Pose("rising",.4f,.25f,.999f);break;
                case "Leap":Body.linearVelocity=new Vector2(Mathf.Clamp(GetTargetDelta().x*1.2f,-profile.speed,profile.speed),10);Change("Leap",.42f);Pose("airdown",.42f,0,.24f);break;
                case "AirSides":Body.linearVelocity=new Vector2(FacingSign*2,9);airStep=0;nextShot=Time.time+.18f;Change("AirSides",.95f);Pose("airside",.18f,0,.24f);break;
                case "Dash":Body.gravityScale=0;Body.linearVelocity=new Vector2(FacingSign,profile.kind==BossKind.Aerial?dashDirection.y*.5f:0)*16*(PhaseTwo?profile.phaseTwoSpeedMultiplier:1);hit=false;Change("Dash",.6f);Pose("special",.6f,.25f,.74f);RelicFX.Dash(gameObject,.6f);break;
                case "Fan":
                    int count=PhaseTwo?11:9;float aim=Mathf.Atan2(dashDirection.y,dashDirection.x)*Mathf.Rad2Deg;
                    for(int i=0;i<count;i++){float a=(aim-75+i*150f/(count-1))*Mathf.Deg2Rad;StartCoroutine(ReleaseWave(new Vector2(Mathf.Cos(a),Mathf.Sin(a))));}
                    fanReady=Time.time+profile.fanCooldown;Change("Recover",1);Pose("cast",.35f,.25f,.999f);break;
                case "Combo":StartCombo();break;
            }
        }
        void StartCombo(){comboStep=0;comboReady=Time.time+profile.comboCooldown;nextShot=Time.time;Change("Combo",2,"attack");}
        System.Collections.IEnumerator GroundSlam(int side){
            int epoch=AttackEpoch;yield return new WaitForSeconds(.30f);
            if(!AttackValid(epoch))yield break;
            Vector2 foot=(Vector2)bodyCollider.bounds.center+new Vector2(side*2.4f,-bodyCollider.bounds.extents.y+.12f);
            if(!CombatGeometry.Clear(AttackCenter,foot+Vector2.up*.15f))yield break;
            BossGroundHazard.Create(this,foot,false,0,PhaseTwo?2:1);
            CombatAudio.Boss(gameObject,"slam",.9f);
        }
        void TickFlight()
        {
            FaceDialogueTarget(TargetTransform);Body.gravityScale=0;if(flightRoute!=null)flightRoute.Move(profile.speed,TargetTransform);
            if(State=="FlightAim")
            {if(Time.time>=deadline){StartCoroutine(ReleaseWave((LockedTarget-(Vector2)transform.position).normalized));Cleanup();Change("Flight",.12f);Pose("cast",.3f,.25f,.999f);flightShots++;if(flightShots%3==0&&Time.time>=nextTrap){nextTrap=Time.time+4;PlaceTrap();}}}
            else if(Time.time>=deadline){LockedTarget=TargetTransform.position;Change("FlightAim",profile.flyWindup);Pose("cast",profile.flyWindup,0,.24f);}
        }
        void PlaceTrap(){
            foreach(var h in Physics2D.RaycastAll(LockedTarget+Vector2.up*.5f,Vector2.down,8)){
                if(!TerrainMotion.Solid(h.collider)||!InArena(h.point+Vector2.up*.1f))continue;
                if(!CombatGeometry.Clear(LockedTarget,h.point+Vector2.up*.08f))continue;
                BossGroundHazard.Create(this,h.point,true,.9f,1);break;
            }
        }
        void CacheFloors()
        {
            var tm=GameObject.Find("GroundTilemap")?.GetComponent<Tilemap>();
            if(tm==null)return;
            var box=bodyCollider as BoxCollider2D;
            float halfWidth=box!=null?box.size.x*Mathf.Abs(transform.lossyScale.x)*.5f:.6f;
            float height=box!=null?box.size.y*Mathf.Abs(transform.lossyScale.y):2;
            var lo=tm.WorldToCell(arenaCenter-arenaSize*.5f-Vector2.one);
            var hi=tm.WorldToCell(arenaCenter+arenaSize*.5f+Vector2.one);
            for(int y=lo.y;y<=hi.y;y++)for(int x=lo.x;x<=hi.x;x++){
                var cell=new Vector3Int(x,y,0);if(!tm.HasTile(cell)||tm.HasTile(cell+Vector3Int.up))continue;
                Vector2 surface=tm.GetCellCenterWorld(cell);surface.y+=tm.layoutGrid.cellSize.y*.5f;
                Vector2 root=surface-Vector2.up*FootOffset+Vector2.up*.01f;
                if(!InArena(root))continue;
                var min=tm.WorldToCell(surface+new Vector2(-halfWidth+.03f,.03f));
                var max=tm.WorldToCell(surface+new Vector2(halfWidth-.03f,height-.03f));bool clear=true;
                for(int yy=min.y;yy<=max.y;yy++)for(int xx=min.x;xx<=max.x;xx++)if(tm.HasTile(new Vector3Int(xx,yy,0)))clear=false;
                if(clear)standingPoints.Add(root);
            }
        }
        Vector2 SafeFloor(Vector2 preferred)
        {
            Vector2 best=spawn;float score=float.MaxValue;
            foreach(var p in standingPoints){float s=(p-preferred).sqrMagnitude;if(s<score){score=s;best=p;}}
            // Interpolate only along a verified continuous ledge; never snap to tile centres.
            float delta=preferred.x-best.x;
            if(Mathf.Abs(delta)<1){foreach(var p in standingPoints)if(Mathf.Abs(p.y-best.y)<.01f&&Mathf.Abs(p.x-best.x-Mathf.Sign(delta))<.01f){best.x=preferred.x;break;}}
            return best;
        }
        void Strike(float angle,float range,int damage) {
            CombatAudio.Boss(gameObject,attack=="Heavy"?"heavy":profile.kind==BossKind.Shockwave&&attack=="Quick"?"quick":"swing");
            BossStrikeEffect.Begin(this,TargetTransform,angle,range,damage,FacingSign);
        }
        void StrikeDirected(float angle,float range,int damage,float axis){CombatAudio.Boss(gameObject,profile.kind==BossKind.Aerial?(axis>0?"rising":"downcut"):"swing");BossStrikeEffect.Begin(this,TargetTransform,angle,range,damage,FacingSign,axis);}
        System.Collections.IEnumerator ReleaseWave(Vector2 dir){
            int epoch=AttackEpoch;
            yield return new WaitForSeconds(.10f);
            if(AttackValid(epoch))Wave(dir);
        }
        void Wave(Vector2 dir){
            dir.Normalize();Vector2 origin;
            if(!CombatMuzzle.TryResolve(gameObject,FacingSign,false,out origin))return;
            CombatAudio.Boss(gameObject,"cast",.8f);
            var projectile=BableCombatFX.Projectile(gameObject,origin,dir,PhaseTwo?12:9,1,Color.white,profile.kind==BossKind.Shockwave?1.65f:.9f,profile.kind==BossKind.Crystal?"CrystalBolt":profile.kind==BossKind.Nero?"ArcaneOrb":"BossWave41");
            if(profile.kind==BossKind.Shockwave){projectile.maxDistance=2.5f*profile.tileSize;projectile.effectArt="BossWave41";}
        }
        void Change(string next,float seconds,string animation="idle"){State=next;stateStarted=Time.time;deadline=Time.time+seconds;
            if(next=="Recover")decisionReady=deadline+.12f;
            if(next=="Dig")CombatAudio.Play("burrow",transform.position,.6f);
            else if(next=="EmergeWarning"||next=="IntroEmerge")CombatAudio.Play("emerge",transform.position,.8f);
            else if(next=="PhaseChange")CombatAudio.Play("phase",transform.position,.9f);
            else if(next=="Dash"||next=="Dive")CombatAudio.Boss(gameObject,"dash");
            else if(next=="FlightAim")CombatAudio.Boss(gameObject,"windup",.6f);
            else if(next=="Leap"||next=="AirSides")CombatAudio.Boss(gameObject,profile.kind==BossKind.Aerial?"leap":"dash",.6f);GetComponent<CharacterPresentation>()?.Act(animation,seconds);}
        void Reposition(){
            // Step out from underneath a camping player. No collision damage and
            // no instant retaliation; the next attack still has its full windup.
            int away=GetTargetDelta().x>=0?-1:1;
            if(Mathf.Abs(transform.position.x+away*2-arenaCenter.x)>arenaSize.x*.5f-1.5f)away=-away;
            FacingSign=away;Change("Reposition",.4f,"run");
        }
        bool CanReposition(int direction){
            Vector2 next=(Vector2)bodyCollider.bounds.center+Vector2.right*direction*(bodyCollider.bounds.extents.x+.4f);
            if(!CombatGeometry.Clear(AttackCenter,next))return false;
            next.y=bodyCollider.bounds.min.y+.15f;
            foreach(var h in Physics2D.RaycastAll(next,Vector2.down,.9f))if(TerrainMotion.Solid(h.collider))return true;
            return false;
        }
        public override bool TryReceiveDamage(DamageInfo d){
            if(!BurrowVulnerable||Health.Invincible||Health.CurrentHealth<=0||phasePending)return false;
            int before=Health.CurrentHealth;
            base.TryReceiveDamage(d);
            if(Health.CurrentHealth<before&&profile.kind==BossKind.Nero&&!phasePresented&&Health.CurrentHealth<=profile.health*.5f)phasePending=true;
            return Health.CurrentHealth<before;
        }
        public int LimitDamageForPhase(int amount){
            if(profile==null||profile.kind!=BossKind.Nero)return amount;
            if(State=="PhaseChange"||phasePending)return 0;
            if(phasePresented)return amount;
            // Applied at the health boundary, including reflection/projectiles, so
            // a large hit cannot jump from phase one straight to the death event.
            int half=Mathf.CeilToInt(profile.health*.5f);
            return Health.CurrentHealth>half?Mathf.Min(amount,Health.CurrentHealth-half):0;
        }
        protected override void OnCollisionStay2D(Collision2D c) { /* Damage only during authored attack windows. */ }
        protected override void OnDrawGizmosSelected(){Gizmos.color=Color.cyan;Gizmos.DrawWireCube(arenaCenter,arenaSize);}
    }
}
