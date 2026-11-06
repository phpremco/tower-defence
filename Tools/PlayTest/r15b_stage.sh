#!/usr/bin/env bash
# Round 15b: loads a boss arena, stages a prop (r15b_props.cs) and stills it at two zooms.
# Usage: r15b_stage.sh <output dir> <boss> <ice|vines> <prefix>
set -u
cd "$(dirname "$0")/../.." || exit 1
out=$1; boss=$2; what=$3; prefix=$4
status=$(unity command editor_status --result-only 2>&1)
if ! echo "$status" | grep -q '"playMode": *"stopped"'; then echo "NOT STOPPED - abort"; exit 1; fi
unity command clear_console --result-only >/dev/null 2>&1
unity command open_scene --path Assets/_Project/Scenes/Boot.unity --result-only >/dev/null 2>&1
unity command editor_play --result-only 2>&1 | grep -o "Entered[^\"]*"
sleep 8
step() { echo "$1" > Tools/PlayTest/frames_arg.txt; unity command eval_file --file Tools/PlayTest/step_frames.cs --result-only >/dev/null 2>&1; }
run() { unity command eval_file --file "Tools/PlayTest/$1" --result-only 2>&1 | grep -o '"result": "[^"]*\|"message": "[^"]*' | cut -c1-400; }
still() { echo "$out/${prefix}_$1.png" > Tools/PlayTest/look_arg.txt; run render_still.cs; }
echo "$boss" > Tools/PlayTest/r14_boss.txt; step 60; sleep 2; run r14_arena.cs; sleep 8
# The arena takes a while to build: step until the wave timeline can be switched off.
for i in $(seq 1 20); do step 30; sleep 2; r=$(run no_waves.cs); case "$r" in *"waves off"*) echo "$r"; break;; esac; done
if [ "$what" = vines ]; then run build_tower.cs; step 20; fi
echo "$what 2.2" > Tools/PlayTest/r15b_arg.txt; run r15b_props.cs
for i in 1 2 3 4; do step 50; done
still close
echo "=== CONSOLE"
unity command console --result-only 2>&1 | grep -o '"message": "[^"]*' | grep -v "allocation" | sort | uniq -c | sort -rn | head -8
unity command editor_stop --result-only >/dev/null 2>&1
