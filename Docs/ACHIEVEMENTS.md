# Steam achievements (proposal)

**Status:** proposal for Remco to decide. Nothing is built or wired yet. See `Docs/PUBLISH_TODO.md`, section Achievements.

**Rule:** once an id is live on Steamworks it must never change. Rename the display name instead. Ids below are UPPER_SNAKE and match the future Steamworks API names.

**Conventions**
- "Crazy" is the display name; the asset id is `Psycho` (`DifficultyProfile` asset name). Triggers compare the asset id.
- A win only counts when `LevelContext.Practice` is false and `PlaytestSettings.InfiniteMoney` is off. No achievements in the boss arena or test mode.
- Continent = `CampaignCatalog` node `Region` (0..5). Only Crystal Plains (region 0) is playable today.
- Hidden = the name and text stay hidden on Steam until unlocked. Used for story spoilers.
- Tracked today: **Yes** = an existing event/data gives the answer; **Partial** = the event exists but needs a filter or a small flag; **New** = a new stat or event is needed.

Total: **40 achievements**.

---

## 1. Campaign

| Id | Display name | Description | Hidden | Trigger | Tracked today? |
| --- | --- | --- | --- | --- | --- |
| CAMPAIGN_FIRST_LEVEL | Boots on the Ground | Win your first level. | no | First `LevelOutcome.Won` on any level/difficulty. | Yes: `LevelController.Finish` -> `ProgressService.MarkCompleted` (`ProgressData.CompletedLevels`). |
| CONTINENT_1_CLEARED | Plains Secured | Clear all 20 levels of Crystal Plains. | no | Every region-0 node in `ProgressService.IsLevelCompleted`. | Yes: `CompletedLevels` + `CampaignCatalog.NodeCount/FirstNode`. |
| CONTINENT_2_CLEARED | Through the Ash | Clear all 20 levels of Ashen Reach. | no | Same, region 1. | Yes (same check; content not built). |
| CONTINENT_3_CLEARED | Crown of Frost | Clear all 20 levels of Northern Crown. | no | Same, region 2. | Yes (same check). |
| CONTINENT_4_CLEARED | Out of the Mire | Clear all 20 levels of Mirewood. | no | Same, region 3. | Yes (same check). |
| CONTINENT_5_CLEARED | South Pole Standing | Clear all 20 levels of Southern Crown. | no | Same, region 4. | Yes (same check). |
| CONTINENT_6_CLEARED | Into the Deep | Clear all 20 levels of The Core. | yes | Same, region 5. | Yes (same check). |
| STORY_DIRECTIVE_7 | Delete Directive 7 | Refuse the order and send the truth home. | yes | Story id of The Core level 9 cutscene marked seen. | Yes: `SeenStoryIds`. |
| STORY_EPILOGUE | Status: Neighbours | See the epilogue. | yes | Epilogue cutscene `CutsceneSequencer.Finished` (or its story id seen). | Partial: `SeenStoryIds` works once the epilogue has a story id. |

## 2. Difficulty

| Id | Display name | Description | Hidden | Trigger | Tracked today? |
| --- | --- | --- | --- | --- | --- |
| DIFF_FIRST_HARD | Raising the Stakes | Win any level on Hard. | no | Won with difficulty `Hard`. | Yes: `CompletedLevels` key `levelId/Hard`. |
| DIFF_FIRST_CRAZY | Gone Crazy | Win any level on Crazy. | no | Won with difficulty `Psycho`. | Yes: `CompletedLevels` key `levelId/Psycho`. |
| DIFF_CONTINENT_1_HARD | Hardened Plains | Clear every Crystal Plains level on Hard or Crazy. | no | All region-0 nodes completed on `Hard` or `Psycho`. | Yes: `ProgressService.IsCompleted`. |
| DIFF_CONTINENT_1_CRAZY | Crystal Madness | Clear every Crystal Plains level on Crazy. | no | All region-0 nodes completed on `Psycho`. | Yes: `IsCompleted`. |
| DIFF_THREE_STARS_10 | Polished Defence | Earn 3 stars on 10 different levels (any difficulty). | no | Count distinct level ids with a `StarRecord.Stars == 3`. | Yes: `ProgressData.Stars` via `ProgressService.RecordStars`. |

## 3. Bosses

| Id | Display name | Description | Hidden | Trigger | Tracked today? |
| --- | --- | --- | --- | --- | --- |
| BOSS_FIRST | Bigger They Come | Defeat your first boss. | no | `LevelContext.EnemyKilled` with `EnemyDefinition.IsBoss`. | Yes: `EnemyKilled` + `IsBoss` (also `StoryTriggerKind.BossDefeated`). |
| BOSS_GRAVEMAW | Dug Too Deep | Defeat Gravemaw. | no | `EnemyKilled` where the definition is the Gravemaw asset. | Yes: `EnemyKilled` (compare `EnemyDefinition.name`). |
| BOSS_CINDER_MATRIARCH | Ashes to Ashes | Defeat the Cinder Matriarch. | no | Same, Cinder Matriarch asset. | Yes (same). |
| BOSS_FROST_WARDEN | Warden Down | Defeat the Frost Warden. | no | Same, Frost Warden asset. | Yes (same). |
| BOSS_ROTBLOOM | Weed Killer | Defeat Rotbloom. | no | Same, Rotbloom asset. | Yes (same). |
| BOSS_SHATTERED | What We Made | Defeat The Shattered. | yes | Same, The Shattered asset. | Yes (same). |
| BOSS_SEALED_ONE | The Seal Holds | Defeat The Sealed One. | yes | Same, The Sealed One asset. | Yes (same). |
| BOSS_FLAWLESS | Untouched Giant | Defeat a continent boss without base or gate damage during its wave. | no | Boss killed; no base/gate damage between `LevelContext.BossWaveStarted` and the kill. | New: per-run flag "structure damaged since boss wave start" (from `BaseStructure.TakeDamage` / `LevelContext.StructureHit`). |

## 4. Towers, upgrades and hybrids

| Id | Display name | Description | Hidden | Trigger | Tracked today? |
| --- | --- | --- | --- | --- | --- |
| TOWER_FIRST_MAX | Three Stars Shining | Upgrade a tower to its top level. | no | `Tower.LevelChanged` with `LevelIndex == Definition.MaxLevelIndex`. | Yes: `Tower.LevelChanged` (also `LevelContext.TowerUpgraded`, no payload). |
| TOWER_MAX_EVERY_TYPE | Full Arsenal | Bring every tower type to its top level at least once. | no | Set of `TowerDefinition.Id` maxed, over all runs, equals the unlocked roster. | New: lifetime set `MaxedTowerIds` in `ProgressData`. |
| TOWER_UPGRADE_PATH | Fork in the Road | Choose an upgrade path for a tower. | no | Player picks path A or B at level 3. | New: path choice is art only today (`TowerArtPath`); needs a gameplay path choice first. |
| TOWER_ROLES_ANSWERED | Right Tool, Right Job | In one level, kill an armoured, a flying and a swarm enemy with the tower made for it. | no | Per run: kills where the killer tower role matches the enemy trait (armour/air/swarm). | New: killer attribution on kill (`Enemy.DamagedBy` source is the `Tower`; store last hitter) + role tag per tower/enemy. |
| HYBRID_FIRST | Better Together | Merge two towers into a combo. | no | Combine succeeds in `PlacementController` (hybrid created). | Partial: success only raises `TowerUpgraded`; add `GameEvent<CombineRecipe> Combined` on `LevelContext`. |
| HYBRID_ALL_RECIPES | Mad Scientist | Make every combo at least once. | no | Lifetime set of recipe ids equals `CodexCatalog.Recipes`. | New: lifetime set `CombinedRecipeIds` in `ProgressData` (needs `Combined` event). |

## 5. Drones and gadgets

| Id | Display name | Description | Hidden | Trigger | Tracked today? |
| --- | --- | --- | --- | --- | --- |
| DRONE_FIRST | Eyes in the Sky | Launch your first drone. | no | `LevelContext.DroneLaunched`. | Yes. |
| GADGET_KILLS_100 | Watch Your Step | Kill 100 enemies with gadgets. | no | Kill whose last damage came from a `PathObject`. | New: kill attribution (gadgets deal source-less damage via `Enemy.TakeDamage`); lifetime counter `GadgetKills`. |

## 6. Spencer

| Id | Display name | Description | Hidden | Trigger | Tracked today? |
| --- | --- | --- | --- | --- | --- |
| HERO_KILLS_500 | Pathfinder's Tally | Spencer defeats 500 enemies. | no | Kill whose last hit came from Spencer (pistol or sword). | New: hero hits are source-less (`Enemy.TakeDamage`); pass the `Commander` as source, then lifetime counter `HeroKills`. |
| HERO_GATE_SAVE | Last Line | Spencer repairs a gate below 10% health and it survives the wave. | no | `HeroAbilityKind.GateRepair` used on a gate at < 10%; gate not wrecked before `WaveStarted` of the next wave. | Partial: `AbilityUsed` + `LevelContext.StructureWrecked`; needs the gate's health at use. |
| HERO_NEVER_DOWN | Still Standing | Win a level on Hard or Crazy without Spencer going down. | no | Won; no `Commander.Downed` during the run. | Partial: `Commander.Downed` exists; needs a per-run flag. |

## 7. Challenge feats

| Id | Display name | Description | Hidden | Trigger | Tracked today? |
| --- | --- | --- | --- | --- | --- |
| FEAT_NO_BASE_DAMAGE | Not a Scratch | Win a level on Medium or harder without base damage. | no | Won with `LevelContext.BaseHealth.Fraction == 1`. | Yes: `BaseHealth` (`Health`) at `Finish`; note `RewardCalculator.StarsFor` uses the same fraction. |
| FEAT_NO_REPAIRS | No Spare Parts | Win a level on Hard or Crazy without repairing anything. | no | Won; no `LevelContext.TowerRepaired`, no repair drone launched, no `GateRepair` ability. | Partial: events exist; needs a per-run flag. |
| FEAT_NO_TOWER_LOST | Nothing Left Behind | Win a level on Hard or Crazy without losing a tower. | no | Won; no `LevelContext.TowerWrecked` in the run. | Partial: `TowerWrecked` exists; needs a per-run flag. |
| FEAT_TWO_TYPES | Minimalist | Win a level on Hard or Crazy with at most two tower types. | no | Won; distinct `TowerDefinition` ids built this run <= 2 (base turret and gates excluded). | New: per-run set of built tower ids (`TowerBuilt` has no payload; use `Tower` creation or add a payload). |

## 8. Codex and meta

| Id | Display name | Description | Hidden | Trigger | Tracked today? |
| --- | --- | --- | --- | --- | --- |
| CODEX_ALL_ENEMIES | Field Biologist | Meet every creature in the Codex. | no | Seen set covers `CodexCatalog.Enemies`. | Partial: `ProgressService.MarkEnemySeen` stores bosses only (`BossDirector`); extend to every species on first spawn (`LevelContext.EnemySpawned`). |
| META_FIRST_UPGRADE | Lab Work | Buy your first permanent upgrade. | no | `ProgressService.TryBuyUpgrade` returns true. | Yes: `TryBuyUpgrade` / `ProgressData.Upgrades` (+ `ProgressService.Changed`). |
| META_UPGRADE_MAXED | Fully Funded | Max out any permanent upgrade. | no | `GetUpgradeLevel(id) == UpgradeDefinition.MaxLevel`. | Yes: `ProgressData.Upgrades`. |

Recipe completion is covered by HYBRID_ALL_RECIPES; making a combo is what fills its Codex entry.

---

## IAchievementService today

`Game.Platform.IAchievementService` has one method: `void Unlock(string achievementId)`. `NullAchievementService` only logs it, and `Bootstrapper` registers it in `Services`. Nobody calls `Unlock` yet. There is no progress or stat API.

**Proposal (short).**
- `AchievementDefinition` (ScriptableObject, `Data/Achievements/`): `Id` (the Steam id), a condition kind (enum: level won with filters, boss killed, counter reached, set complete, story seen) and its parameters (difficulty, region, enemy, threshold). `OnValidate` flags duplicate or empty ids.
- `AchievementTracker` lives in the UI/meta layer. It subscribes to `LevelContext` events, `Tower`/`Commander` events and `ProgressService.Changed`. It keeps per-run flags and writes lifetime counters to `ProgressData`. When a definition is met it calls `Services.Get<IAchievementService>().Unlock(id)`.
- Gameplay never sees achievements or Steam. A later `SteamAchievementService` maps `Unlock` (and an optional `SetProgress(id, current, max)` for counters) to Steamworks.
- On boot the tracker re-checks all definitions against `ProgressData`, so a save from before achievements unlocks what it already earned.

## New stats needed

**Lifetime (in `ProgressData`, saved, migrated by `ProgressMigrator`):**
- `HeroKills` (int): HERO_KILLS_500.
- `GadgetKills` (int): GADGET_KILLS_100.
- `MaxedTowerIds` (list): TOWER_MAX_EVERY_TYPE.
- `CombinedRecipeIds` (list): HYBRID_ALL_RECIPES.
- `SeenEnemyIds` widened from bosses to every species: CODEX_ALL_ENEMIES.
- `UnlockedAchievementIds` (list): a local record, so the tracker does not call `Unlock` twice and works offline.

**Per run (in the tracker, reset at level start, never saved):**
- `StructureDamagedSinceBossWave`: BOSS_FLAWLESS.
- `AnyRepair`: FEAT_NO_REPAIRS.
- `AnyTowerWrecked`: FEAT_NO_TOWER_LOST.
- `BuiltTowerIds` (set): FEAT_TWO_TYPES.
- `HeroDowned`: HERO_NEVER_DOWN.
- `RoleKills` (armour/air/swarm flags): TOWER_ROLES_ANSWERED.

**New events or plumbing (gameplay side, no achievement knowledge):**
- Kill attribution: remember the last damage source on `Enemy` and expose it on `Killed`. Hero and gadget damage must pass a source (today they call source-less `Enemy.TakeDamage`).
- `LevelContext.Combined` (`GameEvent<CombineRecipe>`) when a merge succeeds.
- A payload on `TowerBuilt` (the `Tower`), or a `TowerPlaced` event.
- Gate health at `GateRepair` use, on `Commander.AbilityUsed` or a sibling event.
- Upgrade path choice at level 3 (design item, not built): TOWER_UPGRADE_PATH waits for it.

**Count:** 40 achievements. 15 need work: 8 New (new stat or event), 7 Partial (event exists, needs a per-run flag or filter). 25 work from existing data and events.

## Questions for Remco

- Drop or keep the story-spoiler achievements (STORY_*) as hidden?
- HYBRID_ALL_RECIPES: combos only arrive in Northern Crown. Fine as a late goal?
