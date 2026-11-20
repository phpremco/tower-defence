#!/usr/bin/env bash
# Round 88a: stills of a level from the play camera, taken in the STAGE (Docs/COMMANDS.md, "Round 85" and "Round 88a"),
# never in the open editor: r65_shots.sh without any save handling. The stage has its own product name, so it cannot
# reach Remco's save, and its editor must run with a graphics device (TD_STAGE_GRAPHICS=1 bash Tools/PlayTest/stage.sh start).
# Run it from inside the stage: cd <stage> && OUT=<dir> bash Tools/PlayTest/r88_shots.sh <prefix> <node> <difficulty> <action> ...
# Actions as r65_shots.sh: act:, eval:, arg:<file>=<text>, step:<frames>, skip:<n>, dialogue, scale:<x>,
# shot:<name>|<focus> <zoom> <hud|clean> [w h]. There is no view: action (a batch editor has no Game view).
set -u
cd "$(dirname "$0")/../.." || exit 1
export MSYS_NO_PATHCONV=1
grep -q '^  productName: Tower defense stage' ProjectSettings/ProjectSettings.asset || { echo "not the stage: run this from the stage copy" >&2; exit 1; }
here="$(pwd -W | sed 's|/|\\|g')"
prefix=$1; node=$2; difficulty=$3; shift 3
out=${OUT:-Logs/r88}
mkdir -p "$out"
u() {
  local result n=0
  while :; do
    result="$(UNITY_PROJECT_PATH="$here" unity command "$@" 2>&1)"
    if echo "$result" | grep -q "AMBIGUOUS_EDITOR\|error occurred while sending\|No Pipeline instance" && [ $n -lt 8 ]; then n=$((n + 1)); sleep 4; continue; fi
    echo "$result"; return 0
  done
}
if ! u editor_status --result-only | grep -q '"playMode": *"stopped"'; then echo "STAGE NOT STOPPED - abort"; exit 1; fi
u clear_console --result-only >/dev/null
u open_scene --path Assets/_Project/Scenes/Boot.unity --result-only >/dev/null
u editor_play --result-only | grep -o "Entered[^\"]*"
sleep 8
step() { echo "$1 hold" > Tools/PlayTest/frames_arg.txt; u eval_file --file Tools/PlayTest/step_frames.cs --result-only >/dev/null; }
run() { u eval_file --file "Tools/PlayTest/$1" --result-only | grep -o '"result": "[^"]*\|"message": "[^"]*' | cut -c1-900; }
for i in 1 2 3; do step 60; sleep 1; done
run intro_skip.cs >/dev/null
echo "$node $difficulty" > Tools/PlayTest/r37_load_arg.txt; run r37_load.cs
sleep 8
step 30
run r17_fixed_step.cs
echo "deploy: $(run deploy.cs)"
for i in $(seq 1 10); do step 60; done
echo "dialogue: $(run r16_dialogue_skip.cs)"
step 60
echo "dialogue: $(run r16_dialogue_skip.cs)"
for action in "$@"; do
  case "$action" in
    act:*) echo "${action#act:}" > Tools/PlayTest/r65_act_arg.txt; echo "act ${action#act:}: $(run r65_act.cs)" ;;
    eval:*) echo "${action#eval:}: $(run "${action#eval:}")" ;;
    arg:*) spec="${action#arg:}"; echo "${spec#*=}" > "Tools/PlayTest/${spec%%=*}" ;;
    skip:*) for k in $(seq 1 "${action#skip:}"); do run skip_wave.cs >/dev/null; step 4; done; echo "skipped ${action#skip:}" ;;
    step:*) left=${action#step:}; while [ "$left" -gt 0 ]; do n=$((left > 60 ? 60 : left)); step "$n"; left=$((left - n)); done ;;
    dialogue) echo "dialogue: $(run r16_dialogue_skip.cs)" ;;
    scale:*) u set_timescale --scale "${action#scale:}" --result-only > /dev/null ;;
    shot:*) spec="${action#shot:}"; name="${spec%%|*}"; rest="${spec#*|}"; set -- $rest
      { echo "$1 $2 $3 ${4:-} ${5:-}"; cygpath -m "$out/${prefix}_${name}.png"; } > Tools/PlayTest/r65_shot_arg.txt; echo "$name: $(run r88_shot.cs)" ;;
  esac
done
echo "console: $(u console_status --result-only | grep -E 'consoleErrors|consoleWarnings' | tr -d '\n')"
u console --result-only | grep -o '"logType": "[A-Za-z]*", *"message": "[^"]*' | grep -v "allocation\|\"Log\"" | cut -c1-300 | sort | uniq -c | sort -rn | head -8
u editor_stop --result-only >/dev/null
sleep 4
