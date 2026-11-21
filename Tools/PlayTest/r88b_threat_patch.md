# Round 88b, package "threat": doc text for the curator to merge

## Docs/COMBAT_RULES.md: new section "Structures: who can hurt a tower, and from how far" (after the family table)

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
   levels, slams what it passes for 3 (`ProjectGenerator.CragbackAttackDamage`), so a tower has something to fear before
   the boss and the Cannon has one more reason to kill it early. The ranged kinds (Stinger, Shardback, the bats) keep
   their damage and now reach a tower between two roads.
3. **Every level boss hurts a tower from `CombatRules.BossReach` (4 cells) or further.** The Brood Mother (Venom Spit), the
   Core Wisp (Arc Discharge) and the Pack Alpha (Spine Volley) carry the same volley: 34 a bolt at each of the three nearest
   guns within 5 cells every 3 s (`ProjectGenerator.BossVolley*`), flagged `SparesBlockers`: gates, the base and the hero
   are left to the boss's own blows. The Storm Matriarch already had her Element Storm (7 cells) and her own bolts now
   reach 4.5 cells by rule 1. The difficulty's structure damage in play (multiplier x factor: 0.45 / 0.45 / 1 / 1.4) scales
   every bite, bolt and pulse once, as before.

On paper (`Tools/PlayTest/r88b_threat.txt`, Round88bThreatLab; share of a level-1 Cannon's 220 hit points a tower three
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
nearest-only bite is shared by two guns, a volley's three bolts by six).

## Docs/GAME_DESIGN.md: "Towers get damaged" paragraph, one sentence to add

Towers are threatened by rule: melee creatures bite the front row, slams reach the second row, stings and bolts reach a
tower between two roads, and every level boss throws a volley at the three nearest guns within five cells (Docs/COMBAT_RULES.md).

## Docs/STATUS.md: Round 88b, sub-section "Tower threat"

- Code: `CombatRules` reach table + `BossReach`, `ReachRule` (new), `EnemyDefinition._rules` + `StructureReach`,
  `StructureRegistry.FindNearest` (blocker range; open-only volley), `Enemy.TryAttackStructure`, `BossAbilityDefinition._sparesBlockers`
  + `StructureReach`, `BossAbilities` (volley passes the flag), `CodexView` ("hurts them with its powers").
- Data: `ApplyRound88bThreat.cs` (reach table, rules on 40 creatures, Cragback attack 0 -> 3 and reward 15 -> 16, three volley
  assets appended to their bosses), `en.data.txt` (three names), `en.ui_menus.txt` (one line). Generator: `ProjectGenerator.Round88b.cs`.
- The balance model has no term for tower wear. The Cragback's attack raises its threat by about 8 % (`Threat.Of`, 0.03 a point)
  and its reward by one coin, which is in the calibration's input hash: solve and re-stamp.
- Tests: `Round88bThreatTests` (13).

## Docs/COMMANDS.md: "Round 88b"

    bash Tools/PlayTest/stage.sh run ApplyRound88bThreat.cs ApplyRound88bThreat.Run
    bash Tools/PlayTest/stage.sh run Round88bThreatLab.cs Round88bThreatLab.After     # r88b_threat.txt (Before writes r88b_threat_before.txt)
    bash Tools/PlayTest/r34b_editmode_worker.sh <N> Round88bThreatTests
