#!/usr/bin/env bash
# Plays a level without waves, runs a showcase probe (tower_showcase.cs by default) and captures it into
# Assets/Temp/<prefix>_showcase.png. Usage: showcase_shots.sh <prefix> [level] [probe] [ortho half height] [focus arg]
set -u
cd "$(dirname "$0")/../.." || exit 1
prefix=${1:-show}; level=${2:-0}; probe=${3:-tower_showcase.cs}; half=${4:-3}
status=$(unity command editor_status --result-only 2>&1)
if ! echo "$status" | grep -q '"playMode": *"stopped"'; then echo "NOT STOPPED - abort"; exit 1; fi
unity command clear_console --result-only >/dev/null 2>&1
unity command open_scene --path Assets/_Project/Scenes/Boot.unity --result-only >/dev/null 2>&1
unity command editor_play --result-only 2>&1 | grep -o "Entered[^\"]*"
sleep 8
step() { echo "$1" > Tools/PlayTest/frames_arg.txt; unity command eval_file --file Tools/PlayTest/step_frames.cs --result-only >/dev/null 2>&1; }
run() { unity command eval_file --file "Tools/PlayTest/$1" --result-only 2>&1 | grep -o '"result": "[^"]*' | cut -c12-400; }
shot() { unity command capture_game_view --save_path "Temp/${prefix}_$1.png" --result-only >/dev/null 2>&1; }
step 60; run intro_skip.cs
echo "$level" > Tools/PlayTest/level_arg.txt; run start_level.cs; sleep 6; step 100; step 100
run no_waves.cs >/dev/null
echo "$half" > Tools/PlayTest/zoom_size.txt; echo "${5:-}" > Tools/PlayTest/zoom_arg.txt
echo "$(run "$probe")"
step 8; shot showcase; step 4; shot showcase2
echo "=== CONSOLE"
unity command console --result-only 2>&1 | grep -o '"message": "[^"]*' | grep -v "allocation" | sort | uniq -c | sort -rn | head -6
unity command editor_stop --result-only >/dev/null 2>&1
