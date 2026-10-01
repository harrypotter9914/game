import {
    RigidBody,
    AnimationRenderer,
    number,
    string,
    Transform,
    BoxCollider,
  } from "../../lib/mygameengine";
  import { binding, Binding, makeBinding, prefab } from "./Binding";
  
  @prefab("./assets/prefabs/maoQi.yaml")
  export class MaoQiPrefabBinding extends Binding {
  
  
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
  
    @number()
    @binding((prefabRoot, value) => {
      prefabRoot.getBehaviour(Transform).rotation = value;
    })
    rotation: number;

    @number()
    @binding((prefabRoot, value) => {
      prefabRoot.getBehaviour(Transform).scaleX = value;
    })
    scaleX : number;

    @number()
    @binding((prefabRoot, value) => {
      prefabRoot.getBehaviour(Transform).scaleY = value;
    })
    scaleY : number;

    constructor() {
      super();
      makeBinding(this);
    }
  }
  