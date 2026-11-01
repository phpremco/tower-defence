using System.Collections.Generic;
using Game.Core;
using Game.Enemies;
using Game.Towers;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// A paper model of a level's balance, wave by wave, so a change to a weapon, a creature or a wave shows its
    /// effect without playing. For every wave it works out how much money the player can have had by then (start,
    /// kills, mines), the strongest defence that money buys from the level's towers (damage times fire rate, splash
    /// counted as a bonus against groups, ground and air kept apart), how much effective health the wave brings in
    /// (health plus armour, scaled by difficulty and the wave's own growth) and how long the towers can fire at it
    /// (spawn span plus the time a creature spends inside a tower's range). The ratio is the pressure: below one the
    /// towers alone can hold it, above one the hero, the gates and good placement must make up the difference.
    /// A model, not the game: it ignores placement, gate blocking, drones beyond the harvesters and boss powers.
    /// </summary>
    public static class BalanceLedger
    {
        /// <summary>
        /// Share of the window a tower really spends hurting something: it never covers the whole road, switches
        /// targets, overkills and misses. Calibrated so that one means the greedy bot of the PlayMode probe (every coin
        /// into towers along the road, no upgrades) just about holds the wave.
        /// </summary>
        private const float Utilisation = 0.35f;

        /// <summary>Extra targets a splash shot hits per unit of radius when creatures come in a stream.</summary>
        private const float SplashTargetsPerUnit = 0.8f;
        private const float MaxSplashBonus = 2.2f;

        /// <summary>Share of the level's mines emptied by the time this fraction of the waves has gone by.</summary>
        private const float MineDepletionShare = 0.6f;
        private const int MinesOpenFromWave = 3;

        /// <summary>The hero holds and hits what stops at a gate; counted at this share of the wave window.</summary>
        private const float HeroShare = 0.5f;

        public struct WaveLine
        {
            public int Wave;
            public bool Boss;
            public int Money;
            public float GroundDps;
            public float AirDps;
            public float GroundHealth;
            public float AirHealth;
            public float Window;
            public float GroundPressure;
            public float AirPressure;

            /// <summary>The worse of ground and air: what the player must survive.</summary>
            public float Pressure => Mathf.Max(GroundPressure, AirPressure);

            public override string ToString() =>
                $"w{Wave}{(Boss ? "B" : " ")} money {Money,5} dps {GroundDps,5:0}/{AirDps,4:0} hp {GroundHealth,6:0}/{AirHealth,5:0} window {Window,4:0}s pressure {GroundPressure:0.00}/{AirPressure:0.00}";
        }

        // One purchase step the money can go to: a new tower or the next level of one.
        private readonly struct Step
        {
            public readonly int Cost;
            public readonly float GroundDps;
            public readonly float AirDps;

            public Step(int cost, float groundDps, float airDps)
            {
                Cost = cost;
                GroundDps = groundDps;
                AirDps = airDps;
            }
        }

        public static List<WaveLine> Evaluate(LevelDefinition level, DifficultyProfile difficulty)
        {
            WaveDefinition[] waves = level.GetWaves(difficulty);
            var lines = new List<WaveLine>(waves.Length);
            int kills = 0;
            float heroDps = HeroDps(level);
            // The level counts on the player holding this much permanent upgrade by now; the enemies are scaled the same way.
            float upgrades = 1f + level.ExpectedUpgradeBonus;

            for (int i = 0; i < waves.Length; i++)
            {
                int money = level.StartingMoney + kills + MineIncome(level, i, waves.Length);
                float total = WaveHealth(waves[i], difficulty, level, out float groundHealth, out float airHealth, out float span, out float meanSpeed, out bool boss);
                Buy(level, money, total > 0f ? groundHealth / total : 1f, out float groundDps, out float airDps);
                float window = span + ExposureSeconds(level, meanSpeed);
                float heroDamage = heroDps * window * HeroShare;
                var line = new WaveLine
                {
                    Wave = i + 1,
                    Boss = boss,
                    Money = money,
                    GroundDps = groundDps,
                    AirDps = airDps,
                    GroundHealth = groundHealth,
                    AirHealth = airHealth,
                    Window = window,
                    GroundPressure = Pressure(groundHealth, groundDps * upgrades * window * Utilisation + heroDamage),
                    AirPressure = Pressure(airHealth, airDps * upgrades * window * Utilisation),
                };
                lines.Add(line);
                kills += KillMoney(waves[i], difficulty);
            }

            return lines;
        }

        // The blade and the gun together; the hero only ever uses one at a time, but the gun covers the lulls between.
        private static float HeroDps(LevelDefinition level)
        {
            if (level.Hero == null || level.Hero.Weapon == null)
            {
                return 0f;
            }

            float dps = level.Hero.Damage / level.Hero.StrikeInterval;
            if (level.Hero.RangedWeapon != null)
            {
                dps += level.Hero.RangedWeapon.Damage / level.Hero.RangedWeapon.AttackInterval * 0.5f;
            }

            return dps;
        }

        private static float Pressure(float health, float capacity) => health <= 0f ? 0f : capacity <= 0f ? float.PositiveInfinity : health / capacity;

        // The share of the mines the harvesters have brought home by this wave, less the price of one harvester.
        private static int MineIncome(LevelDefinition level, int waveIndex, int waveCount)
        {
            if (level.MineCells.Length == 0 || level.HarvestDrone == null || waveIndex < MinesOpenFromWave)
            {
                return 0;
            }

            float progress = Mathf.Clamp01((waveIndex - MinesOpenFromWave) / Mathf.Max(1f, waveCount * MineDepletionShare));
            int total = level.MineCells.Length * level.MineStock;
            return Mathf.Max(0, Mathf.RoundToInt(total * progress) - level.HarvestDrone.Cost);
        }

        private static int KillMoney(WaveDefinition wave, DifficultyProfile difficulty)
        {
            int money = 0;
            foreach (SpawnGroup group in wave.Groups)
            {
                money += Count(group, difficulty) * group.Enemy.Reward;
            }

            return money;
        }

        private static int Count(SpawnGroup group, DifficultyProfile difficulty) =>
            group.Enemy.IsBoss ? group.Count : Mathf.Max(1, Mathf.CeilToInt(group.Count * difficulty.CountMultiplier));

        // Effective health is health plus armour (armour soaks that many points before it is gone), split by what
        // can hit the creature; the span is the last spawn moment; the speed is the count-weighted mean.
        private static float WaveHealth(WaveDefinition wave, DifficultyProfile difficulty, LevelDefinition level, out float ground, out float air, out float span, out float meanSpeed, out bool boss)
        {
            ground = 0f;
            air = 0f;
            span = 0f;
            boss = false;
            float speedSum = 0f;
            int total = 0;
            float scale = difficulty.HealthMultiplier * level.EnemyHealthScale * wave.HealthMultiplier;
            foreach (SpawnGroup group in wave.Groups)
            {
                EnemyDefinition enemy = group.Enemy;
                int count = Count(group, difficulty);
                float health = (enemy.MaxHealth + enemy.Armor) * scale * count;
                if ((enemy.Layer & TargetLayers.Ground) != 0)
                {
                    ground += health;
                }
                else
                {
                    air += health;
                }

                boss |= enemy.IsBoss;
                speedSum += enemy.Speed * count;
                total += count;
                span = Mathf.Max(span, group.Delay + (group.Stagger + (count - 1) * group.Interval) * difficulty.SpawnIntervalMultiplier);
            }

            meanSpeed = total > 0 ? speedSum / total : 1f;
            return ground + air;
        }

        // How long one creature stays inside the range of a typical tower: twice the mean range over its speed.
        private static float ExposureSeconds(LevelDefinition level, float speed)
        {
            float range = 0f;
            int count = 0;
            foreach (TowerDefinition tower in level.AvailableTowers)
            {
                if (tower.IsSupport || tower.BlocksGroundEnemies)
                {
                    continue;
                }

                range += tower.GetLevel(0).Range;
                count++;
            }

            return count == 0 ? 0f : 2f * (range / count) / Mathf.Max(0.1f, speed);
        }

        // Spends the money greedily on the step with the most useful damage per coin, where useful means weighted
        // by how much of the wave's health is on the ground and how much in the air.
        private static void Buy(LevelDefinition level, int money, float groundShare, out float groundDps, out float airDps)
        {
            groundDps = 0f;
            airDps = 0f;
            var steps = new List<Step>();
            foreach (TowerDefinition tower in level.AvailableTowers)
            {
                if (tower.IsSupport || tower.BlocksGroundEnemies)
                {
                    continue;
                }

                float splash = tower.SplashRadius > 0f ? Mathf.Min(MaxSplashBonus, 1f + tower.SplashRadius * SplashTargetsPerUnit) : 1f;
                float previous = 0f;
                for (int i = 0; i <= tower.MaxLevelIndex; i++)
                {
                    TowerLevel lvl = tower.GetLevel(i);
                    float dps = lvl.Damage / Mathf.Max(0.05f, lvl.FireInterval) * splash;
                    float gain = dps - previous;
                    previous = dps;
                    steps.Add(new Step(lvl.Cost, (tower.Targets & TargetLayers.Ground) != 0 ? gain : 0f, (tower.Targets & TargetLayers.Air) != 0 ? gain : 0f));
                }
            }

            // The purse is split so that ground and air end up under the same pressure: air defence buys fewer
            // points of damage per coin, so the sky gets more than its share of the health. With anything in the air
            // at all, at least one gun that can reach it is bought. Every tower kind may be bought any number of
            // times, but a level step only after the one before it; the greedy walk approximates that by always
            // taking the best ratio affordable.
            float groundRate = BestRate(steps, false);
            float airRate = BestRate(steps, true);
            float airNeed = airRate > 0f ? (1f - groundShare) / airRate : 0f;
            float groundNeed = groundRate > 0f ? groundShare / groundRate : 0f;
            int airMoney = airNeed + groundNeed > 0f ? Mathf.RoundToInt(money * airNeed / (airNeed + groundNeed)) : 0;
            if (groundShare < 1f)
            {
                airMoney = Mathf.Min(money, Mathf.Max(airMoney, BestAirStepCost(steps, money)));
            }

            Spend(steps, airMoney, true, ref groundDps, ref airDps, out int left);
            Spend(steps, money - airMoney + left, false, ref groundDps, ref airDps, out _);
        }

        // The most damage per coin any single step buys against the ground or the air.
        private static float BestRate(List<Step> steps, bool air)
        {
            float best = 0f;
            foreach (Step step in steps)
            {
                float dps = air ? step.AirDps : step.GroundDps;
                if (step.Cost > 0 && dps / step.Cost > best)
                {
                    best = dps / step.Cost;
                }
            }

            return best;
        }

        // The price of the air gun with the most damage per coin that the purse can pay for, else the cheapest one.
        private static int BestAirStepCost(List<Step> steps, int money)
        {
            int cheapest = int.MaxValue;
            int best = 0;
            float bestRate = 0f;
            foreach (Step step in steps)
            {
                if (step.AirDps <= 0f || step.Cost <= 0)
                {
                    continue;
                }

                cheapest = Mathf.Min(cheapest, step.Cost);
                if (step.Cost <= money && step.AirDps / step.Cost > bestRate)
                {
                    bestRate = step.AirDps / step.Cost;
                    best = step.Cost;
                }
            }

            return best > 0 ? best : cheapest == int.MaxValue ? 0 : cheapest;
        }

        private static void Spend(List<Step> steps, int money, bool air, ref float groundDps, ref float airDps, out int left)
        {
            while (true)
            {
                int best = -1;
                float bestRatio = 0f;
                for (int i = 0; i < steps.Count; i++)
                {
                    Step step = steps[i];
                    if (step.Cost > money || step.Cost <= 0)
                    {
                        continue;
                    }

                    float ratio = (air ? step.AirDps : step.GroundDps) / step.Cost;
                    if (ratio > bestRatio)
                    {
                        bestRatio = ratio;
                        best = i;
                    }
                }

                if (best < 0)
                {
                    left = money;
                    return;
                }

                money -= steps[best].Cost;
                groundDps += steps[best].GroundDps;
                airDps += steps[best].AirDps;
            }
        }
    }
}
