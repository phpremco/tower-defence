#!/usr/bin/env bash
# Round 23a: the codex in a level. Loads campaign node $2 (default 1) on Medium, deploys, waits out the recon, stills the
# field, opens the codex (enemies page: the row count is the level's own roster), tries to zoom with the keyboard while it
# is open, closes it and stills the field again; then a third still with the display-stage lights leaking as before the
# fix, for comparison. Usage: r23a_codex.sh <output dir> [node]
set -u
cd "$(dirname "$0")/../.." || exit 1
out=$1; node=${2:-1}
status=$(unity command editor_status --result-only 2>&1)
if ! echo "$status" | grep -q '"playMode": *"stopped"'; then echo "NOT STOPPED - abort"; exit 1; fi
unity command clear_console --result-only >/dev/null 2>&1
unity command open_scene --path Assets/_Project/Scenes/Boot.unity --result-only >/dev/null 2>&1
unity command editor_play --result-only 2>&1 | grep -o "Entered[^\"]*"
sleep 8
step() { echo "$1" > Tools/PlayTest/frames_arg.txt; unity command eval_file --file Tools/PlayTest/step_frames.cs --result-only >/dev/null 2>&1; }
run() { unity command eval_file --file "Tools/PlayTest/$1" --result-only 2>&1 | grep -o '"result": "[^"]*\|"message": "[^"]*' | cut -c1-1500; }
step 60
echo "node $node" > Tools/PlayTest/r15b_load.txt; run r15b_load.cs; sleep 8
step 30
echo "deploy: $(run deploy.cs)"
for i in $(seq 1 12); do step 60; sleep 1; done
echo "dialogue: $(run r16_dialogue_skip.cs)"
echo "waves: $(run no_waves.cs)"
step 60
echo "before: $(run r15b_lights.cs)"
echo "cam before: $(run r23a_cam.cs)"
echo "$out/r23a_before.png" > Tools/PlayTest/look_arg.txt; run render_still.cs
echo enemies > Tools/PlayTest/menu_arg.txt; echo 0 > Tools/PlayTest/card_arg.txt
echo "open: $(run codex_open.cs)"
step 10
echo "open lights: $(run r15b_lights.cs)"
echo "$out/r23a_open.png" > Tools/PlayTest/look_arg.txt; run render_still.cs
# Holds a key for a moment of real play (the input system reads it on real frames), then lets go.
hold() { unity command simulate_key --key "$1" --action down --result-only 2>&1 | grep -o '"Detail": "[^"]*'; sleep 2; step 30; unity command simulate_key --key "$1" --action up --result-only >/dev/null 2>&1; step 2; }
echo "cam open: $(run r23a_cam.cs)"
hold minus; hold w
echo "cam after keys while open: $(run r23a_cam.cs)"
echo "close: $(run codex_close.cs)"
step 30
echo "after: $(run r15b_lights.cs)"
echo "cam after close: $(run r23a_cam.cs)"
echo "$out/r23a_after.png" > Tools/PlayTest/look_arg.txt; run render_still.cs
hold minus; hold w
echo "cam after keys when closed: $(run r23a_cam.cs)"
echo on > Tools/PlayTest/r23a_oldleak.txt; echo "leak: $(run r23a_oldleak.cs)"; step 5
echo "leak lights: $(run r15b_lights.cs)"
echo "$out/r23a_oldleak.png" > Tools/PlayTest/look_arg.txt; run render_still.cs
echo off > Tools/PlayTest/r23a_oldleak.txt; run r23a_oldleak.cs >/dev/null
printf "%s\n" "$out/r23a_before.png" "$out/r23a_after.png" "$out/r23a_oldleak.png" > Tools/PlayTest/r23a_lum.txt
echo "lum: $(run r23a_lum.cs)"
echo "=== CONSOLE"
unity command console --result-only 2>&1 | grep -o '"message": "[^"]*' | grep -v "allocation" | sort | uniq -c | sort -rn | head -8
unity command editor_stop --result-only >/dev/null 2>&1
