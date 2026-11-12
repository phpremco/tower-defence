#!/usr/bin/env bash
# Round 54: the rebuilt RuinGate and the re-imported Shattered in their real fight (BossArena -> Arena_Ruins, the boss wave,
# its own ground entrance) at fixed 1/30 s frames. At each gate moment (clip frames since it began to open; "shut" is the
# entrance before it opens) it measures what the gate hides from the play camera (r54_gate_hide.cs) at the data scale and
# at each scale in R54_SCALES, and renders stills from the play camera's angle: with the inspection light (r49_still.cs,
# light x1.8) and "_game" in the plain light. "walk" moments also frame the giant; "fold" moments hide the giant (r54_boss_hidden.cs)
# for the gate stills so the iris shows behind it. r54_decor_probe.cs lists the decor on the gate's footprint. After the last moment the Shattered's
# plates are held off as after the rejoin (r54_shells_view.cs: a BoneScaleView, one stepped frame) for one more still. Copies the save aside first,
# restores and verifies it at the end.
# Usage: r54_gate.sh <output dir> <prefix>   (env R54_BACKUP: where the save copy goes; R54_SCALES e.g. "0.58")
set -u
cd "$(dirname "$0")/../.." || exit 1
out=$1; prefix=$2
mkdir -p "$out"
save="$LOCALAPPDATA/../LocalLow/DefaultCompany/Tower defense/Saves/progress.json"
backup="${R54_BACKUP:-$out/${prefix}_progress_before.json}"
had=0; if [ -f "$save" ]; then had=1; cp "$save" "$backup"; md5sum "$save"; else echo "no progress.json before"; fi
status=$(unity command editor_status --result-only 2>&1)
if ! echo "$status" | grep -q '"playMode": *"stopped"'; then echo "NOT STOPPED - abort"; exit 1; fi
unity command clear_console --result-only >/dev/null 2>&1
unity command open_scene --path Assets/_Project/Scenes/Boot.unity --result-only >/dev/null 2>&1
unity command editor_play --result-only 2>&1 | grep -o "Entered[^\"]*"
sleep 8
step() { echo "$*" > Tools/PlayTest/frames_arg.txt; unity command eval_file --file Tools/PlayTest/step_frames.cs --result-only >/dev/null 2>&1; }
run() { unity command eval_file --file "Tools/PlayTest/$1" --result-only 2>&1 | grep -o '"result": "[^"]*\|"message": "[^"]*' | cut -c1-2400; }
still() { echo "$(cygpath -w "$out/${prefix}_$1.png")" > Tools/PlayTest/look_arg.txt; echo "  $1: $(run r49_still.cs)"; }
pair() { echo "1.8" > Tools/PlayTest/r49_light.txt; still "$1"; echo "1" > Tools/PlayTest/r49_light.txt; still "$1_game"; }
until_state() { echo "$1 0" > Tools/PlayTest/r19_until.txt; for i in $(seq 1 60); do r=$(run r19_step_until.cs); case "$r" in *reached*) echo "  $r"; return 0;; *"no boss"*) step 90 hold;; esac; done; echo "  never reached $1: $r"; }
until_gate() { echo "$1" > Tools/PlayTest/r54_until.txt; for i in $(seq 1 10); do r=$(run r54_gate_until.cs); case "$r" in *reached*) echo "  $r"; return 0;; esac; done; echo "  never reached gate frame $1: $r"; }
view() { echo "$1" > Tools/PlayTest/r53_view.txt; echo "$2" > Tools/PlayTest/zoom_size.txt; run r53_view.cs >/dev/null; step 1 hold; }
hide() { echo "$1" > Tools/PlayTest/r54_gate_scale.txt; echo "  hide: $(run r54_gate_hide.cs)"; }
step 60
echo "Shattered" > Tools/PlayTest/r14_boss.txt
run r14_arena.cs; sleep 6; step 100
run r17_fixed_step.cs >/dev/null
run hero_to_base.cs >/dev/null
echo "Shattered" > Tools/PlayTest/enemy_arg.txt
echo "-" > Tools/PlayTest/r45_face.txt
echo "-" > Tools/PlayTest/r54_gate_scale.txt
until_state Entrance
run r16_dialogue_skip.cs >/dev/null
echo "  gate: $(run r53_gate_probe.cs)"
echo "  decor: $(run r54_decor_probe.cs)"
data_scale=$(run r54_gate_hide.cs | grep -o 'gate scale [0-9.]*' | grep -o '[0-9.]*$')
echo "  data scale $data_scale"
for moment in ${R54_MOMENTS:-shut 72:opening_a 96:opening_b 135:open 165:walk_a 200:walk_b}; do
  IFS=: read -r frames label <<< "$moment"
  if [ "$moment" = shut ]; then label=shut; else until_gate "$frames"; fi
  run r16_dialogue_skip.cs >/dev/null
  hide -
  for s in ${R54_SCALES:-}; do hide "$s"; view gate 1.0; pair "gate_${label}_overview_s${s/./}"; hide "$data_scale"; done
  if [ "$label" = shut ] || [ "$label" = open ]; then view gate 1.0; pair "gate_${label}_overview"; fi
  case "$label" in fold*) echo on > Tools/PlayTest/r54_boss_hidden.txt; echo "  $(run r54_boss_hidden.cs)";; esac
  view gate 2.4; pair "gate_${label}"
  case "$label" in fold*) echo off > Tools/PlayTest/r54_boss_hidden.txt; run r54_boss_hidden.cs >/dev/null;; esac
  case "$label" in walk*) view enemy 2.4; pair "boss_${label}";; esac
done
view enemy 2.4
pair "boss_shells_on"
echo "  shells: $(run r54_shells_view.cs)"
pair "boss_shells_off"
echo "=== CONSOLE"
unity command console_status --result-only 2>&1 | grep -E '"consoleErrors"|"consoleWarnings"'
unity command console --result-only 2>&1 | grep -o '"logType": "[A-Za-z]*", *"message": "[^"]*' | grep -v "allocation" | cut -c1-300 | sort | uniq -c | sort -rn | head -10
echo "1" > Tools/PlayTest/r49_light.txt
echo "-" > Tools/PlayTest/r54_gate_scale.txt
unity command editor_stop --result-only >/dev/null 2>&1
sleep 3
if [ "$had" = 1 ]; then cp "$backup" "$save"; md5sum "$save"; else rm -f "$save"; echo "progress.json removed: $([ -f "$save" ] && echo no || echo yes)"; fi
