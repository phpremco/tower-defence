# Continent bosses: design sheet

Status: curator draft, 2026-11-04. Remco gave creative freedom ("wees creatief") and asked for bosses that really play differently: their own entrance, rage at gates, hunting one turret type across the map, walking through gates.

Every boss gets these five parts:
- **one signature mechanic** that changes how you build and play;
- **a telegraph**, so the player sees it coming: sound, light and a direction cue, never blunt arrows;
- **a counterplay** that rewards a smart choice instead of only more firepower;
- **its own entrance** in the map;
- **phases** that show on the HUD boss bar.

All bosses are big, glowing, slow, with a readable head and weapon, and killable. Camera kicks stay short.

## Rules from Remco (2026-11-04)

- **Every boss is described in the Codex enemy index.** The entry gives its signature, how to spot it coming and how to beat it, in plain words, as soon as the player has seen the boss.
- **The field has to stay consistent.**
  - A boss never walks through towers, decor or gates unless that is its mechanic. When that is its mechanic, you see the effect: the gate freezes, breaks or becomes a wreck.
  - Visuals and logic agree. If a crack runs under a tower, the tower is affected (pushed up, damaged, tilted). Nothing stands untouched on a hole or crack.
  - A boss that leaves the road avoids decor and towers the way a walker would, or makes them react.
- **No hard counters that wipe everything out.**
  - Effects that switch your defence off are partial and short.
  - The player always keeps something that works, and there is always an answer that costs money or attention, never an answer that does not exist.
  - Concretely:
    - immunity becomes resistance (halved damage);
    - "everything dark" becomes one element at a time;
    - an effect never hits more than about a quarter of your towers at once.
- **Use the terrain (Remco: "het terrein echt goed gebruiken").**
  - Bosses climb. They clamber over rocks, cliffs and boulders instead of walking round them, and they climb onto gates and the base to attack from above.
  - This needs a climb layer: a height and surface map that includes rocks, structures and the base roof. Bosses follow it with body tilt and leg reach (IK or baked climb clips from Blender), so their feet stay on the surface.
  - While a boss stands on a gate, the gate visibly sags and takes damage from its weight. Walkers can follow it over a gate it has climbed.
  - What each boss climbs:
    - **Gravemaw** surfaces on rocks and climbs over the edge.
    - **The Cinder Matriarch** climbs over rock ridges between the pads, on her way to the cold towers.
    - **The Frost Warden** climbs onto a frozen gate before it smashes it.
    - **The Shattered**'s shards scale the ruins.
    - **The Sealed One** pulls itself up onto the base.
  - The climb layer stays logical. Bosses only climb what is solid. Decor that cannot hold them (crystals, plants) breaks under them.
- **Use the pools (water, lava, bog, quicksand, ice).** Bosses can dive into a pool and hide in it.
  - **While submerged:**
    - only a wake, bubbles or a glow under the surface shows where the boss is;
    - towers cannot target it, except splash damage that lands in the pool, which does reduced damage;
    - it regenerates slowly.
  - **It surfaces elsewhere:** in another pool, or at the bank nearest the road, with a burst of liquid, a splash on nearby towers and a short kick.
  - **The limits:**
    - at most 6 s under per dive;
    - each pool can be used once per phase;
    - the boss never vanishes for long, and its health bar stays visible on the HUD.
  - **The counterplay:**
    - towers near the banks;
    - slowing fields at the exit;
    - mines on the bank.
  - **Per boss:**
    - The **Cinder Matriarch** dives into lava and comes up in another lava pool, closer to her cold target.
    - **Rotbloom** sinks into the bog and lets its roots grow under water to the next bank.
    - The **Frost Warden** breaks through an ice sheet and swims under it.
    - **Gravemaw** uses quicksand as a shortcut for its burrow.
- **The balance ledger counts the effects.** Each boss's mechanic is modelled as a loss of firepower for the duration of the effect, inside its HeroCap.

## Gravemaw (Crystal Plains, finale)

**Theme:** the planet's digging maw. It answers the colony's drill with a drill of its own.

- **Entrance:** it does not come from a spawn origin. Halfway through the boss wave the ground cracks open along a line straight towards the **colony drill rig** at the edge of the map. Gravemaw surfaces under the rig, smashes it (story beat) and turns towards the base.
- **Signature, Burrow:** at 75%, 50% and 25% health it dives under. A crack line runs through the ground and shows where it is heading. The line goes round towers and gates, never visibly under them, except at its end point. It surfaces next to the road, **beside the tower that has done the most damage to it**, and throws that tower up. The tower flies up, lands tilted and loses 40% of its health. It is not destroyed, and it rights itself after 4 s. Every tower the line ends under reacts; nothing stays untouched on a crack.
- **Telegraph:**
  - the crack line glows warm orange;
  - the rumble swells;
  - the target tower gets a pulsing red ring 3 s before Gravemaw surfaces.
- **Counterplay:**
  - Spread your damage over several towers, because a single super tower gets thrown.
  - Sell or move the tower in time, or place a mine on the crack line: a mine that goes off while it is burrowing stuns it on the surface.
- **Phases:** surface, burrow, and enraged below 25%, when it moves faster and its crack lines spawn Skitters.

## Cinder Matriarch (Ashen continent: lava)

**Theme:** the queen of the lava, and she hates cold.

- **Entrance:** she rises out of the largest lava pool, an origin that only she uses.
- **Signature, Hatred of the Cold:** she ignores the road. She **hunts the Cryo towers** (and every tower that slows) across the whole map, because cold kills her brood.
  - She walks between the build pads and goes round decor. She does not walk through towers: rocks and plants burn or are pushed aside, so you see that she passes.
  - At the cold tower she smashes it into a wreck. It can be rebuilt.
  - She has very high HP but is slow.
  - Without cold towers, she walks the road to the base.
- **Telegraph:** when she arrives, a thin heat line runs from her to her first target. That tower starts to glow warm ("she has smelled you").
- **Counterplay:**
  - Use a cold tower as bait: build a cheap Cryo far from the road, so she walks a long way past your firepower.
  - Or build no cold at all, and she takes the long road.
- **Lava trail:** her path leaves glowing lava for 6 s, which damages towers it touches and hurts walkers less.
- **Phases:** walking; then below 50% she lays eggs in the lava pools, which hatch as fire bugs after 10 s unless you shoot them.

## Frost Warden (North continent: ice)

**Theme:** a guardian of ice that freezes your defence in place.

- **Entrance:** from the ice plain behind the map. A frozen lake cracks open and it walks out over the ice.
- **Signature, Gate Breaker:** gates make it **angrier**, not slower. Every gate it reaches it freezes solid in one go: the gate stays upright but becomes an ice block for 8 s. Then it **walks straight through** and the gate shatters (a wreck you can rebuild). Each gate it breaks raises its rage, so it moves faster.
- **Telegraph:**
  - frost creeps over the ground in front of it;
  - a gate it targets gets a blue haze and a crack sound;
  - its rage shows as icicles growing on its back.
- **Counterplay:**
  - Do not lean on gates. Spread towers along the long stretch of road.
  - Fire/lava gadgets (Lava Ball) break its ice armour, and it takes double damage for 5 s.
  - The hero can thaw a frozen gate with Repair, so you lose no wreck.
- **Phases:** armoured (ice armour soaks damage); then exposed below 40%, when the armour breaks and it runs.

## Rotbloom (Mire continent: bog)

**Theme:** a walking flower of rot that makes the map itself hostile.

- **Entrance:** it grows out of the bog. Roots spread over three waves before it shows up, as decor that grows a little each wave, so the player sees something is coming without a countdown.
- **Signature, Overgrowth:** every 12 s it shoots spores at **a small group of build pads** (at most 3, never more than about a quarter of your towers overgrown at once). A pad hit by spores is overgrown: you cannot build there and a tower on it fires at 70% speed until the overgrowth is burned away by an explosion or burned clean by the hero. Vines visibly grow over the tower. It goes for the spot with the most towers.
- **Telegraph:**
  - the flower opens its petals, and a sweet sound rises;
  - the target spots get a green haze 2 s before impact.
- **Counterplay:**
  - Splash damage (cannon, mortar) cleans pads.
  - Do not stack all towers in one cluster.
  - Shooting down the spore pods in the air prevents the overgrowth.
- **Phases:** rooted, where it stands still on the bog and creeps forward by growing roots; then uprooted below 50%, when it walks and leaves rot.

## The Shattered (South continent: crystal ruins, 3 phases)

**Theme:** a crystal giant that splits apart when it takes damage.

- **Entrance:** from the old ruins. A sealed gate in the decor opens for the first and only time.
- **Signature, Splitting:**
  - Phase 1 is one giant.
  - At 66% it splits into **three shards** that each **take a different route** (land, air and the second road), each with a third of the remaining HP.
  - At 33% the surviving shards pull each other back together where they are. Too slow, and the merged giant stands closer to your base.
- **Telegraph:** cracks of light on the giant show the split point; each shard's route lights up briefly.
- **Counterplay:**
  - Cover every route: air defence and the second front matter.
  - Kill one shard fast and the final phase is weaker; each dead shard takes 30% off the giant.
- **Phases:** whole, split, rejoined.

## The Sealed One (Core: finale of the game)

**Theme:** the secret under the planet. The thing the crystals were guarding. It is not an animal; it is the planet's immune system.

- **Entrance:** it does not come to you. **The map comes to it.** At the start of the boss wave the ground near the start of the road cracks open, the road sinks there and a new road of violet light runs from the rift along the old road to the base, so it passes the whole defence (2026-11-05, Remco: the centre road of round 9 skipped the defence).
- **Signature, Recognition:**
  - It "learns". The tower type that dealt the most damage in the previous phase does **half damage** in the next phase (shown on the boss bar with that tower's icon dimmed). Remco judged full immunity too strong.
  - Each phase drains energy from one gate: its gun stops firing, but the gate keeps blocking and returns when the phase ends.
- **Telegraph:**
  - ORA warns by voice ("Adaptation detected: Railgun signature blocked").
  - The boss bar shows the immune icon.
  - The sealed crystals in the decor turn the colour of the immune type.
- **Counterplay:** build a mix of towers. A loadout with three or four different types wins; a monoculture loses.
- **Phases:** four, one per immunity. The last phase is the release: it opens, the crystals go white, and the epilogue starts.

## Extra bosses (already modelled)

- **Crystal Warlord (exists):** stays the mid boss of Crystal Plains.
- **Storm Matriarch:** reserve for a later continent, or the endless mode.

## Decisions

- **2026-11-04, Remco:** "hele vette ideeen". Bosses must appear in the Codex. Interaction on the field must be consistent. "Alles laten wegvallen is te OP", so effects stay partial.
- **Sealed One:** halved damage instead of immunity.
- **Still open, curator default until Remco says otherwise:**
  - Gravemaw may smash the colony drill rig as a story beat. It is decor only and costs nothing in gameplay.
  - The Cinder Matriarch is allowed to leave the road, as an exception for this boss only.

## Implementation notes (round 4a, 2026-11-05)

**Framework (`Assets/_Project/Scripts/Bosses`, assembly `Game.Bosses`; knows Core, Combat, Enemies, Towers, Levels, Economy; Boot creates it, nothing in the level knows it):**
- Data: `BossDefinition` (Game.Enemies) on `EnemyDefinition.Boss`: phases (`BossPhase` name + health share), `BossArrival` (Wave or ColonyRig), `BossSignature`, `BurrowPower`, `ClimbPower`, `DivePower`, impact kick, mechanic clip rate, Codex text (signature, how to see it coming, how to beat it). Generator step "Continent bosses" (`ProjectGenerator.Bosses.cs`) writes all six bosses; only Gravemaw has a running signature.
- `BossDirector` hands every continent boss a `ContinentBossBrain` (state machine on coroutines). While it acts the creature is *directed* (`Enemy.SetDirected`): its own walk, siege and attacks wait. Between acts it walks the road as any creature.
- Climb layer: `ClimbLayer` per cell (open, road, plateau, rock, fragile, quicksand, liquid, tower, gate, base) with heights; the rocks come from the formations as the environment drew them (`EnvironmentInstancer.Formations`), so logic climbs exactly the rock you see. Fragile decor (crystals, plants) is broken (`EnvironmentInstancer.Break`) with a shard burst. Routes: `GridRouter` (A*, no corner cutting next to a footprint), `BossRoutes` (walk back to the road, crack lines, surfacing spots).
- Climbing: a rock straight ahead at the clip's ledge distance is climbed (Climb_Up then Climb_Down at 3x speed, transform moved by the report's end offsets); a gate or the base ahead is climbed onto: it sags (`Tower.SetBurden` -> `TowerDisplacementView`), takes the boss's weight per second plus its blows, and the boss climbs down the far side when the gate falls or a burrow is due. Foot IK is not used: baked clips plus a slope tilt.
- Pools: a pool within reach of the road (kinds from `DivePower`) is dived into (Dive_Into_Liquid), the boss hides under the surface (model hidden, wake + glow, only blasts reach it at `SplashShare`, slow regen), swims at most 6 s (`PoolDiveRules`: once per pool per phase) and bursts out at the bank nearest the road (Surface_Burst, structures there take a splash, one kick). The HUD boss bar stays.
- Concealment: `TargetRegistry.SetConcealed` (towers cannot aim, splash still finds it); damage attribution: projectiles pass their tower (`ISourcedTarget`), the brain keeps a `DamageTally`.
- HUD: the boss bar marks every phase start and names the phase. Codex: bosses are listed only once met (`ProgressData.SeenEnemyIds`), with phases, signature, telegraph and counter.
- Camera: `LevelContext.BossImpact` gives one kick per real impact (break-out, throw, burst from a pool). Audio: `LevelContext.BossRumble` drives an earthquake loop in `SfxPlayer` that swells as a burrow nears the surface.
- Ledger: `EnemyStats.MechanicLossSeconds` (from `BossDefinition.FirepowerLossSeconds`: per burrow the expected hidden seconds plus a quarter of the field silenced for the throw) is spread over the boss's fight (`WaveLoad.BossFight` = main road at its pace plus the gate hold) and taken off every gun's seconds on it, capped at `BalanceRules.MaxBossMechanicShare` (0.35); `BalanceChecks.CheckBossMechanic` fails a boss above the cap. No existing band moved: bosses without boss data lose nothing.

**Gravemaw as built:** enters through a crack along the field edge to the colony rig, breaks out under it (Surface_Throw), the derrick falls across the view (decor only) and it roars; then it burrows straight back to the start of the main road (8 % along, 2.5x speed, not a signature burrow), because the rig stands next to the base and a fight from there would skip the whole road (curator decision). At 75/50/25 % it burrows: the dive faces the shortest crack, the crack runs round towers and gates (a cell clear where it can, never through a footprint, quicksand at a third of the cost), opens ahead of it in warm orange, the rumble swells, the target (top damage, still standing, not upset) gets the pulsing red ring for the last 3 s (a trip slows down to give the full 3 s). It surfaces beside the target with the tower on its right, the tower is flung (loses 40 % of its full health, never its last point, silent 1.1 s in the air plus 4 s tilted) and rights itself. A mine going off on it while it burrows ends the trip there: it surfaces dazed for 4 s with no throw. Sold or moved target: it surfaces, nothing is thrown. Below 25 %: 1.35x pace and 4 Skitters out of each crack. Health 1050 (tuned so the C1_06 finale with Gravemaw stays inside the HeroCap with the hero; re-tune when the real C1 finale exists).

**Placement:** the roadmap puts Gravemaw at the Crystal Plains finale (level 20, S5); only C1_01..06 exist, so it is not placed in the campaign. The developer boss arena (`Data/Levels/BossArena.asset` on the campaign; dev story viewer page "Boss arena") fights any boss in its list on Shard Ridge (C1_03, quicksand, rocks, two gates), Medium, 3000 money, 40 s to build, wave health x2.5, six Skitters; it pays and saves nothing.

## Implementation notes (round 4b, 2026-11-05)

**Brain split.** `ContinentBossBrain` is now only the shared body: the pool entrance, walking back to the road and off-road walks
round towers (`WalkTo`, clearance in whole cells from `BossDefinition.BodyHalfWidth`), climbing rocks and structures, pool
dives (with a pool-to-pool shortcut, `TryPlanDiveTowards`), and the helpers. Each signature is its own class behind
`IBossSignature` (`Begin`, `Entrance`, `NextAct`, `AtStructure`, `WantsOffStructure`, `OnRoad`, `Always`, `End`), picked by
`BossSignatures.For` from `BossDefinition.Signature` (never from the name): `BurrowSignature` (Gravemaw, unchanged
behaviour), `HuntSignature` (Cinder Matriarch), `GateBreakerSignature` (Frost Warden), `NoSignature` (the three not built
yet). What a boss did is kept in `BossRecord` (burrows, throws, hunts, smashes, eggs, frozen/shattered/thawed gates, rage,
armour, the largest share of the field one effect reached). Pure logic in its own classes: `ColdHunt`, `LavaTrail`,
`EggClutch<T>`, `GateRage`, `IceArmour`. Views: `TowerAuraView` (warm glow / blue haze / frost creep), `IceBlockView`,
`LavaTrailView`, `BoneScaleView` (icicles, armour plates), `CrackStyle` (burrow crack, heat line, frost) for `CrackLineView`.

**Shared rules added.**
- `BossArrival.Pool`: the boss rises out of the largest pool of its dive kinds whose way to the road joins it in the road's
  first half (a lake by the base would skip the fight; curator decision), the surface churns 1.6 s with a rumble, it swims to
  the bank nearest the road and bursts out (one kick, a roar). That pool counts as used for its first phase.
- Pool exits keep the whole body clear of towers where they can and never land in the two outer cells of the field (the
  4a "exit behind the decor" bug). Gravemaw's surfacing spot beside its target now pays for every other footprint under
  its body (it picks a clear spot when there is one).
- Walkers follow a boss over a gate it stands on: a gate with a boss's weight on it is *bridged* (`Tower.IsBridged`,
  `IBridgeable`): it holds no walker back and walkers within 1.6 m climb over its top (`StructureRegistry.BridgeLiftAt`).
- Damage scale on a creature (`Enemy.SetDamageScale`) for armour mechanics; a burning source (`IBurningSource`, the Lava Ball's
  `PathObjectDefinition.Burns`) is passed by path objects as the damage source.
- Bosses keep most of their colour when chilled (frost overlay 0.18 instead of 0.5), as they already skip the white hit flash.
- Codex: a boss with a signature says "Towers: see its signature" instead of "leaves them alone".

**Cinder Matriarch as built.** Rises from a lava pool. Every 0.5 s on the road she looks for the nearest standing tower that
slows (`ColdHunt.IsCold`: its own or any level's slow factor); none: she walks the road. She may set out while she has
smashed fewer than `max(1, floor(towers / 4))` and 14 s have passed since her last smash (one tower per approach). The hunt:
she rears and roars while a thin heat line (1.5 s) runs to the tower and it glows warm (light + glow pool); the line cools as
she sets off. She plans a strike spot 3.1 model units (Smash `target_forward_m`) from the tower with her whole body (half
width 1.8 m) clear of other towers, takes a lava pool as a shortcut when its far bank is at least 4 m nearer, walks there
off the road in her Hunt_Stride at 1.25x, two cells clear of every tower where a way exists (else one, else just round the
footprints), climbing ridges and breaking plants on the way, and smashes the tower into a wreck at the Smash impact frame
(`Tower.Smash`). A tower that is sold, wrecked or thrown meanwhile ends the hunt; one she cannot reach is skipped for the
fight. Her lava trail: a patch every 0.6 m from 1.6 m behind her pivot, 6 s, burning structures 14/s (x difficulty structure
damage) and walkers 4/s. Brood (phase 2, below 50 %): up to 2 clutches 30 s apart; she walks to the nearest pool bank, tail
over the lava, and three eggs drop between the first and last egg frame of Lay_Eggs. Eggs are creatures (`CinderEgg`, 70 hp,
shootable, held still by `BroodKeeper`, which outlives her): unshot after 10 s they play Hatch and two `FireLarva` climb out
at frame 32 and make for the road. The data names egg and hatchling.

**Frost Warden as built.** Rises from a frozen lake (ice dives too). On the road frost creeps 2.2 m ahead of it (pale glow +
mist); the next gate ahead within 7 m gets a blue haze and the ice-crack sound (`LevelContext.BossGateWarned`). At the
Freeze_Gate reach (2.4 model units) it stops and breathes: at the cast frame the gate becomes an ice block for 8 s
(`Tower.Freeze`: it still blocks, fires nothing, takes no blows; `IceBlockView` in the level's ice liquid material,
`BossGateFrozen` plays the freeze sound). It climbs onto the block (it sags, walkers follow over) and, timed so the impact
lands as the freeze runs out, crashes down through it (Shatter_Through, from the top to the ground beyond): the gate is a
wreck (`Tower.Smash`), shards, one kick; rage +1 (pace +15 % per gate, up to 4; icicle bones grow 0.2 -> 1.0 in
LateUpdate) and the Enrage clip. The hero's Repair on a frozen gate thaws it (`Tower.Restore` -> `Thaw`): with the Warden on
top it slips down the far side and the gate stands; before it climbed, it attacks the gate like any creature; that gate
cannot be refrozen for 12 s. Ice armour (`IceArmour`): while armoured only half of each hit lands; a burning hit (Lava Ball)
cracks it, the armour mesh hides and hits land double for 5 s; in its Exposed phase (below 40 %) it plays Armour_Break, the
armour and icicles go for good, every hit lands in full and it runs (1.35x, WalkEnraged). Its data has no health armour
layer any more (the ice armour is the mechanic).

**Arena.** `BossArenaDefinition` lists entries (boss + level); the dev Story viewer page "Boss arena" shows one entry per
boss with its level. No Crystal Plains level has lava or ice, so the generator copies a level and turns its water into the
liquid (`Data/Levels/Arena/Arena_Lava.asset` from Crysium Vein, 4 lava pools; `Arena_Ice.asset` from Quartz Flats, 3 ice
pools; nests on turned cells are dropped; both pass the level validator). They are arena only, not in the campaign.

**Ledger.** `BossDefinition.FirepowerLossSeconds`: Cinder = phases x a quarter of the field x 12 s rebuild (6 s), Frost =
3 expected gates x a quarter x 8 s freeze (6 s); `EffectiveHealthFactor` counts the ice armour as more health (Frost x1.6).
Both stay under `MaxBossMechanicShare` on the C1_06 finale fight (test `BossSignatureTests.Ledger_...`). Neither is in a
campaign wave yet, so no band moved.

## Implementation notes (round 4c, 2026-11-05)

**All six signatures run.** `BossSignatures.For` now also picks `OvergrowthSignature` (Rotbloom), `SplitSignature` (the
Shattered) and `RecognitionSignature` (the Sealed One); their numbers are data (`OvergrowthPower`, `SplitPower` with one
`ShardSpec` per shard, `RecognitionPower`), pure logic in `OvergrowthTargets`, `OvergrowthPads`, `SporeVolley`,
`ShardSplit`, `RecognitionTally<T>`, `GateDrainer`. New arrivals: `BossArrival.Ruins` (the level's boss entrance as a
sealed ruin gate) and `BossArrival.Rift` (the centre of the map); the brain runs the signature's `Entrance` for both.
All three wear their round-28 exports (new clip roles Rooted, Uproot, SporeVolley, Split, Rejoin, RiseOntoBase, Adapt).

**Rotbloom as built.** Roots announce it: `RootOmen` (run by `BossDirector` from the level's waves) reads which wave brings
a boss with roots and shows `RotbloomRoots` Stage1/2/3 at the start of the third, second and last wave before it
(`OvergrowthPower.StageFor`), at the bog bank nearest the road of the pool it will rise from; the arena compresses this into
three short escort waves (`BossArenaEntry.LeadWaves`). It rises out of that bog (`BossArrival.Pool`, bog). Rooted (phase 1)
it creeps along the road at 0.35x in its Rooted_Idle loop and drops a fresh Stage1 root clump 2.4 m ahead every 3 m. Every
12 s (the first after 6 s) it blooms: it picks the fullest cluster of standing towers within 16 m (cluster radius 2.6 m), at
most 3 pads, never past a quarter of the towers overgrown counting the pods in the air; the target pads get a green haze,
`LevelContext.BossBloomed` plays a sweet rising chime, the Spore_Volley petals open and one `RotSporePod` per pad leaves
`Socket_Spore` between the first and last shot frame, timed to land as the 2 s warning runs out (at least 0.7 s in the air)
on a 3.5 m arc. A pod is a creature on the air layer: air towers shoot it down and its pad is spared. A pod that lands
overgrows its pad: `LevelContext.ClosedPads` (placement refuses it, "Overgrown: burn it clean first"), the tower on it fires
at 70 % (`Tower.Overgrow`) and `RotOvergrowth` grows over it in 1 s (blend shape Retracted 100 -> 0). Any splash explosion
(`ProjectilePool.Exploded`) whose radius reaches a pad burns it clean, and so does the hero's repair on the tower it guards
(`Tower.Restore` clears the overgrowth; `CanRestore` counts it); the vines flare and pull back. Below 50 % it plays Uproot
(roar and one small kick at frame 36), walks at full pace in its Crawl and leaves an 8 s rot trail (green glow patches). Its
bog dives are the shared body's. When it dies its vines wither and every pad opens.

**The Shattered as built.** It breaks out of a sealed ruin gate: the arena copy of Fault Line (`Arena_Ruins`: two roads, two
flight paths) carries a ground boss entrance a few cells off the main road near its first bend (the generator picks it);
there the ground cracks with five lines of light for 1.6 s with a rumble, then a crystal burst, one kick and a roar. Within
8 % above its split point a cold light pulses at its chest. At 66 % it plays Split; at the burst frame (28): a flash, one kick,
the shatter sound (`LevelContext.BossSplit`), and the three shards leave at their report offsets, each with a third of what it
had left (`ShardSplit.ShardHealth`, `Health.Refill`): the crawler on the giant's own road, the runner on the other road, the
flyer on the first flight path (`ShardRoute`, `EnemySpawner.SpawnOnPath`); each plays Spawn in place, then takes its route,
and each route lights up for 2.5 s. The giant is hidden and out of reach while split; its health follows the shards' together,
so the HUD bar keeps telling the truth. At 33 % the survivors play Rejoin and fly into the giant, which forms where the one
nearest the base stands (a flyer leader forms it on the nearest road cell), with what they have at that moment less 30 % per
dead shard (`ShardSplit.RejoinHealth`); its phase-3 look holds the shells at 0, the crystals at 1.35 and a dead shard's spire
at 0.35 (`BoneScaleView`). A shard that reached the base is gone without a penalty; if every shard dies the giant dies.

**The Sealed One as built.** "The map comes to it", done in light (no terrain change, see MODEL_TODO): hidden at the open cell
nearest the field's middle where its body is clear of towers, the ground rumbles and six violet cracks open for 2.5 s, then
the new road (a wide violet seam, `CrackStyle.Seal`) runs from the rift to the base round the towers in 2 s and stays lit; it
rises (its Rise clip; the seal breaks at frame 22 with a burst and one kick, then it can be shot) and the cracks die down.
It hauls along that way off the road (`WalkTo` with clearance, climbing rocks; a dark furrow opens behind it), falls back to
the road if no way fits its body, and at the base plays Rise_OntoBase: the base takes 120 (x structure damage) at the crush
frame (96), then a blow of its gate damage every 3 s while it lies there (`RecognitionPower.BaseBlowSeconds`). Recognition:
`RecognitionTally` counts each phase's damage per tower type; at each phase change it plays Adapt and at the pulse frame (30)
learns the type with the most damage, which lands only 50 % in the next phase (`Enemy.SourceScale`, never immunity);
`LevelContext.BossAdapted` carries ORA's line ("Adaptation detected: Cannon signature dampened", shown under the boss bar for
5 s with an alert tone) and `LevelContext.BossDampened` puts that tower's picture, greyed, beside the bar; the glow at the
rift (standing in for the sealed crystals) takes the tower's colour. Each phase drains one gate's gun (`GateDrainer`,
`Tower.SetDrained`: the gate still blocks; violet haze and a drain thread), the nearest gate with a gun other than the last,
and gives it back at the phase's end and when the boss dies. Its glow is the art's Phase1..4 emission set per phase
(`ModelArtDefinition` emission sets, `EnemyView.SetEmissionSet`). Its end is the Release clip (mapped to Death): at the white
frame (64) the corpse swaps to the white Release set (`ModelArtDefinition.DeathEmissionSet`). In the arena the fight ends
there (the victory panel comes up over it); the epilogue is a later round.

**Fixes from the 4b review.**
- The Matriarch's grey wash at the smash was the chill overlay: the Cryo she hunts chills her, and the overlay adds its colour
  as light over her near-black body. A chilled boss now cools only a shade (overlay colour x0.12, `BossFrostShade`).
- The world "BOSS" label no longer sits over the HUD bar: while a boss lives (the bar names it) the marker drops its word and
  keeps its arrow 185 px below the top (`BossMarkerView.BarClearance`), also when pinned to the top edge.
- The Codex pedestal seats a model by its posed near-LOD vertices (baked skinned meshes; the far LOD and particles left
  out) and frames a creature again once its animator has posed its idle: it was framed in its bind pose with the far LOD
  (longer, lower legs), so the Matriarch hung a metre above the disc.

**Arena.** All six bosses are entries: Rotbloom on `Arena_Bog` (Glass Meadows, water -> bog, 3 lead waves), the Shattered on
`Arena_Ruins`, the Sealed One on the arena's own Shard Ridge (its gates need an upgrade to have a gun to drain).

**Ledger.** Rotbloom: 4 volleys x a quarter x 30 % slower x 20 s overgrown = 6 s; the Shattered: 18 s split / 3 shards = 6 s
(a third of the field on the wrong route); the Sealed One: 4 phases x 8 % (one gate gun) x 15 s = 4.8 s, and the learned
weapon's lost half counts as health (x1.225: 75 % of its health x 30 % top share x 1). All inside `MaxBossMechanicShare` on the
C1_06 finale fight (`BossRound4cTests.Ledger_...`); none is in a campaign wave, so no band moved.

**Fixes from the 4c review (round 4d).**
- The learned weapon beside the boss bar is a card (92 px, was 60): the tower's own picture from the tower bar
  (`ModelIconRenderer`, same key and model as the card, so the cached picture), drained of colour (a grey copy made once per
  tower), a violet strike across it and the share it loses ("-50%" from `RecognitionPower.DampenedShare`) on the
  `Card_Disabled` frame. The round-4c icon showed as a solid red square; the rebuilt card shows the real picture.
- On the base the Sealed One no longer freezes on Rise_OntoBase's last frame: it stands on the base roof
  (`ClimbLayer.BaseHeight`) in a loop, its own `OnBaseLoop` clip (new role 64, Blender name `OnBase_Loop`, played from the
  rise's pivot) when the art has one, else the data's stand-in (`RecognitionPower.OnBaseStandIn` Idle at
  `OnBaseLoopRate` 0.8). A phase change on the base plays Adapt over the roof (learning at its pulse), then the loop again.
- The victory panel waits for a boss's end: `BossDefinition.ResultHoldSeconds` (written by the generator from the art's
  death clip: the Sealed One's Release, 5.67 s; 0 for the others); `ResultView` holds a victory that long after the kill
  (game time, so a pause holds it too) and asks the story again before it shows.
- The Codex pedestal frames tall creatures whole: the fit measures the pedestal as it stands (its renderers' bounds, the
  foot flares wider than the top), the rings the turning model sweeps up to its padded top, through the camera's tilt,
  and centres them with as much room above as below (`ModelPreview.FitView`).
- Rotbloom's cluster radius is data (`OvergrowthPower.ClusterRadius`, generator `BossSpec.ClusterRadius`) at 3.5 m (was 2.6):
  cells are 1 m and a defence stands 2.7-3.2 m apart, so at 2.6 almost every tower was a cluster of one. With 12 towers the
  first volley now sends 3 pods. The quarter cap is unchanged and still decides small defences (5 towers: 1 pad at a time).

## Round 5: round-29 art and the boss look (2026-11-05)

**Import (targeted, no full art rebuild).** `Tools/PlayTest/Scripts/ApplyBossFx.cs` rebuilds only the re-exported bosses and
shards (`BlenderArtBuilder.RebuildOnly`: FrostWarden, SealedOne, ShardRunner, ShardCrawler) and runs the new
`BlenderBossFxBuilder` (menu `Tools/Generate/Blender Boss FX (sync, marks, sheets, boss props)`, generator step "Boss FX art"
after "Marker art"). It writes `Data/Vfx/BossFxArt.asset` (`BossFxArt`, Game.Core, hung on the `VfxLibrary` and registered at
boot like `MarkerArt`): six ground marks on the new `Game/BossDecal` shader (albedo + R glow / G threshold pack, `_Progress`,
`_OpenDistance` along UV2.x, lips raised by UV2.y, a dark gap along the strip's middle, glow only in the fissure), eight
flipbooks on `Game/VfxParticle`, the ruin gate, seal crystals and ice block (`Data/Art/Bosses/*.asset`), the drained gate
maps and the dampened overlay; the drill wreck goes on `ColonyRigDefinition.Wreck`. Import settings per README round 29
(`BlenderBossFxCatalog.Preprocess`: albedo sRGB + transparent alpha, packs linear, strips Repeat U / Clamp V).

**Wired where 4a-4c had stand-ins.**
- Crack lines (`CrackLineView`): `CrackStyle.Burrow` and `Seal` draw the `GroundCrack` strip (Seal tinted violet), `Furrow`
  the furrow strip; the blade seam is gone for marks, the halo stays faint. Strips start at a random U (`GroundMark.Strip`).
- Lava and rot trails (`LavaTrailView`): one painted piece per patch, turned along the walk, random U, heat cooling from 0.2
  (lava) / decay (rot), fading over the last 30 %.
- Frost creep (`FrostCreepView`): a random frost cell every 1.2 m ahead of the Warden, grows in 1 s, holds 4.5 s, thaws; a
  large held cell under every frozen gate.
- Ice block (`IceBlockView`): the painted block on the gate's own transform, as tall as the gate and wider, centred on it,
  snaps from 0.9 to 1 in 0.3 s, fades on a thaw, two shard bursts on a shatter. The Liquid_Ice cube stays as the fallback.
- Dive wakes (`WakeView`): the liquid's sheet lies flat over the boss, top along its swim, ~2.4 x its half width.
- Spore cloud on a landed pod, vine burn round a burned pad (`VinesView`), shard flash at the split and played backwards
  before the rejoin (`FlipbookFx`).
- Ruin gate (`RuinGateView`): the Shattered's entrance stands the gate facing the camera; closed and breathing, rumble 0.8 s,
  Open (seal shards at 30, light and a point light from 38), the giant appears in the flash and walks out when it is open.
- Seal crystals (`SealCrystalsView`): five round the rift on open ground, rising out of it, dormant lilac, the learned
  weapon's colour x 2 (round 7; was x 2.4), white at the release.
- Old road sinks (`RecognitionSignature.SinkOldRoad`): a `RoadSink` strip over the old road nearest the rift while the new
  road rises (decor only; walkers keep the old road).
- Drained gate (`DrainedLook`): the drained gate's renderers get the drained emission map by property block, back on release.
- Sealed One on the base: its own `OnBase_Loop`; the base takes its blows at the strike frames 34 and 82 (same damage per
  second as the stand-in's timed blows) with dust and a short kick.
- Drill wreck: shown in place of the derrick at Gravemaw's smash, its fallen top away from the hole.
- Dampened card: the `UI_Dampened` ring in the adaptation violet replaces the drawn strike.
- Shard climbs: ShardRunner and ShardCrawler carry Climb_Up/Loop/Down (role mapping only; the shards still walk their route,
  no shard climb logic yet).
- Frost Warden scale fix: `BoneScaleView` no longer holds the armour plates (`GateBreakerPower.ArmourBones` removed); it still
  grows the icicles with the rage.

**Look changes (judged from the game camera).**
- Gravemaw's crack is a fissure: lips raised 0.22 m with lit slopes, a dark gap, the molten glow only in its core (edge glow
  0.15), dark basalt chunks heaved up along both sides as it opens, dust and sparks at its head; cools over 10 s (glow first).
- Frost Warden: its ice (second slot and armour) drops the baked albedo and glow maps, which carried warm brown and orange
  patches that read as dirt and post-its, for one cold tint (0.32, 0.52, 0.72), smoothness 0.85, a bright cold rim and a
  faint glow (`IceLook` on the catalog row); cold mist rolls round its feet and a blue point light hangs on its chest. The
  model itself is still the weak point: see MODEL_TODO "Ronde 5 Unity" (FW1-FW7).

## Round 7: Blender rounds 30-31 in the game, clean cracks, real rig destruction (2026-11-05)

**Import (targeted).** `Tools/PlayTest/Scripts/ApplyBossFx.cs` rebuilt the Frost Warden (round 30, before Remco parked
it for a new body plan; not re-shot), then the Oozer and the SlimeMonarch (round 31 `Strike`), and runs
`BlenderBossFxBuilder` (new: `Gravemaw_CrackSlabs`, the re-exported `Gate_IceBlock`, `SealCrystal` and the rigged
`Cutscene_Drill_Wreck`). The Warden's clip facts come through the import (`round28.clips` in its report): Walk /
WalkEnraged 1.184 / 1.539 per loop, Climb lip 1.657, end 4.617 (+-1.5), Freeze_Gate gate 2.6, Shatter_Through gate
2.6, impact frame 48, end 3.078 (verified on `Data/Art/FrostWarden.asset`). Submerged depth (5.972) is not read by
Unity: the submerged clip stays under its pivot by itself. Nothing placed armour by plate number (only the icicles by
name), so the plate remap needs no code. The round-5 `IceLook` flat tint is off (`FlatColour = false`, white tint):
the baked cold maps show, with the cold rim and glow x 2.

**Gravemaw's cracks (Remco: "sommige overlappen elkaar").**
- A crack is planned on the grid and then shaped (`CrackPath`, called from `BossRoutes.Crack`): pulled straight where
  no costlier ground lies between, back-tracking points dropped, corners rounded (Chaikin, cut at most 1.2 cells).
  The cell staircase whose 3 m strip folded over itself at every step is gone.
- A new burrow keeps off the ground of cracks still on the field (`OldCrackPenalty` on their cells), and any old crack
  it still comes within a strip width of cools away in 0.8 s during the dive, before the new one opens there
  (`CrackLineView.Fade(seconds)`). The dive hole replaces the line's start when the start lies behind it, so a line
  never doubles back at its first bend.
- Probe on the arena: entrance, return and two signature cracks, no self-overlap and no two live cracks closer than a
  strip width (3.7 m apart at the closest).
- Slabs (`CrackSlabPool`, `CrackStyle.WithSlabs`, burrow only): `CrackSlab_A..D` along both lips every 1.8-3.4 m (at
  least three), random variant, scale 0.8-1.3 x 1.6 (the export is too small at the game camera), sunk 0.03-0.08,
  hinge 0.32 of the mark width out from the line, rising edge over the fissure. 65 % on the far lip (edge towards the
  camera); on the near lip they turn 75 % along the crack so the edge, not the flat top, shows. Never on a tower or gate
  footprint (hinge and tip checked). They tip up off their hinge as the crack opens past them (0.4 s, small overshoot),
  glow x 3 and cool with the strip, and settle back and sink as it fades. Pooled per field: built once, reused.

**Colony rig (Remco: "de toren die omvalt ziet er heel dom uit").** Gravemaw bursts up under the derrick at the
break-out cue: one kick, a dust cloud, sparks and debris; the intact rig is swapped for the rigged wreck, which plays
its `Collapse` clip (role Death, round 31) from the standing pose into the wreck; at `top_hits_ground` (frame 40, data
from the report as the clip's event) a second dust and sparks. No toppling of the intact model any more (without the
art the derrick just stops). The haulers: the nearest crashes in a tumble beside the rig and sinks away after 6 s,
the others scatter away from the hole and are gone after 4 s; their cargo glow goes out. Nothing else in the game
reads the rig (only `ColonyRigView`), so nothing treats it as working afterwards.

**Ice block and seal crystals.** The ice block is fitted per axis to the gate it holds (measured in the gate's frame:
width = span + 0.2 m a side, depth likewise but never under 0.55 of the width scale, height as before), so a G1 gate
gets a thinner, lower block. Seal crystals: learned tint x 2 (was 2.4) so the facets read.

## Round 8: the Rime Colossus in the game, Gravemaw's torn fissure, gate crossings (2026-11-05)

**Frost Warden = Rime Colossus (Blender round 32).** Imported with the targeted path (`ApplyBossFx.cs`, art list now
`FrostWarden` only). The re-measured clip facts arrive through the report: Walk / WalkEnraged 1.429 / 1.857 per loop,
Climb edge 2.4, end 5.571 (+-1.5), Climb_Loop 1.143, Dive / Surface end 2.8, Freeze_Gate gate 4.3 with the breath at 30,
Shatter_Through 100 frames, gate 4.3, impact 35, end 3.8; FreezePulse still at 30. `BodyHalfWidth` 1.25 -> 2.0 (the
body is about 6 m over the fists at Unity scale 1, 4.8 m at the titans' 0.8). Nothing placed armour by plate number, so
the new bone map needs no code. The health bar follows the art's measured height. The flow lines up in the arena: it
stops 3.4 m (4.3 x 0.8) before a gate, breathes, climbs onto the block, the fists come down through it at 35, Enrage,
armour break below 40%, icicles grow and leave with the armour. The ice look stays (no flat colour, glow x 2); the
screenshots read as cold ice gauntlets and floes, so the material was not touched. `FrostWardenArenaTests`: with the
new body (longer climb, shatter and freeze distances) the fight reached the base at 11% health with the test's 2%/s
extra damage after the second gate; the test now adds 3%/s (balance itself is untouched; checked with the old width
too, the width was not the cause).

**Gravemaw's crack as a torn fissure** (Remco: "a thin glowing laser line", then "die open start met recht einde en die
rechte hoek zijn niet zo mooi"). All look values sit in `CrackStyle.Burrow` + `FissureLook` (data):
- 4.4 m wide (was 3), a dark gap of 1.4 m (`GapHalf` 0.16) between two lit lips (`LipGlow` 0.75, band 0.025) over a
  faint core deep in the gap (`CoreGlow` 0.3); the lip glow is uneven along its length. `Game/BossDecal` gained
  `_LipGlow`, `_LipBand`, `_CoreGlow` and `_HeadTaper`; the other marks keep their old profile (defaults).
- Shape: the planned route is resampled every 0.5 m and smoothed three times with a 2.5 m moving average (corners become
  curves of a few metres), then meanders up to 0.35 m sideways; its width breathes +-22 % and narrows to a hairline over
  2.6 m at both ends (at most 30 % of a short crack). The tearing head comes to a point over 2.4 m (vertex pinch in the
  shader). Two hairline branches split off near each end and open with the line.
- The view keeps the callers' length (the drawn line is a little longer), so the head still follows the boss.
- Footprints: the strip fades out (vertex alpha, glow too) where it would reach under a tower or gate; chunks and slabs
  keep off them as before. The no-overlap rule uses the wider width.
- Slabs: 2.0 x the export (1-1.7 m plates), hinge on the lip outside the gap, stood up 35 degrees past their modelled tilt,
  glow 0.25 of the hot material, so they read as tilted basalt plates on the lips, not glowing discs over the gap.
- Rubble: up to four debris meshes thrown up and tumbling at the open end (16/s).

**Gate crossings (Remco: "het lichaam beweegt alsof hij erop staat op vlakke grond").** On a gate the bosses played the
rock-climb crawl cycle in place, which reads as walking. New roles `GateMount`, `GateStraddle`, `GateDismount`
(Gravemaw's `Gate_Mount`, `Gate_Straddle_Loop`, `Gate_Dismount` in `BlenderArtCatalog`, events from the report's
round28 block like the other mechanic clips). `ContinentBossBrain.ClimbStructure`, `ClimbDown` and the Frost Warden's
`OntoTheIce` use them when the model has them; until the export arrives they fall back to `Climb_Up` / `Climb_Down`,
and on the gate the climb loop is held on one pose (`Straddle`) instead of crawling.

**Hit reactions** (all creatures and bosses): `HitReaction` rations the flinch (clip and procedural knock): a cooldown
(0.7 s, bosses 1.8 s, never shorter than the hit clip, `EnemyDefinition.HitCooldown` overrides), chip damage gathered
until 4 % (bosses 1.5 %) of its health, and never during a scripted mechanic clip. Every hit still flashes and bleeds.

## Round 9: real gate crossings, Rotbloom batters, finale health (2026-11-05)

**Import (targeted).** `Tools/PlayTest/Scripts/ApplyGateClips.cs` syncs and rebuilds only Gravemaw and the Cinder Matriarch
(Blender round 33) and runs the "Continent bosses" generator step. The importer reads two new series from the report's
round28 block: `weight_frames` (added to `BlenderTravelClips.SeriesKeys`; the clip's cue is the first weight frame, its
event the `impact_frame`) and the per-frame `gate_burden`. Both land on `ModelClip` (`MomentSeconds`, `BurdenAt(seconds)`,
interpolated per frame at 30 fps). Clips stay in model metres; `BossClip` multiplies by `ModelScale` as before.

**Gate crossing (`ContinentBossBrain.CrossGate`)**, used when the model has all three of `GateMount`, `GateStraddle`,
`GateDismount`; the base and bosses without them keep the rock climb (`Climb_*`), and rocks always use `Climb_*`.
- It sets off at the mount's own reach (`gate_forward_m` x scale: Gravemaw 2.56 m, Matriarch 2.88 m) and starts exactly
  there. After Gate_Mount the pivot goes onto the gate centre **on the ground** (not `GateHeight`); the clip lifts the body.
- The sag follows the clip frame by frame (`Tower.SetBurden(clip.BurdenAt(t))`) through mount, loop and dismount instead
  of a flat 1 (lying ~0.21, a heavy push ~0.6-0.7, 0 from `off_gate_frame`). While it lies there the burden never drops
  under 0.05, so the walkers keep their bridge.
- Each weight frame is a blow: the hit, dust at the beam and a short kick (0.45 x the boss's impact kick). The straddle
  loop plays at rate 1 so the blows fall on the authored frames; a loop blow carries the creature's damage per attack
  interval (`BlockadeDamage x loop length / weights / AttackInterval`), so a gate falls exactly as fast as under the old
  timed blows. Mount and dismount play at `ClimbPower.GateCrossRate` (1.5).
- It leaves when the gate falls (at once) or, when its mechanic calls (Gravemaw's next burrow), at the end of a loop, since
  the dismount starts in the loop's first pose. States for probes: `ClimbingGate` (mount), `OnGate`, `ClimbingDown`.
- Fix found on the way: the structure ahead was "the nearest blocker", which can be the hero holding the post (`HeroTarget`
  blocks walkers too). Gravemaw climbed onto Spencer in the first probe. `ContinentBossBrain.Ahead` now only takes gates
  (towers) and the base.
- The Frost Warden climbs its ice block with the rock climb (a block is not a beam), as before.

**Rotbloom batters (curator decision, README round 33: rigid round body, short legs).** New `GateBehaviour` on
`ClimbPower` (replaces `ClimbsGates`): `Climb` (Gravemaw, Matriarch, Warden, Shattered), `Batter` (Rotbloom), `Besiege`
(the Sealed One, unchanged: held and biting like any creature). A batterer stops with its body clear of the gate (half
width + `RamClearance` 0.5 m) and rams with `RamClip` (Rotbloom: Roar, jaw wide at frame 16) at `RamRate` 1.2; the blow
lands on the clip's moment (dust, hit, small kick) and carries the damage of the attack intervals the clip lasts. Between
rams it is free for a step, so its volleys still run. `BossRecord.Rams` counts them.

**Gallery** (`scratchpad/gallery/Gravemaw/21-29`, `CinderMatriarch/21-28`, `Rotbloom/21-23`, captions in `index.txt`).

**Health retuned for the finales** (Docs/BALANCE.md, "Continent bosses as played"): Gravemaw 1050 -> 2200, Cinder Matriarch
5000 -> 2750, Frost Warden 4500 -> 1550 (x1.6 ice armour in the ledger), Rotbloom 4000 -> 3350, the Shattered 4800 -> 2450,
the Sealed One 9000 -> 3100. Rewards follow threat.

## Round 10: the Sealed One rises at the start of the road, boss health per difficulty (2026-11-05)

**Remco:** "ik zou gokken op het pad veranderen zodat hij vanaf het begin begint ofzo?" (round 9: its road from the centre
skipped the whole defence; the probe lost with 85 % left).

**The Sealed One as built now.** The map still comes to it, but at the start of the road: the rift opens at
`RecognitionPower.RiftRoadShare` (0.06) of the main road's length from its start, at the open cell nearest that point where
its body is clear of every tower (search up to 10 cells). The rumble, the six violet cracks, the seal crystals, the sinking
of the old road round the rift and the Rise clip are as before. The new road of violet light (`CrackStyle.Seal`) now runs
from the rift onto the old road and along it to the base (`OffRoadRoute.Build`), lighting up in `RoadRiseSeconds`; it comes
up facing down that road. After the rise it drains its first gate and takes the road like any creature (`JoinRoad`), so
every tower along the road gets its turn and every gate holds it (`GateBehaviour.Besiege`: it bites its way through); its
furrow (`CrackStyle.Furrow`) opens along the road behind it by its path progress. The cross-country haul (`Haul`, its
retry timer) is gone. Recognition, the gate drain, Rise_OntoBase at the base, the on-base loop and the Release are
unchanged. Codex texts (signature, telegraph) say "near the start of the road".

**Boss health per difficulty (Remco: yes).** `DifficultyProfile.BossHealthShare` Easy 1.0 / Medium 1.0 / Hard 1.45 /
Psycho 1.7 on top of the creature health, for continent bosses only (level bosses keep the wave solver's sizing), derived
from the ledger so the boss asks Medium's share of each difficulty's hero cap (Docs/BALANCE.md, "Boss health per
difficulty"). **Health set by a probe that plays like the expected player** (upgrades, research, drones, hero abilities,
every difficulty): Gravemaw 2300, Cinder Matriarch 1450, Frost Warden 1300, Rotbloom 3100, the Shattered 2200, the Sealed One
3350. In those runs the Sealed One rises at the road's start, walks the whole road, is held at the last gate on every
difficulty and costs the gates 750-2400 hp; it never reached the base against a mixed defence (round 9: it skipped the
defence and won with 85 % left).

## Round 12: Gravemaw's 3D fissure, rounded trail ends, wide bodies inside the kerbs, Spencer's boss intros (2026-11-05)

**Gravemaw's fissure is real geometry** (Blender round 34, `Exports/Gravemaw_Fissure`). `FissureKitView` lays the kit along
the drawn crack line (the smoothed, blurred `CrackPath` route): `Fissure_Start` at the dive hole, straights A/B/C behind it,
`Fissure_Head` pointed at the far end, a side crack every 6-10 m. Every module is bent onto the line vertex by vertex (spline
deform), so the crack follows the rounded path without a kink, never ends square and never overlaps itself (the planner's
clearance is unchanged); the curve modules are not needed. All modules go into one mesh, scale `BossFxArt.FissureScale` 2
(about 2.9 m wide, 1.1 m gap). It tears open behind the boss through the `Closed` offsets (UV2/UV3, `Game/FissureRock`
vertex reveal over `FissureOpenLength` 2.2 m), the core glows fresh behind the front and cools over `FissureCoolSeconds`
10 s (the bottom last), and when the crack heals the lips fold flat and the gap shuts. The gap is cut out of the ground in
two stages (`Game/DepthPunch`): a mark pass sets a stencil bit where the opening passes the depth test (pulled 0.3 m along
the view ray so it wins over road tiles), a clear pass writes the far depth there; a single pass cannot do both, because a
fragment that writes its own depth is also tested with it. The painted `BossFX_GroundCrack` strip, its glow halo and the
`Gravemaw_CrackSlabs` no longer draw along a burrow crack (they stay as the fallback without the kit).

**Ground marks never end square** (Remco: lava trail, rule for every trail). `Game/BossDecal` rounds every mark that carries
end data (UV3) to a blunt, slightly torn point: strips over 0.9 of their width (at most 30 % of their length), trail pieces
over the half length the next piece covers. The Matriarch's lava and Rotbloom's rot trail narrow over their last pieces
into a tail (`LavaTrailView`, the oldest pieces 0.4-1 wide); frost cells are round patches; the furrow, the violet road and
the sinking road round their corners (`GroundMark.Rounded`, 1-1.5 m). Slime smears were already radial.

**Wide bodies stay inside the edge stones** (Remco: the slug boss under the kerb). `EnemyDefinition.BodyHalfWidth` (data,
measured from the models) keeps a walker's lane inside the kerb's inner face less 5 cm; wide creatures and bosses walk the
centre. A body that lies on the ground (`GroundBody`: Oozer, Slime Monarch) narrows across to fit between the stones
(`Enemy.BodyFit`, never below 0.8): the Slime Monarch walks at 0.89 of its width.

**Boss intros** (Docs/BOSS_INTROS.md): `BossIntroDefinition` per continent boss on the story (`Data/Story/Intros`), lines
from the doc, curator barks for later meetings. First meeting (`ContinentBossBrain.FirstMeeting`, the Codex seen flag read
before the spawn sets it): when the boss is out (Surfacing, Bursting, Roar or Road) the game slows to 0.25 (never pauses),
the camera glides to the boss (with Spencer when he is within 16 m), Spencer turns to it and plays Startled, holds the aim,
lowers the gun (Lower_Weapon) and holsters; his line shows over his head, the visor cut-in (a camera on his head bone into a
texture, rim light in the boss's tint, the visor hides his face) slides in at the right, the Codex note shows under the boss
bar, then ORA and Voss answer in their own dialogue box (fast typing, auto-advance). Ends within 5.8 s or on any button
(keyboard, mouse, gamepad), at once on a pause; never during a held story box. Later meetings: a bark from the pool. Mid-fight
barks at the signature moments (`BossMoments`), with Point_Shout on Spencer's upper body, 14 s per boss and 6 s between any two
(`BarkCooldown`); none while he is down.

## Round 13a: one ribbon per trail, the Sealed One's furrow only behind it (2026-11-05)

**Lava and rot trails are one ribbon** (Remco: the Matriarch's trail was messy in turns, pieces painting over each other, and
"a row of tiles"). `LavaTrailView` no longer places a `GroundMark.Piece` per patch; it rebuilds ONE ribbon mesh each frame
from the live patches of the `LavaTrail` ring (oldest first, `SlotInOrder`), Catmull-Rom smoothed (3 samples per patch),
reaching on to where the boss walks now (`LavaTrail.Head`). U runs along the distance walked (no repeating tiles); vertex
colour R carries each point's own cooling (`_VertexProgress` on `Game/BossDecal`) and A its fade, so the trail cools along its
length. The head rounds off over 0.7 of its width, the tail over 1.6 and narrows to 0.4 (UV3 end data). A lift (dive, climb)
starts a new stretch (`LavaTrail.StartsRun`). Where the ribbon crosses itself (tight turn, U-turn) every pixel is drawn once:
the trail material sets stencil bit 32 where it draws (`_StencilRef/_StencilComp/_StencilPass`, newest part drawn first so it
wins) and skips pixels already set; `_ClipBelow` 0.03 keeps faint fringes from claiming pixels. Rotbloom's rot uses the same view.

**The Sealed One opens the ground only where it has been** (Remco: the whole road dug open and lit violet at once looked
wrong; gates and the hero stood on sunken ground). The up-front violet road (`CrackStyle.Seal` along the whole way), the
sinking old road (`RoadSink`) and `RecognitionPower.RoadRiseSeconds` are gone; the rumble, the rift, its six cracks, the seal
crystals and the Rise stay. The furrow (`CrackStyle.Furrow`) opens to the point nearest its pivot (`CrackLineView.AlongNear`,
searched forward only), half its body width behind it, and pinches to a point over 3 m (`_HeadTaper`). It never runs under a
structure or the hero: `GroundKeepOut` publishes circles (base 1.6 cells, every gate half the road width, towers and wrecks
0.6 cells, the hero 0.7 m, each + 0.6 m) as shader globals, and `Game/BossDecal` on a mark with `_KeepOut` ends the mark
there like a real end (torn taper over 1.4 m, lips flat, nothing inside). Codex signature text and `BossArrival.Rift` updated.

**Base radar** turns about world up (`Spinner.Initialize(speed, true)`; the cutscene `FieldBase` turns dish and crystal about
the parent's up), so the old base's -90 X node rotation no longer tips it; `RenderersEndingWith` also finds `_LOD1` twins.

**Round-36 attack clips** (`Tools/PlayTest/Scripts/ApplyAttacks36.cs`): `Strike` (Attack, hit frame 10) for Skitter, Prowler,
Roller, Cragback, Bulwark, Shardback, Shalebat and every bat skin (shared `bat` array), plus their corrupted skins; Skyray
Dive, Stinger Strike, Mireleech Latch (scale keys on Seg_1..6/Head kept) reimported. Wisp: `Strike_Float` (Attack) and
`Strike_Ground` (new role `GroundedAttack`), picked by stance in `CreatureClips.Attack`. With an own strike the impact and
bolt fire on the clip's hit frame (`CreatureClips.StrikeLanded`), falling back to the end of the attack so no blow is lost.

## Round 13b: bosses and the decor off the road, intro leftovers (2026-11-06)

**Decor off the road** (Remco: "when the bosses go off-road they walk through things like the crystals or rises, and
nothing happens"). Every imported decor mesh now carries a `DecorContact` (data on `EnvironmentMesh`, set by the
environment generator per decoration kind or kit piece in `BlenderEnvironmentCatalog.ContactOf`; logic never asks a name):
- **Breakable** (crystals, ice shards, flora, eggs, dead trees, reeds, small rocks and basalt spikes, the crystal vein):
  smashed when the body overlaps it (`BodyHalfWidth` x 0.8 plus half the piece's reach, every 0.2 s while it walks or plays
  a mechanic clip). A shard burst in the piece's family colour (crystal pink, flora green, stone dust), the brittle icicle
  impacts pitched down (`SfxLibrary.DecorSmashes`, `LevelContext.DecorSmashed`), and the piece is swapped for its remains
  (Blender round 38, `Tools/Blender/build_decor_remains.py`: `Remains_Crystal` snapped stumps and strewn shards,
  `Remains_Flora` torn bulb and limp blades, `Remains_Stone` rubble), fitted to the piece's footprint, for the rest of the
  level (`EnvironmentInstancer.Break` / `LayRemains`; `EnvironmentSet.Remains`). Before, only formation crystals broke, at
  0.9 m from the pivot, with no remains; the crystal veins and pool-rim plants never did.
- **Obstacle** (spires, columns, border cliffs): every cell under the piece is `SurfaceKind.Obstacle`, closed to walks and
  cracks; a walk keeps its body clearance from it as from a tower (`BossRoutes.Crowded`), and a rock climb never lands
  across one (`BossRoutes.ClearOverGround`).
- **Climbable** (boulders, drifts, scree, terrace steps): a low rise the body walks over; a climbable piece 0.9 m or
  taller is still a rock climbed with Climb_Up/Climb_Down (`ClimbLayer.MaxStepHeight`).
- **Height follow:** `BodyGround` samples the landscape and the climbable tops under the whole footprint (front, back,
  sides at 0.7 of the half width), lifts the body halfway to the highest point and pitches it by front minus back; height
  and tilt ease in over 0.18 s. Measured: onto a plateau 0 -> 0.55 m over about 2 m, no snap (the old per-cell
  `MoveTowards` jumped at the cell edge and the body half sank into the plateau beside it).

**Intro leftovers (round 12).**
- (a) Comic frame: `Tools/Blender/build_ui_comic.py` -> `UI_ComicFrame` (ink border with brush flicks, cyan inlay,
  corner tab with speed stripes, bottom ticks), cut as a 9-sliced sprite by `BlenderUiBuilder`; `BossCutIn` and the
  answer box wear it (without the art the cut-in falls back to the tinted panel).
- (b) Visor reflection: a second camera at the visor looks at the boss (192 px, 128 on Low) and `Game/UI/VisorReflection`
  lays it over the visor in the cut-in: barrel-warped, mirrored, tinted halfway to white from the boss colour, added, in a
  soft oval, brighter to the rim. The oval is placed by projecting the visor (head bone + 0.075 up + 0.1 forward, 0.26 x
  0.08 m) through the cut-in camera every frame. The face stays hidden.
- (c) Labels vs the bubble: `HudKeepOut.AddPriorityLabel` (the hero's speech bubble); while it shows, the other world
  labels (gate Rebuild, boss and base markers) keep off it like off the HUD, moving aside or hiding; the bubble itself
  keeps off the HUD only (`PlaceOverHud`).
- (d) Answer box: `BossAnswerBox`, a compact comic-framed box (600 x 196 canvas units, portrait, name, 26 px text) at the
  right edge where the cut-in was; it takes over when the cut-in slides out. The lower middle stays free.

**Gallery:** `CinderMatriarch/60-62` (crystal before, smash, remains), `Shattered/60-63` (onto and on top of a rise,
spire ahead and passed), `Intros/60-64` (comic cut-in, visor close-up, bubble vs Rebuild label, answer box). The rise and
spire shots are staged walks (the boss's own off-road walk sent past the piece).

**Known:** on narrow strips a climbing boss still vaults a group of tall boulders with its long Climb_Up/Climb_Down pair
(existing behaviour); it can no longer land across an obstacle.
