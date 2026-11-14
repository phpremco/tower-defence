# Winning plans per level and difficulty

Written from the stage-B replay (`Tools/PlayTest/plan_records.txt`, `bash Tools/PlayTest/balance_pipeline.sh readme`) on 2026-11-14 00:50; calibration 2026-11-14 00:49 (factors of 2026-11-10 03:33 kept), seeds 1,2,3, step 0.2, inputs 22954ca8 (current).

A plan counts as a winner only when it won in play on most seeds (the paper verdict is a pre-screen). Each plan is a priority list: a step is
taken from its wave on, as soon as the money is there (an earlier step blocks the later ones). Cells are (x,y) on the level grid. The obvious
family's results are in `Tools/PlayTest/yardstick.txt`. A cell without a winner shows how far the best replayed plan got: a design question.

## Landing Site (Level_C1_01) on Easy

Best spots per tower: Gun Turret (9,3) (9,6) (10,3); Cannon (11,3) (11,6) (12,3).

**Level_C1_01_0_strategy4** (won 3/3 seeds, 3.0 stars, base 600 left)

```
  Towers per threat: swarm: Cannon; pack: Gun Turret; plated: Gun Turret; air: Gun Turret; boss: whatever the ledger rates best.
  Gun Turret spots, best first: (9,3) (9,6) (10,3).
  Cannon spots, best first: (31,14) (11,6) (12,3).
  Upgrades: 1 guns down first, then an upgrade is worth 0.85x its value per coin against a new gun.
  Mines: before every boss wave, never for crowds.
  Slow field: never.
  Harvester: in the first 40 % of the level, once the guns hold the waves at hand with cover 1.20.
  Repair drone: never.
  Attack drone: never.
  Gate upgrades: the defence gate's gun and health, weighed at 0.80x against a tower step.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: never.
  The build order seed 2 took (3 stars, base 600):
  from wave  1: build Cannon at (31,14); build Gun Turret at (9,3)
  from wave  3: upgrade the tower at (9,3)
  from wave  5: build Gun Turret at (9,6)
  from wave  6: upgrade the tower at (9,6)
  from wave  8: build Gun Turret at (10,3)
  from wave  9: upgrade the defence gate
  from wave 11: upgrade the tower at (9,6); upgrade the tower at (10,3)
  from wave 12: build Gun Turret at (10,6); upgrade the tower at (10,6)
```

**Level_C1_01_0_strategy3** (won 2/3 seeds, 3.0 stars, base 600 left)

```
  Towers per threat: swarm: whatever the ledger rates best; pack: whatever the ledger rates best; plated: whatever the ledger rates best; air: whatever the ledger rates best; boss: whatever the ledger rates best.
  Gun Turret spots, best first: (9,3) (5,14) (10,3).
  Cannon spots, best first: (12,7) (5,14) (12,3).
  Upgrades: 2 guns down first, then an upgrade is worth 0.66x its value per coin against a new gun.
  Mines: before every boss wave, never for crowds.
  Slow field: when the guns would not hold the wave (cover under 1.00) or the slow pays by the damage rule.
  Harvester: in the first 60 % of the level, once the guns hold the waves at hand with cover 1.20.
  Repair drone: once a gun or gate has lost 5 % of its health.
  Attack drone: once 6 guns stand.
  Gate upgrades: never.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: never.
  The build order seed 2 took (3 stars, base 600):
  from wave  1: build Cannon at (12,7); build Gun Turret at (9,3)
  from wave  3: upgrade the tower at (9,3)
  from wave  6: upgrade the tower at (9,3)
  from wave  7: build Gun Turret at (5,14)
  from wave  9: build Cannon at (12,3)
  from wave 11: build Gun Turret at (10,3); upgrade the tower at (10,3); build Gun Turret at (10,6)
  from wave 12: upgrade the tower at (10,6); upgrade the tower at (10,6)
```


## Landing Site (Level_C1_01) on Medium

Best spots per tower: Gun Turret (9,3) (9,6) (10,3); Cannon (11,3) (11,6) (12,3).

**Level_C1_01_1_strategy4** (won 2/3 seeds, 3.0 stars, base 600 left)

```
  Towers per threat: swarm: whatever the ledger rates best; pack: whatever the ledger rates best; plated: Gun Turret; air: Cannon; boss: Gun Turret.
  Gun Turret spots, best first: (9,3) (9,6) (10,3).
  Cannon spots, best first: (11,3) (5,14) (11,6).
  Upgrades: 3 guns down first, then an upgrade is worth 0.87x its value per coin against a new gun.
  Mines: before every boss wave, and before a crowd wave when the guns would not hold it (cover under 1.30).
  Slow field: when the guns would not hold the wave (cover under 1.20) or the slow pays by the damage rule.
  Harvester: in the first 40 % of the level, once the guns hold the waves at hand with cover 1.20.
  Repair drone: once a gun or gate has lost 5 % of its health.
  Attack drone: never.
  Gate upgrades: never.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: never.
  The build order seed 1 took (3 stars, base 600):
  from wave  1: build Cannon at (11,3); build Gun Turret at (9,3); build Gun Turret at (9,6)
  from wave  3: upgrade the tower at (9,6)
  from wave  6: upgrade the tower at (11,3)
  from wave  8: upgrade the tower at (9,3); build Gun Turret at (10,3)
  from wave  9: upgrade the tower at (10,3); build Gun Turret at (10,6)
  from wave 10: upgrade the tower at (10,6)
  from wave 11: build Gun Turret at (11,6); upgrade the tower at (11,6)
  from wave 12: upgrade the tower at (11,6)
```

**Level_C1_01_1_strategy2** (won 2/3 seeds, 3.0 stars, base 600 left)

```
  Towers per threat: swarm: whatever the ledger rates best; pack: Cannon; plated: Gun Turret; air: whatever the ledger rates best; boss: Cannon.
  Gun Turret spots, best first: (11,3) (9,3) (5,14).
  Cannon spots, best first: (11,3) (12,7) (12,3).
  Upgrades: 2 guns down first, then an upgrade is worth 0.64x its value per coin against a new gun.
  Mines: before every boss wave, never for crowds.
  Slow field: when the guns would not hold the wave (cover under 1.00) or the slow pays by the damage rule.
  Harvester: never.
  Repair drone: once a gun or gate has lost 25 % of its health.
  Attack drone: once 12 guns stand.
  Gate upgrades: the defence gate's gun and health, weighed at 1.20x against a tower step.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: never.
  The build order seed 2 took (3 stars, base 600):
  from wave  1: build Cannon at (11,3); build Gun Turret at (9,3); upgrade the tower at (9,3)
  from wave  5: upgrade the defence gate
  from wave  8: upgrade the tower at (9,3); build Gun Turret at (5,14)
  from wave  9: build Cannon at (12,7)
  from wave 10: build Cannon at (12,3)
  from wave 11: build Gun Turret at (10,3); upgrade the tower at (10,3)
  from wave 12: build Gun Turret at (10,6)
```


## Landing Site (Level_C1_01) on Hard

Best spots per tower: Gun Turret (9,3) (9,6) (10,3); Cannon (11,3) (11,6) (12,3); Laser Turret (8,6) (9,6) (10,6).

**Level_C1_01_2_strategy4** (won 2/3 seeds, 2.5 stars, base 644 left)

```
  Towers per threat: swarm: Laser Turret; pack: whatever the ledger rates best; plated: Gun Turret; air: whatever the ledger rates best; boss: whatever the ledger rates best.
  Gun Turret spots, best first: (9,3) (9,6) (10,3).
  Cannon spots, best first: (11,6) (11,3) (5,14).
  Laser Turret spots, best first: (8,6) (10,6) (9,6).
  Upgrades: 2 guns down first, then an upgrade is worth 0.84x its value per coin against a new gun.
  Mines: before every boss wave, and before a crowd wave when the guns would not hold it (cover under 1.00).
  Slow field: when the guns would not hold the wave (cover under 1.20) or the slow pays by the damage rule.
  Harvester: in the first 30 % of the level, once the guns hold the waves at hand with cover 1.20.
  Repair drone: once a gun or gate has lost 50 % of its health.
  Attack drone: once 12 guns stand.
  Gate upgrades: never.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: never.
  The build order seed 1 took (3 stars, base 700):
  from wave  1: build Cannon at (11,6); build Cannon at (11,3)
  from wave  3: build Gun Turret at (9,3)
  from wave  4: upgrade the tower at (9,3)
  from wave  5: build Gun Turret at (9,6)
  from wave  7: upgrade the tower at (9,3); upgrade the tower at (9,6)
  from wave  8: upgrade the tower at (9,6)
  from wave  9: build Gun Turret at (10,3); upgrade the tower at (11,6); upgrade the tower at (11,3)
  from wave 10: upgrade the tower at (11,6)
  from wave 11: build Laser Turret at (8,6); upgrade the tower at (10,3); build Gun Turret at (10,6); upgrade the tower at (10,6)
  from wave 12: upgrade the tower at (8,6); upgrade the tower at (10,6); upgrade the tower at (10,3)
```


## Landing Site (Level_C1_01) on Psycho

Best spots per tower: Gun Turret (9,3) (9,6) (10,3); Cannon (11,3) (11,6) (12,3); Laser Turret (8,6) (9,6) (10,6).

**Level_C1_01_3_strategy4** (won 2/3 seeds, 2.5 stars, base 664 left)

```
  Towers per threat: swarm: whatever the ledger rates best; pack: Cannon; plated: whatever the ledger rates best; air: whatever the ledger rates best; boss: whatever the ledger rates best.
  Gun Turret spots, best first: (9,3) (10,3) (11,3).
  Cannon spots, best first: (12,3) (11,6) (11,3).
  Laser Turret spots, best first: (8,6) (9,6) (30,14).
  Upgrades: 1 guns down first, then an upgrade is worth 0.88x its value per coin against a new gun.
  Mines: before every boss wave, and before a crowd wave when the guns would not hold it (cover under 1.00).
  Slow field: when the guns would not hold the wave (cover under 1.20) or the slow pays by the damage rule.
  Harvester: in the first 60 % of the level, once the guns hold the waves at hand with cover 1.00.
  Repair drone: once a gun or gate has lost 25 % of its health.
  Attack drone: once 8 guns stand.
  Gate upgrades: never.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: the weakest ground gun may go for a sky gun the preview asks for (Psycho).
  The build order seed 2 took (3 stars, base 700):
  from wave  1: build Cannon at (12,3); build Cannon at (11,6); build Cannon at (11,3); build Cannon at (12,7); build Cannon at (12,6); upgrade the tower at (11,6)
  from wave  2: build Gun Turret at (9,3)
  from wave  4: upgrade the tower at (9,3); build Gun Turret at (10,3)
  from wave  5: upgrade the tower at (10,3)
  from wave  6: upgrade the tower at (12,6)
  from wave  7: upgrade the tower at (10,3); upgrade the tower at (9,3)
  from wave  8: upgrade the tower at (11,3)
  from wave  9: build Gun Turret at (5,14); upgrade the tower at (5,14); build Gun Turret at (9,6)
  from wave 10: upgrade the tower at (9,6); build Gun Turret at (10,6); upgrade the tower at (10,6); build Gun Turret at (31,14)
  from wave 11: upgrade the tower at (31,14); build Gun Turret at (19,14); upgrade the tower at (19,14)
  from wave 12: upgrade the tower at (19,14); upgrade the tower at (11,6); upgrade the tower at (12,7); upgrade the tower at (10,6); upgrade the tower at (12,6); upgrade the tower at (9,6)
```


## Glass Meadows (Level_C1_02) on Easy

Best spots per tower: Gun Turret (1,15) (1,14) (4,15); Cannon (1,15) (4,15) (1,14); Laser Turret (0,15) (1,14) (1,15).

**Level_C1_02_0_strategy5** (won 3/3 seeds, 3.0 stars, base 700 left)

```
  Towers per threat: swarm: Laser Turret; pack: whatever the ledger rates best; plated: whatever the ledger rates best; air: whatever the ledger rates best; boss: Laser Turret.
  Gun Turret spots, best first: (4,15) (1,15) (1,14).
  Cannon spots, best first: (4,15) (1,15) (1,14).
  Laser Turret spots, best first: (0,15) (1,14) (1,15).
  Upgrades: 3 guns down first, then an upgrade is worth 1.10x its value per coin against a new gun.
  Mines: before every boss wave, and before a crowd wave when the guns would not hold it (cover under 1.00).
  Slow field: when the guns would not hold the wave (cover under 1.00) or the slow pays by the damage rule.
  Harvester: in the first 40 % of the level, once the guns hold the waves at hand with cover 1.50.
  Repair drone: never.
  Attack drone: once 6 guns stand.
  Gate upgrades: never.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: never.
  The build order seed 1 took (3 stars, base 700):
  from wave  1: build Cannon at (4,15); build Gun Turret at (1,15)
  from wave  2: build Gun Turret at (1,14)
  from wave  4: upgrade the tower at (1,15)
  from wave  6: upgrade the tower at (1,15)
  from wave  8: build Cannon at (1,13)
  from wave 10: build Cannon at (4,13)
  from wave 11: upgrade the tower at (1,14); upgrade the tower at (1,14)
  from wave 12: build Gun Turret at (4,14); upgrade the tower at (4,14); upgrade the tower at (4,14)
```

**Level_C1_02_0_strategy1** (won 3/3 seeds, 3.0 stars, base 700 left)

```
  Towers per threat: swarm: whatever the ledger rates best; pack: Laser Turret; plated: Laser Turret; air: Laser Turret; boss: Gun Turret.
  Gun Turret spots, best first: (4,15) (1,14) (1,15).
  Cannon spots, best first: (1,15) (4,15) (1,14).
  Laser Turret spots, best first: (0,15) (1,14) (1,15).
  Upgrades: 3 guns down first, then an upgrade is worth 1.02x its value per coin against a new gun.
  Mines: before every boss wave, and before a crowd wave when the guns would not hold it (cover under 1.00).
  Slow field: never.
  Harvester: in the first 50 % of the level, once the guns hold the waves at hand with cover 1.00.
  Repair drone: never.
  Attack drone: once 6 guns stand.
  Gate upgrades: never.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: never.
  The build order seed 3 took (3 stars, base 700):
  from wave  1: build Cannon at (1,15); build Gun Turret at (4,15)
  from wave  2: build Gun Turret at (1,14)
  from wave  4: upgrade the tower at (4,15)
  from wave  6: upgrade the tower at (1,14)
  from wave  8: upgrade the tower at (4,15)
  from wave  9: build Cannon at (1,13)
  from wave 11: upgrade the tower at (1,14); build Gun Turret at (4,13); upgrade the tower at (4,13)
  from wave 12: upgrade the tower at (4,13); build Gun Turret at (4,14)
```


## Glass Meadows (Level_C1_02) on Medium

Best spots per tower: Gun Turret (1,15) (1,14) (4,15); Cannon (1,15) (4,15) (1,14); Laser Turret (0,15) (1,14) (1,15).

**Level_C1_02_1_strategy3** (won 3/3 seeds, 3.0 stars, base 700 left, repair drone)

```
  Towers per threat: swarm: Laser Turret; pack: whatever the ledger rates best; plated: Gun Turret; air: Gun Turret; boss: whatever the ledger rates best.
  Gun Turret spots, best first: (1,15) (1,14) (4,15).
  Cannon spots, best first: (1,13) (1,15) (4,15).
  Laser Turret spots, best first: (0,15) (1,14) (1,15).
  Upgrades: 4 guns down first, then an upgrade is worth 1.56x its value per coin against a new gun.
  Mines: before every boss wave, and before a crowd wave when the guns would not hold it (cover under 1.30).
  Slow field: when the guns would not hold the wave (cover under 1.00) or the slow pays by the damage rule.
  Harvester: in the first 50 % of the level, once the guns hold the waves at hand with cover 1.50.
  Repair drone: once a gun or gate has lost 5 % of its health.
  Attack drone: once 12 guns stand.
  Gate upgrades: the defence gate's gun and health, weighed at 0.80x against a tower step.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: never.
  The build order seed 3 took (3 stars, base 700):
  from wave  1: build Cannon at (1,13); build Cannon at (1,15)
  from wave  2: build Gun Turret at (1,14)
  from wave  4: build Gun Turret at (4,15)
  from wave  5: upgrade the tower at (4,15)
  from wave  8: upgrade the defence gate
  from wave  9: upgrade the tower at (4,15); upgrade the tower at (1,14); build Gun Turret at (4,13)
  from wave 10: upgrade the tower at (4,13)
  from wave 11: build Gun Turret at (4,14); upgrade the tower at (4,14)
  from wave 12: buy the RepairDrone; upgrade the tower at (4,13)
```

**Level_C1_02_1_strategy5** (won 3/3 seeds, 3.0 stars, base 700 left, repair drone)

```
  Towers per threat: swarm: whatever the ledger rates best; pack: whatever the ledger rates best; plated: Gun Turret; air: whatever the ledger rates best; boss: whatever the ledger rates best.
  Gun Turret spots, best first: (1,15) (1,14) (4,15).
  Cannon spots, best first: (1,15) (4,13) (4,15).
  Laser Turret spots, best first: (0,15) (1,14) (1,15).
  Upgrades: 4 guns down first, then an upgrade is worth 0.83x its value per coin against a new gun.
  Mines: not for bosses, and before a crowd wave when the guns would not hold it (cover under 1.30).
  Slow field: when the guns would not hold the wave (cover under 1.00) or the slow pays by the damage rule.
  Harvester: in the first 30 % of the level, once the guns hold the waves at hand with cover 1.20.
  Repair drone: once a gun or gate has lost 10 % of its health.
  Attack drone: once 8 guns stand.
  Gate upgrades: the defence gate's gun and health, weighed at 0.80x against a tower step.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: never.
  The build order seed 3 took (3 stars, base 700):
  from wave  1: build Cannon at (1,15); build Cannon at (4,13)
  from wave  2: build Gun Turret at (1,14)
  from wave  4: build Gun Turret at (4,15)
  from wave  5: upgrade the tower at (4,15)
  from wave  8: upgrade the defence gate
  from wave  9: upgrade the tower at (4,15); upgrade the tower at (1,14); build Gun Turret at (1,13)
  from wave 10: upgrade the tower at (1,13)
  from wave 11: build Gun Turret at (4,14); upgrade the tower at (4,14)
  from wave 12: buy the RepairDrone; upgrade the tower at (4,14)
```


## Glass Meadows (Level_C1_02) on Hard

Best spots per tower: Gun Turret (1,15) (1,14) (4,15); Cannon (1,15) (4,15) (1,14); Laser Turret (0,15) (1,14) (1,15); Flak Turret (32,16) (31,16) (32,19).

**Level_C1_02_2_strategy3** (won 3/3 seeds, 3.0 stars, base 700 left, repair drone)

```
  Towers per threat: swarm: Cannon; pack: whatever the ledger rates best; plated: whatever the ledger rates best; air: Cannon; boss: Laser Turret.
  Gun Turret spots, best first: (1,15) (1,14) (4,15).
  Cannon spots, best first: (1,14) (4,15) (4,14).
  Laser Turret spots, best first: (0,15) (1,14) (1,15).
  Flak Turret spots, best first: (31,16) (32,16) (32,19).
  Upgrades: 3 guns down first, then an upgrade is worth 1.75x its value per coin against a new gun.
  Mines: not for bosses, and before a crowd wave when the guns would not hold it (cover under 1.00).
  Slow field: when the guns would not hold the wave (cover under 1.00) or the slow pays by the damage rule.
  Harvester: in the first 40 % of the level, once the guns hold the waves at hand with cover 1.00.
  Repair drone: once a gun or gate has lost 25 % of its health.
  Attack drone: once 12 guns stand.
  Gate upgrades: never.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: never.
  The build order seed 1 took (3 stars, base 700):
  from wave  1: build Cannon at (1,14); build Cannon at (4,15)
  from wave  2: build Gun Turret at (1,15)
  from wave  4: upgrade the tower at (1,15)
  from wave  5: upgrade the tower at (4,15)
  from wave  8: upgrade the tower at (1,15); upgrade the tower at (1,14)
  from wave  9: build Cannon at (4,14); upgrade the tower at (4,14)
  from wave 10: build Cannon at (1,13); upgrade the tower at (1,13)
  from wave 11: buy the RepairDrone
  from wave 12: upgrade the tower at (4,15)
```

**Level_C1_02_2_strategy1** (won 3/3 seeds, 3.0 stars, base 700 left, repair drone)

```
  Towers per threat: swarm: Cannon; pack: whatever the ledger rates best; plated: whatever the ledger rates best; air: whatever the ledger rates best; boss: Gun Turret.
  Gun Turret spots, best first: (4,15) (1,15) (1,13).
  Cannon spots, best first: (4,15) (1,14) (1,13).
  Laser Turret spots, best first: (1,14) (4,15) (1,15).
  Flak Turret spots, best first: (31,16) (33,15) (33,16).
  Upgrades: 2 guns down first, then an upgrade is worth 0.78x its value per coin against a new gun.
  Mines: before every boss wave, and before a crowd wave when the guns would not hold it (cover under 1.00).
  Slow field: never.
  Harvester: in the first 60 % of the level, once the guns hold the waves at hand with cover 1.20.
  Repair drone: once a gun or gate has lost 10 % of its health.
  Attack drone: once 6 guns stand.
  Gate upgrades: never.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: never.
  The build order seed 1 took (3 stars, base 700):
  from wave  1: build Cannon at (4,15); build Cannon at (1,14)
  from wave  2: build Gun Turret at (1,15)
  from wave  4: upgrade the tower at (1,15)
  from wave  5: upgrade the tower at (4,15)
  from wave  8: upgrade the tower at (1,15); build Gun Turret at (1,13); upgrade the tower at (1,13); upgrade the tower at (1,13)
  from wave  9: build Cannon at (4,14); buy the RepairDrone
  from wave 10: upgrade the tower at (4,14)
  from wave 11: upgrade the tower at (1,14); build Cannon at (4,13)
  from wave 12: upgrade the tower at (4,13)
```


## Glass Meadows (Level_C1_02) on Psycho

Best spots per tower: Gun Turret (1,15) (1,14) (4,15); Cannon (1,15) (4,15) (1,14); Laser Turret (0,15) (1,14) (1,15); Flak Turret (32,16) (31,16) (32,19).

**Level_C1_02_3_strategy2** (won 3/3 seeds, 3.0 stars, base 800 left, repair drone)

```
  Towers per threat: swarm: Cannon; pack: Gun Turret; plated: whatever the ledger rates best; air: whatever the ledger rates best; boss: whatever the ledger rates best.
  Gun Turret spots, best first: (1,15) (1,14) (4,15).
  Cannon spots, best first: (1,15) (4,15) (1,14).
  Laser Turret spots, best first: (0,15) (1,14) (1,15).
  Flak Turret spots, best first: (32,19) (32,16) (31,16).
  Upgrades: 1 guns down first, then an upgrade is worth 1.31x its value per coin against a new gun.
  Mines: before every boss wave, never for crowds.
  Slow field: when the guns would not hold the wave (cover under 1.20) or the slow pays by the damage rule.
  Harvester: never.
  Repair drone: once a gun or gate has lost 5 % of its health.
  Attack drone: once 8 guns stand.
  Gate upgrades: never.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: the weakest ground gun may go for a sky gun the preview asks for (Psycho).
  The build order seed 1 took (3 stars, base 800):
  from wave  1: build Cannon at (1,15); upgrade the tower at (1,15); build Cannon at (4,15); upgrade the tower at (4,15); build Cannon at (1,14); upgrade the tower at (1,14); build Gun Turret at (1,13); upgrade the tower at (1,13)
  from wave  3: upgrade the tower at (1,13)
  from wave  5: build Gun Turret at (4,13); upgrade the tower at (4,13)
  from wave  6: upgrade the tower at (4,15)
  from wave  7: build Cannon at (4,14)
  from wave  8: upgrade the tower at (4,13); upgrade the tower at (4,14)
  from wave  9: buy the RepairDrone; upgrade the tower at (4,14)
  from wave 10: upgrade the tower at (1,15)
  from wave 11: upgrade the tower at (1,14)
```


## Shard Ridge (Level_C1_03) on Easy

Best spots per tower: Gun Turret (12,11) (13,11) (11,11); Cannon (12,10) (12,11) (13,10); Laser Turret (11,10) (12,10) (13,10); Flak Turret (32,10) (33,10) (34,7).

**Level_C1_03_0_strategy5** (won 3/3 seeds, 3.0 stars, base 700 left, repair drone)

```
  Towers per threat: swarm: Laser Turret; pack: whatever the ledger rates best; plated: whatever the ledger rates best; air: Gun Turret; boss: whatever the ledger rates best.
  Gun Turret spots, best first: (12,11) (13,11) (13,10).
  Cannon spots, best first: (12,11) (12,10) (13,10).
  Laser Turret spots, best first: (11,10) (12,10) (13,10).
  Flak Turret spots, best first: (32,10) (33,10) (34,7).
  Upgrades: 2 guns down first, then an upgrade is worth 0.99x its value per coin against a new gun.
  Mines: not for bosses, and before a crowd wave when the guns would not hold it (cover under 1.00).
  Slow field: when the guns would not hold the wave (cover under 1.00) or the slow pays by the damage rule.
  Harvester: in the first 50 % of the level, once the guns hold the waves at hand with cover 1.20.
  Repair drone: once a gun or gate has lost 5 % of its health.
  Attack drone: never.
  Gate upgrades: the defence gate's gun and health, weighed at 0.80x against a tower step.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: never.
  The build order seed 3 took (3 stars, base 700):
  from wave  1: build Cannon at (12,11); build Gun Turret at (13,11)
  from wave  2: build Gun Turret at (13,10)
  from wave  8: buy the HarvestDrone
  from wave 10: build Flak Turret at (32,10)
  from wave 11: buy the RepairDrone
  from wave 12: upgrade the defence gate
  from wave 13: upgrade the tower at (13,10); upgrade the tower at (13,11)
  from wave 14: upgrade the tower at (32,10)
  from wave 15: build Gun Turret at (11,11); build Gun Turret at (14,11); build Gun Turret at (34,11)
  from wave 16: build Gun Turret at (12,10); build Laser Turret at (11,10)
```

**Level_C1_03_0_strategy3** (won 3/3 seeds, 3.0 stars, base 700 left, repair drone)

```
  Towers per threat: swarm: Laser Turret; pack: Laser Turret; plated: Cannon; air: Cannon; boss: whatever the ledger rates best.
  Gun Turret spots, best first: (11,11) (12,11) (13,11).
  Cannon spots, best first: (12,10) (12,11) (11,11).
  Laser Turret spots, best first: (12,10) (11,10) (13,10).
  Flak Turret spots, best first: (32,10) (33,10) (35,7).
  Upgrades: 4 guns down first, then an upgrade is worth 1.08x its value per coin against a new gun.
  Mines: before every boss wave, and before a crowd wave when the guns would not hold it (cover under 1.30).
  Slow field: when the guns would not hold the wave (cover under 1.20) or the slow pays by the damage rule.
  Harvester: in the first 30 % of the level, once the guns hold the waves at hand with cover 1.50.
  Repair drone: once a gun or gate has lost 5 % of its health.
  Attack drone: once 12 guns stand.
  Gate upgrades: never.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: never.
  The build order seed 1 took (3 stars, base 700):
  from wave  1: build Cannon at (12,10); build Gun Turret at (11,11)
  from wave  2: build Gun Turret at (12,11)
  from wave  8: buy the HarvestDrone
  from wave 10: build Flak Turret at (32,10)
  from wave 11: buy the RepairDrone; upgrade the tower at (12,11)
  from wave 12: upgrade the tower at (11,11)
  from wave 13: build Laser Turret at (11,10)
  from wave 14: upgrade the tower at (32,10)
  from wave 15: build Gun Turret at (13,11); build Gun Turret at (34,11)
  from wave 16: build Laser Turret at (13,10); build Laser Turret at (10,10)
```


## Shard Ridge (Level_C1_03) on Medium

Best spots per tower: Gun Turret (12,11) (13,11) (11,11); Cannon (12,10) (12,11) (13,10); Laser Turret (11,10) (12,10) (13,10); Flak Turret (32,10) (33,10) (34,7).

**Level_C1_03_1_strategy1** (won 3/3 seeds, 3.0 stars, base 700 left, repair drone)

```
  Towers per threat: swarm: Laser Turret; pack: Cannon; plated: Cannon; air: Cannon; boss: whatever the ledger rates best.
  Gun Turret spots, best first: (12,11) (13,11) (11,11).
  Cannon spots, best first: (12,11) (12,10) (11,10).
  Laser Turret spots, best first: (11,10) (12,10) (13,10).
  Flak Turret spots, best first: (32,10) (33,10) (34,7).
  Upgrades: 1 guns down first, then an upgrade is worth 0.76x its value per coin against a new gun.
  Mines: before every boss wave, and before a crowd wave when the guns would not hold it (cover under 1.30).
  Slow field: when the guns would not hold the wave (cover under 1.00) or the slow pays by the damage rule.
  Harvester: never.
  Repair drone: once a gun or gate has lost 25 % of its health.
  Attack drone: once 6 guns stand.
  Gate upgrades: the defence gate's gun and health, weighed at 0.80x against a tower step.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: never.
  The build order seed 2 took (3 stars, base 700):
  from wave  1: build Cannon at (12,11); upgrade the defence gate; build Gun Turret at (13,11)
  from wave  4: upgrade the tower at (13,11)
  from wave  7: build Cannon at (12,10)
  from wave 10: build Flak Turret at (32,10)
  from wave 11: build Flak Turret at (33,10)
  from wave 13: buy the RepairDrone; upgrade the tower at (13,11)
  from wave 14: upgrade the tower at (32,10)
  from wave 15: build Cannon at (11,10)
  from wave 16: build Cannon at (13,10); build Cannon at (33,11); build Gun Turret at (11,11)
```

**Level_C1_03_1_strategy5** (won 3/3 seeds, 3.0 stars, base 700 left, repair drone)

```
  Towers per threat: swarm: Cannon; pack: whatever the ledger rates best; plated: whatever the ledger rates best; air: Cannon; boss: Laser Turret.
  Gun Turret spots, best first: (13,11) (13,10) (12,11).
  Cannon spots, best first: (12,11) (12,10) (13,10).
  Laser Turret spots, best first: (11,10) (12,10) (13,10).
  Flak Turret spots, best first: (33,10) (32,10) (34,7).
  Upgrades: 2 guns down first, then an upgrade is worth 0.81x its value per coin against a new gun.
  Mines: not for bosses, and before a crowd wave when the guns would not hold it (cover under 1.00).
  Slow field: when the guns would not hold the wave (cover under 1.00) or the slow pays by the damage rule.
  Harvester: in the first 50 % of the level, once the guns hold the waves at hand with cover 1.20.
  Repair drone: once a gun or gate has lost 5 % of its health.
  Attack drone: never.
  Gate upgrades: the defence gate's gun and health, weighed at 1.20x against a tower step.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: never.
  The build order seed 3 took (3 stars, base 700):
  from wave  1: build Cannon at (12,11); build Gun Turret at (13,11); upgrade the defence gate
  from wave  4: upgrade the tower at (13,11)
  from wave  6: build Gun Turret at (13,10)
  from wave 10: build Flak Turret at (33,10); build Flak Turret at (32,10)
  from wave 13: buy the HarvestDrone
  from wave 14: buy the RepairDrone; upgrade the tower at (13,10); build Gun Turret at (14,11)
  from wave 15: build Flak Turret at (34,7); upgrade the tower at (14,11)
  from wave 16: build Gun Turret at (12,10); upgrade the tower at (12,10); build Cannon at (11,10); build Cannon at (33,11); build Cannon at (34,11)
```


## Shard Ridge (Level_C1_03) on Hard

Best spots per tower: Gun Turret (12,11) (13,11) (11,11); Cannon (12,10) (12,11) (13,10); Laser Turret (11,10) (12,10) (13,10); Flak Turret (32,10) (33,10) (34,7).

**Level_C1_03_2_strategy2** (won 3/3 seeds, 3.0 stars, base 689 left)

```
  Towers per threat: swarm: Cannon; pack: Cannon; plated: whatever the ledger rates best; air: whatever the ledger rates best; boss: Laser Turret.
  Gun Turret spots, best first: (12,11) (13,11) (11,11).
  Cannon spots, best first: (12,10) (12,11) (13,10).
  Laser Turret spots, best first: (11,10) (12,10) (13,10).
  Flak Turret spots, best first: (32,10) (34,7) (33,10).
  Upgrades: 4 guns down first, then an upgrade is worth 1.57x its value per coin against a new gun.
  Mines: before every boss wave, never for crowds.
  Slow field: when the guns would not hold the wave (cover under 1.00) or the slow pays by the damage rule.
  Harvester: in the first 30 % of the level, once the guns hold the waves at hand with cover 1.20.
  Repair drone: never.
  Attack drone: never.
  Gate upgrades: the defence gate's gun and health, weighed at 0.80x against a tower step.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: never.
  The build order seed 1 took (3 stars, base 700):
  from wave  1: build Cannon at (12,10); build Gun Turret at (12,11); build Gun Turret at (13,11)
  from wave  5: buy the HarvestDrone
  from wave  7: build Cannon at (13,10)
  from wave  8: upgrade the defence gate
  from wave 10: build Flak Turret at (32,10); upgrade the tower at (13,11)
  from wave 11: build Flak Turret at (34,7)
  from wave 12: build Flak Turret at (33,10)
  from wave 13: upgrade the tower at (12,11); build Gun Turret at (11,11); upgrade the tower at (11,11)
  from wave 14: build Gun Turret at (14,11); build Flak Turret at (34,11); upgrade the tower at (14,11)
  from wave 15: build Cannon at (11,10); upgrade the tower at (13,11); upgrade the tower at (14,11)
  from wave 16: build Cannon at (33,11); build Cannon at (32,12); build Cannon at (14,10); build Cannon at (14,12); build Cannon at (32,11)
```

**Level_C1_03_2_strategy1** (won 3/3 seeds, 2.3 stars, base 600 left, repair drone)

```
  Towers per threat: swarm: Cannon; pack: Cannon; plated: Laser Turret; air: Laser Turret; boss: Gun Turret.
  Gun Turret spots, best first: (12,11) (13,11) (11,11).
  Cannon spots, best first: (12,10) (12,11) (13,10).
  Laser Turret spots, best first: (13,10) (12,10) (10,10).
  Flak Turret spots, best first: (34,7) (32,10) (33,10).
  Upgrades: 2 guns down first, then an upgrade is worth 1.25x its value per coin against a new gun.
  Mines: before every boss wave, and before a crowd wave when the guns would not hold it (cover under 1.30).
  Slow field: when the guns would not hold the wave (cover under 1.00) or the slow pays by the damage rule.
  Harvester: never.
  Repair drone: once a gun or gate has lost 50 % of its health.
  Attack drone: once 6 guns stand.
  Gate upgrades: never.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: never.
  The build order seed 2 took (3 stars, base 633):
  from wave  1: build Cannon at (12,10); build Cannon at (12,11)
  from wave  2: build Gun Turret at (13,11)
  from wave  4: upgrade the tower at (13,11)
  from wave  5: upgrade the tower at (13,11)
  from wave  7: build Cannon at (13,10)
  from wave 10: build Flak Turret at (34,7)
  from wave 11: build Flak Turret at (32,10)
  from wave 13: build Cannon at (11,10); build Gun Turret at (11,11); build Flak Turret at (33,10); build Flak Turret at (34,10)
  from wave 15: buy the RepairDrone
  from wave 16: build Cannon at (33,11); build Cannon at (34,11); build Cannon at (32,12); build Cannon at (14,10); build Cannon at (14,11); build Cannon at (14,12)
```


## Shard Ridge (Level_C1_03) on Psycho

Best spots per tower: Gun Turret (12,11) (13,11) (11,11); Cannon (12,10) (12,11) (13,10); Laser Turret (11,10) (12,10) (13,10); Flak Turret (32,10) (33,10) (34,7).

**Level_C1_03_3_strategy1** (won 3/3 seeds, 3.0 stars, base 800 left, repair drone)

```
  Towers per threat: swarm: whatever the ledger rates best; pack: Cannon; plated: Gun Turret; air: whatever the ledger rates best; boss: whatever the ledger rates best.
  Gun Turret spots, best first: (12,11) (13,11) (11,11).
  Cannon spots, best first: (12,10) (13,10) (12,11).
  Laser Turret spots, best first: (13,10) (11,10) (12,10).
  Flak Turret spots, best first: (33,10) (32,10) (34,7).
  Upgrades: 3 guns down first, then an upgrade is worth 1.69x its value per coin against a new gun.
  Mines: before every boss wave, and before a crowd wave when the guns would not hold it (cover under 1.30).
  Slow field: when the guns would not hold the wave (cover under 1.00) or the slow pays by the damage rule.
  Harvester: in the first 50 % of the level, once the guns hold the waves at hand with cover 1.20.
  Repair drone: once a gun or gate has lost 10 % of its health.
  Attack drone: once 12 guns stand.
  Gate upgrades: never.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: the weakest ground gun may go for a sky gun the preview asks for (Psycho).
  The build order seed 1 took (3 stars, base 800):
  from wave  1: build Cannon at (12,10); build Cannon at (13,10); build Cannon at (12,11); buy the HarvestDrone; build Cannon at (33,11); build Cannon at (11,10)
  from wave  2: buy the RepairDrone
  from wave  3: build Gun Turret at (13,11)
  from wave  4: upgrade the tower at (13,11)
  from wave  5: build Gun Turret at (11,11)
  from wave  6: upgrade the tower at (13,11); upgrade the tower at (11,11)
  from wave  8: upgrade the tower at (13,10)
  from wave  9: sell the tower at (12,10); build Flak Turret at (33,10); build Gun Turret at (12,10); upgrade the tower at (12,10)
  from wave 10: sell the tower at (12,11); build Flak Turret at (32,10); build Gun Turret at (12,11); upgrade the tower at (12,11); sell the tower at (33,11); build Flak Turret at (34,7)
  from wave 11: sell the tower at (11,10); build Flak Turret at (34,10); upgrade the tower at (33,10)
  from wave 12: build Gun Turret at (14,11); upgrade the tower at (14,11); build Cannon at (33,11); upgrade the tower at (33,11)
  from wave 13: upgrade the tower at (33,10); upgrade the tower at (32,10); upgrade the tower at (12,10)
  from wave 14: upgrade the tower at (14,11); build Gun Turret at (21,11)
  from wave 15: upgrade the tower at (12,11); upgrade the tower at (21,11); build Gun Turret at (34,11)
  from wave 16: upgrade the tower at (34,11); build Gun Turret at (31,10); upgrade the tower at (31,10); build Gun Turret at (14,10); upgrade the tower at (31,10); upgrade the tower at (14,10); build Gun Turret at (14,12); upgrade the tower at (14,10); upgrade the tower at (14,12); build Gun Turret at (31,11); upgrade the tower at (31,11); build Gun Turret at (13,12)
```

**Level_C1_03_3_strategy2** (won 3/3 seeds, 2.7 stars, base 733 left, repair drone)

```
  Towers per threat: swarm: Cannon; pack: whatever the ledger rates best; plated: whatever the ledger rates best; air: whatever the ledger rates best; boss: whatever the ledger rates best.
  Gun Turret spots, best first: (12,11) (13,10) (13,11).
  Cannon spots, best first: (12,10) (11,10) (13,10).
  Laser Turret spots, best first: (11,10) (12,10) (13,10).
  Flak Turret spots, best first: (32,10) (33,10) (34,7).
  Upgrades: 1 guns down first, then an upgrade is worth 1.10x its value per coin against a new gun.
  Mines: before every boss wave, and before a crowd wave when the guns would not hold it (cover under 1.00).
  Slow field: never.
  Harvester: in the first 30 % of the level, once the guns hold the waves at hand with cover 1.20.
  Repair drone: once a gun or gate has lost 5 % of its health.
  Attack drone: once 6 guns stand.
  Gate upgrades: never.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: the weakest ground gun may go for a sky gun the preview asks for (Psycho).
  The build order seed 2 took (3 stars, base 800):
  from wave  1: build Cannon at (12,10); build Cannon at (11,10); build Cannon at (13,10); buy the HarvestDrone; build Cannon at (12,11); build Cannon at (33,11)
  from wave  2: buy the RepairDrone
  from wave  3: build Gun Turret at (13,11)
  from wave  4: upgrade the tower at (13,11)
  from wave  5: build Gun Turret at (11,11)
  from wave  6: upgrade the tower at (13,11); upgrade the tower at (11,11)
  from wave  8: upgrade the tower at (13,10)
  from wave  9: sell the tower at (12,10); build Flak Turret at (32,10); upgrade the tower at (11,11)
  from wave 10: sell the tower at (11,10); build Flak Turret at (33,10); build Gun Turret at (12,10); upgrade the tower at (12,10); sell the tower at (12,11); build Flak Turret at (34,7); build Gun Turret at (12,11)
  from wave 11: sell the tower at (12,11); build Flak Turret at (34,10)
  from wave 12: build Gun Turret at (12,11); upgrade the tower at (32,10); build Cannon at (11,10); upgrade the tower at (12,10)
  from wave 13: upgrade the tower at (32,10); upgrade the tower at (33,10); upgrade the tower at (12,11); build Gun Turret at (14,11)
  from wave 14: upgrade the tower at (14,11); build Gun Turret at (34,11); upgrade the tower at (34,11)
  from wave 15: build Gun Turret at (21,11); upgrade the tower at (21,11)
  from wave 16: build Gun Turret at (31,10); upgrade the tower at (31,10); build Gun Turret at (14,10); upgrade the tower at (14,10); build Gun Turret at (14,12); upgrade the tower at (14,12); upgrade the tower at (31,10); upgrade the tower at (14,10); build Gun Turret at (31,11); upgrade the tower at (31,11); build Gun Turret at (13,12); upgrade the tower at (13,12); upgrade the tower at (31,11)
```


## Fault Line (Level_C1_04) on Easy

Best spots per tower: Gun Turret (29,5) (29,6) (28,6); Cannon (29,6) (32,6) (29,5); Laser Turret (28,5) (29,5) (29,6); Flak Turret (34,9) (33,9) (34,6).

**Level_C1_04_0_strategy5** (won 3/3 seeds, 3.0 stars, base 700 left)

```
  Towers per threat: swarm: Cannon; pack: whatever the ledger rates best; plated: whatever the ledger rates best; air: whatever the ledger rates best; boss: whatever the ledger rates best.
  Gun Turret spots, best first: (28,6) (29,6) (29,5).
  Cannon spots, best first: (32,6) (29,5) (29,6).
  Laser Turret spots, best first: (29,5) (28,5) (29,6).
  Flak Turret spots, best first: (34,6) (33,9) (34,9).
  Upgrades: 1 guns down first, then an upgrade is worth 1.50x its value per coin against a new gun.
  Mines: not for bosses, and before a crowd wave when the guns would not hold it (cover under 1.00).
  Slow field: never.
  Harvester: never.
  Repair drone: never.
  Attack drone: once 8 guns stand.
  Gate upgrades: never.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: never.
  The build order seed 1 took (3 stars, base 700):
  from wave  1: build Cannon at (32,6)
  from wave  3: build Flak Turret at (34,6)
  from wave  6: build Gun Turret at (28,6)
  from wave  7: upgrade the tower at (28,6)
  from wave  9: build Gun Turret at (29,6)
  from wave 12: build Flak Turret at (33,9); upgrade the tower at (29,6)
  from wave 14: upgrade the tower at (29,6)
  from wave 15: upgrade the tower at (34,6)
  from wave 16: upgrade the tower at (33,9); upgrade the tower at (28,6); build Cannon at (29,5)
```

**Level_C1_04_0_strategy3** (won 3/3 seeds, 3.0 stars, base 700 left, repair drone)

```
  Towers per threat: swarm: whatever the ledger rates best; pack: whatever the ledger rates best; plated: whatever the ledger rates best; air: whatever the ledger rates best; boss: Cannon.
  Gun Turret spots, best first: (29,6) (29,5) (28,6).
  Cannon spots, best first: (32,6) (32,5) (29,5).
  Laser Turret spots, best first: (29,5) (28,5) (32,5).
  Flak Turret spots, best first: (33,9) (34,9) (34,10).
  Upgrades: 2 guns down first, then an upgrade is worth 1.52x its value per coin against a new gun.
  Mines: not for bosses, never for crowds.
  Slow field: when the guns would not hold the wave (cover under 1.00) or the slow pays by the damage rule.
  Harvester: in the first 30 % of the level, once the guns hold the waves at hand with cover 1.20.
  Repair drone: once a gun or gate has lost 50 % of its health.
  Attack drone: once 12 guns stand.
  Gate upgrades: never.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: never.
  The build order seed 2 took (3 stars, base 700):
  from wave  1: build Cannon at (32,6)
  from wave  3: build Flak Turret at (33,9)
  from wave  6: build Gun Turret at (29,6)
  from wave 11: buy the HarvestDrone; upgrade the tower at (29,6)
  from wave 12: upgrade the tower at (33,9)
  from wave 13: build Gun Turret at (29,5)
  from wave 14: build Flak Turret at (34,9)
  from wave 16: buy the RepairDrone; upgrade the tower at (29,5); upgrade the tower at (29,5); upgrade the tower at (34,9); upgrade the tower at (29,6); build Cannon at (32,5)
```


## Fault Line (Level_C1_04) on Medium

Best spots per tower: Gun Turret (29,5) (29,6) (28,6); Cannon (29,6) (32,6) (29,5); Laser Turret (28,5) (29,5) (29,6); Flak Turret (34,9) (33,9) (34,6).

**Level_C1_04_1_strategy2** (won 3/3 seeds, 3.0 stars, base 682 left)

```
  Towers per threat: swarm: whatever the ledger rates best; pack: whatever the ledger rates best; plated: Gun Turret; air: whatever the ledger rates best; boss: whatever the ledger rates best.
  Gun Turret spots, best first: (29,5) (29,6) (28,6).
  Cannon spots, best first: (34,10) (32,5) (32,6).
  Laser Turret spots, best first: (29,5) (28,5) (28,6).
  Flak Turret spots, best first: (34,9) (33,9) (34,6).
  Upgrades: 4 guns down first, then an upgrade is worth 1.36x its value per coin against a new gun.
  Mines: before every boss wave, and before a crowd wave when the guns would not hold it (cover under 1.30).
  Slow field: never.
  Harvester: in the first 30 % of the level, once the guns hold the waves at hand with cover 1.20.
  Repair drone: once a gun or gate has lost 50 % of its health.
  Attack drone: once 8 guns stand.
  Gate upgrades: never.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: never.
  The build order seed 1 took (3 stars, base 682):
  from wave  1: build Cannon at (34,10); build Flak Turret at (34,9)
  from wave  3: build Gun Turret at (29,5)
  from wave  9: build Gun Turret at (29,6); upgrade the tower at (29,5); upgrade the tower at (29,6)
  from wave 12: upgrade the tower at (34,9)
  from wave 13: upgrade the tower at (29,5); build Gun Turret at (28,6); upgrade the tower at (28,6)
  from wave 14: build Gun Turret at (32,5)
  from wave 15: upgrade the tower at (32,5)
  from wave 16: build Gun Turret at (32,6); build Flak Turret at (33,9); build Flak Turret at (34,6)
```

**Level_C1_04_1_strategy1** (won 3/3 seeds, 3.0 stars, base 678 left)

```
  Towers per threat: swarm: whatever the ledger rates best; pack: Gun Turret; plated: Cannon; air: whatever the ledger rates best; boss: Gun Turret.
  Gun Turret spots, best first: (34,6) (29,6) (32,6).
  Cannon spots, best first: (34,10) (32,6) (29,5).
  Laser Turret spots, best first: (28,5) (29,5) (28,6).
  Flak Turret spots, best first: (34,9) (33,9) (34,6).
  Upgrades: 3 guns down first, then an upgrade is worth 1.58x its value per coin against a new gun.
  Mines: not for bosses, and before a crowd wave when the guns would not hold it (cover under 1.30).
  Slow field: when the guns would not hold the wave (cover under 1.20) or the slow pays by the damage rule.
  Harvester: in the first 30 % of the level, once the guns hold the waves at hand with cover 1.50.
  Repair drone: once a gun or gate has lost 25 % of its health.
  Attack drone: once 8 guns stand.
  Gate upgrades: never.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: never.
  The build order seed 3 took (3 stars, base 688):
  from wave  1: build Cannon at (34,10); build Flak Turret at (34,9)
  from wave  3: build Gun Turret at (34,6)
  from wave  9: upgrade the tower at (34,6); build Gun Turret at (29,6); upgrade the tower at (29,6)
  from wave 12: upgrade the tower at (34,9)
  from wave 13: build Gun Turret at (32,6); upgrade the tower at (32,6); build Gun Turret at (28,6)
  from wave 14: upgrade the tower at (28,6); build Gun Turret at (32,5)
  from wave 15: upgrade the tower at (32,5)
  from wave 16: build Gun Turret at (30,9); build Flak Turret at (33,9); build Flak Turret at (31,9)
```


## Fault Line (Level_C1_04) on Hard

Best spots per tower: Gun Turret (29,5) (29,6) (28,6); Cannon (29,6) (32,6) (29,5); Laser Turret (28,5) (29,5) (29,6); Flak Turret (34,9) (33,9) (34,6).

**Level_C1_04_2_strategy1** (won 3/3 seeds, 3.0 stars, base 800 left, repair drone)

```
  Towers per threat: swarm: Cannon; pack: whatever the ledger rates best; plated: Gun Turret; air: Cannon; boss: whatever the ledger rates best.
  Gun Turret spots, best first: (29,5) (29,6) (32,6).
  Cannon spots, best first: (32,5) (32,6) (34,10).
  Laser Turret spots, best first: (28,5) (29,5) (29,6).
  Flak Turret spots, best first: (33,9) (34,9) (34,6).
  Upgrades: 2 guns down first, then an upgrade is worth 1.01x its value per coin against a new gun.
  Mines: before every boss wave, and before a crowd wave when the guns would not hold it (cover under 1.30).
  Slow field: when the guns would not hold the wave (cover under 1.20) or the slow pays by the damage rule.
  Harvester: in the first 60 % of the level, once the guns hold the waves at hand with cover 1.20.
  Repair drone: once a gun or gate has lost 50 % of its health.
  Attack drone: once 12 guns stand.
  Gate upgrades: never.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: never.
  The build order seed 3 took (3 stars, base 800):
  from wave  1: build Cannon at (32,5); build Flak Turret at (33,9)
  from wave  2: build Gun Turret at (29,5)
  from wave  7: buy the HarvestDrone; upgrade the tower at (33,9)
  from wave  8: upgrade the tower at (29,5)
  from wave  9: build Gun Turret at (29,6)
  from wave 10: buy the RepairDrone; upgrade the tower at (29,6)
  from wave 11: build Gun Turret at (32,6); upgrade the tower at (32,6)
  from wave 12: build Gun Turret at (36,6)
  from wave 13: upgrade the tower at (36,6); build Gun Turret at (28,6); upgrade the tower at (28,6); build Gun Turret at (34,6); build Flak Turret at (34,9)
  from wave 14: upgrade the tower at (34,6); build Gun Turret at (30,9); upgrade the tower at (30,9)
  from wave 15: build Gun Turret at (33,6); upgrade the tower at (33,6); build Gun Turret at (31,9); upgrade the tower at (31,9)
  from wave 16: buy the AttackDrone; build Cannon at (34,10); build Cannon at (32,9); build Cannon at (31,10); build Cannon at (32,10); build Cannon at (29,9); build Cannon at (33,5); build Cannon at (33,10)
```

**Level_C1_04_2_strategy3** (won 3/3 seeds, 3.0 stars, base 792 left)

```
  Towers per threat: swarm: Cannon; pack: Cannon; plated: Gun Turret; air: Cannon; boss: Laser Turret.
  Gun Turret spots, best first: (29,5) (29,6) (28,6).
  Cannon spots, best first: (32,6) (29,5) (29,6).
  Laser Turret spots, best first: (28,5) (29,5) (29,6).
  Flak Turret spots, best first: (33,9) (34,9) (34,6).
  Upgrades: 1 guns down first, then an upgrade is worth 0.96x its value per coin against a new gun.
  Mines: before every boss wave, never for crowds.
  Slow field: when the guns would not hold the wave (cover under 1.00) or the slow pays by the damage rule.
  Harvester: in the first 50 % of the level, once the guns hold the waves at hand with cover 1.00.
  Repair drone: never.
  Attack drone: once 6 guns stand.
  Gate upgrades: never.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: never.
  The build order seed 3 took (3 stars, base 800):
  from wave  1: build Cannon at (32,6); build Flak Turret at (33,9)
  from wave  2: build Gun Turret at (29,5)
  from wave  7: buy the HarvestDrone; upgrade the tower at (33,9)
  from wave  8: upgrade the tower at (29,5)
  from wave  9: build Gun Turret at (29,6); upgrade the tower at (29,6)
  from wave 10: build Cannon at (32,5)
  from wave 12: build Cannon at (28,6)
  from wave 13: build Flak Turret at (34,9); build Flak Turret at (34,6)
  from wave 14: upgrade the tower at (32,5); buy the AttackDrone
  from wave 15: build Gun Turret at (30,9); upgrade the tower at (30,9)
  from wave 16: build Gun Turret at (36,6); upgrade the tower at (36,6); build Cannon at (34,10); build Cannon at (33,6); build Cannon at (31,9); build Cannon at (32,9); build Cannon at (31,10); build Cannon at (32,10); build Cannon at (29,9); build Cannon at (33,5)
```


## Fault Line (Level_C1_04) on Psycho

Best spots per tower: Gun Turret (29,5) (29,6) (28,6); Cannon (29,6) (32,6) (29,5); Laser Turret (28,5) (29,5) (29,6); Flak Turret (34,9) (33,9) (34,6).

**Level_C1_04_3_strategy3** (won 3/3 seeds, 1.7 stars, base 446 left, repair drone)

```
  Towers per threat: swarm: whatever the ledger rates best; pack: Gun Turret; plated: whatever the ledger rates best; air: Gun Turret; boss: whatever the ledger rates best.
  Gun Turret spots, best first: (34,6) (29,5) (29,6).
  Cannon spots, best first: (29,6) (32,5) (28,6).
  Laser Turret spots, best first: (28,5) (29,6) (28,6).
  Flak Turret spots, best first: (33,9) (32,9) (34,6).
  Upgrades: 3 guns down first, then an upgrade is worth 1.75x its value per coin against a new gun.
  Mines: before every boss wave, and before a crowd wave when the guns would not hold it (cover under 1.00).
  Slow field: never.
  Harvester: in the first 40 % of the level, once the guns hold the waves at hand with cover 1.20.
  Repair drone: once a gun or gate has lost 10 % of its health.
  Attack drone: once 12 guns stand.
  Gate upgrades: the defence gate's gun and health, weighed at 1.20x against a tower step.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: the weakest ground gun may go for a sky gun the preview asks for (Psycho).
  The build order seed 3 took (2 stars, base 531):
  from wave  1: build Cannon at (29,6); build Flak Turret at (33,9); build Cannon at (32,5); buy the HarvestDrone; upgrade the defence gate; upgrade the tower at (32,5); upgrade the tower at (29,6); build Gun Turret at (34,6)
  from wave  4: upgrade the tower at (33,9); upgrade the tower at (34,6)
  from wave  5: build Flak Turret at (32,9)
  from wave  7: buy the RepairDrone
  from wave  8: upgrade the tower at (32,9)
  from wave  9: build Gun Turret at (29,5)
  from wave 10: upgrade the tower at (29,5); upgrade the tower at (29,5); sell the tower at (34,6); build Flak Turret at (34,6); build Gun Turret at (28,6)
  from wave 11: upgrade the tower at (28,6); build Gun Turret at (32,6)
  from wave 12: upgrade the tower at (32,6); build Gun Turret at (36,6)
  from wave 13: build Gun Turret at (30,9); upgrade the tower at (30,9); upgrade the tower at (36,6); build Gun Turret at (33,6); upgrade the tower at (33,6); build Gun Turret at (31,9); upgrade the tower at (31,9)
  from wave 14: build Gun Turret at (33,5); upgrade the tower at (33,5); build Gun Turret at (29,9)
  from wave 15: upgrade the tower at (29,9)
  from wave 16: build Gun Turret at (28,5); upgrade the tower at (28,5); upgrade the tower at (32,6); upgrade the tower at (30,9); upgrade the tower at (34,6); build Gun Turret at (31,10); upgrade the tower at (31,10); build Gun Turret at (32,10); upgrade the tower at (32,10); build Gun Turret at (33,10); upgrade the tower at (33,10); build Gun Turret at (29,10); upgrade the tower at (29,10); upgrade the tower at (33,6); upgrade the tower at (33,9); build Gun Turret at (28,9)
```

**Level_C1_04_3_strategy4** (won 3/3 seeds, 1.3 stars, base 316 left, repair drone)

```
  Towers per threat: swarm: whatever the ledger rates best; pack: whatever the ledger rates best; plated: whatever the ledger rates best; air: whatever the ledger rates best; boss: whatever the ledger rates best.
  Gun Turret spots, best first: (29,5) (30,9) (29,6).
  Cannon spots, best first: (32,5) (29,6) (29,5).
  Laser Turret spots, best first: (28,5) (29,5) (29,6).
  Flak Turret spots, best first: (33,9) (34,9) (34,6).
  Upgrades: 3 guns down first, then an upgrade is worth 1.33x its value per coin against a new gun.
  Mines: before every boss wave, never for crowds.
  Slow field: never.
  Harvester: in the first 30 % of the level, once the guns hold the waves at hand with cover 1.20.
  Repair drone: once a gun or gate has lost 25 % of its health.
  Attack drone: never.
  Gate upgrades: never.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: the weakest ground gun may go for a sky gun the preview asks for (Psycho).
  The build order seed 3 took (2 stars, base 410):
  from wave  1: build Cannon at (32,5); build Flak Turret at (33,9); build Cannon at (29,6); buy the HarvestDrone; build Cannon at (29,5); build Flak Turret at (34,9); build Cannon at (32,6)
  from wave  3: sell the tower at (32,5); build Flak Turret at (34,6)
  from wave  4: build Gun Turret at (30,9); upgrade the tower at (30,9)
  from wave  5: build Gun Turret at (28,6); upgrade the tower at (28,6)
  from wave  7: buy the RepairDrone
  from wave  8: upgrade the tower at (33,9)
  from wave  9: build Gun Turret at (36,6)
  from wave 10: upgrade the tower at (36,6); upgrade the tower at (30,9); upgrade the tower at (28,6); sell the tower at (29,6); build Flak Turret at (34,10)
  from wave 11: build Gun Turret at (29,6); upgrade the tower at (29,6)
  from wave 12: upgrade the tower at (29,6)
  from wave 13: build Gun Turret at (32,5); upgrade the tower at (32,5); build Gun Turret at (33,6); upgrade the tower at (33,6); build Gun Turret at (31,9); upgrade the tower at (31,9)
  from wave 14: build Gun Turret at (33,5); upgrade the tower at (33,5)
  from wave 15: upgrade the tower at (32,5); build Gun Turret at (32,9); upgrade the tower at (32,9)
  from wave 16: upgrade the tower at (33,6); build Gun Turret at (28,6); upgrade the tower at (32,6); upgrade the tower at (29,5); upgrade the tower at (28,6); build Gun Turret at (29,9); upgrade the tower at (29,9); upgrade the tower at (31,9); build Gun Turret at (28,5); upgrade the tower at (28,5); build Gun Turret at (31,10); upgrade the tower at (31,10); upgrade the tower at (33,9); upgrade the tower at (33,5); build Gun Turret at (32,10)
```


## Quartz Flats (Level_C1_05) on Easy

Best spots per tower: Gun Turret (7,11) (7,13) (3,15); Cannon (3,11) (3,15) (3,16); Laser Turret (6,12) (6,13) (6,14); Flak Turret (34,3) (34,4) (33,3).

**Level_C1_05_0_strategy3** (won 3/3 seeds, 3.0 stars, base 700 left, repair drone)

```
  Towers per threat: swarm: whatever the ledger rates best; pack: Gun Turret; plated: Cannon; air: whatever the ledger rates best; boss: Gun Turret.
  Gun Turret spots, best first: (7,11) (7,13) (3,15).
  Cannon spots, best first: (3,11) (3,15) (3,16).
  Laser Turret spots, best first: (6,14) (6,12) (6,13).
  Flak Turret spots, best first: (34,3) (34,4) (33,3).
  Upgrades: 2 guns down first, then an upgrade is worth 1.02x its value per coin against a new gun.
  Mines: not for bosses, and before a crowd wave when the guns would not hold it (cover under 1.00).
  Slow field: never.
  Harvester: never.
  Repair drone: once a gun or gate has lost 10 % of its health.
  Attack drone: once 6 guns stand.
  Gate upgrades: the defence gate's gun and health, weighed at 1.20x against a tower step.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: never.
  The build order seed 1 took (3 stars, base 700):
  from wave  1: build Cannon at (3,11); build Flak Turret at (34,3)
  from wave  3: build Gun Turret at (7,11)
  from wave  5: upgrade the tower at (7,11)
  from wave  6: build Gun Turret at (7,13)
  from wave  8: upgrade the tower at (34,3)
  from wave 10: upgrade the defence gate
  from wave 11: upgrade the tower at (7,11)
  from wave 12: upgrade the tower at (7,13); upgrade the tower at (7,13)
  from wave 14: build Cannon at (3,15)
  from wave 15: upgrade the tower at (3,11); upgrade the tower at (3,15)
  from wave 16: build Gun Turret at (3,16)
  from wave 17: buy the AttackDrone; buy the RepairDrone
```

**Level_C1_05_0_strategy5** (won 3/3 seeds, 3.0 stars, base 700 left, repair drone)

```
  Towers per threat: swarm: whatever the ledger rates best; pack: whatever the ledger rates best; plated: Laser Turret; air: Cannon; boss: whatever the ledger rates best.
  Gun Turret spots, best first: (7,13) (3,16) (7,11).
  Cannon spots, best first: (3,11) (3,15) (3,16).
  Laser Turret spots, best first: (6,12) (6,13) (6,14).
  Flak Turret spots, best first: (34,3) (34,4) (33,3).
  Upgrades: 3 guns down first, then an upgrade is worth 1.75x its value per coin against a new gun.
  Mines: before every boss wave, never for crowds.
  Slow field: when the guns would not hold the wave (cover under 1.00) or the slow pays by the damage rule.
  Harvester: never.
  Repair drone: once a gun or gate has lost 5 % of its health.
  Attack drone: once 12 guns stand.
  Gate upgrades: never.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: never.
  The build order seed 3 took (3 stars, base 700):
  from wave  1: build Cannon at (3,11); build Flak Turret at (34,3)
  from wave  3: build Gun Turret at (7,13)
  from wave  5: upgrade the tower at (7,13)
  from wave  6: build Gun Turret at (3,16)
  from wave  9: buy the RepairDrone
  from wave 10: upgrade the tower at (3,16)
  from wave 11: build Flak Turret at (34,4)
  from wave 12: upgrade the tower at (7,13)
  from wave 14: upgrade the tower at (3,16); upgrade the tower at (3,11)
  from wave 15: build Cannon at (3,15)
  from wave 16: upgrade the tower at (3,15); build Cannon at (7,11)
  from wave 17: upgrade the tower at (7,11); build Cannon at (6,11)
```


## Quartz Flats (Level_C1_05) on Medium

Best spots per tower: Gun Turret (7,11) (7,13) (3,15); Cannon (3,11) (3,15) (3,16); Laser Turret (6,12) (6,13) (6,14); Flak Turret (34,3) (34,4) (33,3).

**Level_C1_05_1_strategy2** (won 3/3 seeds, 3.0 stars, base 698 left, repair drone)

```
  Towers per threat: swarm: Laser Turret; pack: whatever the ledger rates best; plated: Laser Turret; air: whatever the ledger rates best; boss: whatever the ledger rates best.
  Gun Turret spots, best first: (7,11) (7,13) (3,15).
  Cannon spots, best first: (7,11) (3,11) (3,15).
  Laser Turret spots, best first: (6,12) (6,13) (6,14).
  Flak Turret spots, best first: (34,4) (34,3) (33,3).
  Upgrades: 3 guns down first, then an upgrade is worth 1.76x its value per coin against a new gun.
  Mines: before every boss wave, and before a crowd wave when the guns would not hold it (cover under 1.00).
  Slow field: when the guns would not hold the wave (cover under 1.20) or the slow pays by the damage rule.
  Harvester: never.
  Repair drone: once a gun or gate has lost 5 % of its health.
  Attack drone: once 6 guns stand.
  Gate upgrades: never.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: never.
  The build order seed 3 took (3 stars, base 700):
  from wave  1: build Cannon at (7,11); build Flak Turret at (34,4); build Gun Turret at (7,13)
  from wave  2: upgrade the tower at (7,13)
  from wave  6: upgrade the tower at (7,13); build Gun Turret at (3,15)
  from wave  9: upgrade the tower at (3,15); upgrade the tower at (3,15)
  from wave 10: build Cannon at (3,11)
  from wave 11: build Flak Turret at (34,3)
  from wave 13: upgrade the tower at (7,11)
  from wave 14: build Flak Turret at (33,3); buy the RepairDrone
  from wave 17: buy the AttackDrone; upgrade the tower at (3,11); upgrade the tower at (7,11)
```

**Level_C1_05_1_strategy3** (won 3/3 seeds, 3.0 stars, base 674 left, repair drone)

```
  Towers per threat: swarm: whatever the ledger rates best; pack: whatever the ledger rates best; plated: whatever the ledger rates best; air: Gun Turret; boss: whatever the ledger rates best.
  Gun Turret spots, best first: (7,11) (3,15) (3,16).
  Cannon spots, best first: (3,11) (3,15) (3,16).
  Laser Turret spots, best first: (6,12) (6,14) (6,13).
  Flak Turret spots, best first: (34,4) (34,3) (33,3).
  Upgrades: 2 guns down first, then an upgrade is worth 1.79x its value per coin against a new gun.
  Mines: before every boss wave, and before a crowd wave when the guns would not hold it (cover under 1.30).
  Slow field: when the guns would not hold the wave (cover under 1.00) or the slow pays by the damage rule.
  Harvester: never.
  Repair drone: once a gun or gate has lost 10 % of its health.
  Attack drone: once 8 guns stand.
  Gate upgrades: the defence gate's gun and health, weighed at 1.20x against a tower step.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: never.
  The build order seed 1 took (3 stars, base 676):
  from wave  1: build Cannon at (3,11); build Flak Turret at (34,4); build Gun Turret at (7,11)
  from wave  2: upgrade the tower at (7,11)
  from wave  6: upgrade the defence gate
  from wave  7: upgrade the tower at (34,4)
  from wave  9: upgrade the tower at (7,11)
  from wave 10: build Gun Turret at (3,15); upgrade the tower at (3,15)
  from wave 11: upgrade the tower at (3,11)
  from wave 12: upgrade the tower at (3,15)
  from wave 14: build Cannon at (3,16); build Flak Turret at (34,3)
  from wave 15: upgrade the tower at (3,16); buy the RepairDrone
  from wave 17: build Cannon at (6,11); upgrade the tower at (6,11); upgrade the defence gate
```


## Quartz Flats (Level_C1_05) on Hard

Best spots per tower: Gun Turret (7,11) (7,13) (3,15); Cannon (3,11) (3,15) (3,16); Laser Turret (6,12) (6,13) (6,14); Flak Turret (34,3) (34,4) (33,3).

**Level_C1_05_2_strategy5** (won 2/3 seeds, 2.5 stars, base 707 left)

```
  Towers per threat: swarm: Cannon; pack: Cannon; plated: whatever the ledger rates best; air: whatever the ledger rates best; boss: Laser Turret.
  Gun Turret spots, best first: (7,11) (3,16) (7,13).
  Cannon spots, best first: (3,11) (3,15) (7,11).
  Laser Turret spots, best first: (6,13) (6,14) (6,12).
  Flak Turret spots, best first: (34,4) (34,3) (33,3).
  Upgrades: 4 guns down first, then an upgrade is worth 0.60x its value per coin against a new gun.
  Mines: before every boss wave, and before a crowd wave when the guns would not hold it (cover under 1.30).
  Slow field: when the guns would not hold the wave (cover under 1.00) or the slow pays by the damage rule.
  Harvester: in the first 60 % of the level, once the guns hold the waves at hand with cover 1.00.
  Repair drone: never.
  Attack drone: never.
  Gate upgrades: never.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: never.
  The build order seed 1 took (3 stars, base 800):
  from wave  1: build Cannon at (3,11); build Flak Turret at (34,4); build Gun Turret at (7,11)
  from wave  4: build Cannon at (3,15)
  from wave  7: buy the HarvestDrone
  from wave  8: upgrade the tower at (7,11)
  from wave  9: build Flak Turret at (34,3); build Cannon at (3,16)
  from wave 10: build Flak Turret at (33,3); upgrade the tower at (34,4)
  from wave 11: upgrade the tower at (34,3); upgrade the tower at (33,3)
  from wave 12: build Cannon at (20,6); build Cannon at (6,11)
  from wave 13: build Gun Turret at (7,13); build Gun Turret at (7,10); upgrade the tower at (7,10)
  from wave 14: upgrade the tower at (7,13); build Gun Turret at (34,0); upgrade the tower at (34,0); build Gun Turret at (20,6); build Cannon at (3,10); build Cannon at (8,10); build Cannon at (8,7)
  from wave 15: build Cannon at (7,8); build Cannon at (7,9)
  from wave 16: build Cannon at (8,6)
  from wave 17: build Cannon at (8,9); build Cannon at (6,10); build Cannon at (7,7); build Cannon at (18,11); build Cannon at (6,8)
```

**Level_C1_05_2_strategy4** (won 2/3 seeds, 2.0 stars, base 460 left, repair drone)

```
  Towers per threat: swarm: Cannon; pack: Laser Turret; plated: Cannon; air: Laser Turret; boss: whatever the ledger rates best.
  Gun Turret spots, best first: (7,11) (7,13) (7,10).
  Cannon spots, best first: (7,11) (3,11) (3,15).
  Laser Turret spots, best first: (6,12) (6,13) (6,14).
  Flak Turret spots, best first: (34,3) (34,4) (33,3).
  Upgrades: 1 guns down first, then an upgrade is worth 0.91x its value per coin against a new gun.
  Mines: not for bosses, and before a crowd wave when the guns would not hold it (cover under 1.00).
  Slow field: when the guns would not hold the wave (cover under 1.00) or the slow pays by the damage rule.
  Harvester: in the first 30 % of the level, once the guns hold the waves at hand with cover 1.50.
  Repair drone: once a gun or gate has lost 10 % of its health.
  Attack drone: never.
  Gate upgrades: never.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: never.
  The build order seed 2 took (3 stars, base 728):
  from wave  1: build Cannon at (7,11); build Flak Turret at (34,3); build Gun Turret at (7,13)
  from wave  4: upgrade the tower at (7,11)
  from wave  7: buy the HarvestDrone; upgrade the tower at (34,3)
  from wave  9: build Cannon at (3,11)
  from wave 10: build Flak Turret at (34,4); buy the RepairDrone
  from wave 11: upgrade the tower at (34,4); upgrade the tower at (34,3)
  from wave 12: build Cannon at (3,15); build Cannon at (3,16)
  from wave 13: upgrade the tower at (7,13); build Gun Turret at (7,10)
  from wave 14: upgrade the tower at (7,10); build Gun Turret at (6,11); upgrade the tower at (6,11); upgrade the tower at (7,10); build Gun Turret at (20,6); upgrade the tower at (20,6); build Gun Turret at (34,0); upgrade the tower at (34,0)
  from wave 15: upgrade the tower at (3,11); upgrade the tower at (3,15)
  from wave 16: upgrade the tower at (6,11); upgrade the tower at (7,13)
  from wave 17: upgrade the tower at (3,16); build Cannon at (3,10); build Cannon at (8,10); build Cannon at (8,7); upgrade the tower at (8,7)
```


## Quartz Flats (Level_C1_05) on Psycho

Best spots per tower: Gun Turret (7,11) (7,13) (3,15); Cannon (3,11) (3,15) (3,16); Laser Turret (6,12) (6,13) (6,14); Flak Turret (34,3) (34,4) (33,3).

**Level_C1_05_3_strategy1** (won 3/3 seeds, 2.7 stars, base 866 left)

```
  Towers per threat: swarm: whatever the ledger rates best; pack: whatever the ledger rates best; plated: whatever the ledger rates best; air: Gun Turret; boss: Cannon.
  Gun Turret spots, best first: (7,13) (7,11) (7,10).
  Cannon spots, best first: (3,16) (7,11) (3,11).
  Laser Turret spots, best first: (6,12) (6,13) (6,14).
  Flak Turret spots, best first: (34,3) (35,3) (33,4).
  Upgrades: 3 guns down first, then an upgrade is worth 1.57x its value per coin against a new gun.
  Mines: before every boss wave, and before a crowd wave when the guns would not hold it (cover under 1.00).
  Slow field: when the guns would not hold the wave (cover under 1.00) or the slow pays by the damage rule.
  Harvester: in the first 30 % of the level, once the guns hold the waves at hand with cover 1.20.
  Repair drone: never.
  Attack drone: never.
  Gate upgrades: never.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: the weakest ground gun may go for a sky gun the preview asks for (Psycho).
  The build order seed 1 took (3 stars, base 900):
  from wave  1: build Cannon at (3,16); build Flak Turret at (34,3); build Cannon at (7,11); buy the HarvestDrone; build Cannon at (3,11); build Flak Turret at (35,3); upgrade the tower at (7,11); build Gun Turret at (7,13); upgrade the tower at (7,13)
  from wave  2: upgrade the tower at (7,13)
  from wave  4: upgrade the tower at (3,11)
  from wave  5: upgrade the tower at (34,3)
  from wave  6: build Gun Turret at (7,10); upgrade the tower at (7,10); upgrade the tower at (7,10); build Gun Turret at (3,15); upgrade the tower at (3,15)
  from wave  7: upgrade the tower at (35,3); upgrade the tower at (3,15)
  from wave  9: sell the tower at (3,16); build Flak Turret at (33,4); upgrade the tower at (7,11)
  from wave 10: upgrade the tower at (33,4)
  from wave 11: upgrade the tower at (34,3); upgrade the tower at (35,3); upgrade the tower at (33,4)
  from wave 12: build Gun Turret at (3,15); upgrade the tower at (3,15); upgrade the tower at (3,15); build Gun Turret at (6,11); upgrade the tower at (6,11)
  from wave 13: build Gun Turret at (34,0); upgrade the tower at (34,0)
  from wave 14: build Gun Turret at (3,16); upgrade the tower at (3,16); build Gun Turret at (20,6); upgrade the tower at (20,6); upgrade the tower at (3,11); upgrade the tower at (6,11); upgrade the tower at (3,16); upgrade the tower at (20,6)
  from wave 15: build Gun Turret at (8,10); upgrade the tower at (8,10); upgrade the tower at (8,10); build Gun Turret at (8,7); upgrade the tower at (8,7)
  from wave 16: upgrade the tower at (8,7); build Cannon at (3,10)
  from wave 17: upgrade the tower at (3,10); upgrade the tower at (3,10); build Cannon at (7,8); upgrade the tower at (7,8)
```

**Level_C1_05_3_strategy5** (won 3/3 seeds, 2.3 stars, base 739 left, repair drone)

```
  Towers per threat: swarm: whatever the ledger rates best; pack: whatever the ledger rates best; plated: whatever the ledger rates best; air: Laser Turret; boss: whatever the ledger rates best.
  Gun Turret spots, best first: (7,11) (7,13) (3,15).
  Cannon spots, best first: (7,11) (6,11) (3,11).
  Laser Turret spots, best first: (6,12) (6,13) (6,14).
  Flak Turret spots, best first: (34,3) (34,4) (33,3).
  Upgrades: 2 guns down first, then an upgrade is worth 1.77x its value per coin against a new gun.
  Mines: before every boss wave, and before a crowd wave when the guns would not hold it (cover under 1.30).
  Slow field: when the guns would not hold the wave (cover under 1.20) or the slow pays by the damage rule.
  Harvester: in the first 30 % of the level, once the guns hold the waves at hand with cover 1.00.
  Repair drone: once a gun or gate has lost 25 % of its health.
  Attack drone: never.
  Gate upgrades: never.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: the weakest ground gun may go for a sky gun the preview asks for (Psycho).
  The build order seed 2 took (3 stars, base 900):
  from wave  1: build Cannon at (7,11); build Flak Turret at (34,3); build Cannon at (6,11); buy the HarvestDrone; upgrade the tower at (7,11); build Cannon at (3,11); build Flak Turret at (34,4); build Gun Turret at (7,13); upgrade the tower at (7,13)
  from wave  2: upgrade the tower at (7,13)
  from wave  4: upgrade the tower at (6,11)
  from wave  5: upgrade the tower at (34,3)
  from wave  6: build Gun Turret at (3,15); upgrade the tower at (3,15); upgrade the tower at (3,15); upgrade the tower at (3,11)
  from wave  7: upgrade the tower at (34,4)
  from wave  9: build Flak Turret at (33,3); upgrade the tower at (7,11)
  from wave 10: upgrade the tower at (33,3)
  from wave 11: upgrade the tower at (34,3); upgrade the tower at (34,4)
  from wave 12: buy the RepairDrone; build Cannon at (3,16)
  from wave 13: upgrade the tower at (6,11)
  from wave 14: upgrade the tower at (3,11); upgrade the tower at (3,16); upgrade the tower at (3,16); build Cannon at (3,10); upgrade the tower at (3,10)
  from wave 15: upgrade the tower at (3,10)
  from wave 16: build Gun Turret at (7,10); upgrade the tower at (7,10); upgrade the tower at (7,10)
  from wave 17: build Laser Turret at (6,12); upgrade the tower at (6,12); upgrade the tower at (6,12); build Laser Turret at (6,13); upgrade the tower at (6,13); upgrade the tower at (6,13)
```


## Crysium Vein (Level_C1_06) on Easy

Best spots per tower: Gun Turret (12,10) (12,11) (13,10); Cannon (12,10) (13,10) (14,7); Laser Turret (13,10) (11,10) (12,10); Flak Turret (36,10) (36,7) (36,11).

**Level_C1_06_0_strategy4** (won 3/3 seeds, 3.0 stars, base 700 left, repair drone)

```
  Towers per threat: swarm: Laser Turret; pack: Cannon; plated: whatever the ledger rates best; air: whatever the ledger rates best; boss: whatever the ledger rates best.
  Gun Turret spots, best first: (12,10) (12,11) (13,10).
  Cannon spots, best first: (12,10) (13,10) (14,7).
  Laser Turret spots, best first: (13,10) (11,10) (12,10).
  Flak Turret spots, best first: (36,10) (36,7) (36,11).
  Upgrades: 1 guns down first, then an upgrade is worth 1.36x its value per coin against a new gun.
  Mines: not for bosses, never for crowds.
  Slow field: when the guns would not hold the wave (cover under 1.20) or the slow pays by the damage rule.
  Harvester: never.
  Repair drone: once a gun or gate has lost 50 % of its health.
  Attack drone: once 8 guns stand.
  Gate upgrades: never.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: never.
  The build order seed 1 took (3 stars, base 700):
  from wave  1: build Cannon at (12,10); build Flak Turret at (36,10)
  from wave  4: build Gun Turret at (12,11)
  from wave  6: upgrade the tower at (12,11)
  from wave  7: build Gun Turret at (13,10)
  from wave 10: buy the RepairDrone
  from wave 11: upgrade the tower at (13,10); build Gun Turret at (13,11)
  from wave 13: lay a SlowField on the road at (41,9); upgrade the tower at (12,10)
  from wave 15: upgrade the tower at (13,10); upgrade the tower at (13,11)
  from wave 16: upgrade the tower at (13,11); lay a SlowField on the road at (41,9)
  from wave 17: upgrade the tower at (12,11); lay a SlowField on the road at (41,9); build Cannon at (14,7)
  from wave 18: build Cannon at (14,11); build Cannon at (35,11)
```

**Level_C1_06_0_strategy1** (won 3/3 seeds, 3.0 stars, base 700 left, repair drone)

```
  Towers per threat: swarm: whatever the ledger rates best; pack: Cannon; plated: Cannon; air: Cannon; boss: Laser Turret.
  Gun Turret spots, best first: (12,10) (12,11) (13,10).
  Cannon spots, best first: (13,10) (12,10) (14,7).
  Laser Turret spots, best first: (13,10) (12,10) (11,10).
  Flak Turret spots, best first: (36,10) (36,7) (36,11).
  Upgrades: 4 guns down first, then an upgrade is worth 1.79x its value per coin against a new gun.
  Mines: before every boss wave, and before a crowd wave when the guns would not hold it (cover under 1.00).
  Slow field: when the guns would not hold the wave (cover under 1.20) or the slow pays by the damage rule.
  Harvester: in the first 50 % of the level, once the guns hold the waves at hand with cover 1.00.
  Repair drone: once a gun or gate has lost 50 % of its health.
  Attack drone: once 6 guns stand.
  Gate upgrades: the defence gate's gun and health, weighed at 1.20x against a tower step.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: never.
  The build order seed 2 took (3 stars, base 700):
  from wave  1: build Cannon at (13,10); build Flak Turret at (36,10)
  from wave  4: build Gun Turret at (12,10)
  from wave  8: buy the HarvestDrone
  from wave 11: build Cannon at (14,7)
  from wave 12: buy the RepairDrone
  from wave 13: lay a Mine on the road at (5,15); lay a Mine on the road at (5,15)
  from wave 14: lay a Mine on the road at (5,15); lay a Mine on the road at (5,15)
  from wave 15: lay a Mine on the road at (5,15); lay a Mine on the road at (5,15)
  from wave 16: lay a Mine on the road at (5,15); lay a Mine on the road at (5,15); lay a SlowField on the road at (41,9); upgrade the defence gate
  from wave 17: upgrade the tower at (12,10); upgrade the tower at (12,10); lay a Mine on the road at (5,15)
  from wave 18: lay a Mine on the road at (5,15); lay a Mine on the road at (5,15); lay a SlowField on the road at (41,9); build Cannon at (12,11); build Cannon at (35,11)
```


## Crysium Vein (Level_C1_06) on Medium

Best spots per tower: Gun Turret (12,10) (12,11) (13,10); Cannon (12,10) (13,10) (14,7); Laser Turret (13,10) (11,10) (12,10); Flak Turret (36,10) (36,7) (36,11).

**Level_C1_06_1_strategy1** (won 3/3 seeds, 2.0 stars, base 568 left, repair drone)

```
  Towers per threat: swarm: whatever the ledger rates best; pack: whatever the ledger rates best; plated: Laser Turret; air: whatever the ledger rates best; boss: Laser Turret.
  Gun Turret spots, best first: (12,10) (12,11) (13,10).
  Cannon spots, best first: (12,10) (13,10) (14,7).
  Laser Turret spots, best first: (13,10) (11,10) (12,10).
  Flak Turret spots, best first: (36,10) (36,7) (36,11).
  Upgrades: 3 guns down first, then an upgrade is worth 1.42x its value per coin against a new gun.
  Mines: before every boss wave, never for crowds.
  Slow field: when the guns would not hold the wave (cover under 1.00) or the slow pays by the damage rule.
  Harvester: in the first 60 % of the level, once the guns hold the waves at hand with cover 1.20.
  Repair drone: once a gun or gate has lost 10 % of its health.
  Attack drone: once 12 guns stand.
  Gate upgrades: the defence gate's gun and health, weighed at 0.80x against a tower step.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: never.
  The build order seed 2 took (2 stars, base 622):
  from wave  1: build Cannon at (12,10); build Flak Turret at (36,10)
  from wave  3: build Cannon at (13,10)
  from wave  9: buy the HarvestDrone
  from wave 10: upgrade the defence gate
  from wave 12: build Gun Turret at (12,11); upgrade the tower at (12,11); build Gun Turret at (13,11)
  from wave 14: upgrade the tower at (36,10); upgrade the tower at (13,11)
  from wave 15: build Gun Turret at (14,7); upgrade the tower at (14,7); build Gun Turret at (14,11); upgrade the tower at (14,11)
  from wave 16: build Flak Turret at (36,7); lay a SlowField on the road at (41,9)
  from wave 17: upgrade the tower at (14,7); build Gun Turret at (21,11)
  from wave 18: upgrade the tower at (21,11); build Gun Turret at (36,11); upgrade the tower at (36,11); upgrade the tower at (14,11); upgrade the tower at (13,11); buy the RepairDrone; lay a Mine on the road at (5,15); lay a Mine on the road at (36,15)
```

**Level_C1_06_1_strategy3** (won 3/3 seeds, 2.0 stars, base 568 left, repair drone)

```
  Towers per threat: swarm: whatever the ledger rates best; pack: whatever the ledger rates best; plated: whatever the ledger rates best; air: Gun Turret; boss: Laser Turret.
  Gun Turret spots, best first: (12,11) (13,11) (13,10).
  Cannon spots, best first: (12,10) (13,10) (12,11).
  Laser Turret spots, best first: (13,10) (11,10) (12,10).
  Flak Turret spots, best first: (36,10) (36,7) (36,11).
  Upgrades: 3 guns down first, then an upgrade is worth 1.54x its value per coin against a new gun.
  Mines: before every boss wave, never for crowds.
  Slow field: when the guns would not hold the wave (cover under 1.00) or the slow pays by the damage rule.
  Harvester: in the first 60 % of the level, once the guns hold the waves at hand with cover 1.20.
  Repair drone: once a gun or gate has lost 5 % of its health.
  Attack drone: once 12 guns stand.
  Gate upgrades: the defence gate's gun and health, weighed at 0.80x against a tower step.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: never.
  The build order seed 2 took (2 stars, base 622):
  from wave  1: build Cannon at (12,10); build Flak Turret at (36,10)
  from wave  3: build Cannon at (13,10)
  from wave  9: buy the HarvestDrone
  from wave 10: upgrade the defence gate
  from wave 12: build Gun Turret at (12,11); upgrade the tower at (12,11); build Gun Turret at (13,11)
  from wave 14: upgrade the tower at (36,10); upgrade the tower at (13,11)
  from wave 15: build Gun Turret at (14,11); upgrade the tower at (14,11); build Gun Turret at (14,7); upgrade the tower at (14,7)
  from wave 16: build Flak Turret at (36,7); lay a SlowField on the road at (41,9)
  from wave 17: upgrade the tower at (14,11); build Gun Turret at (21,11)
  from wave 18: upgrade the tower at (21,11); build Gun Turret at (36,11); upgrade the tower at (36,11); upgrade the tower at (14,7); build Gun Turret at (14,10); upgrade the tower at (14,10); buy the RepairDrone; lay a Mine on the road at (5,15); lay a Mine on the road at (36,15)
```


## Crysium Vein (Level_C1_06) on Hard

Best spots per tower: Gun Turret (12,10) (12,11) (13,10); Cannon (12,10) (13,10) (14,7); Laser Turret (13,10) (11,10) (12,10); Flak Turret (36,10) (36,7) (36,11).

**Level_C1_06_2_strategy1** (won 3/3 seeds, 3.0 stars, base 800 left, repair drone)

```
  Towers per threat: swarm: whatever the ledger rates best; pack: whatever the ledger rates best; plated: whatever the ledger rates best; air: whatever the ledger rates best; boss: Gun Turret.
  Gun Turret spots, best first: (12,11) (21,11) (12,10).
  Cannon spots, best first: (14,7) (12,10) (12,11).
  Laser Turret spots, best first: (13,10) (12,10) (11,10).
  Flak Turret spots, best first: (36,10) (36,7) (36,11).
  Upgrades: 1 guns down first, then an upgrade is worth 1.47x its value per coin against a new gun.
  Mines: before every boss wave, and before a crowd wave when the guns would not hold it (cover under 1.00).
  Slow field: when the guns would not hold the wave (cover under 1.00) or the slow pays by the damage rule.
  Harvester: never.
  Repair drone: once a gun or gate has lost 50 % of its health.
  Attack drone: never.
  Gate upgrades: never.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: never.
  The build order seed 2 took (3 stars, base 800):
  from wave  1: build Cannon at (14,7); build Flak Turret at (36,10)
  from wave  2: upgrade the tower at (14,7)
  from wave  4: build Gun Turret at (12,11)
  from wave  5: upgrade the tower at (12,11)
  from wave  6: upgrade the tower at (12,11)
  from wave  7: upgrade the tower at (36,10)
  from wave  9: build Gun Turret at (21,11); lay a Mine on the road at (5,15)
  from wave 10: lay a Mine on the road at (36,15); buy the RepairDrone
  from wave 11: upgrade the tower at (21,11); build Gun Turret at (12,10)
  from wave 12: upgrade the tower at (12,10); build Gun Turret at (13,11); upgrade the tower at (13,11); build Gun Turret at (13,10); upgrade the tower at (13,10)
  from wave 14: upgrade the tower at (13,10); build Gun Turret at (14,11); upgrade the tower at (14,11)
  from wave 15: build Gun Turret at (36,11); upgrade the tower at (36,11); build Flak Turret at (36,7); lay a Mine on the road at (5,15); lay a Mine on the road at (5,15); upgrade the tower at (14,11); upgrade the tower at (12,10)
  from wave 16: build Flak Turret at (34,10); lay a Mine on the road at (36,15); lay a Mine on the road at (36,22); lay a SlowField on the road at (41,9)
  from wave 17: lay a Mine on the road at (36,15); lay a Mine on the road at (36,15); upgrade the tower at (13,11); build Gun Turret at (14,10)
  from wave 18: upgrade the tower at (14,10); upgrade the tower at (14,10); upgrade the tower at (36,11); lay a Mine on the road at (36,15); lay a Mine on the road at (36,22); lay a SlowField on the road at (41,9); upgrade the tower at (14,7); build Cannon at (35,11); upgrade the tower at (35,11)
```

**Level_C1_06_2_strategy3** (won 3/3 seeds, 3.0 stars, base 779 left, repair drone)

```
  Towers per threat: swarm: whatever the ledger rates best; pack: Laser Turret; plated: whatever the ledger rates best; air: whatever the ledger rates best; boss: whatever the ledger rates best.
  Gun Turret spots, best first: (12,11) (13,10) (12,10).
  Cannon spots, best first: (14,7) (12,10) (12,11).
  Laser Turret spots, best first: (11,10) (13,10) (12,10).
  Flak Turret spots, best first: (36,10) (36,7) (36,11).
  Upgrades: 2 guns down first, then an upgrade is worth 1.41x its value per coin against a new gun.
  Mines: before every boss wave, and before a crowd wave when the guns would not hold it (cover under 1.00).
  Slow field: never.
  Harvester: never.
  Repair drone: once a gun or gate has lost 10 % of its health.
  Attack drone: once 8 guns stand.
  Gate upgrades: never.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: never.
  The build order seed 3 took (3 stars, base 800):
  from wave  1: build Cannon at (14,7); build Flak Turret at (36,10)
  from wave  2: upgrade the tower at (14,7)
  from wave  4: build Gun Turret at (12,11)
  from wave  5: upgrade the tower at (12,11)
  from wave  6: upgrade the tower at (12,11)
  from wave  7: upgrade the tower at (36,10)
  from wave  9: build Gun Turret at (13,10)
  from wave 10: lay a Mine on the road at (5,15); lay a Mine on the road at (36,15); buy the RepairDrone
  from wave 11: upgrade the tower at (13,10); build Gun Turret at (12,10)
  from wave 12: upgrade the tower at (12,10); build Gun Turret at (13,11); upgrade the tower at (13,11); build Gun Turret at (14,11); upgrade the tower at (14,11)
  from wave 14: upgrade the tower at (13,10); build Gun Turret at (36,11); upgrade the tower at (36,11)
  from wave 15: build Gun Turret at (21,11); upgrade the tower at (21,11); build Flak Turret at (36,7); lay a Mine on the road at (5,15); lay a Mine on the road at (36,15)
  from wave 16: upgrade the tower at (14,11); build Gun Turret at (14,10); build Flak Turret at (34,10); lay a Mine on the road at (36,15); lay a Mine on the road at (36,15)
  from wave 17: lay a Mine on the road at (36,15); lay a Mine on the road at (36,15); upgrade the tower at (12,10); upgrade the tower at (14,10); build Gun Turret at (32,10)
  from wave 18: upgrade the tower at (32,10); upgrade the tower at (32,10); build Gun Turret at (15,10); upgrade the tower at (15,10); lay a Mine on the road at (5,15); lay a Mine on the road at (36,15); buy the AttackDrone; upgrade the tower at (14,10); upgrade the tower at (15,10); upgrade the tower at (13,11)
```


## Crysium Vein (Level_C1_06) on Psycho

Best spots per tower: Gun Turret (12,10) (12,11) (13,10); Cannon (12,10) (13,10) (14,7); Laser Turret (13,10) (11,10) (12,10); Flak Turret (36,10) (36,7) (36,11).

**Level_C1_06_3_strategy2** (won 3/3 seeds, 2.3 stars, base 717 left, repair drone)

```
  Towers per threat: swarm: Cannon; pack: whatever the ledger rates best; plated: Cannon; air: Laser Turret; boss: Laser Turret.
  Gun Turret spots, best first: (12,10) (12,11) (13,10).
  Cannon spots, best first: (12,10) (13,10) (14,7).
  Laser Turret spots, best first: (13,10) (11,10) (12,10).
  Flak Turret spots, best first: (36,7) (36,10) (36,11).
  Upgrades: 2 guns down first, then an upgrade is worth 1.74x its value per coin against a new gun.
  Mines: not for bosses, never for crowds.
  Slow field: when the guns would not hold the wave (cover under 1.20) or the slow pays by the damage rule.
  Harvester: in the first 60 % of the level, once the guns hold the waves at hand with cover 1.00.
  Repair drone: once a gun or gate has lost 10 % of its health.
  Attack drone: once 12 guns stand.
  Gate upgrades: never.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: the weakest ground gun may go for a sky gun the preview asks for (Psycho).
  The build order seed 2 took (3 stars, base 900):
  from wave  1: build Cannon at (12,10); build Flak Turret at (36,7); build Cannon at (13,10); buy the HarvestDrone; build Cannon at (14,7); build Flak Turret at (36,10); upgrade the tower at (13,10); build Cannon at (12,11)
  from wave  2: sell the tower at (12,10); build Flak Turret at (36,11)
  from wave  4: lay a SlowField on the road at (41,9)
  from wave  5: buy the RepairDrone
  from wave  6: upgrade the tower at (36,7)
  from wave  7: upgrade the tower at (36,10); upgrade the tower at (36,11)
  from wave  9: build Gun Turret at (12,10); upgrade the tower at (12,10)
  from wave 10: upgrade the tower at (12,10); build Gun Turret at (13,11)
  from wave 11: upgrade the tower at (13,11); build Gun Turret at (12,10); upgrade the tower at (12,10); build Gun Turret at (14,11)
  from wave 12: upgrade the tower at (14,11); lay a SlowField on the road at (41,9); upgrade the tower at (13,10); build Gun Turret at (21,11); upgrade the tower at (21,11); upgrade the tower at (14,7)
  from wave 13: lay a SlowField on the road at (41,9)
  from wave 14: lay a SlowField on the road at (41,9)
  from wave 15: upgrade the tower at (14,11); upgrade the tower at (12,10); build Gun Turret at (14,10); lay a SlowField on the road at (41,9); upgrade the tower at (14,7); upgrade the tower at (14,10)
  from wave 16: build Gun Turret at (32,10); lay a SlowField on the road at (41,9); buy the AttackDrone; upgrade the tower at (14,10)
  from wave 17: lay a SlowField on the road at (41,9); upgrade the tower at (32,10); build Gun Turret at (15,10); upgrade the tower at (15,10); build Gun Turret at (31,10)
  from wave 18: upgrade the tower at (31,10); build Gun Turret at (15,11); upgrade the tower at (15,11); build Gun Turret at (16,10); upgrade the tower at (16,10); upgrade the tower at (32,10); upgrade the tower at (31,10); upgrade the tower at (15,10); upgrade the tower at (15,11); upgrade the tower at (13,11); lay a SlowField on the road at (41,9); upgrade the tower at (12,11)
```

**Level_C1_06_3_strategy1** (won 3/3 seeds, 2.0 stars, base 563 left, repair drone)

```
  Towers per threat: swarm: Cannon; pack: Gun Turret; plated: whatever the ledger rates best; air: whatever the ledger rates best; boss: whatever the ledger rates best.
  Gun Turret spots, best first: (12,10) (12,11) (13,10).
  Cannon spots, best first: (12,10) (13,10) (14,7).
  Laser Turret spots, best first: (13,10) (11,10) (12,10).
  Flak Turret spots, best first: (36,10) (36,7) (36,11).
  Upgrades: 3 guns down first, then an upgrade is worth 1.49x its value per coin against a new gun.
  Mines: not for bosses, never for crowds.
  Slow field: when the guns would not hold the wave (cover under 1.20) or the slow pays by the damage rule.
  Harvester: in the first 40 % of the level, once the guns hold the waves at hand with cover 1.20.
  Repair drone: once a gun or gate has lost 5 % of its health.
  Attack drone: once 12 guns stand.
  Gate upgrades: never.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: the weakest ground gun may go for a sky gun the preview asks for (Psycho).
  The build order seed 1 took (2 stars, base 660):
  from wave  1: build Cannon at (12,10); build Flak Turret at (36,10); build Cannon at (13,10); buy the HarvestDrone; upgrade the tower at (13,10); build Cannon at (14,7); build Flak Turret at (36,7); build Cannon at (12,11)
  from wave  2: sell the tower at (12,10); build Flak Turret at (36,11)
  from wave  4: buy the RepairDrone
  from wave  6: upgrade the tower at (36,10); upgrade the tower at (36,7)
  from wave  7: upgrade the tower at (36,11)
  from wave  8: lay a SlowField on the road at (41,9)
  from wave  9: build Gun Turret at (12,10); upgrade the tower at (12,10)
  from wave 10: upgrade the tower at (12,10); build Gun Turret at (13,11)
  from wave 11: upgrade the tower at (13,11); build Gun Turret at (12,10); upgrade the tower at (12,10)
  from wave 12: build Gun Turret at (14,11); upgrade the tower at (14,11); lay a SlowField on the road at (41,9); upgrade the tower at (13,10); build Gun Turret at (21,11); upgrade the tower at (21,11)
  from wave 13: lay a SlowField on the road at (41,9); upgrade the tower at (14,7)
  from wave 14: lay a SlowField on the road at (41,9)
  from wave 15: upgrade the tower at (14,11); upgrade the tower at (12,10); build Gun Turret at (14,10); lay a SlowField on the road at (41,9)
  from wave 16: upgrade the tower at (13,11); upgrade the tower at (14,10); upgrade the tower at (14,7); upgrade the tower at (14,10); lay a SlowField on the road at (41,9); upgrade the tower at (12,11)
  from wave 17: lay a SlowField on the road at (41,9); build Gun Turret at (32,10); upgrade the tower at (32,10); build Gun Turret at (15,10); upgrade the tower at (15,10)
  from wave 18: lay a SlowField on the road at (41,9); buy the AttackDrone; upgrade the tower at (32,10); upgrade the tower at (12,11); upgrade the tower at (15,10); build Cannon at (35,11); upgrade the tower at (35,11); upgrade the tower at (35,11)
```

