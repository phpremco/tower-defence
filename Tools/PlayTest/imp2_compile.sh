#!/usr/bin/env bash
# Recompiles only (Play Mode must be stopped) and prints the compile result and the first console messages.
set -u
cd "$(dirname "$0")/../.." || exit 1
status=$(unity command editor_status --result-only 2>&1)
if ! echo "$status" | grep -q '"playMode": *"stopped"'; then echo "NOT STOPPED - abort"; exit 1; fi
unity command clear_console --result-only >/dev/null 2>&1
unity recompile >/dev/null 2>&1
for i in $(seq 1 60); do sleep 3; s=$(unity command editor_status --result-only 2>&1); echo "$s" | grep -q '"compiling": *false' && break; done
unity command console_status --result-only 2>&1 | grep -o '"compilationFailed": *[a-z]*\|"errors": *[0-9]*\|"warnings": *[0-9]*'
unity command console_status --result-only 2>&1 | grep -o '"consoleErrors": [0-9]*\|"consoleWarnings": [0-9]*'
