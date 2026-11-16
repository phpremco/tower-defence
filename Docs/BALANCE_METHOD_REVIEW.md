# Balance method review (2026-11-16)

An independent read of how balance is computed today, written for Remco's question: *we test for hours per round and
the answer is still "not sure". Is there a way to calculate this properly, so that maps can be generated?*

Read-only review. Nothing was run; every number below is quoted from the repo (source named) or marked as an estimate.

Short answer: **yes, but not by making the paper model better.** The simulations are not the problem (they are
deterministic and take 15 to 20 seconds each). The uncertainty comes from what sits between the simulations and the
verdict: a second, hand-written copy of the game (the ledger) that has to be re-fitted every round, a pass/fail gate of
about twenty conflicting conditions per cell, and no measurement of a human at all. Replace those three with one measured
number per level and policy ("how much tougher could the waves be before this player loses"), and the answer after a
change becomes a table of numbers with a known resolution instead of a list of open decisions.

---

## 1. Diagnosis: where "not sure" comes from

### 1.1 The paper model is a second implementation of the game, and it does not agree with the first

`Assets/_Project/Scripts/Balance` is 11,700 lines in 34 files, with about 70 tuned numbers in `BalanceRules.cs` alone,
plus fitted factors (per-tower efficiency, route fire, queue guard share, siege factor, two utilisations). It re-describes
placement, kill position along the route, gate sieges, leaks, the hero, armour pools, high ground, growth windows.

Its own self-check says how well it agrees with the game (`Tools/PlayTest/calibration.txt`, fit of 2026-11-15):

- Hold-out verdict agreement **342 of 432 runs (79 %)**; the project's own bar is 90 %. BALANCE.md round 67: "under the
  90 % bar as in every round since 62", applied with `apply-force`. Round 27 was 78 %, also applied by override.
- The disagreements are not near misses. Landing Site, the tutorial level: "paper falls (base 0 at wave 7, margin 1.39),
  game won 3 stars at wave 12/12 base 600", for nearly every policy on every difficulty. The other way round on Glass
  Meadows: "paper survives (base 700) ... game lost".
- Per-cell rank correlation between paper margin and play result ranges from 0.46 to 1.00.

One run in five wrong, on the data it was fitted to, is too weak to carry a verdict. But every later stage leans on it:
the solver sizes waves with it, the plan search picks plans with it, the impact calculator reports with it.

### 1.2 The fitted factors are not identified, so they move when the data moves

BALANCE.md round 27, two fits on almost the same records (675 and 687 runs):

| Factor | Fit 1 | Fit 2 |
| --- | --- | --- |
| Route fire | 1.0 | 4.0 |
| Queue guard share | 0.70 | 0.80 |
| Siege factor | 0.91 | 2.05 |

The doc's own words: "not well identified jointly". When one factor can move 4x and another 2x with no change in fit
quality, the factors are absorbing things the model does not describe (the doc lists them: repair drone, tower wear,
hero abilities "have no paper value; the fit absorbs their average"). Per-tower efficiencies drift each round too
(Cannon 1.135 -> 0.842 -> 0.737 -> 0.797 -> 0.842; Gun Turret 1.017 -> 0.671 -> 0.764 -> 0.808 -> 0.876). Each refit
shifts every paper number, so results from different rounds cannot be compared: a moving ruler.

There was also a real bug in the ruler: `FitSiege` accumulated its sums across calls from round 22d to round 27, so the
siege factor depended on grid order for five rounds. And in round 68 a merge duplicated plan records up to five times
("one acceptance run read C1_04/C1_06 as 15/15"). A measurement chain this long has its own defects.

### 1.3 The solver's answer is overruled by hand in almost every cell

The design was "balance by construction": the solver sets wave health so pressure meets a target curve. In practice
`Tools/PlayTest/wave_tuning.txt` carries a per-cell scale on top of the solve, for 19 of 24 cells, from **0.45 to 1.0**,
plus per-cell levers (`plated`, `swarm`, `tempo`, `raid`, `armourgrowth`, `bosscrowd`). The scales were set by playing:
`BalanceRules.cs` says so in a comment ("the Easy scales are set by play ... the band only catches a runaway scale now"),
and the band itself was widened to make room (`MaxEasySurplus` 1.6 -> 1.9 -> 2.4).

So the real method today is already *trial and error in the simulator, by an agent, one lever at a time*, with the model
as an expensive detour: solve on paper, discover in play it is off by up to 2.2x, patch a scale, re-solve, re-measure.
Round 68 lists five "tried and taken back" experiments, each one a full chain on the worker clones.

### 1.4 The measured signal is binary and saturated

A cell is judged on "wins 3/3, 3.0 stars, base 700". In the round 67 table most cells read exactly that before and
after. Win/lose over three seeds can only take four values, and since the probe is deterministic (round 25: byte-identical
across repeats) the seeds mostly agree. A level that is won with 5 % to spare and one won with 80 % to spare look the
same. That is why a small lever move flips cells from 3/3 to 0/3 without warning ("Glass Meadows Medium has no winning
plan" after an armour-growth change), and why nobody can say how far a passing cell is from failing.

The one place that measures a continuous margin is the finale probe: it bisects boss health to "holds x1.18". It is the
most informative number in the pipeline, and it only exists for six bosses.

### 1.5 The acceptance gate mixes difficulty with design wishes, and its conditions conflict

`AcceptanceRules` (773 lines) asks roughly twenty things of a cell at once: obvious rows must lose, must fall before the
last third, the best plan must win N of 5 with M stars, must beat the obvious runs by a star or 25 % base, must use two
"features", replayed plans must be "distinct", Hard must be measurably worse than Medium for the same row, and so on.
Result: **8 of 24 pass**, while a different summary of the same runs says **24 of 24** have a winning plan
(`Plans/README.md`). Two verdicts, same data.

The conditions also contradict each other and the design:

- STATUS round 68, open decision 1: "The Easy acceptance rule and the grace window disagree."
- The Cannon row "must lose" (acceptance), but COMBAT_RULES says the Cannon is on-role for everything Landing Site sends,
  "not a rule break". The gate fails a cell for obeying the rules.
- Exceptions are accumulating as tables: `TutorialCells`, `ForgivingHardCells`, 15 grace `Allowances` (33 allowed waves),
  `PaperHeldBosses`. Each is a place where the criterion could not be met and was excused.

A gate that fails two thirds of the cells, and is shipped past every round, has stopped informing anyone. Its count moves
10 -> 7 -> 8 for reasons unrelated to how hard the levels are.

### 1.6 The referees disagree and there is no rule for who wins

Paper ledger, mono rows, naive bot, affordable bot, creative bot, searched plans, finale probe. Examples from round 68:
searched plans win Quartz Flats Hard 3/3, the creative bot loses 0/3. Rotbloom: play says 3550 health is nine times too
little, the model says 4800 is the most (6.8x apart). When referees disagree the pipeline has no tie-break, so the
outcome is an "open decision for Remco". The process is built to emit open questions.

Also: the plan search optimises against the paper model and the best paper plans are then replayed. Picking the maximum
of a noisy estimate selects for the model's errors (round 27: 89 runs of "paper survives, game lost").

### 1.7 No human is measured anywhere

The link from bot to human is `HumanUtilisation = 0.34` against the bot's 0.27. Its source (BALANCE.md, 2026-11-04) is
one remark by Remco that he had "two or three times" the firepower needed. There is no play log, no recorded human run,
no code that records one. Meanwhile Remco's playtests keep finding things the pipeline did not (round 61: a simple gun
killed flyers; round 64: Laser and attack drone melted armour). So even a perfect bot pipeline would answer "are the bots
balanced", and the honest answer to "is it balanced for a player" stays "not sure". That part is not a machine-time
problem and more machine time cannot fix it.

### 1.8 Every new mechanic resets the chain

Each mechanic (armour rule, high ground, growth windows, sky gun) needs a matching model change. That changes the
`ShapeHash`, the calibration goes stale, measure -> fit -> apply -> solve -> measure -> search -> replay -> acceptance runs
again, and every factor moves (1.2). Rounds 61 to 68 are eight such resets in four days. The hours are spent keeping two
implementations in step, not on learning about the levels.

### What is *not* the problem

- **Simulation noise.** The probe is deterministic with fixed steps and seeds (round 25).
- **Simulation speed.** 15 to 20 s per run headless, 432 runs in about half an hour on five clones (round 25 timings).
- **The rules layer.** `CombatRules.asset`, the role matrix and `CombatRulesTests` are exact, cheap and explainable.

---

## 2. A better way

### 2.1 Principle

1. **One implementation of the rules: the game.** Never predict play with a hand model when the game itself can be asked
   in 20 seconds. A model may *suggest* a starting value; it never gives a verdict.
2. **Measure a continuous margin, not win/lose.**
3. **Keep three questions apart**, each with its own cheap check: are the *rules* right (per coin, per role), how *hard*
   is this level (one number per policy), and does a *human* feel it that way (play logs).

### 2.2 The metric: headroom H

For a level, a difficulty and a fixed reference player (a "policy"):

> **H = the largest multiple of the waves' health at which that policy still wins.**

Found by bisection in the real game, exactly as `FinaleProbeTests` already does for bosses (`Scaled(waves, multiple)`,
`EnemySpawner.WaveHealthScale` exist). H = 1.30 means the policy wins with 30 % to spare. H = 0.85 means the waves would
need to be 15 % softer for it to win.

Why this fixes the listed problems:

- **Continuous and explainable.** "A careless player has 0.8, a sensible player 1.25, the best known plan 1.9" is a
  sentence Remco can check against his own feel.
- **No model, no calibration, nothing to go stale.** A new mechanic needs no model change. It simply shows up in H.
- **Tuning becomes arithmetic.** To put the sensible player at 1.25 where it measures 1.60, multiply the level's scale by
  1.60 / 1.25 and re-measure once. That is what `wave_tuning.txt` scales already do, without the detour.
- **Known resolution.** Bisect to 5 %. A change smaller than that is "no measurable change"; that is a statement, not a
  doubt.

Assumption to verify on day one (stage 1): that winning is monotone in the health multiple (one crossover, not several).
It should be, because kill money follows threat and not health ("tougher creatures pay no more"), but it must be shown.

### 2.3 The reference players: a small fixed ladder

Three policies that read **no balance model** (so they never move between rounds), all of which exist today in
`ProbeStrategy` / the yardstick family:

| Rung | Policy | Exists as |
| --- | --- | --- |
| Careless | owned types in turn on the nearest free spots, random upgrades | `naive` |
| Sensible | buys the counter when a trait appears (follows Voss's hint), upgrades before a second tower, coverage spots | `affordable` plus a role rule; small new work |
| Skilled | best of the known scripted plans for the cell (and Remco's own recorded builds, see 2.6) | `ProbeStrategy.Scripted` |

Plus diagnostic rows that are reported, not gated: one mono row per owned tower.

The player's save (which permanent upgrades are owned) stays an assumption per difficulty, as `Progression` defines it
today. Report H for that expected save and for a fresh save, so the size of that assumption is visible.

### 2.4 What "balanced" means, as numbers

A target band per difficulty on the ladder. The values below are a **proposal for Remco to set**, not a finding:

| Difficulty | Careless H | Sensible H | Skilled H |
| --- | --- | --- | --- |
| Easy | 1.1 or more | 1.5 to 2.0 | (not judged) |
| Medium | below 1.0 | 1.15 to 1.4 | 1.5 or more |
| Hard | below 0.8 | below 1.0 | 1.1 to 1.3 |
| Psycho | (not judged) | below 0.8 | 1.0 to 1.15 |

Three hard gates only:

1. The row marked as the target for that difficulty sits in its band.
2. Skilled H is at least 1.0 in every cell (the level can be won).
3. For the same policy, H falls from Easy to Psycho (each difficulty is harder).

Everything else becomes a **reported ratio, not a pass/fail**:

- Role pressure: `H(mono row without the counter) / H(sensible)`. "The Gun row has 0.55 of the sensible player's
  headroom on Shard Ridge" replaces "falls 6 waves late, allowed". No allowances table.
- Dominance: `H(best mono row) / H(skilled)`. The Cannon row on Landing Site becomes "0.95: one tower is enough here", a
  design fact Remco can accept for a tutorial or not, instead of 17 FAIL lines.
- Shape of the level: per wave, from the same runs, **where the wave died** (furthest progress along the road of any
  creature, 0 to 1), gate damage and base damage. The wave where that line peaks is the pinch. A wave that sticks out is
  fixed by that wave's own multiplier. This is the measured version of the paper "pressure curve".

### 2.5 Cost, on today's machinery

Estimates from the round 25 timings (17 s per run, five clones):

| Job | Runs | Wall clock |
| --- | --- | --- |
| One policy, one cell, H to 5 % (bracket + 6 halvings, 3 seeds at the edge) | about 10 | 3 min on one clone |
| One level, four difficulties, three policies | about 120 | about 7 min |
| All 24 cells, three policies (after a global change such as a price or an armour share) | about 720 | about 40 min |
| Rules-only question (is a tower still on-role per coin) | 0 | seconds (`RoleMatrix`, EditMode) |

That replaces measure + fit + apply + solve + measure + search + replay + acceptance + grace. More important than the
minutes: it is one pass with no judgement calls in the middle, so it does not need an agent iterating for a night.

### 2.6 Humans: the part that cannot be computed

Player skill varies by a large factor. The project's own numbers: the same waves read 2.4x surplus for a fresh save and
3.6x with Remco's upgrades. No method turns that into one "correct" difficulty. What can be done:

1. **Record real plays** (a local file per level played: per wave the towers built and where, money, where the wave died,
   base health, result; plus one question at the end: too easy / fine / too hard). This is a new small feature and needs
   Remco's approval.
2. **Replay each recorded build as a scripted plan** and bisect it. That gives *Remco's H* on that level: it places a
   human on the same ruler as the bots.
3. After about ten rated levels: which rung Remco plays like, and which H he calls "fine". The bands in 2.4 are then set
   from his feel rather than proposed by me. This is the only calibration the method needs, and it is of the *target*, not
   of a model.
4. With outside playtesters later: first-attempt win rate per cell. With N attempts the 95 % interval is about
   +/- 1/sqrt(N): 25 attempts give +/- 20 points, 100 give +/- 10. A cell off its target is corrected by one multiplication
   of its scale.

So balance is stated as: *"On Medium a sensible player has 25 % headroom, a careless one loses around wave 9, the best
plan has 70 %."* That is certain (measured, reproducible to 5 %) and actionable, without claiming to know how a stranger
will do.

### 2.7 Generating maps

A generated map needs a *first guess* of its scale and then one measurement.

1. **Features, from code that exists and is worth keeping:** `LevelShape` and `LevelGeometry` (road length, trunk share,
   fronts, gates, route cells in reach of each build spot), `Threat` (effective health of a wave), `RoleMatrix`
   (damage per coin per trait).
2. **A first-order estimate R:** affordable firepower times time in reach, over wave effective health. The existing
   ledger margin can serve as R as it stands. Its rank correlation with play (0.75 to 0.80) says it is a usable *feature*
   even though it is not a usable *judge*.
3. **A two-number fit:** `log H = a + b log R` over the cells measured so far (24 today). Report the residual spread.
   That spread is the honest predictability of a generated map *before* it is measured.
4. **Close the loop by measurement:** generate, set the scale from the prediction, bisect once (7 min), correct the
   scale, done. Prediction error never ships because the last step is always a measurement. A generator can also reject a
   map whose shape numbers are off (dominance ratio near 1, pinch at wave 2, skilled H below 1 at any scale).

This is the difference with today: the generator does not need the model to be right, only roughly right.

### 2.8 A fast engine-free simulator: later, and only as a refactor

A pure C# simulation of the same rules would turn 17 s into well under a second per run (my estimate, unmeasured) and
allow search and generation at scale. Two cautions:

- Gameplay is about 47,000 lines over Combat, Towers, Enemies, Levels, Hero, Drones and PathObjects with about 90
  per-frame methods. It barely touches physics (one file), which helps, but this is weeks of work, not a day.
- A *port* would be a third implementation and would recreate problem 1.1. It is only worth doing as a **refactor where
  the game itself runs on the engine-free core** (CLAUDE.md architecture rule 2 already asks for logic without engine
  references), proven by trace parity: same seed, same per-wave records in Unity and headless.

Decide this after stage 3, when it is known whether 7 minutes per map is a real limit.

### 2.9 Keep, freeze, drop

| Keep | Freeze (use, stop extending) | Drop |
| --- | --- | --- |
| `CombatRules.asset`, `RoleMatrix`, `CombatRulesTests`, `UpgradeMargin` price rule | `Ledger`, `LevelGeometry`, `LevelShape`, `Threat`: as the first-guess estimate R and for wave composition | The calibration fit as a gate (hold-out bar, `apply-force`, per-tower efficiencies, route fire, queue guard, siege factor) |
| Deterministic probe, worker clones, `ProbeStrategy` family, scripted replay | `WaveComposer`, `WaveScripts` (who comes when); `WaveSolver` only as the starting scale | Plan search on paper (`PlanSearch`, `StrategySearch`) and the search -> replay loop |
| Finale probe bisection (generalise it: it *is* the new method) | `BalanceChecks` bands as a runaway alarm | `AcceptanceRules` as a pass/fail count; grace-window allowances; tutorial/forgiving exception tables |
| `wave_tuning.txt` levers (they are the real tuning knobs) | | The paper impact calculator as a verdict (keep it only as a 6-second hint of which cells to re-measure) |
| Per-wave records the probe already writes | | Extending the paper model for each new mechanic |

---

## 3. Staged plan

### Stage 1 (one day): prove the metric on existing machinery

- Generalise the finale probe's bisection to an ordinary level: policy x cell -> H, written with the per-wave "where it
  died" line. No game code changes; test code only.
- Run it on two levels (Landing Site and Quartz Flats), four difficulties, the three ladder policies plus the mono rows.
  About 300 runs, about 20 minutes of machine time.
- Check four things:
  1. **Monotone:** scan eight multiples for six policy-cells; exactly one win -> lose crossover each.
  2. **Reproducible:** H repeats within 5 % across seeds 1 to 3 and across two workers.
  3. **Sensible:** H agrees with what is known (mono Cannon on Landing Site near the best plan; off-role rows well below
     the sensible player; searched-plan wins that were 2/3 sit near 1.0).
  4. **Rescale works:** change one cell's scale by H / target, re-measure, land within 5 % of the target.
- **Stop rule:** if 1 or 2 fails, the metric is not usable as defined and this proposal needs rework before anything
  else is built on it.

### Stage 2 (two to three days): the table replaces the gate

- H table for all 24 cells, one command, about 40 minutes. One page for Remco: per level and difficulty the three H
  values, the role and dominance ratios, the pinch wave.
- Remco sets the target bands (2.4) from the cells he has played and has an opinion on.
- Rescale the cells that are off; re-measure those only.
- From here a round ends with a table and a diff of the table, not with a pass count.

### Stage 3 (two to three days): humans on the ruler, maps from data

- Play recording and the end-of-level question (needs approval; small).
- Replay Remco's recorded builds, bisect, place him on the ladder; adjust the bands once.
- Fit `log H = a + b log R` on the 24 cells; report the residual. Then build one new level from data, predict, measure,
  correct, and write down the prediction error. Repeat for the next four levels: five real out-of-sample errors.

### Stage 4 (optional, weeks): engine-free core

Only if stage 3 shows that measurement time, not design time, limits map generation.

### What "certain" means, in numbers

| Claim | Target | How it is known |
| --- | --- | --- |
| A measured H is reproducible | within 5 % (the bisection step) | stage 1, check 2 |
| A tuned cell sits in its band | band width 20 % or more, measured to 5 % | stage 2 |
| Effect of a global change | delta H per cell, to 5 %, in 40 minutes | stage 2 |
| A new map's scale before measuring | unknown today; aim for a residual of 15 to 20 % (one standard deviation). If it is 40 %, say so: generation then needs the measurement step every time, which still works | stage 3 |
| A new map's scale after one measured correction | within 5 to 10 % of target | stage 3 |
| "A human finds it right" | Remco: after about ten rated levels. Strangers: only from playtest data, +/- 10 points of win rate per 100 attempts | stage 3 and later |

The first three rows can be certain within a week. The last row cannot be made certain by any calculation, only by
people playing.

---

## 4. What I would stop doing now

1. Refitting the calibration and forcing it past its own bar. A check that is overridden every round is noise.
2. Treating "acceptance N of 24" as the result of a round.
3. Searching plans on paper and replaying them to see whether the paper was right.
4. Adding model terms for new mechanics (petals for the Rotbloom, hero blocking, attackers per gate). Measure instead.
5. Adding allowance and exception tables. If a criterion needs 15 excuses, report the number and let Remco judge it.
6. Running the full chain for a level-local change. One level is seven minutes.
7. Asking the machine whether the game is fun or fair for a person. Ask it for headroom; ask Remco for the target.

## 5. Caveats on this review

- I did not run anything. The H metric's monotonicity and its 5 % reproducibility are expectations, tested first in
  stage 1 with a stop rule.
- Run counts and minutes are derived from the round 25 timings and a bisection of about ten runs; real levels at high
  multiples end early (faster), long Psycho wins take longer.
- The target bands in 2.4 are placeholders so the table has a shape. They are Remco's to set.
- A uniform health multiple does not capture every way a level can be hard (a single air raid with no sky gun owned is a
  wall at any multiple). Those show up as "skilled H below 1 at every scale" and need a composition change, as today.
- I read the method sections and rounds 22, 25, 27, 67 and 68 of BALANCE.md closely, and rounds 61 to 66 only by their
  headings and the summaries in later rounds.
