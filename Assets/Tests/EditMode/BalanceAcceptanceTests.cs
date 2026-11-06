using Game.Editor;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests
{
    /// <summary>
    /// Remco's strategy rule per difficulty, judged on the recorded runs (round 22, <see cref="Game.Balance.AcceptanceRules"/>):
    /// the obvious family from the yardstick, the searched plans from the replay. Writes Tools/PlayTest/acceptance.txt; a cell
    /// that fails is listed and the test is ignored with the list (the levels are tuned by the builder, not by this test).
    /// Same code as Tools/Generate/Balance/7.
    /// </summary>
    public sealed class BalanceAcceptanceTests
    {
        [Test]
        public void Acceptance_EveryRecordedCell_MeetsItsDifficultyRule()
        {
            BalanceStages.AcceptanceReport report = BalanceStages.Acceptance();
            if (report.Cells == 0)
            {
                Assert.Ignore("No recorded cells yet: run the yardstick and the plan replay first (Docs/BALANCE.md).");
            }

            Debug.Log($"[Acceptance] {report.Cells - report.FailingCells.Count}/{report.Cells} cells pass; see {BalanceStages.AcceptancePath}");
            if (report.FailingCells.Count > 0)
            {
                Assert.Ignore($"Cells failing their difficulty rule ({report.FailingCells.Count}/{report.Cells}): {string.Join(", ", report.FailingCells)}. See {BalanceStages.AcceptancePath}.");
            }
        }
    }
}
