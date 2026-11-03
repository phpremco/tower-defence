# Balance model

How the numbers of the game are made, and how to change them. Written 2026-11-02 with the balance overhaul. The aim:
balance by construction, so a new level (hand-made or generated) gets fair waves from rules instead of hand tuning.

## In one picture

```
BalanceSheet (Editor)            WaveScripts (Editor)            the level's map (LevelDefinition)
 difficulty profiles              roster, rhythm, air raids,      roads, gates, nests, boss entrance
 research/power per level         bosses per difficulty,               |
 tower prices                     crowd budget curve                   v
        \                               |                        LevelShape (fronts, trunk, gates, joins)
         \                              v                              |
          +-----> Progression -----> WaveSolver <---- Ledger <---------+
                  (what the player     (per wave: how tough the
                   owns and bought)     creatures are, on Medium)
                                         |
                                         v
                              WaveComposer -> waves of every difficulty -> checks (BalanceChecks)
```

All of this lives in `Assets/_Project/Scripts/Balance` (`Game.Balance`, no engine references), so it runs in the
generator, in the EditMode tests and in the BalanceLab console tool with the same results.

## The pieces

- **Threat** (`Threat.cs`): one threat point is 20 effective health (health + armour; armour soaks exactly its own
  points, whatever its share per hit). Flyers count 1.2x, damage to the base and structures adds a little.
  **Kill money follows threat**: `reward = 1.3 * threat^0.9`, bosses x3. No creature has a hand-set reward any more.
- **Ledger** (`Ledger.cs`, `WaveLoad.cs`): per wave, the money the player can have had (start, earlier kills less what
  leaked, the mines less one harvester), the best defence that buys from the towers the player **owns** (built and
  upgraded tower by tower; an upgrade needs the tower below it), and the pressure = wave health / what that defence
  can deal with in its window. Window = spawn span + each tower's own time in range (2 x range / speed) + a share of
  the gates' hold time. Splash counts by stream density, overkill wastes big hits on small creatures, a boss is a
  single target, a second front or a late-joining nest only meets part of the defence. Pressure 1 = the towers alone
  just hold (calibrated to the PlayMode greedy bot, `BalanceRules.Utilisation`).
- **LevelShape** (`LevelShape.cs`): built from the map. Road length, the *trunk* (the stretch every lane shares to the
  base), the number of ground fronts (roads that meet late count as separate fronts), gates on the trunk or a lane,
  and how much road is left where nest and pool creatures or the boss entrance join.
- **Progression** (`Progression.cs`): towers owned at a level = starting set + rewards of the levels before. Research
  is spent cheapest-first on upgrades of owned guns, hero and gates. The path per difficulty: Easy player = earlier
  levels on Easy; Medium = on Medium; Hard = on Medium and Hard; **Psycho = end game**: the whole campaign on Easy,
  Medium and Hard, every weapon owned (GAME_DESIGN: Psycho is for a fully upgraded commander).
- **WaveSolver** (`WaveSolver.cs`): the script fixes the *crowd* (budget curve `Budget -> FinalBudget`, which sets kill
  money); the solver only chooses each wave's **health multiplier** so the Medium pressure meets the target curve.
  Tougher creatures pay no more, so the purse cannot snowball into ever bigger waves. Waves added or changed by
  another difficulty (extra air raids, extra bosses, the air sprinkle, ambush splits) are solved on that difficulty
  against `Medium target x DifficultyPressure`. Everything else keeps the Medium multiplier with the profile on top.
- **WaveComposer** (`WaveComposer.cs`): the old generator composition (rush, heavy, air, ambush, packs, clusters over
  lanes), now pure and seeded with a stable FNV-1a hash of the level key.
- **BalanceChecks** (`BalanceChecks.cs`): what the solver does not optimise. Every wave inside its band (difficulty
  share x the highest Medium target x 1.4; bosses x1.6), no wave more than 2.6x the few before it, late Easy not empty.

## The target curve (Medium)

`TargetPressure` 0.6, rising from 0.85x early to 1.2x at the end of a level; the first four waves ramp up from 45%;
rush x1.1, heavy x1.15, air x0.9, boss x1.4, the wave after a boss x0.8. Easy lands near 0.7x Medium, Hard 1.5x,
Psycho 2.1x (`DifficultyPressure`).

## The numbers chosen (and why)

| What | Value | Why |
| --- | --- | --- |
| Research per difficulty | 1 / 2 / 3 / 5 | Easy everywhere pays 20% of all research upgrades, Psycho everywhere 100% (GAME_DESIGN, Economy) |
| Research per level | per continent: `EasyShare x Budget_k x w_n / 25.75`, `w_n = 1 + 0.5 (n-1)/18` for n = 1..19, `w_20 = 2`, floor 4 (`MinResearchPerLevel`) | Crystal Plains (`Budget` 748 = 6 towers x 96 + 172 base, gates, hero): L1 6, L6 7, L20 12 on Easy; Easy over all 20 = 150 (20%), Psycho = 750 (100%) |
| Power per difficulty | 0 / 1 / 1.5 / 2.2 | none on Easy; Psycho over a whole continent pays its power budget (Crystal Plains 186) |
| Power per level | `Budget / 2.2 / (20 x (1 + 0.75 x 2)) x (1 + 0.75 x Medium bosses)` | about 4 a level in Crystal Plains; whole points lose up to 6% of the budget |
| Difficulty health / count / tempo | Easy 0.68/0.8/1.2, Medium 1/1/1, Hard 1.15/1.2/0.85, Psycho 1.65/1.5/0.7 | tuned so each lands on its pressure share |
| Kill money | 1.3 x threat^0.9, bosses x3 | about a quarter less than before; the economy's main lever (below) |
| Laser / Tesla / Railgun prices | 130-90-170 / 140-115-190 / 220-150-255 | every upgrade buys at least as much damage x range per coin as the level before |
| Extra boss waves | Hard +1, Psycho +2 | GAME_DESIGN: harder difficulties add boss waves; existing waves become boss waves (no new assets) |

Boss routes: first-world bosses always come through the main gate on every difficulty (LEVEL_GUIDE). The Frozen Shelf
and the Black Marsh keep their own entrance; that is now data per boss and per difficulty (`BossSpec.Routes`, written
as `SpawnSource.Entrance` on the boss group), so a level can send a boss through the gate on Easy and up through a pool
on Psycho without code.

## What the model found (before the overhaul)

- The old ledger bought from `AvailableTowers` (all eight towers on most levels), but the bar only shows what is
  unlocked: Landing has just the Pulse Rifle, and until the Nest the only real air gun is the Pulse Rifle. It also
  bought upgrade steps without the tower beneath them.
- With an honest player, Landing was the hardest level and every later level got easier (Swamp Easy around 0.12).
  The meta economy paid Easy 30% and all difficulties 230% of the upgrades.
- **The snowball**: towers stay, so every coin of kill money keeps paying for the rest of the level. Sizing waves by
  *numbers* lets money and crowd chase each other (budgets x500 in one level). Hence the solver sets toughness, and kill
  money per threat is the lever that decides how much tougher late creatures must be: at 1.3 the median late wave
  needs creatures about 3 to 5 times their base health.

## How to retune

1. Change a number in `BalanceRules` (model and targets), `BalanceSheet` (profiles, payouts, prices) or a
   `WaveScripts` entry (roster, rhythm, crowd, bosses).
2. Check it here: `dotnet run --project Tools/BalanceLab -- solve` (prints the ledger and the checks; needs the .NET 8
   SDK). `-- write` writes the solved data into the existing assets; `-- report` judges the assets as they are;
   `-- dump <Level> <difficulty 0-3> <wave>` shows one wave's groups.
3. Or in Unity: `Tools/Generate/Scenes and Starter Data`, then the EditMode tests (`BalanceLedgerTests`,
   `BalanceModelTests`). The generator and the tool run the same code and write the same numbers.
4. After play, the first knobs: *everything too easy/hard* -> `TargetPressure`; *one difficulty off* -> its profile in
   `BalanceSheet` (then check `DifficultyPressure`); *creatures too spongy late* -> lower `MoneyPerThreat` or raise a
   script's `FinalBudget`; *calibration* -> run the PlayMode greedy bot and move `Utilisation` so pressure 1 is where
   it stops holding (the bot still builds from all available towers, not only the unlocked ones).

## For a level generator

A generated level needs: a map (roads, gates, nests, boss entrance -> `LevelShape`), a `WaveScriptSpec` (roster,
rhythm, air waves, bosses with routes per difficulty, crowd curve) and a place in the campaign. Then
`CampaignModel.BuildWaves` gives balanced waves for all four difficulties, and `BalanceChecks` says whether they hold.
Wave counts must match the level's existing `Data/Waves/Waves_<Level>.asset` when writing outside Unity (the tool rewrites
the groups and health of each stored wave in place); inside Unity the generator writes the whole file.

## Known limits

The model has no placement, drones, path objects, Cryo slow, Support boost, boss powers (minions, splits, regrowth)
or tower damage. Base turrets and base health upgrades are bought but not counted. Air raids against a roster with
only the Pulse Rifle (Forest to Ruins) are sized by the solver, so they are fair in the model, but the unlock order
(Rocket only after the Ruins) is a design choice worth revisiting.

## End game per continent (S3, 2026-11-03)

The campaign is six continents of twenty levels. Research and power are budgeted **per continent** (`BalanceSheet.Continents`,
`ContinentEconomy`): each upgrade carries `UpgradeDefinition._continent` (the continent whose budget pays it; -1 for
dormant upgrades of towers out of the campaign: hybrids, Cryo, Support), and each continent pays its own budget over its
twenty levels, by a level's index (so adding later levels never changes an earlier payout). `Progression` reads
**Psycho as the end game of the level's own continent**: every level up to the end of that continent on Easy, Medium and
Hard, with that continent's weapons owned. Counting the whole campaign would have put six continents of research into the
first. `CampaignModel.MetaTotals(continent, ...)` reports per continent. Tempo/`RegionPressure` per continent is deferred.

## Crystal Plains 1 to 6 (S4, 2026-11-03)

- **Hero gun.** `HeroStats` carries the blaster's magazine and reload; the ledger uses
  `EffectiveGunInterval = (magazine x interval + reload) / magazine` with the owned perks applied. The blaster (8 shots,
  0.2 s, 1.2 s reload) keeps the old 0.35 s a shot; Quick Reload (reload x0.5) counts from the level after C1_04, which
  grants it (`CampaignLevel.RewardHeroPerkId`, `PlayerState.HeroPerks`).
- **Rollers** enter the model at their average speed over a walk-and-roll cycle (`EnemyDefinition.AverageSpeed`), so a
  fast roller spends the shorter time in range it really does. (Correction: as built in S4 the Unity bridge
  `BalanceLedger.Stats` still passed `EnemyDefinition.Speed`, 1.4 instead of 4.13; fixed after S4, only BalanceLab read it.)
- **Flak** is an air-only splash gun to the ledger; the proximity fuse only makes sure the shell hits. Air-only weapons
  never count against a Wisp (ground and air layers): it walks the road, and the game's `TargetLayers.CanHit` rule agrees.
- **Packed raids.** `WaveScriptSpec.FlyerInterval` sets the spacing in an air raid (default 1.6 s; C1_03 and C1_04 use
  0.45 s). The ledger's splash density follows the group interval, so packed raids are where the Flak earns its keep.
- **Result (Unity ledger, `BalanceLedgerTests`):** every wave of C1_01 to C1_06 inside its band on all four difficulties.
  Highest pressure per level on Easy 0.57 to 0.70, Medium 0.92 to 1.03, Hard 1.34 to 1.51, Psycho 1.87 to 2.12.

## Balance after S4 (2026-11-03)

The ledger said every C1_01-06 wave was inside its band, yet the PlayMode bot lost Shard Ridge and Crysium Vein on
Medium at wave 6. A per-wave trace (`Logs/probe_trace.txt`, written by `BalanceProbeTests` through `ProbeTrace`: money
earned, towers, raw damage per second, creatures alive and where, base hits by species, beside the ledger's line)
showed five causes, fixed where they live:

1. **Kill money came too early in the model.** The ledger paid a wave's kills before the next wave. In the game a kill
   pays where the defence holds the creature, after its walk: Shard Ridge Medium had 269 money by wave 6 in the game
   and 354 in the ledger (+32%); slow Cragbacks pay six or seven waves late. `KillPurse` now books every creature's
   reward for the moment it reaches the defence (the first gate every road passes, `KillPurse.DefenceRemaining`),
   on the game's clock (a wave starts its delay after the previous one finished spawning). Ledger and bot money
   now agree within a few percent until the bot starts to leak.
2. **Air guns were re-spent on the ground.** The ledger re-chose the whole defence every wave, so the Flak bought for
   a raid turned into ground guns the wave after. `Ledger.AirHoldings` keeps air-only guns standing and their price out
   of the purse for the rest of the level (the solver carries them too).
3. **Slow and fast waves stack.** `KillPurse.Carried`: ground health of earlier waves that reaches the defence while a
   wave does counts against it at `CarriedShare` (0.5, the other half stays with its own wave). Bosses stay single
   targets. Raging packs (`EnemyStats.FightSpeed`: Prowler 1.5 x 1.6) cross the towers' reach at their rage speed, and
   rollers at their average speed (the Unity bridge passed `Speed`, 1.4 instead of 4.13, until now).
4. **The model trusted the defence too much.** `Utilisation` 0.35 -> 0.27 (bot holds Crysium Vein at 0.27, loses it at
   wave 10 at 0.3 and 0.35) and `HeroShare` 0.5 -> 0.35 (at the busiest gate the hero falls and is out for two
   minutes). The solver may drop a wave below its trend when the trend floor would break the band (`WaveSolver.Solve`).
5. **The simulation itself.** At twenty times speed a slow editor frame was 1 to 3 game seconds: guns fired once a
   frame and flak shells burst a whole step short of their target. Game fixes: `Cadence` keeps every repeated action
   (tower shots, hero strikes, creature bites) on its rhythm whatever the frame length; `ProximityFuse.BurstPoint`
   bursts the shell at the fuse distance from the flyer. The probe also caps a frame at 0.2 game seconds.

The bot (`BalanceProbeTests`) now plays like an average player: towers around the first gate every road passes, one in
four at the last gate before the base as a second line, the hero on the front post, every coin spent on the affordable
step with the most damage per coin, the price of a Flak kept in the purse while flyers are still to come, and the Flak
placed under the flight routes. The smoke test asserts that it wins.

Content: `C1_04` (Fault Line) `FinalBudget` 165 -> 100: with honest money its crowd was so big that the late creatures
ended weaker than the first ones.

Checks: `WorldContentTests.EveryLevel_GrowsEnemyHealthWaveByWave` compares the toughest plain wave of the last third
with the first wave (a boss wave or a wave softened for stacked slow creatures says little on its own).

Result: every C1 wave inside its band on all difficulties (Unity and BalanceLab). Highest pressure per level: Easy 0.57
to 0.87, Medium 1.01 (the boss waves), Hard 1.35 to 1.67, Psycho 1.89 to 2.56. Bot on Medium: Shard Ridge and Crysium
Vein won with the base untouched. Open: on Easy a gun-only bot also clears Shard Ridge's packed raids (11/11 flyers
down), so curator decision 8 ("must struggle") needs bigger raids, a design call.
