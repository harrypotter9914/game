import { Behaviour, number, RigidBody, BoxCollider, Transform } from "../../lib/mygameengine";

export class Walkable extends Behaviour {
  @number()
  speed = 1;

  @number()
  jumpForce = 5;

  private jumpCount = 0;
  private isGrounded = false;

  onStart() {
    document.addEventListener('keydown', this.handleKeyDown.bind(this));
    document.addEventListener('keyup', this.handleKeyUp.bind(this));
  }

  handleKeyDown(event: KeyboardEvent) {
    const rigid = this.gameObject.getBehaviour(RigidBody);

    switch (event.code) {
      case 'ArrowLeft':
        rigid.b2RigidBody.SetLinearVelocity({ x: -this.speed, y: rigid.b2RigidBody.GetLinearVelocity().y });
        break;
      case 'ArrowRight':
        rigid.b2RigidBody.SetLinearVelocity({ x: this.speed, y: rigid.b2RigidBody.GetLinearVelocity().y });
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
        break;
      case 'KeyC':
        // 不处理垂直速度，保持跳跃后的y方向速度
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

    // 添加水平缓冲停止效果
    const velocity = rigid.b2RigidBody.GetLinearVelocity();
    if (velocity.x !== 0) {
      rigid.b2RigidBody.SetLinearVelocity({ x: velocity.x * 0.95, y: velocity.y });
    }
  }

  isCollidingWithGround(collider: BoxCollider): boolean {
    // 检查是否有碰撞发生
    const transform = this.gameObject.getBehaviour(Transform);
    const halfHeight = collider.height / 2;

    // 假设地面y坐标为0，实际实现中根据具体情况检查
    return transform.y - halfHeight <= 0;
  }

  onDestroy() {
    document.removeEventListener('keydown', this.handleKeyDown.bind(this));
    document.removeEventListener('keyup', this.handleKeyUp.bind(this));
  }
}
