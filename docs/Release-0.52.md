# 0.52 — Separate campaign and practice players

- Campaign: title/continue/new journey/settings and the complete story scene. No practice entries or lab scenes.
- Practice: separate executable with its own title, five guardian chambers, rune/combat lab, controls and settings.
- Shared runtime code, prefabs, Boss profiles and combat parameters. Changes are built into both players together.
- Practice cannot enter campaign or read its save. Product/storage identities are separate; campaign keeps the existing 0.51 save location.
- Source publication preserves the original repository history. Unity binary assets use Git LFS, cache/build/model directories stay local, and the Unity editor integration has a pinned portable package URL.
- Automated player checks use isolated save folders, verify scene exclusion/menu separation, open all five Boss chambers, and check laboratory resources and campaign combat basics.

Practice results validate shared systems, but campaign map traversal, triggers, story sequences and balance still require campaign-specific testing.

## Verification

- Windows campaign: 22 checks passed.
- Windows practice: 24 checks passed, including all five Boss rooms.
- Editor menu/storage isolation: 7 checks passed.
- Title and pause menus inspected at 1600 × 900.
- This verifies the split and shared-system smoke checks; it is not a claim of exhaustive campaign playthrough.
