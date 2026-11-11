#!/usr/bin/env bash
# Round 41: the levelled ground under the colony rig. Starts the opening, probes the drill's deck against the ground
# (r39_drill_probe.cs), renders the core shot, then stops and restores the save.
set -u
cd "$(dirname "$0")/../.." || exit 1
out=${OUT:-Logs}
save="/c/Users/remco/AppData/LocalLow/DefaultCompany/Tower defense/Saves/progress.json"
backup="$out/progress_backup.json"
status=$(unity command editor_status --result-only 2>&1)
if ! echo "$status" | grep -q '"playMode": *"stopped"'; then echo "NOT STOPPED - abort"; exit 1; fi
cp "$save" "$backup"; echo "save before $(md5sum "$save" | cut -c1-32)"
unity command clear_console --result-only >/dev/null 2>&1
unity command open_scene --path Assets/_Project/Scenes/Boot.unity --result-only >/dev/null 2>&1
unity command editor_play --result-only 2>&1 | grep -o "Entered[^\"]*"
sleep 8
run() { unity command eval_file --file "Tools/PlayTest/$1" --result-only 2>&1 | grep -o '"result": "[^"]*' | cut -c12-3000; }
run cutscene_start.cs
sleep 3
run r39_drill_probe.cs
echo "$out/r41_drill_core.png" > Tools/PlayTest/look_arg.txt; run n2_render.cs
echo "=== CONSOLE"
unity command console_status --result-only 2>&1 | grep -E "consoleErrors|consoleWarnings"
unity command editor_stop --result-only >/dev/null 2>&1
sleep 3
cp "$backup" "$save"; echo "save after restore $(md5sum "$save" | cut -c1-32)"
