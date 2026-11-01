#!/usr/bin/env bash
# Spawns a creature at the hero's gate and, while the hero fights it, reports the aim of the drawn weapon
# (aim_probe.cs) and captures close-ups in slow motion into Assets/Temp/<prefix>_aim<N>.png.
# Usage: aim_shots.sh <prefix> <level> <zoom> <Name>
set -u
cd "$(dirname "$0")/../.." || exit 1
prefix=$1; level=$2; zoom=$3; name=$4
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
echo "$zoom" > Tools/PlayTest/zoom_size.txt; echo hero > Tools/PlayTest/zoom_arg.txt
run zoom.cs >/dev/null; run zoom_close.cs >/dev/null
echo "$name" > Tools/PlayTest/enemy_arg.txt
echo "$(run spawn_enemy.cs)"
unity command set_timescale --scale 0.1 --result-only >/dev/null 2>&1
for i in 1 2 3 4 5 6; do step 3; echo "$i: $(run aim_probe.cs)"; shot "aim$i"; done
unity command set_timescale --scale 1 --result-only >/dev/null 2>&1
echo "=== CONSOLE"
unity command console --result-only 2>&1 | grep -o '"message": "[^"]*' | grep -v "allocation" | sort | uniq -c | sort -rn | head -6
unity command editor_stop --result-only >/dev/null 2>&1
