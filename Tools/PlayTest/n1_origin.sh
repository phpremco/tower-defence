#!/usr/bin/env bash
# Night 1, item 9: an origin marked at wave 0, its first opening in a nest wave, and the same origin still open (idling)
# long after that wave's last creature. Usage: n1_origin.sh <prefix> <level index> <wave> <seconds before the opening>
set -u
cd "$(dirname "$0")/../.." || exit 1
prefix=$1; level=$2; wave=$3; before=$4; half=3.2
status=$(unity command editor_status --result-only 2>&1)
if ! echo "$status" | grep -q '"playMode": *"stopped"'; then echo "NOT STOPPED - abort"; exit 1; fi
run() { unity command eval_file --file "Tools/PlayTest/$1" --result-only 2>&1 | grep -o '"result": "[^"]*' | cut -c12-900; }
liq() { echo "$*" > Tools/PlayTest/liq_phase.txt; echo "$(run liq_phase.cs)" | grep -o "Origin[^;]*;\|wave [0-9]* countdown.*\|^[a-z].\{0,120\}"; }
shot() { unity command capture_game_view --save_path "Temp/${prefix}_$1.png" --result-only >/dev/null 2>&1; }
unity command clear_console --result-only >/dev/null 2>&1
unity command open_scene --path Assets/_Project/Scenes/Boot.unity --result-only >/dev/null 2>&1
unity command editor_play --result-only 2>&1 | grep -o "Entered[^\"]*"
sleep 8
echo "$level" > Tools/PlayTest/level_arg.txt; run start_level.cs; sleep 10
liq waves off
liq view origin "$half" 0; shot marked
liq wave "$wave"
liq simulate "$before"
liq stepto busy Open 0.5; shot open_first
liq stepto busy Open 6
echo "-- after the creatures:"; liq probe
liq simulate 80
echo "-- long after the wave:"; liq probe
liq view origin "$half" busy; shot open_idle_later
echo "=== CONSOLE"
unity command console_status --result-only 2>&1 | grep -o '"consoleErrors": [0-9]*\|"consoleWarnings": [0-9]*'
unity command editor_stop --result-only >/dev/null 2>&1
if [ -n "${SAVE_BACKUP:-}" ]; then cp "$SAVE_BACKUP" "$USERPROFILE/AppData/LocalLow/DefaultCompany/Tower defense/Saves/progress.json"; fi
if [ -n "${SHOTS:-}" ]; then mkdir -p "$SHOTS"; cp Assets/Temp/${prefix}_*.png "$SHOTS/"; fi
echo "=== END"
