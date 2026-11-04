#!/usr/bin/env bash
# Import stage 3a screenshots: plays a level with the waves off (imp3_phase.cs) and captures the Blender gate, base,
# drones, path objects and resource node at the play zoom and close up, damaged, wrecked and rebuilt, into
# Assets/Temp/<prefix>_*.png (copied to $SHOTS when set). Usage: imp3_shots.sh <prefix> [level] [road index].
# Restores the save from $SAVE_BACKUP when set.
set -u
cd "$(dirname "$0")/../.." || exit 1
prefix=${1:-imp3}; level=${2:-0}; start=${3:-6}
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
phase view base 7; live 4; shot base_play
phase view base 2.6; live 0.3; shot base_close
phase view gate 7; live 0.3; shot gate_play
phase view gate 2.6; live 0.3; shot gate_close
phase gadgets "$start"
phase view gadgets 2.6; live 1.2; shot gadgets_close
phase view gadgets 4.5; live 0.3; shot gadgets_mid
phase view mine 2.2; live 0.3; shot mine_full
phase mine halve; live 0.3; shot mine_half
phase mine empty; live 0.3; shot mine_empty
phase gate 0.3; phase view gate 2.6; live 4; shot gate_hurt
phase view drone 2.6; live 0.3; shot drone_work
phase gate wreck; phase view gate 2.6; live 0.5; shot gate_collapse; live 2; shot gate_wreck
phase gate rebuild; live 0.6; shot gate_build; phase gate upgrade; live 1.5; shot gate_g3
phase showcase on; phase view showcase 3.2; live 0.3; shot drones_showcase; phase showcase off
phase base 0.3; phase view base 2.6; live 1; shot base_hurt
phase base 0; live 0.7; shot base_wreck
echo "=== CONSOLE"
unity command console_status --result-only 2>&1 | grep -o '"consoleErrors": [0-9]*\|"consoleWarnings": [0-9]*'
unity command console --result-only 2>&1 | grep -o '"logType": "[A-Za-z]*", *"message": "[^"]*' | grep -v "allocation older" | cut -c1-300 | head -12
unity command editor_stop --result-only >/dev/null 2>&1
if [ -n "${SAVE_BACKUP:-}" ]; then cp "$SAVE_BACKUP" "$USERPROFILE/AppData/LocalLow/DefaultCompany/Tower defense/Saves/progress.json"; fi
if [ -n "${SHOTS:-}" ]; then mkdir -p "$SHOTS"; cp Assets/Temp/${prefix}_*.png "$SHOTS/"; fi
echo "=== END"
