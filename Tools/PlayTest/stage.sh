#!/usr/bin/env bash
# Round 85: the stage, a private working copy of the project for the times Remco plays in the open editor
# (Docs/COMMANDS.md, "Round 85"). Nothing under the open project's Assets may change while its Play Mode runs: Unity
# would recompile or re-import under the running game. So the round's work is done in a copy
# (TD_STAGE, default C:\Users\remco\TowerDefenseWorkers\stage: Assets, Packages, ProjectSettings, Tools and a Library)
# with its own headless editor, and the worker clones sync from that copy because every script takes the project
# from its own location. The copy has its own product name ("Tower defense stage"), so its editor never sees
# Remco's save folder, and it runs without graphics or sound.
#
#   bash Tools/PlayTest/stage.sh make                 # from the PROJECT: (re)make the copy; the Library comes from clone w6 the first time
#   bash Tools/PlayTest/stage.sh start                # the stage's headless editor (stays up; the Pipeline answers `unity command`)
#   bash Tools/PlayTest/stage.sh stop                 # quits it (only the editor whose command line names the stage)
#   bash Tools/PlayTest/stage.sh cmd <command...>     # `unity command` on the stage's editor, never on the open one
#   bash Tools/PlayTest/stage.sh compile              # refresh + recompile there, waits, prints the compile errors
#   bash Tools/PlayTest/stage.sh run <File.cs> <Entry> # run_script from Tools/PlayTest/Scripts there
#   bash Tools/PlayTest/stage.sh diff                 # what the stage changed against the project (Assets and Tools)
#   bash Tools/PlayTest/stage.sh back                 # copies those changes into the project; refuses while its Play Mode runs
#
# Everything else (pressure.sh, workers.sh, r34b_editmode_worker.sh, finale_workers.sh, closed_loop.py) is run from
# inside the stage as it is from the project: `cd <stage> && bash Tools/PlayTest/pressure.sh ...`.
set -uo pipefail
export MSYS_NO_PATHCONV=1
STAGE="${TD_STAGE:-C:\\Users\\remco\\TowerDefenseWorkers\\stage}"
PROJECT="${TD_PROJECT:-C:\\Users\\remco\\Tower defense}"
UNITY="${TD_UNITY:-C:\\Program Files\\Unity\\Hub\\Editor\\6000.6.5f1\\Editor\\Unity.exe}"
stage_u="$(cygpath -u "$STAGE")"
project_u="$(cygpath -u "$PROJECT")"

on_stage() { UNITY_PROJECT_PATH="$STAGE" unity command "$@"; }

# Retries the calls the Pipeline drops while other editors are up or a domain reload runs.
cmd() {
  local out n=0
  while :; do
    out="$(on_stage "$@" 2>&1)"
    if echo "$out" | grep -q "AMBIGUOUS_EDITOR\|error occurred while sending\|No Pipeline instance" && [ $n -lt 8 ]; then n=$((n + 1)); sleep 4; continue; fi
    echo "$out"; return 0
  done
}

mirror() { robocopy "$1" "$2" /MIR /MT:16 /R:1 /W:1 /NFL /NDL /NJH /NJS /NP /NS /NC > /dev/null; [ $? -le 7 ]; }

case "${1:-}" in
  make)
    mkdir -p "$stage_u/Logs"
    for d in Assets Packages ProjectSettings Tools; do mirror "$PROJECT"'\'"$d" "$STAGE"'\'"$d" || { echo "copy of $d idle|up_to_date|succeeded|failed|completed" >&2; exit 1; }; done
    [ -d "$stage_u/Library" ] || mirror 'C:\Users\remco\TowerDefenseWorkers\w6\Library' "$STAGE"'\Library'
    sed -i 's/^  productName: Tower defense.*$/  productName: Tower defense stage/' "$stage_u/ProjectSettings/ProjectSettings.asset"
    rm -f "$stage_u"/Assets/InitTestScene*.unity*
    echo "stage made at $STAGE" ;;
  start)
    grep -q '^  productName: Tower defense stage' "$stage_u/ProjectSettings/ProjectSettings.asset" || { echo "the stage has no product name of its own" >&2; exit 1; }
    "$UNITY" -batchmode -nographics -job-worker-count 2 -projectPath "$STAGE" -logFile "$STAGE"'\Logs\stage_editor.log' > /dev/null 2>&1 &
    (sleep 5; bash "$stage_u/Tools/PlayTest/lower_priority.sh" 120) > /dev/null 2>&1 &
    echo "stage editor starting (pid $!), BelowNormal priority; wait for: bash Tools/PlayTest/stage.sh cmd editor_status" ;;
  stop)
    powershell -NoProfile -Command "Get-CimInstance Win32_Process -Filter \"Name='Unity.exe'\" | Where-Object { \$_.CommandLine -like '*TowerDefenseWorkers\\stage*' } | ForEach-Object { Stop-Process -Id \$_.ProcessId -Force; 'stopped ' + \$_.ProcessId }" ;;
  cmd) shift; cmd "$@" ;;
  compile)
    cmd refresh > /dev/null
    cmd recompile > /dev/null
    n=0; until cmd recompile_status | grep -q '"status":"\(idle\|up_to_date\|succeeded\|failed\|completed\)"\|compilationFailed' || [ $n -ge 30 ]; do sleep 3; n=$((n + 1)); done
    sleep 2
    cmd recompile_status | tail -1 | cut -c1-600
    grep -a "error CS" "$stage_u/Logs/stage_editor.log" | sort -u | tail -15 ;;
  run) shift; cmd run_script --file "Tools/PlayTest/Scripts/$1" --entry "$2" --timeout_ms 900000 --timeout 960 ;;
  diff)
    for d in Assets Tools; do robocopy "$STAGE"'\'"$d" "$PROJECT"'\'"$d" /E /L /NJH /NJS /NP /NS /NC /NDL /XD Plans_tmp | sed 's/^[[:space:]]*//' | grep -v '^$'; done ;;
  back)
    UNITY_PROJECT_PATH="$PROJECT" unity command editor_status 2>&1 | grep -q '"playMode":"stopped"' || { echo "the open editor is not stopped (Remco plays, or it is not open): nothing copied" >&2; exit 1; }
    for d in Assets Tools; do robocopy "$STAGE"'\'"$d" "$PROJECT"'\'"$d" /E /MT:16 /R:1 /W:1 /NFL /NDL /NJH /NJS /NP /NS /NC > /dev/null; [ $? -le 7 ] || { echo "copy back of $d failed" >&2; exit 1; }; done
    echo "copied back; the open editor imports on its next refresh" ;;
  *) sed -n '2,22p' "$0"; exit 1 ;;
esac
