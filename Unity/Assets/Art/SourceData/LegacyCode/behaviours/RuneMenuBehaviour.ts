import { Behaviour, TextRenderer, getGameObjectById, GameObject } from "../../lib/mygameengine";
import { PlayerSession, RuneId } from "./PlayerSession";
import { GameManager } from "./GameManager";

export class RuneMenuBehaviour extends Behaviour {
    private visible = false;
    private focusRow: 0 | 1 = 0;
    private inventoryIndex = 0;
    private slotIndex = 0;
    private titleObject: GameObject | null = null;
    private inventoryObject: GameObject | null = null;
    private slotsObject: GameObject | null = null;
    private helpObject: GameObject | null = null;
    private readonly handleKeyDownBound = this.handleKeyDown.bind(this);

    onStart() {
        this.titleObject = getGameObjectById('runeMenuTitle');
        this.inventoryObject = getGameObjectById('runeMenuInventory');
        this.slotsObject = getGameObjectById('runeMenuSlots');
        this.helpObject = getGameObjectById('runeMenuHelp');
        document.removeEventListener('keydown', this.handleKeyDownBound);
        document.addEventListener('keydown', this.handleKeyDownBound);
        this.setVisible(false);
    }

    onEnd() {
        document.removeEventListener('keydown', this.handleKeyDownBound);
    }

    private setVisible(visible: boolean) {
        this.visible = visible;
        const objects = [this.titleObject, this.inventoryObject, this.slotsObject, this.helpObject];
        for (const object of objects) {
            if (object) {
                object.active = visible;
            }
        }

        const gameManager = getGameObjectById('gameManager')?.getBehaviour(GameManager);
        if (visible) {
            gameManager?.setWorldPaused(true);
            this.refreshText();
        } else {
            gameManager?.setWorldPaused(false);
        }
    }

    private handleKeyDown(event: KeyboardEvent) {
        if (event.code === 'Tab') {
            event.preventDefault();
            this.setVisible(!this.visible);
            return;
        }

        if (!this.visible) {
            return;
        }

        switch (event.code) {
            case 'ArrowUp':
                this.focusRow = 0;
                this.refreshText();
                break;
            case 'ArrowDown':
                this.focusRow = 1;
                this.refreshText();
                break;
            case 'ArrowLeft':
                this.moveSelection(-1);
                break;
            case 'ArrowRight':
                this.moveSelection(1);
                break;
            case 'Enter':
                this.confirmSelection();
                break;
            case 'Escape':
                this.setVisible(false);
                break;
        }
    }

    private moveSelection(direction: number) {
        if (this.focusRow === 0) {
            const collected = PlayerSession.getCollectedRunes();
            if (collected.length === 0) {
                return;
            }
            this.inventoryIndex = (this.inventoryIndex + direction + collected.length) % collected.length;
        } else {
            const slotCount = PlayerSession.getActiveSlots().length;
            this.slotIndex = (this.slotIndex + direction + slotCount) % slotCount;
        }
        this.refreshText();
    }

    private confirmSelection() {
        if (this.focusRow === 0) {
            const collected = PlayerSession.getCollectedRunes();
            const rune = collected[this.inventoryIndex];
            if (!rune) {
                return;
            }
            if (PlayerSession.isRuneActive(rune)) {
                PlayerSession.deactivateRune(rune);
            } else {
                PlayerSession.activateRune(rune);
            }
        } else {
            PlayerSession.deactivateRuneAt(this.slotIndex);
        }
        this.refreshText();
    }

    private formatInventory(collected: RuneId[]): string {
        if (collected.length === 0) {
            return 'Warehouse: empty';
        }

        return `Warehouse: ${collected
            .map((rune, index) => {
                const active = PlayerSession.isRuneActive(rune) ? '*' : '';
                return this.focusRow === 0 && index === this.inventoryIndex ? `[${rune}${active}]` : `${rune}${active}`;
            })
            .join('  ')}`;
    }

    private formatSlots(slots: Array<RuneId | null>): string {
        return `Slots: ${slots
            .map((rune, index) => {
                const label = rune || 'Empty';
                return this.focusRow === 1 && index === this.slotIndex ? `[${label}]` : label;
            })
            .join('  |  ')}`;
    }

    private refreshText() {
        const collected = PlayerSession.getCollectedRunes();
        const slots = PlayerSession.getActiveSlots();

        const title = this.titleObject?.getBehaviour(TextRenderer);
        const inventory = this.inventoryObject?.getBehaviour(TextRenderer);
        const slotText = this.slotsObject?.getBehaviour(TextRenderer);
        const help = this.helpObject?.getBehaviour(TextRenderer);

        if (title) {
            title.text = 'Rune Warehouse';
            title.color = '#f4e7c5';
        }
        if (inventory) {
            inventory.text = this.formatInventory(collected);
            inventory.color = this.focusRow === 0 ? '#ffd166' : '#ffffff';
        }
        if (slotText) {
            slotText.text = this.formatSlots(slots);
            slotText.color = this.focusRow === 1 ? '#ffd166' : '#ffffff';
        }
        if (help) {
            help.text = 'Arrow keys: move   Enter: equip/remove   Tab/Esc: close';
            help.color = '#bdbdbd';
        }
    }
}
