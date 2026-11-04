#!/usr/bin/env bash
# Import stage 4b: one look per continent 2-6 environment set on a continent-1 level (the theme is swapped in memory by
# imp4b_swap.cs and put back after each set; the level keeps its own baked painting, light and fog). Captures the play
# zoom, a road close-up, a bank, the border and the base into Assets/Temp/<prefix>_<Set>_*.png (copied to $SHOTS).
# Usage: imp4b_env.sh <prefix> <level> <Set> [Set ...]. Restores the save from $SAVE_BACKUP when set.
set -u
cd "$(dirname "$0")/../.." || exit 1
prefix=${1:-env}; level=$2; shift 2
status=$(unity command editor_status --result-only 2>&1)
if ! echo "$status" | grep -q '"playMode": *"stopped"'; then echo "NOT STOPPED - abort"; exit 1; fi
unity command clear_console --result-only >/dev/null 2>&1
unity command open_scene --path Assets/_Project/Scenes/Boot.unity --result-only >/dev/null 2>&1
unity command editor_play --result-only 2>&1 | grep -o "Entered[^\"]*"
sleep 8
run() { unity command eval_file --file "Tools/PlayTest/$1" --result-only 2>&1 | grep -o '"result": "[^"]*' | cut -c12-400; }
phase() { echo "$*" > Tools/PlayTest/imp4_phase.txt; echo "$(run imp4_phase.cs)"; }
shot() { unity command capture_game_view --save_path "Temp/${prefix}_${set}_$1.png" --result-only >/dev/null 2>&1; }
live() { phase play >/dev/null; sleep "$1"; phase pause >/dev/null; }
for set in "$@"; do
  phase play >/dev/null
  echo "$level $set" > Tools/PlayTest/imp4b_swap.txt; run imp4b_swap.cs
  echo "$level" > Tools/PlayTest/level_arg.txt; run start_level.cs; sleep 10
  phase setup
  phase view overview 12; live 2; shot overview
  phase view road 7; live 0.5; shot play
  phase view road 3; live 0.3; shot road
  phase view bank 3; live 0.3; shot bank
  phase view border 4; live 0.3; shot border
  phase view base 4; live 0.3; shot base
  phase play >/dev/null
  echo "$level restore" > Tools/PlayTest/imp4b_swap.txt; run imp4b_swap.cs
done
echo "=== CONSOLE"
unity command console_status --result-only 2>&1 | grep -o '"consoleErrors": [0-9]*\|"consoleWarnings": [0-9]*'
unity command console --result-only 2>&1 | grep -o '"logType": "[A-Za-z]*", *"message": "[^"]*' | grep -v "allocation older" | cut -c1-300 | head -12
unity command editor_stop --result-only >/dev/null 2>&1
if [ -n "${SAVE_BACKUP:-}" ]; then cp "$SAVE_BACKUP" "$USERPROFILE/AppData/LocalLow/DefaultCompany/Tower defense/Saves/progress.json"; fi
if [ -n "${SHOTS:-}" ]; then mkdir -p "$SHOTS"; cp Assets/Temp/${prefix}_*.png "$SHOTS/"; fi
echo "=== END"
