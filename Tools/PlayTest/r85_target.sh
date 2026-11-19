#!/usr/bin/env bash
# Round 85: a targeted measurement of the cells one closed-loop pass moved, before the full 24-cell chain is spent on
# them. Each argument after the name is "levels:difficulties" for r85_measure.sh (its ladders per difficulty).
#   bash Tools/PlayTest/r85_target.sh r85_t1 "0:3" "1:0,2,3" "3:1"
# Records: pressure_<name>_<i>_*; the log Tools/PlayTest/<name>.log ends with TARGET DONE.
cd "$(dirname "$0")/../.."
name="$1"; shift
log="Tools/PlayTest/$name.log"
echo "== target start $(date +%H:%M)" > "$log"
i=0
for pair in "$@"; do
  i=$((i + 1))
  PR_WORKERS=5 bash Tools/PlayTest/r85_measure.sh "${name}_$i" "${pair%%:*}" "${pair#*:}" >> "$log" 2>&1
  echo "== part $i ($pair) done $(date +%H:%M)" >> "$log"
done
echo "TARGET DONE $(date +%H:%M)" >> "$log"
