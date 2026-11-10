#!/usr/bin/env bash
# Round 35: Voss's gate tips in play. Plays C1_02 (campaign index in $1, default 1) from Boot with the story on, sets up a
# hurt gate with money for its upgrade, then a hurt second gate while Spencer's gate is whole, and captures each tip.
# Restores Remco's save afterwards. Usage: bash Tools/PlayTest/r35_tips.sh [level index]
set -u
cd "$(dirname "$0")/../.." || exit 1
source Tools/PlayTest/qa/lib.sh
LIVE="C:/Users/remco/AppData/Local/Temp/claude/b18d9d7fa25a4d30/2aa46f30-11b3-418c-a974-fe154b605af3/scratchpad/save/progress.json"
stopped || { echo "NOT STOPPED - abort"; exit 1; }
echo "save before: $(saveok)"
tip() { echo "$1" > Tools/PlayTest/r35_tip_arg.txt; ev r35_tip.cs; }
unity command clear_console --result-only >/dev/null 2>&1
unity command open_scene --path Assets/_Project/Scenes/Boot.unity --result-only >/dev/null 2>&1
unity command editor_play --result-only 2>&1 | grep -o "Entered[^\"]*"
sleep 8
steps 20
echo "${1:-1}" > Tools/PlayTest/level_arg.txt
ev start_level_briefing.cs
sleep 6
steps 30
tip deploy
steps 30
tip setup
steps 30
tip state; ev qa/dlg_state.cs
tip upgrade
steps 90
tip state; ev qa/dlg_state.cs
shot r35_tip_upgrade_chakra
tip skip
steps 30
tip move
steps 90
tip state; ev qa/dlg_state.cs
shot r35_tip_move_chakra
tip skip
steps 20
tip state; ev qa/dlg_state.cs
errs
unity command editor_stop --result-only >/dev/null 2>&1
sleep 3
restore
