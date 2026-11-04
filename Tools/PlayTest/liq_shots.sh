#!/usr/bin/env bash
# Liquids and origins round screenshots (liq_phase.cs): swaps every other water pool of a level to lava in memory,
# plays it, frames the field, the largest water and lava pools at play zoom and close up. Pictures go to
# Assets/Temp/<prefix>_*.png (copied to $SHOTS when set); the save is restored from $SAVE_BACKUP when set.
# Usage: liq_shots.sh <prefix> <level index> [phase script, default liq_phase.cs] [LOW=1 in the environment for the Low profile]
set -u
cd "$(dirname "$0")/../.." || exit 1
prefix=${1:-liq}; level=${2:-4}; script=${3:-liq_phase.cs}
status=$(unity command editor_status --result-only 2>&1)
if ! echo "$status" | grep -q '"playMode": *"stopped"'; then echo "NOT STOPPED - abort"; exit 1; fi
run() { unity command eval_file --file "Tools/PlayTest/$1" --result-only 2>&1 | grep -o '"result": "[^"]*' | cut -c12-900; }
liq() { echo "$*" > Tools/PlayTest/liq_phase.txt; echo "$(run $script)"; }
shot() { unity command capture_game_view --save_path "Temp/${prefix}_$1.png" --result-only >/dev/null 2>&1; }
step() { echo "$1" > Tools/PlayTest/frames_arg.txt; run step_frames.cs >/dev/null; }
unity command clear_console --result-only >/dev/null 2>&1
liq swap apply "$level"
unity command open_scene --path Assets/_Project/Scenes/Boot.unity --result-only >/dev/null 2>&1
unity command editor_play --result-only 2>&1 | grep -o "Entered[^\"]*"
sleep 8
[ "${LOW:-0}" = "1" ] && run low_profile.cs
echo "$level" > Tools/PlayTest/level_arg.txt; run start_level.cs; sleep 10
liq waves off
liq probe
liq view all 0; step 3; shot field
liq view Water 6; step 3; shot water_play
liq view Water 3; step 3; shot water_close
liq view Lava 6; step 3; shot lava_play
liq view Lava 3; step 3; shot lava_close
echo "=== CONSOLE"
unity command console_status --result-only 2>&1 | grep -o '"consoleErrors": [0-9]*\|"consoleWarnings": [0-9]*'
unity command console --level error --tail 3 --result-only 2>&1 | grep -o '"message": "[^"]\{0,300\}'
unity command editor_stop --result-only >/dev/null 2>&1
sleep 3
liq swap restore "$level"
if [ -n "${SAVE_BACKUP:-}" ]; then cp "$SAVE_BACKUP" "$USERPROFILE/AppData/LocalLow/DefaultCompany/Tower defense/Saves/progress.json"; fi
if [ -n "${SHOTS:-}" ]; then mkdir -p "$SHOTS"; cp Assets/Temp/${prefix}_*.png "$SHOTS/"; fi
echo "=== END"
