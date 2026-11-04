#!/usr/bin/env bash
# Round 25/25b check: plays a level from Boot and runs the listed actions in order, then stops and restores the save.
# Actions: probe:<file.cs> (prints its result), shot:<name> (Assets/Temp/<prefix>_<name>.png, copied to $SHOTS),
# step:<frames>, wave (skips to the next wave), place (selects the first tower card), unplace, fight (a wave with
# the creatures hurt a little so their bars show), view:<what> <size> (zoom.cs), menu (back to the main menu),
# sleep:<s>. Usage: round_ui_shots.sh <prefix> <level> action...
set -u
cd "$(dirname "$0")/../.." || exit 1
prefix=$1; level=$2; shift 2
status=$(unity command editor_status --result-only 2>&1)
if ! echo "$status" | grep -q '"playMode": *"stopped"'; then echo "NOT STOPPED - abort"; exit 1; fi
unity command clear_console --result-only >/dev/null 2>&1
unity command open_scene --path Assets/_Project/Scenes/Boot.unity --result-only >/dev/null 2>&1
unity command editor_play --result-only 2>&1 | grep -o "Entered[^\"]*"
sleep 8
step() { echo "$1" > Tools/PlayTest/frames_arg.txt; unity command eval_file --file Tools/PlayTest/step_frames.cs --result-only >/dev/null 2>&1; }
run() { unity command eval_file --file "Tools/PlayTest/$1" --result-only 2>&1 | grep -o '"result": "[^"]*' | cut -c12-1500; }
shot() { unity command capture_game_view --save_path "Temp/${prefix}_$1.png" --result-only >/dev/null 2>&1; }
step 60
run intro_skip.cs >/dev/null
if [ "$level" != "none" ]; then
  echo "$level" > Tools/PlayTest/level_arg.txt
  run start_level.cs
  sleep 6
  step 100; step 100
fi
for a in "$@"; do
  case "$a" in
    probe:*) echo "$(run "${a#probe:}")";;
    shot:*) shot "${a#shot:}";;
    step:*) step "${a#step:}";;
    sleep:*) sleep "${a#sleep:}";;
    wave) run skip_wave.cs >/dev/null;;
    place) echo 0 > Tools/PlayTest/card_arg.txt; run select_card.cs;;
    unplace) echo "place off" > Tools/PlayTest/imp4_phase.txt; run imp4_phase.cs >/dev/null;;
    fight) run fight_hurt.cs;;
    view:*) v=${a#view:}; echo "${v% *}" > Tools/PlayTest/zoom_arg.txt; echo "${v#* }" > Tools/PlayTest/zoom_size.txt; run zoom.cs >/dev/null; step 3;;
    menu) run goto_menu.cs >/dev/null; sleep 4; step 60;;
    click:*) echo "${a#click:}" > Tools/PlayTest/button_arg.txt; echo "$(run click_button.cs | cut -c1-200)"; step 30;;
    still:*) echo "${SHOTS:-Logs}/${prefix}_${a#still:}.png" > Tools/PlayTest/look_arg.txt; run render_still.cs >/dev/null;;
    run:*) echo "$(run "${a#run:}")";;
    arg:*) kv=${a#arg:}; echo "${kv#*=}" > "Tools/PlayTest/${kv%%=*}";;
    pointer:*) xy=$(cat Tools/PlayTest/n1_pick_xy.txt); unity command simulate_pointer --x ${xy% *} --y ${xy#* } --action ${a#pointer:} --result-only 2>&1 | head -c 300; echo;;
    *) echo "unknown action $a";;
  esac
done
echo "=== CONSOLE"
unity command console_status --result-only 2>&1 | grep -o '"consoleErrors": [0-9]*\|"consoleWarnings": [0-9]*'
unity command console --result-only 2>&1 | grep -o '"logType": "[A-Za-z]*", *"message": "[^"]*' | grep -v "allocation older\|logType\": \"Log" | cut -c1-300 | sort | uniq -c | head -10
unity command editor_stop --result-only >/dev/null 2>&1
if [ -n "${SAVE_BACKUP:-}" ]; then cp "$SAVE_BACKUP" "$USERPROFILE/AppData/LocalLow/DefaultCompany/Tower defense/Saves/progress.json"; fi
if [ -n "${SHOTS:-}" ]; then mkdir -p "$SHOTS"; cp Assets/Temp/${prefix}_*.png "$SHOTS/" 2>/dev/null; fi
echo "=== END"
