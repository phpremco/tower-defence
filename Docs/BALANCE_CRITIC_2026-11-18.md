# Balance critic 3, 2026-11-18 (rounds 79 to 81, HEAD 9f398792)

Read-only. Nothing was run in Unity and no project file was touched. Sources: committed code under
`Assets/_Project/Scripts/Balance`, `Docs/WAVE_GRAMMAR.md`, STATUS rounds 79 to 81, `Tools/PlayTest/wave_tuning.txt`,
`scale_refit_r81.txt`, `wave_ledger_r81_qf.txt`, `wave_ledger_r80b_final.txt`, `pressure_r80_final_records.txt`,
`pressure_r81_final_records.txt`, the wave assets at HEAD and in `Tools/PlayTest/r80_waves`. Inferences are marked.

## Verdicts in short

| Question | Verdict |
| --- | --- |
| 1. Too many interacting rules? | **Yes.** Four of the five new rules repair two properties of the solver itself, not the game. Fix those two properties and the rules can go. |
| 2. Builder scales a per-level exception? | **Yes, and worse:** the refit anchors every cell to the previous round's waves, so the model no longer sets a level's difficulty at all. Level 7 has no anchor. |
| 3. Measurement trustworthy? | **For Easy/Medium and for large moves, yes. For one-seed moves on Hard/Psycho, no.** The skilled picks are three data changes stale, and the bot's build flips from wave 1 on a 10 % health change. |
| 4. Quartz Flats Hard/Psycho | **The wave 15 swarm** (x6.07 / x5.62, 16 300 / 27 100 health), sized by an unbanded variant solve against a splash defence the bot does not hold. |
| 5. Ready for Remco? | **Easy/Medium: playable now, probably too soft. Hard/Psycho: not ready.** Two to three focused rounds if no new rules are added. |

## 1. Rule growth: what the rules are really repairing

Rounds 80 to 81 added: the raid cap, "a surprise lifts no wave", its 5 % relief bound, the rear-rift guard gun, the Easy
purse rule for rift flyers, raids out of the plain mean, and two scale refits. `BalanceRules.cs` now has about 95 public
knobs; `wave_tuning.txt` has 58 per-cell lines of seven kinds (scale, armourgrowth, plated, swarm, raid, tempo,
bosscrowd); `GraceWindows.cs` gains allowances every round (four in 79, two in 81).

Most of the new rules trace to two properties of the solver:

**A. Pressure is not monotone in creature health**, because the ledger's player re-buys its defence for each trial
multiplier. The code says so in three places:
- `WaveSolver.BisectOn` (comment above it, WaveSolver.cs ~line 205): a raid has two or more crossings, so the raid cap
  walks up in 5 % steps.
- `CampaignModel.CapScale` comment: "the pressure is not monotone near the bottom".
- `CampaignModel.Bisect` comment: "The mean is not monotone in the scale", with a 160-step scan as fallback.
Symptoms recorded in STATUS: Quartz Flats wave 13 at x1.18 or x2.46 "by a hair of the purse"; Fault Line Psycho's cliff
(ground x0.82 at scale 0.50, x1.10 at 0.51); Fault Line Hard waves at x0.12 when "lowest crossing" was tried on ground
waves.

**B. One common factor holds the plain mean on the curve** (`CalibrateToHuman`, the `Bisect` over one scale for all
uncapped waves). So anything that lowers one wave raises all the others. That single coupling produced: "surprise waves
on the hero cap lift every other wave 8 %" (hence `SurprisesLiftNoWave`), then levels 6 to 9 % under the curve (hence
`SurpriseReliefMost`), then "capped raids push ground waves up 20 %" (hence raids out of `PlainMean`), then ground waves
moving up to 60 % (hence the fourteen refitted scales), then Quartz Flats' weight "moved to the back".

Each rule is locally reasonable. Together they are a chain where each repairs the side effect of the one before. That is
fitting the model to itself, with the bot as referee. None of it would transfer to level 7, because the things being
tuned (which wave lands on which crossing, what the mean redistributes) are accidents of each level's script.

**Simplification (two changes, then delete):**
1. Fix the reference player's purchases per wave from the purse and the roster, not from the trial multiplier. Then
   pressure is monotone in health, a plain bisection works everywhere, and the raid cap, the crossing walk, `CapScale`'s
   step-down and the scan fallback are not needed.
2. Trim each wave to its own target instead of holding a level mean with one factor. A capped wave is then just lower;
   nothing is redistributed. `SurprisesLiftNoWave`, `SurpriseReliefMost`, the second "calm" solve and "raids out of the
   mean" all disappear. The level's overall height becomes one explicit number (section 2).

Keep as they are (they are design rules about the game, not model repairs): the grammar, `MinSurpriseGroup`, spawn-point
roles and authored schedules, `FlyersAfterBounties`, the Easy purse rule.

## 2. Builder scales: what a factor-only design looks like

The scales at HEAD (`wave_tuning.txt`), Easy / Medium / Hard / Psycho:

| Level | E | M | H | P |
| --- | --- | --- | --- | --- |
| Landing Site | 0.8 | 0.757 | 1 | 0.7 |
| Glass Meadows | 0.658 | 1 | 1 | 0.757 |
| Shard Ridge | 0.83 | 0.72 | 0.62 | 0.48 |
| Fault Line | 0.84 | 1.13 | 0.9 | 0.51 |
| Quartz Flats | 0.66 | 0.67 | 0.73 | 0.44 |
| Crysium Vein | 0.67 | 0.75 | 0.63 | 0.58 |
| Geometric mean | 0.74 | 0.82 | 0.80 | 0.57 |
| Residual around the mean | 0.89 to 1.14 | 0.81 to 1.37 | 0.78 to 1.25 | 0.78 to 1.34 |

Three readings:
- **The model's level is off by a quarter to almost a half, systematically.** 22 of 24 scales are at or under 1. Dividing
  `HumanSurplus` (1.4 / 1.15 / 0.95 / 0.8) by the means gives the surplus actually in use: about 1.9 / 1.4 / 1.2 / 1.4.
  Psycho is run with more surplus than Hard: the model's idea of Psycho (count x1.65, triple start money) is wrong in a
  way the scale hides.
- **The per-level residual is plus or minus 20 to 35 %.** The curve's gate is 6 % (`HumanTargetTolerance`). A gate six
  times tighter than the hand correction on its target checks nothing.
- **`ScaleRefit.cs` fits each scale so the ground waves sum to round 80's stored waves again** (its own doc comment: "so
  the cell stays the level that was measured"). So when a rule changes the model, the scale is moved to cancel it. The
  stored difficulty is inherited from the last measured state; the model only shapes it. For level 7 there is no previous
  state, so the generator would emit whatever the uncorrected model says, 25 to 50 % too hard.

So yes: it is a per-level exception, 24 of them, and it is the thing that actually sets difficulty.

**Factor-only design:**
1. Fold the per-difficulty means into the rules (one number per difficulty, either `HumanSurplus` or a new
   `DifficultyOffset`). All 24 scales start at 1.
2. Admit that the ledger cannot predict a level to better than about 25 %, and make the last stage of the generator a
   **closed-loop solve of one scalar per cell against the probe**: search the scale at which the reference player's
   break-even multiplier hits a per-difficulty target (for example: sensible breaks at x1.12 on Medium, skilled at x1.12
   on Hard, skilled at x1.0 to x1.05 on Psycho). Same procedure and same targets for every level, automated, about 15
   runs per step. That is a rule, not an exception, and it runs unchanged for level 7.
3. Delete `ScaleRefit` (refit to previous waves), the hand scale lines, and the 6 % curve gate; replace the gate with
   "the solved scalar lies within x0.7 to x1.4 of 1", which flags a level whose map or script the model misreads.
4. Treat the other per-cell knobs (armourgrowth, plated, swarm, raid, tempo, bosscrowd: 34 lines) the same way: either
   a per-difficulty rule or gone.

## 3. Is the measurement good enough to steer by?

What holds: the probe is deterministic and stamped; Easy and Medium results (358 of 360) and large moves (Glass Meadows
Psycho `3 2 0 0 0` to `3 3 3 3 0`) are real.

What does not:
- **The skilled picks are stale.** `skilled_picks.txt` was last written in round 79 (git log), on data `c75b37f7`; the
  data changed in 80, 80b and 81. Round 79 itself showed what that costs: Quartz Flats Psycho read 0 of 3 with the old
  pick and 3 of 3 after a refresh. Round 81's "Quartz Flats Psycho 3 -> 1" was measured without a refresh.
- **The "frozen" bot is chaotic in the data.** Same strategy (`Level_C1_05_3_strategy2 adaptive`), seed 2, x1: in round
  80 its wave 1 buys are four Cannons; in round 81 a Pulse Rifle with two upgrades replaces one and it never upgrades a
  Cannon again. By wave 14 it holds Cannons at levels 3, 3, 3, 2, 2 (round 80) against 1, 1 (round 81). A 10 % softer
  opening did that. So a cell flipping is often "the bot took another build", as STATUS already says for four rows, and
  that cannot be told apart from "the level got harder" at three seeds.
- **Rows are not monotone**: Quartz Flats Psycho skilled `3 0 1 1 0`, Fault Line Psycho sensible `2 2 3 2 0`. A cell
  score read at x1 alone is one draw from that.
- **Three seeds are not three samples** (critic 2, still true): seeds mostly replay the same game.
- **Circularity**: the solver's reference player is fitted to the bot's recorded runs (`CalibrationFit`, and WAVE_GRAMMAR
  on the guard gun: "its factors are fitted to the probe bot's recorded runs"), and the same bot then judges the output.
- **Gate erosion**: acceptance reads 7 PASS / 17 FAIL and is "not analysed"; allowances are written each round so the
  grace gate passes; one EditMode test is red at the commit.

Minimum changes:
1. Skilled rung = best of the cell's searched strategies **on today's data**, re-picked mechanically in every data batch
   (the 309-run picks batch), and reported next to the frozen-pick row. Search new strategies when no pick wins.
2. Score a cell by its **break-even multiplier** (highest multiple at which 2 of 3 seeds win, on 0.8 / 0.9 / 1 / 1.12 /
   1.25) and flag non-monotone rows as "fragile", instead of counting seeds at x1.
3. Print, per lost cell, the **wave that did the damage and the standing guns by type** (done by hand for section 4; it
   took one script over the records). Without it every round guesses the cause.
4. Either repair or delete the acceptance gate. A gate that is red on 17 of 24 cells steers nothing.

## 4. Quartz Flats Hard/Psycho, waves 16 and 17: the wave 15 swarm

Single most likely cause: **wave 15 is a swarm that the variant solve sized at x6.07 (Hard) and x5.62 (Psycho)**, and
the skilled rung's build has almost no splash when it arrives. The survivors wreck the gates and take the base during
wave 16; the raid and the boss finish what is left.

Evidence:
- `wave_ledger_r81_qf.txt`: Hard w15 `W3 x6.072 (solver x2.686) hp 16285 n 176`; Psycho w15 `x5.618 hp 27125 n 221`.
  The next largest waves are w14 (8 310 / 13 040) and the boss finale w17 (10 374 / 15 049). The cell's median
  multiplier is 1.74 / 1.61, so wave 15 is 3.5 times the median.
- Why x6: on Medium wave 15 is an **air raid** (`wave_ledger_r80b_final.txt`, C1_05 Medium `w15 A2 x1.127`); on Hard and
  Psycho it is a swarm. `WaveSolver.SolveVariant` sees a changed kind and solves it alone between `MinHealth` 0.4 and
  `MaxHealth` 60, with **no trend band** (the `MaxRise` 1.3 / `MaxDrop` 0.7 band exists only in `Solve`). The ledger's
  pressure on a dense swarm rises slowly with health (it assumes the splash defence is there), so reaching the Hard
  target takes x6. Medium's own swarm (w11, 126 creatures) is x2.21 with 3 113 health: Hard's has five times the health.
  It has been there since round 79 (x5.74 / x5.44), which is why this cell has been red for three rounds.
- In play (`pressure_r8x_final_records.txt`, field 22: health spawned : advance-weighted health), skilled, x1:

  | Run | Cannons held at wave 14 | Wave 15 advance share | Result |
  | --- | --- | --- | --- |
  | Psycho, round 80, seeds 1-3 | levels 3,3,3,2,1 or 3,3,3,2,2 | 16 %, 9 %, 17 % | won, base 883 to 900 of 900 |
  | Psycho, round 81, seeds 2-3 | 1,1 | 51 %, 50 % | base 900 -> 0 **in wave 16** |
  | Psycho, round 81, seed 1 | 2,1 | 35 % | won, base 414 |
  | Hard, round 80, seeds 1-3 | 2,1 / 2,1 / 1,1 | 40 %, 32 %, 44 % | lost, won (576), lost |
  | Hard, round 81, seeds 1-3 | 1,1 | 53 %, 47 %, 49 % | lost in 16, 17, 16 |

- In the two Psycho losses the base goes from full to zero in wave 16 while wave 16's own creatures record no health at
  the base; the killers are wave 15's leftovers (inference from the record layout).
- The raid of wave 16 is softer than in round 80 (x1.40 against x1.48; x1.30 against x1.48), so it is not the raid.

STATUS's reading ("the weight moved to the back, wave 14 +19 %") is a contributor on Hard (base lost 124 to 488 in wave
14), but the outcome tracks the Cannons, not wave 14.

Fix by rule, not for the cell: give `SolveVariant` the same band as `Solve` (a changed wave may not exceed, say, 1.3
times the highest of its three ground neighbours), or cap any wave at about 2.5 times the level's median multiplier.
The same pattern exists elsewhere and is not yet biting: Crysium Vein w16 at x6.36 on Psycho (4.0 times its median),
Quartz Flats Easy w15 at x4.36. Then refresh the picks before reading the cell.

## 5. Readiness

What must be true before asking Remco for a "good balance" playtest:
1. The suite is green and every gate that is kept means something (acceptance repaired or removed; no new allowances).
2. Each difficulty meets one stated margin target on every level, measured with fresh picks: Easy always won; Medium won
   by the sensible rung with a break-even between x1.1 and x1.25; Hard won by the skilled rung, sensible break-even
   near x0.9 to x1; Psycho skilled break-even at x1 or just above.
3. Hard is harder than Medium on every level.
4. No wave above about 2.5 times its level's median multiplier unless authored; all-or-nothing losses under about 15 %.
5. The scales come from a procedure, not from a refit to the previous round.

Where it stands:
- **Easy/Medium:** 358 of 360. But the sensible rung wins all 15 runs, x1.25 included, on all six Medium cells, and the
  skilled rung on four. Medium has more than 25 % of slack everywhere. Remco's "Shard Ridge Medium's boss feels quite
  weak" agrees. Medium is not broken; it is probably flat.
- **Hard:** fails item 3 on Fault Line (15 of 15 on both rungs, same as Medium) and Crysium Vein (sensible 15 of 15);
  Quartz Flats 0 of 3; sensible ranges from 15 of 15 to 3 of 15 across levels. Not even.
- **Psycho:** five of six cells fine for the skilled rung; Quartz Flats open.
- All-or-nothing 25 % of losses; one red test; picks stale.

Distance: with the wave-15 band and a pick refresh, Quartz Flats is likely one round. Evening out Hard and tightening
Medium is one round if done by a closed-loop scalar (section 2), several if done by more rules. The structural work
(sections 1 and 2) is what makes level 7 possible and is the larger item.

One suggestion for the coordinator: the bot cannot tell whether Medium is fun or flat. A 20-minute Medium run of levels
1 to 3 by Remco would answer that now and costs him less than another three rounds cost here. His own rule is "only when
balance is good", so this is his call.

## Recommendations, in order

1. Band or cap variant-solved waves (Quartz Flats w15, Crysium Vein w16), refresh the skilled picks, re-measure. No other
   rule this round.
2. Stop refitting scales to the previous waves. Fold the per-difficulty means into the rules and replace the 24 scales
   with a closed-loop scalar per cell against a per-difficulty break-even target.
3. Make the ledger's purchases independent of the trial multiplier and trim per wave instead of by a level mean; then
   delete the raid cap walk, `SurprisesLiftNoWave`, `SurpriseReliefMost`, the calm solve and the guard-gun booking.
4. Report break-even multipliers and the killer wave per lost cell; repair or delete the acceptance gate.
5. Tighten Medium (target a sensible break-even of x1.1 to x1.25) and restore Hard above Medium on Fault Line and
   Crysium Vein, through the per-difficulty targets, not per level.

## Caveats

- Nothing was run. The wave 15 finding rests on the committed ledger printouts and records; that the swarm's survivors
  (not the raid) take the base in wave 16 is inferred from the per-wave record fields.
- Whether a monotone ledger player (section 1) is cheap to build was not checked in `Ledger.cs`.
- I did not check whether the bot's Cannon-or-Rifle choice at wave 1 comes from the preview, the purse or the pick.
- The break-even targets in sections 2 and 5 are proposals, not measured values.
