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

### Round 37 (2026-11-11)

- **Orbit clipping probe:** `echo 2 > Tools/PlayTest/r37_probe_arg.txt`, then `probe:r37_orbit_probe.cs` inside `n2_cut.sh`: temporary mesh colliders on the mothership (and the livery plate), the shot sought in 200 steps; every plume axis, capsule and booster body and smoke/steam/spark sprite is tested against the hull. Prints `total hits 0` when clean.
- **Magnified orbit stills:** `n2_cut.sh` takes `"zoom:<shot> <progress>|<fov> <x> <y> <z> [side|under]"` (`r37_zoom.cs`): the shot camera kept in place and aimed at a point in the ship's own space with a narrow lens.
- **Field interpenetration scrub:** shots in `Tools/PlayTest/r37_probe_arg.txt` (e.g. `3 4 5 6 7 10 13 14 15`), then `probe:r37_field_probe.cs` inside `n2_cut.sh`: haulers, builders, Spencer and the brood against base modules, gates, the drill, the capsule (mesh colliders) and the decor (`EnvironmentInstancer.IsClear`); writes `Tools/PlayTest/r37_field_probe.txt` (bounding boxes, so treat single-sample hits as near misses and check with a `look:` still).
- **Hull livery texture:** `python -I Tools/Blender/build_hull_livery.py --unity` (Pillow, not Blender; Play Mode stopped), then the generator's Cutscene step (`ApplyCutsceneShots.cs`) sets the cast's `_hullLivery` and the importer (mipmaps, aniso 8).
- **Steam gameplay screenshots:** `OUT=<dir> bash Tools/PlayTest/r37_steam.sh <prefix> <node> <difficulty 0-3> <actions...>`: `def:<from> <to> <towers> <seed> [air]` (r37_defence.cs: a varied, upgraded defence on buildable cells beside the road), `skip:<n>`, `step:<frames>` (1/30 s each), `drones`, `revive`, `money:<n>`, `probe`, `shot:<name>|<crowd|air|boss|hero|drones|keep|x,z> <zoom x play zoom> <clean|hud> [dx dz]` (r37_shot.cs renders 3840x2160 off-screen; `clean` hides the screen-space interface for that picture only). The planet map: `OUT=<dir> bash Tools/PlayTest/r37_map.sh <prefix>`. Both keep the save as it was.

### Round 38 (2026-11-11)

- **Gravemaw maw import only:** `unity command run_script --file Tools/PlayTest/Scripts/ApplyGravemawMaw.cs --entry ApplyGravemawMaw.Run --timeout_ms 900000 --timeout 960` syncs the Gravemaw export and rebuilds just its art row (`RebuildOnly("Gravemaw")`); prints the parts with their High/Low materials, the renderers per LOD (tris, bones) and the clips.
- **Gravemaw maw stills:** `[FACE=1] bash Tools/PlayTest/r38_gravemaw_shots.sh <out dir> <prefix> [close zoom]` loads the Gravemaw arena, waits for the roar after it surfaces at the rig and renders 1920x1080 stills at the play zoom and close (`r38_still.cs`, no screen-space interface), a forced LOD1 roar and one on the road; `r38_maw.cs` (`r38_arg.txt`: `probe`, `roar`, `face`, `lod <n>`) lists each renderer's materials. `FACE=1` turns the boss towards the camera for that roar (a diagnostic of the throat only). Keeps the save as it was.

### Round 39 (2026-11-11)

- **Played run of the opening (no seeks):** `OUT=<dir> HOLD=<s> bash Tools/PlayTest/r39_play.sh` plays the cutscene from Boot in real time, confirms every dialogue line with a simulated Enter key (Input System), but holds the gate stand's dialogue open for HOLD seconds while it probes the brood (`r39_swarm.cs`: retreat clock "held", creatures standing) and renders `r39_play_hold.png`; after the confirm it renders the retreat (`r39_play_retreat.png`), follows the handoff into the first level and prints the console counts. Copies the save aside first and restores it afterwards.
- **Magnified stills from a shot's own camera:** `"z39:<name>|<shot> <progress>|<fov> <target> [dx dy dz]"` inside `n2_cut.sh` (`r39_zoom.cs`); the target is `drill`, `base`, `core`, `capsule`, `hero`, `beacon`, `haulerN`, `builderN` or `x y z`.
- **Field probes (inside `n2_cut.sh` as `probe:<file>`):** `r39_ground_probe.cs` (every hauler and builder baked and held against the drawn ground; moments in `r39_ground_arg.txt`), `r39_base_probe.cs` (how each built piece meets the ground; `r39_base_arg.txt`), `r39_crystal_probe.cs` (every crystal against the convex hulls of rocks, cliffs, terraces, modules, drill and capsule; writes `r39_crystal_probe.txt`), `r39_drill_probe.cs` (the drill deck's footprint against the slope), `r39_capsule_probe.cs` (ramp, Exit socket and Spencer's stops in the capsule's frame), `r39_haulers.cs` (where every drone stands), `r39_decor_list.cs` (instanced pieces by mesh).
- **What draws a pixel:** `r39_pixel_probe.cs` (`r39_pixel_arg.txt`: `<shot> <progress>` then `x y` lines; optional `r39_pixel_cam.txt` `px py pz lx ly lz fov`) hides each renderer under the pixel in turn and ranks the change; `r39_ray.cs` marches the shot camera's ray to the ground and lists the instanced decor and renderers there (instanced decor does not show in the pixel probe).

### Round 40 (2026-11-11)

- **Spencer's acting pass (Blender):** `"C:\Program Files\Blender Foundation\Blender 5.2\blender.exe" --background --factory-startup --threads 8 --python Tools/Blender/build_mercer_acting.py -- --stage <dir> --src <pass-4 Mercer.blend> --units ArtSource/Blend/SpencerFace_units.npz`. `--src` must be the blend *without* the acting pass (ArtSource/Blend/Mercer.blend now carries it; the pass-4 blend is in git history before round 40). `CAPSULE_SCALE` in the script must equal `CutsceneCast._landedCapsuleScale` (1.4). Copy `Mercer.fbx`, `Mercer_report.json` into `ArtSource/Exports/Mercer/` and `blend/Mercer.blend` over `ArtSource/Blend/Mercer.blend`.
- **Spencer import only:** `unity command run_script --file Tools/PlayTest/Scripts/ApplySpencerActing.cs --entry ApplySpencerActing.Run --timeout_ms 900000 --timeout 960` syncs Mercer and rebuilds his hero art (clips by role, the acting rows with root offsets and face tracks from the report) and lists clips, acting rows and each face mesh's blend shapes.
- **Capsule fit probe (edit mode, preview scene):** `echo "<scale> <frame step> [last frame]" > Tools/PlayTest/r40_fit_arg.txt`, then `unity command eval_file --file Tools/PlayTest/r40_fit_probe.cs`; every triangle edge of Spencer's baked body, face and visor that crosses the capsule hull outside the doorway is a hit, and soles more than 3 cm under the open ramp are counted; writes `Tools/PlayTest/r40_fit_probe.txt`.
- **Gravemaw roar probe (edit mode):** `unity command eval_file --file Tools/PlayTest/r40_roar_probe.cs`: Roar/Surface_Throw/Move lengths and moments, the Roar's last frame against the crawl's first, the head's pitch through the roar.
- **Staging geometry of the opening:** `probe:r40_layout.cs` inside `n2_cut.sh` lists the shots and prints the capsule, exit root, beacon, dig site, the Queen's spot and the guard post in their anchors' frames.

### Round 44 (2026-11-11)

- **Spencer's footfalls:** `Tools/Generate/Hero Footfalls` (or `unity command eval_file --file Tools/PlayTest/r44_bake_footfalls.cs`, which also lists them) samples every clip of his body on the imported model and writes `ModelClip._footfallSeconds` into `Data/Art/Story/Mercer.asset`. The hero build (`BlenderStoryBuilder.BuildHero`, so also `ApplySpencerActing.cs`) runs it too; rerun it after any new clip.
- **Opening shots and sounds:** `ApplyCutsceneShots.cs` (round 35) writes the arrival and launch shots, the cast's capsule count and flinch face, the cues, layers and `ShakeRumble` per shot and the bank; `Tools/Generate/Opening Cutscene Sounds` rewrites only the bank and the shots' sound (fill the burrow-close slot this way once `Approved/Intro/Threat/threat_burrow_close_0N.wav` land).
- **Shot indices changed:** the opening has 17 shots now (2 arrival, 3 launch, 4 landing, ... 11 "ORA... what is this?", 14 reaction, 16 handoff); seek stills with `n2_cut.sh` accordingly.

### Round 45 (2026-11-12)

- **Boss close-up detail import only:** `unity command run_script --file Tools/PlayTest/Scripts/ApplyBossDetail45.cs --entry ApplyBossDetail45.Run --timeout_ms 900000 --timeout 960` syncs only the sixteen boss exports (not the small species another Blender agent may be writing) and rebuilds their art rows and the corrupted skins of the same exports (`RebuildOnly`); prints each row's parts with High/Low material, emission and smoothness, and the renderers per LOD (tris, bones). The parts (`<Boss>_<Part>`) take the detail families the report's `round45_detail`/`r45_detail` block lists for them.
- **Boss close-ups in game lighting:** `[STEPS=n] [FAR=1] bash Tools/PlayTest/r45_boss_shots.sh <out dir> <prefix> <level index> <Enemy[:ortho half height]> ...` plays the level from Boot without waves, spawns each boss, freezes it, zooms in and renders 1920x1080 stills off-screen (`r45_still.cs`): `<prefix>_<Enemy>.png` turned to face the camera, `_side` turned 35 degrees more (`Tools/PlayTest/r45_face.txt`: `<Enemy> [extra yaw]`, `-` for its own heading). Copies the save aside and restores it.

### Round 46 import (2026-11-12)

- **Small-enemy close-up detail import only:** `unity command run_script --file Tools/PlayTest/Scripts/ApplySmallDetail46.cs --entry ApplySmallDetail46.Run --timeout_ms 900000 --timeout 960` syncs only the seventeen small-species exports (Bat included, so the four element skins too) and rebuilds their 38 art rows (with the corrupted skins) through `RebuildOnly`; same per-row report as round 45. Close-ups of small enemies work with `r45_boss_shots.sh` at an ortho half height of 0.45-0.9 (`Roller:0.8:Idle` to show its face uncurled).

### Round 59 (2026-11-13)

- **Salvo data only:** `ApplyCutsceneShots.cs` (as round 44) writes the cast's salvo (`_capsuleCount`, `_releaseGap`, `_pathfinderFlight`, `_cargoFlights`) and the launch shot's length, cues and layers from `ProjectGenerator.Cutscene.cs` / `.CutsceneSounds.cs`.
- **Salvo stills and probes:** `CUT_OUT=<dir> bash Tools/PlayTest/r59_cut.sh <prefix> "<shot> <progress>" "z59:<name>|<shot> <progress>|<capsule> <nozzle|body|head|tail|mid|bay> <fov> [side|under]" probe:<file.cs> ...` (n2_cut.sh plus the save kept and restored, MD5 printed at both ends). The launch is shot 3 (11.5 s): progress = seconds / 11.5.
- **Salvo probe:** `probe:r59_salvo_probe.cs` (shot from `Tools/PlayTest/r59_probe_arg.txt`, default 3): seeks the launch in 1/60 s steps and, per capsule, counts pops (a part switching on or off while 2 px or more of it is seen, hidden by the planet or the hull counted), jumps, axis and path turn rates (snap above 720 deg/s), where each capsule goes behind the rim and at what height, and boosters inside the planet; writes `Logs/r59/salvo_probe.txt` and `.csv`. `probe:r59_end_probe.cs` prints each capsule's end against the lens. `probe:r37_orbit_probe.cs` with `3` in `r37_probe_arg.txt` checks the launch against the hull.
- **Played run with the sound recorder:** `OUT=Logs/r59 HOLD=8 bash Tools/PlayTest/r59_play.sh` (r43_play.sh plus `r59_sound_rec.cs`: every one-shot start under the cutscene audio with its first three volumes; the report counts the launch shot's swells against its cues).

## Round 61: air defence probe, air raid lab, shockwave stills (added 2026-11-13)

- **Who clears the sky (PlayMode, in the open editor, one run at a time):** `bash Tools/PlayTest/r61_air_probe.sh <label> [levels=2,3 difficulties=0,1 seeds=1,2,3]`
  runs `AirDefenceProbeTests.Report_SkyGunsAgainstNone_OnTheFirstAirLevels` (three bots a cell and seed: no sky gun, no sky gun but
  guns beside a besieged base, two sky guns before the first raid), backs the save up and verifies it after, and copies the table
  to `Tools/PlayTest/r61_air_defence_<label>.txt`. About 35 s a run.
- **The sky on paper, nothing written:** `unity command run_script --file Tools/PlayTest/Scripts/AirRaidLab.cs --entry AirRaidLab.Run --timeout_ms 900000 --timeout 960`
  with overrides in `Tools/PlayTest/r61_lab_arg.txt` (`label=`, `levels=C1_03,C1_04`, `air.PulseRifle=0.2`, `rule.SkyHoldPressure=0`,
  `rule.RaidHealthFloor=0.6,0,0,0`); output `Tools/PlayTest/r61_lab_<label>.txt`. "waves moved against the stored set 0/N" on every
  level means the stored waves are exactly what the solve gives.
- **Flyers per wave as stored:** `unity command eval_file --file Tools/PlayTest/r61_air_waves.cs` -> `Tools/PlayTest/r61_air_waves.txt`.
- **Round 61's data without the generator:** `unity command run_script --file Tools/PlayTest/Scripts/ApplyRound61.cs --entry ApplyRound61.Run --timeout_ms 120000 --timeout 180`
  (the Gun Turret's air share and the hero's shockwave asset on the effect library), then the solve stage (`ApplyWaves.cs`).
- **Shockwave stills:** `[LOW=1] [NEAR=1] [PROBE=1] [FRAMES="6 8 11 15 20 26 34 45 60"] bash Tools/PlayTest/r61_shockwave.sh <out dir> <prefix> [level]`
  (towers by the hero, walkers at his post, the ability through the level's own event, stills of the play camera at stepped
  frames of 0.02 s; `PROBE=1` prints every part's live state with `r61_shock_probe.cs`). The wave leaves 0.1 s (5 frames) after the request.

## Round 62: what-ifs on a clone, the pipeline on sixteen cells, the tower's air read (added 2026-11-13)

- **A what-if played on a clone, nothing written in the project:** `bash Tools/PlayTest/r62_variant_worker.sh <N> <label> "<file>::<sed expression>" [more edits] -- [filter lines]`
  syncs clone wN, applies the edits inside the clone only (a rule in `BalanceRules.cs`, a spec number in `LevelSpecs.Crystal.cs`),
  solves the waves there (`BalanceStages.SolveWaves`, headless) and plays the three-bot air report (`AirDefenceProbeTests`) on
  the clone's waves; the report comes back as `Tools/PlayTest/r62_air_<label>.txt`. About six minutes for one cell and three
  seeds; five variants side by side on w1..w5. An edit that changes nothing stops the run. Examples:
  `"Assets/_Project/Scripts/Balance/BalanceRules.cs::s/RaidHealthFloor = { 0.6f,/RaidHealthFloor = { 0.45f,/" -- levels=2 difficulties=0`,
  `"Assets/_Project/Scripts/Editor/LevelSpecs/LevelSpecs.Crystal.cs::s/AirRaidScale = new\[\] { 1.25f,/AirRaidScale = new[] { 1.5f,/" -- levels=3 difficulties=0`.
- **The air raid lab** (`AirRaidLab.cs`, round 61) takes `raid.<Key>=<Easy>,<Medium>,<Hard>,<Psycho>` (a level's `AirRaidScale` in
  memory). Its `rule.*` overrides now go into a fresh `BalanceRules` before the model is built: in round 61 they were written
  into the generator's shared `BalanceSheet.Rules` after the calibration had copied the rules, so a `rule.*` run showed the
  *previous* run's override and the override stayed in the editor until the next domain reload (a solve after it would have used
  it). Check with `unity command eval_file --file Tools/PlayTest/r62_rules_check.cs --result-only` (prints the editor's live
  `RaidHealthFloor`); a domain reload without a script change: `unity command eval_file --file Tools/PlayTest/r62_reload.cs`.
- **The pipeline on a set of cells** (the order that worked; the main editor only solves and applies, everything else on the clones):
  1. `bash Tools/PlayTest/workers.sh run measure 5 "levels=2,3,4,5"` (the yardstick of those cells, merged into `calibration_records.txt`);
  2. drop the replayed plans of those cells from `plan_records.txt` (they were played on the old waves and the fit reads them):
     `awk -F'|' '$1=="run"{keep=($2!~/C1_0[3-6]/)} keep' plan_records.txt`;
  3. `bash Tools/PlayTest/r34b_editmode_worker.sh 6 CalibrationFitTests`, copy `calibration.txt` and `calibration_waves.txt` back from w6;
  4. `balance_pipeline.sh apply` (or `apply-force`), then `solve` (a new efficiency moves every wave of every level, C1_01 included,
     and every record is stale again), **or** `balance_pipeline.sh apply-keep`: the calibration is stamped for the current inputs
     (hash, shape, seeds, step, date) and keeps its efficiencies and factors, so no wave moves; the result line lists every kept
     number beside the fit's, and every report header then reads `calibration <date> (factors of <older date> kept)`.
     `Round62Tests.EveryStoredWave_IsWhatTheSolveGives_AtTheAppliedCalibration` guards it. What a full apply would do, in
     memory: the air raid lab with `eff.<TowerId>=<fit>` and `cal.SiegeFactor=`, `cal.QueueGuardShare=`, `cal.RouteFire=` lines
     (`total health x1.14 of the stored set` per cell);
  5. `bash Tools/PlayTest/r34b_editmode_worker.sh <N> PlanSearchTests "levels=<n>"` on four clones side by side, copy each clone's
     `Tools/PlayTest/Plans/Level_<key>_*` back (delete the project's old files of those cells first);
  6. `bash Tools/PlayTest/workers.sh run replay 5 "levels=2,3,4,5"`;
  7. `bash Tools/PlayTest/r34b_editmode_worker.sh 6 BalanceAcceptanceTests`, copy `acceptance.txt` back.
- **A few cells after a builder step** (a scale in `wave_tuning.txt`, then `balance_pipeline.sh solve` in the editor):
  `bash Tools/PlayTest/r62_cells_chain.sh 2:3 3:2 4:3 > Logs/r62/chain.log 2>&1 &` (cells as node:difficulty) measures them, searches
  each on its own clone, copies the plan files back and replays them; about fifteen minutes for three cells. Then fit,
  apply-keep and acceptance as above.
- **A per-cell table from the record files, no editor:** `python -I Tools/PlayTest/r62_cells.py Tools/PlayTest/calibration_records.txt Tools/PlayTest/plan_records.txt C1_03 C1_04`
  (best replayed plan, plans winning on most seeds, the obvious family with the wave a losing row fell at).
- **The tower's ground/air read in stills:** `OUT=<dir> bash Tools/PlayTest/r62_hint_shot.sh <prefix> <node> hand:PulseRifle hand:Rocket menu:PulseRifle view:menu`
  (`hand:` picks the card and puts the cursor on a free build cell, `menu:` builds one and opens its menu, each rendered
  3840x2160 off-screen with the interface; `view:<name>` captures the Game view itself, which is the one that shows the tower
  menu: the off-screen render misses that panel). Copies the save aside and restores it, MD5 printed at both ends.

## Round 64: armour audit, boss lab, loading and game-over probes (added 2026-11-15)

- **Targeted data step (never the full generator):** `unity command run_script --file Tools/PlayTest/Scripts/ApplyRound64.cs --entry ApplyRound64.Run --timeout_ms 300000 --timeout 360`
  stamps every weapon's damage type and armour factor (`ProjectGenerator.WeaponDamageTypes`, `BalanceSheet.ArmourFactorOf`), the
  armoured troops' body and armour (`BalanceSheet.TroopArmourShare`), the two wisp voices' presence and the music loops' streaming
  import. Then `bash Tools/PlayTest/balance_pipeline.sh solve`.
- **Recompile with the play-mode guard:** `bash Tools/PlayTest/r64_compile.sh` (refuses while Play Mode runs, waits for the reload, prints errors).
- **Armour audit (no assets written):** `unity command run_script --file Tools/PlayTest/Scripts/ArmourTtk.cs --entry ArmourTtk.Run --timeout_ms 120000 --timeout 180`
  writes `Tools/PlayTest/r64_armour_ttk_<label>.txt`: every damage source against every armoured troop (seconds to kill through
  the game's own `Health`, coins x seconds for bought sources). `Tools/PlayTest/r64_ttk_arg.txt`: `label=`, `factor.<Asset>=`
  (a source's factor as it was), `pool.<Enemy>=body,armour`.
- **Boss lab (no assets written):** `unity command run_script --file Tools/PlayTest/Scripts/BossLab.cs --entry BossLab.Run --timeout_ms 900000 --timeout 960`
  solves every level in memory and prints per cell the human surplus against the curve, the total health against the stored set
  and every boss wave (boss health as spawned, wave multiplier and boss scale, boss alone and whole wave with the hero against
  the cap). `Tools/PlayTest/r64_lab_arg.txt`: `label=`, `levels=C1_03`, `rule.<Field>=` (floats or a comma list, e.g.
  `rule.LevelBossAlone=0,0,0,0` switches the rule off), `enemy.<Id>=body,armour`, `armour.<TowerId>=`, `hero.blade=`, `eff.`, `cal.`.
  "waves moved against the stored set 0/N" on every cell means the stored waves are the solve.
- **Level start hitch:** `bash Tools/PlayTest/r64_hitch.sh old|new <node> <difficulty> <label>` (old = one-frame load as before the
  round, new = through `LevelLoader`); `Logs/r64/hitch_<label>.txt` lists every frame over 50 ms with what the loading screen had
  reported by then, the longest frame and when the level was ready.
- **Loading screen stills:** `OUT=<dir> bash Tools/PlayTest/r64_loading.sh <node> <difficulty> <prefix> <frames> [<frames> ...]` steps the
  paused editor through the load and renders the screen with its interface at 1920x1080 and 1280x720 off-screen (`r64_shot.cs`,
  the Game view is left alone).
- **Game over:** `RUN=9 OUT=<dir> bash Tools/PlayTest/r64_gameover.sh <node> <difficulty> lose|win <prefix>` runs a level at speed,
  ends it and probes the clock, the summed positions of creatures, shots and drones and the loops still audible at 1.5, 5.5 and
  8.5 s after (`r64_gameover.cs probe`); once the panel is up the three probes must be identical.
- Every harness copies the save aside, restores it and checks it against `Tools/PlayTest/r64_save_md5.txt`.

## Round 65: plateau rules, line of fire, the high-ground hint, flight curves, boss fates (added 2026-11-15)

- **Targeted data step (never the full generator):** `unity command run_script --file Tools/PlayTest/Scripts/ApplyRound65.cs --entry ApplyRound65.Run --timeout_ms 900000 --timeout 960`
  writes the terrain rules asset (`Data/Balance/TerrainRules.asset`: reach on high ground, the sight ratio), shapes every
  campaign level's plateaus (`ProjectGenerator.ApplyPlateauSkirt`: raised cells beside the road, a liquid, a mine or a nest
  taken down, the ring written as `_slopeCells`, shadowed raised cells blocked), paints its ground again, rebuilds the arena
  copies from it and writes the story's hints (the high-ground hint, every hint's loading tip). `Tools/PlayTest/r65_apply_arg.txt`
  may hold `nobake` (skip the painting) and `only=C1_01` (one level). Safe to run again. Then `balance_pipeline.sh solve`.
- **Text map with the plateau rules:** `unity command eval_file --file Tools/PlayTest/dump_map.cs` now marks the slope ring
  (`/`) and raised cells closed for towers (`#`).
- **Line of fire:** `bash Tools/PlayTest/r34b_editmode_worker.sh 6 LineOfFireTests`, then read the clone's
  `Tools/PlayTest/r65_line_of_fire.txt` under `TowerDefenseWorkers/w6` (per level: raised build cells, the tightest line over
  the plateau and its slope, and how many build cells on the plain have a line under raised ground; copy it back to keep it).
  `unity command eval_file --file Tools/PlayTest/r65_muzzles.cs` lists every tower's range and lowest muzzle per level.
- **Stills from the play camera:** `[STORY=1] [NODEPLOY=1] OUT=<dir> bash Tools/PlayTest/r65_shots.sh <prefix> <node> <difficulty> <action> ...`
  with `act:tower <TowerId> <x> <y> [upgrades]`, `act:select <x> <y>` (tower menu: range ring and info line), `act:hand <TowerId> <x> <y>`
  (placing: ring and status line), `act:spawn <Enemy> <path> [count] [Gate|Nest]`, `act:probe` (towers with height and range, the
  status line), `arg:<file>=<text>`, `eval:<file.cs>`, `step:<frames>`, `skip:<n>`, `scale:<x>`, `fly:<n>`,
  `shot:<name>|cell:<x>:<y> <zoom> <hud|clean> [w h]` (off-screen; `keep` leaves the camera) and `view:<name>` (the Game view
  itself, with post-processing and the interface as shown). `STORY=1` starts the level as the map does (Medium), so the story
  plays. Copies the save aside, restores it and checks its MD5. `bash Tools/PlayTest/r65_look.sh <prefix>` recompiles, paints
  Landing Site again and takes its plateau stills (for a change to the painter or the landscape).
- **The high-ground hint in play:** `STORY=1 OUT=<dir> bash Tools/PlayTest/r65_shots.sh v 0 1 "arg:r35_tip_arg.txt=setup" "eval:r35_tip.cs" "act:tower PulseRifle 14 1" "step:150" "view:voss_1" "arg:r65_dlg_arg.txt=next" "eval:r65_dlg.cs" ...`
  (`r65_dlg.cs`: `state`, `next`, `skip`; reports whether the save has seen `Dlg_Tip_HighGround`).
- **Flight routes:** `unity command eval_file --file Tools/PlayTest/r65_air_routes.cs` writes `Tools/PlayTest/r65_air_routes.txt`
  (every flight route and nest flight: length as its waypoints give it and as flown, sharpest turn, radius per flyer) and
  `.csv`. In play: `r65_shots.sh ... "eval:no_waves.cs" "act:spawn Shalebat 0 1 Nest" "act:spawn Skyray 1 1" "scale:2" "fly:14"`
  appends every flyer's place, facing and roll to `Logs/r65/fly_trace.csv`; the top-down picture:
  `python -I Tools/PlayTest/r65_fly_plot.py <map dump txt> Level_C1_04 <out.png> Logs/r65/fly_trace.csv` (the map dump is the
  `result` of `dump_map.cs` saved as text). Python is installed on this machine (3.12, with Pillow; no matplotlib).
- **Where a boss fell:** every probe run writes a `boss w<N> <name> <hp> hp died at <share> of the road ... first gate
  reached|not reached` line into its trace; `python -I Tools/PlayTest/r65_boss_fates.py [level name] [difficulty]` collects them
  from the worker clones' `Logs/probe_trace.txt` after a measure or a replay (a clone keeps only its last test run).
- **Boss rules on paper:** the boss lab (round 64) takes `rule.BossWaveShare=`, `rule.LevelBossAlone=` and
  `rule.LevelBossAloneMost=`; `label=` names the output `Tools/PlayTest/r64_lab_<label>.txt`.

## Round 66: ten upgrade tiers, Spencer's gear, the upgrade screen (added 2026-11-15)

- **Targeted data step (never the full generator):** `unity command run_script --file Tools/PlayTest/Scripts/ApplyRound66.cs --entry ApplyRound66.Run --timeout_ms 300000 --timeout 360`
  rewrites the permanent upgrades as the generator's "Upgrades" step does (ten tiers from `BalanceRules.UpgradeTierCostCurve`,
  `Hero_RepairDrone` and `Hero_Armour`, the tier caps per continent on `UpgradeCatalog.asset`), seats the hero's gear art
  (`Data/Art/Story/MercerGearArt.asset`: the companion's model, every armour plate on the suit's surface in the idle pose)
  and writes the story's hints (Voss's explanation of the upgrade screen). Safe to run again. No solve is needed: the
  model's shopper buys what it bought (next point).
- **Re-stamping the calibration after it** (the hash counts the upgrades, so every report called it STALE and the plan
  search skipped itself): `apply-keep` takes the hash from `Tools/PlayTest/calibration.txt`, so the fit has to be run
  first, and as the applied fit was (the yardstick alone): move `Tools/PlayTest/plan_records.txt` out of the project,
  `bash Tools/PlayTest/r34b_editmode_worker.sh 6 CalibrationFitTests`, move the plan records back, copy
  `calibration.txt` from w6 (check it differs from the old one only in date and hashes), then
  `bash Tools/PlayTest/balance_pipeline.sh apply-keep`; `unity command eval_file --file Tools/PlayTest/r66_stale.cs`
  must say `stale False`.
- **Did the model's player move?** `unity command run_script --file Tools/PlayTest/Scripts/R66PlayerStates.cs --entry R66PlayerStates.Run --timeout_ms 120000 --timeout 180`
  writes `Tools/PlayTest/r66_player_states_<label>.txt` (label = the text of `r66_states_arg.txt`): per cell the research
  earned and spent, every upgrade level bought and the mean tower bonus. Run it before and after a change to the upgrade
  catalogue and `diff` the two files; identical files mean every stored wave is still the solve.
- **Recompile:** `bash Tools/PlayTest/r66_compile.sh` (r64's script with "up_to_date" taken for done: r64's loop waited its
  full three minutes when nothing had changed).
- **The hero's gear without Play Mode:** `bash Tools/PlayTest/r66_sheet.sh <label> <tiers> [out folder] [fresh]` runs the
  apply step and renders the display figure per armour tier from the front, the back, the side and above
  (`R66GearSheet.cs`, 900 px, companion drone included); `fresh` deletes the gear art asset first so it is written again with
  the class's current defaults (the asset keeps its own numbers otherwise). `R66Bones.cs` (same `run_script` form) lists
  his bones in the idle pose and the repair drone's nodes in `Logs/r66/bones.txt`.
- **The upgrade screen in stills:** `[GEAR="Hero_Armour=4 Hero_RepairDrone=2"] OUT=<dir> bash Tools/PlayTest/r66_base.sh <prefix> <points> <action> ...`
  plays from Boot and jumps to the base with that much research and power (unsaved until a purchase; the save is copied
  aside and restored): `tip:state|next|skip` (Voss's first-visit explanation, `r66_tip.cs`), `select:<tile name>`,
  `buy:<row> [times]` (presses the row's own button, so a locked or unaffordable row buys nothing, and prints every row),
  `shot:<name>` (the screen with its interface off-screen at 1920x1080 and 1280x720), `step:<frames>`, `view:<name>`.
- **Spencer in play:** `GEAR="Hero_Armour=4 Hero_RepairDrone=2 Hero_Damage=4" OUT=<dir> bash Tools/PlayTest/r66_shots.sh <prefix> <node> <difficulty> <action> ...`
  is `r65_shots.sh` with the gear set in memory before the level loads, `hero:hurt <share>` / `hero:probe` (`r66_hero.cs`:
  health, mending, the gear's numbers, the companion's light, the blade and its aura, the plate count) and the shot focus
  `hero` (`r66_shot.cs`). A fight at his gate: `"eval:no_waves.cs" "act:spawn Skitter 0 30" "step:10" "eval:teleport_to_gate.cs" "step:215"`,
  then a row of `"shot:sNN|hero 3 clean" "step:3"` catches the cuts. He mends about seven seconds of stepping after `hero:hurt`.
- **Side by side:** `python -I Tools/PlayTest/r66_montage.py <out.png> <scale> <x0,y0,x1,y1 as shares, or -> <in.png> ...`.
- **The economy table:** `Round66Tests.Projection_ReachesTierTenOnTheFifthContinent_AndWritesTheReport` writes
  `Tools/PlayTest/r66_upgrade_economy.txt` (in the clone when run with `r34b_editmode_worker.sh`; copy it back).
- **An old save in the new model:** put a COPY of a save at `Tools/PlayTest/r66_save_copy.json` and run `Round66Tests`
  (`SaveCopy_LoadsIntoTheTenTierModel_WithoutLoss`; skipped without the file). Delete the copy afterwards: it is a save, not a fixture.

## Round 67: combat rules, roles, named upgrade steps, grace windows (added 2026-11-15)

- **Targeted data step (never the full generator):** `bash Tools/PlayTest/r67_apply.sh` recompiles (refuses while Play Mode
  runs), runs `Tools/PlayTest/Scripts/ApplyRound67.cs` and writes the role matrix. The apply step writes
  `Data/Balance/CombatRules.asset` from the generator's tables (`ProjectGenerator.Damage.cs`: damage type against armour
  and air, the named exceptions, trait thresholds, the reference fight and margin per trait, the grace windows), stamps
  every weapon with its damage type and a reference to the rules (no weapon keeps a factor), rewrites every tower's
  levels as named one-thing steps with its roles (`ProjectGenerator.Towers.cs`, `ProjectGenerator.World.cs`: the
  generator's "Towers" and "Roster" steps alone; the result lists price and damage per second per level before and
  after), takes the level-3 path-B looks out of the tower art and writes the blade's looks per tier into the hero's gear
  art. Safe to run again. On its own: `unity command run_script --file Tools/PlayTest/Scripts/ApplyRound67.cs --entry ApplyRound67.Run --timeout_ms 300000 --timeout 360`.
- **Recompile with the play-mode guard:** `bash Tools/PlayTest/r67_compile.sh`.
- **The role matrix (no asset written):** `bash Tools/PlayTest/balance_pipeline.sh roles` (or `unity command run_script --file Tools/PlayTest/Scripts/RoleMatrixReport.cs --entry RoleMatrixReport.Run --timeout_ms 120000 --timeout 180`)
  writes `Tools/PlayTest/r67_role_matrix.txt`: every tower at level 1, level 3 and level 3 with every permanent upgrade
  against the reference group of each trait (seconds for 100 coins to clear it), every role break the tests would report
  under its table, the helpers, and every creature with its traits. The Markdown tables are the ones in Docs/COMBAT_RULES.md.
- **The rule tests:** `bash Tools/PlayTest/r34b_editmode_worker.sh 6 CombatRulesTests` (the table, the wiring of every weapon
  asset, the roles per coin at three moments, the helpers, the named steps, the grace verdicts; they also rewrite the two
  reports in the clone).
- **Grace windows (the growth layer), a pipeline stage:** `bash Tools/PlayTest/balance_pipeline.sh grace` after a measure
  writes `Tools/PlayTest/grace_windows.txt`: per cell and counter trait (armour, air) the wave the trait first appears in
  (from the stored waves) and how long each yardstick row of one tower type that is not made for it held after that
  (`mono <TowerId>` in `calibration_records.txt`), against the rules' window. Part of `balance_pipeline.sh all`.
- **Changing a rule:** Docs/COMBAT_RULES.md, "Changing a rule". A factor that a tower of the levels uses is in the
  calibration's input hash: measure, fit, apply and solve follow.
- **Stills of the tower menu:** `OUT=<dir> bash Tools/PlayTest/r65_shots.sh r67 0 1 "eval:no_waves.cs" "act:tower PulseRifle 14 1" "act:select 14 1" "step:6" "shot:menu_gun_1080|cell:14:1 7 hud 1920 1080" "shot:menu_gun_720|cell:14:1 7 hud 1280 720"`
  (the info line reads "Next: <step name>: <what it changes>"). The blade's stages: `GEAR="Hero_Damage=5"` (orange) or
  `=10` (red) with `r66_shots.sh` and the shot focus `hero`.
- `Tools/PlayTest/r64_save_md5.txt` holds the save's MD5 the shot harnesses check against after restoring; set it to the
  save's MD5 at the start of a round (Remco plays between rounds).
- **The worker clones are silent** (Remco heard the hidden workers play the game): every clone sync sets
  `m_DisableAudio: 1` in the clone's own `ProjectSettings/AudioManager.asset` (`workers.sh`, `r34b_solve_worker.sh`,
  `r62_variant_worker.sh`; the project's own file stays 0), and the probe sets `AudioListener.volume = 0` in batch mode.
  A new clone script that plays must do the same. `r34b_editmode_worker.sh` leaves audio on: an EditMode run plays
  nothing, and `AudioTests` build clips, which fails with audio disabled.

## Round 68: growth, a few cells at a time (added 2026-11-16)

- Change a lever in `Tools/PlayTest/wave_tuning.txt` (`Key difficulty armourgrowth|airgrowth value`, besides scale,
  plated, swarm, tempo, raid, bosscrowd) or a troop in `LevelSpecs.Crystal.cs` (`Join(..., maxDifficulty: Medium)` for a
  troop that comes later on Easy and Medium only), recompile, then solve:
  `unity command run_script --file Tools/PlayTest/Scripts/ApplyRound68.cs --entry ApplyRound68.Run --timeout_ms 900000 --timeout 960`.
  A lever on Medium moves every difficulty of that level (the others derive from Medium's solve): measure all four.
- `bash Tools/PlayTest/r68_chain.sh "0:0,1:1"` (node:difficulty list) runs measure, the plan search per cell, the replay and
  the acceptance on the clones and dedupes `plan_records.txt`; then `bash Tools/PlayTest/balance_pipeline.sh grace readme`
  in the editor. 16 cells take about 30 minutes.
- `bash Tools/PlayTest/r68_rows.sh` prints every cell's mono rows from the yardstick (wins and the wave they fell at).
- A quick yardstick of the mono rows only: the filter line `rows=mono` (`workers.sh run measure 5 "cells=..." "rows=mono"`);
  its records are no calibration set.
- `bash Tools/PlayTest/finale_workers.sh 1:2 1:3` probes Medium on seeds 2 and 3 (difficulty:seed); it overwrites
  `finale_probe.txt` with those cells only, so copy the report aside first.
- If a chain is stopped, kill the clones' Unity (`Get-CimInstance Win32_Process -Filter "Name='Unity.exe'"` with
  `TowerDefenseWorkers` in the command line) and delete `TowerDefenseWorkers\wN\Temp\UnityLockfile`, or the next run finds
  every worker "missing".

## Round 69: saves, settings, string tables (added 2026-11-16)

- Saves: `progress.json` and `settings.json` in `%USERPROFILE%\AppData\LocalLow\DefaultCompany\Tower defense\Saves`, each
  with a `.bak` (the previous good write). A file that fails to parse is copied to `<name>.json.damaged` and the backup is
  loaded. To reset the options only, delete `settings.json` (and its `.bak`): the next boot writes the defaults.
- Data texts to keys after a generator change without the full generator: `unity command menu --path "Tools/Generate/Localisation Tables" --timeout 300`
  (also the generator's last data step "Localisation"); see `Docs/LOCALISATION.md`.
- `Tools/PlayTest/Scripts/R69TextScan.cs` (run_script, read only) lists every string field with words in the assets;
  `R69MigrationCheck.cs` runs the settings split on a copy of the real save in the temp folder and reports the fields.
- `Tools/PlayTest/r69_shots.sh`: main menu, options, level HUD, tower menu and a Voss line at 1920x1080 into
  `ArtSource/Previews/QA/r69_*.png` (back up the save first; it writes `settings.json`).
- `click_button.cs` now prefers an exact label over a partial match ("Back" used to hit "Mute in background").

## Round 70: display options, glyphs, rebinding, IL2CPP (added 2026-11-16)

- **Stills of the options pages and glyphs:** `bash Tools/PlayTest/r70_shots.sh` (back up `progress.json` first; delete the
  `settings.json` and `.bak` the run writes if none existed before). It captures every still twice, at 1920x1080 and
  1280x720 (`r70_still.cs`, argument `"<png> <width> <height>"` in `look_arg.txt`; the camera target is set before the canvases
  update, so the 720p still shows the real 720p layout).
- **A virtual pad of any family:** write `DualSenseGamepadHID`, `XInputControllerWindows` or `SwitchProControllerHID` to
  `Tools/PlayTest/r70_pad_arg.txt` and run `eval_file --file Tools/PlayTest/r70_pad.cs` (adds the pad and clicks its right
  stick, so prompts switch to that family; `remove` takes the virtual pads away). `r70_press.cs` presses one control of it
  (`buttonNorth` in `r70_press_arg.txt`) and reports where Confirm and Speed sit; `r70_rebind.cs` (`start`, `cancel`,
  `probe` in `r70_rebind_arg.txt`) drives the controls page. Queued state events must be written per control
  (`StateEvent.From` + `WriteValueIntoEvent`); a whole `GamepadState` fails on pads with their own state format (XInput).
- The Input System's own rebinding timeout (`WithTimeout`) ended the wait at once in the unfocused editor; the controls
  page times the wait itself in unscaled time (8 s).
- **Button prompt tables only:** `unity command menu --path "Tools/Generate/Button Prompt Tables"` (Xbox, PlayStation,
  Nintendo and keyboard tables onto the UI skin; the full generator runs the same step).
- **IL2CPP Windows build:** `Tools/Build/Windows Player (IL2CPP)` or, editor closed (or from a clone),
  `& $unity -batchmode -quit -projectPath . -executeMethod Game.Editor.BuildScript.BuildWindowsIl2Cpp -logFile Logs\build_il2cpp.log`.
  Writes `Builds/WindowsIl2Cpp/TowerDefense.exe`; sets the IL2CPP backend and managed stripping "Low" for that build only
  and puts both back after it, so the everyday build stays Mono. `Assets/link.xml` keeps `Game.Economy`, `Game.Platform` and
  `Unity.InputSystem` whole (JsonUtility save types, binding strings and override JSON). **Not built yet (2026-11-16):** this
  editor has only the Mono player variations, and the machine has no Visual Studio C++ tools or Windows SDK. Install both:
  `"C:\Program Files\Unity Hub\Unity Hub.exe" -- --headless install-modules --version 6000.6.5f1 -m windows-il2cpp` and
  `winget install Microsoft.VisualStudio.2022.BuildTools --override "--add Microsoft.VisualStudio.Workload.VCTools --includeRecommended --passive"`.
  Then build from a clone (`r34b_editmode_worker.sh` shows how a clone is synced) so the open editor is never touched, and
  smoke-test the player headless-ish: `Builds\WindowsIl2Cpp\TowerDefense.exe -screen-fullscreen 0 -screen-width 1280
  -screen-height 720 -logFile Logs\player_il2cpp.log`, close it after the main menu, and search the log for
  `MissingMethodException`, `TypeLoadException`, `ExecutionEngineException` and `NullReferenceException`.

## Round 71: Nintendo confirm swap, button tokens, options in the pause menu, IL2CPP build (added 2026-11-16)

- **Stills and pad checks:** `bash Tools/PlayTest/r71_shots.sh` (back up `progress.json` first; delete the `settings.json`
  and `.bak` the run writes if none existed before). It adds a virtual Switch Pro pad (`r70_pad.cs`), prints where confirm
  and cancel sit for the game and for the menus (`r71_probe.cs`), presses real queued buttons (`r70_press.cs`: B on the
  controls page goes back, A advances a dialogue, B skips it), shows a real dialogue asset with a button token
  (`r71_dialogue.cs` with the asset name in `r71_dialogue_arg.txt`, `r71_goto_line.cs` to its second line), and opens the
  options from the pause menu (`r71_pause.cs` with `on`/`off` in `r71_pause_arg.txt`). Removing a virtual pad pauses the
  level (lost controller), so the harness resumes it each time.
- **Pad family in code:** `InputSystemProvider.UsePadLayout(PadLayout.Nintendo)` switches names, glyphs and the confirm
  convention as if that pad were picked up (tests use it; a Steam Input layer could too).
- **IL2CPP Windows build plus smoke run, from a clone:** `bash Tools/PlayTest/r71_il2cpp_build.sh <N> [seconds]` mirrors the
  project into worker clone wN (own product name, audio off), builds with `Game.Editor.BuildScript.BuildWindowsIl2Cpp`
  in batch mode (the open editor is untouched), reports time and size, starts the player windowed at 1280x720 for the
  given seconds, closes it and greps `Logs/player_il2cpp.log` in the clone for exceptions. The player writes its saves
  under `LocalLow/DefaultCompany/Tower defense worker wN`, never Remco's folder.

## Round 72: credits, crash log, IL2CPP smoke test (added 2026-11-16)

- **Credits data only:** `unity command menu --path "Tools/Generate/Credits"` writes `Data/Menu/Credits.asset` (sections of
  `credits.*` keys plus the two OFL texts) and sets it on the Boot scene's bootstrapper (it opens Boot on its own, saves it and
  restores the scenes that were open). The full generator runs the data step before the scenes and wires it in `CreateBootScene`.
- **Stills of the menu (version corner) and the credits screen:** `bash Tools/PlayTest/r72_shots.sh` (back up `progress.json`
  first; delete the `settings.json` the run writes). `r72_credits.cs` (`probe`, `scroll <v>`, `stick <y>` in
  `r72_credits_arg.txt`) reads and drives the page; `r72_selected.cs` prints the focused UI object.
- **Crash log:** players (not the editor) append every logged exception to `crash.log` in `Application.persistentDataPath`
  (`%USERPROFILE%\AppData\LocalLow\<company>\<product>\crash.log`), with version, scene, level and UTC time; 256 KB, then it is
  rotated to `crash.1.log`; at most 10 entries per minute, the next entry says how many were left out. A tester sends that file
  plus `Player.log` from the same folder.
- **IL2CPP smoke build and scripted run:** `bash Tools/PlayTest/r72_il2cpp_smoke.sh <N> [skip-build]` mirrors the project into
  worker clone wN, builds `Game.Editor.BuildScript.BuildWindowsIl2CppSmoke` (IL2CPP plus the `TD_SMOKE_TEST` define, output
  `Builds/WindowsIl2CppSmoke`), empties the clone player's own save folder (`LocalLow/DefaultCompany/Tower defense worker wN`),
  runs `TowerDefense.exe -autotest` (windowed 1280x720) until it quits by itself, then `-autotest-crashprobe` (one deliberate
  exception that must land in `crash.log`), and prints the `[AutoTest]` lines, the exception count and the crash log. The
  route and its checks live in `Game.UI.SmokeTestDriver`; a summary is also written to `autotest.txt` in the player's data
  folder. `-autotest` does nothing in a release build (`Game.Core.SmokeTestGate`: editor, development or smoke build only).
- **Shader keepers only:** `unity command menu --path "Tools/Generate/Shader Keepers"` writes the keeper materials (every
  shader the game finds by name at runtime) and puts the whole list on the Boot scene. Run it whenever a new `Shader.Find`
  name is added to `CreateShaderKeepers`; `ShaderKeeperTests` fails until then. A missing keeper only shows in a player build.

## Round 73: call next wave (added 2026-11-16)

- **Wave call rules only:** `unity command menu --path "Tools/Generate/Wave Call"` writes `Data/Levels/WaveCall.asset` (coins per
  second saved, cap per call, cooldown, whether the first wave may be called) and sets it on the campaign catalog. The full
  generator runs the same step after "Campaign". The numbers live in `ProjectGenerator.WaveCall.cs`.
- **A PlayMode test on a silent clone:** `bash Tools/PlayTest/r73_playmode_worker.sh <N> <TestFilter>` syncs clone wN (own
  product name, audio off), runs the filter headless and prints the results and the save MD5 before and after.
  `Game.Tests.WaveCallPlayTests` (about 30 s) plays the call in Landing Site; `Game.Tests.EarlyCallProbeTests` (about 3 min)
  plays the best Landing Site Easy plan waiting and calling every wave early on seeds 1-3 and writes
  `Tools/PlayTest/r73_early_call.txt` in the clone (copy it back).
- **Probe switch:** `BalanceProbeTests.CallEarly = true` makes the probe bot call every wave as soon as the level accepts it;
  off by default, so the balance pipeline keeps the waiting player as its baseline.
- **Stills of the HUD as the game shows it:** `Tools/PlayTest/r73_still.cs` (same `look_arg.txt` as `r70_still.cs`) renders the
  field with its post stack and the canvases on their own camera without post, blended on the CPU, so the corner icons are
  not vignetted. `bash Tools/PlayTest/r73_shots.sh` (back up `progress.json`, delete the `settings.json` it writes) makes the
  `r73_*` stills; `r73_call.cs` reads and drives the call (`probe`, `flush`, `call`, `fast`, `key <name>`, `tap <control> <ms>`).
  `r73_pad_check.sh` checks that a View tap calls and a 0.4 s hold does not.
- Mind: a key press queued while the editor is paused by `step_frames.cs ... hold` is not delivered to the game; step once
  without `hold` before a press.

## Balance stage 1: headroom (added 2026-11-16)

The measured replacement for the paper verdict (Docs/BALANCE_METHOD_REVIEW.md, results and method in Docs/BALANCE_HEADROOM.md):
per level, difficulty, reference player and seed, the largest uniform multiple of the creatures' health that player still
wins at, bisected in the real game. Test and tool code only; no data is written and the old pipeline is untouched.

- **One command:** `bash Tools/PlayTest/headroom.sh "0,2" "0,1,2,3"` (campaign nodes, difficulties). It refreshes the worker
  clones (`C:\Users\remco\TowerDefenseWorkers\wN`, silent, own save folder), deals the units over them, runs
  `Game.Tests.HeadroomProbeTests.Measure_Headroom` headless, appends every run and edge to
  `Tools/PlayTest/headroom_records.txt` and writes the table `Tools/PlayTest/headroom.txt`. The open editor is not used;
  it may stay open, in or out of Play Mode. The save's MD5 is checked before and after.
- **What it plays by default:** the ladder (`careless`, `sensible`, `skilled`) on seeds 1-3, plus the mono rows (`mono`) and
  the sensible player on a plain save (`fresh`) on seed 1. Env: `HR_POLICIES`, `HR_SEEDS`, `HR_EXTRA`, `HR_EXTRA_SEEDS`,
  `HR_PRECISION` (0.03), `HR_WORKERS` (default `workers.sh count`, 5 here), `HR_FIRST` (first clone), `HR_RECORDS`.
- **Exact units:** `HR_UNITS="0:1:sensible:1,2:3:mono-Cannon:1" bash Tools/PlayTest/headroom.sh` (node:difficulty:policy:seed).
- **Monotonicity scan:** `HR_SCAN="0.7,0.8,0.88,0.94,1.06,1.13,1.25,1.45"` plays those multiples of each measured edge after
  the bisection; the report counts the win/lose crossovers per unit and pools the runs into an outcome band.
- **Rescale check / what-if on a cell's scale:** `HR_BASE="2:1:1.28"` multiplies that cell's health by the factor before the
  multiple (a runtime copy of the level; no asset changes). The report lists such cells against their unscaled H.
- **Only the table again:** `bash Tools/PlayTest/headroom.sh report` (or `python Tools/PlayTest/headroom_report.py <records> <report>`).
  A unit measured twice is kept twice in the records: the table takes the last, the "Reruns" section compares them.
- **Cost:** about 16 s a run, 7 runs a bisection (13 with the scan); two levels, four difficulties, the default set with
  the scan is 88 units, about 75 minutes on five clones.
- **In the editor instead** (not needed normally): write `Tools/PlayTest/headroom_arg.txt` (`units=`, `precision=`, `scan=`,
  `base=` lines, see `HeadroomProbeTests`) and run the explicit test; the probe installs an in-memory save, but back up
  `progress.json` first as for any Play Mode run.
- If a batch is stopped: kill the clones' Unity (command line contains `TowerDefenseWorkers`) and delete
  `TowerDefenseWorkers\wN\Temp\UnityLockfile`, as for the other worker scripts.

## Balance stage 1b: pressure (added 2026-11-17)

The continuous sibling of the headroom probe (results and the definition in Docs/BALANCE_HEADROOM.md, "Stage 1b"): a reference
player plays a level at a few fixed multiples of the creatures' health; nothing is bisected. Per run the records keep, per
wave, the health spawned, that health weighted by how far each creature got, the health that reached the base, base and gate
damage, and the build order taken. Test and tool code only; `headroom.sh` is untouched and still works beside it.

- **One command:** `bash Tools/PlayTest/pressure.sh "0,2" "0,1,2,3"` (campaign nodes, difficulties). It refreshes the worker
  clones (silent: `m_DisableAudio: 1` is set and checked in each clone; own save folder), deals the units over them, runs
  `Game.Tests.PressureProbeTests.Measure_Pressure` headless, appends every run to `Tools/PlayTest/pressure_records.txt` and
  writes the report with the stop rules to `Tools/PlayTest/pressure.txt` (`pressure_report.py`). The open editor is not used.
  The save's MD5 is checked before and after.
- **Round 76 changed the defaults and the skilled rung** (seeds 1-3, multiples 0.8 to 1.25, no extra rows, `skilled` adaptive,
  the frozen script is `script`): see "Round 76" below. What follows describes stage 1b and round 74.
- **What it plays by default:** the rungs `skilled` (the best replayed plan as a fixed script) and `sensible` (the reactive
  bot, repaired in round 74) on seeds 1-5 at the multiples 0.5, 0.65, 0.8, 1, 1.25, 1.6, 2, plus the Cannon row and the
  careless bot on node 0. The `typical` rung of stage 1b (the sensible bot's seed 1 build order as a fixed script) is no
  longer in the default; `PR_POLICIES="skilled,typical,sensible"` still plays it. Env: `PR_POLICIES`, `PR_SEEDS`,
  `PR_MULTIPLES`, `PR_EXTRA` (`node:policy,...`, `""` for none), `PR_WORKERS` (default `workers.sh count`), `PR_FIRST`, `PR_RECORDS`.
- **Exact units:** `PR_UNITS="0:1:skilled:3,2:0:sensible:1" PR_MULTIPLES="0.5,1" bash Tools/PlayTest/pressure.sh`
  (node:difficulty:policy:seed; policies also `careless`, `fresh`, `mono-<TowerId>`).
- **The acceptance reading proposed in stage 1b** (seeds won at three multiples):
  `PR_MULTIPLES="0.8,1,1.25" PR_EXTRA="" PR_POLICIES="skilled,sensible" bash Tools/PlayTest/pressure.sh "<nodes>" "<difficulties>"`.
- **Only the report again:** `bash Tools/PlayTest/pressure.sh report` (or `python Tools/PlayTest/pressure_report.py <records> <report>`).
  A unit and multiple measured twice: the report takes the last record.
- **Cost:** about 15 s a run; the default set on two levels is 160 units x 7 multiples = 1120 runs, 58 minutes on five clones.
- The stop rules (monotone share, seed spread) are constants at the top of `pressure_report.py`; they were fixed before the
  stage 1b batch and should not be tuned to a result.
- If a batch is stopped: kill the clones' Unity (command line contains `TowerDefenseWorkers`) and delete
  the clone's `Temp/UnityLockfile`, as for the other worker scripts.
- Pitfall met while writing it: the Windows paths in `pressure.sh` need the same doubled backslashes as in `headroom.sh`;
  with single ones robocopy gets a wrong path, returns 16 and the script stops at the sync. Unlike `headroom.sh`, the sync
  step here fails loudly when a clone could not be refreshed.

## Round 74: the repaired sensible bot, seeds won, authored nests (added 2026-11-17)

- **The reading:** `PR_RECORDS=Tools/PlayTest/<name>_records.txt PR_MULTIPLES="0.8,1,1.25" PR_EXTRA="" bash Tools/PlayTest/pressure.sh "0,2" "0,1,2,3"`
  (240 runs, about 14 minutes on five clones), then `python Tools/PlayTest/pressure_wins.py <records>` for the seeds won per
  rung, cell and multiple; with a second records file it prints before -> after side by side; `--bot` lists the sensible
  bot's counter purchases per seed (first Rocket wave, Rockets and Cannons by wave 14), the purse at the start of waves
  10 to 14 and the idle-counter note. Keep a records file per measurement (`PR_RECORDS`); the report takes the last record
  of a unit and multiple, so mixing rounds in one file hides the earlier one.
- **New record fields** (after the note, before the worker and the batch): the purse at the first look of every wave
  (`wave=coins;...`) and the idle-counter note (`Trait=coins/price` when a counter trait was alive at the end and its
  counter's price was unspent). `pressure_report.py` reads both layouts.
- **The sensible bot's rules** live in `BalanceProbeTests.SensibleAct` (PlayMode test code): living flyers and plated
  creatures count as coming; the counters of a trait grow (a tower, then an upgrade, in turn) after a wave in which the base
  or a gate lost health to it or when the preview shows half as many again; the trait furthest behind is served first;
  while it saves for a counter only a wrecked defence gate is repaired; a marked rift behind the defence gate gets a gun at
  the gate its walkers reach, one wave ahead.
- **An authored nest:** `Join(..., Nest, delay, nest: n)` in a level spec sends every rift group of that troop through nest n
  of the level (counted from 1 in `LevelDefinition.Nests`); it lands in the wave data as `SpawnGroup.Nest` (0 = drawn). The
  level validator asks for it when at most `LevelValidator.SmallNestGroup` walkers would draw one of several nests.
  `unity command eval_file --file Tools/PlayTest/nest_join_probe.cs` lists every level's nests, the road cell each joins at,
  ties and which lie behind the defence gate.
- **The solve stage keeps the string keys** (fixed in round 75): `CreateDifficulty` writes a localised field through
  `LocalisationTables.KeyOrText`, which keeps the key of round 69 when the table already holds the same text. Until then the
  "Difficulties" step of `ApplyRound68.cs` / `BalanceStages.SolveWaves` wrote the plain names over the keys and the four files
  had to be restored from git after every solve.
- **Replaying a level's searched plans** after its data changed: `bash Tools/PlayTest/workers.sh run replay 5 "cells=0:0,0:1,0:2,0:3" "seeds=1,2,3"`
  replaces their records in `plan_records.txt`, and the pressure probe's `skilled` rung takes the best of them
  (`HeadroomProbeTests.BestRecordedPlan`). Mind that a lost replay leaves a cut-off build order: in round 74 the plans of
  Landing Site lost to the authored rift, a new search (`r34b_editmode_worker.sh N PlanSearchTests "cells=0:1"`) wrote the
  same plans, and both were rolled back with `git checkout -- Tools/PlayTest/plan_records.txt Tools/PlayTest/Plans`. The
  skilled rung reacts to a marked rift itself instead (`ProbeStrategy.ScriptedGuardingRifts`).

## Round 75: the other levels of the first continent (added 2026-11-17)

- **Nodes:** 0 Landing Site (C1_01), 1 Glass Meadows (C1_02), 2 Shard Ridge (C1_03), 3 Fault Line (C1_04), 4 Quartz Flats
  (C1_05), 5 Crysium Vein (C1_06).
- **The baseline of a level set:** `PR_RECORDS=Tools/PlayTest/<name>_records.txt PR_MULTIPLES="0.8,1,1.25" PR_EXTRA="" bash Tools/PlayTest/pressure.sh "1,3,4,5" "0,1,2,3"`
  (480 runs, 32 minutes on five clones; the later levels take about 20 s a run).
- **One-tower rows** on Hard and Psycho at the data: `PR_UNITS="1:2:mono-Cannon:1,1:2:mono-PulseRifle:1,1:2:mono-Laser:1,..." PR_MULTIPLES="1" PR_RECORDS=... bash Tools/PlayTest/pressure.sh`
  (24 units for four levels, 3 minutes). The Gun Turret's id is `PulseRifle`, the Flak's `Rocket`.
- **A scale of one cell** (the builder's lever): a line `C1_04 1 0.82` in `Tools/PlayTest/wave_tuning.txt` (level key, difficulty,
  target scale), then the solve stage alone:
  `unity command run_script --file Tools/PlayTest/Scripts/ApplyRound68.cs --entry ApplyRound68.Run --timeout_ms 900000 --timeout 960`
  (8 seconds; check `editor_status` first). It rewrites only the wave assets whose numbers moved and the level's
  `_balanceTargetScale`. Mind two things: the scale does not act linearly on Medium (1 -> 0.82 moved Fault Line Medium's
  wave multipliers by x0.756, 0.87 -> 0.7 moved Crysium Vein's by x0.68; Hard followed its scale exactly), and a wave asset
  written for the first time since round 74 gains a `Nest: 0` line on every group (no change in the game).
- **Health per coin of every cell** (a reading aid, not a gate): `python Tools/PlayTest/health_per_coin.py [waves]` sums, per cell, the health of
  every creature but the bosses (count x difficulty count x health x wave multiplier x difficulty health) over the kill coins
  plus the starting money.
- **A recorded plan against today's data:** `bash Tools/PlayTest/workers.sh run replay 3 "cells=4:2" "seeds=1,2,3"` plays the
  cell's adaptive strategies; it rewrites `plan_records.txt` and `plan_replay_partial.txt`, so restore both from git when the
  replay was only a check. The pressure probe's `skilled` rung is not that replay: it plays the build order of the best
  strategy's lowest winning seed as a frozen script (see STATUS round 75, Quartz Flats Hard).
- **While the clones run, the open editor answers slowly:** `eval_file` timed out twice ("Main thread operation timed out after
  5000ms") during a batch and ran at once after it. Run editor steps between batches.

## Round 76: a frozen bot, stamped records, affordability, the adaptive skilled rung (added 2026-11-17)

- **The rule:** a batch changes the bot or the data, never both. The probe is deterministic, so every difference between two
  records of one unit is an effect of a change (bot, data or game code), never noise. There is no "one or two seeds are
  noise" rule (round 75 had one; it was wrong).
- **The stamp:** every pressure record ends (before the worker and the batch) with `bot=<version>:<hash>;data=<hash>;game=<hash>`
  (`Assets/Tests/PlayMode/ProbeStamp.cs`). The bot hash covers the probe's code (`BalanceProbeTests`, `BotRun`, `ProbeStrategy`,
  `ProbePlayer`, `PressureProbeTests`, `HeadroomProbeTests`), `plan_records.txt` and `Plans/*.json` (what the skilled rung is
  picked from and plays); data is `Assets/_Project/Data/**/*.asset`; game is `Assets/_Project/Scripts/**/*.cs`. Bump
  `ProbeStamp.BotVersion` with every change of a reference player's rules; the hash catches a change that forgot to.
  **Bot version 2 is frozen** (the stamp of the round 76 measurement is in STATUS, "Round 76"): do not touch those files
  for a measurement that must be comparable with it.
- **The tools refuse to mix bots:** `pressure_wins.py <after> <before>` refuses two files measured with different or unknown
  (unstamped: before round 76) bots, and says whether the data differs too; `pressure_report.py` (and so `pressure.sh`)
  refuses a records file that holds more than one bot. `--mixed-bots` prints anyway, with a warning. Keep one records file
  (`PR_RECORDS`) per bot and data state. The rule itself is in `Tools/PlayTest/pressure_stamp.py`.
- **The reading since round 76** (the defaults of `pressure.sh`): rungs `skilled,sensible`, seeds 1-3, health multiples
  0.8, 0.9, 1, 1.12, 1.25 (health jitter instead of more seeds), no extra rows:
  `PR_RECORDS=Tools/PlayTest/<name>_records.txt bash Tools/PlayTest/pressure.sh "0,1,2,3,4,5" "0,1,2,3"` (720 runs, about 75
  minutes on five clones), then `python Tools/PlayTest/pressure_wins.py <records>`; `--sales` counts the sensible bot's runs
  with a sale per cell.
- **The rungs:** `skilled` is the cell's best searched strategy (`HeadroomProbeTests.BestRecordedStrategy`: the strategy
  behind the best plan of `plan_records.txt`, read from `Plans/`) played adaptively, with a gun at a rift behind the defence
  gate a wave ahead (`ProbeStrategy.AdaptiveGuardingRifts`). `script` is the skilled rung of rounds 74 and 75 (that
  strategy's best recorded build order as a frozen script); it is not in the default any more. `sensible` is bot version 2
  (`BalanceProbeTests.SensibleAct`): it knows the level's Codex roster (`LevelRoster`) from second zero and no wave preview;
  with plated creatures in the roster its first gun is the Cannon, with flyers in the roster it keeps a Flak's price back
  until a wave's bounties pay for one; a counter is bought when its trait is met on the field; a gun is sold only when
  flyers are biting the base, nothing that can aim up reaches it and the purse is short.
- **Another data state with the same bot, without touching the project:** `PR_OVERLAY=<folder>` copies that folder's files
  over every clone after the sync (same relative paths, e.g. `Assets/_Project/Data/Waves/Waves_C1_04.asset` taken with
  `git show <rev>:<path>`). The records carry that state's data stamp. The next `pressure.sh` or `workers.sh` sync restores
  the clones.
- **A run's per-wave trace for a diagnosis:** add `keeptrace=1` to the clone's `pressure_arg.txt` (or run a single unit by
  hand): the probe's `Logs/probe_trace.txt` is then kept instead of deleted after each unit.
- **Pitfall found this round: the first frames of a level ran on the wall clock.** `BalanceProbeTests.Probe` set the fixed
  step after the level scene had loaded, so the frames in between took as long as they took and the first wave started a
  fraction of a step earlier or later against the bot's buying beat. The same unit played twice (in another place of a
  worker's list) came out as two different games (Quartz Flats Hard, skilled, seed 1: 2224, 2130 and 2105 coins earned in
  three plays). The step is now set before the scene loads. Check after any change of the probe loop:
  `PR_RECORDS=Tools/PlayTest/<name>_records.txt PR_WORKERS=2 PR_UNITS="3:1:sensible:1,4:2:script:1,4:2:skilled:1,3:1:sensible:1,4:2:script:1,4:2:skilled:1" PR_MULTIPLES="1,1.12" bash Tools/PlayTest/pressure.sh`
  plays three units twice, once early and once late in a clone's list; the two records of each must be identical.
  Records of before round 76 carry that wobble: identical batches reproduced (same list, same places), re-dealt ones need not.
- **Affordability of the first counter tower, from data alone:** `Game.Levels.CounterAffordability` (no bot, no model): per
  cell and counter trait the starting money and the bounties of every wave before the deadline (the trait's first wave plus
  the grace waves) against the cheapest owned counter's price. `Tools/Validate/Counter Affordability (first continent)` in
  the editor, or the EditMode test `CounterAffordabilityTests` (in the suite; `bash Tools/PlayTest/r34b_editmode_worker.sh 6 CounterAffordabilityTests`,
  then copy `Tools/PlayTest/affordability.txt` back from the clone), writes the table and logs every cell that is not
  payable from bounties as a `[Validator]` warning. A warning, not a failing test.
- **What a builder scale did to the waves, bosses included:** `python Tools/PlayTest/wave_health.py C1_06 [revision]` lists
  per difficulty the waves whose health multiplier or boss scale moved since the revision, and the product a wave boss
  carries. A wave boss (no continent-boss definition) carries wave multiplier x `_bossHealthScale`; the solve re-sizes that
  scale when the builder scale moves, so the boss does not follow the crowd (see STATUS round 76).
- **Never edit a script while it runs.** `pressure.sh` was changed during the 720-run batch; bash reads a script as it goes
  and stopped at the collection step with a syntax error. The runs were complete in the clones
  (`TowerDefenseWorkers\wN\Tools\PlayTest\pressure_records.txt`; the next `pressure.sh` deletes them, so collect first):
  `for i in 1 2 3 4 5; do sed "s/\$/|w$i|<batch start, epoch seconds>/" <clone>/Tools/PlayTest/pressure_records.txt >> <records>; done`,
  then check each clone's `Logs/results.xml` for `result="Passed"` and its `Logs/unity.log` for `[Probe] error`.
- **The old gates on a moved cell without moving the pipeline's inputs:** replay (`workers.sh run replay 5 "cells=3:2,5:1" "seeds=1,2,3"`),
  `r34b_editmode_worker.sh 6 BalanceAcceptanceTests` and copy the clone's `acceptance.txt` aside; measure
  (`workers.sh run measure 5 "cells=3:2,5:1" "seeds=1,2,3"`), `GraceReport.cs` and compare `grace_windows.txt`;
  `finale_workers.sh` and copy `finale_probe.txt` aside. Then `git checkout -- Tools/PlayTest/plan_records.txt Tools/PlayTest/plan_replay_partial.txt Tools/PlayTest/calibration_records.txt Tools/PlayTest/yardstick_partial.txt Tools/PlayTest/grace_windows.txt Tools/PlayTest/finale_probe.txt`:
  `plan_records.txt` is part of the bot stamp, the others feed the calibration.

## Round 77: structure damage factor, first-flyer rule, boss factor, Rotbloom (added 2026-11-17)

- **How the levels end (the all-or-nothing share):** `python Tools/PlayTest/pressure_endings.py <records> [<records before>] [--cells 3:1,4:1] [--difficulties 0,1]`
  prints, per file, rung and difficulty: runs won by how much base they keep, runs lost in the last wave, and the share of
  lost runs that fell in the last wave from a base at 90 % or more. Same stamp rule as `pressure_wins.py`.
- **A difficulty factor measured before it is in the data:** copy `Assets/_Project/Data/Difficulty/Medium.asset` into an
  overlay folder (same relative path), change the number there and run `PR_OVERLAY=<folder> PR_RECORDS=... bash Tools/PlayTest/pressure.sh "0,1,2,3,4,5" "1"`
  (180 runs, 15 minutes). The project is not touched; the final value goes in through `BalanceSheet` and the solve stage.
- **The structure damage factor** is `BalanceSheet.EasyStructureFactor` / `MediumStructureFactor` -> `DifficultyProfile._structureDamageFactor`
  (the solve stage writes the profiles too). The wave solver reads `CalibratedStructureDamage` (the multiplier without the
  factor), so changing the factor moves no wave. Changing `_structureDamageMultiplier` itself re-solves every difficulty of
  every level (Medium is the solver's base curve): do not use it as a softness lever.
- **The first-flyer rule** is `BalanceRules.FlyersAfterBounties` (Easy to Psycho), applied by `CampaignModel.BuildWaves`
  at every solve; `CounterAffordabilityTests.FirstFlyers_WhereTheRuleHolds_ArePayableFromBounties` fails when the stored
  waves break it. The purse rule both sides share is `Game.Balance.CounterPurse`.
- **A rule tried on paper before the solve:** `Tools/PlayTest/r64_lab_arg.txt` with `rule.<Field>=...` lines and
  `BossLab.Run` (round 64) now also takes `bool[]` fields (`rule.FlyersAfterBounties=false,false,false,false`). It prints
  per cell how many waves would move against the stored set and every level boss with what it asks alone; no asset is written.
  Restore the arg file with `git checkout` afterwards.
- **After a `git checkout` of an asset the open editor still holds the old version in memory** until it refreshes: a report
  script then reads data that is not on disk (the grace report mixed two wave sets this round). Run
  `unity command eval_file --file Tools/PlayTest/refresh_assets.cs` first.
- **Never run a generator script right after an edit without reading `compilationFailed`:** `run_script` runs on the last
  good assemblies when the compile failed, so the solve wrote the previous code's waves this round. Check
  `unity command console_status --result-only` for `"compilationFailed": false` in the same command chain.
- **The grace gate after a difficulty or wave change:** `bash Tools/PlayTest/workers.sh run measure 5 "difficulties=0,1" "seeds=1,2,3"`
  (12 cells, 8 minutes), then `GraceReport.cs`. `CombatRulesTests` reads `calibration_records.txt`, so the measured records
  stay in the tree this time (they are the rows of today's data).
- **Rotbloom:** `Tools/PlayTest/Scripts/R77Rotbloom.cs` writes the asset's health (28200, as the generator's spec); then
  `bash Tools/PlayTest/finale_workers.sh` (24 cells, 5 minutes).

## Round 78: next-wave preview, bot version 3, the skilled rung's picks (added 2026-11-17)

- **The preview's stills:** `bash Tools/PlayTest/r78_shots.sh <node> <name> <wave> [<name> <wave> ...]` plays the campaign node
  on Medium from Boot and writes `ArtSource/Previews/QA/r78_<name>_1080.png` and `_720.png` with the HUD as it stands when
  wave `<wave>` is the next one (1 = the opening countdown); it walks there with the developer fast-forward and kills every
  creature on the way (`r78_preview.cs fast`). `R78_CAPTURE=1` also saves the Game view itself (`capture_game_view`) as
  `r78_<name>_gameview.png`; `R78_LOW=1` plays on the Low (Switch) profile; `R78_MARGIN=5` lays the HUD out inside a 5 %
  safe margin (`r78_margin.cs`). Back up `progress.json` first and delete the `settings.json` the run writes. Round 78's set:
  `r78_shots.sh 0 opening 1 plated 7 boss 12`, `R78_CAPTURE=1 r78_shots.sh 5 flyers 7 six 18` (and `five 13`),
  `R78_LOW=1 R78_MARGIN=5 r78_shots.sh 5 low_five 13 low_six 18`.
- **Mind the stills' blur:** `r73_still.cs` draws the canvases through the renderer's full-screen tilt-shift pass, so the HUD in
  a High-profile still is softer than in the game; the Low-profile stills and the `_gameview` captures show it as sharp as it is.
- **What the preview lists:** `unity command eval_file --file Tools/PlayTest/r78_preview.cs` with `Tools/PlayTest/r78_preview_arg.txt`
  = `probe` (the wave, the preview's entries in the HUD's order, how many have a chip, the strip's place against the icon
  row), `list` (the preview of every wave of the running level) or `fast`.
- **The preview's data:** `LevelContext.NextWave` (set by the level controller) through `Game.Levels.WavePreview.Collect`; the HUD
  (`UI/WavePreviewView.cs`) and the sensible bot (`BotRun.Preview`) read exactly that list. EditMode: `WavePreviewTests`
  (`bash Tools/PlayTest/r34b_editmode_worker.sh 6 WavePreviewTests`); its last test fails when a wave of the first
  continent lists more creature types than the HUD has places at the default margin (six).
- **Bot version 3** (`ProbeStamp.BotVersion`): the sensible rung reads the preview (a flyer or plated mark in the next wave
  counts as met, and its number sizes the counters); both rungs put one sky gun on the free cell nearest a flyer that
  bites the base out of reach of every sky gun (bought, else a sky gun moved, else paid by a sale; `BalanceProbeTests.CoverBaseSky`);
  the skilled rung plays the pick of `Tools/PlayTest/skilled_picks.txt`. A record's note says `base sky gun w12 bought` when
  the rule acted. The skilled rung still knows the level (its ledger of the coming waves); only the sensible rung is held
  to what the HUD shows.
- **Refreshing the skilled rung's picks** (after any wave, tower or bot change; it makes a new bot stamp, so never inside a
  measurement):
  1. `PR_RECORDS=Tools/PlayTest/skilled_picks_<round>_records.txt PR_POLICIES="strategy1,strategy2,strategy3,strategy4,strategy5" PR_MULTIPLES="1" bash Tools/PlayTest/pressure.sh "0,1,2,3,4,5" "0,1,2,3"`
     plays every searched strategy of every cell (`Tools/PlayTest/Plans/<level>_<difficulty>_strategyN.json`) as the skilled
     rung plays one, on seeds 1-3 at the data (360 units, 309 runs; a cell without a strategyN writes a `pskip` line).
  2. `python Tools/PlayTest/skilled_picks.py Tools/PlayTest/skilled_picks_<round>_records.txt` prints every candidate per cell
     (seeds won, base kept, stars) with the pick starred; add `--write` to write `Tools/PlayTest/skilled_picks.txt`. The rule:
     most seeds won, then most base health kept over the seeds, then stars, then the lowest number.
  3. The pick file is in the bot stamp (`ProbeStamp.BotFiles`): measure with it and keep its records apart from the old pick's.
  A cell without a line in the file falls back to round 68's `plan_records.txt` (as bot version 2 did).
- **Two measurements side by side as a Markdown table:** `python Tools/PlayTest/pressure_versus.py <records before> <records after> [--mixed-bots]`
  (per cell and rung the seeds won at every multiple, moved rows in bold, runs won at the foot). Round 78's measurement:
  `Tools/PlayTest/pressure_r78_v3_records.txt`, stamp `bot=3:5bddeb15; data=645e4d4c; game=64e52f85`.
- **Version 2 against version 3:** `python Tools/PlayTest/pressure_wins.py <v3 records> Tools/PlayTest/pressure_r77_final_records.txt --mixed-bots`
  (the override is meant for exactly this: the data is the same, the bot differs) and
  `python Tools/PlayTest/pressure_endings.py <v3 records> Tools/PlayTest/pressure_r77_final_records.txt --mixed-bots`.
- **Stopping a batch by hand:** there is no `pkill` in this Git Bash; stop the clones with
  `powershell -NoProfile -Command "Get-CimInstance Win32_Process -Filter \"Name='Unity.exe'\" | Where-Object { $_.CommandLine -like '*TowerDefenseWorkers*' } | ForEach-Object { Stop-Process -Id $_.ProcessId -Force }"`,
  delete `TowerDefenseWorkers\wN\Temp\UnityLockfile` and the half-written records file; `pressure.sh` then ends by itself.

## Round 79: the wave grammar (added 2026-11-17)

- **The design:** Docs/WAVE_GRAMMAR.md. The numbers are `Game.Balance.WaveGrammarRules` (one object, `BalanceRules.Grammar`); the
  picking rules `WaveGrammar` (pattern per wave, which waves carry a surprise), the cuts `WaveTimeline`, the layout itself in
  `WaveComposer.Compose`. A level's script keeps its roster and its rhythm (`P`, `R`, `H`; the old `M` is gone).
- **After any change of a grammar number or a script:** recompile, read `compilationFailed`, then the solve stage alone, as in
  round 75: `unity command run_script --file Tools/PlayTest/Scripts/ApplyRound68.cs --entry ApplyRound68.Run --timeout_ms 900000 --timeout 960`
  (17 seconds; `editor_status` first). Never the full generator.
- **The plan the grammar makes of every cell** (pattern, clusters, surprise and boss per wave), without writing an asset:
  `unity command run_script --file Tools/PlayTest/Scripts/WaveGrammarReport.cs --entry WaveGrammarReport.Run --timeout_ms 900000 --timeout 960`
  writes `Tools/PlayTest/wave_grammar.txt`. Letters: `s` stream, `C` spread clusters, `W` swarm, `T` tank escort, `X` combo
  swarm, `A` air raid; the number is the clusters; `!` a rift surprise, `^` a flank, `B` a boss. (It is a `run_script`, not
  an `eval_file`: solving six levels takes longer than the five seconds an eval gets on the main thread.)
- **The shape of the stored waves:** `python Tools/PlayTest/wave_shape.py` prints per cell the median and longest spawn span,
  the clusters (a pause of four seconds or more ends one; `--gap` changes that), the creature types per wave, the level's
  spawn seconds and its creatures; `--rev HEAD` reads a revision instead of the working tree (before against after);
  `--waves C1_05:1` lists every wave of one cell with its groups (level key, difficulty index).
- **The validator's rule** on the stored waves is `Game.Levels.WaveShape.Check` (silence between two spawns at most 16 s, a
  wave at most 120 s of spawning; the one-nest-per-wave rule went in round 81); `LevelValidator` runs it, so every test that
  validates a level does. The grammar's own tests: `bash Tools/PlayTest/r34b_editmode_worker.sh 6 WaveGrammarTests`.
- **Several test classes in one worker run:** the filter takes names separated by a semicolon
  (`r34b_editmode_worker.sh 6 "WaveGrammarTests;WorldContentTests;Round64Tests"`); `Game.Tests` runs the whole suite (about
  twelve minutes). `python Tools/PlayTest/editmode_failures.py` prints the totals and every failed test with its message
  from the clone's `Logs/results.xml`.
- **A quick reading before the full one:** `PR_RECORDS=Tools/PlayTest/<name>_records.txt PR_MULTIPLES="1" bash Tools/PlayTest/pressure.sh "0,1,2,3,4,5" "0,1,2,3"`
  (144 runs at the data), then `pressure_wins.py` and `pressure_endings.py` as before.
- **Pitfalls met this round:** a Bash heredoc whose text holds an odd number of apostrophes fails to parse in this shell even
  when its delimiter is quoted (write such a file with the editor tool, or a patch script from a file); a rift flyer in a
  surprise before the difficulty's first raid makes the ledger's Easy player buy a sky gun out of a thin purse, and the
  human calibration then halves the whole level (Fault Line Easy fell to x0.4 of its health): rift flyers waited for the
  first raid in round 79 (`WaveComposer.Unlocked`); round 80 took that wait away and holds them on Easy with the purse
  floor of the Easy raid rule instead (see "Round 80").
- **Why a wave's multiplier is what it is (round 79, second half):** `Tools/PlayTest/Scripts/WaveLedgerLab.cs` solves a level in
  memory as the solve stage does and prints, per wave of the cells named in `Tools/PlayTest/wave_ledger_arg.txt`
  (`cell=C1_05:2`, one per line): the grammar's pattern, the multiplier of this solve against the stored one, the solver's own
  multiplier before the human calibration, the pressure with the hero against the cap, the purse, ground and flyer health,
  the ground window (`span`) and the flyers' window (`sky`), creatures, both pressures and what the ledger's player buys; at
  the foot per cell the sum of multipliers against the stored set and every wave that moved more than 3 %. `grammar.<Field>=`
  and `rule.<Field>=` lines override a number of `WaveGrammarRules` / `BalanceRules` for that run only (a float, an int or a
  comma list), `label=<name>` names the output `Tools/PlayTest/wave_ledger_<name>.txt`. No asset is written:
  `unity command run_script --file Tools/PlayTest/Scripts/WaveLedgerLab.cs --entry WaveLedgerLab.Run --timeout_ms 900000 --timeout 960`
  (four seconds a level; `editor_status` first). The grammar switched off for a comparison with round 78:
  `grammar.BeatSeconds=1000`, `SwarmBeatSeconds=1000`, `ComboBeatSeconds=1000`, `EscortBeatSeconds=1000`, `SurpriseFreeWaves=99`,
  `PauseCredit=1000`, `ComboFrom=2`, `BossBeats=1`, `FinaleBeats=1`, `EscortCatchCells=0`, `EscortTempo=1` (it gave round 78's
  multipliers of Quartz Flats Hard to three decimals on sixteen of seventeen waves).
- **A strategy searched on today's waves, tried without touching the project:** `bash Tools/PlayTest/r34b_editmode_worker.sh 6 PlanSearchTests "cells=1:3,4:2"`
  (a minute; the result line reads Skipped, the files are written), copy the clone's `Tools/PlayTest/Plans/Level_<key>_<d>*`
  into an overlay folder under the same relative path and play them as the skilled rung would:
  `PR_OVERLAY=<folder> PR_UNITS="1:3:strategy1:1,1:3:strategy1:2,..." PR_MULTIPLES="1" PR_RECORDS=Tools/PlayTest/<name>_records.txt bash Tools/PlayTest/pressure.sh`.
  The records carry the overlay's bot stamp. Round 79: `skilled_search_r79_records.txt`.
- **The skilled rung alone** (after a pick refresh, as its own batch): `PR_POLICIES="skilled" PR_RECORDS=Tools/PlayTest/<name>_records.txt bash Tools/PlayTest/pressure.sh "0,1,2,3,4,5" "0,1,2,3"`
  (360 runs, about 40 minutes). The sensible rung does not read the pick file, so its records of the same data stay valid;
  keep the two files apart (different bot stamps).
- **Pitfalls of the second half:** never `git checkout` a file to undo an experiment while a round is uncommitted: it throws
  the round's own changes away with the experiment (`WaveSolver.cs` was restored from a worker clone, which is a byte copy of
  the tree at its last sync: `cmp` against `TowerDefenseWorkers\wN\...` shows what an experiment left behind). A Python
  patch written through a Bash heredoc turns `\n` inside a C# string into a real line break; write C# with the editor tool.

## Round 80: no wait for the raid, raids capped at the guns held, a surprise lifts no wave (added 2026-11-17)

- **The three rules** are in Docs/WAVE_GRAMMAR.md ("Surprises", "A surprise lifts no other wave", "A raid is capped at the
  sky guns held"): `WaveComposer.Unlocked` (no wait for the first raid), `CampaignModel.BuildWaves` (Easy: the flying troops
  wait for `EasyRaidFloor`), `BalanceRules.RaidsCappedAtHeldGuns` and `SurprisesLiftNoWave` (`CampaignModel.SolveDifficulty`
  solves a difficulty up to three times and lowers raids and surprise-free waves to the other solves). The solve stage now
  takes about 32 seconds (was 17).
- **`WaveLedgerLab.cs` takes `bool` and `bool[]` rule fields** (`rule.FlyersAfterBounties=false,false,false,false`,
  `rule.RaidsCappedAtHeldGuns=false`, `rule.SurprisesLiftNoWave=false`, `grammar.Surprises=false`). With the first-flyer
  rule off the Medium line is the curve the other difficulties copy (raids where the script writes them): that is where
  to read why a raid a harder difficulty copies has the multiplier it has. Round 80's runs: `wave_ledger_r80_*.txt`
  (`noguard`: the guard gone and nothing else; `easyfloor`; `phantom` and `phantom_off`: Quartz Flats Medium with raids
  unmoved, grammar on and off; `lowest`: every wave at its lowest crossing, not kept; `gm` and `gm_nosurprise`: Glass
  Meadows Psycho with and without surprises).
- **Where the gates and the rifts lie on the road:** `unity command eval_file --file Tools/PlayTest/r80_gates.cs` prints per
  level of the first continent the main road's length in cells, the defence gate's index, every gate's index and the
  index each rift's walkers join at.
- **The build order of one run** is field 24 of a pressure record (`build:<tower>:<x>:<y>:<wave>;upgrade::<x>:<y>:<wave>;...`),
  the base damage per wave field 21, the gate damage per wave field 22:
  `awk -F'|' '$2=="Level_C1_02" && $3==3 && $4=="skilled" && $6==1 {print $5, $7, $21, $22, $24}' <records>`.
- **Waiting for a batch from a tool call:** a foreground `sleep` chain is cut off after about three minutes; run the batch in
  the background and poll its records file in loops of under three minutes.
- **Pitfall:** a rule that softens one kind of wave is answered by the human calibration (one common factor holds the plain
  mean on its target), so the other waves get tougher. Check the lab's foot ("sum of multipliers") for every cell before a
  solve; round 80's first try lost Quartz Flats Psycho that way.

## Round 80b: raids out of the plain mean, the relief bound, the guard gun, scales re-expressed (added 2026-11-17)

- **The rules** are in Docs/WAVE_GRAMMAR.md ("A raid is capped at the sky guns held", "A surprise lifts no other wave",
  "The guard gun of a rear rift"). In code: `BalanceChecks.PlainMean` (no air raid), `WaveSpec.AirRaid` /
  `WaveDefinition._airRaid` (written by the solve stage), `WaveSolver.BisectOn` (a raid's lowest crossing whenever
  `BalanceRules.RaidsCappedAtHeldGuns`), `CampaignModel.CalibrateToHuman` (a raid is never lifted above the solver's
  crossing), `CampaignModel.SolveDifficulty` (the calm solve and `BalanceRules.SurpriseReliefMost`), `KillPurse`
  (`RearRiftGuardCells`, `BooksRearGuard`, `RearGuardFromWave`). `BalanceRules.LowestRaidCrossing` and the raid's second
  solve of round 80 are gone; the solve stage takes about 27 seconds.
- **The human-curve gate on paper, before a solve:** `WaveLedgerLab.cs` now prints at its foot, per cell, the human
  surplus against the surplus the curve asks, with `OUT` where `BalanceLedgerTests` would fail, and per wave the
  human-lens pressure (`p` = a plain wave, `-` = left out of the mean). All 24 cells in one run:
  `cell=C1_01:0` ... `cell=C1_06:3` in `Tools/PlayTest/wave_ledger_arg.txt` (25 to 40 seconds).
- **Re-expressing the builder scales** after a change of what the curve is read on:
  `unity command run_script --file Tools/PlayTest/Scripts/ScaleRefit.cs --entry ScaleRefit.Run --timeout_ms 900000 --timeout 960`
  (about six minutes; run it in the background and wait for `Tools/PlayTest/scale_refit.txt`). Per cell it bisects the
  scale at which this solve gives the measured waves' sum of multipliers again, raids aside. The measured waves are
  the copies in `Tools/PlayTest/r80_waves/` (round 80's wave assets, taken from a worker clone before the first solve);
  `skip=C1_04:0` lines in `Tools/PlayTest/scale_refit_arg.txt` keep a cell's scale. The lines it proposes go into
  `Tools/PlayTest/wave_tuning.txt` by hand, then the solve stage. Mind: the search is not monotone everywhere (Fault Line
  Psycho jumps from x0.82 at scale 0.50 to x1.10 at 0.51); read the "sum" it prints for the fitted scale.
- **Ground and raid sums of the stored waves against round 80's:** the snippet in STATUS round 80b ("How the waves
  moved") reads `_healthMultiplier` and `_airRaid` from the wave assets and from `Tools/PlayTest/r80_waves/`.
- **Pitfalls met this round:**
  - Save the measured wave assets before the first solve of a round that is not committed (`cp` from the tree, or from
    `TowerDefenseWorkers\w1\Assets\_Project\Data\Waves` while the clones still hold the last sync): the solve stage
    overwrites them and git only has the round before.
  - A foreground command that runs longer than about two minutes is moved to the background by the tool; start long
    Unity scripts in the background and poll the file they write with sleeps under two minutes.
  - `eval_file` times out ("Main thread operation timed out after 5000ms") right after a refresh while the editor
    still reloads; `run_script` waits.
  - Leaving a kind of wave out of the plain mean shrinks the set the curve is held on: with raids and surprise waves
    both out, Glass Meadows Psycho, Fault Line Hard and Crysium Vein Psycho were held on two waves each and one came out
    at x0.24. Surprise waves stay in the mean.

## Round 81: surprises from the spawn points, the level's own surprise schedule, the Medium boss factor (added 2026-11-17)

- **The design** is in Docs/WAVE_GRAMMAR.md, "Surprises" (roles of spawn points, "Writing a level's surprises", the
  default plan). In code: `WaveScriptSpec.Surprises` (`SurpriseSpec`, `SurpriseEntrySpec`, `SpawnPointKind`), the helpers
  `Surprise(...)`, `FromNest(...)`, `DownPath(...)` in `WaveScripts`, `WaveGrammar.Plan` (a written surprise first, else
  the default with `WaveGrammarRules.MinSurpriseGroup`), `WaveComposer.FillAuthored` / `SurpriseHeads`,
  `LevelDefinition.RoleOfPath` / `RoleOfNest` (`SpawnRole`, `_surprisePaths`, `_mainNests`), `SurpriseSchedule.Check`.
- **After writing or changing a level's surprises:** recompile, read `compilationFailed`, the solve stage
  (`ApplyRound68.cs`, as in "Round 79"; it logs every schedule error as `[Validator]`:
  `unity command console --result-only | grep Validator`), then `WaveGrammarReport.cs` (`*` marks a written surprise,
  `!` a default rift surprise, `^` a flank) and `bash Tools/PlayTest/r34b_editmode_worker.sh 6 "WaveGrammarTests;RearNestTests"`.
- **What comes out of the nests in the stored waves** (per cell and wave: creature, count as stored and as played, nest):
  the listing used in STATUS round 81 is a twenty-line Python read of the wave assets (source 4 = nest); the same per
  wave with the groups' timing is `python Tools/PlayTest/wave_shape.py --waves C1_05:1`.
- **The threshold on paper:** `grammar.MinSurpriseGroup=1` in `Tools/PlayTest/wave_ledger_arg.txt` gives round 80's
  default plan without the filler (`WaveLedgerLab.cs`); `WaveGrammarReport.cs` shows the plan of every cell.
- **A boss factor on paper:** `rule.LevelBossAlone=0.2,0.33,0.4,0.45` and `label=<name>` in `Tools/PlayTest/r64_lab_arg.txt`,
  then `BossLab.cs` ("Round 64"); it prints every level boss with its health as spawned and what it asks alone. Put the
  arg file back to `label=final`. Round 81's runs: `r64_lab_r81_alone025.txt`, `_alone0.3.txt`, `_alone0.33.txt`.
- **The gates after a measurement:** `Tools/PlayTest/r81_chain.sh` (finale probe, grace measurement, replay, acceptance,
  plan files back to HEAD's; it waits for `Tools/PlayTest/r81_final.log`). Then, with the open editor free:
  `unity command eval_file --file Tools/PlayTest/refresh_assets.cs`, `GraceReport.cs`, the allowances in
  `Assets/_Project/Scripts/Editor/GraceWindows.cs`, and the whole suite: `bash Tools/PlayTest/r34b_editmode_worker.sh 6 Game.Tests`,
  `python Tools/PlayTest/editmode_failures.py`. The chain's last EditMode run of round 80b came before the grace report
  and so read the old rows; round 81 splits them.
- **Pitfalls met this round:**
  - A rule that changes which waves carry a surprise moves a cell's solve by far more than the surprise itself (Shard
    Ridge Hard: the flank one wave later made the ground waves 19 % softer at the same builder scale). Run
    `ScaleRefit.cs` after such a change and read every cell against the last measured waves before measuring.
  - A background batch started from the agent tool cannot be waited for with a bare `sleep`; wait with an
    `until grep -q ... ; do sleep 5; done` loop on the file the batch writes.
  - While a chain runs, do not edit anything under `Assets`, `Packages`, `ProjectSettings` or `Tools`: every stage syncs
    the clones again and would pick a half-made change up (and a data or bot change would split the stamps).

## Round 82: which roads a wave uses, the band on every ground wave, two written surprise schedules (added 2026-11-18)

- **The design** is in Docs/WAVE_GRAMMAR.md ("Which roads a wave uses", "No ground wave far tougher than the waves before
  it"). In code: `MainRoads`, `RoadSpec`, `WaveScriptSpec.RoadPlan` (helper `OnRoads` in `WaveScripts`),
  `WaveGrammar.RoadsOn`, `WaveGrammarRules.MultiRoadShare` and `SecondRoadEvery`, `WaveComposer.LanesOf`,
  `RoadSchedule.Check`; the band in `WaveSolver.Solve` and `SolveVariant` (`Trend`, the lift of a capped group).
- **The road plan of every cell:** `WaveGrammarReport.cs` ("Round 79") now prints `-` (first road), `_` (second road) or
  `=` (both) behind every wave of a level that forks.
- **The old behaviour on paper:** `grammar.MultiRoadShare=1,1,1,1` in `Tools/PlayTest/wave_ledger_arg.txt` (every wave
  down every road; a flank wave's front still keeps to the first road).
- **What the ledger reads of single waves:** `Tools/PlayTest/Scripts/WaveLoadProbe.cs` with lines `C1_03:2:7` (level key,
  difficulty index, wave) in `Tools/PlayTest/wave_load_arg.txt` writes `Tools/PlayTest/wave_load.txt`: the level's lanes
  (length, trunk share) and gates, and per wave the roads, the load (stream health, reach, span, gate seconds, window),
  the ledger's line (its health includes what earlier waves carry in) and every group with its path. This is how the
  paper waves of Shard Ridge were traced to carried Cragbacks (the wave's own health was a quarter of the ledger's).
- **Paper waves and outliers of a wave set:** a ground wave under x0.4 or above 2.5 times its cell's median is worth a
  look; the test `WaveGrammarTests.NoStoredGroundWave_IsFarTougherThanItsLevel` gates the upper side. The listing used in
  STATUS round 82 is a twenty-line Python read of `_healthMultiplier` and `_airRaid` per difficulty block of the wave assets.
- **The scale refit reads `Tools/PlayTest/r81_waves/`** now (round 81's measured waves). Mind: with the road rule the
  sum of multipliers is no measure of a forked cell any more (a wave that drops to the floor takes the sum down while
  the cell plays as hard): Shard Ridge Hard read x0.72 of round 81's sum at the same scale and the sensible rung won
  one seed fewer. Round 82 took the refit only for Easy and Medium cells whose sum had grown; the others kept their
  scale and were judged by the measurement.
- **The finale stage:** `bash Tools/PlayTest/finale_workers.sh` (the probe on the waves as they are), then
  `unity command run_script --file Tools/PlayTest/Scripts/ApplyFinale.cs --entry ApplyFinale.Run --timeout_ms 120000 --timeout 180`
  (writes every continent boss's health from the probe), then the probe once more and `ContinentFinaleTests`.
- **An EditMode run on w6 beside a pressure batch on w1 to w5** works (the probe runs on fixed steps), and so does the
  ledger lab in the open editor; the solve stage may run once the batch has synced, since the clones are copies.
- **Pitfalls met this round:**
  - A Bash heredoc with an odd number of apostrophes fails even with a quoted delimiter (again): write patch scripts
    with the editor tool and run them.
  - `unity command recompile_status` answers `up_to_date` when the editor had already compiled on its own; a wait loop
    must take that as done too.
  - A boss arena plays a campaign level's wave asset (Arena_Bog: Glass Meadows'), so a test over every
    `LevelDefinition` meets a level's written surprise under a name that has no script.
  - A lower band on a wave solved on its own (never softer than 0.7 of the waves before) was tried and taken out: where
    the pressure is carried in by earlier waves the wave cannot be softened into its band, and Shard Ridge Hard left the
    human curve's gate (1.36x where 1.49x is asked).

## Round 82b: reading a measurement, the picks with a fresh search (added 2026-11-18)

- **`result="Skipped"` on `BalanceAcceptanceTests` is the test working:** it writes `acceptance.txt` and then ignores
  itself with the list of failing cells whenever one fails. Read the report (`grep -c ": PASS"`), not the result line.
  The same holds for `PlanSearchTests.Search_WritesPlansForEveryCell` on a stale calibration hash (the files are written).
- **The calibration's input hash goes stale after the finale stage** (it counts creature health): every report header
  then reads `inputs ... (STALE, data now ...)` and the plan search skips itself. Remedy: `apply-keep` ("Round 66");
  it rewrites `Data/Balance/BalanceCalibration.asset`, so do it before a round's first measurement, not after.
- **Lost runs, boss margins and break-even from a records file:** `python Tools/PlayTest/pressure_killers.py lost <records> [--multiple 1]`
  (per lost run: the wave it fell in, base damage per wave, the wave that took the most, gate damage of the last waves,
  the guns standing by type and level), `... boss <records> <records before> [--difficulty 1]` (per level and rung at x1
  and x1.25: seeds won, the last wave's gate and base damage, base left) and `... breakeven <records>` (the highest
  multiple at which two of three seeds win; `fragile` marks a row that is not monotone).
- **A cell where no pick wins 3 of 3** (after step 2 of "Refreshing the skilled rung's picks", Round 78): search it on
  today's waves and try the result through an overlay ("Round 79", `PlanSearchTests` with `cells=<node>:<difficulty>`).
  If the search's best beats the old best, replace the cell's files in `Tools/PlayTest/Plans/` (delete the old
  `Level_<key>_<d>*` first) and write that cell's line in `skilled_picks.txt` by hand from the search's records, with a
  comment naming them (`skilled_picks.py` refuses a records file with two stamps). All of it is one bot batch; measure
  the skilled rung alone afterwards ("Round 79"). Round 82b: Crysium Vein Medium (`skilled_search_r82_records.txt`).
- **Before softening a Medium cell for the skilled rung, look at the sensible rung:** when the lower rung wins 15 of 15
  there, the upper rung's strategy is stale, not the level too hard.
- **The old gates on one cell with new plans:** `cp plan_records_<round>_replay.txt plan_records.txt`, then
  `workers.sh run replay 5 "cells=5:1" "seeds=1,2,3"`, `r34b_editmode_worker.sh 6 BalanceAcceptanceTests`, copy the
  clone's `acceptance.txt` aside and put both plan files back with `git show HEAD:<path> > <path>` (they are in the bot stamp).
- **The EditMode suite beside a pressure batch** takes 17 minutes instead of 12.
- **Pitfall:** a foreground `sleep N` of more than a few seconds is refused by the agent tool; wait with a bounded loop
  (`n=0; until grep -q "<done line>" <log> || [ $n -ge 22 ]; do sleep 5; n=$((n+1)); done`) and repeat the call.

## UI fix: level picker hints (added 2026-11-18)

- **Stills of the planet map's help line:** `[GAMEVIEW=1] bash Tools/PlayTest/ui_map_hints.sh <tag>` plays Boot, opens the map
  and writes `ArtSource/Previews/QA/ui_levelpicker_hints_<tag>_<state>_<1080|720>.png` for the region and planet layer with
  the keyboard and the region layer with an Xbox and a PlayStation pad (`r73_still.cs`, `r70_pad.cs`). It copies
  `progress.json`, its `.bak` and `settings.json` aside and puts them back, printing the MD5 at both ends. `GAMEVIEW=1`
  adds the editor's Game view capture, which shows the text as sharp as the game does.
- **Help-line templates** (`PromptLine`): `\n` starts a row, two or more spaces start a hint group, one space next to a
  `{0}` hole is a fixed gap; write no colon behind a hole. `PromptLineLayoutTests` holds the map hints to two rows.
- **Pitfall:** `sed` and Python heredocs in this shell turn a written `\n` into a real line break in a table file; put
  the two characters in with the Edit tool.

## Round 83: the monotone player, every wave to its own target, the closed loop (added 2026-11-18)

- **The design** is in Docs/WAVE_GRAMMAR.md ("The solver", "How to balance a new level", "Rules of rounds 80 and 80b that
  round 83 took out"). In code: `Ledger.PlanOf` and `WaveLoad.Of(..., book)` (the purchase is decided at book health),
  `WaveLoad.StreamOverkill` (carried health is not overkilled), `WaveSolver.Solve` and `Shares` (one pass per difficulty),
  `CampaignModel.SolveDifficulty` and `ModelTarget`, `BalanceRules.DifficultyScale`, `CellScalarLeast` / `CellScalarMost`,
  `WaveComposer.DropLoneWalkers`. Gone: `WaveSolver.SolveVariant`, `CampaignModel.CalibrateToHuman` / `SizeBosses` /
  `CapScale`, `BalanceRules.RaidsCappedAtHeldGuns`, `SurprisesLiftNoWave`, `SurpriseReliefMost`, `RearRiftGuardCells`,
  `BooksRearGuard`, `CalibrateToHuman`, `LevelBossAloneMost`, `MaxBossGrowth`, `CampaignModel.ReferenceInputFor`.
  `ScaleRefit.cs` (the refit to the previous round's waves) has no use any more, and `BalanceCurve.cs` prints a trim
  of one (there is no solver shape apart from the stored waves): the ledger lab replaces both.
- **The solve stage** is the same command (`ApplyRound68.cs`, "Round 79") and takes two to three seconds.
- **The ledger lab** (`WaveLedgerLab.cs`, "Round 79") prints per wave: the multiplier now and stored, `aim` (the wave's own
  target at scale one), the pressure with the hero and of the towers alone, the health multiplier a level boss carries
  (now and stored), the purse and the purchase; at the foot per cell the scale in use, the sum against the stored set and
  the lowest and highest ground wave over the median. All 24 cells take two seconds. `rule.<Field>=` still overrides a
  number for one run (`rule.LevelBossAlone=0.35,0.55,0.65,0.7`, `rule.DifficultyScale=1,1,1,1`).
- **The closed loop:** `python Tools/PlayTest/closed_loop.py <records> [--levels 0,1] [--centre] [--write]` (its header
  has the targets). Wide ladder for a first pass:
  `PR_RECORDS=Tools/PlayTest/<name>_records.txt PR_MULTIPLES="0.4,0.5,0.64,0.8,1,1.25,1.6,2" bash Tools/PlayTest/pressure.sh "0,1,2,3,4,5" "0,1,2,3"`
  (1152 runs, about 95 minutes on five clones); then the standard ladder (720 runs, an hour). `--write` rewrites the
  scale lines of `Tools/PlayTest/wave_tuning.txt`; then the solve stage.
- **A rule that the calibration's hash counts** (`InputHash`: `LevelBossAlone`, the floors, the utilisations, ...) makes
  every report STALE: re-stamp before the measurement ("Round 66"): move `plan_records.txt` out,
  `r34b_editmode_worker.sh 6 CalibrationFitTests`, move it back, copy the clone's `calibration.txt` and
  `calibration_waves.txt`, `balance_pipeline.sh apply-keep`, `eval_file r66_stale.cs` must say `stale False`.
- **Waiting from the agent tool:** a foreground command is moved to the background after about three minutes and a
  notification comes when it ends, so start a batch (or an `until grep -q DONE <log>; do sleep 10; done; <report>` line)
  with the background flag and wait for the notification instead of polling.
- **Pitfalls met this round:**
  - A Python patch in a Bash heredoc with an apostrophe in a C# comment fails to parse (again): write the patch with
    the editor tool and run it.
  - `unity command` answers `AMBIGUOUS_EDITOR` now and then while the clones run (their Pipeline servers are up too);
    repeat the call.
  - A worker run that ends in 40 seconds with "no results.xml" and `Scripts have compiler errors` without a `error CS`
    line in `Logs/unity_editmode.log` was a lost ILPP pipe; run it again.
  - A capped swarm carries its unbought budget as health (`timing.HealthMultiplier`): every floor, band and median in
    the solver must be read on the creatures as they walk in (multiplier times that lift), or a swarm passes them.
  - The median the solver caps a wave with must be the median the gate reads (every ground wave, a boss wave's crowd
    included).

## Round 83b: the closed-loop acceptance, picks on break-even, the paper band on the solver's own waves (added 2026-11-18)

- **The acceptance gate** is `ClosedLoopAcceptanceTests` (EditMode; `BalanceAcceptanceTests` is gone). It reads the
  records files named in `Tools/PlayTest/closed_loop_records.txt` (one per line, `#` comments), computes every rung's
  break-even as `closed_loop.py` does and writes `Tools/PlayTest/closed_loop_acceptance.txt`. Accepted: Easy both
  rungs win every seed at the data and the weaker one holds the whole ladder; Medium the sensible rung within one
  ladder step of x1.18; Hard the sensible rung within one step of x0.95; on both the better of the two rungs not
  more than a step under x1.06; Psycho the better rung within a step of x1.06; on every level Hard breaks the
  sensible rung earlier than Medium. A rung that holds or loses the whole ladder where a target is binding is a
  finding (nobody knows how far off it is). **A measurement whose data stamp is not the project's makes the test
  inconclusive** (`result="Inconclusive"`, the reading is in the message): after any change under
  `Assets/_Project/Data`, measure again and point the file at the new records. `balance_pipeline.sh acceptance` runs
  it. The old plan report still exists as a tool (menu `Tools/Generate/Balance/7 Acceptance report`,
  `acceptance.txt`); nothing gates on it.
- **A round's measurement, start to end** (what round 83b ran; each line is one batch, bot and data never together):
  1. data: `PR_RECORDS=Tools/PlayTest/<name>_records.txt bash Tools/PlayTest/pressure.sh "0,1,2,3,4,5" "0,1,2,3"`,
     `closed_loop.py <records> --centre`; move the scalars the sensible rung asks (Easy, Medium, Hard) by hand or
     with `--write`, solve (`ApplyRound68.cs`).
  2. bot: search on six clones side by side
     (`for n in 0 1 2 3 4 5; do bash Tools/PlayTest/r34b_editmode_worker.sh $((n+1)) PlanSearchTests "levels=$n" & done; wait`,
     two and a half minutes), replace the project's `Tools/PlayTest/Plans/Level_<key>_*` by each clone's, then play
     the candidates: Easy on two multiples
     (`PR_RECORDS=..._easy_records.txt PR_POLICIES="strategy1,strategy2,strategy3,strategy4,strategy5" PR_MULTIPLES="1,1.25" bash Tools/PlayTest/pressure.sh "0,1,2,3,4,5" "0"`)
     and the rest on the standard ladder (`... pressure.sh "0,1,2,3,4,5" "1,2,3"`): 1014 runs, 80 minutes.
     `python Tools/PlayTest/skilled_picks.py <easy records> <records> [--write]` ranks on break-even.
  3. **Keep the old pick as a candidate where the search's best is under the last skilled reading:**
     `git show HEAD:Tools/PlayTest/Plans/<old>.json > Tools/PlayTest/Plans/<cell>_strategy<N+1>.json`, set its `"Name"`
     to the new file name, play it (`PR_UNITS="0:2:strategy2:1,0:2:strategy2:2,0:2:strategy2:3" PR_RECORDS=... bash Tools/PlayTest/pressure.sh`)
     and write the cell's line in `skilled_picks.txt` by hand with a comment naming the records (the plan files are in
     the bot stamp, so these records carry another stamp than the batch of step 2).
  4. data: the Psycho cells and the skilled guards from the candidates' rows (the picked row is the skilled rung's),
     the means folded into `BalanceRules.DifficultyScale` (new factor = old x geometric mean of the difficulty's six
     scalars, scalars divided by the same), solve, finale probe, then the final measurement and the grace measure.
- **The paper band is asserted on the solver's own waves** (`BalanceLedgerTests.EveryWave_StaysInsideItsDifficultyBand`):
  the stored multipliers over `DifficultyScale` times the cell's scalar. Its report
  (`balance_ledger.txt`, last block) lists the waves over the hero cap that the solver cannot lower (a wave on the
  lower edge of its band, a boss on its health floor or flying); any other wave over the cap fails, and so do more
  than 3 % held waves.
- **The paper finale reads a continent boss at its own health** (`FinaleModel.Judge`, `ArenaMultiplier`): the boss
  alone no longer rides the stand-in wave's multiplier, which is the crowd's.
- **Pitfalls met this round:**
  - `pressure_versus.py` refuses round 82's final records against round 83's (the picks changed the bot between
    them): `--mixed-bots` for the sensible rung, which does not read the pick file.
  - A fresh plan search can be worse than the strategies it replaces (Landing Site Hard: best of the search
    `3 0 0 0 0`, the old pick `3 3 2 0 0`). Never delete the old pick before the candidates are played.
  - `PlanSearchTests` with `levels=<n>` writes fewer than five strategies for many cells (one to three on Medium and
    Hard): the ranking then has little to choose from.

## Round 84: composition rules per difficulty, the calm reference of a surprise wave (added 2026-11-18)

- **The design** is in Docs/WAVE_GRAMMAR.md ("What a wave is made of", "The solver" item 4). In code:
  `BalanceRules.PlatedBias`, `SwarmBias`, `ArmourGrowth`, `AirGrowth` (one value per difficulty), `WaveComposer.GrowthOn`
  (plated troops grow where the difficulty of the level sends no flyers), `WaveSolver.Solve(..., calm)` and
  `Solution.WalkIn`, `CampaignModel.SolveDifficulty` (a difficulty with surprises is solved without them first, as a
  reference only). Gone: `ScriptLever`, the fields `PlatedBias`, `SwarmBias`, `Tempo`, `RaidWave`, `BossCrowd`,
  `ArmourGrowth`, `AirGrowth` of `WaveScriptSpec`, `WaveComposer.AddRaid`, the lever moves of the old builder stage
  (`BalanceStages.BuildFromVerdicts` asks a question where it used to move a lever).
- **`Tools/PlayTest/wave_tuning.txt` holds `Key difficulty scalar` lines only** (`WaveTuning.OtherLines`, test
  `Round83Tests.TheTuningFile_HoldsACellsScalar_AndNothingElse`). A four-word line is not read any more.
- **A composition rule on paper, all 24 cells in one call:**
  `bash Tools/PlayTest/r84_lab.sh <label> ["rule.PlatedBias=1,1,1,1" "rule.ArmourGrowth=1,1,1,1" "grammar.Surprises=false" ...]`
  writes the lab's arg file and `Tools/PlayTest/wave_ledger_<label>.txt` (`editor_status` first; it retries on
  `AMBIGUOUS_EDITOR`). Then `python Tools/PlayTest/ledger_compare.py <base label> <label> ... [--cells C1_03:2] [--waves]`
  (per cell: bodies, ground health, median, softest and toughest wave, spawning seconds; `--waves` every wave that
  moved, with its boss) and `python Tools/PlayTest/ledger_held.py <label> ...` (the waves over the hero cap per cell).
  The lab reads the cell's levers from the code, so the base of a comparison must be run **before** the code changes
  (round 84: `wave_ledger_r84_base.txt`).
- **How much of a wave is armour:** `Tools/PlayTest/Scripts/PlatedShareLab.cs` (arguments `rule.<Field>=` and `label=` in
  `Tools/PlayTest/plated_share_arg.txt`) writes per cell and wave the plated troops' share of the threat and their
  number, without growth and with it.
- **What the surprises do to the other waves:** `unity command run_script --file Tools/PlayTest/Scripts/SurpriseLiftLab.cs --entry SurpriseLiftLab.Run --timeout_ms 900000 --timeout 960`
  writes `Tools/PlayTest/surprise_lift.txt`: every wave more than 5 % above the same level solved without surprises and
  the most per kind (a boss wave on the health of the whole wave). The bounds of
  `WaveGrammarTests.ASurprise_MakesNoOtherWaveTougher` (plain and raid x1.15, swarm x1.25, boss wave x1.3) come from it.
- **A cell that overshoots in the closed loop is bisected:** when two passes read a cell on opposite sides of its
  target, take the geometric middle of the two effective scales (scalar times `DifficultyScale`), not the loop's
  factor once more (Glass Meadows Medium went 1.004 -> 1.121 -> 1.061).
- **After a fold into `DifficultyScale`** write the scalars from the `--centre` line of `closed_loop.py` (the cells
  over the mean), recompile, then the solve stage; `r84_lab.sh` must print `moved more than 3 %: none` for all cells
  (the stored waves are the solve).
- **Pitfalls met this round:**
  - The solve stage right after a recompile can answer `An error occurred while sending the request` (the domain
    reload): nothing was written. Run it again and check the lab's foot.
  - A Bash heredoc that holds an apostrophe fails here even when quoted (again), and one that writes Python with
    `"\n"` in it turns the two characters into a line break: write patch scripts with the editor tool and use
    `chr(10)`.
  - A foreground wait loop over two minutes is moved to the background and reports "completed" on its own timeout;
    wait for the batch's own notification.

## Round 85: the stage, the save rule, throttled workers, ladders above x1.25 (added 2026-11-18)

- **The save is never restored by any script any more.** `workers.sh` (and `workers_keepsave.sh`, now a three-line
  wrapper that runs `workers.sh` under its old name for the chains of rounds 80b to 84), `pressure.sh` and
  `finale_workers.sh` compare the MD5 of `Saves/progress.json` before and after a run and only **warn** when it moved:
  the clones have their own product name and cannot reach that folder, so a change is Remco playing in the open editor.
  `balance_pipeline.sh` warns too; it restores only on request, `TD_RESTORE_SAVE=1` together with `--serial` (a PlayMode
  stage in the open editor writes the save itself), and only when nobody plays. Never copy, restore or write the save by
  hand. **Old one-off screenshot and smoke scripts still restore it** (`imp*_*.sh`, `n1_*.sh`, `origin_shots.sh`,
  `liq_shots.sh`, `vfx_shots.sh`, `round_ui_shots.sh`, `r16_*` to `r63_*` shot and cut scripts, `r37_steam.sh`,
  `r61_air_probe.sh`): they play in the open editor and copy the save back afterwards. Do not run them while Remco may
  play; if one is needed, take the restore out first.
- **The stage: a private working copy for the hours Remco plays** (`Tools/PlayTest/stage.sh`). Nothing under the open
  project's `Assets` may change while its Play Mode runs (Unity recompiles or re-imports under the running game), so a
  round's code and data work is done in `C:\Users\remco\TowerDefenseWorkers\stage` (Assets, Packages, ProjectSettings,
  Tools and a Library; product name "Tower defense stage", so it never sees the save):
  - `bash Tools/PlayTest/stage.sh make` (from the project) makes or refreshes it; `start` / `stop` its headless editor;
    `cmd <command>` is `unity command` on the stage's editor (never on the open one); `compile`; `run <File.cs> <Entry>`
    runs a script of `Tools/PlayTest/Scripts` there; `diff` lists what the stage changed; `back` copies it into the
    project (it refuses while the open editor's Play Mode runs; check `unity command editor_status` first anyway).
  - Every other script is run from inside the stage as it is from the project (`cd <stage> && bash Tools/PlayTest/pressure.sh ...`):
    the worker clones sync from the copy they are started from. The clones' product-name line is now matched as
    `Tower defense.*`, so a clone of the stage gets its worker name too.
  - Docs live in the project only (the stage carries no `Docs`).
- **Throttle (Remco uses the PC while a batch runs):** at most two worker clones at a time (`PR_WORKERS=2` for
  `pressure.sh`, `workers.sh run ... 2`, `finale_workers.sh` with two difficulties per call), never the stage editor and
  workers at once, and every worker and stage Unity process at **BelowNormal** priority: `pressure.sh`, `workers.sh`,
  `finale_workers.sh`, `r34b_editmode_worker.sh` and `stage.sh start` call `Tools/PlayTest/lower_priority.sh` right
  after the launch (it re-applies for two minutes, so asset import workers are caught; `lower_priority.ps1` does the
  work; the open editor's processes never match). Check with
  `powershell -NoProfile -Command "Get-Process Unity | Select-Object Id,PriorityClass"`.
- **The finale probe on two clones:** `finale_workers.sh 0 1`, copy `finale_probe.txt` aside, `finale_workers.sh 2 3`;
  each call overwrites the report, so merge the two (the header of the first, every `cell` line of both).
- **Ladders above x1.25 (the Easy and Medium targets of round 85):** `bash Tools/PlayTest/r85_measure.sh <name> [levels] [difficulties]`
  plays Easy on x1 / 1.25 / 1.4 / 1.57 / 1.76, Medium on x0.9 / 1 / 1.12 / 1.25 / 1.4 / 1.57 and Hard and Psycho on the
  standard ladder into `pressure_<name>_easy_records.txt`, `pressure_<name>_medium_records.txt` and
  `pressure_<name>_records.txt`, and names the three in `closed_loop_records.txt` when all 24 cells were played. Give all
  three to `closed_loop.py` (and `--write`), as one measurement.
- **A targeted measurement after a closed-loop pass:** `bash Tools/PlayTest/r85_target.sh <name> "0:3" "1:0,2,3"`
  (each argument "levels:difficulties" for `r85_measure.sh`; records `pressure_<name>_<i>_*`, about 3 minutes a cell on
  five clones). Read with `closed_loop.py <the records> --mixed-data` (each part has its own data stamp only when the
  data moved in between; cells not played read "not measured"). Run `closed_loop.py` from the project root: from
  another folder it finds no `wave_tuning.txt` and prints every scalar as 1. The full chain (`r85_chain.sh`) overwrites
  `closed_loop_records.txt` only when all 24 cells are played.
- **A scalar is checked on paper first:** set the cell's line in `wave_tuning.txt`, `cell=C1_02:3` in
  `wave_ledger_arg.txt` and `stage.sh run WaveLedgerLab.cs WaveLedgerLab.Run`: the lab solves that cell with the
  scalar in the file without writing assets (seconds). Round 85 found a cell whose waves jump between scalars that way.
- **Boss against the toughest ordinary creature:** `python -I Tools/PlayTest/boss_vs_regular.py [--root <project>]`
  (every boss as spawned, body and armour, against the toughest regular creature of its level and difficulty, on the
  bars, under light fire and under shells); `python -I Tools/PlayTest/ledger_bosses.py <label> ...` puts the boss waves
  of ledger lab runs side by side (crowd, boss, what the wave asks with the hero against its cap).
- **The laser against the Gun Turret per difficulty:** `stage.sh run LaserDuel.cs LaserDuel.Difficulties` (or
  `unity command run_script --file Tools/PlayTest/Scripts/LaserDuel.cs --entry LaserDuel.Difficulties ...` in the project)
  writes `Tools/PlayTest/laser_share.txt`: per difficulty and species the useful share of the data DPS, the kill rate in a
  packed stream and the holding power, before and after the round 85 beam rule.
- **Which towers the bot rungs buy:** `python -I Tools/PlayTest/tower_mix.py <records> [--multiple 1]` (each tower's
  share of the tower levels bought per difficulty, and the share of runs that build it).
- **Boss armour on the data without the generator:** `stage.sh run ApplyRound85.cs ApplyRound85.Run` (or the menu
  `Tools/Generate/Balance/12 Boss armour`), then the solve stage (`ApplyRound68.cs`). Armour is in the calibration's
  hash: re-stamp ("Round 66" pitfall above).

## Round 86: the one boss rule, the boss on paper, the beam and the frame, the picks batch (added 2026-11-19)

- **The design** is in Docs/WAVE_GRAMMAR.md ("The solver", item 6) and Docs/COMBAT_RULES.md (section 2). In code:
  `BossYardstick` (the toughest ordinary creatures met, under shells and under light fire), `WaveSolver.Solve` (the boss
  by the rule, its crowd on the lower edge of its band), `BalanceRules.BossOverRegular`, `LightFireOnArmour`,
  `LightFireInTheAir`, `BossBookShare`, `HumanBossUtilisation`, `CampaignModel.CountsAs` (what a wave counts as for the
  waves after it), `Health.BarFraction` / `Refill(max, armour)` / `LowerTo`, `Game.Towers.BeamBudget`. Gone:
  `LevelBossAlone`, `LevelBossGrowthCap`, `LevelBossHealthFloor`.
- **Every stored boss against the rule:** the test `Round86Tests.EveryLevelBoss_StandsOnItsMultiple` writes
  `Tools/PlayTest/boss_rule.txt` (per boss: times the toughest creature before it under both rows, as the solver
  counts a surprise wave and as the creatures really walked in). `python -I Tools/PlayTest/boss_vs_regular.py --root .`
  still gives the table against the toughest creature of the whole level.
- **Why the paper and the game disagree on a boss wave:** `stage.sh run BossPaperLab.cs BossPaperLab.Run` with
  `Tools/PlayTest/boss_paper_arg.txt` (`cell=C1_01:0`, `wave=12`, `multiple=1.76`, one `stand=Cannon:0` line per gun that
  stood in a recorded run, tower id and level index, `label=`) writes `Tools/PlayTest/boss_paper_<label>.txt`: the
  ledger's terms for that wave and defence (crowd against stream capacity, boss against single-target capacity, per gun
  the seconds the boss is in reach and what the ledger lets it deal in them, and the same at the gun's full data rate).
- **The beam at other frame lengths:** `stage.sh run LaserDuel.cs LaserDuel.Frames` writes
  `Tools/PlayTest/laser_frames.txt` (round 85's and round 86's beam at sixty frames a second, 0.05 s and the probe's
  0.2 s). `stage.sh run LaserRole.cs LaserRole.Run` writes `Tools/PlayTest/laser_role.txt` (what each difficulty sends
  on the ground, body against armour, and what the Gun Turret, the Laser and the Cannon land on that mix per coin).
- **Re-stamping the calibration without moving a file in the project:** `TD_DROP=Tools/PlayTest/plan_records.txt bash Tools/PlayTest/r34b_editmode_worker.sh 6 CalibrationFitTests`
  (the clone fits without the plan records; the project's copy, which is in the bot stamp, stays where it is), then copy
  the clone's `calibration.txt` and `calibration_waves.txt`, `ApplyCalibration.Keep`, `r66_stale.cs` ("Round 66"). Every
  rule in `InputHash` asks for it, `DifficultyScale` and `BossBookShare` included; the fit ends "Skipped" (under its
  bar) and that is expected, only the stamp is taken.
- **The skilled picks, a bot batch of its own:** `bash Tools/PlayTest/r86_picks.sh <name>` (in the background, about
  two hours on six clones; data must stand still): keeps every present pick aside (`Plans_old_<name>`), searches every
  level anew on six clones, adds the old pick to each cell's candidates as its next strategy number and plays every
  candidate on four multiples per difficulty. Then
  `python -I Tools/PlayTest/skilled_picks.py Tools/PlayTest/skilled_picks_<name>_easy_records.txt Tools/PlayTest/skilled_picks_<name>_medium_records.txt Tools/PlayTest/skilled_picks_<name>_records.txt --write`.
  A new pick file is a new bot: the next measurement is the first of that bot.
- **A fold into `DifficultyScale`, start to end** (round 86 did it after the second measurement): the new factor is the
  old one times the geometric mean of the difficulty's six scalars, the scalars are divided by the mean and clamped to
  0.7 to 1.4 (`Round83Tests.ACellsScalar_LiesNearOne`), recompile, re-stamp (above), solve, `r84_lab.sh` must print
  no moved wave.
- **Pitfalls met this round:**
  - `pressure.sh` syncs the clones at the start of every batch, and `r85_measure.sh` is three batches: a change in the
    stage between two of them gives the three records files different data stamps (`closed_loop.py --mixed-data` reads
    them, the acceptance test does not). Freeze the stage for a final measurement.
  - A Perl substitution with `|` as its delimiter turns `\|\|` in the pattern into an alternation that matches the
    empty string at the top of the file. Use braces.
  - The ledger's purchase reads a gun's single-target capacity even on a wave without a boss (`Gain`, while the ground
    is open), so a change of that number moved plain waves (Quartz Flats Easy wave 6: x2.91 -> x1.22). The boss rate
    (`BossFire`) therefore applies only to a load that has a boss.
  - `LevelDefinition` assets of the arenas share the campaign's waves: a test that names `Arena_Ruins` is reading
    Fault Line.

## Round 87: the formula, one scalar per level, frozen gates, archetype rungs, the laser role (added 2026-11-20)

- **The design** is in Docs/WAVE_GRAMMAR.md ("The solver (round 87: a formula)", "How to balance a new level (round 87)")
  and Docs/COMBAT_RULES.md (section 1: family table, retarget time). In code: `WaveFormula` (walk-in multiplier and boss
  bar), `WaveSolver.Solve` (applies it), `BossYardstick` (the floor: three times the bar of the toughest walker met),
  `BalanceRules` (`RampStart`, `RampEnd`, `RushFactor`, `HeavyFactor`, `AirFactor`, `AfterBossFactor`, `BossCrowdFactor`,
  `PeakPhases`, `LongLevelWaves`, `LongLevelPeakPhase`, `PeakFactor`, `DifficultyScale`, `BossBase`, `BossDifficulty`,
  `BossLevelGrowth`, `BossEarlyShare`, `FlyingBossShare`, `LevelScalarLeast`, `LevelScalarMost`), `CombatRules.Families`,
  `Game.Towers.Retarget`, `BalanceSheet.SpecialistPremium` / `SpecialistPriceCap`, `FinaleProbeReport.DerivedHealth`.
- **`Tools/PlayTest/wave_tuning.txt` holds `KEY scalar` lines only, one per level** (`C1_01 0.875`).
- **Solve and reports:** `stage.sh run ApplyRound68.cs ApplyRound68.Run` (the solve stage, as before), then
  `stage.sh run Round87Lab.cs Round87Lab.Run` writes `Tools/PlayTest/r87_waves.txt` (every wave: walk-in multiplier, kind,
  peak, boss) and `r87_bosses.txt` (every level boss: bar, body, armour, formula bar, the floor, whether it binds).
  `boss_share.py` reads `r87_waves.txt`: run the lab after every solve.
- **A measurement:** `PR_SEEDS=1 PR_WORKERS=6 bash Tools/PlayTest/r87_measure.sh <name>` from the stage, in the background
  (one seed, four rungs, 24 cells: 45 minutes on six clones; three seeds: two and a quarter hours). Then
  `python -I Tools/PlayTest/closed_loop.py <easy records> <medium records> <records> --centre` (table, gates, proposed
  level scalars and `DifficultyScale`), `python -I Tools/PlayTest/boss_share.py <the same>` (where the lost runs fell),
  `python -I Tools/PlayTest/archetype_table.py <the same> --rungs laser,cannon,sensible,skilled`,
  `python -I Tools/PlayTest/tower_mix.py <records> --rungs`.
- **The gates are frozen** in `Tools/PlayTest/closed_loop_thresholds.txt` (2026-11-20; `closed_loop.py`, `boss_share.py`
  and `ClosedLoopAcceptanceTests` read that file and nothing else). A change is a dated design decision on a line of its
  own, never a reaction to a reading.
- **Rule constants and the boss floor:** a level scalar or `PeakFactor` raises the walkers and with them the floor
  (three times the toughest walker). When the floor passes the formula on a last boss, that boss moves with the scalar
  and `WaveFormulaTests.EveryStoredLevelBoss_GrowsWithTheDifficulty` / `TheLastBoss_GrowsWithTheLevel` can fail: check the
  "floor binds" column of `r87_bosses.txt` after every solve (Glass Meadows Medium is the tightest).
- **The finale stage, with retry:** `bash Tools/PlayTest/r87_finale_cycle.sh <name> [apply]` (from the stage): the probe
  on four clones, a difficulty whose clone wrote no report is played again, the result in
  `finale_probe_<name>.txt`; with `apply` the finale stage follows. Round 87's stage rule
  (`FinaleProbeReport.DerivedHealth`): Medium's holding multiple times the hold share, and never more than the finale
  player of any other difficulty still beats; a boss beaten on every difficulty whose derived health lies within 20 %
  of the health it was probed at stays where it is (`FinaleProbeReport.SettledBand`; without it the stage walks a boss
  back up to a health it has already lost at). Repeat probe and apply until 24 of 24 win at the data. **A change of
  `DifficultyScale` moves the arenas' crowds: probe the finales again after the last tuning pass** (round 87: Gravemaw
  lost Hard after Hard's factor went from 1.23 to 1.36).
- **The laser:** `stage.sh run ApplyRound87Laser.cs ApplyRound87Laser.Run` (the rules asset: family table and Beam on
  armour; the creature families; the Laser's levels and sweep; the turrets' retarget time; safe to run again; tower
  numbers are in the calibration's hash, re-stamp after it). `stage.sh run LaserRole.cs LaserRole.Run` writes
  `Tools/PlayTest/laser_role_r87.txt`. `Game.Editor.StreamDuel` plays one tower against a stream through the firing
  rules (`Cadence`, `BeamBudget`, `Retarget`) for lab scripts and tests.
- **The archetype rungs** (bot version 4): `laser` and `cannon` are the sensible player with a favourite ground gun
  (`ProbeStrategy.MainGun`, `BalanceProbeTests.MainGunAct`); `PR_POLICIES="laser,cannon" bash Tools/PlayTest/r87_archetypes.sh <name>`
  measures them alone.
- **Pitfalls met this round:**
  - `cd <stage> && ( ... ) & bash Tools/PlayTest/x.sh` runs `x.sh` in the folder the shell STARTED in (the `&` ends the
    list the `cd` belongs to). A finale probe ran from the project that way and wrote its report there. Start every
    background command with `cd <stage> || exit 1;` on a line of its own.
  - Never run the whole EditMode suite on the stage editor: `PlanSearchTests` and `CalibrationFitTests` rewrite
    `Tools/PlayTest/Plans` and `calibration*.txt`. Run it on a clone (`r34b_editmode_worker.sh 6 Game.Tests`).
  - The probe's health multiple scales the bosses too (`LevelDefinition.EnemyHealthScale`); a level scalar does not.
    Where every lost run falls in the boss wave, a scalar moves the reading less than the loop assumes.
  - The skilled rung plays a recorded plan: after a rule change (boss armour, laser) the picks are stale and the
    Easy and Psycho readings with them. Run `r86_picks.sh` before the last tuning passes, not after.
  - A clone that ends in a minute with "Scripts have compiler errors" and no `error CS` line lost its ILPP pipe: play
    that part again (`r87_finale_cycle.sh` does so for the finale probe).

## Round 88a: stills in the stage, spawn clearance, sound blows, the labs (added 2026-11-20)

- **Stills without touching the open editor or the save.** The stage's editor can run with a graphics device:
  `bash Tools/PlayTest/stage.sh stop`, then `TD_STAGE_GRAPHICS=1 bash Tools/PlayTest/stage.sh start` (batch mode, no
  `-nographics`). From inside the stage: `OUT=<dir> bash Tools/PlayTest/r88_shots.sh <prefix> <node> <difficulty> <action> ...`
  (the actions of `r65_shots.sh` except `view:`; it plays Boot in the stage, never copies or restores a save, and refuses
  to run anywhere but the stage). Stills are written by `r88_shot.cs`: `r65_shot.cs` with an sRGB target, so the picture
  is as bright as the Game view (the HDR target of `r65_shot.cs` writes linear values: its stills are too dark).
  A batch editor has no Game view, so there is no `view:` action; the HUD is drawn through the tilt-shift pass and
  looks softer than in the game ("Mind the stills' blur", round 78).
- **Every level boss on its own level:** `OUT=<dir> bash Tools/PlayTest/r88_boss_shots.sh <prefix> [steps]` (from the stage;
  spawns each boss of the six levels, lets it walk, puts the play camera on it at the play zoom, `r88_focus.cs`).
  `python -I` with PIL makes contact sheets of the crops.
- **Probes for stills (eval files, stage or project):** `r88_spawn_probe.cs` (drawn size of every spawn prop and the build
  cells under it), `r88_drone_beam.cs` (`r88_drone_arg.txt`: `hurt` damages every tower, `probe` prints the drone, its
  body, its tool socket and where the beam starts, `zoom`), `r88_buy_repair.cs` (launches the repair drone from its asset,
  whatever the save offers), `r88_ping_probe.cs` (watched spawn points, markers on the HUD and the radar),
  `r88_light_probe.cs` (quality level, ambient, lights, volumes).
- **Labs on the assets (no Play Mode):** `stage.sh run Round88Lab.cs Round88Lab.Clearance` (per level the cells the spawn
  points keep free, how many were build cells, the validator, recorded plan spots on them: `r88_clearance.txt`),
  `Round88Lab.WaveSpans` (seconds a wave's ground creatures keep coming, per level and difficulty: `r88_wave_spans.txt`;
  the Chrono Field's lifetime follows the longest), `Round88Lab.DroneSockets` (each drone model's sockets against its body).
- **Data of the round without the full generator:** `stage.sh run ApplyRound88.cs ApplyRound88.Run` fills
  `SfxLibrary._radarPings` and measures the blow of every filled sound set (`SoundSet._impact`; the library, every creature
  voice, the hero's abilities). Run it again after new recordings; the generator's own `FillSet` writes the same.
- **Pitfalls met this round:**
  - A bash heredoc in the Bash tool turns a doubled backslash into one (a newline escape becomes a real newline) and chokes
    on apostrophes in C# comments: write C# files and Python edit specs with the Write tool.
  - `stage.sh compile` prints the `error CS` lines of the whole log, old ones included: the first line
    (`recompile_status ... completed`, with or without "n errors") is the verdict.
  - `stage.sh make` mirrors the project over the stage (`/MIR`): files that exist only in the stage are deleted. The
    leftovers of round 87 (`*_r87_final2*`) were copied to `TowerDefenseWorkers/r87_stage_leftover` first.
  - Unscaled time barely moves while frames are stepped in one eval call: a marker that lasts five real seconds is still
    up after 200 stepped frames.

## Round 88b: blast caps, tower threat, the escort rule, the economy labs, the re-stamp, stills at their own size (added 2026-11-21)

- **The design** is in Docs/COMBAT_RULES.md ("Structures: who can hurt a tower, and from how far", "The blast cap per
  level", the armoured reference fight of two Cragbacks in section 4) and Docs/WAVE_GRAMMAR.md ("The solver": the ramp
  0.6 to 1.68, the swarm between two raids, `BossRewardFactor` 1.5, point 2b the escort rule; "How to balance a new
  level": the targets of Hard and Psycho).
- **Data of the round without the full generator** (each safe to run again; each moves the calibration's input hash,
  so re-stamp after it):

      bash Tools/PlayTest/stage.sh run ApplyRound88bTowers.cs ApplyRound88bTowers.Run      # the Cannon's blast caps 2 / 3 / 4
      bash Tools/PlayTest/stage.sh run ApplyRound88bThreat.cs ApplyRound88bThreat.Run      # reach table, rules on 40 creatures, Cragback slam, three boss volleys
      bash Tools/PlayTest/stage.sh run ApplyRound88bEscort.cs ApplyRound88bEscort.Run      # escort worth per difficulty, six new brood powers
      bash Tools/PlayTest/stage.sh run ApplyRound88bEconomy.cs ApplyRound88bEconomy.Run    # level bosses' kill money back on the rule

  `ApplyRound88bEconomy` is for a change of `BalanceRules.BossRewardFactor` (3 -> 1.5 this round): it puts every level
  boss's kill money back on the rule; continent bosses keep their staged numbers. `ApplyRound88bEscort` is the step
  after a change of `BalanceSheet.*EscortWorth` (0.25 / 0.5 / 0.65 / 0.75).
- **Re-stamp the calibration:** `bash Tools/PlayTest/r88b_restamp.sh <label>` (from the stage). It runs the fit on clone
  w6 without the plan records (the round 86 recipe), copies the two calibration files to the stage, takes the stamp
  only (`ApplyCalibration.Keep`), solves (`ApplyRound68.Run`), rewrites the wave and boss reports (`Round87Lab.Run`)
  and ends with the stale check. Log: `Tools/PlayTest/r88b_<label>.log`, last line `RESTAMPED`.
- **Labs on the assets (no Play Mode):**

      bash Tools/PlayTest/stage.sh run CannonCapLab.cs CannonCapLab.Run                 # -> Tools/PlayTest/r88b_cannon_cap.txt
      bash Tools/PlayTest/stage.sh run CannonCapLab.cs CannonCapLab.Roles               # -> r88b_cannon_roles.txt (the reference fights at level 1)
      bash Tools/PlayTest/stage.sh run WispReachLab.cs WispReachLab.Run                 # -> r88b_wisp_reach.txt (what of the ground an air-only gun reaches)
      bash Tools/PlayTest/stage.sh run Round88bThreatLab.cs Round88bThreatLab.After     # -> r88b_threat.txt (Before writes r88b_threat_before.txt)
      bash Tools/PlayTest/stage.sh run Round88bEscortLab.cs Round88bEscortLab.Run       # -> r88b_escort.txt
      bash Tools/PlayTest/stage.sh run Round88bEconomyLab.cs Round88bEconomyLab.Run     # -> r88b_economy.txt
      bash Tools/PlayTest/stage.sh run Round88bEconomyLab.cs Round88bEconomyLab.Players # -> r88b_players.txt
      bash Tools/PlayTest/stage.sh run Round88bEconomyLab.cs Round88bEconomyLab.Maps    # -> r88b_maps.txt
      bash Tools/PlayTest/stage.sh run Round88bEconomyLab.cs Round88bEconomyLab.Raids   # -> r88b_raids.txt

  - `r88b_economy.txt`: per level and difficulty every stored wave as spawned and as paid (creatures, walk-in health of
    crowd and boss, bounties after the income multiplier, money before the wave as a plain sum, with the model's mines,
    and by `KillPurse`), the pressure index (health over money held), the ledger's window, the rate index (health per
    second per 100 coins held) and the ledger's pressure, their trend over the level, the money totals (last quarter,
    last wave, bosses, bosses before the last wave), and a table of "what if" scenarios (ramp, breather, peak phases,
    boss bounties, income, Psycho's start money).
  - `r88b_players.txt`: the permanent upgrades the model's player owns per level and difficulty, half of them, none of
    them, and Remco's save of 2026-11-20 beside them. `r88b_maps.txt`: roads, gates, rifts, build spots, first flyers
    and the purse by then, raids, first plated wave, bosses. `r88b_raids.txt`: raids, their share, raids in two waves
    running, every peak with its distance from its phase, and what candidate peak rules move.
  - `StreamDuel.Dps(tower, level, stream, step, splashTargets)` plays a tower with another blast cap than its data has
    (a lab's what-if).
- **Tests of the round** (on a clone): `bash Tools/PlayTest/r34b_editmode_worker.sh <N> Round88bTowersTests`,
  `Round88bThreatTests`, `EscortRuleTests`, `Round88bWaveTests`, `MarkerLayoutTests`.
- **Measuring Hard and Psycho: one more rung below.** The frozen targets of Hard and Psycho moved one step harder on a
  dated line of `Tools/PlayTest/closed_loop_thresholds.txt` (Hard x0.95 -> x0.85, Psycho x1.06 -> x0.95; Easy and
  Medium unchanged), so both are measured on a ladder that starts at x0.71:

      PR_MULTIPLES="0.71,0.8,0.9,1,1.12,1.25" PR_SEEDS=1 PR_WORKERS=6 bash Tools/PlayTest/r87_measure.sh <name>

  The Easy and Medium ladders are set inside the script.
- **Money left over, from the records:** `python -I Tools/PlayTest/r88b_spare.py <records> [...] [--rungs skilled,sensible] [--waves]`:
  per difficulty, level and rung, on the runs won at the data: coins unspent, earned (book), received (through the
  income multiplier), repairs, and the spare `received x (1 - 1 / break-even)`; then how far the waves get, the gate
  health lost and the purse at wave start by third of the level. `Tools/PlayTest/r88b_spare_before.txt` is round 87's
  final measurement. Note: field 17 of a record ("earned") is the start money plus BOOK bounties, before the income
  multiplier.
- **Tower wear in the records:** since round 88b the probe's records carry in their note field
  `wear guns <health lost> hurt <n> wrecked <n> gates <health lost>`, for the tower-damage table.
- **Stills with world labels in the right place:** `r88_shot.cs` (and `r65_shot.cs`) draws the HUD at the still's
  size while markers and badges were placed for the batch editor's own 640x480 screen: world labels land on panels
  they never touch in the game. `Tools/PlayTest/r88b_shot.cs` gives the camera its target first, steps two frames so
  every view lays itself out for that size, then takes the picture; it prints the safe area and every showing world
  label, with "ON PANEL" / "OUTSIDE" for one that touches a keep-out zone or leaves the safe area.
  `Tools/PlayTest/r88b_shot_arg.txt`: `<width> <height> <png path relative to the stage>`. In `r88_shots.sh`:
  `"arg:r88b_shot_arg.txt=1920 1080 Logs/r88b/x.png" "eval:r88b_shot.cs"`.
  Only views that read the canvas's camera are right in it (`SurprisePingView`, `RearNestMarkerView`); the boss
  marker, the base alert and the rebuild badges still pass `null` and are NOT placed right in such a still.
- **Pings where a still needs them:** `Tools/PlayTest/r88b_ping.cs`, `r88b_ping_arg.txt` one line of words:
  `vp:<x>:<y>` a surprise ping at the ground point under that viewport point (outside 0..1 = off screen that way),
  `rear:<x>:<y>` Voss's rear-rift warning there, `margin:<percent>` the title-safe margin for the session (in memory),
  `dump` screen, canvas, safe area, zones and showing labels. The layout rule itself is `Game.UI.MarkerLayout`
  (obstacles from `HudKeepOut`).
- **Pitfalls met this round:**
  - The stage's batch editor screen is 640x480 (4:3). Anything that is laid out in screen space is laid out for that,
    whatever size a still is drawn at.
  - Round 88a's `SfxPlayer` asked the audio system for its buffer size, which logs an error in the silent worker
    clones and failed every probe run ("2 probe errors, 0 runs"). It now reads
    `AudioSettings.GetConfiguration().sampleRate` first. A probe batch that ends in a minute with "0 runs": read the
    `[Probe] error` lines in the clone's `Logs/unity.log`.
  - A baseline of the OLD data is measured from a second copy (`TowerDefenseWorkers\stageB`, mirrored from the project
    plus the probe files), because `pressure.sh` syncs the clones from the folder it is started in.
  - The role matrix in Docs/COMBAT_RULES.md section 4 is a copy of `Tools/PlayTest/r67_role_matrix.txt`; after a change
    of a blast cap or a reference fight, rewrite the file (`balance_pipeline.sh roles`) and copy the table again.
- **More scripts of the curator (round 88b):** `ApplyRound88bArmour.cs` (`ApplyRound88bArmour.Run`: every boss whose
  armour stood on the old share gets `BalanceRules.BossArmourShare` of its body; then `ApplyRound88bEconomy`, re-stamp,
  the finale stage), `ApplyRound88bSockets.cs` (the beam socket of the three level bosses with a volley),
  `Round88bBones.cs` (lists a boss rig's bones). **Order of a final:** rule passes, picks, finale stage, THEN the
  three-seed measurement (round 88b measured first; the finale stage then moved two boss assets and the acceptance
  test reads the records as another data state).

## Round 88c: the first raid, the boss entrance, stills of a boss fight, the wave timeline by hand (added 2026-11-21)

- **The design** is in Docs/WAVE_GRAMMAR.md and Docs/COMBAT_RULES.md, "Round 88c". In code: `FirstRaidRule`,
  `AirRaidAlertView`, `BalanceRules.WaveCeiling` / `FirstRaidPurseGuns` / `FirstRaidWarningWaves` / `Escort*Cells` /
  `EscortDashFactor`, `WaveGrammarRules.BossEntranceGap` / `BossTailStretch`, `EscortDash`,
  `BossAbilityDefinition.FlightSeconds`.
- **Data of the round without the full generator:** `bash Tools/PlayTest/stage.sh run ApplyRound88c.cs ApplyRound88c.Run`
  (the purse on the four difficulties, five brood call sounds, the combat rules asset). Then re-stamp
  (`r88b_restamp.sh <label>`): the rules are in the calibration's hash.
- **Labs:** `stage.sh run Round88cLab.cs Round88cLab.FirstRaid` (`r88c_first_raid.txt`: per cell the first raid, the
  warning wave, a boss beside it, the purse and its share of the level's coins) and `Round88cLab.Ladder`
  (`r88c_ladder.txt`: Medium against Hard, crowd health, last boss, health per coin).
- **Stills (stage, graphics on):** `OUT=Logs/r88c bash Tools/PlayTest/r88c_stills.sh menu alert bosswave volley win`.
  `win` is the win rule in play (the last boss killed with creatures alive: still running; all killed: won).
- **`r88c_field_probe.cs`** (`r88c_field_arg.txt`, words): `state`, `diag`, `bolts`, `purse`, `killboss`, `killall`,
  `nogates`, `healbase`, `wavescale:<n>` (creatures spawned by hand as tough as wave n), `gotowave:<n>` (the wave
  clock jumps: wave n starts on the next frame as stored; `skip_wave.cs` does nothing in the stage, the fast-forward
  is developer-only).
- **Pitfalls met this round:**
  - `PR_SEEDS` is a list of seeds ("1,2,3", the default), not a count: `PR_SEEDS=3` plays seed 3 only.
  - A stepped frame in the stage is 0.02 s unless `r17_fixed_step.cs` ran; time a still by a probe, not by frames.
  - A fresh stage save owns only the first guns: a rule that reads the towers on offer (the purse) shows other numbers
    there than with Remco's save.
  - A foreground shell command is cut loose after three minutes; start anything longer in the background and poll.


## Round 88d: start money for the air defence, the rebuild chip, the gate bar, the crew raises a base turret (added 2026-11-21)

- **The design** is in Docs/WAVE_GRAMMAR.md ("Round 88d: the purse became start money") and Docs/STATUS.md ("Round 88d").
  In code: `StartMoneyRule` (the one place a level's opening coins are summed), `BalanceRules.RaidStartGuns` /
  `DifficultyProfile.RaidStartGuns` (the old `FirstRaidPurseGuns`), `LevelFacts.RaidStartMoney` (the model's copy),
  `ICrewRaisable` / `Tower.AwaitsCrew` / `RepairDispatcher.NeedsWork` (a wrecked base turret is a repair job),
  `GateRebuildBadges` (chip, full line under the cursor), `WorldBar.FaceCamera` (the gate's bar).
- **Data of the round without the full generator:** `bash Tools/PlayTest/stage.sh run ApplyRound88c.cs ApplyRound88c.Run`
  (it writes `_raidStartGuns` on the four difficulties; the old serialized name is read through `FormerlySerializedAs`),
  then `bash Tools/PlayTest/r88b_restamp.sh <label>`: the level facts now carry the air-raid start money, so the
  calibration's input hash moved. The stored waves do not change with it (the wave formula does not read money).
- **Stills and play checks (stage, graphics on):** `OUT=Logs/r88d bash Tools/PlayTest/r88d_stills.sh <part> <tag>`,
  parts `field` (Shard Ridge Hard: guns round the first gate, gates hurt, the gate wrecked, the cursor on the wreck),
  `alert` (start money and the warning two waves before the first raid), `turret` (Landing Site Medium: base turrets
  mounted by hand, one wrecked, a repair drone launched, played until the turret stands again).
- **`r88d_scene.cs`** (`r88d_scene_arg.txt`, words): `towers:<TowerId>:<count>`, `wreckgate`, `hurtgates:<share>`,
  `hurttowers:<share>`, `focusgate:<zoom>`, `cursorgate`, `cursoraway`, `turrets` (mounts the level's base turrets on
  a save that has not bought them), `wreckturret`, `focusturret:<zoom>`, `state`.
- **Tests of the round** (on a clone): `bash Tools/PlayTest/r34b_editmode_worker.sh 6 Round88dTests` (also
  `Round88cTests`, `RepairDispatcherTests`, `TowerTests`, `CounterAffordabilityTests`).
- **Pitfalls met this round:**
  - A rebuild badge is now placed through the canvas's camera, so `r88b_shot.cs` shows it where the game does.
  - Files deleted in the project while a round runs come back with `stage.sh back` (the stage still has them): before
    copying back, delete from the stage what `stage.sh diff` lists that the round did not make.
  - The same script line again: a heredoc in the Bash tool halves a doubled backslash. `r88d_stills.sh` takes its
    Windows path from `cygpath -w`.
