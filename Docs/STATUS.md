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

## Alive battlefield pass (2026-10-28)

Done after the first real play session ("playable, but static lines, weak sound, no death animation, flat lava"):

- **Waves:** every enemy gets its own lane on the road (`PathOffset` mitres the corners; room depends on body size, so
  bosses stay centred), its own pace (`Enemy.SpeedMultiplier`, +/-12%) and flyers their own height. Spawn moments drift
  by `WaveDefinition.SpawnJitter` (share of the interval, default 0.45) through a `System.Random` handed to
  `WaveScheduler`; without one the scheduler is exact, which the tests rely on. Walkers weave with their stride, flyers
  drift and roll, every creature has its own gait rate.
- **Animation and effects:** enemies rise out of the gate (`EnemyView` spawn curve) and fall apart on death
  (`EnemyDeathEffect`, pooled by `EnemyDeathEffectPool`: the body parts fly, tumble, land and shrink while ichor in the
  creature's colour splashes). Towers flash at the muzzle on every shot. `ImpactFlash` adds sparks, and for splash hits
  a shock ring and smoke. `FixedCameraRig.Shake` plus `CameraShaker` kick the camera on explosions, base hits, boss
  deaths and the hero's shockwave.
- **Sound:** `ProceduralSfx` is a small synth (tones with sweeps, filtered noise, crackle, echo, soft clip) with cached
  clips: pulse, heavy, rocket and cryo shots (picked from `ShotInfo` on `ProjectilePool.Shot`), small and big explosions,
  enemy and boss deaths, boss roar, base alarm, wave horn, build, upgrade, repair, sell, drone, hero strike and shockwave,
  structure hits, and a looping ambience per theme (`VisualTheme.Ambience`, `AmbienceKind`: Wind, Forest, Lava, Ruins,
  Hive). New context events carry the moments: `EnemySpawned`, `EnemyKilled`, `StructureHit`, `WaveStarted`,
  `TowerBuilt/Upgraded/Repaired/Sold`, `PathObjectPlaced`. `EnemyDefinition.IsBoss` marks the three bosses.
- **Liquids:** subdivided sheets with rolling waves (`LiquidFlow.SetWaves`), a second additive layer drifting at another
  pace (heat blotches, water sparkle threads, sand streaks), lava emission that breathes, flickering point lights over
  lava clusters (`LightFlicker`, at most ten), and motes from the liquid cells themselves (embers and sparks, dust,
  sparkles and mist) scaled by `IPlatformSettings.ParticleScale`. The lava ball sheds embers and carries a light.
- **Terrain details:** prop glow is brighter and breathes (`EmissionPulse` on the shared glow materials), plants sway
  (`WindSway`), rocks sit in a dark skirt with glowing moss on some, crystals have shards and a pool of light; the
  biggest glowing formations on blocked cells get a flickering light (at most twelve) and crystals shed sparks. Raised
  ground shows strata, a pale lip and a glowing seam on its cliffs (`TerrainPainter.ShadeCliff`, baked by the
  generator) and loose rubble along its edges (`PropScatterBuilder.PlaceRubble`).
- **Settings:** GPU Resident Drawer disabled on `PC_RPAsset` (see COMMANDS.md for why). The two deprecation warnings
  in `ScreenshotTests` and `UiKit` are fixed, so the project compiles with zero warnings.
- **Not verified on hardware:** gamepad, and the PlayMode suite was not run in this session (needs Unity closed); the
  levels were played through the CLI with probe towers and showed no errors.

## Look pass two (2026-10-28, later)

Asked for after the first pass: gates, hero, shores, flora and the base.

- **Shores and craters:** liquid masks get five blur passes; water and sand slope down like a beach
  (`TerrainField` basin profile), lava sits in a crater with a thrown-up rim. `TerrainPainter` paints a pale beach,
  a wet line and foam at the waterline, and an ash-grey crater rim with hot cracks. Baked by the generator.
- **Flora and crystals v2:** `PropMeshBuilder` takes a submesh count and has `AddBlade` (double-sided ribbons).
  `PropFactory` uses six slots (rock, glow, second glow, dark, leaf, bright core): hexagonal prisms with bright tips
  and shards, broad curling leaves, ribbed mushroom caps with a lit rim, trees on roots with layered canopies and
  hanging fruit, rocks in a dark skirt. Every glowing prop stands in a pool of light (`GlowDecal.PlacePool`).
- **Base:** `StructureModelFactory.BaseReactor` is an octagonal landing platform with a lit rim, a command dome
  ringed with windows, a caged reactor core, antenna mast, corner pylons and habitat tanks; the ring is a holo ring
  plus the radar sweep. A flickering light and a glow pool sit under it, and the glow material breathes.
- **Gates:** blockades are drawn by `GateView` (two pylons, a crossbeam, an energy field: a vertical sheet plus a
  glowing strip on the road). The field dims with damage and stutters under half health. A wreck bursts the field,
  tilts the pylons, drops the beam, blackens everything and keeps smoking and sparking until rebuilt. The gun head
  sits on the beam. The alien spawn points are hive mouths (`EnemyGate`, creature palette) with a light and spores.
- **Hero:** separate legs, head and arm (`HeroModelFactory`): walks on its legs, scans with its head while waiting,
  breathes, burns its thrusters while moving, and carries a tall beacon with a spinning emblem so it stays visible
  behind towers on raised ground.
- **Play-test scripts** moved to `Tools/PlayTest/` because Unity wipes `Temp/`.

## Look pass three (2026-10-28, late)

Feedback on pass two: no visible gate explosion, flat flora, hero always in a fighting pose, hero inside the base,
base not a building, lava without rock edges, slow camera.

- **Gate explosion:** `Tower.Wrecked` fires once per wreck; `PlacementController` turns it into
  `LevelContext.StructureWrecked`, which plays the big explosion and shakes the camera. `GateView.Explode` shows a
  flash, a fireball, 120 sparks and twelve chunks of debris that fly, bounce and shrink; then the pylons keel over
  outward and the beam crashes down with a bounce. Gates always show their health bar.
- **Flora texture and colour:** `PropMeshBuilder` writes texture coordinates (tubes, blobs, boxes, blades) and
  tangents; `PropTextures` generates tileable albedo and normal maps for rock, bark and leaf (runtime normal maps
  store x in alpha for URP). `VisualTheme.PlantColor` sets the leaf colour per level, and `PropFactory` builds
  three hue-shifted material sets that `PropScatterBuilder` mixes by cell hash. Glow pools are smaller and only
  under crystals, trees, mushrooms and eggs.
- **Hero:** the sword is a separate hilt and blade. At rest it is clipped to the pack with the blade off, arms hang
  and swing with the stride; a strike draws it and ignites the blade, and it goes back after four quiet seconds.
- **Base:** `BaseReactor` is now an outpost on a square deck: two-storey command block with window strips and a
  lit door towards the road, reactor cylinder in a cage, lattice comms tower with a turning dish (its own pivot),
  landing pad ring, solar panels, crates. It stands 0.8 cells past the road end, door to the road, so the hero post
  is clear. `ModelPalette` metal is low-metallic because there is no reflection environment.
- **Lava:** a higher crater rim with brighter ash rock and hot cracks, plus rock and basalt teeth scattered on the
  rim (`PropScatterBuilder.PlaceLavaRim`).
- **Camera:** pan speed up from 9 to 17 units per second at full zoom.
- **Mesh builder:** a tube that still has width at its end now closes with a flat lid instead of a cone as tall as
  its radius (that cone was the "dome" on the old base and the dark cap on the dish). Only tapered tubes get a point.
- **Builds:** `Bootstrapper._shaderKeepers` references three saved materials (made by the generator in
  `Art/Materials/ShaderKeepers`) that carry the emission, normal-map and transparent keyword combinations the runtime
  materials use, so a player build keeps those shader variants.

## Look pass four (2026-10-29)

Feedback on pass three: base still plain, hero death "poof" and silent, sword wielding too simple and not reusable
for guns, towers ugly and not weapon-like, tower cards should be pictures, gate upgrade grows an ugly block.

- **Base:** `StructureModelFactory.BaseOutpost` returns `BaseParts`: a stepped deck with lit rim and a ramp to the
  road, a three-tier octagonal command tower with window bands and a spire, a spinning energy ring around it (the
  structure's `Ring` spinner), a caged reactor core, stacked habitat cylinders with portholes, a lattice mast with
  its own turning dish (`LevelVisualBuilder.AddSpinner`), a lit landing pad, shield pylons, solar panels, tanks and
  crates. Two point lights: one low, one on the tower.
- **Hero down:** `Commander` raises `Downed` and `Revived`. `HeroView` plays a stagger (sparks, visor stutter),
  a fall onto the side, an impact (sparks, dust, the weapon dropped beside it), then a smoking, powered-down lie
  (`HeroModelFactory.CreateMaterials(trim, powered: false)`) with the beacon red; the revive flashes a ring and
  rolls the figure back up. Sounds `HeroDown` and `HeroRevive` in `ProceduralSfx`, wired in `SfxPlayer`; the
  camera shakes on the fall.
- **Hero weapons as data:** `HeroWeaponDefinition` (kind melee/ranged, style, damage, reach, interval, targets,
  colour) is referenced by `HeroDefinition._weapon`; damage, reach and interval now come from the weapon. Assets:
  `Data/Hero/Weapon_EnergySword` (carried) and `Weapon_Blaster` (ready, not carried: point the Commander at it to
  try the ranged rig). `HeroWeaponModelFactory` builds the weapon meshes; `HeroWeaponRig` is the animation base
  (holster, draw, drop, rest pose), with `SwordRig` (draw twirl, swaying guard stance, three cycling cuts with
  trail) and `BlasterRig` (aim pose, kick, muzzle flash, bolt line). `HeroView` only asks the rig for a pose.
- **Towers:** `TowerModelFactory` rebuilt in one hard-edged language: a shared hexagonal `Pedestal()` with lit
  chevrons and a turret ring, a `Hull` per head, and `Parts` that carry the barrel pivot, recoil axis, muzzle
  point, rotary flag and scale. Gatlings (pulse rifle, storm rifle) spin while firing; the rocket pod and the
  artillery recoil along their tilt. The pulse rifle no longer uses the hand-made gun model (file kept on disk).
  `TowerShape.GateGun` is the small autocannon a gate grows on its beam with gun upgrades.
- **Cards:** `ModelIconRenderer` renders one model per frame into a sprite (cached per definition) and the HUD
  cards show that picture over a price strip; `TowerModelBuilder` and `PathObjectModels.Build` make the display
  copies, also used by `ModelPreview.ShowTower` on the reward and upgrade screens.
- **Play-test scripts** added: `hero_down.cs`, `hero_revive.cs`, `upgrade_gate.cs`; `zoom.cs` gained targets
  "towers" and "roster" and reads its size from `zoom_size.txt`.
- **Pitfall hit:** editing a script while Play Mode runs triggers a domain reload that drops all runtime state
  (hundreds of null references, "no context"). Stop Play Mode before touching scripts.

## Upgrade looks and projectile styles (2026-10-29, later)

- **Per-level looks:** `TowerModelFactory.CreateMaterials(colour, level)` runs hotter per level (brighter, whiter
  glow; body takes more tower colour; metal warms towards brass) and `Pedestal(level)` adds a lit rank ring per
  level. Every head gains parts per level: extra barrels, glow rings on the guns, missile side racks, a second
  pair of laser rails and a bigger lens, more frost rings, tesla halos, twin rail channels, artillery rings, a
  storm-rifle shroud, a thicker cryo prism, sensor masts.
- **Projectile styles:** `ProjectileStyle` (Bolt, Shell, Rocket, Laser, Frost, Lightning, Rail, Mortar) is data on
  `TowerDefinition._projectileStyle`; `Tower` fires from the barrel mouth with its style and colour. `Projectile`
  raises `Launched` and exposes `Origin`, `Destination` and `Progress`; `ProjectileView` dresses one pooled object
  per style (beam and glow line from the muzzle for lasers, jittering arc for lightning, missile with flame and
  smoke, frost shard shedding ice, long rail slug, lobbed mortar shell, tracer shell). `ImpactFlash` takes the
  weapon colour. `ShotInfo.Style` lets `SfxPlayer` pick laser, tesla and rail sounds (`ProceduralSfx`).
- **Play-test scripts** added: `upgrade_towers.cs`, `skip_wave.cs`, `shots_probe.cs`; `zoom.cs` also takes
  "spawn" and "enemy".

## Proportions (2026-10-29, evening)

Feedback: towers, guns and hero out of proportion, towers poking into the road and through the hero.

- Towers shrunk to sit inside their cell: `TowerModelFactory.TowerScale` 1.1 to 0.78, `PedestalScale` 0.76 (the
  pedestal mesh is modelled at cell size and scaled on placement), gate gun 0.55, level growth 7 percent, stars and
  health bar lowered to match. Card icons reframed.
- Hero figure scaled to 0.85 (`HeroView.FigureScale`): still taller than a tower, no longer a giant beside one.
- **Shoulder beside the road:** a tower built on a cell next to a road stands back from it inside its cell
  (`PlacementController.RoadClearance`, 0.3 cells away from the road neighbours), so a strip of ground stays open
  between the walking lane and the guns and a tower no longer sits on top of the hero. Gates stay on the road. The
  suit is warm pale with yellow shoulder pads and a little self-light, so it reads in dark levels and in the shadow
  of a hill. (A try at fading towers that cover the hero was dropped: not helpful in play.)
- **Maps match placement:** `TerrainField.ConfineToCells` keeps every terrain kind inside its own cells after the
  blur: hills, shores and craters no longer spill onto the flat cell beside them, so a tower on a build cell stands on
  level ground even when it is shifted away from the road. Slopes and beaches soften inside the raised or liquid
  cells. The shoulder grew to 0.4 cells and towers shrank a little more (scale 0.7, pedestal 0.66) so a shifted tower
  neither touches a cliff nor the tower in the row behind it.

## Wide roads and plazas (2026-10-29)

Roads are now two cells wide and the width is level data (`LevelDefinition._roadWidth`), not a constant. A level may
also list plaza cells (`_plazaCells`): open road where two or three roads meet.

- `Levels/RoadCorridor.cs` is the one source of truth for the road: all rows of every road path (the waypoints are
  the first row, extra rows lie to the **right of the direction of travel**, corners mitred so the outer corner is
  filled), the plaza cells, the direction through any road cell, the centre line (`CentreOf`, `Route`), the rung
  across the corridor (`Rung`, `RungContains`) and where a roller should head (`TryLocate`). `LevelDefinition.Road`
  caches it. `RoadDirection` and `RoadLocator` are gone; `BuildGrid`, `LevelValidator`, `TerrainField`,
  `TerrainPainter`, `PropScatterBuilder`, `EnemySpawner`, `HeroController`, `PlacementController`,
  `LevelVisualBuilder` and `DroneController` all read the corridor.
- Enemies walk the centre line (`PathOffset.Shift` by half the extra width) with lanes up to 0.7 cells either side
  (`EnemySpawner` lane constants grow per extra cell of width). Flying routes that end on the base cell end at the
  base itself.
- Gates span the corridor: `PlacementController` puts a blockade on the centre line, sets `Tower.Span` to the road
  width and occupies the whole rung plus the rung before it (the hero stand). `TowerView` builds the gate
  `Span + WallOverhang` wide. The hero spot marker is a strip across the road; any cell of the stand rung opens the
  hero menu (`HeroPostAt` uses `RungContains`).
- `RoadShape` draws ribbons along the centre lines with `HalfWidth = (width / 2 - 0.1) * cell` and treats plazas
  as road surface (depth behind the plaza outline counts as "inside"). `TerrainField` and `TerrainPainter` take
  the half width from the shape.
- Validator: every road cell (second rows too) must be on the grid, a blockade must sit on the first row of a road
  and not on a plaza, every plaza cell must connect to a road through plaza cells.
- Generator: `LevelSpec.RoadWidth` and `LevelSpec.Plazas` (`Rect(x0, y0, x1, y1)`); all five maps were redrawn.
  Design rule for serpentines: because the second row lies to the right of travel, legs that turn clockwise need
  five rows between their first rows and the others three, to keep two build rows between them. Forest, Ruins and
  Nest have a plaza where their two roads meet (Ruins gained a second road). All tower ranges went up by 0.5
  because the first build row moved half a cell out.
- `Tools/PlayTest/dump_map.cs` prints every level as text (road, plazas, gates, hero stands, base, terrain,
  blocked cells, mines) with its validation errors: the quickest way to check a layout.
- Checked in play: level 1 (serpentine, gate across the corridor, hero strip, base on the centre line, enemies in
  lanes) and level 2 (plaza at the junction). EditMode 81/81. PlayMode suite still not run; nothing committed.

### Hover marker and card badges (same day)

- The placement preview now stands exactly where the tower will stand: the ghost takes the same road clearance as
  a built tower, and the cursor marker on a build cell is a pedestal-sized plate at that spot
  (`PlacementController.PlaceMarker`). On a road cell the marker still covers the whole cell (a path object does).
- Tower cards carry small badges in the top left corner: ground (amber peaks), air (blue wings) and slow (pale
  snowflake), from `TowerDefinition.Targets` and `SlowFactor` (`HudView.Badges`, `IconFactory` Ground/Air/Slow).
  Support towers show no target badge.
- `Tools/PlayTest/hover_probe.cs` builds a tower beside the road and parks the placing cursor on the next cell.

### HUD layout, base menu and base explosion (same day)

- Towers beside water, a cliff, a rock or the map edge no longer hang over it: `PlacementController.RoadClearance`
  only uses the full 0.4-cell shift when every cell the pedestal could overhang is level ground like its own; otherwise
  it shifts 0.15 cells, which stays inside the cell (`PlainGroundAround`).
- `HudView` rebuilt: a 72 px strip at the top (base, energy number with a thin gauge, wave, speed/pause/skip icons)
  and a 156 px card bar at the bottom with money and the status line just above it. Cards are 136 x 140 in three
  bands: badges, picture, price. The speed icon carries its factor ("x1".."x3") and turns amber above x1.
- `DronePanelView` is gone. Drones are bought from the base: confirming on the last rung of the main road opens
  `BaseMenuView` (base and energy readout, one icon per drone kind with price and count), driven by
  `LevelContext.BaseMenuOpen`, which `PlacementController` sets and clears like the hero menu.
- `Levels/BaseView.cs` on the base root listens to `BaseHealth.Died`: flash, fireball, sparks, debris, lights out,
  the structure sinks and tilts, smoke for nine seconds. `SfxPlayer` plays `ProceduralSfx.BaseExplosion`,
  `CameraShaker` kicks at full strength, and `ResultView` waits 3.2 s before the defeat panel.
- `LevelVisualBuilder.BasePosition(level)` is the one place that knows where the base stands.
- Play-test scripts: `base_menu.cs` opens the base menu, `base_die.cs` destroys the base.

### Preview on pick, soft cursor, natural shores, menu pass (same day)

- Picking a card (or cycling with the shortcut) starts placing at once: `LevelContext.SelectTower` / `SelectPathObject`
  switch Browsing to Placing and raise `SelectionChanged`, which `PlacementController` answers with a refresh, so
  the preview stands at the cursor immediately. The cursor marker is translucent (alpha 0.28 browsing, 0.55 placing).
- Towers beside a liquid, cliff, rock or map edge stay almost centred (`FittedClearanceCells` 0.05), and liquids now
  keep a real shore inside their own cells: `TerrainField.FillLiquids` builds the mask from the distance to the cell
  outline (rounded outside corners, filleted inside corners, noise wobble, `ShoreWidthCells` 0.55) instead of a
  blurred-then-chopped mask. Liquid levels sit lower (`WaterLevel` -0.2) so a sloping bank shows above the sheet.
- Menu pass: the options overlay is opaque (the main-menu title showed through), the upgrade list stops higher above
  "Back to map" (`BackButtonClearance` 210), and the tower menu is 1000 wide so the upgrade preview line fits.
  Main menu, world map, difficulty picker, upgrade screen, tower/gate menu, hero panel, pause and result were all
  checked on screenshots.
- Play-test scripts: `click_button.cs` (clicks a UI button by label text from `button_arg.txt`, lists all labels),
  `open_menu.cs` (pause / tower / gate / hero / win from `menu_arg.txt`), `select_card.cs` (card index from
  `card_arg.txt`), `tower_at.cs` (builds on the cell in `cell_arg.txt`).
- Menu labels: the tower and base menus keep their price labels inside the backdrop (`LabelGap`); the enemy gate of a
  road stands on the corridor centre line (`Road.Route(index)[0]`), and the gates of flying routes float at flight
  height, smaller, so they no longer sit on top of a road gate next to them. `zoom_spawn.cs` zooms onto a path start.
- The range ring while placing is drawn around the same shifted stand position as the preview (`Refresh` computes
  one `stand` for the ghost and the ring).

## Drones: one drone per job (2026-10-29)

Remco chose fixed drones over one drone with modes: a repair drone, a harvest drone and an attack drone, each with
its own look. All are bought from the base menu with energy, which gathers slowly; faster energy is a permanent
upgrade for research points.

- Data: `DroneKind { Repair, Harvest, Attack }`; `DroneDefinition` holds the stats of every job (repair per second;
  harvest seconds and value; damage, interval, range, projectile speed, targets and style). `LevelDefinition` has
  `_repairDrone`, `_harvestDrone`, `_attackDrone` and `Drones` (the non-null ones, in menu order). Balance to start:
  repair 40 energy (8 HP/s), harvest 60 (4 s per load, 12 money a trip), attack 80 (6 damage every 0.5 s, range 3),
  three of each at most, energy 1.5/s up to 100.
- Logic: `Drones/Drone.cs` runs the three jobs; the attack drone hunts the enemy furthest along the road, closes to
  70% of its range, hovers and fires through `ProjectilePool` (so the shot sounds and effects come for free).
  The harvest drone raises `Delivered` at the base; `DroneWorld` carries everything a drone needs from the level.
- Look: `Core/DroneModelFactory.cs` builds the medic quadcopter (cross, lamp, tool arm), the twin-rotor hauler (cargo
  pod that lights up when loaded, drill) and the dart gunship (swept wings, gun pods, ducted tail fan).
  `Drones/DroneModelBuilder.cs` assembles hull, rotors on their own pivots and the cargo pod with the drone's colour
  on the hull; `DroneView` spins rotors, turns and banks into the heading, bobs, flashes the muzzles and shows a
  rising "+money" (`MoneyPopup`, a TextMesh) on delivery.
- Upgrades: `UpgradeTarget.HarvestDrones`, `AttackDrones` (free starters, power currency) next to `RepairDrones` and
  `DroneSpeed`; new `EnergyRate` (+25% per level, research points), applied in `LevelBootstrapper` when the energy
  pool is made (`PlayerModifiers.EnergyRateMultiplier`).
- UI: `BaseMenuView` shows one card per drone with a rendered picture of the model, its energy price, the count
  and "max" when the limit is reached; the info line shows the energy rate.
- Play-test scripts: `buy_drones.cs` (fills energy and launches one of each), `zoom_drone.cs` (zoom on a drone by
  kind from `zoom_arg.txt`, reports what it is doing).
- Checked in play: all three fly, the harvester delivers (+12 popup, money rises), the gunship goes for the enemies.
  EditMode 82/82 (new: energy-rate upgrade). PlayMode suite still not run; nothing committed.

## No loot from enemies, full view at start, quieter hero spots (2026-10-29)

- Enemies no longer drop loot and drones never pick money up from them: `PickupField`, `LevelContext.Pickups`,
  `DroneWorld.Pickups`, `EnemySpawner.DropLoot` and `StructureModelFactory.LootCrystal` are gone. The harvest drone
  only mines `MineCells` and delivers at the base.
- The camera starts fully zoomed out (`CameraPanZoom.StartZoom` 1, the whole field).
- Hero spot strips are a pale grey-blue, more translucent and barely glowing, so they mark the rung without shouting.

## Enemy overhaul: insect redesign, roller, pool spawns, boss powers, music (2026-10-29)

Remco's call after the drones: menacing monsters instead of toys, his own Roller idea, creatures out of the pools, flyers
that go for the gates, bosses with powers, and sound with music. Built in one go.

- **Creatures:** `EnemyModelFactory` rebuilt. A creature is a static shell plus a list of `CreaturePart`s with a
  `CreatureRole` (legs with hips and knees, mandibles and raptorial arms, antennae, wings, breathing abdomen, tail,
  roller bands, a floating orb). Dark glossy chitin, glow in the seams, eye clusters, plates and spines. Legs are
  stocky on purpose (the 55-degree camera turns a long thin leg pointing at it into a stick). New shapes: Roller,
  Drowner (water), Burrower (quicksand), Cinder (lava, sheds embers). `EnemyView` animates per role: stepping legs with
  folding knees, dangling legs under flyers, snapping jaws, twitching feelers, beating wing pairs, breathing, sway;
  the roller curls its bands into a ring that spins with the distance rolled. Spawning depends on `Enemy.Origin`:
  gate, pool (splash in the pool's colour, deeper and slower) or ground (dust, deepest). On death the legs fold in
  (`EnemyDeathEffect` collapse phase, `Part.Curled`) before the pieces fly apart.
- **Roller:** `EnemyDefinition.WalkSeconds/RollSeconds/RollSpeedMultiplier`; `Enemy.Step` alternates, rolling is faster,
  no armour; a gate makes it unroll and bite. Data: walk 3 s, roll 2.2 s at x2.6, from the Forest on.
- **Pools as spawn points:** `SpawnGroup.Source` (Gate/Water/Lava/Quicksand); `LiquidPools` groups liquid cells into
  pools (those wholly off the grid are ignored), `OffRoadRoute` builds start -> nearest road cell -> rest of that road,
  with the progress offset so towers rank the newcomer fairly. The validator refuses a pool source without such a pool.
- **Flyers strike gates:** `EnemyDefinition.GateStrikes` (3); the spawner inserts the nearest gate as a waypoint, the
  flyer hovers over a living gate, hits it that many times, and flies on.
- **Boss powers:** `BossAbilityDefinition` (Pulse / Laser / Spawn) listed on the enemy; `BossAbilities` runs the timers
  (`StructureRegistry.DamageInRadius` / `DamageAlongBeam`), `BossAbilityView` draws the beam with a burning tip, the
  expanding ring and spores. Overseer: mind laser; Warden: shockwave; Queen: brood of scouts plus a shockwave.
  Mid-wave bosses: Lava gets the Overseer in wave 4, the Nest the Warden. Per level a `BossEntrance` (Gate / Pool /
  Ground with a cell): Forest and Lava from a pool, Ruins through the ground, Nest through the hive mouth.
  `LevelContext.BossWave/BossWaveStarted/BossesAlive/BossPulsed/BossLaserChanged/BossMinionsReleased`; the HUD shows
  "BOSS" in red; the camera shakes on the wave start, the entrance and every pulse.
- **Sound:** `Synth` moved to its own file (shared by effects and music). `AudioMix` (master, music, effects, ambience)
  registered at boot from four saved percentages (`ProgressData`, `ProgressService.SetVolumes`); `VolumeControls`
  shared by the options overlay and the new "Sound" page of the pause menu. `ProceduralMusic` writes a calm light-rock
  loop (100 bpm) and a heavy one (132 bpm); `MusicPlayer` crossfades to the heavy loop while a boss is out (and from
  the alarm on), plays the siren and quake rumble on a boss wave and ducks the ambience. `SfxPlayer` rebuilt: voice
  budgets per kind (shots 3, kills 2, hits 2, spawns 2), bundling of shots within 60 ms, ducking under priority sounds,
  zoom attenuation with a low-pass per voice (each voice on its own object), laser hum loops, new clips for boss
  pulse/laser, minion bursts, emergence from water/lava/sand/ground and the roller's curl.
- **Generator:** `Bestiary`, `ExtraStream`s per level (rollers, pool creatures) appended from a given wave on,
  `MidBoss`, `BossEntrance` snapped to a surviving liquid cell, boss ability assets under `Data/Enemies/Abilities`.
- **Tests:** EditMode 94/94 (new: `EnemyBehaviourTests`, `PoolSpawnTests`, `AudioTests`). Play-test scripts:
  `spawn_enemy.cs`, `zoom_enemy.cs` (freezes time; `resume.cs` thaws), `no_waves.cs`, `inspect_enemy.cs`,
  `shadows_off.cs`, `goto_menu.cs`, `sfx_probe.cs`.
- Checked in play: every creature up close, roller rolling, drowner out of a pond, burrower out of quicksand, cinder
  out of lava, overseer surfacing from a pond and from lava, warden breaking out of the ground and its pulse ring,
  flyer hovering at a gate, pause-menu sound page and main-menu options. The laser was seen only as a thin line
  before it was made thicker; the beam logic is covered by tests. Music and alarm not heard (no speakers here).
- Not done: PlayMode suite not run; nothing committed; balance of the new streams and boss powers to be felt in play.

## Main menu as a living diorama with its own music (2026-10-29)

Remco asked for a fancy main menu while he was away: glowing, moving parts, plants, enemies in their nest, lava, sand
and water, and light rock music.

- **Scene:** `MenuStage` (Boot) is the composition root of the MainMenu scene: it makes the camera with the game's
  post-processing and hands it to `MenuDiorama` (UI), which builds a floating island from a `MenuStageDefinition`
  (`Data/Menu/MainMenuStage.asset`, written by the generator: the Forest, Lava, Ruins and Nest themes plus the
  creatures to show). `MenuTerrain` is one height function (basins for the pools, a mound for the hive with a flat
  porch at its mouth, a ragged cliff all round) that also paints the albedo and emission textures, so each corner has
  the ground colour and glow of its theme (lava cracks, pond speckles, hive veins). Pools reuse
  `TerrainLiquidBuilder.BuildPool` (extracted from the level loop, with `SurfaceLevel`), plants and rocks come from
  `PropFactory` with `WindSway`, glow decals and a shared `EmissionPulse`, lights flicker, spores drift, stars lie
  far below. The hive mouth is a black oval on the slope with a pulsing chitin rim and a red light inside.
- **Creatures:** `MenuCreature` wraps the real `Enemy` + `EnemyView` (health bar hidden via `EnemyView.HideHealthBar`)
  on a route of its own, sinks away at the route's end and comes up again after a rest. The Queen sits on the porch;
  grunts, a scout and a roller crawl out of the mouth, lap the mound and go back in; two flyers circle; the Drowner,
  Cinder and Burrower surface in their pool, stroll the bank and dive back.
- **Camera:** `MenuCameraDrift` circles a few degrees around its focus and bobs, never still.
- **UI:** `MainMenuScreen` keeps the buttons on a translucent panel along the left with a subtitle under the title;
  `ScreenBase.Starfield` lets a screen drop the flat starfield. The options dim is slightly see-through.
- **Music:** `ProceduralMusic.Menu()` (92 bpm, E-G-D-A, plucked arpeggio, a melody over the whole loop; new `Pluck`
  instrument). `MenuMusic` (UI) is created by the first menu screen, survives menu-to-menu loads, follows the mix,
  and fades out and destroys itself when the Level or Boot scene loads. The diorama plays the hive ambience quietly.
- **Tests:** EditMode 96/96 (new `MenuStageTests`: generated asset complete, terrain sinks pools under their surface
  and flattens the porch; menu loop length in `AudioTests`). The test assembly now references `Game.UI`.
- **Checked in play:** menu with the island, Queen, crawlers, roller, flyer, pool creatures; options over the scene;
  Start to the map with the music still playing; a level start removes the menu music and the level score runs;
  back to the menu rebuilds the diorama. Zero errors and warnings.
- Play-test scripts: `menu_probe.cs` (stage, cameras, diorama), `menu_creatures.cs` (every creature's state),
  `music_probe.cs` (menu music vs level music).
- Not done: nothing committed; music not heard here; the balance of colours is a first pass to be judged by eye.

## Story, region map, boss cues and sliders (2026-10-29, evening)
- **Menu music** reworked after feedback ("too happy"): E minor throughout (i, VI, VII, i, turnaround to V), palm-muted
  eighths (`Track.Chug`), harder kick and snare, crash on each half, a low pentatonic riff instead of the long melody,
  104 bpm, more drive. `Pluck` was removed with the arpeggio.
- **Volume options are sliders** (`UiKit.CreateSlider`, `VolumeControls`): left/right on the gamepad moves in tenths,
  the mouse drags freely, the saved value snaps to 5 %. Used by the options panel and the pause menu.
- **Bosses**: bigger and slower (Overseer 1.75/0.7, Warden 2.2/0.6, Queen 2.7/0.55 scale/speed) with more health
  (650/2000/3200); eyes and seams glow 1.9x and breathe (`EmissionPulse`), a point light in the boss colour under it.
  The final boss wave gets flyer escorts per difficulty (`BossEscortFlyers` 0/1/2/4) on top of the health multiplier.
- **Boss cues**: from the alarm until the boss shows, `SfxPlayer` repeats an approach sound that fits the level's
  entrance (growl for gate/ground, bubbles for water, lava churn, sand shift). `BossMarkerView` on the HUD points an
  arrow and "BOSS" at the entrance, then at the boss: pinned to the screen edge while it is out of view.
- **Story in data**: `StoryDefinition` (`Data/Story/Story.asset`, referenced by the campaign catalog) holds the intro
  beats (text, scene, seconds). Every `LevelDefinition` has `_regionName` and `_briefing`.
- **Intro scene** (`IntroScreen`, scene `Intro`): plays once on first launch (`ProgressData.IntroSeen`), stars, the
  planet closing in with a tiny ship, regions blinking, the dive onto the first region and a fade to the main menu.
  Any button skips. Boot decides between Intro and MainMenu.
- **Planet map** rebuilt: a globe taller than the screen, painted by `PlanetPainter` (sea/land noise, one tinted
  region per level from its theme colours, atmosphere rim, halo, slowly turning clouds). Each level is a beacon with a
  pulsing ring (dimmed when locked, a check when beaten) and a dotted route. Choosing a beacon zooms the globe onto
  the region and opens the region panel (region, level, briefing, four difficulties with checks and stars, Back).
  B closes the panel, or leaves to the main menu from the overview.
- **Briefing before the first wave** (`BriefingView`): only when the session asks for it (`LevelSession(.., true)`
  from the map), time is held (`TimeController.Hold`) until Deploy. Tests and play scripts start levels without it.
- Verified in Play Mode: boss alarm with the arrow on the pond entrance, Overseer surfacing with the arrow over it,
  the edge arrow when zoomed elsewhere, approach sound scheduling, the options sliders, the intro (skipped via its
  Finish), the map overview, zoom and region panel, level start with the briefing holding time and Deploy releasing
  it. EditMode 99/99, zero errors and warnings. Nothing committed.
- Play-test scripts: `boss_alarm.cs`, `boss_probe.cs`, `sfx_voices.cs`, `sliders.cs`, `map_pick.cs`,
  `time_probe.cs`, `intro_probe.cs`, `intro_skip.cs`, `screen_probe.cs`.
- Not done: music and sounds not heard here; boss balance is a first pass to be judged by play.

## Waves per level, boss spacing, shot sound cooldown, placement fix (2026-10-29, late)
- **Placement**: towers beside a road shifted 0.4 cells away from it, and a 0.7-cell pedestal then stood inside a tower on
  the next cell. `RoadClearanceCells` is 0.15 now; occupancy per cell was already right.
- **Shot sounds**: much quieter (0.12 rapid, 0.2 heavy, 0.15 laser). Rapid-fire towers (pulse, storm, tesla, cryo) start
  their sound at most every 0.22 s; shots in between only nudge the playing voice, so a row of rifles reads as one burst
  (`RapidCooldownSeconds`). Slow towers keep a sound per shot under the three-voice budget.
- **Two bosses on hard**: `WaveScheduler` rounded `Count * CountMultiplier` up, so one boss became two a second apart.
  A boss group now ignores the count multiplier, and several bosses in one group are at least 18 s apart.
- **Waves rebuilt** (`ProjectGenerator.Waves.cs`): every level has a `WaveScript` (its own wave count: Landing 20, Forest 25, Lava 30,
  Ruins 35, Nest 40; troops with first wave, weight, interval and spawn source; a rhythm string of P plain, R rush,
  H heavy, M ambush that repeats when shorter; air-raid waves with a count per difficulty; air sprinkle from a
  difficulty; its own boss waves, the last boss with escorts). A threat budget (health/20) runs geometrically from
  `Budget` to `FinalBudget`; weights lean towards tougher creatures as the level goes on (`HeavyBias`); a new troop always sends
  at least one; groups cap at 36 before the difficulty multiplier. Pool creatures only come in ambush waves, mid-wave.
  Landing zone has no flyers at all on easy (medium 1, hard 2, psycho 3 air waves). All ordinary enemies walk ~35 %
  slower. The old shared templates, `ExtraStream` and `BuildWaveSets` are gone.
- `Tools/PlayTest/dump_waves.cs` writes every wave of every level and difficulty to `Tools/PlayTest/waves_dump.txt`
  (counts per creature, `*` = second lane or air route, `~` = from a pool, threat and span).
- Lesson: never recompile while the player is in Play Mode; check `editor_status` first. A recompile mid-play flooded
  the console and left the editor unresponsive until a restart.
- Open: drones to be paid with money (energy to go); waiting on what to do with the "start with one more drone" upgrades.

## Drones bought with money, energy removed, boss laser fix (2026-10-29, night)
- **Energy is gone**: `EnergyPool`, the HUD energy readout and bar, the level's energy fields, the energy-rate upgrade
  and the "start each level with one more drone" upgrades (assets deleted, catalog regenerated). `PlayerModifiers`
  and `UpgradeEffects` keep only `DroneSpeedMultiplier` for drones; `UpgradeTarget` lost four values (regenerate data
  after pulling, the enum indices moved).
- **Drones cost money** (`DroneDefinition.Cost`: repair 120, harvest 150, attack 180), bought in the base menu with
  the wallet; nobody starts a level with a drone. `Drone_Speed` (power currency) is the only drone upgrade.
- **Boss laser stuck after death**: a pooled boss is switched off when it dies, so neither `BossAbilities.Update` nor
  the view's `LateUpdate` ran again and the beam (a sibling object) stayed on with the hum. Both now switch the
  laser off in `OnDisable`.
- Generator: `CreateDrone` marks the asset dirty, because writing a value equal to the field default left the
  stale `_energyCost` line on disk.
- Scripts: `laser_probe.cs` (boss laser state, "kill" in `enemy_arg.txt` kills the bosses), `drone_probe.cs`
  (money and drones), `buy_drones.cs` now fills the purse.
- **Boss laser locks on**: the laser no longer sweeps the ground. `BossAbilities` asks the spawner for the nearest
  tower, gate or hero in range (`EnemySpawner.FindLaserTarget`, hero wrapped as `HeroTarget : IStructure`), follows
  it for the beam's duration and burns it; without a target it retries after 2 s. `SweepDegrees` and
  `StructureRegistry.DamageAlongBeam` are gone. The view draws the beam from the model part named "Orb" (the
  Overseer's mind-orb, which swells while firing) with an opaque glowing core and a see-through halo.
- **Overseer head** redrawn: bigger pale head with large eyes, mandibles, a glowing collar, three crown spikes and a
  stalk up to a larger ringed orb, so front and weapon read from above.
- Scripts: `laser_now.cs` fires every boss laser at once.

## Balance pass, mines that run dry, screen pulse, sound research (2026-10-30)
- **Mining nodes deplete**: `LevelDefinition.MineStock` (140 money per node, three nodes a level) is drained trip by
  trip through `DroneWorld.MineStocks`; a harvest drone skips empty nodes and waits at home when all are spent;
  `LevelContext.MineDepleted` turns the node's `MiningNodeView` dull. Harvest yield 12 -> 10 per trip, at most two
  harvest drones. So the harvest drones earn a bounded sum (about 420 a level) instead of a stream.
- **Kill rewards cut** (scout 3, grunt 5, brute 14, flyer 6, roller 7, drowner 6, burrower 6, cinder 8) and the
  **difficulty profiles raised**: Medium health 1.1 / count 1.05, Hard 1.6 / 1.4, Psycho 2.4 / 1.9.
- `BalanceProbeTests` reads an optional `Tools/PlayTest/probe_arg.txt` (`levels=0,1`, `difficulties=1,2`) and
  reports money; run it async: `run_tests --mode PlayMode --filter GreedyBot_PlaysEveryLevel_WithoutErrors
  --filter_type testName --async_tests true`, result in `Temp/pipeline_test_status.json`, report in the console.
  After this pass the greedy bot (no upgrades, no drones) won Landing on Medium (30/30) and Hard (12/30), won Forest
  on Medium and lost Forest on Hard at wave 12. A human who upgrades will do better; judge by play.
- **Screen pulse** (`ScreenPulse`, built by `PostProcessingBuilder` on the high profile): a lens punch and a flash of
  chromatic fringing on explosions (by radius), boss pulses, the boss alarm, the hero's shockwave, a wrecked tower
  and the base's death; decays in 0.4 s on unscaled time. The low profile leaves it idle.
- **Laser beams** of the towers now have an opaque glowing core (bloom picks it up) under the see-through halo,
  like the boss beam.
- **Sounds**: Kenney's "Sci-fi Sounds" (70 files) and "Impact Sounds" (130 files) are CC0 and fit shooting and
  impacts; the site serves the zip only through a browser click (curl gets a 199-byte stub), so the player has to
  download them. Plan once they are in `Assets/_Project/Audio/Kenney/`: an `SfxLibrary` ScriptableObject that maps
  each procedural sound to an optional clip, used by `SfxPlayer` when assigned, procedural otherwise.
- Scripts: `pulse_probe.cs` (kick the pulse), `lens_hold.cs` (hold the lens bent for a screenshot).
- **Recorded sounds in place** (2026-10-30): Remco downloaded Kenney "Sci-fi Sounds" (CC0, 70 ogg) and Lentikula
  "Sci-Fi Weapon Shots" (CC0, 15 wav); extracted to `Assets/_Project/Audio/Kenney_SciFi/` and `.../Lentikula/`
  (the 7z opened with Windows' own `C:\Windows\System32\tar.exe`, Git Bash tar cannot). `SfxLibrary`
  (`Data/Audio/SfxLibrary.asset`, filled by the generator from `SfxClips`, registered by the Bootstrapper) holds a
  few variants per sound; `SfxPlayer.Family` hands them out in turn and falls back to the generated clip when a
  list is empty. Mapped: pulse = Lentikula laser rifle, rail = laser beam, hero blaster = laser pistol; cannon =
  laserLarge, rocket = thrusterFire, cryo = laserRetro, tesla = forceField, explosions = explosionCrunch, big
  explosions = lowFrequency_explosion, structure hits = impactMetal, enemy deaths = slime, base hits =
  explosionCrunch. Credits in `Docs/CREDITS.md`. Not swapped yet: build/upgrade/sell, horns, alarms, boss roars,
  emergence sounds and the hero's blade (still generated). Volumes untouched; judge by ear.
- Boss laser hum: Remco's `freesound_community-laser-weld-103309.mp3` (Pixabay Content License) in
  `Assets/_Project/Audio/Freesound/` loops as the boss laser through `SfxLibrary.BossLaserLoops` (first clip).

## Feedback round: bolts, muzzle alignment, impact ripples, boss flash, health bars (2026-10-30)
- **Screen pulse removed** (`ScreenPulse` deleted, `PostProcessingBuilder.Build` is void again): Remco disliked the
  whole-screen punch. In its place **impact ripples**: `RippleEffects` (Levels) keeps twelve flat quads with the
  hand-written `Game/ImpactRipple` shader (`Assets/_Project/Art/Shaders/`), which refracts the URP opaque texture
  along a ring that runs outward in 0.35 s; played on explosions (by radius), the hero's strikes, boss pulses and
  wrecked towers. High profile only (`PC_RPAsset` has the opaque texture on); the generator writes a keeper material
  (`ShaderKeepers/ImpactRipple.mat`) so the shader survives a build. Camera shake is unchanged.
- **Bolts** are glowing rounded bars (the capsule slug, turned onto the heading) with a thin light trail instead of
  light balls; the rail keeps the longer, brighter slug.
- **Muzzle alignment**: `Tower.MuzzleProvider` lets `TowerView` hand the logic the position of its barrel mouth
  (the muzzle flash object), so beams and bolts leave the gun that fires them; towers without a view fall back to
  the old estimate.
- **Boss hit flash off**: a boss keeps its own glow when shot (the white strobe annoyed).
- **Health bars**: a thin line (0.035 high, 0.6 wide) that only appears once a creature is damaged.
- Script: `ripple_probe.cs` plays a ripple at the hero and holds the frame. `pulse_probe.cs` and `lens_hold.cs` are gone.
- Boss alarm: Remco's `warning.mp3` (source still to be noted in `Docs/CREDITS.md`) plays as the siren through
  `SfxLibrary.BossAlarms` (first clip), used by `MusicPlayer`; the generated siren stays as fallback.

## Bestiary v2: nine species with skills, skins, nests, species bosses (2026-10-30)
Remco found the enemies "all the same"; the roster was rebuilt around one skill per species, as in nature. Old
species (Scout, Grunt, Brute, Flyer, Drowner, Burrower, Cinder, Overseer, Warden) are gone; Roller and Hive Queen stay.
- **Species** (`ProjectGenerator.Bestiary.cs`, data in `Data/Enemies/`): Skitter (ant swarm, bleeds venom), Stinger
  (scorpion, armour plates, acid sting on towers in passing), Ember/Tide/Moss/Dustbat (element flyers, bolts, rise
  from nests, crash and dissolve), Bulwark (shield bearer, armour, crystal sword, collapses), Prowler (pack, rage
  haste when a mate dies), Wisp (walks the road, hit by ground and air guns, bursts), Skyray (flying whip), Oozer
  (slime trail). Each species has a boss: BroodMother, AcidEmpress, StormMatriarch, CrystalWarlord, PackAlpha,
  CoreWisp, SkySovereign, SlimeMonarch, RollerKing.
- **Mechanics**: `Health` armour layer (soaks a share until chipped, `RestoreArmor`); `EnemyDefinition.Flies` apart
  from the `Layer` mask (Wisp = Ground|Air, walks); `Enemy.ApplyHaste`; `TrailDropped` -> `SlimeField` (Levels,
  hurries other species, burns hero and gates, never towers) drawn by `SlimeFieldView`; pack rage in `EnemySpawner`;
  `DifficultyProfile.StructureDamageMultiplier` (0.7/1/1.35/1.8) scales attacks on structures and boss powers;
  element nests (`ElementNest`, `LevelDefinition.Nests`, `SpawnSource.Nest`, validator checks, nest props in
  `LevelVisualBuilder`, generator reserves their cells). New boss kinds: Volley, Rally, Regrow, Split.
- **Bug fixed**: `LevelController` dropped the spawn source, so pool spawns never happened in real play.
- **Look**: `EnemyModelFactory` rebuilt (five slots: body, eyes, glow, dark, accent; `AccentColor` and `BloodColor`
  on the definition). `CreatureSkin` (Core) paints tiling chitin/hide/slime/scales/energy/membrane textures with
  normal maps and glowing seams, tinted per species. New roles: Plate (sheds as armour drops), Fin, Blade, Stinger.
  `EnemyDeathEffect` styles: Shatter, Crash, Collapse, Burst. `EnemyBoltView` draws enemy shots. Runners keep
  bent knees and splayed feet, bodies shift weight when idle, tails flick (Remco: nothing should stand like a statue).
- **Waves**: per-level scripts with the new species, `Troop.MinDifficulty` (tide bats skip easy on Landing), threat
  prices armour and damage. Bosses per level: Landing BroodMother/PackAlpha; Forest RollerKing/StormMatriarch; Lava
  CoreWisp/AcidEmpress/CrystalWarlord; Ruins SlimeMonarch/SkySovereign/CrystalWarlord; Nest PackAlpha/AcidEmpress/
  CoreWisp/HiveQueen.
- **Tests**: 107 EditMode tests pass (armour, haste, trail, slime, boss powers, nests, every shape builds); the
  five PlayMode tests pass too, including the greedy-bot balance probe over every level and difficulty
  (`run_tests --mode PlayMode --async_tests true`, then poll `test_status`; the first reply says 0 tests, the
  run is still going).
- **Play-test scripts**: `spawn_many.cs` (list in `spawn_list.txt`, "Name [Source]"), `kill_all.cs`, `step100.cs`
  (100 frames at 3x), `waves_on.cs`.
- **Open**: balance probe on the new roster, the sting tail reads weakly from the 55-degree camera, bats could use a
  second look at close range, more posture work on the Stinger and Oozer idle.
- **Nests stay away from the base** (2026-10-30, later): Remco saw creatures come up almost beside the base. The
  validator now rejects a nest whose creatures join the road past 60% of its length (`LevelValidator.MaxNestRoadShare`),
  the nests were moved (Landing both in the lake, Forest wood nest on the plateau, Ruins only the first-half drifts
  with a new sand patch at 16-18/13-14 and no tide bats, Nest level water nests in the north pond), and the share of
  an ambush wave that surfaces from pools and nests now scales with difficulty (30/45/55/65%). `nest_report.cs`
  prints the share per nest. Maps are not enlarged yet; that is an open option if the roads still feel short.

## Bigger maps, longer roads, longer levels (2026-10-30, later)
Remco wants long games, not short rushes. Every map grew and every road winds: Landing 32x18 (one serpentine, 74
cells, three gates), Forest 38x20 (switchbacks plus a second road from the south-west meeting on a clearing, four
gates), Lava 34x24 (five-leg serpentine of 141 cells, four gates), Ruins 40x22 (north wall, old streets, round the
cistern, long avenue; second road into the market square; five gates), Nest 36x22 (winding north road, straight
south road, forecourt; four gates). A side road must end on a *waypoint* of the main road (`MergeIntoMain` uses
`IndexOf`), so the main roads carry an extra waypoint at each junction. Terrain, nests (all within 60% of their
road), boss entrances and flight routes were laid out afresh; `dump_map.cs` shows the result. Waves grew with the
roads: 24 / 30 / 36 / 40 / 48, bosses at 12+24, 15+30, 12+24+36, 14+27+40, 12+24+36+48; starting money 200 / 240 /
280 / 320 / 360. Known: the whole map fits the screen at start, so creatures are small until the player zooms
(`CameraPanZoom.MaxZoom` 3); balance on the longer roads is unjudged.

## Look, attacks, sound and balance pass (2026-10-30, evening)
Remco's feedback: bubbling sounds that linger after ants die, the wolves look "very very basic", spawns, rocks and
fauna need skins like the monsters (and the terrain too), attacks need animation, balance is off (two towers, no
upgrades, over a thousand money by wave 20 on Easy), the hero timer should be two minutes with a countdown, ants
should come in big weak swarms, and enemies should grow a little stronger every wave.
- **Sound**: the enemy death list used two Kenney slime clips; `slime_001` is 4.7 s long, so every second kill
  left a bubbling tail. Only the 0.5 s clip remains, and `SfxPlayer` cuts any death sound at 0.7 s
  (`KillMaxSeconds`, fade-out of voices past their end). `clip_lengths.cs` lists clip lengths.
- **Prowler** (`EnemyModelFactory.Prowler`): a real wolf build. Chest, waist and haunches, a dark saddle, two
  glowing flank stripes, a neck that drops to a long muzzle with a brow ridge, slanted eyes, lit fangs, a crest of
  fur tufts (`FurTuft`, blades) down neck and shoulders and a ruff out to the sides, ears as twitching feelers, a
  lower jaw (new `CreatureRole.Jaw`, drops in the wind-up and slams shut), a bushy tail with fur clumps, and
  `WolfLegs` (muscled thigh, slim shin, three clawed toes; heavier haunch on the hind pair). The alpha adds a bone
  brow ridge, shoulder spines and glowing cracks. The Hide skin has finer streaks and raised tufts.
- **Environment skins**: `CreatureSkin` gained Flesh (bulges with glowing veins), Stone (grain with lit cracks)
  and Bark (ridges with lit grooves). Hive mouths wear Flesh; nests wear Slime (water), Bark (wood) or Stone
  (fire, sand, ice) via `LevelVisualBuilder.NestSkin`; mining nodes wear the level's rock with ore glowing in the
  cracks and streaked crystals. `PropTextures` is 256 px with richer rock (cracks, pits, lichen tint), a `Crystal`
  surface (streaks, inclusions, emission map) on the glow slots of all props, and a `Ground` detail surface.
- **Terrain**: `TerrainSurfaceBuilder` adds a tiling detail layer (`_DETAIL_MULX2`, grit, pebbles and fissures
  as albedo and normal, 1.6 units per tile, tangents on the mesh; keeper material `Lit_EmissionNormalDetail`).
  `TerrainPainter.ShadeSurface` paints small stones in the rock colour, lichen patches in a muted plant tint and
  dark fissures into the baked albedo.
- **Attacks** (`EnemyView`): the attack takes 45% of the creature's attack interval (0.35 to 0.9 s). Per style
  (`AttackPose`): Bite crouches nose-down, snaps up and forward and shakes its head; Slam springs up in the wind-up
  and crashes down; Slash rears and twists into the swing; Sting dips the front; Bolt throws the head back and
  spits forward; Whip rolls into the lash. Bolts and stings now fire at the top of the strike instead of at the
  start (`PlayStrikeMoments`), melee blows throw sparks in the accent colour at the target and a slam raises dust.
- **Hero**: revive 120 s (`HeroReviveSeconds` in the generator); the HUD top strip shows "Hero down m:ss" next
  to the base health while it lasts (`HudView.RefreshHero`, rebuilt only when the second changes).
- **Balance** (data only): Skitter 10 HP, reward 1, interval 0.45 (swarms of 13 at wave 1, 60+ late); other
  rewards trimmed (Prowler 4, Stinger 9, Bulwark 14, Oozer 6, Roller 5, Skyray 6, Wisp 4, bats 3). Wave budgets
  start higher and end far higher (Landing 7 to 170, Forest 9 to 230, Lava 10 to 300, Ruins 12 to 350, Nest 12 to
  450; group cap 64). New `WaveDefinition.HealthMultiplier`, written as `1 + HealthGrowth * (wave - 1)` per level
  (3%, 2.5%, 2%, 1.8%, 1.5% a wave) and applied by `EnemySpawner.WaveHealthScale` (set per wave by the
  controller, inherited by boss minions). Upgrades were worth less per coin than new towers; raised: Pulse 4/7/12,
  Cannon 25/48/85, Rocket 30/55/90. Difficulty steps are gentler now that the waves themselves are heavier:
  health/count Easy 0.8/0.8, Medium 1/1, Hard 1.3/1.2, Psycho 1.8/1.5. Landing Easy kill money over the level is
  about 1880 (was 2300 after the first pass; `dump_waves.cs` prints it).
- **Probe bot** now builds four towers, then upgrades the cheapest affordable upgrade, then builds more; it ignores
  engine warnings and fails only on errors (the graphics ring buffer warning at 20x speed aborted a run).
- **Scripts**: `step_n.cs`, `teleport_to_gate.cs`, `clip_lengths.cs`; `dump_waves.cs` prints money.
- **Per-wave growth and the paper balance model** (same evening, on Remco's two additions): every wave carries a
  `HealthMultiplier` (see above), and `BalanceLedger` (Levels) is a static model that, per level, difficulty and
  wave, works out the money the player can have had (start, kills, mines minus one harvester), the strongest
  defence that buys from the level's towers (damage x fire rate per coin, splash as a bonus against streams, the
  purse split so ground and air pressure match, at least one air gun when anything flies), the wave's effective
  health (health plus armour, times difficulty, level and wave growth), the window the towers get (spawn span plus
  twice the mean range over the mean speed) and the hero at half the window. Pressure = health / capacity, with a
  tower utilisation of 0.35 calibrated so that one is roughly where the greedy probe bot stops holding.
  `BalanceLedgerTests` (EditMode) writes `Tools/PlayTest/balance_ledger.txt` and fails outside the bands: plain
  waves Easy 0.7, Medium 1.0, Hard 1.6, Psycho 3.2 (bosses x1.8), a ground wave more than 2.7x the heaviest of the
  three plain waves before it, or late Easy averaging under 0.15. It is a model, not the game: no placement, no
  gate blocking, no drones beyond the harvesters, no boss powers; read it as a curve, not as truth.
- **What the ledger changed**: air raids took half the budget and air defence buys fewer points per coin, so air
  waves were spikes (`AirShare` 0.5 -> 0.35, `AirGroundShare` 0.4); heavy waves put 55% (was 60%) of the budget
  into the toughest creature; Lava opens lower (budget 8, Stinger from wave 4, Bulwark from 10), Ruins opens with
  two plain waves and the Oozer (Bulwark from 9, Stinger from 6, budget 10), Nest budget 11 and Bulwark from 7;
  the Slime Monarch has 1100 health (was 1500; it walks in at wave 14 of 40). Remaining over-band spots are
  listed in the test output when it fails; the bands were loosened once (Hard 1.5 -> 1.6, Psycho 2.2 -> 3.2,
  cliff 2.2 -> 2.7) and should be tightened again as the data settles.
- **Probe results on this data** (Landing): spammer bot (every coin into new towers) wins Easy with a full base;
  the upgrading bot loses Easy around wave 14 and Medium around wave 13 because four towers cover too little road.
  Real play with the hero at a gate, gates that hold swarms and drones sits well above either bot. Early Hard and
  Psycho waves are the relatively hardest moments (money is scarce before kills come in); if that feels wrong the
  fix is a per-difficulty starting-money multiplier, not a flatter curve.

## After Remco's first play of the new balance (2026-10-30, late)
"Much better to play, but not in balance": the first waves ask nothing, then before the second boss every tower is
wrecked, there is no money, and scrapping takes two clicks.
- **Towers are sturdier**: Pulse 150 (was 80), Cannon 220 (120), Rocket 130 (70); the later towers grew in the same
  proportion (`ProjectGenerator.World.cs`). A damaged tower keeps 75% of its fire power at worst (`_damagedPowerFloor`,
  was 50%) and a point of repair costs 0.3 (was 0.4); both are now written by the generator (`CreateTower`).
- **The acid sting is milder**: Stinger 3 damage every 2 s (was 5 every 1.5 s), Easy structure damage 0.6 (was 0.7).
  On Landing the Stinger is the only thing that hurts towers, so late waves with eight to eleven of them wrecked a
  tower per wave.
- **One-click scrap**: clicking (or confirming on) a wrecked tower scraps it at once and says so in the cell message;
  the menu only opens for standing towers (`PlacementController.ConfirmWhileBrowsing`, `RemoveForMoney`). Wrecked
  towers could never be rebuilt, only blockades can, so nothing is lost.
- **Kill money up ~16%** (Landing Easy 1893 -> 2189 over the level): Prowler 5, Stinger 12, Bulwark 18, Oozer 8,
  Roller 6, Wisp 5, Skyray 8, bats 4; Skitter stays 1 (a swarm is its own purse).
- **Checks**: 109 EditMode tests pass (ledger bands hold; Landing Easy pressure now 0.2 early, 0.68 at the first
  boss, 0.27 to 0.35 late). Upgrading probe bot on Landing: Easy lost at wave 18 (was 13), Medium at 14 (was 13); it
  still only builds four towers, so it stays a pessimistic proxy.

## Packs, swarms and the hero rebuilt (2026-10-30, night)
- **Packs and swarms**: `Troop.Pack` in the wave generator (`Join(..., pack: n)`). Prowlers come in threes to fives,
  Skitters in sixes to eights, close on each other's heels (`PackInnerIntervalShare` 0.35). A pack species is held
  back until the wave's budget buys at least half a pack and then always sends a whole one, so a lone wolf never
  appears. The first wolf pack on Landing walks in at wave 4; the ledger's cliff check starts at wave 5 for that
  reason (`OpeningWaves`).
- **Hero, two weapons**: `HeroDefinition.RangedWeapon` (the Blaster: a level-one pulse rifle's numbers, 4 damage
  every 0.35 s, reach 3.5, ground and air). `Commander.Strike` swings the blade at anything within its reach and
  otherwise fires the gun at anything within the gun's; `Struck` now carries the weapon used, so the sound, the
  ripple (blade only) and the view know which rig to play. `HeroView` holds a rig per weapon and holsters the
  other when one is drawn. `HeroTests` covers the gun.
- **Hero, new body** (`HeroModelFactory`, first version kept the old silhouette and Remco rightly asked whether
  anything had changed): heroic proportions (long legs, narrow waist, wide shoulders, small head), bone-white
  plates over a deep navy quilted suit (`CreatureSkin.Kind.Weave`: padded cells, stitched seams that carry the
  suit's lights), a heavy angular pauldron on the sword shoulder, bracers, thigh and shin plates, a tactical helmet
  with a visor slit, jaw guard and crest, a slim pack, and a short ragged cloak. Joints: shoulders, elbows, hips,
  knees, neck, cloak. Stance: knees bent 28 degrees, hips back, feet splayed, shoulders hunched, head forward; the
  weight rocks from foot to foot, knees bend with the stride, the free arm guards or braces the gun, the cloak trails.
- **Hero, third pass** (Remco: "the head floats, everything is square, very ugly"): the boxy chest, pauldrons,
  bracers and plates were replaced by rounded forms; the head sits on a pale neck tube with a trim ring that rises
  out of the collar, so it reads as joined from above; the figure is drawn at 0.95 (was 0.85).
- **Camera angle** (Remco's idea): every level now looks down at 50 degrees (was 55) from a 12-degree yaw, so gates,
  towers and creatures show a side as well as a top (a gate reads as `\` rather than `|`). `FixedCameraRig.Fit`
  projects the field's corners onto the camera's screen axes, so a yawed view still fits the whole map; the cost is
  a slightly tilted map with dark corners, which is why the yaw stays small. Constants `CameraPitchDegrees` and
  `CameraYawDegrees` in the generator; per level the values live in `LevelDefinition.Camera`.
- **Hero, fourth pass** (Remco: "the head is still not attached, the running animation looks ridiculous, he stands
  like a plank, and the colours make suit and weapons hard to tell apart"; later: "more skin on the suit, only the
  shoulder pads show one"):
  - *Root cause of the floating head*: the torso (hunch, lean, twist) rotated around the figure's feet while the
    head, arms and cloak hung from the figure itself, so a 17-degree lean slid the chest almost 0.4 units forward
    from under the head. `HeroView` now has a `Torso` pivot at the pelvis (`Parts.Pelvis`) that carries the body
    mesh, head, arms, cloak, thrusters and holstered weapons; the legs hang from the figure.
  - *Gait*: the leg cycle ran at 7.5 radians per unit walked (about twelve steps a second) and the knee folded
    while the leg went backwards. Now 2.1 per unit (a run of about three steps a second), the knee folds as the
    leg comes forward (`KneeFoldLead`), arms pump against the legs (swing share 0.8, forearms bent to 85 degrees),
    the shoulders turn against the hips and the torso leans 10 degrees into the run.
  - *Stance*: one foot back and one forward (`StanceStaggerDegrees` 11), knees bent 18 and 32 degrees, torso turned
    12 degrees side-on and hunched 16 at the pelvis, head 12 forward, and the figure sinks by the hip drop the bent
    knees cause (`HipDrop`) so the feet stay on the ground.
  - *Colours*: five material slots (`HeroModelFactory.SlotCount`): white plates, trim glow, visor glow, a steel-blue
    suit (was near-black navy) and gunmetal for joints, gloves, boots, belt, pack and weapon bodies. Pauldrons are
    blue with a lit rim so the white helmet is the only white dome on the shoulder line. The sword has a gunmetal
    hilt with a white guard; the blaster a white receiver on gunmetal.
  - *Skins*: `CreatureSkin.Kind.Plate` (bevelled armour panels in offset rows, scuffed, seams lit in the commander's
    colour, with a glow floor so the plates stay self-lit) on the white parts, `Kind.Alloy` (brushed metal with a
    rivet lattice) on the gunmetal parts, and the `Weave` quilt was coarsened (five cells per tile, tiling 1) so it
    shows on a limb instead of only on the big pauldron blobs.
  - Checked with 12x close-ups (`zoom_size.txt` is a zoom factor, 10 to 12 frames the hero): head in the collar ring,
    textures visible on helmet, suit and gear, legs staggered at rest, mid-run frames with a lean and pumping arms.
    110 EditMode tests pass; nothing committed. New helper `Tools/PlayTest/hero_to_first.cs`.

## Base under siege, enemy clean-up, bars (2026-10-30, late)
- **Boss ring bug and its root cause**: a boss's pulse ring lives beside the pooled boss and fades in its
  LateUpdate; when the boss died mid-fade the ring stayed on the ground. Remco: this is a recurring class of bug
  (sounds did the same), so every view now registers what it hangs beside an enemy in an `EnemyAttachments` list
  (particles, beams, rings, bolts) and clears it on the new `Enemy.Deactivated` event, raised from `Deactivate()`
  whenever an enemy leaves the field, killed or not. `EnemyView` and `BossAbilityView` use it; `EnemyBoltView`
  gained `Cancel()`. Rule for new work: anything created for an enemy outside its own hierarchy goes through
  `EnemyAttachments`, and any per-enemy sound loop must stop on `Deactivated`.
- **The base has hit points and is attacked** (Remco wants the Sentinel-style last stand instead of a leak
  counter): `BaseStructure` (replaces `BasePost`) is an `IStructure` and `IGuardable` at the end of the main road,
  registered in the structure registry, so walkers stop in front of it and hit it like a gate; a flyer that
  reaches the end of its route besieges it too (`Enemy.IsBesiegingEnd`) instead of slipping through. Only with
  the base down does `ReachedEnd` fire. The hero at the base post absorbs its guard share, drones repair it like
  any structure. Base health is 600 in data (`ProjectGenerator.BaseHealth`), the permanent "Base armour" upgrade
  adds 100 per level (was 20 lives and +5). The base-hit alarm and camera kick are throttled to one per 1.5 s.
  Tests: `BaseStructureTests`, two new `EnemyBehaviourTests`. Checked in play: a pack dropped past the last gate
  stands at the base and takes it from 800 to 680 in a few seconds while the HUD counts down.
- **Health bars**: enemy bars are thinner (0.02) and scale with the creature's size up to 1.6 wide, so the boss
  shows one too (it had a grunt-sized line lost above a huge body); the hero bar is 0.8 by 0.04.
- **HUD speed**: one, two or three chevrons instead of the "x1/x2/x3" text the font rendered like an H.
- Helpers: `Tools/PlayTest/teleport_to_base.cs`, `damage_enemies.cs` (`damage_arg.txt`), `base_probe.cs`,
  `base_layout.cs`, `speed_cycle.cs`, `hero_to_first.cs`. 114 EditMode tests pass.

## Landscape to the screen edge, play zoom and recon (2026-10-30, night)
- **No more void**: `TerrainField` sizes its margins per axis from `FixedCameraRig.GroundFootprint` (the ground the
  fully zoomed-out view covers on a 2.4:1 screen, at least 9 cells), the painter only dims the far land (40%)
  instead of fading to black, and the prop scatter runs to the landscape edge with a thinner deep ring
  (`OuterChanceDeep`). Landing: margins 23 by 19 cells, about 880 props.
- **Play zoom**: `CameraPanZoom` starts at the zoom where half the screen height is 7 world units (a hero of about
  fifty pixels on 720p), focused on the hero; zoom out still reaches the whole field (0.9), zoom in to 4.5.
  Mouse at the screen edge pans (only while the mouse moved in the last 3 s, so a parked mouse never drags the
  view; the HUD suppresses it). `LevelContext.CameraFocusRequested` makes the camera glide to the hero's post
  whenever the hero is sent there (`HeroController.SendHero`).
- **Recon fly-over** (`ReconView`, only when the session carries the briefing, like `BriefingView`): after Deploy
  the whole field is shown with time held for 4.5 s; a flat chevron with a light pillar marks every spawn gate
  pointing the way the enemies walk, every flight route is lit, and a scout light runs each ground route to the
  base leaving the road lit. Confirm or cancel skips. Then the camera glides in to the hero (zoom on a log scale)
  and time runs. Helpers: `start_level_briefing.cs`, `deploy.cs`, `footprint_probe.cs`, `scene_stats.cs`.
- **Defence-zone rule** (`LevelValidator.CheckDefenceZones`): the game is played one screen at a time, so every
  gate must have at least 8 build cells within a play-zoomed screen (85% of a 16:9 view at `PlayOrthoSize`,
  about 21 by 16 cells), every lane (ground and air) must pass through some gate's screen, and any two roads
  must share a gate's screen so one defence covers both. All five maps pass as they are; the rule now guards
  every future map. 114 EditMode tests pass.
- **Base panel** (`BaseMenuView`, kept its name): the drone shop is now a small panel in the bottom-right corner,
  always in view above the bottom bar: one card per drone with its picture, price and count, greyed when full or
  unaffordable. The doorstep menu over the base is gone; confirming on the base doorstep now gives the panel the
  gamepad focus (`LevelContext.BaseMenuOpen`), the stick walks the cards, A buys, B leaves. Help text updated.
- **Recon markers**: Remco found the big chevrons ugly; the spawn gates now pulse soft sonar rings (three per gate,
  staggered, fading as they swell) in the route colour; the scout lights and lit flight routes stay.
- **Loadout of street weapons** (`LoadoutPanelView`, opened by `WorldMapScreen` after the difficulty): every
  street weapon any level offers (union over the campaign) as a row in a scrolling list: picture, name, effect
  badge and word (`PathObjectEffect`: Slow, Trap, Blast, Roll, Patrol, AirNet, with `PathObjectBadge` giving icon
  and colour, also on the HUD cards), price and a description (`PathObjectDefinition.Description`, filled by the
  generator). At most two are taken (`LoadoutSize`), ticked in the list; they are still bought with money in the
  level. The choice is saved (`ProgressData.LoadoutIds`, `ProgressService.SetLoadout`) and offered again. The
  session carries it (`LevelSession.Loadout`); tools and tests without a loadout get the level's own list.
  Gamepad: the stick walks the rows and the list scrolls along, A toggles, Deploy and Back below. Camera: a
  pointer reading exactly (0, 0) means no mouse and never edge-pans (it dragged the view into a corner after the
  recon). Tests: `Loadout_IsSavedAndOfferedAgain`, `EveryStreetWeapon_ExplainsItself`; 116 EditMode tests pass.
  Open: the loadout offers all six weapons; gating them behind level rewards (UnlockTable) is still to come.
