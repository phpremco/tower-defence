#!/bin/bash
# Round 84: one run of the wave ledger lab on all 24 cells with rule overrides.
#   bash Tools/PlayTest/r84_lab.sh <label> ["rule.ArmourGrowth=1,1,1,1" "rule.PlatedBias=1,1,1,1" ...]
# Writes Tools/PlayTest/wave_ledger_<label>.txt; no asset is written. Check editor_status (playMode stopped) first.
cd "$(dirname "$0")/../.."
label="$1"; shift
{
  for k in 1 2 3 4 5 6; do for d in 0 1 2 3; do echo "cell=C1_0$k:$d"; done; done
  for o in "$@"; do echo "$o"; done
  echo "label=$label"
} > Tools/PlayTest/wave_ledger_arg.txt
for try in 1 2 3; do
  out=$(unity command run_script --file Tools/PlayTest/Scripts/WaveLedgerLab.cs --entry WaveLedgerLab.Run --timeout_ms 900000 --timeout 960 2>&1)
  if echo "$out" | grep -q "written Tools/PlayTest/wave_ledger_$label.txt"; then echo "written wave_ledger_$label.txt"; exit 0; fi
  sleep 3
done
echo "$out" | cut -c1-600; exit 1
