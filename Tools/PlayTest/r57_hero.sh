#!/usr/bin/env bash
# Round 57: the hero firing several shots in a row. As r51_hero.sh up to the aim, then held single steps: every frame the
# upper body's turn since the last frame (r57_hero_pose.cs) goes to <out>/<prefix>_frames.txt, and at each of the first
# SHOTS shots a still on the shot frame and two frames later (inspection light; the first also in the plain light) are
# rendered from the game camera framed on the hero. Copies the save aside first, restores and verifies it at the end.
# Usage: [ENEMY=name] [SHOTS=3] [FRAMES=150] [STILLS=0] r57_hero.sh <output dir> <prefix> <level index>
set -u
cd "$(dirname "$0")/../.." || exit 1
out=$1; prefix=$2; level=$3
shots_wanted=${SHOTS:-3}; frames=${FRAMES:-150}; stills=${STILLS:-1}
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
run() { unity command eval_file --file "Tools/PlayTest/$1" --result-only 2>&1 | grep -o '"result": "[^"]*\|"message": "[^"]*' | cut -c1-1600; }
still() { echo "${LIGHT:-1.8}" > Tools/PlayTest/r49_light.txt; echo "0" > Tools/PlayTest/r50_orbit.txt; echo "$(cygpath -w "$out/${prefix}_$1.png")" > Tools/PlayTest/look_arg.txt; echo "  $1: $(run r50_still.cs)"; }
frame() { echo "${ZOOM:-1.6}" > Tools/PlayTest/zoom_size.txt; run r49_zoom_hero.cs >/dev/null; }
step 60
echo "$level" > Tools/PlayTest/level_arg.txt; run start_level.cs; sleep 6; step 100; step 100
run no_waves.cs >/dev/null
run r16_dialogue_skip.cs >/dev/null
step "2 hold"
echo "-" > Tools/PlayTest/r45_face.txt
echo "  $(run r50_hero_count.cs)"
echo "${ENEMY:-Skitter}" > Tools/PlayTest/enemy_arg.txt
echo "$(run spawn_enemy.cs)"
for i in $(seq 1 200); do
  step "3 hold"; state=$(run r50_hero_state.cs)
  if echo "$state" | grep -qE "draw |shot@"; then break; fi
done
echo "draw: $state"
frame
echo "reset" > Tools/PlayTest/r57_pose_arg.txt; run r57_hero_pose.cs >/dev/null
: > "$out/${prefix}_frames.txt"
seen=$(echo "$state" | grep -o "shot@" | wc -l)
base=$seen
for i in $(seq 1 "$frames"); do
  step "1 hold"; frame
  line=$(run r57_hero_pose.cs); echo "$line" >> "$out/${prefix}_frames.txt"
  shots=$(echo "$line" | grep -o "shots=[0-9]*" | cut -d= -f2)
  if [ "${shots:-0}" -gt "$seen" ] && [ $((seen - base)) -lt "$shots_wanted" ]; then
    seen=$shots; n=$((seen - base))
    echo "shot $n (count $seen): $line"
    if [ "$stills" = 1 ]; then
      still "shot${n}"
      [ "$n" = 1 ] && LIGHT=1 still "shot1_game"
      echo "reset" > Tools/PlayTest/r57_pose_arg.txt
      step "2 hold"; frame; run r57_hero_pose.cs >/dev/null; still "shot${n}_b"
      echo "reset" > Tools/PlayTest/r57_pose_arg.txt; run r57_hero_pose.cs >/dev/null
    fi
  fi
  if [ $((seen - base)) -ge "$shots_wanted" ] && [ "$i" -gt 40 ] && [ "$stills" = 1 ]; then break; fi
done
run kill_all.cs >/dev/null; step 20
echo "1" > Tools/PlayTest/r49_light.txt
echo "=== CONSOLE"
unity command console_status --result-only 2>&1 | grep -E '"consoleErrors"|"consoleWarnings"'
unity command console --result-only 2>&1 | grep -o '"logType": "[A-Za-z]*", *"message": "[^"]*' | grep -v "allocation" | cut -c1-300 | sort | uniq -c | sort -rn | head -10
unity command editor_stop --result-only >/dev/null 2>&1
sleep 3
if [ "$had" = 1 ]; then cp "$backup" "$save"; md5sum "$save"; else rm -f "$save"; echo "progress.json removed: $([ -f "$save" ] && echo no || echo yes)"; fi
