#!/usr/bin/env bash
# Round 78: the next-wave preview in the HUD's top strip, with stills at 1920x1080 and 1280x720 in
# ArtSource/Previews/QA/r78_*.png. Usage: r78_shots.sh <campaign node> <name> <wave> [<name> <wave> ...]: plays the node on
# Medium from Boot, takes "<name>" when wave <wave> is the next one (0 waves started = the opening countdown), walking there
# with the developer fast-forward (every creature killed on the way). The caller backs up and restores the save and deletes
# any settings.json the run writes. Stops Play Mode at the end. Env: R78_LOW=1 plays on the Low (Switch) profile (the
# tilt-shift pass is switched back on afterwards), R78_MARGIN=<percent> lays the HUD out inside that safe margin,
# R78_CAPTURE=1 also saves the Game view itself (capture_game_view) as r78_<name>_gameview.png.
set -u
cd "$(dirname "$0")/../.." || exit 1
out=ArtSource/Previews/QA
status=$(unity command editor_status --result-only 2>&1)
if ! echo "$status" | grep -q '"playMode": *"stopped"'; then echo "NOT STOPPED - abort"; exit 1; fi
node=$1; shift
unity command clear_console --result-only >/dev/null 2>&1
unity command open_scene --path Assets/_Project/Scenes/Boot.unity --result-only >/dev/null 2>&1
unity command editor_play --result-only >/dev/null 2>&1
sleep 8
step() { echo "$1" > Tools/PlayTest/frames_arg.txt; unity command eval_file --file Tools/PlayTest/step_frames.cs --result-only >/dev/null 2>&1; }
run() { unity command eval_file --file "Tools/PlayTest/$1" --result-only 2>&1 | grep -o '"result": "[^"]*' | cut -c12-1800; }
still() {
  echo "$out/r78_$1_1080.png 1920 1080" > Tools/PlayTest/look_arg.txt; run r73_still.cs
  echo "$out/r78_$1_720.png 1280 720" > Tools/PlayTest/look_arg.txt; run r73_still.cs
}
preview() { echo "$1" > Tools/PlayTest/r78_preview_arg.txt; echo "-- $(run r78_preview.cs)"; }
step 30
if [ -n "${R78_LOW:-}" ]; then run low_profile.cs; fi
if [ -n "${R78_MARGIN:-}" ]; then echo "$R78_MARGIN" > Tools/PlayTest/r78_margin_arg.txt; run r78_margin.cs; fi
echo "$node" > Tools/PlayTest/level_arg.txt
run start_level.cs
sleep 6
step "100 hold"
preview list
started=0
while [ $# -ge 2 ]; do
  name=$1; wave=$2; shift 2
  while [ $started -lt $((wave - 1)) ]; do
    echo fast > Tools/PlayTest/r78_preview_arg.txt; run r78_preview.cs >/dev/null
    step "4 hold"
    started=$((started + 1))
  done
  step "12 hold"
  preview probe
  still "$name"
  if [ -n "${R78_CAPTURE:-}" ]; then
    # The Game view itself, as the editor shows it (1280x720 whatever the view's size): the HUD without the still's post stack.
    step 2
    unity command capture_game_view --save_path "Temp/r78_${name}_gameview.png" --result-only >/dev/null 2>&1
    mv "Assets/Temp/r78_${name}_gameview.png" "$out/r78_${name}_gameview.png" && rm -f "Assets/Temp/r78_${name}_gameview.png.meta"
    step "2 hold"
  fi
done
if [ -n "${R78_LOW:-}" ]; then run high_profile.cs; fi
echo "=== CONSOLE"
unity command console --result-only 2>&1 | grep -o '"message": "[^"]*' | grep -v "allocation" | sort | uniq -c | sort -rn | head -12
unity command console_status --result-only 2>&1 | grep -E '"(error|warn)"'
unity command editor_stop --result-only >/dev/null 2>&1
