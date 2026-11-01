#!/usr/bin/env bash
# Plays Boot, skips the intro, opens the upgrade screen (goto_base.cs) and captures it, then selects a few tiles
# (base_select.cs with zoom_arg.txt) and captures each. Usage: base_shot.sh <prefix> [tile ...]
set -u
cd "$(dirname "$0")/../.." || exit 1
prefix=${1:-base}; shift || true
status=$(unity command editor_status --result-only 2>&1)
if ! echo "$status" | grep -q '"playMode": *"stopped"'; then echo "NOT STOPPED - abort"; exit 1; fi
unity command clear_console --result-only >/dev/null 2>&1
unity command open_scene --path Assets/_Project/Scenes/Boot.unity --result-only >/dev/null 2>&1
unity command editor_play --result-only 2>&1 | grep -o "Entered[^\"]*"
sleep 8
step() { echo "$1" > Tools/PlayTest/frames_arg.txt; unity command eval_file --file Tools/PlayTest/step_frames.cs --result-only >/dev/null 2>&1; }
run() { unity command eval_file --file "Tools/PlayTest/$1" --result-only 2>&1 | grep -o '"result": "[^"]*' | cut -c12-300; }
full() { unity command eval_file --file "Tools/PlayTest/$1" --result-only 2>&1 | head -c 600; }
shot() { unity command capture_game_view --save_path "Temp/${prefix}_$1.png" --result-only >/dev/null 2>&1; }
step 60; run intro_skip.cs
echo "0" > Tools/PlayTest/points_arg.txt
echo "$(full goto_base.cs)"; sleep 4; step 40; step 40; shot screen
for tile in "$@"; do echo "$tile" > Tools/PlayTest/zoom_arg.txt; echo "$(run base_select.cs)"; step 20; shot "$tile"; done
echo "=== CONSOLE"
unity command console --result-only 2>&1 | grep -o '"message": "[^"]*' | grep -v "allocation" | sort | uniq -c | sort -rn | head -6
unity command editor_stop --result-only >/dev/null 2>&1
