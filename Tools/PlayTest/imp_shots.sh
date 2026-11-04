#!/usr/bin/env bash
# Plays a level with the waves off, then for every lineup line "<ortho half height> <Enemy> <Enemy> ..." in the
# arguments builds the creatures side by side in the middle of the view (imp_lineup.cs) and captures
# Assets/Temp/<prefix>_<n>.png. Usage: imp_shots.sh <prefix> <level> "<half> <Name> ..." ["<half> <Name> ..."]
set -u
cd "$(dirname "$0")/../.." || exit 1
prefix=$1; level=$2; shift 2
status=$(unity command editor_status --result-only 2>&1)
if ! echo "$status" | grep -q '"playMode": *"stopped"'; then echo "NOT STOPPED - abort"; exit 1; fi
unity command clear_console --result-only >/dev/null 2>&1
unity command open_scene --path Assets/_Project/Scenes/Boot.unity --result-only >/dev/null 2>&1
unity command editor_play --result-only 2>&1 | grep -o "Entered[^\"]*"
sleep 8
step() { echo "$1" > Tools/PlayTest/frames_arg.txt; unity command eval_file --file Tools/PlayTest/step_frames.cs --result-only >/dev/null 2>&1; }
run() { unity command eval_file --file "Tools/PlayTest/$1" --result-only 2>&1 | grep -o '"result": "[^"]*' | cut -c12-300; }
step 60; run intro_skip.cs
echo "$level" > Tools/PlayTest/level_arg.txt; run start_level.cs; sleep 6; step 100; step 100
run no_waves.cs >/dev/null
run hero_to_base.cs >/dev/null; step 10
n=0
for line in "$@"; do
  n=$((n+1))
  echo "$line" > Tools/PlayTest/imp_arg.txt
  echo "$(run imp_lineup.cs)"; step 10; step 10
  unity command capture_game_view --save_path "Temp/${prefix}_${n}.png" --result-only >/dev/null 2>&1
done
echo "=== CONSOLE"
unity command console --result-only 2>&1 | grep -o '"message": "[^"]*' | grep -v "allocation" | sort | uniq -c | sort -rn | head -6
unity command editor_stop --result-only >/dev/null 2>&1
echo "=== END"
