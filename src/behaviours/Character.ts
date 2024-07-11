class Character {
    private attackRange: number = 1;
    private attackSpeed: number = 1;
    private maxHealth: number = 100;
    private currentHealth: number = 100;
    private baseAttackPower: number = 10;
    private attackPower: number = this.baseAttackPower;
    private items: Item[] = [];
    private coinMagnetArea: any; // 假设这是一个表示吸引金币区域的对象
    private revivalCounter: number = 1;
    private discountRate: number = 1;
    public reflectDamage: boolean = false;
    public runeSlots: (Item | null)[] = [null, null, null, null]; // 符文凹槽
    public runeInventory: Item[] = []; // 符文仓库

    // 增加攻击距离
    increaseAttackRange() {
        this.attackRange += 1;
        // 修改武器模型大小逻辑
        console.log("Attack range increased to:", this.attackRange);
    }

    decreaseAttackRange() {
        this.attackRange -= 1;
        // 修改武器模型大小逻辑
        console.log("Attack range decreased to:", this.attackRange);
    }

    // 吸收金币
    enableCoinMagnet() {
        // 启用吸收金币逻辑，假设有一个方法 enableMagnet
        this.coinMagnetArea.enableMagnet();
        console.log("Coin magnet enabled.");
    }

    disableCoinMagnet() {
        // 禁用吸收金币逻辑，假设有一个方法 disableMagnet
        this.coinMagnetArea.disableMagnet();
        console.log("Coin magnet disabled.");
    }

    // 血量低时增加攻击力
    enableLowHealthAttackBoost() {
        if (this.currentHealth < this.maxHealth * 0.3) {
            this.attackPower = this.baseAttackPower * 1.5;
            console.log("Low health attack boost enabled. Attack power:", this.attackPower);
        }
    }

    disableLowHealthAttackBoost() {
        this.attackPower = this.baseAttackPower;
        console.log("Low health attack boost disabled. Attack power:", this.attackPower);
    }

    // 反弹伤害
    enableDamageReflection() {
        this.reflectDamage = true;
        console.log("Damage reflection enabled.");
    }

    disableDamageReflection() {
        this.reflectDamage = false;
        console.log("Damage reflection disabled.");
    }

    // 增加攻击速度
    increaseAttackSpeed() {
        this.attackSpeed *= 1.5;
        // 改变攻击动画速度或缩短后摇时间
        console.log("Attack speed increased to:", this.attackSpeed);
    }

    decreaseAttackSpeed() {
        this.attackSpeed /= 1.5;
        // 改变攻击动画速度或缩短后摇时间
        console.log("Attack speed decreased to:", this.attackSpeed);
    }

    // 增加血量上限并降低攻击力
    increaseMaxHealthAndDecreaseAttack() {
        this.maxHealth += 50;
        this.attackPower -= 5;
        console.log("Max health increased to:", this.maxHealth, "Attack power decreased to:", this.attackPower);
    }

    decreaseMaxHealthAndIncreaseAttack() {
        this.maxHealth -= 50;
        this.attackPower += 5;
        console.log("Max health decreased to:", this.maxHealth, "Attack power increased to:", this.attackPower);
    }

    // 一次性复活
    enableOneTimeRevival() {
        this.revivalCounter = 1;
        console.log("One-time revival enabled.");
    }

    disableOneTimeRevival() {
        this.revivalCounter = 0;
        console.log("One-time revival disabled.");
    }

    // 购买道具打折
    enableDiscount() {
        this.discountRate = 0.8; // 假设打八折
        console.log("Discount enabled. Rate:", this.discountRate);
    }

    disableDiscount() {
        this.discountRate = 1;
        console.log("Discount disabled. Rate:", this.discountRate);
    }

    // 攻击方法
    attack() {
        console.log("Attack with power:", this.attackPower);
        this.playAttackAnimation();
    }

    playAttackAnimation() {
        console.log("Playing attack animation.");
    }

    // 添加道具
    addItem(item: Item) {
        item.applyEffect(this);
        this.items.push(item);
    }

    // 移除道具
    removeItem(item: Item) {
        item.removeEffect(this);
        this.items = this.items.filter(i => i !== item);
    }

    // 拖拽符文到凹槽
    dragRuneToSlot(rune: Item, slotIndex: number) {
        if (this.runeSlots[slotIndex]) {
            this.runeSlots[slotIndex]?.removeEffect(this);
        }
        this.runeSlots[slotIndex] = rune;
        rune.applyEffect(this);
        console.log(`Rune ${rune.constructor.name} added to slot ${slotIndex}`);
    }

    // 从凹槽中移除符文
    removeRuneFromSlot(slotIndex: number) {
        const rune = this.runeSlots[slotIndex];
        if (rune) {
            rune.removeEffect(this);
            this.runeSlots[slotIndex] = null;
            console.log(`Rune ${rune.constructor.name} removed from slot ${slotIndex}`);
        }
    }
}