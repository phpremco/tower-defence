#!/usr/bin/env bash
# Round 49 smoke material fix: in-game stills of the code-built smoke, as r49_shots.sh (Level from Boot without waves, the
# inspection light of r49_still.cs). A probe tower knocked down to 20% health smokes and sparks; the hero is knocked down and
# smokes where it lies. r49_particle_probe.cs lists every particle renderer with its material (flagging the URP default) at
# each stage. Copies the save aside first, restores and verifies it at the end.
# Usage: [TOWER=name] [LIGHT=f] r49_smoke_shots.sh <output dir> <prefix> <level index>
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
run() { unity command eval_file --file "Tools/PlayTest/$1" --result-only 2>&1 | grep -o '"result": "[^"]*\|"message": "[^"]*' | cut -c1-3000; }
still() { echo "$2" > Tools/PlayTest/r49_light.txt; echo "$(cygpath -w "$out/${prefix}_$1.png")" > Tools/PlayTest/look_arg.txt; echo "  $1: $(run r49_still.cs)"; }
light=${LIGHT:-1.8}
step 60
echo "$level" > Tools/PlayTest/level_arg.txt; run start_level.cs; sleep 6; step 100; step 100
run no_waves.cs >/dev/null
run r16_dialogue_skip.cs >/dev/null
echo "=== LEVEL PARTICLES"; run r49_particle_probe.cs
echo "${TOWER:-Cannon}" > Tools/PlayTest/tower_arg.txt
echo "1.6" > Tools/PlayTest/zoom_size.txt
echo "=== TOWER"; run r49_smoke_tower.cs; step 150
run r49_particle_probe.cs | tr '|' '\n' | grep -i "probetower"
still TowerDamageSmoke "$light"; still TowerDamageSmoke_game 1
echo "=== HERO"; run hero_down.cs; step 120
echo "1.4" > Tools/PlayTest/zoom_size.txt
run r49_zoom_hero.cs; step 60
run r49_particle_probe.cs | tr '|' '\n' | grep -iE "^ *!!|smoke|spark|dust" | head -40
still HeroDownSmoke "$light"; still HeroDownSmoke_game 1
echo "1" > Tools/PlayTest/r49_light.txt
echo "=== CONSOLE"
unity command console_status --result-only 2>&1 | grep -E '"consoleErrors"|"consoleWarnings"'
unity command console --result-only 2>&1 | grep -o '"logType": "[A-Za-z]*", *"message": "[^"]*' | grep -v "allocation" | cut -c1-300 | sort | uniq -c | sort -rn | head -10
unity command editor_stop --result-only >/dev/null 2>&1
sleep 3
if [ "$had" = 1 ]; then cp "$backup" "$save"; md5sum "$save"; else rm -f "$save"; echo "progress.json removed: $([ -f "$save" ] && echo no || echo yes)"; fi
