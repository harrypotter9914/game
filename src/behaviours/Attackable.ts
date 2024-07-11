import { Behaviour } from "../../lib/mygameengine";
import { MainRolePrefabBinding } from "../bindings/MainRolePrefabBinding";

export class Attackable extends Behaviour {
    private attackPower: number = 10;
    private attackSpeed: number = 1;
    public mainRoleBinding: MainRolePrefabBinding | null = null;

    onStart() {
        this.mainRoleBinding = this.gameObject.getBehaviour(MainRolePrefabBinding);
        document.addEventListener('keydown', this.handleKeyDown.bind(this));
    }

    onDestroy() {
        document.removeEventListener('keydown', this.handleKeyDown.bind(this));
    }

    handleKeyDown(event: KeyboardEvent) {
        if (event.code === 'KeyX') {
            this.attack();
        }
    }

    // 攻击方法
    attack() {
        // 播放攻击动画逻辑
        console.log("Attack with power:", this.attackPower);
        this.playAttackAnimation();
    }

    // 播放攻击动画方法
    playAttackAnimation() {
        if (this.mainRoleBinding) {
            this.mainRoleBinding.action = 'attack';
            setTimeout(() => {
                this.mainRoleBinding.action = 'idle'; // 攻击结束后恢复到idle状态
            }, 500 / this.attackSpeed); // 根据攻击速度调整动画持续时间
        }
        console.log("Playing attack animation.");
    }

    // 可以添加其他与攻击相关的方法，如设置攻击力和攻击速度
    setAttackPower(power: number) {
        this.attackPower = power;
    }

    setAttackSpeed(speed: number) {
        this.attackSpeed = speed;
    }
}
