import { Behaviour, getGameObjectById, GameObject, Transform, BitmapRenderer } from "../../lib/mygameengine";
import { GameManager } from "./GameManager";
import { AudioBehaviour } from "../../lib/mygameengine";

enum MainMenuState {
    StartGame,
    QuitGame
}

export class MainMenuStateMachine extends Behaviour {
    private currentState: MainMenuState = MainMenuState.StartGame;
    private menuImage: GameObject | null = null;
    private startPageImage: GameObject | null = null;
    private camera: GameObject | null = null;
    private offsetX = -1280;
    private offsetY = -720;
    private bgmusic3: AudioBehaviour | null = null;
    private readonly handleKeyDownBound = this.handleKeyDown.bind(this);

    onStart() {
        this.bgmusic3 = new AudioBehaviour();
        this.bgmusic3.source = "./assets/audio/mainscreen.wav";
        this.bgmusic3.setLoop(true);
        this.bgmusic3.setVolume(0.5);
        this.menuImage = getGameObjectById('menuImage');
        this.startPageImage = getGameObjectById('menuImage1');
        this.camera = getGameObjectById('camera');
        this.updateState(MainMenuState.StartGame);
        this.updateMenuImagePosition();
        document.removeEventListener('keydown', this.handleKeyDownBound);
        document.addEventListener('keydown', this.handleKeyDownBound);

        setTimeout(() => {
            if (this.startPageImage) {
                this.startPageImage.active = false;
            }
        }, 3000);
    }

    handleKeyDown(event: KeyboardEvent) {
        if (!this.gameObject.active) {
            return;
        }

        switch (event.code) {
            case 'ArrowUp':
            case 'ArrowDown':
                this.currentState = this.currentState === MainMenuState.StartGame ? MainMenuState.QuitGame : MainMenuState.StartGame;
                this.updateState(this.currentState);
                break;
            case 'Enter':
                this.executeSelection();
                break;
        }
    }

    executeSelection() {
        if (this.currentState === MainMenuState.StartGame) {
            this.startGame();
        } else {
            this.quitGame();
        }
    }

    updateState(newState: MainMenuState) {
        this.currentState = newState;
        if (this.menuImage) {
            const bitmapRenderer = this.menuImage.getBehaviour(BitmapRenderer);
            if (bitmapRenderer) {
                bitmapRenderer.source = this.currentState === MainMenuState.StartGame ? './assets/images/startgame.jpg' : './assets/images/quitgame.jpg';
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
            }
        }

        if (this.startPageImage && this.camera) {
            const cameraTransform = this.camera.getBehaviour(Transform);
            const startPageImageTransform = this.startPageImage.getBehaviour(Transform);
            if (cameraTransform && startPageImageTransform) {
                startPageImageTransform.x = cameraTransform.x + this.offsetX;
                startPageImageTransform.y = cameraTransform.y + this.offsetY;
            }
        }
    }

    onTick() {
        if (this.gameObject.active) {
            this.bgmusic3?.play();
            this.updateMenuImagePosition();
        } else {
            this.bgmusic3?.stop();
        }
    }

    startGame() {
        const gameManager = getGameObjectById('gameManager');
        if (gameManager) {
            gameManager.getBehaviour(GameManager).startNewGame();
        }
    }

    quitGame() {
        window.close();
    }

    onEnd() {
        document.removeEventListener('keydown', this.handleKeyDownBound);
    }
}
