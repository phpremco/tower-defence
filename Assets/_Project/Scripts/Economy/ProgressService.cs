using System;
using System.Collections.Generic;
using Game.Platform;

namespace Game.Economy
{
    /// <summary>Owns the saved progress: currencies, unlocked weapons, completed levels and bought upgrades.</summary>
    public sealed class ProgressService
    {
        private const string SaveKey = "progress";

        private readonly ISaveService _save;

        public ProgressService(ISaveService save, IEnumerable<string> startingTowerIds)
        {
            _save = save;
            if (_save.TryLoad(SaveKey, out ProgressData data) && data != null)
            {
                ProgressMigrator.Migrate(data);
            }
            else
            {
                data = new ProgressData { Version = ProgressMigrator.CurrentVersion };
            }

            Data = data;
            foreach (string id in startingTowerIds)
            {
                UnlockTower(id);
            }
        }

        public ProgressData Data { get; }

        public event Action Changed;

        public bool IsTowerUnlocked(string towerId) => Data.UnlockedTowerIds.Contains(towerId);

        /// <summary>Returns true when the tower was not unlocked before.</summary>
        public bool UnlockTower(string towerId)
        {
            if (IsTowerUnlocked(towerId))
            {
                return false;
            }

            Data.UnlockedTowerIds.Add(towerId);
            return true;
        }

        public bool IsHeroPerkUnlocked(string perkId) => Data.UnlockedHeroPerkIds.Contains(perkId);

        /// <summary>Returns true when the perk was not unlocked before. Saved with the next <see cref="Save"/>.</summary>
        public bool UnlockHeroPerk(string perkId) => AddOnce(Data.UnlockedHeroPerkIds, perkId);

        public bool IsPathObjectUnlocked(string pathObjectId) => Data.UnlockedPathObjectIds.Contains(pathObjectId);

        /// <summary>Returns true when the gadget was not unlocked before. Saved with the next <see cref="Save"/>.</summary>
        public bool UnlockPathObject(string pathObjectId) => AddOnce(Data.UnlockedPathObjectIds, pathObjectId);

        private static bool AddOnce(List<string> list, string id)
        {
            if (string.IsNullOrEmpty(id) || list.Contains(id))
            {
                return false;
            }

            list.Add(id);
            return true;
        }

        public bool IsCompleted(string levelId, string difficultyId) => Data.CompletedLevels.Contains(Key(levelId, difficultyId));

        public bool IsLevelCompleted(string levelId)
        {
            string prefix = levelId + "/";
            foreach (string entry in Data.CompletedLevels)
            {
                if (entry.StartsWith(prefix, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        public void MarkCompleted(string levelId, string difficultyId)
        {
            string key = Key(levelId, difficultyId);
            if (!Data.CompletedLevels.Contains(key))
            {
                Data.CompletedLevels.Add(key);
            }
        }

        public int GetStars(string levelId, string difficultyId)
        {
            string key = Key(levelId, difficultyId);
            foreach (StarRecord record in Data.Stars)
            {
                if (record.Key == key)
                {
                    return record.Stars;
                }
            }

            return 0;
        }

        /// <summary>Remembers the best rating for a level and difficulty.</summary>
        public void RecordStars(string levelId, string difficultyId, int stars)
        {
            string key = Key(levelId, difficultyId);
            for (int i = 0; i < Data.Stars.Count; i++)
            {
                if (Data.Stars[i].Key == key)
                {
                    if (stars > Data.Stars[i].Stars)
                    {
                        Data.Stars[i] = new StarRecord { Key = key, Stars = stars };
                    }

                    return;
                }
            }

            Data.Stars.Add(new StarRecord { Key = key, Stars = stars });
        }

        public void AddRewards(int researchPoints, int powerCurrency)
        {
            Data.ResearchPoints += researchPoints;
            Data.PowerCurrency += powerCurrency;
        }

        public int GetUpgradeLevel(string upgradeId)
        {
            foreach (UpgradeLevel entry in Data.Upgrades)
            {
                if (entry.Id == upgradeId)
                {
                    return entry.Level;
                }
            }

            return 0;
        }

        /// <summary>True when the player holds enough of the currency this upgrade is paid in.</summary>
        public bool CanAfford(UpgradeDefinition upgrade, int cost)
        {
            int held = upgrade.Currency == UpgradeCurrency.PowerCurrency ? Data.PowerCurrency : Data.ResearchPoints;
            return held >= cost;
        }

        public bool TryBuyUpgrade(UpgradeDefinition upgrade)
        {
            int level = GetUpgradeLevel(upgrade.Id);
            if (level >= upgrade.MaxLevel)
            {
                return false;
            }

            int cost = upgrade.CostForNextLevel(level);
            if (!CanAfford(upgrade, cost))
            {
                return false;
            }

            if (upgrade.Currency == UpgradeCurrency.PowerCurrency)
            {
                Data.PowerCurrency -= cost;
            }
            else
            {
                Data.ResearchPoints -= cost;
            }

            SetUpgradeLevel(upgrade.Id, level + 1);
            Save();
            return true;
        }

        /// <summary>Remembers that the intro has played, so it does not play again on the next launch.</summary>
        public void MarkIntroSeen()
        {
            Data.IntroSeen = true;
            Save();
        }

        /// <summary>True once the player has met this creature (bosses: their Codex entry is open).</summary>
        public bool HasSeenEnemy(string enemyId) => Data.SeenEnemyIds != null && Data.SeenEnemyIds.Contains(enemyId);

        /// <summary>Remembers that the player has met a creature, so its Codex entry stays open.</summary>
        public void MarkEnemySeen(string enemyId)
        {
            if (HasSeenEnemy(enemyId))
            {
                return;
            }

            Data.SeenEnemyIds ??= new List<string>();
            Data.SeenEnemyIds.Add(enemyId);
            Save();
        }

        public bool HasSeenStory(string storyId) => Data.SeenStoryIds.Contains(storyId);

        /// <summary>Remembers that a story moment has played, so a once-per-save trigger stays quiet afterwards.</summary>
        public void MarkStorySeen(string storyId)
        {
            if (HasSeenStory(storyId))
            {
                return;
            }

            Data.SeenStoryIds.Add(storyId);
            Save();
        }

        public void SetVolume(int percent)
        {
            Data.MasterVolumePercent = percent;
            Save();
        }

        /// <summary>Stores all four volume groups at once, in percent.</summary>
        public void SetVolumes(int master, int music, int effects, int ambience)
        {
            Data.MasterVolumePercent = master;
            Data.MusicVolumePercent = music;
            Data.EffectsVolumePercent = effects;
            Data.AmbienceVolumePercent = ambience;
            Save();
        }

        /// <summary>Stores the camera options: pan speed in percent of the default, and edge scrolling on or off.</summary>
        public void SetCameraControls(int speedPercent, bool edgeScrolling)
        {
            Data.CameraSpeedPercent = speedPercent;
            Data.EdgeScrollingOff = !edgeScrolling;
            Save();
        }

        /// <summary>Stores the radar options: shown or not, and its size in percent of the default.</summary>
        public void SetRadar(bool on, int sizePercent)
        {
            Data.RadarOff = !on;
            Data.RadarSizePercent = sizePercent;
            Save();
        }

        public void SetHudMargin(int percent)
        {
            Data.HudMarginPercent = percent;
            Save();
        }

        /// <summary>The street weapons chosen last time, by asset name.</summary>
        public IReadOnlyList<string> Loadout => Data.LoadoutIds;

        public void SetLoadout(IEnumerable<string> ids)
        {
            Data.LoadoutIds = new List<string>(ids);
            Save();
        }

        public void Save()
        {
            _save.Save(SaveKey, Data);
            Changed?.Invoke();
        }

        private void SetUpgradeLevel(string upgradeId, int level)
        {
            for (int i = 0; i < Data.Upgrades.Count; i++)
            {
                if (Data.Upgrades[i].Id == upgradeId)
                {
                    Data.Upgrades[i] = new UpgradeLevel { Id = upgradeId, Level = level };
                    return;
                }
            }

            Data.Upgrades.Add(new UpgradeLevel { Id = upgradeId, Level = level });
        }

        private static string Key(string levelId, string difficultyId) => levelId + "/" + difficultyId;
    }
}

