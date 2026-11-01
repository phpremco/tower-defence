#!/usr/bin/env bash
# Plays a level and zooms onto the first pool of a kind, then captures three frames a short while apart into
# Assets/Temp/<prefix>_<kind>_<n>.png so surface motion can be judged. Usage: liquid_shots.sh <prefix> <level> <kind> [zoom] [ortho half height for a close-up]
set -u
cd "$(dirname "$0")/../.." || exit 1
prefix=${1:-liq}
level=${2:-0}
kind=${3:-Water}
zoom=${4:-7}
status=$(unity command editor_status --result-only 2>&1)
if ! echo "$status" | grep -q '"playMode": *"stopped"'; then echo "NOT STOPPED - abort"; exit 1; fi
unity command clear_console --result-only >/dev/null 2>&1
unity command open_scene --path Assets/_Project/Scenes/Boot.unity --result-only >/dev/null 2>&1
unity command editor_play --result-only 2>&1 | grep -o "Entered[^\"]*"
sleep 8
step() { echo "$1" > Tools/PlayTest/frames_arg.txt; unity command eval_file --file Tools/PlayTest/step_frames.cs --result-only >/dev/null 2>&1; }
run() { unity command eval_file --file "Tools/PlayTest/$1" --result-only 2>&1 | grep -o '"result": "[^"]*' | cut -c12-3000; }
shot() { unity command capture_game_view --save_path "Temp/${prefix}_$1.png" --result-only >/dev/null 2>&1; }
step 60
run intro_skip.cs
echo "$level" > Tools/PlayTest/level_arg.txt
run start_level.cs
sleep 6
step 100; step 100
echo "$kind" > Tools/PlayTest/zoom_arg.txt; echo "$zoom" > Tools/PlayTest/zoom_size.txt
run zoom_liquid.cs
echo "${5:-0}" > Tools/PlayTest/zoom_size.txt; [ "${5:-0}" != "0" ] && run zoom_close.cs
echo "PROBE: $(run particle_probe.cs)"
for n in 1 2 3; do step 12; shot "${kind}_$n"; done
echo "=== CONSOLE"
unity command console --result-only 2>&1 | grep -o '"message": "[^"]*' | grep -v "allocation" | sort | uniq -c | sort -rn | head -6
unity command editor_stop --result-only >/dev/null 2>&1
