import { b2Vec2 } from "@flyover/box2d";
import { RigidBody } from "../../lib/mygameengine";
import { Walkable } from "./Walkable";

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
    switch (event.code) {
      case 'ArrowLeft':
        rigid.b2RigidBody.SetLinearVelocity(new b2Vec2(-this.walkable.speed, rigid.b2RigidBody.GetLinearVelocity().y));
        this.walkable.lastAction = 'left';
        break;
      case 'ArrowRight':
        rigid.b2RigidBody.SetLinearVelocity(new b2Vec2(this.walkable.speed, rigid.b2RigidBody.GetLinearVelocity().y));
        this.walkable.lastAction = 'right';
        break;
      case 'KeyC':
        this.walkable.jump(rigid);
        this.walkable.changeState(new AirState(this.walkable));
        break;
    }
  }

  handleKeyUp(event: KeyboardEvent) {
    const rigid = this.walkable.gameObject.getBehaviour(RigidBody);
    switch (event.code) {
      case 'ArrowLeft':
      case 'ArrowRight':
        rigid.b2RigidBody.SetLinearVelocity(new b2Vec2(rigid.b2RigidBody.GetLinearVelocity().x * 0.5, rigid.b2RigidBody.GetLinearVelocity().y));
        this.walkable.mainRoleBinding!.action = this.walkable.lastAction; // 松开按键时恢复到松开按键前的action
        break;
    }
  }

  update(duringTime: number) {
    // 在地面时，重置土狼时间
    this.walkable.coyoteTimer = this.walkable.coyoteTime;
  }

  exit() {}
}

class AirState extends State {
  enter() {}

  handleInput(event: KeyboardEvent) {
    const rigid = this.walkable.gameObject.getBehaviour(RigidBody);
    switch (event.code) {
      case 'ArrowLeft':
        rigid.b2RigidBody.SetLinearVelocity(new b2Vec2(-this.walkable.speed, rigid.b2RigidBody.GetLinearVelocity().y));
        this.walkable.lastAction = 'left';
        break;
      case 'ArrowRight':
        rigid.b2RigidBody.SetLinearVelocity(new b2Vec2(this.walkable.speed, rigid.b2RigidBody.GetLinearVelocity().y));
        this.walkable.lastAction = 'right';
        break;
      case 'KeyC':
        if (!this.walkable.airJumped) {
          this.walkable.jump(rigid, 0.6);
          this.walkable.airJumped = true;
          this.walkable.changeState(new DoubleJumpedState(this.walkable)); // 切换到二段跳后的状态
        }
        break;
    }
  }

  handleKeyUp(event: KeyboardEvent) {
    // 可以在需要时处理松开按键的逻辑
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
    const rigid = this.walkable.gameObject.getBehaviour(RigidBody);
    switch (event.code) {
      case 'KeyC':
        this.walkable.wallJump(rigid);
        this.walkable.changeState(new AirState(this.walkable));
        break;
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
    const rigid = this.walkable.gameObject.getBehaviour(RigidBody);
    switch (event.code) {
      case 'ArrowLeft':
        rigid.b2RigidBody.SetLinearVelocity(new b2Vec2(-this.walkable.speed, rigid.b2RigidBody.GetLinearVelocity().y));
        this.walkable.lastAction = 'left';
        break;
      case 'ArrowRight':
        rigid.b2RigidBody.SetLinearVelocity(new b2Vec2(this.walkable.speed, rigid.b2RigidBody.GetLinearVelocity().y));
        this.walkable.lastAction = 'right';
        break;
    }
  }

  handleKeyUp(event: KeyboardEvent) {
    const rigid = this.walkable.gameObject.getBehaviour(RigidBody);
    switch (event.code) {
      case 'ArrowLeft':
      case 'ArrowRight':
        rigid.b2RigidBody.SetLinearVelocity(new b2Vec2(rigid.b2RigidBody.GetLinearVelocity().x * 0.5, rigid.b2RigidBody.GetLinearVelocity().y));
        this.walkable.mainRoleBinding!.action = this.walkable.lastAction;
        break;
    }
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