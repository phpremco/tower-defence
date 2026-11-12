#!/usr/bin/env bash
# Round 51: a projectile trail as stills: level from Boot without waves, the hero at his post, a probe tower (TOWER, default
# Railgun) beside his gate (r51_tower_near.cs), a few creatures sent down the road; held steps until a shot's trail has grown
# (r51_trail_find.cs frames it at ZOOM), then stills lit for inspection and in the game light. Copies the save aside first,
# restores and verifies it at the end. Usage: [TOWER=name] [ZOOM=half height] r51_trail.sh <output dir> <prefix> <level index>
set -u
cd "$(dirname "$0")/../.." || exit 1
out=$1; prefix=$2; level=$3
mkdir -p "$out"
save="$LOCALAPPDATA/../LocalLow/DefaultCompany/Tower defense/Saves/progress.json"
backup="$out/${prefix}_progress_before.json"
had=0; if [ -f "$save" ]; then had=1; cp "$save" "$backup"; md5sum "$save"; else echo "no progress.json before"; fi
status=$(unity command editor_status --result-only 2>&1)
if ! echo "$status" | grep -q '"playMode": *"stopped"'; then echo "NOT STOPPED - abort"; exit 1; fi
unity command clear_console --result-only >/dev/null 2>&1
unity command open_scene --path Assets/_Project/Scenes/Boot.unity --result-only >/dev/null 2>&1
unity command editor_play --result-only 2>&1 | grep -o "Entered[^\"]*"
sleep 8
step() { echo "$1" > Tools/PlayTest/frames_arg.txt; unity command eval_file --file Tools/PlayTest/step_frames.cs --result-only >/dev/null 2>&1; }
run() { unity command eval_file --file "Tools/PlayTest/$1" --result-only 2>&1 | grep -o '"result": "[^"]*\|"message": "[^"]*' | cut -c1-600; }
still() { echo "$2" > Tools/PlayTest/r49_light.txt; echo "0" > Tools/PlayTest/r50_orbit.txt; echo "$(cygpath -w "$out/${prefix}_$1.png")" > Tools/PlayTest/look_arg.txt; echo "  $1: $(run r50_still.cs)"; }
step 60
echo "$level" > Tools/PlayTest/level_arg.txt; run start_level.cs; sleep 6; step 100; step 100
run no_waves.cs >/dev/null
run r16_dialogue_skip.cs >/dev/null
echo "${TOWER:-Railgun}" > Tools/PlayTest/tower_arg.txt; echo "  $(run r51_tower_near.cs)"
echo "-" > Tools/PlayTest/r45_face.txt
echo "${ZOOM:-2.5}" > Tools/PlayTest/zoom_size.txt
echo "${ENEMY:-Cragback}" > Tools/PlayTest/enemy_arg.txt
for i in 1 2 3; do run spawn_enemy.cs >/dev/null; step 20; done
found=""
for i in $(seq 1 150); do
  step "3 hold"; found=$(run r51_trail_find.cs)
  if echo "$found" | grep -q "found"; then break; fi
done
echo "  $found"
step "1 hold"; run r51_trail_find.cs >/dev/null
still trail "${LIGHT:-1.8}"; still trail_game 1
run kill_all.cs >/dev/null; step 20
echo "1" > Tools/PlayTest/r49_light.txt
echo "=== CONSOLE"
unity command console_status --result-only 2>&1 | grep -E '"consoleErrors"|"consoleWarnings"'
unity command console --result-only 2>&1 | grep -o '"logType": "[A-Za-z]*", *"message": "[^"]*' | grep -v "allocation" | cut -c1-300 | sort | uniq -c | sort -rn | head -10
unity command editor_stop --result-only >/dev/null 2>&1
sleep 3
if [ "$had" = 1 ]; then cp "$backup" "$save"; md5sum "$save"; else rm -f "$save"; echo "progress.json removed: $([ -f "$save" ] && echo no || echo yes)"; fi
