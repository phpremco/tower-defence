using System.Collections.Generic;
using Game.Balance;
using Game.Editor;
using NUnit.Framework;

namespace Game.Tests
{
    /// <summary>Teasers preview a creature late in a level: only in the last third, never more than their count.</summary>
    public sealed class TeaserTests
    {
        private const float LastThird = 2f / 3f;

        [Test]
        public void EveryTeaser_ComesInTheLastThird()
        {
            int seen = 0;
            foreach (CampaignLevelSpec spec in LevelSpecs.All)
            {
                foreach (TeaserSpec teaser in spec.Waves.Teasers)
                {
                    seen++;
                    Assert.GreaterOrEqual(teaser.Wave, spec.Waves.Waves * LastThird, $"{spec.Key}: the {teaser.Enemy} teaser comes too early");
                    Assert.LessOrEqual(teaser.Wave, spec.Waves.Waves, $"{spec.Key}: the {teaser.Enemy} teaser comes after the last wave");
                    Assert.Greater(teaser.Count, 0);
                }
            }

            Assert.Greater(seen, 0, "Crystal Plains level 5 teases the fast rollers");
        }

        // The S4 teaser rule: never on a boss wave, a species the level's troops do not already bring, and a first sighting
        // dialogue so the surprise is spoken.
        [Test]
        public void EveryTeaser_IsANewSpecies_OffTheBossWaves_WithAFirstSighting()
        {
            foreach (CampaignLevelSpec spec in LevelSpecs.All)
            {
                foreach (TeaserSpec teaser in spec.Waves.Teasers)
                {
                    foreach (BossSpec boss in spec.Waves.Bosses)
                    {
                        Assert.AreNotEqual(boss.Wave, teaser.Wave, $"{spec.Key}: the {teaser.Enemy} teaser shares wave {teaser.Wave} with {boss.Enemy}");
                    }

                    foreach (TroopSpec troop in spec.Waves.Troops)
                    {
                        Assert.AreNotEqual(troop.Enemy, teaser.Enemy, $"{spec.Key}: the {teaser.Enemy} teaser is already one of the level's troops");
                    }

                    Assert.IsTrue(System.Array.Exists(spec.Story, s => s.Kind == "FirstSighting" && s.Enemy == teaser.Enemy), $"{spec.Key}: the {teaser.Enemy} teaser has no first sighting dialogue");
                }
            }
        }

        [Test]
        public void Composer_AddsExactlyTheTeaser_OnItsWaveOnly()
        {
            var enemies = new Dictionary<string, EnemyStats>
            {
                ["Walker"] = new EnemyStats { Id = "Walker", Health = 20f, Speed = 1f },
                ["Racer"] = new EnemyStats { Id = "Racer", Health = 100f, Speed = 3f },
            };
            var script = new WaveScriptSpec
            {
                Key = "Teaser",
                Waves = 9,
                Troops = new[] { new TroopSpec { Enemy = "Walker", Weight = 1f, Interval = 1f } },
                Teasers = new[] { new TeaserSpec { Enemy = "Racer", Wave = 8, Count = 3 } },
            };

            for (int number = 1; number <= script.Waves; number++)
            {
                for (int difficulty = 0; difficulty < 4; difficulty++)
                {
                    WaveSpec wave = WaveComposer.Compose(script, enemies, difficulty, number, 1f, -1, BalanceRules.Default);
                    int racers = 0;
                    foreach (GroupSpec group in wave.Groups)
                    {
                        racers += group.Enemy.Id == "Racer" ? group.Count : 0;
                    }

                    Assert.AreEqual(number == 8 ? 3 : 0, racers, $"wave {number} difficulty {difficulty}");
                }
            }
        }
    }
}
