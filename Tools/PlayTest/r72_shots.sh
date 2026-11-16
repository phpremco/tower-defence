#!/usr/bin/env bash
# Round 72: stills of the main menu with the version corner and of the credits screen (top, middle, licence text), each
# at 1920x1080 and 1280x720, into ArtSource/Previews/QA/r72_*.png; a gamepad check (a virtual pad's left stick scrolls
# the page, B closes it and the focus returns to Credits) and the console summary. The caller backs up and restores the
# save and deletes any settings.json the run writes. Stops Play Mode at the end.
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
run() { unity command eval_file --file "Tools/PlayTest/$1" --result-only 2>&1 | grep -o '"result": "[^"]*' | cut -c12-400; }
still() {
  echo "$out/r72_$1_1080.png 1920 1080" > Tools/PlayTest/look_arg.txt; run r70_still.cs
  echo "$out/r72_$1_720.png 1280 720" > Tools/PlayTest/look_arg.txt; run r70_still.cs
}
click() { echo "$1" > Tools/PlayTest/button_arg.txt; run click_button.cs; }
credits() { echo "$1" > Tools/PlayTest/r72_credits_arg.txt; run r72_credits.cs; }
pad() { echo "$1" > Tools/PlayTest/r70_pad_arg.txt; run r70_pad.cs; }
press() { echo "$1" > Tools/PlayTest/r70_press_arg.txt; run r70_press.cs; }
step 30
run goto_menu.cs
sleep 3
step 40
still main_menu
click "Credits"
step 20
credits probe
still credits_top
credits "scroll 0.55"
step 5
still credits_middle
credits "scroll 0"
step 5
still credits_licence
credits "scroll 1"
step 5
echo "=== gamepad: stick scrolls, B closes"
pad XInputControllerWindows
step 10
credits "stick -1"
step 30
credits probe
credits "stick 0"
step 5
press buttonEast
step 10
credits probe
run r72_selected.cs
pad remove
step 10
echo "=== CONSOLE"
unity command console --result-only 2>&1 | grep -o '"message": "[^"]*' | grep -v "allocation" | sort | uniq -c | sort -rn | head -12
unity command console_status --result-only 2>&1 | grep -E '"(error|warn)"'
unity command editor_stop --result-only >/dev/null 2>&1
