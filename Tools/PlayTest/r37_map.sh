#!/usr/bin/env bash
# Round 37: Steam screenshots of the planet map: the region view of Landfall and, after "cancel", the whole planet.
# Renders 3840x2160 off-screen (r37_shot.cs, camera kept) with and without the interface. Keeps the save as it was.
# Usage: OUT=<dir> r37_map.sh <prefix>
set -u
cd "$(dirname "$0")/../.." || exit 1
prefix=${1:-map}; out=${OUT:-Logs}
save="$LOCALAPPDATA/../LocalLow/DefaultCompany/Tower defense/Saves/progress.json"
backup="$out/${prefix}_progress_before.json"
had=0; if [ -f "$save" ]; then had=1; cp "$save" "$backup"; md5sum "$save"; fi
status=$(unity command editor_status --result-only 2>&1)
if ! echo "$status" | grep -q '"playMode": *"stopped"'; then echo "NOT STOPPED - abort"; exit 1; fi
unity command clear_console --result-only >/dev/null 2>&1
unity command open_scene --path Assets/_Project/Scenes/Boot.unity --result-only >/dev/null 2>&1
unity command editor_play --result-only 2>&1 | grep -o "Entered[^\"]*"
sleep 8
step() { echo "$1" > Tools/PlayTest/frames_arg.txt; unity command eval_file --file Tools/PlayTest/step_frames.cs --result-only >/dev/null 2>&1; }
run() { unity command eval_file --file "Tools/PlayTest/$1" --result-only 2>&1 | grep -o '"result": "[^"]*\|"message": "[^"]*' | cut -c1-300; }
shot() { echo "keep 1 $2 $(cygpath -w "$out/${prefix}_$1_4k.png")" > Tools/PlayTest/r37_shot_arg.txt; echo "$1: $(run r37_shot.cs)"; }
step 60
echo "$(run goto_map.cs)"; sleep 4; step 150
shot region clean; shot region_hud hud
echo cancel > Tools/PlayTest/map_arg.txt; run map_step.cs >/dev/null; step 150
shot planet clean; shot planet_hud hud
echo "=== CONSOLE"
unity command console --result-only 2>&1 | grep -o '"logType": "[A-Za-z]*", *"message": "[^"]*' | grep -v "allocation" | sort | uniq -c | sort -rn | head -6
unity command editor_stop --result-only >/dev/null 2>&1
sleep 3
if [ "$had" = 1 ]; then cp "$backup" "$save"; md5sum "$save"; fi
