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

- **Entrance:** it does not come to you. **The map comes to it.** At the start of the boss wave the ground under the map cracks open, the road sinks and a new road rises that leads to the base from the centre of the map.
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
