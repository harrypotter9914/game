import { Enemy } from "./Enemy";
import { Collider, RigidBody, Transform } from "../../lib/mygameengine";
import { b2Vec2 } from "@flyover/box2d";
import { tryHitPlayer } from "./EnemyRuntime";

export abstract class EnemyState {
    protected enemy: Enemy;
    constructor(enemy: Enemy) { this.enemy = enemy; }
    abstract enter(): void;
    abstract update(duringTime: number): void;
    abstract exit(): void;
    abstract handleCollisionEnter(other: RigidBody, otherCollider: Collider, self: RigidBody, selfCollider: Collider): void;
}

export class HurtState extends EnemyState {
    private previousState: EnemyState;
    constructor(enemy: Enemy, previousState: EnemyState) { super(enemy); this.previousState = previousState; }
    enter() { setTimeout(() => this.enemy.changeState(this.previousState), 180); }
    update() {}
    exit() {}
    handleCollisionEnter() {}
}

export class PatrolState extends EnemyState {
    private patrolDirection = 1;
    enter() {}
    update() {
        const rigidBody = this.enemy.gameObject.getBehaviour(RigidBody);
        const transform = this.enemy.gameObject.getBehaviour(Transform);
        const playerTransform = this.enemy.getPlayerTransform();
        if (!rigidBody || !transform || !playerTransform) return;
        rigidBody.b2RigidBody.SetLinearVelocity(new b2Vec2(this.patrolDirection * this.enemy.patrolSpeed, rigidBody.b2RigidBody.GetLinearVelocity().y));
        this.enemy.enemyBinding!.action = this.patrolDirection > 0 ? 'rightpatrol' : 'leftpatrol';
        if (Math.abs(playerTransform.x - transform.x) < 520 && Math.abs(playerTransform.y - transform.y) < 160) {
            this.enemy.changeState(new ChaseState(this.enemy));
        }
    }
    exit() {}
    handleCollisionEnter(other: RigidBody, otherCollider: Collider, self: RigidBody, selfCollider: Collider) {
        if (selfCollider.tag === 'enemybody' && otherCollider.tag === 'block') this.patrolDirection *= -1;
    }
}

export class ChaseState extends EnemyState {
    enter() {}
    update() {
        const transform = this.enemy.gameObject.getBehaviour(Transform);
        const rigidBody = this.enemy.gameObject.getBehaviour(RigidBody);
        const playerTransform = this.enemy.getPlayerTransform();
        if (!playerTransform || !transform || !rigidBody) return;
        const dx = playerTransform.x - transform.x;
        const distance = Math.abs(dx);
        const direction = dx > 0 ? 1 : -1;
        rigidBody.b2RigidBody.SetLinearVelocity(new b2Vec2(direction * this.enemy.chaseSpeed, rigidBody.b2RigidBody.GetLinearVelocity().y));
        this.enemy.enemyBinding!.action = direction > 0 ? 'rightrun' : 'leftrun';
        if (distance < 120) this.enemy.changeState(new AttackState(this.enemy));
        if (distance > 640) this.enemy.changeState(new PatrolState(this.enemy));
    }
    exit() {}
    handleCollisionEnter() {}
}

export class AttackState extends EnemyState {
    private phase: 'windup' | 'cooldown' = 'windup';
    private phaseStartedAt = 0;
    enter() { this.phase = 'windup'; this.phaseStartedAt = Date.now(); }
    update() {
        const transform = this.enemy.gameObject.getBehaviour(Transform);
        const rigidBody = this.enemy.gameObject.getBehaviour(RigidBody);
        const playerTransform = this.enemy.getPlayerTransform();
        if (!playerTransform || !transform || !rigidBody) return;
        rigidBody.b2RigidBody.SetLinearVelocity(new b2Vec2(0, rigidBody.b2RigidBody.GetLinearVelocity().y));
        const dx = playerTransform.x - transform.x;
        const distance = Math.abs(dx);
        const facingLeft = dx < 0;
        this.enemy.enemyBinding!.action = facingLeft ? 'leftattack' : 'rightattack';

        if (distance > 150) {
            this.enemy.changeState(new ChaseState(this.enemy));
            return;
        }

        const elapsed = Date.now() - this.phaseStartedAt;
        if (this.phase === 'windup' && elapsed > 220) {
            tryHitPlayer(this.enemy.gameObject, 1, facingLeft ? 'leftattack' : 'rightattack');
            this.phase = 'cooldown';
            this.phaseStartedAt = Date.now();
            return;
        }

        if (this.phase === 'cooldown' && elapsed > 480) {
            this.phase = 'windup';
            this.phaseStartedAt = Date.now();
        }
    }
    exit() {}
    handleCollisionEnter() {}
}
