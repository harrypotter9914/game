import { Behaviour, number, RigidBody, BoxCollider, Transform } from "../../lib/mygameengine";
import { MainRolePrefabBinding } from "../bindings/MainRolePrefabBinding"; // 假设文件路径正确
import { b2WorldManifold, b2Vec2 } from "@flyover/box2d"; // 假设路径正确

export class Walkable extends Behaviour {
  @number()
  speed = 1;

  @number()
  jumpForce = 5;

  @number()
  wallJumpForce = 5;

  private jumpCount = 0;
  private isGrounded = false;
  private isOnWall = false;
  private justWallJumped = false;
  private mainRoleBinding: MainRolePrefabBinding | null = null;
  private lastAction: string = 'right'; // 默认的action
  private coyoteTime = 0.1; // 小跳跃时间
  private coyoteTimer = 0;

  onStart() {
    this.mainRoleBinding = this.gameObject.getBehaviour(MainRolePrefabBinding);
    if (this.mainRoleBinding) {
      this.mainRoleBinding.action = this.lastAction; // 设置默认的action为right
    } else {
      console.warn('MainRolePrefabBinding not found on the game object.');
    }
    document.addEventListener('keydown', this.handleKeyDown.bind(this));
    document.addEventListener('keyup', this.handleKeyUp.bind(this));
  }

  handleKeyDown(event: KeyboardEvent) {
    const rigid = this.gameObject.getBehaviour(RigidBody);

    switch (event.code) {
      case 'ArrowLeft':
        rigid.b2RigidBody.SetLinearVelocity(new b2Vec2(-this.speed, rigid.b2RigidBody.GetLinearVelocity().y));
        this.lastAction = 'left';
        if (this.mainRoleBinding) {
          this.mainRoleBinding.action = this.lastAction;
        }
        break;
      case 'ArrowRight':
        rigid.b2RigidBody.SetLinearVelocity(new b2Vec2(this.speed, rigid.b2RigidBody.GetLinearVelocity().y));
        this.lastAction = 'right';
        if (this.mainRoleBinding) {
          this.mainRoleBinding.action = this.lastAction;
        }
        break;
      case 'KeyC':
        if (this.isGrounded || this.coyoteTimer > 0) {
          this.jump(rigid);
        } else if (this.jumpCount < 2 && !this.justWallJumped) {
          this.jump(rigid, 0.8); // 二段跳的力度略小
        } else if (this.isOnWall) {
          this.wallJump(rigid);
        }
        break;
    }
  }

  handleKeyUp(event: KeyboardEvent) {
    const rigid = this.gameObject.getBehaviour(RigidBody);

    switch (event.code) {
      case 'ArrowLeft':
      case 'ArrowRight':
        rigid.b2RigidBody.SetLinearVelocity(new b2Vec2(rigid.b2RigidBody.GetLinearVelocity().x * 0.5, rigid.b2RigidBody.GetLinearVelocity().y));
        if (this.mainRoleBinding) {
          this.mainRoleBinding.action = this.lastAction; // 松开按键时恢复到松开按键前的action
        }
        break;
      case 'KeyC':
        if (rigid.b2RigidBody.GetLinearVelocity().y < this.jumpForce / 2) {
          rigid.b2RigidBody.SetLinearVelocity(new b2Vec2(rigid.b2RigidBody.GetLinearVelocity().x, this.jumpForce / 2));
        }
        break;
    }
  }

  jump(rigid: RigidBody, multiplier: number = 1) {
    rigid.b2RigidBody.SetLinearVelocity(new b2Vec2(rigid.b2RigidBody.GetLinearVelocity().x, this.jumpForce * multiplier));
    this.jumpCount++;
  }

  wallJump(rigid: RigidBody) {
    const direction = this.lastAction === 'left' ? 1 : -1;
    rigid.b2RigidBody.SetLinearVelocity(new b2Vec2(this.wallJumpForce * direction, this.jumpForce));
    this.justWallJumped = true;
  }

  onTick(duringTime: number) {
    const rigid = this.gameObject.getBehaviour(RigidBody);
    const collider = this.gameObject.getBehaviour(BoxCollider);

    this.isGrounded = this.checkGroundCollision(collider);
    this.isOnWall = this.checkWallCollision(collider);

    if (this.isGrounded) {
      this.jumpCount = 0;
      this.coyoteTimer = this.coyoteTime;
    } else {
      this.coyoteTimer -= duringTime;
    }

    const velocity = rigid.b2RigidBody.GetLinearVelocity();
    if (velocity.x !== 0) {
      rigid.b2RigidBody.SetLinearVelocity(new b2Vec2(velocity.x * 0.95, velocity.y));
    }


    // 调试信息，输出当前状态
    console.log(`isGrounded: ${this.isGrounded}, isOnWall: ${this.isOnWall}, position: (${rigid.b2RigidBody.GetPosition().x}, ${rigid.b2RigidBody.GetPosition().y})`);
  }

  checkGroundCollision(collider: BoxCollider): boolean {
    const contacts = collider.getContacts();
    const transform = collider.gameObject.getBehaviour(Transform);
    const colliderBottom = transform.y - collider.height / 2;

    for (const contact of contacts) {
        const worldManifold = new b2WorldManifold();
        contact.GetWorldManifold(worldManifold);
        const normal = worldManifold.normal;
        const points = worldManifold.points;

        // 调试信息
        console.log(`Contact normal: (${normal.x}, ${normal.y})`);
        console.log(`Contact points: ${points.map(p => `(${p.x}, ${p.y})`).join(', ')}`);
        console.log(`Collider bottom: ${colliderBottom}`);

        for (const point of points) {
            console.log(`Point.y: ${point.y}, ColliderBottom: ${colliderBottom}`);
            // 检查碰撞点是否在角色的底部，并且法线方向正确
            if (normal.y > 0.5 && point.y <= colliderBottom) {
                return true;
            }
        }
    }
    return false;
  }

  checkWallCollision(collider: BoxCollider): boolean {
    const contacts = collider.getContacts();
    for (const contact of contacts) {
        const worldManifold = new b2WorldManifold();
        contact.GetWorldManifold(worldManifold);
        const normal = worldManifold.normal;
        const points = worldManifold.points;

        for (const point of points) {
            if (Math.abs(normal.x) > 0.5) {
                return true;
            }
        }
    }
    return false;
  }


  onDestroy() {
    document.removeEventListener('keydown', this.handleKeyDown.bind(this));
    document.removeEventListener('keyup', this.handleKeyUp.bind(this));
  }
}
