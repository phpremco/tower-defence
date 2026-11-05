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

## Easy economy and the human lens (2026-11-04)

Remco on C1_01 Easy: "je krijgt gewoon te veel geld": he could place two or three times the firepower the waves needed,
and the commander never had to act. Easy being hard to lose is fine; the surplus was not.

- **Human lens** (`BalanceRules.HumanUtilisation` 0.34, `HumanTowersOnly()`): the bot calibration (`Utilisation` 0.27,
  pressure 1 = the bot just holds) stays for the solver and the bands; the lens judges a competent player instead, who
  places guns in the bends and at the gates where one gun covers two stretches of road (about a quarter more useful fire
  than the bot), with the towers alone (`HeroShare` 0). At 0.34 a fresh player on C1_01 Easy had 2.4x the firepower the
  plain waves need, and 3.6x with the permanent Pulse/Cannon upgrades in Remco's save (damage and rate x1.3 each, range
  x1.15): his "two or three times" sits in between. Medium was 1.5x, Hard 0.9x, Psycho 0.8x.
- **Easy money, not creatures:** `DifficultyProfile` has two new shares, `IncomeMultiplier` (bounties and harvested ore,
  through `Wallet.Earn`, which keeps the fractions so a one-coin ant still pays 0.85 on average) and
  `StartingMoneyMultiplier`. Easy: 0.85 and 0.8 (C1_01 starts with 160 instead of 200); the others 1. The ledger reads them
  (`LevelInput.Income`, scaled `StartingMoney`), but the solver sizes creatures against the full purse
  (`CampaignModel.ReferenceInputFor`), otherwise it would weaken Easy's re-solved waves to make up for the thinner purse.
  Every stored wave is unchanged.
- **Result (human lens, towers alone, plain waves after the opening):** Easy 1.76x to 2.00x (was 2.25x to 2.57x), Medium
  1.45x to 1.73x, Hard 0.83x to 1.24x, Psycho 0.65x to 0.96x. Easy asks 0.78 to 0.85 of Medium (C1_05 0.91; it was 0.61 to
  0.74). C1_01 Easy boss wave: 1.6x -> 1.3x for the towers alone. Bot bands unchanged and all inside.
- **Checks** (`BalanceChecks.CheckHuman`, run by `BalanceLedgerTests` and BalanceLab): Easy at most `MaxEasySurplus` 2.1x,
  Easy at most `MaxEasyShareOfMedium` 0.93 of Medium, each difficulty asks more than the one below. The old Easy money fails
  the first on every C1 level.
- **Why not 1.2x to 1.4x on Easy:** Medium cannot get harder for the bot (it barely holds the Medium boss waves, and the
  smoke run asserts a win), so Easy can only come down to about 0.8 of Medium before the two feel alike: 1.8x to 2x. A
  tighter Easy needs Medium raised first (`TargetPressure`, then the bot recalibrated), a separate decision.

## Heavier curve and the campaign ramp (2026-11-04, later)

Remco: "alles wat zwaarder maken", then: "misschien kunnen we wel rekening houden dat het wel te halen blijft. Hoe verder je in
het spel komt hoe lastiger, dit moet wel in balans blijven. Maar te makkelijk is saai."

- **The waves are calibrated to a human target curve** (`BalanceRules.HumanSurplus`, `CampaignModel.CalibrateToHuman`). The
  solver still shapes every wave (opening ramp, rhythm, bosses, breathers) against the bot calibration; then each
  difficulty's health multipliers are trimmed by one factor so a competent player's towers alone (the human lens, with that
  difficulty's real money) have exactly the target surplus on the plain waves. Targets at a continent's first level: Easy
  1.4x, Medium 1.15x, Hard 0.95x, Psycho 0.8x.
- **The ramp:** the target pressure rises 1.5% per level of a continent (`HumanRampPerLevel`), up to +20% (`HumanRampMax`,
  reached at level 14), net of the upgrades the progression model expects there. Crystal Plains 1 to 6, towers alone:
  Easy 1.40 1.38 1.36 1.34 1.32 1.30; Medium 1.15 1.13 1.12 1.10 1.09 1.07; Hard 0.95 0.94 0.92 0.91 0.90 0.90; Psycho 0.80 0.79
  0.78 0.77 0.75 0.74. Before: Easy 1.85 1.77 1.76 2.00 1.89 1.93, Medium 1.49 1.45 1.46 1.56 1.73 1.65 (C1_04 and C1_05 were
  the easiest levels), Hard 0.93 0.86 0.83 1.22 1.13 1.24, Psycho 0.81 0.74 0.65 0.96 0.89 0.80.
- **Beatable, bosses killable:** after the trim no wave may ask more of that player *with the hero* (`HumanWithHero`) than
  `HeroCap` (Easy 1.0, Medium 1.15, Hard 1.25, Psycho 1.35). A boss wave or spike above it is softened on its own and the other
  waves are trimmed again (three passes). The ledger's pressure is not monotone in one wave's health near the bottom (the
  defence is re-chosen and slow creatures of earlier waves arrive with it), so the cap steps the scale down and then bisects.
  Plain surplus with the hero: Easy 1.50-1.81x, Medium 1.18-1.34x, Hard 1.01-1.09x, Psycho 0.85-0.91x.
- **What the player is assumed to own** (the progression model, research spent cheapest first): Easy 0, 4, 8, 18, 23, 28
  research by C1_01..06 (cleared on Easy), Medium 0, 8, 23, 33, 48, 58, Hard 0, 28, 58, 90, 118, 148 (Medium and Hard clears),
  Psycho 222 at every C1 level: the whole continent so far on Easy, Medium and Hard (towers +38% damage x rate on average,
  hero damage 1.3, gates 1.4). Psycho below one with the hero is deliberate: the end-game player also brings what the model
  leaves out (drones, path objects, base turrets, hero abilities).
- **Difficulty profiles** now set the feel at a given hardness, not the hardness (money and toughness trade against each
  other under the calibration; a bigger crowd pays for itself): Easy income 0.8, start 0.8, crowd 0.9 (was 0.85/0.8/0.8);
  Medium income 0.95; Hard crowd 1.3 (was 1.2) and income 1.1; Psycho crowd 1.65 (was 1.5) and income 1.1. Without the extra
  Hard income its creatures on C1_01-03 came out weaker than Medium's at the same pressure.
- **Solver and bands:** `TargetPressure` 0.6 -> 0.8 (the Medium trim is then 0.97-1.15), `DifficultyPressure`
  {0.7, 1, 1.5, 2.1} -> {0.8, 1, 1.35, 1.7}; the bands follow both. All C1 waves inside them.
- **Checks** (`BalanceLedgerTests`, BalanceLab): `CheckHumanTarget` (each level within 6% of its curve), `CheckRamp` (within a
  continent no level is more than 3% easier than the one before, per difficulty), `CheckBeatable` (with the hero: plain
  surplus at least 1.15 / 1.05 / 0.95 / 0.82, no wave over its cap + 4%), `CheckHuman` with `MaxEasySurplus` 2.1 -> 1.6.
  `balance_ledger.txt` ends with the per-level table and the research assumed.
- **What changed in the data:** only health multipliers (crowds, rewards and timing are the scripts'). Ground health over a
  level, C1_01: Easy 8.9k -> 10.9k, Medium 14.5k -> 19.3k, Hard 20.9k -> 24.7k, Psycho 36.6k -> 42.5k; C1_06: Easy 17.9k ->
  26.6k, Medium 26.0k -> 39.8k, Hard 45.9k -> 67.0k, Psycho 61.7k -> 73.6k.
- **Tools:** `Tools/PlayTest/Scripts/BalanceCurve.cs` (in-memory curve table, optional overrides in `curve_arg.txt`) and
  `Tools/PlayTest/Scripts/ApplyWaves.cs` (generator steps Difficulties and Waves only), both through
  `unity command run_script ... --timeout_ms 900000` (eval_file stops after 5 s; delayCall never runs while the editor is
  unfocused).
- **The probe bot now represents the human lens** (`BalanceProbeTests`): placement by road coverage (bends and the gate
  queue), a second line at the next trunk gate, self-judgement in the human lens with the hero, extra Flak when a raid
  outgrows the sky guns, new guns and upgrades both weighed by gain per coin times the quality of their spot, repairs, the
  hero's abilities and the hero falling back with the queue. Last full run on Crystal Plains: Easy 6/6, Medium 6/6, Hard 4/6,
  Psycho 1/6 (no permanent upgrades). `EasyAndMedium_Bot_ClearsEveryLevel` asserts Easy everywhere and Medium everywhere but
  Fault Line: its two ground fronts (a second road joins at the last gate) are beyond the bot, which defends one gate and won
  Fault Line Medium in about half its runs. `Utilisation` 0.27 is kept for the solver's shape; the bot holding
  Medium bosses that the human lens with the hero rates at about 1.1 supports `HumanUtilisation` 0.34.
- **How in-level and permanent upgrades are counted:** the ledger buys step by step, a new tower or one level up on a tower it
  already bought, by gain per coin; an upgrade step costs its own price and adds only the difference over the level below,
  and air guns kept between waves are charged every step up to their level. Permanent research upgrades (`Progression`)
  scale the towers' damage, rate and range, the hero and the gates before any of that.

## Laser and gun (2026-11-05)

Remco: "laser turret heeft een hoge fire rate, maar lage damage, maar hij is minder effectief dan de gun turret met de
mieren, dat zou niet moeten, hij is duurder."

**Measured** with a duel simulation that runs the game's own rules frame by frame (`Tools/PlayTest/Scripts/LaserDuel.cs`:
`Tower.Update` with `Cadence`, the furthest-along target, homing shots wasted on a creature that died before they land, the
beam's lock): one tower beside a straight road against a stream of one species. *Holding power* = the toughest creature it
stops with at most 10% leaking, as health per second per 100 coins of the tower's total price.

| Stream | Gun L1 / L2 / L3 | Laser before | Laser after |
| --- | --- | --- | --- |
| Skitters every 0.2 s (dense swarm) | 5 / 2 / 38 | 2 / 1 / 1 | 62 / 55 / 46 |
| Skitters every 0.45 s | 22 / 18 / 33 | 1 / 12 / 10 | 41 / 36 / 41 |
| Wisps | 23 / 25 / 29 | 24 / 21 / 21 | 33 / 34 / 32 |
| Prowlers (raging) | 17 / 27 / 25 | 10 / 9 / 15 | 31 / 36 / 31 |
| Rollers | 20 / 24 / 30 | 18 / 16 / 18 | 31 / 27 / 32 |
| Cragbacks | 27 / 28 / 32 | 28 / 27 / 28 | 33 / 34 / 34 |
| A boss (useful DPS per 100 coins) | 29 / 28 / 30 | 30 / 30 / 30 | 30 / 30 / 30 |

**Why it lost:** the beam charged 0.35 s on *every* new target. Towers aim at the creature furthest along the road; in a
swarm that is the one about to leave reach or the one just killed, so the laser spent most of its time charging: 17% of
its data DPS against dense Skitters (the gun 92-100%), under 50% against Prowlers. On paper (and in the ledger) it was 30.8
DPS per 100 coins against the gun's 28.6, so the model counted a laser the game did not have.

**The change (mechanics and three data fields; no damage, rate, range or price changed):**
- A beam holds its creature until it dies or leaves reach (`Tower.ReleaseBeam`), and charges (`TowerDefinition.BeamLockSeconds`,
  0.35) only when it was idle. A live beam swings straight on to the next creature.
- `BeamSweepSeconds` (Laser 0.05): after a kill the beam is on the next creature within 0.05 s instead of waiting out its
  pulse interval. Small creatures die at the rate the beam can swing, not at its pulse rate: that is the swarm role. On a
  big creature nothing changes (a plain 40 / 67 / 120 DPS).
- The Laser's pulse lands the frame it is fired (`ProjectileSpeed` 40 -> 400, the beam's view is unchanged), so it never
  spends a second pulse on a creature that is already dead.

**Roles now:** the Gun is the cheap all-rounder (the only C1 gun that hits ground and air, 50 to build, fills spots);
the Laser is the swarm melter on the ground: 1.2 to 2x the gun's holding power per coin against Skitters and Prowlers,
it alone holds a dense swarm, and 1.05 to 1.2x against heavy creatures; the Cannon is splash and heavy hides.

**In the model: unchanged.** The ledger already counted the laser at its data DPS (30.8 per 100 coins, against the gun's 28.6),
which the laser now really delivers; against swarms it delivers more. Modelling the sweep (tried tonight: a creature that
needs k pulses dies in (k-1) intervals plus the sweep, capped at 1.3x) made the solver build Glass Meadows' and Quartz Flats'
opening waves 10 to 45% tougher (it expects lasers on the first ants), and the probe bot lost both on Medium three runs out of
three. So a laser player keeps the swarm advantage as surplus, and a gun player is not punished for it.

## Drones, one at a time (2026-11-05)

Remco: "De drones krijgen geen introductie, dit moet wel gebeuren met uitleg in de eerste levels, tot de uitleg er is moet je
ze niet kunnen kiezen."

**Where each drone first earns its price** (Crystal Plains, from `Tools/PlayTest/Scripts/DroneValue.cs` and the wear the
probe bot measured, `[Probe] ... wear gates / guns, repairs bought`):

| Level | Gate / gun wear Easy, Medium (HP over the level) | One harvester nets (Easy / Medium / Hard) | Other |
| --- | --- | --- | --- |
| C1_01 Landing Site | 500 / 1771, guns 86 / 81 | +24 / +59 / +90 | no gun-biters, one front; the tutorial level |
| C1_02 Glass Meadows | 490 / 989, guns 22 / 54 | -41 / -21 / +1 (short level, far nodes) | |
| C1_03 Shard Ridge | 1062 / 1165, guns 0 / 141 | **+39 / +80 / +107** (nearest nodes, 16 waves) | first flyers that bite guns (Skyray) |
| C1_04 Fault Line | 1500 (Easy), guns 209 | +10 / +41 / +75 | **two ground fronts**, packed raids, bat swarms |
| C1_05 Quartz Flats | 209 (Easy), guns 18 | +40 / +68 / +97 | |
| C1_06 Crysium Vein | | +6 / +22 / +50 (far nodes) | 40-54 gun-biters |

A repair drone (120) mends 8 HP a second for free; mending by hand costs 0.3 a health point, so it pays for itself after
400 HP of wear. A harvester (150) brings 10 ore a trip; a trip is two flights between base and node plus 4 s of mining.
The attack drone (180, 12 DPS, flies) is poor DPS per coin (a sixth of a gun) but goes where the towers are not.

**Choice (one kind a level):** repair drone at **C1_02** (the gates lose 500 to 1000 health there, more than the drone costs
to mend by hand; C1_01 stays the tutorial: turrets, the gate, the hero), harvest drone at **C1_03** (the first level where one
harvester clearly earns its price back; at C1_02 it would lose money), attack drone at **C1_04** (the second road and the
flyers that bite guns).

**How it works:** `CampaignLevelSpec.IntroduceDrones` names the kinds a level brings in; the generator offers a kind from
that level on, in campaign order (`ConfigureLevelDrones`; a kind not introduced yet leaves the level's drone slot empty, so
the panel never shows it and the base's doorstep does not open an empty panel). The intro dialogue (`Dlg_L02_RepairDrone`,
`Dlg_L03_HarvestDrone`, `Dlg_L04_AttackDrone`, LevelStart, after the level's own opening line, once per save) names its drone
in `DialogueDefinition.IntroducedDrones`; while it plays the drone panel frames that card in pulsing yellow with a NEW tag,
and keeps it lit for 15 s afterwards or until the first one is bought. Replaying an earlier level does not offer later
drones (the same rule as the gadgets sold per level). `DroneIntroTests` guard it: level 1 offers none, an offered kind is
never taken away, every kind is introduced by a dialogue where it is first offered, an intro plays at level start and only
names drones the level offers.

**In the model:** `LevelInput.Harvest` (false while the level offers no harvest drone): the ledger books no ore there.
C1_01 and C1_02 therefore lost the ore money the model used to give them, and their creatures were re-solved to the same
human curve (mean toughness Easy/Medium/Hard/Psycho: C1_01 1.30/2.29/2.13/2.87 -> 1.03/1.76/1.80/2.42, C1_02
1.10/1.66/1.51/2.11 -> 0.99/1.34/1.24/1.93). The waves of C1_03 to C1_06 are byte-for-byte what they were.

**Finding, not changed:** the ledger books the nodes' whole stock (less one harvester) over the first 60% of the waves.
Measured trip times say one harvester mines about a third of that in a level, two about two thirds. A rate-limited ore
model (tried tonight) took 150 to 250 coins a level off the model's purse, which pushed C1_04 Medium off its curve and C1_03
Psycho's boss waves over the hero cap. `Utilisation` and `HumanUtilisation` were calibrated against a bot that never
harvests while the model counted ore, so the ore model and those two calibrations should change together, with a bot that
buys harvesters. Left for a separate pass.

## Probe determinism (2026-11-05, round 4d)

After boss round 4c the probe went from 3/3 to 1/3, 1/3 and 2/3 over three whole-class runs, each losing a different
Medium level (Crysium Vein, Landing Site, Shard Ridge). Verdict: **not a regression, and not spawn variance: a harness
artefact of slow editor frames.**

- **No campaign change in 4c.** `git diff 2462c6d2..853372dd` touches no campaign data (levels, waves, towers, creatures,
  difficulties); the code it adds to shared classes is inert in a level without those bosses (`Enemy.SourceScale` null,
  `Tower` overgrowth rate 1 and never drained, `ClosedPads` empty, `RootOmen` only built when a wave sends a rooted boss,
  `BossFrostShade` a boss-only overlay colour).
- **Old timing.** The probe set `Time.timeScale = clamp(0.2 / realFrame, 1, 20)`. That kept a step at 0.2 s only while an
  editor frame took 10-200 ms; a frame slower than 0.2 s (an import, the Blender agent exporting in parallel, a GC) hit the
  floor of 1 and became a step of up to `Time.maximumDeltaTime` (0.333 s). The steps a run took depended on the machine,
  so two runs of one build differed, and coarse steps cost the bot shots and gate holds.
- **Now.** Every frame advances the game by exactly `StepSeconds` 0.2 (`Time.captureDeltaTime`), and the level's draws
  (spawner: nests, spread, pool cells; wave flow: nest assignment, group jitter) are seeded from the test
  (`LevelSession.Seed` -> `RandomFor(SpawnStream/WaveStream)`; `ProbeSeed` 1; unseeded in the game). Two runs of one
  build give byte-identical traces. `Tools/PlayTest/probe_arg.txt` takes `seed=N` and `step=S` to sample other draws or
  step sizes; every run appends a line to `Logs/probe_results.txt`.
- **Evidence (Landing Site, Shard Ridge, Crysium Vein; Easy and Medium):**

| Step | Seeds | Runs | Lost |
| --- | --- | --- | --- |
| 0.2 s (the probe's) | 1-7 | 42 | 0 |
| 0.05 s (finer than any old frame) | 1-2 | 12 | 0 |
| 0.333 s (a hitching editor) | 1-4 | 24 | 3: Crysium Vein Medium seed 1, Landing Site Medium seed 3, Shard Ridge Medium seed 4, each at its last wave |

  The three levels that lost under 0.333 s steps are exactly the three the flaky runs lost. Finer steps win like the
  probe's 0.2 s, so 0.2 s is not a generous setting; spawn draws alone never decided a run. Shard Ridge Medium is the
  thinnest margin (base 480-522 of 600 at 0.2 s).
- **Assertions unchanged:** every band, check and win requirement stands; the seed and the step only remove the machine
  from the result.
- **What determinism exposed: Quartz Flats Easy is bistable for the bot (open, pre-existing).** With the fixed seed the
  Easy/Medium class fails the same way every run: Quartz Flats **Easy** lost at wave 15 (seed 1), while Quartz Flats Medium
  wins. Seeds 1-7 at 0.2 s: Easy won 3 of 7 (lost seeds 1, 2, 3, 7), Medium 7 of 7. It is not the step (0.05 s: 1 of 3 won;
  the old adaptive timing: 1 of 4 won), not the new seeding (both streams sharing one seed: 3 of 6 won), and not round 4c:
  the committed probe and spawner (unseeded, old timing) won 3 of 5. The earlier green runs were luck. The branch: the
  winning runs buy one Cannon before the dense waves (12-15), the losing runs stay all Pulse Rifle (10-12 towers, 0-2
  upgrades), the hero goes down at the trunk gate from wave 12 and the crowd breaks through. The ledger disagrees: it rates
  C1_05 Easy at 1.32x the firepower needed against Medium 1.09x (Easy asks 0.82 of Medium). So either the bot's purse
  timing misses the splash gun a player would buy for the crowd waves, or Easy's thinner money (income 0.8, start 0.8)
  costs more on this level than the model counts. The assertion is unchanged (it still fails at seed 1); choosing a
  winning seed or narrowing the level would hide the finding. Options for the curator: a bot rule "one splash gun before
  a dense wave in the preview" (like its Flak reserve), a look at C1_05 Easy money, or a documented narrower assertion like
  `TwoFrontLevels`.

### Quartz Flats Easy: a splash gun before the crowd (2026-11-05, round 5)

Curator decision on the bistable Quartz Flats Easy: the bot gets a human-like habit, analogous to its Flak reserve. While no
splash gun stands and a wave to come is a crowd for a shell (the ledger's splash count `1 + 2 x radius x ground density`
reaches `CrowdSplashTargets` 2), it saves the cheapest owned splash gun's price from the wave whose income covers it twice
(`SavingFrom`, shared with the air reserve) and builds it (`SplashReserve`, `CheapestSplash` in `BalanceProbeTests`). Only the
bot changed; no level, wave or money data moved, so the ledger is unchanged.

| Quartz Flats, step 0.2 | Seeds | Easy won | Medium won |
| --- | --- | --- | --- |
| before (round 4d) | 1-7 | 3 of 7 | 7 of 7 |
| with the splash habit | 1-7 | 7 of 7 (base 600/600 every seed) | 7 of 7 (base 132-588) |

Crysium Vein Easy/Medium also won on all seven seeds in the same runs. C1_05 Easy money was not raised.
