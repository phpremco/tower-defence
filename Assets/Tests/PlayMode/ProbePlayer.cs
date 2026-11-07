using System.Collections.Generic;
using Game.Balance;
using Game.Core;
using Game.Economy;
using Game.Levels;
using Game.Platform;

namespace Game.Tests
{
    /// <summary>
    /// The player a probe run plays as (round 22d). Stage A plans for the campaign's expected player at a level and difficulty
    /// (permanent upgrades, perks, base armour and turrets bought with the research of the levels before, Progression.StateAt);
    /// stage B and the yardstick must play that same player, or paper and play judge different games. Before the level scene
    /// loads, this writes that state into a ProgressService over an in-memory save and registers it in place of the file-backed
    /// one, so no probe run reads or writes Remco's progress.json. The fresh-save sanity runs install a plain state.
    /// </summary>
    internal static class ProbePlayer
    {
        /// <summary>A save that lives in memory for one run: the probe never touches the player's file.</summary>
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

        /// <summary>
        /// Installs the player for a run: the expected player of the cell (or a plain one), owning <paramref name="owned"/>.
        /// Call after the Boot scene registered its services and before the level scene loads. Returns the state installed.
        /// </summary>
        public static PlayerState Install(CampaignCatalog catalog, int node, int difficulty, bool fresh, IEnumerable<string> owned)
        {
            PlayerState state = StateFor(catalog, node, difficulty, fresh);
            var progress = new ProgressService(new MemorySave(), StartingTowers());
            foreach (string id in owned)
            {
                progress.UnlockTower(id);
            }

            foreach (string perk in state.HeroPerks)
            {
                progress.UnlockHeroPerk(perk);
            }

            foreach (KeyValuePair<string, int> upgrade in state.UpgradeLevels)
            {
                progress.Data.Upgrades.Add(new UpgradeLevel { Id = upgrade.Key, Level = upgrade.Value });
            }

            progress.Data.ResearchPoints = state.ResearchEarned - state.ResearchSpent;
            progress.MarkIntroSeen();
            Services.Register(progress);
            return state;
        }

        /// <summary>The expected player of the cell by the balance model (or the same towers on a plain save).</summary>
        public static PlayerState StateFor(CampaignCatalog catalog, int node, int difficulty, bool fresh)
        {
            PlayerState state = Model(catalog).PlayerAt(ModelIndex(catalog, node), difficulty);
            return fresh ? state.Plain() : state;
        }

        /// <summary>One line for the run's log: what the installed player brings.</summary>
        public static string Describe(PlayerState state, bool fresh)
        {
            if (fresh)
            {
                return "fresh save";
            }

            return string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                "expected player: research {0} spent {1}, {2} upgrades, hero x{3:0.00}, gates x{4:0.00}, base +{5:0}, base weapon {6}, perks {7}",
                state.ResearchEarned, state.ResearchSpent, state.UpgradeLevels.Count, state.HeroDamage, state.GateHealth, state.BaseHealthBonus, state.BaseWeaponLevel, state.HeroPerks.Count);
        }

        // The model's level index skips catalog nodes without a level.
        private static int ModelIndex(CampaignCatalog catalog, int node)
        {
            int index = 0;
            for (int i = 0; i < node; i++)
            {
                index += catalog.Nodes[i].Level != null ? 1 : 0;
            }

            return index;
        }

        private static CampaignModel Model(CampaignCatalog catalog)
        {
#if UNITY_EDITOR
            var upgrades = UnityEditor.AssetDatabase.LoadAssetAtPath<UpgradeCatalog>(BalanceProbeTests.UpgradesPath);
            return BalanceLedger.Campaign(catalog, upgrades, StartingTowers(), BalanceRules.Default, UnityEditor.AssetDatabase.LoadAssetAtPath<BalanceCalibration>(BalanceCalibration.AssetPath));
#else
            return new CampaignModel();
#endif
        }

        private static IReadOnlyList<string> StartingTowers()
        {
#if UNITY_EDITOR
            var unlocks = UnityEditor.AssetDatabase.LoadAssetAtPath<UnlockTable>(BalanceProbeTests.UnlocksPath);
            return unlocks.StartingTowerIds;
#else
            return new string[0];
#endif
        }
    }
}
