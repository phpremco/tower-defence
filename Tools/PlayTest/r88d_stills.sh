#!/usr/bin/env bash
# Round 88d: the stills and play checks of the round, taken in the STAGE (its editor started with TD_STAGE_GRAPHICS=1),
# never in the open editor and without any save handling. From the stage:
#   OUT=Logs/r88d bash Tools/PlayTest/r88d_stills.sh <part> <tag> ...     (tag: before | after, part of the file names)
# Parts: field  (Shard Ridge Hard: guns round the first gate, the gates hurt -> gatebar_<tag>; the gate wrecked -> badge_<tag>,
#                and with the cursor on the wreck -> badge_<tag>_selected),
#        alert  (Shard Ridge Hard: start money, the warning two waves before the first raid -> alert_<tag>),
#        turret (Landing Site Medium: base turrets mounted, one wrecked, a repair drone launched; plays until it stands again).
set -u
cd "$(dirname "$0")/../.." || exit 1
export MSYS_NO_PATHCONV=1
grep -q '^  productName: Tower defense stage' ProjectSettings/ProjectSettings.asset || { echo "not the stage: run this from the stage copy" >&2; exit 1; }
here="$(cygpath -w "$(pwd)")"
out=${OUT:-Logs/r88d}
mkdir -p "$out"
u() {
  local result n=0
  while :; do
    result="$(UNITY_PROJECT_PATH="$here" unity command "$@" 2>&1)"
    if echo "$result" | grep -q "AMBIGUOUS_EDITOR\|error occurred while sending\|No Pipeline instance" && [ $n -lt 8 ]; then n=$((n + 1)); sleep 4; continue; fi
    echo "$result"; return 0
  done
}
step() { echo "$1 hold" > Tools/PlayTest/frames_arg.txt; u eval_file --file Tools/PlayTest/step_frames.cs --result-only >/dev/null; }
steps() { local left=$1; while [ "$left" -gt 0 ]; do local n=$((left > 60 ? 60 : left)); step "$n"; left=$((left - n)); done; }
run() { u eval_file --file "Tools/PlayTest/$1" --result-only | grep -o '"result": "[^"]*\|"message": "[^"]*' | cut -c1-1600; }
probe() { echo "$*" > Tools/PlayTest/r88c_field_arg.txt; run r88c_field_probe.cs; }
scene() { echo "$*" > Tools/PlayTest/r88d_scene_arg.txt; run r88d_scene.cs; }
shot() { echo "1920 1080 $out/$1.png" > Tools/PlayTest/r88b_shot_arg.txt; echo "$1: $(run r88b_shot.cs | cut -c1-500)"; }
begin() {
  if ! u editor_status --result-only | grep -q '"playMode": *"stopped"'; then echo "STAGE NOT STOPPED - abort"; exit 1; fi
  u clear_console --result-only >/dev/null
  u open_scene --path Assets/_Project/Scenes/Boot.unity --result-only >/dev/null
  u editor_play --result-only | grep -o "Entered[^\"]*"
  sleep 8
  for i in 1 2 3; do step 60; sleep 1; done
}
level() { # <node> <difficulty>
  begin
  echo "$1 $2" > Tools/PlayTest/r37_load_arg.txt; run r37_load.cs
  sleep 8
  step 30
  run r17_fixed_step.cs
  echo "deploy: $(run deploy.cs)"
  for i in $(seq 1 10); do step 60; done
  echo "dialogue: $(run r16_dialogue_skip.cs)"
  step 60
  echo "dialogue: $(run r16_dialogue_skip.cs)"
}
finish() {
  echo "console: $(u console_status --result-only | grep -E 'consoleErrors|consoleWarnings' | tr -d '\n')"
  u console --result-only | grep -o '"logType": "[A-Za-z]*", *"message": "[^"]*' | grep -v "allocation\|\"Log\"" | cut -c1-300 | sort | uniq -c | sort -rn | head -8
  u editor_stop --result-only >/dev/null
  sleep 4
}

part="$1"; tag="${2:-after}"
case "$part" in
  field)
    level 2 2
    echo "start: $(scene state | cut -c1-200)"
    run no_waves.cs
    echo "setup: $(scene towers:PulseRifle:5 hurtgates:0.55 hurttowers:0.55 focusgate:1.5)"
    step 6
    shot "gatebar_$tag"
    echo "wreck: $(scene wreckgate cursoraway)"
    steps 240
    echo "state: $(scene state | cut -c1-600)"
    shot "badge_$tag"
    echo "cursor: $(scene cursorgate)"
    step 6
    echo "state: $(scene state | cut -c1-600)"
    shot "badge_${tag}_selected"
    finish ;;
  alert)
    level 2 2
    echo "start: $(scene state | cut -c1-120)"
    echo "jump: $(probe killall gotowave:9)"
    step 20
    echo "wave 9: $(probe purse)"
    echo "fade: $(scene alertin)"
    step 2
    shot "alert_$tag"
    echo "after the still: $(probe purse)"
    finish ;;
  turret)
    level 0 1
    run no_waves.cs
    echo "setup: $(scene turrets)"
    step 4
    echo "wreck: $(scene wreckturret focusturret:1.6 state)"
    echo "drone: $(run r88_buy_repair.cs)"
    step 10
    shot "turret_wrecked"
    u set_timescale --scale 4 --result-only > /dev/null
    for i in $(seq 1 30); do
      steps 60
      state="$(scene state)"
      echo "t$i: $(echo "$state" | cut -c1-400)"
      [ "$i" = 3 ] && shot "turret_crew_at_work"
      echo "$state" | grep -q "BaseTurret[^;]*WRECK" || break
    done
    u set_timescale --scale 1 --result-only > /dev/null
    step 10
    shot "turret_raised"
    finish ;;
esac
