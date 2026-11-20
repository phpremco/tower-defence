#!/usr/bin/env bash
# Round 87: one finale probe on four clones that retries a difficulty whose clone wrote no report (a lost ILPP pipe),
# merged into finale_probe.txt and kept as finale_probe_<name>.txt. With "apply" the finale stage follows.
#   bash Tools/PlayTest/r87_finale_cycle.sh <name> [apply] [only: difficulties already probed are kept from <file>]
set -uo pipefail
cd "$(dirname "$0")/../.."
name="$1"; apply="${2:-}"; keep="${3:-}"
out="Tools/PlayTest/finale_probe_$name.txt"
log="Tools/PlayTest/r87_finale_$name.log"
: > "$log"
if [ -n "$keep" ]; then cp "$keep" "$out"; else
  bash Tools/PlayTest/finale_workers.sh >> "$log" 2>&1
  cp Tools/PlayTest/finale_probe.txt "$out"
fi
for try in 1 2 3; do
  missing=()
  for d in 0 1 2 3; do grep -q "^cell|[A-Za-z]*|$d|" "$out" || missing+=("$d"); done
  [ "${#missing[@]}" -eq 0 ] && break
  echo "== retry $try: difficulties ${missing[*]}" >> "$log"
  bash Tools/PlayTest/finale_workers.sh "${missing[@]}" >> "$log" 2>&1
  { grep -v '^cell|' "$out"; grep -v '^cell|' Tools/PlayTest/finale_probe.txt | grep '^  [A-Z]'; { grep '^cell|' "$out"; grep '^cell|' Tools/PlayTest/finale_probe.txt; } | sort -t'|' -k2,2 -k3,3n; } > "$out.tmp"
  mv "$out.tmp" "$out"
done
cp "$out" Tools/PlayTest/finale_probe.txt
echo "cells: $(grep -c '^cell|' "$out"), lost at the data: $(grep '^cell|' "$out" | grep -c '|lost|')" >> "$log"
if [ "$apply" = "apply" ]; then
  bash Tools/PlayTest/stage.sh run ApplyFinale.cs ApplyFinale.Run | tail -1 | cut -c1-300 >> "$log"
  cat Tools/PlayTest/finale_apply.txt >> "$log"
fi
echo "DONE" >> "$log"
