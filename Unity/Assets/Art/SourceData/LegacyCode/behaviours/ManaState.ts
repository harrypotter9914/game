import { Behaviour, getGameObjectById, GameObject, BitmapRenderer } from "../../lib/mygameengine";
import { HealthStateMachine } from "./HealthState";
import { PlayerSession } from "./PlayerSession";

export class ManaStateMachine extends Behaviour {
    private currentMana = 3;
    private blue: GameObject | null = null;
    private healthStateMachine: HealthStateMachine | null = null;
    private isDecreasingMana = false;

    onStart() {
        PlayerSession.ensureInitialized();
        this.blue = getGameObjectById('blue');

        const player = getGameObjectById('mainRole');
        if (player) {
            this.healthStateMachine = player.getBehaviour(HealthStateMachine);
        }

        this.currentMana = PlayerSession.getMana();
        this.updateManaImage();
    }

    updateManaImage() {
        if (!this.blue) return;

        const bitmapRenderer = this.blue.getBehaviour(BitmapRenderer);
        if (!bitmapRenderer) {
            return;
        }

        const imageSource = `./assets/images/blue${Math.max(0, Math.min(3, this.currentMana))}.png`;
        bitmapRenderer.source = imageSource;
    }

    setMana(mana: number) {
        PlayerSession.setMana(mana);
        this.currentMana = PlayerSession.getMana();
        this.updateManaImage();
    }

    increaseMana(amount: number) {
        this.setMana(this.currentMana + amount);
    }

    decreaseMana(amount: number) {
        if (this.isDecreasingMana || this.currentMana <= 0 || this.healthStateMachine?.isHealthFull()) return;

        this.isDecreasingMana = true;
        if (this.currentMana > 0 && this.healthStateMachine && !this.healthStateMachine.isHealthFull()) {
            this.healthStateMachine.increaseHealth(1);
        }
        this.setMana(this.currentMana - amount);
        this.isDecreasingMana = false;
    }
}
