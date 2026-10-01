import {
  RigidBody,
  AnimationRenderer,
  number,
  string,
} from "../../lib/mygameengine";
import { binding, Binding, makeBinding, prefab } from "./Binding";

const ACTION_MAP: Record<string, string> = {
  leftpatrol: "action1",
  rightpatrol: "action2",
  leftrun: "action5",
  rightrun: "action6",
  leftattack: "action3",
  rightattack: "action4",
  leftsufferattack: "action7",
  rightsufferattack: "action7",
  leftdead: "action8",
  rightdead: "action8",
};

@prefab("./assets/prefabs/enemy1.yaml")
export class Enemy1PrefabBinding extends Binding {
  @string()
  @binding((prefabRoot, value) => {
    const action = ACTION_MAP[value] || value;
    prefabRoot.children[0].getBehaviour(AnimationRenderer).action = action;
  })
  action: string;

  @number()
  @binding((prefabRoot, value) => {
    prefabRoot.getBehaviour(RigidBody).x = value;
  })
  x: number;

  @number()
  @binding((prefabRoot, value) => {
    prefabRoot.getBehaviour(RigidBody).y = value;
  })
  y: number;

  constructor() {
    super();
    makeBinding(this);
  }
}
