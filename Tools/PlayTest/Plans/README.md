# Winning plans per level and difficulty

Written by the plan search (stage A, `Tools/Generate/Balance/5` or `PlanSearchTests`) on 2026-11-07 13:33; calibration 2026-11-07 01:56, seeds 1,2,3, step 0.2, inputs 956d2750 (current).

Each plan is a priority list: a step is taken from its wave on, as soon as the money is there (an earlier step blocks the later ones).
Cells are (x,y) on the level grid. The paper verdict comes from the leak model (the base survives, stars by the base left); what the
same plans did in play is in `Tools/PlayTest/plan_replay.txt`, the obvious family in `Tools/PlayTest/yardstick.txt`. A cell without a
winning plan shows the best plan found and where it falls: a design question, not a plan to follow.

## Landing Site (Level_C1_01) on Easy

Best spots per tower: Gun Turret (5,14) (6,14) (31,8); Cannon (5,14) (31,8) (6,14).

**Level_C1_01_0_plan1** (3* base 600, margin 0.68, 307 coins left, 0 coins on gate repairs)

```
  from wave  1: build Gun Turret at (5,14); build Gun Turret at (6,14)
  from wave  2: build Gun Turret at (31,8)
  from wave  4: upgrade the tower at (5,14)
  from wave  6: upgrade the tower at (6,14)
```

**Level_C1_01_0_plan2** (3* base 600, margin 0.59, 310 coins left, 0 coins on gate repairs)

```
  from wave  1: build Cannon at (13,7); build Gun Turret at (5,13)
  from wave  3: upgrade the tower at (5,13)
  from wave  4: build Gun Turret at (31,8)
```


## Landing Site (Level_C1_01) on Medium

Best spots per tower: Gun Turret (5,14) (6,14) (31,8); Cannon (5,14) (31,8) (6,14).

**Level_C1_01_1_plan1** (3* base 600, margin 0.40, 394 coins left, 0 coins on gate repairs)

```
  from wave  1: build Cannon at (13,7); build Cannon at (14,7)
  from wave  3: build Gun Turret at (6,14); repair the gates when they would fall or it is cheaper than a gun
```

**Level_C1_01_1_plan2** (3* base 600, margin 0.64, 306 coins left, 0 coins on gate repairs)

```
  from wave  1: build Gun Turret at (5,13); build Cannon at (14,7); build Gun Turret at (5,15)
  from wave  2: build Gun Turret at (5,14)
  from wave  5: upgrade the tower at (5,13)
  from wave  7: upgrade the tower at (5,13)
```


## Landing Site (Level_C1_01) on Hard

Best spots per tower: Gun Turret (5,14) (6,14) (31,8); Cannon (5,14) (31,8) (6,14).

**Level_C1_01_2_plan1** (3* base 600, margin 0.56, 152 coins left, 162 coins on gate repairs)

```
  from wave  1: build Cannon at (13,7); build Cannon at (14,7)
  from wave  2: build Gun Turret at (31,8)
  from wave  3: build Gun Turret at (6,14)
  from wave  5: upgrade the tower at (31,8)
  from wave  6: upgrade the tower at (31,8)
  from wave  8: upgrade the tower at (14,7)
  from wave  9: build Gun Turret at (5,14); repair the gates when they would fall or it is cheaper than a gun
  from wave 10: upgrade the tower at (13,7)
  from wave 11: upgrade the tower at (14,7); repair the gates when they would fall or it is cheaper than a gun
```


## Landing Site (Level_C1_01) on Psycho

Best spots per tower: Gun Turret (5,14) (6,14) (31,8); Cannon (5,14) (31,8) (6,14); Laser Turret (6,14) (5,14) (7,14); Flak Turret (30,15) (29,15) (30,18).

No plan holds on paper; the best found (falls w7, margin 0.48):

**Level_C1_01_3_best** (falls w7, margin 0.48, 18 coins left, 0 coins on gate repairs)

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

**Level_C1_02_0_plan1** (3* base 700, margin 0.72, 309 coins left, 0 coins on gate repairs)

```
  from wave  1: build Gun Turret at (5,7); build Cannon at (5,6)
  from wave  3: upgrade the tower at (5,7)
  from wave  4: upgrade the tower at (5,7)
  from wave  7: build Gun Turret at (7,6)
```

**Level_C1_02_0_plan2** (3* base 700, margin 0.48, 325 coins left, 0 coins on gate repairs)

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

**Level_C1_02_2_plan1** (3* base 700, margin 0.45, 368 coins left, 0 coins on gate repairs, repair drone)

```
  from wave  1: build Gun Turret at (6,5); build Cannon at (5,6)
  from wave  2: upgrade the tower at (5,6)
  from wave  3: upgrade the tower at (6,5)
  from wave  4: upgrade the tower at (6,5)
  from wave  7: buy the RepairDrone
  from wave  8: build Cannon at (5,7)
  from wave  9: repair the gates when they would fall or it is cheaper than a gun; repair the gates when they would fall or it is cheaper than a gun
  from wave 10: upgrade the tower at (31,16)
```

**Level_C1_02_2_plan2** (3* base 700, margin 0.45, 327 coins left, 0 coins on gate repairs, repair drone)

```
  from wave  1: build Cannon at (4,7); build Gun Turret at (7,5); build Gun Turret at (7,6); build Cannon at (5,6)
  from wave  2: upgrade the tower at (5,6)
  from wave  3: build Cannon at (5,7)
  from wave  5: build Gun Turret at (6,5)
  from wave  6: buy the RepairDrone
```


## Glass Meadows (Level_C1_02) on Psycho

Best spots per tower: Gun Turret (7,6) (5,6) (5,7); Cannon (30,15) (30,16) (31,16); Laser Turret (7,6) (6,5) (7,5); Flak Turret (32,16) (31,16) (32,19).

No plan holds on paper; the best found (falls w8, margin 0.27):

**Level_C1_02_3_best** (falls w8, margin 0.27, 135 coins left, 240 coins on gate repairs, repair drone)

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

**Level_C1_03_0_plan1** (3* base 700, margin 0.72, 503 coins left, 0 coins on gate repairs)

```
  from wave  1: build Gun Turret at (13,11); buy the HarvestDrone
  from wave  3: build Cannon at (16,11); upgrade the tower at (13,11)
  from wave  4: build Cannon at (14,11)
  from wave  5: build Flak Turret at (32,10)
  from wave 13: upgrade the tower at (32,10)
```

**Level_C1_03_0_plan2** (3* base 700, margin 0.75, 312 coins left, 0 coins on gate repairs)

```
  from wave  1: build Gun Turret at (12,11); build Laser Turret at (13,10)
  from wave  3: build Cannon at (16,11); build Flak Turret at (34,11)
  from wave  6: upgrade the tower at (14,11)
  from wave  7: upgrade the tower at (34,11)
```


## Shard Ridge (Level_C1_03) on Medium

Best spots per tower: Gun Turret (12,11) (13,11) (14,11); Cannon (16,11) (12,10) (12,11); Laser Turret (13,10) (9,10) (11,10); Flak Turret (32,10) (33,10) (34,7).

**Level_C1_03_1_plan1** (3* base 700, margin 0.36, 363 coins left, 0 coins on gate repairs, repair drone)

```
  from wave  1: build Gun Turret at (12,11)
  from wave  2: buy the HarvestDrone
  from wave  3: build Cannon at (16,11)
  from wave  5: buy the RepairDrone
  from wave  7: upgrade the tower at (12,11)
  from wave  9: build Flak Turret at (34,10)
  from wave 11: build Laser Turret at (12,10); build Cannon at (14,11)
  from wave 15: repair the gates when they would fall or it is cheaper than a gun
  from wave 16: upgrade the tower at (34,11)
```

**Level_C1_03_1_plan2** (3* base 697, margin 0.55, 118 coins left, 0 coins on gate repairs)

```
  from wave  1: build Cannon at (13,11); build Laser Turret at (11,10)
  from wave  2: build Flak Turret at (34,10)
  from wave  4: upgrade the tower at (34,10)
  from wave  5: build Cannon at (12,11); build Cannon at (13,10)
  from wave  6: build Flak Turret at (34,7)
  from wave  7: build Laser Turret at (14,10)
  from wave  9: build Gun Turret at (14,11)
  from wave 10: build Flak Turret at (35,7); build Flak Turret at (33,10)
  from wave 13: build Gun Turret at (15,11); upgrade the tower at (33,10)
  from wave 14: upgrade the tower at (14,10)
  from wave 15: upgrade the tower at (13,11); upgrade the tower at (11,10); upgrade the tower at (14,10); upgrade the tower at (33,10); upgrade the tower at (15,11)
  from wave 16: upgrade the tower at (15,11)
```


## Shard Ridge (Level_C1_03) on Hard

Best spots per tower: Gun Turret (12,11) (13,11) (14,11); Cannon (16,11) (12,10) (12,11); Laser Turret (13,10) (9,10) (11,10); Flak Turret (32,10) (33,10) (34,7).

No plan holds on paper; the best found (falls w13, margin 0.21):

**Level_C1_03_2_best** (falls w13, margin 0.21, 45 coins left, 0 coins on gate repairs, repair drone)

```
  from wave  1: build Gun Turret at (16,11); build Cannon at (13,10); build Cannon at (12,10)
  from wave  2: buy the RepairDrone
  from wave  4: build Flak Turret at (35,7)
  from wave  5: upgrade the tower at (35,7)
  from wave  6: build Gun Turret at (17,11)
  from wave  7: upgrade the tower at (13,10); buy the HarvestDrone
  from wave  9: build Flak Turret at (34,7)
  from wave 10: upgrade the tower at (13,10)
  from wave 11: upgrade the tower at (16,11)
  from wave 12: build Cannon at (12,11)
  from wave 14: upgrade the tower at (12,10)
```


## Shard Ridge (Level_C1_03) on Psycho

Best spots per tower: Gun Turret (12,11) (13,11) (14,11); Cannon (16,11) (12,10) (12,11); Laser Turret (13,10) (9,10) (11,10); Flak Turret (32,10) (33,10) (34,7).

No plan holds on paper; the best found (falls w8, margin 0.24):

**Level_C1_03_3_best** (falls w8, margin 0.24, 69 coins left, 0 coins on gate repairs)

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

**Level_C1_04_0_plan1** (3* base 697, margin 0.56, 144 coins left, 0 coins on gate repairs, repair drone)

```
  from wave  1: build Gun Turret at (32,6); upgrade the tower at (32,6)
  from wave  2: build Flak Turret at (34,9); upgrade the tower at (32,6); buy the HarvestDrone
  from wave  3: build Gun Turret at (33,6); upgrade the tower at (33,6); buy the RepairDrone
  from wave  4: build Gun Turret at (28,5); upgrade the tower at (33,6)
  from wave  5: build Flak Turret at (33,9); upgrade the tower at (28,5); upgrade the tower at (28,5)
  from wave  6: build Gun Turret at (29,5); upgrade the tower at (29,5)
  from wave  7: build Gun Turret at (29,6); upgrade the tower at (29,5)
```

**Level_C1_04_0_plan2** (3* base 700, margin 0.59, 108 coins left, 0 coins on gate repairs)

```
  from wave  1: build Gun Turret at (28,5); build Gun Turret at (32,5); build Gun Turret at (29,5)
  from wave  4: build Flak Turret at (34,6)
  from wave  5: build Cannon at (33,6)
  from wave  7: upgrade the tower at (29,5)
  from wave  8: build Flak Turret at (34,10); build Flak Turret at (32,9); upgrade the tower at (28,5); upgrade the tower at (33,6)
  from wave 10: upgrade the tower at (32,5)
  from wave 11: upgrade the tower at (33,6)
  from wave 13: build Gun Turret at (32,6)
  from wave 15: upgrade the tower at (32,6)
```


## Fault Line (Level_C1_04) on Medium

Best spots per tower: Gun Turret (32,6) (33,6) (28,5); Cannon (32,6) (33,6) (33,9); Laser Turret (28,5) (29,5) (32,5); Flak Turret (34,9) (33,9) (34,6).

**Level_C1_04_1_plan1** (2* base 608, margin 0.08, 41 coins left, 0 coins on gate repairs, repair drone)

```
  from wave  1: build Gun Turret at (33,6); build Cannon at (33,9); upgrade the tower at (33,6)
  from wave  2: buy the RepairDrone
  from wave  4: build Flak Turret at (34,9)
  from wave  6: build Flak Turret at (34,6)
  from wave 10: upgrade the tower at (34,6)
  from wave 12: repair the gates when they would fall or it is cheaper than a gun
  from wave 13: upgrade the tower at (33,6)
  from wave 14: build Cannon at (29,6)
  from wave 15: upgrade the tower at (34,6)
  from wave 16: upgrade the tower at (34,9); upgrade the tower at (29,6)
```

**Level_C1_04_1_plan2** (2* base 595, margin 0.46, 21 coins left, 107 coins on gate repairs)

```
  from wave  1: build Gun Turret at (29,5); build Gun Turret at (33,6); build Flak Turret at (34,9)
  from wave  2: upgrade the tower at (33,6)
  from wave  5: upgrade the tower at (33,6)
  from wave  6: repair the gates when they would fall or it is cheaper than a gun
  from wave  7: build Gun Turret at (32,6)
  from wave 10: upgrade the tower at (32,6)
  from wave 13: repair the gates when they would fall or it is cheaper than a gun
  from wave 14: build Gun Turret at (32,5)
  from wave 15: upgrade the tower at (32,5)
  from wave 16: upgrade the tower at (32,5)
```


## Fault Line (Level_C1_04) on Hard

Best spots per tower: Gun Turret (32,6) (33,6) (28,5); Cannon (32,6) (33,6) (33,9); Laser Turret (28,5) (29,5) (32,5); Flak Turret (34,9) (33,9) (34,6).

No plan holds on paper; the best found (falls w16, margin 0.29):

**Level_C1_04_2_best** (falls w16, margin 0.29, 15 coins left, 0 coins on gate repairs, repair drone)

```
  from wave  1: build Gun Turret at (29,6); build Cannon at (32,6); build Flak Turret at (33,9)
  from wave  2: buy the RepairDrone
  from wave  4: build Cannon at (33,10); upgrade the tower at (29,6)
  from wave  7: upgrade the tower at (33,9)
  from wave  9: build Gun Turret at (33,6)
  from wave 12: upgrade the tower at (33,6)
```


## Fault Line (Level_C1_04) on Psycho

Best spots per tower: Gun Turret (32,6) (33,6) (28,5); Cannon (32,6) (33,6) (33,9); Laser Turret (28,5) (29,5) (32,5); Flak Turret (34,9) (33,9) (34,6).

No plan holds on paper; the best found (falls w11, margin 0.29):

**Level_C1_04_3_best** (falls w11, margin 0.29, 0 coins left, 0 coins on gate repairs)

```
  from wave  1: build Gun Turret at (33,6); build Cannon at (32,6)
  from wave  3: build Flak Turret at (34,10)
  from wave  4: build Gun Turret at (32,5)
  from wave  5: upgrade the tower at (33,6)
  from wave  7: build Gun Turret at (29,5); upgrade the tower at (32,5)
  from wave 10: build Cannon at (29,6); upgrade the tower at (34,10)
  from wave 11: upgrade the tower at (29,6)
  from wave 12: upgrade the tower at (33,6)
  from wave 13: upgrade the tower at (29,5)
  from wave 14: build Cannon at (33,9); build Gun Turret at (28,5); upgrade the tower at (32,5)
  from wave 15: upgrade the tower at (28,5)
  from wave 16: upgrade the tower at (33,9); upgrade the tower at (33,9); upgrade the tower at (28,5)
```


## Quartz Flats (Level_C1_05) on Easy

Best spots per tower: Gun Turret (7,5) (7,4) (20,5); Cannon (7,5) (7,4) (20,5); Laser Turret (7,5) (6,6) (7,4); Flak Turret (34,4) (34,5) (35,4).

**Level_C1_05_0_plan1** (3* base 700, margin 0.63, 460 coins left, 0 coins on gate repairs)

```
  from wave  1: build Gun Turret at (7,4); build Cannon at (6,6)
  from wave  3: build Flak Turret at (34,4)
  from wave  4: upgrade the tower at (7,4)
  from wave  5: upgrade the tower at (34,4)
  from wave  7: build Cannon at (7,5)
  from wave 12: build Cannon at (6,5)
  from wave 14: upgrade the tower at (20,4)
```

**Level_C1_05_0_plan2** (3* base 700, margin 0.40, 244 coins left, 0 coins on gate repairs, repair drone)

```
  from wave  1: build Gun Turret at (7,4); build Cannon at (7,5)
  from wave  2: build Flak Turret at (35,4)
  from wave  3: repair the gates when they would fall or it is cheaper than a gun
  from wave  5: build Laser Turret at (8,6); build Flak Turret at (35,5); buy the RepairDrone
  from wave  9: upgrade the tower at (6,6)
```


## Quartz Flats (Level_C1_05) on Medium

Best spots per tower: Gun Turret (7,5) (7,4) (20,5); Cannon (7,5) (7,4) (20,5); Laser Turret (7,5) (6,6) (7,4); Flak Turret (34,4) (34,5) (35,4).

**Level_C1_05_1_plan1** (3* base 700, margin 0.32, 129 coins left, 0 coins on gate repairs, repair drone)

```
  from wave  1: build Cannon at (7,5); build Cannon at (6,5); build Flak Turret at (35,4)
  from wave  3: build Cannon at (20,4)
  from wave  4: build Cannon at (20,5); buy the RepairDrone
  from wave  5: build Cannon at (7,4)
  from wave  6: build Laser Turret at (21,5); build Laser Turret at (6,6); upgrade the tower at (7,5)
  from wave  7: upgrade the tower at (7,4)
  from wave  8: upgrade the tower at (20,4)
  from wave  9: upgrade the tower at (6,6)
  from wave 10: upgrade the tower at (6,5); upgrade the tower at (20,4)
  from wave 12: build Laser Turret at (8,6)
  from wave 13: upgrade the tower at (21,5)
  from wave 14: upgrade the tower at (7,4); upgrade the tower at (8,6)
  from wave 15: upgrade the tower at (35,4); upgrade the tower at (21,5); upgrade the tower at (6,6)
  from wave 16: upgrade the tower at (7,5)
  from wave 17: upgrade the tower at (6,5); upgrade the tower at (8,6)
```

**Level_C1_05_1_plan2** (3* base 700, margin 0.41, 88 coins left, 0 coins on gate repairs, repair drone)

```
  from wave  1: build Cannon at (6,6); build Gun Turret at (7,4)
  from wave  3: build Flak Turret at (33,5); upgrade the tower at (6,6); buy the RepairDrone
  from wave  4: build Laser Turret at (7,5)
  from wave  8: build Cannon at (20,4); build Flak Turret at (34,4); buy the HarvestDrone
  from wave  9: build Cannon at (20,5); upgrade the tower at (6,5)
  from wave 10: upgrade the tower at (6,5)
  from wave 11: upgrade the tower at (20,5)
  from wave 13: upgrade the tower at (20,5)
  from wave 15: repair the gates when they would fall or it is cheaper than a gun
```


## Quartz Flats (Level_C1_05) on Hard

Best spots per tower: Gun Turret (7,5) (7,4) (20,5); Cannon (7,5) (7,4) (20,5); Laser Turret (7,5) (6,6) (7,4); Flak Turret (34,4) (34,5) (35,4).

No plan holds on paper; the best found (falls w15, margin 0.10):

**Level_C1_05_2_best** (falls w15, margin 0.10, 7 coins left, 68 coins on gate repairs)

```
  from wave  1: build Cannon at (6,5); build Cannon at (7,5)
  from wave  3: build Flak Turret at (34,4)
  from wave  6: repair the gates when they would fall or it is cheaper than a gun
  from wave  7: build Laser Turret at (21,5); upgrade the tower at (7,5); repair the gates when they would fall or it is cheaper than a gun
  from wave 10: build Flak Turret at (35,4)
  from wave 11: build Flak Turret at (34,5)
  from wave 12: upgrade the tower at (35,4)
  from wave 13: upgrade the tower at (34,4)
  from wave 14: upgrade the tower at (34,4); upgrade the tower at (34,5)
  from wave 16: upgrade the tower at (34,5)
  from wave 17: upgrade the tower at (6,5); upgrade the tower at (6,5); upgrade the tower at (7,5); repair the gates when they would fall or it is cheaper than a gun
```


## Quartz Flats (Level_C1_05) on Psycho

Best spots per tower: Gun Turret (7,5) (7,4) (20,5); Cannon (7,5) (7,4) (20,5); Laser Turret (7,5) (6,6) (7,4); Flak Turret (34,4) (34,5) (35,4).

No plan holds on paper; the best found (falls w8, margin 0.19):

**Level_C1_05_3_best** (falls w8, margin 0.19, 16 coins left, 0 coins on gate repairs)

```
  from wave  1: build Cannon at (7,5); build Gun Turret at (6,6); build Flak Turret at (35,5)
  from wave  3: build Laser Turret at (8,6); build Flak Turret at (34,4)
  from wave  4: upgrade the tower at (8,6)
  from wave  5: build Cannon at (6,5); upgrade the tower at (7,5)
  from wave  6: upgrade the tower at (7,5)
  from wave  7: build Laser Turret at (7,4)
  from wave  9: build Cannon at (20,4); build Laser Turret at (20,5)
  from wave 10: upgrade the tower at (35,5); upgrade the tower at (7,4); upgrade the tower at (20,5)
  from wave 11: upgrade the tower at (7,4); sell the tower at (21,5)
  from wave 12: upgrade the tower at (35,5)
  from wave 14: build Laser Turret at (21,5); upgrade the tower at (6,5)
  from wave 15: upgrade the tower at (20,4)
  from wave 16: upgrade the tower at (6,5)
  from wave 17: build Laser Turret at (21,5); upgrade the tower at (6,6); upgrade the tower at (21,5)
```


## Crysium Vein (Level_C1_06) on Easy

Best spots per tower: Gun Turret (12,10) (12,11) (13,10); Cannon (17,11) (17,7) (17,10); Laser Turret (13,10) (11,10) (12,10); Flak Turret (36,10) (36,7) (36,11).

**Level_C1_06_0_plan1** (3* base 700, margin 0.62, 451 coins left, 0 coins on gate repairs)

```
  from wave  1: build Cannon at (12,10); build Gun Turret at (13,11); upgrade the tower at (13,11)
  from wave  6: build Flak Turret at (35,10)
  from wave  8: upgrade the tower at (13,11)
  from wave 13: build Laser Turret at (15,10); upgrade the tower at (35,10)
```

**Level_C1_06_0_plan2** (3* base 700, margin 0.53, 476 coins left, 0 coins on gate repairs)

```
  from wave  1: build Gun Turret at (13,11); build Gun Turret at (12,10)
  from wave  3: build Gun Turret at (14,11); build Cannon at (17,10)
  from wave  4: build Flak Turret at (36,10)
  from wave  5: upgrade the tower at (14,11)
  from wave  8: upgrade the tower at (12,10)
  from wave 10: upgrade the tower at (36,10)
```


## Crysium Vein (Level_C1_06) on Medium

Best spots per tower: Gun Turret (12,10) (12,11) (13,10); Cannon (17,11) (17,7) (17,10); Laser Turret (13,10) (11,10) (12,10); Flak Turret (36,10) (36,7) (36,11).

**Level_C1_06_1_plan1** (3* base 700, margin 0.27, 364 coins left, 0 coins on gate repairs, repair drone)

```
  from wave  1: build Cannon at (12,10); build Cannon at (17,10)
  from wave  3: repair the gates when they would fall or it is cheaper than a gun
  from wave  5: build Flak Turret at (36,10); buy the RepairDrone
  from wave 10: build Cannon at (16,11)
  from wave 11: repair the gates when they would fall or it is cheaper than a gun
```

**Level_C1_06_1_plan2** (3* base 700, margin 0.25, 348 coins left, 0 coins on gate repairs, repair drone)

```
  from wave  1: build Cannon at (12,10); build Cannon at (16,11)
  from wave  3: build Flak Turret at (36,10)
  from wave  7: buy the RepairDrone
  from wave 12: build Gun Turret at (13,10); build Gun Turret at (14,11)
```


## Crysium Vein (Level_C1_06) on Hard

Best spots per tower: Gun Turret (12,10) (12,11) (13,10); Cannon (17,11) (17,7) (17,10); Laser Turret (13,10) (11,10) (12,10); Flak Turret (36,10) (36,7) (36,11).

**Level_C1_06_2_plan1** (2* base 584, margin 0.50, 309 coins left, 169 coins on gate repairs)

```
  from wave  1: build Cannon at (16,11); build Cannon at (18,11); buy the HarvestDrone
  from wave  3: build Flak Turret at (34,10); repair the gates when they would fall or it is cheaper than a gun
  from wave  4: build Gun Turret at (13,10)
  from wave  5: build Cannon at (17,11); build Gun Turret at (12,10); upgrade the tower at (18,11)
  from wave  9: upgrade the tower at (17,11); lay a SlowField on the road at (15,9); upgrade the tower at (13,10)
  from wave 10: upgrade the tower at (34,10)
  from wave 11: upgrade the tower at (16,11)
  from wave 12: lay a Mine on the road at (14,9); repair the gates when they would fall or it is cheaper than a gun
  from wave 13: build Cannon at (17,7); upgrade the tower at (17,10)
  from wave 14: build Cannon at (17,10)
  from wave 16: build Gun Turret at (14,7)
  from wave 18: upgrade the tower at (14,7); lay a Mine on the road at (14,9)
```


## Crysium Vein (Level_C1_06) on Psycho

Best spots per tower: Gun Turret (12,10) (12,11) (13,10); Cannon (17,11) (17,7) (17,10); Laser Turret (13,10) (11,10) (12,10); Flak Turret (36,10) (36,7) (36,11).

No plan holds on paper; the best found (falls w12, margin 0.19):

**Level_C1_06_3_best** (falls w12, margin 0.19, 34 coins left, 82 coins on gate repairs)

```
  from wave  1: build Cannon at (18,11); build Laser Turret at (15,10)
  from wave  2: build Flak Turret at (34,10)
  from wave  4: upgrade the tower at (15,10)
  from wave  5: repair the gates when they would fall or it is cheaper than a gun
  from wave  6: build Laser Turret at (13,10)
  from wave  8: build Laser Turret at (13,11); upgrade the tower at (12,10)
  from wave  9: build Cannon at (12,10)
  from wave 10: build Cannon at (17,10)
  from wave 16: upgrade the tower at (15,10)
  from wave 18: upgrade the tower at (13,11); upgrade the tower at (13,11)
```

