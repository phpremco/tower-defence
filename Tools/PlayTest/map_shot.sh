#!/usr/bin/env bash
# Plays Boot, skips the intro, jumps to the planet map and captures it into Assets/Temp/<prefix>_map_<n>.png,
# three frames a short while apart; then selects the first region (map_pick.cs) and captures the region view.
set -u
cd "$(dirname "$0")/../.." || exit 1
prefix=${1:-map}
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
echo "$(run goto_map.cs)"; sleep 4; step 90
for n in 1 2; do step 15; shot "map_$n"; done
echo cancel > Tools/PlayTest/map_arg.txt; run map_step.cs >/dev/null; step 120; shot planet; step 30; shot planet2
echo "$(run map_probe.cs)"
echo "=== CONSOLE"
unity command console --result-only 2>&1 | grep -o '"message": "[^"]*' | grep -v "allocation" | sort | uniq -c | sort -rn | head -6
unity command editor_stop --result-only >/dev/null 2>&1
