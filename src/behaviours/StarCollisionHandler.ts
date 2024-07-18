import { Behaviour, Collider, RigidBody, GameObject, Transform } from "../../lib/mygameengine";
import { BitmapRenderer } from "../../lib/mygameengine";
import { getGameObjectById } from "../../lib/mygameengine";
import { Walkable } from "./Walkable";

export class StarCollisionHandler extends Behaviour {
    private camera: GameObject | null = null;
    private iconDisplay: GameObject | null = null;
    private interactingStar: GameObject | null = null;
    private isInteracting: boolean = false;
    private interactionProgress: number = 0; // 交互进度
    private interactionSpeed: number = 0.05; // 交互速度
    private finalScaleX: number = 0.6; // 最终的X缩放
    private finalScaleY: number = 0.6; // 最终的Y缩放
    private offsetX: number = -370; // X轴偏移量
    private offsetY: number = -400; // Y轴偏移量
    private isGrowing: boolean = false; // 是否在放大
    private Walkable: Walkable

    onStart() {
        this.camera = getGameObjectById('camera');
        this.iconDisplay = getGameObjectById('iconDisplay');

        document.addEventListener('keydown', this.handleKeyDown.bind(this));
        console.log("StarCollisionHandler started");
    }


    setInteractingStar(star: GameObject) {
        this.interactingStar = star;
    }

    handleKeyDown(event: KeyboardEvent) {
        if (event.code === 'KeyE' && this.interactingStar && !this.isGrowing) {
            this.isInteracting = true; // 开始交互
            this.isGrowing = true; // 图标开始放大
            this.interactionProgress = 0; // 重置交互进度
            this.handleStarCollision(this.Walkable.startag);
        } else if (event.code === 'KeyQ' && this.iconDisplay) {
            this.isInteracting = true; // 开始交互
            this.isGrowing = false; // 图标开始缩小
            this.interactionProgress = 1; // 从最大大小开始
        }
    }

    completeInteraction() {
        if (this.iconDisplay && this.interactingStar) {
            // 还原图标大小和位置
            const iconTransform = this.iconDisplay.getBehaviour(Transform);
            if (iconTransform) {
                iconTransform.scaleX = 0.001;
                iconTransform.scaleY = 0.001;
                iconTransform.x = this.interactingStar.getBehaviour(Transform).x;
                iconTransform.y = this.interactingStar.getBehaviour(Transform).y;
            }

            // 禁用星星对象
            this.interactingStar.active = false;
            this.interactingStar = null;
            this.isInteracting = false;
        }
    }

    handleStarCollision(tag: string) {
        if (this.iconDisplay) {
            const bitmapRenderer = this.iconDisplay.getBehaviour(BitmapRenderer);
            if (bitmapRenderer) {
                const imageSource = `./assets/images/${tag}.png`; // 根据tag生成图片路径
                bitmapRenderer.source = imageSource;
                console.log(`Icon updated: Source: ${imageSource}`);

                // 将图标移动到星星位置并开始变大
                if (this.interactingStar) {
                    const starTransform = this.interactingStar.getBehaviour(Transform);
                    const iconTransform = this.iconDisplay.getBehaviour(Transform);
                    if (starTransform && iconTransform) {
                        iconTransform.x = starTransform.x;
                        iconTransform.y = starTransform.y;
                        iconTransform.scaleX = 0.001;
                        iconTransform.scaleY = 0.001;
                        this.interactionProgress = 0; // 重置交互进度
                    } else {
                        console.error("Star or icon Transform not found");
                    }
                }
            } else {
                console.error("BitmapRenderer not found on iconDisplay GameObject");
            }
        } else {
            console.error("Icon display GameObject not found");
        }
    }

    onTick(duringTime: number) {
        if (this.isInteracting && this.iconDisplay) {
            const iconTransform = this.iconDisplay.getBehaviour(Transform);
            if (iconTransform) {
                // 更新图标大小
                if (this.isGrowing) {
                    this.interactionProgress += this.interactionSpeed * duringTime;
                    if (this.interactionProgress >= 1) {
                        this.interactionProgress = 1; // 限制最大值为1
                        this.isInteracting = false; // 停止交互
                    }
                } else {
                    this.interactionProgress -= this.interactionSpeed * duringTime;
                    if (this.interactionProgress <= 0) {
                        this.interactionProgress = 0; // 限制最小值为0
                        this.isInteracting = false; // 停止交互
                        this.completeInteraction(); // 完成交互
                    }
                }
                iconTransform.scaleX = 0.001 + (this.finalScaleX - 0.001) * this.interactionProgress; // 从0.001到指定大小
                iconTransform.scaleY = 0.001 + (this.finalScaleY - 0.001) * this.interactionProgress; // 从0.001到指定大小
                console.log(`Icon scaling: (${iconTransform.scaleX}, ${iconTransform.scaleY})`);

                // 更新图标位置，增加偏移量
                const starTransform = this.interactingStar?.getBehaviour(Transform);
                if (starTransform) {
                    iconTransform.x = starTransform.x + this.offsetX * this.interactionProgress;
                    iconTransform.y = starTransform.y + this.offsetY * this.interactionProgress;
                    console.log(`Icon position: (${iconTransform.x}, ${iconTransform.y})`);
                }
            }
        }
    }
}
