import { Behaviour, getGameObjectById, GameObject, Transform, BitmapRenderer } from "../../lib/mygameengine";
import { GameManager } from "./GameManager";

export class HealthStateMachine extends Behaviour {
    public currentHealth: number = 6; // 初始血量为6格
    private maxHealth: number = 6; // 最大血量
    private specialMaxHealth: number = 7; // 特殊情况下的最大血量
    private blood: GameObject | null = null;
    private gameManager: GameObject | null = null;

    onStart() {
        // 获取 blood 对象
        this.blood = getGameObjectById('blood');
        if (!this.blood) {
            console.error("Blood GameObject not found");
            return;
        }

        // 获取 GameManager 对象
        this.gameManager = getGameObjectById('gameManager');
        if (!this.gameManager) {
            console.error("GameManager GameObject not found");
            return;
        }

        // 初始化血量图片
        this.updateHealthImage();
    }

    updateHealthImage() {
        if (!this.blood) return;

        const bitmapRenderer = this.blood.getBehaviour(BitmapRenderer);
        if (!bitmapRenderer) {
            console.error("BitmapRenderer not found on Blood GameObject");
            return;
        }

        const imageSource = `./assets/images/blood${this.currentHealth}.png`;
        bitmapRenderer.source = imageSource;
        console.log(`Health updated: ${this.currentHealth}, Source: ${imageSource}`);
    }

    setHealth(health: number) {
        if (health < 0) {
            this.currentHealth = 0;
        } else if (health > this.maxHealth) {
            this.currentHealth = this.maxHealth;
        } else {
            this.currentHealth = health;
        }

        this.updateHealthImage();

        if (this.currentHealth <= 0) {
            this.handleDeath();
        }
    }

    increaseHealth(amount: number) {
        this.setHealth(this.currentHealth + amount);
    }

    decreaseHealth(amount: number) {
        this.setHealth(this.currentHealth - amount);
    }

    setSpecialMaxHealth() {
        this.maxHealth = this.specialMaxHealth;
    }

    isHealthFull(): boolean {
        return this.currentHealth >= this.maxHealth;
    }

    resetMaxHealth() {
        this.maxHealth = 6;
    }


    handleDeath() {
        console.log("Player died");
        if (this.gameManager) {
            this.gameManager.getBehaviour(GameManager).switchScene('mainmenu');
        }
    }
}