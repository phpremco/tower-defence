# S1 – Story engine (spec)

Source: `Docs/STORY.md`, `Docs/STORY_ROADMAP.md` (F0, S1). Assumes F0 is merged: token-based `TimeController.Hold`, `ProgressData.Version`, `HasSeenStory(id)` / `MarkStorySeen(id)`, generator steps with one partial per feature.

## 1. Data (`Game.Levels`, folder `Scripts/Levels/Story/`)

Levels cannot see UI, so all story data lives in `Game.Levels`; only views live in `Game.UI`.

- **`SpeakerDefinition`** (SO, id = asset name): `_displayName`, `_roleColour` (name tag, rim light, box border), `_portrait` (`PortraitModel` enum), `_portraitOverride` (Sprite, optional; wins over the model when set, so a drawn portrait arrives later via data only), `_defaultGlitch` (bool, unused for ORA in level 1, ready for later).
- **`enum PortraitModel { Hero, Scientist, Hologram, Emblem, Probe }`** (values only appended). Hero = Mercer from `HeroModelFactory`; Scientist = Voss; Hologram = ORA (emissive core plus orbit rings, no body); Emblem = NTI (flat emissive badge); Probe = Pioneer-1 (dish and antenna).
- **`DialogueDefinition`** (SO, id = asset name, e.g. `Dlg_L01_Start`): `_lines : DialogueLine[]`. The id is the once-per-save key.
- **`DialogueLine`** (struct): `Speaker` (SpeakerDefinition), `Segments : DialogueSegment[]`, `Glitch : bool` (the whole line jitters and the portrait flickers).
- **`DialogueSegment`** (struct): `Text : string`, `Style : SegmentStyle`.
- **`enum SegmentStyle { Normal, Redacted, Struck, Glitch }`**.

**Why segments, not markup:** with segments, a struck word or a `[REDACTED]` span has exact edges and is checked when the data is saved. Nothing is parsed at runtime, so a typo in a tag cannot fail silently. It is IL2CPP-plain (structs and arrays), and translation later works per segment. In the inspector a plain line is one segment. The generator writes lines with a helper (`Say(ora, "Enemy wave inbound.")`, `Seg("Protocol "), Redacted("Omega")`).

`OnValidate`: the dialogue has lines, every line has a speaker and non-empty text, a Redacted segment has at least one character (its length sets the bar width).

**Assets:** `Data/Story/Speakers/Speaker_{Ora,Voss,Mercer,Nti,Pioneer1}.asset`, `Data/Story/Dialogues/Dlg_L01_*.asset`. A new partial `Editor/ProjectGenerator.Story.cs`, registered as one F0 generator step, uses `LoadOrCreate<T>` and overwrites the fields on every run, so it never makes duplicates. It also writes the trigger arrays onto the level assets.

## 2. Triggers

- **`StoryTrigger`** (struct, on `LevelDefinition._story : StoryTrigger[]`, exposed as `StoryTriggers`):
  - `Kind : StoryTriggerKind { LevelStart, WaveStart, FirstSighting, BossSpawn, BossDefeated, LevelWon, FirstUse, BetweenLevels }`
  - `Wave : int` (1-based, the HUD number)
  - `Enemy : EnemyDefinition` (FirstSighting; optional filter for Boss*)
  - `PathObject : PathObjectDefinition` (FirstUse)
  - `Dialogue : DialogueDefinition`
  - `Repeat : StoryRepeat { OncePerSave, Always }`

  These are plain fields read by kind, with no `[SerializeReference]`. `LevelDefinition.OnValidate` warns when a field that the kind needs is missing.
- **Replay rule:** a `OncePerSave` dialogue plays if `!HasSeenStory(id)`, or if `DifficultyProfile.ReplaysStory` is set. That is a new bool, true only on Easy, and it carries the roadmap's "always on Easy". `Always` plays every run. Within one run, each trigger fires at most once.
- **`StoryEvent`** (struct: Kind, Wave, Enemy, PathObject) and **`StoryTriggerSelector`** (pure static C#): `Select(triggers, evt, isSeen, replay, firedThisRun, results)`. It appends matching dialogues in array order and does not allocate (the caller passes the list in). Tests target this.
- **Hooks** (subscribed by `StoryDirector`, all existing `LevelContext` events):

| Kind | Source |
| --- | --- |
| LevelStart | first frame where `!BriefingPending && !ReconPending` |
| WaveStart n | `WaveStarted` + `WaveNumber` (confirm 0/1-based at `LevelController:103`) |
| FirstSighting | `EnemySpawned(enemy)` → `enemy.Definition` |
| BossSpawn / BossDefeated | `EnemySpawned` / `EnemyKilled` where `Definition.IsBoss` |
| LevelWon | `Changed` with `Outcome == Won` (before `ResultView`) |
| FirstUse | `PathObjectSpawned(po)` → definition |
| BetweenLevels | the triggers of the level just won, played by `WorldMapScreen` when it opens after a win, before focusing the next node |

- **Briefing:** `BriefingView` stays the title card (region, name, Deploy). `LevelDefinition.Briefing` is the default LevelStart beat. If a level has no LevelStart trigger, the card shows the briefing text as today. If it has one, the card leaves the body out and the dialogue tells the story. So there is one source per level and no second system.

## 3. Runtime: `StoryDirector` (`Game.UI`, added in `HudView.Initialize`)

- **Queue:** `Queue<DialogueDefinition>`. It takes one hold token (`_time.Hold(...)`) when the first dialogue starts and releases it when the queue is empty, which also happens in `OnDestroy`. It calls `MarkStorySeen` per dialogue when it ends or is skipped. The save happens at the existing save points; BetweenLevels saves at once.
- **`LevelContext.StoryBusy`** (bool, new): it is true while the queue is not empty. `TimeController.Apply` also mirrors the token count to **`LevelContext.Held`**. Gameplay input gates check `Paused || Held`: `PlacementController`, `HeroController`, skip wave and speed toggle. Today the briefing and the Codex let keyboard confirm through, and this fixes that too.
- **Waiting rules:** do not start while `BriefingPending`, `ReconPending`, `Paused`, or while the Codex is open (`CodexView.IsOpen`, new getter). The Codex button is not interactable while `StoryBusy`. `ResultView` waits for `!StoryBusy`, so LevelWon plays first. A loss clears the queue.
- **Time:** the hold freezes the scaled game clock, so the wave countdown and the game speed stay where they were. Typewriter and animations use unscaled time.
- **Input** (`IInputProvider`, **no interface change**):
  - `Confirmed` (ignored when `FromPointer`): finish the typewriter, or go to the next line.
  - `Cancelled` (ignored when `FromPointer`): skip everything queued.
  - The mouse uses the box itself (a full Button, which advances) plus visible **Next** and **Skip** buttons.
  - `PauseToggled` is ignored while `StoryBusy`: `TimeController.TogglePause` returns early. The Cancel handlers in the HUD and tower menu return early on `StoryBusy`, so Esc/B never deselects or opens anything underneath.
- **`LevelContext.StoryEnabled`** is set from `LevelSession.Briefing`, so tools and simulation tests skip story exactly as they skip the briefing.
- **Public test API:** `Advance()`, `SkipAll()`, `IsPlaying`.

## 4. View: `DialogueView` (`Game.UI`)

- **Layout:** built in code from `UiKit` on the HUD safe root (reference 1920×1080 via the existing `CanvasScaler`, which works from 720p up). A light dim covers the field (raycast-blocking). The box sits low but above the tower bar, about 1300×260.
- **Portrait frame:** 240×240 on the left, with an inner border in the role colour.
- **Name tag** above the box, in the role colour.
- **Text:** `UiKit.BodyFont`, about 34 pt, wrap on.
- **Prompt:** a blinking "▸" when the line is complete.
- **Typewriter:** the full text is laid out once; unrevealed characters are wrapped in `<color=#0000>` so the layout never jumps. Speed is about 45 characters per second (a const on the view).
- **Struck / Redacted:** legacy `Text` has no strikethrough, so a small `SegmentOverlay` reads `Text.cachedTextGenerator.characters` after layout. It draws thin bars (Struck) or solid black bars (Redacted, with the underlying characters transparent) over the segment's character range. No new packages.
- **Glitch:**
  - On a segment: random character swaps for short bursts, settling to the real text.
  - On a line: also positional jitter and portrait flicker.
- **Portraits** go through **`PortraitProvider`** (UI, owned by `StoryDirector`), which is the only caller of `ModelIconRenderer` for portraits. It builds via **`PortraitModels.Build(stage, PortraitModel)`** (UI, procedural; Hero reuses `HeroModelFactory`).
- **`ModelIconRenderer` extension:** add the overload `Request(key, build, done, IconFraming framing)`.
  - **`IconFraming`** is a readonly struct: size, ortho size, lift, pitch, yaw, rim light.
  - `IconFraming.Bust` = 256 px, a tighter ortho, a higher lift, and a back rim light in the role colour.
  - Render textures are kept per size. The existing overload uses `IconFraming.Default`, so all current callers stay untouched.
  - The static singleton is not spread. Only `PortraitProvider` adds a call site, and it caches per `(speaker, framing)`.

## 5. Tests

- **EditMode** (`Tests/EditMode/StoryTriggerSelectorTests.cs`):
  - each kind matches only its event;
  - WaveStart matches exactly n;
  - order is preserved for two triggers on one event;
  - OncePerSave is skipped when seen and plays when `replay`;
  - `Always` ignores seen;
  - a trigger fires at most once per run;
  - FirstSighting with a different enemy does not match.
- **EditMode** `StoryDataTests`: every `DialogueDefinition` and `SpeakerDefinition` asset passes validation, and every level trigger has the fields its kind needs.
- **PlayMode** `StorySmokeTests`:
  - load `Level_Landing` via `LevelSession(..., briefing: true)` on Medium;
  - Deploy, skip recon;
  - assert the LevelStart dialogue plays, `Held` is true, and `SkipWave` is ignored;
  - `Advance()` through all lines; the hold is released;
  - wave 2 starts → its dialogue plays → `SkipAll()`;
  - run the level at simulation speed to the end;
  - `LogAssert.NoUnexpectedReceived()`.
- **Effects fixture:** a test-only dialogue (in the test, not shipped) with Redacted, Struck and Glitch segments renders without errors.

## 6. First content (Level_Landing, English)

The script is Dutch; the in-game text is English and written here. **Proposed** lines are mine and need Remco's approval, since the script has only one Level 1 dialogue.

| Trigger | Lines |
| --- | --- |
| LevelStart (script) | ORA: "Hostile wave approaching. Place turrets along the path." / Mercer: "And whatever gets through is mine." |
| WaveStart 2 (proposed) | ORA: "Second wave detected. Larger signature." / Mercer: "They're not testing us anymore." |
| BossSpawn (proposed) | ORA: "Large contact on the main road." / Mercer: "That one's mine too. Keep the guns on it." |
| LevelWon (proposed, from "the Burrowers are awake") | ORA: "Perimeter holding. Wave repelled." / Mercer: "We woke something up down there." |

All four are OncePerSave. ORA has no glitch in level 1: the script wants it subtle and later.

## 7. Build steps

1. **Data plus selector:** `SpeakerDefinition`, `DialogueDefinition`, `DialogueLine`, `DialogueSegment`, `SegmentStyle`, `PortraitModel`, `StoryTrigger*`, `StoryEvent`, `StoryRepeat`, `StoryTriggerSelector`; `LevelDefinition._story`; `DifficultyProfile._replaysStory`; EditMode tests. Compiles, nothing visible yet. ⚠ `LevelDefinition`.
2. **Generator content:** `ProjectGenerator.Story.cs`, the speakers, the four Level 1 dialogues, the triggers on `Level_Landing`, Easy `ReplaysStory`; `StoryDataTests`. ⚠ generator step registration in `ProjectGenerator.cs`.
3. **Director plus plain view:**
   - `StoryDirector`, `DialogueView` (text, typewriter, name, buttons, placeholder portrait colour);
   - `LevelContext.StoryBusy/Held/StoryEnabled`;
   - gates in `PlacementController`, `HeroController`, `TimeController.TogglePause`, `HudView`/`TowerMenuView` cancel, `ResultView` wait, `BriefingView` body rule, `CodexView.IsOpen`;
   - PlayMode smoke.

   ⚠ `LevelContext`, `TimeController` (F0 agent), `LevelBootstrapper` (one line: `StoryEnabled`).
4. **Portraits and effects:** `IconFraming`, the `ModelIconRenderer` overload, `PortraitProvider`, `PortraitModels`, `SegmentOverlay`, glitch, and the effects fixture test. Screenshot check via `ScreenshotTests`.
5. **BetweenLevels:** `WorldMapScreen` plays the won level's BetweenLevels triggers through a map-side `StoryDirector` mode, without a level time hold, before node focus.

`IInputProvider` and `ProgressData` are **not** changed (only the F0 API is used).

## 8. Open questions for Remco

1. Should the Level 1 lines marked "proposed" stay, or do you want to write them yourself? Should Mercer swear or stay clean?
2. Replay: does "always on Easy" mean that Easy always replays story (as specced), or does each dialogue get its own setting?
3. Should a dialogue during a wave freeze the game (as specced), or should mid-wave teasers run without stopping, like a radio message?
4. Voss has no 3D model. Should she get a quick procedural scientist bust (a new silhouette: coat, visor), or a drawn portrait later?
5. Should skip (B/Esc) skip the whole queue, or only the current dialogue?

---

## Critic review v1 and curator decisions (2026-11-03) — these OVERRIDE the sections above where they conflict

1. **Input gates first.** Build step 1 adds one `LevelContext.InputBlocked` (Paused || held) and uses it in `PlacementController.OnConfirmed`/`OnCancelled`, `HeroController.CanAct`, `LevelController.OnSkipWave`, speed buttons. This also fixes the existing keyboard-confirm leak through Briefing/Codex.
2. **Esc is Cancel, Pause is P/Start.** Pause may open on top of a dialogue; the pause menu freezes dialogue input (no "pause blocked" rule). Dialogue advance listens to `Confirmed` only (ignoring `FromPointer`) and buttons never get EventSystem selection, so one press = one advance.
3. **Hold release safety.** Never call `Apply()`/restore timeScale from `OnDestroy` or after `Outcome != Running`; ResultView owns timeScale after the outcome.
4. **Saving seen ids.** `MarkStorySeen` persists immediately (verify ProgressService saves); LevelWon dialogue must play BEFORE the result screen and be marked seen independently of `RewardCalculator`.
5. **BetweenLevels / map dialogues deferred to S3** (needs a pending-story handoff from level to map). FirstUse also deferred (no content yet). Per-segment Glitch style deferred; ORA glitch is a per-line `GlitchIntensity` float (0 = none) so it can escalate across continents.
6. **Split player from triggers.** `DialoguePlayer` (queue, reveal clock on unscaled time, skip) + `DialogueView` (needs only a root, input provider, clock) are reusable by S2 cutscenes and the world map; `LevelStoryListener` maps LevelContext events to dialogues. Extend/replace the existing `StoryDefinition` intro beats with dialogue references instead of ignoring it.
7. **Speaker frame style** per `SpeakerDefinition`: Portrait (Mercer, Voss), Hologram (ORA), Transmission (NTI messages: no portrait, header bar, monospace feel), Log (Pioneer-1).
8. **Stable line ids** on every line for a future string table; text box sized for ~25% longer (Dutch) text.
9. **Boss alarm ordering:** if WaveStart and BossSpawn dialogues fire the same frame, BossSpawn wins and WaveStart for that wave is skipped (one stacked box, after the alarm sound starts).
10. **Typewriter/glitch logic is pure** (reveal count from elapsed time, seeded random) and EditMode-tested. PlayMode run-to-end uses an auto-advance test hook.
11. **Text rendering — DECIDED (Remco, 2026-11-03): use TextMeshPro for DialogueView.** Background: TextMeshPro (part of the already-installed `com.unity.ugui` 2.x, gives strikethrough, mark, maxVisibleCharacters, per-character jitter, font fallback) for DialogueView only, versus legacy `Text` + `SegmentOverlay`. TMP Essentials must be imported by code (no editor clicks).
12. Build order: (1) input gates + data types + selector/pure logic with tests, (2) LevelStoryListener + DialoguePlayer + generator step + level-1 content, (3) DialogueView with final frame styles and text effects, (4) 3D bust portraits + hologram/emblem, (5) PlayMode smoke + polish screenshots.
13. **Owner decisions 2026-11-03:** every dialogue freezes the game (hold), also mid-wave; portraits are 3D busts of our own models (build a simple Dr. Voss model; ORA hologram, NTI emblem), replaceable by a drawn sprite via data; "Skip" skips the whole current conversation (later triggers in the level still play); story plays once per save on every difficulty (no Easy replay rule for now).
