export type RuneId =
  | "Romance"
  | "Rings"
  | "Star"
  | "Sun"
  | "Flight"
  | "Woman"
  | "Harvest"
  | "Moon";

export type AbilityId = "shockwave" | "crystalDash" | "wallJump" | "doubleJump";

export const ALL_RUNES: RuneId[] = [
  "Romance",
  "Rings",
  "Star",
  "Sun",
  "Flight",
  "Woman",
  "Harvest",
  "Moon",
];

const DEFAULT_UNLOCKED_ABILITIES: AbilityId[] = ["wallJump", "doubleJump"];

const RUNE_NAME_MAP: Record<string, RuneId> = {
  romance: "Romance",
  rings: "Rings",
  star: "Star",
  sun: "Sun",
  flight: "Flight",
  woman: "Woman",
  harvest: "Harvest",
  moon: "Moon",
};

type SessionState = {
  initialized: boolean;
  paused: boolean;
  health: number;
  maxHealth: number;
  mana: number;
  maxMana: number;
  collectedRunes: RuneId[];
  activeSlots: Array<RuneId | null>;
  unlockedAbilities: AbilityId[];
  respawnX: number;
  respawnY: number;
  womanReviveReady: boolean;
};

const state: SessionState = {
  initialized: false,
  paused: false,
  health: 6,
  maxHealth: 6,
  mana: 3,
  maxMana: 3,
  collectedRunes: [],
  activeSlots: [null, null, null, null],
  unlockedAbilities: [...DEFAULT_UNLOCKED_ABILITIES],
  respawnX: 100,
  respawnY: 100,
  womanReviveReady: false,
};

function clamp(value: number, min: number, max: number): number {
  return Math.max(min, Math.min(max, value));
}

function uniquePush<T>(items: T[], value: T) {
  if (!items.includes(value)) {
    items.push(value);
  }
}

function isRuneActiveInternal(rune: RuneId): boolean {
  return state.activeSlots.includes(rune);
}

function syncDerivedStats() {
  let maxHealth = 6;

  if (isRuneActiveInternal("Romance")) {
    maxHealth += 1;
  }

  state.maxHealth = maxHealth;
  state.health = clamp(state.health, 0, state.maxHealth);
  state.mana = clamp(state.mana, 0, state.maxMana);
  state.womanReviveReady = isRuneActiveInternal("Woman");
}

export class PlayerSession {
  static ensureInitialized() {
    if (state.initialized) {
      return;
    }

    state.initialized = true;
    PlayerSession.resetForNewGame();
  }

  static normalizeRuneName(name: string | undefined | null): RuneId | null {
    if (!name) {
      return null;
    }

    return RUNE_NAME_MAP[name.trim().toLowerCase()] ?? null;
  }

  static resetForNewGame() {
    state.initialized = true;
    state.paused = false;
    state.health = 6;
    state.maxHealth = 6;
    state.mana = 3;
    state.maxMana = 3;
    state.collectedRunes = [];
    state.activeSlots = [null, null, null, null];
    state.unlockedAbilities = [...DEFAULT_UNLOCKED_ABILITIES];
    state.respawnX = 100;
    state.respawnY = 100;
    state.womanReviveReady = false;
    syncDerivedStats();
  }

  static isPaused(): boolean {
    return state.paused;
  }

  static setPaused(paused: boolean) {
    state.paused = paused;
  }

  static getHealth(): number {
    PlayerSession.ensureInitialized();
    return state.health;
  }

  static getMaxHealth(): number {
    PlayerSession.ensureInitialized();
    return state.maxHealth;
  }

  static setHealth(health: number) {
    PlayerSession.ensureInitialized();
    state.health = clamp(health, 0, state.maxHealth);
  }

  static increaseHealth(amount: number) {
    PlayerSession.setHealth(state.health + amount);
  }

  static getMana(): number {
    PlayerSession.ensureInitialized();
    return state.mana;
  }

  static getMaxMana(): number {
    PlayerSession.ensureInitialized();
    return state.maxMana;
  }

  static setMana(mana: number) {
    PlayerSession.ensureInitialized();
    state.mana = clamp(mana, 0, state.maxMana);
  }

  static increaseMana(amount: number) {
    PlayerSession.setMana(state.mana + amount);
  }

  static tryConsumeMana(amount: number): boolean {
    PlayerSession.ensureInitialized();
    if (state.mana < amount) {
      return false;
    }

    state.mana -= amount;
    return true;
  }

  static getCollectedRunes(): RuneId[] {
    PlayerSession.ensureInitialized();
    return [...state.collectedRunes];
  }

  static getActiveSlots(): Array<RuneId | null> {
    PlayerSession.ensureInitialized();
    return [...state.activeSlots];
  }

  static getActiveRunes(): RuneId[] {
    return PlayerSession.getActiveSlots().filter(Boolean) as RuneId[];
  }

  static isRuneCollected(rune: RuneId): boolean {
    PlayerSession.ensureInitialized();
    return state.collectedRunes.includes(rune);
  }

  static isRuneActive(rune: RuneId): boolean {
    PlayerSession.ensureInitialized();
    return isRuneActiveInternal(rune);
  }

  static collectRune(rune: RuneId) {
    PlayerSession.ensureInitialized();
    uniquePush(state.collectedRunes, rune);
    if (!isRuneActiveInternal(rune)) {
      const emptyIndex = state.activeSlots.findIndex((slot) => slot === null);
      if (emptyIndex >= 0) {
        state.activeSlots[emptyIndex] = rune;
      }
    }
    syncDerivedStats();
  }

  static activateRune(rune: RuneId): boolean {
    PlayerSession.ensureInitialized();
    if (!state.collectedRunes.includes(rune)) {
      return false;
    }
    if (isRuneActiveInternal(rune)) {
      return false;
    }

    const emptyIndex = state.activeSlots.findIndex((slot) => slot === null);
    if (emptyIndex < 0) {
      return false;
    }

    state.activeSlots[emptyIndex] = rune;
    syncDerivedStats();
    return true;
  }

  static deactivateRune(rune: RuneId): boolean {
    PlayerSession.ensureInitialized();
    const index = state.activeSlots.findIndex((slot) => slot === rune);
    if (index < 0) {
      return false;
    }
    state.activeSlots[index] = null;
    syncDerivedStats();
    return true;
  }

  static deactivateRuneAt(slotIndex: number): boolean {
    PlayerSession.ensureInitialized();
    if (slotIndex < 0 || slotIndex >= state.activeSlots.length || !state.activeSlots[slotIndex]) {
      return false;
    }
    state.activeSlots[slotIndex] = null;
    syncDerivedStats();
    return true;
  }

  static toggleRune(rune: RuneId): boolean {
    if (PlayerSession.isRuneActive(rune)) {
      return PlayerSession.deactivateRune(rune);
    }
    return PlayerSession.activateRune(rune);
  }

  static unlockAbility(ability: AbilityId) {
    PlayerSession.ensureInitialized();
    uniquePush(state.unlockedAbilities, ability);
  }

  static hasAbility(ability: AbilityId): boolean {
    PlayerSession.ensureInitialized();
    return state.unlockedAbilities.includes(ability);
  }

  static getAttackRangeMultiplier(): number {
    return isRuneActiveInternal("Sun") ? 1.4 : 1;
  }

  static getAttackSpeedMultiplier(): number {
    return isRuneActiveInternal("Star") ? 1.4 : 1;
  }

  static getAttackPowerMultiplier(): number {
    let multiplier = 1;

    if (isRuneActiveInternal("Romance")) {
      multiplier *= 0.85;
    }

    if (isRuneActiveInternal("Rings") && state.health > 0 && state.health <= Math.ceil(state.maxHealth * 0.35)) {
      multiplier *= 1.5;
    }

    return multiplier;
  }

  static hasDamageReflect(): boolean {
    return isRuneActiveInternal("Moon");
  }

  static hasCoinMagnet(): boolean {
    return isRuneActiveInternal("Flight");
  }

  static getShopDiscountMultiplier(): number {
    return isRuneActiveInternal("Harvest") ? 0.8 : 1;
  }

  static setRespawnPoint(x: number, y: number) {
    PlayerSession.ensureInitialized();
    state.respawnX = x;
    state.respawnY = y;
  }

  static getRespawnPoint() {
    PlayerSession.ensureInitialized();
    return { x: state.respawnX, y: state.respawnY };
  }

  static tryRevive(): boolean {
    PlayerSession.ensureInitialized();
    if (!state.womanReviveReady) {
      return false;
    }

    state.womanReviveReady = false;
    const slotIndex = state.activeSlots.findIndex((slot) => slot === "Woman");
    if (slotIndex >= 0) {
      state.activeSlots[slotIndex] = null;
    }
    state.health = Math.max(2, Math.ceil(state.maxHealth * 0.5));
    syncDerivedStats();
    return true;
  }
}
