#!/usr/bin/env bash
# Round 35 font trial: Voss's dialogue box at 1280x720 and 1920x1080 in the default TMP face (Liberation Sans) and in
# Chakra Petch Medium and SemiBold (made at runtime, nothing saved), on C1_02 with the gate-upgrade tip on screen.
# Restores Remco's save afterwards. Usage: bash Tools/PlayTest/r35_font.sh
set -u
cd "$(dirname "$0")/../.." || exit 1
source Tools/PlayTest/qa/lib.sh
LIVE="C:/Users/remco/AppData/Local/Temp/claude/b18d9d7fa25a4d30/2aa46f30-11b3-418c-a974-fe154b605af3/scratchpad/save/progress.json"
stopped || { echo "NOT STOPPED - abort"; exit 1; }
tip() { echo "$1" > Tools/PlayTest/r35_tip_arg.txt; ev r35_tip.cs; }
font() { echo "$1" > Tools/PlayTest/r35_tmp_font_arg.txt; ev r35_tmp_font.cs; steps 4; }
unity command clear_console --result-only >/dev/null 2>&1
unity command open_scene --path Assets/_Project/Scenes/Boot.unity --result-only >/dev/null 2>&1
unity command editor_play --result-only 2>&1 | grep -o "Entered[^\"]*"
sleep 8
steps 20
echo 1 > Tools/PlayTest/level_arg.txt
ev start_level_briefing.cs
sleep 6
steps 30
tip deploy
steps 30
tip setup
steps 30
tip upgrade
steps 240
tip state
still r35_font_liberation_720 1280 720; still r35_font_liberation_1080 1920 1080
font Medium
still r35_font_chakra_medium_720 1280 720; still r35_font_chakra_medium_1080 1920 1080
font SemiBold
still r35_font_chakra_semibold_720 1280 720
errs
unity command editor_stop --result-only >/dev/null 2>&1
sleep 3
restore
