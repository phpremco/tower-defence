#!/usr/bin/env bash
# The balance pipeline, chained (round 22; Docs/BALANCE.md "The balance pipeline"). Run from the project root with the
# editor open (Unity CLI, Pipeline package). Every stage reads the previous stage's files and the game's definitions;
# nothing is typed in. Stages: measure (PlayMode yardstick) -> fit (EditMode) -> apply (asset) -> solve (waves) ->
# search (stage A, EditMode) -> replay (stage B, PlayMode) -> acceptance (EditMode). Use "all" or any list of stages.
#
#   Tools/PlayTest/balance_pipeline.sh all
#   Tools/PlayTest/balance_pipeline.sh search replay acceptance
#
# Tools/PlayTest/probe_arg.txt (untracked, optional) narrows the PlayMode stages: levels=, difficulties=, seeds=, step=.
# The save rule: a PlayMode run may create Saves/progress.json; the script restores the state it found.
set -euo pipefail
cd "$(dirname "$0")/../.."

SAVE="$LOCALAPPDATA/../LocalLow/DefaultCompany/Tower defense/Saves/progress.json"
SAVE_BACKUP="$(mktemp)"
HAD_SAVE=0
if [ -f "$SAVE" ]; then HAD_SAVE=1; cp "$SAVE" "$SAVE_BACKUP"; fi
restore_save() {
  if [ "$HAD_SAVE" = 1 ]; then cp "$SAVE_BACKUP" "$SAVE"; else rm -f "$SAVE"; fi
}
trap restore_save EXIT

stopped() {
  unity command editor_status 2>&1 | grep -q '"playMode":"stopped"'
}

require_stopped() {
  if ! stopped; then echo "Play Mode is running: stop it first (never recompile or test while Remco plays)." >&2; exit 1; fi
}

playmode_test() {
  # Starts a PlayMode test asynchronously and waits for the runner to finish.
  require_stopped
  unity command open_scene --path Assets/_Project/Scenes/Boot.unity --timeout 60 > /dev/null
  unity command run_tests --mode PlayMode --filter "$1" --include_explicit true --async_tests true --timeout 60 > /dev/null
  sleep 20
  while unity command test_status 2>&1 | grep -q running; do sleep 30; done
  unity command test_status 2>&1 | tail -1
}

editmode_test() {
  require_stopped
  unity command run_tests --mode EditMode --filter "$1" --result-only --timeout 1800 | grep -E '"FullName"|"Status"|"Message"' | head -20
}

for stage in "$@"; do
  case "$stage" in
    all) exec "$0" measure fit apply solve search replay acceptance readme ;;
    measure)    echo "== measure: yardstick (obvious family, seeds)"; playmode_test Measure_Yardstick_ObviousFamily ;;
    stepcheck)  echo "== step check"; playmode_test Measure_StepCheck_FineAgainstUsual ;;
    fit)        echo "== fit: calibration from the records"; editmode_test CalibrationFitTests; sed -n '1,40p' Tools/PlayTest/calibration.txt ;;
    apply)      echo "== apply: calibration asset"; require_stopped; unity command run_script --file Tools/PlayTest/Scripts/ApplyCalibration.cs --entry ApplyCalibration.Run --timeout_ms 120000 --timeout 180 | tail -1 ;;
    solve)      echo "== solve: waves against the calibration"; require_stopped; unity command run_script --file Tools/PlayTest/Scripts/ApplyWaves.cs --entry ApplyWaves.Run --timeout_ms 900000 --timeout 960 | tail -1 ;;
    search)     echo "== search: plans per cell (stage A)"; editmode_test PlanSearchTests; cat Tools/PlayTest/plans_summary.txt ;;
    replay)     echo "== replay: searched plans (stage B)"; playmode_test Replay_SearchedPlans_EveryCell ;;
    acceptance) echo "== acceptance per difficulty"; editmode_test BalanceAcceptanceTests; cat Tools/PlayTest/acceptance.txt ;;
    build)      echo "== build: lever from the stage-B verdicts, then solve (round 22d)"; require_stopped; unity command run_script --file Tools/PlayTest/Scripts/BuildFromVerdicts.cs --entry BuildFromVerdicts.Run --timeout_ms 900000 --timeout 960 | tail -1; cat Tools/PlayTest/builder.txt ;;
    readme)     echo "== readme: the plans that won in stage B"; require_stopped; unity command run_script --file Tools/PlayTest/Scripts/ReadmePlans.cs --entry ReadmePlans.Run --timeout_ms 300000 --timeout 360 | tail -1 ;;
    droneprice) echo "== drone price: the repair drone by the investment rule"; require_stopped; unity command run_script --file Tools/PlayTest/Scripts/ApplyDronePrice.cs --entry ApplyDronePrice.Run --timeout_ms 120000 --timeout 180 | tail -1 ;;
    *) echo "unknown stage $stage" >&2; exit 1 ;;
  esac
done
