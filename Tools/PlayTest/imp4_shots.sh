#!/usr/bin/env bash
# Import stage 4a screenshots of the level environment: plays a level with the waves off (imp4_phase.cs), a few towers
# by the road and creatures spread along it, and captures the overview, the play zoom and close-ups of the road edge,
# a plaza, a liquid bank, a nest, the map border and the base into Assets/Temp/<prefix>_L<level>_*.png (copied to
# $SHOTS when set). Usage: imp4_shots.sh <prefix> <level> [level ...]. Restores the save from $SAVE_BACKUP when set;
# LOW=1 shoots the Low (Switch) profile (low_profile.cs before, high_profile.cs after).
set -u
cd "$(dirname "$0")/../.." || exit 1
prefix=${1:-imp4}; shift
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
if [ "${LOW:-0}" = "1" ]; then run low_profile.cs; fi
for level in "$@"; do
  phase play >/dev/null
  echo "$level" > Tools/PlayTest/level_arg.txt; run start_level.cs; sleep 10
  phase setup
  phase view overview 12; live 3; shot overview
  phase view road 7; live 0.5; shot play
  phase view road 3; live 0.3; shot road
  phase place; phase view road 7; live 0.6; shot placing; phase view road 3; live 0.2; shot placing_close; phase place off; live 0.6
  phase view plaza 3; live 0.3; shot plaza
  phase view bank 3; live 0.3; shot bank
  phase view nest 3; live 0.3; shot nest
  phase view border 4; live 0.3; shot border
  phase view base 4; live 0.3; shot base
  phase view gate 3; live 0.3; shot gate
  phase view back 5; live 0.3; shot back
  phase view corner 4; live 0.3; shot corner
done
echo "=== CONSOLE"
unity command console_status --result-only 2>&1 | grep -o '"consoleErrors": [0-9]*\|"consoleWarnings": [0-9]*'
unity command console --result-only 2>&1 | grep -o '"logType": "[A-Za-z]*", *"message": "[^"]*' | grep -v "allocation older" | cut -c1-300 | head -12
unity command editor_stop --result-only >/dev/null 2>&1
if [ "${LOW:-0}" = "1" ]; then sleep 3; run high_profile.cs; fi
if [ -n "${SAVE_BACKUP:-}" ]; then cp "$SAVE_BACKUP" "$USERPROFILE/AppData/LocalLow/DefaultCompany/Tower defense/Saves/progress.json"; fi
if [ -n "${SHOTS:-}" ]; then mkdir -p "$SHOTS"; cp Assets/Temp/${prefix}_L*.png "$SHOTS/"; fi
echo "=== END"
