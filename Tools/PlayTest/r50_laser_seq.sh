#!/usr/bin/env bash
# Round 50: the ray boss's tail shot as a sequence of stills (r50_beam_shots.sh flow and lighting): spawn the Sky Sovereign,
# walk it (held steps, no free frames) until it is within reach of the first gate (x >= REACH_X), make its laser due in
# LEAD seconds, then render the wind-up, the shot, the held aim and the recovery from the game camera, lit for inspection
# and from the side (camera orbited by SIDE), with r50_beam_probe.cs and r50_clip_state.cs at each step. Copies the save
# aside first, restores and verifies it at the end. Usage: r50_laser_seq.sh <output dir> <prefix> <level index>
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
run() { unity command eval_file --file "Tools/PlayTest/$1" --result-only 2>&1 | grep -o '"result": "[^"]*\|"message": "[^"]*' | cut -c1-2400; }
still() { echo "$2" > Tools/PlayTest/r49_light.txt; echo "$3" > Tools/PlayTest/r50_orbit.txt; echo "$(cygpath -w "$out/${prefix}_$1.png")" > Tools/PlayTest/look_arg.txt; echo "  $1: $(run r50_still.cs)"; }
shoot() {
  echo "${ZOOM:-3.2}" > Tools/PlayTest/zoom_size.txt
  echo "== $1"; echo "  $(run r45_zoom.cs)"; step "1 hold"
  echo "  probe: $(run r50_beam_probe.cs)"; echo "  clips: $(run r50_clip_state.cs)"; echo "  points: $(run r51_beam_points.cs)"; if [ -n "${R51_REGION:-}" ]; then echo "$R51_REGION" | tr "," " " > Tools/PlayTest/r51_region.txt; echo "  region: $(run r51_screen_probe.cs)"; fi
  still "$1" "${LIGHT:-1.8}" "0"; still "$1_game" 1 "0"; still "$1_side" "${LIGHT:-1.8}" "${SIDE:-70 22 1.2}"
  run resume.cs >/dev/null
}
step 60
echo "$level" > Tools/PlayTest/level_arg.txt; run start_level.cs; sleep 6; step 100; step 100
run no_waves.cs >/dev/null
run r16_dialogue_skip.cs >/dev/null
run hero_to_base.cs >/dev/null; step 20
echo "SkySovereign" > Tools/PlayTest/enemy_arg.txt
echo "$(run spawn_enemy.cs)"
for i in $(seq 1 60); do
  step "20 hold"; state=$(run r50_clip_state.cs)
  x=$(echo "$state" | grep -o "pos ([-0-9.]*" | grep -o "[-0-9.]*$")
  if awk "BEGIN { exit !($x >= ${REACH_X:--10}) }"; then break; fi
done
echo "  reached: $state"
echo "${LEAD:-1.5}" > Tools/PlayTest/r50_laser_in.txt; echo "  $(run r50_laser_in.cs)"
step "60 hold"; shoot windup
step "33 hold"; shoot fire
step "90 hold"; shoot hold
step "${RECOVER:-125} hold"; shoot recover
run kill_all.cs >/dev/null; step 20
echo "1" > Tools/PlayTest/r49_light.txt; echo "0" > Tools/PlayTest/r50_orbit.txt
echo "=== CONSOLE"
unity command console_status --result-only 2>&1 | grep -E '"consoleErrors"|"consoleWarnings"'
unity command console --result-only 2>&1 | grep -o '"logType": "[A-Za-z]*", *"message": "[^"]*' | grep -v "allocation" | cut -c1-300 | sort | uniq -c | sort -rn | head -10
unity command editor_stop --result-only >/dev/null 2>&1
sleep 3
if [ "$had" = 1 ]; then cp "$backup" "$save"; md5sum "$save"; else rm -f "$save"; echo "progress.json removed: $([ -f "$save" ] && echo no || echo yes)"; fi
