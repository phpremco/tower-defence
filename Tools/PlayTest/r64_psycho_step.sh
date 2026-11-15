#!/usr/bin/env bash
# Round 64, last iteration: the Psycho cells after the growth cap for Psycho bosses and two builder steps. Re-stamps the
# calibration for the new inputs (fit on clone w5 for the hash, factors kept), then measures, searches and replays the cells.
set -euo pipefail
cd "$(dirname "$0")/../.."
ROOT="$(cygpath -u "${TD_WORKERS_ROOT:-C:\Users\remco\TowerDefenseWorkers}")"
cells=(0:3 1:3 2:3 3:3 4:3 5:3)
bash Tools/PlayTest/r34b_editmode_worker.sh 5 CalibrationFitTests | tail -1
cp "$ROOT/w5/Tools/PlayTest/calibration.txt" "$ROOT/w5/Tools/PlayTest/calibration_waves.txt" Tools/PlayTest/
bash Tools/PlayTest/balance_pipeline.sh apply-keep | tail -1 | cut -c150-330
bash Tools/PlayTest/workers.sh run measure 5 "cells=$(IFS=,; echo "${cells[*]}")" | tail -2
bash Tools/PlayTest/r64_cells_chain.sh search "${cells[@]}"
bash Tools/PlayTest/r64_cells_chain.sh replay "${cells[@]}"
