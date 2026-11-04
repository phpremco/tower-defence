#!/usr/bin/env bash
# Planet pass 2026-11-04: renders the planet map (whole planet with each region selected, a region zoomed with its
# levels, a level close-up) and the cutscene's survey and orbit shots plus mothership close-ups at 1920x1080 into
# $OUT/<prefix>_*.png. Usage: fb2_shots.sh <prefix> [map|cut|all]. Stops Play Mode at the end. LOW=1 renders Low.
set -u
cd "$(dirname "$0")/../.." || exit 1
prefix=${1:-fb2}
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
map() { echo "$1" > Tools/PlayTest/map_arg.txt; run map_step.cs; step "${2:-120}"; }
step 20
[ "${LOW:-0}" = "1" ] && run low_profile.cs
if [ "$what" = "map" ] || [ "$what" = "all" ]; then
  run goto_map.cs; sleep 4; step 120; still map_region
  map cancel; still map_planet_r0
  for r in 1 2 3 4 5; do map next; still "map_planet_r$r"; done
  for r in 1 2 3 4 5; do map prev 10; done
  step 120; still map_planet_back
  map choose; still map_region2
  map choose; still map_level
fi
if [ "$what" = "cut" ] || [ "$what" = "all" ]; then
  run cutscene_start.cs; sleep 4; step 10
  for moment in "1 0.2 survey_a" "1 0.6 survey_b" "2 0.2 orbit_a" "2 0.5 orbit_b" "2 0.75 orbit_c"; do
    set -- $moment
    echo "$1 $2" > Tools/PlayTest/cut_arg.txt
    unity command eval_file --file Tools/PlayTest/cutscene_seek.cs --result-only 2>&1 | grep -o '"result": "[^"]*' | cut -c12-160
    step 3
    still "cut_$3"
  done
  # Mothership close-ups: "seek shot progress | camera offset from the ship (ship space) x y z | look offset" lines.
  for close in "2 0.3 ship_side 9 3 4" "2 0.3 ship_bay 2 -6 -3" "2 0.55 ship_rear -4 2 -14" "2 0.45 ship_drop 6 -8 -6"; do
    set -- $close
    echo "$1 $2" > Tools/PlayTest/cut_arg.txt
    unity command eval_file --file Tools/PlayTest/cutscene_seek.cs --result-only >/dev/null 2>&1
    step 3
    unity command eval_file --file Tools/PlayTest/cutscene_seek.cs --result-only >/dev/null 2>&1
    echo "$4 $5 $6" > Tools/PlayTest/fb2_ship_arg.txt
    echo "$out/${prefix}_cut_$3.png" > Tools/PlayTest/look_arg.txt
    run fb2_ship.cs
  done
fi
echo "=== CONSOLE"
unity command console_status --result-only 2>&1 | head -12
unity command editor_stop --result-only >/dev/null 2>&1
