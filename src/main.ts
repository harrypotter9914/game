import { registerBehaviourClass, AnimationRenderer, Binding, BitmapRenderer, BoxCollider, Camera, CapsuleCollider, CircleCollider, EdgeCollider, PhysicsWorld, RigidBody, ShapeRectRenderer, TextRenderer, Transform, GameEngine } from "../lib/mygameengine";
import { Attackable } from "./behaviours/Attackable";
import { Enemy } from "./behaviours/Enemy";
import { GameManager } from "./behaviours/GameManager";
import { HealthStateMachine } from "./behaviours/HealthState";
import { MainMenuStateMachine } from "./behaviours/MainMenuState";
import { ManaStateMachine } from "./behaviours/ManaState";
import { Walkable } from "./behaviours/Walkable";
import { BlockPrefabBinding } from "./bindings/BlockPrefabBinding";
import { EnemyPrefabBinding } from "./bindings/EnemyPrefabBinding";
import { MainRolePrefabBinding } from "./bindings/MainRolePrefabBinding";
import { MaoQiPrefabBinding } from "./bindings/MaoQiPrefabBinding";
import { WallPrefabBinding } from "./bindings/WallPrefabBinding";
registerBehaviourClass(AnimationRenderer);
registerBehaviourClass(Binding);
registerBehaviourClass(BitmapRenderer);
registerBehaviourClass(BoxCollider);
registerBehaviourClass(Camera);
registerBehaviourClass(CapsuleCollider);
registerBehaviourClass(CircleCollider);
registerBehaviourClass(EdgeCollider);
registerBehaviourClass(PhysicsWorld);
registerBehaviourClass(RigidBody);
registerBehaviourClass(ShapeRectRenderer);
registerBehaviourClass(TextRenderer);
registerBehaviourClass(Transform);
registerBehaviourClass(Attackable);
registerBehaviourClass(Enemy);
registerBehaviourClass(GameManager);
registerBehaviourClass(HealthStateMachine);
registerBehaviourClass(MainMenuStateMachine);
registerBehaviourClass(ManaStateMachine);
registerBehaviourClass(Walkable);
registerBehaviourClass(Binding);
registerBehaviourClass(BlockPrefabBinding);
registerBehaviourClass(EnemyPrefabBinding);
registerBehaviourClass(MainRolePrefabBinding);
registerBehaviourClass(MaoQiPrefabBinding);
registerBehaviourClass(WallPrefabBinding);
const engine = new GameEngine();
engine.start();
