#!/usr/bin/env bash
# Plays a level without waves, spawns one creature at the gate where the hero stands and captures a run of frames
# of the hero and of the creature a few frames apart, so motion can be judged from a series. Writes
# Assets/Temp/<prefix>_hero<N>.png and <prefix>_enemy<N>.png. Usage: motion_shots.sh <prefix> <level> <zoom> <Name> [frames between shots]
set -u
cd "$(dirname "$0")/../.." || exit 1
prefix=$1; level=$2; zoom=$3; name=$4; gap=${5:-6}
status=$(unity command editor_status --result-only 2>&1)
if ! echo "$status" | grep -q '"playMode": *"stopped"'; then echo "NOT STOPPED - abort"; exit 1; fi
unity command clear_console --result-only >/dev/null 2>&1
unity command open_scene --path Assets/_Project/Scenes/Boot.unity --result-only >/dev/null 2>&1
unity command editor_play --result-only 2>&1 | grep -o "Entered[^\"]*"
sleep 8
step() { echo "$1" > Tools/PlayTest/frames_arg.txt; unity command eval_file --file Tools/PlayTest/step_frames.cs --result-only >/dev/null 2>&1; }
run() { unity command eval_file --file "Tools/PlayTest/$1" --result-only 2>&1 | grep -o '"result": "[^"]*' | cut -c12-300; }
shot() { unity command capture_game_view --save_path "Temp/${prefix}_$1.png" --result-only >/dev/null 2>&1; }
step 60; run intro_skip.cs
echo "$level" > Tools/PlayTest/level_arg.txt; run start_level.cs; sleep 6; step 100; step 100
run no_waves.cs >/dev/null
echo "$zoom" > Tools/PlayTest/zoom_size.txt
echo "$name" > Tools/PlayTest/enemy_arg.txt
echo "$(run spawn_enemy.cs)"; step 10
for i in 1 2 3 4; do echo hero > Tools/PlayTest/zoom_arg.txt; run zoom.cs >/dev/null; run zoom_close.cs >/dev/null; step 1; shot "hero$i"; step "$gap"; done
for i in 1 2 3 4; do echo enemy > Tools/PlayTest/zoom_arg.txt; run zoom.cs >/dev/null; run zoom_close.cs >/dev/null; step 1; shot "enemy$i"; step "$gap"; done
echo "=== CONSOLE"
unity command console --result-only 2>&1 | grep -o '"message": "[^"]*' | grep -v "allocation" | sort | uniq -c | sort -rn | head -6
unity command editor_stop --result-only >/dev/null 2>&1
