#!/usr/bin/env bash
# S4 screenshots: every Crystal Plains level's opening view and one of its dialogues, the Cragback, the Shalebat and a
# rift, Mercer's two abilities, the Flak against packed flyers, the Laser, and a Land Mine and Chrono Field on the road.
# Captures land in Assets/Temp/s4_*.png (move them out and delete the folder afterwards). Dialogues mark the save:
# back it up before and restore it after. Usage: s4_shots.sh [part ...]  parts: levels creatures hero flak laser gadgets
set -u
cd "$(dirname "$0")/../.." || exit 1
parts=${*:-levels creatures hero flak laser gadgets}
status=$(unity command editor_status --result-only 2>&1)
if ! echo "$status" | grep -q '"playMode": *"stopped"'; then echo "NOT STOPPED - abort"; exit 1; fi
unity command clear_console --result-only >/dev/null 2>&1
unity command open_scene --path Assets/_Project/Scenes/Boot.unity --result-only >/dev/null 2>&1
unity command editor_play --result-only 2>&1 | grep -o "Entered[^\"]*"
sleep 8
step() { echo "$1" > Tools/PlayTest/frames_arg.txt; unity command eval_file --file Tools/PlayTest/step_frames.cs --result-only >/dev/null 2>&1; }
run() { unity command eval_file --file "Tools/PlayTest/$1" --result-only 2>&1 | grep -o '"result": "[^"]*' | cut -c12-200; }
s4() { echo "$*" > Tools/PlayTest/s4_arg.txt; echo "  $(run s4_scene.cs)"; }
shot() { unity command capture_game_view --save_path "Temp/s4_$1.png" --result-only >/dev/null 2>&1; echo "  shot s4_$1"; }
load() { echo "$1" > Tools/PlayTest/level_arg.txt; run start_level.cs; sleep 6; step 100; step 100; }
step 60
dialogues=(Dlg_L01_Stone Dlg_L02_Start Dlg_L03_Air Dlg_L04_Start Dlg_L05_Fast Dlg_L06_Chrono)

for part in $parts; do
  case $part in
    levels)
      for i in 0 1 2 3 4 5; do
        load $i
        shot "level$((i + 1))_open"
        s4 dialogue "${dialogues[$i]}"; step 3; run dialogue_finish.cs >/dev/null; step 40; shot "level$((i + 1))_dialogue"
      done
      ;;
    creatures)
      load 3; run no_waves.cs >/dev/null; run hero_to_base.cs >/dev/null
      s4 rift 7; step 3; shot rift
      s4 spawn Cragback 1; s4 spawn Shalebat 2; step 40
      s4 focus enemy:Cragback 10; s4 freeze; step 2; shot cragback; s4 thaw
      s4 focus enemy:Shalebat 10; s4 freeze; step 2; shot shalebat; s4 thaw
      s4 spawn Skitter 1; s4 spawn Roller 1; s4 spawn Prowler 1; s4 spawn Wisp 1; s4 spawn Cragback 1; step 30
      s4 focus road 6; s4 freeze; step 2; shot lineup; s4 thaw
      ;;
    hero)
      load 0; run no_waves.cs >/dev/null
      s4 spawn Skitter 12; step 120; step 60
      s4 focus post 9; s4 ability 0; step 2; shot hero_shockwave
      s4 ability 1; step 4; shot hero_repair; step 10; shot hero_repair_late
      ;;
    flak)
      load 3; run no_waves.cs >/dev/null
      s4 tower Rocket air 3
      s4 spawn Skyray 8; s4 spawn Shalebat 4
      s4 focus air 7
      for k in 1 2 3 4 5 6; do step 40; shot "flak_$k"; done
      ;;
    laser)
      load 1; run no_waves.cs >/dev/null
      s4 tower Laser road 2
      s4 spawn Skitter 14
      s4 focus road 7
      for k in 1 2 3; do step 50; shot "laser_$k"; done
      ;;
    gadgets)
      load 5; run no_waves.cs >/dev/null
      s4 gadget Mine road; s4 gadget SlowField post
      s4 focus post 7; step 20; shot chrono_field
      s4 focus road 7; step 2; shot land_mine
      s4 spawn Roller 2; s4 spawn Skitter 6; step 120
      s4 focus post 7; step 2; shot chrono_slowed
      ;;
  esac
done

echo "=== CONSOLE"
unity command console --result-only 2>&1 | grep -o '"message": "[^"]*' | grep -v "allocation" | sort | uniq -c | sort -rn | head -10
unity command editor_stop --result-only >/dev/null 2>&1
ls Assets/Temp | grep s4_ | grep -v meta
