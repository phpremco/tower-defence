# Continent cutscenes: scripts

Status: writer draft for the curator, 2026-11-05. Remco: "bedenk eerst een script wat in het verhaal past en maak het
dan". This file is the script only; building comes later, after approval. Sources: `Docs/STORY.md` (the script wins),
`Docs/STORY_ROADMAP.md`, `Docs/BOSSES.md`, `Docs/specs/S2_CUTSCENES.md` (shot format), `ArtSource/README.md` (what
exists in Blender).

## 1. Where they play, how long, and the rules

### Placement: after the continent boss, not before level 1

Each cutscene plays **once, right after the continent boss is beaten in level 20** (the first win on any difficulty),
before the result screen. Then the normal flow continues: result screen, upgrade screen, map.

Why after the boss:
- **The strongest story beats are there.** In `STORY.md` every continent's key line is the "Level 20, einde"
  dialogue: "Te makkelijk ... Waarom?", "Een Warden. Een bewaker. Maar waarvan?", "Wíj vielen hén aan", "Dat was ooit
  een Burrower", the seal closing. Those lines need pictures; a text box after a boss kill undersells them.
- **The boss is the visual payoff.** The bosses are big, slow, glowing, fully rigged, and most have a Death clip that
  already tells a story (the Warden kneels, Rotbloom wilts shut, the Shattered explodes, the seal closes over the
  Sealed One). Their death belongs in a cinematic, not at play zoom.
- **Crystal Plains already has its "before" cutscene.** The opening cutscene is continent 1's arrival. With "after
  the boss", every continent gets exactly one new cutscene, including continent 1, and the pattern stays the same.
- **The bridge to the next continent fits at the end.** Each script ends with the planet (the veins light up, the
  STORY.md hint-B device) and the landing on the next continent. Its level 1 then opens with its existing LevelStart
  line ("Second continent, second base..."). The Core's cutscene is the finale and the epilogue.

Fallback while a continent is not built yet ("coming soon"): the arrival shot is skipped (no next level to build it
from) and the cutscene ends on the planet shot, with the next region lit.

### Length

- **About 45 to 55 s of picture** per cutscene on auto-advance (the `AutoLength` sum), **about 60 to 75 s** with the
  text boxes read at a normal pace. The opening is about 2 minutes; these are shorter on purpose: the player has just
  won and wants the reward.
- Text boxes wait for the player (S2 rule). Skippable at any time with the existing two-press skip; a skip goes to
  the result screen and still marks the cutscene seen.
- Replay: from the dev Story viewer and an Options/Codex "Replay" entry, like the opening (`ReturnScene`).

### Rules (for every shot below)

1. **Camera kicks only on impacts, once per shot at most, never sustained shake.** Strength 0.15 to 0.3 (the existing
   cap, `CutsceneCameraFeel.Calm`). Rumbles are sound, dust and light only.
2. **Enemies look alien and dangerous** (alien design rule): seen low and close, matte surfaces, warm glow, blades
   and maws readable. Never a cute or glossy beat, not even in the peaceful epilogue: calm is not cuddly.
3. **Only the existing cast:** Mercer, Dr. Voss, ORA, NTI (transmission frame and emblem), Pioneer-1 (probe, wreck,
   log). Plus Burrower species, bosses and our drones. No extra crew, no new named people.
4. **Human reactions are front-facing.** Whenever a shot is about a human's reaction, the camera is in front of the
   face: within 40 degrees of the face's forward direction, at eye height (plus or minus 0.3 m), looking at the head.
   Never over the reacting character's shoulder. For Mercer the "face" is the visor (the art rule says his face is
   never shown; the visor carries the expression with its glow and reflections), so the visor must fill the
   framing's upper third and reflect what he sees. For Voss it is her real face (the MPFB model, shape keys
   Smile/Frown).
5. **Dialogue in the established voices**, short, through the existing dialogue box with portraits. ORA: dry,
   factual, glitching from continent 2 on. Voss: curious, warm, doubting. Mercer: dry humour, few words, growing
   doubt. NTI: cold corporate. Lines marked **[STORY]** are faithful English translations of `STORY.md`; lines marked
   **[new]** are proposals for approval. Ids: `Dlg_Cut_C<n>_<shot>`.
6. **Light per continent palette** (round 17 moods), with the per-shot `ShotLighting` rig (moon key, rim, fill,
   exposure). Fades go to black, never white. A flash is a light, not a full-screen white.
7. **The field stays consistent** (BOSSES.md rule): a boss that falls does not fall through towers or decor; what it
   hits reacts.

### Shot format used below

Each shot names the fields of `CutsceneShot`: **Set** (`CutsceneSet`), **Action** (`CutsceneAction`, new values are
appended), **Timing** (`Fixed` with its seconds, or `AfterDialogue` with the dialogue delay plus the seconds after
it; "auto" is the picture length), **Camera** (keys with an `Anchor`, framing, FOV, the move), **On screen / action**,
**Light** (`ShotLighting`), **Sound** (`CutsceneCue` with a `CutsceneSound`; Shake = one kick), **Music**
(`CutsceneMusic`: Keep, Silence, Wonder, Tension, Attack), **Fades / letterbox / title card**, **Dialogue**.
New enum values, anchors and sets are collected in section 9.

---

## 2. Crystal Plains (continent 1): "The First Giant"

**Plays after:** Gravemaw, level C1_20. **Next:** Ashen Reach.

**Story purpose.** Continent 1's arc is "the Burrowers wake up; the base survives". The player learns: we won the
plains, NTI's machine starts again at once (the drill that Gravemaw smashed is put back up), and Voss already knows
the next continents will be different. A quiet seed for later: under the ground the eyes close and turn away, and a
pulse runs down a vein, away and downward. No one comments on it (hint B is only named in continent 3). The planet
shot lights the first vein line on the map.

Palette: night, cool blue-violet moonlight, pink-magenta Crysium; Gravemaw plum-bronze with magenta-pink glow.

**Shot 1. The giant falls.** Set `Field` (C1_20, dressing only) / `BossFalls` (new). Fixed 8 s. Fade in 0.8 s.
- Camera: anchor `BossFall` (new). Low and wide, 7 m off Gravemaw's left flank at 1.0 m, FOV 44, a slow push in to
  5 m, FOV 40. The head ends in the right third, the toppled colony drill rig behind it, left.
- On screen: Gravemaw half out of its last breach hole beside the road, crystal plates cracked. It rears for a weak
  `Roar` (jaws open, slower than in the fight), then plays `Death`: the body arches, drops and slides; the pink
  emission fades segment by segment from head to tail. Dust rolls towards the lens. The smashed drill rig (its story
  beat in the fight) lies on its side, sparks dripping from the cut cable.
- Light: exposure +0.9, moon key 0.9, warm rim from Gravemaw's own glow (fades with it), violet fill 0.3.
- Sound: Growl (weak) at 0.5 s, `BossFall` (new) at the impact, Shake 0.25 at the impact (Death frame about 40).
- Music: Attack until the impact, then Silence.

**Shot 2. Mercer lowers the gun.** Set `Field` / `HeroStandsDown` (new). Fixed 5 s.
- Camera: anchor `Gate`. Front-facing medium close on Mercer at his guard post: 2.2 m in front of him, 25 degrees to
  his right, eye height 1.4, FOV 34, a slow 0.4 m push. The dead boss is out of focus over his left shoulder.
- On screen: Mercer stands with the blaster up, then lowers it, a long breath (Idle, shoulders drop), and holsters it.
  The fading pink glow slides across his visor.
- Light: exposure +1.0, moon key 1.1, warm rim 1.0, `ClearView` on.
- Sound: HeroStep at 1.2 s, a holster click (new small `Holster` sound, or BuildClank at 0.2) at 3.6 s.
- Music: Silence.

**Shot 3. The drill goes back up.** Set `Field` / `RigRaised` (new). AfterDialogue, delay 4 s, then 3 s (auto 7 s).
- Camera: anchor `Drill`. Wide three-quarter view at 3 m height, FOV 46, a slow crane up to 5 m.
- On screen: four builder drones hover over the toppled derrick, cyan tethers taut, and pull it upright; it settles
  with a heavy clank. A fresh drill string sinks (`Cutscene_Drill` `Sink`), dust ring, the beacon light starts again.
  Hauler drones lift off from the pad behind.
- Light: exposure +0.8, cool rim 1.0, the drill beacon's own light.
- Sound: DroneFans at 0.3 s, BuildClank at 2.6 s, Rumble when the bit bites at 4.5 s (no kick).
- Music: Wonder, low.
- Dialogue: ORA: "Crystal Plains secured. Extraction at full capacity." **[STORY]**

**Shot 4. Five more.** Set `Field` / `ConsoleTalk` (new). AfterDialogue, delay 2 s, then 4 s (auto 6 s).
- Camera: anchor `Console` (new: the radar console beside the base). Key 1: front-facing chest-up on Voss at the
  console's `Operator` spot, 30 degrees to her left, FOV 32. Key 2 (0.6): a slow drift right until Mercer's visor
  (front three-quarter) is in frame as he walks up opposite her; Voss stays in focus.
- On screen: ORA's hologram turns above the console (`Ora` Idle, Talk on her line). Voss (Idle, Talk) looks up from
  the radar disc, then at Mercer; the Frown shape key eases on her second line. Mercer walks in (Walk) and stops.
- Light: exposure +0.9, the console screens as a cyan fill on Voss's face, moon key 0.8, cool rim.
- Sound: RadarPing (soft) at 0.5 s.
- Dialogue: Mercer: "There are five more continents, Doc." Voss: "I know. And over there, they're different." **[STORY]**

**Shot 5. Below, the eyes close.** Set `Underground` / `EyesWithdraw` (new). Fixed 7 s. Fade in 0.8 s.
- Camera: anchor `World`. The opening's cave, the same low corridor dolly, but backwards: the camera slowly drifts back
  out of the cave, FOV 50.
- On screen: most eye pairs are already shut. The few open pairs turn away from the camera, blink and close, one after
  another, deeper and deeper. Then one cyan-pink pulse runs along a vein in the floor, away from the camera and down
  into the dark, and is gone.
- Light: exposure +1.2, cave rim 0.7 (the opening's cave light).
- Sound: Rumble (faint) at 0.5 s, Growl (very low, distant) at 3 s.
- Music: Tension, low.

**Shot 6. The first vein.** Set `Survey` / `VeinsLight` (new). Fixed 8 s.
- Camera: anchor `World`. S-69-CRYS from high orbit, Crystal Plains at the top of the curve, a slow pull back and a
  quarter turn of the globe towards Ashen Reach.
- On screen: one thin pink-gold vein line lights up on the map from Crystal Plains and runs inwards, towards a point
  under the crust that is not yet shown. The planet turns until the smoke-brown Ashen Reach comes over the limb.
- Sound: RadarPing at 1 s and 4 s.
- Music: Wonder.

**Shot 7. Arrival: Ashen Reach.** Set `Arrival` (new) / `CapsulesLand` (new). Fixed 9 s. Fade out 1.2 s.
Title card: "Ashen Reach".
- Camera: anchor `Landing` (on the arrival field). Low wide shot across the ash plain, FOV 48; the capsules streak
  down through the smoke-brown sky; the camera tilts down to the touchdown, then a short push towards the hatch.
- On screen: three capsules with plasma trails, legs unfold (`Land`), the first one opens (`Open`), Mercer steps out
  at `Exit`, looks at the ember cracks at his feet, then at the horizon. Cinders drift.
- Light: Ashen Reach theme (smoke-brown night, ember orange from below), exposure +0.6, warm rim.
- Sound: Landing at 3.8 s, Shake 0.2 at the touchdown, HeroStep at 6.5 s.
- Music: Wonder, fades out under the title.

**Total:** about 50 s of picture, about 62 s with reading.

### Needs (Crystal Plains)

| Item | Status |
| --- | --- |
| Gravemaw model, `Roar`, `Death`, `Crawl` | Exists (`Exports/Gravemaw`, imported for the fight) |
| `Gravemaw_Hole` (breach hole) | Exists (`Exports/Env/Gravemaw_Hole`) |
| Colony drill rig, `Sink` and `Spin` | Exists (`Exports/Cutscene_Drill`) |
| **Toppled drill rig** (the smashed state, or a `Topple` clip on the drill) | **New** (also needed for the boss's in-game story beat) |
| Builder and hauler drones | Exist (`Drone_Builder`, `Drone_Hauler`, cutscene flight code in `FieldBase`) |
| Mercer: Idle, Walk, Run; pistol in `Holster_R` | Exist |
| **Mercer `Holster` clip** (pistol back into the holster, upper body, about 20 frames) | **New** (fallback: the pistol snaps to the socket off-screen) |
| Radar console with the `Operator` spot | Exists (`Exports/RadarConsole`), not yet placed in the cutscene field |
| ORA hologram (Idle, Talk, Glitch) | Exists (`Exports/Ora`) |
| Voss (MPFB, Idle, Talk, Blink, shape keys) | Exists (`Exports/Voss`); first time in 3D in a cutscene (bust rig: she stands, arms at her sides) |
| Underground cave kit and rigged eyes | Exist (`Exports/Underground`) |
| **Eyes turning away and closing in order; a pulse along `Cave_Vein`** | **New** behaviour (shader state exists: blink, look, glow) |
| **Vein lines on the planet map** (`Planet_S69_Veins`, one channel per continent, plus a seal-ring mask) | **New** texture |
| Capsules (`Land`, `Open`, `Exit`), plasma trail | Exist (`Cutscene_Capsule`, `Cutscene_FX`) |
| Ashen Reach environment and sky | Exist (`Exports/Env/AshenReach`, `Skies/Sky_AshenReach`) |
| Sounds `BossFall`, `Holster` | **New** |

---

## 3. Ashen Reach (continent 2): "Too Easy"

**Plays after:** the Cinder Matriarch, level C2_20. **Next:** Northern Crown.

**Story purpose.** Continent 2 is "confidence: we can handle this". The cutscene lets the player feel that confidence
and then undercuts it: the Burrowers do not flee in panic, they leave in order, all to the north, past the base,
ignoring it. ORA glitches on one word (hint A, which started in level 10). Voss asks the question that carries the
player into the hard continent: why north?

Palette: smoke-brown night, ember orange cracks, lava glow; the Matriarch molten orange under black crust.

**Shot 1. The queen of the lava sinks.** Set `Field` (C2_20) / `BossFalls`. Fixed 9 s. Fade in 0.6 s.
- Camera: anchor `BossFall`, set on the bank of the largest lava pool. Low across the pool's surface, 8 m away, FOV 46;
  a slow arc of 15 degrees to the left.
- On screen: the Matriarch, chimneys cracked, staggers to the bank of the pool she rose from (`Hunt_Stride`, slower),
  plays `Death` and slides back into the lava. Her chimneys go dark one by one (`Vent_1..3` emission off). In the
  nearby pools her last eggs (`CinderEgg`) dim and collapse (`Burst`: the cores go out, no larva). The lava's surface
  closes over her with a slow crust.
- Light: exposure +0.8, warm key from the lava below, moon rim 0.6.
- Sound: Growl (weak) at 1 s, `LavaHiss` (new) at the slide, Shake 0.2 at the splash (Death frame about 50).
- Music: Attack until the splash, then Silence.

**Shot 2. Easier than I thought.** Set `Field` / `HeroStandsDown`. AfterDialogue, delay 3 s, then 3 s (auto 6 s).
- Camera: anchor `BossFall`, now across the pool looking back at the bank: front-facing on Mercer as he walks up to
  the edge, 3 m away, eye height 1.4, FOV 32, `ClearView`.
- On screen: Mercer (Walk, then Idle) stops at the bank, holsters the pistol (`Holster`). The cooling lava turns from
  orange to black crust; steam rises between him and the lens; the last red glow sits in his visor.
- Light: exposure +0.9, warm under-light fading over the shot, cool rim.
- Sound: `LavaHiss` (soft loop), HeroStep at 1.5 s.
- Dialogue: Mercer: "This is going easier than I thought." **[STORY]**

**Shot 3. They leave.** Set `Field` / `SwarmRetreats` (new). Fixed 12 s.
- Camera: anchor `Entrance`. Starts low beside the road (as the opening's swarm shot), FOV 50; at 0.4 it cranes up to
  14 m and turns to follow the stream to the north edge of the map, FOV 44.
- On screen: from the burrows and the lava origins Burrowers pour out, but not towards the base. Slagstriders wade
  out of the lava, Siftjaws surface and dive again along the dunes, Skitters stream between them, Shalebats lift
  overhead. They pass the base at a distance without a look and run north in long lines, over the mesa ridge, into
  the haze. Nobody idles; the lines keep moving.
- Light: exposure +1.0, warm rim from the burrows, moon key 0.8.
- Sound: Emerge at 0.5 s and 2 s, CreatureChorus (distant) through the shot, Drums (slow, far) at 6 s.
- Music: Tension.

**Shot 4. Why north?** Set `Field` / `ConsoleTalk`. AfterDialogue, delay 1.5 s, then 3 s (auto 4.5 s).
Overlay: `RadarRetreat` (new).
- Camera: anchor `Console`. Front-facing chest-up on Voss, FOV 32, a slow push; ORA's hologram beside her.
- On screen: the radar shows a red mass that thins and slides off the top of the disc. ORA's hologram tears on one
  word (`Glitch` clip and the dialogue glitch effect). Voss watches the radar, then looks up, past the camera, north.
- Light: console cyan fill, moon key 0.8, warm rim.
- Sound: RadarPing at 0.4 s and 1.6 s.
- Dialogue: ORA: "Contacts withdrawing. Heading: north. Cause: un- ...unknown." **[new, glitch on "un-"]**
  Voss: "Too easy. They're pulling back to the north. Why?" **[STORY]**

**Shot 5. The second vein.** Set `Survey` / `VeinsLight`. Fixed 7 s.
- Camera: as continent 1's planet shot, from over Ashen Reach, turning north.
- On screen: a second vein line lights from Ashen Reach and runs inwards towards the same hidden point as the first.
  The globe turns north until the ice of Northern Crown fills the top of the frame.
- Sound: RadarPing at 1 s. Music: Tension, low.

**Shot 6. Arrival: Northern Crown.** Set `Arrival` / `CapsulesLand`. Fixed 9 s. Fade out 1.2 s.
Title card: "Northern Crown".
- Camera: anchor `Landing`. Wide and low in a snow squall, FOV 48; tilt down to the touchdown on blue-black slate.
- On screen: capsules through the squall, landing; Mercer steps out, frost creeping over the visor's edge. Far off on
  a basalt ridge, a line of dark shapes with amber glows stands still and watches (Shardbacks): they got here first.
- Light: Northern Crown theme (blue-white cold, amber heat in the cracks), exposure +0.6.
- Sound: Landing at 3.8 s, Shake 0.2 at the touchdown, wind (theme ambience), Growl (far) at 7 s.
- Music: Tension.

**Total:** about 48 s of picture, about 62 s with reading.

### Needs (Ashen Reach)

| Item | Status |
| --- | --- |
| Cinder Matriarch: `Death`, `Hunt_Stride`, `Idle`; vents by bone scale | Exists (`Exports/CinderMatriarch`, round 16 and 28 clips; round 28 not imported) |
| `CinderEgg` (`Idle`, `Burst`) | Exists (`Exports/CinderEgg`, not imported) |
| Lava pool and lava origin | Exist (`Origin2_Lava`, lava liquid and banks `Ash_Bank_*`) |
| **Lava crust closing over the boss** (a crust value in the liquid shader, local) | **New** |
| Slagstrider, Siftjaw, Skitter, Shalebat (run, fly, Siftjaw dig) | Exist |
| **Swarm route that leaves the map instead of attacking** (a retreat path for `FieldSwarm`) | **New** behaviour |
| **Radar overlay mode "retreat"** (contacts sliding off the disc) | **New** (`RadarOverlay` variant) |
| ORA `Glitch` clip, dialogue glitch effect | Exist |
| Mercer `Holster` | New (see continent 1) |
| Shardback (watching on the ridge) | Exists |
| Northern Crown environment and sky | Exist (`Env/NorthernCrown`, `Sky_NorthernCrown`) |
| Sound `LavaHiss` | **New** |

---

## 4. Northern Crown (continent 3): "The Warden"

**Plays after:** the Frost Warden, level C3_20. **Next:** Mirewood.

**Story purpose.** Continent 3 is the hard one and the home of hint B. The cutscene makes the Warden's last act say
what its name says: dying, it does not lunge at the base; it turns back to the frozen lake it came from and plants
its glaives there, as if guarding the spot. The ice cracks in a ring of glyphs, the same glyphs that glow on its own
armour, and under the ice the amber veins all run down. Voss and Mercer say the STORY.md lines. Far below, out of
focus, a gold ring: the first glimpse of what the veins guard (nobody names it yet).

Palette: hard dark blue-white cold, amber-gold heat under the ice; the Warden's seal glyphs amber-gold.

**Shot 1. The Warden turns back.** Set `Field` (C3_20) / `BossFalls`. Fixed 10 s. Fade in 0.6 s.
- Camera: anchor `BossFall`, placed at the edge of the frozen lake behind the map. Low over the ice, 9 m away, FOV 44,
  a slow dolly alongside it.
- On screen: the Warden, armour gone (exposed phase, `WalkEnraged` hunch), stops on the road, turns its back to the
  base and walks back, slowly, to the cracked lake edge. It plays `Death` there as a kneel: both glaives driven into
  the ice. Its seal glyphs flare amber-gold once, then go out. The icicles on its back crack off.
- Light: exposure +0.8, moon key 0.9, amber rim from its glyphs (flare, then off), blue fill.
- Sound: Growl (low) at 1 s, `IceBreak` (new) at the glaive impact, Shake 0.2 at the impact.
- Music: Attack, then Silence after the flare.

**Shot 2. The ring in the ice.** Set `Field` / `GlyphRing` (new). Fixed 7 s.
- Camera: anchor `BossFall`. Starts close on the glaive tips in the ice, FOV 40, then cranes straight up to 9 m,
  looking down, FOV 46.
- On screen: from the glaive tips the cracks do not spread at random: they run in a ring around the kneeling Warden
  and draw the glyphs of its own armour in the ice. Through the ice, amber veins converge under the ring and run
  straight down, out of sight.
- Light: exposure +1.0, amber light from under the ice, cool moon key.
- Sound: `IceBreak` (soft, travelling) through the shot, `CrystalChime` (new) as the ring closes at 5 s.
- Music: Silence, then Wonder (low, uneasy) from 5 s.

**Shot 3. A guardian of what?** Set `Field` / `HeroKneels` (new). AfterDialogue, delay 2 s, then 4 s (auto 6 s).
- Camera: anchor `BossFall`. Low across the ice, front-facing on Mercer: 2 m in front of him at kneeling eye height
  (0.95 m), FOV 30, `ClearView`. The kneeling Warden behind him, out of focus.
- On screen: Mercer kneels at the ring (`Repair` kneel pose, no beam), his glove hovering over a glyph. The amber glow
  under-lights his visor; the glyph pattern reflects in it.
- Light: exposure +1.0, amber under-light, moon key 0.7.
- Dialogue: Voss: "He didn't fight to win, Mercer. He was standing for something. As if he was guarding something."
  Mercer: "A Warden. A guardian. But of what?" **[STORY]**

**Shot 4. Down the veins.** Set `Underground` / `VeinDive` (new). Fixed 9 s. Fade in 0.6 s.
- Camera: anchor `World`. A slow vertical dive: through the ice sheet, into dark rock, following the amber veins down,
  FOV 55 to 48.
- On screen: the veins join others coming in from the sides, all heading down. At the end, far below in the dark and
  out of focus, a great gold ring of light, just a shape. Cut to black before it resolves.
- Light: exposure +1.2, amber vein light, faint gold far below; Underground theme with an ice tint.
- Sound: Rumble (deep, slow), a low `SealHum` (new) fading in at the end.
- Music: Tension. Fade out 0.8 s.

**Shot 5. The third vein.** Set `Survey` / `VeinsLight`. Fixed 7 s.
- On screen: the third vein lights. Now three lines clearly meet at one point under the crust. The globe turns to the
  fog-covered Mirewood.
- Sound: RadarPing at 1 s. Music: Tension, low.

**Shot 6. Arrival: Mirewood.** Set `Arrival` / `CapsulesLand`. Fixed 9 s. Fade out 1.2 s. Title card: "Mirewood".
- Camera: anchor `Landing`. Inside the fog bank, FOV 50; the capsules come through it as glowing smudges, then a tilt
  down to the causeway.
- On screen: landing beside black water; reeds with lantern seed heads, a fog vent breathing. Mercer steps out, ankle
  deep. Far off in the bog, one cold glint of metal catches his lamp and is gone (no comment).
- Light: Mirewood theme (teal bioluminescence, violet fungus), exposure +0.7.
- Sound: Landing at 4 s, Shake 0.15 at the touchdown, bog ambience (bubbles, insects).
- Music: Wonder, low.

**Total:** about 48 s of picture, about 63 s with reading.

### Needs (Northern Crown)

| Item | Status |
| --- | --- |
| Frost Warden: `WalkEnraged`, `Death`, icicles by scale, seal glyphs in the Emission map | Exists (`Exports/FrostWarden`, round 16 and 28) |
| **Warden `Death` as a kneel facing away, glaives planted** (the round-16 Death is 84 frames; check it reads as a kneel, else a `Death_Kneel` variant) | **Check / maybe new clip** |
| Frozen lake, ice origin, banks | Exist (`Origin2_Ice`, `North_Bank_*`) |
| **Glyph ring cracks in the ice** (a decal or crack-strip texture with the seal glyphs, drawn on over time) | **New** (glyph shapes exist in `titan_kit.seal_rune`; new `FX_GlyphRing` texture) |
| Mercer kneel (`Repair` without the beam) | Exists |
| **Underground set with an ice theme and a vertical vein dive; a far gold ring** (reuse `Core_Seal_Intact`, far and blurred) | **New** shot behaviour and theme; the seal model exists (`Env/TheCore`) |
| Mirewood environment, `FogVent`, `Reed`, sky | Exist |
| Sounds `IceBreak`, `CrystalChime`, `SealHum` | **New** |

---

## 5. Mirewood (continent 4): "We Struck First"

**Plays after:** Rotbloom, level C4_20, fought at the Pioneer-1 wreck. **Next:** Southern Crown.

**Story purpose.** Continent 4 is twist A: NTI knew. The fight was at the wreck. In the cutscene the wreck is freed
from Rotbloom's roots and Mercer stands at it to say the line that turns the whole war around: we attacked them.
Voss adds the second half (Pioneer-1 drilled too, and after the drilling the attack began), and the camera shows the
old bore hole, so the player connects drilling and waking. The planet shot then shows the fourth vein and, for the
first time, a faint ring where they meet. As a bridge to continent 5, heavy NTI cargo pods leave the mothership:
the superweapons are on their way.

Palette: dark, wet, foggy; teal bioluminescence, violet fungus light; Rotbloom's bloom wine-red.

**Shot 1. Rotbloom wilts.** Set `Field` (C4_20) / `BossFalls`. Fixed 9 s. Fade in 0.6 s.
- Camera: anchor `Wreck` (new: the Pioneer-1 wreck on the arena). Low at the bog's edge, 8 m away, FOV 46, a slow push.
- On screen: Rotbloom (uprooted) drags itself to the wreck it grew around and plays `Death`: the bloom wilts shut,
  the petals blacken. The root mass over the hull (`RotbloomRoots` stage 3) withers and slackens, roots slide off the
  metal into the water. The NTI logo on the hull comes free.
- Light: exposure +0.9, teal fill from the bog, violet rim from the fungi, the wine-red bloom glow fading.
- Sound: Growl (wet, dying) at 1 s, `BogBubble` (new) at the collapse. No kick (a soft fall into mud).
- Music: Attack, then Silence.

**Shot 2. The status light.** Set `Field` / `HeroAtWreck` (new). AfterDialogue, delay 5 s, then 3 s (auto 8 s).
- Camera: anchor `Wreck`. Front-facing on Mercer as he walks up to the hull: starts 5 m in front of him, he walks into
  a medium close (2 m, eye height 1.4, FOV 32), `ClearView`. The wreck's hull fills the right half.
- On screen: Mercer (Walk, then Idle) stops at the hull beside the `Log` socket. The wreck's status light still
  blinks red at `StatusLight`; it blinks in his visor.
- Light: exposure +1.0, the red status light as a small key on his visor, teal fill.
- Sound: HeroStep (wet) through the walk, a faint electronic tick on every blink.
- Dialogue: Mercer: "So they're not attacking us. We attacked them." **[STORY]**

**Shot 3. The bore hole.** Set `Field` / `BoreHole` (new). AfterDialogue, delay 2 s, then 4 s (auto 6 s).
- Camera: anchor `Wreck`. Over the wreck's tail, then a tilt down, FOV 40.
- On screen: behind the wreck, half under the moss, a capped bore hole with a broken drill string sticking out, the
  ground around it cracked in the same pattern as our drill's cracks. The Crysium shards around the hole are dull and
  split.
- Light: exposure +1.0, cold moon key, faint pink at the bottom of the hole.
- Dialogue: Voss: "And there's something else. Pioneer-1 drilled too. After the drilling, the attack began." **[STORY]**

**Shot 4. The light goes out.** Set `Field` / `HeroAtWreck`. Fixed 4 s.
- Camera: anchor `Wreck`. Close on the status light, Mercer's glove in the foreground right.
- On screen: Mercer pulls the log module from the `Log` socket. The status light blinks twice more and goes out after
  six years.
- Sound: a soft click, then silence. Music: Silence.

**Shot 5. The fourth vein, and the pods.** Set `Survey` / `VeinsLight`, then `Orbit` / `CargoDrop` (new).
Fixed 10 s (the set changes at 5 s through a short fade).
- Camera: first the planet as before; then the mothership in low orbit, FOV 46.
- On screen: the fourth vein lights; where the four meet, a faint ring of light shows on the map for the first time.
  Cut to the mothership: its bay opens and three heavy cargo pods with the NTI emblem drop towards the south, burning.
- Sound: RadarPing at 1 s; re-entry whoosh on the pods.
- Music: Tension.

**Shot 6. Arrival: Southern Crown.** Set `Arrival` / `CapsulesLand`. Fixed 9 s. Fade out 1.2 s.
Title card: "Southern Crown".
- Camera: anchor `Landing`. Low over cracked pack ice, FOV 48.
- On screen: the capsules land, then the three cargo pods slam down further off. With each impact the magenta veins in
  the ice cracks pulse brighter, a wave running away from the impact (the network feels every blow). Mercer steps out
  and looks at the pods, not at the plain.
- Light: Southern Crown theme (pale ice, dark rock, hot magenta-white veins), exposure +0.6.
- Sound: Landing at 3.5 s, Shake 0.2 at the first pod impact only, Earthquake (soft) after each pod.
- Music: Tension.

**Total:** about 46 s of picture, about 60 s with reading.

### Needs (Mirewood)

| Item | Status |
| --- | --- |
| Rotbloom: `Death` (bloom wilts shut), `Crawl` | Exists (`Exports/Rotbloom`) |
| `RotbloomRoots` stage 3 | Exists (`Exports/RotbloomRoots`, not imported) |
| **Roots withering and sliding off the wreck** (a wither value on the root material, or a scale-down per root) | **New** |
| Pioneer-1 wreck with `StatusLight`, `Smoke`, `Log` | Exists (`Exports/Pioneer1_Wreck`) |
| **Log module prop** (a small pull-out cartridge for the `Log` socket) | **New** (small) |
| **Pioneer-1 bore hole with a broken drill string stub** | **New** prop (crack texture `FX_DrillCracks` exists) |
| Mothership with `Bay` | Exists (`Cutscene_Mothership`) |
| **NTI cargo pod** (heavy, emblem on the side) | **New**, or reuse `Cutscene_Capsule` with the `NTI_Emblem` decal and a bigger scale |
| **Vein pulse wave on impacts** (shader value travelling along `South_Decor_CrackedVein` and the ground veins) | **New** behaviour |
| Southern Crown environment and sky | Exist |
| Sound `BogBubble`, wet steps | **New** (`BogBubble`); wet steps from the bog ambience |

---

## 6. Southern Crown (continent 5): "What We Made"

**Plays after:** The Shattered, level C5_20. **Next:** The Core.

**Story purpose.** Continent 5 is "NTI's superweapons crack the veins further". The cutscene shows the price: when the
Shattered breaks for the last time, what lies in the crater is a single, ordinary-sized Burrower, burnt out by
overcharged Crysium. Voss says it plainly. Then the ground splits along the veins, ORA reports the energy in The Core,
and the camera falls through the crack to the first clear look at the cracked seal with violet light leaking through,
something vast moving under it. Nobody calls it a seal yet (Voss decodes it in The Core, level 5).

Palette: pale shattered ice over dark rock, overcharged Crysium hot magenta to white.

**Shot 1. The last break.** Set `Field` (C5_20) / `BossFalls`. Fixed 9 s. Fade in 0.6 s.
- Camera: anchor `BossFall`. Low and wide, 10 m away (it is big and about to explode), FOV 46, still.
- On screen: the rejoined giant (phase-3 look) staggers; `Death`: its crystals swell and glow white, light leaks from
  every crack, then the final burst (Death frame 46). Shards rain down on the ice, ringing; the crystals are gone by
  frame 54. Dust and glittering haze settle.
- Light: exposure +0.8, then a burst point light (not a screen flash), magenta rim, cold key.
- Sound: `CrystalBurst` (new) at the burst, Shake 0.3 at the burst, shard rain (`CrystalChime`, scattered).
- Music: Attack until the burst, then Silence.

**Shot 2. What it was.** Set `Field` / `HeroKneels`. AfterDialogue, delay 3 s, then 4 s (auto 7 s).
- Camera: anchor `BossFall`. In the crater, low, front-facing on Mercer as he kneels: 1.8 m in front, kneeling eye
  height, FOV 30, `ClearView`. Between him and the lens, in the soft foreground, the body.
- On screen: in the centre of the crater lies one Burrower of normal size, curled up, dark, the last crystal growths
  on its back dimming and falling off like dead scales. Mercer kneels beside it (`Repair` kneel), does not touch it.
- Light: exposure +1.0, a dying magenta glow from the body under-lighting his visor, cold key.
- Dialogue: Voss: "That used to be a Burrower. Our weapons turned it into this." **[STORY]**

**Shot 3. The ground opens.** Set `Field` / `GroundSplits` (new). AfterDialogue, delay 3 s, then 4 s (auto 7 s).
- Camera: anchor `BossFall`. Medium wide at 4 m height, FOV 44, pulling back as the crack runs.
- On screen: from the crater a crack races across the field along a vein, the lips heave up, white-magenta light
  floods out. Mercer stands and steps back from the edge (the opening's reaction motion, front-facing), hand up
  against the light.
- Light: exposure +0.9, strong magenta-white from below as the key, cool rim.
- Sound: Earthquake at 0.5 s, Shake 0.2 at the split, Rumble after.
- Music: Tension.
- Dialogue: ORA: "Warning: fracture in the ground. Unknown energy source in The Core. That energy... is growing."
  **[STORY]**

**Shot 4. Down the crack.** Set `Underground` / `VeinDive`. Fixed 10 s. Fade in 0.4 s.
- Camera: anchor `World`. Falls through the crack, past layers of rock and crystal, FOV 55; at 0.6 it levels out
  over a vast dark cavern, FOV 50.
- On screen: far below, the great seal: a gold ring 7 m across (seen huge, from above), cracked, sick violet light
  leaking through the cracks. Under it, three faint rings of violet light slowly turn and something vast shifts, a
  shadow only.
- Light: Core palette: indigo-black, gold-white seal light, violet leaks; exposure +1.2.
- Sound: `SealHum` rising, a slow deep pulse like a heartbeat.
- Music: Tension.

**Shot 5. All five.** Set `Survey` / `VeinsLight`. Fixed 6 s.
- On screen: the fifth vein lights. All five meet at the ring on the map, and the ring now shows violet cracks.
- Sound: RadarPing, then the `SealHum` under it.

**Shot 6. Descent: The Core.** Set `Arrival` / `BaseLowered` (new). Fixed 9 s. Fade out 1.4 s. Title card: "The Core".
- Camera: anchor `Landing`. Looking up from the crystal plateau at the mouth of the crack far above, FOV 50, then a
  tilt down to the plateau.
- On screen: builder drones lower base modules on cyan tethers down through the crack, one after the other, onto a
  pale lilac crystal plateau at the edge of the abyss. Mercer rides down on the last module, steps off and looks into
  the dark.
- Light: The Core theme (lit from below by the seal), exposure +0.7.
- Sound: DroneFans, BuildClank as modules touch down.
- Music: Tension, low.

**Total:** about 48 s of picture, about 62 s with reading.

### Needs (Southern Crown)

| Item | Status |
| --- | --- |
| The Shattered: phase-3 look, `Death` (burst 46, gone 54) | Exists (`Exports/Shattered`, round 28 improvement pass, not imported) |
| **The burnt-out Burrower in the crater** (an existing species in its `Death` end pose with a "burnt out" skin: dark, cracked, a few dimming crystal growths) | **New skin** on an existing model (species: see open questions) |
| Mercer kneel, react step-back | Exist (`Repair` pose; `CutsceneHeroRig.React`) |
| `South_Decor_CrackedVein`, crack strips | Exist (`Env/SouthernCrown`, `Cutscene_FX`) |
| **A long crack that runs across the field and opens** (crack strip + heaved lips, light from below) | **New** set behaviour (textures exist) |
| Seal `Core_Seal_Cracked`, `Core_SealVein_*` | Exist (`Env/TheCore`) |
| The Sealed One's rings glowing under the seal (only the rings) | Exists (`SealedOne` `Submerged_Loop`: rings glow through) |
| Core environment, plateau, sky | Exist |
| Builder drones with tethers (`FX_BeamSoft`) | Exist |
| Sounds `CrystalBurst`, heartbeat pulse | **New** |

---

## 7. The Core (continent 6): "Neighbours" (finale and epilogue)

**Plays after:** The Sealed One, level C6_20. **Next:** none; it ends on the planet, then the credits or the map.

**Story purpose.** Everything is resolved. The ring of overcharged crystals (built in levels 15 to 19) fires inwards,
the Sealed One releases, sinks and the seal stones close over it: the weapon that caused the damage repairs it. The
Burrowers who fought beside us stand around the seal; one of them comes to Mercer, looks at him and goes into the
ground: a thank-you. The epilogue: the logs reached Earth, NTI lost the mission, mining continues only at the surface,
far from the veins, in agreement with the Burrowers. Voss's closing thought mirrors the prologue's empty Earth, and
ORA's last line mirrors her first one ("No life forms detected").

Palette: indigo-black crystal glass, gold-white seal light, sick violet corruption; the epilogue at first light.

**Shot 1. The ring fires.** Set `Field` (C6_20) / `SealRingFires` (new). Fixed 10 s. Fade in 0.6 s.
- Camera: anchor `Seal` (new: the seal's centre). High and wide over the plateau, 16 m up, FOV 50, a slow descending
  orbit of 30 degrees.
- On screen: the Sealed One in its last phase plays `Release`: the rings unfold and slow, the plates open wide, it
  kneels, its light turns white (Release emission). Around it, the overcharged crystals in their ring flare one after
  another, and their beams all bend inwards and meet over the seal. The corrupted creatures on the plateau stop and
  crumble into violet dust.
- Light: exposure +0.8, gold-white from the seal, the white of the release growing, violet fading out.
- Sound: `SealHum` at full, then each crystal's charge (BuildClank pitched up, or a new `CrystalCharge`), Shake 0.25
  when the beams meet.
- Music: Attack, then Silence when the beams meet.

**Shot 2. The seal closes.** Set `Field` / `SealCloses` (new). AfterDialogue, delay 5 s, then 2 s (auto 7 s).
- Camera: anchor `Seal`. Medium, at the rim of the seal, 6 m from the centre, FOV 42, still.
- On screen: `Death`: the plates close, the rings shrink, it sinks; the seven seal stones close over the fissure
  (Death frame 128). The cracked seal is swapped for the intact one; the gold glyphs light up, steady and calm.
- Light: violet out, gold-white in; exposure +0.9.
- Sound: Rumble as it sinks, `SealHum` falling to a soft steady tone, then near silence.
- Dialogue: ORA: "Seal restored. Energy flow stopped." **[STORY]**

**Shot 3. A thank-you.** Set `Field` / `BurrowerFarewell` (new). AfterDialogue, delay 7 s, then 3 s (auto 10 s).
- Camera: anchor `Seal`. Key 1: low behind the Burrowers standing round the seal, FOV 46. Key 2 (0.35): reverse,
  front-facing on Mercer over the Queen's shoulder edge (her silhouette soft at the frame's edge, Mercer's visor
  clear and in focus), FOV 32. Key 3 (0.7): side two-shot as she burrows.
- On screen: Burrowers of every continent stand around the seal, still, their glows calm. The Queen (the screamer of
  the opening cutscene) walks out of them to Mercer and stops close, towering over him; her eye clusters look down at
  him, her head lowers. Mercer does not raise his gun. She turns and digs into the ground beside the seal and is gone;
  the others follow, one by one, into the ground. The plateau is empty and quiet.
- Light: gold-white seal light as the key from below, cool rim, exposure +1.0, `ClearView`.
- Sound: Growl (soft, low, not hostile) at 3 s, Emerge (reversed in feeling: a dig) at 6 s.
- Music: Wonder, low.
- Dialogue: Mercer: "Pretty sure that was a thank-you." **[STORY]**

**Shot 4. Epilogue: Earth.** Set `Earth` / `TransmissionArrives` (new). AfterDialogue, delay 1.5 s, then 4 s
(auto 5.5 s). Fade in 1 s.
- Camera: as the prologue's Earth shot, slower.
- On screen: the grey-brown Earth. A thin cyan transmission line arrives from deep space and touches it. Over it, the
  NTI transmission frame with the emblem flickers and goes dark for good.
- Dialogue: ORA: "The logs reached Earth. Nova Terra Industries lost the mission. A new team took over." **[STORY
  epilogue narration, given to ORA as in the prologue]**

**Shot 5. Epilogue: the surface.** Set `Field` (C1_01, the first landing site) / `PeacefulMining` (new).
AfterDialogue, delay 2 s, then 5 s (auto 7 s).
- Camera: anchor `Console`. Key 1: wide over the base at first light, FOV 46. Key 2 (0.5): front-facing chest-up on
  Voss at the console, FOV 32.
- On screen: the base on Crystal Plains in the first daylight the game has shown. The big drill is capped and quiet;
  haulers mine only small surface nodes at the edge of the field, far from the vein lines. At the far edge of the
  field, a few Skitters and a Cragback cross unhurried, glows low; nobody aims at anybody. Voss at the console watches
  them, the Smile shape key easing in at the end of her line.
- Light: a new dawn variant of the Crystal Plains theme: low warm sun, cool shadows, exposure +0.4.
- Sound: DroneHarvest (soft), wind, a distant creature call.
- Music: Wonder.
- Dialogue: ORA: "Mining continues at the surface only. Far from the veins, and by agreement with the Burrowers."
  **[STORY epilogue narration]** Voss: "Earth ran empty because nobody looked after it. Here, we do it differently."
  **[STORY]**

**Shot 6. Status.** Set `Survey` / `VeinsLight` (all lit, seal intact). AfterDialogue, delay 2 s, then 4 s (auto 6 s).
Fade out 2 s.
- Camera: S-69-CRYS from high orbit, a slow pull back until the mothership's running lights pass in front.
- On screen: all veins glow softly; the ring where they meet is whole and gold. Night side: a few small colony lights.
- Sound: RadarPing, once.
- Music: Wonder, resolving.
- Dialogue: ORA: "All systems nominal. Life forms detected: many. Status: neighbours." **[STORY]**

**Total:** about 46 s of picture, about 72 s with reading (the finale may read longer; it is the last scene).

### Needs (The Core)

| Item | Status |
| --- | --- |
| The Sealed One: `Release`, `Death` (seal stones close at 128), phase and release emission maps | Exists (`Exports/SealedOne`, round 16 and 28, not imported) |
| `Core_Seal_Broken` / `_Cracked` / `_Intact` swap | Exists (`Env/TheCore`) |
| Overcharged crystal | Exists (`Exports/Super_OverchargedCrystal`) |
| **Converging beams from the crystal ring** (beam sprites exist in `Cutscene_FX`; the inward bend is new) | **New** effect |
| Corrupted skins | Exist (`Corrupted_skins_report.json`, 27 species) |
| **Corrupted creatures crumbling to dust** (a dissolve on the corrupted skin) | **New** effect |
| Queen (the opening's screamer) | Exists (`Exports/Queen`) |
| **Queen `Burrow` clip** (lowers her head, digs in, sinks; about 90 frames) and a dig clip for 2-3 common species, or one shared dig effect that hides them | **New** clips |
| Earth set and the NTI transmission frame | Exist (`EarthSet`, speaker frame `Transmission`) |
| **Transmission line from space** | **New** effect (`FX_BeamSoft` texture exists) |
| **Dawn variant of the Crystal Plains theme** (and a dawn sky) | **New** data (theme) and a sky (`Sky_CrystalPlains` is night) |
| Capped drill, small surface nodes (`ResourceNode`) | Drill exists; **capped state new** (a cap plate); nodes exist |
| Voss at the console with Smile | Exists (shape keys) |
| Sounds `CrystalCharge` (or pitched clank), soft creature call | **New** (optional) |

---

## 8. Fix note: the opening cutscene's Mercer reaction shot

**What is wrong.** Shot `MercerReacts` (5.5 s, after the swarm) shows Mercer's back. The generator's keys are written
"in front of him, a little to his right" relative to anchor `Commander`, whose facing is `FieldLayout.ReactFacing`
(the dig-site facing turned 150 degrees from the gate), and the rig then turns his head and torso back towards the
swarm. Two likely causes, to be measured, not guessed:
1. **Model forward vs anchor forward.** Every Blender FBX so far arrived turned 180 degrees about Y in Unity
   (creatures, round 26 props). If Mercer's posed head faces the anchor's -Z, "in front" by the anchor is behind him.
2. **The pose turns away from the camera.** He faces away from the gate and looks back over his shoulder at the swarm;
   with the camera on the anchor's front and the swarm behind him, any extra head or torso turn shows the helmet's
   back.

**Fix (data plus one check):**
- **Frame him facing the swarm, not facing away.** The natural reaction is to stare at what is coming. Turn
  `ReactFacing` to face the gate and the swarm (he steps back, away from them, still looking at them; head turn 0 to
  10 degrees, no look over the shoulder).
- **Put the camera between him and the swarm, slightly off-axis:** 2.6 m in front of his visor, 25 to 30 degrees to
  his right, at eye height 1.4 (look-at the head bone, y 1.45), FOV 32 to 30, a slow 0.5 m push. The swarm is then
  behind the camera: show it in the visor (reflection of the burrows' warm light and the running shapes), in the warm
  rim on his suit, and in the sound. Optional third key (0.75): orbit to a front three-quarter profile so the gate and
  the crowd come into soft focus over his shoulder, while the visor still faces the lens within 40 degrees.
- **Write the keys from the face, not the root.** Add an anchor (`CommanderFace`) resolved from the posed `Head` bone
  (position and its real forward after the import correction), so a model or import change cannot flip the shot.
- **Guard it with a test.** In `CutsceneDataTests` (or the screenshot test): at progress 0.25, 0.5 and 0.75, the angle
  between Mercer's visor forward and the direction from his head to the camera is under 40 degrees. The same check
  applies to every human reaction shot in the continent cutscenes (section 1, rule 4).
- **Light:** keep the cool key from the camera side so the visor reads, warm rim from the burrows behind him. The
  current still is dark with a rock in the foreground: keep `ClearView` on.

---

## 9. Shared building blocks (for the later spec)

Collected from the scripts; the builder decides the final shape.

- **Trigger and flow:** a `CutsceneDefinition` per continent (`Cutscene_C1_Finale` ... `Cutscene_C6_Finale`), named
  on the continent or on its boss level and played on the first `LevelWon` of that level, once per save
  (`SeenStoryIds`). New `CutsceneHandoff` value (appended): `ContinueFlow` (back into the post-level flow: result
  screen).
- **Sets (appended to `CutsceneSet`):** `Arrival` (a second field built from another `LevelDefinition`, dressing only).
  `Field` builds the boss level (`CutsceneCast.FieldLevel`), the epilogue's shot 5 builds C1_01: the cast needs a
  per-shot or second field level (`ArrivalLevel`, `EpilogueLevel`). `ArrivalLevel` null skips the arrival shot.
- **Actions (appended to `CutsceneAction`):** `BossFalls`, `HeroStandsDown`, `RigRaised`, `ConsoleTalk`,
  `EyesWithdraw`, `VeinsLight`, `CapsulesLand`, `SwarmRetreats`, `GlyphRing`, `HeroKneels`, `VeinDive`,
  `HeroAtWreck`, `BoreHole`, `CargoDrop`, `GroundSplits`, `BaseLowered`, `SealRingFires`, `SealCloses`,
  `BurrowerFarewell`, `TransmissionArrives`, `PeacefulMining`.
- **Anchors (appended to `CameraAnchor`):** `BossFall` (where the boss dies; from level data, for example its last
  breach, pool or the spot in front of the last gate it reached), `Console`, `Wreck`, `Seal`, `CommanderFace`.
- **Cast:** a `Boss` (enemy and art) per cutscene, the swarm list per continent, the console, ORA and Voss figures.
- **Overlay (appended):** `RadarRetreat`.
- **Sounds (appended to `CutsceneSound`):** `BossFall`, `Holster`, `LavaHiss`, `IceBreak`, `CrystalChime`,
  `SealHum`, `BogBubble`, `CrystalBurst`, `CrystalCharge`. Each falls back to a procedural or library clip until
  Remco's own sound exists.
- **Planet:** `VeinsLight` reads data per cutscene: how many veins are lit (1 to 5, or all), and the ring state (none,
  faint, cracked, intact). The map globe should show the same state after each continent (STORY.md "Gloeiende aders").
- **Themes:** the Underground set needs a theme per cutscene (opening cave, ice, Core).

---

## 10. Story consistency check (for curator approval)

What these scripts depend on, or extend slightly:

1. **Placement after the boss** moves each next continent's landing from a level 1 text beat into the end of the
   previous cutscene. The level 1 LevelStart lines stay as written.
2. **Gravemaw smashes the colony drill rig** (BOSSES.md curator default, still open). Shot 3 of continent 1 depends
   on it and reconciles it with ORA's "Extraction at full capacity" by putting the rig back up. If the default is
   dropped, shot 3 shows the drill simply restarting.
3. **Early hint-B seed in continent 1** (eyes turning away, a pulse running down a vein). STORY.md first names hint B
   in continent 3, level 9. This is a silent visual only.
4. **Veins lighting on the planet after each continent** is STORY.md's own proposal ("A + B zichtbaar maken"). The
   ring shows faint after continent 4 and cracked after continent 5; STORY.md places the cracked ring in The Core.
   The seal glimpse in continents 3 and 5 never names it: Voss still decodes the seal in The Core, level 5.
5. **ORA's glitch line in continent 2** ("Cause: un- ...unknown") is new; it fits the glitches that start in level 10.
6. **The Frost Warden's last act** (turning back to the lake, the glyph ring) extends STORY.md ("the name Warden is the
   first real hint") with a visual. The glyphs already exist in its emission map. BOSSES.md has it come from the
   frozen lake, which this uses.
7. **The metal glint at the Mirewood landing** comes slightly before level 5 ("Mercer ziet metaal in het moeras"). No
   line; it can be cut.
8. **Pioneer-1's bore hole** is new decor that makes STORY.md's line "Pioneer-1 drilled too" visible. STORY.md calls
   Pioneer-1 an unmanned probe; a probe with a drill is consistent with "it drilled".
9. **NTI cargo pods at the end of continent 4** tease the superweapons before continent 5, level 4, where NTI sends
   them. No dialogue.
10. **The Shattered's origin.** STORY.md: "a Burrower full of overcharged Crysium; we made this monster". BOSSES.md: "a
    crystal giant from the old ruins; a sealed gate in the decor opens". The script follows STORY.md (a burnt-out
    Burrower lies in the crater). "Old ruins" would imply builders on the planet, which is a lore extension; if it
    stays, suggest the ruins are old Burrower crystal structures around the veins.
11. **The Sealed One's end** combines STORY.md ("at its defeat the seal closes"), BOSSES.md ("the last phase is the
    release") and the round 28 clips (`Release`, then `Death` with the stones closing). The ring of overcharged
    crystals from levels 15 to 19 fires in shot 1, as STORY.md level 15 explains.
12. **The Burrower who thanks Mercer** is proposed as the Queen from the opening (a bookend: the one who screamed the
    first attack ends it). STORY.md only says "a Burrower".
13. **Epilogue narration** has no speaker in STORY.md. ORA speaks it, as she narrates the prologue. "A new team takes
    over" is shown only by the NTI frame going dark; no new organisation or person is named.
14. **First daylight in the epilogue** (a dawn theme for Crystal Plains) is new; every level so far is a night theme.
15. **Voss in 3D.** So far she only appears as a portrait. The scripts put her at the radar console, chest-up, using
    her existing bust rig (she stands; no arm gestures).
16. **Mercer's face.** The art rule says his face is never shown; the visor carries the expression. The scripts follow
    that and frame the visor from the front. A face reveal would need a face model (MPFB) under a clearing visor.
17. **Out of scope here:** the continent 5, level 14 intermission (the base partly destroyed) and the level
    intermissions (`Dlg_*`) are not part of these cutscenes.
18. **BOSSES.md vs STORY.md mechanics** (the Cinder Matriarch hunting cold towers vs "Cryo slows her"; the Frost
    Warden's Gate Breaker vs "freezes turrets in a radius"; the Sealed One's Recognition vs "summons corrupted
    enemies") do not affect these scripts; only death and aftermath are shown.

## 11. Open questions for the curator

1. **Placement:** after the boss, before the result screen (proposed), or after the result screen?
2. **The battlefield in the aftermath shots:** rebuild the player's own towers and wrecks from the finished run (a
   tower snapshot passed to the cutscene, which makes it feel earned), or a fixed authored layout per boss level, or
   an empty dressed field (simplest, but odd right after a battle)?
3. **Mercer's face:** visor front only (as written), or one helmet-off or clear-visor moment in the finale (needs an
   MPFB face)?
4. **Voss in 3D at the console** (as written), or portrait only as before?
5. **The Shattered:** which species lies in the crater (proposal: a Cragback, the stone heavy, at normal size), and do
   the "old ruins" from BOSSES.md stay?
6. **The thanking Burrower:** the Queen from the opening (proposed), or a common species?
7. **Arrival shots when the next continent is not built:** skip them (proposed), or show the landing as a teaser anyway?
8. **Early teasers:** OK to show the continent 1 vein pulse, the Mirewood metal glint and the continent 4 cargo pods
   before their story beats?
9. **New lines for approval:** ORA "Contacts withdrawing. Heading: north. Cause: un- ...unknown." (continent 2); the
   epilogue narration given to ORA (three lines, translated from STORY.md).
10. **Dawn in the epilogue:** OK to show the first daylight of the game?
