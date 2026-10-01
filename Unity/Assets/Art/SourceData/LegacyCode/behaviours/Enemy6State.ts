import { Enemy6 } from "./Enemy6";
import { Collider, RigidBody, Transform } from "../../lib/mygameengine";
import { b2Vec2 } from "@flyover/box2d";
import { tryHitPlayer } from "./EnemyRuntime";
import { Enemy6HealthStateMachine } from "./Enemy6HealthStateMachine";
import { BattleCue } from "./BattleCue";

export abstract class Enemy6State {
    protected enemy6: Enemy6;
    constructor(enemy6: Enemy6) { this.enemy6 = enemy6; }
    abstract enter(): void;
    abstract update(duringTime: number): void;
    abstract exit(): void;
    abstract handleCollisionEnter(other: RigidBody, otherCollider: Collider, self: RigidBody, selfCollider: Collider): void;
}

export class HurtState extends Enemy6State {
    private previousState: Enemy6State;
    constructor(enemy6: Enemy6, previousState: Enemy6State) { super(enemy6); this.previousState = previousState; }
    enter() { setTimeout(() => this.enemy6.changeState(this.previousState), 220); }
    update() {}
    exit() {}
    handleCollisionEnter() {}
}

export class PatrolState extends Enemy6State {
    enter() { this.enemy6.enemy6Binding!.action = 'rightpatrol'; }
    update() {
        const transform = this.enemy6.gameObject.getBehaviour(Transform);
        const playerTransform = this.enemy6.getPlayerTransform();
        if (!transform || !playerTransform) return;
        if (Math.abs(playerTransform.x - transform.x) < 1200) this.enemy6.changeState(new ChaseState(this.enemy6));
    }
    exit() {}
    handleCollisionEnter() {}
}

export class ChaseState extends Enemy6State {
    enter() {}
    update() {
        const transform = this.enemy6.gameObject.getBehaviour(Transform);
        const rigidBody = this.enemy6.gameObject.getBehaviour(RigidBody);
        const playerTransform = this.enemy6.getPlayerTransform();
        const health = this.enemy6.gameObject.getBehaviour(Enemy6HealthStateMachine);
        if (!playerTransform || !transform || !rigidBody || !health) return;
        const phaseTwo = health.currentHealth <= 9;
        const dx = playerTransform.x - transform.x;
        const direction = dx > 0 ? 1 : -1;
        const speed = phaseTwo ? this.enemy6.chaseSpeed * 1.35 : this.enemy6.chaseSpeed;
        rigidBody.b2RigidBody.SetLinearVelocity(new b2Vec2(direction * speed, rigidBody.b2RigidBody.GetLinearVelocity().y));
        this.enemy6.enemy6Binding!.action = direction > 0 ? 'rightrun' : 'leftrun';
        if (Math.abs(dx) < (phaseTwo ? 660 : 460)) this.enemy6.changeState(new AttackState(this.enemy6));
    }
    exit() {}
    handleCollisionEnter() {}
}

export class AttackState extends Enemy6State {
    private phase: 'telegraph' | 'burst' | 'recover' = 'telegraph';
    private phaseStartedAt = 0;
    private announcedPhaseTwo = false;
    enter() { this.phase = 'telegraph'; this.phaseStartedAt = Date.now(); }
    update() {
        const transform = this.enemy6.gameObject.getBehaviour(Transform);
        const rigidBody = this.enemy6.gameObject.getBehaviour(RigidBody);
        const playerTransform = this.enemy6.getPlayerTransform();
        const health = this.enemy6.gameObject.getBehaviour(Enemy6HealthStateMachine);
        if (!playerTransform || !transform || !rigidBody || !health) return;

        const phaseTwo = health.currentHealth <= 9;
        const dx = playerTransform.x - transform.x;
        const distance = Math.abs(dx);
        const facingLeft = dx < 0;
        const sign = facingLeft ? -1 : 1;
        this.enemy6.enemy6Binding!.action = facingLeft ? 'leftattack' : 'rightattack';

        if (phaseTwo && !this.announcedPhaseTwo) {
            this.announcedPhaseTwo = true;
            BattleCue.show('Final Nero', 'Phase two awakened', 1200);
        }

        if (distance > (phaseTwo ? 860 : 620)) {
            this.enemy6.changeState(new ChaseState(this.enemy6));
            return;
        }

        const elapsed = Date.now() - this.phaseStartedAt;
        if (this.phase === 'telegraph') {
            rigidBody.b2RigidBody.SetLinearVelocity(new b2Vec2(0, rigidBody.b2RigidBody.GetLinearVelocity().y));
            if (elapsed < 20) {
                BattleCue.show('Final Nero', phaseTwo ? 'Dash barrage' : 'Crimson strike', phaseTwo ? 700 : 560);
            }
            if (elapsed > (phaseTwo ? 360 : 460)) {
                this.phase = 'burst';
                this.phaseStartedAt = Date.now();
                rigidBody.b2RigidBody.SetLinearVelocity(new b2Vec2(sign * (phaseTwo ? 11 : 7), rigidBody.b2RigidBody.GetLinearVelocity().y));
                tryHitPlayer(this.enemy6.gameObject, phaseTwo ? 2 : 1, facingLeft ? 'leftattack' : 'rightattack');
                return;
            }
        } else if (this.phase === 'burst') {
            if (phaseTwo && elapsed > 150 && elapsed < 210) {
                tryHitPlayer(this.enemy6.gameObject, 1, facingLeft ? 'leftattack' : 'rightattack');
            }
            if (elapsed > (phaseTwo ? 260 : 190)) {
                this.phase = 'recover';
                this.phaseStartedAt = Date.now();
                rigidBody.b2RigidBody.SetLinearVelocity(new b2Vec2(0, rigidBody.b2RigidBody.GetLinearVelocity().y));
            }
        } else if (elapsed > (phaseTwo ? 620 : 860)) {
            this.phase = 'telegraph';
            this.phaseStartedAt = Date.now();
        }
    }
    exit() {}
    handleCollisionEnter() {}
}
