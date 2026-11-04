#!/usr/bin/env bash
# Import stage 4b: build spot screenshots (no tiles at rest, soft glow round the cursor while placing). Plays a level
# with the waves off (imp4_phase.cs) and captures rest, rest close, placing and placing close into
# Assets/Temp/<prefix>_L<level>_*.png (copied to $SHOTS when set). Usage: imp4b_pads.sh <prefix> <level> [level ...].
# Restores the save from $SAVE_BACKUP when set.
set -u
cd "$(dirname "$0")/../.." || exit 1
prefix=${1:-pads}; shift
status=$(unity command editor_status --result-only 2>&1)
if ! echo "$status" | grep -q '"playMode": *"stopped"'; then echo "NOT STOPPED - abort"; exit 1; fi
unity command clear_console --result-only >/dev/null 2>&1
unity command open_scene --path Assets/_Project/Scenes/Boot.unity --result-only >/dev/null 2>&1
unity command editor_play --result-only 2>&1 | grep -o "Entered[^\"]*"
sleep 8
run() { unity command eval_file --file "Tools/PlayTest/$1" --result-only 2>&1 | grep -o '"result": "[^"]*' | cut -c12-400; }
phase() { echo "$*" > Tools/PlayTest/imp4_phase.txt; echo "$(run imp4_phase.cs)"; }
shot() { unity command capture_game_view --save_path "Temp/${prefix}_L${level}_$1.png" --result-only >/dev/null 2>&1; }
live() { phase play >/dev/null; sleep "$1"; phase pause >/dev/null; }
for level in "$@"; do
  phase play >/dev/null
  echo "$level" > Tools/PlayTest/level_arg.txt; run start_level.cs; sleep 10
  phase setup
  phase view road 7; live 0.5; shot rest
  phase view road 3; live 0.3; shot rest_close
  phase place; phase view road 7; live 0.8; shot placing
  phase view road 3; live 0.3; shot placing_close
  phase place off; live 0.6
done
echo "=== CONSOLE"
unity command console_status --result-only 2>&1 | grep -o '"consoleErrors": [0-9]*\|"consoleWarnings": [0-9]*'
unity command console --result-only 2>&1 | grep -o '"logType": "[A-Za-z]*", *"message": "[^"]*' | grep -v "allocation older" | cut -c1-300 | head -12
unity command editor_stop --result-only >/dev/null 2>&1
if [ -n "${SAVE_BACKUP:-}" ]; then cp "$SAVE_BACKUP" "$USERPROFILE/AppData/LocalLow/DefaultCompany/Tower defense/Saves/progress.json"; fi
if [ -n "${SHOTS:-}" ]; then mkdir -p "$SHOTS"; cp Assets/Temp/${prefix}_L*.png "$SHOTS/"; fi
echo "=== END"
