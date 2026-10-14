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
