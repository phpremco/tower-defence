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
  level and difficulty, measured, never typed (until round 86; since round 87 one scalar per level, see "The solver").

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
| Boss entrance / finale | The crowd in clusters with long breathers; the boss walks in behind the last cluster (round 88c: `BossEntranceGap` 8 s after its last creature, see "Round 88c") | Every boss wave; the last wave takes three clusters | `BossBeats` 2, `FinaleBeats` 3, `BossBreather` 9 / 8 / 7 / 6 s, `MinBossBeatSeconds` 3 |

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
solver's target curve, as before (until round 86; round 87 replaced the solve by a formula, see "The solver"). The old letter `M` (ambush) is gone: surprises are placed by rule, not written per level.

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
solver", below; test `Round83Tests.NoStoredGroundWave_IsPaper`). (The band: until round 86; round 87 replaced the
solve by a formula, see "The solver".)

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
  whatever is written, and `KillPurse` books each kill where it happens. (The sizing on the ledger: until round 86;
  round 87 replaced the solve by a formula, see "The solver".)
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
solver", below), and the rules went. (The "why it is not needed" column describes the solve of rounds 83 to 86: a
target per wave, the hero cap, a scalar per cell; round 87 replaced the solve by a formula, see "The solver".)

| Rule | What it repaired | Why it is not needed |
| --- | --- | --- |
| A raid is capped at the sky guns held (`RaidsCappedAtHeldGuns`, the 5 % walk to the lowest crossing) | A raid met its target two or three times, with a sky gun's next step bought in between | The purchase is decided before the multiplier: one crossing, a plain bisection. A raid is sized against the sky guns the player plans for it |
| A surprise lifts no other wave (`SurprisesLiftNoWave`, the second "calm" solve) | The common factor put what the hero cap took off a surprise wave onto every other wave | Every wave has its own target; a capped wave is just lower. Test `WaveGrammarTests.ASurprise_MakesNoOtherWaveTougher` |
| The relief bound (`SurpriseReliefMost` 0.05) | The rule above left levels under the 6 % curve gate | No level mean is held and the gate is gone |
| Raids out of the plain mean, as a special case of the calibration | Capped raids pushed the ground waves up through the mean | There is no mean to push. `BalanceChecks.PlainMean` (a reading aid now) still leaves raids out |
| The guard gun of a rear rift (`RearRiftGuardCells`, `BooksRearGuard`) | The model's purse was all at the front while both reference players stand a gun at a far rift | On paper it moved six waves of Glass Meadows and Quartz Flats by 2 to 14 % (`wave_ledger_r83_noguard.txt`); the cell's scalar is measured with that gun standing, so the level carries it |
| The scale refit (`ScaleRefit.cs`) and the 24 builder scales | Re-expressed every scale after a rule changed what the mean was read on | A cell's scalar is a plain factor on creature health and comes from the closed loop |

Kept, because they are rules about the game: the grammar, `MinSurpriseGroup`, spawn-point roles and schedules, road
plans, `FlyersAfterBounties`, the Easy purse rule, `RaidHealthFloor`, the band and its 2.5-times-the-median gate
(the band and the median caps: until round 86; round 87 replaced the solve by a formula, see "The solver").

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
there is one solve per difficulty and the band has a lower edge too (until round 86; round 87 replaced the solve by a
formula without a band, see "The solver"). Gates on the stored waves:
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
- **Solver**: every difficulty is solved on its own, wave by wave, each wave to its own target (until round 86; round 87
  replaced the solve by a formula that reads no purse and no defence, see "The solver").
- **What the model does not see**: that a tank in front costs single-target guns time on the small ones behind it, and that
  a breather lets the base's guns clear besiegers. Both are measured by the pressure probe, not predicted.
- **The brood of a level boss (round 88b).** `WaveLoad.Of` adds it to the boss wave as a stream of
  `floor(EscortWorth[d] x boss bar / creature bar)` creatures of the boss's Spawn creature (`EnemyStats.RuledEscort`,
  filled by `BalanceLedger.Stats` for a boss without boss data), joining its road at `BalanceRules.EscortStartShare`
  (0.6 with the stored weights; a continent boss's call keeps `SummonStartShare` 0.5 and its rate). The brood of a
  boss that FLIES (the Storm Matriarch's bats: 7 of the 42 stored boss walks) is not in the ledger: added to the air
  health it moved the ledger's purchase and broke `Round83Tests.AWavesPressure_NeverFalls_AsItsCreaturesGrowTougher`
  by a hair, and the air side has no stream that joins mid-road; the bots meet it, the paper does not. Also not
  modelled: the plated share (the whole worth is counted in the light creature's body), the cap, and that a boss
  killed early sends less (the ledger counts the whole walk: conservative). The brood pays no kill money in play and
  none in the ledger. The wave formula and `BossYardstick` do not read the brood: a boss's bar and its wave's
  multiplier are as the formula sets them. `FinaleModel` is untouched (continent bosses only). `boss_share.py` needs
  no change (it counts lost runs per wave). The wave preview lists the wave's groups, as before; the brood's creature
  is in the level's roster (`LevelRoster`), the plated one is a creature of the level's own waves. `InputHash` takes
  the escort constants, the worth per difficulty and the brood creature per level boss, so the calibration stamp is
  stale after a change to any of them until it is re-stamped (Docs/COMMANDS.md, "Round 88b").

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
  and each body is harder (until round 86; round 87 replaced the solve by a formula, see "The solver"). Over a level it is small (Glass Meadows Psycho: 731 -> 704 creatures). The test
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
- **Fault Line is a doorstep level (round 88b).** Its two roads (106 and 45 cells) share only the last nine cells
  before the base (trunk share 0.08; Shard Ridge and Crysium Vein 0.27), and the one gate both roads pass stands at
  cell 100 of 107; the gates at 11, 87 and 94 guard the long road alone. A defence that covers both roads stands at
  the base: the bots' creatures die at 0.72 to 0.91 of the road on every difficulty, and on Psycho the outer gates
  are gone by wave 5. With seven raids in sixteen waves (44 %; the other Psycho cells 0 to 35 %), three boss waves
  and the smallest kill money of the long levels (2554 coins; Shard Ridge 2716, Quartz Flats 3458; counted in round
  87, before the boss bounty rule moved), Psycho has six ground waves without a boss, and whatever leaks from the
  late swarm is on the base at once (a full base lost in wave 13 alone). The rule "a swarm between two raids carries
  no peak" ("The solver", point 1) takes the peak off that swarm; the map and the raid count are the level's own.
  Left for a script decision: one raid fewer on Psycho (`AirWavesPerDifficulty` 7 -> 6) gives the late game a second
  ground wave, which is more ground health, not less.

## The solver (round 87: a formula)

A wave's health is no longer solved against the paper ledger. `WaveSolver.Solve` applies `WaveFormula`
(called by `CampaignModel.BuildWaves`); no purse, no defence and no other wave is read.

1. **A wave.** The multiplier its creatures walk in with (after the composer's lift for a capped troop, as before):

       walkIn = DifficultyScale[d] x LevelScalar[L] x Ramp(phase) x KindFactor x Peak

   - `DifficultyScale` (1.5 / 1.7 / 1.36 / 2.08): the multiplier of a level's middle wave per difficulty.
   - `LevelScalar`: one number per level in `Tools/PlayTest/wave_tuning.txt` (`C1_01 0.875`), set by the closed loop,
     held to 0.7 .. 1.4 (`LevelScalarLeast`, `LevelScalarMost`).
   - `Ramp`: geometric in the wave's phase `(n-1)/(N-1)` from `RampStart` 0.6 to `RampEnd` 1.68 (round 88b; 0.75
     and 1.34 in round 87). Round 87's pair was fitted on the plain ground waves round 86 stored
     (`Tools/PlayTest/r87_fit.py` on `r87_dump_r86.txt`, 244 waves of 24 cells): geometric in phase leaves an rms
     residual of x1.373, geometric in wave number x1.382; a slope per difficulty x1.343 (not kept: three more
     numbers for 3 %). The residual is large because round 86's waves jumped from wave to wave (band, caps, purse);
     the fit only set where the first measurement started. **Why steeper (round 88b):** with 0.75 to 1.34 a
     creature's health rose x1.8 over a level against x3 to x4.7 for a gun upgraded to level 3, and on paper the
     pressure per coin stood still from the middle to the last third of a level on Hard
     (`Tools/PlayTest/r88b_economy.txt`). Remco: "the later in a level the easier it feels". 0.6 to 1.68 is x2.8
     over a level; the middle wave stays near `DifficultyScale`.
   - `KindFactor`: rush 1.1, heavy 1.15, raid 0.9 (`RushFactor`, `HeavyFactor`, `AirFactor`), times
     `AfterBossFactor` 0.8 on the wave after a boss, times `BossCrowdFactor` 0.55 on the crowd of a boss wave.
   - `Peak`: `PeakFactor` 1.35 on two peak waves per level and difficulty, three on a level of `LongLevelWaves` (16)
     waves or more. `PeakPhases` 0.4 and 0.7 (`LongLevelPeakPhase` 0.55). Every peak first lands on the wave
     nearest its phase that is a ground wave without a boss and not the breather after one, no two side by side;
     then a peak moves to a neighbour that carries a surprise or is a heavy wave when that keeps it clear of the
     other peaks (`WaveFormula.Peaks`, `WaveSolver.PeaksOf`). In two steps so that a surprise can draw a peak onto
     its own wave and never pushes one onto another wave. Remco: waves before the boss must be able to hurt.
   - **A swarm between two air raids carries no peak (round 88b,** `BalanceRules.PeakOnARushBetweenRaids` **false).**
     The peaks are placed as before (`WaveFormula.Peaks`); then a peak that fell on a rush wave whose two neighbours
     are both raids is dropped, and it is not placed anew, so every other peak of the level stays on its wave
     (`WaveSolver.PeaksOf`, `WaveSolver.IsRushBetweenRaids`). Between two raids the purse belongs to the sky guns, and
     the swarm is the wave that asks for the most ground guns. In the first continent it takes the peak off four waves:
     Fault Line Psycho wave 13, Quartz Flats Hard wave 7, Quartz Flats Psycho wave 7 and Crysium Vein Psycho wave 16,
     each x1 / 1.35 of what it was (read on round 87's ramp: Fault Line x3.32 -> x2.46, 12 500 -> 9 300 health;
     Quartz Flats x1.79 -> x1.33 and x2.74 -> x2.03; Crysium Vein x3.48 -> x2.58; the steeper ramp of this round
     moves those multipliers, not the ratio). In round 87's records those waves ended 32 of 57, 0 of 22, 0 of 46 and
     1 of 48 lost runs: only Fault Line's was a wall.
     Tried on paper and not kept (`Tools/PlayTest/r88b_raids.txt`): "no peak between two raids, placed anew" moves
     nine peaks in seven Hard and Psycho cells and puts peaks on waves 1 to 3 (Fault Line Psycho: peaks 2 and 4 only),
     and "no peak on a rush between raids, placed anew" puts Fault Line Psycho's third peak on wave 2. Raids are the
     script's (`AirWaves`, a difficulty takes the first few): "no raids in two waves running" would undo the double
     raids Shard Ridge writes on purpose (waves 11-12 and 14-15) and leave Fault Line Psycho with Medium's raids.
     Tests: `Round88bWaveTests`.
   - A raid keeps `RaidHealthFloor`; the flyer rules (the Easy purse rule, `FlyersAfterBounties`) are unchanged and
     still read the ledger's purse to decide WHEN flyers come. Nothing else reads the ledger for sizing.
   - A surprise wave gets the formula like any wave and makes no other wave tougher (it can draw a peak onto
     itself, which leaves the neighbour that would have carried it an ordinary wave;
     `WaveGrammarTests.ASurprise_MakesNoOtherWaveTougher`).
2. **A level's boss.** Its bar as spawned (body plus armour):

       bossBar = BossBase x BossDifficulty[d] x (1 + BossLevelGrowth x L) x (BossEarlyShare + (1 - BossEarlyShare) x phase) x (flies ? FlyingBossShare : 1)

   `BossBase` 1550 (round 88b; 2050 in round 87), `BossDifficulty` 0.7 / 1 / 1.15 / 1.8 (never falling), `BossLevelGrowth` 0.16 (0.12 in round 87) per level of the
   continent, `BossEarlyShare` 0.3, `FlyingBossShare` 0.6. It does not move with the level scalar or
   `DifficultyScale`. **Floor only:** never under `BossOverRegular` (3) times the bar (body plus armour) of the
   toughest ordinary walker met before it (`BossYardstick`; flyers are not met, a surprise wave creatures count as
   they walk in). On the round 87 data the floor binds on 4 of 42 bosses: three earlier Psycho bosses and the last
   boss of Fault Line Medium (by one per cent). Tougher walkers (a higher level scalar, `PeakFactor`, `DifficultyScale`)
   lift the floor; once it passes the formula on a last boss, that boss moves with the scalar and the two monotone
   tests can fail, so the floor is what limits how small the boss can be made against the waves around it. `BossArmourShare` is 0.5 since round 88b (1 in round 87): the pool is half the body, a third of the bar (`ApplyRound85.cs` raises a pool, `ApplyRound88bArmour.cs` brought it down). Round 88b: a flying boss takes `FlyingBossShare` of the floor too.
   Gone: the light-fire row of the yardstick (`LightFireOnArmour`, `LightFireInTheAir`), the book floor
   (`BossBookShare`), the calm reading of a surprise wave, the separate rule for a flying boss, the boss held
   against its own wave.

   **A level boss's kill money (round 88b):** `BalanceRules.BossRewardFactor` is 1.5 times the kill money of its
   threat (was 3). Since armour doubled a boss's threat in round 87 a mid-level boss's bounty was a windfall (257 to
   415 coins on Hard and Psycho; its wave added 34 to 82 % to the purse on Hard, with the x0.8 breather after it),
   and the last boss's bounty cannot be spent at all. `ApplyRound88bEconomy.cs` puts every level boss's kill money
   back on the rule; continent bosses keep their staged numbers.
2b. **A level boss's brood (round 88b: the escort rule).** Remco's playtest of round 87: "the boss wave feels pathetic
   ... once the boss passed the first defence it felt like 5 minutes until it reached the end; it sends little ants
   that die before the last gate". Every level boss (a boss without boss data; `EscortRule.Binds`) sends a brood out
   of its body along its walk, by one rule:

       brood health over an unhindered walk, spawn to base = EscortWorth[d] x the boss's bar as spawned

   - `EscortWorth` is a difficulty factor (`DifficultyProfile._escortWorth`, from `BalanceSheet.EasyEscortWorth`,
     `MediumEscortWorth`, `HardEscortWorth`, `PsychoEscortWorth`): 0.25 / 0.5 / 0.65 / 0.75. These are the curator's
     values of round 88b and may still move after measurement (the package first had 0.4 / 1 / 1.1 / 1.2). No number
     per level or per boss: a longer road, a bigger boss or another creature all get the same share.
   - **It grows along the road.** Every call (the boss's one Spawn power: its interval and first delay) grants the
     budget of the seconds since the last call, weighted by how far down its road the boss is: from
     `EscortStartWeight` 0.4 times the mean rate at the spawn to `EscortEndWeight` 1.6 at the base, a straight line
     (`EscortBrood.Weight`). With these, 35 % of the brood comes in the first half of the walk and 65 % in the second;
     a boss killed at a first gate at 13 % of its road has sent 6 %. The budget buys whole creatures; the rest
     carries to the next call. A call that buys nothing makes no sound.
   - **As tough as its own wave.** A brood creature walks in with the multiplier of the ordinary creatures of the
     boss's own wave (difficulty health x level scale x the wave's walk-in multiplier), remembered at the boss's
     spawn: a later wave starting while the boss lives does not change it. (Before: the multiplier of whichever
     wave was under way.)
   - **A plated share.** `EscortHeavyShare` 0.25 of the brood walks in as the toughest plated ordinary walker the
     level has sent before the boss (`EscortRule.Tougher`; on Crystal Plains the Cragback), so the brood is not only
     food for a blast. Only when that share of the whole walk pays for at least one, never for a boss that flies,
     and never a creature the level has not shown.
   - **A cap.** A boss held at a gate or slowed keeps calling at the rate of where it stands, up to
     `EscortMostOfWorth` 1.5 times the whole walk's worth.
   - **No bounty.** `EscortBountyShare` 0: a brood creature pays nothing (before: full bounty, 37 to 110 coins per
     brood mother walk). It counts for the wave's end like any creature and bites gates and the base as its kind does.
   - The creature per boss is data (its Spawn power's minion): Brood Mother and Hive Queen Skitters, Storm Matriarch
     Mossbats, Slime Monarch Oozers (they had these), and new in round 88b Pack Alpha Prowlers (`PackCall`), Core Wisp
     Wisps (`WispWake`), Acid Empress Stingers (`StingerGuard`), Crystal Warlord Shard Runners (`ShardGuard`), Sky
     Sovereign Skyrays (`RayWing`), Roller King Rollers (`RollerPack`) (`ProjectGenerator.Escorts.cs`,
     `ApplyRound88bEscort.cs`). The power's own count is not read under the rule.
   - **Continent bosses are outside the rule**: their calls keep count, interval, creature, bounty and
     `DifficultyProfile._bossEscortShare` (1 / 1 / 1 / 0.5) to the number (`EscortRuleTests.AContinentBoss_KeepsItsOwnCall`).
     Side effect removed: until round 88a Psycho's 0.5 also halved the brood of a LEVEL boss (the field was meant
     for the finale); level bosses no longer read it.

   Before the rule (lab `Tools/PlayTest/r88b_escort.txt`): the brood mother's plain call (5 Skitters of 11 book
   health every 10 s) came to 43 to 56 % of her bar on Easy to Hard and 24 to 33 % on Psycho, spread evenly over
   the walk; the Storm Matriarch's bats 28 to 43 %; Core Wisp and Pack Alpha sent nothing on a timer. The first gate
   stands at 10 to 14 % of the boss's road on all six levels, so 85 to 90 % of the old brood already came behind
   it: the complaint is not how much health came but that it was paper, evenly thin, and the same at the end as at
   the start. After, as a walk realises it (the last call falls before the base, and a plated creature not yet
   paid for is not sent): 85 to 100 % of the difficulty's worth, 59 to 76 % of it in the second half of the walk
   (means 67 to 73 % per difficulty). That was read at the package's first worths (Easy 35 to 40 % of the bar at
   0.4, Medium 85 to 99 % at 1, Hard 95 to 110 % at 1.1, Psycho 102 to 121 % at 1.2); the lab has not been run
   again at 0.25 / 0.5 / 0.65 / 0.75.

   What to turn (curator, after the bots):
   - `BalanceSheet.*EscortWorth` (then `ApplyRound88bEscort.Run`): how much brood a difficulty gets. The one knob
     for "the boss wave is a wall" (down) or "still dull" (up).
   - `BalanceRules.EscortStartWeight` / `EscortEndWeight`: how late it comes. 1 / 1 is even; 0.4 / 1.6 is the
     stored slope; 0.2 / 1.8 puts 70 % in the second half. They change no total.
   - `BalanceRules.EscortHeavyShare`: how much of it survives a blast (plated). 0 is ants only.
3. **Play's correction** is the level's scalar and nothing else. `closed_loop.py` sets it from the geometric mean
   of the level's four cells; what the difficulties still disagree on goes to `DifficultyScale` (`--centre`).

Gone with the solve: the target curve (`TargetPressure`, `OpeningShare`, `OpeningWaves`, `EarlyTargetShare`,
`LateTargetShare`, `BossFactor`), the band and the median caps (`MaxRise`, `MaxDrop`, `MostOverMedian`,
`LeastOfMedian`, `MinHealth`, `MaxHealth`), the hero cap on a wave, the bisection, the calm reference solve
(`CampaignModel.CountsAs`, `CalmOf`), `ArmourAim` and the raid's air growth in the target, the paper band checks
(`BalanceChecks.Check`, `CheckHumanTarget`, `MaxPressure`, `DifficultyPressure`, `BandMargin`, `BossAllowance`,
`MaxCliff`, `MinLateEasyPressure`, `HumanTargetTolerance`), the builder stages that moved a scale per cell
(`BalanceStages.Build`, `BuildFromVerdicts`). The ledger stays as the paper model of the plan search, the finale
model and the reports (`balance_ledger.txt`).

Reports: `stage.sh run Round87Lab.cs Round87Lab.Run` writes `Tools/PlayTest/r87_waves.txt` (every wave's walk-in
multiplier with kind, peak, boss, raid, surprise) and `r87_bosses.txt` (every level boss: bar, body, armour, the
formula bar, times the toughest walker before it under both rows, whether the floor binds).

Tests: `WaveFormulaTests` (the formula, the ramp, the peaks, the boss formula and its floor, every stored wave on
the formula, two or three peaks per cell, every stored boss on formula or floor, bosses growing with difficulty and
with the level, the scalar moving no boss on its formula), `Round83Tests.NoStoredGroundWave_IsPaper` (against the
median of the whole level again), `ALevelsScalar_LiesNearOne`, `TheTuningFile_HoldsALevelsScalar_AndNothingElse`.

## What a wave is made of: rules per difficulty, no lever per cell (round 84)

Until round 83b `Tools/PlayTest/wave_tuning.txt` held 36 composition levers written by the old builder for single
cells. They are rules in `BalanceRules` now, or gone; the file holds a cell's scalar and nothing else (test
`Round83Tests.TheTuningFile_HoldsACellsScalar_AndNothingElse`; until round 86: since round 87 it holds one scalar per
level, test `TheTuningFile_HoldsALevelsScalar_AndNothingElse`, see "The solver"). A new level gets all of it without a
line written.

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

## How to balance a new level (round 87)

The model shapes a level; it does not know how tough it is as played to better than a quarter. So the last step is
measured, by one procedure that is the same for every level.

1. Write the level's script (roster, rhythm, raids, bosses, teasers; its own surprises and road plan where wanted) and
   make its map. No scale, no lever: the level starts at scalar one, on top of the difficulty's factor, and what its
   waves are made of per difficulty (plated and swarm weight, the growth of plated troops) is `BalanceRules`
   ("What a wave is made of", above). `wave_tuning.txt` takes the level's scalar from step 5 and nothing else.
2. Solve: `unity command run_script --file Tools/PlayTest/Scripts/ApplyRound68.cs --entry ApplyRound68.Run --timeout_ms 900000 --timeout 960`.
3. Search the skilled rung's strategies for the level (`PlanSearchTests` with `levels=<node>`, two minutes a level),
   play every candidate on the standard ladder and pick per cell the one that breaks even highest
   (`skilled_picks.py`, round 83b: the loop reads the rung's break-even, so the pick is ranked on it and not on the
   base kept at the data). A fresh search is not always better than the strategy a cell had: keep the old pick as one
   more candidate. This is a bot batch: never together with a data change.
4. Measure: `bash Tools/PlayTest/r87_measure.sh <name> "<node>"` (the rungs skilled, sensible, laser and cannon; Easy
   on x1.25 / 1.4 / 1.57 / 1.76 / 1.97 / 2.21, Medium on x0.9 .. x1.57, Hard and Psycho on the standard ladder with
   one more rung below since round 88b: `PR_MULTIPLES="0.71,0.8,0.9,1,1.12,1.25"`, Docs/COMMANDS.md, "Round 88b").
5. `python -I Tools/PlayTest/closed_loop.py <the three records files> --levels <node> [--centre] [--write]`: per cell
   the break-even of the reference rung (the geometric middle of the last multiple two of three seeds hold and the
   first they lose) is read against its target, and the new level scalar is the old one times the geometric mean, over
   the level's four cells, of measured over target. Targets and gates are frozen in
   `Tools/PlayTest/closed_loop_thresholds.txt` (2026-11-20; Hard and Psycho moved on a dated line of the same day,
   round 88b):

   | Difficulty | Target | Measured on |
   | --- | --- | --- |
   | Easy | the weaker rung breaks even at x1.66 (x1.55 in round 85); read on both sides like the others, no "over the ladder" | x1.25 / 1.4 / 1.57 / 1.76 / 1.97 / 2.21 |
   | Medium | the sensible rung breaks even at x1.32; the best of skilled / laser / cannon itself reads x1.06 | x0.9 .. x1.57 |
   | Hard | the sensible rung breaks even at x0.85 (x0.95 in round 87); the best of skilled / laser / cannon itself reads x1.06 | x0.71 / 0.8 / 0.9 / 1 / 1.12 / 1.25 (the standard ladder, x0.8 .. x1.25, with one more rung below) |
   | Psycho | the skilled rung breaks even at x0.95 (x1.06 in round 87) | the same six rungs |

   A cell is accepted within one ladder step of its target on both sides. The other gates: laser and cannon within
   one step of each other, and at most 40 % of a level's lost runs in a boss wave (aim 35 %).

   So Medium is never flat, Hard is half again as tough as Medium for the same player on every level (x1.32 against
   x0.85), and Easy is won by both rungs with room. Round 85 (Remco after playing level 1 on Easy, Medium and Hard:
   "Easy and Medium a tad too hard, Hard feels right; not much less, but a bit"): the Easy and Medium targets moved
   one ladder step up, Hard and Psycho stayed. The standard ladder does not reach them, which is why
   `r87_measure.sh` plays Easy and Medium on their own ladders. Round 88b (Remco after playing round 87: "too easy
   on all difficulties", almost 1000 coins left on Hard, a giant row of Cannons on Psycho; Easy "fine", Medium "OK
   for now"): the reference rung of Hard and Psycho breaks even one ladder step earlier, written as a dated line in
   the thresholds file before any measurement of the round; Easy and Medium stay. The standard ladder's lowest rung
   is then within a step of Hard's target, so both are measured with x0.71 below it.
6. Solve again and measure again (step 4). A level whose cells read their targets is done; else `closed_loop.py` once
   more (the level scalar is a plain factor, so one pass lands within a ladder step unless the bot takes another
   build). A level that two passes read on opposite sides of its targets is bisected (round 84): the geometric middle
   of the two level scalars, not the loop's factor a third time. A level scalar the loop asks outside 0.7 to 1.4
   (`LevelScalarLeast`, `LevelScalarMost`) stays on the bound and the level is reported.
7. Gates: `ClosedLoopAcceptanceTests` (reads the same thresholds file: every cell's reference rung breaks even within
   one ladder step of its target on both sides in the measurement named in `Tools/PlayTest/closed_loop_records.txt`,
   the best of skilled / laser / cannon on Medium and Hard, laser against cannon, the share of lost runs in a boss
   wave; a measurement of other data than the project's is inconclusive, never a pass),
   `python -I Tools/PlayTest/boss_share.py <records>` (shows where the lost runs fall: boss waves, last wave, peak
   waves), `ALevelsScalar_LiesNearOne` (0.7 to 1.4: outside it the model misreads the level, look at the map or the
   script), `WaveFormulaTests`, `Round83Tests.NoStoredGroundWave_IsPaper`, the grace report, the finale probe when the
   level is a continent's last.
8. When a continent is done: `closed_loop.py <records> --centre` prints `DifficultyScale` times the difficulty's mean
   residual; fold it into `BalanceRules.DifficultyScale`. The level scalars need no division.

What the procedure does not do: it does not choose which wave is the hard one (the script and the solver do), and it
does not judge fun. A level that reads its targets and plays badly is a script or map question.

## Round 88c: the wave ceiling, the boss behind its crowd, the first raid (2026-11-21)

Three rules from Remco's playtest of round 88b. None names a level.

**1. The ceiling on a wave (`BalanceRules.WaveCeiling` 1.8, `WaveFormula.Shape`).** The formula is now
`walk-in = DifficultyScale x level scalar x min(ramp x kind x peak, WaveCeiling) x boss relief` (boss crowd and
after-boss factors are applied after the ceiling). Round 88b's ramp (x1.68 at the end) times a late peak times a heavy
wave reached x2.6 of the level's middle: Shard Ridge wave 13 on Hard and Psycho, the wave between its raids, where 56 of
60 lost Psycho runs ended and `WaveGrammarTests.NoStoredGroundWave_IsFarTougherThanItsLevel` went red. A peak in the
first three fifths of a level keeps its whole factor (the latest that stays under the ceiling: a heavy peak at 0.64 of
the level); a later one is cut to the ceiling. The ramp alone never meets it (1.68 < 1.8). It binds the third peak of
the levels of sixteen waves and more; Landing Site and Glass Meadows (twelve waves, peaks at 4 and 8) do not move.
The bound of the test was not touched.

**2. A boss walks in behind its last cluster (`WaveGrammarRules.BossEntranceGap` 8 s, `BossTailStretch` 1.2,
`WaveComposer`).** Until now the boss came out at the moment its last cluster started. On a heavy boss wave that cluster
opens with the plated walkers, which walk at the boss's own pace (Cragback 0.6, Brood Mother 0.55): they covered the
boss from the entrance to the base ("those Cragbacks were on top of the boss at the same speed"). Now the boss comes
out `BossEntranceGap` seconds after the last creature of that cluster (the cluster's length is stretched by 1.2, the
most a difficulty's head count times its tempo adds in play). The crowd is cells down the road, the boss closes the
wave in plain view. Its own brood no longer comes out under its feet either: every creature of a brood breaks out of
the boss's front and runs (`EscortDashFactor` 2.6 times its pace, six seconds at most) to a place of its own ahead of
it, the first `EscortLeadCells` 1.2 clear of the two bodies, each next one `EscortSpacingCells` 0.7 further, four
places (`EscortSlots`) before the first is used again (`EnemySpawner.SpawnEscort`, `EscortDash`). Tests:
`Round88cTests.NoStoredLevelBoss_ComesOutWhileItsOwnRoadsCrowdStillDoes`, `EveryLevelBossesBrood_ReachesItsPlaceAheadOfTheBoss_WithinTheDash`.

**3. The first air raid is announced and paid for (`FirstRaidRule`, `BalanceRules.FirstRaidWarningWaves` 2,
`FirstRaidPurseGuns` 0 / 0 / 1 / 1, `DifficultyProfile.FirstRaidPurseGuns`).** Remco on Shard Ridge Hard: "suddenly
tons of air units, none all game, and it comes together with a boss, when all money is already spent on guns". The
rules said until now: Easy and Medium hold the flyers back until the bounties have paid for a sky gun; Hard and Psycho
"are expected to know the level". A first play cannot. Now, on every level and difficulty, two waves before the
level's first raid wave the HUD warns ("ALERT! Air raid in 2 waves."), and on Hard and Psycho the warning comes with a
purse: the price of the level's sky gun (the cheapest gun the player is offered that aims up only; without one, the
cheapest that can aim up) times the difficulty's factor. Once per level, paid into the wallet as it is (no income
multiplier). The late surprise stays; what changes is that it can be answered. The options that were weighed:

| Option | What it does for the complaint | Cost | Verdict |
| --- | --- | --- | --- |
| More starting money | Nothing unless the player knows to keep it: it is spent on ground guns in wave 1, ten waves before the raid. Softens every opening (start money is 10 % of a Hard level's coins) | Moves the openings Remco called right | Not taken |
| A purse with the warning, two waves ahead | The coins arrive when the player is told what they are for and cannot have been spent before. One sky gun (120) is about 5 % of a Hard level's coins (Shard Ridge Hard: 2318) and 3.5 % of a Psycho level's | Flyer levels on Hard and Psycho get softer by that much; the closed loop re-centres | **Taken** (Hard and Psycho) |
| The first raid never shares or follows a boss wave | Takes the squeeze out of Shard Ridge (boss in 10, raids in 11 and 12) but moves the raid scripts, and with them cells that sit on their target, and weakens the late surprise Remco likes | A regeneration of the raid scripts and a new measurement of the cells it moves | Not taken; the ceiling (rule 1) takes the peak between the raids down 15 % instead |
| The preview warns ahead | The next-wave preview already shows the flyers one wave ahead; one wave is too late when the purse is spent | None | **Taken as the two-wave warning**, on all four difficulties |

Not in the paper model: the ledger and the plan evaluator do not know the purse (they read start money, bounties and
ore). The bots do get it, so the closed loop measures it; the paper pressure of the waves after a first raid on Hard
and Psycho reads a little too high.

