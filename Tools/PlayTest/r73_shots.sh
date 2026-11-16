#!/usr/bin/env bash
# Round 73: the call-next-wave button in a level (Landing Site, Medium), with stills at 1920x1080 and 1280x720 in
# ArtSource/Previews/QA/r73_*.png: the lit button with its keyboard glyph between waves, the "+X early" text after a real
# N key press, then the same with a virtual XInput pad (the View glyph, a real tap of View). Waves are fast-forwarded with
# the developer tool so the countdown comes quickly. The caller backs up and restores the save and deletes any
# settings.json the run writes. Stops Play Mode at the end.
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
  echo "$out/r73_$1_1080.png 1920 1080" > Tools/PlayTest/look_arg.txt; run r73_still.cs
  echo "$out/r73_$1_720.png 1280 720" > Tools/PlayTest/look_arg.txt; run r73_still.cs
}
call() { echo "$1" > Tools/PlayTest/r73_call_arg.txt; echo "-- $(run r73_call.cs)"; }
pad() { echo "$1" > Tools/PlayTest/r70_pad_arg.txt; run r70_pad.cs; }
press() { echo "$1" > Tools/PlayTest/r70_press_arg.txt; run r70_press.cs | cut -c1-40; }
step 30
echo 0 > Tools/PlayTest/level_arg.txt
run start_level.cs
sleep 6
step "100 hold"
call probe
call flush
step "5 hold"
call probe
still hud_call_keyboard
step 1
call "key n"
step "3 hold"
call probe
still early_bonus_keyboard
call "key n"
echo "(a second N right after: refused)"
echo "=== pad"
pad XInputControllerWindows
step 200
call flush
step "5 hold"
call probe
still hud_call_pad
step 1
call "tap select 60"
step "3 hold"
call probe
still early_bonus_pad
pad remove
step 10
echo "=== CONSOLE"
unity command console --result-only 2>&1 | grep -o '"message": "[^"]*' | grep -v "allocation" | sort | uniq -c | sort -rn | head -12
unity command console_status --result-only 2>&1 | grep -E '"(error|warn)"'
unity command editor_stop --result-only >/dev/null 2>&1
