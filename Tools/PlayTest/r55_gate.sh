#!/usr/bin/env bash
# Round 55: the Shattered through the RuinGate in its real fight (BossArena -> Arena_Ruins, the boss wave, its own ground
# entrance) at fixed 1/30 s frames. At each gate moment in R55_MEASURE (clip frames since it began to open) it measures
# where the giant and the gate cut through each other (r55_gate_clear.cs) at the data scale and at each scale in
# R55_SCALES times each set-back in R55_BACKS (live looks; the data is not changed); with R55_PROFILE also the opening's
# profile (r55_gate_profile.cs), with R55_DECOR what else on the field cuts the giant (r55_decor_clear.cs). A "hide" moment also runs the hidden-cell probe (r54_gate_hide.cs) at each R55_SCALES.
# At each other moment in R55_STILLS ("<frame>:<label>") it renders stills from the play camera's angle on the gate
# (inspection light x1.8 and "_game"); "*overview*" labels at play zoom, "top*" labels as one top-down debug still with
# the gate's axis, the route and the camera direction drawn (r55_topdown.cs). Also prints the route (r55_route.cs).
# Copies the save aside first, restores and verifies it at the end.
# Usage: r55_gate.sh <output dir> <prefix>   (env R55_BACKUP: where the save copy goes)
set -u
cd "$(dirname "$0")/../.." || exit 1
out=$1; prefix=$2
mkdir -p "$out"
save="$LOCALAPPDATA/../LocalLow/DefaultCompany/Tower defense/Saves/progress.json"
backup="${R55_BACKUP:-$out/${prefix}_progress_before.json}"
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
until_gate() { echo "$1" > Tools/PlayTest/r54_until.txt; for i in $(seq 1 10); do r=$(run r54_gate_until.cs); case "$r" in *reached*) return 0;; esac; done; echo "  never reached gate frame $1: $r"; }
view() { echo "$1" > Tools/PlayTest/r53_view.txt; echo "$2" > Tools/PlayTest/zoom_size.txt; run r53_view.cs >/dev/null; step 1 hold; }
clear_at() { echo "$1" > Tools/PlayTest/r55_gate_scale.txt; echo "${2:--}" > Tools/PlayTest/r55_gate_setback.txt; echo "    $(run r55_gate_clear.cs)"; if [ -n "${R55_PROFILE:-}" ]; then echo "    profile: $(run r55_gate_profile.cs)"; fi; if [ -n "${R55_DECOR:-}" ]; then echo "    decor: $(run r55_decor_clear.cs)"; fi; }
step 60
echo "Shattered" > Tools/PlayTest/r14_boss.txt
run r14_arena.cs; sleep 6; step 100
run r17_fixed_step.cs >/dev/null
run hero_to_base.cs >/dev/null
echo "Shattered" > Tools/PlayTest/enemy_arg.txt
echo "-" > Tools/PlayTest/r45_face.txt
echo "-" > Tools/PlayTest/r55_gate_scale.txt; echo "-" > Tools/PlayTest/r55_gate_setback.txt
until_state Entrance
run r16_dialogue_skip.cs >/dev/null
echo "  gate: $(run r53_gate_probe.cs)"
echo "  route: $(run r55_route.cs)"
data_scale=$(run r55_gate_clear.cs | grep -o 'gate scale [0-9.]*' | grep -o '[0-9.]*$')
echo "  data scale $data_scale"
moments=$( (for m in ${R55_MEASURE:-}; do echo "$m:m"; done; for s in ${R55_STILLS:-}; do echo "$s"; done) | sort -t: -k1,1n)
for moment in $moments; do
  IFS=: read -r frames label <<< "$moment"
  until_gate "$frames"
  run r16_dialogue_skip.cs >/dev/null
  if [ "$label" = m ] || [ "$label" = hide ]; then
    echo "  frame $frames"
    clear_at -
    for s in ${R55_SCALES:-}; do for b in ${R55_BACKS:--}; do clear_at "$s" "$b"; done; done
    if [ "$label" = hide ]; then for s in ${R55_SCALES:-}; do echo "$s" > Tools/PlayTest/r54_gate_scale.txt; echo "    hide: $(run r54_gate_hide.cs)"; done; echo "-" > Tools/PlayTest/r54_gate_scale.txt; fi
    clear_at "$data_scale" "${R55_DATA_BACK:--}"
  else
    echo "  frame $frames $label"
    clear_at -
    case "$label" in
      top*) echo on > Tools/PlayTest/r55_topdown.txt; echo "    $(run r55_topdown.cs)"; echo "1" > Tools/PlayTest/r49_light.txt; still "$label"
            echo off > Tools/PlayTest/r55_topdown.txt; run r55_topdown.cs >/dev/null; view gate 2.4;;
      *overview*) view gate 1.0; pair "$label"; view gate 2.4;;
      *) view gate 2.4; pair "$label";;
    esac
  fi
done
echo "=== CONSOLE"
unity command console_status --result-only 2>&1 | grep -E '"consoleErrors"|"consoleWarnings"'
unity command console --result-only 2>&1 | grep -o '"logType": "[A-Za-z]*", *"message": "[^"]*' | grep -v "allocation" | cut -c1-300 | sort | uniq -c | sort -rn | head -10
echo "1" > Tools/PlayTest/r49_light.txt
echo "-" > Tools/PlayTest/r55_gate_scale.txt; echo "-" > Tools/PlayTest/r55_gate_setback.txt
unity command editor_stop --result-only >/dev/null 2>&1
sleep 3
if [ "$had" = 1 ]; then cp "$backup" "$save"; md5sum "$save"; else rm -f "$save"; echo "progress.json removed: $([ -f "$save" ] && echo no || echo yes)"; fi
