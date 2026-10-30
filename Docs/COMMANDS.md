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
  every gate), `hero_to_base.cs` and `zoom.cs` (close-up on "hero", "gate" or "base" from `zoom_arg.txt`).
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
- `Tools/PlayTest/zoom_spawn.cs` zooms onto the start of path `zoom_arg.txt` (a path index) at `zoom_size.txt` (0 restores the full view).
- `Tools/PlayTest/click_button.cs` clicks a UI button whose label contains `button_arg.txt` (empty text lists every label): walk the menus without a mouse. `open_menu.cs` opens pause / tower / gate / hero / win (`menu_arg.txt`), `select_card.cs` picks a tower card (`card_arg.txt`), `tower_at.cs` builds on a cell (`cell_arg.txt`, "x y").
- `Tools/PlayTest/base_menu.cs` opens the base menu; `base_die.cs` destroys the base (explosion, then the defeat panel).
- `Tools/PlayTest/spawn_enemy.cs` spawns an enemy by asset name from `enemy_arg.txt` ("Roller", "Drowner Water", bosses use the level entrance); `zoom_enemy.cs` zooms on it and freezes time for a screenshot, `resume.cs` thaws; `no_waves.cs` switches the wave timeline off; `inspect_enemy.cs` lists its renderers; `shadows_off.cs` drops its shadows; `goto_menu.cs` returns to the main menu; `sfx_probe.cs` builds the loop sounds in the editor.
- `Tools/PlayTest/menu_probe.cs` lists the main menu stage (asset, cameras, diorama); `menu_creatures.cs` lists every diorama creature with its state; `music_probe.cs` reports the menu music and whether a level score is running.
- `Tools/PlayTest/boss_alarm.cs` raises the boss alarm by hand; `boss_probe.cs` reports the boss marker, approach sounds and the boss (light, pulse, scale); `sfx_voices.cs` lists the sound voices and approach timers; `sliders.cs` lists the volume sliders; `map_pick.cs` presses the first open beacon on the planet map; `time_probe.cs` reports time scale, briefing and countdown; `intro_probe.cs` and `intro_skip.cs` watch and skip the intro; `screen_probe.cs` prints the game view size (captures are resized to 1280x720, so circles look squashed when the view is wider than 16:9).
- `Tools/PlayTest/dump_waves.cs` writes every generated wave per level and difficulty to `Tools/PlayTest/waves_dump.txt`; `wave_probe.cs` reports the wave number, countdown and living enemies by kind.
- `Tools/PlayTest/laser_probe.cs` reports the boss laser, beam renderers and hum count (write "kill" in `enemy_arg.txt` to kill every boss first); `drone_probe.cs` reports money and active drones. `laser_now.cs` makes every living boss fire its laser on the next frame.
- The balance bot takes an optional `probe_arg.txt` (`levels=0,1` and `difficulties=1,2` lines) and must run async in PlayMode (see STATUS).
- `Tools/PlayTest/sfx_library_probe.cs` reports the recorded sound library and how many clips each list holds.
- `Tools/PlayTest/hum_probe.cs` reports which clip the boss laser loop plays.
- `Tools/PlayTest/alarm_probe.cs` reports which clip the boss alarm plays; `ripple_probe.cs` plays an impact ripple at the hero and holds the frame for a screenshot.
- `Tools/PlayTest/hover_probe.cs` builds a tower beside the road and puts the placing cursor on the next cell (zoom with `towers`).
- `Tools/PlayTest/dump_map.cs` prints every level as a text map with its validation errors (run after the generator).
- The generator menu item can take longer than the CLI's 30 s main-thread timeout (five terrain bakes); the error
  "Main thread operation timed out" is harmless, the generator keeps running. Wait for it by running the EditMode
  tests next (they queue behind it), then `clear_console`.
- More scripts in `Tools/PlayTest/`: `hero_down.cs` (drains the hero), `hero_revive.cs` (cuts the revive timer),
  `upgrade_gate.cs` (one level on every gate), `upgrade_towers.cs`, `skip_wave.cs`, `towers_all.cs` (one probe of
  every tower asset near the spawn, Editor only), `shots_probe.cs` (lists shots in flight and centres the camera on
  one); `zoom.cs` also takes "towers", "roster", "spawn" and "enemy" and a size from `zoom_size.txt`.
