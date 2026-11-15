#!/usr/bin/env bash
# Round 66: r65_shots.sh with the hero's gear. GEAR="Hero_Armour=4 Hero_RepairDrone=2 Hero_Damage=4" sets those upgrade
# levels in memory before the level loads; "hero:<r66_hero.cs line>" hurts or probes him; a shot's focus may be "hero".
# Round 65: stills of a level from the play camera. Loads campaign node $2 on difficulty $3 from Boot (story on, briefing
# deployed, dialogue skipped; NODEPLOY=1 leaves the briefing up; STORY=1 starts the level as the map does, with its
# story, on the difficulty of start_level_briefing.cs) and runs the actions in order:
#   act:<r65_act.cs line>      "act:tower Laser 12 2 2", "act:select 12 2", "act:hand Cannon 13 3", "act:spawn Skitter 0 6", "act:probe"
#   eval:<file.cs>             any other probe in Tools/PlayTest
#   arg:<file>=<text>          writes an argument file in Tools/PlayTest first (e.g. "arg:r35_tip_arg.txt=setup")
#   step:<frames>              frames of 1/30 s
#   skip:<n>                   start the next n waves
#   dialogue                   skip a conversation on screen
#   fly:<n>                    n times 60 frames, every living flyer's place, facing and roll into Logs/r66/fly_trace.csv
#   scale:<x>                  the game's time scale
#   shot:<name>|<focus> <zoom> <hud|clean> [w h]   r66_shot.cs -> $OUT/<prefix>_<name>.png (focus cell:<x>:<y> or keep)
#   view:<name>                the Game view itself (1280x720, the interface exactly as shown)
# Copies the save aside, restores it and checks it against Tools/PlayTest/r64_save_md5.txt.
# Usage: OUT=<dir> bash Tools/PlayTest/r65_shots.sh <prefix> <node> <difficulty> <action> ...
set -u
cd "$(dirname "$0")/../.." || exit 1
prefix=$1; node=$2; difficulty=$3; shift 3
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
run() { unity command eval_file --file "Tools/PlayTest/$1" --result-only 2>&1 | grep -o '"result": "[^"]*\|"message": "[^"]*' | cut -c1-900; }
for i in 1 2 3; do step 60; sleep 1; done
if [ -n "${GEAR:-}" ]; then echo "$GEAR" > Tools/PlayTest/r66_gear_arg.txt; echo "$(run r66_gear.cs)"; fi
if [ "${STORY:-0}" = 1 ]; then echo "$node" > Tools/PlayTest/level_arg.txt; run start_level_briefing.cs; else echo "$node $difficulty" > Tools/PlayTest/r37_load_arg.txt; run r37_load.cs; fi
sleep 8
step 30
run r17_fixed_step.cs
if [ "${NODEPLOY:-0}" != 1 ]; then
  echo "deploy: $(run deploy.cs)"
  for i in $(seq 1 10); do step 60; done
  echo "dialogue: $(run r16_dialogue_skip.cs)"
  step 60
  echo "dialogue: $(run r16_dialogue_skip.cs)"
fi
for action in "$@"; do
  case "$action" in
    act:*) echo "${action#act:}" > Tools/PlayTest/r65_act_arg.txt; echo "act ${action#act:}: $(run r65_act.cs)" ;;
    hero:*) echo "${action#hero:}" > Tools/PlayTest/r66_hero_arg.txt; echo "hero ${action#hero:}: $(run r66_hero.cs)" ;;
    eval:*) echo "${action#eval:}: $(run "${action#eval:}")" ;;
    arg:*) spec="${action#arg:}"; echo "${spec#*=}" > "Tools/PlayTest/${spec%%=*}" ;;
    skip:*) for k in $(seq 1 "${action#skip:}"); do run skip_wave.cs >/dev/null; step 4; done; echo "skipped ${action#skip:}" ;;
    step:*) left=${action#step:}; while [ "$left" -gt 0 ]; do n=$((left > 60 ? 60 : left)); step "$n"; left=$((left - n)); done ;;
    dialogue) echo "dialogue: $(run r16_dialogue_skip.cs)" ;;
    fly:*) echo 60 > Tools/PlayTest/r65_fly_arg.txt; for k in $(seq 1 "${action#fly:}"); do last=$(run r65_fly_trace.cs); done; echo "fly trace: $last" ;;
    scale:*) unity command set_timescale --scale "${action#scale:}" --result-only > /dev/null 2>&1 ;;
    view:*) unity command capture_game_view --save_path "Temp/${action#view:}.png" --result-only > /dev/null 2>&1
      if [ -f "Assets/Temp/${action#view:}.png" ]; then cp "Assets/Temp/${action#view:}.png" "$out/${prefix}_view_${action#view:}.png"; echo "view: ${prefix}_view_${action#view:}.png"; else echo "view: no picture"; fi
      unity command delete_asset --asset "Assets/Temp/${action#view:}.png" --confirm true --result-only > /dev/null 2>&1 ;;
    shot:*) spec="${action#shot:}"; name="${spec%%|*}"; rest="${spec#*|}"; set -- $rest
      { echo "$1 $2 $3 ${4:-} ${5:-}"; cygpath -m "$out/${prefix}_${name}.png"; } > Tools/PlayTest/r66_shot_arg.txt; echo "$name: $(run r66_shot.cs)" ;;
  esac
done
echo "console: $(unity command console_status --result-only 2>&1 | grep -E 'consoleErrors|consoleWarnings' | tr -d '\n')"
unity command console --result-only 2>&1 | grep -o '"logType": "[A-Za-z]*", *"message": "[^"]*' | grep -v "allocation\|\"Log\"" | cut -c1-300 | sort | uniq -c | sort -rn | head -8
unity command editor_stop --result-only >/dev/null 2>&1
sleep 4
cp "$backup" "$save"; now=$(md5sum < "$save" | cut -d' ' -f1); echo "save md5 after restore: $now $([ "$now" = "$want" ] && echo OK || echo MISMATCH)"
