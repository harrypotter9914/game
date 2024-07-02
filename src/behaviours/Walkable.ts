import { Behaviour, number, RigidBody } from "../../lib/mygameengine";

export class Walkable extends Behaviour {
  @number()
  speed = 1;

  @number()
  jumpForce = 5;

  private jumpCount = 0;
  private isGrounded = true;

  onStart() {
    document.addEventListener('keydown', this.handleKeyDown.bind(this));
    document.addEventListener('keyup', this.handleKeyUp.bind(this));
  }

  handleKeyDown(event: KeyboardEvent) {
    const rigid = this.gameObject.getBehaviour(RigidBody);
    let velocity = rigid.b2RigidBody.GetLinearVelocity();

    switch (event.code) {
      case 'ArrowLeft':
        rigid.b2RigidBody.SetLinearVelocity({ x: -this.speed, y: velocity.y });
        break;
      case 'ArrowRight':
        rigid.b2RigidBody.SetLinearVelocity({ x: this.speed, y: velocity.y });
        break;
      case 'KeyC':
        if (this.jumpCount < 2) {
          rigid.b2RigidBody.SetLinearVelocity({ x: velocity.x, y: this.jumpForce });
          this.jumpCount++;
          this.isGrounded = false;
        }
        break;
    }
  }

  handleKeyUp(event: KeyboardEvent) {
    const rigid = this.gameObject.getBehaviour(RigidBody);
    let velocity = rigid.b2RigidBody.GetLinearVelocity();

    switch (event.code) {
      case 'ArrowLeft':
      case 'ArrowRight':
        // 实现缓冲停止效果
        rigid.b2RigidBody.SetLinearVelocity({ x: velocity.x * 0.5, y: velocity.y });
        break;
      case 'KeyC':
        break;
    }
  }

  onUpdate() {
    const rigid = this.gameObject.getBehaviour(RigidBody);
    const velocity = rigid.b2RigidBody.GetLinearVelocity();

    // 检测是否接触地面，假设y速度非常小（接近于0）时视为接触地面
    if (Math.abs(velocity.y) < 0.01) {
      this.isGrounded = true;
      this.jumpCount = 0;
    } else {
      this.isGrounded = false;
    }

    // 添加水平缓冲停止效果
    if (velocity.x !== 0) {
      rigid.b2RigidBody.SetLinearVelocity({ x: velocity.x * 0.95, y: velocity.y });
    }
  }

  onDestroy() {
    document.removeEventListener('keydown', this.handleKeyDown.bind(this));
    document.removeEventListener('keyup', this.handleKeyUp.bind(this));
  }
}
