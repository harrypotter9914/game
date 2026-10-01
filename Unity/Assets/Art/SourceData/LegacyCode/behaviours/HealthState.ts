import { Behaviour, getGameObjectById, GameObject, BitmapRenderer, RigidBody, Transform } from "../../lib/mygameengine";
import { GameManager } from "./GameManager";
import { PlayerSession } from "./PlayerSession";

export class HealthStateMachine extends Behaviour {
    public currentHealth = 6;
    private blood: GameObject | null = null;
    private gameManager: GameObject | null = null;
    public isdead = false;

    onStart() {
        PlayerSession.ensureInitialized();
        this.blood = getGameObjectById('blood');
        this.gameManager = getGameObjectById('gameManager');
        this.currentHealth = PlayerSession.getHealth();
        this.isdead = false;
        this.updateHealthImage();
    }

    private getDisplayHealth(): number {
        return Math.max(0, Math.min(6, this.currentHealth));
    }

    updateHealthImage() {
        if (!this.blood) return;

        const bitmapRenderer = this.blood.getBehaviour(BitmapRenderer);
        if (!bitmapRenderer) {
            return;
        }

        const imageSource = `./assets/images/blood${this.getDisplayHealth()}.png`;
        bitmapRenderer.source = imageSource;
    }

    setHealth(health: number) {
        PlayerSession.setHealth(health);
        this.currentHealth = PlayerSession.getHealth();
        this.updateHealthImage();

        if (this.currentHealth <= 0) {
            this.isdead = true;
            this.handleDeath();
        }
    }

    increaseHealth(amount: number) {
        this.setHealth(this.currentHealth + amount);
    }

    decreaseHealth(amount: number) {
        this.setHealth(this.currentHealth - amount);
    }

    isHealthFull(): boolean {
        return this.currentHealth >= PlayerSession.getMaxHealth();
    }

    private reviveAtRespawn() {
        const player = getGameObjectById('mainRole');
        if (!player) {
            return;
        }

        const respawn = PlayerSession.getRespawnPoint();
        const rigid = player.getBehaviour(RigidBody);
        const transform = player.getBehaviour(Transform);
        if (rigid) {
            rigid.x = respawn.x;
            rigid.y = respawn.y;
            rigid.b2RigidBody.SetTransformXY(respawn.x, respawn.y, 0);
            rigid.b2RigidBody.SetLinearVelocity({ x: 0, y: 0 } as any);
        }
        if (transform) {
            transform.x = respawn.x;
            transform.y = respawn.y;
        }
    }

    handleDeath() {
        if (PlayerSession.tryRevive()) {
            this.isdead = false;
            this.currentHealth = PlayerSession.getHealth();
            this.updateHealthImage();
            this.reviveAtRespawn();
            return;
        }

        setTimeout(() => {
            if (this.gameManager) {
                this.gameManager.getBehaviour(GameManager).switchScene('diedmenu');
            }
        }, 500);
    }
}
