import { Behaviour, number, RigidBody, Transform, getGameObjectById } from "../../lib/mygameengine";
import { Collider } from "../../lib/mygameengine";
import { ChaseState, EnemyState, PatrolState } from "./EnemyState";
import { EnemyPrefabBinding } from "../bindings/EnemyPrefabBinding";
import { Walkable } from "./Walkable";
import { EnemyHealthStateMachine } from "./EnemyHealthStateMachine";
import { EnemyRegistry } from "./EnemyRegistry";

export class Enemy extends Behaviour {
    @number()
    patrolSpeed = 5;

    @number()
    chaseSpeed = 100;

    private currentState: EnemyState;
    private playerTransform: Transform | null = null;
    public enemyBinding: EnemyPrefabBinding | null = null;
    private player: Walkable | null = null;

    onStart() {
        if (!this.gameObject.getBehaviour(EnemyHealthStateMachine)) {
            this.gameObject.addBehaviour(new EnemyHealthStateMachine());
        }

        const playerObject = getGameObjectById('mainRole');
        if (playerObject) {
            this.playerTransform = playerObject.getBehaviour(Transform);
            this.player = playerObject.getBehaviour(Walkable);
        } else {
            console.warn("Player object not found");
        }

        this.enemyBinding = this.gameObject.getBehaviour(EnemyPrefabBinding);
        if (!this.enemyBinding) {
            console.error("EnemyPrefabBinding not found on the game object.");
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

    changeState(newState: EnemyState) {
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
