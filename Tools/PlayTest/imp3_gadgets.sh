#!/usr/bin/env bash
# Import stage 3a: the path objects alone, placed along the road from a road cell (imp3_phase.cs "gadgets"), captured
# close up, mid and after rolling. Usage: imp3_gadgets.sh <prefix> [level] [road cell index]. Restores $SAVE_BACKUP.
set -u
cd "$(dirname "$0")/../.." || exit 1
prefix=${1:-imp3g}; level=${2:-0}; start=${3:-20}
status=$(unity command editor_status --result-only 2>&1)
if ! echo "$status" | grep -q '"playMode": *"stopped"'; then echo "NOT STOPPED - abort"; exit 1; fi
unity command clear_console --result-only >/dev/null 2>&1
unity command open_scene --path Assets/_Project/Scenes/Boot.unity --result-only >/dev/null 2>&1
unity command editor_play --result-only 2>&1 | grep -o "Entered[^\"]*"
sleep 8
run() { unity command eval_file --file "Tools/PlayTest/$1" --result-only 2>&1 | grep -o '"result": "[^"]*' | cut -c12-400; }
phase() { echo "$*" > Tools/PlayTest/imp3_phase.txt; echo "$(run imp3_phase.cs)"; }
shot() { unity command capture_game_view --save_path "Temp/${prefix}_$1.png" --result-only >/dev/null 2>&1; }
live() { phase play >/dev/null; sleep "$1"; phase pause >/dev/null; }
echo "$level" > Tools/PlayTest/level_arg.txt; run start_level.cs; sleep 10
phase setup
phase gadgets "$start"
phase view gadgets 2.4; live 0.4; shot gadgets_deploy; live 1.2; shot gadgets_close
phase view gadgets 4.5; live 0.3; shot gadgets_mid
phase view gadgets 7; live 0.3; shot gadgets_play
phase gadgets "$start"; phase view gadgets 2.4; live 3; shot gadgets_rolled
echo "=== CONSOLE"
unity command console_status --result-only 2>&1 | grep -o '"consoleErrors": [0-9]*\|"consoleWarnings": [0-9]*'
unity command console --result-only 2>&1 | grep -o '"logType": "[A-Za-z]*", *"message": "[^"]*' | grep -v "allocation older" | cut -c1-300 | head -12
unity command editor_stop --result-only >/dev/null 2>&1
if [ -n "${SAVE_BACKUP:-}" ]; then cp "$SAVE_BACKUP" "$USERPROFILE/AppData/LocalLow/DefaultCompany/Tower defense/Saves/progress.json"; fi
if [ -n "${SHOTS:-}" ]; then mkdir -p "$SHOTS"; cp Assets/Temp/${prefix}_*.png "$SHOTS/"; fi
echo "=== END"
