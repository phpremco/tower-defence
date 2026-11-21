#!/usr/bin/env bash
# Round 88b: re-stamps the calibration on the stage after a rule or data change that is in its input hash, then solves
# the waves and rewrites the wave and boss reports. The fit runs on clone w6 without the plan records (round 86 recipe);
# only the stamp is taken (ApplyCalibration.Keep). Run from the stage: bash Tools/PlayTest/r88b_restamp.sh <label>
set -uo pipefail
cd "$(dirname "$0")/../.."
label="${1:-restamp}"
log="Tools/PlayTest/r88b_${label}.log"
{
  TD_DROP=Tools/PlayTest/plan_records.txt bash Tools/PlayTest/r34b_editmode_worker.sh 6 CalibrationFitTests
  cp /c/Users/remco/TowerDefenseWorkers/w6/Tools/PlayTest/calibration.txt Tools/PlayTest/calibration.txt
  cp /c/Users/remco/TowerDefenseWorkers/w6/Tools/PlayTest/calibration_waves.txt Tools/PlayTest/calibration_waves.txt
  bash Tools/PlayTest/stage.sh cmd refresh > /dev/null
  bash Tools/PlayTest/stage.sh run ApplyCalibration.cs ApplyCalibration.Keep | tail -1 | cut -c1-400
  bash Tools/PlayTest/stage.sh run ApplyRound68.cs ApplyRound68.Run | tail -1 | cut -c1-500
  bash Tools/PlayTest/stage.sh run Round87Lab.cs Round87Lab.Run | tail -1 | cut -c1-200
  bash Tools/PlayTest/stage.sh cmd eval_file --file Tools/PlayTest/r66_stale.cs | tail -1 | cut -c1-300
  echo RESTAMPED
} > "$log" 2>&1
