import { Behaviour, getGameObjectById, GameObject, Transform, BitmapRenderer } from "../../lib/mygameengine";
import { GameManager } from "./GameManager";
import { AudioBehaviour, AudioSystem } from "../../lib/mygameengine";

enum DiedMenuState {
    Image1,
    Image2,
    Image3,
    Image4,
    Image5
}

export class DiedMenuStateMachine extends Behaviour {
    private currentState: DiedMenuState = DiedMenuState.Image1;
    private menuImage: GameObject | null = null;
    private camera: GameObject | null = null;
    private offsetX: number = -1280; // X 轴偏移量
    private offsetY: number = -705; // Y 轴偏移量;
    private imageSources: string[] = [
        './assets/images/died1.png',
        './assets/images/died2.png',
        './assets/images/died3.png',
        './assets/images/died4.png',
        './assets/images/died5.png'
    ];
    private changeInterval: number = 3000; // 图片切换间隔（毫秒）
    private changeTimer: number | null = null;
    private bgmusic2: AudioBehaviour | null = null;


    onStart() {
        // 初始化音频行为
        this.bgmusic2 = new AudioBehaviour();
        this.bgmusic2.source = "./assets/audio/dead.wav"; // 设置跳跃音频的路径
        this.bgmusic2.setLoop(true); // 设置不循环播放
        this.bgmusic2.setVolume(0.5); // 设置音量，范围是 0.0 到 1.0
        // 获取菜单图片的 GameObject
        this.menuImage = getGameObjectById('diedmenuImage');

        // 获取摄像机对象
        this.camera = getGameObjectById('camera');

        // 初始化状态
        this.updateState(DiedMenuState.Image1);

        // 监听键盘事件
        document.addEventListener('keydown', this.handleKeyDown.bind(this));

        // 设置图片位置
        this.updateMenuImagePosition();

        // 开始图片切换计时器
        this.startImageChangeTimer();

        if (this.gameObject.active === true) {
            this.bgmusic2.play();
        } 
    }


    onUpdate() {
        if (!this.gameObject.active) {
            // 移除键盘事件监听器
            this.bgmusic2.stop();
            document.removeEventListener('keydown', this.handleKeyDown.bind(this));
            this.onDestroy();
        }
    }

    handleKeyDown(event: KeyboardEvent) {
        if (event.code === 'Enter' && this.gameObject.active === true) {
            this.quitGame();
        }
    }

    startImageChangeTimer() {
        if (this.changeTimer !== null) {
            clearInterval(this.changeTimer);
        }
        this.changeTimer = setInterval(() => {
            this.changeImage();
        }, this.changeInterval);
    }

    changeImage() {
        switch (this.currentState) {
            case DiedMenuState.Image1:
                this.updateState(DiedMenuState.Image2);
                break;
            case DiedMenuState.Image2:
                this.updateState(DiedMenuState.Image3);
                break;
            case DiedMenuState.Image3:
                this.updateState(DiedMenuState.Image4);
                break;
            case DiedMenuState.Image4:
                this.updateState(DiedMenuState.Image5);
                break;
            case DiedMenuState.Image5:
                this.updateState(DiedMenuState.Image1);
                break;
        }
    }

    updateState(newState: DiedMenuState) {
        this.currentState = newState;

        // 更新图片的source属性
        if (this.menuImage) {
            const bitmapRenderer = this.menuImage.getBehaviour(BitmapRenderer);
            if (bitmapRenderer) {
                bitmapRenderer.source = this.imageSources[this.currentState];
                console.log(`Current State: ${DiedMenuState[this.currentState]}, Source: ${bitmapRenderer.source}`);
            }
        }
    }

    updateMenuImagePosition() {
        if (this.menuImage && this.camera) {
            const cameraTransform = this.camera.getBehaviour(Transform);
            const menuImageTransform = this.menuImage.getBehaviour(Transform);
            if (cameraTransform && menuImageTransform) {
                menuImageTransform.x = cameraTransform.x + this.offsetX;
                menuImageTransform.y = cameraTransform.y + this.offsetY;
                console.log(`Menu image position updated to (${menuImageTransform.x}, ${menuImageTransform.y}) with offset (${this.offsetX}, ${this.offsetY})`);
            } else {
                console.error("Menu image or camera Transform not found");
            }
        } else {
            console.error("Menu image or camera GameObject not found");
        }
    }


    quitGame() {
        // 退出游戏
        window.close();
    }

    onDestroy() {
        // 移除键盘事件监听器
        document.removeEventListener('keydown', this.handleKeyDown.bind(this));

        // 清除图片切换计时器
        if (this.changeTimer !== null) {
            clearInterval(this.changeTimer);
            this.changeTimer = null;
        }
    }
}
