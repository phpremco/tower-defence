#!/usr/bin/env bash
# Plays the opening cutscene from Boot and captures sought moments into Assets/Temp/<prefix>_<shot>_<progress>.png.
# Usage: cut_shots.sh <prefix> "<shot> <progress>" ... (shot index from 0). Stops Play Mode at the end.
set -u
cd "$(dirname "$0")/../.." || exit 1
prefix=${1:-cut}
shift
status=$(unity command editor_status --result-only 2>&1)
if ! echo "$status" | grep -q '"playMode": *"stopped"'; then echo "NOT STOPPED - abort"; exit 1; fi
unity command clear_console --result-only >/dev/null 2>&1
unity command open_scene --path Assets/_Project/Scenes/Boot.unity --result-only >/dev/null 2>&1
unity command editor_play --result-only 2>&1 | grep -o "Entered[^\"]*"
sleep 8
step() { echo "$1" > Tools/PlayTest/frames_arg.txt; unity command eval_file --file Tools/PlayTest/step_frames.cs --result-only >/dev/null 2>&1; }
run() { unity command eval_file --file "Tools/PlayTest/$1" --result-only 2>&1 | grep -o '"result": "[^"]*' | cut -c12-220; }
step 20
run cutscene_start.cs
sleep 4
step 10
for moment in "$@"; do
  echo "$moment" > Tools/PlayTest/cut_arg.txt
  unity command eval_file --file Tools/PlayTest/cutscene_seek.cs --result-only 2>&1 | grep -v "^ *\"(output|diagnostics|success)" | head -5
  step 3
  run cutscene_probe.cs
  name=$(echo "$moment" | tr ' .' '_p')
  unity command capture_game_view --save_path "Temp/${prefix}_${name}.png" --result-only >/dev/null 2>&1
done
echo "=== CONSOLE"
unity command console --result-only 2>&1 | grep -o '"message": "[^"]*' | grep -v "allocation" | sort | uniq -c | sort -rn | head -8
unity command editor_stop --result-only >/dev/null 2>&1
