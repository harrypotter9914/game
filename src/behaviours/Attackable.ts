import { Behaviour, Collider, RigidBody, Transform } from "../../lib/mygameengine";
import { MainRolePrefabBinding } from "../bindings/MainRolePrefabBinding";
import { Walkable } from "./Walkable";
import { AirState, WallState, DoubleJumpedState } from "./State";
import { getGameObjectById } from "../../lib/mygameengine";
import { MaoQiPrefabBinding } from "../bindings/MaoQiPrefabBinding";
import { AudioBehaviour, AudioSystem } from "../../lib/mygameengine";

export class Attackable extends Behaviour {
    private attackPower: number = 10;
    private attackSpeed: number = 1;
    public mainRoleBinding: MainRolePrefabBinding | null = null;
    private walkable: Walkable;
    private maoQiInstance: MaoQiPrefabBinding | null = null;
    public lastAttackDirection: string = 'right'; // 记录最后的攻击方向
    private attackAudio: AudioBehaviour | null = null;

    constructor(walkable: Walkable) {
        super();
        this.walkable = walkable;
        // 初始化音频行为
        this.attackAudio = new AudioBehaviour();
        this.attackAudio.source = "./assets/audio/07_human_atk_sword_2.wav"; 
        this.attackAudio.setLoop(false); // 设置不循环播放
        this.attackAudio.setVolume(1);
    }

    onStart() {
        this.mainRoleBinding = this.gameObject.getBehaviour(MainRolePrefabBinding);
        this.maoQiInstance = getGameObjectById('maoQi').getBehaviour(MaoQiPrefabBinding);
        document.addEventListener('keydown', this.handleKeyDown.bind(this));
    }

    onDestroy() {
        document.removeEventListener('keydown', this.handleKeyDown.bind(this));
    }

    handleKeyDown(event: KeyboardEvent) {
        if (event.code === 'KeyX') {
            const upPressed = this.walkable.upArrowPressed;
            const downPressed = this.walkable.downArrowPressed;
            const rigid = this.walkable.gameObject.getBehaviour(RigidBody);

            if (this.walkable.currentState instanceof WallState) {
                // 墙上状态不能攻击
                return;
            }

            if (downPressed && (this.walkable.currentState instanceof AirState || this.walkable.currentState instanceof DoubleJumpedState)) {
                // 空中下攻击
                this.attack(this.walkable.lastAction.includes('left') ? 'leftdownattack' : 'rightdownattack');
                this.lastAttackDirection = this.walkable.lastAction.includes('left') ? 'left' : 'right'; // 更新最后攻击方向
            } else if (upPressed) {
                // 上攻击
                this.attack(this.walkable.lastAction.includes('left') ? 'leftupattack' : 'rightupattack');
                this.lastAttackDirection = this.walkable.lastAction.includes('left') ? 'left' : 'right'; // 更新最后攻击方向
            } else {
                // 普通攻击
                this.attack(this.walkable.lastAction.includes('left') ? 'leftattack' : 'rightattack');
                this.lastAttackDirection = this.walkable.lastAction.includes('left') ? 'left' : 'right'; // 更新最后攻击方向
            }
        }
    }

    attack(action: string) {
        if (this.mainRoleBinding) {
            this.mainRoleBinding.action = action;
            this.movePrefab(action); // 移动剑气预制体
            if (this.attackAudio) {
                this.attackAudio.play();
            }
            setTimeout(() => {
                this.mainRoleBinding.action = this.walkable.lastAction.includes('left') ? 'leftidle' : 'rightidle'; // 攻击结束后恢复到idle状态
            }, 500 / this.attackSpeed); // 根据攻击速度调整动画持续时间
        }
    }


    movePrefab(action: string) {
        if (!this.maoQiInstance) {
            console.error("MaoQi instance not found.");
            return;
        }

        // 实时获取主角的Transform
        const playerObject = getGameObjectById('mainRole');
        if (!playerObject) {
            console.error("Main role object not found.");
            return;
        }

        const transform = playerObject.getBehaviour(Transform);
        if (!transform) {
            console.error("Main role Transform not found.");
            return;
        }


        let offsetX = 0;
        let offsetY = 0;
        let rotation = 0; // 剑气的旋转角度
        let scaleX = 1; // 剑气的水平缩放比例
        let scaleY = 1; // 剑气的垂直缩放比例

        // 为每个方向设置不同的偏移量、旋转角度和缩放比例
        if (action.includes('leftattack')) {
            offsetX = -30; // 左方向的偏移量
            offsetY = -20;
            rotation = 0; // 左方向的旋转角度
            scaleX = -1; // 左右镜像
            scaleY = 1; // 垂直缩放正常
        } else if (action.includes('rightattack')) {
            offsetX = 110; // 右方向的偏移量
            offsetY = -20;
            rotation = 0; // 右方向的旋转角度
            scaleX = 1; // 正常方向
            scaleY = 1; // 垂直缩放正常
        } else if (action.includes('leftupattack')) {
            offsetX = 30;
            offsetY = -80; // 上方向的偏移量
            rotation = -90; // 上方向的旋转角度
            scaleX = 1; // 正常方向
            scaleY = -1; // 上下镜像
        } else if (action.includes('rightupattack')) {
            offsetX = 50;
            offsetY = -80; // 上方向的偏移量
            rotation = -90; // 上方向的旋转角度
            scaleX = 1; // 正常方向
            scaleY = 1; // 垂直缩放正常
        } else if (action.includes('leftdownattack')) {
            offsetX = 30;
            offsetY = 60; // 下方向的偏移量
            rotation = 90; // 下方向的旋转角度
            scaleX = 1; // 正常方向
            scaleY = -1; // 上下镜像
        } else if (action.includes('rightdownattack')) {
            offsetX = 50;
            offsetY = 70; // 下方向的偏移量
            rotation = 90; // 下方向的旋转角度
            scaleX = 1; // 正常方向
            scaleY = 1; // 垂直缩放正常
        }

        this.maoQiInstance.x = transform.x + offsetX;
        this.maoQiInstance.y = transform.y + offsetY;
        this.maoQiInstance.rotation = rotation;
        this.maoQiInstance.scaleX = scaleX; // 设置水平缩放比例
        this.maoQiInstance.scaleY = scaleY; // 设置垂直缩放比例
        
    

        // 0.3秒后将预制体移回原来的位置
        setTimeout(() => {
            this.maoQiInstance.x = -1000;
            this.maoQiInstance.y = -1000;
            this.maoQiInstance.rotation = 0; // 恢复原始旋转角度
            this.maoQiInstance.scaleX = 1; // 恢复原始水平缩放比例
            this.maoQiInstance.scaleY = 1; // 恢复原始垂直缩放比例
            console.log(`Moved prefab back to original position (-1000, -1000)`);
        }, 40);
    }

    onTick(duringTime: number) {
        if(this.gameObject.active === false) {
            this.attackAudio.stop();
        }
    }

    setAttackPower(power: number) {
        this.attackPower = power;
    }

    setAttackSpeed(speed: number) {
        this.attackSpeed = speed;
    }
}
