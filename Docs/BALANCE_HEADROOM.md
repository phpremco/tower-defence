# BALANCE_HEADROOM.md

Balance method, stage 1 (2026-11-16): the test of the headroom metric that Docs/BALANCE_METHOD_REVIEW.md proposes.
Test and tool code only. No gameplay, data or old-pipeline change. How to run it: Docs/COMMANDS.md, "Balance stage 1: headroom".

## Verdict

**Stage 1 did not pass its stop rules. Do not start Stage 2 as written.**

| Stop rule | Result | Numbers |
| --- | --- | --- |
| 1. Winning is monotone in the health multiple | **Fails** | 17 of 78 scanned units have more than one win/lose crossover (sensible 8 of 24, plain-save sensible 3 of 8, mono rows 3 of 11, careless 2 of 12, skilled 1 of 23). The flip-back zone is 5 % wide in the mildest cases and 41 % in the widest. |
| 2. H repeats within 5 % | **Fails across seeds, holds for reruns** | Same seed, other worker: 8 of 8 edges identical, 54 of 54 runs the same result and base health. Across seeds 1-3: within 5 % in 3 of 20 cell-rungs. Sensible: 3-13 % on Landing Site, 13-221 % on Shard Ridge. Skilled: 0-66 %. Careless: 79-622 %. |
| 3. Agrees with known facts | Mostly | See "Sanity". The ladder order and the Easy-to-Psycho order do not hold. |
| 4. Rescale lands on target | Holds per seed | 6 of 6 within -1.7 to +0.8 % of the per-seed target. The seed spread of the cell stays (see "Rescale"). |

The review's stop rule: "if 1 or 2 fails, the metric is not usable as defined and this proposal needs rework before
anything else is built on it." Both fail. What works and what would need to change is under "What this means".

## What was measured

- **H** = the largest uniform multiple of every creature's health at which a fixed reference player still wins the level,
  found by bisection in the real game: from x1.0 outward in steps of 1.5 until the result flips, then the geometric middle
  until first loss / last win is within 3 %. Bounds 0.1 and 16. H below is the last multiple won.
- The level is played as it is (own waves, purse, the cell's expected player from `Progression`). Only a runtime copy of
  the `LevelDefinition` carries the multiple in its enemy health scale. Kill money does not depend on health.
- **Reference players**, none of which reads the balance model to decide:
  - *careless*: the existing `naive` bot (owned types in turn on the spots nearest the gate, a random upgrade now and then).
  - *sensible*: the `affordable` buyer on role spots, plus one new rule: when the wave being fought or the next one shows
    armour or flyers and no standing tower has the matching role (`CombatRules.Answers`, the round 67 role table), the
    cheapest owned counter is bought first and the purse is kept for it.
  - *skilled*: the build order that the cell's best replayed plan really took (`Tools/PlayTest/plan_records.txt`: most seeds
    won, then stars, then base health), replayed as a fixed script.
  - *mono rows*: one tower type each (seed 1). *fresh*: the sensible player on a plain save (seed 1).
- Levels: Landing Site (C1_01) and Shard Ridge (C1_03), four difficulties, seeds 1-3, deterministic probe at 0.2 s steps.
- After each bisection eight more multiples around the edge were played (0.7 to 1.45 times the edge): the monotonicity scan.
- Per run the records keep, per wave, how far its furthest creature got along its road (0 to 1), base and gate damage.

## The H table

Median over seeds 1-3, range in brackets. `<0.10` = lost at every multiple down to a tenth of the health.

| Cell | Careless | Sensible | Skilled | Sensible, plain save |
| --- | --- | --- | --- | --- |
| Landing Site Easy | 1.62 (0.62-1.66) | 1.16 (1.16-1.19) | 1.75 (1.62-1.84) | 1.19 |
| Landing Site Medium | 1.54 (0.54-1.54) | 1.22 (1.19-1.26) | 1.26 (<0.10-1.36) | 1.26 |
| Landing Site Hard | 1.00 (0.14-1.03) | 0.84 (0.82-0.88) | 1.14 (all seeds) | 0.82 |
| Landing Site Psycho | 0.52 (0.46-0.82) | 0.53 (0.50-0.57) | 1.16 (0.97-1.22) | 0.54 |
| Shard Ridge Easy | <0.10 | 0.41 (0.34-1.08) | 1.26 (1.26-2.09) | 0.34 |
| Shard Ridge Medium | <0.10 | 0.84 (0.50-1.11) | 1.58 (all seeds) | 0.84 |
| Shard Ridge Hard | <0.10 | 0.54 (0.50-0.57) | 1.58 (1.54-1.70) | 0.50 |
| Shard Ridge Psycho | <0.10 | 0.27 (0.18-0.36) | 1.26 (1.08-1.32) | 0.25 |

Mono rows, seed 1: H, in brackets as a share of the sensible player's median H (`<0.10` is counted as 0.10). Dominance =
best mono row / skilled.

```
  Level_C1_01 Easy      Cannon 2.09 (1.79); PulseRifle 0.76 (0.65); dominance 1.19
  Level_C1_01 Medium    Cannon 1.84 (1.50); PulseRifle 0.59 (0.48); dominance 1.46
  Level_C1_01 Hard      Cannon 1.14 (1.36); Laser 0.54 (0.65); PulseRifle 0.56 (0.67); dominance 1.00
  Level_C1_01 Psycho    Cannon 1.50 (2.83); Laser 0.37 (0.70); PulseRifle 0.40 (0.76); dominance 1.29
  Level_C1_03 Easy      Cannon <0.10 (0.24); Laser <0.10 (0.24); PulseRifle 0.27 (0.65); dominance 0.21
  Level_C1_03 Medium    Cannon <0.10 (0.12); Laser <0.10 (0.12); PulseRifle <0.10 (0.12); dominance 0.06
  Level_C1_03 Hard      Cannon <0.10 (0.18); Laser <0.10 (0.18); PulseRifle <0.10 (0.18); dominance 0.06
  Level_C1_03 Psycho    Cannon <0.10 (0.36); Laser <0.10 (0.36); PulseRifle <0.10 (0.36); dominance 0.08
```

## Stop rule 1: monotonicity

78 units were scanned (bisection runs plus eight scan points, 14 to 18 multiples each). 61 show one crossover. 17 show
two to five. Examples (W won, L lost, sorted by multiple):

```
Landing Site Medium careless seed 2:  1.25W 1.37L 1.46L 1.50W 1.54W 1.58L
Landing Site Psycho mono Cannon:      1.22W 1.34L 1.43W 1.50W 1.54L
Shard Ridge Easy    sensible seed 2:  0.76L 0.87W 0.96W 1.00W 1.03W 1.05W 1.08W 1.11L
Shard Ridge Psycho  sensible seed 1:  0.36W 0.37L 0.38L 0.39W 0.40L 0.42L 0.44L 0.46W 0.53L
Shard Ridge Psycho  sensible seed 2:  0.20W 0.22L 0.24W 0.24W 0.26W 0.27W 0.27W 0.28L 0.29L 0.30L 0.31W 0.35L
```

A likely reason, from the records (not proven by a separate experiment): a level is won or lost on one or two
all-or-nothing moments (the last boss wave, the air raid on Shard Ridge, a gate that falls). In a win the base usually
ends full; in a loss it ends at zero. A slightly different multiple shifts when creatures die, the bot then buys or
repairs in another order, and the decisive moment tips the other way. The fixed script is the least affected (1 of 23),
the reactive bots the most.

All runs of all seeds pooled per cell and rung (lowest multiple lost, highest multiple won, what lies between):

```
  Level_C1_01 Easy      careless  band 0.63-1.66 (width 162 %): 15 won, 8 lost inside (45 runs)
  Level_C1_01 Easy      sensible  band 1.19-1.19 (width 0 %): 2 won, 2 lost inside (48 runs)
  Level_C1_01 Easy      skilled   band 1.66-1.84 (width 11 %): 5 won, 5 lost inside (45 runs)
  Level_C1_01 Medium    careless  band 0.56-1.54 (width 176 %): 12 won, 11 lost inside (45 runs)
  Level_C1_01 Medium    sensible  band 1.22-1.26 (width 3 %): 5 won, 2 lost inside (48 runs)
  Level_C1_01 Medium    skilled   band 0.13-1.36 (width 929 %): 14 won, 9 lost inside (34 runs)
  Level_C1_01 Hard      careless  band 0.15-1.03 (width 604 %): 10 won, 13 lost inside (46 runs)
  Level_C1_01 Hard      sensible  band 0.78-0.88 (width 13 %): 10 won, 7 lost inside (48 runs)
  Level_C1_01 Hard      skilled   sharp: won up to 1.14, lost from 1.16 (42 runs)
  Level_C1_01 Psycho    careless  band 0.47-0.82 (width 75 %): 9 won, 15 lost inside (44 runs)
  Level_C1_01 Psycho    sensible  band 0.51-0.58 (width 13 %): 8 won, 7 lost inside (52 runs)
  Level_C1_01 Psycho    skilled   band 1.00-1.22 (width 22 %): 9 won, 5 lost inside (42 runs)
  Level_C1_03 Easy      sensible  band 0.34-1.08 (width 213 %): 11 won, 23 lost inside (54 runs)
  Level_C1_03 Easy      skilled   band 1.29-2.09 (width 62 %): 8 won, 14 lost inside (43 runs)
  Level_C1_03 Medium    sensible  band 0.52-1.11 (width 114 %): 16 won, 17 lost inside (49 runs)
  Level_C1_03 Medium    skilled   sharp: won up to 1.58, lost from 1.62 (45 runs)
  Level_C1_03 Hard      sensible  band 0.46-0.57 (width 23 %): 11 won, 8 lost inside (52 runs)
  Level_C1_03 Hard      skilled   band 1.58-1.70 (width 8 %): 4 won, 6 lost inside (45 runs)
  Level_C1_03 Psycho    sensible  band 0.17-0.46 (width 170 %): 20 won, 25 lost inside (59 runs)
  Level_C1_03 Psycho    skilled   band 1.11-1.35 (width 22 %): 10 won, 5 lost inside (42 runs)
  widest band: 929 %
```

## Stop rule 2: reproducibility

- **Same seed, rerun on another worker clone:** the eight sensible seed-1 bisections were repeated. 8 of 8 edges identical;
  all 54 shared runs have the same result, wave and base health. In 3 runs the per-wave detail differed slightly; each was
  the first run of a freshly started editor process. Two units of the first smoke batch also repeated exactly.
- **Across seeds 1-3** (max / min - 1):

| Cell | Careless | Sensible | Skilled |
| --- | --- | --- | --- |
| Landing Site Easy | 169 % | 3 % | 14 % |
| Landing Site Medium | 183 % | 5 % | 8 % (seeds 1 and 2; seed 3 loses at every multiple) |
| Landing Site Hard | 622 % | 8 % | 0 % |
| Landing Site Psycho | 79 % | 13 % | 26 % |
| Shard Ridge Easy | wall | 221 % | 66 % |
| Shard Ridge Medium | wall | 119 % | 0 % |
| Shard Ridge Hard | wall | 13 % | 11 % |
| Shard Ridge Psycho | wall | 103 % | 22 % |

  Within 5 %: 3 of 20. Part of the careless bot's spread is its own: its seed also shuffles which spots it takes. The
  sensible player draws nothing itself; its spread comes from the game's seed (which nest a creature leaves, group spacing).

So the simulation is deterministic, but one seed is not representative, and the bisection step (3 %) is much finer than
the real resolution.

## Stop rule 3: sanity against known facts

| Known fact | Measured | |
| --- | --- | --- |
| Cannon row wins Landing Site Hard and Psycho | H 1.14 and 1.50 | agrees |
| Off-role rows lose | Landing Site Gun Turret 0.40-0.76, Laser 0.37-0.54; Shard Ridge every mono row <0.10 except Gun Turret on Easy 0.27 | agrees |
| Shard Ridge: every obvious row falls at the air raid (yardstick: wave 13 or 14) | Careless and mono rows lose at every multiple down to a tenth of the health | agrees; it is the "wall" the review warned of |
| Landing Site Medium best plan won 2 of 3 seeds | 1.36, 1.26 and a loss at every multiple on seed 3 | agrees with 2 of 3, but it is not "near 1.0" |
| Skilled at least sensible | 8 of 8 cells | holds |
| Sensible at least careless | Fails on Landing Site Easy, Medium, Hard (careless 1.62 / 1.54 / 1.00 against 1.16 / 1.22 / 0.84), even on Psycho; holds on Shard Ridge | **does not hold** |
| H falls from Easy to Psycho | Careless on Landing Site yes. Sensible: Landing Site 1.16, 1.22, 0.84, 0.53; Shard Ridge 0.41, 0.84, 0.54, 0.27. Skilled: 1.75, 1.26, 1.14, 1.16 and 1.26, 1.58, 1.58, 1.26 | **does not hold** |

Notes:

- The "careless" bot is not weak on Landing Site: on two of three seeds it beats the sensible rule there. It builds at
  the gate, where the leak line shows nearly every wave dying.
- Shard Ridge Easy reads harder than Medium for the sensible player on two of three seeds. Whether that is the Easy waves
  or the rule was not investigated.
- The sensible player sits below 1.0 on Hard and Psycho of both levels and on most of Shard Ridge: it loses the levels as
  shipped. The skilled script wins every cell (H 1.14 to 1.75), except Landing Site Medium on seed 3.
- Plain save against expected save (sensible, seed 1): the same within 8 %. The save assumption is small on these two levels.

## Stop rule 4: rescale

Two cells were rescaled by 0.672 (to bring a median of 0.84 to 1.25) through a runtime override, and measured again on all seeds:

| Cell | Seed | H before | Target | H after | Off |
| --- | --- | --- | --- | --- | --- |
| Landing Site Hard, sensible | 1 / 2 / 3 | 0.82 / 0.88 / 0.84 | 1.21 / 1.31 / 1.25 | 1.19 / 1.29 / 1.26 | -1.7 / -1.7 / +0.8 % |
| Shard Ridge Medium, sensible | 1 / 2 / 3 | 0.84 / 1.11 / 0.50 | 1.25 / 1.65 / 0.75 | 1.22 / 1.62 / 0.76 | -1.7 / -1.7 / +0.8 % |

Per seed the arithmetic works, as it must: health is a plain product, so this mostly confirms the bisection. The median
lands at 1.26 and 1.22 against 1.25. But Shard Ridge Medium still runs from 0.76 to 1.62 over three seeds after tuning:
a rescale moves a cell, it does not make it certain.

## Per-wave leak line

Seed 1, at the data (x1.00) and at the last multiple won. Per wave the furthest its creatures got, in percent of the road
(B = that wave hit the base, . = wave not reached). Landing Site's gates stand at 12 % and 87 % of the road, so "11" means
the wave died at the first gate. Every run's line is in `Tools/PlayTest/headroom_records.txt`.

```
  Level_C1_01 Easy      careless  data x1.00  10 11 11 11 11 11 14 11 11 11 11 11  won w12/12 base 600/600
  Level_C1_01 Easy      careless  edge x1.66  11 11 10 11 11 11 14 11 11 12 18 12  won w12/12 base 600/600
  Level_C1_01 Easy      sensible  data x1.00   9  9  9  9 10 11 13 11 10 11 12 14  won w12/12 base 600/600
  Level_C1_01 Easy      sensible  edge x1.19   9  9  9  9 10 11 13 11 10 12 86 83  won w12/12 base 600/600
  Level_C1_01 Easy      skilled   data x1.00  10  9 10 10 11 11 14 11 10 11 11 10  won w12/12 base 600/600
  Level_C1_01 Easy      skilled   edge x1.62  11 10 11 11 11 11 14 11 11 12 99  B  won w12/12 base 277/600
  Level_C1_01 Medium    careless  data x1.00  10 11 10 11 11 11 14 11 11 11 12 12  won w12/12 base 600/600
  Level_C1_01 Medium    careless  edge x1.54  11 11 11 11 11 11 14 11 11 12 12 12  won w12/12 base 600/600
  Level_C1_01 Medium    sensible  data x1.00  10 10 10 10 10 10 13 11 10 11 12 12  won w12/12 base 600/600
  Level_C1_01 Medium    sensible  edge x1.26  11 10 10 10 10 11 14 11 10 12 86 13  won w12/12 base 600/600
  Level_C1_01 Medium    skilled   data x1.00  10  9 10 10  8 10 14 11  9 11 11 10  won w12/12 base 600/600
  Level_C1_01 Medium    skilled   edge x1.36  10  9 10 10 11 11 14 11 11 12 85 13  won w12/12 base 600/600
  Level_C1_01 Hard      careless  data x1.00  11 11 11 11 11 12 15 14 18 12 12 12  won w12/12 base 700/700
  Level_C1_01 Hard      careless  edge x1.00  11 11 11 11 11 12 15 14 18 12 12 12  won w12/12 base 700/700
  Level_C1_01 Hard      sensible  data x1.00  11 11 11 11 11 14 60 99 99 74  B  .  lost w11/12 base 0/700
  Level_C1_01 Hard      sensible  edge x0.82  10 11 10 11 11 12 15 12 85 12 86 13  won w12/12 base 700/700
  Level_C1_01 Hard      skilled   data x1.00  11 10 11 10 11 11 14  9 12 12 11 11  won w12/12 base 700/700
  Level_C1_01 Hard      skilled   edge x1.14  11 10 11 11 11 11 15 12 99 40 54  B  won w12/12 base 691/700
  Level_C1_01 Psycho    careless  data x1.00  11 11 11 11 14 14 99 63 99 99 99  B  lost w12/12 base 0/700
  Level_C1_01 Psycho    careless  edge x0.46  10 10 11 11 11 11 22 12 12 12 11 11  won w12/12 base 700/700
  Level_C1_01 Psycho    sensible  data x1.00  11 11 12 12 12 86 99 85 99 99 86  B  lost w12/12 base 0/700
  Level_C1_01 Psycho    sensible  edge x0.53  11 11 11 11 11 12 87 10 84 85 85 16  won w12/12 base 700/700
  Level_C1_01 Psycho    skilled   data x1.00  11 10 11 10 12 12 89 38 83 42 13 10  won w12/12 base 700/700
  Level_C1_01 Psycho    skilled   edge x1.16  11 11 11 10 12 12 99 75 83 68 66  B  won w12/12 base 185/700
  Level_C1_03 Easy      careless  data x1.00  76 54 53 75 76 75 76 76 54 76 53 99  B  B  .  .  lost w14/16 base 0/700
  Level_C1_03 Easy      sensible  data x1.00  58 58 56 57 57 71 57 56 57 55 55 99  B  B  .  .  lost w14/16 base 0/700
  Level_C1_03 Easy      sensible  edge x0.34  58 58 55 56 56 56 56 55 55 55 54 99  B  B 79 50  won w16/16 base 297/700
  Level_C1_03 Easy      skilled   data x1.00  56 56 55 56 56 56 56 55 56 57 57 81 57 56 83 55  won w16/16 base 700/700
  Level_C1_03 Easy      skilled   edge x2.09  59 58 57 56 56 56 56 55 57 74 74 90 86 85 91 57  won w16/16 base 700/700
  Level_C1_03 Medium    careless  data x1.00  75 54 53 54 53 53 76 54 54 53 99  B  B  .  .  .  lost w13/16 base 0/700
  Level_C1_03 Medium    sensible  data x1.00  57 57 55 55 56 54 53 57 54 55 82 99  B  B  B  .  lost w15/16 base 0/700
  Level_C1_03 Medium    sensible  edge x0.84  57 57 55 55 55 54 52 56 54 53 82 99  B 56 99  B  won w16/16 base 490/700
  Level_C1_03 Medium    skilled   data x1.00  58 57 56 56 56 71 57 57 58 70 75 89 57 71 86 76  won w16/16 base 700/700
  Level_C1_03 Medium    skilled   edge x1.58  74 60 56 74 75 74 74 74 60 74 79 96 86 87 99  B  won w16/16 base 700/700
  Level_C1_03 Hard      careless  data x1.00  54 70 53 53 52 52 53 54 52 53 99  B  B  .  .  .  lost w13/16 base 0/700
  Level_C1_03 Hard      sensible  data x1.00  57 57 55 56 55 54 41 56 53 59 99  B  B 99  B  .  lost w15/16 base 0/700
  Level_C1_03 Hard      sensible  edge x0.54  56 56 55 55 55 53 41 55 43 56 99  B  B 99  B  B  won w16/16 base 36/700
  Level_C1_03 Hard      skilled   data x1.00  58 58 56 56 56 73 59 56 56 75 99  B  B 99 86 77  won w16/16 base 700/700
  Level_C1_03 Hard      skilled   edge x1.54  57 75 57 57 57 75 59 58 56 78 99  B  B 99 99  B  won w16/16 base 282/700
  Level_C1_03 Psycho    careless  data x1.00  53 53 52 55 52 54 53 54 51 54 99  B  B  .  .  .  lost w13/16 base 0/800
  Level_C1_03 Psycho    sensible  data x1.00  54 54 51 60 41 55 54 45 47 51 99  B  B  .  .  .  lost w13/16 base 0/800
  Level_C1_03 Psycho    sensible  edge x0.36  52 52 40 55 40 42 54 42 42 52 82 88 51 89 99  B  won w16/16 base 744/800
  Level_C1_03 Psycho    skilled   data x1.00  57 57 57 57 57 58 57 57 56 74 92 99  B 83 85 76  won w16/16 base 800/800
  Level_C1_03 Psycho    skilled   edge x1.26  57 57 57 58 57 59 57 58 56 77 99 99  B  B  B  B  won w16/16 base 150/800
```

What it shows: on Landing Site everything dies at the first gate until wave 10 or later, and the level is decided in the
last two or three waves. On Shard Ridge waves 11 to 13 reach the base in almost every run, win or lose. The line marks
where a level is decided; it moves little with the multiple until the edge.

## Cost

| | Runs | Wall clock |
| --- | --- | --- |
| Main batch: 88 units (ladder x 3 seeds, mono rows, plain save; scan on all) | 1298 | 74 min on 5 clones |
| Rescale check (6 units) | 37 | 4 min |
| Rerun check (8 units) | 54 | 4 min |
| First smoke batch (not in the records) | 61 | 5 min |
| **Total** | **1450** | **87 min** (about 385 machine minutes) |

16 s a run. A bisection is about 7 runs without the scan and 13 with it (the review estimated 10). The review's budget
(300 runs, 20 minutes) covered one seed and six scans; three seeds and a scan everywhere cost four to five times that.

## What this means

What held up:

- The machinery. The probe is deterministic per seed, a bisection costs about 2 minutes, nothing needs a model or a fit,
  and a new mechanic needs no code. One command gives the table.
- Tuning arithmetic per seed.
- The qualitative facts match the yardstick (Cannon row, off-role rows, the air wall, the 2-of-3 plan).

What did not:

- **A single-seed win/lose edge is not a stable number.** Its real resolution on these two levels is 5 to 15 % for the
  better-behaved cells and a factor of two or more for the others. A band of "sensible 1.15 to 1.4" cannot be gated with it.
- **The reactive bots are a poor ruler.** Their order is not careless < sensible < skilled, their edges flip back most
  often, and the careless one carries its own dice.
- **Walls.** Where a row loses at every multiple, H says only "<0.10". That is useful as a flag, but it is not a margin.

Options for a rework, in order of cost. None is built; this is a proposal to decide on.

1. **Use fixed build orders as the ruler, not reactive bots.** The scripted rung was the most monotone (22 of 23) and has
   the smaller spread (median about 12 %, against 13 % for sensible with a far worse tail). Remco's own recorded builds
   (review 2.6) would be scripts too. Several scripts per cell (a weak, a typical and a strong one) can stand in for the ladder.
2. **Report H as a median over more seeds with its range**, never as one number. Bands must then be wider than the
   measured range, which rules out fine gates on Shard Ridge.
3. **Measure a continuous outcome at the data instead of bisecting a binary one:** base health left, gates lost and the
   leak line, per wave, over seeds. It costs one run per seed and does not need monotonicity. Its weakness is the one
   the review named: most wins end with a full base, so it saturates on easy cells.
4. **Give the careless bot a seed of its own**, apart from the game's, so its spread can be told from the level's.
5. **Treat walls as a composition check** (is the counter owned, affordable and in reach before the first raid), which
   needs no bisection.

Recommendation: do not build the 24-cell table or replace the acceptance gate yet. First spend a short stage on options 1
and 2 for the same two levels (scripts only, five seeds) and see whether the spread drops under about 10 %. If it does,
Stage 2 can go ahead with script-based rungs and wide bands. If it does not, the headroom number is a rough guide
(easy / about right / too hard) rather than a gate, and the review's promise of "reproducible to 5 %" has to be dropped.

## Files

- `Assets/Tests/PlayMode/HeadroomProbeTests.cs`: the bisection (explicit test `Measure_Headroom`).
- `Assets/Tests/PlayMode/ProbeStrategy.cs`, `BalanceProbeTests.cs`: the sensible rung (`PlayStyle.Sensible`, `SensibleAct`).
- `Assets/Tests/PlayMode/BotRun.cs`: the per-wave reach (`Reach`).
- `Tools/PlayTest/headroom.sh`, `headroom_report.py`: the one command and the table.
- `Tools/PlayTest/headroom_records.txt` (every run and edge), `Tools/PlayTest/headroom.txt` (the full report with all 78 scans).
