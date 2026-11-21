# Round 88b, package "waves": doc text for the curator to merge

## Docs/WAVE_GRAMMAR.md, section "The solver (round 87: a formula)", under the `Peak` bullet of point 1

   - **A swarm between two air raids carries no peak (round 88b,** `BalanceRules.PeakOnARushBetweenRaids` **false).**
     The peaks are placed as before (`WaveFormula.Peaks`); then a peak that fell on a rush wave whose two neighbours
     are both raids is dropped, and it is not placed anew, so every other peak of the level stays on its wave
     (`WaveSolver.PeaksOf`, `WaveSolver.IsRushBetweenRaids`). Between two raids the purse belongs to the sky guns, and
     the swarm is the wave that asks for the most ground guns. In the first continent it takes the peak off four waves:
     Fault Line Psycho wave 13 (x3.32 -> x2.46, 12 500 -> 9 300 health), Quartz Flats Hard wave 7 (x1.79 -> x1.33),
     Quartz Flats Psycho wave 7 (x2.74 -> x2.03) and Crysium Vein Psycho wave 16 (x3.48 -> x2.58). In round 87's
     records those waves ended 32 of 57, 0 of 22, 0 of 46 and 1 of 48 lost runs: only Fault Line's was a wall.
     Tried on paper and not kept (`Tools/PlayTest/r88b_raids.txt`): "no peak between two raids, placed anew" moves
     nine peaks in seven Hard and Psycho cells and puts peaks on waves 1 to 3 (Fault Line Psycho: peaks 2 and 4 only),
     and "no peak on a rush between raids, placed anew" puts Fault Line Psycho's third peak on wave 2. Raids are the
     script's (`AirWaves`, a difficulty takes the first few): "no raids in two waves running" would undo the double
     raids Shard Ridge writes on purpose (waves 11-12 and 14-15) and leave Fault Line Psycho with Medium's raids.
     Tests: `Round88bWaveTests`.

## Docs/WAVE_GRAMMAR.md, section "Known and open" (new bullet)

- **Fault Line is a doorstep level (round 88b).** Its two roads (106 and 45 cells) share only the last nine cells
  before the base (trunk share 0.08; Shard Ridge and Crysium Vein 0.27), and the one gate both roads pass stands at
  cell 100 of 107; the gates at 11, 87 and 94 guard the long road alone. A defence that covers both roads stands at
  the base: the bots' creatures die at 0.72 to 0.91 of the road on every difficulty, and on Psycho the outer gates
  are gone by wave 5. With seven raids in sixteen waves (44 %; the other Psycho cells 0 to 35 %), three boss waves
  and the smallest kill money of the long levels (2554 coins; Shard Ridge 2716, Quartz Flats 3458), Psycho has six
  ground waves without a boss, and whatever leaks from the late swarm is on the base at once (a full base lost in
  wave 13 alone). The rule above takes the peak off that swarm; the map and the raid count are the level's own.
  Left for a script decision: one raid fewer on Psycho (`AirWavesPerDifficulty` 7 -> 6) gives the late game a second
  ground wave, which is more ground health, not less.

## Docs/STATUS.md, round 88b, causes (two or three sentences each)

**Money and the late game.** A level pays its money late and its creatures toughen little: 41 to 70 % of the kill
money arrives in the last quarter of the waves and 17 to 46 % in the last wave itself (528 to 730 coins on Hard,
which nobody can spend; it is most of the "almost 1000 coins left"), while a creature's health rises x1.8 over a
level (ramp 0.75 to 1.34) against x3 to x4.7 for a gun upgraded to level 3. On Hard and Psycho a mid-level boss
pays 257 to 415 coins (bounty x3 on a threat that armour doubled in round 87: Brood Mother 198 -> 337); its wave
adds 34 to 82 % to the purse on Hard, and the x0.8 breather follows: on paper the fire a coin must deliver stops
rising from the middle third to the last third on Hard (x1.00 over six levels; Fault Line x0.85, Quartz Flats
x0.65, Crysium Vein x0.72), where Easy, Medium and Psycho rise x1.37. The bots, who spend everything, still hold
the Easy levels with 39 to 48 % of their money to spare and Medium with 16 to 30 % (the targets ask for that);
Hard 6 to 17 %, Psycho 10 to 13 %.

**Fault Line on Psycho.** The level design: two roads that meet nine cells before the base, so the defence stands
on the doorstep, seven raids in sixteen waves that keep three to five of fourteen to seventeen towers on the sky,
and the smallest kill money of the long levels. The formula then put the ramp, the rush factor and a peak on the
only late ground wave (wave 13, x3.32 on 208 runners between the raids of 12 and 14): 32 of 57 lost bot runs end
there, from a full base. Rule: a swarm between two raids carries no peak (x2.46 now); it stays the level's toughest
wave and the cell stays the toughest Psycho.

**Crysium Vein on Easy and the uneven Easy.** Not the upgrades: the model's Easy player at level 6 owns 28 research
points of upgrades (Gun Turret and Cannon x1.10, gates x1.2, base +100), Remco's save 39 points (Gun Turret x1.20,
gates x1.4, a base turret) with 112 unspent. Every Easy level is decided in its boss wave (Landing Site and Glass
Meadows 100 % of the lost runs, Crysium Vein 81 %, the base from full to nothing in that wave), the level's scalar
does not move the boss, and the boss formula does not read the boss's pace: the Pack Alpha walks at 1.3 against the
Brood Mother's 0.55, so Crysium Vein's finale asks 2.39 bar x speed per coin held where Landing Site's asks 1.25.
The reading is also the weaker of two rungs, and the skilled pick is the weak one on exactly Glass Meadows and
Crysium Vein (two Cannons against a boss that is half armour; the sensible rung reads x1.48, one step under target).

## Docs/COMMANDS.md, "Round 88b" (new lines)

- `bash Tools/PlayTest/stage.sh run Round88bEconomyLab.cs Round88bEconomyLab.Run` writes `Tools/PlayTest/r88b_economy.txt`:
  per level and difficulty every stored wave as spawned and as paid (creatures, walk-in health of crowd and boss,
  bounties after the income multiplier, money before the wave as a plain sum, with the model's mines, and by
  `KillPurse`), the pressure index (health over money held), the ledger's window, the rate index (health per second
  per 100 coins held) and the ledger's pressure, their trend over the level, the money totals (last quarter, last
  wave, bosses, bosses before the last wave), and a table of "what if" scenarios (ramp, breather, peak phases,
  boss bounties, income, Psycho's start money).
- `... Round88bEconomyLab.Players` writes `r88b_players.txt` (the permanent upgrades the model's player owns per
  level and difficulty, half of them, none of them, and Remco's save of 2026-11-20 beside them);
  `... Round88bEconomyLab.Maps` writes `r88b_maps.txt` (roads, gates, rifts, build spots, first flyers and the purse
  by then, raids, first plated wave, bosses); `... Round88bEconomyLab.Raids` writes `r88b_raids.txt` (raids, their
  share, raids in two waves running, every peak with its distance from its phase, and what candidate peak rules move).
- `python -I Tools/PlayTest/r88b_spare.py <records> [...] [--rungs skilled,sensible] [--waves]`: per difficulty, level
  and rung, on the runs won at the data: coins unspent, earned (book), received (through the income multiplier),
  repairs, and the spare `received x (1 - 1 / break-even)`; then how far the waves get, the gate health lost and the
  purse at wave start by third of the level. `Tools/PlayTest/r88b_spare_before.txt` is round 87's final measurement.
  Note: field 17 of a record ("earned") is the start money plus BOOK bounties, before the income multiplier.
