#!/usr/bin/env bash
# VFX round screenshots: plays a level, lines up every tower at level 1 and 3 by the road with creatures walking past
# (imp2_phase.cs) and captures the fight, then plays single impacts in slow motion (vfx_phase.cs burst) and a boss
# going down for real (spawned, walked to the lineup, killed: the chain explosion through EnemySpawner). Pictures go
# to Assets/Temp/<prefix>_*.png (copied to $SHOTS when set). Restores the save from $SAVE_BACKUP when set.
# Usage: vfx_shots.sh <prefix> [level] [road index] [boss asset]
set -u
cd "$(dirname "$0")/../.." || exit 1
prefix=${1:-vfx}; level=${2:-0}; start=${3:-0}; boss=${4:-CrystalWarlord}
status=$(unity command editor_status --result-only 2>&1)
if ! echo "$status" | grep -q '"playMode": *"stopped"'; then echo "NOT STOPPED - abort"; exit 1; fi
unity command clear_console --result-only >/dev/null 2>&1
unity command open_scene --path Assets/_Project/Scenes/Boot.unity --result-only >/dev/null 2>&1
unity command editor_play --result-only 2>&1 | grep -o "Entered[^\"]*"
sleep 8
run() { unity command eval_file --file "Tools/PlayTest/$1" --result-only 2>&1 | grep -o '"result": "[^"]*' | cut -c12-400; }
phase() { echo "$*" > Tools/PlayTest/imp2_phase.txt; echo "$(run imp2_phase.cs)"; }
vfx() { echo "$*" > Tools/PlayTest/vfx_phase.txt; echo "$(run vfx_phase.cs)"; }
shot() { unity command capture_game_view --save_path "Temp/${prefix}_$1.png" --result-only >/dev/null 2>&1; }
live() { phase play >/dev/null; sleep "$1"; phase pause >/dev/null; }
echo "$level" > Tools/PlayTest/level_arg.txt; run start_level.cs; sleep 10
phase setup "$start"
# The fight in slow motion, with a fresh group walked in to the middle of the lineup, so shots are in the air.
phase view lineup 4.5; vfx time 0.12 >/dev/null; vfx wave 24; live 0.8; shot fight_a; vfx probe; live 0.8; shot fight_b; live 0.8; shot fight_c
phase view lineup 2.8; vfx wave 24; live 0.8; shot close_a; vfx probe; live 0.8; shot close_b; live 0.8; shot close_c
vfx time 1 >/dev/null
# Single impacts in slow motion, so the frames between two CLI calls stay short.
phase view lineup 4
for spec in "Mortar 1.8" "Shell 1.2" "Rocket 1.1" "Barrel 2"; do
  name=${spec%% *}
  vfx time 0.08 >/dev/null
  vfx burst $spec 2 1.5
  live 0.3; shot ${name}_1; live 1.5; shot ${name}_2; live 4; shot ${name}_3
  vfx time 1 >/dev/null; live 3
done
vfx time 0.08 >/dev/null
vfx boss "$boss"
phase view lineup 4.5
live 0.3; shot boss_1; live 2.5; shot boss_2; live 4; shot boss_3; live 8; shot boss_4; live 14; shot boss_5
vfx time 1 >/dev/null
echo "=== CONSOLE"
unity command console_status --result-only 2>&1 | grep -o '"consoleErrors": [0-9]*\|"consoleWarnings": [0-9]*'
unity command editor_stop --result-only >/dev/null 2>&1
if [ -n "${SAVE_BACKUP:-}" ]; then cp "$SAVE_BACKUP" "$USERPROFILE/AppData/LocalLow/DefaultCompany/Tower defense/Saves/progress.json"; fi
if [ -n "${SHOTS:-}" ]; then cp Assets/Temp/${prefix}_*.png "$SHOTS/"; fi
echo "=== END"
