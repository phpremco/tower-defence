### What changed, and why (Remco's playtest of level 1 on Easy, Medium and Hard, 2026-11-18: "plays fun")

1. **Easy and Medium a notch softer, Hard and Psycho as they were** ("Easy and Medium a tad too hard; Hard feels right.
   Not much less, but a bit"). The closed loop's targets: Easy, the weaker rung breaks even at x1.55 at least (was
   x1.4, and the old reading "holds the ladder to x1.25" could not see the difference); Medium, the sensible rung at
   x1.32 (was x1.18); one ladder step each. Hard (sensible x0.95, the better rung x1.06 at least) and Psycho (skilled
   x1.06) unchanged. `closed_loop.py` and `ClosedLoopAcceptanceTests` carry the new targets; Easy and Medium are
   measured on ladders that reach them (`r85_measure.sh`: Easy x1 to x1.76, Medium x0.9 to x1.57). Easy is judged on
   its break-even now (it passed by holding x1.25 before).
2. **Every boss walks in armour** ("they have no armour; that should be standard"): `BalanceRules.BossArmourShare`
   0.25, the armour pool of every boss (level and continent bosses) is at least a quarter of its body, so light fire
   chips it at 0.2 and the Cannon and rockets strip it, exactly as on every plated creature (COMBAT_RULES section 1).
   Written into the creature data by the generator (`ProjectGenerator.ArmourOf`), by `BalanceStages.ApplyBossArmour`
   (`ApplyRound85.cs`, menu `Tools/Generate/Balance/12`) and kept by the finale stage when it moves a continent
   boss's health (the pool now moves with the body: the first version kept the pool of the old, bigger body, which
   left Rotbloom with 6800 armour on 17950 body). Kill money follows the formula where the boss was on it.
3. **A boss is three times the toughest ordinary creature before it** ("the lava rocks seem stronger; bosses must
   feel bizarre"). The "lava rocks" are the Cragback, the stone heavy of level 1 (60 body and 160 armour in the book,
   so under light fire it is worth 860 against the 220 of its bar). On Landing Site Easy the boss had the bar of a
   Cragback (392 against 378) and no armour: under light fire a third of one. `BalanceRules.BossOverRegular` 3 on every
   difficulty: a level boss's floor is three times the health bar (body plus armour, as it walks in) of the toughest
   ordinary creature the player met in the level before it. The solver (`WaveSolver.Solve`) sizes the boss first; a
   boss wave over its cap gives way in its crowd first (to the lower edge of its band; with the crowd on its floor, as the first agent had it, a row of Gun Turrets won Landing Site Easy without the Cannon, which the grace gate refuses), then a boss above its floor comes down
   towards it, never under it. A boss on that floor that is over the cap on its own is the wave's fight by rule. Both
   boss floors are taken off the wave's lift now (a capped troop makes every creature of the wave tougher, the boss
   included), so the boss's bar is the floor as it walks in. Under light fire the boss is now 1.4 times a Cragback
   (shells: 3 times); see the question below.
4. **The laser's free kills** ("bizarrely strong on Easy and Medium, on Psycho actually not"). Cause, with numbers
   (`Tools/PlayTest/laser_share.txt`, `LaserDuel.Difficulties`): a beam holds one creature and swings on the moment it
   dies; every kill cost it only the sweep time (0.05 s) instead of the rest of its pulse. A creature that dies in one or
   two pulses then dies up to four times as fast as the laser's data says. Skitters, the swarm of level 1, spawn with
   12 health on Easy, 22 on Medium and Hard and 51 on Psycho: one or two pulses on Easy, seven on Psycho. So the same
   tower was worth more the lower a difficulty's health. One rule (`IHitBudget`, `Tower.ReleaseBeam`): the pulse that
   kills costs the beam the share of it the creature needed; a beam wastes nothing on a dead body and gains nothing
   either. Its role (light fire that wastes nothing on a swarm) is kept.
