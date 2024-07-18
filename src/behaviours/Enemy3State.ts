import { Enemy3 } from "./Enemy3";
import { Collider, RigidBody, Transform } from "../../lib/mygameengine";
import { b2Vec2 } from "@flyover/box2d";
import { Enemy3HealthStateMachine } from "./Enemy3HealthStateMachine"; // 引入血量状态机
import { AudioBehaviour, AudioSystem } from "../../lib/mygameengine";

export abstract class Enemy3State {
    protected enemy3: Enemy3;

    constructor(enemy3: Enemy3) {
        this.enemy3 = enemy3;
    }

    abstract enter(): void;
    abstract update(duringTime: number): void;
    abstract exit(): void;

    // 添加抽象方法
    abstract handleCollisionEnter(other: RigidBody, otherCollider: Collider, self: RigidBody, selfCollider: Collider): void;
}


export class HurtState extends Enemy3State {
    private previousState: Enemy3State;
    private hurtDirection: string;
    private damageAudio: AudioBehaviour | null = null;
    private attackAudio: AudioBehaviour | null = null;


    
    constructor(enemy3: Enemy3, previousState: Enemy3State, hurtDirection: string) {
        super(enemy3);
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
            this.enemy3.enemy3Binding!.action = 'rightsufferattack';
        } else {
            this.enemy3.enemy3Binding!.action = 'leftsufferattack';
        }

        if (this.damageAudio) {
            this.damageAudio.play();
        }

        // 向攻击方向击飞一段距离
        const rigidBody = this.enemy3.gameObject.getBehaviour(RigidBody);
        const force = this.hurtDirection === 'left' ? new b2Vec2(-20, 0) : new b2Vec2(20, 0);
        rigidBody.b2RigidBody.ApplyLinearImpulse(force, rigidBody.b2RigidBody.GetWorldCenter(), true);

        // 减少敌人的血量
        const healthStateMachine = this.enemy3.gameObject.getBehaviour(Enemy3HealthStateMachine);
        healthStateMachine.decreaseHealth(1);

    }

    update(duringTime: number) {
        // 检测动画是否完成，可以用一个计时器或检查动画状态
        setTimeout(() => {
            this.enemy3.changeState(this.previousState);
        }, 200); // 受击状态持续1秒，之后恢复之前的状态
    }

    exit() {
        console.log("Exiting Hurt State");
    }

    handleCollisionEnter(other: RigidBody, otherCollider: Collider, self: RigidBody, selfCollider: Collider) {
        // 受击状态不处理碰撞
    }
}


export class PatrolState extends Enemy3State {
    private patrolDirection: number = 1; // 1 为右，-1 为左
    
    enter() {
        console.log("Entering Patrol State");
        this.enemy3.gameObject.getBehaviour(RigidBody).b2RigidBody.SetAwake(true); // 确保物理引擎对该对象进行更新
    }

    update(duringTime: number) {
        const transform = this.enemy3.gameObject.getBehaviour(Transform);
        const rigidBody = this.enemy3.gameObject.getBehaviour(RigidBody);

        // 移动并检测碰撞
        const velocity = new b2Vec2(this.patrolDirection * this.enemy3.patrolSpeed, rigidBody.b2RigidBody.GetLinearVelocity().y);
        rigidBody.b2RigidBody.SetLinearVelocity(velocity);
        
        if (this.enemy3.enemy3Binding) {
            if (this.patrolDirection > 0) {
                this.enemy3.enemy3Binding.action = 'rightpatrol';
            } else {
                this.enemy3.enemy3Binding.action = 'leftpatrol';
            }
        } else {
            console.error("Enemy3PrefabBinding is not initialized.");
        }

        // 检测是否进入战斗状态
        const playerTransform = this.enemy3.getPlayerTransform();
        if (playerTransform && this.isPlayerInRange(playerTransform)) {
            this.enemy3.changeState(new ChaseState(this.enemy3));
        }

    }

    exit() {
        console.log("Exiting Patrol State");
    }

    handleCollisionEnter(other: RigidBody, otherCollider: Collider, self: RigidBody, selfCollider: Collider) {
        if (selfCollider.tag === 'enemy3body' && otherCollider.tag === 'block') {
            this.patrolDirection *= -1; // 碰到墙壁时改变方向
            console.log("碰到墙壁，改变方向");
        }

        // 检查是否被攻击
        if (selfCollider.tag === 'enemy3body' && otherCollider.tag === 'MaoQi') {
            const attackDirection = this.enemy3.getPlayerLastAttackDirection(); // 假设有一个方法获取玩家上次攻击的方向
            this.enemy3.changeState(new HurtState(this.enemy3, this, attackDirection));
        }
    }
    
    private isPlayerInRange(playerTransform: Transform): boolean {
        const distance = Math.sqrt(Math.pow(playerTransform.x - this.enemy3.gameObject.getBehaviour(Transform).x, 2) + 
                                   Math.pow(playerTransform.y - this.enemy3.gameObject.getBehaviour(Transform).y, 2));
        return distance < 600; // 假设检测范围为 200
    }
}


export class ChaseState extends Enemy3State {
    enter() {
        console.log("Entering Chase State");
    }

    update(duringTime: number) {
        const transform = this.enemy3.gameObject.getBehaviour(Transform);
        const rigidBody = this.enemy3.gameObject.getBehaviour(RigidBody);
        const playerTransform = this.enemy3.getPlayerTransform();

        if (playerTransform) {
            const direction = Math.atan2(playerTransform.y - transform.y, playerTransform.x - transform.x);
            let moveX = Math.cos(direction) * this.enemy3.chaseSpeed;
            let moveY = Math.sin(direction) * this.enemy3.chaseSpeed;

            // 只在X轴上移动
            rigidBody.b2RigidBody.SetLinearVelocity(new b2Vec2(moveX, rigidBody.b2RigidBody.GetLinearVelocity().y));

            if (playerTransform.x > transform.x) {
                this.enemy3.enemy3Binding!.action = 'rightrun';
            } else {
                this.enemy3.enemy3Binding!.action = 'leftrun';
            }
        }

        // 检测是否进入攻击状态
        if (this.isPlayerClose(playerTransform)) {
            this.enemy3.changeState(new AttackState(this.enemy3));
        }

        // 检测是否玩家离开追逐范围
        if (!this.isPlayerInRange(playerTransform)) {
            this.enemy3.changeState(new PatrolState(this.enemy3));
        }

    }

    exit() {
        console.log("Exiting Chase State");
    }

    handleCollisionEnter(other: RigidBody, otherCollider: Collider, self: RigidBody, selfCollider: Collider) {
        // 检查是否被攻击
        if (selfCollider.tag === 'enemy3body' && otherCollider.tag === 'MaoQi') {
            const attackDirection = this.enemy3.getPlayerLastAttackDirection(); // 假设有一个方法获取玩家上次攻击的方向
            this.enemy3.changeState(new HurtState(this.enemy3, this, attackDirection));
        }
    }

    private isPlayerClose(playerTransform: Transform | null): boolean {
        if (!playerTransform) return false;
        const distance = Math.sqrt(Math.pow(playerTransform.x - this.enemy3.gameObject.getBehaviour(Transform).x, 2) + 
                                   Math.pow(playerTransform.y - this.enemy3.gameObject.getBehaviour(Transform).y, 2));
        return distance < 100; // 假设攻击范围为 10
    }

    private isPlayerInRange(playerTransform: Transform | null): boolean {
        if (!playerTransform) return false;
        const distance = Math.sqrt(Math.pow(playerTransform.x - this.enemy3.gameObject.getBehaviour(Transform).x, 2) + 
                                   Math.pow(playerTransform.y - this.enemy3.gameObject.getBehaviour(Transform).y, 2));
        return distance < 600; // 假设追逐范围为 100
    }

}


export class AttackState extends Enemy3State {

    constructor(enemy3: Enemy3) {
        super(enemy3);
    }

    enter() {
        console.log("Entering Attack State");
    }

    update(duringTime: number) {
        const transform = this.enemy3.gameObject.getBehaviour(Transform);
        const playerTransform = this.enemy3.getPlayerTransform();

        if (playerTransform) {
            // 根据小怪和玩家的位置确定攻击方向
            if (playerTransform.x < transform.x) {
                this.enemy3.enemy3Binding!.action = 'leftattack';
            } else {
                this.enemy3.enemy3Binding!.action = 'rightattack';
            }

            // 简单的攻击逻辑
            console.log("Attacking player");

            // 检测是否需要返回巡逻或继续追逐
            if (!this.isPlayerClose(playerTransform)) {
                this.enemy3.changeState(new ChaseState(this.enemy3));
            }
        }
    }

    exit() {
        console.log("Exiting Attack State");
    }

    handleCollisionEnter(other: RigidBody, otherCollider: Collider, self: RigidBody, selfCollider: Collider) {
        // 检查是否被攻击
        if (selfCollider.tag === 'enemy3body' && otherCollider.tag === 'MaoQi') {
            const attackDirection = this.enemy3.getPlayerLastAttackDirection(); // 假设有一个方法获取玩家上次攻击的方向
            this.enemy3.changeState(new HurtState(this.enemy3, this, attackDirection));
        }
    }

    private isPlayerClose(playerTransform: Transform | null): boolean {
        if (!playerTransform) return false;
        const distance = Math.sqrt(Math.pow(playerTransform.x - this.enemy3.gameObject.getBehaviour(Transform).x, 2) + 
                                   Math.pow(playerTransform.y - this.enemy3.gameObject.getBehaviour(Transform).y, 2));
        return distance < 100; // 假设攻击范围为 10
    }
}