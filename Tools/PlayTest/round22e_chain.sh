#!/usr/bin/env bash
# Round 22e's PlayMode chain, run in the background with a log: re-measure the yardstick on the cells whose waves changed
# (the expected player rule moved the Hard and Psycho waves), replay the searched strategies on every cell, then the
# acceptance report and the README. Stage A (the strategy search) runs before it; the builder after it.
#
#   bash Tools/PlayTest/round22e_chain.sh "difficulties=2,3" > Tools/PlayTest/round22e_chain.log 2>&1 &
#
# The first argument is the measure filter (empty = every cell); the replay plays every cell.
set -uo pipefail
cd "$(dirname "$0")/../.."
echo "== chain start $(date '+%F %T')"
if [ -n "${1:-}" ]; then
  bash Tools/PlayTest/measure_cells.sh "$1"
else
  bash Tools/PlayTest/balance_pipeline.sh measure
fi
echo "== measure done $(date '+%F %T')"
rm -f Tools/PlayTest/probe_arg.txt
bash Tools/PlayTest/balance_pipeline.sh replay
echo "== replay done $(date '+%F %T')"
bash Tools/PlayTest/balance_pipeline.sh acceptance readme
echo "== chain done $(date '+%F %T')"
