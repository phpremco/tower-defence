#!/usr/bin/env bash
# Round 53: the Shattered's real entrance (BossArena -> Arena_Ruins, its wave, its own ground entrance) at fixed 1/30 s
# frames: stills of the ruin gate from the play camera's angle at its moments (shut, seal cracking, the break-out, open)
# and, once the giant is on the road, the bar against its crest with the body turned to four headings. Copies the save
# aside first, restores and verifies it at the end.
# Usage: r53_headings.sh <output dir> <prefix>   (env R53_BACKUP: where the save copy goes)
set -u
cd "$(dirname "$0")/../.." || exit 1
out=$1; prefix=$2
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
step() { echo "$*" > Tools/PlayTest/frames_arg.txt; unity command eval_file --file Tools/PlayTest/step_frames.cs --result-only >/dev/null 2>&1; }
run() { unity command eval_file --file "Tools/PlayTest/$1" --result-only 2>&1 | grep -o '"result": "[^"]*\|"message": "[^"]*' | cut -c1-1600; }
still() { echo "$(cygpath -w "$out/${prefix}_$1.png")" > Tools/PlayTest/look_arg.txt; echo "  $1: $(run r45_still.cs)"; }
until_state() { echo "$1 0" > Tools/PlayTest/r19_until.txt; for i in $(seq 1 60); do r=$(run r19_step_until.cs); case "$r" in *reached*) echo "  $r"; return 0;; *"no boss"*) step 90 hold;; esac; done; echo "  never reached $1: $r"; }
gate_still() { echo "gate" > Tools/PlayTest/r53_view.txt; echo "$2" > Tools/PlayTest/zoom_size.txt; run r53_view.cs >/dev/null; step 1 hold; still "$1"; }
step 60
echo "Shattered" > Tools/PlayTest/r14_boss.txt
run r14_arena.cs; sleep 6; step 100
run r17_fixed_step.cs >/dev/null
run hero_to_base.cs >/dev/null
echo "Shattered" > Tools/PlayTest/enemy_arg.txt
echo "-" > Tools/PlayTest/r45_face.txt
until_state Entrance
run r16_dialogue_skip.cs >/dev/null
echo "  gate: $(run r53_gate_probe.cs)"
gate_still gate_overview 1.3
gate_still gate_shut 2.4
for moment in 32:seal 30:breakout 60:opening 70:open; do
  IFS=: read -r frames label <<< "$moment"
  step "$frames" hold; run r16_dialogue_skip.cs >/dev/null
  gate_still "gate_${label}" 2.4
done
until_state Road
step 60 hold; run r16_dialogue_skip.cs >/dev/null
echo "  crest: $(run r53_crest_probe.cs)"
echo "enemy" > Tools/PlayTest/r53_view.txt; echo "2.4" > Tools/PlayTest/zoom_size.txt
for yaw in walk 0 90 180 270; do
  run r53_view.cs >/dev/null
  if [ "$yaw" = walk ]; then echo "-" > Tools/PlayTest/r45_face.txt; else echo "Shattered $yaw" > Tools/PlayTest/r45_face.txt; fi
  step 1 hold; still "bar_${yaw}"
done
echo "-" > Tools/PlayTest/r45_face.txt
echo "=== CONSOLE"
unity command console_status --result-only 2>&1 | grep -E '"consoleErrors"|"consoleWarnings"'
unity command console --result-only 2>&1 | grep -o '"logType": "[A-Za-z]*", *"message": "[^"]*' | grep -v "allocation" | cut -c1-300 | sort | uniq -c | sort -rn | head -10
unity command editor_stop --result-only >/dev/null 2>&1
sleep 3
if [ "$had" = 1 ]; then cp "$backup" "$save"; md5sum "$save"; else rm -f "$save"; echo "progress.json removed: $([ -f "$save" ] && echo no || echo yes)"; fi
