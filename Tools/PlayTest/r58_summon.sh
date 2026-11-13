#!/usr/bin/env bash
# Round 58 (from r57_creatures.sh's SealedOneSummon): spawns the Sealed One on the level, lets it walk STEPS held frames,
# primes its Summon (r57_spawn_now.cs) and steps one held frame at a time through the clip, logging r58_bar_probe.cs (bar
# height and the bar's screen row against the halo's) every third frame into <out>/<prefix>_summon_bar.txt; from the clip's
# moment (sparks alive, r57_moment_probe.cs) probes PAST more frames (the widest flare), then renders from the PLAY camera
# (zoomed, not orbited) lit and plain; the frames after the stills are frozen by the zoom.
# Copies the save aside first, restores and verifies it at the end.
# Usage: [STEPS=n] r58_summon.sh <output dir> <prefix> <level index>
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
run() { unity command eval_file --file "Tools/PlayTest/$1" --result-only 2>&1 | grep -o '"result": "[^"]*\|"message": "[^"]*' | cut -c1-600; }
render() { echo "$2" > Tools/PlayTest/r49_light.txt; echo "$(cygpath -w "$out/${prefix}_$1.png")" > Tools/PlayTest/look_arg.txt; echo "  $1: $(run r50_still.cs)"; }
step 60; run intro_skip.cs >/dev/null
echo "$level" > Tools/PlayTest/level_arg.txt; run start_level.cs; sleep 6; step 100; step 100
run no_waves.cs >/dev/null
run r16_dialogue_skip.cs >/dev/null
run hero_to_base.cs >/dev/null; step 20
echo "SealedOne" > Tools/PlayTest/enemy_arg.txt
echo "  $(run spawn_enemy.cs)"; step "${STEPS:-90} hold"
run r16_dialogue_skip.cs >/dev/null
# Its rise out of the rift is a scripted clip: wait until it walks.
for i in $(seq 1 30); do w=$(run r58_bar_probe.cs); echo "$w" | grep -q "once=Rise" || break; step "30 hold"; done
echo "  walking: $w"
log="$out/${prefix}_summon_bar.txt"; : > "$log"
m=""
for try in 1 2 3; do
  echo "  try $try: $(run r57_spawn_now.cs)"
  for i in $(seq 1 90); do
    step "1 hold"; if [ $((i % 3)) = 0 ]; then run r58_bar_probe.cs | sed 's/^"result": "//' >> "$log"; fi; m=$(run r57_moment_probe.cs)
    if echo "$m" | grep -q "alive [1-9]"; then break 2; fi
  done
done
echo "  moment: $m"
echo "  at moment: $(run r58_bar_probe.cs)"
# On to the halo's widest flare (the offline measure puts it at 1.55 s, just after the moment), probing every frame.
for i in $(seq 1 ${PAST:-8}); do step "1 hold"; echo "  +$i: $(run r58_bar_probe.cs)"; done
echo "${ZOOM:-7}" > Tools/PlayTest/zoom_size.txt; echo "0" > Tools/PlayTest/r50_orbit.txt; echo "-" > Tools/PlayTest/r45_face.txt
echo "  $(run r45_zoom.cs)"
render "SealedOne_summon" 1.8; render "SealedOne_summon_game" 1
run resume.cs >/dev/null
echo "1" > Tools/PlayTest/r49_light.txt; echo "0" > Tools/PlayTest/r50_orbit.txt
echo "=== CONSOLE"
unity command console_status --result-only 2>&1 | grep -E '"consoleErrors"|"consoleWarnings"'
unity command console --result-only 2>&1 | grep -o '"logType": "[A-Za-z]*", *"message": "[^"]*' | grep -v "allocation" | cut -c1-300 | sort | uniq -c | sort -rn | head -10
unity command editor_stop --result-only >/dev/null 2>&1
sleep 3
if [ "$had" = 1 ]; then cp "$backup" "$save"; md5sum "$save"; else rm -f "$save"; echo "progress.json removed: $([ -f "$save" ] && echo no || echo yes)"; fi
