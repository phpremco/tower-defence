#!/usr/bin/env bash
# S3 check pictures: the planet with its continents, the Crystal Plains region view, and every campaign level in play
# (overview after a wave starts). Writes Assets/Temp/<prefix>_*.png; copy them out and delete the folder afterwards.
# Usage: s3_shots.sh <prefix> [level count]
set -u
cd "$(dirname "$0")/../.." || exit 1
prefix=${1:-s3}
count=${2:-6}
status=$(unity command editor_status --result-only 2>&1)
if ! echo "$status" | grep -q '"playMode": *"stopped"'; then echo "NOT STOPPED - abort"; exit 1; fi
unity command clear_console --result-only >/dev/null 2>&1
unity command open_scene --path Assets/_Project/Scenes/Boot.unity --result-only >/dev/null 2>&1
unity command editor_play --result-only 2>&1 | grep -o "Entered[^\"]*"
sleep 8
step() { echo "$1" > Tools/PlayTest/frames_arg.txt; unity command eval_file --file Tools/PlayTest/step_frames.cs --result-only >/dev/null 2>&1; }
run() { unity command eval_file --file "Tools/PlayTest/$1" --result-only 2>&1 | grep -o '"result": "[^"]*' | cut -c12-300; }
shot() { unity command capture_game_view --save_path "Temp/${prefix}_$1.png" --result-only >/dev/null 2>&1; }
step 30
run goto_map.cs; sleep 4; step 120
shot region
echo "$(run map_probe.cs)"
echo cancel > Tools/PlayTest/map_arg.txt; run map_step.cs >/dev/null; step 150; shot planet
for ((i = 0; i < count; i++)); do
  echo "$i" > Tools/PlayTest/level_arg.txt
  run start_level.cs
  sleep 6; step 100; step 60
  run skip_wave.cs >/dev/null; step 80
  shot "level$((i + 1))"
done
echo "=== CONSOLE"
unity command console --result-only 2>&1 | grep -o '"message": "[^"]*' | grep -v "allocation" | sort | uniq -c | sort -rn | head -8
unity command editor_stop --result-only >/dev/null 2>&1
ls Assets/Temp
