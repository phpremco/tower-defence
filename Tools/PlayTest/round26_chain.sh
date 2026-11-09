#!/usr/bin/env bash
# Round 26: the whole pipeline after a model-shape change (approach/behind cover, the blocking hero, the gate-upgrade step) and the
# light-fire armour factor 0.2: measure and replay on the workers, fit, apply by override (the bar is the curator's call: the fit
# prints its hold-out line), solve, search, acceptance, readme. Run from the project root with the editor open and stopped:
#   bash Tools/PlayTest/round26_chain.sh > Tools/PlayTest/round26_chain.log 2>&1 &
set -uo pipefail
cd "$(dirname "$0")/../.."
SAVE="$LOCALAPPDATA/../LocalLow/DefaultCompany/Tower defense/Saves/progress.json"
echo "== round26 chain start $(date '+%H:%M:%S'); save $(md5sum "$SAVE" | cut -c1-8)"
for stage in measure fit apply-force solve search replay acceptance readme; do
  echo "== $stage $(date '+%H:%M:%S')"
  if ! bash Tools/PlayTest/balance_pipeline.sh "$stage"; then
    echo "== $stage FAILED $(date '+%H:%M:%S')"
    [ "$stage" = measure ] || [ "$stage" = replay ] && exit 1
  fi
done
echo "== round26 chain done $(date '+%H:%M:%S'); save $(md5sum "$SAVE" | cut -c1-8)"
ls Assets/InitTestScene*.unity 2>/dev/null && echo "InitTestScene left behind"
echo EXIT 0
