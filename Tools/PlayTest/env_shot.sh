#!/usr/bin/env bash
# Plays a level from Boot without waves and captures the opening view into Assets/Temp/<prefix>_play.png and a camera-only
# render, for comparing the environment (lights, fog, ambient) before and after a refactor. Usage: env_shot.sh <prefix> [level]
set -u
cd "$(dirname "$0")/../.." || exit 1
prefix=${1:-env}
level=${2:-0}
status=$(unity command editor_status --result-only 2>&1)
if ! echo "$status" | grep -q '"playMode": *"stopped"'; then echo "NOT STOPPED - abort"; exit 1; fi
unity command clear_console --result-only >/dev/null 2>&1
unity command open_scene --path Assets/_Project/Scenes/Boot.unity --result-only >/dev/null 2>&1
unity command editor_play --result-only 2>&1 | grep -o "Entered[^\"]*"
sleep 8
step() { echo "$1" > Tools/PlayTest/frames_arg.txt; unity command eval_file --file Tools/PlayTest/step_frames.cs --result-only >/dev/null 2>&1; }
run() { unity command eval_file --file "Tools/PlayTest/$1" --result-only 2>&1 | grep -o '"result": "[^"]*' | cut -c12-120; }
step 30
echo "$level" > Tools/PlayTest/level_arg.txt
run start_level.cs
sleep 5
step 5
run no_waves.cs
step 40
unity command capture_game_view --save_path "Temp/${prefix}_play.png" --result-only >/dev/null 2>&1
unity command capture_game_view --source camera --save_path "Temp/${prefix}_cam.png" --result-only >/dev/null 2>&1
run pixel_probe.cs
echo "=== CONSOLE"
unity command console --result-only 2>&1 | grep -o '"message": "[^"]*' | grep -v "allocation" | sort | uniq -c | sort -rn | head -6
unity command editor_stop --result-only >/dev/null 2>&1
