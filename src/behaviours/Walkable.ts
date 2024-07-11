import { Behaviour, number, RigidBody, GameObject, PhysicsSystem, Transform, Collider, getGameObjectById } from "../../lib/mygameengine";
import { MainRolePrefabBinding } from "../bindings/MainRolePrefabBinding"; 
import { b2Vec2 } from "@flyover/box2d"; 
import { State, GroundState, AirState, WallState, CornerState, DoubleJumpedState } from "../behaviours/State";
import { Camera } from "../../lib/mygameengine"; 

export class Walkable extends Behaviour {
    @number()
    speed = 1;

    @number()
    jumpForce = 5;

    @number()
    wallJumpForce = 5;

    @number()
    wallSlideSpeed = 2; // 角色在墙上的滑动速度

    public isGrounded = false;
    public isOnWall = false;
    public airJumped = false; // 跟踪空中跳跃状态
    public mainRoleBinding: MainRolePrefabBinding | null = null;
    public lastAction: string = 'right';
    public coyoteTime = 0.1; // 小跳跃时间
    public coyoteTimer = 0;
    public isMoving = false; // 跟踪是否正在移动
    private currentState: State;
    public initialJump = true; // 跟踪是否是初始跳跃
    private cameraTransform: Transform | null = null;
    private playerTransform: Transform | null = null;

    constructor() {
        super();
        this.currentState = new GroundState(this); // 初始状态为地面状态
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
    }

    handleKeyDown(event: KeyboardEvent) {
        this.currentState.handleInput(event);
    }

    handleKeyUp(event: KeyboardEvent) {
        this.currentState.handleKeyUp(event);
    }

    handleCollisionEnter(other: RigidBody, otherCollider: Collider, self: RigidBody, selfCollider: Collider) {
        const otherGameObject = other.gameObject;
        if (otherGameObject.tag === 'ground') {
            this.isGrounded = true;
            this.airJumped = false; // 重置空中跳跃状态
        }
        if (otherGameObject.tag === 'wall') {
            this.isOnWall = true;
        }

        // 检查是否需要切换到 CornerState
        if (this.isGrounded && this.isOnWall) {
            this.changeState(new CornerState(this));
        }
    }

    handleCollisionExit(other: RigidBody, otherCollider: Collider, self: RigidBody, selfCollider: Collider) {
        const otherGameObject = other.gameObject;
        if (otherGameObject.tag === 'ground') {
            this.isGrounded = false;
        }
        if (otherGameObject.tag === 'wall') {
            this.isOnWall = false;
        }
    }

    jump(rigid: RigidBody, multiplier: number = 1) {
        rigid.b2RigidBody.SetLinearVelocity(new b2Vec2(rigid.b2RigidBody.GetLinearVelocity().x, this.jumpForce * multiplier));
    }

    wallJump(rigid: RigidBody) {
      const direction = this.lastAction === 'left' ? 1 : -1;
      const horizontalSpeed = this.wallJumpForce * direction;
      const verticalSpeed = this.jumpForce * 0.7;
      rigid.b2RigidBody.SetLinearVelocity(new b2Vec2(horizontalSpeed, verticalSpeed));
      console.log(`wall jump: (${horizontalSpeed}, ${verticalSpeed})`);
  }

    onTick(duringTime: number) {
        this.currentState.update(duringTime);

        const rigid = this.gameObject.getBehaviour(RigidBody);
        let velocity = rigid.b2RigidBody.GetLinearVelocity();

        if (this.isMoving) {
            if (this.lastAction === 'left') {
                velocity = new b2Vec2(-this.speed, velocity.y);
            } else if (this.lastAction === 'right') {
                velocity = new b2Vec2(this.speed, velocity.y);
            }
        } else {
            // 手动减速
            velocity = new b2Vec2(velocity.x * 0.5, velocity.y); // 调整减速系数以控制减速速度
        }

        rigid.b2RigidBody.SetLinearVelocity(velocity);

        // 更新相机位置
        if (this.cameraTransform && this.playerTransform) {
            this.cameraTransform.x = this.playerTransform.x;
            this.cameraTransform.y = this.playerTransform.y;
        }
    }

    onDestroy() {
        document.removeEventListener('keydown', this.handleKeyDown.bind(this));
        document.removeEventListener('keyup', this.handleKeyUp.bind(this));
    }
}
