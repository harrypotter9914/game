import { Enemy4 } from "./Enemy4";
import { Collider, RigidBody, Transform } from "../../lib/mygameengine";
import { b2Vec2 } from "@flyover/box2d";
import { tryHitPlayer } from "./EnemyRuntime";
import { BattleCue } from "./BattleCue";

export abstract class Enemy4State {
    protected enemy4: Enemy4;
    constructor(enemy4: Enemy4) { this.enemy4 = enemy4; }
    abstract enter(): void;
    abstract update(duringTime: number): void;
    abstract exit(): void;
    abstract handleCollisionEnter(other: RigidBody, otherCollider: Collider, self: RigidBody, selfCollider: Collider): void;
}

export class HurtState extends Enemy4State {
    private previousState: Enemy4State;
    constructor(enemy4: Enemy4, previousState: Enemy4State) { super(enemy4); this.previousState = previousState; }
    enter() { setTimeout(() => this.enemy4.changeState(this.previousState), 260); }
    update() {}
    exit() {}
    handleCollisionEnter() {}
}

export class PatrolState extends Enemy4State {
    enter() { this.enemy4.enemy4Binding!.action = 'rightpatrol'; }
    update() {
        const transform = this.enemy4.gameObject.getBehaviour(Transform);
        const playerTransform = this.enemy4.getPlayerTransform();
        if (!transform || !playerTransform) return;
        if (Math.abs(playerTransform.x - transform.x) < 920) this.enemy4.changeState(new ChaseState(this.enemy4));
    }
    exit() {}
    handleCollisionEnter() {}
}

export class ChaseState extends Enemy4State {
    enter() {}
    update() {
        const transform = this.enemy4.gameObject.getBehaviour(Transform);
        const rigidBody = this.enemy4.gameObject.getBehaviour(RigidBody);
        const playerTransform = this.enemy4.getPlayerTransform();
        if (!playerTransform || !transform || !rigidBody) return;
        const dx = playerTransform.x - transform.x;
        const direction = dx > 0 ? 1 : -1;
        const distance = Math.abs(dx);
        rigidBody.b2RigidBody.SetLinearVelocity(new b2Vec2(direction * this.enemy4.chaseSpeed, rigidBody.b2RigidBody.GetLinearVelocity().y));
        this.enemy4.enemy4Binding!.action = direction > 0 ? 'rightrun' : 'leftrun';
        if (distance < 520) this.enemy4.changeState(new AttackState(this.enemy4));
    }
    exit() {}
    handleCollisionEnter() {}
}

export class AttackState extends Enemy4State {
    private phase: 'telegraph' | 'charge' | 'recover' = 'telegraph';
    private phaseStartedAt = 0;
    enter() { this.phase = 'telegraph'; this.phaseStartedAt = Date.now(); }
    update() {
        const transform = this.enemy4.gameObject.getBehaviour(Transform);
        const rigidBody = this.enemy4.gameObject.getBehaviour(RigidBody);
        const playerTransform = this.enemy4.getPlayerTransform();
        if (!playerTransform || !transform || !rigidBody) return;
        const dx = playerTransform.x - transform.x;
        const distance = Math.abs(dx);
        const facingLeft = dx < 0;
        const sign = facingLeft ? -1 : 1;
        this.enemy4.enemy4Binding!.action = facingLeft ? 'leftattack' : 'rightattack';

        if (distance > 700) {
            this.enemy4.changeState(new ChaseState(this.enemy4));
            return;
        }

        const elapsed = Date.now() - this.phaseStartedAt;
        if (this.phase === 'telegraph') {
            rigidBody.b2RigidBody.SetLinearVelocity(new b2Vec2(0, rigidBody.b2RigidBody.GetLinearVelocity().y));
            if (elapsed < 20) {
                BattleCue.show('Shockwave Boss', 'Brace for the wave', 650);
            }
            if (elapsed > 520) {
                this.phase = 'charge';
                this.phaseStartedAt = Date.now();
                rigidBody.b2RigidBody.SetLinearVelocity(new b2Vec2(sign * 9, rigidBody.b2RigidBody.GetLinearVelocity().y));
                tryHitPlayer(this.enemy4.gameObject, 2, facingLeft ? 'leftattack' : 'rightattack');
                return;
            }
        } else if (this.phase === 'charge') {
            if (elapsed > 280) {
                this.phase = 'recover';
                this.phaseStartedAt = Date.now();
                rigidBody.b2RigidBody.SetLinearVelocity(new b2Vec2(0, rigidBody.b2RigidBody.GetLinearVelocity().y));
            }
        } else if (elapsed > 760) {
            this.phase = 'telegraph';
            this.phaseStartedAt = Date.now();
        }
    }
    exit() {}
    handleCollisionEnter() {}
}
