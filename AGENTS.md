# Babel working agreements

- Active project: `Unity/`, Unity 6000.5.9f1. The root TypeScript game is historical source; preserve it unless explicitly asked to change it.
- User authorizes syncing requested, verified project updates to their existing `harrypotter9914/game` GitHub repository as part of delivery. Use normal commits and pushes; never force-push or overwrite unrelated changes. Do not schedule background updates.
- Ship two Windows executables from ONE source tree using `BablePlayerBuilds.Both`: campaign (`Builds/bable`) and standalone practice (`Builds/bable-practice`). Keep all gameplay code, definitions, prefabs, animations and Boss profiles shared. Do not create a copied practice project.
- Campaign has no practice entry points or test scenes. Practice has no campaign entry point, story progression or campaign save access. Keep their storage identities separate.
- For every update, build both players and run `tools/Test-Players.ps1` plus targeted checks for changed behavior. Always isolate automated saves with `-bableSaveRoot`; never overwrite the player's real save.
- Track Unity Assets INCLUDING `.meta`, Packages and ProjectSettings. Use Git LFS for binary assets. Do not commit Library, Temp, builds, downloaded model runtimes, account/session information, secret keys, local recordings or `reference/` archives.
- Keep dependencies portable and version-pinned. Do not add machine-specific `file:` package dependencies.
- Report actual test and upload results, including limitations. Passing practice checks does not replace campaign traversal and story integration testing.
