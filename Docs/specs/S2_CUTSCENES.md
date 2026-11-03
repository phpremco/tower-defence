# S2 – Opening cutscene and prologue (spec)

Source: `Docs/STORY.md` ("Proloog", "Openingscutscene"), `Docs/STORY_ROADMAP.md` (S2), `Docs/specs/S1_STORY_ENGINE.md` incl. its critic decisions. **Prerequisite:** S1 merged: `SpeakerDefinition` (frame styles Portrait/Hologram/Transmission/Log), `DialogueDefinition`, `DialoguePlayer` (queue, unscaled reveal clock, skip, auto-advance hook) and `DialogueView` (TextMeshPro, needs only a root, input provider and clock). S2 adds no second text system.

## 1. Data (`Game.Levels`, folder `Scripts/Levels/Story/Cutscene/`)

- **`StoryDefinition`** gets `_openingCutscene : CutsceneDefinition` (`OpeningCutscene`). It replaces `_introBeats`; `StoryBeat`, `StoryScene` and `IntroScreen` are deleted in step 5 (same `Story.asset`, so ids stay).
- **`CutsceneDefinition`** (SO, id = asset name, `Cutscene_Opening`): `_shots : CutsceneShot[]`, `_cast : CutsceneCast`, `_handoff : CutsceneHandoff { ReturnScene, FirstCampaignLevel }`, `_handoffDifficulty : DifficultyProfile`.
- **`CutsceneShot`** (struct, plain fields, no `[SerializeReference]`):
  - `Set : CutsceneSet { Earth, Survey, Orbit, Field, Underground }` and `Action : CutsceneAction` (enum, see §3; values only appended).
  - `Camera : CameraKey[]`; `Timing : ShotTiming { Fixed, AfterDialogue }`; `Seconds` (fixed length, or minimum after the dialogue ends).
  - `Dialogue : DialogueDefinition` and `DialogueDelay` (seconds into the shot).
  - `Cues : CutsceneCue[]`; `Music : CutsceneMusic { Keep, Silence, Wonder, Tension, Attack }`.
  - `FadeIn`, `FadeOut` (to black, never white); `Letterbox : bool`; `TitleCard : string` (shot 10: "Defend the base").
- **`CameraKey`** (struct): `At` (0..1 of the shot), `Position`, `LookAt`, `FieldOfView`, `Ease : Ease { Linear, In, Out, InOut }`, `Mode : CameraKeyMode { Free, MatchLevelRig }`. `MatchLevelRig` takes the pose the Level scene's `FixedCameraRig` will start in (from its static `FitSize`/angle helpers), so the last frame matches the first frame of level 1.
- **`CutsceneCue`** (struct): `At` (seconds), `Kind : CueKind { Sound, Shake, SetEvent }`, `Sound : CutsceneSound` (enum → `SfxLibrary` set), `Clip : AudioClip` (optional override, wins), `Strength`, `EventId : int` (set-specific beat, e.g. "next module unfolds").
- **`CutsceneCast`** (SO, the look in data): `Hero : HeroDefinition`, `Hauler : DroneDefinition`, `Builder : DroneDefinition`, `Swarm : EnemyDefinition[]`, `Screamer : EnemyDefinition`, `FieldLevel : LevelDefinition` (null = first campaign level), per-set `VisualTheme`s (`Theme_Earth`, `Theme_Underground` new; Orbit/Field reuse level 1's), counts (`CapsuleCount`, `EyePairs`, `DroneCount`) and optional prefab overrides per role (`Mothership`, `Capsule`, `Drill`, `Beacon`, `EarthSurface : Texture2D`). A set builds the override when set, the procedural model otherwise, so a low-poly model arrives by data only.
- **`OnValidate`**: shots exist; `AfterDialogue` needs a dialogue; `CutsceneActions.Supports(set, action)`; camera keys sorted by `At`; the last shot has a `MatchLevelRig` key when the handoff is `FirstCampaignLevel`.

## 2. Runtime (`Game.UI`, folder `UI/Cutscene/`; composition root in `Game.Boot`)

- **`CutsceneStage`** (Boot, like `MenuStage`): creates the camera, `AudioListener` and `CutscenePlayer`. Each set owns its own `Volume` built with `PostProcessingBuilder` from its theme; volumes cross-fade by weight between sets.
- **`CutsceneRequest`** (service, Levels): `Cutscene`, `Handoff`, `ReturnScene`, `AutoAdvance`, `Speed` (test only). `Bootstrapper.Start` registers it when `!IntroSeen` (handoff `FirstCampaignLevel`). Options registers `ReturnScene = MainMenu`.
- **`CutsceneSequencer`** (pure C#): shot index, local time, cues due this frame (fires each once, in order, also on a large `dt`), dialogue gating, `Skip()`, `Finished`. All time is `Time.unscaledDeltaTime × Speed`; `Time.timeScale` is never touched.
- **`CameraPath`** / **`Easing`** (pure): Catmull-Rom position, eased look-at and FOV. `CutsceneCamera` applies the sample plus a shake from a small pure `ShakeCurve` extracted from `FixedCameraRig` (same feel, one source).
- **`CutscenePlayer`**: builds sets lazily behind the first fade, keeps them and toggles them by shot; drives the sequencer, the `DialoguePlayer` (frame styles come from the speakers) and `CutsceneOverlay` (letterbox bars, black fades, TMP title card, skip button). It does not derive from `ScreenBase`, because that starts menu music and the starfield; it repeats only its Boot fallback.
- **Set registry, no reflection:** `ICutsceneSet { Build(Transform root, CutsceneContext ctx); Enter(CutsceneAction a); Tick(float time, float progress); OnEvent(int id); Exit(); }`. `CutsceneSets.Create(CutsceneSet)` is a `switch` returning `new EarthSet()`, … IL2CPP-safe.
- **Skip:** the whole cutscene. A visible "Skip" button (pointer), B/Esc (`Cancelled`) or Start (`PauseToggled`) shows "Press again to skip" for 2 s, and a second press skips. A (`Confirmed`) only advances dialogue. Skip and the end do the same thing: `MarkIntroSeen` (saved at once), fade, handoff.
- **Mandatory once:** Boot routes to `Intro` until `IntroSeen`, so the first launch always starts in the cutscene. Quitting before the end replays it next launch.
- **Handoff:** `FirstCampaignLevel` resolves `catalog.Nodes[catalog.FirstNode(0)].Level` and registers `new LevelSession(level, _handoffDifficulty, briefing: true)`, then loads `Level`. `ReturnScene` loads that scene. `MenuMusic` also fades on `Intro`.

## 3. Prologue and the 10 shots

All text is ORA/Mercer/Voss dialogue (English, generator-written, proposed until Remco approves). Every model is a textured factory model (`ModelPalette`, `PropFactory.Dress`, the UnitLit rim shader), never a bare primitive. Glow comes from emission plus bloom, not full-screen pulses.

| # | Set / action | Reuses | New (basic-first) | Later via data |
| --- | --- | --- | --- | --- |
| P | Earth / `EarthDrift`, then Survey / `ScanPlanet` | `PlanetPainter.PaintClouds`/`PaintHalo`, the globe's atmosphere rim, baked globe maps for S-69-CRYS | `EarthPainter`: a grey-brown sphere, dull oceans, open-pit scars, a few dying amber city lights, a smog rim; slow orbit. Survey: scan rings sweep S-69-CRYS, crystal glints. ORA Hologram boxes, about 8 lines condensed from the prologue | `EarthSurface` texture, theme |
| 1 | Orbit / `CapsulesFall` | S-69-CRYS globe, `DroneModelFactory` nav lights | `MothershipModelFactory` (spine, ring, engine glow, hull lights); `CapsuleModelFactory`; falling star = emissive head + `TrailRenderer` + `ParticleKit` sparks, orange→white | `Mothership`/`Capsule` prefabs |
| 2 | Field / `PathfinderLands` | `LevelVisualBuilder.Build(FieldLevel)` (the real level 1 terrain, props, mist), `PropKind.Crystal`, `HeroView.BuildDisplayFigure` | capsule landing dust, `BeaconModel` (tripod, pulsing lamp, point light); Mercer walks and looks around via `CutsceneHeroMotion` | `Beacon` prefab, hero skin |
| 3 | Field / `BaseUnfolds` | `BuildBaseDisplay`, `GatePylon`/`GateBeam`, repair-drone welding sparks | staggered unfold (scale + rotation per module on `SetEvent`), fast sun sweep for the timelapse shadows | module prefabs |
| 4 | Field / `DrillDown` | `Earthquakes` set, camera shake | `DrillModelFactory` (derrick, spinning bit), dust ring, ground cracks glow | `Drill` prefab |
| 5 | Field / `DronesHarvest`, `CoreIgnites` | hauler drones (lit cargo pod, drill), harvest/fan loops | flock routes deposit→base; core emission and light ramp with bloom | `DroneCount`, hauler def |
| 6 | Underground / `CrystalsTremble`, `EyesOpen` | crystal props, warm creature palette | dark cavern shell, crystals jitter on the drill's cue, `EyePairs` (≈300 High / 120 Low) emissive pairs open (scale Y) in a spreading wave; camera dives through dust | counts, theme |
| 7 | Field / `GroundShivers` + `RadarOverlay` | `BaseAlerts`, `BossAlarms` | local dust ripple at Mercer's feet; UI radar (sweep, 1 → 10 → red blob of merging dots) | — |
| 8 | Field / `SwarmPours` | `MenuCreature` (or `EnemyDisplayBuilder` + route mover) on the level road, `ElementNest` cracks, `CreatureVoice`/`CreatureChorus` | crack spawn points from the level's nests; the scream from `Screamer` | `Swarm` list |
| 9 | Field / `GateStand` | `BlasterRig`, Voss Portrait, ORA Hologram | Mercer runs to the gate and draws | — |
| 10 | Field / `Handoff` | — | camera to the `MatchLevelRig` pose, TMP title card, fade, load Level | — |

**Hero motion:** `HeroView` animates from `Commander`. Step 3 adds a tiny `IHeroMotion` (IsWalking, velocity, weapon drawn) that `Commander` implements and `CutsceneHeroMotion` fakes. This needs review because it touches Hero.

**Low profile:** the particle counts, eye pairs and drone count scale with `IPlatformSettings.ParticleScale`.

## 4. Audio

Reuse: `GameMusic`/`BossMusic`/`ProceduralMusic.Calm`/`Intense` for the music moods, `Earthquakes`, `DroneFanLoops`, `DroneHarvestLoops`, `StructureHits` (build clanks), `Upgrades` (core ignition), `BaseAlerts`, `BossAlarms`, `HeroSteps`, creature voices and `CreatureChorus`.

Optional new clips, through the `Clip` override or new library sets: re-entry whoosh, deep drill rumble loop, radar ping, an underground heartbeat drone, a primal scream and a cutscene score.

## 5. Tests

- **EditMode** `CutsceneSequencerTests`:
  - fixed shots end at exact times;
  - `AfterDialogue` holds until the dialogue is done plus `Seconds`;
  - cues fire once, in order, on a big `dt`;
  - `Skip` finishes once;
  - `Speed` scales time.
- **EditMode** `CameraPathTests`: the endpoints equal the keys, a single key works, and the easing stays monotonic.
- **EditMode** `CutsceneDataTests`:
  - the generated asset validates;
  - every action is supported by its set;
  - the handoff level resolves;
  - the auto-advance runtime falls inside the agreed band.
- **PlayMode** `CutsceneSmokeTests` (in-memory progress):
  - Boot → Intro with `AutoAdvance`, `Speed = 8`: the Level scene loads, a `LevelContext` exists, `IntroSeen` is true, `LogAssert.NoUnexpectedReceived()`;
  - a skip in shot 3 also reaches the Level scene;
  - a replay with `ReturnScene` reaches MainMenu.
- **Screenshots:** `ScreenshotTests.Cutscene_WritesShots` uses `CutscenePlayer.Seek(shot, progress)` and writes `Logs/cutscene_shotNN.png` mid-shot for P and 1–10. These run through the CLI test filter and are reported to Remco.

## 6. Build steps

1. **Data plus pure logic:**
   - the types in §1;
   - `CutsceneSequencer`, `CameraPath`, `Easing`, `CutsceneActions`;
   - `StoryDefinition._openingCutscene`;
   - EditMode tests.

   ⚠ `StoryDefinition` (S1 also edits it).
2. **Player and flow with the Field set**, shots 2, 9 and 10:
   - `CutsceneStage`, `CutscenePlayer`, `CutsceneOverlay`, `CutsceneCamera`, `ShakeCurve`, `CutsceneRequest`, `CutsceneAudio`;
   - `ProjectGenerator.Cutscene.cs` (step "Cutscene" after "Story");
   - the Intro scene in `.Scenes.cs`;
   - `Bootstrapper` routing, the `MenuMusic` Intro rule, the PlayMode smoke.

   It already looks finished, because it is the real level 1 terrain. ⚠ `ProjectGenerator.Steps.cs`, `.Scenes.cs`, `Bootstrapper`, `FixedCameraRig` (extract only).
3. **The rest of Field**, shots 3, 4, 5, 7 and 8:
   - `DrillModelFactory`, `BeaconModel`, `RadarOverlay`;
   - `IHeroMotion` + `CutsceneHeroMotion`.

   ⚠ `HeroView`, `Commander`.
4. **Space and underground**, P, 1 and 6:
   - `EarthPainter`, `MothershipModelFactory`, `CapsuleModelFactory`;
   - the Underground set;
   - the new themes;
   - the screenshot test.
5. **Replay and cleanup:**
   - a "Replay intro" button in `OptionsOverlay`;
   - delete `IntroScreen`, `StoryBeat`, `StoryScene` and `_introBeats`;
   - the cast prefab overrides;
   - Low-profile scaling;
   - the final screenshots.

## 7. Open questions for Remco

1. **Pacing.** Should text boxes wait for A, Pokémon style (as specced), with the visual shots timed? What total length do you want (my suggestion: about 2 minutes on auto)?
2. **Difficulty and briefing.** On which difficulty does the first launch drop into level 1 (Easy or Medium)? Should the level briefing card and recon still show after "Defend the base", or does the level go straight in?
3. **Skip on first launch.** Is a press-twice confirmation right, or should one press skip at once?
4. **Prologue.** The prologue is condensed to about 8 ORA boxes in English. Do you write or approve the lines? Should Earth get a few real-world shots (empty mines, dead cities) or only the planet from orbit?
5. **Camera.** The cutscene uses a free perspective camera and only ends on the fixed ortho view. Is that OK, given the "no free camera" rule for gameplay?

---

## Curator decisions (2026-11-03) — override the sections above where they conflict

- Owner wants autonomy; these defaults stand unless Remco overrules them.
- **Pacing:** prologue about 40 s, cutscene about 2–2.5 min of auto-play time excluding reading; dialogue boxes wait for the player.
- **First launch** drops into level 1 on **Easy** (the "learn and follow the story" difficulty). The level's LevelStart dialogue replaces the briefing body (S1 rule); recon still shows as normal.
- **Skip:** one press shows "Press again to skip" for 2 s; a second press skips. A visible button does the same for the mouse.
- **English lines:** the curator/builder writes the English prologue and cutscene lines as a faithful translation of `Docs/STORY.md`, with stable line ids.
- **Camera:** a free perspective camera is fine for cutscenes. Only gameplay keeps the fixed orthographic camera. The last shot blends into the level camera.
- **Replacement is in scope:** `IntroScreen`, `StoryBeat`, `StoryScene` and the old intro beats are removed in step 5 once the new cutscene works (no dead code). `IHeroMotion`, `ShakeCurve` and the `MenuMusic` fade are approved small refactors.
- **Dependency:** S1 must be built and merged before S2 step 2+. S2 step 1 (pure sequencer + data types) may be built after S1 step 1.

## Critic review v1 and curator decisions (2026-11-03) — override everything above

1. **Post-processing:** `PostProcessingBuilder.Build` returns the `Volume` (weight + optional parent). `CutsceneStage` (Boot) injects a `Func<VisualTheme, Volume>` into `CutsceneContext`; UI never references URP directly.
2. **Lighting:** extract `LevelBootstrapper.BuildEnvironment` (sun, rim, fog, trilight ambient, SkyProbe) into a shared `ThemeLighting.Apply(theme)` used by the level and by cutscene sets. Pure move; the level must look identical (screenshot before/after).
3. **`LevelVisualBuilder.Build`** gets a dressing-only option (no chevrons, route markers, mining nodes, nests markers or gameplay overlays).
4. **Tilt-shift:** the cutscene camera uses a second renderer without the TiltShift feature (generator-created), or a toggle; no band blur on cutscene shots.
5. **End of cutscene:** no "last frame = first frame" promise. Aim at the level's play pose with a long-lens perspective camera, then fade to black and load the level. The briefing card and recon show as usual.
6. **Reuse what exists:** `SceneNames.Intro`, Boot routing on `IntroSeen`, the Intro scene generator and `MarkIntroSeen`. Step 2 only swaps `IntroScreen` for the cutscene stage in the generator. `MenuMusic` fade only matters for replays.
7. **No `IHeroMotion`.** Animate `HeroView.BuildDisplayFigure` inside the cutscene (its own small walk/turn/draw-pistol rig); `HeroView` and `Commander` stay untouched.
8. **Seek-safe sets:** every set's `Tick(t)` depends on time only (eyes, flock, unfold, particles via `ParticleSystem.Simulate`). Seek skips dialogue holds. Auto-advance uses S1's `DialoguePlayer` test hook.
9. **Performance:** eyes are one combined mesh, or `Graphics.RenderMeshInstanced` with a per-instance open value (shader property). The swarm creatures are pooled. Low profile halves the counts and caps dust and overdraw separately.
10. **Input:** while a dialogue box is open, Cancel belongs to the dialogue (skip conversation). With no box open, Cancel or Start arms the whole-cutscene skip. Never both on one press.
11. **Data validation:** `_handoffDifficulty` is validated against `CampaignCatalog.Difficulties`. The handoff skips the loadout panel (documented).
12. **Scope:** step 4 (new factories for Earth, mothership, capsules, underground) is built, but each shot has a fallback using existing sets, so the cutscene is playable before every factory exists. Every set still gets emissive glow, trails and point lights; no bare primitives.
13. **Order with S1:** S1 swaps `_introBeats` for dialogue references; S2 only adds `_openingCutscene`. S1 adds the TMP asmdef reference to Game.UI.

## Builder notes steps 4–5 (2026-11-03)

Curator-level choices made while building, for review:

1. **Space sets are real 3D, not a canvas backdrop.** Earth, the survey and the orbit each build a lit `PlanetBody` (the map globe's sphere and materials, made `internal` in `PlanetGlobe`) in front of the cutscene camera, with their own sun under the set root (no shadows, three times the theme's light intensity because space has no ambient) and reflections at 12 % (a glossy sea seen at a grazing angle otherwise mirrors the whole sky probe). The backdrop canvas layer of steps 1–3 is gone, as is the stand-in `SpaceSet`.
2. **Earth** is painted at runtime by `EarthPainter` (1024×512, 512 on Low, painted once and cached): grey-brown land, murky seas, 46 terraced open-pit scars, dirty ice, dim amber city clusters that mostly went dark, and a brown smog deck. The cast's `EarthSurface` texture replaces only the albedo. No real-world close-ups (spec question 4) — only the planet from orbit.
3. **Survey** uses the campaign's baked globe maps (fallback: a plain painted globe). The scanner is a bright ring sweeping pole to pole every 4.2 s with a dimmer echo, a cage of eight turning meridians, and crystal glints that flare where the ring has just passed, on the camera side only.
4. **Orbit**: the planet is seen from low orbit with the first landing region turned to the top of the curve; `MothershipModelFactory` (spine, command module, cargo, bay, radiators, three nozzles, separate turning habitat ring) glides past; `CapsuleDrop` releases `CapsuleCount` capsules (Low halves it) on a cubic path that speeds up, heats the shield, flares a head glow and stretches a burning trail inside the outer 35 % of the planet radius. The trail is the path sampled back in time (seek-safe), not a `TrailRenderer`. The drop repeats while the box is read. Spark particles from the spec were left out (they would need `Simulate` to stay seek-safe; the trail and the head glow read as the falling star already).
5. **Underground** keeps the steps 1–3 cave and its single combined eye mesh (critic decision 9 allows one mesh); changes: shut eyes now collapse to nothing (before, hundreds of thin amber slits were visible before anything opened), the first two pairs are 1.6× and 1.3× size, the atmosphere comes from the new `Theme_Underground` instead of hand-set fog, and the dust cap scales with the profile.
6. **Themes as data**: `Data/Story/Cutscene/Theme_Earth.asset` (pale dusty sun, brown sky, saturation −22) and `Theme_Underground.asset` (near-black fog 0.05, faint cold ambient, warm rim), written by the "Cutscene" generator step and set on the cast.
7. **Polish**: hauler drones fly at 2.2× scale in the cutscene (cinematic scale, the level is unchanged); the Queen as screamer is shown at 0.7×; the cutscene dialogue box sits 6 units above the bottom letterbox bar at 86 % size (`DialogueView.SetPlacement`; the in-level default is untouched).
8. **Low profile** (`SwitchPlatformSettings`): half the stars, capsules, eyes, drones, swarm and every dust or spark cap; the Earth map at half resolution; a new `IPlatformSettings.HighQualityBloom` (false on Low) switches the bloom to its cheaper filter. That also applies to levels on Low.
9. **Replay intro** in the options column (after "Sound test", only when the story has an opening cutscene) registers a `ReturnScene = MainMenu` request and loads Intro; it never touches the save.
10. **Cleanup**: `IntroScreen`, `StoryBeat`, `StoryScene`, `_introBeats` and their generator code are deleted; `PlanetPainter` keeps only `PaintHalo` (its surface and cloud painters were used by the old intro only); the tools `intro_probe.cs`/`intro_skip.cs` are deleted. The cast's `Beacon` override is now wired (`BeaconModel` hides its procedural parts and keeps the light, the pulse and the glow pool).
11. **Screenshot check**: `ScreenshotTests.Cutscene_WritesShots` seeks the middle of every shot, writes `Logs/cutscene_shotNN.png` at 1920×1080 and fails when a picture is black (mean brightness below 0.006).
