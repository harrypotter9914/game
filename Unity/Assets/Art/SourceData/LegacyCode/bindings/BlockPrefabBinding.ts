import { RigidBody, number } from "../../lib/mygameengine";
import { Binding, binding, makeBinding, prefab } from "./Binding";
@prefab("./assets/prefabs/block.yaml")
export class BlockPrefabBinding extends Binding {
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
