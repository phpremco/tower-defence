# Wave grammar (round 79)

How the creatures of a wave arrive. Remco, 2026-11-17: surprises ("everything stands at the front gate, and poof, something
comes behind it"), waves that are long and distributed (spread clusters or big swarms, small pauses, combo swarms), and
tanks that draw the fire while the small ones slip past. And: general rules and factors, so a new level is quick to make.

The grammar sits on the existing pipeline and replaces nothing in it:

- **The script** (`WaveScriptSpec` in a level's spec) is the level's character: the roster (`TroopSpec`: which creature,
  from which wave, from the gate or out of a rift), the rhythm (one letter per wave), the raids, the bosses, the teasers.
- **The grammar** (`WaveGrammar`, `WaveTimeline`, numbers in `BalanceRules.Grammar` = `WaveGrammarRules`) decides how a
  wave's creatures are laid out in time and which waves carry a surprise. One set of numbers per difficulty for every level.
- **The solver** (`WaveSolver`) still only sets how tough the creatures are. The grammar never changes how many creatures
  a wave has, so the kill money of a level is what the script's budget says (test: `TheGrammar_MovesCreaturesInTime_NeverTheirNumber`).

A new level therefore needs a roster, a rhythm and a map. Where the clusters, the breathers, the surprises and the finale
fall follows from the rules below.

## The vocabulary

A wave has **one front pattern** (`WavePattern`, kept on `WaveSpec.Pattern`), is cut into **clusters** by **breathers**, and
may carry **one surprise** (`SurpriseKind`).

| Pattern | What the player sees | Picked when | Parameters (`WaveGrammarRules`) |
| --- | --- | --- | --- |
| Stream | One short mixed stream | A plain wave too short to cut (the opening waves) | - |
| Spread clusters | The mixed stream in two to four clusters with a short silence between | Rhythm `P` | `BeatSeconds` 7, `MinBeatShare` 0.7, `MaxBeats` 4, `Breather` 6 / 5 / 4.5 / 4 s |
| Swarm | One long surge of the two cheapest creatures, close together | Rhythm `R` | `SwarmBeatSeconds` 14 (a surge is cut later than a stream) |
| Tank escort | The toughest troop walks in first; the others come close together and start late enough to run into its back | Rhythm `H` | `EscortCatchCells` 10, `MaxEscortLag` 14 s, `EscortTempo` 0.7, `EscortBeatSeconds` 22 |
| Combo swarm | Every troop of the level at once, no head start for anyone | Every second plain wave from `ComboFrom` 0.45 of the level on | `ComboTempo` 1, `ComboBeatSeconds` 12 |
| Air raid | A packed raid over a thin ground stream | The script's raid waves (unchanged) | the script's |
| Breather | Silence between two clusters | Whenever a front is longer than about a beat and a half | `Breather` per difficulty |
| Surprise: rift | The level's rift troops surface mid-wave, behind the gate where the level has such a rift | See "Surprises" | `SurpriseShare` 0.12 / 0.18 / 0.22 / 0.26, `SurpriseAt` 0.5, `SurpriseMinDelay` 12 s |
| Surprise: flank | A share of the wave comes down the second road alone, mid-wave | A level without rift troops but with a second road | the same, `FlankFrom` 0.35 |
| Boss entrance / finale | The crowd in clusters with long breathers; the boss walks in with the last cluster | Every boss wave; the last wave takes three clusters | `BossBeats` 2, `FinaleBeats` 3, `BossBreather` 9 / 8 / 7 / 6 s, `MinBossBeatSeconds` 3 |

Notes on what is emergent and what is not:

- **Tank escort is timing only.** The escort starts `EscortCatchCells x (1 / tank speed - 1 / escort speed)` seconds behind
  the first tank, so the fastest escort reaches the tanks' back after ten cells of road, about where the first guns stand.
  Guns that shoot the creature furthest along are busy on the tanks when the small ones arrive, and at a gate the queue
  mixes both. There is **no taunt** and no change to targeting; a player who retargets beats it. A taunt mechanic would
  need Remco's yes.
- **A cluster is not a new group of creatures.** `WaveTimeline.Spread` cuts the composed front at equal moments and starts
  each cluster one breather after the last creature of the one before (a silence that was already there, such as the
  escort's lag, is not added to); a group that runs across a cut is split. Inside a cluster the creatures keep the order
  and spacing the composer gave them (packs, loose runners, lanes).
- **A combo swarm keeps the troops' own spacing.** At three quarters of it the ledger's Easy player could not shoot that
  many bodies in the window whatever their health, and the solver left paper creatures (x0.14); "all at once" is the pattern.
- **Harder is tighter.** A cluster's contents shrink with the difficulty's spawn tempo; the breather is fixed seconds per
  difficulty and shorter on the harder ones.

## How a wave is picked

1. An air raid wave of the script is an air raid.
2. Otherwise the rhythm letter gives the front: `R` swarm, `H` tank escort, `P` plain.
3. A plain wave is spread clusters; from `ComboFrom` of the level on, every second plain wave is a combo swarm.
4. The front is cut into clusters of about a beat (`WaveTimeline.Beats`), never more than `MaxBeats`, with the
   difficulty's breather between them. A front shorter than a beat and a half stays whole.
5. A boss wave is cut into `BossBeats` clusters (the last wave of the level: `FinaleBeats`) with the long boss breather,
   whatever its length, and the boss walks in with the last cluster.
6. A surprise rides on the wave when the surprise rule says so.

The rhythm string stays the level's own (variety between levels); the ramp comes from the script's budget curve and the
solver's target curve, as before. The old letter `M` (ambush) is gone: surprises are placed by rule, not written per level.

## Surprises

- **Source.** A level with rift troops (a `TroopSpec` with the nest source) surprises through its rifts only, once those
  troops have joined; a level without them uses its second road once `FlankFrom` of its waves have passed. A level with
  neither has no surprise.
- **A surprise never introduces the sky.** Flyers out of a rift wait for the difficulty's first air raid: the raid is the
  announced first meeting with flyers (the Easy raid rule and the first-flyer rule place it where a sky gun is payable),
  the rifts follow. Measured on paper: one Shalebat out of a rift in wave 5 of Fault Line Easy made the ledger's player
  buy a sky gun out of a thin purse and the calibration halved the health of the whole level.
- **Where they may fall.** Never in the surprise-free opening (`SurpriseFreeWaves` 4 / 3 / 2 / 2 waves), never on an air
  raid, a swarm, a boss wave or the last wave (those are events of their own; a swarm also carries a high health
  multiplier that a rift's tanks would inherit: three Cragbacks at x5.4 behind the gate ended every skilled run of Glass
  Meadows Easy in the first measurement). The first wave that may carry one does; the next comes `SurpriseSpacing` waves
  later at the earliest (4 / 3 / 3 / 2). So there is never more than one surprise per wave and never more than one in N waves.
- **How big.** `SurpriseShare` of the wave's budget comes as the surprise, the rest at the front; the wave is no bigger
  for it. A surprise is never empty: where the share buys no creature yet (an early wave, a heavy rift troop), one comes.
  The growth of plated troops past their grace window (round 68) is the front's alone: a surprise is sized by its share
  (with the growth on top, seven Cragbacks came out behind the gate of Glass Meadows on Psycho where the share asked two).
  The shares are the second set tried: at 0.2 / 0.3 / 0.35 / 0.4 the rifts behind the gate of Glass Meadows and Quartz
  Flats took the base on Hard for both reference players.
- **When in the wave.** Halfway through the front's spawning, never before twelve seconds: the player is committed at the
  front. (A rift troop keeps its own delay on top, seven seconds in the first continent.)
- **Fairness.** Nothing new was needed: a rift that a wave will use shows its marker on the ground before it opens and Voss
  warns when a rift behind the gate opens for the first time (`RearNestWatch`); a rift's walkers always use the troop's
  authored nest, so the lesson is the same on every run. The next-wave preview (round 78) lists the surprise's creature
  types and numbers with the rest of the wave and never says where they come from. A flank walks in at the second road's
  start in plain sight.
- **A type's first appearance** stays what it was: the troop's first wave in the script, the first-flyer rule
  (`BalanceRules.FlyersAfterBounties`), the grace window and the teasers. The grammar adds nothing there.

First continent (the level's own roster decides the source):

| Level | Surprise | Out of |
| --- | --- | --- |
| Landing Site | Cragbacks | the rift behind the first gate (as before, from wave 7; now repeated by the spacing) |
| Glass Meadows | Cragbacks, from wave 7 | its one rift, which lies behind the first gate (new troop in the script) |
| Shard Ridge | a flank | the second road |
| Fault Line | Shalebats | its three rifts (drawn per bat, as before) |
| Quartz Flats | Shalebats, from wave 9 with Cragbacks | bats: both rifts; Cragbacks: the rift behind the first gate (new troop) |
| Crysium Vein | Shalebats | its rift |

## The finale rule

The known problem (rounds 77 and 78): a last wave that takes the whole base at once (Quartz Flats wave 17: 750 to 800 base
health in one wave), so a run is lost from a full base. The boss used to walk in fourteen seconds into one block of crowd.

The rule, for every boss wave and with one cluster more for the last wave of a level: the crowd comes in clusters with a
long breather between them (`BossBreather`, 6 to 9 seconds), and the boss walks in with the last cluster. A defence that
bends on the first cluster has the breather to clear the creatures at its base, mend and bring the hero back before the
boss; a boss's own powers no longer fall on the whole crowd at once. The wave's size and the boss's health are untouched.

## How the balance model stays honest

- **Counts and money**: unchanged by construction (the grammar only moves creatures in time). `KillPurse` books every kill at
  its own spawn moment plus the walk, so a surprise's or a late cluster's coins arrive when they would in play.
- **Breathers**: a wave's window used to be "start to last spawn". With breathers that would hand the towers firing time
  in which nothing is in reach, and the solver would answer with tougher creatures. `WaveLoad.Span` is now the seconds in
  which something is really coming (`WaveTimeline.ActiveSeconds`): of every pause only `PauseCredit` (2 s) counts, the rest
  is idle. The same for the flyers' window. Density (splash) is read against the same active seconds, so clusters count as
  dense as they are.
- **Surprises**: the groups carry their source and nest, so the existing reads apply: `WaveLoad.Reach` and `RouteKill` give a
  rift behind the gate only the defence that lies behind its join cell, a flank the second road's share.
- **Tank escort and combo**: only spacing and delays, which the ledger reads from the groups (span, density, carried health).
- **Solver**: a wave with a surprise is solved on its own difficulty (its share and its place differ per difficulty), like
  an extra raid or boss (`WaveSolver.SolveVariant`).
- **What the model does not see**: that a tank in front costs single-target guns time on the small ones behind it, and that
  a breather lets the base's guns clear besiegers. Both are measured by the pressure probe, not predicted.

## Validation

- `WaveGrammarTests` (EditMode): the cuts keep every creature and put the breather between the clusters; the idle part of
  a pause is no window; patterns follow the rhythm; surprises keep their spacing and stay off the opening, raids, boss waves
  and the last wave on every level and difficulty; every level of the first continent has a surprise; a rift opens
  mid-wave through the authored nest and on surprise waves only; a flank comes down the second road alone; a boss walks in
  with the last cluster after a breather; the escort's lag; counts never change.
- `LevelValidator` (rule `WaveShape.Check`, on every stored wave as the scheduler plays it): no silence between two spawns
  longer than `WaveGrammarRules.MaxPauseSeconds` (16 s), no wave that spawns longer than `MaxWaveSeconds` (120 s), walkers
  out of at most one authored nest per wave (one surprise per wave).
- `python Tools/PlayTest/wave_shape.py [--rev <revision>] [--waves C1_05:1]`: spans, clusters and creature types of the
  stored waves, per cell or wave by wave.

## Tried on paper or in play and not kept (round 79, second half)

Each was reverted; the stored waves are the first half's (data stamp `c75b37f7`). `WaveLedgerLab.cs` (Docs/COMMANDS.md,
"Round 79") reproduces the paper numbers.

- **The flyers' window without the time before the first flyer.** `WaveLoad` counts a wave's flyer window from the wave's
  start, so two bats of a rift that opens after nineteen seconds read as twenty seconds of fire for the sky guns (Quartz
  Flats Hard wave 9: `sky 20.1 s` for two bats). Counting from the first flyer is the honest clock, but the ledger's air
  side has nothing else to stand on: a flyer is shot at along its whole flight and while it bites the base, and the model
  only knows the spawn window plus one pass. With the honest clock one Shalebat made wave 7 of Fault Line Medium a paper wave
  (x1.58 -> x0.32 for every creature in it, the solver on its floor), Quartz Flats Easy wave 10 fell x2.37 -> x0.74 and a
  flying boss took its whole wave down (Quartz Flats Hard wave 11 x2.52 -> x0.92) unless it kept its old fourteen seconds.
  In play both reference players win those waves as stored. Not kept; the air model is the thing to mend, in a round of its own.
- **A surprise wave that keeps Medium's multiplier** instead of being solved on its own difficulty. It does not soften the
  surprise waves as a rule: Glass Meadows Psycho wave 10 stays on the hero cap (x2.2), Quartz Flats Hard wave 9 rises
  (x2.10 -> x2.50), Shard Ridge Psycho's flank wave 7 rises sevenfold (x0.33 -> x2.34) and Fault Line Easy grows 14 %. Not kept.
- **A builder scale for Glass Meadows Psycho** (0.757 -> 0.68): the waves that decide the cell sit on the hero cap (7, 9, 10,
  12) and do not move with the scale; measured `3 2 0 0 0` for the skilled rung before and after
  (`pressure_r79b_gm_psycho_records.txt`). Not kept.

## Known and open

- **A surprise costs bodies.** Its share is taken from the front's budget and spent on the rift's troop; where that troop is
  heavy (a Cragback) the wave has fewer creatures, and a pack species whose share falls under half a pack sends none
  (Glass Meadows Psycho wave 10: 58 creatures in round 78, 33 now; Quartz Flats Psycho wave 14: 112 -> 89). The solver
  answers with tougher creatures (wave 10: x1.19 -> x2.20, the wave's health +37 %), so a surprise wave pays a little less
  and each body is harder. Over a level it is small (Glass Meadows Psycho: 731 -> 704 creatures). The test
  `TheGrammar_MovesCreaturesInTime_NeverTheirNumber` covers the cuts, not the surprise.
- **A raid of a harder difficulty copies Medium's multiplier** when Medium has the same raid in its script (before the
  flyer floors move Medium's raids), as before the grammar. Quartz Flats wave 13: the Medium solve now gives that raid
  x2.46 before the calibration (round 78: x1.18; without surprises x1.98, with the cuts alone about x1.9), and Easy, Hard and
  Psycho take it over (Hard x1.02 -> x2.25). No single rule explains the step; the level now really has its rift bats (they
  were in the script from wave 6 but never came: the level had no ambush wave), and the cuts give the towers more time.
- **The reference players and the rear rift:** both put one gun at the gate a rift's walkers reach. On Glass Meadows the
  walkers of the rift behind the first gate meet no gate and walk to the base (every surprise Cragback of the skilled runs on
  Psycho reached it). A matter for bot version 4.
