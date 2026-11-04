# COMMANDS.md

Commands used to drive Unity from the command line. Verified on 2026-10-14 against Unity CLI `1.0.0-beta.13`,
Editor `6000.6.5f1` and `com.unity.pipeline@0.8.0-exp.1`. The CLI is in beta: run `unity list` to see the current commands.

## Paths (this machine)

| What | Path |
| --- | --- |
| Project | `C:\Users\remco\Tower defense` |
| Unity CLI | `C:\Program Files\Unity Hub\resources\unity.exe` (not on PATH) |
| Editor | `C:\Program Files\Unity\Hub\Editor\6000.6.5f1\Editor\Unity.exe` |
| Git | `C:\Program Files\Git\cmd\git.exe` (not on PATH) |

In PowerShell, set up the session first:

```powershell
$env:Path += ";C:\Program Files\Unity Hub\resources;C:\Program Files\Git\cmd"
```

## Primary route: Unity CLI + Pipeline package (Editor stays open)

Always run from the project root. Add `--no-banner` for clean output and `--result-only` to `unity command` for plain JSON.

| Goal | Command |
| --- | --- |
| Is the Editor connected? | `unity status` |
| Environment check | `unity doctor` |
| List all Pipeline commands | `unity list` |
| Editor state (compiling, play mode) | `unity command editor_status --result-only` |
| Recompile scripts | `unity recompile` (or `unity command recompile`, then poll `recompile_status`) |
| Compile errors / console counts | `unity command console_status --result-only` |
| Read console (errors and warnings) | `unity command console --result-only` |
| Clear console | `unity command clear_console` |
| List tests | `unity command list_tests --result-only` |
| Run tests (EditMode / PlayMode) | `unity test` or `unity command run_tests` (poll `test_status`) |
| Enter / exit play mode | `unity command editor_play` / `unity command editor_stop` |
| Speed up simulation | `unity command set_timescale` |
| Screenshot of Game view | `unity command capture_game_view` (use `save_path` to write a PNG) |
| Run a generator method | `unity command menu "Tools/Generate/<Name>"` (once the menu items exist) |
| Create / open scene | `unity command create_scene` / `unity command open_scene` |
| Save everything | `unity command save_all` |
| Build | `unity build` (or `unity command build`, poll `build_status`) |
| Long-running command | add `--detach` to get a job ID, then `unity job` |

Command arguments differ per command: run `unity command <name> --help` before first use.

Notes:

- `create_*`, `write_text_file` and similar commands are confined to the *authoring root* (currently `Assets`).
- After adding or changing C# files, run `recompile` and wait until `recompile_status` is `completed` and `console_status` shows 0 errors before the next step.
- Let Unity create `.meta` files itself (focus or recompile the Editor), and include them in the commit.

## Fallback: batchmode (Editor must be closed for this project)

```powershell
$unity = "C:\Program Files\Unity\Hub\Editor\6000.6.5f1\Editor\Unity.exe"

# Run an editor script
& $unity -batchmode -quit -projectPath . -executeMethod <Namespace.Class.Method> -logFile -

# EditMode tests (use PlayMode for the other suite)
& $unity -batchmode -projectPath . -runTests -testPlatform EditMode -testResults results.xml -logFile -

# Build: via a BuildScript with -executeMethod (to be added in a later milestone)
```

## Git

```powershell
git status
git log --oneline
```

Commit and push only on request. Commit messages are in English.

## Verified recipes and pitfalls (milestone 1)

Parameters are flags named after the command schema: `unity command --query <name> --detail full` lists them.

| Task | Command |
| --- | --- |
| Run the scene/data generator | `unity command menu --path "Tools/Generate/Scenes and Starter Data"` |
| Run EditMode tests | `unity command run_tests --mode EditMode --timeout 120 --result-only` |
| Open a scene | `unity command open_scene --path Assets/_Project/Scenes/Boot.unity` |
| Delete an asset (also removes `.meta`) | `unity command delete_asset --asset <Assets/...> --confirm true` |
| Run a C# snippet | write it to `Temp/x.cs`, then `unity command eval_file --file Temp/x.cs --result-only` |
| Screenshot | `unity command capture_game_view --save_path <path>` |

Pitfalls:

- **Play Mode does not advance while the Editor window is unfocused** (`Time.frameCount` stays 1, `editor_focus` and `set_autotick` do not help). Step frames by hand: `EditorApplication.isPaused = true; for (...) EditorApplication.Step();` through `eval_file`, then screenshot.
- `capture_game_view --save_path` is resolved inside `Assets/` (`Temp/level.png` becomes `Assets/Temp/level.png`). Read it, then delete it with `delete_asset` so no stray asset or `.meta` is committed.
- Passing C# with spaces through `eval --code` breaks in PowerShell; use `eval_file`.
- `Shader.Find` is unreliable for placeholders; use `GraphicsSettings.currentRenderPipeline.defaultMaterial`.
- Console warnings seen at startup and not caused by the project: Burst blocked by Smart App Control, and Visual Studio UDP port 56630.

## Milestone 2 additions

| Task | Command |
| --- | --- |
| Run PlayMode tests (async, domain reload drops sync calls) | `unity command run_tests --mode PlayMode --async_tests true`, then poll `unity command test_status` |
| Regenerate level 1 data and scenes | `unity command menu --path "Tools/Generate/Scenes and Starter Data"` (idempotent) |

- Open the **Boot** scene before entering Play Mode (`open_scene`). Playing from the Level scene reports "No LevelSession registered" by design.
- `eval_file` runs on the main thread with a 5 second limit: step at most ~100 frames per call and loop in the shell.
- In generator code, `EditorSceneManager.NewScene` unloads assets nothing references yet. Load assets by path after creating the scene, or object references are saved as null.
- Do not name a class after a native Unity type: `DebugSettings` was renamed to `PlaytestSettings` while chasing a null reference (the real cause was the item above, but the rename is harmless).

## Milestone 3 additions

- `capture_game_view` returns a stale image after a scene switch while the Editor is unfocused, even with `RepaintAllViews`. For menu screens, verify structure with an `eval_file` script that lists the `Button` components and their labels instead.
- Saved progress lives in `Application.persistentDataPath/Saves/progress.json` (Windows: `%USERPROFILE%\AppData\LocalLow\<company>\<product>\Saves`). Delete it to reset to a new game.

## Batchmode recipes (Editor closed) - verified 2026-10-14

When the Editor stops answering CLI commands, close it and run (from the project root, PowerShell):

```powershell
$unity = "C:\Program Files\Unity\Hub\Editor\6000.6.5f1\Editor\Unity.exe"
# Generate data and scenes
Start-Process $unity -ArgumentList '-batchmode','-nographics','-projectPath','.','-executeMethod','Game.Editor.ProjectGenerator.GenerateAll','-quit','-logFile','Temp\gen.log' -Wait
# EditMode tests (use PlayMode without -nographics for the other suite)
Start-Process $unity -ArgumentList '-batchmode','-nographics','-projectPath','.','-runTests','-testPlatform','EditMode','-testResults','Temp\results.xml','-logFile','Temp\tests.log' -Wait
```

Do not start tests through the CLI while the Editor is in Play Mode: the run fails and leaves red errors in the console.

## Windows build (added 2026-10-14)

```powershell
# Unity must be closed. Writes Builds/Windows/TowerDefense.exe (git ignores Builds/).
& "C:\Program Files\Unity\Hub\Editor\6000.6.5f1\Editor\Unity.exe" -batchmode -quit -projectPath . -executeMethod Game.Editor.BuildScript.BuildWindows -logFile Logs\build.log
```

The same build is in the editor menu `Tools/Build/Windows Player`. The build starts in borderless full screen at the desktop
resolution (Alt+Enter toggles windowed). Quit from the main menu.

PlayMode tests: the CLI runner reports 0 tests, so close Unity and use `-runTests -testPlatform PlayMode` as above.

## Play-testing through the CLI (added 2026-10-28)

- `eval_file` takes a method body: no `using` lines (they parse as using-statements), so write fully qualified names
  (`Game.Core.Services.Get<Game.Levels.CampaignCatalog>()`). The play-test scripts live in `Tools/PlayTest/` (Unity's
  `Temp/` gets wiped by the generator and test runs): `start_level.cs` (level index from `Tools/PlayTest/level_arg.txt`),
  `towers.cs` (probe towers plus skip wave), `step10.cs`, `inspect.cs` (enemies, effects, audio), `wreck.cs` (wrecks
  every gate), `hero_to_base.cs` / `hero_to_first.cs` (send the hero to its last or first post), `teleport_to_base.cs` (drop every living enemy two waypoints before the road end to watch the siege), `damage_enemies.cs` (hurt all by the share in `damage_arg.txt`), `base_probe.cs` / `base_layout.cs` (base health, blocker, gate positions), `start_level_briefing.cs` + `deploy.cs` (start with briefing and watch the recon fly-over), `footprint_probe.cs` (terrain margins vs camera footprint), `scene_stats.cs` (prop and renderer counts), `camera_probe.cs` (camera focus vs hero), `loadout_probe.cs` (texts on the loadout panel); walk the map with `click_button.cs` ("Start", a difficulty, a weapon name, "Deploy") and `zoom.cs` (close-up on "hero", "gate" or "base" from `zoom_arg.txt`; `zoom_size.txt` is a zoom factor, 10 to 12 frames the hero).
- After `editor_stop`, wait a few seconds and `open_scene` Boot again before `editor_play`, or the first `eval_file`
  calls land in a half-loaded scene and report "no context".
- Stepping many frames inside one `eval_file` call logs `Internal: deleting an allocation that is older than its
  permitted lifetime` warnings (about eight per stepped frame in a level). They come from the Editor stepping without
  its own update in between, not from the game: single steps with real Editor updates between them log nothing.
  Step in batches of ten and judge the console by its error count, or the warning flood pushes errors out of the
  2000-entry buffer (`clear_console` first).
- The GPU Resident Drawer is off on `PC_RPAsset` on purpose: with it on, every level switch logs "A BatchDrawCommand
  was submitted with an invalid Batch, Mesh, or Material ID" for every renderer of the old scene, because the whole
  level is built from runtime meshes and materials that the scene switch unloads.
- `capture_game_view --save_path Temp/x.png` writes `Assets/Temp/x.png`; delete it with `delete_asset` afterwards.
- Never edit a C# file while Play Mode runs: the Editor recompiles and reloads the domain, every runtime object
  loses its state and the console fills with null references. Stop, edit, recompile, play again.
- `Tools/PlayTest/buy_drones.cs` fills the energy pool and launches one drone of each kind; `zoom_drone.cs` zooms on a drone by kind (`zoom_arg.txt`: Repair, Harvest or Attack) and reports what it is doing.
- Drone helpers (2026-10-31): `buy_repair_drones.cs` launches three repair drones, `damage_gates.cs` damages every gate and the base by `damage_arg.txt`, `repair_probe.cs` lists each drone with its activity and repair job, `structures_probe.cs` lists structure health, `drone_showcase.cs` builds a big display model of the kind in `zoom_arg.txt` beside the base and zooms on it (camera `SetView` zoom: bigger is closer, 9 fills the screen with the base), `base_select.cs` selects that drone tile on the upgrade screen.
- `Tools/PlayTest/zoom_spawn.cs` zooms onto the start of path `zoom_arg.txt` (a path index) at `zoom_size.txt` (0 restores the full view).
- `Tools/PlayTest/goto_base.cs` (adds the research and power in `points_arg.txt` first, unsaved until a purchase) and `goto_map.cs` jump to the upgrade screen and the planet map from a Boot play session; `map_step.cs` drives the map (`map_arg.txt`: next, prev, choose, cancel; "cancel" from a region backs out to the planet), `map_probe.cs` reports layer, region, focus, zoom and the visible badges and markers, `map_click.cs` calls the marker click handler for the index in `map_arg.txt`; for a REAL mouse click use `map_marker_screen.cs` (screen position of marker `map_arg.txt`) or `ui_button_screen.cs` (screen position of the active Button labelled `map_arg.txt`) and then `unity command simulate_pointer --x <x> --y <y>` (y from the bottom; aim a few pixels inside a button at the screen edge), `map_hold.cs` simulates holding the right arrow button, `step_frames.cs` advances the paused editor by `frames_arg.txt` frames (needed because Play Mode does not tick while the editor is unfocused); `globe_probe.cs` reports the globe image, texture and camera sizes; `buy_base_weapon.cs` buys base weapon levels (`points_arg.txt`, saves the profile: back up `%USERPROFILE%AppDataocallowdefaultcompanytower defensesavesprogress.json` first) and `base_turrets_probe.cs` lists the base turrets in a level.
- `capture_game_view` returns a 1280x720 picture whatever size the game view has; with the editor laid out as it is (about 1317x519) everything looks stretched vertically by 1.4. judge proportions with `globe_probe.cs` or `screen_probe.cs`, not from the picture.
- `tools/playtest/click_button.cs` clicks a ui button whose label contains `button_arg.txt` (empty text lists every label): walk the menus without a mouse. `open_menu.cs` opens pause / tower / gate / hero / win (`menu_arg.txt`), `select_card.cs` picks a tower card (`card_arg.txt`), `tower_at.cs` builds on a cell (`cell_arg.txt`, "x y").
- `Tools/PlayTest/base_menu.cs` opens the base menu; `base_die.cs` destroys the base (explosion, then the defeat panel).
- `Tools/PlayTest/spawn_enemy.cs` spawns an enemy by asset name from `enemy_arg.txt` ("Roller", "Drowner Water", bosses use the level entrance); `zoom_enemy.cs` zooms on it and freezes time for a screenshot, `resume.cs` thaws; `no_waves.cs` switches the wave timeline off; `inspect_enemy.cs` lists its renderers; `shadows_off.cs` drops its shadows; `goto_menu.cs` returns to the main menu; `sfx_probe.cs` builds the loop sounds in the editor.
- `Tools/PlayTest/menu_probe.cs` lists the main menu stage (asset, cameras, diorama); `menu_creatures.cs` lists every diorama creature with its state; `music_probe.cs` reports the menu music and whether a level score is running.
- `Tools/PlayTest/boss_alarm.cs` raises the boss alarm by hand; `boss_probe.cs` reports the boss marker, approach sounds and the boss (light, pulse, scale); `sfx_voices.cs` lists the sound voices and approach timers; `sliders.cs` lists the volume sliders; `map_pick.cs` presses the first open beacon on the planet map; `time_probe.cs` reports time scale, briefing and countdown; `cutscene_probe.cs` and `cutscene_seek.cs` watch and seek the opening cutscene; `screen_probe.cs` prints the game view size (captures are resized to 1280x720, so circles look squashed when the view is wider than 16:9).
- `Tools/PlayTest/dump_waves.cs` writes every generated wave per level and difficulty to `Tools/PlayTest/waves_dump.txt`; `wave_probe.cs` reports the wave number, countdown and living enemies by kind.
- `Tools/PlayTest/laser_probe.cs` reports the boss laser, beam renderers and hum count (write "kill" in `enemy_arg.txt` to kill every boss first); `drone_probe.cs` reports money and active drones. `laser_now.cs` makes every living boss fire its laser on the next frame.
- The balance bot takes an optional `probe_arg.txt` (`levels=0,1` and `difficulties=1,2` lines) and must run async in PlayMode (see STATUS).
- `Tools/PlayTest/sfx_library_probe.cs` reports the recorded sound library and how many clips each list holds. Since 2026-11-01 the library holds `SoundSet`s with a measured gain and lists the `CreatureVoice` assets; see `Docs/AUDIO.md` for the rules and levels, and the Options screen "Sound test" to audition every set.
- `Tools/PlayTest/look_shots.sh <prefix> [level]` plays a level from Boot, launches drones and a wave, and captures overview, wave, hero and base close-ups into `Assets/Temp/<prefix>_*.png` (delete the folder afterwards); it stops Play Mode at the end. `shader_probe.cs` reports whether `Game/UnitLit` compiled.
- `Tools/PlayTest/hum_probe.cs` reports which clip the boss laser loop plays.
- `Tools/PlayTest/turn_probe.cs` (after `hero_to_base.cs`) steps until the hero arrives and then samples its yaw, leg swing and turn-step blend every three frames, to read the arrival turn as numbers.
- `Tools/PlayTest/countdown_probe.cs` reports the HUD wave line, the countdown line with its colour and the context countdown.
- `Tools/PlayTest/codex_open.cs` opens the codex on the page in `menu_arg.txt` ("recipes" or "enemies") at the row in `card_arg.txt`; `codex_close.cs` closes it. `beam_probe.cs` lists the beam loop voices of the sound player (clip, volume, seconds since the last laser tick).
- `Tools/Audio/Import Approved Clips` (editor menu) cuts the laser charge recording into `Approved/laser_beam_loop.wav` and writes `hero-death.mp3` as `Approved/Hero/hero_death.wav`; run it before the generator when either recording changes.
- `Tools/PlayTest/alarm_probe.cs` reports which clip the boss alarm plays; `ripple_probe.cs` plays an impact ripple at the hero and holds the frame for a screenshot.
- `Tools/PlayTest/hover_probe.cs` builds a tower beside the road and puts the placing cursor on the next cell (zoom with `towers`).
- `Tools/PlayTest/dump_map.cs` prints every level as a text map with its validation errors (run after the generator).
- The generator menu item can take longer than the CLI's 30 s main-thread timeout (five terrain bakes); the error
  "Main thread operation timed out" is harmless, the generator keeps running. Wait for it by running the EditMode
  tests next (they queue behind it), then `clear_console`. Since the terrain skin pass (2026-10-30) a full run takes
  about a minute; pass `--timeout 300` to `unity command menu` and the call waits for it instead of erroring.
- More scripts in `Tools/PlayTest/`: `hero_down.cs` (drains the hero), `hero_revive.cs` (cuts the revive timer),
  `upgrade_gate.cs` (one level on every gate), `upgrade_towers.cs`, `skip_wave.cs`, `towers_all.cs` (one probe of
  every tower asset near the spawn, Editor only), `shots_probe.cs` (lists shots in flight and centres the camera on
  one); `zoom.cs` also takes "towers", "roster", "spawn" and "enemy" and a size from `zoom_size.txt`.
- `Tools/PlayTest/step_n.cs` steps `step_arg.txt` ("frames timescale", e.g. "4 1") for catching a short animation in
  a row of screenshots; `teleport_to_gate.cs` drops every living walker on its route just before the first gate, so
  attacks can be watched without waiting for the walk; `clip_lengths.cs` lists the length of the recorded death,
  explosion and hit clips (how the 4.7 s slime clip was found). `dump_waves.cs` now also prints the kill money per
  wave and the total per difficulty.
- Balance probe: `run_tests --mode PlayMode --filter BalanceProbeTests --async_tests true`, then poll `test_status`
  and read the `[Probe]` line from `console`. The bot builds four towers, then upgrades the cheapest upgrade it can
  afford, then builds more. It still has no drones and does not move the hero.
- `unity recompile` takes no `--result-only`; if a flag is rejected nothing recompiled, and a generator run right after
  uses the old code. Always check `console_status` for `compilationFailed: false` before `menu` or `run_tests`.
- If `test_status` says "running" for minutes while `editor_status` says play mode is stopped, the runner is stuck (it
  happens when a run starts on a compile error or with a modified scene open): `open_scene` Boot, then start again.
- Music loops: drop a generated track in `Assets/_Project/Audio/` as `menu-music.mp3`, `game-music.mp3` or `boss-music.mp3`, run `unity command menu --path "Tools/Audio/Make Music Loops" --timeout 300` (cuts the fades, blends the tail over the head, writes `Approved/Music/*_loop.wav`), then the generator; check with `eval_file --file Tools/PlayTest/loop_probe.cs` (first and last half second against the median, both should be near 1).
- Balance model: `run_tests --mode EditMode --filter BalanceLedgerTests --timeout 300` writes
  `Tools/PlayTest/balance_ledger.txt` (one line per wave: money, bought DPS ground/air, wave HP ground/air, window,
  pressure ground/air; B marks a boss wave). Failures name level, difficulty and wave. Since 2026-11-04 each level ends with
  a "human lens" line: a competent player's firepower over need on plain waves (towers alone), Easy to Psycho, and how much
  of Medium Easy asks (Docs/BALANCE.md, "Easy economy and the human lens").
- Difficulty profiles only: `unity command eval_file --file Tools/PlayTest/apply_difficulties.cs` runs just the generator's
  difficulty step (`BalanceSheet.Difficulties` -> `Data/Difficulty/*.asset`) when the full generator must not run (it
  syncs the Blender exports from ArtSource while a Blender agent works there).
- Difficulties and waves only (2026-11-04): `unity command run_script --file Tools/PlayTest/Scripts/ApplyWaves.cs --entry
  ApplyWaves.Run --timeout_ms 900000 --timeout 960` runs the generator's "Difficulties" and "Waves" steps (profiles, level
  payouts, solved and human-calibrated waves) without the Blender syncs. `eval_file` stops after 5 seconds and an
  `EditorApplication.delayCall` never runs while the editor is unfocused, so long editor jobs go through `run_script`.
- Balance curve without writing anything: `unity command run_script --file Tools/PlayTest/Scripts/BalanceCurve.cs --entry
  BalanceCurve.Run --timeout_ms 900000 --timeout 960` writes `Tools/PlayTest/balance_curve.txt` (per level: human surplus
  towers alone and with the hero, worst wave with the hero, trim against the solver's shape and creature toughness, bot-lens
  peaks against the bands). Try a knob first with `Tools/PlayTest/curve_arg.txt` (`TargetPressure=0.8`,
  `HeroCap=1,1.15,1.25,1.35`, `d2.Income=1.1` for a difficulty field), then put it in code and run ApplyWaves.
- `balance_ledger.txt` ends with the human curve per level (towers alone and with the hero, research assumed). The ledger
  test fails when a level is off its curve, easier than the level before it (ramp), or not beatable with the hero.
- Full bot run (every C1 level on every difficulty): set `Tools/PlayTest/probe_arg.txt` and run
  `--filter "Game.Tests.BalanceProbeTests.Full_GreedyBot_PlaysEveryLevel_WithoutErrors" --include_explicit true`.

## Graphics pass (added 2026-11-01, evening)

- `Tools/PlayTest/look_layers.sh <prefix> [level]` measures the rendered colour of road, ground and hero
  (`pixel_probe.cs`) and switches mist, the ground detail layer and post-processing off one by one
  (`look_toggle.cs`, `look_arg.txt`: mist, detail, fog, post, rim) with a screenshot after each.
- `Tools/PlayTest/liquid_shots.sh <prefix> <level> <Water|Lava|Bog> [zoom] [ortho half height]` zooms on the
  largest pool of a kind (`zoom_liquid.cs`) and captures three frames; `particle_probe.cs` lists the particle
  systems whose name contains `zoom_arg.txt`; `zoom_close.cs` sets the orthographic size directly (the gameplay
  zoom clamps at 4.5), `particle_material_probe.cs` reports how the default particle material blends.
- `Tools/PlayTest/burst_shots.sh <prefix> <level> "<Shell|Rocket|Mortar|Mine|Barrel|BossPulse|Death> <radius>" ...`
  plays one impact effect after another at the hero (`burst_probe.cs`) in slow motion and captures three frames
  of each, both as `--source camera` (no post-processing, for structure) and as the screen (`_screen.png`, with
  bloom). `burst_inspect.cs` lists the active ImpactFlash objects. `time_free.cs` restores the time scale.
- `Tools/PlayTest/showcase_shots.sh <prefix> [level] [probe] [ortho half height] [focus]` runs
  `tower_showcase.cs` (every tower at level 0 and 2 in a row along the main route) and captures it;
  `roster_shots.sh` does the same with `towers_all.cs`. `enemy_shots.sh <prefix> <level> <zoom> <Name> ...`
  spawns creatures at the gate (the hero is sent to the base first) and captures frozen close-ups;
  `STEPS=4` in the environment shortens the walk for creatures that die fast near the gate.
- `Tools/PlayTest/profile_shots.sh <prefix> <level> <high|low>` captures a level under a quality profile;
  `low_profile.cs` registers the Low (Switch) settings and switches the tilt-shift pass off; `renderer_probe.cs`
  lists the renderer features and the tilt-shift material.
- Pitfalls: the unfocused editor runs one long frame (up to a third of a second) between two CLI calls, so a
  short effect is gone before the next capture; `burst_probe.cs` therefore slows time to 0.05 and
  `time_free.cs` restores it (`Time.captureDeltaTime` does not help with `EditorApplication.Step`).
  `capture_game_view --source camera` renders the camera fresh without post-processing and overlay UI.
  Never edit a C# file between a recompile and `editor_play`: Unity recompiles when play starts and reloads
  the domain mid-play (hundreds of NullReferenceExceptions, "no context" from every probe).
- The URP depth of field (Gaussian and Bokeh) does not work with the orthographic camera; the tilt-shift is the
  `Game/TiltShift` full-screen pass on `PC_Renderer` (material `Art/Materials/TiltShift.mat`: band, feather,
  radius), created by the generator and switched by `Bootstrapper` from `IPlatformSettings.DepthOfFieldEnabled`.
- Planet and screens (2026-11-01 late): `Tools/PlayTest/map_shot.sh <prefix>` captures the region view, backs out
  to the planet (`map_step.cs` cancel) and captures that; `base_shot.sh <prefix> [Repair|Harvest|Attack ...]`
  opens the upgrade screen and selects drone tiles. The planet maps are baked by the generator into
  `Art/Planet/*.png` (`ProjectGenerator.Planet.cs`, `GlobePainter`) and referenced by the `CampaignCatalog`;
  painting them live takes over five seconds and trips the CLI main-thread timeout.
- Motion and aim (2026-11-01 night): `Tools/PlayTest/motion_shots.sh <prefix> <level> <zoom> <Name> [gap]` spawns a
  creature at the hero's gate and captures a run of close-ups of the hero and of the creature a few frames apart
  (the unfocused editor steps about half a second per frame, so a run shows poses, not smooth motion);
  `aim_shots.sh <prefix> <level> <zoom> <Name>` does the same in slow motion (`set_timescale --scale 0.1`) and
  prints `aim_probe.cs`: the angle between the drawn weapon's barrel and the line to the commander's target
  (a few degrees, the kick of a shot and the aim wobble, is right). A bat forces the blaster, as the sword
  cannot reach the air; a boss kills the hero within seconds, so use a Skitter or a bat for hero shots.

## Balance model outside Unity (BalanceLab)

The pure balance model (`Assets/_Project/Scripts/Balance`) also runs as a .NET 8 console tool, with or without the
Editor open. It reads the generated assets and the wave scripts. Docs/BALANCE.md explains the model.

| Goal | Command |
| --- | --- |
| Judge the assets as they are | `dotnet run --project Tools/BalanceLab -- report` (writes `Tools/PlayTest/balance_ledger.txt`) |
| Solve the waves from the scripts and the balance sheet | `dotnet run --project Tools/BalanceLab -- solve` |
| Solve and write the result into the existing assets | `dotnet run --project Tools/BalanceLab -- write` |
| Show one solved wave | `dotnet run --project Tools/BalanceLab -- dump Landing 1 12` (level key, difficulty 0-3, wave) |
| Prove the writer (must leave `git diff` empty) | `dotnet run --project Tools/BalanceLab -- roundtrip` |

`write` only rewrites existing files (same GUIDs); a change in the number of waves needs the Unity generator.

## Opening cutscene (added 2026-11-03)

- `Tools/PlayTest/cut_shots.sh <prefix> "<shot> <progress>" ...` plays the cutscene from Boot, seeks each moment (shot index from 0) and captures `Assets/Temp/<prefix>_*.png`; write `0.02` into `Tools/PlayTest/cut_speed.txt` first to freeze time for stills (delete it afterwards). `cutscene_probe.cs` reports shot, time, dialogue, fade; `cutscene_advance.cs` presses confirm.
- Letting the cutscene run to its end marks the intro seen in the real save: back up and restore `progress.json`.

## Campaign structure (added 2026-11-03, S3)

- PlayMode smoke runs (default): `unity command run_tests --mode PlayMode --filter ContinentSimulationTests --async_tests true`
  (each level of every filled continent on Easy) and `--filter BalanceProbeTests` (greedy bot, first and last level on
  Medium). The full runs (every difficulty) are `[Explicit]` with `[Category("Full")]`: run them by name, e.g.
  `--filter "Game.Tests.ContinentSimulationTests.Full_EveryLevelOnEveryDifficulty_RunsWithoutErrors"`.
- The generator caches terrain bakes in `Library/TerrainBakeCache.txt` (hash of the level's map, its theme and the
  painter sources); delete the file to force a full rebake. An unchanged run takes about 20 s instead of 2 to 3 minutes.
- `Tools/PlayTest/dump_map.cs` prints the campaign levels (`Level_C1_01` ...) with their validator errors.
- BalanceLab builds from `Tools/BalanceLab/BalanceLab.csproj` (git-ignored, so recreate it on a fresh clone): it
  compiles the tool, `Scripts/Balance/**`, `Editor/BalanceSheet.cs`, `Editor/WaveScripts.cs` and
  `Editor/LevelSpecs/*.cs`. No dotnet on PATH here: use Unity's
  `& "C:\Program Files\Unity\Hub\Editor\6000.6.5f1\Editor\Data\DotNetSdk\dotnet.exe" run --project Tools/BalanceLab -- report`.
  It overwrites `Tools/PlayTest/balance_ledger.txt` with system-culture numbers and differs slightly from Unity (Mono
  keeps float intermediates in double precision; see STATUS), so restore the file or rerun `BalanceLedgerTests` after.
- `Tools/PlayTest/rugged_probe.cs` prints the globe ruggedness under every Crystal Plains site and a coarse ridge map;
  `float_probe.cs` shows whether the runtime keeps float intermediates in double precision.

## Balance probe trace (added 2026-11-03)

- `BalanceProbeTests` writes `Logs/probe_trace.txt`: per wave the money earned, unspent money, base, hero state, creatures
  alive and where (origin, distance walked), base hits by species, flyers shot down, towers with their levels and raw
  damage per second, and the ledger's line for the same wave. Read it after a lost probe run before touching numbers.

## Blender model import (added 2026-11-04, import stage 1)

The Blender scripts write to `ArtSource/Exports/` (git-ignored); Unity never reads that folder directly. The import:

| Goal | Command |
| --- | --- |
| Copy changed exports into `Assets/_Project/Art/Models/Blender/` | `unity command menu --path "Tools/Generate/Sync Blender Models" --timeout 600` |
| Sync, rebuild materials and art data, point the enemies at them | `unity command menu --path "Tools/Generate/Blender Creatures (sync, materials, enemies)" --timeout 600` |
| Everything (also part of the full generator, step "Creature art") | `unity command menu --path "Tools/Generate/Scenes and Starter Data" --timeout 600` |
| Recompile + creature art in one go (Play Mode must be stopped) | `bash Tools/PlayTest/imp_regen.sh` |
| List every art definition with clips, distances, moments | `eval_file --file Tools/PlayTest/imp_art_probe.cs`, then read `Tools/PlayTest/imp_art_probe.txt` |
| List the bones, meshes and clips of one imported FBX | write the asset name to `Tools/PlayTest/imp_arg.txt`, `eval_file --file Tools/PlayTest/imp_inspect.cs`, read `imp_inspect.txt` |
| Performance check (densest wave of C1_06 and C1_05, High and Low) | `run_tests --mode PlayMode --filter ModelPerformanceTests --async_tests true`; numbers in `Logs/model_perf.txt` and the test output |

- **What gets copied** (`BlenderSync`): per asset the FBX, `_LOD1.fbx`, the four 1024 maps, the `Low/` 512 maps, baked skins
  under `Skins/<Name>/` and the report; plus `Exports/Detail/Detail_*_Normal.png`. A file is copied only when its size or
  write time differs, so a rerun with unchanged exports copies nothing. Without `ArtSource/` the copies in `Assets` stay.
- **Import settings** (`BlenderModelImporter`, applies to everything under `Art/Models/Blender/`): scale 1 with file units,
  Bake Axis Conversion, Generic rig, imported normals with Mikktspace tangents, no materials, scale curves kept, takes
  renamed from `Rig|Clip` to `Clip` and the cycles looped (`BlenderArtCatalog.IsLoop`); the far LOD's mesh node loses
  its `_LOD1` suffix (`_Far`) so Unity stops warning about LOD naming. Normal maps as normal maps, Mask and detail
  normals linear, Albedo and Emission sRGB, detail normals tiling with aniso 4. Bump `GetVersion()` after changing it.
- **Which model an enemy wears** is `BlenderArtCatalog.EnemyArt`; scales, pivots, power frames and the look per material
  (emission, smoothness, detail strength, rim, brightness) are the rows in `BlenderArtCatalog.CreateCreatures`. The
  generated data: `Data/Art/<Id>.asset` (`ModelArtDefinition`), `Art/Materials/Blender/<Id>.mat` and `<Id>_Low.mat`.
- A new creature: add its export folder name to a row (and the enemy to `EnemyArt`), run the creature menu item.
- Screenshots of creatures at play size: `Tools/PlayTest/enemy_shots.sh <prefix> <level> <ortho half height> <Enemy> ...`.

### Towers (added 2026-11-04, import stage 2)

| Goal | Command |
| --- | --- |
| Sync the tower exports, rebuild their materials, tier art and tower art, point the towers at them | `unity command menu --path "Tools/Generate/Blender Towers (sync, materials, towers)" --timeout 900` (also the full generator's step "Tower art") |
| Recompile + tower art in one go (Play Mode must be stopped) | `bash Tools/PlayTest/imp2_regen.sh` |
| Recompile only, with the error and warning count | `bash Tools/PlayTest/imp2_compile.sh` |
| Bones (with world rest pose), meshes and clips of one tower tier | write `Tower_Gun/Tower_Gun_T2` to `Tools/PlayTest/imp_arg.txt`, `eval_file --file Tools/PlayTest/imp2_inspect.cs`, read `imp2_inspect.txt` |
| Screenshots: every tower at level 1 and 3 firing, close-up, damage smoke, wreck | `SAVE_BACKUP=<backup> SHOTS=<folder> bash Tools/PlayTest/imp2_shots.sh <prefix> [level] [road index]` (pictures in `Assets/Temp/`) |

- **Which model a tower wears** is `BlenderTowerCatalog` (tower id, export, tiers, aim, shot-origin bones, wreck debris,
  look). The generated data: `Data/Art/Towers/Tower_<Name>.asset` (`TowerArtDefinition`: level and path per tier),
  `Data/Art/Towers/Tower_<Name>_<Tier>.asset` (`ModelArtDefinition` per tier), `Tower_Wreck.asset`, and the materials
  `Art/Materials/Blender/Tower_<Name>.mat` and `_Low.mat`. A tower without `Art` keeps its procedural model.
- The tier FBX files are synced by name (`BlenderSync.Sync(assets, extraModels)`); each holds the near mesh and the
  far one (renamed `_Far` at import) on one rig, so a LOD group picks one and one Animator drives both.
- Each Unity CLI command answered in about a minute on 2026-11-04 (the editor was slow to reply all session); give
  long timeouts and avoid `unity command console` on a full console (it can run for minutes): use `console_status`.

### Our own side: gate, base, drones, path objects, resource node (added 2026-11-04, import stage 3a)

| Goal | Command |
| --- | --- |
| Sync the colony exports, rebuild their materials and art, point the gate towers, visual themes, drones and path objects at them | `unity command menu --path "Tools/Generate/Blender Colony (sync, gate, base, drones, gadgets)" --timeout 600` (also the full generator's step "Colony art") |
| Recompile + colony art in one go (Play Mode must be stopped) | `bash Tools/PlayTest/imp3_regen.sh` |
| Node tree (renderers, bones with local position) and clips of FBX files | list the asset paths in `Tools/PlayTest/imp3_inspect.txt`, `eval_file --file Tools/PlayTest/imp3_inspect.cs` |
| World positions and axes of named sockets | asset paths, then a last line with the comma-separated node names, in `imp3_inspect.txt`; `eval_file --file Tools/PlayTest/imp3_sockets.cs` |
| Screenshots: base and gate at play zoom and close, gate hurt/collapse/wreck/rebuilt/level 3, the ore vein full/half/empty, drones at work and in a showcase, base hurt and wreck | `SAVE_BACKUP=<backup> SHOTS=<folder> bash Tools/PlayTest/imp3_shots.sh <prefix> [level] [road cell]` |
| Screenshots: every path object in the data placed along the road (deploy, close, mid, play zoom, after rolling) | `SAVE_BACKUP=<backup> SHOTS=<folder> bash Tools/PlayTest/imp3_gadgets.sh <prefix> [level] [road cell]` |

- **Which model an owner wears** is `BlenderColonyCatalog` (gate levels and wreck, base overlay and wreck, the model per
  drone kind and per path object look, the clip roles by take name). Generated: `Data/Art/Colony/*.asset`
  (`ModelArtDefinition` per FBX, `GateArt.asset`, `BaseArt.asset`) and `Art/Materials/Blender/<Export>.mat` / `_Low.mat`.
  Owners: `TowerDefinition.GateArt` (every gate), `VisualTheme.BaseArt` and `ResourceNodeArt` (every theme),
  `DroneDefinition.Art`, `PathObjectDefinition.Art`. Without art each keeps its procedural look.
- The exports face **-Z** once imported (the README's "faces -Y in Blender (Unity +Z)" is the other way round): every
  colony `ModelArtDefinition` turns its model by `Yaw` 180. `ColonyArtTests` checks the gate's hero post, the base's door
  and the attack drone's guns end up on the right side.
- `BlenderSync` takes an export in a sub folder (`Env/ResourceNode`): files are named after the last part.
- `imp3_phase.cs` holds the level paused between phases like `imp2_phase.cs`; "base 0" ends the level (defeat), so it
  is the last phase of a run, and the save is restored from `SAVE_BACKUP` afterwards.

### Hero, story busts and cutscene models (added 2026-11-04, import stage 3b)

| Goal | Command |
| --- | --- |
| Sync Mercer (and his weapons), the busts and the cutscene machines and drones, rebuild their materials and art, point the hero, the speakers and the cutscene cast at them | `unity command menu --path "Tools/Generate/Blender Story (sync, hero, portraits, cutscene)" --timeout 600` (also the full generator's step "Story art", after "Cutscene") |
| Screenshots: Mercer idle (play zoom and close), running, shooting, slashing, the shockwave, a fight at play zoom, down and getting up | `SAVE_BACKUP=<backup> SHOTS=<folder> bash Tools/PlayTest/imp3b_hero.sh <prefix> [level]` (phases in `imp3b_phase.cs`) |
| Cutscene stills without lag: the editor is paused, the cutscene sought and the camera rendered at 1920x1080 in one call | `CUT_OUT=<windows folder, C:/...> bash Tools/PlayTest/imp3b_cut.sh <prefix> "<shot> <progress>" ...` (`imp3b_still.cs`) |
| Portraits: the dialogue box per speaker plus the bust PNG | `PORTRAITS=<windows folder> bash Tools/PlayTest/dialogue_shots.sh mercer ora voss nti pioneer`, then restore the save |
| Sync the stage 3b exports only (before the builder runs) | `eval_file --file Tools/PlayTest/imp3b_sync.cs`; world positions of the named nodes in the bind pose: `imp3b_nodes.cs` (FBX paths in `imp3_inspect.txt`) |

- **What wears what** is `BlenderStoryCatalog` (the take-to-role table, the upper-body clips, the report keys of the
  moments and gait distances, the bust framing per speaker). Generated: `Data/Art/Story/*.asset` (`ModelArtDefinition`
  per FBX, `MercerArt.asset` = `HeroArtDefinition`, `Portrait_<Asset>.asset` = `PortraitArtDefinition`) and
  `Art/Materials/Blender/<Export>.mat` / `_Low.mat`. Owners: `HeroDefinition.Art`, `SpeakerDefinition.PortraitArt` (not
  the hero: his bust is `HeroArtDefinition.Bust`), `CutsceneCast.*Art`. `BlenderColonyBuilder` now also writes
  `BaseArtDefinition.TimelapseOrder` from the base report for the cutscene.
- The cutscene poses its models with `ArtModel.Sample` (`AnimationClip.SampleAnimation`), never a clip player, so a
  sought moment equals a played one and nothing needs disposing. The level hero uses the `ModelClipPlayer` with its new
  upper-body layer (`ModelArtDefinition.UpperBodyBone`, `ModelClip.UpperBody`).
- `cut_shots.sh`/`cut_stills.sh` capture a moment a second or more after the seek (the cutscene keeps playing between CLI
  calls); use `imp3b_cut.sh` for exact frames. Its output path must be a Windows path (`C:/...`), not `/c/...`.

### Level environment: ground sets, kit, decor (added 2026-11-04, import stage 4a)

| Goal | Command |
| --- | --- |
| Sync the environment exports (tiling sets, kit, decor, origins), rebuild their materials and the `EnvironmentSet` per continent, point every campaign theme at its continent's set | `unity command menu --path "Tools/Generate/Blender Environment (sync, ground sets, kit, decor)" --timeout 900` (also the full generator's step "Environment art", after "Campaign levels") |
| Recompile + environment in one go (Play Mode must be stopped) | `bash Tools/PlayTest/imp4_regen.sh` |
| Screenshots per level: overview, play zoom, road edge, plaza, bank, nest, border, base, gate, back wall, corner (towers by the road, creatures along it) | `SAVE_BACKUP=<backup> SHOTS=<folder> bash Tools/PlayTest/imp4_shots.sh <prefix> <level index> [...]` (phases in `imp4_phase.cs`); `LOW=1` shoots the Low (Switch) profile |
| Set tunables of the Crystal Plains set (float or Vector2 fields, nested as `_ground._glow`) | write `field value` lines to `Tools/PlayTest/imp4_tune.txt`, `eval_file --file Tools/PlayTest/imp4_tune.cs` |
| The set's meshes (bounds, vertex colours, decor scales) | `eval_file --file Tools/PlayTest/imp4_inspect.cs`; node transforms inside env FBX files: `imp4_nodes.cs` |
| Reimport only the environment FBX files (after an importer change that touches only them) | `eval_file --file Tools/PlayTest/imp4_reimport.cs` |
| Text map with rifts marked `R` | `eval_file --file Tools/PlayTest/imp4_map.cs` |

- **What is placed where** is `EnvironmentLayout` (pure rules, tested by `EnvironmentArtTests`): kerbs on every open road
  side (end caps beside gates, plaza joins), corners per grid point, pads on every `BuildGrid.CanBuild` cell, plinths
  under gates, banks round pools, terrace steps round raised cells, rift pieces on one-wide straight fissures, cliff walls
  behind the field (back and the far 75 % of the flanks, openings at road ends and the base). Which export is which piece
  is `BlenderEnvironmentCatalog` (one row per continent). Generated: `Data/Art/Environment/CrystalPlains.asset`,
  `Data/Art/Environment/Meshes/*_Mirrored.asset`, `Art/Materials/Blender/Env/<Export>.mat` / `_Low.mat`.
- The imported env meshes keep the axis change on the FBX node (Blender Z up as a 90 degree X turn), so each
  `EnvironmentMesh` stores the node matrix and the instancer puts it under every placement. After the change Blender -Y
  is Unity -Z: an edge piece has its road on local -Z (the README's `LookRotation(-side)` is the other way round).
- Round 8's flora FBX files carry six colour sets with `Sway` last; Unity only imports the first, so
  `BlenderModelImporter.OnPostprocessModel` rebuilds the sway colours from the shape when red does not follow the height
  (`EnvironmentSway`); every other env mesh drops its helper colour sets.
- The set's tunables (paint tint, height blend, sharpness, scatter, far decor dimming, wind) are data: the builder does
  not overwrite them, so tweaks in the asset survive a regeneration.

### Continents 2-6, future creatures, build spots (added 2026-11-04, import stage 4b)

| Goal | Command |
| --- | --- |
| Creature art incl. the P3 species, the continent bosses (two materials), Gravemaw and the `<Id>_Corrupted` skins | `unity command menu --path "Tools/Generate/Blender Creatures (sync, materials, enemies)" --timeout 900` |
| Environment sets for all six continents (only C1 is put on themes; C2-6 have no levels yet) | `bash Tools/PlayTest/imp4_regen.sh` (same menu as stage 4a) |
| One look per continent set on a continent-1 level (theme swapped in memory by `imp4b_swap.cs`, put back after each set; nothing saved) | `SAVE_BACKUP=<backup> SHOTS=<folder> bash Tools/PlayTest/imp4b_env.sh <prefix> <level index> AshenReach NorthernCrown Mirewood SouthernCrown TheCore` |
| Build spots at rest, while placing, close up (`imp4_phase.cs place` now also puts the cursor on a free cell by the road focus) | `SAVE_BACKUP=<backup> SHOTS=<folder> bash Tools/PlayTest/imp4b_pads.sh <prefix> <level index>` |
| A Shardback losing its armour (shed prism clusters) | `eval_file --file Tools/PlayTest/imp4b_creatures.cs` via `bash Tools/PlayTest/imp4b_creature_shots.sh <prefix> <level index>` |

- A two-material model (`<Asset>_<Second>_*.png`, e.g. `Gravemaw_Crystal_*`) gets `ModelArtDefinition.SecondMaterial`
  (+ Low); `BlenderSync` copies those maps, `CreatureModel` wears both slots on a mesh with two sub-meshes and drives
  flash, frost and glow on both. The report's detail blocks are split body first, second slot after.
- Round 17 shares the build pad, gate plinth, nest shells, eggs and spawn openings with Crystal Plains; the kerb ends,
  plaza joins and plaza edges are the continent's continuous straight kerb, a plaza corner its inner corner stone.
  Terraces, rifts and silhouettes do not exist for C2-6 and are left out on purpose (`EnvironmentArtTests`).
- Build pads: `EnvironmentSet.ShowBuildPads` is off, so no tile is drawn at rest; while a tower is placed
  `BuildPadHighlight` feeds the ground shader (`PlacingGlow` in `EnvironmentGround.shader`) the free-cell mask and the
  cursor, and the free cells glow round the cursor (`PlacingColour`, `PlacingGlow`, `PlacingRadiusCells` on the set).

## Blender: MPFB 2 (MakeHuman for Blender) for human characters (added 2026-11-04)

Approved by Remco 2026-11-04 as the base for human characters (GPLv3 code, CC0 assets and output). Installed into
Blender 5.2.3 as an extension from the official extensions.blender.org repository (version 2.0.18 at install time):

```
# Install and enable from extensions.blender.org (needs --online-mode headless, else "Online access required")
"C:\Program Files\Blender Foundation\Blender 5.2\blender.exe" --online-mode --command extension install -s -e mpfb
```

Asset packs (all CC0, from the official makehumancommunity file server) are loaded with MPFB's own operator
`bpy.ops.mpfb.load_pack(filepath=<zip>)`, which extracts them into
`%APPDATA%\Blender Foundation\Blender\5.2\extensions\.user\blender_org\mpfb\data`:

```
curl -L -f -O https://files.makehumancommunity.org/asset_packs/makehuman_system_assets/makehuman_system_assets_cc0.zip
curl -L -f -O https://files.makehumancommunity.org/asset_packs/hair01/hair01_cc0.zip
curl -L -f -O https://files.makehumancommunity.org/functional/faceunits01.zip
# install_packs.py: import addon_utils; addon_utils.enable("bl_ext.blender_org.mpfb"); bpy.ops.mpfb.load_pack(filepath=z) per zip
blender --background --factory-startup --python install_packs.py -- <zip> <zip> ...
```

Scripts that need MPFB enable it themselves (`addon_utils.enable("bl_ext.blender_org.mpfb")`), so they still run with
`--factory-startup`. Python API: `bl_ext.blender_org.mpfb.services.humanservice.HumanService.create_human()`,
`add_builtin_rig`, `add_mhclo_asset`, `set_character_skin`; targets via `TargetService`.

### Planet S-69-CRYS, mothership v2, Voss v3 (added 2026-11-04)

| Goal | Command |
| --- | --- |
| Sync the planet maps and wire them to the campaign | `unity command menu --path "Tools/Generate/Blender Planet (sync, S-69-CRYS maps)" --timeout 600` (also the generator step "Planet art") |
| Targeted import without the full generator (mothership maps reimported at 2048, mothership + Voss rebuilt, planet synced) | `eval_file --file Tools/PlayTest/fb2_import.cs` (times out the CLI after 5 s and keeps running; wait for `Data/Art/Planet/Planet_S69.asset`) |
| Rebuild only some story exports | `Game.Editor.BlenderStoryBuilder.RebuildOnly("<Export>", ...)` from an eval script |
| Set the planet look tunables | `field value` lines (`_seaLift 1.8`, `_regionRim 0.6`, ...) in `Tools/PlayTest/fb2_set_tune.txt`, `eval_file --file Tools/PlayTest/fb2_set_tune.cs` |
| Screenshots: map per region, region zoom, level, survey/orbit, mothership close-ups | `OUT=<windows folder> bash Tools/PlayTest/fb2_shots.sh <prefix> [map\|cut\|all]` |
| PlayMode test classes one at a time | `bash Tools/PlayTest/fb2_playmode.sh ContinentSimulationTests StoryCrystalTests ...` |

- For exact cutscene stills use `imp3b_cut.sh` (paused seek and render in one call); `fb2_shots.sh` cut stills can land a
  second late, mid-fade. `fb2_still.cs` is the same with the `fb1_tune.txt` shader lines applied.

### Effects from Blender and the painted planet layout (added 2026-11-04, VFX round)

| Goal | Command |
| --- | --- |
| Sync `ArtSource/Exports/VFX`, rebuild the effect materials (High + `_Low`) and sheets, rewrite the impact and weapon recipes and the library | `unity command menu --path "Tools/Generate/Blender VFX (sync, materials, effect library)" --timeout 600` (also the full generator's step "Effect library") |
| Put the painted planet layout on the campaign (continent radii, Crystal Plains sites from `PlanetLayout`, globe rebake) without the full generator | `unity command menu --path "Tools/Generate/Planet Layout (continents, level sites)" --timeout 300` |
| Screenshots: tower lineup firing at creatures, single impacts in slow motion, a boss killed at the lineup (chain explosion) | `SAVE_BACKUP=<backup> SHOTS=<folder> bash Tools/PlayTest/vfx_shots.sh <prefix> [level] [road index] [boss asset]` (phases in `vfx_phase.cs`: `burst`, `boss`, `wave`, `time`, `probe`) |

- Which texture becomes which material is `BlenderVfxCatalog` (shader kind and the colour per packed channel); the atlas
  layout comes from `VFX_report.json`. Generated: `Data/Vfx/Sheets/<Name>.asset` (`VfxSheet`),
  `Art/Materials/Vfx/<Name>.mat` / `_Low.mat`. The recipes are `ProjectGenerator.Vfx` (`VfxSpecs`, `WeaponSpecs`):
  `Data/Vfx/Vfx_<Impact>.asset` and `Data/Vfx/Weapon_<Family>.asset`, mapped per `ProjectileStyle` in `VfxLibrary`.
- Shaders: `Game/VfxParticle` (premultiplied; additive or covering per material, flipbook frame blending through the
  UV2/AnimBlend streams, soft particles against the orthographic depth, fixed atlas cell for cards and lines, a scrolling
  noise layer), `Game/VfxSmoke` (six-way lighting from the main light, temperature through a fire ramp times the heat
  curve in Custom1.x), `Game/VfxDistortion` (opaque-texture refraction; High only).

### Liquids, mist, spawn origins and route gates (added 2026-11-04, liquids round)

| Goal | Command |
| --- | --- |
| Sync `ArtSource/Exports/Liquids`, build the `Game/Liquid` materials (High + `_Low`), the liquid set and the mist, put them on every level theme | `unity command menu --path "Tools/Generate/Blender Liquids (sync, materials, mist)" --timeout 600` (also the generator step "Liquid art") |
| Sync the living origins (`Env/Origin2_*`) and `SpawnGate_Ground/Air`, build their art and `Data/Art/Origins/SpawnOrigins_CrystalPlains.asset`, point every level at it | `unity command menu --path "Tools/Generate/Blender Origins (sync, spawn origins, route gates)" --timeout 600` (generator step "Origin art", after "Effect library") |
| Swept banks (`BankSweep_A/B/C`, `BankAccent` per continent) | part of the environment menu / step "Environment art" |
| Screenshots: the field, the largest water and lava pool at play zoom and close (every other water pool is turned to lava in memory, restored after) | `SAVE_BACKUP=<backup> SHOTS=<folder> bash Tools/PlayTest/liq_shots.sh <prefix> <level index>`; `LOW=1` for the Low profile |
| Screenshots: an origin marked at wave 0, the gates, then a wave with nest creatures: tremor, burst, open maw, spawning | `SAVE_BACKUP=<backup> SHOTS=<folder> bash Tools/PlayTest/origin_shots.sh <prefix> <level index> <wave> <seconds before the opening>` (C1_01: wave 8, 16 s) |
| Which waves send creatures out of a nest, per level | `eval_file --file Tools/PlayTest/origin_waves.cs` |
| Set origin tunables (`_closeDelay 3`, `_openLead 2.5`, ...) | lines in `Tools/PlayTest/origin_tune.txt`, `eval_file --file Tools/PlayTest/origin_tune.cs` |
| Whether the liquid and mist shaders compiled | `eval_file --file Tools/PlayTest/liq_shader_probe.cs` |

- Phases in `liq_phase.cs`: `swap`, `view <kind|all|origin|gate>`, `waves off`, `wave <n>` (starts it, controller held),
  `simulate <s>` (runs the wave logic without frames), `stepto <origin|busy> <state> <s>` (steps real frames), `probe`.
- `LevelController` holds a nest spawn until its origin is open (the maw is up 2.53 s after the opening starts); a wave
  called early opens them at once.

### Menus from Blender (added 2026-11-04, menu round)

| Goal | Command |
| --- | --- |
| Sync the menu island + hive mouth, build their materials and `MenuDioramaArt` from `Menu_Island_layout.json`, hand it to the menu stage | `unity command menu --path "Tools/Generate/Blender Menu Diorama (sync, island, props, pools)" --timeout 600` (generator step "Menu art") |
| Sync the upgrade screen's pedestal, halo and backdrop into `DisplayStageArt` on the UI skin | `unity command menu --path "Tools/Generate/Blender Display Stage (sync, pedestal, halo, backdrop)" --timeout 600` (generator step "Display stage art") |
| Main menu stills (1920x1080, three moments) | `OUT=<windows folder> bash Tools/PlayTest/menu_shots.sh <prefix>` |
| Upgrade screen with subjects selected, plus a preview PNG and an imported/built audit of every subject | `OUT=<windows folder> bash Tools/PlayTest/upgrade_shots.sh <prefix> Cannon Mercer Repair Base` (pictures of the previews in `$OUT/previews`) |
| Menu camera onto a point (`zoom_arg.txt`: x y z distance pitch yaw), lists the pools and banks | `eval_file --file Tools/PlayTest/menu_zoom.cs` |
| Menu art, height map, ground under each menu creature, display stage nodes | `menu_art_probe.cs`, `menu_ground_probe.cs` (in play), `display_probe.cs` |
