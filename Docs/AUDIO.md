# Audio: mixing rules and levels

How the game decides what is heard, how loud, and when something stays silent. The goal is a field that feels
alive without turning into noise: a crowd of thirty ants is one skittering bed, not thirty sounds.

## Pipeline

1. **Files** live in `Assets/_Project/Audio/Approved/<Category>/...` (Remco's recordings and syntheses) plus the
   CC0 packs (`Kenney_SciFi`, `Lentikula`, `Freesound`). Variants are numbered `name_01.wav`, loops end in `_loop`.
2. **The generator** (`Tools/Generate/Scenes and Starter Data`, file `ProjectGenerator.Audio.cs`) fills
   `Data/Audio/SfxLibrary.asset` and one `Data/Audio/Voices/Voice_<Species>.asset` per creature. It measures the
   loudness of every file (90th percentile of 50 ms RMS windows) and stores a **gain per set** that brings the set
   to the shared target (RMS 0.1, about -20 dBFS). Playback code never compensates for how hot a file was mastered.
3. **`SfxPlayer`** (level sound hub) and **`CreatureChorus`** (every creature's voice) play sets through a pool of
   24 voices with a budget per kind. **`MusicPlayer`** adds the recorded ambience layers under the generated bed.
4. **Sound test**: main menu, Options, "Sound test" lists every set with its variant count and gain. Each button
   plays the next variant at the measured gain only; in the game each kind is mixed quieter than that.

## Rules (from common game-audio practice)

- **Hierarchy.** Player feedback and critical cues first, enemies second, ambience last. Priority sounds always
  play, never get dulled by zoom, and duck everything else for 0.6 s to 45 percent.
- **Voice budgets.** Shots 3, kills 2, hits 2, spawns 2, creature filler 4, crew (hero, drones) 3. When a budget is
  full the oldest voice of that kind is replaced. The ear cannot separate more anyway.
- **Bundle, do not stack.** The same shot within 60 ms (heavy) or 220 ms (rapid) nudges the playing voice louder
  instead of starting another. The laser tower zaps once per shot (the approved `laser.wav`), bundled the same way.
- **Crowds share a bed.** One movement loop per species. One creature plays it at half level; four fill it; more
  never get louder (square-root curve). Footsteps are rationed: at most one step of a species every 110 ms.
- **Distance.** Zoomed out, filler drops to 60 percent and loses everything above 2.6 kHz; priority sounds do not.
- **Variation.** Every set plays its variants in turn, every voice gets ±8 percent pitch jitter.
- **Moments, not moods.** Death cries are cut at 0.7 s. Base alarm at most once per 1.5 s. A slow applies its chill
  at most every 0.3 s even when a cryo tower hits a whole crowd.

## Levels

All values are multipliers on the loudness-normalised clip (0 dB = the measured target). Rough dB in brackets.

| Kind | Level | Note |
| --- | --- | --- |
| Gate or base explosion, boss death, hero death | 1.0 (0 dB) | priority |
| Boss pulse and fission | 1.2 to 1.8 (+2 to +5 dB) | priority; the recordings are dense and measure hot, so they need headroom above 0 dB |
| Tower break, hero pulse, base alert, howl | 0.7 to 0.8 (-2 to -3 dB) | priority |
| Mine or barrel, upgrade, scrap, net, build | 0.5 to 0.7 (-3 to -6 dB) | priority |
| Rocket or shell explosion, cash | 0.4 to 0.5 (-6 to -8 dB) | hit or crew |
| Hero blaster, saber swing and hit | 0.3 to 0.35 (-9 to -10 dB) | crew |
| Boss loop, boss steps, boss attacks | 0.4 to 0.55 (-5 to -8 dB) | the loudest creature |
| Creature death cry, creature attack | 0.3 to 0.45 (-7 to -10 dB) | |
| Structure hit, creature impact, freeze chill | 0.25 to 0.3 (-10 to -12 dB) | |
| Tower shots | 0.4 to 0.6 (-4 to -8 dB) | bundled volleys |
| Creature movement loop (crowd of four) | 0.24 (-12 dB) | one creature: -18 dB |
| Creature steps, fireball and cart loops, hero steps | 0.10 to 0.16 (-16 to -20 dB) | |
| Ambience layers (leaves, wind, pools) | 0.10 (-20 dB) | background only; a pool scales with its size and is panned to where it lies |
| Drone fan, welding, drilling, drone laser | 0.05 to 0.07 (-23 to -26 dB) | deliberately quiet |
| Saber hum | 0.05 (-26 dB) | presence only |

Enemies sit roughly 6 to 12 dB under the player's own actions, in line with what mixing guides suggest, but
their beds and steps stay capped, because many of them sound at once and the sum is what the player hears.
Environment loops exist only for things on screen: wind where there are leaves or snow, bubbling only over a
visible pool. They are background, never the loudest thing in a level.

## Where the sounds hook in

| Sound | Trigger |
| --- | --- |
| Explosions gate, base, tower break | `TowerWrecked` (gate or tower), `BaseHealth.Died` |
| Rocket vs cannon explosion | `ProjectilePool.Exploded` with the projectile style |
| Mine and barrel | `PathObjectTriggered` |
| Net, slow field, cart, fireball | `PathObjectSpawned` by effect; loops follow the live path objects |
| Drone fan, repair, harvest, laser, cash | drone list: alive count, `Activity`, `Fired`, `Delivered` |
| Hero gun, saber, steps, pulse, death, fall | `Commander.Struck` (weapon kind), walking distance, `ShockwaveReleased`, `Downed` |
| Creature loop, steps, roll | distance travelled per frame (`PathProgress`), `IsRolling` |
| Species loop presence | `CreatureVoice._presence` multiplies the loop level of one species (Wisp 2, Core Wisp 1.5): a hum needs more than legs |
| Creature attack and impact | `Enemy.Attacked`; impact scheduled after range / bolt speed |
| Creature death, boss growl | `EnemyKilled`, `EnemySpawned` (boss) |
| Boss powers | `BossAbilities` events per boss instance (pulse, volley, rally, regrow, spawn, laser) |
| Freeze | `Enemy.Slowed` (first slow only) |
| Structure hit, base alert | `StructureHit`, `BaseHealth.Changed` |
| Upgrade, scrap, cash | `TowerUpgraded`, `TowerScrapped`, `TowerSold` |
| Ambience layers | wind by theme (forest, blizzard); one loop per pool of water, bog or lava cells, sized and panned by the pool |
| Music | `SfxLibrary` menu/game/boss sets from `Approved/Music/*_loop.wav`; `MusicPlayer` crossfades game and boss, `MenuMusic` plays the menu loop; music keeps its mastered level (no measured gain) |
| Boss approach | `LevelContext.ComingBoss` voice growls from the gate every 3 s until the boss shows; pools keep their generated churn; no growl means no heartbeat |
| Boss pulse, fission, brood | `_pulse` and `_spawn` of the boss voice (`enemy_power_pulse`, species screech for brood); generated bursts are fallbacks only |

Element bats use the attack of their element: Fire, Water, Wood = moss, Sand, Ice = frost, Bog = forest.
