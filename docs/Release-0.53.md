# Babel 0.53 — responsive menus and input focus

## Behavior

- Title, pause and death use borderless background artwork covering the whole window. Gameplay uses the full viewport. UI controls scale uniformly to remain inside the window, including 4:3, 16:10, 16:9, ultrawide and small windows. Display settings include multiple aspect ratios.
- The title uses a new transparent decorative BABEL logo derived from the original title reference; the unrelated Hollow subtitle is removed. Continue/New/Settings/Quit use equal spacing when a save exists.
- Mouse mode highlights only hovered menu buttons. Keyboard directions and controller input show persistent navigation focus. Actual input switches modes automatically. Settings tabs independently mark the page being displayed.
- Controller settings rows support directional adjustment. Rebinding suspends menu navigation, accepts a controller button, supports B/East or Escape cancellation and restores the original row focus. Keyboard/mouse and controller bindings remain separate. Settings hints update with the active input device.
- Rune storage uses a dark full-screen background and original circular gold socket artwork. View anywhere; equip or remove at an altar in the campaign. Practice permits unrestricted equipment changes. Rune effects and save restoration are preserved.
- Player damage flash and shake are stronger; independent comfort sliders still apply.

## Verification

The editor regression suite exercises virtual mouse, keyboard and controller input; hover clearing; active tabs; controller rebinding and cancellation; volume adjustment; rune equipment restrictions; accepted-damage feedback; and menu bounds across multiple aspect ratios. Captures are inspected separately because bounds checks alone cannot establish readability.

Validation on 2026-10-01: all 62 editor checks passed; campaign player passed 22 checks and practice player passed 24 checks. Both Windows builds completed successfully as version 0.53.0.

Both campaign and practice Windows players are built from the same source and tested with `tools/Test-Players.ps1`, using isolated save directories. These checks do not constitute a physical-controller hardware test or a complete campaign traversal.

## Artwork

Six raster assets in `Unity/Assets/Resources/Bable/NewArt/Release53` were generated using the built-in image generation tool. Exact prompts and source references are recorded in [Art-0.53.json](Art-0.53.json). Previous assets remain available. The new rune ring is an original ornament; it does not remove a watermark from the old stock image. The BABEL logo is a generated interpretation of the original reference, not a pixel-identical extraction.
