#!/usr/bin/env bash
# Plays a level without waves, spawns each named enemy at the gate, lets it walk a moment and captures a frozen
# close-up into Assets/Temp/<prefix>_<name>.png. Usage: enemy_shots.sh <prefix> <level> <zoom> <Name> [Name ...]
set -u
cd "$(dirname "$0")/../.." || exit 1
prefix=$1; level=$2; zoom=$3; shift 3
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
# The hero leaves the gate for the base, or it kills the creature before the camera gets there.
run hero_to_base.cs >/dev/null; step 20
echo "$zoom" > Tools/PlayTest/zoom_size.txt
for name in "$@"; do
  echo "$name" > Tools/PlayTest/enemy_arg.txt
  echo "$(run spawn_enemy.cs)"; step ${STEPS:-25}
  echo "$(run zoom_enemy.cs)"; run zoom_close.cs >/dev/null; step 2; shot "$name"; run resume.cs >/dev/null
  echo kill > Tools/PlayTest/enemy_arg.txt
done
echo "=== CONSOLE"
unity command console --result-only 2>&1 | grep -o '"message": "[^"]*' | grep -v "allocation" | sort | uniq -c | sort -rn | head -6
unity command editor_stop --result-only >/dev/null 2>&1
