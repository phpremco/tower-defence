#!/usr/bin/env bash
# Spawn origin screenshots (liq_phase.cs): plays a level, frames a nest origin at wave 0 (marked), starts a wave that
# sends creatures out of a nest, runs its logic up to just before the opening, then steps real frames to the moments
# of the opening (tremor), the burst, the open maw and a creature coming out. Pictures go to Assets/Temp/<prefix>_*.png
# (copied to $SHOTS when set); the save is restored from $SAVE_BACKUP when set.
# Usage: origin_shots.sh <prefix> <level index> <wave (1-based)> <seconds into the wave just before the opening> [ortho half height]
set -u
cd "$(dirname "$0")/../.." || exit 1
prefix=${1:-origin}; level=${2:-0}; wave=${3:-8}; before=${4:-16}; half=${5:-3.2}
status=$(unity command editor_status --result-only 2>&1)
if ! echo "$status" | grep -q '"playMode": *"stopped"'; then echo "NOT STOPPED - abort"; exit 1; fi
run() { unity command eval_file --file "Tools/PlayTest/$1" --result-only 2>&1 | grep -o '"result": "[^"]*' | cut -c12-900; }
liq() { echo "$*" > Tools/PlayTest/liq_phase.txt; echo "$(run liq_phase.cs)"; }
shot() { unity command capture_game_view --save_path "Temp/${prefix}_$1.png" --result-only >/dev/null 2>&1; }
unity command clear_console --result-only >/dev/null 2>&1
unity command open_scene --path Assets/_Project/Scenes/Boot.unity --result-only >/dev/null 2>&1
unity command editor_play --result-only 2>&1 | grep -o "Entered[^\"]*"
sleep 8
echo "$level" > Tools/PlayTest/level_arg.txt; run start_level.cs; sleep 10
liq waves off
liq probe
liq view origin "$half" 0; shot marked_wave0
liq view origin 7 0; shot marked_field
liq view gate 2.6 0; shot gate_ground
liq view gate 2.6 1; shot gate_second
liq wave "$wave"
liq simulate "$before"
liq probe
liq stepto busy Opening 0.6
liq view origin "$half" busy; shot opening_tremor
liq stepto busy Opening 1.85; shot opening_burst
liq stepto busy Opening 2.2; shot opening_burst_b
liq stepto busy Open 0.3; shot open_maw
liq stepto busy Open 0.5; shot open_maw_b
liq stepto busy Open 1.2; shot mid_spawn
liq stepto busy Open 2.1; shot mid_spawn_c
liq probe
liq stepto busy Open 3; shot mid_spawn_b
echo "=== CONSOLE"
unity command console_status --result-only 2>&1 | grep -o '"consoleErrors": [0-9]*\|"consoleWarnings": [0-9]*'
unity command console --level error --tail 3 --result-only 2>&1 | grep -o '"message": "[^"]\{0,300\}'
unity command editor_stop --result-only >/dev/null 2>&1
if [ -n "${SAVE_BACKUP:-}" ]; then cp "$SAVE_BACKUP" "$USERPROFILE/AppData/LocalLow/DefaultCompany/Tower defense/Saves/progress.json"; fi
if [ -n "${SHOTS:-}" ]; then mkdir -p "$SHOTS"; cp Assets/Temp/${prefix}_*.png "$SHOTS/"; fi
echo "=== END"
