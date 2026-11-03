#!/usr/bin/env bash
# Plays the opening cutscene from Boot, seeks to "<shot> <progress>" ($1) and runs each eval file named after it
# (arguments: file:arg, where arg goes to look_arg.txt). Stops Play Mode at the end.
set -u
cd "$(dirname "$0")/../.." || exit 1
moment=$1
shift
status=$(unity command editor_status --result-only 2>&1)
if ! echo "$status" | grep -q '"playMode": *"stopped"'; then echo "NOT STOPPED - abort"; exit 1; fi
unity command open_scene --path Assets/_Project/Scenes/Boot.unity --result-only >/dev/null 2>&1
unity command editor_play --result-only >/dev/null 2>&1
sleep 8
step() { echo "$1" > Tools/PlayTest/frames_arg.txt; unity command eval_file --file Tools/PlayTest/step_frames.cs --result-only >/dev/null 2>&1; }
step 20
unity command eval_file --file Tools/PlayTest/cutscene_start.cs --result-only >/dev/null 2>&1
sleep 4
step 10
echo "$moment" > Tools/PlayTest/cut_arg.txt
unity command eval_file --file Tools/PlayTest/cutscene_seek.cs --result-only >/dev/null 2>&1
step 3
for job in "$@"; do
  echo "${job#*:}" > Tools/PlayTest/look_arg.txt
  unity command eval_file --file "Tools/PlayTest/${job%%:*}" --result-only 2>&1 | grep -o '"result": "[^"]*' | cut -c12-
done
unity command editor_stop --result-only >/dev/null 2>&1
