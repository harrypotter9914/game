import { Enemy3 } from "./Enemy3";
import { Collider, RigidBody, Transform } from "../../lib/mygameengine";
import { b2Vec2 } from "@flyover/box2d";
import { tryHitPlayer } from "./EnemyRuntime";
import { BattleCue } from "./BattleCue";

export abstract class Enemy3State {
    protected enemy3: Enemy3;
    constructor(enemy3: Enemy3) { this.enemy3 = enemy3; }
    abstract enter(): void;
    abstract update(duringTime: number): void;
    abstract exit(): void;
    abstract handleCollisionEnter(other: RigidBody, otherCollider: Collider, self: RigidBody, selfCollider: Collider): void;
}

export class HurtState extends Enemy3State {
    private previousState: Enemy3State;
    constructor(enemy3: Enemy3, previousState: Enemy3State) { super(enemy3); this.previousState = previousState; }
    enter() { setTimeout(() => this.enemy3.changeState(this.previousState), 260); }
    update() {}
    exit() {}
    handleCollisionEnter() {}
}

export class PatrolState extends Enemy3State {
    private patrolDirection = 1;
    enter() {}
    update() {
        const rigidBody = this.enemy3.gameObject.getBehaviour(RigidBody);
        const transform = this.enemy3.gameObject.getBehaviour(Transform);
        const playerTransform = this.enemy3.getPlayerTransform();
        if (!rigidBody || !transform || !playerTransform) return;
        rigidBody.b2RigidBody.SetLinearVelocity(new b2Vec2(this.patrolDirection * this.enemy3.patrolSpeed, rigidBody.b2RigidBody.GetLinearVelocity().y));
        this.enemy3.enemy3Binding!.action = this.patrolDirection > 0 ? 'rightpatrol' : 'leftpatrol';
        if (Math.abs(playerTransform.x - transform.x) < 720) this.enemy3.changeState(new ChaseState(this.enemy3));
    }
    exit() {}
    handleCollisionEnter(other: RigidBody, otherCollider: Collider, self: RigidBody, selfCollider: Collider) { if (selfCollider.tag === 'enemybody' && otherCollider.tag === 'block') this.patrolDirection *= -1; }
}

export class ChaseState extends Enemy3State {
    enter() {}
    update() {
        const transform = this.enemy3.gameObject.getBehaviour(Transform);
        const rigidBody = this.enemy3.gameObject.getBehaviour(RigidBody);
        const playerTransform = this.enemy3.getPlayerTransform();
        if (!playerTransform || !transform || !rigidBody) return;
        const dx = playerTransform.x - transform.x;
        const distance = Math.abs(dx);
        const direction = dx > 0 ? 1 : -1;
        rigidBody.b2RigidBody.SetLinearVelocity(new b2Vec2(direction * this.enemy3.chaseSpeed, rigidBody.b2RigidBody.GetLinearVelocity().y));
        this.enemy3.enemy3Binding!.action = direction > 0 ? 'rightrun' : 'leftrun';
        if (distance < 220) this.enemy3.changeState(new AttackState(this.enemy3));
        if (distance > 860) this.enemy3.changeState(new PatrolState(this.enemy3));
    }
    exit() {}
    handleCollisionEnter() {}
}

export class AttackState extends Enemy3State {
    private phase: 'lift' | 'slam' | 'recover' = 'lift';
    private phaseStartedAt = 0;
    enter() { this.phase = 'lift'; this.phaseStartedAt = Date.now(); }
    update() {
        const transform = this.enemy3.gameObject.getBehaviour(Transform);
        const playerTransform = this.enemy3.getPlayerTransform();
        const rigidBody = this.enemy3.gameObject.getBehaviour(RigidBody);
        if (!playerTransform || !transform || !rigidBody) return;
        rigidBody.b2RigidBody.SetLinearVelocity(new b2Vec2(0, rigidBody.b2RigidBody.GetLinearVelocity().y));
        const dx = playerTransform.x - transform.x;
        const distance = Math.abs(dx);
        const facingLeft = dx < 0;
        this.enemy3.enemy3Binding!.action = facingLeft ? 'leftattack' : 'rightattack';

        if (distance > 260) {
            this.enemy3.changeState(new ChaseState(this.enemy3));
            return;
        }

        const elapsed = Date.now() - this.phaseStartedAt;
        if (this.phase === 'lift' && elapsed < 10) {
            BattleCue.show('Giant Penitent', 'Heavy slam', 520);
        }
        if (this.phase === 'lift' && elapsed > 420) {
            this.phase = 'slam';
            this.phaseStartedAt = Date.now();
            tryHitPlayer(this.enemy3.gameObject, 2, facingLeft ? 'leftattack' : 'rightattack');
            return;
        }
        if (this.phase === 'slam' && elapsed > 160) {
            this.phase = 'recover';
            this.phaseStartedAt = Date.now();
            return;
        }
        if (this.phase === 'recover' && elapsed > 880) {
            this.phase = 'lift';
            this.phaseStartedAt = Date.now();
        }
    }
    exit() {}
    handleCollisionEnter() {}
}
