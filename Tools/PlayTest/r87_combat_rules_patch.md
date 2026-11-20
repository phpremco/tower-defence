# Round 87: changes for Docs/COMBAT_RULES.md (paste per section)

Numbers marked `<r87>` come from `Tools/PlayTest/laser_role_r87.txt` and `Tools/PlayTest/r67_role_matrix.txt`; re-read
them after the last apply before pasting.

## Section 1, the rule table: replace the Beam row

| **Beam** | **0.1** | 0.2 | Laser, Cryo, Cryo Beam | A beam glances off plates, and a pulse this small barely marks them; it spreads out on the way up. |

Add under the table (after "How the two columns work in play"):

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

## Section 2: after the trait table, add

**Family** is the one thing typed on a creature (see section 1): `Energy` for the Wisp and the Core Wisp today. The
creature table at the end of `Tools/PlayTest/r67_role_matrix.txt` has a Family column.

## Section 3, tower roles: replace the Gun Turret and Laser rows

| **Gun Turret** | Light fire | The all-rounder, cheap: soft bodies big and small, a fifth in the air. Its turret swings to each new target, so a fast swarm outruns it. Only chips armour. |
| **Laser** | Light fire | The swarm gun: a fast beam of small pulses that is on the next creature at once. Weak on armour (a tenth) and on one big target; cannot aim up. Half again the damage on energy bodies. |

Cannon = slow, clustered, armoured. Laser = fast, spread out, light. Gun Turret = a bit of everything.

## Section 4, the matrix

Replace the Gun Turret and Laser rows with the rows of `Tools/PlayTest/r67_role_matrix.txt` `<r87>`. Note under the
table: the matrix's reference fights do not play the retarget time (they count shots), so it understates the Laser's
edge on the Swarm and Fast columns; `Tools/PlayTest/laser_role_r87.txt` plays it.

## Section 5, upgrade steps: replace the Laser row

| Laser | 4 damage, 9 shots/s, range 4.3 | **Overcharged emitter**: shots/s 9 > 15 | **Focused lens**: damage 4 > 7.2 |

(The Laser's steps swapped order in round 87: its pulse must stay under the Gun Turret's round at every level, 5 / 5 /
10, so the rate comes first, as on the Gun Turret. Prices 130 / 55 / 100 are unchanged: the ratios between its levels
are.)

## Section 7, changing a rule: add

- A family factor: `ProjectGenerator.FamilyRules`; a creature's family: `ProjectGenerator.CreatureFamilies`; a
  turret's retarget time: `ProjectGenerator.TurretRetargetSeconds`. Then
  `bash Tools/PlayTest/stage.sh run ApplyRound87Laser.cs ApplyRound87Laser.Run` and
  `bash Tools/PlayTest/stage.sh run LaserRole.cs LaserRole.Run`.
- The specialist's price rule (`BalanceSheet.SpecialistPriceCap`, premium `BalanceSheet.SpecialistPremium`): on the
  creatures it is bought against, a coin in a specialist buys at least the premium times what a coin in the Gun Turret
  buys there. `Round87Tests` holds the Laser to it on a swarm that overloads it, per level and difficulty.

## Section 8, "What the matrix shows that is not fixed": replace the bullet "Gun Turret and Laser do the same job"

- **Gun Turret and Laser no longer do the same job (round 87).** Per coin on one big soft body the Laser buys a
  little less than the Gun Turret (x0.97); on a swarm that overloads it, clearly more, because the turret pays a swing
  for every creature and the beam does not; on armour and on a boss half, because a beam works on armour at a tenth.
  Numbers per difficulty: `Tools/PlayTest/laser_role_r87.txt`.

## Docs/COMMANDS.md, new section "Round 87"

- `bash Tools/PlayTest/stage.sh run ApplyRound87Laser.cs ApplyRound87Laser.Run`: the rules asset (family table, Beam
  on armour), the creature families, the Laser's levels and sweep, the turrets' retarget time. Safe to run again.
  Tower numbers and factors are in the calibration's hash: re-stamp after it.
- `bash Tools/PlayTest/stage.sh run LaserRole.cs LaserRole.Run` writes `Tools/PlayTest/laser_role_r87.txt`.
- `Game.Editor.StreamDuel`: one tower against a stream through the firing rules (`Cadence`, `BeamBudget`, `Retarget`),
  for lab scripts and tests.
- Never run the whole EditMode suite on the stage editor: `PlanSearchTests` and `CalibrationFitTests` rewrite
  `Tools/PlayTest/Plans` and `calibration*.txt`. Run classes by name.
