#!/usr/bin/env bash
# From a Boot play session: the main menu, then the options panel (with Replay intro), rendered at 1920x1080 into
# $CUT_OUT (default Logs)/<prefix>_menu.png and <prefix>_options.png; then presses Replay intro and reports the scene.
set -u
cd "$(dirname "$0")/../.." || exit 1
prefix=${1:-menu}
out=${CUT_OUT:-Logs}
status=$(unity command editor_status --result-only 2>&1)
if ! echo "$status" | grep -q '"playMode": *"stopped"'; then echo "NOT STOPPED - abort"; exit 1; fi
unity command clear_console --result-only >/dev/null 2>&1
unity command open_scene --path Assets/_Project/Scenes/Boot.unity --result-only >/dev/null 2>&1
unity command editor_play --result-only >/dev/null 2>&1
sleep 8
step() { echo "$1" > Tools/PlayTest/frames_arg.txt; unity command eval_file --file Tools/PlayTest/step_frames.cs --result-only >/dev/null 2>&1; }
run() { unity command eval_file --file "Tools/PlayTest/$1" --result-only 2>&1 | grep -o '"result": "[^"]*' | cut -c12-300; }
still() { echo "$out/${prefix}_$1.png" > Tools/PlayTest/look_arg.txt; run render_still.cs; }
step 20
run goto_menu.cs
sleep 3
step 30
echo "Options" > Tools/PlayTest/button_arg.txt
run click_button.cs
step 20
still options
echo "Replay intro" > Tools/PlayTest/button_arg.txt
run click_button.cs
sleep 4
step 30
run cutscene_probe.cs
echo "=== CONSOLE"
unity command console --result-only 2>&1 | grep -o '"message": "[^"]*' | grep -v "allocation" | sort | uniq -c | sort -rn | head -8
unity command editor_stop --result-only >/dev/null 2>&1
