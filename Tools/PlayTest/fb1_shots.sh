#!/usr/bin/env bash
# Playtest feedback 2026-11-04: renders the cutscene's space and planet-surface moments, the drone shots, the C1_01
# base, the main menu and the planet map (overview, region, chosen level) at 1920x1080 into $OUT/<prefix>_*.png.
# Usage: fb1_shots.sh <prefix> [cut|map|base|all]. Stops Play Mode at the end. LOW=1 renders the Low profile.
set -u
cd "$(dirname "$0")/../.." || exit 1
prefix=${1:-fb1}
what=${2:-all}
out=${OUT:-Logs}
status=$(unity command editor_status --result-only 2>&1)
if ! echo "$status" | grep -q '"playMode": *"stopped"'; then echo "NOT STOPPED - abort"; exit 1; fi
unity command clear_console --result-only >/dev/null 2>&1
unity command open_scene --path Assets/_Project/Scenes/Boot.unity --result-only >/dev/null 2>&1
unity command editor_play --result-only 2>&1 | grep -o "Entered[^\"]*"
sleep 8
step() { echo "$1" > Tools/PlayTest/frames_arg.txt; unity command eval_file --file Tools/PlayTest/step_frames.cs --result-only >/dev/null 2>&1; }
run() { unity command eval_file --file "Tools/PlayTest/$1" --result-only 2>&1 | grep -o '"result": "[^"]*' | cut -c12-220; }
still() { echo "$out/${prefix}_$1.png" > Tools/PlayTest/look_arg.txt; unity command eval_file --file Tools/PlayTest/render_still.cs --result-only 2>&1 | grep -o "wrote[^\"]*"; }
step 20
[ "${LOW:-0}" = "1" ] && run low_profile.cs
if [ "$what" = "cut" ] || [ "$what" = "all" ]; then
  run cutscene_start.cs; sleep 4; step 10
  # shot index, progress, name
  for moment in "0 0.5 earth" "1 0.4 survey" "2 0.2 orbit_a" "2 0.75 orbit_b" "3 0.12 landing_a" "3 0.45 landing_b" "4 0.3 build_a" "4 0.33 build_b" "4 0.36 build_c" "4 0.7 build_d" "6 0.3 haul_a" "6 0.34 haul_b" "6 0.38 haul_c" "6 0.7 haul_d" "7 0.2 deliver"; do
    set -- $moment
    echo "$1 $2" > Tools/PlayTest/cut_arg.txt
    unity command eval_file --file Tools/PlayTest/cutscene_seek.cs --result-only 2>&1 | grep -o '"result": "[^"]*' | cut -c12-160
    step 3
    still "cut_$3"
  done
fi
if [ "$what" = "map" ] || [ "$what" = "all" ]; then
  run goto_menu.cs; sleep 4; step 60; still menu_main
  run goto_map.cs; sleep 4; step 90; still map_region
  echo cancel > Tools/PlayTest/map_arg.txt; run map_step.cs; step 120; still map_planet
  echo choose > Tools/PlayTest/map_arg.txt; run map_step.cs; step 120; still map_region2
  run map_step.cs; step 120; still map_level
fi
if [ "$what" = "base" ] || [ "$what" = "all" ]; then
  echo 0 > Tools/PlayTest/level_arg.txt; run start_level.cs; sleep 6; step 100; step 60
  run no_waves.cs
  echo base > Tools/PlayTest/zoom_arg.txt
  for z in 6 3; do echo $z > Tools/PlayTest/zoom_size.txt; run zoom.cs; step 5; still "base_zoom$z"; done
fi
echo "=== CONSOLE"
unity command console --result-only 2>&1 | grep -o '"message": "[^"]*' | grep -v "allocation" | sort | uniq -c | sort -rn | head -10
unity command editor_stop --result-only >/dev/null 2>&1
