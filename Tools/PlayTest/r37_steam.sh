#!/usr/bin/env bash
# Round 37: Steam gameplay screenshots in real play. Loads campaign node $2 on difficulty $3 (0 Easy .. 3 Psycho) from
# Boot with the story on, deploys the briefing, skips its dialogue, then runs the actions in order:
#   def:<from> <to> <towers> <seed> [air]   build an upgraded defence (r37_defence.cs)
#   skip:<n>        start the next n waves at once (stacks them, so the field fills)
#   step:<frames>   step that many frames at 1/30 s each
#   drones          launch one drone of every kind the level offers (buy_drones.cs)
#   dialogue        skip any conversation on screen
#   probe           counts of creatures, flyers, the boss
#   shot:<name>|<focus> <zoom> <hud|clean> [dx dz]   render 3840x2160 (r37_shot.cs) into $OUT/<name>_4k.png
# Keeps the save exactly as it was. Usage: OUT=<dir> r37_steam.sh <prefix> <node> <difficulty> <action> ...
set -u
cd "$(dirname "$0")/../.." || exit 1
prefix=$1; node=$2; difficulty=$3; shift 3
out=${OUT:-Logs}
save="$LOCALAPPDATA/../LocalLow/DefaultCompany/Tower defense/Saves/progress.json"
backup="$out/${prefix}_progress_before.json"
had=0; if [ -f "$save" ]; then had=1; cp "$save" "$backup"; md5sum "$save"; else echo "no progress.json before"; fi
status=$(unity command editor_status --result-only 2>&1)
if ! echo "$status" | grep -q '"playMode": *"stopped"'; then echo "NOT STOPPED - abort"; exit 1; fi
unity command clear_console --result-only >/dev/null 2>&1
unity command open_scene --path Assets/_Project/Scenes/Boot.unity --result-only >/dev/null 2>&1
unity command editor_play --result-only 2>&1 | grep -o "Entered[^\"]*"
sleep 8
step() { echo "$1 hold" > Tools/PlayTest/frames_arg.txt; unity command eval_file --file Tools/PlayTest/step_frames.cs --result-only >/dev/null 2>&1; }
run() { unity command eval_file --file "Tools/PlayTest/$1" --result-only 2>&1 | grep -o '"result": "[^"]*\|"message": "[^"]*' | cut -c1-400; }
for i in 1 2 3; do step 60; sleep 1; done
echo "$node $difficulty" > Tools/PlayTest/r37_load_arg.txt; run r37_load.cs; sleep 8
step 30
run r17_fixed_step.cs
echo "deploy: $(run deploy.cs)"
for i in $(seq 1 10); do step 60; done
echo "dialogue: $(run r16_dialogue_skip.cs)"
step 60
echo "dialogue: $(run r16_dialogue_skip.cs)"
for action in "$@"; do
  case "$action" in
    def:*) echo "${action#def:}" > Tools/PlayTest/r37_def_arg.txt; echo "defence: $(run r37_defence.cs)" ;;
    skip:*) for k in $(seq 1 "${action#skip:}"); do run skip_wave.cs >/dev/null; step 4; done; echo "skipped ${action#skip:}" ;;
    step:*) left=${action#step:}; while [ "$left" -gt 0 ]; do n=$((left > 120 ? 120 : left)); step "$n"; left=$((left - n)); run r16_dialogue_skip.cs >/dev/null; done ;;
    drones) echo "drones: $(run buy_drones.cs)" ;;
    dialogue) run r16_dialogue_skip.cs ;;
    revive) echo "revive: $(run hero_revive.cs)"; step 90 ;;
    money:*) echo "${action#money:}" > Tools/PlayTest/r37_money_arg.txt; echo "$(run r37_money.cs)" ;;
    probe) echo "probe: $(run r37_probe.cs)" ;;
    shot:*) spec="${action#shot:}"; name="${spec%%|*}"; rest="${spec#*|}"; set -- $rest
      echo "$1 $2 $3 $(cygpath -w "$out/${name}_4k.png") ${4:-} ${5:-}" > Tools/PlayTest/r37_shot_arg.txt; echo "$name: $(run r37_shot.cs)" ;;
  esac
done
echo "=== CONSOLE"
unity command console --result-only 2>&1 | grep -o '"logType": "[A-Za-z]*", *"message": "[^"]*' | grep -v "allocation" | sort | uniq -c | sort -rn | head -8
unity command editor_stop --result-only >/dev/null 2>&1
sleep 3
if [ "$had" = 1 ]; then cp "$backup" "$save"; md5sum "$save"; else rm -f "$save"; echo "progress.json removed: $([ -f "$save" ] && echo no || echo yes)"; fi
