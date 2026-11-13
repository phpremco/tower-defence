#!/usr/bin/env bash
# Round 60: Shard Ridge (campaign index 2, the first level that sells the rocket, Remco's "spawn inside a spawn") after the
# fix: stills of its route gates and nest (r60_view.cs, r49_still.cs), then the drone cards with the HUD (render_still.cs):
# idle, the Repair card holding the gamepad focus, and the Harvest card under a real simulated mouse (r60_drone_card.cs
# prints what each card and the description box show). Copies the save aside first, restores and verifies it at the end.
# Usage: bash Tools/PlayTest/r60_hud.sh <output dir> <prefix>
set -u
cd "$(dirname "$0")/../.." || exit 1
out=$1; prefix=$2
mkdir -p "$out"
save="/c/Users/remco/AppData/LocalLow/DefaultCompany/Tower defense/Saves/progress.json"
backup="$out/${prefix}_progress_before.json"
status=$(unity command editor_status --result-only 2>&1)
if ! echo "$status" | grep -q '"playMode": *"stopped"'; then echo "NOT STOPPED - abort"; exit 1; fi
cp "$save" "$backup"; echo "save before $(md5sum "$save" | cut -c1-32)"
unity command clear_console --result-only >/dev/null 2>&1
unity command open_scene --path Assets/_Project/Scenes/Boot.unity --result-only >/dev/null 2>&1
unity command editor_play --result-only 2>&1 | grep -o "Entered[^\"]*"
sleep 8
step() { echo "$*" > Tools/PlayTest/frames_arg.txt; unity command eval_file --file Tools/PlayTest/step_frames.cs --result-only >/dev/null 2>&1; }
run() { unity command eval_file --file "Tools/PlayTest/$1" --result-only 2>&1 | grep -o '"result": "[^"]*\|"message": "[^"]*' | cut -c1-2400; }
lit() { echo "$(cygpath -w "$out/${prefix}_$1.png")" > Tools/PlayTest/look_arg.txt; echo "  $1: $(run r49_still.cs)"; }
hud() { echo "$(cygpath -w "$out/${prefix}_$1.png")" > Tools/PlayTest/look_arg.txt; echo "  $1: $(run render_still.cs)"; }
card() { echo "$*" > Tools/PlayTest/r60_card_arg.txt; run r60_drone_card.cs; }
echo "-" > Tools/PlayTest/r45_face.txt
echo "2" > Tools/PlayTest/level_arg.txt
echo "level: $(run start_level.cs)"; sleep 8; step 90 hold; run r16_dialogue_skip.cs >/dev/null; step 30 hold; run r16_dialogue_skip.cs >/dev/null; step 10 hold
for spec in "SpawnGate_3 3.2" "SpawnGate_0 3.2" "Origin_0 3.2"; do
  echo "$spec 0" > Tools/PlayTest/r60_view.txt
  echo "  $(run r60_view.cs)"; step 3 hold
  name=${spec%% *}
  echo "1" > Tools/PlayTest/r49_light.txt; lit "nest_${name}_game"
  echo "1.8" > Tools/PlayTest/r49_light.txt; lit "nest_${name}_lit"
done
echo "1" > Tools/PlayTest/r49_light.txt
echo "SpawnGate_0 1.6 0" > Tools/PlayTest/r60_view.txt; run r60_view.cs >/dev/null; step 3 hold
echo "  idle: $(card state)"
# The mouse first, while the interface is still an overlay (render_still.cs turns it into camera space for good).
xy=$(card pos 1 | sed "s/^\"result\": \"//"); echo "  harvest card at $xy"
# The event system notices the pointer only on frames that run free (a held pause skips its update): move twice, step free.
for i in 1 2; do unity command simulate_pointer --x ${xy% *} --y ${xy#* } --action move --result-only >/dev/null 2>&1; step 2; done
step 3 hold
echo "  hover: $(card state)"; hud drone_hud_hover_harvest
unity command simulate_pointer --x 5 --y 5 --action move --result-only >/dev/null 2>&1; step 3 hold
echo "  mouse away: $(card state)"; hud drone_hud_idle
echo "  $(card select 0)"; step 3 hold
echo "  focus: $(card state)"; hud drone_hud_focus_repair
echo "=== CONSOLE"
unity command console_status --result-only 2>&1 | grep -E "consoleErrors|consoleWarnings"
unity command console --result-only 2>&1 | grep -o '"logType": "[A-Za-z]*", *"message": "[^"]*' | grep -v "allocation" | sort | uniq -c | sort -rn | head -8
unity command editor_stop --result-only >/dev/null 2>&1
sleep 3
cp "$backup" "$save"; echo "save after restore $(md5sum "$save" | cut -c1-32)"
