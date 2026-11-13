#!/usr/bin/env bash
# Round 60: a real simulated mouse over the drone cards of Shard Ridge (campaign index 2): moves the pointer onto card 1,
# steps, and prints what the event system and the cards see (r60_pointer_probe.cs, r60_drone_card.cs); renders the
# hovered HUD to <out>/<prefix>_drone_hud_hover_harvest.png. Copies the save aside first and restores it after.
set -u
cd "$(dirname "$0")/../.." || exit 1
out=$1; prefix=$2
save="/c/Users/remco/AppData/LocalLow/DefaultCompany/Tower defense/Saves/progress.json"
backup="$out/${prefix}_progress_before_hover.json"
status=$(unity command editor_status --result-only 2>&1)
if ! echo "$status" | grep -q '"playMode": *"stopped"'; then echo "NOT STOPPED - abort"; exit 1; fi
cp "$save" "$backup"; echo "save before $(md5sum "$save" | cut -c1-32)"
unity command clear_console --result-only >/dev/null 2>&1
unity command open_scene --path Assets/_Project/Scenes/Boot.unity --result-only >/dev/null 2>&1
unity command editor_play --result-only 2>&1 | grep -o "Entered[^\"]*"
sleep 8
step() { echo "$*" > Tools/PlayTest/frames_arg.txt; unity command eval_file --file Tools/PlayTest/step_frames.cs --result-only >/dev/null 2>&1; }
run() { unity command eval_file --file "Tools/PlayTest/$1" --result-only 2>&1 | grep -o '"result": "[^"]*\|"message": "[^"]*' | sed 's/^"result": "//' | cut -c1-2400; }
card() { echo "$*" > Tools/PlayTest/r60_card_arg.txt; run r60_drone_card.cs; }
echo "2" > Tools/PlayTest/level_arg.txt
echo "level: $(run start_level.cs)"; sleep 8; step 90 hold; run r16_dialogue_skip.cs >/dev/null; step 30 hold; run r16_dialogue_skip.cs >/dev/null; step 10 hold
xy=$(card pos 1); echo "harvest card at $xy"
for action in move move; do unity command simulate_pointer --x ${xy% *} --y ${xy#* } --action $action --result-only 2>&1 | grep -o '"Success": [a-z]*'; step 2; done
sleep 1; step 3 hold
echo "pointer: $(run r60_pointer_probe.cs)"
echo "cards: $(card state)"
echo "$(cygpath -w "$out/${prefix}_drone_hud_hover_harvest.png")" > Tools/PlayTest/look_arg.txt; echo "render: $(run render_still.cs)"
echo "=== CONSOLE"
unity command console_status --result-only 2>&1 | grep -E "consoleErrors|consoleWarnings"
unity command editor_stop --result-only >/dev/null 2>&1
sleep 3
cp "$backup" "$save"; echo "save after restore $(md5sum "$save" | cut -c1-32)"
