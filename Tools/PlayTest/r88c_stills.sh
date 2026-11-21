#!/usr/bin/env bash
# Round 88c: the stills of the round, taken in the STAGE (its editor started with TD_STAGE_GRAPHICS=1), never in the open
# editor and without any save handling. Usage (from the stage): OUT=Logs/r88c bash Tools/PlayTest/r88c_stills.sh <part> ...
# Parts: menu (the main menu's boss without a ground mark), alert (the ALERT! ping label and the air raid warning),
#        escort (a level boss with its brood running ahead; also the win check: the boss dies, the brood lives),
#        volley (the Brood Mother's volley in the air and landing on three guns).
set -u
cd "$(dirname "$0")/../.." || exit 1
export MSYS_NO_PATHCONV=1
grep -q '^  productName: Tower defense stage' ProjectSettings/ProjectSettings.asset || { echo "not the stage: run this from the stage copy" >&2; exit 1; }
here="$(pwd -W | sed 's|/|\\|g')"
out=${OUT:-Logs/r88c}
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
run() { u eval_file --file "Tools/PlayTest/$1" --result-only | grep -o '"result": "[^"]*\|"message": "[^"]*' | cut -c1-1400; }
probe() { echo "$*" > Tools/PlayTest/r88c_field_arg.txt; run r88c_field_probe.cs; }
shot() { # <name> <focus> <zoom> <hud|clean>
  { echo "$2 $3 $4 1920 1080"; cygpath -m "$out/$1.png"; } > Tools/PlayTest/r65_shot_arg.txt; echo "$1: $(run r88_shot.cs)"; }
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
spawn() { echo "$1" > Tools/PlayTest/enemy_arg.txt; run spawn_enemy.cs; }

for part in "$@"; do
  case "$part" in
    menu)
      begin
      run goto_menu.cs
      sleep 6
      run r17_fixed_step.cs
      seen=""
      for i in $(seq 1 60); do
        step 60
        seen="$(run r88c_menu_probe.cs)"
        echo "$seen" | grep -q "SHOWN" && break
      done
      step 90
      echo "menu probe: $(run r88c_menu_probe.cs)"
      { echo "1920 1080 ui"; cygpath -m "$out/menu_no_ring.png"; } > Tools/PlayTest/r88c_cam_arg.txt; run r88c_cam_shot.cs
      { echo "3840 2160 clean"; cygpath -m "$out/menu_no_ring_4k.png"; } > Tools/PlayTest/r88c_cam_arg.txt; run r88c_cam_shot.cs
      finish ;;
    alert)
      # Shard Ridge on Hard: its first raid is wave 11, so the warning and the purse come with wave 9.
      level 2 2
      echo "before: $(probe purse)"
      echo "jump: $(probe killall gotowave:9)"
      step 20
      echo "wave 9: $(probe purse state | cut -c1-300)"
      echo "1920 1080 $out/air_raid_alert.png" > Tools/PlayTest/r88b_shot_arg.txt; run r88b_shot.cs | cut -c1-400
      echo "vp:0.62:0.45" > Tools/PlayTest/r88b_ping_arg.txt; run r88b_ping.cs | cut -c1-300
      step 6
      echo "1920 1080 $out/alert_label.png" > Tools/PlayTest/r88b_shot_arg.txt; run r88b_shot.cs | cut -c1-400
      finish ;;
    escort)
      # Landing Site on Hard, no waves and no gates: creatures as tough as wave 8 (the first boss wave on Hard). A Cragback
      # first (the plated walker the brood may come as), then the Brood Mother; stills once a Cragback walks with it.
      level 0 2
      run no_waves.cs
      echo "setup: $(probe nogates wavescale:8)"
      echo "1" > Tools/PlayTest/zoom_size.txt
      spawn Cragback; step 10; probe killall
      step 20
      spawn BroodMother
      u set_timescale --scale 4 --result-only > /dev/null
      for i in $(seq 1 16); do
        steps 120
        state="$(probe state)"
        echo "t$i: $(echo "$state" | cut -c1-900)"
        echo "$state" | grep -q "Cragback" && break
      done
      u set_timescale --scale 1 --result-only > /dev/null
      steps 45
      echo "at the still: $(probe state | cut -c1-900)"
      echo "BroodMother" > Tools/PlayTest/enemy_arg.txt
      run r88_focus.cs; step 2
      shot boss_mark_escorts keep 1 clean
      echo "2" > Tools/PlayTest/zoom_size.txt; run r88_focus.cs; step 2
      shot boss_mark_escorts_close keep 1 clean
      steps 90
      echo "three seconds on: $(probe state | cut -c1-900)"
      run r88_focus.cs; step 2
      shot boss_mark_escorts_close2 keep 1 clean
      echo "1" > Tools/PlayTest/zoom_size.txt
      finish ;;
    bosswave)
      # The real thing, Landing Site on Hard: its last wave (heavy, with the boss) as stored. The boss must come out behind
      # its plated crowd, in plain view, and its brood must run ahead of it. Gates wrecked so the column keeps walking.
      level 0 2
      echo "setup: $(probe killall gotowave:12)"
      step 5
      echo "at: $(probe state | cut -c1-200)"
      u set_timescale --scale 4 --result-only > /dev/null
      for i in $(seq 1 40); do
        steps 60
        state="$(probe state)"
        echo "$state" | grep -q "BOSS" && break
      done
      u set_timescale --scale 1 --result-only > /dev/null
      echo "boss out: $(echo "$state" | cut -c1-1200)"
      echo "BroodMother" > Tools/PlayTest/enemy_arg.txt
      echo "1" > Tools/PlayTest/zoom_size.txt
      for n in 1 2 3; do
        steps 150
        echo "still $n: $(probe state | cut -c1-1200)"
        run r88_focus.cs; step 2
        shot bosswave_$n keep 1 clean
      done
      finish ;;
    win)
      # The win rule in play, Landing Site on Easy: the last wave comes out, its boss is killed while other creatures
      # (its brood among them) live: the level must go on; only when the last of them dies is it won.
      level 0 0
      echo "jump: $(probe killall gotowave:12)"
      u set_timescale --scale 4 --result-only > /dev/null
      for i in $(seq 1 60); do
        steps 30
        state="$(probe healbase state diag)"
        echo "$state" | grep -q "BOSS" && echo "$state" | grep -q "no wave spawning" && echo "$state" | grep -q "running" && break
      done
      u set_timescale --scale 1 --result-only > /dev/null
      echo "last wave out, boss and brood on the field: $(probe state diag | cut -c1-500)"
      echo "boss killed: $(probe killboss | cut -c1-100)"
      steps 30
      echo "a second later: $(probe healbase state | cut -c1-400)"
      echo "the rest killed: $(probe killall | cut -c1-100)"
      steps 10
      echo "then: $(probe state | cut -c1-200)"
      finish ;;
    volley)
      level 0 2
      run no_waves.cs
      spawn BroodMother
      u set_timescale --scale 4 --result-only > /dev/null
      steps 480
      u set_timescale --scale 1 --result-only > /dev/null
      echo "Cannon 3 2.2 3.6" > Tools/PlayTest/r88c_towers_arg.txt; echo "towers: $(run r88c_towers_near_boss.cs)"
      echo "BroodMother" > Tools/PlayTest/enemy_arg.txt
      echo "1.5" > Tools/PlayTest/zoom_size.txt
      flying=0
      for i in $(seq 1 80); do
        step 4
        state="$(probe bolts)"
        flying=$(echo "$state" | grep -o "flight [0-9]*" | grep -o "[0-9]*")
        [ "${flying:-0}" -gt 0 ] && break
      done
      echo "launch: $state"
      run r88_focus.cs; step 1
      echo "probe: $(run r88c_bolt_probe.cs)"
      shot volley_1_launch keep 1 clean
      step 10; shot volley_2_in_flight keep 1 clean
      echo "probe: $(run r88c_bolt_probe.cs)"
      step 10; shot volley_3_in_flight keep 1 clean
      echo "probe: $(run r88c_bolt_probe.cs)"
      for i in $(seq 1 30); do
        step 2
        state="$(probe bolts)"
        flying=$(echo "$state" | grep -o "flight [0-9]*" | grep -o "[0-9]*")
        [ "${flying:-0}" -eq 0 ] && break
      done
      echo "landed: $state"
      shot volley_4_impact keep 1 clean
      step 3; shot volley_5_after keep 1 clean
      echo "1" > Tools/PlayTest/zoom_size.txt
      finish ;;
  esac
done
