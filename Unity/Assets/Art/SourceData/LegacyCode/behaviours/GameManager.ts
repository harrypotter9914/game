import { Behaviour, getGameObjectById, GameObject } from "../../lib/mygameengine";
import { PlayerSession } from "./PlayerSession";

export class GameManager extends Behaviour {
  private currentScene: GameObject | null = null;
  private overlayScene: GameObject | null = null;
  private scenes: { [key: string]: GameObject | null } = {};
  private blood: GameObject | null = null;
  private blue: GameObject | null = null;
  private iconDisplay: GameObject | null = null;
  private readonly handleKeyDownBound = this.handleKeyDown.bind(this);

  onStart() {
    PlayerSession.ensureInitialized();

    this.scenes = {
      mainmenu: getGameObjectById('mainmenu'),
      scene: getGameObjectById('scene'),
      diedmenu: getGameObjectById('diedmenu'),
      pausemenu: getGameObjectById('pausemenu'),
    };

    this.blood = getGameObjectById('blood');
    this.blue = getGameObjectById('blue');
    this.iconDisplay = getGameObjectById('iconDisplay');

    document.removeEventListener('keydown', this.handleKeyDownBound);
    document.addEventListener('keydown', this.handleKeyDownBound);

    requestAnimationFrame(() => {
      this.showExclusiveScene('mainmenu');
    });
  }

  private setHudActive(active: boolean) {
    if (this.blood) this.blood.active = active;
    if (this.blue) this.blue.active = active;
    if (this.iconDisplay) this.iconDisplay.active = active;
  }

  private hideAllScenes() {
    for (const scene of Object.values(this.scenes)) {
      if (scene) {
        scene.active = false;
      }
    }
  }

  private clearOverlay() {
    if (this.overlayScene) {
      this.overlayScene.active = false;
      this.overlayScene = null;
    }
  }

  private showExclusiveScene(sceneId: string) {
    this.hideAllScenes();
    this.currentScene = this.scenes[sceneId] || null;
    this.overlayScene = null;

    if (this.currentScene) {
      this.currentScene.active = true;
    }

    const gameplayVisible = sceneId === 'scene';
    this.setHudActive(gameplayVisible);
    PlayerSession.setPaused(false);
  }

  handleKeyDown(event: KeyboardEvent) {
    if (event.key === 'Escape') {
      if (this.overlayScene?.id === 'pausemenu') {
        this.resumeGame();
        return;
      }

      if (this.currentScene?.id === 'scene' && !PlayerSession.isPaused()) {
        this.openPauseMenu();
      }
    }
  }

  startNewGame() {
    PlayerSession.resetForNewGame();
    this.showExclusiveScene('scene');
  }

  setWorldPaused(paused: boolean) {
    PlayerSession.setPaused(paused);
  }

  togglePause() {
    if (this.overlayScene?.id === 'pausemenu') {
      this.resumeGame();
    } else {
      this.openPauseMenu();
    }
  }

  private openPauseMenu() {
    if (!this.scenes.pausemenu || this.currentScene?.id !== 'scene') {
      return;
    }

    this.overlayScene = this.scenes.pausemenu;
    this.overlayScene.active = true;
    this.setHudActive(true);
    PlayerSession.setPaused(true);
  }

  resumeGame() {
    this.clearOverlay();
    if (this.currentScene?.id === 'scene') {
      this.currentScene.active = true;
      this.setHudActive(true);
    }
    PlayerSession.setPaused(false);
  }

  switchScene(sceneId: string) {
    this.showExclusiveScene(sceneId);
  }

  onEnd() {
    document.removeEventListener('keydown', this.handleKeyDownBound);
  }
}
