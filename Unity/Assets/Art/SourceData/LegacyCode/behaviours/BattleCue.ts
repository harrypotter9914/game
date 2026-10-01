import { TextRenderer, Transform, getGameObjectById } from "../../lib/mygameengine";

let hideTimer: ReturnType<typeof setTimeout> | null = null;

export class BattleCue {
  static show(title: string, detail: string = "", duration: number = 900) {
    const titleObject = getGameObjectById('battleCueTop');
    const detailObject = getGameObjectById('battleCueBottom');
    const titleText = titleObject?.getBehaviour(TextRenderer);
    const detailText = detailObject?.getBehaviour(TextRenderer);
    if (titleText) {
      titleText.text = title;
      titleText.color = '#ffcf5a';
    }
    if (detailText) {
      detailText.text = detail;
      detailText.color = '#ffffff';
    }
    if (hideTimer) {
      clearTimeout(hideTimer);
    }
    hideTimer = setTimeout(() => BattleCue.clear(), duration);
  }

  static clear() {
    const titleText = getGameObjectById('battleCueTop')?.getBehaviour(TextRenderer);
    const detailText = getGameObjectById('battleCueBottom')?.getBehaviour(TextRenderer);
    if (titleText) {
      titleText.text = '';
    }
    if (detailText) {
      detailText.text = '';
    }
  }
}
