import {
  RigidBody,
  AnimationRenderer,
  number,
  string,
} from "../../lib/mygameengine";
import { binding, Binding, makeBinding, prefab } from "./Binding";

const ACTION_MAP: Record<string, string> = {
  leftattack: "action1",
  rightattack: "action2",
  leftsufferattack: "action3",
  rightsufferattack: "action4",
  leftpatrol: "action5",
  rightpatrol: "action6",
  leftrun: "action7",
  rightrun: "action8",
  leftdead: "action9",
  rightdead: "action10",
};

@prefab("./assets/prefabs/enemy3.yaml")
export class Enemy3PrefabBinding extends Binding {
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
