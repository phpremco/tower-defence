using Game.Balance;
using Game.Editor;
using NUnit.Framework;

namespace Game.Tests
{
    /// <summary>
    /// The research and power curve per continent (Docs/BALANCE.md): Easy everywhere in a continent pays a fifth of its
    /// research budget, Psycho everywhere all of it; Psycho pays the power budget; payouts depend on a level's index only.
    /// </summary>
    public sealed class ContinentEconomyTests
    {
        private static readonly BalanceRules Rules = BalanceRules.Default;
        private const int CrystalBudget = 748;
        private const int CrystalPower = 186;
        private const double PowerTolerance = 0.06;

        [Test]
        public void Weights_RiseGently_AndTheFinalePaysDouble()
        {
            Assert.AreEqual(1.0, ContinentEconomy.ResearchWeight(0, Rules), 1e-9);
            Assert.AreEqual(1.5, ContinentEconomy.ResearchWeight(18, Rules), 1e-9);
            Assert.AreEqual(2.0, ContinentEconomy.ResearchWeight(19, Rules), 1e-9);
            Assert.AreEqual(25.75, ContinentEconomy.ResearchWeightSum(Rules), 1e-9);
        }

        [Test]
        public void CrystalPlains_PaysTheSpecNumbers()
        {
            Assert.AreEqual(6, ContinentEconomy.Research(CrystalBudget, 0, Rules), "level 1 on Easy");
            Assert.AreEqual(7, ContinentEconomy.Research(CrystalBudget, 5, Rules), "level 6 on Easy");
            Assert.AreEqual(12, ContinentEconomy.Research(CrystalBudget, 19, Rules), "the finale on Easy");
            Assert.AreEqual(30, (int)(ContinentEconomy.Research(CrystalBudget, 0, Rules) * Rules.ResearchMultipliers[3]), "level 1 on Crazy is 6 x 5");
        }

        [Test]
        public void AContinent_PaysAFifthOnEasy_AndAllOnPsycho()
        {
            foreach (BalanceSheet.Continent continent in BalanceSheet.Continents)
            {
                if (continent.ResearchBudget == 0)
                {
                    continue;
                }

                int easy = 0;
                int power = 0;
                for (int i = 0; i < Rules.LevelsPerContinent; i++)
                {
                    easy += ContinentEconomy.Research(continent.ResearchBudget, i, Rules);
                    power += ContinentEconomy.Power(continent.PowerBudget, (int)Rules.ExpectedBossesPerLevel, Rules);
                }

                Assert.AreEqual(0.2, easy / (double)continent.ResearchBudget, 0.03, $"{continent.Key}: Easy everywhere");
                Assert.AreEqual(1.0, easy * Rules.ResearchMultipliers[3] / continent.ResearchBudget, 0.05, $"{continent.Key}: Psycho everywhere");
                // Power pays about four a level, so rounding to whole points costs up to a twentieth of the budget.
                Assert.AreEqual(1.0, power * Rules.PowerMultipliers[3] / continent.PowerBudget, PowerTolerance, $"{continent.Key}: power on Psycho");
                Assert.AreEqual(0f, Rules.PowerMultipliers[0], "Easy pays no power");
            }
        }

        [Test]
        public void ASmallBudget_NeverPaysBelowTheFloor()
        {
            for (int i = 0; i < Rules.LevelsPerContinent; i++)
            {
                Assert.GreaterOrEqual(ContinentEconomy.Research(40, i, Rules), Rules.MinResearchPerLevel);
            }

            Assert.AreEqual(0, ContinentEconomy.Research(0, 0, Rules), "a continent without a budget pays nothing");
        }

        [Test]
        public void Bosses_PayMorePower()
        {
            Assert.Greater(ContinentEconomy.Power(CrystalPower, 3, Rules), ContinentEconomy.Power(CrystalPower, 1, Rules));
            Assert.AreEqual(4, ContinentEconomy.Power(CrystalPower, 2, Rules), "about four a level");
        }

        [Test]
        public void Psycho_IsTheEndGameOfItsOwnContinent()
        {
            var campaign = new System.Collections.Generic.List<CampaignLevel>
            {
                new CampaignLevel { Key = "A1", Continent = 0, RewardTowerId = "Laser", Research = 10 },
                new CampaignLevel { Key = "A2", Continent = 0, Research = 10 },
                new CampaignLevel { Key = "B1", Continent = 1, RewardTowerId = "Cryo", Research = 10 },
            };
            var upgrades = new System.Collections.Generic.List<UpgradeStats>();

            PlayerState crazy = Progression.StateAt(campaign, 0, Progression.EndGameDifficulty, new[] { "Rifle" }, upgrades, Rules);
            Assert.IsTrue(crazy.OwnedTowers.Contains("Laser"), "its own continent's weapons");
            Assert.IsFalse(crazy.OwnedTowers.Contains("Cryo"), "not the next continent's");
            Assert.AreEqual(2 * 10 * 6, crazy.ResearchEarned, "both levels of the continent on Easy, Medium and Hard (1 + 2 + 3)");
            Assert.AreEqual(3, Progression.EndOfContinent(campaign, 2));
        }
    }
}
