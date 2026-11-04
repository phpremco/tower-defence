#!/usr/bin/env bash
# Import stage 3b hero screenshots: plays a level with the waves off (imp3b_phase.cs) and captures Mercer idle, running,
# shooting, slashing, using his abilities, down and getting up, at play zoom and close, into Assets/Temp/<prefix>_*.png
# (copied to $SHOTS when set). Usage: imp3b_hero.sh <prefix> [level]. Restores the save from $SAVE_BACKUP when set.
set -u
cd "$(dirname "$0")/../.." || exit 1
prefix=${1:-imp3b}; level=${2:-0}
status=$(unity command editor_status --result-only 2>&1)
if ! echo "$status" | grep -q '"playMode": *"stopped"'; then echo "NOT STOPPED - abort"; exit 1; fi
unity command clear_console --result-only >/dev/null 2>&1
unity command open_scene --path Assets/_Project/Scenes/Boot.unity --result-only >/dev/null 2>&1
unity command editor_play --result-only 2>&1 | grep -o "Entered[^\"]*"
sleep 8
run() { unity command eval_file --file "Tools/PlayTest/$1" --result-only 2>&1 | grep -o '"result": "[^"]*' | cut -c12-400; }
phase() { echo "$*" > Tools/PlayTest/imp3_phase.txt; echo "$(run imp3b_phase.cs)"; }
shot() { unity command capture_game_view --save_path "Temp/${prefix}_$1.png" --result-only >/dev/null 2>&1; }
live() { phase play >/dev/null; sleep "$1"; phase pause >/dev/null; }
echo "$level" > Tools/PlayTest/level_arg.txt; run start_level.cs; sleep 10
phase setup
phase view 6; live 1; shot idle_play
phase view 1.6; live 0.5; shot idle_close
phase move 1; live 0.6; phase view 1.8; shot run_close
live 4; phase move 0; live 6
phase view 1.8
phase foe Skitter 3.2; live 0.12; shot shoot_a; live 0.3; shot shoot_b; phase clear
phase foe Skitter 1.3; live 0.12; shot slash_a; live 0.15; shot slash_b; phase clear; live 4.5
phase foe Cragback 3.3; live 0.4; shot fight_close; phase view 6; shot fight_play; phase view 1.8; phase clear; live 4.5
phase ability 0; live 0.35; shot ability0_a; live 0.4; shot ability0_b
phase ability 1; live 0.6; shot ability1
phase view 1.8; phase down; live 0.5; shot down_fall; live 1.5; shot down_lying
phase revive; live 0.5; shot revive_a; live 1; shot revive_b
echo "=== CONSOLE"
unity command console_status --result-only 2>&1 | grep -o '"consoleErrors": [0-9]*\|"consoleWarnings": [0-9]*'
unity command console --result-only 2>&1 | grep -o '"logType": "[A-Za-z]*", *"message": "[^"]*' | grep -v "allocation older" | cut -c1-300 | head -12
unity command editor_stop --result-only >/dev/null 2>&1
if [ -n "${SAVE_BACKUP:-}" ]; then cp "$SAVE_BACKUP" "$USERPROFILE/AppData/LocalLow/DefaultCompany/Tower defense/Saves/progress.json"; fi
if [ -n "${SHOTS:-}" ]; then mkdir -p "$SHOTS"; cp Assets/Temp/${prefix}_*.png "$SHOTS/"; fi
echo "=== END"
