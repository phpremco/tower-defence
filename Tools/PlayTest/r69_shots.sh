#!/usr/bin/env bash
# Round 69: English text spot-check after the string tables. From Boot: main menu, options (with Language), a level
# HUD, the tower menu and a Voss dialogue, rendered at 1920x1080 into ArtSource/Previews/QA/r69_*.png; then the console
# summary. Back up the save before and restore it after (the caller does). Stops Play Mode at the end.
set -u
cd "$(dirname "$0")/../.." || exit 1
out=ArtSource/Previews/QA
status=$(unity command editor_status --result-only 2>&1)
if ! echo "$status" | grep -q '"playMode": *"stopped"'; then echo "NOT STOPPED - abort"; exit 1; fi
unity command clear_console --result-only >/dev/null 2>&1
unity command open_scene --path Assets/_Project/Scenes/Boot.unity --result-only >/dev/null 2>&1
unity command editor_play --result-only >/dev/null 2>&1
sleep 8
step() { echo "$1" > Tools/PlayTest/frames_arg.txt; unity command eval_file --file Tools/PlayTest/step_frames.cs --result-only >/dev/null 2>&1; }
run() { unity command eval_file --file "Tools/PlayTest/$1" --result-only 2>&1 | grep -o '"result": "[^"]*' | cut -c12-300; }
still() { echo "$out/r69_$1.png" > Tools/PlayTest/look_arg.txt; run render_still.cs; }
click() { echo "$1" > Tools/PlayTest/button_arg.txt; run click_button.cs; }
step 30
run goto_menu.cs
sleep 3
step 40
still main_menu
click "Options"
step 20
still options
click "Back"
step 10
echo 0 > Tools/PlayTest/level_arg.txt
run start_level.cs
sleep 6
step 100; step 100; step 100
still level_hud
run towers.cs
step 20
echo tower > Tools/PlayTest/menu_arg.txt
run open_menu.cs
step 20
still tower_menu
echo voss > Tools/PlayTest/dialogue_arg.txt
run dialogue_show.cs
step 40
run dialogue_finish.cs
step 30
still voss_dialogue
echo "=== CONSOLE"
unity command console --result-only 2>&1 | grep -o '"message": "[^"]*' | grep -v "allocation" | sort | uniq -c | sort -rn | head -12
unity command console_status --result-only 2>&1 | grep -E '"(error|warn)"'
unity command editor_stop --result-only >/dev/null 2>&1
