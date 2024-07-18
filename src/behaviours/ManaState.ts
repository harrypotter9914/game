// ManaStateMachine.ts
import { Behaviour, getGameObjectById, GameObject, BitmapRenderer } from "../../lib/mygameengine";
import { HealthStateMachine } from "./HealthState";

export class ManaStateMachine extends Behaviour {
    private currentMana: number = 3; // 初始蓝量为3格
    private maxMana: number = 3; // 最大蓝量
    private blue: GameObject | null = null;
    private healthStateMachine: HealthStateMachine | null = null;
    private isDecreasingMana: boolean = false;

    onStart() {
        // 获取 blue 对象
        this.blue = getGameObjectById('blue');
        if (!this.blue) {
            console.error("Blue GameObject not found");
            return;
        }

        // 获取 HealthStateMachine
        const player = getGameObjectById('mainRole');
        if (player) {
            this.healthStateMachine = player.getBehaviour(HealthStateMachine);
        } else {
            console.error("Player GameObject not found");
        }

        // 初始化蓝量图片
        this.updateManaImage();
    }

    updateManaImage() {
        if (!this.blue) return;

        const bitmapRenderer = this.blue.getBehaviour(BitmapRenderer);
        if (!bitmapRenderer) {
            console.error("BitmapRenderer not found on Blue GameObject");
            return;
        }

        const imageSource = `./assets/images/blue${this.currentMana}.png`;
        bitmapRenderer.source = imageSource;
        console.log(`Mana updated: ${this.currentMana}, Source: ${imageSource}`);
    }

    setMana(mana: number) {
        if (mana < 0) {
            this.currentMana = 0;
        } else if (mana > this.maxMana) {
            this.currentMana = this.maxMana;
        } else {
            this.currentMana = mana;
        }

        this.updateManaImage();
    }

    increaseMana(amount: number) {
        this.setMana(this.currentMana + amount);
    }

    decreaseMana(amount: number) {
        if (this.isDecreasingMana || this.currentMana <= 0 || this.healthStateMachine?.isHealthFull()) return;

        this.isDecreasingMana = true;
        setTimeout(() => {
            if (this.currentMana > 0 && this.healthStateMachine && !this.healthStateMachine.isHealthFull()) {
                this.healthStateMachine.increaseHealth(1);
            }
            this.setMana(this.currentMana - amount);
            this.isDecreasingMana = false;
        }, 1500); // 长按 1.5 秒
    }
}
