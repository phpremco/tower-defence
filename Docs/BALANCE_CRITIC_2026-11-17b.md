# Balance critic 2, 2026-11-17 (rounds 74 and 75)

Independent read of round 74 (e61315f3) and round 75 (f1bc6623). Read-only: nothing was run in Unity, no code or data
changed. Numbers come from `Tools/PlayTest/pressure_r74_*_records.txt` and `pressure_r75_*_records.txt` (read with Python),
the diffs of both commits, `health_per_coin.py` and the assets. Where something is inferred, it says so.

## Verdicts in short

| Question | Verdict |
| --- | --- |
| 1. Sensible bot a credible human? | **Not yet.** Much better than stage 1b, but it reads a wave preview the game does not have, opens by spending everything on the dearest gun, and its new sell rule fires in 240 of 240 flyer runs, also where it is not needed. The sell rule hides a real level property on Fault Line and Quartz Flats. |
| 2a. Landing Site rift authored | **Justified. Keep.** The harder Hard and Psycho readings are the honest level, not a regression. |
| 2b. Three builder scales | **Weakly justified.** Health per coin is not a valid yardstick; the before/after on the sensible rung is confounded with a bot change. Crysium Vein Medium overshot, Fault Line Medium's cut fixed nothing. |
| 3. "Noise" on Shard Ridge Hard | **Not noise. The bot changed between batches.** The probe is deterministic. This invalidates the "one or two seeds either way are noise" rule in STATUS round 75. |
| 4. Open questions | 6 are Remco's, the rest the agents can decide. List below. |
| 5. Next round | Freeze the bot, separate bot and data batches, build the affordability check, make the skilled rung adaptive, re-run the old gates. |

## 3. The "noise" first, because every table depends on it

STATUS round 75 says Shard Ridge Hard at x1.25 read 4, 2 and 2 "in three batches on unchanged data" and concludes that
one or two seeds either way are noise. The records say otherwise. Sensible bot, Shard Ridge Hard, x1.25, base health at the
end per seed 1 to 5:

| Batch | Seed 1 | 2 | 3 | 4 | 5 | Won |
| --- | --- | --- | --- | --- | --- | --- |
| r74_baseline (played twice, different workers) | 116 | 388 | 116 | 28 | lost | 4 |
| r74_final | 116 | 388 | 116 | 28 | lost | 4 |
| r75_bot | 116 | 388 | 116 | 28 | lost | 4 |
| r75_data | lost | 372 | lost | 252 | lost | 2 |
| r75_final | lost | 372 | lost | 252 | lost | 2 |

- Four batches on different workers agree to the health point. Then everything shifts at once and stays shifted.
- Between r75_bot and r75_data the build order differs in **60 of 60** Shard Ridge runs, although `Waves_C1_03.asset` was
  not touched. So the bot's code changed between those batches. In r75_bot no Shard Ridge run sells; from r75_data on every
  run sells in wave 10 or 11. That is the second version of the sell rule ("a gun that can aim up must reach the base"; the
  first version did not sell while a Gun Turret stood anywhere).
- Across all batch pairs, about 430 runs have an identical build order in two batches. Outcome flips among them: **0**.
  Different end health: 1 (Shard Ridge Easy, seed 1, x0.8: 656.8 against 642.4).

Consequences:

1. **The probe is deterministic per code, data and seed.** Every changed cell in a table is a real effect of a code or data
   change. "Not bold = noise" in the round 75 table is wrong. Shard Ridge Hard 5 5 4 to 5 5 2, Crysium Vein Hard 5 5 5 to
   5 5 3 and Quartz Flats Hard 0 0 0 to 4 0 0 are all bot edits.
2. **The data change of round 75 was measured together with a bot change** (r75_bot to r75_data moved both). On unchanged
   data that step alone moved Quartz Flats (10 outcome flips in 60 runs) and Crysium Vein Hard (2 flips in 15). So
   "Crysium Vein Medium sensible 3 1 0 to 5 5 5" cannot be credited to the scale alone. Only the skilled rows are clean.
3. **The sell rule made the bot worse on Shard Ridge Hard.** Seed 1, x1.25: in wave 10 with 7 coins it sells its upgraded
   Laser, buys the Flak and puts a Gun Turret on the Laser's cell; it loses in wave 16. Round 74's bot waited one wave,
   bought the Flak in wave 11 from its purse and won with 116.
4. **Five seeds are not five samples.** With the nest authored the seeds mostly replay the same game. The real uncertainty
   is the policy: a small rule change flips two seeds. Fragile, not noisy. The earlier critic's health jitter (item 2.5)
   is still the right answer and is still not done.

## 1. Is the sensible bot a stand-in for a reasonable human?

What is good now: it remembers living flyers, grows its counters, serves the trait furthest behind, guards a marked rift,
and records its purse. The pass criteria of the first critic were met honestly (round 74 table).

What is still not human:

- **It reads a preview the game does not show.** `PreviewCount` (BalanceProbeTests.cs:2985, `Horizon = 1`) counts the flyers
  and plated creatures of the current and the next wave. I found no HUD element that shows wave contents: in
  `Scripts/UI` and `Scripts/Levels` only `LevelRoster` (the level Codex) and `LoadingTips` read the wave data. So "announced
  one wave ahead" describes the bot. A human knows either more (the Codex lists every creature of this level and
  difficulty from second zero; Voss's loading tip) or less (nothing until the Skyray is on screen and Voss scans it).
- **Its opening is "the dearest affordable until the purse is empty".** Fault Line Medium: 260 coins, two Lasers, purse
  0, 0, 7, 17 at waves 1 to 4. A human who read the Codex keeps 120 back or opens with one Laser and a Flak. The round
  itself lists this as a remaining fault (finding 8).
- **The sell rule is a patch on that opening.** It fires in every run of the four flyer levels (240 of 240), including
  Shard Ridge, where round 74's bot won 5 of 5 at x0.8 and x1 without selling. A human sells when the base is being eaten
  and the purse cannot get there, not as a standing habit one wave ahead of a raid they cannot see coming.
- Also still missing: repairing guns while saving, sizing Cannons ahead of a plated pack (Glass Meadows Hard and Psycho).

Does the sell rule hide a level problem? **Yes, on Fault Line Medium, Hard, Psycho and Quartz Flats.** The facts from
STATUS round 75 and the records:

- First Skyray in wave 3 or 4. Income at that point is 10 to 30 coins a wave. From an empty purse the Flak's 120 arrives
  around wave 8.
- One Skyray of 32 health that reaches the base stays there: base lost 12, 168, 126, 378 in waves 4 to 7 (baseline, seed 1).
- Selling at a 70 % refund is the only way out for a player who spent the opening on ground guns.

So the level is only fair for a player who knew flyers were coming before placing the first gun. The game offers that
knowledge only in the Codex and a loading tip. Both the rule set (COMBAT_RULES section 6, "may hold 1 wave" on Medium) and
the generator (the Easy raid rule: no raid before the kills have paid a sky gun) already say what is wanted; nothing checks
it on Medium and above. The missing gate is **affordability of the first counter**, and it can be computed from the wave
data without any bot.

Verdict: use the sensible bot as a "careful reactive player" traffic light. Do not tune levels to it until its opening is
fixed and its knowledge matches the game's.

## 2. Were the data changes justified?

### 2a. Landing Site wave 7 rift authored to nest 1: yes

- It carries out Remco's own decision of 2026-11-10 (the rear rift is a lesson) and removes the one coin flip that decided
  whether the lesson exists. The code is small and clean: `SpawnGroup.Nest`, `NestAssignment.Authored`, a validator rule,
  a deterministic tie-break in `OffRoadRoute`. Tests were added.
- The cost is real but it is the level's true face: every lost run of the sensible bot on Hard and Psycho at x1 goes from a
  full base to zero in one wave (base lost 681 to 694 in wave 11 or 12; `pressure_r75_final_records.txt`). The skilled
  script on Psycho loses 4 of 5 the same way. One unshot Cragback ends the level. Before, two seeds in five skipped that.
- Small remarks, no action needed now: `SmallNestGroup = 2` is arbitrary (three walkers over two nests still miss the rear
  rift one run in four); nest (10,8) is now dead layout.

### 2b. Builder scales on Fault Line Medium and Hard, Crysium Vein Medium: weak

**Health per coin is not a yardstick across difficulties.** It divides non-boss health by kill coins plus starting money.
It ignores, by its own docstring, armour, flight and tempo. It also ignores that guns are capital that keeps firing (more
waves and longer waves mean more health per coin can be killed), free damage (gates, Spencer, base turret), Psycho's
tripled starting money arriving at second zero, repairs, and the expected permanent upgrades per difficulty. Its own table
shows it does not predict play:

| Level | Health per coin E / M / H / P (today) | Order in play |
| --- | --- | --- |
| Glass Meadows | 10.1 / **21.7 / 13.8** / 20.5 | Medium easier than Hard (correct order) |
| Shard Ridge | 8.7 / **10.9 / 7.5** / 10.4 | Hard below Easy on paper, harder in play |
| Fault Line | 5.1 / 7.9 / 11.3 / 20.4 | Medium still mixed for the sensible bot |
| Quartz Flats | 13.3 / 19.9 / 21.1 / 24.4 | Easy at 13.3 reads 5 5 5; Fault Line Medium at 7.9 reads 3 3 4 |

Medium sat above Hard on four levels; play was out of order on two. Two hits, two false alarms. It is a reading aid at
best, and the commit message ("so health per coin rises with difficulty") made it the goal.

What the clean evidence (skilled script, deterministic) says:

| Cell | Skilled before | After | Read |
| --- | --- | --- | --- |
| Crysium Vein Medium (x0.68 health) | 5 4 1 | 5 5 5 | Was about as hard as Hard (5 5 0). Now reads like Easy. **Overshoot.** |
| Fault Line Medium (x0.756 health) | 5 5 4 | 5 5 5 | One seed at x1.25. Sensible went 3 4 2 to 3 3 4: its losses are not about health. **The cut fixed nothing.** |
| Fault Line Hard (x1.19 health) | 5 5 5 | 5 5 3 | Reasonable: both rungs had over 25 % headroom. |

Side effect nobody flagged: Fault Line Medium now carries 7720 health in total, less than Landing Site Medium (9367).
Level 4 has less to shoot than level 1.

Regression risk:

- `acceptance.txt` and `grace_windows.txt` are from round 68 (commit 8761045e). `CombatRulesTests` reads the recorded grace
  windows, so "green on the new data" says nothing about the three moved cells. Fault Line Hard was a PASS and Crysium Vein
  Medium a PASS in the old acceptance; both moved.
- The finale probes were not run. Whether the boss wave's multiplier moved with the scale was not stated; if it did, the
  bosses of rounds 64 and 65 are off on three cells. Unverified by me.
- The scale acts non-linearly on Medium (0.87 to 0.7 gave x0.68). The lever is not the lever the agent thought it was.

## 4. Open questions: who decides

### For Remco (yes or no; my recommendation in bold)

| # | Question | Recommendation |
| --- | --- | --- |
| R1 | Show the next wave's creatures in the HUD (icons, flyers and plated marked)? It is on your own inspiration list in CLAUDE.md, and the bot already plays as if it exists. | **Yes.** Small UI feature. It makes "one wave of notice" true for humans and makes the bot honest. |
| R2 | First flyers on Medium and above never before the kills have paid one sky gun (the rule Easy already has)? Raids move a little later; the last ones may drop. | **Yes for Medium, no for Hard and Psycho.** Hard players are expected to know the level. |
| R3 | A creature that reaches the base stays and bites until it is shot, so one Skyray or one Cragback ends a level from a full base. Soften it (flyers make one pass and leave, or the base's own turret always handles a lone attacker)? | **Yes, soften, on Easy and Medium only.** This one rule is behind most "all or nothing" cells: Landing Site Hard and Psycho, Fault Line, Quartz Flats' finale. Without it no ruler can measure "won but hurt". |
| R4 | Landing Site rift on Psycho: two Cragbacks at x2.58 walk through everything. Keep? | **Keep for now.** Decide after R3; with R3 answered it probably solves itself. |
| R5 | Quartz Flats Medium finale: the boss takes the whole base or nothing. Lower the boss on Medium (`_bossHealthScale` 1.18)? | **Yes, to about 1.0 on Medium only,** then run the finale probe. It is your sizing rule, so your call. |
| R6 | Shard Ridge Hard is roomy. Harden? | **No.** After the bot change it reads 5 5 2, not 5 5 4. Leave it. |

### For the agents (defaults, no need to ask)

| Question in the reports | Default |
| --- | --- |
| Soft first raid as a script rule (r74 q5) | No rule. R2 covers the need; a second rule would fight the hero cap. |
| Plan search blind to a rear rift (r74 q6), skilled rung is a frozen script (r75 q6) | Make the skilled rung the adaptive replay of the best strategy plus the rift reaction. Drop frozen scripts. |
| Crysium Vein Medium at 5 5 5 (r75 q4) | Set the scale to 0.78 and re-measure with a frozen bot. Skilled should lose at least one seed at x1.25. |
| Fault Line Medium still mixed (r75 q1) | Put the scale back to 1 (two lines in `wave_tuning.txt`) until the lost runs are traced. The trace is cheap: record the Flak's health and the flyers' distance to it. |
| Recalibrate all Medium and Hard scales to health per coin (r75 q3) | No. Health per coin is not a target. |
| Quartz Flats Psycho not harder than Hard (r75 q7) | Wait for the adaptive skilled rung. No change. |
| Sensible bot on a knife edge on Landing Site Hard (r74 q3) | Fix the opening (below); do not tune the level to it. |
| `SmallNestGroup`, the dead nest, the latent Quartz Flats nest | Leave. The validator will speak when a walker troop is written there. |

## 5. Next round: at most five things

1. **Freeze the bot and fix how batches are compared.** Stamp every record with the bot's version (a constant bumped on
   every rule change, or the git hash plus a dirty flag). Never change bot and data in the same batch. Remove "one or two
   seeds are noise" from STATUS. Then re-measure the three moved cells against their old scales with one bot, so the data
   change gets its own before and after.
2. **Give the bot the human's knowledge and a human's opening.** It may read the level roster from second zero (the
   Codex). It may read the next wave only if R1 is approved. Opening: when the roster holds flyers, keep one sky gun's
   price in reserve or open with one; otherwise spread over two cheaper guns. Then restrict the sell rule to the emergency
   (flyers on the base, base losing health, purse short) and check it fires in a minority of runs.
3. **An affordability check from data, no bot** (EditMode, seconds to run): for every cell and counter trait, the coins a
   player can have earned by the first wave of that trait plus the grace window, against the counter's price. Print the
   table for all 24 cells; make it a validator warning. This replaces health per coin as the economy reading.
4. **Skilled rung adaptive** (best recorded strategy replayed adaptively, with the rift reaction), and health jitter in
   place of seeds: x0.8, 0.9, 1, 1.12, 1.25 on three seeds.
5. **Re-run the old gates on the moved cells:** acceptance, the grace-window measure for C1_04 and C1_06, the finale probes.
   Report any verdict that changed. Revert Fault Line Medium to scale 1 and set Crysium Vein Medium to 0.78 first.

Stop doing:

- Tuning level data to the sensible bot while its rules still change every batch.
- Using health per coin as a target or in commit messages.
- Reading a difference of one or two seeds as noise, or as signal, without checking what changed in the code.
- Adding bot rules that only repair the previous rule (sell, then sell-with-reach, then base guard). Three rules in one
  round moved more cells than the data did.
- Frozen-script "skilled" readings (Quartz Flats Hard: 0 of 5 where the adaptive replay wins 3 of 3).

## Caveats

- Nothing was run in Unity. The determinism finding rests on the committed records only.
- "No wave preview in the HUD" is from a search of `Scripts/UI` and `Scripts/Levels` for readers of the wave data. If a
  preview exists under another name, question R1 changes to "is it clear enough", and section 1 softens.
- Which bot rule caused each flip between r75_bot and r75_data was not traced; that the bot changed is certain.
- I did not check whether the builder scale moves boss health.
