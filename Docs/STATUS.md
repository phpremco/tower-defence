# STATUS.md

Working notes for continuing the project (updated 2026-10-14). Read CLAUDE.md and GAME_DESIGN.md first.

## Where we are
Milestones 1 to 3 are done and playable (menu, planet map with difficulties and completion marks, level 1, result screen with stars,
base upgrade screen, saved progress). Parts of milestone 4 (tower damage, repair, wreck/scrap) and the in-level tower menu exist.
The look uses CC0 Kenney assets (see CREDITS.md). It is placeholder quality on purpose.

## Open issue being chased
Player reports "cannot place turrets" (twice). Placement code was fixed once (tower menu swallowed mouse clicks) and works in simulated
tests, but the report came back. Diagnostics now log every confirm to the console as `[Placement] ...` (mode, cell, canBuild, money,
pointerOverUi). Next step: ask the player to try placing once, then read `Logs/Editor.log` for those lines and fix what they show.
The HUD blocking check now only counts real buttons under the pointer.

## How the visuals can change later
- Models and tints are data: `VisualTheme` (terrain, road, base, scenery, colours per level), `TowerDefinition.Model`, `EnemyDefinition.Model`.
- Interface look is data: `UiSkin` (sprites, fonts). Screens are built in code from `UiKit`.
- Logic never references visuals (view components only), so swapping models or restyling needs no gameplay changes.

## Known gaps and ideas
- Road/terrain uses plain slabs; richer per-level themes, effects (lasers, explosions, damage numbers) and audio come in milestone 10.
- Hard and Psycho difficulty are untested for balance.
- Wrecks/scrap economy, blockades and hero, drones, path objects, combining are later milestones.
- Root `GAME_DESIGN.md` is a duplicate of `Docs/GAME_DESIGN.md` (delete was blocked; remove by hand).
- Remote repo: not set up yet. Push only when the user provides a private repo URL.

## Handy facts
- Unity CLI path and commands: see `Docs/COMMANDS.md`.
- Regenerate data and scenes: menu `Tools/Generate/Scenes and Starter Data`. It is idempotent and overwrites generated assets.
- Play Mode only advances when the Editor window is focused; for automated checks step frames with `EditorApplication.Step()`.

## Milestone 5 (blockades and hero) - first slice

- **Blockades** are `TowerDefinition`s with `BlocksGroundEnemies` placed by `LevelDefinition.Blockades` (cell + definition).
  They reuse Tower: levels, HP bonus per level (`TowerLevel.HealthBonus`), guns from level 2, repair, and "rebuild" when wrecked
  (level 0 cost). Ground enemies stop within 0.85 of a living blockade and hit it (`EnemyDefinition.BlockadeDamage`).
- **Hero** (`Game.Hero.Commander`, `HeroDefinition`): posts = every blockade + the base. Move with the HUD panel (bottom left),
  keys H / F / G (X, LT, RT on a gamepad), or the third icon of a blockade menu ("Send hero"). Guards a blockade (takes
  `GuardShare` of the hits), goes down when its health runs out and returns after `ReviveSeconds`.
- Not done yet: permanent hero upgrades in the upgrade screen, hero visuals beyond placeholders, balance pass for hard/psycho.
