#!/usr/bin/env bash
# Round 49: in-game stills of the redesigned creatures, as r45_boss_shots.sh (level from Boot without waves, spawn through
# the spawner, walk STEPS frames, freeze, zoom onto the drawn body, 1920x1080 off-screen renders facing the camera and
# turned 35 degrees), but lit for judging the model: LIGHT (default 1.8) raises the theme's directional lights and ambient
# and adds a soft camera-side key for the render only (r49_still.cs); "<name>_game" is the same view in the plain game
# light. A fourth field "death" then kills the creature and renders the corpse a moment after the death and later (vents
# fading), with r49_vent_probe.cs reporting the vent emitters before, during and after. Copies the save aside first,
# restores and verifies it at the end.
# PROBE=<file.cs> (optional) runs one more eval probe from Tools/PlayTest per creature, after the zoom and pose.
# Usage: [STEPS=n] [LIGHT=f] [PROBE=f] r49_shots.sh <output dir> <prefix> <level index> <Enemy[:half height[:ClipRole[:death]]]> ...
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
run() { unity command eval_file --file "Tools/PlayTest/$1" --result-only 2>&1 | grep -o '"result": "[^"]*\|"message": "[^"]*' | cut -c1-900; }
still() { echo "$2" > Tools/PlayTest/r49_light.txt; echo "$(cygpath -w "$out/${prefix}_$1.png")" > Tools/PlayTest/look_arg.txt; echo "  $1: $(run r49_still.cs)"; }
light=${LIGHT:-1.8}
step 60
echo "$level" > Tools/PlayTest/level_arg.txt; run start_level.cs; sleep 6; step 100; step 100
run no_waves.cs >/dev/null
run r16_dialogue_skip.cs >/dev/null
run hero_to_base.cs >/dev/null; step 20
for spec in "$@"; do
  IFS=: read -r name zoom pose death <<< "$spec"; zoom=${zoom:-2.2}
  echo "$name" > Tools/PlayTest/enemy_arg.txt
  echo "$(run spawn_enemy.cs)"; step "${STEPS:-90}"
  run r16_dialogue_skip.cs >/dev/null
  echo "$zoom" > Tools/PlayTest/zoom_size.txt
  echo "  $(run r45_zoom.cs)"; step 2
  if [ -n "$pose" ]; then echo "$name $pose" > Tools/PlayTest/r45_pose.txt; echo "  $(run r45_pose.cs)"; step 1; fi
  echo "  vents: $(run r49_vent_probe.cs)"
  if [ -n "${PROBE:-}" ]; then echo "  probe: $(run "$PROBE")"; fi
  echo "$name 0" > Tools/PlayTest/r45_face.txt; still "$name" "$light"; still "${name}_game" 1
  echo "$name 35" > Tools/PlayTest/r45_face.txt; still "${name}_side" "$light"
  if [ -n "$pose" ]; then echo "$name off" > Tools/PlayTest/r45_pose.txt; run r45_pose.cs >/dev/null; fi
  echo "-" > Tools/PlayTest/r45_face.txt
  run resume.cs >/dev/null
  if [ -n "$death" ]; then
    # Held steps: game time moves only in steps here, so the death is followed frame by frame.
    step "1 hold"; run kill_all.cs >/dev/null; step "${DEATH_A:-40} hold"
    echo "  corpses: $(run r49_corpse_probe.cs)"
    echo "  vents at death: $(run r49_vent_probe.cs)"; still "${name}_death" "$light"
    step "${DEATH_B:-120} hold"; echo "  corpses: $(run r49_corpse_probe.cs)"; echo "  vents later: $(run r49_vent_probe.cs)"; still "${name}_death_late" "$light"
    step "${DEATH_C:-200} hold"; echo "  corpses: $(run r49_corpse_probe.cs)"; echo "  vents gone: $(run r49_vent_probe.cs)"
    step 1
  fi
  run kill_all.cs >/dev/null; step 20
done
echo "1" > Tools/PlayTest/r49_light.txt
echo "=== CONSOLE"
unity command console_status --result-only 2>&1 | grep -E '"consoleErrors"|"consoleWarnings"'
unity command console --result-only 2>&1 | grep -o '"logType": "[A-Za-z]*", *"message": "[^"]*' | grep -v "allocation" | cut -c1-300 | sort | uniq -c | sort -rn | head -10
unity command editor_stop --result-only >/dev/null 2>&1
sleep 3
if [ "$had" = 1 ]; then cp "$backup" "$save"; md5sum "$save"; else rm -f "$save"; echo "progress.json removed: $([ -f "$save" ] && echo no || echo yes)"; fi
