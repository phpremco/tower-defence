#!/usr/bin/env bash
# Import stage 2 screenshots: plays a level, pauses it, lines up every tower at level 1 and level 3 beside the road with
# creatures walking past (imp2_phase.cs), and captures the lineup at the play zoom and close up, the damage smoke and a
# wreck into Assets/Temp/<prefix>_*.png (copied to $SHOTS when set). The level is held between phases and runs only for
# set real seconds before each capture, so slow CLI round trips never let it run on. Usage: imp2_shots.sh <prefix> [level] [road index].
# Restores the save from $SAVE_BACKUP when set.
set -u
cd "$(dirname "$0")/../.." || exit 1
prefix=${1:-imp2}; level=${2:-0}; start=${3:-0}
status=$(unity command editor_status --result-only 2>&1)
if ! echo "$status" | grep -q '"playMode": *"stopped"'; then echo "NOT STOPPED - abort"; exit 1; fi
unity command clear_console --result-only >/dev/null 2>&1
unity command open_scene --path Assets/_Project/Scenes/Boot.unity --result-only >/dev/null 2>&1
unity command editor_play --result-only 2>&1 | grep -o "Entered[^\"]*"
sleep 8
run() { unity command eval_file --file "Tools/PlayTest/$1" --result-only 2>&1 | grep -o '"result": "[^"]*' | cut -c12-400; }
phase() { echo "$*" > Tools/PlayTest/imp2_phase.txt; echo "$(run imp2_phase.cs)"; }
shot() { unity command capture_game_view --save_path "Temp/${prefix}_$1.png" --result-only >/dev/null 2>&1; }
# Lets the level run for a number of real seconds, then holds it for the capture.
live() { phase play >/dev/null; sleep "$1"; phase pause >/dev/null; }
echo "$level" > Tools/PlayTest/level_arg.txt; run start_level.cs; sleep 10
phase setup "$start"
phase view lineup 7; live 6; shot play_a; live 0.3; shot play_b
phase view lineup 2.6; live 2; shot close_a; live 0.25; shot close_b
phase view lineup 4.2; live 2; shot mid
phase hurt 0.3; phase view lineup 2.6; live 3; shot smoke
phase wreck PulseRifle_L1; phase wreck Cannon_L3; phase view wreck 1.8; live 3; shot wreck
echo "=== CONSOLE"
unity command console_status --result-only 2>&1 | grep -o '"consoleErrors": [0-9]*\|"consoleWarnings": [0-9]*'
unity command editor_stop --result-only >/dev/null 2>&1
if [ -n "${SAVE_BACKUP:-}" ]; then cp "$SAVE_BACKUP" "$USERPROFILE/AppData/LocalLow/DefaultCompany/Tower defense/Saves/progress.json"; fi
if [ -n "${SHOTS:-}" ]; then cp Assets/Temp/${prefix}_*.png "$SHOTS/"; fi
echo "=== END"
