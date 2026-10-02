# Babel

A medieval fantasy platform adventure. The active implementation is the **Unity 6 project in `Unity/`**. The original TypeScript game remains at the repository root for historical reference.

## Open the source

Install **Git LFS** and **Unity 6000.5.9f1** with Windows Build Support, then:

```sh
git lfs install
git clone https://github.com/harrypotter9914/game.git
cd game
git lfs pull
```

Open `Unity/` from Unity Hub. Let Unity restore packages and import assets. The editor integration is pinned to a Git commit; no local tools directory is required. Keep all `.meta` files when moving or editing assets.

## Two players, one implementation

| Player | Contents | Output |
| --- | --- | --- |
| Campaign | Title, prologue, complete campaign, saves, settings | `Builds/bable/bable.exe` |
| Practice | Separate title, five Boss chambers, rune/combat laboratory, settings | `Builds/bable-practice/bable-practice.exe` |

Use **Bable → Build → Build Both Windows Players**, or:

```powershell
./tools/Build-Players.ps1 -UnityEditor 'C:/path/to/Unity.exe'
./tools/Test-Players.ps1
```

The practice player uses `BABLE_PRACTICE` only for menus, scene access and storage. Gameplay, character definitions, Boss profiles, combat logic and assets are shared. Practice unlocks abilities/runes and offers test controls; the lab replenishes gold. It cannot load the campaign scene or read/write the campaign save. Campaign builds exclude all six testing scenes and have no practice menu entries.

Default Windows data folders:

- Campaign: `%USERPROFILE%/AppData/LocalLow/Yibo Wang/bable/Babel`
- Practice: `%USERPROFILE%/AppData/LocalLow/Yibo Wang/bable-practice/BabelPractice`

Automated checks use their own `-bableSaveRoot` folders. Tests cover both exported executables, including all five practice chambers. Campaign integration and traversal still need separate testing even when shared combat passes in practice.

## Version control

Track `Unity/Assets`, `Unity/Packages`, `Unity/ProjectSettings`, `.meta` files and the build/test scripts. Binary artwork and audio use Git LFS. Unity caches, generated players, test saves, local voice-model installations and working archives are excluded. Rebuild players locally; executable folders are not source files.

Future requested updates are committed and synchronized after validation. See [working agreements](AGENTS.md) and [0.54 notes](docs/Release-0.54.md).

## Menus and controls

Windows can be resized to different aspect ratios. Full-screen menu artwork covers the viewport while controls scale to fit; settings and shop panels retain their decorative frames. Mouse hover, keyboard navigation and controller focus switch automatically with input. Settings tabs mark the current page independently of the selected control. Controller remapping supports confirmation, cancellation and return to the same row without a keyboard.

The rune repository can be viewed anywhere. Campaign equipment changes require standing at an altar outside an active Boss encounter; the standalone practice player allows changes anywhere. Existing rune effects and restored save equipment are unchanged.

## Assets and release status

This repository is a development/portfolio project, not a Steam release. New title/pause/death artwork and the eight rune icons were generated for this game. Other inherited assets retain their original ownership and licenses; this repository does not grant a blanket reuse license. Bundled third-party notices remain beside the relevant assets. The historical root game is retained as originally published and does not represent the current Unity asset selection.
