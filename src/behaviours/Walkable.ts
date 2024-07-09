import { Behaviour, number, RigidBody, GameObject, PhysicsSystem } from "../../lib/mygameengine";
import { MainRolePrefabBinding } from "../bindings/MainRolePrefabBinding"; 
import { b2Vec2, b2Contact, b2ContactListener } from "@flyover/box2d"; 
import { State, GroundState } from "../behaviours/State";

class CustomContactListener extends b2ContactListener {
  private walkable: Walkable;

  constructor(walkable: Walkable) {
    super();
    this.walkable = walkable;
  }

  BeginContact(contact: b2Contact) {
    this.handleContact(contact, true);
  }

  EndContact(contact: b2Contact) {
    this.handleContact(contact, false);
  }

  private handleContact(contact: b2Contact, begin: boolean) {
    const fixtureA = contact.GetFixtureA();
    const fixtureB = contact.GetFixtureB();
    const bodyA = fixtureA.GetBody();
    const bodyB = fixtureB.GetBody();
    const gameObjectA = bodyA.GetUserData() as GameObject;
    const gameObjectB = bodyB.GetUserData() as GameObject;

    if (gameObjectA && gameObjectB) {
      if (gameObjectA === this.walkable.gameObject || gameObjectB === this.walkable.gameObject) {
        const other = gameObjectA === this.walkable.gameObject ? gameObjectB : gameObjectA;
        if (other.tag === 'ground') {
          this.walkable.isGrounded = begin;
          if (begin) {
            this.walkable.airJumped = false; // 重置空中跳跃状态
          }
        }
        if (other.tag === 'wall') {
          this.walkable.isOnWall = begin;
        }
      }
    }
  }
}

export class Walkable extends Behaviour {
  @number()
  speed = 1;

  @number()
  jumpForce = 5;

  @number()
  wallJumpForce = 5;

  public isGrounded = false;
  public isOnWall = false;
  public airJumped = false; // 跟踪空中跳跃状态
  public mainRoleBinding: MainRolePrefabBinding | null = null;
  public lastAction: string = 'right';
  public coyoteTime = 0.1; // 小跳跃时间
  public coyoteTimer = 0;
  private currentState: State;

  constructor() {
    super();
    this.currentState = new GroundState(this); // 初始状态为地面状态
  }

  changeState(newState: State) {
    this.currentState.exit();
    this.currentState = newState;
    this.currentState.enter();
  }

  onStart() {
    this.mainRoleBinding = this.gameObject.getBehaviour(MainRolePrefabBinding);
    if (this.mainRoleBinding) {
      this.mainRoleBinding.action = this.lastAction;
    } else {
      console.warn('MainRolePrefabBinding not found on the game object.');
    }
    document.addEventListener('keydown', this.handleKeyDown.bind(this));
    document.addEventListener('keyup', this.handleKeyUp.bind(this));

    const physicsSystem = this.gameObject.engine.getSystem(PhysicsSystem);
    if (physicsSystem) {
      physicsSystem.m_world.SetContactListener(new CustomContactListener(this));
    }
  }

  handleKeyDown(event: KeyboardEvent) {
    this.currentState.handleInput(event);
  }

  handleKeyUp(event: KeyboardEvent) {
    this.currentState.handleKeyUp(event);
  }

  jump(rigid: RigidBody, multiplier: number = 1) {
    rigid.b2RigidBody.SetLinearVelocity(new b2Vec2(rigid.b2RigidBody.GetLinearVelocity().x, this.jumpForce * multiplier));
  }

  wallJump(rigid: RigidBody) {
    const direction = this.lastAction === 'left' ? 1 : -1;
    rigid.b2RigidBody.SetLinearVelocity(new b2Vec2(this.wallJumpForce * direction, this.jumpForce));
  }

  onTick(duringTime: number) {
    this.currentState.update(duringTime);

    const rigid = this.gameObject.getBehaviour(RigidBody);
    const velocity = rigid.b2RigidBody.GetLinearVelocity();
    if (velocity.x !== 0) {
      rigid.b2RigidBody.SetLinearVelocity(new b2Vec2(velocity.x * 0.95, velocity.y));
    }

    console.log(`isGrounded: ${this.isGrounded}, isOnWall: ${this.isOnWall}, airJumped: ${this.airJumped}, position: (${rigid.b2RigidBody.GetPosition().x}, ${rigid.b2RigidBody.GetPosition().y})`);
  }

  onDestroy() {
    document.removeEventListener('keydown', this.handleKeyDown.bind(this));
    document.removeEventListener('keyup', this.handleKeyUp.bind(this));
  }
}