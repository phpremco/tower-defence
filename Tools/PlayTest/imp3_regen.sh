#!/usr/bin/env bash
# Recompiles (only with Play Mode stopped) and rebuilds the Blender colony art: sync, materials, gate, base, drones,
# path objects and the resource node, and points their owners at it.
set -u
cd "$(dirname "$0")/../.." || exit 1
status=$(unity command editor_status --result-only 2>&1)
if ! echo "$status" | grep -q '"playMode": *"stopped"'; then echo "NOT STOPPED - abort"; exit 1; fi
unity command clear_console --result-only >/dev/null 2>&1
unity recompile >/dev/null 2>&1
for i in $(seq 1 60); do sleep 3; s=$(unity command editor_status --result-only 2>&1); echo "$s" | grep -q '"compiling": *false' && break; done
unity command console_status --result-only 2>&1 | grep -o '"compilationFailed": *[a-z]*'
unity command menu --path "Tools/Generate/Blender Colony (sync, gate, base, drones, gadgets)" --timeout 600 --result-only 2>&1 | grep -o '"message": "[^"]*'
until unity command editor_status --result-only 2>/dev/null | grep -q '"status": "ready"'; do sleep 3; done
unity command console --result-only 2>&1 | grep -o '"level": "[a-z]*", *"logType": "[A-Za-z]*", *"message": "[^"]*' | cut -c1-260 | head -20
