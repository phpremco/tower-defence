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
| **Beam** | **0.1** | 0.2 | Laser, Cryo, Cryo Beam | A beam glances off plates, and a pulse this small barely marks them; it spreads out on the way up. |
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

### Damage type against creature family (round 87)

A third factor, for what a creature is **made of**. Unlike a trait, a family does not follow from a creature's numbers:
it is typed on the creature (`EnemyDefinition.Family`, written by the generator from `ProjectGenerator.CreatureFamilies`).
The table (`CombatRules.Families`, generator source `ProjectGenerator.FamilyRules`) lists only the pairs that differ from
one; every other damage type on every other family lands as its row above says.

| Damage type | Family | Factor | Creatures | Why |
| --- | --- | --- | --- | --- |
| **Beam** | **Energy** | **1.5** | Wisp, Core Wisp | A beam feeds straight into an energy body and overloads it. |

- The factor multiplies the hit **before** the air share and the armour share, on the creature that is hit
  (`Hits.Deal`, `DamageProfile.Against(layer, family)`). Beam on an energy creature's armour is 1.5 x 0.1.
- It is the damage type's, not a weapon's: the **Cryo and the Cryo Beam are beams too and overload energy bodies as
  the Laser does**. That is meant. No code names a tower.
- A beam's kill pulse is priced on what the creature really takes (`Enemy.HitToKill` includes the family), so the
  round 86 beam rule holds on energy bodies.
- A new pairing is one line in `ProjectGenerator.FamilyRules`; a new family is an enum value
  (`Game.Core.CreatureFamily`) and a line per creature in `ProjectGenerator.CreatureFamilies`.

### Which creature a gun takes (round 88f)

Every gun, base turret, armed gate, attack drone and Spencer take the creature in reach with **the least road left to
its goal**: the threat nearest the base, whichever road or air route it came by (`ITargetable.DistanceToGoal`, one rule
in `TargetRegistry.FindBest`; nothing per tower). Until round 88f the rule was "the furthest travelled", and every level
has a main road of 95 to 116 cells beside side roads and air routes of 35 to 48: a creature biting the last gate off a
side road had travelled 40 cells and lost to one 50 cells down the main road, so the guns shot past it (Remco saw a
Cannon do it). A blast lands on that creature and takes what stands near it; no gun scores clusters. A beam still holds
the creature it burns until it dies or leaves its reach. Tests: `Round88fTests` (every tower asset opens fire on the
creature nearest its goal).

### Retarget time (round 87)

A turret has to swing to a new target: its first shot at a creature other than the one it shot last comes its
**retarget time** later, on top of its reload (`TowerDefinition.RetargetSeconds`, the rule in `Game.Towers.Retarget`,
played by `Tower.Update`). A tower that had nothing to shoot at has had the time to turn: its first shot is free.

| Tower | Retarget | Why |
| --- | --- | --- |
| Gun Turret, Storm Rifle (its hybrid) | 0.15 s | A turret swings its barrel. |
| Laser, Cryo Beam | 0 (a beam has its sweep, 0.033 s, inside `BeamBudget`) | A beam is on the next creature at once. |
| Every other tower | 0 | A shell or an arc is not aimed at one creature the same way; not asked for this round. |

Every creature that dies **or walks out of reach** costs the turret a swing, so the cost grows with the number of
bodies a second, not with their health: on a body a second a level 1 Gun Turret loses about a seventh of its fire, on
a dense swarm most of it. This is what a swarm of fast light creatures asks a Laser for. It is added to the cooldown
like a reload, so it is the same at sixty frames a second and at the probe's 0.2 s steps for streams up to about five
bodies a second (above that a 0.2 s step sees fewer changes of target than there were).

`_beamLockSeconds` (0.35 s on every tower asset, read for beams only) is something else: the time an **idle** beam
charges before its first pulse. A live beam never pays it.

### Named exceptions

The only place a weapon may differ from its damage type. Each has a reason; the tests refuse one without.

| Weapon | Against | Factor | Reason |
| --- | --- | --- | --- |
| Flak Turret | armour | 0.5 (its type says 1) | Its airburst is fragments, not a shell: it is the air specialist and no anti-armour gun (Remco 2026-11-15). No armoured flyer exists today. |

The Railgun is not an exception: piercing is its own damage type with its own row.

## Structures: who can hurt a tower, and from how far (round 88b)

Remco's playtest (2026-11-20): "My towers are never damaged because I place them between the roads." Before this round
only a creature with an attack of its own bit a structure, at 1.6 to 3 cells, and most kinds had none: on Landing Site
and Glass Meadows nothing ever hurt a tower, and elsewhere a tower three cells off the road was safe. Three rules, all in
data, hold on every level.

1. **Reach by attack style** (`CombatRules._reach`, one table, nothing per creature). A creature's attack in passing
   reaches a structure beside the road as far as its style says, and never less than its own attack range:

   | Style | Reach (cells) | Why |
   | --- | --- | --- |
   | Bite, Slash, Whip (no row) | the creature's own range, 1.6 to 2.5 | melee: the front row only |
   | Slam | 3.2 | a slam shakes the ground: the second row |
   | Sting | 4.0 | venom sprayed in an arc |
   | Bolt | 4.5 | a spat bolt flies as far as a gun's shot |

   The reach is for the guns (towers, base turrets, drones). Whatever bars the road (a gate, the base, the hero) is still
   met at the creature's own attack range (`StructureRegistry.FindNearest(origin, range, blockerRange)`), so no gate takes
   more than before. A creature still hits only the nearest structure in reach, so the front row shields the row behind it.
2. **Who bites.** Swarm kinds (Skitter, Prowler, Roller) never hurt a tower. The Cragback, the heavy walker of the first
   levels, slams what it passes for 1.5 (3 in the first pass, halved after the bots lost three to six guns a run; `ProjectGenerator.CragbackAttackDamage`), so a tower has something to fear before
   the boss and the Cannon has one more reason to kill it early. The ranged kinds (Stinger, Shardback, the bats) keep
   their damage and now reach a tower between two roads.
3. **Every level boss hurts a tower from `CombatRules.BossReach` (4 cells) or further.** The Brood Mother (Venom Spit), the
   Core Wisp (Arc Discharge) and the Pack Alpha (Spine Volley) carry the same volley: 8 a bolt (34 in the first pass, 14 until round 88f) at each of the three nearest
   guns within 5 cells every 3 s (`ProjectGenerator.BossVolley*`), flagged `SparesBlockers`: gates, the base and the hero
   are left to the boss's own blows. The Storm Matriarch already had her Element Storm (7 cells) and her own bolts now
   reach 4.5 cells by rule 1. The difficulty's structure damage in play (multiplier x factor: 0.45 / 0.45 / 1 / 1.4) scales
   every bite, bolt and pulse once, as before.
   **Round 88f:** three bolts of 8 every 3 s are what one repair drone mends (8 a second), so a boss held at a gate is
   answered by a crew or a repair; at 14 the volleys were two thirds of all tower wear in the bots' Shard Ridge runs and
   wrecked the three guns beside a held boss in 34 s on Psycho (47 s on Hard).
4. **A gun takes the difficulty's share of a blow (round 88f).** `DifficultyProfile.TowerDamageFactor` (Easy, Medium,
   Hard 1; Psycho 0.7) scales what lands on a tower or a base turret, in one place (`Tower.TakeDamage` reads
   `StructureRegistry.GunDamageShare`, set by the level). Gates, the base and the hero take the bite in full. On Psycho a
   gun is so bitten as on Hard (1.4 x 0.7 = 0.98): Psycho wears guns down more because there are more creatures and its
   bosses live longer, not because each blow is heavier.

In code: `ReachRule`, `EnemyDefinition.StructureReach`, `Enemy.TryAttackStructure`, `BossAbilityDefinition.StructureReach`;
generator `ProjectGenerator.Round88b.cs`; tests `Round88bThreatTests`. The balance model has no term for tower wear: the
Cragback's attack only raises its threat by about 8 % (`Threat.Of`) and its kill money by one coin (15 -> 16).

On paper (`Tools/PlayTest/r88b_threat.txt`, `Round88bThreatLab`; share of a level-1 Cannon's 220 hit points a tower three
cells off the main road loses over a level, as played):

| Level | Easy | Medium | Hard | Psycho | before (E / M / H / P) |
| --- | --- | --- | --- | --- | --- |
| Landing Site | 24 % | 26 % | 103 % | 206 % | 0 / 0 / 0 / 0 |
| Glass Meadows | 35 % | 37 % | 152 % | 297 % | 1 / 1 / 9 / 16 |
| Shard Ridge | 19 % | 20 % | 70 % | 158 % | 0 / 0 / 0 / 0 |
| Fault Line | 23 % | 29 % | 118 % | 172 % | 8 / 9 / 20 / 27 |
| Quartz Flats | 28 % | 32 % | 121 % | 236 % | 2 / 2 / 37 / 49 |
| Crysium Vein | 22 % | 28 % | 82 % | 198 % | 0 / 0 / 0 / 23 |

One pass of the slow Brood Mother with the tower among her three nearest guns all the way: 34 % on Easy and Medium, 75 %
on Hard, 105 % on Psycho; held in reach (at a gate) she costs each of those three guns 27 / 27 / 59 / 83 % per 10 s.
The lab's assumptions are in the report's head (first gate holds, ordinary creatures cover half their pass alive, a
nearest-only bite is shared by two guns, a volley's three bolts by six). As played, the probe's records carry the wear
since round 88b (Docs/COMMANDS.md, "Round 88b").

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

**Family** is the one thing typed on a creature (see section 1): `Energy` for the Wisp and the Core Wisp today. The
creature table at the end of `Tools/PlayTest/r67_role_matrix.txt` has a Family column.

**Every boss is armoured (round 85).** A boss, a level's or a continent's, carries an armour pool
(`BalanceRules.BossArmourShare` of its body). So a boss answers to the same rows as every plated creature: light fire
only chips the pool (section 1), shells and slugs strip it, and the Cannon is what a boss asks for. The rule is written
into the creature data by the generator (`ProjectGenerator.ArmourOf`) and by `BalanceStages.ApplyBossArmour`, and the
finale stage keeps it when it moves a continent boss's health.

**Round 87:** a level boss's size is a formula of level, difficulty and wave phase with three times the bar of the
toughest walker met before it as a floor only, and its armour pool is half its body since round 88b (`BossArmourShare` 0.5, a third of the bar; 1 in round 87); see
Docs/WAVE_GRAMMAR.md, "The solver (round 87: a formula)".

**Round 88b:** a level boss's brood is sized by the escort rule (Docs/WAVE_GRAMMAR.md, "The solver", point 2b): a worth
per difficulty times the boss's bar, growing along its road, a quarter plated where the level has shown a plated walker,
no bounty. A level boss also hurts the guns near its road (the section "Structures" above), and its kill money is
`BalanceRules.BossRewardFactor` 1.5 times that of its threat (was 3).

**A boss reads its phases on its whole bar** (`Health.BarFraction`, body and armour together; round 86). On the body
alone (`Health.Fraction`) a boss in armour kept its first phase until every plate was gone, a fifth of the fight under
shells and half of it under light fire. The Shattered's shards take their share of the body and of the armour the
giant has left, and the giant that pulls back together has the survivors' body and armour.

Armour and air have a **factor** (section 1). Swarm, mob, tough and fast have none: they are held by mechanics that already
exist (a hit bigger than a creature is wasted; a blast takes as many creatures as its gun's level allows (the Cannon two as
built, three and four with its stars; the Flak four at every level; section 3); a shell that is not guided misses a runner).
The role tests measure all six the same way.

## 3. Tower roles

| Tower | Role | In one line |
| --- | --- | --- |
| **Gun Turret** | Light fire | The all-rounder, cheap: soft bodies big and small, a fifth in the air. Its turret swings to each new target, so a fast swarm outruns it. Only chips armour. |
| **Laser** | Light fire | The swarm gun: a fast beam of small pulses that is on the next creature at once. Weak on armour (a tenth) and on one big target; cannot aim up. Half again the damage on energy bodies. |
| **Cannon** | Splash, anti-armour | Blast: breaks bunched groups and strips armour. Cannot aim up, and its shell lands where a runner was. |
| **Flak Turret** | Anti-air | The one gun that lands in full on flyers. Cannot hit the ground, except the Wisp and the Core Wisp: a ball of energy on the road is a target of the ground and of the air (round 88a; `TargetLayersExtensions.CanHit`: a gun reaches whatever shares a layer with it). |
| **Tesla** | Splash | Chain lightning: jumps through a bunched group at short range. A fifth on armour; cannot aim up. |
| **Railgun** | Anti-armour | Slug: made for armour and bosses, from far away. Slow: wasted on swarms, and half in the air. |
| **Cryo** | Control | Slows what passes, so every other gun has longer. Its own damage is a token. |
| **Support** | Booster | The guns around it reach further and fire faster. It fires nothing itself. |
| **Artillery** (Cannon + Flak) | Splash, anti-armour, anti-air | A big blast for groups and armour that also bursts among flyers. |
| **Storm Rifle** (Gun Turret + Tesla) | Light fire, splash | Rapid arcs for swarms and small groups. A fifth on armour, under half in the air. |
| **Cryo Beam** (Laser + Cryo) | Light fire, control | A beam that burns one big soft body and chills it. Glances off armour, a fifth in the air. |
| Gate gun, base turret | Helper | They help. They replace no tower. |

Cannon = slow, clustered, armoured. Laser = fast, spread out, light. Gun Turret = a bit of everything.

A hybrid has its parents' roles. Roles are data on the tower (`TowerDefinition.Roles`, `RoleLine`).

### The blast cap per level (round 88b)

Remco's playtest of 2026-11-20: the Cannon is needed, but a Cannon-heavy build was the best way to play every level. His
rule: **the Cannon's shell hurts at most two creatures as built, three at level 2 and four at level 3** (it was three at
every level). The cap is data per tower level (`TowerLevel.SplashTargets`; zero keeps the tower's own `_splashTargets`,
and zero there is the whole radius), read through one accessor, `TowerDefinition.SplashTargetsAt(levelIndex)`, by
everything: the game (`Tower.Shoot` fires with the cap of its current level, `Projectile.CapSplash` keeps the nearest to
the impact), the balance model (`BalanceLedger.Stats` puts it on `TowerLevelStats.SplashTargets`;
`Ledger.SplashCap(tower, level, rules)` is the one reader for the ledger, the leak model and the route walk), the labs
(`RoleMatrix`, `StreamDuel`, `ArmourTtk.cs`), the calibration's input hash and the texts (`BlastRead`).
`TowerDefinition.OnValidate` refuses a cap on a tower without a blast and a cap that shrinks with the level. Generator:
`ProjectGenerator.Towers.cs` (`CannonSplashTargets = { 2, 3, 4 }`).

| Tower | Level 1 | Level 2 | Level 3 |
| --- | --- | --- | --- |
| Cannon | 2 | 3 | 4 |
| Flak Turret | 4 | 4 | 4 |
| Artillery, Tesla, Storm Rifle | whole radius | whole radius | whole radius |

The Artillery is built from a Cannon and a Flak and keeps its uncapped blast: it is a hybrid, a reward bought with two
towers and a recipe, and its role line ("a big blast for groups") is that blast. Nothing about it changed.

What the player sees: the placement line and the tower menu say "Blast hits up to 2"; the upgrade preview says "Next:
Heavier shells: damage 25 > 56.9, blast hits up to 2 > 3"; the Codex recipe page names a cap that grows as a span.

What it does on paper (`Tools/PlayTest/r88b_cannon_cap.txt`, bar taken off per second per 100 coins, played through the
firing rules; Medium, the other difficulties read the same):

| Cannon | Dense swarm | Spaced stream | Dense pack | Plated column | One plated walker | Boss |
| --- | --- | --- | --- | --- | --- | --- |
| L1, three (before) | 37.6 | 20.3 | 46.4 | 47.0 | 15.9 | 15.0 |
| L1, two (after) | 25.1 | 20.2 | 31.3 | 31.4 | 15.9 | 15.0 |
| L2, three (unchanged) | 19.3 | 10.4 | 52.7 | 53.0 | 18.6 | 17.4 |
| L3, three (before) | 10.7 | 5.8 | 56.0 | 60.5 | 21.5 | 20.2 |
| L3, four (after) | 14.2 | 5.8 | 59.2 | 76.0 | 21.5 | 20.2 |
| Gun Turret L1 / L2 / L3 | 18.6 / 18.5 / 21.5 | 24.6 / 32.7 / 39.8 | 21.4 / 24.4 / 28.3 | 5.0 / 5.8 / 6.8 | 5.7 / 6.7 / 7.8 | 6.0 / 6.9 / 8.0 |
| Laser L1 / L2 / L3 | 27.5 / 32.2 / 29.4 | 21.5 / 18.5 / 15.0 | 27.6 / 32.3 / 37.7 | 2.7 / 3.2 / 3.8 | 2.7 / 3.2 / 3.7 | 2.6 / 3.1 / 3.6 |

So: a level-1 Cannon loses a third against anything bunched (x0.67), a level-2 Cannon is what it was, a level-3 Cannon
gains a quarter to a third against bunched bodies it does not overkill (x1.25 to x1.33 on a plated column). Against one
body (a plated walker, a boss) and against a stream a blast apart nothing changes: there the cap never bound.

**The cap alone does not close the gap between a Cannon build and a Laser or Gun Turret build on the levels without
flyers.** With no blast at all (one creature a shell) a coin of Cannon is worth x1.30 to x1.55 a coin of Gun Turret and
x2.3 to x2.9 a coin of Laser on the stored ground waves of Landing Site and Glass Meadows, because 35 to 46 % of the
ground bar there is armour (plated troops that grow wave after wave, and every boss with armour equal to its body) and a
bullet works on armour at 0.2, a beam at 0.1, a shell at 1. On one plated walker or a boss the Cannon is x2.5 to x2.8 the
Gun Turret and x5.7 to x5.9 the Laser per coin at every level, before and after. What drives the Cannon archetype is the
armour share of the waves times the armour factors, not the size of the blast; the cap takes a third off the Cannon's
crowd work at level 1 and gives it back, and more, at level 3.

## 4. The matrix: seconds for 100 coins of a tower to clear a trait's reference group

Lower is better. **Bold** = the tower is made for that trait. `-` = cannot. Towers at level 1; the same tables for level 3
and for level 3 with every permanent upgrade against late waves are in `Tools/PlayTest/r67_role_matrix.txt` (rewritten by
`balance_pipeline.sh roles` and by the tests).

| Tower | Coins | Swarm | Mob | Tough | Armoured | Airborne | Fast |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Gun Turret | 50 | **6.5** | 20.3 | **12.2** | 104.9 | 65.0 | **4.5** |
| Laser | 130 | **6.9** | 21.2 | **12.7** | 209.2 | - | **4.7** |
| Cannon | 100 | 9.6 | **14.9** | 22.3 | **17.0** | - | - |
| Flak Turret | 120 | - | - | - | - | **9.5** | - |
| Tesla | 140 | 11.2 | **11.0** | 15.8 | 44.1 | - | 6.0 |
| Railgun | 220 | 58.1 | 41.5 | 22.5 | **45.3** | 50.5 | 12.2 |
| Cryo | 90 | 38.9 | 131.4 | 78.5 | 1302.3 | 418.2 | 29.0 |
| Storm Rifle | 310 | **9.9** | **16.9** | **16.0** | 89.6 | 21.3 | **5.8** |
| Cryo Beam | 360 | **16.4** | 40.9 | **24.0** | 391.2 | 127.2 | **8.9** |
| Artillery | 370 | 35.5 | **29.9** | 34.2 | **26.1** | **20.3** | 15.0 |

Round 87: the Gun Turret and Laser rows are those of `Tools/PlayTest/r67_role_matrix.txt` (its "Laser Turret"). The
matrix's reference fights do not play the retarget time (they count shots), so it understates the Laser's edge on the
Swarm and Fast columns; `Tools/PlayTest/laser_role_r87.txt` plays it.

Round 88b: the Cannon's row and the Armoured column above are from before the round (a blast of three at level 1,
three Cragbacks at a gate). `Tools/PlayTest/r67_role_matrix.txt` is rewritten with the new cap and the new reference
fight by `balance_pipeline.sh roles` and by the tests; until this table is copied from it again, read the Cannon's
level-1 numbers in `Tools/PlayTest/r88b_cannon_roles.txt`.

The reference groups (data, `CombatRules.Scenarios`), each played at four health scales and averaged so no role rests on
one lucky number:

| Trait | Reference fight | Margin the rules ask |
| --- | --- | --- |
| Swarm | a stream of 12 Skitters, 1.5 cells apart, on the run | 1.15x |
| Mob | 5 Oozers queued at a gate | 1.25x |
| Tough | 1 Shard Crawler | 1.15x |
| **Armoured** | 2 Cragbacks at a gate (3 until round 88b) | **2x** |
| **Airborne** | a wing of 4 Skyrays | **2x** |
| Fast | 1 Roller at speed | 1.15x |

**Why two Cragbacks (round 88b).** The fight was sized when a shell took three. A level-1 Cannon's blast now hurts two
(the cap 2 / 3 / 4, section 3), and the role "shells break plates" is asserted on a fight its level-1 blast covers. On
three Cragbacks a level-1 Cannon needs twice the shells and is only x1.29 the Tesla per coin (the Tesla's chain takes
all three at a fifth on armour), where the rule asks x2; on two it is x2.59, as it was on three with a blast of three.
At level 3 the role also holds on three. Numbers: `Tools/PlayTest/r88b_cannon_roles.txt`; the fight is rule data
(`Scenario(EnemyTraits.Armoured, "Cragback", 2, ...)` in `ProjectGenerator.Damage.cs`, `_scenarios` in
`CombatRules.asset`).

**The rule the tests hold:** for every trait, the best tower made for it clears its reference group at least *margin* times
faster per coin than **every** tower that is not made for it, at level 1, at level 3, and at level 3 with all ten tiers of
permanent upgrades against creatures six times as tough.

What the matrix says at one glance:

- **Air:** the Flak (9.5) against the best gun not made for it, the Storm Rifle (21.3): 2.2x. A Gun Turret needs 65.
- **Armour:** the Cannon (17.0) against the best gun not made for it, the Tesla (44.1): 2.6x. The Gun Turret needs 100, the Laser 209 (a beam works on armour at a tenth since round 87). These seconds are the old fight's (three Cragbacks, a blast of three); on the new one (two Cragbacks, a blast of two) the ratio is the same, x2.59.
- **The size traits are thin.** Light fire against a splash tower on a swarm: 1.5x. The Cannon against light fire on a
  bunched mob: 1.3x at level 1 and about even at level 3 until round 88b. With a blast of two the level-1 Cannon clears
  the mob fight (five Oozers at a gate) at x0.91 the Gun Turret per coin and x0.95 the Laser (it was x1.37 and x1.42).
  The mob role still holds because the Tesla is the best mob tower, but on a level without the Tesla the level-1 Cannon
  is no longer the better buy against a soft queue: counter to plates as built, crowd-breaker by upgrading. These are
  mechanics, not factors, and the margins are what the mechanics give today (see "What the audit found").

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
| Laser | 4 damage, 9 shots/s, range 4.3 | **Overcharged emitter**: shots/s 9 > 15 | **Focused lens**: damage 4 > 7.2 |
| Cannon | 25, 0.6/s, 4.0, blast of 2 | **Heavier shells**: damage 25 > 56.9, blast of 3 | **High-explosive filler**: damage 56.9 > 118.3, blast of 4 |
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

The Cannon's two steps also raise its blast cap (2 > 3 > 4, round 88b; section 3). The cap is not a step of its own: it
grows with the stars beside the step's named thing, the data check (`TowerDefinition.StepProblem`) does not count it,
and the tower menu lists it after the named thing. The rule "an upgrade buys more single-target damage per coin than a
second tower" (`BalanceRules.UpgradeMargin`) is about one target and is untouched: no damage, rate or price moved
(`TowerRolesTests.EveryUpgradeStep_BeatsMoreTowersOfTheSameType` passes as it did).

The Laser's steps swapped order in round 87: its pulse must stay under the Gun Turret's round at every level, 5 / 5 /
10, so the rate comes first, as on the Gun Turret. Prices 130 / 55 / 100 are unchanged: the ratios between its levels
are.

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

**Round 87: `GraceWindows.Allowances` is empty.** The critic of round 86 read the allowances as readings written down
after the fact (18 of them, up to seven waves on a window of three). They were deleted; an allowance comes back only
with a written design reason, never because a row was measured late. On round 87's waves the gate therefore fails:
94 rows, 51 in time, 2 early, 35 late (29 of them one or two waves, Quartz Flats Medium three, Shard Ridge Hard and
Psycho four to six), 6 not judged; no row without the counter wins a level (`Tools/PlayTest/grace_windows.txt`). The
table below is history.

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

Round 87:

- A family factor: `ProjectGenerator.FamilyRules`; a creature's family: `ProjectGenerator.CreatureFamilies`; a
  turret's retarget time: `ProjectGenerator.TurretRetargetSeconds`. Then
  `bash Tools/PlayTest/stage.sh run ApplyRound87Laser.cs ApplyRound87Laser.Run` and
  `bash Tools/PlayTest/stage.sh run LaserRole.cs LaserRole.Run`.
- The specialist's price rule (`BalanceSheet.SpecialistPriceCap`, premium `BalanceSheet.SpecialistPremium`): on the
  creatures it is bought against, a coin in a specialist buys at least the premium times what a coin in the Gun Turret
  buys there. `Round87Tests` holds the Laser to it on a swarm that overloads it, per level and difficulty.

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
- **Gun Turret and Laser no longer do the same job (round 87).** Per coin on one big soft body the Laser buys a
  little less than the Gun Turret (x0.97); on a swarm that overloads it, clearly more, because the turret pays a swing
  for every creature and the beam does not; on armour and on a boss half, because a beam works on armour at a tenth.
  Numbers per difficulty: `Tools/PlayTest/laser_role_r87.txt`.
- **The Cannon's splash is worth little against soft bodies**: 1.3x over light fire per coin on a bunched mob at level 1,
  about even at level 3. Its real edge is armour (6x). So a level without armour or without bunching never asks for it,
  and a level with both (Landing Site) asks for nothing else. Round 88b capped the blast per level (2 / 3 / 4,
  section 3): at level 1 the Cannon is now under light fire on a soft queue (x0.91 the Gun Turret per coin), and the
  armour edge is untouched.
- **The Railgun is three times worse per coin than the Cannon against armoured troops** (45 against 17). It is single
  target and slow; it earns its price on reach (6 against 4) and on one big armoured body, which Crystal Plains 1 to 6 do
  not have.

## Round 88c: a volley flies, a struck gun shudders, the brood runs ahead (2026-11-21)

- **A boss's volley has a flight time** (`BossAbilityDefinition.FlightSeconds`, 1.1 s on the three level-boss volleys;
  zero lands at once). The damage is dealt when the bolt lands, not when it leaves (`BossAbilities.LandBolts`,
  `VolleyLanded`): the player sees the bolt arrive, then the bar drop. A gun wrecked or sold in the meantime takes
  nothing; a boss that dies takes its bolts in the air with it. Numbers unchanged (14 a bolt, three guns, every 3 s,
  within 5 cells).
- **What the player sees** (views only): a flare of sparks at the boss's head, three large bolts with a wake that are
  lobbed to their guns (`EnemyBoltView.Lob`), a shell of the bolt's colour that bursts over each gun, the hit sound,
  and the gun rocking on its footing (`TowerDisplacementView`: any single blow of 3 % of a structure's health or more).
- **The boss's ground mark is quiet** (`BossAuraView`): a faint shadow, one thin dull-red ring, two small chevrons. No
  white core, no glow band, no ripple. A creature shown outside a fight (the menu backdrop) wears none.
- **The brood runs ahead of its boss**: see Docs/WAVE_GRAMMAR.md, "Round 88c", rule 2.
- **Win rule, checked** (`WaveClock.IsCleared`, `EnemySpawner.Alive`): a level is won when every wave has started, the
  last one has sent its last creature (held nest spawns included) and nothing lives, whatever sent it (a wave, a nest,
  a brood, a split). `Alive` is now the length of the living list itself and a creature is released once only.

