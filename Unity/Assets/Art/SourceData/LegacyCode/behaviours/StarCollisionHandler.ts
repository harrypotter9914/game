import { Behaviour, GameObject, Transform, BitmapRenderer, getGameObjectById } from "../../lib/mygameengine";
import { AbilityId, PlayerSession, RuneId } from "./PlayerSession";

const ABILITY_UNLOCKS: Record<string, AbilityId | undefined> = {
    sword: 'shockwave',
    '10.1 builders': 'crystalDash',
};

export class StarCollisionHandler extends Behaviour {
    private interactingStar: GameObject | null = null;
    private interactingTag = '';
    private iconDisplay: GameObject | null = null;
    private readonly handleKeyDownBound = this.handleKeyDown.bind(this);
    private hideTimer: ReturnType<typeof setTimeout> | null = null;

    onStart() {
        this.iconDisplay = getGameObjectById('iconDisplay');
        document.removeEventListener('keydown', this.handleKeyDownBound);
        document.addEventListener('keydown', this.handleKeyDownBound);
    }

    onEnd() {
        document.removeEventListener('keydown', this.handleKeyDownBound);
        if (this.hideTimer) {
            clearTimeout(this.hideTimer);
            this.hideTimer = null;
        }
    }

    setInteractingStar(star: GameObject | null, tag: string = '') {
        this.interactingStar = star;
        this.interactingTag = tag;
    }

    tryCollectCurrentStar() {
        if (this.interactingStar) {
            this.collectStar(this.interactingStar, this.interactingTag);
        }
    }

    handleKeyDown(event: KeyboardEvent) {
        if (event.code === 'KeyE' && this.interactingStar) {
            this.collectStar(this.interactingStar, this.interactingTag);
        }
    }

    private normalizeCollectibleName(tag: string | undefined | null): string {
        if (!tag) {
            return '';
        }
        return tag.replace(/^hoxi\s*/, '').trim();
    }

    private resolveRune(name: string): RuneId | null {
        return PlayerSession.normalizeRuneName(name);
    }

    private resolveAbility(name: string): AbilityId | undefined {
        return ABILITY_UNLOCKS[name.trim().toLowerCase()];
    }

    private showCollectible(source: string, x: number, y: number) {
        if (!this.iconDisplay) {
            return;
        }

        const bitmap = this.iconDisplay.getBehaviour(BitmapRenderer);
        const transform = this.iconDisplay.getBehaviour(Transform);
        if (bitmap) {
            bitmap.source = source;
        }
        if (transform) {
            transform.x = x + 160;
            transform.y = y - 180;
            transform.scaleX = 0.6;
            transform.scaleY = 0.6;
        }

        if (this.hideTimer) {
            clearTimeout(this.hideTimer);
        }
        this.hideTimer = setTimeout(() => {
            if (transform) {
                transform.x = -120000;
                transform.y = -800;
            }
        }, 1800);
    }

    private collectStar(star: GameObject, colliderTag: string) {
        const starTag = colliderTag || star.id || '';
        const name = this.normalizeCollectibleName(starTag);
        const rune = this.resolveRune(name);
        const unlock = this.resolveAbility(name);
        const transform = star.getBehaviour(Transform);
        if (transform) {
            PlayerSession.setRespawnPoint(transform.x, transform.y + 120);
            if (starTag) {
                this.showCollectible(`./assets/images/${starTag}.png`, transform.x, transform.y);
            }
        }

        if (rune) {
            PlayerSession.collectRune(rune);
        } else if (unlock) {
            PlayerSession.unlockAbility(unlock);
        } else {
            PlayerSession.increaseMana(1);
        }

        star.active = false;
        this.interactingStar = null;
        this.interactingTag = '';
    }
}
