# COMBAT_RULES.md

The combat rules of Deepwake, since round 67 (2026-11-15); the growth layer of section 6 since round 68 (2026-11-16). One page that shows who is for what.

Remco's model has two layers:

1. **Roles (fixed, the whole game).** Every tower is good at one job. Off its job it is clearly worse per coin. No easy air
   without air defence, no easy armour without anti-armour.
2. **Growth (per level).** Creatures grow through the waves. When a new kind appears, a few off-role guns may still handle
   its first wave or two; a few waves later only the counter tower does.

Everything on this page is data (`Assets/_Project/Data/Balance/CombatRules.asset`, written by the generator from
`ProjectGenerator.Damage.cs`) and is held by tests (`CombatRulesTests`). A new weapon or creature that breaks a role fails a
test before anyone plays it.

## 1. The rule table: damage type against trait

A weapon carries **one thing: what its damage is made of**. What a hit is worth follows from this table. No weapon has a
number of its own.

| Damage type | On armour | In the air | Weapons | Why |
| --- | --- | --- | --- | --- |
| **Explosive** | 1 | 1 | Cannon, Artillery, Flak (see exceptions), mine, barrel, Spencer's shockwave | A blast strips armour point for point and bursts among flyers. |
| **Piercing** | **1.5** | 0.5 | Railgun | A slug is made for armour. One slow shot is a poor way to hunt flyers. |
| **Bullet** | 0.2 | 0.2 | Gun Turret, base turret, gate gun, Spencer's blaster, attack drone | Light fire only chips armour; a gun not made for the sky lands a fifth up there. |
| **Beam** | 0.2 | 0.2 | Laser, Cryo, Cryo Beam | A beam is light fire: it glances off plates and spreads out on the way up. |
| **Arc** | 0.2 | 0.4 | Tesla, Storm Rifle | Lightning runs over plates instead of through them, and thins out on its way up. |
| **Blade** | 0.5 | cannot reach | Spencer's energy sword | A blade cuts armour at half. |
| **Burn** | 0.5 | cannot reach | Lava ball | Fire heats armour at half. |
| **Ram** | 0.5 | cannot reach | Ground drone | A ram dents armour at half. |

How the two columns work in play:

- **Armour** is a pool in front of the body (the blue bar). While any is left, a hit works on the pool at the share above
  and nothing reaches the body. Once it is gone every weapon lands in full. So light fire still finishes what shells have
  stripped.
- **Air** is the share of a hit that lands on a creature only the sky can be aimed at. It is taken on the creature the hit
  lands on, so a blast that catches a flyer and a walker hurts each at its own share.
- Whether a weapon can aim up at all is its **targets** (ground, air or both), not its damage: the Cannon's shell would
  hurt a flyer, but a Cannon cannot aim up.

### Named exceptions

The only place a weapon may differ from its damage type. Each has a reason; the tests refuse one without.

| Weapon | Against | Factor | Reason |
| --- | --- | --- | --- |
| Flak Turret | armour | 0.5 (its type says 1) | Its airburst is fragments, not a shell: it is the air specialist and no anti-armour gun (Remco 2026-11-15). No armoured flyer exists today. |

The Railgun is not an exception: piercing is its own damage type with its own row.

## 2. Creature traits

A creature carries its numbers and nothing else. Its traits follow from them (thresholds in the rules asset):

| Trait | Rule | Answered by |
| --- | --- | --- |
| **Armoured** | has an armour pool | anti-armour: explosive and piercing |
| **Airborne** | only the sky can be aimed at | anti-air |
| **Swarm** | body + armour of 40 or less | light fire (a big hit is wasted on a small body) |
| **Mob** | between swarm and tough; bunches up at a gate | splash |
| **Tough** | body + armour of 250 or more, and every boss | light fire held on one target |
| **Fast** | 1.3 cells a second or faster | guided or instant fire (an unguided shell lands where it was) |

Every creature has exactly one size (swarm, mob, tough) and any of armoured, airborne, fast. The full list with traits is at
the end of `Tools/PlayTest/r67_role_matrix.txt`. Crystal Plains 1 to 6: Skitter (swarm, fast), Prowler and Roller (mob,
fast), Oozer and Mireleech (mob), Wisp (swarm), Cragback (mob, armoured), Skyray (mob, airborne, fast), the bats (swarm,
airborne, fast), the bosses (tough).

**Every boss is armoured (round 85).** A boss, a level's or a continent's, carries an armour pool of at least
`BalanceRules.BossArmourShare` (0.25) of its body; one whose own data gives it more keeps that. So a boss answers to the
same rows as every plated creature: light fire chips the pool at 0.2, shells and slugs strip it, and the Cannon is what a
boss asks for. The rule is written into the creature data by the generator (`ProjectGenerator.ArmourOf`) and by
`BalanceStages.ApplyBossArmour`, and the finale stage keeps it when it moves a continent boss's health.

**A level's boss is sized on this table (round 86).** It costs the player's guns three times
(`BalanceRules.BossOverRegular`) what the toughest ordinary creature met before it cost them, under the explosive row
(armour point for point, the bar) and under the light-fire rows (armour at 0.2, a flyer at 0.2 on top) both; the
solver's `BossYardstick` reads the two columns from `BalanceRules.LightFireOnArmour` and `LightFireInTheAir`, which a
test holds to this table. On the bar alone (round 85) a boss of three Cragback bars was 1.4 Cragbacks to a Gun Turret
or a Laser, because the Cragback is three quarters armour and a boss a fifth. Details: Docs/WAVE_GRAMMAR.md, "The
solver", item 6.

**A boss reads its phases on its whole bar** (`Health.BarFraction`, body and armour together; round 86). On the body
alone (`Health.Fraction`) a boss in armour kept its first phase until every plate was gone, a fifth of the fight under
shells and half of it under light fire. The Shattered's shards take their share of the body and of the armour the
giant has left, and the giant that pulls back together has the survivors' body and armour.

Armour and air have a **factor** (section 1). Swarm, mob, tough and fast have none: they are held by mechanics that already
exist (a hit bigger than a creature is wasted; a blast takes at most three; a shell that is not guided misses a runner).
The role tests measure all six the same way.

## 3. Tower roles

| Tower | Role | In one line |
| --- | --- | --- |
| **Gun Turret** | Light fire | Cheap: streams of small creatures, runners and soft bodies. Only chips armour, and lands a fifth in the air. |
| **Laser** | Light fire | The Gun Turret's job with nearly three times the firepower in one cell. Glances off armour; cannot aim up. |
| **Cannon** | Splash, anti-armour | Blast: breaks bunched groups and strips armour. Cannot aim up, and its shell lands where a runner was. |
| **Flak Turret** | Anti-air | The one gun that lands in full on flyers. Cannot hit the ground. |
| **Tesla** | Splash | Chain lightning: jumps through a bunched group at short range. A fifth on armour; cannot aim up. |
| **Railgun** | Anti-armour | Slug: made for armour and bosses, from far away. Slow: wasted on swarms, and half in the air. |
| **Cryo** | Control | Slows what passes, so every other gun has longer. Its own damage is a token. |
| **Support** | Booster | The guns around it reach further and fire faster. It fires nothing itself. |
| **Artillery** (Cannon + Flak) | Splash, anti-armour, anti-air | A big blast for groups and armour that also bursts among flyers. |
| **Storm Rifle** (Gun Turret + Tesla) | Light fire, splash | Rapid arcs for swarms and small groups. A fifth on armour, under half in the air. |
| **Cryo Beam** (Laser + Cryo) | Light fire, control | A beam that burns one big soft body and chills it. Glances off armour, a fifth in the air. |
| Gate gun, base turret | Helper | They help. They replace no tower. |

A hybrid has its parents' roles. Roles are data on the tower (`TowerDefinition.Roles`, `RoleLine`).

## 4. The matrix: seconds for 100 coins of a tower to clear a trait's reference group

Lower is better. **Bold** = the tower is made for that trait. `-` = cannot. Towers at level 1; the same tables for level 3
and for level 3 with every permanent upgrade against late waves are in `Tools/PlayTest/r67_role_matrix.txt` (rewritten by
`balance_pipeline.sh roles` and by the tests).

| Tower | Coins | Swarm | Mob | Tough | Armoured | Airborne | Fast |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Gun Turret | 50 | **6.5** | 20.3 | **12.2** | 104.9 | 65.0 | **4.5** |
| Laser | 130 | **7.9** | 19.7 | **11.5** | 97.8 | - | **4.3** |
| Cannon | 100 | 9.6 | **14.9** | 22.3 | **17.0** | - | - |
| Flak Turret | 120 | - | - | - | - | **9.5** | - |
| Tesla | 140 | 11.2 | **11.0** | 15.8 | 44.1 | - | 6.0 |
| Railgun | 220 | 58.1 | 41.5 | 22.5 | **45.3** | 50.5 | 12.2 |
| Cryo | 90 | 38.9 | 131.4 | 78.5 | 675.2 | 418.2 | 29.0 |
| Storm Rifle | 310 | **9.9** | **16.9** | **16.0** | 89.6 | 21.3 | **5.8** |
| Cryo Beam | 360 | **16.4** | 40.9 | **24.0** | 203.1 | 127.2 | **8.9** |
| Artillery | 370 | 35.5 | **29.9** | 34.2 | **26.1** | **20.3** | 15.0 |

The reference groups (data, `CombatRules.Scenarios`), each played at four health scales and averaged so no role rests on
one lucky number:

| Trait | Reference fight | Margin the rules ask |
| --- | --- | --- |
| Swarm | a stream of 12 Skitters, 1.5 cells apart, on the run | 1.15x |
| Mob | 5 Oozers queued at a gate | 1.25x |
| Tough | 1 Shard Crawler | 1.15x |
| **Armoured** | 3 Cragbacks at a gate | **2x** |
| **Airborne** | a wing of 4 Skyrays | **2x** |
| Fast | 1 Roller at speed | 1.15x |

**The rule the tests hold:** for every trait, the best tower made for it clears its reference group at least *margin* times
faster per coin than **every** tower that is not made for it, at level 1, at level 3, and at level 3 with all ten tiers of
permanent upgrades against creatures six times as tough.

What the matrix says at one glance:

- **Air:** the Flak (9.5) against the best gun not made for it, the Storm Rifle (21.3): 2.2x. A Gun Turret needs 65.
- **Armour:** the Cannon (17.0) against the best gun not made for it, the Tesla (44.1): 2.6x. Gun Turret and Laser need 100.
- **The size traits are thin.** Light fire against a splash tower on a swarm: 1.5x. The Cannon against light fire on a
  bunched mob: 1.3x at level 1 and about even at level 3. These are mechanics, not factors, and the margins are what the
  mechanics give today (see "What the audit found").

### Helpers

Spencer, the base turret, the gate gun, the attack drone and the path objects follow the same table. The tests hold that
none of them does more per coin against a trait than the best tower made for it (a free helper is weighed as 100 coins, a
path object over 120 seconds of a tower's fire because it is spent after one use). Numbers: the "Helpers" table in
`Tools/PlayTest/r67_role_matrix.txt`.

## 5. Upgrade steps: one named thing per step

The A/B paths at level 3 are cut (Remco 2026-11-15). Every in-level step has a name and improves **one** thing; the tower
menu shows it ("Next: Faster cycling: shots/s 2.9 > 6.0"). The steps are data on the tower's levels
(`TowerLevel.StepName`, `TowerLevel.Step`), and a step that changes two things is refused by the data check.

Each tower's damage per second per level is what it was before the round, so prices and the rule "an upgrade beats a second
tower" stand. What changed: reach no longer grows with every star (it grew 0.3 a level on every gun).

| Tower | Built | Step 1 (level 2) | Step 2 (level 3) |
| --- | --- | --- | --- |
| Gun Turret | 5 damage, 2.9 shots/s, range 4.0 | **Faster cycling**: shots/s 2.9 > 6.0 | **Heavier rounds**: damage 5 > 10 |
| Laser | 8, 5.0/s, 4.3 | **Focused lens**: damage 8 > 13.3 | **Overcharged emitter**: shots/s 5.0 > 9.0 |
| Cannon | 25, 0.6/s, 4.0 | **Heavier shells**: damage 25 > 56.9 | **High-explosive filler**: damage 56.9 > 118.3 |
| Flak Turret | 14, 1.8/s, 5.5 | **Heavier bursts**: damage 14 > 27.5 | **Faster loaders**: shots/s 1.8 > 3.2 |
| Tesla | 16, 2.0/s, 3.5 | **Higher voltage**: damage 16 > 28.9 | **Faster discharge**: shots/s 2.0 > 3.5 |
| Railgun | 80, 0.45/s, 6.0 | **Heavier slug**: damage 80 > 132 | **Faster capacitors**: shots/s 0.45 > 0.76 |
| Cryo | slows to 50%, range 3.7 | **Deeper chill**: slows to 50% > 35% | **Extended nozzle**: range 3.7 > 4.6 |
| Support | range 3.0 | **Extended relay**: range 3.0 > 4.0 | **Stronger signal**: boost x1 > x2 |
| Artillery | 70, 0.6/s, 5.1 | **Heavier shells**: damage 70 > 148.6 | **Autoloader**: shots/s 0.6 > 1.2 |
| Storm Rifle | 14, 5.0/s, 4.1 | **Faster cycling**: shots/s 5.0 > 7.9 | **Higher voltage**: damage 14 > 23.5 |
| Cryo Beam | 8, 6.7/s, 4.3 | **Focused lens**: damage 8 > 15 | **Overcharged emitter**: shots/s 6.7 > 12.1 |

Why these: a step strengthens the tower's own job and never its off-job. The Cannon's two steps are both its shell: a
faster Cannon would be a better swarm gun, which is not its job. The Gun Turret's first step is its rate, because rate is
what a swarm asks.

A gate and the base's turret are structures, not towers: their steps are refits ("Armed gate", "Fortified gate", "Turret
Mk II", "Turret Mk III") and change health and gun together.

Hybrids have their own steps (above); combining makes a new tower at its own level 1.

## 6. Growth: the grace window

After a creature trait that needs a counter (armour, air) first appears in a level, a defence **without** the counter tower

- **may** still hold for the grace window, and
- **must** have lost within a few waves after it.

| | Easy | Medium | Hard | Psycho |
| --- | --- | --- | --- | --- |
| May hold (waves) | 2 | 1 | 1 | 0 |
| Must have lost within (waves after that) | 4 | 4 | 3 | 3 |

One table (`Game.Balance.GrowthWindow`): the generator writes it into the rules asset (`_graceWaves`, `_fallWithinWaves`)
and onto every wave script.

### How a level makes it happen (round 68)

- **The window opens on the first wave a plated troop walks in.** Through the grace waves the troops come as the script
  writes them: a few, so guns that are not made for armour can still deal with them.
- **Then they grow.** With every wave past the window the plated troops come in greater numbers, by the difficulty's
  growth factor per wave (`BalanceRules.ArmourGrowth`, 1.4 / 1.4 / 1.5 / 1.5; a lever per cell in
  `Tools/PlayTest/wave_tuning.txt` until round 84), through the waves a row must fall in; after that they stay as they
  are. They also close ranks (packs of three, a tight file from a nest), so a blast catches several. **The growth
  applies where a difficulty of a level sends no flyers** (round 84; Landing Site and Glass Meadows in the first
  continent): there armour is the only trait that asks for a counter. Where flyers come too, the raids end a row
  without a counter, and growth on top was measured (round 68) and read on paper (round 84, Docs/WAVE_GRAMMAR.md,
  "What a wave is made of") as more than those levels carry.
- **A grown wave aims above the level's curve** by as much as it has grown, so the extra bodies keep their toughness. It
  is marked as a *counter check* (`WaveDefinition.CounterCheck`) and is left out of the mean of the plain waves, as a boss
  wave is: the ordinary waves stay on the human curve, the counter checks sit above it on purpose. The cap "no wave asks
  more than the player with the hero can hold" still applies to them.
- The same exists for flyers (`BalanceRules.AirGrowth`: the raids, the sprinkle and flying troops). It is built (the composer and the solver) and
  stands at one on every difficulty (see below).
- **Composition is the second lever**: when a trait opens its window long before the waves are big enough to end a row in
  it, the troop can walk in later on the easier difficulties (`Join(..., maxDifficulty: Medium)` next to a Hard troop from
  the old wave; round 68 on Shard Ridge). The window opens on the first plated troop of that difficulty.

Why head count and not a share of the wave: a Cragback walks at 0.6 cells a second down a road of a hundred cells, so
light fire has minutes on it. Measured: with every non-rush wave nine tenths plated at the old wave size, a Gun Turret row
still won Landing Site Easy without losing a gate. Armour only forces the counter when there is more of it than light
fire can strip in the time the walk gives, and that is a matter of how much, not of what share.

### The gate (a failing test)

`CombatRulesTests.NoRowWithoutTheCounter_OutlastsItsWindow_AsPlayed` reads the yardstick's rows of one tower type
(`balance_pipeline.sh measure`, then `grace` -> `Tools/PlayTest/grace_windows.txt`) and fails when

1. a row without the counter tower **wins** a level whose window closes before the last wave (no exception anywhere), or
2. a row falls later than the window plus its cell's **allowance** (`GraceWindows.Allowances`: cell, trait, waves late at
   most, reason).

State after round 68: no row without the counter wins any of the 24 cells (round 67: the Gun Turret row won Landing Site
Easy and Medium and Glass Meadows Easy, the Laser row Glass Meadows Easy). 94 rows: 56 in time, 1 early, 29 late inside
an allowance, 8 not judged. The allowances (`GraceWindows.Allowances`, each with its reason in the code):

| Cells | Trait | Late by at most | Why |
| --- | --- | --- | --- |
| Landing Site Medium 1, Hard 2, Psycho 3; Glass Meadows Easy 1, Hard 1, Psycho 3 | armour | 1 to 3 waves | Growth is at the measured edge of what a mixed plan still wins. The rush and boss waves inside the window carry no plated troops, so the row lasts to the next plated wave. |
| Glass Meadows Medium 1 | armour | 1 wave | At the old scale 1.15 with growth no searched plan won (best 0/3). Scale 1.07 with growth 1.3 ends the row in time, but the best plan then wins 2/3 and the creative bot 0/3; the cell keeps scale 1.0 and growth 1.2 (both 3/3). |
| Shard Ridge Easy 1, Medium 1 | armour | 1 wave | The Cragbacks now walk in from wave 8 on Easy and Medium (was 4: five waves late); the row falls to the first raid, one wave after the window. |
| Shard Ridge Hard 6, Psycho 7 | armour | 6 to 7 waves | Remco's plated pack from wave 3 (round 34b) opens the window early and the troops stay few until the raids. Growth 1.5 left no plan winning, growth 1.2 moved no row and cost the creative bot Hard and Psycho. |
| Quartz Flats Easy 2 | armour | 2 waves | Growth 1.5 left no searched plan winning (best 0/3); the row falls to the raid of wave 8 and after. |
| Fault Line Easy, Quartz Flats Medium and Psycho, Crysium Vein Medium | air | 1 wave | The row falls to the raid one wave after the window closes. A bigger raid there costs the mixed plan its margin (these are the cells the creative bot already loses seeds on). |
| **Round 77:** Glass Meadows Medium 1 -> 2, Shard Ridge Medium 1 -> 2 (armour); Fault Line Easy 1 (armour, new) and 1 -> 2 (air); Crysium Vein Easy 1 (air, new) | armour, air | 1 to 2 waves | The structure damage factor (Remco, round 77: creatures bite every structure at 0.45 of their damage on Easy and Medium) keeps a row's base a wave or two longer. Measured with the factor on base and gates alone the rows were as late, so it is the softer siege itself. Every row still loses all three seeds. |
| **Round 77:** Fault Line Medium 2 (air, new), Quartz Flats Medium 3 (armour, new) | air, armour | 2 to 3 waves | The same softer bites, and the first-flyer rule moved the raids that used to end these rows (first raid wave 6, was 3 and 4). Every row still loses all three seeds. |
| **Round 84:** Glass Meadows Medium 2 -> 1 (armour); Fault Line Easy and Psycho (air) removed | armour, air | - | Glass Meadows Medium grows by the rule (1.4, was its own 1.2) and its row is one wave late in two measurements; the two air allowances were unused in two measurements (rounds 83b and 84). 18 allowances stand. |

What stands in the way of closing the rest: on Shard Ridge Hard and Psycho the early plated pack is Remco's choice (a later
pack, or the window counted from the first *grown* plated wave, would close it; his call); on Landing Site and Glass
Meadows plated troops in the rush and boss waves of the window; the raid one wave earlier on the four air cells.

A row of the counter tower is not judged by the window: the **Cannon row still wins Landing Site** (every difficulty) and
Glass Meadows Easy, Medium and Psycho, because those levels send nothing the Cannon is off-role for in numbers. Round 68
tried the one in-roster trait, swarm: a loose stream of Skitters on Landing Site Hard and Psycho (weight 3 from wave 4) and
the swarm lever at 2.5 both left the Cannon row at 3/3 (a level-1 Cannon clears a swarm only 1.5x slower than a Gun Turret
per coin) and cost the creative bot Hard. Options in Docs/STATUS.md, round 68.

## 7. Changing a rule

1. Change the table in `Assets/_Project/Scripts/Editor/ProjectGenerator.Damage.cs` (a factor, an exception with its
   reason, a reference fight, a margin, the grace window) or a tower's role and steps in `ProjectGenerator.Towers.cs` /
   `ProjectGenerator.World.cs`.
2. `bash Tools/PlayTest/r67_apply.sh` (recompile, write the data, write the matrix).
3. Read `Tools/PlayTest/r67_role_matrix.txt`: every role break is listed under its table.
4. `bash Tools/PlayTest/r34b_editmode_worker.sh 6 CombatRulesTests`.
5. A factor that a tower in the levels uses moves the balance: run the pipeline (Docs/COMMANDS.md, "Round 67").

A new weapon: give it a damage type in `ProjectGenerator.WeaponDamageTypes`. That is all; it reads the table.
A new creature: nothing; its traits follow from its numbers.

## 8. What the audit found (round 67)

Where behaviour was decided per weapon instead of by a rule:

| What | Before | Now |
| --- | --- | --- |
| Air share | A number typed on the Gun Turret only (0.2, round 61). Base turret, Spencer's blaster and the attack drone, the same bullets, landed in full on flyers. Railgun, Cryo, Storm Rifle and Cryo Beam too. | The table's air column, by damage type, for every source. |
| Where the air share was taken | On the creature the tower aimed at: a blast aimed at a walker hurt the flyers it caught at the ground share. | On each creature hit (`Hits.Deal`). |
| Armour share | A factor stamped on every weapon from a code table (round 64); the asset could be edited to disagree. | Read from the rules asset at the moment of the hit. No weapon stores a factor. |
| Flak on armour | 0.5, then 1 (round 64), an open question. | 0.5, a named exception (Remco 2026-11-15). |
| Tesla, Storm Rifle on armour | 0.5: a Tesla killed armoured troops nearly as fast per coin as a Cannon (1.2x). | 0.2, like all light fire: 2.6x. **Remco's 2026-11-09 number was 0.5; see the open decisions.** |
| Support mast | Its boost grew with its level by a constant in code. | A named step in data. |
| Upgrade levels | Every level changed damage, rate and reach at once; level 3 had two art paths. | One named thing per step; one look per level. |
| The four ways a hit reached a creature | Projectile, hero blow, shockwave and path object each called the creature themselves. | All through `Hits.Deal`. |

What the matrix shows that is **not** fixed, because it is a design choice:

- **The beam's free kills (mended in round 85).** A beam holds one creature and sweeps on the moment it dies. Until
  round 85 every kill cost the beam only its sweep time (0.05 s) instead of the rest of its pulse, so a creature that
  died in one or two pulses died up to four times as fast as the laser's damage per second says. That is worth the more
  the lower a difficulty's health: on Easy a level 2 Laser cleared a packed Skitter stream at x3.55 its data DPS, on
  Psycho at x1.02 (`Tools/PlayTest/laser_share.txt`). Now the pulse that kills costs the beam the share of it the
  creature needed (`IHitBudget`, `Tower.ReleaseBeam`): it wastes nothing on a dead body and gains nothing either, and it
  clears the stream at x0.98 to x0.99 of its data on every difficulty. Its role is unchanged: it is still the light
  fire that wastes nothing on swarms. Round 86: the price of the killing pulse is settled as the pulse leaves
  (`Game.Towers.BeamBudget`, `Tower.Update`), not a frame later. Settled later it depended on the frame length: at
  the balance probe's 0.2 s steps a level 3 Laser cleared Easy Skitters at x0.49 of its data (x0.77 on Medium and
  Hard, x0.92 on Psycho) where sixty frames a second gave x0.98; now x0.97 to x0.99 at every frame length
  (`Tools/PlayTest/laser_frames.txt`).
- **Gun Turret and Laser do the same job** (light fire) at nearly the same damage per coin. The Laser is the job in fewer
  cells; nothing else sets them apart.
- **The Cannon's splash is worth little against soft bodies**: 1.3x over light fire per coin on a bunched mob at level 1,
  about even at level 3. Its real edge is armour (6x). So a level without armour or without bunching never asks for it,
  and a level with both (Landing Site) asks for nothing else.
- **The Railgun is three times worse per coin than the Cannon against armoured troops** (45 against 17). It is single
  target and slow; it earns its price on reach (6 against 4) and on one big armoured body, which Crystal Plains 1 to 6 do
  not have.
