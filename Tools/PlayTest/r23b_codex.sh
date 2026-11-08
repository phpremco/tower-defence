#!/usr/bin/env bash
# Round 23b: the display font's K and X. Loads campaign node $2 (default 1) on Medium, deploys, opens the codex on the
# enemies page at row $3 (default 0, Skitter on C1_02) and captures the Game view (with the overlay UI) to
# <output dir>/r23b_codex.png, plus the main menu with its Codex button to r23b_menu.png. Restore the save afterwards.
# Usage: r23b_codex.sh <output dir> [node] [row]
set -u
cd "$(dirname "$0")/../.." || exit 1
out=$1; node=${2:-1}; row=${3:-0}
status=$(unity command editor_status --result-only 2>&1)
if ! echo "$status" | grep -q '"playMode": *"stopped"'; then echo "NOT STOPPED - abort"; exit 1; fi
unity command clear_console --result-only >/dev/null 2>&1
unity command open_scene --path Assets/_Project/Scenes/Boot.unity --result-only >/dev/null 2>&1
unity command editor_play --result-only 2>&1 | grep -o "Entered[^\"]*"
sleep 8
step() { echo "$1" > Tools/PlayTest/frames_arg.txt; unity command eval_file --file Tools/PlayTest/step_frames.cs --result-only >/dev/null 2>&1; }
run() { unity command eval_file --file "Tools/PlayTest/$1" --result-only 2>&1 | grep -o '"result": "[^"]*\|"message": "[^"]*' | cut -c1-600; }
shot() { unity command capture_game_view --save_path "Temp/$1.png" --result-only >/dev/null 2>&1; cp "Assets/Temp/$1.png" "$out/"; }
step 60
step 120
shot r23b_menu
echo "node $node" > Tools/PlayTest/r15b_load.txt; run r15b_load.cs; sleep 8
step 30
echo "deploy: $(run deploy.cs)"
for i in $(seq 1 12); do step 60; sleep 1; done
echo "dialogue: $(run r16_dialogue_skip.cs)"
echo "waves: $(run no_waves.cs)"
step 60
echo enemies > Tools/PlayTest/menu_arg.txt; echo "$row" > Tools/PlayTest/card_arg.txt
echo "open: $(run codex_open.cs)"
step 20
shot r23b_codex
echo recipes > Tools/PlayTest/menu_arg.txt; echo 0 > Tools/PlayTest/card_arg.txt
echo "recipes: $(run codex_open.cs)"
step 20
shot r23b_codex_recipes
echo "=== CONSOLE"
unity command console_status --result-only 2>&1 | grep -o '"consoleErrors": [0-9]*\|"consoleWarnings": [0-9]*'
unity command console --result-only 2>&1 | grep -o '"message": "[^"]*' | grep -v "allocation" | sort | uniq -c | sort -rn | head -8
unity command editor_stop --result-only >/dev/null 2>&1
