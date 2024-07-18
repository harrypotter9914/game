import { b2Vec2 } from "@flyover/box2d";
import { RigidBody } from "../../lib/mygameengine";
import { Walkable } from "./Walkable";
import { HealthStateMachine } from "./HealthState";
import { AudioBehaviour, AudioSystem } from "../../lib/mygameengine";


function handleMovement(event: KeyboardEvent, walkable: Walkable) {
  const rigid = walkable.gameObject.getBehaviour(RigidBody);
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

  constructor(walkable: Walkable, animationDuration: number = 500) {
      super(walkable);
      this.animationDuration = animationDuration;
      this.healthStateMachine = this.walkable.gameObject.getBehaviour(HealthStateMachine);
      // 初始化音频行为
      this.damageAudio = new AudioBehaviour();
      this.damageAudio.source = "./assets/audio/21_orc_damage_1.wav"; 
      this.damageAudio.setLoop(false); // 设置不循环播放
      this.damageAudio.setVolume(1);
  }

  enter() {

    // 减少血量
    if (this.healthStateMachine) {
      this.healthStateMachine.decreaseHealth(1);
      console.log(`Health: ${this.healthStateMachine.currentHealth}`);
    }

    console.log("Entering Hurt State");
    const rigidBody = this.walkable.gameObject.getBehaviour(RigidBody);
    const enemyAction = this.walkable.lastEnemyAction;

    console.log(`lastEnemyAction: ${enemyAction}`);
  
    if (this.damageAudio) {
      this.damageAudio.play();
  }

    let impulseX: number

    if (enemyAction.includes('left')) {
        this.walkable.mainRoleBinding!.action = 'rightsufferattack';
        impulseX = -150;
    } else {
        this.walkable.mainRoleBinding!.action = 'leftsufferattack';
        impulseX = 150;
    }
    rigidBody.b2RigidBody.ApplyLinearImpulse(new b2Vec2(impulseX, 0), rigidBody.b2RigidBody.GetWorldCenter(), true);


      // 设定定时器在动画结束后恢复到地面状态
      setTimeout(() => {
          this.walkable.changeState(new GroundState(this.walkable));
      }, this.animationDuration);
  }

  handleInput(event: KeyboardEvent) {
      // 在受击状态中不处理任何输入
  }

  handleKeyUp(event: KeyboardEvent) {
      // 在受击状态中不处理任何输入
  }

  update(duringTime: number) {
      // 在受击状态中不更新其他逻辑
  }

  exit() {
      console.log("Exiting Hurt State");
  }
}

export class GroundState extends State {
  private healthStateMachine: HealthStateMachine | null;
  enter() {
    this.walkable.airJumped = false; // 重置空中跳跃状态
    this.walkable.coyoteTimer = this.walkable.coyoteTime; // 重置土狼时间
    this.walkable.initialJump = true; // 重置初始跳跃状态
    this.healthStateMachine = this.walkable.gameObject.getBehaviour(HealthStateMachine);
    if (this.healthStateMachine.isdead === true && this.walkable.lastAction.includes('left')) {
      this.walkable.mainRoleBinding!.action = 'leftdead';
    } else if (this.healthStateMachine.isdead === true && this.walkable.lastAction.includes('right')) {
      this.walkable.mainRoleBinding!.action = 'rightdead';
    }
    if (this.walkable.lastAction === 'leftrun' || this.walkable.lastAction === 'leftjump') {
      this.walkable.mainRoleBinding!.action = 'leftidle';
      this.walkable.lastAction = 'leftidle';
      if (this.walkable.isMoving === true) {
        this.walkable.mainRoleBinding!.action = 'leftrun';
        this.walkable.lastAction = 'leftrun';
      }
    } else if (this.walkable.lastAction === 'rightrun' || this.walkable.lastAction === 'rightjump') {
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
        } else if (this.walkable.lastAction === 'rightrun' || this.walkable.lastAction === 'rightidle') {
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
          } else if (this.walkable.lastAction === 'rightrun' || this.walkable.lastAction === 'rightjump') {
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
      } else if (event.code === 'KeyC') {
          // 按下C键时，检测左右箭头键的状态决定墙跳方向
          if (this.walkable.leftArrowPressed || this.walkable.rightArrowPressed) {
              console.log('wall jump');
              this.walkable.wallJump(rigid);
              this.walkable.changeState(new AirState(this.walkable));
          }
      }
  } else if (event.type === 'keyup') {
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

    handleMovement(event, this.walkable);
    }

  handleKeyUp(event: KeyboardEvent) {
      // 处理键盘抬起事件
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

  update(duringTime: number) {
    const rigid = this.walkable.gameObject.getBehaviour(RigidBody);


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