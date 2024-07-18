import { Behaviour, number, RigidBody, Transform, getGameObjectById } from "../../lib/mygameengine";
import { Collider } from "../../lib/mygameengine";
import { b2Vec2 } from "@flyover/box2d";
import { ChaseState, Enemy5State, PatrolState } from "./Enemy5State";
import { Enemy5PrefabBinding } from "../bindings/Enemy5PrefabBinding";
import { Walkable } from "./Walkable";
import { Enemy5HealthStateMachine } from "./Enemy5HealthStateMachine";

export class Enemy5 extends Behaviour {
    @number()
    patrolSpeed = 5;

    @number()
    chaseSpeed = 100;

    private currentState: Enemy5State;
    private playerTransform: Transform | null = null;
    public enemy5Binding: Enemy5PrefabBinding | null = null;
    private player: Walkable | null = null; // 添加对玩家对象的引用

    onStart() {
        this.currentState = new PatrolState(this);
        this.currentState.enter();

        const enemy5 = getGameObjectById('enemy5');
        if (enemy5) {
            enemy5.addBehaviour(new Enemy5HealthStateMachine());
        } else {
            console.error("Enemy5 GameObject not found");
        }

        // 获取玩家对象的 Transform
        const playerObject = getGameObjectById('mainRole');
        if (playerObject) {
            this.playerTransform = playerObject.getBehaviour(Transform);
            this.player = playerObject.getBehaviour(Walkable); // 获取玩家行为
        } else {
            console.warn("Player object not found");
        }

        // 初始化 enemy5Binding
        this.enemy5Binding = this.gameObject.getBehaviour(Enemy5PrefabBinding);
        if (!this.enemy5Binding) {
            console.error("Enemy5PrefabBinding not found on the game object.");
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

    changeState(newState: Enemy5State) {
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