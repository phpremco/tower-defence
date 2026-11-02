using System.Collections.Generic;

namespace Game.Balance
{
    /// <summary>Who a creature can be hit by, and what a gun can hit. Same bits as <c>Game.Core.TargetLayers</c>.</summary>
    public static class Layer
    {
        public const int Ground = 1;
        public const int Air = 2;

        public static bool Has(int mask, int layer) => (mask & layer) != 0;
    }

    /// <summary>The numbers of one creature that matter for balance.</summary>
    public sealed class EnemyStats
    {
        public string Id = string.Empty;
        public int Layer = Balance.Layer.Ground;
        public bool Flies;
        public float Health = 1f;
        public float Armor;
        public float Speed = 1f;
        public int Reward;
        public int BaseDamage = 1;
        public float AttackDamage;
        public float AttackInterval = 1f;
        public float BlockadeDamage;
        public bool IsBoss;

        /// <summary>Health plus armour: armour soaks that many points before it is gone, whatever its share per hit.</summary>
        public float EffectiveHealth => Health + Armor;
    }

    /// <summary>One upgrade level of a tower.</summary>
    public struct TowerLevelStats
    {
        public int Cost;
        public float Damage;
        public float Range;
        public float Interval;
        public float Slow;

        public TowerLevelStats(int cost, float damage, float range, float interval, float slow = 0f)
        {
            Cost = cost;
            Damage = damage;
            Range = range;
            Interval = interval;
            Slow = slow;
        }

        public float Dps => Interval > 0f ? Damage / Interval : 0f;
    }

    /// <summary>The numbers of one tower that matter for balance.</summary>
    public sealed class TowerStats
    {
        public string Id = string.Empty;
        public int Targets = Layer.Ground;
        public float Splash;
        public bool IsSupport;
        public bool BlocksGround;
        public bool IsBaseDefence;
        public float SupportRateBonus;
        public TowerLevelStats[] Levels = new TowerLevelStats[0];

        /// <summary>A gun the player builds: not a support mast, not a gate, not the base's own turret.</summary>
        public bool IsGun => !IsSupport && !BlocksGround && !IsBaseDefence && Levels.Length > 0;
    }

    /// <summary>The commander: the blade at a gate and the blaster for whatever is out of reach.</summary>
    public sealed class HeroStats
    {
        public float BladeDamage;
        public float BladeInterval = 1f;
        public float GunDamage;
        public float GunInterval = 1f;
    }

    /// <summary>A difficulty profile: how it scales the waves and how it pays.</summary>
    public sealed class DifficultyStats
    {
        public string Id = string.Empty;
        public int Index;
        public float Health = 1f;
        public float Count = 1f;
        public float Interval = 1f;
        public float Research = 1f;
        public float Power;
        public float StructureDamage = 1f;
    }

    /// <summary>A permanent upgrade bought with research or power between levels.</summary>
    public sealed class UpgradeStats
    {
        public string Id = string.Empty;
        public UpgradeKind Kind;
        public string TowerId = string.Empty;
        public bool PaidInPower;
        public float BonusPerLevel;
        public int[] Costs = new int[0];

        public int TotalCost
        {
            get
            {
                int sum = 0;
                foreach (int cost in Costs)
                {
                    sum += cost;
                }

                return sum;
            }
        }
    }

    /// <summary>What a permanent upgrade improves, as far as the balance model is concerned.</summary>
    public enum UpgradeKind
    {
        Other = 0,
        TowerDamage = 1,
        TowerRange = 2,
        TowerFireRate = 3,
        HeroDamage = 4,
        GateHealth = 5,
        BaseHealth = 6,
        BaseWeapon = 7,
    }

    /// <summary>One run of identical creatures inside a wave, as the game stores it.</summary>
    public struct GroupSpec
    {
        public EnemyStats Enemy;
        public int Count;
        public int Path;
        public int Source;
        public float Interval;
        public float Delay;
        public float Stagger;
    }

    /// <summary>One wave as the game stores it.</summary>
    public sealed class WaveSpec
    {
        public readonly List<GroupSpec> Groups = new List<GroupSpec>();
        public float StartDelay = 8f;
        public float HealthMultiplier = 1f;
    }
}
