# S3 – Campaign structure (spec)

Source: `Docs/STORY.md`, `Docs/STORY_ROADMAP.md` (S3, "Besloten"). Assumes F0 is merged, including the wave storage change to one `LevelWaves` asset per level, and S1 (dialogue assets, `StoryTrigger`). Only S3 is described here. Content polish of continent 1 belongs to S4 and S5.

## 1. Globe and regions

**`MapRegion`** gets new fields. They are appended, and the existing fields stay as they are.
- `Key` (stable id: `C1`…`C6`)
- `Layer` (`MapLayer { Surface, Core }`)
- `Tempo` (`RegionTempo { Introduction, Cruise, Heavy, Finale }`, from the script's arc)
- `Elements : Element[]` (which elements the continent may use)

The vein and seal map effects wait for continents 3 and 5. No field is added for them now.

| # | Key | Name | Lon, Lat | Radius | Tempo | Elements |
| --- | --- | --- | --- | --- | --- | --- |
| 0 | C1 | Crystal Plains | -40, 6 | 30 | Introduction | Stone, Water, Wood, Sand, Fire |
| 1 | C2 | Ashen Reach | 60, -15 | 30 | Cruise | Fire, Sand, Stone |
| 2 | C3 | Northern Crown | 60, 62 | 30 | Heavy | Ice, Stone, Fire |
| 3 | C4 | Mirewood | -140, 15 | 30 | Cruise | Wood, Water, Bog |
| 4 | C5 | Southern Crown | 150, -60 | 30 | Heavy | Ice + all |
| 5 | C6 | The Core | 150, -32 (fissure) | 12 | Finale | all (corrupted) |

- **Spacing.** The surface centres lie at least 77° apart, so the 30° caps never overlap. The Crowns reach the poles (lat 62+30 and -60-30).
- **Climate.** **`ClimateBands`** (pure) gives the elements a latitude allows:
  - Ice only where |lat| ≥ 50.
  - Fire and Sand only where |lat| ≤ 45.
  - Above |lat| 70, only Ice and Stone.
  - Water, Wood, Bog and Stone fit anywhere else.
  - A level may use only the elements in (its region's `Elements` ∩ the band at its node).
- **The Core** is no cap of land but a thin dark crack with a faint ember glow on Southern Crown's north rim, where the script's level 5-20 "ground tears open". `PlanetGlobe` draws it as a small emissive strip that breathes slowly (about 6 s, low amplitude, never pulses) and brightens once C5 is complete; until then focusing it shows "coming soon". Entering it (a cutaway to an inner glowing sphere with the Core's nodes) waits for the continent 6 plan.
- **Unlock rule** in `CampaignRules`: `IsRegionOpen(r) = r == 0 || last node of region r-1 completed (any difficulty)`. Within a region, the node-by-node rule stays.
- **Node placement.** **`NodePlacer.Route(centre, radius, count = 20, entryBearing)`** (pure, `Balance/Layout`) lays a serpentine of 4 rows × 5 nodes, 9° apart (1° over `MinimumSpacingDegrees`), all within radius − 4°. The generator writes the result into the catalog. C1 starts at the south-west coast, the Crowns end at their pole and C5 ends at the fissure. Spacing is compared only within one `Layer`.
- **Node count.** A region holds 0 nodes ("coming soon", already supported) or 20 nodes; a node without a level shows "coming soon". In S3, C1 holds 20 nodes (1–6 with levels) and C2–C6 hold none.

## 2. Level template generator

### Spec record

**`CampaignLevelSpec`** is pure C# with no engine types, like `WaveScripts`, so BalanceLab compiles it too. It is not named `LevelSpec`, because a private class with that name already exists in `ProjectGenerator.World.cs`. There is one file per continent: `Editor/LevelSpecs/LevelSpecs.Crystal.cs`, then `.Ashen`, `.Northern`, `.Mirewood`, `.Southern` and `.Core`. Each exposes `Specs : CampaignLevelSpec[]`.

| Field | Meaning |
| --- | --- |
| `Key` | `C1_01`, giving the asset `Level_C1_01` (ids are fixed once shipped) |
| `Name` | display name |
| `Briefing` | default LevelStart text (S1) |
| `Road` | `RoadTemplate` (`Serpentine`, `Hook`, `Spiral`, `Fork`, `LateJoin`, `TwinGate`) |
| `Seed` | int |
| `Width`, `Height`, `RoadWidth` | grid size and road width |
| `Gates` | number of blockades, 1–3 |
| `Plazas` | 0–2 |
| `Terrain` | `(Element, Share)[]`: liquid or patch share per element |
| `Nests` | `(Element, Count)[]` |
| `Waves` | `WaveScriptSpec` (troops with from-wave, rhythm, air, bosses with routes, budget curve) |
| `Teasers` | `(EnemyId, Wave, Count)[]`, compiled into a tiny fixed troop late in the level |
| `RewardTower` | tower id or null |
| `RewardGadgets` | path object ids |
| `RewardHeroPerk` | perk id or null |
| `Source` | `UnlockSource` |
| `Story` | `(Kind, Wave, EnemyId, DialogueId)[]`, resolved to the S1 `StoryTrigger` by the asset name in `Data/Story/Dialogues`. A missing id fails the step. |
| `Money`, `BaseHealth` | economy |
| `BossEntrance` | the default is `Gate` (LEVEL_GUIDE) |

- **What is derived.** Towers for sale are the continent roster: `PulseRifle`, `Cannon`, `Laser`, `Rocket`, `Tesla`, `Railgun`, plus `AcidSprayer` from S5. The bar already hides towers that are not unlocked. Gadgets for sale are every gadget unlocked up to this level. Recipes are none until C3.
- **Unlock timing.** A weapon or hero perk listed at level N in the script goes into the reward of level N-1, so Voss introduces it at the start of level N. Level 1's Gun and Cannon become the `UnlockTable` starting set.

| Reward of | Grants (debut level) |
| --- | --- |
| C1_01 | Laser (2) |
| C1_02 | Rocket, shown as "Flak" (3) |
| C1_04 | perk QuickReload (5) |
| C1_05 | Mine and SlowField, shown as "Land Mine" and "Chrono Field" (6) |
| C1_08 | Tesla (9) |
| C1_09 | perk WideSlash (10) |
| C1_12 | Railgun (13) |
| C1_14 | perk FieldRepair (15) |
| C1_15 | AcidSprayer (16, S5) |
| C1_17 | LavaBall (18) |
| C1_18 | GroundDrone, shown as "R.A.T." (19) |

The roadmap's S4 line says Quick Reload comes "after level 5". This spec follows the script's "Nieuw" column and the N-1 rule instead.

### Procedural roads

These live in `Balance/Layout` (pure, inside `Game.Balance`, so there is no new asmdef and BalanceLab runs them under `dotnet`).

- **`LayoutRandom`** is SplitMix64, seeded with FNV-1a of `Key` + `Seed`. `System.Random` is never used.
- **`RoadGenerator.Build(spec, attempt)`** builds the template's leg skeleton and lets the seed jitter leg lengths, connector positions, the entry edge and the base corner. It returns waypoints as `Cell[]`, the second road, gate cells (straight cells on the trunk, at about 30/65% of the road), plaza rectangles on trunk corners, north and south air routes, and the nest cells (within 60% of the road from the base, never next to the base).
- **`LayoutChecks`** checks:
  - parallel legs are at least 5 + (roadWidth − 1) cells apart, and a plaza edge is at least 3 cells away;
  - the road is 74 to 141 cells long;
  - the fronts allowed by the tempo (Introduction: at most 2);
  - a second road counts as a late front only by design (`LevelShape`).

### Unity side

The Unity side is a new partial, `ProjectGenerator.Templates.cs`. Its step is "Campaign levels" and it replaces "Landing level" and "World levels".
- For each attempt from 0 to 23, it fills an in-memory `LevelDefinition` (`ScriptableObject.CreateInstance`), runs the existing **`LevelValidator.Validate`** on it (including the defence-zone rule) and keeps the first attempt that passes. The attempt order is fixed, so the result is deterministic. If all 24 fail, the step fails and names the key and the errors.
- It reuses `WritePaths`, `MergeIntoMain`, `RoadCells`, `ScatterBlocked`, `PickMineCells`, `WriteTerrain`/`Erode`, `WriteNests`, `NearestLiquid`, `ConfigureThemePalette`, `ConfigureLevelExtras` and `BakeAllTerrain`.
- Terrain patches come from `TerrainNoise` blobs per element share, kept clear of the road.
- Themes: `Theme_C1_01`… use one continent palette, tinted by the level's dominant element.

BalanceLab gets a `maps` command that prints an ASCII map of every spec (it grows from `Tools/PlayTest/dump_map.cs`), so a map can be reviewed without Unity.

**S3 content:** specs for C1_01 to C1_06, minimal but playable, using existing assets:
- Rocket stands in for Flak and Roller is the level-5 teaser.
- Flyers appear late in level 3.
- Stone, Flak's rebuild and the final dialogues come in S4.
- C1_07 to C1_20 come in S5.

## 3. Retiring the 7 old levels and migrating saves

**Removal.** A generator step, "Retire old levels", runs `AssetDatabase.DeleteAsset` over a fixed list. It is idempotent. The list:
- `Level_{Landing,Forest,Lava,Ruins,Nest,Ice,Swamp}`
- their `Theme_*`
- their `LevelWaves`
- `Art/Terrain/Level_<Key>_*`

Code that goes:
- the private `LevelSpec`, `CreateWorldLevels` and the Landing `ConfigureLevel`;
- their `WaveScripts` entries;
- `BalanceSheet.CampaignOrder`, which becomes the order of the specs.

Code that stays:
- ice, bog, pools, nests and the boss entrances;
- the hybrids, Support and Cryo assets (ids are never deleted). They are only kept out of every level.

The menu stage (`CreateMenuStage`) switches to the C1 levels.

**Migration** is `ProgressMigrator` step 2, `RestartCampaign`.

| Kept | Cleared |
| --- | --- |
| `ResearchPoints` | `CompletedLevels` |
| `PowerCurrency` | `Stars` |
| all `Upgrades` | `UnlockedTowerIds` (reset to the starting set by `ProgressService`) |
| volumes | `SeenStoryIds` |
| `LoadoutIds` (filtered at runtime) | `IntroSeen` |

The step also adds two empty lists: `UnlockedHeroPerkIds` and `UnlockedPathObjectIds`.

Why reset instead of mapping: no 7-to-20 mapping makes sense, and mapped completions would open levels whose weapon and story introductions were never seen. The story restarts, so the intro and dialogues must play again. Keeping currencies and upgrades puts a returning player ahead, not behind, and only development saves exist.

Upgrades of towers that are out of the campaign (hybrids, Support, Cryo) stay saved. They are dormant until those towers return, and nothing is refunded.

## 4. Research and power for 120 levels

The 18%-a-level growth goes, and so does `ResearchGrowthPerLevel`. The rule now holds **per continent**:

- **Upgrade tags.** `UpgradeDefinition._continent` (int, default 0) tags which continent's research pays for an upgrade. `Cost_k` is the total cost of the research upgrades tagged k. C1 has 6 towers × 96 plus 172 for base, gate and hero, which is **748 RP** (844 once AcidSprayer arrives in S5). Later continents add upgrade tiers (new ids) with their own tag.
- **Research per level.** The base (Easy) research of level n in continent k is `EasyShare × Cost_k × w_n / Σw`, where `w_n = 1 + ResearchCurveRise × (n−1)/18` for n = 1..19 (rise 0.5), and `w_20 = ContinentFinaleWeight` = 2.0. That gives Σw = 25.75. The weights are fixed by index (`LevelsPerContinent` = 20), so a level's payout does not change when later levels are added.
  - C1 (748): L1 6, L6 7, L10 7, L19 9, L20 12 RP on Easy.
  - Multipliers are 1/2/3/5, so Psycho pays L1 29 and L20 58.
  - Easy everywhere in a continent pays about 20% of `Cost_k`, and Psycho everywhere pays 100%.
- **Power.** Each continent pays its tagged drone upgrade cost on Psycho, spread evenly: weight 1 per level plus 0.75 per Medium boss. C1 has 186 power, so the base is about 4 per level.
- **`Progression`** changes:
  - `CampaignLevel` gains `Continent`.
  - The Psycho end game now means **the levels up to the end of the level's own continent**, cleared on Easy, Medium and Hard, with that continent's weapons owned. Before, it meant `campaign.Count`, which would put a whole campaign of research into C1.
  - Easy, Medium and Hard keep walking every earlier level of every continent.
- **`BalanceRules`** gets `RegionPressure` per tempo: Introduction 1.0, Cruise 1.05, Heavy 1.2, Finale 1.3. It multiplies `TargetPressure`. Only C1 uses it now.
- **`CampaignModel.MetaTotals`** reports totals per continent. `BalanceSheet.Research` and `.Power` take (continent, index).
- **Docs.** `BALANCE.md` gets a rewrite of its numbers table and an "end game per continent" note.

## 5. Hero unlocks (data hook only)

- **`HeroPerkDefinition`** (ScriptableObject, `Game.Hero`; id = asset name) has `_displayName`, `_slot` (`HeroPerkSlot { Pistol, Blade, Ability }`) and `_description`. It has no effect yet. S4's `HeroAbilityDefinition` references a perk.
- **`LevelReward.HeroPerk`** is a new reference. It is granted on the first clear (`ProgressService.UnlockHeroPerk`) and shown as one line on the result screen.
- **Assets:** `Data/Hero/Perks/Perk_{QuickReload,WideSlash,FieldRepair}`.

## 6. Difficulty display name

`BalanceSheet.Difficulty` gains `DisplayName`. `CreateDifficulty` writes `_displayName = "Crazy"` for the `Psycho` asset. The asset name, the save keys (`levelId/Psycho`) and the code constants stay as they are. The difficulty table in GAME_DESIGN gets updated.

## 7. Tests

**EditMode, changed:**

| Test | Change |
| --- | --- |
| `EveryLevel_GivesANewWeapon…` | becomes `Rewards_FollowTheContinentScript`: every roster tower outside the starting set is granted exactly once, by the level before its debut; no reward repeats; waves exist on all difficulties |
| `AllLevels_ExistAndPassTheValidator` | per region, 0 or 20 nodes; every non-null level is valid |
| `FirstWorld_BossesComeThroughTheMainGate` | applies to the whole of C1 |
| `CombineRecipes_StartAtTheNest` | becomes `Continent1_HasNoRecipes` |
| `OnlyTheLandingRegionIsOpenAtStart…` | expects 20 nodes in region 0 |
| `LaterLevels_CountOnUpgrades` | checked within a continent |
| `MetaEconomy_FollowsTheDesign` | checked per continent (Easy ≈ 20% ± 3%, Psycho ≈ 100% ± 5%) |

**EditMode, new:** `LayoutTests` (every template × seeds 0–49 keeps spacing and length; same spec, same waypoints; nests within 60%), `NodePlacementTests` (20 nodes per surface region, spacing ≥ 8, Core excluded), `ClimateTests` (spec elements fit band and region), `TeaserTests` (only in the last third, at most `Count`), `RegionUnlockTests` (fake catalog: region r opens after r-1's 20th level), `ProgressMigrationTests` (v1 with old completions → v2: currencies and upgrades kept, progress cleared, new lists present), `HeroPerkTests` (continents 1–5 grant Pistol, Blade, Ability in order).

**PlayMode:** `LevelSimulationTests` becomes `ContinentSimulationTests` with `[ValueSource]` over the filled regions: `Smoke` (first, middle and last existing level) runs by default; `Full` (`[Category("Full")]`, `[Explicit]`, all 20) only on request. `BalanceProbeTests` gets the same split; Docs/COMMANDS.md gets the filter line.

## 8. Build steps

1. **Pure layer**, dotnet only. It can run in parallel in a worktree.
   - New: `Balance/Layout/{LayoutRandom, RoadTemplate, RoadGenerator, LayoutChecks, NodePlacer, ClimateBands}.cs`.
   - Continent-aware `Progression`, `CampaignModel` and `BalanceRules`.
   - `BalanceSheet` curve and display names.
   - `Editor/LevelSpecs/CampaignLevelSpec.cs` and `LevelSpecs.Crystal.cs` (C1_01–06).
   - BalanceLab `maps` and `solve` per continent.
2. **Runtime data hooks** (Unity compile; one agent):
   - `MapRegion`, `MapLayer`, `RegionTempo`, `CampaignRules`;
   - `LevelReward` (`NewPathObjects`, `HeroPerk`), `HeroPerkDefinition`, `UpgradeDefinition._continent`;
   - `ProgressData`, `ProgressMigrator`, `ProgressService` unlock methods;
   - `RewardCalculator`, `ResultView`, and the loadout gating in `LoadoutPanelView`.
3. **Generator** (Unity; one agent):
   - New: `ProjectGenerator.Templates.cs` and `ProjectGenerator.Campaign.cs` (`CreateCampaign` moves out of `ProjectGenerator.cs`, with regions, placement and the Core).
   - `.Steps.cs`, `.Context.cs` (`CampaignLevels` replaces `LandingLevel`/`WorldLevels`), `.Waves.cs` (payouts per continent), `.Difficulty.cs`, the upgrade tags in `CreateUpgrades`.
   - `.Story.cs` triggers from the specs.
4. **Retirement:** the "Retire old levels" step; deleting the old code paths in `.World.cs`, `ProjectGenerator.cs` and `WaveScripts.cs`; the menu stage; the `Tools/PlayTest` helpers and `ScreenshotTests` that name the old levels.
5. **Globe:** `PlanetGlobe` (Core crack), `WorldMapScreen` (20 markers per region, the Core panel), `ProjectGenerator.Planet.cs` (region paint).
6. **Tests and docs**, then the Unity run:
   - generate; EditMode all; PlayMode Smoke; ledger;
   - screenshots of the globe and of C1_01, C1_03 and C1_06;
   - update BALANCE.md, LEVEL_GUIDE.md, GAME_DESIGN (map and difficulties) and STATUS.

**Collision hot spots:**
- `ProjectGenerator.Waves.cs` and the BalanceLab `GameData` wave reader: the F0 agent is rewriting them now, so start step 3 only after that merge.
- `ProjectGenerator.cs`, `.Steps.cs`, `.Context.cs`.
- `ProgressData` and `ProgressMigrator`: S1 may append too, so steps must be appended in merge order.
- `LevelDefinition` (S1 adds `_story`), `LevelReward`, `WorldMapScreen`.

## 9. Open questions (defaults stated)

1. **Research per level falls** from 15 to about 6 at level 1 on Easy, because the rule is now per continent. *Default:* accept it. If early levels feel stingy, the knob is `ResearchCurveRise` or a starting grant of RP.
2. **S3 ships C1 levels 1–6 only**, and 7–20 stay "coming soon" until S5. *Default:* yes, so S4 remains the "play and decide" point.
3. **Dormant upgrades are not refunded** in the migration. *Default:* no refund, because only development saves exist.

---

## Critic review v1 and curator decisions (2026-11-03) — override everything above

1. **Leg spacing:** encode the owner's rule from STATUS "Wide roads": 5 rows between legs that turn clockwise, 3 for the others (plus road width), not a flat 6. Target roads stay 74–141 cells.
2. **Gates:** 3–5 gates per level, spread along the road. On forks, place one gate deterministically within a screen downstream of the junction. Add an explicit junction waypoint (`MergeIntoMain` matches with `IndexOf`).
3. **Nests:** the join point must lie within the first 60% of road progress measured from the spawn, matching `LevelValidator.cs:444-461`.
4. **Determinism:** the pure `LayoutChecks` in `Game.Balance` (dotnet-runnable) chooses the attempt. It covers road length, leg gap, plots per gate (≥8), gate screen coverage, dead-end corners, terrain share, map-fits-screen, nests and merges. Unity's `LevelValidator` only asserts. Waves are solved after the layout.
5. **S1 collision:** S1 is built first on `Level_Landing` as a proof. S3 step "retire old levels" moves the level-1 story triggers and S1 tests onto `C1_01`. The `Story` tuple in level specs carries the full S1 `StoryTrigger` (incl. `PathObject` and `Repeat`).
6. **Economy:** each continent gets an explicit RP budget `Budget_k` (data), not only the cost of upgrades tagged to it. The per-level reward has a floor (`MinResearchPerLevel`, data) so small continents never pay ~1 RP. "Psycho = end of this continent" stays a solver assumption.
7. **Dormant upgrades** use `_continent = -1` (default); only tagged upgrades count toward a budget.
8. Use integer-correct numbers in the spec and tests (Psycho L1 = 6×5 = 30).
9. **Migration:** reset `UnlockedTowerIds`, `UnlockedPathObjectIds`, `SeenStoryIds`, `IntroSeen` and completions; keep RP, power, bought upgrades and settings. A returning player may max C1 quickly: accepted. No gadgets until the script unlocks them (C1 level 6): intended.
10. **Globe:** the serpentine node layout is computed in a tangent frame around the region centre (poles inside the Crowns). `GlobePainter` skips `Layer == Core`; The Core shows as a subtle crack marker only, with no breathing animation for now. No new `IsRegionOpen`; `CampaignRules.IsOpen` already chains across regions.
11. **Deferred:** `ClimateBands` as a runtime system (latitude only guides the spec authoring for now), `Tempo`/`RegionPressure` and empty spec files for continents 2–6 (regions exist as data, specs come with their content).
12. **Generator runtime:** cache terrain bakes by a hash of the spec (skip when unchanged) and bake per continent.
13. **PlayMode:** a Smoke run is the default (each C1 level on Easy, accelerated); a Full run (all difficulties) runs on demand.
14. **Build order:** pure layer (`LayoutChecks`, road generator, economy curve) with dotnet + EditMode tests, then the generator after F0 part 2 is merged and after S1 is built.

---

## Builder notes (S3 build, 2026-11-03)

Choices made while building, at curator level, for Remco or the curator to overrule:

1. **Road templates.** Built: `Serpentine` (horizontal legs, entry from the top or the bottom), `Hook` (vertical columns,
   then a long run east to the base), `Fork` (serpentine plus a second road from the south-west joining the last leg early,
   with a plaza) and `LateJoin` (the same, joining close to the base). `Spiral` and `TwinGate` are left for S5 when a level
   needs them (no content asks for them yet). Every map enters at the west edge and ends at the base on the east edge.
2. **Leg spacing** is checked on the real corridors (second row to the right of travel, mitred corners): any two legs that
   do not share an end cell keep two free build rows; `RoadGenerator.UTurnGap` gives 5 for a clockwise U-turn and 3 for
   the other way at road width 2 (the owner's rule). Plazas keep the same two rows to legs they do not touch.
3. **Gates:** 3 to 5 per spec; one 5 to 7 cells before the base (the screen the flight routes cross), one 4 to 7 cells
   downstream of a junction unless the last gate already is within 10, the rest spread from 12% of the walk. Straight
   cells only (3 cells from a turn), 6 cells apart, never on or next to a plaza.
4. **Screen size** for the pure checks is a constant half window of 10 x 7.5 cells (the STATUS figure of about 21 x 16);
   the real camera footprint is still checked by `LevelValidator` on the written asset. `LevelValidator.ValidateLayout`
   (everything but the waves) was split out for the generator, because waves are solved after the layout.
5. **Nests** are placed 3 to 5 cells from the road, joining the main road between 6% and 50% of its walk (the validator
   allows 60%), 8 cells from the base; liquid nests get a pond. The second road is never a nest's nearest road.
6. **Fronts:** Fork and LateJoin both count as two ground fronts in `LevelShape` (the trunk after the junction is under
   35% of the main walk); the Introduction tempo allows two, so `LayoutSpec.MaxFronts = 2`. `RegionPressure` stays deferred.
7. **Crystal Plains 1 to 6** (names are proposals): Landing Site (serpentine, 22 waves), Glass Meadows (hook, 24), Shard
   Ridge (fork, 26, first flyers from wave 19, tide bats from 18), Twin Rivers (late join, 26), Quartz Flats (hook with five
   columns, 28, Roller teaser x3 on wave 22), Crysium Vein (fork, 30). No flyers at all in 1 and 2 (no air tower before
   the Flak); bosses all through the main gate. C1_04's air sprinkle starts on Hard, because on Medium it pushed wave 8 out
   of its band.
8. **Rewards:** C1_01 Laser, C1_02 Rocket (shown as "Flak"), C1_04 `Perk_QuickReload`, C1_05 Mine + SlowField (shown as
   "Land Mine" and "Chrono Field"); C1_03 and C1_06 grant nothing. Starting set: PulseRifle and Cannon. Both weapon sources
   are `HomePlanetIntel` until S4 decides the "developed by Voss" text. Gadgets are sold from the level after the one that
   grants them; the loadout panel only lists unlocked gadgets and is skipped while none is unlocked.
9. **Display names:** Psycho shows as "Crazy" (`DifficultyProfile._displayName`), Rocket as "Flak", Mine as "Land Mine",
   SlowField as "Chrono Field". The Pulse Rifle keeps its name ("Gun Turret" is for S4 with its rebuild).
10. **Continents** as in the table; the Core is a region with `Layer = Core`, radius 12, no sites, not painted by
    `GlobePainter` and drawn on the map as a small ember crack (three thin strokes over a faint glow, no animation).
    Sites without a level are named "<continent> <n>" and raise land but carry no lights. `ClimateBands`, `Tempo` and
    `MapRegion.Elements` are not added (deferred, no consumer).
11. **Upgrade tags:** a tower's upgrades belong to the first continent whose roster (`LevelSpecs.Rosters`) has the tower;
    base, gates, commander and drones belong to continent 0. Hybrids, Cryo and Support are dormant (-1) and hidden on the
    upgrade screen (`UpgradeDefinition.IsInCampaign`). Budgets: C1 research 748, power 186; C2 to C6 are 0 until their
    content exists.
12. **Power rounding:** about four points a level means whole numbers lose up to 6% of a continent's power budget on
    Psycho (176 of 186 for Crystal Plains); accepted, the test allows 6%.
13. **Bake cache:** `Library/TerrainBakeCache.txt` maps each level to an FNV hash of its map, its theme
    (`EditorJsonUtility`) and the painter sources (`Levels/Terrain*`, `Road*`, `Liquid*`, `Prop*`); an unchanged run skips
    the bake (about 20 s instead of 2.5 min).
14. **Teasers** are a new pure `TeaserSpec` on `WaveScriptSpec` (enemy, wave, count), composed as a fixed group outside the
    budget, through the gate or the north flight route.
15. **Removed as dead code:** the private `LevelSpec`, `CreateWorldLevels`, `ConfigureWorldLevel`, the Landing
    `ConfigureLevel`/`ConfigureTheme`, `Area`, `Terrain`, `Rect`, `Cells`, `Nest`, `NearestLiquid` and the Landing layout
    constants. Pool entrances and `NearestLiquid` return when a level needs one.
16. **PlayMode:** `LevelSimulationTests` became `ContinentSimulationTests` (smoke: every level of each filled continent on
    Easy, plus the old level-1 loss check; full: every difficulty, `[Explicit]`, category Full). `BalanceProbeTests`
    smoke = first and last level on Medium; full is `[Explicit]`.
