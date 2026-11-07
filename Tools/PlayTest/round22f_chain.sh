#!/usr/bin/env bash
# Round 22f's PlayMode chain, run in the background with a log: for each group of cells whose waves the builder (or the Easy
# raid rule, or the own-reward player) changed, re-measure the yardstick, search the strategies and replay them, merging the
# records with the cells outside the group; then the acceptance report and the README. Each argument is one group as
# "levels=...;difficulties=..." (catalog node indices and difficulty indices).
#
#   bash Tools/PlayTest/round22f_chain.sh "levels=0;difficulties=2,3" "levels=4,5;difficulties=0,1,2,3" > Tools/PlayTest/round22f_chain.log 2>&1 &
set -uo pipefail
cd "$(dirname "$0")/../.."
echo "== chain start $(date '+%F %T')"
for group in "$@"; do
  levels="${group%%;*}"
  difficulties="${group##*;}"
  echo "== group $levels $difficulties: measure $(date '+%F %T')"
  bash Tools/PlayTest/measure_cells.sh "$levels" "$difficulties"
  echo "== group $levels $difficulties: search $(date '+%F %T')"
  printf '%s\n%s\n' "$levels" "$difficulties" > Tools/PlayTest/probe_arg.txt
  bash Tools/PlayTest/balance_pipeline.sh search | tail -3
  rm -f Tools/PlayTest/probe_arg.txt
  echo "== group $levels $difficulties: replay $(date '+%F %T')"
  bash Tools/PlayTest/replay_cells.sh "$levels" "$difficulties"
done
rm -f Tools/PlayTest/probe_arg.txt
echo "== groups done $(date '+%F %T')"
bash Tools/PlayTest/balance_pipeline.sh acceptance readme
echo "== chain done $(date '+%F %T')"
