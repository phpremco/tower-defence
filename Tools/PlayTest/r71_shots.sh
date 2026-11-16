#!/usr/bin/env bash
# Round 71: the Nintendo confirm swap, button tokens in a Voss line and the options inside the pause menu, with stills
# at 1920x1080 and 1280x720 in ArtSource/Previews/QA/r71_*.png and real queued pad presses (a virtual Switch Pro pad):
# B (south) must go back and A (east) must confirm. Removing a pad pauses the level (a lost controller), so the run
# resumes it each time. The caller backs up and restores the save. Stops Play Mode at the end.
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
run() { unity command eval_file --file "Tools/PlayTest/$1" --result-only 2>&1 | grep -o '"result": "[^"]*' | cut -c12-600; }
still() {
  echo "$out/r71_$1_1080.png 1920 1080" > Tools/PlayTest/look_arg.txt; run r70_still.cs
  echo "$out/r71_$1_720.png 1280 720" > Tools/PlayTest/look_arg.txt; run r70_still.cs
}
click() { echo "$1" > Tools/PlayTest/button_arg.txt; run click_button.cs | cut -c1-60; }
pad() { echo "$1" > Tools/PlayTest/r70_pad_arg.txt; run r70_pad.cs; }
press() { echo "$1" > Tools/PlayTest/r70_press_arg.txt; run r70_press.cs | cut -c1-40; step 5; }
probe() { echo "-- $1: $(run r71_probe.cs)"; }
step 30
run goto_menu.cs
sleep 3
step 40
probe "menu, keyboard"
pad SwitchProControllerHID
step 10
probe "menu, Switch Pro pad"
click "Options"
step 20
click "Controls"
step 20
still controls_nintendo
probe "controls page"
press buttonSouth
step 10
probe "after B (south) on the controls page: back to the options column"
click "Back"
step 10
pad remove
step 5
echo 0 > Tools/PlayTest/level_arg.txt
run start_level.cs
sleep 6
step 100; step 100; step 100
pad SwitchProControllerHID
step 10
echo Dlg_L02_RepairDrone > Tools/PlayTest/r71_dialogue_arg.txt
run r71_dialogue.cs
step 30
probe "before A"
press buttonEast
probe "after A (east): confirm moved the dialogue on"
run r71_goto_line.cs
step 10
probe "drone line shown"
still hint_nintendo
press buttonSouth
probe "after B (south): cancel skipped the conversation"
pad remove
step 5
echo off > Tools/PlayTest/r71_pause_arg.txt
run r71_pause.cs
pad XInputControllerWindows
step 5
run r71_pause.cs
run r71_dialogue.cs
step 30
probe "before A (south) on Xbox"
press buttonSouth
probe "after A (south) on Xbox"
run r71_goto_line.cs
step 10
probe "drone line, Xbox"
still hint_xbox
pad remove
step 5
run r71_pause.cs
run r71_advance.cs
run r71_advance.cs
run r71_advance.cs
step 10
pad SwitchProControllerHID
step 5
echo on > Tools/PlayTest/r71_pause_arg.txt
run r71_pause.cs
step 10
click "Options"
step 20
probe "pause > options"
still pause_options
click "Controls"
step 20
still pause_controls
press buttonSouth
step 10
probe "after B on the controls page"
press buttonSouth
step 10
probe "after B on the options column (back to the pause menu, still paused)"
echo off > Tools/PlayTest/r71_pause_arg.txt
run r71_pause.cs
step 10
pad remove
step 5
echo "=== CONSOLE"
unity command console --result-only 2>&1 | grep -o '"message": "[^"]*' | grep -v "allocation" | sort | uniq -c | sort -rn | head -12
unity command console_status --result-only 2>&1 | grep -E '"(error|warn)"'
unity command editor_stop --result-only >/dev/null 2>&1
