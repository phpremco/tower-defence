#!/usr/bin/env bash
# Round 70: stills of the new options pages and the button glyphs, each at 1920x1080 and 1280x720, into
# ArtSource/Previews/QA/r70_*.png: options, display page, keep-settings prompt, controls page with Xbox glyphs, the same
# with PlayStation glyphs (a virtual DualSense), the rebind wait, a level HUD with the hero badges and a Voss line with
# pad glyphs. Then the console summary. The caller backs up and restores the save. Stops Play Mode at the end.
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
  echo "$out/r70_$1_1080.png 1920 1080" > Tools/PlayTest/look_arg.txt; run r70_still.cs
  echo "$out/r70_$1_720.png 1280 720" > Tools/PlayTest/look_arg.txt; run r70_still.cs
}
click() { echo "$1" > Tools/PlayTest/button_arg.txt; run click_button.cs; }
pad() { echo "$1" > Tools/PlayTest/r70_pad_arg.txt; run r70_pad.cs; }
rebind() { echo "$1" > Tools/PlayTest/r70_rebind_arg.txt; run r70_rebind.cs; }
step 30
run goto_menu.cs
sleep 3
step 40
click "Options"
step 20
still options
click "Display and graphics"
step 20
still display
click "Apply display settings"
step 10
still keep_prompt
click "Revert"
step 10
click "Back"
step 10
click "Controls"
step 20
rebind probe
still controls_xbox
pad DualSenseGamepadHID
step 10
rebind probe
still controls_ps
rebind start
step 5
sleep 1
step 5
still rebind_wait
rebind cancel
step 20
sleep 1
step 10
click "Back"
step 10
click "Back"
step 10
echo 0 > Tools/PlayTest/level_arg.txt
run start_level.cs
sleep 6
step 100; step 100; step 100
pad XInputControllerWindows
step 10
still level_hud_xbox
echo voss > Tools/PlayTest/dialogue_arg.txt
run dialogue_show.cs
step 40
run dialogue_finish.cs
step 30
still voss_xbox
pad DualSenseGamepadHID
step 10
still voss_ps
pad remove
step 10
echo "=== CONSOLE"
unity command console --result-only 2>&1 | grep -o '"message": "[^"]*' | grep -v "allocation" | sort | uniq -c | sort -rn | head -12
unity command console_status --result-only 2>&1 | grep -E '"(error|warn)"'
unity command editor_stop --result-only >/dev/null 2>&1
