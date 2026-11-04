#!/usr/bin/env bash
# Import stage 4a: recompiles (only with Play Mode stopped) and rebuilds the Blender environment: sync, ground sets,
# kit and decor materials, the Crystal Plains set, and the campaign themes pointed at it.
set -u
cd "$(dirname "$0")/../.." || exit 1
status=$(unity command editor_status --result-only 2>&1)
if ! echo "$status" | grep -q '"playMode": *"stopped"'; then echo "NOT STOPPED - abort"; exit 1; fi
unity command clear_console --result-only >/dev/null 2>&1
unity recompile >/dev/null 2>&1
for i in $(seq 1 60); do sleep 3; s=$(unity command editor_status --result-only 2>&1); echo "$s" | grep -q '"compiling": *false' && break; done
if unity command console_status --result-only 2>&1 | grep -q '"compilationFailed": *true'; then echo "COMPILE FAILED"; unity command console --result-only 2>&1 | grep -o '"message": "[^"]*' | head -10; exit 1; fi
unity command menu --path "Tools/Generate/Blender Environment (sync, ground sets, kit, decor)" --timeout 900 --result-only 2>&1 | grep -o '"message": "[^"]*'
until unity command editor_status --result-only 2>/dev/null | grep -q '"status": "ready"'; do sleep 3; done
unity command console --result-only 2>&1 | grep -o '"level": "[a-z]*", *"logType": "[A-Za-z]*", *"message": "[^"]*' | cut -c1-260 | head -20
