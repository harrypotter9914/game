# Babel 0.57 — regional camera framing

- Ordinary exploration and Boss arenas use a fixed orthographic size of 7 (14 world units vertically). Nearby walls, props and breakable gates do not alter zoom.
- The authored lower-right moss labyrinth uses the existing close size of 3.5 (7 world units vertically); transitions run at 0.8 size units per second. Arena interiors are excluded. A 0.8-unit boundary margin prevents zoom toggling on a seam.
- A 24% screen-width by 18% screen-height dead zone keeps the camera stationary during small movements and turns. Crossing an edge starts smooth follow. The slight upward framing bias remains, preserving the view ahead and above the player.
- Damage shake is applied separately from the tracked camera position, so it cannot shift the dead zone. Pause freezes follow and zoom. Loading, checkpoint alignment and respawn can explicitly settle the camera to the current region.
- The same camera implementation is shared by campaign and practice. Practice rooms retain the ordinary view; only the authored campaign maze selects the close view.

## Validation

- Revision57: 48 campaign play-mode checks passed. Coverage includes the five Boss arenas, ordinary passages, maze entry/exit and boundary hysteresis, four window aspect ratios, horizontal and vertical dead-zone follow, pause, checkpoint alignment, full-speed Dash travel, actual player walking and shake recovery.
- Screenshots captured on actual campaign terrain for the forest, lower-right maze and Abaddon room.
- Automated save data is isolated under `reference/revision57/` with `-bableSaveRoot`.
- Built campaign and standalone practice 0.57.0 through `BablePlayerBuilds.Both`.
- `tools/Test-Players.ps1` passed for both exported executables. Campaign checks cover clean scene/menu separation and runtime systems; practice checks cover all five shared-profile chambers. Saves are isolated under `reference/smoke-20261024-110406/`.

These targeted checks do not constitute a new full campaign traversal or a player study. The framing values are an initial design tuning, not a measured playtest optimum.
