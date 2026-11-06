#!/usr/bin/env bash
# Round 15: plays level_arg's level from Boot, launches every drone the save allows (three rounds of r14_launch.cs),
# stills the base with its idle drones (close and at the play zoom), then wrecks the gates, destroys the base and stills the wrecks.
# Usage: r15_base.sh <output dir> <prefix> [wreck|drones] (wreck skips the drone launch and stills, drones stops after them). Restores nothing: back up the save before and restore it after.
set -u
cd "$(dirname "$0")/../.." || exit 1
out=$1; prefix=$2
status=$(unity command editor_status --result-only 2>&1)
if ! echo "$status" | grep -q '"playMode": *"stopped"'; then echo "NOT STOPPED - abort"; exit 1; fi
unity command clear_console --result-only >/dev/null 2>&1
unity command open_scene --path Assets/_Project/Scenes/Boot.unity --result-only >/dev/null 2>&1
unity command editor_play --result-only 2>&1 | grep -o "Entered[^\"]*"
sleep 8
step() { echo "$1" > Tools/PlayTest/frames_arg.txt; unity command eval_file --file Tools/PlayTest/step_frames.cs --result-only >/dev/null 2>&1; }
run() { unity command eval_file --file "Tools/PlayTest/$1" --result-only 2>&1 | grep -o '"result": "[^"]*' | cut -c12-400; }
still() { echo "$out/${prefix}_$1.png" > Tools/PlayTest/look_arg.txt; run render_still.cs; }
step 60; run start_level.cs; sleep 5; step 60; step 60
run no_waves.cs
if [ "${3:-}" != "wreck" ]; then
for round in 1 2 3; do run r14_launch.cs; step 30; done
for i in 1 2 3 4 5 6 7 8 9 10; do step 100; done
run r14_drones.cs
run r15_drone_local.cs
echo "base 6" > Tools/PlayTest/r14_focus.txt; run r14_focus.cs; step 5; still drones_close
echo "play 0" > Tools/PlayTest/r14_focus.txt; run r14_focus.cs; step 5; still drones_play
fi
if [ "${3:-}" = "drones" ]; then unity command editor_stop --result-only >/dev/null 2>&1; exit 0; fi
run wreck.cs
run base_die.cs
for i in 1 2; do step 100; done
echo "play 0" > Tools/PlayTest/r14_focus.txt; run r14_focus.cs; step 2; still wreck_play_early
still_world() { echo "$out/${prefix}_$1.png" > Tools/PlayTest/look_arg.txt; run r15_still_world.cs; }
still_world wreck_play_world_early
echo "base 5" > Tools/PlayTest/r14_focus.txt; run r14_focus.cs; step 2; still_world wreck_close_world_early
for i in 1 2 3 4 5 6 7 8; do step 100; done
still_world wreck_close_world_late
echo "play 0" > Tools/PlayTest/r14_focus.txt; run r14_focus.cs; step 2; still_world wreck_play_world_late
echo "=== CONSOLE"
unity command console --result-only 2>&1 | grep -o '"message": "[^"]*' | grep -v "allocation" | sort | uniq -c | sort -rn | head -8
unity command editor_stop --result-only >/dev/null 2>&1
