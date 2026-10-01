import { Enemy1 } from "./Enemy1";
import { Collider, RigidBody, Transform } from "../../lib/mygameengine";
import { b2Vec2 } from "@flyover/box2d";
import { tryHitPlayer } from "./EnemyRuntime";

export abstract class Enemy1State {
    protected enemy1: Enemy1;
    constructor(enemy1: Enemy1) { this.enemy1 = enemy1; }
    abstract enter(): void;
    abstract update(duringTime: number): void;
    abstract exit(): void;
    abstract handleCollisionEnter(other: RigidBody, otherCollider: Collider, self: RigidBody, selfCollider: Collider): void;
}

export class HurtState extends Enemy1State {
    private previousState: Enemy1State;
    constructor(enemy1: Enemy1, previousState: Enemy1State) { super(enemy1); this.previousState = previousState; }
    enter() { setTimeout(() => this.enemy1.changeState(this.previousState), 220); }
    update() {}
    exit() {}
    handleCollisionEnter() {}
}

export class PatrolState extends Enemy1State {
    private patrolDirection = 1;
    enter() {}
    update() {
        const rigidBody = this.enemy1.gameObject.getBehaviour(RigidBody);
        const transform = this.enemy1.gameObject.getBehaviour(Transform);
        const playerTransform = this.enemy1.getPlayerTransform();
        if (!rigidBody || !transform || !playerTransform) return;
        rigidBody.b2RigidBody.SetLinearVelocity(new b2Vec2(this.patrolDirection * this.enemy1.patrolSpeed, rigidBody.b2RigidBody.GetLinearVelocity().y));
        this.enemy1.enemy1Binding!.action = this.patrolDirection > 0 ? 'rightpatrol' : 'leftpatrol';
        if (Math.abs(playerTransform.x - transform.x) < 520) this.enemy1.changeState(new ChaseState(this.enemy1));
    }
    exit() {}
    handleCollisionEnter(other: RigidBody, otherCollider: Collider, self: RigidBody, selfCollider: Collider) { if (selfCollider.tag === 'enemybody' && otherCollider.tag === 'block') this.patrolDirection *= -1; }
}

export class ChaseState extends Enemy1State {
    enter() {}
    update() {
        const transform = this.enemy1.gameObject.getBehaviour(Transform);
        const rigidBody = this.enemy1.gameObject.getBehaviour(RigidBody);
        const playerTransform = this.enemy1.getPlayerTransform();
        if (!playerTransform || !transform || !rigidBody) return;
        const dx = playerTransform.x - transform.x;
        const distance = Math.abs(dx);
        const direction = dx > 0 ? 1 : -1;
        rigidBody.b2RigidBody.SetLinearVelocity(new b2Vec2(direction * this.enemy1.chaseSpeed, rigidBody.b2RigidBody.GetLinearVelocity().y));
        this.enemy1.enemy1Binding!.action = direction > 0 ? 'rightrun' : 'leftrun';
        if (distance < 145) this.enemy1.changeState(new AttackState(this.enemy1));
        if (distance > 660) this.enemy1.changeState(new PatrolState(this.enemy1));
    }
    exit() {}
    handleCollisionEnter() {}
}

export class AttackState extends Enemy1State {
    private phase: 'brace' | 'strike' | 'recover' = 'brace';
    private phaseStartedAt = 0;
    enter() { this.phase = 'brace'; this.phaseStartedAt = Date.now(); }
    update() {
        const transform = this.enemy1.gameObject.getBehaviour(Transform);
        const rigidBody = this.enemy1.gameObject.getBehaviour(RigidBody);
        const playerTransform = this.enemy1.getPlayerTransform();
        if (!playerTransform || !transform || !rigidBody) return;
        rigidBody.b2RigidBody.SetLinearVelocity(new b2Vec2(0, rigidBody.b2RigidBody.GetLinearVelocity().y));
        const dx = playerTransform.x - transform.x;
        const distance = Math.abs(dx);
        const facingLeft = dx < 0;
        this.enemy1.enemy1Binding!.action = facingLeft ? 'leftattack' : 'rightattack';

        if (distance > 170) {
            this.enemy1.changeState(new ChaseState(this.enemy1));
            return;
        }

        const elapsed = Date.now() - this.phaseStartedAt;
        if (this.phase === 'brace' && elapsed > 280) {
            this.phase = 'strike';
            this.phaseStartedAt = Date.now();
            tryHitPlayer(this.enemy1.gameObject, 1, facingLeft ? 'leftattack' : 'rightattack');
            return;
        }
        if (this.phase === 'strike' && elapsed > 180) {
            this.phase = 'recover';
            this.phaseStartedAt = Date.now();
            return;
        }
        if (this.phase === 'recover' && elapsed > 620) {
            this.phase = 'brace';
            this.phaseStartedAt = Date.now();
        }
    }
    exit() {}
    handleCollisionEnter() {}
}
