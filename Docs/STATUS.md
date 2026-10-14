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

## Custom (AI) models

- Put a single-mesh `.glb` in `Assets/_Project/Art/Models/Custom/`, add a recipe for it in `ModelRecipe.For` (parts are
  chosen by where a triangle sits in the model's bounding box), run `Tools/Generate/Build Custom Models` (also part of the
  starter-data generator). It writes mesh, materials and prefab to `Custom/Built/`.
- `GlbMeshReader` only reads positions and triangles. Models with textures or several materials need a real glTF
  importer (glTFast package), which is not installed.
- A tower using such a model sets `KeepModelColours` so the tower tint and glow are not applied (pulse rifle does).

## Milestones 6 to 10 - first versions (built unattended on 2026-10-14, please review)

Choices made without asking, because the design doc leaves them open. All are data or small code, easy to change:

- **Drones:** work on their own (no manual steering). Repair drones heal the most damaged tower or blockade for free,
  collector drones pick up loot that defeated enemies drop (a bonus of half the reward, only while collectors fly).
  Bought between levels with power currency, as upgrade levels in the "Drones" group (one level = one more drone).
  Flying enemies can shoot them down; lost drones stay lost for that level.
- **Path objects** (`PathObjectDefinition`, kinds Mine / SlowField / Roller): consumed on use or after a lifetime, not
  upgradeable. Each level sells a subset (see `LevelDefinition.PathObjects`). Placed like towers (select in the bar, click a
  cell, click again). Road objects need a free road cell; "beside" objects a free build cell.
- **Combining:** `CombineRecipe` (two kinds + result + price). Select a tower next to its partner and a fourth icon
  "Combine" appears. Both towers become one hybrid (value of both is kept for selling). Only level 5 lists recipes, as in
  the design table. Hybrids: Artillery (Cannon+Rocket), Storm Rifle (Pulse+Tesla), Cryo Beam (Laser+Cryo).
- **Towers:** all eight standard towers exist (Laser and Tesla are plain fast/splash shooters for now, Cryo slows, Support
  boosts range and fire rate nearby). Railgun and Support are not rewarded in world 1 (as designed), only reachable with
  the unlock-all test option.
- **Levels 2 to 5** are generated data (`ProjectGenerator.World.cs`): own palette, road layout, blockades, bosses on 2/4/5,
  second entrance on 2 and 5, a new weapon as reward each. Enemy health scales per level (`EnemyHealthMultiplier`).
- **In-level kill money no longer scales with difficulty** (it made hard levels easier); research and power rewards still do.
- **Audio** is generated at runtime (`ProceduralSfx`, played by `SfxPlayer`): shots, kills, explosions, base hits.
- **Balance probe:** `BalanceProbeTests` plays all levels and difficulties with a greedy bot and logs the outcome.
  With the current numbers the bot wins Easy on every level and loses Psycho on all but the first. Real players have
  upgrades, hero and drones, so expect it to be easier for them. Tune `CountScale`/`HealthScale` per level in the generator.
- **Screenshots without the editor:** `ScreenshotTests` writes `Logs/screenshot_level5.png`.

Still open / known gaps: hero and drone models are placeholders, no per-tower second upgrade path from level 3 (design
mentions two paths), boss abilities, endless mode, real audio, menu navigation by gamepad has not been tried on hardware.

## Look, maps and drones (2026-10-14, evening)

- **Maps:** 20x12 (Landing), 26x14 (Forest), 22x16 (Lava), 28x16 (Ruins), 24x14 (Nest). Roads, blockades, flight routes and
  terrain are authored in `ProjectGenerator.World.cs`; rock formations and mining nodes are placed from a fixed seed
  (`ProjectGenerator.Terrain.cs`). A side road is merged into the main road after the junction (`MergeIntoMain`) and the
  validator rejects any road that does not end at the base.
- **Landscape:** `TerrainField` (heights and masks) -> `TerrainPainter` (ground texture, cobbled road) ->
  `TerrainSurfaceBuilder`, `TerrainLiquidBuilder`, `PropScatterBuilder`, `AmbientParticlesBuilder`. The generator bakes the ground
  texture to `Art/Terrain/*.png` (`BakeTerrain`); without it the level paints at load time. Terrain is data on the level
  (`TerrainPatch`: Raised, Water, Lava, Quicksand; liquids are not buildable, raised ground lifts towers).
- **Models:** all procedural (`PropMeshBuilder` and the `*ModelFactory` classes). Towers, enemies and path objects choose
  their model by an enum on their definition (`TowerShape`, `EnemyShape`, `PathObjectLook`). The Pulse Rifle keeps the
  imported machine gun and gets add-ons per level (`TowerShape.PulseRifle`).
- **Animation:** enemies walk with distance-synced legs, rear/strike/flinch on attack and hit; the commander winds up and cuts
  with a lunge and a blade trail. Tower barrels recoil.
- **Camera:** WASD / arrows / right stick pan, wheel / `-` `+` / stick clicks zoom (`CameraPanZoom`, `FixedCameraRig.SetView`).
  The keyboard no longer moves the build cursor (the mouse and the left stick do).
- **Drones:** bought in the level with energy (`EnergyPool`, `LevelDefinition.EnergyMax/EnergyPerSecond`, starts at 0). Collectors
  take loot first, otherwise mine one of the level's `MineCells` and carry the yield home. Permanent upgrades only add free
  starting drones.
- **Known gaps:** death animation, hero leg animation, boss abilities, endless mode, real audio, balance on the bigger maps
  (see the probe results below if present), gamepad testing on hardware.
- **Test tooling:** the Unity CLI test runner returns 0 tests for PlayMode; run PlayMode with Unity closed (see COMMANDS.md).
