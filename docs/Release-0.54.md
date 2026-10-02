# Babel 0.54 — directional combat and Boss approach checkpoints

## Fixes

- A saved Up override could point to the mouse button used on the controls page, leaving the Up Arrow unable to aim attacks. Loading bindings now repairs mouse/empty direction overrides back to their keyboard defaults, while preserving other custom controls. The normal atomic settings writer retains the previous file as a backup.
- Direction remapping accepts keyboard keys or controller controls. Other actions still accept mouse buttons. The initial click/press is guarded, and rejected candidates cannot auto-complete a capture through the Input System's match timer. Deliberate controller rebinding and cancellation remain supported.
- Player direction is sampled before combat each frame, so simultaneous direction-plus-attack input no longer depends on component execution order. Melee, Shockwave and charged Dash retain their existing unlock requirements, damage, cooldowns and mana costs. Downward melee remains an airborne attack.
- Five approach altars were added on existing solid terrain outside the five Boss encounter rectangles. Existing altars remain. Activating an altar immediately persists the return point without restoring health or mana.

## Approach locations

| Boss | Altar position | Approach |
| --- | --- | --- |
| Abaddon / Shockwave | 65.5, -47 | Upper platform before dropping into the first chamber |
| Korah / Burrow | 256.5, -47 | Left platform before the chamber descent |
| Aerial | 314.5, -27 | Left approach ledge outside the encounter |
| Crystal | 359.5, 29 | Raised approach outside the left entrance |
| Nero | 158.5, 77 | Left entrance before the throne hall |

## Validation

The dedicated regression suite passed 63 checks using isolated save directories. It injects actual keyboard and controller events, including simultaneous direction and attack. All four directions of melee, Shockwave and charged Dash damage an actual enemy; the tests also check beam direction and mana cost, Dash direction and displacement, binding repair/reload/capture, five altar triggers, immediate disk persistence and actual respawn placement. Altar screenshots were inspected.

On 2026-10-02, the existing 62-check menu/input suite also passed. Both 0.54.0 Windows players built successfully; campaign passed 22 smoke checks and standalone practice passed 24. Test saves were isolated from the player's existing progress.

Hardware limitations: controller checks use a virtual Input System device, not a physical gamepad. Approach checks validate local ground, trigger, encounter separation and respawn safety; they do not represent a fresh full-campaign playthrough.
