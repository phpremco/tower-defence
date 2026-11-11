#!/usr/bin/env bash
# Round 42 (copy of r39_play.sh with an audio log): a full played run of the opening cutscene (no seeks): every dialogue is confirmed with the keyboard's Enter
# through the Input System, except the gate stand's, which is held open for HOLD real seconds while the brood is probed
# and shot; then it is confirmed and the handoff (roar, retreat, burrows closing) plays into the first level.
# Usage: OUT=<dir> HOLD=<s> bash Tools/PlayTest/r39_play.sh. Restores the save afterwards.
set -u
cd "$(dirname "$0")/../.." || exit 1
out=${OUT:-Logs/r42}; hold=${HOLD:-20}
mkdir -p "$out"; rm -f "$out/r42_audio.txt"
save="/c/Users/remco/AppData/LocalLow/DefaultCompany/Tower defense/Saves/progress.json"
backup="$out/progress_backup.json"
status=$(unity command editor_status --result-only 2>&1)
if ! echo "$status" | grep -q '"playMode": *"stopped"'; then echo "NOT STOPPED - abort"; exit 1; fi
cp "$save" "$backup"; echo "save before $(md5sum "$save" | cut -c1-32)"
rm -f Tools/PlayTest/cut_speed.txt Tools/PlayTest/cut_auto.txt
unity command clear_console --result-only >/dev/null 2>&1
unity command open_scene --path Assets/_Project/Scenes/Boot.unity --result-only >/dev/null 2>&1
unity command editor_play --result-only 2>&1 | grep -o "Entered[^\"]*"
sleep 8
run() { unity command eval_file --file "Tools/PlayTest/$1" --result-only 2>&1 | grep -o '"result": "[^"]*' | cut -c12-3000; }
press() { unity command simulate_key --key Enter --action down --result-only >/dev/null 2>&1; sleep 0.15; unity command simulate_key --key Enter --action up --result-only >/dev/null 2>&1; }
run cutscene_start.cs
held=0; started=$(date +%s); last=""
while [ $(( $(date +%s) - started )) -lt 600 ]; do
  sleep 1
  state=$(run r39_swarm.cs); echo "$(( $(date +%s) - started ))s $(run r42_audio_probe.cs)" >> "$out/r42_audio.txt"
  [ "$state" != "$last" ] && echo "$(( $(date +%s) - started ))s $state"; last="$state"
  case "$state" in
    "no cutscene player"*) echo "handed off: $state"; break ;;
  esac
  if echo "$state" | grep -q "GateStand" && echo "$state" | grep -qv "dialogue=none" && [ "$held" -eq 0 ]; then
    echo "holding the gate line for ${hold}s"
    sleep $(( hold / 2 )); echo "mid-hold: $(run r39_swarm.cs)"
    echo "$out/r39_play_hold.png" > Tools/PlayTest/look_arg.txt; run n2_render.cs
    sleep $(( hold - hold / 2 )); echo "end of hold: $(run r39_swarm.cs)"
    held=1
  fi
  if echo "$state" | grep -qv "dialogue=none"; then press; fi
  if echo "$state" | grep -q "Handoff t=1\.[0-9]" && [ ! -f "$out/r39_play_retreat.png" ]; then echo "$out/r39_play_retreat.png" > Tools/PlayTest/look_arg.txt; run n2_render.cs; fi
done
sleep 6; echo "after handoff: $(run r42_audio_probe.cs)" >> "$out/r42_audio.txt"; sleep 4; echo "after handoff +4s: $(run r42_audio_probe.cs)" >> "$out/r42_audio.txt"
echo "scene: $(run cutscene_probe.cs)"
echo "=== CONSOLE"
unity command console_status --result-only 2>&1 | grep -E "consoleErrors|consoleWarnings"
unity command console --result-only 2>&1 | grep -o '"logType": "[A-Za-z]*", *"message": "[^"]*' | grep -v "allocation" | sort | uniq -c | sort -rn | head -8
unity command editor_stop --result-only >/dev/null 2>&1
sleep 3
cp "$backup" "$save"; echo "save after restore $(md5sum "$save" | cut -c1-32)"
