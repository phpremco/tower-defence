#!/usr/bin/env bash
# Import stage 4b: the Shardback (armour 1, 2/3, 1/3, 0: its prism clusters shed) and Mireleech in a level at play zoom
# and close. Usage: imp4b_creature_shots.sh <prefix> <level index>. $SAVE_BACKUP / $SHOTS as imp4_shots.sh.
set -u
cd "$(dirname "$0")/../.." || exit 1
prefix=${1:-cre}; level=${2:-0}
status=$(unity command editor_status --result-only 2>&1)
if ! echo "$status" | grep -q '"playMode": *"stopped"'; then echo "NOT STOPPED - abort"; exit 1; fi
unity command clear_console --result-only >/dev/null 2>&1
unity command open_scene --path Assets/_Project/Scenes/Boot.unity --result-only >/dev/null 2>&1
unity command editor_play --result-only 2>&1 | grep -o "Entered[^\"]*"
sleep 8
run() { unity command eval_file --file "Tools/PlayTest/$1" --result-only 2>&1 | grep -o '"result": "[^"]*' | cut -c12-400; }
phase() { echo "$*" > Tools/PlayTest/imp4_phase.txt; echo "$(run imp4_phase.cs)"; }
cre() { echo "$*" > Tools/PlayTest/imp4b_creatures.txt; echo "$(run imp4b_creatures.cs)"; }
shot() { unity command capture_game_view --save_path "Temp/${prefix}_$1.png" --result-only >/dev/null 2>&1; }
live() { phase play >/dev/null; sleep "$1"; phase pause >/dev/null; }
phase play >/dev/null
echo "$level" > Tools/PlayTest/level_arg.txt; run start_level.cs; sleep 10
phase setup
cre spawn
cre view 7; live 0.4; shot play
cre view 3; live 0.3; shot close
cre view 1.6; live 0.3; shot macro
echo "=== CONSOLE"
unity command console_status --result-only 2>&1 | grep -o '"consoleErrors": [0-9]*\|"consoleWarnings": [0-9]*'
unity command console --result-only 2>&1 | grep -o '"logType": "[A-Za-z]*", *"message": "[^"]*' | grep -v "allocation older" | cut -c1-300 | head -12
unity command editor_stop --result-only >/dev/null 2>&1
if [ -n "${SAVE_BACKUP:-}" ]; then cp "$SAVE_BACKUP" "$USERPROFILE/AppData/LocalLow/DefaultCompany/Tower defense/Saves/progress.json"; fi
if [ -n "${SHOTS:-}" ]; then mkdir -p "$SHOTS"; cp Assets/Temp/${prefix}_*.png "$SHOTS/"; fi
echo "=== END"
