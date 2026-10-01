import { Behaviour, number, RigidBody, Transform, getGameObjectById } from "../../lib/mygameengine";
import { Collider } from "../../lib/mygameengine";
import { ChaseState, Enemy2State, PatrolState } from "./Enemy2State";
import { Enemy2PrefabBinding } from "../bindings/Enemy2PrefabBinding";
import { Walkable } from "./Walkable";
import { Enemy2HealthStateMachine } from "./Enemy2HealthStateMachine";
import { EnemyRegistry } from "./EnemyRegistry";

export class Enemy2 extends Behaviour {
    @number()
    patrolSpeed = 5;

    @number()
    chaseSpeed = 100;

    private currentState: Enemy2State;
    private playerTransform: Transform | null = null;
    public enemy2Binding: Enemy2PrefabBinding | null = null;
    private player: Walkable | null = null;

    onStart() {
        if (!this.gameObject.getBehaviour(Enemy2HealthStateMachine)) {
            this.gameObject.addBehaviour(new Enemy2HealthStateMachine());
        }

        const playerObject = getGameObjectById('mainRole');
        if (playerObject) {
            this.playerTransform = playerObject.getBehaviour(Transform);
            this.player = playerObject.getBehaviour(Walkable);
        } else {
            console.warn("Player object not found");
        }

        this.enemy2Binding = this.gameObject.getBehaviour(Enemy2PrefabBinding);
        if (!this.enemy2Binding) {
            console.error("Enemy2PrefabBinding not found on the game object.");
        }

        const rigidBody = this.gameObject.getBehaviour(RigidBody);
        if (rigidBody) {
            rigidBody.onCollisionEnter = this.handleCollisionEnter.bind(this);
        }

        this.currentState = new PatrolState(this);
        this.currentState.enter();
        EnemyRegistry.add(this.gameObject);
    }

    getPlayerLastAttackDirection(): string {
        return this.player ? this.player.getLastAttackDirection() : 'right';
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

    onEnd() {
        EnemyRegistry.remove(this.gameObject);
    }
}
