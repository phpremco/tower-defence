# Winning plans per level and difficulty

Written by the plan search (stage A, `Tools/Generate/Balance/5` or `PlanSearchTests`) on 2026-11-07 03:44; calibration 2026-11-07 01:56, seeds 1,2,3, step 0.2, inputs 551e7c40 (current).

Each plan is a priority list: a step is taken from its wave on, as soon as the money is there (an earlier step blocks the later ones).
Cells are (x,y) on the level grid. The paper verdict comes from the leak model (the base survives, stars by the base left); what the
same plans did in play is in `Tools/PlayTest/plan_replay.txt`, the obvious family in `Tools/PlayTest/yardstick.txt`. A cell without a
winning plan shows the best plan found and where it falls: a design question, not a plan to follow.

## Landing Site (Level_C1_01) on Easy

Best spots per tower: Gun Turret (5,14) (6,14) (31,8); Cannon (5,14) (31,8) (6,14).

**Level_C1_01_0_plan1** (3* base 600, margin 0.95, 329 coins left, 0 coins on gate repairs)

```
  from wave  1: build Gun Turret at (5,14); build Gun Turret at (6,14)
  from wave  2: build Gun Turret at (31,8)
  from wave  4: upgrade the tower at (5,14)
  from wave  6: upgrade the tower at (6,14)
```

**Level_C1_01_0_plan2** (3* base 600, margin 0.65, 360 coins left, 0 coins on gate repairs)

```
  from wave  1: build Gun Turret at (5,13); build Gun Turret at (6,14)
  from wave  2: build Cannon at (14,7)
```


## Landing Site (Level_C1_01) on Medium

Best spots per tower: Gun Turret (5,14) (6,14) (31,8); Cannon (5,14) (31,8) (6,14).

**Level_C1_01_1_plan1** (3* base 600, margin 0.48, 373 coins left, 0 coins on gate repairs)

```
  from wave  1: build Cannon at (14,7); build Gun Turret at (32,8); upgrade the tower at (32,8)
  from wave  6: build Cannon at (13,7)
```

**Level_C1_01_1_plan2** (3* base 600, margin 0.64, 307 coins left, 0 coins on gate repairs)

```
  from wave  1: build Gun Turret at (5,13); build Cannon at (14,7); build Gun Turret at (5,15)
  from wave  2: build Gun Turret at (5,14)
  from wave  5: upgrade the tower at (5,13)
  from wave  7: upgrade the tower at (5,13)
```


## Landing Site (Level_C1_01) on Hard

Best spots per tower: Gun Turret (5,14) (6,14) (31,8); Cannon (5,14) (31,8) (6,14).

**Level_C1_01_2_plan1** (3* base 600, margin 0.45, 283 coins left, 142 coins on gate repairs)

```
  from wave  1: build Cannon at (14,7); build Cannon at (13,7)
  from wave  3: build Cannon at (31,8); build Cannon at (5,14)
  from wave  4: build Cannon at (6,14)
  from wave  6: build Gun Turret at (32,8); repair the gates when they would fall or it is cheaper than a gun
```


## Landing Site (Level_C1_01) on Psycho

Best spots per tower: Gun Turret (5,14) (6,14) (31,8); Cannon (5,14) (31,8) (6,14); Laser Turret (6,14) (5,14) (7,14); Flak Turret (30,15) (29,15) (30,18).

No plan holds on paper; the best found (falls w7, margin 0.48):

**Level_C1_01_3_best** (falls w7, margin 0.48, 21 coins left, 0 coins on gate repairs)

```
  from wave  1: build Laser Turret at (6,14); upgrade the tower at (6,14)
  from wave  2: upgrade the tower at (6,14)
  from wave  3: build Laser Turret at (5,14); upgrade the tower at (5,14)
  from wave  4: upgrade the tower at (5,14)
  from wave  5: upgrade the tower at (7,14); upgrade the tower at (7,14)
  from wave  6: build Laser Turret at (31,8); upgrade the tower at (31,8)
  from wave  7: build Laser Turret at (6,13); upgrade the tower at (31,8); repair the gates when they would fall or it is cheaper than a gun
  from wave  8: build Laser Turret at (7,14); repair the gates when they would fall or it is cheaper than a gun; repair the gates when they would fall or it is cheaper than a gun
  from wave 11: repair the gates when they would fall or it is cheaper than a gun
```


## Glass Meadows (Level_C1_02) on Easy

Best spots per tower: Gun Turret (7,6) (5,6) (5,7); Cannon (30,15) (30,16) (31,16); Laser Turret (7,6) (6,5) (7,5).

**Level_C1_02_0_plan1** (3* base 700, margin 0.73, 312 coins left, 0 coins on gate repairs)

```
  from wave  1: build Gun Turret at (5,7); build Cannon at (5,6)
  from wave  3: upgrade the tower at (5,7)
  from wave  4: upgrade the tower at (5,7)
  from wave  7: build Gun Turret at (7,6)
```

**Level_C1_02_0_plan2** (3* base 700, margin 0.49, 327 coins left, 0 coins on gate repairs)

```
  from wave  1: build Cannon at (5,6)
  from wave  3: build Cannon at (4,7); build Cannon at (30,15)
```


## Glass Meadows (Level_C1_02) on Medium

Best spots per tower: Gun Turret (7,6) (5,6) (5,7); Cannon (30,15) (30,16) (31,16); Laser Turret (7,6) (6,5) (7,5).

**Level_C1_02_1_plan1** (3* base 700, margin 0.41, 337 coins left, 0 coins on gate repairs)

```
  from wave  1: build Cannon at (4,7); build Cannon at (5,6)
  from wave  3: build Cannon at (5,7)
  from wave  7: upgrade the tower at (4,7)
  from wave  8: repair the gates when they would fall or it is cheaper than a gun
```

**Level_C1_02_1_plan2** (3* base 700, margin 0.49, 255 coins left, 0 coins on gate repairs)

```
  from wave  1: build Cannon at (31,16); build Cannon at (5,7)
  from wave  2: upgrade the tower at (5,7)
  from wave  6: build Cannon at (4,7)
  from wave  8: build Gun Turret at (6,5)
  from wave  9: upgrade the tower at (6,5)
```


## Glass Meadows (Level_C1_02) on Hard

Best spots per tower: Gun Turret (7,6) (5,6) (5,7); Cannon (30,15) (30,16) (31,16); Laser Turret (7,6) (6,5) (7,5).

**Level_C1_02_2_plan1** (3* base 700, margin 0.45, 392 coins left, 0 coins on gate repairs, repair drone)

```
  from wave  1: build Cannon at (4,7); build Gun Turret at (7,5); build Gun Turret at (7,6); build Cannon at (5,6)
  from wave  2: upgrade the tower at (5,6)
  from wave  3: build Cannon at (5,7)
  from wave  4: buy the RepairDrone
  from wave  5: build Gun Turret at (6,5)
```

**Level_C1_02_2_plan2** (3* base 700, margin 0.40, 387 coins left, 0 coins on gate repairs, repair drone)

```
  from wave  1: build Cannon at (4,7); build Cannon at (5,6)
  from wave  3: build Cannon at (30,15); buy the RepairDrone
  from wave  8: upgrade the tower at (4,7); upgrade the tower at (5,6)
```


## Glass Meadows (Level_C1_02) on Psycho

Best spots per tower: Gun Turret (7,6) (5,6) (5,7); Cannon (30,15) (30,16) (31,16); Laser Turret (7,6) (6,5) (7,5); Flak Turret (32,16) (31,16) (32,19).

No plan holds on paper; the best found (falls w10, margin 0.36):

**Level_C1_02_3_best** (falls w10, margin 0.36, 237 coins left, 102 coins on gate repairs, repair drone)

```
  from wave  1: build Cannon at (5,7); build Gun Turret at (6,5); build Gun Turret at (7,5)
  from wave  2: upgrade the tower at (6,5)
  from wave  3: repair the gates when they would fall or it is cheaper than a gun
  from wave  4: upgrade the tower at (7,5); upgrade the tower at (5,7)
  from wave  5: upgrade the tower at (5,7)
  from wave  6: repair the gates when they would fall or it is cheaper than a gun
  from wave  8: upgrade the tower at (7,5); buy the RepairDrone; repair the gates when they would fall or it is cheaper than a gun
```


## Shard Ridge (Level_C1_03) on Easy

Best spots per tower: Gun Turret (12,11) (13,11) (14,11); Cannon (16,11) (12,10) (12,11); Laser Turret (13,10) (9,10) (11,10); Flak Turret (32,10) (33,10) (34,7).

**Level_C1_03_0_plan1** (3* base 699, margin 0.65, 493 coins left, 0 coins on gate repairs)

```
  from wave  1: build Gun Turret at (13,11); buy the HarvestDrone
  from wave  3: build Cannon at (16,11)
  from wave  4: build Cannon at (14,11)
  from wave  5: upgrade the tower at (13,11)
  from wave  7: build Flak Turret at (32,10)
  from wave 11: upgrade the tower at (32,10)
```

**Level_C1_03_0_plan2** (3* base 696, margin 0.74, 253 coins left, 0 coins on gate repairs)

```
  from wave  1: build Gun Turret at (13,11); build Gun Turret at (17,11)
  from wave  3: build Cannon at (14,11)
  from wave  4: upgrade the tower at (13,11)
  from wave  5: build Flak Turret at (33,10)
  from wave 11: build Cannon at (16,11)
  from wave 15: upgrade the tower at (33,10)
```


## Shard Ridge (Level_C1_03) on Medium

Best spots per tower: Gun Turret (12,11) (13,11) (14,11); Cannon (16,11) (12,10) (12,11); Laser Turret (13,10) (9,10) (11,10); Flak Turret (32,10) (33,10) (34,7).

**Level_C1_03_1_plan1** (3* base 700, margin 0.27, 311 coins left, 0 coins on gate repairs, repair drone)

```
  from wave  1: build Gun Turret at (12,11)
  from wave  2: buy the HarvestDrone
  from wave  3: build Cannon at (16,11)
  from wave  5: buy the RepairDrone
  from wave  9: build Flak Turret at (34,10)
  from wave 10: upgrade the tower at (12,11)
  from wave 11: build Laser Turret at (13,10); build Cannon at (14,11)
  from wave 15: repair the gates when they would fall or it is cheaper than a gun
  from wave 16: upgrade the tower at (34,11)
```

**Level_C1_03_1_plan2** (3* base 700, margin 0.30, 87 coins left, 0 coins on gate repairs, repair drone)

```
  from wave  1: build Cannon at (14,11); build Cannon at (16,11)
  from wave  2: buy the RepairDrone
  from wave  6: build Flak Turret at (34,10)
  from wave  7: build Flak Turret at (34,7)
  from wave  8: upgrade the tower at (34,10)
  from wave  9: build Flak Turret at (33,10)
  from wave 10: upgrade the tower at (34,10)
  from wave 11: upgrade the tower at (16,11); upgrade the tower at (33,10)
  from wave 13: build Gun Turret at (15,11)
  from wave 14: upgrade the tower at (16,11); upgrade the tower at (15,11)
  from wave 15: upgrade the tower at (33,10)
  from wave 16: upgrade the tower at (15,11)
```


## Shard Ridge (Level_C1_03) on Hard

Best spots per tower: Gun Turret (12,11) (13,11) (14,11); Cannon (16,11) (12,10) (12,11); Laser Turret (13,10) (9,10) (11,10); Flak Turret (32,10) (33,10) (34,7).

No plan holds on paper; the best found (falls w14, margin 0.22):

**Level_C1_03_2_best** (falls w14, margin 0.22, 46 coins left, 0 coins on gate repairs, repair drone)

```
  from wave  1: build Cannon at (14,11); build Cannon at (12,10)
  from wave  5: upgrade the tower at (14,11); buy the RepairDrone
  from wave  6: build Flak Turret at (33,10)
  from wave  7: repair the gates when they would fall or it is cheaper than a gun
  from wave  9: build Laser Turret at (14,10)
  from wave 10: upgrade the tower at (14,10)
  from wave 11: upgrade the tower at (33,10)
  from wave 12: repair the gates when they would fall or it is cheaper than a gun
  from wave 15: upgrade the tower at (14,10)
  from wave 16: upgrade the tower at (12,10)
```


## Shard Ridge (Level_C1_03) on Psycho

Best spots per tower: Gun Turret (12,11) (13,11) (14,11); Cannon (16,11) (12,10) (12,11); Laser Turret (13,10) (9,10) (11,10); Flak Turret (32,10) (33,10) (34,7).

No plan holds on paper; the best found (falls w8, margin 0.27):

**Level_C1_03_3_best** (falls w8, margin 0.27, 54 coins left, 0 coins on gate repairs)

```
  from wave  1: build Cannon at (12,10); build Cannon at (12,11)
  from wave  2: build Gun Turret at (13,11)
  from wave  3: build Gun Turret at (15,11); upgrade the tower at (13,11)
  from wave  4: build Gun Turret at (17,11); upgrade the tower at (13,11)
  from wave  5: build Flak Turret at (34,10); build Flak Turret at (33,10); build Flak Turret at (35,7)
  from wave  6: upgrade the tower at (17,11)
  from wave  7: build Gun Turret at (16,11); build Cannon at (14,11)
  from wave  8: build Cannon at (13,10); upgrade the tower at (17,11)
  from wave 10: build Flak Turret at (34,7); upgrade the tower at (13,10)
  from wave 11: build Laser Turret at (9,10); repair the gates when they would fall or it is cheaper than a gun
  from wave 12: upgrade the tower at (12,11); upgrade the tower at (15,11); upgrade the tower at (33,10)
  from wave 14: upgrade the tower at (14,11)
  from wave 15: upgrade the tower at (12,11); upgrade the tower at (15,11); upgrade the tower at (34,7); repair the gates when they would fall or it is cheaper than a gun
```


## Fault Line (Level_C1_04) on Easy

Best spots per tower: Gun Turret (32,6) (33,6) (28,5); Cannon (32,6) (33,6) (33,9); Laser Turret (28,5) (29,5) (32,5); Flak Turret (34,9) (33,9) (34,6).

**Level_C1_04_0_plan1** (3* base 698, margin 0.43, 203 coins left, 0 coins on gate repairs)

```
  from wave  1: build Gun Turret at (28,5); build Gun Turret at (32,5); upgrade the tower at (28,5)
  from wave  2: build Gun Turret at (32,6); build Flak Turret at (34,9)
  from wave  4: build Flak Turret at (33,9)
  from wave 13: upgrade the tower at (29,5)
```

**Level_C1_04_0_plan2** (3* base 700, margin 0.43, 107 coins left, 26 coins on gate repairs, repair drone)

```
  from wave  1: build Gun Turret at (32,6); build Gun Turret at (29,6)
  from wave  2: build Gun Turret at (32,5); build Flak Turret at (34,6); build Cannon at (33,10); buy the RepairDrone
  from wave  3: repair the gates when they would fall or it is cheaper than a gun
  from wave  4: build Laser Turret at (33,5); build Flak Turret at (32,9)
  from wave  5: upgrade the tower at (34,6); upgrade the tower at (33,10); repair the gates when they would fall or it is cheaper than a gun
  from wave  6: build Laser Turret at (28,5)
  from wave  7: build Gun Turret at (33,6); repair the gates when they would fall or it is cheaper than a gun
  from wave  8: build Gun Turret at (29,5); build Flak Turret at (31,9); buy the AttackDrone
  from wave  9: build Laser Turret at (33,9)
  from wave 10: build Flak Turret at (34,9)
  from wave 11: upgrade the tower at (28,5)
  from wave 12: upgrade the tower at (33,5); upgrade the tower at (32,9); upgrade the tower at (33,6)
  from wave 13: upgrade the tower at (34,6)
  from wave 14: upgrade the tower at (33,6)
  from wave 15: upgrade the tower at (33,5)
  from wave 16: upgrade the tower at (29,6); upgrade the tower at (28,5)
```


## Fault Line (Level_C1_04) on Medium

Best spots per tower: Gun Turret (32,6) (33,6) (28,5); Cannon (32,6) (33,6) (33,9); Laser Turret (28,5) (29,5) (32,5); Flak Turret (34,9) (33,9) (34,6).

**Level_C1_04_1_plan1** (3* base 700, margin 0.16, 98 coins left, 0 coins on gate repairs, repair drone)

```
  from wave  1: build Gun Turret at (33,6); upgrade the tower at (33,6)
  from wave  2: buy the HarvestDrone; buy the RepairDrone
  from wave  3: build Cannon at (33,10)
  from wave  4: build Flak Turret at (34,9)
  from wave  6: build Flak Turret at (34,6)
  from wave  8: upgrade the tower at (33,10)
  from wave 12: repair the gates when they would fall or it is cheaper than a gun
  from wave 13: upgrade the tower at (33,6)
  from wave 14: build Cannon at (29,6)
  from wave 15: upgrade the tower at (34,6)
  from wave 16: upgrade the tower at (34,9); upgrade the tower at (29,6)
```

**Level_C1_04_1_plan2** (2* base 610, margin 0.50, 30 coins left, 80 coins on gate repairs)

```
  from wave  1: build Gun Turret at (29,5); build Gun Turret at (33,6); build Flak Turret at (34,9)
  from wave  2: upgrade the tower at (33,6)
  from wave  5: upgrade the tower at (33,6)
  from wave  6: repair the gates when they would fall or it is cheaper than a gun
  from wave  7: build Gun Turret at (32,6)
  from wave 10: upgrade the tower at (32,6)
  from wave 13: repair the gates when they would fall or it is cheaper than a gun
  from wave 14: build Gun Turret at (32,5)
  from wave 15: upgrade the tower at (32,6); upgrade the tower at (32,5)
  from wave 16: upgrade the tower at (32,5)
```


## Fault Line (Level_C1_04) on Hard

Best spots per tower: Gun Turret (32,6) (33,6) (28,5); Cannon (32,6) (33,6) (33,9); Laser Turret (28,5) (29,5) (32,5); Flak Turret (34,9) (33,9) (34,6).

**Level_C1_04_2_plan1** (2* base 468, margin 0.35, 50 coins left, 0 coins on gate repairs, repair drone)

```
  from wave  1: build Gun Turret at (29,5); build Cannon at (32,6); build Flak Turret at (33,9)
  from wave  2: buy the RepairDrone
  from wave  4: build Cannon at (32,5)
  from wave  7: build Gun Turret at (28,5); upgrade the tower at (33,9); upgrade the tower at (32,5)
  from wave  9: upgrade the tower at (28,5)
```

**Level_C1_04_2_plan2** (1* base 386, margin 0.35, 30 coins left, 0 coins on gate repairs, repair drone)

```
  from wave  1: build Gun Turret at (33,6); build Flak Turret at (34,6)
  from wave  2: build Cannon at (33,9); buy the RepairDrone
  from wave  3: build Gun Turret at (32,6); build Cannon at (29,6)
  from wave  4: build Flak Turret at (32,9); upgrade the tower at (33,9)
  from wave  5: build Flak Turret at (34,9); upgrade the tower at (34,6)
  from wave  7: build Laser Turret at (28,5); upgrade the tower at (32,9)
  from wave  9: upgrade the tower at (32,9); upgrade the tower at (34,9)
  from wave 10: upgrade the tower at (33,10)
  from wave 12: upgrade the tower at (34,6)
  from wave 13: build Laser Turret at (29,5)
  from wave 16: upgrade the tower at (33,10)
```


## Fault Line (Level_C1_04) on Psycho

Best spots per tower: Gun Turret (32,6) (33,6) (28,5); Cannon (32,6) (33,6) (33,9); Laser Turret (28,5) (29,5) (32,5); Flak Turret (34,9) (33,9) (34,6).

No plan holds on paper; the best found (falls w12, margin 0.29):

**Level_C1_04_3_best** (falls w12, margin 0.29, 52 coins left, 0 coins on gate repairs)

```
  from wave  1: build Cannon at (33,10); build Gun Turret at (33,6); build Flak Turret at (32,9)
  from wave  2: upgrade the tower at (33,6)
  from wave  4: build Gun Turret at (29,6)
  from wave  5: build Gun Turret at (32,5); upgrade the tower at (29,6)
  from wave  6: upgrade the tower at (33,10); upgrade the tower at (32,9)
  from wave  8: build Cannon at (32,6); build Flak Turret at (34,9); buy the HarvestDrone; buy the AttackDrone; repair the gates when they would fall or it is cheaper than a gun
  from wave 11: build Cannon at (33,9); upgrade the tower at (32,9)
  from wave 13: build Laser Turret at (28,5); upgrade the tower at (32,5); upgrade the tower at (32,5)
  from wave 14: build Laser Turret at (33,5); upgrade the tower at (34,9); upgrade the tower at (33,9); upgrade the tower at (28,5)
  from wave 16: upgrade the tower at (34,9); upgrade the tower at (33,9); upgrade the tower at (28,5)
```


## Quartz Flats (Level_C1_05) on Easy

Best spots per tower: Gun Turret (7,5) (7,4) (20,5); Cannon (7,5) (7,4) (20,5); Laser Turret (7,5) (6,6) (7,4); Flak Turret (34,4) (34,5) (35,4).

**Level_C1_05_0_plan1** (3* base 700, margin 0.53, 446 coins left, 0 coins on gate repairs, repair drone)

```
  from wave  1: build Gun Turret at (7,4)
  from wave  2: build Flak Turret at (34,4); buy the HarvestDrone
  from wave  3: build Gun Turret at (21,5)
  from wave  4: build Cannon at (6,5)
  from wave  5: upgrade the tower at (21,5)
  from wave  8: upgrade the tower at (21,5)
  from wave 11: buy the RepairDrone
  from wave 13: build Gun Turret at (7,5)
  from wave 14: build Gun Turret at (20,5)
  from wave 17: upgrade the tower at (6,6)
```

**Level_C1_05_0_plan2** (3* base 700, margin 0.40, 279 coins left, 0 coins on gate repairs, repair drone)

```
  from wave  1: build Gun Turret at (7,4); build Cannon at (7,5)
  from wave  2: build Flak Turret at (33,4)
  from wave  3: repair the gates when they would fall or it is cheaper than a gun; upgrade the tower at (20,5)
  from wave  5: build Laser Turret at (6,6); buy the RepairDrone
  from wave  7: build Flak Turret at (35,5)
```


## Quartz Flats (Level_C1_05) on Medium

Best spots per tower: Gun Turret (7,5) (7,4) (20,5); Cannon (7,5) (7,4) (20,5); Laser Turret (7,5) (6,6) (7,4); Flak Turret (34,4) (34,5) (35,4).

**Level_C1_05_1_plan1** (3* base 700, margin 0.25, 337 coins left, 0 coins on gate repairs, repair drone)

```
  from wave  1: build Cannon at (7,4); build Cannon at (7,5); build Flak Turret at (34,4)
  from wave  3: buy the RepairDrone
  from wave  4: build Gun Turret at (6,5)
  from wave  6: build Cannon at (6,6)
  from wave  7: upgrade the tower at (7,4)
  from wave  8: upgrade the tower at (21,5)
  from wave 11: build Gun Turret at (20,5)
  from wave 12: upgrade the tower at (21,5)
  from wave 15: repair the gates when they would fall or it is cheaper than a gun
```

**Level_C1_05_1_plan2** (3* base 698, margin 0.57, 119 coins left, 0 coins on gate repairs)

```
  from wave  1: build Cannon at (7,4); build Cannon at (6,6)
  from wave  3: build Flak Turret at (35,5)
  from wave  5: upgrade the tower at (6,6)
  from wave  6: upgrade the tower at (7,4); upgrade the tower at (35,5)
  from wave  8: build Flak Turret at (35,4)
  from wave 11: build Flak Turret at (33,5); build Laser Turret at (8,6)
  from wave 12: upgrade the tower at (33,5)
  from wave 13: upgrade the tower at (6,6)
  from wave 14: upgrade the tower at (21,5)
  from wave 15: upgrade the tower at (7,4)
  from wave 16: upgrade the tower at (33,5)
  from wave 17: upgrade the tower at (21,5)
```


## Quartz Flats (Level_C1_05) on Hard

Best spots per tower: Gun Turret (7,5) (7,4) (20,5); Cannon (7,5) (7,4) (20,5); Laser Turret (7,5) (6,6) (7,4); Flak Turret (34,4) (34,5) (35,4).

No plan holds on paper; the best found (falls w15, margin 0.23):

**Level_C1_05_2_best** (falls w15, margin 0.23, 27 coins left, 0 coins on gate repairs, repair drone)

```
  from wave  1: build Cannon at (6,5); build Gun Turret at (6,6)
  from wave  2: build Flak Turret at (33,5)
  from wave  3: upgrade the tower at (6,5)
  from wave  4: buy the RepairDrone
  from wave  7: build Flak Turret at (34,4)
  from wave  8: build Laser Turret at (8,6)
  from wave 10: build Cannon at (20,4)
  from wave 11: upgrade the tower at (20,4)
  from wave 13: upgrade the tower at (33,5)
  from wave 14: build Gun Turret at (21,5)
  from wave 15: build Cannon at (7,4); upgrade the tower at (7,5)
  from wave 17: upgrade the tower at (7,4); upgrade the tower at (7,4); buy the AttackDrone
```


## Quartz Flats (Level_C1_05) on Psycho

Best spots per tower: Gun Turret (7,5) (7,4) (20,5); Cannon (7,5) (7,4) (20,5); Laser Turret (7,5) (6,6) (7,4); Flak Turret (34,4) (34,5) (35,4).

No plan holds on paper; the best found (falls w10, margin 0.09):

**Level_C1_05_3_best** (falls w10, margin 0.09, 112 coins left, 186 coins on gate repairs)

```
  from wave  1: build Cannon at (7,5); build Cannon at (6,5)
  from wave  2: build Flak Turret at (34,5); upgrade the tower at (6,5)
  from wave  3: build Laser Turret at (7,4); repair the gates when they would fall or it is cheaper than a gun
  from wave  4: repair the gates when they would fall or it is cheaper than a gun
  from wave  5: build Cannon at (6,6)
  from wave  8: build Laser Turret at (20,5); repair the gates when they would fall or it is cheaper than a gun
  from wave 10: build Flak Turret at (34,4)
  from wave 12: upgrade the tower at (6,5)
  from wave 13: build Laser Turret at (8,6); build Cannon at (20,4); upgrade the tower at (6,6); upgrade the tower at (20,5)
  from wave 15: build Laser Turret at (21,5); upgrade the tower at (8,6)
  from wave 16: upgrade the tower at (20,4); upgrade the tower at (21,5)
  from wave 17: upgrade the tower at (21,5)
```


## Crysium Vein (Level_C1_06) on Easy

Best spots per tower: Gun Turret (12,10) (12,11) (13,10); Cannon (17,11) (17,7) (17,10); Laser Turret (13,10) (11,10) (12,10); Flak Turret (36,10) (36,7) (36,11).

**Level_C1_06_0_plan1** (3* base 700, margin 0.59, 429 coins left, 0 coins on gate repairs)

```
  from wave  1: build Cannon at (12,10); build Gun Turret at (13,11); upgrade the tower at (13,11)
  from wave  6: build Flak Turret at (35,10)
  from wave  8: upgrade the tower at (13,11)
  from wave 13: upgrade the tower at (35,10)
  from wave 15: build Laser Turret at (13,10)
```

**Level_C1_06_0_plan2** (3* base 700, margin 0.48, 426 coins left, 0 coins on gate repairs, repair drone)

```
  from wave  1: build Gun Turret at (13,11); build Gun Turret at (12,10); build Gun Turret at (13,10)
  from wave  4: build Flak Turret at (36,10)
  from wave  7: build Cannon at (17,10)
  from wave  8: upgrade the tower at (12,10)
  from wave 11: buy the RepairDrone
```


## Crysium Vein (Level_C1_06) on Medium

Best spots per tower: Gun Turret (12,10) (12,11) (13,10); Cannon (17,11) (17,7) (17,10); Laser Turret (13,10) (11,10) (12,10); Flak Turret (36,10) (36,7) (36,11).

**Level_C1_06_1_plan1** (3* base 700, margin 0.25, 335 coins left, 0 coins on gate repairs, repair drone)

```
  from wave  1: build Cannon at (12,10); build Cannon at (17,10)
  from wave  3: repair the gates when they would fall or it is cheaper than a gun
  from wave  5: build Flak Turret at (36,10); buy the RepairDrone
  from wave 10: build Cannon at (16,11)
  from wave 11: repair the gates when they would fall or it is cheaper than a gun
```

**Level_C1_06_1_plan2** (3* base 700, margin 0.32, 274 coins left, 0 coins on gate repairs, repair drone)

```
  from wave  1: build Cannon at (12,10); build Cannon at (16,11)
  from wave  3: build Flak Turret at (36,10)
  from wave  7: buy the RepairDrone
  from wave 12: build Gun Turret at (13,10); upgrade the tower at (16,11)
  from wave 14: build Gun Turret at (14,11)
```


## Crysium Vein (Level_C1_06) on Hard

Best spots per tower: Gun Turret (12,10) (12,11) (13,10); Cannon (17,11) (17,7) (17,10); Laser Turret (13,10) (11,10) (12,10); Flak Turret (36,10) (36,7) (36,11).

No plan holds on paper; the best found (falls w12, margin 0.07):

**Level_C1_06_2_best** (falls w12, margin 0.07, 33 coins left, 62 coins on gate repairs)

```
  from wave  1: build Cannon at (17,11); build Gun Turret at (13,11); build Cannon at (18,11)
  from wave  2: build Gun Turret at (14,11)
  from wave  4: build Flak Turret at (36,7); repair the gates when they would fall or it is cheaper than a gun
  from wave  5: build Gun Turret at (14,7); build Flak Turret at (36,10)
  from wave  6: build Laser Turret at (11,10); build Gun Turret at (12,10); build Laser Turret at (14,10); build Flak Turret at (36,11)
  from wave  7: upgrade the tower at (36,11); repair the gates when they would fall or it is cheaper than a gun
  from wave  8: upgrade the tower at (36,10); buy the HarvestDrone
  from wave  9: upgrade the tower at (12,10)
  from wave 10: build Flak Turret at (34,10)
  from wave 13: build Cannon at (17,7)
  from wave 14: build Gun Turret at (12,11); upgrade the tower at (14,10)
  from wave 15: upgrade the tower at (36,10)
  from wave 16: build Gun Turret at (13,10)
  from wave 17: upgrade the tower at (13,10)
  from wave 18: upgrade the tower at (17,11); upgrade the tower at (14,10)
```


## Crysium Vein (Level_C1_06) on Psycho

Best spots per tower: Gun Turret (12,10) (12,11) (13,10); Cannon (17,11) (17,7) (17,10); Laser Turret (13,10) (11,10) (12,10); Flak Turret (36,10) (36,7) (36,11).

No plan holds on paper; the best found (falls w5, margin 0.00):

**Level_C1_06_3_best** (falls w5, margin 0.00, 91 coins left, 0 coins on gate repairs)

```
  from wave  1: build Cannon at (17,7); build Cannon at (18,11); build Cannon at (12,10); build Flak Turret at (35,10)
  from wave  3: build Gun Turret at (12,11); upgrade the tower at (18,11)
  from wave  4: build Cannon at (17,11); upgrade the tower at (35,10)
  from wave  5: build Laser Turret at (13,11)
  from wave  6: build Gun Turret at (14,7); upgrade the tower at (17,11)
  from wave  8: upgrade the tower at (35,10)
  from wave  9: build Gun Turret at (13,10); build Cannon at (17,10)
  from wave 10: sell the tower at (14,10)
  from wave 11: build Cannon at (16,11); build Laser Turret at (14,10)
  from wave 14: build Laser Turret at (15,10); upgrade the tower at (17,10); buy the AttackDrone
  from wave 17: upgrade the tower at (15,10)
  from wave 18: build Laser Turret at (14,10); upgrade the tower at (17,7); upgrade the tower at (17,11); upgrade the tower at (15,10)
```

