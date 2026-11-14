#!/usr/bin/env bash
# The balance pipeline, chained (round 22; Docs/BALANCE.md "The balance pipeline"). Run from the project root with the
# editor open (Unity CLI, Pipeline package). Every stage reads the previous stage's files and the game's definitions;
# nothing is typed in. Stages: measure (PlayMode yardstick) -> fit (EditMode) -> apply (asset) -> solve (waves) ->
# search (stage A, EditMode) -> replay (stage B, PlayMode) -> acceptance (EditMode). Use "all" or any list of stages.
#
#   Tools/PlayTest/balance_pipeline.sh all
#   Tools/PlayTest/balance_pipeline.sh search replay acceptance
#   Tools/PlayTest/balance_pipeline.sh --serial measure         # the PlayMode stages in the open editor instead of the workers
#   Tools/PlayTest/balance_pipeline.sh snapshot                 # the impact baseline (round 25): take it before changing a number
#   Tools/PlayTest/balance_pipeline.sh impact                   # paper deltas against the baseline, flagged cells, replay filter
#
# Round 25: the PlayMode stages (measure, replay, stepcheck excepted) run on parallel headless workers by default
# (Tools/PlayTest/workers.sh; clones of the project, the open editor stays usable); --serial runs them in the open editor
# as before. Tools/PlayTest/probe_arg.txt (untracked, optional) narrows the PlayMode stages: levels=, difficulties=, seeds=,
# cells=node:difficulty[:seed], step=. The workers read the same file. The apply stage refuses a fit below the calibration bar;
# apply-force applies it anyway (the curator's call).
# The save rule: a PlayMode run may create Saves/progress.json; the script restores the state it found.
set -euo pipefail
cd "$(dirname "$0")/../.."

SERIAL=0
if [ "${1:-}" = "--serial" ]; then SERIAL=1; shift; fi

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
  # Starts a PlayMode test asynchronously in the open editor and waits for the runner to finish.
  require_stopped
  unity command open_scene --path Assets/_Project/Scenes/Boot.unity --timeout 60 > /dev/null
  unity command run_tests --mode PlayMode --filter "$1" --include_explicit true --async_tests true --timeout 60 > /dev/null
  sleep 20
  while unity command test_status 2>&1 | grep -q running; do sleep 30; done
  unity command test_status 2>&1 | tail -1
}

# The filter file's lines as worker arguments (the workers get the same narrowing as the editor would).
filter_lines() {
  if [ -f Tools/PlayTest/probe_arg.txt ]; then grep -E '^(levels|difficulties|seeds|cells|step|seed)=' Tools/PlayTest/probe_arg.txt || true; fi
}

playmode_stage() {
  # $1 the stage (measure|replay), $2 the editor test. Parallel workers unless --serial; the editor path writes the records
  # whole, the workers merge whole cells into them (measure_cells.sh / replay_cells.sh semantics).
  if [ "$SERIAL" = 1 ]; then
    playmode_test "$2"
  else
    mapfile -t lines < <(filter_lines)
    bash Tools/PlayTest/workers.sh run "$1" "${lines[@]}"
  fi
}

editmode_test() {
  require_stopped
  unity command run_tests --mode EditMode --filter "$1" --result-only --timeout 1800 | grep -E '"FullName"|"Status"|"Message"' | head -20
}

run_script() {
  require_stopped
  unity command run_script --file "$1" --entry "$2" --timeout_ms "$3" --timeout "$4" | tail -1
}

for stage in "$@"; do
  case "$stage" in
    all) exec "$0" $([ "$SERIAL" = 1 ] && echo --serial) measure fit apply solve search replay acceptance readme ;;
    measure)    echo "== measure: yardstick (obvious family, seeds)"; playmode_stage measure Measure_Yardstick_ObviousFamily ;;
    stepcheck)  echo "== step check"; playmode_test Measure_StepCheck_FineAgainstUsual ;;
    fit)        echo "== fit: calibration from the records"; editmode_test CalibrationFitTests; sed -n '1,40p' Tools/PlayTest/calibration.txt ;;
    apply)      echo "== apply: calibration asset (refused below the bar)"; run_script Tools/PlayTest/Scripts/ApplyCalibration.cs ApplyCalibration.Run 120000 180 ;;
    apply-force) echo "== apply: calibration asset, bar override"; run_script Tools/PlayTest/Scripts/ApplyCalibration.cs ApplyCalibration.Force 120000 180 ;;
    apply-keep) echo "== apply: inputs re-stamped, efficiencies and factors kept (round 62)"; run_script Tools/PlayTest/Scripts/ApplyCalibration.cs ApplyCalibration.Keep 120000 180 ;;
    solve)      echo "== solve: waves against the calibration"; run_script Tools/PlayTest/Scripts/ApplyWaves.cs ApplyWaves.Run 900000 960 ;;
    search)     echo "== search: plans per cell (stage A)"; editmode_test PlanSearchTests; cat Tools/PlayTest/plans_summary.txt ;;
    replay)     echo "== replay: searched plans (stage B)"; playmode_stage replay Replay_SearchedPlans_EveryCell ;;
    acceptance) echo "== acceptance per difficulty"; editmode_test BalanceAcceptanceTests; cat Tools/PlayTest/acceptance.txt ;;
    build)      echo "== build: lever from the stage-B verdicts, then solve (round 22d)"; run_script Tools/PlayTest/Scripts/BuildFromVerdicts.cs BuildFromVerdicts.Run 900000 960; cat Tools/PlayTest/builder.txt ;;
    readme)     echo "== readme: the plans that won in stage B"; run_script Tools/PlayTest/Scripts/ReadmePlans.cs ReadmePlans.Run 300000 360 ;;
    droneprice) echo "== drone price: the repair drone by the investment rule"; run_script Tools/PlayTest/Scripts/ApplyDronePrice.cs ApplyDronePrice.Run 120000 180 ;;
    finale)     echo "== finale: boss health and shares from the finale probe (round 22f)"; run_script Tools/PlayTest/Scripts/ApplyFinale.cs ApplyFinale.Run 120000 180 ;;
    snapshot)   echo "== snapshot: the impact baseline (paper of every cell on the current data and calibration)"; run_script Tools/PlayTest/Scripts/Impact.cs Impact.Snapshot 900000 960 ;;
    impact)     echo "== impact: paper deltas against the baseline (round 25)"; run_script Tools/PlayTest/Scripts/Impact.cs Impact.Run 600000 660; cat Tools/PlayTest/impact.txt ;;
    *) echo "unknown stage $stage" >&2; exit 1 ;;
  esac
done
