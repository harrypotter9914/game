import { Behaviour, number, RigidBody, Transform, Collider, getGameObjectById } from "../../lib/mygameengine";
import { MainRolePrefabBinding } from "../bindings/MainRolePrefabBinding";
import { b2Vec2 } from "@flyover/box2d";
import { State, GroundState, HurtState } from "../behaviours/State";
import { Attackable } from "./Attackable";
import { HealthStateMachine } from "./HealthState";
import { ManaStateMachine } from "./ManaState";
import { AudioBehaviour } from "../../lib/mygameengine";
import { StarCollisionHandler } from "./StarCollisionHandler";
import { resolveEnemyAction, damageEnemy } from "./EnemyRuntime";
import { PlayerSession } from "./PlayerSession";
import { RuneMenuBehaviour } from "./RuneMenuBehaviour";
import { EnemyRegistry } from "./EnemyRegistry";

export class Walkable extends Behaviour {
    @number()
    speed = 1;

    @number()
    jumpForce = 5;

    @number()
    wallJumpForce = 5;

    @number()
    wallSlideSpeed = 2;

    private attackable: Attackable;
    public isGrounded = false;
    public isOnWall = false;
    public airJumped = false;
    public mainRoleBinding: MainRolePrefabBinding | null = null;
    public lastAction: string = 'rightidle';
    public coyoteTime = 0.1;
    public coyoteTimer = 0;
    public isMoving = false;
    public currentState: State;
    public initialJump = true;
    private cameraTransform: Transform | null = null;
    private playerTransform: Transform | null = null;
    public leftArrowPressed = false;
    public rightArrowPressed = false;
    public upArrowPressed = false;
    public downArrowPressed = false;
    public lastEnemyAction: string = 'rightattack';
    public currentAction: string = 'idle';
    private hurtCooldown = false;
    private aKeyPressedStartTime = 0;
    private isAKeyPressed = false;
    private jumpAudio: AudioBehaviour | null = null;
    private bgmusic1: AudioBehaviour | null = null;
    public startag = '';
    public offsetX = 550;
    public offsetY = 150;
    private groundContactCount = 0;
    private wallContactCount = 0;
    private pendingDamage = 1;
    private pendingEnemySource: any = null;
    private dashCooldownUntil = 0;
    private invulnerableUntil = 0;
    private readonly handleKeyDownBound = this.handleKeyDown.bind(this);
    private readonly handleKeyUpBound = this.handleKeyUp.bind(this);

    constructor() {
        super();
        this.currentState = new GroundState(this);

        this.jumpAudio = new AudioBehaviour();
        this.jumpAudio.source = "./assets/audio/12_human_jump_1.wav";
        this.jumpAudio.setLoop(false);
        this.jumpAudio.setVolume(1);

        this.bgmusic1 = new AudioBehaviour();
        this.bgmusic1.source = "./assets/audio/tower.wav";
        this.bgmusic1.setLoop(true);
        this.bgmusic1.setVolume(0.5);
    }

    changeState(newState: State) {
        this.currentState.exit();
        this.currentState = newState;
        this.currentState.enter();
    }

    onStart() {
        PlayerSession.ensureInitialized();

        this.mainRoleBinding = this.gameObject.getBehaviour(MainRolePrefabBinding);
        if (this.mainRoleBinding) {
            this.mainRoleBinding.action = this.lastAction;
        }

        document.removeEventListener('keydown', this.handleKeyDownBound);
        document.removeEventListener('keyup', this.handleKeyUpBound);
        document.addEventListener('keydown', this.handleKeyDownBound);
        document.addEventListener('keyup', this.handleKeyUpBound);

        const rigidBody = this.gameObject.getBehaviour(RigidBody);
        if (rigidBody) {
            rigidBody.onCollisionEnter = this.handleCollisionEnter.bind(this);
            rigidBody.onCollisionExit = this.handleCollisionExit.bind(this);
        }

        const cameraObject = getGameObjectById('camera');
        if (cameraObject) {
            this.cameraTransform = cameraObject.getBehaviour(Transform);
        }

        const playerObject = getGameObjectById('mainRole');
        if (playerObject) {
            this.playerTransform = playerObject.getBehaviour(Transform);
        }

        const existingAttackable = this.gameObject.getBehaviour(Attackable);
        this.attackable = existingAttackable || new Attackable(this);
        if (!existingAttackable) {
            this.gameObject.addBehaviour(this.attackable);
        }

        if (!this.gameObject.getBehaviour(HealthStateMachine)) {
            this.gameObject.addBehaviour(new HealthStateMachine());
        }
        if (!this.gameObject.getBehaviour(ManaStateMachine)) {
            this.gameObject.addBehaviour(new ManaStateMachine());
        }
        if (!this.gameObject.getBehaviour(StarCollisionHandler)) {
            this.gameObject.addBehaviour(new StarCollisionHandler());
        }
        if (!this.gameObject.getBehaviour(RuneMenuBehaviour)) {
            this.gameObject.addBehaviour(new RuneMenuBehaviour());
        }

        if (this.gameObject.active && this.bgmusic1) {
            this.bgmusic1.play();
        }

        if (this.cameraTransform) {
            this.cameraTransform.scaleX = 0.6;
            this.cameraTransform.scaleY = 0.6;
        }
    }

    getLastAttackDirection(): string {
        return this.attackable?.lastAttackDirection || 'right';
    }

    queueIncomingHit(amount: number, enemyAction: string, source?: any) {
        if (PlayerSession.isPaused()) {
            return;
        }

        if (Date.now() < this.invulnerableUntil || this.hurtCooldown) {
            return;
        }

        this.pendingDamage = amount;
        this.lastEnemyAction = enemyAction || 'rightattack';
        this.pendingEnemySource = source || null;

        if (PlayerSession.hasDamageReflect() && source) {
            damageEnemy(source, 1, this.lastEnemyAction.includes('left') ? 'right' : 'left');
        }

        this.changeState(new HurtState(this));
        this.hurtCooldown = true;
        this.invulnerableUntil = Date.now() + 800;
        setTimeout(() => {
            this.hurtCooldown = false;
        }, 800);
    }

    consumePendingDamage(): number {
        const damage = this.pendingDamage;
        this.pendingDamage = 1;
        return damage;
    }

    handleKeyDown(event: KeyboardEvent) {
        if (PlayerSession.isPaused()) {
            return;
        }

        this.currentState.handleInput(event);

        if (event.key === 'a' && !this.isAKeyPressed) {
            this.aKeyPressedStartTime = Date.now();
            this.isAKeyPressed = true;
            if (this.lastAction.includes('left')) {
                this.mainRoleBinding!.action = 'leftheal';
            } else {
                this.mainRoleBinding!.action = 'rightheal';
            }
        }

        if (event.code === 'KeyZ') {
            this.tryCrystalDash();
        }
    }

    handleKeyUp(event: KeyboardEvent) {
        if (!PlayerSession.isPaused()) {
            this.currentState.handleKeyUp(event);
        }

        if (event.key === 'a' && this.isAKeyPressed) {
            const aKeyPressDuration = Date.now() - this.aKeyPressedStartTime;
            if (aKeyPressDuration >= 1500) {
                const manaStateMachine = this.gameObject.getBehaviour(ManaStateMachine);
                if (manaStateMachine) {
                    manaStateMachine.decreaseMana(1);
                }
            }
            this.isAKeyPressed = false;
        }
    }

    private tryCrystalDash() {
        if (!PlayerSession.hasAbility('crystalDash')) {
            return;
        }

        if (Date.now() < this.dashCooldownUntil) {
            return;
        }

        const rigid = this.gameObject.getBehaviour(RigidBody);
        if (!rigid) {
            return;
        }

        const direction = this.lastAction.includes('left') ? -1 : 1;
        rigid.b2RigidBody.SetLinearVelocity(new b2Vec2(direction * 12, rigid.b2RigidBody.GetLinearVelocity().y));
        this.invulnerableUntil = Date.now() + 250;
        this.dashCooldownUntil = Date.now() + 1200;

        const playerTransform = this.gameObject.getBehaviour(Transform);
        if (!playerTransform) {
            return;
        }

        for (const enemy of EnemyRegistry.getAll()) {
            const enemyTransform = enemy.getBehaviour(Transform);
            if (!enemyTransform) {
                continue;
            }

            const dx = enemyTransform.x - playerTransform.x;
            const dy = Math.abs(enemyTransform.y - playerTransform.y);
            if (Math.sign(dx) === direction && Math.abs(dx) < 260 && dy < 120) {
                damageEnemy(enemy, 2, direction < 0 ? 'left' : 'right');
            }
        }
    }

    handleCollisionEnter(other: RigidBody, otherCollider: Collider, self: RigidBody, selfCollider: Collider) {
        if (selfCollider.tag === 'feet' && otherCollider.tag === 'block') {
            this.groundContactCount++;
            if (this.groundContactCount > 0) {
                this.isGrounded = true;
                this.airJumped = false;
            }
        }
        if (selfCollider.tag === 'body' && otherCollider.tag === 'block') {
            this.wallContactCount++;
            if (this.wallContactCount > 0) {
                this.isOnWall = true;
            }
        }

        if ((selfCollider.tag === 'feet' || selfCollider.tag === 'body') && otherCollider.tag.includes('hoxi')) {
            const starCollisionHandler = this.gameObject.getBehaviour(StarCollisionHandler);
            if (starCollisionHandler) {
                starCollisionHandler.setInteractingStar(other.gameObject, otherCollider.tag);
                starCollisionHandler.tryCollectCurrentStar();
                this.startag = otherCollider.tag;
            }
        }

        if ((selfCollider.tag === 'body' || selfCollider.tag === 'feet') && (otherCollider.tag === 'enemybody' || otherCollider.tag === 'enemyfeet')) {
            this.queueIncomingHit(1, resolveEnemyAction(other.gameObject), other.gameObject);
        }
    }

    handleCollisionExit(other: RigidBody, otherCollider: Collider, self: RigidBody, selfCollider: Collider) {
        if (selfCollider.tag === 'feet' && otherCollider.tag === 'block') {
            this.groundContactCount--;
            if (this.groundContactCount <= 0) {
                this.groundContactCount = 0;
                this.isGrounded = false;
            }
        }
        if (selfCollider.tag === 'body' && otherCollider.tag === 'block') {
            this.wallContactCount--;
            if (this.wallContactCount <= 0) {
                this.wallContactCount = 0;
                this.isOnWall = false;
            }
        }
        if ((selfCollider.tag === 'feet' || selfCollider.tag === 'body') && otherCollider.tag.includes('hoxi')) {
            const starCollisionHandler = this.gameObject.getBehaviour(StarCollisionHandler);
            if (starCollisionHandler) {
                starCollisionHandler.setInteractingStar(null, '');
            }
        }
    }

    jump(rigid: RigidBody, multiplier: number = 1) {
        if (this.jumpAudio) {
            this.jumpAudio.play();
        }
        rigid.b2RigidBody.SetLinearVelocity(new b2Vec2(rigid.b2RigidBody.GetLinearVelocity().x, this.jumpForce * multiplier));
    }

    wallJump(rigid: RigidBody) {
        let direction = 0;

        if (this.leftArrowPressed) {
            direction = 1;
        } else if (this.rightArrowPressed) {
            direction = -1;
        }

        const horizontalSpeed = this.wallJumpForce * direction;
        const verticalSpeed = this.jumpForce * 0.7;
        if (this.jumpAudio) {
            this.jumpAudio.play();
        }
        rigid.b2RigidBody.SetLinearVelocity(new b2Vec2(horizontalSpeed, verticalSpeed));
    }

    onTick(duringTime: number) {
        if (PlayerSession.isPaused()) {
            return;
        }

        this.currentState.update(duringTime);

        if (this.gameObject.active === false) {
            this.bgmusic1?.stop();
            if (this.cameraTransform) {
                this.cameraTransform.scaleX = 1;
                this.cameraTransform.scaleY = 1;
            }
            return;
        }

        const rigid = this.gameObject.getBehaviour(RigidBody);
        if (!rigid) {
            return;
        }

        let velocity = rigid.b2RigidBody.GetLinearVelocity();

        if (this.isMoving) {
            if (this.lastAction === 'leftrun') {
                velocity = new b2Vec2(-this.speed, velocity.y);
            } else if (this.lastAction === 'rightrun') {
                velocity = new b2Vec2(this.speed, velocity.y);
            }
        } else {
            velocity = new b2Vec2(velocity.x * 0.5, velocity.y);
        }

        rigid.b2RigidBody.SetLinearVelocity(velocity);

        if (this.cameraTransform && this.playerTransform) {
            this.cameraTransform.x = this.playerTransform.x + this.offsetX;
            this.cameraTransform.y = this.playerTransform.y + this.offsetY;
        }
    }

    onEnd() {
        document.removeEventListener('keydown', this.handleKeyDownBound);
        document.removeEventListener('keyup', this.handleKeyUpBound);
    }
}
