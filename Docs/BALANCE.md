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

## Continent bosses as played (2026-11-05)

Remco: "check aan het eind nog even of dit in balans is op het moment dat die levels gespeeld worden, dus welke wapens heb
je, welke upgrades heb je unlockt, heb je superweapons, lukt het met geld etc."

**How (two lenses, both on a stand-in, because only C1_01-06 exist).**
- **Ledger** (`FinaleModel`, Game.Balance; `ContinentFinaleTests`): the campaign extended to level 20 of Crystal Plains
  (`FinaleModel.Planned`: the real six levels, then planned levels on C1_06's field paying the continent's research curve,
  `BalanceSheet.PlannedTowers` handing out Tesla from C1_09, Railgun from C1_13, Cryo from C2_04). Each finale is C1_06's own
  waves with its last boss wave carrying the continent boss, all waves scaled by one factor so the plain waves sit on the
  level-20 human curve (ramp +20 %). Every later continent is judged as "a player at the end of a continent": later
  continents raise creatures and firepower together through the solver's wave factor, so the boss against its own field is
  what matters. Lines in `Tools/PlayTest/finale_balance.txt`.
- **Finale probe** (scratchpad `round9/finale.sh`): the boss's arena field on Medium, every wave at the finale's health (C1_06's
  Medium boss wave x the level-20 scale 1.55), the finale's Medium money (1407) spent by a plain builder on the loadout at
  level 1 (11 guns spread along the main road, no upgrades, no hero abilities), the hero on its post, one seed.

**Expected player at each finale.**

| Finale | Guns owned (counted) | Research earned E / M / H / P | Not counted (surplus) |
| --- | --- | --- | --- |
| Gravemaw, C1_20 | Gun, Cannon, Laser, Flak, Tesla, Railgun | 138 / 276 / 690 / 900 (all C1 upgrades from Hard on) | Acid Sprayer (no data), Mine, Chrono Field, Lava Ball, base turrets, drones, Quick Reload/Wide Slash/Field Repair |
| Cinder Matriarch, C2_20 | + Cryo (no upgrades: C2 research has no budget yet) | same | Decoy Beacon (no data), the above |
| Frost Warden, C3_20 | same | same | the five combos (no data; Artillery/Cryo Beam/Storm Rifle left out) |
| Rotbloom, C4_20 | same | same | Barricade (no data) |
| The Shattered, C5_20 | same | same | superweapons, Spike Strip, Rollback Field (no data) |
| The Sealed One, C6_20 | same | same | allied Burrowers (no data) |

Money at the boss wave (ledger purse): Easy 1281, Medium 1407, Hard 1923, Psycho 1817. Superweapons do not exist yet, so the
C5/C6 finales are judged without them: they can only make those fights easier.

**Counterplay owned by its finale** (`BalanceSheet.Finales`, `EveryBossCounterplay_IsOwnedByItsFinale`): Gravemaw's Mine and
spreading (C1); the Matriarch's Cryo bait (Cryo returns in C2, before her finale) or no cold at all; the Warden's fire (the
Lava Ball arrives in Crystal Plains 7-20, so long before C3) and the hero's Repair; Rotbloom's splash (Cannon), sky guns for
the pods (Flak) and Repair; the Shattered's sky guns and the second road; the Sealed One's mix of 3-4 tower types. Nothing
depends on a tool that arrives later. Cryo itself is counted at level 1 strength (its continent has no research budget yet).

**Result per boss (health after the change; ledger: boss alone with the hero, against plain waves and the cap; probe on
Medium).**

| Boss | Health before -> after | Ledger boss alone E / M / H / P (cap 1.00 / 1.15 / 1.25 / 1.35) | Finale probe, Medium | Verdict |
| --- | --- | --- | --- | --- |
| Gravemaw | 1050 -> 2200 | 0.80 / 1.10 / 0.81 / 0.78 | won, base 600/600 (5401 hp in game; burrows give the guns time) | good; at 1050 it was a pushover at its finale (0.45 of the cap) |
| Cinder Matriarch | 5000 -> 1250 | 0.36 / 0.49 / 0.36 / 0.34 | at 2750/1500/1250 with a Cryo on the road: lost (boss left at 53/22/19 %); at 1250 without cold towers: won, base 583 | good: her counter decides the fight (5000 was 2.8x the cap) |
| Frost Warden | 4500 -> 850 | 0.44 / 0.60 / 0.44 / 0.41 | at 1550: lost at 47 % left; at 850: won, base 600 | good (4500 x1.6 armour was 4x the cap: round 8's "reached the base with 11 %" was this) |
| Rotbloom | 4000 -> 3350 | 0.80 / 1.09 / 0.82 / 0.76 | won, base 600/600 | good |
| The Shattered | 4800 -> 1600 | 0.52 / 0.71 / 0.52 / 0.49 | at 2450/1950: lost at 21 %; at 1600: won, base 600 | good |
| The Sealed One | 9000 -> 3100 | 0.81 / 1.10 / 0.83 / 0.76 | lost with 85 % left (7611 hp): its new road from the centre skips the defence along the main road | **too hard, open** (below) |

Mechanic loss stays at 1-10 % of each fight (cap 35 %). Rewards follow threat (Gravemaw 257 -> 500, the Sealed One 1778 -> 681).

**What the two lenses taught.**
- The ledger rates a lone boss as easier to stop than the game does: a boss that reaches the base ends the level in seconds
  (it climbs on and the base has 600), and slow bosses spend less time in range than the window assumes. Gravemaw is the
  exception because its burrows hold it on the field. So the health was set by the probe (a plain builder that just wins
  leaves a competent player with upgrades and hero play a margin) and the ledger test only guards the top (killable inside
  the hero cap everywhere); "boss alone under a plain wave" is reported, not failed.
- **Hard and Psycho:** with one health for all difficulties the boss alone asks 0.35-0.8 of those caps, below their plain
  waves; their difficulty comes from the bigger crowds and extra boss waves. If Remco wants the boss itself to be the climax
  there, a per-difficulty boss health share on `DifficultyProfile` (about Hard x1.45, Psycho x1.65) would do it: a proposal,
  not built.
- **Finale escorts:** C1_06's last boss wave with any of these bosses asks 1.5-2.4 with the hero, well over the cap; the solver
  would soften the whole wave, boss included. A finale script should send its continent boss with light escorts.
- **The Sealed One (open):** "the map comes to it" puts its road from the centre to the base, so a defence built along the old
  road barely touches it. The ledger counts the main road. Options for Remco: let the new road be at least as long as half the
  main road, open it a wave earlier so players can build along it, or lower its health much further (about 450 would let the
  probe win, which is no final boss).
- Probe caveats: one seed, Medium only, no upgrades, Remco's save not used for upgrades in the reasoning (it was restored
  byte-identical after every run).

## Boss health per difficulty and the finale probe as played (2026-11-05, round 10)

Remco: boss health per difficulty, yes. Curator concern: round 9 set boss health with 11 level-1 guns, no upgrades, no hero
abilities, Medium only, which is weaker than a real player ("te makkelijk is saai").

**Boss share (data).** `DifficultyProfile.BossHealthShare` (generator: `BalanceSheet.Difficulty.BossHealth`): Easy 1.0,
Medium 1.0, Hard 1.45, Psycho 1.7, on top of the difficulty's creature health. Applied in `EnemySpawner.Launch` to creatures
with boss data (the six continent bosses) and in the ledger through `EnemyStats.IsContinentBoss` /
`DifficultyStats.ShareFor` (`WaveLoad`, `KillPurse`). Level bosses in campaign waves (Hive Queen, Brood Mother, Crystal
Warlord, Acid Empress ...) do not take it: the wave solver already sizes them per difficulty with the rest of their wave
(bands x1.8 for boss waves), so no campaign wave or band moved. Derivation (`ContinentFinaleTests.BossHealthShare_...`,
table `Tools/PlayTest/finale_boss_share.txt`): with one health everywhere the boss alone asked 0.96 of Medium's hero cap but
0.65 / 0.58 of Hard's / Psycho's; the share each needs to match Medium's ratio is 1.44-1.48 (Hard) and 1.66-1.73 (Psycho)
for all six bosses. Easy would need 1.19; it stays at 1 (Easy forgives: 0.84x Medium's ratio). The test fails when Hard or
Psycho leave Medium's ratio by more than 12 % or Easy asks more than Medium.

**Finale probe as played** (scratchpad `round10/finale.sh`, `batch_f.sh`): the boss's arena field on each difficulty; every
wave at the finale's health (the stand-in's last boss wave on that difficulty x the plain-wave scale of `FinaleModel`:
1.56 / 1.55 / 1.77 / 1.53); the finale money (1281 / 1407 / 1923 / 1817); the research that difficulty's path earns by
Crystal Plains 20 (138 / 276 / 690 / 900) bought cheapest first into the in-memory progress (Medium: every C1 gun damage 2,
range 2, rate 1, base, gates and hero 2; Hard/Psycho: all C1 upgrades); a repair and an attack drone bought first; about 60 %
of the money on the six C1 guns in turn spread along the main road, the rest on their upgrades, then more guns; income spent
on upgrades during the fight; the hero's shockwave and gate repair used as the probe bot does; a hurt gate repaired with
money. The Cinder Matriarch's player uses her counter (a Cryo as bait near the road's start); one Medium run without it.
One seed, 0.2 s steps.

| Boss | Easy | Medium | Hard | Psycho | Final health |
| --- | --- | --- | --- | --- | --- |
| Gravemaw (2500 in the runs) | won, base 800/800, to 3.9 m of the base, gates -700, mechanic 40 % | won, 800/800, 3.9 m, -700, 43 % | won, 900/900, 3.9 m, -1600, 36 % | won, 900/900, 3.9 m, -800, 36 % | 2200 -> **2300** (ledger ceiling) |
| Cinder Matriarch, Cryo bait | won, 800/800, held in her hunt, 98 % | won, 800/800, gates -700, 51 %; **no bait: lost, boss at 25 %** | won, 900/900, 80 % | **lost**, boss at 13 %, 88 % | 1250 -> **1450** |
| Frost Warden | won, 800/800, got 62 % of the way (15.5 of 41 m), gates -1400, 20 % | won, 800/800, 71 % (11.8 m), gates -2100, 26 % | won, 900/900, 77 % (9.4 m), gates -3200, 28 % | **lost**, boss at 2 %, gates -4000, 37 % | 850 -> **1300** |
| Rotbloom | won, 800/800, 16.9 of 30 m, -786, 14 % | won, 800/800, 8.1 m, -2951, 25 % | won, 900/900, 18.5 m, -2587, 36 % | won, 900/900, 3.1 m, -4203, 30 % | 3350 -> **3100** (3350 lost Medium with the boss at 0 %) |
| The Shattered | won (2350), 800/800, -758, 42 % | won, 800/800, gates untouched, 50 % | **lost**, boss at 29 %, 71 % | **lost**, boss at 19 %, 65 % | 1600 -> **2200** |
| The Sealed One (3500 in the runs) | won, 800/800, held at the last gate (6.7 m), gates -756, 6 % | won, 800/800, last gate, -1502, 5 % | won, 900/900, last gate, -1670, 6 % | won, 900/900, last gate, -2430, 7 % | 3100 -> **3350** (ledger ceiling) |

"Mechanic" is the share of the boss's time alive in any state but walking the road (entrance, burrow, hunt, climb, freeze,
split, adapt). Base 800/900 is the research base upgrade.

**Verdicts.**
- **Easy:** forgiving everywhere (all won with the base whole; in an earlier 0.1 s run at 2200 Gravemaw reached the base on Easy and took 80 of it).
- **Medium:** every boss reaches deep into the defence and costs gates 700-3000 hp; none touches the base against a mixed,
  upgraded defence. The Matriarch without her counter wins (lost with her at 25 %); with it she dies in her hunt. Medium is
  now a real gate fight; the base threat comes from the bosses that rush (Matriarch, Shattered, Warden).
- **Hard:** tense for the rushers (Warden won at 77 % of the way, Shattered lost at 29 %), comfortable for the slow walkers.
- **Psycho:** the plain bot loses to the Matriarch (13 %), the Warden (2 %) and the Shattered (19 %), all close; it wins the
  slow three with the base whole. A player who builds at the boss's way in (the bot spreads along the whole road) and plays
  the counter wins; never impossible, but no margin.
- **Ledger ceiling:** the slow walkers (Gravemaw, the Sealed One) are held at the last gate in every run whatever their
  health (3100-3900 for the Sealed One, 2200-2800 for Gravemaw gave the same picture), but the ledger rates them at the hero
  cap; their health is the highest that keeps `ContinentFinaleTests` killable (boss alone <= cap x 1.04). Their threat is the
  gate fight, not the base.
- **Open:** the Shattered's arena ruin gate is 10.7 m from the base, so its split and rejoin happen next to the base; on Hard
  and Psycho that decides it (lost at 2200, 2350 and 2600; Medium flips from trivial at 2200 to lost at 2600). The real C5
  finale should put its ruin gate earlier on the road; re-tune it then.

## Tower parity check (2026-11-06, round 13b)

`TowerParityTests` (EditMode, a test only: no balance value changed) values every buildable gun at every level per coin
against four fixed reference streams and writes the table to `Tools/PlayTest/tower_parity.txt`. It uses the ledger's
per-tower formula: DPS x overkill x splash (density-based, capped at `MaxSplashTargets`) x time in reach
(`window + 2 x range / speed`), divided by the coins to reach that level. Hybrids cost both partners plus the recipe,
combined at the cheapest level. Utilisation cancels and is left out; slow, beam lock, placement and permanent upgrades are
not modelled.

Streams: swarm (ground, 40 hp, 1.4 speed, 1.2 per cell, 10 s), heavy (ground, 450 effective hp, 0.7, 0.15, 12 s), air
(45 hp, 1.4, 0.8, 10 s), boss (one target, 2500 hp, 0.5, 4 s gate hold). Gates, the base turret, Support and slow masts
under 10 DPS (Cryo) are skipped. Ground-only guns are not compared on air, Flak only on air.

Bands: value per coin within 0.75 to 1.35 of the median of the towers on the same layer at the same level; an upgrade buys
at least 0.85 of what a new build buys per coin. 46 cases fall outside; they are listed in `KnownOutliers` and show as
ignored ("Known outlier, awaiting Remco's playtest: ...") until Remco has played them:

- Railgun: 0.16 to 0.46x on swarms and air (overkill), 0.70x on heavy at L2; its upgrades fall to 0.46x.
- Tesla 1.8 to 1.9x on swarms, above the Cannon (1.48x at L0, its L2 upgrade 0.59x from overkill).
- Flak 1.8 to 2.4x the air median, but its upgrades buy only 0.82x a new build.
- Laser 1.38x / 1.355x against a boss at L0 / L1 (cheap beam DPS); against swarms it sits at the median (1.06x). The
  critic's "Laser high vs swarms" does not show in this model.
- Hybrids: Storm Rifle 1.9 to 2.4x on swarms and air; Artillery and Cryo Beam 0.4 to 0.7x on swarms and air.

## Monoculture check (2026-11-06, round 20)

Remco's rule: "Als je kunt winnen door alleen een rijtje torens neer te zetten van hetzelfde soort, dan is het niet in
balans." The right towers, upgrades, selling and rebuying, mines, drones (and later superweapons) must change how a run
goes, and placement must ask for thought. Round 20 built the tools to measure that; **no balance value changed**.

**EditMode (seconds, report only, the suite stays green):**
- `MonocultureLedgerTests` -> `Tools/PlayTest/monoculture_ledger.txt`: the ledger (human lens, with the hero) per C1 level
  and difficulty, once with the whole owned loadout and once per owned gun alone. "HOLDS" = every wave inside the hero cap,
  the bar the full loadout must pass. Breaches of the rule (Medium at most one gun alone, Hard/Psycho none) are listed at
  the end.
- `TowerRolesTests` -> `Tools/PlayTest/tower_roles.txt`: (1) upgrade vs n towers of the same type at equal coins, per step:
  hit, single-target DPS, DPS through an 8 plating, group DPS, targets per volley, rate and risk (Remco: the upgrade must win
  on hit and single target, more towers only on targets/rate and spread risk; "rule met" per step); (2) role matrix, DPS per
  100 coins against swarm, clustered group, air, armour (the current pool armour, and a flat per-hit plating 4/8/12 with a
  20% floor) and one high-health target; hybrids priced as partners + recipe; (3) best vs median build spot on C1_03 per
  tower: road in reach and bunched road (queue before a gate, bends).
- `DroneLensTests` -> `Tools/PlayTest/drone_lens.txt`: per level, difficulty and drone its price against 3 x the cheapest
  owned gun, its rate (harvest coins/s over the real wave timeline and ore stock; repair at the towers' repair price; attack
  at the waves' bounty per hit point), payback in seconds and waves, and for the harvester the net coins by the level's end
  when bought at wave 1, a third, half, two thirds and the last two waves, plus the latest wave that still breaks even.

**PlayMode (`MonocultureProbeTests`, one class at a time, deterministic: seed 1, 0.2 s steps):**
- Strategies (`ProbeStrategy`, state in `BotRun`): *mono T* (one owned type, placed by role, hero on, no drones, gadgets or
  selling; wrecks scrapped), *creative* (every owned type placed by role: splash guns weigh the queue and bends, rate-of-fire
  guns the road in reach, sky guns under the flight routes; plus one drone of each kind the level offers, two mines a wave
  in the gate's queue before crowd and boss waves and one on a burrowing boss, one slow field a wave past the gate, and it
  sells its weakest ground gun when the refund buys the sky gun the preview asks for), *naive* (the creative bot's purse
  and choices on seeded random spots, random upgrades, gadgets anywhere on the road). The plain `BalanceProbeTests` bot is
  unchanged (`ProbeStrategy.Greedy`).
- Smoke (default category, about 8 runs): C1_03 and C1_06 on Medium, mono Pulse/Cannon/Laser + creative ->
  `Tools/PlayTest/monoculture_smoke.txt`. Full set: `run_tests --mode PlayMode --filter MonocultureProbeTests --include_explicit true`
  (category "Monoculture"; `Tools/PlayTest/probe_arg.txt` `levels=`/`difficulties=` narrow it) -> `Tools/PlayTest/monoculture.txt`:
  6 levels x Medium/Hard x (each owned ground-capable mono type + creative + naive).
- Columns: result, stars, wave reached, base hp, towers built, upgrades, sold, mines, slow fields, drones, ore delivered,
  coins unspent, and per tower type the damage really dealt per tower-second with creatures on the field against its data
  DPS (the "Laser does almost no damage" question). The end of the file sums that over all runs.
- Rules, **reported, not failed** (the test shows as ignored with "Known, awaiting Remco's rebalance by role" while any
  breach stands; level errors still fail): Medium at most one mono type wins and only with one star; Hard none; the
  creative bot wins Medium and Hard with two or more stars; the naive bot loses from Medium on (if it wins, the level does
  not ask for thought).

**First results (2026-11-06, full set, seed 1; tables in the files above):**

| Level | Medium: mono winners | Medium creative / naive | Hard: mono winners | Hard creative / naive |
| --- | --- | --- | --- | --- |
| C1_01 Landing Site | Pulse 3*, Cannon 3* | won 3* / won 3* | Cannon 3* | won 3* / won 3* |
| C1_02 Glass Meadows | Pulse 3*, Cannon 3*, Laser 3* | won 3* / won 3* | Cannon 3* | won 3* / won 3* |
| C1_03 Shard Ridge | none | won 3* / lost w13 | none | won 3* / won 3* |
| C1_04 Fault Line | none | won 3* / lost w8 | none | won 2* / lost w6 |
| C1_05 Quartz Flats | none | won 1* (base 210) / lost w13 | none | won 2* / lost w11 |
| C1_06 Crysium Vein | none | won 3* / lost w7 | none | lost w11 / lost w7 |

- From C1_03 on no row of one type wins, on Medium or Hard. C1_01 and C1_02 are won by a row of Cannons on Hard and by
  every type on Medium, and by the naive bot: the first two levels ask for no thought. Shard Ridge Hard is easier than its
  Medium for the naive bot (Hard income 1.1 against Medium 0.95).
- The creative bot misses the bar on Quartz Flats Medium (1 star) and loses Crysium Vein Hard.
- In-game DPS per tower-second with creatures on the field: Cannon 25.6 (143% of its data: splash hits count per creature),
  Laser 19.3 (39%), Pulse 5.2 (30%), Flak 4.0 (16%, only fires in raids). Per 100 coins the Laser deals 14.8 against the
  Pulse's 10.4, so in play the Laser does not do "almost no damage"; both lose most of their data DPS to time without a
  target in reach. The ledger pre-screen agrees with the probe except on C1_03 Medium (ledger: Pulse alone holds).
- The ledger lens: an upgrade beats n towers of the same type at equal coins on single-target DPS on 10 of 20 steps (met:
  Pulse L1->L2, Cannon both, Laser L1->L2, hybrids); not met: Pulse L0->L1, Laser L0->L1, Tesla, Railgun, Flak, Cryo.
- Drones: the harvester (150 = 3 Pulse Rifles) bought at wave 1 nets +40 to +136 by the level's end, breaks even when
  bought up to wave 7-12 of 16-18, and loses 60-110 in the last two waves. The creative bot (which saves for it once three
  guns stand and the sky is covered) never had 150 spare before the late waves on Medium; the repair drone it did buy pays
  back after 50 busy seconds; the attack drone (12 DPS for 180) is a quarter of a Pulse Rifle per coin.

## Roles by enemy (2026-11-06, round 21a)

Remco's role model (memory `tower-defense-balance-principle`): many enemies want rate of fire, enemies close together want
the Cannon's splash, air wants air guns, armour and high health want Railgun and Cannon; an upgrade must hit harder than a
second tower of the same type for the same coins; placement must matter; a drone is an investment. Round 21a turns the
round-20 measurements into mechanics and data.

**Mechanics (code, data-driven):**
- **Plating** (`EnemyDefinition._plating`, `_platingFloor` 0.2): every hit loses the plating's points, at least the floor
  share gets through (`Health.ThroughPlating`; the model's copy `Game.Balance.Plating`, checked equal in `PlatingTests`). It
  never wears off, so it taxes light hits and barely touches heavy ones. Damage dealt a little every frame (a rolling lava
  ball, a ground drone) passes it (`ISourcedTarget.TakeDamageFrom(..., continuous)`). The bosses keep their armour pool
  (bar, regrowth); species armour is plating now: Stinger 200 hp/6, Shardback 220/7, Bulwark 380/9 (none in C1).
  **Cragback**: a light hide of 5 ("Use the cannon. Heavy shells for heavy hides", Dlg_L01_Stone): Gun Turret 5 -> 1,
  Laser 8 -> 3, Cannon 25 -> 20 (upgraded 15 -> 10, 18 -> 13, 85 -> 80). The heavy plates of level 11 stay a story beat.
  Threat prices plating against a 15-point hit (`BalanceRules.PlatingReferenceHit`), so a plated creature pays more.
- **Layer share** (`TowerDefinition._groundDamageFactor` / `_airDamageFactor`): the Gun Turret does half damage to air-only
  creatures (a Wisp walks the road and takes full damage). The ledger and the lenses use the same share.
- **Aimed shots retarget** (`Projectile`): a single-target shot whose creature died in flight turns to the nearest live
  creature of the same layer within 1.5 cells instead of landing on nothing. Round 20 measured the Gun Turret at 30% of its
  data DPS in play, mostly shots on ants already killed by the shot before; a shell's splash never lost them.
- **Ledger**: per-wave health-weighted plating mixes (`WaveLoad.StreamPlates/BossPlates/AirPlates`, `PlateMix`) scale each
  gun's capacity by the share of its hit that lands; the layer share likewise. FinaleModel, the role lens and the probe bot's
  own judgement use the same ledger.
- **Field ease** (`LevelDefinition._balanceFieldEase`, per difficulty, balance model only): how much more the towers do on a
  map than the ledger reckons, measured with the probes. The two short opening levels were held by every bot with the base
  untouched and 150-250 coins to spare on Medium and Hard (the ledger counts a shell on at most 2.4 ants; packs of six die to
  one). Values (Easy/Medium/Hard/Psycho, a short array repeats its last): C1_01 1/1/1.6 (Easy and Medium stay forgiving,
  curator rule), C1_02 1/1.8/1.2, C1_03 1/0.85/1, C1_05 0.85/0.85/1, others 1. Below 1 where the creative bot fell short
  of the competent player the ledger assumes. FinaleModel ignores it (its field is a stand-in).
- **Loose troops** (`TroopSpec.Loose`): creatures that walk in one at a time, a whole interval apart (`LooseRunners` 1 s =
  1.6 cells, beyond a shell's reach), alternating over the lanes.

**Upgrades** (BalanceSheet prices, Flak damage): every step's DPS per coin spent to reach it is at least 3% above the level
below, asserted in `TowerRolesTests.EveryUpgradeStep_BeatsMoreTowersOfTheSameType` (exception: the Cryo, a slow mast whose
stars buy chill, Remco 2026-11-01). Prices build/L1/L2: Gun Turret 50/50/90 (was 60), Cannon 100/110/180 (120), Flak
120/100/150 (140/200; damage 14/25/40, was 22/34), Laser 130/75/145 (90/170), Tesla 140/100/155 (115/190), Railgun
220/125/200 (150/255). More towers still win on targets per volley, rate and spread risk (tower_roles.txt).

**Waves** (LevelSpecs.Crystal): C1_01 ant packs (Cannon) + loose runners from wave 3 (rate) + Cragbacks from the rifts (4)
and the road (6), rhythm with real heavy waves; C1_02 mostly loose runners, packs from 4, Cragbacks from 5; C1_03 to C1_06
add loose runners, Cragbacks earlier (C1_04 wave 6, C1_06 wave 7), more and bigger raids (C1_03 Medium 3 raids, Hard 4,
raid scale 1/1.3/1.5; C1_04 1/1.2/1.4; C1_05 unchanged sprinkle). Flak is owned from C1_03, where the first
flyers come; the Cannon (plating's answer) from the start; Railgun (C1_13) is not needed in C1.

**Bots** (BalanceProbeTests, every strategy): a repair is bought only when the structure would fall in the coming wave (its
health below the worse of this and the last wave's loss x1.25) or when the repair is cheaper per point than the cheapest
gun that would have to stop the same leak for the rest of the level; coins spent on repairs are a column in
monoculture.txt and in the probe trace. The creative bot saves for the harvester in the first third of a level while its
guns hold the waves at hand outright, and lays a slow field where its guns overlap most when the slow pays (below), not
only when pressed.

**Slow as damage** (tower_roles.txt section 4): damage per creature passing = DPS x seconds in reach; a slowed stretch L at
speed share s adds L/v x (1/s - 1). Cryo L0 (90 coins, 0.5) doubles a neighbour's damage per creature on its stretch and
pays from 26 DPS of neighbours (L1 41, L2 55): two Gun Turrets or one Cannon L1 beside it. The Chrono Field (45 coins, 0.45,
20 s) multiplies by 1.56 and pays from 23 DPS against a gun counted for one wave, but 368 DPS against a gun kept for a
16-wave level: it is a late-wave and boss tool, not a default. The creative bot uses that rule (field pays when the guns'
DPS at the queue x the extra seconds beats the same coins in the cheapest gun for the waves left).

**Results (full set, seed 1; Tools/PlayTest/monoculture.txt; before = round 20, after = round 21a):**

| Level | Medium mono winners | Medium creative / naive | Hard mono winners | Hard creative / naive |
| --- | --- | --- | --- | --- |
| C1_01 Landing Site | Pulse 3*, Cannon 3* -> Pulse 3*, Cannon 3* (forgiving, by rule) | won 3* / won 3* -> won 3* / won 3* | Cannon 3* -> none | won 3* / won -> won 3* / lost |
| C1_02 Glass Meadows | Pulse, Cannon, Laser 3* -> none | won 3* / won -> lost w12 (boss) / lost | Cannon 3* -> Cannon 3* | won 3* / won -> lost w12 / lost |
| C1_03 Shard Ridge | none -> none | won 3* / lost -> won 3* / lost | none -> none | won 3* / won -> won 2* / lost |
| C1_04 Fault Line | none -> none | won 3* / lost -> won 3* / lost | none -> none | won 2* / lost -> lost w10 / lost |
| C1_05 Quartz Flats | none -> none | won 1* / lost -> won 1* / lost | none -> none | won 2* / lost -> lost w15 / lost |
| C1_06 Crysium Vein | none -> none | won 3* / lost -> won 3* / lost | none -> none | lost w11 / lost -> lost w10 / lost |

Mono rows now lose everywhere from C1_02 Medium on except Cannon on C1_02 Hard; C1_01 Hard rejects every mono row and the
naive bot; the naive bot loses every Medium+ run from C1_02 on (Shard Ridge Hard fixed). Open: the creative bot misses its bar
on C1_02 Medium/Hard (the Brood Mother at wave 12), C1_04/C1_05/C1_06 Hard and C1_05 Medium (1 star). In play per tower-second:
Cannon 26.4 (138% of data), Laser 24.3 (45%), Gun Turret 14.5 (46%, was 30% before the retarget), Flak 5.1. Repairs bought
with coins: 4.5% of everything earned over the set. Psycho continent-boss health share 1.7 -> 1.15 (ContinentFinaleTests
derives it; the end-game player's cheaper upgrades moved it).

## Roles in play, round 21a2 (2026-11-06)

What changed after round 21a, and why:
- **Ants climb over a crowded gate** (`EnemyDefinition._slipCrowd`, Skitter 3): an ant that reaches a gate where three
  others already bite climbs over and runs on (never past the base). The gate queue was one perfect shell target, so the
  Cannon beat rate of fire even against loose runners.
- **Light guns look past plates** (`TowerDefinition._softTargetShare`, Gun Turret and Laser 0.5;
  `TargetRegistry.FindBest(..., hit, minShare)`, `IPlated`): they aim at the creature furthest along that lets at least half
  of their hit through, and shoot a plated one only when nothing softer is in reach.
- **Shot retarget is data** (`TowerDefinition._retargetRadius`, 1.5 cells for every tower; 0 turns it off; drones 0).
- **Cannon** reloads in 1.6/1.35/1.15 s (was 1.4/1.2/1.0). Tried and dropped: a longer-reaching, faster Gun Turret (4.0,
  0.30 s; the ledger, which already overrates it in play, solved harsher waves and the greedy probe lost Quartz Flats and
  Crysium Vein) and a smaller first shell and blast (22, splash 1.0; the creative bot lost more levels than the Cannon row).
- **Bots:** the creative bot saves for the harvester only while its guns hold the waves at hand with a margin (cover 1.2;
  at 0.9 it starved Fault Line and Quartz Flats Hard).
- **Field ease lowered toward 1:** C1_01 1/1/1.4/1 (was 1.6 on Hard), C1_02 1/1.3/1.1/1 (was 1.8/1.2), C1_03 1/0.85/1,
  C1_05 0.85/0.85/1, others 1. C1_06 wave 15 is plain instead of a rush and C1_04's Psycho raids are 1.3x instead of 1.4x (two band checks sat on their
  edge).
- **Per-role in play** (Tools/PlayTest/monoculture.txt "Roles in play": DPS per 100 coins standing by what the hits landed
  on; loose = a light walker with fewer than two others within 1.2 cells): Cannon 9.1 loose / 7.4 bunched / 3.7 plated,
  Laser 5.6 / 2.7 / 3.3, Gun Turret 4.9 / 3.3 / 3.5, Flak 2.3 on air. The Cannon still wins per coin on loose ants in play;
  the swarm role is not real yet (see STATUS, round 21a2).
- **Derived boss shares moved again** (ContinentFinaleTests): Hard 1.45 -> 1.75, Psycho 1.15 -> 1.5 (continent bosses only;
  none in a C1 campaign wave). TowerParityTests: Cannon L0-L2 against a lone boss sit at 0.72-0.74x the median (band 0.75)
  since the slower reload; listed as known outliers.

## Ledger calibrated in play (2026-11-06, round 21a3)

**Where a light gun's data DPS went** (Tools/PlayTest/tower_efficiency.txt, Medium, every level, mono rows + creative):

| tower | idle (nothing in reach) | wear | waste (shots hitting nothing) | plate | overkill | raw in play / data |
| --- | --- | --- | --- | --- | --- | --- |
| Gun Turret, reach 3.5 (21a2) | 0.52 | 0.00 | 0.09 | 0.10 | 0.14 | 0.47 |
| Gun Turret, reach 4.0 (21a3) | 0.43 | 0.00 | 0.09 | 0.11 | 0.13 | 0.53 |
| Laser, reach 4.3 (21a3) | 0.43 | 0.00 | 0.12 | 0.11 | 0.17 | 0.47 |
| Cannon (shell to the spot) | 0.41 | 0.00 | - | 0.04 | 0.25 | 1.09 (every creature caught) |

Time with nothing in reach is the main loss, so the Gun Turret and the Laser reach as far as the Cannon now (4.0 / 4.3,
+0.3 to +0.8 by level). Overkill is the Cannon's cost: a quarter of what its shells carry is past the last hit point.

**The calibration:** `TowerDefinition._ledgerEfficiency` (balance model only) scales every gun's capacity in the ledger.
It is the share of data damage the ledger does not model (aimed guns: hits carried over data damage; splash guns: time with
something in reach x tower power, the hits per burst the ledger models itself), relative to the tower-seconds mean. A sky gun
is judged only while something flies. Values now: Cannon 1.135, Laser 0.912, Gun Turret 1.017, Flak 0.446 (others 1).
**Re-measure** after any tower, wave or bot change that moves where guns stand or what they hit:
1. `run_tests --mode PlayMode --filter Measure_TowerEfficiency_OnMedium --include_explicit true` (about 8 minutes; move
   `Tools/PlayTest/probe_arg.txt` aside) -> `Tools/PlayTest/tower_efficiency.txt`;
2. `run_script Tools/PlayTest/Scripts/ApplyEfficiency.cs` writes the factor lines into the TowerDefinitions (never by hand;
   the generator does not touch the field);
3. `ApplyWaves.cs`, then EditMode `BalanceLedgerTests`/`ContinentFinaleTests` and the probes.

**Field ease is 1 on every level** (the knob stays, unused).

**Swarm role mechanics:** the Cannon's shell is fired at the spot (`TowerDefinition._homing` false) and flies at 5 cells a
second, so a running ant can be at the blast's edge when it lands; queues, packs and slow walkers are not. Role numbers
count the hit points really taken (overkill out).

**Per role in play (DPS per 100 coins standing):** loose: Laser 6.3 > Cannon 5.6 > Gun Turret 4.8; bunched: Cannon 7.1 >
Laser 3.7 / Gun Turret 3.5; plated: Cannon 3.1 > Laser 2.8 > Gun Turret 2.2; air: Flak 1.7 (Gun Turret 0.2).

Boss shares re-derived: Hard 1.45, Psycho 1.75. C1_04 Psycho raids 1.2x, C1_06 Hard four raids (band edges).

**Round 21a4 (2026-11-06):** retarget follows the genre convention, per tower (`TowerDefinition._retargetRadius`, default
0): Flak guided shells 1.5 cells; Gun Turret bullets none, at 30 cells/s; Cannon shells land where aimed; Laser, Tesla,
Railgun instant. The naive probe bot now plays a careless human (first free spots nearest the defence gate, random
affordable upgrades). Efficiency re-measured; the calibration loop itself (Utilisation fitted to the 2026-11-04 greedy bot)
is to be rebuilt next round, see STATUS.

## The balance pipeline (2026-11-06, round 22)

Remco's requirement: when anything is tweaked, the whole check runs again with one command and no one has to think
anything up. The critic of round 21 found the old loop circular (the ledger calibrated on bots that bought by the ledger,
one seed, a mean-normalised efficiency, wave scripts changed to make a table come out). Round 22 replaces it with a
pipeline of stages, each one entry point, each reading the previous stage's files and the game's definitions. Nothing is
typed in from a past run. Commands: Docs/COMMANDS.md "Round 22"; chained: `bash Tools/PlayTest/balance_pipeline.sh all`.

```
measure (PlayMode)      obvious family + creative reference, every cell, seeds 1..N, fixed step -> yardstick.txt, calibration_records.txt
fit (EditMode)          per-tower efficiency fitted on the records, hold-out + step check -> calibration.txt
apply (script/menu)     calibration.txt -> Data/Balance/BalanceCalibration.asset (with the InputHash of the data it was fitted on)
solve (script/menu)     waves of every level re-solved against the calibrated model (the generator's own steps)
search (EditMode, A)    thousands of plans per cell on paper -> Plans/<Level>_<d>.txt (readable) + *.json (replayable), plans_summary.txt
replay (PlayMode, B)    the searched plans over the seeds -> plan_replay.txt, plan_records.txt (feeds the next fit)
acceptance (EditMode)   Remco's rule per difficulty on the recorded runs -> acceptance.txt
```

**What is data now.** `BalanceCalibration` (asset, written only by the apply stage): per tower type the absolute in-play
efficiency, the human and bot utilisation, the seeds, the step, the date and the `InputHash` of the inputs it was fitted
on. `TowerDefinition._ledgerEfficiency` and `LevelDefinition._balanceFieldEase` are gone (the fudge knobs the critic named).
Every stage prints the calibration's state against the current `InputHash` (towers, creatures, difficulties, upgrades,
maps with their build spots and reach multipliers, gadgets, drones, rules; the waves are left out because the solve stage
changes them on purpose). A different hash means STALE: the report says so, `PlanSearchTests` is ignored with the reason,
and the right move is measure -> fit -> apply again. Recalibrate after a mechanic or data change; never between a solve
and its verify.

**Placement is in the model.** `LevelGeometry` (built from the map by `BalanceLedger.Geometry`) samples the walking and
flight routes cell by cell with the share of creatures that pass each cell (the trunk counts fully, a lane of its own its
share), knows the gates and every buildable cell beside the road, and gives a spot's cover for a reach: the route cells in
reach, whether the defence gate's queue is in reach, and the bunched share (queue, bends). `Ledger.Capacity` now takes that
cover: a gun's time on a wave is the spawn span plus its own pass (route in reach over the creatures' speed) and, only when
it reaches the queue, the gate's hold, where a splash gun's blast meets its cap. The spotless ledger (solver, bands) uses
the ideal cover (a straight pass, the queue in reach). Per-spot modifiers are a data hook: `TerrainBalance.RangeMultiplier`
per `TerrainKind` (all 1 today) feeds `LevelGeometry.SetSpotRange`, and the spot lens, the plan search and the fit all read
the reach through it, so a raised platform that lengthens reach later needs that one table and `Tower.Range`, no new rule.

**The calibration (fit stage, `CalibrationFit`).** Every recorded run (bot or replayed plan) carries per wave: the towers
standing with their cells, the health the wave spawned, and the damage each tower type really took off that wave's
creatures (after plating and overkill). The paper side is `PlanEvaluator.CapacityByType` for the same standing set from
its real spots at efficiency one, in the human lens with the hero. Per type, efficiency = sum of realised / sum of paper
capacity, where a wave the defence out-gunned saturates (its capacity counts only up to the health there was to take,
shared in proportion); iterated to a fixed point. No mean normalisation, no field ease. Then the fit checks itself: with
the fitted numbers, the ledger's verdict on every run (every wave inside the hero cap) against what happened, mono rows
counted separately, and per cell the Spearman correlation of the analytic margin and the realised score (stars, base,
coins). Hold-out: fitted on the odd levels and judged on the even ones and back. The bar: 90% agreement (mono too) and
Spearman 0.8. The step check fits the same runs at 0.2 s and 0.05 s; a type drifting more than 10% means the coarse step
is not a calibration step. The calibration's utilisation is the rules' frame (human 0.34, bot 0.27); the efficiencies are
absolute within it.

**The yardstick (measure stage).** The obvious family reads no balance model, so it never moves between rounds:
*mono T* (a row of one owned type on its role spots; the row first, then alternating an upgrade and a tower), *naive*
(the owned types in turn on the first free spots nearest the gate, an air gun every third tower where flyers come, a
random upgrade every third purchase), *affordable* (the dearest step it can afford, coverage spots). The ledger-driven
*creative* bot stays as a reference row. Every cell is played over the seeds (default 1..3, `seeds=` in probe_arg) at a
fixed 0.2 s step; the table shows wins over seeds, the header the seeds, the step and the calibration state.

**The tester, stage A (`PlanSearch`).** Omniscient: it reads every wave, origin and route, as a replaying player learns
them. A plan (`PlanSpec`, JSON) is an ordered list of steps, each taken from its wave on as soon as the purse has the price
(an unaffordable step blocks the ones after it, as it would a player following the plan): builds (type, spot, wave),
upgrades, mines (one blast off the wave's stream), slow fields (the guns covering the cell deal more for that wave),
drone buys (the harvester opens the ore, the attack drone fires everywhere; the repair drone has no paper value) and, on
Psycho only, a sale (Remco: selling is a last resort, never required). Spots are the best few per type from the geometry,
waves are coarse. Thousands of random plans per cell are judged by `PlanEvaluator` (purse walk with the ledger's kill
timing, placement, gadget windows), the best few are improved by small changes, and the output per cell is readable:
the best spots per tower with their cover, the obvious family on the same paper, and the distinct winning plans (different
spots, mixes or timing) wave by wave, which tower where and when, and what to upgrade. Remco can follow a plan himself.

**The tester, stage B (`PlanReplayTests`).** Replays the cell's plans as scripted runs (`ProbeStrategy.Scripted`: no
decision at run time; the hero fights and wrecks are scrapped as before) over the seeds, records outcome, stars, base
health and coins, and writes its wave records to `plan_records.txt`, so the next fit learns from plans as well as bots.

**Acceptance per difficulty (`AcceptanceRules`, `BalanceAcceptanceTests`).** Remco's strategy rule holds on every
difficulty; only the margin grows. Judged on recorded runs; the one exception is explicit in the rule
(`AcceptanceRules.TutorialCells`: C1_01 Medium may stay forgiving; its obvious wins are noted, not failed).

| Difficulty | Obvious family | Best searched plan | Margin |
| --- | --- | --- | --- |
| Easy | two of the three kinds win on most seeds; nobody dies before the last third | wins every seed with 3 stars | beats every obvious run on every seed: a star more, or 25% more base and 20% more coins |
| Medium | mono rows never win; naive at most 1 in 5 with 1 star; affordable loses on most seeds | wins 4 of 5 with 2 stars; 3 distinct plans win | the obvious plans' best outcome two stars below |
| Hard | every obvious run loses and falls before the last third | wins 3 of 5 using two of: gadgets, a drone, a sale, three or more upgrades | as Hard |
| C1_01 Hard (forgiving, Remco 2026-11-07; `ForgivingHardCells`) | may win, but for the same strategy worse than on C1_01 Medium: fewer wins, fewer stars or at most 85% of the base, and a lower score | wins every seed | visibly beats every obvious run: a star more, or 25% more base |
| Psycho | as Hard | wins 2 of 5, buys a drone; a plan reaches the finale on every seed; the replayed plans differ in spots, mix or timing | as Hard |

**Data changes of round 22 (the swarm role in the data, the upgrade margin).**
- `TowerDefinition._splashTargets`: a blast hurts at most that many creatures, the nearest to the impact (Cannon 3, Flak
  4, others unlimited; the ledger's `Splash` caps at it, the Codex says "hits up to N creatures"). Against a dense ant
  swarm the rate-of-fire guns now deal more per coin; the Cannon keeps the groups, the queue and the plates.
- `BalanceRules.UpgradeMargin` 1.15 (was 1.03 in the test only): `BalanceSheet.Prices` derives every upgrade step from
  the DPS it buys so the coins to reach a level buy at least 1.15x the single-target DPS per coin of the level below
  (rounded down to 5). Build prices stay a table (Gun Turret 50, Cannon 100, Flak 120, Laser 130, Tesla 140, Railgun
  220); the steps are no longer typed: Gun Turret 50/40/65, Cannon 100/95/155, Flak 120/80/105, Laser 130/55/100.
- Removed, as hidden fudges a player cannot see: `_softTargetShare` (light guns aiming past plates: plating should make
  the Cannon wanted, not quietly steer the light guns) and `_slipCrowd` (ants climbing a crowded gate, added only to break
  the Cannon's queue feast; the splash cap does that honestly and gates keep their value). The Codex lines went with them.

**Known limits.** The repair drone, tower wear and the hero's abilities have no paper value (the fit absorbs their average
into the efficiencies). Boss mechanics are the ledger's cap-based loss, as before. The builder (solve against a frozen
calibration, re-solve only failing cells, at most two iterations, then a design question) runs the solver once per
pipeline run; per-cell script levers are a later step (see STATUS, round 22).

**First run of the pipeline (2026-11-06, seed 1).** Calibration: Cannon 0.842, Gun Turret 0.671, Laser 0.569, Flak 1.032
(hold-out agreement 90%, mono 94%, Spearman 0.53: the ranking bar is not met yet). Two fixes came out of the fit: a boss
wave is judged at cap 1 (`BalanceRules.BossWaveCap`) and the spotless ledger buys from the mean cover of the six best spots
per type (`LevelGeometry.TypicalCover`, `Ledger.TypicalSpots`), so the solver reckons with the placement the calibration was
fitted on. The obvious family loses every measured cell from C1_02 on (Medium and Hard), the searched plans hold on paper on
the Easy cells and C1_01 Medium, and 5 of 7 replayed plans won with 3 stars; from C1_02 Medium on no searched plan holds on
paper although the spotless ledger puts a competent player on the curve there: the round's open point (STATUS, round 22).

## Leaks, gates and repairs in the plan model (2026-11-06, round 22b)

The curator's decision on round 22's open question: the plan model must not judge a wave by a cap alone. A wave over
pressure one is a wave that leaks, and whether the level is lost depends on what the leak does to the base. So:

- **Leak** (`Leak`, `BalanceRules.LeakFactor`): the share of a wave over pressure one gets past the defence
  (`1 - 1/pressure`). Those creatures stand at the base's doorstep and hammer it with their structure damage
  (`WaveLoad.GroundStrike`, bosses `BossStrike`, flyers `AirStrike`; at most `AttackersPerGate` at once) for `LeakFactor`
  seconds each: the measured life of a leaker at the doorstep (the hero, the base turret and the guns in reach kill it
  eventually; none of that is in the model). Fitted on the records as base health really lost over the paper siege per second
  (`CalibrationFit.FitLeak`, line `leak`, asset `_leakFactor`); the wave records carry `BaseLost` and `GateLost` per wave
  (BotRun). A guard-based variant (damage over the hit points per second of the guns reaching the doorstep, `SpotCover.Base`,
  `LevelGeometry.Doorstep`) was tried first and dropped: see STATUS round 22b for both fits; the doorstep cover stays in the
  geometry as data for a better model.
- **A plan holds when the base survives the level** (`PlanOutcome.Holds`); its stars follow the base left by the game's
  thresholds (`LevelInput.StarFractions`), and `PlanOutcome.AnalyticScore` is the same formula as a recorded run's
  `RealisedScore` (`Leak.Score`), so the fit's Spearman compares like with like. The hero cap is still reported as the
  margin (`Margin`, `FirstBreak` = the first wave that leaks).
- **Gates wear** (`PlanEvaluator.GateLoss`): the share of the stream the guns on the walk do not kill before the queue
  (`beforeGate`, the same spots without the gate's hold) hits the trunk gates at the attackers' strike over the hold;
  the bosses at theirs. The next wave's load is built with the worn gates (`WaveLoad.Of(..., gateHealth)`), so a broken
  gate holds nothing. The spotless ledger (solver, bands) keeps full gates every wave, as before.
- **Repairs are plan actions** (Remco): a `repair` step (`PlanStep.Repair`, item `gates`) restores the trunk gates at the
  Blockade's `RepairCostPerHealth` (a wrecked gate at its build cost; `LevelInput.GateRepairCostPerHealth`,
  `GateRebuildCost`), and is taken only by the bots' rule (`PlanEvaluator.RepairPrice`): when a gate would fall in the
  coming wave, or when a hit point is cheaper to restore than to stop with the cheapest gun over the waves left;
  otherwise the step is skipped. The replay plays it the same way (`ScriptedAct`, `WorthRepair`), and a scripted plan
  repairs gates only where it says so (`Maintain` leaves gates to the plan; guns are still repaired by the bots' rule).
  A **repair drone** (`DroneStats.RepairPerSecond`) restores `rate x RepairDroneBusyShare x wave seconds` per wave, gates
  first, then the base. Its busy share is measured: the probe records the seconds a repair drone is on the field and the
  seconds it spends repairing (`RepairDroneSeconds`, `RepairDroneBusy`; line `repairbusy`, asset `_repairDroneBusyShare`).
  The search samples repair drones and gate repairs; the constructive family has a variant with a repair drone and one
  that repairs by hand before every boss. plans_summary.txt reports the best plan's repair coins and how many distinct
  winners buy a repair drone; the README lists both per plan.
- **Constructive plans** (`PlanSearch.ConstructivePlans`): what a player who read the preview does, a primary and a
  secondary type on their best spots and a priority list (upgrade the best-placed gun, add a gun, in turn), a sky gun the
  wave before the flyers, the harvester early, a mine before each boss; two orders each. They join the random sample.
- **Base armour** is a flat bonus in the game (`+100` per level): `PlayerState.BaseHealthBonus`, `LevelInput.BaseHealth`.
- **The cap trim** (`CampaignModel.CalibrateToHuman`): the last pass's rescale could lift a capped wave back over the cap
  through the creatures of earlier waves that arrive with it; a final cap-only sweep (`CapSweeps`) now runs until every
  wave fits. The composed waves carry the generator's countdown clock (`BalanceRules.FirstWaveDelay`, `WaveDelay`), so the
  model judges the stored waves exactly.
- `Tools/PlayTest/Plans/README.md` is written by the search stage: per level and difficulty the best one or two winning
  plans in words, the best spots per tower, and the repair use of each plan.

## The siege model: leaks at the gates and the doorstep (2026-11-07, round 22c)

Round 22b's leak model (a share of a wave over pressure one hammers the base for a fitted number of seconds) could not be
calibrated: hold-out agreement 35%, Spearman -0.05. The per-wave table of paper beside play (`Tools/PlayTest/calibration_waves.txt`,
written by the fit stage) showed three model errors, and the fix is the siege model in `Leak.cs`:

1. **Leakers do not vanish.** Where the paper said a wave leaked 50%, the game's towers plus hero killed 86%; where it said
   everything leaked, they still killed about half. A creature the guns do not kill on the walk stands at the defence gate's
   queue and keeps being shot until it dies or the gate falls. The paper now resolves that fight in time: the leak L (health
   not killed on the walk, `Ledger.Capacity` from the spot without the gate's hold) hammers the gate at attackers x strike x
   `SiegeFactor` (at most `AttackersPerGate` at once) while the guns that reach the queue and the hero kill it at their damage per
   second; the gate falls after gate / hammer seconds or the leak is cleared after L / (guns + hero) seconds, whichever first.
   Guards fight at full damage per second (times the calibrated efficiency): the utilisation of the pressure model is the share
   of a pass a gun spends hurting something, and a creature hammering a structure stands in reach until it dies.
2. **The gates shield the base.** On C1_01 Psycho the obvious bots lost 1000 gate health in wave 8 with the base untouched and
   the base in wave 9. What gets past the defence gate walks on to the other trunk gates (nothing shoots there but the hero,
   who falls back once his gate is gone), and only then to the base's doorstep, where the guns in reach of the last road cells
   (`SpotCover.Base`), the base's own turrets and the hero (when no gate stands) meet it. Flyers pass the gates and lay siege to
   the base directly. A fight at the base with no guard at all means the base falls, as it does in play.
3. **Bosses that survive the walk kill the base.** A boss wave inside the hero cap (total capacity above its health) still lost
   the game on C1_01 and C1_02 Medium: the boss broke two gates and the base before the towers around the first gate had worn
   it down. The siege resolves the boss as a single attacker with its own strike, at the gate, the rest and the doorstep.

**The hero is explicit data** (the curator's decision): `HeroStats.MaxHealth`, `GuardShare`, `ReviveSeconds` from the
`HeroDefinition`, beside his blade and blaster. At his post (the defence gate, as the bots play him) he kills at
`Ledger.HeroDps` and takes his guard share of the hammer instead of the gate (the gate takes 1 - GuardShare while he stands);
when the hammer has taken his health he goes down for his revive time (the clock runs on through the following waves,
`SiegeState.HeroDownLeft`). Against flyers at the base he has the blaster only. **The base turrets** are explicit data too:
`LevelInput.BaseTurret` (the level's turret as `TowerStats`), the count and level from the permanent base weapon upgrade
(`PlayerState.BaseWeaponLevel`, `PlacementController.BaseTurretSides`), at the doorstep for both layers. No hidden terms are
fitted; the one measured number is `BalanceRules.SiegeFactor`: the gate and base health the recorded runs really lost over the
paper hammer at factor one, per run capped at the structures it had (1.19 on the 427 runs; line `siege` in calibration.txt,
`_siegeFactor` on the asset, applied by the apply stage).

**The fit judges the player who really played.** The yardstick and the replays run on a fresh save: no permanent upgrades, no
perks, no base turrets. Until round 22c the fit's paper side used the campaign's expected player (research spent on tower
boosts, hero damage, gate and base bonuses), so every capacity was overstated on later levels and the efficiencies came out
low (0.53-0.58). `CampaignModel.InputForFreshPlayer` (`PlayerState.Plain`) is the fit's input now; the solver and the plan
search keep the expected player, which is the player the waves are for. For the recorded bot runs the prediction repairs the
gates by the bots' own rule (`PlanEvaluator.RepairPrice`: a gate at risk, or a hit point cheaper to restore than to stop);
a replayed plan repairs only where it says so.

**Result (`Tools/PlayTest/calibration.txt`, 427 runs, 3 seeds).** Efficiencies Cannon 0.748, Gun Turret 0.708, Laser 0.658,
Flak 0.816; siege factor 1.189; repair-drone busy share 0.967. Hold-out verdict agreement 92% (mono rows 96%), Spearman 0.84 of
the plan ranking per cell: both bars met, the asset applied. Iteration one (siege model, fresh player, guards at the pressure
model's utilisation) gave 91% / 0.71; iteration two (guards at full damage per second, the hero's guard share, the bots' repair
rule) 92% / 0.84. The 29 disagreements left are nearly all "paper survives, game lost" on C1_02 Hard mono rows (lost at wave
10-12, paper base untouched) and C1_06 Hard creative (lost at wave 7): the paper is now a little lenient where a run falls to
a late crowd, which is the side that keeps the plan search honest (a plan that holds on paper is replayed, not trusted).

**Per wave in the plan model** (`PlanEvaluator`): `SiegeState` carries the defence gate, the other trunk gates together, the
base and the hero's down time between waves; a `repair` step restores every trunk gate; the repair drone restores the defence
gate first, then the rest, then the base. `PlanWaveLine` reports the leak, the gate and base damage and whether the hero fell.
`SpotCover.Ideal` (a level without a map) reaches the doorstep as well as the queue.

**Solver note.** The human-target bisection (`CampaignModel.Bisect`) now scans the whole scale range when the bisection
misses by more than 0.5%: the plain mean is not a smooth function of one scale, because the ledger's purchase for a wave can
flip between scales a few percent apart (Quartz Flats Easy wave 9 goes from pressure 0.62 to 0.97 between scales 0.95 and
1.00, the mean from 0.72 to 0.79 around a target of 0.757). The scan cannot close such a jump; see STATUS round 22c for the
open ramp failure it leaves (C1_05 Easy 1.26x against the curve's 1.32x, C1_06 Easy 1.33x).

**Stage B's verdict on the siege model (same day, after the replay).** The 71 searched plans replayed over seeds 1-3 (213 runs,
`plan_replay.txt`) win on Easy (C1_01-C1_06, mostly 3/3 with 3 stars), on C1_01 Medium and Hard and on C1_04 Medium, and lose
every other cell from C1_02 Medium on, where they held on paper with margins of 0.2-0.5 (pressure two to five times the cap,
"cleared" by the queue guards). Refitted with those runs (633 runs) the calibration falls below its bar: hold-out agreement 84%
(mono 97%), Spearman 0.66, with 73 "paper survives, game lost" of which 45 are plans. So the siege model is right about the bot
family (clear wins and clear losses) and too lenient exactly where a plan is thin: guards at the queue at full damage per second
clear leaks that in play slip past, spread out, or break the gate before the guns are done. A third term would be the queue
guards' real share of their damage per second (iteration one at utilisation 0.34 gave 91% / 0.71 on the bots; the truth sits
between), fitted on bot and plan runs together. Until a fit passes with the plan runs included, the analytic stage is a
pre-screen and stage B is the judge: a plan counts as a winner only when it wins in the replay.

## Player-state parity, the sky-hold rule and the guard share (2026-11-07, round 22d)

**Stage A and stage B judge the same player (the curator's decision).** The plan search plans for the campaign's expected player at
a level and difficulty (`Progression.StateAt`: the research of the levels before, spent cheapest first on tower boosts, the hero, the
gates, base armour and base turrets). Until round 22d the probe bots and the replays played a fresh save. Now `PlayerState.UpgradeLevels`
carries the upgrades the paper bought by id and level, and `ProbePlayer` (PlayMode tests) writes that state into a `ProgressService`
over an in-memory save before the level loads, registered in place of the file-backed one: the probe never reads or writes
`progress.json` any more. `PlayerStateParityTests` proves that the save gives the game the same modifiers (`UpgradeEffects`) as the paper's
boosts, hero damage, gate health, base armour and base weapon level, on every cell. The yardstick keeps one fresh-save sanity cell
(C1_01 Easy, `affordable` and `naive`, reported as "on a fresh save" in acceptance.txt and the yardstick table). Each run record carries
its player (`RunRecord.FreshPlayer`, 13th field; old records read as fresh) and the fit judges each run against the player who played it.

**Finding.** Player state was not the gap. Re-measured with the expected player on the 22c waves (yardstick 426 runs, replay 213 runs,
2026-11-07 07:20 and 09:02): the searched plans still lose C1_02 Medium and Hard, C1_03 Medium and C1_05 Medium on every seed and C1_06
Medium on two of three, where they hold on paper with margins 1.0-1.15. What the expected player did change: C1_01 Psycho is won by the
obvious family (base 800 with the end-game player's armour; `affordable` 3/3, mono rows 2/3), C1_04 Hard's plan3 wins 3/3 (2.3 stars) and
the creative reference wins C1_06 Medium 3/3 (was 1/3). The Easy cells from C1_03 on are still lost by every obvious kind (the Easy rule
wants two of three to win).

**The queue-guard share (one model iteration, not applied).** `BalanceRules.QueueGuardShare` scales the damage per second the guards at
the queue and the doorstep land on a siege (`Leak.Fight`); the fit tries 0.3..1.0 (`CalibrationFit.FitGuards`, refitting the siege factor
for each) and keeps the share with the best verdict agreement (`queueguard` line, `_queueGuardShare` on the asset). On the 639 runs of
this round it picked 0.30, the bottom of the grid, for hold-out agreement 81% (mono 94%), Spearman 0.62: below the bar, so the 22c
calibration stays applied (guards at full rate). The disagreements pull both ways: lowering the guards fixes "paper survives, game lost"
on C1_02 Medium's plans (13 runs, margins 1.00-1.15) and C1_02/C1_04 Easy, and creates "paper falls, game won" on C1_01 Hard and Psycho
and C1_06 Easy (10, 9 and 14 runs). One global share cannot carry both; the analytic stage stays the pre-screen and stage B the judge.

**The purchase flip (decision 3) is fixed in `Ledger.Buy`.** The cause was not the greedy's path alone but the sky guns: on Quartz Flats Easy
wave 8 the ledger bought a second Flak step (80 coins) when the air was already held, because it was the only step that lowered the
worse pressure; a hair of health decided it, and `AirHoldings` carried the 80 coins out of every later purse (wave 9: 0.64 -> 0.97). Two
changes: sky guns are bought only while the flyers' pressure is above `BalanceRules.SkyHoldPressure` (1: until they are held; a sky gun
does nothing for the ground), and after the greedy an improvement pass (`Improve`) takes each bought tower away in turn, lets the greedy
spend the refund again and keeps the trial with the lowest worse pressure (ties to the cheaper sky spend). The plain mean of C1_05 Easy is
now 0.717 / 0.738 / 0.747 / 0.752 / 0.756 / 0.760 / 0.769 / 0.783 at scales 0.90 to 1.05 (was 0.739 -> 0.794 between 0.99 and 1.00).
`WaveLine.Purchase` names the ledger's purchase per wave (type:level x count) so a scan can see such a flip. The stored waves are re-solved
by the builder stage.

**C1_01 Hard is its own explicit rule (Remco 2026-11-07).** `AcceptanceRules.ForgivingHardCells` = C1_01 Hard: the obvious family may win,
but for the same strategy it must do worse than on C1_01 Medium (fewer wins, fewer stars or at most 85% of the base left) and score lower;
the best plan wins every seed and visibly beats every obvious run (a star more, or 25% more base). The acceptance stage passes the level's
Medium yardstick runs beside the Hard cell for this. On the 22c waves the cell fails: mono Pulse, mono Cannon and naive do exactly as well
on Hard as on Medium (3/3, 3 stars, base 600).

**The repair drone is an investment (Remco 2026-11-07).** `BalanceSheet.RepairDronePrice` = repair rate x the calibration's measured busy
share x the cheapest repair cost per hit point of the guns the selling levels offer x their mean wave spacing on Medium x
`BalanceRules.RepairDroneBreakEvenWaves` (3), rounded to 5. From the data: 8 hp/s x 0.967 x 0.30 coins/hp x 24.3 s = 56.4 coins of repairs
saved per wave; price 170 (was 120). Per level the break-even is two waves before the end (C1_02: wave 10 of 12; C1_03/C1_04: 13 of 16;
C1_05: 15 of 17; C1_06: 15-16 of 18) and a drone bought at wave 1 pays back in 2.3-3.6 waves (`Tools/PlayTest/drone_price.txt`,
`Tools/Generate/Balance/9`, `DroneLensTests.RepairDrone_PriceFollowsTheInvestmentRule`). The rate is unchanged. The generator reads the same
rule (`RepairDronePriceFromData`, 120 before any level exists). Drone prices left the `InputHash` (a price is a plan input, not a capacity
input), so the applied calibration's hash was re-stamped to the current data (551e7c40 -> 956d2750); its numbers were not touched.

**The builder moves on stage-B verdicts.** `AcceptanceRules.LeverFor` reads a cell's recorded runs by the difficulty's rule (too easy: the
obvious family wins where it must lose, or on Easy wins so comfortably that no plan can beat it; too hard: the best replayed plan misses
its bar, or on Easy fewer than two obvious kinds win; both at once: a design question) and `BalanceStages.BuildFromVerdicts` moves the
cell's target scale one step (1.15) that way, at most twice per cell over the rounds (`Tools/PlayTest/builder_iterations.txt`), then solves.
The search and the replay then judge the new waves. `Tools/PlayTest/Plans/README.md` is now written from the replay
(`BalanceStages.WriteWinningPlans`, stage `readme`): the plans that won in play, one or two per cell, with the best spots.

## Adaptive strategies, the first-attempt player and the in-play ramp (2026-11-07, round 22e)

**Play is the judge (the curator's decision 1).** The paper ramp rule (each level of a continent at least as hard as the one
before, `BalanceChecks.CheckRamp`) is a report check now: `BalanceLedgerTests` writes its warnings into `balance_ledger.txt`
and the log and no longer fails on it, because the builder moves a cell's target scale on stage-B verdicts and those moves
pull against the paper ramp (C1_03-C1_06 eased, C1_01 hardened); where they conflict stage B wins. The ramp is judged in play
instead: `AcceptanceRules.PlayMargin` scores a group of runs 0 to 1 (a win 0.5 plus half its stars share, a loss half of how far
it got), `CellMargin` is the mean of the best replayed plan's and the creative reference's margin, and `InPlayRamp` flags a level
whose margin is above the level before's by more than `BalanceRules.InPlayRampTolerance` (0.15) as a dip. The acceptance report
and the builder print the table; the builder nudges a dip harder when the verdicts leave the cell alone.

**The expected player is the first-attempt player (decision 3).** `Progression.StateAt` builds the player who first plausibly
tries a cell: the towers and perks of the levels before, their research on the path of the difficulty (`PathOf`: Easy on Easy,
Medium on Medium, Hard on Medium and Hard, Psycho on all four), plus the level itself cleared on the lower difficulties of that
path (`OwnLevelPathOf`: a Hard player beat the level on Medium first, a Psycho player on Easy, Medium and Hard). C1_01 Psycho is
now a player with the first level's research three times over (few upgrades, base 600), not the end-game player with base 800.
The level's own tower reward is not counted (the bar sells the level's towers whatever the player owns, and the PlayMode probe
owns the levels before). `ContinentEconomyTests.Psycho_IsTheFirstAttemptPlayer_NotTheEndGame` fixes the rule. The Hard and
Psycho waves were re-solved for this player (Easy and Medium are unchanged).

**Strategies instead of fixed scripts (decision 2).** A `StrategySpec` is a way of playing: per tower type the spots it takes
first (`SpotPriority`), a favoured type per threat (`ThreatWeight` over swarm, pack, plated, air, boss), the upgrade policy
(`UpgradeBias`, `TowersBeforeUpgrades`), the mine, slow-field and drone triggers (`MineCoverBelow`, `MineOnBoss`, `FieldCoverBelow`,
`HarvesterCover` and `HarvesterShare`, `RepairDroneWear`, `AttackDroneAfterTowers`), the repair rule as the bots have it, and
`AllowSell` (Psycho only for searched strategies). The creative reference bot is the point with no spot lists and every weight
one (`StrategySpec.Creative`). The threat shares of a wave come from its load (`StrategyExecutor.ThreatShares`: swarm by count,
pack by density, plated by the share a light hit loses to plating, air and boss by health share); a candidate tower's value per
coin is multiplied by the strategy's mean weight over the waves at hand.

- *Executor on paper* (`StrategyExecutor`): `PlanEvaluator.Evaluate` takes a `PlanPolicy` that adds steps at the start of every
  wave; the executor mirrors the adaptive bot's `Act` (repairs by rule, the harvester while the guns hold, the sky gun's price
  aside before a raid the sky guns cannot hold, a splash gun before the first crowd wave, gadgets and drones by the triggers, then
  the best step per coin with the wait rule). The outcome's plan is the concrete order the paper took.
- *Executor in play*: the creative bot reads the same parameters from `ProbeStrategy.Spec` (spot lists ahead of its own ranking,
  threat weights on new towers, the bias on upgrades, the triggers); `ProbeStrategy.Adaptive(spec)` plays a searched strategy.
  `BotRun.Moves` records every build, upgrade, gadget, drone and sale with its wave; the records carry them (`move` lines) and
  the README prints the winning seed's order beside the strategy's words. The acceptance rules judge the features a run used
  from its moves (`AcceptanceRules.SpecOf`), so "three upgrades, a drone, a gadget" is what really happened.
- *Search* (`StrategySearch`): samples the space (spot order from the geometry's best spots, kept or shuffled by neighbour swaps;
  a favoured type for some threats; the knobs from small grids), executes each on paper, improves the best few one parameter at a
  time, keeps the distinct winners. Two strategies are distinct when they differ in two of: the first three spots of a type,
  the favourite per threat, the timing category of the gadgets and drones (`StrategySpec.DistinctFrom`).

**Script levers (decision 4).** For a cell the scale cannot move (C1_01 Easy and Hard keep the base whole at any scale), the
builder applies, in order and one per round, `WaveScriptSpec.PlatedBias` (plated troops weigh 1.5x in every fill of the
difficulty), `Tempo` (every group's interval x0.85) and `RaidWave` (wave 3 opens with five fast, spread runners of the cheapest
troop). They live in `wave_tuning.txt` beside the scales (`WaveTuning.ReadLevers`) and the composer applies them per difficulty.
An air lever is not used on a level whose player owns no sky gun.

## The own reward, the Easy raid rule, Psycho above Hard (2026-11-07, round 22f)

**The first-attempt player owns the level's own reward (the curator's answer 1).** `Progression.StateAt` adds the level's
`RewardTowerId` and `RewardHeroPerkId` when `OwnLevelPathOf(d)` is not empty: a Hard player beat the level on Medium first, a
Psycho player on Easy, Medium and Hard, so the weapon that clear handed out is on their bar. The PlayMode probe owns the same
(`BalanceProbeTests.OwnedTowers(catalog, level, difficulty)`; the mono family gains the reward's row on those difficulties).
In Crystal Plains this touches C1_01 Hard/Psycho (the Laser) and C1_02 Hard/Psycho (the Flak); those waves were re-solved.

**The Easy raid rule (answer 3).** On Easy the first air raid comes no earlier than the wave by which the expected Easy player's
kill money (the ledger's purse above the start money, which the opening ground defence takes) covers the cheapest owned sky
gun (an air-only gun when owned, else any gun that reaches the air), plus `BalanceRules.EasyRaidMarginWaves` (1). A player who
owns no sky gun, or whose kills never cover one, gets no raid on Easy. It is a solver rule, not a script edit:
`CampaignModel.EasyRaidFloor` walks the ledger over the Easy waves as solved, `WaveScriptSpec.FirstAirWaveFloor` carries the
floor per difficulty, `WaveComposer` shifts the difficulty's raids together until the first reaches it (a raid pushed past
the last wave is gone; `FirstAirWave` and the sprinkle follow), and `BuildWaves` solves Easy again with the raids moved.
With the data of this round the floor moved Fault Line's first Easy raid from wave 3 to 10 (raids 10, 13, 16), Quartz Flats'
from 4 to 9 (9, 13) and Crysium Vein's from 8 to 9 (9, 12, 15); Shard Ridge's raids (12, 15) already sat past it. Easy kill
money is thin (income 0.8, small early waves), which is why the floor lands late; whether the start money above the opening
defence should count is a design question (below). The builder reports the rule's effect per Easy cell and makes no lever
move on a cell the rule is about to change, so the next measurement judges the rule's waves alone.

**Psycho must ask more than Hard (answer 4).** `BuildFromVerdicts` computes the human lens on the stored waves per difficulty
(`BalanceChecks.CheckHuman`'s input) and, where Psycho's plain mean is at or under Hard's, applies the next script lever to the
Psycho cell (plated, tempo, raid; counted as a move, the target not lowered) instead of a scale move. This round: Crysium Vein
Psycho (0.96 against 0.98) got plated troops x1.5.

**The acceptance's feature check was blind (a bug of round 22e, fixed).** `BalanceStages.LoadPlans` parsed the strategy files
(`*_strategy*.json`) as fixed plans with no steps; by name they shadowed the moves the runs really took, so every Hard and
Psycho cell read "uses 0 of the game's features" and "buys no drone". The strategy files are skipped now and the moves judge:
on the 22e records C1_04 Hard and C1_06 Hard pass (the winners used 13-14 upgrades, drones, and on Crysium Vein 13 gadgets),
Shard Ridge Psycho's winner used 19 upgrades, a repair drone and sales. The Psycho drone rule applies only where the level sells
a drone (Landing Site sells none). When no strategy holds on paper the search now hands the replay its best three by paper
score instead of one (the paper is pessimistic on the hard cells; the replay judges).

**The finale probe (`FinaleProbeTests`, PlayMode, explicit; built and compiling in round 22f, not yet run).** Every continent
boss on its arena field, per difficulty, against the finale player of `FinaleModel` (level 20: the planned towers on a runtime
copy of the arena level's bar, that difficulty path's research spent, the finale's money as the start purse), played by the
creative reference bot with the arena's waves at a boss health multiple the test bisects (from the data outward by 1.4 until
the verdict flips, then three halvings) to the largest the bot still beats. It writes `Tools/PlayTest/finale_probe.txt`
(`FinaleProbeReport`, pure): per boss and difficulty the holding multiple, the as-is verdict, the share and health probed.
Derived from it: the boss's data health = health x Medium's holding multiple x `BalanceRules.FinaleHoldShare` (0.85: the bot is
the reference, a human gets margin), and the boss health share per difficulty = the probed share x that difficulty's holding
multiple over Medium's, averaged over the bosses. `ContinentFinaleTests` reads the report when it exists: a ledger "too hard"
becomes a note where the probe beat the boss as the data has it, the share test judges the probe's derived shares, and a third
test holds the data health to the derived one (10 %); without the report the ledger judges as before (its two known failures
stay). The stage `finale` (`ApplyFinale.cs`, menu `Tools/Generate/Balance/10`) writes both into the assets; because health and
shares are calibration inputs the pipeline reports the calibration stale afterwards and measure, fit and apply follow. The
probe plugs into the ordinary probe through `BalanceProbeTests.ProbeOverride` (field, waves, purse, player, owned towers,
ledger view), so hero use, repairs, drones and the records come free; the plan data moved to `FinalePlan` (Game.Balance) so the
PlayMode assembly can read it (`BalanceSheet.PlannedTowers/Finales` forward to it).

## Armour as a pool, the hero's pulse and his health in the model (2026-11-08, round 24)

**Remco's armour design replaces the per-hit plating.** Armour is a pool in front of the body, shown as the blue line over
the green one. While any is left, a hit works on the armour at the weapon's *armour factor* and nothing reaches the body;
the part of a hit the armour did not need carries on to the body in full; once the armour is gone every weapon lands in
full. The factor is data on the weapon, never a name in logic: `TowerDefinition._armourFactor` (Gun Turret, Laser, Cryo,
Cryo Beam, Blockade gun, Base Turret 0.3; Flak, Tesla, Storm Rifle 0.5; Cannon, Artillery 1; Railgun 1.5),
`HeroWeaponDefinition._armourFactor` (blade 1, blaster 0.3), `HeroAbilityDefinition._armourFactor` (shockwave 1); mines
and lava balls are heavy (1). `Health(max, armor)` applies it (`Health.TakeDamage(amount, armourFactor)` returns the points
the hit was worth: armour chipped plus body hurt, which is what the probe records and what a boss's tally sees). Plating,
`PlatingFloor`, `ArmorReduction`, `IPlated` and the light guns' soft targeting are gone; the one `_armor` field remains and the
bosses that had an armour pool (Acid Empress, Crystal Warlord, Roller King, the titans) keep it with the new rule.

Species converted (Bestiary): Cragback 160 health + 60 armour (worth 360 to light fire, 220 to a shell; the old plating made
it 800 to a fresh Gun Turret and 200 to a shell), Stinger 200 + 90, Shardback 220 + 110, Bulwark 380 + 180. Kill money
follows threat as before; `BalanceRules.ArmorThreatShare` is 1.25 now (a point of armour costs a light gun more than a point
of health; 0.8 when armour soaked a share of every hit) and `PlatingReferenceHit` is gone. Rewards moved a little (Cragback
25 -> 22; the armoured bosses of later continents up by about 15%).

**The paper side is `ArmourMix`** (replaces `PlateMix`): per wave and layer group the body health H and the armour A; a weapon
of factor f pays H + A/f for a group the ledger counts as H + A, so its damage counts at the share (H + A) / (H + A/f): one
for a shell or a soft wave, 0.61 for light fire on a Cragback wave, over one for the Railgun. `Ledger.Capacity`, `Leak.Dps`
and the hero use it; the strategy threat share "plated" is the share of its damage the cheapest gun loses to the wave's
armour; the builder's `plated` lever weighs armoured troops. `ArmourPoolTests` holds the game and the paper to each other
(a light gun that kills a Cragback fires what the paper says, to within one pulse).

**The hero's pulse counts (goal 2a).** The bot already fired the shockwave at a crowd of four or at a boss
(`BalanceProbeTests.ShockwaveCrowd`); the ledger now counts it: `HeroStats.PulseDamage/PulseCooldown/PulseArmourFactor` from
the Shockwave ability, `Ledger.HeroPulseDps(level, targets)` = damage x targets / cooldown, on `BalanceRules.ShockwaveCrowd`
(4, or fewer when the wave has fewer) in a wave's window and on the attackers in front of him at a siege. The blade, the
blaster and the pulse each land at the share the group's armour lets them (`Ledger.HeroDps(level, armour, pulseTargets)`);
the blaster now takes the permanent hero damage boost like the blade.

**Spencer's survivability (goal 2b, in data).** `HeroDefinition`: health 200 -> 300, and he regenerates 6 a second from 4 s
after the last hit (`_regenPerSecond`, `_regenDelaySeconds`; `Commander.Recover`, every hit on him goes through
`Commander.Hurt`, the hero target included). The siege model carries his health from wave to wave (`SiegeState.HeroHealth`:
each fight wears it by his guard share of the hammer, a fall resets it, and the quiet of a wave - the clock less the window
less the delay - heals it at his rate, `Leak.Recover`). Before, every fight assumed a fresh hero at full health.

**Gates (goal 3, checked, nothing built).** Repairs are weighed, on paper and in play, by one rule (`PlanEvaluator.RepairPrice`,
`BalanceProbeTests.WorthRepair`): repair when the gate would fall in the coming wave, or when a hit point is cheaper to restore
than to stop with the cheapest gun step over the waves left; a plan's `repair` step goes through the same rule. In-level gate
upgrades (the Blockade's gun at level 1, more health at level 2) are bought by nobody: the paper's `TowerStats.IsGun` excludes
gates from its steps and the bots upgrade only towers. A gate-upgrade step (value: the gate's extra hold and its gun at the
queue) is the next model term if Remco wants the bot to use them; Dr. Voss's gate guidance is a later round.

The jetpack switch time (`HeroDefinition.SwitchSeconds`) is not a model term: the bots and the plans keep the hero at the
defence gate, so no switch happens in the measured runs.

All of this is in the `InputHash` (armour factors, hero pulse and regeneration, the two rules), so the round's pipeline run
starts with measure, fit and apply.

## Impact in minutes, parallel workers, the C1_03 stall (2026-11-09, round 25)

Remco, after round 24's eight hours of machine time: "als ik straks nog iets wil aanpassen wil ik niet dat het weer een hele dag
duurt; het zou fijn zijn als er dan gewoon berekend kan worden wat de impact is." Commands: Docs/COMMANDS.md "Round 25".

**The impact calculator (`BalanceStages.Impact`, stage `impact`; baseline `snapshot`).** The pipeline's expensive part is play;
its cheap part is the paper, and after round 24 the paper carries a calibration. A number tweak (an armour factor, a price, a
health) changes paper and play alike, so the calibration's per-tower efficiencies stay valid and the paper can be re-evaluated
at once. The baseline (`Tools/PlayTest/impact_baseline/paper.txt`) holds, per cell, the paper outcome of every searched strategy
(`Plans/*_strategyN.json` through `StrategyExecutor`), the fixed plans, the creative reference and the obvious family
(`PlanSearch.ObviousPlans`): holds or falls, stars, base left, fall wave, margin and score; plus the input hash, the shape hash,
the calibration date and the band. The impact stage evaluates the same plans on the current data and writes `impact.txt`: per
cell the best plan before -> after, how many plans and obvious kinds hold, the creative reference, and a flag with its reasons.
A cell is flagged when a row's verdict flipped (holds <-> falls), the best plan's stars moved, or a margin moved by the band or
more; the Psycho-above-Hard ordering per level (the best plan's margin on Psycho below the one on Hard) flags both cells when
it flips; the upgrade rule per gun (DPS per coin of a level at least `UpgradeMargin` times the level below, the worst step) is
reported before -> after. The output ends with the exact replay filter (`cells=node:difficulty,...`).

*The band* is the calibration's own error: the per-tower efficiencies fitted on the odd levels against those fitted on the even
ones, relative to the fit on all, weighted by capacity (a margin is capacity over pressure, so a relative error in the
efficiencies is the same relative error in the margin). On round 24's records: Cannon 0.79/0.61, Laser 0.72/0.68, PulseRifle
0.77/0.80, Rocket 1.03/0.83, band 0.12. A smaller move than that is inside what the calibration cannot tell apart.

*Numbers against rules.* `InputHash` fingerprints the numbers the model reads; it changes on every tweak, and the calibration
asset still reports STALE on it (PlanSearchTests is ignored then, as before). The new `ShapeHash` fingerprints the rules: the
sources of `Scripts/Balance` (the paper) and `Scripts/Combat` (how a hit lands), the field names of `BalanceRules` (not their
values), the tower and creature types, the difficulties and the level keys. The fit writes the shape into calibration.txt and
apply into the asset (`BalanceCalibration.ShapeHashValue`); the impact report says STALE only when the shape differs from the
calibrated one (or, for an asset applied before this round, from the baseline's), and then the paper is indicative and a
re-measure is the next step. A number tweak keeps the shape and gets a real impact.

*Apply bar.* Round 24's apply stage wrote a fit below the 90 % / 0.8 bar without a word. The fit now writes a `holdout` line
(agreement, mono agreement, Spearman, accepted) and `ApplyCalibration` refuses a fit below the bar unless forced
(`apply-force`), which prints a warning into the result.

**Parallel workers (`Tools/PlayTest/workers.sh`).** The robust option on this machine is N batchmode editors on clones of the
project (a Unity instance locks its project folder; clones with a copied Library start in about 45 s, compile their scripts
once, and import nothing). Each worker gets its own `probe_arg.txt` with `cells=node:difficulty:seed` units (the filter learnt
single cells and single seeds this round, in the probe and in the editor stages), runs the measure or replay test, and the
records are merged by whole cells as `measure_cells.sh`/`replay_cells.sh` always did. Three things made it fast and honest:
the probe turns the cameras off in batch mode (a frame then costs the simulation only; `-nographics` is not usable, URP logs
render errors without a device), `-job-worker-count 2` (six editors each spinning sixteen job threads ran twenty times slower),
and N from the RAM (a worker grows to about 4 GB over a long run; six of them on 32 GB paged). Each clone's product name is
"Tower defense worker wN", so its save folder is its own; the live save's MD5 is checked before and after as well.

*Determinism.* The same 54 runs (C1_01-C1_03 Hard and Psycho, seeds 1-3) were played four times headless and once in the open
editor (`--serial`, rendered): headless runs are identical to the byte across workers and repeats, and identical to the editor's
run (0 of 54 lines differ). The one run that differed (10 lines: coins left, and two Psycho outcomes) was the first parallel
attempt, six editors each spinning sixteen job threads on eight cores while the machine paged: the fixed-step probe is
deterministic unless the machine thrashes, which is what round 4d saw with editor hitches. `ModelClipPlayer.AlwaysAnimate` keeps
every rig evaluated headless so sockets stay where a drawn frame puts them; it changed no result, and stays as the faithful
setting.

**The C1_03 Easy stall (round 24's question 6), found and fixed.** Reproduced on a worker (`cells=2:0:1`, the measure test):
the creative bot's run hit the 900 s wall clock at wave 16 with the base whole, and the new `[Probe] wall clock ran out`
diagnostic (`LevelController.SpawnDiagnostic`) said: no wave spawning, held 0, alive 1: one Skitter, 4 of 13 health, at cell
(7,8) with progress 76.8, that is at the gate on the second road (cell 8,9), where the creative bot had no tower in reach
and a repair drone (8 health a second) on duty. A Skitter bites a gate for 3 every 1.1 s: the drone out-repairs it for ever,
no gun reaches it, the hero stands at the defence gate, and the level cannot end. A player with a repair drone and an
uncovered gate can meet exactly this (one straggler, the last wave spawned, nothing to do but wait), so the fix is in the
game: a creature held at a gate whose health has not dropped since it arrived gives up after `Enemy.GateStalemateSeconds`
(45 s) and burrows away (`Enemy.GaveUp` -> `EnemySpawner.Withdraw`: no reward, counted out). Any headway, or another gate,
restarts its clock, so sieges that wear a gate down are untouched. `EnemyBehaviourTests.Walker_GivesUpOnAGateItMakesNoHeadway
Against_ButBitesOnWhenTheGateWears` covers both halves; the re-run of the cell (same seed, same bot) wins 16/16 with the base
whole in 14 s of wall clock instead of timing out. The 22f/24 records of C1_03 Easy creative seeds 1 and 3 (and the three
C1_03 strategy replays that timed out) are losses that should be re-measured: `bash Tools/PlayTest/measure_cells.sh "cells=2:0"`.

**Timings (this machine: i9-9900K, 8 cores, 32 GB; the editor open beside the workers).** One headless worker plays a run in
about 15-20 s (a Hard/Psycho replay run), against about 25-30 s in the open editor and 44 s in the editor while workers ran
beside it; a worker starts in about 45 s (Unity boot and a script compile on the clone). 54 replay runs (six cells, three
seeds): editor serial 40 min 7 s; 5 workers 19 min 37 s, of which 15 min were one run sitting out the 900 s wall clock on
the Shard Ridge stall (the other four workers were done in 4 min 20 s); 6 workers with sixteen job threads each 22 min 48 s
(paging), with two job threads 1 min 57 s for the first 18 runs. With the stall fixed, a full measure (432 runs) projects to
about 25-30 min and a full replay (264 runs) to about 15-20 min on five workers, against 2 h 50 and 2 h 15 in round 24; a
tweak's impact table takes 6 s (snapshot 6 s, three fits included) and the replay of its flagged cells a few minutes.

## Approach cover, the blocking hero and the gate-upgrade step (2026-11-09, round 26)

**What the records say about the paper (the residuals).** The fit's disagreements are not noise around a constant: they sit in
cells. Read with the fit's wave table (`calibration_waves.txt`, one line per recorded wave: paper pressures, walk and queue
guards, siege damage, beside the game's damage by type and gate and base losses) and two traced runs. (1) The paper counted a gun's
whole cover as walk cover; a gun behind the defence gate (C1_02's strategies at (5,7)-(7,6) behind the gate at road index 11) kills
nothing before the gate, and a gun on the long stretch behind a *fallen* gate (C1_01's rows at (5,14), index 40-72, behind the gate
at index 14) kills everything, which the siege model, where "nothing shoots at the rest gates but the hero", could not see. (2) The
hero is a blocker in play (`HeroTarget.BlocksGround`): the creatures that reach him bite him for their full blockade damage, so at a
besieged gate he falls in about 20 s (300 health against five biters) and is down for 120 s, three or four waves; the paper's
`GuardShare` 0.4 gave him 54 s and the gate the rest. (3) Per tower type the realised share of the nominal rate on the paper-leaky
waves is flat over creature health (Gun Turret about 0.40, Laser 0.30, Cannon 0.70-0.98 with its splash), except small creatures
(Gun Turret 0.23 under 20 health): the per-type efficiency is the right shape of calibration; what varies between cells is where
the kills happen, not how many.

**Terms built.** `SpotCover.ApproachPass` and `BehindPass` (the geometry marks every route sample past the defence gate on its road;
a lane that does not pass the gate is all approach; the spotless ideal is all approach): `Leak.AddTower` credits the walk with the
gun's capacity times its approach share, puts the behind share into `SiegeGuards.BehindStream/BehindSingle`, and `Leak.Resolve`
takes that off what got past the defence gate before the rest gates. `HeroStats.Blocks` (from `HeroDefinition.Flies`): while a
blocking hero stands the whole hammer lands on him and none on the gate (`HammerShare` 1); a flying hero absorbs his `GuardShare`.
Two terms tried and dropped, with the numbers so the next round does not repeat them: a kill-waste factor meanHealth/(meanHealth+hit)
on single-target guns (81 % / 0.67, the paper more pessimistic) and a siege clock (fights capped at the wave's length, the rest
standing into the next wave: 60 % / 0.73, 234 "paper survives, game lost": the game's sieges are deadlier than a clocked model).

**Fit after the terms:** 81 % / 0.68 (mono 93 %) against 82 % / 0.65 before on the same 696 runs: the ranking a little better, the
agreement the same. Applied by override as the best available (the pipeline's `apply` refuses it; `apply-force` prints the warning).
The remaining error is the walk's quality by geometry: C1_05's early gate with fast swarms leaks where the paper holds, C1_04's and
C1_06's late trunk gates kill on the long walk where the paper leaks. The candidates are a fitted per-level walk factor (cheap,
honest, not a mechanic) or a route-sampled kill model gun by gun along the road (the physical answer, slower). Remco decides.

**Light fire 0.2 (Remco, round 26):** Gun Turret, Laser, Cryo, Cryo Beam, Blockade gun, Base Turret and the hero's blaster 0.3 -> 0.2
(`ProjectGenerator.LightFireArmourFactor`, `ApplyRoles.Run`). A Cragback (160 + 60) is now worth 460 to light fire (was 360) and 220 to
a shell. The impact on the paper (`impact.txt`): 8 cells flagged, the plans that lean on Gun Turrets against armour fall (C1_03
Medium 5/5 -> 0/5, C1_01 Medium's one holding plan), C1_04 Easy keeps its wins at 2 stars, C1_05/C1_06 Medium margins up with the
re-solved waves. The play verdict comes from the round-26 chain (measure, replay) on the new data.

**The gate-upgrade step.** The Blockade's levels are in the model as a tower (`LevelInput.Gate`, `TowerLevelStats.HealthBonus`): level 1
a gun at the queue (6 damage at 0.45 s, reach 3.9, armour factor 0.2) and +300 health for 90, level 2 (11 at 0.35 s, +500) for 150.
`SiegeState.GateLevel/GateBonus`: the bonus lands on the standing defence gate, a repair restores the upgraded full, the gun counts in
the pressure (`Ledger.GateCapacity` from the gate's cell) and as a queue guard (`Leak.AddGate`). The step (`PlanStep.GateUpgrade`,
"gate") is weighed by the rule every other step gets: the cover gain of the gun per coin, with the health bonus credited at the
repair price of that much health (`PlanEvaluator.GateUpgradeWorth`), times the strategy's `UpgradeBias` and its `GateUpgradeBias`
(0 never; sampled, mutated and counted as a distinct timing by the search). In play the creative and adaptive bots weigh the defence
gate's next level beside the gun upgrades with the same ledger, scripted plans take a "gate" step, and the move is recorded as
`gate`; the acceptance rules count a gate upgrade as one of the game's features a Hard or Psycho plan may use.

## The route-sampled kill model (2026-11-09, round 27)

The curator's answer to round 26's question 1: not a fitted per-level walk factor but a physical model of *where* a gun kills,
reusable on every continent. `RouteKill` (Game.Balance) replaces the spawn-span-plus-pass credit of `Ledger.Capacity` in the
siege's walk; the pressure lens (purchases, the solver, the hero-cap margin) keeps that capacity, so a refit never shifts the
scale the waves were solved on.

- **Routes.** `LevelGeometry.GroundRoutes`/`AirRoutes` hold every road and flight path cell by cell in travel order with the
  index of the defence gate on it (`Route.GateIndex`; a lane that never meets the gate, and every flyer, is all approach);
  `InReach(spot, reach, route)` gives the cells of a route a gun covers (cached). `SpotCover.Spot` remembers the spot a cover
  came from, so `Leak.AddTower` can hand the gun to the route model; a typical or ideal cover (no spot) keeps the old credit.
- **Groups.** `WaveLoad.Groups` carries each group of a wave (count, body health and armour per creature, speed, path, source,
  boss, flyer). Path spawns start at their road's first cell, nest and pool creatures where the nests join the main road
  (the mean of `LevelShape.NestRemaining`), an entrance boss where the entrance meets it, flyers on their flight path or, on a
  level without one (all of Crystal Plains), along the road itself. Carried creatures of the wave before scale the stream up.
- **The walk.** For every group and gun: the gun's rate (dps x efficiency x `Utilisation` x `RouteFire` x layer factor x the
  group's armour share x overkill x splash at the stream's density) times the seconds it fires at one creature: the creature's
  time in reach (cells in reach over its speed), but at most one over the arrival rate of the creatures still alive where the gun
  first sees them (several in reach at once share the gun). That damage is spread over the gun's cells; marching the route, a
  creature dies where the accumulated damage reaches its body plus armour, and what is alive at the defence gate is the leak
  the siege resolves. Two passes: the second knows where the first's kills thinned the stream (the sharing and the splash
  density). The cells behind the gate meet only what got past it (`BehindStream/BehindSingle`). The guns' per-type account
  (`SiegeGuards.Dealt`, plus each type's share of the queue and doorstep kills, `SiegeLine.QueueGuardKills/DoorGuardKills`
  by `QueueByType/DoorByType`) is the paper side the fit reads.
- **One fitted number, `BalanceRules.RouteFire`.** The per-type efficiencies are still fitted on the pressure lens (so the
  purchases and the solver keep their scale); the route model's scale is one factor chosen with the queue-guard share on a grid
  (`CalibrationFit.FitGuards`: fire 1..8 x share 0.3..1, the pair with the best verdict agreement, ties by the ranking), written
  as `routefire` into calibration.txt and `_routeFire` on the asset. A ratio fit of the factor was tried first and dropped: the
  account is dominated by the queue fights, so the ratio starved the walk (fire 0.53, 75 % / 0.70).
- **A fit bug found on the way.** `FitSiege` added to its sums on every call, so the siege factor was a running ratio over the
  whole guard-share grid and depended on the grid's order (round 22d onward). The sums are per call now.
- **Tried and dropped (numbers so the next round does not repeat them):** the hero as a two-biter blocker (the gate taking the
  rest of the hammer while he stands: the records show gates wearing while he stands, but the verdicts fell from 79 % to 77 % and
  the ranking from 0.77 to 0.73); a per-type efficiency fitted on the route account itself (Rocket pinned at 4.0 because flyers had
  no route yet, efficiencies 0.35 that would have doubled every pressure).

**Fit (675 runs, 243 replayed plans):** Cannon 0.737, Laser 0.693, PulseRifle 0.764, Rocket 0.878; route fire 1.0, queue guard
0.70, siege 0.912. Self check 83 % (mono 97 %), Spearman 0.78; **hold-out 78 % (mono 91 %), Spearman 0.76** against round 26's
82 % / 0.69 on the same shape of data. The bar (90 % / 0.8) is not met; the ranking is a good deal better and the agreement a
little worse, the paper now lenient (89 "paper survives, game lost": C1_02 Easy/Medium and C1_05 strategies where the gates
wear down in play while the paper's blocking hero clears the queue) rather than pessimistic. Applied by override as the better
of the two by the distance to the bar and because it is the physical model the next continents need. Cost: the paper is slower
(the impact baseline 149 s against 6 s; a plan search per cell is minutes, not seconds).

**Next terms, if the bar is wanted:** the hero's realised kill rate at a besieged gate (the records say he falls at wave 7 on
C1_02 Easy with the gates losing 400-1200 a wave; the paper keeps him up), and the attackers at a gate (the game has no cap on
how many bite at once; `AttackersPerGate` 5 is the model's).

**Round 27 passes on continent 1 (2026-11-09, the route model applied).** Two tuning passes by levers in `wave_tuning.txt`, each
re-solved and judged in play on the workers (pass 1: 14 cells re-measured and replayed in 22 min; pass 2: 6 cells with a fresh
strategy search for C1_02 Psycho, C1_03 Hard, C1_05 Medium/Hard, 9 min). Pass 1: C1_01 Hard/Psycho tempo x0.85, C1_02 Easy scale
0.87 -> 0.757, C1_02 Psycho plated x1.5, C1_03 Hard scale 1.15 -> 1, C1_03 Psycho tempo x0.85, C1_06 Psycho scale 0.87 -> 0.757.
Pass 2: C1_01 Hard and Psycho scale 1.15 -> 1.32 (the mono rows still won with the base whole at 1.15 after plated and tempo),
C1_01 Psycho a raid at wave 3, C1_02 Psycho tempo x0.85, C1_03 Hard scale 1 -> 0.87. In-play margins after pass 2 (E/M/H/P):
C1_01 1.00/1.00/1.00/0.83; C1_02 1.00/0.92/1.00/0.53; C1_03 1.00/1.00/0.92/1.00; C1_04 1.00/1.00/0.75/0.63; C1_05 0.92/0.60/0.64/0.34;
C1_06 1.00/0.89/0.86/0.65. Acceptance stays 5/24: C1_02 Easy's plans win 3/3 now (was 0/3) but the obvious rows win with the base
whole too (the Easy margin rule); C1_03 Hard's new strategies win (0.43 -> 0.92) while the mono rows still reach wave 13 of 16; C1_01
Hard's mono rows finally do worse than on Medium at scale 1.32 but still win 3 stars with the base whole; C1_06 Psycho 0.39 -> 0.65.
The swarm lever (`swarm`, unarmoured troops no heavier than twice the lightest weighted x1.5) is built and in the builder's order
after plated; no cell got it yet. The refit on the pass-2 records (687 runs): Cannon 0.749, Laser 0.705, PulseRifle 0.790, Rocket
0.901; hold-out 77 % (mono 92 %), Spearman 0.78; self 81 % / 0.83; the grid chose route fire 4.0, queue guard 0.8, siege 2.05 where the
675-run fit chose 1.0 / 0.7 / 0.91: the fire and the siege factor are not well identified jointly (a design question).

**Finale probe (goal 3, run twice on the clones, 3.5 and 6 min).** With the health range widened to x16 the creative bot at the
finale player beats every continent boss far above its data: Cinder Matriarch x14.8 on Easy and Hard (x2.1 Medium), Frost Warden
x8-11, the Sealed One x12-15 (x6.1 Psycho), the Shattered x10-15, Gravemaw x5.4/2.4/7.2/4.4, Rotbloom x2.4/1.7/2.3/1.0. The holds are
not monotonic over the difficulties (the finale player's research and money grow faster than the boss share), and applying the
`finale` stage would multiply most boss health by 5-12x, so it was **not** applied; Remco decides what a continent finale should ask.
