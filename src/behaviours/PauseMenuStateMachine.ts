import { Behaviour, getGameObjectById, GameObject, Transform, BitmapRenderer } from "../../lib/mygameengine";
import { GameManager } from "./GameManager";
import { AudioBehaviour, AudioSystem } from "../../lib/mygameengine";

enum PauseMenuState {
    None,
    ContinueGame,
    ExitGame
}

export class PauseMenuStateMachine extends Behaviour {
    private currentState: PauseMenuState = PauseMenuState.None;
    private menuImage3: GameObject | null = null;
    private camera: GameObject | null = null;
    private offsetX: number = -1280; // X 轴偏移量
    private offsetY: number = -720; // Y 轴偏移量
    private bgmusic: AudioBehaviour | null = null;

    onStart() {
        // 初始化音频行为
        this.bgmusic = new AudioBehaviour();
        this.bgmusic.source = "./assets/audio/mainscreen.wav"; // 使用与主菜单相同的背景音乐
        this.bgmusic.setLoop(true); // 设置不循环播放
        this.bgmusic.setVolume(0.5); // 设置音量，范围是 0.0 到 1.0

        // 获取菜单图片的 GameObject
        this.menuImage3 = getGameObjectById('pauseMenuImage');

        // 获取摄像机对象
        this.camera = getGameObjectById('camera');

        // 初始化状态
        this.updateState(PauseMenuState.ContinueGame);

        // 监听键盘事件
        document.addEventListener('keydown', this.handleKeyDown.bind(this));

        // 设置图片位置
        this.updateMenuImagePosition();

        if (this.gameObject.active === true && this.bgmusic) {
            this.bgmusic.play();
        }
    }

    onEnter() {
        // 每次进入暂停菜单时初始化状态
        this.updateState(PauseMenuState.None);
    }

    handleKeyDown(event: KeyboardEvent) {
        switch (event.code) {
            case 'ArrowUp':
                this.navigateUp();
                break;
            case 'ArrowDown':
                this.navigateDown();
                break;
            case 'Enter':
                this.executeSelection();
                break;
        }
    }

    navigateUp() {
        switch (this.currentState) {
            case PauseMenuState.ExitGame:
                this.updateState(PauseMenuState.ContinueGame);
                break;
            case PauseMenuState.ContinueGame:
                this.updateState(PauseMenuState.None);
                break;
            case PauseMenuState.None:
                this.updateState(PauseMenuState.ExitGame);
                break;
        }
    }

    navigateDown() {
        switch (this.currentState) {
            case PauseMenuState.None:
                this.updateState(PauseMenuState.ContinueGame);
                break;
            case PauseMenuState.ContinueGame:
                this.updateState(PauseMenuState.ExitGame);
                break;
            case PauseMenuState.ExitGame:
                this.updateState(PauseMenuState.None);
                break;
        }
    }

    executeSelection() {
        switch (this.currentState) {
            case PauseMenuState.ContinueGame:
                this.continueGame();
                break;
            case PauseMenuState.ExitGame:
                this.quitGame();
                break;
            case PauseMenuState.None:
                console.log("No selection made.");
                break;
        }
    }

    updateState(newState: PauseMenuState) {
        this.currentState = newState;

        // 更新图片的source属性
        if (this.menuImage3) {
            const bitmapRenderer = this.menuImage3.getBehaviour(BitmapRenderer);
            if (bitmapRenderer) {
                switch (this.currentState) {
                    case PauseMenuState.None:
                        bitmapRenderer.source = './assets/images/nos.png';
                        break;
                    case PauseMenuState.ContinueGame:
                        bitmapRenderer.source = './assets/images/continue.png';
                        break;
                    case PauseMenuState.ExitGame:
                        bitmapRenderer.source = './assets/images/exit.png';
                        break;
                }
                console.log(`Current State: ${PauseMenuState[this.currentState]}, Source: ${bitmapRenderer.source}`);
            }
        }
    }

    updateMenuImagePosition() {
        if (this.menuImage3 && this.camera) {
            const cameraTransform = this.camera.getBehaviour(Transform);
            const menuImageTransform = this.menuImage3.getBehaviour(Transform);
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

    onUpdate() {

        if (this.gameObject.active === false) {
            // 移除键盘事件监听器
            this.bgmusic.stop();
            this.onDestroy();
        }
    }

    continueGame() {
        // 切换到游戏场景
        const gameManager = getGameObjectById('gameManager');
        if (gameManager) {
            gameManager.getBehaviour(GameManager).switchScene('scene');
        }
    }

    quitGame() {
        // 退出游戏
        window.close();
    }

    onDestroy() {
        // 移除键盘事件监听器
        document.removeEventListener('keydown', this.handleKeyDown.bind(this));
    }
}
