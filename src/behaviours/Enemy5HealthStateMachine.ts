import { Behaviour, GameObject } from "../../lib/mygameengine";
import { Enemy5 } from "./Enemy5";
import { Enemy5PrefabBinding } from "../bindings/Enemy5PrefabBinding"; 

export class Enemy5HealthStateMachine extends Behaviour {
    public currentHealth: number = 3; // 初始血量为3点
    private maxHealth: number = 3; // 最大血量
    private enemy5: GameObject | null = null;

    onStart() {
        // 获取敌人对象
        this.enemy5 = this.gameObject;
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
        console.log("Enemy5 died");

        // 播放死亡动画
        if (this.enemy5) {
            const enemy5PrefabBinding = this.enemy5.getBehaviour(Enemy5PrefabBinding);
            if (enemy5PrefabBinding) {
                enemy5PrefabBinding.action = 'leftdead'; // 假设这是播放死亡动画的方法
            }
        }

        setTimeout(() => {
        if (this.enemy5) {
            this.enemy5.active = false; 
            }
        }, 500);
    }
}
