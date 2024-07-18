import { Behaviour, number, RigidBody, GameObject, PhysicsSystem, Transform, Collider, getGameObjectById } from "../../lib/mygameengine";
import { MainRolePrefabBinding } from "../bindings/MainRolePrefabBinding"; 
import { b2Vec2 } from "@flyover/box2d"; 
import { State, GroundState, AirState, WallState, CornerState, DoubleJumpedState, HurtState } from "../behaviours/State";
import { Attackable } from "./Attackable";
import { EnemyPrefabBinding } from "../bindings/EnemyPrefabBinding";
import { Enemy } from "./Enemy";
import { HealthStateMachine } from "./HealthState";
import { ManaStateMachine } from "./ManaState";
import { AudioBehaviour, AudioSystem } from "../../lib/mygameengine";
import { StarCollisionHandler } from "./StarCollisionHandler";

export class Walkable extends Behaviour {
    @number()
    speed = 1;

    @number()
    jumpForce = 5;

    @number()
    wallJumpForce = 5;

    @number()
    wallSlideSpeed = 2; // 角色在墙上的滑动速度

    private attackable: Attackable;
    public isGrounded = false;
    public isOnWall = false;
    public airJumped = false; // 跟踪空中跳跃状态
    public mainRoleBinding: MainRolePrefabBinding | null = null;
    public lastAction: string = 'rightidle';
    public coyoteTime = 0.1; // 小跳跃时间
    public coyoteTimer = 0;
    public isMoving = false; // 跟踪是否正在移动
    public currentState: State;
    public initialJump = true; // 跟踪是否是初始跳跃
    private cameraTransform: Transform | null = null;
    private playerTransform: Transform | null = null;
    private cameraZoomedOut = false; // 跟踪摄像机是否缩放
    public leftArrowPressed: boolean = false;
    public rightArrowPressed: boolean = false;
    public upArrowPressed: boolean = false;
    public downArrowPressed: boolean = false;
    public lastEnemyAction: string = ''; // 跟踪最后一次敌人的动作
    public currentAction: string = 'idle'; // 跟踪当前动作
    private hurtCooldown: boolean = false; // 跟踪受击冷却状态
    private aKeyPressedStartTime: number = 0;
    private isAKeyPressed: boolean = false;
    private jumpAudio: AudioBehaviour | null = null;
    private bgmusic1: AudioBehaviour | null = null;
    public startag: string = '';
    public offsetX: number = 550; // X轴偏移量
    public offsetY: number = 150; // Y轴偏移量

    private groundContactCount = 0; // 跟踪feet接触的数量
    private wallContactCount = 0; // 跟踪body接触的数量

    constructor() {
        super();
        this.currentState = new GroundState(this); // 初始状态为地面状态

        // 初始化音频行为
        this.jumpAudio = new AudioBehaviour();
        this.jumpAudio.source = "./assets/audio/12_human_jump_1.wav"; // 设置跳跃音频的路径
        this.jumpAudio.setLoop(false); // 设置不循环播放
        this.jumpAudio.setVolume(1);

        // 初始化音频行为
        this.bgmusic1 = new AudioBehaviour();
        this.bgmusic1.source = "./assets/audio/tower.wav"; // 设置跳跃音频的路径
        this.bgmusic1.setLoop(true); // 设置循环播放
        this.bgmusic1.setVolume(0.5); // 设置音量，范围是 0.0 到 1.0
    }

    changeState(newState: State) {
        this.currentState.exit();
        this.currentState = newState;
        this.currentState.enter();
    }

    onStart() {
        this.mainRoleBinding = this.gameObject.getBehaviour(MainRolePrefabBinding);
        if (this.mainRoleBinding) {
            this.mainRoleBinding.action = this.lastAction;
        } else {
            console.warn('MainRolePrefabBinding not found on the game object.');
        }
        document.addEventListener('keydown', this.handleKeyDown.bind(this));
        document.addEventListener('keyup', this.handleKeyUp.bind(this));

        const rigidBody = this.gameObject.getBehaviour(RigidBody);
        if (rigidBody) {
            rigidBody.onCollisionEnter = this.handleCollisionEnter.bind(this);
            rigidBody.onCollisionExit = this.handleCollisionExit.bind(this);
        }

         // 获取相机对象
        const cameraObject = getGameObjectById('camera');
        if (cameraObject) {
            this.cameraTransform = cameraObject.getBehaviour(Transform);
        }

         // 获取玩家对象的 Transform
         const playerObject = getGameObjectById('mainRole');
         if (playerObject) {
             this.playerTransform = playerObject.getBehaviour(Transform);
         }


         // 添加 Attackable 行为
        const attackable = new Attackable(this);
        this.gameObject.addBehaviour(attackable);
        this.attackable = attackable;

        if (this.gameObject.active === true) {
            this.bgmusic1.play();
            this.cameraTransform.scaleX = 0.6;
            this.cameraTransform.scaleY = 0.6;
        } 


        const player = getGameObjectById('mainRole');
        if (player) {
            player.addBehaviour(new HealthStateMachine());
            player.addBehaviour(new ManaStateMachine());
            player.addBehaviour(new StarCollisionHandler());
            console.log("HealthStateMachine and ManaStateMachine added to player");
        } else {
            console.error("Player GameObject not found");
        }

    }


    getLastAttackDirection(): string {
        return this.attackable.lastAttackDirection;
    }

    handleKeyDown(event: KeyboardEvent) {
        this.currentState.handleInput(event);

        if (event.key === 'a' && !this.isAKeyPressed) {
            this.aKeyPressedStartTime = Date.now();
            this.isAKeyPressed = true;
            if (this.lastAction.includes('left')) {
                this.mainRoleBinding.action = 'leftheal';
                } else if (this.lastAction.includes('right')) {
                this.mainRoleBinding.action = 'rightheal';
                }
        }
        
        if (event.key === 'z') { // 假设按下 'z' 键缩放摄像机
            this.cameraZoomedOut = true;
        }
    }

    handleKeyUp(event: KeyboardEvent) {
        this.currentState.handleKeyUp(event);

        if (event.key === 'a' && this.isAKeyPressed) {
            const aKeyPressDuration = Date.now() - this.aKeyPressedStartTime;
            if (aKeyPressDuration >= 1500) {
                const manaStateMachine = this.gameObject.getBehaviour(ManaStateMachine);
                if (manaStateMachine) {
                    manaStateMachine.decreaseMana(1);
                }
            }
            this.isAKeyPressed = false;
        }

        if (event.key === 'z') {
            this.cameraZoomedOut = false;
        }
    }

    handleCollisionEnter(other: RigidBody, otherCollider: Collider, self: RigidBody, selfCollider: Collider) {
        if (selfCollider.tag === 'feet' && otherCollider.tag === 'block') {
            this.groundContactCount++;
            if (this.groundContactCount > 0) {
                this.isGrounded = true;
                this.airJumped = false; // 重置空中跳跃状态
            }
        }
        if (selfCollider.tag === 'body' && otherCollider.tag === 'block') {
            this.wallContactCount++;
            if (this.wallContactCount > 0) {
                this.isOnWall = true;
                console.log('on wall');
            }
        }

        if ((selfCollider.tag === 'feet' || selfCollider.tag === 'body') && otherCollider.tag.includes('hoxi')) {
            const starCollisionHandler = this.gameObject.getBehaviour(StarCollisionHandler);
            if (starCollisionHandler) {
                starCollisionHandler.setInteractingStar(other.gameObject); // 设置交互的星星对象
                this.startag = other.gameObject.tag;
            }
        }

        if ((selfCollider.tag === 'body' || selfCollider.tag === 'feet') && (otherCollider.tag === 'enemybody' || otherCollider.tag === 'enemyfeet')) {
            if (!this.hurtCooldown) {
                this.lastEnemyAction = other.gameObject.getBehaviour(EnemyPrefabBinding)?.action || '';
                this.changeState(new HurtState(this));
                this.hurtCooldown = true;
                setTimeout(() => {
                    this.hurtCooldown = false;
                }, 1000); // 1秒冷却时间
            }
        }
    }

    handleCollisionExit(other: RigidBody, otherCollider: Collider, self: RigidBody, selfCollider: Collider) {
        if (selfCollider.tag === 'feet' && otherCollider.tag === 'block') {
            this.groundContactCount--;
            if (this.groundContactCount === 0) {
                this.isGrounded = false;
                console.log('not grounded');
            }
        }
        if (selfCollider.tag === 'body' && otherCollider.tag === 'block') {
            this.wallContactCount--;
            if (this.wallContactCount === 0) {
                this.isOnWall = false;
            }
        }
    }


    jump(rigid: RigidBody, multiplier: number = 1) {
        if (this.jumpAudio) {
            this.jumpAudio.play();
        }
        rigid.b2RigidBody.SetLinearVelocity(new b2Vec2(rigid.b2RigidBody.GetLinearVelocity().x, this.jumpForce * multiplier));
    }

    wallJump(rigid: RigidBody) {
        let direction: number = 0; // 默认为0，如果没有按下方向键则不施加水平力

        if (this.leftArrowPressed) {
            direction = 1;
        } else if (this.rightArrowPressed) {
            direction = -1;
        }

        const horizontalSpeed = this.wallJumpForce * direction;
        const verticalSpeed = this.jumpForce * 0.7;
        if (this.jumpAudio) {
            this.jumpAudio.play();
        }
        rigid.b2RigidBody.SetLinearVelocity(new b2Vec2(horizontalSpeed, verticalSpeed));
        console.log(`wall jump: (${horizontalSpeed}, ${verticalSpeed})`);
      }

    onTick(duringTime: number) {
        this.currentState.update(duringTime);

        if(this.gameObject.active === false) {
            this.bgmusic1.stop();
            this.cameraTransform.scaleX = 1;
            this.cameraTransform.scaleY = 1;
        }

        const rigid = this.gameObject.getBehaviour(RigidBody);
        let velocity = rigid.b2RigidBody.GetLinearVelocity();

        if (this.isMoving) {
            if (this.lastAction === 'leftrun') {
                velocity = new b2Vec2(-this.speed, velocity.y);
            } else if (this.lastAction === 'rightrun') {
                velocity = new b2Vec2(this.speed, velocity.y);
            }
        } else {
            // 手动减速
            velocity = new b2Vec2(velocity.x * 0.5, velocity.y); // 调整减速系数以控制减速速度
        }

        rigid.b2RigidBody.SetLinearVelocity(velocity);

      
        // 直接更新相机位置
        if (this.cameraTransform && this.playerTransform) {
            this.cameraTransform.x = this.playerTransform.x + this.offsetX;
            this.cameraTransform.y = this.playerTransform.y + this.offsetY;
        }
    }

    onDestroy() {
        document.removeEventListener('keydown', this.handleKeyDown.bind(this));
        document.removeEventListener('keyup', this.handleKeyUp.bind(this));
    }

    lerp(start: number, end: number, t: number): number {
        return start + t * (end - start);
    }

}
