import { Enemy } from "./Enemy";
import { Collider, RigidBody, Transform } from "../../lib/mygameengine";
import { b2Vec2 } from "@flyover/box2d";


export abstract class EnemyState {
    protected enemy: Enemy;

    constructor(enemy: Enemy) {
        this.enemy = enemy;
    }

    abstract enter(): void;
    abstract update(duringTime: number): void;
    abstract exit(): void;

    // 添加抽象方法
    abstract handleCollisionEnter(other: RigidBody, otherCollider: Collider, self: RigidBody, selfCollider: Collider): void;
}



export class PatrolState extends EnemyState {
    private patrolDirection: number = 1; // 1 为右，-1 为左

    enter() {
        console.log("Entering Patrol State");
        this.enemy.gameObject.getBehaviour(RigidBody).b2RigidBody.SetAwake(true); // 确保物理引擎对该对象进行更新
    }

    update(duringTime: number) {
        const transform = this.enemy.gameObject.getBehaviour(Transform);
        const rigidBody = this.enemy.gameObject.getBehaviour(RigidBody);

        // 移动并检测碰撞
        const velocity = new b2Vec2(this.patrolDirection * this.enemy.patrolSpeed, rigidBody.b2RigidBody.GetLinearVelocity().y);
        rigidBody.b2RigidBody.SetLinearVelocity(velocity);
        
        if (this.enemy.enemyBinding) {
            if (this.patrolDirection > 0) {
                this.enemy.enemyBinding.action = 'rightpatrol';
            } else {
                this.enemy.enemyBinding.action = 'leftpatrol';
            }
        } else {
            console.error("EnemyPrefabBinding is not initialized.");
        }

        // 检测是否进入战斗状态
        const playerTransform = this.enemy.getPlayerTransform();
        if (playerTransform && this.isPlayerInRange(playerTransform)) {
            this.enemy.changeState(new ChaseState(this.enemy));
        }

    }

    exit() {
        console.log("Exiting Patrol State");
    }

    handleCollisionEnter(other: RigidBody, otherCollider: Collider, self: RigidBody, selfCollider: Collider) {
        if (selfCollider.tag === 'body' && otherCollider.tag === 'block') {
            this.patrolDirection *= -1; // 碰到墙壁时改变方向
            console.log("碰到墙壁，改变方向");
        }
    }

    private isPlayerInRange(playerTransform: Transform): boolean {
        const distance = Math.sqrt(Math.pow(playerTransform.x - this.enemy.gameObject.getBehaviour(Transform).x, 2) + 
                                   Math.pow(playerTransform.y - this.enemy.gameObject.getBehaviour(Transform).y, 2));
        return distance < 200; // 假设检测范围为 200
    }
}


export class ChaseState extends EnemyState {
    enter() {
        console.log("Entering Chase State");
    }

    update(duringTime: number) {
        const transform = this.enemy.gameObject.getBehaviour(Transform);
        const rigidBody = this.enemy.gameObject.getBehaviour(RigidBody);
        const playerTransform = this.enemy.getPlayerTransform();

        if (playerTransform) {
            const direction = Math.atan2(playerTransform.y - transform.y, playerTransform.x - transform.x);
            let moveX = Math.cos(direction) * this.enemy.chaseSpeed;
            let moveY = Math.sin(direction) * this.enemy.chaseSpeed;

            // 只在X轴上移动
            rigidBody.b2RigidBody.SetLinearVelocity(new b2Vec2(moveX, rigidBody.b2RigidBody.GetLinearVelocity().y));

            if (playerTransform.x > transform.x) {
                this.enemy.enemyBinding!.action = 'rightrun';
            } else {
                this.enemy.enemyBinding!.action = 'leftrun';
            }

            console.log(`Chasing player: moving to (${moveX}, ${moveY})`);
        }

        // 检测是否进入攻击状态
        if (this.isPlayerClose(playerTransform)) {
            this.enemy.changeState(new AttackState(this.enemy));
        }

        // 检测是否玩家离开追逐范围
        if (!this.isPlayerInRange(playerTransform)) {
            this.enemy.changeState(new PatrolState(this.enemy));
        }

    }

    exit() {
        console.log("Exiting Chase State");
    }

    handleCollisionEnter(other: RigidBody, otherCollider: Collider, self: RigidBody, selfCollider: Collider) {
        // 这里可以不处理任何事情，或者添加需要的逻辑
    }

    private isPlayerClose(playerTransform: Transform | null): boolean {
        if (!playerTransform) return false;
        const distance = Math.sqrt(Math.pow(playerTransform.x - this.enemy.gameObject.getBehaviour(Transform).x, 2) + 
                                   Math.pow(playerTransform.y - this.enemy.gameObject.getBehaviour(Transform).y, 2));
        return distance < 100; // 假设攻击范围为 10
    }

    private isPlayerInRange(playerTransform: Transform | null): boolean {
        if (!playerTransform) return false;
        const distance = Math.sqrt(Math.pow(playerTransform.x - this.enemy.gameObject.getBehaviour(Transform).x, 2) + 
                                   Math.pow(playerTransform.y - this.enemy.gameObject.getBehaviour(Transform).y, 2));
        return distance < 200; // 假设追逐范围为 100
    }

}


export class AttackState extends EnemyState {
    enter() {
        console.log("Entering Attack State");
    }

    update(duringTime: number) {
        const transform = this.enemy.gameObject.getBehaviour(Transform);
        const playerTransform = this.enemy.getPlayerTransform();

        if (playerTransform) {
            // 根据小怪和玩家的位置确定攻击方向
            if (playerTransform.x < transform.x) {
                this.enemy.enemyBinding!.action = 'leftattack';
            } else {
                this.enemy.enemyBinding!.action = 'rightattack';
            }

            // 简单的攻击逻辑
            console.log("Attacking player");

            // 检测是否需要返回巡逻或继续追逐
            if (!this.isPlayerClose(playerTransform)) {
                this.enemy.changeState(new ChaseState(this.enemy));
            }
        }
    }

    exit() {
        console.log("Exiting Attack State");
    }

    handleCollisionEnter(other: RigidBody, otherCollider: Collider, self: RigidBody, selfCollider: Collider) {
        // 这里可以不处理任何事情，或者添加需要的逻辑
    }

    private isPlayerClose(playerTransform: Transform | null): boolean {
        if (!playerTransform) return false;
        const distance = Math.sqrt(Math.pow(playerTransform.x - this.enemy.gameObject.getBehaviour(Transform).x, 2) + 
                                   Math.pow(playerTransform.y - this.enemy.gameObject.getBehaviour(Transform).y, 2));
        return distance < 100; // 假设攻击范围为 10
    }
}