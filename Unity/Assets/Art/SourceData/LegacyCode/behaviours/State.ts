import { b2Vec2 } from "@flyover/box2d";
import { RigidBody } from "../../lib/mygameengine";
import { Walkable } from "./Walkable";
import { HealthStateMachine } from "./HealthState";
import { AudioBehaviour } from "../../lib/mygameengine";
import { PlayerSession } from "./PlayerSession";

function handleMovement(event: KeyboardEvent, walkable: Walkable) {
  const rigid = walkable.gameObject.getBehaviour(RigidBody);
  if (!rigid) {
    return;
  }

  switch (event.code) {
      case 'ArrowLeft':
          rigid.b2RigidBody.SetLinearVelocity(new b2Vec2(-walkable.speed, rigid.b2RigidBody.GetLinearVelocity().y));
          walkable.lastAction = 'leftrun';
          walkable.currentAction = 'leftrun';
          walkable.mainRoleBinding!.action = 'leftrun';
          walkable.isMoving = true;
          break;
      case 'ArrowRight':
          rigid.b2RigidBody.SetLinearVelocity(new b2Vec2(walkable.speed, rigid.b2RigidBody.GetLinearVelocity().y));
          walkable.lastAction = 'rightrun';
          walkable.currentAction = 'rightrun';
          walkable.mainRoleBinding!.action = 'rightrun';
          walkable.isMoving = true;
          break;
      case 'ArrowUp':
          walkable.upArrowPressed = true;
          break;
      case 'ArrowDown':
          walkable.downArrowPressed = true;
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
      case 'ArrowUp':
          walkable.upArrowPressed = false;
          break;
      case 'ArrowDown':
          walkable.downArrowPressed = false;
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

export class HurtState extends State {
  private animationDuration: number;
  private healthStateMachine: HealthStateMachine | null;
  private damageAudio: AudioBehaviour | null = null;

  constructor(walkable: Walkable, animationDuration: number = 400) {
      super(walkable);
      this.animationDuration = animationDuration;
      this.healthStateMachine = this.walkable.gameObject.getBehaviour(HealthStateMachine);
      this.damageAudio = new AudioBehaviour();
      this.damageAudio.source = "./assets/audio/21_orc_damage_1.wav";
      this.damageAudio.setLoop(false);
      this.damageAudio.setVolume(1);
  }

  enter() {
    const damage = this.walkable.consumePendingDamage();
    if (this.healthStateMachine) {
      this.healthStateMachine.decreaseHealth(damage);
    }

    const rigidBody = this.walkable.gameObject.getBehaviour(RigidBody);
    const enemyAction = this.walkable.lastEnemyAction;

    this.damageAudio?.play();

    let impulseX: number;

    if (enemyAction.includes('left')) {
        this.walkable.mainRoleBinding!.action = 'rightsufferattack';
        impulseX = -120;
    } else {
        this.walkable.mainRoleBinding!.action = 'leftsufferattack';
        impulseX = 120;
    }

    if (rigidBody) {
      rigidBody.b2RigidBody.ApplyLinearImpulse(new b2Vec2(impulseX, 0), rigidBody.b2RigidBody.GetWorldCenter(), true);
    }

    setTimeout(() => {
        this.walkable.changeState(new GroundState(this.walkable));
    }, this.animationDuration);
  }

  handleInput(_event: KeyboardEvent) {}
  handleKeyUp(_event: KeyboardEvent) {}
  update(_duringTime: number) {}
  exit() {}
}

export class GroundState extends State {
  private healthStateMachine: HealthStateMachine | null = null;

  enter() {
    this.walkable.airJumped = false;
    this.walkable.coyoteTimer = this.walkable.coyoteTime;
    this.walkable.initialJump = true;
    this.healthStateMachine = this.walkable.gameObject.getBehaviour(HealthStateMachine);

    if (this.healthStateMachine?.isdead === true && this.walkable.lastAction.includes('left')) {
      this.walkable.mainRoleBinding!.action = 'leftdead';
      return;
    }

    if (this.healthStateMachine?.isdead === true && this.walkable.lastAction.includes('right')) {
      this.walkable.mainRoleBinding!.action = 'rightdead';
      return;
    }

    if (this.walkable.lastAction === 'leftrun' || this.walkable.lastAction === 'leftjump') {
      this.walkable.mainRoleBinding!.action = this.walkable.isMoving ? 'leftrun' : 'leftidle';
      this.walkable.lastAction = this.walkable.isMoving ? 'leftrun' : 'leftidle';
    } else if (this.walkable.lastAction === 'rightrun' || this.walkable.lastAction === 'rightjump') {
      this.walkable.mainRoleBinding!.action = this.walkable.isMoving ? 'rightrun' : 'rightidle';
      this.walkable.lastAction = this.walkable.isMoving ? 'rightrun' : 'rightidle';
    }
  }

  handleInput(event: KeyboardEvent) {
    const rigid = this.walkable.gameObject.getBehaviour(RigidBody);
    handleMovement(event, this.walkable);
    if (event.code === 'KeyC' && rigid) {
        if (this.walkable.lastAction === 'leftrun' || this.walkable.lastAction === 'leftidle') {
          this.walkable.mainRoleBinding!.action = 'leftjump';
          this.walkable.lastAction = 'leftjump';
        } else if (this.walkable.lastAction === 'rightrun' || this.walkable.lastAction === 'rightidle') {
          this.walkable.mainRoleBinding!.action = 'rightjump';
          this.walkable.lastAction = 'rightjump';
        }
        this.walkable.jump(rigid);
        this.walkable.initialJump = false;
    }
  }

  handleKeyUp(event: KeyboardEvent) {
    handleMovementKeyUp(event, this.walkable);
  }

  update(_duringTime: number) {
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
    if (event.code === 'KeyC' && rigid) {
        if (this.walkable.initialJump === true && this.walkable.coyoteTimer > 0) {
            this.walkable.mainRoleBinding!.action = this.walkable.lastAction === 'leftrun' ? 'leftjump' : 'rightjump';
            this.walkable.jump(rigid);
            this.walkable.initialJump = false;
        } else if (!this.walkable.airJumped && PlayerSession.hasAbility('doubleJump')) {
          if (this.walkable.lastAction === 'leftrun' || this.walkable.lastAction === 'leftjump') {
            this.walkable.mainRoleBinding!.action = 'leftjump';
            this.walkable.lastAction = 'leftjump';
          } else {
            this.walkable.mainRoleBinding!.action = 'rightjump';
            this.walkable.lastAction = 'rightjump';
          }
            this.walkable.jump(rigid, 0.7);
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

    if (event.type === 'keydown') {
      if (event.code === 'ArrowLeft') {
          this.walkable.leftArrowPressed = true;
      } else if (event.code === 'ArrowRight') {
          this.walkable.rightArrowPressed = true;
      } else if (event.code === 'ArrowUp') {
          this.walkable.upArrowPressed = true;
      } else if (event.code === 'ArrowDown') {
          this.walkable.downArrowPressed = true;
      } else if (event.code === 'KeyC' && rigid && PlayerSession.hasAbility('wallJump')) {
          if (this.walkable.leftArrowPressed || this.walkable.rightArrowPressed) {
              this.walkable.wallJump(rigid);
              this.walkable.changeState(new AirState(this.walkable));
          }
      }
    }

    handleMovement(event, this.walkable);
  }

  handleKeyUp(event: KeyboardEvent) {
      if (event.code === 'ArrowLeft') {
        this.walkable.leftArrowPressed = false;
      } else if (event.code === 'ArrowRight') {
        this.walkable.rightArrowPressed = false;
      } else if (event.code === 'ArrowUp') {
        this.walkable.upArrowPressed = false;
      } else if (event.code === 'ArrowDown') {
        this.walkable.downArrowPressed = false;
      }
  }

  update(_duringTime: number) {
    if (this.walkable.isGrounded === true && this.walkable.isOnWall === true) {
      this.walkable.changeState(new CornerState(this.walkable));
    } else if (this.walkable.isGrounded === false && this.walkable.isOnWall === false) {
      this.walkable.changeState(new AirState(this.walkable));
    }
  }

  exit() {}
}

export class CornerState extends State {
  enter() {}

  handleInput(event: KeyboardEvent) {
    const rigid = this.walkable.gameObject.getBehaviour(RigidBody);
    handleMovement(event, this.walkable);
    if (event.code === 'KeyC' && rigid) {
        this.walkable.mainRoleBinding!.action = this.walkable.lastAction === 'leftrun' ? 'leftjump' : 'rightjump';
        this.walkable.jump(rigid);
        this.walkable.initialJump = false;
    }
  }

  handleKeyUp(event: KeyboardEvent) {
    handleMovementKeyUp(event, this.walkable);
  }

  update(_duringTime: number) {
    if (this.walkable.isGrounded === true && this.walkable.isOnWall === false) {
      this.walkable.changeState(new GroundState(this.walkable));
    } else if (this.walkable.isOnWall === true && this.walkable.isGrounded === false) {
      this.walkable.changeState(new WallState(this.walkable));
    }
  }

  exit() {}
}

export class DoubleJumpedState extends State {
  enter() {}
  handleInput(event: KeyboardEvent) {
    handleMovement(event, this.walkable);
  }
  handleKeyUp(event: KeyboardEvent) {
    handleMovementKeyUp(event, this.walkable);
  }
  update(_duringTime: number) {
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
