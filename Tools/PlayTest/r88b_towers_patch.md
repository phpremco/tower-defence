# Round 88b, tower package: doc text for the curator to merge

Reports this text leans on (all in the stage, `Tools/PlayTest/`): `r88b_cannon_cap.txt` (the paper table),
`r88b_cannon_roles.txt` (the two reference fights at level 1), `r88b_wisp_reach.txt` (what of the ground an air-only gun
reaches).

---

## Docs/COMBAT_RULES.md, section 2 "Creature traits", last paragraph

Replace "a blast takes at most three" by:

> a blast takes as many creatures as its gun's level allows (the Cannon two as built, three and four with its stars; the
> Flak four at every level)

## Docs/COMBAT_RULES.md, section 3 "Tower roles", new sub-section after the table

### The blast cap per level (round 88b)

Remco's playtest of 2026-11-20: the Cannon is needed, but a Cannon-heavy build was the best way to play every level. His
rule: **the Cannon's shell hurts at most two creatures as built, three at level 2 and four at level 3.** The cap is data
per tower level (`TowerLevel.SplashTargets`; zero keeps the tower's own `_splashTargets`, and zero there is the whole
radius), read through one accessor, `TowerDefinition.SplashTargetsAt(levelIndex)`, by everything: the game
(`Tower.Shoot` fires with the cap of its current level, `Projectile.CapSplash` keeps the nearest to the impact), the
balance model (`BalanceLedger.Stats` puts it on `TowerLevelStats.SplashTargets`; `Ledger.SplashCap(tower, level, rules)`
is the one reader for the ledger, the leak model and the route walk), the labs (`RoleMatrix`, `StreamDuel`,
`ArmourTtk.cs`) and the texts (`BlastRead`). `TowerDefinition.OnValidate` refuses a cap on a tower without a blast and a
cap that shrinks with the level.

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

## Docs/COMBAT_RULES.md, section 5 "Upgrade steps", after the table

The Cannon's two steps also raise its blast cap (2 > 3 > 4, round 88b). The cap is not a step of its own: it grows with
the stars beside the step's named thing, the data check (`TowerDefinition.StepProblem`) does not count it, and the tower
menu lists it after the named thing. The rule "an upgrade buys more single-target damage per coin than a second tower"
(`BalanceRules.UpgradeMargin`) is about one target and is untouched: no damage, rate or price moved
(`TowerRolesTests.EveryUpgradeStep_BeatsMoreTowersOfTheSameType` passes as it did).

In the table, the Cannon's row becomes: `| Cannon | 25, 0.6/s, 4.0, blast of 2 | **Heavier shells**: damage 25 > 56.9,
blast of 3 | **High-explosive filler**: damage 56.9 > 118.3, blast of 4 |`.

## Docs/COMBAT_RULES.md, section 4 "The matrix" (open, needs a decision: see STATUS)

The armoured reference fight is "three Cragbacks at a gate", sized when a shell took three. A level-1 Cannon now takes
two of them a shell and needs twice the shells to clear the group, so on that fight it is x1.29 the Tesla per coin (the
Tesla's chain takes all three at a fifth on armour) where the rule asks x2: `CombatRulesTests.TheRoles_Hold_PerCoin(0)`
and `ABrokenRule_IsCaught` are red on it. On two Cragbacks the Cannon is x2.59 the Tesla, as it was on three with a
blast of three. At level 3 the roles hold. Numbers: `Tools/PlayTest/r88b_cannon_roles.txt`.

## Docs/STATUS.md, "Round 88b", the tower package

**The Cannon's blast is capped per level: 2 / 3 / 4** (was 3 at every level). Data on the tower's levels
(`TowerLevel.SplashTargets`), one accessor (`TowerDefinition.SplashTargetsAt`), read by the game, the balance model, the
labs, the input hash and the texts. Flak 4 / 4 / 4, hybrids unchanged (the Artillery keeps the whole radius).
Generator: `ProjectGenerator.Towers.cs` (`CannonSplashTargets = { 2, 3, 4 }`, `Blast(levels, caps)`); without the
generator: `Tools/PlayTest/Scripts/ApplyRound88bTowers.cs` (idempotent; `ProjectGenerator.ApplyBlastCaps`).

**The model follows the game on two things of round 88a.**
- *An air-only gun reaches a creature of both layers.* `Layer.CanHit(mask, layer)` in the model is the game's
  `TargetLayersExtensions.CanHit` (the model's assembly cannot reference the game's; a test holds the two to each other
  over every pair of layers). `WaveGroup.Layer` carries the creature's layers, `WaveLoad.StreamSkyHealth` /
  `BossSkyHealth` the part of the ground on both layers, and `WaveLoad.StreamShareFor(targets)` / `BossShareFor` is the
  one answer to "what of the ground does this gun reach": the ledger's capacity, the leak model's queue fire, the route
  walk (per group, and only groups the gun can shoot at share its time) and the attack drone read it. Before, the Flak was
  worth nothing against the ground in the model. What it now reaches, from the stored waves
  (`Tools/PlayTest/r88b_wisp_reach.txt`): nothing on Landing Site, 5 to 9 % of the ground bar on Glass Meadows, 9 to 15 %
  on Shard Ridge, 9 to 14 % on Fault Line, 14 to 22 % on Quartz Flats, 0 / 0 / 7 / 5 % on Crysium Vein; in single waves up
  to 81 % (Fault Line Psycho). The Flak stays a sky gun to the ledger's purse (`Option.AirOnly`): it is still bought for
  flyers only, but one that stands now also counts against the Wisps of every later wave. The share is read on the
  wave's own creatures (what earlier waves left on the road counts as the same mix), so it does not move with the wave's
  health multiplier and a tougher wave never reads as lighter (`Round83Tests` caught the first version doing that).
- *The Chrono Field's lifetime.* Nothing in the model or the bots carried 20 s or a third of a wave: the plan model
  credited a field for the whole wave it was laid in by a flat constant (`PlanEvaluator.FieldWaveShare = 1`), which a
  20 s field never earned on a long wave and a 60 s field does (the longest ground wave of the continent keeps coming
  55 s). The constant is now a function of the data: `FieldWaveShare(field, load)` = the field's lifetime
  (`GadgetStats.Lifetime`, from `PathObjectDefinition.Lifetime`) over the wave's ground span (`WaveLoad.Span`), at most one. With 60 s it is
  one on every wave whose walkers spawn within a minute (all of them: 55 s at most); only a boss wave whose brood keeps
  coming over the boss's whole walk has a longer span, and a field there is now credited for its minute and not for the
  whole walk (not measured how many waves that is). A shorter field would be credited for its share. The bots lay
  one field a wave and weigh it against a gun over the waves left (`BalanceProbeTests.FieldPays`), which is the same
  rule; nothing to change there. A field that outlives its wave into the next one is not credited (the model drops it
  at the wave's end): a small under-count, left as it is.

**The calibration's input hash moved** (the Cannon's caps, every splash gun's cap per level, the gadgets' lifetime): the
stored calibration reads as stale until the curator re-stamps it. The model's own numbers moved too wherever a Flak
stands on a level with Wisps (Glass Meadows to Crysium Vein).

**Open: the armoured role at level 1** (see the COMBAT_RULES note above). Two tests red by the new rule, not touched:
the reference fight is rule data and the choice is Remco's. Either the armoured reference group becomes two Cragbacks
(what a Cannon as built takes; the generator's `Scenario(EnemyTraits.Armoured, "Cragback", 3, ...)` in
`ProjectGenerator.Damage.cs` and `_scenarios` in `CombatRules.asset`), or the Tesla's chain gets a cap of its own, or the
margin on that fight is a level-3 rule only. Also on that page: with a blast of two the level-1 Cannon clears the mob
reference fight (five Oozers at a gate) at x0.91 the Gun Turret per coin and x0.95 the Laser (it was x1.37 and x1.42); the
mob role still holds in the test because the Tesla is the best mob tower, but on a level without the Tesla the level-1
Cannon is no longer the better buy against a soft queue. That is the rule doing its work ("counter to plates, crowd-breaker
by upgrading"), and it is worth saying to Remco in those words.

**Texts that still speak of the old Cannon** (not changed; story and rule data are not this package's):
`en.data.txt` `Dlg_Scan_Prowler` "The Cannon's splash hits the whole pack at once" (a pack is three; a level-1 shell
takes two), and the mob reference fight's reason "one blast takes three" (`CombatRules.asset`, generator
`ProjectGenerator.Damage.cs`).

## Docs/COMMANDS.md, "Round 88b"

```
# the Cannon's blast caps without the generator (idempotent), then the paper tables
bash Tools/PlayTest/stage.sh run ApplyRound88bTowers.cs ApplyRound88bTowers.Run
bash Tools/PlayTest/stage.sh run CannonCapLab.cs CannonCapLab.Run      # -> Tools/PlayTest/r88b_cannon_cap.txt
bash Tools/PlayTest/stage.sh run CannonCapLab.cs CannonCapLab.Roles    # -> Tools/PlayTest/r88b_cannon_roles.txt
bash Tools/PlayTest/stage.sh run WispReachLab.cs WispReachLab.Run      # -> Tools/PlayTest/r88b_wisp_reach.txt
bash Tools/PlayTest/r34b_editmode_worker.sh <N> Round88bTowersTests
```
`StreamDuel.Dps(tower, level, stream, step, splashTargets)` plays a tower with another blast cap than its data has (a
lab's what-if).
