import { Behaviour, number, RigidBody, Transform, getGameObjectById } from "../../lib/mygameengine";
import { Collider } from "../../lib/mygameengine";
import { b2Vec2 } from "@flyover/box2d";
import { ChaseState, Enemy4State, PatrolState } from "./Enemy4State";
import { Enemy4PrefabBinding } from "../bindings/Enemy4PrefabBinding";
import { Walkable } from "./Walkable";
import { Enemy4HealthStateMachine } from "./Enemy4HealthStateMachine";

export class Enemy4 extends Behaviour {
    @number()
    patrolSpeed = 5;

    @number()
    chaseSpeed = 100;

    private currentState: Enemy4State;
    private playerTransform: Transform | null = null;
    public enemy4Binding: Enemy4PrefabBinding | null = null;
    private player: Walkable | null = null; // 添加对玩家对象的引用

    onStart() {
        this.currentState = new PatrolState(this);
        this.currentState.enter();

        const enemy4 = getGameObjectById('enemy4');
        if (enemy4) {
            enemy4.addBehaviour(new Enemy4HealthStateMachine());
        } else {
            console.error("Enemy4 GameObject not found");
        }

        // 获取玩家对象的 Transform
        const playerObject = getGameObjectById('mainRole');
        if (playerObject) {
            this.playerTransform = playerObject.getBehaviour(Transform);
            this.player = playerObject.getBehaviour(Walkable); // 获取玩家行为
        } else {
            console.warn("Player object not found");
        }

        // 初始化 enemy4Binding
        this.enemy4Binding = this.gameObject.getBehaviour(Enemy4PrefabBinding);
        if (!this.enemy4Binding) {
            console.error("Enemy4PrefabBinding not found on the game object.");
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

    changeState(newState: Enemy4State) {
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