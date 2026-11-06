#!/usr/bin/env bash
# Round 15: loads the Cinder Matriarch's arena, stills a cluster of breakable crystals by the road at the play zoom, smashes
# them as a boss body would (r15_smash.cs) and stills the remains. Usage: r15_remains.sh <output dir> <prefix> [boss]
set -u
cd "$(dirname "$0")/../.." || exit 1
out=$1; prefix=$2; boss=${3:-CinderMatriarch}
status=$(unity command editor_status --result-only 2>&1)
if ! echo "$status" | grep -q '"playMode": *"stopped"'; then echo "NOT STOPPED - abort"; exit 1; fi
unity command clear_console --result-only >/dev/null 2>&1
unity command open_scene --path Assets/_Project/Scenes/Boot.unity --result-only >/dev/null 2>&1
unity command editor_play --result-only 2>&1 | grep -o "Entered[^\"]*"
sleep 8
step() { echo "$1" > Tools/PlayTest/frames_arg.txt; unity command eval_file --file Tools/PlayTest/step_frames.cs --result-only >/dev/null 2>&1; }
run() { unity command eval_file --file "Tools/PlayTest/$1" --result-only 2>&1 | grep -o '"result": "[^"]*' | cut -c12-400; }
still() { echo "$out/${prefix}_$1.png" > Tools/PlayTest/look_arg.txt; run render_still.cs; }
echo "$boss" > Tools/PlayTest/r14_boss.txt; step 60; run r14_arena.cs; sleep 5; step 60; step 60
run no_waves.cs
echo pick > Tools/PlayTest/r15_smash_arg.txt; run r15_smash.cs; step 5; still intact
echo break > Tools/PlayTest/r15_smash_arg.txt; run r15_smash.cs; step 30; still remains
echo "=== CONSOLE"
unity command console --result-only 2>&1 | grep -o '"message": "[^"]*' | grep -v "allocation" | sort | uniq -c | sort -rn | head -8
unity command editor_stop --result-only >/dev/null 2>&1
