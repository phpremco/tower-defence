#!/usr/bin/env bash
# Round 50: the hero's draw before his first shot, as stills: level from Boot without waves, the hero at his first post, the
# Drawing and Struck events hooked (r50_hero_count.cs), one ENEMY sent down the road; held steps until the draw begins, then
# stills at the start of the draw, mid-draw, at the aim and just after the first shot (r50_hero_state.cs at each), from the
# game camera framed on the hero. Copies the save aside first, restores and verifies it at the end.
# Usage: [ENEMY=name] r50_hero_draw.sh <output dir> <prefix> <level index>
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
run() { unity command eval_file --file "Tools/PlayTest/$1" --result-only 2>&1 | grep -o '"result": "[^"]*\|"message": "[^"]*' | cut -c1-1600; }
still() { echo "${LIGHT:-1.8}" > Tools/PlayTest/r49_light.txt; echo "0" > Tools/PlayTest/r50_orbit.txt; echo "$(cygpath -w "$out/${prefix}_$1.png")" > Tools/PlayTest/look_arg.txt; echo "  $1: $(run r50_still.cs)"; }
shoot() { echo "${ZOOM:-1.6}" > Tools/PlayTest/zoom_size.txt; run r49_zoom_hero.cs >/dev/null; step "1 hold"; echo "== $1: $(run r50_hero_state.cs)"; still "$1"; }
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
  if echo "$state" | grep -q "draw "; then break; fi
done
shoot draw_start
step "15 hold"; shoot draw_mid
step "17 hold"; shoot aim
step "6 hold"; shoot fire
step "12 hold"; shoot fire_next
run kill_all.cs >/dev/null; step 20
echo "1" > Tools/PlayTest/r49_light.txt
echo "=== CONSOLE"
unity command console_status --result-only 2>&1 | grep -E '"consoleErrors"|"consoleWarnings"'
unity command console --result-only 2>&1 | grep -o '"logType": "[A-Za-z]*", *"message": "[^"]*' | grep -v "allocation" | cut -c1-300 | sort | uniq -c | sort -rn | head -10
unity command editor_stop --result-only >/dev/null 2>&1
sleep 3
if [ "$had" = 1 ]; then cp "$backup" "$save"; md5sum "$save"; else rm -f "$save"; echo "progress.json removed: $([ -f "$save" ] && echo no || echo yes)"; fi
