#!/usr/bin/env bash
# Keeps a cutscene session open for tuning: fb1_tune.sh start | seek "<shot> <progress>" | tune <name> | stop.
set -u
cd "$(dirname "$0")/../.." || exit 1
out=${OUT:-Logs}
step() { echo "$1" > Tools/PlayTest/frames_arg.txt; unity command eval_file --file Tools/PlayTest/step_frames.cs --result-only >/dev/null 2>&1; }
run() { unity command eval_file --file "Tools/PlayTest/$1" --result-only 2>&1 | grep -o '"result": "[^"]*' | cut -c12-400; }
case "$1" in
  start)
    status=$(unity command editor_status --result-only 2>&1)
    if ! echo "$status" | grep -q '"playMode": *"stopped"'; then echo "NOT STOPPED - abort"; exit 1; fi
    unity command clear_console --result-only >/dev/null 2>&1
    unity command open_scene --path Assets/_Project/Scenes/Boot.unity --result-only >/dev/null 2>&1
    unity command editor_play --result-only 2>&1 | grep -o "Entered[^\"]*"; sleep 8; step 20; run cutscene_start.cs; sleep 4; step 10;;
  seek) echo "$2" > Tools/PlayTest/cut_arg.txt; run cutscene_seek.cs; step 3; run cutscene_seek.cs >/dev/null;;
  tune) run cutscene_seek.cs >/dev/null; run fb1_tune.cs; echo "$out/$2.png" > Tools/PlayTest/look_arg.txt; run render_still.cs;;
  stop) unity command console --result-only 2>&1 | grep -o '"message": "[^"]*' | grep -v "allocation" | sort | uniq -c | sort -rn | head -8; unity command editor_stop --result-only >/dev/null 2>&1;;
esac
