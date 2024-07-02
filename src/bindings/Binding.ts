import { GameObject, Behaviour } from "../../lib/mygameengine";

export const prefab: (prefabUrl: string) => ClassDecorator =
  (prefabUrl) => (target: any) => {
    target.__prefabUrl = prefabUrl;
    console.log(target);
  };

export const binding: (
  updater: (prefabRoot: GameObject, value: any) => void
) => PropertyDecorator = (updater) => (target: any, key) => {
  if (!target.__bindings) {
    target.__bindings = [];
  }
  target.__bindings.push({
    key,
    updater,
  });
};

export const makeBinding = (behavour: Binding) => {
  const bindings = (behavour as any).__proto__.__bindings || [];
  for (const binding of bindings) {
    const privateKey = "_" + binding.key;
    const desc = {
      get: function () {
        return this[privateKey];
      },
      set: function (v) {
        const changed = v !== this[privateKey];
        this[privateKey] = v;
        if (changed) {
          this.updateListeners.push(() => binding.updater(this.gameObject, v));
          this.invalidateProperties();
        }
      },
    };

    Object.defineProperty(behavour, binding.key, desc);
  }
};

export class Binding extends Behaviour {
  updateListeners: (() => void)[] = [];

  set active(value: boolean) {
    super.active = value;
    if (value) {
      this.updateProperties();
    }
  }

  protected invalidateProperties() {
    if (this._active) {
      this.updateProperties();
    }
  }

  protected updateProperties() {
    this.updateListeners.forEach((listener) => listener());
    this.updateListeners = [];
  }
}
