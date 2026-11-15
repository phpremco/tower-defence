#!/usr/bin/env bash
# Round 66: re-seats the hero's gear (ApplyRound66.cs) and renders the gear sheet (R66GearSheet.cs) without Play Mode.
# Usage: bash Tools/PlayTest/r66_sheet.sh <label> <tiers, comma separated> [out folder] [fresh]
#   "fresh" deletes the gear art asset first, so it is written again with the class's current defaults.
set -u
cd "$(dirname "$0")/../.." || exit 1
if ! unity command editor_status --result-only 2>&1 | grep -q '"playMode": *"stopped"'; then echo "NOT STOPPED - abort"; exit 1; fi
if [ "${4:-}" = fresh ]; then unity command delete_asset --asset Assets/_Project/Data/Art/Story/MercerGearArt.asset --confirm true --result-only >/dev/null 2>&1; fi
unity command run_script --file Tools/PlayTest/Scripts/ApplyRound66.cs --entry ApplyRound66.Run --timeout_ms 300000 --timeout 360 2>&1 | grep -o '"result":"[^"]*' | sed 's/\n/\n/g' | head -14
printf 'label=%s\ntiers=%s\nout=%s\n' "$1" "$2" "${3:-Logs/r66}" > Tools/PlayTest/r66_sheet_arg.txt
unity command run_script --file Tools/PlayTest/Scripts/R66GearSheet.cs --entry R66GearSheet.Run --timeout_ms 120000 --timeout 180 2>&1 | grep -o '"result":"[^"]*'
