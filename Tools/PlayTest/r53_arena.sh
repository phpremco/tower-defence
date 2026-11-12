#!/usr/bin/env bash
# Round 53: the Shattered in its real fight (BossArena -> Arena_Ruins). Loads the arena from Boot, stops the wave timeline,
# brings the boss in through the level's entrance as its wave does (spawn_enemy.cs "Shattered Entrance"), and at each
# moment in r53_moments (frames:label) probes the gate's footprint and the bar against the crest and renders stills from
# the play camera's angle (r53_view.cs on the gate or the boss, then r45_still.cs). Copies the save aside first, restores and
# verifies it at the end.
# Usage: r53_arena.sh <output dir> <prefix> [zoom]
set -u
cd "$(dirname "$0")/../.." || exit 1
out=$1; prefix=$2; zoom=${3:-2.4}
mkdir -p "$out"
save="$LOCALAPPDATA/../LocalLow/DefaultCompany/Tower defense/Saves/progress.json"
backup="${R53_BACKUP:-$out/${prefix}_progress_before.json}"
had=0; if [ -f "$save" ]; then had=1; cp "$save" "$backup"; md5sum "$save"; else echo "no progress.json before"; fi
status=$(unity command editor_status --result-only 2>&1)
if ! echo "$status" | grep -q '"playMode": *"stopped"'; then echo "NOT STOPPED - abort"; exit 1; fi
unity command clear_console --result-only >/dev/null 2>&1
unity command open_scene --path Assets/_Project/Scenes/Boot.unity --result-only >/dev/null 2>&1
unity command editor_play --result-only 2>&1 | grep -o "Entered[^\"]*"
sleep 8
step() { echo "$1" > Tools/PlayTest/frames_arg.txt; unity command eval_file --file Tools/PlayTest/step_frames.cs --result-only >/dev/null 2>&1; }
run() { unity command eval_file --file "Tools/PlayTest/$1" --result-only 2>&1 | grep -o '"result": "[^"]*\|"message": "[^"]*' | cut -c1-1600; }
still() { echo "$(cygpath -w "$out/${prefix}_$1.png")" > Tools/PlayTest/look_arg.txt; echo "  $1: $(run r45_still.cs)"; }
step 60
echo "Shattered" > Tools/PlayTest/r14_boss.txt
run r14_arena.cs; sleep 6
echo "Shattered Entrance" > Tools/PlayTest/enemy_arg.txt
echo "$zoom" > Tools/PlayTest/zoom_size.txt
echo "-" > Tools/PlayTest/r45_face.txt
if [ -n "${SPAWN:-}" ]; then step 200; run no_waves.cs >/dev/null; run spawn_enemy.cs; fi
for moment in ${R53_MOMENTS:-4:shut 70:opening 160:open 420:walk}; do
  IFS=: read -r frames label <<< "$moment"
  step "$frames"
  run r16_dialogue_skip.cs >/dev/null
  echo "  gate: $(run r53_gate_probe.cs)"
  echo "  crest: $(run r53_crest_probe.cs)"
  echo "  pose: $(run r53_pose_compare.cs)"
  echo "gate" > Tools/PlayTest/r53_view.txt; echo "  $(run r53_view.cs)"; step 1 hold; still "gate_${label}"
  echo "enemy" > Tools/PlayTest/r53_view.txt; echo "  $(run r53_view.cs)"; step 1 hold; still "boss_${label}"
done
echo "=== CONSOLE"
unity command console_status --result-only 2>&1 | grep -E '"consoleErrors"|"consoleWarnings"'
unity command console --result-only 2>&1 | grep -o '"logType": "[A-Za-z]*", *"message": "[^"]*' | grep -v "allocation" | cut -c1-300 | sort | uniq -c | sort -rn | head -10
unity command editor_stop --result-only >/dev/null 2>&1
sleep 3
if [ "$had" = 1 ]; then cp "$backup" "$save"; md5sum "$save"; else rm -f "$save"; echo "progress.json removed: $([ -f "$save" ] && echo no || echo yes)"; fi
