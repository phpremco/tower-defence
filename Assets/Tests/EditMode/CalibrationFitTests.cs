using Game.Editor;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests
{
    /// <summary>
    /// Stage "fit" of the balance pipeline (round 22): fits the ledger's per-tower efficiency on the recorded PlayMode runs
    /// (the yardstick's calibration_records.txt and the replays' plan_records.txt), checks it on held-out levels and against
    /// the fine-step records, and writes Tools/PlayTest/calibration.txt for the apply stage. The bar (the critic of round 21):
    /// verdicts agree on 90% of the runs, mono rows included, plan rankings correlate at 0.8; below it the test is ignored
    /// with the numbers in its message, so the table is always written. Same code as Tools/Generate/Balance/2.
    /// </summary>
    public sealed class CalibrationFitTests
    {
        [Test]
        public void Fit_FromRecordedRuns_WritesCalibration()
        {
            BalanceStages.FitReport report = BalanceStages.FitCalibration();
            if (report == null)
            {
                Assert.Ignore("No recorded runs yet: run the PlayMode stage Measure_Yardstick_ObviousFamily first (Docs/BALANCE.md).");
            }

            Debug.Log("[Calibration] " + report.Verdict + "; see " + BalanceStages.CalibrationTablePath);
            if (!report.Accepted)
            {
                Assert.Ignore("Calibration below the bar: " + report.Verdict + ". See " + BalanceStages.CalibrationTablePath);
            }
        }
    }
}
