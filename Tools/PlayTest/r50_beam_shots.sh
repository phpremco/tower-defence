#!/usr/bin/env bash
# Round 50: stills of boss beams and volleys leaving their bones (r49_shots.sh flow: level from Boot without waves, the hero
# sent home, one probe tower beside the road near the spawn as a target). Per creature: spawn, walk STEPS frames, prime its
# power (laser_now.cs or volley_now.cs) or pose it (ClipRole), step FIRE frames, run r50_beam_probe.cs, zoom, and render
# with r50_still.cs: "<name>" lit for inspection, "<name>_game" in the game light, "<name>_side" from a camera orbited by
# SIDE ("<yaw> <pitch> <height>", default "70 22 1.2") so the beam keeps its place on the animated body. Copies the save
# aside first, restores and verifies it at the end.
# Usage: [STEPS=n] [FIRE=n] [TOWER=name] r50_beam_shots.sh <output dir> <prefix> <level index> <Enemy:half height:laser|volley|ClipRole> ...
set -u
cd "$(dirname "$0")/../.." || exit 1
out=$1; prefix=$2; level=$3; shift 3
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
step 60
echo "$level" > Tools/PlayTest/level_arg.txt; run start_level.cs; sleep 6; step 100; step 100
run no_waves.cs >/dev/null
run r16_dialogue_skip.cs >/dev/null
run hero_to_base.cs >/dev/null; step 20
echo "${TOWER:-PulseRifle}" > Tools/PlayTest/tower_arg.txt; echo "  $(run build_tower.cs)"
echo "-" > Tools/PlayTest/r45_face.txt
for spec in "$@"; do
  IFS=: read -r name zoom mode <<< "$spec"; zoom=${zoom:-3}
  echo "$name" > Tools/PlayTest/enemy_arg.txt
  echo "$(run spawn_enemy.cs)"; step "${STEPS:-60}"
  case "$mode" in
    laser) echo "${LEAD:-1.5}" > Tools/PlayTest/r50_laser_in.txt; echo "  $(run r50_laser_in.cs)"; step "${FIRE:-120}";;
    volley) echo "  $(run volley_now.cs)"; step "${FIRE:-12}";;
    strike|hover)
      # Walk on until the flyer holds its place at a gate: mid-strike (its attack clip on top) or between strikes (none on top).
      last=""
      for i in $(seq 1 ${WAIT:-60}); do
        step "${CHUNK:-10} hold"; state=$(run r50_clip_state.cs)
        if [ "$mode" = strike ] && echo "$state" | grep -qE "once=(Strike|Dive)@0.${WINDOW:-[2-4]}"; then break; fi
        if [ "$mode" = hover ] && echo "$state" | grep -q "once=none" && [ "$(echo "$state" | grep -o "pos ([^)]*)")" = "$(echo "${last:-}" | grep -o "pos ([^)]*)")" ]; then break; fi
        last=$state
      done
      echo "  state: $state";;
    *) ;;
  esac
  run r16_dialogue_skip.cs >/dev/null
  echo "$zoom" > Tools/PlayTest/zoom_size.txt
  echo "  $(run r45_zoom.cs)"; step 2
  if [ -n "$mode" ] && [ "$mode" != laser ] && [ "$mode" != volley ] && [ "$mode" != strike ] && [ "$mode" != hover ]; then echo "$name $mode" > Tools/PlayTest/r45_pose.txt; echo "  $(run r45_pose.cs)"; step 1; fi
  echo "  probe: $(run r50_beam_probe.cs)"
  echo "  clips: $(run r50_clip_state.cs)"
  still "$name" "${LIGHT:-1.8}" "0"; still "${name}_game" 1 "0"; still "${name}_side" "${LIGHT:-1.8}" "${SIDE:-70 22 1.2}"
  if [ -n "$mode" ] && [ "$mode" != laser ] && [ "$mode" != volley ] && [ "$mode" != strike ] && [ "$mode" != hover ]; then echo "$name off" > Tools/PlayTest/r45_pose.txt; run r45_pose.cs >/dev/null; fi
  run resume.cs >/dev/null
  run kill_all.cs >/dev/null; step 20
done
echo "1" > Tools/PlayTest/r49_light.txt; echo "0" > Tools/PlayTest/r50_orbit.txt
echo "=== CONSOLE"
unity command console_status --result-only 2>&1 | grep -E '"consoleErrors"|"consoleWarnings"'
unity command console --result-only 2>&1 | grep -o '"logType": "[A-Za-z]*", *"message": "[^"]*' | grep -v "allocation" | cut -c1-300 | sort | uniq -c | sort -rn | head -10
unity command editor_stop --result-only >/dev/null 2>&1
sleep 3
if [ "$had" = 1 ]; then cp "$backup" "$save"; md5sum "$save"; else rm -f "$save"; echo "progress.json removed: $([ -f "$save" ] && echo no || echo yes)"; fi
