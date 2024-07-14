import { b2Vec2 } from "@flyover/box2d";
import { RigidBody } from "../../lib/mygameengine";
import { Walkable } from "./Walkable";



function handleMovement(event: KeyboardEvent, walkable: Walkable) {
  const rigid = walkable.gameObject.getBehaviour(RigidBody);
  switch (event.code) {
      case 'ArrowLeft':
          rigid.b2RigidBody.SetLinearVelocity(new b2Vec2(-walkable.speed, rigid.b2RigidBody.GetLinearVelocity().y));
          walkable.lastAction = 'leftrun';
          walkable.mainRoleBinding!.action = 'leftrun'; 
          walkable.isMoving = true;
          break;
      case 'ArrowRight':
          rigid.b2RigidBody.SetLinearVelocity(new b2Vec2(walkable.speed, rigid.b2RigidBody.GetLinearVelocity().y));
          walkable.lastAction = 'rightrun';
          walkable.mainRoleBinding!.action = 'rightrun'; 
          walkable.isMoving = true;
          break;
  }
}

function handleMovementKeyUp(event: KeyboardEvent, walkable: Walkable) {
  switch (event.code) {
      case 'ArrowLeft':
          walkable.isMoving = false;
          walkable.mainRoleBinding!.action = 'leftidle';
        break;
      case 'ArrowRight':
          walkable.isMoving = false;
          walkable.mainRoleBinding!.action = 'rightidle';
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
    this.walkable.coyoteTimer = this.walkable.coyoteTime; // 重置土狼时间
    this.walkable.initialJump = true; // 重置初始跳跃状态

    if (this.walkable.lastAction === 'leftrun' || this.walkable.lastAction === 'leftjump') {
      this.walkable.mainRoleBinding!.action = 'leftidle';
      this.walkable.lastAction = 'leftidle';
      if (this.walkable.isMoving === true) {
        this.walkable.mainRoleBinding!.action = 'leftrun';
        this.walkable.lastAction = 'leftrun';
      }
    } else {
      this.walkable.mainRoleBinding!.action = 'rightidle';
      this.walkable.lastAction = 'rightidle';
      if (this.walkable.isMoving === true) {
        this.walkable.mainRoleBinding!.action = 'rightrun';
        this.walkable.lastAction = 'rightrun';
      }
    }
  }

  handleInput(event: KeyboardEvent) {
    const rigid = this.walkable.gameObject.getBehaviour(RigidBody);
    handleMovement(event, this.walkable);
    if (event.code === 'KeyC') {
        console.log('press c');
        if (this.walkable.lastAction === 'leftrun' || this.walkable.lastAction === 'leftidle') {
          this.walkable.mainRoleBinding!.action = 'leftjump';
          this.walkable.lastAction = 'leftjump';
        } else {
          this.walkable.mainRoleBinding!.action = 'rightjump';
          this.walkable.lastAction = 'rightjump';
        }
        this.walkable.jump(rigid);
        this.walkable.initialJump = false; // 禁用土狼时间
        console.log('jump');
    }
  }

  handleKeyUp(event: KeyboardEvent) {
    handleMovementKeyUp(event, this.walkable);
  }

  update(duringTime: number) {
    if (this.walkable.isGrounded === false) {
        this.walkable.changeState(new AirState(this.walkable));
    } else if (this.walkable.isGrounded === true && this.walkable.isOnWall === true) {
        this.walkable.changeState(new CornerState(this.walkable));
    }
  }

  exit() {}
}

export class AirState extends State {

  enter() {}

  handleInput(event: KeyboardEvent) {
   const rigid = this.walkable.gameObject.getBehaviour(RigidBody);
    handleMovement(event, this.walkable);
    if (event.code === 'KeyC') {
        // 如果是初始跳跃并且在土狼时间内，允许跳跃
        if (this.walkable.initialJump === true && this.walkable.coyoteTimer > 0) {
            this.walkable.mainRoleBinding!.action = this.walkable.lastAction === 'leftrun' ? 'leftjump' : 'rightjump';
            this.walkable.jump(rigid);
            this.walkable.initialJump = false; // 禁用土狼时间
            console.log('coyote time jump');
        } else if (this.walkable.initialJump === false && this.walkable.airJumped === false) {
          if (this.walkable.lastAction === 'leftrun' || this.walkable.lastAction === 'leftjump') {
            this.walkable.mainRoleBinding!.action = 'leftjump';
            this.walkable.lastAction = 'leftjump';
          } else {
            this.walkable.mainRoleBinding!.action = 'rightjump';
            this.walkable.lastAction = 'rightjump';
          }
            this.walkable.jump(rigid, 0.7);
            console.log('double jump');
            this.walkable.airJumped = true;
            this.walkable.changeState(new DoubleJumpedState(this.walkable));
        }
    }
  }

  handleKeyUp(event: KeyboardEvent) {
    handleMovementKeyUp(event, this.walkable);
  }

  update(duringTime: number) {
    this.walkable.coyoteTimer -= duringTime;
    console.log(this.walkable.coyoteTimer);
    if (this.walkable.isGrounded === true && this.walkable.isOnWall === false) {
        this.walkable.changeState(new GroundState(this.walkable));
    } else if (this.walkable.isOnWall === true && this.walkable.isGrounded === false) {
        this.walkable.changeState(new WallState(this.walkable));
    } else if (this.walkable.isGrounded === true && this.walkable.isOnWall === true) {
      this.walkable.changeState(new CornerState(this.walkable));
    }
  }

  exit() {}
}

export class WallState extends State {
  enter() {}

  handleInput(event: KeyboardEvent) {
    const rigid = this.walkable.gameObject.getBehaviour(RigidBody);
    handleMovement(event, this.walkable);
    if (event.code === 'KeyC') {
          console.log('wall jump');
          this.walkable.wallJump(rigid);
          this.walkable.changeState(new AirState(this.walkable));
      }
    }

  handleKeyUp(event: KeyboardEvent) {
  }

  update(duringTime: number) {
    const rigid = this.walkable.gameObject.getBehaviour(RigidBody);
    let velocity = rigid.b2RigidBody.GetLinearVelocity();

    // 在墙上时应用滑动速度
    velocity = new b2Vec2(velocity.x, -this.walkable.wallSlideSpeed);
    rigid.b2RigidBody.SetLinearVelocity(velocity);

    if (this.walkable.isGrounded === true && this.walkable.isOnWall === true) {
      this.walkable.changeState(new CornerState(this.walkable));
    }else if (this.walkable.isGrounded === false && this.walkable.isOnWall === false) {
      this.walkable.changeState(new AirState(this.walkable));
    }
  }

  exit() {}
}

export class CornerState extends State {
  enter() {

  }

  handleInput(event: KeyboardEvent) {
    // 在卡墙角状态时执行地面态的操作
    const rigid = this.walkable.gameObject.getBehaviour(RigidBody);
    handleMovement(event, this.walkable);
    if (event.code === 'KeyC') {
      this.walkable.mainRoleBinding!.action = this.walkable.lastAction === 'leftrun' ? 'leftjump' : 'rightjump';
        this.walkable.jump(rigid);
        this.walkable.initialJump = false; // 禁用土狼时间
        console.log('jump');
    }
  }

  handleKeyUp(event: KeyboardEvent) {
    handleMovementKeyUp(event, this.walkable);
  }

  update(duringTime: number) {
    // 如果角色不再同时接触墙壁和地面，退出卡墙角状态
  if (this.walkable.isGrounded === true && this.walkable.isOnWall === false) {
      this.walkable.changeState(new GroundState(this.walkable));   
    } else if (this.walkable.isOnWall === true && this.walkable.isGrounded === false) {
      this.walkable.changeState(new WallState(this.walkable));
    }
  }

  exit() {
  }
}


export class DoubleJumpedState extends State {
  enter() {}

  handleInput(event: KeyboardEvent) {
    handleMovement(event, this.walkable);
  }

  handleKeyUp(event: KeyboardEvent) {
    handleMovementKeyUp(event, this.walkable);
  }

  update(duringTime: number) {
    if (this.walkable.isGrounded === true && this.walkable.isOnWall === false) {
        this.walkable.changeState(new GroundState(this.walkable));
    } else if (this.walkable.isOnWall === true && this.walkable.isGrounded === false) {
        this.walkable.changeState(new WallState(this.walkable));
    } else if (this.walkable.isGrounded === true && this.walkable.isOnWall === true) {
      this.walkable.changeState(new CornerState(this.walkable));
    }
  }

  exit() {}
}