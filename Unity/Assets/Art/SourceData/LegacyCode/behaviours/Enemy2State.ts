import { Enemy2 } from "./Enemy2";
import { Collider, RigidBody, Transform } from "../../lib/mygameengine";
import { b2Vec2 } from "@flyover/box2d";
import { tryHitPlayer } from "./EnemyRuntime";
import { BattleCue } from "./BattleCue";

export abstract class Enemy2State {
    protected enemy2: Enemy2;
    constructor(enemy2: Enemy2) { this.enemy2 = enemy2; }
    abstract enter(): void;
    abstract update(duringTime: number): void;
    abstract exit(): void;
    abstract handleCollisionEnter(other: RigidBody, otherCollider: Collider, self: RigidBody, selfCollider: Collider): void;
}

export class HurtState extends Enemy2State {
    private previousState: Enemy2State;
    constructor(enemy2: Enemy2, previousState: Enemy2State) { super(enemy2); this.previousState = previousState; }
    enter() { setTimeout(() => this.enemy2.changeState(this.previousState), 220); }
    update() {}
    exit() {}
    handleCollisionEnter() {}
}

export class PatrolState extends Enemy2State {
    private patrolDirection = 1;
    enter() {}
    update() {
        const rigidBody = this.enemy2.gameObject.getBehaviour(RigidBody);
        const transform = this.enemy2.gameObject.getBehaviour(Transform);
        const playerTransform = this.enemy2.getPlayerTransform();
        if (!rigidBody || !transform || !playerTransform) return;
        rigidBody.b2RigidBody.SetLinearVelocity(new b2Vec2(this.patrolDirection * this.enemy2.patrolSpeed, rigidBody.b2RigidBody.GetLinearVelocity().y));
        this.enemy2.enemy2Binding!.action = this.patrolDirection > 0 ? 'rightpatrol' : 'leftpatrol';
        if (Math.abs(playerTransform.x - transform.x) < 760 && Math.abs(playerTransform.y - transform.y) < 220) {
            this.enemy2.changeState(new ChaseState(this.enemy2));
        }
    }
    exit() {}
    handleCollisionEnter(other: RigidBody, otherCollider: Collider, self: RigidBody, selfCollider: Collider) { if (selfCollider.tag === 'enemybody' && otherCollider.tag === 'block') this.patrolDirection *= -1; }
}

export class ChaseState extends Enemy2State {
    enter() {}
    update() {
        const transform = this.enemy2.gameObject.getBehaviour(Transform);
        const rigidBody = this.enemy2.gameObject.getBehaviour(RigidBody);
        const playerTransform = this.enemy2.getPlayerTransform();
        if (!playerTransform || !transform || !rigidBody) return;
        const dx = playerTransform.x - transform.x;
        const distance = Math.abs(dx);
        const direction = dx > 0 ? 1 : -1;

        if (distance > 340) {
            rigidBody.b2RigidBody.SetLinearVelocity(new b2Vec2(direction * this.enemy2.chaseSpeed, rigidBody.b2RigidBody.GetLinearVelocity().y));
            this.enemy2.enemy2Binding!.action = direction > 0 ? 'rightrun' : 'leftrun';
        } else if (distance < 220) {
            rigidBody.b2RigidBody.SetLinearVelocity(new b2Vec2(-direction * this.enemy2.chaseSpeed, rigidBody.b2RigidBody.GetLinearVelocity().y));
            this.enemy2.enemy2Binding!.action = direction > 0 ? 'leftpatrol' : 'rightpatrol';
        } else {
            rigidBody.b2RigidBody.SetLinearVelocity(new b2Vec2(0, rigidBody.b2RigidBody.GetLinearVelocity().y));
            this.enemy2.changeState(new AttackState(this.enemy2));
        }

        if (distance > 860) this.enemy2.changeState(new PatrolState(this.enemy2));
    }
    exit() {}
    handleCollisionEnter() {}
}

export class AttackState extends Enemy2State {
    private phase: 'aim' | 'fire' | 'recover' = 'aim';
    private phaseStartedAt = 0;
    enter() { this.phase = 'aim'; this.phaseStartedAt = Date.now(); }
    update() {
        const transform = this.enemy2.gameObject.getBehaviour(Transform);
        const playerTransform = this.enemy2.getPlayerTransform();
        const rigidBody = this.enemy2.gameObject.getBehaviour(RigidBody);
        if (!playerTransform || !transform || !rigidBody) return;
        rigidBody.b2RigidBody.SetLinearVelocity(new b2Vec2(0, rigidBody.b2RigidBody.GetLinearVelocity().y));
        const dx = playerTransform.x - transform.x;
        const distance = Math.abs(dx);
        const facingLeft = dx < 0;
        this.enemy2.enemy2Binding!.action = facingLeft ? 'leftattack' : 'rightattack';

        if (distance < 180 || distance > 460) {
            this.enemy2.changeState(new ChaseState(this.enemy2));
            return;
        }

        const elapsed = Date.now() - this.phaseStartedAt;
        if (this.phase === 'aim' && elapsed < 10) {
            BattleCue.show('Boneslinger', 'Ranged shot incoming', 420);
        }
        if (this.phase === 'aim' && elapsed > 350) {
            this.phase = 'fire';
            this.phaseStartedAt = Date.now();
            tryHitPlayer(this.enemy2.gameObject, 1, facingLeft ? 'leftattack' : 'rightattack');
            return;
        }
        if (this.phase === 'fire' && elapsed > 120) {
            this.phase = 'recover';
            this.phaseStartedAt = Date.now();
            return;
        }
        if (this.phase === 'recover' && elapsed > 720) {
            this.phase = 'aim';
            this.phaseStartedAt = Date.now();
        }
    }
    exit() {}
    handleCollisionEnter() {}
}
