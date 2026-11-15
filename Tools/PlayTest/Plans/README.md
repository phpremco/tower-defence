# Winning plans per level and difficulty

Written from the stage-B replay (`Tools/PlayTest/plan_records.txt`, `bash Tools/PlayTest/balance_pipeline.sh readme`) on 2026-11-15 15:06; calibration 2026-11-15 14:41 (factors of 2026-11-15 12:40 kept), seeds 1,2,3, step 0.2, inputs c0e168ae (current).

A plan counts as a winner only when it won in play on most seeds (the paper verdict is a pre-screen). Each plan is a priority list: a step is
taken from its wave on, as soon as the money is there (an earlier step blocks the later ones). Cells are (x,y) on the level grid. The obvious
family's results are in `Tools/PlayTest/yardstick.txt`. A cell without a winner shows how far the best replayed plan got: a design question.

## Landing Site (Level_C1_01) on Easy

Best spots per tower: Gun Turret (9,3) (9,6) (10,3); Cannon (11,3) (11,6) (12,3).

**Level_C1_01_0_strategy4** (won 3/3 seeds, 3.0 stars, base 600 left)

```
  Towers per threat: swarm: whatever the ledger rates best; pack: whatever the ledger rates best; plated: whatever the ledger rates best; air: Gun Turret; boss: Cannon.
  Gun Turret spots, best first: (9,3) (9,6) (10,3).
  Cannon spots, best first: (31,14) (11,3) (13,3).
  Upgrades: 2 guns down first, then an upgrade is worth 1.01x its value per coin against a new gun.
  Mines: before every boss wave, and before a crowd wave when the guns would not hold it (cover under 1.30).
  Slow field: when the guns would not hold the wave (cover under 1.20) or the slow pays by the damage rule.
  Harvester: in the first 50 % of the level, once the guns hold the waves at hand with cover 1.00.
  Repair drone: once a gun or gate has lost 10 % of its health.
  Attack drone: never.
  Gate upgrades: never.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: never.
  The build order seed 1 took (3 stars, base 600):
  from wave  1: build Cannon at (31,14); build Gun Turret at (9,3)
  from wave  3: upgrade the tower at (9,3)
  from wave  5: build Gun Turret at (9,6)
  from wave  6: upgrade the tower at (9,6)
  from wave  9: build Cannon at (11,3)
  from wave 10: build Cannon at (13,3)
  from wave 11: upgrade the tower at (9,6)
  from wave 12: upgrade the tower at (9,3)
```

**Level_C1_01_0_strategy3** (won 3/3 seeds, 2.7 stars, base 533 left)

```
  Towers per threat: swarm: Cannon; pack: whatever the ledger rates best; plated: Cannon; air: whatever the ledger rates best; boss: whatever the ledger rates best.
  Gun Turret spots, best first: (31,14) (9,6) (10,3).
  Cannon spots, best first: (12,3) (12,7) (11,6).
  Upgrades: 1 guns down first, then an upgrade is worth 0.81x its value per coin against a new gun.
  Mines: before every boss wave, never for crowds.
  Slow field: when the guns would not hold the wave (cover under 1.00) or the slow pays by the damage rule.
  Harvester: in the first 60 % of the level, once the guns hold the waves at hand with cover 1.00.
  Repair drone: once a gun or gate has lost 10 % of its health.
  Attack drone: once 12 guns stand.
  Gate upgrades: the defence gate's gun and health, weighed at 1.20x against a tower step.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: never.
  The build order seed 1 took (3 stars, base 600):
  from wave  1: build Cannon at (12,3); build Gun Turret at (31,14)
  from wave  3: build Gun Turret at (9,6)
  from wave  5: upgrade the tower at (9,6)
  from wave  8: upgrade the defence gate
  from wave 10: build Cannon at (12,7)
  from wave 11: upgrade the tower at (9,6)
  from wave 12: build Cannon at (11,6)
```


## Landing Site (Level_C1_01) on Medium

Best spots per tower: Gun Turret (9,3) (9,6) (10,3); Cannon (11,3) (11,6) (12,3).

**Level_C1_01_1_strategy2** (won 3/3 seeds, 3.0 stars, base 600 left)

```
  Towers per threat: swarm: whatever the ledger rates best; pack: whatever the ledger rates best; plated: whatever the ledger rates best; air: whatever the ledger rates best; boss: Gun Turret.
  Gun Turret spots, best first: (9,6) (10,6) (9,3).
  Cannon spots, best first: (5,14) (11,6) (11,3).
  Upgrades: 1 guns down first, then an upgrade is worth 0.72x its value per coin against a new gun.
  Mines: before every boss wave, and before a crowd wave when the guns would not hold it (cover under 1.00).
  Slow field: when the guns would not hold the wave (cover under 1.00) or the slow pays by the damage rule.
  Harvester: in the first 50 % of the level, once the guns hold the waves at hand with cover 1.00.
  Repair drone: once a gun or gate has lost 5 % of its health.
  Attack drone: once 8 guns stand.
  Gate upgrades: never.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: never.
  The build order seed 1 took (3 stars, base 600):
  from wave  1: build Cannon at (5,14); build Gun Turret at (9,6); build Gun Turret at (10,6)
  from wave  3: upgrade the tower at (10,6)
  from wave  6: build Cannon at (11,6)
  from wave  8: upgrade the tower at (9,6); build Gun Turret at (9,3)
  from wave 10: build Cannon at (11,3)
  from wave 11: upgrade the tower at (10,6); upgrade the tower at (9,3)
  from wave 12: build Gun Turret at (10,3); upgrade the tower at (10,3)
```

**Level_C1_01_1_strategy4** (won 3/3 seeds, 3.0 stars, base 600 left)

```
  Towers per threat: swarm: whatever the ledger rates best; pack: Cannon; plated: whatever the ledger rates best; air: Cannon; boss: whatever the ledger rates best.
  Gun Turret spots, best first: (9,6) (31,14) (9,3).
  Cannon spots, best first: (5,14) (11,6) (12,3).
  Upgrades: 2 guns down first, then an upgrade is worth 0.69x its value per coin against a new gun.
  Mines: before every boss wave, never for crowds.
  Slow field: when the guns would not hold the wave (cover under 1.00) or the slow pays by the damage rule.
  Harvester: in the first 40 % of the level, once the guns hold the waves at hand with cover 1.20.
  Repair drone: once a gun or gate has lost 5 % of its health.
  Attack drone: never.
  Gate upgrades: the defence gate's gun and health, weighed at 1.20x against a tower step.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: never.
  The build order seed 1 took (3 stars, base 600):
  from wave  1: build Cannon at (5,14); build Gun Turret at (9,6); upgrade the tower at (9,6)
  from wave  5: upgrade the defence gate
  from wave  8: build Cannon at (11,6)
  from wave  9: build Cannon at (12,3)
  from wave 10: build Cannon at (12,6)
  from wave 11: upgrade the tower at (9,6)
  from wave 12: build Cannon at (11,3)
```


## Landing Site (Level_C1_01) on Hard

Best spots per tower: Gun Turret (9,3) (9,6) (10,3); Cannon (11,3) (11,6) (12,3); Laser Turret (8,6) (9,6) (10,6).

**Level_C1_01_2_strategy3** (won 2/3 seeds, 3.0 stars, base 700 left)

```
  Towers per threat: swarm: Cannon; pack: whatever the ledger rates best; plated: whatever the ledger rates best; air: whatever the ledger rates best; boss: Gun Turret.
  Gun Turret spots, best first: (9,3) (9,6) (10,3).
  Cannon spots, best first: (11,3) (12,3) (11,6).
  Laser Turret spots, best first: (9,6) (8,6) (10,6).
  Upgrades: 4 guns down first, then an upgrade is worth 1.30x its value per coin against a new gun.
  Mines: before every boss wave, never for crowds.
  Slow field: when the guns would not hold the wave (cover under 1.00) or the slow pays by the damage rule.
  Harvester: in the first 60 % of the level, once the guns hold the waves at hand with cover 1.50.
  Repair drone: never.
  Attack drone: once 6 guns stand.
  Gate upgrades: never.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: never.
  The build order seed 2 took (3 stars, base 700):
  from wave  1: build Cannon at (11,3); build Cannon at (12,3)
  from wave  3: build Gun Turret at (9,3)
  from wave  4: build Gun Turret at (9,6)
  from wave  5: upgrade the tower at (9,6)
  from wave  7: upgrade the tower at (9,6); upgrade the tower at (9,3)
  from wave  8: upgrade the tower at (9,3)
  from wave  9: build Gun Turret at (10,3); upgrade the tower at (10,3); build Gun Turret at (10,6); upgrade the tower at (10,6); upgrade the tower at (11,3); upgrade the tower at (12,3)
  from wave 10: build Cannon at (11,6)
  from wave 11: upgrade the tower at (11,6); upgrade the tower at (10,6)
  from wave 12: upgrade the tower at (10,3)
```

**Level_C1_01_2_strategy2** (won 2/3 seeds, 3.0 stars, base 700 left)

```
  Towers per threat: swarm: Cannon; pack: Laser Turret; plated: whatever the ledger rates best; air: whatever the ledger rates best; boss: whatever the ledger rates best.
  Gun Turret spots, best first: (9,3) (9,6) (10,3).
  Cannon spots, best first: (11,6) (12,7) (11,3).
  Laser Turret spots, best first: (8,6) (11,6) (9,6).
  Upgrades: 1 guns down first, then an upgrade is worth 1.44x its value per coin against a new gun.
  Mines: before every boss wave, never for crowds.
  Slow field: when the guns would not hold the wave (cover under 1.20) or the slow pays by the damage rule.
  Harvester: never.
  Repair drone: never.
  Attack drone: never.
  Gate upgrades: never.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: never.
  The build order seed 1 took (3 stars, base 700):
  from wave  1: build Cannon at (11,6); build Cannon at (12,7)
  from wave  3: build Gun Turret at (9,3)
  from wave  4: upgrade the tower at (9,3)
  from wave  5: upgrade the tower at (9,3)
  from wave  7: build Gun Turret at (9,6); upgrade the tower at (9,6)
  from wave  8: upgrade the tower at (9,6)
  from wave  9: build Gun Turret at (10,3); upgrade the tower at (11,6); upgrade the tower at (12,7); build Cannon at (11,3)
  from wave 10: upgrade the tower at (11,3)
  from wave 11: upgrade the tower at (10,3); upgrade the tower at (10,3); upgrade the tower at (11,6)
  from wave 12: build Gun Turret at (10,6); upgrade the tower at (10,6)
```


## Landing Site (Level_C1_01) on Psycho

Best spots per tower: Gun Turret (9,3) (9,6) (10,3); Cannon (11,3) (11,6) (12,3); Laser Turret (8,6) (9,6) (10,6).

**Level_C1_01_3_strategy2** (won 2/3 seeds, 3.0 stars, base 700 left)

```
  Towers per threat: swarm: whatever the ledger rates best; pack: whatever the ledger rates best; plated: Laser Turret; air: whatever the ledger rates best; boss: whatever the ledger rates best.
  Gun Turret spots, best first: (9,3) (9,6) (10,3).
  Cannon spots, best first: (11,6) (12,3) (11,3).
  Laser Turret spots, best first: (8,6) (9,6) (10,6).
  Upgrades: 2 guns down first, then an upgrade is worth 1.01x its value per coin against a new gun.
  Mines: before every boss wave, and before a crowd wave when the guns would not hold it (cover under 1.00).
  Slow field: when the guns would not hold the wave (cover under 1.20) or the slow pays by the damage rule.
  Harvester: in the first 60 % of the level, once the guns hold the waves at hand with cover 1.20.
  Repair drone: once a gun or gate has lost 10 % of its health.
  Attack drone: never.
  Gate upgrades: never.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: the weakest ground gun may go for a sky gun the preview asks for (Psycho).
  The build order seed 1 took (3 stars, base 700):
  from wave  1: build Cannon at (11,6); build Cannon at (12,3); build Cannon at (11,3); build Cannon at (12,7); build Laser Turret at (8,6); upgrade the tower at (8,6)
  from wave  2: build Gun Turret at (9,3)
  from wave  3: upgrade the tower at (9,3)
  from wave  4: upgrade the tower at (9,3)
  from wave  6: upgrade the tower at (11,6); upgrade the tower at (11,3); upgrade the tower at (12,7)
  from wave  8: upgrade the tower at (8,6)
  from wave  9: build Gun Turret at (9,6); upgrade the tower at (11,6); upgrade the tower at (12,3)
  from wave 10: build Cannon at (5,14); upgrade the tower at (9,6)
  from wave 11: upgrade the tower at (11,3); upgrade the tower at (12,7)
  from wave 12: upgrade the tower at (9,6)
```

**Level_C1_01_3_strategy3** (won 2/3 seeds, 3.0 stars, base 700 left)

```
  Towers per threat: swarm: whatever the ledger rates best; pack: whatever the ledger rates best; plated: whatever the ledger rates best; air: whatever the ledger rates best; boss: whatever the ledger rates best.
  Gun Turret spots, best first: (9,3) (9,6) (10,3).
  Cannon spots, best first: (11,3) (11,6) (12,6).
  Laser Turret spots, best first: (8,6) (9,6) (11,6).
  Upgrades: 1 guns down first, then an upgrade is worth 1.07x its value per coin against a new gun.
  Mines: before every boss wave, and before a crowd wave when the guns would not hold it (cover under 1.00).
  Slow field: never.
  Harvester: in the first 40 % of the level, once the guns hold the waves at hand with cover 1.00.
  Repair drone: once a gun or gate has lost 25 % of its health.
  Attack drone: once 8 guns stand.
  Gate upgrades: never.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: the weakest ground gun may go for a sky gun the preview asks for (Psycho).
  The build order seed 1 took (3 stars, base 700):
  from wave  1: build Cannon at (11,3); build Cannon at (11,6); build Cannon at (12,6); build Cannon at (12,3); build Cannon at (12,7); build Gun Turret at (9,3); upgrade the tower at (9,3)
  from wave  3: upgrade the tower at (9,3)
  from wave  4: build Gun Turret at (9,6); upgrade the tower at (9,6)
  from wave  6: upgrade the tower at (11,6); upgrade the tower at (12,6)
  from wave  7: upgrade the tower at (9,6)
  from wave  9: build Gun Turret at (10,3); upgrade the tower at (10,3); upgrade the tower at (11,6); upgrade the tower at (11,3); upgrade the tower at (12,7)
  from wave 10: build Gun Turret at (10,6); upgrade the tower at (10,6); build Gun Turret at (5,14)
  from wave 11: upgrade the tower at (12,3)
  from wave 12: upgrade the tower at (10,6); upgrade the tower at (10,3)
```


## Glass Meadows (Level_C1_02) on Easy

Best spots per tower: Gun Turret (1,15) (1,14) (4,15); Cannon (1,15) (4,15) (1,14); Laser Turret (0,15) (1,14) (1,15).

**Level_C1_02_0_strategy3** (won 3/3 seeds, 3.0 stars, base 700 left)

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
  from wave  7: build Cannon at (1,13)
  from wave  9: build Cannon at (4,13)
  from wave 10: upgrade the tower at (1,14); upgrade the tower at (1,15)
  from wave 11: upgrade the tower at (1,14)
  from wave 12: build Gun Turret at (4,14); upgrade the tower at (4,14); upgrade the tower at (4,14)
```

**Level_C1_02_0_strategy4** (won 3/3 seeds, 3.0 stars, base 700 left)

```
  Towers per threat: swarm: whatever the ledger rates best; pack: whatever the ledger rates best; plated: whatever the ledger rates best; air: Cannon; boss: Laser Turret.
  Gun Turret spots, best first: (1,15) (1,14) (4,15).
  Cannon spots, best first: (4,15) (1,15) (1,13).
  Laser Turret spots, best first: (0,15) (1,15) (0,14).
  Upgrades: 3 guns down first, then an upgrade is worth 1.05x its value per coin against a new gun.
  Mines: before every boss wave, never for crowds.
  Slow field: when the guns would not hold the wave (cover under 1.00) or the slow pays by the damage rule.
  Harvester: never.
  Repair drone: once a gun or gate has lost 5 % of its health.
  Attack drone: once 8 guns stand.
  Gate upgrades: never.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: never.
  The build order seed 1 took (3 stars, base 700):
  from wave  1: build Cannon at (4,15); build Gun Turret at (1,15)
  from wave  2: build Gun Turret at (1,14)
  from wave  4: upgrade the tower at (1,15)
  from wave  7: build Cannon at (1,13)
  from wave  9: build Cannon at (4,13)
  from wave 10: upgrade the tower at (1,14); upgrade the tower at (1,15)
  from wave 11: upgrade the tower at (1,14)
  from wave 12: build Gun Turret at (4,14); upgrade the tower at (4,14); upgrade the tower at (4,14)
```


## Glass Meadows (Level_C1_02) on Medium

Best spots per tower: Gun Turret (1,15) (1,14) (4,15); Cannon (1,15) (4,15) (1,14); Laser Turret (0,15) (1,14) (1,15).

**Level_C1_02_1_strategy4** (won 3/3 seeds, 3.0 stars, base 700 left, repair drone)

```
  Towers per threat: swarm: whatever the ledger rates best; pack: Laser Turret; plated: whatever the ledger rates best; air: Cannon; boss: Cannon.
  Gun Turret spots, best first: (1,15) (1,14) (4,15).
  Cannon spots, best first: (1,15) (4,13) (4,15).
  Laser Turret spots, best first: (0,15) (1,14) (4,15).
  Upgrades: 2 guns down first, then an upgrade is worth 1.47x its value per coin against a new gun.
  Mines: before every boss wave, never for crowds.
  Slow field: when the guns would not hold the wave (cover under 1.20) or the slow pays by the damage rule.
  Harvester: in the first 60 % of the level, once the guns hold the waves at hand with cover 1.20.
  Repair drone: once a gun or gate has lost 50 % of its health.
  Attack drone: once 8 guns stand.
  Gate upgrades: the defence gate's gun and health, weighed at 0.80x against a tower step.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: never.
  The build order seed 2 took (3 stars, base 700):
  from wave  1: build Cannon at (1,15); build Cannon at (4,13)
  from wave  2: build Gun Turret at (1,14)
  from wave  4: upgrade the tower at (1,14)
  from wave  6: upgrade the defence gate
  from wave  8: upgrade the tower at (4,13)
  from wave  9: build Gun Turret at (4,15); upgrade the tower at (4,15)
  from wave 10: upgrade the tower at (1,15)
  from wave 11: upgrade the tower at (4,15); upgrade the tower at (1,14)
  from wave 12: build Cannon at (4,14)
```

**Level_C1_02_1_strategy2** (won 3/3 seeds, 3.0 stars, base 700 left, repair drone)

```
  Towers per threat: swarm: whatever the ledger rates best; pack: whatever the ledger rates best; plated: whatever the ledger rates best; air: whatever the ledger rates best; boss: whatever the ledger rates best.
  Gun Turret spots, best first: (1,15) (1,14) (4,15).
  Cannon spots, best first: (1,15) (1,13) (4,15).
  Laser Turret spots, best first: (0,15) (1,14) (1,15).
  Upgrades: 3 guns down first, then an upgrade is worth 1.15x its value per coin against a new gun.
  Mines: before every boss wave, never for crowds.
  Slow field: when the guns would not hold the wave (cover under 1.00) or the slow pays by the damage rule.
  Harvester: in the first 50 % of the level, once the guns hold the waves at hand with cover 1.00.
  Repair drone: once a gun or gate has lost 5 % of its health.
  Attack drone: once 12 guns stand.
  Gate upgrades: the defence gate's gun and health, weighed at 1.20x against a tower step.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: never.
  The build order seed 2 took (3 stars, base 700):
  from wave  1: build Cannon at (1,15); build Cannon at (1,13)
  from wave  2: build Gun Turret at (1,14)
  from wave  4: upgrade the tower at (1,14)
  from wave  6: upgrade the defence gate
  from wave  8: upgrade the tower at (1,15)
  from wave  9: build Gun Turret at (4,15); upgrade the tower at (4,15)
  from wave 10: upgrade the tower at (1,13)
  from wave 11: upgrade the tower at (4,15)
  from wave 12: buy the RepairDrone; upgrade the tower at (1,14)
```


## Glass Meadows (Level_C1_02) on Hard

Best spots per tower: Gun Turret (1,15) (1,14) (4,15); Cannon (1,15) (4,15) (1,14); Laser Turret (0,15) (1,14) (1,15); Flak Turret (32,16) (31,16) (32,19).

**Level_C1_02_2_strategy2** (won 3/3 seeds, 3.0 stars, base 700 left)

```
  Towers per threat: swarm: whatever the ledger rates best; pack: Cannon; plated: whatever the ledger rates best; air: Cannon; boss: Laser Turret.
  Gun Turret spots, best first: (1,15) (1,14) (4,14).
  Cannon spots, best first: (4,15) (1,14) (4,13).
  Laser Turret spots, best first: (0,15) (1,15) (1,14).
  Flak Turret spots, best first: (32,16) (32,19) (33,15).
  Upgrades: 2 guns down first, then an upgrade is worth 1.61x its value per coin against a new gun.
  Mines: not for bosses, and before a crowd wave when the guns would not hold it (cover under 1.30).
  Slow field: when the guns would not hold the wave (cover under 1.20) or the slow pays by the damage rule.
  Harvester: in the first 40 % of the level, once the guns hold the waves at hand with cover 1.20.
  Repair drone: once a gun or gate has lost 50 % of its health.
  Attack drone: once 8 guns stand.
  Gate upgrades: never.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: never.
  The build order seed 3 took (3 stars, base 700):
  from wave  1: build Cannon at (4,15); build Cannon at (1,14)
  from wave  2: build Gun Turret at (1,15)
  from wave  4: upgrade the tower at (1,15)
  from wave  5: upgrade the tower at (4,15)
  from wave  7: upgrade the tower at (1,14)
  from wave  8: upgrade the tower at (1,15)
  from wave  9: build Cannon at (4,13); upgrade the tower at (4,13)
  from wave 10: build Cannon at (1,13); upgrade the tower at (1,13); build Gun Turret at (4,14)
  from wave 11: upgrade the tower at (4,14); upgrade the tower at (4,14)
  from wave 12: upgrade the tower at (4,13)
```

**Level_C1_02_2_strategy1** (won 3/3 seeds, 3.0 stars, base 700 left, repair drone)

```
  Towers per threat: swarm: whatever the ledger rates best; pack: whatever the ledger rates best; plated: Laser Turret; air: whatever the ledger rates best; boss: Laser Turret.
  Gun Turret spots, best first: (1,14) (1,15) (4,14).
  Cannon spots, best first: (4,15) (1,14) (1,13).
  Laser Turret spots, best first: (0,15) (1,14) (1,15).
  Flak Turret spots, best first: (32,16) (31,16) (32,19).
  Upgrades: 2 guns down first, then an upgrade is worth 1.33x its value per coin against a new gun.
  Mines: before every boss wave, and before a crowd wave when the guns would not hold it (cover under 1.00).
  Slow field: when the guns would not hold the wave (cover under 1.00) or the slow pays by the damage rule.
  Harvester: in the first 40 % of the level, once the guns hold the waves at hand with cover 1.50.
  Repair drone: once a gun or gate has lost 5 % of its health.
  Attack drone: never.
  Gate upgrades: never.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: never.
  The build order seed 3 took (3 stars, base 700):
  from wave  1: build Cannon at (4,15); build Cannon at (1,14)
  from wave  2: build Gun Turret at (1,15)
  from wave  4: upgrade the tower at (1,15)
  from wave  5: upgrade the tower at (4,15)
  from wave  7: upgrade the tower at (1,14)
  from wave  8: upgrade the tower at (1,15); build Gun Turret at (4,14); upgrade the tower at (4,14)
  from wave  9: upgrade the tower at (4,14); buy the RepairDrone
  from wave 10: build Cannon at (1,13)
  from wave 11: build Gun Turret at (32,16); upgrade the tower at (1,13)
  from wave 12: upgrade the tower at (4,15)
```


## Glass Meadows (Level_C1_02) on Psycho

Best spots per tower: Gun Turret (1,15) (1,14) (4,15); Cannon (1,15) (4,15) (1,14); Laser Turret (0,15) (1,14) (1,15); Flak Turret (32,16) (31,16) (32,19).

**Level_C1_02_3_strategy2** (won 3/3 seeds, 3.0 stars, base 800 left, repair drone)

```
  Towers per threat: swarm: whatever the ledger rates best; pack: whatever the ledger rates best; plated: whatever the ledger rates best; air: whatever the ledger rates best; boss: Gun Turret.
  Gun Turret spots, best first: (1,15) (1,14) (4,15).
  Cannon spots, best first: (1,15) (1,14) (1,13).
  Laser Turret spots, best first: (1,14) (1,15) (0,15).
  Flak Turret spots, best first: (32,16) (31,16) (32,19).
  Upgrades: 2 guns down first, then an upgrade is worth 1.43x its value per coin against a new gun.
  Mines: before every boss wave, and before a crowd wave when the guns would not hold it (cover under 1.00).
  Slow field: never.
  Harvester: in the first 30 % of the level, once the guns hold the waves at hand with cover 1.50.
  Repair drone: once a gun or gate has lost 25 % of its health.
  Attack drone: never.
  Gate upgrades: never.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: the weakest ground gun may go for a sky gun the preview asks for (Psycho).
  The build order seed 1 took (3 stars, base 800):
  from wave  1: build Cannon at (1,15); build Cannon at (1,14); upgrade the tower at (1,15); upgrade the tower at (1,14); build Cannon at (1,13); upgrade the tower at (1,13); build Cannon at (4,15)
  from wave  3: build Gun Turret at (4,14)
  from wave  4: upgrade the tower at (4,14)
  from wave  6: upgrade the tower at (1,15); upgrade the tower at (4,15)
  from wave  7: upgrade the tower at (4,14)
  from wave  8: upgrade the tower at (4,15)
  from wave  9: buy the RepairDrone; upgrade the tower at (1,13)
  from wave 10: upgrade the tower at (1,14)
  from wave 11: build Gun Turret at (4,13); upgrade the tower at (4,13); upgrade the tower at (4,13)
  from wave 12: build Laser Turret at (0,15); upgrade the tower at (0,15)
```

**Level_C1_02_3_strategy1** (won 3/3 seeds, 3.0 stars, base 800 left, repair drone)

```
  Towers per threat: swarm: whatever the ledger rates best; pack: whatever the ledger rates best; plated: whatever the ledger rates best; air: whatever the ledger rates best; boss: whatever the ledger rates best.
  Gun Turret spots, best first: (1,15) (4,14) (1,14).
  Cannon spots, best first: (1,13) (1,15) (1,14).
  Laser Turret spots, best first: (0,15) (1,14) (1,15).
  Flak Turret spots, best first: (31,16) (32,16) (32,19).
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
  The build order seed 3 took (3 stars, base 800):
  from wave  1: build Cannon at (1,13); build Cannon at (1,15); build Cannon at (1,14); upgrade the tower at (1,15); upgrade the tower at (1,13); upgrade the tower at (1,14); build Cannon at (4,15)
  from wave  3: build Gun Turret at (4,14)
  from wave  4: upgrade the tower at (4,14)
  from wave  6: upgrade the tower at (1,15); upgrade the tower at (4,15)
  from wave  7: upgrade the tower at (4,14)
  from wave  8: upgrade the tower at (4,15)
  from wave  9: upgrade the tower at (1,13)
  from wave 10: upgrade the tower at (1,14); build Cannon at (4,13)
  from wave 11: upgrade the tower at (4,13); buy the RepairDrone
  from wave 12: upgrade the tower at (4,13)
```


## Shard Ridge (Level_C1_03) on Easy

Best spots per tower: Gun Turret (12,11) (13,11) (11,11); Cannon (12,10) (12,11) (13,10); Laser Turret (11,10) (12,10) (13,10); Flak Turret (32,10) (33,10) (34,7).

**Level_C1_03_0_strategy2** (won 3/3 seeds, 3.0 stars, base 700 left, repair drone)

```
  Towers per threat: swarm: whatever the ledger rates best; pack: whatever the ledger rates best; plated: whatever the ledger rates best; air: Gun Turret; boss: Laser Turret.
  Gun Turret spots, best first: (12,11) (13,11) (11,11).
  Cannon spots, best first: (12,11) (12,10) (13,10).
  Laser Turret spots, best first: (11,10) (12,10) (13,10).
  Flak Turret spots, best first: (32,10) (33,10) (34,7).
  Upgrades: 3 guns down first, then an upgrade is worth 1.64x its value per coin against a new gun.
  Mines: before every boss wave, never for crowds.
  Slow field: never.
  Harvester: in the first 30 % of the level, once the guns hold the waves at hand with cover 1.00.
  Repair drone: once a gun or gate has lost 10 % of its health.
  Attack drone: once 8 guns stand.
  Gate upgrades: never.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: never.
  The build order seed 1 took (3 stars, base 700):
  from wave  1: build Cannon at (12,11); build Gun Turret at (13,11)
  from wave  2: build Gun Turret at (11,11)
  from wave  9: build Flak Turret at (32,10)
  from wave 10: buy the HarvestDrone
  from wave 12: buy the RepairDrone; upgrade the tower at (13,11)
  from wave 13: upgrade the tower at (11,11); upgrade the tower at (13,11)
  from wave 14: build Gun Turret at (12,10); upgrade the tower at (32,10); upgrade the tower at (12,10)
  from wave 15: upgrade the tower at (12,10)
  from wave 16: upgrade the tower at (11,11); build Gun Turret at (13,10); upgrade the tower at (13,10); build Gun Turret at (14,11); upgrade the tower at (14,11)
```

**Level_C1_03_0_strategy1** (won 3/3 seeds, 3.0 stars, base 700 left)

```
  Towers per threat: swarm: Laser Turret; pack: whatever the ledger rates best; plated: Laser Turret; air: Laser Turret; boss: Laser Turret.
  Gun Turret spots, best first: (12,11) (13,11) (11,11).
  Cannon spots, best first: (12,11) (12,10) (13,10).
  Laser Turret spots, best first: (11,10) (12,10) (13,12).
  Flak Turret spots, best first: (32,10) (33,10) (34,7).
  Upgrades: 4 guns down first, then an upgrade is worth 1.69x its value per coin against a new gun.
  Mines: before every boss wave, and before a crowd wave when the guns would not hold it (cover under 1.00).
  Slow field: never.
  Harvester: never.
  Repair drone: never.
  Attack drone: never.
  Gate upgrades: never.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: never.
  The build order seed 1 took (3 stars, base 700):
  from wave  1: build Cannon at (12,11); build Gun Turret at (13,11)
  from wave  2: build Gun Turret at (11,11)
  from wave  5: build Gun Turret at (12,10)
  from wave  7: upgrade the tower at (13,11)
  from wave  8: upgrade the tower at (12,10)
  from wave 10: build Flak Turret at (32,10); upgrade the tower at (11,11)
  from wave 12: upgrade the tower at (13,11)
  from wave 13: upgrade the tower at (12,10)
  from wave 14: upgrade the tower at (32,10)
  from wave 15: upgrade the tower at (11,11); build Gun Turret at (13,10)
  from wave 16: upgrade the tower at (13,10); build Gun Turret at (14,11); upgrade the tower at (14,11); upgrade the tower at (13,10)
```


## Shard Ridge (Level_C1_03) on Medium

Best spots per tower: Gun Turret (12,11) (13,11) (11,11); Cannon (12,10) (12,11) (13,10); Laser Turret (11,10) (12,10) (13,10); Flak Turret (32,10) (33,10) (34,7).

**Level_C1_03_1_strategy2** (won 3/3 seeds, 3.0 stars, base 700 left, repair drone)

```
  Towers per threat: swarm: whatever the ledger rates best; pack: whatever the ledger rates best; plated: whatever the ledger rates best; air: whatever the ledger rates best; boss: whatever the ledger rates best.
  Gun Turret spots, best first: (12,11) (13,11) (11,11).
  Cannon spots, best first: (12,11) (12,10) (13,10).
  Laser Turret spots, best first: (11,10) (10,10) (12,10).
  Flak Turret spots, best first: (32,10) (33,10) (34,7).
  Upgrades: 4 guns down first, then an upgrade is worth 1.64x its value per coin against a new gun.
  Mines: before every boss wave, and before a crowd wave when the guns would not hold it (cover under 1.00).
  Slow field: when the guns would not hold the wave (cover under 1.00) or the slow pays by the damage rule.
  Harvester: in the first 50 % of the level, once the guns hold the waves at hand with cover 1.00.
  Repair drone: once a gun or gate has lost 25 % of its health.
  Attack drone: once 12 guns stand.
  Gate upgrades: the defence gate's gun and health, weighed at 0.80x against a tower step.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: never.
  The build order seed 1 took (3 stars, base 700):
  from wave  1: build Cannon at (12,11); build Gun Turret at (13,11); build Gun Turret at (11,11)
  from wave  6: buy the HarvestDrone
  from wave  7: build Gun Turret at (12,10)
  from wave 10: build Flak Turret at (32,10); build Flak Turret at (33,10)
  from wave 12: buy the RepairDrone
  from wave 13: upgrade the defence gate
  from wave 14: build Cannon at (13,10); upgrade the tower at (13,11); upgrade the tower at (32,10); upgrade the tower at (12,10)
  from wave 15: upgrade the tower at (11,11)
  from wave 16: build Gun Turret at (14,11); upgrade the tower at (14,11); build Gun Turret at (21,11); upgrade the tower at (13,11); build Flak Turret at (34,7)
```

**Level_C1_03_1_strategy1** (won 3/3 seeds, 3.0 stars, base 700 left, repair drone)

```
  Towers per threat: swarm: whatever the ledger rates best; pack: Laser Turret; plated: Laser Turret; air: Gun Turret; boss: whatever the ledger rates best.
  Gun Turret spots, best first: (12,11) (13,11) (11,11).
  Cannon spots, best first: (12,11) (13,10) (11,10).
  Laser Turret spots, best first: (12,10) (11,10) (13,10).
  Flak Turret spots, best first: (32,10) (33,10) (34,7).
  Upgrades: 4 guns down first, then an upgrade is worth 1.66x its value per coin against a new gun.
  Mines: before every boss wave, and before a crowd wave when the guns would not hold it (cover under 1.00).
  Slow field: never.
  Harvester: in the first 40 % of the level, once the guns hold the waves at hand with cover 1.20.
  Repair drone: once a gun or gate has lost 25 % of its health.
  Attack drone: once 8 guns stand.
  Gate upgrades: the defence gate's gun and health, weighed at 1.20x against a tower step.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: never.
  The build order seed 3 took (3 stars, base 700):
  from wave  1: build Cannon at (12,11); build Laser Turret at (12,10)
  from wave  4: build Gun Turret at (13,11)
  from wave  8: build Flak Turret at (32,10)
  from wave 10: build Flak Turret at (33,10)
  from wave 12: buy the HarvestDrone
  from wave 13: buy the RepairDrone
  from wave 14: upgrade the defence gate; build Cannon at (13,10); upgrade the tower at (13,11)
  from wave 15: build Gun Turret at (11,11)
  from wave 16: upgrade the tower at (11,11); build Gun Turret at (14,11); upgrade the tower at (14,11); build Gun Turret at (21,11); upgrade the tower at (12,10); upgrade the tower at (13,11)
```


## Shard Ridge (Level_C1_03) on Hard

Best spots per tower: Gun Turret (12,11) (13,11) (11,11); Cannon (12,10) (12,11) (13,10); Laser Turret (11,10) (12,10) (13,10); Flak Turret (32,10) (33,10) (34,7).

**Level_C1_03_2_strategy1** (won 3/3 seeds, 2.7 stars, base 667 left, repair drone)

```
  Towers per threat: swarm: Gun Turret; pack: Laser Turret; plated: whatever the ledger rates best; air: whatever the ledger rates best; boss: whatever the ledger rates best.
  Gun Turret spots, best first: (13,11) (11,11) (12,11).
  Cannon spots, best first: (12,11) (34,11) (12,10).
  Laser Turret spots, best first: (11,10) (13,10) (12,10).
  Flak Turret spots, best first: (32,10) (34,7) (34,11).
  Upgrades: 3 guns down first, then an upgrade is worth 0.78x its value per coin against a new gun.
  Mines: before every boss wave, and before a crowd wave when the guns would not hold it (cover under 1.00).
  Slow field: when the guns would not hold the wave (cover under 1.20) or the slow pays by the damage rule.
  Harvester: in the first 30 % of the level, once the guns hold the waves at hand with cover 1.20.
  Repair drone: once a gun or gate has lost 10 % of its health.
  Attack drone: once 8 guns stand.
  Gate upgrades: never.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: never.
  The build order seed 2 took (3 stars, base 700):
  from wave  1: build Cannon at (12,11); build Gun Turret at (13,11); build Gun Turret at (11,11)
  from wave  5: buy the HarvestDrone
  from wave  6: build Cannon at (34,11)
  from wave  8: buy the RepairDrone
  from wave  9: build Flak Turret at (32,10)
  from wave 10: upgrade the tower at (13,11); upgrade the tower at (11,11); build Gun Turret at (14,11)
  from wave 12: build Flak Turret at (34,7)
  from wave 13: upgrade the tower at (14,11); build Gun Turret at (12,10); upgrade the tower at (12,10); build Gun Turret at (13,10); upgrade the tower at (13,10)
  from wave 14: build Gun Turret at (21,11); upgrade the tower at (32,10); build Gun Turret at (31,10)
  from wave 15: build Gun Turret at (14,10); upgrade the tower at (31,10); build Gun Turret at (14,12)
  from wave 16: build Gun Turret at (31,11); build Gun Turret at (13,12); build Gun Turret at (11,10); build Cannon at (33,11); build Cannon at (32,12); build Gun Turret at (30,10); build Cannon at (32,11); upgrade the tower at (31,11); build Gun Turret at (15,10); upgrade the tower at (30,10)
```


## Shard Ridge (Level_C1_03) on Psycho

Best spots per tower: Gun Turret (12,11) (13,11) (11,11); Cannon (12,10) (12,11) (13,10); Laser Turret (11,10) (12,10) (13,10); Flak Turret (32,10) (33,10) (34,7).

**Level_C1_03_3_strategy2** (won 3/3 seeds, 3.0 stars, base 785 left, repair drone)

```
  Towers per threat: swarm: whatever the ledger rates best; pack: whatever the ledger rates best; plated: Gun Turret; air: whatever the ledger rates best; boss: whatever the ledger rates best.
  Gun Turret spots, best first: (12,11) (12,10) (13,10).
  Cannon spots, best first: (12,10) (12,11) (13,10).
  Laser Turret spots, best first: (11,10) (12,10) (13,10).
  Flak Turret spots, best first: (32,10) (33,10) (34,10).
  Upgrades: 1 guns down first, then an upgrade is worth 1.56x its value per coin against a new gun.
  Mines: before every boss wave, and before a crowd wave when the guns would not hold it (cover under 1.30).
  Slow field: when the guns would not hold the wave (cover under 1.20) or the slow pays by the damage rule.
  Harvester: in the first 40 % of the level, once the guns hold the waves at hand with cover 1.50.
  Repair drone: once a gun or gate has lost 5 % of its health.
  Attack drone: once 8 guns stand.
  Gate upgrades: never.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: the weakest ground gun may go for a sky gun the preview asks for (Psycho).
  The build order seed 2 took (3 stars, base 800):
  from wave  1: build Cannon at (12,10); build Cannon at (12,11); build Cannon at (13,10); buy the HarvestDrone; build Cannon at (13,11); build Cannon at (11,10); build Gun Turret at (11,11)
  from wave  3: buy the RepairDrone
  from wave  5: upgrade the tower at (11,11); build Gun Turret at (21,11)
  from wave  6: upgrade the tower at (11,11); upgrade the tower at (21,11)
  from wave  7: upgrade the tower at (13,10)
  from wave  9: sell the tower at (12,10); build Flak Turret at (32,10); build Gun Turret at (12,10)
  from wave 10: upgrade the tower at (12,10); build Gun Turret at (34,11); sell the tower at (34,11); build Flak Turret at (33,10); build Gun Turret at (34,11)
  from wave 11: sell the tower at (34,11); build Flak Turret at (34,10)
  from wave 12: build Flak Turret at (34,7); upgrade the tower at (32,10)
  from wave 13: upgrade the tower at (33,10); upgrade the tower at (12,10); build Cannon at (33,11); sell the tower at (12,11); build Flak Turret at (34,11)
  from wave 14: upgrade the tower at (13,11); upgrade the tower at (33,11)
  from wave 15: upgrade the tower at (11,10)
  from wave 16: build Cannon at (12,11); upgrade the tower at (12,11); build Cannon at (32,12); upgrade the tower at (32,12); build Cannon at (14,10); upgrade the tower at (14,10)
```

**Level_C1_03_3_strategy1** (won 3/3 seeds, 2.7 stars, base 751 left, repair drone)

```
  Towers per threat: swarm: whatever the ledger rates best; pack: Cannon; plated: whatever the ledger rates best; air: whatever the ledger rates best; boss: Gun Turret.
  Gun Turret spots, best first: (12,11) (12,10) (11,11).
  Cannon spots, best first: (12,10) (13,10) (11,10).
  Laser Turret spots, best first: (13,10) (11,10) (12,10).
  Flak Turret spots, best first: (33,10) (32,10) (34,11).
  Upgrades: 4 guns down first, then an upgrade is worth 1.40x its value per coin against a new gun.
  Mines: before every boss wave, and before a crowd wave when the guns would not hold it (cover under 1.00).
  Slow field: never.
  Harvester: in the first 60 % of the level, once the guns hold the waves at hand with cover 1.20.
  Repair drone: once a gun or gate has lost 5 % of its health.
  Attack drone: never.
  Gate upgrades: never.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: the weakest ground gun may go for a sky gun the preview asks for (Psycho).
  The build order seed 1 took (3 stars, base 800):
  from wave  1: build Cannon at (12,10); build Cannon at (13,10); build Cannon at (11,10); buy the HarvestDrone; build Cannon at (12,11); build Cannon at (13,11)
  from wave  2: buy the RepairDrone
  from wave  3: build Gun Turret at (11,11)
  from wave  5: upgrade the tower at (11,11); build Gun Turret at (34,11); upgrade the tower at (34,11)
  from wave  6: upgrade the tower at (34,11)
  from wave  8: upgrade the tower at (13,10)
  from wave  9: sell the tower at (12,10); build Flak Turret at (33,10); build Gun Turret at (12,10)
  from wave 10: upgrade the tower at (12,10); build Gun Turret at (14,11); sell the tower at (14,11); build Flak Turret at (32,10); build Gun Turret at (14,11)
  from wave 11: sell the tower at (14,11); build Flak Turret at (34,10)
  from wave 12: build Flak Turret at (34,7); upgrade the tower at (33,10)
  from wave 13: upgrade the tower at (32,10); build Cannon at (33,11); sell the tower at (11,10); build Flak Turret at (35,7); build Cannon at (11,10)
  from wave 14: upgrade the tower at (13,11)
  from wave 15: upgrade the tower at (12,10)
  from wave 16: build Cannon at (32,12); upgrade the tower at (32,12); upgrade the tower at (33,11); build Cannon at (14,10); upgrade the tower at (14,10); build Cannon at (14,11); upgrade the tower at (14,11)
```


## Fault Line (Level_C1_04) on Easy

Best spots per tower: Gun Turret (29,5) (29,6) (28,6); Cannon (29,6) (32,6) (29,5); Laser Turret (28,5) (29,5) (29,6); Flak Turret (34,9) (33,9) (34,6).

**Level_C1_04_0_strategy1** (won 3/3 seeds, 3.0 stars, base 700 left)

```
  Towers per threat: swarm: whatever the ledger rates best; pack: whatever the ledger rates best; plated: whatever the ledger rates best; air: whatever the ledger rates best; boss: whatever the ledger rates best.
  Gun Turret spots, best first: (29,6) (28,6) (29,5).
  Cannon spots, best first: (32,6) (29,6) (29,5).
  Laser Turret spots, best first: (28,5) (29,5) (29,6).
  Flak Turret spots, best first: (34,9) (34,10) (33,9).
  Upgrades: 4 guns down first, then an upgrade is worth 1.44x its value per coin against a new gun.
  Mines: not for bosses, and before a crowd wave when the guns would not hold it (cover under 1.00).
  Slow field: when the guns would not hold the wave (cover under 1.00) or the slow pays by the damage rule.
  Harvester: in the first 60 % of the level, once the guns hold the waves at hand with cover 1.00.
  Repair drone: never.
  Attack drone: once 12 guns stand.
  Gate upgrades: the defence gate's gun and health, weighed at 1.20x against a tower step.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: never.
  The build order seed 3 took (3 stars, base 700):
  from wave  1: build Cannon at (32,6)
  from wave  3: build Flak Turret at (34,9)
  from wave  6: build Gun Turret at (29,6)
  from wave 11: buy the HarvestDrone
  from wave 12: build Gun Turret at (28,6)
  from wave 13: build Flak Turret at (34,10)
  from wave 14: upgrade the defence gate
  from wave 15: upgrade the tower at (34,9)
  from wave 16: upgrade the tower at (34,10); upgrade the tower at (29,6); upgrade the tower at (29,6); upgrade the tower at (28,6); upgrade the tower at (34,9)
```

**Level_C1_04_0_strategy2** (won 3/3 seeds, 3.0 stars, base 700 left, repair drone)

```
  Towers per threat: swarm: Cannon; pack: whatever the ledger rates best; plated: Cannon; air: Laser Turret; boss: whatever the ledger rates best.
  Gun Turret spots, best first: (29,5) (28,6) (29,6).
  Cannon spots, best first: (32,6) (29,6) (29,5).
  Laser Turret spots, best first: (28,5) (28,6) (29,6).
  Flak Turret spots, best first: (34,9) (34,10) (34,6).
  Upgrades: 3 guns down first, then an upgrade is worth 1.18x its value per coin against a new gun.
  Mines: before every boss wave, and before a crowd wave when the guns would not hold it (cover under 1.00).
  Slow field: when the guns would not hold the wave (cover under 1.20) or the slow pays by the damage rule.
  Harvester: in the first 60 % of the level, once the guns hold the waves at hand with cover 1.50.
  Repair drone: once a gun or gate has lost 25 % of its health.
  Attack drone: once 12 guns stand.
  Gate upgrades: never.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: never.
  The build order seed 1 took (3 stars, base 700):
  from wave  1: build Cannon at (32,6)
  from wave  3: build Flak Turret at (34,9)
  from wave  6: build Gun Turret at (29,5)
  from wave 11: buy the HarvestDrone; upgrade the tower at (29,5)
  from wave 12: upgrade the tower at (34,9)
  from wave 14: build Cannon at (29,6)
  from wave 15: build Flak Turret at (34,10)
  from wave 16: buy the RepairDrone; upgrade the tower at (34,10); upgrade the tower at (34,9); build Cannon at (30,9)
```


## Fault Line (Level_C1_04) on Medium

Best spots per tower: Gun Turret (29,5) (29,6) (28,6); Cannon (29,6) (32,6) (29,5); Laser Turret (28,5) (29,5) (29,6); Flak Turret (34,9) (33,9) (34,6).

**Level_C1_04_1_strategy2** (won 3/3 seeds, 3.0 stars, base 672 left)

```
  Towers per threat: swarm: Gun Turret; pack: Laser Turret; plated: Gun Turret; air: Gun Turret; boss: whatever the ledger rates best.
  Gun Turret spots, best first: (29,5) (29,6) (28,6).
  Cannon spots, best first: (29,6) (32,6) (29,5).
  Laser Turret spots, best first: (28,5) (29,5) (29,6).
  Flak Turret spots, best first: (34,9) (33,9) (34,6).
  Upgrades: 1 guns down first, then an upgrade is worth 1.68x its value per coin against a new gun.
  Mines: before every boss wave, and before a crowd wave when the guns would not hold it (cover under 1.00).
  Slow field: never.
  Harvester: in the first 30 % of the level, once the guns hold the waves at hand with cover 1.20.
  Repair drone: once a gun or gate has lost 50 % of its health.
  Attack drone: once 12 guns stand.
  Gate upgrades: never.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: never.
  The build order seed 3 took (3 stars, base 694):
  from wave  1: build Cannon at (29,6); build Flak Turret at (34,9)
  from wave  3: build Gun Turret at (29,5)
  from wave  9: upgrade the tower at (29,5); upgrade the tower at (34,9)
  from wave 10: build Gun Turret at (28,6)
  from wave 12: upgrade the tower at (28,6)
  from wave 13: build Gun Turret at (32,6); upgrade the tower at (32,6); build Gun Turret at (32,5)
  from wave 14: upgrade the tower at (32,5)
  from wave 15: build Gun Turret at (36,6)
  from wave 16: build Gun Turret at (30,9); upgrade the tower at (30,9); upgrade the tower at (36,6); build Gun Turret at (34,6); upgrade the tower at (34,6); upgrade the tower at (34,9); upgrade the tower at (32,5)
```

**Level_C1_04_1_strategy3** (won 3/3 seeds, 1.7 stars, base 351 left)

```
  Towers per threat: swarm: Laser Turret; pack: Gun Turret; plated: Gun Turret; air: Cannon; boss: whatever the ledger rates best.
  Gun Turret spots, best first: (29,5) (28,6) (29,6).
  Cannon spots, best first: (29,6) (32,6) (29,5).
  Laser Turret spots, best first: (28,5) (29,5) (29,6).
  Flak Turret spots, best first: (34,9) (33,9) (34,6).
  Upgrades: 1 guns down first, then an upgrade is worth 1.67x its value per coin against a new gun.
  Mines: not for bosses, never for crowds.
  Slow field: when the guns would not hold the wave (cover under 1.00) or the slow pays by the damage rule.
  Harvester: never.
  Repair drone: once a gun or gate has lost 5 % of its health.
  Attack drone: once 12 guns stand.
  Gate upgrades: never.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: never.
  The build order seed 3 took (2 stars, base 456):
  from wave  1: build Cannon at (29,6); build Flak Turret at (34,9)
  from wave  3: build Gun Turret at (29,5)
  from wave  6: upgrade the tower at (29,5)
  from wave  7: upgrade the tower at (29,5)
  from wave 10: build Gun Turret at (28,6)
  from wave 13: upgrade the tower at (28,6); build Gun Turret at (32,6); upgrade the tower at (32,6); build Gun Turret at (32,5)
  from wave 14: upgrade the tower at (32,5); build Gun Turret at (36,6)
  from wave 15: upgrade the tower at (36,6)
  from wave 16: build Gun Turret at (30,9); upgrade the tower at (30,9); build Gun Turret at (34,6); upgrade the tower at (34,6); upgrade the tower at (34,9); upgrade the tower at (34,9)
```


## Fault Line (Level_C1_04) on Hard

Best spots per tower: Gun Turret (29,5) (29,6) (28,6); Cannon (29,6) (32,6) (29,5); Laser Turret (28,5) (29,5) (29,6); Flak Turret (34,9) (33,9) (34,6).

**Level_C1_04_2_strategy1** (won 3/3 seeds, 3.0 stars, base 800 left, repair drone)

```
  Towers per threat: swarm: whatever the ledger rates best; pack: Laser Turret; plated: Laser Turret; air: Gun Turret; boss: Cannon.
  Gun Turret spots, best first: (28,6) (29,6) (29,5).
  Cannon spots, best first: (29,6) (32,6) (29,5).
  Laser Turret spots, best first: (29,5) (29,6) (28,5).
  Flak Turret spots, best first: (34,9) (33,9) (34,6).
  Upgrades: 1 guns down first, then an upgrade is worth 1.27x its value per coin against a new gun.
  Mines: not for bosses, and before a crowd wave when the guns would not hold it (cover under 1.00).
  Slow field: never.
  Harvester: in the first 60 % of the level, once the guns hold the waves at hand with cover 1.20.
  Repair drone: once a gun or gate has lost 5 % of its health.
  Attack drone: once 6 guns stand.
  Gate upgrades: never.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: never.
  The build order seed 2 took (3 stars, base 800):
  from wave  1: build Cannon at (29,6); build Flak Turret at (34,9)
  from wave  2: build Gun Turret at (28,6)
  from wave  7: buy the HarvestDrone; upgrade the tower at (34,9)
  from wave  8: upgrade the tower at (28,6)
  from wave  9: build Gun Turret at (29,5)
  from wave 10: upgrade the tower at (29,5); build Gun Turret at (30,9); upgrade the tower at (30,9)
  from wave 11: build Gun Turret at (32,6)
  from wave 13: upgrade the tower at (29,5); upgrade the tower at (32,6); build Gun Turret at (36,6); upgrade the tower at (36,6); build Gun Turret at (32,5); upgrade the tower at (32,5)
  from wave 14: upgrade the tower at (32,5); build Gun Turret at (34,6); upgrade the tower at (34,6); build Gun Turret at (33,6)
  from wave 15: upgrade the tower at (33,6); build Gun Turret at (31,9); upgrade the tower at (31,9)
  from wave 16: buy the RepairDrone; buy the AttackDrone; build Laser Turret at (28,5); build Laser Turret at (29,9); build Gun Turret at (33,5); upgrade the tower at (33,5); build Gun Turret at (32,9); upgrade the tower at (32,9); build Laser Turret at (35,9)
```

**Level_C1_04_2_strategy2** (won 3/3 seeds, 3.0 stars, base 800 left, repair drone)

```
  Towers per threat: swarm: whatever the ledger rates best; pack: Cannon; plated: whatever the ledger rates best; air: Cannon; boss: whatever the ledger rates best.
  Gun Turret spots, best first: (29,5) (29,6) (28,6).
  Cannon spots, best first: (29,6) (32,6) (32,5).
  Laser Turret spots, best first: (28,5) (29,5) (29,6).
  Flak Turret spots, best first: (34,9) (33,9) (34,6).
  Upgrades: 2 guns down first, then an upgrade is worth 1.66x its value per coin against a new gun.
  Mines: before every boss wave, and before a crowd wave when the guns would not hold it (cover under 1.00).
  Slow field: when the guns would not hold the wave (cover under 1.00) or the slow pays by the damage rule.
  Harvester: in the first 50 % of the level, once the guns hold the waves at hand with cover 1.20.
  Repair drone: once a gun or gate has lost 5 % of its health.
  Attack drone: once 12 guns stand.
  Gate upgrades: never.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: never.
  The build order seed 3 took (3 stars, base 800):
  from wave  1: build Cannon at (29,6); build Flak Turret at (34,9)
  from wave  2: build Gun Turret at (29,5)
  from wave  7: buy the HarvestDrone; upgrade the tower at (34,9)
  from wave  8: upgrade the tower at (29,5)
  from wave  9: build Gun Turret at (28,6)
  from wave 10: upgrade the tower at (28,6); build Gun Turret at (32,6); upgrade the tower at (32,6)
  from wave 11: build Gun Turret at (32,5)
  from wave 12: buy the RepairDrone
  from wave 13: upgrade the tower at (32,5); build Gun Turret at (30,9); upgrade the tower at (30,9); build Gun Turret at (36,6); upgrade the tower at (36,6)
  from wave 14: build Gun Turret at (34,6); upgrade the tower at (34,6); build Gun Turret at (33,6); upgrade the tower at (33,6)
  from wave 15: build Gun Turret at (31,9); upgrade the tower at (31,9); build Gun Turret at (33,5); upgrade the tower at (33,5)
  from wave 16: buy the AttackDrone; build Cannon at (34,10); build Cannon at (32,9); build Cannon at (31,10); build Cannon at (33,9); build Cannon at (32,10); build Cannon at (29,9); upgrade the tower at (32,5)
```


## Fault Line (Level_C1_04) on Psycho

Best spots per tower: Gun Turret (29,5) (29,6) (28,6); Cannon (29,6) (32,6) (29,5); Laser Turret (28,5) (29,5) (29,6); Flak Turret (34,9) (33,9) (34,6).

**Level_C1_04_3_strategy2** (won 3/3 seeds, 2.3 stars, base 627 left, repair drone)

```
  Towers per threat: swarm: Cannon; pack: whatever the ledger rates best; plated: Gun Turret; air: whatever the ledger rates best; boss: whatever the ledger rates best.
  Gun Turret spots, best first: (29,6) (28,6) (32,6).
  Cannon spots, best first: (32,6) (29,6) (29,5).
  Laser Turret spots, best first: (29,5) (28,5) (29,6).
  Flak Turret spots, best first: (33,9) (34,10) (34,9).
  Upgrades: 2 guns down first, then an upgrade is worth 1.07x its value per coin against a new gun.
  Mines: not for bosses, and before a crowd wave when the guns would not hold it (cover under 1.00).
  Slow field: when the guns would not hold the wave (cover under 1.20) or the slow pays by the damage rule.
  Harvester: in the first 60 % of the level, once the guns hold the waves at hand with cover 1.20.
  Repair drone: once a gun or gate has lost 10 % of its health.
  Attack drone: never.
  Gate upgrades: the defence gate's gun and health, weighed at 1.20x against a tower step.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: the weakest ground gun may go for a sky gun the preview asks for (Psycho).
  The build order seed 3 took (3 stars, base 778):
  from wave  1: build Cannon at (32,6); build Flak Turret at (33,9); upgrade the defence gate; build Cannon at (29,6); buy the HarvestDrone; build Cannon at (29,5); build Flak Turret at (34,10)
  from wave  3: buy the RepairDrone
  from wave  4: build Gun Turret at (28,6)
  from wave  5: upgrade the tower at (28,6); build Gun Turret at (30,9)
  from wave  6: sell the tower at (30,9); build Flak Turret at (34,9)
  from wave  7: upgrade the tower at (33,9)
  from wave  9: build Gun Turret at (30,9); upgrade the tower at (30,9)
  from wave 10: build Gun Turret at (36,6); upgrade the tower at (36,6); upgrade the tower at (30,9); build Cannon at (32,5)
  from wave 11: upgrade the tower at (32,5)
  from wave 12: sell the tower at (32,6); build Flak Turret at (34,6)
  from wave 13: upgrade the tower at (28,6); build Gun Turret at (32,6); upgrade the tower at (32,6); build Gun Turret at (33,6); upgrade the tower at (33,6); build Gun Turret at (31,9); upgrade the tower at (31,9)
  from wave 14: build Gun Turret at (33,5); upgrade the tower at (33,5); build Gun Turret at (32,9)
  from wave 15: upgrade the tower at (32,9); build Gun Turret at (29,9)
  from wave 16: upgrade the tower at (32,5); upgrade the tower at (29,9); build Gun Turret at (28,5); upgrade the tower at (28,5); build Gun Turret at (31,10); upgrade the tower at (31,10); build Gun Turret at (32,10); upgrade the tower at (32,10); upgrade the tower at (34,10); upgrade the tower at (29,5); build Gun Turret at (33,10)
```

**Level_C1_04_3_strategy1** (won 3/3 seeds, 2.0 stars, base 597 left, repair drone)

```
  Towers per threat: swarm: whatever the ledger rates best; pack: whatever the ledger rates best; plated: whatever the ledger rates best; air: whatever the ledger rates best; boss: Cannon.
  Gun Turret spots, best first: (29,5) (29,6) (32,6).
  Cannon spots, best first: (29,6) (32,6) (29,5).
  Laser Turret spots, best first: (28,5) (29,6) (29,5).
  Flak Turret spots, best first: (34,9) (33,9) (34,6).
  Upgrades: 4 guns down first, then an upgrade is worth 0.71x its value per coin against a new gun.
  Mines: not for bosses, and before a crowd wave when the guns would not hold it (cover under 1.00).
  Slow field: when the guns would not hold the wave (cover under 1.00) or the slow pays by the damage rule.
  Harvester: in the first 30 % of the level, once the guns hold the waves at hand with cover 1.20.
  Repair drone: once a gun or gate has lost 10 % of its health.
  Attack drone: never.
  Gate upgrades: the defence gate's gun and health, weighed at 1.20x against a tower step.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: the weakest ground gun may go for a sky gun the preview asks for (Psycho).
  The build order seed 3 took (2 stars, base 699):
  from wave  1: build Cannon at (29,6); build Flak Turret at (34,9); build Cannon at (32,6); buy the HarvestDrone; build Cannon at (29,5); build Flak Turret at (33,9); upgrade the defence gate
  from wave  3: buy the RepairDrone
  from wave  4: build Gun Turret at (34,6)
  from wave  5: upgrade the tower at (34,6); build Gun Turret at (28,6)
  from wave  6: sell the tower at (28,6); build Flak Turret at (34,10)
  from wave  7: upgrade the tower at (34,9)
  from wave  9: build Gun Turret at (28,6); upgrade the tower at (28,6)
  from wave 10: build Gun Turret at (32,5); build Cannon at (30,9); upgrade the tower at (32,5)
  from wave 11: build Gun Turret at (36,6)
  from wave 12: sell the tower at (36,6); build Flak Turret at (31,9); build Gun Turret at (36,6)
  from wave 13: upgrade the tower at (36,6); build Gun Turret at (33,6); upgrade the tower at (33,6); build Gun Turret at (33,5); upgrade the tower at (33,5); build Gun Turret at (32,9); upgrade the tower at (32,9)
  from wave 14: build Gun Turret at (29,9); upgrade the tower at (29,9); build Gun Turret at (28,5)
  from wave 15: upgrade the tower at (28,5); build Gun Turret at (31,10); upgrade the tower at (31,10)
  from wave 16: upgrade the tower at (32,6); upgrade the tower at (32,5); build Gun Turret at (32,10); upgrade the tower at (32,10); build Gun Turret at (33,10); upgrade the tower at (33,10); build Gun Turret at (29,10); upgrade the tower at (29,10); upgrade the tower at (30,9); upgrade the tower at (33,9); build Gun Turret at (28,9)
```


## Quartz Flats (Level_C1_05) on Easy

Best spots per tower: Gun Turret (7,11) (7,13) (3,15); Cannon (3,11) (3,15) (3,16); Laser Turret (6,12) (6,13) (6,14); Flak Turret (34,3) (34,4) (33,3).

**Level_C1_05_0_strategy2** (won 3/3 seeds, 3.0 stars, base 700 left, repair drone)

```
  Towers per threat: swarm: whatever the ledger rates best; pack: whatever the ledger rates best; plated: Cannon; air: whatever the ledger rates best; boss: Cannon.
  Gun Turret spots, best first: (7,11) (7,13) (3,15).
  Cannon spots, best first: (3,11) (7,11) (3,15).
  Laser Turret spots, best first: (6,12) (6,13) (6,14).
  Flak Turret spots, best first: (34,3) (34,4) (33,3).
  Upgrades: 4 guns down first, then an upgrade is worth 0.86x its value per coin against a new gun.
  Mines: before every boss wave, and before a crowd wave when the guns would not hold it (cover under 1.00).
  Slow field: never.
  Harvester: never.
  Repair drone: once a gun or gate has lost 10 % of its health.
  Attack drone: once 12 guns stand.
  Gate upgrades: the defence gate's gun and health, weighed at 1.20x against a tower step.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: never.
  The build order seed 3 took (3 stars, base 700):
  from wave  1: build Cannon at (3,11); build Flak Turret at (34,3)
  from wave  3: build Gun Turret at (7,11)
  from wave  6: build Gun Turret at (7,13); upgrade the tower at (7,11)
  from wave  7: upgrade the tower at (7,13)
  from wave 10: upgrade the defence gate; build Gun Turret at (3,15)
  from wave 12: build Flak Turret at (34,4); upgrade the tower at (7,11)
  from wave 14: upgrade the tower at (3,11)
  from wave 15: build Cannon at (20,6)
  from wave 16: build Cannon at (6,11); upgrade the tower at (3,15); upgrade the tower at (3,15)
  from wave 17: upgrade the tower at (7,13); buy the RepairDrone
```

**Level_C1_05_0_strategy5** (won 3/3 seeds, 3.0 stars, base 700 left, repair drone)

```
  Towers per threat: swarm: Gun Turret; pack: Laser Turret; plated: Cannon; air: Gun Turret; boss: Laser Turret.
  Gun Turret spots, best first: (7,13) (7,11) (3,15).
  Cannon spots, best first: (3,11) (7,11) (3,15).
  Laser Turret spots, best first: (6,12) (6,13) (6,14).
  Flak Turret spots, best first: (34,3) (34,4) (33,3).
  Upgrades: 4 guns down first, then an upgrade is worth 1.18x its value per coin against a new gun.
  Mines: before every boss wave, and before a crowd wave when the guns would not hold it (cover under 1.00).
  Slow field: when the guns would not hold the wave (cover under 1.00) or the slow pays by the damage rule.
  Harvester: in the first 40 % of the level, once the guns hold the waves at hand with cover 1.20.
  Repair drone: once a gun or gate has lost 5 % of its health.
  Attack drone: once 8 guns stand.
  Gate upgrades: the defence gate's gun and health, weighed at 1.20x against a tower step.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: never.
  The build order seed 3 took (3 stars, base 700):
  from wave  1: build Cannon at (3,11); build Flak Turret at (34,3)
  from wave  3: build Gun Turret at (7,13)
  from wave  8: buy the HarvestDrone
  from wave  9: build Gun Turret at (7,11)
  from wave 10: buy the RepairDrone
  from wave 11: build Flak Turret at (34,4)
  from wave 12: upgrade the defence gate; upgrade the tower at (7,11)
  from wave 13: upgrade the tower at (7,13)
  from wave 14: build Gun Turret at (3,15); upgrade the tower at (3,11)
  from wave 15: upgrade the tower at (7,11)
  from wave 16: upgrade the tower at (7,13); upgrade the tower at (3,15); upgrade the tower at (3,15)
  from wave 17: build Gun Turret at (7,10); upgrade the tower at (7,10); upgrade the tower at (7,10); build Gun Turret at (6,11)
```


## Quartz Flats (Level_C1_05) on Medium

Best spots per tower: Gun Turret (7,11) (7,13) (3,15); Cannon (3,11) (3,15) (3,16); Laser Turret (6,12) (6,13) (6,14); Flak Turret (34,3) (34,4) (33,3).

**Level_C1_05_1_strategy5** (won 3/3 seeds, 3.0 stars, base 700 left, repair drone)

```
  Towers per threat: swarm: Cannon; pack: whatever the ledger rates best; plated: Cannon; air: whatever the ledger rates best; boss: whatever the ledger rates best.
  Gun Turret spots, best first: (7,11) (7,13) (3,15).
  Cannon spots, best first: (3,11) (3,15) (3,16).
  Laser Turret spots, best first: (6,12) (6,13) (3,15).
  Flak Turret spots, best first: (34,3) (34,4) (33,3).
  Upgrades: 2 guns down first, then an upgrade is worth 0.66x its value per coin against a new gun.
  Mines: not for bosses, never for crowds.
  Slow field: when the guns would not hold the wave (cover under 1.00) or the slow pays by the damage rule.
  Harvester: in the first 60 % of the level, once the guns hold the waves at hand with cover 1.20.
  Repair drone: once a gun or gate has lost 10 % of its health.
  Attack drone: once 12 guns stand.
  Gate upgrades: never.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: never.
  The build order seed 1 took (3 stars, base 700):
  from wave  1: build Cannon at (3,11); build Flak Turret at (34,3); build Gun Turret at (7,11)
  from wave  7: buy the HarvestDrone; upgrade the tower at (7,11)
  from wave  9: build Cannon at (3,15)
  from wave 10: upgrade the tower at (7,11)
  from wave 11: build Flak Turret at (34,4); build Cannon at (3,16)
  from wave 12: build Cannon at (6,11)
  from wave 13: build Cannon at (3,10)
  from wave 14: build Flak Turret at (33,3)
  from wave 15: buy the RepairDrone; upgrade the tower at (6,11)
  from wave 16: build Gun Turret at (7,13); upgrade the tower at (7,13); build Gun Turret at (7,10)
  from wave 17: upgrade the tower at (7,10); upgrade the tower at (7,10); build Cannon at (20,6)
```

**Level_C1_05_1_strategy4** (won 3/3 seeds, 3.0 stars, base 700 left, repair drone)

```
  Towers per threat: swarm: Cannon; pack: whatever the ledger rates best; plated: Laser Turret; air: whatever the ledger rates best; boss: whatever the ledger rates best.
  Gun Turret spots, best first: (7,11) (7,13) (3,15).
  Cannon spots, best first: (7,11) (3,11) (3,15).
  Laser Turret spots, best first: (6,12) (6,13) (6,11).
  Flak Turret spots, best first: (34,3) (34,4) (33,3).
  Upgrades: 3 guns down first, then an upgrade is worth 1.45x its value per coin against a new gun.
  Mines: not for bosses, and before a crowd wave when the guns would not hold it (cover under 1.30).
  Slow field: when the guns would not hold the wave (cover under 1.00) or the slow pays by the damage rule.
  Harvester: in the first 40 % of the level, once the guns hold the waves at hand with cover 1.50.
  Repair drone: once a gun or gate has lost 10 % of its health.
  Attack drone: never.
  Gate upgrades: the defence gate's gun and health, weighed at 0.80x against a tower step.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: never.
  The build order seed 1 took (3 stars, base 700):
  from wave  1: build Cannon at (7,11); build Flak Turret at (34,3); build Gun Turret at (7,13)
  from wave  7: buy the HarvestDrone
  from wave  8: upgrade the defence gate
  from wave  9: upgrade the tower at (7,11)
  from wave 10: upgrade the tower at (7,13); upgrade the tower at (7,13)
  from wave 11: build Cannon at (3,11)
  from wave 12: build Flak Turret at (34,4)
  from wave 13: build Gun Turret at (3,15); upgrade the tower at (3,15)
  from wave 14: build Gun Turret at (3,16); build Flak Turret at (33,3)
  from wave 15: buy the RepairDrone; upgrade the tower at (3,11)
  from wave 16: upgrade the tower at (3,16); upgrade the tower at (3,15)
  from wave 17: upgrade the tower at (3,16); build Cannon at (6,11); upgrade the tower at (6,11)
```


## Quartz Flats (Level_C1_05) on Hard

Best spots per tower: Gun Turret (7,11) (7,13) (3,15); Cannon (3,11) (3,15) (3,16); Laser Turret (6,12) (6,13) (6,14); Flak Turret (34,3) (34,4) (33,3).

**Level_C1_05_2_strategy1** (won 3/3 seeds, 2.3 stars, base 671 left, repair drone)

```
  Towers per threat: swarm: Laser Turret; pack: Cannon; plated: Gun Turret; air: Laser Turret; boss: Laser Turret.
  Gun Turret spots, best first: (3,16) (7,13) (7,11).
  Cannon spots, best first: (3,11) (3,15) (3,16).
  Laser Turret spots, best first: (6,13) (6,12) (6,14).
  Flak Turret spots, best first: (34,3) (34,4) (33,3).
  Upgrades: 2 guns down first, then an upgrade is worth 1.72x its value per coin against a new gun.
  Mines: before every boss wave, never for crowds.
  Slow field: when the guns would not hold the wave (cover under 1.20) or the slow pays by the damage rule.
  Harvester: in the first 40 % of the level, once the guns hold the waves at hand with cover 1.50.
  Repair drone: once a gun or gate has lost 5 % of its health.
  Attack drone: never.
  Gate upgrades: never.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: never.
  The build order seed 3 took (3 stars, base 736):
  from wave  1: build Cannon at (3,11); build Flak Turret at (34,3); build Gun Turret at (3,16)
  from wave  4: upgrade the tower at (3,11)
  from wave  7: buy the HarvestDrone; upgrade the tower at (34,3)
  from wave  9: upgrade the tower at (3,16); build Cannon at (3,15)
  from wave 10: build Flak Turret at (34,4); buy the RepairDrone
  from wave 11: upgrade the tower at (34,4)
  from wave 12: build Cannon at (7,11); upgrade the tower at (7,11)
  from wave 13: upgrade the tower at (3,15); build Gun Turret at (7,13); upgrade the tower at (7,13)
  from wave 14: build Gun Turret at (6,11); upgrade the tower at (6,11); upgrade the tower at (7,11); upgrade the tower at (6,11); upgrade the tower at (3,11); upgrade the tower at (7,13); upgrade the tower at (3,16)
  from wave 15: build Gun Turret at (7,10); upgrade the tower at (34,3); upgrade the tower at (7,10)
  from wave 16: upgrade the tower at (7,10); build Cannon at (3,10)
  from wave 17: upgrade the tower at (3,10); upgrade the tower at (3,10); upgrade the tower at (3,15); build Cannon at (20,6)
```

**Level_C1_05_2_strategy3** (won 3/3 seeds, 2.3 stars, base 578 left, repair drone)

```
  Towers per threat: swarm: Laser Turret; pack: Laser Turret; plated: whatever the ledger rates best; air: whatever the ledger rates best; boss: Cannon.
  Gun Turret spots, best first: (3,16) (7,11) (3,15).
  Cannon spots, best first: (3,11) (3,15) (3,16).
  Laser Turret spots, best first: (6,12) (6,13) (6,14).
  Flak Turret spots, best first: (34,3) (34,4) (32,3).
  Upgrades: 1 guns down first, then an upgrade is worth 1.55x its value per coin against a new gun.
  Mines: before every boss wave, and before a crowd wave when the guns would not hold it (cover under 1.30).
  Slow field: when the guns would not hold the wave (cover under 1.00) or the slow pays by the damage rule.
  Harvester: in the first 40 % of the level, once the guns hold the waves at hand with cover 1.20.
  Repair drone: once a gun or gate has lost 50 % of its health.
  Attack drone: never.
  Gate upgrades: never.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: never.
  The build order seed 1 took (3 stars, base 784):
  from wave  1: build Cannon at (3,11); build Flak Turret at (34,3); build Gun Turret at (3,16)
  from wave  4: upgrade the tower at (3,11)
  from wave  7: buy the HarvestDrone; upgrade the tower at (34,3)
  from wave  9: upgrade the tower at (3,16); build Cannon at (3,15)
  from wave 10: build Flak Turret at (34,4); upgrade the tower at (34,4)
  from wave 11: upgrade the tower at (34,3); build Gun Turret at (7,11)
  from wave 12: buy the RepairDrone; upgrade the tower at (7,11); upgrade the tower at (7,11)
  from wave 13: build Gun Turret at (6,11); upgrade the tower at (6,11); build Gun Turret at (7,13)
  from wave 14: upgrade the tower at (7,13); build Gun Turret at (7,10); upgrade the tower at (7,10); build Gun Turret at (20,6); upgrade the tower at (20,6); upgrade the tower at (3,11); upgrade the tower at (7,10)
  from wave 15: upgrade the tower at (6,11); upgrade the tower at (7,13); upgrade the tower at (3,15)
  from wave 16: build Gun Turret at (34,0); upgrade the tower at (34,0); upgrade the tower at (3,16)
  from wave 17: build Cannon at (3,10); upgrade the tower at (3,10); build Cannon at (8,10); upgrade the tower at (8,10); build Cannon at (8,7)
```


## Quartz Flats (Level_C1_05) on Psycho

Best spots per tower: Gun Turret (7,11) (7,13) (3,15); Cannon (3,11) (3,15) (3,16); Laser Turret (6,12) (6,13) (6,14); Flak Turret (34,3) (34,4) (33,3).

**Level_C1_05_3_strategy2** (won 3/3 seeds, 3.0 stars, base 889 left, repair drone)

```
  Towers per threat: swarm: whatever the ledger rates best; pack: Cannon; plated: Laser Turret; air: whatever the ledger rates best; boss: Cannon.
  Gun Turret spots, best first: (7,13) (7,11) (3,15).
  Cannon spots, best first: (3,10) (3,15) (3,16).
  Laser Turret spots, best first: (6,12) (6,13) (6,14).
  Flak Turret spots, best first: (34,3) (34,4) (35,3).
  Upgrades: 4 guns down first, then an upgrade is worth 1.66x its value per coin against a new gun.
  Mines: before every boss wave, and before a crowd wave when the guns would not hold it (cover under 1.30).
  Slow field: when the guns would not hold the wave (cover under 1.00) or the slow pays by the damage rule.
  Harvester: in the first 30 % of the level, once the guns hold the waves at hand with cover 1.00.
  Repair drone: once a gun or gate has lost 5 % of its health.
  Attack drone: once 12 guns stand.
  Gate upgrades: never.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: the weakest ground gun may go for a sky gun the preview asks for (Psycho).
  The build order seed 3 took (3 stars, base 900):
  from wave  1: build Cannon at (3,10); build Flak Turret at (34,3); build Cannon at (3,15); buy the HarvestDrone; build Cannon at (3,16); build Flak Turret at (34,4); build Cannon at (7,11); build Gun Turret at (7,13); upgrade the tower at (7,13)
  from wave  3: upgrade the tower at (7,13)
  from wave  4: upgrade the tower at (7,11)
  from wave  5: upgrade the tower at (3,10)
  from wave  6: buy the RepairDrone; upgrade the tower at (34,3)
  from wave  7: build Gun Turret at (7,10); upgrade the tower at (34,4); upgrade the tower at (7,10)
  from wave  8: upgrade the tower at (3,15)
  from wave  9: sell the tower at (3,16); build Flak Turret at (35,3)
  from wave 10: upgrade the tower at (34,3); upgrade the tower at (35,3)
  from wave 11: upgrade the tower at (34,4); upgrade the tower at (7,10)
  from wave 12: build Cannon at (3,16); upgrade the tower at (3,16); build Cannon at (6,11)
  from wave 13: build Gun Turret at (34,0); upgrade the tower at (34,0)
  from wave 14: build Gun Turret at (20,6); upgrade the tower at (20,6); build Gun Turret at (8,10); upgrade the tower at (8,10); build Gun Turret at (8,7); upgrade the tower at (8,7); buy the AttackDrone; upgrade the tower at (6,11)
  from wave 15: upgrade the tower at (7,11); upgrade the tower at (6,11)
  from wave 16: upgrade the tower at (8,7); upgrade the tower at (8,10)
  from wave 17: upgrade the tower at (3,10); upgrade the tower at (3,15); upgrade the tower at (3,16); upgrade the tower at (20,6); build Cannon at (3,11)
```

**Level_C1_05_3_strategy3** (won 3/3 seeds, 3.0 stars, base 874 left, repair drone)

```
  Towers per threat: swarm: Laser Turret; pack: Laser Turret; plated: whatever the ledger rates best; air: whatever the ledger rates best; boss: Cannon.
  Gun Turret spots, best first: (7,11) (7,13) (7,10).
  Cannon spots, best first: (3,15) (7,11) (3,16).
  Laser Turret spots, best first: (6,14) (6,12) (6,13).
  Flak Turret spots, best first: (34,3) (34,4) (33,3).
  Upgrades: 2 guns down first, then an upgrade is worth 1.16x its value per coin against a new gun.
  Mines: not for bosses, and before a crowd wave when the guns would not hold it (cover under 1.00).
  Slow field: when the guns would not hold the wave (cover under 1.20) or the slow pays by the damage rule.
  Harvester: in the first 30 % of the level, once the guns hold the waves at hand with cover 1.20.
  Repair drone: once a gun or gate has lost 5 % of its health.
  Attack drone: never.
  Gate upgrades: never.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: the weakest ground gun may go for a sky gun the preview asks for (Psycho).
  The build order seed 1 took (3 stars, base 900):
  from wave  1: build Cannon at (3,15); build Flak Turret at (34,3); build Cannon at (7,11); buy the HarvestDrone; build Laser Turret at (6,14); build Flak Turret at (34,4); upgrade the tower at (6,14); build Gun Turret at (7,13); upgrade the tower at (7,13)
  from wave  2: upgrade the tower at (7,13)
  from wave  4: upgrade the tower at (6,14)
  from wave  5: upgrade the tower at (7,11)
  from wave  6: buy the RepairDrone; upgrade the tower at (34,3); build Gun Turret at (7,10)
  from wave  7: upgrade the tower at (34,4); upgrade the tower at (7,10)
  from wave  8: upgrade the tower at (3,15)
  from wave  9: sell the tower at (7,10); build Flak Turret at (33,3)
  from wave 10: upgrade the tower at (34,3); upgrade the tower at (33,3)
  from wave 11: build Gun Turret at (7,10); upgrade the tower at (34,4); upgrade the tower at (7,10)
  from wave 12: upgrade the tower at (7,10); build Cannon at (3,16); upgrade the tower at (3,16)
  from wave 13: build Gun Turret at (6,11); upgrade the tower at (6,11)
  from wave 14: build Gun Turret at (20,6); upgrade the tower at (20,6); build Gun Turret at (34,0); upgrade the tower at (34,0); upgrade the tower at (7,11); upgrade the tower at (3,15)
  from wave 15: upgrade the tower at (6,11); upgrade the tower at (3,16)
  from wave 16: upgrade the tower at (20,6)
  from wave 17: build Cannon at (3,11); upgrade the tower at (3,11); upgrade the tower at (3,11); build Cannon at (3,10); upgrade the tower at (3,10)
```


## Crysium Vein (Level_C1_06) on Easy

Best spots per tower: Gun Turret (12,10) (12,11) (13,10); Cannon (12,10) (13,10) (14,7); Laser Turret (13,10) (11,10) (12,10); Flak Turret (36,10) (36,7) (36,11).

**Level_C1_06_0_strategy3** (won 3/3 seeds, 3.0 stars, base 700 left, repair drone)

```
  Towers per threat: swarm: whatever the ledger rates best; pack: whatever the ledger rates best; plated: Gun Turret; air: Cannon; boss: Laser Turret.
  Gun Turret spots, best first: (13,11) (12,10) (12,11).
  Cannon spots, best first: (12,10) (13,10) (14,7).
  Laser Turret spots, best first: (13,10) (11,10) (12,10).
  Flak Turret spots, best first: (36,10) (36,7) (36,11).
  Upgrades: 4 guns down first, then an upgrade is worth 1.72x its value per coin against a new gun.
  Mines: before every boss wave, and before a crowd wave when the guns would not hold it (cover under 1.00).
  Slow field: when the guns would not hold the wave (cover under 1.00) or the slow pays by the damage rule.
  Harvester: in the first 30 % of the level, once the guns hold the waves at hand with cover 1.20.
  Repair drone: once a gun or gate has lost 25 % of its health.
  Attack drone: once 8 guns stand.
  Gate upgrades: the defence gate's gun and health, weighed at 1.20x against a tower step.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: never.
  The build order seed 2 took (3 stars, base 700):
  from wave  1: build Cannon at (12,10); build Flak Turret at (36,10)
  from wave  4: build Gun Turret at (13,11)
  from wave  8: buy the HarvestDrone
  from wave 10: build Gun Turret at (12,11)
  from wave 11: buy the RepairDrone
  from wave 13: upgrade the tower at (13,11); lay a Mine on the road at (5,15); lay a Mine on the road at (5,15)
  from wave 14: lay a Mine on the road at (5,15); lay a Mine on the road at (36,15)
  from wave 15: upgrade the defence gate; upgrade the tower at (12,11)
  from wave 16: build Gun Turret at (13,10); upgrade the tower at (13,10); lay a Mine on the road at (5,15); lay a Mine on the road at (5,15)
  from wave 17: upgrade the tower at (13,10); lay a Mine on the road at (5,15); lay a Mine on the road at (36,15)
  from wave 18: lay a Mine on the road at (36,15); upgrade the tower at (13,11); upgrade the tower at (12,11); lay a Mine on the road at (5,15); upgrade the defence gate
```

**Level_C1_06_0_strategy1** (won 3/3 seeds, 3.0 stars, base 700 left)

```
  Towers per threat: swarm: Cannon; pack: whatever the ledger rates best; plated: whatever the ledger rates best; air: Cannon; boss: whatever the ledger rates best.
  Gun Turret spots, best first: (12,10) (12,11) (13,10).
  Cannon spots, best first: (12,10) (14,7) (13,10).
  Laser Turret spots, best first: (13,10) (11,10) (12,10).
  Flak Turret spots, best first: (36,10) (36,7) (36,11).
  Upgrades: 4 guns down first, then an upgrade is worth 1.60x its value per coin against a new gun.
  Mines: before every boss wave, and before a crowd wave when the guns would not hold it (cover under 1.00).
  Slow field: when the guns would not hold the wave (cover under 1.00) or the slow pays by the damage rule.
  Harvester: never.
  Repair drone: never.
  Attack drone: once 8 guns stand.
  Gate upgrades: the defence gate's gun and health, weighed at 1.20x against a tower step.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: never.
  The build order seed 1 took (3 stars, base 700):
  from wave  1: build Cannon at (12,10); build Flak Turret at (36,10)
  from wave  4: build Gun Turret at (12,11)
  from wave  7: build Gun Turret at (13,10); upgrade the tower at (13,10)
  from wave 10: upgrade the defence gate; upgrade the tower at (12,11)
  from wave 11: upgrade the tower at (36,10)
  from wave 13: lay a Mine on the road at (5,15); lay a Mine on the road at (36,15)
  from wave 14: lay a Mine on the road at (5,15)
  from wave 15: upgrade the tower at (13,10); upgrade the tower at (12,11)
  from wave 16: build Gun Turret at (13,11); build Gun Turret at (14,7)
  from wave 17: lay a Mine on the road at (5,15); lay a Mine on the road at (36,15); lay a SlowField on the road at (41,9); build Cannon at (35,11); upgrade the tower at (14,7)
  from wave 18: upgrade the tower at (13,11); upgrade the tower at (14,7); lay a Mine on the road at (5,15); lay a Mine on the road at (36,15); upgrade the tower at (13,11)
```


## Crysium Vein (Level_C1_06) on Medium

Best spots per tower: Gun Turret (12,10) (12,11) (13,10); Cannon (12,10) (13,10) (14,7); Laser Turret (13,10) (11,10) (12,10); Flak Turret (36,10) (36,7) (36,11).

**Level_C1_06_1_strategy3** (won 3/3 seeds, 3.0 stars, base 700 left, repair drone)

```
  Towers per threat: swarm: whatever the ledger rates best; pack: Cannon; plated: Laser Turret; air: Cannon; boss: Gun Turret.
  Gun Turret spots, best first: (12,10) (12,11) (13,11).
  Cannon spots, best first: (14,7) (12,11) (12,10).
  Laser Turret spots, best first: (11,10) (13,11) (13,10).
  Flak Turret spots, best first: (36,10) (36,7) (35,10).
  Upgrades: 2 guns down first, then an upgrade is worth 1.53x its value per coin against a new gun.
  Mines: not for bosses, never for crowds.
  Slow field: never.
  Harvester: in the first 40 % of the level, once the guns hold the waves at hand with cover 1.00.
  Repair drone: once a gun or gate has lost 50 % of its health.
  Attack drone: once 6 guns stand.
  Gate upgrades: never.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: never.
  The build order seed 2 took (3 stars, base 700):
  from wave  1: build Cannon at (14,7); build Flak Turret at (36,10)
  from wave  3: upgrade the tower at (14,7)
  from wave  6: build Gun Turret at (12,10)
  from wave 10: buy the HarvestDrone; upgrade the tower at (12,10)
  from wave 11: upgrade the tower at (36,10)
  from wave 12: build Gun Turret at (12,11)
  from wave 13: build Flak Turret at (36,7)
  from wave 14: upgrade the tower at (12,11); build Gun Turret at (13,11)
  from wave 15: upgrade the tower at (13,11); upgrade the tower at (12,10); build Gun Turret at (14,11)
  from wave 16: upgrade the tower at (14,11); buy the RepairDrone
  from wave 17: upgrade the tower at (14,11); build Gun Turret at (13,10)
  from wave 18: upgrade the tower at (13,10); build Gun Turret at (21,11); upgrade the tower at (21,11); build Gun Turret at (36,11); upgrade the tower at (36,11); build Gun Turret at (14,10); upgrade the tower at (14,10); buy the AttackDrone
```

**Level_C1_06_1_strategy2** (won 3/3 seeds, 3.0 stars, base 700 left, repair drone)

```
  Towers per threat: swarm: Gun Turret; pack: whatever the ledger rates best; plated: Laser Turret; air: whatever the ledger rates best; boss: Gun Turret.
  Gun Turret spots, best first: (12,10) (12,11) (13,10).
  Cannon spots, best first: (13,10) (12,10) (12,11).
  Laser Turret spots, best first: (13,10) (11,10) (12,10).
  Flak Turret spots, best first: (36,10) (36,7) (36,11).
  Upgrades: 2 guns down first, then an upgrade is worth 0.72x its value per coin against a new gun.
  Mines: not for bosses, and before a crowd wave when the guns would not hold it (cover under 1.00).
  Slow field: when the guns would not hold the wave (cover under 1.00) or the slow pays by the damage rule.
  Harvester: in the first 40 % of the level, once the guns hold the waves at hand with cover 1.00.
  Repair drone: once a gun or gate has lost 25 % of its health.
  Attack drone: never.
  Gate upgrades: the defence gate's gun and health, weighed at 1.20x against a tower step.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: never.
  The build order seed 1 took (3 stars, base 700):
  from wave  1: build Cannon at (13,10); build Flak Turret at (36,10)
  from wave  2: upgrade the defence gate
  from wave  5: build Gun Turret at (12,10)
  from wave 10: buy the HarvestDrone; upgrade the tower at (12,10)
  from wave 11: build Gun Turret at (12,11)
  from wave 12: upgrade the tower at (12,11); build Gun Turret at (13,11)
  from wave 13: build Gun Turret at (14,7); upgrade the tower at (14,7)
  from wave 14: upgrade the tower at (13,11); build Gun Turret at (14,11)
  from wave 15: upgrade the tower at (14,11); build Gun Turret at (21,11); upgrade the tower at (21,11)
  from wave 16: lay a Mine on the road at (5,15); lay a Mine on the road at (36,15); lay a SlowField on the road at (41,9); buy the RepairDrone
  from wave 17: build Gun Turret at (36,11); upgrade the tower at (36,11)
  from wave 18: build Gun Turret at (14,10); upgrade the tower at (14,10); build Gun Turret at (32,10); upgrade the tower at (32,10); build Gun Turret at (15,10); build Gun Turret at (31,10); lay a Mine on the road at (5,15); lay a Mine on the road at (36,15); lay a SlowField on the road at (41,9); upgrade the tower at (13,10)
```


## Crysium Vein (Level_C1_06) on Hard

Best spots per tower: Gun Turret (12,10) (12,11) (13,10); Cannon (12,10) (13,10) (14,7); Laser Turret (13,10) (11,10) (12,10); Flak Turret (36,10) (36,7) (36,11).

**Level_C1_06_2_strategy3** (won 3/3 seeds, 2.0 stars, base 568 left, repair drone)

```
  Towers per threat: swarm: Cannon; pack: Cannon; plated: Laser Turret; air: whatever the ledger rates best; boss: whatever the ledger rates best.
  Gun Turret spots, best first: (12,10) (12,11) (13,10).
  Cannon spots, best first: (13,10) (12,10) (14,11).
  Laser Turret spots, best first: (13,10) (11,10) (12,10).
  Flak Turret spots, best first: (36,10) (36,11) (36,7).
  Upgrades: 2 guns down first, then an upgrade is worth 1.12x its value per coin against a new gun.
  Mines: not for bosses, never for crowds.
  Slow field: when the guns would not hold the wave (cover under 1.00) or the slow pays by the damage rule.
  Harvester: in the first 60 % of the level, once the guns hold the waves at hand with cover 1.50.
  Repair drone: once a gun or gate has lost 10 % of its health.
  Attack drone: once 8 guns stand.
  Gate upgrades: the defence gate's gun and health, weighed at 1.20x against a tower step.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: never.
  The build order seed 2 took (3 stars, base 771):
  from wave  1: build Cannon at (13,10); build Flak Turret at (36,10)
  from wave  2: upgrade the defence gate
  from wave  4: build Gun Turret at (12,10)
  from wave  7: buy the HarvestDrone
  from wave  8: upgrade the tower at (12,10); build Gun Turret at (12,11)
  from wave  9: upgrade the tower at (12,11); upgrade the tower at (12,10)
  from wave 10: build Gun Turret at (13,11); upgrade the tower at (13,11)
  from wave 11: build Gun Turret at (14,7); upgrade the tower at (14,7)
  from wave 12: build Gun Turret at (14,11); upgrade the tower at (14,11); build Gun Turret at (21,11); build Gun Turret at (36,11); upgrade the tower at (36,11)
  from wave 13: upgrade the tower at (21,11); build Gun Turret at (14,10); upgrade the tower at (14,10)
  from wave 14: build Gun Turret at (32,10); upgrade the tower at (32,10)
  from wave 15: build Gun Turret at (15,10); upgrade the tower at (15,10); upgrade the tower at (32,10); upgrade the tower at (36,10); upgrade the tower at (14,10)
  from wave 16: lay a SlowField on the road at (41,9); buy the RepairDrone; buy the AttackDrone
  from wave 17: build Gun Turret at (31,10); upgrade the tower at (31,10); build Gun Turret at (15,11); upgrade the tower at (15,11)
  from wave 18: build Gun Turret at (16,10); upgrade the tower at (16,10); build Gun Turret at (32,11); upgrade the tower at (32,11); build Gun Turret at (15,7); upgrade the tower at (15,7); build Gun Turret at (16,11); upgrade the tower at (16,11); build Gun Turret at (14,6); upgrade the tower at (14,6); lay a SlowField on the road at (41,9); upgrade the tower at (13,10); upgrade the tower at (31,10)
```

**Level_C1_06_2_strategy1** (won 2/3 seeds, 1.5 stars, base 436 left)

```
  Towers per threat: swarm: whatever the ledger rates best; pack: Cannon; plated: whatever the ledger rates best; air: whatever the ledger rates best; boss: whatever the ledger rates best.
  Gun Turret spots, best first: (12,10) (12,11) (13,10).
  Cannon spots, best first: (12,10) (13,10) (12,11).
  Laser Turret spots, best first: (13,10) (11,10) (12,10).
  Flak Turret spots, best first: (36,10) (36,7) (36,11).
  Upgrades: 2 guns down first, then an upgrade is worth 0.97x its value per coin against a new gun.
  Mines: before every boss wave, never for crowds.
  Slow field: never.
  Harvester: in the first 50 % of the level, once the guns hold the waves at hand with cover 1.50.
  Repair drone: never.
  Attack drone: never.
  Gate upgrades: the defence gate's gun and health, weighed at 1.20x against a tower step.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: never.
  The build order seed 3 took (2 stars, base 616):
  from wave  1: build Cannon at (12,10); build Flak Turret at (36,10)
  from wave  2: upgrade the defence gate
  from wave  4: build Gun Turret at (12,11)
  from wave  7: buy the HarvestDrone
  from wave  8: upgrade the tower at (12,11); build Gun Turret at (13,10); upgrade the tower at (13,10)
  from wave  9: build Gun Turret at (21,11)
  from wave 10: upgrade the tower at (21,11); build Gun Turret at (14,7)
  from wave 11: upgrade the tower at (14,7); build Gun Turret at (14,11); upgrade the tower at (14,11)
  from wave 12: build Gun Turret at (13,11); upgrade the tower at (13,11); build Gun Turret at (36,11); build Gun Turret at (14,10)
  from wave 13: upgrade the tower at (14,10); upgrade the tower at (36,11); build Gun Turret at (32,10); upgrade the tower at (32,10)
  from wave 14: upgrade the tower at (36,10)
  from wave 15: build Gun Turret at (15,10); upgrade the tower at (15,10); build Gun Turret at (31,10); upgrade the tower at (31,10); build Gun Turret at (15,11)
  from wave 16: build Cannon at (35,11); build Cannon at (32,11); build Cannon at (16,10); build Cannon at (15,7); build Cannon at (16,11)
  from wave 17: upgrade the tower at (15,11); build Gun Turret at (14,6)
  from wave 18: lay a Mine on the road at (5,15); lay a Mine on the road at (36,15); upgrade the tower at (32,11); upgrade the tower at (35,11); build Cannon at (33,10); upgrade the tower at (13,10); upgrade the tower at (32,10); upgrade the tower at (33,10); upgrade the tower at (16,10)
```


## Crysium Vein (Level_C1_06) on Psycho

Best spots per tower: Gun Turret (12,10) (12,11) (13,10); Cannon (12,10) (13,10) (14,7); Laser Turret (13,10) (11,10) (12,10); Flak Turret (36,10) (36,7) (36,11).

**Level_C1_06_3_strategy5** (won 3/3 seeds, 2.7 stars, base 783 left, repair drone)

```
  Towers per threat: swarm: Laser Turret; pack: Cannon; plated: Cannon; air: whatever the ledger rates best; boss: whatever the ledger rates best.
  Gun Turret spots, best first: (12,10) (12,11) (13,10).
  Cannon spots, best first: (14,7) (13,10) (13,11).
  Laser Turret spots, best first: (13,10) (11,10) (12,10).
  Flak Turret spots, best first: (36,10) (34,10) (36,11).
  Upgrades: 2 guns down first, then an upgrade is worth 1.60x its value per coin against a new gun.
  Mines: before every boss wave, never for crowds.
  Slow field: when the guns would not hold the wave (cover under 1.00) or the slow pays by the damage rule.
  Harvester: in the first 30 % of the level, once the guns hold the waves at hand with cover 1.50.
  Repair drone: once a gun or gate has lost 10 % of its health.
  Attack drone: never.
  Gate upgrades: never.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: the weakest ground gun may go for a sky gun the preview asks for (Psycho).
  The build order seed 1 took (3 stars, base 900):
  from wave  1: build Cannon at (14,7); build Flak Turret at (36,10); upgrade the tower at (14,7); build Cannon at (13,10); buy the HarvestDrone; build Cannon at (13,11); build Flak Turret at (34,10); upgrade the tower at (13,10)
  from wave  3: buy the RepairDrone
  from wave  5: build Gun Turret at (12,10); sell the tower at (13,11); build Flak Turret at (36,11); upgrade the tower at (12,10)
  from wave  6: upgrade the tower at (36,10)
  from wave  7: upgrade the tower at (34,10)
  from wave  9: upgrade the tower at (12,10); build Gun Turret at (12,11); upgrade the tower at (12,11)
  from wave 10: build Gun Turret at (13,11); upgrade the tower at (13,11)
  from wave 11: upgrade the tower at (13,11); upgrade the tower at (12,11)
  from wave 12: build Gun Turret at (12,10); upgrade the tower at (12,10); build Gun Turret at (14,11); upgrade the tower at (14,11); upgrade the tower at (13,10); upgrade the tower at (14,11); build Gun Turret at (21,11); upgrade the tower at (21,11)
  from wave 13: sell the tower at (12,10); build Flak Turret at (36,7); build Gun Turret at (12,10)
  from wave 14: lay a SlowField on the road at (41,9)
  from wave 15: upgrade the tower at (12,10); upgrade the tower at (12,10); build Gun Turret at (14,10); upgrade the tower at (14,10); upgrade the tower at (14,10)
  from wave 16: build Gun Turret at (32,10); lay a SlowField on the road at (41,9); build Cannon at (35,11); upgrade the tower at (32,10); build Gun Turret at (15,10); upgrade the tower at (15,10); build Gun Turret at (31,10); upgrade the tower at (31,10); build Gun Turret at (15,11); upgrade the tower at (15,11); build Gun Turret at (16,10)
  from wave 17: upgrade the tower at (16,10); build Gun Turret at (32,11); upgrade the tower at (32,11)
  from wave 18: upgrade the tower at (32,10); build Gun Turret at (15,7); upgrade the tower at (15,7); build Gun Turret at (16,11); upgrade the tower at (16,11); build Gun Turret at (14,6); upgrade the tower at (14,6); build Gun Turret at (33,10); upgrade the tower at (33,10); upgrade the tower at (31,10); build Gun Turret at (17,10); upgrade the tower at (17,10); build Gun Turret at (16,7); lay a Mine on the road at (5,15); lay a Mine on the road at (36,15); lay a SlowField on the road at (41,9); upgrade the tower at (32,11); upgrade the tower at (16,7)
```

**Level_C1_06_3_strategy2** (won 3/3 seeds, 2.3 stars, base 715 left, repair drone)

```
  Towers per threat: swarm: whatever the ledger rates best; pack: whatever the ledger rates best; plated: Laser Turret; air: whatever the ledger rates best; boss: whatever the ledger rates best.
  Gun Turret spots, best first: (12,10) (12,11) (13,10).
  Cannon spots, best first: (13,10) (14,7) (12,10).
  Laser Turret spots, best first: (12,10) (13,10) (11,10).
  Flak Turret spots, best first: (36,10) (36,7) (36,11).
  Upgrades: 2 guns down first, then an upgrade is worth 1.06x its value per coin against a new gun.
  Mines: before every boss wave, never for crowds.
  Slow field: when the guns would not hold the wave (cover under 1.00) or the slow pays by the damage rule.
  Harvester: in the first 30 % of the level, once the guns hold the waves at hand with cover 1.20.
  Repair drone: once a gun or gate has lost 50 % of its health.
  Attack drone: never.
  Gate upgrades: never.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: the weakest ground gun may go for a sky gun the preview asks for (Psycho).
  The build order seed 2 took (3 stars, base 900):
  from wave  1: build Cannon at (13,10); build Flak Turret at (36,10); build Cannon at (14,7); buy the HarvestDrone; upgrade the tower at (13,10); build Cannon at (12,10); build Flak Turret at (36,7); upgrade the tower at (14,7)
  from wave  3: buy the RepairDrone
  from wave  5: build Gun Turret at (12,11); sell the tower at (12,10); build Flak Turret at (36,11); upgrade the tower at (12,11)
  from wave  6: lay a Mine on the road at (5,15)
  from wave  7: upgrade the tower at (36,10); upgrade the tower at (36,7)
  from wave  9: upgrade the tower at (12,11); build Gun Turret at (12,10)
  from wave 10: upgrade the tower at (12,10); build Gun Turret at (13,11)
  from wave 11: upgrade the tower at (13,11); build Gun Turret at (14,11); upgrade the tower at (14,11)
  from wave 12: build Gun Turret at (21,11); upgrade the tower at (13,10); upgrade the tower at (14,11); build Gun Turret at (14,10); upgrade the tower at (14,10); upgrade the tower at (21,11)
  from wave 13: sell the tower at (12,10); build Flak Turret at (34,10); build Gun Turret at (12,10)
  from wave 14: sell the tower at (12,10); build Flak Turret at (35,10)
  from wave 15: build Gun Turret at (12,10); upgrade the tower at (12,10); build Gun Turret at (32,10); upgrade the tower at (32,10); lay a SlowField on the road at (41,9); upgrade the tower at (32,10); upgrade the tower at (14,10)
  from wave 16: build Gun Turret at (15,10); upgrade the tower at (15,10); build Gun Turret at (31,10); upgrade the tower at (31,10); build Gun Turret at (15,11); upgrade the tower at (15,11); build Gun Turret at (16,10); upgrade the tower at (16,10); build Gun Turret at (32,11); upgrade the tower at (32,11)
  from wave 17: build Gun Turret at (15,7); upgrade the tower at (15,7)
  from wave 18: build Gun Turret at (16,11); lay a Mine on the road at (5,15); upgrade the tower at (31,10); upgrade the tower at (16,11); build Gun Turret at (14,6); upgrade the tower at (14,6); build Gun Turret at (33,10); upgrade the tower at (33,10); build Gun Turret at (17,10); upgrade the tower at (17,10); upgrade the tower at (32,11); build Gun Turret at (16,7); upgrade the tower at (16,7); lay a Mine on the road at (5,15); lay a SlowField on the road at (41,9); build Cannon at (35,11); upgrade the tower at (33,10)
```

