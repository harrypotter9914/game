import { Behaviour, getGameObjectById, GameObject, Transform, BitmapRenderer } from "../../lib/mygameengine";
import { AudioBehaviour } from "../../lib/mygameengine";

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
    private offsetX = -1280;
    private offsetY = -705;
    private imageSources: string[] = [
        './assets/images/died1.png',
        './assets/images/died2.png',
        './assets/images/died3.png',
        './assets/images/died4.png',
        './assets/images/died5.png'
    ];
    private changeInterval = 3000;
    private changeTimer: ReturnType<typeof setInterval> | null = null;
    private bgmusic2: AudioBehaviour | null = null;
    private readonly handleKeyDownBound = this.handleKeyDown.bind(this);

    onStart() {
        this.bgmusic2 = new AudioBehaviour();
        this.bgmusic2.source = "./assets/audio/dead.wav";
        this.bgmusic2.setLoop(true);
        this.bgmusic2.setVolume(0.5);

        this.menuImage = getGameObjectById('diedmenuImage');
        this.camera = getGameObjectById('camera');
        this.updateState(DiedMenuState.Image1);
        this.updateMenuImagePosition();

        document.removeEventListener('keydown', this.handleKeyDownBound);
        document.addEventListener('keydown', this.handleKeyDownBound);

        this.startImageChangeTimer();
    }

    handleKeyDown(event: KeyboardEvent) {
        if (event.code === 'Enter' && this.gameObject.active) {
            window.close();
        }
    }

    startImageChangeTimer() {
        if (this.changeTimer) {
            clearInterval(this.changeTimer);
        }
        this.changeTimer = setInterval(() => {
            if (this.gameObject.active) {
                this.changeImage();
            }
        }, this.changeInterval);
    }

    changeImage() {
        const nextState = (this.currentState + 1) % this.imageSources.length;
        this.updateState(nextState as DiedMenuState);
    }

    updateState(newState: DiedMenuState) {
        this.currentState = newState;
        const bitmapRenderer = this.menuImage?.getBehaviour(BitmapRenderer);
        if (bitmapRenderer) {
            bitmapRenderer.source = this.imageSources[this.currentState];
        }
    }

    updateMenuImagePosition() {
        if (!this.menuImage || !this.camera) {
            return;
        }

        const cameraTransform = this.camera.getBehaviour(Transform);
        const menuImageTransform = this.menuImage.getBehaviour(Transform);
        if (cameraTransform && menuImageTransform) {
            menuImageTransform.x = cameraTransform.x + this.offsetX;
            menuImageTransform.y = cameraTransform.y + this.offsetY;
        }
    }

    onTick() {
        if (this.gameObject.active) {
            this.bgmusic2?.play();
            this.updateMenuImagePosition();
        } else {
            this.bgmusic2?.stop();
        }
    }

    onEnd() {
        document.removeEventListener('keydown', this.handleKeyDownBound);
        if (this.changeTimer) {
            clearInterval(this.changeTimer);
            this.changeTimer = null;
        }
        this.bgmusic2?.stop();
    }
}
