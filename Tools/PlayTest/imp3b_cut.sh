#!/usr/bin/env bash
# Import stage 3b cutscene stills: plays the opening cutscene from Boot and renders sought moments (imp3b_still.cs, the
# editor held paused) into $CUT_OUT/<prefix>_<shot>_<progress>.png. Usage: imp3b_cut.sh <prefix> "<shot> <progress>" ...
set -u
cd "$(dirname "$0")/../.." || exit 1
prefix=${1:-cut}
out=${CUT_OUT:-Logs}
shift
status=$(unity command editor_status --result-only 2>&1)
if ! echo "$status" | grep -q '"playMode": *"stopped"'; then echo "NOT STOPPED - abort"; exit 1; fi
unity command clear_console --result-only >/dev/null 2>&1
unity command open_scene --path Assets/_Project/Scenes/Boot.unity --result-only >/dev/null 2>&1
unity command editor_play --result-only 2>&1 | grep -o "Entered[^\"]*"
sleep 8
step() { echo "$1" > Tools/PlayTest/frames_arg.txt; unity command eval_file --file Tools/PlayTest/step_frames.cs --result-only >/dev/null 2>&1; }
run() { unity command eval_file --file "Tools/PlayTest/$1" --result-only 2>&1 | grep -o '"result": "[^"]*' | cut -c12-220; }
step 20
run cutscene_start.cs
sleep 4
step 10
for moment in "$@"; do
  echo "$moment" > Tools/PlayTest/cut_arg.txt
  name=$(echo "$moment" | tr ' .' '_p')
  echo "$out/${prefix}_${name}.png" > Tools/PlayTest/look_arg.txt
  run imp3b_still.cs
done
echo "=== CONSOLE"
unity command console --result-only 2>&1 | grep -o '"logType": "[A-Za-z]*", *"message": "[^"]*' | grep -v "allocation" | sort | uniq -c | sort -rn | head -8
unity command editor_stop --result-only >/dev/null 2>&1
