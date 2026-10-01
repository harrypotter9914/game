import { Behaviour, RigidBody, Transform, getGameObjectById } from "../../lib/mygameengine";
import { MainRolePrefabBinding } from "../bindings/MainRolePrefabBinding";
import { Walkable } from "./Walkable";
import { AirState, WallState, DoubleJumpedState } from "./State";
import { MaoQiPrefabBinding } from "../bindings/MaoQiPrefabBinding";
import { AudioBehaviour } from "../../lib/mygameengine";
import { EnemyRegistry } from "./EnemyRegistry";
import { damageEnemy } from "./EnemyRuntime";
import { PlayerSession } from "./PlayerSession";

export class Attackable extends Behaviour {
    private attackPower = 1;
    private attackSpeed = 1;
    public mainRoleBinding: MainRolePrefabBinding | null = null;
    private walkable: Walkable | null = null;
    private maoQiInstance: MaoQiPrefabBinding | null = null;
    public lastAttackDirection: string = 'right';
    private attackAudio: AudioBehaviour | null = null;
    private readonly handleKeyDownBound = this.handleKeyDown.bind(this);
    private attackLockUntil = 0;
    private shockwaveCooldownUntil = 0;

    constructor(walkable?: Walkable) {
        super();
        this.walkable = walkable ?? null;
        this.attackAudio = new AudioBehaviour();
        this.attackAudio.source = "./assets/audio/07_human_atk_sword_2.wav";
        this.attackAudio.setLoop(false);
        this.attackAudio.setVolume(1);
    }

    onStart() {
        this.mainRoleBinding = this.gameObject.getBehaviour(MainRolePrefabBinding);
        this.walkable = this.walkable ?? this.gameObject.getBehaviour(Walkable);
        const maoQi = getGameObjectById('maoQi');
        this.maoQiInstance = maoQi ? maoQi.getBehaviour(MaoQiPrefabBinding) : null;
        document.removeEventListener('keydown', this.handleKeyDownBound);
        document.addEventListener('keydown', this.handleKeyDownBound);
    }

    onEnd() {
        document.removeEventListener('keydown', this.handleKeyDownBound);
    }

    handleKeyDown(event: KeyboardEvent) {
        if (PlayerSession.isPaused()) {
            return;
        }

        if (Date.now() < this.attackLockUntil) {
            return;
        }

        if (event.code === 'KeyX') {
            this.performMeleeAttack();
        }

        if (event.code === 'KeyV') {
            this.performShockwave();
        }
    }

    private performMeleeAttack() {
        const walkable = this.walkable ?? this.gameObject.getBehaviour(Walkable);
        if (!walkable) {
            return;
        }

        const upPressed = !!walkable.upArrowPressed;
        const downPressed = !!walkable.downArrowPressed;

        if (walkable.currentState instanceof WallState) {
            return;
        }

        let action: string;
        if (downPressed && (walkable.currentState instanceof AirState || walkable.currentState instanceof DoubleJumpedState)) {
            action = walkable.lastAction.includes('left') ? 'leftdownattack' : 'rightdownattack';
        } else if (upPressed) {
            action = walkable.lastAction.includes('left') ? 'leftupattack' : 'rightupattack';
        } else {
            action = walkable.lastAction.includes('left') ? 'leftattack' : 'rightattack';
        }

        this.lastAttackDirection = walkable.lastAction.includes('left') ? 'left' : 'right';
        this.attack(action, 120 * PlayerSession.getAttackRangeMultiplier(), 1, walkable);
    }

    private performShockwave() {
        const walkable = this.walkable ?? this.gameObject.getBehaviour(Walkable);
        if (!walkable || !PlayerSession.hasAbility('shockwave')) {
            return;
        }

        if (Date.now() < this.shockwaveCooldownUntil) {
            return;
        }

        if (!PlayerSession.tryConsumeMana(1)) {
            return;
        }

        const direction = walkable.lastAction.includes('left') ? 'left' : 'right';
        this.lastAttackDirection = direction;
        this.attack(direction === 'left' ? 'leftattack' : 'rightattack', 320, 2, walkable, true);
        this.shockwaveCooldownUntil = Date.now() + 1600;
    }

    private attack(action: string, range: number, baseDamage: number, walkable: Walkable, isShockwave: boolean = false) {
        if (!this.mainRoleBinding) {
            return;
        }

        this.attackSpeed = PlayerSession.getAttackSpeedMultiplier();
        const actualDamage = Math.max(1, Math.round(baseDamage * PlayerSession.getAttackPowerMultiplier()));

        this.mainRoleBinding.action = action;
        this.movePrefab(action, isShockwave);
        this.attackAudio?.play();
        this.hitEnemies(range, actualDamage);

        const recoverDelay = Math.round(500 / this.attackSpeed);
        this.attackLockUntil = Date.now() + recoverDelay;
        setTimeout(() => {
            if (this.mainRoleBinding) {
                this.mainRoleBinding.action = walkable.lastAction.includes('left') ? 'leftidle' : 'rightidle';
            }
        }, recoverDelay);
    }

    private hitEnemies(range: number, damage: number) {
        const playerObject = getGameObjectById('mainRole');
        if (!playerObject) {
            return;
        }

        const transform = playerObject.getBehaviour(Transform);
        if (!transform) {
            return;
        }

        const attackDirection = this.lastAttackDirection;
        for (const enemy of EnemyRegistry.getAll()) {
            const enemyTransform = enemy.getBehaviour(Transform);
            if (!enemyTransform) {
                continue;
            }

            const dx = enemyTransform.x - transform.x;
            const dy = Math.abs(enemyTransform.y - transform.y);
            const inFront = attackDirection === 'left' ? dx < 0 : dx > 0;
            if (!inFront || Math.abs(dx) > range || dy > 140) {
                continue;
            }

            damageEnemy(enemy, damage, attackDirection);
        }
    }

    movePrefab(action: string, isShockwave: boolean) {
        if (!this.maoQiInstance) {
            return;
        }

        const playerObject = getGameObjectById('mainRole');
        if (!playerObject) {
            return;
        }

        const transform = playerObject.getBehaviour(Transform);
        if (!transform) {
            return;
        }

        let offsetX = 0;
        let offsetY = 0;
        let rotation = 0;
        let scaleX = isShockwave ? 2.2 : 1;
        let scaleY = isShockwave ? 1.8 : 1;

        if (action.includes('leftattack')) {
            offsetX = isShockwave ? -150 : -30;
            offsetY = -20;
            scaleX *= -1;
        } else if (action.includes('rightattack')) {
            offsetX = isShockwave ? 170 : 110;
            offsetY = -20;
        } else if (action.includes('leftupattack')) {
            offsetX = 30;
            offsetY = -80;
            rotation = -90;
            scaleY *= -1;
        } else if (action.includes('rightupattack')) {
            offsetX = 50;
            offsetY = -80;
            rotation = -90;
        } else if (action.includes('leftdownattack')) {
            offsetX = 30;
            offsetY = 60;
            rotation = 90;
            scaleY *= -1;
        } else if (action.includes('rightdownattack')) {
            offsetX = 50;
            offsetY = 70;
            rotation = 90;
        }

        this.maoQiInstance.x = transform.x + offsetX;
        this.maoQiInstance.y = transform.y + offsetY;
        this.maoQiInstance.rotation = rotation;
        this.maoQiInstance.scaleX = scaleX;
        this.maoQiInstance.scaleY = scaleY;

        setTimeout(() => {
            if (!this.maoQiInstance) {
                return;
            }
            this.maoQiInstance.x = -1000;
            this.maoQiInstance.y = -1000;
            this.maoQiInstance.rotation = 0;
            this.maoQiInstance.scaleX = 1;
            this.maoQiInstance.scaleY = 1;
        }, isShockwave ? 120 : 60);
    }

    onTick() {
        if (this.gameObject.active === false) {
            this.attackAudio?.stop();
        }
    }
}
