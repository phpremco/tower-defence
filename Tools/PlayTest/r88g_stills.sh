#!/usr/bin/env bash
# Round 88g: the stills and the play check of the round (the drone strip over the Drones panel, the repair focus on a
# tower), taken in the STAGE (its editor started with TD_STAGE_GRAPHICS=1), never in the open editor and without any
# save handling. From the stage:
#   OUT=Logs/r88g bash Tools/PlayTest/r88g_stills.sh <part>
# Parts: play  (Shard Ridge Hard: five guns round the first gate, two repair drones and a harvest drone; the strip in
#               each state, the toggle on and off, the badge, and the edge cases of the mark played through: two drones,
#               the mark taken off, an upgrade, a sale, a wreck, a drone shot down, the gate and the 720p picture),
#        three (Fault Line Hard: all three drone kinds on the strip).
set -u
cd "$(dirname "$0")/../.." || exit 1
export MSYS_NO_PATHCONV=1
grep -q '^  productName: Tower defense stage' ProjectSettings/ProjectSettings.asset || { echo "not the stage: run this from the stage copy" >&2; exit 1; }
here="$(cygpath -w "$(pwd)")"
out=${OUT:-Logs/r88g}
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
run() { u eval_file --file "Tools/PlayTest/$1" --result-only | grep -o '"result": "[^"]*\|"message": "[^"]*' | cut -c1-2400; }
old() { echo "$*" > Tools/PlayTest/r88d_scene_arg.txt; run r88d_scene.cs; }
g() { echo "$*" > Tools/PlayTest/r88g_scene_arg.txt; run r88g_scene.cs; }
shot() { echo "${2:-1920} ${3:-1080} $out/$1.png" > Tools/PlayTest/r88b_shot_arg.txt; echo "$1: $(run r88b_shot.cs | cut -c1-300)"; }
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
# Game seconds at four times the speed (a stepped frame is 1/30 s of real time).
wait_s() { u set_timescale --scale 4 --result-only > /dev/null; steps $(( $1 * 30 / 4 )); u set_timescale --scale 1 --result-only > /dev/null; }
finish() {
  echo "console: $(u console_status --result-only | grep -E 'consoleErrors|consoleWarnings' | tr -d '\n')"
  u console --result-only | grep -o '"logType": "[A-Za-z]*", *"message": "[^"]*' | grep -v "allocation\|\"Log\"" | cut -c1-300 | sort | uniq -c | sort -rn | head -8
  u editor_stop --result-only >/dev/null
  sleep 4
}

case "${1:-play}" in
  play)
    level 2 2
    run no_waves.cs
    echo "setup: $(old towers:PulseRifle:5 focusgate:1.5)"
    echo "drones: $(g drones:Repair:2 drones:Harvest:1)"
    wait_s 2
    echo "A all whole: $(g state)"
    shot strip_idle
    echo "B hurt, gun 3 marked: $(g hurtall:0.5 focus:3 closemenu)"
    wait_s 3
    echo "B on the way: $(g state)"
    shot strip_flying
    wait_s 8
    echo "B at work: $(g state)"
    shot strip_working_badge
    shot strip_working_badge_720 1280 720
    echo "C menu on the marked gun: $(g menu:3)"
    step 4
    echo "C: $(g state)"
    shot menu_focus_on
    shot menu_focus_on_720 1280 720
    echo "D mark off: $(g focus:3)"
    step 4
    echo "D: $(g state)"
    shot menu_focus_off
    echo "D closed: $(g closemenu)"
    wait_s 2
    echo "D a while later (the drone finishes what it started): $(g state)"
    echo "E mark on gun 1 while both drones work: $(g focus:1 closemenu)"
    wait_s 6
    echo "E: $(g state)"
    echo "F the drones shot at: $(g downdrone:0 hurtdrone:1:0.25)"
    wait_s 6
    echo "F: $(g state)"
    shot strip_charging_low_battery
    echo "H the marked gun upgraded: $(g upgrade:1 closemenu)"
    wait_s 1
    echo "H: $(g state)"
    echo "I the marked gun hurt again: $(g hurt:1:0.4)"
    wait_s 5
    echo "I before the sale: $(g state)"
    echo "I sold under repair: $(g sell:1)"
    wait_s 5
    echo "I after: $(g state)"
    echo "J another gun marked and wrecked: $(g focus:0 closemenu)"
    step 4
    echo "J marked: $(g state | grep -o 'gun0[^;]*\|focus badges[^;]*' | tr '
' ' ')"
    echo "J wreck: $(g wreck:0)"
    step 4
    echo "J after: $(g state | grep -o 'gun0[^;]*\|focus badges[^;]*' | tr '
' ' ')"
    echo "K the gate marked and hurt: $(g focusgate closemenu) $(old hurtgates:0.6)"
    wait_s 8
    echo "K: $(g state)"
    shot gate_marked
    wait_s 30
    echo "M half a minute later: $(g state)"
    echo "L a chip pressed: $(g chip:0)"
    steps 45
    shot chip_camera
    finish ;;
  three)
    level 3 2
    run no_waves.cs
    echo "setup: $(old towers:PulseRifle:4 focusgate:1.5)"
    echo "drones: $(g drones:Repair:3 drones:Harvest:2 drones:Attack:2 hurtall:0.5)"
    wait_s 12
    echo "state: $(g state)"
    shot strip_three_kinds
    shot strip_three_kinds_720 1280 720
    finish ;;
esac
