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
  pressure ground/air; B marks a boss wave). Failures name level, difficulty and wave.

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
