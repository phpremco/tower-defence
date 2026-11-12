#!/usr/bin/env bash
# Round 57: in-game stills of the round-57 imports, each lit for inspection (light x1.8) and in the plain light (_game).
# Level from Boot without waves; each spec "Enemy:half height[:ClipRole fraction]" is spawned through the level's spawner,
# walks STEPS held frames, is framed (r45_zoom.cs, time frozen) and rendered facing the camera and from 35 degrees (_side).
# Specials, by name: "SealedOneSummon" spawns the Sealed One, calls its Summon (r57_spawn_now.cs) and steps one held frame at a
# time until the clip's moment threw its sparks (r57_moment_probe.cs), then renders with the camera orbited to its face, so
# the sparks stay where the game put them; "FireLarvaFeet" logs FEET held frames of the crawl (r57_feet.cs) into
# <out>/<prefix>_FireLarva_feet.txt before its stills. Copies the save aside first, restores and verifies it at the end.
# Usage: [STEPS=n] r57_creatures.sh <output dir> <prefix> <level index> <spec> ...
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
run() { unity command eval_file --file "Tools/PlayTest/$1" --result-only 2>&1 | grep -o '"result": "[^"]*\|"message": "[^"]*' | cut -c1-600; }
render() { echo "$2" > Tools/PlayTest/r49_light.txt; echo "$(cygpath -w "$out/${prefix}_$1.png")" > Tools/PlayTest/look_arg.txt; echo "  $1: $(run r50_still.cs)"; }
both() { render "$1" 1.8; render "${1}_game" 1; }
step 60; run intro_skip.cs >/dev/null
echo "$level" > Tools/PlayTest/level_arg.txt; run start_level.cs; sleep 6; step 100; step 100
run no_waves.cs >/dev/null
run r16_dialogue_skip.cs >/dev/null
run hero_to_base.cs >/dev/null; step 20
for spec in "$@"; do
  IFS=: read -r name zoom pose <<< "$spec"; zoom=${zoom:-2.2}
  enemy=$name
  case "$name" in SealedOneSummon) enemy=SealedOne ;; FireLarvaFeet) enemy=FireLarva ;; esac
  echo "== $name"
  echo "$enemy" > Tools/PlayTest/enemy_arg.txt
  echo "  $(run spawn_enemy.cs)"; step "${STEPS:-90} hold"
  run r16_dialogue_skip.cs >/dev/null
  echo "$zoom" > Tools/PlayTest/zoom_size.txt
  echo "0" > Tools/PlayTest/r50_orbit.txt
  if [ "$name" = SealedOneSummon ]; then
    # A boss in a scripted moment (its rise, a climb) shows no power clip: prime again until the Summon runs.
    for try in 1 2 3 4 5 6; do
      echo "  try $try: $(run r57_spawn_now.cs)"
      for i in $(seq 1 90); do
        step "1 hold"; m=$(run r57_moment_probe.cs)
        if echo "$m" | grep -q "alive [1-9]"; then break 2; fi
      done
    done
    echo "  moment: $m"
    step "2 hold"; echo "  +2: $(run r57_moment_probe.cs)"
    echo "-" > Tools/PlayTest/r45_face.txt
    echo "  $(run r45_zoom.cs)"
    echo "$enemy 0 ${ORBIT_HEIGHT:-3}" > Tools/PlayTest/enemy_arg.txt; echo "  $(run r57_orbit_face.cs)"
    both "SealedOne_summon"
    echo "$enemy 35 ${ORBIT_HEIGHT:-3}" > Tools/PlayTest/enemy_arg.txt; run r57_orbit_face.cs >/dev/null; render "SealedOne_summon_side" 1.8
    echo "0" > Tools/PlayTest/r50_orbit.txt
    run resume.cs >/dev/null; run kill_all.cs >/dev/null; step 20
    continue
  fi
  if [ "$name" = FireLarvaFeet ]; then
    : > "$out/${prefix}_FireLarva_feet.txt"
    for i in $(seq 1 "${FEET:-90}"); do step "1 hold"; run r57_feet.cs | sed 's/^"result": "//' >> "$out/${prefix}_FireLarva_feet.txt"; done
    name=FireLarva
  fi
  echo "-" > Tools/PlayTest/r45_face.txt
  echo "  $(run r45_zoom.cs)"; step 2
  if [ -n "$pose" ]; then echo "$enemy $pose" > Tools/PlayTest/r45_pose.txt; echo "  $(run r45_pose.cs)"; step 1; fi
  echo "$enemy 0" > Tools/PlayTest/r45_face.txt; both "$name"
  echo "$enemy 35" > Tools/PlayTest/r45_face.txt; render "${name}_side" 1.8
  if [ -n "$pose" ]; then echo "$enemy off" > Tools/PlayTest/r45_pose.txt; run r45_pose.cs >/dev/null; fi
  echo "-" > Tools/PlayTest/r45_face.txt
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
