using System;
using System.Collections.Generic;

namespace Game.Balance
{
    /// <summary>
    /// One group of a wave as the route-sampled kill model walks it (round 27): identical creatures on one path from one
    /// source, each with its body health and armour after the difficulty's scaling.
    /// </summary>
    public sealed class WaveGroup
    {
        public int Count;

        /// <summary>Body health and armour of one creature, scaled; <see cref="Total"/> is what a heavy weapon must deal.</summary>
        public float Health;
        public float Armour;
        public float Speed = 1f;
        public int Path;
        public int Source;
        public bool IsBoss;
        public bool Flies;

        public float Total => Health + Armour;

        /// <summary>Share of a weapon's damage that counts against this creature, by the weapon's armour factor (<see cref="ArmourMix.Share"/>).</summary>
        public float Share(float armourFactor) => Armour <= 0f || armourFactor <= 0f ? 1f : Total / (Health + Armour / armourFactor);
    }

    /// <summary>
    /// What one wave brings, as the ledger needs it: effective health split into the ground stream, the bosses and the
    /// air, the time the towers get, how dense the streams are (for splash), the mean size of a creature (for overkill),
    /// the share of the defence each creature really meets (fronts and late joiners), and the kill money.
    /// </summary>
    public sealed class WaveLoad
    {
        /// <summary>The wave's groups for the route-sampled kill model (round 27); empty for a load built by hand.</summary>
        public readonly List<WaveGroup> Groups = new List<WaveGroup>();

        /// <summary>Spawn source value of a boss that comes up through the level's own boss entrance.</summary>
        public const int EntranceSource = 7;

        public float StreamHealth;
        public float BossHealth;
        public float AirHealth;

        /// <summary>Share of the ground defence the stream and the bosses meet (1 = all of it).</summary>
        public float StreamReach = 1f;
        public float BossReach = 1f;

        /// <summary>Seconds the wave takes to spawn, and the seconds the gates' hold adds for guns that reach the queue.</summary>
        public float Span;
        public float GateSeconds;
        public float Window;
        public float BossWindow;

        /// <summary>Seconds of full firepower the bosses' mechanics take away (see <see cref="EnemyStats.MechanicLossSeconds"/>).</summary>
        public float BossMechanicLoss;

        /// <summary>Seconds the bosses spend on the field: the main road at their pace, plus the time a gate holds them.</summary>
        public float BossFight;

        /// <summary>Share of the fight their mechanics take the defence's firepower away (zero without a continent boss).</summary>
        public float BossMechanicShare => BossFight > 0f ? BossMechanicLoss / BossFight : 0f;
        public float AirWindow;
        public float GroundSpeed = 1f;
        public float BossSpeed = 1f;
        public float AirSpeed = 1f;
        public float GroundDensity;
        public float AirDensity;
        public float MeanHealth;
        public float MeanAirHealth;
        public int KillMoney;
        public bool HasBoss;

        /// <summary>Creatures of the ground stream and bosses in the wave (after the difficulty's count share), for the leak model.</summary>
        public int GroundCount;
        public int BossCount;
        public int AirCount;

        /// <summary>
        /// Structure damage per second one leaked creature does at a gate or the base (mean over the ground stream, over the bosses),
        /// and the base damage one flyer that reaches the end does (mean): what a leak costs the base (<see cref="Leak"/>).
        /// </summary>
        public float GroundStrike;
        public float BossStrike;
        public float AirBaseDamage;

        /// <summary>Structure damage per second one flyer does when it lays siege to the base (mean).</summary>
        public float AirStrike;

        /// <summary>Structure damage per second the queue hammers the trunk gates with: the walkers (up to the rules' attackers) and the bosses apart.</summary>
        public float GateStrike;
        public float BossGateStrike;

        /// <summary>Raw seconds the trunk gates hold the walkers and the bosses, before the towers' usable share.</summary>
        public float GateHold;
        public float BossGateHold;

        /// <summary>The armour of the ground stream, the bosses and the flyers beside their health: how much of a weapon's damage counts, by its armour factor.</summary>
        public readonly ArmourMix StreamArmour = new ArmourMix();
        public readonly ArmourMix BossArmour = new ArmourMix();
        public readonly ArmourMix AirArmour = new ArmourMix();

        /// <summary>
        /// The seconds a gun gets on the bosses out of the time they spend in its reach: the mechanics take away their share
        /// of the fight (a burrow can happen anywhere on the road), never more than the rules' cap (Docs/BOSSES.md).
        /// </summary>
        public float BossSeconds(float inReach, BalanceRules rules) => inReach * (1f - Math.Min(rules.MaxBossMechanicShare, BossMechanicShare));

        public float GroundPressure(float stream, float single)
        {
            float pressure = 0f;
            if (StreamHealth > 0f)
            {
                pressure += Ratio(StreamHealth, stream * StreamReach);
            }

            if (BossHealth > 0f)
            {
                pressure += Ratio(BossHealth, single * BossReach);
            }

            return pressure;
        }

        public float AirPressure(float air) => AirHealth > 0f ? Ratio(AirHealth, air) : 0f;

        private static float Ratio(float health, float capacity) => capacity <= 0f ? float.PositiveInfinity : health / capacity;

        public static WaveLoad Of(LevelInput level, WaveSpec wave, DifficultyStats difficulty, BalanceRules rules) => Of(level, wave, difficulty, rules, -1f);

        /// <summary>As above with the gates at <paramref name="gateHealth"/> each instead of the level's full gate health (the plan model's worn gates; below zero = full).</summary>
        public static WaveLoad Of(LevelInput level, WaveSpec wave, DifficultyStats difficulty, BalanceRules rules, float gateHealth)
        {
            var load = new WaveLoad();
            float span = 0f;
            float airSpan = 0f;
            float speed = 0f;
            float airSpeed = 0f;
            float bossSpeed = 0f;
            int ground = 0;
            int air = 0;
            int bosses = 0;
            float reachWeighted = 0f;
            float bossReachWeighted = 0f;
            float gateDps = 0f;
            float bossGateDps = 0f;
            float strikeSum = 0f;
            float bossStrikeSum = 0f;
            float airBaseDamage = 0f;
            float airStrikeSum = 0f;
            float scale = difficulty.Health * level.HealthScale * wave.HealthMultiplier;

            foreach (GroupSpec group in wave.Groups)
            {
                EnemyStats enemy = group.Enemy;
                if (enemy == null)
                {
                    continue;
                }

                int count = enemy.IsBoss ? group.Count : Math.Max(1, (int)Math.Ceiling(group.Count * difficulty.Count));
                float each = enemy.EffectiveHealth * scale * difficulty.ShareFor(enemy);
                float health = each * count;
                float armour = enemy.Armor * scale * difficulty.ShareFor(enemy) * count;
                float end = group.Delay + (group.Stagger + (count - 1) * group.Interval) * difficulty.Interval;
                load.KillMoney += (int)Math.Round(enemy.Reward * count * level.Income);
                load.HasBoss |= enemy.IsBoss;
                float strike = enemy.AttackInterval > 0f ? enemy.BlockadeDamage / enemy.AttackInterval * difficulty.StructureDamage : 0f;
                load.Groups.Add(new WaveGroup
                {
                    Count = count,
                    Health = each - armour / count,
                    Armour = armour / count,
                    Speed = enemy.InRangeSpeed,
                    Path = group.Path,
                    Source = group.Source,
                    IsBoss = enemy.IsBoss,
                    Flies = !Layer.Has(enemy.Layer, Layer.Ground),
                });

                if (!Layer.Has(enemy.Layer, Layer.Ground))
                {
                    load.AirHealth += health;
                    load.AirArmour.Add(health - armour, armour);
                    airSpan = Math.Max(airSpan, end);
                    airSpeed += enemy.InRangeSpeed * count;
                    air += count;
                    airBaseDamage += enemy.BaseDamage * count;
                    airStrikeSum += strike * count;
                    continue;
                }

                float reach = Reach(level.Shape, group, rules);
                if (enemy.IsBoss)
                {
                    load.BossHealth += health;
                    load.BossArmour.Add(health - armour, armour);
                    bossReachWeighted += reach * health;
                    bossSpeed += enemy.InRangeSpeed * count;
                    bosses += count;
                    bossGateDps += strike;
                    bossStrikeSum += strike * count;
                    load.BossMechanicLoss += enemy.MechanicLossSeconds * count;
                    continue;
                }

                load.StreamHealth += health;
                load.StreamArmour.Add(health - armour, armour);
                reachWeighted += reach * health;
                span = Math.Max(span, end);
                speed += enemy.InRangeSpeed * count;
                ground += count;
                gateDps += strike * Math.Min(count, rules.AttackersPerGate);
                strikeSum += strike * count;
            }

            load.GroundCount = ground;
            load.BossCount = bosses;
            load.AirCount = air;
            load.GroundStrike = ground > 0 ? strikeSum / ground : 0f;
            load.BossStrike = bosses > 0 ? bossStrikeSum / bosses : 0f;
            load.AirBaseDamage = air > 0 ? airBaseDamage / air : 0f;
            load.AirStrike = air > 0 ? airStrikeSum / air : 0f;
            load.GroundSpeed = ground > 0 ? speed / ground : 1f;
            load.AirSpeed = air > 0 ? airSpeed / air : 1f;
            load.BossSpeed = bosses > 0 ? bossSpeed / bosses : 1f;
            load.MeanHealth = ground > 0 ? load.StreamHealth / ground : 0f;
            load.MeanAirHealth = air > 0 ? load.AirHealth / air : 0f;
            load.StreamReach = load.StreamHealth > 0f ? reachWeighted / load.StreamHealth : 1f;
            load.BossReach = load.BossHealth > 0f ? bossReachWeighted / load.BossHealth : 1f;
            // Creatures per cell of road: the spawn rate over the walking speed.
            load.GroundDensity = ground > 1 ? ground / Math.Max(1f, span) / load.GroundSpeed : 0f;
            load.AirDensity = air > 1 ? air / Math.Max(1f, airSpan) / load.AirSpeed : 0f;

            float gates = gateHealth < 0f ? level.GateHealth : gateHealth;
            float hold = GateHoldOf(level, gates, gateDps, rules);
            load.GateStrike = gateDps;
            load.BossGateStrike = bossGateDps;
            load.GateHold = hold;
            load.BossGateHold = GateHoldOf(level, gates, bossGateDps, rules);
            load.Span = span;
            load.GateSeconds = hold * rules.GateWindowShare;
            load.Window = span + load.GateSeconds;
            load.BossWindow = load.BossGateHold * rules.GateWindowShare;
            load.BossFight = bosses > 0 ? level.Shape.MainLength / Math.Max(0.01f, load.BossSpeed) + load.BossWindow : 0f;
            load.AirWindow = airSpan;
            return load;
        }

        // The defence a group meets: all of it on the trunk, the separate front's own share off it; creatures from a nest,
        // pool or entrance only meet what lies on the road ahead of where they join.
        private static float Reach(LevelShape shape, GroupSpec group, BalanceRules rules)
        {
            float trunk;
            if (group.Source == 0)
            {
                LevelShape.Lane lane = shape.LaneFor(group.Path);
                trunk = lane != null ? lane.TrunkShare : 1f;
            }
            else if (group.Source == EntranceSource)
            {
                trunk = shape.EntranceRemaining;
            }
            else
            {
                trunk = Mean(shape) ;
            }

            float share = Math.Min(1f, trunk / Math.Max(0.01f, rules.TrunkReference));
            return share + (1f - share) * rules.FrontEfficiency;
        }

        private static float Mean(LevelShape shape)
        {
            if (shape.NestRemaining.Count == 0)
            {
                return 1f;
            }

            float sum = 0f;
            foreach (float remaining in shape.NestRemaining)
            {
                sum += remaining;
            }

            return sum / shape.NestRemaining.Count;
        }

        // Seconds the gates on the main road hold the queue: each one until it breaks, at most the cap.
        private static float GateHoldOf(LevelInput level, float gateHealth, float attackDps, BalanceRules rules)
        {
            if (attackDps <= 0f || gateHealth <= 0f)
            {
                return 0f;
            }

            float total = 0f;
            foreach (LevelShape.Gate gate in level.Shape.Gates)
            {
                if (gate.OnTrunk || gate.PathIndex == (level.Shape.Lanes.Count > 0 ? level.Shape.Lanes[0].PathIndex : 0))
                {
                    total += Math.Min(rules.MaxGateHoldSeconds, gateHealth / attackDps);
                }
            }

            return total;
        }
    }
}
