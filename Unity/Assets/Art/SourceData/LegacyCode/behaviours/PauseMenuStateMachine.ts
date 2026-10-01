import { Behaviour, getGameObjectById, GameObject, Transform, BitmapRenderer } from "../../lib/mygameengine";
import { GameManager } from "./GameManager";
import { AudioBehaviour } from "../../lib/mygameengine";

enum PauseMenuState {
    None,
    ContinueGame,
    ExitGame
}

export class PauseMenuStateMachine extends Behaviour {
    private currentState: PauseMenuState = PauseMenuState.ContinueGame;
    private menuImage3: GameObject | null = null;
    private camera: GameObject | null = null;
    private offsetX = -1280;
    private offsetY = -720;
    private bgmusic: AudioBehaviour | null = null;
    private readonly handleKeyDownBound = this.handleKeyDown.bind(this);

    onStart() {
        this.bgmusic = new AudioBehaviour();
        this.bgmusic.source = "./assets/audio/mainscreen.wav";
        this.bgmusic.setLoop(true);
        this.bgmusic.setVolume(0.3);

        this.menuImage3 = getGameObjectById('pauseMenuImage');
        this.camera = getGameObjectById('camera');
        this.updateState(PauseMenuState.ContinueGame);
        this.updateMenuImagePosition();
        document.removeEventListener('keydown', this.handleKeyDownBound);
        document.addEventListener('keydown', this.handleKeyDownBound);
    }

    handleKeyDown(event: KeyboardEvent) {
        if (!this.gameObject.active) {
            return;
        }

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
        this.updateState(this.currentState === PauseMenuState.ExitGame ? PauseMenuState.ContinueGame : PauseMenuState.ExitGame);
    }

    navigateDown() {
        this.updateState(this.currentState === PauseMenuState.ContinueGame ? PauseMenuState.ExitGame : PauseMenuState.ContinueGame);
    }

    executeSelection() {
        if (this.currentState === PauseMenuState.ContinueGame) {
            this.continueGame();
        } else {
            this.quitGame();
        }
    }

    updateState(newState: PauseMenuState) {
        this.currentState = newState;
        if (this.menuImage3) {
            const bitmapRenderer = this.menuImage3.getBehaviour(BitmapRenderer);
            if (bitmapRenderer) {
                bitmapRenderer.source = this.currentState === PauseMenuState.ContinueGame ? './assets/images/continue.png' : './assets/images/exit.png';
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
            }
        }
    }

    onTick() {
        if (this.gameObject.active) {
            this.bgmusic?.play();
            this.updateMenuImagePosition();
        } else {
            this.bgmusic?.stop();
        }
    }

    continueGame() {
        const gameManager = getGameObjectById('gameManager');
        if (gameManager) {
            gameManager.getBehaviour(GameManager).resumeGame();
        }
    }

    quitGame() {
        window.close();
    }

    onEnd() {
        document.removeEventListener('keydown', this.handleKeyDownBound);
    }
}
