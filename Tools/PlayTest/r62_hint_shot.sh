#!/usr/bin/env bash
# Round 62: stills of the tower bar and the tower menu with a tower's ground/air read (TargetRead). Loads campaign node $2 on
# Easy from Boot (the briefing deployed, the dialogue skipped), then for each action "hand:<TowerId>" or "menu:<TowerId>" sets
# the HUD up (r62_hint.cs) and renders 3840x2160 off-screen with the interface (r37_shot.cs, the Game view is left alone) into
# $OUT/<prefix>_<action>.png. "view:<name>" captures the Game view itself instead (after the action before it). Copies the save aside first, restores it and prints its MD5 at both ends.
# Usage: OUT=<dir> bash Tools/PlayTest/r62_hint_shot.sh <prefix> <node> <action> ...
set -u
cd "$(dirname "$0")/../.." || exit 1
prefix=$1; node=$2; shift 2
out=${OUT:-Logs/r62}
mkdir -p "$out"
save="$LOCALAPPDATA/../LocalLow/DefaultCompany/Tower defense/Saves/progress.json"
backup="$out/${prefix}_progress_before.json"
had=0; if [ -f "$save" ]; then had=1; cp "$save" "$backup"; echo "save md5 before: $(md5sum < "$save" | cut -d' ' -f1)"; else echo "no progress.json before"; fi
if ! unity command editor_status --result-only 2>&1 | grep -q '"playMode": *"stopped"'; then echo "NOT STOPPED - abort"; exit 1; fi
unity command clear_console --result-only >/dev/null 2>&1
unity command open_scene --path Assets/_Project/Scenes/Boot.unity --result-only >/dev/null 2>&1
unity command editor_play --result-only 2>&1 | grep -o "Entered[^\"]*"
sleep 8
step() { echo "$1 hold" > Tools/PlayTest/frames_arg.txt; unity command eval_file --file Tools/PlayTest/step_frames.cs --result-only >/dev/null 2>&1; }
run() { unity command eval_file --file "Tools/PlayTest/$1" --result-only 2>&1 | grep -o '"result": "[^"]*\|"message": "[^"]*' | cut -c1-600; }
for i in 1 2 3; do step 60; sleep 1; done
echo "$node 0" > Tools/PlayTest/r37_load_arg.txt; run r37_load.cs; sleep 8
step 30
run r17_fixed_step.cs
echo "deploy: $(run deploy.cs)"
for i in $(seq 1 10); do step 60; done
echo "dialogue: $(run r16_dialogue_skip.cs)"
step 60
echo "dialogue: $(run r16_dialogue_skip.cs)"
for action in "$@"; do
  if [ "${action%%:*}" = "view" ]; then
    # The Game view as it is (1280x720 whatever its size, so stretched): the screen-space interface exactly as the player sees it.
    unity command capture_game_view --save_path "Temp/${action#view:}.png" --result-only > /dev/null 2>&1
    if [ -f "Assets/Temp/${action#view:}.png" ]; then cp "Assets/Temp/${action#view:}.png" "$out/${prefix}_view_${action#view:}.png"; echo "view: $out/${prefix}_view_${action#view:}.png"; else echo "view: no picture"; fi
    unity command delete_asset --asset "Assets/Temp/${action#view:}.png" --confirm true --result-only > /dev/null 2>&1
    continue
  fi
  echo "${action/:/ }" > Tools/PlayTest/r62_hint_arg.txt
  echo "$action: $(run r62_hint.cs)"
  step 20
  echo "state" > Tools/PlayTest/r62_hint_arg.txt
  echo "  after 20 frames: $(run r62_hint.cs)"
  echo "keep 1 hud $(cygpath -w "$out/${prefix}_${action/:/_}.png")" > Tools/PlayTest/r37_shot_arg.txt
  echo "  $(run r37_shot.cs)"
done
echo "console: $(unity command console_status --result-only 2>&1 | grep -E 'consoleErrors|consoleWarnings' | tr -d '\n')"
unity command console --result-only 2>&1 | grep -o '"logType": "[A-Za-z]*", *"message": "[^"]*' | grep -v "allocation\|\"Log\"" | sort | uniq -c | sort -rn | head -8
unity command editor_stop --result-only >/dev/null 2>&1
sleep 3
if [ "$had" = 1 ]; then cp "$backup" "$save"; echo "save md5 after restore: $(md5sum < "$save" | cut -d' ' -f1)"; else rm -f "$save"; echo "progress.json removed"; fi
