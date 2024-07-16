import { Behaviour } from "../../lib/mygameengine";
import { MainRolePrefabBinding } from "../bindings/MainRolePrefabBinding";
import { Walkable } from "./Walkable";
import { AirState, GroundState, WallState } from "./State";

export class Attackable extends Behaviour {
    private attackPower: number = 10;
    private attackSpeed: number = 1;
    public mainRoleBinding: MainRolePrefabBinding | null = null;
    private walkable: Walkable;

    constructor(walkable: Walkable) {
        super();
        this.walkable = walkable;
    }

    onStart() {
        this.mainRoleBinding = this.gameObject.getBehaviour(MainRolePrefabBinding);
        document.addEventListener('keydown', this.handleKeyDown.bind(this));
    }

    onDestroy() {
        document.removeEventListener('keydown', this.handleKeyDown.bind(this));
    }

    handleKeyDown(event: KeyboardEvent) {
        if (event.code === 'KeyX') {
            const upPressed = this.walkable.upArrowPressed;
            const downPressed = this.walkable.downArrowPressed;

            if (this.walkable.currentState instanceof WallState) {
                // 墙上状态不能攻击
                return;
            }

            if (downPressed && this.walkable.currentState instanceof AirState) {
                // 空中下攻击
                this.attack(this.walkable.lastAction.includes('left') ? 'leftdownattack' : 'rightdownattack');
            } else if (upPressed) {
                // 上攻击
                this.attack(this.walkable.lastAction.includes('left') ? 'leftupattack' : 'rightupattack');
            } else {
                // 普通攻击
                this.attack(this.walkable.lastAction.includes('left') ? 'leftattack' : 'rightattack');
            }
        }
    }

    attack(action: string) {
        if (this.mainRoleBinding) {
            this.mainRoleBinding.action = action;
            setTimeout(() => {
                this.mainRoleBinding.action = this.walkable.lastAction.includes('left') ? 'leftidle' : 'rightidle'; // 攻击结束后恢复到idle状态
            }, 500 / this.attackSpeed); // 根据攻击速度调整动画持续时间
        }
        console.log(`Playing ${action} animation with power: ${this.attackPower}`);
    }

    setAttackPower(power: number) {
        this.attackPower = power;
    }

    setAttackSpeed(speed: number) {
        this.attackSpeed = speed;
    }
}
