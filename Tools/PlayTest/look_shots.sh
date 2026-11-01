#!/usr/bin/env bash
# Plays level 1 from Boot, launches the drones and a wave, and captures the overview plus close-ups of the hero and
# the base into Assets/Temp/<prefix>_*.png, then prints the console summary. Usage: look_shots.sh <prefix> [level]
set -u
cd "$(dirname "$0")/../.." || exit 1
prefix=${1:-look}
level=${2:-0}
status=$(unity command editor_status --result-only 2>&1)
if ! echo "$status" | grep -q '"playMode": *"stopped"'; then echo "NOT STOPPED - abort"; exit 1; fi
unity command clear_console --result-only >/dev/null 2>&1
unity command open_scene --path Assets/_Project/Scenes/Boot.unity --result-only >/dev/null 2>&1
unity command editor_play --result-only 2>&1 | grep -o "Entered[^\"]*"
sleep 8
step() { echo "$1" > Tools/PlayTest/frames_arg.txt; unity command eval_file --file Tools/PlayTest/step_frames.cs --result-only >/dev/null 2>&1; }
run() { unity command eval_file --file "Tools/PlayTest/$1" --result-only 2>&1 | grep -o '"result": "[^"]*' | cut -c12-90; }
shot() { unity command capture_game_view --save_path "Temp/${prefix}_$1.png" --result-only >/dev/null 2>&1; }
step 60
run intro_skip.cs
echo "$level" > Tools/PlayTest/level_arg.txt
run start_level.cs
sleep 6
step 100; step 100
shot play
run buy_drones.cs >/dev/null
run skip_wave.cs >/dev/null
step 100
shot wave
for z in hero base; do
  echo $z > Tools/PlayTest/zoom_arg.txt; echo 6 > Tools/PlayTest/zoom_size.txt
  run zoom.cs >/dev/null; step 3; shot $z
done
echo "=== CONSOLE"
unity command console --result-only 2>&1 | grep -o '"message": "[^"]*' | grep -v "allocation" | sort | uniq -c | sort -rn | head -6
ls Assets/Temp
unity command editor_stop --result-only >/dev/null 2>&1
