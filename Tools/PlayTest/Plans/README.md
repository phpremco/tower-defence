# Winning plans per level and difficulty

Written from the stage-B replay (`Tools/PlayTest/plan_records.txt`, `bash Tools/PlayTest/balance_pipeline.sh readme`) on 2026-11-07 17:43; calibration 2026-11-07 01:56, seeds 1,2,3, step 0.2, inputs 956d2750 (current).

A plan counts as a winner only when it won in play on most seeds (the paper verdict is a pre-screen). Each plan is a priority list: a step is
taken from its wave on, as soon as the money is there (an earlier step blocks the later ones). Cells are (x,y) on the level grid. The obvious
family's results are in `Tools/PlayTest/yardstick.txt`. A cell without a winner shows how far the best replayed plan got: a design question.

## Landing Site (Level_C1_01) on Easy

Best spots per tower: Gun Turret (5,14) (6,14) (31,8); Cannon (5,14) (31,8) (6,14).

**Level_C1_01_0_strategy4** (won 3/3 seeds, 3.0 stars, base 600 left)

```
  Towers per threat: swarm: Gun Turret; pack: Cannon; plated: Cannon; air: Cannon; boss: whatever the ledger rates best.
  Gun Turret spots, best first: (5,14) (6,14) (31,8).
  Cannon spots, best first: (31,8) (6,14) (5,14).
  Upgrades: 1 guns down first, then an upgrade is worth 0.61x its value per coin against a new gun.
  Mines: not for bosses, and before a crowd wave when the guns would not hold it (cover under 1.00).
  Slow field: never.
  Harvester: in the first 50 % of the level, once the guns hold the waves at hand with cover 1.50.
  Repair drone: never.
  Attack drone: never.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: never.
  The build order seed 2 took (3 stars, base 600):
  from wave  1: build Cannon at (31,8); build Gun Turret at (5,14)
  from wave  3: build Gun Turret at (6,14)
  from wave  6: upgrade the tower at (5,14)
  from wave  8: upgrade the tower at (6,14)
  from wave 10: build Cannon at (13,7)
  from wave 11: build Cannon at (14,7)
  from wave 12: upgrade the tower at (5,14); upgrade the tower at (6,14)
```

**Level_C1_01_0_strategy1** (won 3/3 seeds, 3.0 stars, base 600 left)

```
  Towers per threat: swarm: Gun Turret; pack: Gun Turret; plated: whatever the ledger rates best; air: whatever the ledger rates best; boss: Cannon.
  Gun Turret spots, best first: (6,14) (5,14) (31,8).
  Cannon spots, best first: (14,7) (5,14) (31,8).
  Upgrades: 2 guns down first, then an upgrade is worth 1.05x its value per coin against a new gun.
  Mines: before every boss wave, never for crowds.
  Slow field: when the guns would not hold the wave (cover under 1.00) or the slow pays by the damage rule.
  Harvester: in the first 30 % of the level, once the guns hold the waves at hand with cover 1.50.
  Repair drone: once a gun or gate has lost 5 % of its health.
  Attack drone: once 6 guns stand.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: never.
  The build order seed 3 took (3 stars, base 600):
  from wave  1: build Cannon at (14,7); build Gun Turret at (6,14)
  from wave  3: upgrade the tower at (6,14)
  from wave  5: build Gun Turret at (5,14)
  from wave  7: build Gun Turret at (31,8)
  from wave  8: upgrade the tower at (31,8)
  from wave 10: build Cannon at (13,7)
  from wave 11: upgrade the tower at (31,8); upgrade the tower at (5,14)
```


## Landing Site (Level_C1_01) on Medium

Best spots per tower: Gun Turret (5,14) (6,14) (31,8); Cannon (5,14) (31,8) (6,14).

**Level_C1_01_1_strategy2** (won 3/3 seeds, 3.0 stars, base 600 left)

```
  Towers per threat: swarm: Cannon; pack: whatever the ledger rates best; plated: Gun Turret; air: whatever the ledger rates best; boss: Cannon.
  Gun Turret spots, best first: (5,14) (31,8) (6,14).
  Cannon spots, best first: (5,14) (31,8) (6,14).
  Upgrades: 3 guns down first, then an upgrade is worth 1.79x its value per coin against a new gun.
  Mines: before every boss wave, and before a crowd wave when the guns would not hold it (cover under 1.30).
  Slow field: when the guns would not hold the wave (cover under 1.20) or the slow pays by the damage rule.
  Harvester: in the first 40 % of the level, once the guns hold the waves at hand with cover 1.50.
  Repair drone: once a gun or gate has lost 25 % of its health.
  Attack drone: once 12 guns stand.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: never.
  The build order seed 2 took (3 stars, base 600):
  from wave  1: build Cannon at (5,14); build Gun Turret at (31,8); build Gun Turret at (6,14)
  from wave  3: upgrade the tower at (31,8)
  from wave  8: upgrade the tower at (6,14)
  from wave 10: build Gun Turret at (5,13); upgrade the tower at (5,13); upgrade the tower at (31,8)
  from wave 11: upgrade the tower at (6,14)
  from wave 12: upgrade the tower at (5,13); upgrade the tower at (5,14)
```

**Level_C1_01_1_strategy3** (won 3/3 seeds, 3.0 stars, base 600 left)

```
  Towers per threat: swarm: whatever the ledger rates best; pack: whatever the ledger rates best; plated: Gun Turret; air: Cannon; boss: Gun Turret.
  Gun Turret spots, best first: (5,14) (6,14) (31,8).
  Cannon spots, best first: (5,14) (31,8) (6,14).
  Upgrades: 3 guns down first, then an upgrade is worth 1.59x its value per coin against a new gun.
  Mines: before every boss wave, and before a crowd wave when the guns would not hold it (cover under 1.00).
  Slow field: when the guns would not hold the wave (cover under 1.00) or the slow pays by the damage rule.
  Harvester: in the first 50 % of the level, once the guns hold the waves at hand with cover 1.50.
  Repair drone: once a gun or gate has lost 10 % of its health.
  Attack drone: never.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: never.
  The build order seed 2 took (3 stars, base 600):
  from wave  1: build Cannon at (5,14); build Gun Turret at (6,14); build Gun Turret at (31,8)
  from wave  3: upgrade the tower at (31,8)
  from wave  8: upgrade the tower at (6,14)
  from wave 10: build Gun Turret at (32,8); upgrade the tower at (32,8); upgrade the tower at (31,8)
  from wave 11: upgrade the tower at (6,14)
  from wave 12: upgrade the tower at (32,8); build Cannon at (14,7)
```


## Landing Site (Level_C1_01) on Hard

Best spots per tower: Gun Turret (5,14) (6,14) (31,8); Cannon (5,14) (31,8) (6,14).

**Level_C1_01_2_strategy1** (won 3/3 seeds, 3.0 stars, base 700 left)

```
  Towers per threat: swarm: Cannon; pack: Cannon; plated: whatever the ledger rates best; air: Gun Turret; boss: Cannon.
  Gun Turret spots, best first: (5,14) (6,14) (31,8).
  Cannon spots, best first: (5,14) (14,7) (13,7).
  Upgrades: 4 guns down first, then an upgrade is worth 1.54x its value per coin against a new gun.
  Mines: before every boss wave, never for crowds.
  Slow field: when the guns would not hold the wave (cover under 1.20) or the slow pays by the damage rule.
  Harvester: in the first 40 % of the level, once the guns hold the waves at hand with cover 1.00.
  Repair drone: once a gun or gate has lost 10 % of its health.
  Attack drone: once 6 guns stand.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: never.
  The build order seed 3 took (3 stars, base 700):
  from wave  1: build Cannon at (5,14); build Cannon at (14,7)
  from wave  3: build Gun Turret at (6,14)
  from wave  4: build Gun Turret at (31,8)
  from wave  5: upgrade the tower at (31,8)
  from wave  6: upgrade the tower at (6,14)
  from wave  7: build Gun Turret at (32,8)
  from wave  9: upgrade the tower at (32,8); build Gun Turret at (5,13); upgrade the tower at (5,13)
  from wave 10: build Gun Turret at (5,15); upgrade the tower at (5,15); build Gun Turret at (11,6); upgrade the tower at (11,6)
  from wave 11: build Gun Turret at (10,6); upgrade the tower at (10,6); build Gun Turret at (12,6); upgrade the tower at (12,6); upgrade the tower at (14,7)
  from wave 12: upgrade the tower at (11,6); upgrade the tower at (10,6); upgrade the tower at (12,6); build Cannon at (13,7); build Cannon at (11,3); upgrade the tower at (11,3)
```


## Landing Site (Level_C1_01) on Psycho

Best spots per tower: Gun Turret (5,14) (6,14) (31,8); Cannon (5,14) (31,8) (6,14).

**Level_C1_01_3_strategy1** (won 2/3 seeds, 3.0 stars, base 700 left)

```
  Towers per threat: swarm: whatever the ledger rates best; pack: whatever the ledger rates best; plated: Cannon; air: whatever the ledger rates best; boss: Gun Turret.
  Gun Turret spots, best first: (5,14) (6,14) (32,8).
  Cannon spots, best first: (14,7) (5,14) (31,8).
  Upgrades: 1 guns down first, then an upgrade is worth 1.79x its value per coin against a new gun.
  Mines: before every boss wave, and before a crowd wave when the guns would not hold it (cover under 1.30).
  Slow field: when the guns would not hold the wave (cover under 1.00) or the slow pays by the damage rule.
  Harvester: in the first 30 % of the level, once the guns hold the waves at hand with cover 1.50.
  Repair drone: once a gun or gate has lost 5 % of its health.
  Attack drone: never.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: the weakest ground gun may go for a sky gun the preview asks for (Psycho).
  The build order seed 1 took (3 stars, base 700):
  from wave  1: build Cannon at (14,7); upgrade the tower at (14,7)
  from wave  3: build Gun Turret at (5,14); upgrade the tower at (5,14)
  from wave  4: build Gun Turret at (6,14)
  from wave  5: upgrade the tower at (6,14)
  from wave  7: build Gun Turret at (32,8); upgrade the tower at (32,8)
  from wave  8: upgrade the tower at (5,14)
  from wave  9: upgrade the tower at (6,14); upgrade the tower at (32,8); build Cannon at (31,8); upgrade the tower at (31,8)
  from wave 10: upgrade the tower at (14,7); upgrade the tower at (31,8); build Cannon at (13,7)
  from wave 11: upgrade the tower at (13,7); build Cannon at (11,6)
  from wave 12: upgrade the tower at (11,6); upgrade the tower at (11,6)
```


## Glass Meadows (Level_C1_02) on Easy

Best spots per tower: Gun Turret (7,6) (5,6) (5,7); Cannon (30,15) (30,16) (31,16); Laser Turret (7,6) (6,5) (7,5).

**Level_C1_02_0_strategy4** (won 3/3 seeds, 3.0 stars, base 700 left)

```
  Towers per threat: swarm: Laser Turret; pack: Cannon; plated: Gun Turret; air: Laser Turret; boss: Laser Turret.
  Gun Turret spots, best first: (7,6) (6,5) (5,6).
  Cannon spots, best first: (4,7) (31,16) (30,15).
  Laser Turret spots, best first: (6,5) (7,6) (7,5).
  Upgrades: 1 guns down first, then an upgrade is worth 1.61x its value per coin against a new gun.
  Mines: not for bosses, never for crowds.
  Slow field: when the guns would not hold the wave (cover under 1.20) or the slow pays by the damage rule.
  Harvester: in the first 50 % of the level, once the guns hold the waves at hand with cover 1.00.
  Repair drone: once a gun or gate has lost 50 % of its health.
  Attack drone: once 8 guns stand.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: never.
  The build order seed 2 took (3 stars, base 700):
  from wave  1: build Cannon at (4,7); build Gun Turret at (7,6)
  from wave  2: upgrade the tower at (7,6)
  from wave  5: upgrade the tower at (7,6)
  from wave  7: build Gun Turret at (6,5)
  from wave  8: upgrade the tower at (6,5)
  from wave  9: upgrade the tower at (4,7)
  from wave 10: upgrade the tower at (6,5)
  from wave 12: build Laser Turret at (7,5)
```

**Level_C1_02_0_strategy1** (won 3/3 seeds, 3.0 stars, base 700 left)

```
  Towers per threat: swarm: Laser Turret; pack: Gun Turret; plated: whatever the ledger rates best; air: whatever the ledger rates best; boss: whatever the ledger rates best.
  Gun Turret spots, best first: (7,6) (5,7) (5,6).
  Cannon spots, best first: (5,7) (30,15) (30,16).
  Laser Turret spots, best first: (7,5) (6,5) (7,6).
  Upgrades: 2 guns down first, then an upgrade is worth 1.22x its value per coin against a new gun.
  Mines: before every boss wave, and before a crowd wave when the guns would not hold it (cover under 1.00).
  Slow field: never.
  Harvester: in the first 60 % of the level, once the guns hold the waves at hand with cover 1.50.
  Repair drone: never.
  Attack drone: once 8 guns stand.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: never.
  The build order seed 1 took (3 stars, base 700):
  from wave  1: build Cannon at (5,7); build Gun Turret at (7,6)
  from wave  2: upgrade the tower at (7,6)
  from wave  5: upgrade the tower at (7,6)
  from wave  7: build Gun Turret at (5,6)
  from wave  8: upgrade the tower at (5,6)
  from wave  9: upgrade the tower at (5,7)
  from wave 10: build Gun Turret at (6,5); upgrade the tower at (6,5)
  from wave 11: upgrade the tower at (6,5)
  from wave 12: upgrade the tower at (5,6); build Gun Turret at (7,5)
```


## Glass Meadows (Level_C1_02) on Medium

Best spots per tower: Gun Turret (7,6) (5,6) (5,7); Cannon (30,15) (30,16) (31,16); Laser Turret (7,6) (6,5) (7,5).

**Level_C1_02_1_strategy5** (won 3/3 seeds, 3.0 stars, base 700 left)

```
  Towers per threat: swarm: whatever the ledger rates best; pack: whatever the ledger rates best; plated: Laser Turret; air: whatever the ledger rates best; boss: Gun Turret.
  Gun Turret spots, best first: (7,6) (5,6) (5,7).
  Cannon spots, best first: (5,7) (30,15) (30,16).
  Laser Turret spots, best first: (7,6) (6,5) (7,5).
  Upgrades: 2 guns down first, then an upgrade is worth 1.31x its value per coin against a new gun.
  Mines: not for bosses, never for crowds.
  Slow field: when the guns would not hold the wave (cover under 1.00) or the slow pays by the damage rule.
  Harvester: in the first 30 % of the level, once the guns hold the waves at hand with cover 1.20.
  Repair drone: once a gun or gate has lost 10 % of its health.
  Attack drone: once 12 guns stand.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: never.
  The build order seed 2 took (3 stars, base 700):
  from wave  1: build Cannon at (5,7); build Gun Turret at (7,6); upgrade the tower at (7,6)
  from wave  3: upgrade the tower at (7,6)
  from wave  5: build Gun Turret at (5,6)
  from wave  6: upgrade the tower at (5,6)
  from wave  7: upgrade the tower at (5,6)
  from wave  8: upgrade the tower at (5,7)
  from wave 10: build Cannon at (30,15)
  from wave 11: buy the RepairDrone
  from wave 12: upgrade the tower at (30,15)
```


## Glass Meadows (Level_C1_02) on Hard

Best spots per tower: Gun Turret (7,6) (5,6) (5,7); Cannon (30,15) (30,16) (31,16); Laser Turret (7,6) (6,5) (7,5).

**Level_C1_02_2_strategy1** (won 3/3 seeds, 3.0 stars, base 700 left)

```
  Towers per threat: swarm: whatever the ledger rates best; pack: Cannon; plated: Laser Turret; air: Gun Turret; boss: whatever the ledger rates best.
  Gun Turret spots, best first: (7,6) (7,5) (5,7).
  Cannon spots, best first: (5,7) (30,15) (30,16).
  Laser Turret spots, best first: (7,6) (6,5) (7,5).
  Upgrades: 2 guns down first, then an upgrade is worth 1.30x its value per coin against a new gun.
  Mines: not for bosses, and before a crowd wave when the guns would not hold it (cover under 1.30).
  Slow field: when the guns would not hold the wave (cover under 1.20) or the slow pays by the damage rule.
  Harvester: in the first 60 % of the level, once the guns hold the waves at hand with cover 1.20.
  Repair drone: once a gun or gate has lost 25 % of its health.
  Attack drone: once 12 guns stand.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: never.
  The build order seed 3 took (3 stars, base 700):
  from wave  1: build Cannon at (5,7); build Cannon at (30,15)
  from wave  2: build Gun Turret at (7,6)
  from wave  4: upgrade the tower at (7,6)
  from wave  6: build Gun Turret at (7,5)
  from wave  7: upgrade the tower at (7,5)
  from wave  8: build Gun Turret at (6,5); upgrade the tower at (6,5); build Gun Turret at (5,6)
  from wave  9: upgrade the tower at (5,6)
  from wave 10: upgrade the tower at (7,6); build Gun Turret at (8,6); buy the RepairDrone; build Gun Turret at (1,15); upgrade the tower at (1,15)
  from wave 11: upgrade the tower at (1,15); upgrade the tower at (8,6); build Gun Turret at (4,13); upgrade the tower at (4,13)
  from wave 12: upgrade the tower at (4,13); build Gun Turret at (4,14); upgrade the tower at (4,14); upgrade the tower at (4,14)
```

**Level_C1_02_2_strategy3** (won 3/3 seeds, 3.0 stars, base 700 left)

```
  Towers per threat: swarm: whatever the ledger rates best; pack: Cannon; plated: whatever the ledger rates best; air: Laser Turret; boss: whatever the ledger rates best.
  Gun Turret spots, best first: (7,6) (5,6) (5,7).
  Cannon spots, best first: (5,7) (30,15) (30,16).
  Laser Turret spots, best first: (7,5) (7,6) (6,5).
  Upgrades: 3 guns down first, then an upgrade is worth 1.04x its value per coin against a new gun.
  Mines: before every boss wave, never for crowds.
  Slow field: when the guns would not hold the wave (cover under 1.00) or the slow pays by the damage rule.
  Harvester: in the first 30 % of the level, once the guns hold the waves at hand with cover 1.20.
  Repair drone: once a gun or gate has lost 25 % of its health.
  Attack drone: once 6 guns stand.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: never.
  The build order seed 3 took (3 stars, base 700):
  from wave  1: build Cannon at (5,7); build Cannon at (30,15)
  from wave  2: build Gun Turret at (7,6)
  from wave  4: upgrade the tower at (7,6)
  from wave  6: build Gun Turret at (5,6)
  from wave  7: upgrade the tower at (5,6)
  from wave  8: build Gun Turret at (6,5); upgrade the tower at (6,5); build Gun Turret at (7,5)
  from wave  9: upgrade the tower at (7,5)
  from wave 10: upgrade the tower at (7,6); build Gun Turret at (8,6); buy the RepairDrone; build Gun Turret at (1,15); upgrade the tower at (1,15)
  from wave 11: upgrade the tower at (1,15); build Gun Turret at (4,13); upgrade the tower at (4,13); upgrade the tower at (4,13)
  from wave 12: build Gun Turret at (4,14); upgrade the tower at (4,14); upgrade the tower at (4,14); build Gun Turret at (13,17)
```


## Glass Meadows (Level_C1_02) on Psycho

Best spots per tower: Gun Turret (7,6) (5,6) (5,7); Cannon (30,15) (30,16) (31,16); Laser Turret (7,6) (6,5) (7,5).

No replayed plan won in play. The best, **Level_C1_02_3_strategy1**, won 0/3 and reached wave 8/12:

```
  Towers per threat: swarm: Gun Turret; pack: Laser Turret; plated: Laser Turret; air: Gun Turret; boss: whatever the ledger rates best.
  Gun Turret spots, best first: (7,6) (6,5) (5,7).
  Cannon spots, best first: (5,7) (30,15) (30,16).
  Laser Turret spots, best first: (7,6) (6,5) (7,5).
  Upgrades: 2 guns down first, then an upgrade is worth 1.30x its value per coin against a new gun.
  Mines: not for bosses, and before a crowd wave when the guns would not hold it (cover under 1.00).
  Slow field: never.
  Harvester: in the first 40 % of the level, once the guns hold the waves at hand with cover 1.50.
  Repair drone: once a gun or gate has lost 50 % of its health.
  Attack drone: once 12 guns stand.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: the weakest ground gun may go for a sky gun the preview asks for (Psycho).
  The build order seed 1 took (lost at wave 8):
  from wave  1: build Cannon at (5,7); build Gun Turret at (7,6); upgrade the tower at (7,6)
  from wave  5: build Gun Turret at (6,5); upgrade the tower at (6,5)
  from wave  6: build Gun Turret at (5,6)
  from wave  7: upgrade the tower at (5,6)
```


## Shard Ridge (Level_C1_03) on Easy

Best spots per tower: Gun Turret (12,11) (13,11) (14,11); Cannon (16,11) (12,10) (12,11); Laser Turret (13,10) (9,10) (11,10); Flak Turret (32,10) (33,10) (34,7).

**Level_C1_03_0_strategy1** (won 3/3 seeds, 3.0 stars, base 700 left)

```
  Towers per threat: swarm: Laser Turret; pack: Cannon; plated: Laser Turret; air: whatever the ledger rates best; boss: whatever the ledger rates best.
  Gun Turret spots, best first: (13,11) (12,11) (14,11).
  Cannon spots, best first: (16,11) (12,10) (12,11).
  Laser Turret spots, best first: (13,10) (9,10) (11,10).
  Flak Turret spots, best first: (32,10) (33,10) (34,7).
  Upgrades: 2 guns down first, then an upgrade is worth 1.00x its value per coin against a new gun.
  Mines: not for bosses, and before a crowd wave when the guns would not hold it (cover under 1.00).
  Slow field: never.
  Harvester: never.
  Repair drone: once a gun or gate has lost 5 % of its health.
  Attack drone: never.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: never.
  The build order seed 3 took (3 stars, base 700):
  from wave  1: build Cannon at (16,11); build Gun Turret at (13,11)
  from wave  2: upgrade the tower at (13,11)
  from wave  4: build Gun Turret at (12,11)
  from wave  6: upgrade the tower at (12,11)
  from wave  8: upgrade the tower at (13,11)
  from wave 10: build Flak Turret at (32,10)
  from wave 13: build Flak Turret at (33,10)
  from wave 14: buy the RepairDrone
  from wave 16: build Laser Turret at (13,10); upgrade the tower at (13,10)
```

**Level_C1_03_0_strategy3** (won 3/3 seeds, 3.0 stars, base 700 left)

```
  Towers per threat: swarm: Laser Turret; pack: Laser Turret; plated: whatever the ledger rates best; air: Laser Turret; boss: whatever the ledger rates best.
  Gun Turret spots, best first: (12,11) (13,11) (14,11).
  Cannon spots, best first: (16,11) (12,10) (12,11).
  Laser Turret spots, best first: (13,10) (9,10) (11,10).
  Flak Turret spots, best first: (32,10) (33,10) (34,7).
  Upgrades: 2 guns down first, then an upgrade is worth 0.98x its value per coin against a new gun.
  Mines: before every boss wave, and before a crowd wave when the guns would not hold it (cover under 1.30).
  Slow field: when the guns would not hold the wave (cover under 1.00) or the slow pays by the damage rule.
  Harvester: never.
  Repair drone: never.
  Attack drone: once 6 guns stand.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: never.
  The build order seed 3 took (3 stars, base 700):
  from wave  1: build Cannon at (16,11); build Gun Turret at (12,11)
  from wave  2: upgrade the tower at (12,11)
  from wave  5: build Gun Turret at (13,11)
  from wave  6: upgrade the tower at (13,11)
  from wave  8: upgrade the tower at (13,11)
  from wave 10: build Flak Turret at (32,10)
  from wave 11: upgrade the tower at (12,11)
  from wave 13: build Flak Turret at (33,10)
  from wave 14: upgrade the tower at (32,10)
  from wave 16: build Laser Turret at (13,10); build Laser Turret at (9,10)
```


## Shard Ridge (Level_C1_03) on Medium

Best spots per tower: Gun Turret (12,11) (13,11) (14,11); Cannon (16,11) (12,10) (12,11); Laser Turret (13,10) (9,10) (11,10); Flak Turret (32,10) (33,10) (34,7).

**Level_C1_03_1_strategy1** (won 3/3 seeds, 3.0 stars, base 700 left)

```
  Towers per threat: swarm: whatever the ledger rates best; pack: whatever the ledger rates best; plated: Laser Turret; air: whatever the ledger rates best; boss: Gun Turret.
  Gun Turret spots, best first: (12,11) (13,11) (14,11).
  Cannon spots, best first: (12,10) (16,11) (13,10).
  Laser Turret spots, best first: (13,10) (9,10) (11,10).
  Flak Turret spots, best first: (32,10) (33,10) (34,7).
  Upgrades: 1 guns down first, then an upgrade is worth 1.63x its value per coin against a new gun.
  Mines: before every boss wave, and before a crowd wave when the guns would not hold it (cover under 1.00).
  Slow field: when the guns would not hold the wave (cover under 1.20) or the slow pays by the damage rule.
  Harvester: in the first 30 % of the level, once the guns hold the waves at hand with cover 1.00.
  Repair drone: once a gun or gate has lost 5 % of its health.
  Attack drone: once 12 guns stand.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: never.
  The build order seed 1 took (3 stars, base 700):
  from wave  1: build Cannon at (12,10); build Laser Turret at (13,10)
  from wave  4: upgrade the tower at (13,10)
  from wave  6: build Gun Turret at (12,11)
  from wave 10: build Flak Turret at (32,10); build Flak Turret at (33,10)
  from wave 13: buy the HarvestDrone
  from wave 14: buy the RepairDrone; upgrade the tower at (32,10)
  from wave 15: upgrade the tower at (12,11)
  from wave 16: upgrade the tower at (12,11); upgrade the tower at (13,10); build Gun Turret at (13,11); upgrade the tower at (13,11); upgrade the tower at (13,11); build Gun Turret at (14,11); upgrade the tower at (14,11)
```

**Level_C1_03_1_strategy3** (won 3/3 seeds, 3.0 stars, base 700 left)

```
  Towers per threat: swarm: whatever the ledger rates best; pack: Gun Turret; plated: whatever the ledger rates best; air: Cannon; boss: whatever the ledger rates best.
  Gun Turret spots, best first: (12,11) (13,11) (14,11).
  Cannon spots, best first: (12,10) (16,11) (13,11).
  Laser Turret spots, best first: (13,10) (9,10) (11,10).
  Flak Turret spots, best first: (32,10) (33,10) (34,7).
  Upgrades: 1 guns down first, then an upgrade is worth 1.42x its value per coin against a new gun.
  Mines: before every boss wave, never for crowds.
  Slow field: when the guns would not hold the wave (cover under 1.20) or the slow pays by the damage rule.
  Harvester: in the first 30 % of the level, once the guns hold the waves at hand with cover 1.20.
  Repair drone: once a gun or gate has lost 5 % of its health.
  Attack drone: once 6 guns stand.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: never.
  The build order seed 3 took (3 stars, base 700):
  from wave  1: build Cannon at (12,10); build Gun Turret at (12,11); upgrade the tower at (12,11); build Gun Turret at (13,11)
  from wave  7: buy the HarvestDrone
  from wave 10: build Flak Turret at (32,10); build Flak Turret at (33,10)
  from wave 13: upgrade the tower at (12,11); buy the RepairDrone; upgrade the tower at (13,11)
  from wave 14: upgrade the tower at (32,10); upgrade the tower at (13,11)
  from wave 15: build Gun Turret at (14,11)
  from wave 16: upgrade the tower at (14,11); upgrade the tower at (14,11); build Gun Turret at (15,11); upgrade the tower at (15,11); upgrade the tower at (15,11); build Gun Turret at (16,11); upgrade the tower at (16,11); upgrade the tower at (16,11)
```


## Shard Ridge (Level_C1_03) on Hard

Best spots per tower: Gun Turret (12,11) (13,11) (14,11); Cannon (16,11) (12,10) (12,11); Laser Turret (13,10) (9,10) (11,10); Flak Turret (32,10) (33,10) (34,7).

No replayed plan won in play. The best, **Level_C1_03_2_strategy1**, won 1/3 and reached wave 16/16:

```
  Towers per threat: swarm: whatever the ledger rates best; pack: whatever the ledger rates best; plated: whatever the ledger rates best; air: Laser Turret; boss: Laser Turret.
  Gun Turret spots, best first: (12,11) (14,11) (17,11).
  Cannon spots, best first: (13,10) (12,11) (12,10).
  Laser Turret spots, best first: (13,10) (9,10) (11,10).
  Flak Turret spots, best first: (32,10) (33,10) (34,7).
  Upgrades: 2 guns down first, then an upgrade is worth 1.34x its value per coin against a new gun.
  Mines: not for bosses, never for crowds.
  Slow field: when the guns would not hold the wave (cover under 1.20) or the slow pays by the damage rule.
  Harvester: in the first 60 % of the level, once the guns hold the waves at hand with cover 1.50.
  Repair drone: once a gun or gate has lost 10 % of its health.
  Attack drone: never.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: never.
  The build order seed 1 took (1 stars, base 12):
  from wave  1: build Cannon at (13,10); build Gun Turret at (12,11); upgrade the tower at (12,11)
  from wave  2: upgrade the tower at (12,11)
  from wave  5: build Cannon at (12,10)
  from wave  7: buy the HarvestDrone
  from wave  8: build Gun Turret at (14,11); upgrade the tower at (14,11)
  from wave  9: build Flak Turret at (32,10)
  from wave 10: upgrade the tower at (14,11); build Gun Turret at (17,11)
  from wave 12: upgrade the tower at (32,10); upgrade the tower at (17,11); build Gun Turret at (13,11)
  from wave 13: upgrade the tower at (13,11); build Flak Turret at (33,10); build Flak Turret at (34,7)
  from wave 14: upgrade the tower at (33,10); build Gun Turret at (16,11); upgrade the tower at (16,11); build Gun Turret at (15,11)
  from wave 15: build Gun Turret at (31,10); upgrade the tower at (31,10); build Gun Turret at (14,10); upgrade the tower at (14,10)
  from wave 16: upgrade the tower at (15,11); build Gun Turret at (14,12); upgrade the tower at (14,12); build Gun Turret at (31,11); upgrade the tower at (31,11); build Gun Turret at (13,12); build Gun Turret at (11,10); build Gun Turret at (30,10); upgrade the tower at (30,10); upgrade the tower at (13,12)
```


## Shard Ridge (Level_C1_03) on Psycho

Best spots per tower: Gun Turret (12,11) (13,11) (14,11); Cannon (16,11) (12,10) (12,11); Laser Turret (13,10) (9,10) (11,10); Flak Turret (32,10) (33,10) (34,7).

**Level_C1_03_3_strategy1** (won 3/3 seeds, 3.0 stars, base 800 left)

```
  Towers per threat: swarm: Gun Turret; pack: whatever the ledger rates best; plated: whatever the ledger rates best; air: whatever the ledger rates best; boss: Laser Turret.
  Gun Turret spots, best first: (12,11) (13,11) (14,11).
  Cannon spots, best first: (13,11) (12,10) (16,11).
  Laser Turret spots, best first: (11,10) (13,10) (15,10).
  Flak Turret spots, best first: (32,10) (33,10) (34,10).
  Upgrades: 3 guns down first, then an upgrade is worth 1.01x its value per coin against a new gun.
  Mines: before every boss wave, never for crowds.
  Slow field: never.
  Harvester: never.
  Repair drone: once a gun or gate has lost 5 % of its health.
  Attack drone: once 8 guns stand.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: the weakest ground gun may go for a sky gun the preview asks for (Psycho).
  The build order seed 3 took (3 stars, base 800):
  from wave  1: build Cannon at (13,11); build Gun Turret at (12,11); build Gun Turret at (14,11); upgrade the tower at (14,11)
  from wave  3: upgrade the tower at (12,11)
  from wave  4: upgrade the tower at (14,11)
  from wave  5: build Gun Turret at (15,11)
  from wave  7: upgrade the tower at (15,11); build Gun Turret at (16,11)
  from wave  8: upgrade the tower at (16,11)
  from wave  9: sell the tower at (12,11); build Flak Turret at (32,10); build Gun Turret at (12,11); upgrade the tower at (12,11)
  from wave 10: upgrade the tower at (12,11); sell the tower at (15,11); build Flak Turret at (33,10); build Gun Turret at (15,11); build Flak Turret at (34,10)
  from wave 11: buy the RepairDrone
  from wave 12: upgrade the tower at (16,11); upgrade the tower at (15,11); build Gun Turret at (17,11)
  from wave 13: upgrade the tower at (17,11); upgrade the tower at (15,11); build Flak Turret at (34,7); upgrade the tower at (32,10)
  from wave 14: build Gun Turret at (13,10); upgrade the tower at (13,10); build Gun Turret at (12,10)
  from wave 15: upgrade the tower at (12,10); upgrade the tower at (13,10); sell the tower at (17,11); build Flak Turret at (35,7)
  from wave 16: upgrade the tower at (12,10); build Cannon at (32,12); build Cannon at (14,10); build Cannon at (14,12); build Cannon at (32,11); build Cannon at (15,10); build Cannon at (13,12)
```


## Fault Line (Level_C1_04) on Easy

Best spots per tower: Gun Turret (32,6) (33,6) (28,5); Cannon (32,6) (33,6) (33,9); Laser Turret (28,5) (29,5) (32,5); Flak Turret (34,9) (33,9) (34,6).

**Level_C1_04_0_strategy1** (won 3/3 seeds, 3.0 stars, base 700 left)

```
  Towers per threat: swarm: Laser Turret; pack: whatever the ledger rates best; plated: Laser Turret; air: whatever the ledger rates best; boss: whatever the ledger rates best.
  Gun Turret spots, best first: (33,6) (32,6) (29,6).
  Cannon spots, best first: (32,6) (33,6) (33,9).
  Laser Turret spots, best first: (32,5) (28,5) (29,5).
  Flak Turret spots, best first: (34,9) (34,6) (34,10).
  Upgrades: 2 guns down first, then an upgrade is worth 1.58x its value per coin against a new gun.
  Mines: before every boss wave, never for crowds.
  Slow field: never.
  Harvester: in the first 60 % of the level, once the guns hold the waves at hand with cover 1.00.
  Repair drone: once a gun or gate has lost 5 % of its health.
  Attack drone: once 12 guns stand.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: never.
  The build order seed 2 took (3 stars, base 700):
  from wave  1: build Cannon at (32,6)
  from wave  3: build Flak Turret at (34,9)
  from wave  7: build Gun Turret at (33,6)
  from wave 12: buy the HarvestDrone
  from wave 13: upgrade the tower at (33,6)
  from wave 14: upgrade the tower at (33,6); upgrade the tower at (32,6)
  from wave 15: upgrade the tower at (34,9)
  from wave 16: upgrade the tower at (34,9); build Cannon at (33,9); buy the RepairDrone; build Gun Turret at (29,6); upgrade the tower at (29,6)
```


## Fault Line (Level_C1_04) on Medium

Best spots per tower: Gun Turret (32,6) (33,6) (28,5); Cannon (32,6) (33,6) (33,9); Laser Turret (28,5) (29,5) (32,5); Flak Turret (34,9) (33,9) (34,6).

**Level_C1_04_1_strategy1** (won 3/3 seeds, 3.0 stars, base 700 left)

```
  Towers per threat: swarm: Gun Turret; pack: whatever the ledger rates best; plated: Laser Turret; air: whatever the ledger rates best; boss: Laser Turret.
  Gun Turret spots, best first: (32,6) (33,6) (28,5).
  Cannon spots, best first: (32,6) (33,6) (33,9).
  Laser Turret spots, best first: (28,5) (33,5) (29,5).
  Flak Turret spots, best first: (34,9) (33,9) (34,6).
  Upgrades: 3 guns down first, then an upgrade is worth 1.23x its value per coin against a new gun.
  Mines: before every boss wave, and before a crowd wave when the guns would not hold it (cover under 1.30).
  Slow field: never.
  Harvester: in the first 30 % of the level, once the guns hold the waves at hand with cover 1.00.
  Repair drone: once a gun or gate has lost 5 % of its health.
  Attack drone: once 12 guns stand.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: never.
  The build order seed 1 took (3 stars, base 700):
  from wave  1: build Cannon at (32,6); build Flak Turret at (34,9)
  from wave  3: build Gun Turret at (33,6)
  from wave  9: upgrade the tower at (33,6); upgrade the tower at (33,6)
  from wave 12: upgrade the tower at (34,9); build Gun Turret at (28,5)
  from wave 13: upgrade the tower at (28,5)
  from wave 14: build Flak Turret at (33,9)
  from wave 15: build Gun Turret at (29,5)
  from wave 16: upgrade the tower at (29,5); build Flak Turret at (34,6); buy the RepairDrone
```

**Level_C1_04_1_strategy2** (won 3/3 seeds, 3.0 stars, base 700 left)

```
  Towers per threat: swarm: whatever the ledger rates best; pack: whatever the ledger rates best; plated: whatever the ledger rates best; air: Cannon; boss: Gun Turret.
  Gun Turret spots, best first: (33,6) (32,6) (29,6).
  Cannon spots, best first: (32,6) (33,6) (33,10).
  Laser Turret spots, best first: (33,5) (28,5) (29,5).
  Flak Turret spots, best first: (34,9) (33,9) (32,9).
  Upgrades: 1 guns down first, then an upgrade is worth 1.40x its value per coin against a new gun.
  Mines: before every boss wave, never for crowds.
  Slow field: never.
  Harvester: in the first 30 % of the level, once the guns hold the waves at hand with cover 1.20.
  Repair drone: never.
  Attack drone: once 12 guns stand.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: never.
  The build order seed 1 took (3 stars, base 700):
  from wave  1: build Cannon at (32,6); build Flak Turret at (34,9)
  from wave  3: build Gun Turret at (33,6)
  from wave  9: upgrade the tower at (33,6); upgrade the tower at (33,6)
  from wave 12: upgrade the tower at (34,9); build Gun Turret at (29,6)
  from wave 13: upgrade the tower at (29,6)
  from wave 14: build Flak Turret at (33,9)
  from wave 15: build Gun Turret at (32,5)
  from wave 16: upgrade the tower at (32,5); build Flak Turret at (32,9); upgrade the tower at (33,9); upgrade the tower at (32,5)
```


## Fault Line (Level_C1_04) on Hard

Best spots per tower: Gun Turret (32,6) (33,6) (28,5); Cannon (32,6) (33,6) (33,9); Laser Turret (28,5) (29,5) (32,5); Flak Turret (34,9) (33,9) (34,6).

**Level_C1_04_2_strategy1** (won 3/3 seeds, 3.0 stars, base 781 left)

```
  Towers per threat: swarm: whatever the ledger rates best; pack: whatever the ledger rates best; plated: whatever the ledger rates best; air: Cannon; boss: whatever the ledger rates best.
  Gun Turret spots, best first: (32,6) (33,6) (28,5).
  Cannon spots, best first: (32,6) (33,6) (33,9).
  Laser Turret spots, best first: (28,5) (33,5) (29,5).
  Flak Turret spots, best first: (34,9) (33,9) (34,6).
  Upgrades: 3 guns down first, then an upgrade is worth 0.92x its value per coin against a new gun.
  Mines: before every boss wave, and before a crowd wave when the guns would not hold it (cover under 1.00).
  Slow field: when the guns would not hold the wave (cover under 1.00) or the slow pays by the damage rule.
  Harvester: in the first 30 % of the level, once the guns hold the waves at hand with cover 1.00.
  Repair drone: once a gun or gate has lost 10 % of its health.
  Attack drone: once 12 guns stand.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: never.
  The build order seed 3 took (3 stars, base 800):
  from wave  1: build Cannon at (32,6); build Flak Turret at (34,9)
  from wave  2: build Gun Turret at (33,6)
  from wave  7: buy the HarvestDrone; upgrade the tower at (34,9)
  from wave  9: upgrade the tower at (33,6)
  from wave 10: build Gun Turret at (28,5); upgrade the tower at (28,5); upgrade the tower at (33,6)
  from wave 11: build Gun Turret at (29,5); upgrade the tower at (29,5)
  from wave 12: build Gun Turret at (29,6)
  from wave 13: upgrade the tower at (29,6); build Gun Turret at (32,5); upgrade the tower at (29,5)
  from wave 14: upgrade the tower at (34,9); upgrade the tower at (32,5); build Gun Turret at (30,9); upgrade the tower at (30,9)
  from wave 15: upgrade the tower at (32,5); build Gun Turret at (31,9)
  from wave 16: upgrade the tower at (31,9); upgrade the tower at (30,9); build Gun Turret at (33,5); upgrade the tower at (33,5); build Flak Turret at (33,9); build Flak Turret at (34,6); build Flak Turret at (34,10); build Flak Turret at (32,9); build Gun Turret at (29,6); buy the RepairDrone; upgrade the tower at (29,6)
```


## Fault Line (Level_C1_04) on Psycho

Best spots per tower: Gun Turret (32,6) (33,6) (28,5); Cannon (32,6) (33,6) (33,9); Laser Turret (28,5) (29,5) (32,5); Flak Turret (34,9) (33,9) (34,6).

No replayed plan won in play. The best, **Level_C1_04_3_strategy1**, won 0/3 and reached wave 13/16:

```
  Towers per threat: swarm: whatever the ledger rates best; pack: whatever the ledger rates best; plated: whatever the ledger rates best; air: Cannon; boss: whatever the ledger rates best.
  Gun Turret spots, best first: (28,5) (29,5) (32,6).
  Cannon spots, best first: (33,9) (33,6) (32,6).
  Laser Turret spots, best first: (28,5) (29,5) (32,5).
  Flak Turret spots, best first: (34,9) (34,10) (33,9).
  Upgrades: 2 guns down first, then an upgrade is worth 1.67x its value per coin against a new gun.
  Mines: before every boss wave, and before a crowd wave when the guns would not hold it (cover under 1.30).
  Slow field: when the guns would not hold the wave (cover under 1.00) or the slow pays by the damage rule.
  Harvester: in the first 30 % of the level, once the guns hold the waves at hand with cover 1.20.
  Repair drone: once a gun or gate has lost 25 % of its health.
  Attack drone: once 8 guns stand.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: the weakest ground gun may go for a sky gun the preview asks for (Psycho).
  The build order seed 1 took (lost at wave 13):
  from wave  1: build Cannon at (33,9); build Flak Turret at (34,9)
  from wave  2: build Gun Turret at (28,5)
  from wave  4: upgrade the tower at (28,5)
  from wave  5: build Gun Turret at (29,5); upgrade the tower at (29,5)
  from wave  9: upgrade the tower at (29,5); build Gun Turret at (32,6)
  from wave 10: upgrade the tower at (32,6); build Gun Turret at (28,5)
  from wave 11: upgrade the tower at (28,5); upgrade the tower at (34,9); upgrade the tower at (32,6); sell the tower at (28,5); build Flak Turret at (34,10)
  from wave 12: build Gun Turret at (28,5)
  from wave 13: upgrade the tower at (28,5); build Gun Turret at (33,6); upgrade the tower at (33,6)
```


## Quartz Flats (Level_C1_05) on Easy

Best spots per tower: Gun Turret (7,5) (7,4) (20,5); Cannon (7,5) (7,4) (20,5); Laser Turret (7,5) (6,6) (7,4); Flak Turret (34,4) (34,5) (35,4).

**Level_C1_05_0_strategy3** (won 2/3 seeds, 3.0 stars, base 700 left)

```
  Towers per threat: swarm: Cannon; pack: whatever the ledger rates best; plated: whatever the ledger rates best; air: Gun Turret; boss: whatever the ledger rates best.
  Gun Turret spots, best first: (7,5) (7,4) (21,5).
  Cannon spots, best first: (7,5) (6,5) (20,5).
  Laser Turret spots, best first: (7,5) (7,4) (6,6).
  Flak Turret spots, best first: (33,4) (34,4) (34,5).
  Upgrades: 2 guns down first, then an upgrade is worth 1.09x its value per coin against a new gun.
  Mines: before every boss wave, and before a crowd wave when the guns would not hold it (cover under 1.00).
  Slow field: never.
  Harvester: never.
  Repair drone: once a gun or gate has lost 10 % of its health.
  Attack drone: once 8 guns stand.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: never.
  The build order seed 1 took (3 stars, base 700):
  from wave  1: build Cannon at (7,5); build Flak Turret at (33,4)
  from wave  3: build Gun Turret at (7,4)
  from wave  7: upgrade the tower at (7,4)
  from wave  8: build Gun Turret at (21,5)
  from wave 11: build Flak Turret at (34,4)
  from wave 12: buy the RepairDrone
  from wave 13: upgrade the tower at (21,5)
  from wave 14: build Gun Turret at (6,5); upgrade the tower at (6,5); build Gun Turret at (6,6)
  from wave 15: upgrade the tower at (6,6); build Gun Turret at (20,5)
  from wave 16: upgrade the tower at (20,5)
  from wave 17: buy the AttackDrone; upgrade the tower at (6,6); upgrade the tower at (6,5); upgrade the tower at (7,4)
```


## Quartz Flats (Level_C1_05) on Medium

Best spots per tower: Gun Turret (7,5) (7,4) (20,5); Cannon (7,5) (7,4) (20,5); Laser Turret (7,5) (6,6) (7,4); Flak Turret (34,4) (34,5) (35,4).

No replayed plan won in play. The best, **Level_C1_05_1_strategy1**, won 0/3 and reached wave 11/17:

```
  Towers per threat: swarm: whatever the ledger rates best; pack: Gun Turret; plated: Laser Turret; air: whatever the ledger rates best; boss: Cannon.
  Gun Turret spots, best first: (7,5) (7,4) (20,5).
  Cannon spots, best first: (7,5) (7,4) (20,5).
  Laser Turret spots, best first: (7,5) (6,6) (7,4).
  Flak Turret spots, best first: (33,4) (34,4) (34,5).
  Upgrades: 2 guns down first, then an upgrade is worth 0.96x its value per coin against a new gun.
  Mines: before every boss wave, and before a crowd wave when the guns would not hold it (cover under 1.30).
  Slow field: when the guns would not hold the wave (cover under 1.00) or the slow pays by the damage rule.
  Harvester: in the first 50 % of the level, once the guns hold the waves at hand with cover 1.20.
  Repair drone: once a gun or gate has lost 25 % of its health.
  Attack drone: never.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: never.
  The build order seed 1 took (lost at wave 11):
  from wave  1: build Cannon at (7,5); build Flak Turret at (33,4); build Gun Turret at (7,4)
  from wave 10: build Gun Turret at (20,5); build Gun Turret at (21,5)
  from wave 11: upgrade the tower at (7,4)
```


## Quartz Flats (Level_C1_05) on Hard

Best spots per tower: Gun Turret (7,5) (7,4) (20,5); Cannon (7,5) (7,4) (20,5); Laser Turret (7,5) (6,6) (7,4); Flak Turret (34,4) (34,5) (35,4).

No replayed plan won in play. The best, **Level_C1_05_2_strategy1**, won 0/3 and reached wave 10/17:

```
  Towers per threat: swarm: whatever the ledger rates best; pack: Cannon; plated: whatever the ledger rates best; air: whatever the ledger rates best; boss: whatever the ledger rates best.
  Gun Turret spots, best first: (6,6) (7,4) (20,5).
  Cannon spots, best first: (7,5) (7,4) (20,5).
  Laser Turret spots, best first: (7,5) (6,6) (7,4).
  Flak Turret spots, best first: (34,5) (34,4) (35,4).
  Upgrades: 1 guns down first, then an upgrade is worth 1.78x its value per coin against a new gun.
  Mines: before every boss wave, and before a crowd wave when the guns would not hold it (cover under 1.00).
  Slow field: never.
  Harvester: in the first 40 % of the level, once the guns hold the waves at hand with cover 1.20.
  Repair drone: once a gun or gate has lost 10 % of its health.
  Attack drone: once 8 guns stand.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: never.
  The build order seed 1 took (lost at wave 10):
  from wave  1: build Cannon at (7,5); build Flak Turret at (34,5); build Gun Turret at (6,6)
  from wave  5: upgrade the tower at (6,6)
  from wave  8: upgrade the tower at (6,6); build Gun Turret at (7,4); upgrade the tower at (7,4)
  from wave  9: build Gun Turret at (20,5); upgrade the tower at (20,5)
```


## Quartz Flats (Level_C1_05) on Psycho

Best spots per tower: Gun Turret (7,5) (7,4) (20,5); Cannon (7,5) (7,4) (20,5); Laser Turret (7,5) (6,6) (7,4); Flak Turret (34,4) (34,5) (35,4).

No replayed plan won in play. The best, **Level_C1_05_3_strategy1**, won 0/3 and reached wave 13/17:

```
  Towers per threat: swarm: Gun Turret; pack: Gun Turret; plated: Gun Turret; air: whatever the ledger rates best; boss: Laser Turret.
  Gun Turret spots, best first: (7,5) (6,6) (7,4).
  Cannon spots, best first: (7,5) (7,4) (20,5).
  Laser Turret spots, best first: (7,5) (6,6) (7,4).
  Flak Turret spots, best first: (34,4) (34,5) (35,5).
  Upgrades: 3 guns down first, then an upgrade is worth 0.91x its value per coin against a new gun.
  Mines: before every boss wave, never for crowds.
  Slow field: when the guns would not hold the wave (cover under 1.00) or the slow pays by the damage rule.
  Harvester: in the first 30 % of the level, once the guns hold the waves at hand with cover 1.00.
  Repair drone: once a gun or gate has lost 25 % of its health.
  Attack drone: once 6 guns stand.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: the weakest ground gun may go for a sky gun the preview asks for (Psycho).
  The build order seed 2 took (lost at wave 13):
  from wave  1: build Cannon at (7,5); build Flak Turret at (34,4); build Gun Turret at (6,6)
  from wave  2: upgrade the tower at (6,6)
  from wave  4: build Gun Turret at (7,4); upgrade the tower at (7,4)
  from wave  6: build Gun Turret at (20,5)
  from wave  7: sell the tower at (20,5); build Flak Turret at (34,5); build Gun Turret at (20,5)
  from wave  8: upgrade the tower at (20,5)
  from wave  9: build Gun Turret at (21,5); upgrade the tower at (21,5)
  from wave 10: build Gun Turret at (6,5); upgrade the tower at (6,5)
  from wave 11: build Gun Turret at (5,10); upgrade the tower at (34,4); upgrade the tower at (34,5); upgrade the tower at (5,10); build Gun Turret at (5,10)
  from wave 12: upgrade the tower at (5,10); build Gun Turret at (7,4); build Gun Turret at (5,11)
  from wave 13: upgrade the tower at (5,10); upgrade the tower at (5,11)
```


## Crysium Vein (Level_C1_06) on Easy

Best spots per tower: Gun Turret (12,10) (12,11) (13,10); Cannon (17,11) (17,7) (17,10); Laser Turret (13,10) (11,10) (12,10); Flak Turret (36,10) (36,7) (36,11).

**Level_C1_06_0_strategy2** (won 3/3 seeds, 3.0 stars, base 700 left)

```
  Towers per threat: swarm: Cannon; pack: whatever the ledger rates best; plated: whatever the ledger rates best; air: whatever the ledger rates best; boss: whatever the ledger rates best.
  Gun Turret spots, best first: (12,10) (12,11) (13,10).
  Cannon spots, best first: (17,11) (17,7) (17,10).
  Laser Turret spots, best first: (13,10) (11,10) (12,10).
  Flak Turret spots, best first: (36,10) (36,7) (36,11).
  Upgrades: 2 guns down first, then an upgrade is worth 1.43x its value per coin against a new gun.
  Mines: not for bosses, never for crowds.
  Slow field: when the guns would not hold the wave (cover under 1.00) or the slow pays by the damage rule.
  Harvester: never.
  Repair drone: once a gun or gate has lost 25 % of its health.
  Attack drone: once 8 guns stand.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: never.
  The build order seed 1 took (3 stars, base 700):
  from wave  1: build Cannon at (17,11); build Flak Turret at (36,10)
  from wave  4: build Gun Turret at (12,10)
  from wave  6: upgrade the tower at (12,10)
  from wave  8: upgrade the tower at (12,10)
  from wave 10: upgrade the tower at (36,10)
  from wave 12: build Cannon at (17,7)
  from wave 14: lay a SlowField on the road at (41,9)
  from wave 15: lay a SlowField on the road at (41,9); buy the RepairDrone
  from wave 16: lay a SlowField on the road at (41,9)
  from wave 17: lay a SlowField on the road at (41,9); build Cannon at (17,10); upgrade the tower at (17,10)
  from wave 18: lay a SlowField on the road at (41,9); build Cannon at (16,11); upgrade the tower at (16,11)
```

**Level_C1_06_0_strategy1** (won 3/3 seeds, 3.0 stars, base 700 left)

```
  Towers per threat: swarm: Cannon; pack: whatever the ledger rates best; plated: Laser Turret; air: Gun Turret; boss: whatever the ledger rates best.
  Gun Turret spots, best first: (13,10) (12,11) (12,10).
  Cannon spots, best first: (17,11) (16,11) (17,7).
  Laser Turret spots, best first: (13,10) (11,10) (12,10).
  Flak Turret spots, best first: (36,10) (36,7) (36,11).
  Upgrades: 3 guns down first, then an upgrade is worth 1.46x its value per coin against a new gun.
  Mines: not for bosses, never for crowds.
  Slow field: when the guns would not hold the wave (cover under 1.20) or the slow pays by the damage rule.
  Harvester: never.
  Repair drone: once a gun or gate has lost 10 % of its health.
  Attack drone: once 12 guns stand.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: never.
  The build order seed 2 took (3 stars, base 700):
  from wave  1: build Cannon at (17,11); build Flak Turret at (36,10)
  from wave  4: build Gun Turret at (13,10)
  from wave  6: upgrade the tower at (13,10)
  from wave  8: upgrade the tower at (13,10)
  from wave 10: upgrade the tower at (36,10)
  from wave 12: build Cannon at (16,11)
  from wave 13: lay a SlowField on the road at (41,9)
  from wave 14: lay a SlowField on the road at (41,9)
  from wave 15: lay a SlowField on the road at (41,9)
  from wave 16: buy the RepairDrone
  from wave 17: lay a SlowField on the road at (41,9); build Cannon at (17,7); build Cannon at (17,10)
  from wave 18: lay a SlowField on the road at (41,9); upgrade the tower at (16,11); build Cannon at (18,11)
```


## Crysium Vein (Level_C1_06) on Medium

Best spots per tower: Gun Turret (12,10) (12,11) (13,10); Cannon (17,11) (17,7) (17,10); Laser Turret (13,10) (11,10) (12,10); Flak Turret (36,10) (36,7) (36,11).

**Level_C1_06_1_strategy1** (won 3/3 seeds, 3.0 stars, base 700 left)

```
  Towers per threat: swarm: Cannon; pack: whatever the ledger rates best; plated: whatever the ledger rates best; air: Cannon; boss: whatever the ledger rates best.
  Gun Turret spots, best first: (12,10) (12,11) (13,10).
  Cannon spots, best first: (17,11) (17,10) (17,7).
  Laser Turret spots, best first: (13,10) (13,11) (12,10).
  Flak Turret spots, best first: (35,10) (36,7) (36,11).
  Upgrades: 2 guns down first, then an upgrade is worth 1.73x its value per coin against a new gun.
  Mines: before every boss wave, never for crowds.
  Slow field: when the guns would not hold the wave (cover under 1.00) or the slow pays by the damage rule.
  Harvester: never.
  Repair drone: once a gun or gate has lost 25 % of its health.
  Attack drone: once 6 guns stand.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: never.
  The build order seed 1 took (3 stars, base 700):
  from wave  1: build Cannon at (17,11); build Flak Turret at (35,10)
  from wave  3: upgrade the tower at (17,11)
  from wave  5: build Gun Turret at (12,10)
  from wave  7: upgrade the tower at (12,10)
  from wave  9: upgrade the tower at (35,10)
  from wave 10: upgrade the tower at (12,10)
  from wave 12: build Cannon at (17,10)
  from wave 13: build Flak Turret at (36,7)
  from wave 14: build Gun Turret at (12,11)
  from wave 15: upgrade the tower at (12,11); lay a SlowField on the road at (41,9)
  from wave 16: lay a SlowField on the road at (41,9); buy the RepairDrone
  from wave 17: upgrade the tower at (12,11); build Gun Turret at (13,10)
  from wave 18: upgrade the tower at (13,10); build Gun Turret at (13,11); upgrade the tower at (13,11); lay a Mine on the road at (5,15); lay a Mine on the road at (36,15); lay a SlowField on the road at (41,9); buy the AttackDrone
```


## Crysium Vein (Level_C1_06) on Hard

Best spots per tower: Gun Turret (12,10) (12,11) (13,10); Cannon (17,11) (17,7) (17,10); Laser Turret (13,10) (11,10) (12,10); Flak Turret (36,10) (36,7) (36,11).

**Level_C1_06_2_strategy1** (won 2/3 seeds, 3.0 stars, base 800 left)

```
  Towers per threat: swarm: whatever the ledger rates best; pack: Gun Turret; plated: Gun Turret; air: whatever the ledger rates best; boss: Cannon.
  Gun Turret spots, best first: (13,10) (12,10) (14,7).
  Cannon spots, best first: (17,10) (12,10) (17,7).
  Laser Turret spots, best first: (13,10) (11,10) (12,10).
  Flak Turret spots, best first: (36,10) (36,7) (36,11).
  Upgrades: 4 guns down first, then an upgrade is worth 0.65x its value per coin against a new gun.
  Mines: before every boss wave, never for crowds.
  Slow field: when the guns would not hold the wave (cover under 1.20) or the slow pays by the damage rule.
  Harvester: never.
  Repair drone: once a gun or gate has lost 25 % of its health.
  Attack drone: once 8 guns stand.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: never.
  The build order seed 3 took (3 stars, base 800):
  from wave  1: build Cannon at (17,10); build Flak Turret at (36,10); build Gun Turret at (13,10)
  from wave  3: build Gun Turret at (12,10)
  from wave  4: upgrade the tower at (13,10)
  from wave  5: upgrade the tower at (12,10)
  from wave  8: upgrade the tower at (36,10); build Gun Turret at (14,7)
  from wave  9: upgrade the tower at (14,7); build Gun Turret at (12,11)
  from wave 11: upgrade the tower at (13,10); upgrade the tower at (12,11); build Gun Turret at (13,11)
  from wave 12: upgrade the tower at (13,11); build Gun Turret at (14,11); upgrade the tower at (14,11); build Gun Turret at (14,10)
  from wave 13: build Flak Turret at (36,7)
  from wave 14: upgrade the tower at (14,10); build Gun Turret at (32,10)
  from wave 15: upgrade the tower at (32,10); build Gun Turret at (15,10); build Gun Turret at (31,10); upgrade the tower at (31,10); lay a SlowField on the road at (41,9); upgrade the tower at (17,10)
  from wave 16: lay a SlowField on the road at (41,9)
  from wave 17: lay a Mine on the road at (5,15); lay a Mine on the road at (5,15); upgrade the tower at (32,10); upgrade the tower at (15,10); build Gun Turret at (15,11)
  from wave 18: upgrade the tower at (15,11); build Gun Turret at (16,10); upgrade the tower at (31,10); upgrade the tower at (16,10); build Gun Turret at (32,11); lay a Mine on the road at (5,15); lay a Mine on the road at (36,15); lay a SlowField on the road at (41,9); buy the RepairDrone
```


## Crysium Vein (Level_C1_06) on Psycho

Best spots per tower: Gun Turret (12,10) (12,11) (13,10); Cannon (17,11) (17,7) (17,10); Laser Turret (13,10) (11,10) (12,10); Flak Turret (36,10) (36,7) (36,11).

No replayed plan won in play. The best, **Level_C1_06_3_strategy1**, won 0/3 and reached wave 16/18:

```
  Towers per threat: swarm: whatever the ledger rates best; pack: whatever the ledger rates best; plated: Cannon; air: Laser Turret; boss: whatever the ledger rates best.
  Gun Turret spots, best first: (12,11) (12,10) (13,11).
  Cannon spots, best first: (17,11) (16,11) (17,10).
  Laser Turret spots, best first: (13,10) (11,10) (12,10).
  Flak Turret spots, best first: (36,11) (36,7) (36,10).
  Upgrades: 3 guns down first, then an upgrade is worth 1.05x its value per coin against a new gun.
  Mines: not for bosses, and before a crowd wave when the guns would not hold it (cover under 1.00).
  Slow field: when the guns would not hold the wave (cover under 1.20) or the slow pays by the damage rule.
  Harvester: in the first 40 % of the level, once the guns hold the waves at hand with cover 1.50.
  Repair drone: once a gun or gate has lost 10 % of its health.
  Attack drone: never.
  Sky guns: saved for from the wave whose income covers one before a raid the standing sky guns cannot hold; a splash gun before the first crowd wave.
  Repairs: the gate when it would fall in the coming wave or a hit point is cheaper to restore than to stop with a gun; wrecks scrapped.
  Selling: the weakest ground gun may go for a sky gun the preview asks for (Psycho).
  The build order seed 2 took (lost at wave 16):
  from wave  1: build Cannon at (17,11); build Flak Turret at (36,11)
  from wave  2: build Gun Turret at (12,11)
  from wave  3: upgrade the tower at (12,11)
  from wave  4: build Gun Turret at (12,10); upgrade the tower at (12,10)
  from wave  6: build Flak Turret at (36,7)
  from wave  8: lay a Mine on the road at (5,15); lay a Mine on the road at (36,15)
  from wave  9: build Gun Turret at (13,11); upgrade the tower at (13,11)
  from wave 10: upgrade the tower at (12,10)
  from wave 11: build Gun Turret at (13,10); upgrade the tower at (13,10)
  from wave 12: build Gun Turret at (14,7); upgrade the tower at (14,7); upgrade the tower at (13,10)
  from wave 13: build Gun Turret at (14,11); upgrade the tower at (14,11); sell the tower at (12,11); build Flak Turret at (36,10); build Gun Turret at (12,11)
  from wave 14: sell the tower at (12,11); build Flak Turret at (35,10); build Gun Turret at (12,11)
  from wave 15: upgrade the tower at (12,11); upgrade the tower at (14,7); upgrade the tower at (14,11); buy the HarvestDrone
  from wave 16: upgrade the tower at (13,11); build Gun Turret at (14,10); upgrade the tower at (14,10); build Gun Turret at (32,10)
```

