# Babel 0.55 — directional Dash and Boss combat

## Changes

- Up/down Dash use separate generated animation strips, with anatomy drawn for each direction. No rotation of a horizontally scaled character. Charging and horizontal crystal Dash also receive clean isolated frames after finding neighboring sword fragments in their old sheet.
- Abaddon's quick slash had a real slice through its extended blade (49 connected opaque pixels across the cut). Replace the quick sequence and its neighboring-frame contamination with a complete four-frame strip. Repair the same issue in Abaddon's death and Nero's casting animations.
- Added authored overhead sword attacks for Abaddon and Nero, with 0.7/0.65 second telegraphs, a limited upward blade sweep, terrain occlusion, 2.4 second cooldown and recovery. They can also step away from an overhead player, checking terrain and ledge support.
- Abaddon alternates close heavy/quick attacks and chooses waves within their effective distance. Azazel limits consecutive rising attacks and mixes its jump attacks. Nero uses spacing, close combos, overhead counters, rushes and fans according to range/height/cooldowns; phase-two fans are eligible in close combat.
- Nero enters phase two at half health. A large hit cannot skip the threshold. The complete existing transformation plays for 1.8 seconds, cancels pending attacks, protects the transformation and restores vulnerability afterward. Phase two is latched until encounter reset, has 11 projectiles (versus 9), faster rush/projectiles and its existing stronger close attacks. The health bar marks `II` and changes fill color.
- The original design audit identifies Nero as the explicitly two-phase Boss. The other four retain their original identities and mechanics; no unsupported transformation phases were assigned to them. Korah escapes through burrowing and Bel retains aimed projectiles and traps.

## Art

Five transparent sheets produced with the built-in image generation tool, using the existing characters as identity references. Import rectangles follow measured gutters rather than assuming a perfect grid. Animation clips share the original campaign/practice controllers. Prompts and exact output asset paths are in `Art-0.55.json`.

Reviewed all five Bosses' attack, idle, hurt and death strips plus Nero's transformation and both slams. New strips were scanned for connected opaque pixels crossing import boundaries; none were found. New pivot and pixels-per-unit values preserve actor body scale rather than fitting each entire sword silhouette into a fixed size.

## Validation

- 55 targeted Play Mode checks passed: live up/down Dash animation selection, alternating close attacks, overhead hit/telegraph/range/terrain checks, pause, half-health transition protection and reset, 11 phase-two projectiles, animation references, and engagement/AI advancement in all five authored campaign rooms.
- 63 directional-input/checkpoint regression checks passed: keyboard and virtual controller, all four melee/beam/Dash directions, real target health changes, binding capture, five approach checkpoints, disk persistence and respawn.
- Automated runs use isolated `-bableSaveRoot` folders under ignored `reference/revision55`.
- Room tests enter the authored rooms and exercise short combat sequences; they are not a complete start-to-ending campaign traversal. Controller tests use Input System virtual devices, not a physical controller. Visual inspection includes extracted frames and sampled in-room runtime renders, not a guarantee that every possible animation transition is defect-free.

Both Windows executables built successfully with `BablePlayerBuilds.Both`. `tools/Test-Players.ps1` passed for campaign and practice; reports are in ignored `reference/smoke-20261002-224343`. Campaign reports 22 feature checks and practice reports 24 feature checks. Builds identify themselves as 0.55.0, and retain separate save identities and scene sets.
