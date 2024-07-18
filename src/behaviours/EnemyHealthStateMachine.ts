import { Behaviour, GameObject } from "../../lib/mygameengine";

export class EnemyHealthStateMachine extends Behaviour {
    private currentHealth: number = 3; // 初始血量为3点
    private maxHealth: number = 3; // 最大血量
    private enemy: GameObject | null = null;

    onStart() {
        // 获取敌人对象
        this.enemy = this.gameObject;
    }

    setHealth(health: number) {
        if (health < 0) {
            this.currentHealth = 0;
        } else if (health > this.maxHealth) {
            this.currentHealth = this.maxHealth;
        } else {
            this.currentHealth = health;
        }

        if (this.currentHealth <= 0) {
            this.handleDeath();
        }
    }

    decreaseHealth(amount: number) {
        this.setHealth(this.currentHealth - amount);
    }

    handleDeath() {
        console.log("Enemy died");
        // 处理敌人死亡，例如播放死亡动画，移除敌人等
        if (this.enemy) {
            this.enemy.active = false; // 简单的示例，设置为不活跃
        }
    }
}
