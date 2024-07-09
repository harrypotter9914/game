import { b2Vec2 } from "@flyover/box2d";
import { RigidBody } from "../../lib/mygameengine";
import { Walkable } from "./Walkable";


function handleMovement(event: KeyboardEvent, walkable: Walkable) {
  const rigid = walkable.gameObject.getBehaviour(RigidBody);
  switch (event.code) {
      case 'ArrowLeft':
          rigid.b2RigidBody.SetLinearVelocity(new b2Vec2(-walkable.speed, rigid.b2RigidBody.GetLinearVelocity().y));
          walkable.lastAction = 'left';
          walkable.mainRoleBinding!.action = 'left'; 
          walkable.isMoving = true;
          break;
      case 'ArrowRight':
          rigid.b2RigidBody.SetLinearVelocity(new b2Vec2(walkable.speed, rigid.b2RigidBody.GetLinearVelocity().y));
          walkable.lastAction = 'right';
          walkable.mainRoleBinding!.action = 'right'; 
          walkable.isMoving = true;
          break;
  }
}

function handleMovementKeyUp(event: KeyboardEvent, walkable: Walkable) {
  switch (event.code) {
      case 'ArrowLeft':
      case 'ArrowRight':
          walkable.isMoving = false;
          walkable.mainRoleBinding!.action = walkable.lastAction;
          break;
  }
}



export abstract class State {
  protected walkable: Walkable;

  constructor(walkable: Walkable) {
    this.walkable = walkable;
  }

  abstract enter(): void;
  abstract handleInput(event: KeyboardEvent): void;
  abstract handleKeyUp(event: KeyboardEvent): void;
  abstract update(duringTime: number): void;
  abstract exit(): void;
}

export class GroundState extends State {
  enter() {
    this.walkable.airJumped = false; // 重置空中跳跃状态
  }

  handleInput(event: KeyboardEvent) {
    const rigid = this.walkable.gameObject.getBehaviour(RigidBody);
    handleMovement(event, this.walkable);
    if (event.code === 'KeyC') {
        this.walkable.jump(rigid);
        this.walkable.changeState(new AirState(this.walkable));
    }
  }

  handleKeyUp(event: KeyboardEvent) {
    handleMovementKeyUp(event, this.walkable);
  }

  update(duringTime: number) {
    this.walkable.coyoteTimer = this.walkable.coyoteTime;
  }

  exit() {}
}

class AirState extends State {
  enter() {}

  handleInput(event: KeyboardEvent) {
    const rigid = this.walkable.gameObject.getBehaviour(RigidBody);
    handleMovement(event, this.walkable);
    if (event.code === 'KeyC' && !this.walkable.airJumped) {
        this.walkable.jump(rigid, 0.6);
        this.walkable.airJumped = true;
        this.walkable.changeState(new DoubleJumpedState(this.walkable));
    }
  }

  handleKeyUp(event: KeyboardEvent) {
    handleMovementKeyUp(event, this.walkable);
  }

  update(duringTime: number) {
    this.walkable.coyoteTimer -= duringTime;
    if (this.walkable.isGrounded) {
        this.walkable.changeState(new GroundState(this.walkable));
    } else if (this.walkable.isOnWall) {
        this.walkable.changeState(new WallState(this.walkable));
    }
  }

  exit() {}
}

class WallState extends State {
  enter() {}

  handleInput(event: KeyboardEvent) {
    if (event.code === 'KeyC') {
        const rigid = this.walkable.gameObject.getBehaviour(RigidBody);
        this.walkable.wallJump(rigid);
        this.walkable.changeState(new AirState(this.walkable));
    }
  }

  handleKeyUp(event: KeyboardEvent) {
    // 可以在需要时处理松开按键的逻辑
  }

  update(duringTime: number) {
    if (!this.walkable.isOnWall) {
        this.walkable.changeState(new AirState(this.walkable));
    }
  }

  exit() {}
}

class DoubleJumpedState extends State {
  enter() {}

  handleInput(event: KeyboardEvent) {
    handleMovement(event, this.walkable);
  }

  handleKeyUp(event: KeyboardEvent) {
    handleMovementKeyUp(event, this.walkable);
  }

  update(duringTime: number) {
    if (this.walkable.isGrounded) {
        this.walkable.changeState(new GroundState(this.walkable));
    } else if (this.walkable.isOnWall) {
        this.walkable.changeState(new WallState(this.walkable));
    }
  }

  exit() {}
}