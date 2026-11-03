# Level guide

Rules for building a level that plays fairly. A level is data (`LevelDefinition` plus its `LevelWaves` asset), generated from a
`CampaignLevelSpec` in `Editor/LevelSpecs/LevelSpecs.<Continent>.cs` by `ProjectGenerator.Templates.cs`; nothing here needs code. The validator (`LevelValidator`) and the EditMode tests
(`WorldContentTests`, `BalanceLedgerTests`) check the rules that can be checked.

## Roads and spawns

- One main road from the main gate to the base; side roads may join it. Every land spawn must reach the base.
- Roads are 74 to 141 cells long; a wide road (`RoadCorridor`) gets a second build row on the right of travel,
  and legs keep 5 cells between parallel runs and 3 to a plaza edge.
- Nests (ambush spawns in the field) join the road within the first 60% of its walk from the spawn, never next to the base itself.
- Three to five gates per level, on straight road cells, spread over the walk; a second road gets a gate within a screen
  downstream of its junction. The pure `LayoutChecks` enforce all of this before an asset is written.
  The share of ambush creatures per difficulty is in the `DifficultyProfile`.
- Every extra spawn point (second gate, pool, nest) is a second front the player must hold. Count the fronts
  before adding waves: two is normal, three is a hard level, four only in the second world.

## Bosses

- **Bosses come through the main gate by default**, with the other creatures, so the towers built against the
  wave also fight the boss. This is the rule for the whole first continent (Crystal Plains).
- A separate boss entrance (a pool or the open ground, `BossEntrance.Kind` other than `Gate`) is allowed only when
  at least one of these holds, and the level description says which:
  - it is a late level (second world or later), where the player arrives with stronger permanent upgrades;
  - the level pays clearly more money per wave, so a second defended front is affordable;
  - the road is simple (one road, at most two fronts), so the boss lane is the only extra.
- A boss that regrows armour must stay beatable with the level's own roster: the ledger counts health plus armour
  once, so regrowth is tuned by hand and kept small (see the Crystal Warlord and Acid Empress history in STATUS).
- Boss waves pay for the losses they cause: a boss pays three times the kill money of its threat (`BossRewardFactor`).
  Kill money is the same on every difficulty; the difficulty's reward multiplier pays research, not money.
- Bosses are per difficulty (`BossSpec.MinDifficulty` in `WaveScripts`): every difficulty keeps the level's bosses,
  Hard adds one boss wave and Psycho another, by turning an ordinary wave into a boss wave. The way in is per boss
  and per difficulty too (`BossSpec.Routes`: main gate, second road, or the level's own entrance, written as
  `SpawnSource.Entrance`). The first world stays on the main gate everywhere (`WorldContentTests`).

## Waves

- Wave count and bosses differ per level; never one fixed number. The script sets the crowd (`Budget` to
  `FinalBudget`); the wave solver sets how tough each wave's creatures are (Docs/BALANCE.md). Never hand-tune a wave's
  health multiplier: change the script or the rules and regenerate. Bosses are species bosses of the creatures that
  appear in that level, the Hive Queen only at the end of the world.
- Difficulty adds air; the easy script of a level must be holdable with the starting roster.
- After any change to a weapon, a creature or a wave, run the balance model: `dotnet run --project Tools/BalanceLab --
  solve` (or in Unity: regenerate, then `unity command run_tests --mode EditMode --filter BalanceLedgerTests --timeout
  300`) and read `Tools/PlayTest/balance_ledger.txt`. Every wave must stay inside its difficulty band.
- Fronts: the model counts a second road that meets the main road late (less than 35% shared trunk) as a separate
  front whose towers work at 70%. A nest or pool whose creatures join with little road left meets less defence. Gates
  on the trunk hold the queue and give the towers extra time. All of it comes from the map (`LevelShape`).

## Look

- Impact effects are data: one `ImpactVfxDefinition` per kind of blast in `Data/Vfx` (flash, shock ring, sparks,
  smoke, shards, light, camera shake) and the `VfxLibrary` that maps projectile styles, the mine, the death burst
  and the boss pulse to them; a path object may carry its own definition (`_impactVfx`).
- Quality profiles: `PcPlatformSettings` (High) and `SwitchPlatformSettings` (Low: softer bloom, no mist, no
  tilt-shift, half the particles), chosen by the `_lowQuality` flag on the Boot scene.
- One `VisualTheme` per level: palette, light, fog, bloom, ambience kind. Water, bog and lava cells in the terrain
  drive their own loops and particles; the theme's ambience kind adds wind or a lava bed.
- Terrain textures per level live in `Art/Terrain/Level_<Name>_albedo.png` and `_emission.png`.

## Checklist before a level ships

1. `LevelValidator` passes (roads reach the base, build spots valid, every referenced asset present).
2. Boss entrance follows the rule above.
3. Ledger inside its bands on all four difficulties.
4. Play the level once on Medium from the menu: towers survive a boss wave, the money after it allows a rebuild.

## Stone and teasers (S4)

- **Rifts** (`TerrainKind.Rift`, element `Stone`) are written by a spec's terrain share `TerrainShare("Stone", share)` and
  hold stone nests (`NestCount("Stone", n)`). A rift cannot hold a tower and is spawn terrain, like a liquid. Stone
  creatures are heavy and slow and carry no armour before level 11 of Crystal Plains (the armour story).
- **Teasers:** in the last third of the level, never on a boss wave, the same count on every difficulty, a species that is
  not one of the level's troops, with a `FirstSighting` dialogue (`TeaserTests`), killable with the level's own roster.
- **Air before the Flak:** no flyer on any difficulty before the level after the one that grants the Flak
  (`Continent1ContentTests`).
