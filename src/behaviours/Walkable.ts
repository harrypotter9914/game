import { Behaviour, number, RigidBody } from "../../lib/mygameengine";

export class Walkable extends Behaviour {
  @number()
  speed = 1;

  @number()
  jumpForce = 5;

  onStart() {
    document.addEventListener('keydown', this.handleKeyDown.bind(this));
    document.addEventListener('keyup', this.handleKeyUp.bind(this));
  }

  handleKeyDown(event: KeyboardEvent) {
    const rigid = this.gameObject.getBehaviour(RigidBody);

    switch (event.code) {
      case 'ArrowLeft':
        rigid.b2RigidBody.SetLinearVelocity({ x: -this.speed, y: 0 });
        break;
      case 'ArrowRight':
        rigid.b2RigidBody.SetLinearVelocity({ x: this.speed, y: 0 });
        break;
      case 'KeyC':
        rigid.b2RigidBody.SetLinearVelocity({ x: rigid.b2RigidBody.GetLinearVelocity().x, y: this.jumpForce });
        break;
    }
  }

  handleKeyUp(event: KeyboardEvent) {
    const rigid = this.gameObject.getBehaviour(RigidBody);

    switch (event.code) {
      case 'ArrowLeft':
        rigid.b2RigidBody.SetLinearVelocity({ x: 0, y: 0 });
        break;
      case 'ArrowRight':
        rigid.b2RigidBody.SetLinearVelocity({ x: 0, y: 0 });
        break;
      case 'KeyC':
        break;
    }
  }

  onDestroy() {
    document.removeEventListener('keydown', this.handleKeyDown.bind(this));
    document.removeEventListener('keyup', this.handleKeyUp.bind(this));
  }
}

