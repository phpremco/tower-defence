#!/usr/bin/env bash
# Plays a level, measures the rendered colour of road, ground and hero (pixel_probe.cs), then switches mist, the
# ground detail layer and post-processing off one by one with a screenshot after each, so a dark or washed picture
# can be traced to its layer. Usage: look_layers.sh <prefix> [level]
set -u
cd "$(dirname "$0")/../.." || exit 1
prefix=${1:-layers}
level=${2:-0}
status=$(unity command editor_status --result-only 2>&1)
if ! echo "$status" | grep -q '"playMode": *"stopped"'; then echo "NOT STOPPED - abort"; exit 1; fi
unity command clear_console --result-only >/dev/null 2>&1
unity command open_scene --path Assets/_Project/Scenes/Boot.unity --result-only >/dev/null 2>&1
unity command editor_play --result-only 2>&1 | grep -o "Entered[^\"]*"
sleep 8
step() { echo "$1" > Tools/PlayTest/frames_arg.txt; unity command eval_file --file Tools/PlayTest/step_frames.cs --result-only >/dev/null 2>&1; }
run() { unity command eval_file --file "Tools/PlayTest/$1" --result-only 2>&1 | grep -o '"result": "[^"]*' | cut -c12-400; }
shot() { unity command capture_game_view --save_path "Temp/${prefix}_$1.png" --result-only >/dev/null 2>&1; }
step 60
run intro_skip.cs
echo "$level" > Tools/PlayTest/level_arg.txt
run start_level.cs
sleep 6
step 100; step 100
echo "FULL: $(run pixel_probe.cs)"
shot full
for layer in mist detail post; do
  echo $layer > Tools/PlayTest/look_arg.txt
  run look_toggle.cs >/dev/null; step 3
  echo "NO $layer: $(run pixel_probe.cs)"
  shot "no_$layer"
done
echo "=== CONSOLE"
unity command console --result-only 2>&1 | grep -o '"message": "[^"]*' | grep -v "allocation" | sort | uniq -c | sort -rn | head -6
unity command editor_stop --result-only >/dev/null 2>&1
