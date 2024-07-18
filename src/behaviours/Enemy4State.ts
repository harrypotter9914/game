import { Enemy4 } from "./Enemy4";
import { Collider, RigidBody, Transform } from "../../lib/mygameengine";
import { b2Vec2 } from "@flyover/box2d";
import { Enemy4HealthStateMachine } from "./Enemy4HealthStateMachine"; // 引入血量状态机
import { AudioBehaviour, AudioSystem } from "../../lib/mygameengine";

export abstract class Enemy4State {
    protected enemy4: Enemy4;

    constructor(enemy4: Enemy4) {
        this.enemy4 = enemy4;
    }

    abstract enter(): void;
    abstract update(duringTime: number): void;
    abstract exit(): void;

    // 添加抽象方法
    abstract handleCollisionEnter(other: RigidBody, otherCollider: Collider, self: RigidBody, selfCollider: Collider): void;
}


export class HurtState extends Enemy4State {
    private previousState: Enemy4State;
    private hurtDirection: string;
    private damageAudio: AudioBehaviour | null = null;
    private attackAudio: AudioBehaviour | null = null;


    
    constructor(enemy4: Enemy4, previousState: Enemy4State, hurtDirection: string) {
        super(enemy4);
        this.previousState = previousState;
        this.hurtDirection = hurtDirection;
        // 初始化音频行为
        this.damageAudio = new AudioBehaviour();
        this.damageAudio.source = "./assets/audio/21_orc_damage_1.wav"; 
        this.damageAudio.setLoop(false); // 设置不循环播放
        this.damageAudio.setVolume(1);
    }

    enter() {
        console.log("Entering Hurt State");

        // 播放受击动画
        if (this.hurtDirection === 'left') {
            this.enemy4.enemy4Binding!.action = 'rightsufferattack';
        } else {
            this.enemy4.enemy4Binding!.action = 'leftsufferattack';
        }

        if (this.damageAudio) {
            this.damageAudio.play();
        }

        // 向攻击方向击飞一段距离
        const rigidBody = this.enemy4.gameObject.getBehaviour(RigidBody);
        const force = this.hurtDirection === 'left' ? new b2Vec2(-20, 0) : new b2Vec2(20, 0);
        rigidBody.b2RigidBody.ApplyLinearImpulse(force, rigidBody.b2RigidBody.GetWorldCenter(), true);

        // 减少敌人的血量
        const healthStateMachine = this.enemy4.gameObject.getBehaviour(Enemy4HealthStateMachine);
        healthStateMachine.decreaseHealth(1);

    }

    update(duringTime: number) {
        // 检测动画是否完成，可以用一个计时器或检查动画状态
        setTimeout(() => {
            this.enemy4.changeState(this.previousState);
        }, 200); // 受击状态持续1秒，之后恢复之前的状态
    }

    exit() {
        console.log("Exiting Hurt State");
    }

    handleCollisionEnter(other: RigidBody, otherCollider: Collider, self: RigidBody, selfCollider: Collider) {
        // 受击状态不处理碰撞
    }
}


export class PatrolState extends Enemy4State {
    private patrolDirection: number = 1; // 1 为右，-1 为左
    
    enter() {
        console.log("Entering Patrol State");
        this.enemy4.gameObject.getBehaviour(RigidBody).b2RigidBody.SetAwake(true); // 确保物理引擎对该对象进行更新
    }

    update(duringTime: number) {
        const transform = this.enemy4.gameObject.getBehaviour(Transform);
        const rigidBody = this.enemy4.gameObject.getBehaviour(RigidBody);

        // 移动并检测碰撞
        const velocity = new b2Vec2(this.patrolDirection * this.enemy4.patrolSpeed, rigidBody.b2RigidBody.GetLinearVelocity().y);
        rigidBody.b2RigidBody.SetLinearVelocity(velocity);
        
        if (this.enemy4.enemy4Binding) {
            if (this.patrolDirection > 0) {
                this.enemy4.enemy4Binding.action = 'rightpatrol';
            } else {
                this.enemy4.enemy4Binding.action = 'leftpatrol';
            }
        } else {
            console.error("Enemy4PrefabBinding is not initialized.");
        }

        // 检测是否进入战斗状态
        const playerTransform = this.enemy4.getPlayerTransform();
        if (playerTransform && this.isPlayerInRange(playerTransform)) {
            this.enemy4.changeState(new ChaseState(this.enemy4));
        }

    }

    exit() {
        console.log("Exiting Patrol State");
    }

    handleCollisionEnter(other: RigidBody, otherCollider: Collider, self: RigidBody, selfCollider: Collider) {
        if (selfCollider.tag === 'enemy4body' && otherCollider.tag === 'block') {
            this.patrolDirection *= -1; // 碰到墙壁时改变方向
            console.log("碰到墙壁，改变方向");
        }

        // 检查是否被攻击
        if (selfCollider.tag === 'enemy4body' && otherCollider.tag === 'MaoQi') {
            const attackDirection = this.enemy4.getPlayerLastAttackDirection(); // 假设有一个方法获取玩家上次攻击的方向
            this.enemy4.changeState(new HurtState(this.enemy4, this, attackDirection));
        }
    }
    
    private isPlayerInRange(playerTransform: Transform): boolean {
        const distance = Math.sqrt(Math.pow(playerTransform.x - this.enemy4.gameObject.getBehaviour(Transform).x, 2) + 
                                   Math.pow(playerTransform.y - this.enemy4.gameObject.getBehaviour(Transform).y, 2));
        return distance < 600; // 假设检测范围为 200
    }
}


export class ChaseState extends Enemy4State {
    enter() {
        console.log("Entering Chase State");
    }

    update(duringTime: number) {
        const transform = this.enemy4.gameObject.getBehaviour(Transform);
        const rigidBody = this.enemy4.gameObject.getBehaviour(RigidBody);
        const playerTransform = this.enemy4.getPlayerTransform();

        if (playerTransform) {
            const direction = Math.atan2(playerTransform.y - transform.y, playerTransform.x - transform.x);
            let moveX = Math.cos(direction) * this.enemy4.chaseSpeed;
            let moveY = Math.sin(direction) * this.enemy4.chaseSpeed;

            // 只在X轴上移动
            rigidBody.b2RigidBody.SetLinearVelocity(new b2Vec2(moveX, rigidBody.b2RigidBody.GetLinearVelocity().y));

            if (playerTransform.x > transform.x) {
                this.enemy4.enemy4Binding!.action = 'rightrun';
            } else {
                this.enemy4.enemy4Binding!.action = 'leftrun';
            }
        }

        // 检测是否进入攻击状态
        if (this.isPlayerClose(playerTransform)) {
            this.enemy4.changeState(new AttackState(this.enemy4));
        }

        // 检测是否玩家离开追逐范围
        if (!this.isPlayerInRange(playerTransform)) {
            this.enemy4.changeState(new PatrolState(this.enemy4));
        }

    }

    exit() {
        console.log("Exiting Chase State");
    }

    handleCollisionEnter(other: RigidBody, otherCollider: Collider, self: RigidBody, selfCollider: Collider) {
        // 检查是否被攻击
        if (selfCollider.tag === 'enemy4body' && otherCollider.tag === 'MaoQi') {
            const attackDirection = this.enemy4.getPlayerLastAttackDirection(); // 假设有一个方法获取玩家上次攻击的方向
            this.enemy4.changeState(new HurtState(this.enemy4, this, attackDirection));
        }
    }

    private isPlayerClose(playerTransform: Transform | null): boolean {
        if (!playerTransform) return false;
        const distance = Math.sqrt(Math.pow(playerTransform.x - this.enemy4.gameObject.getBehaviour(Transform).x, 2) + 
                                   Math.pow(playerTransform.y - this.enemy4.gameObject.getBehaviour(Transform).y, 2));
        return distance < 100; // 假设攻击范围为 10
    }

    private isPlayerInRange(playerTransform: Transform | null): boolean {
        if (!playerTransform) return false;
        const distance = Math.sqrt(Math.pow(playerTransform.x - this.enemy4.gameObject.getBehaviour(Transform).x, 2) + 
                                   Math.pow(playerTransform.y - this.enemy4.gameObject.getBehaviour(Transform).y, 2));
        return distance < 600; // 假设追逐范围为 100
    }

}


export class AttackState extends Enemy4State {

    constructor(enemy4: Enemy4) {
        super(enemy4);
    }

    enter() {
        console.log("Entering Attack State");
    }

    update(duringTime: number) {
        const transform = this.enemy4.gameObject.getBehaviour(Transform);
        const playerTransform = this.enemy4.getPlayerTransform();

        if (playerTransform) {
            // 根据小怪和玩家的位置确定攻击方向
            if (playerTransform.x < transform.x) {
                this.enemy4.enemy4Binding!.action = 'leftattack';
            } else {
                this.enemy4.enemy4Binding!.action = 'rightattack';
            }

            // 简单的攻击逻辑
            console.log("Attacking player");

            // 检测是否需要返回巡逻或继续追逐
            if (!this.isPlayerClose(playerTransform)) {
                this.enemy4.changeState(new ChaseState(this.enemy4));
            }
        }
    }

    exit() {
        console.log("Exiting Attack State");
    }

    handleCollisionEnter(other: RigidBody, otherCollider: Collider, self: RigidBody, selfCollider: Collider) {
        // 检查是否被攻击
        if (selfCollider.tag === 'enemy4body' && otherCollider.tag === 'MaoQi') {
            const attackDirection = this.enemy4.getPlayerLastAttackDirection(); // 假设有一个方法获取玩家上次攻击的方向
            this.enemy4.changeState(new HurtState(this.enemy4, this, attackDirection));
        }
    }

    private isPlayerClose(playerTransform: Transform | null): boolean {
        if (!playerTransform) return false;
        const distance = Math.sqrt(Math.pow(playerTransform.x - this.enemy4.gameObject.getBehaviour(Transform).x, 2) + 
                                   Math.pow(playerTransform.y - this.enemy4.gameObject.getBehaviour(Transform).y, 2));
        return distance < 100; // 假设攻击范围为 10
    }
}