using System;
using System.Collections.Generic;

namespace Game.Balance
{
    /// <summary>One level of the campaign, in play order, as the progression model sees it.</summary>
    public sealed class CampaignLevel
    {
        public string Key = string.Empty;

        /// <summary>Index of the continent (map region) the level belongs to.</summary>
        public int Continent;

        /// <summary>Ids of the towers the level sells (the bar shows only those the player has unlocked).</summary>
        public List<string> TowerIds = new List<string>();
        public string RewardTowerId = string.Empty;

        /// <summary>Hero perk id granted on the first clear, or empty.</summary>
        public string RewardHeroPerkId = string.Empty;
        public int Research;
        public int Power;

        /// <summary>The builder's target scale per difficulty as stored on the level (LevelDefinition.BalanceTargetScale), 1 = the curve.</summary>
        public float[] TargetScale = { 1f };

        public float TargetScaleFor(int difficulty) => TargetScale == null || TargetScale.Length == 0 ? 1f : TargetScale[difficulty < TargetScale.Length ? difficulty : TargetScale.Length - 1];
    }

    /// <summary>What the player brings into a level on a difficulty: owned towers and the permanent upgrades bought.</summary>
    public sealed class PlayerState
    {
        public readonly HashSet<string> OwnedTowers = new HashSet<string>();
        public readonly Dictionary<string, TowerBoost> Boosts = new Dictionary<string, TowerBoost>();
        public readonly HashSet<string> HeroPerks = new HashSet<string>();
        public float HeroDamage = 1f;
        public float GateHealth = 1f;
        /// <summary>Flat base health the "Base armour" upgrades add (the game adds it to the level's base health).</summary>
        public float BaseHealthBonus;

        /// <summary>Levels of the permanent base weapon upgrade: each adds a turret beside the base or strengthens them (PlacementController).</summary>
        public int BaseWeaponLevel;
        public int ResearchEarned;
        public int ResearchSpent;

        /// <summary>The permanent upgrades bought, by upgrade id and level: what a save holding this state carries (round 22d, the replay plays it).</summary>
        public readonly Dictionary<string, int> UpgradeLevels = new Dictionary<string, int>();

        /// <summary>Mean damage times fire rate over the owned guns: the old "expected upgrade bonus", for reports.</summary>
        public float MeanTowerBonus;

        /// <summary>The same towers without any permanent upgrade or perk: a fresh save, as the probe bots play (round 22c).</summary>
        public PlayerState Plain()
        {
            var plain = new PlayerState();
            foreach (string id in OwnedTowers)
            {
                plain.OwnedTowers.Add(id);
                plain.Boosts[id] = TowerBoost.None;
            }

            return plain;
        }
    }

    /// <summary>
    /// Walks the campaign in order and works out, per level and difficulty, what the player owns and has bought by then: the
    /// player at the moment they would first plausibly try that cell (the curator, round 22e), not the end-game player. Towers
    /// and perks come from the starting set plus the rewards of the levels before. Research comes from the levels before, on the
    /// path a player of that difficulty takes (<see cref="PathOf"/>: an Easy player clears each level on Easy; a Medium player
    /// on Medium; a Hard player on Medium and Hard; a Psycho player on all four), plus the level itself cleared on the lower
    /// difficulties of that path (<see cref="OwnLevelPathOf"/>: a Hard player has beaten the level on Medium first, a Psycho
    /// player on Easy, Medium and Hard). So C1_01 Psycho is a player with the first level's research three times over and few
    /// upgrades, not one with the whole continent behind them. The research is spent cheapest first on upgrades of the owned
    /// guns, the hero, the gates and the base, which is roughly how a player spreads it.
    /// </summary>
    public static class Progression
    {
        public const int EndGameDifficulty = 3;

        /// <summary>The difficulties a player of the given difficulty has cleared each earlier level on.</summary>
        public static int[] PathOf(int difficulty)
        {
            switch (difficulty)
            {
                case 0:
                    return new[] { 0 };
                case 1:
                    return new[] { 1 };
                case 2:
                    return new[] { 1, 2 };
                default:
                    return new[] { 0, 1, 2, 3 };
            }
        }

        /// <summary>The difficulties a player of the given difficulty has cleared the level itself on before trying it: the path below it.</summary>
        public static int[] OwnLevelPathOf(int difficulty)
        {
            int[] path = PathOf(difficulty);
            var below = new List<int>();
            foreach (int d in path)
            {
                if (d < difficulty)
                {
                    below.Add(d);
                }
            }

            return below.ToArray();
        }

        public static PlayerState StateAt(
            IReadOnlyList<CampaignLevel> campaign,
            int levelIndex,
            int difficulty,
            IEnumerable<string> startingTowers,
            IReadOnlyList<UpgradeStats> upgrades,
            BalanceRules rules)
        {
            var state = new PlayerState();
            foreach (string id in startingTowers)
            {
                state.OwnedTowers.Add(id);
            }

            float multiplier = 0f;
            foreach (int d in PathOf(difficulty))
            {
                multiplier += rules.ResearchMultipliers[Math.Min(d, rules.ResearchMultipliers.Length - 1)];
            }

            for (int i = 0; i < levelIndex && i < campaign.Count; i++)
            {
                if (!string.IsNullOrEmpty(campaign[i].RewardTowerId))
                {
                    state.OwnedTowers.Add(campaign[i].RewardTowerId);
                }

                if (!string.IsNullOrEmpty(campaign[i].RewardHeroPerkId))
                {
                    state.HeroPerks.Add(campaign[i].RewardHeroPerkId);
                }

                state.ResearchEarned += (int)Math.Round(campaign[i].Research * multiplier, MidpointRounding.AwayFromZero);
            }

            // The level itself, beaten on the lower difficulties of the path before this attempt (its tower reward is not counted:
            // the bar sells the level's own towers whatever the player owns, and the PlayMode probe owns the levels before only).
            if (levelIndex >= 0 && levelIndex < campaign.Count)
            {
                float own = 0f;
                foreach (int d in OwnLevelPathOf(difficulty))
                {
                    own += rules.ResearchMultipliers[Math.Min(d, rules.ResearchMultipliers.Length - 1)];
                }

                state.ResearchEarned += (int)Math.Round(campaign[levelIndex].Research * own, MidpointRounding.AwayFromZero);
            }

            Spend(state, upgrades);
            return state;
        }

        /// <summary>One past the last level of the given level's continent, in campaign order.</summary>
        public static int EndOfContinent(IReadOnlyList<CampaignLevel> campaign, int levelIndex)
        {
            if (levelIndex < 0 || levelIndex >= campaign.Count)
            {
                return campaign.Count;
            }

            int continent = campaign[levelIndex].Continent;
            int end = levelIndex + 1;
            while (end < campaign.Count && campaign[end].Continent <= continent)
            {
                end++;
            }

            return end;
        }

        // Cheapest next level first over every useful upgrade; ties go to the earlier one in the catalogue.
        private static void Spend(PlayerState state, IReadOnlyList<UpgradeStats> upgrades)
        {
            var levels = new int[upgrades.Count];
            int purse = state.ResearchEarned;
            while (true)
            {
                int best = -1;
                int bestCost = int.MaxValue;
                for (int i = 0; i < upgrades.Count; i++)
                {
                    UpgradeStats upgrade = upgrades[i];
                    if (upgrade.PaidInPower || levels[i] >= upgrade.Costs.Length || !Useful(state, upgrade))
                    {
                        continue;
                    }

                    int cost = upgrade.Costs[levels[i]];
                    if (cost <= purse && cost < bestCost)
                    {
                        best = i;
                        bestCost = cost;
                    }
                }

                if (best < 0)
                {
                    break;
                }

                purse -= bestCost;
                state.ResearchSpent += bestCost;
                levels[best]++;
            }

            var boosts = new Dictionary<string, TowerBoost>();
            for (int i = 0; i < upgrades.Count; i++)
            {
                UpgradeStats upgrade = upgrades[i];
                if (levels[i] > 0)
                {
                    state.UpgradeLevels[upgrade.Id] = levels[i];
                }

                float bonus = levels[i] * upgrade.BonusPerLevel;
                if (bonus <= 0f)
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(upgrade.TowerId))
                {
                    TowerBoost boost = boosts.TryGetValue(upgrade.TowerId, out TowerBoost found) ? found : TowerBoost.None;
                    switch (upgrade.Kind)
                    {
                        case UpgradeKind.TowerDamage:
                            boost.Damage += bonus;
                            break;
                        case UpgradeKind.TowerFireRate:
                            boost.Rate += bonus;
                            break;
                        case UpgradeKind.TowerRange:
                            boost.Range += bonus;
                            break;
                    }

                    boosts[upgrade.TowerId] = boost;
                }
                else if (upgrade.Kind == UpgradeKind.HeroDamage)
                {
                    state.HeroDamage += bonus;
                }
                else if (upgrade.Kind == UpgradeKind.GateHealth)
                {
                    state.GateHealth += bonus;
                }
                else if (upgrade.Kind == UpgradeKind.BaseHealth)
                {
                    state.BaseHealthBonus += bonus;
                }
                else if (upgrade.Kind == UpgradeKind.BaseWeapon)
                {
                    state.BaseWeaponLevel += levels[i];
                }
            }

            float sum = 0f;
            foreach (string id in state.OwnedTowers)
            {
                TowerBoost boost = boosts.TryGetValue(id, out TowerBoost found) ? found : TowerBoost.None;
                state.Boosts[id] = boost;
                sum += boost.Damage * boost.Rate - 1f;
            }

            state.MeanTowerBonus = state.OwnedTowers.Count > 0 ? sum / state.OwnedTowers.Count : 0f;
        }

        // Upgrades of towers the player does not own yet wait; base health and base turrets are not in the model.
        private static bool Useful(PlayerState state, UpgradeStats upgrade)
        {
            switch (upgrade.Kind)
            {
                case UpgradeKind.TowerDamage:
                case UpgradeKind.TowerFireRate:
                case UpgradeKind.TowerRange:
                    return state.OwnedTowers.Contains(upgrade.TowerId);
                case UpgradeKind.HeroDamage:
                case UpgradeKind.GateHealth:
                case UpgradeKind.BaseHealth:
                case UpgradeKind.BaseWeapon:
                    return true;
                default:
                    return true;
            }
        }
    }
}
