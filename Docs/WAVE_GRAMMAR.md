# Wave grammar and the balance algorithm (round 79; rules of rounds 80 to 83 marked)

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
- **The closed loop** (`Tools/PlayTest/closed_loop.py`, round 83) sets how tough the level is as played: one scalar per
  level and difficulty, measured, never typed.

A new level therefore needs a roster, a rhythm and a map. Where the clusters, the breathers, the surprises and the finale
fall follows from the rules below; how tough it is follows from "How to balance a new level" at the end of this file.

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
| Surprise: rift | The level's rift troops surface mid-wave, behind the gate where the level has such a rift | See "Surprises" | `SurpriseShare` 0.12 / 0.18 / 0.22 / 0.26, `SurpriseAt` 0.5, `SurpriseMinDelay` 12 s, `MinSurpriseGroup` 3 |
| Surprise: flank | A share of the wave comes down the second road alone, mid-wave | A level without rift troops but with a second road | the same, `FlankFrom` 0.35 |
| Surprise: authored | What the level writes for that wave: any creatures, any number, out of any of its surprise points at once | The level's own schedule (`WaveScriptSpec.Surprises`), see "Writing a level's surprises" | the entry's |
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
6. A surprise rides on the wave when the level's own schedule writes one for it, else when the surprise rule says so.

The rhythm string stays the level's own (variety between levels); the ramp comes from the script's budget curve and the
solver's target curve, as before. The old letter `M` (ambush) is gone: surprises are placed by rule, not written per level.

## Which roads a wave uses (round 82)

Remco, 2026-11-18: any subset of the main paths in a wave, varied across the level, Easy mostly simple, Psycho more
combinations of roads; by rule, and a level may overrule it.

A level that forks has two main roads (Shard Ridge, Fault Line, Crysium Vein; the composer's `Road` and the level's
second road). `WaveGrammar.RoadsOn` picks per wave and difficulty which of them the front walks in on (`MainRoads`:
`First`, `Second`, `Both`), in this order:

1. **The level's own plan** (`WaveScriptSpec.RoadPlan`, helper `OnRoads(wave, MainRoads.Second)` or
   `OnRoads(wave, Hard, Psycho, MainRoads.Both)` in the level's spec, next to `Surprises`). Validated by
   `RoadSchedule.Check` (the solve stage logs `[Validator]`): the wave exists, a road is named, and a second road only on
   a level that has two main ground paths.
2. **A flank wave**: the flank comes down the second road alone, so the front keeps to the first road.
3. **A boss wave and the last wave**: every road.
4. **The rule**: `WaveGrammarRules.MultiRoadShare` (0.3 / 0.45 / 0.6 / 0.75) of the waves come down both roads, on the
   whole-number steps of the share (Easy: waves 4, 7, 10, 14, 17; Psycho: 2, 3, 4, 6, 7, 8, ...), so they are spread
   evenly and the first wave of a level is always one road. The other waves take one road each, in turns
   (`SecondRoadEvery` 2: the first road, the second, the first, ...). A share of 1 on every difficulty is the behaviour
   before round 82 (every wave down every road).

The plan of every cell: `WaveGrammarReport.cs` prints `-` (first road), `_` (second) or `=` (both) behind each wave of a
level that forks.

How the model budgets it: the choice only sets the path of the front's groups, and the ledger already reads a wave by
its groups (`WaveLoad.Reach` the defence a lane meets, `KillPurse` the walk to the defence and so when a kill pays and
which wave a slow creature arrives in, the route model the guns a route passes). The number of creatures and the kill
money are the script's whatever the roads (test `TheLevelsOwnRoads_ReplaceTheRule_OnTheirWaveAndDifficulties`). Every
difficulty is solved on its own since round 83, so a wave needs no Medium twin.

Known: on Shard Ridge the second road is short (46 cells against 109) and skips the first two gates. A slow plated
creature on the long road arrives waves later (`KillPurse.Carried`), one on the short road in its own wave; where
carried Cragbacks are what asks, no multiplier of the wave itself reaches its target. Until round 82 such a wave fell to
the solver's floor (ten "paper" waves in the stored set); since round 83 it keeps the lower edge of its band ("The
solver", below; test `Round83Tests.NoStoredGroundWave_IsPaper`).

## Surprises

Remco's design (2026-11-17, round 81), which the rest of this section follows:

- **All spawn points are the same kind of thing; each has a role in the level's data.** A level's spawn points are the
  starts of its ground paths (the route gates) and its nests (the rifts). `LevelDefinition.RoleOfPath` /
  `RoleOfNest` (`SpawnRole`): a ground path is a **main** path unless the level lists it in `_surprisePaths`; a nest is a
  **surprise** point unless the level lists it in `_mainNests`. The front of a wave comes through the main points, a
  surprise through surprise points. It is a role, not a different mechanism: the same spawn groups, the same scheduler.
- **Any combination is allowed in one wave**: any main paths together with any surprise points, one or all of them. No
  validator rule limits a wave to one nest (round 79's rule "walkers out of one authored nest per wave" is gone).
- **What comes out of a surprise point can be written per level and per wave** ("Writing a level's surprises" below).
  A small group, even one big creature, is fine when the level wrote it.
- **The grammar still makes a default plan by rule** (below) for every wave the level does not write. The default is
  never a stray creature: round 79's filler ("a surprise is never empty: where the share buys no creature, one comes")
  is gone. A default surprise is a real group of at least `WaveGrammarRules.MinSurpriseGroup` creatures (3, a data value;
  all its groups together, before the difficulty's count multiplier), and a wave whose share buys fewer carries none: the
  next wave that can takes it. What Remco saw in play, one Cragback walking the road behind a gate, was that filler and
  the Easy share buying a single heavy troop.

### Writing a level's surprises

In the level's spec (`LevelSpecs`, the `Waves = new WaveScriptSpec { ... }` block), next to the roster, the rhythm, the
bosses and the teasers:

```csharp
Surprises = new[]
{
    // Wave 6, every difficulty: three Cragbacks out of nest 2.
    Surprise(6, FromNest(2, "Cragback", 3)),
    // Wave 9 on Hard and Psycho only: a swarm of bats out of nests 1 and 3 at once, and a file of Skitters in
    // clusters down surprise path 4. Easy and Medium keep the grammar's default on wave 9.
    Surprise(9, Hard, Psycho,
        FromNest(1, "Shalebat", 8, WavePattern.Swarm),
        FromNest(3, "Shalebat", 8, WavePattern.Swarm),
        DownPath(4, "Skitter", 12, WavePattern.SpreadClusters)),
    // Wave 11: no surprise at all, whatever the default plan would have put there.
    Surprise(11),
},
```

- An entry (`SurpriseEntrySpec`): the creature, how many, the point (a nest counted from 1 in the level's nests, or a
  ground path's index), the pattern (`Stream` one file at the creature's roster spacing, `SpreadClusters` that file cut
  into clusters with the difficulty's breather, `Swarm` close together as a rush wave), and optionally its own spacing
  (`interval`) and a delay after the surprise's moment (`delay`).
- A written surprise **replaces the default on that wave** for the difficulties it names; the other difficulties and the
  other waves keep the default plan, which counts a written surprise for its spacing. It may fall on any wave, a raid, a
  swarm or a boss wave included: that is the designer's call.
- **The wave is no bigger for it.** The front gives up the threat the entries cost, so the level's kill money stays what
  the script's budget says; entries that cost more than the whole wave leave no front and open at once. The solver and
  the ledger read the wave as composed (the groups carry their nest or path), so the health multiplier is sized for
  whatever is written, and `KillPurse` books each kill where it happens.
- The count is the script's, like every count in a wave: the difficulty's count multiplier (0.9 / 1 / 1.3 / 1.65)
  applies in play. Write an entry per difficulty where the exact number matters.
- It comes mid-wave like every surprise (halfway the front, never before twelve seconds).
- **Validation.** `SurpriseSchedule.Check` (the solve stage logs every error as `[Validator]`; the tests
  `WaveGrammarTests.TheSurpriseSchedule_IsCheckedAgainstTheLevelsSpawnPoints` and `EveryLevelsOwnSurprises_FitItsSpawnPoints`):
  the wave exists, every entry has a known creature and a count of at least one, its point exists and has the surprise
  role, a nest sends only creatures of its element (in the first continent: Cragback and Shalebat out of the stone
  nests), a path only walkers. `LevelDefinition.OnValidate` checks the roles themselves (a surprise path is a ground path
  and never the only one; a main nest exists) and, as before, every stored spawn group (the nest exists and fits the
  creature, the path exists; every nest leaves its walkers a real walk to the base).
- After writing one: recompile, read `compilationFailed`, run the solve stage (Docs/COMMANDS.md, "Round 79"), then
  `WaveGrammarReport.cs`: a written surprise shows as `*`.
- Not there yet: no road template lays out a surprise **path** (the generator writes every ground path as main), so in
  the six levels as they are a surprise comes out of nests; `_surprisePaths` and `DownPath` are ready for the first
  layout that has one. Landing Site (round 81), Glass Meadows and Quartz Flats (round 82) write surprises of their own.

### The default plan (the rules)

- **Source.** A level with rift troops (a `TroopSpec` with the nest source) surprises through its rifts only, once those
  troops have joined; a level without them uses its second road once `FlankFrom` of its waves have passed. A level with
  neither has no surprise.
- **A rift's flyers do not wait for an air raid (round 80, Remco: "a bit of panic belongs to it").** Round 79 held them
  back until the difficulty's first raid; that guard is gone, on every difficulty. What still holds any flyer back,
  whatever sends it, is the purse:
  - Easy and Medium: the first-flyer rule of round 77 (`BalanceRules.FlyersAfterBounties`, unchanged): no flyer before
    the bounties pay for a sky gun by the end of the grace window.
  - Easy only: the flying troops also wait for the wave of the Easy raid rule (round 22f: the expected player's kills
    have paid for a sky gun, plus a wave; `CampaignModel.EasyRaidFloor`). Before round 80 that rule moved only the raids.
    Without it the guard's removal put one Shalebat in wave 5 of Fault Line Easy, the ledger's Easy player bought the sky
    gun out of its ground defence and the human calibration took 38 % off the whole level (waves at x0.12 to x0.19);
    Quartz Flats Easy lost 14 %, Crysium Vein Easy 18 %. It is a purse rule, not a wait for the raid: on Easy a rift's
    bats may still come before the first raid when the purse is there. **Approved by Remco (2026-11-17).**
  - Hard and Psycho: nothing. Crysium Vein Hard meets its rift's Shalebats in wave 6, two waves before its first raid.
- **Where they may fall.** Never in the surprise-free opening (`SurpriseFreeWaves` 4 / 3 / 2 / 2 waves), never on an air
  raid, a swarm, a boss wave or the last wave (those are events of their own; a swarm also carries a high health
  multiplier that a rift's tanks would inherit: three Cragbacks at x5.4 behind the gate ended every skilled run of Glass
  Meadows Easy in the first measurement). The first wave that may carry one does; the next comes `SurpriseSpacing` waves
  later at the earliest (4 / 3 / 3 / 2). So there is never more than one surprise per wave and never more than one in N waves.
  **A surprise may fall on a tank escort wave (Remco's decision, 2026-11-17):** that is the behaviour as built, and no
  rule is to be added against it.
- **How big.** `SurpriseShare` of the wave's budget comes as the surprise, the rest at the front; the wave is no bigger
  for it. **A default surprise is a real group** (`MinSurpriseGroup` 3, round 81): where the share buys fewer (an early
  wave, a heavy rift troop) the wave carries none and the first later wave that can takes it. Several rift troops may
  open several nests at once; the group is the whole surprise (Quartz Flats: bats out of both rifts and a Cragback out
  of the rear one).
  **No walker leaves a spawn point alone (round 83).** Where the share buys a single walker for a rift or a road, it
  stays home and what it would have cost makes the wave's other creatures a little tougher, like any budget a wave could
  not spend (`WaveComposer.DropLoneWalkers`). Two is the least, so it holds as played on every difficulty. One big
  creature out of a rift is the level's to write. Test: `Round83Tests.TheDefaultPlan_SendsNoWalkerAlone_OutOfARift`.
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
| Landing Site | none since round 81 | its rift troop is the Cragback alone and no wave's share buys three of them (threat 15 a head; three cost more than wave 7 has); a written surprise is the way to open that rift |
| Glass Meadows | the level's own since round 82: two Cragbacks in wave 10, Medium and up | its rear rift (the default plan still sends none: one rift, one heavy rift troop) |
| Shard Ridge | a flank | the second road |
| Fault Line | Shalebats | its three rifts (drawn per bat, as before) |
| Quartz Flats | Easy and Medium: the level's own since round 82 (Easy wave 14: bats out of both rifts; Medium waves 12 and 16: two Cragbacks out of the rear rift with bats out of the other). Hard and Psycho: the level's own since round 83 (Hard waves 10 and 12, Psycho wave 12: one Cragback, two as played, out of the rear rift with bats out of the other; none in the tank escort of wave 14, where Hard was lost); the other surprise waves keep the default, which is bats only | bats: both rifts; Cragbacks: the rift behind the first gate |
| Crysium Vein | Shalebats | its rift |

## Rules of rounds 80 and 80b that round 83 took out

The critic of 2026-11-18 (Docs/BALANCE_CRITIC_2026-11-18.md) traced four rules to two properties of the solver itself:
the model's player re-bought its defence for every trial multiplier (so pressure was not monotone in health), and one
common factor held a level's mean (so lowering one wave lifted the others). Round 83 mended both properties ("The
solver", below), and the rules went:

| Rule | What it repaired | Why it is not needed |
| --- | --- | --- |
| A raid is capped at the sky guns held (`RaidsCappedAtHeldGuns`, the 5 % walk to the lowest crossing) | A raid met its target two or three times, with a sky gun's next step bought in between | The purchase is decided before the multiplier: one crossing, a plain bisection. A raid is sized against the sky guns the player plans for it |
| A surprise lifts no other wave (`SurprisesLiftNoWave`, the second "calm" solve) | The common factor put what the hero cap took off a surprise wave onto every other wave | Every wave has its own target; a capped wave is just lower. Test `WaveGrammarTests.ASurprise_MakesNoOtherWaveTougher` |
| The relief bound (`SurpriseReliefMost` 0.05) | The rule above left levels under the 6 % curve gate | No level mean is held and the gate is gone |
| Raids out of the plain mean, as a special case of the calibration | Capped raids pushed the ground waves up through the mean | There is no mean to push. `BalanceChecks.PlainMean` (a reading aid now) still leaves raids out |
| The guard gun of a rear rift (`RearRiftGuardCells`, `BooksRearGuard`) | The model's purse was all at the front while both reference players stand a gun at a far rift | On paper it moved six waves of Glass Meadows and Quartz Flats by 2 to 14 % (`wave_ledger_r83_noguard.txt`); the cell's scalar is measured with that gun standing, so the level carries it |
| The scale refit (`ScaleRefit.cs`) and the 24 builder scales | Re-expressed every scale after a rule changed what the mean was read on | A cell's scalar is a plain factor on creature health and comes from the closed loop |

Kept, because they are rules about the game: the grammar, `MinSurpriseGroup`, spawn-point roles and schedules, road
plans, `FlyersAfterBounties`, the Easy purse rule, `RaidHealthFloor`, the band and its 2.5-times-the-median gate.

## No ground wave far tougher than the waves before it (round 82)

The Medium solve always held a ground wave's multiplier inside a band of the three ground waves before it (at most
x1.3 over the toughest, `WaveSolver.MaxRise`). Two holes let a swarm through at three times its level's median
(Quartz Flats Hard wave 15: x6.07, 16 300 health where the finale has 10 400; Psycho x5.62; Crysium Vein Psycho wave 16
x6.36), and half of that wave's health reached the gates in the lost runs:

- a wave solved on its own difficulty (`SolveVariant`: Medium has a raid there, or a surprise, or other roads) was
  searched over the whole range, without the band;
- a swarm whose groups are capped at 64 creatures carries what it could not buy as health on top of the solved
  multiplier (`WaveComposer.Compose`, the shortfall), and the band looked at the multiplier before that lift.

Since round 82 the band holds on the creatures as they walk in (the lift included). A raid stays free. Since round 83
there is one solve per difficulty and the band has a lower edge too ("The solver", below). Gates on the stored waves:
`WaveGrammarTests.NoStoredGroundWave_IsFarTougherThanItsLevel` (no ground wave above 2.5 times the median of its level
and difficulty) and `Round83Tests.NoStoredGroundWave_IsPaper` (none under 0.4 times it, the opening waves and the crowd
of a boss wave aside).

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
- **Solver**: every difficulty is solved on its own, wave by wave, each wave to its own target ("The solver", below).
- **What the model does not see**: that a tank in front costs single-target guns time on the small ones behind it, and that
  a breather lets the base's guns clear besiegers. Both are measured by the pressure probe, not predicted.

## Validation

- `WaveGrammarTests` (EditMode): the cuts keep every creature and put the breather between the clusters; the idle part of
  a pause is no window; patterns follow the rhythm; surprises keep their spacing and stay off the opening, raids, boss waves
  and the last wave on every level and difficulty; every level of the first continent has a surprise; a rift opens
  mid-wave through the authored nest and on surprise waves only; a flank comes down the second road alone; a boss walks in
  with the last cluster after a breather; the escort's lag; counts never change; a default surprise is a group or the wave
  carries none; a level's own surprise replaces the default on its wave and difficulties, out of several points at once,
  and the front pays for it; the schedule is checked against the level's spawn points.
- `LevelValidator` (rule `WaveShape.Check`, on every stored wave as the scheduler plays it): no silence between two spawns
  longer than `WaveGrammarRules.MaxPauseSeconds` (16 s), no wave that spawns longer than `MaxWaveSeconds` (120 s). Any
  combination of spawn points in one wave is valid (round 81). The default plan's group rule is checked where the plan
  is made (`WaveGrammarTests.ASurprise_IsARealGroup_AndAWaveWhoseShareBuysFewerCarriesNone`), not on the stored waves:
  there a written surprise of one creature is as valid as a swarm.
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
- **The step of Quartz Flats wave 13** (the raid at x1.18 in round 78, x2.46 in round 79) was the solver's two crossings:
  gone with the monotone player of round 83 (test `Round83Tests.AWavesAirPressure_NeverFalls_AsItsFlyersGrowTougher`).
- **The rear rift of Glass Meadows costs the front a gun (round 80, the cause of Glass Meadows Psycho).** The road index
  of every gate and rift (`Tools/PlayTest/r80_gates.cs`): Landing Site's rear rift joins the road 3 cells behind the first
  gate, where a gun that guards it still fires on the front; Glass Meadows' rift joins 24 cells behind the first gate
  (index 35, gates at 11, 45, 81, 89), out of reach of every gun at the front. Both reference players guard a marked
  rear rift with a gun a wave ahead; on Psycho that is a Cannon in wave 6 at the price of the front Cannon's upgrade,
  and the swarm of wave 8 then takes the first gate. Round 80b booked that gun in the model's purse; round 83 took the
  booking out again (the cell's scalar is measured with the gun standing). The other way, not built: the map (a rift that joins within a gun's reach of the first gate, as on
  Landing Site; needs a relayout and a new plan search, so a bot batch of its own).

## The solver (round 83)

One pass per difficulty, wave by wave, in `WaveSolver.Solve` (called by `CampaignModel.BuildWaves`). No difficulty
copies another, no level mean is held, nothing is solved twice.

1. **The player.** A competent player's towers alone (`BalanceRules.HumanTowersOnly`: the fitted tower efficiencies at
   `HumanUtilisation`), with the difficulty's real money (`KillPurse`: a kill pays when the creature reaches the defence).
2. **The purchase is decided before the wave's toughness is known** (`Ledger.PlanOf`): the player buys for the creatures
   and numbers the preview shows, at book health, with what earlier waves left on the road. The pressure of a wave
   therefore never falls when its creatures get tougher, and one multiplier meets a target. Tests:
   `Round83Tests.AWavesPressure_NeverFalls_AsItsCreaturesGrowTougher` (ground, both lenses, every stored wave) and
   `AWavesAirPressure_NeverFalls_AsItsFlyersGrowTougher` (every raid).
3. **Every wave has its own target**: the level's target (`CampaignModel.ModelTarget`: one over `HumanSurplus` per
   difficulty, with the ramp per level of a continent) times the wave's share of the curve (`WaveSolver.Shares`: the
   opening ramp, the slow rise, the factor of its kind, the boss peak and the breather after it, the growth of plated
   troops and of raids; normalised so the plain waves average one). A bisection finds the multiplier.
4. **The band.** A ground wave stays within x0.7 to x1.3 of the toughest of the three ground waves before it, as its
   creatures walk in. A wave drops under the band only when its own health is what asks too much (a thin purse after
   the sky guns of a raid); where the pressure is carried in by earlier waves it keeps the lower edge instead of
   becoming paper. Once four ground waves are solved, no ground wave is above 2.2 times their median or, when it drops
   under its band, under 0.45 times it (the gates on the stored waves allow 2.5 and 0.4). All of it is read on the
   creatures as they walk in (a capped swarm carries its unbought budget as health). A raid is free; its flyers keep
   `RaidHealthFloor`.
   **A surprise wave stands in the band and the median as the wave it would be without its surprise (round 84).** Its
   creatures are fewer and tougher, and a late swarm lives on the upper edge of its band: Crysium Vein Psycho wave 16
   stood x1.59 above the same level solved without surprises, because wave 9 (a rift surprise at x4.5 where the calm
   wave is x2.3) lifted the band. A difficulty that carries surprises is therefore solved once without them first
   (`CampaignModel.SolveDifficulty`, `WaveSolver.Solution.WalkIn`), and that solve is only a reference: what a
   surprise wave counts as for the waves after it. Nothing is redistributed and no wave is lowered to it. Taking the
   surprise waves out of the median alone was tried first and moved that swarm from x1.61 to x1.55: the band was the
   channel, not the median. What is left is what the purse moves (`SurpriseLiftLab.cs`): a plain wave x1.09 at most, a
   raid x1.03, a swarm x1.23 (Glass Meadows Psycho wave 11), a boss wave x1.29 in health (Shard Ridge Psycho wave 10).
   Test `WaveGrammarTests.ASurprise_MakesNoOtherWaveTougher`, swarms included since this round.
   Round 85 opened a third channel (the boss rule read a surprise wave's creatures at their own multiplier) and widened
   the test's bounds for it; round 86 closed it (item 6) and the bounds are x1.15 / x1.25 / x1.3 again. Measured after
   it: a plain wave x1.05, a raid x1.09, a swarm x1.12 (Shard Ridge Easy wave 10), a boss wave x1.07.
5. **The hero cap.** No plain wave or raid asks more of that player with the hero (`HumanWithHero`) than `HeroCap`. The
   wave's own health gives way, by the same band rule. A boss wave is not sized by the paper at all (next item).
6. **A level's boss, one rule (round 86).** `WaveSolver.Solve`, `BossYardstick`:
   - **The boss costs the player's guns `BossOverRegular` (3 on every difficulty) times what the toughest ordinary
     creature met in the level before it cost them, under shells and under light fire both.** Under shells a creature
     costs its bar (body plus armour; armour point for point). Under light fire (Gun Turret, Laser) a point of armour
     costs five (`BalanceRules.LightFireOnArmour` 0.2, the armour column of the combat table), and whatever flies costs
     five times as much again (`LightFireInTheAir` 0.2, the air column). The boss walks in at the least multiple of its
     book health and armour that meets both rows; the two rows may name different creatures. A walking boss is held
     against the walkers (a raid has a multiplier of its own); a flying boss against everything met, and for it the
     shells decide, because light fire already pays five times for a boss in the air.
   - **Never under its book health** (`BossBookShare` 1, in the solver's own measure): an early boss that comes after
     swarm creatures only would be three Skitters otherwise (wave 5 of Landing Site on Psycho: 0.08 of its Codex entry).
   - **A boss with nothing before it** is held against the toughest creature of its own wave.
   - **A surprise wave counts as the calm wave it would have been** for the yardstick too (as for the band and the
     median, item 4): its creatures are met as kinds, at the calm multiplier. So a surprise moves no boss.
   - **Its crowd comes on the lower edge of its band**: the creatures of the waves before it, a little softer (x0.7 of
     the toughest of the three ground waves before). No paper target, no paper cap, so no cliff: until round 86 the
     crowd was filled up to a target around the boss and gave way under a cap, and it jumped between a floor and the
     edge of the band when the cap was met or missed by a hair (Glass Meadows Psycho wave 5: x0.41, x1.75 and x0.55 on
     three neighbouring scalars).
   - **Every boss walks in armour** (round 85): at least `BossArmourShare` (0.25) of its body, on the creature data
     (the generator, `ApplyRound85.cs`, the finale stage).
   - Nothing of it is read on the health as stored, so a cell's scalar moves a boss exactly as it moves every other
     creature (test `Round86Tests.ACellsScalar_MovesNoWaveOfTheSolve_OnMediumHardAndPsycho`).

   Gone in round 86: `LevelBossAlone` (the share of the defence a boss asks alone) and `LevelBossGrowthCap` (a boss
   wave's own cap), which decided nothing once every last boss stood on the multiple, and `LevelBossHealthFloor` (a
   floor on the stored health), which bound only the early Psycho bosses and did not move with the scalar. Remco's
   reason for the rule (round 85): on Easy the boss of Landing Site had the bar of a Cragback and no armour, so under
   light fire it was a third as tough as the "lava rocks" of the waves before it. Round 85 held the boss on the bar
   alone; the Cragback is three quarters armour and a boss a fifth, so under a Gun Turret or a Laser a boss of three
   bars was still only 1.4 Cragbacks. Tests: `Round86Tests.EveryLevelBoss_StandsOnItsMultiple` (the rule on every
   stored boss, report `Tools/PlayTest/boss_rule.txt`), `ABoss_CostsItsMultiple_UnderShellsAndUnderLightFire`.

   **How the paper reads a boss** (`BalanceRules.HumanBossUtilisation` 1, round 86): the ledger charged a gun's fire
   on a boss at the utilisation of a stream (0.34), which is what a gun loses to the gaps in a stream; a boss is one
   slow body and the ledger already counts only the seconds it is in a gun's reach. With the seven guns a reference
   player really stood on Landing Site Easy at x1.76 of the data, in a run that lost no base health, the boss alone
   read 1.45 and the wave 2.56; at one it reads 0.42 (`BossPaperLab.cs`). The lenses of the solver and the tests use
   it; the bots' own lens and the calibration fit do not (they were fitted the old way). A boss wave may still read
   over the hero cap on paper; `BalanceLedgerTests` reports those and holds them under a ceiling.
7. **The flyer rules** stay as they were and solve a difficulty again when they move a raid: the Easy purse rule and
   `FlyersAfterBounties`.
8. **Play's correction, as two plain factors on creature health** (`CampaignModel.SolveDifficulty`): the difficulty's
   factor (`BalanceRules.DifficultyScale`, one number per difficulty for every level) and the cell's scalar
   (`Tools/PlayTest/wave_tuning.txt`, `KEY D scale`). They act on every creature alike, a boss included, exactly as the
   probe's health multiple does.

The solve stage takes two to three seconds for the six levels (it took 27).

Why a wave's multiplier is what it is: `WaveLedgerLab.cs` (Docs/COMMANDS.md, "Round 83") prints per wave its own target
(`aim`), the pressure with and without the hero, the purse, the purchase and the boss's health multiplier.

## What a wave is made of: rules per difficulty, no lever per cell (round 84)

Until round 83b `Tools/PlayTest/wave_tuning.txt` held 36 composition levers written by the old builder for single
cells. They are rules in `BalanceRules` now, or gone; the file holds a cell's scalar and nothing else (test
`Round83Tests.TheTuningFile_HoldsACellsScalar_AndNothingElse`). A new level gets all of it without a line written.

| Lever (cells that had it) | Now | Paper evidence (`r84_lab.sh`, `ledger_compare.py`; ground health of the level against round 83b) |
| --- | --- | --- |
| `plated` (11: nine Hard or Psycho at 1.5, Glass Meadows and Crysium Vein Medium, Fault Line Hard 1.25) | `BalanceRules.PlatedBias` 1 / 1 / 1.5 / 1.5: on Hard and Psycho armour weighs half again as much in every fill | Alone (against no weight at all) it takes 4 to 13 % of the bodies on Hard and Psycho and moves the level's health by -8 to +9 %; the three cells that gain it (Fault Line Psycho, Quartz Flats Hard and Psycho) move by 5 % or less with the swarm weight beside it |
| `swarm` (8: seven Hard or Psycho, Glass Meadows Medium at 2) | `BalanceRules.SwarmBias` 1 / 1 / 1.5 / 1.5: on Hard and Psycho the light unarmoured troops weigh half again as much | Alone it adds 3 to 13 % bodies on Hard and Psycho and moves the health by -6 to +6 %; the cells that gain it (Fault Line Hard, Quartz Flats and Crysium Vein Hard and Psycho) get 2 to 13 % more bodies and -1 to +16 % health with both weights |
| `armourgrowth` (8: Landing Site and Glass Meadows, 1.4 / 1.4 / 1.5 / 1.5, Glass Meadows Medium 1.2) | `BalanceRules.ArmourGrowth` 1.4 / 1.4 / 1.5 / 1.5 **where a difficulty of a level sends no flyers** (`WaveComposer.GrowthOn`) | On every level alike it does not hold: ground health x1.3 to x2.8 on the four levels with flyers, and six and seven waves of Shard Ridge Hard and Psycho two to four times over the hero cap (`ledger_held.py`), four of Crysium Vein Medium, three of Crysium Vein Psycho. Round 68 measured the same (no plan won Shard Ridge Hard or Quartz Flats Easy with it). Without it Landing Site and Glass Meadows lose 15 to 20 % of their Easy and Medium health and the armour window. Where flyers come, the raids end a row without a counter (the grace report) |
| `tempo` (3: Landing Site Hard and Psycho, Glass Meadows Psycho, 0.85) | gone | The difficulty's own spawn tempo and the cell's scalar do the work: the same bodies, the spawning 12 to 17 % longer, health +4 to 5 % |
| `raid` (4: a few spread runners opening wave 3 or 5 of Landing Site Psycho and Glass Meadows Medium, Hard, Psycho) | gone | Five bodies; Glass Meadows Hard is the same level to a percent without it. The grammar's surprises do this job by rule |
| `bosscrowd` (2: Shard Ridge Hard and Psycho, 0.35 where the rule is 0.6) | gone: `BalanceRules.BossWaveShare` 0.6 everywhere | The solver gives the crowd's health way to the boss since round 83. The two cells get the fuller crowd of every other level (83 bodies for 48 in wave 10) and their last boss is 17 and 33 % lighter on paper (x1.38 -> x1.15, x2.04 -> x1.37); a thinner crowd on every Hard and Psycho boss wave would make every boss tougher (0.35 everywhere: Medium bosses +20 to 50 %) and is Remco's call |

The cells whose make-up moved: Glass Meadows Medium (growth 1.2 -> 1.4, no plated, swarm or raid lever), Crysium Vein
Medium (no plated lever), Fault Line Hard and Psycho, Quartz Flats Hard and Psycho, Crysium Vein Hard and Psycho
(the Hard and Psycho weights), Shard Ridge Hard and Psycho (the crowd, Psycho's swarm weight 1.25 -> 1.5), Landing
Site Hard and Psycho and Glass Meadows Hard and Psycho (tempo, raid). Eleven cells have the same bodies in every wave (every Easy cell, four Medium cells, Landing Site Hard); over all 24 the bodies move by -7 to +22 % (Shard Ridge Psycho) and the paper health by -5 to +16 % (Crysium Vein Hard).

## How to balance a new level (round 83)

The model shapes a level; it does not know how tough it is as played to better than a quarter. So the last step is
measured, by one procedure that is the same for every level.

1. Write the level's script (roster, rhythm, raids, bosses, teasers; its own surprises and road plan where wanted) and
   make its map. No scale, no lever: the cell starts at scalar one, on top of the difficulty's factor, and what its
   waves are made of per difficulty (plated and swarm weight, the growth of plated troops) is `BalanceRules`
   ("What a wave is made of", above). `wave_tuning.txt` takes the cell's scalar from step 5 and nothing else.
2. Solve: `unity command run_script --file Tools/PlayTest/Scripts/ApplyRound68.cs --entry ApplyRound68.Run --timeout_ms 900000 --timeout 960`.
3. Search the skilled rung's strategies for the level (`PlanSearchTests` with `levels=<node>`, two minutes a level),
   play every candidate on the standard ladder and pick per cell the one that breaks even highest
   (`skilled_picks.py`, round 83b: the loop reads the rung's break-even, so the pick is ranked on it and not on the
   base kept at the data). A fresh search is not always better than the strategy a cell had: keep the old pick as one
   more candidate. This is a bot batch: never together with a data change.
4. Measure on the wide ladder:
   `PR_RECORDS=Tools/PlayTest/<name>_records.txt PR_MULTIPLES="0.4,0.5,0.64,0.8,1,1.25,1.6,2" bash Tools/PlayTest/pressure.sh "<node>" "0,1,2,3"`
   (192 runs a level, about a quarter of an hour).
5. `python Tools/PlayTest/closed_loop.py <records> --levels <node> --write`: per difficulty the break-even of the
   reference rung (the geometric middle of the last multiple two of three seeds hold and the first they lose) over its
   target gives the new scalar. Targets:

   | Difficulty | Target | Reads on the standard ladder (x0.8 / 0.9 / 1 / 1.12 / 1.25) |
   | --- | --- | --- |
   | Easy | the weaker of the two rungs breaks even at x1.55 at least (round 85; x1.4 before). The ladder cannot read x1.55: its readings are x1.32, x1.48, x1.66 and "over x1.86", so the gate accepts x1.48 or more, which is "holds x1.4" (round 86 says so; the loop still aims at x1.55) | measured on x1 / 1.25 / 1.4 / 1.57 / 1.76: both rungs win to x1.4 |
   | Medium | the sensible rung breaks even at x1.32 (round 85; x1.18 before); the skilled rung at x1.06 at least | measured on x0.9 / 1 / 1.12 / 1.25 / 1.4 / 1.57: sensible wins to x1.25 and loses x1.4 |
   | Hard | the sensible rung breaks even at x0.95; the skilled rung at x1.06 at least | sensible wins to x0.9 and loses x1; skilled wins x1 |
   | Psycho | the skilled rung breaks even at x1.06 | skilled wins x1 and loses x1.12 |

   So Medium is never flat, Hard is a third tougher than Medium for the same player on every level, and Easy is
   won by both rungs with room. Round 85 (Remco after playing level 1 on Easy, Medium and Hard: "Easy and Medium a
   tad too hard, Hard feels right; not much less, but a bit"): the Easy and Medium targets moved one ladder step up,
   Hard and Psycho stayed. The standard ladder does not reach them: `Tools/PlayTest/r85_measure.sh` plays Easy and
   Medium on their own ladders (Docs/COMMANDS.md, "Round 85").
6. Solve again and measure on the standard ladder (`pressure.sh "<node>" "0,1,2,3"`, 120 runs). A cell whose reference
   rung reads its target is done; else `closed_loop.py` once more (the scalar is a plain factor, so one pass lands
   within a ladder step unless the bot takes another build). A cell that two passes read on opposite sides of its
   target is bisected (round 84): the geometric middle of the two scales, not the loop's factor a third time. A
   scalar the loop asks outside 0.7 to 1.4 stays on the bound and the cell is reported (Quartz Flats Hard, 0.7).
7. Gates: `ClosedLoopAcceptanceTests` (round 83b: every cell's reference rung breaks even within one ladder step of
   its target in the measurement named in `Tools/PlayTest/closed_loop_records.txt`, Easy is won on every seed at the
   data and holds the ladder, Hard breaks the sensible rung earlier than Medium on every level; a measurement of
   other data than the project's is inconclusive, never a pass), `Round83Tests.ACellsScalar_LiesNearOne` (0.7 to 1.4:
   outside it the model misreads the level, look at the map or the script), the two median gates,
   `BalanceLedgerTests.EveryWave_StaysInsideItsDifficultyBand` (the paper band and the hero cap on the solver's own
   waves, the stored ones with both factors taken off), the grace report, the finale probe when the level is a
   continent's last.
8. When a continent is done: `closed_loop.py <records> --centre` gives the geometric mean of each difficulty's scalars;
   fold it into `BalanceRules.DifficultyScale` and divide the scalars by it, so the next level starts closer.

What the procedure does not do: it does not choose which wave is the hard one (the script and the solver do), and it
does not judge fun. A level that reads its targets and plays badly is a script or map question.
