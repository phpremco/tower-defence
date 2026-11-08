#!/usr/bin/env bash
# Round 23a: a Laser probe tower beside the road of campaign node $2 (default 1), Cragbacks and Skitters sent past it,
# the game at x3, stills of the beam and of the plated creatures' blue line. Usage: r23a_laser.sh <output dir> [node]
set -u
cd "$(dirname "$0")/../.." || exit 1
out=$1; node=${2:-1}
status=$(unity command editor_status --result-only 2>&1)
if ! echo "$status" | grep -q '"playMode": *"stopped"'; then echo "NOT STOPPED - abort"; exit 1; fi
unity command clear_console --result-only >/dev/null 2>&1
unity command open_scene --path Assets/_Project/Scenes/Boot.unity --result-only >/dev/null 2>&1
unity command editor_play --result-only 2>&1 | grep -o "Entered[^\"]*"
sleep 8
step() { echo "$1" > Tools/PlayTest/frames_arg.txt; unity command eval_file --file Tools/PlayTest/step_frames.cs --result-only >/dev/null 2>&1; }
run() { unity command eval_file --file "Tools/PlayTest/$1" --result-only 2>&1 | grep -o '"result": "[^"]*\|"message": "[^"]*' | cut -c1-600; }
step 60
echo "node $node" > Tools/PlayTest/r15b_load.txt; run r15b_load.cs; sleep 8
step 30
echo "waves: $(run no_waves.cs)"
echo "dialogue: $(run r16_dialogue_skip.cs)"
echo Laser > Tools/PlayTest/tower_arg.txt; echo "tower: $(run build_tower.cs)"
echo "Laser Turret" > Tools/PlayTest/r23a_tower_name.txt
for e in Cragback Cragback Skitter Cragback; do echo "$e" > Tools/PlayTest/enemy_arg.txt; run spawn_enemy.cs; step 8; done
echo 0.9 > Tools/PlayTest/zoom_size.txt; echo 3 > Tools/PlayTest/r23a_speed.txt
for i in 1 2 3 4 5 6; do
  echo "shot $i: $(run r23a_speed.cs)"; step 4
  echo "$out/r23a_x3_$i.png" > Tools/PlayTest/look_arg.txt; run render_still.cs >/dev/null
  step 8
done
echo "=== CONSOLE"
unity command console --result-only 2>&1 | grep -o '"message": "[^"]*' | grep -v "allocation" | sort | uniq -c | sort -rn | head -8
unity command editor_stop --result-only >/dev/null 2>&1
