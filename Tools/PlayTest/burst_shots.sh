#!/usr/bin/env bash
# Plays a level, zooms on the hero and fires one impact effect after another (burst_probe.cs), capturing three
# frames of each into Assets/Temp/<prefix>_<name>_<n>.png. Usage: burst_shots.sh <prefix> <level> "<name> <radius>" ...
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
shot() { unity command capture_game_view --source camera --save_path "Temp/${prefix}_$1.png" --result-only >/dev/null 2>&1; unity command capture_game_view --save_path "Temp/${prefix}_$1_screen.png" --result-only >/dev/null 2>&1; }
step 60
run intro_skip.cs
echo "$level" > Tools/PlayTest/level_arg.txt
run start_level.cs
sleep 6
step 100; step 100
run no_waves.cs >/dev/null
echo hero > Tools/PlayTest/zoom_arg.txt; echo 4 > Tools/PlayTest/zoom_size.txt
run zoom.cs >/dev/null; step 3
for spec in "$@"; do
  echo "$spec" > Tools/PlayTest/vfx_arg.txt
  name=${spec%% *}
  echo "$(run burst_probe.cs)"
  step 2; shot "${name}_1"; step 5; shot "${name}_2"; step 12; shot "${name}_3"; run time_free.cs >/dev/null; step 40
done
run time_free.cs >/dev/null
echo "=== CONSOLE"
unity command console --result-only 2>&1 | grep -o '"message": "[^"]*' | grep -v "allocation" | sort | uniq -c | sort -rn | head -6
unity command editor_stop --result-only >/dev/null 2>&1
