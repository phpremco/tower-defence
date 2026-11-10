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
- The probe is deterministic since 2026-11-05 (seeded draws, fixed 0.2 s game steps). `Tools/PlayTest/probe_arg.txt` may add
  `seed=N` and `step=S` (also read by the explicit `Full` test with `levels=`/`difficulties=`); each run appends a line to
  `Logs/probe_results.txt`. Rerun a failure with its seed to reproduce it exactly.
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

## Night 1 fixes (added 2026-11-04)

| Goal | Command |
| --- | --- |
| Put the colony drill rig on the Crystal Plains set | `unity command menu --path "Tools/Generate/Colony Rig (drill beside every Crystal Plains level)" --timeout 120` (also generator step "Colony rig") |
| Where each level's rig stands | `eval_file --file Tools/PlayTest/rig_probe.cs` |
| Developer mode data flag on / off | `eval_file --file Tools/PlayTest/dev_mode_on.cs` (`Tools/PlayTest/dev_arg.txt`: `off` to turn it off); a development build also takes `-devmode` |
| Level shots with pointer clicks | `SAVE_BACKUP=<backup> SHOTS=<folder> bash Tools/PlayTest/n1_shots.sh <prefix> <level index or none> ...` (adds `arg:<file>=<value>` and `pointer:<move|click>` at `n1_pick_xy.txt` to round_ui_shots' actions) |
| Origins staying open after their first wave | `SAVE_BACKUP=<backup> SHOTS=<folder> bash Tools/PlayTest/n1_origin.sh <prefix> 0 8 16` |

## Night 2: opening cutscene and round-26 art (added 2026-11-04)

| Goal | Command |
| --- | --- |
| Sync the cave kit, the Burrower eyes, the Earth maps (gloss baked from the mask), the cutscene sprites and the theme skies, and hand them to the cutscene cast and the themes | `unity command menu --path "Tools/Generate/Blender Cutscene (cave, eyes, Earth, FX, skies)" --timeout 600` (also generator step "Cutscene art", after "Story art") |
| Regenerate only the opening cutscene data (shots, cues, cast) and the story/cutscene art hand-outs, without the full generator | `unity command run_script --file Tools/PlayTest/Scripts/ApplyCutscene.cs --entry ApplyCutscene.Run --timeout_ms 600000 --timeout 660` |
| Cutscene stills, probes and close inspections in one play session | `CUT_OUT=<C:/...> bash Tools/PlayTest/n2_cut.sh <prefix> "<shot> <progress>" "look:<shot> <progress>\|<place> px py pz lx ly lz fov" probe:<file.cs> ...` |
| Probes (run inside `n2_cut.sh`) | `n2_layout_probe.cs` (field layout: road, gate, dig site, guard post), `n2_eye_probe.cs` (seated eye pairs), `n2_build_probe.cs` (every base module and builder in the timelapse), `n2_hand_probe.cs` (Mercer's hand and head), `n2_cam_probe.cs` (camera and Mercer) |
| Inspect imported cave pieces and eyes (orientation, rig colours) | `eval_file --file Tools/PlayTest/n2_cave_probe.cs`; the drill's Sink/Spin clips: `n2_drill_probe.cs` |
| Set float fields on a data asset | first line of `Tools/PlayTest/n2_set.txt` the asset path, then `field value` lines; `eval_file --file Tools/PlayTest/n2_set.cs` |

- **Stills of skinned models are one pose late** when the paused editor renders right after a seek: skinning runs in the
  player loop. `n2_still.cs` (used by `n2_cut.sh`) seeks, steps one frame, seeks the same moment again and renders, so
  Mercer, the creatures and the instanced decor show the moment itself (`imp3b_still.cs` shows the previous pose and no
  instanced decor).
- The round-26 FBX files arrive turned half a turn about Y like every export of ours (an eye's cornea at -Z, a wall's
  backing at +Z): `UndergroundSet` and `EyeField` add a 180 degree turn under the node matrix to get the README frame
  (cavern side and eye forward +Z, `axis_unity` as written).

## Laser duel, drone value and targeted generator steps (added 2026-11-05)

| What | Command |
| --- | --- |
| Tower duel: Gun against Laser per species, useful DPS and holding power per 100 coins | `unity command run_script --file Tools/PlayTest/Scripts/LaserDuel.cs --entry LaserDuel.Run --timeout_ms 600000 --timeout 660` (table in `Tools/PlayTest/laser_duel.txt`; beam variants in `duel_arg.txt`, e.g. `sticky=0`, `swing=10`, `speed=40` for the beam before 2026-11-05) |
| What a harvest drone earns per level and difficulty, gun-biters and flyers per level | `run_script --file Tools/PlayTest/Scripts/DroneValue.cs --entry DroneValue.Run` (writes `Tools/PlayTest/drone_value.txt`) |
| Only the generator's "Roster" step (Laser and the other non-core towers) | `run_script --file Tools/PlayTest/Scripts/ApplyTowers.cs --entry ApplyTowers.Run` |
| Only the drone offers per level and the "Level story" step (dialogues, triggers) | `run_script --file Tools/PlayTest/Scripts/ApplyDrones.cs --entry ApplyDrones.Run` |
| Screenshots: a drone introduction with its lit card, a Laser against a Skitter swarm | `bash Tools/PlayTest/n3_shots.sh` (pictures in `Assets/Temp/n3_*.png`; restore the save after; `n3_beam_probe.cs` prints the beam's target and cooldown) |

The probe bot's `[Probe]` lines now end with the wear of the run: gate and gun health lost and money spent on repairs.

## Round 8: overview, radar, crack showcase (added 2026-11-05)

**Input.** New action `Overview` in `Settings/Input/GameInput.inputactions`: hold **Tab**, or hold the gamepad **View**
(Select) button for 0.25 s. The gamepad's Skip-wave binding on the same button is now a tap (`tap(duration=0.25)`).
The binding lives in the action asset, so it can be rebound there (there is no in-game rebinding screen yet).
While held: the mouse points (click fixes the spot), or the left stick / pan keys move the aim and A fixes it;
release returns to the play view, at the fixed spot when there is one. The radar (top right) takes a click to send the
camera; options "Radar On/Off" and "Radar size" (saved as `RadarOff`, `RadarSizePercent`).

| What | Command |
| --- | --- |
| Hold / release the overview in Play Mode | `unity command simulate_key --key Tab --action down` / `--action up` |
| Click (real UI click: down, wait, up; `click` alone is too quick for the radar) | `simulate_pointer --x X --y Y --action down`, then `--action up` (y from the bottom) |
| Radar and overview screenshots at one Game view size | `sh <scratchpad>/round8/r8shots.sh 1280 720` (C1_04, an opened nest, play, overview, pick, return, radar click, Options) |
| Lay a burrow crack along a stretch of road to judge its look | `eval_file --file <scratchpad>/round8/crack_show.cs` (args `startShare cells openShare zoom` in `crack_show.txt`; `crack_clear.cs` removes it, `zoom_at.cs` zooms on a slab) |
| Only the Frost Warden's art | `run_script --file Tools/PlayTest/Scripts/ApplyBossFx.cs --entry ApplyBossFx.Run --timeout_ms 900000 --timeout 960` |
| Only the colony base (sync `ArtSource/Exports/Base`, rebuild `BaseArt` with footprint, gun mounts and drone docks, point the themes at it) | `run_script --file Tools/PlayTest/Scripts/ApplyBase2.cs --entry ApplyBase2.Run --timeout_ms 600000 --timeout 660` |
| Round 14 base helpers (Play Mode, `n1_shots.sh`) | `r14_focus.cs` (camera on `base`/`bays`/`pad`/`front`/`play` from `r14_focus.txt`), `r14_bays.cs` (bays and shuttle clock; `r14_arg.txt` "shuttle <s>" sets it), `r14_launch.cs` / `r14_launch_repair.cs`, `r14_hurt_turret.cs`, `r14_hurt_base.cs` (`r14_hurt.txt`), `r14_stage_dock.cs` (poses bay 1 for a still, `r14_stage.txt` depth), `r14_snap.cs` (still only mid-dock, bounds in `r14_wait.txt`; game time cannot be slowed from outside, so poll it repeatedly), `r14_arena.cs` + `r14_boss.cs` (boss arena from Boot; `r14_arg.txt` "near" puts the boss before the base), `base2_fit.cs` (what lies under the base on every level), `base2_probe.cs` (model hierarchy) |

- `capture_game_view` always writes 1280x720; the Game view size (`PlayModeWindow.SetCustomRenderingResolution`) still
  decides the layout. The Game view is left at 1920x1080.
- The PlayMode test runner can keep a queued run after `cancel_tests`; check `test_status` and `editor_status` again
  before the next recompile.

## Round 22: the balance pipeline (added 2026-11-06)

One entry point per stage; every stage reads the previous stage's files and the game's definitions (Docs/BALANCE.md,
"The balance pipeline"). Chained: `bash Tools/PlayTest/balance_pipeline.sh all` (or a list of stages: `measure fit apply
solve search replay acceptance`, plus `stepcheck`). The optional, untracked `Tools/PlayTest/probe_arg.txt` narrows the
PlayMode stages (`levels=0,1`, `difficulties=1,2`, `seeds=1,2,3`, `step=0.05`). Check `editor_status` is `stopped` first.

| Stage | Command (CLI) | Writes |
| --- | --- | --- |
| measure (yardstick: obvious family + creative, all seeds) | `run_tests --mode PlayMode --filter Measure_Yardstick_ObviousFamily --include_explicit true --async_tests true`, poll `test_status` | `Tools/PlayTest/yardstick.txt`, `calibration_records.txt` |
| step check (same runs at 0.2 s and 0.05 s) | `run_tests --mode PlayMode --filter Measure_StepCheck_FineAgainstUsual --include_explicit true --async_tests true` | `calibration_records_step.txt` |
| fit | `run_tests --mode EditMode --filter CalibrationFitTests` or menu `Tools/Generate/Balance/2 Fit calibration` | `calibration.txt` (factor lines, agreement, step check) |
| apply | `run_script --file Tools/PlayTest/Scripts/ApplyCalibration.cs --entry ApplyCalibration.Run --timeout_ms 120000 --timeout 180` or menu `.../3 Apply calibration` | `Assets/_Project/Data/Balance/BalanceCalibration.asset` |
| solve | `run_script --file Tools/PlayTest/Scripts/ApplyWaves.cs --entry ApplyWaves.Run --timeout_ms 900000 --timeout 960` or menu `.../4 Solve waves` | the wave assets |
| search (stage A) | `run_tests --mode EditMode --filter PlanSearchTests` or menu `.../5 Plan search` | `Tools/PlayTest/Plans/<Level>_<d>.txt` + `*.json`, `plans_summary.txt` |
| replay (stage B) | `run_tests --mode PlayMode --filter Replay_SearchedPlans_EveryCell --include_explicit true --async_tests true` | `plan_replay.txt`, `plan_records.txt` |
| acceptance | `run_tests --mode EditMode --filter BalanceAcceptanceTests` or menu `.../7 Acceptance report` | `acceptance.txt` |

Batchmode fallback (editor closed): `Unity -batchmode -projectPath . -runTests -testPlatform PlayMode -testFilter
Measure_Yardstick_ObviousFamily -testResults results.xml -logFile -` for the PlayMode stages (explicit tests run when
named by the filter), `-testPlatform EditMode -testFilter CalibrationFitTests` and so on for the EditMode stages, and
`-executeMethod ApplyCalibration.Run` / `ApplyWaves.Run` for the scripts.

- Every report header states the calibration's state: `calibration <date>, seeds, step, inputs <hash> (current|STALE)`.
  The hash (`InputHash`) covers towers, creatures, difficulties, upgrades, maps with their spots, gadgets, drones and the
  rules; a stale calibration is reported by every stage and by `PlanSearchTests` (ignored with the reason), never reused silently.
- The PlayMode test runner reports `0/0 passed` for these long async runs; judge them by `test_status` and the files written.
- A PlayMode run creates `Saves/progress.json`; `balance_pipeline.sh` restores the state it found (absent or a backup).

### Round 22b additions (2026-11-06)

- The wave records (`calibration_records.txt`, `plan_records.txt`) carry four more fields per wave: base lost, gate lost,
  repair-drone seconds on the field, repair-drone seconds repairing. Old records read as zeros.
- `calibration.txt` has two more key lines the apply stage writes to the asset: `leak <factor>` (the leak model's base
  damage factor) and `repairbusy <share>` (only when a run had a repair drone).
- The plan search also writes `Tools/PlayTest/Plans/README.md` (the plans for Remco). A plan file may contain `repair`
  steps (item `gates`); the replay takes them by the bots' repair rule.
- EditMode `LeakModelTests` covers the leak model without assets.
- Timing: one probe run at step 0.2 s takes about 20-25 s on this machine when the editor is unfocused; the full
  yardstick (6 levels x 4 difficulties x 3 seeds x 5-6 strategies, about 430 runs) about 3 hours.

### Round 22c additions (2026-11-07)

- The fit stage also writes `Tools/PlayTest/calibration_waves.txt`: one line per recorded wave with the paper side (pressures,
  walk kills, queue and doorstep guards, the siege's gate and base damage, the state after it) beside what the game recorded
  (damage by type, base and gate losses). Read it with a script when a model term is in doubt; it is the fit's working table.
- `calibration.txt` carries `siege <factor>` instead of `leak`; the apply stage writes it to `_siegeFactor` on the asset.
- The fit judges the recorded runs against the fresh player the yardstick plays with (`CampaignModel.InputForFreshPlayer`).
- A scratch check of the solver against the stored waves is a few lines in an `eval_file` script: build the calibrated model,
  `BalanceLedger.Evaluate(model, index, level, profile, d, null, model.Rules.HumanTowersOnly())` for the stored waves,
  `model.BuildWaves(index, WaveScripts.ByKey(key), out _)` for the in-memory solve, `BalanceChecks.PlainMean` of both.

### Round 22d additions (2026-11-07)

- New stages in `balance_pipeline.sh`: `build` (the builder on stage-B verdicts: lever once per cell from the records, then solve;
  `Tools/PlayTest/Scripts/BuildFromVerdicts.cs`, menu `Tools/Generate/Balance/6b`), `readme` (`Plans/README.md` from the plans that won in
  the replay; `ReadmePlans.cs`, menu `.../8`; run it before a new search, which rewrites the plan files) and `droneprice` (the repair
  drone by the investment rule; `ApplyDronePrice.cs`, menu `.../9`, report `Tools/PlayTest/drone_price.txt`). `all` ends with `readme`.
- The probe plays the campaign's expected player (`ProbePlayer`): no PlayMode balance run touches `Saves/progress.json` any more (the
  progress lives in memory for the run). The yardstick adds the fresh-save sanity runs on C1_01 Easy.
- Record files: the `run` line has a 13th field, `expected` or `fresh` (the player it was played with).
- `calibration.txt` has a `queueguard <share>` line (the apply stage writes `_queueGuardShare`); the hash no longer covers drone prices.
- `Tools/PlayTest/builder_iterations.txt` counts the builder's moves per cell (at most two).
- Scan of the ledger's purchase per wave: `WaveLine.Purchase` (type index:level x count) in an `eval_file` script beside the pressures.

### Round 22e additions (2026-11-07)

- Stage A searches **strategies** (`StrategySearch`, `Tools/Generate/Balance/5`, `PlanSearchTests`): per cell the distinct winning
  strategies are written as `Tools/PlayTest/Plans/<Level>_<d>_strategyN.json` (a `StrategySpec`: spot priority per tower, favoured
  tower per threat, upgrade policy, gadget and drone triggers) and described in `<Level>_<d>.txt` with the build order the paper
  took. About 10 s per cell at 1500 samples. The fixed-plan search of round 22 is still callable as `BalanceStages.SearchFixedPlans`.
- The replay plays `_strategy*.json` with the adaptive bot (`ProbeStrategy.Adaptive`; record strategy name `strategy <name>`) and any
  other `<Level>_<d>_*.json` as a scripted plan. The record files carry `move|wave|kind|item|x|y` lines: the build order each run took.
- `bash Tools/PlayTest/measure_cells.sh "difficulties=2,3"` re-measures the yardstick for a filter and merges the records with the
  cells outside it (the measure stage writes `calibration_records.txt` whole). `bash Tools/PlayTest/round22e_chain.sh "<filter>"`
  runs measure (filtered), replay (all), acceptance and readme in one go; start it in the background with a log.
- `Tools/PlayTest/wave_tuning.txt` may hold script levers beside the scales: `KEY D plated 1.5`, `KEY D tempo 0.85`, `KEY D raid 3`
  (`WaveTuning.ReadLevers`, applied by the composer per difficulty). The builder applies them in that order to a cell still too easy
  after a scale move.
- `acceptance.txt` and `builder.txt` end with the in-play ramp table (`BalanceStages.InPlayRamp`).

### Round 22f additions (2026-11-07)

- `bash Tools/PlayTest/replay_cells.sh "levels=2,3" "difficulties=3"` replays the searched strategies for a filter and merges
  `plan_records.txt` with the cells outside it (the replay writes the file whole). Run `balance_pipeline.sh search` with the
  same filter in `probe_arg.txt` first when the strategies must be fresh. `bash Tools/PlayTest/round22f_chain.sh
  "levels=0;difficulties=2,3" "levels=4,5;difficulties=0,1,2,3"` chains measure, search and replay per cell group, then
  acceptance and readme; start it in the background with a log.
- The builder (`build`) prints per Easy cell what the Easy raid rule does at the solve ("moves the first raid from wave 3 to
  wave 10") and skips the lever there; per Psycho cell a script lever when the paper says Psycho asks no more than Hard.
- Finale probe (built, not yet run): `run_tests --mode PlayMode --filter Measure_ContinentFinales_AsPlayed --include_explicit true
  --async_tests true`, narrowed by `probe_arg.txt` lines `bosses=0` (index in `FinalePlan.Finales`: Gravemaw 0 ... SealedOne 5),
  `difficulties=1`, `seed=1`, `step=0.2`; writes `Tools/PlayTest/finale_probe.txt`. Then `ContinentFinaleTests` (EditMode) reads
  it, and `bash Tools/PlayTest/balance_pipeline.sh finale` (or menu `Tools/Generate/Balance/10`) writes the derived boss health
  and shares into the assets (calibration inputs: the pipeline then says STALE until measure, fit and apply ran).

### Round 24 additions (2026-11-08)

- `unity command run_script --file Tools/PlayTest/Scripts/ApplyRoles.cs --entry ApplyRoles.Run --timeout_ms 600000 --timeout 660`
  now also runs the generator's Hero step (health, regeneration, the weapons' armour factors) beside Towers, Roster and
  Bestiary; its result line prints the Gun Turret's armour factor and the Cragback's armour and reward.
- Armour is a pool with per-weapon factors (Docs/BALANCE.md, round 24): a mechanic change, so the calibration is STALE until
  `bash Tools/PlayTest/balance_pipeline.sh all` has run (measure, fit, apply, solve, search, replay, acceptance, readme; about
  five to six hours of machine time). Start it in the background with a log: `bash Tools/PlayTest/balance_pipeline.sh all >
  Tools/PlayTest/round24_chain.log 2>&1 &`, then `build` and `round22f_chain.sh` on the cells the builder changed.
- `bash Tools/PlayTest/r24_face.sh <out dir> [node] [prefix]`: one idle and one face close-up still of Spencer after a Mercer
  re-sync (round 23d's probe scripts; the editor is stopped afterwards).
- `unity command get_console_logs` does not exist in this CLI build: use `console_status` (counts) and `recompile_status`.

### Round 25: impact and parallel runs (2026-11-09)

Remco's wish: change a number and see the impact without a day of machine time. Three pieces (Docs/BALANCE.md "Impact in
minutes"): an analytic impact calculator against a saved baseline, headless parallel workers for the PlayMode stages, and a
`cells=` filter so only the flagged cells are replayed. The editor must be stopped (`unity command editor_status`) for the
editor stages; the workers never touch the open editor.

**The how-to (change a number, see the impact, replay what might flip):**

1. Baseline first, on the data before the change (once after every committed pipeline run; seconds):
   `bash Tools/PlayTest/balance_pipeline.sh snapshot` -> `Tools/PlayTest/impact_baseline/paper.txt` (the paper verdict of every
   searched plan, the creative reference and the obvious family in every cell, the input and shape hashes, and the band: the
   calibration's own error, from the odd/even hold-out efficiencies).
2. Change the number (data or generator: e.g. the Gun Turret's armour factor 0.3 -> 0.25 in `BalanceSheet`/`ApplyRoles.cs`).
3. `bash Tools/PlayTest/balance_pipeline.sh impact` (seconds; menu `Tools/Generate/Balance/8 Impact`) -> `Tools/PlayTest/impact.txt`:
   per cell the best plan, the plans and obvious kinds holding and the creative reference, before -> after; the cells flagged
   (a verdict flipped, stars moved, or a margin moved by the band or more; the Psycho-above-Hard ordering; the upgrade rule
   per gun); and the exact replay filter. `STALE` at the top means the model's shape changed (a rule, a term, a tower or
   creature type, the combat code), and then measure, fit and apply come first; a number inside the calibrated model reuses
   the calibration's efficiencies, which is the point.
4. Replay only the flagged cells, in parallel: `bash Tools/PlayTest/replay_cells.sh "cells=2:0,3:2"` (the line impact.txt
   prints; `measure_cells.sh` for the yardstick), then `balance_pipeline.sh acceptance` (and `fit` when the records changed
   enough to matter). Revert the number if the impact is not what you wanted.

**Workers** (`Tools/PlayTest/workers.sh`): clones of the project (Assets, Packages, ProjectSettings, Library, Tools) under
`C:\Users\remco\TowerDefenseWorkers\wN`, each running `Unity -batchmode -runTests PlayMode` headless with its own
`probe_arg.txt` (`cells=node:difficulty:seed` units dealt round robin) and `-job-worker-count 2`; the probe turns the cameras
off in batch mode (and keeps every rig evaluated, `ModelClipPlayer.AlwaysAnimate`). Each clone's product name is "Tower
defense worker wN", so its persistentDataPath is its own and Remco's save is never read or written (the script checks the
save's MD5 before and after as well). `workers.sh count` gives N from the cores and the RAM (4 GB per worker, 10 GB kept:
5 on this machine); `workers.sh sync [N] [--library]` refreshes the clones (the Library only when missing or on `--library`);
`workers.sh run replay|measure [N] [filter lines]` runs and merges. `balance_pipeline.sh measure|replay` use the workers by
default; `--serial` (first argument) runs them in the open editor as before. Env: `TD_WORKERS`, `TD_WORKERS_ROOT`,
`TD_LEVELS` (the campaign nodes, default 0..5), `TD_JOB_WORKERS`, `TD_GRAPHICS=0` for `-nographics` (URP then logs render
errors, so it is off by default). The clones are disposable (about 6 GB each); delete the folder to start clean.

**Apply bar**: `balance_pipeline.sh apply` refuses a fit whose hold-out line is below the bar (90 % / 0.8; the fit writes
`holdout` and `shape` lines into calibration.txt since this round); `apply-force` applies it anyway, with a warning.

**Stall diagnostic**: when the probe's 900 s wall clock runs out, it logs `[Probe] wall clock ran out at wave N: ...` with
`LevelController.SpawnDiagnostic()` (wave spawning or not, held nest spawns, every creature still counted alive) into the
console and `Logs/probe_trace.txt`.

### Round 26 (2026-11-09)

- `unity command eval_file --file Tools/PlayTest/r26_geometry.cs --result-only` (reads `Tools/PlayTest/geometry_arg.txt`: one line per
  level, `C1_02 5,7 7,6`): the plan model's defence gate, its index on the main road, and per spot the ground, approach and behind cover
  at two reaches, into `geometry_dump.txt`. Use it when a paper verdict and a play record disagree about where a tower kills.
- A worker run's per-wave trace is in its clone: `C:\Users\remco\TowerDefenseWorkers\wN\Logs\probe_trace.txt` (one unit, e.g.
  `bash Tools/PlayTest/workers.sh run replay "cells=1:0:1"`, puts the cell's five strategies on w1). Mind: the merge replaces the
  whole cell's records (all seeds) with the unit's runs; `git checkout Tools/PlayTest/plan_records.txt` afterwards.
- Residuals of the fit with awk on `calibration_waves.txt` (columns in `CalibrationFit.WaveTableHeader`): waves with `gPressure > 1.05`
  and no gate or base loss in play are the paper's pessimism, waves with `gPressure <= 1` and a loss its optimism; group by level,
  difficulty, mean health (`recHealth/groundCount`), arrival rate (`groundCount/window`).
- `bash Tools/PlayTest/round26_chain.sh > Tools/PlayTest/round26_chain.log 2>&1 &`: measure (workers), fit, `apply-force`, solve, search,
  replay (workers), acceptance, readme; stops only when a PlayMode stage fails. `apply-force` is in the chain because the fit is below
  the bar this round (its warning is in the log).
- The gate-upgrade step: a plan file may hold `{"Kind":"gate","Item":"gates"}` steps; a strategy JSON has `GateUpgradeBias` (0 never).

### Round 27 (2026-11-09)

- The fit's grid now chooses the route model's `routefire` beside `queueguard` (calibration.txt lines; `_routeFire` on the asset,
  `BalanceRules.RouteFire`); `CalibrationFitTests` takes about five minutes (56 grid points, each a siege fit and a check).
- The paper is slower with the route model: `balance_pipeline.sh snapshot` about 2.5 min (was 6 s), `impact` the same; a plan
  search per cell is minutes. Run `search` in the background with a log.
- Finale probe on the clones, one difficulty per worker: `bash Tools/PlayTest/finale_workers.sh` (or `finale_workers.sh 1` for
  Medium only) merges the four reports into `Tools/PlayTest/finale_probe.txt`; then `run_tests --mode EditMode --filter
  ContinentFinaleTests` and `balance_pipeline.sh finale`. The clones must be idle (no measure or replay running).
- The builder's lever order is plated, swarm, tempo, raid (`wave_tuning.txt` line `Key d swarm 1.5`: unarmoured troops no heavier
  than twice the lightest troop weigh x1.5 in every fill of that difficulty).
- Pass logs of this round: `Tools/PlayTest/round27_pass1.log` (measure + replay of the tuned and flagged cells on the workers).

### Round 29 (2026-11-09)

- Continent bosses' data again (health, armour pool, ward and escort powers, Codex text) without the full generator:
  `unity command run_script --file Tools/PlayTest/Scripts/ApplyBossData.cs --entry ApplyBossData.Run --timeout_ms 300000 --timeout 360`
  (menu `Tools/Generate/Balance/11 Continent bosses (data and powers)`). `ApplyBosses.cs` (round 28) stays the art import.
- One campaign level's map again from its spec (layout, terrain bake, the arena copies that borrow its map): write the key into
  `Tools/PlayTest/relayout_arg.txt` (one line, `C1_05`), then
  `unity command run_script --file Tools/PlayTest/Scripts/RelayoutLevel.cs --entry RelayoutLevel.Run --timeout_ms 600000 --timeout 660`.
  The result line gives the gate indices and shares on the walked road. Solve the waves afterwards (`balance_pipeline.sh solve`);
  the other levels are byte-stable (check `git status Assets/_Project/Data/Levels`).
- A layout spec may carry `FirstGateShare` (default 0.12; Quartz Flats 0.3).
- The ward power: `BossAbilityKind.Ward` (`SweepSeconds` up, `Interval` between); `Enemy.IsWarded` turns every hit away; the
  ability view rings and shimmers it. Escorts: the existing `Spawn` kind on the continent bosses (`<Boss>Call.asset`).
- The ledger reads both through `EnemyStats.WardShare` / `SummonsPerSecond` (`BalanceLedger.AddPowers`, `WaveLoad` boss branch,
  `WaveGroup.StartShare` for the route model). Changing them changes the model's shape: `fit` and `apply-force` before `impact`.
- Logs of this round: `Tools/PlayTest/r29_*.log`.

### Round 30 (2026-11-10)

- A layout spec may carry `OpeningClearShare` (default 0; Quartz Flats 0.4): no terrain patch within `RoadGenerator.OpeningClearance`
  (4) cells of the main road's first share of the walk, so every cell beside the opening legs is a build spot. Nest ponds are exempt.
  Quartz Flats went back to the default `FirstGateShare` 0.12 (RelayoutLevel picked layout attempt 1: the road itself changed).
- Boss arena copies (`CreateArenaLevel`): a pool boss rises from the largest pool of its kinds in the road's first half, and the
  arena's plain builder stands its towers at the first gate, so the copy now loses the pools of the boss's kinds between the first
  gate (12 %) and the middle (`MidroadPoolLatest` 0.55) and, when no pool joins before the first gate, carves a two-by-two
  entrance lake just past the spawn (`EntrancePoolRoadShare` 0.05). The generator logs both (`[Generator] Arena_Ice: ...`).
- `[Boss] <boss>: rises from the <kind> pool of N cells at (x, y) (P % of the road), exit (x, y)` is logged once per pool
  entrance (`ContinentBossBrain.RiseFromPool`); read it with `unity command console --tail 400 --level log --result-only`
  after a finale run to see where a boss came up. `unity command console` exists in this CLI build (round 24 said only
  `console_status` did).
- `DifficultyProfile.BossEscortShare` (Psycho 0.5): the share of a continent boss's escort call (the `Spawn` power) released on
  that difficulty, in play (`BossAbilities.SetEscortShare`) and in the ledger (`DifficultyStats.BossEscort`, `WaveLoad`); splits
  keep their count. In the inputs hash.
- `BalanceRules.PsychoBossShareAtLeastHard`: the `finale` stage writes one boss health share for Hard and Psycho,
  `FinaleProbeReport.HardPsychoShare` = Hard's derivation pulled down to `PsychoCeiling` (the share at which the Psycho finale
  player still beats every boss once `DerivedHealth` has moved it); `ContinentFinaleTests` checks the same number.
- Running a single-boss finale probe in the open editor: `printf 'bosses=2\ndifficulties=1\n' > Tools/PlayTest/probe_arg.txt`,
  `run_tests --mode PlayMode --filter Measure_ContinentFinales_AsPlayed --include_explicit true --async_tests true` (about a
  minute); it overwrites `finale_probe.txt` with that cell only, so copy the merged report aside first and restore it.
- The fit after a difficulty's money changed is wrong until the Psycho cells are re-measured: the records were played at the
  old money and the model judges them at the new (round 30's first fit fell to 74 % / 0.73 for that reason; not applied).
- Logs of this round: `Tools/PlayTest/r30_*.log`.

### Round 31 (2026-11-10)

- `SpotCover.Gate` (a gun guards the defence gate's queue) is true only when the gun reaches where the held walkers stand:
  `LevelGeometry.QueuePoints()` (the stop point `StandDistance` 1.4 cells before the gate along the main road, and the road cell
  behind it). `LevelGeometry.Rank` scores approach cells in full and cells behind the gate at `BehindRankWeight` 0.25. Check a
  level's spots with `r26_geometry.cs` (`geometry_arg.txt`, one line `C1_02 5,7 7,6 ...`) -> `geometry_dump.txt`.
- Nest groups walk from every nest of the level in equal shares (the game draws a nest per creature); a walker that joins the
  road behind the defence gate passes its queue: `RouteResult.PassedStream/PassedSingle` -> `SiegeGuards.PassedStream/
  PassedSingle`, which `Leak.Resolve` sends to the other gates and the doorstep. The routes are walked even with no gun standing
  (`Leak.WalkRoutes` on every level with routes). Both are shape changes: `fit`, `apply-force`, `solve`, `search`, `replay`.
- `BalanceRules.RepairDronePrice` (100) is the designer's price; the `droneprice` stage writes it and reports the rule's figure
  as advice (`drone_price.txt`). Set it to 0 to let the investment rule price the drone again.
- A per-cell view of the replays and the yardstick from the record files, without the editor:
  `awk -F'|' '$1=="run"{...}' Tools/PlayTest/plan_records.txt` (fields: run|level|difficulty|strategy|seed|step|won/lost|stars|
  waveReached|waveCount|baseLeft|coins|player); the `wave|n|dealt|...|gateDamage|baseDamage` lines follow each run.
- A worker trace of one unit (`workers.sh run replay "cells=0:0:3"`) merges that unit's runs over the cell: copy
  `plan_records.txt` aside first and restore it after (the trace in `C:\Users\remco\TowerDefenseWorkers\w1\Logs\probe_trace.txt`
  lists per wave start which creatures are alive and where: `where Rift:1@22` = one creature out of a rift nest at 22 % of the road).
- The `run_tests --filter` takes one class name; `A|B` matches nothing. Run the classes one after another.
- Logs of this round: `Tools/PlayTest/r31_*.log`.
- **The Pipeline stops answering after a long EditMode run** (round 29 and twice this round: the HTTP server answers 401 and the
  heartbeat in `Library/Pipeline/.unity-pipeline-port` goes on, but every command times out after the test runner's
  `IPostBuildCleanup`). Restart the editor from the command line: find the editor's PID (`Get-Process Unity`, the one with the
  project's `-projectPath` and no `-batchmode`), `Stop-Process -Id <pid> -Force`, delete `Temp\UnityLockfile`, then
  `Start-Process "C:\Program Files\Unity\Hub\Editor\6000.6.5f1\Editor\Unity.exe" -ArgumentList '-projectPath "C:\Users\remco\Tower defense"'`;
  `editor_status` answers again after about a minute. Assets written by run_scripts are saved on disk already.
- Do not call `unity command` inside a poll loop while a long EditMode test runs: the call blocks until the Pipeline answers.
  Poll files (`plans_summary.txt`, the worker logs, `Logs/Editor.log`) instead.
- Continent boss numbers by hand: change the `BossSpec` in `ProjectGenerator.Bosses.cs`, recompile, then
  `unity command run_script --file Tools/PlayTest/Scripts/ApplyBossData.cs --entry ApplyBossData.Run --timeout_ms 300000 --timeout 360`
  (Gravemaw 55050 -> 39540 this round). `unity command test_status --result-only` lists every test of the last run with its
  status and message (the way to find the suite's failures).

### Round 34b (2026-11-10)

- **EditMode stages headless on a clone** (the Pipeline stopped answering after every EditMode run in the open editor this round):
  `bash Tools/PlayTest/r34b_editmode_worker.sh <N> <TestClass> [filter lines]` syncs clone wN from the project (Assets, Packages,
  ProjectSettings, Tools; the clone's own product name), writes its `probe_arg.txt` from the filter lines (`cells=1:2,2:3`), runs
  `Unity -batchmode -nographics -runTests EditMode -testFilter <TestClass>` and prints the result line per test. Outputs stay in the
  clone: copy back `Tools/PlayTest/Plans/<cell>.txt` and `<cell>_strategyN.json` after a `PlanSearchTests` run (delete the project's old
  JSONs of that cell first; the search deletes stale ones only in its own folder), `acceptance.txt` after `BalanceAcceptanceTests`,
  `balance_ledger.txt` after `BalanceLedgerTests`. A plan search of ten cells on four clones took 3.5 minutes (15-30 s a cell). Clone w6
  is spare: the measure/replay/finale runs use w1..w5 and overwrite their Tools from the project, so copy a clone's plans back before the
  next `workers.sh run`. The clones must be idle (no measure, replay or finale on that clone).
- **Restarting the editor:** `Stop-Process` the editor, then wait until `Get-Process Unity` shows no process with the project's
  `-projectPath` (the AssetImportWorkers exit a few seconds after the editor) and `Temp\UnityLockfile` is gone (delete it only then), and
  start **one** editor. Starting 8 s after the kill left the lockfile in place: the new editor exited at once with the "another Unity
  instance is running" dialog. Never run batchmode on the main project while it is open; the clones are for that.
- **An async EditMode run** (`run_tests --async_tests true`) writes `Temp/pipeline_test_status.json` (`"status": "completed"`, the
  summary and every result); read that file instead of calling `test_status` while the run is on, and after it (the call hung the
  Pipeline this round).
- Logs of this round: `Tools/PlayTest/r34b_*.log`; the search summaries in `r34b_plans_summary.txt`, the first acceptance in
  `acceptance_r34b_pass1.txt`.

### Round 35 (2026-11-10)

- **Gate tips in play:** `bash Tools/PlayTest/r35_tips.sh [campaign index]` plays C1_02 from Boot with the story on, deploys the briefing, finishes the recon (`r35_tip.cs` modes `deploy`, `setup`, `upgrade`, `move`, `skip`, `state`), triggers both Voss tips and captures them (`ArtSource/Previews/QA/r35_tip_*.png`); restores the save from the scratchpad copy.
- **Laser at x3 on armour:** `bash Tools/PlayTest/r35_laser.sh [node]`: a probe Laser Turret, a Cragback held 2.5 m from it (`r35_armour.cs`, walk off) at time scale 3, stills `r35_laser_x3_*.png`. Editor steps are long at x3: the armour of a held Cragback is gone within two steps.
- **Cutscene field probe:** `probe:r35_cut_probe.cs` inside `n2_cut.sh` (eye pairs, drill against the colony rig, each landed builder clear of decor).
- **Cutscene data only:** `unity command run_script --file Tools/PlayTest/Scripts/ApplyCutsceneShots.cs --entry ApplyCutsceneShots.Run --timeout_ms 600000 --timeout 660` (the generator's Cutscene step without the Blender art hand-outs, so no material reserialisation). **Sound library only:** `Tools/PlayTest/Scripts/ApplySounds.cs` (`ApplySounds.Run`).
- **TMP font trial (nothing saved):** `bash Tools/PlayTest/r35_font.sh` (`r35_tmp_font.cs`, weight in `r35_tmp_font_arg.txt`): the dialogue box at 720p/1080p in Liberation Sans and in runtime Chakra Petch faces.
- **Full EditMode suite headless:** `bash Tools/PlayTest/r34b_editmode_worker.sh 6 "Game\.Tests\..*"` (the regex filter runs every class; explicit tests stay skipped).

### Round 36 (2026-11-10)

- **Deorbit booster model:** `"C:\Program Files\Blender Foundation\Blender 5.2\blender.exe" --background --factory-startup --threads 6 --python Tools/Blender/build_booster36.py -- --preview <dir>` (out: `ArtSource/Exports/Cutscene_Booster/`), then with Play Mode stopped `unity command eval_file --file Tools/PlayTest/r36_booster_import.cs` (syncs and builds only `Cutscene_Booster` and points the cast's `_boosterArt` at it; a full generator run does the same through `BlenderStoryBuilder.Apply`).
- **Orbit and landing stills:** `CUT_OUT=<dir> bash Tools/PlayTest/n2_cut.sh <prefix> "2 0.2" "2 0.45" "3 0.17"` (shot 2 is the orbit, shot 3 the landing). With another agent rendering in Blender a still can take a minute; run it in the background rather than under a short `timeout`, which kills the script before its `editor_stop`.
