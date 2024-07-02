import { Behaviour, number, RigidBody } from "../../lib/mygameengine";

export class Walkable extends Behaviour {
  @number()
  speed = 1;

  @number()
  jumpForce = 5;

  private rigid: RigidBody;

  onStart() {
    this.rigid = this.gameObject.getBehaviour(RigidBody);
    document.addEventListener('keydown', this.handleKeyDown.bind(this));
  }

  handleKeyDown(event: KeyboardEvent) {
    switch (event.code) {
      case 'ArrowLeft':
        this.rigid.b2RigidBody.SetLinearVelocity({ x: -this.speed, y: this.rigid.b2RigidBody.GetLinearVelocity().y });
        break;
      case 'ArrowRight':
        this.rigid.b2RigidBody.SetLinearVelocity({ x: this.speed, y: this.rigid.b2RigidBody.GetLinearVelocity().y });
        break;
      case 'KeyC':
        this.rigid.b2RigidBody.SetLinearVelocity({ x: this.rigid.b2RigidBody.GetLinearVelocity().x, y: this.jumpForce });
        break;
    }
  }

  onDestroy() {
    document.removeEventListener('keydown', this.handleKeyDown.bind(this));
  }
}
