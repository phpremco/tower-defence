#!/usr/bin/env bash
# Round 15: plays level_arg's level from Boot and writes the codex preview of each named enemy (r15_codex.cs), plus one
# still of the whole codex on the first. Usage: r15_codex.sh <output dir> <prefix> <enemy asset name> ...
set -u
cd "$(dirname "$0")/../.." || exit 1
out=$1; prefix=$2; shift 2
status=$(unity command editor_status --result-only 2>&1)
if ! echo "$status" | grep -q '"playMode": *"stopped"'; then echo "NOT STOPPED - abort"; exit 1; fi
unity command clear_console --result-only >/dev/null 2>&1
unity command open_scene --path Assets/_Project/Scenes/Boot.unity --result-only >/dev/null 2>&1
unity command editor_play --result-only 2>&1 | grep -o "Entered[^\"]*"
sleep 8
step() { echo "$1" > Tools/PlayTest/frames_arg.txt; unity command eval_file --file Tools/PlayTest/step_frames.cs --result-only >/dev/null 2>&1; }
run() { unity command eval_file --file "Tools/PlayTest/$1" --result-only 2>&1 | grep -o '"result": "[^"]*' | cut -c12-400; }
# ARENA=<boss> loads that boss's developer arena (its codex lists the bosses) instead of the level in level_arg.txt.
if [ -n "${ARENA:-}" ]; then echo "$ARENA" > Tools/PlayTest/r14_boss.txt; step 60; run r14_arena.cs; else step 60; run start_level.cs; fi
sleep 5; step 60; step 60
run no_waves.cs
first=1
for enemy in "$@"; do
  echo "$enemy" > Tools/PlayTest/r15_codex_name.txt
  echo "$out/${prefix}_$enemy.png" > Tools/PlayTest/look_arg.txt; run r15_codex.cs
  if [ $first = 1 ]; then echo "$out/${prefix}_${enemy}_screen.png" > Tools/PlayTest/look_arg.txt; run render_still.cs; first=0; fi
done
echo "=== CONSOLE"
unity command console --result-only 2>&1 | grep -o '"message": "[^"]*' | grep -v "allocation" | sort | uniq -c | sort -rn | head -8
unity command editor_stop --result-only >/dev/null 2>&1
