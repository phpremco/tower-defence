#!/usr/bin/env bash
# UI fix (2026-11-18): stills of the planet map's help line at 1920x1080 and 1280x720 into
# ArtSource/Previews/QA/ui_levelpicker_hints_<tag>_<state>_<size>.png: the region layer and the planet layer with the
# keyboard, the region layer with an Xbox and a PlayStation pad, the planet layer with the PlayStation pad.
# GAMEVIEW=1 adds the editor's own Game view capture of the keyboard region layer (1280x720, the overlay as the game draws it).
# Keeps the save folder: progress.json, its .bak and settings.json are copied aside and put back, MD5 printed at both ends.
set -u
cd "$(dirname "$0")/../.." || exit 1
tag=${1:-after}
out=ArtSource/Previews/QA
saves="$USERPROFILE/AppData/LocalLow/DefaultCompany/Tower defense/Saves"
keep="${TEMP:-/tmp}/ui_map_hints_save_$$"
status=$(unity command editor_status --result-only 2>&1)
if ! echo "$status" | grep -q '"playMode": *"stopped"'; then echo "NOT STOPPED - abort"; exit 1; fi
mkdir -p "$keep"
for f in progress.json progress.json.bak settings.json; do [ -f "$saves/$f" ] && cp -p "$saves/$f" "$keep/$f"; done
echo "save before: $(md5sum "$saves/progress.json" | cut -c1-32)"
unity command clear_console --result-only >/dev/null 2>&1
unity command open_scene --path Assets/_Project/Scenes/Boot.unity --result-only >/dev/null 2>&1
unity command editor_play --result-only >/dev/null 2>&1
for i in 1 2 3 4; do sleep 2; done
step() { echo "$1" > Tools/PlayTest/frames_arg.txt; unity command eval_file --file Tools/PlayTest/step_frames.cs --result-only >/dev/null 2>&1; }
run() { unity command eval_file --file "Tools/PlayTest/$1" --result-only 2>&1 | grep -o '"result": "[^"]*' | cut -c12-400; }
still() {
  echo "$out/ui_levelpicker_hints_${tag}_$1_1080.png 1920 1080" > Tools/PlayTest/look_arg.txt; run r73_still.cs
  echo "$out/ui_levelpicker_hints_${tag}_$1_720.png 1280 720" > Tools/PlayTest/look_arg.txt; run r73_still.cs
}
map() { echo "$1" > Tools/PlayTest/map_arg.txt; run map_step.cs; }
pad() { echo "$1" > Tools/PlayTest/r70_pad_arg.txt; run r70_pad.cs; }
step 30
run goto_map.cs
sleep 4
step 90; step 60
still keys_region
if [ "${GAMEVIEW:-0}" = 1 ]; then unity command capture_game_view --save_path "Temp/ui_map_hints_gameview.png" --result-only >/dev/null 2>&1; cp Assets/Temp/ui_map_hints_gameview.png "$out/ui_levelpicker_hints_${tag}_keys_region_gameview.png"; unity command delete_asset --asset Assets/Temp/ui_map_hints_gameview.png --confirm true --result-only >/dev/null 2>&1; fi
map cancel; step 90
still keys_planet
map choose; step 90
pad XInputControllerWindows; step 10
still xbox_region
pad DualSenseGamepadHID; step 10
still ps_region
map cancel; step 90
still ps_planet
pad remove
step 5
echo "=== CONSOLE"
unity command console_status --result-only 2>&1 | grep -E '"(error|warn)"'
unity command editor_stop --result-only >/dev/null 2>&1
sleep 3
for f in progress.json progress.json.bak settings.json; do
  if [ -f "$keep/$f" ]; then cp -p "$keep/$f" "$saves/$f"; else rm -f "$saves/$f"; fi
done
rm -rf "$keep"
echo "save after: $(md5sum "$saves/progress.json" | cut -c1-32)"
