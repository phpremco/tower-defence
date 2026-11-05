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
