import { Behaviour, number, RigidBody, Transform, getGameObjectById } from "../../lib/mygameengine";
import { Collider } from "../../lib/mygameengine";
import { b2Vec2 } from "@flyover/box2d";
import { ChaseState, Enemy2State, PatrolState } from "./Enemy2State";
import { Enemy2PrefabBinding } from "../bindings/Enemy2PrefabBinding";
import { Walkable } from "./Walkable";
import { Enemy2HealthStateMachine } from "./Enemy2HealthStateMachine";

export class Enemy2 extends Behaviour {
    @number()
    patrolSpeed = 5;

    @number()
    chaseSpeed = 100;

    private currentState: Enemy2State;
    private playerTransform: Transform | null = null;
    public enemy2Binding: Enemy2PrefabBinding | null = null;
    private player: Walkable | null = null; // 添加对玩家对象的引用

    onStart() {
        this.currentState = new PatrolState(this);
        this.currentState.enter();

        const enemy2 = getGameObjectById('enemy2');
        if (enemy2) {
            enemy2.addBehaviour(new Enemy2HealthStateMachine());
        } else {
            console.error("Enemy2 GameObject not found");
        }

        // 获取玩家对象的 Transform
        const playerObject = getGameObjectById('mainRole');
        if (playerObject) {
            this.playerTransform = playerObject.getBehaviour(Transform);
            this.player = playerObject.getBehaviour(Walkable); // 获取玩家行为
        } else {
            console.warn("Player object not found");
        }

        // 初始化 enemy2Binding
        this.enemy2Binding = this.gameObject.getBehaviour(Enemy2PrefabBinding);
        if (!this.enemy2Binding) {
            console.error("Enemy2PrefabBinding not found on the game object.");
        }

        // 添加碰撞检测
        const rigidBody = this.gameObject.getBehaviour(RigidBody);
        if (rigidBody) {
            rigidBody.onCollisionEnter = this.handleCollisionEnter.bind(this);
        }

    }

    getPlayerLastAttackDirection(): string {
        if (this.player) {
            return this.player.getLastAttackDirection();
        }
        return 'right'; // 默认方向
    }

    handleCollisionEnter(other: RigidBody, otherCollider: Collider, self: RigidBody, selfCollider: Collider) {
        if (this.currentState && typeof this.currentState.handleCollisionEnter === 'function') {
            this.currentState.handleCollisionEnter(other, otherCollider, self, selfCollider);
        }
    }

    changeState(newState: Enemy2State) {
        this.currentState.exit();
        this.currentState = newState;
        this.currentState.enter();
    }

    onTick(duringTime: number) {
        if (this.currentState) {
            this.currentState.update(duringTime);
        } else {
            console.warn("Current state is not initialized");
        }
    }

    getPlayerTransform(): Transform | null {
        return this.playerTransform;
    }

    getCurrentSpeed(): number {
        if (this.currentState instanceof PatrolState) {
            return this.patrolSpeed;
        } else if (this.currentState instanceof ChaseState) {
            return this.chaseSpeed;
        }
        return 0;
    }
}