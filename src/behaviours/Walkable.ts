import { Behaviour, number, RigidBody } from "../../lib/mygameengine";

export class Walkable extends Behaviour {
  @number()
  speed = 1;

  onStart() {
    this.gameObject.onClick = () => {
      const rigid = this.gameObject.getBehaviour(RigidBody);
      // rigid.b2RigidBody.SetLinearVelocity({ x: this.speed, y: 0 })
      rigid.b2RigidBody.SetLinearVelocity({ x: this.speed, y: 0 });
    };
  }
}
