import { Behaviour, number, RigidBody, Transform, getGameObjectById } from "../../lib/mygameengine";
import { Collider } from "../../lib/mygameengine";
import { ChaseState, Enemy4State, PatrolState } from "./Enemy4State";
import { Enemy4PrefabBinding } from "../bindings/Enemy4PrefabBinding";
import { Walkable } from "./Walkable";
import { Enemy4HealthStateMachine } from "./Enemy4HealthStateMachine";
import { EnemyRegistry } from "./EnemyRegistry";

export class Enemy4 extends Behaviour {
    @number()
    patrolSpeed = 5;

    @number()
    chaseSpeed = 100;

    private currentState: Enemy4State;
    private playerTransform: Transform | null = null;
    public enemy4Binding: Enemy4PrefabBinding | null = null;
    private player: Walkable | null = null;

    onStart() {
        if (!this.gameObject.getBehaviour(Enemy4HealthStateMachine)) {
            this.gameObject.addBehaviour(new Enemy4HealthStateMachine());
        }

        const playerObject = getGameObjectById('mainRole');
        if (playerObject) {
            this.playerTransform = playerObject.getBehaviour(Transform);
            this.player = playerObject.getBehaviour(Walkable);
        }

        this.enemy4Binding = this.gameObject.getBehaviour(Enemy4PrefabBinding);

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

    changeState(newState: Enemy4State) {
        this.currentState.exit();
        this.currentState = newState;
        this.currentState.enter();
    }

    onTick(duringTime: number) {
        if (this.currentState) {
            this.currentState.update(duringTime);
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
