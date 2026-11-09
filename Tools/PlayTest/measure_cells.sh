#!/usr/bin/env bash
# Re-measures the yardstick for a subset of cells and merges the records (round 22e; parallel workers since round 25). The
# yardstick stage writes calibration_records.txt whole, so a narrowed run would drop the cells it did not play; the merge
# keeps the old records of every cell outside the filter and adds the new ones. By default the units run on headless
# workers (Tools/PlayTest/workers.sh: clones of the project, the open editor stays free); --serial runs them in the editor.
#
#   bash Tools/PlayTest/measure_cells.sh "difficulties=2,3"
#   bash Tools/PlayTest/measure_cells.sh "levels=2" "difficulties=1"
#   bash Tools/PlayTest/measure_cells.sh "cells=2:0,3:2"            # the cells an impact report flagged (node:difficulty)
#   bash Tools/PlayTest/measure_cells.sh --serial "levels=2"
#
# Run from the project root; the yardstick table of the narrowed run goes to yardstick_partial.txt.
set -euo pipefail
cd "$(dirname "$0")/../.."
if [ "${1:-}" != "--serial" ]; then
  exec bash Tools/PlayTest/workers.sh run measure "$@"
fi
shift
RECORDS=Tools/PlayTest/calibration_records.txt
ARG=Tools/PlayTest/probe_arg.txt
printf '%s\n' "$@" > "$ARG"
cp "$RECORDS" "$RECORDS.before"
bash Tools/PlayTest/balance_pipeline.sh --serial measure
rm -f "$ARG"
cp Tools/PlayTest/yardstick.txt Tools/PlayTest/yardstick_partial.txt

# Which cells the new file holds (level|difficulty|player), then the old records minus those cells, then the new records.
awk -F'|' '$1 == "run" { print $2 "|" $3 "|" $13 }' "$RECORDS" | sort -u > "$RECORDS.cells"
awk -F'|' -v cells="$RECORDS.cells" '
  BEGIN { while ((getline line < cells) > 0) { drop[line] = 1 } }
  $1 == "run" { keep = !((($2 "|" $3 "|" $13) in drop)) }
  keep { print }
' "$RECORDS.before" > "$RECORDS.merged"
cat "$RECORDS" >> "$RECORDS.merged"
mv "$RECORDS.merged" "$RECORDS"
rm -f "$RECORDS.cells" "$RECORDS.before"
echo "records merged: $(grep -c '^run|' "$RECORDS") runs in $RECORDS"
