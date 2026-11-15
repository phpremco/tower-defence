#!/usr/bin/env bash
# Round 66: stills of the upgrade screen. Plays from Boot, jumps to the base with <points> research and power added (unsaved
# until a purchase; the save is copied aside and restored anyway) and runs the actions in order:
#   GEAR="Hero_Armour=4 ..."   upgrade levels set in memory first (r66_gear.cs)
#   tip:<state|next|skip>    Dr. Voss's first-visit explanation (r66_tip.cs)
#   buy:<row> [times]        presses an upgrade row of the chosen tile (r66_buy.cs)
#   select:<tile name>       r66_select.cs ("Spencer", "Base", a tower's name)
#   eval:<file.cs>           any probe in Tools/PlayTest
#   arg:<file>=<text>        writes an argument file in Tools/PlayTest first
#   step:<frames>            frames of 1/30 s
#   shot:<name>              the screen with its interface off-screen at 1920x1080 and 1280x720 (r64_shot.cs) ->
#                            $OUT/<prefix>_<name>_1080.png and _720.png
#   view:<name>              the Game view itself
# Usage: OUT=<dir> bash Tools/PlayTest/r66_base.sh <prefix> <points> <action> ...
set -u
cd "$(dirname "$0")/../.." || exit 1
prefix=$1; points=$2; shift 2
out=${OUT:-Logs/r66}
mkdir -p "$out" Logs/r66
save="$LOCALAPPDATA/../LocalLow/DefaultCompany/Tower defense/Saves/progress.json"
backup="Logs/r66/${prefix}_progress_before.json"
want=$(tr -d '\r\n ' < Tools/PlayTest/r64_save_md5.txt)
cp "$save" "$backup"; echo "save md5 before: $(md5sum < "$save" | cut -d' ' -f1)"
if ! unity command editor_status --result-only 2>&1 | grep -q '"playMode": *"stopped"'; then echo "NOT STOPPED - abort"; exit 1; fi
unity command clear_console --result-only >/dev/null 2>&1
unity command open_scene --path Assets/_Project/Scenes/Boot.unity --result-only >/dev/null 2>&1
unity command editor_play --result-only 2>&1 | grep -o "Entered[^\"]*"
sleep 8
step() { echo "$1 hold" > Tools/PlayTest/frames_arg.txt; unity command eval_file --file Tools/PlayTest/step_frames.cs --result-only >/dev/null 2>&1; }
run() { unity command eval_file --file "Tools/PlayTest/$1" --result-only 2>&1 | grep -o '"result": "[^"]*\|"message": "[^"]*' | cut -c1-1500; }
for i in 1 2 3; do step 60; sleep 1; done
if [ -n "${GEAR:-}" ]; then echo "$GEAR" > Tools/PlayTest/r66_gear_arg.txt; echo "$(run r66_gear.cs)"; fi
echo "$points" > Tools/PlayTest/points_arg.txt
echo "base: $(run goto_base.cs)"
sleep 4
for i in 1 2; do step 60; sleep 1; done
for action in "$@"; do
  case "$action" in
    select:*) echo "${action#select:}" > Tools/PlayTest/r66_select_arg.txt; echo "select: $(run r66_select.cs)"; step 20 ;;
    tip:*) echo "${action#tip:}" > Tools/PlayTest/r66_tip_arg.txt; echo "tip $(run r66_tip.cs)"; step 10 ;;
    buy:*) echo "${action#buy:}" > Tools/PlayTest/r66_buy_arg.txt; echo "buy ${action#buy:}: $(run r66_buy.cs)"; step 20 ;;
    eval:*) echo "${action#eval:}: $(run "${action#eval:}")" ;;
    arg:*) spec="${action#arg:}"; echo "${spec#*=}" > "Tools/PlayTest/${spec%%=*}" ;;
    step:*) left=${action#step:}; while [ "$left" -gt 0 ]; do n=$((left > 60 ? 60 : left)); step "$n"; left=$((left - n)); done ;;
    view:*) unity command capture_game_view --save_path "Temp/${action#view:}.png" --result-only > /dev/null 2>&1
      if [ -f "Assets/Temp/${action#view:}.png" ]; then cp "Assets/Temp/${action#view:}.png" "$out/${prefix}_view_${action#view:}.png"; echo "view: ${prefix}_view_${action#view:}.png"; else echo "view: no picture"; fi
      unity command delete_asset --asset "Assets/Temp/${action#view:}.png" --confirm true --result-only > /dev/null 2>&1 ;;
    shot:*) name="${action#shot:}"
      echo "1920 1080 $(cygpath -m "$out/${prefix}_${name}_1080.png")" > Tools/PlayTest/r64_shot_arg.txt; echo "$name 1080: $(run r64_shot.cs)"
      echo "1280 720 $(cygpath -m "$out/${prefix}_${name}_720.png")" > Tools/PlayTest/r64_shot_arg.txt; echo "$name 720: $(run r64_shot.cs)" ;;
  esac
done
echo "console: $(unity command console_status --result-only 2>&1 | grep -E 'consoleErrors|consoleWarnings' | tr -d '\n')"
unity command console --result-only 2>&1 | grep -o '"logType": "[A-Za-z]*", *"message": "[^"]*' | grep -v "allocation\|\"Log\"" | cut -c1-300 | sort | uniq -c | sort -rn | head -8
unity command editor_stop --result-only >/dev/null 2>&1
sleep 4
cp "$backup" "$save"; now=$(md5sum < "$save" | cut -d' ' -f1); echo "save md5 after restore: $now $([ "$now" = "$want" ] && echo OK || echo MISMATCH)"
