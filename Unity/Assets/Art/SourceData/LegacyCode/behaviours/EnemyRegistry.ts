import { GameObject } from "../../lib/mygameengine";

const enemyRegistry = new Set<GameObject>();

export class EnemyRegistry {
  static add(enemy: GameObject) {
    enemyRegistry.add(enemy);
  }

  static remove(enemy: GameObject) {
    enemyRegistry.delete(enemy);
  }

  static getAll(): GameObject[] {
    return Array.from(enemyRegistry).filter((enemy) => enemy.active);
  }
}
