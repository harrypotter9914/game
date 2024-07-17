import { Behaviour, getGameObjectById, GameObject } from "../../lib/mygameengine";
import { GameManager } from "./GameManager";
import { BitmapRenderer } from "../../lib/mygameengine";

enum MainMenuState {
    None,
    StartGame,
    QuitGame
}

export class MainMenuStateMachine extends Behaviour {
    private currentState: MainMenuState = MainMenuState.None;
    private menuImage: GameObject | null = null;

    onStart() {
        // 获取菜单图片的 GameObject
        this.menuImage = getGameObjectById('menuImage');

        // 初始化状态
        this.updateState(MainMenuState.None);

        // 监听键盘事件
        document.addEventListener('keydown', this.handleKeyDown.bind(this));
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
        if (this.currentState === MainMenuState.QuitGame) {
            this.updateState(MainMenuState.StartGame);
        } else if (this.currentState === MainMenuState.StartGame) {
            this.updateState(MainMenuState.None);
        }
    }

    navigateDown() {
        if (this.currentState === MainMenuState.None) {
            this.updateState(MainMenuState.StartGame);
        } else if (this.currentState === MainMenuState.StartGame) {
            this.updateState(MainMenuState.QuitGame);
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
