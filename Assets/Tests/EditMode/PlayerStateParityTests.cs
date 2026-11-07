using System.Collections.Generic;
using Game.Balance;
using Game.Core;
using Game.Economy;
using Game.Editor;
using Game.Levels;
using Game.Platform;
using NUnit.Framework;

namespace Game.Tests
{
    /// <summary>
    /// Player-state parity (round 22d): the campaign's expected player on paper (Progression.StateAt) and the same purchases
    /// written into a save (as the probe does before a replay, ProbePlayer) must give the game the same modifiers. Otherwise
    /// stage A plans for one player and stage B plays another.
    /// </summary>
    public sealed class PlayerStateParityTests
    {
        private const float Tolerance = 0.001f;

        private sealed class MemorySave : ISaveService
        {
            private readonly Dictionary<string, object> _data = new Dictionary<string, object>();

            public void Save<T>(string key, T data) => _data[key] = data;

            public bool TryLoad<T>(string key, out T data)
            {
                if (_data.TryGetValue(key, out object value) && value is T found)
                {
                    data = found;
                    return true;
                }

                data = default;
                return false;
            }
        }

        [Test]
        public void ExpectedPlayer_WrittenIntoASave_GivesTheGameTheSameModifiers()
        {
            BalanceStages.Loaded loaded = BalanceStages.Load(false);
            Assert.IsNotNull(loaded.Catalog, "Run Tools/Generate/Scenes and Starter Data first.");
            int checkedCells = 0;
            for (int index = 0; index < loaded.Model.Levels.Count; index++)
            {
                for (int difficulty = 0; difficulty < loaded.Model.Difficulties.Count; difficulty++)
                {
                    PlayerState state = loaded.Model.PlayerAt(index, difficulty);
                    ProgressService progress = SaveOf(state, loaded.Unlocks.StartingTowerIds);
                    string cell = loaded.Model.Levels[index].Key + " " + difficulty;

                    PlayerModifiers global = UpgradeEffects.ComputeGlobal(progress, loaded.Upgrades);
                    Assert.AreEqual(state.HeroDamage, global.HeroDamageMultiplier, Tolerance, cell + " hero damage");
                    Assert.AreEqual(state.GateHealth, global.BlockadeHealthMultiplier, Tolerance, cell + " gate health");
                    Assert.AreEqual(state.BaseHealthBonus, global.BaseHealthBonus, 0.5f, cell + " base armour");
                    Assert.AreEqual(state.BaseWeaponLevel, global.BaseWeaponLevel, cell + " base weapon level");

                    foreach (string id in state.OwnedTowers)
                    {
                        TowerBoost boost = state.Boosts.TryGetValue(id, out TowerBoost found) ? found : TowerBoost.None;
                        PlayerModifiers tower = UpgradeEffects.ComputeForTower(progress, loaded.Upgrades, id);
                        Assert.AreEqual(boost.Damage, tower.TowerDamageMultiplier, Tolerance, cell + " " + id + " damage");
                        Assert.AreEqual(boost.Rate, tower.TowerFireRateMultiplier, Tolerance, cell + " " + id + " fire rate");
                        Assert.AreEqual(boost.Range, tower.TowerRangeMultiplier, Tolerance, cell + " " + id + " range");
                    }

                    checkedCells++;
                }
            }

            Assert.Greater(checkedCells, 0);
        }

        [Test]
        public void ExpectedPlayer_SpendsResearch_FromTheSecondLevelOn()
        {
            BalanceStages.Loaded loaded = BalanceStages.Load(false);
            Assert.IsNotNull(loaded.Catalog, "Run Tools/Generate/Scenes and Starter Data first.");
            Assume.That(loaded.Model.Levels.Count, Is.GreaterThan(1));
            PlayerState fresh = loaded.Model.PlayerAt(0, 1);
            PlayerState second = loaded.Model.PlayerAt(1, 1);
            Assert.IsEmpty(fresh.UpgradeLevels, "the first level's player has nothing to spend");
            Assert.IsNotEmpty(second.UpgradeLevels, "the second level's Medium player should hold permanent upgrades");
            Assert.IsEmpty(second.Plain().UpgradeLevels, "a plain state carries no upgrades");
        }

        // The same purchases as a save, the way ProbePlayer writes them before a probe run.
        private static ProgressService SaveOf(PlayerState state, IReadOnlyList<string> startingTowers)
        {
            var progress = new ProgressService(new MemorySave(), startingTowers);
            foreach (string id in state.OwnedTowers)
            {
                progress.UnlockTower(id);
            }

            foreach (KeyValuePair<string, int> upgrade in state.UpgradeLevels)
            {
                progress.Data.Upgrades.Add(new UpgradeLevel { Id = upgrade.Key, Level = upgrade.Value });
            }

            return progress;
        }
    }
}
