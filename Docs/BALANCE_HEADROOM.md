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

---

# Stage 1b: a continuous pressure score at fixed multiples (2026-11-17)

Test and tool code only. No gameplay or data change; the stage 1 pipeline (`headroom.sh`) is untouched and still works.
How to run it: Docs/COMMANDS.md, "Balance stage 1b: pressure". Everything below is from one batch: Landing Site (C1_01) and
Shard Ridge (C1_03), four difficulties, seeds 1-5, seven health multiples (0.5, 0.65, 0.8, 1, 1.25, 1.6, 2), 1120 runs.

## Verdict

**Stage 1b did not pass its stop rules either. The pressure score is not a continuous ruler: it is two-valued, because the
game is.** It is good enough for a coarse reading (easy / about right / too hard / broken whatever the health) and it found
three structural faults that need no fine ruler. It is not good enough to gate percentages.

| Stop rule (fixed before the runs) | Result | Numbers |
| --- | --- | --- |
| (a) P is monotone in the health multiple for at least 95 % of the unit/seed curves (tolerance 0.02 per step) | **Fails by one curve** | 151 of 160 curves = 94.4 %. Fixed build orders: skilled 38 of 40, typical 40 of 40 (together 97.5 %). Sensible bot 34 of 40 (85 %), careless 20 of 20, Cannon row 19 of 20. 9 of 960 steps drop by more than the tolerance; 6 of the 9 curves drop by less than 0.06, 3 are real flip-backs (a loss, then a win at more health). |
| (b) Seed spread of P at the data, (max - min) / median, at most 10 % for the skilled script in every cell | **Fails** | 1 of 8 cells (Shard Ridge Easy, 0.9 %). The others: 17 to 151 %. |
| (c) Agrees with known facts | **Holds** | Cannon row wins Landing Site 5 of 5 seeds on all four difficulties. The sensible player wins Shard Ridge Hard and Psycho on 0 of 5 seeds. The air raid hits the base in wave 12 or 13 on 5 of 5 seeds (sensible; Medium, Hard, Psycho) and 4 of 5 (Easy). |

## What was measured

Per run, defined in the header of `Tools/PlayTest/pressure_report.py` before the batch was started:

```
P = A + D          0 = nothing got anywhere, 2 = everything walked in and the base fell
A = mean over the level's waves of a_w
    a_w = sum(health_i * furthest_i) / sum(health_i) over the creatures spawned in wave w;
          furthest_i = the share of its road creature i covered before it died (1 = it reached the base);
          a wave that never came because the base fell counts as 1
D = base health lost over the run / base maximum, capped at 1; a lost level counts as 1
```

Why this shape: stage 1's leak line was the furthest single creature of a wave, which jumps. `a_w` weighs every creature
by its health, so one creature a step further moves it a little (new in `BotRun.Advance`). D adds what the leaks cost.
The same check on the other candidates, for comparison: A alone 148 of 160 monotone, D alone 157, win/lose 157, the
furthest-creature score of stage 1 119, gate damage 88.

- **Rungs:** *skilled* = the build order of the cell's best replayed plan as a fixed script (as in stage 1). *typical* = the
  build order the sensible bot took on seed 1 at the data, replayed as a fixed script on every seed and multiple (new).
  *sensible* = the reactive bot of stage 1, kept as the stand-in for a real player. Landing Site also got the Cannon row
  and the careless bot.
- **No bisection.** Every unit is played at the seven multiples; nothing depends on the previous run.

## The table

P at the data (x1.00): median over seeds 1-5, range in brackets, seeds won.

| Cell | Skilled script | Typical script | Sensible bot |
| --- | --- | --- | --- |
| Landing Site Easy | 0.113 (0.090-0.115) 5/5 | 0.106 (0.085-0.107) 5/5 | 0.106 (0.084-0.108) 5/5 |
| Landing Site Medium | 1.107 (0.081-1.109) 2/5 | 0.111 (0.087-0.113) 5/5 | 0.111 (0.087-0.113) 5/5 |
| Landing Site Hard | 0.108 (0.092-0.255) 5/5 | 1.285 (1.251-1.289) 0/5 | 1.263 (0.256-1.274) 1/5 |
| Landing Site Psycho | 0.692 (0.126-1.163) 3/5 | 1.289 (1.270-1.338) 0/5 | 1.289 (1.254-1.330) 0/5 |
| Shard Ridge Easy | 0.473 (0.472-0.476) 5/5 | 1.491 (1.488-1.492) 0/5 | 1.490 (0.444-1.492) 1/5 |
| Shard Ridge Medium | 0.518 (0.491-1.119) 5/5 | 1.447 (1.237-1.511) 2/5 | 1.335 (1.237-1.511) 3/5 |
| Shard Ridge Hard | 0.839 (0.658-0.850) 5/5 | 1.503 (1.454-1.508) 0/5 | 1.503 (1.449-1.508) 0/5 |
| Shard Ridge Psycho | 0.606 (0.531-0.633) 5/5 | 1.537 (1.519-1.541) 0/5 | 1.538 (1.481-1.541) 0/5 |

Landing Site only: Cannon row 0.109 / 0.112 / 0.109 / 0.137, 5/5 on every difficulty; careless bot 1.123 / 1.127 / 1.160 /
1.173 with 2/5, 2/5, 1/5, 0/5.

Seeds won per multiple, and H50 = the multiple at which half the seeds are lost (interpolated on the log of the multiple).
This is the reading the data supports best; its resolution is the grid step, about 25 %.

```
                                    x0.5   x0.65  x0.8   x1     x1.25  x1.6   x2     H50
  skilled     Landing Site Easy     5/5    5/5    5/5    5/5    5/5    4/5    0/5    1.74
  skilled     Landing Site Medium   2/5    2/5    2/5    2/5    2/5    0/5    0/5    <0.5
  skilled     Landing Site Hard     5/5    5/5    5/5    5/5    0/5    0/5    0/5    1.12
  skilled     Landing Site Psycho   5/5    5/5    3/5    3/5    0/5    0/5    0/5    1.04
  skilled     Shard Ridge Easy      5/5    5/5    5/5    5/5    5/5    2/5    1/5    1.54
  skilled     Shard Ridge Medium    5/5    5/5    5/5    5/5    5/5    3/5    0/5    1.66
  skilled     Shard Ridge Hard      5/5    5/5    5/5    5/5    5/5    3/5    0/5    1.66
  skilled     Shard Ridge Psycho    5/5    5/5    5/5    5/5    4/5    0/5    0/5    1.37
  typical     Landing Site Easy     5/5    5/5    5/5    5/5    5/5    0/5    0/5    1.41
  typical     Landing Site Medium   5/5    5/5    5/5    5/5    5/5    0/5    0/5    1.41
  typical     Landing Site Hard     0/5    0/5    0/5    0/5    0/5    0/5    0/5    <0.5
  typical     Landing Site Psycho   0/5    0/5    0/5    0/5    0/5    0/5    0/5    <0.5
  typical     Shard Ridge Easy      0/5    0/5    0/5    0/5    0/5    0/5    0/5    <0.5
  typical     Shard Ridge Medium    5/5    3/5    3/5    2/5    0/5    0/5    0/5    0.89
  typical     Shard Ridge Hard      4/5    0/5    0/5    0/5    0/5    0/5    0/5    0.55
  typical     Shard Ridge Psycho    0/5    0/5    0/5    0/5    0/5    0/5    0/5    <0.5
  sensible    Landing Site Easy     5/5    5/5    5/5    5/5    0/5    0/5    0/5    1.12
  sensible    Landing Site Medium   5/5    5/5    5/5    5/5    2/5    0/5    0/5    1.20
  sensible    Landing Site Hard     5/5    5/5    4/5    1/5    0/5    0/5    0/5    0.89
  sensible    Landing Site Psycho   4/5    0/5    0/5    0/5    0/5    0/5    0/5    0.55
  sensible    Shard Ridge Easy      2/5    1/5    1/5    1/5    0/5    0/5    0/5    <0.5
  sensible    Shard Ridge Medium    5/5    3/5    3/5    3/5    0/5    0/5    0/5    1.04
  sensible    Shard Ridge Hard      3/5    0/5    0/5    0/5    0/5    0/5    0/5    0.52
  sensible    Shard Ridge Psycho    0/5    0/5    0/5    0/5    0/5    0/5    0/5    <0.5
  careless    Landing Site Easy     5/5    2/5    2/5    2/5    2/5    2/5    0/5    0.62
  careless    Landing Site Medium   5/5    2/5    2/5    2/5    2/5    1/5    0/5    0.62
  careless    Landing Site Hard     2/5    2/5    2/5    1/5    0/5    0/5    0/5    <0.5
  careless    Landing Site Psycho   3/5    2/5    1/5    0/5    0/5    0/5    0/5    0.57
  mono-Cannon Landing Site Easy     5/5    5/5    5/5    5/5    5/5    5/5    4/5    >2
  mono-Cannon Landing Site Medium   5/5    5/5    5/5    5/5    5/5    5/5    1/5    1.84
  mono-Cannon Landing Site Hard     5/5    5/5    5/5    5/5    0/5    0/5    0/5    1.12
  mono-Cannon Landing Site Psycho   5/5    5/5    5/5    5/5    5/5    0/5    0/5    1.41
```

The full per-multiple P table, the per-seed derived edges and the per-wave advance are in `Tools/PlayTest/pressure.txt`.

## Why the score is two-valued

Skilled script, P per multiple (median over seeds):

```
Landing Site Hard   x0.5 0.09  x0.65 0.10  x0.8 0.10  x1 0.11  x1.25 1.22  x1.6 1.24  x2 1.39     won 5 5 5 5 0 0 0
Shard Ridge Hard    x0.5 0.51  x0.65 0.55  x0.8 0.64  x1 0.84  x1.25 0.94  x1.6 1.50  x2 1.62     won 5 5 5 5 5 3 0
```

- On Landing Site nothing moves until the level is lost. Up to the last multiple won A rises by a few hundredths and D
  stays 0; the next step adds a whole point. A run that wins ends with a full base in almost every case; a run that loses
  ends at zero. There is no "won with damage" zone to interpolate in, so P carries the same information as win/lose plus
  a small term. That is a property of the level (one gate holds everything, and when it falls everything is through),
  not of the score.
- Shard Ridge Hard and Psycho do degrade gradually for the skilled script, because the air raid costs base health every
  time without ending the level. Only there is P a real margin.
- The seed spread of rule (b) is mostly not noise around a mean. On Landing Site the seeds fall into two groups:
  seeds 1 and 2 read 0.090 / 0.090, seeds 3, 4 and 5 read 0.113 / 0.115 / 0.114 (Easy, skilled). See the first finding below.

## Structural findings (health-independent)

A loss at half the health is not a balance margin; it is a fault of the level or of the player. The batch shows three.

1. **Landing Site, wave 7: the seed draws a nest behind the first gate.** Wave 7 has one nest group and the level has two
   nests, at cells (17,1) and (10,8); `LevelController.CreateOrigins` draws the nest per spawn from the seed. On seeds 3,
   4 and 5 a wave 7 creature comes up past the first gate and walks to the far gate and the base (wave 7 reach 0.97 to
   1.00 at the data, against 0.13 to 0.14 on seeds 1 and 2). Players with no gun back there lose the far gate (exactly 1000 health in waves
   11 and 12) and then the base. Evidence: the skilled script on Medium loses on seeds 3, 4 and 5 at
   every multiple including x0.5 (base lost 588 in wave 12, game time 554 s against 329 s), and the careless bot wins
   2 of 5 from x0.65 to x1.25. The sensible bot and the Cannon row have a gun at (19,14) and do not notice. This is
   stage 1's "seed 3 loses at every multiple", explained. Which of the two nests is the one behind the gate was not
   checked on the map; the split by seed is what the records show.
2. **Shard Ridge Easy: the sensible player cannot buy the sky gun in time.** See the next section.
3. **Shard Ridge Hard and Psycho are a wall for the sensible player** (0 of 5 at the data; Hard 3 of 5 at half the
   health, Psycho 0 of 5 at every multiple), while the skilled script wins 5 of 5 up to x1.25 on Hard. The gap between
   the rungs is the whole grid.

## Why Shard Ridge Easy reads harder than Medium (sensible player)

Not the waves' health: Easy spawns 11 383 health against Medium's 16 837 (68 %; a won run of each, seed 2), and a won
level earns about the same coins (1428 against 1495). It is the timing of the first air raid.

| | Easy | Medium |
| --- | --- | --- |
| Income / starting money (difficulty profile) | x0.8 / x0.8 (192 coins) | x0.95 / x1 (240 coins) |
| Wave 11 | the heaviest ground wave so far (1224 health), no flyers | the soft air introduction (191 health) |
| Expected player (research from two earlier levels) | 12 points, 2 upgrades | 24 points, 5 upgrades |
| First flyers (wave assets) | wave 12: 4 Skyrays inside a normal wave (wave health multiplier 0.6) | wave 11: 4 Skyrays in a soft introduction wave (multiplier 0.242), then 5 in wave 12 |
| Sky gun (Rocket, 120 coins) bought by the sensible bot at the data | never on seeds 1, 3, 4, 5; wave 13 on seed 2 | wave 10 on all five seeds |
| Result at the data / at half the health | 1 of 5 / 2 of 5 | 3 of 5 / 5 of 5 |

On Medium the preview shows flyers during wave 10, the bot buys the Rocket then, and it stands before the raid. On
Easy the bot spends its wave 10 money on a Gun Turret and its upgrade, the preview first shows flyers during wave 11,
and nothing at all is bought in waves 11 and 12 (the rule keeps the purse for the Rocket; the purse itself is not in
the records, so "it did not reach 120 coins in time" is inferred). From wave 13
the preview shows no flyers, so the bot stops saving and upgrades again while the Skyrays sit on the base: it loses 221
to 499 base health in wave 13 and the rest in wave 14, the same at x0.5 as at x1 (seed 1: 901 and 818 coins earned, no
Rocket). On seed 2 the Rocket is bought in wave 13 and the level is won with a full base. So Easy is decided by whether
one 120-coin purchase lands a little earlier or later; Medium is not. Part of this is the bot's rule (it forgets flyers
that are still alive once the preview moves on); part is the data (Easy has the smallest starting purse, no introduction wave
before its first raid, and its heaviest ground wave right in front of it). The skilled script, which buys the Rocket in wave 10 on Easy too, wins 5 of 5 up to x1.25.

## Why the careless bot beats the sensible bot on Landing Site

Two causes, both in what they build, read from the recorded build orders.

- **The careless bot builds the Cannon; the sensible bot does not.** The careless rule takes the owned ground guns in
  turn, so half its guns are Cannons (Easy, seed 1, x1.25: 4 Cannons and 5 Gun Turrets, 4 upgrades; it wins with a
  full base). The sensible rule buys "the dearest step it can afford", and an upgrade of a standing Gun Turret (40 or
  65 coins) is nearly always affordable before 100 coins for a Cannon are saved: one Cannon and eight Gun Turrets with
  7 upgrades in the same cell, and it loses in wave 12. The Cannon row is the strongest thing on this level (5 of 5 up
  to x1.6 on Easy), so the bot that stumbles into Cannons wins.
- **On Hard the sensible bot starves itself.** At the data, seed 1, it builds a Laser and two Gun Turrets by wave 5 and
  then nothing: it spends 213 coins on repairs, earns 601 coins in all (the careless bot 1740) and loses in wave 11
  with three guns. At x0.8 the same bot buys a Cannon in wave 6 and wins. The likely mechanism, not separately tested:
  armour is in the preview, the rule keeps the purse for the counter, and the gate repairs take the coins before they
  reach its price. This is a fault of the bot's saving rule, not of the level.

The careless bot's own weakness is the first finding above: it has no gun at the far gate, so it loses on seeds 3 to 5.
In stage 1 (seeds 1 to 3) that showed as its huge spread.

## Stop rules in detail

**(a) Monotone.** The nine curves that are not (P per multiple, L = lost, ! = the drop):

```
Landing Site Hard     skilled      seed 2: 0.5:0.08 0.65:0.08 0.8:0.09 1:0.09 1.25:1.22L 1.6:1.17L! 2:1.39L   (-0.044)
Landing Site Psycho   mono-Cannon  seed 3: 0.5:0.11 0.65:0.11 0.8:0.13 1:0.16 1.25:0.14! 1.6:1.29L 2:1.32L     (-0.023)
Landing Site Psycho   sensible     seed 1: 0.5:0.18 0.65:1.25L 0.8:1.28L 1:1.25L! 1.25:1.33L 1.6:1.44L 2:1.47L  (-0.025)
Landing Site Psycho   skilled      seed 4: 0.5:0.12 0.65:0.35 0.8:1.13L 1:0.69! 1.25:1.21L 1.6:1.40L 2:1.40L    (-0.439)
Shard Ridge Easy      sensible     seed 2: 0.5:0.43 0.65:1.48L 0.8:0.44! 1:0.44 1.25:1.49L 1.6:1.51L 2:1.52L    (-1.045)
Shard Ridge Medium    sensible     seed 5: 0.5:0.47 0.65:1.44L 0.8:1.44L 1:1.33! 1.25:1.45L 1.6:1.54L 2:1.56L   (-0.107)
Shard Ridge Psycho    sensible     seeds 1, 3, 5: three drops of 0.02 to 0.05 between two lost runs
```

Six are small wobbles between runs with the same result. Three are flip-backs (a win above a loss): one for a script,
two for the sensible bot. Win/lose itself flips back in 3 of 160 curves on this grid, against 17 of 78 in stage 1's
fine scan: a coarse grid hides most of the flip-back zone, it does not remove it.

**(b) Spread of P at the data over five seeds**, (max - min) / median, absolute range in brackets:

| Cell | Skilled script | Typical script | Sensible bot |
| --- | --- | --- | --- |
| Landing Site Easy | 21.8 % (0.025) | 21.1 % (0.022) | 22.5 % (0.024) |
| Landing Site Medium | 92.9 % (1.029) | 23.0 % (0.026) | 23.4 % (0.026) |
| Landing Site Hard | 151.0 % (0.163) | 3.0 % (0.039) | 80.7 % (1.018) |
| Landing Site Psycho | 149.9 % (1.038) | 5.3 % (0.068) | 5.9 % (0.076) |
| Shard Ridge Easy | 0.9 % (0.004) | 0.3 % (0.004) | 70.3 % (1.047) |
| Shard Ridge Medium | 121.4 % (0.629) | 18.9 % (0.274) | 20.5 % (0.274) |
| Shard Ridge Hard | 22.9 % (0.192) | 3.6 % (0.054) | 4.0 % (0.060) |
| Shard Ridge Psycho | 16.8 % (0.102) | 1.5 % (0.023) | 3.9 % (0.061) |

Three kinds of spread: about 22 % wherever the wave 7 nest draw splits the seeds without changing the result (within a
group it is about 2 %); about 100 % or more wherever the seeds straddle a win and a loss; and 1 to 5 % only where every
seed ends the same way. The low figures of the typical script on Hard and Psycho are losses on every seed: the score is
stable there because it is saturated. The derived edge (where base damage crosses half) spreads 0 to 60 % over the seeds.

**(c) Known facts.**

| Known fact | Measured | |
| --- | --- | --- |
| Cannon row is strong on Landing Site | 5 of 5 at the data on all four difficulties; H50 >2, 1.84, 1.12, 1.41, at or above the skilled script (1.74, <0.5, 1.12, 1.04) | agrees |
| Shard Ridge Hard and Psycho are too hard for the sensible player | 0 of 5 at the data; Hard 3 of 5 at x0.5, Psycho 0 of 5 at every multiple | agrees |
| The air raids of waves 11 to 13 on Shard Ridge reach the base | Sensible: base hit in wave 12 and 13 on 5 of 5 seeds (Hard, Psycho), wave 13 on 5 of 5 (Medium) and 4 of 5 (Easy). Skilled: Hard wave 12 on 4 of 5 and wave 13 on 5 of 5, Psycho wave 13 on 4 of 5, Medium wave 13 on 3 of 5, Easy never | agrees, except the skilled script on Easy |

The typical script reads like the sensible bot nearly everywhere (the same median to 0.03 in 7 of 8 cells). So a reactive
bot is not much noisier than its own frozen build order: the spread comes from the game's seed, not from the bot reacting.

## Cost

| | Runs | Wall clock |
| --- | --- | --- |
| Main batch: 160 units (3 rungs x 8 cells x 5 seeds, Cannon row and careless on Landing Site) x 7 multiples | 1120 (+40 reference runs for the typical scripts) | 58 min on 5 clones |
| Smoke batch (not in the records) | 6 | 2 min |

15 s a run, 275 machine minutes. One cell, one rung, five seeds, seven multiples: 35 runs, about 9 machine minutes.

## Recommendation

**Do not use P, or H, as a gate with percentage bands. Do use this probe to drive structural wave and map fixes, and
read it as win counts.**

- Good enough, today, for: "this cell is lost whatever the health" (a loss at x0.5), "this cell falls between x1 and
  x1.25 for the script" (five seeds agree), the per-wave advance and base-hit lines that name the wave, and the build
  orders that name the cause. That is what found the three structural faults above, and those are the next fixes worth
  making; none of them is a health number.
- Not good enough for: tuning a cell to "sensible 1.15 to 1.4". The resolution is one grid step (about 25 %) where the
  seeds agree and nothing where they straddle the edge. A finer grid would run into stage 1's flip-back zone.
- A real continuous margin needs the game to have one: damage that is survivable and counted (the base taking chip
  damage, as on Shard Ridge Hard), not a single gate that holds or falls. Whether Landing Site should have that is a
  design question for Remco, not a measurement question.

Proposed order, none of it built:

1. Decide the three structural findings (wave 7 nest draw on Landing Site; Easy's first raid on Shard Ridge without an
   introduction wave and with the smallest starting purse; the Hard and Psycho air wall). They are data fixes.
2. Repair the sensible bot before it is used as "a real player" again: keep saving for a counter while its targets are
   still alive, and do not let repairs starve the counter purchase. Until then its Shard Ridge Easy and Landing Site Hard
   readings are the bot's, not the level's.
3. As the acceptance reading, use seeds won out of five at x0.8, x1 and x1.25 for a fixed script and for the repaired
   sensible bot (30 runs a cell, about 8 machine minutes), with three verdicts: wins 5 of 5 at x1.25 = roomy, wins at x1
   but not x1.25 = tight, loses at x1 = too hard; plus the flag "lost at x0.5 = structural".

## Files (stage 1b)

- `Assets/Tests/PlayMode/PressureProbeTests.cs`: the probe (explicit test `Measure_Pressure`), the typical script.
- `Assets/Tests/PlayMode/BotRun.cs`: `Advance()` and `WaveAdvance` (per wave: health spawned, progress-weighted health, health that reached the base); `BalanceProbeTests.ProbeResult.Advance` carries it.
- `Assets/Tests/PlayMode/HeadroomProbeTests.cs`: `Scaled` and `BestRecordedPlan` opened to the test assembly (no behaviour change).
- `Tools/PlayTest/pressure.sh`, `pressure_report.py`: the one command and the report with the stop rules.
- `Tools/PlayTest/pressure_records.txt` (every run, with its build order), `Tools/PlayTest/pressure.txt` (the full report).

# Round 74: the ruler after the bot repair (2026-11-17)

The reading proposed above (seeds won out of five at x0.8, x1 and x1.25, a fixed script and the sensible bot) was used to carry
out the fix plan of Docs/BALANCE_CRITIC_2026-11-17.md. Full account: Docs/STATUS.md, "Round 74". In short:

- The sensible bot was the fault behind most of stage 1b's "structural" findings. Repaired (living creatures count as coming,
  counters grow with losses and with the preview, the trait furthest behind first, repairs wait for the counter, a marked rift
  behind the gate gets a gun), it wins Shard Ridge Easy and Medium on 5 of 5 seeds at every multiple (was 1 and 3 of 5 at
  the data) and Hard on 5, 5 and 4 (was 0). Shard Ridge needed no data change.
- Landing Site's wave 7 rift is authored (`SpawnGroup.Nest`), so the seed no longer draws it. The seed spread of P at the
  data fell from 21 to 93 % to 0.5 to 2.2 % in the cells every seed wins (the skilled script on Easy excepted: 24 %, one
  creature shot on its way or not). The skilled script, which now reacts to a marked rift, wins Medium 5 of 5 (was 2).
- The price: with the rift on every run, Landing Site Hard reads 4, 0, 0 for the sensible bot (3, 3, 0 before) and Psycho
  5, 1, 0 for the skilled script (4, 3, 0 before). Open for Remco in the STATUS entry.
- Records and reports: `Tools/PlayTest/pressure_r74_baseline*.txt`, `pressure_r74_final*.txt`; `pressure_wins.py` prints the
  win table and the bot's purchases with the purse.

## Round 75: the other four levels (2026-11-17)

The same reading (seeds won of five at x0.8, x1, x1.25) on Glass Meadows, Fault Line, Quartz Flats and Crysium Vein; the
tables, the diagnoses and the open questions are in Docs/STATUS.md, "Round 75".

- Most of the baseline's zeros were the sensible bot again: an opening of ground guns only, a first Skyray one wave ahead and
  10 to 30 coins a wave. It now sells a gun for its first Flak, builds sky guns by the counter rules only and puts a gun by a
  besieged base. Fault Line Hard went from 0 0 0 to 5 5 4, Psycho to 4 3 0, Quartz Flats Hard to 4 0 0.
- Two level faults in the data, both the builder's scale: Medium carried more health per coin than Hard on Fault Line and
  Crysium Vein (`Tools/PlayTest/health_per_coin.py`). Three scales moved in `wave_tuning.txt`; Crysium Vein Medium went from
  3 1 0 to 5 5 5 (skilled 5 4 1 to 5 5 5), Fault Line's order is restored for the skilled rung.
- Still off: Fault Line Medium for the sensible bot (3 3 4, indifferent to health) and Quartz Flats Medium (2 0 0: the boss
  wave takes the whole base or none of it).
- The skilled rung is a frozen script; on Quartz Flats Hard it loses 5 of 5 at the data where the same strategy replayed
  adaptively wins 3 of 3. Read a skilled zero with that in mind.
- Records and reports: `Tools/PlayTest/pressure_r75_*.txt`.
