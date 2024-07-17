import { Behaviour, getGameObjectById, GameObject } from "../../lib/mygameengine";

export class GameManager extends Behaviour {
    private currentScene: GameObject | null = null;
    private scenes: { [key: string]: GameObject } = {};

    onStart() {
        console.log("Initializing scenes...");

        // 初始化所有场景并设置为不活跃状态
        this.scenes = {
            'mainmenu': getGameObjectById('mainmenu'),
            'scene': getGameObjectById('scene'),
            // 添加更多场景...
        };

        // 调试信息
        console.log("mainmenu scene:", this.scenes['mainmenu']);
        console.log("scene scene:", this.scenes['scene']);

        for (const sceneId in this.scenes) {
            const scene = this.scenes[sceneId];
            if (scene) {
                console.log(`Deactivating scene: ${sceneId}`);
                this.setActive(scene, false);
                console.log(`${sceneId} active status after deactivation: ${scene.active}`);
            } else {
                console.error(`Scene with ID ${sceneId} not found`);
            }
        }

        // 设置初始场景
        this.switchScene('mainmenu');
    }

    switchScene(sceneId: string) {
        if (this.currentScene) {
            console.log(`Deactivating current scene: ${this.currentScene.id}`);
            this.setActive(this.currentScene, false);
            console.log(`Current scene ${this.currentScene.id} active status after deactivation: ${this.currentScene.active}`);
        }

        const newScene = this.scenes[sceneId];
        if (newScene) {
            console.log(`Activating new scene: ${sceneId}`);
            this.setActive(newScene, true);
            this.currentScene = newScene;
            console.log(`New scene ${sceneId} active status after activation: ${newScene.active}`);
            console.log(`Switched to scene: ${sceneId}`);
        } else {
            console.error(`Scene with ID ${sceneId} not found`);
        }
    }

    setActive(gameObject: GameObject, value: boolean) {
        gameObject._active = value;
        console.log(`Setting active: ${value} for GameObject with id: ${gameObject.id}`);
        for (const behaviour of gameObject.behaviours) {
            behaviour.active = value;
            console.log(`Setting active: ${value} for Behaviour: ${behaviour.constructor.name}`);
        }
        for (const child of gameObject.children) {
            this.setActive(child, value);
            console.log(`Setting active: ${value} for Child GameObject with id: ${child.id}`);
        }
    }
}
