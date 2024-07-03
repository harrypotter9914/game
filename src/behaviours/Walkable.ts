import { Behaviour, number, RigidBody, BoxCollider, Transform } from "../../lib/mygameengine";
import { MainRolePrefabBinding } from "../bindings/MainRolePrefabBinding"; // 假设文件路径正确

export class Walkable extends Behaviour {
  @number()
  speed = 1;

  @number()
  jumpForce = 5;

  private jumpCount = 0;
  private isGrounded = false;
  private mainRoleBinding: MainRolePrefabBinding | null = null;
  private lastAction: string = 'right'; // 默认的action

  onStart() {
    this.mainRoleBinding = this.gameObject.getBehaviour(MainRolePrefabBinding);
    if (this.mainRoleBinding) {
      this.mainRoleBinding.action = this.lastAction; // 设置默认的action为right
    } else {
      console.warn('MainRolePrefabBinding not found on the game object.');
    }
    document.addEventListener('keydown', this.handleKeyDown.bind(this));
    document.addEventListener('keyup', this.handleKeyUp.bind(this));
  }

  handleKeyDown(event: KeyboardEvent) {
    const rigid = this.gameObject.getBehaviour(RigidBody);

    switch (event.code) {
      case 'ArrowLeft':
        rigid.b2RigidBody.SetLinearVelocity({ x: -this.speed, y: rigid.b2RigidBody.GetLinearVelocity().y });
        this.lastAction = 'left';
        if (this.mainRoleBinding) {
          this.mainRoleBinding.action = this.lastAction;
        }
        break;
      case 'ArrowRight':
        rigid.b2RigidBody.SetLinearVelocity({ x: this.speed, y: rigid.b2RigidBody.GetLinearVelocity().y });
        this.lastAction = 'right';
        if (this.mainRoleBinding) {
          this.mainRoleBinding.action = this.lastAction;
        }
        break;
      case 'KeyC':
        if (this.jumpCount < 2) {
          rigid.b2RigidBody.SetLinearVelocity({ x: rigid.b2RigidBody.GetLinearVelocity().x, y: this.jumpForce });
          this.jumpCount++;
        }
        break;
    }
  }

  handleKeyUp(event: KeyboardEvent) {
    const rigid = this.gameObject.getBehaviour(RigidBody);

    switch (event.code) {
      case 'ArrowLeft':
      case 'ArrowRight':
        rigid.b2RigidBody.SetLinearVelocity({ x: rigid.b2RigidBody.GetLinearVelocity().x * 0.5, y: rigid.b2RigidBody.GetLinearVelocity().y });
        if (this.mainRoleBinding) {
          this.mainRoleBinding.action = this.lastAction; // 松开按键时恢复到松开按键前的action
        }
        break;
      case 'KeyC':
        break;
    }
  }

  onUpdate() {
    const rigid = this.gameObject.getBehaviour(RigidBody);
    const collider = this.gameObject.getBehaviour(BoxCollider);

    if (this.isCollidingWithGround(collider)) {
      this.isGrounded = true;
      this.jumpCount = 0;
    } else {
      this.isGrounded = false;
    }

    const velocity = rigid.b2RigidBody.GetLinearVelocity();
    if (velocity.x !== 0) {
      rigid.b2RigidBody.SetLinearVelocity({ x: velocity.x * 0.95, y: velocity.y });
    }
  }

  isCollidingWithGround(collider: BoxCollider): boolean {
    // 检查是否有碰撞发生
    const transform = this.gameObject.getBehaviour(Transform);
    const halfHeight = collider.height / 2;

    // 假设地面y坐标为0
    return transform.y - halfHeight <= 0;
  }

  onDestroy() {
    document.removeEventListener('keydown', this.handleKeyDown.bind(this));
    document.removeEventListener('keyup', this.handleKeyUp.bind(this));
  }
}
