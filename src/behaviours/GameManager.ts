import { Behaviour, getGameObjectById, GameObject, Transform } from "../../lib/mygameengine";

export class GameManager extends Behaviour {
  private currentScene: GameObject | null = null;
  private scenes: { [key: string]: GameObject } = {};
  private blood: GameObject | null = null;
  private blue: GameObject | null = null;
  private camera: GameObject | null = null;

  onStart() {
    console.log("Initializing scenes...");

    // 初始化所有场景
    this.scenes = {
      'mainmenu': getGameObjectById('mainmenu'),
      'scene': getGameObjectById('scene'),
      // 添加更多场景...
    };

    // 初始化 blood 和 blue 对象
    this.blood = getGameObjectById('blood');
    this.blue = getGameObjectById('blue');

    // 初始化 camera 对象
    this.camera = getGameObjectById('camera');

    // 调试信息
    console.log("mainmenu scene:", this.scenes['mainmenu']);
    console.log("scene scene:", this.scenes['scene']);


   // 在下一帧禁用所有场景
   requestAnimationFrame(() => {
    this.deactivateAllScenes();

    // 在下一帧启用mainmenu场景
    requestAnimationFrame(() => {
      this.switchScene('mainmenu');
    });
  });

    // 监听键盘事件
    document.addEventListener('keydown', this.handleKeyDown.bind(this));
  }

  deactivateAllScenes() {
    for (const sceneId in this.scenes) {
      const scene = this.scenes[sceneId];
      if (scene) {
        scene.active = false;
        console.log(`Deactivating scene: ${sceneId}`);
      }
    }

    // 确保 blood 和 blue 在初始化时禁用
    if (this.blood) {
      this.blood.active = false;
    }

    if (this.blue) {
      this.blue.active = false;
    }
  }

  handleKeyDown(event: KeyboardEvent) {
    if (event.key === 'Escape') {
      this.switchScene('mainmenu');
    }
  }

  switchScene(sceneId: string) {
    if (this.currentScene) {
      console.log(`Deactivating current scene: ${this.currentScene.id}`);
      this.currentScene.active = false;  // 使用 GameObject 的 active setter
      console.log(`Current scene ${this.currentScene.id} active status after deactivation: ${this.currentScene.active}`);
      // 如果是离开 scene 场景，禁用 blood 和 blue
      if (this.currentScene.id === 'scene') {
        if (this.blood) this.blood.active = false;
        if (this.blue) this.blue.active = false;
      }
    }

    const newScene = this.scenes[sceneId];
    if (newScene) {
      console.log(`Activating new scene: ${sceneId}`);
      newScene.active = true;  // 使用 GameObject 的 active setter
      this.currentScene = newScene;
      console.log(`New scene ${sceneId} active status after activation: ${newScene.active}`);
      console.log(`Switched to scene: ${sceneId}`);
      // 如果是进入 scene 场景，启用 blood 和 blue
      if (sceneId === 'scene') {
        if (this.blood) this.blood.active = true;
        if (this.blue) this.blue.active = true;
      } 
    } else {
      console.error(`Scene with ID ${sceneId} not found`);
    }
  }


  onDestroy() {
    // 移除键盘事件监听器
    document.removeEventListener('keydown', this.handleKeyDown.bind(this));
  }
}
