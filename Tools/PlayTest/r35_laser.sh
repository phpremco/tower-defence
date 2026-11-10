#!/usr/bin/env bash
# Round 35: playtest notes 9 and 10 checked in one run. A probe Laser Turret beside the road of campaign node $1 (default 1),
# Cragbacks and a Skitter sent past it at x3, stills of the beam, the blue armour bar and the glancing sparks.
# Usage: bash Tools/PlayTest/r35_laser.sh [node]
set -u
cd "$(dirname "$0")/../.." || exit 1
source Tools/PlayTest/qa/lib.sh
LIVE="C:/Users/remco/AppData/Local/Temp/claude/b18d9d7fa25a4d30/2aa46f30-11b3-418c-a974-fe154b605af3/scratchpad/save/progress.json"
stopped || { echo "NOT STOPPED - abort"; exit 1; }
node=${1:-1}
unity command clear_console --result-only >/dev/null 2>&1
unity command open_scene --path Assets/_Project/Scenes/Boot.unity --result-only >/dev/null 2>&1
unity command editor_play --result-only 2>&1 | grep -o "Entered[^\"]*"
sleep 8
steps 60
echo "node $node" > Tools/PlayTest/r15b_load.txt; ev r15b_load.cs; sleep 8
steps 30
echo "waves: $(ev no_waves.cs)"
echo "dialogue: $(ev r16_dialogue_skip.cs)"
echo Laser > Tools/PlayTest/tower_arg.txt; echo "tower: $(ev build_tower.cs)"
echo Cragback > Tools/PlayTest/enemy_arg.txt; ev spawn_enemy.cs >/dev/null; steps 4
echo 5 > Tools/PlayTest/zoom_size.txt; echo 3 > Tools/PlayTest/r23a_speed.txt
for i in 1 2 3 4 5 6 7 8; do
  r=$(ev r35_armour.cs); echo "shot $i: $r"
  case "$r" in no*) echo Cragback > Tools/PlayTest/enemy_arg.txt; ev spawn_enemy.cs >/dev/null; steps 4; echo "respawn: $(ev r35_armour.cs)";; esac
  steps 2
  echo "$SHOTS/r35_laser_x3_$i.png" > Tools/PlayTest/look_arg.txt; ev render_still.cs >/dev/null
  steps 5
done
errs
unity command editor_stop --result-only >/dev/null 2>&1
sleep 3
echo "save: $(saveok)"
