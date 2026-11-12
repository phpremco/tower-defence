#!/usr/bin/env bash
# Round 45: boss close-ups in game lighting. Plays a level from Boot without waves, spawns each named boss through the
# level's spawner, lets it walk STEPS frames, freezes time, zooms the play camera onto it (ortho half height per boss)
# and renders 1920x1080 stills off-screen (r45_still.cs, no screen-space interface): turned to face the camera, and
# FAR=1 adds one at the play zoom as it walks. Copies the save aside first, restores and verifies it at the end.
# The zoom centres on the drawn body (r45_zoom.cs), so a flyer is framed, not its shadow; a third field poses the boss in
# that clip role for the stills (Idle uncurls a roller; r45_pose.cs holds the rig in it for one stepped frame). FAR=1 needs zoom_enemy.cs's play-zoom framing first.
# Usage: [STEPS=n] [FAR=1] r45_boss_shots.sh <output dir> <prefix> <level index> <Enemy[:half height[:ClipRole]]> ...
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
run() { unity command eval_file --file "Tools/PlayTest/$1" --result-only 2>&1 | grep -o '"result": "[^"]*\|"message": "[^"]*' | cut -c1-400; }
still() { echo "$(cygpath -w "$out/${prefix}_$1.png")" > Tools/PlayTest/look_arg.txt; echo "  $1: $(run r45_still.cs)"; }
step 60; run intro_skip.cs >/dev/null
echo "$level" > Tools/PlayTest/level_arg.txt; run start_level.cs; sleep 6; step 100; step 100
run no_waves.cs >/dev/null
run r16_dialogue_skip.cs >/dev/null
run hero_to_base.cs >/dev/null; step 20
for spec in "$@"; do
  IFS=: read -r name zoom pose <<< "$spec"; zoom=${zoom:-2.2}
  echo "$name" > Tools/PlayTest/enemy_arg.txt
  echo "$(run spawn_enemy.cs)"; step "${STEPS:-90}"
  run r16_dialogue_skip.cs >/dev/null
  echo "$zoom" > Tools/PlayTest/zoom_size.txt
  if [ -n "${FAR:-}" ]; then echo "-" > Tools/PlayTest/r45_face.txt; echo "  $(run zoom_enemy.cs)"; step 2; still "${name}_walk"; fi
  echo "  $(run r45_zoom.cs)"; step 2
  if [ -n "$pose" ]; then echo "$name $pose" > Tools/PlayTest/r45_pose.txt; echo "  $(run r45_pose.cs)"; step 1; fi
  echo "$name 0" > Tools/PlayTest/r45_face.txt; still "$name"
  echo "$name 35" > Tools/PlayTest/r45_face.txt; still "${name}_side"
  if [ -n "$pose" ]; then echo "$name off" > Tools/PlayTest/r45_pose.txt; run r45_pose.cs >/dev/null; fi
  echo "-" > Tools/PlayTest/r45_face.txt
  run resume.cs >/dev/null
  run kill_all.cs >/dev/null; step 20
done
echo "=== CONSOLE"
unity command console_status --result-only 2>&1 | grep -E '"consoleErrors"|"consoleWarnings"'
unity command console --result-only 2>&1 | grep -o '"logType": "[A-Za-z]*", *"message": "[^"]*' | grep -v "allocation" | cut -c1-300 | sort | uniq -c | sort -rn | head -10
unity command editor_stop --result-only >/dev/null 2>&1
sleep 3
if [ "$had" = 1 ]; then cp "$backup" "$save"; md5sum "$save"; else rm -f "$save"; echo "progress.json removed: $([ -f "$save" ] && echo no || echo yes)"; fi
