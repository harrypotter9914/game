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
  leftrun: "action3",
  rightrun: "action4",
  leftattack: "action5",
  rightattack: "action6",
  leftsufferattack: "action11",
  rightsufferattack: "action12",
  leftdead: "action13",
  rightdead: "action13",
};

@prefab("./assets/prefabs/enemy4.yaml")
export class Enemy4PrefabBinding extends Binding {
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
