#!/usr/bin/env bash
# Round 73: a real tap of the pad's View button calls the next wave (a virtual XInput pad, Landing Site Medium); a hold
# opens the overview instead. The caller backs up and restores the save. Stops Play Mode at the end.
set -u
cd "$(dirname "$0")/../.." || exit 1
status=$(unity command editor_status --result-only 2>&1)
if ! echo "$status" | grep -q '"playMode": *"stopped"'; then echo "NOT STOPPED - abort"; exit 1; fi
unity command clear_console --result-only >/dev/null 2>&1
unity command open_scene --path Assets/_Project/Scenes/Boot.unity --result-only >/dev/null 2>&1
unity command editor_play --result-only >/dev/null 2>&1
sleep 8
step() { echo "$1" > Tools/PlayTest/frames_arg.txt; unity command eval_file --file Tools/PlayTest/step_frames.cs --result-only >/dev/null 2>&1; }
run() { unity command eval_file --file "Tools/PlayTest/$1" --result-only 2>&1 | grep -o '"result": "[^"]*' | cut -c12-600; }
call() { echo "$1" > Tools/PlayTest/r73_call_arg.txt; echo "-- $(run r73_call.cs)"; }
pad() { echo "$1" > Tools/PlayTest/r70_pad_arg.txt; run r70_pad.cs; }
step 30
echo 0 > Tools/PlayTest/level_arg.txt
run start_level.cs
sleep 6
step 100
pad XInputControllerWindows
step 10
call flush
call "tap select 60"
step 3
call probe
call flush
call "tap select 400"
echo "(a 0.4 s hold: the overview, no call)"
step 3
call probe
pad remove
step 10
unity command console_status --result-only 2>&1 | grep -E '"(error|warn)"'
unity command editor_stop --result-only >/dev/null 2>&1
