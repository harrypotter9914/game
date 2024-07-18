import { Behaviour, GameObject } from "../../lib/mygameengine";
import { Enemy4 } from "./Enemy4";
import { Enemy4PrefabBinding } from "../bindings/Enemy4PrefabBinding"; 

export class Enemy4HealthStateMachine extends Behaviour {
    public currentHealth: number = 3; // 初始血量为3点
    private maxHealth: number = 3; // 最大血量
    private enemy4: GameObject | null = null;

    onStart() {
        // 获取敌人对象
        this.enemy4 = this.gameObject;
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
        console.log("Enemy4 died");

        // 播放死亡动画
        if (this.enemy4) {
            const enemy4PrefabBinding = this.enemy4.getBehaviour(Enemy4PrefabBinding);
            if (enemy4PrefabBinding) {
                enemy4PrefabBinding.action = 'leftdead'; // 假设这是播放死亡动画的方法
            }
        }

        setTimeout(() => {
        if (this.enemy4) {
            this.enemy4.active = false; 
            }
        }, 500);
    }
}
