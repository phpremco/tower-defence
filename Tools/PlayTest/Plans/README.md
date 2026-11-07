# Winning plans per level and difficulty

Written by the plan search (stage A, `Tools/Generate/Balance/5` or `PlanSearchTests`) on 2026-11-07 01:20; calibration 2026-11-06 21:30, seeds 1, step 0.2, inputs 9b275f8f (STALE, data now 9e849721).

Each plan is a priority list: a step is taken from its wave on, as soon as the money is there (an earlier step blocks the later ones).
Cells are (x,y) on the level grid. The paper verdict comes from the leak model (the base survives, stars by the base left); what the
same plans did in play is in `Tools/PlayTest/plan_replay.txt`, the obvious family in `Tools/PlayTest/yardstick.txt`. A cell without a
winning plan shows the best plan found and where it falls: a design question, not a plan to follow.

## Landing Site (Level_C1_01) on Easy

Best spots per tower: Gun Turret (5,14) (6,14) (31,8); Cannon (5,14) (31,8) (6,14).

**Level_C1_01_0_plan1** (3* base 600, margin 1.11, 268 coins left, 0 coins on gate repairs)

```
  from wave  1: build Gun Turret at (5,14); build Gun Turret at (5,15)
  from wave  2: build Gun Turret at (32,8); upgrade the tower at (5,15)
  from wave  6: upgrade the tower at (5,15); upgrade the tower at (32,8)
  from wave 11: repair the gates when they would fall or it is cheaper than a gun
```

**Level_C1_01_0_plan2** (3* base 600, margin 1.02, 258 coins left, 0 coins on gate repairs)

```
  from wave  1: build Cannon at (13,7); build Gun Turret at (5,13)
  from wave  3: upgrade the tower at (5,13)
  from wave  4: build Gun Turret at (31,8)
  from wave  5: upgrade the tower at (5,13)
```


## Landing Site (Level_C1_01) on Medium

Best spots per tower: Gun Turret (5,14) (6,14) (31,8); Cannon (5,14) (31,8) (6,14).

**Level_C1_01_1_plan1** (3* base 600, margin 1.00, 140 coins left, 0 coins on gate repairs)

```
  from wave  1: build Gun Turret at (5,13); build Gun Turret at (32,8); build Gun Turret at (5,14)
  from wave  3: build Cannon at (31,8); upgrade the tower at (32,8)
  from wave  5: upgrade the tower at (5,14)
  from wave  8: upgrade the tower at (5,13)
  from wave  9: upgrade the tower at (5,13); upgrade the tower at (5,14)
  from wave 10: build Gun Turret at (6,14)
```

**Level_C1_01_1_plan2** (3* base 600, margin 1.06, 9 coins left, 76 coins on gate repairs)

```
  from wave  1: build Gun Turret at (5,14); build Gun Turret at (6,14)
  from wave  2: upgrade the tower at (6,14)
  from wave  5: build Cannon at (13,7)
  from wave  6: build Cannon at (32,8)
  from wave  7: upgrade the tower at (5,14); upgrade the tower at (6,14)
  from wave  8: repair the gates when they would fall or it is cheaper than a gun
  from wave 10: upgrade the tower at (5,14); upgrade the tower at (32,8); upgrade the tower at (32,8)
  from wave 11: upgrade the tower at (13,7); upgrade the tower at (13,7); repair the gates when they would fall or it is cheaper than a gun
```


## Landing Site (Level_C1_01) on Hard

Best spots per tower: Gun Turret (5,14) (6,14) (31,8); Cannon (5,14) (31,8) (6,14).

No plan holds on paper; the best found (falls w11, margin 0.68):

**Level_C1_01_2_best** (falls w11, margin 0.68, 315 coins left, 60 coins on gate repairs)

```
  from wave  1: build Gun Turret at (5,14); build Gun Turret at (6,14); upgrade the tower at (5,14); upgrade the tower at (5,14)
  from wave  2: upgrade the tower at (6,14)
  from wave  3: upgrade the tower at (6,14)
  from wave  4: build Gun Turret at (31,8)
  from wave  5: upgrade the tower at (31,8); upgrade the tower at (31,8)
  from wave  6: build Gun Turret at (32,8); upgrade the tower at (32,8)
  from wave  7: build Gun Turret at (5,13); upgrade the tower at (32,8); repair the gates when they would fall or it is cheaper than a gun
  from wave  8: repair the gates when they would fall or it is cheaper than a gun
  from wave 11: repair the gates when they would fall or it is cheaper than a gun
```


## Landing Site (Level_C1_01) on Psycho

Best spots per tower: Gun Turret (5,14) (6,14) (31,8); Cannon (5,14) (31,8) (6,14); Laser Turret (6,14) (5,14) (7,14); Flak Turret (30,15) (29,15) (30,18).

No plan holds on paper; the best found (falls w8, margin 0.61):

**Level_C1_01_3_best** (falls w8, margin 0.61, 260 coins left, 60 coins on gate repairs)

```
  from wave  1: build Gun Turret at (5,14); build Gun Turret at (6,14)
  from wave  2: upgrade the tower at (5,14); upgrade the tower at (5,14); upgrade the tower at (6,14)
  from wave  3: upgrade the tower at (6,14)
  from wave  4: build Gun Turret at (31,8)
  from wave  5: upgrade the tower at (31,8); upgrade the tower at (31,8)
  from wave  6: build Gun Turret at (32,8); upgrade the tower at (32,8)
  from wave  7: build Gun Turret at (5,13); upgrade the tower at (32,8); repair the gates when they would fall or it is cheaper than a gun
  from wave  8: repair the gates when they would fall or it is cheaper than a gun; upgrade the tower at (5,13)
  from wave 10: upgrade the tower at (5,13)
  from wave 11: repair the gates when they would fall or it is cheaper than a gun
```


## Glass Meadows (Level_C1_02) on Easy

Best spots per tower: Gun Turret (7,6) (5,6) (5,7); Cannon (30,15) (30,16) (31,16); Laser Turret (7,6) (6,5) (7,5).

**Level_C1_02_0_plan1** (3* base 700, margin 1.03, 121 coins left, 0 coins on gate repairs)

```
  from wave  1: build Gun Turret at (6,5); build Cannon at (5,7)
  from wave  3: build Gun Turret at (7,5)
  from wave  4: build Cannon at (5,6); upgrade the tower at (6,5)
  from wave  6: upgrade the tower at (6,5)
  from wave  7: upgrade the tower at (7,5)
  from wave  8: upgrade the tower at (7,5)
```

**Level_C1_02_0_plan2** (3* base 700, margin 1.14, 66 coins left, 0 coins on gate repairs)

```
  from wave  1: build Gun Turret at (7,6)
  from wave  2: upgrade the tower at (7,6); upgrade the tower at (7,6)
  from wave  3: build Gun Turret at (5,6); upgrade the tower at (5,6)
  from wave  4: build Gun Turret at (5,7); upgrade the tower at (5,6)
  from wave  5: upgrade the tower at (5,7); upgrade the tower at (5,7)
  from wave  6: build Gun Turret at (8,6); upgrade the tower at (6,5)
  from wave  7: build Gun Turret at (7,5); upgrade the tower at (6,5)
  from wave  8: repair the gates when they would fall or it is cheaper than a gun
  from wave 11: repair the gates when they would fall or it is cheaper than a gun
```


## Glass Meadows (Level_C1_02) on Medium

Best spots per tower: Gun Turret (7,6) (5,6) (5,7); Cannon (30,15) (30,16) (31,16); Laser Turret (7,6) (6,5) (7,5).

**Level_C1_02_1_plan1** (1* base 245, margin 0.41, 87 coins left, 0 coins on gate repairs)

```
  from wave  1: build Cannon at (5,6); build Cannon at (4,7)
  from wave  4: upgrade the tower at (5,6)
  from wave  6: build Cannon at (5,7)
  from wave  7: upgrade the tower at (5,7)
  from wave  9: upgrade the tower at (30,15)
  from wave 11: upgrade the tower at (5,7)
  from wave 12: upgrade the tower at (5,6)
```

**Level_C1_02_1_plan2** (1* base 228, margin 0.44, 57 coins left, 0 coins on gate repairs)

```
  from wave  1: build Cannon at (5,6); build Cannon at (5,7); build Cannon at (4,7)
  from wave  2: build Cannon at (30,15); upgrade the tower at (4,7)
  from wave  3: build Laser Turret at (6,5); build Gun Turret at (7,5); repair the gates when they would fall or it is cheaper than a gun
  from wave  4: build Laser Turret at (8,7); build Gun Turret at (8,6); build Gun Turret at (7,6)
  from wave  5: build Cannon at (30,16); upgrade the tower at (4,7); upgrade the tower at (6,5)
  from wave  8: upgrade the tower at (8,7); upgrade the tower at (7,6)
  from wave  9: upgrade the tower at (8,6)
  from wave 10: build Cannon at (31,16); upgrade the tower at (8,6)
  from wave 12: upgrade the tower at (7,5); upgrade the tower at (8,7)
```


## Glass Meadows (Level_C1_02) on Hard

Best spots per tower: Gun Turret (7,6) (5,6) (5,7); Cannon (30,15) (30,16) (31,16); Laser Turret (7,6) (6,5) (7,5).

No plan holds on paper; the best found (falls w11, margin 0.42):

**Level_C1_02_2_best** (falls w11, margin 0.42, 39 coins left, 60 coins on gate repairs)

```
  from wave  1: build Cannon at (4,7); build Cannon at (5,7)
  from wave  3: build Cannon at (5,6); upgrade the tower at (4,7); upgrade the tower at (5,7)
  from wave  5: build Laser Turret at (7,6)
  from wave  8: build Gun Turret at (6,5); build Cannon at (30,15); repair the gates when they would fall or it is cheaper than a gun
  from wave 11: upgrade the tower at (6,5); upgrade the tower at (7,6); upgrade the tower at (6,5); upgrade the tower at (5,6)
  from wave 12: repair the gates when they would fall or it is cheaper than a gun
```


## Glass Meadows (Level_C1_02) on Psycho

Best spots per tower: Gun Turret (7,6) (5,6) (5,7); Cannon (30,15) (30,16) (31,16); Laser Turret (7,6) (6,5) (7,5); Flak Turret (32,16) (31,16) (32,19).

No plan holds on paper; the best found (falls w8, margin 0.38):

**Level_C1_02_3_best** (falls w8, margin 0.38, 46 coins left, 60 coins on gate repairs)

```
  from wave  1: build Cannon at (4,7); build Cannon at (5,7)
  from wave  2: build Gun Turret at (7,6); upgrade the tower at (5,7)
  from wave  3: repair the gates when they would fall or it is cheaper than a gun
  from wave  4: build Cannon at (31,16)
  from wave  5: build Laser Turret at (7,5)
  from wave  6: build Gun Turret at (6,5); build Cannon at (30,15); build Laser Turret at (8,7); repair the gates when they would fall or it is cheaper than a gun
  from wave  7: build Cannon at (5,6); upgrade the tower at (7,5)
  from wave  8: upgrade the tower at (31,16)
  from wave  9: upgrade the tower at (5,6); upgrade the tower at (30,15)
  from wave 10: build Cannon at (30,16); upgrade the tower at (31,16); upgrade the tower at (30,15)
  from wave 11: upgrade the tower at (4,7)
  from wave 12: repair the gates when they would fall or it is cheaper than a gun
```


## Shard Ridge (Level_C1_03) on Easy

Best spots per tower: Gun Turret (12,11) (13,11) (14,11); Cannon (16,11) (12,10) (12,11); Laser Turret (13,10) (9,10) (11,10); Flak Turret (32,10) (33,10) (34,7).

**Level_C1_03_0_plan1** (1* base 34, margin 0.06, 76 coins left, 0 coins on gate repairs)

```
  from wave  1: build Cannon at (13,11); build Gun Turret at (17,11)
  from wave  2: build Cannon at (14,11)
  from wave  5: build Gun Turret at (12,11); build Cannon at (13,10); upgrade the tower at (17,11)
  from wave  6: upgrade the tower at (12,11)
  from wave  7: build Gun Turret at (15,11); build Flak Turret at (32,10); build Flak Turret at (34,10); build Flak Turret at (33,10)
  from wave  9: build Cannon at (12,10)
  from wave 10: upgrade the tower at (34,10)
  from wave 11: upgrade the tower at (17,11)
  from wave 12: build Laser Turret at (9,10); upgrade the tower at (15,11)
  from wave 13: build Cannon at (16,11); upgrade the tower at (12,11); upgrade the tower at (34,10)
  from wave 15: upgrade the tower at (16,11)
  from wave 16: upgrade the tower at (16,11)
```


## Shard Ridge (Level_C1_03) on Medium

Best spots per tower: Gun Turret (12,11) (13,11) (14,11); Cannon (16,11) (12,10) (12,11); Laser Turret (13,10) (9,10) (11,10); Flak Turret (32,10) (33,10) (34,7).

No plan holds on paper; the best found (falls w11, margin 0.32):

**Level_C1_03_1_best** (falls w11, margin 0.32, 106 coins left, 0 coins on gate repairs, repair drone)

```
  from wave  1: build Gun Turret at (17,11); build Gun Turret at (12,11); build Cannon at (13,10)
  from wave  2: upgrade the tower at (17,11)
  from wave  3: build Flak Turret at (32,10)
  from wave  4: build Laser Turret at (15,10); build Gun Turret at (15,11); build Flak Turret at (34,10); build Flak Turret at (33,10); upgrade the tower at (12,11)
  from wave  5: build Flak Turret at (35,7); upgrade the tower at (34,10); repair the gates when they would fall or it is cheaper than a gun
  from wave  6: build Cannon at (16,11); build Cannon at (12,10); upgrade the tower at (35,7)
  from wave  7: repair the gates when they would fall or it is cheaper than a gun
  from wave  8: build Gun Turret at (14,11)
  from wave  9: build Laser Turret at (14,10); upgrade the tower at (35,7); buy the RepairDrone
  from wave 11: build Laser Turret at (9,10); upgrade the tower at (32,10); repair the gates when they would fall or it is cheaper than a gun
  from wave 12: upgrade the tower at (12,10)
  from wave 13: upgrade the tower at (13,10)
  from wave 14: build Cannon at (13,11); upgrade the tower at (33,10); upgrade the tower at (9,10)
  from wave 15: upgrade the tower at (12,11); upgrade the tower at (32,10); upgrade the tower at (34,10)
  from wave 16: upgrade the tower at (14,10); upgrade the tower at (13,11)
```


## Shard Ridge (Level_C1_03) on Hard

Best spots per tower: Gun Turret (12,11) (13,11) (14,11); Cannon (16,11) (12,10) (12,11); Laser Turret (13,10) (9,10) (11,10); Flak Turret (32,10) (33,10) (34,7).

No plan holds on paper; the best found (falls w8, margin 0.19):

**Level_C1_03_2_best** (falls w8, margin 0.19, 102 coins left, 0 coins on gate repairs, repair drone)

```
  from wave  1: build Cannon at (13,10); build Laser Turret at (11,10)
  from wave  2: build Cannon at (12,10); upgrade the tower at (11,10)
  from wave  3: build Flak Turret at (33,10)
  from wave  5: build Flak Turret at (32,10)
  from wave  7: build Laser Turret at (9,10)
  from wave  8: build Flak Turret at (34,10); buy the RepairDrone
  from wave 10: upgrade the tower at (33,10)
  from wave 11: upgrade the tower at (33,10); upgrade the tower at (34,10); repair the gates when they would fall or it is cheaper than a gun
  from wave 14: repair the gates when they would fall or it is cheaper than a gun
  from wave 16: repair the gates when they would fall or it is cheaper than a gun
```


## Shard Ridge (Level_C1_03) on Psycho

Best spots per tower: Gun Turret (12,11) (13,11) (14,11); Cannon (16,11) (12,10) (12,11); Laser Turret (13,10) (9,10) (11,10); Flak Turret (32,10) (33,10) (34,7).

No plan holds on paper; the best found (falls w7, margin 0.22):

**Level_C1_03_3_best** (falls w7, margin 0.22, 8 coins left, 0 coins on gate repairs)

```
  from wave  1: build Gun Turret at (12,11); build Cannon at (13,10); build Gun Turret at (17,11); upgrade the tower at (12,11)
  from wave  2: upgrade the tower at (13,10); upgrade the tower at (17,11)
  from wave  3: build Flak Turret at (34,10); upgrade the tower at (17,11)
  from wave  4: build Cannon at (16,11); build Laser Turret at (14,10); build Gun Turret at (15,11); buy the HarvestDrone
  from wave  5: build Flak Turret at (33,10); build Flak Turret at (34,7); upgrade the tower at (16,11)
  from wave  7: build Gun Turret at (13,11); build Gun Turret at (14,11)
  from wave  8: build Cannon at (12,10); upgrade the tower at (35,7); upgrade the tower at (13,11); sell the tower at (33,10)
  from wave  9: upgrade the tower at (15,11)
  from wave 11: upgrade the tower at (13,11)
  from wave 12: upgrade the tower at (12,10)
  from wave 13: build Laser Turret at (15,10); upgrade the tower at (35,7)
  from wave 15: upgrade the tower at (14,10)
  from wave 16: build Laser Turret at (33,10); upgrade the tower at (15,10); upgrade the tower at (15,10)
```


## Fault Line (Level_C1_04) on Easy

Best spots per tower: Gun Turret (32,6) (33,6) (28,5); Cannon (32,6) (33,6) (33,9); Laser Turret (28,5) (29,5) (32,5); Flak Turret (34,9) (33,9) (34,6).

**Level_C1_04_0_plan1** (2* base 386, margin 0.42, 53 coins left, 0 coins on gate repairs)

```
  from wave  1: build Gun Turret at (32,5); build Gun Turret at (32,6); build Gun Turret at (29,5)
  from wave  2: build Gun Turret at (33,6); upgrade the tower at (32,6)
  from wave  4: upgrade the tower at (32,5); upgrade the tower at (33,6)
  from wave  5: upgrade the tower at (29,5)
  from wave  6: build Cannon at (29,6)
  from wave  7: build Flak Turret at (34,9)
  from wave  8: build Flak Turret at (34,10)
  from wave  9: build Flak Turret at (34,6); build Gun Turret at (28,5)
  from wave 10: upgrade the tower at (28,5)
  from wave 11: upgrade the tower at (32,5); upgrade the tower at (29,5)
  from wave 13: build Laser Turret at (33,9)
  from wave 14: buy the AttackDrone
  from wave 16: upgrade the tower at (28,5)
```

**Level_C1_04_0_plan2** (1* base 237, margin 0.28, 29 coins left, 0 coins on gate repairs)

```
  from wave  1: build Gun Turret at (29,5); build Gun Turret at (33,6)
  from wave  3: build Gun Turret at (28,5); upgrade the tower at (33,6)
  from wave  5: build Cannon at (32,6); build Gun Turret at (29,6); build Gun Turret at (32,5); upgrade the tower at (29,5)
  from wave  6: build Flak Turret at (32,9); upgrade the tower at (32,5)
  from wave  7: buy the HarvestDrone
  from wave  8: upgrade the tower at (32,9)
  from wave  9: build Laser Turret at (33,9); upgrade the tower at (32,6)
  from wave 10: build Cannon at (33,10)
  from wave 11: upgrade the tower at (29,6); upgrade the tower at (29,6)
  from wave 12: upgrade the tower at (33,6); repair the gates when they would fall or it is cheaper than a gun
  from wave 13: upgrade the tower at (32,6); upgrade the tower at (33,10)
  from wave 14: repair the gates when they would fall or it is cheaper than a gun
  from wave 15: upgrade the tower at (28,5); upgrade the tower at (32,9)
```


## Fault Line (Level_C1_04) on Medium

Best spots per tower: Gun Turret (32,6) (33,6) (28,5); Cannon (32,6) (33,6) (33,9); Laser Turret (28,5) (29,5) (32,5); Flak Turret (34,9) (33,9) (34,6).

No plan holds on paper; the best found (falls w12, margin 0.18):

**Level_C1_04_1_best** (falls w12, margin 0.18, 52 coins left, 0 coins on gate repairs)

```
  from wave  1: build Cannon at (33,10); build Gun Turret at (28,5); build Gun Turret at (29,6); upgrade the tower at (28,5)
  from wave  2: upgrade the tower at (29,6)
  from wave  4: upgrade the tower at (29,6)
  from wave  5: upgrade the tower at (28,5)
  from wave  6: build Laser Turret at (29,5); build Laser Turret at (32,5)
  from wave  7: build Cannon at (33,9)
  from wave  8: build Cannon at (33,6); build Cannon at (32,6)
  from wave 10: build Flak Turret at (34,6)
  from wave 11: upgrade the tower at (34,6)
  from wave 12: buy the AttackDrone
  from wave 14: build Laser Turret at (33,5); upgrade the tower at (34,6)
  from wave 15: upgrade the tower at (32,5)
```


## Fault Line (Level_C1_04) on Hard

Best spots per tower: Gun Turret (32,6) (33,6) (28,5); Cannon (32,6) (33,6) (33,9); Laser Turret (28,5) (29,5) (32,5); Flak Turret (34,9) (33,9) (34,6).

No plan holds on paper; the best found (falls w9, margin 0.28):

**Level_C1_04_2_best** (falls w9, margin 0.28, 51 coins left, 60 coins on gate repairs)

```
  from wave  1: build Gun Turret at (32,5); build Gun Turret at (32,6); build Gun Turret at (33,6)
  from wave  2: build Flak Turret at (34,10)
  from wave  3: upgrade the tower at (32,6); upgrade the tower at (32,6)
  from wave  4: build Laser Turret at (28,5); build Gun Turret at (29,6); build Cannon at (33,9); repair the gates when they would fall or it is cheaper than a gun
  from wave  6: build Flak Turret at (31,9); build Flak Turret at (32,9); upgrade the tower at (33,6)
  from wave  7: upgrade the tower at (32,9)
  from wave  8: upgrade the tower at (33,6); upgrade the tower at (33,9)
  from wave  9: upgrade the tower at (34,10)
  from wave 10: repair the gates when they would fall or it is cheaper than a gun
  from wave 11: upgrade the tower at (34,10)
  from wave 12: upgrade the tower at (29,6)
  from wave 13: build Laser Turret at (33,5)
  from wave 14: build Laser Turret at (29,5); upgrade the tower at (31,9); repair the gates when they would fall or it is cheaper than a gun
  from wave 15: upgrade the tower at (32,5)
```


## Fault Line (Level_C1_04) on Psycho

Best spots per tower: Gun Turret (32,6) (33,6) (28,5); Cannon (32,6) (33,6) (33,9); Laser Turret (28,5) (29,5) (32,5); Flak Turret (34,9) (33,9) (34,6).

No plan holds on paper; the best found (falls w5, margin 0.18):

**Level_C1_04_3_best** (falls w5, margin 0.18, 25 coins left, 60 coins on gate repairs)

```
  from wave  1: build Cannon at (32,5); build Gun Turret at (32,6); build Gun Turret at (33,6); upgrade the tower at (32,6)
  from wave  4: build Flak Turret at (34,6); build Laser Turret at (28,5); repair the gates when they would fall or it is cheaper than a gun
  from wave  7: build Flak Turret at (32,9); upgrade the tower at (33,6)
  from wave  8: build Cannon at (33,10); repair the gates when they would fall or it is cheaper than a gun
  from wave  9: upgrade the tower at (32,6)
  from wave 10: repair the gates when they would fall or it is cheaper than a gun
  from wave 11: build Cannon at (33,9); build Cannon at (29,6); upgrade the tower at (34,6)
  from wave 12: build Gun Turret at (29,5); upgrade the tower at (34,6); upgrade the tower at (32,9)
  from wave 13: upgrade the tower at (29,5)
  from wave 14: upgrade the tower at (29,6)
  from wave 16: upgrade the tower at (29,6)
```


## Quartz Flats (Level_C1_05) on Easy

Best spots per tower: Gun Turret (7,5) (7,4) (20,5); Cannon (7,5) (7,4) (20,5); Laser Turret (7,5) (6,6) (7,4); Flak Turret (34,4) (34,5) (35,4).

**Level_C1_05_0_plan1** (3* base 700, margin 0.00, 179 coins left, 0 coins on gate repairs, repair drone)

```
  from wave  1: build Cannon at (6,5); build Cannon at (6,6)
  from wave  2: buy the RepairDrone
  from wave  4: build Cannon at (7,5)
  from wave 10: upgrade the tower at (6,5)
  from wave 11: build Flak Turret at (34,5)
  from wave 13: build Cannon at (7,4); upgrade the tower at (6,5)
  from wave 15: upgrade the tower at (20,5)
```

**Level_C1_05_0_plan2** (3* base 700, margin 0.01, 106 coins left, 0 coins on gate repairs, repair drone)

```
  from wave  1: build Cannon at (20,4); build Gun Turret at (7,4); build Cannon at (7,5)
  from wave  2: build Cannon at (6,5); buy the RepairDrone
  from wave  5: build Cannon at (20,5); upgrade the tower at (7,5)
  from wave  8: build Cannon at (6,6); build Gun Turret at (21,5); build Flak Turret at (33,4); upgrade the tower at (7,5)
  from wave 10: upgrade the tower at (6,5)
  from wave 15: upgrade the tower at (6,5); upgrade the tower at (21,5)
```


## Quartz Flats (Level_C1_05) on Medium

Best spots per tower: Gun Turret (7,5) (7,4) (20,5); Cannon (7,5) (7,4) (20,5); Laser Turret (7,5) (6,6) (7,4); Flak Turret (34,4) (34,5) (35,4).

**Level_C1_05_1_plan1** (1* base 32, margin 0.05, 152 coins left, 0 coins on gate repairs)

```
  from wave  1: build Gun Turret at (20,5); build Cannon at (7,5)
  from wave  2: buy the HarvestDrone
  from wave  3: build Gun Turret at (6,6)
  from wave  5: build Cannon at (20,4)
  from wave  6: build Flak Turret at (33,5); build Cannon at (6,5)
  from wave  7: upgrade the tower at (20,4)
  from wave  9: upgrade the tower at (6,6); upgrade the tower at (6,6)
  from wave 10: build Flak Turret at (33,4)
  from wave 12: upgrade the tower at (20,5)
  from wave 13: build Gun Turret at (7,4); upgrade the tower at (20,5); upgrade the tower at (33,4)
  from wave 15: build Laser Turret at (8,6)
  from wave 16: upgrade the tower at (33,4); upgrade the tower at (21,5)
```


## Quartz Flats (Level_C1_05) on Hard

Best spots per tower: Gun Turret (7,5) (7,4) (20,5); Cannon (7,5) (7,4) (20,5); Laser Turret (7,5) (6,6) (7,4); Flak Turret (34,4) (34,5) (35,4).

No plan holds on paper; the best found (falls w10, margin 0.26):

**Level_C1_05_2_best** (falls w10, margin 0.26, 5 coins left, 0 coins on gate repairs)

```
  from wave  1: build Gun Turret at (7,5); build Cannon at (6,6); build Cannon at (6,5)
  from wave  3: build Flak Turret at (35,4); build Flak Turret at (34,4); upgrade the tower at (6,5)
  from wave  4: build Laser Turret at (7,4); build Flak Turret at (34,5)
  from wave  7: build Gun Turret at (20,5); build Gun Turret at (21,5); upgrade the tower at (6,6)
  from wave  9: build Laser Turret at (8,6); upgrade the tower at (21,5)
  from wave 11: upgrade the tower at (20,5)
  from wave 13: build Cannon at (20,4); upgrade the tower at (34,5)
  from wave 14: upgrade the tower at (20,4)
  from wave 15: upgrade the tower at (20,5)
  from wave 16: repair the gates when they would fall or it is cheaper than a gun; repair the gates when they would fall or it is cheaper than a gun; repair the gates when they would fall or it is cheaper than a gun
  from wave 17: upgrade the tower at (20,4)
```


## Quartz Flats (Level_C1_05) on Psycho

Best spots per tower: Gun Turret (7,5) (7,4) (20,5); Cannon (7,5) (7,4) (20,5); Laser Turret (7,5) (6,6) (7,4); Flak Turret (34,4) (34,5) (35,4).

No plan holds on paper; the best found (falls w8, margin 0.22):

**Level_C1_05_3_best** (falls w8, margin 0.22, 10 coins left, 0 coins on gate repairs)

```
  from wave  1: build Cannon at (7,5); build Cannon at (7,4); build Flak Turret at (35,5)
  from wave  3: build Cannon at (20,4); build Gun Turret at (6,5)
  from wave  4: build Flak Turret at (33,4)
  from wave  6: build Laser Turret at (21,5); upgrade the tower at (7,4)
  from wave  7: build Gun Turret at (20,5)
  from wave  8: upgrade the tower at (20,5)
  from wave  9: build Laser Turret at (6,6)
  from wave 10: build Flak Turret at (35,4); buy the AttackDrone
  from wave 11: build Laser Turret at (8,6)
  from wave 12: upgrade the tower at (8,6)
  from wave 15: upgrade the tower at (7,4); upgrade the tower at (6,5)
  from wave 16: upgrade the tower at (8,6)
```


## Crysium Vein (Level_C1_06) on Easy

Best spots per tower: Gun Turret (12,10) (12,11) (13,10); Cannon (17,11) (17,7) (17,10); Laser Turret (13,10) (11,10) (12,10); Flak Turret (36,10) (36,7) (36,11).

No plan holds on paper; the best found (falls w16, margin 0.16):

**Level_C1_06_0_best** (falls w16, margin 0.16, 45 coins left, 181 coins on gate repairs)

```
  from wave  1: build Gun Turret at (13,10); build Gun Turret at (13,11)
  from wave  2: build Cannon at (18,11)
  from wave  4: upgrade the tower at (13,11)
  from wave  6: upgrade the tower at (13,11)
  from wave  7: build Gun Turret at (12,10); build Flak Turret at (36,10); repair the gates when they would fall or it is cheaper than a gun
  from wave  8: upgrade the tower at (12,10)
  from wave  9: upgrade the tower at (36,10)
  from wave 11: repair the gates when they would fall or it is cheaper than a gun
  from wave 12: build Flak Turret at (34,10)
  from wave 13: upgrade the tower at (12,10)
  from wave 15: upgrade the tower at (36,10)
  from wave 16: lay a Mine on the road at (14,9)
  from wave 18: lay a Mine on the road at (12,9)
```


## Crysium Vein (Level_C1_06) on Medium

Best spots per tower: Gun Turret (12,10) (12,11) (13,10); Cannon (17,11) (17,7) (17,10); Laser Turret (13,10) (11,10) (12,10); Flak Turret (36,10) (36,7) (36,11).

No plan holds on paper; the best found (falls w11, margin 0.00):

**Level_C1_06_1_best** (falls w11, margin 0.00, 32 coins left, 0 coins on gate repairs)

```
  from wave  1: build Cannon at (17,11); build Cannon at (12,10); build Cannon at (17,10)
  from wave  3: build Flak Turret at (36,10); upgrade the tower at (12,10)
  from wave  4: upgrade the tower at (17,11)
  from wave  6: build Laser Turret at (11,10)
  from wave  8: build Flak Turret at (34,10)
  from wave  9: build Gun Turret at (12,11)
  from wave 10: upgrade the tower at (12,11)
  from wave 12: build Flak Turret at (36,7)
  from wave 15: build Laser Turret at (14,10)
  from wave 16: build Laser Turret at (13,11); upgrade the tower at (36,7)
  from wave 17: upgrade the tower at (36,7)
  from wave 18: upgrade the tower at (14,10)
```


## Crysium Vein (Level_C1_06) on Hard

Best spots per tower: Gun Turret (12,10) (12,11) (13,10); Cannon (17,11) (17,7) (17,10); Laser Turret (13,10) (11,10) (12,10); Flak Turret (36,10) (36,7) (36,11).

No plan holds on paper; the best found (falls w8, margin 0.13):

**Level_C1_06_2_best** (falls w8, margin 0.13, 69 coins left, 0 coins on gate repairs, repair drone)

```
  from wave  1: build Cannon at (18,11); build Cannon at (17,10); build Cannon at (12,10)
  from wave  3: build Flak Turret at (35,10)
  from wave  5: upgrade the tower at (18,11); buy the RepairDrone
  from wave  6: upgrade the tower at (18,11)
  from wave  7: lay a Mine on the road at (13,9)
  from wave  9: build Flak Turret at (36,10)
  from wave 10: upgrade the tower at (35,10)
  from wave 11: lay a SlowField on the road at (15,9)
  from wave 13: upgrade the tower at (35,10)
  from wave 14: build Cannon at (17,11)
  from wave 15: buy the AttackDrone
```


## Crysium Vein (Level_C1_06) on Psycho

Best spots per tower: Gun Turret (12,10) (12,11) (13,10); Cannon (17,11) (17,7) (17,10); Laser Turret (13,10) (11,10) (12,10); Flak Turret (36,10) (36,7) (36,11).

No plan holds on paper; the best found (falls w6, margin 0.07):

**Level_C1_06_3_best** (falls w6, margin 0.07, 40 coins left, 0 coins on gate repairs, repair drone)

```
  from wave  1: build Cannon at (17,10); build Gun Turret at (13,10); lay a Mine on the road at (13,9); lay a Mine on the road at (15,9)
  from wave  5: build Flak Turret at (36,11)
  from wave  6: upgrade the tower at (13,10)
  from wave  8: upgrade the tower at (17,10)
  from wave 10: build Laser Turret at (12,10); lay a Mine on the road at (13,9)
  from wave 11: buy the RepairDrone
  from wave 12: build Flak Turret at (35,10); build Flak Turret at (36,7)
  from wave 14: build Cannon at (16,11); upgrade the tower at (17,10); upgrade the tower at (12,10); upgrade the tower at (12,10)
  from wave 15: upgrade the tower at (16,11)
  from wave 17: upgrade the tower at (16,11)
  from wave 18: upgrade the tower at (13,10)
```

