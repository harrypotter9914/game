// 道具抽象类
abstract class Item {
    abstract applyEffect(character: Character): void;
    abstract removeEffect(character: Character): void;
    abstract getDescription(): string; // 获取符文描述
}

// 各种符文类
class SunItem extends Item {
    applyEffect(character: Character): void {
        character.increaseAttackRange();
    }
    removeEffect(character: Character): void {
        character.decreaseAttackRange();
    }
    getDescription(): string {
        return "增加攻击距离。女巫审判的符文，象征着尼禄至高无上的权利。";
    }
}

class FlightItem extends Item {
    applyEffect(character: Character): void {
        character.enableCoinMagnet();
    }
    removeEffect(character: Character): void {
        character.disableCoinMagnet();
    }
    getDescription(): string {
        return "吸收附近掉落金币。你要专心仰赖耶和华，不可倚靠自己的聪明，在你一切所行的事上，都要认定他，他必指引你的路。旧约《箴言》3:5-6";
    }
}

class RingsItem extends Item {
    applyEffect(character: Character): void {
        character.enableLowHealthAttackBoost();
    }
    removeEffect(character: Character): void {
        character.disableLowHealthAttackBoost();
    }
    getDescription(): string {
        return "血量低时增加攻击力。在黑暗的丛林里，主是我唯一的光。";
    }
}

class MoonItem extends Item {
    applyEffect(character: Character): void {
        character.enableDamageReflection();
    }
    removeEffect(character: Character): void {
        character.disableDamageReflection();
    }
    getDescription(): string {
        return "受到伤害时会反弹部分伤害。《神曲·天堂篇》结束篇句'高于这些星辰之上，尽是上帝的荣耀'";
    }
}

class StarItem extends Item {
    applyEffect(character: Character): void {
        character.increaseAttackSpeed();
    }
    removeEffect(character: Character): void {
        character.decreaseAttackSpeed();
    }
    getDescription(): string {
        return "增加攻击速度。'上帝赋予我手能使打仗的力量，教导我的指头战斗。' ——《圣经·诗篇》144:1";
    }
}

class RomanceItem extends Item {
    applyEffect(character: Character): void {
        character.increaseMaxHealthAndDecreaseAttack();
    }
    removeEffect(character: Character): void {
        character.decreaseMaxHealthAndIncreaseAttack();
    }
    getDescription(): string {
        return "增加血量上限并降低攻击力。但那等候耶和华的，必重新得力。他们必如鹰展翅上腾；他们奔跑却不困倦，行走却不疲乏";
    }
}

class WomanItem extends Item {
    applyEffect(character: Character): void {
        character.enableOneTimeRevival();
    }
    removeEffect(character: Character): void {
        character.disableOneTimeRevival();
    }
    getDescription(): string {
        return "死亡后原地复活一次。'我是复活，我是生命；信我的，虽然他死了，也必得以复活。' —《约翰福音》11:25";
    }
}

class HarvestItem extends Item {
    applyEffect(character: Character): void {
        character.enableDiscount();
    }
    removeEffect(character: Character): void {
        character.disableDiscount();
    }
    getDescription(): string {
        return "买道具打折。'你们要给人和永不借贷，也不多要。' —《路加福音》6:35";
    }
}