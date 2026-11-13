#!/usr/bin/env bash
# Round 61: stills of the hero's shockwave across its timeline, in the game's own light, with towers firing beside him.
# Starts level <index> (default 0), switches the waves off, stands a Cannon and two Gun Turrets by the hero, spawns Skitters
# until a few are at his post, uses the shockwave and renders the play camera framed on the hero at the frames in FRAMES
# (stepped frames of 0.02 s after the request). LOW=1 plays the Low (Switch) profile. Copies the save aside first, restores
# and verifies it at the end. Usage: [LOW=1] [FRAMES="1 4 8 13 19 27 40 60"] bash Tools/PlayTest/r61_shockwave.sh <out dir> <prefix> [level]
set -u
cd "$(dirname "$0")/../.." || exit 1
out=$1; prefix=$2; level=${3:-0}
frames=${FRAMES:-"1 4 8 13 19 27 40 60"}
mkdir -p "$out"
save="$LOCALAPPDATA/../LocalLow/DefaultCompany/Tower defense/Saves/progress.json"
backup="$out/${prefix}_progress_before.json"
cp "$save" "$backup"; echo "save before $(md5sum < "$save" | cut -c1-32)"
status=$(unity command editor_status --result-only 2>&1)
if ! echo "$status" | grep -q '"playMode": *"stopped"'; then echo "NOT STOPPED - abort"; exit 1; fi
unity command clear_console --result-only >/dev/null 2>&1
unity command open_scene --path Assets/_Project/Scenes/Boot.unity --result-only >/dev/null 2>&1
unity command editor_play --result-only 2>&1 | grep -o "Entered[^\"]*"
sleep 8
step() { echo "$1" > Tools/PlayTest/frames_arg.txt; unity command eval_file --file Tools/PlayTest/step_frames.cs --result-only >/dev/null 2>&1; }
run() { unity command eval_file --file "Tools/PlayTest/$1" --result-only 2>&1 | grep -o '"result": "[^"]*\|"message": "[^"]*' | cut -c1-600; }
shock() { echo "$1" > Tools/PlayTest/r61_shock_arg.txt; run r61_shock_setup.cs; }
still() { echo "1" > Tools/PlayTest/r49_light.txt; echo "0" > Tools/PlayTest/r50_orbit.txt; echo "$(cygpath -w "$out/${prefix}_$1.png")" > Tools/PlayTest/look_arg.txt; echo "  $1: $(run r50_still.cs)"; }
frame() { echo "${ZOOM:-4.2}" > Tools/PlayTest/zoom_size.txt; run r49_zoom_hero.cs >/dev/null; }
step 60
[ "${LOW:-0}" = 1 ] && echo "profile: $(run low_profile.cs)"
echo "$level" > Tools/PlayTest/level_arg.txt; run start_level.cs; sleep 6; step 100; step 100
run no_waves.cs >/dev/null
run r16_dialogue_skip.cs >/dev/null
step "2 hold"
echo "-" > Tools/PlayTest/r45_face.txt
echo "towers: $(shock towers)"
near=""
for i in $(seq 1 120); do
  # Tough walkers, so some reach the post alive and the towers have something to fire at while the wave rings out.
  if [ "$i" -le 16 ]; then
    if [ $((i % 4)) = 0 ]; then echo "Cragback" > Tools/PlayTest/enemy_arg.txt; else echo "Prowler" > Tools/PlayTest/enemy_arg.txt; fi
    run spawn_enemy.cs >/dev/null
  fi
  step "40 hold"; near=$(shock near)
  n=$(echo "$near" | grep -o "near [0-9]*" | cut -d' ' -f2)
  if [ "${n:-0}" -ge ${NEAR:-3} ]; then break; fi
done
echo "at the post after $i rounds: $near"
frame; step "1 hold"; frame
still "t00_before"
echo "$(shock fire)"
last=0
for f in $frames; do
  step "$((f - last)) hold"; frame; last=$f
  still "t$(printf '%02d' "$f")"
  [ "${PROBE:-0}" = 1 ] && echo "    $(run r61_shock_probe.cs)"
done
run kill_all.cs >/dev/null; step 20
echo "=== CONSOLE"
unity command console_status --result-only 2>&1 | grep -E '"consoleErrors"|"consoleWarnings"'
unity command console --result-only 2>&1 | grep -o '"logType": "[A-Za-z]*", *"message": "[^"]*' | grep -v "allocation" | cut -c1-300 | sort | uniq -c | sort -rn | head -8
unity command editor_stop --result-only >/dev/null 2>&1
sleep 3
cp "$backup" "$save"; echo "save after restore $(md5sum < "$save" | cut -c1-32)"
rm -f "$backup"
