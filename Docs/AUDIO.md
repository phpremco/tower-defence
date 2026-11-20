# Audio: mixing rules and levels

How the game decides what is heard, how loud, and when something stays silent. The goal is a field that feels
alive without turning into noise: a crowd of thirty ants is one skittering bed, not thirty sounds.

## Pipeline

1. **Files** live in `Assets/_Project/Audio/Approved/<Category>/...` (Remco's recordings and syntheses) plus the
   CC0 packs (`Kenney_SciFi`, `Lentikula`, `Freesound`). Variants are numbered `name_01.wav`, loops end in `_loop`.
   The mp3 sources Remco supplied for the intro and dialogue sounds (round 42 onward) come from free sound sites
   such as Pixabay (free licence, no attribution required); check each file's page again before a commercial release.
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
  instead of starting another.
- **Beams hum as a choir (round 88a).** A beam tower makes no sound per pulse (a Laser fires 9 to 15 a second): each
  firing tower holds one of four voices of `laser_beam_loop.wav` (`Game.Core.BeamHum`, pure and tested; `SfxPlayer` sets
  the loops). The voices share the power of one beam (each at 1/sqrt(n) of its level, so four Lasers are as loud as one),
  a tower that keeps firing settles after 0.6 s over 2.5 s to 55 percent of its opening level and only opens at full
  level again after a silence, and the voices are tuned 3.5 percent apart, drift 1.2 percent over 3.7 s and start at
  different points of the loop, so equal recordings never stack into one hard tone. A fifth tower stays silent.
- **A blow lands on its picture (round 88a).** A power sound swells before it hits. The generator measures per sound
  set where the blow lies (`SoundSet._impact`, the first 10 ms window at 70 percent of the loudest; `SoundImpact`), and
  a sound tied to a moment on screen is started that much (plus the device's buffer delay) ahead of it, or part-way into
  its swell when the moment is too near (`SfxPlayer.PlayOnImpact`). Spencer's shockwave (0.19 s of swell, the wave
  leaves 0.1 s after the button) starts 0.09 s plus the buffer delay into the recording; a boss's pulse is on a timer, so
  its sound starts 0.35 to 0.52 s ahead (`CreatureChorus.AnnouncePulse`) and its blow lands with the ring.
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
| Tower break, hero pulse, base alert, howl, radar ping | 0.7 to 0.8 (-2 to -3 dB) | priority |
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
| Flak shot and burst (S4) | `ProjectilePool.Shot` and `Exploded` with `ProjectileStyle.Flak`: Kenney `impactMetal` at pitch 1.3 and `explosionCrunch` at pitch 1.25 (`FlakShotVolume` 0.55, `FlakBurstVolume` 0.35) |
| Stone stand-ins (S4) | `Voice_Cragback` (ice beetle walk and screech plus heavy steps, pitch 0.85), `Voice_Shalebat` (bat wings, screech and sand attack, pitch 0.85); `SoundSet._pitch` replays a borrowed clip slower; Remco's own stone sounds replace them in data |
| Rift ambience (S4) | `SfxLibrary.RiftLoops`: the lava-bed loop at pitch 0.8, one layer per level with rifts, scaled 2.3 like a pool |
| Mine and barrel | `PathObjectTriggered` |
| Net, slow field, cart, fireball | `PathObjectSpawned` by effect; loops follow the live path objects |
| Drone fan, repair, harvest, laser, cash | drone list: alive count, `Activity`, `Fired`, `Delivered` |
| Hero gun, saber, steps, abilities, death, fall | `Commander.Struck` (weapon kind), walking distance, `AbilityUsed` (the ability's own `SoundSet`, else the pulse for a shockwave and the repair chime), `Downed` |
| Creature loop, steps, roll | distance travelled per frame (`PathProgress`), `IsRolling` |
| Species loop presence | `CreatureVoice._presence` multiplies the loop level of one species (Wisp 1, Core Wisp 0.4 since round 64: their loop is static, and at 3 and 2 it read as a loud hiss; the Roller keeps 7): a hum may need more than legs |
| Creature attack and impact | `Enemy.Attacked`; impact scheduled after range / bolt speed |
| Creature death, boss growl | `EnemyKilled`, `EnemySpawned` (boss) |
| Boss powers | `BossAbilities` events per boss instance (pulse, volley, rally, regrow, spawn, laser) |
| Freeze | `Enemy.Slowed` (first slow only) |
| Structure hit, base alert | `StructureHit`, `BaseHealth.Changed` |
| Upgrade, scrap, cash | `TowerUpgraded`, `TowerScrapped`, `TowerSold` |
| Ambience layers | wind by theme (forest, blizzard); one loop per pool of water, bog or lava cells, sized and panned by the pool |
| Music | `SfxLibrary` menu/game/boss sets from `Approved/Music/*_loop.wav`; `MusicPlayer` crossfades game and boss, `MenuMusic` plays the menu loop; music keeps its mastered level (no measured gain) |
| Radar ping (round 88a) | `LevelContext.SurpriseSpotted` (the first creature out of a surprise spawn point or a nest behind the gate after 6 s of silence there, `SurpriseWatch`): `SfxLibrary.RadarPings` = `Approved/Intro/Colony/colony_radar_ping.wav`, level 0.8, priority, at most once a second |
| Boss approach | `LevelContext.ComingBoss` voice growls from the gate every 3 s until the boss shows; pools keep their generated churn; no growl means no heartbeat |
| Boss pulse, fission, brood | `_pulse` and `_spawn` of the boss voice (`enemy_power_pulse`, species screech for brood); generated bursts are fallbacks only |

Element bats use the attack of their element: Fire, Water, Wood = moss, Sand, Ice = frost, Bog = forest.

## Dialogue box (round 43)

Remco's approved files in `Approved/Dialogue/`, wired by `Tools/Generate/Dialogue Sounds` (also a step of the full generator):

- **Opener:** `open_static_squelch.wav` (walkie-talkie static) when a speaker starts talking: the first line in a fresh box
  and each change of speaker inside it. Lines of the same speaker that follow on in the box get none. **No close sound.**
- **Typing blips:** one soft blip per 2 revealed letters or digits (spaces, punctuation and redacted bars are silent), a random
  variant of the speaker's own set (`Typing/<voice>_01..06.wav`, on `SpeakerDefinition._typingBlips`: Mercer = spencer, Voss,
  Ora, Nti, Pioneer1), never the same twice in a row; a speaker without a set uses ORA's (the default set). At most one blip
  per frame, none when a confirm jumps the line to its end, none while paused (a paused dialogue does not reveal).
- Data: `Data/Audio/DialogueSounds.asset` (opener, volumes, letters per blip, default set), referenced by the sound library.
  Logic: `TypingCadence` (pure, tested); playback: `DialogueVoice`, used by `DialogueView` (cutscene, level briefings, Voss's
  scans and tips, the story viewer) and `BossAnswerBox` (boss intro answers). 2D, through the effects volume.
- The opening's gate shot no longer cues `capsule_radio_call_open` as Voss starts: the box's opener does that; the radio static
  loop still holds under his line.

## Wanted: opening cutscene music and sounds (Remco's search list, 2026-11-11)

The opening cutscene now borrows level sounds (see STATUS round 36, section 6). Tick an item off when an approved file
lands in `Approved/`; the curator then wires it into `Cutscene_Opening` by data.

### Music
- [ ] (open, by design: no intro music yet; the empty mood tracks in `CutsceneSounds_Opening` are the hook) Intro theme with its own arc: tense space calm, build-up during the drop and landing, threat when the brood
      appears, a short peak on "Defend the base". Ideally as stems or sections, so it can follow the shots.

### Orbit and mothership
- [x] Space ambience: a low, dark drone bed
- [x] Mothership engine hum (loop)
- [x] Cargo bay doors opening (hydraulics, metal)
- [x] Docking clamps releasing (clack, latch)

### Launch
- [x] Cold-gas puff at release (short hiss)
- [x] Booster ignition (whump)
- [x] Booster burn (loop, rumbling roar)
- [x] Stage separation (bang, metal, pyro bolts)
- [x] Booster sound fading into the distance

### Re-entry and landing
- [x] Re-entry plasma roar
- [x] Capsule rattling and creaking under load
- [x] Wind rush as it falls
- [x] Retro rockets firing (short bursts)
- [x] Heavy touchdown thud, metal creak, dust and gravel settling

### Capsule and Spencer
- [x] Pressure release (long hiss)
- [x] Hatch unlocking and swinging open
- [x] Footsteps on metal, then on rock and gravel
- [x] Breathing inside the helmet (calm, then fast)
- [x] Gasp, frightened panting
- [x] Pistol drawn from the holster (leather, click)
- [x] Pistol charging or arming
- [x] Radio beep and static (Voss calling in)

### Colony
- [x] Builder drones (hum, landing thump)
- [x] Drill starting up and running (loop)
- [x] Crystal hum (loop)
- [x] Radar ping and beacon beep

### Threat
- [x] Earthquake and deep ground rumble
- [x] Ground tearing open, burrows bursting
- [x] Eyes opening in the dark (wet, organic)
- [x] Skitter swarm chittering (loop)
- [x] Queen roar (big, alien)
- [x] Swarm retreating, chittering fading into the burrows

### Environment
- [x] Wind over the plain (loop)
- [x] Drips in the cave
- [x] Distant alien wildlife calls

Round 42 (2026-11-11): all 61 approved files in `Approved/Intro/` are wired into `Cutscene_Opening` through the sound bank
`Data/Story/Cutscene/CutsceneSounds_Opening.asset` (one slot per sound, filled by `Tools/Generate/Opening Cutscene Sounds`,
re-runnable; a slot without a file plays its fallback or stays silent). No slot is empty. Music stays empty on purpose: the
space drone bed carries the prologue until Remco picks a theme (put a clip in the bank's Wonder/Tension/Attack track).
`capsule_gasp_dry` is in the bank as an alternative take but not cued (the helmet gasp plays). See STATUS "Round 42".
