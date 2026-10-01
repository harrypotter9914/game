import { GameObject, RigidBody, Transform, getGameObjectById } from "../../lib/mygameengine";
import { b2Vec2 } from "@flyover/box2d";
import { EnemyHealthStateMachine } from "./EnemyHealthStateMachine";
import { Enemy1HealthStateMachine } from "./Enemy1HealthStateMachine";
import { Enemy2HealthStateMachine } from "./Enemy2HealthStateMachine";
import { Enemy3HealthStateMachine } from "./Enemy3HealthStateMachine";
import { Enemy4HealthStateMachine } from "./Enemy4HealthStateMachine";
import { Enemy5HealthStateMachine } from "./Enemy5HealthStateMachine";
import { Enemy6HealthStateMachine } from "./Enemy6HealthStateMachine";
import { EnemyPrefabBinding } from "../bindings/EnemyPrefabBinding";
import { Enemy1PrefabBinding } from "../bindings/Enemy1PrefabBinding";
import { Enemy2PrefabBinding } from "../bindings/Enemy2PrefabBinding";
import { Enemy3PrefabBinding } from "../bindings/Enemy3PrefabBinding";
import { Enemy4PrefabBinding } from "../bindings/Enemy4PrefabBinding";
import { Enemy5PrefabBinding } from "../bindings/Enemy5PrefabBinding";
import { Enemy6PrefabBinding } from "../bindings/Enemy6PrefabBinding";
import { Walkable } from "./Walkable";

type EnemyHealthBehaviour =
  | EnemyHealthStateMachine
  | Enemy1HealthStateMachine
  | Enemy2HealthStateMachine
  | Enemy3HealthStateMachine
  | Enemy4HealthStateMachine
  | Enemy5HealthStateMachine
  | Enemy6HealthStateMachine;

type EnemyBindingBehaviour =
  | EnemyPrefabBinding
  | Enemy1PrefabBinding
  | Enemy2PrefabBinding
  | Enemy3PrefabBinding
  | Enemy4PrefabBinding
  | Enemy5PrefabBinding
  | Enemy6PrefabBinding;

export function getEnemyBinding(gameObject: GameObject): EnemyBindingBehaviour | null {
  return (
    gameObject.getBehaviour(EnemyPrefabBinding) ||
    gameObject.getBehaviour(Enemy1PrefabBinding) ||
    gameObject.getBehaviour(Enemy2PrefabBinding) ||
    gameObject.getBehaviour(Enemy3PrefabBinding) ||
    gameObject.getBehaviour(Enemy4PrefabBinding) ||
    gameObject.getBehaviour(Enemy5PrefabBinding) ||
    gameObject.getBehaviour(Enemy6PrefabBinding)
  );
}

export function getEnemyHealth(gameObject: GameObject): EnemyHealthBehaviour | null {
  return (
    gameObject.getBehaviour(EnemyHealthStateMachine) ||
    gameObject.getBehaviour(Enemy1HealthStateMachine) ||
    gameObject.getBehaviour(Enemy2HealthStateMachine) ||
    gameObject.getBehaviour(Enemy3HealthStateMachine) ||
    gameObject.getBehaviour(Enemy4HealthStateMachine) ||
    gameObject.getBehaviour(Enemy5HealthStateMachine) ||
    gameObject.getBehaviour(Enemy6HealthStateMachine)
  );
}

export function resolveEnemyAction(gameObject: GameObject): string {
  return getEnemyBinding(gameObject)?.action || "rightattack";
}

function getEnemyHitProfile(source: GameObject, action: string) {
  if (source.getBehaviour(Enemy4PrefabBinding)) {
    return { range: 190, vertical: 160, requireFacing: true };
  }
  if (source.getBehaviour(Enemy5PrefabBinding)) {
    return { range: 150, vertical: 170, requireFacing: true };
  }
  if (source.getBehaviour(Enemy6PrefabBinding)) {
    return { range: 210, vertical: 180, requireFacing: true };
  }
  if (source.getBehaviour(Enemy3PrefabBinding)) {
    return { range: 170, vertical: 150, requireFacing: true };
  }
  if (source.getBehaviour(Enemy2PrefabBinding)) {
    return { range: 360, vertical: 120, requireFacing: true };
  }
  if (source.getBehaviour(Enemy1PrefabBinding)) {
    return { range: 125, vertical: 120, requireFacing: true };
  }
  return { range: 110, vertical: 110, requireFacing: true };
}

export function damageEnemy(gameObject: GameObject, amount: number, attackDirection: string): boolean {
  const binding = getEnemyBinding(gameObject);
  const health = getEnemyHealth(gameObject);
  if (!binding || !health || !gameObject.active) {
    return false;
  }

  const isShieldEnemy = !!gameObject.getBehaviour(Enemy1PrefabBinding);
  if (isShieldEnemy) {
    const shieldFacingRight = binding.action.includes("right");
    const hitFromFront =
      (shieldFacingRight && attackDirection === "left") ||
      (!shieldFacingRight && attackDirection === "right");
    if (hitFromFront) {
      binding.action = shieldFacingRight ? "rightpatrol" : "leftpatrol";
      return false;
    }
  }

  binding.action = attackDirection === "left" ? "rightsufferattack" : "leftsufferattack";
  health.decreaseHealth(amount);
  const rigidBody = gameObject.getBehaviour(RigidBody);
  if (rigidBody) {
    const impulse = attackDirection === "left" ? -8 : 8;
    rigidBody.b2RigidBody.ApplyLinearImpulse(
      new b2Vec2(impulse, 0),
      rigidBody.b2RigidBody.GetWorldCenter(),
      true,
    );
  }
  return true;
}

export function tryHitPlayer(source: GameObject, damage: number, customAction?: string) {
  const player = getGameObjectById("mainRole");
  if (!player) {
    return;
  }

  const walkable = player.getBehaviour(Walkable);
  const playerTransform = player.getBehaviour(Transform);
  const sourceTransform = source.getBehaviour(Transform);
  if (!walkable || !playerTransform || !sourceTransform || !source.active) {
    return;
  }

  const action = customAction || resolveEnemyAction(source);
  const profile = getEnemyHitProfile(source, action);
  const dx = playerTransform.x - sourceTransform.x;
  const dy = Math.abs(playerTransform.y - sourceTransform.y);
  const horizontal = Math.abs(dx);
  const facingLeft = action.includes("left");
  const inFront = facingLeft ? dx <= 0 : dx >= 0;

  if (horizontal > profile.range || dy > profile.vertical) {
    return;
  }

  if (profile.requireFacing && !inFront) {
    return;
  }

  walkable.queueIncomingHit(damage, action, source);
}
