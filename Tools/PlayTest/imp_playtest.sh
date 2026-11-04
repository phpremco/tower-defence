#!/usr/bin/env bash
# Runs one PlayMode test filter (Play Mode must be stopped) and waits for it; prints the summary and failures.
set -u
cd "$(dirname "$0")/../.." || exit 1
status=$(unity command editor_status --result-only 2>&1)
if ! echo "$status" | grep -q '"playMode": *"stopped"'; then echo "NOT STOPPED - abort"; exit 1; fi
unity command clear_console >/dev/null 2>&1
unity command open_scene --path Assets/_Project/Scenes/Boot.unity --result-only >/dev/null 2>&1
unity command run_tests --mode PlayMode --filter "$1" --async_tests true --result-only >/dev/null 2>&1
sleep 15
for i in $(seq 1 360); do s=$(unity command test_status --result-only 2>&1); echo "$s" | grep -q '"status": *"completed"' && break; sleep 5; done
echo "$1: $(echo "$s" | grep -o '"total": [0-9]*\|"passed": [0-9]*\|"failed": [0-9]*' | tr '\n' ' ')"
echo "$s" | grep -o '"Message": "[^"]*' | grep -v '"Message": "$' | cut -c1-400
unity command console_status --result-only 2>&1 | grep -o '"consoleErrors": *[0-9]*\|"consoleWarnings": *[0-9]*' | tr '\n' ' '; echo
