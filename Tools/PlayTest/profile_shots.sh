#!/usr/bin/env bash
# Plays a level under the High (PC) or Low (Switch) quality profile and captures the overview and a hero close-up
# into Assets/Temp/<prefix>_<profile>_*.png. Usage: profile_shots.sh <prefix> <level> <high|low>
set -u
cd "$(dirname "$0")/../.." || exit 1
prefix=${1:-prof}; level=${2:-0}; profile=${3:-high}
status=$(unity command editor_status --result-only 2>&1)
if ! echo "$status" | grep -q '"playMode": *"stopped"'; then echo "NOT STOPPED - abort"; exit 1; fi
unity command clear_console --result-only >/dev/null 2>&1
unity command open_scene --path Assets/_Project/Scenes/Boot.unity --result-only >/dev/null 2>&1
unity command editor_play --result-only 2>&1 | grep -o "Entered[^\"]*"
sleep 8
step() { echo "$1" > Tools/PlayTest/frames_arg.txt; unity command eval_file --file Tools/PlayTest/step_frames.cs --result-only >/dev/null 2>&1; }
run() { unity command eval_file --file "Tools/PlayTest/$1" --result-only 2>&1 | grep -o '"result": "[^"]*' | cut -c12-300; }
shot() { unity command capture_game_view --save_path "Temp/${prefix}_${profile}_$1.png" --result-only >/dev/null 2>&1; }
step 60; run intro_skip.cs
if [ "$profile" = "low" ]; then echo "$(run low_profile.cs)"; fi
echo "$level" > Tools/PlayTest/level_arg.txt; run start_level.cs; sleep 6; step 100; step 100
shot play
run buy_drones.cs >/dev/null; run skip_wave.cs >/dev/null; step 100
shot wave
echo hero > Tools/PlayTest/zoom_arg.txt; echo 6 > Tools/PlayTest/zoom_size.txt; run zoom.cs >/dev/null; step 3
shot hero
echo "=== CONSOLE"
unity command console --result-only 2>&1 | grep -o '"message": "[^"]*' | grep -v "allocation" | sort | uniq -c | sort -rn | head -6
unity command editor_stop --result-only >/dev/null 2>&1
