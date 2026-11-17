# Balance critic, 2026-11-17

Independent read of balance stage 1 and 1b (Docs/BALANCE_HEADROOM.md, Tools/PlayTest/pressure.txt, headroom.txt). Read-only:
nothing was run, no code or data changed. Numbers are from `pressure_records.txt`, the wave and level assets, and the code
lines named. Where something is inferred and not read from a record, it says so.

## Summary of verdicts

| Stage 1b claim | Verdict |
| --- | --- |
| Landing Site wave 7 nest draw causes the seed spread | **Correct**, and now checked on the map. But it is not a new finding and not really "noise": it is a known, decided feature that the probe's players cannot see. |
| Shard Ridge Easy is harder than Medium because of raid timing and purse | **Half right.** The timing facts are correct. The cause is the bot, not the Easy data: a Rocket bought one wave after the raid still wins with a full base. |
| Shard Ridge Hard and Psycho are a wall for a sensible player | **Over-claimed.** The bot buys exactly one sky gun and never a second. That rule, not the level, explains most of the 0 of 5. |
| Sensible bot forgets alive flyers, repairs starve the counter | **Correct** (first one confirmed in code, second plausible), but a third and bigger fault was missed: one counter is "enough" forever. |
| "Lost at x0.5 = structural" | **Misleading as worded.** 6 of the 8 rung-cells that lose most seeds at x0.5 are bot faults, 2 are the nest draw. None is a proven level fault. |
| Do not gate on P or H with percentage bands | **Agree.** |

## 1. Are the stage 1b conclusions sound?

### 1a. Landing Site wave 7 nest draw: correct, with three corrections

What the data and code say:

- `Level_C1_01.asset` `_nests`: (17,1) and (10,8), both element 7. First gate at (14,5), 13 % of the road; far gates at
  (21,17) and (28,17), 87 % and 93 %. (The doc says two gates; there are three.)
- `OffRoadRoute.NearestRoadCell` sends a nest walker to the nearest road cell. (17,1) joins at x = 17 on the first leg,
  **three cells behind the first gate**. (10,8) joins at x = 10, four cells in front of it.
- Wave 7 is the only wave with a nest group, on every difficulty: 1 Cragback on Easy, Medium and Hard, 2 on Psycho
  (`Waves_C1_01.asset`, `Source: 4`). `NestAssignment.Draw` (NestAssignment.cs:25-50) draws per creature, so it is one coin
  flip on three difficulties.
- Records, wave 7 reach at x1: seeds 1 and 2 = 0.12 to 0.15 on Easy, Medium, Hard (every rung); seeds 3, 4, 5 = 0.86 to
  1.00. On Psycho all five seeds read 0.86 or more (two draws, so at least one lands behind). Medium skilled, seeds 3 to 5:
  lost at x0.5 and x1 at the same game time (553.7 s and 553.8 s), far gate lost 24 + 976, base lost 588. One Cragback,
  alone, 220 s after the last wave.

So the mechanism is right. Corrections:

1. **It was already known and decided.** STATUS.md round 31 (line 4606) describes exactly this trace, and round 34
   (line 5120) names (17,1) as the rear nest. Remco kept the rear rift on purpose on 2026-11-10 and asked for a warning
   (`RearNestWatch`, the "BEHIND THE GATE" marker, Voss's lines). The stage 1b note "which nest is behind the gate was not
   checked" was one grep away.
2. **For a human it is telegraphed.** `OriginDirector` marks every origin a wave will use from the first second of the
   level, and the warning plays the first time. The fixed scripts and the bots read none of that. The seed spread is
   therefore mostly a blind ruler, not an unfair level. The "skilled" Medium script simply was recorded on a front-nest seed.
3. **What is a real problem is smaller and different:** with one creature, the draw decides whether the lesson exists at
   all. Half of the players never meet the rear rift on their first Landing Site run, and the once-per-save warning then
   fires somewhere else or on a replay.

Side finding: (10,8) is exactly three cells from both the first leg (y = 5) and the third leg (y = 11). The game breaks the tie
by `HashSet` iteration order (OffRoadRoute.cs:55-67, strict `<`); it lands on the first leg today (records: 9 % join). If
that order ever changes, this nest joins at 58 % of the road. `LevelShape.Nearest` uses an ordered list, so the model and
the game can disagree here without warning.

### 1b. Shard Ridge Easy against Medium: the facts hold, the conclusion does not

Confirmed in `Waves_C1_03.asset`: Easy's first flyers are 4 Skyrays in wave 12 (wave multiplier 0.6), none before. Medium
has 4 in wave 11 at 0.242, then 5 in wave 12 at 1.403. Easy profile: income x0.8, starting money x0.8.

But the records say the raid is not what kills Easy:

- Sensible, Easy, seed 2, x1: Rocket bought in **wave 13**, one wave after the raid. Result: won, base 700 of 700, no
  base damage at all. Seed 5 at x0.5: Rocket in wave 14, won with 177 left.
- Seeds 1, 3, 4, 5 at x1: no Rocket ever, base lost in waves 13 and 14. Seed 1's own build order shows three purchases in
  wave 13 (an upgrade, a 50-coin Gun Turret, another upgrade) while the Skyrays were on the base. The purse is not
  recorded, so "it could have afforded 120 then" is inferred, but it is the likely reading.
- Easy's 4 Skyrays carry about 33 health each (80 x 0.68 x 0.6), 130 in total. They sit on the base and nothing the bot
  owns reaches them there.

So Easy is lost because the bot stops thinking about flyers when the preview moves on
(`ComingCounterTraits`, BalanceProbeTests.cs:2533-2551, reads wave definitions only, never what is alive). A player who
reacts one wave late still wins untouched. "Easy needs an introduction wave" is not shown by this data. It may still be a
good idea for feel, but the probe gives no evidence for it yet.

One thing in the data does look wrong and was not flagged: on **Hard** the first raid (wave 11, 5 Skyrays) comes at
multiplier 1.106 and the second (wave 12, 6 Skyrays) at 0.212. Medium has the soft wave first. This looks like the soft
introduction landed on the wrong wave on Hard. Inferred from the pattern; worth one look in the wave script.

### 1c. Sensible-bot faults: two confirmed, one missed

- **Forgets alive flyers:** confirmed in code (above).
- **Repairs starve the counter:** plausible. `SensibleAct` returns without spending when the counter is not affordable
  (line 2514), and the repair step (lines 1494-1508) runs on its own rule. Landing Site Hard, seed 1: 3 guns, 213 coins on
  repairs, 601 earned. I did not trace the call order further.
- **Missed: one counter is enough forever.** Line 2489: the rule is skipped as soon as any living tower answers the trait.
  Records, Shard Ridge at x1: the sensible bot owns one Rocket on Medium (5 of 5 seeds), one on Hard (4 of 5), one on
  Psycho (5 of 5). The skilled script owns 3 on Medium and Hard and 5 on Psycho. Hard's raids are 5, 6, 8 and 10 Skyrays.
  One level-1 Rocket against ten Skyrays is the "wall".

That changes finding 3 of stage 1b. The skilled script on Hard takes 96 to 232 base damage per run in waves 12 and 13 and wins
5 of 5 up to x1.25. That is chip damage, not a wall. Whether Hard is too hard for a sensible human is unknown until the bot
buys a second and third sky gun.

### Other claims

- "The score is two-valued because the game is": agree, and it points at the maps. Every level has its first gate at 10
  to 14 % of the road and the next one far behind (Landing Site 87 %). Everything piles up and dies at gate 1 until the
  gate falls, then nothing stands in the way.
- "A reactive bot is no noisier than its frozen build order": true, which makes the `typical` rung redundant.

## 2. Is "seeds won out of 5 at x0.8 / x1 / x1.25" a usable ruler?

**Yes, as a coarse traffic light for fixing structure. No, as a measurement.**

Real resolution:

- **In health:** one grid step, about 25 %. Stage 1's fine scan found flip-back zones 5 to 41 % wide, so a finer grid
  buys nothing.
- **In seeds:** 5 of 5 only says the true win rate is above about 55 % (95 % confidence); 0 of 5 says below 45 %. Only
  "all" and "none" are distinguishable. 2, 3 or 4 of 5 all mean "mixed".
- **The seeds are not five independent measurements.** With the nest draw out of the picture, Landing Site seeds agree to
  about 2 %. Five identical runs give false confidence. The real uncertainty is the policy: a small change in health
  reorders its purchases and flips the result.

What it misses:

- Whether a loss is the level's fault or the player's. "Lost at x0.5" fired for 8 rung-cells (2 seeds or fewer won):
  2 trace to the nest draw (skilled Medium, careless Hard), 6 to the bot rules or to the script frozen from them.
- Anything a human sees and the bot does not (marked origins, flyers on the base, a gate about to fall).
- Role rules: when an off-role row falls. Keep `CombatRulesTests` for that; the ruler does not replace it.
- Dominance, unless the mono rows are run. Difficulty order, except coarsely. "Won but ugly" (keep the base-hit line).

Cheapest improvements, in order:

1. **Repair the reference player before anything else** (section 3, item 1). Every "sensible" number so far measures the
   bot. This is test code and costs no design decision.
2. **Drop the `typical` rung.** Same median as `sensible` in 7 of 8 cells. Saves a third of the runs.
3. **Record the purse per wave** in the run record. Two of the three stage 1b explanations rest on an inferred purse.
4. **Run x0.5 only for cells that lose 4 or more of 5 at x0.8**, and call the flag "inspect the build order", not
   "structural".
5. **Spend the saved runs on health jitter, not more seeds:** x0.8, x0.9, x1, x1.12, x1.25 on 3 seeds (15 runs, as now)
   and read "won N of 15 in the band". That samples the flip-back fragility, which is the real noise.
6. **Re-record the skilled scripts** after the nest fix, so no script is blind to a rear rift.

**Is a random nest draw a feature or noise?** Both, depending on the count. It is fair variety when (a) the origin is
marked from the start, which it is, and (b) many creatures draw, so the split averages out. It is noise when one or two
creatures draw and the outcome is all-or-nothing, as on Landing Site. Rule of thumb for data and for a future generator:
**a nest group that decides whether a route is used at all must be authored, not drawn.** Make the draw fair by removing
it there, not by making both draws equally defendable.

## 3. Fix plan for the next agent

Data goes through the generator and the wave scripts (`ProjectGenerator.*`, `Tools/PlayTest/wave_tuning.txt`,
`WaveScript`), never by editing an `.asset` by hand. Verify with
`PR_MULTIPLES="0.8,1,1.25" PR_EXTRA="" PR_POLICIES="skilled,sensible" bash Tools/PlayTest/pressure.sh "<nodes>" "<difficulties>"`
(node 0 = Landing Site, node 2 = Shard Ridge) unless another command is given.

### Priority 1: repair the sensible bot (test code, blocks everything else)

| Change | Where | Why |
| --- | --- | --- |
| Count living flyers and armoured creatures as "coming" | `ComingCounterTraits`, BalanceProbeTests.cs:2533 | Forgets Skyrays on the base. |
| Scale the counter: buy or upgrade another one when the base or a gate lost health to that trait in the last wave, or when the preview shows more of the trait than last time | `SensibleAct`, line 2489 | One Rocket forever. |
| While saving for a counter, repair only the defence gate and only when it is wrecked | repair step, lines 1494-1508 | Repairs eat the purse. |
| React to a marked rear origin: one gun in reach of the road behind the gate before that wave | `SensibleAct`, reading `RearNests.BehindGate` and the origin states | A human is warned; the bot is not. |
| Write the purse at each wave start into the record | `BotRun`, `PressureProbeTests` | Evidence instead of inference. |

Expected effect: Shard Ridge Easy sensible from 1 of 5 to 5 of 5 at x1; Landing Site Hard from 1 of 5 to mostly won;
Shard Ridge Hard and Psycho unknown, which is the point.
Pass: the bot buys a Rocket on Shard Ridge Easy on 5 of 5 seeds no later than wave 13, owns 2 or more Rockets by wave 14 on
Hard, and no run ends with more coins unspent than the counter costs while its target is alive.
Then re-run both levels, all difficulties (about 240 runs, 12 minutes). **Decide items 3 and 4 below only on that table.**

### Priority 2: Landing Site wave 7 nest (data, small)

- **Change:** make the wave 7 Cragback always use the rear rift. Data-only route: remove nest (10,8) from Landing Site's
  layout nests, so (17,1) is the only candidate. It has no other user: wave 7 is the level's only nest group. Keep the
  rift terrain as scenery if the validator allows it.
- **Why:** Remco chose the rear rift as a lesson. A lesson that happens on half the runs is not a lesson, and it is the
  one source of seed spread on this level. It also removes the tie-break at (10,8).
- **Expected:** every seed reads wave 7 reach 0.86 or more. The old skilled Medium script loses 5 of 5 (it has no rear
  gun), so re-record it. Sensible and the Cannon row stay 5 of 5 on Easy and Medium (both already hold a gun at (19,14)).
- **Pass:** wave 7 reach the same on all five seeds; seed spread of P at x1 under 5 % in the cells every seed wins;
  re-recorded skilled wins 5 of 5 at x1 on Easy and Medium.
- **Ask Remco:** a single Cragback that nobody shoots ends the level by itself at half health. Is that the intended
  price of ignoring the warning on Easy? If not, the gentler version is the same creature at a low wave multiplier on Easy.
- If a code change is acceptable later: an optional fixed nest index on `SpawnGroup`. Needs approval; not needed now.

### Priority 3: Shard Ridge Easy first raid (wait for the repaired bot)

- **Do nothing to the data first.** The records say a late Rocket still wins untouched.
- If the repaired bot still loses 2 or more of 5 at x1: raise `FirstAirWaveFloor` for Easy by one wave (the existing
  "Easy raid rule") or give Easy the single soft flyer Medium has in wave 11. One of the two, not both.
- **Pass:** sensible 5 of 5 at x0.8 and x1 on Easy, and Easy not behind Medium at any multiple.

### Priority 4: Shard Ridge Hard and Psycho air raids (wait for the repaired bot)

- **First:** check the Hard script: wave 11 at 1.106 and wave 12 at 0.212 look swapped. If so, fix that alone and re-measure.
- **If the repaired bot still wins 0 of 5 at x0.8:** thin the late raids, not the health. Hard and Psycho stack raids in
  waves 14 and 15 (8 then 10 Skyrays); Medium has one (9 in wave 15). Drop the wave 14 raid on Hard, or cut both by a
  quarter. Use the script's raid levers.
- **Why not health:** ten Skyrays deal up to about 57 damage a second (10 x 8 every 1.4 s, before the difficulty's structure factor) to a 700-point base. The level is lost in seconds by a
  player one sky gun short, at any health multiple. That cliff is the problem.
- **Pass:** Hard: sensible wins 3 or more of 5 at x0.8 and loses most at x1.25; skilled stays 5 of 5 at x1. Psycho:
  skilled 5 of 5 at x1, sensible may lose. Base-hit line: the raid costs base health without ending the run on most seeds.
- **Design question for Remco (not for the agent):** flyers that reach the base stay and hit it until killed. A softer
  base attack for flyers would give the game the survivable, counted damage the ruler needs. It is a rule change.

### Priority 5: the Cannon row on Landing Site (needs one decision)

Use the rules, and stop retrying what they already rule out:

- **Armour mix:** no. The Cannon is the anti-armour gun.
- **Spread formations:** no. Round 68 tried a loose Skitter stream. Everything stops at gate 1 and becomes a pile.
- **Swarms:** no. A shell takes at most 3 creatures every 1.67 s, so one Cannon kills up to 1.8 small bodies a second
  whatever its level. A nine-Cannon row clears about 16 a second; the densest Skitter group arrives at 5 a second. Rounds
  64, 65 and 68 measured the same outcome.
- **Flyers earlier:** yes, this is the only option the rules predict will work, because "cannot aim up" is a target rule
  and not a number. Pick: a small sprinkle of bats (Dustbat, 31 health, swarm) on **Hard and Psycho only**, from wave 6 or
  7, sized so two or three Gun Turrets handle it at their 0.2 share. Not Skyrays, so Voss does not advise a Flak the
  player may not own.
- **Easy and Medium:** accept the Cannon row. It is on-role for everything a first-visit player meets, and this is the
  tutorial. Report it as dominance, not as a failure.

Needs Remco's yes: STATUS round 68 lists flyers on Landing Site as "not allowed by the brief".
Verify: `PR_UNITS` with `0:2:mono-Cannon:<seed>` and `0:3:mono-Cannon:<seed>` for seeds 1 to 5, at 0.8, 1, 1.25, plus
skilled and sensible on the same cells.
Pass: Cannon row 0 of 5 at x1 on Hard and Psycho, base hit by the bat wave; a row of Cannons plus Gun Turrets and the
skilled script still 5 of 5 at x1; `CombatRulesTests` green.

### The same patterns in the other levels (read from data, not measured)

| Level | Pattern | Evidence |
| --- | --- | --- |
| C1_02 Glass Meadows | None of these. One nest, no nest groups, no flyers. Cannon row known to win Easy, Medium, Psycho. | wave asset; COMBAT_RULES section 6 |
| C1_04 | Small nest groups drawn over **three** nests (2 to 5 Shalebats in wave 7), two nests equidistant from two road legs (the tie-break above). Easy's first flyers are 2 from a nest in wave 7; Medium has a single soft one in wave 3. | `_nests` (37,16), (30,11), (22,11) |
| C1_05 | Nest (20,7) joins at 50 % of the road, just behind gate 2 at 49 %, with a near tie. Easy has no soft first flyer (4 Shalebats from a nest in wave 8 at 1.319); the other difficulties have one in wave 4 at about 0.34. | wave asset |
| C1_06 | One nest, so no draw. Easy, Medium and Hard start air with 3 to 6 Shalebats in wave 7 and no soft wave before. Medium wave 12 sends 11. | wave asset |

Bats carry 31 health against the Skyray's 80, so these are milder than Shard Ridge. Measure them only after the bot is repaired.

## 4. What to drop from the method

1. **P as a score.** It is win or lose plus a small term. Keep its parts as diagnostics: the per-wave advance and the
   base-hit line.
2. **Bisection and H to 3 %** (stage 1). The edge is not a stable number.
3. **The `typical` rung.**
4. **The careless bot as a rung.** It carries its own dice and is not ordered below the others.
5. **Stop rules on seed spread.** Spread across seeds is a property of the level's draws, not of the ruler.
6. **"Lost at x0.5 = structural" as a verdict.** Keep it as a prompt to read the build order.
7. **The percentage bands of BALANCE_METHOD_REVIEW 2.4** and the 24-cell H table of its stage 2.
8. **More composition experiments against the Cannon row** inside Skitters and Cragbacks. Four rounds, same result.
9. **Explaining a result from a build order without the purse.** Record it first.

## Caveats

- Nothing was run. Every expected effect in section 3 is a prediction to be checked with the pass criteria.
- The "Hard wave 11 and 12 swapped" reading and the "seed 1 could afford the Rocket in wave 13" reading are inferences.
- I did not trace the order of the repair step against `SensibleAct`, nor what the raid levers do to each wave.
- The other four levels were read from their assets only.
