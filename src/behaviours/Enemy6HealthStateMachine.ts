import { Behaviour, GameObject } from "../../lib/mygameengine";
import { Enemy6 } from "./Enemy6";
import { Enemy6PrefabBinding } from "../bindings/Enemy6PrefabBinding"; 

export class Enemy6HealthStateMachine extends Behaviour {
    public currentHealth: number = 3; // 初始血量为3点
    private maxHealth: number = 3; // 最大血量
    private enemy6: GameObject | null = null;

    onStart() {
        // 获取敌人对象
        this.enemy6 = this.gameObject;
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
        console.log("Enemy6 died");

        // 播放死亡动画
        if (this.enemy6) {
            const enemy6PrefabBinding = this.enemy6.getBehaviour(Enemy6PrefabBinding);
            if (enemy6PrefabBinding) {
                enemy6PrefabBinding.action = 'leftdead'; // 假设这是播放死亡动画的方法
            }
        }

        setTimeout(() => {
        if (this.enemy6) {
            this.enemy6.active = false; 
            }
        }, 500);
    }
}
