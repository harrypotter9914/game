import { Behaviour, number, RigidBody, Transform, getGameObjectById } from "../../lib/mygameengine";
import { Collider } from "../../lib/mygameengine";
import { b2Vec2 } from "@flyover/box2d";
import { ChaseState, Enemy6State, PatrolState } from "./Enemy6State";
import { Enemy6PrefabBinding } from "../bindings/Enemy6PrefabBinding";
import { Walkable } from "./Walkable";
import { Enemy6HealthStateMachine } from "./Enemy6HealthStateMachine";

export class Enemy6 extends Behaviour {
    @number()
    patrolSpeed = 5;

    @number()
    chaseSpeed = 100;

    private currentState: Enemy6State;
    private playerTransform: Transform | null = null;
    public enemy6Binding: Enemy6PrefabBinding | null = null;
    private player: Walkable | null = null; // 添加对玩家对象的引用

    onStart() {
        this.currentState = new PatrolState(this);
        this.currentState.enter();

        const enemy6 = getGameObjectById('enemy6');
        if (enemy6) {
            enemy6.addBehaviour(new Enemy6HealthStateMachine());
        } else {
            console.error("Enemy6 GameObject not found");
        }

        // 获取玩家对象的 Transform
        const playerObject = getGameObjectById('mainRole');
        if (playerObject) {
            this.playerTransform = playerObject.getBehaviour(Transform);
            this.player = playerObject.getBehaviour(Walkable); // 获取玩家行为
        } else {
            console.warn("Player object not found");
        }

        // 初始化 enemy6Binding
        this.enemy6Binding = this.gameObject.getBehaviour(Enemy6PrefabBinding);
        if (!this.enemy6Binding) {
            console.error("Enemy6PrefabBinding not found on the game object.");
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

    changeState(newState: Enemy6State) {
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