import { Enemy5 } from "./Enemy5";
import { Collider, RigidBody, Transform } from "../../lib/mygameengine";
import { tryHitPlayer } from "./EnemyRuntime";
import { BattleCue } from "./BattleCue";

export abstract class Enemy5State {
    protected enemy5: Enemy5;
    constructor(enemy5: Enemy5) { this.enemy5 = enemy5; }
    abstract enter(): void;
    abstract update(duringTime: number): void;
    abstract exit(): void;
    abstract handleCollisionEnter(other: RigidBody, otherCollider: Collider, self: RigidBody, selfCollider: Collider): void;
}

export class HurtState extends Enemy5State {
    private previousState: Enemy5State;
    constructor(enemy5: Enemy5, previousState: Enemy5State) { super(enemy5); this.previousState = previousState; }
    enter() { setTimeout(() => this.enemy5.changeState(this.previousState), 220); }
    update() {}
    exit() {}
    handleCollisionEnter() {}
}

export class PatrolState extends Enemy5State {
    enter() { this.enemy5.enemy5Binding!.action = 'rightpatrol'; }
    update() {
        const transform = this.enemy5.gameObject.getBehaviour(Transform);
        const playerTransform = this.enemy5.getPlayerTransform();
        if (!transform || !playerTransform) return;
        if (Math.abs(playerTransform.x - transform.x) < 820) this.enemy5.changeState(new ChaseState(this.enemy5));
    }
    exit() {}
    handleCollisionEnter() {}
}

export class ChaseState extends Enemy5State {
    enter() {}
    update() {
        const transform = this.enemy5.gameObject.getBehaviour(Transform);
        const playerTransform = this.enemy5.getPlayerTransform();
        if (!playerTransform || !transform) return;
        if (Math.abs(playerTransform.x - transform.x) < 460) this.enemy5.changeState(new AttackState(this.enemy5));
    }
    exit() {}
    handleCollisionEnter() {}
}

export class AttackState extends Enemy5State {
    private phase: 'burrow' | 'burst' | 'recover' = 'burrow';
    private phaseStartedAt = 0;
    enter() { this.phase = 'burrow'; this.phaseStartedAt = Date.now(); }
    update() {
        const transform = this.enemy5.gameObject.getBehaviour(Transform);
        const playerTransform = this.enemy5.getPlayerTransform();
        const rigidBody = this.enemy5.gameObject.getBehaviour(RigidBody);
        if (!playerTransform || !transform || !rigidBody) return;
        const elapsed = Date.now() - this.phaseStartedAt;

        if (this.phase === 'burrow') {
            this.enemy5.enemy5Binding!.action = 'leftpatrol';
            if (elapsed < 20) {
                BattleCue.show('Burrow Boss', 'Below your feet', 620);
            }
            if (elapsed > 540) {
                const offset = playerTransform.x > transform.x ? -140 : 140;
                const targetX = playerTransform.x + offset;
                const targetY = transform.y;
                rigidBody.x = targetX;
                rigidBody.y = targetY;
                rigidBody.b2RigidBody.SetTransformXY(targetX, targetY, 0);
                rigidBody.b2RigidBody.SetLinearVelocity({ x: 0, y: 0 } as any);
                transform.x = targetX;
                transform.y = targetY;
                this.enemy5.enemy5Binding!.action = offset < 0 ? 'rightattack' : 'leftattack';
                tryHitPlayer(this.enemy5.gameObject, 2, offset < 0 ? 'rightattack' : 'leftattack');
                this.phase = 'burst';
                this.phaseStartedAt = Date.now();
                return;
            }
        } else if (this.phase === 'burst') {
            if (elapsed > 240) {
                this.phase = 'recover';
                this.phaseStartedAt = Date.now();
                return;
            }
        } else if (elapsed > 960) {
            if (Math.abs(playerTransform.x - transform.x) > 700) {
                this.enemy5.changeState(new ChaseState(this.enemy5));
            } else {
                this.phase = 'burrow';
                this.phaseStartedAt = Date.now();
            }
        }
    }
    exit() {}
    handleCollisionEnter() {}
}
