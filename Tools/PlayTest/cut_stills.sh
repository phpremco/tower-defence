#!/usr/bin/env bash
# Plays the opening cutscene from Boot and renders sought moments at 1920x1080 into $CUT_OUT (default Logs)/<prefix>_<shot>_<progress>.png.
# Usage: cut_stills.sh <prefix> "<shot> <progress> [dlg]" ... (shot index from 0; dlg opens the shot's dialogue box).
# Low profile first: LOW=1. Stops Play Mode at the end.
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
[ "${LOW:-0}" = "1" ] && run low_profile.cs
run cutscene_start.cs
sleep 4
step 10
for moment in "$@"; do
  echo "$moment" > Tools/PlayTest/cut_arg.txt
  unity command eval_file --file Tools/PlayTest/cutscene_seek.cs --result-only 2>&1 | grep -v "^ *\"(output|diagnostics|success)" | head -5
  step 3
  case "$moment" in *dlg*) run cut_dialogue.cs; step 90;; esac
  run cutscene_probe.cs
  name=$(echo "$moment" | tr ' .' '_p')
  echo "$out/${prefix}_${name}.png" > Tools/PlayTest/look_arg.txt; unity command eval_file --file Tools/PlayTest/render_still.cs --result-only 2>&1 | grep -o "wrote[^\"]*"
done
echo "=== CONSOLE"
unity command console --result-only 2>&1 | grep -o '"message": "[^"]*' | grep -v "allocation" | sort | uniq -c | sort -rn | head -8
unity command editor_stop --result-only >/dev/null 2>&1
