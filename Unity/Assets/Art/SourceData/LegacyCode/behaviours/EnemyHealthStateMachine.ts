import { Behaviour, GameObject } from "../../lib/mygameengine";
import { Enemy } from "./Enemy";
import { EnemyPrefabBinding } from "../bindings/EnemyPrefabBinding"; 

export class EnemyHealthStateMachine extends Behaviour {
    public currentHealth: number = 3; // 初始血量为3点
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

        // 播放死亡动画
        if (this.enemy) {
            const enemyPrefabBinding = this.enemy.getBehaviour(EnemyPrefabBinding);
            if (enemyPrefabBinding) {
                enemyPrefabBinding.action = 'leftdead'; // 假设这是播放死亡动画的方法
            }
        }

        setTimeout(() => {
        if (this.enemy) {
            this.enemy.active = false; 
            }
        }, 500);
    }
}
