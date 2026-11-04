#!/usr/bin/env bash
# Round menu 2026-11-04: plays Boot to the main menu and renders it at 1920x1080 into $OUT/<prefix>_menu_<n>.png a few
# moments apart (creatures emerging), plus menu_probe.cs. Stops Play Mode at the end. LOW=1 renders the Low profile.
set -u
cd "$(dirname "$0")/../.." || exit 1
prefix=${1:-menu}
out=${OUT:-Logs}
status=$(unity command editor_status --result-only 2>&1)
if ! echo "$status" | grep -q '"playMode": *"stopped"'; then echo "NOT STOPPED - abort"; exit 1; fi
unity command clear_console --result-only >/dev/null 2>&1
unity command open_scene --path Assets/_Project/Scenes/Boot.unity --result-only >/dev/null 2>&1
unity command editor_play --result-only 2>&1 | grep -o "Entered[^\"]*"
sleep 8
step() { echo "$1" > Tools/PlayTest/frames_arg.txt; unity command eval_file --file Tools/PlayTest/step_frames.cs --result-only >/dev/null 2>&1; }
run() { unity command eval_file --file "Tools/PlayTest/$1" --result-only 2>&1 | grep -o '"result": "[^"]*' | cut -c12-600; }
still() { echo "$out/${prefix}_$1.png" > Tools/PlayTest/look_arg.txt; unity command eval_file --file Tools/PlayTest/render_still.cs --result-only 2>&1 | grep -o "wrote[^\"]*"; }
[ "${LOW:-0}" = "1" ] && run low_profile.cs
step 30; still menu_1
step 120; still menu_2
step 200; still menu_3
run menu_probe.cs
echo "=== CONSOLE"
unity command console_status --result-only 2>&1 | grep -o '"consoleErrors": [0-9]*\|"consoleWarnings": [0-9]*'
unity command console --result-only 2>&1 | grep -o '"message": "[^"]*' | grep -v "allocation" | sort | uniq -c | head -12
unity command editor_stop --result-only >/dev/null 2>&1
