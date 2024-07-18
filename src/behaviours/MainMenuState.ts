import { Behaviour, getGameObjectById, GameObject, Transform } from "../../lib/mygameengine";
import { GameManager } from "./GameManager";
import { BitmapRenderer } from "../../lib/mygameengine";
import { AudioBehaviour, AudioSystem } from "../../lib/mygameengine";

enum MainMenuState {
    None,
    StartGame,
    QuitGame
}

export class MainMenuStateMachine extends Behaviour {
    private currentState: MainMenuState = MainMenuState.None;
    private menuImage: GameObject | null = null;
    private startPageImage: GameObject | null = null;
    private camera: GameObject | null = null;
    private offsetX: number = -1280; // X 轴偏移量
    private offsetY: number = -720; // Y 轴偏移量
    private bgmusic3: AudioBehaviour | null = null;


    onStart() {

        // 初始化音频行为
        this.bgmusic3 = new AudioBehaviour();
        this.bgmusic3.source = "./assets/audio/mainscreen.wav"; // 设置跳跃音频的路径
        this.bgmusic3.setLoop(true); // 设置不循环播放
        this.bgmusic3.setVolume(0.5); // 设置音量，范围是 0.0 到 1.0
        // 获取菜单图片的 GameObject
        this.menuImage = getGameObjectById('menuImage');
        this.startPageImage = getGameObjectById('menuImage1');

        // 获取摄像机对象
        this.camera = getGameObjectById('camera');

        // 初始化状态
        this.updateState(MainMenuState.StartGame);

        // 监听键盘事件
        document.addEventListener('keydown', this.handleKeyDown.bind(this));

        // 设置图片位置
        this.updateMenuImagePosition();

        // 等待500毫秒后禁用startPageImage
        setTimeout(() => {
            if (this.startPageImage) {
                this.startPageImage.active = false;
            }
            // 播放背景音乐
            if (this.gameObject.active === true && this.bgmusic3) {
                this.bgmusic3.play();
            }
        }, 7000);
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
            case MainMenuState.QuitGame:
                this.updateState(MainMenuState.StartGame);
                break;
            case MainMenuState.StartGame:
                this.updateState(MainMenuState.None);
                break;
            case MainMenuState.None:
                this.updateState(MainMenuState.QuitGame);
                break;
        }
    }

    navigateDown() {
        switch (this.currentState) {
            case MainMenuState.None:
                this.updateState(MainMenuState.StartGame);
                break;
            case MainMenuState.StartGame:
                this.updateState(MainMenuState.QuitGame);
                break;
            case MainMenuState.QuitGame:
                this.updateState(MainMenuState.None);
                break;
        }
    }

    executeSelection() {
        switch (this.currentState) {
            case MainMenuState.StartGame:
                this.startGame();
                break;
            case MainMenuState.QuitGame:
                this.quitGame();
                break;
            case MainMenuState.None:
                console.log("No selection made.");
                break;
        }
    }

    updateState(newState: MainMenuState) {
        this.currentState = newState;

        // 更新图片的source属性
        if (this.menuImage) {
            const bitmapRenderer = this.menuImage.getBehaviour(BitmapRenderer);
            if (bitmapRenderer) {
                switch (this.currentState) {
                    case MainMenuState.None:
                        bitmapRenderer.source = './assets/images/nosellect.jpg';
                        break;
                    case MainMenuState.StartGame:
                        bitmapRenderer.source = './assets/images/startgame.jpg';
                        break;
                    case MainMenuState.QuitGame:
                        bitmapRenderer.source = './assets/images/quitgame.jpg';
                        break;
                }
                console.log(`Current State: ${MainMenuState[this.currentState]}, Source: ${bitmapRenderer.source}`);
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
        
        if (this.startPageImage && this.camera) {
            const cameraTransform = this.camera.getBehaviour(Transform);
            const startPageImageTransform = this.startPageImage.getBehaviour(Transform);
            if (cameraTransform && startPageImageTransform) {
                startPageImageTransform.x = cameraTransform.x + this.offsetX;
                startPageImageTransform.y = cameraTransform.y + this.offsetY;
                console.log(`Start page image position updated to (${startPageImageTransform.x}, ${startPageImageTransform.y}) with offset (${this.offsetX}, ${this.offsetY})`);
            } else {
                console.error("Start page image or camera Transform not found");
            }
        } else {
            console.error("Start page image or camera GameObject not found");
        }
    }

    onUpdate() {
        if (this.gameObject.active === false) {
            // 移除键盘事件监听器
            this.bgmusic3.stop();
            this.onDestroy();
        }
    }

    startGame() {
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
