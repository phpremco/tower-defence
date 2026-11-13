#!/usr/bin/env bash
# Round 61: the air-defence report (AirDefenceProbeTests) in the open editor, one run at a time. Backs the save up, plays, restores
# and verifies it. Usage: bash Tools/PlayTest/r61_air_probe.sh <label> [filter lines...]   (default: levels 2,3 Easy+Medium, seeds 1-3)
# The report is copied to Tools/PlayTest/r61_air_defence_<label>.txt.
set -uo pipefail
cd "$(dirname "$0")/../.."
label="${1:-run}"; shift || true
SAVE="$LOCALAPPDATA/../LocalLow/DefaultCompany/Tower defense/Saves/progress.json"
BACKUP="$(mktemp)"
before="$(md5sum < "$SAVE" | cut -d' ' -f1)"
cp "$SAVE" "$BACKUP"
echo "save md5 before: $before"
restore() {
  cp "$BACKUP" "$SAVE"
  after="$(md5sum < "$SAVE" | cut -d' ' -f1)"
  echo "save md5 after restore: $after $([ "$after" = "$before" ] && echo OK || echo MISMATCH)"
  rm -f Tools/PlayTest/probe_arg.txt
}
trap restore EXIT

if ! unity command editor_status 2>&1 | grep -q '"playMode":"stopped"'; then echo "Play Mode is not stopped: abort" >&2; exit 1; fi
if unity command console_status --result-only 2>&1 | grep -q '"compilationFailed": true'; then echo "compile errors: abort" >&2; exit 1; fi
if [ "$#" -gt 0 ]; then printf '%s\n' "$@" > Tools/PlayTest/probe_arg.txt; else printf 'levels=2,3\ndifficulties=0,1\nseeds=1,2,3\n' > Tools/PlayTest/probe_arg.txt; fi
rm -f Tools/PlayTest/air_defence.txt
unity command clear_console > /dev/null 2>&1
unity command open_scene --path Assets/_Project/Scenes/Boot.unity --timeout 60 > /dev/null
unity command run_tests --mode PlayMode --filter Report_SkyGunsAgainstNone_OnTheFirstAirLevels --include_explicit true --async_tests true --timeout 60 > /dev/null
started=$(date +%s)
sleep 20
# The runner's own status decides, never a log line; a ceiling keeps a stuck runner from holding the editor for ever.
while unity command test_status 2>&1 | grep -q running; do
  sleep 20
  if [ $(( $(date +%s) - started )) -gt 5400 ]; then echo "timed out after 90 min" >&2; break; fi
done
echo "runner: $(unity command test_status 2>&1 | tail -1 | cut -c1-300)"
echo "took $(( $(date +%s) - started )) s; play mode: $(unity command editor_status 2>&1 | grep -o '"playMode":"[a-z]*"')"
echo "console: $(unity command console_status --result-only 2>&1 | grep -E 'consoleErrors|consoleWarnings' | tr -d '\n')"
if [ -f Tools/PlayTest/air_defence.txt ]; then cp Tools/PlayTest/air_defence.txt "Tools/PlayTest/r61_air_defence_$label.txt"; echo "report: Tools/PlayTest/r61_air_defence_$label.txt"; else echo "no report written"; fi
