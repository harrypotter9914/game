import { Behaviour, GameObject } from "../../lib/mygameengine";
import { Enemy1 } from "./Enemy1";
import { Enemy1PrefabBinding } from "../bindings/Enemy1PrefabBinding"; 

export class Enemy1HealthStateMachine extends Behaviour {
    public currentHealth: number = 3; // 初始血量为3点
    private maxHealth: number = 3; // 最大血量
    private enemy1: GameObject | null = null;

    onStart() {
        // 获取敌人对象
        this.enemy1 = this.gameObject;
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
        console.log("Enemy1 died");

        // 播放死亡动画
        if (this.enemy1) {
            const enemy1PrefabBinding = this.enemy1.getBehaviour(Enemy1PrefabBinding);
            if (enemy1PrefabBinding) {
                enemy1PrefabBinding.action = 'leftdead'; // 假设这是播放死亡动画的方法
            }
        }

        setTimeout(() => {
        if (this.enemy1) {
            this.enemy1.active = false; 
            }
        }, 500);
    }
}
